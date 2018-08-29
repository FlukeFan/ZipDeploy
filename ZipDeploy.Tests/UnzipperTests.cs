﻿using System;
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
            ExistingFiles("binary1.dll", "binary2.exe");

            CreateZip("publish.zip", "binary1.dll", "binary2.exe");

            NewUnzipper().UnzipBinaries();

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

            CreateZip("publish.zip", @"binary.dll");

            NewUnzipper().UnzipBinaries();

            File.ReadAllText("zzz__binary.dll.fordelete.txt").Should().Be(expectedContent);
        }

        [Test]
        public void Unzip_OverwritesWebConfig()
        {
            ExistingFiles("web.config");

            CreateZip("publish.zip", @"web.config");

            NewUnzipper().UnzipBinaries();

            File.ReadAllText("web.config").Should().Be("zipped content of web.config");
        }

        [Test]
        public void Unzip_NonBinariesAreNotUnzipped()
        {
            ExistingFiles();

            CreateZip("publish.zip", @"wwwroot\lib\jQuery.js");

            NewUnzipper().UnzipBinaries();

            File.Exists(@"wwwroot\lib\jQuery.js").Should().BeFalse("only binaries should be unzipped (they are not unzipped until 'sync')");
        }

        [Test]
        public void Unzip_KeepsOriginalIfNoChanges()
        {
            File.WriteAllText("binary.dll", "zipped content of binary.dll");

            var existingModifiedDateTime = DateTime.UtcNow - TimeSpan.FromHours(3);
            File.SetLastWriteTimeUtc("binary.dll", existingModifiedDateTime);

            CreateZip("publish.zip", @"binary.dll");

            NewUnzipper().UnzipBinaries();

            File.GetLastWriteTimeUtc("binary.dll").Should().Be(existingModifiedDateTime);
        }

        [Test]
        public void Unzip_OverwritesExistingUnzippedArchive()
        {
            ExistingFiles("installing.zip");

            CreateZip("publish.zip");

            NewUnzipper().UnzipBinaries();

            File.Exists("publish.zip").Should().BeFalse("publish should have been renamed");
            File.Exists("installing.zip").Should().BeTrue("publish should have been renamed to installing.zip");
            File.ReadAllText("installing.zip").Should().NotBe("existing content of installing.zip", "previous installing.zip should have been overwritten");
        }

        [Test]
        public void Unzip_RenamesObsoleteBinaries()
        {
            ExistingFiles("file1.dll", "legacy.dll");

            CreateZip("publish.zip", "file1.dll");

            NewUnzipper().UnzipBinaries();

            File.ReadAllText("file1.dll").Should().Be("zipped content of file1.dll");
            File.Exists("legacy.dll").Should().BeFalse("obsolete legacy.dll should have been renamed");
        }

        [Test]
        public void Sync_OverwritesExistingDeployedArchive()
        {
            var previousContent = "existing content of deployed.zip";

            ExistingFiles("deployed.zip");

            File.ReadAllText("deployed.zip").Should().Be(previousContent);

            CreateZip("installing.zip");

            NewUnzipper().SyncNonBinaries();

            File.Exists("installing.zip").Should().BeFalse("installing.zip should have been renamed");
            File.Exists("deployed.zip").Should().BeTrue("installing should have been renamed to deployed.zip");
            File.ReadAllText("deployed.zip").Should().NotBe(previousContent);
        }

        [Test]
        public void Sync_ObsoleteFilesAreRemoved()
        {
            ExistingFiles(
                "new.dll",
                "zzz__obsolete.dll.fordelete.txt",
                @"wwwroot\legacy.txt",
                @"wwwroot\zzz__new.txt.fordelete.txt");

            CreateZip("installing.zip", "new.dll", @"wwwroot\new.txt");

            NewUnzipper().SyncNonBinaries();

            File.Exists("zzz__obsolete.dll.fordelete.txt").Should().BeFalse("ZipDeploy should have deleted obsolete.dll.fordelete.txt");
            File.Exists(@"wwwroot\legacy.txt").Should().BeFalse("legacy.txt should have been removed");
            File.Exists(@"wwwroot\zzz__new.txt.fordelete.txt").Should().BeFalse("new.txt.fordelete.txt should have been removed");
        }

        [Test]
        public void Sync_BinariesAreNotReExtracted()
        {
            ExistingFiles("fresh.dll", "web.config");

            CreateZip("installing.zip", "fresh.dll", "web.config");

            NewUnzipper().SyncNonBinaries();

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

            NewUnzipper().SyncNonBinaries();

            File.ReadAllText(@"wwwroot\file1.txt").Should().Be(@"zipped content of wwwroot\file1.txt");
            File.ReadAllText(@"wwwroot\file2.txt").Should().Be(@"zipped content of wwwroot\file2.txt");
        }

        [Test]
        public void CaseInsensitiveFilesAreHandled()
        {
            ExistingFiles("file.txt", "file.dll");

            CreateZip("publish.zip", "FILE.TXT", "FILE.DLL");

            var unzipper = NewUnzipper();
            unzipper.UnzipBinaries();
            unzipper.SyncNonBinaries();

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

            CreateZip("publish.zip", "file1.txt");

            var unzipper = NewUnzipper(opt => opt
                .IgnorePathStarting("log.txt")
                .IgnorePathStarting("uploads\\sub")
                .IgnorePathStarting("uploads2\\subfolder"));

            unzipper.UnzipBinaries();
            unzipper.SyncNonBinaries();

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

        private Unzipper NewUnzipper(Action<ZipDeployOptions> configure = null)
        {
            var options = new ZipDeployOptions();
            configure?.Invoke(options);
            return new Unzipper(options);
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
