using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using WebSocketSharp;

namespace Oxide.Plugins
{
    [Info("GameStoresRUST", "HOUGAN & Sstine & rostov114 # GAMESTORES", "0.3.9")]
    public class GameStoresRUST : RustPlugin
    {
        #region References

        [PluginReference] private Plugin ImageLibrary;

        #endregion

        #region Classes

        private static class Delays
        {
            //[JsonProperty("Игроки с активными запросами к АПИ")]
            public static List<ulong> ItemList = new List<ulong>();
            //[JsonProperty("Количество запросов за последнюю секунду")]
            public static int RequestPerSecond = 0;
            //[JsonProperty("Ограничение запросов в секунду")]
            public static int RequestPerSecondLimit = 20;

            public static bool CanRequest(BasePlayer player)
            {
                if (RequestPerSecond > RequestPerSecondLimit)
                {
                    return false;
                }

                if (ItemList.Contains(player.userID))
                {
                    player.ChatMessage(instance._(player, "WaitPreviousAction"));
                    return false;
                }
                return true;
            }

            public static void FinishRequest(BasePlayer player)
            {
                if (ItemList.Contains(player.userID))
                    ItemList.Remove(player.userID);
            }

            public static IEnumerator MakeRequest(BasePlayer player)
            {
                RequestPerSecond++;

                if (ItemList.Contains(player.userID)) yield return null;
                ItemList.Add(player.userID);

                yield return new WaitForSeconds(3f);
                if (ItemList.Contains(player.userID))
                    ItemList.Remove(player.userID);
            }
        }

        private class Stats
        {

        }

        private class Configuration
        {
            public class API
            {
                [JsonProperty("ИД магазина в сервисе")]
                public string ShopID = "UNDEFINED";
                [JsonProperty("ИД сервера в сервисе")]
                public string ServerID = "UNDEFINED";
                [JsonProperty("Секретный ключ (не распространяйте его)")]
                public string SecretKey = "UNDEFINED";
            }

            public class Interface
            {
                [JsonProperty("Включить изображение корзины")]
                public bool BucketEnable = true;
                [JsonProperty("Включить отображение названий предметов")]
                public bool TextShow = true;
                [JsonProperty("Ссылка на изображение корзины (BUCKET - стандартное изображение)")]
                public string BucketURL = "BUCKET";
                [JsonProperty("Количество предметов на строке")]
                public int ItemOnString = 7;
                [JsonProperty("Количество строк в интерфейсе")]
                public int StringAmount = 3;
                [JsonProperty("Сторона одного предмета")]
                public int ItemSide = 150;
                [JsonProperty("Отступ между предметами")]
                public int ItemMargin = 5;
				[JsonProperty("Настройки позиции изображение корзины")]
                public Position BucketPosition = new Position();

				public class Position
				{
					public string AnchorMin = "0 1";
					public string AnchorMax = "0 1";
					public string OffsetMin = "8 -40";
					public string OffsetMax = "43 -6";
				}
            }

            public class TOP
            {
                [JsonProperty("Отправлять данные топа игроков")]
                public bool UseTop = false;
            }

            [JsonProperty("Настройки API плагина")]
            public API APISettings = new API();
            [JsonProperty("Настройки интерфейса плагина")]
            public Interface InterfaceSettings = new Interface();
            [JsonProperty("Настройки статистики сервера")]
            public TOP TOPSettings = new TOP();
        }

        private class WItem
        {
            public string ID;
            public string Name;
            public int ItemID;
            public int Amount;
            public string ShortName;
            public string Command;
            public string ImageUrl;

            public bool Blocked;
            public double Block_Date = 0;
            public bool IsBlueprint;
            public bool IsCommand;
            public bool IsItem;

            public WItem(Dictionary<string, object> data, bool show = true)
            {
                try
                {
                    if (data.ContainsKey("id")) ID = data["id"].ToString();

                    if (data.ContainsKey("name")) Name = data["name"].ToString();
                    if (data.ContainsKey("item_id")) ItemID = System.Convert.ToInt32(data["item_id"]);
                    if (data.ContainsKey("amount")) Amount = System.Convert.ToInt32(data["amount"]);
                    if (data.ContainsKey("command")) Command = data["command"].ToString();
                    if (data.ContainsKey("img")) ImageUrl = data["img"].ToString();

                    if (data.ContainsKey("blocked")) Blocked = bool.Parse(data["blocked"].ToString());

                    if (data.ContainsKey("block_date") && data["block_date"] is int)
                    {
                        double.TryParse(data["block_date"].ToString(), out Block_Date);
                    }

                    if (data.ContainsKey("type"))
                    {
                        IsBlueprint = data["type"].ToString() == "bp";
                        IsCommand = data["type"].ToString() == "command";
                        IsItem = data["type"].ToString() == "item";
                    }

                    if (ItemID != 0)
                    {
                        var itemInfo = ItemManager.FindItemDefinition(ItemID);
                        if (itemInfo != null) ShortName = itemInfo.shortname;
                        else if (IDToShortName.ContainsKey(ItemID))
                        {
                            itemInfo = ItemManager.FindItemDefinition(IDToShortName[ItemID]);
                            if (itemInfo == null) return;

                            ShortName = itemInfo.shortname;
                        }
                    }

                    var imageLibrary = instance.plugins.Find("ImageLibrary");
                    if (imageLibrary != null)
                    {
                        if (ItemID == 0)
                        {
                            if ((bool)imageLibrary.Call("HasImage", $"IconGS.{ID}"))
                            {
                                string probablyId = (string)imageLibrary.Call("GetImage", $"IconGS.{ID}");
                                if (!probablyId.IsNullOrEmpty() && probablyId != instance.NoImageID && probablyId != instance.LoadingImageID)
                                    ImageUrl = probablyId;
                                return;
                            }

                            if (!ImageUrl.IsNullOrEmpty())
                            {
                                imageLibrary.Call("AddImage", ImageUrl.Replace("https", "http"), $"IconGS.{ID}");
                            }
                        }
                        else
                        {
                            string probablyId = (string)imageLibrary.Call("GetImage", ShortName);
                            if (!probablyId.IsNullOrEmpty() && probablyId != instance.NoImageID && probablyId != instance.LoadingImageID)
                                ImageUrl = probablyId;
                        }
                    }
                }
                catch (NullReferenceException e)
                {
                    Interface.Oxide.LogError(JsonConvert.SerializeObject(data));
                }
            }
        }

        #endregion

        #region Variables

        private static bool initialization = false;
        private static bool Initialized = false;
        private static bool SecureConnection = true;
        private static GameStoresRUST instance;
        private static Configuration Settings = new Configuration();
        private string ShopURL = "UNDEFINED";
        private int StartBalance = 0;
        public string NoImageID = "";
        public string LoadingImageID = "";
        private Coroutine LoadingCoroutine;
        private Dictionary<int, Dictionary<ulong, int>> ListTimeOutCommand = new Dictionary<int, Dictionary<ulong, int>>();
        private Dictionary<ulong, List<int>> playersBasketCache = new Dictionary<ulong, List<int>>();
        private HashSet<ulong> ListBannedCommandUserID = new HashSet<ulong>();
        private Timer TimerCheckInstant;
        private string MainApiLink = "https://gamestores.app/api/";
        private string ReserveApiLink = "https://gs.gamestores.app/api/";
        //private string BaseRequest => $"https://gamestores.app/api/?shop_id={Settings.APISettings.ShopID}&secret={Settings.APISettings.SecretKey}{(!Settings.APISettings.ServerID.IsNullOrEmpty() && Settings.APISettings.ServerID != "0" && Settings.APISettings.ServerID != "1" && Settings.APISettings.ServerID != "UNDEFINED" ? $"&server={Settings.APISettings.ServerID}" : "")}";
        private string BaseRequestParams => $"?shop_id={Settings.APISettings.ShopID}&secret={Settings.APISettings.SecretKey}{(!Settings.APISettings.ServerID.IsNullOrEmpty() && Settings.APISettings.ServerID != "0" && Settings.APISettings.ServerID != "1" && Settings.APISettings.ServerID != "UNDEFINED" ? $"&server={Settings.APISettings.ServerID}" : "")}";
        private string BaseRequest = "";
        #endregion

        #region Language
        private void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>() 
            {
                // Global
                { "days", "дн." },
                { "hour", "ч." },
                { "minutes", "мин." },
                { "seconds", "сек." },
                
                
                // Chat messages
                { "PluginNotInitialized", "Плагин магазина запущен некорректно" },
                { "PlayerFloodBlock", "Вы сделали слишком много запросов. Отдохните немного, скоро возможность выполнять действие к вам вернется." },
                { "ItemNotFound", "Предмет не найден, попробуйте перезапустить корзину" },
                { "UnexpectedError", "Непредвиденная ошибка со стороны сервера, просим прощения!" },
                { "WaitPreviousAction", "Пожалуйста, дождитесь <<окончания>> предыдущего действия!"},

                // UI
                { "BASKET", "КОРЗИНА СЕРВЕРА" },
                { "EXIT", "ВЫХОД" },
                { "BASKET.DESCRIPTION", "Это ваша корзина с покупками, вы можете забрать их в любой момент" },
                { "BASKET.EMPTY", "Ваша корзина пуста" },
                { "HELP", "ПОМОЩЬ" },
                { "REQUEST.PROCESSING", "Подождите, мы обрабатываем ваш запрос..." },
                { "BASKET.UNAVAILABLE", "Корзина временно недоступна, попробуйте позже" },
                { "BASKET.NO.AUTH", "Вы не авторизованы в магазине!\n" +
                                    "Ссылку на авторизацию вы можете найти в разделе 'ПОМОЩЬ'" },


                { "USER.MANUAL", "ИНСТРУКЦИЯ ПОЛЬЗОВАТЕЛЯ" },
                { "USER.MANUAL.DESCRIPTION", "Здесь вы можете забрать товары приобретенные у нас в магазине!\n" +
                                             "Пополнить счёт можно различными способами: электронные кошельки, карты и т.д.\n" +
                                             "Авторизация в магазине происходит при помощи вашего аккаунта STEAM" },

                { "USER.MANUAL.BALANCE", "\n<size=18>При первой авторизации вы получите в подарок <b>{0} рублей</b>!</size>" },
                { "TAKE.ITEM.BLOCKED", "Вы не можете забрать этот предмет из корзины!\n" +
                                       "До его разблокировки осталось: {0}" },
                { "TAKE.REQUEST.PROCESSING", "Подождите, ваш запрос обрабатывается!" },
                { "TAKE.LAST.REQUEST.PROCESSING", "Подождите, ваш предыдущий запрос ещё обрабатывается!" },
                { "TAKE.WAIT", "ПОДОЖДИТЕ" },
                { "TAKE.GIVE.ERROR", "ОШИБКА\nПОЛУЧЕНИЯ" },
                { "TAKE.GIVE.ERROR.NOTIFY", "Произошла непредвиденная ошибка, попробуйте позже!\n" +
                                            "Ваш предмет в безопасности, не переживайте!" },
                { "TAKE.GIVE.SUCCESS", "УСПЕШНО\nПОЛУЧЕНО" },
                { "TAKE.GIVE.FEET", "Вы успешно получили предмет  › <size=20>{0}</size> ‹\n" +
                                    "У вас недостаточно места в инвентаре, <b>предмет брошен</b> под ноги!" },
                { "TAKE.GIVE.INVENTORY", "Вы успешно получили предмет  › <size=20>{0}</size> ‹" },
                { "TAKE.GIVE.COMMAND", "Вы успешно получили предмет  › <size=20>{0}</size> ‹\n" +
                                       "Теперь вам доступны новые привилегии!" },
                { "TAKE.GIVE.BLUEPRINT.FEET", "Вы успешно получили рецепт предмета  › <size=20>{0}</size> ‹\n" +
                                              "У вас недостаточно места в инвентаре, <b>предмет брошен</b> под ноги!" },
                { "TAKE.GIVE.BLUEPRINT.INVENTORY", "Вы успешно получили рецепт предмета  › <size=20>{0}</size> ‹" },
                { "MANY.REQUESTS", "Вы делаете слишком много запросов к серверу!\n" +
                                   "Подождите <b>одну секунду</b>!" }
            }, this, "ru");

            lang.RegisterMessages(new Dictionary<string, string>() 
            {
                // Global
                { "days", "d." },
                { "hour", "h." },
                { "minutes", "m." },
                { "seconds", "s." },
                
                
                // Chat messages
                { "PluginNotInitialized", "The store plugin is not launched correctly" },
                { "PlayerFloodBlock", "You have made too many requests. Relax a little, soon the ability to perform the action will return to you." },
                { "ItemNotFound", "Item not found, try restarting your cart" },
                { "UnexpectedError", "Unexpected error from server side, sorry!" },
                { "WaitPreviousAction", "Please wait for the <<end>> of the previous step!"},

                // UI
                { "BASKET", "SERVER BASKET" },
                { "EXIT", "EXIT" },
                { "BASKET.DESCRIPTION", "This is your shopping cart, you can pick them up at any time" },
                { "BASKET.EMPTY", "Basket empty" },
                { "HELP", "HELP" },
                { "REQUEST.PROCESSING", "Please wait, we are processing your request ..." },
                { "BASKET.UNAVAILABLE", "Cart is temporarily unavailable, please try again later" },
                { "BASKET.NO.AUTH", "You are not authorized in the store!\n" +
                                    "You can find a link to authorization in the 'HELP' section" },


                { "USER.MANUAL", "USER'S MANUAL" },
                { "USER.MANUAL.DESCRIPTION", "Here you can pick up the products purchased from our store!\n" +
                                             "You can replenish your account in various ways: e-wallets, cards, etc.\n" +
                                             "Authorization in the store takes place using your STEAM account" },

                { "USER.MANUAL.BALANCE", "\n<size=18>At the first authorization, you will receive a gift of <b>{0} rubles</b>!</size>" },
                { "TAKE.ITEM.BLOCKED", "You cannot pick up this item from your cart!\n" +
                                       "Before its unlocking, there are: {0}" },
                { "TAKE.REQUEST.PROCESSING", "Please wait, your request is being processed!" },
                { "TAKE.LAST.REQUEST.PROCESSING", "Please wait, your previous request is still being processed!" },
                { "TAKE.WAIT", "WAIT" },
                { "TAKE.GIVE.ERROR", "ERROR\nRECEIVING" },
                { "TAKE.GIVE.ERROR.NOTIFY", "An unexpected error occured, please try again later!\n" +
                                            "Your item is safe, don't worry!" },
                { "TAKE.GIVE.SUCCESS", "SUCCESSFULLY\nRECEIVED" },
                { "TAKE.GIVE.FEET", "You have successfully obtained an item  › <size=20>{0}</size> ‹\n" +
                                    "You don't have enough space in your inventory, <b>the item was thrown</b> at your feet!" },
                { "TAKE.GIVE.INVENTORY", "You have successfully obtained an item  › <size=20>{0}</size> ‹" },
                { "TAKE.GIVE.COMMAND", "You have successfully obtained an item  › <size=20>{0}</size> ‹\n" +
                                       "New privileges are now available to you!" },
                { "TAKE.GIVE.BLUEPRINT.FEET", "You have successfully obtained the item blueprint  › <size=20>{0}</size> ‹\n" +
                                              "You don't have enough space in your inventory, <b>the item was thrown</b> at your feet!" },
                { "TAKE.GIVE.BLUEPRINT.INVENTORY", "You have successfully obtained the item blueprint  › <size=20>{0}</size> ‹" },
                { "MANY.REQUESTS", "You are making too many server requests!\n" +
                                   "Wait <b>one second</b>!" }
            }, this, "en");
        }

        private string _(BasePlayer player, string key, params object[] args)
        {
            return string.Format(lang.GetMessage(key, this, player?.UserIDString), args);
        }
        #endregion

        #region Interface

        private int TryImageCounter = 0;
        private void OnServerInitialized()
        {
            BaseRequest = MainApiLink + BaseRequestParams;
            if (Settings.APISettings.ServerID != "0" && Settings.APISettings.ServerID != "UNDEFINED")
            {
                TimerCheckInstant = timer.Repeat(60, 0, CheckInstant);
            } else
            {
                LogAction(null, $"Некорректный ServerID, автоматическая выдача команд невозможна");
                PrintError($"Incorrect ServerID, reciving auto commands (Products) disabled");
            }
            
            if (!ImageLibrary)
            {
                if (TryImageCounter < 3)
                {
                    LogAction(null, $"ImageLibrary is not initialized, try again in 1 sec.", true);
                    TryImageCounter++;

                    timer.Once(1, OnServerInitialized);
                    return;
                }
                else LogAction(null, $"Starting without ImageLibrary", true);
            }

            if (ImageLibrary)
            {
                NoImageID = (string)ImageLibrary.Call("GetImage", "NONE");
                LoadingImageID = (string)ImageLibrary.Call("GetImage", "LOADING");
            }

            if (Settings.InterfaceSettings.BucketURL.Contains("http") && plugins.Find("ImageLibrary") != null)
            {
                ImageLibrary.Call("AddImage", Settings.InterfaceSettings.BucketURL, "GameStoresRUSTBucket");
                LoadingCoroutine = ServerMgr.Instance.StartCoroutine(WaitForLoad());
            }
            else
            {
                //BasePlayer.activePlayerList.ForEach(OnPlayerInit);
                for (var i = 0; i < BasePlayer.activePlayerList.Count; i++)
                {
                    OnPlayerConnected(BasePlayer.activePlayerList[i]);
                }
            }

            instance = this;
            Settings.TOPSettings.UseTop = false; //Принудительное отключение топа игроков
            if (!Settings.TOPSettings.UseTop) Unsubscribe(nameof(OnEntityDeath));
            if (!Settings.TOPSettings.UseTop) Unsubscribe(nameof(OnPlayerDisconnected));

            if (Settings.APISettings.ShopID == "UNDEFINED" || Settings.APISettings.SecretKey == "UNDEFINED")
            {
                LogAction(null, $"Verify that plugin is installed correct! Some of API settings are 'UNDEFINED'", true, true);
                return;
            }

            timer.Once(2, FetchShopUrl);

            timer.Every(1, () => Delays.RequestPerSecond = 0);

            ListTimeOutCommand[0] = new Dictionary<ulong, int>();
            ListTimeOutCommand[1] = new Dictionary<ulong, int>();
            ListTimeOutCommand[2] = new Dictionary<ulong, int>();

            int totalTick = 0;
            int tick = 0;
            timer.Repeat(2, 0, () =>
            {
                if (totalTick == 30)
                {
                    totalTick = 0;
                    ListBannedCommandUserID.Clear();
                }
                ListTimeOutCommand[0].Clear();
                if (tick == 3 || tick == 6)
                {
                    ListTimeOutCommand[1].Clear();
                }

                if (tick == 6)
                {
                    tick = 0;
                    ListTimeOutCommand[2].Clear();
                }
                tick++;
                totalTick++;
            });
        }

        private IEnumerator WaitForLoad()
        {
            while (!(bool)ImageLibrary.Call("HasImage", "GameStoresRUSTBucket"))
            {
                PrintError($"Image of bucket is loading!");
                yield return new WaitForSeconds(1);
            }

            PrintWarning("Image of bucket loaded correct!");
            //BasePlayer.activePlayerList.ForEach(OnPlayerInit);
            for (var i = 0; i < BasePlayer.activePlayerList.Count; i++)
            {
                OnPlayerConnected(BasePlayer.activePlayerList[i]);
            }
            yield return 0;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                Settings = Config.ReadObject<Configuration>();
                if (Settings?.APISettings == null) LoadDefaultConfig();
            }
            catch
            {
                LogAction(null, $"Error reading config, creating one new config!", true, true);
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig()
        {
            Settings = new Configuration();
        }

        protected override void SaveConfig() => Config.WriteObject(Settings);

        private void Unload()
        {
            //if (Initialized) StatHandler.SendStats();

            if (LoadingCoroutine != null) ServerMgr.Instance.StopCoroutine(LoadingCoroutine);
            //BasePlayer.activePlayerList.ForEach(p => CuiHelper.DestroyUi(p, IconLayer));
            //BasePlayer.activePlayerList.ForEach(p => CuiHelper.DestroyUi(p, StoreLayer));
            foreach (var pl in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(pl, IconLayer);
                CuiHelper.DestroyUi(pl, StoreLayer);
                OnPlayerConnected(pl);
            }
        }

        #endregion

        #region Hooks

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            StatHandler.AddStat(new StatHandler.TimeStat(player));
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerConnected(player));
                return;
            }

            if (Settings.InterfaceSettings.BucketEnable)
                InitializeIcon(player);
        }

        #endregion

        #region Stats

        private static class StatHandler
        {
            internal class Stat
            {
                [JsonProperty("dataType")]
                public string DataType;
            }
            internal class KillStat : Stat
            {
                [JsonProperty("player_id")]
                public string PlayerUserId;
                [JsonProperty("victim_id")]
                public string VictimUserID;
                [JsonProperty("type")]
                public string Type;
                [JsonProperty("time")]
                public string Time;
            }
            internal class TimeStat : Stat
            {
                [JsonProperty("player_id")]
                public string PlayerID;
                [JsonProperty("username")]
                public string PlayerName;
                [JsonProperty("played")]
                public string Played;
                [JsonProperty("time")]
                public string Time;

                public TimeStat(BasePlayer player)
                {
                    PlayerID = player.UserIDString;
                    PlayerName = player.displayName;
                    DataType = "leave";
                    Played = player.secondsConnected.ToString();
                    Time = CurrentTime().ToString();
                }
            }

            private static List<Stat> Stats = new List<Stat>();
            public static void AddStat(Stat stat)
            {
                Stats.Add(stat);
                if (Stats.Count > 10)
                {
                    SendStats();
                    Stats.Clear();
                }
            }

            public static void SendStats()
            {
                try
                {


                    if (!Initialized)
                    {
                        instance.LogAction(null, $"Sending stats error! Plugin not initialized", true, true);
                        return;
                    }
                    if (Stats.Count == 0)
                    {
                        instance.LogAction(null, $"Nothing to send", true);
                        return;
                    }

                    var obj = JsonConvert.SerializeObject(Stats);
                    RequestPost($"&method=topData&data={obj}", (i, s) => {
                        if (i != 200)
                            return;

                        Dictionary<string, object> response = JsonConvert.DeserializeObject<Dictionary<string, object>>(s, new KeyValuesConverter());
                        if (response.ContainsKey("result") && response["result"].ToString() == "success")
                        {
                            instance.LogAction(null, $"GameStores sent stats successful!", true);
                            Stats.Clear();
                        }
                        else
                        {
                            instance.LogAction(null, $"Sending stats error!", true, true);
                        }
                    });


                }
                catch
                {
                    // ignored 
                }
            }
        }

        [ConsoleCommand("sendtop")]
        private void CmdSendTop(ConsoleSystem.Arg args)
        {
            if (args.Player() != null && !args.Player().IsAdmin) return;

            StatHandler.SendStats();
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null || info.Initiator == null)
                return;
            if (Settings.TOPSettings.UseTop)
            {
                BaseEntity initiator = info.Initiator;

                if (entity as BasePlayer == null && initiator as BasePlayer == null)
                    return;
                StatHandler.KillStat stat = new StatHandler.KillStat();

                if (initiator as BasePlayer != null) stat.PlayerUserId = initiator.ToPlayer().UserIDString;
                else if (initiator.PrefabName.Contains("agents")) stat.PlayerUserId = "1";
                else return;

                if (entity as BasePlayer != null)
                {
                    stat.VictimUserID = entity.ToPlayer().UserIDString;
                    stat.Type = entity.ToPlayer().IsSleeping() ? "sleeper" : "kill";
                }
                else if (entity.PrefabName.Contains("agents"))
                {
                    stat.VictimUserID = "1";
                    stat.Type = "kill";
                }
                else
                {
                    return;
                }

                stat.DataType = "death";

                stat.Time = CurrentTime().ToString();
                StatHandler.AddStat(stat);
            }
        }

        #endregion

        #region Commands

        [ChatCommand("store")]
        private void CmdChatStore(BasePlayer player, string command, string[] args)
        {
            if (player == null || player.Connection == null || player.IsSleeping())
            {
                return;
            }                                                                                  
            if (!Initialized)
            {
                errorsReq++;
                player.ChatMessage(_(player, "PluginNotInitialized"));
                return;

            }

            if (args.Length == 1 && args[0].ToLower() == "hide")
            {
                CuiHelper.DestroyUi(player, IconLayer);
            }
            else
            {
                if (!Delays.CanRequest(player)) return;
                InitializeStore(player, 0, true);
            }
        }

        [ConsoleCommand("UI_GameStoresRUST")]
        private void CmdConsoleCommand(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player == null) return;

            if (ListBannedCommandUserID.Contains(player.userID))
            {
                player.ChatMessage(_(player, "PlayerFloodBlock"));
                return;
            }

            if (ListTimeOutCommand[0].ContainsKey(player.userID) == false)
                ListTimeOutCommand[0][player.userID] = 1;
            else
                ListTimeOutCommand[0][player.userID] = ListTimeOutCommand[0][player.userID] + 1;

            if (ListTimeOutCommand[1].ContainsKey(player.userID) == false)
                ListTimeOutCommand[1][player.userID] = 1;
            else
                ListTimeOutCommand[1][player.userID] = ListTimeOutCommand[1][player.userID] + 1;

            if (ListTimeOutCommand[2].ContainsKey(player.userID) == false)
                ListTimeOutCommand[2][player.userID] = 1;
            else
                ListTimeOutCommand[2][player.userID] = ListTimeOutCommand[2][player.userID] + 1;

            if (ListTimeOutCommand[0][player.userID] >= 10 || ListTimeOutCommand[1][player.userID] >= 20 || ListTimeOutCommand[2][player.userID] >= 30)
            {
                this.ListBannedCommandUserID.Add(player.userID);
            }

            if (!args.HasArgs(1))
            {
                player.SendConsoleCommand("chat.say /store");
                return;
            }

            switch (args.Args[0].ToLower())
            {
                case "page":
                    {
                        int page = 0;
                        if (!args.HasArgs(2) || !int.TryParse(args.Args[1], out page)) return;
                        InitializeStore(player, page, false);
                        break;
                    }
                case "help":
                    {
                        string helpLayer = StoreLayer + ".Help";
                        CuiHelper.DestroyUi(player, helpLayer);
                        CuiHelper.DestroyUi(player, StoreLayer);

                        CuiElementContainer container = new CuiElementContainer();
                        container.Add(new CuiPanel
                        {
                            CursorEnabled = true,
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                            Image = { Color = "0 0 0 0.9", Material = "assets/content/ui/uibackgroundblur.mat" },
                        }, "Overlay", helpLayer);

                        container.Add(new CuiLabel
                        {
                            RectTransform = { AnchorMin = "0 0.6", AnchorMax = "1 0.78", OffsetMax = "0 0" },
                            Text = { Text = _(player, "USER.MANUAL"), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 34 }
                        }, helpLayer);

                        container.Add(new CuiLabel
                        {
                            RectTransform = { AnchorMin = "0 0.3", AnchorMax = "1 0.72", OffsetMax = "0 0" },
                            Text = { Text = _(player, "USER.MANUAL.DESCRIPTION"), 
                            Font = "robotocondensed-regular.ttf", FontSize = 22, Align = TextAnchor.MiddleCenter
                        }
                        }, helpLayer);

                        string addText = StartBalance > 0 ? _(player, "USER.MANUAL.BALANCE", StartBalance) : "";
                        container.Add(new CuiLabel
                        {
                            RectTransform = { AnchorMin = "0 0.1", AnchorMax = "1 0.48", OffsetMax = "0 0" },
                            Text = { Text = $"{addText}\n{ShopURL.ToUpper()}", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 32}
                        }, helpLayer);
                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                            Button = { Color = "0 0 0 0", Close = helpLayer, Command = "chat.say /store" },
                            Text = { Text = "" }
                        }, helpLayer);

                        CuiHelper.AddUi(player, container);
                        break;
                    }
                case "take":
                    {
                        if (!args.HasArgs(3)) return;

                        int index = 0, id = 0;
                        if (!int.TryParse(args.Args[1], out index) || !int.TryParse(args.Args[2], out id)) return;

                        if (!playersBasketCache.ContainsKey(player.userID))
                        {
                            player.ChatMessage(_(player, "ItemNotFound"));
                            return;
                        } 
                        else if (!playersBasketCache[player.userID].Contains(id))
                        {
                            player.ChatMessage(_(player, "ItemNotFound"));
                            return;
                        }

                        if (args.HasArgs(5) && args.Args[3].ToLower() == "blocked")
                        {
                            double left = 0;
                            if (!double.TryParse(args.Args[4], out left)) return;

                            TimeSpan span = TimeSpan.FromSeconds(left);

                            string text = "";
                            if (span.Days >= 1)
                                text += $"{span.Days} " + _(player, "days") + " ";
                            if (span.Hours >= 1)
                                text += $"{span.Hours} " + _(player, "hours") + " ";
                            if (span.Minutes >= 1)
                                text += $"{span.Minutes} " + _(player, "minutes") + " ";
                            if (span.Seconds >= 1)
                                text += $"{span.Seconds} " + _(player, "seconds");

                            ShowNotify(player, _(player, "TAKE.ITEM.BLOCKED", text));
                            return;
                        }

                        ShowNotify(player, _(player, "TAKE.REQUEST.PROCESSING"));
                        CuiElementContainer container = new CuiElementContainer();

                        if (!Delays.CanRequest(player))
                        {
                            ShowNotify(player, _(player, "TAKE.LAST.REQUEST.PROCESSING"));
                            container.Add(new CuiButton
                            {
                                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                                Button = { Color = "1 1 1 0.2", Close = StoreLayer + ".BlockPanel." + index + ".Open", Command = $"UI_GameStoresRUST take {index} {id}" },
                                Text = { Text = _(player, "TAKE.WAIT"), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", Color = "1 1 1 0.4", FontSize = 24 }
                            }, StoreLayer + ".BlockPanel." + index, StoreLayer + ".BlockPanel." + index + ".Open");

                            CuiHelper.AddUi(player, container);
                            return;
                        }

                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                            Button = { FadeIn = 1f, Color = "1 1 1 0.2", Close = StoreLayer + ".BlockPanel." + index + ".Open", Command = $"UI_GameStoresRUST take {index} {id}" },
                            Text = { Text = _(player, "TAKE.WAIT"), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", Color = "1 1 1 0.4", FontSize = 24 }
                        }, StoreLayer + ".BlockPanel." + index, StoreLayer + ".BlockPanel." + index + ".Open");
                        CuiHelper.AddUi(player, container);

                        LogAction(null, $"---------------------------------");
                        LogAction(player, $"Запрос на получение предмета: {id}");
                        Request($"&item=true&steam_id={player.UserIDString}&id={id}", (i, s) =>
                        {
                            switch (i)
                            {
                                case 0:
                                    LogAction(player, $"API не ответило на запрос: {id}");
                                    PrintError("Api does not responded to a request");
                                    if (player != null)
                                    {
                                        player.ChatMessage(_(player, "UnexpectedError"));
                                        CuiHelper.DestroyUi(player, StoreLayer);
                                    }
                                    break;
                                case 200:
                                    Dictionary<string, object> response = JsonConvert.DeserializeObject<Dictionary<string, object>>(s, new KeyValuesConverter());
                                    if (!response.ContainsKey("data"))
                                    {
                                        LogAction(player, $"Ошибка получения товара, отсутствует Data [{id}]");

                                        container.Clear();
                                        CuiHelper.DestroyUi(player, StoreLayer + ".BlockPanel." + index + ".Open");
                                        container.Add(new CuiButton
                                        {
                                            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                                            Button = { FadeIn = 1f, Color = "1 0.5 0.5 0.2", Close = StoreLayer + ".BlockPanel." + index + ".Open", Command = $"UI_GameStoresRUST take {index} {id}" },
                                            Text = { Text = _(player, "TAKE.GIVE.ERROR"), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", Color = "1 0.7 0.7 1", FontSize = 22 }
                                        }, StoreLayer + ".BlockPanel." + index, StoreLayer + ".BlockPanel." + index + ".Open");
                                        ShowNotify(player, _(player, "TAKE.GIVE.ERROR.NOTIFY"));
                                        CuiHelper.AddUi(player, container);
                                        return;
                                    }

                                    LogAction(player, $"Товар отмечен полученным [{id}]");
                                    Request($"&gived=true&id={id}", (code, newResponse) =>
                                    {
                                        if (code != 200 || JsonConvert.DeserializeObject<JObject>(newResponse)["result"].ToString() != "success")
                                        {
                                            container.Clear();
                                            CuiHelper.DestroyUi(player, StoreLayer + ".BlockPanel." + index + ".Open");
                                            container.Add(new CuiButton
                                            {
                                                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                                                Button = { FadeIn = 1f, Color = "1 0.5 0.5 0.2", Close = StoreLayer + ".BlockPanel." + index + ".Open", Command = $"UI_GameStoresRUST take {index} {id}" },
                                                Text = { Text = _(player, "TAKE.GIVE.ERROR"), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", Color = "1 0.7 0.7 1", FontSize = 22 }
                                            }, StoreLayer + ".BlockPanel." + index, StoreLayer + ".BlockPanel." + index + ".Open");
                                            ShowNotify(player, _(player, "TAKE.GIVE.ERROR.NOTIFY"));
                                            CuiHelper.AddUi(player, container);
                                            return;
                                        }
                                        ProcessTake(player, response["data"] as Dictionary<string, object>);
                                    }, player);

                                    container.Clear();
                                    CuiHelper.DestroyUi(player, StoreLayer + ".BlockPanel." + index + ".Open");
                                    container.Add(new CuiButton
                                    {
                                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                                        Button = { FadeIn = 1f, Color = "0.5 1 0.5 0.2" },
                                        Text = { Text = _(player, "TAKE.GIVE.SUCCESS"), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", Color = "0.7 1 0.7 1", FontSize = 22 }
                                    }, StoreLayer + ".BlockPanel." + index, StoreLayer + ".BlockPanel." + index + ".Open");

                                    if (playersBasketCache.ContainsKey(player.userID) && playersBasketCache[player.userID].Contains(id))
                                        playersBasketCache[player.userID].RemoveAt(playersBasketCache[player.userID].IndexOf(id));

                                    CuiHelper.AddUi(player, container);
                                    break;
                                case 404:
                                    LogAction(player, $"Сайт не ответил на запрос: {id}");
                                    player.ChatMessage(_(player, "UnexpectedError"));
                                    CuiHelper.DestroyUi(player, StoreLayer);
                                    LogAction(null, "Response code: 404, please check your configurations", true);
                                    break;
                            }
                        }, player);
                        break;
                    }
            }
        }

        #endregion

        #region Interface

        private static string StoreLayer = "UI_GameStoresRUST_Store";
        private void InitializeStore(BasePlayer player, int page, bool first = true)
        {
            CuiElementContainer container = new CuiElementContainer();

            if (first)
            {
                CuiHelper.DestroyUi(player, StoreLayer);
                container.Add(new CuiPanel
                {
                    CursorEnabled = true,
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Image = { Color = "0 0 0 0.8", Material = "assets/content/ui/uibackgroundblur.mat" },
                }, "Overlay", StoreLayer);

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.3 0.9", AnchorMax = "0.7 1", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0" },
                    Text = { Text = _(player, "BASKET"), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 32, Color = "1 1 1 0.6" }
                }, StoreLayer, StoreLayer + ".ITT");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.8 0.9", AnchorMax = "0.935 1", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0", Close = StoreLayer, Command = "closemenu" },
                    Text = { Text = _(player, "EXIT"), Align = TextAnchor.MiddleRight, Font = "robotocondensed-regular.ttf", FontSize = 28 }
                }, StoreLayer);
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 -0", OffsetMax = "0 20" },
                    Text = { Text = _(player, "BASKET.DESCRIPTION"), Align = TextAnchor.UpperCenter, Font = "robotocondensed-regular.ttf", Color = "1 1 1 0.4" }
                }, StoreLayer + ".ITT");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.065 0.9", AnchorMax = "0.2 1", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0", Command = "UI_GameStoresRUST help" },
                    Text = { Text = _(player, "HELP"), Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf", FontSize = 28 }
                }, StoreLayer);
            }

            CuiHelper.DestroyUi(player, StoreLayer + ".BlockPanel");
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.9", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0" }
            }, StoreLayer, StoreLayer + ".BlockPanel");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1.1", OffsetMax = "0 0" },
                Text = { Text = _(player, "REQUEST.PROCESSING"), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 34 }
            }, StoreLayer + ".BlockPanel", StoreLayer + ".BlockPanel.Text");

            CuiHelper.AddUi(player, container);

            Request($"&method=basket&steam_id={player.UserIDString}", (code, response) =>
            {
               
                switch (code)
                {
                    default:
                        {
                            CuiHelper.DestroyUi(player, StoreLayer + ".BlockPanel.Text");
                            CuiElementContainer secondContainer = new CuiElementContainer();
                            secondContainer.Add(new CuiLabel
                            {
                                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                                Text = { Text = _(player, "BASKET.UNAVAILABLE"), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 38 }
                            }, StoreLayer + ".BlockPanel", StoreLayer + ".BlockPanel.Text");
                            CuiHelper.AddUi(player, secondContainer);
                            break;
                        }
                    case 200:
                        {
                            var firstInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(response, new KeyValuesConverter());
                            if (firstInfo.ContainsKey("result"))
                            {
                                if (firstInfo["result"].ToString() == "fail")
                                {
                                    if (firstInfo["code"].ToString() == "104")
                                    {
                                        CuiHelper.DestroyUi(player, StoreLayer + ".BlockPanel.Text");
                                        CuiElementContainer secondContainer = new CuiElementContainer();
                                        secondContainer.Add(new CuiLabel
                                        {
                                            RectTransform = { AnchorMin = "0 0.1", AnchorMax = "1 1", OffsetMax = "0 0" },
                                            Text = { Text = _(player, "BASKET.EMPTY"), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 34 }
                                        }, StoreLayer + ".BlockPanel", StoreLayer + ".BlockPanel.Text");
                                        CuiHelper.AddUi(player, secondContainer);
                                    }
                                    if (firstInfo["code"].ToString() == "105")
                                    {
                                        CuiHelper.DestroyUi(player, StoreLayer + ".BlockPanel.Text");
                                        CuiElementContainer secondContainer = new CuiElementContainer();
                                        secondContainer.Add(new CuiLabel
                                        {
                                            RectTransform = { AnchorMin = "0 0.1", AnchorMax = "1 1", OffsetMax = "0 0" },
                                            Text = { Text = _(player, "BASKET.NO.AUTH"), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 34 }
                                        }, StoreLayer + ".BlockPanel", StoreLayer + ".BlockPanel.Text");
                                        CuiHelper.AddUi(player, secondContainer);
                                    }
                                }
                                else
                                {
                                    CuiElementContainer secondContainer = new CuiElementContainer();

                                    if (!(firstInfo["data"] is List<object>))
                                    {
                                        PrintError("Unkown error #1");
                                        CuiHelper.DestroyUi(player, StoreLayer);
                                        return;
                                    }
                                    List<object> data = firstInfo["data"] as List<object>;
                                    List<WItem> wItems = new List<WItem>();

                                    foreach (var check in data.Skip(page * 21).Take(21))
                                    {
                                        wItems.Add(new WItem(check as Dictionary<string, object>));

                                    }
                                    if (playersBasketCache.ContainsKey(player.userID))
                                        playersBasketCache[player.userID].Clear();

                                    foreach (Dictionary<string, object> product in data)
                                    {
                                        if (product.ContainsKey("id"))
                                        {
                                            if (!playersBasketCache.ContainsKey(player.userID))
                                                playersBasketCache.Add(player.userID, new List<int>());

                                            playersBasketCache[player.userID].Add(Convert.ToInt32(product["id"]));
                                        }
                                    }
                                    secondContainer.Add(new CuiLabel
                                    {
                                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.14", OffsetMax = "0 0" },
                                        Text = { Text = (page + 1).ToString(), Align = TextAnchor.MiddleCenter, FontSize = 34 }
                                    }, StoreLayer + ".BlockPanel");

                                    secondContainer.Add(new CuiButton
                                    {
                                        RectTransform = { AnchorMin = "0.4 0.14", AnchorMax = "0.4 0.14", OffsetMin = "-40 -125", OffsetMax = "125 40" },
                                        Button = { Color = "0 0 0 0", Command = page > 0 ? $"UI_GameStoresRUST page {page - 1}" : "" },
                                        Text = { Text = "<", Color = page > 0 ? "1 1 1 1" : "1 1 1 0.2", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 80 }
                                    }, StoreLayer + ".BlockPanel");

                                    secondContainer.Add(new CuiButton
                                    {
                                        RectTransform = { AnchorMin = "0.6 0.14", AnchorMax = "0.6 0.14", OffsetMin = "-125 -125", OffsetMax = "40 40" },
                                        Button = { Color = "0 0 0 0", Command = (page + 1) * 21 < data.Count ? $"UI_GameStoresRUST page {page + 1}" : "" },
                                        Text = { Text = ">", Color = (page + 1) * 21 < data.Count ? "1 1 1 1" : "1 1 1 0.2", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 80 }
                                    }, StoreLayer + ".BlockPanel");


                                    double xSwitch = 0;
                                    double ySwitch = 0;
                                    for (int i = 0; i < Settings.InterfaceSettings.ItemOnString * Settings.InterfaceSettings.StringAmount; i++)
                                    {
                                        UI_RecountPosition(ref xSwitch, ref ySwitch, i, Settings.InterfaceSettings.ItemOnString * Settings.InterfaceSettings.StringAmount);

                                        secondContainer.Add(new CuiButton
                                        {
                                            RectTransform = { AnchorMin = "0.495 0.55", AnchorMax = "0.495 0.55", OffsetMin = $"{xSwitch} {ySwitch - Settings.InterfaceSettings.ItemSide}", OffsetMax = $"{xSwitch + Settings.InterfaceSettings.ItemSide} {ySwitch}" },
                                            Button = { Color = "1 1 1 0.2", Command = $"" },
                                            Text = { Text = "" }
                                        }, StoreLayer + ".BlockPanel", StoreLayer + $".BlockPanel.{i}");
                                    }

                                    /*if (player.IsAdmin) 
                                    {
                                        LogToFile("Test", "123 - " + JsonConvert.SerializeObject(wItems), this);  
                                    } */
                                    foreach (var check in wItems.Select((i, t) => new { A = i, B = t }))
                                    {
                                        if (check.A.IsBlueprint)
                                        {
                                            if (plugins.Find("ImageLibrary") != null)
                                            {
                                                secondContainer.Add(new CuiElement
                                                {
                                                    Parent = StoreLayer + ".BlockPanel." + check.B,
                                                    Components =
                                                {
                                                    new CuiRawImageComponent { Png = (string) plugins.Find("ImageLibrary").Call("GetImage", "blueprintbase") },
                                                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" },
                                                }
                                                });
                                            }
                                            else
                                            {
                                                secondContainer.Add(new CuiElement
                                                {
                                                    Parent = StoreLayer + ".BlockPanel." + check.B,
                                                    Components =
                                                {
                                                    new CuiRawImageComponent { Url = "https://gamestores.app/img/games/rust/blueprintbase.png" },
                                                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" },
                                                }
                                                });
                                            }
                                        }
                                        if (!check.A.ImageUrl.Contains("http"))
                                        {
                                            secondContainer.Add(new CuiElement
                                            {
                                                Parent = StoreLayer + ".BlockPanel." + check.B,
                                                Components =
                                            {
                                                new CuiRawImageComponent { Png = check.A.ImageUrl },
                                                new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" },
                                            }
                                            });
                                        }
                                        else
                                        {
                                            secondContainer.Add(new CuiElement
                                            {
                                                Parent = StoreLayer + ".BlockPanel." + check.B,
                                                Components =
                                            {
                                                new CuiRawImageComponent { Url = check.A.ImageUrl },
                                                new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" },
                                            }
                                            });
                                        }

                                        if (check.A.Amount > 1)
                                        {
                                            secondContainer.Add(new CuiLabel
                                            {
                                                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 5", OffsetMax = "-5 0" },
                                                Text = { Text = "x" + check.A.Amount, Align = TextAnchor.LowerRight, Font = "robotocondensed-regular.ttf", FontSize = 24 }
                                            }, StoreLayer + ".BlockPanel." + check.B);
                                        }

                                        if (check.A.Blocked)
                                        {
                                            double left = check.A.Block_Date - CurrentTime();

                                            secondContainer.Add(new CuiButton
                                            {
                                                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                                                Button = { Color = "1 0.5 0.5 0.2", Command = $"UI_GameStoresRUST take {check.B} {check.A.ID} blocked {left}" },
                                                Text = { Text = "", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 38 }
                                            }, StoreLayer + ".BlockPanel." + check.B, StoreLayer + ".BlockPanel." + check.B + ".Open");
                                        }
                                        else
                                        {
                                            secondContainer.Add(new CuiButton
                                            {
                                                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                                                Button = { Color = "0 0 0 0", Close = StoreLayer + ".BlockPanel." + check.B + ".Open", Command = $"UI_GameStoresRUST take {check.B} {check.A.ID}" },
                                                Text = { Text = "" }
                                            }, StoreLayer + ".BlockPanel." + check.B, StoreLayer + ".BlockPanel." + check.B + ".Open");
                                        }

                                        /*secondContainer.Add(new CuiLabel
                                        {
                                            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 -22.5", OffsetMax = "0 0" },
                                            Text = { Text = ((check.A.Name.IndexOf("lang:") == 0) ? _(player, check.A.Name.Substring(5)) : check.A.Name) , Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 16, Color = "1 1 1 0.8" }
                                        }, StoreLayer + ".BlockPanel." + check.B);*/

                                        secondContainer.Add(new CuiLabel
                                        {
                                            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 -22.5", OffsetMax = "0 0" },
                                            Text = { Text = check.A.Name, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 16, Color = "1 1 1 0.8" }
                                        }, StoreLayer + ".BlockPanel." + check.B);
                                    }

                                    CuiHelper.AddUi(player, secondContainer);
                                    CuiHelper.DestroyUi(player, StoreLayer + ".BlockPanel.Text");
                                }
                            }
                            break;
                        }
                }
            }, player);
        }

        private string IconLayer = "UI_GameStoresRUST_Bucket";
        private void InitializeIcon(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, IconLayer);
            CuiElementContainer container = new CuiElementContainer();
            if (Settings.InterfaceSettings.BucketURL.Contains("http"))
            {
                if (ImageLibrary)
                {
                    container.Add(new CuiElement
                    {
                        Parent = "Overlay",
                        Name = IconLayer,
                        Components =
                        {
                            new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "GameStoresRUSTBucket") },
                            new CuiRectTransformComponent { AnchorMin = Settings.InterfaceSettings.BucketPosition.AnchorMin, AnchorMax = Settings.InterfaceSettings.BucketPosition.AnchorMax, OffsetMin = Settings.InterfaceSettings.BucketPosition.OffsetMin, OffsetMax = Settings.InterfaceSettings.BucketPosition.OffsetMax }
                        }
                    });

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                        Button = { Color = "0 0 0 0", Command = "chat.say /store" },
                        Text = { Text = "" }
                    }, IconLayer);
                }
                else
                {
                    container.Add(new CuiElement
                    {
                        Parent = "Overlay",
                        Name = IconLayer,
                        Components =
                        {
                            new CuiRawImageComponent { Url = Settings.InterfaceSettings.BucketURL },
                            new CuiRectTransformComponent { AnchorMin = Settings.InterfaceSettings.BucketPosition.AnchorMin, AnchorMax = Settings.InterfaceSettings.BucketPosition.AnchorMax, OffsetMin = Settings.InterfaceSettings.BucketPosition.OffsetMin, OffsetMax = Settings.InterfaceSettings.BucketPosition.OffsetMax }
                        }
                    });

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                        Button = { Color = "0 0 0 0", Command = "chat.say /store" },
                        Text = { Text = "" }
                    }, IconLayer);
                }
            }
            else
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = Settings.InterfaceSettings.BucketPosition.AnchorMin, AnchorMax = Settings.InterfaceSettings.BucketPosition.AnchorMax, OffsetMin = Settings.InterfaceSettings.BucketPosition.OffsetMin, OffsetMax = Settings.InterfaceSettings.BucketPosition.OffsetMax },
                    Button = { Color = "1 1 1 0.6", Sprite = "assets/icons/open.png", Command = "chat.say /store" },
                    Text = { Text = "" }
                }, "Overlay", IconLayer);
            }

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Utils

        private void ShowNotify(BasePlayer player, string text)
        {
            CuiHelper.DestroyUi(player, StoreLayer + ".Notify");
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.80", AnchorMax = "1 0.90", OffsetMax = "0 0" },
                Text = { FadeIn = 1f, Text = text, Align = TextAnchor.UpperCenter, Font = "robotocondensed-regular.ttf", FontSize = 16 }
            }, StoreLayer, StoreLayer + ".Notify");

            CuiHelper.AddUi(player, container);
        }

        private void ProcessTake(BasePlayer player, Dictionary<string, object> obj)
        {
            //foreach (var check in obj)
            //   PrintError(check.Key + " -> " + check.Value); 
            LogAction(player, $"Начало обработки товара");
            WItem itemInfo = new WItem(obj);
            try
            {
                if (itemInfo.IsItem)
                {
                    LogAction(player, $"Попытка получения предмета: {itemInfo.ShortName} [{itemInfo.Amount}]");
                    var info = ItemManager.FindItemDefinition(itemInfo.ShortName);
                    if (info == null) return;

                    var item = ItemManager.Create(info, itemInfo.Amount);
                    if (!player.inventory.GiveItem(item))
                    {
                        LogAction(player, $"У игрока не было места для получения предмета, предмет выброшен {itemInfo.ShortName} [{itemInfo.Amount} {player.transform.position}]");

                        item.Drop(player.transform.position, Vector3.down * 3);
                        ShowNotify(player, _(player, "TAKE.GIVE.FEET", info.displayName.english));
                    }
                    else
                    {
                        LogAction(player, $"Предмет выдан игроку в инвентарь");
                        ShowNotify(player, _(player, "TAKE.GIVE.INVENTORY", info.displayName.english));
                    }
                }

                if (itemInfo.IsCommand)
                {
                    LogAction(player, $"Попытка получения команды");

                    string command = itemInfo.Command.Replace("\n", "|").Replace("%steamid%", player.UserIDString, StringComparison.OrdinalIgnoreCase).Replace("%username%", player.displayName, StringComparison.OrdinalIgnoreCase);
                    foreach (var check in command.Split('|'))
                    {
                        LogAction(player, $"Исполнение команды: {check}");
                        Server.Command(check);
                    }

                    ShowNotify(player, _(player, "TAKE.GIVE.COMMAND", itemInfo.Name));
                }

                if (itemInfo.IsBlueprint)
                {
                    LogAction(player, $"Попытка получения рецепта {itemInfo.ShortName}");
                    Item create = ItemManager.CreateByItemID(-996920608);

                    var info = ItemManager.FindItemDefinition(itemInfo.ShortName);
                    create.blueprintTarget = info.itemid;

                    if (!player.inventory.GiveItem(create))
                    {
                        LogAction(player, $"У игрока не было места для получения рецепта, рецепт выброшен {itemInfo.ShortName} [{itemInfo.Amount} {player.transform.position}]");

                        create.Drop(player.transform.position, Vector3.down * 3);
                        ShowNotify(player, _(player, "TAKE.GIVE.BLUEPRINT.FEET", info.displayName.english));
                    }
                    else
                    {
                        LogAction(player, $"Рецепт выдан игроку в инвентарь");
                        ShowNotify(player, _(player, "TAKE.GIVE.BLUEPRINT.INVENTORY", info.displayName.english));
                    }
                }
            }
            catch (Exception e)
            {
                LogAction(player, $"Ошибка получения товара [{itemInfo.ID}][{itemInfo.Name}]");
                LogToFile("!Errors", $"{DateTime.Now.ToShortTimeString()}| Error: '{e.ToString()}'", this);
                PrintError($"Error receiving product [{itemInfo.ID}], info saved to log");
            }
            
        }

        private static readonly Dictionary<int, string> IDToShortName = new Dictionary<int, string> { [-1461508848] = "rifle.ak", [2115555558] = "ammo.handmade.shell", [-533875561] = "ammo.pistol", [1621541165] = "ammo.pistol.fire", [-422893115] = "ammo.pistol.hv", [815896488] = "ammo.rifle", [805088543] = "ammo.rifle.explosive", [449771810] = "ammo.rifle.incendiary", [1152393492] = "ammo.rifle.hv", [1578894260] = "ammo.rocket.basic", [1436532208] = "ammo.rocket.fire", [542276424] = "ammo.rocket.hv", [1594947829] = "ammo.rocket.smoke", [-1035059994] = "ammo.shotgun", [1818890814] = "ammo.shotgun.fire", [1819281075] = "ammo.shotgun.slug", [1685058759] = "antiradpills", [93029210] = "apple", [-1565095136] = "apple.spoiled", [-1775362679] = "arrow.bone", [-1775249157] = "arrow.fire", [-1280058093] = "arrow.hv", [-420273765] = "arrow.wooden", [563023711] = "autoturret", [790921853] = "axe.salvaged", [-337261910] = "bandage", [498312426] = "barricade.concrete", [504904386] = "barricade.metal", [-1221200300] = "barricade.sandbags", [510887968] = "barricade.stone", [-814689390] = "barricade.wood", [1024486167] = "barricade.woodwire", [2021568998] = "battery.small", [97329] = "bbq", [1046072789] = "trap.bear", [97409] = "bed", [-1480119738] = "tool.binoculars", [1611480185] = "black.raspberries", [-1386464949] = "bleach", [93832698] = "blood", [-1063412582] = "blueberries", [-1887162396] = "blueprintbase", [-55660037] = "rifle.bolt", [919780768] = "bone.club", [-365801095] = "bone.fragments", [68998734] = "botabag", [-853695669] = "bow.hunting", [271534758] = "box.wooden.large", [-770311783] = "box.wooden", [-1192532973] = "bucket.water", [-307490664] = "building.planner", [707427396] = "burlap.shirt", [707432758] = "burlap.shoes", [-2079677721] = "cactusflesh", [-1342405573] = "tool.camera", [-139769801] = "campfire", [-1043746011] = "can.beans", [2080339268] = "can.beans.empty", [-171664558] = "can.tuna", [1050986417] = "can.tuna.empty", [-1693683664] = "candycaneclub", [523409530] = "candycane", [1300054961] = "cctv.camera", [-2095387015] = "ceilinglight", [1428021640] = "chainsaw", [94623429] = "chair", [1436001773] = "charcoal", [1711323399] = "chicken.burned", [1734319168] = "chicken.cooked", [-1658459025] = "chicken.raw", [-726947205] = "chicken.spoiled", [-341443994] = "chocholate", [1540879296] = "xmasdoorwreath", [94756378] = "cloth", [3059095] = "coal", [3059624] = "corn", [2045107609] = "clone.corn", [583366917] = "seed.corn", [2123300234] = "crossbow", [1983936587] = "crude.oil", [1257201758] = "cupboard.tool", [-1144743963] = "diving.fins", [-1144542967] = "diving.mask", [-1144334585] = "diving.tank", [1066729526] = "diving.wetsuit", [-1598790097] = "door.double.hinged.metal", [-933236257] = "door.double.hinged.toptier", [-1575287163] = "door.double.hinged.wood", [-2104481870] = "door.hinged.metal", [-1571725662] = "door.hinged.toptier", [1456441506] = "door.hinged.wood", [1200628767] = "door.key", [-778796102] = "door.closer", [1526866730] = "xmas.door.garland", [1925723260] = "dropbox", [1891056868] = "ducttape", [1295154089] = "explosive.satchel", [498591726] = "explosive.timed", [1755466030] = "explosives", [726730162] = "facialhair.style01", [-1034048911] = "fat.animal", [252529905] = "femalearmpithair.style01", [471582113] = "femaleeyebrow.style01", [-1138648591] = "femalepubichair.style01", [305916740] = "female_hairstyle_01", [305916742] = "female_hairstyle_03", [305916744] = "female_hairstyle_05", [1908328648] = "fireplace.stone", [-2078972355] = "fish.cooked", [-533484654] = "fish.raw", [1571660245] = "fishingrod.handmade", [1045869440] = "flamethrower", [1985408483] = "flameturret", [97513422] = "flare", [1496470781] = "flashlight.held", [1229879204] = "weapon.mod.flashlight", [-1722829188] = "floor.grill", [1849912854] = "floor.ladder.hatch", [-1266285051] = "fridge", [-1749787215] = "boots.frog", [28178745] = "lowgradefuel", [-505639592] = "furnace", [1598149413] = "furnace.large", [-1779401418] = "gates.external.high.stone", [-57285700] = "gates.external.high.wood", [98228420] = "gears", [1422845239] = "geiger.counter", [277631078] = "generator.wind.scrap", [115739308] = "burlap.gloves", [-522149009] = "gloweyes", [3175989] = "glue", [718197703] = "granolabar", [384204160] = "grenade.beancan", [-1308622549] = "grenade.f1", [-217113639] = "fun.guitar", [-1580059655] = "gunpowder", [-1832205789] = "male_hairstyle_01", [305916741] = "female_hairstyle_02", [936777834] = "attire.hide.helterneck", [-1224598842] = "hammer", [-1976561211] = "hammer.salvaged", [-1406876421] = "hat.beenie", [-1397343301] = "hat.boonie", [1260209393] = "bucket.helmet", [-1035315940] = "burlap.headwrap", [-1381682752] = "hat.candle", [696727039] = "hat.cap", [-2128719593] = "coffeecan.helmet", [-1178289187] = "deer.skull.mask", [1351172108] = "heavy.plate.helmet", [-450738836] = "hat.miner", [-966287254] = "attire.reindeer.headband", [340009023] = "riot.helmet", [124310981] = "hat.wolf", [1501403549] = "wood.armor.helmet", [698310895] = "hatchet", [523855532] = "hazmatsuit", [2045246801] = "clone.hemp", [583506109] = "seed.hemp", [-148163128] = "attire.hide.boots", [-132588262] = "attire.hide.skirt", [-1666761111] = "attire.hide.vest", [-465236267] = "weapon.mod.holosight", [-1211618504] = "hoodie", [2133577942] = "hq.metal.ore", [-1014825244] = "humanmeat.burned", [-991829475] = "humanmeat.cooked", [-642008142] = "humanmeat.raw", [661790782] = "humanmeat.spoiled", [-1440143841] = "icepick.salvaged", [569119686] = "bone.armor.suit", [1404466285] = "heavy.plate.jacket", [-1616887133] = "jacket.snow", [-1167640370] = "jacket", [-1284735799] = "jackolantern.angry", [-1278649848] = "jackolantern.happy", [776005741] = "knife.bone", [108061910] = "ladder.wooden.wall", [255101535] = "trap.landmine", [-51678842] = "lantern", [-789202811] = "largemedkit", [516382256] = "weapon.mod.lasersight", [50834473] = "leather", [-975723312] = "lock.code", [1908195100] = "lock.key", [-1097452776] = "locker", [146685185] = "longsword", [-1716193401] = "rifle.lr300", [193190034] = "lmg.m249", [371156815] = "pistol.m92", [3343606] = "mace", [825308669] = "machete", [830965940] = "mailbox", [1662628660] = "male.facialhair.style02", [1662628661] = "male.facialhair.style03", [1662628662] = "male.facialhair.style04", [-1832205788] = "male_hairstyle_02", [-1832205786] = "male_hairstyle_04", [1625090418] = "malearmpithair.style01", [-1269800768] = "maleeyebrow.style01", [429648208] = "malepubichair.style01", [-1832205787] = "male_hairstyle_03", [-1832205785] = "male_hairstyle_05", [107868] = "map", [997973965] = "mask.balaclava", [-46188931] = "mask.bandana", [-46848560] = "metal.facemask", [-2066726403] = "bearmeat.burned", [-2043730634] = "bearmeat.cooked", [1325935999] = "bearmeat", [-225234813] = "deermeat.burned", [-202239044] = "deermeat.cooked", [-322501005] = "deermeat.raw", [-1851058636] = "horsemeat.burned", [-1828062867] = "horsemeat.cooked", [-1966381470] = "horsemeat.raw", [968732481] = "meat.pork.burned", [991728250] = "meat.pork.cooked", [-253819519] = "meat.boar", [-1714986849] = "wolfmeat.burned", [-1691991080] = "wolfmeat.cooked", [179448791] = "wolfmeat.raw", [431617507] = "wolfmeat.spoiled", [688032252] = "metal.fragments", [-1059362949] = "metal.ore", [1265861812] = "metal.plate.torso", [374890416] = "metal.refined", [1567404401] = "metalblade", [-1057402571] = "metalpipe", [-758925787] = "mining.pumpjack", [-1411620422] = "mining.quarry", [88869913] = "fish.minnows", [-2094080303] = "smg.mp5", [843418712] = "mushroom", [-1569356508] = "weapon.mod.muzzleboost", [-1569280852] = "weapon.mod.muzzlebrake", [449769971] = "pistol.nailgun", [590532217] = "ammo.nailgun.nails", [3387378] = "note", [1767561705] = "burlap.trousers", [106433500] = "pants", [-1334615971] = "heavy.plate.pants", [-135651869] = "attire.hide.pants", [-1595790889] = "roadsign.kilt", [-459156023] = "pants.shorts", [106434956] = "paper", [-578028723] = "pickaxe", [-586116979] = "jar.pickle", [-1379225193] = "pistol.eoka", [-930579334] = "pistol.revolver", [548699316] = "pistol.semiauto", [142147109] = "planter.large", [148953073] = "planter.small", [102672084] = "attire.hide.poncho", [640562379] = "pookie.bear", [-1732316031] = "xmas.present.large", [-2130280721] = "xmas.present.medium", [-1725510067] = "xmas.present.small", [1974032895] = "propanetank", [-225085592] = "pumpkin", [509654999] = "clone.pumpkin", [466113771] = "seed.pumpkin", [2033918259] = "pistol.python", [2069925558] = "target.reactive", [-1026117678] = "box.repair.bench", [1987447227] = "research.table", [540154065] = "researchpaper", [1939428458] = "riflebody", [-288010497] = "roadsign.jacket", [-847065290] = "roadsigns", [3506021] = "rock", [649603450] = "rocket.launcher", [3506418] = "rope", [569935070] = "rug.bear", [113284] = "rug", [1916127949] = "water.salt", [-1775234707] = "salvaged.cleaver", [-388967316] = "salvaged.sword", [2007564590] = "santahat", [-1705696613] = "scarecrow", [670655301] = "hazmatsuit_scientist", [1148128486] = "hazmatsuit_scientist_peacekeeper", [-141135377] = "weapon.mod.small.scope", [109266897] = "scrap", [-527558546] = "searchlight", [-1745053053] = "rifle.semiauto", [1223860752] = "semibody", [-419069863] = "sewingkit", [-1617374968] = "sheetmetal", [2057749608] = "shelves", [24576628] = "shirt.collared", [-1659202509] = "shirt.tanktop", [2107229499] = "shoes.boots", [191795897] = "shotgun.double", [-1009492144] = "shotgun.pump", [2077983581] = "shotgun.waterpipe", [378365037] = "guntrap", [-529054135] = "shutter.metal.embrasure.a", [-529054134] = "shutter.metal.embrasure.b", [486166145] = "shutter.wood.a", [1628490888] = "sign.hanging.banner.large", [1498516223] = "sign.hanging", [-632459882] = "sign.hanging.ornate", [-626812403] = "sign.pictureframe.landscape", [385802761] = "sign.pictureframe.portrait", [2117976603] = "sign.pictureframe.tall", [1338515426] = "sign.pictureframe.xl", [-1455694274] = "sign.pictureframe.xxl", [1579245182] = "sign.pole.banner.large", [-587434450] = "sign.post.double", [-163742043] = "sign.post.single", [-1224714193] = "sign.post.town", [644359987] = "sign.post.town.roof", [-1962514734] = "sign.wooden.huge", [-705305612] = "sign.wooden.large", [-357728804] = "sign.wooden.medium", [-698499648] = "sign.wooden.small", [1213686767] = "weapon.mod.silencer", [386382445] = "weapon.mod.simplesight", [1859976884] = "skull_fire_pit", [960793436] = "skull.human", [1001265731] = "skull.wolf", [1253290621] = "sleepingbag", [470729623] = "small.oil.refinery", [1051155022] = "stash.small", [865679437] = "fish.troutsmall", [927253046] = "smallwaterbottle", [109552593] = "smg.2", [-2092529553] = "smgbody", [691633666] = "snowball", [-2055888649] = "snowman", [621575320] = "shotgun.spas12", [-2118132208] = "spear.stone", [-1127699509] = "spear.wooden", [-685265909] = "spikes.floor", [552706886] = "spinner.wheel", [1835797460] = "metalspring", [-892259869] = "sticks", [-1623330855] = "stocking.large", [-1616524891] = "stocking.small", [789892804] = "stone.pickaxe", [-1289478934] = "stonehatchet", [-892070738] = "stones", [-891243783] = "sulfur", [889398893] = "sulfur.ore", [-1625468793] = "supply.signal", [1293049486] = "surveycharge", [1369769822] = "fishtrap.small", [586484018] = "syringe.medical", [110115790] = "table", [1490499512] = "targeting.computer", [3552619] = "tarp", [1471284746] = "techparts", [456448245] = "smg.thompson", [110547964] = "torch", [1588977225] = "xmas.decoration.baubels", [918540912] = "xmas.decoration.candycanes", [-471874147] = "xmas.decoration.gingerbreadmen", [205978836] = "xmas.decoration.lights", [-1044400758] = "xmas.decoration.pinecone", [-2073307447] = "xmas.decoration.star", [435230680] = "xmas.decoration.tinsel", [-864578046] = "tshirt", [1660607208] = "tshirt.long", [260214178] = "tunalight", [-1847536522] = "vending.machine", [-496055048] = "wall.external.high.stone", [-1792066367] = "wall.external.high", [562888306] = "wall.frame.cell.gate", [-427925529] = "wall.frame.cell", [995306285] = "wall.frame.fence.gate", [-378017204] = "wall.frame.fence", [447918618] = "wall.frame.garagedoor", [313836902] = "wall.frame.netting", [1175970190] = "wall.frame.shopfront", [525244071] = "wall.frame.shopfront.metal", [-1021702157] = "wall.window.bars.metal", [-402507101] = "wall.window.bars.toptier", [-1556671423] = "wall.window.bars.wood", [61936445] = "wall.window.glass.reinforced", [112903447] = "water", [1817873886] = "water.catcher.large", [1824679850] = "water.catcher.small", [-1628526499] = "water.barrel", [547302405] = "waterjug", [1840561315] = "water.purifier", [-460592212] = "xmas.window.garland", [3655341] = "wood", [1554697726] = "wood.armor.jacket", [-1883959124] = "wood.armor.pants", [-481416622] = "workbench1", [-481416621] = "workbench2", [-481416620] = "workbench3", [-1151126752] = "xmas.lightstring", [-1926458555] = "xmas.tree" };

        private void UI_RecountPosition(ref double xSwitch, ref double ySwitch, int count, int max)
        {
            var stringAmount = Math.Ceiling((double)max / Settings.InterfaceSettings.ItemOnString);
            var currentString = Math.Floor((double)count / Settings.InterfaceSettings.ItemOnString);
            var currentPosition = count % Settings.InterfaceSettings.ItemOnString;


            var topYPosition = 0 + (float)stringAmount / 2 * Settings.InterfaceSettings.ItemSide + ((float)stringAmount / 2 - 1) * Settings.InterfaceSettings.ItemMargin;
            var topXPosition = 0 - (float)Settings.InterfaceSettings.ItemOnString / 2 * Settings.InterfaceSettings.ItemSide - ((float)Settings.InterfaceSettings.ItemOnString / 2 - 1) * Settings.InterfaceSettings.ItemMargin;

            var curYPosition = topYPosition - currentString * Settings.InterfaceSettings.ItemSide - (currentString) * (Settings.InterfaceSettings.ItemMargin + (Settings.InterfaceSettings.TextShow ? 20 : 0));
            var curXPosition = topXPosition + currentPosition * Settings.InterfaceSettings.ItemSide + (currentPosition) * Settings.InterfaceSettings.ItemMargin;

            xSwitch = curXPosition;
            ySwitch = curYPosition;
        }

        public static bool CheckInstantWork = false;
        public Timer CheckInstantWorkGlobalTimer = null;

        private void CheckInstant()
        {
            if (CheckInstantWork == true)
                return;

            CheckInstantWork = true;
            if (CheckInstantWorkGlobalTimer != null)
                CheckInstantWorkGlobalTimer.Destroy();

            CheckInstantWorkGlobalTimer = timer.Once(300, () => { CheckInstantWork = false; });

            Request($"&method=basket.commands.instant", (code, response) =>
            {
                switch (code)
                {
                    case 0:
                        {
                            LogAction(null, $"Ошибка выполнения запроса для выдачи автоматических команд");
                            CheckInstantWork = false;
                            break;
                        }
                    case 200:
                        {
                            var firstInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(response, new KeyValuesConverter());
                            if (firstInfo.ContainsKey("result"))
                            {
                                if (firstInfo["result"].ToString() == "success")
                                {
                                    List<object> data = firstInfo["data"] as List<object>;
                                    int i = 1;
                                    timer.Once(data.Count + 2, () => { CheckInstantWork = false; });

                                    foreach (Dictionary<string, object> product in data)
                                    {
                                        if (product.ContainsKey("id"))
                                        {
                                            timer.Once(i, () => { TakeInstant(Convert.ToInt32(product["id"]), product["steam_id"].ToString()); } );
                                            i++;
                                        }

                                    }
                                } else CheckInstantWork = false;
                            } else CheckInstantWork = false;
                            break;
                        }
                    default:
                        {
                            LogAction(null, $"Ошибка выполнения запроса для выдачи автоматических команд");
                            CheckInstantWork = false;
                            break;
                        }
                }
            }, null); 
        }

        private void TakeInstant(int GsProductId, string SteamId)
        {
            LogAction(null, $"---------------------------------");
            LogAction(null, $"[AutoCommands] Запрос на получение товара [{GsProductId}][{SteamId}]");
            Request($"&item=true&id={GsProductId}", (i, s) =>
            {
                switch (i)
                {
                    case 0:
                        LogAction(null, $"[AutoCommands] API не ответило на запрос: {GsProductId}");
                        PrintError("Api does not responded to a request");
                        break;
                    case 200:
                        Dictionary<string, object> response = JsonConvert.DeserializeObject<Dictionary<string, object>>(s, new KeyValuesConverter());
                        if (!response.ContainsKey("data"))
                        {
                            LogAction(null, $"[AutoCommands] Ошибка получения товара, отсутствует Data [{GsProductId}]");
                            return;
                        } else
                        {
                            Request($"&gived=true&id={GsProductId}", (code, newResponse) =>
                            {
                                if (code != 200 || JsonConvert.DeserializeObject<JObject>(newResponse)["result"].ToString() != "success")
                                {
                                    LogAction(null, $"[AutoCommands] Ошибка получения товара [{GsProductId}][{SteamId}]");
                                    return;
                                }
                                else
                                {
                                    WItem itemInfo = new WItem(response["data"] as Dictionary<string, object>);
                                    if (itemInfo.IsCommand)
                                    {
                                        LogAction(null, $"[AutoCommands] Товар отмечен полученным [{GsProductId}][{SteamId}]");
                                        string command = itemInfo.Command.Replace("\n", "|").Replace("%steamid%", SteamId, StringComparison.OrdinalIgnoreCase);
                                        foreach (var check in command.Split('|'))
                                        {
                                            LogAction(null, $"[AutoCommands] Исполнение команды: {check} [{GsProductId}][{SteamId}]");
                                            Server.Command(check);
                                        }
                                    }
                                    else
                                    {
                                        LogAction(null, $"[AutoCommands] Ошибка получения (товар не является командой) [{GsProductId}][{SteamId}]");
                                    }
                                }
                            }, null);
                        }
                       
                        break;
                    case 404:
                        LogAction(null, $"[AutoCommands] Api не ответило на запрос: {GsProductId}");
                        LogAction(null, "Response code: 404, please check your configurations", true);
                        break;
                }
            }, null);
        }

        private Timer RestartTimer = null;
        private void SetReserveApiLink()
        {
            if (BaseRequest.Contains(MainApiLink))
            {
                LogAction(null, $"Trying use reserve api link", true);
                BaseRequest = ReserveApiLink + BaseRequestParams;
                if (!Initialized)
                    FetchShopUrl();
                timer.Once(300, () => {
                    TryChangeLink();
                });
            } else
            {
                LogAction(null, $"Plugin will automatically restart after 10 minutes", true);
                RestartTimer = timer.Once(600, () => { LogAction(null, $"Restarting...", true); Server.Command("o.reload GameStoresRUST"); });
            }                         
        }

        private void TryChangeLink()
        {
            string newLink = ReserveApiLink + BaseRequestParams;
            if (BaseRequest.Contains(ReserveApiLink))
            {
                newLink = MainApiLink + BaseRequestParams;
            }
            CheckRequest($"{newLink}&info=true", (code, response) =>
            {
                switch (code)
                {
                    case 200:
                        {
                            if (response.Length < 1)
                            {
                                return;
                            }
                            var firstInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(response, new KeyValuesConverter());
                            if (!firstInfo.ContainsKey("data"))
                            {
                                initialization = false;
                            }
                            else
                            {
                                if (RestartTimer != null)
                                {
                                    LogAction(null, $"Restart was canceled");
                                    RestartTimer.Destroy();
                                    RestartTimer = null;
                                }
                                LogAction(null, $"API link was changed", true);
                                BaseRequest = newLink;
                                if (!Initialized)
                                    FetchShopUrl();
                                if (BaseRequest.Contains(ReserveApiLink))
                                    timer.Once(600, () => { TryChangeLink(); });
                            }
                            break;
                        }
                    default:
                        break;

                }
            });
        }

        private void FetchShopUrl()
        {
            initialization = true;
            timer.Once(5, () => { initialization = false; });
            Request($"&info=true", (code, response) =>
            {
                LogAction(null, $"-----------------------------", true);
                LogAction(null, $" GameStores {Version} (c) 2022", true);
                try
                {
                    if (response.Length < 1)
                    {
                        LogToFile("!Errors", $"{DateTime.Now.ToShortTimeString()}| Response: '{response}'", this);
                        LogAction(null, " Incorrect API response! Saved to log!", true, true);
                        LogAction(null, $"-----------------------------", true);
                        SetReserveApiLink();
                        initialization = false;
                        return;
                    }
                    var firstInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(response, new KeyValuesConverter());
                    if (!firstInfo.ContainsKey("data"))
                    {
                        LogAction(null, $"     Wrong Secret Key", true, true);
                        LogAction(null, $"-----------------------------", true);
                        initialization = false;
                    }
                    else
                    { 
                        firstInfo = firstInfo["data"] as Dictionary<string, object>;
                        ShopURL = firstInfo["link"].ToString();
                        StartBalance = int.Parse(firstInfo["default_balance"].ToString());
                        LogAction(null, "      Initialized - OK", true);
                        LogAction(null, $"-----------------------------", true);
                        //BasePlayer.activePlayerList.ForEach(OnPlayerInit);
                        foreach (var pl in BasePlayer.activePlayerList)
                        {
                            OnPlayerConnected(pl);
                        }

                        Initialized = true;
                        initialization = false;
                    }
                }
                catch (JsonException e)
                {
                    
                    LogToFile("Errors", $"{DateTime.Now.ToShortTimeString()}| JsonError | Response: '{response}'", this);
                    LogAction(null, " JSON Error! Saved to log!", true, true);
                    LogAction(null, $"-----------------------------", true);
                    SetReserveApiLink();
                    initialization = false;
                }

            });
        }

        private static int errorsReq = 0;
        private static void Request(string ask, Action<int, string> callback, BasePlayer player = null, bool cancel = true)
        {
            if (player != null && !Delays.CanRequest(player))
            {
                instance.ShowNotify(player, instance._(player, "MANY.REQUESTS"));
                instance.timer.Once(1f, () =>
                {
                    CuiHelper.DestroyUi(player, StoreLayer + ".Notify");
                    Request(ask, callback, player, cancel);
                });

                return;
            };
            if (player != null) ServerMgr.Instance.StartCoroutine(Delays.MakeRequest(player));
            Dictionary<string, string> reqHeaders = new Dictionary<string, string>{{ "User-Agent", "GameStores Plugin" }};
            if (!SecureConnection && instance.BaseRequest.Contains("https://")) instance.BaseRequest = instance.BaseRequest.Replace("https://", "http://");
            instance.webrequest.Enqueue(instance.BaseRequest + ask, "", (code, response) =>
            {
                if (instance == null) return;
                
                switch (code)
                {
                    case 200:
                        {
                            break;
                        }

                    case 404:
                        {
                            instance.LogAction(null, $"Please check your configuration! [404] #2", true);
                            break;
                        }

                    default:
                        {
                            instance.LogAction(null, $"Time out waiting for GS API #1");
                            errorsReq++;
                            
                            if (errorsReq >= 10)
                            {
                                errorsReq = 0;
                                instance.TryChangeLink();
                            }
                            
                            break;
                        }
                }

                if (SecureConnection && response.Contains("The authentication or decryption has failed."))
                {
                    instance.LogAction(null, "HTTPS request is broken (broken CA certificate?). Changed to non secure connection!", true, true);

                    SecureConnection = false;
                    instance.BaseRequest = instance.BaseRequest.Replace("https://", "http://");

                    Request(ask, callback, player, cancel);
                    return;
                }

                if (player != null && cancel) Delays.FinishRequest(player);

                callback?.Invoke(code, response);
            }, instance, RequestMethod.GET, reqHeaders);
                      
        }
        private static void CheckRequest(string link, Action<int, string> callback)
        {
            Dictionary<string, string> reqHeaders = new Dictionary<string, string> { { "User-Agent", "GameStores Plugin" } };
            if (!SecureConnection && link.Contains("https://")) link = link.Replace("https://", "http://");
            instance.webrequest.Enqueue(link, "", (code, response) =>
            {
                if (instance == null) return;

                switch (code)
                {
                    case 0:
                        {
                            
                            break;
                        }
                    case 404:
                        {
                            
                            break;
                        }
                }

                callback?.Invoke(code, response);
            }, instance, RequestMethod.GET, reqHeaders);

        }

        private static void RequestPost(string ask, Action<int, string> callback, BasePlayer player = null, bool cancel = true)
        {
            if (player != null && !Delays.CanRequest(player))
            {
                instance.ShowNotify(player, instance._(player, "MANY.REQUESTS"));
                instance.timer.Once(1f, () =>
                {
                    CuiHelper.DestroyUi(player, StoreLayer + ".Notify");
                    RequestPost(ask, callback, player, cancel);
                });
                return;
            };
            if (player != null) ServerMgr.Instance.StartCoroutine(Delays.MakeRequest(player));
            string body, reqLink;
            int pos = instance.BaseRequest.IndexOf("?");
            if (pos < 0)
            {
                body = ask;
                reqLink = instance.BaseRequest;
            }
            else
            {
                reqLink = instance.BaseRequest.Substring(0, pos);
                body = $"{instance.BaseRequest.Substring(pos + 1)}{ask}";
            }
            Dictionary<string, string> reqHeaders = new Dictionary<string, string> { { "User-Agent", "GameStores Plugin" } };
            if (!SecureConnection && reqLink.Contains("https://")) reqLink = reqLink.Replace("https://", "http://");
            instance.webrequest.Enqueue(reqLink, body, (code, response) =>
            {
                if (instance == null) return;

                switch (code)
                {
                    case 0:
                        {
                            instance.PrintError($"Time out waiting for GS API #1");
                            break;
                        }
                    case 404:
                        {
                            instance.PrintError($"Plese check your configuration! [404] #2");
                            break;
                        }
                }

                if (player != null && cancel) Delays.FinishRequest(player);

                callback?.Invoke(code, response);
            }, instance, RequestMethod.POST, reqHeaders);
        }

        private void LogAction(BasePlayer player, string text, bool printToConsole = false, bool printError = false)
        {
            LogToFile($"!global", $"{DateTime.Now.ToShortTimeString()} {(player != null ? "[" + player.userID + "]" : " ")}{text}", this);

            if (printToConsole)
                if (printError)
                    instance.PrintError($"{text}");
                else 
                    PrintWarning($"{text}");
            if (player != null)
                LogPlayerAction(player, text);
        }
        private void LogPlayerAction(BasePlayer player, string text) => LogToFile($"{player.userID}", $"{DateTime.Now.ToShortTimeString()} {text}", this);
        private static double CurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

        #endregion
    }
}