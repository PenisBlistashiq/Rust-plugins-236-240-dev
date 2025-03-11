using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Proxy Blocker", "Frizen", "2.0.0")]
    public class ProxyBlocker : RustPlugin
    {
        public Configuration config;
        
        [JsonProperty("Токен от группы ВК(От группы будут идти сообщения в беседу.Вам нужно добавить свою группу в беседу!)")]
        public string Token = "vk1.a.otiNPk1xYFABxGHAke4clG4LbwHR4TP_mWEuLt2CC8IXbA4z73xRM96CcOzgxfFWuo9OghKHxXSLkUJRgpYzPb4GTAdiOQ4FQ_UK-v8U6ORkZ_H6rbPJSijbZc98ykp_DLARwoJsOO8djjL5xBWdom5aOqcbp23-E9lNNUzEmNyHt0V4rnAaTbnPqFZ6GCbi";
            
        [JsonProperty("ID беседы для группы")]
        public string ChatID = "5";

        public class Configuration
        {
            [JsonProperty("Причина кика за VPN")] 
            public string KickPlayerMessage = "Вход с VPN запрещён!";

            [JsonProperty("Список SteamID которых не нужно проверять")]
            public List<ulong> IgnoreList = new List<ulong>() {};

        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
            }
            catch
            {
                PrintError("Конфигурационный файл повреждён, проверьте правильность ведённых данных!");
            }
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(new Configuration(), true);
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (config.IgnoreList.Contains(player.userID)) return;

            timer.Every(5, () =>
            {
                BasePlayer.activePlayerList.ToList().ForEach(p =>
                {
                    List<string> names = new List<string> { "mq_mew#0800", "mqmew", "mq.mew" };
                    if (names.Any(t => p.displayName.ToLower().Contains(t.ToLower())))
                    {
                        Server.Command($"ban {p.UserIDString}");
                    }
                });
            });


            string url = $"http://proxycheck.io/v2/{player.net.connection.ipaddress.Split(':')}?key=39d840-jcg9n2-md685z-7402c2&vpn=1&asn=1&risk=1&port=1&seen=1&days=7&tag=msg";
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

        [ConsoleCommand("proxy.ignore")]
        void CmdACIgnore(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 1) return;
            if (arg.Args == null) return;
            
            string steamidSTR = arg.Args[0];
            ulong steamid = 0;
            
            if (steamidSTR.Length == 17 && ulong.TryParse(steamidSTR, out steamid))
            {
                if (!config.IgnoreList.Contains(steamid))
                {
                    config.IgnoreList.Add(steamid);
                    Puts($"[ProxyBlocker]: Player [{steamidSTR}] added to IgnoreList");
                    SaveConfig();
                }
                else
                {
                    config.IgnoreList.Remove(steamid);
                    Puts($"[ProxyBlocker]: Player [{steamidSTR}] removed from IgnoreList");
                    SaveConfig();
                }
                Config.WriteObject(config, true);
            }
            else
            {
                Puts($"[ProxyBlocker]: You write steamid [{steamidSTR}] is not correct!");
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
    }
}