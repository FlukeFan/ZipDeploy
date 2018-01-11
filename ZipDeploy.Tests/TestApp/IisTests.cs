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
            var outputFolder = Test.GetOutputFolder();
            var iisFolder = Path.Combine(outputFolder, "IisSite");
            Directory.CreateDirectory(iisFolder);

            Iis.CreateIisSite(iisFolder);

            var slnFolder = Test.GetSlnFolder();
            var srcCopyFolder = Path.Combine(outputFolder, "src");

            if (Directory.Exists(srcCopyFolder))
                Directory.Delete(srcCopyFolder, true);

            Directory.CreateDirectory(srcCopyFolder);

            var testAppSrc = Path.Combine(slnFolder, "ZipDeploy.TestApp");
            var testAppCopy = Path.Combine(srcCopyFolder, "ZipDeploy.TestApp");

            FileSystem.CopyDir(testAppSrc, testAppCopy);

            Iis.DeleteIisSite();
        }
    }
}
