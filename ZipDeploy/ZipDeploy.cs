using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ZipDeploy
{
    public class ZipDeploy
    {
        private enum State
        {
            Idle,
            FoundZip,
            UnzippingBinaries,
            AwaitingRestart,
        }

        private object              _stateLock      = new object();
        private State               _installState;
        private ILogger<ZipDeploy>  _log;
        private RequestDelegate     _next;
        private string              _iisUrl;

        public ZipDeploy(RequestDelegate next, ILogger<ZipDeploy> log, ZipDeployOptions options)
        {
            _log = log;
            _next = next;
            _iisUrl = options.IisUrl;

            _log.LogInformation($"ZipDeploy started [IisUrl={_iisUrl}]");

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

            var config = (string)null;

            _log.LogDebug("Opening publish.zip");
            using (var zipFile = ZipFile.OpenRead("publish.zip"))
            {
                var entries = zipFile.Entries
                    .ToDictionary(zfe => zfe.FullName, zfe => zfe);

                _log.LogDebug($"{entries.Count} entries in zip");

                var dlls = entries.Keys
                    .Where(k => Path.GetExtension(k)?.ToLower() == ".dll")
                    .ToList();

                _log.LogDebug($"{dlls.Count} dlls in zip");

                var dllsWithoutExtension = dlls.Select(dll => Path.GetFileNameWithoutExtension(dll)).ToList();

                foreach (var entry in entries)
                {
                    var fullName = entry.Key;

                    if (!dllsWithoutExtension.Contains(Path.GetFileNameWithoutExtension(fullName)))
                        continue;

                    if (File.Exists(fullName))
                    {
                        var destinationFile = $"{fullName}.fordelete.txt";

                        if (File.Exists(destinationFile))
                        {
                            _log.LogDebug($"deleting existing {destinationFile}");
                            File.Delete(destinationFile);
                        }

                        _log.LogDebug($"renaming {fullName} to {destinationFile}");
                        File.Move(fullName, destinationFile);
                    }

                    var zipEntry = entry.Value;

                    using (var streamWriter = File.Create(fullName))
                    using (var zipInput = zipEntry.Open())
                    {
                        _log.LogDebug($"extracting {fullName}");
                        zipInput.CopyTo(streamWriter);
                    }
                }

                if (entries.ContainsKey("web.config"))
                {
                    using (var zipInput = entries["web.config"].Open())
                    using (var sr = new StreamReader(zipInput))
                        config = sr.ReadToEnd();
                }
            }

            if (File.Exists("installing.zip"))
            {
                _log.LogDebug($"deleting existing installing.zip");
                File.Delete("installing.zip");
            }

            _log.LogDebug($"renaming publish.zip to installing.zip");
            File.Move("publish.zip", "installing.zip");

            _log.LogDebug("Triggering restart by touching web.config");
            config = config ?? File.ReadAllText("web.config");
            File.SetLastWriteTimeUtc("web.config", File.GetLastWriteTimeUtc("web.config") + TimeSpan.FromSeconds(1));
        }

        private void CompleteInstallation()
        {
            _log.LogDebug("detected intalling.zip; completing installation");
            if (File.Exists("installing.zip"))
            {
                using (var zipFile = ZipFile.OpenRead("installing.zip"))
                {
                    var entries = zipFile.Entries
                        .Where(e => e.Length != 0)
                        .ToDictionary(zfe => zfe.FullName, zfe => zfe);

                    var dlls = entries.Keys
                        .Where(k => Path.GetExtension(k)?.ToLower() == ".dll")
                        .ToList();

                    var dllsWithoutExtension = dlls.Select(dll => Path.GetFileNameWithoutExtension(dll)).ToList();

                    foreach (var entry in entries)
                    {
                        var fullName = entry.Key;

                        if (dllsWithoutExtension.Contains(Path.GetFileNameWithoutExtension(fullName)))
                            continue;

                        if (fullName == "web.config")
                            continue;

                        if (File.Exists(fullName))
                        {
                            var destinationFile = $"{fullName}.fordelete.txt";

                            if (File.Exists(destinationFile))
                                File.Delete(destinationFile);

                            File.Move(fullName, destinationFile);
                        }

                        var zipEntry = entry.Value;

                        var folder = Path.GetDirectoryName(fullName);

                        if (!string.IsNullOrWhiteSpace(folder))
                            Directory.CreateDirectory(folder);

                        using (var streamWriter = File.Create(fullName))
                        using (var zipInput = zipEntry.Open())
                            zipInput.CopyTo(streamWriter);
                    }
                }

                if (File.Exists("deployed.zip"))
                    File.Delete("deployed.zip");

                File.Move("installing.zip", "deployed.zip");
            }

            Task.Run(() => DeleteForDeleteFiles());
        }

        private void DeleteForDeleteFiles()
        {
            foreach (var forDelete in Directory.GetFiles(".", "*.fordelete.txt", SearchOption.AllDirectories))
            {
                while (File.Exists(forDelete))
                {
                    try
                    {
                        File.Delete(forDelete);
                    }
                    catch (Exception e)
                    {
                        _log.LogDebug(e, $"Error deleting {forDelete}");
                        Thread.Sleep(0);
                    }
                }
            }

            _log.LogDebug("Completed deletion of *.fordelete.txt files");
        }

        private void StartWatchingForInstaller()
        {
            var fsw = new FileSystemWatcher(Environment.CurrentDirectory, "publish.zip");
            fsw.Created += ZipFileChange;
            fsw.Changed += ZipFileChange;
            fsw.Renamed += ZipFileChange;
            fsw.EnableRaisingEvents = true;
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
            if (string.IsNullOrWhiteSpace(_iisUrl))
                return;

            Task.Run(() => Handler.LogAndSwallowException(_log, "CallIis", () =>
            {
                _log.LogDebug($"Making request to IIS: {_iisUrl}");
                using (var client = new HttpClient())
                using (client.GetAsync(_iisUrl).GetAwaiter().GetResult())
                { }
            }));
        }
    }
}
