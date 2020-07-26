using System;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using FluentAssertions;
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
                var log = File.ReadAllText(Path.Combine(iisFolder, "nlog.log"));
                throw new Exception($"assertion failure with log:\n\n{log}\n\n", e);
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
                var globalModulesList = iisManager.GetApplicationHostConfiguration()
                    .GetSection("system.webServer/globalModules")
                    .GetCollection();

                var globalModules = globalModulesList.Select(m => m.Attributes["name"].Value.ToString()).ToList();
                globalModules.Should().Contain("AspNetCoreModule");
                globalModules.Should().Contain("AspNetCoreModuleV2");

                DeleteIisSite(iisManager);

                var pool = iisManager.ApplicationPools.Add(c_iisName);
                pool.ProcessModel.IdentityType = ProcessModelIdentityType.NetworkService;

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
