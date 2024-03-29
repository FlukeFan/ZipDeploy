﻿using System.IO;
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

        [Test]
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

        [Test]
        [Test.IsSlow]
        public void DeployZip3_1()
        {
            IisAdmin.VerifyModuleInstalled(
                moduleName: "AspNetCoreModuleV2",
                downloadUrl: "https://download.visualstudio.microsoft.com/download/pr/7e35ac45-bb15-450a-946c-fe6ea287f854/a37cfb0987e21097c7969dda482cebd3/dotnet-hosting-3.1.10-win.exe");

            DeployZip(new DeployZipOptions
            {
                ExpectedRuntimeVersion = "3.1",
                AppSourceFolder = "ZipDeploy.TestApp3_1",
                AppPublishFolder = @"bin\Debug\netcoreapp3.1\win-x64\publish",
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
            Test.WriteProgress($"appSourceFolder={options.AppSourceFolder}");
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

            var existingZipTemp = Path.Combine(outputFolder, "publish.zip");
            ZipFile.CreateFromDirectory(iisFolder, existingZipTemp);
            var existingZip = Path.Combine(iisFolder, "publish.zip");
            File.Move(existingZipTemp, existingZip);

            IisAdmin.ShowLogOnFail(iisFolder, () =>
            {
                IisAdmin.CreateIisSite(iisFolder);

                Get("http://localhost:8099/home/runtime").Should().Be(options.ExpectedRuntimeVersion);

                Wait.For(() =>
                {
                    File.Exists(Path.Combine(iisFolder, ZipDeployOptions.DefaultNewPackageFileName)).Should().BeFalse("existing publish.zip should have been picked up at startup");
                    File.Exists(Path.Combine(iisFolder, ZipDeployOptions.DefaultDeployedPackageFileName)).Should().BeTrue("deployment should be complete, and publish.zip should have been renamed to deployed.zip");
                });

                // avoids IIS returning 500.3:  https://github.com/dotnet/aspnetcore/issues/10117
                System.Threading.Thread.Sleep(200);

                Get("http://localhost:8099").Should().Contain("Version=123");
                Get("http://localhost:8099/test.js").Should().Contain("alert(123);");

                Test.WriteProgress($"Verified version 123");

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

                Test.WriteProgress($"Wrote {publishZip}");

                Wait.For(() =>
                {
                    File.Exists(publishZip).Should().BeFalse($"file {publishZip} should have been picked up by ZipDeploy");
                    File.GetLastWriteTimeUtc(configFile).Should().NotBe(lastConfigChange, $"file {configFile} should have been updated");
                });

                Test.WriteProgress($"Verified {publishZip} has been picked up and {configFile} has been updated");
                System.Threading.Thread.Sleep(200); // remove once IIS recycle working locally

                var webConfig = Path.Combine(iisFolder, "web.config");
                File.WriteAllText(webConfig, File.ReadAllText(webConfig).Replace("stdoutLogEnabled=\"false\"", "stdoutLogEnabled=\"true\""));

                // the binaries have been replaced, and the web.config should have been touched
                // the next request should complete the installation, and return the new responses

                Get("http://localhost:8099").Should().Contain("Version=234");
                Get("http://localhost:8099/test.js").Should().Contain("alert(234);");

                Test.WriteProgress($"Verified version 234");

                File.Exists(Path.Combine(iisFolder, ZipDeployOptions.DefaultNewPackageFileName)).Should().BeFalse("publish.zip should have been renamed to deployed.zip");
                File.Exists(Path.Combine(iisFolder, ZipDeployOptions.DefaultDeployedPackageFileName)).Should().BeTrue("deployment should be complete, and publish.zip should have been renamed to deployed.zip");
                File.Exists(Path.Combine(iisFolder, "zzz__ZipDeploy.dll.fordelete.txt")).Should().BeFalse("obsolete binaries should have been deleted on next startup");

                IisAdmin.DeleteIisSite();
            });

            Test.WriteProgress($"appSourceFolder={options.AppSourceFolder} success");
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
