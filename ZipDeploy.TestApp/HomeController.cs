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
    }
}
