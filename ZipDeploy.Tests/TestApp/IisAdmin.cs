using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Web.Administration;

namespace ZipDeploy.Tests.TestApp
{
    public static class IisAdmin
    {
        private const string    c_iisName = "ZipDeployTestApp";
        private const int       c_iisPort = 8099;

        public static void ShowLogOnFail(string iisFolder, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                var logsFolder = Path.Combine(iisFolder, "logs");
                var logFiles = new Dictionary<string, string>();

                foreach (var logFile in Directory.GetFiles(logsFolder))
                    logFiles.Add(logFile, File.ReadAllText(logFile));

                var log = logFiles.Any()
                    ? string.Join("\n\n", logFiles.Select(lf => $"{lf.Key}:\n{lf.Value}"))
                    : $"No log files found in {logsFolder}";

                throw new Exception($"assertion failure with log:\n\n{log}\n\n", e);
            }
        }

        public static void VerifyModuleInstalled(string moduleName, string downloadUrl)
        {
            using (var iisManager = new ServerManager())
            {
                var globalModulesList = iisManager.GetApplicationHostConfiguration()
                .GetSection("system.webServer/globalModules")
                .GetCollection();

                var globalModules = globalModulesList.Select(m => m.Attributes["name"].Value.ToString()).ToList();

                if (globalModules.Contains(moduleName))
                    return;

                Test.WriteProgress($"Downloading {downloadUrl} for module {moduleName}");
                var filename = Path.GetFileName(downloadUrl);

                if (!File.Exists(filename))
                    new WebClient().DownloadFile(downloadUrl, filename);

                Test.WriteProgress($"Executing {filename} /install /quiet /norestart");
                Exec.Cmd("", filename, "/install /quiet /norestart");
                Exec.Cmd("", "iisreset", "");
            }
        }

        public static void CreateIisSite(string iisFolder)
        {
            var sec = new DirectorySecurity(iisFolder, AccessControlSections.Access);
            var everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            var rights = FileSystemRights.FullControl;
            var inheritFlags = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
            var propFlags = PropagationFlags.InheritOnly;
            sec.AddAccessRule(new FileSystemAccessRule(everyone, rights, inheritFlags, propFlags, AccessControlType.Allow));
            new DirectoryInfo(iisFolder).SetAccessControl(sec);

            using (var iisManager = new ServerManager())
            {
                DeleteIisSite(iisManager);

                var pool = iisManager.ApplicationPools.Add(c_iisName);
                pool.ProcessModel.IdentityType = ProcessModelIdentityType.NetworkService;
                pool.Recycling.DisallowOverlappingRotation = true;

                var site = iisManager.Sites.Add("ZipDeployTestApp", iisFolder, c_iisPort);
                site.ApplicationDefaults.ApplicationPoolName = pool.Name;

                iisManager.CommitChanges();

                Test.WriteProgress($"Created IIS site {c_iisName}:{c_iisPort} in {iisFolder}");
            }
        }

        public static void DeleteIisSite()
        {
            using (var iisManager = new ServerManager())
            {
                var siteCount = iisManager.Sites.Count(s => s.Name == c_iisName);

                if (siteCount > 0)
                    DeleteIisSite(iisManager);

                var poolCount = iisManager.ApplicationPools.Count(p => p.Name == c_iisName);

                if (poolCount > 0)
                    DeleteIisPool(iisManager);
            }
        }

        private static void DeleteIisSite(ServerManager iisManager)
        {
            var site = iisManager.Sites.SingleOrDefault(s => s.Name == c_iisName);

            if (site == null)
                return;

            iisManager.Sites.Remove(site);
            iisManager.CommitChanges();
            Test.WriteProgress($"Removed IIS site {c_iisName}");
        }

        private static void DeleteIisPool(ServerManager iisManager)
        {
            var pool = iisManager.ApplicationPools.SingleOrDefault(s => s.Name == c_iisName);

            if (pool == null)
                return;

            iisManager.ApplicationPools.Remove(pool);
            iisManager.CommitChanges();
            Test.WriteProgress($"Removed IIS pool {c_iisName}");
        }
    }
}
