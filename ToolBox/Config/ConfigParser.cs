using ToolBox.AddonModules.Extensions;
namespace ToolBox.Config
{
    internal class Parser
    {
        internal static Dictionary<string, object> GetConfig(List<string> toolNames)
        {
            Dictionary<string, List<string>> schemaFile = Schema.GetSchema();
            Dictionary<string, object> root = [];
            Stack<(Dictionary<string, object>, int)> stack = new();
            foreach (string toolName in toolNames)
            {
                if (!schemaFile.ContainsKey(toolName)) { continue; }
                string filePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\\.dotnet\\tools\\ToolBoxConfigs\\{toolName}.config";
                string[] lines = File.ReadAllLines(filePath);
                string description = "";
                string defaultValue = "";
                List<string> options = [];
                string key;
                string currentValue;
                var toolDict = new Dictionary<string, object>();
                root[toolName] = toolDict;
                stack.GoDown((toolDict, -1));
                foreach (string line in lines)
                {
                    int indent = line.TakeWhile(c => c == '\t').Count();
                    int headerLineEnd = -1;
                    if (!line.StartsWith('/')) { headerLineEnd = line.IndexOf(':'); }
                    int commentLineStart = line.IndexOf('/');
                    int keyValueLineSeparator = line.IndexOf('=');
                    if (headerLineEnd != -1)
                    {
                        while (indent <= stack.Current().Item2) { stack.GoUp(); }
                        string header = line[..headerLineEnd];
                        var headerDict = new Dictionary<string, object>();
                        stack.Current().Item1[header] = headerDict;
                        stack.GoDown((headerDict, indent));
                        continue;
                    }
                    else if (commentLineStart != -1)
                    {
                        description = line[(commentLineStart + 1)..line.IndexOf('(')];
                        description = description.Trim();
                        defaultValue = line[(line.IndexOf(':') + 1)..line.IndexOf(')')];
                        defaultValue = defaultValue.Trim();
                        options = [.. line[(line.IndexOf('[') + 1)..line.IndexOf(']')].Split(',')];
                        continue;
                    }
                    else if (keyValueLineSeparator != -1)
                    {
						while (indent <= stack.Current().Item2) { stack.GoUp(); }
						key = line[..keyValueLineSeparator].Trim();
                        currentValue = line[(keyValueLineSeparator + 1)..].Trim().Trim('"');
                        var keyDict = new Dictionary<string, object>();
                        stack.Current().Item1[key] = keyDict;
                        stack.GoDown((keyDict, indent));
                        keyDict["description"] = description;
                        keyDict["defaultValue"] = defaultValue;
                        keyDict["options"] = options;
                        keyDict["currentValue"] = currentValue;
                        continue;
                    }
                    else { continue; }
                }
                stack.Clear();
            }
            return root;
        }
    }
}