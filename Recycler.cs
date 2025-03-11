using System;
using UnityEngine;
using Oxide.Core;
using System.Text;
using System.Linq;
using Oxide.Core.Plugins;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Recycler", "M0rfin", "1.0.0")]
	[Description("Карманный переработчик ресурсов")]
    public class Recycler : RustPlugin
    {
        #region CLASSES

        public class RecyclerBox : MonoBehaviour
        {
            private const int SIZE = 1;

            StorageContainer storage;
            BasePlayer player;

            public void Init(StorageContainer storage, BasePlayer player)
            {
                this.storage = storage;
                this.player = player;
                storage.inventory.onItemAddedRemoved += (item, insert) => {if (insert)RecycleItem(item);};
            }

            public bool HasRecyclable(Item slot) => slot.info.Blueprint != null;
            void RecycleItem(Item slot)
            {
                    
                bool flag = false;
                if (!HasRecyclable(slot)) return;
                float single = 0.5f;
                if (slot.hasCondition)
                {
                    single = Mathf.Clamp01(single * slot.conditionNormalized * slot.maxConditionNormalized);
                }
                int num = 1;
                if (slot.amount > 1)
                {
                    num = slot.amount;
                }
                if (slot.info.Blueprint.scrapFromRecycle > 0)
                {
                    Item item = ItemManager.CreateByName("scrap", slot.info.Blueprint.scrapFromRecycle * num, (ulong)0);
                    MoveItemToOutput(item);
                }
                slot.UseItem(num);
                foreach (ItemAmount ingredient in slot.info.Blueprint.ingredients)
                {
                    float blueprint = (float)ingredient.amount / (float)slot.info.Blueprint.amountToCreate;
                    int num1 = 0;
                    if (blueprint > 1f)
                    {
                        num1 = Mathf.CeilToInt(Mathf.Clamp(blueprint * single * UnityEngine.Random.Range(1f, 1f), 1f, ingredient.amount) * (float)num);
                    }
                    else
                    {
                        for (int j = 0; j < num; j++)
                        {
                            if (UnityEngine.Random.Range(0f, 1f) <= single)
                            {
                                num1++;
                            }
                        }
                    }
                    if (num1 > 0)
                    {
                        MoveItemToOutput(ItemManager.Create(ingredient.itemDef, num1, (ulong)0));
                    }
                }
            }

            public void MoveItemToOutput(Item newItem)
            {
                if (!newItem.MoveToContainer(player.inventory.containerMain))
                    newItem.Drop(player.GetCenter(), player.GetDropVelocity());
            }

            public static RecyclerBox Spawn(BasePlayer player)
            {
                player.EndLooting();
                var storage = SpawnContainer(player);
                var box = storage.gameObject.AddComponent<RecyclerBox>();
                box.Init(storage, player);
                return box;
            }
            
            private static StorageContainer SpawnContainer(BasePlayer player)
            {
                var position = player.transform.position - new Vector3(0, 100, 0);
                
                var storage = GameManager.server.CreateEntity("assets/bundled/prefabs/static/recycler_static.prefab") as StorageContainer;
                if (storage == null) return null;
                storage.transform.position = position;
                storage.panelName = "recycler";
                ItemContainer container = new ItemContainer { playerOwner = player };
                container.ServerInitialize((Item)null, SIZE);
                if ((int)container.uid == 0)
                    container.GiveUID();
                storage.inventory = container;
                if (!storage) return null;
                storage.SendMessage("SetDeployedBy", player, (SendMessageOptions)1);
                storage.Spawn();
                return storage;
            }

            private void PlayerStoppedLooting(BasePlayer player)
            {
                Close();
            }

            public void Close()
            {
                foreach (var item in Items)
                    item.MoveToContainer(player.inventory.containerMain);
                ClearItems();
                storage.Kill();
            }

            public void StartLoot()
            {
                storage.SetFlag(BaseEntity.Flags.Open, true, false);
                player.inventory.loot.StartLootingEntity(storage, false);
                player.inventory.loot.AddContainer(storage.inventory);
                player.inventory.loot.SendImmediate();
                player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", storage.panelName);
                storage.SendNetworkUpdate();
                storage.DecayTouch();
            }

            public void Push(List<Item> items)
            {
                for (int i = items.Count - 1; i >= 0; i--)
                    items[i].MoveToContainer(storage.inventory);
            }

            public void ClearItems()
            {
                storage.inventory.itemList.Clear();
            }

            public List<Item> Items => storage.inventory.itemList.Where(i => i != null).ToList();

        }

        #endregion

        #region OXIDE HOOKS

        void OnServerInitialized()
        {
            permission.RegisterPermission(permissionName, this);
		}
		
	    const string permissionName = "recycler.use";
        
        #endregion

        #region COMMANDS

        [ChatCommand("rec")]
        void cmdChatRecycler(BasePlayer player, string cmd, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, permissionName))
            {
                SendReply(player,"У вас нет доступа к переработчику!");
				return;
            }
            if (InDuel(player)) return;
            timer.Once(0.3f, () => { OpenRecycler(player); });
        }

        #endregion

        #region CORE

        void OpenRecycler(BasePlayer player)
        {
            var box = RecyclerBox.Spawn(player);
            box.StartLoot();
        }

        #endregion

        #region EXTERNAL CALLS

        [PluginReference] Plugin Duels;

        bool InDuel(BasePlayer player) => Duels?.Call<bool>("inDuel", player) ?? false;

        #endregion
    }
}
