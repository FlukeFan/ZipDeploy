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

        public virtual async Task StartedAsync()
        {
            if (File.Exists(_options.NewPackageFileName))
            {
                _logger.LogInformation($"Found {_options.NewPackageFileName} at startup - waiting {_options.StartupPublishDelay} to trigger restart");

                await Task.Run(async () =>
                {
                    await Task.Delay(_options.StartupPublishDelay);
                    await OnPackageDetectedAsync(null);
                });
            }
        }

        protected virtual void OnPackageDetected(object sender, FileSystemEventArgs e)
        {
            OnPackageDetectedAsync(e).GetAwaiter().GetResult();
        }

        protected virtual async Task OnPackageDetectedAsync(FileSystemEventArgs e)
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
