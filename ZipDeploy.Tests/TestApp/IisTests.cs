using System.IO;
using System.Net;
using FluentAssertions;
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

            Get("http://localhost:8099").Should().Contain("Version=123");
            Get("http://localhost:8099/test.js").Should().Contain("alert(123);");

            Iis.DeleteIisSite();
        }

        private string Get(string url)
        {
            var request = HttpWebRequest.Create(url);
            var response = request.GetResponse();
            using (var stream = response.GetResponseStream())
            using (var streamReader = new StreamReader(stream))
                return streamReader.ReadToEnd();
        }
    }
}
