using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ZipDeploy
{
    public class ZipDeploy
    {
        private RequestDelegate _next;
        private string          _restartUrl;

        public ZipDeploy(RequestDelegate next, ZipDeployOptions options)
        {
            _next = next;
            _restartUrl = options.RestartUrl;
        }

        public async Task Invoke(HttpContext context)
        {
            await _next(context);
            return;
        }
    }
}
