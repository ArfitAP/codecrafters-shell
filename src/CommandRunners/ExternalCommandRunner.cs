using System.Diagnostics;

namespace CodeCrafters.Shell.src.CommandRunners
{
    internal class ExternalCommandRunner
    {
        private EnvironmentManager environmentManager;
        private BackgroundCommandRunner backgroundCommandRunner;

        public ExternalCommandRunner(EnvironmentManager environmentManager, BackgroundCommandRunner backgroundCommandRunner)
        {
            this.environmentManager = environmentManager;
            this.backgroundCommandRunner = backgroundCommandRunner;
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

            try
            {
                // 1. Resolve all commands before starting any process.
                var startInfos = new List<ProcessStartInfo>();

                foreach (var args in commandParts)
                {
                    string fullPath = environmentManager.GetCommandPath(args[0]);

                    if (string.IsNullOrEmpty(fullPath))
                    {
                        Console.WriteLine($"{args[0]}: command not found");
                        return;
                    }

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = fullPath,
                        UseShellExecute = false,
                        RedirectStandardInput = startInfos.Count > 0,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    foreach (var arg in args.Skip(1))
                        startInfo.ArgumentList.Add(arg);

                    startInfos.Add(startInfo);
                }

                // 2. Create and start all processes.
                foreach (var startInfo in startInfos)
                {
                    var process = new Process
                    {
                        StartInfo = startInfo
                    };

                    processes.Add(process);
                    process.Start();
                }

                // 3. Start draining stderr for every process immediately.
                var errorTasks = processes
                    .Select(p => p.StandardError.BaseStream.CopyToAsync(
                        Console.OpenStandardError()))
                    .ToList();

                // 4. Connect adjacent processes concurrently.
                var pipeTasks = new List<Task>();

                for (int i = 0; i < processes.Count - 1; i++)
                {
                    var source = processes[i];
                    var destination = processes[i + 1];

                    pipeTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await source.StandardOutput.BaseStream.CopyToAsync(
                                destination.StandardInput.BaseStream);
                        }
                        finally
                        {
                            // Signal EOF to the downstream command.
                            destination.StandardInput.Close();
                        }
                    }));
                }

                // 5. Consume the last process's output concurrently.
                var lastOutputTask = processes[^1]
                    .StandardOutput.BaseStream.CopyToAsync(
                        Console.OpenStandardOutput());

                // 6. Wait for all processes and stream operations.
                var exitTasks = processes
                    .Select(p => p.WaitForExitAsync());

                await Task.WhenAll(
                    pipeTasks
                        .Concat(errorTasks)
                        .Append(lastOutputTask)
                        .Concat(exitTasks));
            }
            finally
            {
                // 7. Clean up even if an operation fails.
                foreach (var process in processes)
                {
                    try
                    {
                        if (!process.HasExited)
                            process.Kill(entireProcessTree: true);
                    }
                    catch (InvalidOperationException)
                    {
                        // The process may not have started.
                    }
                    catch (System.ComponentModel.Win32Exception)
                    {
                        // The process may already have exited.
                    }

                    process.Dispose();
                }
            }
        }
    }
}
