
using System.Diagnostics;

namespace CodeCrafters.Shell.src
{
    internal class ExternalCommandRunner
    {
        private EnvironmentManager environmentManager;

        public ExternalCommandRunner(EnvironmentManager environmentManager)
        {
            this.environmentManager = environmentManager;
        }

        public bool TryExecuteExternalCommand(string command, List<string> args, OutputWriter outputWriter)
        {
            string fullPath = environmentManager.GetCommandPath(args[0]);

            if (!string.IsNullOrEmpty(fullPath))
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
    }
}
