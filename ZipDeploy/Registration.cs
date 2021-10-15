using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ZipDeploy
{
    public static class Registration
    {
        public static IServiceCollection RegisterLogger(this IServiceCollection services, ILoggerFactory loggerFactory)
        {
            services.AddSingleton(loggerFactory);
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            return services;
        }

        public static IServiceCollection RegisterDefaults(this IServiceCollection services, ZipDeployOptions options)
        {
            services.AddSingleton(options);
            services.AddSingleton<ILockProcess, LockProcess>();
            services.AddSingleton<IDetectPackage, DetectPackage>();
            services.AddSingleton<ITriggerRestart, AspNetRestart>();
            services.AddSingleton<ICleaner, Cleaner>();
            services.AddSingleton<IUnzipper, Unzipper>();
            services.AddSingleton<IProcessWebConfig, ProcessWebConfig>();
            services.AddSingleton<ICanPauseTrigger, CanPauseTrigger>();
            return services;
        }
    }
}
