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
        [Test.IsSlow]
        public void DeployZipExeStyle()
        {
            IisAdmin.VerifyModuleInstalled(
                moduleName: "AspNetCoreModule",
                downloadUrl: "https://download.microsoft.com/download/6/E/B/6EBD972D-2E2F-41EB-9668-F73F5FDDC09C/dotnet-hosting-2.1.3-win.exe");

            DeployZip(new DeployZipOptions
            {
                ExpectedRuntimeVersion = "2.1",
                AppSourceFolder = "ZipDeploy.TestApp2_1Exe",
                AppPublishFolder = @"bin\Debug\netcoreapp2.1\win-x64\publish",
            });
        }

        [Test][Ignore("to implement .UseZipDeploy()")]
        [Test.IsSlow]
        public void DeployZip2_1()
        {
            IisAdmin.VerifyModuleInstalled(
                moduleName: "AspNetCoreModule",
                downloadUrl: "https://download.microsoft.com/download/6/E/B/6EBD972D-2E2F-41EB-9668-F73F5FDDC09C/dotnet-hosting-2.1.3-win.exe");

            DeployZip(new DeployZipOptions
            {
                ExpectedRuntimeVersion = "2.1",
                AppSourceFolder = "ZipDeploy.TestApp2_1",
                AppPublishFolder = @"bin\Debug\netcoreapp2.1\win-x64\publish",
            });
        }

        private class DeployZipOptions
        {
            public string ExpectedRuntimeVersion;
            public string AppSourceFolder;
            public string AppPublishFolder;
        }

        private void DeployZip(DeployZipOptions options)
        {
            IisAdmin.DeleteIisSite();

            var outputFolder = Test.GetOutputFolder();
            Test.WriteProgress($"outputFolder={outputFolder}");

            var slnFolder = Test.GetSlnFolder();
            Test.WriteProgress($"slnFolder={slnFolder}");

            var srcCopyFolder = Path.Combine(outputFolder, "src");

            FileSystem.DeleteFolder(srcCopyFolder);
            FileSystem.CopySource(slnFolder, srcCopyFolder, "Build");
            FileSystem.CopySource(slnFolder, srcCopyFolder, "ZipDeploy");
            FileSystem.CopySource(slnFolder, srcCopyFolder, options.AppSourceFolder);
            File.Copy(Path.Combine(slnFolder, "icon.png"), Path.Combine(srcCopyFolder, "icon.png"));

            var testAppfolder = Path.Combine(srcCopyFolder, options.AppSourceFolder);
            Exec.DotnetPublish(testAppfolder);

            var publishFolder = Path.Combine(testAppfolder, options.AppPublishFolder);
            var iisFolder = Path.Combine(outputFolder, "IisSite");

            FileSystem.DeleteFolder(iisFolder);
            Directory.Move(publishFolder, iisFolder);

            IisAdmin.ShowLogOnFail(iisFolder, () =>
            {
                IisAdmin.CreateIisSite(iisFolder);

                Get("http://localhost:8099/home/runtime").Should().Be(options.ExpectedRuntimeVersion);
                Get("http://localhost:8099").Should().Contain("Version=123");
                Get("http://localhost:8099/test.js").Should().Contain("alert(123);");

                FileSystem.CopySource(slnFolder, srcCopyFolder, options.AppSourceFolder);
                FileSystem.ReplaceText(testAppfolder, @"HomeController.cs", "private const int c_version = 123;", "private const int c_version = 234;");
                FileSystem.ReplaceText(testAppfolder, @"wwwroot\test.js", "alert(123);", "alert(234);");
                Exec.DotnetPublish(testAppfolder);

                var uploadingZip = Path.Combine(iisFolder, "uploading.zip");
                ZipFile.CreateFromDirectory(publishFolder, uploadingZip);

                var configFile = Path.Combine(iisFolder, "web.config");
                var lastConfigChange = File.GetLastWriteTimeUtc(configFile);

                var publishZip = Path.Combine(iisFolder, ZipDeployOptions.DefaultNewPackageFileName);
                File.Move(uploadingZip, publishZip);

                Wait.For(() =>
                {
                    File.Exists(publishZip).Should().BeFalse($"file {publishZip} should have been picked up by ZipDeploy");
                    File.GetLastWriteTimeUtc(configFile).Should().NotBe(lastConfigChange, $"file {configFile} should have been updated");
                });

                // the binaries have been replaced, and the web.config should have been touched
                // the next request should complete the installation, and return the new responses

                Get("http://localhost:8099/test.js").Should().Contain("alert(234);");
                Get("http://localhost:8099").Should().Contain("Version=234");

                File.Exists(Path.Combine(iisFolder, ZipDeployOptions.DefaultNewPackageFileName)).Should().BeFalse("publish.zip should have been renamed to deployed.zip");
                File.Exists(Path.Combine(iisFolder, ZipDeployOptions.DefaultDeployedPackageFileName)).Should().BeTrue("deployment should be complete, and publish.zip should have been renamed to deployed.zip");
                File.Exists(Path.Combine(iisFolder, "zzz__ZipDeploy.dll.fordelete.txt")).Should().BeFalse("obsolete binaries should have been deleted on next startup");

                IisAdmin.DeleteIisSite();
            });
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
