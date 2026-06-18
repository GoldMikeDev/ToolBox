using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
using System.Xml.Linq;
using ToolBox.AddonModules;
using ToolBox.AddonModules.Updater;
using ToolBox.Config;
[assembly: SupportedOSPlatform("windows")]
namespace ToolBox
{
	class ToolBox
	{
		static readonly string header = @"
  ______                ___    _____                     
 /\__  _\              /\_ \  /\  _ `\                   
 \/_/\ \/   ___     ___\//\ \ \ \ \L\ \    ___   __  _   
    \ \ \  / __`\  / __`\\ \ \ \ \  _ <.  / __`\/\ \/ \  
     \ \ \/\ \L\ \/\ \L\ \\_\ \_\ \ \L\ \/\ \L\ \/>  </  
      \ \_\ \____/\ \____//\____\\ \____/\ \____//\_/\_\ 
       \/_/\/___/  \/___/ \/____/ \/___/  \/___/ \//\/_/ 
 -------------------------------------------------------";
		static readonly string prompt = " 🧰 > ";
		static readonly string[] allTools = ["ToolBox", "SteeleTerm", "WrapHDL"];
		static readonly string author = "GoldMike";
		static readonly Lock outputLock = new();
		static List<string> installedTools = [];
		static readonly string[] availableCommands = ["AllTools", "Config", "Exit", "Help", "InstalledTools", "Update", "Reset"];
		static readonly Dictionary<string, string[]> argsPrimary = new(StringComparer.Ordinal);
		static readonly Dictionary<string, string[]> argsSecondary = new(StringComparer.Ordinal);
		static readonly Dictionary<string, string[]> argsSecondaryUpdate = new(StringComparer.Ordinal) { ["--update"] = ["--forceUpdate"], ["--updateMajor"] = ["--forceUpdate"], ["--updateMinor"] = ["--forceUpdate"] };
		static readonly Dictionary<string, string[]> argsTertiary = new(StringComparer.Ordinal);
		static readonly Dictionary<string, string[]> argsTertiaryUpdate = new(StringComparer.Ordinal) { ["--forceUpdate"] = ["--skipVersion"] };
		static void Main()
		{
		Reset:
			Console.OutputEncoding = System.Text.Encoding.UTF8;
			Console.WriteLine(header);
			Console.WriteLine($" Ver {GetToolVersion()}");
			Console.WriteLine("");
			Console.WriteLine(" 📁 All Tools");
			Console.WriteLine(" 📂 Installed Tools:");
			var spinner = new ConsoleSpinner(outputLock, prompt, 100, 150);
			spinner.Start("⏳ Scanning installed tools");
			installedTools = GetInstalledToolCommandsByAuthor(author);
			spinner.StopAndFlush();
			if (installedTools.Count == 0)
			{
				installedTools = ["None"];
				Console.WriteLine("    ❌ None");
			}
			else
			{
				if (installedTools.Contains("ToolBox")) Console.WriteLine("    🧰 ToolBox:");
				for (int i = 0; i < installedTools.Count; i++)
				{
					if (installedTools[i] == "ToolBox") continue;
					Console.WriteLine($"       🔧 {installedTools[i]}");
				}
			}
			Console.WriteLine(" 🚪 Exit");
			Console.WriteLine("");
			spinner.Start("⏳ Building config files");
			Generator.CreateFiles(installedTools);
			spinner.StopAndFlush();
			spinner.Start("⏳ Building autocomplete");
			BuildAutocomplete(installedTools);
			spinner.StopAndFlush();
			spinner.Start("⏳ Loading and Validating command history");
			CSRB.Load("ToolBoxCommandHistory.csrb", spinner);
			spinner.StopAndFlush();
		Prompt:
			int lineTop = Console.CursorTop;
			Console.Write(prompt);
			string input = AutocompleteAndCommandHistory(spinner);
			if (installedTools.Any(t => input.Contains(t)))
			{
				ToolRunner(input);
				Console.WriteLine();
				goto Prompt;
			}
			switch (input)
			{
				case "Reset":
					goto Reset;
				case "AllTools":
					Console.WriteLine();
					Console.WriteLine(" 📂 All Tools:");
					if (allTools.Contains("ToolBox")) Console.WriteLine("    🧰 ToolBox:");
					for (int i = 0; i < allTools.Length; i++)
					{
						if (allTools[i] == "ToolBox") continue;
						Console.WriteLine($"       🔧 {allTools[i]}");
					}
					Console.WriteLine();
					goto Prompt;
				case "Config":
					Console.WriteLine();
					Editor.EditConfig(installedTools);
					Console.WriteLine();
					goto Prompt;
				case "Exit":
					spinner.Start("⏳ Saving command history");
					CSRB.Save(spinner);
					spinner.StopAndFlush();
					return;
				case "Help":
					Console.WriteLine();
					Console.WriteLine("      ToolBox Commands:");
					Console.WriteLine("        \'AllTools\'                            List all available tools.");
					Console.WriteLine("        \'Config\'                              Open config menu for installed tools.");
					Console.WriteLine("        \'Exit\'                                Close ToolBox.");
					Console.WriteLine("        \'Help\'                                Print help to console.");
					Console.WriteLine("        \'InstalledTools\'                      List all installed tools.");
					Console.WriteLine("        \'Update\'                              Checks for and installs any ToolBox updates.");
					Console.WriteLine("        \'Reset\'                               Reloads ToolBox.");
					Console.WriteLine();
					Console.WriteLine("      For dedicated tool help use \'<toolname> --help\'");
					Console.WriteLine();
					goto Prompt;
				case "InstalledTools":
					Console.WriteLine();
					PrintInstalledToolsByAuthor(author);
					Console.WriteLine();
					goto Prompt;
				case "Update":
					Console.WriteLine();
					UpdateToolBox();
					goto Prompt;
				default:
					ClearLine(lineTop);
					goto Prompt;
			}
		}
		static void UpdateToolBox()
		{
			Console.WriteLine("    Local(Source) or Remote(NuGet) (L/R):");
			Console.WriteLine();
		UpdateToolBox:
			int lineTop = Console.CursorTop;
			Console.Write(prompt);
			string? updateSource = Console.ReadLine();
			if (string.IsNullOrWhiteSpace(updateSource)) { ClearLine(lineTop); goto UpdateToolBox; }
			updateSource = updateSource.ToUpperInvariant();
			switch (updateSource)
			{
				case "Exit":
					return;
				case "L":
					bool major = false;
					bool minor = false;
					bool force = false;
					bool skip = false;
					Console.WriteLine();
					Console.WriteLine("    Select update options:");
					Console.WriteLine("    ----------------------------------------------------");
					Console.WriteLine("    Version bump:");
					Console.WriteLine("     M - Major");
					Console.WriteLine("     m - Minor");
					Console.WriteLine("     p - patch (auto used if no version arg specified)");
					Console.WriteLine("    ----------------------------------------------------");
					Console.WriteLine("     f - Force Update (bypass version hash checks)");
					Console.WriteLine("    ----------------------------------------------------");
					Console.WriteLine("     s - Skips version increment (Requires Force Update)");
					Console.WriteLine();
				LocalSource:
					lineTop = Console.CursorTop;
					Console.Write(prompt);
					string? updateTarget = Console.ReadLine();
					if (string.IsNullOrWhiteSpace(updateTarget)) { ClearLine(lineTop); goto LocalSource; }
					if ((updateTarget.Contains('M') && updateTarget.Contains('m')) || (updateTarget.Contains('M') && updateTarget.Contains('p')) || (updateTarget.Contains('m') && updateTarget.Contains('p')))
					{
						Console.WriteLine("Only one version bump type can be specified.");
						ClearLine(lineTop);
						goto LocalSource;
					}
					if (updateTarget.Contains('s') && !updateTarget.Contains('f'))
					{
						Console.WriteLine("Skip version requires force update.");
						ClearLine(lineTop);
						goto LocalSource;
					}
					if (updateTarget.Contains('M')) { major = true; }
					if (updateTarget.Contains('m')) { minor = true; }
					if (updateTarget.Contains('f')) { force = true; }
					if (updateTarget.Contains('s')) { skip = true; }
					Update.UpdateTool("ToolBox", "ToolBox.csproj", major, minor, force, skip, false, null, null, false);
					return;
				case "R":
					Console.WriteLine("Not implemented yet.");
					/*
					var spinner = new ConsoleSpinner(outputLock, prompt, 100, 150);
					spinner.Start("⏳ Starting update process");
					bool inheritConsole = true;
					Update.Cmd.Run("dotnet", "tool update --global ToolBox", null, false, true, true, inheritConsole);
					*/
					return;
				default:
					ClearLine(lineTop);
					goto UpdateToolBox;
			}
		}
		static string AutocompleteAndCommandHistory(ConsoleSpinner spinner)
		{
			string currentInput = "";
			string commandInput = "";
			int arg1 = -1;
			string argPrimaryInput = "";
			int arg2 = -1;
			string argSecondaryInput = "";
			int arg3 = -1;
			//string argTertiaryInput = "";
			string ghost = "";
			int i = 0;
			string inputBuffer = "";
			while (true)
			{
				ConsoleKeyInfo key = Console.ReadKey(true);
				if (key.Key == ConsoleKey.Backspace)
				{
					arg1 = arg2 = arg3 = -1;
					if (currentInput.Length > 0)
					{
						currentInput = currentInput[..^1];
						Console.Write("\b \b");
						if (currentInput.Length == 0) { ClearLine(Console.CursorTop); Console.Write(prompt); }
					}
				}
				if (key.Key == ConsoleKey.Enter)
				{
					CSRB.Write(currentInput, spinner);
					ClearLine(Console.CursorTop);
					Console.Write(prompt + currentInput);
					Console.WriteLine();
					i = 0;
					return currentInput;
				}
				if (key.Key == ConsoleKey.Tab || key.Key == ConsoleKey.RightArrow)
				{
					if (ghost.Length != 0)
					{
						currentInput += ghost;
						ClearLine(Console.CursorTop);
						Console.Write(prompt + currentInput);
					}
				}
				if (key.Key == ConsoleKey.UpArrow)
				{
					if (i == 0) { inputBuffer = currentInput; }
					currentInput = CSRB.Read(i, CSRB.Direction.previous, spinner, out i);
					ClearLine(Console.CursorTop);
					Console.Write(prompt + currentInput);
					continue;
				}
				if (key.Key == ConsoleKey.DownArrow)
				{
					if (i == 0)
					{
						currentInput = inputBuffer;
						ClearLine(Console.CursorTop);
						Console.Write(prompt + currentInput);
						continue;
					}
					else
					{
						currentInput = CSRB.Read(i, CSRB.Direction.next, spinner, out i);
						i--;
						ClearLine(Console.CursorTop);
						Console.Write(prompt + currentInput);
						continue;
					}
				}
				if (key.Key == ConsoleKey.LeftArrow || key.KeyChar == '\0' || char.IsControl(key.KeyChar)) continue;
				ghost = "";
				currentInput += key.KeyChar;
				Console.Write(key.KeyChar);
				arg1 = currentInput.IndexOf(' ');
				if (arg1 != -1)
				{
					commandInput = currentInput[..arg1];
					arg1++;
					arg2 = currentInput.IndexOf(' ', arg1);
					if (arg2 != -1)
					{
						argPrimaryInput = currentInput[arg1..arg2];
						arg2++;
						arg3 = currentInput.IndexOf(' ', arg2);
						if (arg3 != -1) argSecondaryInput = currentInput[(arg2)..arg3];
					}
				}
				var commandMatches = installedTools.Where(t => t.StartsWith(currentInput, StringComparison.Ordinal)).Concat(availableCommands.Where(t => t.StartsWith(currentInput, StringComparison.Ordinal))).OrderBy(x => x, StringComparer.Ordinal);
				var argMatchesPrimary = (argsPrimary.TryGetValue(commandInput, out var value) ? value : []).Where(x => x.StartsWith(currentInput[(commandInput.Length + 1)..].TrimStart(), StringComparison.Ordinal)).OrderBy(x => x, StringComparer.Ordinal);
				var argMatchesSecondary = (argsSecondary.TryGetValue(argPrimaryInput, out var value2) ? value2 : []).Concat(argsSecondaryUpdate.TryGetValue(argPrimaryInput, out var value4) ? value4 : []).Where(x => x.StartsWith(currentInput[(commandInput.Length + argPrimaryInput.Length + 2)..].TrimStart(), StringComparison.Ordinal)).OrderBy(x => x, StringComparer.Ordinal);
				var argMatchesTertiary = (argsTertiary.TryGetValue(argSecondaryInput, out var value3) ? value3 : []).Concat(argsTertiaryUpdate.TryGetValue(argSecondaryInput, out var value5) ? value5 : []).Where(x => x.StartsWith(currentInput[(commandInput.Length + argPrimaryInput.Length + argSecondaryInput.Length + 3)..].TrimStart(), StringComparison.Ordinal)).OrderBy(x => x, StringComparer.Ordinal);
				if (commandMatches.Any())
				{
					ClearLine(Console.CursorTop);
					ghost = commandMatches.First();
					ghost = ghost[currentInput.Length..];
					Console.Write(prompt + currentInput);
					var (Left, Top) = Console.GetCursorPosition();
					Console.ForegroundColor = ConsoleColor.DarkGray;
					Console.Write(ghost);
					Console.ResetColor();
					Console.SetCursorPosition(Left, Top);
				}
				if (arg1 != -1 && argMatchesPrimary.Any())
				{
					ClearLine(Console.CursorTop);
					ghost = argMatchesPrimary.First();
					ghost = string.Concat(commandInput,' ',ghost);
					ghost = ghost[currentInput.Length..];
					Console.Write(prompt + currentInput);
					var (Left, Top) = Console.GetCursorPosition();
					Console.ForegroundColor = ConsoleColor.DarkGray;
					Console.Write(ghost);
					Console.ResetColor();
					Console.SetCursorPosition(Left, Top);
				}
				if (arg2 != -1 && argMatchesSecondary.Any())
				{
					ClearLine(Console.CursorTop);
					ghost = argMatchesSecondary.First();
					ghost = string.Concat(commandInput,' ',argPrimaryInput,' ',ghost);
					ghost = ghost[currentInput.Length..];
					Console.Write(prompt + currentInput);
					var (Left, Top) = Console.GetCursorPosition();
					Console.ForegroundColor = ConsoleColor.DarkGray;
					Console.Write(ghost);
					Console.ResetColor();
					Console.SetCursorPosition(Left, Top);
				}
				if (arg3 != -1 && argMatchesTertiary.Any())
				{
					ClearLine(Console.CursorTop);
					ghost = argMatchesTertiary.First();
					ghost = string.Concat(commandInput,' ',argPrimaryInput,' ',argSecondaryInput,' ',ghost);
					ghost = ghost[currentInput.Length..];
					Console.Write(prompt + currentInput);
					var (Left, Top) = Console.GetCursorPosition();
					Console.ForegroundColor = ConsoleColor.DarkGray;
					Console.Write(ghost);
					Console.ResetColor();
					Console.SetCursorPosition(Left, Top);
				}
			}
		}
		static void ClearLine(int top)
		{
			int width = Math.Max(1, Console.BufferWidth);
			Console.SetCursorPosition(0, top);
			Console.Write(new string(' ', Math.Max(0, width - 1)));
			Console.SetCursorPosition(0, top);
		}
		static void PrintInstalledToolsByAuthor(string authorExact)
		{
			var tools = GetInstalledGlobalTools().Where(t => PackageAuthorsContainExact(t.PackageId, t.Version, authorExact)).SelectMany(t => t.Commands).Distinct().OrderBy(x => x).ToList();
			if (tools.Count == 0)
			{
				Console.WriteLine(" 📂 Installed Tools:");
				Console.WriteLine("    🚫 None");
				return;
			}
			Console.WriteLine(" 📂 Installed Tools:");
			if (installedTools.Contains("ToolBox")) Console.WriteLine("    🧰 ToolBox:");
			for (int i = 0; i < installedTools.Count; i++)
			{
				if (installedTools[i] == "ToolBox") continue;
				Console.WriteLine($"       🔧 {allTools[i]}");
			}
		}
		static List<string> GetInstalledToolCommandsByAuthor(string authorExact)
		{
			return [.. GetInstalledGlobalTools().Where(t => PackageAuthorsContainExact(t.PackageId, t.Version, authorExact)).SelectMany(t => t.Commands).Distinct().OrderBy(x => x)];
		}
		static string GetToolVersion()
		{
			var asm = typeof(ToolBox).Assembly;
			var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
			if (!string.IsNullOrWhiteSpace(info))
			{
				int plus = info.IndexOf('+');
				if (plus >= 0) info = info[..plus];
				return info;
			}
			return asm.GetName().Version?.ToString() ?? "0.0.0";
		}
		static void ToolRunner(string input)
		{
			var tokens = Tokenise(input);
			if (tokens.Count == 0) return;
			var tool = tokens[0];
			if (tool == "None") { Console.WriteLine(prompt + "None means none dumbass."); return; }
			if (tool == "ToolBox") { Console.WriteLine(prompt + "ToolBox is already running."); return; }
			if (tool == "SteeleTerm" && tokens.Any(t => t == "--fileBrowser" || t == "--serial" || t == "--ssh")) { RunPassthroughTool(tokens); return; }
			var psi = new ProcessStartInfo
			{
				FileName = tool,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				RedirectStandardInput = true,
				CreateNoWindow = true,
				StandardOutputEncoding = System.Text.Encoding.UTF8,
				StandardErrorEncoding = System.Text.Encoding.UTF8,
				StandardInputEncoding = System.Text.Encoding.UTF8
			};
			for (int i = 1; i < tokens.Count; i++) psi.ArgumentList.Add(tokens[i]);
			using var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
			const string sentinel = "🔍 Verifying parent is ToolBox...";
			var spinner = new ConsoleSpinner(outputLock, prompt, 100, 0);
			int acked = 0;
			p.OutputDataReceived += (_, e) => {
				if (e.Data == null) return;
				var raw = e.Data;
				int cr = raw.LastIndexOf('\r');
				var line = cr >= 0 ? raw[(cr + 1)..] : raw;
				var trimmed = line.Trim();
				if (string.Equals(trimmed, sentinel, StringComparison.Ordinal))
				{
					lock (outputLock) { if (line.Length == 0) Console.WriteLine(); else Console.WriteLine(prompt + line); }
					spinner.Start("⏳ Waiting for ToolBox");
					if (Interlocked.Exchange(ref acked, 1) == 0) { try { p.StandardInput.WriteLine("ToolBox is open"); p.StandardInput.Flush(); } catch { } }
					return;
				}
				if (spinner.Active) { spinner.Enqueue(line, false); spinner.RequestStopAndFlush(); return; }
				lock (outputLock) { if (Console.CursorLeft != 0) Console.WriteLine(); if (line.Length == 0) Console.WriteLine(); else Console.WriteLine(prompt + line); }
			};
			p.ErrorDataReceived += (_, e) => {
				if (e.Data == null) return;
				var line = e.Data.Replace("\r", "");
				if (spinner.Active) { spinner.Enqueue(line, true); spinner.RequestStopAndFlush(); return; }
				lock (outputLock) { if (Console.CursorLeft != 0) Console.WriteLine(); if (line.Length == 0) Console.WriteLine(); else Console.Error.WriteLine(prompt + line); }
			};
			try { p.Start(); }
			catch (Exception ex) { lock (outputLock) Console.WriteLine(prompt + ex.Message); return; }
			p.BeginOutputReadLine();
			p.BeginErrorReadLine();
			try { p.WaitForExit(); }
			finally { spinner.StopAndFlush(); }
		}
		static List<string> Tokenise(string input)
		{
			var tokens = new List<string>();
			var cur = "";
			bool inQuotes = false;
			for (int i = 0; i < input.Length; i++)
			{
				char c = input[i];
				if (c == '"') { inQuotes = !inQuotes; continue; }
				if (!inQuotes && char.IsWhiteSpace(c)) { if (cur.Length > 0) { tokens.Add(cur); cur = ""; } continue; }
				cur += c;
			}
			if (cur.Length > 0) tokens.Add(cur);
			return tokens;
		}
		sealed class ToolRow
		{
			public string PackageId { get; init; } = "";
			public string Version { get; init; } = "";
			public List<string> Commands { get; init; } = [];
		}
		static List<ToolRow> GetInstalledGlobalTools()
		{
			var output = RunAndCapture("dotnet", "tool list --global");
			var lines = output.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
			var result = new List<ToolRow>();
			foreach (var raw in lines)
			{
				var line = raw.TrimEnd();
				if (line.StartsWith("Package", StringComparison.Ordinal)) continue;
				if (line.All(ch => ch == '-' || ch == ' ')) continue;
				var parts = SplitByritespace(line);
				if (parts.Count < 3) continue;
				var packageId = parts[0];
				var version = parts[1];
				var commands = parts.Skip(2).SelectMany(p => p.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
				result.Add(new ToolRow { PackageId = packageId, Version = version, Commands = commands });
			}
			return result;
		}
		static bool PackageAuthorsContainExact(string packageId, string version, string authorExact)
		{
			string? nuspecPath = FindNuspecInDotnetToolStore(packageId, version) ?? FindNuspecInGlobalPackages(packageId, version);
			if (nuspecPath == null) return false;
			try
			{
				var doc = XDocument.Load(nuspecPath);
				var authorsElement = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "authors");
				if (authorsElement == null) return false;
				var authors = authorsElement.Value.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()).ToList();
				return authors.Any(a => a == authorExact);
			}
			catch { return false; }
		}
		static string? FindNuspecInDotnetToolStore(string packageId, string version)
		{
			var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			if (string.IsNullOrWhiteSpace(home)) return null;
			var storeRoot = Path.Combine(home, ".dotnet", "tools", ".store");
			var pkgRoot = Path.Combine(storeRoot, packageId.ToLowerInvariant(), version.ToLowerInvariant());
			if (!Directory.Exists(pkgRoot)) return null;
			return Directory.EnumerateFiles(pkgRoot, "*.nuspec", SearchOption.AllDirectories).FirstOrDefault();
		}
		static string? FindNuspecInGlobalPackages(string packageId, string version)
		{
			var globalPackages = GetGlobalPackagesFolder();
			if (string.IsNullOrWhiteSpace(globalPackages)) return null;
			var pkgRoot = Path.Combine(globalPackages, packageId.ToLowerInvariant(), version.ToLowerInvariant());
			if (!Directory.Exists(pkgRoot)) return null;
			return Directory.EnumerateFiles(pkgRoot, "*.nuspec", SearchOption.TopDirectoryOnly).FirstOrDefault();
		}
		static string GetGlobalPackagesFolder()
		{
			var output = RunAndCapture("dotnet", "nuget locals global-packages --list");
			var idx = output.IndexOf("global-packages:", StringComparison.Ordinal);
			if (idx < 0) return "";
			var after = output[(idx + "global-packages:".Length)..].Trim();
			var firstLine = after.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
			return firstLine.Trim();
		}
		static string RunAndCapture(string fileName, string arguments)
		{
			var psi = new ProcessStartInfo
			{
				FileName = fileName,
				Arguments = arguments,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};
			using var p = Process.Start(psi);
			if (p == null) return "";
			var stdout = p.StandardOutput.ReadToEnd();
			var stderr = p.StandardError.ReadToEnd();
			p.WaitForExit();
			return (stdout + "\n" + stderr).Trim();
		}
		static List<string> SplitByritespace(string s)
		{
			var list = new List<string>();
			int i = 0;
			while (i < s.Length)
			{
				while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
				if (i >= s.Length) break;
				int start = i;
				while (i < s.Length && !char.IsWhiteSpace(s[i])) i++;
				list.Add(s[start..i]);
			}
			return list;
		}
		static void RunPassthroughTool(List<string> tokens)
		{
			var psi = new ProcessStartInfo
			{
				FileName = tokens[0],
				UseShellExecute = false,
				RedirectStandardOutput = false,
				RedirectStandardError = false,
				RedirectStandardInput = false,
				CreateNoWindow = false
			};
			psi.Environment["TOOLBOX_HOST"] = "1";
			psi.Environment["TOOLBOX_PREFIX"] = prompt;
			for (int i = 1; i < tokens.Count; i++) psi.ArgumentList.Add(tokens[i]);
			using var p = Process.Start(psi);
			if (p == null) return;
			p.WaitForExit();
		}
		static void BuildAutocomplete(List<string> installedTools)
		{
			if (installedTools == null || installedTools.Count == 0) return;
			var seen = new HashSet<string>(StringComparer.Ordinal);
			for (int i = 0; i < installedTools.Count; i++)
			{
				var cmd = installedTools[i];
				if (string.IsNullOrWhiteSpace(cmd) || cmd == "None") continue;
				if (!seen.Add(cmd)) continue;
				if (cmd == "ToolBox") continue;
				string text = "";
				try { text = RunAndCapture(cmd, "--help"); } catch { continue; }
				if (string.IsNullOrWhiteSpace(text)) { continue; }
				using StringReader currentLine = new(text);
				while (true)
				{
					var autocompleteDictionary = "";
					int argStart;
					int argEnd;
					List<string> args = [];
					string updateArgCheck;
					var line = currentLine.ReadLine();
					if (line == null) break;
					line = line.Trim();
					if (line.Length == 0) continue;
					if (line.StartsWith("Primary args:", StringComparison.Ordinal)) autocompleteDictionary = "Primary";
					while (autocompleteDictionary == "Primary")
					{
						line = currentLine.ReadLine();
						if (line == null) break;
						line = line.Trim();
						if (line.Length == 0) { continue; }
						argStart = line.IndexOf('-');
						if (argStart != -1)
						{
							argEnd = line.IndexOf(' ', argStart);
							if (line[argEnd - 1] == '\'') argEnd--;
							args.Add(line[argStart..argEnd]);
						}
						if (line.StartsWith("Secondary args:", StringComparison.Ordinal))
						{
							args = [.. args.OrderBy(x => x, StringComparer.Ordinal)];
							argsPrimary[cmd] = [.. args];
							args.Clear();
							autocompleteDictionary = "Secondary";
							break;
						}
					}
					while (autocompleteDictionary == "Secondary")
					{
						line = currentLine.ReadLine();
						if (line == null) break;
						line = line.Trim();
						if (line.Length == 0) { continue; }
						argStart = line.IndexOf('-');
						if (argStart != -1)
						{
							argEnd = line.IndexOf(' ', argStart);
							if (line[argEnd - 1] == '\'') argEnd--;
							updateArgCheck = line[argStart..argEnd];
							if (updateArgCheck == "--forceUpdate") continue;
							args.Add(line[argStart..argEnd]);
						}
						if (line.StartsWith("Tertiary args:", StringComparison.Ordinal))
						{
							args = [.. args.OrderBy(x => x, StringComparer.Ordinal)];
							argsSecondary[cmd] = [.. args];
							args.Clear();
							autocompleteDictionary = "Tertiary";
							break;
						}
					}
					while (autocompleteDictionary == "Tertiary")
					{
						line = currentLine.ReadLine();
						if (line == null) break;
						line = line.Trim();
						if (line.Length == 0) { continue; }
						argStart = line.IndexOf('-');
						if (argStart != -1)
						{
							argEnd = line.IndexOf(' ', argStart);
							if (line[argEnd - 1] == '\'') argEnd--;
							updateArgCheck = line[argStart..argEnd];
							if (updateArgCheck == "--skipVersion") continue;
							args.Add(line[argStart..argEnd]);
						}
					}
					if (autocompleteDictionary == "Tertiary")
					{
						args = [.. args.OrderBy(x => x, StringComparer.Ordinal)];
						argsTertiary[cmd] = [.. args];
						args.Clear();
						break;
					}
				}
			}
		}
	}
}