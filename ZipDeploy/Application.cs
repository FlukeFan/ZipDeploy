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
        private readonly ILockProcess _lockProcess;
        private readonly ICleaner _cleaner;
        private readonly IDetectPackage _detectPackage;
        private readonly ITriggerRestart _triggerRestart;
        private readonly ZipDeployOptions _options;
        private readonly IUnzipper _unzipper;

        public Application(
            ILogger<Application> logger,
            ILockProcess lockProcess,
            ICleaner cleaner,
            IDetectPackage detectPackage,
            ITriggerRestart triggerRestart,
            ZipDeployOptions options,
            IUnzipper unzipper)
        {
            _logger = logger;
            _lockProcess = lockProcess;
            _cleaner = cleaner;
            _detectPackage = detectPackage;
            _triggerRestart = triggerRestart;
            _options = options;
            _unzipper = unzipper;
        }

        async Task IHostedService.StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Application startup");
            await _lockProcess.LockAsync();

            _logger.LogDebug("ZipDeploy wireup package detection");
            _detectPackage.PackageDetectedAsync += _triggerRestart.TriggerAsync;

            await _logger.RetryAsync(_options, "Delete obsolete files", () =>
                { _cleaner.DeleteObsoleteFiles(); return Task.CompletedTask; });

            await _logger.RetryAsync(_options, "Start package detection", () =>
                _detectPackage.StartedAsync());
        }

        async Task IHostedService.StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Application stopped");

            await _logger.RetryAsync(_options, "ZipDeploy before shutdown", async () =>
            {
                if (File.Exists(Path.Combine(Environment.CurrentDirectory, _options.NewPackageFileName)))
                {
                    _logger.LogDebug("Found package {packageName}", _options.NewPackageFileName);
                    await _unzipper.UnzipAsync();
                }

                _logger.LogDebug("ZipDeploy completed after process shutdown");
            });
        }
    }
}
