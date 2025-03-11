using System.Linq;
using System;
using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("BlockSystem", "Chibubrik", "1.0.0")]
    class BlockSystem : RustPlugin
    {
        #region Вар
        [PluginReference] Plugin ImageLibrary;
        #endregion

        #region Предметы
        Dictionary<int, List<string>> settings = new Dictionary<int, List<string>>() 
        {
            [7200] = new List<string>() 
            {
                "shotgun.double",
                "pistol.revolver"
            },
            [14400] = new List<string>() 
            {
                "pistol.semiauto",
                "pistol.python",
                "pistol.m92"
            },
            [21600] = new List<string>() 
            {
                "shotgun.pump",
                "smg.2",
                "grenade.f1",
                "rifle.semiauto",
                "coffeecan.helmet",
                "roadsign.jacket",
                "roadsign.kilt"
            },
            [43200] = new List<string>() 
            {
                "smg.thompson",
                "smg.mp5",
                "shotgun.spas12",
                "grenade.beancan"
            },
            [64800] = new List<string>() 
            {
                "rifle.ak",
                "rifle.lr300",
                "rifle.bolt",
                "explosive.satchel",
                "metal.facemask",
                "metal.plate.torso",
                "rifle.l96",
                "rifle.m39"
            },
            [86400] = new List<string>() 
            {
                "lmg.m249",
                "ammo.rifle.explosive",
                "explosive.timed",
                "rocket.launcher",
                "heavy.plate.helmet",
                "heavy.plate.jacket",
                "heavy.plate.pants"
            },
        };
        #endregion

        #region Хуки
        void OnServerInitialized()
        {
            foreach (var check in settings.SelectMany(p => p.Value))
            {
                ImageLibrary.Call("AddImage", $"https://rustlabs.com/img/items180/{check}.png", check);
            }
        }

        private object CanWearItem(PlayerInventory inventory, Item item) {
            var player = inventory.gameObject.ToBaseEntity() as BasePlayer;
            var isBlocked = IsBlocked(item.info) > 0 ? false : (bool?) null;
            
            if (isBlocked == false) {
                if (player.GetComponent<NPCPlayer>() != null || player.GetComponent<BaseNpc>() != null || player.IsNpc)
                    return null;

                SendReply(player, "Предмет " + item.info.shortname + " временно заблокирован, подождите " + FormatShortTime(TimeSpan.FromSeconds(IsBlocked(item.info))));
            }

            return isBlocked;
        }

        private object CanEquipItem(PlayerInventory inventory, Item item) {
            var player = inventory.gameObject.ToBaseEntity() as BasePlayer;
            if (player == null)
                return null;
            
            var isBlocked = IsBlocked(item.info) > 0 ? false : (bool?) null;
            if (isBlocked == false) {
                if (player.GetComponent<NPCPlayer>() != null || player.GetComponent<BaseNpc>() != null || player.IsNpc)
                    return null;

                    SendReply(player, "Предмет " + item.info.shortname + " временно заблокирован, подождите " + FormatShortTime(TimeSpan.FromSeconds(IsBlocked(item.info))));
            }

            return isBlocked;
        }

        private object CanMoveItem(Item item, PlayerInventory inventory, uint targetContainer) {
            if (inventory == null || item == null)
                return null;

            BasePlayer player = inventory.GetComponent<BasePlayer>();
            if (player == null)
                return null;

            ItemContainer container = inventory.FindContainer(targetContainer);
            if (container == null || container.entityOwner == null)
                return null;

            if (container.entityOwner is AutoTurret) {
                var isBlocked = IsBlocked(item.info.shortname) > 0 ? false : (bool?) null;
                if (isBlocked == false) {
                    SendReply(player, "Предмет " + item.info.shortname + " временно заблокирован, подождите " + FormatShortTime(TimeSpan.FromSeconds(IsBlocked(item.info))));
                    return true;
                }
            }

            return null;
        }

        private object OnReloadWeapon(BasePlayer player, BaseProjectile projectile)
        {
            if (player is NPCPlayer)
                return null;
            
            if (player.GetComponent<NPCPlayer>() != null || player.GetComponent<BaseNpc>() != null || player.IsNpc)
                return null;

            var isBlocked = IsBlocked(projectile.primaryMagazine.ammoType) > 0 ? false : (bool?) null;
            if (isBlocked == false) {
                List<Item> list = player.inventory.FindItemIDs(projectile.primaryMagazine.ammoType.itemid).ToList<Item>();
                if (list.Count == 0) {
                    List<Item> list2 = new List<Item>();
                    player.inventory.FindAmmo(list2, projectile.primaryMagazine.definition.ammoTypes);
                    if (list2.Count > 0) {
                        isBlocked = IsBlocked(list2[0].info) > 0 ? false : (bool?) null;
                    }
                }

                if (isBlocked == false) {
                    SendReply(player, $"Вы <color=#81B67A>не можете</color> использовать этот тип боеприпасов!");
                }

                return isBlocked;
            }

            return null;
        }

        private object OnReloadMagazine(BasePlayer player, BaseProjectile projectile) {
            if (player is NPCPlayer)
                return null;

            NextTick(() => {
                var isBlocked = IsBlocked(projectile.primaryMagazine.ammoType) > 0 ? false : (bool?) null;
                if (isBlocked == false) {
                    player.GiveItem(ItemManager.CreateByItemID(projectile.primaryMagazine.ammoType.itemid, projectile.primaryMagazine.contents, 0UL), BaseEntity.GiveItemReason.Generic);
                    projectile.primaryMagazine.contents = 0;
                    projectile.GetItem().LoseCondition(projectile.GetItem().maxCondition);
                    projectile.SendNetworkUpdate();
                    player.SendNetworkUpdate();

                    SendReply(player, $"<color=#81B67A>Хорошая</color> попытка, правда ваше оружие теперь сломано!");
                }
            });

            return null;
        }

        private object CanAcceptItem(ItemContainer container, Item item) {
            if (container == null || item == null || container.entityOwner == null)
                return null;

            if (container.entityOwner is AutoTurret) {
                BasePlayer player = item.GetOwnerPlayer();
                if (player == null)
                    return null;

                var isBlocked = IsBlocked(item.info.shortname) > 0 ? false : (bool?) null;
                if (isBlocked == false) {
                    return ItemContainer.CanAcceptResult.CannotAcceptRightNow;
                }
            }

            return null;
        }
        #endregion

        #region Методы
        private double IsBlocked(ItemDefinition itemDefinition) => IsBlocked(itemDefinition.shortname);
        private double IsBlocked(string shortname) 
        {
            if (!settings.SelectMany(p => p.Value).Contains(shortname))
                return 0;
            var blockTime = settings.FirstOrDefault(p => p.Value.Contains(shortname)).Key;
            var lefTime = (UnBlockTime(blockTime)) - CurrentTime();
            return lefTime > 0 ? lefTime : 0;
        }

        private double UnBlockTime(int amount) => SaveRestore.SaveCreatedTime.ToUniversalTime().Subtract(epoch).TotalSeconds + amount;
        static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        static double CurrentTime() { return DateTime.UtcNow.Subtract(epoch).TotalSeconds; }

        public static string FormatShortTime(TimeSpan time) 
        {
            string result = string.Empty;
            result = $"{time.Hours.ToString("00")}:";
            result += $"{time.Minutes.ToString("00")}";
            return result;
        }
        #endregion

        #region Команда
        [ChatCommand("block")]
        void ChatBlock(BasePlayer player) => BlockUI(player);
        #endregion

        #region Интерфейс
        void BlockUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "Block");
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.9", Material = "assets/content/ui/uibackgroundblur.mat" }
            }, "Overlay", "Block");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0.9", Close = "Block" },
                Text = { Text = "" }
            }, "Block");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.513 0.73", AnchorMax = "0.515 0.76", OffsetMax = "0 0" },
                Image = { Color = "0.46 0.73 0.43 1" }
            }, "Block");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.42 0.73", AnchorMax = "0.515 0.733", OffsetMax = "0 0" },
                Image = { Color = "0.46 0.73 0.43 1" }
            }, "Block");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.42 0.3", AnchorMax = "0.422 0.73", OffsetMax = "0 0" },
                Image = { Color = "0.46 0.73 0.43 1" }
            }, "Block");
            
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.42 0.3", AnchorMax = "0.515 0.304", OffsetMax = "0 0" },
                Image = { Color = "0.46 0.73 0.43 1" }
            }, "Block");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.513 0.27", AnchorMax = "0.515 0.304", OffsetMax = "0 0" },
                Image = { Color = "0.46 0.73 0.43 1" }
            }, "Block");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.43 0.27", AnchorMax = "0.59 0.274", OffsetMax = "0 0" },
                Image = { Color = "0.46 0.73 0.43 1" }
            }, "Block");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.07 0.21", AnchorMax = "0.96 0.3", OffsetMax = "0 0" },
                Text = { Text = $"Magix rust", Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, "Block");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.04 0.76", AnchorMax = "0.96 0.83", OffsetMax = "0 0" },
                Text = { Text = $"<b><size=20>БЛОКИРОВКА ПРЕДМЕТОВ</size></b>\nЗдесь вы можете узнать когда будет доступен тот, или иной предмет.", Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 10, Font = "robotocondensed-regular.ttf" }
            }, "Block");

            var color1 = IsBlocked(settings.ElementAt(0).Value.ElementAt(0)) > 0 ? "0.80 0.34 0.34 1" : "0.46 0.73 0.43 1";
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.395 0.691", AnchorMax = "0.475 0.694", OffsetMax = "0 0" },
                Image = { Color = color1 }
            }, "Block");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.345 0.387", AnchorMax = "0.4 1", OffsetMax = "0 0" },
                Text = { Text = $"{TimeSpan.FromSeconds(settings.ElementAt(0).Key).TotalHours} ЧАСА", Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-regular.ttf" }
            }, "Block");

            var color2 = IsBlocked(settings.ElementAt(1).Value.ElementAt(0)) > 0 ? "0.80 0.34 0.34 1" : "0.46 0.73 0.43 1";
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.375 0.622", AnchorMax = "0.475 0.626", OffsetMax = "0 0" },
                Image = { Color = color2 }
            }, "Block");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.305 0.613", AnchorMax = "0.4 1", OffsetMax = "0 0" },
                Text = { Text = $"{TimeSpan.FromSeconds(settings.ElementAt(1).Key).TotalHours} ЧАСА", Color = "1 1 1 0.5", Align = TextAnchor.LowerCenter, FontSize = 14, Font = "robotocondensed-regular.ttf" }
            }, "Block");

            var color3 = IsBlocked(settings.ElementAt(2).Value.ElementAt(0)) > 0 ? "0.80 0.34 0.34 1" : "0.46 0.73 0.43 1";
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.355 0.552", AnchorMax = "0.475 0.556", OffsetMax = "0 0" },
                Image = { Color = color3 }
            }, "Block");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.26 0.543", AnchorMax = "0.4 1", OffsetMax = "0 0" },
                Text = { Text = $"{TimeSpan.FromSeconds(settings.ElementAt(2).Key).TotalHours} ЧАСОВ", Color = "1 1 1 0.5", Align = TextAnchor.LowerCenter, FontSize = 14, Font = "robotocondensed-regular.ttf" }
            }, "Block");
 
            var color4 = IsBlocked(settings.ElementAt(3).Value.ElementAt(0)) > 0 ? "0.80 0.34 0.34 1" : "0.46 0.73 0.43 1";
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.335 0.481", AnchorMax = "0.475 0.484", OffsetMax = "0 0" },
                Image = { Color = color4 }
            }, "Block");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.215 0.47", AnchorMax = "0.4 1", OffsetMax = "0 0" },
                Text = { Text = $"{TimeSpan.FromSeconds(settings.ElementAt(3).Key).TotalHours} ЧАСОВ", Color = "1 1 1 0.5", Align = TextAnchor.LowerCenter, FontSize = 14, Font = "robotocondensed-regular.ttf" }
            }, "Block");

            var color5 = IsBlocked(settings.ElementAt(4).Value.ElementAt(0)) > 0 ? "0.80 0.34 0.34 1" : "0.46 0.73 0.43 1";
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.315 0.408", AnchorMax = "0.475 0.412", OffsetMax = "0 0" },
                Image = { Color = color5 }
            }, "Block");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.175 0.4", AnchorMax = "0.4 1", OffsetMax = "0 0" },
                Text = { Text = $"{TimeSpan.FromSeconds(settings.ElementAt(4).Key).TotalHours} ЧАСОВ", Color = "1 1 1 0.5", Align = TextAnchor.LowerCenter, FontSize = 14, Font = "robotocondensed-regular.ttf" }
            }, "Block");

            var color6 = IsBlocked(settings.ElementAt(5).Value.ElementAt(0)) > 0 ? "0.80 0.34 0.34 1" : "0.46 0.73 0.43 1";
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.295 0.341", AnchorMax = "0.475 0.345", OffsetMax = "0 0" },
                Image = { Color = color6 }
            }, "Block");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.14 0.332", AnchorMax = "0.4 1", OffsetMax = "0 0" },
                Text = { Text = $"{TimeSpan.FromSeconds(settings.ElementAt(5).Key).TotalHours} ЧАСА", Color = "1 1 1 0.5", Align = TextAnchor.LowerCenter, FontSize = 14, Font = "robotocondensed-regular.ttf" }
            }, "Block");

            float width = 0.9f, height = 0.07f, startxBox = 0.394f, startyBox = 0.727f - height, xmin = startxBox, ymin = startyBox;
            for (int z = 0; z < settings.Count(); z++)
            {
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = xmin + " " + ymin, AnchorMax = (xmin + width) + " " + (ymin + height * 1), OffsetMin = "2 2", OffsetMax = "-2 -2" },
                    Image = { Color = "0 0 0 0" }
                }, "Block", "Items");
                xmin += width;
                if (xmin + width >= 1)
                {
                    xmin = startxBox;
                    ymin -= height;
                }

                float width1 = 0.045f, height1 = 1f, startxBox1 = 0.09f, startyBox1 = 1f - height1, xmin1 = startxBox1, ymin1 = startyBox1;
                var item = settings.ElementAt(z).Value;
                foreach (var check in item)
                {
                    var color = IsBlocked(check) > 0 ? "0.80 0.34 0.34 1" : "0.46 0.73 0.43 1";
                    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = xmin1 + " " + ymin1, AnchorMax = (xmin1 + width1) + " " + (ymin1 + height1 * 1), OffsetMin = "2 0", OffsetMax = "-2 0" },
                        Image = { Color = color }
                    }, "Items", "Settings");
                    xmin1 += width1;

                    container.Add(new CuiElement
                    {
                        Parent = "Settings",
                        Components =
                        {
                            new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", check) },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "6 6", OffsetMax = "-6 -6" }
                        }
                    });
                }
            }

            CuiHelper.AddUi(player, container);
        }
        #endregion 
    }
}