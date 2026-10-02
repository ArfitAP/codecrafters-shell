using CodeCrafters.Shell.src;
using System.Diagnostics;

class Program
{
    static void Main()
    {
        WorkingDirectoryManager workingDirectoryManager = new WorkingDirectoryManager();
        workingDirectoryManager.WorkingDirectory = Directory.GetCurrentDirectory();

        List<string> builtInCommands = ["exit", "echo", "type", "pwd", "cd"];

        ReadLine.ReadLine.Context.AutoCompletionHandler = new AutoCompletionHandler(workingDirectoryManager);

        while (true)
        {
            string? input = ReadLine.ReadLine.Read("$ ");
            List<string> args = InputParser.ParseInput(input!);
            string command = args[0];

            OutputWriter outputWriter = new OutputWriter(ref args, workingDirectoryManager.WorkingDirectory);

            if (command == "exit")
            {
                break;
            }
            else if(command == "pwd")
            {
                outputWriter.WriteOutput(workingDirectoryManager.WorkingDirectory);
            }
            else if(command == "echo")
            {
                outputWriter.WriteOutput(string.Join(" ", args.Skip(1)));
                continue;
            }
            else if (command == "cd")
            {
                if (args.Count < 2)
                {
                    continue;
                }
                string newDirectory = args[1];
                if(Path.IsPathRooted(newDirectory))
                {
                    newDirectory = Path.GetFullPath(newDirectory);
                }
                else if (newDirectory.StartsWith("~"))
                {
                    newDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), newDirectory[1..]);
                }
                else
                {
                    string tmpDirectory = workingDirectoryManager.WorkingDirectory;
                    string[] paths = newDirectory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    foreach (string path in paths)
                    {
                        if (path == "..")
                        {
                            tmpDirectory = Path.GetDirectoryName(tmpDirectory) ?? workingDirectoryManager.WorkingDirectory;
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
                    workingDirectoryManager.WorkingDirectory = newDirectory;
                }
                else
                {
                    outputWriter.WriteError($"cd: {newDirectory}: No such file or directory");
                }
                continue;
            }
            else if (command == "type")
            {
                if (args.Count < 2)
                {
                    continue;
                }
                string commandName = args[1];
                if (builtInCommands.Contains(commandName))
                {
                    outputWriter.WriteOutput($"{commandName} is a shell builtin");
                }
                else
                {
                    string fullPath = GetCommandPath(commandName);
                    if (!string.IsNullOrEmpty(fullPath))
                    {
                        outputWriter.WriteOutput($"{commandName} is {fullPath}");
                    }
                    else
                    {
                        outputWriter.WriteError($"{commandName}: not found");
                    }
                }        
            }
            else
            {
                string fullPath = GetCommandPath(args[0]);

                if (!string.IsNullOrEmpty(fullPath))
                {
                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = args[0],
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    foreach (var arg in args.Skip(1))
                    {
                        processStartInfo.ArgumentList.Add(arg);
                    }

                    var process = Process.Start(processStartInfo);

                    if(outputWriter.RedirectOutputToFile && !string.IsNullOrEmpty(outputWriter.StandardOutputFileName))
                    {
                        using var output = File.Create(outputWriter.StandardOutputFileName);
                        process!.StandardOutput.BaseStream.CopyTo(output);
                    }
                    else if (outputWriter.AppendOutputToFile && !string.IsNullOrEmpty(outputWriter.StandardOutputFileName))
                    {
                        using var output = new StreamWriter(File.Open(outputWriter.StandardOutputFileName, FileMode.Append));

                        using var standardOutput = process!.StandardOutput;
                        string text = standardOutput.ReadToEnd();
                        output.Write(text);
                        if (text.Length > 0 && !text.EndsWith('\n'))
                        {
                            output.WriteLine();
                        }
                    }
                    else
                    {
                        using var output = process!.StandardOutput;
                        string text = output.ReadToEnd();
                        Console.Write(text);
                        if (text.Length > 0 && !text.EndsWith('\n'))
                        {
                            Console.WriteLine();
                        }
                    }

                    if (outputWriter.RedirectErrorToFile && !string.IsNullOrEmpty(outputWriter.StandardErrorFileName))
                    {
                        using var output = File.Create(outputWriter.StandardErrorFileName);
                        process!.StandardError.BaseStream.CopyTo(output);
                    }
                    else if (outputWriter.AppendErrorToFile && !string.IsNullOrEmpty(outputWriter.StandardErrorFileName))
                    {
                        using var output = new StreamWriter(File.Open(outputWriter.StandardErrorFileName, FileMode.Append));

                        using var standardError = process!.StandardError;
                        string text = standardError.ReadToEnd();
                        output.Write(text);
                        if (text.Length > 0 && !text.EndsWith('\n'))
                        {
                            output.WriteLine();
                        }
                    }
                    else
                    {
                        using var output = process!.StandardError;
                        string text = output.ReadToEnd();
                        Console.Write(text);
                        if (text.Length > 0 && !text.EndsWith('\n'))
                        {
                            Console.WriteLine();
                        }
                    }

                    process?.WaitForExit();
                }
                else
                {
                    outputWriter.WriteError($"{command}: command not found");
                }
            }    
        }
    }

    static string GetCommandPath(string commandName)
    {
        string? path = Environment.GetEnvironmentVariable("PATH");
        if (path != null)
        {
            foreach (string entry in path.Split(Path.PathSeparator))
            {
                string fullPath = Path.Combine(entry, commandName);
                if (File.Exists(fullPath))
                {
                    if(OperatingSystem.IsWindows() || File.GetUnixFileMode(fullPath).HasFlag(UnixFileMode.UserExecute))
                    {
                        return fullPath;
                    }
                }
            }
        }
        return string.Empty;
    }
}
