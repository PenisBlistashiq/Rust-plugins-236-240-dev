using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
	[Info("BHair", "King", "1.0.0")]
	public class BHair : RustPlugin
	{
        #region [Vars]
        [PluginReference] Plugin ImageLibrary;
        private static BHair plugin;
        public string Layer = "BHair.Layer";
        public string NewLayer = "BHair.NewLayer";
        public string NewLineLayer = "BHair.NewLineLayer";
        public string HairLayer = "BHair.HairLayer";
        #endregion

        #region [Config]
        private PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            config = PluginConfig.DefaultConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();

            if (config.PluginVersion < Version)
                UpdateConfigValues();

            Config.WriteObject(config, true);
        }

        private void UpdateConfigValues()
        {
            PluginConfig baseConfig = PluginConfig.DefaultConfig();
            if (config.PluginVersion < Version)
            {
                config.PluginVersion = Version;
                if (Version == new VersionNumber(1, 0, 0))
                {
                    //
                }

                PrintWarning("Config checked completed!");
            }
            config.PluginVersion = Version;
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        public class MainSettings
        {
            [JsonProperty("Команда для меню прицелов")]
            public string openMenu;

            [JsonProperty(PropertyName = "Минимальный размер прицелов")]
            public int MinHairSize;

            [JsonProperty(PropertyName = "Максимальный размер прицелов")]
            public int MaxHairSize;
        }

        public class SettingsDefaultHair
        {
            [JsonProperty("Айди стандартного прицела ( Первоначальный айди прицела )")]
            public int HairID;

            [JsonProperty("Размеры стандратного прицела ( Первоначальный размер прицела )")]
            public int SizeHair;
        }

        public class SettingsHair
        {
            [JsonProperty("Картинка - ( Прицел )")]
            public string Image;

            [JsonProperty("Айди прицела ( При создании новых прибавлять )")]
            public int HairID;
        }

        private class PluginConfig
        {
            [JsonProperty("Основные настройки")]
            public MainSettings _MainSettings = new MainSettings();

            [JsonProperty("Стандартные настройки игрока")]
            public SettingsDefaultHair _SettingsDefaultHair = new SettingsDefaultHair();

            [JsonProperty("Настройка выбора прицелов")]
            public List<SettingsHair> _SettingsHair = new List<SettingsHair>();

            [JsonProperty("Config version")]
            public VersionNumber PluginVersion = new VersionNumber();

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    _MainSettings = new MainSettings
                    {
                        openMenu = "hair",
                        MinHairSize = 5,
                        MaxHairSize = 20,
                    },
                    _SettingsDefaultHair = new SettingsDefaultHair
                    {
                        HairID = 14,
                        SizeHair = 7,
                    },
                    _SettingsHair = new List<SettingsHair>()
                    {
                        new SettingsHair()
                        {
                            Image = "https://imgur.com/O1T5M2S.png",
                            HairID = 1,
                        },
                        new SettingsHair()
                        {
                            Image = "https://i.imgur.com/62zrHBV.png",
                            HairID = 2,
                        },
                        new SettingsHair()
                        {
                            Image = "https://i.imgur.com/RACMuqg.png",
                            HairID = 3,
                        },
                        new SettingsHair()
                        {
                            Image = "https://i.imgur.com/tqtF73m.png",
                            HairID = 4,
                        },
                        new SettingsHair()
                        {
                            Image = "https://imgur.com/lBZ2Khj.png",
                            HairID = 5,
                        },
                        new SettingsHair()
                        {
                            Image = "https://imgur.com/7zs9aHt.png",
                            HairID = 6,
                        },
                        new SettingsHair()
                        {
                            Image = "https://imgur.com/udgZFcU.png",
                            HairID = 7,
                        },
                        new SettingsHair()
                        {
                            Image = "https://i.imgur.com/mIbPpj3.png",
                            HairID = 8,
                        },
                        new SettingsHair()
                        {
                            Image = "https://i.imgur.com/XCSkVNk.png",
                            HairID = 9,
                        },
                        new SettingsHair()
                        {
                            Image = "https://i.imgur.com/mIbPpj3.png",
                            HairID = 10,
                        },
                        new SettingsHair()
                        {
                            Image = "https://imgur.com/udgZFcU.png",
                            HairID = 11,
                        },
                        new SettingsHair()
                        {
                            Image = "https://imgur.com/7zs9aHt.png",
                            HairID = 12,
                        },
                        new SettingsHair()
                        {
                            Image = "https://imgur.com/lBZ2Khj.png",
                            HairID = 13,
                        },
                        new SettingsHair()
                        {
                            Image = "https://i.imgur.com/21Af6kF.png",
                            HairID = 14,
                        },
                    },
                    PluginVersion = new VersionNumber()
                };
            }
        }
        #endregion

        #region [Data]
        private void LoadData()
        {
            try
            {
                data = Interface.Oxide.DataFileSystem.ReadObject<PluginData>("BHair/playerData");
            }
            catch (Exception e)
            {
                PrintError(e.ToString());
            }

            if (data == null) data = new PluginData();
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject("BHair/playerData", data);

        private class PluginData
        {
            public Dictionary<ulong, PlayerData> PlayerData = new Dictionary<ulong, PlayerData>();
        }
    
        private static PluginData data;

        private class PlayerData
        {
            public int HairID;
            public int HairSize;

            public static PlayerData GetOrAdd(BasePlayer player)
            {
                return GetOrAdd(player.userID);
            }

            public static PlayerData GetOrAdd(ulong userId)
            {
                if (!data.PlayerData.ContainsKey(userId))
                    data.PlayerData.Add(userId, new PlayerData
                    {
                        HairID = plugin.config._SettingsDefaultHair.HairID,
                        HairSize = plugin.config._SettingsDefaultHair.SizeHair,
                    });

                return data.PlayerData[userId];
            }
        }
        #endregion

        #region [ImageLibrary]
        private bool HasImage(string imageName, ulong imageId = 0) => (bool)ImageLibrary.Call("HasImage", imageName, imageId);
        private bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);
        private string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary?.Call("GetImage", shortname, skin);
        #endregion

        #region [Oxide]
        private void OnServerInitialized()
        {
            plugin = this;
            LoadData();
            cmd.AddChatCommand(config._MainSettings.openMenu, this, "MenuOpen");
            foreach (var key in config._SettingsHair)
            {
                AddImage(key.Image, key.Image);
            }
            foreach (var player in BasePlayer.activePlayerList)
            {
                OnPlayerConnected(player);
            }
        }
        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
                CuiHelper.DestroyUi(player, HairLayer);
            }

            SaveData();
            plugin = null;
            data = null;
        }
        #endregion

        #region [Rust-Api]
        private void OnPlayerConnected(BasePlayer player)
        {
            var data = PlayerData.GetOrAdd(player);
            if (data != null)
            {
                var find = config._SettingsHair.FirstOrDefault(p => p.HairID == data.HairID);
                if (find == null) return;

                HairUI(player, find.Image);
            }
        }
        #endregion

        #region [Ui]
        private void MenuOpen(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Material = "assets/content/ui/uibackgroundblur.mat", Color = "0 0 0 0.77" },
            }, "Overlay", Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Color = "0.36 0.33 0.28 0.2", Material = "assets/icons/greyout.mat", Close = Layer },
                Text = { Text = "" }
            }, Layer);

            CuiHelper.AddUi(player, container);
            MainUi(player);
        }

        private void MainUi(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, NewLayer);
            CuiHelper.DestroyUi(player, NewLineLayer);
            var container = new CuiElementContainer();
            var data = PlayerData.GetOrAdd(player);
            float width; float xSwitch; width = 250; xSwitch = 0;
            var steps = config._MainSettings.MaxHairSize - config._MainSettings.MinHairSize;
            var progress = (float)(data.HairSize - 5) / steps;
            var size = width / steps;

            container.Add(new CuiPanel
            {
                Image = { Color = "0.36 0.34 0.32 0.45", Material = "assets/icons/greyout.mat" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-319 -37", OffsetMax = "322 176" },
            }, Layer, NewLayer);

            container.Add(new CuiPanel
            {
                Image = { Color = "0.36 0.34 0.32 0.45", Material = "assets/icons/greyout.mat" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-149 -70", OffsetMax = "152 -44" },
            }, Layer, NewLineLayer);

            container.Add(new CuiElement
            {
                Parent = NewLayer,
                Components =
                {
                    new CuiTextComponent { Text = $"Список прицелов, которые вы можете выбрать", Color = "1 1 1 0.65", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 24 },
                    new CuiOutlineComponent{Color = "0 0 0 1", Distance = "0.05 0.05"},
                    new CuiRectTransformComponent { AnchorMin = "0 0.845", AnchorMax = "0.998 1" },
                }
            });

            foreach (var check in config._SettingsHair.Select((i, t) => new { A = i, B = t }).Take(14))
            {
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{0.0343235 + check.B * 0.1335 - Math.Floor((float) check.B / 7) * 7 * 0.1335} {0.48 - Math.Floor((float) check.B/ 7) * 0.387}",
                                      AnchorMax = $"{0.158 + check.B * 0.1335 - Math.Floor((float) check.B / 7) * 7 * 0.1335} {0.84 - Math.Floor((float) check.B / 7) * 0.387}", },
                    Image = { Color = "0.2 0.2 0.2 0.45", Material = "assets/icons/greyout.mat" }
                }, NewLayer, NewLayer + $".{check.B}.HairList");

                container.Add(new CuiElement
                {
                    Parent = NewLayer + $".{check.B}.HairList",
                    Components =
                    {
                        new CuiRawImageComponent { Png = GetImage(check.A.Image), Color = "1 1 1 0.75" },
                        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = $"20 20",OffsetMax = $"-20 -20"}
                    }
                });

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = $"0.985 0.005" },
                    Image = { Color = data.HairID == check.A.HairID ? $"1 1 0.55" : $"0 0 0 0", Material = "assets/icons/greyout.mat" }
                }, NewLayer + $".{check.B}.HairList");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Color = "0 0 0 0", Command = $"UI_BHAIR sethair {check.A.HairID}" },
                    Text = { Text = "" }
                }, NewLayer + $".{check.B}.HairList");
            }

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.1 0.385", AnchorMax = "0.896 0.56"},
                Image = { Color = "0.2 0.2 0.2 0.45", Material = "assets/icons/greyout.mat" }
            }, NewLineLayer, ".HairSize.Line");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = $"{progress} 0.95" },
                Image = { Color = "1 1 0.55", Material = "assets/icons/greyout.mat" }
            }, ".HairSize.Line", ".HairSize.Line.Finish");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-5 -5", OffsetMax = "5 5" },
                Image = { Color = "0 0 0 1", Material = "assets/icons/greyout.mat" }
            }, ".HairSize.Line.Finish");

            for (var i = config._MainSettings.MinHairSize; i <= config._MainSettings.MaxHairSize; i++)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0 1", OffsetMin = $"{xSwitch} 0", OffsetMax = $"{xSwitch + size} 0" },
                    Text = { Text = "" },
                    Button = { Color = "0 0 0 0", Command = $"UI_BHAIR setsize {i}" }
                }, ".HairSize.Line");
                xSwitch += size;
            }

            CuiHelper.AddUi(player, container);
        }

        private void HairUI(BasePlayer player, string hair)
        {
            CuiHelper.DestroyUi(player, HairLayer);
            var container = new CuiElementContainer();
            var data = PlayerData.GetOrAdd(player);
            float margin = data.HairSize;
            if (data.HairID == config._SettingsHair.Count()) return;

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0" }
            }, "Hud", HairLayer);

            container.Add(new CuiElement
            {
                Parent = HairLayer,
                Components =
                {
                    new CuiRawImageComponent { Png = GetImage($"{hair}"), Color = "1 1 1 0.8" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = $"-{margin} -{margin}", OffsetMax = $"{margin} {margin}" }
                }
            });

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region [ConsoleCommand && ChatCommand]
        [ConsoleCommand("UI_BHAIR")]
        private void CmdConsoleMarkers(ConsoleSystem.Arg arg)
        {
            var player = arg?.Player();
            if (player == null || !arg.HasArgs()) return;

            switch (arg.Args[0])
            {
                case "setsize":
                {
                    int HairSize;
                    if (!arg.HasArgs(2) || !int.TryParse(arg.Args[1], out HairSize)) return;

                    var data = PlayerData.GetOrAdd(player);
                    if (data == null) return;

                    var find = config._SettingsHair.FirstOrDefault(p => p.HairID == data.HairID);
                    if (find == null) return;

                    data.HairSize = HairSize;

                    HairUI(player, find.Image);
                    MainUi(player);
                    break;
                }
                case "sethair":
                {
                    int HairID;
                    if (!arg.HasArgs(2) || !int.TryParse(arg.Args[1], out HairID)) return;

                    var data = PlayerData.GetOrAdd(player);
                    if (data == null) return;

                    var find = config._SettingsHair.FirstOrDefault(p => p.HairID == HairID);
                    if (find == null) return;

                    data.HairID = HairID;

                    HairUI(player, find.Image);
                    MainUi(player);
                    break;
                }
            }
        }
        #endregion
    }
}