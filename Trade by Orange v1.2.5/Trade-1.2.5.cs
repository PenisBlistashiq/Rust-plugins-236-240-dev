///Скачано с дискорд сервера Rust Edit [PRO+]
///discord.gg/9vyTXsJyKR

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Network;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;


///Скачано с дискорд сервера Rust Edit [PRO+]
///discord.gg/9vyTXsJyKR

namespace Oxide.Plugins
{
    [Info("Trade", "discord.gg/9vyTXsJyKR", "1.2.5")]
    [Description("https://rustworkshop.space/resources/trade.130/")]
    public class Trade : RustPlugin
    {
        #region Vars

        private const string prefabEntity = "assets/bundled/prefabs/static/wall.frame.shopfront.metal.static.prefab";
        private const string prefabTestPlayer = "assets/prefabs/player/player.prefab";
        private static bool isTradingEntity(BaseEntity entity) => entity != null && entity.name == keyName;
        private const string keyName = "TradePluginFront";
        private static Trade plugin;
        private static bool testTradeFlag;

        #endregion

        #region Oxide Hooks

        private void Init()
        {
            plugin = this;

            foreach (var command in config.command)
            {
                cmd.AddChatCommand(command, this, nameof(cmdControlChat));
                cmd.AddConsoleCommand(command, this, nameof(cmdControlConsole));
            }

            foreach (var command in config.acceptCommand)
            {
                cmd.AddChatCommand(command, this, nameof(cmdControlChat));
                cmd.AddConsoleCommand(command, this, nameof(cmdControlConsole));
            }

            foreach (var command in config.declineCommand)
            {
                cmd.AddChatCommand(command, this, nameof(cmdControlChat));
                cmd.AddConsoleCommand(command, this, nameof(cmdControlConsole));
            }

            foreach (var value in config.permissions)
            {
                permission.RegisterPermission(value.permission, this);
            }
        }

        private void OnServerInitialized()
        {
            LoadData();
        }

        private void Unload()
        {
            SaveData();
        }

        private void OnShopCompleteTrade(ShopFront entity)
        {
            if (isTradingEntity(entity) == false)
            {
                return;
            }

            if (config.logTrade == true)
            {
                LogTrade(entity);
            }

            var player1 = entity.vendorPlayer;
            var player2 = entity.customerPlayer;
            var data1 = Data.Get(player1.UserIDString, true);
            var data2 = Data.Get(player2.UserIDString, true);
            data1.lastTradeUse = DateTime.UtcNow;
            data2.lastTradeUse = DateTime.UtcNow;
            RunEffect(player1.transform.position, config.effectOnCompleteTrade, player1);
            RunEffect(player2.transform.position, config.effectOnCompleteTrade, player2);

            NextTick(() =>
            {
                if (entity.IsValid() == true && entity.HasFlag(BaseEntity.Flags.Reserved3) == false)
                {
                    entity.Kill();
                }
            });
        }

        private void OnLootEntityEnd(BasePlayer player, ShopFront entity)
        {
            if (isTradingEntity(entity) == false)
            {
                return;
            }

            NextTick(() =>
            {
                if (entity.IsValid() == true && entity.HasFlag(BaseEntity.Flags.Reserved3) == false)
                {
                    var player1 = entity.vendorPlayer;
                    var player2 = entity.customerPlayer;
                    entity.ReturnPlayerItems(player1);
                    entity.ReturnPlayerItems(player2);
                    entity.Kill();
                }
            });
        }

        private object OnEntityVisibilityCheck(BaseEntity ent, BasePlayer player, uint id, string debugName,
            float maximumDistance)
        {
            if (isTradingEntity(ent) == true)
            {
                return true;
            }

            return null;
        }

        // private object CanAcceptItem(ItemContainer container, Item item, int targetPos)
        // {
        //     var entity = item.parentItem?.parent?.entityOwner?.GetComponent<ShopFront>();
        //     if (isTradingEntity(entity) == true)
        //     {
        //         var isMod = item.GetHeldEntity()?.GetComponent<ProjectileWeaponMod>() != null;
        //         if (item.parentItem != null && isMod)
        //         {
        //             return ItemContainer.CanAcceptResult.CannotAccept;
        //         }
        //     }
        //
        //     return null;
        // }

        // private void OnItemSplit(Item item, int amount)
        // {
        //     var entity = item.parent?.entityOwner?.GetComponent<ShopFront>();
        //     if (isTradingEntity(entity) == true)
        //     {
        //         entity.ResetTrade();
        //     }
        // }

        #endregion

        #region Commands

        private void cmdControlConsole(ConsoleSystem.Arg arg)
        {
            cmdControlChat(arg.Player(), arg.cmd.FullName, arg.Args);
        }

        private void cmdControlChat(BasePlayer player, string command, string[] args)
        {
            var perm = GetPermission(player.userID, config.permissions);
            if (perm == null)
            {
                SendMessage(player, MessageType.Permission);
                return;
            }

            if (HaveAbilityForTrading(player) == false)
            {
                return;
            }

            if (config.acceptCommand.Contains(command))
            {
                AcceptOrCancelRequest(player, false);
                return;
            }

            if (config.declineCommand.Contains(command))
            {
                AcceptOrCancelRequest(player, true);
                return;
            }

            var action = args?.Length > 0 ? args[0] : "unknown";
            switch (action.ToLower())
            {
                case "accept":
                case "+":
                case "yes":
                case "y":
                case "a":
                    AcceptOrCancelRequest(player, false);
                    return;
                case "cancel":
                case "-":
                case "no":
                case "n":
                case "c":
                    AcceptOrCancelRequest(player, true);
                    return;

                case "test":
                    if (player.IsAdmin == true)
                    {
                        TestTrade(player);
                    }

                    return;

                default:
                    SendTradeRequest(player, action, perm.cooldown);
                    return;
            }
        }

        #endregion

        #region Core

        private void SendTradeRequest(BasePlayer sender, string receiverString, int cooldown)
        {
            var component = sender.GetComponent<PendingTrade>();
            if (component != null)
            {
                SendMessage(sender, MessageType.RequestAlreadyPending);
                return;
            }

            var data = Data.Get(sender.UserIDString, true);
            var passed = (DateTime.UtcNow - data.lastTradeUse).TotalSeconds;
            var leftSeconds = cooldown - passed;
            if (leftSeconds > 0)
            {
                SendMessage(sender, MessageType.Cooldown, "{seconds}", leftSeconds.ToString("0.0"));
                return;
            }

            var target = FindPlayer(receiverString);
            if (target is string)
            {
                var text = target.ToString();
                if (text == receiverString)
                {
                    SendMessage(sender, MessageType.NoPlayers, "{name}", receiverString);
                    return;
                }

                SendMessage(sender, MessageType.MultiplePlayers, "{list}", text);
                return;
            }

            var targetPlayer = target as BasePlayer;
            if (targetPlayer == null)
            {
                SendMessage(sender, "Error: targetPlayer == null");
                return;
            }

            var pendingScript = targetPlayer.GetComponent<PendingTrade>();
            if (pendingScript != null)
            {
                SendMessage(sender, MessageType.RequestAlreadyPending);
                return;
            }

            targetPlayer.gameObject.AddComponent<PendingTrade>().Setup(sender);
            SendMessage(sender, MessageType.RequestSent, "{name}", targetPlayer.displayName);
            SendMessage(targetPlayer, MessageType.RequestReceived, "{name}", sender.displayName);
        }

        private void AcceptOrCancelRequest(BasePlayer player1, bool cancel)
        {
            var script = player1.GetComponent<PendingTrade>();
            if (script == null)
            {
                SendMessage(player1, MessageType.NoPending);
                return;
            }

            var player2 = script.callerPlayer;
            UnityEngine.Object.Destroy(script);
            if (cancel == true || HaveAbilityForTrading(player1) == false || HaveAbilityForTrading(player2) == false)
            {
                SendMessage(player1, MessageType.TradeCancelled);
                SendMessage(player2, MessageType.TradeCancelled);
                return;
            }

            if (player2 == null)
            {
                return;
            }
            

            BeginTrade(player1, player2);
        }

        private bool HaveAbilityForTrading(BasePlayer player)
        {
            if (player.CanBuild() == false && config.blockInBuildingPrivilege == true)
            {
                SendMessage(player, MessageType.CantRightNow);
                return false;
            }
            
            if (config.blockInRaidblock == true && InRaidBlock(player) == true)
            {
                SendMessage(player, MessageType.CantRightNow);
                return false;
            }

            var message = CanUseTrade(player);
            if (string.IsNullOrEmpty(message) == false)
            {
                SendMessage(player, message);
                return false;
            }

            return true;
        }

        private void BeginTrade(BasePlayer player1, BasePlayer player2)
        {
            SendMessage(player1, MessageType.TradeBegins, "{name1}", player1.displayName, "{name2}", player2.displayName);
            SendMessage(player2, MessageType.TradeBegins, "{name1}", player1.displayName, "{name2}", player2.displayName);
            var perm1 = GetPermission(player1.userID, config.permissions);
            var perm2 = GetPermission(player2.userID, config.permissions);
            if (perm1 == null)
            {
                SendMessage(player1, MessageType.Permission);
                return;
            }

            if (perm2 == null)
            {
                SendMessage(player2, MessageType.Permission);
                return;
            }

            var maxSize = perm1.size > perm2.size ? perm1.size : perm2.size;
            if (maxSize > 12)
            {
                maxSize = 12;
            }

            var front = GameManager.server.CreateEntity(prefabEntity, GetRandomPosition())?.GetComponent<ShopFront>();
            if (front == null)
            {
                return;
            }

            UnityEngine.Object.Destroy(front.GetComponent<DestroyOnGroundMissing>());
            UnityEngine.Object.Destroy(front.GetComponent<GroundWatch>());
            front.globalBroadcast = true;
            front.name = keyName;
            front.enableSaving = false;
            front.Spawn();    

            timer.Once(1f, () =>
            {
                if (player1.IsValid() == false || player2.IsValid() == false)
                {
                    return;
                }

                SendEntity(player1, player2);
                SendEntity(player2, player1);
                var customerInventory = front.customerInventory;
                var vendorInventory = front.inventory;
                customerInventory.capacity = maxSize;
                vendorInventory.capacity = maxSize;
                OpenContainer(player1, front, vendorInventory);
                OpenContainer(player2, front, vendorInventory);
                front.customerPlayer = player1;
                player1.inventory.loot.AddContainer(customerInventory);
                player1.inventory.loot.SendImmediate();
                front.vendorPlayer = player2;
                player2.inventory.loot.AddContainer(customerInventory);
                player2.inventory.loot.SendImmediate();
                front.UpdatePlayers();
                ModifyFront(front);
                MimicPlayer(front, player2.userID.IsSteamId() ? player1 : player2);
                
                if (config.startCooldownAtAccepting == true)
                {
                    var data1 = Data.Get(player1.UserIDString, true);
                    var data2 = Data.Get(player2.UserIDString, true);
                    data1.lastTradeUse = DateTime.UtcNow;
                    data2.lastTradeUse = DateTime.UtcNow;
                }
            });
        }

        private static Vector3 GetRandomPosition()
        {
            var randomX = Core.Random.Range(-1000, 1000);
            var randomZ = Core.Random.Range(-1000, 1000);
            var randomPos = new Vector3(randomX, -1000, randomZ);
            return randomPos;
        }

        private static void ModifyFront(ShopFront front)
        {
            front.customerInventory.MarkDirty();
            front.vendorInventory.MarkDirty();
            front.customerInventory.onItemAddedRemoved += (item, b) => UpdateItem(item, b);
            front.vendorInventory.onItemAddedRemoved  += (item, b) => UpdateItem(item, b);
            front.customerInventory.canAcceptItem += (item, i) =>
            {
                return CanAcceptItem(item, i, front, false);
            };
            front.vendorInventory.canAcceptItem += (item, i) =>
            {
                return CanAcceptItem(item, i, front, true);
            };
        }

        private static bool CanAcceptItem(Item item, int slot, ShopFront entity, bool forVendor)
        {
            var itemOwner = item.GetOwnerPlayer();
            var itemParent = item.parent;
            var word = forVendor ? "Vendor" : "Customer";
            var allowedPlayer = forVendor ? entity.vendorPlayer : entity.customerPlayer;
            var allowedInventory = forVendor ? entity.vendorInventory : entity.customerInventory;
            var flag1 = allowedPlayer == itemOwner;
            var flag2 = itemParent == allowedInventory;
            var flag3 = allowedInventory.GetSlot(slot) == null;
            var flagFinal = (flag1 || flag2) && flag3;
            
            if (itemOwner != null && itemOwner.IsAdmin == true)
            {
                itemOwner.ConsoleMessage($"Can accept '{item.info.shortname} x{item.amount}' in {word}: {flagFinal} ({flag1}, {flag2}, {flag3})");
            }
            
            if (flagFinal == true)
            {
                RemoveMods(item);
                return true;
            }
            else
            {
                return false;
            }
        }

        private void MimicPlayer(ShopFront front, BasePlayer player)
        {
            if (player.userID.IsSteamId() == true)
            {
                return;
            }
            
            var inventory = front.vendorPlayer == player ? front.vendorInventory : front.customerInventory;

            timer.Repeat(5f, 12, () =>
            {
                if (front.IsValid() == true && player.IsValid() == true)
                {
                    if (inventory.itemList.Count == 0)
                    {
                        var item = ItemManager.CreateByName("rifle.ak");
                        var sub = ItemManager.CreateByName("weapon.mod.8x.scope");
                        var item2 = ItemManager.CreateByName("wood", 1000);
                        sub.MoveToContainer(item.contents);
                        player.GiveItem(item);
                        player.GiveItem(item2);
                        item.MoveToContainer(inventory);
                        item2.MoveToContainer(inventory);
                    }
                        
                    front.AcceptClicked(new BaseEntity.RPCMessage {player = player});
                }
            });
        }

        private static void OpenContainer(BasePlayer player, BaseEntity entity, ItemContainer container)
        {
            player.EndLooting();
            var loot = player.inventory.loot;
            if (entity == null)
            {
                var position = player.transform.position + new Vector3(0, 500, 0);
                entity = GameManager.server.CreateEntity("assets/prefabs/visualization/sphere.prefab", position);
                if (entity != null)
                {
                    entity.enableSaving = false;
                    entity.Spawn();
                    container.entityOwner = entity;
                }
                else
                {
                    return;
                }
            }

            container.playerOwner = player;
            loot.Clear();
            loot.PositionChecks = false;
            loot.entitySource = entity;
            loot.itemSource = (Item) null;
            loot.MarkDirty();
            loot.AddContainer(container);
            loot.SendImmediate();
            player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", "shopfront");
            player.SendNetworkUpdateImmediate();
        }

        private void TestTrade(BasePlayer player1)
        {
            var position = new Vector3(1000, 2000, 1000);
            var player2 = GameManager.server.CreateEntity(prefabTestPlayer, position).GetComponent<BasePlayer>();
            if (player2 != null)
            {
                player2.Spawn();
                if (testTradeFlag == true)
                {
                    BeginTrade(player1, player2);
                }
                else
                {
                    BeginTrade(player2, player1);
                }

                var type = testTradeFlag ? "cusomer" : "vendor";
                testTradeFlag = !testTradeFlag;
                player1.ChatMessage($"Test trade flag is now: {testTradeFlag}, you are now {type}");
            }
        }

        private static void RemoveMods(Item item)
        {
            if (item == null)
            {
                return;
            }

            var parent = item.parent;
            if (parent != null && item.contents != null)
            {
                foreach (var mod in item.contents.itemList.ToArray())
                {
                    if (mod.MoveToContainer(parent) == false)
                    {
                        mod.Drop(item.parent.dropPosition, Vector3.zero);
                    }
                }
            }
        }

        private static void ResetFront(Item item)
        {
            var parent = item.parent?.entityOwner as ShopFront;
            if (parent != null)
            {
                parent.ResetTrade();
            }
        }

        private static void UpdateItem(Item item, bool action)
        {
            if (action)
            {
                item.OnDirty += ResetFront;
            }
            else
            {
                item.OnDirty -= ResetFront;
            }
        }

        #endregion

        #region Utils

        private static object FindPlayer(string targetName)
        {
            var match = new Predicate<BasePlayer>(x =>
                x.UserIDString == targetName || x.displayName.Contains(targetName, CompareOptions.IgnoreCase));
            var targets = BasePlayer.activePlayerList.Where(x => match(x)).ToArray();
            if (targets.Length == 0)
            {
                return targetName;
            }

            if (targets.Length > 1)
            {
                return targets.Select(x => x.displayName).ToSentence();
            }

            return targets[0];
        }

        private static void RunEffect(Vector3 position, string prefab, BasePlayer player = null)
        {
            var effect = new Effect();
            
            effect.Init(Effect.Type.Generic, position, Vector3.zero);
            effect.pooledString = prefab;

            var repeats = Convert.ToInt32(config.effectsVolume);
            if (repeats < 1)
            {
                repeats = 1;
                position -= new Vector3(0, Math.Abs(config.effectsVolume), 0);
            }

            for (var i = 0; i < repeats; i++)
            {
                if (player != null)
                {
                    EffectNetwork.Send(effect, player.net.connection);
                }
                else
                {
                    EffectNetwork.Send(effect);
                }
            }
        }

        private static void SendEntity(BasePlayer player, BaseEntity entity)
        {
            var connection = player.Connection;
            if (Network.Net.sv.write.Start() == false || connection == null) return;
            ++connection.validate.entityUpdates;
            var saveInfo = new BaseNetworkable.SaveInfo() {forConnection = connection, forDisk = false};
            Network.Net.sv.write.PacketID(Network.Message.Type.Entities);
            Network.Net.sv.write.UInt32(connection.validate.entityUpdates);
            entity.ToStreamForNetwork((Stream) Network.Net.sv.write, saveInfo);
            Network.Net.sv.write.Send(new SendInfo(connection));
        }
        
        private void LogTrade(ShopFront entity)
        {
            var player1 = entity.vendorPlayer;
            var player2 = entity.customerPlayer;
            var inventory1 = entity.vendorInventory;
            var inventory2 = entity.customerInventory;
            var text = $"\n{player1.displayName}[{player1.userID}] (Vendor) and {player2.displayName}[{player2.userID}] (Customer)\n" +
                       $"Finished trade at {DateTime.UtcNow} (UTC) with {inventory1.itemList.Count} + {inventory2.itemList.Count} items!\n";
 
            text += "Vendor sent:\n";
            foreach (var item in inventory1.itemList)
            {
                text += $" * {item.info.shortname} x{item.amount}\n";
            }
            
            text += "Customer sent:\n";
            foreach (var item in inventory2.itemList)
            {
                text += $" * {item.info.shortname} x{item.amount}\n";
            }
            
            LogToFile("results", text, this);
        }

        #endregion

        #region Configuration | 2.0.0

        private static ConfigData config = new ConfigData();

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Command")]
            public string[] command =
            {
                "trade",
                "t"
            };

            [JsonProperty(PropertyName = "Extra accept command")]
            public string[] acceptCommand =
            {
                "ta",
                "taccept"
            };

            [JsonProperty(PropertyName = "Extra decline command")]
            public string[] declineCommand =
            {
                "td",
                "tcancel",
            };

            [JsonProperty(PropertyName = "Effect on completing trade")]
            public string effectOnCompleteTrade = "assets/prefabs/building/wall.frame.shopfront/effects/metal_transaction_complete.prefab";

            [JsonProperty(PropertyName = "Effect on pending request")]
            public string effectOnRequest = "assets/bundled/prefabs/fx/invite_notice.prefab";

            [JsonProperty(PropertyName = "Log trades")]
            public bool logTrade = false;

            [JsonProperty(PropertyName = "Block trade in raidblock")]
            public bool blockInRaidblock = true;
 
            [JsonProperty(PropertyName = "Block in building privilege")]
            public bool blockInBuildingPrivilege = true;

            [JsonProperty(PropertyName = "Start cooldown after accepting trade")]
            public bool startCooldownAtAccepting = false;

            [JsonProperty(PropertyName = "Effects volume")]
            public float effectsVolume = 1f;

            [JsonProperty("Chat sender id")]
            public ulong senderId = 0;

            [JsonProperty(PropertyName = "Permissions")]
            public PermissionEntry[] permissions =
            {
                new PermissionEntry {permission = "trade.default", priority = 1, size = 2, cooldown = 600,},
                new PermissionEntry {permission = "trade.vip", priority = 2, size = 6, cooldown = 300,},
                new PermissionEntry {permission = "trade.top", priority = 3, size = 12, cooldown = 60,},
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<ConfigData>();
                if (config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                timer.Every(10f,
                    () =>
                    {
                        PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                    });
                LoadDefaultConfig();
                return;
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            config = new ConfigData();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion
        
        #region Language | 24.05.2020
        
        private Dictionary<object, string> langMessages = new Dictionary<object, string>
        {
            {MessageType.Usage, "Usage:\n/trade playerName\n/trade yes\n/trade no"}, 
            {MessageType.NoPending, "There are no pending trades!"},
            {MessageType.RequestAlreadyPending, "That player already have pending requests!"},
            {MessageType.TradeBegins, "Trade between {name1} and {name2} begins!"},
            {MessageType.RequestSent, "You sent trade request to {name}"},
            {MessageType.RequestReceived, "You received trade request from {name}"},
            {MessageType.TradeCancelled, "Trade was cancelled"}, 
            {MessageType.CantRightNow, "You can't do that right now"},
            {MessageType.Cooldown, "Cooldown for {seconds}"},
            {MessageType.Permission, "You don't have permission to do that!"},
            {MessageType.NoPlayers, "There are no players with that 'Name' or 'Steam ID' ({name})"},
            {MessageType.MultiplePlayers, "There are multiple players with that 'Name' :\n{list}"}
        };

        private enum MessageType
        {
            Usage,
            Permission,
            Cooldown,
            NoPending,
            RequestAlreadyPending,
            TradeBegins,
            TradeCancelled,
            RequestSent,
            RequestReceived,
            CantRightNow,
            MultiplePlayers,
            NoPlayers
        }
        
        protected override void LoadDefaultMessages()
        {
            var dictionary = new Dictionary<string, string>();
            foreach (var pair in langMessages)
            {
                var key = pair.Key.ToString();
                var value = pair.Value;
                dictionary.TryAdd(key, value);
            }
            lang.RegisterMessages(dictionary, this);
        }

        private string GetMessage(MessageType key, string playerID = null, params object[] args)
        {
            var keyString = key.ToString();
            var message = lang.GetMessage(keyString, this, playerID);
            if (message == keyString)
            {
                return $"{keyString} is not defined in lang!";
            }
            
            var organized = OrganizeArgs(args);
            message = ReplaceArgs(message, organized);
            return message;
        }
        
        private static Dictionary<string, object> OrganizeArgs(object[] args)
        {
            var dic = new Dictionary<string, object>();
            for (var i = 0; i < args.Length; i += 2)
            {
                var value = args[i].ToString();
                var nextValue = i + 1 < args.Length ? args[i + 1] : null;
                dic.TryAdd(value, nextValue);
            }

            return dic;
        }

        private static string ReplaceArgs(string message, Dictionary<string, object> args)
        {
            if (args == null || args.Count < 1)
            {
                return message;
            }
            
            foreach (var pair in args)
            {
                var s0 = "{" + pair.Key + "}";
                var s1 = pair.Key;
                var s2 = pair.Value != null ? pair.Value.ToString() : "null";
                message = message.Replace(s0, s2, StringComparison.InvariantCultureIgnoreCase);
                message = message.Replace(s1, s2, StringComparison.InvariantCultureIgnoreCase);
            }

            return message;
        }

        private void SendMessage(object receiver, MessageType key, params object[] args)
        {
            var userID = (receiver as BasePlayer)?.UserIDString;
            var message = GetMessage(key, userID, args);
            SendMessage(receiver, message);
        }
        
        private void SendMessage(object receiver, string message)
        {
            if (receiver == null)
            {
                Puts(message);
                return;
            }
            
            var console = receiver as ConsoleSystem.Arg;
            if (console != null)
            {
                SendReply(console, message);
                return;
            }
            
            var player = receiver as BasePlayer;
            if (player != null)
            {
                if (config.senderId == 0)
                {
                    player.ChatMessage(message);
                }
                else
                {
                    player.SendConsoleCommand("chat.add", (object) 2, (object) config.senderId, (object) message);
                }
               
                return;
            }
        }

        private void Broadcast(string message)
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                player.ChatMessage(message);
            }
        }

        #endregion
        
        #region Data | 2.2.0
        
        private static PluginData Data = new PluginData();
        private string dataFilename => $"{Name}\\data";
        private bool dataValid = false;

        private class DataEntry
        {
            [JsonProperty] private DateTime lastUse = DateTime.UtcNow;
            [JsonProperty] private int loadsCount = 0;
            [JsonIgnore] public double daysSinceLastUse => (DateTime.UtcNow - lastUse).TotalDays;
            [JsonProperty] public DateTime lastTradeUse;

            public void MarkUsed()
            {
                lastUse = DateTime.UtcNow;
                loadsCount++;
            }
        }
        
        private class PluginData
        {
            /* ### Values ### */
            // ReSharper disable once MemberCanBePrivate.Local
            [JsonProperty] private Dictionary<string, DataEntry> values = new Dictionary<string, DataEntry>();
            [JsonProperty] public readonly DateTime creationTime = SaveRestore.SaveCreatedTime;
            [JsonIgnore] private Dictionary<string, DataEntry> cache = new Dictionary<string, DataEntry>();
            
            /* ### Variables ### */
            [JsonIgnore] public bool needWipe => differentCreationDates;
            [JsonIgnore] public bool needCleanup => values.Count > minimalLimitForCleanup;
            [JsonIgnore] private bool differentCreationDates => (SaveRestore.SaveCreatedTime - creationTime).TotalHours > 1;
            [JsonIgnore] private static int cacheLifeSpan => 300;
            [JsonIgnore] public static int unusedDataLifeSpanDays => 14;
            [JsonIgnore] private static int minimalLimitForCleanup => 500;
            [JsonIgnore] public int valuesCount => values.Count;
            
            public DataEntry Get(object param, bool createNewOnMissing)
            {
                var key = GetKeyFrom(param);
                if (string.IsNullOrEmpty(key) == true)
                {
                    return null;
                }
                
                var value = (DataEntry) null;
                if (cacheLifeSpan > 0 && cache.TryGetValue(key, out value) == true)
                {
                    return value;
                }

                if (values.TryGetValue(key, out value) == false && createNewOnMissing == true)
                {
                    value = new DataEntry();
                    values.Add(key, value);
                }

                if (value != null)
                {
                    value.MarkUsed();

                    if (cacheLifeSpan > 0)
                    {
                        cache.TryAdd(key, value);
                    }
                }
                
                return value;
            }
            
            public void Set(object param, DataEntry value)
            {
                var key = GetKeyFrom(param);
                if (string.IsNullOrEmpty(key) == true)
                {
                    return;
                }

                if (value == null)
                {
                    if (values.ContainsKey(key) == true)
                    {
                        values.Remove(key);
                    }
                    
                    if (cache.ContainsKey(key) == true)
                    {
                        cache.Remove(key);
                    }
                }
                else
                {
                    if (values.TryAdd(key, value) == false)
                    {
                        values[key] = value;
                    
                        if (cache.ContainsKey(key) == true)
                        {
                            cache[key] = value;
                        }
                    }
                }
            }

            public void Cleanup()
            {
                var keys = new List<string>();
                foreach (var pair in values)
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    
                    if (value.daysSinceLastUse > unusedDataLifeSpanDays)
                    {
                        keys.Add(key);
                    }
                }

                foreach (var key in keys)
                {
                    Set(key, null);
                }
            }

            public void ResetCache()
            {
                cache.Clear();
            }

            private static string GetKeyFrom(object obj)
            {
                if (obj == null)
                {
                    return null;
                }

                if (obj is string)
                {
                    return obj as string;
                }

                if (obj is BasePlayer)
                {
                    return (obj as BasePlayer).UserIDString;
                }

                if (obj is BaseNetworkable)
                {
                    return (obj as BaseNetworkable).net?.ID.ToString();
                }

                return obj.ToString();
            }
        }

        private void LoadData()
        {
            try
            {
                Data = Interface.Oxide.DataFileSystem.ReadObject<PluginData>($"{dataFilename}");
                
                if (Data.needWipe == true)
                {
                    Interface.Oxide.DataFileSystem.WriteObject($"{dataFilename}_old", Data);
                    Data = new PluginData();
                    PrintWarning($"Data was wiped by auto-wiping function (Old: {Data.creationTime}, New: {SaveRestore.SaveCreatedTime})");
                }

                if (Data.needCleanup == true)
                {
                    var oldCount = Data.valuesCount;
                    Data.Cleanup();
                    var newCount = Data.valuesCount;
                    PrintWarning($"Removed {oldCount - newCount} values that are older than {PluginData.unusedDataLifeSpanDays} days (Was: {oldCount}, Now: {newCount})");
                }

                dataValid = true;
                timer.Every(Core.Random.Range(500, 700), SaveData);
                SaveData();
            }
            catch (Exception e)
            {
                Data = new PluginData();
                dataValid = false;

                for (var i = 0; i < 5; i++)
                {
                    PrintError("!!! CRITICAL DATA ERROR !!!\n * Data was not loaded!\n * Data auto-save was disabled!");
                }
                
                LogToFile("errors", $"\n\nError: {e.Message}\n\nTrace: {e.StackTrace}\n\n", this);
            }
        }

        private void SaveData()
        {
            if (Data != null && dataValid == true)
            {
                Data.ResetCache();
                Interface.Oxide.DataFileSystem.WriteObject(dataFilename, Data);
            }
        }

        #endregion

        #region Permission Support

        private class PermissionEntry
        {
            [JsonProperty(PropertyName = "Permission")]
            public string permission;

            [JsonProperty(PropertyName = "Priority")]
            public int priority;

            [JsonProperty(PropertyName = "Size")] 
            public int size;

            [JsonProperty(PropertyName = "Cooldown")]
            public int cooldown;
        }

        private PermissionEntry GetPermission(ulong playerID, PermissionEntry[] permissions)
        {
            var value = (PermissionEntry) null;
            var idString = playerID.ToString();
            var num = -1;
            foreach (var entry in permissions)
            {
                if (permission.UserHasPermission(idString, entry.permission) && entry.priority > num)
                {
                    num = entry.priority;
                    value = entry;
                }
            }

            if (playerID.IsSteamId() == false)
            {
                value = permissions.FirstOrDefault();
            }

            return value;
        }

        #endregion

        #region Scripts

        private class PendingTrade : MonoBehaviour
        {
            private BasePlayer targetPlayer;
            public BasePlayer callerPlayer;

            private void Awake()
            {
                targetPlayer = GetComponent<BasePlayer>();
            }

            private void Start()
            {
                RunEffect(targetPlayer.transform.position, config.effectOnRequest, targetPlayer);
                RunEffect(callerPlayer.transform.position, config.effectOnRequest, callerPlayer);
                Invoke(nameof(TimedDestroy), 30f);
            }

            public void Setup(BasePlayer v2)
            {
                callerPlayer = v2;
            }

            public void TimedDestroy()
            {
                Destroy(this);
                plugin.SendMessage(targetPlayer, MessageType.TradeCancelled);
            }
        }

        #endregion

        #region NoEscape API

        [PluginReference] private Plugin NoEscape;

        private bool InRaidBlock(BasePlayer player)
        {
            return NoEscape?.Call<bool>("IsRaidBlocked", player) ?? false;
        }

        private static string CanUseTrade(BasePlayer player)
        {
            var result = Interface.CallHook("CanTrade", player);
            return result?.ToString();
        }

        #endregion
    }
}
