
namespace CodeCrafters.Shell.src
{
    internal class HistoryManager
    {
        private List<string> history = new List<string>();

        public void AddToHistory(string command)
        {
            history.Add(command);
        }

        public void PrintHistory(OutputWriter outputWriter)
        {
            for (int i = 0; i < history.Count; i++)
            {
                outputWriter.WriteOutput($"    {i + 1}  {history[i]}");
            }
        }
    }
}
