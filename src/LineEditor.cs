using System;
using System.Collections.Generic;
using System.Text;

namespace CodeCrafters.Shell.src
{
    internal class LineEditor
    {
        private readonly List<string> _history = new();
        private readonly AutoCompletionManager _autoCompletionManager;

        public LineEditor(AutoCompletionManager autoCompletionManager)
        {
            _autoCompletionManager = autoCompletionManager;
        }

        public string? ReadLine(string prompt)
        {
            Console.Write(prompt);

            var buffer = new StringBuilder();
            int cursor = 0;                 // cursor position inside buffer
            int historyIndex = _history.Count;
            string savedInput = "";         // what the user typed before browsing history
            bool lastWasTab = false;        // for "press TAB twice to list matches"

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(intercept: true);
                bool tabThisRound = false;

                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        Console.WriteLine();
                        string line = buffer.ToString();
                        if (!string.IsNullOrWhiteSpace(line))
                            _history.Add(line);
                        return line;

                    case ConsoleKey.Tab:
                        tabThisRound = true;
                        HandleTab(prompt, buffer, ref cursor, lastWasTab);
                        break;

                    case ConsoleKey.Backspace:
                        if (cursor > 0)
                        {
                            buffer.Remove(cursor - 1, 1);
                            cursor--;
                            Redraw(prompt, buffer, cursor);
                        }
                        break;

                    case ConsoleKey.Delete:
                        if (cursor < buffer.Length)
                        {
                            buffer.Remove(cursor, 1);
                            Redraw(prompt, buffer, cursor);
                        }
                        break;

                    case ConsoleKey.LeftArrow:
                        if (cursor > 0) { cursor--; Redraw(prompt, buffer, cursor); }
                        break;

                    case ConsoleKey.RightArrow:
                        if (cursor < buffer.Length) { cursor++; Redraw(prompt, buffer, cursor); }
                        break;

                    case ConsoleKey.Home:
                        cursor = 0;
                        Redraw(prompt, buffer, cursor);
                        break;

                    case ConsoleKey.End:
                        cursor = buffer.Length;
                        Redraw(prompt, buffer, cursor);
                        break;

                    case ConsoleKey.UpArrow:
                        if (historyIndex > 0)
                        {
                            if (historyIndex == _history.Count)
                                savedInput = buffer.ToString();
                            historyIndex--;
                            SetBuffer(buffer, _history[historyIndex], out cursor);
                            Redraw(prompt, buffer, cursor);
                        }
                        break;

                    case ConsoleKey.DownArrow:
                        if (historyIndex < _history.Count)
                        {
                            historyIndex++;
                            string text = historyIndex == _history.Count
                                ? savedInput
                                : _history[historyIndex];
                            SetBuffer(buffer, text, out cursor);
                            Redraw(prompt, buffer, cursor);
                        }
                        break;

                    case ConsoleKey.Escape:
                        buffer.Clear();
                        cursor = 0;
                        Redraw(prompt, buffer, cursor);
                        break;

                    default:
                        // Ctrl combinations
                        if ((key.Modifiers & ConsoleModifiers.Control) != 0)
                        {
                            if (key.Key == ConsoleKey.C)       // Ctrl+C
                            {
                                Console.WriteLine("^C");
                                return "";
                            }
                            if (key.Key == ConsoleKey.D && buffer.Length == 0) // Ctrl+D = EOF
                            {
                                Console.WriteLine();
                                return null;
                            }
                            if (key.Key == ConsoleKey.L)       // clear screen
                            {
                                Console.Clear();
                                Redraw(prompt, buffer, cursor);
                            }
                            if (key.Key == ConsoleKey.U)       // delete to start of line
                            {
                                buffer.Remove(0, cursor);
                                cursor = 0;
                                Redraw(prompt, buffer, cursor);
                            }
                            if (key.Key == ConsoleKey.K)       // delete to end of line
                            {
                                buffer.Remove(cursor, buffer.Length - cursor);
                                Redraw(prompt, buffer, cursor);
                            }
                            break;
                        }

                        // Normal printable character
                        if (!char.IsControl(key.KeyChar))
                        {
                            buffer.Insert(cursor, key.KeyChar);
                            cursor++;
                            Redraw(prompt, buffer, cursor);
                        }
                        break;
                }

                lastWasTab = tabThisRound;
            }
        }

        // ---------- TAB completion ----------

        private void HandleTab(string prompt, StringBuilder buffer, ref int cursor, bool secondTab)
        {
            // Complete the word that ends at the cursor
            string text = buffer.ToString();
            int wordStart = text.LastIndexOf(' ', Math.Max(cursor - 1, 0)) + 1;
            if (cursor == 0) wordStart = 0;
            string prefix = text.Substring(wordStart, cursor - wordStart);

            bool isFirstWord = wordStart == 0;

            IEnumerable<string> candidates = _autoCompletionManager.GetSuggestions(prefix, isFirstWord, out bool isFolder);

            var matches = candidates
                .OrderBy(c => c)
                .ToList();

            if (matches.Count == 0)
            {
                Console.Write('\a'); // bell
                return;
            }

            if (matches.Count == 1)
            {
                // Single match: complete it and add a trailing space
                string completion = matches[0] + (isFolder ? Path.DirectorySeparatorChar.ToString() : " ");
                buffer.Remove(wordStart, cursor - wordStart);
                buffer.Insert(wordStart, completion);
                cursor = wordStart + completion.Length;
                Redraw(prompt, buffer, cursor);
                return;
            }

            // Multiple matches: complete the longest common prefix
            string common = LongestCommonPrefix(matches);
            if (common.Length > prefix.Length)
            {
                buffer.Remove(wordStart, cursor - wordStart);
                buffer.Insert(wordStart, common);
                cursor = wordStart + common.Length;
                Redraw(prompt, buffer, cursor);
            }
            else if (secondTab)
            {
                // Second TAB in a row: list all possibilities (like bash)
                Console.WriteLine();
                Console.WriteLine(string.Join("  ", matches));
                Redraw(prompt, buffer, cursor, fullRedrawOnNewLine: true);
            }
            else
            {
                Console.Write('\a'); // bell; press TAB again to list
            }
        }

        private static string LongestCommonPrefix(List<string> items)
        {
            string prefix = items[0];
            foreach (var s in items.Skip(1))
            {
                int i = 0;
                while (i < prefix.Length && i < s.Length && prefix[i] == s[i]) i++;
                prefix = prefix.Substring(0, i);
            }
            return prefix;
        }

        // ---------- helpers ----------

        private static void SetBuffer(StringBuilder buffer, string text, out int cursor)
        {
            buffer.Clear();
            buffer.Append(text);
            cursor = buffer.Length;
        }

        private int _lastRenderedLength = 0;

        /// Redraws the prompt + buffer on the current line and positions the cursor.
        private void Redraw(string prompt, StringBuilder buffer, int cursor, bool fullRedrawOnNewLine = false)
        {
            if (fullRedrawOnNewLine)
                _lastRenderedLength = 0;

            // Go to start of the line, rewrite everything, then blank out leftovers
            Console.Write('\r');
            Console.Write(prompt);
            Console.Write(buffer.ToString());

            int extra = _lastRenderedLength - buffer.Length;
            if (extra > 0)
                Console.Write(new string(' ', extra));

            _lastRenderedLength = buffer.Length;

            // Place the cursor
            Console.Write('\r');
            Console.Write(prompt);
            Console.Write(buffer.ToString(0, cursor));
        }
    }
}
