namespace ToolBox.Config
{
    public class Generator
    {
        public static void CreateFiles(List<string> installedTools)
        {
			Dictionary<string, List<string>> schemaFile = Schema.GetSchema();
            foreach (string tool in installedTools)
            {
                if (!schemaFile.ContainsKey(tool)) { continue; }
				string toolName = tool;
                string filePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\\.dotnet\\tools\\ToolBoxConfigs\\{toolName}.config";
				if (File.Exists(filePath)) { continue; }
				Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
				StreamWriter writer = File.AppendText(filePath);
				string[] lines = [.. schemaFile.GetValueOrDefault(tool, [])];
                foreach (string line in lines) { writer.WriteLine(line); }
				writer.Dispose();
			}
        }
    }
}