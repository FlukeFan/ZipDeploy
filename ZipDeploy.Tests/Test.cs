using System.IO;
using NUnit.Framework;

namespace ZipDeploy.Tests
{
    public static class Test
    {
        public static void WriteProgress(string line)
        {
            TestContext.Progress.WriteLine(line);
        }

        public static string GetSlnFolder()
        {
            var slnPath = Path.GetFullPath(".");

            while (slnPath.Contains("ZipDeploy.Tests"))
                slnPath = Directory.GetParent(slnPath).FullName;

            return slnPath;
        }

        public static string GetOutputFolder()
        {
            var outputFolder = Path.Combine(GetSlnFolder(), "_output");
            Directory.CreateDirectory(outputFolder);
            return outputFolder;
        }
    }
}
