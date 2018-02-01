using System;
using System.Collections.Generic;
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

            UsingArchive("publish.zip", (entries, dllsWithoutExtension) =>
            {
                foreach (var entry in entries)
                {
                    var fullName = entry.Key;

                    if (!dllsWithoutExtension.Contains(PathWithoutExtension(fullName)))
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
            });

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
            var zippedFiles = new List<string>();

            UsingArchive("installing.zip", (entries, dllsWithoutExtension) =>
            {
                foreach (var entry in entries)
                {
                    var fullName = entry.Key;
                    zippedFiles.Add(NormalisePath(fullName));

                    if (dllsWithoutExtension.Contains(PathWithoutExtension(fullName)))
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
            });

            if (File.Exists("deployed.zip"))
                File.Delete("deployed.zip");

            File.Move("installing.zip", "deployed.zip");

            DeleteObsoleteFiles(zippedFiles);
        }

        private void UsingArchive(string zipFile, Action<IDictionary<string, ZipArchiveEntry>, IList<string>> action)
        {
            _log.LogDebug($"Opening {zipFile}");

            using (var zipArchive = ZipFile.OpenRead(zipFile))
            {
                var entries = zipArchive.Entries
                    .Where(e => e.Length != 0)
                    .ToDictionary(zfe => zfe.FullName, zfe => zfe);

                var dlls = entries.Keys
                    .Where(k => Path.GetExtension(k)?.ToLower() == ".dll")
                    .ToList();

                _log.LogDebug($"{dlls.Count} binaries (dlls) in zip");

                var dllsWithoutExtension = dlls.Select(dll => PathWithoutExtension(dll)).ToList();

                action(entries, dllsWithoutExtension);
            }
        }

        public static string PathWithoutExtension(string file)
        {
            var extension = Path.GetExtension(file);

            while (!string.IsNullOrEmpty(extension))
            {
                file = file.Substring(0, file.Length - extension.Length);
                extension = Path.GetExtension(file);
            }

            return file;
        }

        private void DeleteObsoleteFiles(IList<string> zippedFiles)
        {
            foreach (var forDelete in Directory.GetFiles(".", "*", SearchOption.AllDirectories).Select(f => NormalisePath(f)))
            {
                if (zippedFiles.Contains(forDelete) || ShouldIgnore(forDelete))
                    continue;

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

            _log.LogDebug("Completed deletion of obsolete files");
        }

        private bool ShouldIgnore(string forDeleteFile)
        {
            var file = Path.GetFileName(forDeleteFile);

            var knownZips = new List<string>
            {
                "publish.zip",
                "installing.zip",
                "deployed.zip",
            };

            var isZip = knownZips.Contains(file);

            if (isZip)
                return true;

            return false;
        }

        private string NormalisePath(string file)
        {
            file = file.Replace("\\", "/");
            file = file.StartsWith("./") ? file.Substring(2) : file;
            return file;
        }
    }
}
