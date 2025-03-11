using ConVar;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("WoundedLogo", "Frizen", "2.0.0")]
    internal class WoundedLogo: RustPlugin
    {
        #region Vars
        [PluginReference] private Plugin ImageLibrary;
        private bool _isCH47 = false;
        private bool _isHeli = false;
        private bool _isShip = false;
        private const string _layer = "WoundedLogo";
        #endregion

        #region Config
        private static Configuration _config;
        public class Configuration
        {

            [JsonProperty("Картинки")]
            public Dictionary<string, string> Imgs = new Dictionary<string, string>();

            public static Configuration DefaultConfig()
            {
                return new Configuration
                {
                    Imgs =
                    {
                        ["LogoText"] = "https://i.imgur.com/sJKehAZ.png",
                        ["LogoBTN"] = "https://i.imgur.com/9XZ2Dwg.png",
                        ["Heli"] = "https://i.imgur.com/x7aKqvw.png",
                        ["Heli_Active"] = "https://i.imgur.com/L2g9Kdm.png",
                        ["CargoShip"] = "https://i.imgur.com/kmH4Qb0.png",
                        ["CargoShip_Active"] = "https://i.imgur.com/solQ9MA.png",
                        ["CH47"] = "https://i.imgur.com/75zWb8a.png",
                        ["CH47_Active"] = "https://i.imgur.com/ezGZZIm.png",
                        ["BackgroundLogo"] = "https://i.imgur.com/3cxM5iR.png",
                        ["OnlineBar"] = "https://i.imgur.com/u7nshQU.png",
                        ["ActiveOnlineBar"] = "https://i.imgur.com/rL5rfVA.png",
                        ["PeopleIcon"] = "https://i.imgur.com/6NFZW4B.png",
                    }
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) LoadDefaultConfig();
                SaveConfig();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                PrintWarning("Creating new config file.");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig() => _config = Configuration.DefaultConfig();
        protected override void SaveConfig() => Config.WriteObject(_config);
        #endregion

        #region Hooks
        private void OnServerInitialized()
        {
            FirstCheckEvents();

            foreach (var img in _config.Imgs)
            {
                ImageLibrary.Call("AddImage", img.Value, img.Key);
            }

            foreach (var item in BasePlayer.activePlayerList)
            {
                OnPlayerConnected(item);
            }
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerConnected(player));
                return;
            }

            if (!MenuOpened.ContainsKey(player.userID))
            {
                MenuOpened.Add(player.userID, true);
            }

            DrawMenu(player);

            timer.Once(1f, () =>
            {
                foreach (var players in BasePlayer.activePlayerList)
                {
                    if (players.userID == player.userID) continue;
                    bool IsOpen;
                    if (MenuOpened.TryGetValue(player.userID, out IsOpen) && IsOpen)
                    {
                        OnlineUI(players);
                    }
                }
            });
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            timer.Once(1f, () =>
            {
                foreach (var players in BasePlayer.activePlayerList)
                {
                    OnlineUI(players);
                }
            });
        }

        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity == null) return;
            if (entity is CH47Helicopter)
            {
                _isCH47 = true;
                foreach (var player in BasePlayer.activePlayerList)
                {
                    RefreshEvents(player, "CH47");
                }
                return;
            }
            if (entity is BaseHelicopter)
            {
                _isHeli = true;
                foreach (var player in BasePlayer.activePlayerList)
                {
                    RefreshEvents(player, "Heli");
                }
                return;
            }
            if (entity is CargoShip)
            {
                _isShip = true;
                foreach (var player in BasePlayer.activePlayerList)
                {
                    RefreshEvents(player, "Cargo");
                }
                return;
            }

        }

        void OnEntityKill(BaseEntity entity)
        {
            if (entity == null) return;
            if (entity is CH47Helicopter)
            {
                _isCH47 = false;
                foreach (var player in BasePlayer.activePlayerList)
                {
                    RefreshEvents(player, "CH47");
                }
                return;
            }
            if (entity is BaseHelicopter)
            {
                _isHeli = false;
                foreach (var player in BasePlayer.activePlayerList)
                {
                    RefreshEvents(player, "Heli");
                }
                return;
            }
            if (entity is CargoShip)
            {
                _isShip = false;
                foreach (var player in BasePlayer.activePlayerList)
                {
                    RefreshEvents(player, "Cargo");
                }
                return;
            }

        }

        void OnEntityDeath(BaseEntity entity, HitInfo info)
        {
            if (entity == null) return;
            if (entity is CH47Helicopter)
            {
                _isCH47 = false;
                foreach (var player in BasePlayer.activePlayerList)
                {
                    RefreshEvents(player, "CH47");
                }
                return;
            }
            if (entity is BaseHelicopter)
            {
                _isHeli = false;
                foreach (var player in BasePlayer.activePlayerList)
                {
                    RefreshEvents(player, "Heli");
                }
                return;
            }
            if (entity is CargoShip)
            {
                _isShip = false;
                foreach (var player in BasePlayer.activePlayerList)
                {
                    RefreshEvents(player, "Cargo");
                }
                return;
            }

        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, _layer);
            }
        }
        #endregion

        #region UI
        private void DrawMenu(BasePlayer player)
        {
            var container = new CuiElementContainer();

            CuiHelper.DestroyUi(player, "WoundedLogo");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "1 1 1 0" },
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "30 -90.5", OffsetMax = "209.5 -21.5" }
            }, "Overlay", "WoundedLogo");

            bool IsOpen;
            if (MenuOpened.TryGetValue(player.userID, out IsOpen) && IsOpen)
            {
                container.Add(new CuiElement
                {
                    Name = "LogoBG",
                    Parent = "WoundedLogo",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("BackgroundLogo") },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "LogoBTN",
                    Parent = "WoundedLogo",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("LogoText") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-77.673 -14.558", OffsetMax = "-21.121 14.558" }
                }
                });
            }
            else
            {
                container.Add(new CuiElement
                {
                    Name = "LogoBTN",
                    Parent = "WoundedLogo",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("LogoBTN") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-89.579 -34.61", OffsetMax = "-20.494 34.611" }
                }
                });
            }

            container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0", Command = "wounded.menu" },
                Text = { Text = $"", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.LowerCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
            }, "LogoBTN");

            CuiHelper.AddUi(player, container);

            CuiHelper.DestroyUi(player, "BarActive");
            CuiHelper.DestroyUi(player, "OnlineBar");
            CuiHelper.DestroyUi(player, "Peoples");
            CuiHelper.DestroyUi(player, "Online");
            if (IsOpen)
            {
                OnlineUI(player);
                RefreshEvents(player, "All");
            }
        }

        private void RefreshEvents(BasePlayer player, string type)
        {
            var container = new CuiElementContainer();

            string HeliImage = _isHeli ? "Heli_Active" : "Heli";
            string CargoImage = _isShip ? "CargoShip_Active" : "CargoShip";
            string CH47Image = _isCH47 ? "CH47_Active" : "CH47";

            switch (type)
            {
                case "Heli":
                    CuiHelper.DestroyUi(player, "Heli");
                    container.Add(new CuiElement
                    {
                        Name = "Heli",
                        Parent = "WoundedLogo",
                        Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage(HeliImage) },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "58.13 -20.75", OffsetMax = "79.47 0.787" }
                }
                    });

                    break;
                case "Cargo":
                    CuiHelper.DestroyUi(player, "Cargo");

                    container.Add(new CuiElement
                    {
                        Name = "Cargo",
                        Parent = "WoundedLogo",
                        Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage(CargoImage) },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "22.252 -20.75", OffsetMax = "43.592 0.787" }
                }
                    });

                    break;
                case "CH47":
                    CuiHelper.DestroyUi(player, "CH47");
                    container.Add(new CuiElement
                    {
                        Name = "CH47",
                        Parent = "WoundedLogo",
                        Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage(CH47Image) },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-12.156 -20.75", OffsetMax = "9.185 0.787" }
                }
                    });

                    break;
                case "All":
                    CuiHelper.DestroyUi(player, "Heli");
                    CuiHelper.DestroyUi(player, "Cargo");
                    CuiHelper.DestroyUi(player, "CH47");

                    container.Add(new CuiElement
                    {
                        Name = "CH47",
                        Parent = "WoundedLogo",
                        Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage(CH47Image) },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-12.156 -20.75", OffsetMax = "9.185 0.787" }
                }
                    });

                    container.Add(new CuiElement
                    {
                        Name = "Cargo",
                        Parent = "WoundedLogo",
                        Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage(CargoImage) },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "22.252 -20.75", OffsetMax = "43.592 0.787" }
                }
                    });

                    container.Add(new CuiElement
                    {
                        Name = "Heli",
                        Parent = "WoundedLogo",
                        Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage(HeliImage) },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "58.13 -20.75", OffsetMax = "79.47 0.787" }
                }
                    });

                    break;
            }

            bool IsOpen;
            if (MenuOpened.TryGetValue(player.userID, out IsOpen) && !IsOpen)
            {
                return;
            }

            CuiHelper.AddUi(player, container);
        }

        private void OnlineUI(BasePlayer player)
        {
            var container = new CuiElementContainer();

            CuiHelper.DestroyUi(player, "BarActive");
            CuiHelper.DestroyUi(player, "OnlineBar");
            CuiHelper.DestroyUi(player, "Peoples");
            CuiHelper.DestroyUi(player, "Online");

            float onlinebar = GetOnline() / Convert.ToSingle(ConVar.Server.maxplayers);

            container.Add(new CuiElement
            {
                Name = "OnlineBar",
                Parent = "WoundedLogo",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("OnlineBar") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-12.156 5.086", OffsetMax = "78 20.34" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "BarActive",
                Parent = "OnlineBar",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("ActiveOnlineBar") },
                    new CuiRectTransformComponent {  AnchorMin = "0 0", AnchorMax = onlinebar > 0.2 ? $"{onlinebar} 1" : "0.2 1", OffsetMin = "1 2", OffsetMax = "-1 -2" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Peoples",
                Parent = "OnlineBar",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("PeopleIcon") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-40.499 -4.013", OffsetMax = "-31.901 4.013" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Online",
                Parent = "OnlineBar",
                Components = {
                    new CuiTextComponent { Text = $"{GetOnline()} / {Convert.ToSingle(ConVar.Server.maxplayers)}", Font = "robotocondensed-bold.ttf", FontSize = 7, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "8.83 -7.628", OffsetMax = "45.078 7.627" }
                }
            });

            bool IsOpen;
            if (MenuOpened.TryGetValue(player.userID, out IsOpen) && !IsOpen)
            {
                return;
            }

            CuiHelper.AddUi(player, container);


        }
        #endregion

        #region Helpers

        public Dictionary<ulong, bool> MenuOpened = new Dictionary<ulong, bool>();

        [ConsoleCommand("wounded.menu")]
        private void MenuHandler(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player == null) return;
            if (!MenuOpened.ContainsKey(player.userID)) MenuOpened.Add(player.userID, true);

            if (!MenuOpened[player.userID])
            {
                MenuOpened[player.userID] = true;
                DrawMenu(player);
                return;
            }
            if (MenuOpened[player.userID])
            {
                MenuOpened[player.userID] = false;
                CuiHelper.DestroyUi(player, "BarActive");
                CuiHelper.DestroyUi(player, "OnlineBar");
                CuiHelper.DestroyUi(player, "Peoples");
                CuiHelper.DestroyUi(player, "Online");
                DrawMenu(player);
                return;
            }
        }

        [PluginReference] private Plugin FreeOnline;
        float GetOnline()
        {
            float online;
            if (FreeOnline)
                online = FreeOnline.Call<int>("GetOnline");
            else online = 20;

            return online;
        }
        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex)) hex = "#FFFFFFFF";
            var str = hex.Trim('#');
            if (str.Length == 6) str += "FF";
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
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        private void FirstCheckEvents()
        {

            foreach (var entity in BaseEntity.serverEntities)
            {
                if (entity is CargoShip)
                {
                    _isShip = true;
                }
                if (entity is CH47Helicopter)
                {
                    _isCH47 = true;
                }
                if (entity is BaseHelicopter)
                {
                    _isHeli = true;
                }
            }

        }

        public string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary?.Call("GetImage", shortname, skin);
        #endregion

    }
}
