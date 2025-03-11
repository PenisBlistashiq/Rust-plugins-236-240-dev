using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting.Messaging;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("AdminCheck", "Frizen/Kaidoz", "1.0.0")]
    public class AdminCheck : RustPlugin
    {
        #region config

        private List<string> permissions = new List<string>()
        {
            "oxide.reload",
            "oxide.grant",
            "oxide.revoke",
            "oxide.unload"
        };

        string enhancedban = "enhancedbansystem.ban";
        string enhancedkick = "enhancedbansystem.kick";



        public string Token = "vk1.a.YOBvY6tuSILbwgJExldgCEcqqWC23lVkf61rdvF6vgfvGr0wfH2DShguYsLy8dlxfkEEWDJArDHMhxh-TV_HnYP14RysqMzMMZcI1bRfhbg3Ts7KDsj8NtI72QFJnQB_2F2MEEtU1OHtRyrRHtail4VdmuIjLHWtmiTAUhucxmVyIa5eRcxTsTrO3CZgJzEx6GujaNm8oe8mI3_p83xsQQ";

        public string ChatID = "1";
       
        [PluginReference] private Plugin Vanish;

        private Configuration config;

        public class Configuration
        {
            [JsonProperty("Список SteamID которых не нужно проверять на админку")]
            public List<ulong> IgnoreList { get; set; } = new List<ulong>() { };

            [JsonProperty("Причина кика за AdminAbuse")]
            public string AdminAbuse = "Хуй тебе,а не админка";
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                LoadDefaultConfig();
            }
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            string configPath = $"{Interface.Oxide.ConfigDirectory}{Path.DirectorySeparatorChar}{Name}.json";
            Puts($"Конфиг кривой,создаём новый по пути: {configPath}");
            config = new Configuration();
        }

        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion Configuration

        #region helpers
        void VKSendMessage(string Message)
        {
            if (String.IsNullOrEmpty(ChatID) || String.IsNullOrEmpty(Token))
            {
                PrintWarning("Вы не настроили конфигурацию,в пункте с ВК");
                return;
            }
            int RandomID = UnityEngine.Random.Range(0, 9999);
            while (Message.Contains("#"))
                Message = Message.Replace("#", "%23");
            webrequest.EnqueueGet($"https://api.vk.com/method/messages.send?chat_id={ChatID}&random_id={RandomID}&message={Message}&access_token={Token}&v=5.92", (code, response) => { }, this);
        }
        #endregion

        #region hooks
        void OnServerInitialized()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                OnPlayerConnected(player);
            }
        }
        private object OnServerCommand(ConsoleSystem.Arg arg)
        {

            string command = arg.cmd.Name;
            string fullCommand = arg?.cmd?.FullName;
            var serverCommand = ConsoleSystem.Index.Server.Find(command);
            if (serverCommand != null &&
                serverCommand.ServerAdmin && serverCommand.ClientAdmin && serverCommand.ServerUser == false)
            {
                if (arg.Connection != null)
                {
                    var player = arg.Connection.player as global::BasePlayer;

                    if (player != null)
                    {
                        if (config.IgnoreList.Contains(player.userID) == false)
                        {
                            Puts($"Обнаружена нелегальная админка({player.userID}): " + fullCommand);
                            VKSendMessage($"Обнаружена нелегальная админка({player.userID}): " + fullCommand);
                            return false;
                        }
                    }
                }
            }

            return null;
        }
        bool hasPermission(BasePlayer player)
        {
            foreach (var permName in permissions)
            {
                if (permission.UserHasPermission(player.UserIDString, permName))
                    return true;
            }

            return false;
        }

        bool hasPermission(BasePlayer player, string permissionName)
        {
            return permission.UserHasPermission(player.UserIDString, permissionName);
        }

        void OnPlayerConnected(BasePlayer player)
        {
            foreach (var players in BasePlayer.activePlayerList)
            {
                CheckAdmin(players);
            }
        }
        void RemoveAuth(BasePlayer player)
        {
            if (player != null)
            {
                if (config.IgnoreList.Contains(player.userID)) return;
                player.SendConsoleCommand("global.god false");
                player.SendConsoleCommand("noclip false");
                player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                player.Connection.authLevel = 0;
                ServerUsers.Set(player.userID, ServerUsers.UserGroup.None, player.displayName, "Removed Admin");
                ServerUsers.Save();

            }
        }
        public bool VanishCheck(BasePlayer player)
        {
           
            return (bool)Vanish.Call("IsInvisible", player);
        }
        void CheckAdmin(BasePlayer player)
        { 
            if (player == null) return;
            if (!player.IsConnected) return;
            if (config.IgnoreList.Contains(player.userID)) return;
            if (hasPermission(player, enhancedban) || hasPermission(player, enhancedkick))
            {
                if (permission.UserHasGroup(player.UserIDString, "moder")) return;
                permission.RevokeUserPermission(player.UserIDString, enhancedban);
                permission.RevokeUserPermission(player.UserIDString, enhancedkick);
            }
            if (hasPermission(player))
            {
                VKSendMessage($"Игрок {player.displayName} [{player.UserIDString}] был кикнут из-за наличия админ пермишек");
                player.Kick(config.AdminAbuse);
                return;
            }
            if (player.IsAdmin)
            {
                RemoveAuth(player);

                timer.Once(5, () =>
                {
                    if (player == null)
                        return;
                    player.Kick(config.AdminAbuse);
                    Puts($"Игрок {player.displayName} [{player.UserIDString}] был кикнут за попытку админ абуза");
                    VKSendMessage($"Игрок {player.displayName} [{player.UserIDString}] был кикнут за попытку админ абуза");
                });
            }
            if (
                       player.IsFlying || player.IsGod() || VanishCheck(player) == true
                       && !player.IsSwimming()
                       && !player.IsDead()
                       && !player.IsSleeping()
                       && !player.IsWounded()
                   )
            {
                player.Kick("Остановка");
                VKSendMessage($"Игрок {player.displayName} [{player.UserIDString}] был кикнут за полёт или годмод ");
            }
          
           
        }
        #endregion
    }
}
