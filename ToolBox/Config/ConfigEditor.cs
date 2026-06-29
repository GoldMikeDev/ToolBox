using ToolBox.AddonModules;
namespace ToolBox.Config
{
    public class Editor
	{
		public static void EditConfig(List<string> installedTools, ConsoleSpinner? spinner = null)
		{
			Dictionary<string, object> configs = Parser.GetConfig(installedTools);
			int depth = 0;
			List<string> configPath = [];
		PrintTop:
			int lineTop = Console.CursorTop;
			PrintToConsole(configs, depth, configPath, spinner);

			goto PrintTop;
		}
		public static void PrintToConsole(Dictionary<string, object> configs, int depth, List<string> configPath,ConsoleSpinner? spinner = null)
		{
			if (depth < 0) { if (spinner != null) { spinner.Enqueue(" ⚠️ Config load error", true); } else { Console.WriteLine(" ⚠️ Config load error"); } return; }
			if (depth == 0 && configPath.Count == 0) { Console.WriteLine(); foreach (string key in configs.Keys) { Console.WriteLine($" 📁 {key}"); } Console.WriteLine(); return; }
			foreach (string key in configs.Keys)
			{
				if (key != configPath.ElementAtOrDefault(depth)) { Console.WriteLine($" 📁 {key}"); }
				else
				{
					Console.WriteLine($" 📂 {key}");
					PrintToConsole(configs, depth + 1, configPath, spinner);
				}
			}
		}
	}
}