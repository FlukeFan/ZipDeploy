using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ZipDeploy
{
    public class ZipDeploy
    {
        private ILogger<ZipDeploy>  _log;
        private RequestDelegate     _next;
        private string              _restartUrl;

        public ZipDeploy(RequestDelegate next, ILogger<ZipDeploy> log, ZipDeployOptions options)
        {
            _log = log;
            _next = next;
            _restartUrl = options.RestartUrl;

            _log.LogInformation($"ZipDeploy started [RestartUrl={_restartUrl}]");
        }

        public async Task Invoke(HttpContext context)
        {
            await _next(context);
            return;
        }
    }
}
