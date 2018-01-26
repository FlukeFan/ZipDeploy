using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace ZipDeploy
{
    public class Unzipper
    {
        private ILogger<Unzipper>   _log;

        public Unzipper()
        {
            _log = ZipDeploy.LogFactory.CreateLogger<Unzipper>();
        }

        public void UnzipBinaries()
        {
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

            if (!string.IsNullOrEmpty(config) || File.Exists("web.config"))
            {
                _log.LogDebug("Triggering restart by touching web.config");
                config = config ?? File.ReadAllText("web.config");
                File.WriteAllText("web.config", config);
                File.SetLastWriteTimeUtc("web.config", File.GetLastWriteTimeUtc("web.config") + TimeSpan.FromSeconds(1));
            }
        }

        public void SyncNonBinaries()
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

            DeleteForDeleteFiles();
        }

        private void DeleteForDeleteFiles()
        {
            foreach (var forDelete in Directory.GetFiles(".", "*.fordelete.txt", SearchOption.AllDirectories))
            {
                var count = 3;

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

                        if (count-- <= 0)
                            throw;
                    }
                }
            }

            _log.LogDebug("Completed deletion of *.fordelete.txt files");
        }
    }
}
