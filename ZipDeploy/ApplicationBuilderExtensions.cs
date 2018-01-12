using System;
using Microsoft.AspNetCore.Builder;

namespace ZipDeploy
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseZipDeploy(this IApplicationBuilder app)
        {
            return app.UseZipDeploy(null);
        }

        public static IApplicationBuilder UseZipDeploy(this IApplicationBuilder app, Action<ZipDeployOptions> configure)
        {
            var options = new ZipDeployOptions();
            configure?.Invoke(options);
            return app.UseMiddleware<ZipDeploy>(options);
        }
    }
}
