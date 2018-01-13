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

        public static void DeleteFolder(string folder)
        {
            while (Directory.Exists(folder))
                try { Directory.Delete(folder, true); }
                catch { }
        }

        public static void CopySource(string slnFolder, string srcCopyFolder, string projectName)
        {
            var src = Path.Combine(slnFolder, projectName);
            var copy = Path.Combine(srcCopyFolder, projectName);

            FileSystem.CopyDir(src, copy);
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
