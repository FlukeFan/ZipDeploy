using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace ZipDeploy
{
    /// <summary> Raises PackageDetected when <see cref="ZipDeployOptions.WatchFilter" /> changes </summary>
    public interface IDetectPackage
    {
        event Action PackageDetected;
    }

    public class DetectPackage : IDetectPackage
    {
        private ILogger<DetectPackage> _logger;
        private FileSystemWatcher _fsw;

        public event Action PackageDetected;

        public DetectPackage(ILogger<DetectPackage> logger, ZipDeployOptions options)
        {
            _logger = logger;

            _fsw = new FileSystemWatcher(Environment.CurrentDirectory, options.NewPackageFileName);
            _fsw.Created += OnPackageDetected;
            _fsw.Changed += OnPackageDetected;
            _fsw.Renamed += OnPackageDetected;
            _fsw.EnableRaisingEvents = true;
            _logger.LogInformation($"Watching for {options.NewPackageFileName} in {Environment.CurrentDirectory}");

            if (!string.IsNullOrWhiteSpace(options.LegacyPackageFileName) && File.Exists(options.LegacyPackageFileName))
                File.Move(options.LegacyPackageFileName, options.NewPackageFileName);
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
