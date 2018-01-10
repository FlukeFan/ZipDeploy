using System.Linq;
using Microsoft.Web.Administration;

namespace ZipDeploy.Tests.TestApp
{
    public static class Iis
    {
        private const string    c_iisName = "ZipDeployTestApp";
        private const int       c_iisPort = 8099;

        public static void CreateIisSite(string iisFolder)
        {
            using (var iisManager = new ServerManager())
            {
                if (iisManager.Sites.Count(s => s.Name == c_iisName) == 0)
                {
                    iisManager.Sites.Add("ZipDeployTestApp", iisFolder, c_iisPort);
                    iisManager.CommitChanges();
                }

                Test.WriteProgress($"Created IIS site {c_iisName}:{c_iisPort} in {iisFolder}");
            }
        }

        public static void DeleteIisSite()
        {
            using (var iisManager = new ServerManager())
            {
                var site = iisManager.Sites.SingleOrDefault(s => s.Name == c_iisName);
                iisManager.Sites.Remove(site);
                iisManager.CommitChanges();
                Test.WriteProgress($"Removed IIS set {c_iisName}");
            }
        }
    }
}
