using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace ZipDeploy
{
    public interface ITriggerRestart
    {
        void Trigger(ZipContext zipContext);
    }

    public class AspNetRestart : ITriggerRestart
    {
        private ILogger<AspNetRestart> _logger;

        public AspNetRestart(ILogger<AspNetRestart> logger)
        {
            _logger = logger;
        }

        public void Trigger(ZipContext zipContext)
        {
            if (File.Exists("web.config"))
            {
                _logger.LogDebug("Triggering restart by touching web.config");
                var config = File.ReadAllText("web.config");
                File.WriteAllText("web.config", config);
                File.SetLastWriteTimeUtc("web.config", File.GetLastWriteTimeUtc("web.config") + TimeSpan.FromSeconds(1));
            }
        }
    }
}
