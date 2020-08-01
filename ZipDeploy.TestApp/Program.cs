using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using NLog;
using NLog.Extensions.Logging;
using NLog.Web;

namespace ZipDeploy.TestApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            LogManager.ThrowExceptions = true;
            NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger().Info("Logging configured");

            ZipDeploy.LoggerFactory.AddNLog();

            ZipDeploy.Run(
                _ => { },
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
