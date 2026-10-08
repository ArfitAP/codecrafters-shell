using CodeCrafters.Shell.src.CommandRunners;

namespace CodeCrafters.Shell.src.AutoCompletion
{
    internal class AutoCompletionManager
    {
        // characters to start completion from
        private EnvironmentManager environmentManager;

        public AutoCompletionManager(EnvironmentManager environmentManager)
        {
            this.environmentManager = environmentManager;
        }

        public AutoCompletionResult[] GetSuggestions(string text, bool isFirstWord)
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
    }
}
