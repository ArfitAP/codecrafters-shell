using ReadLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace CodeCrafters.Shell.src
{
    internal class AutoCompletionHandler : IAutoCompleteHandler
    {
        private WorkingDirectoryManager workingDirectoryManager;

        // characters to start completion from
        public char[] Separators { get; set; } = new char[] { ' ', '.', '/' };

        private List<string> builtInCommands = ["exit", "echo"];
        private string lastInput = string.Empty;
        private List<string> lastSuggestions = new List<string>();

        public AutoCompletionHandler(WorkingDirectoryManager workingDirectoryManager)
        {
            this.workingDirectoryManager = workingDirectoryManager;
        }

        // text - The current text entered in the console
        // index - The index of the terminal cursor within {text}
        public string[] GetSuggestions(string text, int index)
        {
            string[] cmdParts = text.Split(' ');
            if(cmdParts.Length == 1)
            {
                if (lastInput != string.Empty && text == lastInput && lastSuggestions.Count > 0)
                {
                    lastSuggestions.Sort();
                    Console.WriteLine();
                    Console.WriteLine(string.Join("  ", lastSuggestions));
                    Console.Write($"$ {text}");
                    return [];
                }

                var matches = builtInCommands
                    .Where(command => command.StartsWith(text, StringComparison.Ordinal))
                    .Select(command => command + " ")
                    .ToArray();

                if (matches.Length == 0)
                {
                    string? path = Environment.GetEnvironmentVariable("PATH");
                    if (path != null)
                    {
                        foreach (string entry in path.Split(Path.PathSeparator))
                        {
                            if (!Directory.Exists(entry))
                            {
                                continue;
                            }

                            foreach (string fullFileName in Directory.GetFiles(entry))
                            {
                                string commandName = Path.GetFileName(fullFileName);
                                if (commandName.StartsWith(text, StringComparison.Ordinal))
                                {
                                    matches = matches.Append(commandName + " ").ToArray();
                                }
                            }
                        }
                    }

                    lastSuggestions = matches.ToList();

                    if (matches.Count() > 1)
                    {
                        StringBuilder sb = new();
                        int currLength = text.Length;
                        int maxLength = matches.Min(m => m.Length);
                        for (int i = currLength; i < maxLength; i++)
                        {
                            char currentChar = matches[0][i];
                            if (matches.All(m => m[i] == currentChar))
                            {
                                sb.Append(currentChar);
                            }
                            else
                            {
                                break;
                            }
                        }

                        if (sb.Length > 0)
                        {
                            lastInput = text + sb.ToString();
                            return new string[] { lastInput };
                        }

                        Console.Write("\x07");
                        lastInput = text;
                        return [];
                    }
                    else if (matches.Count() == 1)
                    {
                        lastInput = string.Empty;
                        lastSuggestions = [];
                        return matches;
                    }

                    // If no matches found, print a bell sound to indicate no suggestions are available
                    Console.Write("\x07");
                }

                return matches;
            }
            else
            {
                var lastPart = cmdParts.Last();
                var nestedpaths = lastPart.Split('/');
                var lastNestedPart = nestedpaths.Last();
                var serachPath = Path.Combine([workingDirectoryManager.WorkingDirectory, ..nestedpaths.Take(nestedpaths.Length - 1).ToArray()]);

                var allfiles = Directory.GetFiles(serachPath);

                var matches = allfiles
                    .Where(file => Path.GetFileName(file).StartsWith(lastNestedPart, StringComparison.Ordinal))
                    .Select(file => Path.GetFileName(file) + " ")
                    .ToArray();

                return matches;
            }
        }

        public void ResetSuggestions()
        {
            lastInput = string.Empty;
            lastSuggestions.Clear();
        }
    }
}
