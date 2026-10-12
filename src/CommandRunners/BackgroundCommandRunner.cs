
using System.Diagnostics;

namespace CodeCrafters.Shell.src.CommandRunners
{
    internal class BackgroundCommandRunner
    {
        private Dictionary<int, BackgroundJobInfo> backgroundProcesses = new();
        private HashSet<int> completedProcesses = new();

        public (int, BackgroundJobInfo) RunCommandInBackground(string command, List<string> args)
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
                        foreach(var jobInfo in backgroundProcesses.Values)
                        {
                            if(jobInfo.latest)
                            {
                                jobInfo.secondlatest = true;
                            }
                            else
                            {
                                jobInfo.secondlatest = false;
                            }

                            jobInfo.latest = false;
                        }

                        backgroundProcesses.Add(i, new BackgroundJobInfo(i, process.Id, string.Join(" ", [command, ..args]), "Running".PadRight(24), process));
                        break;
                    }
                }
            }

            return (i, backgroundProcesses[i]); // Return the process ID for background tracking
        }

        public List<BackgroundJobInfo> GetBackgroundProcesses()
        {
            foreach (int i in completedProcesses)
            {
                if (backgroundProcesses.ContainsKey(i))
                {
                    backgroundProcesses.Remove(i);
                }
            }
            completedProcesses.Clear();

            foreach (var jobInfo in backgroundProcesses.Values)
            {
                if (jobInfo.Process.HasExited)
                {
                    jobInfo.Status = "Done".PadRight(24);
                    completedProcesses.Add(jobInfo.Id);
                }
            }

            return backgroundProcesses.Values.ToList();
        }
    }
}
