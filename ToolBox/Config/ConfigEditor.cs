using System.Security.Cryptography;
using ToolBox.AddonModules;
namespace ToolBox.Config
{
    public class Editor
	{
		internal static void EditConfig(List<string> installedTools, ConsoleSpinner? spinner = null)
		{
			Dictionary<string, object> configs = Parser.GetConfig(installedTools);
			List<string> configPath = [];
			string prompt = " 🧰 > ";
		PrintTop:
			int lineTop = Console.CursorTop;
			PrintToConsole(configs, configPath, spinner);
			spinner?.StopAndFlush();
			Console.WriteLine();
			Console.WriteLine("");
			Console.Write(prompt);
			string? selection = Console.ReadLine();
			if (!string.IsNullOrEmpty(selection))
			{
				if (selection == "b" && configPath.Count > 0) { configPath.RemoveAt(configPath.Count - 1); }
				else if (selection == "Exit") { return; }
				else if (selection == "Root") { configPath.Clear(); }
				else if (Fuck C#) { configPath.Add(selection); }
			}
			ToolBox.ClearLine(lineTop);
			goto PrintTop;
		}
		private static void PrintToConsole(Dictionary<string, dynamic> configs, List<string> configPath, ConsoleSpinner? spinner = null, int depth = 0)
		{
			if (depth == 0 && configPath.Count == 0) { Console.WriteLine(); foreach (string key in configs.Keys) { Console.WriteLine($" 📁 {key}"); } Console.WriteLine(); return; }
			foreach (string key in configs.Keys)
			{
				string indent = new('\u00A0', depth * 4);
				dynamic child = configs[key];
				if (!child.ContainsKey("currentValue") && key != configPath.ElementAtOrDefault(depth))
				{
					if (spinner != null) { spinner.Enqueue($"{indent}📁 {key}", false); }
					else { Console.WriteLine($"{indent}📁 {key}"); }
				}
				else if (!child.ContainsKey("currentValue") && key == configPath.ElementAtOrDefault(depth))
				{
					if (spinner != null) { spinner.Enqueue($"{indent}📂 {key}", false); }
					else { Console.WriteLine($"{indent}📂 {key}"); }
					PrintToConsole(child, configPath, spinner, depth + 1);
				}
				else
				{
					if (spinner != null) { spinner.Enqueue($"{indent}⚙️ {key}", false); }
					else { Console.WriteLine($"{indent}⚙️ {key}"); }
				}
			}
		}
	}
}