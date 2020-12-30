using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using NLog;
using NLog.Common;
using NLog.Config;
using NLog.Targets;
using NLog.Web;

namespace ZipDeploy.TestApp2_1
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            InternalLogger.LogFile = "logs\\nlog.internal.log";
            InternalLogger.LogLevel = LogLevel.Info;

            LogManager.ThrowExceptions = true;
            var config = new LoggingConfiguration();
            var logFile = new FileTarget("fileTarget") { FileName = "logs\\nlog.log" };
            config.AddTarget(logFile);
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, logFile);

            LogManager.ThrowExceptions = true;
            LogManager.Configuration = config;

            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseNLog()
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();
    }
}
