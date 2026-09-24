using System;
using System.Collections.Generic;
using System.Text;

namespace CodeCrafters.Shell.src
{
    internal static class InputParser
    {
        public static List<string> ParseInput(string input)
        {
            List<string> args = new List<string>();
            StringBuilder currentArg = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (c == '\'')
                {
                    inQuotes = !inQuotes;
                }
                else if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (currentArg.Length > 0)
                    {
                        args.Add(currentArg.ToString());
                        currentArg.Clear();
                    }
                }
                else
                {
                    currentArg.Append(c);
                }
            }
            if (currentArg.Length > 0 || args.Count == 0)
            {
                args.Add(currentArg.ToString());
            }
            return args;
        }
    }
}
