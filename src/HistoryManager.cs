
namespace CodeCrafters.Shell.src
{
    internal class HistoryManager
    {
        private List<string> history = new List<string>();

        public void AddToHistory(string command)
        {
            history.Add(command);
        }

        public void PrintHistory(int n, OutputWriter outputWriter)
        {
            int start = Math.Max(0, history.Count - n);
            for (int i = start; i < history.Count; i++)
            {
                outputWriter.WriteOutput($"    {i + 1}  {history[i]}");
            }
        }
    }
}
