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

            CopySource(slnFolder, srcCopyFolder, "Build");
            CopySource(slnFolder, srcCopyFolder, "ZipDeploy");
            CopySource(slnFolder, srcCopyFolder, "ZipDeploy.TestApp");

            Iis.DeleteIisSite();
        }

        private void CopySource(string slnFolder, string srcCopyFolder, string projectName)
        {
            var src = Path.Combine(slnFolder, projectName);
            var copy = Path.Combine(srcCopyFolder, projectName);

            FileSystem.CopyDir(src, copy);
        }
    }
}
