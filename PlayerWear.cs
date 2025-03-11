using System;
using System.Collections.Generic;
using System.Globalization;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Color = UnityEngine.Color;
using Newtonsoft.Json;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("PlayerWear", "Drop Dead", "1.0.0")]
    public class PlayerWear : RustPlugin
    {
        public Dictionary<ulong, string> command = new Dictionary<ulong, string>();

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject($"{Title}/Players", command);
        }

        private void LoadData()
        {
            try
            {
                command = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, string>>($"{Title}/Players");
            }
            catch (Exception e)
            {
                PrintError(e.ToString());
            }

            if (command == null) command = new Dictionary<ulong, string>();
        }

        public class items
        {
            [JsonProperty("Шортнейм")]
            public string shortname = "";
            [JsonProperty("Количество")]
            public int amount = 1;
            [JsonProperty("Скин")]
            public ulong skinid = 0;
            [JsonProperty("Контейнер (wear/belt/main)")]
            public string container = "wear";
            [JsonProperty("Заблокировать перемещение?")]
            public bool move = false;
            [JsonProperty("Заблокировать дроп?")]
            public bool drop = false;
        }

        private PluginConfig cfg;

        public class PluginConfig
        {
            [JsonProperty("Предметы по группам")]
            public Dictionary<string, List<items>> perms = new Dictionary<string, List<items>>
            {
                ["raid"] = new List<items> { new items { shortname = "rifle.ak", amount = 1, skinid = 0, container = "belt" } },
                ["admin"] = new List<items> { new items { shortname = "ammo.rifle", amount = 1, skinid = 0, container = "main" } },
            };
        }

        private void Init()
        {
            cfg = Config.ReadObject<PluginConfig>();
            Config.WriteObject(cfg);
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(new PluginConfig(), true);
        }

        void OnServerInitialized()
        {
            foreach (var player in BasePlayer.activePlayerList) OnPlayerConnected(player);
        }

        void Unload()
        {
            SaveData();
        }

        void OnServerSave()
        {
            SaveData();
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerConnected(player));
                return;
            }

            CheckPlayer(player);
        }

        void CheckPlayer(BasePlayer player)
        {
            if (player == null) return;
            if (!command.ContainsKey(player.userID)) command.Add(player.userID, "raid");

            player.inventory.Strip();

            var items = cfg.perms[command[player.userID]];
            if (items == null) return;
            foreach (var item in items)
            {
                var newitem = ItemManager.CreateByName(item.shortname, item.amount, item.skinid);
                if (newitem == null) continue;
                if (item.container == "wear") player.inventory.containerWear.Insert(newitem);//newitem.MoveToContainer(player.inventory.containerWear);
                if (item.container == "belt") player.inventory.containerBelt.Insert(newitem);//newitem.MoveToContainer(player.inventory.containerBelt);
                if (item.container == "main") player.inventory.containerMain.Insert(newitem);//newitem.MoveToContainer(player.inventory.containerMain);
            }
        }

        void OnPlayerRespawned(BasePlayer player)
        {
            CheckPlayer(player);
        }

        object CanMoveItem(Item item, PlayerInventory playerLoot, uint targetContainer, int targetSlot, int amount)
        {
            if (item == null) return null;
            foreach (var itemz in cfg.perms)
            {
                foreach (var items in itemz.Value)
                {
                    if (items.shortname == item.info.shortname && items.move) return false;
                }
            }
            return null;
        }

        object OnItemAction(Item item, string action, BasePlayer player)
        {
            if (player == null || item == null) return null;
            if (action != "drop") return null;
            foreach (var itemz in cfg.perms)
            {
                foreach (var items in itemz.Value)
                {
                    if (items.shortname == item.info.shortname && items.drop) return false;
                }
            }
            return null;
        }

        private object CanUseLockedEntity(BasePlayer player, BaseLock @lock)
        {
            if (player == null || @lock == null || !@lock.IsLocked())
                return null;
            var parentEntity = @lock.GetParentEntity();
            var ownerID = @lock.OwnerID.IsSteamId() ? @lock.OwnerID : parentEntity != null ? parentEntity.OwnerID : 0;
            if (!ownerID.IsSteamId() || ownerID == player.userID)
                return null;
            if (player.IsAdmin) return true;
            if (@lock is CodeLock)
            {
                if (command[ownerID] == "admin" && command[player.userID] == "admin")
                {
                    var codeLock = @lock as CodeLock;
                    var whitelistPlayers = (List<ulong>)codeLock.guestPlayers;
                    if (!whitelistPlayers.Contains(player.userID))
                        whitelistPlayers.Add(player.userID);
                }
            }
            return null;
        }

        [ChatCommand("events")]
        void EventCmd(BasePlayer player, string commands, string[] args)
        {
            if (!player.IsAdmin) return;

            if (args.Length < 2)
            {
                player.ChatMessage("/event raid ник - добавить игрока в команду рейдеров\n/event admin ник - добавить игрока в команду админов");
                return;
            }

            if (args[0] != "raid" && args[0] != "admin")
            {
                player.ChatMessage("Вы не правильно указали группу в которую перенести игрока");
                return;
            }

            var target = BasePlayer.Find(args[1]);
            if (target == null)
            {
                player.ChatMessage("Игрок не найден, попробуйте уточнить имя или SteamID");
                return;
            }
            if (command.ContainsKey(target.userID))
            {
                if (command[target.userID] == args[0])
                {
                    player.ChatMessage($"Игрок {target.displayName} уже находится в той команде в которую вы пытаетесь его перенести");
                    return;
                }
                command[target.userID] = args[0];
                player.ChatMessage($"Вы успешно перенесли игрока {target.displayName} в другую команду. Его лут обновлён");
                CheckPlayer(target);
            }
        }
    }
}