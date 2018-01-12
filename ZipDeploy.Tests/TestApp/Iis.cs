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
                var siteCount = iisManager.Sites.Count(s => s.Name == c_iisName);

                if (siteCount > 0)
                    DeleteIisSite(iisManager);

                iisManager.Sites.Add("ZipDeployTestApp", iisFolder, c_iisPort);
                iisManager.CommitChanges();

                Test.WriteProgress($"Created IIS site {c_iisName}:{c_iisPort} in {iisFolder}");
            }
        }

        public static void DeleteIisSite()
        {
            using (var iisManager = new ServerManager())
            {
                DeleteIisSite(iisManager);
            }
        }

        private static void DeleteIisSite(ServerManager iisManager)
        {
            var site = iisManager.Sites.SingleOrDefault(s => s.Name == c_iisName);

            if (site == null)
                return;

            iisManager.Sites.Remove(site);
            iisManager.CommitChanges();
            Test.WriteProgress($"Removed IIS site {c_iisName}");
        }
    }
}
