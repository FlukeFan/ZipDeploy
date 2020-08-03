#pragma warning disable CA1063 // Implement IDisposable Correctly
using System;

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

        public void Dispose()
        {
        }
    }
}
#pragma warning restore CA1063 // Implement IDisposable Correctly
