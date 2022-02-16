using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ZipDeploy
{
    public interface IUnzipper
    {
        Task UnzipAsync();
    }

    public class Unzipper : IUnzipper
    {
        private ILogger<Unzipper>   _logger;
        private ZipDeployOptions    _options;
        private FsUtil _fsUtil;

        public Unzipper(ILogger<Unzipper> logger, ZipDeployOptions options)
        {
            _logger = logger;
            _options = options;
            _fsUtil = new FsUtil(logger);
        }

        public virtual async Task UnzipAsync()
        {
            _logger.LogInformation("Unzipping deployment package");
            var zippedFiles = new List<string>();

            await UsingArchiveAsync((entries, fileHashes) =>
            {
                foreach (var entry in entries)
                {
                    var fullName = entry.Key;
                    zippedFiles.Add(_fsUtil.NormalisePath(fullName));
                    Extract(fullName, entry.Value, fileHashes);
                }
            });

            RenameObsoleteFiles(zippedFiles);

            _fsUtil.PrepareForDelete(_options.DeployedPackageFileName);
            _fsUtil.MoveFile(_options.NewPackageFileName, _options.DeployedPackageFileName);
            _logger.LogInformation("Completed unzipping of deployment package");
        }

        protected virtual void Extract(string fullName, ZipArchiveEntry zipEntry, IDictionary<string, string> fileHashes)
        {
            using (var zipInput = zipEntry.Open())
            using (var md5 = MD5.Create())
            {
                var fileHashBytes = md5.ComputeHash(zipInput);
                var fileHash = BitConverter.ToString(fileHashBytes);

                if (fileHashes.ContainsKey(fullName) && fileHash == fileHashes[fullName])
                {
                    _logger.LogDebug($"no changes detected - skipping {fullName}");
                    return;
                }

                fileHashes[fullName] = fileHash;
            }

            _fsUtil.PrepareForDelete(fullName);

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

        protected virtual async Task UsingArchiveAsync(Action<IDictionary<string, ZipArchiveEntry>, IDictionary<string, string>> action)
        {
            await _options.UsingArchiveAsync(_logger, zipArchive =>
            {
                var entries = zipArchive.Entries
                    .Where(e => e.Length != 0)
                    .ToDictionary(zfe => zfe.FullName, zfe => zfe);

                _logger.LogDebug($"{entries.Count} files in zip");

                var fileHashes = new Dictionary<string, string>();

                if (File.Exists(_options.HashesFileName))
                {
                    fileHashes = File.ReadAllLines(_options.HashesFileName)
                        .Select(l => l.Split('|'))
                        .ToDictionary(a => a[0], a => a[1]);
                }

                action(entries, fileHashes);

                var hashesStrings = fileHashes
                    .Select(kvp => $"{kvp.Key}|{kvp.Value}");

                File.WriteAllLines(_options.HashesFileName, hashesStrings);
                return Task.CompletedTask;
            });
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

        protected virtual void RenameObsoleteFiles(IList<string> zippedFiles)
        {
            foreach (var fullName in Directory.GetFiles(".", "*", SearchOption.AllDirectories).Select(f => _fsUtil.NormalisePath(f)))
                if (!zippedFiles.Contains(fullName) && !ShouldIgnore(fullName))
                    _fsUtil.PrepareForDelete(fullName);
        }

        protected virtual bool ShouldIgnore(string forDeleteFile)
        {
            var file = Path.GetFileName(forDeleteFile);

            var knownFiles = new List<string>
            {
                _options.NewPackageFileName,
                _options.DeployedPackageFileName,
                _options.HashesFileName,
            };

            var isKnownfile = knownFiles.Contains(file);

            if (isKnownfile)
                return true;

            if (_fsUtil.IsForDelete(forDeleteFile))
                return true;

            if (_options.PathsToIgnore.Any(p => _fsUtil.NormalisePath(forDeleteFile).ToLower().StartsWith(_fsUtil.NormalisePath(p.ToLower()))))
                return true;

            return false;
        }
    }
}
