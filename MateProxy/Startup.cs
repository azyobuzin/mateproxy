using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MateProxy.BodyDataTransformers;
using MateProxy.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProxyKit;
using Rin.Core.Record;

namespace MateProxy
{
    public class Startup
    {
        private readonly MateProxyOptions _options;

        public Startup(IConfiguration config)
        {
            this._options = config.Get<MateProxyOptions>();
            this._options.Validate();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddProxy(httpClientBuilder =>
            {
                if (this._options.SkipVerifyServerCertificate)
                {
                    httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() =>
                        new HttpClientHandler
                        {
                            AllowAutoRedirect = false,
                            UseCookies = false,
                            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                        });
                }
            });

            var rinBuilder = services.AddRin(options =>
            {
                if (this._options.IncludePathPatterns?.Length > 0 || this._options.ExcludePathPatterns?.Length > 0
                    || this._options.IncludeHeaderPatterns?.Length > 0 || this._options.ExcludeHeaderPatterns?.Length > 0)
                    options.RequestRecorder.Excludes.Add(this.ExcludesRequest);
                options.RequestRecorder.RetentionMaxRequests = this._options.RetentionMaxRequest > 0
                    ? this._options.RetentionMaxRequest : int.MaxValue;
                options.RequestRecorder.EnableBodyCapturing = this._options.EnableBodyCapturing;
                options.RequestRecorder.AllowRunningOnProduction = true;

                options.Inspector.MountPath = this._options.InspectorPath;
            });

            if (this._options.Storage == StorageKind.Redis)
            {
                rinBuilder.UseRedisStorage(options =>
                {
                    options.Expiry = this._options.RedisExpirationSeconds > 0
                            ? TimeSpan.FromSeconds(this._options.RedisExpirationSeconds)
                            : TimeSpan.MaxValue;
                    options.KeyPrefix = this._options.RedisKeyPrefix ?? "";
                    options.ConnectionConfiguration = this._options.RedisConnectionConfiguration;
                });
            }

            services.AddSingleton(BodyDataTransformerChain.DefaultRequestBodyDataTransformer);
            services.AddSingleton(BodyDataTransformerChain.DefaultResponseBodyDataTransformer);

            WebSocketProxyMiddleware.Setup(this._options.SkipVerifyServerCertificate);
        }

        private bool ExcludesRequest(HttpRequest request)
        {
            var path = (string)request.Path;

            // IncludePathPatterns が設定されており、それらのどれにもマッチしないならば除外
            var includePathPatterns = this._options.IncludePathPatterns;
            if (includePathPatterns?.Length > 0)
            {
                var match = false;

                foreach (var includePattern in includePathPatterns)
                {
                    if (Regex.IsMatch(path, includePattern))
                    {
                        match = true;
                        break;
                    }
                }

                if (!match) return true;
            }

            // ExcludePathPatterns にマッチするならば除外
            var excludePathPatterns = this._options.ExcludePathPatterns;
            if (excludePathPatterns?.Length > 0)
            {
                foreach (var excludePattern in excludePathPatterns)
                {
                    if (Regex.IsMatch(path, excludePattern))
                        return true;
                }
            }

            var headers = request.Headers;

            // IncludeHeaderPatterns が設定されており、それらのどれにもマッチしないならば除外
            var includeHeaderPatterns = this._options.IncludeHeaderPatterns;
            if (includeHeaderPatterns?.Length > 0)
            {
                var match = false; // OR

                foreach (var patternDic in includeHeaderPatterns)
                {
                    if (patternDic == null) continue;

                    var headerPatterns = patternDic
                        .Where(x => !string.IsNullOrEmpty(x.Key) && !string.IsNullOrEmpty(x.Value))
                        .ToArray();

                    if (headerPatterns.Length == 0) continue;

                    var innerMatch = true; // AND

                    foreach (var (key, pattern) in headerPatterns)
                    {
                        headers.TryGetValue(key, out var headerValues);
                        // ToString で ?? string.Empty される
                        var headerValue = headerValues.ToString();

                        if (!Regex.IsMatch(headerValue, pattern))
                        {
                            innerMatch = false;
                            break;
                        }
                    }

                    if (innerMatch)
                    {
                        match = true;
                        break;
                    }
                }

                if (!match) return true;
            }

            // ExcludeHeaderPatterns にマッチするならば除外
            var excludeHeaderPatterns = this._options.ExcludeHeaderPatterns;
            if (excludeHeaderPatterns?.Length > 0)
            {
                foreach (var patternDic in excludeHeaderPatterns)
                {
                    if (patternDic == null) continue;

                    var headerPatterns = patternDic
                        .Where(x => !string.IsNullOrEmpty(x.Key) && !string.IsNullOrEmpty(x.Value))
                        .ToArray();

                    if (headerPatterns.Length == 0) continue;

                    var innerMatch = true; // AND

                    foreach (var (key, pattern) in headerPatterns)
                    {
                        headers.TryGetValue(key, out var headerValues);
                        var headerValue = headerValues.ToString();

                        if (!Regex.IsMatch(headerValue, pattern))
                        {
                            innerMatch = false;
                            break;
                        }
                    }

                    if (innerMatch) return true; // OR
                }
            }

            return false;
        }

        public void Configure(IApplicationBuilder app, ILogger<Startup> log)
        {
            app.UseRin();

            foreach (var route in this._options.Routes)
            {
                log.LogInformation(
                    "Configuring route '{RouteName}' ({Path} -> {Upstream})",
                    route.Name, route.Path, route.Upstream);

                app.Map(route.Path.TrimEnd('/'), builder =>
                {
                    builder.Use((context, next) =>
                    {
                        log.LogInformation(
                            "Use route '{RouteName}' ({Path} -> {Upstream})",
                            route.Name, route.Path, route.Upstream);
                        return next();
                    });
                    builder.UseWebSockets();
                    builder.UseMiddleware(typeof(WebSocketProxyMiddleware), route);
                    builder.RunProxy(context => HandleRoute(route, context));
                });
            }
        }

        private static async Task<HttpResponseMessage> HandleRoute(RouteOptions route, HttpContext httpContext)
        {
            var forwardContext = httpContext.ForwardTo(route.Upstream).ApplyRouteOptions(route);

            using (TimelineScope.Create("SendRequest", TimelineEventCategory.Data,
                forwardContext.UpstreamRequest.RequestUri.ToString()))
            {
                return await forwardContext.Send();
            }
        }
    }
}
