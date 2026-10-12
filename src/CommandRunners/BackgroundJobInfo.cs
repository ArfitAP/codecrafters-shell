
using System.Diagnostics;

namespace CodeCrafters.Shell.src.CommandRunners
{
    internal class BackgroundJobInfo
    {
        public BackgroundJobInfo(int id, int pid, int orderNumber, string command, string status, Process process)
        {
            Id = id;
            Pid = pid;
            OrderNumber = orderNumber;
            Command = command;
            Status = status;
            Process = process;
        }

        public int Id { get; }
        public int Pid { get; }
        public int OrderNumber { get; }
        public string Command { get; }
        public string Status { get; set; }
        public Process Process { get; }
    }
}
