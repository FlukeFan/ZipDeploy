using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ZipDeploy
{
    /// <summary> Raises PackageDetected when <see cref="ZipDeployOptions.WatchFilter" /> changes </summary>
    public interface IDetectPackage
    {
        event Func<Task> PackageDetectedAsync;
        Task StartedAsync();
    }

    public class DetectPackage : IDetectPackage
    {
        private ILogger<DetectPackage> _logger;
        private FileSystemWatcher _fsw;
        private ZipDeployOptions _options;

        public event Func<Task> PackageDetectedAsync;

        public DetectPackage(ILogger<DetectPackage> logger, ZipDeployOptions options)
        {
            _logger = logger;
            _options = options;

            _fsw = new FileSystemWatcher(Environment.CurrentDirectory, options.NewPackageFileName);
            _fsw.Created += OnPackageDetected;
            _fsw.Changed += OnPackageDetected;
            _fsw.Renamed += OnPackageDetected;
            _fsw.EnableRaisingEvents = true;
            _logger.LogInformation($"Watching for {options.NewPackageFileName} in {Environment.CurrentDirectory}");
        }

        public virtual Task StartedAsync()
        {
            if (File.Exists(_options.NewPackageFileName))
            {
                _logger.LogInformation($"Found {_options.NewPackageFileName} at startup - waiting {_options.StartupPublishDelay} to trigger restart");

                // don't wait on this task - it should run in the background, and trigger after the appropriate period
                Task.Run(async () =>
                {
                    await Task.Delay(_options.StartupPublishDelay);
                    await OnPackageDetectedAsync(this, null);
                });
            }

            return Task.CompletedTask;
        }

        protected virtual void OnPackageDetected(object sender, FileSystemEventArgs e)
        {
            OnPackageDetectedAsync(sender, e).GetAwaiter().GetResult();
        }

        protected virtual async Task OnPackageDetectedAsync(object sender, FileSystemEventArgs e)
        {
            _logger.LogInformation("Detected installation package");

            await _logger.RetryAsync(_options, "zip file detected", async () =>
            {
                if (PackageDetectedAsync != null)
                    await PackageDetectedAsync();

                _fsw.EnableRaisingEvents = false;
            });
        }
    }
}
