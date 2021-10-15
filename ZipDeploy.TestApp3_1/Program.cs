using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using NLog;
using NLog.Common;
using NLog.Config;
using NLog.Targets;
using NLog.Web;

namespace ZipDeploy.TestApp3_1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            InternalLogger.LogFile = "logs\\nlog.internal.log";
            InternalLogger.LogLevel = LogLevel.Info;

            LogManager.ThrowExceptions = true;
            var config = new LoggingConfiguration();
            var layout = "${longdate}|${level:uppercase=true}|${processid}|${logger}|${message} ${exception:format=tostring}";
            var logFile = new FileTarget("fileTarget") { FileName = "logs\\nlog.log", ConcurrentWrites = true, Layout = layout };
            config.AddTarget(logFile);
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, logFile);

            LogManager.ThrowExceptions = true;
            LogManager.Configuration = config;

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseNLog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
