using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using IApplicationLifetime = Microsoft.AspNetCore.Hosting.IApplicationLifetime;

namespace ZipDeploy
{
    public class ApplicationLifetime : IHostedService
    {
        private readonly ILogger<ApplicationLifetime> _logger;
        private readonly IApplicationLifetime _applicationLifetime;
        private readonly ICleaner _cleaner;
        private readonly IDetectPackage _detectPackage;
        private readonly ITriggerRestart _triggerRestart;
        private readonly ZipDeployOptions _options;
        private readonly IUnzipper _unzipper;

        public ApplicationLifetime(
            ILogger<ApplicationLifetime> logger,
            IApplicationLifetime applicationLifetime,
            ICleaner cleaner,
            IDetectPackage detectPackage,
            ITriggerRestart triggerRestart,
            ZipDeployOptions options,
            IUnzipper unzipper)
        {
            _logger = logger;
            _applicationLifetime = applicationLifetime;
            _cleaner = cleaner;
            _detectPackage = detectPackage;
            _triggerRestart = triggerRestart;
            _options = options;
            _unzipper = unzipper;
        }

        Task IHostedService.StartAsync(CancellationToken cancellationToken)
        {
            _applicationLifetime.ApplicationStarted.Register(BeforeStart);
            _applicationLifetime.ApplicationStopped.Register(AfterStopped);
            return Task.CompletedTask;
        }

        Task IHostedService.StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public virtual void BeforeStart()
        {
            _logger.LogInformation("ZipDeploy startup - cleaning up");

            _detectPackage.PackageDetected += () =>
                _triggerRestart.Trigger();

            _cleaner.DeleteObsoleteFiles();
        }

        public virtual void AfterStopped()
        {
            _logger.LogInformation("ZipDeploy unzipping after application stopped");

            _logger.Try("ZipDeploy before shutdown", () =>
            {
                if (File.Exists(Path.Combine(Environment.CurrentDirectory, _options.NewPackageFileName)))
                {
                    _logger.LogDebug("Found package {packageName}", _options.NewPackageFileName);
                    _unzipper.Unzip();
                }

                _logger.LogDebug("ZipDeploy completed after process shutdown");
            });
        }
    }
}
