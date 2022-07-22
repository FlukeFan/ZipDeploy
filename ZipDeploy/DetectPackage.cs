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
        Task StartedAsync(bool hadStartupErrors);
        void Stop();
    }

    public class DetectPackage : IDetectPackage, IDisposable
    {
        private ILogger<DetectPackage> _logger;
        private ZipDeployOptions _options;
        private FileSystemWatcher _fsw;

        public event Func<Task> PackageDetectedAsync;

        public DetectPackage(ILogger<DetectPackage> logger, ZipDeployOptions options)
        {
            _logger = logger;
            _options = options;
        }

        public virtual Task StartedAsync(bool hadStartupErrors)
        {
            _fsw = new FileSystemWatcher(Environment.CurrentDirectory, _options.NewPackageFileName);
            _fsw.Created += OnPackageDetected;
            _fsw.Changed += OnPackageDetected;
            _fsw.Renamed += OnPackageDetected;
            _fsw.Error += OnError;
            _fsw.EnableRaisingEvents = true;

            var restart = false;
            var restartReason = string.Empty;

            if (hadStartupErrors && _options.RestartOnStartupError)
            {
                restartReason = $"Error during startup";
                restart = true;
            }
            else if (File.Exists(_options.NewPackageFileName))
            {
                restartReason = $"Found {_options.NewPackageFileName} at startup";
                restart = true;
            }

            if (restart)
            {
                _logger.LogInformation($"{restartReason} - waiting {_options.StartupPublishDelay} to trigger restart");

                // don't wait on this task - it should run in the background, and trigger after the appropriate period
                Task.Run(async () =>
                {
                    await Task.Delay(_options.StartupPublishDelay);
                    await OnPackageDetectedAsync(this, null, restartReason);
                });
            }
            else
            {
                _logger.LogInformation($"Watching for {_options.NewPackageFileName} in {Environment.CurrentDirectory}");
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Stop();
        }

        public void Stop()
        {
            using (_fsw)
            {
                if (_fsw != null)
                    _fsw.EnableRaisingEvents = false;

                _fsw = null;
            }
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            var ex = e?.GetException();
            _logger.LogError(ex, $"Error in FileSystemWatcher: {ex?.Message}");
        }

        protected virtual void OnPackageDetected(object sender, FileSystemEventArgs e)
        {
            OnPackageDetectedAsync(sender, e, "Detected installation package").GetAwaiter().GetResult();
        }

        protected virtual async Task OnPackageDetectedAsync(object sender, FileSystemEventArgs e, string reason)
        {
            _logger.LogInformation($"OnPackageDetectedAsync: {reason}");

            await _logger.RetryAsync(_options, "zip file detected", async () =>
            {
                if (PackageDetectedAsync != null)
                    await PackageDetectedAsync();

                Stop();
            });
        }
    }
}
