
namespace CodeCrafters.Shell.src
{
    internal class AutoCompletionManager
    {
        // characters to start completion from
        private EnvironmentManager environmentManager;

        public AutoCompletionManager(EnvironmentManager environmentManager)
        {
            this.environmentManager = environmentManager;
        }

        public string[] GetSuggestions(string text, bool isFirstWord, out bool isFolder)
        {
            isFolder = false;
            if(isFirstWord)
            {
                var matches = BuiltInCommandRunner.builtInCommands
                    .Where(command => command.StartsWith(text, StringComparison.Ordinal))
                    .ToArray();

                if (matches.Length == 0)
                {
                    matches = environmentManager.GetEnvironmentExecutablesWithPrefix(text);
                }

                return matches;
            }
            else
            {
                var nestedpaths = text.Split(Path.DirectorySeparatorChar);
                var lastNestedPart = nestedpaths.Last();
                var serachPath = Path.Combine([Directory.GetCurrentDirectory(), ..nestedpaths.Take(nestedpaths.Length - 1).ToArray()]);

                var allfiles = Directory.GetFiles(serachPath);

                var matches = allfiles
                    .Where(file => Path.GetFileName(file).StartsWith(lastNestedPart, StringComparison.Ordinal))
                    .Select(file => Path.Combine([.. nestedpaths.Take(nestedpaths.Length - 1), Path.GetFileName(file)]))
                    .ToArray();

                if(matches.Any()) return matches;

                var allDirs = Directory.GetDirectories(serachPath);

                matches = allDirs
                    .Where(file => Path.GetFileName(file).StartsWith(lastNestedPart, StringComparison.Ordinal))
                    .Select(file => Path.Combine([..nestedpaths.Take(nestedpaths.Length - 1), Path.GetFileName(file)]))
                    .ToArray();

                if (matches.Any()) isFolder = true;

                return matches;
            }
        }
    }
}
