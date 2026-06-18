namespace ToolBox.Config
{
    public class Editor
	{
		public static void EditConfig(List<string> installedTools)
		{
			Dictionary<string, object> configs = Parser.GetConfig(installedTools);
			string configPath = "";
			foreach (string key in configs.Keys) { Console.WriteLine(key); }
		}
	}
}