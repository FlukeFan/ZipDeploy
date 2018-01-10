using System.IO;
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

            Iis.CreateIisSite(iisFolder);

            Iis.DeleteIisSite();
        }
    }
}
