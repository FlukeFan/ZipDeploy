using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace ZipDeploy
{
    public class Unzipper
    {
        private ILogger<Unzipper>   _log;
        private ZipDeployOptions    _options;

        public Unzipper(ZipDeployOptions options)
        {
            _log = ZipDeploy.LogFactory.CreateLogger<Unzipper>();
            _options = options;
        }

        public void UnzipBinaries()
        {
            var config = (string)null;
            var zippedFiles = new List<string>();

            UsingArchive("publish.zip", (entries, binariesWithoutExtension, fileHashes) =>
            {
                foreach (var entry in entries)
                {
                    var fullName = entry.Key;
                    zippedFiles.Add(NormalisePath(fullName));

                    if (!binariesWithoutExtension.Contains(PathWithoutExtension(fullName)))
                        continue;

                    Extract(fullName, entry.Value, fileHashes);
                }

                if (entries.ContainsKey("web.config"))
                {
                    using (var zipInput = entries["web.config"].Open())
                    using (var sr = new StreamReader(zipInput))
                        config = sr.ReadToEnd();
                }
            });

            RenameObsoleteBinaries(zippedFiles);

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

            UsingArchive("installing.zip", (entries, binariesWithoutExtension, fileHashes) =>
            {
                foreach (var entry in entries)
                {
                    var fullName = entry.Key;
                    zippedFiles.Add(NormalisePath(fullName));

                    if (binariesWithoutExtension.Contains(PathWithoutExtension(fullName)))
                        continue;

                    if (fullName == "web.config")
                        continue;

                    Extract(fullName, entry.Value, fileHashes);
                }
            });

            DeleteObsoleteFiles(zippedFiles);

            if (File.Exists("deployed.zip"))
                File.Delete("deployed.zip");

            File.Move("installing.zip", "deployed.zip");
        }

        private void Extract(string fullName, ZipArchiveEntry zipEntry, IDictionary<string, string> fileHashes)
        {
            string fileHash = "";

            using (var zipInput = zipEntry.Open())
            using (var md5 = MD5.Create())
            {
                var fileHashBytes = md5.ComputeHash(zipInput);
                fileHash = BitConverter.ToString(fileHashBytes);

                if (fileHashes.ContainsKey(fullName) && fileHash == fileHashes[fullName])
                {
                    _log.LogDebug($"no changes detected - skipping {fullName}");
                    return;
                }

                fileHashes[fullName] = fileHash;
            }

            var renamed = RenameFile(fullName);

            var folder = Path.GetDirectoryName(fullName);

            if (!string.IsNullOrWhiteSpace(folder))
                Directory.CreateDirectory(folder);

            using (var streamWriter = File.Create(fullName))
            using (var zipInput = zipEntry.Open())
            {
                _log.LogDebug($"extracting {fullName}");
                zipInput.CopyTo(streamWriter);
            }
        }

        private void UsingArchive(string zipFile, Action<IDictionary<string, ZipArchiveEntry>, IList<string>, IDictionary<string, string>> action)
        {
            _log.LogDebug($"Opening {zipFile}");

            using (var zipArchive = ZipFile.OpenRead(zipFile))
            {
                var entries = zipArchive.Entries
                    .Where(e => e.Length != 0)
                    .ToDictionary(zfe => zfe.FullName, zfe => zfe);

                var binaries = entries.Keys
                    .Where(k => IsBinary(k))
                    .ToList();

                _log.LogDebug($"{binaries.Count} binaries (dlls) in zip");

                var binariesWithoutExtension = binaries.Select(binary => PathWithoutExtension(binary)).ToList();

                var fileHashes = new Dictionary<string, string>();

                if (File.Exists("zipDeployFileHashes.txt"))
                {
                    fileHashes = File.ReadAllLines("zipDeployFileHashes.txt")
                        .Select(l => l.Split('|'))
                        .ToDictionary(a => a[0], a => a[1]);
                }

                action(entries, binariesWithoutExtension, fileHashes);

                var hashesStrings = fileHashes
                    .Select(kvp => $"{kvp.Key}|{kvp.Value}");

                File.WriteAllLines("zipDeployFileHashes.txt", hashesStrings);
            }
        }

        private bool IsBinary(string file)
        {
            var extension = Path.GetExtension(file)?.ToLower();
            return new string[] { ".dll", ".exe" }.Contains(extension);
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

        private void RenameObsoleteBinaries(IList<string> zippedFiles)
        {
            foreach (var fullName in Directory.GetFiles(".", "*", SearchOption.AllDirectories).Select(f => NormalisePath(f)))
                if (IsBinary(fullName) && !zippedFiles.Contains(fullName) && !ShouldIgnore(fullName))
                    RenameFile(fullName);
        }

        private void DeleteObsoleteFiles(IList<string> zippedFiles)
        {
            foreach (var forDelete in Directory.GetFiles(".", "*", SearchOption.AllDirectories).Select(f => NormalisePath(f)))
            {
                if (zippedFiles.Contains(forDelete) || ShouldIgnore(forDelete))
                    continue;

                DeleteFile(forDelete);
            }

            _log.LogDebug("Completed deletion of obsolete files");
        }

        private string RenameFile(string file)
        {
            if (!File.Exists(file))
                return null;

            var fileName = Path.GetFileName(file);
            var path = Path.GetDirectoryName(file);
            var destinationFile = Path.Combine(path, $"zzz__{fileName}.fordelete.txt");

            DeleteFile(destinationFile);

            _log.LogDebug($"renaming {file} to {destinationFile}");
            File.Move(file, destinationFile);

            return destinationFile;
        }

        private void DeleteFile(string file)
        {
            var count = 3;

            while (File.Exists(file))
            {
                try
                {
                    _log.LogDebug($"deleting existing {file}");
                    File.Delete(file);
                }
                catch (Exception e)
                {
                    _log.LogDebug(e, $"Error deleting {file}");
                    Thread.Sleep(0);

                    if (count-- <= 0)
                        throw;
                }
            }
        }

        private bool ShouldIgnore(string forDeleteFile)
        {
            var file = Path.GetFileName(forDeleteFile);

            var knownFiles = new List<string>
            {
                "publish.zip",
                "installing.zip",
                "deployed.zip",
                "zipDeployFileHashes.txt",
            };

            var isKnownfile = knownFiles.Contains(file);

            if (isKnownfile)
                return true;

            if (_options.PathsToIgnore.Any(p => NormalisePath(forDeleteFile).ToLower().StartsWith(NormalisePath(p.ToLower()))))
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
