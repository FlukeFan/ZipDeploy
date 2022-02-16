﻿using System;
using System.IO;
using System.Threading.Tasks;
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
            RunAsync(loggerFactory, setupOptions, program).GetAwaiter().GetResult();
        }

        public static async Task RunAsync(ILoggerFactory loggerFactory, Action<ZipDeployOptions> setupOptions, Action program)
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

            using (provider)
            {
                try
                {
                    var lockProcess = provider.GetRequiredService<ILockProcess>();
                    await lockProcess.LockAsync();

                    var detectPackage = provider.GetRequiredService<IDetectPackage>();
                    var triggerRestart = provider.GetRequiredService<ITriggerRestart>();
                    detectPackage.PackageDetectedAsync += triggerRestart.TriggerAsync;

                    var cleaner = provider.GetRequiredService<ICleaner>();
                    cleaner.DeleteObsoleteFiles();
                    await detectPackage.StartedAsync();
                    program();
                }
                finally
                {
                    await logger.RetryAsync(options, "ZipDeploy before shutdown", async () =>
                    {
                        if (File.Exists(Path.Combine(Environment.CurrentDirectory, options.NewPackageFileName)))
                        {
                            logger.LogInformation("Found package {packageName}", options.NewPackageFileName);
                            var unzipper = provider.GetRequiredService<IUnzipper>();
                            await unzipper.UnzipAsync();
                        }

                        logger.LogDebug("ZipDeploy completed after process shutdown");
                    });
                }
            }
        }

        public static IServiceCollection AddZipDeploy(this IServiceCollection services, Action<ZipDeployOptions> setupOptions = null)
        {
            var options = new ZipDeployOptions();
            setupOptions?.Invoke(options);
            services.RegisterDefaults(options);
            services.AddHostedService<Application>();
            return services;
        }
    }
}
