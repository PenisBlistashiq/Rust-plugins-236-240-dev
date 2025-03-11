using Network;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
namespace Oxide.Plugins
{
    [Info("Trade", "OxideBro", "2.2.3")]
    public class Trade : RustPlugin
    {
        private static Trade ins;
        private PluginConfig config;
        public List<TradeBox> tradeBoxes = new List<TradeBox>();
        private List<TradePendings> pendings = new List<TradePendings>();
        private Dictionary<BasePlayer, DateTime> Cooldowns = new Dictionary<BasePlayer, DateTime>();

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Благодарим за покупку плагина на сайте RustPlugin.ru. Если вы передадите этот плагин сторонним лицам знайте - это лишает вас гарантированных обновлений!");
            config = PluginConfig.DefaultConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
            if (config.PluginVersion < Version)
                UpdateConfigValues();
            Config.WriteObject(config, true);
        }

        private void UpdateConfigValues()
        {
            PluginConfig baseConfig = PluginConfig.DefaultConfig();
            if (config.PluginVersion < new VersionNumber(2, 2, 0))
            {
                PrintWarning("Config update detected! Updating config values...");
                config.mainSettings.permsNum = new Dictionary<string, PermissionTrade>()
                {
                    ["trade.one"] = new PermissionTrade()
                    {
                        GetCapacity = 4,
                        GetCooldown = 50,
                    },
                    ["trade.two"] = new PermissionTrade()
                    {
                        GetCapacity = 5,
                        GetCooldown = 40,
                    },
                    ["trade.three"] = new PermissionTrade()
                    {
                        GetCapacity = 6,
                        GetCooldown = 30,
                    },
                };
                PrintWarning("Config update completed!");
            }
            config.PluginVersion = Version;
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        public class MainSettings
        {
            [JsonProperty("Запретить принимать запрос в BuildingBlock")]
            public bool getCupAuth = true;

            [JsonProperty("Запретить отправлять запрос в BuildingBlock")]
            public bool getCupSend = true;

            [JsonProperty("Запретить использовать трейд в полёте")]
            public bool getFly = true;

            [JsonProperty("Запретить использовать трейд в воде")]
            public bool getSwim = true;

            [JsonProperty("Запретить обмениватся игрокам если игроки не в тиме (Стандартная система друзей)")]
            public bool enabledTeamate = false;

            [JsonProperty("Запретить использовать трейд в предсмертном состоянии")]
            public bool getWound = true;

            [JsonProperty("Время ответа на предложения обмена (секунд)")]
            public int getTime = 15;

            [JsonProperty("Задержка использования трейда (Cooldown - секунд)")]
            public double CooldownTrade = 60.0;

            [JsonProperty("Разрешить трейд если между игроками если их дистанция больше указанной (-1 - отключение)")]
            public double TradeDistance = 50;

            [JsonProperty("Количество активных слотов при обмене")]
            public int getInt = 8;

            [JsonProperty("Список привилегий и размера слотов при обмене")]
            public Dictionary<string, PermissionTrade> permsNum = new Dictionary<string, PermissionTrade>();

            [JsonProperty("Привилегия на использование команды trade")]
            public string Permission = "trade.use";

            [JsonProperty("Разрешить использование трейда только если игрок имеет привилегию указаную в конфиге")]
            public bool UsePermission = false;
        }

        public class PermissionTrade
        {
            [JsonProperty("Размер слотов у данной привилегии")]
            public int GetCapacity = 0;
            [JsonProperty("Задержка после обмена у данной привилегии")]
            public int GetCooldown = 0;
        }

        class PluginConfig
        {
            [JsonProperty("Основные")]
            public MainSettings mainSettings;

            [JsonProperty("Версия конфигурации")]
            public VersionNumber PluginVersion = new VersionNumber();


            [JsonIgnore]
            [JsonProperty("Инициализация плагина")]
            public bool Init;

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    mainSettings = new MainSettings()
                    {
                        permsNum = new Dictionary<string, PermissionTrade>()
                        {
                            ["trade.one"] = new PermissionTrade()
                            {
                                GetCapacity = 4,
                                GetCooldown = 50,
                            },
                            ["trade.two"] = new PermissionTrade()
                            {
                                GetCapacity = 5,
                                GetCooldown = 40,
                            },
                            ["trade.three"] = new PermissionTrade()
                            {
                                GetCapacity = 6,
                                GetCooldown = 30,
                            },
                        }
                    },
                    PluginVersion = new VersionNumber(),

                };
            }
        }

        [PluginReference] Plugin Duel;
        private bool IsDuelPlayer(BasePlayer player)
        {
            if (!Duel) return false;
            var dueler = Duel?.Call("IsPlayerOnActiveDuel", player);
            if (dueler is bool) return (bool)dueler;
            return false;
        }

        private bool IsTeamate(BasePlayer player, BasePlayer target)
        {
            if (!config.mainSettings.enabledTeamate) return true;
            if (player.currentTeam == 0 || target.currentTeam == 0) return false;
            return player.currentTeam == target.currentTeam;
        }

        int GetTradeSize(string UserID)
        {
            int size = config.mainSettings.getInt;
            foreach (var num in config.mainSettings.permsNum)
            {
                if (permission.UserHasPermission(UserID, num.Key))
                    if (num.Value.GetCapacity > size) size = num.Value.GetCapacity;
            }
            return size;
        }

        double GetPlayerCooldown(string UserID)
        {
            var cd = config.mainSettings.CooldownTrade;
            foreach (var num in config.mainSettings.permsNum)
            {
                if (permission.UserHasPermission(UserID, num.Key))
                    if (num.Value.GetCooldown < cd)
                        cd = num.Value.GetCooldown;
            }
            return cd;
        }

        void Reply(BasePlayer player, string langKey, params object[] args) => SendReply(player, Messages[langKey], args);

        bool CanPlayerTrade(BasePlayer player)
        {
            var reply = 2808;
            if (reply == 0) { }
            if (!config.Init) return false;
            if (config.mainSettings.getSwim)
            {
                if (player.IsSwimming())
                {
                    Reply(player, "DENIED.SWIMMING");
                    return false;
                }
            }
            if (config.mainSettings.getCupSend || config.mainSettings.getCupAuth)
            {
                if (!player.CanBuild())
                {
                    Reply(player, "DENIED.PRIVILEGE");
                    return false;
                }
            }
            if (config.mainSettings.getFly)
            {
                if (!player.IsOnGround() || player.IsFlying)
                {
                    Reply(player, "DENIED.FALLING");
                    return false;
                }
            }
            if (config.mainSettings.getWound)
            {
                if (player.IsWounded())
                {
                    Reply(player, "DENIED.WOUNDED");
                    return false;
                }
            }
            if (Cooldowns.ContainsKey(player))
            {
                double seconds = Cooldowns[player].Subtract(DateTime.Now).TotalSeconds;
                if (seconds >= 0)
                {
                    Reply(player, "COOLDOWN", seconds);
                    return false;
                }
            }
            if (IsDuelPlayer(player))
            {
                Reply(player, "DENIED.DUEL");
                return false;
            }
            var canTrade = Interface.Call("CanTrade", player);
            if (canTrade != null)
            {
                if (canTrade is string)
                {
                    SendReply(player, Convert.ToString(canTrade));
                    return false;
                }
                Reply(player, "DENIED.GENERIC");
                return false;
            }
            return true;
        }

        Dictionary<string, string> Messages = new Dictionary<string, string>() {
                {
                "DENIED.SWIMMING", "Недоступно, вы плаваете!"
            }
            , {
                "DENIED.DUEL", "Недоступно, один из игроков на Duel!"
            }
            , {
                "DENIED.PERMISSIONON", "Недоступно, у Вас нету прав на использование трейда!"
            }
            , {
                "DENIED.PERMISSIOONTARGETN", "Недоступно, у {0} прав на использование трейда!"
            }
            , {
                "DENIED.FALLING", "Недоступно, вы левитируете!"
            }
            , {
                "DENIED.WOUNDED", "Недоступно, вы в предсмертном состоянии!"
            }
            , {
                "DENIED.GENERIC", "Недоступно, заблокировано другим плагином!"
            }
            , {
                "DENIED.PRIVILEGE", "Недоступно, вы в зоне Building Blocked!"
            }
            , {
                "DENIED.PERMISSION", "Недоступно, вы в зоне Building Blocked!"
            }
            , {
                "TRADE.HELP", "Trade by RustPlugin.ru\nИспользуйте комманду <color=orange>/trade \"НИК\"</color> для обмена\nЧто бы принять обмен, введите: <color=orange>/trade yes</color> (или /trade accept)\nЧто бы отказаться от обмена введите: <color=orange>/trade no </color> (или /trade cancel)"
            }
            , {
                "PLAYER.NOT.FOUND", "Игрок '{0}' не найден!"
            }
             , {
                "TRADE.ALREADY.PENDING", "Невозможно! Вы либо вам уже отправлено предложение обмена!"
            }
            , {
                "TRADE.TARGET.ALREADY.PENDING", "Невозможно! У игрока есть активное предложение обмена!"
            }

            , {
                "TRADE.ACCEPT.PENDING.EMPTY", "У вас нет входящих предложний обмена!"
            }
            , {
                "TRADE.CANCELED", "Trade отменен!"
            }
            , {
                "TRADE.TOYOU", "Нельзя отправлять запрос самому себе!"
            }
            , {
                "TRADE.SUCCESS", "Trade успешно завершён!"
            }
            , {
                "PENDING.RECIEVER.FORMAT", "Игрок '{0}' отправил вам предложние обмена\nДля принятия обмена используйте команду <color=orange>/trade yes</color>\nЧто бы отказаться введите <color=orange>/trade no</color>"
            }
            , {
                "PENDING.SENDER.FORMAT", "Предложение обмена игроку '{0}' успешно отправлено, ожидайте..."
            }
            , {
                "PENDING.TIMEOUT.SENDER", "Trade отменён! Причина: время истекло."
            }
            , {
                "PENDING.TIMEOUT.RECIEVER", "Trade отменён! Причина: вы вовремя не приняли запрос."
            }
            , {
                "PENDING.CANCEL.SENDER", "Trade отменён! Причина: игрок '{0}' отказался"
            },
            {
                "COOLDOWN", "Вы только недавно обменивались, подождите - {0:0} сек."
            },
            {
                "GET.FRIENDS", "Вы не состоите в одной тиме с игроком {0}, трейд запрещен"
            },
            {
                "GET.DISTANCE", "Трейд запрещен на малых дистанциях между вами игроком"
            },
        };


        private void Loaded()
        {
            ins = this;
            lang.RegisterMessages(Messages, this);
            Messages = lang.GetMessages("en", this);
            permission.RegisterPermission(config.mainSettings.Permission, this);
            var perms = config.mainSettings.permsNum.Where(p => p.Key.StartsWith("trade."));
            foreach (var perm in perms)
            {
                if (!permission.PermissionExists(perm.Key))
                    permission.RegisterPermission(perm.Key, this);

            }
            if (!permission.PermissionExists(config.mainSettings.Permission))
                permission.RegisterPermission(config.mainSettings.Permission, this);
            config.Init = true;
        }

        void OnServerInitialized()
        {
            timer.Every(1f, TradeTimerHandle);
        }

        class TradePendings
        {
            public BasePlayer target;
            public BasePlayer player;
            public int seconds;

            public TradePendings(BasePlayer player, int Seconds, BasePlayer target)
            {
                this.target = target;
                this.player = player;
                seconds = Seconds;

            }
        }

        void TradeTimerHandle()
        {
            for (int i = pendings.Count - 1;
           i >= 0;
           i--)
            {
                var pend = pendings[i];
                if (pend.target != null && !pend.target.IsConnected || pend.target.IsWounded())
                {
                    pendings.RemoveAt(i);
                    continue;
                }
                if (pend.player != null && !pend.player.IsConnected || pend.player.IsWounded())
                {
                    pendings.RemoveAt(i);
                    continue;
                }
                if (--pend.seconds <= 0)
                {
                    pendings.RemoveAt(i);
                    if (pend.player.IsConnected) Reply(pend.player, "PENDING.TIMEOUT.SENDER");
                    if (pend.target.IsConnected) Reply(pend.target, "PENDING.TIMEOUT.RECIEVER");
                }
            }
        }

        void Unload()
        {
            foreach (var trade in tradeBoxes)
            {
                UnityEngine.Object.Destroy(trade);
            }
        }

        private void OnItemSplit(Item item, int amount)
        {
            if (!config.Init) return;
            if (item == null) return;
            if (item.GetRootContainer() == null || item.GetRootContainer()?.entityOwner == null || item.GetRootContainer()?.entityOwner?.GetComponent<ShopFront>() == null) return;
            var container = item.GetRootContainer().entityOwner?.GetComponent<ShopFront>();
            if (container != null)
                if (container.GetComponent<TradeBox>() != null)
                {
                    if (container.vendorInventory != null && container.customerInventory != null)
                        if (container.vendorInventory.IsLocked() || container.customerInventory.IsLocked())
                            container.ResetTrade();
                }
        }

        [PluginReference] Plugin SkinBox;

        bool isSkinBox(ulong playerID)
        {
            if (!SkinBox) return false;

            var result = SkinBox?.Call("IsSkinBoxPlayer", playerID);
            if (result != null)
                return (bool)SkinBox?.Call("IsSkinBoxPlayer", playerID);
            return false;
        }

        object CanMoveItem(Item item, PlayerInventory playerLoot, uint targetContainer)
        {
            if (!config.Init) return null;
            if (playerLoot == null) return null;
            var container = playerLoot.FindContainer(targetContainer);
            if (container == null) return null;
            var player = playerLoot.containerMain.playerOwner;
            if (player == null) return null;
            if (container.entityOwner != null && container.entityOwner is ShopFront)
            {
                var shopfront = container.entityOwner.GetComponent<ShopFront>();
                if (shopfront != null)
                {
                    if (item.contents != null)
                    {
                        item.contents.SetLocked(true);
                        item.MarkDirty();
                    }
                    if (shopfront.IsPlayerCustomer(player) && shopfront.customerInventory.uid != targetContainer)
                        return false;
                    else if (shopfront.IsPlayerVendor(player) && shopfront.vendorInventory.uid != targetContainer) return false;
                }
            }
            else
            {
                if (item.contents != null && item.contents.IsLocked() && !isSkinBox(player.userID))
                {
                    item.contents.SetLocked(false);
                    item.MarkDirty();
                }
            }
            return null;
        }

        object OnItemAction(Item item, string action, BasePlayer player)
        {
            if (player == null || item == null) return null;
            if (item.GetRootContainer() == null) return null;
            var container = item.GetRootContainer();
            if (container.entityOwner != null && container.entityOwner is ShopFront && container.entityOwner.GetComponent<TradeBox>() != null)
                return false;
            return null;
        }

        void OnEntityKill(ShopFront shop)
        {
            if (shop == null) return;
            if (shop.GetComponent<TradeBox>() != null)
            {
                if (shop.GetComponent<TradeBox>().player1 != null)
                    shop.GetComponent<TradeBox>().player1.EndLooting();
                if (shop.GetComponent<TradeBox>().player2 != null)
                    shop.GetComponent<TradeBox>().player2.EndLooting();
            }
        }

        object OnEntityVisibilityCheck(ShopFront shop, BasePlayer player, uint rpcId, string debugName, float maximumDistance)
        {
            if (!config.Init) return null;
            if (shop == null || shop?.net.ID == null || player == null) return null;
            if (shop.GetComponent<TradeBox>() != null)
            {
                if (shop.IsPlayerVendor(player))
                {
                    shop.SetFlag(global::BaseEntity.Flags.Reserved1, true, false, true);
                    shop.vendorInventory.SetLocked(true);
                }
                else if (shop.IsPlayerCustomer(player))
                {
                    shop.SetFlag(global::BaseEntity.Flags.Reserved2, true, false, true);
                    shop.customerInventory.SetLocked(true);
                }
                if (shop.HasFlag(global::BaseEntity.Flags.Reserved1) && shop.HasFlag(global::BaseEntity.Flags.Reserved2))
                {
                    shop.SetFlag(global::BaseEntity.Flags.Reserved3, true, false, true);
                    shop.Invoke(new Action(shop.GetComponent<TradeBox>().CustomCompleteTrade), 2f);
                    return false;
                }
                return true;
            }
            return null;
        }

        public BasePlayer FindOnline(string nameOrUserId, ulong playerid = 2875392)
        {
            nameOrUserId = nameOrUserId.ToLower();
            foreach (BasePlayer activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.displayName.ToLower().Contains(nameOrUserId) || activePlayer.UserIDString == nameOrUserId) return activePlayer;
            }
            return null;
        }

        [ConsoleCommand("trade")]
        void cmdTrade(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || arg.Args.Length == 0) return;
            var name = arg.Args[0];
            CmdChatTrade(player, string.Empty, new string[] {
                name
            }
            );
        }

        [ChatCommand("trade")]
        void CmdChatTrade(BasePlayer player, string command, string[] args)
        {
            if (!config.Init) return;
            if (player == null) return;
            if (args.Length == 0 || args == null)
            {
                Reply(player, "TRADE.HELP");
                return;
            }
            if (config.mainSettings.UsePermission && !permission.UserHasPermission(player.UserIDString, config.mainSettings.Permission))
            {
                Reply(player, "DENIED.PERMISSIONON");
                return;
            }
            if (Cooldowns.ContainsKey(player))
            {
                double seconds = Cooldowns[player].Subtract(DateTime.Now).TotalSeconds;
                if (seconds >= 0)
                {
                    Reply(player, "COOLDOWN", seconds);
                    return;
                }
            }

            switch (args[0])
            {
                default:
                    if (!CanPlayerTrade(player))
                        return;
                    var name = args[0];
                    var target = FindOnline(name);
                    if (target == null)
                    {
                        Reply(player, "PLAYER.NOT.FOUND", name);
                        return;
                    }
                    if (target == player)
                    {
                        Reply(player, "TRADE.TOYOU");
                        return;
                    }
                    if (config.mainSettings.UsePermission && !permission.UserHasPermission(target.UserIDString, config.mainSettings.Permission))
                    {
                        Reply(player, "DENIED.PERMISSIOONTARGETN", target.displayName);
                        return;
                    }
                    var findPLayerPendings = pendings.Find(p => p.player == player || p.target == player);
                    if (findPLayerPendings != null)
                    {
                        Reply(player, "TRADE.ALREADY.PENDING");
                        return;
                    }

                    var tradeTargetpend = pendings.Find(p => p.target == target || p.player == target);
                    if (tradeTargetpend != null)
                    {
                        Reply(player, "TRADE.TARGET.ALREADY.PENDING");
                        return;
                    }

                    if (!IsTeamate(player, target))
                    {
                        Reply(player, "GET.FRIENDS", target.displayName);
                        return;
                    }

                    if (config.mainSettings.TradeDistance > 0 && Vector3.Distance(player.transform.position, target.transform.position) < config.mainSettings.TradeDistance)
                    {
                        Reply(player, "GET.DISTANCE", target.displayName);
                        return;
                    }


                    pendings.Add(new TradePendings(player, config.mainSettings.getTime, target));
                    Reply(player, "PENDING.SENDER.FORMAT", target.displayName);
                    Reply(target, "PENDING.RECIEVER.FORMAT", player.displayName);
                    break;
                case "accept":
                case "yes":
                    if (!CanPlayerTrade(player)) return;
                    var tp = pendings.Find(p => p.target == player);
                    if (tp == null)
                    {
                        Reply(player, "TRADE.ACCEPT.PENDING.EMPTY");
                        return;
                    }
                    if (IsDuelPlayer(tp.target) || IsDuelPlayer(tp.player))
                    {
                        pendings.Remove(tp);
                        Reply(tp.player, "DENIED.DUEL");
                        Reply(tp.target, "DENIED.DUEL");
                        return;
                    }
                    pendings.Remove(tp);
                    TradeBox trade = TradeBox.Spawn();
                    if (trade == null) return;

                    tradeBoxes.Add(trade);
                    timer.Once(0.5f, () =>
                    {
                        if (tp.player == null || !tp.player.IsConnected) return;
                        if (tp.target == null || !tp.target.IsConnected) return;
                        trade.StartLoot(tp.player, tp.target);
                    });
                    break;
                case "cancel":
                case "no":
                    var pend = pendings.Find(p => p.target == player);
                    if (pend == null)
                    {
                        Reply(player, "TRADE.ACCEPT.PENDING.EMPTY");
                        return;
                    }
                    pendings.Remove(pend);

                    if (pend.player.IsConnected) Reply(pend.player, "PENDING.CANCEL.SENDER", player.displayName);
                    Reply(pend.target, "TRADE.CANCELED");
                    break;
            }
        }

        public class TradeBox : MonoBehaviour
        {
            public ShopFront shopFront;
            public BasePlayer player1, player2;

            void Awake()
            {
                shopFront = gameObject.GetComponent<ShopFront>();
                enabled = false;
            }

            public static TradeBox Spawn()
            {
                var storage = SpawnContainer(new Vector3());
                var box = storage.gameObject.AddComponent<TradeBox>();
                return box;
            }

            private static ShopFront SpawnContainer(Vector3 position)
            {
                ShopFront shopFront = GameManager.server.CreateEntity("assets/prefabs/building/wall.frame.shopfront/wall.frame.shopfront.metal.prefab", position, new Quaternion(), true) as ShopFront;
                if (shopFront == null) return null;
                shopFront.Spawn();
                shopFront.vendorInventory.capacity = 1;
                shopFront.customerInventory.capacity = 1;
                UnityEngine.Object.Destroy(shopFront.GetComponent<DestroyOnGroundMissing>());
                UnityEngine.Object.Destroy(shopFront.GetComponent<GroundWatch>());
                return shopFront;
            }

            public void StartLoot(BasePlayer player, BasePlayer target)
            {
                if (player == null || target == null)
                {
                    Destroy(this);
                    return;
                }
                player1 = player;
                player2 = target;
                shopFront.vendorInventory.capacity = ins.GetTradeSize(player.UserIDString);
                shopFront.customerInventory.capacity = ins.GetTradeSize(target.UserIDString);
                player.EndLooting();
                target.EndLooting();
                shopFront.vendorPlayer = player1;
                shopFront.customerPlayer = player2;
                if (!player1.net.subscriber.IsSubscribed(shopFront.net.group))
                    player1.net.subscriber.Subscribe(shopFront.net.group);
                if (!player2.net.subscriber.IsSubscribed(shopFront.net.group))
                    player2.net.subscriber.Subscribe(shopFront.net.group);
                SendEntity(player1, (BaseEntity)shopFront);
                SendEntity(player2, (BaseEntity)shopFront);
                SendEntity(player1, (BaseEntity)player2);
                SendEntity(player2, (BaseEntity)player1);
                StartLooting(player1);
                StartLooting(player2);
                shopFront.ResetTrade();
                shopFront.UpdatePlayers();
                enabled = true;
            }

            public void SendEntity(BasePlayer a, BaseEntity b, string reason = "")
            {
                if (Net.sv.write.Start())
                {
                    a.net.connection.validate.entityUpdates++;
                    BaseNetworkable.SaveInfo c = new BaseNetworkable.SaveInfo
                    {
                        forConnection = a.net.connection,
                        forDisk = false
                    }
                    ;
                    Net.sv.write.PacketID(Message.Type.Entities);
                    Net.sv.write.UInt32(a.net.connection.validate.entityUpdates);
                    b.ToStreamForNetwork(Net.sv.write, c);
                    Net.sv.write.Send(new SendInfo(a.net.connection));
                }
            }

            public void StartLooting(BasePlayer player)
            {
                player.inventory.loot.StartLootingEntity(shopFront, false);
                player.inventory.loot.AddContainer(shopFront.vendorInventory);
                player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", shopFront.panelName);
                shopFront.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                player.inventory.loot.AddContainer(shopFront.customerInventory);
                player.inventory.loot.SendImmediate();
            }

            public void PlayerStoppedLooting(BasePlayer player) => Destroy(this);

            public void OnDestroy()
            {
                if (player1 != null)
                    player1.EndLooting();
                if (player2 != null)
                    player2.EndLooting();
                if (shopFront != null && !shopFront.IsDestroyed)
                    shopFront.Kill();
            }

            void FixedUpdate()
            {
                if (!player1.net.subscriber.IsSubscribed(shopFront.net.group))
                {
                    SendEntity(player1, shopFront);
                    SendEntity(player1, player2);
                    player1.net.subscriber.Subscribe(shopFront.net.group);
                    shopFront.UpdatePlayers();
                }
                if (!player2.net.subscriber.IsSubscribed(shopFront.net.group))
                {
                    SendEntity(player2, shopFront);
                    SendEntity(player2, player1);
                    player2.net.subscriber.Subscribe(shopFront.net.group);
                    shopFront.UpdatePlayers();
                }
            }

            public void CustomCompleteTrade()
            {
                if (shopFront.vendorPlayer != null && shopFront.customerPlayer != null && shopFront.HasFlag(global::BaseEntity.Flags.Reserved1) && shopFront.HasFlag(global::BaseEntity.Flags.Reserved2))
                {
                    for (int i = shopFront.vendorInventory.capacity - 1; i >= 0; i--)
                    {
                        Item slot = shopFront.vendorInventory.GetSlot(i);
                        Item slot2 = shopFront.customerInventory.GetSlot(i);
                        if (shopFront.customerPlayer && slot != null)
                        {
                            player2.GiveItem(slot, global::BaseEntity.GiveItemReason.Generic);
                        }
                        if (shopFront.vendorPlayer && slot2 != null)
                        {
                            player1.GiveItem(slot2, global::BaseEntity.GiveItemReason.Generic);
                        }
                    }
                    global::Effect.server.Run(shopFront.transactionCompleteEffect.resourcePath, player1, 0u, new Vector3(0f, 1f, 0f), Vector3.zero, null, false);
                    global::Effect.server.Run(shopFront.transactionCompleteEffect.resourcePath, player2, 0u, new Vector3(0f, 1f, 0f), Vector3.zero, null, false);
                    ins.Reply(player1, "TRADE.SUCCESS");
                    ins.Reply(player2, "TRADE.SUCCESS");
                    ins.Cooldowns[player1] = DateTime.Now.AddSeconds(ins.GetPlayerCooldown(player1.UserIDString));
                    ins.Cooldowns[player2] = DateTime.Now.AddSeconds(ins.GetPlayerCooldown(player2.UserIDString));
                    ins.tradeBoxes.Remove(this);
                    Destroy(this);
                }
            }
        }

        private bool PlayerGetActiveTrade(BasePlayer player)
        {
            var contains = pendings.Find(p => p.target == player);
            return contains != null;
        }
    }
}