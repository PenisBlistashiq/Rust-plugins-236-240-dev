using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ConVar;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("XerCopterCraft", "Mercury", "1.0.3")]
    class XerCopterCraft : RustPlugin
    {
        /// <summary>
        ///  Обновление 1.0.3
        ///  - Поправлен подгрузка изображений
        ///  - Изменен метод форматирования HEX цетов
        ///  - Добавлена поддержка IQChat
        /// </summary>
        /// 

        #region Reference

        [PluginReference] Plugin ImageLibrary, IQChat;

        #region IQChat
        public void SendChat(BasePlayer player, string Message, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message, config.prefixChat, config.avatarChat);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
        #endregion

        #region IL
        private string GetImage(string fileName, ulong skin = 0)
        {
            var imageId = (string)plugins.Find("ImageLibrary").CallHook("GetImage", fileName, skin);
            if (!string.IsNullOrEmpty(imageId))
                return imageId;
            return string.Empty;
        }
        public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);
        public bool HasImage(string imageName) => (bool)ImageLibrary?.Call("HasImage", imageName);

        private IEnumerator DownloadImages()
        {
            var Items = config.CraftItemList;

            PrintError("AddImages SkyPlugins.ru...");
            foreach (var Item in Items)
            {
                if (!HasImage($"{Item.Key}_256px"))
                    AddImage($"http://api.skyplugins.ru/api/getimage/{Item.Key}/256", $"{Item.Key}_256px");
            }
            yield return new WaitForSeconds(0.04f);
            PrintError("AddImages SkyPlugins.ru - completed..");
        }
        #endregion

        #endregion

        #region Var
        private string prefab = "assets/content/vehicles/minicopter/minicopter.entity.prefab";
        #endregion

        #region Configuration
        private static Configuration config = new Configuration();

        private class Configuration
        {
            [JsonProperty("Avatar IQChat")]
            public ulong avatarChat = 0;
            [JsonProperty("Префикс IQChat")]
            public string prefixChat = "[Copter]";
            [JsonProperty("SkinId (Иконка в инвентаре)")]
            public ulong skinID = 1680939801;
            [JsonProperty("Миникоптер(Эту вещь игрок будет держать в руках,когда поставит - он заменится на коптер)")]
            public string Item = "electric.flasherlight";
            [JsonProperty("Название вещи в инвентаре")]
            public string ItemName = "Minicopter";
            [JsonProperty("Вещи для крафта")]
            public Dictionary<string, int> CraftItemList = new Dictionary<string, int>();

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    CraftItemList = new Dictionary<string, int>
                    {
                        ["metalblade"] = 10,
                        ["rope"] = 10,
                        ["gears"] = 15,
                        ["wood"] = 5000,
                        ["metal.fragments"] = 6500,
                    }
                };
            }
            
            internal class Interface
            {
                [JsonProperty("Title в меню")]
                public string TitleMenu = "Создание миникоптера";
                [JsonProperty("Title предметов в меню")]
                public string TitleItems = "Список предметов,которые требуются для создания миникоптера";
                [JsonProperty("Текст в кнопке")]
                public string ButtonTitle = "Создать";
                [JsonProperty("Символ показывающий,что у игрока достаточно предметов на крафт")]
                public string Sufficiently = "√";
                [JsonProperty("Цвет символа(HEX)")]
                public string SufficientlyColor = "#33F874FF";
                [JsonProperty("Цвет показателя,сколько необходимо еще компонентов на создание")]
                public string IndispensablyColor = "#F83232FF";
                [JsonProperty("Minicopter.png(512x512)")]
                public string CopterPNG = "https://i.imgur.com/PoeTa16.png";
            }

            internal class Other
            {
                [JsonProperty("Звук при создании коптера")]
                public string EffectCreatedCopter = "assets/prefabs/deployable/tier 1 workbench/effects/experiment-start.prefab";
                [JsonProperty("Звук когда у игрока недостаточно ресурсов")]
                public string EffectCanceled = "assets/prefabs/npc/autoturret/effects/targetlost.prefab";
            }

            [JsonProperty("Настройки интерфейса")]
            public Interface InterfaceSettings = new Interface();
            [JsonProperty("Дополнительные настройки")]
            public Other OtherSettings = new Other();
        }
    
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Ошибка чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);
      
        #endregion

        #region Commands

        [ChatCommand("craft")]
        void OpenCraftMenu(BasePlayer player)
        {
            if (player == null) return;
            if (permission.UserHasPermission(player.UserIDString, "xercoptercraft.use"))
                OpenMenuCraft(player);
            else SendChat(player, "Недостаточно прав");
        }

        [ConsoleCommand("craft_copter")]
        void CraftCopter(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (!CraftCheck(player))
            {
                Effect.server.Run(config.OtherSettings.EffectCanceled, player.transform.localPosition);
                MessageUI(player, "Недостаточно ресурсов", config.InterfaceSettings.IndispensablyColor);
                return;
            }
            foreach (var item in config.CraftItemList)
            {
                player.inventory.Take(null, ItemManager.FindItemDefinition(item.Key).itemid, item.Value);
            }
            GiveMinicopter(player);
            MessageUI(player, "Миникоптер создан успешно", config.InterfaceSettings.SufficientlyColor);
            Effect.server.Run(config.OtherSettings.EffectCreatedCopter, player.transform.localPosition);
            CuiHelper.DestroyUi(player, MainPanel);
            LogToFile("XerCopterLog", $"{player.displayName + "/" + player.UserIDString} скрафтил коптер",this);
            PrintWarning($"{player.displayName + "/" + player.UserIDString} скрафтил коптер");
        }

        [ConsoleCommand("give_minicopter")]
        void GiveMinicopterCommand(ConsoleSystem.Arg args)
        {
            BasePlayer target = BasePlayer.FindByID(ulong.Parse(args.Args[0]));
            if (target == null) { PrintWarning("Игрока нет на сервере!Он не получил миникоптер!"); return; };
            GiveMinicopter(target);
            PrintWarning($"Миникоптер выдан игроку {target.userID}");
            if(target.IsConnected)
               MessageUI(target, "Вы получили миникоптер", config.InterfaceSettings.SufficientlyColor);
        }

        #endregion

        #region Hooks
        void Unload() => ServerMgr.Instance.StopCoroutine(DownloadImages());

        void OnServerInitialized()
        {
            permission.RegisterPermission("xercoptercraft.use", this);
            AddImage(config.InterfaceSettings.CopterPNG, "CopterImage");
            ServerMgr.Instance.StartCoroutine(DownloadImages());
        }

        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            CheckDeploy(go.ToBaseEntity());
        }

        #endregion

        #region Main

        private void SpawnCopter(Vector3 position, Quaternion rotation = default(Quaternion), ulong ownerID = 0)
        {
            MiniCopter copter = (MiniCopter)GameManager.server.CreateEntity(prefab, position, rotation);
            if (copter == null) { return; }
            copter.Spawn();
        }

        private void GiveMinicopter(BasePlayer player, bool pickup = false)
        {
            var item = CreateItem();
            player.GiveItem(item);
        }

        private void CheckDeploy(BaseEntity entity)
        {
            if (entity == null) { return; }
            if (!CopterCheck(entity.skinID)) { return; }
            SpawnCopter(entity.transform.position, entity.transform.rotation, entity.OwnerID);
            timer.Once(0.5f, () => { entity.Kill(); });
        }

        private bool CopterCheck(ulong skin)
        {
            return skin != 0 && skin == config.skinID;
        }

        private Item CreateItem()
        {
            var item = ItemManager.CreateByName(config.Item, 1, config.skinID);
            if (item == null)
            {
                return null;
            }
            item.name = config.ItemName;
            return item;
        }

        private bool CraftCheck(BasePlayer player)
        {
            var craft = config.CraftItemList;
            var more = new Dictionary<string, int>();

            foreach (var component in craft)
            {
                var name = component.Key;
                var has = player.inventory.GetAmount(ItemManager.FindItemDefinition(component.Key).itemid);
                var need = component.Value;
                if (has < component.Value)
                {
                    if (!more.ContainsKey(name))
                    {
                        more.Add(name, 0);
                    }

                    more[name] += need - has;
                }
            }

            if (more.Count == 0)
                return true;
            else
                return false;
        }

        private bool UseCraft(BasePlayer player, string Short)
        {
            var craft = config.CraftItemList;
            var more = new Dictionary<string, int>();

            foreach (var component in craft)
            {
                var name = component.Key;
                var has = player.inventory.GetAmount(ItemManager.FindItemDefinition(component.Key).itemid);
                var need = component.Value;
                if (has < component.Value)
                {
                    if (!more.ContainsKey(name))
                    {
                        more.Add(name, 0);
                    }

                    more[name] += need - has;
                }
            }

            if (more.ContainsKey(Short))
                return true;
            else
                return false;
        }

        #endregion

        #region UI

        #region Parent
        static string MainPanel = "XCC_MAINPANEL_3365";
        static string CraftItemsPanel = "XCC_CRAFT_ITEMS_PANEL";
        static string ItemParent = "XCC_CRAFT_ITEMS_PARENT";
        static string MessagePanel = "XCC_MESSAGE_PANEL";
        #endregion

        #region Message

        void MessageUI(BasePlayer player, string Messages, string Color)
        {
            CuiHelper.DestroyUi(player, MessagePanel);
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.3291668 0.8583333", AnchorMax = "0.6614581 0.9166667" },
                Image = {FadeIn = 0.4f, Color = HexToRustFormat(Color) }
            }, "Overlay", MessagePanel);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text = { Text = String.Format(Messages), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 30, Color = HexToRustFormat("#FFFFFFFF") }
            }, MessagePanel);

            CuiHelper.AddUi(player, container);

            timer.Once(2f, () => { CuiHelper.DestroyUi(player, MessagePanel); });
        }

        #endregion

        #region MainMenu

        void OpenMenuCraft(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, MainPanel);
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1"},
                Image = { Color = HexToRustFormat("#0000008F") }
            }, "Overlay", MainPanel);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "-100 -100", AnchorMax = "100 100" },
                Button = { Close = MainPanel, Color = "0 0 0 0" },
                Text = { FadeIn = 0.8f, Text = "" }
            }, MainPanel);

            #region Titles

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.8962963", AnchorMax = "1 1" },
                Text = { Text = String.Format(config.InterfaceSettings.TitleMenu), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 30, Color = HexToRustFormat("#FFFFFFFF") }
            }, MainPanel);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.4805566", AnchorMax = "1 0.5546331" },
                Text = { Text = String.Format(config.InterfaceSettings.TitleItems), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 30, Color = HexToRustFormat("#FFFFFFFF") }
            }, MainPanel);

            #endregion

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.4078125 0.5546297", AnchorMax = "0.5895833 0.5962934" },
                Button = { Command = "craft_copter", Color = HexToRustFormat("#319A56FF") },
                Text = { FadeIn = 0.9f, Text = config.InterfaceSettings.ButtonTitle,Align = TextAnchor.MiddleCenter, FontSize = 25 }
            }, MainPanel);

            container.Add(new CuiElement
            {
                Parent = MainPanel,
                Components =
                {
                    new CuiRawImageComponent {
                        Png = GetImage("CopterImage"), 
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.3989581 0.6",
                        AnchorMax = "0.5968745 0.8925924"
                    },
                }
            });

            #region CraftItems

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.1442708 0.01944444", AnchorMax = "0.8614583 0.4787037" },
                Image = { Color = "0 0 0 0" }
            },  MainPanel, CraftItemsPanel);

            int x = 0, y = 0, i = 0;
            foreach (var items in config.CraftItemList)
            {
                string color = UseCraft(player, items.Key) ? "#A60D0D2F" : "#1FB91931";

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{0.01386664 + (x * 0.17)} {0.5463711 - (y * 0.45)}", AnchorMax = $"{0.1416122 + (x * 0.17)} {0.8891131 - (y * 0.45)}" },
                    Image = { Color = HexToRustFormat(color) }
                }, CraftItemsPanel, $"Item_{i}");

                container.Add(new CuiElement
                {
                    Parent = $"Item_{i}",
                    Components =
                    {
                    new CuiRawImageComponent {
                        Png = GetImage($"{items.Key}_256px"),
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    },
                }
                });

                var has = player.inventory.GetAmount(ItemManager.FindItemDefinition(items.Key).itemid);
                var result = items.Value - has <= 0 ? $"<color={config.InterfaceSettings.SufficientlyColor}>{config.InterfaceSettings.Sufficiently}</color>" : $"<color={config.InterfaceSettings.IndispensablyColor}>{Convert.ToInt32(items.Value - has).ToString()}</color>";

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = String.Format(result.ToString()), Align = TextAnchor.LowerCenter, Font = "robotocondensed-bold.ttf", FontSize = 16, Color = HexToRustFormat("#FFFFFFFF") }
                }, $"Item_{i}");


                x++; i++;
                if (x == 6)
                {
                    x = 0;
                    y++;
                }
                if (x == 6 && y == 1)
                    break;
            }
            #endregion

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #endregion

        #region Help

        public static StringBuilder sb = new StringBuilder();
        private static string HexToRustFormat(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            sb.Clear();
            return sb.AppendFormat("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a).ToString();
        }

        #endregion        
    }
}
