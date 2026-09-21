using System.Diagnostics;

class Program
{
    static void Main()
    {
        string workingDirectory = Directory.GetCurrentDirectory();
        List<string> builtInCommands = ["exit", "echo", "type", "pwd"];

        while(true)
        {
            Console.Write("$ ");
            string command = Console.ReadLine()!;

            if (command == "exit")
            {
                break;
            }
            else if(command == "pwd")
            {
                Console.WriteLine(workingDirectory);
            }
            else if(command != null && command.StartsWith("echo "))
            {
                Console.WriteLine(command[5..].Trim());
                continue;
            }
            else if (command != null && command.StartsWith("type "))
            {
                string commandName = command[5..].Trim();
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
                string[] programargs = command!.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                string fullPath = GetCommandPath(programargs[0]);

                if (!string.IsNullOrEmpty(fullPath))
                {
                    var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = programargs[0],
                        Arguments = string.Join(" ", programargs.Skip(1))
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
