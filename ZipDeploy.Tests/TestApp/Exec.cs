using System.Diagnostics;
using FluentAssertions;

namespace ZipDeploy.Tests.TestApp
{
    public static class Exec
    {
        public static void Cmd(string workingDir, string program, string args)
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = program;
                process.StartInfo.Arguments = args;
                process.StartInfo.WorkingDirectory = workingDir;
                process.StartInfo.UseShellExecute = false;
                process.Start();
                process.WaitForExit();

                process.ExitCode.Should().Be(0);
            }
        }

        public static void DotnetPublish(string workingDir)
        {
            Cmd(workingDir, "dotnet.exe", "publish --self-contained --runtime win-x64");
        }
    }
}
