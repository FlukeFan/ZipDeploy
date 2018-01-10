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
        }

        private void CreateIisSite(string iisFolder)
        {
            var iisManager = new ServerManager();

            var name = "ZipDeployTestApp";

            if (iisManager.Sites.Count(s => s.Name == name) == 0)
            {
                iisManager.Sites.Add("ZipDeployTestApp", iisFolder, 8099);
                iisManager.CommitChanges();
            }

            Test.WriteProgress($"Created IIS site {name}:8099 in {iisFolder}");
        }
    }
}
