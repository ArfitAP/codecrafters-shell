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
            bool inSingleQuotes = false;
            bool inDoubleQuotes = false;
            bool escapeNextChar = false;
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (escapeNextChar)
                {
                    currentArg.Append(c);
                    escapeNextChar = false;
                }
                else if (c == '\'' && !inDoubleQuotes)
                {
                    inSingleQuotes = !inSingleQuotes;
                }
                else if (c == '"' && !inSingleQuotes)
                {
                    inDoubleQuotes = !inDoubleQuotes;
                }
                else if(c == '\\' && !inSingleQuotes && !inDoubleQuotes)
                {
                    escapeNextChar = true;
                }
                else if (char.IsWhiteSpace(c) && !inSingleQuotes && !inDoubleQuotes)
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
