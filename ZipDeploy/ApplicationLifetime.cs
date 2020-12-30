using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace ZipDeploy
{
    public class ApplicationLifetime : IHostedService
    {
        private readonly IApplicationLifetime _applicationLifetime;

        public ApplicationLifetime(IApplicationLifetime applicationLifetime)
        {
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
        }

        public virtual void Stopped()
        {
        }
    }
}
