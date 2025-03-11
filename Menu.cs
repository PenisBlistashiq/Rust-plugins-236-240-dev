using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using ru = Oxide.Game.Rust;
using UnityEngine;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Menu", "Forum: https://topplugin.ru Ds: alone_sempai Vk: https://vk.com/rustnastroika", "1.0.3")]

    class Menu : RustPlugin
    {
        [PluginReference] private Plugin ImageLibrary;

        private const string Layer = "asd";

        private void LoadImages()
        {
            if (!ImageLibrary)
            {
                PrintError("IMAGE LIBRARY IS NOT INSTALLED!");
            }
            else
            {
                ImageLibrary?.Call("AddImage", "https://i.postimg.cc/YSmpXPPJ/image-3.png", "online");
                ImageLibrary?.Call("AddImage", "https://i.postimg.cc/d10xZ6Dr/Frame-1463.png", "logoava1");
                ImageLibrary?.Call("AddImage", "https://i.postimg.cc/8z33n0dg/image-2.png", "bg1");
                ImageLibrary?.Call("AddImage", "https://i.postimg.cc/FHYrQbhW/image-24.png", "poloskamenu1");
                ImageLibrary?.Call("AddImage", "https://i.postimg.cc/DfPgsDtG/image-27.png", "name123");
                ImageLibrary?.Call("AddImage", "https://i.postimg.cc/7YVGLN8q/image-23.png", "name456");
                ImageLibrary?.Call("AddImage", "https://i.postimg.cc/nV2JjSYv/image-6.png", "store");
                ImageLibrary?.Call("AddImage", "https://i.postimg.cc/kXyKLmTM/menub.png", "info");
            }
        }

        private void OnServerInitialized()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                Main_menu(player);
                RefreshUI(player, "all");
                ServerMgr.Instance.StartCoroutine(StartUpdate(player));
                player.SetFlag(BaseEntity.Flags.Reserved3, true);
                ServerMgr.Instance.StopCoroutine(StartUpdate(player));
            }

            LoadImages();
            AddCovalenceCommand("menuOpen", nameof(CmdMenuOpen));
        }

        private readonly List<BasePlayer> MenuUsers2 = new List<BasePlayer>();
        private void CmdMenuOpen(IPlayer user, string cmd, string[] args)
        {
            var player = user?.Object as BasePlayer;
            if (player == null) return;

            if (MenuUsers2.Contains(player))
            {
                player.SetFlag(BaseEntity.Flags.Reserved3, true);
                ServerMgr.Instance.StartCoroutine(StartUpdate(player));
                CuiHelper.DestroyUi(player, Layer + "name");
                CuiHelper.DestroyUi(player, Layer + "online");
                CuiHelper.DestroyUi(player, Layer + "icons.online");
                CuiHelper.DestroyUi(player, Layer + "invise2");
                CuiHelper.DestroyUi(player, Layer + "text.menu");
                CuiHelper.DestroyUi(player, Layer + "menu");
                CuiHelper.DestroyUi(player, Layer + "icons.store");
                CuiHelper.DestroyUi(player, Layer + "icons.info");
                CuiHelper.DestroyUi(player, Layer + "name1");
                CuiHelper.DestroyUi(player, Layer + "logoavaa2");
                CuiHelper.DestroyUi(player, Layer + "poloskamenu1");
                CuiHelper.DestroyUi(player, Layer + "text.buttons");
                CuiHelper.DestroyUi(player, Layer + "text.buttons");
                CuiHelper.DestroyUi(player, Layer + "text.buttons");
                CuiHelper.DestroyUi(player, Layer + "text.buttons");
                CuiHelper.DestroyUi(player, Layer + "text.buttons");
                CuiHelper.DestroyUi(player, Layer + "text.buttons");
                CuiHelper.DestroyUi(player, Layer + "text.buttons");
                ServerMgr.Instance.StopCoroutine(StartUpdate(player));

                RefreshUI(player, "all");
                MenuUsers2.Remove(player);
            }
            else
            {
                CuiHelper.DestroyUi(player, Layer + "name");
                CuiHelper.DestroyUi(player, Layer + "online");
                CuiHelper.DestroyUi(player, Layer + "icons.online");
                CuiHelper.DestroyUi(player, Layer + "invise2");
                CuiHelper.DestroyUi(player, Layer + "text.menu");
                CuiHelper.DestroyUi(player, Layer + "menu");
                CuiHelper.DestroyUi(player, Layer + "icons.store");
                CuiHelper.DestroyUi(player, Layer + "icons.info");
                CuiHelper.DestroyUi(player, Layer + "name1");
                CuiHelper.DestroyUi(player, Layer + "logoavaa2");
                CuiHelper.DestroyUi(player, Layer + "poloskamenu1");
                CuiHelper.DestroyUi(player, Layer + "text.buttons");
                CuiHelper.DestroyUi(player, Layer + "text.buttons");
                CuiHelper.DestroyUi(player, Layer + "text.buttons");
                CuiHelper.DestroyUi(player, Layer + "text.buttons");
                CuiHelper.DestroyUi(player, Layer + "text.buttons");
                CuiHelper.DestroyUi(player, Layer + "text.buttons");
                CuiHelper.DestroyUi(player, Layer + "text.buttons");

                player.SetFlag(BaseEntity.Flags.Reserved3, false);

                RefreshUI(player, "open");
                MenuUsers2.Add(player);
            }
        }
        private IEnumerator StartUpdate(BasePlayer player)
        {
            while (player != null && player.IsConnected)
            {
                RefreshUI(player, "timeandonline");
                yield return new WaitForSeconds(2.5f);
            }
        }
        void startupdatecon(ConsoleSystem.Arg ar)
        {
            var target = ar.Player();
            if (target.IsAdmin) { }
            if (target.IsSleeping())
            {
                return;
            }
            if (target.gameObject == null)
            {
                return;
            }
            if (target != null)
            {
                return;
            }
        }
        private void OnPlayerConnected(BasePlayer player)
        {
            Main_menu(player);
            RefreshUI(player, "all");
            ServerMgr.Instance.StartCoroutine(StartUpdate(player));
            player.SetFlag(BaseEntity.Flags.Reserved3, true);
            ServerMgr.Instance.StopCoroutine(StartUpdate(player));
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            player.SetFlag(BaseEntity.Flags.Reserved3, false);
        }

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
                player.SetFlag(BaseEntity.Flags.Reserved3, false);
            }
        }

        #region Gui
        public void RefreshUI(BasePlayer player, string Type)
        {
            var c = new CuiElementContainer();

            switch (Type)
            {
                case "timeandonline":
                    CuiHelper.DestroyUi(player, Layer + "text.online");

                    UI.AddText1(ref c, Layer + "online", Layer + "text.online", "1 1 1 1", $"{BasePlayer.activePlayerList.Count}", TextAnchor.MiddleLeft, 11, "0 0", "1 1", "", "", "0 0 0 1", "robotocondensed-bold.ttf", "0.55 0.55");
                    break;

                case "all":
                    CuiHelper.DestroyUi(player, Layer + "invise");
                    CuiHelper.DestroyUi(player, Layer + "name");
                    CuiHelper.DestroyUi(player, Layer + "text.menu");
                    CuiHelper.DestroyUi(player, Layer + "menu");
                    CuiHelper.DestroyUi(player, Layer + "icons.store");
                    CuiHelper.DestroyUi(player, Layer + "icons.info");
                    CuiHelper.DestroyUi(player, Layer + "logoavaa2");
                    CuiHelper.DestroyUi(player, Layer + "poloskamenu1");
                    CuiHelper.DestroyUi(player, Layer + "name1");


                    //logo menu 2

                    CuiHelper.DestroyUi(player, Layer + "logoavaa2");
                    CuiHelper.DestroyUi(player, Layer + "poloskamenu1");

                    UI.AddImage(ref c, Layer, Layer + "logoavaa2", "0.8 0.8 0.8 0", "", "assets/icons/greyout.mat", "0.5 0.5", "0.5 0.5", "8.5 -50", "175.613 8");
                    UI.AddRawImage1(ref c, Layer + "logoavaa2", Layer + "logoavaa.online2", ImageLibrary?.Call<string>("GetImage", "bg1"), "0 0 0 1", "", "", "0 0", "1 1", "1 1", "-1 -1");

                    UI.AddImage(ref c, Layer, Layer + "poloskamenu1", "0.8 0.8 0.8 0", "", "assets/icons/greyout.mat", "0.5 0.5", "0.5 0.5", "43 -32", "100.613 -13");
                    UI.AddRawImage1(ref c, Layer + "poloskamenu1", Layer + "poloskamenu1.ava", ImageLibrary?.Call<string>("GetImage", "poloskamenu1"), "", "", "", "0 0", "1 1", "1 1", "-1 -1");

                    // text
                    UI.AddImage(ref c, Layer, Layer + "name", "0.8 0.8 0.8 0", "", "assets/icons/greyout.mat", "0.5 0.5", "0.5 0.5", "40 -14.014", "160.547 6");
                    UI.AddRawImage1(ref c, Layer + "name", Layer + "tittle", ImageLibrary?.Call<string>("GetImage", "name123"), "1 1 1 1", "", "", "0 0", "1 1", "1 1", "-1 -1");

                    UI.AddImage(ref c, Layer, Layer + "name1", "0.8 0.8 0.8 0", "", "assets/icons/greyout.mat", "0.5 0.5", "0.5 0.5", "48 -27.514", "95.547 -17");
                    UI.AddRawImage1(ref c, Layer + "name1", Layer + "tittle", ImageLibrary?.Call<string>("GetImage", "name456"), "1 1 1 1", "", "", "0 0", "1 1", "1 1", "-1 -1");

                    //buttons

                    CuiHelper.DestroyUi(player, Layer + "icons.store");
                    CuiHelper.DestroyUi(player, Layer + "icons.info");

                    UI.AddImage(ref c, Layer, Layer + "icons.store", "0.8 0.8 0.8 0", "", "assets/icons/greyout.mat", "0.5 0.5", "0.5 0.5", "84 -47", "100.337 -33.008");
                    UI.AddButton(ref c, Layer, Layer + "icons.store", $"chat.say /menu", "", "0 0 0 0.3", "", "assets/icons/greyout.mat", "0.5 0.5", "0.5 0.5", "84 -47", "100.337 -33.008");
                    UI.AddRawImage1(ref c, Layer + "icons.store", Layer + "icon.store", ImageLibrary?.Call<string>("GetImage", "info"), "1 1 1 1", "", "", "0.05 0", "0.95 1", "1 1", "-1 -1");

                    UI.AddImage(ref c, Layer, Layer + "icons.info", "0.8 0.8 0.8 0", "", "assets/icons/greyout.mat", "0.5 0.5", "0.5 0.5", "102.337 -47", "118.337 -33.008");
                    UI.AddButton(ref c, Layer, Layer + "icons.info", $"chat.say /store", "", "0 0 0 0.3", "", "assets/icons/greyout.mat", "0.5 0.5", "0.5 0.5", "102.337 -47", "118.337 -33.008");
                    UI.AddRawImage1(ref c, Layer + "icons.info", Layer + "icon.info", ImageLibrary?.Call<string>("GetImage", "store"), "1 1 1 1", "", "", "0 -0.1", "1 1.1", "1 1", "-1 -1");

                    //online menu


                    CuiHelper.DestroyUi(player, Layer + "online");
                    CuiHelper.DestroyUi(player, Layer + "icons.online");

                    UI.AddImage(ref c, Layer, Layer + "online", "0.8 0.8 0.8 0", "", "assets/icons/greyout.mat", "0.5 0.5", "0.5 0.5", "61.337 -57.3", "86 -24.008");
                    UI.AddImage(ref c, Layer, Layer + "icons.online", "0.8 0.8 0.8 0", "", "assets/icons/greyout.mat", "0.5 0.5", "0.5 0.5", "38.337 -45.3", "68.337 -24.008");
                    UI.AddRawImage1(ref c, Layer + "icons.online", Layer + "icon.online", ImageLibrary?.Call<string>("GetImage", "online"), "1 1 1 1", "", "", "0 -0.5", "1.1 1", "1 1", "-1 -1");

                    //logo menu

                    CuiHelper.DestroyUi(player, Layer + "logoavaa");

                    UI.AddImage(ref c, Layer, Layer + "logoavaa", "0.8 0.8 0.8 0", "", "assets/icons/greyout.mat", "0.5 0.5", "0.5 0.5", "-25 -62.48", "50.613 20.878");
                    UI.AddRawImage1(ref c, Layer + "logoavaa", Layer + "logoavaa.online", ImageLibrary?.Call<string>("GetImage", "logoava1"), "1 1 1 0.8", "", "", "0 0", "1 1", "1 1", "-1 -1");

                    //onlinetext

                    CuiHelper.DestroyUi(player, Layer + "menu");

                    UI.AddImage(ref c, Layer, Layer + "menu", "0.8 0.8 0.8 0.0", "", "assets/icons/greyout.mat", "0.5 0.5", "0.5 0.5", "-13 -60.48", "30.613 -25.878");
                    UI.AddText1(ref c, Layer + "online", Layer + "text.online", "1 1 1 1", $"{BasePlayer.activePlayerList.Count}", TextAnchor.MiddleLeft, 11, "0 0", "1 1", "", "", "0 0 0 1", "robotocondensed-bold.ttf", "0.55 0.55", 0);
                    UI.AddButton(ref c, Layer, Layer + "invise", "menuOpen", "", "0.8 0.8 0.8 0", "", "assets/icons/greyout.mat", "0.5 0.5", "0.5 0.5", "-13 -60.48", "50.613 15.878");

                    break;
                case "open":
                    CuiHelper.DestroyUi(player, Layer + "invise2");
                    CuiHelper.DestroyUi(player, Layer + "invise");

                    UI.AddButton(ref c, Layer, Layer + "invise2", "menuOpen", "", "0.8 0.8 0.8 0.0", "", "assets/icons/greyout.mat", "0.5 0.5", "0.5 0.5", "-13 -60.48", "50.613 15.878");
                    break;
            }
            CuiHelper.AddUi(player, c);
        }

        public void Main_menu(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);

            var c = new CuiElementContainer();
            //button store

            UI.AddImage(ref c, "Overlay", Layer, "0 0 0 0", "", "", "0 1", "0 1", "4.135 -43.232", "37.366 -0.011");

            CuiHelper.AddUi(player, c);
        }



        #endregion

        #region config

        public class PluginConfig
        {

        }

        protected override void LoadDefaultConfig()
        {
            config = new PluginConfig
            {
            };
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }
        PluginConfig config;

        public static class UI
        {
            public static void AddImage(ref CuiElementContainer container, string parrent, string name, string color, string sprite, string mat, string aMin, string aMax, string oMin, string oMax, string outline = "", string dist = "")
            {
                if (string.IsNullOrEmpty(sprite) && !string.IsNullOrEmpty(mat))
                    container.Add(new CuiElement()
                    {
                        Parent = parrent,
                        Name = name,
                        Components =
                        {
                            new CuiImageComponent{Color = color, Material = mat},
                            new CuiRectTransformComponent{AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax}
                        }
                    });

                if (string.IsNullOrEmpty(sprite) && string.IsNullOrEmpty(mat))
                    container.Add(new CuiElement()
                    {
                        Parent = parrent,
                        Name = name,
                        Components =
                    {
                        new CuiImageComponent{Color = color},
                        new CuiRectTransformComponent{AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax}
                    }
                    });
            }

            public static void AddRawImage(ref CuiElementContainer container, string parrent, string name, string png, string color, string sprite, string mat, string aMin, string aMax, string oMin, string oMax)
            {
                if (string.IsNullOrEmpty(sprite) && string.IsNullOrEmpty(mat))
                    container.Add(new CuiElement()
                    {
                        Parent = parrent,
                        Name = name,
                        Components =
                    {
                        new CuiRawImageComponent{Color = color, Png = png},
                        new CuiRectTransformComponent{AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax},
                        new CuiOutlineComponent{Color = "0 0 0 1", Distance = "0.55 0.55"}
                    }
                    });
            }


            public static void AddRawImage1(ref CuiElementContainer container, string parrent, string name, string png, string color, string sprite, string mat, string aMin, string aMax, string oMin, string oMax)
            {
                if (string.IsNullOrEmpty(sprite) && string.IsNullOrEmpty(mat))
                    container.Add(new CuiElement()
                    {
                        Parent = parrent,
                        Name = name,
                        Components =
                    {
                        new CuiRawImageComponent{Color = color, Png = png},
                        new CuiRectTransformComponent{AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax},
                    }
                    });
            }

            public static void AddText(ref CuiElementContainer container, string parrent, string name, string color, string text, TextAnchor align, int size, string aMin, string aMax, string oMin, string oMax, string outColor = "0 0 0 1", string font = "robotocondensed-bold.ttf", string dist = "0.55 0.55", float FadeIN = 0f, float FadeOut = 0f)
            {
                container.Add(new CuiElement()
                {
                    Parent = parrent,
                    Name = name,
                    FadeOut = FadeOut,
                    Components =
                    {
                        new CuiTextComponent{Color = color,Text = text, Align = align, FontSize = size, Font = font, FadeIn = 0.5f},
                        new CuiRectTransformComponent{AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax},
                    }
                });

            }

            public static void AddText1(ref CuiElementContainer container, string parrent, string name, string color, string text, TextAnchor align, int size, string aMin, string aMax, string oMin, string oMax, string outColor = "0 0 0 1", string font = "robotocondensed-bold.ttf", string dist = "0.55 0.55", float FadeIN = 0f, float FadeOut = 0f)
            {
                container.Add(new CuiElement()
                {
                    Parent = parrent,
                    Name = name,
                    FadeOut = FadeOut,
                    Components =
                    {
                        new CuiTextComponent{Color = color,Text = text, Align = align, FontSize = size, Font = font},
                        new CuiRectTransformComponent{AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax},
                    }
                });

            }
            public static void AddText2(ref CuiElementContainer container, string parrent, string name, string color, string text, TextAnchor align, int size, string aMin, string aMax, string oMin, string oMax, string outColor = "0 0 0 1", string font = "robotocondensed-bold.ttf", string dist = "0.55 0.55", float FadeIN = 0f, float FadeOut = 0f)
            {
                container.Add(new CuiElement()
                {
                    Parent = parrent,
                    Name = name,
                    FadeOut = FadeOut,
                    Components =
                    {
                        new CuiTextComponent{Color = color,Text = text, Align = align, FontSize = size, Font = font},
                        new CuiRectTransformComponent{AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax},
                        new CuiOutlineComponent{Color = "1 1 1 0.1", Distance = "0.1 0.1"}
                    }
                });

            }
            public static void AddButton(ref CuiElementContainer container, string parrent, string name, string cmd, string close, string color, string sprite, string mat, string aMin, string aMax, string oMin, string oMax, string outline = "", string dist = "")
            {
                if (string.IsNullOrEmpty(sprite) && !string.IsNullOrEmpty(mat))
                    container.Add(new CuiElement()
                    {
                        Parent = parrent,
                        Name = name,
                        Components =
                    {
                        new CuiButtonComponent{Command = cmd, Color = color, Close = close, Material = mat, },
                        new CuiRectTransformComponent{AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax}
                    }
                    });

                if (string.IsNullOrEmpty(sprite) && string.IsNullOrEmpty(mat))
                    container.Add(new CuiElement()
                    {
                        Parent = parrent,
                        Name = name,
                        Components =
                    {
                        new CuiButtonComponent{Command = cmd, Color = color, Close = close, },
                        new CuiRectTransformComponent{AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax}
                    }
                    });
            }
        }

        #endregion
    }
}