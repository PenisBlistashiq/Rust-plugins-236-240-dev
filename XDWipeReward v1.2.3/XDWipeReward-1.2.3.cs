using System;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using Rust;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core.Plugins;
using ConVar;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("XDWipeReward", "Skuli Dropek", "1.2.3")]
    [Description("Награда первым N игрокам после вайпа")]
    public class XDWipeReward : RustPlugin
    {
        [PluginReference] Plugin IQChat;
        #region Config
        public Configuration config;

        public class Setings
        {
            [JsonProperty("Количество игроков")]
            public int PlayersIntConnect;

            [JsonProperty("Команда для выдачи приза (если не нужно то оставить поля пустым)")]
            public string CommandPrize;

            [JsonProperty("У вас магазин ОВХ?")]
            public bool OVHStore;

            [JsonProperty("Бонус в виде баланса GameStores или OVH (если не нужно оставить пустым)")]
            public string GameStoreBonus;

            [JsonProperty("Лог сообщения(Показывается в магазине после выдачи в истории. Если OVH оставить пустым)")]
            public string GameStoreMSG;

            [JsonProperty("Id Магазина(GameStore. Если OVH оставить пустым)")]
            public string Store_Id;
            [JsonProperty("API KEY Магазина(GameStore. Если OVH оставить пустым)")]
            public string Store_Key;
        }

        public class Configuration
        {
            [JsonProperty("Настройки")]
            public Setings setings;
        }


        protected override void LoadDefaultConfig()
        {
            config = new Configuration()
            {
                setings = new Setings
                {
                    PlayersIntConnect = 100,
                    CommandPrize = "say %STEAMID%",
                    OVHStore = false,
                    GameStoreBonus = "",
                    GameStoreMSG = "За заход после вайпа:3",
                    Store_Id = "ID",
                    Store_Key = "KEY"

                }
            };
            SaveConfig(config);
        }

        void SaveConfig(Configuration config)
        {
            Config.WriteObject(config, true);
            SaveConfig();
        }

        public void LoadConfigVars()
        {
            config = Config.ReadObject<Configuration>();
            Config.WriteObject(config, true);
        }
        #endregion

        private void Init() => LoadPlayerData();
        private void Unload()
        {
            SavePlayerData();
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, WipeR);
        }
        private void OnServerInitialized()
        {
            LoadConfigVars();

            if (!string.IsNullOrEmpty(config.setings.GameStoreBonus) && !config.setings.OVHStore)
            {
                if (config.setings.Store_Id == "ID" || config.setings.Store_Key == "KEY")
                {
                    NextTick(() =>
                    {
                        PrintError("Вы не настроили ID И KEY от магазина GameStores");
                        Interface.Oxide.UnloadPlugin(Name);
                    });
                    return;
                }
            }

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                OnPlayerInit(player);
        }

        void OnNewSave(string filename)
        {
            playersInfo.Clear();
            Wipe = true;
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (Wipe)
            {
                if (playersInfo.Count >= config.setings.PlayersIntConnect)
                {
                    Wipe = false;
                    return;
                }
                if (!playersInfo.ContainsKey(player.userID))
                {
                    playersInfo.Add(player.userID, false);
                    WipeRewardGui(player);
                }
            }
        }

        [ConsoleCommand("giveprize")]
        void GivePrize(ConsoleSystem.Arg arg)
        {
            BasePlayer p = arg.Player();
            CuiHelper.DestroyUi(p, WipeR);
            if (!playersInfo.ContainsKey(p.userID))
                return;
            if (playersInfo[p.userID] == false)
            {
                if (!string.IsNullOrEmpty(config.setings.CommandPrize))
                {
                    Server.Command(config.setings.CommandPrize.Replace("%STEAMID%", p.UserIDString));
                }
                if (!string.IsNullOrEmpty(config.setings.GameStoreBonus))
                {
                    GiveReward(p.userID);
                }
                playersInfo[p.userID] = true;
                SendChat(p, "Вы успешно <color=#A1FF919A>забрали награду</color>!");
            }          
        }

        void GiveReward(ulong ID)
        {
            if (!config.setings.OVHStore)
            {
                string url = $"https://gamestores.ru/api?shop_id={config.setings.Store_Id}&secret={config.setings.Store_Key}&action=moneys&type=plus&steam_id={ID}&amount={config.setings.GameStoreBonus}&mess={config.setings.GameStoreMSG}";
                webrequest.Enqueue(url, null, (i, s) =>
                {
                    if (i != 200) { }
                    if (s.Contains("success"))
                    {
                        PrintWarning($"Игрок [{ID}] зашел 1 из первых, и получил бонус в нашем магазине. В виде [{config.setings.GameStoreBonus} руб]");
                    }
                    else
                    {
                        PrintWarning($"Игрок {ID} проголосовал за сервер, но не авторизован в магазине.");
                    }
                }, this);
            }
            else
            {
                plugins.Find("RustStore").CallHook("APIChangeUserBalance", ID, config.setings.GameStoreBonus, new Action<string>((result) =>
                {
                    if (result == "SUCCESS")
                    {
                        PrintWarning($"Игрок [{ID}] зашел 1 из первых, и получил бонус в нашем магазине. В виде [{config.setings.GameStoreBonus} руб]");
                        return;
                    }
                    PrintWarning($"Игрок {ID} проголосовал за сервер, но не авторизован в магазине. Ошибка: {result}");
                }));
            }
        }



        #region Parent
        public static string WipeR = "WipeR_CUI";
        #endregion

        #region GUI

        public void WipeRewardGui(BasePlayer p)
        {
            CuiHelper.DestroyUi(p, WipeR);
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-175 -340", OffsetMax = "-1 -280" },
                Image = { Color = "0 0 0 0.4", Material = "assets/content/ui/uibackgroundblur.mat", Sprite = "assets/content/ui/ui.background.tiletex.psd" }
            }, "Overlay", WipeR);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.2 0.01111135", AnchorMax = "0.8 0.4" },
                Button = { Command = "giveprize", Color = HexToRustFormat("#71FF9A9A") },
                Text = { Text = "Забрать награду", Align = TextAnchor.MiddleCenter, FontSize = 13 }
            }, WipeR);

            #region Title
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.4444442", AnchorMax = "1 1" },
                Text = { Text = $"Вы {Players.Count} из {config.setings.PlayersIntConnect}\n Поэтому получаете награду", FontSize = 13, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }

            }, WipeR);

            #endregion

            CuiHelper.AddUi(p, container);
        }

        #endregion

        #region Help

        List<ulong> Players = new List<ulong>();
        bool Wipe = false;
        private static string HexToRustFormat(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        public void SendChat(BasePlayer player, string Message)
        {
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message, "");
            else player.SendConsoleCommand("chat.add", 0, Message);
        }

        #endregion

        #region data
        private Dictionary<ulong, bool> playersInfo = new Dictionary<ulong, bool>();

        private void LoadPlayerData() => playersInfo = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, bool>>(Name);
        private void SavePlayerData() => Interface.GetMod().DataFileSystem.WriteObject(this.Name, playersInfo);
        #endregion
    }
}
