using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace ZipDeploy.TestApp
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

        // https://stackoverflow.com/a/49309382/357728
        public static string GetNetCoreVersion()
        {
            var assembly = typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly;
            var assemblyPath = assembly.CodeBase.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            int netCoreAppIndex = Array.IndexOf(assemblyPath, "Microsoft.NETCore.App");
            if (netCoreAppIndex > 0 && netCoreAppIndex < assemblyPath.Length - 2)
                return assemblyPath[netCoreAppIndex + 1];
            return null;
        }
    }
}
