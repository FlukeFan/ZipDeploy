﻿using System;
using System.Collections.Generic;
using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace ZipDeploy
{
    public class ZipDeployOptions
    {
        public const string DefaultNewPackageFileName       = "publish.zip";
        public const string DefaultDeployedPackageFileName  = "deployed.zip";
        public const string DefaultHashesFileName           = "zipDeployFileHashes.txt";

        public IList<string>        PathsToIgnore           { get; protected set; } = new List<string>();
        public Func<string, string> ProcessWebConfig        { get; protected set; } = DefaultProcessWebConfig;

        public string               NewPackageFileName      { get; set; } = DefaultNewPackageFileName;
        public string               DeployedPackageFileName { get; set; } = DefaultDeployedPackageFileName;
        public string               HashesFileName          { get; set; } = DefaultHashesFileName;
        public TimeSpan             StartupPublishDelay     { get; set; } = TimeSpan.FromSeconds(3);

        /// <summary>Default implementation is to return the web.config content unchanged</summary>
        public static string DefaultProcessWebConfig(string beforeConfig)
        {
            return beforeConfig;
        }

        /// <summary>Specify any paths to ignore (e.g., "log.txt", or "logs/", or "uploads\today")</summary>
        public ZipDeployOptions IgnorePathStarting(string path)
        {
            PathsToIgnore.Add(path);
            return this;
        }

        /// <summary>Specify custom function to transform the web.config.</summary>
        public ZipDeployOptions UseProcessWebConfig(Func<string, string> processWebConfig)
        {
            ProcessWebConfig = processWebConfig;
            return this;
        }

        internal void UsingArchive(ILogger logger, Action<ZipArchive> action)
        {
            logger.LogDebug($"Opening {NewPackageFileName}");

            using (var zipArchive = ZipFile.OpenRead(NewPackageFileName))
                action(zipArchive);
        }
    }
}
