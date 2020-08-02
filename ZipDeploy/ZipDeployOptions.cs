using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ZipDeploy
{
    public class ZipDeployOptions
    {
        public const string DefaultWatchFilter          = "publish.zip";
        public const string DefaultNewZipFileName       = "publish.zip";
        public const string DefaultTempZipFileName      = "installing.zip";
        public const string DefaultDeployedZipFileName  = "deployed.zip";
        public const string DefaultHashesFileName       = "zipDeployFileHashes.txt";

        public string               IisUrl              { get; protected set; }
        public string               WatchFilter         { get; protected set; } = DefaultWatchFilter;
        public string               NewZipFileName      { get; protected set; } = DefaultNewZipFileName;
        public string               TempZipFileName     { get; protected set; } = DefaultTempZipFileName;
        public string               DeployedZipFileName { get; protected set; } = DefaultDeployedZipFileName;
        public string               HashesFileName      { get; protected set; } = DefaultHashesFileName;
        public IList<string>        PathsToIgnore       { get; protected set; } = new List<string>();
        public Func<string, bool>   IsBinary            { get; protected set; } = DefaultIsBinary;
        public Func<string, string> ProcessWebConfig    { get; protected set; } = DefaultProcessWebConfig;

        public IWatchForPackage WatchForPackage;

        public IWatchForPackage NewWatchForPackage()
        {
            return new WatchForPackage(NewZipFileName);
        }

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

        /// <summary>Specify the IIS (not Kestrel) URL that is used to make a request to the application</summary>
        public ZipDeployOptions UseIisUrl(string iisUrl)
        {
            IisUrl = iisUrl;
            return this;
        }

        /// <summary>Specify any paths to ignore (e.g., "log.txt", or "logs/", or "uploads\today")</summary>
        public ZipDeployOptions IgnorePathStarting(string path)
        {
            PathsToIgnore.Add(path);
            return this;
        }

        /// <summary>Specify the name of the new uploaded zip file - defaults to "publish.zip"</summary>
        public ZipDeployOptions UseNewZipFileName(string newZipFileName)
        {
            NewZipFileName = newZipFileName;
            return this;
        }

        /// <summary>Specify the name of the new temporary zip file used between restarts - defaults to "installing.zip"</summary>
        public ZipDeployOptions UseTempZipFileName(string tempZipFileName)
        {
            TempZipFileName = tempZipFileName;
            return this;
        }

        /// <summary>Specify the name of the deployed zip file after syncing remaining contents - defaults to "deployed.zip"</summary>
        public ZipDeployOptions UseDeployedZipFileName(string deployedZipFileName)
        {
            DeployedZipFileName = deployedZipFileName;
            return this;
        }

        /// <summary>Specify the name of the file used to store hashes of the unzipped files - defaults to "zipDeployFileHashes.txt"</summary>
        public ZipDeployOptions UseHashesFileName(string hashesFileName)
        {
            HashesFileName = hashesFileName;
            return this;
        }

        /// <summary>Specify custom function to determine binary files (which are processed before restart of web application)</summary>
        public ZipDeployOptions UseIsBinary(Func<string, bool> isBinary)
        {
            IsBinary = isBinary;
            return this;
        }

        /// <summary>Specify custom processing for the web.config content</summary>
        public ZipDeployOptions UseProcessWebConfig(Func<string, string> processWebConfig)
        {
            ProcessWebConfig = processWebConfig;
            return this;
        }
    }
}
