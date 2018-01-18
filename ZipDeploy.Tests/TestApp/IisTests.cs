using System.IO;
using System.IO.Compression;
using System.Net.Http;
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

            FileSystem.DeleteFolder(srcCopyFolder);
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

            FileSystem.CopySource(slnFolder, srcCopyFolder, "ZipDeploy.TestApp");
            FileSystem.ReplaceText(testAppfolder, @"HomeController.cs", "private const int c_version = 123;", "private const int c_version = 234;");
            FileSystem.ReplaceText(testAppfolder, @"wwwroot\test.js", "alert(123);", "alert(234);");
            Exec.DotnetPublish(testAppfolder);

            var uploadingZip = Path.Combine(iisFolder, "uploading.zip");
            ZipFile.CreateFromDirectory(publishFolder, uploadingZip);

            var configFile = Path.Combine(iisFolder, "web.config");
            var lastConfigChange = File.GetLastWriteTimeUtc(configFile);

            var publishZip = Path.Combine(iisFolder, "publish.zip");
            File.Move(uploadingZip, publishZip);

            Iis.ShowLogOnFail(iisFolder, () =>
                Wait.For(() =>
                {
                    File.Exists(publishZip).Should().BeFalse($"file {publishZip} should have been picked up by ZipDeploy");
                    File.GetLastWriteTimeUtc(configFile).Should().NotBe(lastConfigChange, $"file {configFile} should have been updated");
                }));

            // the binaries have been replaced, and the web.config should have been touched
            // the next request should complete the installation, and return the new responses

            Iis.ShowLogOnFail(iisFolder, () =>
            {
                Wait.Until("site should not return old value", () => !Get("http://localhost:8099").Contains("123"));

                Get("http://localhost:8099").Should().Contain("Version=234");
                Get("http://localhost:8099/test.js").Should().Contain("alert(234);");
            });

            File.Exists(Path.Combine(iisFolder, "publish.zip")).Should().BeFalse("publish.zip should have been renamed to installing.zip");
            File.Exists(Path.Combine(iisFolder, "installing.zip")).Should().BeFalse("installing.zip should have been renamed to deployed.zip");
            File.Exists(Path.Combine(iisFolder, "deployed.zip")).Should().BeTrue("deployment should be complete, and installing.zip should have been renamed to deployed.zip");

            Iis.DeleteIisSite();
        }

        private string Get(string url)
        {
            var response = new HttpClient().GetAsync(url).GetAwaiter().GetResult();
            using (var stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
            using (var streamReader = new StreamReader(stream))
                return streamReader.ReadToEnd();
        }
    }
}
