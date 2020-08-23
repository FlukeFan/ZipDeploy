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
    public interface IUnzipper
    {
        void Unzip();
        void DeleteObsoleteFiles();
    }

    public class Unzipper : IUnzipper
    {
        private ILogger<Unzipper>   _logger;
        private ZipDeployOptions    _options;

        public Unzipper(ILogger<Unzipper> logger, ZipDeployOptions options)
        {
            _logger = logger;
            _options = options;
        }

        public void Unzip()
        {
            UnzipBinaries();
            SyncNonBinaries();
        }

        public void DeleteObsoleteFiles()
        {
            foreach (var fullName in Directory.GetFiles(".", "*", SearchOption.AllDirectories).Select(f => NormalisePath(f)))
                if (Path.GetFileName(fullName).StartsWith("zzz_") && fullName.EndsWith(".fordelete.txt"))
                    DeleteFile(fullName);
        }

        public void UnzipBinaries()
        {
            var zippedFiles = new List<string>();

            UsingArchive(_options.NewPackageFileName, (entries, binariesWithoutExtension, fileHashes) =>
            {
                foreach (var entry in entries)
                {
                    var fullName = entry.Key;
                    zippedFiles.Add(NormalisePath(fullName));

                    if (!binariesWithoutExtension.Contains(PathWithoutExtension(fullName)))
                        continue;

                    Extract(fullName, entry.Value, fileHashes);
                }
            });

            RenameObsoleteBinaries(zippedFiles);

            if (File.Exists(_options.LegacyTempFileName))
                DeleteFile(_options.LegacyTempFileName);

            MoveFile(_options.NewPackageFileName, _options.LegacyTempFileName);
        }

        public void SyncNonBinaries()
        {
            var zippedFiles = new List<string>();

            UsingArchive(_options.LegacyTempFileName, (entries, binariesWithoutExtension, fileHashes) =>
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

            if (File.Exists(_options.DeployedPackageFileName))
                DeleteFile(_options.DeployedPackageFileName);

            MoveFile(_options.LegacyTempFileName, _options.DeployedPackageFileName);
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
                    _logger.LogDebug($"no changes detected - skipping {fullName}");
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
                _logger.LogDebug($"extracting {fullName}");
                zipInput.CopyTo(streamWriter);
            }
        }

        private void UsingArchive(string zipFile, Action<IDictionary<string, ZipArchiveEntry>, IList<string>, IDictionary<string, string>> action)
        {
            _logger.LogDebug($"Opening {zipFile}");

            using (var zipArchive = ZipFile.OpenRead(zipFile))
            {
                var entries = zipArchive.Entries
                    .Where(e => e.Length != 0)
                    .ToDictionary(zfe => zfe.FullName, zfe => zfe);

                var binaries = entries.Keys
                    .Where(k => _options.IsBinary(k))
                    .ToList();

                _logger.LogDebug($"{binaries.Count} binaries (dlls) in zip");

                var binariesWithoutExtension = binaries.Select(binary => PathWithoutExtension(binary)).ToList();

                var fileHashes = new Dictionary<string, string>();

                if (File.Exists(_options.HashesFileName))
                {
                    fileHashes = File.ReadAllLines(_options.HashesFileName)
                        .Select(l => l.Split('|'))
                        .ToDictionary(a => a[0], a => a[1]);
                }

                action(entries, binariesWithoutExtension, fileHashes);

                var hashesStrings = fileHashes
                    .Select(kvp => $"{kvp.Key}|{kvp.Value}");

                File.WriteAllLines(_options.HashesFileName, hashesStrings);
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

        private void RenameObsoleteBinaries(IList<string> zippedFiles)
        {
            foreach (var fullName in Directory.GetFiles(".", "*", SearchOption.AllDirectories).Select(f => NormalisePath(f)))
                if (_options.IsBinary(fullName) && !zippedFiles.Contains(fullName) && !ShouldIgnore(fullName))
                    RenameFile(fullName);
        }

        private string RenameFile(string file)
        {
            if (!File.Exists(file))
                return null;

            var fileName = Path.GetFileName(file);
            var path = Path.GetDirectoryName(file);
            var destinationFile = Path.Combine(path, $"zzz__{fileName}.fordelete.txt");

            DeleteFile(destinationFile);

            MoveFile(file, destinationFile);

            return destinationFile;
        }

        private void DeleteFile(string file)
        {
            Try(() => File.Delete(file),
                () => File.Exists(file),
                $"delete file {file}");
        }

        private void MoveFile(string file, string destinationFile)
        {
            Try(() => File.Move(file, destinationFile),
                () => File.Exists(file),
                $"moving file {file} to {destinationFile}");
        }

        private void Try(Action action, Func<bool> notComplete, string what)
        {
            var count = 3;

            while (notComplete())
            {
                try
                {
                    _logger.LogDebug(what);
                    action();
                }
                catch (Exception e)
                {
                    _logger.LogDebug(e, $"Error during {what}");
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
                _options.NewPackageFileName,
                _options.LegacyTempFileName,
                _options.DeployedPackageFileName,
                _options.HashesFileName,
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
