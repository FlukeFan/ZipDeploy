﻿using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ZipDeploy
{
    public class ZipDeploy
    {
        private static ILogger<ZipDeploy> _logger;

        public static void Run(Action<ZipDeployOptions> setupOptions, Action program)
        {
            Run(null, setupOptions, program);
        }

        public static void Run(ILoggerFactory loggerFactory, Action<ZipDeployOptions> setupOptions, Action program)
        {
            loggerFactory = loggerFactory ?? new LoggerFactory();
            _logger = loggerFactory.CreateLogger<ZipDeploy>();
            _logger.LogDebug("ZipDeploy starting");

            var options = new ZipDeployOptions(loggerFactory);
            var context = new ZipContext();

            IQueryPackageName queryPackageName = null;

            try
            {
                setupOptions?.Invoke(options);

                var provider = options.ServiceCollection.BuildServiceProvider();

                var detectPackage = provider.GetRequiredService<IDetectPackage>();
                var triggerRestart = provider.GetRequiredService<ITriggerRestart>();
                queryPackageName = provider.GetRequiredService<IQueryPackageName>();

                detectPackage.PackageDetected += () =>
                {
                    context.SetPackageName(queryPackageName.FindPackageName());
                    triggerRestart.Trigger(context);
                };

                // DeleteForDeleteFiles();
                
                // if (!filesToUnzip)
                    program();
            }
            finally
            {
                using (context)
                    _logger.Try("ZipDeploy before shutdown", () =>
                    {
                        var packageName = queryPackageName?.FindPackageName();

                        if (packageName != null)
                        {
                            _logger.LogDebug("Found package {packageName}", packageName);
                            var unzipper = new Unzipper(loggerFactory.CreateLogger<Unzipper>(), options);
                            unzipper.UnzipBinaries();
                            unzipper.SyncNonBinaries(deleteObsolete: false);
                        }

                        _logger.LogDebug("ZipDeploy completed after process shutdown");
                    });
            }
        }
    }
}
