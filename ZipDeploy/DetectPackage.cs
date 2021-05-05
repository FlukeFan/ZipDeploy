using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ZipDeploy
{
    /// <summary> Raises PackageDetected when <see cref="ZipDeployOptions.WatchFilter" /> changes </summary>
    public interface IDetectPackage
    {
        event Action PackageDetected;
        void Started();
    }

    public class DetectPackage : IDetectPackage
    {
        private ILogger<DetectPackage> _logger;
        private FileSystemWatcher _fsw;
        private ZipDeployOptions _options;

        public event Action PackageDetected;

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

        public void Started()
        {
            if (File.Exists(_options.NewPackageFileName))
            {
                _logger.LogInformation($"Found {_options.NewPackageFileName} at startup - waiting {_options.StartupPublishDelay} to trigger restart");

                Task.Run(async () =>
                {
                    await Task.Delay(_options.StartupPublishDelay);
                    OnPackageDetected(null, null);
                });
            }
        }

        private void OnPackageDetected(object sender, FileSystemEventArgs e)
        {
            _logger.LogInformation("Detected installation package");
            _logger.Try("zip file detected", () =>
            {
                PackageDetected?.Invoke();
                _fsw.EnableRaisingEvents = false;
            });
        }
    }
}
