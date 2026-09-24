using CodeCrafters.Shell.src;
using System.Diagnostics;

class Program
{
    static void Main()
    {
        string workingDirectory = Directory.GetCurrentDirectory();
        List<string> builtInCommands = ["exit", "echo", "type", "pwd", "cd"];

        while(true)
        {
            Console.Write("$ ");
            List<string> args = InputParser.ParseInput(Console.ReadLine()!);
            string command = args[0];

            if (command == "exit")
            {
                break;
            }
            else if(command == "pwd")
            {
                Console.WriteLine(workingDirectory);
            }
            else if(command == "echo")
            {
                Console.WriteLine(string.Join(" ", args.Skip(1)));
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
                    string tmpDirectory = workingDirectory;
                    string[] paths = newDirectory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    foreach (string path in paths)
                    {
                        if (path == "..")
                        {
                            tmpDirectory = Path.GetDirectoryName(tmpDirectory) ?? workingDirectory;
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
                    workingDirectory = newDirectory;
                }
                else
                {
                    Console.WriteLine($"cd: {newDirectory}: No such file or directory");
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
                    Console.WriteLine($"{commandName} is a shell builtin");
                }
                else
                {
                    string fullPath = GetCommandPath(commandName);
                    if (!string.IsNullOrEmpty(fullPath))
                    {
                        Console.WriteLine($"{commandName} is {fullPath}");
                    }
                    else
                    {
                        Console.WriteLine($"{commandName}: not found");
                    }
                }        
            }
            else
            {
                string fullPath = GetCommandPath(args[0]);

                if (!string.IsNullOrEmpty(fullPath))
                {
                    var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = args[0],
                        Arguments = string.Join(" ", args.Skip(1).Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg))
                    });

                    process?.WaitForExit();
                }
                else
                {
                    Console.WriteLine($"{command}: command not found");
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
