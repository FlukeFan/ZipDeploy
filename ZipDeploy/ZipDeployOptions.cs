using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ZipDeploy
{
    public class ZipDeployOptions
    {
        public const string DefaultNewPackageFileName       = "publish.zip";
        public const string DefaultLegacyTempFileName       = "installing.zip";
        public const string DefaultDeployedPackageFileName  = "deployed.zip";
        public const string DefaultHashesFileName           = "zipDeployFileHashes.txt";

        public ZipDeployOptions() : this(new LoggerFactory()) { }

        public ZipDeployOptions(ILoggerFactory loggerFactory)
        {
            ServiceCollection.AddSingleton(loggerFactory);
            ServiceCollection.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
 
            ServiceCollection.AddSingleton(this);
            ServiceCollection.AddSingleton<IDetectPackage, DetectPackage>();
            ServiceCollection.AddSingleton<ITriggerRestart, AspNetRestart>();
            ServiceCollection.AddSingleton<IUnzipper, Unzipper>();
        }

        public IServiceCollection   ServiceCollection       { get; protected set; } = new ServiceCollection();
        public IList<string>        PathsToIgnore           { get; protected set; } = new List<string>();
        public Func<string, bool>   IsBinary                { get; protected set; } = DefaultIsBinary;
        public Func<string, string> ProcessWebConfig        { get; protected set; } = DefaultProcessWebConfig;

        public string               NewPackageFileName      { get; set; } = DefaultNewPackageFileName;
        public string               LegacyTempFileName      { get; set; } = DefaultLegacyTempFileName;
        public string               DeployedPackageFileName { get; set; } = DefaultDeployedPackageFileName;
        public string               HashesFileName          { get; set; } = DefaultHashesFileName;

        /// <summary>Default implementation is to return the web.config content unchanged</summary>
        public static string DefaultProcessWebConfig(string beforeConfig)
        {
            return beforeConfig;
        }

        /// <summary>Default implementation is to return true if the extension is .dll or .exe</summary>
        public static bool DefaultIsBinary(string file)
        {
            var extension = Path.GetExtension(file)?.ToLower();
            return new string[] { ".dll", ".exe" }.Contains(extension);
        }

        /// <summary>Specify any paths to ignore (e.g., "log.txt", or "logs/", or "uploads\today")</summary>
        public ZipDeployOptions IgnorePathStarting(string path)
        {
            PathsToIgnore.Add(path);
            return this;
        }

        /// <summary>Specify custom function to determine binary files (which are processed before restart of web application)</summary>
        public ZipDeployOptions UseIsBinary(Func<string, bool> isBinary)
        {
            IsBinary = isBinary;
            return this;
        }

        /// <summary>Specify custom function to transform the web.config.</summary>
        public ZipDeployOptions UseProcessWebConfig(Func<string, string> processWebConfig)
        {
            ProcessWebConfig = processWebConfig;
            return this;
        }

        internal void UsingArchive(Action<ZipArchive> action)
        {
            var packageName = NewPackageFileName;
            using (var zipArchive = ZipFile.OpenRead(packageName))
                action(zipArchive);
        }
    }
}
