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

            _fsw = new FileSystemWatcher(Environment.CurrentDirectory, options.DeployedPackageFileName);
            _fsw.Created += OnPacakgeDetected;
            _fsw.Changed += OnPacakgeDetected;
            _fsw.Renamed += OnPacakgeDetected;
            _fsw.EnableRaisingEvents = true;
            _logger.LogInformation($"Watching for {options.DeployedPackageFileName} in {Environment.CurrentDirectory}");
        }

        private void OnPacakgeDetected(object sender, FileSystemEventArgs e)
        {
            _logger.Try("zip file detected", () =>
                PackageDetected?.Invoke());
        }
    }
}
