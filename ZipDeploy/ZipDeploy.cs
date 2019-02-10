using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ZipDeploy
{
    public class ZipDeploy : IDisposable
    {
        private enum State
        {
            Idle,
            FoundZip,
            UnzippingBinaries,
            AwaitingRestart,
        }

        public static ILoggerFactory LogFactory;

        private object              _stateLock      = new object();
        private State               _installState;
        private FileSystemWatcher   _fsw;
        private ILogger<ZipDeploy>  _log;
        private RequestDelegate     _next;
        private ZipDeployOptions    _options;

        public ZipDeploy(RequestDelegate next, ILoggerFactory logFactory, ZipDeployOptions options)
        {
            LogFactory = logFactory;
            _log = logFactory.CreateLogger<ZipDeploy>();
            _next = next;
            _options = options;

            _log.LogInformation($"ZipDeploy started "
                + $"[IisUrl={_options.IisUrl}] "
                + $"[NewZipFileName={_options.NewZipFileName}] "
                + $"[TempZipFileName={_options.TempZipFileName}] "
                + $"[DeployedZipFileName={_options.DeployedZipFileName}] "
                + $"[HashesFileName={_options.HashesFileName}] "
                + $"[IgnoredPaths={string.Join(", ", _options.PathsToIgnore)}] "
                + $"[UserDomainName={Environment.UserDomainName}] [UserName={Environment.UserName}]");

            CompleteInstallation();

            _installState = State.Idle;

            StartWatchingForInstaller();
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

        private void CompleteInstallation()
        {
            if (File.Exists(_options.TempZipFileName))
            {
                _log.LogDebug($"detected {_options.TempZipFileName}; completing installation");

                var unzipper = new Unzipper(_options);
                unzipper.SyncNonBinaries();
            }
        }

        private void StartWatchingForInstaller()
        {
            _fsw = new FileSystemWatcher(Environment.CurrentDirectory, _options.NewZipFileName);
            _fsw.Created += ZipFileChange;
            _fsw.Changed += ZipFileChange;
            _fsw.Renamed += ZipFileChange;
            _fsw.EnableRaisingEvents = true;
            _log.LogInformation($"Watching for {_options.NewZipFileName} in {Environment.CurrentDirectory}");
        }

        private void ZipFileChange(object sender, FileSystemEventArgs e)
        {
            _log.LogAndSwallowException("ZipFileChange", DetectInstaller);
        }

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
            return File.Exists(_options.NewZipFileName);
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

        public void Dispose()
        {
            using (_fsw)
                _fsw.EnableRaisingEvents = false;
        }
    }
}
