using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace ZipDeploy
{
    public interface ILockProcess
    {
        void Lock();
    }

    public class LockProcess : ILockProcess
    {
        private ILogger _logger;
        private ZipDeployOptions _options;

        public LockProcess(ILogger<LockProcess> logger, ZipDeployOptions options)
        {
            _logger = logger;
            _options = options;
        }

        public virtual void Lock()
        {
            if (string.IsNullOrWhiteSpace(_options.ProcessLockName))
            {
                _logger.LogInformation("No global lock configured");
                return;
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _logger.LogInformation("Global lock only supported on Windows");
                return;
            }

            var semaphoreName = $"Global\\{_options.ProcessLockName}";

#pragma warning disable PC001 // API not supported on all platforms
            var semaphore = new Semaphore(1, 1, semaphoreName, out _);
#pragma warning restore PC001 // API not supported on all platforms

            try
            {
                if (_options.ProcessLockTimeout.HasValue)
                {
                    _logger.LogDebug($"Waiting on lock {semaphoreName} for {_options.ProcessLockTimeout}");
                    semaphore.WaitOne(_options.ProcessLockTimeout.Value);
                }
                else
                {
                    _logger.LogDebug($"Waiting on lock {semaphoreName}");
                    semaphore.WaitOne(Timeout.Infinite);
                }
            }
            catch(Exception ex)
            {
                throw new Exception($"Could not obtain lock {semaphoreName}", ex);
            }

            _logger.LogInformation($"Obtained lock {semaphoreName}");
        }
    }
}
