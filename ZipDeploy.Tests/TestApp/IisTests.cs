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
            Iis.DeleteIisSite();

            var outputFolder = Test.GetOutputFolder();
            var slnFolder = Test.GetSlnFolder();
            var srcCopyFolder = Path.Combine(outputFolder, "src");

            FileSystem.CopySource(slnFolder, srcCopyFolder, "Build");
            FileSystem.CopySource(slnFolder, srcCopyFolder, "ZipDeploy");
            FileSystem.CopySource(slnFolder, srcCopyFolder, "ZipDeploy.TestApp");

            var testAppfolder = Path.Combine(srcCopyFolder, "ZipDeploy.TestApp");
            Exec.DotnetPublish(testAppfolder);

            var publishFolder = Path.Combine(testAppfolder, @"bin\Debug\netcoreapp2.0\publish");
            var iisFolder = Path.Combine(outputFolder, "IisSite");

            FileSystem.DeleteFolder(iisFolder);
            Directory.Move(publishFolder, iisFolder);

            Iis.CreateIisSite(iisFolder);

            Iis.DeleteIisSite();
        }
    }
}
