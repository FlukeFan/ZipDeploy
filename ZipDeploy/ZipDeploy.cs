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

            _log.LogInformation($"ZipDeploy started [IisUrl={_options.IisUrl}] [IgnoredPaths={string.Join(", ", _options.PathsToIgnore)}]");

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
                    {
                        _installState = State.AwaitingRestart;
                    }
                }
                catch
                {
                    // replace the state so we can try again
                    _installState = State.FoundZip;
                    StartWebRequest();
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
            if (File.Exists("installing.zip"))
            {
                _log.LogDebug("detected installing.zip; completing installation");

                var unzipper = new Unzipper(_options);
                unzipper.SyncNonBinaries();
            }
        }

        private void StartWatchingForInstaller()
        {
            _fsw = new FileSystemWatcher(Environment.CurrentDirectory, "publish.zip");
            _fsw.Created += ZipFileChange;
            _fsw.Changed += ZipFileChange;
            _fsw.Renamed += ZipFileChange;
            _fsw.EnableRaisingEvents = true;
            _log.LogInformation($"Watching for publish.zip in {Environment.CurrentDirectory}");
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
            return File.Exists("publish.zip");
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
