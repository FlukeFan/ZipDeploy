using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
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
        public async Task Unzip_BinariesAreRenamed()
        {
            ExistingFiles("binary1.dll", "binary2.exe");

            await CreateZipAsync(ZipDeployOptions.DefaultNewPackageFileName, "binary1.dll", "binary2.exe");

            await NewUnzipper().UnzipAsync();

            File.ReadAllText("binary1.dll").Should().Be("zipped content of binary1.dll");
            File.ReadAllText("zzz__binary1.dll.fordelete.txt").Should().Be("existing content of binary1.dll");
            File.ReadAllText("binary2.exe").Should().Be("zipped content of binary2.exe");
            File.ReadAllText("zzz__binary2.exe.fordelete.txt").Should().Be("existing content of binary2.exe");
        }

        [Test]
        public async Task Unzip_ExistingMarkedDeleteFilesAreOverwritten()
        {
            var expectedContent = "existing content of binary.dll";

            ExistingFiles("binary.dll", "zzz__binary.dll.fordelete.txt");

            File.ReadAllText("zzz__binary.dll.fordelete.txt").Should().NotBe(expectedContent);

            await CreateZipAsync(ZipDeployOptions.DefaultNewPackageFileName, @"binary.dll");

            await NewUnzipper().UnzipAsync();

            File.ReadAllText("zzz__binary.dll.fordelete.txt").Should().Be(expectedContent);
        }

        [Test]
        public async Task Unzip_OverwritesWebConfig()
        {
            ExistingFiles("web.config");

            await CreateZipAsync(ZipDeployOptions.DefaultNewPackageFileName, @"web.config");

            var restarter = new AspNetRestart(new LoggerFactory().CreateLogger<AspNetRestart>(), new ProcessWebConfig(), new CanPauseTrigger(), new ZipDeployOptions());
            await restarter.TriggerAsync();

            File.ReadAllText("web.config").Should().Be("zipped content of web.config");
        }

        [Test]
        public async Task Unzip_KeepsOriginalIfNoChanges()
        {
            ExistingFiles();

            await CreateZipAsync(ZipDeployOptions.DefaultNewPackageFileName, @"binary.dll", @"nonbinary.txt");

            var unzipper = NewUnzipper();
            await unzipper.UnzipAsync();

            var existingModifiedDateTime = DateTime.UtcNow - TimeSpan.FromHours(3);
            File.SetLastWriteTimeUtc("binary.dll", existingModifiedDateTime);
            File.SetLastWriteTimeUtc("nonbinary.txt", existingModifiedDateTime);

            await CreateZipAsync(ZipDeployOptions.DefaultNewPackageFileName, @"binary.dll", @"nonbinary.txt");

            await unzipper.UnzipAsync();

            File.GetLastWriteTimeUtc("binary.dll").Should().Be(existingModifiedDateTime);
            File.GetLastWriteTimeUtc("nonbinary.txt").Should().Be(existingModifiedDateTime);
        }

        [Test]
        public async Task Unzip_OverwritesExistingDeployedArchive()
        {
            ExistingFiles(ZipDeployOptions.DefaultDeployedPackageFileName);

            await CreateZipAsync(ZipDeployOptions.DefaultNewPackageFileName);

            await NewUnzipper().UnzipAsync();

            File.Exists(ZipDeployOptions.DefaultNewPackageFileName).Should().BeFalse("publish.zip should have been renamed");
            File.Exists(ZipDeployOptions.DefaultDeployedPackageFileName).Should().BeTrue("publish.zip should have been renamed to deployed.zip");
            File.ReadAllText(ZipDeployOptions.DefaultDeployedPackageFileName).Should().NotBe("existing content of deployed.zip", "previous deployed.zip should have been overwritten");
        }

        [Test]
        public async Task Unzip_RenamesObsoleteBinaries()
        {
            ExistingFiles("file1.dll", "legacy.dll");

            await CreateZipAsync(ZipDeployOptions.DefaultNewPackageFileName, "file1.dll");

            await NewUnzipper().UnzipAsync();

            File.ReadAllText("file1.dll").Should().Be("zipped content of file1.dll");
            File.Exists("legacy.dll").Should().BeFalse("obsolete legacy.dll should have been renamed");
        }

        [Test]
        public async Task Sync_ObsoleteFilesAreRemoved()
        {
            ExistingFiles(
                "new.dll",
                "zzz__obsolete.dll.fordelete.txt",
                @"wwwroot\zzz__new.txt.fordelete.txt");

            await CreateZipAsync(ZipDeployOptions.DefaultDeployedPackageFileName, "new.dll", @"wwwroot\new.txt");

            await NewCleaner().DeleteObsoleteFilesAsync();

            File.Exists("zzz__obsolete.dll.fordelete.txt").Should().BeFalse("ZipDeploy should have deleted obsolete.dll.fordelete.txt");
            File.Exists(@"wwwroot\zzz__new.txt.fordelete.txt").Should().BeFalse("new.txt.fordelete.txt should have been removed");
        }

        [Test]
        public async Task Unzip_NonBinariesAreExtracted()
        {
            ExistingFiles(@"wwwroot\file1.txt");

            await CreateZipAsync(ZipDeployOptions.DefaultNewPackageFileName,
                @"file1.dll",
                @"wwwroot\file1.txt",
                @"wwwroot\file2.txt");

            await NewUnzipper().UnzipAsync();

            File.ReadAllText(@"wwwroot\file1.txt").Should().Be(@"zipped content of wwwroot\file1.txt");
            File.ReadAllText(@"wwwroot\file2.txt").Should().Be(@"zipped content of wwwroot\file2.txt");
        }

        [Test]
        public async Task CaseInsensitiveFilesAreHandled()
        {
            ExistingFiles("file.txt", "file.dll");

            await CreateZipAsync(ZipDeployOptions.DefaultNewPackageFileName, "FILE.TXT", "FILE.DLL");

            var unzipper = NewUnzipper();
            await unzipper.UnzipAsync();

            File.ReadAllText("FILE.TXT").Should().Be("zipped content of FILE.TXT");
            File.ReadAllText("FILE.DLL").Should().Be("zipped content of FILE.DLL");
        }

        [Test]
        public async Task PathsCanBeExcluded()
        {
            ExistingFiles(
                "log.txt",
                "uploads/sub/sub/file1.txt",
                "uploads//sub/file2.dll",
                "uploads2\\subfolder\\file3.txt");

            await CreateZipAsync(ZipDeployOptions.DefaultNewPackageFileName, "file1.txt");

            var unzipper = NewUnzipper(opt => opt
                .IgnorePathStarting("log.txt")
                .IgnorePathStarting("uploads\\sub")
                .IgnorePathStarting("uploads2\\subfolder"));

            await unzipper.UnzipAsync();

            File.Exists("file1.txt").Should().BeTrue("unzip should have extracted file1.txt");
            File.Exists("log.txt").Should().BeTrue("log.txt should have been ignored");
            File.Exists("uploads/sub/sub/file1.txt").Should().BeTrue("uploads/sub/file1.txt should have been ignored");
            File.Exists("uploads/sub/file2.dll").Should().BeTrue("uploads/file2.dll should have been ignored");
            File.Exists("uploads2/subfolder/file3.txt").Should().BeTrue("uploads2/subfolder/file3.txt should have been ignored");
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

        private async Task CreateZipAsync(string zipFileName, params string[] files)
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
                        await streamWriter.WriteAsync($"zipped content of {file}");
                }
            }

            File.Move(tmpFile, zipFileName);
        }
    }
}
