using CodeCrafters.Shell.src.CommandRunners;
using System.Diagnostics;

namespace CodeCrafters.Shell.src.AutoCompletion
{
    internal class AutoCompletionManager
    {
        // characters to start completion from
        private EnvironmentManager environmentManager;
        private BuiltInCommandRunner builtInCommandRunner;

        public AutoCompletionManager(EnvironmentManager environmentManager, BuiltInCommandRunner builtInCommandRunner)
        {
            this.environmentManager = environmentManager;
            this.builtInCommandRunner = builtInCommandRunner;
        }

        public AutoCompletionResult[] GetSuggestions(string wholeCommand, string text, bool isFirstWord)
        {
            if(isFirstWord)
            {
                var matches = BuiltInCommandRunner.builtInCommands
                    .Where(command => command.StartsWith(text, StringComparison.Ordinal))
                    .Select(command => new AutoCompletionResult(command, AutoCompletionType.Executable))
                    .ToArray();

                if (matches.Length == 0)
                {
                    matches = environmentManager.GetEnvironmentExecutablesWithPrefix(text)
                        .Select(executable => new AutoCompletionResult(executable, AutoCompletionType.Executable))
                        .ToArray();
                }

                return matches;
            }
            else if (builtInCommandRunner.IsCompleterRegistered(wholeCommand.Split(' ')[0]))
            {
                var scriptPath = builtInCommandRunner.GetCompleterScriptPath(wholeCommand.Split(' ')[0]);

                return GetCompleterSuggestions(scriptPath)
                    .Select(s => new AutoCompletionResult(s, AutoCompletionType.Executable))
                    .ToArray();
            }
            else
            {
                var nestedpaths = text.Split(Path.DirectorySeparatorChar);
                var lastNestedPart = nestedpaths.Last();
                var serachPath = Path.Combine([Directory.GetCurrentDirectory(), ..nestedpaths.Take(nestedpaths.Length - 1).ToArray()]);

                AutoCompletionResult[] matches = Array.Empty<AutoCompletionResult>();

                var allfiles = Directory.GetFiles(serachPath);

                var matchedFiles = allfiles
                    .Where(file => Path.GetFileName(file).StartsWith(lastNestedPart, StringComparison.Ordinal))
                    .Select(file => Path.Combine([.. nestedpaths.Take(nestedpaths.Length - 1), Path.GetFileName(file)]))
                    .ToArray();

                foreach (var file in matchedFiles)
                {
                    matches = matches.Append(new AutoCompletionResult(file, AutoCompletionType.File)).ToArray();
                }

                var allDirs = Directory.GetDirectories(serachPath);

                var matchedDirs = allDirs
                    .Where(file => Path.GetFileName(file).StartsWith(lastNestedPart, StringComparison.Ordinal))
                    .Select(file => Path.Combine([..nestedpaths.Take(nestedpaths.Length - 1), Path.GetFileName(file)]))
                    .ToArray();

                foreach (var dir in matchedDirs)
                {
                    matches = matches.Append(new AutoCompletionResult(dir, AutoCompletionType.Folder)).ToArray();
                }

                return matches;
            }
        }

        private string[] GetCompleterSuggestions(string scriptPath)
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = scriptPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);

            if (process == null)
                return [];

            string output = process.StandardOutput.ReadToEnd();

            process.WaitForExit();

            return output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
