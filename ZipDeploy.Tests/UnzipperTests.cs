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
            ZipDeploy.LogFactory = new LoggerFactory();

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
            ExistingFiles("binary.dll");

            CreateZip("publish.zip", "binary.dll");

            new Unzipper().UnzipBinaries();

            File.ReadAllText("binary.dll").Should().Be("zipped content of binary.dll");
            File.ReadAllText("binary.dll.fordelete.txt").Should().Be("existing content of binary.dll");
        }

        [Test]
        public void Unzip_ExistingMarkedDeleteFilesAreOverwritten()
        {
            var expectedContent = "existing content of binary.dll";

            ExistingFiles("binary.dll", "binary.dll.fordelete.txt");

            File.ReadAllText("binary.dll.fordelete.txt").Should().NotBe(expectedContent);

            CreateZip("publish.zip", @"binary.dll");

            new Unzipper().UnzipBinaries();

            File.ReadAllText("binary.dll.fordelete.txt").Should().Be(expectedContent);
        }

        [Test]
        public void Unzip_OverwritesWebConfig()
        {
            ExistingFiles("web.config");

            CreateZip("publish.zip", @"web.config");

            new Unzipper().UnzipBinaries();

            File.ReadAllText("web.config").Should().Be("zipped content of web.config");
        }

        [Test]
        public void Unzip_NonBinariesAreNotUnzipped()
        {
            ExistingFiles();

            CreateZip("publish.zip", @"wwwroot\lib\jQuery.js");

            new Unzipper().UnzipBinaries();

            File.Exists(@"wwwroot\lib\jQuery.js").Should().BeFalse("only binaries should be unzipped (they are not unzipped until 'sync')");
        }

        [Test]
        public void Unzip_OverwritesExistingUnzippedArchive()
        {
            ExistingFiles("installing.zip");

            CreateZip("publish.zip");

            new Unzipper().UnzipBinaries();

            File.Exists("publish.zip").Should().BeFalse("publish should have been renamed");
            File.Exists("installing.zip").Should().BeTrue("publish should have been renamed to installing.zip");
            File.ReadAllText("installing.zip").Should().NotBe("existing content of installing.zip", "previous installing.zip should have been overwritten");
        }

        [Test]
        public void Sync_OverwritesExistingDeployedArchive()
        {
            var previousContent = "existing content of deployed.zip";

            ExistingFiles("deployed.zip");

            File.ReadAllText("deployed.zip").Should().Be(previousContent);

            CreateZip("installing.zip");

            new Unzipper().SyncNonBinaries();

            File.Exists("installing.zip").Should().BeFalse("installing.zip should have been renamed");
            File.Exists("deployed.zip").Should().BeTrue("installing should have been renamed to deployed.zip");
            File.ReadAllText("deployed.zip").Should().NotBe(previousContent);
        }

        [Test]
        public void Sync_ObsoleteFilesAreRemoved()
        {
            ExistingFiles(
                "new.dll",
                "obsolete.dll.fordelete.txt",
                @"wwwroot\legacy.txt",
                @"wwwroot\new.txt.fordelete.txt");

            CreateZip("installing.zip", "new.dll", @"wwwroot\new.txt");

            new Unzipper().SyncNonBinaries();

            File.Exists("obsolete.dll.fordelete.txt").Should().BeFalse("ZipDeploy should have deleted obsolete.dll.fordelete.txt");
            File.Exists(@"wwwroot\legacy.txt").Should().BeFalse("legacy.txt should have been removed");
            File.Exists(@"wwwroot\new.txt.fordelete.txt").Should().BeFalse("new.txt.fordelete.txt should have been removed");
        }

        [Test]
        public void Sync_BinariesAreNotReExtracted()
        {
            ExistingFiles("fresh.dll", "web.config");

            CreateZip("installing.zip", "fresh.dll", "web.config");

            new Unzipper().SyncNonBinaries();

            File.ReadAllText("fresh.dll").Should().Contain("existing");
            File.ReadAllText("web.config").Should().Contain("existing");
        }

        [Test]
        public void Sync_NonBinariesAreExtracted()
        {
            ExistingFiles(@"wwwroot\file1.txt");

            CreateZip("installing.zip",
                @"file1.dll",
                @"wwwroot\file1.txt",
                @"wwwroot\file2.txt");

            new Unzipper().SyncNonBinaries();

            File.ReadAllText(@"wwwroot\file1.txt").Should().Be(@"zipped content of wwwroot\file1.txt");
            File.ReadAllText(@"wwwroot\file2.txt").Should().Be(@"zipped content of wwwroot\file2.txt");
        }

        [Test]
        public void PathWithoutExtension()
        {
            Unzipper.PathWithoutExtension("test.dll").Should().Be("test");
            Unzipper.PathWithoutExtension(@"wwwroot\test.txt").Should().Be(@"wwwroot\test");
            Unzipper.PathWithoutExtension(@"wwwroot\test.dll.dll").Should().Be(@"wwwroot\test");
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
