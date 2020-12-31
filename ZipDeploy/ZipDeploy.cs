using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ZipDeploy
{
    public static class ZipDeploy
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
            var logger = loggerFactory.CreateLogger(typeof(ZipDeploy));
            logger.LogDebug("ZipDeploy starting");

            var options = new ZipDeployOptions();
            setupOptions?.Invoke(options);

            var provider = new ServiceCollection()
                .RegisterLogger(loggerFactory)
                .RegisterDefaults(options)
                .BuildServiceProvider();

            try
            {
                var detectPackage = provider.GetRequiredService<IDetectPackage>();
                var triggerRestart = provider.GetRequiredService<ITriggerRestart>();

                detectPackage.PackageDetected += () =>
                    triggerRestart.Trigger();

                var cleaner = provider.GetRequiredService<ICleaner>();
                cleaner.DeleteObsoleteFiles();
                program();
            }
            finally
            {
                logger.Try("ZipDeploy before shutdown", () =>
                {
                    if (File.Exists(Path.Combine(Environment.CurrentDirectory, options.NewPackageFileName)))
                    {
                        logger.LogDebug("Found package {packageName}", options.NewPackageFileName);
                        var unzipper = provider.GetRequiredService<IUnzipper>();
                        unzipper.Unzip();
                    }

                    logger.LogDebug("ZipDeploy completed after process shutdown");
                });
            }
        }

        public static IServiceCollection AddZipDeploy(this IServiceCollection services, Action<ZipDeployOptions> setupOptions = null)
        {
            var options = new ZipDeployOptions();
            setupOptions?.Invoke(options);
            services.RegisterDefaults(options);
            services.AddHostedService<ApplicationLifetime>();
            return services;
        }
    }
}
