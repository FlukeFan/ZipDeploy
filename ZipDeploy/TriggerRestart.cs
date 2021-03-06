﻿using System;
using System.IO;
using System.Threading;
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
        private IProcessWebConfig _processWebConfig;
        private ICanPauseTrigger _canPauseTrigger;
        private ZipDeployOptions _options;

        public AspNetRestart(
            ILogger<AspNetRestart> logger,
            IProcessWebConfig processWebConfig,
            ICanPauseTrigger canPauseTrigger,
            ZipDeployOptions options)
        {
            _logger = logger;
            _processWebConfig = processWebConfig;
            _canPauseTrigger = canPauseTrigger;
            _options = options;
        }

        public virtual void Trigger()
        {
            _logger.LogInformation("Awaiting trigger of restart");

            using (var semaphore = new SemaphoreSlim(0, 1))
            {
                _canPauseTrigger.Release(semaphore);
                semaphore.Wait();
            }

            _logger.LogInformation("Triggering restart");

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
                webConfigContent = _processWebConfig.Process(webConfigContent);
                File.WriteAllText("web.config", webConfigContent);
                File.SetLastWriteTimeUtc("web.config", File.GetLastWriteTimeUtc("web.config") + TimeSpan.FromSeconds(1));
                _options.PathsToIgnore.Add("web.config");
            });
        }
    }
}
