using ReadLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace CodeCrafters.Shell.src
{
    internal class AutoCompletionHandler : IAutoCompleteHandler
    {
        // characters to start completion from
        public char[] Separators { get; set; } = new char[] { ' ', '.', '/' };

        private List<string> builtInCommands = ["exit", "echo"];

        // text - The current text entered in the console
        // index - The index of the terminal cursor within {text}
        public string[] GetSuggestions(string text, int index)
        {
            return builtInCommands
                .Where(command => command.StartsWith(text, StringComparison.Ordinal))
                .Select(command => command + " ")
                .ToArray();
        }
    }
}
