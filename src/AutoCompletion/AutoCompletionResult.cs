
namespace CodeCrafters.Shell.src.AutoCompletion
{
    internal class AutoCompletionResult
    {
        public string matchedText { get; } = string.Empty;

        public AutoCompletionType type { get; } = AutoCompletionType.File;

        public AutoCompletionResult(string matchedText, AutoCompletionType type)
        {
            this.matchedText = matchedText;
            this.type = type;
        }
    }
}
