using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ZipDeploy
{
    public class ZipDeploy
    {
        public static ILoggerFactory LoggerFactory = new LoggerFactory();

        private static ILogger<ZipDeploy> _logger;

        public static void Run(Action<ZipDeployOptions> setupOptions, Action program)
        {
            var options = new ZipDeployOptions();
            var context = new ZipContext();

            IQueryPackageName queryPackageName = null;

            LoggerFactory = LoggerFactory ?? new LoggerFactory();
            _logger = LoggerFactory.CreateLogger<ZipDeploy>();
            _logger.LogDebug("ZipDeploy starting");

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
                            var unzipper = new Unzipper(options);
                            unzipper.UnzipBinaries();
                            unzipper.SyncNonBinaries(deleteObsolete: false);
                        }

                        _logger.LogDebug("ZipDeploy completed after process shutdown");
                    });
            }
        }

        public static ZipDeployOptions DefaultOptions()
        {
            return new ZipDeployOptions();
        }
    }
}
