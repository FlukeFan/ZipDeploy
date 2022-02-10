using System;
using System.Diagnostics;
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
        private Semaphore _semaphore;
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

            var logMessage = _options.ProcessLockTimeout.HasValue
                ? $"Waiting on lock {semaphoreName} for {_options.ProcessLockTimeout}"
                : $"Waiting on lock {semaphoreName}";

            _logger.LogDebug(logMessage);

            try
            {
                var locked = false;
                var stopwatch = Stopwatch.StartNew();

                while (!locked)
                {
#pragma warning disable PC001 // API not supported on all platforms
                    _semaphore = new Semaphore(1, 1, semaphoreName, out locked);
#pragma warning restore PC001 // API not supported on all platforms

                    if (!locked)
                    {
                        // if we couldn't create a new (global) named semaphore
                        // then another process has it, so we should dispose of this one and wait

                        using (_semaphore) { }
                        Thread.Sleep(200);

                        if (_options.ProcessLockTimeout.HasValue && stopwatch.Elapsed > _options.ProcessLockTimeout.Value)
                            throw new Exception($"Could not create new Semaphore {semaphoreName}");
                    }
                }
            }
            catch(Exception ex)
            {
                TryTriggerRestart();
                throw new Exception($"Could not obtain lock {semaphoreName}", ex);
            }

            // note, we deliberately don't dispose of the sempahore we create
            // so that Windows will clear it up once the process dies (and so dll files should be unlocked by this point)
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
