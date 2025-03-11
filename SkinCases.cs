using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using WebSocketSharp;

namespace Oxide.Plugins
{
    [Info("SkinCases", "Houga", "0.0.8")]
    [Description("Реализует систему скинов и кейсов")]
    public class SkinCases : RustPlugin
    { 
        #region Classes
        
        private class Rarity 
        {
            [JsonProperty("Шанс выпадения предмета данной редкости")]
            public int Chance;
            [JsonProperty("Цвет этой редкости в интерфейсе")]
            public string Color;

            public Rarity(int chance, string color)
            {
                Chance = chance;
                Color  = color;
            }
        }

        private class Configuration
        {
            public class Cases
            {
                [JsonProperty("Список доступных для открытия кейсов")]
                public List<Case> CaseLits = new List<Case>();
            }

            public class Chances
            {
                [JsonProperty("Настройка реального шанса выпадения")]
                public List<Rarity> Rarities = new List<Rarity>();
                [JsonProperty("Настройка визуального шанса выпадения")]
                public List<Rarity> FakeRarities = new List<Rarity>();
            }

            public class Drops
            {
                public class Drop
                {
                    [JsonProperty("Список выпадающих кейсов (название -> кол-во)")]
                    public Dictionary<string, int> DropCases = new Dictionary<string, int>(); 
                    [JsonProperty("Интервал получения выбранных кейсов в секундах реального времени")]
                    public int Interval;

                    [JsonProperty("Название привилегии, игроки с которой будут получать наборы")]
                    public string Permission;

                    [JsonProperty("Лимит кол-ва не открытых кейсов этого типа для выдачи бесплатного")]
                    public int MaxCaseAmount;
                    [JsonProperty("Время следующего выпадения (не меняйте, и следите чтобы не было равно 0)")]
                    public double NextDropTime; 
                }

                [JsonProperty("Список выпадающих предметов")] 
                public List<Drop> DropList = new List<Drop>();
            }

            public class Others
            {
                [JsonProperty("Разрешить открывать несколько кейсов сразу (если да, визуальное наслаивание интерфейсов, но не критично)")]
                public bool AllowOpenFewCase = false;
            }

            [JsonProperty("Настройки кейсов")] 
            public Cases CaseSettings;
            [JsonProperty("Настройки шансов выпадения предметов")]
            public Chances ChancesSettings;
            [JsonProperty("Настройки автоматической выдачи кейсов")]
            public Drops DropSettrings;
            [JsonProperty("Остальные настройки плагина")]
            public Others OtherSettings;
            

            public static Configuration Generate()
            {
                return new Configuration
                {
                    CaseSettings = new Cases
                    {
                        CaseLits = new List<Case>
                        {
                            new Case("Бесплатный кейс", new List<CaseItem>
                            {
                                new CaseItem("wood",            2000, 4000, 0),
                                new CaseItem("stones",          2000, 4000, 0),
                                new CaseItem("leather",         500,  1000, 0),
                                new CaseItem("cloth",           500,  1000, 0),
                                new CaseItem("lowgradefuel",    150,  500,  1),
                                new CaseItem("metal.fragments", 1000, 2000, 1),
                                new CaseItem("metal.refined",   50,   150,  1),
                                new CaseItem("sulfur",          500,  2000, 2),
                                new CaseItem("gunpowder",       500,  1000, 2),
                                new CaseItem("explosive.timed", 2,    4,    3),
                            }, "https://i.imgur.com/ZMHNQEd.png"),
                            new Case("Компонентный кейс", new List<CaseItem>
                            {
                                new CaseItem("metalblade",  30, 40, 0),
                                new CaseItem("sewingkit",   20, 30, 0),
                                new CaseItem("roadsigns",   10, 15, 0),
                                new CaseItem("metalpipe",   10, 20, 0),
                                new CaseItem("gears",       15, 25, 1),
                                new CaseItem("smgbody",     2,  8,  1),
                                new CaseItem("metalspring", 15, 25, 1),
                                new CaseItem("semibody",    1,  6,  2),
                                new CaseItem("techparts",   10, 15, 2),
                                new CaseItem("riflebody",   10, 15, 3),
                            }, "https://i.imgur.com/2jfUOa8.png"),
                            new Case("Ресурсный кейс", new List<CaseItem>
                            {
                                new CaseItem("wood",            10000, 20000, 0),
                                new CaseItem("stones",          10000, 20000, 0),
                                new CaseItem("leather",         1000,  1500,  0),
                                new CaseItem("cloth",           500,   1000,  0),
                                new CaseItem("lowgradefuel",    450,   900,   1),
                                new CaseItem("metal.fragments", 10000, 15000, 1),
                                new CaseItem("metal.refined",   400,   800,   1),
                                new CaseItem("sulfur",          3000,  6000,  2),
                                new CaseItem("gunpowder",       1500,  3000,  2),
                                new CaseItem("explosive.timed", 5,     10,    3),
                            }, "https://i.imgur.com/S2el4AY.png"),
                            new Case("Оружейный кейс", new List<CaseItem>
                            {
                                new CaseItem("pistol.semiauto", 1, 1, 0),
                                new CaseItem("pistol.python",   1, 1, 0),
                                new CaseItem("pistol.m92",      1, 1, 1),
                                new CaseItem("smg.2",           1, 1, 1),
                                new CaseItem("rifle.m39",       1, 1, 1),
                                new CaseItem("smg.thompson",    1, 1, 2),
                                new CaseItem("rifle.semiauto",  1, 1, 2),
                                new CaseItem("rifle.lr300",     1, 1, 2),
                                new CaseItem("rifle.bolt",      1, 1, 3),
                                new CaseItem("rifle.ak",        1, 1, 3),
                            }, "https://i.imgur.com/Eobztbd.png"),
                            new Case("Пуся кейс", new List<CaseItem>
                            {
                                new CaseItem("explosive.timed",      2,    10,   3),
                                new CaseItem("ammo.rocket.basic",    4,    12,   3),
                                new CaseItem("lmg.m249",             1,    1,    3),
                                new CaseItem("gunpowder",            2000, 8000, 3),
                                new CaseItem("rifle.l96",            1,    1,    3),
                                new CaseItem("supply.signal",        3,    6,    3),
                                new CaseItem("explosive.satchel",    10,   25,   3),
                                new CaseItem("ammo.rifle.explosive", 100,  500,  3),
                                new CaseItem("grenade.f1",           2,    5,    3, 1630356878),
                                new CaseItem("fuse",                 1,    1,    3, 1627796062),
                            }, "https://i.imgur.com/48tLKlE.png"),
                            new Case("Взрывной кейс", new List<CaseItem>
                            {
                                new CaseItem("grenade.beancan",      1,    1,    0),
                                new CaseItem("explosive.timed",      4,    10,   3),
                                new CaseItem("ammo.rocket.basic",    2,    12,   3),
                                new CaseItem("lmg.m249",             1,    1,    1),
                                new CaseItem("gunpowder",            2000, 8000, 2),
                                new CaseItem("rifle.l96",            1,    1,    1),
                                new CaseItem("supply.signal",        3,    6,    1),
                                new CaseItem("explosive.satchel",    10,   25,   2),
                                new CaseItem("ammo.rifle.explosive", 300,  1000, 3),
                                new CaseItem("grenade.f1",           2,    8,    3, 1630356878),
                            }, "https://i.imgur.com/Dhmb1Ss.png"),
                        }
                    },
                    ChancesSettings = new Chances
                    {
                        Rarities = new List<Rarity>
                        {
                            new Rarity(35, "#FFFFFF0F"),
                            new Rarity(30, "#ADDFFF0F"),
                            new Rarity(25, "#C4A6FF0F"), 
                            new Rarity(5,  "#FFAE2B0F"),
                        },
                        FakeRarities = new List<Rarity>
                        {
                            new Rarity(15, "#FFFFFF0F"),
                            new Rarity(30, "#ADDFFF0F"),
                            new Rarity(30, "#C4A6FF0F"),
                            new Rarity(10, "#FFAE2B0F"),
                        }
                    },
                    DropSettrings = new Drops
                    {
                        DropList = new List<Drops.Drop>
                        {
                            new Drops.Drop
                            {
                                Permission = "skincases.default",
                                DropCases = new Dictionary<string, int>
                                {
                                    ["Бесплатный кейс"] = 3
                                },
                                Interval = 30,
                                MaxCaseAmount = 30
                            },
                            new Drops.Drop
                            {
                                Permission = "skincases.vip",
                                DropCases = new Dictionary<string, int>
                                {
                                    ["Взрывной кейс"] = 1
                                },
                                Interval      = 50,
                                MaxCaseAmount = 30
                            },
                        }
                    },
                    OtherSettings = new Others()
                };
            }
        }
        
        private class Case
        {
            [JsonProperty("Отображаемое название кейса")]
            public string DisplayName;
            [JsonProperty("Ссылка на изображение кейса")]
            public string ImageURL;
            
            [JsonProperty("Предметы получаемые при выпадении")]
            public List<CaseItem> Items = new List<CaseItem>();

            public Case(string displayName, List<CaseItem> caseItems, string url)
            {
                DisplayName = displayName;
                ImageURL = url;

                Items = caseItems;
            }

            public CaseItem GetFinishItem(bool fake = false)
            { 
                var usedInRowRarities = CaseHandler.ChancesSettings.FakeRarities.Select((i, t) => new {A = i, B = t}).Where(c => Items.Any(i => c.B == i.RarityType)).ToList();
                if (!fake)
                    usedInRowRarities = CaseHandler.ChancesSettings.Rarities.Select((i, t) => new {A = i, B = t}).Where(c => Items.Any(i => c.B == i.RarityType)).ToList();


                int totalRes = usedInRowRarities.Sum(p => p.A.Chance);
                int currentRes = Core.Random.Range(0, totalRes);

                int currentTry = 0;
                foreach (var check in usedInRowRarities.OrderBy(p => p.A.Chance))
                {
                    if (check.A.Chance + currentTry >= currentRes)
                    {
                        var item = Items.Where(p => p.RarityType == check.B).ToList().GetRandom();
                        return item;
                    }
                
                    currentTry += check.A.Chance;
                }

                return null;
            }

            public List<CaseItem> GetRandomLine()
            {
                List<CaseItem> skinLine = new List<CaseItem>();
                for (int i = 0; i < Oxide.Core.Random.Range(107, 108); i++)
                {
                    skinLine.Add(GetFinishItem(true));
                }

                var really = GetFinishItem(); 
                skinLine.Add(really);
                for (int i = 0; i < 3; i++)
                {
                    skinLine.Add(GetFinishItem(true));
                }

                return skinLine;
            }
        }
        
        private class InventoryItem
        {
            [JsonProperty("Короткое название предмета")]
            public string ShortName;
            [JsonProperty("ID скина для предмета")]
            public ulong SkinID;
            [JsonProperty("Количество предметов")]
            public int Amount;
            
            [JsonProperty("Дополнительная команда")]
            public string Command;
            [JsonProperty("Ссылка на изображение")]
            public string PictureURL;

            public Item CreateItem(BasePlayer player)
            {
                if (!string.IsNullOrEmpty(Command)) _.Server.Command(Command.Replace("%STEAMID%", player.UserIDString));
                if (!string.IsNullOrEmpty(ShortName))
                {
                    Item item = ItemManager.CreateByPartialName(ShortName, Amount);
                    item.skin = SkinID;

                    return item;
                }

                return null;
            }

            public static InventoryItem Generate(CaseItem caseItem)
            {
                return new InventoryItem
                {
                    ShortName  = caseItem.ShortName,
                    SkinID     = caseItem.SkinID,
                    Amount     = caseItem.GetRandomAmount(),
                    Command    = caseItem.Command,
                    PictureURL = caseItem.PictureURL
                };
            }
        }
        
        private class CaseItem : InventoryItem
        {
            [JsonProperty("Тип редкости для предмета")]
            public int RarityType = 0;
            
            [JsonProperty("Минимальное количество при выпадени")]
            public int MinimalAmount = 0;
            [JsonProperty("Максимальное количество при выпадении")]
            public int MaximumAmount = 0;
            
            public int GetRandomAmount() => Core.Random.Range(MinimalAmount, MaximumAmount);

            public CaseItem(string shortname, int minAmount, int maxAmount, int rarityType, ulong skinId = 0, string url = "", string command = "")
            {
                ShortName     = shortname;
                SkinID        = skinId;
                MinimalAmount = minAmount;
                MaximumAmount = maxAmount;
                RarityType    = rarityType;
                PictureURL    = url == "" ? null : url;
                Command       = command;
            }
        }    
        
        private class PlayerInventory
        {
            [JsonProperty("Доступные кейсы для открытия")]
            public Dictionary<string, int> Cases = new Dictionary<string, int>();
            [JsonProperty("Полученные вещи из кейсов")]
            public List<InventoryItem> Inventory = new List<InventoryItem>();

            [JsonProperty("История действия с кейсами")]
            public List<string> LogHistory = new List<string>();
            [JsonProperty("История ошибочных действий с кейсами")]
            public List<string> ErrorHistory = new List<string>();
        }
        
        #endregion
        
        #region Variables

        private static SkinCases _;
        private static Configuration CaseHandler = Configuration.Generate();
        private Hash<ulong, PlayerInventory> PlayerInventories = new Hash<ulong, PlayerInventory>();
        
        #endregion

        #region Initialization
        
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                CaseHandler = Config.ReadObject<Configuration>();
                if (CaseHandler.OtherSettings == null) CaseHandler.OtherSettings = new Configuration.Others();
                if (CaseHandler.ChancesSettings == null) CaseHandler.ChancesSettings = new Configuration.Chances();
                if (CaseHandler.CaseSettings == null) CaseHandler.CaseSettings = new Configuration.Cases();
                if (CaseHandler.DropSettrings == null) CaseHandler.DropSettrings = new Configuration.Drops();   
            }
            catch
            {
                PrintWarning($"Failed to read configuration [oxide/configs/{Name}]!");
                PrintWarning($"Check it with JSON Validator!");
                //LoadDefaultConfig();
                return;
            }
            
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => CaseHandler = Configuration.Generate();
        protected override void SaveConfig()        => Config.WriteObject(CaseHandler);

        private void OnServerInitialized()
        { 
            try { webrequest.Enqueue($"http://api.hougan.space/grab/{Name}/{Version}/{737}", "", (i, s) => {}, this); } catch (NullReferenceException) { }
            
            _ = this; 
            BasePlayer.activePlayerList.ForEach(OnPlayerInit);

            CaseHandler.DropSettrings.DropList.ForEach(p => { permission.RegisterPermission(p.Permission, this); });
            ServerMgr.Instance.StartCoroutine(GiveCases()); 
                
            CaseHandler.CaseSettings.CaseLits.ForEach(c =>
            {
                ImageLibrary.Call("AddImage", c.ImageURL, GetNameFromURI(c.ImageURL));
                c.Items.ForEach(i =>
                {
                    if (!i.ShortName.IsNullOrEmpty())
                    {
                        ImageLibrary.Call("AddImage", $"http://api.hougan.space/rust/item/getImage/{i.ShortName}/128", i.ShortName + 128, i.SkinID);
                    } 
                    if (i.SkinID != 0)
                    {
                        ImageLibrary.Call("AddImage", $"http://api.hougan.space/rust/skin/getImage/{i.SkinID}.png", i.ShortName, i.SkinID);
                    }

                    if (!i.PictureURL.IsNullOrEmpty())
                    {
                        ImageLibrary.Call("AddImage", i.PictureURL, GetNameFromURI(i.PictureURL)); 
                    }
                });
            });
            
            timer.Every(60, () => { BasePlayer.activePlayerList.ForEach(GiveAutoCases); }).Callback();
            timer.Every(60, () => { BasePlayer.activePlayerList.ForEach(SaveData); }).Callback();
        }
        
        private void OnPlayerInit(BasePlayer player)
        {
            LoadData(player);
        }
        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            SaveData(player);
        }
        
        private void Unload()
        {
            ServerMgr.Instance.StopAllCoroutines();
            foreach (var check in PlayerInventories)
            {
                SaveData(check.Key);
            }
        }

        #endregion

        #region Interface

        [ConsoleCommand("UI_SkinCases")]
        private void CmdChatHandler(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            ValidatePlayer(player);
            
            if (player != null && args.HasArgs(1))
            {
                if (args.Args[0] == "open")
                {
                    int caseId = -1;
                    if (!int.TryParse(args.Args[1], out caseId))
                    {
                        ErrorHistory(player, $"Попытка открытия кейса с неправильным ID: '{args.Args[1]}'");
                        return;
                    }
                    
                    var cCase = CaseHandler.CaseSettings.CaseLits.ElementAt(caseId);
                    if (cCase != null) 
                    {
                        if (!HasCase(player, cCase))
                        {
                            ErrorHistory(player, $"Попытка открытия кейса '{cCase.DisplayName}', которого нету в инвентаре у игрока");
                            return;
                        }
                        
                        LogHistory(player, $"Начало открытие кейса '{cCase.DisplayName}'!");
                        RemoveCase(player, cCase);
                        
                        CuiHelper.DestroyUi(player, Layer + ".Btn");
                        CuiHelper.DestroyUi(player, Layer + ".Btn1");
                        CuiElementContainer container = new CuiElementContainer();
                        
                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = "0.5 0.52", AnchorMax = "0.5 0.52", OffsetMin = "-150 -20", OffsetMax = "150 20" },
                            Button = { Color = "1 1 1 0.03" },
                            Text = { Text = $"ПОДОЖДИТЕ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 20 }
                        }, Layer, Layer + ".Btn");

                        if (!CaseHandler.OtherSettings.AllowOpenFewCase)
                        {
                            container.Add(new CuiPanel
                            {
                                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"},
                                Image         = {Color     = "0 0 0 0"}
                            }, "Overlay", Layer + ".Overlay");
                        }
                        
                        ServerMgr.Instance.StartCoroutine(DrawLine(player, cCase.GetRandomLine(), cCase, true));
                        CuiHelper.AddUi(player, container);
                    }
                    else
                    {
                        ErrorHistory(player, $"Попытка открытия несуществующего кейса: '{caseId}'");
                    }
                }

                if (args.Args[0] == "shop")
                {
                    LogHistory(player, "Игрок открывает список своих кейсов");
                    UI_DrawCaseInventory(player);
                }

                if (args.Args[0] == "shoppage")
                {
                    LogHistory(player, "Игрок открывает список своих кейсов");
                    UI_DrawCaseInventory(player, args.HasArgs(2) ? int.Parse(args.Args[1]) : 0);
                }

                if (args.Args[0] == "show")
                {
                    int caseId = -1;
                    if (!int.TryParse(args.Args[1], out caseId))
                    {
                        ErrorHistory(player, $"Попытка просмотра кейса с неправильным ID: '{args.Args[1]}'");
                        return;
                    }
                    
                    var cCase = CaseHandler.CaseSettings.CaseLits.ElementAt(caseId);
                    if (cCase != null)
                    {
                        LogHistory(player, $"Игрок просматривает кейс '{cCase.DisplayName}'");
                        if (!HasCase(player, cCase))
                            ErrorHistory(player, $"У игрока отсутствует просматриваемый кейс");
                        
                        UI_DrawCase(player, cCase);
                    }
                }

                if (args.Args[0] == "invpage")
                {
                    LogHistory(player, "Игрок открывает список своих предметов");
                    UI_DrawInventory(player, args.HasArgs(2) ? int.Parse(args.Args[1]) : 0);
                }

                if (args.Args[0] == "inv")
                {
                    LogHistory(player, $"---------------------");
                    LogHistory(player, "Игрок просматривает свои полученные предметы:");
                    foreach (var check in PlayerInventories[player.userID].Inventory)
                    {
                        LogHistory(player, $" - {check.ShortName} -> {check.Amount}x");
                    }
                    LogHistory(player, $"---------------------");
                    
                    UI_DrawInventory(player);
                }
                if (args.Args[0] == "take")
                {
                    int itemId = -1;
                    if (!int.TryParse(args.Args[1], out itemId))
                    {
                        ErrorHistory(player, $"Попытка взять предмет с неправильным порядковым номером: '{args.Args[1]}'");
                        return;
                    }
                    
                    var item = PlayerInventories[player.userID].Inventory.ElementAt(itemId);
                    if (item != null)
                    {
                        if (item.ShortName != "")
                        {
                            if (player.inventory.containerMain.itemList.Count >= 24)
                            {
                                ErrorHistory(player, $"Попытка забрать предмет {item.ShortName} x{item.Amount}, у игрока нету места!");
                                player.ChatMessage($"У вас <color=#4286f4>недостаточно</color> места в основном инвентаре!");
                                return;
                            }
                         
                            LogHistory(player, $"Игрок забрал предмет: {item.ShortName} x{item.Amount}");
                        }
                        
                        if (item.ShortName != "")
                            player.ChatMessage($"Вы <color=#4286f4>успешно</color> забрали {ItemManager.FindItemDefinition(item.ShortName).displayName.english} x{item.Amount}");
                        else
                            player.ChatMessage($"Вы <color=#4286f4>успешно</color> забрали предмет!");
                        
                        item.CreateItem(player)?.MoveToContainer(player.inventory.containerMain);
                        PlayerInventories[player.userID].Inventory.Remove(item);
                        
                        UI_DrawInventory(player);
                    }
                    else
                    {
                        ErrorHistory(player, $"Попытка забрать предмет под несуществующем номером: '{itemId}'");
                    }
                }
            }
               
        }

        private IEnumerator DrawLine(BasePlayer player, List<CaseItem> skinList, Case cCase, bool really = false)
        {
            List<CaseItem> localList = new List<CaseItem>();
            
            for (int z = 0; z < skinList.Count - 5; z++)
            {
                localList = skinList.Skip(z).Take(5).ToList();
                
                CuiElementContainer container = new CuiElementContainer();
                foreach (var check in localList.Select((i, t) => new {A = i, B = t}))
                {
                    try
                    {
                        CuiHelper.DestroyUi(player, Layer + $".Prize.{check.B}.Img");
                        CuiHelper.DestroyUi(player, Layer + $".Prize.{check.B}");

                        var kitMargin = 0.08f;
                        container.Add(new CuiButton
                        {
                            RectTransform =
                            {
                                AnchorMin = $"{0.3375973784 + check.B * kitMargin} 0.7",
                                AnchorMax = $"{0.3375973784 + check.B * kitMargin} 0.7",
                                OffsetMin = "-50 -50", OffsetMax = "50 50",
                            },
                            Button = {Color = HexToRustFormat(CaseHandler.ChancesSettings.Rarities.ElementAtOrDefault(check.A.RarityType).Color.Remove(6, 2) + 23)},
                            Text = {Text = ""}
                        }, Layer, Layer + $".Prize.{check.B}");

                        string img = !check.A.PictureURL.IsNullOrEmpty() ? GetNameFromURI(check.A.PictureURL) : check.A.ShortName;
                        string imageName = check.A.SkinID == 0 && check.A.PictureURL.IsNullOrEmpty() ? check.A.ShortName + 128 : img; 
                        
                        container.Add(new CuiElement 
                        {
                            Parent = Layer + $".Prize.{check.B}",
                            Name = Layer + $".Prize.{check.B}.Img",
                            Components =
                            { 
                                new CuiRawImageComponent {Png = (string) ImageLibrary.Call("GetImage", imageName, check.A.SkinID), Color = "1 1 1 1"},
                                new CuiRectTransformComponent
                                    {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5"}
                            }
                        });
                    }
                    catch (NullReferenceException e)
                    {
                        PrintWarning(e.StackTrace);
                    }
                } 
                CuiHelper.AddUi(player, container);
                container.Clear();
                
                Effect x = new Effect("assets/bundled/prefabs/fx/notice/stack.world.fx.prefab", player, 0, new Vector3(), new Vector3());
                EffectNetwork.Send(x, player.Connection);

                float delay = (float) (((float) Math.Pow(1.3, z - 50) / 502500) + (float) z * 0.001);
                yield return new WaitForSeconds(delay); 
            }
            
            CuiHelper.DestroyUi(player, Layer + ".Btn");
            CuiElementContainer newCont = new CuiElementContainer();
            int caseIndex = CaseHandler.CaseSettings.CaseLits.IndexOf(cCase);

            if (!really)
            {
                string openCase = HasCase(player, cCase) ? "ОТКРЫТЬ КЕЙС" : "У ВАС НЕТУ КЕЙСА";
                
                newCont.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.5 0.52", AnchorMax = "0.5 0.52", OffsetMin = "-150 -20", OffsetMax = "150 20" },
                    Button = { Color = "1 1 1 0.03", Command = HasCase(player, cCase) ? $"UI_SkinCases open {caseIndex}" : ""},
                    Text = { Text = openCase, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 20 }
                }, Layer, Layer + ".Btn");
            }
            else
            {
                CuiHelper.DestroyUi(player, Layer + ".Overlay");
                Effect x = new Effect("assets/prefabs/deployable/vendingmachine/effects/vending-machine-purchase-human.prefab", player, 0, new Vector3(), new Vector3());
                EffectNetwork.Send(x, player.Connection);
                
                string openCase = HasCase(player, cCase) ? "ОТКРЫТЬ КЕЙС" : "ВЫБЕРИТЕ КЕЙС";
                
                newCont.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.5 0.52", AnchorMax = "0.5 0.52", OffsetMin = "-200 -20", OffsetMax = "-5 20" },
                    Button = { Color = "1 1 1 0.03", Command = $"UI_SkinCases show {caseIndex}" },
                    Text = { Text = "ПРИНЯТЬ НАГРАДУ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 20 }
                }, Layer, Layer + ".Btn");
                newCont.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.5 0.52", AnchorMax = "0.5 0.52", OffsetMin = "5 -20", OffsetMax = "200 20" },
                    Button = { Color = "1 1 1 0.03", Command = HasCase(player, cCase) ? $"UI_SkinCases open {caseIndex}" : "UI_SkinCases shop"},
                    Text = { Text = openCase, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 20 }
                }, Layer, Layer + ".Btn1");
 
                var caseItem = localList.ElementAt(2);
                var addedItem = AddItem(player, caseItem);
                if (caseItem.RarityType == 3 && localList.ElementAt(1).RarityType != 3)
                {
                    Server.Broadcast($"Игрок <color=#4286f4>{player.displayName}</color> получил <color=#f4c242>легендарный</color> предмет!\n" +
                                     $"Испытай удачу: <color=#4286f4>/case</color>");
                }
                
				if (!string.IsNullOrEmpty(addedItem.ShortName))
					LogHistory(player, $"Получен предмет {addedItem.ShortName} [{addedItem.Amount}x] из кейса {cCase.DisplayName}");
                else if (!string.IsNullOrEmpty(addedItem.Command))
				    LogHistory(player, $"Получен предмет {addedItem.Command} из кейса {cCase.DisplayName}");
            }

            CuiHelper.AddUi(player, newCont);
        }

        private void UI_DrawCaseInventory(BasePlayer player, int page = 0)
        {
            CuiHelper.DestroyUi(player, Layer);
            CuiElementContainer container = new CuiElementContainer();
            
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.8", }
            }, "Overlay", Layer);
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Close = Layer },
                Text = { Text = "" }
            }, Layer);
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "-100 -80", OffsetMax = "100 -30" },
                Button = { Color = "1 1 1 0", Command = $"UI_SkinCases inv" },
                Text = { Text = $"->  ОТКРЫТЬ ИНВЕНТАРЬ СОБРАННЫХ ПРЕДМЕТОВ  <-", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 24 }
            }, Layer, Layer + ".Inventory");

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 -1", AnchorMax                                        = "1 0", OffsetMax             = "0 5"},
                Text          = {Text      = "Там хранятся все полученные предметы из кейсов, вы сможете забрать их в любое удобное вам время!\n" +
                        "Внимание, производится очистка кейса каждый глобальный WIPE!", Align = TextAnchor.UpperCenter, Font = "robotocondensed-regular.ttf", Color = "1 1 1 0.7"}
            }, Layer + ".Inventory");


            var availableCases = CaseHandler.CaseSettings.CaseLits.OrderByDescending(p => PlayerInventories[player.userID].Cases[p.DisplayName] > 0).ToList();
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-100 -50", OffsetMax = "0 50" },
                Button = { Color = "1 1 1 0", Command = $"UI_SkinCases shoppage {page + 1}" },
                Text = { Text = $">", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 80, Color = availableCases.Count > (page + 1) * 10 ? "1 1 1 1" : "1 1 1 0.2" }
            }, Layer);
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "0 -50", OffsetMax = "100 50" },
                Button        = { Color     = "1 1 1 0", Command = $"UI_SkinCases shoppage {page - 1}" },
                Text          = { Text      = $"<", Align        = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 80, Color = page > 0 ? "1 1 1 1" : "1 1 1 0.2" }
            }, Layer);

            var list = availableCases.Skip(page * 10).Take(10);
            float currentX = -525;
            float currentY = Mathf.CeilToInt((list.Count() / 5f) - 0.01f) / 2f * 230;
            
            var playerInfo = new PlayerInventory();
            if (!PlayerInventories.TryGetValue(player.userID, out playerInfo)) return;
            
            foreach (var check in list.Select((i,t) => new { A = i, B = t })) 
            {
                int indexOfCase = CaseHandler.CaseSettings.CaseLits.IndexOf(check.A);
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.5 0.5", AnchorMax = $"0.5 0.5", OffsetMin = $"{currentX} {currentY - 200 - 20}", OffsetMax = $"{currentX + 200} {currentY - 20}" },
                    Button = { Color = "1 1 1 0", Command = $"UI_SkinCases show {indexOfCase}"},
                    Text = { Text = "" }
                }, Layer, Layer + $".{check.B}"); 
                
                container.Add(new CuiElement
                {
                    Parent = Layer + $".{check.B}",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", GetNameFromURI(check.A.ImageURL)), Color = playerInfo.Cases[check.A.DisplayName] > 0 ? "1 1 1 1" : "1 1 1 0.3" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"}
                    }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 -15", OffsetMax = "15 0" },
                    Button = { Color = "1 1 1 0", Command = $"UI_SkinCases show {indexOfCase}" },
                    Text = { Text = $"x{playerInfo.Cases[check.A.DisplayName]}", Align = TextAnchor.LowerRight, Font = "robotocondensed-regular.ttf", FontSize = 30, Color = playerInfo.Cases[check.A.DisplayName] > 0 ? "1 1 1 0.8" : "1 1 1 0.2" }
                }, Layer + $".{check.B}"); 

                currentX += 210;
                if (currentX > 500)
                {
                    currentX = -525;
                    currentY -= 230;
                }
            }

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 0.08", AnchorMax = "1 0.08", OffsetMax = "0 50"},
                Text = {Text = "Вы получаете <b>бесплатные кейсы</b>, просто играя на сервере!\n" +
                        "Если вы не хотите ждать, вы можете приобрести кейсы в нашем магазине", Align = TextAnchor.UpperCenter, Font = "robotocondensed-regular.ttf", Color = "1 1 1 0.7"}
            }, Layer); 

            CuiHelper.AddUi(player, container);
        }
        
        private void UI_DrawInventory(BasePlayer player, int page = 0)
        {
            CuiHelper.DestroyUi(player, Layer);
            CuiElementContainer container = new CuiElementContainer();
            
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.8" }
            }, "Overlay", Layer);
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Close = Layer },
                Text = { Text = "" }
            }, Layer);
            
            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".SkinsHandler",
                Components =
                {
                    new CuiImageComponent { Color = "1 1 1 0" },
                    new CuiRectTransformComponent { AnchorMin = $"0.5 0.5", AnchorMax = $"0.5 0.5", OffsetMin = "-600 -220", OffsetMax = "600 220" }
                }
            });

                
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.9", AnchorMax = "0.5 0.9", OffsetMin = "-100 -20", OffsetMax = "100 20" },
                Button = { Color = "1 1 1 0.03", Command = $"UI_SkinCases shop" },
                Text = { Text = $"МОИ КЕЙСЫ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 20 }
            }, Layer, Layer + ".Inventory");
            
            if (PlayerInventories[player.userID].Inventory.Count == 0)
            {

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0" },
                    Text = { Text = $"У ВАС НЕТУ ПРЕДМЕТОВ\n" +
                                    $"<size=24>ВЫ ПОЛУЧИТЕ ИХ ИЗ КЕЙСА</size>\n\n" +
                                    $"<size=16>КУПИТЕ VIP, ЧТОБЫ ПОЛУЧАТЬ БОЛЬШЕ ПРЕДМЕТОВ</size>", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 34 }
                }, Layer + ".SkinsHandler");
                
                CuiHelper.AddUi(player, container);
                return;
            }
            
            
            if (page > 0)
            {
                container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.5 0.9", AnchorMax = "0.5 0.9", OffsetMin = "-175 -20", OffsetMax = "-110 20" },
                        Button = { Color = "1 1 1 0.03", Command = $"UI_SkinCases invpage {page - 1}" },
                        Text = { Text = $"<<<", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 20 }
                    }, Layer, Layer + ".Nazad");
            }

            if ((float) PlayerInventories[player.userID].Inventory.Count > (page + 1) * 44)
            {
                container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.5 0.9", AnchorMax = "0.5 0.9", OffsetMin = "110 -20", OffsetMax = "175 20" },
                        Button = { Color = "1 1 1 0.03", Command = $"UI_SkinCases invpage {page + 1}" },
                        Text = { Text = $">>>", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 20 }
                    }, Layer, Layer + ".Vpered");
            }

            var   list     = PlayerInventories[player.userID].Inventory.Skip(page * 44).Take(44);
            float currentX = -600;
            float currentY = Mathf.CeilToInt((list.Count() / 11f) - 0.010737f) / 2f * 106f;
            
            var playerInfo = new PlayerInventory();
            if (!PlayerInventories.TryGetValue(player.userID, out playerInfo)) return;
            foreach (var check in list.Select((i,t) => new { A = i, B = t }))
            { 
                string currentColor = "FFFFFF03";
                container.Add(new CuiButton 
                {
                    RectTransform = { AnchorMin = $"0.5 0.5", AnchorMax = $"0.5 0.5", OffsetMin = $"{15 +currentX} {currentY - 100 - 1}", OffsetMax = $"{15 + currentX + 100} {currentY - 1}" },
                    Button = { Color = "1 1 1 0.03", Command = $"UI_SkinCases take {check.B + page * 44}"}, 
                    Text = { Text = "" }
                }, Layer + ".SkinsHandler", Layer + $".{check.B}");
                
                string img = !check.A.PictureURL.IsNullOrEmpty() ? GetNameFromURI(check.A.PictureURL) : check.A.ShortName;
                string imageName = check.A.SkinID == 0 && check.A.PictureURL.IsNullOrEmpty() ? check.A.ShortName + 128 : img; 
                
                container.Add(new CuiElement
                {
                    Parent = Layer + $".{check.B}",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", imageName, check.A.SkinID) },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 -50", OffsetMax = "50 50" }
                    }
                });
            
                /*container.Add(new CuiElement
                {
                    Parent = Layer + $".{check.B}",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", check.A., check.A.Id) },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" }
                    }
                });*/

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "-5 10", OffsetMin = "0 3"},
                    Button = { Color = "1 1 1 0", Command = $"UI_SkinCases take {check.B + page * 44}" },
                    Text = { Text = check.A.Amount > 1 ? ("x" + check.A.Amount) : "", Align = TextAnchor.LowerRight, Font = "robotocondensed-regular.ttf", Color = "1 1 1 0.4"}
                }, Layer + $".{check.B}");

                currentX += 107; 
                if (currentX > 550)
                {
                    currentX =  -600;
                    currentY -= 107;
                }
            }

            CuiHelper.AddUi(player, container);
        }

        [PluginReference] private Plugin ImageLibrary;
        private const string Layer = "UI_SkinCases";
        private void UI_DrawCase(BasePlayer player, Case currentCase)
        {
            CuiHelper.DestroyUi(player, Layer); 
            CuiElementContainer container = new CuiElementContainer();
            
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.8" }
            }, "Overlay", Layer); 
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Close = Layer },
                Text = { Text = "" }
            }, Layer);
            
            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".SkinsHandler",
                Components =
                {
                    new CuiImageComponent { Color = "1 1 1 0.03" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.25", AnchorMax = "0.5 0.25", OffsetMin = "-330 -130", OffsetMax = "330 130" }
                }
            });
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.3375973784 0.7", AnchorMax = "0.65759839287374 0.7", OffsetMin = "-50 0", OffsetMax = "50 60" },
                Button = { Color = "1 1 1 0.03" },
                Text = { Text = "" }
            }, Layer);
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.3375973784 0.7", AnchorMax = "0.65759839737284 0.7", OffsetMin = "-50 -60", OffsetMax = "50 0" },
                Button = { Color = "1 1 1 0.03" },
                Text = { Text = "" }
            }, Layer);
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.52", AnchorMax = "0.5 0.52", OffsetMin = "-150 -20", OffsetMax = "150 20" },
                Button = { Color = "1 1 1 0.03", Command = $"UI_SkinCases open {CaseHandler.CaseSettings.CaseLits.IndexOf(currentCase)}" },
                Text = { Text = $"ОТКРЫТЬ ЗА {15} BET", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 20 }
            }, Layer, Layer + ".Btn");
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.9", AnchorMax = "0.5 0.9", OffsetMin = "-205 -20", OffsetMax = "-5 20" },
                Button = { Color = "1 1 1 0.03737", Command = $"UI_SkinCases inv" },
                Text = { Text = $"ИНВЕНТАРЬ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 20 }
            }, Layer, Layer + ".Inventory");
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.9", AnchorMax = "0.5 0.9", OffsetMin = "5 -20", OffsetMax = "205 20" },
                Button = { Color = "1 1 1 0.03", Command = $"UI_SkinCases shop 0" },
                Text = { Text = $"МОИ КЕЙСЫ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 20 }
            }, Layer, Layer + ".Shop");
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.5 0.7", AnchorMax = $"0.5 0.7", OffsetMin = "-5 -80", OffsetMax = "5 -60" },
                Button = { Color = "1 1 1 0.03" },
                Text = { Text = "" }
            }, Layer, Layer + ".Delimiter");
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.5 0.7", AnchorMax = $"0.5 0.7", OffsetMin = "-5 60", OffsetMax = "5 80" },
                Button = { Color = "1 1 1 0.03737" },
                Text = { Text = "" }
            }, Layer, Layer + ".Delimiter");
            

            var kitMargin = 0.197f;
            foreach (var check in currentCase.Items.OrderBy(p => p.RarityType).Select((i,t) => new { A = i, B = t }))
            {
                string currentColor = CaseHandler.ChancesSettings.Rarities[check.A.RarityType].Color;
                container.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = $"{0.01200886 + check.B * kitMargin - Math.Floor((double) check.B / 5) * 5 * kitMargin} {0.5106891 - Math.Floor((double) check.B / 5) * 0.48f}", 
                        AnchorMax = $"{0.1975984 + check.B * kitMargin - Math.Floor((double) check.B / 5) * 5 * kitMargin} {0.9652275 - Math.Floor((double) check.B / 5) * 0.48f}", 
                        OffsetMax = "0 0"
                    },
                    Button = { Color = HexToRustFormat(CaseHandler.ChancesSettings.Rarities.ElementAtOrDefault(check.A.RarityType).Color) },
                    Text = { Text = "" }
                }, Layer + ".SkinsHandler", Layer + $".{check.B}");

                string img = check.A.PictureURL != null ? GetNameFromURI(check.A.PictureURL) : check.A.ShortName;
                container.Add(new CuiElement
                {
                    Parent = Layer + $".{check.B}",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", img, check.A.SkinID) },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" }
                    }
                });

                string niceName = $"{check.A.MinimalAmount} шт. - {check.A.MaximumAmount} шт.";
                
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.2", OffsetMax = "0 0" },
                    Button = { Color = HexToRustFormat(CaseHandler.ChancesSettings.Rarities.ElementAtOrDefault(check.A.RarityType).Color.Remove(6, 2) + 23) },
                    Text = { Text = niceName, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf" }
                }, Layer + $".{check.B}");
            }

            CuiHelper.AddUi(player, container);
            
            List<CaseItem> newLine = new List<CaseItem>();
            for (int i = 0; i < 6; i++)
            {
                newLine.Add(currentCase.GetFinishItem());
            } 
            ServerMgr.Instance.StartCoroutine(DrawLine(player, newLine, currentCase));
        }

        #endregion

        #region Functions

        private IEnumerator GiveCases()
        {
            while (true)
            {
                PrintWarning($"Start giving free cases to users:");
                Stopwatch x = new Stopwatch();
                int affected = 0;
                x.Start();
                
                CaseHandler.DropSettrings.DropList.ForEach(p =>
                {
                    if (CurrentTime() > p.NextDropTime)
                    {
                        try
                        {
                            p.NextDropTime = CurrentTime() + p.Interval;

                            foreach (var group in permission.GetPermissionGroups(p.Permission))
                            {
                                foreach (var users in permission.GetUsersInGroup(group))
                                {
                                    foreach (var cases in p.DropCases)
                                    {
                                        var id = ulong.Parse(users.Split('(')[0].Replace(" ", ""));

                                        if (p.MaxCaseAmount > 0)
                                        {
                                            if (!PlayerInventories.ContainsKey(id)) continue;
                                            if (!PlayerInventories[id].Cases.ContainsKey(cases.Key)) continue;
                                            if (PlayerInventories[id].Cases[cases.Key] >= p.MaxCaseAmount) continue;
                                        }

                                        AddCase(id.ToString(), cases.Key, cases.Value);
                                        affected++;
                                    }
                                }
                            }
                        }
                        catch (FormatException)
                        {
                            PrintError($"Failed to give free cases!");
                        }
                        
                    }
                });

                x.Stop();
                PrintError($"Giving free cases finish. Elapsed time: {x.Elapsed.ToShortString()}:{x.Elapsed.Milliseconds}. Affected: {affected}");
                SaveConfig();
                yield return new WaitForSeconds(180);
            }
        }
        
        private InventoryItem AddItem(BasePlayer player, CaseItem caseItem)
        {
            var item = InventoryItem.Generate(caseItem);
            PlayerInventories[player.userID].Inventory.Add(item);
            
            return item;
        }

        private bool HasCase(BasePlayer player, Case cCase)
        {
            if (!PlayerInventories[player.userID].Cases.ContainsKey(cCase.DisplayName)) return false; 

            return PlayerInventories[player.userID].Cases[cCase.DisplayName] > 0;
        }

        private void RemoveCase(BasePlayer player, Case cCase) => PlayerInventories[player.userID].Cases[cCase.DisplayName] = Math.Max(PlayerInventories[player.userID].Cases[cCase.DisplayName] - 1, 0);
        private void AddCase(BasePlayer player, Case cCase, int amount = 1)
        {
            if (player != null && player.IsConnected)
            {
                player?.ChatMessage($"Вы получили кейс: \"<color=#f4c242>{cCase.DisplayName}</color>\"!\n" +
                    $"Испытайте свою удачу: <color=#4286f4>/case</color>");
            }
            
            ValidatePlayer(player);
            if (!PlayerInventories[player.userID].Cases.ContainsKey(cCase.DisplayName))
                PlayerInventories[player.userID].Cases.Add(cCase.DisplayName, 0);  
            
            for (int i = 0; i < amount; i++)
                PlayerInventories[player.userID].Cases[cCase.DisplayName]++;
        }
        private void AddCase(BasePlayer player, string cCase, int amount = 1)
        {
            var findCase = CaseHandler.CaseSettings.CaseLits.FirstOrDefault(p => p.DisplayName == cCase);
            if (findCase == null)
            {
                ErrorHistory(player, $"Попытка добавление несуществующего кейса: '{cCase}' для игрока '{player}'");
                return;
            }
            
            AddCase(player, findCase, amount);
        }

        private void  AddCase(string playerId, string cCase, int amount = 1)
        {
            BasePlayer target = BasePlayer.Find(playerId);
            if (target == null) target = new BasePlayer {userID = ulong.Parse(playerId), displayName = "UNKOWN"};
            
            var findCase = CaseHandler.CaseSettings.CaseLits.FirstOrDefault(p => p.DisplayName == cCase);
            if (findCase == null) 
            {
                ErrorHistory(target, $"Попытка добавление несуществующего кейса: '{cCase}' для игрока '{playerId}'");
                return;
            }
            
            AddCase(target, findCase, amount); 
        }
        
        private void LogHistory(BasePlayer player, string text)
        {
            PlayerInventories[player.userID].LogHistory.Add($"[{DateTime.Now.ToShortTimeString()}] {text}");
        }

        private void ErrorHistory(BasePlayer player, string text)
        {
            LogToFile($"Errors", $"------------------------\n" +
                                 $"Игрок: {player}\n" +
                                 $"{text}\n" +
                                 $"------------------------", this);
            PlayerInventories[player.userID].ErrorHistory.Add($"[{DateTime.Now.ToShortTimeString()}] {text}");
        }

        private void ValidatePlayer(BasePlayer player)
        {
            if (PlayerInventories.ContainsKey(player.userID)) return;
            LoadData(player);
        }

        #endregion
 
        #region Data

        private void GiveAutoCases(BasePlayer player)
        {
            
        }

        private void LoadData(BasePlayer player)
        {
            var possibleData = Interface.Oxide.DataFileSystem.ReadObject<PlayerInventory>($"SkinCases/{player.userID}");
            
            if (!PlayerInventories.ContainsKey(player.userID))
                PlayerInventories.Add(player.userID, new PlayerInventory());
             
            PlayerInventories[player.userID] = possibleData ?? new PlayerInventory();

            foreach (var check in CaseHandler.CaseSettings.CaseLits)
            {
                if (!PlayerInventories[player.userID].Cases.ContainsKey(check.DisplayName))
                    PlayerInventories[player.userID].Cases.Add(check.DisplayName, 0); 
            }

            GiveAutoCases(player);
        }

        private void SaveData(BasePlayer player) => SaveData(player.userID);
        private void SaveData(ulong userId)
        {
            Interface.Oxide.DataFileSystem.WriteObject($"SkinCases/{userId}", PlayerInventories[userId]);
        }

        #endregion

        #region Commands

        [ConsoleCommand("caseinfo")]
        private void cmdInfo(ConsoleSystem.Arg args)
        {
            args.ReplyWithObject($"Не открыто кейсов у активных игроков: {PlayerInventories.Sum(p => p.Value.Cases.Count)} шт.");
        }
        
        [ConsoleCommand("allcase")]
        private void cmdGiveAllCase(ConsoleSystem.Arg args)
        {
            if (args.Player() != null && !args.Player().IsAdmin) return;
             
            CaseHandler.CaseSettings.CaseLits.ForEach(p => 
            { 
                AddCase(BasePlayer.Find(args.Args[0]), p, int.Parse(args.Args[1]));  
            });
        }
         
        [ConsoleCommand("case")]
        private void cmdGiveCase(ConsoleSystem.Arg args)
        {
            if (args.Player() != null && !args.Player().IsAdmin) return;
            
            AddCase(args.Args[0], args.Args[1], int.Parse(args.Args[2]));
        }
 
        [ConsoleCommand("caseall")]
        private void cmdGiveCaseAll(ConsoleSystem.Arg args)
        {
            if (args.Player() != null && !args.Player().IsAdmin) return;
            
            BasePlayer.activePlayerList.ForEach(p =>
            {
                AddCase(p, args.Args[0], int.Parse(args.Args[1]));
            });
        }
        
        [ChatCommand("case")]
        private void CmdChatCase(BasePlayer player)
        {
			if (Interface.Oxide.CallHook("CanUseCase", player) != null) return;
            UI_DrawCaseInventory(player);
        }

        #endregion
        
        #region Utils

        private static string GetNameFromURI(string uri)
        {
            return uri.Split('/')[uri.Split('/').Length - 1].Split('.')[0];
        } 
        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        private static double CurrentTime() { return DateTime.UtcNow.Subtract(epoch).TotalSeconds; }
        
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

            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        #endregion
    }
}