using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ZipDeploy
{
    public class ZipDeploy
    {
        public static void Run(Action program)
        {
            Run(null, null, program);
        }

        public static void Run(Action<ZipDeployOptions> setupOptions, Action program)
        {
            Run(null, setupOptions, program);
        }

        public static void Run(ILoggerFactory loggerFactory, Action<ZipDeployOptions> setupOptions, Action program)
        {
            loggerFactory = loggerFactory ?? new LoggerFactory();
            var logger = loggerFactory.CreateLogger<ZipDeploy>();
            logger.LogDebug("ZipDeploy starting");

            var options = new ZipDeployOptions(loggerFactory);
            var context = new ZipContext();

            IQueryPackageName queryPackageName = null;

            setupOptions?.Invoke(options);
            var provider = options.ServiceCollection.BuildServiceProvider();

            try
            {
                var detectPackage = provider.GetRequiredService<IDetectPackage>();
                var triggerRestart = provider.GetRequiredService<ITriggerRestart>();
                queryPackageName = provider.GetRequiredService<IQueryPackageName>();

                detectPackage.PackageDetected += () =>
                {
                    context.SetPackageName(queryPackageName.FindPackageName());
                    triggerRestart.Trigger(context);
                };

                var unzipper = provider.GetRequiredService<IUnzipper>();
                unzipper.DeleteObsoleteFiles();

                if (!File.Exists(Path.Combine(Environment.CurrentDirectory, options.NewPackageFileName)))
                {
                    logger.LogInformation($"Package {options.NewPackageFileName} not found - running program");
                    program();
                }
                else
                {
                    logger.LogInformation($"Package {options.NewPackageFileName} found - skipping program");
                }
            }
            finally
            {
                logger.Try("ZipDeploy before shutdown", () =>
                {
                    var packageName = queryPackageName?.FindPackageName();

                    if (packageName != null)
                    {
                        logger.LogDebug("Found package {packageName}", packageName);
                        var unzipper = provider.GetRequiredService<IUnzipper>();
                        unzipper.Unzip();
                    }

                    logger.LogDebug("ZipDeploy completed after process shutdown");
                });
            }
        }
    }
}
