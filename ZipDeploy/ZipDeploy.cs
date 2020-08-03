using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ZipDeploy
{
    public class ZipDeploy
    {
        private static ILogger<ZipDeploy> _logger;

        public static void Run(Action<ZipDeployOptions> setupOptions, Action program)
        {
            var options = new ZipDeployOptions();
            var context = new ZipContext();

            LoggerFactory = LoggerFactory ?? new LoggerFactory();
            _logger = LoggerFactory.CreateLogger<ZipDeploy>();
            _logger.LogDebug("ZipDeploy starting");

            try
            {
                setupOptions?.Invoke(options);

                var detectPackage = options.DetectPackage = options.DetectPackage ?? options.NewDetectPackage();
                var triggerRestart = options.TriggerRestart ?? options.NewTriggerRestart();
                var queryPackageName = options.QueryPackageName = options.QueryPackageName ?? options.NewQueryPackageName();

                detectPackage.PackageDetected += () =>
                {
                    context.SetPackageName(queryPackageName.FindPackageName());
                    triggerRestart.Trigger(context);
                };

                // DeleteForDeleteFiles();
                
                // if (!filesToUnzip)
                    program();
            }
            finally
            {
                using (context)
                    _logger.Try("ZipDeploy before shutdown", () =>
                    {
                        var packageName = options.QueryPackageName.FindPackageName();

                        if (packageName != null)
                        {
                            _logger.LogDebug("Found package {packageName}", packageName);
                            var unzipper = new Unzipper(options);
                            unzipper.UnzipBinaries();
                            unzipper.SyncNonBinaries(deleteObsolete: false);
                        }

                        _logger.LogDebug("ZipDeploy completed after process shutdown");
                    });
            }
        }

        public static ZipDeployOptions DefaultOptions()
        {
            return new ZipDeployOptions();
        }

        private enum State
        {
            Idle,
            FoundZip,
            UnzippingBinaries,
            AwaitingRestart,
        }

        public static ILoggerFactory LoggerFactory = new LoggerFactory();

        private object              _stateLock      = new object();
        private State               _installState;
        private ILogger<ZipDeploy>  _log;
        private RequestDelegate     _next;
        private ZipDeployOptions    _options;

        public ZipDeploy(RequestDelegate next, ZipDeployOptions options)
        {
            _log = LoggerFactory.CreateLogger<ZipDeploy>();
            _next = next;
            _options = options;

            _log.LogInformation($"ZipDeploy started "
                + $"[IisUrl={_options.IisUrl}] "
                + $"[NewPackageFileName={_options.NewPackageFileName}] "
                + $"[LegacyTempFileName={_options.LegacyTempFileName}] "
                + $"[DeployedPackageFileName={_options.DeployedPackageFileName}] "
                + $"[HashesFileName={_options.HashesFileName}] "
                + $"[IgnoredPaths={string.Join(", ", _options.PathsToIgnore)}] "
                + $"[UserDomainName={Environment.UserDomainName}] [UserName={Environment.UserName}]");

            //CompleteInstallation();

            _installState = State.Idle;

            //StartWatchingForInstaller();
            DetectInstaller();
        }

        private void InstallBinaries()
        {
            _log.LogDebug("Installing binaries (and renaming old ones)");
            var unzipper = new Unzipper(_options);
            unzipper.UnzipBinaries();
        }

        //private void CompleteInstallation()
        //{
        //    if (File.Exists(_options.TempZipFileName))
        //    {
        //        _log.LogDebug($"detected {_options.TempZipFileName}; completing installation");

        //        var unzipper = new Unzipper(_options);
        //        unzipper.SyncNonBinaries();
        //    }
        //}

        private void DetectInstaller()
        {
            if (_installState != State.Idle)
                return;

            lock (_stateLock)
            {
                if (_installState != State.Idle)
                    return;

                if (NewZipFileExists())
                {
                    _log.LogDebug("Detected installer");
                    _installState = State.FoundZip;
                }
            }
        }

        private bool NewZipFileExists()
        {
            return File.Exists(_options.NewPackageFileName);
        }
    }
}
