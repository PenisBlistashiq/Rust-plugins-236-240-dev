using System;
using System.Linq;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Kits", "Chibubrik", "1.0.0")]
    class Kits : RustPlugin
    {
        #region Вар
        private List<Kit> _kits;
        private Dictionary<ulong, Dictionary<string, KitData>> _kitsData;
        private Dictionary<BasePlayer, List<string>> _kitsGUI = new Dictionary<BasePlayer, List<string>>();

        private string Layer = "KITS_UI";
        private string LayerInfo = "Kits_UI";
        [PluginReference] Plugin ImageLibrary;
        #endregion

        #region Конфиг
        public Configuration config;
        public class Configuration
        {
            [JsonProperty("Наборы при респавне")] public List<string> CustomKit;
            public static Configuration GetNewConfig()
            {
                return new Configuration
                {
                    CustomKit = new List<string>
                    {
                        "autokit",
                        "autokit2"
                    }
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config?.CustomKit == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Ошибка чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfig();
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Класс
        public class Kit
        {
            [JsonProperty("Название")] public string Name;
            [JsonProperty("Формат названия")] public string DisplayName;
            [JsonProperty("Максимум использований")] public int Amount;
            [JsonProperty("Кулдаун")] public double Cooldown;
            [JsonProperty("Виден или скрыт")] public bool Hide;
            [JsonProperty("Привилегия")] public string Permission;
            [JsonProperty("Изображение")] public string Url;
            [JsonProperty("Предметы")] public List<KitItem> Items;
        }
        public class KitItem
        {
            [JsonProperty("Название предмета")] public string ShortName;
            [JsonProperty("Количество")] public int Amount;
            [JsonProperty("Чертеж")] public int Blueprint;
            [JsonProperty("Место")] public string Container;
            [JsonProperty("Состояние")] public float Condition;
            [JsonProperty("Скин")] public ulong SkinID;
            [JsonProperty("Оружие")] public Weapon Weapon;
            public List<ItemContent> Content;

        }
        public class Weapon
        {
            [JsonProperty("Название боеприпаса")] public string ammoType;
            [JsonProperty("Количество")] public int ammoAmount;
        }
        public class ItemContent
        {
            [JsonProperty("Название предмета")] public string ShortName;
            [JsonProperty("Состояние")] public float Condition;
            [JsonProperty("Количество")] public int Amount;
        }
        public class KitData
        {
            [JsonProperty("Количество")] public int Amount;
            [JsonProperty("Кулдаун")] public double Cooldown;
        }
        #endregion

        #region Хуки
        private void OnPlayerRespawned(BasePlayer player)
        {
            foreach (var kits in config.CustomKit)
            {
                if (_kits.Exists(x => x.Name == kits))
                {
                    var kit1 = _kits.First(x => x.Name == kits);
                    if (permission.UserHasPermission(player.UserIDString, kit1.Permission))
                    {
                        player.inventory.Strip();
                        GiveItems(player, kit1);
                        return;
                    }
                }
            }
            if (_kits.Exists(x => x.Name == "autokit"))
            {
                player.inventory.Strip();
                var kit = _kits.First(x => x.Name == "autokit");
                GiveItems(player, kit);
            }

        }

        private void SaveKits()
        {
            Interface.Oxide.DataFileSystem.WriteObject("Kits/KitsSettings", _kits);
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("Kits/Kits_Data", _kitsData);
        }

        private void OnServerSave()
        {
            SaveData();
            SaveKits();
        }

        private void Loaded()
        {
            _kits = Interface.Oxide.DataFileSystem.ReadObject<List<Kit>>("Kits/KitsSettings");
            _kitsData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Dictionary<string, KitData>>>("Kits/Kits_Data");
        }

        void OnServerInitialized()
        {
            timer.Every(5, () =>
            {
                BasePlayer.activePlayerList.ToList().ForEach(p =>
                {
                    List<string> names = new List<string> { "#MAGIXRUST"};
                    if (names.Any(t => p.displayName.ToLower().Contains(t.ToLower())))
                    {
                        permission.GrantUserPermission(p.UserIDString, "kits.bonus", this);
                    }
                    else
                    {
                        permission.RevokeUserPermission(p.UserIDString, "kits.bonus");
                    }
                });
            });
            
            foreach (var check in _kits)
            {
                if (!permission.PermissionExists(check.Permission))
                    permission.RegisterPermission(check.Permission, this);
                ImageLibrary.Call("AddImage", check.Url, check.Url);
            }

            ImageLibrary.Call("AddImage", "https://imgur.com/c7ccHY5.png", "tF1Max3");
            ImageLibrary.Call("AddImage", "https://imgur.com/tF1Max3.png", "c7ccHY5");

            timer.Repeat(1, 0, RefreshCooldownKitsUI);
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            _kitsGUI.Remove(player);
        }

        private void Unload()
        {
            SaveData();
            
            foreach (var player in BasePlayer.activePlayerList)
            {
                DestroyUI(player);
                CuiHelper.DestroyUi(player, LayerInfo);
            }
        }
        #endregion

        #region Команды
        [ConsoleCommand("kit")]
        private void CommandConsoleKit(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null)
                return;

            var player = arg.Player();

            if (!arg.HasArgs())
                return;

            var value = arg.Args[0].ToLower();

            if (value == "ui")
            {
                if (_kitsGUI.ContainsKey(player))
                {
                    DestroyUI(player);
                }
                else
                {
                    DrawUI(player);
                    return;
                }
            }

            if (!_kitsGUI.ContainsKey(player))
                return;

            if (!_kitsGUI[player].Contains(value))
                return;

            GiveKit(player, value);

            var container = new CuiElementContainer();
            var check = _kits.First(x => x.Name == value);
            var data = GetPlayerData(player.userID, value);

            if (check.Amount > 0)
            {
                if (data.Amount >= check.Amount)
                {
                    player.SendConsoleCommand("kit ui");
                    foreach (var name in _kitsGUI[player])
                    {
                        CuiHelper.DestroyUi(player, $"{name}.time");
                        CuiHelper.DestroyUi(player, $"{name}.button");
                        CuiHelper.DestroyUi(player, $"{name}.imagess");
                        CuiHelper.DestroyUi(player, $"{name}.images");
                        CuiHelper.DestroyUi(player, $"{name}.buttons");
                        CuiHelper.DestroyUi(player, $"{name}.available");
                        CuiHelper.DestroyUi(player, $"{name}");
                    }

                    CuiHelper.AddUi(player, container);
                    return;
                }
            }

            if (check.Cooldown > 0)
            {
                var currentTime = GetCurrentTime();
                if (data.Cooldown > currentTime)
                {
                    CuiHelper.DestroyUi(player, $"{check.Name}.time");
                    CuiHelper.DestroyUi(player, $"{check.Name}.button");
                    CuiHelper.DestroyUi(player, $"{check.Name}.imagess");
                    CuiHelper.DestroyUi(player, $"{check.Name}.images");
                    CuiHelper.DestroyUi(player, $"{check.Name}.buttons");
                    CuiHelper.DestroyUi(player, $"{check.Name}.available");

                    CooldownUI(ref container, value, TimeSpan.FromSeconds(data.Cooldown - currentTime));
                }
            }

            CuiHelper.AddUi(player, container);

            return;
        }
	
		[ChatCommand("kitmenu")]
		private void CommandChatKitMenu(BasePlayer player, string command, string[] args) => CommandChatKit(player, command, args);

        [ChatCommand("kit")]
        private void CommandChatKit(BasePlayer player, string command, string[] args)
        {
            if (player == null)
                return;

            if (args.Length == 0)
            {
                if (_kitsGUI.ContainsKey(player))
                {
                    DestroyUI(player);
                    return;
                }
                else
                {
                    DrawUI(player);
                    return;
                }
            }

            if (!player.IsAdmin)
            {
                GiveKit(player, args[0].ToLower());
                return;
            }

            switch (args[0].ToLower())
            {
                case "new":
                    if (args.Length < 2)
                        SendReply(player, "/kit new название - создать набор");
                    else
                        KitCommandAdd(player, args[1].ToLower());
                    return;
                case "remove":
                    if (args.Length < 2)
                        SendReply(player, "/kit remove название удалить набор");
                    else
                        KitCommandRemove(player, args[1].ToLower());
                    return;
                case "reset":
                    _kitsData.Clear();

                    SendReply(player, "Вы обнулили все данные о использовании наборов игроков");
                    return;
                default:
                    GiveKit(player, args[0].ToLower());
                    return;
            }
        }

        [ConsoleCommand("kits")]
        private void CommandKits(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player != null && args.HasArgs(1))
            {
                if (args.Args[0] == "skip")
                {
                    DrawUI(player, int.Parse(args.Args[1]));
                }
            }
        }
        #endregion

        #region Кит
        private bool GiveKit(BasePlayer player, string kitname)
        {
            if (string.IsNullOrEmpty(kitname))
                return false;

            if (Interface.Oxide.CallHook("canRedeemKit", player) != null)
            {
                return false;
            }
            if (!_kits.Exists(x => x.Name == kitname))
            {
                SendReply(player, "Этого комплекта не существует");
                return false;
            }

            var kit = _kits.First(x => x.Name == kitname);

            if (!string.IsNullOrEmpty(kit.Permission) && !permission.UserHasPermission(player.UserIDString, kit.Permission))
            {
                SendReply(player, "У вас нет полномочий использовать этот комплект");
                return false;
            }

            var data = GetPlayerData(player.userID, kitname);

            if (kit.Amount > 0 && data.Amount >= kit.Amount)
            {
                SendReply(player, "Вы уже использовали этот комплект максимальное количество раз");
                return false;
            }

            if (kit.Cooldown > 0)
            {
                var currentTime = GetCurrentTime();
                if (data.Cooldown > currentTime)
                {
                    SendReply(player, $"Вы сможете использовать этот комплект через <color=#FFB02E>{FormatTime(TimeSpan.FromSeconds(data.Cooldown - currentTime))}</color>");
                    return false;
                }
            }

            int beltcount = kit.Items.Where(i => i.Container == "belt").Count();
            int wearcount = kit.Items.Where(i => i.Container == "wear").Count();
            int maincount = kit.Items.Where(i => i.Container == "main").Count();
            int totalcount = beltcount + wearcount + maincount;
            if ((player.inventory.containerBelt.capacity - player.inventory.containerBelt.itemList.Count) < beltcount || (player.inventory.containerWear.capacity - player.inventory.containerWear.itemList.Count) < wearcount || (player.inventory.containerMain.capacity - player.inventory.containerMain.itemList.Count) < maincount)
                if (totalcount > (player.inventory.containerMain.capacity - player.inventory.containerMain.itemList.Count))
                {
                    player.ChatMessage("Недостаточно места в инвентаре");
                    return false;
                }
            GiveItems(player, kit);

            if (kit.Amount > 0) data.Amount += 1;

            if (kit.Cooldown > 0)  data.Cooldown = GetCurrentTime() + kit.Cooldown;

            SendReply(player, $"Вы успешно получили набор <color=#53afd4>{kit.DisplayName}</color>");
            return true;
        }

        private void KitCommandAdd(BasePlayer player, string kitname)
        {
            if (_kits.Exists(x => x.Name == kitname))
            {
                SendReply(player, "Этот набор уже существует");
                return;
            }

            _kits.Add(new Kit
            {
                Name = kitname,
                DisplayName = kitname,
                Cooldown = 600,
                Hide = true,
                Permission = "kits.default",
                Amount = 0,
                Url = "",
                Items = GetPlayerItems(player)
            });
            permission.RegisterPermission($"kits.default", this);
            SendReply(player, $"Вы создали новый набор {kitname}");

            SaveKits();
            SaveData();
        }

        private void KitCommandRemove(BasePlayer player, string kitname)
        {
            if (_kits.RemoveAll(x => x.Name == kitname) <= 0)
            {
                SendReply(player, "Этого набора не существует");
                return;
            }

            SendReply(player, $"Набор <color=#FFB02E>{kitname}</color> был удалён");

            SaveKits();
        }

        private void GiveItems(BasePlayer player, Kit kit)
        {
            foreach (var kitem in kit.Items)
            {
                GiveItem(player,
                    BuildItem(kitem.ShortName, kitem.Amount, kitem.SkinID, kitem.Condition, kitem.Blueprint,
                        kitem.Weapon, kitem.Content),
                    kitem.Container == "belt" ? player.inventory.containerBelt :
                    kitem.Container == "wear" ? player.inventory.containerWear : player.inventory.containerMain);
            }
        }
        private void GiveItem(BasePlayer player, Item item, ItemContainer cont = null)
        {
            if (item == null) return;
            var inv = player.inventory;

            var moved = item.MoveToContainer(cont) || item.MoveToContainer(inv.containerMain);
            if (!moved)
            {
                if (cont == inv.containerBelt)
                    moved = item.MoveToContainer(inv.containerWear);
                if (cont == inv.containerWear)
                    moved = item.MoveToContainer(inv.containerBelt);
            }

            if (!moved)
                item.Drop(player.GetCenter(),player.GetDropVelocity());
        }
        private Item BuildItem(string ShortName, int Amount, ulong SkinID, float Condition, int blueprintTarget, Weapon weapon, List<ItemContent> Content)
        {
            Item item = ItemManager.CreateByName(ShortName, Amount > 1 ? Amount : 1, SkinID);
            item.condition = Condition;

            if (blueprintTarget != 0)
                item.blueprintTarget = blueprintTarget;

            if (weapon != null)
            {
                (item.GetHeldEntity() as BaseProjectile).primaryMagazine.contents = weapon.ammoAmount;
                (item.GetHeldEntity() as BaseProjectile).primaryMagazine.ammoType = ItemManager.FindItemDefinition(weapon.ammoType);
            }
            if (Content != null)
            {
                foreach (var cont in Content)
                {
                    Item new_cont = ItemManager.CreateByName(cont.ShortName, cont.Amount);
                    new_cont.condition = cont.Condition;
                    new_cont.MoveToContainer(item.contents);
                }
            }
            return item;
        }
        #endregion
        
        private List<Kit> GetKits(BasePlayer player)
        {
            return _kits.Where(kit => kit.Hide == false && (string.IsNullOrEmpty(kit.Permission) || permission.UserHasPermission(player.UserIDString, kit.Permission)) && (kit.Amount == 0 || (kit.Amount > 0 && GetPlayerData(player.userID, kit.Name).Amount < kit.Amount))).ToList();
        }

        #region Интерфейс
        void DrawUI(BasePlayer player, int page = 1)
        {
            CuiHelper.DestroyUi(player, Layer);
            CuiElementContainer container = new CuiElementContainer();
            _kitsGUI[player] = new List<string>();
            var currentTime = GetCurrentTime();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.9" },
            }, "Overlay", Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Color = "1 1 1 0", Command = "kit ui" },
                Text = { Text = "" }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.47 0.447", AnchorMax = "0.53 0.553", OffsetMax = "0 0" },
                Button = { Color = "1 1 1 0", Command = "kit ui" },
                Text = { Text = "✖", Color = "1 1 1 0.8", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 40 }
            }, Layer, "Kit");

            container.Add(new CuiElement
            {
                Parent = "Kit",
                Components = {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "c7ccHY5"), Color = "0.93 0.24 0.38 0.15", FadeIn = 1f},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                }
            });

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.5 0.5", AnchorMax = $"0.5 0.5" },
                Image = { Color = "0 0 0 0" }
            }, Layer, "Center");
            
            for (var i = 0; i < GetKits(player).ToList().Count; i++) 
            {
                var check = GetKits(player).ElementAt(i);
                
                var r = GetKits(player).ToList().Count * 10 + 50;
                var c = (double) GetKits(player).ToList().Count / 2;
                var pos = i / c * Math.PI;    
                var x = r * Math.Sin(pos);
                var y = r * Math.Cos(pos);

                _kitsGUI[player].Add(check.Name);
                var data = GetPlayerData(player.userID, check.Name);

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{x - 35} {y - 35}", AnchorMax = $"{x + 35} {y + 35}" },
                    Image = { Color = "0 0 0 0" }
                }, "Center", $"{check.Name}");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0.1", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Button = { Color = "1 1 1 0" },
                    Text = { Text = $"" }
                }, $"{check.Name}", "Image");

                container.Add(new CuiElement
                {
                    Parent = "Image",
                    Components = {
                         new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", check.Url), FadeIn = 1f},
                         new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "18 16", OffsetMax = "-18 -16" }
                    }
                });

                if (check.Cooldown > 0 && (data.Cooldown > currentTime))
                {

                    CooldownUI(ref container, check.Name, TimeSpan.FromSeconds(data.Cooldown - currentTime));
                }
                else
                {
                    ButtonUI(ref container, check.Name, player);
                }
            }

            CuiHelper.AddUi(player, container);
        }

        void ButtonUI(ref CuiElementContainer container, string name, BasePlayer player)
        {
            var check = _kits.FirstOrDefault(z => z.Name == name);
            container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0", Command = $"kit {name}" },
                RectTransform = { AnchorMin = "0.74 0.77", AnchorMax = "1 1", OffsetMax = "0 0" },
                Text = { Text = $"{check.DisplayName.Remove(1)}", Color = "1 1 1 0.5", Align = TextAnchor.LowerCenter, Font = "robotocondensed-regular.ttf", FontSize = 12 }
            }, $"{name}", $"{name}.button");
            
            container.Add(new CuiElement
            {
                Parent = $"{name}",
                Name = $"{name}.images",
                Components = {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "tF1Max3"), Color = "1 1 1 0.05"},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0", Command = $"kit {name}" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Text = { Text = $"" }
            }, $"{name}");
        }

        private void CooldownUI(ref CuiElementContainer container, string name, TimeSpan time)
        {
            var check = _kits.FirstOrDefault(z => z.Name == name);
            container.Add(new CuiElement
            {
                Parent = $"{name}",
                Name = $"{name}.imagess",
                Components = {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "tF1Max3"), Color = "0.93 0.24 0.38 0.15"},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.74 0.77", AnchorMax = "1 1", OffsetMax = "0 0" },
                Text = { Text = $"{check.DisplayName.Remove(1)}", Color = "1 1 1 0.5", Align = TextAnchor.LowerCenter, Font = "robotocondensed-regular.ttf", FontSize = 12 }
            }, $"{name}", $"{name}.buttons");

            container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0.15", AnchorMax = "1 1", OffsetMax = "0 0" },
                Text = { Text = $"{FormatTime(time)}", Color = "1 1 1 0.5", Align = TextAnchor.LowerCenter, Font = "robotocondensed-regular.ttf", FontSize = 10 }
            }, $"{name}", $"{name}.time");
        }

        private void RefreshCooldownKitsUI()
        {
            var currentTime = GetCurrentTime();
            foreach (var player in _kitsGUI)
            {
                var container = new CuiElementContainer();
                if (!_kitsData.ContainsKey(player.Key.userID)) continue;
                var playerKitsData = _kitsData[player.Key.userID];
                foreach (var name in player.Value)
                {
                    var playerKitData = playerKitsData[name];
                    if (playerKitData.Cooldown > 0)
                    {
                        CuiHelper.DestroyUi(player.Key, $"{name}.time");
                        CuiHelper.DestroyUi(player.Key, $"{name}.button");
                        CuiHelper.DestroyUi(player.Key, $"{name}.imagess");
                        CuiHelper.DestroyUi(player.Key, $"{name}.images");
                        CuiHelper.DestroyUi(player.Key, $"{name}.buttons");
                        CuiHelper.DestroyUi(player.Key, $"{name}.available");
                        if (playerKitData.Cooldown < currentTime)
                        {
                            ButtonUI(ref container, name, player.Key);
                            CuiHelper.DestroyUi(player.Key, $"{name}.button");
                            CuiHelper.DestroyUi(player.Key, $"{name}.Take");
                            CuiHelper.DestroyUi(player.Key, $"{name}.Times");
                            CuiHelper.DestroyUi(player.Key, $"{name}.buttons");
                            CuiHelper.DestroyUi(player.Key, $"{name}.images");
                        }
                        else
                        {
                            CooldownUI(ref container, name, TimeSpan.FromSeconds(playerKitData.Cooldown - currentTime));
                        }
                    }
                }
                CuiHelper.AddUi(player.Key, container);
            }
        }

        #endregion

        #region Хелпер
        private KitData GetPlayerData(ulong userID, string name)
        {
            if (!_kitsData.ContainsKey(userID))
                _kitsData[userID] = new Dictionary<string, KitData>();

            if (!_kitsData[userID].ContainsKey(name))
                _kitsData[userID][name] = new KitData();

            return _kitsData[userID][name];
        }

        private List<KitItem> GetPlayerItems(BasePlayer player)
        {
            List<KitItem> kititems = new List<KitItem>();
            foreach (Item item in player.inventory.containerWear.itemList)
            {
                if (item != null)
                {
                    var iteminfo = ItemToKit(item, "wear");
                    kititems.Add(iteminfo);
                }
            }
            foreach (Item item in player.inventory.containerMain.itemList)
            {
                if (item != null)
                {
                    var iteminfo = ItemToKit(item, "main");
                    kititems.Add(iteminfo);
                }
            }
            foreach (Item item in player.inventory.containerBelt.itemList)
            {
                if (item != null)
                {
                    var iteminfo = ItemToKit(item, "belt");
                    kititems.Add(iteminfo);
                }
            }
            return kititems;
        }

        private KitItem ItemToKit(Item item, string container)
        {
            KitItem kitem = new KitItem();
            kitem.Amount = item.amount;
            kitem.Container = container;
            kitem.SkinID = item.skin;
            kitem.Blueprint = item.blueprintTarget;
            kitem.ShortName = item.info.shortname;
            kitem.Condition = item.condition;
            kitem.Weapon = null;
            kitem.Content = null;
            if (item.info.category == ItemCategory.Weapon)
            {
                BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
                if (weapon != null)
                {
                    kitem.Weapon = new Weapon();
                    kitem.Weapon.ammoType = weapon.primaryMagazine.ammoType.shortname;
                    kitem.Weapon.ammoAmount = weapon.primaryMagazine.contents;
                }
            }
            if (item.contents != null)
            {
                kitem.Content = new List<ItemContent>();
                foreach (var cont in item.contents.itemList)
                {
                    kitem.Content.Add(new ItemContent()
                    {
                        Amount = cont.amount,
                        Condition = cont.condition,
                        ShortName = cont.info.shortname
                    });
                }
            }
            return kitem;
        }

        private List<Kit> GetKitsForPlayer(BasePlayer player)
        {
            return _kits.Where(kit => (string.IsNullOrEmpty(kit.Permission) || permission.UserHasPermission(player.UserIDString, kit.Permission)) && (kit.Amount == 0 || (kit.Amount > 0 && GetPlayerData(player.userID, kit.Name).Amount < kit.Amount))).ToList();
        }

        private BasePlayer FindPlayer(BasePlayer player, string nameOrID)
        {
            ulong id;
            if (ulong.TryParse(nameOrID, out id) && nameOrID.StartsWith("") && nameOrID.Length == 17)
            {
                var findedPlayer = BasePlayer.FindByID(id);
                if (findedPlayer == null || !findedPlayer.IsConnected)
                {
                    SendReply(player, "Игрок не найден");
                    return null;
                }

                return findedPlayer;
            }

            var foundPlayers = BasePlayer.activePlayerList.Where(x => x.displayName.ToLower().Contains(nameOrID.ToLower()));

            if (foundPlayers.Count() == 0)
            {
                SendReply(player, "Игрок не найден");
                return null;
            }

            if (foundPlayers.Count() > 1)
            {
                SendReply(player, "Найдено несколько игроков");
                return null;
            }

            return foundPlayers.First();
        }

        private double GetCurrentTime() => new TimeSpan(DateTime.UtcNow.Ticks).TotalSeconds;

        private string GetFormatTime(double seconds)
        {
            double minutes = Math.Floor((double)(seconds / 60));
            seconds -= (int)(minutes * 60);
            return string.Format("{0}:{1:00}", minutes, seconds);
        }

        public static string FormatTime(TimeSpan time)
        {
            string result = string.Empty;
            if (time.Days != 0) result += $"{Format(time.Days, "", "", "")}:";
            if (time.Hours != 0) result += $"{Format(time.Hours, "", "", "")}:";
            if (time.Minutes != 0) result += $"{Format(time.Minutes, "", "", "")}:";
            if (time.Seconds != 0) result += $"{Format(time.Seconds, "", "", "")} ";
            return result;
        }

        private static string Format(int units, string form1, string form2, string form3)
        {
            var tmp = units % 10;
            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9) return $"{units} {form1}";
            if (tmp >= 2 && tmp <= 4) return $"{units} {form2}";
            return $"{units} {form3}";
        }

        private void DestroyUI(BasePlayer player)
        {
            if (!_kitsGUI.ContainsKey(player))
                return;

            foreach (var name in _kitsGUI[player])
            {
                CuiHelper.DestroyUi(player, $"{name}.time");
                CuiHelper.DestroyUi(player, $"{name}.button");
                CuiHelper.DestroyUi(player, $"{name}.buttons");
                CuiHelper.DestroyUi(player, $"{name}.Times");
                CuiHelper.DestroyUi(player, $"{name}.Block");
                CuiHelper.DestroyUi(player, $"{name}.Blocks");
                CuiHelper.DestroyUi(player, $"{name}.Take");
                CuiHelper.DestroyUi(player, $"{name}");
            }
            CuiHelper.DestroyUi(player, Layer);

            _kitsGUI.Remove(player);
        }
        #endregion
    }
}
                             