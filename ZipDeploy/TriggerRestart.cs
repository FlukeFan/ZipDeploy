using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace ZipDeploy
{
    public interface ITriggerRestart
    {
        void Trigger();
    }

    public class AspNetRestart : ITriggerRestart
    {
        private ILogger<AspNetRestart> _logger;

        public AspNetRestart()
        {
            _logger = ZipDeploy.LoggerFactory.CreateLogger<AspNetRestart>();
        }

        public void Trigger()
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
