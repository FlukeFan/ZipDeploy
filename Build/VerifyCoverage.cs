using System;
using System.Collections.Generic;
using System.Xml;

namespace Build
{
    public class VerifyCoverage : Command
    {
        public override void Execute(Stack<string> args)
        {
            if (args.Count != 2)
                throw new Exception($"usage: dotnet Build.dll VerifyCoverage <target%> <coverage file>");

            var targetPercentage = double.Parse(args.Pop());
            var coverageFile = args.Pop();

            var doc = new XmlDocument();
            doc.Load(coverageFile);

            var lineCoverageNode = doc.SelectSingleNode("/CoverageReport/Summary/Linecoverage");

            if (lineCoverageNode == null || string.IsNullOrWhiteSpace(lineCoverageNode.InnerText))
                throw new Exception($"Could not find line coverage in {coverageFile}.  Are you missing <DebugType>Full</DebugType> for a .Net Core test suite?");

            var actualLineCoverage = double.Parse(lineCoverageNode.InnerText.Replace("%", ""));

            if (actualLineCoverage < targetPercentage)
                throw new Exception($"Expected at least {targetPercentage}% coverage, only got {actualLineCoverage}% coverage");

            UsingConsoleColor(ConsoleColor.Green, () => Console.WriteLine($"Coverage of {actualLineCoverage}% is greater than target of {targetPercentage}% "));
        }
    }
}
