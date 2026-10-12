namespace CodeCrafters.Shell.src.CommandRunners
{
    internal class BuiltInCommandRunner
    {
        public static string[] builtInCommands = ["exit", "echo", "type", "pwd", "cd", "complete"];

        private EnvironmentManager environmentManager;
        private Dictionary<string, string> completionDictionary;
        public BuiltInCommandRunner(EnvironmentManager environmentManager)
        {
            this.environmentManager = environmentManager;
            this.completionDictionary = new Dictionary<string, string>();
        }

        public bool TryExecuteBuiltInCommand(string command, List<string> args, OutputWriter outputWriter)
        {
            if (command == "pwd")
            {
                outputWriter.WriteOutput(Directory.GetCurrentDirectory());
                return true;
            }
            else if (command == "complete")
            {
                if (args.Contains("-C"))
                {
                    int index = args.IndexOf("-C");
                    completionDictionary[args[index + 2]] = args[index + 1];
                }
                else if (args.Contains("-r"))
                {
                    int index = args.IndexOf("-r");
                    if (completionDictionary.ContainsKey(args[index + 1]))
                    {
                        completionDictionary.Remove(args[index + 1]);
                    }
                }
                else if (args.Contains("-p"))
                {
                    int index = args.IndexOf("-p");
                    if (completionDictionary.ContainsKey(args[index + 1]))
                    {
                        outputWriter.WriteOutput($"complete -C '{completionDictionary[args[index + 1]]}' {args[index + 1]}");
                    }
                    else
                    {
                        outputWriter.WriteError($"complete: {args[index + 1]}: no completion specification");
                    }
                }
                return true;
            }
            else if (command == "echo")
            {
                outputWriter.WriteOutput(string.Join(" ", args.Skip(1)));
                return true;
            }
            else if (command == "cd")
            {
                if (args.Count < 2)
                {
                    return true;
                }
                string newDirectory = args[1];
                if (Path.IsPathRooted(newDirectory))
                {
                    newDirectory = Path.GetFullPath(newDirectory);
                }
                else if (newDirectory.StartsWith("~"))
                {
                    newDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), newDirectory[1..]);
                }
                else
                {
                    string tmpDirectory = Directory.GetCurrentDirectory();
                    string[] paths = newDirectory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    foreach (string path in paths)
                    {
                        if (path == "..")
                        {
                            tmpDirectory = Path.GetDirectoryName(tmpDirectory) ?? Directory.GetCurrentDirectory();
                        }
                        else if (path != ".")
                        {
                            tmpDirectory = Path.Combine(tmpDirectory, path);
                        }
                    }

                    newDirectory = tmpDirectory;
                }

                if (Directory.Exists(newDirectory))
                {
                    Directory.SetCurrentDirectory(newDirectory);
                }
                else
                {
                    outputWriter.WriteError($"cd: {newDirectory}: No such file or directory");
                }
                return true;
            }
            else if (command == "type")
            {
                if (args.Count < 2)
                {
                    return true;
                }
                string commandName = args[1];
                if (builtInCommands.Contains(commandName))
                {
                    outputWriter.WriteOutput($"{commandName} is a shell builtin");
                }
                else
                {
                    string fullPath = environmentManager.GetCommandPath(commandName);
                    if (!string.IsNullOrEmpty(fullPath))
                    {
                        outputWriter.WriteOutput($"{commandName} is {fullPath}");
                    }
                    else
                    {
                        outputWriter.WriteError($"{commandName}: not found");
                    }
                }
                return true;
            }

            return false;
        }

        public bool IsCompleterRegistered(string command)
        {
            return completionDictionary.ContainsKey(command);
        }

        public string GetCompleterScriptPath(string command)
        {
            return completionDictionary[command];
        }
    }
}
