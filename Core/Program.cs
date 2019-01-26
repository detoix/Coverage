using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Core
{
    public class Program
    {
        //+++ b/AcadExtensions/Commands.cs => AcadExtensions/Commands.cs
        const string FileNamePattern = @"(?<=\+\+\+ b\/).+";
        //@@ -17,0 +18,2 @@ namespace AcadExtensions => -17,0 +18,2
        //@@ -20,0 +23 @@ namespace AcadExtensions => -20,0 +23
        const string LineNumbersPattern = @"(?<=\@\@ )-[0-9]+(,[0-9]+)? \+[0-9]+(,[0-9]+)?(?= \@\@)";
        //-29,0 +30,3 => 30,3
        //-29,0 +30 => 30
        const string NewLinesPattern = @"(?<=\+)[0-9]+(,[0-9]+)?";

        /// <param name="args">[0]path to git diff, [1] path to opencover sonarcube report, [2] path to save coverage report</param>
        public static void Main(string[] args)
        {
            if (args.Length != 3 || args.Take(2).Any(arg => !File.Exists(arg)))
            {
                throw new ArgumentException("Invalid input!");
            }

            var newLines = NewLinesFrom(args[0]);
            var coveredLines = CoveredLinesFrom(args[1]);
            newLines.IntersectWith(coveredLines.Keys);
            var coverableNewLines = newLines;
            var coverableNewLinesCount = (double)coverableNewLines.Count;
            var coveredNewLinesCount = (double)(coverableNewLines.Where(e => coveredLines.TryGetValue(e, out var covered) && covered).Count());
            var percentage = coveredNewLinesCount / coverableNewLinesCount;

            ReportFor(coverableNewLines, coveredLines, args[2]);
            Console.WriteLine($"Coverage summary: {coveredNewLinesCount} / {coverableNewLinesCount} = {percentage * 100}%");
        }

        static HashSet<(string FileName, int LineNumber)> NewLinesFrom(string path)
        {
            var result = new HashSet<(string, int)>();
            var visitedFile = string.Empty;

            foreach (var line in File.ReadLines(path))
            {
                if (Regex.Match(line, FileNamePattern) is Match filePath && filePath.Success)
                {
                    visitedFile = filePath.Value.EndsWith(".cs") ? filePath.Value : string.Empty;
                }
                if (!string.IsNullOrEmpty(visitedFile)
                    && Regex.Match(line, LineNumbersPattern) is Match lineNumbers && lineNumbers.Success
                    && Regex.Match(lineNumbers.Value, NewLinesPattern) is Match newLines && newLines.Success)
                {
                    var startAndCount = newLines.Value.Split(','); 
                    //30,3 => [30, 3]
                    if (startAndCount.Length == 2 
                        && int.TryParse(startAndCount[0], out var start) 
                        && int.TryParse(startAndCount[1], out var count))
                    {
                        for (int lineNumber = start; lineNumber < start + count; lineNumber++)
                        {
                            result.Add(
                                (visitedFile, lineNumber));
                        }
                    }
                    //30 => [30]
                    else if (startAndCount.Length == 1
                        && int.TryParse(startAndCount[0], out var singleNewLineNumber))
                    {
                        result.Add(
                            (visitedFile, singleNewLineNumber));
                    }
                    else
                    {
                        throw new ArgumentException("Regex went wrong!");
                    }
                }
            }

            return result;
        }

        static Dictionary<(string FileName, int LineNumber), bool> CoveredLinesFrom(string path)
        {
            var result = new Dictionary<(string, int), bool>();
            foreach (var file in XElement.Load(path).Elements())
            {
                var filePath = file.Attribute("path").Value;
                var commonStartSubstring = CommonRootOf(filePath, path);
                var visitedFile = filePath.Remove(0, commonStartSubstring.Length + 1).Replace("\\", "/");

                foreach (var line in file.Elements())
                {
                    if (int.TryParse(line.Attribute("lineNumber").Value, out var lineNumber)
                        && bool.TryParse(line.Attribute("covered").Value, out var covered))
                    {
                        result.Add(
                            (visitedFile, lineNumber), covered);
                    }
                }
            }

            return result;
        }

        static string CommonRootOf(string first, string second)
        {
            var firstPathParts = first.Split(Path.DirectorySeparatorChar);
            var secondPathParts = second.Split(Path.DirectorySeparatorChar);
            var commonRootParts = firstPathParts
                .Zip(secondPathParts, (a, b) => new { A = a, B = b })
                .TakeWhile(e => e.A == e.B)
                .Select(e => e.A)
                .ToArray();
            var result = string.Join(Path.DirectorySeparatorChar.ToString(), commonRootParts);
            return result;
        }

        static void ReportFor(
            IEnumerable<(string FileName, int LineNumber)> linesToCheck, 
            IDictionary<(string FileName, int LineNumber), bool> coveredLines,
            string coverageReportPath)
        {
            var sb = new StringBuilder();
            foreach (var line in linesToCheck)
            {
                var reportLine = $"{line.FileName}\t{line.LineNumber}\tcovered\t{coveredLines.TryGetValue(line, out var covered) && covered}";
                sb.AppendLine(reportLine);
                Console.WriteLine(reportLine);
            }

            File.Delete(coverageReportPath);
            File.WriteAllText(coverageReportPath, sb.ToString());
        }
    }
}