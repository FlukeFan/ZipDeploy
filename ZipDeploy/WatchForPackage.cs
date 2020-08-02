using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace ZipDeploy
{
    /// <summary> Raises PackageDetected when <see cref="ZipDeployOptions.WatchFilter" /> changes </summary>
    public interface IWatchForPackage
    {
        event Action PackageDetected;
    }

    public class WatchForPackage : IWatchForPackage
    {
        private ILogger<WatchForPackage> _logger;
        private FileSystemWatcher _fsw;

        public event Action PackageDetected;

        public WatchForPackage(string watchfilter)
        {
            _logger = ZipDeploy.LoggerFactory.CreateLogger<WatchForPackage>();

            _fsw = new FileSystemWatcher(Environment.CurrentDirectory, watchfilter);
            _fsw.Created += OnPacakgeDetected;
            _fsw.Changed += OnPacakgeDetected;
            _fsw.Renamed += OnPacakgeDetected;
            _fsw.EnableRaisingEvents = true;
            _logger.LogInformation($"Watching for {watchfilter} in {Environment.CurrentDirectory}");
        }

        private void OnPacakgeDetected(object sender, FileSystemEventArgs e)
        {
            _logger.Try("zip file detected", () =>
                PackageDetected?.Invoke());
        }
    }
}
