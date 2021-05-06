using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ZipDeploy
{
    public class Application : IHostedService
    {
        private readonly ILogger<Application> _logger;
        private readonly ICleaner _cleaner;
        private readonly IDetectPackage _detectPackage;
        private readonly ITriggerRestart _triggerRestart;
        private readonly ZipDeployOptions _options;
        private readonly IUnzipper _unzipper;

        public Application(
            ILogger<Application> logger,
            ICleaner cleaner,
            IDetectPackage detectPackage,
            ITriggerRestart triggerRestart,
            ZipDeployOptions options,
            IUnzipper unzipper)
        {
            _logger = logger;
            _cleaner = cleaner;
            _detectPackage = detectPackage;
            _triggerRestart = triggerRestart;
            _options = options;
            _unzipper = unzipper;
        }

        Task IHostedService.StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Application startup");

            _logger.LogDebug("ZipDeploy wireup package detection");
            _detectPackage.PackageDetected += _triggerRestart.Trigger;

            _logger.Retry(_options, "Delete obsolete files", () =>
                _cleaner.DeleteObsoleteFiles());

            _logger.Retry(_options, "Start package detection", () =>
                _detectPackage.Started());

            return Task.CompletedTask;
        }

        Task IHostedService.StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Application stopped");

            _logger.Retry(_options, "ZipDeploy before shutdown", () =>
            {
                if (File.Exists(Path.Combine(Environment.CurrentDirectory, _options.NewPackageFileName)))
                {
                    _logger.LogDebug("Found package {packageName}", _options.NewPackageFileName);
                    _unzipper.Unzip();
                }

                _logger.LogDebug("ZipDeploy completed after process shutdown");
            });

            return Task.CompletedTask;
        }
    }
}
