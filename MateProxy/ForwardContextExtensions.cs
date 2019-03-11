using MateProxy.Options;
using ProxyKit;

namespace MateProxy
{
    internal static class ForwardContextExtensions
    {
        public static ForwardContext ApplyRouteOptions(this ForwardContext forwardContext, RouteOptions route)
        {
            if (route.CopyXForwardedHeaders)
                forwardContext.CopyXForwardedHeaders();

            if (route.AddXForwardedHeaders)
                forwardContext.AddXForwardedHeaders();

            var headers = forwardContext.UpstreamRequest.Headers;

            if (route.HostHeaderMode == HostHeaderMode.PreserveHost)
            {
                headers.Host = forwardContext.HttpContext.Request.Host.Value;
            }
            else if (forwardContext.HttpContext.Request.Headers.TryGetValue(XForwardedExtensions.XForwardedHost, out var forwardedHostValues)
                && forwardedHostValues.Count > 0)
            {
                if (route.HostHeaderMode == HostHeaderMode.FirstXForwardedHost)
                {
                    headers.Host = forwardedHostValues[0];
                }
                else if (route.HostHeaderMode == HostHeaderMode.LastXForwardedHost)
                {
                    headers.Host = forwardedHostValues[forwardedHostValues.Count - 1];
                }
            }

            return forwardContext;
        }
    }
}
