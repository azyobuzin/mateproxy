using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using MateProxy.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using ProxyKit;
using Rin.Core.Record;

namespace MateProxy
{
    public class WebSocketProxyMiddleware
    {
        private static HttpMessageInvoker s_webSocketHttpClient;
        private static readonly MethodInfo s_copyProxyHttpResponse = typeof(ProxyMiddleware)
            .GetMethod("CopyProxyHttpResponse", BindingFlags.NonPublic | BindingFlags.Static);

        public static void Setup(bool skipVerifyServerCertificate)
        {
            // https://github.com/dotnet/corefx/blob/700f035b9533ed4e439de83e99da1293b7a82e93/src/System.Net.WebSockets.Client/src/System/Net/WebSockets/WebSocketHandle.Managed.cs#L123-L152
            var handler = new SocketsHttpHandler()
            {
                AllowAutoRedirect = false,
                UseCookies = false,
                PooledConnectionLifetime = TimeSpan.Zero,
            };

            if (skipVerifyServerCertificate)
            {
                handler.SslOptions.RemoteCertificateValidationCallback =
                    (sender, certificate, chain, sslPolicyErrors) => true;
            }

            // HttpClient ではなく素の Handler を叩いてあげるようにする
            s_webSocketHttpClient = new HttpMessageInvoker(handler);
        }

        private readonly RequestDelegate _next;
        private readonly RouteOptions _route;
        private readonly ILogger _logger;

        public WebSocketProxyMiddleware(RequestDelegate next, RouteOptions route, ILogger<WebSocketProxyMiddleware> logger)
        {
            this._next = next;
            this._route = route;
            this._logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                await this._next(context);
                return;
            }

            this._logger.LogInformation("Accepting WebSocket");

            var forwardContext = context.ForwardTo(this._route.Upstream);
            if (this._route.CopyXForwardedHeaders) forwardContext.CopyXForwardedHeaders();
            if (this._route.AddXForwardedHeaders) forwardContext.AddXForwardedHeaders();

            var upstreamRequest = forwardContext.UpstreamRequest;

            HttpResponseMessage upstreamResponse;

            using (TimelineScope.Create("SendRequest", TimelineEventCategory.Data,
               forwardContext.UpstreamRequest.RequestUri.ToString()))
            {
                upstreamResponse = await s_webSocketHttpClient.SendAsync(upstreamRequest, context.RequestAborted);
            }

            using (upstreamResponse)
            {
                if (upstreamResponse.StatusCode != HttpStatusCode.SwitchingProtocols)
                {
                    // WebSocket での接続に失敗したので、 ProxyMiddleware と同じ操作をして終了
                    await (Task)s_copyProxyHttpResponse.Invoke(null, new object[] { context, upstreamResponse });
                    return;
                }

                this._logger.LogInformation("Upgrading the connection");

                // ヘッダーをコピーして、 101 を返す

                foreach (var header in upstreamResponse.Headers)
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }

                var upgradeFeature = context.Features.Get<IHttpUpgradeFeature>();

                using (var clientStream = await upgradeFeature.UpgradeAsync())
                {
                    this._logger.LogInformation("Upgraded the connection");

                    var upstreamStream = await upstreamResponse.Content.ReadAsStreamAsync();

                    // ひたすらストリームを受け流す
                    await Task.WhenAll(
                        clientStream.CopyToAsync(upstreamStream),
                        upstreamStream.CopyToAsync(clientStream)
                    );
                }
            }
        }
    }
}
