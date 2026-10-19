using System.Text;

namespace CodeCrafters.Shell.src
{
    internal static class InputParser
    {
        public static List<List<string>> ParseInput(string input, VariableManager variableManager)
        {
            List<List<string>> commandArgs = new List<List<string>>();

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
                else if(c == '\\' && (!inSingleQuotes || inDoubleQuotes))
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

            List<string> currentCommandArgs = new List<string>();
            foreach (var arg in args)
            {
                if (arg == "|")
                {
                    commandArgs.Add(currentCommandArgs);
                    currentCommandArgs = new List<string>();
                }
                else
                {
                    currentCommandArgs.Add(arg);
                }
            }
            commandArgs.Add(currentCommandArgs);

            for (int i = 0; i < commandArgs.Count; i++)
            {
                for (int j = 0; j < commandArgs[i].Count; j++)
                {
                    string arg = commandArgs[i][j];
                    if (arg.Contains("$"))
                    {
                        int index = arg.IndexOf('$');
                        string variableName = arg.Substring(index + 1);
                        string variableValue = variableManager.GetVariable(variableName);
                        
                        commandArgs[i][j] = $"{arg[..index]}{variableValue}";
                    }
                }
            }

            return commandArgs;
        }
    }
}
