using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
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

            LoggerFactory = LoggerFactory ?? new LoggerFactory();
            _logger = LoggerFactory.CreateLogger<ZipDeploy>();
            _logger.LogDebug("ZipDeploy starting");

            try
            {
                setupOptions?.Invoke(options);

                var detectPackage = options.DetectPackage = options.DetectPackage ?? options.NewDetectPackage();
                var triggerRestart = options.TriggerRestart ?? options.NewTriggerRestart();
                options.QueryPackageName = options.QueryPackageName ?? options.NewQueryPackageName();

                detectPackage.PackageDetected += () => triggerRestart.Trigger();

                // DeleteForDeleteFiles();
                
                // if (!filesToUnzip)
                    program();
            }
            finally
            {
                var packageName = options.QueryPackageName.FindPackageName();

                if (packageName != null)
                {
                    _logger.LogDebug("Found package {packageName}", packageName);
                    File.Move(packageName, options.DeployedPackageFileName);
                }

                // RenameBinaries
                // Unzip
                _logger.LogDebug("ZipDeploy completed after process shutdown");
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

        public async Task Invoke(HttpContext context)
        {
            await _next(context);

            if (_installState != State.FoundZip)
                return;

            _log.LogAndSwallowException("ZipDeploy.Invoke - installerDetected", () =>
            {
                try
                {
                    lock(_stateLock)
                    {
                        if (_installState != State.FoundZip)
                            return;

                        _installState = State.UnzippingBinaries;
                    }

                    InstallBinaries();

                    lock(_stateLock)
                        _installState = State.AwaitingRestart;
                }
                catch (Exception e)
                {
                    _log.LogError(e, $"Exception unzipping binaries");

                    lock (_stateLock)
                        _installState = State.FoundZip;

                    throw;
                }
            });
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

            if (_installState == State.FoundZip)
                StartWebRequest();
        }

        private bool NewZipFileExists()
        {
            return File.Exists(_options.NewPackageFileName);
        }

        private void StartWebRequest()
        {
            if (string.IsNullOrWhiteSpace(_options.IisUrl))
                return;

            Task.Run(() => Handler.LogAndSwallowException(_log, "CallIis", () =>
            {
                _log.LogDebug($"Making request to IIS: {_options.IisUrl}");
                using (var client = new HttpClient())
                using (client.GetAsync(_options.IisUrl).GetAwaiter().GetResult())
                { }
            }));
        }
    }
}
