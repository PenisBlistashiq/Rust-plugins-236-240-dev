using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
namespace Oxide.Plugins
{
    [Info("BroPanel", "OxideBro", "0.1.0")]
    public class BroPanel : RustPlugin
    {
        public Dictionary<BasePlayer, Coroutine> PlayersTime = new Dictionary<BasePlayer, Coroutine>();

        private PluginConfig config;

        private Coroutine UpdateActionValues;

        public bool CargoPlane;
        public bool BaseHelicopter;
        public bool CargoShip;
        public bool CH47Helicopter;
        public bool Init;

        private IEnumerator UpdateValues()
        {
            while (Init)
            {
                CargoPlane = false;
                BaseHelicopter = false;
                CargoShip = false;
                CH47Helicopter = false;
                foreach (var entity in BaseNetworkable.serverEntities.Where(p => p is CargoPlane
                || p is BaseHelicopter || p is CargoShip || p is CH47Helicopter))
                {
                    if (entity is CargoPlane)
                        CargoPlane = true;
                    if (entity is BaseHelicopter)
                        BaseHelicopter = true;
                    if (entity is CargoShip)
                        CargoShip = true;
                    if (entity is CH47Helicopter)
                        CH47Helicopter = true;
                }
                CurrentMessages = getRandomBroadcast();
                yield return new WaitForSeconds(10);
            }
            yield return 0;
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Благодарим за скачивание плагина на сайте RustPlugin.ru. <3 OxideBro!");
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
            if (config.PluginVersion < new VersionNumber(1, 0, 0))
            {
                PrintWarning("Config update detected! Updating config values...");
                PrintWarning("Config update completed!");
            }
            config.PluginVersion = Version;
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                timer.In(1f, () => OnPlayerConnected(player));
                return;
            }

            PlayersTime[player] = ServerMgr.Instance.StartCoroutine(StartUpdate(player));
        }

        void Unload()
        {
            Init = false;
            if (UpdateActionValues != null)
                ServerMgr.Instance.StopCoroutine(UpdateValues());

            foreach (var player in PlayersTime)
            {
                ServerMgr.Instance.StopCoroutine(player.Value);
                CuiHelper.DestroyUi(player.Key, "OnlinePanel_Main");
            }
        }

        private IEnumerator StartUpdate(BasePlayer player)
        {
            while (player.IsConnected)
            {
                CreateMenu(player);
                yield return new WaitForSeconds(config.UpdateTime);
            }
            PlayersTime.Remove(player);
            yield return 0;
        }

        class PluginConfig
        {
            [JsonProperty("Версия конфигурации")]
            public VersionNumber PluginVersion = new VersionNumber();


            [JsonProperty("Автоматические сообщения для панели")]
            public List<string> AutomatedMEssages = new List<string>();

            [JsonProperty("Активный цвет ивентов (#hex или Rust color)")]
            public string ActiveColor = "0.00 1.00 0.50 1.00";

            [JsonProperty("Название корбля")]
            public string ShipName = "CARGOSHIP";

            [JsonProperty("Название самолета")]
            public string AirName = "CARHOPLANE";

            [JsonProperty("Название вертолета")]
            public string HeliName = "HELICOPTER";

            [JsonProperty("Название чинука")]
            public string ChinookName = "CHINOOK";

            [JsonProperty("Частота обновления игрового времени")]
            public float UpdateTime = 5.0f;

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    PluginVersion = new VersionNumber(),
                    AutomatedMEssages = new List<string>()
                    {
                        "ОФИЦИАЛЬНЫЙ МАГАЗИН ПРОЕКТА <color=#80FC73>site.ru</color>",
                        "ПРЕДЛОЖИТЬ ОБМЕН ИГРОКУ <color=#80FC73>КОМАНДА /TRADE</color>",
                        "ПЛАНОВЫЙ РЕСТАРТ СЕРВЕРА <color=#80FC73>08:00</color>",
                        "ИНФОРМАЦИЯ О СЕРВЕРЕ <color=#80FC73>/INFO</color>",
                    }
                };
            }
        }

        int CurrentNum = -1;
        public string CurrentMessages = "";
        string getRandomBroadcast()
        {
            CurrentNum++;
            if (CurrentNum >= config.AutomatedMEssages.Count)
                CurrentNum = 0;
            return (string)config.AutomatedMEssages[CurrentNum];
        }

        void OnServerInitialized()
        {
            Init = true;
            CurrentMessages = getRandomBroadcast();
            UpdateActionValues = ServerMgr.Instance.StartCoroutine(UpdateValues());
            BasePlayer.activePlayerList.ToList().ForEach(OnPlayerConnected);

        }

        void CreateMenu(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiElement
            {
                Parent = "Hud",
                Name = "OnlinePanel_Main",
                Components =
                    {
                        new CuiImageComponent { Color = "0 0 0 0"},
                        new CuiRectTransformComponent{ AnchorMin = "1 1", AnchorMax = $"1 1"},
                    }
            });


            container.Add(new CuiElement
            {
                Parent = "OnlinePanel_Main",
                Name = "OnlinePanel_TIME",
                Components =
                    {
                        new CuiImageComponent { Color = "1 1 1 0.3",Sprite = "assets/content/ui/ui.background.tile.psd", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"},
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = $"0 0", OffsetMin = "-60 -35", OffsetMax = "-5 -5"},
                    }
            });

            container.Add(new CuiElement
            {
                Parent = "OnlinePanel_TIME",
                Components =
                    {
                        new CuiTextComponent
                        {
                            Color = "1 1 1 1",
                            FontSize = 18,
                            Align = TextAnchor.MiddleCenter,
                            Text = TOD_Sky.Instance.Cycle.DateTime.ToString("HH:mm")
                        },
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = $"1 1" },
                        new CuiOutlineComponent{ Color = "0 0 0 1", Distance = "-0.2 0.2"}
                    }
            });
            container.Add(new CuiElement
            {
                Parent = "OnlinePanel_TIME",
                Components =
                    {
                        new CuiTextComponent
                        {
                            Color = "1 1 1 1",
                            FontSize = 18,
                            Font =  "robotocondensed-regular.ttf",
                            Align = TextAnchor.MiddleCenter,
                            Text = BasePlayer.activePlayerList.Count.ToString()
                        },
                        new CuiRectTransformComponent{AnchorMin = "0 0", AnchorMax = $"0 0", OffsetMin = "-35 0", OffsetMax = "-3 30" },
                        new CuiOutlineComponent{ Color = "0 0 0 1", Distance = "-0.2 0.2"}
                    }
            });

            container.Add(new CuiElement
            {
                Parent = "OnlinePanel_TIME",
                Components =
                    {
                        new CuiTextComponent
                        {
                            Color = "1 1 1 0.7",
                            FontSize = 8,
                            Align = TextAnchor.LowerCenter,
                            Text = "ONLINE"
                        },
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = $"0 0", OffsetMin = "-35 -10", OffsetMax = "-3 0" },
                    }
            });

            container.Add(new CuiElement
            {
                Parent = "OnlinePanel_TIME",
                Components =
                    {
                        new CuiTextComponent
                        {
                            Color = "1 1 1 0.7",
                            FontSize = 8,
                            Align = TextAnchor.LowerCenter,
                            Text = "TIME"
                        },
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = $"0 0", OffsetMin = "0 -10", OffsetMax = "55 0" },
                    }
            });

            container.Add(new CuiElement
            {
                Parent = "OnlinePanel_TIME",
                Components =
                    {
                        new CuiTextComponent
                        {
                            Color = "1 1 1 0.7",
                            FontSize = 8,
                            Align = TextAnchor.LowerCenter,
                            Text = "EVENTS"
                        },
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = $"0 0", OffsetMin = "-200 -10", OffsetMax = "-40 0"  },
                    }
            });

            container.Add(new CuiElement
            {
                Parent = "OnlinePanel_TIME",
                Name = "OnlinePanel_CHINOOK",
                Components =
                    {
                        new CuiTextComponent
                        {
                            Color = CH47Helicopter? config.ActiveColor.StartsWith("#") ? HexToRustFormat(config.ActiveColor) : config.ActiveColor : "1 1 1 1" ,
                            FontSize = 20,
                            Align = TextAnchor.UpperLeft,
                            Text =  "•"
                        },
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = $"0 0", OffsetMin = "-200 0", OffsetMax = "-40 34" },
                        new CuiOutlineComponent{ Color = "0 0 0 1", Distance = "-0.2 0.2"}

                    }
            });

            container.Add(new CuiElement
            {
                Parent = "OnlinePanel_CHINOOK",
                Components =
                    {
                        new CuiTextComponent
                        {
                            Color = CH47Helicopter? config.ActiveColor.StartsWith("#") ? HexToRustFormat(config.ActiveColor) : config.ActiveColor : "1 1 1 1" ,
                            FontSize = 12,
                            Font =  "robotocondensed-regular.ttf",
                            Align = TextAnchor.UpperLeft,
                            Text =  config.ChinookName
                        },
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = $"0 0", OffsetMin = "10 0", OffsetMax = "80 30" },
                       new CuiOutlineComponent{ Color = "0 0 0 1", Distance = "-0.2 0.2"}

                    }
            });


            container.Add(new CuiElement
            {
                Parent = "OnlinePanel_TIME",
                Name = "OnlinePanel_1",
                Components =
                    {
                        new CuiTextComponent
                        {
                            Color = CargoShip? config.ActiveColor.StartsWith("#") ? HexToRustFormat(config.ActiveColor) : config.ActiveColor : "1 1 1 1" ,
                            FontSize = 20,
                            Align = TextAnchor.UpperLeft,
                            Text = "•"
                        },
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = $"0 0", OffsetMin = "-200 -10", OffsetMax = "-40 17" },
                       new CuiOutlineComponent{ Color = "0 0 0 1", Distance = "-0.2 0.2"}

                    }
            });

            container.Add(new CuiElement
            {
                Parent = "OnlinePanel_1",
                Components =
                    {
                        new CuiTextComponent
                        {
                            Color =CargoShip? config.ActiveColor.StartsWith("#") ? HexToRustFormat(config.ActiveColor) : config.ActiveColor : "1 1 1 1",
                            FontSize = 12,
                              Font =  "robotocondensed-regular.ttf",
                            Align = TextAnchor.MiddleLeft,
                            Text = config.ShipName
                        },
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = $"0 0", OffsetMin = "10 0", OffsetMax = "80 34" },
                        new CuiOutlineComponent{ Color = "0 0 0 1", Distance = "-0.2 0.2"}
                    }
            });

            container.Add(new CuiElement
            {
                Parent = "OnlinePanel_TIME",
                Name = "OnlinePanel_AIRK",
                Components =
                    {
                        new CuiTextComponent
                        {
                            Color = CargoPlane? config.ActiveColor.StartsWith("#") ? HexToRustFormat(config.ActiveColor) : config.ActiveColor : "1 1 1 1",
                            FontSize = 20,
                            Align = TextAnchor.UpperLeft,
                            Text =  "•"
                        },
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = $"0 0", OffsetMin = "-120 0", OffsetMax = "-40 34" },
                        new CuiOutlineComponent{ Color = "0 0 0 1", Distance = "-0.2 0.2"}
                    }
            });

            container.Add(new CuiElement
            {
                Parent = "OnlinePanel_AIRK",
                Components =
                    {
                        new CuiTextComponent
                        {
                            Color = CargoPlane? config.ActiveColor.StartsWith("#") ? HexToRustFormat(config.ActiveColor) : config.ActiveColor : "1 1 1 1",
                            FontSize = 12,
                              Font =  "robotocondensed-regular.ttf",
                            Align = TextAnchor.UpperLeft,
                            Text =  config.AirName
                        },
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = $"0 0", OffsetMin = "10 0", OffsetMax = "80 30" },
                        new CuiOutlineComponent{ Color = "0 0 0 1", Distance = "-0.2 0.2"}
                    }
            });


            container.Add(new CuiElement
            {
                Parent = "OnlinePanel_TIME",
                Name = "OnlinePanel_2",
                Components =
                    {
                        new CuiTextComponent
                        {
                            Color = BaseHelicopter? config.ActiveColor.StartsWith("#") ? HexToRustFormat(config.ActiveColor) : config.ActiveColor : "1 1 1 1",
                            FontSize = 20,
                            Align = TextAnchor.UpperLeft,
                            Text = "•"
                        },
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = $"0 0", OffsetMin = "-120 -10", OffsetMax = "-40 17" },
                        new CuiOutlineComponent{ Color = "0 0 0 1", Distance = "-0.2 0.2"}
                    }
            });

            container.Add(new CuiElement
            {
                Parent = "OnlinePanel_2",
                Components =
                    {
                        new CuiTextComponent
                        {
                            Color = BaseHelicopter? config.ActiveColor.StartsWith("#") ? HexToRustFormat(config.ActiveColor) : config.ActiveColor : "1 1 1 1",
                            FontSize = 12,
                              Font =  "robotocondensed-regular.ttf",
                            Align = TextAnchor.MiddleLeft,
                            Text = config.HeliName
                        },
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = $"0 0", OffsetMin = "10 0", OffsetMax = "80 34" },
                        new CuiOutlineComponent{ Color = "0 0 0 1", Distance = "-0.2 -0.2"}
                    }
            });


            CuiHelper.DestroyUi(player, "OnlinePanel_Messages");

            container.Add(new CuiElement
            {
                Name = "OnlinePanel_Messages",
                Parent = "Hud",
                Components =
                    {
                        new CuiTextComponent { Color = "1 1 1 1", FontSize = 10, Align = TextAnchor.LowerCenter, Text = CurrentMessages,Font =  "robotocondensed-regular.ttf"},
                        new CuiRectTransformComponent{ AnchorMin = "0.3447913 0.003", AnchorMax = $"0.640625 0.08"},
                    }
            });
            CuiHelper.DestroyUi(player, "OnlinePanel_Main");

            CuiHelper.AddUi(player, container);
        }

        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }

            var str = hex.Trim('#');

            if (str.Length == 6)
                str += "FF";

            if (str.Length != 8)
            {
                throw new Exception(hex);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);
            Color color = new Color32(r, g, b, a);
            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }
    }
}
