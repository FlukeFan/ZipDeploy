using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace ZipDeploy
{
    public class FsUtil
    {
        public const string ForDeletePrefix = "zzz__";
        public const string ForDeletePostfix = ".fordelete.txt";

        private ILogger _logger;

        public FsUtil(ILogger logger)
        {
            _logger = logger;
        }

        public string NormalisePath(string file)
        {
            file = file.Replace("\\", "/");
            file = file.StartsWith("./") ? file.Substring(2) : file;
            return file;
        }

        public void DeleteFile(string file)
        {
            Try(() => File.Delete(file),
                () => File.Exists(file),
                $"delete file {file}");
        }

        public void MoveFile(string file, string destinationFile)
        {
            Try(() => File.Move(file, destinationFile),
                () => File.Exists(file),
                $"moving file {file} to {destinationFile}");
        }

        public void DeleteDirectory(string directory)
        {
            Try(() => Directory.Delete(directory, true),
                () => Directory.Exists(directory),
                $"delete directory {directory}");
        }

        public void MoveDirectory(string directory, string destinationDirectory)
        {
            Try(() => Directory.Move(directory, destinationDirectory),
                () => Directory.Exists(directory),
                $"moving directory {directory} to {destinationDirectory}");
        }

        public void PrepareForDelete(string fullName)
        {
            var fileExists = File.Exists(fullName);
            var directoryExists = !fileExists && Directory.Exists(fullName);

            if (!fileExists && !directoryExists)
                return;

            var fileName = Path.GetFileName(fullName);
            var path = Path.GetDirectoryName(fullName);
            var destinationFullName = Path.Combine(path, $"{ForDeletePrefix}{fileName}{ForDeletePostfix}");

            PrepareForDelete(destinationFullName);

            if (fileExists)
                MoveFile(fullName, destinationFullName);
            else
                MoveDirectory(fullName, destinationFullName);
        }

        public bool IsForDelete(string fullName)
        {
            return Path.GetFileName(fullName).StartsWith(ForDeletePrefix) && fullName.EndsWith(ForDeletePostfix);
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
    }
}
