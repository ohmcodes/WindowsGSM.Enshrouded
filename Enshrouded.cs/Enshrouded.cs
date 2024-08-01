using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Query;
using WindowsGSM.GameServer.Engine;
using System.IO;
using Newtonsoft.Json;

namespace WindowsGSM.Plugins
{
    public class Enshrouded: SteamCMDAgent
    {
        // - Plugin Details
        public Plugin Plugin = new Plugin
        {
            name = "WindowsGSM.Enshrouded", // WindowsGSM.XXXX
            author = "ohmcodes",
            description = "WindowsGSM plugin for supporting Enshrouded Dedicated Server",
            version = "1.0.3",
            url = "https://github.com/ohmcodes/WindowsGSM.Enshrouded", // Github repository link (Best practice)
            color = "#34c9eb" // Color Hex
        };

        // - Settings properties for SteamCMD installer
        public override bool loginAnonymous => true;
        public override string AppId => "2278520"; // Game server appId Steam

        // - Standard Constructor and properties
        public Enshrouded(ServerConfig serverData) : base(serverData) => base.serverData = _serverData = serverData;
        private readonly ServerConfig _serverData;
        public string Error, Notice;


        // - Game server Fixed variables
        public override string StartPath => "enshrouded_server.exe"; // Game server start path
        public string FullName = "Enshrouded Dedicated Server"; // Game server FullName
        public bool AllowsEmbedConsole = true;  // Does this server support output redirect?
        public int PortIncrements = 1; // This tells WindowsGSM how many ports should skip after installation

        // TODO: Undisclosed method
        public object QueryMethod = new A2S(); // Query method should be use on current server type. Accepted value: null or new A2S() or new FIVEM() or new UT3()

        // - Game server default values
        public string Port = "15636"; // Default port
        public string QueryPort = "15637"; // Default query port. This is the port specified in the Server Manager in the client UI to establish a server connection.
        // TODO: Unsupported option
        public string Defaultmap = "Dedicated"; // Default map name

        // TODO: May not support
        public string Maxplayers = "16"; // Default maxplayers

        public string Additional = "-log"; // Additional server start parameter

        // Random Passwords for UserGroups
        public string AdminPassword;
        public string FriendPassword;
        public string GuestPassword;

        private static Random random = new Random();

        private string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public void GeneratePasswords()
        {
            AdminPassword = GenerateRandomString(8);
            FriendPassword = GenerateRandomString(8);
            GuestPassword = GenerateRandomString(8);
        }

        // - Create a default cfg for the game server after installation
        public async void CreateServerCFG()
        {
            GeneratePasswords();

            var serverConfig = new
            {
                name = $"{_serverData.ServerName}",
                password = "",
                saveDirectory = "./savegame",
                logDirectory = "./logs",
                ip = $"{_serverData.ServerIP}",
                gamePort = Int32.Parse(_serverData.ServerPort),
                queryPort = Int32.Parse(_serverData.ServerQueryPort),
                slotCount = Int32.Parse(_serverData.ServerMaxPlayer),
                gameSettingsPreset = "Default",
                gameSettings = new
                {
                    playerHealthFactor = 1,
                    playerManaFactor = 1,
                    playerStaminaFactor = 1,
                    enableDurability = true,
                    enableStarvingDebuff = false,
                    foodBuffDurationFactor = 1,
                    fromHungerToStarving = 600000000000,
                    shroudTimeFactor = 1,
                    tombstoneMode = "AddBackpackMaterials",
                    miningDamageFactor = 1,
                    plantGrowthSpeedFactor = 1,
                    resourceDropStackAmountFactor = 1,
                    factoryProductionSpeedFactor = 1,
                    perkUpgradeRecyclingFactor = 0.500000,
                    perkCostFactor = 1,
                    experienceCombatFactor = 1,
                    experienceMiningFactor = 1,
                    experienceExplorationQuestsFactor = 1,
                    randomSpawnerAmount = "Normal",
                    aggroPoolAmount = "Normal",
                    enemyDamageFactor = 1,
                    enemyHealthFactor = 1,
                    enemyStaminaFactor = 1,
                    enemyPerceptionRangeFactor = 1,
                    bossDamageFactor = 1,
                    bossHealthFactor = 1,
                    threatBonus = 1,
                    pacifyAllEnemies = false,
                    dayTimeDuration = 1800000000000,
                    nightTimeDuration = 720000000000
                },
                userGroups = new[]
                {
                    new
                    {
                        name = "Admin",
                        password = AdminPassword,
                        canKickBan = true,
                        canAccessInventories = true,
                        canEditBase = true,
                        canExtendBase = true,
                        reservedSlots = 0
                    },
                    new
                    {
                        name = "Friend",
                        password = FriendPassword,
                        canKickBan = false,
                        canAccessInventories = true,
                        canEditBase = true,
                        canExtendBase = false,
                        reservedSlots = 0
                    },
                    new
                    {
                        name = "Guest",
                        password = GuestPassword,
                        canKickBan = false,
                        canAccessInventories = false,
                        canEditBase = false,
                        canExtendBase = false,
                        reservedSlots = 0
                    }
                }
            };

            // Convert the object to JSON format
            string jsonContent = JsonConvert.SerializeObject(serverConfig, Formatting.Indented);

            // Specify the file path
            string filePath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, "enshrouded_server.json");

            // Write the JSON content to the file
            File.WriteAllText(filePath, jsonContent);
        }

        // - Start server function, return its Process to WindowsGSM
        public async Task<Process> Start()
        {
            string shipExePath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);
            if (!File.Exists(shipExePath))
            {
                Error = $"{Path.GetFileName(shipExePath)} not found ({shipExePath})";
                return null;
            }

            // Prepare start parameter
            string param = $" {_serverData.ServerParam} ";
			param += $"-ip=\"{_serverData.ServerIP}\" ";
            param += $"-gamePort={_serverData.ServerPort} ";
            param += $"-queryPort={_serverData.ServerQueryPort} ";
            param += $"-slotCount={_serverData.ServerMaxPlayer} ";
            param += $"-name=\"\"\"{_serverData.ServerName}\"\"\"";

            // Prepare Process
            var p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = ServerPath.GetServersServerFiles(_serverData.ServerID),
                    FileName = shipExePath,
                    Arguments = param.ToString(),
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };

            // Set up Redirect Input and Output to WindowsGSM Console if EmbedConsole is on
            if (AllowsEmbedConsole)
            {
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                var serverConsole = new ServerConsole(_serverData.ServerID);
                p.OutputDataReceived += serverConsole.AddOutput;
                p.ErrorDataReceived += serverConsole.AddOutput;
            }

            // Start Process
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
                return null; // return null if fail to start
            }
        }

        // - Stop server function
        public async Task Stop(Process p)
        {
            await Task.Run(() =>
            {
                Functions.ServerConsole.SetMainWindow(p.MainWindowHandle);
                Functions.ServerConsole.SendWaitToMainWindow("^c");
                p.WaitForExit(2000);
            });
        }
        public async Task<Process> Install()
        {
            var steamCMD = new Installer.SteamCMD();
            Process p = await steamCMD.Install(_serverData.ServerID, string.Empty, AppId, true, loginAnonymous);
            Error = steamCMD.Error;
            return p;
        }
        public async Task<Process> Update(bool validate = false, string custom = null)
        {
            var (p, error) = await Installer.SteamCMD.UpdateEx(serverData.ServerID, AppId, validate, custom: custom, loginAnonymous: loginAnonymous);
            Error = error;
            await Task.Run(() => { p.WaitForExit(); });

            return p;
        }

        public bool IsInstallValid()
        {
            return File.Exists(ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath));
        }
        public bool IsImportValid(string path)
        {
            string importPath = Path.Combine(path, StartPath);
            Error = $"Invalid Path! Fail to find {Path.GetFileName(StartPath)}";
            return File.Exists(importPath);
        }
        public string GetLocalBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return steamCMD.GetLocalBuild(_serverData.ServerID, AppId);
        }
        public async Task<string> GetRemoteBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return await steamCMD.GetRemoteBuild(AppId);
        }
    }
}
