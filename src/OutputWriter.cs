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
    }
}
