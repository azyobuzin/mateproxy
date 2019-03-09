using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MateProxy.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProxyKit;
using Rin.Core.Record;
using Rin.Storage.Redis;

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

            services.AddRin(options =>
            {
                if (this._options.IncludePatterns?.Length > 0 || this._options.ExcludePatterns?.Length > 0)
                    options.RequestRecorder.Excludes.Add(this.ExcludesRequest);
                options.RequestRecorder.RetentionMaxRequests = this._options.RetentionMaxRequest > 0
                    ? this._options.RetentionMaxRequest : int.MaxValue;
                options.RequestRecorder.EnableBodyCapturing = this._options.EnableBodyCapturing;
                options.RequestRecorder.AllowRunningOnProduction = true;

                if (this._options.Storage == StorageKind.Redis)
                {
                    options.RequestRecorder.StorageFactory = RedisRecordStorage.DefaultFactoryWithOptions(redisOptions =>
                    {
                        redisOptions.Expiry = this._options.RedisExpirationSeconds > 0
                            ? TimeSpan.FromSeconds(this._options.RedisExpirationSeconds)
                            : TimeSpan.MaxValue;
                        redisOptions.KeyPrefix = this._options.RedisKeyPrefix ?? "";
                        redisOptions.ConnectionConfiguration = this._options.RedisConnectionConfiguration;
                    });
                }

                options.Inspector.MountPath = this._options.InspectorPath;
                options.Inspector.ResponseBodyDataTransformers.Add(new UngzipTransformer());
            });

            WebSocketProxyMiddleware.Setup(this._options.SkipVerifyServerCertificate);
        }

        private bool ExcludesRequest(HttpRequest request)
        {
            var path = (string)request.Path;

            // IncludePatterns が設定されており、それらのどれにもマッチしないならば除外
            var includePatterns = this._options.IncludePatterns;
            if (includePatterns?.Length > 0)
            {
                var match = false;

                foreach (var includePattern in includePatterns)
                {
                    if (Regex.IsMatch(path, includePattern))
                    {
                        match = true;
                        break;
                    }
                }

                if (!match) return true;
            }

            // ExcludePatterns にマッチするならば除外
            var excludePatterns = this._options.ExcludePatterns;
            if (excludePatterns?.Length > 0)
            {
                foreach (var excludePattern in excludePatterns)
                {
                    if (Regex.IsMatch(path, excludePattern))
                        return true;
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
            var forwardContext = httpContext.ForwardTo(route.Upstream);

            if (route.CopyXForwardedHeaders) forwardContext.CopyXForwardedHeaders();
            if (route.AddXForwardedHeaders) forwardContext.AddXForwardedHeaders();

            using (TimelineScope.Create("SendRequest", TimelineEventCategory.Data,
                forwardContext.UpstreamRequest.RequestUri.ToString()))
            {
                return await forwardContext.Send();
            }
        }
    }
}
