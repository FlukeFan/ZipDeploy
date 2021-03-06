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

        public string               NewPackageFileName      { get; set; } = DefaultNewPackageFileName;
        public string               DeployedPackageFileName { get; set; } = DefaultDeployedPackageFileName;
        public string               HashesFileName          { get; set; } = DefaultHashesFileName;
        public TimeSpan             StartupPublishDelay     { get; set; } = TimeSpan.FromSeconds(3);
        public TimeSpan             ErrorRetryPeriod        { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>Specify any paths to ignore (e.g., "log.txt", or "logs/", or "uploads\today")</summary>
        public ZipDeployOptions IgnorePathStarting(string path)
        {
            PathsToIgnore.Add(path);
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
