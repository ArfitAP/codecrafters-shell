
namespace CodeCrafters.Shell.src
{
    internal class HistoryManager
    {
        private List<string> history = new List<string>();
        private List<string> notAppendedHistory = new List<string>();

        public void AddToHistory(string command)
        {
            history.Add(command);
            notAppendedHistory.Add(command);
        }

        public void PrintHistory(int n, OutputWriter outputWriter)
        {
            int start = Math.Max(0, history.Count - n);
            for (int i = start; i < history.Count; i++)
            {
                outputWriter.WriteOutput($"    {i + 1}  {history[i]}");
            }
        }

        public void ReadHistoryFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            var lines = File.ReadAllLines(filePath);
            for (int i = 0; i < lines.Length; i++)
            {
                if(!string.IsNullOrWhiteSpace(lines[i])) AddToHistory(lines[i]);
            }
        }

        public void WriteHistoryToFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            File.WriteAllLines(filePath, history);
        }

        public void AppendHistoryToFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            File.AppendAllLines(filePath, notAppendedHistory);
            notAppendedHistory.Clear();
        }
    }
}
