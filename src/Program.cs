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

            Console.WriteLine($"{command}: command not found");
        }
    }
}
