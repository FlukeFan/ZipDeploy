using System;
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

            LoggerFactory = LoggerFactory ?? new LoggerFactory();
            _logger = LoggerFactory.CreateLogger<ZipDeploy>();
            _logger.LogDebug("ZipDeploy starting");

            try
            {
                setupOptions?.Invoke(options);

                var detectPackage = options.DetectPackage = options.DetectPackage ?? options.NewDetectPackage();
                var triggerRestart = options.TriggerRestart ?? options.NewTriggerRestart();
                var queryPackageName = options.QueryPackageName = options.QueryPackageName ?? options.NewQueryPackageName();

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
                        var packageName = options.QueryPackageName.FindPackageName();

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
