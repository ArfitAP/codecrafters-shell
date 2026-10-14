using System.Diagnostics;
using System.IO.Pipelines;
using System.Text;

namespace CodeCrafters.Shell.src.CommandRunners
{
    internal class ExternalCommandRunner
    {
        private EnvironmentManager environmentManager;
        private BackgroundCommandRunner backgroundCommandRunner;
        private BuiltInCommandRunner builtInCommandRunner;
        private HistoryManager historyManager;

        public ExternalCommandRunner(EnvironmentManager environmentManager, BackgroundCommandRunner backgroundCommandRunner, BuiltInCommandRunner builtInCommandRunner, HistoryManager historyManager)
        {
            this.environmentManager = environmentManager;
            this.backgroundCommandRunner = backgroundCommandRunner;
            this.builtInCommandRunner = builtInCommandRunner;
            this.historyManager = historyManager;
        }

        public bool TryExecuteExternalCommand(string command, List<string> args, OutputWriter outputWriter)
        {
            string fullPath = environmentManager.GetCommandPath(args[0]);
            bool isBackgroundProcess = args[^1].Equals("&");

            if (isBackgroundProcess)
            {
                args = args.Take(args.Count - 1).ToList();

                (int id, BackgroundJobInfo jobInfo) result = backgroundCommandRunner.RunCommandInBackground(args[0], args.Skip(1).ToList());

                Console.WriteLine($"[{result.id}] {result.jobInfo.Pid}");

                return true;
            }
            else if (!string.IsNullOrEmpty(fullPath))
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

                if (outputWriter.RedirectOutputToFile && !string.IsNullOrEmpty(outputWriter.StandardOutputFileName))
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
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task TryExecutePipedCommandAsync(List<List<string>> commandParts)
        {
            if (commandParts.Count == 0)
                return;

            var processes = new List<Process>();
            var pipes = new List<Pipe>();
            var tasks = new List<Task>();
            var ownedStreams = new List<Stream>();

            try
            {
                // Validate all external commands before starting the pipeline.
                foreach (var args in commandParts)
                {
                    if (!IsBuiltin(args[0]))
                    {
                        string fullPath =
                            environmentManager.GetCommandPath(args[0]);

                        if (string.IsNullOrEmpty(fullPath))
                        {
                            Console.WriteLine(
                                $"{args[0]}: command not found");
                            return;
                        }
                    }
                }

                // One pipe connects each adjacent pair of stages.
                for (int i = 0; i < commandParts.Count - 1; i++)
                    pipes.Add(new Pipe());

                for (int i = 0; i < commandParts.Count; i++)
                {
                    Stream input;
                    Stream output;

                    if (i == 0)
                    {
                        input = Stream.Null;
                    }
                    else
                    {
                        input = pipes[i - 1].Reader.AsStream();
                        ownedStreams.Add(input);
                    }

                    if (i == commandParts.Count - 1)
                    {
                        output = Console.OpenStandardOutput();
                    }
                    else
                    {
                        output = pipes[i].Writer.AsStream();
                        ownedStreams.Add(output);
                    }

                    var args = commandParts[i];

                    if (IsBuiltin(args[0]))
                    {
                        // Built-ins run inside this shell process.
                        Pipe? outputPipe = i < commandParts.Count - 1 ? pipes[i] : null;

                        tasks.Add(RunBuiltinStageAsync(
                            args, input, output, outputPipe));
                    }
                    else
                    {
                        string fullPath =
                            environmentManager.GetCommandPath(args[0]);

                        var startInfo = new ProcessStartInfo
                        {
                            FileName = fullPath,
                            UseShellExecute = false,
                            RedirectStandardInput = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };

                        foreach (var arg in args.Skip(1))
                            startInfo.ArgumentList.Add(arg);

                        var process = new Process
                        {
                            StartInfo = startInfo
                        };

                        processes.Add(process);
                        process.Start();

                        // Feed this process from the preceding stage.
                        tasks.Add(FeedProcessAsync(process, input));

                        // Forward this process's output to the next stage.
                        Pipe? outputPipe = i < commandParts.Count - 1 ? pipes[i] : null;
                        tasks.Add(ForwardOutputAsync(process, output, outputPipe));

                        // Drain stderr concurrently to prevent pipe blockage.
                        tasks.Add(process.StandardError.BaseStream.CopyToAsync(
                            Console.OpenStandardError()));

                        tasks.Add(process.WaitForExitAsync());
                    }
                }

                await Task.WhenAll(tasks);
            }
            finally
            {
                foreach (var process in processes)
                {
                    try
                    {
                        if (!process.HasExited)
                            process.Kill(entireProcessTree: true);
                    }
                    catch (InvalidOperationException)
                    {
                        // Process was never started or is already detached.
                    }
                    catch (System.ComponentModel.Win32Exception)
                    {
                        // Process has already exited.
                    }
                }

                foreach (var stream in ownedStreams)
                    await stream.DisposeAsync();

                foreach (var pipe in pipes)
                {
                    await pipe.Reader.CompleteAsync();
                    await pipe.Writer.CompleteAsync();
                }

                foreach (var process in processes)
                    process.Dispose();
            }
        }

        private static async Task FeedProcessAsync(
            Process process,
            Stream input)
        {
            try
            {
                await input.CopyToAsync(
                    process.StandardInput.BaseStream);
            }
            finally
            {
                process.StandardInput.Close();
            }
        }

        private static async Task ForwardOutputAsync(
            Process process,
            Stream output,
            Pipe? outputPipe)
        {
            try
            {
                await process.StandardOutput.BaseStream.CopyToAsync(output);
                await output.FlushAsync();
            }
            finally
            {
                if (outputPipe is not null)
                    await outputPipe.Writer.CompleteAsync();
            }
        }

        private async Task RunBuiltinStageAsync(
            List<string> args,
            Stream input,
            Stream output,
            Pipe? outputPipe)
        {
            try
            {
                using var reader = new StreamReader(
                    input, Encoding.UTF8, leaveOpen: true);

                using var writer = new StreamWriter(
                    output, new UTF8Encoding(false), leaveOpen: true)
                {
                    AutoFlush = true
                };

                await ExecuteBuiltinAsync(args, reader, writer);
                await writer.FlushAsync();
            }
            finally
            {
                if (outputPipe is not null)
                    await outputPipe.Writer.CompleteAsync();
            }
        }

        private bool IsBuiltin(string command)
        {
            return BuiltInCommandRunner.builtInCommands.Contains(command);
        }

        private Task ExecuteBuiltinAsync(List<string> args, StreamReader input, StreamWriter output)
        {
            return Task.Run(async () =>
            {
                var writer = new OutputWriter(
                    ref args,
                    output,
                    Console.Error);

                string command = args[0];

                builtInCommandRunner.TryExecuteBuiltInCommand(
                    command,
                    args,
                    writer);

                await output.FlushAsync();
            });
        }
    }
}
