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

            while (!File.Exists(Path.Combine(slnPath, "ZipDeploy.sln")))
                slnPath = Directory.GetParent(slnPath).FullName;

            return slnPath;
        }

        public static string GetOutputFolder()
        {
            var outputFolder = Path.Combine(GetSlnFolder(), "_output");
            Directory.CreateDirectory(outputFolder);
            return outputFolder;
        }

        public class IsSlowAttribute : CategoryAttribute
        {
            public IsSlowAttribute() : base("Slow")
            {
            }
        }
    }
}
