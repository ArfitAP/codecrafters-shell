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
                    Console.WriteLine($"{commandName}: not found");
                }
                continue;
            }

            Console.WriteLine($"{command}: command not found");
        }
    }
}
