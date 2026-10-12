
namespace CodeCrafters.Shell.src.CommandRunners
{
    internal record BackgroundJobInfo(int Id, int Pid, string command, string status)
    {
        public bool latest { get; set; } = true;
        public bool secondlatest { get; set; } = false;
    }
}
