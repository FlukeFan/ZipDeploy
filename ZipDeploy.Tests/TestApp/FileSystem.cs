using System.IO;

namespace ZipDeploy.Tests.TestApp
{
    public static class FileSystem
    {
        public static void CopyDir(string sourceDir, string destDir)
        {
            if (Directory.Exists(destDir))
                Directory.Delete(destDir, true);

            Directory.CreateDirectory(destDir);

            foreach (var srcDir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(srcDir.Replace(sourceDir, destDir));

            foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
                File.Copy(file, file.Replace(sourceDir, destDir));
        }
    }
}
