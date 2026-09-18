using System.Diagnostics;

class Program
{
    static void Main()
    {
        while(true)
        {
            Console.Write("$ ");
            string? command = Console.ReadLine();

            if (command == "exit")
            {
                break;
            }
            else if(command != null && command.StartsWith("echo "))
            {
                Console.WriteLine(command[5..].Trim());
                continue;
            }
            else if (command != null && command.StartsWith("type "))
            {
                string commandName = command[5..].Trim();
                if (commandName == "exit" || commandName == "echo" || commandName == "type")
                {
                    Console.WriteLine($"{commandName} is a shell builtin");
                }
                else
                {
                    string? path = Environment.GetEnvironmentVariable("PATH");
                    bool found = false;
                    if (path != null)
                    {
                        foreach (string entry in path.Split(Path.PathSeparator))
                        {   
                            string fullPath = Path.Combine(entry, commandName);
                            if (File.Exists(fullPath))
                            {
                                if(OperatingSystem.IsWindows() || File.GetUnixFileMode(fullPath).HasFlag(UnixFileMode.UserExecute))
                                {
                                    Console.WriteLine($"{commandName} is {fullPath}");
                                    found = true;
                                    break;
                                }                         
                            }
                        }
                    }

                    if (!found)
                        Console.WriteLine($"{commandName}: not found");
                }
                continue;
            }

            bool executed = false;
            string[] programargs = command!.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            foreach (string entry in Environment.GetEnvironmentVariable("PATH")!.Split(Path.PathSeparator))
            {
                string fullPath = Path.Combine(entry, programargs[0]);
                if (File.Exists(fullPath))
                {
                    if (OperatingSystem.IsWindows() || File.GetUnixFileMode(fullPath).HasFlag(UnixFileMode.UserExecute))
                    {
                        var process = Process.Start(new ProcessStartInfo
                        {
                            FileName = fullPath,
                            Arguments = string.Join(" ", programargs.Skip(1))
                        });

                        process?.WaitForExit();
                        executed = true;
                        break;
                    }
                }
            }
            
            if(!executed)
                Console.WriteLine($"{command}: command not found");
        }
    }
}
