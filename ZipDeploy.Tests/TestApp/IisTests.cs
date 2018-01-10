using System.IO;
using System.Linq;
using Microsoft.Web.Administration;
using NUnit.Framework;

namespace ZipDeploy.Tests.TestApp
{
    [TestFixture]
    public class IisTests
    {
        [Test]
        public void DeployZip()
        {
            var iisFolder = Path.Combine(Test.GetOutputFolder(), "IisSite");
            Directory.CreateDirectory(iisFolder);

            CreateIisSite(iisFolder);

            DeleteIisSite();
        }

        #region IIS

        private const string    c_iisName = "ZipDeployTestApp";
        private const int       c_iisPort = 8099;

        private void CreateIisSite(string iisFolder)
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

        private void DeleteIisSite()
        {
            using (var iisManager = new ServerManager())
            {
                var site = iisManager.Sites.SingleOrDefault(s => s.Name == c_iisName);
                iisManager.Sites.Remove(site);
                iisManager.CommitChanges();
                Test.WriteProgress($"Removed IIS set {c_iisName}");
            }
        }

        #endregion
    }
}
