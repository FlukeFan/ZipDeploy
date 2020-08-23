#pragma warning disable CA1063 // Implement IDisposable Correctly
using System;
using System.IO.Compression;
using System.Threading;

namespace ZipDeploy
{
    public class ZipContext : IDisposable
    {
        private string _packageName;
        private Lazy<ZipArchive> _zipArchive;
        
        public ZipContext()
        {
            _zipArchive = new Lazy<ZipArchive>(OpenZip, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public ZipContext SetPackageName(string packageName)
        {
            _packageName = packageName;
            return this;
        }

        protected ZipArchive OpenZip()
        {
            var packageName = _packageName ?? ZipDeployOptions.DefaultNewPackageFileName;
            return ZipFile.OpenRead(packageName);
        }

        public void UsingArchive(Action<ZipArchive> action)
        {
            action(_zipArchive.Value);
        }

        public void Dispose()
        {
            if (_zipArchive.IsValueCreated)
                using (_zipArchive.Value) { }
        }
    }
}
#pragma warning restore CA1063 // Implement IDisposable Correctly
