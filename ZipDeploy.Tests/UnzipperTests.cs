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

            var unzipper = new Unzipper();
            unzipper.UnzipBinaries();

            File.ReadAllText("binary.dll").Should().Be("zipped content of binary.dll");
            File.ReadAllText("binary.dll.fordelete.txt").Should().Be("existing content of binary.dll");
        }

        [Test]
        public void Unzip_NonBinariesAreNotUnzipped()
        {
            ExistingFiles();

            CreateZip("publish.zip", @"wwwroot\lib\jQuery.js");

            var unzipper = new Unzipper();
            unzipper.UnzipBinaries();

            File.Exists(@"wwwroot\lib\jQuery.js").Should().BeFalse("only binaries should be unzipped (they are not unzipped until 'sync')");
        }

        [Test]
        public void ObsoleteFilesAreRemoved()
        {
            ExistingFiles("new.dll", "obsolete.dll.fordelete.txt");

            CreateZip("installing.zip", "new.dll");

            var unzipper = new Unzipper();
            unzipper.SyncNonBinaries();

            File.Exists("obsolete.dll.fordelete.txt").Should().BeFalse("ZipDeploy should have deleted obsolete.dll.fordelete.txt");
        }

        private void ExistingFiles(params string[] files)
        {
            foreach (var file in files)
                File.WriteAllText(file, $"existing content of {file}");
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
