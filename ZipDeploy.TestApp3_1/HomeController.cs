using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace ZipDeploy.TestApp3_1
{
    public class HomeController : Controller
    {
        private const int c_version = 123;

        public IActionResult Index()
        {
            return Content($"<script src='test.js'></script>Version={c_version}", "text/html");
        }

        public IActionResult Runtime()
        {
            var runtimeVersion = GetNetCoreVersion();
            runtimeVersion = string.Join(".", runtimeVersion.Split('.').Take(2));
            return Content(runtimeVersion);
        }

        // https://weblog.west-wind.com/posts/2018/Apr/12/Getting-the-NET-Core-Runtime-Version-in-a-Running-Application
        public static string GetNetCoreVersion()
        {
            var fullVersion = Assembly
                .GetEntryAssembly()?
                .GetCustomAttribute<TargetFrameworkAttribute>()?
                .FrameworkName;

            var versionNumber = fullVersion.Split('=')[1];
            return versionNumber.TrimStart('v');
        }
    }
}
