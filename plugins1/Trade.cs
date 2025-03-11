using Network;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Oxide.Plugins
{
    [Info("Trade", "OxideBro", "1.1.41")] public class Trade: RustPlugin
    {
        [PluginReference] Plugin Duel, NoteUI, Notify;
		
        public Timer mytimer;
        private Dictionary < BasePlayer, DateTime > Cooldowns = new Dictionary < BasePlayer, DateTime > ();
        private static Trade m_Instance;
        private Dictionary < string, TradeController > m_Boxes = new Dictionary < string, TradeController > ();
        private static Dictionary < string, ShopFront > boxes = new Dictionary < string, ShopFront > ();
        private static Dictionary < string, List < BasePlayer >> players = new Dictionary < string, List < BasePlayer >> ();
        private List < ulong > tradingPlayers = new List < ulong > ();
        private Dictionary < BasePlayer, BasePlayer > pendings = new Dictionary < BasePlayer, BasePlayer > ();
		
        bool getCupAuth = true;
        bool getCupSend = true;
        bool getFly = true;
        bool getSwim = true;
        bool getWound = true;
        int getTime = 15;
        private double CooldownTrade = 60f;
        public int getInt = 8;
        public string Permission = "trade.use";
        public bool UsePermission = false;
		
        private void LoadDefaultConfig()
        {
            GetConfig("Основное", "Запретить отправлять запрос в BuildingBlock", ref getCupSend);
            GetConfig("Основное", "Запретить принимать запрос в BuildingBlock", ref getCupAuth);
            GetConfig("Основное", "Запретить отправлять запрос в BuildingBlock", ref getCupSend);
            GetConfig("Основное", "Запретить использовать трейд в полёте", ref getFly);
            GetConfig("Основное", "Запретить использовать трейд в воде", ref getSwim);
            GetConfig("Основное", "Запретить использовать трейд в предсмертном состоянии", ref getWound);
            GetConfig("Основное", "Время ответа на предложения обмена (секунд)", ref getTime);
            GetConfig("Основное", "Задержка использования трейда (Cooldown - секунд)", ref CooldownTrade);
            GetConfig("Основное", "Количество активных слотов при обмене", ref getInt);
            GetConfig("Основное", "Привилегия на использование команды trade", ref Permission);
            GetConfig("Основное", "Разрешить использование трейда только если игрок имеет привилегию указаную в конфиге", ref UsePermission);
            SaveConfig();
        }
		
        private void GetConfig < T > (string menu, string Key, ref T var)
        {
            if (Config[menu, Key] != null)
            {
                var = (T) Convert.ChangeType(Config[menu, Key], typeof(T));
            }
            Config[menu, Key] =
                var;
        }
		
        private void Loaded()
        {
            m_Instance = this;
            lang.RegisterMessages(Messages, this);
            Messages = lang.GetMessages("en", this);
            LoadDefaultConfig();
            permission.RegisterPermission(Permission, this);
        }
		
        void Unload()
        {
            foreach(var trade in m_Boxes)
            {
                Destroy(trade.Key);
                UnityEngine.Object.Destroy(trade.Value, 0.1f);
            }
        }
		
        [ChatCommand("test2281337")]
        void test2281337(BasePlayer player, string command, string[] args)
        { 
            if (!player.IsAdmin)
            {
            SendNotify(player, "Нет доступа");
            }
            else
            {
                NoteUI?.Call("DrawInfoNote", player, "Трейд успешно состоялся");
            }
        }

        private void OnShopCompleteTrade(ShopFront shop)
        {
            var trade = m_Boxes.Select(p => p.Value).FirstOrDefault(p => p.shop == shop);
            if (trade != null)
            {
                SendNotify(trade.player1, Messages["TRADE.SUCCESS"]);
                SendNotify(trade.player2, Messages["TRADE.SUCCESS"]);
                Cooldowns[trade.player1] = DateTime.Now.AddSeconds(CooldownTrade);
                Cooldowns[trade.player2] = DateTime.Now.AddSeconds(CooldownTrade);

                NoteUI?.Call("DrawInfoNote", trade.player1, "Трейд успешно состоялся");
                NoteUI?.Call("DrawInfoNote", trade.player2, "Трейд успешно состоялся");
            }
        }

        private void SendNotify(BasePlayer player, string message, int type = 0)
        {
            if (Notify != null)
                Notify?.Call("SendNotify", player, type, message);
            else
                SendReply(player, message);
        }

        [ConsoleCommand("trade")] 
		void cmdTrade(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || arg.Args.Length == 0) return;
            var name = arg.Args[0];
            CmdChatTrade(player, string.Empty, new string[]
            {
                name
            });
        }
		
        public BasePlayer FindOnline(string nameOrUserId)
        {
            nameOrUserId = nameOrUserId.ToLower();
            foreach(BasePlayer activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.displayName.ToLower().Contains(nameOrUserId) || activePlayer.UserIDString == nameOrUserId) return activePlayer;
            }
            return null;
        }


        [ChatCommand("tradeY")]
        void CmdChatTradeY(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
           
            if (UsePermission && !permission.UserHasPermission(player.UserIDString, Permission))
            {
                SendNotify(player, Messages["DENIED.PERMISSIONON"]);
                return;
            }
            if (Cooldowns.ContainsKey(player))
            {
                double seconds = Cooldowns[player].Subtract(DateTime.Now).TotalSeconds;
                if (seconds >= 0)
                {
                    SendNotify(player, string.Format(Messages["COOLDOWN"], seconds));
                    return;
                }
            }
            if (player == null) return; if (getCupAuth)
            {
                if (!player.CanBuild())
                {
                    SendNotify(player, Messages["DENIED.PRIVILEGE"]);
                    return;
                }
            }
            BasePlayer player2;
            if (!pendings.TryGetValue(player, out player2))
            {
                SendNotify(player, Messages["TRADE.ACCEPT.PENDING.EMPTY"]);
                return;
            }
            if (IsDuelPlayer(player))
            {
                pendings.Remove(player2);
                pendings.Remove(player);
                SendNotify(player, Messages["DENIED.DUEL"]);
                SendNotify(player2, Messages["DENIED.DUEL"]);
                return;
            }
            if (IsDuelPlayer(player2))
            {
                pendings.Remove(player2);
                pendings.Remove(player);
                SendNotify(player2, Messages["DENIED.DUEL"]);
                SendNotify(player, Messages["DENIED.DUEL"]);
                return;
            }
            if (!CanPlayerTrade(player)) return; pendings.Remove(player); timer.Once(0.2f, () => OpenBox(player2, player));
        }
		
		[ChatCommand("trade")] 
		void CmdChatTrade(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            if (args.Length == 0 || args == null)
            {
                SendNotify(player, Messages["TRADE.HELP"]);
                return;
            }
            if (UsePermission && !permission.UserHasPermission(player.UserIDString, Permission))
            {
                SendNotify(player, Messages["DENIED.PERMISSIONON"]);
                return;
            }
            if (Cooldowns.ContainsKey(player))
            {
                double seconds = Cooldowns[player].Subtract(DateTime.Now).TotalSeconds;
                if (seconds >= 0)
                {
                    SendNotify(player, string.Format(Messages["COOLDOWN"], seconds));
                    return;
                }
            }
            switch (args[0])
            {
                default: if (!CanPlayerTrade(player))
                {
                    return;
                }if (IsDuelPlayer(player))
                {
                    SendNotify(player, Messages["DENIED.DUEL"]);
                    return;
                }
                var name = args[0];
                var target = FindOnline(name);
                if (target == null)
                {
                    SendNotify(player, string.Format(Messages["PLAYER.NOT.FOUND"], name));
                    return;
                }
                if (target == player)
                {
                    SendNotify(player, Messages["TRADE.TOYOU"]);
                    return;
                }
                if (UsePermission && !permission.UserHasPermission(target.UserIDString, Permission))
                {
                    SendNotify(player, string.Format(Messages["DENIED.PERMISSIOONTARGETN"], target.displayName));
                    return;
                }
                if (getCupSend)
                {
                    if (!player.CanBuild())
                    {
                        SendNotify(player, Messages["DENIED.PRIVILEGE"]);
                        return;
                    }
                }
                BasePlayer anotherTarget;
                if (pendings.TryGetValue(player, out anotherTarget))
                {
                    SendNotify(player, string.Format(Messages["TRADE.ALREADY.PENDING.ANOTHER.PLAYER"], anotherTarget.displayName));
                    return;
                }
                pendings[target] = player;SendNotify(player, string.Format(Messages["PENDING.SENDER.FORMAT"], target.displayName));SendNotify(target, string.Format(Messages["PENDING.RECIEVER.FORMAT"], player.displayName));mytimer = timer.Once(getTime, () =>
                {
                    if (target != null && player != null && player.IsConnected && target.IsConnected && pendings.ContainsKey(target))
                    {
                        pendings.Remove(target);
                        pendings.Remove(player);
                        SendNotify(player, Messages["PENDING.TIMEOUT.SENDER"]);
                        SendNotify(target, Messages["PENDING.TIMEOUT.RECIEVER"]);
                    }
                });
                return;
                case "accept":
                        if (player == null) return;if (getCupAuth)
                    {
                        if (!player.CanBuild())
                        {
                            SendNotify(player, Messages["DENIED.PRIVILEGE"]);
                            return;
                        }
                    }
                    BasePlayer player2;
                    if (!pendings.TryGetValue(player, out player2))
                    {
                        SendNotify(player, Messages["TRADE.ACCEPT.PENDING.EMPTY"]);
                        return;
                    }
                    if (IsDuelPlayer(player))
                    {
                        pendings.Remove(player2);
                        pendings.Remove(player);
                        SendNotify(player, Messages["DENIED.DUEL"]);
                        SendNotify(player2, Messages["DENIED.DUEL"]);
                        return;
                    }
                    if (IsDuelPlayer(player2))
                    {
                        pendings.Remove(player2);
                        pendings.Remove(player);
                        SendNotify(player2, Messages["DENIED.DUEL"]);
                        SendNotify(player, Messages["DENIED.DUEL"]);
                        return;
                    }
                    if (!CanPlayerTrade(player)) return;pendings.Remove(player);timer.Once(0.2f, () => OpenBox(player2, player));
                    return;
                case "yes":
                        if (player == null) return;if (getCupAuth)
                    {
                        if (!player.CanBuild())
                        {
                            SendNotify(player, Messages["DENIED.PRIVILEGE"]);
                            return;
                        }
                    }
                    if (!pendings.TryGetValue(player, out player2))
                    {
                        SendNotify(player, Messages["TRADE.ACCEPT.PENDING.EMPTY"]);
                        return;
                    }
                    if (IsDuelPlayer(player))
                    {
                        SendNotify(player, Messages["DENIED.DUEL"]);
                        SendNotify(player2, Messages["DENIED.DUEL"]);
                        return;
                    }
                    if (IsDuelPlayer(player2))
                    {
                        SendNotify(player2, Messages["DENIED.DUEL"]);
                        SendNotify(player, Messages["DENIED.DUEL"]);
                        return;
                    }
                    if (!CanPlayerTrade(player)) return;pendings.Remove(player);timer.Once(0.2f, () => OpenBox(player2, player));
                    return;
                case "cancel":
                        if (player == null) return;if (!pendings.TryGetValue(player, out player2))
                    {
                        SendNotify(player, Messages["TRADE.ACCEPT.PENDING.EMPTY"]);
                        return;
                    }
                    pendings.Remove(player);
                    if (player2 ? .IsConnected == true) SendNotify(player2, string.Format(Messages["PENDING.CANCEL.SENDER"], player.displayName));SendNotify(player2, Messages["TRADE.CANCELED"]);
                    return;
                case "no":
                        if (player == null) return;if (!pendings.TryGetValue(player, out player2))
                    {
                        SendNotify(player, Messages["TRADE.ACCEPT.PENDING.EMPTY"]);
                        return;
                    }
                    pendings.Remove(player);
                    if (player2 ? .IsConnected == true) SendNotify(player2, string.Format(Messages["PENDING.CANCEL.SENDER"], player.displayName));SendNotify(player2, Messages["TRADE.CANCELED"]);
                    return;
            }
        }
		
        private void RemovePending(BasePlayer player)
        {
            BasePlayer player2;
            if (!pendings.TryGetValue(player, out player2)) return;
            pendings.Remove(player);
            pendings.Remove(player2);
        }
		
        private bool IsDuelPlayer(BasePlayer player)
        {
            if (Duel == null) return false;
            var dueler = Duel.Call("IsPlayerOnActiveDuel", player);
            if (dueler is bool) return (bool) dueler;
            return false;
        }
		
        void OnPlayerLootEnd(PlayerLoot inventory)
        {
            var player = inventory.gameObject.ToBaseEntity();
            if (player == null) return;
            var box = m_Boxes.Select(p => p.Value).FirstOrDefault(p => p.player1 == player || p.player2 == player);
            if (box != null)
            {
                OnTradeCanceled(box.guid);
            }
        }
		
        private static void DrawUI(BasePlayer a)
        {
            CuiHelper.AddUi(a, @"[{""name"":""TradeBox_Button"",""parent"":""Overlay"",""components"":[{""type"":""UnityEngine.UI.Button"",""command"":""tradebox.button"",""color"":""1 1 1 0""},{""type"":""RectTransform"",""anchormin"":""0.65 0.1504"",""anchormax"":""0.9464 0.2455"",""offsetmin"":""0 0"",""offsetmax"":""1 1""}]},{""name"":""CuiElement"",""parent"":""Overlay"",""components"":[{""type"":""RectTransform"",""anchormin"":""0.052598808363463684 0.092059326594"",""anchormax"":""0.1046145666867 0.18685186552"",""offsetmin"":""0 0"",""offsetmax"":""1 1""}]},{""name"":""CuiElement"",""parent"":""Overlay"",""components"":[{""type"":""RectTransform"",""anchormin"":""0.052660845544 0.0926855466859"",""anchormax"":""0.104166467 0.18571845242"",""offsetmin"":""0 0"",""offsetmax"":""1 1""}]}]");
        }
		
        private static void DestroyUI(BasePlayer a)
        {
            CuiHelper.DestroyUi(a, "TradeBox_Button");
        }
		
        void DestroyUIPlayer(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "ContainerUI");
        }
		
        private static void cmdTradeButton(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            var shop = boxes.Select(p => p.Value).FirstOrDefault(p => p.customerPlayer == player || p.vendorPlayer == player);
            if (shop == null) return;
            if (shop.HasFlag(BaseEntity.Flags.Reserved3)) return;
            if (!shop.IsTradingPlayer(player))
            {
                return;
            }
            if (shop.vendorPlayer == null || shop.customerPlayer == null)
            {
                return;
            }
            if (shop.IsPlayerVendor(player))
            {
                if (shop.HasFlag(BaseEntity.Flags.Reserved1))
                {
                    shop.ResetTrade();
                }
                else
                {
                    shop.SetFlag(BaseEntity.Flags.Reserved1, true);
                    shop.vendorInventory.SetLocked(true);
                }
            }
            else if (shop.IsPlayerCustomer(player))
            {
                if (shop.HasFlag(BaseEntity.Flags.Reserved2))
                {
                    shop.ResetTrade();
                }
                else
                {
                    shop.SetFlag(BaseEntity.Flags.Reserved2, true);
                    shop.customerInventory.SetLocked(true);
                }
            }
            if (shop.HasFlag(BaseEntity.Flags.Reserved1) && shop.HasFlag(BaseEntity.Flags.Reserved2))
            {
                shop.SetFlag(BaseEntity.Flags.Reserved3, true);
                shop.Invoke(shop.CompleteTrade, 2f);
            }
        }
		
		[ConsoleCommand("tradebox.button")] 
		void cmdTradeButton1(ConsoleSystem.Arg a)
        {
            cmdTradeButton(a);
        }
		
        public static PluginTimers GetTimer()
        {
            return m_Instance.timer;
        }
		
        public static string Create(BasePlayer player1, BasePlayer player2, int getInt)
        {
            BaseEntity entity = GameManager.server.CreateEntity("assets/prefabs/building/wall.frame.shopfront/wall.frame.shopfront.metal.prefab", new Vector3(), new Quaternion(), true);
            entity.transform.position = Vector3.zero;
            UnityEngine.Object.Destroy(entity.GetComponent < DestroyOnGroundMissing > ());
            UnityEngine.Object.Destroy(entity.GetComponent < GroundWatch > ());
            entity.Spawn();
            ShopFront shopFront = (ShopFront) entity;
            shopFront.vendorInventory.capacity = getInt;
            shopFront.customerInventory.capacity = getInt;
            string guid = CuiHelper.GetGuid();
            boxes.Add(guid, shopFront);
            var SendNotify = 145;
            if (SendNotify == 0)
            {}
            players[guid] = new List < BasePlayer > ()
            {
                player1,
                player2
            };
            if (!player1.net.subscriber.IsSubscribed(shopFront.net.group)) player1.net.subscriber.Subscribe(shopFront.net.group);
            if (!player2.net.subscriber.IsSubscribed(shopFront.net.group)) player2.net.subscriber.Subscribe(shopFront.net.group);
            SendEntity(player1, (BaseEntity) shopFront);
            SendEntity(player2, (BaseEntity) shopFront);
            SendEntity(player1, (BaseEntity) player2);
            SendEntity(player2, (BaseEntity) player1);
            player1.EndLooting();
            player2.EndLooting();
            GetTimer().Once(0.1f, (Action)(() => StartLooting(guid, player1)));
            GetTimer().Once(0.5f, (Action)(() => StartLooting(guid, player2)));
            return guid;
        }
		
        static void SendEntity(BasePlayer a, BaseEntity b)
        {
            if (Net.sv.write.Start())
            {
                a.net.connection.validate.entityUpdates++;
                BaseNetworkable.SaveInfo c = new BaseNetworkable.SaveInfo
                {
                    forConnection = a.net.connection, forDisk = false
                };
                Net.sv.write.PacketID(Message.Type.Entities);
                Net.sv.write.UInt32(a.net.connection.validate.entityUpdates);
                b.ToStreamForNetwork(Net.sv.write, c);
                Net.sv.write.Send(new SendInfo(a.net.connection));
            }
        }
		
        public static T AddComponent < T > (string a) where T: Component
        {
            ShopFront b;
            if (!boxes.TryGetValue(a, out b))
            {
                throw new InvalidOperationException("AddBehaviour: TradeBox for {guid} not Found");
            }
            return b.gameObject.AddComponent < T > ();
        }
		
        public static void Destroy(string a)
        {
            ShopFront b;
            if (boxes.TryGetValue(a, out b))
            {
                if (players.ContainsKey(a))
                {
                    players[a].ForEach(DestroyUI);
                    players.Remove(a);
                }
                boxes.Remove(a);
                b.Kill();
            }
        }
		
        private void OnItemSplit(Item item, int amount)
        {
            if (item == null) return;
			var cont = item.GetRootContainer();
			if (cont == null) return;
            var container = cont.entityOwner as ShopFront;
            if (container != null && container is ShopFront)
                if (boxes.Values.Contains(container))
                {
                    if (container.vendorInventory != null && container.customerInventory != null)
                        if (container.vendorInventory.IsLocked() || container.customerInventory.IsLocked()) container.ResetTrade();
                }            
        }
		
        private void DropTrade(TradeController trade)
        {
            m_Boxes.Remove(trade.guid);
            tradingPlayers.Remove(trade.player1.userID);
            tradingPlayers.Remove(trade.player2.userID);
            Destroy(trade.guid);
            UnityEngine.Object.DestroyImmediate(trade);
        }
		
        private void OpenBox(BasePlayer player1, BasePlayer player2, ulong pIayerid = 2281488)
        {
            var guid = Create(player1, player2, getInt);
            var trade = AddComponent < TradeController > (guid);
            trade.Init(guid, player1, player2);
            m_Boxes.Add(guid, trade);
            tradingPlayers.Add(player1.userID);
            tradingPlayers.Add(player2.userID);
        }
		
        private void OnTradeCanceled(string guid)
        {
            TradeController trade;
            if (m_Boxes.TryGetValue(guid, out trade))
            {
                if (trade.shop.customerInventory != null)
                {
                    for (int i = trade.shop.customerInventory.itemList.Count - 1; i >= 0; i--)
                    {
                        trade.player1.GiveItem(trade.shop.customerInventory.itemList[i], BaseEntity.GiveItemReason.Generic);
                    }
                }
                if (trade.shop.vendorInventory != null)
                {
                    for (int i = trade.shop.vendorInventory.itemList.Count - 1; i >= 0; i--)
                    {
                        trade.player2.GiveItem(trade.shop.vendorInventory.itemList[i], BaseEntity.GiveItemReason.Generic);
                    }
                }
                DropTrade(trade);
            }
        }
		
        object CanMoveItem(Item item, PlayerInventory playerLoot, uint targetContainer)
        {
            if (playerLoot == null) return null;
            var cont1 = playerLoot.FindContainer(targetContainer);
            if (cont1 == null) return null;
            var player = playerLoot.containerMain.playerOwner;
            if (player == null) return null;
            if (cont1.entityOwner != null && cont1.entityOwner is ShopFront)
            {
                var shopfront = cont1.entityOwner.GetComponent < ShopFront > ();
                if (shopfront.IsPlayerCustomer(player) && shopfront.customerInventory.uid != targetContainer) return false;
                else if (shopfront.IsPlayerVendor(player) && shopfront.vendorInventory.uid != targetContainer) return false;
            }
            return null;
        }
		
        public static void StartLooting(string guid, BasePlayer player)
        {
            ShopFront shopFront;
            if (!boxes.TryGetValue(guid, out shopFront)) return;
            player.inventory.loot.StartLootingEntity((BaseEntity) shopFront, false);
            player.inventory.loot.AddContainer(shopFront.vendorInventory);
            player.inventory.loot.SendImmediate();
            player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", "shopfront");
            shopFront.DecayTouch();
            shopFront.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
            player.inventory.loot.AddContainer(shopFront.customerInventory);
            player.inventory.loot.SendImmediate();
            if ((UnityEngine.Object) shopFront.customerPlayer == (UnityEngine.Object) null) shopFront.customerPlayer = player;
            else shopFront.vendorPlayer = player;
            DrawUI(player);
            shopFront.ResetTrade();
            shopFront.UpdatePlayers();
        }
		
        void Reply(BasePlayer player, string langKey, params object[] args) => SendReply(player, Messages[langKey], args);
		
        bool CanPlayerTrade(BasePlayer player)
        {
            if (getSwim)
            {
                if (player.IsSwimming())
                {
                    SendNotify(player, Messages["DENIED.SWIMMING"]);
                    return false;
                }
            }
            if (getCupSend && getCupAuth)
            {
                if (!player.CanBuild())
                {
                    SendNotify(player, Messages["DENIED.PRIVILEGE"]);
                    return false;
                }
            }
            if (getFly)
            {
                if (!player.IsOnGround() || player.IsFlying)
                {
                    SendNotify(player, Messages["DENIED.FALLING"]);
                    return false;
                }
            }
            if (getWound)
            {
                if (player.IsWounded())
                {
                    SendNotify(player, Messages["DENIED.WOUNDED"]);
                    return false;
                }
            }
            if (Cooldowns.ContainsKey(player))
            {
                double seconds = Cooldowns[player].Subtract(DateTime.Now).TotalSeconds;
                if (seconds >= 0)
                {
                    SendNotify(player, string.Format(Messages["COOLDOWN"], seconds));
                    return false;
                }
            }
            if (IsDuelPlayer(player)) return false;
            var canTrade = Interface.Call("CanTrade", player);
            if (canTrade != null)
            {
                if (canTrade is string)
                {
                    SendNotify(player, Convert.ToString(canTrade));
                    return false;
                }
                SendNotify(player, Messages["DENIED.GENERIC"]);
                return false;
            }
            return true;
        }
		
        class TradeController: MonoBehaviour
        {
            public string guid;
            public ShopFront shop;
            public BasePlayer player1, player2;
            public void Init(string guid, BasePlayer player1, BasePlayer player2)
            {
                this.guid = guid;
                this.player1 = player1;
                this.player2 = player2;
            }
            private void Awake()
            {
                shop = GetComponent < ShopFront > ();
            }
        }
		
        Dictionary < string, string > Messages = new Dictionary < string, string > ()
        {
            {
                "DENIED.SWIMMING",
                "Недоступно, вы плаваете!"
            },
            {
                "DENIED.DUEL",
                "Недоступно, один из игроков на Duel!"
            },
            {
                "DENIED.PERMISSIONON",
                "Недоступно, у Вас нету прав на использование трейда!"
            },
            {
                "DENIED.PERMISSIOONTARGETN",
                "Недоступно, у {0} прав на использование трейда!"
            },
            {
                "DENIED.FALLING",
                "Недоступно, вы левитируете!"
            },
            {
                "DENIED.WOUNDED",
                "Недоступно, вы в предсмертном состоянии!"
            },
            {
                "DENIED.GENERIC",
                "Недоступно, заблокировано другим плагином!"
            },
            {
                "DENIED.PRIVILEGE",
                "Недоступно, вы в зоне Building Blocked!"
            },
            {
                "DENIED.PERMISSION",
                "Недоступно, вы в зоне Building Blocked!"
            },
            {
                "TRADE.HELP",
                "OLD ISLAND\nИспользуйте комманду <color=orange>/trade \"НИК\"</color> для обмена\nЧто бы принять обмен, введите: <color=orange>/trade yes</color> (или /trade accept)\nЧто бы отказаться от обмена введите: <color=orange>/trade no </color> (или /trade cancel)"
            },
            {
                "PLAYER.NOT.FOUND",
                "Игрок '{0}' не найден!"
            },
            {
                "TRADE.ALREADY.PENDING.ANOTHER.PLAYER",
                "Невозможно! Игрок '{0}' уже отправил вам предложение обмена!"
            },
            {
                "TRADE.ACCEPT.PENDING.EMPTY",
                "У вас нет входящих предложний обмена!"
            },
            {
                "TRADE.CANCELED",
                "Trade отменен!"
            },
            {
                "TRADE.TOYOU",
                "Нельзя отправлять запрос самому себе!"
            },
            {
                "TRADE.SUCCESS",
                "Trade успешно завершён!"
            },
            {
                "PENDING.RECIEVER.FORMAT",
                "Игрок '{0}' отправил вам предложние обмена\nДля принятия обмена используйте команду <color=orange>/trade yes</color>\nЧто бы отказаться введите <color=orange>/trade no</color>"
            },
            {
                "PENDING.SENDER.FORMAT",
                "Предложение обмена игроку '{0}' успешно отправлено, ожидайте..."
            },
            {
                "PENDING.TIMEOUT.SENDER",
                "Trade отменён! Причина: время истекло."
            },
            {
                "PENDING.TIMEOUT.RECIEVER",
                "Trade отменён! Причина: вы вовремя не приняли запрос."
            },
            {
                "PENDING.CANCEL.SENDER",
                "Trade отменён! Причина: игрок '{0}' отказался"
            },
            {
                "COOLDOWN",
                "Вы только недавно обменивались, подождите - {0:0} сек."
            },
        };
    }
} 