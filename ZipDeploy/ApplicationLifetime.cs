using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ZipDeploy
{
    public class ApplicationLifetime : IHostedService
    {
        private readonly ILogger<ApplicationLifetime> _logger;
        private readonly Microsoft.AspNetCore.Hosting.IApplicationLifetime _applicationLifetime;

        public ApplicationLifetime(ILogger<ApplicationLifetime> logger, Microsoft.AspNetCore.Hosting.IApplicationLifetime applicationLifetime)
        {
            _logger = logger;
            _applicationLifetime = applicationLifetime;
        }

        Task IHostedService.StartAsync(CancellationToken cancellationToken)
        {
            _applicationLifetime.ApplicationStarted.Register(Started);
            _applicationLifetime.ApplicationStopped.Register(Stopped);
            return Task.CompletedTask;
        }

        Task IHostedService.StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public virtual void Started()
        {
            _logger.LogInformation("ZipDeploy startup - cleaning up");
        }

        public virtual void Stopped()
        {
            _logger.LogInformation("ZipDeploy unzipping after application stopped");
        }
    }
}
