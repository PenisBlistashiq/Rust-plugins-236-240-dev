using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oxide.Core.Libraries.Covalence;
using Oxide.Plugins.TreePlanterEx;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Tree Planter", "Bazz3l", "1.2.2")]
    [Description("Buy and plant trees in building authed areas using in-game currency.")]
    public class TreePlanter : CovalencePlugin
    {
        [PluginReference] Plugin ServerRewards, Economics, Clans;

        #region Fields

        private const string PERM_USE = "treeplanter.use";
        private PluginConfig _config;

        #endregion

        #region Config

        protected override void LoadDefaultConfig() => _config = PluginConfig.DefaultConfig();

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _config = Config.ReadObject<PluginConfig>();

                if (_config == null)
                {
                    throw new JsonException();
                }

                if (_config.ToDictionary().Keys
                    .SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys)) return;

                PrintWarning("Loaded updated config.");

                SaveConfig();
            }
            catch
            {
                PrintWarning("Loaded default config.");

                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(_config, true);

        private class PluginConfig
        {
            [JsonProperty("UseServerRewards (use server rewards as currency)")]
            public bool UseServerRewards;

            [JsonProperty("UseEconomics (use economics as currency)")]
            public bool UseEconomics;

            [JsonProperty("UseCurrency (use custom items as currency, by specifying the CurrencyItem)")]
            public bool UseCurrency;

            [JsonProperty("CurrencyItem (set an item id to use as currency, default is set to scrap)")]
            public int CurrencyItem;

            [JsonProperty("CurrencySkinID (set an skin id to use as currency, default is set to 0)")]
            public ulong CurrencySkinID;

            [JsonProperty("EnableOwner (enables owners to chop down trees)")]
            public bool EnableOwner;

            [JsonProperty("EnableClan (enables clan members to chop down trees)")]
            public bool EnableClan;

            [JsonProperty("BlockedAgriItems (specify which items should only be placed in a planter box)")]
            public Dictionary<string, bool> AgriBlocked;

            [JsonProperty("TreeConfigs (list of available trees to purchase)")]
            public List<TreeConfig> TreeConfigs;

            [JsonIgnore]
            private Dictionary<string, TreeConfig> _availableConfigs;

            [JsonIgnore]
            public Dictionary<string, TreeConfig> AvailableConfigs
            {
                get {
                    if (_availableConfigs != null) return _availableConfigs;

                    _availableConfigs = new Dictionary<string, TreeConfig>(StringComparer.InvariantCultureIgnoreCase);

                    TreeConfigs.ForEach(x => _availableConfigs[x.Name] = x);

                    return _availableConfigs;
                }
            }

            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig
                {
                    UseServerRewards = false,
                    UseEconomics = false,
                    UseCurrency = true,
                    EnableOwner = false,
                    EnableClan = false,
                    CurrencyItem = -932201673,

                    AgriBlocked = new Dictionary<string, bool>
                    {
                        {"seed.black.berry", true},
                        {"seed.blue.berry", true},
                        {"seed.green.berry", true},
                        {"seed.yellow.berry", true},
                        {"seed.white.berry", true},
                        {"seed.red.berry", true},
                        {"seed.corn", true},
                        {"clone.corn", true},
                        {"seed.pumpkin", true},
                        {"clone.pumpkin", true},
                        {"seed.hemp", true},
                        {"clone.hemp", true}
                    },

                    TreeConfigs = new List<TreeConfig>
                    {
                        new TreeConfig("oak a",
                            "assets/bundled/prefabs/autospawn/resource/v3_temp_field/oak_e.prefab"),
                        new TreeConfig("oak b",
                            "assets/bundled/prefabs/autospawn/resource/v3_temp_field_large/oak_b.prefab"),
                        new TreeConfig("oak c",
                            "assets/bundled/prefabs/autospawn/resource/v3_temp_field_large/oak_c.prefab"),

                        new TreeConfig("birch a",
                            "assets/bundled/prefabs/autospawn/resource/v3_temp_forest/birch_large_temp.prefab"),
                        new TreeConfig("birch b",
                            "assets/bundled/prefabs/autospawn/resource/v3_temp_beachside/birch_small_temp.prefab"),
                        new TreeConfig("birch c",
                            "assets/bundled/prefabs/autospawn/resource/v2_temp_forest/birch_large_temp.prefab"),

                        new TreeConfig("douglas a",
                            "assets/bundled/prefabs/autospawn/resource/v3_temp_forest_pine/douglas_fir_a.prefab"),
                        new TreeConfig("douglas b",
                            "assets/bundled/prefabs/autospawn/resource/v3_temp_forest_pine/douglas_fir_b.prefab"),
                        new TreeConfig("douglas c",
                            "assets/bundled/prefabs/autospawn/resource/v3_temp_forest_pine/douglas_fir_c.prefab"),

                        new TreeConfig("palm a",
                            "assets/bundled/prefabs/autospawn/resource/v3_arid_beachside/palm_tree_small_a_entity.prefab"),
                        new TreeConfig("palm b",
                            "assets/bundled/prefabs/autospawn/resource/v3_arid_beachside/palm_tree_small_b_entity.prefab"),
                        new TreeConfig("palm c",
                            "assets/bundled/prefabs/autospawn/resource/v3_arid_beachside/palm_tree_small_c_entity.prefab"),

                        new TreeConfig("swamp a",
                            "assets/bundled/prefabs/autospawn/resource/swamp-trees/swamp_tree_a.prefab"),
                        new TreeConfig("swamp b",
                            "assets/bundled/prefabs/autospawn/resource/swamp-trees/swamp_tree_b.prefab"),
                        new TreeConfig("swamp c",
                            "assets/bundled/prefabs/autospawn/resource/swamp-trees/swamp_tree_c.prefab"),

                        new TreeConfig("pine a",
                            "assets/bundled/prefabs/autospawn/resource/v3_temp_forest_pine/pine_a.prefab"),
                        new TreeConfig("pine b",
                            "assets/bundled/prefabs/autospawn/resource/v3_temp_field/pine_b.prefab"),
                        new TreeConfig("pine c",
                            "assets/bundled/prefabs/autospawn/resource/v3_temp_forest_pine/pine_c.prefab"),
                    }
                };
            }

            public TreeConfig FindAvailableConfig(string name)
            {
                if (string.IsNullOrEmpty(name)) return null;

                TreeConfig treeItem;

                return AvailableConfigs.TryGetValue(name, out treeItem) ? treeItem : null;
            }
        }

        private class TreeConfig
        {
            [JsonProperty("Name", Order = 0)] public string Name;

            [JsonProperty("Cost", Order = 1)] public int Cost = 10;

            [JsonProperty("Amount", Order = 2)] public int Amount = 1;

            [JsonProperty("Prefab", Order = 3)] public string Prefab;

            public TreeConfig(string name, string prefab)
            {
                Name = name;
                Prefab = prefab;
            }

            public void GiveItem(BasePlayer player)
            {
                Item item = ItemManager.CreateByName("clone.hemp", Amount);
                item.name = Name;
                item.text = Name;
                item.info.stackable = 1;
                player.GiveItem(item);
            }

            public static void RefundItem(BasePlayer player, Item item, GrowableGenes growableGenes)
            {
                Item refund = ItemManager.CreateByName(item.info.shortname, 1);
                refund.instanceData = new ProtoBuf.Item.InstanceData { };
                refund.instanceData.dataInt = GrowableGeneEncoding.EncodeGenesToInt(growableGenes);
                player.GiveItem(refund);
            }
        }

        #endregion

        #region Lang

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Message.Prefix", "<color=#DC143C>Tree Planter</color>: "},
                {"Message.Info", "Please select one of the following."},
                {"Message.Item", "{0} | ${1}"},
                {"Message.Authed", "You must have build privilege."},
                {"Message.Balance", "You don't have enough for that."},
                {"Message.Planter", "Must be planted in a planter."},
                {"Message.Ground", "Must be planted in the ground."},
                {"Message.Planted", "<color=#FFC55C>{0}</color> was successfully planted."},
                {"Message.Received", "You've purchased <color=#FFC55C>{0}x</color> <color=#FFC55C>{1}</color>."},
                {"Error.NotFound", "No item found by that name."}
            }, this);
        }

        #endregion

        #region Oxide

        private void OnServerInitialized() => AddCovalenceCommand("tree", nameof(TreeCommand), PERM_USE);

        private object OnMeleeAttack(BasePlayer player, HitInfo info)
        {
            if (player == null || info.HitEntity == null)
                return null;

            TreeEntity entity = info.HitEntity as TreeEntity;

            if (entity == null || entity.OwnerID == 0)
                return null;

            if (!IsEntityOwner(entity.OwnerID, player.userID))
                return true;

            return null;
        }

        private void OnEntityBuilt(Planner plan, GameObject gameObject)
        {
            BasePlayer player = plan.GetOwnerPlayer();
            if (player == null || !HasPermission(player, PERM_USE)) return;

            GrowableEntity growableEntity = gameObject.GetComponent<GrowableEntity>();

            Item item = player.GetActiveItem();
            if (item == null) return;

            NextFrame(() =>
            {
                TreeConfig treeConfig = _config.FindAvailableConfig(item.name);

                if (treeConfig != null && growableEntity != null && growableEntity.GetParentEntity() is PlanterBox)
                    TryPlantTree(player, item, growableEntity, treeConfig);
                else if (treeConfig != null && growableEntity != null)
                    TryPlantTree(player, item, growableEntity, treeConfig);
                else if (growableEntity == null || !(growableEntity.GetParentEntity() is PlanterBox))
                    TryPlantSeed(player, item, growableEntity);
            });
        }

        #endregion

        #region Core

        private void TryPlantTree(BasePlayer player, Item item, GrowableEntity entity, TreeConfig treeItem)
        {
            if (player == null || item == null) return;

            if (entity.GetParentEntity() is PlanterBox)
            {
                treeItem.GiveItem(player);
                KillSeed(entity);
                player.ChatMessage(Lang("Message.Ground", player.UserIDString));
                return;
            }

            if (!player.IsBuildingAuthed())
            {
                treeItem.GiveItem(player);
                KillSeed(entity);
                player.ChatMessage(Lang("Message.Authed", player.UserIDString));
                return;
            }

            KillSeed(entity);
            PlantTree(player, entity, treeItem.Prefab);

            player.ChatMessage(Lang("Message.Planted", player.UserIDString, item.name.TitleCase()));
        }

        private void TryPlantSeed(BasePlayer player, Item item, GrowableEntity entity)
        {
            if (player == null || item == null || IsBlockedAgri(item.info.shortname)) return;

            TreeConfig.RefundItem(player, item, entity.Genes);

            KillSeed(entity);

            player.ChatMessage(Lang("Message.Planter", player.UserIDString));
        }

        private void PlantTree(BasePlayer player, GrowableEntity plant, string prefabName)
        {
            BaseEntity entity = GameManager.server.CreateEntity(prefabName, plant.ServerPosition, Quaternion.identity);
            if (entity == null) return;
            entity.OwnerID = player.userID;
            entity.Spawn();
        }

        private void ListTrees(BasePlayer player)
        {
            StringBuilder stringBuilder = new StringBuilder(Lang("Message.Prefix", player.UserIDString));
            stringBuilder.Append(Lang("Message.Info", player.UserIDString));
            stringBuilder.AppendLine();

            _config.TreeConfigs.ForEach(x =>
            {
                stringBuilder.Append(Lang("Message.Item", player.UserIDString, x.Name, x.Cost));
                stringBuilder.AppendLine();
            });

            player.ChatMessage(stringBuilder.ToString());
        }

        private void KillSeed(GrowableEntity plant)
        {
            if (!IsValid(plant)) return;

            plant.Kill();
        }

        private bool TakeCurrency(BasePlayer player, int treeCost)
        {
            if (treeCost == 0)
                return true;

            if (_config.UseServerRewards && ServerRewards != null)
                return ServerRewards.Call<object>("TakePoints", player.userID, treeCost) != null;

            if (_config.UseEconomics && Economics != null)
                return Economics.Call<bool>("Withdraw", player.userID, (double) treeCost);

            if (_config.UseCurrency && HasAmount(player, treeCost))
            {
                TakeAmount(player, treeCost);
                return true;
            }

            return false;
        }

        #endregion

        #region Command

        private void TreeCommand(IPlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                ListTrees(player.ToBasePlayer());
                return;
            }

            TreeConfig treeConfig = _config.FindAvailableConfig(string.Join(" ", args));

            if (treeConfig == null)
            {
                player.Message(Lang("Error.NotFound", player.Id));
                return;
            }

            if (!TakeCurrency(player.ToBasePlayer(), treeConfig.Cost))
            {
                player.Message(Lang("Message.Balance", player.Id));
                return;
            }

            treeConfig.GiveItem(player.ToBasePlayer());

            player.Message(Lang("Message.Received", player.Id, treeConfig.Amount, treeConfig.Name));
        }

        #endregion

        #region Helpers

        private bool HasPermission(BasePlayer player, string permName)
            => permission.UserHasPermission(player.UserIDString, permName);

        private bool IsValid(BaseEntity entity)
            => entity.IsValid() && !entity.IsDestroyed;

        private bool SameClan(ulong userID, ulong targetID)
        {
            string playerClan = Clans?.Call<string>("GetClanOf", userID);
            if (string.IsNullOrEmpty(playerClan)) return false;

            string targetClan = Clans?.Call<string>("GetClanOf", targetID);
            if (string.IsNullOrEmpty(targetClan)) return false;

            return playerClan == targetClan;
        }

        private bool IsBlockedAgri(string shortname) => !(_config.AgriBlocked.ContainsKey(shortname) && _config.AgriBlocked[shortname]);

        private bool IsEntityOwner(ulong userID, ulong ownerID)
        {
            if (_config.EnableOwner && userID == ownerID)
                return true;

            if (_config.EnableClan && !SameClan(userID, ownerID))
                return false;

            return true;
        }

        private string Lang(string key, string id = null, params object[] args) =>
            string.Format(lang.GetMessage(key, this, id), args);

        #endregion

        #region Inventory Methods | Needed To Check For Skinned Scrap

        bool HasAmount(BasePlayer player, int amount)
            => GetAmount(player.inventory, _config.CurrencyItem, _config.CurrencySkinID) >= amount;

        void TakeAmount(BasePlayer player, int amount)
            => TakeAmount(player.inventory, _config.CurrencyItem, _config.CurrencySkinID, amount);

        int GetAmount(PlayerInventory inventory, int itemid, ulong skinID = 0UL)
        {
            if (itemid == 0)
                return 0;

            int num = 0;

            if (inventory.containerMain != null)
                num += GetAmount(inventory.containerMain, itemid, skinID, true);

            if (inventory.containerBelt != null)
                num += GetAmount(inventory.containerBelt, itemid, skinID, true);

            return num;
        }

        int GetAmount(ItemContainer container, int itemid, ulong skinID = 0UL, bool usable = false)
        {
            int num = 0;

            foreach (Item obj in container.itemList)
            {
                if (obj.info.itemid == itemid && obj.skin == skinID && (!usable || !obj.IsBusy()))
                    num += obj.amount;
            }

            return num;
        }

        int TakeAmount(PlayerInventory inventory, int itemid, ulong skinID, int amount)
        {
            int num1 = 0;

            if (inventory.containerMain != null)
            {
                int num2 = TakeAmount(inventory.containerMain, itemid, amount, skinID);
                num1 += num2;
                amount -= num2;
            }

            if (amount <= 0)
                return num1;

            if (inventory.containerBelt != null)
            {
                int num2 = TakeAmount(inventory.containerBelt, itemid, amount, skinID);
                num1 += num2;
            }

            return num1;
        }

        int TakeAmount(ItemContainer container, int itemid, int amount, ulong skinID)
        {
            int num1 = 0;

            if (amount == 0)
                return num1;

            List<Item> list = Facepunch.Pool.GetList<Item>();

            foreach (Item obj in container.itemList)
            {
                if (obj.info.itemid != itemid || obj.skin != skinID) continue;

                int num2 = amount - num1;

                if (num2 <= 0) continue;

                if (obj.amount > num2)
                {
                    obj.MarkDirty();
                    obj.amount -= num2;
                    num1 += num2;
                    Item byItemId = ItemManager.CreateByItemID(itemid);
                    byItemId.amount = num2;
                    byItemId.CollectedForCrafting(container.playerOwner);
                    break;
                }

                if (obj.amount <= num2)
                {
                    num1 += obj.amount;

                    list.Add(obj);
                }

                if (num1 == amount)
                    break;
            }

            list.ForEach(obj =>
            {
                if (obj == null) return;

                obj.RemoveFromContainer();
                obj.Remove();
            });

            ItemManager.DoRemoves();

            Facepunch.Pool.FreeList(ref list);

            return num1;
        }

        #endregion
    }

    namespace TreePlanterEx
    {
        public static class PlayerEx
        {
            public static BasePlayer ToBasePlayer(this IPlayer player) => player?.Object as BasePlayer;
        }
    }
}