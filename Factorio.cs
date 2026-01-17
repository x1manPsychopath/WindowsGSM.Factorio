using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.IO.Compression;
using System.Net.Http;
using WindowsGSM.GameServer.Engine;
using WindowsGSM.Functions;
using WindowsGSM.GameServer;

namespace WindowsGSM.Plugins
{
    public class Factorio : SteamCMDAgent
    {
        public Plugin Plugin = new Plugin
        {
            name = "Factorio",
            author = "x1manPsychopath",
            description = "Factorio Dedicated Server plugin with mod support",
            version = "3.0",
            url = "https://github.com/x1manPsychopath/WindowsGSM.Factorio",
            color = "#E69500"
        };

        public override string AppId => "894490";
        public override bool loginAnonymous => true;
        public override string StartPath => "bin/Factorio.exe";

        private string ModsDir => ServerPath.GetServersServerFiles(_serverData.ServerID, "mods");
        private string ModListPath => Path.Combine(ModsDir, "mod-list.json");

        public Factorio(ServerConfig serverData) : base(serverData) { }

        // -----------------------------
        //  SERVER CONFIG GENERATION
        // -----------------------------
        private void EnsureServerSettings()
        {
            string path = ServerPath.GetServersServerFiles(_serverData.ServerID, "server-settings.json");

            if (!File.Exists(path))
            {
                string json =
@"{
  ""name"": ""Factorio Server"",
  ""description"": ""WindowsGSM Managed Server"",
  ""tags"": [""windowsgsm""],
  ""max_players"": 10,
  ""visibility"": ""public"",
  ""username"": """",
  ""password"": """",
  ""token"": """"
}";
                File.WriteAllText(path, json);
            }
        }

        private void EnsureDefaultSave()
        {
            string savesDir = ServerPath.GetServersServerFiles(_serverData.ServerID, "saves");
            Directory.CreateDirectory(savesDir);

            string savePath = Path.Combine(savesDir, "default.zip");
            string exe = ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);

            if (!File.Exists(savePath) && File.Exists(exe))
            {
                var p = new Process();
                p.StartInfo.FileName = exe;
                p.StartInfo.Arguments = $"--create \"{savePath}\"";
                p.StartInfo.WorkingDirectory = Path.GetDirectoryName(exe);
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.WaitForExit();
            }
        }

        // -----------------------------
        //  MOD SUPPORT CORE
        // -----------------------------
        private class ModListEntry
        {
            public string Name;
            public bool Enabled;
        }

        private List<ModListEntry> LoadModList()
        {
            var list = new List<ModListEntry>();

            if (!File.Exists(ModListPath))
            {
                // base only
                list.Add(new ModListEntry { Name = "base", Enabled = true });
                SaveModList(list);
                return list;
            }

            var lines = File.ReadAllLines(ModListPath);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("{") && trimmed.Contains("\"name\""))
                {
                    string name = ExtractJsonValue(trimmed, "name");
                    string enabledStr = ExtractJsonValue(trimmed, "enabled");
                    bool enabled = enabledStr.Equals("true", StringComparison.OrdinalIgnoreCase);
                    if (!string.IsNullOrEmpty(name))
                    {
                        list.Add(new ModListEntry { Name = name, Enabled = enabled });
                    }
                }
            }

            if (!list.Any(m => m.Name == "base"))
            {
                list.Insert(0, new ModListEntry { Name = "base", Enabled = true });
            }

            return list;
        }

        private void SaveModList(List<ModListEntry> mods)
        {
            Directory.CreateDirectory(ModsDir);

            using (var sw = new StreamWriter(ModListPath, false))
            {
                sw.WriteLine("{");
                sw.WriteLine("  \"mods\": [");

                for (int i = 0; i < mods.Count; i++)
                {
                    var m = mods[i];
                    string line =
                        $"    {{ \"name\": \"{m.Name}\", \"enabled\": {(m.Enabled ? "true" : "false")} }}";
                    if (i < mods.Count - 1) line += ",";
                    sw.WriteLine(line);
                }

                sw.WriteLine("  ]");
                sw.WriteLine("}");
            }
        }

        private string ExtractJsonValue(string jsonLine, string key)
        {
            // naive but safe enough for our own mod-list.json
            string pattern = $"\"{key}\"";
            int idx = jsonLine.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (idx == -1) return string.Empty;

            idx = jsonLine.IndexOf(":", idx);
            if (idx == -1) return string.Empty;

            idx++;
            while (idx < jsonLine.Length && char.IsWhiteSpace(jsonLine[idx])) idx++;

            if (idx >= jsonLine.Length) return string.Empty;

            if (jsonLine[idx] == '\"')
            {
                idx++;
                int end = jsonLine.IndexOf("\"", idx);
                if (end == -1) return string.Empty;
                return jsonLine.Substring(idx, end - idx);
            }
            else
            {
                int end = jsonLine.IndexOfAny(new[] { ',', '}' }, idx);
                if (end == -1) end = jsonLine.Length;
                return jsonLine.Substring(idx, end - idx).Trim();
            }
        }

        private void EnsureModSupport()
        {
            Directory.CreateDirectory(ModsDir);

            if (!File.Exists(ModListPath))
            {
                var list = new List<ModListEntry>
                {
                    new ModListEntry { Name = "base", Enabled = true }
                };
                SaveModList(list);
            }

            AutoEnableMods();
        }

        private void AutoEnableMods()
        {
            Directory.CreateDirectory(ModsDir);

            var mods = Directory.GetFiles(ModsDir, "*.zip");
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var mod in mods)
            {
                string file = Path.GetFileNameWithoutExtension(mod);
                string modName = file.Contains("_") ? file.Substring(0, file.LastIndexOf("_")) : file;
                if (!string.Equals(modName, "base", StringComparison.OrdinalIgnoreCase))
                {
                    names.Add(modName);
                }
            }

            var list = LoadModList();

            foreach (var name in names)
            {
                var existing = list.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                {
                    list.Add(new ModListEntry { Name = name, Enabled = true });
                }
                else
                {
                    existing.Enabled = true;
                }
            }

            SaveModList(list);
        }

        private void DisableAllMods()
        {
            var list = LoadModList();

            foreach (var m in list)
            {
                if (!string.Equals(m.Name, "base", StringComparison.OrdinalIgnoreCase))
                {
                    m.Enabled = false;
                }
            }

            SaveModList(list);
        }

        public void ImportModpack(string path)
        {
            Directory.CreateDirectory(ModsDir);

            if (Directory.Exists(path))
            {
                foreach (var file in Directory.GetFiles(path, "*.zip"))
                {
                    File.Copy(file, Path.Combine(ModsDir, Path.GetFileName(file)), true);
                }
            }
            else if (File.Exists(path) && path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                string temp = Path.Combine(Path.GetTempPath(), "factorio_modpack_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(temp);

                ZipFile.ExtractToDirectory(path, temp);

                foreach (var file in Directory.GetFiles(temp, "*.zip", SearchOption.AllDirectories))
                {
                    File.Copy(file, Path.Combine(ModsDir, Path.GetFileName(file)), true);
                }

                Directory.Delete(temp, true);
            }

            AutoEnableMods();
        }

        public async Task AutoUpdateMods()
        {
            Directory.CreateDirectory(ModsDir);
            var mods = Directory.GetFiles(ModsDir, "*.zip");

            using var client = new HttpClient();

            foreach (var mod in mods)
            {
                string file = Path.GetFileNameWithoutExtension(mod);
                string modName = file.Contains("_") ? file.Substring(0, file.LastIndexOf("_")) : file;

                if (string.Equals(modName, "base", StringComparison.OrdinalIgnoreCase))
                    continue;

                string apiUrl = $"https://mods.factorio.com/api/mods/{modName}";

                try
                {
                    string json = await client.GetStringAsync(apiUrl);
                    string latestUrl = ExtractLatestDownloadUrl(json);
                    if (!string.IsNullOrEmpty(latestUrl))
                    {
                        string newFileName = Path.GetFileName(new Uri(latestUrl).LocalPath);
                        string newModPath = Path.Combine(ModsDir, newFileName);

                        var data = await client.GetByteArrayAsync(latestUrl);
                        await File.WriteAllBytesAsync(newModPath, data);

                        File.Delete(mod);
                    }
                }
                catch
                {
                    // ignore failed updates
                }
            }

            AutoEnableMods();
        }

        private string ExtractLatestDownloadUrl(string json)
        {
            const string key = "\"download_url\":\"";
            int idx = json.IndexOf(key, StringComparison.Ordinal);
            if (idx == -1) return null;

            idx += key.Length;
            int end = json.IndexOf("\"", idx, StringComparison.Ordinal);
            if (end == -1) return null;

            string url = json.Substring(idx, end - idx);
            return "https://mods.factorio.com" + url;
        }

        private string GetInstalledModsSummary()
        {
            Directory.CreateDirectory(ModsDir);
            var list = LoadModList();

            if (list == null || list.Count == 0)
                return "No mods found.";

            var lines = new List<string>();
            foreach (var m in list)
            {
                lines.Add($"{m.Name} - {(m.Enabled ? "Enabled" : "Disabled")}");
            }

            return string.Join(Environment.NewLine, lines);
        }

        private string CheckModConflicts()
        {
            Directory.CreateDirectory(ModsDir);
            var mods = Directory.GetFiles(ModsDir, "*.zip");

            var nameToFiles = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var mod in mods)
            {
                string file = Path.GetFileNameWithoutExtension(mod);
                string modName = file.Contains("_") ? file.Substring(0, file.LastIndexOf("_")) : file;

                if (!nameToFiles.ContainsKey(modName))
                    nameToFiles[modName] = new List<string>();

                nameToFiles[modName].Add(Path.GetFileName(mod));
            }

            var conflicts = nameToFiles.Where(kv => kv.Value.Count > 1).ToList();

            if (!conflicts.Any())
                return "No obvious conflicts detected (no duplicate mod names).";

            var lines = new List<string> { "Potential conflicts detected (multiple versions of same mod):" };
            foreach (var kv in conflicts)
            {
                lines.Add($"{kv.Key}: {string.Join(", ", kv.Value)}");
            }

            return string.Join(Environment.NewLine, lines);
        }

        // -----------------------------
        //  SERVER START
        // -----------------------------
        public override async Task<Process> Start()
        {
            EnsureServerSettings();
            EnsureDefaultSave();
            EnsureModSupport();

            string exe = ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);
            string settings = ServerPath.GetServersServerFiles(_serverData.ServerID, "server-settings.json");

            string args =
                $"--start-server-load-latest " +
                $"--server-settings \"{settings}\" " +
                $"--port {_serverData.ServerPort}";

            var p = new Process();
            p.StartInfo.FileName = exe;
            p.StartInfo.Arguments = args;
            p.StartInfo.WorkingDirectory = Path.GetDirectoryName(exe);
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = false;

            p.Start();
            return p;
        }

        // -----------------------------
        //  WINDOWSGSM UI MENU
        // -----------------------------
        public override PluginMenu GetMenu()
        {
            var menu = new PluginMenu();

            menu.Add("Mods ▸ Import Modpack", async () =>
            {
                string path = await UI.SelectFileOrFolder("Select a modpack (.zip or folder)");
                if (!string.IsNullOrEmpty(path))
                {
                    ImportModpack(path);
                    UI.ShowMessage("Modpack imported and mods enabled.");
                }
            });

            menu.Add("Mods ▸ Enable All Mods", () =>
            {
                AutoEnableMods();
                UI.ShowMessage("All mods enabled.");
            });

            menu.Add("Mods ▸ Disable All Mods", () =>
            {
                DisableAllMods();
                UI.ShowMessage("All non-base mods disabled.");
            });

            menu.Add("Mods ▸ Auto‑Update Mods", async () =>
            {
                await AutoUpdateMods();
                UI.ShowMessage("Mods updated to latest versions (where possible).");
            });

            menu.Add("Mods ▸ View Installed Mods", () =>
            {
                string summary = GetInstalledModsSummary();
                UI.ShowMessage(summary);
            });

            menu.Add("Mods ▸ Check for Conflicts", () =>
            {
                string report = CheckModConflicts();
                UI.ShowMessage(report);
            });

            return menu;
        }
    }
}
