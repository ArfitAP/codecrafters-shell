using CodeCrafters.Shell.src;
using CodeCrafters.Shell.src.AutoCompletion;
using CodeCrafters.Shell.src.CommandRunners;

class Program
{
    static async Task Main()
    {
        EnvironmentManager environmentManager = new EnvironmentManager();

        BackgroundCommandRunner backgroundCommandRunner = new BackgroundCommandRunner();
        BuiltInCommandRunner builtInCommandRunner = new BuiltInCommandRunner(environmentManager, backgroundCommandRunner);
        ExternalCommandRunner externalCommandRunner = new ExternalCommandRunner(environmentManager, backgroundCommandRunner, builtInCommandRunner);

        AutoCompletionManager autoCompletionManager = new AutoCompletionManager(environmentManager, builtInCommandRunner);

        LineEditor editor = new LineEditor(autoCompletionManager);

        while (true)    
        {
            backgroundCommandRunner.Reap(true);
            string? input = editor.ReadLine("$ ");

            List<List<string>> commandParts = InputParser.ParseInput(input!);

            if(commandParts.Count == 1)
            { 
                List<string> args = commandParts[0];
                string command = args[0];

                OutputWriter outputWriter = new OutputWriter(ref args, Console.Out, Console.Error);

                if (command == "exit")
                {
                    break;
                }

                if(!builtInCommandRunner.TryExecuteBuiltInCommand(command, args, outputWriter))
                {
                    if(!externalCommandRunner.TryExecuteExternalCommand(command, args, outputWriter))
                    {
                        outputWriter.WriteError($"{command}: command not found");
                    }
                }
            }
            else
            {
                await externalCommandRunner.TryExecutePipedCommandAsync(commandParts);
            }
        }
    }
}
