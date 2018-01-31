using System.IO;
using System.Threading;

namespace ZipDeploy.Tests
{
    public static class FileSystem
    {
        public static void CopyDir(string sourceDir, string destDir)
        {
            DeleteFolder(destDir);

            Directory.CreateDirectory(destDir);

            foreach (var srcDir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(srcDir.Replace(sourceDir, destDir));

            foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
                File.Copy(file, file.Replace(sourceDir, destDir));
        }

        public static void CreateFolder(string file)
        {
            var folder = Path.GetDirectoryName(file);

            if (!string.IsNullOrWhiteSpace(folder) && !Directory.Exists(folder))
                Directory.CreateDirectory(folder);
        }

        public static void DeleteFolder(string folder)
        {
            var count = 3;

            while (Directory.Exists(folder))
                try { Directory.Delete(folder, true); }
                catch
                {
                    Thread.Sleep(0);

                    if (count-- == 0)
                        throw;
                }
        }

        public static void CopySource(string slnFolder, string srcCopyFolder, string projectName)
        {
            var src = Path.Combine(slnFolder, projectName);
            var copy = Path.Combine(srcCopyFolder, projectName);

            CopyDir(src, copy);
        }

        public static void ReplaceText(string folder, string file, string find, string replace)
        {
            var path = Path.Combine(folder, file);
            var content = File.ReadAllText(path);
            content = content.Replace(find, replace);
            File.WriteAllText(path, content);
        }
    }
}
