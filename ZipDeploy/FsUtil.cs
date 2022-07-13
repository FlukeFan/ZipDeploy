using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace ZipDeploy
{
    public class FsUtil
    {
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

        public void PrepareForDelete(string fullName)
        {
            if (!File.Exists(fullName))
                return;

            var fileName = Path.GetFileName(fullName);
            var path = Path.GetDirectoryName(fullName);
            var destinationFile = Path.Combine(path, $"zzz__{fileName}.fordelete.txt");

            DeleteFile(destinationFile);
            MoveFile(fullName, destinationFile);
        }

        public bool IsForDelete(string fullName)
        {
            return Path.GetFileName(fullName).StartsWith("zzz__") && fullName.EndsWith(".fordelete.txt");
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
