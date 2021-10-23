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
        private ITriggerRestart _triggerRestart;

        public LockProcess(ILogger<LockProcess> logger, ZipDeployOptions options, ITriggerRestart triggerRestart)
        {
            _logger = logger;
            _options = options;
            _triggerRestart = triggerRestart;
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
                var locked = false;

                if (_options.ProcessLockTimeout.HasValue)
                {
                    _logger.LogDebug($"Waiting on lock {semaphoreName} for {_options.ProcessLockTimeout}");
                    locked = semaphore.WaitOne(_options.ProcessLockTimeout.Value);
                }
                else
                {
                    _logger.LogDebug($"Waiting on lock {semaphoreName}");
                    locked = semaphore.WaitOne(Timeout.Infinite);
                }

                if (!locked)
                    throw new Exception($"Failed to obtain lock");
            }
            catch(Exception ex)
            {
                TryTriggerRestart();
                throw new Exception($"Could not obtain lock {semaphoreName}", ex);
            }

            _logger.LogInformation($"Obtained lock {semaphoreName}");
        }

        private void TryTriggerRestart()
        {
            try
            {
                _logger.LogInformation($"Attempting to trigger restart after failing to obtain lock");
                _triggerRestart.Trigger();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error attepting restart: {ex.Message}");
            }
        }
    }
}
