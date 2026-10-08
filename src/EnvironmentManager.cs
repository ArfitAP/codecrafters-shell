using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace CodeCrafters.Shell.src
{
    internal class EnvironmentManager
    {
        public string GetCommandPath(string commandName)
        {
            string? path = Environment.GetEnvironmentVariable("PATH");
            if (path != null)
            {
                foreach (string entry in path.Split(Path.PathSeparator))
                {
                    string fullPath = Path.Combine(entry, commandName);
                    if (File.Exists(fullPath))
                    {
                        if (OperatingSystem.IsWindows() || File.GetUnixFileMode(fullPath).HasFlag(UnixFileMode.UserExecute))
                        {
                            return fullPath;
                        }
                    }
                }
            }
            return string.Empty;
        }

        public string[] GetEnvironmentExecutablesWithPrefix(string prefix)
        {
            string[] matches = Array.Empty<string>();
            string? path = Environment.GetEnvironmentVariable("PATH");
            if (path != null)
            {
                foreach (string entry in path.Split(Path.PathSeparator))
                {
                    if (!Directory.Exists(entry))
                    {
                        continue;
                    }

                    foreach (string fullFileName in Directory.GetFiles(entry))
                    {
                        string commandName = Path.GetFileName(fullFileName);
                        if (commandName.StartsWith(prefix, StringComparison.Ordinal))
                        {
                            matches = matches.Append(commandName).ToArray();
                        }
                    }
                }
            }

            return matches;
        }
    }
}
