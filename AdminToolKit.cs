using System;
using System.Linq;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Admin Tool Kit", "ITU", "1.0.0")]
    [Description("Some convenient tool for server admins.")]
    internal class AdminToolKit : RustPlugin
    {
        #region Field

        Configuration _config;

        private int _antiHackUserlevel = 2;

        private const string TerrainPerm = "admintoolkit.TerrainKick";
        private const string AutoTimePerm = "admintoolkit.AutoTime";
        private const string AutoRecoverPerm = "admintoolkit.AutoRecover";
        private const string AutoGodPerm = "admintoolkit.AutoGod";

        #endregion

        #region Config

        public class Configuration
        {
            [JsonProperty(PropertyName = "Auto Time (-1 to disable): ")]
            public readonly int Time = 12;

            [JsonProperty(PropertyName = "Disable inside terrain kicks")]
            public readonly bool DisableTerrainKick = true;

            [JsonProperty(PropertyName = "Enable Auto Recover")]
            public readonly bool AutoRecover = true;

            [JsonProperty(PropertyName = "Enable Auto God mode")]
            public readonly bool AutoGod = true;
        }
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try 
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new Exception();
            }
            catch 
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
            }
        }
       
        protected override void SaveConfig() => Config.WriteObject(_config);

        protected override void LoadDefaultConfig() => _config = new Configuration();
        
        #endregion

        #region Hooks

        private void Init()
        {
            LoadConfig();
            RegisterPermission();

            _antiHackUserlevel = ConVar.AntiHack.userlevel;
            if (_config.DisableTerrainKick) ConVar.AntiHack.userlevel = 0;
            
            foreach (var player in BasePlayer.activePlayerList.Where(player => permission.UserHasPermission(player.UserIDString, AutoTimePerm)))
            {
                AutoTime(player);
            }

            foreach (var player in BasePlayer.activePlayerList.Where(player => permission.UserHasPermission(player.UserIDString, AutoRecoverPerm)))
            {
                RecoverHealth(player);
                RecoverMetabolism(player);
            }
            
            foreach (var player in BasePlayer.activePlayerList.Where(player => permission.UserHasPermission(player.UserIDString, AutoGodPerm)))
            {
                if (player.IsAdmin) player.SendConsoleCommand("god 1");
            }
        }

        private void Unload()
        {
            ConVar.AntiHack.userlevel = _antiHackUserlevel;
            if (_config.Time == -1) return;

            foreach (var player in BasePlayer.activePlayerList.Where(player => permission.UserHasPermission(player.UserIDString, AutoTimePerm)))
            {
                player.SendConsoleCommand("admintime -1");
            }
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            AutoTime(player);
            if (permission.UserHasPermission(player.UserIDString, AutoGodPerm) && player.IsAdmin) player.SendConsoleCommand("god 1");
        }
        
        private object OnPlayerViolation(BasePlayer player, AntiHackType type)
        {
            if (permission.UserHasPermission(player.UserIDString, TerrainPerm) && type == AntiHackType.InsideTerrain) return false;
            return null;
        }

        private void OnPlayerSleepEnded(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, AutoRecoverPerm)) return;
            
            RecoverHealth(player);
            RecoverMetabolism(player);
        }

        #endregion

        #region Helper

        private void RegisterPermission()
        {
            if (_config.DisableTerrainKick) permission.RegisterPermission(TerrainPerm, this);
            if (_config.Time != -1) permission.RegisterPermission(AutoTimePerm, this);
            if (_config.AutoRecover) permission.RegisterPermission(AutoRecoverPerm, this);
            if (_config.AutoGod) permission.RegisterPermission(AutoGodPerm, this);
        }

        private void AutoTime(BasePlayer player)
        {
            if (player == null) return;
            if (_config.Time == -1) return;
            if (!permission.UserHasPermission(player.UserIDString, AutoTimePerm)) return;
            
            player.SendConsoleCommand("admintime", _config.Time);
        }

        private static void RecoverHealth(BasePlayer player)
        {
            if (player == null) return;
            player.Heal(100);
        }

        private static void RecoverMetabolism(BasePlayer player)
        {
            if (player == null) return;

            var playerState = player.metabolism;
            
            playerState.bleeding.value = playerState.bleeding.min;
            playerState.calories.value = playerState.calories.max;
            playerState.comfort.value = 0;
            playerState.hydration.value = playerState.hydration.max;
            playerState.oxygen.value = playerState.oxygen.max;
            playerState.poison.value = playerState.poison.min;
            playerState.radiation_level.value = playerState.radiation_level.min;
            playerState.radiation_poison.value = playerState.radiation_poison.min;
            playerState.temperature.value = (PlayerMetabolism.HotThreshold + PlayerMetabolism.ColdThreshold) / 2;
            playerState.wetness.value = playerState.wetness.min;
        }
        
        #endregion
    }
}