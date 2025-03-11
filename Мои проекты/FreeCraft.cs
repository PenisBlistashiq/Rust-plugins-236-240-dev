using Oxide.Core;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("FreeCraft", "YourName", "1.0.0")]
    public class FreeCraft : RustPlugin
    {
        private Dictionary<ulong, bool> freeCraftingPlayers = new Dictionary<ulong, bool>();
        private readonly Dictionary<ulong, BaseEntity> playerWorkbenches = new Dictionary<ulong, BaseEntity>();
        private const string WorkbenchPrefab = "assets/prefabs/deployable/tier 3 workbench/workbench3.deployed.prefab";
        /// <summary>
        /// Множитель для количества ресурсов
        /// </summary>
        private const int ResourceMultiplier = 1000;

        private void Init()
        {
            permission.RegisterPermission("freecraft.admin", this);
            LoadData();
        }

        private void OnServerInitialized()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (playerWorkbenches.ContainsKey(player.userID))
                {
                    SpawnWorkbench(player);
                }
            }
        }

        private void Unload()
        {
            foreach (BaseEntity workbench in playerWorkbenches.Values)
            {
                if (workbench?.IsDestroyed == false)
                {
                    workbench.Kill();
                }
            }
            SaveData();
        }

        [ChatCommand("freecrafting")]
        private void FreeCraftingCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "freecraft.admin"))
            {
                SendReply(player, "У вас нет прав на использование этой команды!");
                return;
            }

            if (args.Length == 0)
            {
                SendReply(player, "Использование: /freecrafting <ник/steamid>");
                return;
            }

            BasePlayer targetPlayer = FindPlayer(args[0]);
            if (targetPlayer == null)
            {
                SendReply(player, "Игрок не найден!");
                return;
            }

            ulong targetId = targetPlayer.userID;
            if (!freeCraftingPlayers.ContainsKey(targetId))
            {
                freeCraftingPlayers[targetId] = true;
                SendReply(player, $"Бесплатный крафт включен для игрока {targetPlayer.displayName}");
                SendReply(targetPlayer, "Для вас включен бесплатный крафт!");
            }
            else
            {
                freeCraftingPlayers.Remove(targetId);
                SendReply(player, $"Бесплатный крафт выключен для игрока {targetPlayer.displayName}");
                SendReply(targetPlayer, "Бесплатный крафт отключен!");
            }
            SaveData();
        }

        [ChatCommand("noworkbench")]
        private void NoWorkbenchCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "freecraft.admin"))
            {
                SendReply(player, "У вас нет прав на использование этой команды!");
                return;
            }

            if (args.Length == 0)
            {
                SendReply(player, "Использование: /noworkbench <ник/steamid>");
                return;
            }

            BasePlayer targetPlayer = FindPlayer(args[0]);
            if (targetPlayer == null)
            {
                SendReply(player, "Игрок не найден!");
                return;
            }

            ulong targetId = targetPlayer.userID;
            if (!playerWorkbenches.ContainsKey(targetId))
            {
                SpawnWorkbench(targetPlayer);
                SendReply(player, $"Крафт без верстака включен для игрока {targetPlayer.displayName}");
                SendReply(targetPlayer, "Для вас включен крафт без верстака!");
            }
            else
            {
                RemoveWorkbench(targetPlayer);
                SendReply(player, $"Крафт без верстака выключен для игрока {targetPlayer.displayName}");
                SendReply(targetPlayer, "Крафт без верстака отключен!");
            }
            SaveData();
        }

        private void SpawnWorkbench(BasePlayer player)
        {
            RemoveWorkbench(player);

            BaseEntity workbench = GameManager.server.CreateEntity(WorkbenchPrefab, player.transform.position + new Vector3(0f, -1f, 0f));
            if (workbench == null)
            {
                return;
            }

            workbench.Spawn();
            workbench.enableSaving = false;
            workbench.GetComponent<BoxCollider>().enabled = false;
            workbench.GetComponent<MeshRenderer>().enabled = false;

            playerWorkbenches[player.userID] = workbench;
        }

        private void RemoveWorkbench(BasePlayer player)
        {
            BaseEntity workbench;
            if (playerWorkbenches.TryGetValue(player.userID, out workbench))
            {
                if (workbench?.IsDestroyed == false)
                {
                    workbench.Kill();
                }
                playerWorkbenches.Remove(player.userID);
            }
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            if (player == null)
            {
                return;
            }

            RemoveWorkbench(player);
        }

        private void OnTick()
        {
            foreach (KeyValuePair<ulong, BaseEntity> entry in playerWorkbenches)
            {
                BasePlayer player = BasePlayer.FindByID(entry.Key);
                BaseEntity workbench = entry.Value;

                if (player == null || workbench?.IsDestroyed != false)
                {
                    continue;
                }

                Vector3 newPos = player.transform.position + new Vector3(0f, -1f, 0f);
                if (workbench.transform.position != newPos)
                {
                    workbench.transform.position = newPos;
                }
            }
        }

        private BasePlayer FindPlayer(string nameOrId)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                string playerName = player.displayName.ToLowerInvariant();
                string searchName = nameOrId.ToLowerInvariant();

                if (playerName.Contains(searchName) ||
                    player.UserIDString.Equals(nameOrId, StringComparison.OrdinalIgnoreCase))
                {
                    return player;
                }
            }
            return null;
        }

        private object OnItemCraft(ItemCrafter itemCrafter, ItemBlueprint blueprint, int amount)
        {
            if (itemCrafter == null)
            {
                return null;
            }

            if (blueprint == null)
            {
                return null;
            }

            if (blueprint.ingredients == null)
            {
                return null;
            }

            BasePlayer player = itemCrafter.GetComponent<BasePlayer>();
            if (player == null)
            {
                return null;
            }

            if (freeCraftingPlayers.ContainsKey(player.userID))
            {
                foreach (ItemAmount ingredient in blueprint.ingredients)
                {
                    PlayerInventory inventory = player.inventory.GetComponent<PlayerInventory>();
                    if (inventory == null)
                    {
                        continue;
                    }

                    if (inventory.containerMain == null)
                    {
                        continue;
                    }

                    inventory.containerMain.AddItem(ingredient.itemDef, (int)(ingredient.amount * amount * ResourceMultiplier), 0);
                }
            }

            return null;
        }

        private object CanCraft(ItemCrafter itemCrafter, ItemBlueprint blueprint, int amount)
        {
            if (itemCrafter == null)
            {
                return null;
            }

            if (blueprint == null)
            {
                return null;
            }

            BasePlayer player = itemCrafter.GetComponent<BasePlayer>();
            return player == null ? (object)null : freeCraftingPlayers.ContainsKey(player.userID) || (object)null;
        }

        private void SaveData()
        {
            if (freeCraftingPlayers != null)
            {
                Interface.Oxide.DataFileSystem.WriteObject($"{Name}/freecrafting", freeCraftingPlayers);
            }

            Dictionary<ulong, bool> workbenchPlayers = new Dictionary<ulong, bool>();
            foreach (ulong player in playerWorkbenches.Keys)
            {
                workbenchPlayers[player] = true;
            }
            Interface.Oxide.DataFileSystem.WriteObject($"{Name}/workbenches", workbenchPlayers);
        }

        private void LoadData()
        {
            freeCraftingPlayers = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, bool>>($"{Name}/freecrafting") ?? new Dictionary<ulong, bool>();
            Dictionary<ulong, bool> workbenchPlayers = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, bool>>($"{Name}/workbenches") ?? new Dictionary<ulong, bool>();

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (workbenchPlayers.ContainsKey(player.userID))
                {
                    SpawnWorkbench(player);
                }
            }
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("New config created");
        }
    }
}
