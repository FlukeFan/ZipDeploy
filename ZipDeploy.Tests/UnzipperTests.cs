using System;
using System.IO;
using System.IO.Compression;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace ZipDeploy.Tests
{
    [TestFixture]
    public class UnzipperTests
    {
        private string _originalCurrentDirectory;
        private string _filesFolder;

        [SetUp]
        public void SetUp()
        {
            _filesFolder = Path.Combine(Test.GetOutputFolder(), "testFiles");
            FileSystem.DeleteFolder(_filesFolder);
            Directory.CreateDirectory(_filesFolder);
            _originalCurrentDirectory = Environment.CurrentDirectory;
            Environment.CurrentDirectory = _filesFolder;
        }

        [TearDown]
        public void TearDown()
        {
            Environment.CurrentDirectory = _originalCurrentDirectory;
        }

        [Test]
        public void Unzip_BinariesAreRenamed()
        {
            ExistingFiles("binary1.dll", "binary2.exe");

            CreateZip(ZipDeployOptions.DefaultNewPackageFileName, "binary1.dll", "binary2.exe");

            NewUnzipper().Unzip();

            File.ReadAllText("binary1.dll").Should().Be("zipped content of binary1.dll");
            File.ReadAllText("zzz__binary1.dll.fordelete.txt").Should().Be("existing content of binary1.dll");
            File.ReadAllText("binary2.exe").Should().Be("zipped content of binary2.exe");
            File.ReadAllText("zzz__binary2.exe.fordelete.txt").Should().Be("existing content of binary2.exe");
        }

        [Test]
        public void Unzip_ExistingMarkedDeleteFilesAreOverwritten()
        {
            var expectedContent = "existing content of binary.dll";

            ExistingFiles("binary.dll", "zzz__binary.dll.fordelete.txt");

            File.ReadAllText("zzz__binary.dll.fordelete.txt").Should().NotBe(expectedContent);

            CreateZip(ZipDeployOptions.DefaultNewPackageFileName, @"binary.dll");

            NewUnzipper().Unzip();

            File.ReadAllText("zzz__binary.dll.fordelete.txt").Should().Be(expectedContent);
        }

        [Test]
        public void Unzip_OverwritesWebConfig()
        {
            ExistingFiles("web.config");

            CreateZip(ZipDeployOptions.DefaultNewPackageFileName, @"web.config");

            var restarter = new AspNetRestart(new LoggerFactory().CreateLogger<AspNetRestart>(), new ZipDeployOptions());
            restarter.Trigger();

            File.ReadAllText("web.config").Should().Be("zipped content of web.config");
        }

        [Test]
        public void Unzip_KeepsOriginalIfNoChanges()
        {
            ExistingFiles();

            CreateZip(ZipDeployOptions.DefaultNewPackageFileName, @"binary.dll", @"nonbinary.txt");

            var unzipper = NewUnzipper();
            unzipper.Unzip();

            var existingModifiedDateTime = DateTime.UtcNow - TimeSpan.FromHours(3);
            File.SetLastWriteTimeUtc("binary.dll", existingModifiedDateTime);
            File.SetLastWriteTimeUtc("nonbinary.txt", existingModifiedDateTime);

            CreateZip(ZipDeployOptions.DefaultNewPackageFileName, @"binary.dll", @"nonbinary.txt");

            unzipper.Unzip();

            File.GetLastWriteTimeUtc("binary.dll").Should().Be(existingModifiedDateTime);
            File.GetLastWriteTimeUtc("nonbinary.txt").Should().Be(existingModifiedDateTime);
        }

        [Test]
        public void Unzip_OverwritesExistingDeployedArchive()
        {
            ExistingFiles(ZipDeployOptions.DefaultDeployedPackageFileName);

            CreateZip(ZipDeployOptions.DefaultNewPackageFileName);

            NewUnzipper().Unzip();

            File.Exists(ZipDeployOptions.DefaultNewPackageFileName).Should().BeFalse("publish.zip should have been renamed");
            File.Exists(ZipDeployOptions.DefaultDeployedPackageFileName).Should().BeTrue("publish.zip should have been renamed to deployed.zip");
            File.ReadAllText(ZipDeployOptions.DefaultDeployedPackageFileName).Should().NotBe("existing content of deployed.zip", "previous deployed.zip should have been overwritten");
        }

        [Test]
        public void Unzip_RenamesObsoleteBinaries()
        {
            ExistingFiles("file1.dll", "legacy.dll");

            CreateZip(ZipDeployOptions.DefaultNewPackageFileName, "file1.dll");

            NewUnzipper().Unzip();

            File.ReadAllText("file1.dll").Should().Be("zipped content of file1.dll");
            File.Exists("legacy.dll").Should().BeFalse("obsolete legacy.dll should have been renamed");
        }

        [Test]
        public void Sync_ObsoleteFilesAreRemoved()
        {
            ExistingFiles(
                "new.dll",
                "zzz__obsolete.dll.fordelete.txt",
                @"wwwroot\zzz__new.txt.fordelete.txt");

            CreateZip(ZipDeployOptions.DefaultDeployedPackageFileName, "new.dll", @"wwwroot\new.txt");

            NewCleaner().DeleteObsoleteFiles();

            File.Exists("zzz__obsolete.dll.fordelete.txt").Should().BeFalse("ZipDeploy should have deleted obsolete.dll.fordelete.txt");
            File.Exists(@"wwwroot\zzz__new.txt.fordelete.txt").Should().BeFalse("new.txt.fordelete.txt should have been removed");
        }

        [Test]
        public void Unzip_NonBinariesAreExtracted()
        {
            ExistingFiles(@"wwwroot\file1.txt");

            CreateZip(ZipDeployOptions.DefaultNewPackageFileName,
                @"file1.dll",
                @"wwwroot\file1.txt",
                @"wwwroot\file2.txt");

            NewUnzipper().Unzip();

            File.ReadAllText(@"wwwroot\file1.txt").Should().Be(@"zipped content of wwwroot\file1.txt");
            File.ReadAllText(@"wwwroot\file2.txt").Should().Be(@"zipped content of wwwroot\file2.txt");
        }

        [Test]
        public void CaseInsensitiveFilesAreHandled()
        {
            ExistingFiles("file.txt", "file.dll");

            CreateZip(ZipDeployOptions.DefaultNewPackageFileName, "FILE.TXT", "FILE.DLL");

            var unzipper = NewUnzipper();
            unzipper.Unzip();

            File.ReadAllText("FILE.TXT").Should().Be("zipped content of FILE.TXT");
            File.ReadAllText("FILE.DLL").Should().Be("zipped content of FILE.DLL");
        }

        [Test]
        public void PathsCanBeExcluded()
        {
            ExistingFiles(
                "log.txt",
                "uploads/sub/sub/file1.txt",
                "uploads//sub/file2.dll",
                "uploads2\\subfolder\\file3.txt");

            CreateZip(ZipDeployOptions.DefaultNewPackageFileName, "file1.txt");

            var unzipper = NewUnzipper(opt => opt
                .IgnorePathStarting("log.txt")
                .IgnorePathStarting("uploads\\sub")
                .IgnorePathStarting("uploads2\\subfolder"));

            unzipper.Unzip();

            File.Exists("file1.txt").Should().BeTrue("unzip should have extracted file1.txt");
            File.Exists("log.txt").Should().BeTrue("log.txt should have been ignored");
            File.Exists("uploads/sub/sub/file1.txt").Should().BeTrue("uploads/sub/file1.txt should have been ignored");
            File.Exists("uploads/sub/file2.dll").Should().BeTrue("uploads/file2.dll should have been ignored");
            File.Exists("uploads2/subfolder/file3.txt").Should().BeTrue("uploads2/subfolder/file3.txt should have been ignored");
        }

        [Test]
        public void PathWithoutExtension()
        {
            Unzipper.PathWithoutExtension("test.dll").Should().Be("test");
            Unzipper.PathWithoutExtension(@"wwwroot\test.txt").Should().Be(@"wwwroot\test");
            Unzipper.PathWithoutExtension(@"wwwroot\test.dll.dll").Should().Be(@"wwwroot\test");
        }

        [Test]
        public void ZipDeploy_RegistrationIsComplete()
        {
            ZipDeploy.Run(
                new LoggerFactory(),
                _ => { },
                () =>
                {
                    // do nothing
                });
        }

        private Unzipper NewUnzipper(Action<ZipDeployOptions> configure = null)
        {
            var options = new ZipDeployOptions();

            configure?.Invoke(options);
            return new Unzipper(new LoggerFactory().CreateLogger<Unzipper>(), options);
        }

        private Cleaner NewCleaner()
        {
            return new Cleaner(new LoggerFactory().CreateLogger<Cleaner>());
        }

        private void ExistingFiles(params string[] files)
        {
            foreach (var file in files)
            {
                FileSystem.CreateFolder(file);
                File.WriteAllText(file, $"existing content of {file}");
            }
        }

        private void CreateZip(string zipFileName, params string[] files)
        {
            var tmpFile = zipFileName + ".tmp";

            using (var zipStream = File.OpenWrite(tmpFile))
            using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create))
            {
                foreach (var file in files)
                {
                    var entry = zipArchive.CreateEntry(file);
                    using (var entryStream = entry.Open())
                    using (var streamWriter = new StreamWriter(entryStream))
                        streamWriter.Write($"zipped content of {file}");
                }
            }

            File.Move(tmpFile, zipFileName);
        }
    }
}
