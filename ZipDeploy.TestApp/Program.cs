using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using NLog.Web;

namespace ZipDeploy.TestApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //LogManager.ThrowExceptions = true;
            NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger().Info("Logging configured");
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
