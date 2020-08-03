using System.IO;

namespace ZipDeploy
{
    public interface IQueryPackageName
    {
        string FindPackageName();
    }

    public class QueryPackageName : IQueryPackageName
    {
        public string FindPackageName()
        {
            if (File.Exists(ZipDeployOptions.DefaultNewPackageFileName))
                return ZipDeployOptions.DefaultNewPackageFileName;

            return null;
        }
    }
}
