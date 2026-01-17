using System;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Engine;
using WindowsGSM.GameServer.Query;
using System.IO;

namespace WindowsGSM.Plugins
{
    public class Factorio : SteamCMDAgent
    {
        public Plugin Plugin = new Plugin
        {
            name = "WindowsGSM.Factorio",
            author = "x1manPsychopath",
            description = "WindowsGSM plugin for Factorio Dedicated Server",
            version = "2.0",
            url = "https://github.com/",
            color = "#FFD700"
        };

        public Factorio(ServerConfig serverData) : base(serverData)
            => base.serverData = _serverData = serverData;

        private readonly ServerConfig _serverData;

        // Correct dedicated server App ID
        public override string AppId => "894490";

        // Dedicated server supports anonymous login
        public override bool loginAnonymous => true;

        // Correct executable path for dedicated server
        public override string StartPath => @"bin/Factorio.exe";

        public string FullName = "Factorio with x1manPsychopath and Friends Server";
        public bool AllowsEmbedConsole = true;
        public int PortIncrements = 1;
        public object QueryMethod = new A2S();

        public string DefaultSaveName => $"{_serverData.ServerMap}_save.zip";

        // Create default config + save file
        public async void CreateServerCFG()
        {
            string serverFiles = ServerPath.GetServersServerFiles(_serverData.ServerID);

            string dataDir = Path.Combine(serverFiles, "data");
            Directory.CreateDirectory(dataDir);

            string settingsPath = Path.Combine(dataDir, "server-settings.json");

            if (!File.Exists(settingsPath))
            {
                File.WriteAllText(settingsPath,
@"{
  ""name"": ""Factorio with x1manPsychopath and Friends Server"",
  ""description"": ""WindowsGSM Managed Server"",
  ""tags"": [""windowsgsm""],
  ""max_players"": 10,
  ""visibility"": ""public""
}");
            }

            // Create save if missing
            string savePath = Path.Combine(serverFiles, DefaultSaveName);
            if (!File.Exists(savePath))
            {
                string exe = Path.Combine(serverFiles, StartPath);
                if (File.Exists(exe))
                {
                    var p = new Process
                    {
                        StartInfo =
                        {
                            FileName = exe,
                            WorkingDirectory = Path.GetDirectoryName(exe),
                            Arguments = $"--create \"{savePath}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    p.Start();
                    p.WaitForExit();
                }
            }
        }

        public async Task<Process> Start()
        {
            string serverFiles = ServerPath.GetServersServerFiles(_serverData.ServerID);
            string exePath = Path.Combine(serverFiles, StartPath);

            if (!File.Exists(exePath))
            {
                Error = $"Missing executable: {exePath}";
                return null;
            }

            string savePath = Path.Combine(serverFiles, DefaultSaveName);
            if (!File.Exists(savePath))
            {
                Error = $"Save file not found: {savePath}";
                return null;
            }

            string settingsPath = Path.Combine(serverFiles, "data/server-settings.json");

            var param = new StringBuilder();
            param.Append($" --start-server \"{savePath}\"");
            param.Append($" --server-settings \"{settingsPath}\"");
            param.Append($" --port {_serverData.ServerPort}");

            if (!string.IsNullOrWhiteSpace(_serverData.ServerParam))
                param.Append($" {_serverData.ServerParam}");

            var p = new Process
            {
                StartInfo =
                {
                    FileName = exePath,
                    WorkingDirectory = Path.GetDirectoryName(exePath),
                    Arguments = param.ToString(),
                    UseShellExecute = false,
                    RedirectStandardInput = AllowsEmbedConsole,
                    RedirectStandardOutput = AllowsEmbedConsole,
                    RedirectStandardError = AllowsEmbedConsole,
                    CreateNoWindow = AllowsEmbedConsole
                },
                EnableRaisingEvents = true
            };

            if (AllowsEmbedConsole)
            {
                var console = new ServerConsole(_serverData.ServerID);
                p.OutputDataReceived += console.AddOutput;
                p.ErrorDataReceived += console.AddOutput;
            }

            try
            {
                p.Start();
                if (AllowsEmbedConsole)
                {
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                }
                return p;
            }
            catch (Exception e)
            {
                Error = e.Message;
                return null;
            }
        }

        public async Task Stop(Process p)
        {
            await Task.Run(() =>
            {
                try
                {
                    if (p.StartInfo.RedirectStandardInput)
                        p.StandardInput.WriteLine("quit");
                    else
                        p.Kill();
                }
                catch { }
            });
        }

        public override bool IsInstallValid()
        {
            return File.Exists(ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath));
        }

        public override bool IsImportValid(string path)
        {
            return File.Exists(Path.Combine(path, StartPath));
        }
    }
}