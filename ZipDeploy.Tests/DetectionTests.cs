using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using ZipDeploy.Tests.TestApp;

namespace ZipDeploy.Tests
{
    [TestFixture]
    public class DetectionTests
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
        public async Task WhenPackageDeplyed_PackageIsDetected()
        {
            var detected = false;
            using (var detector = NewDetectPackage())
            {
                detector.PackageDetectedAsync += () => { detected = true; return Task.CompletedTask; };
                await detector.StartedAsync(hadStartupErrors: false);

                detected.Should().Be(false);

                File.WriteAllBytes(ZipDeployOptions.DefaultNewPackageFileName, new byte[0]);

                Wait.For(TimeSpan.FromSeconds(1), () => detected.Should().Be(true));
            }
        }

        private DetectPackage NewDetectPackage(Action<ZipDeployOptions> configure = null)
        {
            var options = new ZipDeployOptions();
            configure?.Invoke(options);
            return new DetectPackage(new LoggerFactory().CreateLogger<DetectPackage>(), options);
        }
    }
}
