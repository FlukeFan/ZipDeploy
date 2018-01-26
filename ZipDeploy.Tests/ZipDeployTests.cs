using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace ZipDeploy.Tests
{
    [TestFixture]
    public class ZipDeployTests
    {
        private string              _originalCurrentDirectory;
        private string              _filesFolder;
        private IList<ZipDeploy>    _zdInstances;

        [SetUp]
        public void SetUp()
        {
            _filesFolder = Path.Combine(Test.GetOutputFolder(), "testFiles");
            FileSystem.DeleteFolder(_filesFolder);
            Directory.CreateDirectory(_filesFolder);
            _originalCurrentDirectory = Environment.CurrentDirectory;
            Environment.CurrentDirectory = _filesFolder;

            _zdInstances = new List<ZipDeploy>();
        }

        [TearDown]
        public void TearDown()
        {
            Environment.CurrentDirectory = _originalCurrentDirectory;

            foreach (var zdInstance in _zdInstances)
                using (zdInstance) { }
        }

        [Test]
        public void BinariesAreRenamed()
        {
            ExistingFiles("binary.dll");

            UploadPublishZip("binary.dll");

            var zipDeploy = NewZipDeploy();

            MakeRequest(zipDeploy);

            File.ReadAllText("binary.dll").Should().Be("zipped content of binary.dll");
            File.ReadAllText("binary.dll.fordelete.txt").Should().Be("existing content of binary.dll");
        }

        [Test]
        public void ObsoleteFilesAreRemoved()
        {
            ExistingFiles("new.dll", "obsolete.dll.fordelete.txt");

            InstallingZip("new.dll");

            var zipDeploy = NewZipDeploy();

            File.Exists("obsolete.dll.fordelete.txt").Should().BeFalse("ZipDeploy should have deleted obsolete.dll.fordelete.txt");
        }

        private void ExistingFiles(params string[] files)
        {
            foreach (var file in files)
                File.WriteAllText(file, $"existing content of {file}");
        }

        private ZipDeploy NewZipDeploy(Action<ZipDeployOptions> configureOptions = null)
        {
            RequestDelegate next = httpContext => Task.CompletedTask;
            var options = new ZipDeployOptions();
            configureOptions?.Invoke(options);
            var zipDeploy = new ZipDeploy(next, new FakeLogger(), options);
            _zdInstances.Add(zipDeploy);
            return zipDeploy;
        }

        private void MakeRequest(ZipDeploy zipDeploy)
        {
            zipDeploy.Invoke(new FakeContext()).GetAwaiter().GetResult();
        }

        private void UploadPublishZip(params string[] files)
        {
            UploadZip("publish.zip", files);
        }

        private void InstallingZip(params string[] files)
        {
            UploadZip("installing.zip", files);
        }

        private void UploadZip(string zipFileName, params string[] files)
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

        private class FakeLogger : ILogger<ZipDeploy>, IDisposable
        {
            public IDisposable BeginScope<TState>(TState state)
            {
                return this;
            }

            public void Dispose()
            {
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                Console.WriteLine(formatter(state, exception));
            }
        }

        private class FakeContext : HttpContext
        {
            public override IFeatureCollection Features => throw new NotImplementedException();

            public override HttpRequest Request => throw new NotImplementedException();

            public override HttpResponse Response => throw new NotImplementedException();

            public override ConnectionInfo Connection => throw new NotImplementedException();

            public override WebSocketManager WebSockets => throw new NotImplementedException();

            [Obsolete]
            public override AuthenticationManager Authentication => throw new NotImplementedException();

            public override ClaimsPrincipal User { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override IDictionary<object, object> Items { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override IServiceProvider RequestServices { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override CancellationToken RequestAborted { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override string TraceIdentifier { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override ISession Session { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override void Abort()
            {
                throw new NotImplementedException();
            }
        }
    }
}
