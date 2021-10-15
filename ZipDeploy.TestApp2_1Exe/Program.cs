using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Common;
using NLog.Config;
using NLog.Targets;
using NLog.Web;

namespace ZipDeploy.TestApp2_1Exe
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            InternalLogger.LogFile = "logs\\nlog.internal.log";
            InternalLogger.LogLevel = NLog.LogLevel.Info;

            LogManager.ThrowExceptions = true;
            var config = new LoggingConfiguration();
            var layout = "${longdate}|${level:uppercase=true}|${processid}|${logger}|${message} ${exception:format=tostring}";
            var logFile = new FileTarget("fileTarget") { FileName = "logs\\nlog.log", Layout = layout };
            config.AddTarget(logFile);
            config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, logFile);

            LogManager.ThrowExceptions = true;

            var loggerFactory = LoggerFactory.Create(c => c
                .SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace)
                .AddNLog(config));

            ZipDeploy.Run(
                loggerFactory,
                options => options.IgnorePathStarting("logs").UsingProcessLock(typeof(Program).FullName),
                () => BuildWebHost(args).Run());
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseNLog()
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();
    }
}
