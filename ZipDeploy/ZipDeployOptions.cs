﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
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
        public string               ProcessLockName         { get; set; }
        public TimeSpan?            ProcessLockTimeout      { get; set; }
        public bool                 RestartOnStartupError   { get; set; } = true;

        /// <summary>Specify any paths to ignore (e.g., "log.txt", or "logs/", or "uploads\today")</summary>
        public ZipDeployOptions IgnorePathStarting(string path)
        {
            PathsToIgnore.Add(path);
            return this;
        }

        /// <summary>Specify a named synchronization primitive to use (will be prefixed with Global\ and used to prevent multiple processes running at the same time).</summary>
        public ZipDeployOptions UsingProcessLock(string name, TimeSpan? timeout = null)
        {
            ProcessLockName = name;
            ProcessLockTimeout = timeout;
            return this;
        }

        internal async Task UsingArchiveAsync(ILogger logger, Func<ZipArchive, Task> actionAsync)
        {
            if (File.Exists(NewPackageFileName))
            {
                logger.LogDebug($"Opening {NewPackageFileName}");

                using (var zipArchive = ZipFile.OpenRead(NewPackageFileName))
                    await actionAsync(zipArchive);
            }
            else
            {
                logger.LogInformation($"{NewPackageFileName} not found");
                await actionAsync(null);
            }
        }
    }
}
