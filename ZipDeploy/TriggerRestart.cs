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
        private ZipDeployOptions _options;

        public AspNetRestart(ILogger<AspNetRestart> logger, ZipDeployOptions options)
        {
            _logger = logger;
            _options = options;
        }

        public void Trigger()
        {
            _options.UsingArchive(_logger, zipArchive =>
            {
                var webConfigContent = (string)null;
                var webConfigEntry = zipArchive.GetEntry("web.config");

                if (webConfigEntry != null && webConfigEntry.Length != 0)
                {
                    _logger.LogDebug("Found web.config content in package");

                    using (var zipFileContext = webConfigEntry.Open())
                    using (var sr = new StreamReader(zipFileContext))
                        webConfigContent = sr.ReadToEnd();
                }

                if (string.IsNullOrWhiteSpace(webConfigContent) && File.Exists("web.config"))
                {
                    _logger.LogDebug("Using existing web.config content");
                    webConfigContent = File.ReadAllText("web.config");
                }

                if (string.IsNullOrWhiteSpace(webConfigContent))
                {
                    _logger.LogDebug("Unable to find content for web.config to trigger restart");
                    return;
                }

                _logger.LogDebug("Triggering restart by touching web.config");
                webConfigContent = _options.ProcessWebConfig(webConfigContent);
                File.WriteAllText("web.config", webConfigContent);
                File.SetLastWriteTimeUtc("web.config", File.GetLastWriteTimeUtc("web.config") + TimeSpan.FromSeconds(1));
                _options.PathsToIgnore.Add("web.config");
            });
        }
    }
}
