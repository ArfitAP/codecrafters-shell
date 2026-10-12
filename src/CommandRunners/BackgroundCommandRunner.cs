using System;
using System.Collections.Generic;
using System.Text;

namespace CodeCrafters.Shell.src.CommandRunners
{
    internal class BackgroundCommandRunner
    {
        private Dictionary<int, System.Diagnostics.Process> backgroundProcesses = new();

        public (int, int) RunCommandInBackground(string command, List<string> args)
        {
            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = command,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var arg in args)
            {
                processStartInfo.ArgumentList.Add(arg);
            }

            var process = System.Diagnostics.Process.Start(processStartInfo);

            int i = 1;
            if (process != null)
            {
                while(true)
                {
                    if(backgroundProcesses.ContainsKey(i))
                    {
                        i++;
                    }
                    else
                    {
                        backgroundProcesses.Add(i, process);
                        break;
                    }
                }
            }

            return (i, process!.Id); // Return the process ID for background tracking
        }
    }
}
