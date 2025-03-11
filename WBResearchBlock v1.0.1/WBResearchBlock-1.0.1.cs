using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;

#region Changelogs and ToDo
/**********************************************************************
* 
* 1.0.0 :       -   Initial release
* 1.0.1 :       -   Added permisions for workbench tiers
*                              
**********************************************************************/
#endregion

namespace Oxide.Plugins
{
    [Info("WBResearchBlock", "Krungh Crow", "1.0.1")]
    [Description("Blocks researching through a workbench")]

    class WBResearchBlock : RustPlugin
    {
        #region Variables
        const string Wb1_Perm = "wbresearchblock.usewb1";
        const string Wb2_Perm = "wbresearchblock.usewb2";
        const string Wb3_Perm = "wbresearchblock.usewb3";
        const string Bypass_Perm = "wbresearchblock.bypas";

        ulong chaticon = 0;
        string prefix;

        #endregion

        #region Configuration
        void Init()
        {
            if (!LoadConfigVariables())
            {
            Puts("Config file issue detected. Please delete file, or check syntax and fix.");
            return;
            }
            permission.RegisterPermission(Wb1_Perm, this);
            permission.RegisterPermission(Wb2_Perm, this);
            permission.RegisterPermission(Wb3_Perm, this);
            permission.RegisterPermission(Bypass_Perm, this);
            prefix = configData.PlugCFG.Prefix;
            chaticon = configData.PlugCFG.Chaticon;
        }

        private ConfigData configData;

        class ConfigData
        {
            [JsonProperty(PropertyName = "Main config")]
            public SettingsPlugin PlugCFG = new SettingsPlugin();
        }

        class SettingsPlugin
        {
            [JsonProperty(PropertyName = "Chat Steam64ID")]
            public ulong Chaticon = 0;
            [JsonProperty(PropertyName = "Chat Prefix")]
            public string Prefix = "[<color=green>WBR</color><color=yellow>Block</color>] ";
        }

        private bool LoadConfigVariables()
        {
            try
            {
            configData = Config.ReadObject<ConfigData>();
            }
            catch
            {
            return false;
            }
            SaveConf();
            return true;
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Fresh install detected Creating a new config file.");
            configData = new ConfigData();
            SaveConf();
        }

        void SaveConf() => Config.WriteObject(configData, true);
        #endregion

        #region LanguageAPI
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ChatInfo"] = "Researching through Workbenches is blocked you can still research some items through the research table!",
                ["NoPermission"] = "Research through workbench is blocked!\nCheck <color=green>/wbrblock</color> for more information!",
            }, this);
        }
        #endregion
                
        #region Commands

        [ChatCommand("wbrblock")]
        private void WBRBCMD(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                Player.Message(player, prefix + string.Format(msg("ChatInfo", player.UserIDString)), chaticon);
            }
        }
        #endregion

        #region Hooks
        private object CanLootEntity(BasePlayer player, Workbench bench)
        {

            if (player == null || bench == null) return null;
            if (permission.UserHasPermission(player.UserIDString, Bypass_Perm)) return null;
            if (bench.ShortPrefabName.Contains("workbench1") && permission.UserHasPermission(player.UserIDString, Wb1_Perm)) return null;
            if (bench.ShortPrefabName.Contains("workbench2") && permission.UserHasPermission(player.UserIDString, Wb2_Perm)) return null;
            if (bench.ShortPrefabName.Contains("workbench3") && permission.UserHasPermission(player.UserIDString, Wb3_Perm)) return null;
            TIP(player, string.Format(msg("NoPermission", player.UserIDString)), 15);
            return false;
        }
        #endregion

        #region Message helper
        private string msg(string key, string id = null) => lang.GetMessage(key, this, id);

        void TIP(BasePlayer player, string message, float dur)
        {
            if (player == null) return;
            player.SendConsoleCommand("gametip.showgametip", message, 0);
            timer.Once(dur, () => player?.SendConsoleCommand("gametip.hidegametip"));
        }
        #endregion
    }
}