using System.Text.RegularExpressions;

namespace CodeCrafters.Shell.src
{
    internal class VariableManager
    {
        private Dictionary<string, string> declaredVariables;

        public VariableManager()
        {
            declaredVariables = new Dictionary<string, string>();
        }

        public string PrintVariable(string variableName)
        {
            if (declaredVariables.ContainsKey(variableName))
            {
                return $"declare -- {variableName}=\"{declaredVariables[variableName]}\"";
            }
            else
            {
                return $"declare: {variableName}: not found";
            }
        }

        public bool SetVariable(string variableName, string value)
        {
            string pattern = @"^[a-zA-Z_][a-zA-Z0-9_]*$";

            if (Regex.IsMatch(variableName, pattern))
            {
                declaredVariables[variableName] = value;
                return true;
            }
            else
            {
                return false;
            }
        }

        public string GetVariable(string variableName)
        {
            if (declaredVariables.ContainsKey(variableName))
            {
                return declaredVariables[variableName];
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
