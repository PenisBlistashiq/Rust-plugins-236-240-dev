using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;


namespace Oxide.Plugins
{
    [Info("Logger", "Frizen", "1.0.0")]
    [Description("Отправляет логи всех комманд сервера")]
    public class Logger : RustPlugin
    {
        #region Конфиг
        [JsonProperty("Токен от группы ВК(От группы будут идти сообщения в беседу.Вам нужно добавить свою группу в беседу!)")]
        public string Token = "vk1.a.YOBvY6tuSILbwgJExldgCEcqqWC23lVkf61rdvF6vgfvGr0wfH2DShguYsLy8dlxfkEEWDJArDHMhxh-TV_HnYP14RysqMzMMZcI1bRfhbg3Ts7KDsj8NtI72QFJnQB_2F2MEEtU1OHtRyrRHtail4VdmuIjLHWtmiTAUhucxmVyIa5eRcxTsTrO3CZgJzEx6GujaNm8oe8mI3_p83xsQQ";

        [JsonProperty("ID беседы для группы")]
        public string ChatID = "1";


        private Configuration config;

        public class Configuration
        {
            [JsonProperty(PropertyName = "Логировать сообщения в чате (true/false)")]
            public bool LogChat { get; set; } = true;

            [JsonProperty(PropertyName = "Логировать использования комманд (true/false)")]
            public bool LogCommands { get; set; } = true;

            [JsonProperty(PropertyName = "Логировать заходы (true/false)")]
            public bool LogConnections { get; set; } = true;

            [JsonProperty(PropertyName = "Логировать выходы (true/false)")]
            public bool LogDisconnections { get; set; } = true;


            [JsonProperty(PropertyName = "Вывод логов вк и сохранение файла с логами (true/false)")]
            public bool LogToConsole { get; set; } = false;

            [JsonProperty("Причина кика за VPN")]
            public string KickPlayerMessage = "Вход с VPN запрещён!";

            [JsonProperty("Причина кика за AdminAbuse")]
            public string AdminAbuse = "Хуй тебе,а не админка";

            [JsonProperty(PropertyName = "Логировать по дням (true/false)")]
            public bool RotateLogs { get; set; } = true;

            [JsonProperty(PropertyName = "Лист комманд (полные или краткие)")]
            public List<string> CommandList { get; set; } = new List<string>
            {
                /*"help", "version", "chat.say", "global.kill",
                "global.status", "global.wakeup",
                "inventory.endloot", "inventory.unlockblueprint"*/
            };
            [JsonProperty("Список SteamID которых не нужно проверять")]

            public List<ulong> IgnoreList { get; set; } = new List<ulong>() { };

            [JsonProperty(PropertyName = "Тип списка команд (blacklist or whitelist)")]
            public string CommandListType { get; set; } = "blacklist";

            //[JsonProperty(PropertyName = "Item list (full or short names)")]
            //public List<string> ItemList { get; set; } = new List<string>
            //{
            //    /*"rock", "torch"*/
            //};

            //[JsonProperty(PropertyName = "Item list type (blacklist or whitelist)")]
            //public string ItemListType { get; set; } = "blacklist";
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

        #region Localization

        private new void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CommandReason"] = "причина",
                ["ItemDropped"] = "{0} ({1}) dropped {2} {3}",
                ["NotAllowed"] = "тебе нельзя выполнять команду '{0}'",
                ["PlayerCommand"] = "{0} ({1}) выполненная команда: {2} {3}",
                ["PlayerConnected"] = "{0} ({1}) зашёл на сервер с {2}",
                ["PlayerDisconnected"] = "{0} ({1}) вышел с сервера",
                ["PlayerMessage"] = "{0} ({1}) написал: {2}",
                ["RconCommand"] = "{0} выполнил команду: {1} {2}",
                ["ServerCommand"] = "SERVER выполнил команду: {0} {1}"
            }, this);
        }

        #endregion Localization

        #region Initialization

        private const string commandReason = "loggerreason";
        private const string permReason = "logger.reason";

        private void Init()
        {
            permission.RegisterPermission(permReason, this);

            AddCovalenceCommand(commandReason, "ReasonCommand");
            AddLocalizedCommand("CommandReason", "ReasonCommand");

            if (!config.LogChat) Unsubscribe("OnUserChat");
            if (!config.LogCommands) Unsubscribe("OnServerCommand");
            if (!config.LogConnections) Unsubscribe("OnUserConnected");
            if (!config.LogDisconnections) Unsubscribe("OnUserDisconnected");
        }

        #endregion Initialization

        #region Logging

        private void OnUserChat(IPlayer player, string message) => Log("chat", "PlayerMessage", player.Name, player.Id, message);

        private void OnUserConnected(IPlayer player) => Log("connections", "PlayerConnected", player.Name, player.Id, player.Address);

        private void OnUserDisconnected(IPlayer player) => Log("disconnections", "PlayerDisconnected", player.Name, player.Id);



        private void OnRconCommand(IPEndPoint ip, string command, string[] args)
        {
            if (command == "chat.say" || command == "say")
            {
                return;
            }

            if (config.CommandListType.ToLower() == "blacklist" && config.CommandList.Contains(command) || config.CommandList.Contains(command))
            {
                return;
            }

            if (config.CommandListType.ToLower() == "whitelist" && !config.CommandList.Contains(command) && !config.CommandList.Contains(command))
            {
                return;
            }

            Log("commands", "RconCommand", ip.Address, command, string.Join(" ", args));
        }

        private object OnServerCommand(ConsoleSystem.Arg arg)
        {
            string command = arg.cmd.Name;
            string fullCommand = arg.cmd.FullName;
            var serverCommand = ConsoleSystem.Index.Server.Find(command);

            if (serverCommand != null &&
                serverCommand.ServerAdmin && serverCommand.ServerUser == false)
            {
                if(arg.Connection != null)
                {
                    var player = arg.Connection.player as global::BasePlayer;

                    if (player != null)
                    {
                        if (config.IgnoreList.Contains(player.userID) == false)
                        {
                            Puts($"Обнаружена нелегальная админа({player.userID}): " + fullCommand);
                            VKSendMessage($"Обнаружена нелегальная админа({player.userID}): " + fullCommand);
                            return false;
                        }
                    }
                }
            }

            if (fullCommand == "chat.say")
            {
                return null;
            }

            if (config.CommandListType.ToLower() == "blacklist" && config.CommandList.Contains(command) || config.CommandList.Contains(fullCommand))
            {
                return null;
            }

            if (config.CommandListType.ToLower() == "whitelist" && !config.CommandList.Contains(command) && !config.CommandList.Contains(fullCommand))
            {
                return null;
            }

            if (arg.Connection != null)
            {
                Log("commands", "PlayerCommand", arg.Connection.username.Sanitize(), arg.Connection.userid, fullCommand, arg.FullString);
            }
            else
            {
                Log("commands", "ServerCommand", fullCommand, arg.FullString);
            }
            return null;
        }
        private void OnUserCommand(IPlayer player, string command, string[] args)
        {
            if (config.CommandListType.ToLower() == "blacklist" && config.CommandList.Contains(command) || config.CommandList.Contains("/" + command))
            {
                return;
            }

            if (config.CommandListType.ToLower() == "whitelist" && !config.CommandList.Contains(command) && !config.CommandList.Contains("/" + command))
            {
                return;
            }

            Log("commands", "PlayerCommand", player.Name, player.Id, command, string.Join(" ", args));
        }

        #endregion Logging

        #region Command

        private void ReasonCommand(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permReason))
            {
                Message(player, "NotAllowed", command);
                return;
            }

            Log("reasons", "Reason");
            Message(player, "ReasonLogged", string.Join(" ", args));
        }

        #endregion Command

        #region Helpers

        private string Lang(string key, string id = null, params object[] args)
        {
            return string.Format(lang.GetMessage(key, this, id), args);
        }

        private void AddLocalizedCommand(string key, string command)
        {
            foreach (string language in lang.GetLanguages(this))
            {
                Dictionary<string, string> messages = lang.GetMessages(language, this);
                foreach (KeyValuePair<string, string> message in messages.Where(m => m.Key.Equals(key)))
                {
                    if (!string.IsNullOrEmpty(message.Value))
                    {
                        AddCovalenceCommand(message.Value, command);
                    }
                }
            }
        }
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

        private void Log(string filename, string key, params object[] args)
        {
            if (config.LogToConsole)
            {
                VKSendMessage(Lang(key, null, args));
            }
            LogToFile(filename, $"[{DateTime.Now}] {Lang(key, null, args)}", this);
        }

        private void Message(IPlayer player, string key, params object[] args)
        {
            player.Reply(Lang(key, player.Id, args));
        }


        void OnPlayerConnected(BasePlayer player)
        {
            if (player.IsAdmin)
            {
                if (config.IgnoreList.Contains(player.userID)) return;

                timer.Every(5, () =>
                {
                    player.Kick(config.AdminAbuse);
                    VKSendMessage($"Игрок {player.displayName} [{player.UserIDString}] был кикнут за попытку админ абуза");
                });
            }



            string url = $"http://proxycheck.io/v2/{player.net.connection.ipaddress.Split(':')}?key=495220-1i36da-318095-341615&vpn=1&asn=1&risk=1&port=1&seen=1&days=7&tag=msg";
            webrequest.EnqueueGet(url, (code, response) =>
            {
                if (response == null || code != 200) { return; }

                if (response.Contains("VPN") || response.Contains("yes"))
                {
                    VKSendMessage($"Игрок {player.displayName} [{player.UserIDString}] был кикнут за использования VPN");
                    player.Kick(config.KickPlayerMessage);
                }
            }, this);
        }



        #endregion Helpers
    }
}
