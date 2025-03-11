using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Kits", "xkrystalll", "2.0.0")]

    class Kits: RustPlugin
    {
        #region Fields

        private string Layer = "UI.Kits";
        private string LayerBlur = "UI.Kits.Blur";
        private string LayerBlurKitsInfo = "UI.Kits.Blur";
        

        #endregion 

        #region Hooks
        
        object OnPlayerRespawned(BasePlayer player)
        { 
            player.inventory.Strip();
 
            foreach (var kitItem in _config.spawnKit)
            {
                var item = kitItem.ToItem();

                if (kitItem.Container == "wear")
                {
                    item.MoveToContainer(player.inventory.containerWear);
                    
                    continue; 
                }

                if (kitItem.Container == "belt")
                {
                    item.MoveToContainer(player.inventory.containerBelt);
                    continue;
                }

                item.MoveToContainer(player.inventory.containerMain);
            }
            
            return null;
        }

        void Loaded()
        {
            LoadData();
			AddImage("https://static.moscow.ovh/images/games/rust//plugins/ultimate_ui/exit.png", "Kits_img_exit");
        }

        void OnServerInitialized()
        {
            timer.Every(310f, SaveData);
            
            foreach (var kit in _config.kits)
            {
                if (string.IsNullOrEmpty(kit.privilege)) continue;
                
                permission.RegisterPermission(kit.privilege, this);
            }
                        
            SaveConfig();
        }

        private KitsInfo GetKitsInfo(BasePlayer player)
        {
            KitsInfo result;
            if (!storedData.players.TryGetValue(player.userID, out result))
            {
                result = storedData.players[player.userID] = new KitsInfo();
            }

            return result;
        }

        private KitData GetKitData(KitsInfo kitsInfo, KitInfo kitInfo)
        {
            KitData result;
            if (!kitsInfo.kits.TryGetValue(kitInfo.kitName, out result))
            {
                result = kitsInfo.kits[kitInfo.kitName] = new KitData()
                {
                    amount = kitInfo.maxUse,
                    cooldown = 0
                };
            }

            return result;
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
                CuiHelper.DestroyUi(player, LayerBlur);
            }
            
            SaveData();
        }

        #endregion

        #region Commands
        
        [ChatCommand("addkit")]
        private void CreateKit(BasePlayer player, string command, string[] args)
        {
            if (player.Connection.authLevel < 2) return;
            
            if (_config.kits.Exists(x => x.kitName == args[0]))
            {
                SendReply(player, "Название кита уже существует!");
                return;
            }

            _config.kits.Add(new KitInfo()
            {
                kitName = args[0],
                cooldownKit = 3600,
                items = GetPlayerItems(player),
                maxUse = -1,
                privilege = "kits.new"
            });
            
            permission.RegisterPermission($"kits.new", this); 
            SendReply(player, $"Вы создали кит с именем {args[0]}");
            SaveConfig();
        }

        [ConsoleCommand("UI_KITS")]
        private void cmdConsoleHandler(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();

            if (!arg.HasArgs(1)) return;

            var cmd = arg.GetString(0);

            var kits = GetKitsForPlayer(player);
            var kitsInfo = GetKitsInfo(player);
            
            switch (cmd)
            {
                case "prev":
                {
                    player.SendConsoleCommand("UI_KITS showavailablekits");
                    break;
                }
                case "close":
                {
                    CuiHelper.DestroyUi(player, Layer);
                    CuiHelper.DestroyUi(player, LayerBlur);
                    break;
                }
                case "showinfokit":
                {
                    var targetNameKit = arg.GetString(1);
                    if (arg.HasArgs(3)) targetNameKit += $" {arg.GetString(2)}";
                    var kitInfo = _config.kits.FirstOrDefault(x => x.kitName.Equals(targetNameKit));
                    if (kitInfo == null) return;

                    CuiHelper.DestroyUi(player, Layer);

                    var container = new CuiElementContainer();
                    
                    container.Add(new CuiPanel
                    {
                        CursorEnabled = true,
                        Image =
                    {
                        FadeIn = 0.2f,
                        Sprite = "assets/content/ui/ui.background.transparent.radial.psd",
                        Color = "0 0 0 1"
                    }
                    }, "Overlay", Layer);
                    container.Add(new CuiPanel
                    {
                    Image =
                    {
                        FadeIn = 0.2f,
                        Color = "0.2 0.2 0.17 0.7",
                        Material = "assets/content/ui/uibackgroundblur.mat"
                    }
                    }, Layer);
                    
                    container.Add(new CuiLabel
                    {
                        Text = { Text = targetNameKit.ToUpper(), Align = TextAnchor.UpperCenter, FontSize = 40, Font = "robotocondensed-bold.ttf" },
                        RectTransform = { AnchorMin = "0.3 1", AnchorMax = "0.7 1", OffsetMin = "0 -155", OffsetMax = "0 -91.6" }
                    }, Layer);
                    container.Add(new CuiLabel
                    {
                        Text = { Text = GetMsg("kit.contains.label", player.userID), Align = TextAnchor.UpperCenter, FontSize = 18, Font = "robotocondensed-regular.ttf" },
                        RectTransform = { AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 -155", OffsetMax = "0 -133" }
                    }, Layer);

                    container.Add(new CuiElement
                    {
                        Parent = Layer,
                        Components =
                        {
                            GetImageComponent("https://static.moscow.ovh/images/games/rust//plugins/ultimate_ui/exit.png", "Kits_img_exit"),
                            new CuiRectTransformComponent {AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-73.9 20", OffsetMax = "-28.6 80"},
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = Layer,
                        Components =
                        {
                            new CuiImageComponent {Color = "0.33 0.87 0.59 0.6"},
                            new CuiRectTransformComponent {AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-291.3 22.6", OffsetMax = "-108 25.2"}
                        }
                    });
                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Color = "0 0 0 0",
                            Command = "UI_KITS prev",
                            Close = Layer
                        },
                        Text = { Text = GetMsg("ui.back", player.userID), Align = TextAnchor.UpperCenter, FontSize = 18 },
                        RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-291.3 22.6", OffsetMax = "-108 49.2" },
                    }, Layer);
                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Color = "0 0 0 0",
                            Command = "UI_KITS prev",
                            Close = Layer
                        },
                        Text = { Text = "" },
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    }, Layer);

                    var itemSize = 65.3f;
                    var itemSep = 6.6f;
                    var num = Mathf.Min(6, kitInfo.items.Count);
                    var posX = -(itemSize * num + itemSep * (num - 1)) / 2f;
                    var posY = 0f;
                    
                    for (var i = 0; i < kitInfo.items.Count;)
                    {
                        var item = kitInfo.items[i];
                        container.Add(new CuiPanel
                        {
                            RectTransform =
                            {
                                AnchorMin = "0.5 0.65", AnchorMax = "0.5 0.65", OffsetMin = $"{posX} {posY - itemSize}", OffsetMax = $"{posX + itemSize} {posY}"
                            },
                            Image = { Color = "0 0 0 0.6" }
                        }, Layer, Layer + $".Item{i}");
                        
                        container.Add(new CuiElement
                        {
                            Parent = Layer + $".Item{i}",
                            Components =
                            {
                                GetItemImageComponent(ItemManager.FindItemDefinition(item.ID).shortname),
                                new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" },
                            }
                        });

                        if (item.Amount > 1)
                        {
                            container.Add(new CuiLabel()
                            {
                                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "-3 -3" },
                                Text = { Text = $"x{item.Amount}", Font = "RobotoCondensed-Bold.ttf", Align = TextAnchor.LowerRight, FontSize = 14 }
                            }, Layer + $".Item{i}");
                        }

                        if (++i % 6 == 0)
                        {
                            posY -= itemSize + itemSep;
                            num = Mathf.Min(6, kitInfo.items.Count - i);
                            posX = -(itemSize * num + itemSep * (num - 1)) / 2f;
                        }
                        else posX += itemSize + itemSep;
                    }
                    CuiHelper.AddUi(player, container);

                    break;
                }
                case "showavailablekits":
                {
                    CuiHelper.DestroyUi(player, Layer);

                    var container = new CuiElementContainer();

                    container.Add(new CuiPanel
                    {
                        CursorEnabled = true,
                        Image =
                        {
                            FadeIn = 0.2f,
                            Sprite = "assets/content/ui/ui.background.transparent.radial.psd",
                            Color = "0 0 0 1"
                        }
                    }, "Overlay", Layer);
                    container.Add(new CuiPanel
                    {
                        Image =
                        {
                            FadeIn = 0.2f,
                            Color = "0.2 0.2 0.17 0.7",
                            Material = "assets/content/ui/uibackgroundblur.mat"
                        }
                    }, Layer);

                    container.Add(new CuiLabel
                    {
                        Text = { Text = GetMsg("SERVER_NAME", player.userID), Align = TextAnchor.UpperCenter, FontSize = 40, Font = "robotocondensed-bold.ttf" },
                        RectTransform = { AnchorMin = "0.3 1", AnchorMax = "0.7 1", OffsetMin = "0 -155", OffsetMax = "0 -91.6" }
                    }, Layer);
                    container.Add(new CuiLabel
                    {
                        Text = { Text = GetMsg("ui.warning.clearInventory", player.userID), Align = TextAnchor.UpperCenter, FontSize = 16, Font = "robotocondensed-regular.ttf" },
                        RectTransform = { AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 -155", OffsetMax = "0 -133" }
                    }, Layer);

                    container.Add(new CuiElement
                    {
                        Parent = Layer,
                        Components =
                        {
                            GetImageComponent("https://static.moscow.ovh/images/games/rust//plugins/ultimate_ui/exit.png", "Kits_img_exit"),
                            new CuiRectTransformComponent {AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-73.9 20", OffsetMax = "-28.6 80"},
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = Layer,
                        Components =
                        {
                            new CuiImageComponent {Color = "0.33 0.87 0.59 0.6"},
                            new CuiRectTransformComponent {AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-291.3 22.6", OffsetMax = "-108 25.2"}
                        }
                    });
                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Color = "0 0 0 0",
                            Command = "UI_KITS close",
                            Close = Layer
                        },
                        Text = { Text = GetMsg("ui.exit", player.userID), Align = TextAnchor.UpperCenter, FontSize = 22 },
                        RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-291.3 22.6", OffsetMax = "-108 49.2" },
                    }, Layer);
                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Color = "0 0 0 0",
                            Command = "UI_KITS close",
                            Close = Layer
                        },
                        Text = { Text = "" },
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    }, Layer);

                        var kitSizeX = 183.3f;
                    var kitSizeY = 46.6f;
                    var kitSepX = 13.3f;
                    var kitSepY = 45f;
                    var num = Mathf.Min(5, kits.Count);
                    var posX = -(kitSizeX * num + kitSepX * (num - 1)) / 2f;
                    var posY = 0f;
                    
                    for (var i = 0; i < kits.Count;)
                    {
                        var kit = kits.ElementAt(i);
                        var dataPlayer = GetKitData(kitsInfo, kit);
                        var time = dataPlayer.cooldown - TimeHelper.GetTimeStamp();

                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = "0.5 0.62", AnchorMax = "0.5 0.66", OffsetMin = $"{posX} {posY - kitSizeY}", OffsetMax = $"{posX + kitSizeX} {posY}"},
                            Text =
                            {
                                Align = TextAnchor.MiddleCenter,
                                FontSize = 20,
                                Text = kit.kitName
                            },
                            Button =
                            {
                                Color = "0 0 0 0.5",
                                Command = $"UI_KITS givekit {kit.kitName} {i}"
                            }
                        }, Layer, Layer + $".Kits{i}");
                        
                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = "0.087 0.95", AnchorMax = "1 1.01", OffsetMin = "-16 -16", OffsetMax = "0 0" },
                            Text =
                            {
                                Text = GetMsg("kit.about.text", player.userID),
                                FontSize = 12,
                                Align = TextAnchor.MiddleCenter
                            },
                            Button =
                            {
                                Color = "0 0 0.3 0.4",
                                Command = $"UI_KITS showinfokit {kit.kitName}"
                            }
                        }, Layer + $".Kits{i}");

                        if (time < 0)
                        {
                            container.Add(new CuiPanel
                            {
                                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.02", OffsetMin = "0 0", OffsetMax = "0 4.6" },
                                Image = { Color = "0.33 0.87 0.59 0.6" }
                            }, Layer + $".Kits{i}", Layer + $".Kits{i}.Status");
                        }
                        else
                        {
                            container.Add(new CuiLabel
                            {
                                Text =
                                {
                                    Align = TextAnchor.LowerCenter,
                                    FontSize = 13,
                                    Font = "RobotoCondensed-Regular.ttf",
                                    Text = TimeHelper.FormatTime(TimeSpan.FromSeconds(time), 2)
                                },
                                RectTransform = { AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 2", OffsetMax = $"0 {kitSepY}" }
                            }, Layer + $".Kits{i}", Layer + $".Kits{i}.Status.Text");
                                container.Add(new CuiPanel
                            {
                                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.02", OffsetMin = "0 0", OffsetMax = "0 4.6" },
                                Image = { Color = "0.87 0.33 0.33 0.5" }
                            }, Layer + $".Kits{i}", Layer + $".Kits{i}.Status");
                        }
                        if (++i % 5 == 0)
                        {
                            posY -= kitSizeY + kitSepY;
                            num = Mathf.Min(5, kits.Count - i);
                            posX = -(kitSizeX * num + kitSepX * (num - 1)) / 2f;
                        }
                        else posX += kitSizeX + kitSepX;
                    }

                    CuiHelper.AddUi(player, container);

                    break;
                }
                case "givekit":
                {
                    var nameKit = arg.GetString(1, "text");

                    int idKit;

                    if (arg.HasArgs(4))
                    {
                        idKit = arg.GetInt(3);
                        nameKit += " " + arg.GetString(2);
                    }
                    else idKit = arg.GetInt(2);

                    var kitInfo1 = _config.kits.Find(kit => kit.kitName == nameKit);
                    if (kitInfo1 == null) return;

                    var playerData = GetKitData(kitsInfo, kitInfo1);

                    var kitData = _config.kits.First(x => x.kitName == nameKit);
                    if (playerData != null)
                    {
                        if (playerData.cooldown > TimeHelper.GetTimeStamp()) return;

                        if (playerData.amount != -1)
                        {
                            if (playerData.amount == 0) return;
                        }

                            int beltcount = kitData.items.Where(i => i.Container == "belt").Count();

                            int wearcount = kitData.items.Where(i => i.Container == "wear").Count();

                            int maincount = kitData.items.Where(i => i.Container == "main").Count();



                            int totalcount = beltcount + wearcount + maincount;
                            if ((player.inventory.containerBelt.capacity - player.inventory.containerBelt.itemList.Count) < beltcount || (player.inventory.containerWear.capacity - player.inventory.containerWear.itemList.Count) < wearcount || (player.inventory.containerMain.capacity - player.inventory.containerMain.itemList.Count) < maincount) if (totalcount > (player.inventory.containerMain.capacity - player.inventory.containerMain.itemList.Count))
                                {

                                    player.ShowToast(1, "Нет места в инвентаре");
                                    return;
                                }

                            GiveItems(player, kitData);
                        playerData.cooldown = TimeHelper.GetTimeStamp() + kitData.cooldownKit;

                        CuiHelper.DestroyUi(player, Layer + $".Kits{idKit}.Status.Text");
                        CuiHelper.DestroyUi(player, Layer + $".Kits{idKit}.Status");
                        CuiHelper.DestroyUi(player, Layer + "Status");

                        var container = new CuiElementContainer();

                        container.Add(new CuiLabel
                        {
                            Text =
                        {
                            Align = TextAnchor.LowerCenter,
                            FontSize = 13,
                            Font = "RobotoCondensed-Regular.ttf",
                            Text = TimeHelper.FormatTime(TimeSpan.FromSeconds(playerData.cooldown - TimeHelper.GetTimeStamp()))
                        },
                            RectTransform = { AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 2", OffsetMax = $"0 24" }
                        }, Layer + $".Kits{idKit}", Layer + $".Kits{idKit}.Status.Text");
                        container.Add(new CuiPanel
                        {
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 0", OffsetMax = "0 4.6" },
                            Image = { Color = "0.87 0.33 0.33 0.5" }
                        }, Layer + $".Kits{idKit}", Layer + $".Kits{idKit}.Status");

                        container.Add(new CuiLabel
                        {
                            Text = { Text = GetMsg("kit.gived", player.userID), Align = TextAnchor.LowerCenter, FontSize = 16, Font = "robotocondensed-bold.ttf" },
                            RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-250 104", OffsetMax = "250 130" }
                        }, Layer, Layer + "Status");
                        CuiHelper.AddUi(player, container);

                        CuiHelper.AddUi(player, container);

                        if (kitData.maxUse != -1) playerData.amount -= 1;
                    }

                    break;
                }
            }
        }

        [ChatCommand("kit")] 
        void KitOpen(BasePlayer player, string command, string[] args)
        { 
            var ret = Interface.Call("CanRedeemKit", player) as string;
             
            if (ret != null)
            {
                SendReply(player, ret);
                return;
            }
            
            player.SendConsoleCommand("UI_KITS showavailablekits");
        }
        
        [ChatCommand("kits")] 
        void KitsOpen(BasePlayer player, string command, string[] args)
        {
            var ret = Interface.Call("CanRedeemKit", player) as string;
            
            if (ret != null)
            {
                SendReply(player, ret);
                return;
            }
            
            player.SendConsoleCommand("UI_KITS showavailablekits");
        }

        #endregion
        
        #region Methods
        
        private List<KitInfo> GetKitsForPlayer(BasePlayer player)
        {
            var kitsInfo = GetKitsInfo(player);
            return _config.kits.Where(kit => (string.IsNullOrEmpty(kit.privilege) || permission.UserHasPermission(player.UserIDString, kit.privilege)) && GetKitData(kitsInfo, kit).amount != 0).ToList(); 
        }
         
        private void GiveItems(BasePlayer player, KitInfo kit)
        {
            foreach(var kitItem in kit.items)
            {             
                var item = kitItem.ToItem();
                GiveItem(player, item, kitItem.Position, kitItem.Container == "belt" ? player.inventory.containerBelt : kitItem.Container == "wear" ? player.inventory.containerWear : player.inventory.containerMain);
            }
        }
        
        private void GiveItem(BasePlayer player, Item item, int position, ItemContainer cont = null)
        {
            if (item == null) return;
            
            if (cont.IsFull())
                player.GiveItem(item);
            if (cont.itemList.Any(x => x.position == position))
                item.MoveToContainer(cont);
            else
                item.MoveToContainer(cont, position);
            
        }
        
        private List<ItemData> GetPlayerItems(BasePlayer player)
        {
            List<ItemData> kititems = new List<ItemData>();
            foreach (Item item in player.inventory.containerWear.itemList)
            {
                if (item != null)
                {
                    var ItemData = ItemToKit(item, "wear");
                    kititems.Add(ItemData);
                }
            }
            foreach (Item item in player.inventory.containerMain.itemList)
            {
                if (item != null)
                {
                    var ItemData = ItemToKit(item, "main");
                    kititems.Add(ItemData);
                }
            }
            foreach (Item item in player.inventory.containerBelt.itemList)
            {
                if (item != null)
                {
                    var ItemData = ItemToKit(item, "belt");
                    kititems.Add(ItemData);
                }
            }
            return kititems;
        }
        
        private ItemData ItemToKit(Item item, string container)
        {
            return ItemData.FromItem(item, container);
        }
        
        private static string HexToRGB(string hex)
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

        #endregion 

        #region Config

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();
        }

        protected override void LoadDefaultConfig()
        {
            Item rock = ItemManager.CreateByName("rock", 1, 0);
            Item torch = ItemManager.CreateByName("torch", 1, 0);
            _config = new Configuration()
            {
                spawnKit = new List<ItemData>()
                {
                    ItemData.FromItem(rock, "belt"),
                    ItemData.FromItem(torch, "belt"),
                }
            };
        }
        
        public Configuration _config;

        public class Configuration
        {
            [JsonProperty("Набор на респавне")] public List<ItemData> spawnKit = new List<ItemData>();
            [JsonProperty("Наборы")] public List<KitInfo> kits = new List<KitInfo>();
        }
        
        #endregion

        #region Class

        public class KitInfo
        {
            [JsonProperty("Название")] public string kitName = "";
            [JsonProperty("Максимум использований")] public int maxUse = 0;
            [JsonProperty("Кулдаун")] public double cooldownKit = 0;
            [JsonProperty("Привилегия")] public string privilege = "";
            [JsonProperty("Предметы")] public List<ItemData> items = new List<ItemData>();
        }

        public class ItemData
        {
            public int ID;
            public int Position = -1;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public int Amount;
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string Container;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool IsBlueprint;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public int BlueprintTarget;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public ulong Skin;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public float Fuel;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public int FlameFuel;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public float Condition;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public float MaxCondition = -1;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public int Ammo;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public int AmmoType;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public int DataInt;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string Name;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string Text;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public uint AssociatedEntityId;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public List<ItemData> Contents = new List<ItemData>();

            public Item ToItem()
            {
                if (Amount == 0)
                    return null;

                Item item = ItemManager.CreateByItemID(ID, Amount, Skin);
                if (item == null)
                    return null;

                item.position = Position;

                if (IsBlueprint)
                {
                    item.blueprintTarget = BlueprintTarget;
                    return item;
                }

                item.fuel = Fuel;
                item.condition = Condition;

                if (MaxCondition != -1)
                    item.maxCondition = MaxCondition;

                if (Contents != null)
                {
                    if (Contents.Count > 0)
                    {
                        if (item.contents == null)
                        {
                            item.contents = new ItemContainer();
                            item.contents.ServerInitialize(null, Contents.Count);
                            item.contents.GiveUID();
                            item.contents.parent = item;
                        }
                        foreach (var contentItem in Contents)
                            contentItem.ToItem()?.MoveToContainer(item.contents);
                    }
                }
                else
                    item.contents = null;

                BaseProjectile.Magazine magazine = item.GetHeldEntity()?.GetComponent<BaseProjectile>()?.primaryMagazine;
                FlameThrower flameThrower = item.GetHeldEntity()?.GetComponent<FlameThrower>();

                if (magazine != null)
                {
                    magazine.contents = Ammo;
                    magazine.ammoType = ItemManager.FindItemDefinition(AmmoType);
                }

                if (flameThrower != null)
                    flameThrower.ammo = FlameFuel;

                if (DataInt > 0 || AssociatedEntityId != 0)
                {
                    item.instanceData = new ProtoBuf.Item.InstanceData
                    {
                        ShouldPool = false,
                        dataInt = DataInt,
                    };

                    if (AssociatedEntityId != 0)
                    {
                        var associatedEntity = BaseNetworkable.serverEntities.Find(AssociatedEntityId);
                        if (associatedEntity != null)
                        {
                            associatedEntity._limitedNetworking = false;

                            item.instanceData.subEntity = AssociatedEntityId;
                        }
                    }
                }

                item.text = Text;

                if (Name != null)
                    item.name = Name;

                return item;
            }

            public static ItemData FromItem(Item item, string container) => new ItemData
            {
                ID = item.info.itemid,
                Position = item.position,
                Container = container,
                Ammo = item.GetHeldEntity()?.GetComponent<BaseProjectile>()?.primaryMagazine?.contents ?? 0,
                AmmoType = item.GetHeldEntity()?.GetComponent<BaseProjectile>()?.primaryMagazine?.ammoType?.itemid ?? 0,
                Amount = item.amount,
                Condition = item.condition,
                MaxCondition = item.maxCondition,
                Fuel = item.fuel,
                Skin = item.skin,
                Contents = item.contents?.itemList?.Select(x => FromItem(x, container)).ToList(),
                FlameFuel = item.GetHeldEntity()?.GetComponent<FlameThrower>()?.ammo ?? 0,
                IsBlueprint = item.IsBlueprint(),
                BlueprintTarget = item.blueprintTarget,
                DataInt = item.instanceData?.dataInt ?? 0,
                AssociatedEntityId = item.instanceData?.subEntity ?? 0,
                Name = string.IsNullOrEmpty(item.name) ? item.info.displayName.translated : item.name,
                Text = item.text
            };
        }
        
        // public class ItemData
        // {
        //     [JsonProperty("Позиция")] public int position = 0;
        //     [JsonProperty("Shortname")] public string shortname = "";
        //     [JsonProperty("Количество")] public int amount = 0;
        //     [JsonProperty("Место")] public string place = "main";
        //     [JsonProperty("Скин")] public ulong skinID = 0U;
        //     [JsonProperty("Контейнер")] public List<ItemContent> Content { get; set; }
        //     [JsonProperty("Прочность")] public float Condition { get; set; }
        //     [JsonProperty("Оружие")] public Weapon Weapon { get; set; }
        // }
        
        // public class Weapon
        // {
        //     public string ammoType { get; set; }
        //     public int ammoAmount { get; set; }
        // }
        // public class ItemContent
        // {
        //     public string ShortName { get; set; }
        //     public float Condition { get; set; }
        //     public int Amount { get; set; }
        // }

        #endregion
        
        #region Data

        class StoredData
        {
            public Dictionary<ulong, KitsInfo> players = new Dictionary<ulong, KitsInfo>();
        }

        class KitsInfo
        {
            public Dictionary<string, KitData> kits = new Dictionary<string, KitData>();
        }

        class KitData
        {
            public int amount = 0;
            public double cooldown = 0;
        }
        
        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("Kits/kits", storedData);
        }

        void LoadData()
        {
            try
            {
                storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("Kits/kits");
            }
            catch (Exception ex)
            {
                PrintError($"Failed to load data: {ex}");
            }
            
            if (storedData == null)
                storedData = new StoredData();
        }

        StoredData storedData;
        
        #endregion

        #region Helper

        private static class TimeHelper
        {
            public static string FormatTime(TimeSpan time, int maxSubstr = 5, string language = "ru")
            {
                string result = string.Empty;
                switch (language)
                {
                    case "ru":
                        int i = 0;
                        if (time.Days != 0 && i < maxSubstr)
                        {
                            if (!string.IsNullOrEmpty(result))
                                result += " ";

                            result += $"{Format(time.Days, "д", "д", "д")}";
                            i++;
                        }

                        if (time.Hours != 0 && i < maxSubstr)
                        {
                            if (!string.IsNullOrEmpty(result))
                                result += " ";

                            result += $"{Format(time.Hours, "ч", "ч", "ч")}";
                            i++;
                        }

                        if (time.Minutes != 0 && i < maxSubstr)
                        {
                            if (!string.IsNullOrEmpty(result))
                                result += " ";

                            result += $"{Format(time.Minutes, "м", "м", "м")}";
                            i++;
                        }

                        
                        
                        if (time.Days == 0)
                        {
                            if (time.Seconds != 0 && i < maxSubstr)
                            {
                                if (!string.IsNullOrEmpty(result))
                                    result += " ";

                                result += $"{Format(time.Seconds, "с", "с", "с")}";
                                i++;
                            }
                        }

                        break;
                    case "en":
                        result = string.Format("{0}{1}{2}{3}",
                            time.Duration().Days > 0
                                ? $"{time.Days:0} day{(time.Days == 1 ? String.Empty : "s")}, "
                                : string.Empty,
                            time.Duration().Hours > 0
                                ? $"{time.Hours:0} hour{(time.Hours == 1 ? String.Empty : "s")}, "
                                : string.Empty,
                            time.Duration().Minutes > 0
                                ? $"{time.Minutes:0} minute{(time.Minutes == 1 ? String.Empty : "s")}, "
                                : string.Empty,
                            time.Duration().Seconds > 0
                                ? $"{time.Seconds:0} second{(time.Seconds == 1 ? String.Empty : "s")}"
                                : string.Empty);

                        if (result.EndsWith(", ")) result = result.Substring(0, result.Length - 2);

                        if (string.IsNullOrEmpty(result)) result = "0 seconds";
                        break;
                }

                return result;
            }

            private static string Format(int units, string form1, string form2, string form3)
            {
                var tmp = units % 10;

                if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                    return $"{units}{form1}";

                if (tmp >= 2 && tmp <= 4)
                    return $"{units}{form2}";

                return $"{units}{form3}";
            }

            private static DateTime Epoch = new DateTime(1970, 1, 1);

            public static double GetTimeStamp()
            {
                return DateTime.Now.Subtract(Epoch).TotalSeconds;
            }
        }

        #endregion
		
		
		public CuiRawImageComponent GetAvatarImageComponent(ulong user_id, string color = "1.0 1.0 1.0 1.0"){
			
			if (plugins.Find("ImageLoader")) return plugins.Find("ImageLoader").Call("BuildAvatarImageComponent",user_id) as CuiRawImageComponent;
			if (plugins.Find("ImageLibrary")) {
				return new CuiRawImageComponent { Png = (string)plugins.Find("ImageLibrary").Call("GetImage", user_id.ToString()), Color = color, Sprite = "assets/content/textures/generic/fulltransparent.tga" };
			}
			return new CuiRawImageComponent {Url = "https://image.flaticon.com/icons/png/512/37/37943.png", Color = color, Sprite = "assets/content/textures/generic/fulltransparent.tga"};
		}
		public CuiRawImageComponent GetImageComponent(string url, string shortName="", string color = "1.0 1.0 1.0 1.0"){
			
			if (plugins.Find("ImageLoader")) return plugins.Find("ImageLoader").Call("BuildImageComponent",url) as CuiRawImageComponent;
			if (plugins.Find("ImageLibrary")) {
				if (!string.IsNullOrEmpty(shortName)) url = shortName;
				//Puts($"{url}: "+ (string)plugins.Find("ImageLibrary").Call("GetImage", url));
				return new CuiRawImageComponent { Png = (string)plugins.Find("ImageLibrary").Call("GetImage", url), Color = color, Sprite = "assets/content/textures/generic/fulltransparent.tga"};
			}
			return new CuiRawImageComponent {Url = url, Color = color, Sprite = "assets/content/textures/generic/fulltransparent.tga"};
		}
		public CuiRawImageComponent GetItemImageComponent(string shortName){
			string itemUrl = shortName;
			if (plugins.Find("ImageLoader")) {itemUrl = $"https://static.moscow.ovh/images/games/rust/icons/{shortName}.png";}
            return GetImageComponent(itemUrl, shortName);
		}
		public bool AddImage(string url,string shortName=""){
			if (plugins.Find("ImageLoader")){				
				plugins.Find("ImageLoader").Call("CheckCachedOrCache", url);
				return true;
			}else
			if (plugins.Find("ImageLibrary")){
				if (string.IsNullOrEmpty(shortName)) shortName=url;
				plugins.Find("ImageLibrary").Call("AddImage", url, shortName);
				//Puts($"Add Image {shortName}");
				return true;
			}	
			return false;		
		}
		#region Lang
        private void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"kit.gived", "Набор выдан, если инвентарь переполнен то предметы выпадут на пол."},
                {"kit.about.text", "<color=#FFFFFF>Посмотреть содержимое</color>"},
                {"ui.exit", "Выход"},
                {"ui.warning.clearInventory", "Освободите инвентарь перед получением!"},
                {"SERVER_NAME", "Wounded Rust"},
                {"ui.back", "Вернуться назад"},
                {"kit.contains.label", "Данный набор содержит следующие предметы:"},
            }, this, "ru");
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"kit.gived", "The kit was gived, if the inventory is full, then the items will fall to the floor."},
                {"kit.about.text", "<color=#FFFFFF>Посмотреть содержимое</color>"},
                {"ui.exit", "Exit"},
                {"ui.warning.clearInventory", "Free up inventory before receiving!"},
                {"SERVER_NAME", "Wounded Rust"},
                {"ui.back", "Back"},
                {"kit.contains.label", "This kit contains the following items:"},
            }, this);


        }
        string GetMsg(string key, ulong id) => lang.GetMessage(key, this, id.ToString());
        #endregion
    }
}