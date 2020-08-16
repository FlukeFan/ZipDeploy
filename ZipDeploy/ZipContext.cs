#pragma warning disable CA1063 // Implement IDisposable Correctly
using System;
using System.IO.Compression;

namespace ZipDeploy
{
    public class ZipContext : IDisposable
    {
        private string _packageName;

        public ZipContext SetPackageName(string packageName)
        {
            _packageName = packageName;
            return this;
        }

        public void UsingArchive(Action<ZipArchive> action)
        {
            var packageName = _packageName ?? ZipDeployOptions.DefaultNewPackageFileName;
            using (var zipArchive = ZipFile.OpenRead(packageName))
                action(zipArchive);
        }

        public void Dispose()
        {
        }
    }
}
#pragma warning restore CA1063 // Implement IDisposable Correctly
