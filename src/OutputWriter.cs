using System;
using System.Collections.Generic;
using System.Text;

namespace CodeCrafters.Shell.src
{
    internal class OutputWriter
    {
        public string? StandardOutputFileName { get; set; }
        public string? StandardErrorFileName { get; set; }

        public bool RedirectOutputToFile { get; set; }
        public bool RedirectErrorToFile { get; set; }

        public OutputWriter(ref List<string> args, string workingDirectory)
        {
            int stdRedirectIndex = args.Count;
            int errRedirectIndex = args.Count;
            if (args.Contains(">") || args.Contains("1>"))
            {
                stdRedirectIndex = args.IndexOf(">") != -1 ? args.IndexOf(">") : args.IndexOf("1>");
                if (stdRedirectIndex + 1 < args.Count)
                {
                    StandardOutputFileName = Path.Combine(workingDirectory, args[stdRedirectIndex + 1]);
                    RedirectOutputToFile = true;
                    File.Create(StandardOutputFileName).Close();
                }
            }
            if (args.Contains("2>"))
            {
                errRedirectIndex = args.IndexOf("2>");
                if (errRedirectIndex + 1 < args.Count)
                {
                    StandardErrorFileName = Path.Combine(workingDirectory, args[errRedirectIndex + 1]);
                    RedirectErrorToFile = true;
                    File.Create(StandardErrorFileName).Close();
                }
            }
            int minRedirectIndex = Math.Min(stdRedirectIndex, errRedirectIndex);
            args = args.Take(minRedirectIndex).ToList();          
        }

        public void WriteOutput(string output)
        {
            if (RedirectOutputToFile && !string.IsNullOrEmpty(StandardOutputFileName))
            {
                File.WriteAllText(StandardOutputFileName, output);
            }
            else
            {
                Console.WriteLine(output);
            }
        }

        public void WriteError(string output)
        {
            if (RedirectErrorToFile && !string.IsNullOrEmpty(StandardErrorFileName))
            {
                File.WriteAllText(StandardErrorFileName, output);
            }
            else
            {
                Console.WriteLine(output);
            }
        }
    }
}
