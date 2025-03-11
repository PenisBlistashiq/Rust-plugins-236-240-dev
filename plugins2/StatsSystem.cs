using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;


namespace Oxide.Plugins
{
    [Info("StatsSystem", "Dezz", "1.0.6")]
    class StatsSystem : RustPlugin
    {
        #region Вар
        string Layer = "Stats_UI";

        [PluginReference] Plugin ImageLibrary, RustStore;

        Dictionary<ulong, DBSettings> DB = new Dictionary<ulong, DBSettings>();

        public string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary?.Call("GetImage", shortname, skin);
        #endregion

        #region Класс
        public class DBSettings
        {
            public string DisplayName;
            public int Points = 0;
            public bool IsConnected;
            public int Balance;
            public Dictionary<string, int> Settings = new Dictionary<string, int>()
            {
                ["Kill"] = 0,
                ["Death"] = 0,
                ["Time"] = 0
            };
            public Dictionary<string, int> Res = new Dictionary<string, int>()
            {
                ["wood"] = 0,
                ["stones"] = 0,
                ["metal.ore"] = 0,
                ["sulfur.ore"] = 0,
                ["hq.metal.ore"] = 0,
                ["cloth"] = 0,
                ["leather"] = 0,
                ["fat.animal"] = 0,
                ["cratecostume"] = 0
            };
        }
        #endregion

        #region Конфиг
        Configuration config;
        class Configuration 
        {
            [JsonProperty("ID магазина")] public string ShopID = "";
            [JsonProperty("Secret ключ магазина")] public string Secret = "";
            [JsonProperty("Настройки бонусов")] public List<string> Bonus;
            public static Configuration GetNewConfig() 
            {
                return new Configuration
                {
                    Bonus = new List<string>()
                    {
                        "0",
                        "0",
                        "0",
                        "0",
                        "0",
                        "0",
                        "0",
                        "0",
                        "0",
                        "0"
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
                if (config?.Bonus == null) LoadDefaultConfig();
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

        #region Хуки
        void OnServerInitialized()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("StatsSystem/PlayerList"))
                DB = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, DBSettings>>("StatsSystem/PlayerList");

            foreach (var check in ResImage)
                ImageLibrary.Call("AddImage", $"https://rustlabs.com/img/items180/{check}.png", check);

            foreach (var check in BasePlayer.activePlayerList)
                OnPlayerConnected(check);

            timer.Every(60f, PlayTime);
        }

        void PlayTime()
        {
            foreach (var check in BasePlayer.activePlayerList)
                DB[check.userID].Settings["Time"] += 1;
        }

        void OnPlayerConnected(BasePlayer player) 
        {
            SteamAvatarAdd(player.UserIDString);
            if (!DB.ContainsKey(player.userID))
                DB.Add(player.userID, new DBSettings());

            DB[player.userID].DisplayName = player.displayName;
            DB[player.userID].IsConnected = true;
        }

        void OnPlayerDisconnected(BasePlayer player) 
        {
            DB[player.userID].IsConnected = false;
            SaveDataBase();
        }
        
        void Unload()
        {
            SaveDataBase();
        }

        void SaveDataBase() 
        {
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("StatsSystem/PlayerList", DB); 
        }

        void OnDispenserGather(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (dispenser == null || player == null || item == null) return;
            if (DB[player.userID].Res.ContainsKey(item.info.shortname))
            {
                DB[player.userID].Res[item.info.shortname] += item.amount;
                return;
            }
        }

        void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (dispenser == null || player == null || item == null) return;
            if (DB[player.userID].Res.ContainsKey(item.info.shortname))
            {
                DB[player.userID].Res[item.info.shortname] += item.amount;
                DB[player.userID].Points += 7;
                return;
            }
        }

        void OnCollectiblePickup(Item item, BasePlayer player)
        {
            if (item == null || player == null) return;
            if (DB[player.userID].Res.ContainsKey(item.info.shortname))
            {
                DB[player.userID].Res[item.info.shortname] += item.amount;
                return;
            }
        }

        void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (info == null || player == null || player.IsNpc || info.InitiatorPlayer == null || info.InitiatorPlayer.IsNpc) return;
            
            if (info.InitiatorPlayer != null)
            {
                var killer = info.InitiatorPlayer;

                if (killer != player) 
                { 
                    if (DB.ContainsKey(killer.userID))
                    {
                        DB[killer.userID].Settings["Kill"]++;
                        DB[killer.userID].Points += 100;
                    }
                }
                if (DB.ContainsKey(player.userID))
                {
                    DB[player.userID].Settings["Death"]++;
                    DB[player.userID].Points -= 25;
                }
            }
        }

        public ulong lastDamageName;
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is BradleyAPC && info.Initiator is BasePlayer)
                lastDamageName = info.Initiator.ToPlayer().userID;
            if (entity is BaseHelicopter && info.Initiator is BasePlayer)
                lastDamageName = info.Initiator.ToPlayer().userID;
        }  

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return;
            BasePlayer player = null;

            if (info.InitiatorPlayer != null) 
                player = info.InitiatorPlayer;

            if (player == null) return;
            
            if (entity is BradleyAPC)
            { 
                player = BasePlayer.FindByID(lastDamageName);
                DB[player.userID].Points += 750;
            }

            if (entity is BaseHelicopter)
            { 
                player = BasePlayer.FindByID(lastDamageName);
                DB[player.userID].Points += 1500;    
            }

            if (entity.ShortPrefabName.Contains("barrel"))
            {
                DB[player.userID].Res["cratecostume"]++;
                DB[player.userID].Points += 2;
            }
        }

        void OnNewSave()
        {
            timer.In(60, () => 
            {
                PrintWarning("Обнаружен вайп, происходит выдача призов за топ и очистка даты!");

                foreach (var check in DB)
                {
                    check.Value.Points = 0;
                    check.Value.IsConnected = false;
                    check.Value.Settings = new Dictionary<string, int>()
                    {
                        ["Kill"] = 0,
                        ["Death"] = 0,
                        ["Time"] = 0
                    };
                    check.Value.Res = new Dictionary<string, int>()
                    {
                        ["wood"] = 0,
                        ["stones"] = 0,
                        ["metal.ore"] = 0,
                        ["sulfur.ore"] = 0,
                        ["hq.metal.ore"] = 0,
                        ["cloth"] = 0,
                        ["leather"] = 0,
                        ["fat.animal"] = 0,
                        ["cratecostume"] = 0
                    };
                }
                int x = 0;
                foreach (var check in DB.Take(10))
                {
                    check.Value.Balance += int.Parse(config.Bonus.ElementAt(x));
                    x++;
                }

                SaveDataBase();
            });
        }
        #endregion

        #region Вывод коинов
        void ApiChangeGameStoresBalance(ulong userId, int amount)
        {
            var player = BasePlayer.FindByID(userId);
            ExecuteApiRequest(new Dictionary<string, string>()
            {
                { "action", "moneys" },
                { "type", "plus" },
                { "DisplayName", player.displayName.ToUpper() },
                { "steam_id", userId.ToString() },
                { "amount", amount.ToString() },
                { "mess", "Спасибо что играете у нас!"}
            });
        }

        void APIChangeUserBalance(ulong steam, int balanceChange)
        {
            if (RustStore)
            {
                plugins.Find("RustStore").CallHook("APIChangeUserBalance", steam, balanceChange, new Action<string>((result) =>
                {
                    if (result == "SUCCESS")
                    {
                        LogToFile("LogMoscow", $"СтимID: {steam}\nУспешно получил {balanceChange} рублей на игровой счет!\n", this);
                        PrintWarning($"Игрок {steam} успешно получил {balanceChange} рублей");
                    }
                    else
                    {
                        PrintError($"Ошибка пополнения баланса для {steam}!");
                        PrintError($"Причина: {result}");
                        LogToFile("logError", $"Баланс игрока {steam} не был изменен, ошибка: {result}", this);
                    }
                }));
            }
        }

        void ExecuteApiRequest(Dictionary<string, string> args)
        {
            string url = $"https://gamestores.ru/api/?shop_id={config.ShopID}&secret={config.Secret}" + $"{string.Join("", args.Select(arg => $"&{arg.Key}={arg.Value}").ToArray())}";
            LogToFile("LogGS", $"Ник: {args["DisplayName"]}\nСтимID: {args["steam_id"]}\nУспешно получил {args["amount"]} рублей на игровой счет!\n", this);
            webrequest.EnqueueGet(url, (i, s) =>
            {
                if (i != 200)
                {
                    PrintError($"Ошибка соединения с сайтом!");
                }
                else
                {
                    JObject jObject = JObject.Parse(s);
                    if (jObject["result"].ToString() == "fail")
                    {
                        PrintError($"Ошибка пополнения баланса для {args["steam_id"]}!");
                        PrintError($"Причина: {jObject["message"].ToString()}");
                        LogToFile("logError", $"Баланс игрока {args["steam_id"]} не был изменен, ошибка: {jObject["message"].ToString()}", this);
                    }
                    else
                    {
                        PrintWarning($"Игрок {args["steam_id"]} успешно получил {args["amount"]} рублей");
                    }
                }
            }, this);
        }
        #endregion

        #region Картинки ресурсов
        List<string> ResImage = new List<string>()
        {
            "wood",
            "stones",
            "metal.ore",
            "sulfur.ore",
            "hq.metal.ore",
            "cloth",
            "leather",
            "fat.animal",
            "cratecostume"
        };
        #endregion

        #region Команды
        [ChatCommand("stat")]
        void ChatTop(BasePlayer player) 
        {
            StatsUI(player); 
        }

        [ConsoleCommand("stats")]
        void ConsoleSkip(ConsoleSystem.Arg args)
        {
            var player = args.Player();

            if (player != null && args.HasArgs(1))
            {
                if (args.Args[0] == "profile")
                {
                    ProfileUI(player, ulong.Parse(args.Args[1]), int.Parse(args.Args[2]));
                }
                if (args.Args[0] == "back")
                {
                    StatsUI(player);
                }
                if (args.Args[0] == "skip")
                {
                    StatsUI(player, int.Parse(args.Args[1]));
                }
                if (args.Args[0] == "take")
                {
                    if (DB[player.userID].Balance == 0)
                    {
                        SendReply(player, "Ваш баланс на данный момент пуст!");
                        return;
                    }
                    if (string.IsNullOrEmpty(config.Secret)) APIChangeUserBalance(player.userID, DB[player.userID].Balance);
                    else ApiChangeGameStoresBalance(player.userID, DB[player.userID].Balance);

                    SendReply(player, $"Вы успешно вывели {DB[player.userID].Balance} рублей, на игровой магазин!");
                    DB[player.userID].Balance -= DB[player.userID].Balance;
                    CuiHelper.DestroyUi(player, "MainStats");
                }
            }
        }
        #endregion

        #region Интерфейс
        void StatsUI(BasePlayer player, int page = 0)
        {
            CuiHelper.DestroyUi(player, "MainStats");
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0" }
            }, "Overlay", "MainStats");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.9", Material = "assets/icons/greyout.mat" }
            }, "MainStats");            

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.31 0.2", AnchorMax = "0.69 0.8", OffsetMax = "0 0" },
                Image = { Color = "1 1 1 0" }
            }, "MainStats", Layer);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.23", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Image = { Color = "0.36 0.34 0.32 0.75", Material = "assets/icons/greyout.mat" }
            }, Layer, "Top");

            /*container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.003 0.94", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Button = { Color = "0.56 0.87 0.56 0" },
                Text = { Text = $"        #            имя игрока                                    награда                        очки", Color = "1 1 1 1", Align = TextAnchor.MiddleLeft, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, "Top");*/

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.01 0.95", AnchorMax = $"1 1", OffsetMin = "2 1", OffsetMax = "-2 -1" },
                Image = { Color = "0.5 0.5 0.5 0" }
            }, "Top", "TTT");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.0 -0.55", AnchorMax = $"0.07 1.215", OffsetMax = "0 0" },
                Button = { Color = "0.56 0.87 0.56 0" },
                Text = { Text = $"#", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, "TTT");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.115 -0.55", AnchorMax = $"0.4 1.215", OffsetMax = "0 0" },
                Button = { Color = "0.56 0.87 0.56 0" },
                Text = { Text = $"ИМЯ ИГРОКА", Color = "1 1 1 1", Align = TextAnchor.MiddleLeft, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, "TTT");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.4 -0.55", AnchorMax = $"0.565 1.215", OffsetMax = "0 0" },
                Button = { Color = "0.56 0.87 0.56 0" },
                Text = { Text = $"НАГРАДА", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, "TTT");   

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.63 -0.55", AnchorMax = $"1.075 1.215", OffsetMax = "0 0" },
                Button = { Color = "0.56 0.87 0.56 0" },
                Text = { Text = $"ОЧКИ", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, "TTT");                        

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.8 0.02", AnchorMax = $"0.987 0.11", OffsetMax = "0 0" },
                Button = { Color = "0.46 0.44 0.42 1.00", Material = "assets/icons/greyout.mat", Close = "MainStats" },
                Text = { Text = $"ЗАКРЫТЬ", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, "Top");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.38 0.02", AnchorMax = $"0.58 0.11", OffsetMax = "0 0" },
                Button = { Color = "0.38 0.71 0.12 0.7", Material = "assets/icons/greyout.mat", Command = $"stats profile {player.userID} 0" },
                Text = { Text = $"МОЙ ПРОФИЛЬ", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, "Top");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.1525 0.02", AnchorMax = $"0.2225 0.11", OffsetMax = "0 0" },
                Button = { Color = "0.46 0.44 0.42 1.00", Material = "assets/icons/greyout.mat", Command = DB.Count() > (page + 1) * 10 ? $"stats skip {page + 1}" : "" },
                Text = { Text = $">", Color = DB.Count() > (page + 1) * 10 ? "1 1 1 1" : "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, "Top");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.0825 0.02", AnchorMax = $"0.1525 0.11", OffsetMax = "0 0" },
                Button = { Color = "0.3294118 0.3294118 0.3294118 1", Material = "assets/icons/greyout.mat", Command = "" },
                Text = { Text = $"{page + 1}", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, "Top");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.0125 0.02", AnchorMax = $"0.0825 0.11", OffsetMax = "0 0" },
                Button = { Color = "0.46 0.44 0.42 0.77", Material = "assets/icons/greyout.mat", Command = page >= 1 ? $"stats skip {page - 1}" : "" },
                Text = { Text = $"<", Color = page >= 1 ? "1 1 1 1" : "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, "Top");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 0.22", OffsetMax = "0 0" },
                Image = { Color = "0.36 0.34 0.32 0.75", Material = "assets/icons/greyout.mat", }
            }, Layer, "InfoTop");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.01 0.01", AnchorMax = $"0.99 0.99", OffsetMax = "0 0" },
                Text = { Text = "Очки даются:\nУбийство +100, добыча руды +7, разрушение бочки +2, уничтожение танка +750\nОчки отнимаются:\nСмерть и самоубийство -25\nНаграды выдаются после вайпа на сервере!", Color = "1 1 1 0.8", Align = TextAnchor.MiddleLeft, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, "InfoTop");

            float width1 = 0.785f, height1 = 0.063f, startxBox1 = 0.01f, startyBox1 = 0.95f - height1, xmin1 = startxBox1, ymin1 = startyBox1;
            if (page == 0)
            {
                for (int x = 0; x < DB.Take(10).Count(); x++)
                {
                    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = $"{xmin1} {ymin1}", AnchorMax = $"{xmin1 + width1} {ymin1 + height1 * 1}", OffsetMin = "2 1", OffsetMax = "-2 -1" },
                        Image = { Color = "0 0 0 0" }
                     }, Layer, "PlayerTop");

                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = $"0.46 0", AnchorMax = $"0.75 1", OffsetMax = "0 0" },
                        Text = { Text = $"{config.Bonus.ElementAt(x)}", Color = "1 1 1 0.8", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-regular.ttf" }
                    }, "PlayerTop");

                    xmin1 += width1;
                    if (xmin1 + width1 >= 1)
                    {
                        xmin1 = startxBox1;
                        ymin1 -= height1;
                    }
                }
            }            

            float width = 0.98f, height = 0.063f, startxBox = 0.01f, startyBox = 0.95f - height, xmin = startxBox, ymin = startyBox, z = 0;
            var items = from item in DB orderby item.Value.Points descending select item;
            foreach (var check in items.Skip(page * 10).Take(10))
            {
                z++;
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{xmin} {ymin}", AnchorMax = $"{xmin + width} {ymin + height * 1}", OffsetMin = "2 1", OffsetMax = "-2 -1" },
                    Image = { Color = "0 0 0 0" }
                 }, Layer, "PlayerTop");

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                    Image = { Color = "1 1 1 0.1" }
                }, "PlayerTop");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.08 1", OffsetMax = "0 0" },
                    Text = { Text = $"{z + page * 10}", Color = "1 1 1 0.8", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-regular.ttf" }
                }, "PlayerTop");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0.115 0", AnchorMax = $"0.425 1", OffsetMax = "0 0" },
                    Text = { Text = $"{check.Value.DisplayName}", Color = "1 1 1 0.8", Align = TextAnchor.MiddleLeft, FontSize = 14, Font = "robotocondensed-regular.ttf" }
                }, "PlayerTop");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0.4 0", AnchorMax = $"0.575 1", OffsetMax = "0 0" },
                    Text = { Text = $"", Color = "1 1 1 0.8", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-regular.ttf" }
                }, "PlayerTop");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0.63 0", AnchorMax = $"1.1 1", OffsetMax = "0 0" },
                    Text = { Text = $"{check.Value.Points}", Color = "1 1 1 0.8", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-regular.ttf" }
                }, "PlayerTop");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0", Command = $"stats profile {check.Key} {z + page * 10}" },
                    Text = { Text = $"", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
                }, "PlayerTop");

                xmin += width;
                if (xmin + width >= 1)
                {
                    xmin = startxBox;
                    ymin -= height;
                }
            }

            CuiHelper.AddUi(player, container);
        }
        [ChatCommand("stats")]
        void cmdProfileUis(BasePlayer player)
        {
            ProfileUI(player, player.userID, 0);
        }

        void ProfileUI(BasePlayer player, ulong SteamID, int z)
        {
            CuiHelper.DestroyUi(player, "MainStats");
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0" }
            }, "Overlay", "MainStats");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.9", Material = "assets/icons/greyout.mat" }
            }, "MainStats");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.31 0.3", AnchorMax = "0.69 0.75", OffsetMax = "0 0" },
                Image = { Color = "0.36 0.34 0.32 0.75", Material = "assets/icons/greyout.mat" }
            }, "MainStats", Layer);

            var target = DB[SteamID];
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.01 0.9", AnchorMax = $"0.99 0.99", OffsetMax = "0 0" },
                Text = { Text = $"<b><size=20>{target.DisplayName.ToUpper()}</size></b>", Color = "1 1 1 0.8", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, Layer);

            /*container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.51 0.131", AnchorMax = $"0.518 0.777", OffsetMax = "0 0" },
                Image = { Color = "1 1 1 0.1" }
            }, Layer);*/

            if (SteamID == player.userID)
            {
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0.01 0.02", AnchorMax = $"0.258 0.11", OffsetMax = "0 0" },
                    Image = { Color = "0.46 0.44 0.42 1.00", Material = "assets/icons/greyout.mat", }
                }, Layer, "Balance");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                    Text = { Text = $"Ваш баланс: {target.Balance}", Color = "1 1 1 0.8", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
                }, "Balance");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.265 0.02", AnchorMax = $"0.415 0.11", OffsetMax = "0 0" },
                    Button = { Color = "0.46 0.44 0.42 1.00", Material = "assets/icons/greyout.mat", Command = "stats take" },
                    Text = { Text = $"ВЫВЕСТИ", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
                }, Layer);
            }

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.682 0.02", AnchorMax = $"0.83 0.11", OffsetMax = "0 0" },
                Button = { Color = "0.46 0.44 0.42 1.00", Material = "assets/icons/greyout.mat", Command = "stats back" },
                Text = { Text = $"НАЗАД", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.837 0.02", AnchorMax = $"0.987 0.11", OffsetMax = "0 0" },
                Button = { Color = "0.46 0.44 0.42 1.00", Material = "assets/icons/greyout.mat", Close = "MainStats" },
                Text = { Text = $"ЗАКРЫТЬ", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, Layer);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.015 0.41", AnchorMax = $"0.3 0.81", OffsetMax = "0 0" },
                Image = { Color = "1 1 1 0.1" }
            }, Layer, "Avatar");

            container.Add(new CuiElement
            {
                Parent = "Avatar",
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", SteamID.ToString()) },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "7 7", OffsetMax = "-7 -7" }
                }
            });

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.35 0.76", AnchorMax = $"0.95 0.81", OffsetMax = "0 0" },
                Image = { Color = "1 1 1 0.1" }
            }, Layer, "Place");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0.03 0", AnchorMax = $"0.95 1", OffsetMax = "0 0" },
                Text = { Text = $"МЕСТО В ТОПЕ:", Color = "1 1 1 0.8", Align = TextAnchor.MiddleLeft, FontSize = 10, Font = "robotocondensed-regular.ttf" }
            }, "Place");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.97 1", OffsetMax = "0 0" },
                Text = { Text = $"{z}", Color = "1 1 1 0.8", Align = TextAnchor.MiddleRight, FontSize = 10, Font = "robotocondensed-regular.ttf" }
            }, "Place");               
                      

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.35 0.7", AnchorMax = $"0.95 0.75", OffsetMax = "0 0" },
                Image = { Color = "1 1 1 0.1" }
            }, Layer, "Points");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0.03 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Text = { Text = $"ОЧКОВ:", Color = "1 1 1 0.8", Align = TextAnchor.MiddleLeft, FontSize = 10, Font = "robotocondensed-regular.ttf" }
            }, "Points");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.97 1", OffsetMax = "0 0" },
                Text = { Text = $"{target.Points}", Color = "1 1 1 0.8", Align = TextAnchor.MiddleRight, FontSize = 10, Font = "robotocondensed-regular.ttf" }
            }, "Points");            

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.35 0.64", AnchorMax = $"0.95 0.69", OffsetMax = "0 0" },
                Image = { Color = "1 1 1 0.1" }
            }, Layer, "Status");

            var status = target.IsConnected == true ? "ОНЛАЙН" : "ОФЛАЙН";
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0.03 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Text = { Text = $"СТАТУС:", Color = "1 1 1 0.8", Align = TextAnchor.MiddleLeft, FontSize = 10, Font = "robotocondensed-regular.ttf" }
            }, "Status");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.985 1", OffsetMax = "0 0" },
                Text = { Text = $"{status}", Color = "1 1 1 0.8", Align = TextAnchor.MiddleRight, FontSize = 10, Font = "robotocondensed-regular.ttf" }
            }, "Status");

            float width1 = 0.6075f, height1 = 0.06f, startxBox1 = 0.3465f, startyBox1 = 0.638f - height1, xmin1 = startxBox1, ymin1 = startyBox1;
            foreach (var check in target.Settings)
            {
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{xmin1} {ymin1}", AnchorMax = $"{xmin1 + width1} {ymin1 + height1 * 1}", OffsetMin = "2 2", OffsetMax = "-2 -2" },
                    Image = { Color = "1 1 1 0.1" }
                }, Layer, "Count");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0.03 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                    Text = { Text = $"{check.Key.Replace("Kill", "УБИЙСТВ").Replace("Death", "СМЕРТЕЙ").Replace("Time", "АКТИВНОСТЬ")}", Color = "1 1 1 0.8", Align = TextAnchor.MiddleLeft, FontSize = 10, Font = "robotocondensed-regular.ttf" }
                }, "Count");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.97 1", OffsetMax = "0 0" },
                    Text = { Text = $"{check.Value}", Color = "1 1 1 0.8", Align = TextAnchor.MiddleRight, FontSize = 10, Font = "robotocondensed-regular.ttf" }
                }, "Count");

                xmin1 += width1;
                if (xmin1 + width1 >= 0)
                {
                    xmin1 = startxBox1;
                    ymin1 -= height1;
                }
            }

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.35 0.405", AnchorMax = $"0.95 0.455", OffsetMax = "0 0" },
                Image = { Color = "1 1 1 0.1" }
            }, Layer, "KD");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0.03 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Text = { Text = $"К/Д", Color = "1 1 1 0.8", Align = TextAnchor.MiddleLeft, FontSize = 10, Font = "robotocondensed-regular.ttf" }
            }, "KD");

            var kd = target.Settings["Death"] == 0 ? target.Settings["Kill"] : (float)Math.Round(((float)target.Settings["Kill"]) / target.Settings["Death"], 1);
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.97 1", OffsetMax = "0 0" },
                Text = { Text = $"{kd}", Color = "1 1 1 0.8", Align = TextAnchor.MiddleRight, FontSize = 10, Font = "robotocondensed-regular.ttf" }
            }, "KD");

            float width = 0.11f, height = 0.18f, startxBox = 0.005f, startyBox = 0.35f - height, xmin = startxBox, ymin = startyBox;
            foreach (var check in target.Res)
            {
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{xmin} {ymin}", AnchorMax = $"{xmin + width} {ymin + height * 1}", OffsetMin = "2 2", OffsetMax = "-2 -2" },
                    Image = { Color = "1 1 1 0.1" }
                }, Layer, "Images");

                container.Add(new CuiElement
                {
                    Parent = "Images",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", check.Key) },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" }
                    }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.97 1", OffsetMax = "0 0" },
                    Text = { Text = $"{check.Value}", Color = "1 1 1 0.8", Align = TextAnchor.LowerRight, FontSize = 14, Font = "robotocondensed-regular.ttf" }
                }, "Images");

                xmin += width;
                if (xmin + width >= 1)
                {
                    xmin = startxBox;
                    ymin -= height;
                }
            }

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Подгрузка аватарок
        void SteamAvatarAdd(string userid)
        {
            string url = "http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=B23DC0D84302CF828713C73F35A30006&" + "steamids=" + userid;
            webrequest.Enqueue(url, null, (code, response) =>
            {
                if (code == 200)
                {
                    string Avatar = (string)JObject.Parse(response)["response"]?["players"]?[0]?["avatarfull"];
                    ImageLibrary.Call("AddImage", Avatar, userid);
                }
            }, this);
        }
        #endregion
    }
}