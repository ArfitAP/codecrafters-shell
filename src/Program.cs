using CodeCrafters.Shell.src;
using CodeCrafters.Shell.src.AutoCompletion;
using CodeCrafters.Shell.src.CommandRunners;

class Program
{
    static async Task Main()
    {
        EnvironmentManager environmentManager = new EnvironmentManager();
        HistoryManager historyManager = new HistoryManager();

        BackgroundCommandRunner backgroundCommandRunner = new BackgroundCommandRunner();
        BuiltInCommandRunner builtInCommandRunner = new BuiltInCommandRunner(environmentManager, backgroundCommandRunner, historyManager);
        ExternalCommandRunner externalCommandRunner = new ExternalCommandRunner(environmentManager, backgroundCommandRunner, builtInCommandRunner, historyManager);

        AutoCompletionManager autoCompletionManager = new AutoCompletionManager(environmentManager, builtInCommandRunner);

        LineEditor editor = new LineEditor(autoCompletionManager);

        historyManager.ReadHistoryFromFile(environmentManager.GetDefaultHistoryFilePath());

        while (true)    
        {
            backgroundCommandRunner.Reap(true);
            string? input = editor.ReadLine("$ ");

            historyManager.AddToHistory(input!);
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
