using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("LogoMenu", "", "1.1.0")]
    public class LogoMenu : RustPlugin
    {
        [PluginReference] private Plugin ImageLibrary;

        public List<BasePlayer> PlayersTime = new List<BasePlayer>();

        public string Layer = "LogoMenu";
        public string LayerOnline = "LogoMenu.Online";
        public string LayerButton = "LogoMenu.Button";

        public float FadeIn = 1.0f;
        public float FadeOut = 0.7f;

        private Dictionary<string, string> Images = new Dictionary<string, string>
        {
            ["GS_Store"] = "https://cdn.discordapp.com/attachments/727064710238830643/943852653224550453/LightRust_1243.png",
            ["Online"] = "https://cdn.discordapp.com/attachments/1018247567508774965/1041700929457627176/1.png",
            ["CargoPlane"] = "https://cdn.discordapp.com/attachments/1018247567508774965/1041448035609235466/c44335b18ccdd010.png",
            ["Helicopter"] = "https://cdn.discordapp.com/attachments/1018247567508774965/1041446635219202129/LightRust_1.png",
            ["CH47Helicopter"] = "https://cdn.discordapp.com/attachments/1018247567508774965/1041448035898630144/76b1d033bbdbeba7.png",
            ["BradleyAPC"] = "https://cdn.discordapp.com/attachments/1018247567508774965/1041448426119888906/1.png",
            ["MenuButton"] = "https://cdn.discordapp.com/attachments/922548321531338772/922577086722502656/ec653bf8c05f2fb9.png",
            ["porno"] = "https://cdn.discordapp.com/attachments/1047137189076672543/1047188912373764207/1516806452_741_gaymanporn_org.png"
        };

        private void OnServerInitialized()
        {
            foreach (var check in Images)
                ImageLibrary.Call("AddImage", check.Value, check.Key);

            foreach (var player in BasePlayer.activePlayerList)
            {
                OnPlayerConnected(player);
            }
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerConnected(player));
                return;
            }

            UpdatePlayerUI();
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            UpdatePlayerUI();
        }

        private void UpdatePlayerUI()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                timer.Once(1, () =>
                {
                    OnlineTextUI(player);

                    MenuUI(player);
                });
            }
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
                CuiHelper.DestroyUi(player, LayerButton);
            }

            foreach (var player in PlayersTime)
            {
                CuiHelper.DestroyUi(player, LayerOnline);
            }
        }


        [ConsoleCommand("logo.menu.open")]
        private void CMD_ClickMenu(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 -720", OffsetMax = "680 -65" },
            }, "Hud", Layer);

            container.Add(new CuiElement
            {
                Name = Layer + ".porno",
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent {Png = ImageLibrary.Call<string>("GetImage", "porno" ), Color = "0 0 0 0"},
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0", AnchorMax = "1 1",
                    }
                }
            });


        }

        void OnlineTextUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, LayerOnline);

            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 -34", OffsetMax = "300 0" },
            }, "Hud", LayerOnline);

            string online = $"{BasePlayer.activePlayerList.Count}/{ConVar.Server.maxplayers}";

            container.Add(new CuiElement
            {
                Parent = LayerOnline,
                Name = Layer + ".Text",
                Components =
                {
                    new CuiTextComponent { Text = online, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 13, Color = HexToRustFormat("#FFFFFF99") },
                    new CuiRectTransformComponent{ AnchorMin = "0.3 0", AnchorMax = "0.3 0", OffsetMin = "-24 6.5", OffsetMax = "27.5 30" },
                }
            });

            CuiHelper.AddUi(player, container);
        }

        void MenuUI(BasePlayer player) /*bool open = false*/
        {
            CuiHelper.DestroyUi(player, Layer);

            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 -34", OffsetMax = "300 0" },
            }, "Hud", Layer);

            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".GameStores.Picture",
                Components =
                {
                    new CuiRawImageComponent { Color = HexToRustFormat("##FFFFFF29"), Png = (string) ImageLibrary.Call("GetImage", "GS_Store") },
                    new CuiRectTransformComponent { AnchorMin = "0.1 0", AnchorMax = "0.1 0", OffsetMin = "-16.5 6.5", OffsetMax = "7 30" }
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.1 0", AnchorMax = "0.1 0", OffsetMin = "-16.5 6.5", OffsetMax = "7 30" },
                Text = { Text = "" },
                Button =
                {
                    Color = "0 0 0 0",
                    Command = "chat.say /store"
                }

            }, Layer);

            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".Online.Picture",
                Components =
                {
                    new CuiRawImageComponent { Color = HexToRustFormat("##FFFFFF29"), Png = (string) ImageLibrary.Call("GetImage", "Online") },
                    new CuiRectTransformComponent { AnchorMin = "0.2 0", AnchorMax = "0.2 0", OffsetMin = "-20 6.5", OffsetMax = "3.5 30" }
                }
            });

            //if (open)
            //{
            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".CargoPlane",
                FadeOut = FadeOut,
                Components =
                    {
                        new CuiImageComponent { FadeIn = FadeIn, Color = "0 0 0 0" },
                        new CuiRectTransformComponent { AnchorMin = "0.4 0", AnchorMax = "0.4 0", OffsetMin = "0 6.5", OffsetMax = "23.5 30" }
                    }
            });

            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".BaseHelicopter",
                FadeOut = FadeOut,
                Components =
                    {
                        new CuiImageComponent { FadeIn = FadeIn, Color = "0 0 0 0" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-1 6.5", OffsetMax = "22 30" }
                    }
            });

            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".CH47Helicopter",
                FadeOut = FadeOut,
                Components =
                    {
                        new CuiImageComponent { FadeIn = FadeIn, Color = "0 0 0 0" },
                        new CuiRectTransformComponent { AnchorMin = "0.6 0", AnchorMax = "0.6 0", OffsetMin = "-3 6.5", OffsetMax = "20 30" }
                    }
            });

            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".BradleyAPC",
                FadeOut = FadeOut,
                Components =
                    {
                        new CuiImageComponent { FadeIn = FadeIn, Color = "0 0 0 0" },
                        new CuiRectTransformComponent { AnchorMin = "0.7 0", AnchorMax = "0.7 0", OffsetMin = "-5 6.5", OffsetMax = "18 30" }
                    }
            });
            //}

            CuiHelper.AddUi(player, container);

            RefreshUI(player, "CargoPlane");
            RefreshUI(player, "BaseHelicopter");
            RefreshUI(player, "BradleyAPC");
            RefreshUI(player, "CH47Helicopter");
        }

        /*void ButtonMenuUI(BasePlayer player)
	    {
		    CuiHelper.DestroyUi(player, LayerButton);
		    
		    CuiElementContainer container = new CuiElementContainer();

		    container.Add(new CuiPanel
		    {
			    CursorEnabled = false,
			    Image = { Color = "0 0 0 0" },
			    RectTransform = { AnchorMin = "0.764 1", AnchorMax = "0.764 1", OffsetMin = "0 -34", OffsetMax = "300 0" },
		    }, "Hud", LayerButton);

		    container.Add(new CuiElement
		    {
			    Parent = LayerButton,
			    Components =
			    {
				    new CuiRawImageComponent { Color = HexToRustFormat("##FFFFFF29"), Png = (string) ImageLibrary.Call("GetImage", "MenuButton") },
				    new CuiRectTransformComponent { AnchorMin = "0.9 0", AnchorMax = "0.9 0", OffsetMin = "2 6.5", OffsetMax = "25.5 30" },
			    }
		    });
	        
		    container.Add(new CuiButton	
		    {
			    RectTransform = { AnchorMin = "0.9 0", AnchorMax = "0.9 0", OffsetMin = "2 6.5", OffsetMax = "25.5 30" },
			    Button = { Command = "logo.menu.open", Color = "0 0 0 0" },
			    Text = { Text = "" }
		    }, LayerButton);
		    
		    CuiHelper.AddUi(player, container);
	    }*/


        private void RefreshUI(BasePlayer player, string Type)
        {
            CuiElementContainer RefreshContainer = new CuiElementContainer();

            switch (Type)
            {
                case "CargoPlane":
                    CuiHelper.DestroyUi(player, Layer + ".CargoPlane.Destroy");

                    RefreshContainer.Add(new CuiElement
                    {
                        Parent = Layer + ".CargoPlane",
                        Name = Layer + ".CargoPlane.Destroy",
                        FadeOut = FadeOut,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                FadeIn = FadeIn, Color = IsCargoPlane() ? HexToRustFormat("##EDBE5C5B") : HexToRustFormat("##FFFFFF29"),
                                Png = (string) ImageLibrary.Call("GetImage", "CargoPlane")
                            },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" },
                        }
                    });
                    break;

                case "BaseHelicopter":
                    CuiHelper.DestroyUi(player, Layer + ".BaseHelicopter.Destroy");

                    RefreshContainer.Add(new CuiElement
                    {
                        Parent = Layer + ".BaseHelicopter",
                        Name = Layer + ".BaseHelicopter.Destroy",
                        FadeOut = FadeOut,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                FadeIn = FadeIn, Color = IsBaseHelicopter() ? HexToRustFormat("##EDBE5C5B") : HexToRustFormat("##FFFFFF29"),
                                Png = (string) ImageLibrary.Call("GetImage", "Helicopter")
                            },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" },
                        }
                    });
                    break;

                case "BradleyAPC":
                    CuiHelper.DestroyUi(player, Layer + ".BradleyAPC.Destroy");

                    RefreshContainer.Add(new CuiElement
                    {
                        Parent = Layer + ".BradleyAPC",
                        Name = Layer + ".BradleyAPC.Destroy",
                        FadeOut = FadeOut,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                FadeIn = FadeIn, Color = IsBradleyAPC() ? HexToRustFormat("##EDBE5C5B") : HexToRustFormat("##FFFFFF29"),
                                Png = (string) ImageLibrary.Call("GetImage", "BradleyAPC")
                            },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" },
                        }
                    });
                    break;

                case "CH47Helicopter":
                    CuiHelper.DestroyUi(player, Layer + ".CH47Helicopter.Destroy");

                    RefreshContainer.Add(new CuiElement
                    {
                        Parent = Layer + ".CH47Helicopter",
                        Name = Layer + ".CH47Helicopter.Destroy",
                        FadeOut = FadeOut,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                FadeIn = FadeIn, Color = IsCH47Helicopter() ? HexToRustFormat("##EDBE5C5B") : HexToRustFormat("##FFFFFF29"),
                                Png = (string) ImageLibrary.Call("GetImage", "CH47Helicopter")
                            },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" },
                        }
                    });
                    break;
            }

            CuiHelper.AddUi(player, RefreshContainer);
        }

        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity == null || entity?.net.ID == null) return;

            string type = string.Empty;

            if (entity is CargoPlane)
                type = "CargoPlane";
            if (entity is BaseHelicopter)
                type = "BaseHelicopter";
            if (entity is BradleyAPC)
                type = "BradleyAPC";
            if (entity is CH47Helicopter)
                type = "CH47Helicopter";

            RefreshUI(type);
        }

        void OnEntityKill(BaseNetworkable entity)
        {
            if (entity == null || entity?.net.ID == null) return;

            string type = string.Empty;

            if (entity is CargoPlane)
                type = "CargoPlane";
            if (entity is BaseHelicopter)
                type = "BaseHelicopter";
            if (entity is BradleyAPC)
                type = "BradleyAPC";
            if (entity is CH47Helicopter)
                type = "CH47Helicopter";

            RefreshUI(type);
        }

        private void RefreshUI(string tag)
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                RefreshUI(player, tag);
            }
        }

        string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex)) hex = "#FFFFFFFF";
            var str = hex.Trim('#');
            if (str.Length == 6) str += "FF";
            if (str.Length != 8) throw new Exception(hex);

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber); asd
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);
            Color color = new Color32(r, g, b, a);

            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }

        bool IsCargoPlane()
        {
            foreach (var check in BaseNetworkable.serverEntities)
                if (check is CargoPlane) return true;
            return false;
        }
        bool IsBaseHelicopter()
        {
            foreach (var check in BaseNetworkable.serverEntities)
                if (check is BaseHelicopter) return true;
            return false;
        }
        bool IsBradleyAPC()
        {
            foreach (var check in BaseNetworkable.serverEntities)
                if (check is BradleyAPC) return true;
            return false;
        }
        bool IsCH47Helicopter()
        {
            foreach (var check in BaseNetworkable.serverEntities)
                if (check is CH47Helicopter) return true;
            return false;
        }
    }
}