
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
                        var maxOrderNumber = backgroundProcesses.Values.Any() ? backgroundProcesses.Values.Max(j => j.OrderNumber) : 0;

                        backgroundProcesses.Add(i, new BackgroundJobInfo(i, process.Id, maxOrderNumber + 1, string.Join(" ", [command, ..args]), "Running".PadRight(24), process));
                        break;
                    }
                }
            }

            return (i, backgroundProcesses[i]); // Return the process ID for background tracking
        }

        public List<BackgroundJobInfo> GetBackgroundProcesses()
        {
            Reap();

            return backgroundProcesses.Values.ToList();
        }

        public void Reap(bool printDoneJobs = false)
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

                    if (printDoneJobs)
                    {
                        char recentnessChar = ' ';
                        if (jobInfo.OrderNumber == GetMaxOrderNumber())
                        {
                            recentnessChar = '+';
                        }
                        else if (jobInfo.OrderNumber == GetSecondMaxOrderNumber())
                        {
                            recentnessChar = '-';
                        }
                        Console.WriteLine($"[{jobInfo.Id}]{recentnessChar}  {jobInfo.Status} {jobInfo.Command}");
                    }
                }
            }
        }

        public int GetMaxOrderNumber()
        {
            return backgroundProcesses.Values.Any() ? backgroundProcesses.Values.Max(j => j.OrderNumber) : 0;
        }

        public int GetSecondMaxOrderNumber()
        {
            var orderedValues = backgroundProcesses.Values.OrderByDescending(j => j.OrderNumber).ToList();
            return orderedValues.Count > 1 ? orderedValues[1].OrderNumber : 0;
        }
    }
}
