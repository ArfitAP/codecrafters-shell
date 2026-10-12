
using System.Diagnostics;

namespace CodeCrafters.Shell.src.CommandRunners
{
    internal class BackgroundJobInfo
    {
        public BackgroundJobInfo(int id, int pid, string command, string status, Process process)
        {
            Id = id;
            Pid = pid;
            Command = command;
            Status = status;
            Process = process;
        }

        public int Id { get; }
        public int Pid { get; }
        public string Command { get; }
        public string Status { get; set; }
        public Process Process { get; }
        public bool latest { get; set; } = true;
        public bool secondlatest { get; set; } = false;
    }
}
