using CodeCrafters.Shell.src;
using CodeCrafters.Shell.src.AutoCompletion;
using CodeCrafters.Shell.src.CommandRunners;

class Program
{
    static void Main()
    {
        EnvironmentManager environmentManager = new EnvironmentManager();

        ExternalCommandRunner externalCommandRunner = new ExternalCommandRunner(environmentManager);
        BuiltInCommandRunner builtInCommandRunner = new BuiltInCommandRunner(environmentManager);

        AutoCompletionManager autoCompletionManager = new AutoCompletionManager(environmentManager);

        LineEditor editor = new LineEditor(autoCompletionManager);

        while (true)    
        {
            string? input = editor.ReadLine("$ ");

            List<string> args = InputParser.ParseInput(input!);
            string command = args[0];

            OutputWriter outputWriter = new OutputWriter(ref args);

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
    }
}
