using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MateProxy.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProxyKit;

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
            services.AddProxy();

            services.AddRin(options =>
            {
                options.RequestRecorder.AllowRunningOnProduction = true;

                // TODO
            });
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            app.UseRin();

            // TODO: 設定に応じてプロキシを設定

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }
    }
}
