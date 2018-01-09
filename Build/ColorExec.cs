using System.Diagnostics;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Build
{
    public class ColorExec : Task
    {
        [Required]
        public string FileName          { get; set; }

        public string Arguments         { get; set; }
        public string WorkingDirectory  { get; set; }

        public override bool Execute()
        {
            Log.LogMessage(MessageImportance.Normal, $"ColorExec WorkingDirectory='{WorkingDirectory}' FileName='{FileName}' Arguments='{Arguments}'");

            using (var process = new Process())
            {
                process.StartInfo.FileName = FileName;
                process.StartInfo.Arguments = Arguments;
                process.StartInfo.WorkingDirectory = WorkingDirectory;
                process.StartInfo.UseShellExecute = false;
                process.Start();
                process.WaitForExit();

                return process.ExitCode == 0;
            }
        }
    }
}
