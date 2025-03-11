using Rust;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core.Configuration;
using Random = System.Random;
using Oxide.Core;
using Oxide.Core.Plugins;

/*
 * This update 3.5.7
 * Added 5 missing prefabs using automation checks so far.
 * Re-wrote config generations now includes planned auto updater features.
 * This update is fully backwards compatible with your current files.
 * This update also does not affect any websites.
 * Added Lang File System
 * Updated blacklist command
 * Fixed items not being removed from held entity 
 */

namespace Oxide.Plugins
{
    [Info("BetterLoot", "Tryhard & Khan", "3.5.8")]
    [Description("A light loot container modification system")]
    public class BetterLoot : RustPlugin
    {
        #region Fields

        [PluginReference] Plugin CustomLootSpawns;
        
        private static BetterLoot _instance;
        private static PluginConfig _config;
        private bool Changed = true;
        private bool initialized;
        private double baseItemRarity = 2;
        private int populatedContainers;
        private const string Admin = "betterloot.admin";
        
        StoredExportNames storedExportNames = new StoredExportNames();
        StoredBlacklist storedBlacklist = new StoredBlacklist();
        Random rng = new Random();
        Dictionary<string, List<string>[]> Items = new Dictionary<string, List<string>[]>();
        Dictionary<string, List<string>[]> Blueprints = new Dictionary<string, List<string>[]>();
        Dictionary<string, int[]> itemWeights = new Dictionary<string, int[]>();
        Dictionary<string, int[]> blueprintWeights = new Dictionary<string, int[]>();
        Dictionary<string, int> totalItemWeight = new Dictionary<string, int>();
        Dictionary<string, int> totalBlueprintWeight = new Dictionary<string, int>();
        
        private static int RarityIndex(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.None: return 0;
                case Rarity.Common: return 1;
                case Rarity.Uncommon: return 2;
                case Rarity.Rare: return 3;
                case Rarity.VeryRare: return 4;
            }
            return -1;
        }

        #endregion

        #region DataFile

        DynamicConfigFile lootTable;
        Dictionary<string, object> lootTables = null;
        DynamicConfigFile getFile(string file) => Interface.Oxide.DataFileSystem.GetDatafile($"{Name}/{file}");
        bool chkFile(string file) => Interface.Oxide.DataFileSystem.ExistsDatafile($"{Name}/{file}");

        private class StoredExportNames
        {
            public int version;
            public Dictionary<string, string> AllItemsAvailable = new Dictionary<string, string>();
        }
        
        private int ItemWeight(double baseRarity, int index) { return (int)(Math.Pow(baseRarity, 4 - index) * 1000); }
        
        private object GetAmounts(ItemAmount amount, int mul = 1)
        {
            if (amount.itemDef.isWearable || (amount.itemDef.condition.enabled && amount.itemDef.GetComponent<ItemModDeployable>() == null))
                mul = 1;
            object options = new Dictionary<string, object>
            {
                ["Min"] = (int)amount.amount * mul,
                ["Max"] = ((ItemAmountRanged)amount).maxAmount > 0f &&
                          ((ItemAmountRanged)amount).maxAmount > amount.amount
                    ? (int)((ItemAmountRanged)amount).maxAmount * mul
                    : (int)amount.amount * mul,
            };
            return options;
        }
        
        private void GetLootSpawn(LootSpawn lootSpawn, ref Dictionary<string, object> items)
        {
            if (lootSpawn.subSpawn != null && lootSpawn.subSpawn.Length > 0)
            {
                foreach (var entry in lootSpawn.subSpawn)
                    GetLootSpawn(entry.category, ref items);
                return;
            }
            if (lootSpawn.items != null && lootSpawn.items.Length > 0)
            {
                foreach (var amount in lootSpawn.items)
                {
                    object options = GetAmounts(amount, 1);
                    string itemName = amount.itemDef.shortname;
                    if (amount.itemDef.spawnAsBlueprint)
                        itemName += ".blueprint";
                    if (!items.ContainsKey(itemName))
                        items.Add(itemName, options);
                }
            }
        }
        
        private void LoadAllContainers()
        {
            try { lootTable = getFile("LootTables"); }
            catch (JsonReaderException e)
            {
                PrintWarning($"JSON error in 'LootTables' > Line: {e.LineNumber} | {e.Path}");
                Interface.GetMod().UnloadPlugin(Name);
                return;
            }
            lootTables = new Dictionary<string, object>();
            lootTables = lootTable["LootTables"] as Dictionary<string, object>;
            if (lootTables == null)
                lootTables = new Dictionary<string, object>();
            bool wasAdded = false;
            foreach (var lootPrefab in _config.Generic.WatchedPrefabs)
            {
                if (!lootTables.ContainsKey((string)lootPrefab))
                {
                    var loot = GameManager.server.FindPrefab((string)lootPrefab)?.GetComponent<LootContainer>();
                    if (loot == null)
                        continue;
                    var container = new Dictionary<string, object>();
                    container.Add("Enabled", !((string)lootPrefab).Contains("bradley_crate") && !((string)lootPrefab).Contains("heli_crate"));
                    container.Add("Scrap", loot.scrapAmount);
                    int slots = 0;
                    if (loot.LootSpawnSlots.Length > 0)
                    {
                        LootContainer.LootSpawnSlot[] lootSpawnSlots = loot.LootSpawnSlots;
                        for (int i = 0; i < lootSpawnSlots.Length; i++)
                            slots += lootSpawnSlots[i].numberToSpawn;
                    }
                    else
                        slots = loot.maxDefinitionsToSpawn;
                    container.Add("ItemsMin", slots);
                    container.Add("ItemsMax", slots);
                    container.Add("MaxBPs", 1);
                    var itemList = new Dictionary<string, object>();
                    if (loot.lootDefinition != null)
                        GetLootSpawn(loot.lootDefinition, ref itemList);
                    else if (loot.LootSpawnSlots.Length > 0)
                    {
                        LootContainer.LootSpawnSlot[] lootSpawnSlots = loot.LootSpawnSlots;
                        foreach (var lootSpawnSlot in lootSpawnSlots)
                        {
                            GetLootSpawn(lootSpawnSlot.definition, ref itemList);
                        }
                    }
                    container.Add("ItemList", itemList);
                    lootTables.Add((string)lootPrefab, container);
                    wasAdded = true;
                }

            }
            if (wasAdded)
            {
                lootTable.Set("LootTables", lootTables);
                lootTable.Save();
            }
            wasAdded = false;
            bool wasRemoved = false;
            int activeTypes = 0;
            foreach (var lootTable in lootTables.ToList())
            {
                var loot = GameManager.server.FindPrefab(lootTable.Key)?.GetComponent<LootContainer>();
                if (loot == null)
                {
                    lootTables.Remove(lootTable.Key);
                    wasRemoved = true;
                    continue;
                }
                var container = lootTable.Value as Dictionary<string, object>;
                if (!container.ContainsKey("Enabled"))
                {
                    container.Add("Enabled", true);
                    wasAdded = true;
                }
                if ((bool)container["Enabled"])
                    activeTypes++;
                if (!container.ContainsKey("Scrap"))
                {
                    container.Add("Scrap", loot.scrapAmount);
                    wasAdded = true;
                }

                int slots = 0;
                if (loot.LootSpawnSlots.Length > 0)
                {
                    LootContainer.LootSpawnSlot[] lootSpawnSlots = loot.LootSpawnSlots;
                    for (int i = 0; i < lootSpawnSlots.Length; i++)
                        slots += lootSpawnSlots[i].numberToSpawn;
                }
                else
                    slots = loot.maxDefinitionsToSpawn;
                if (!container.ContainsKey("MaxBPs"))
                {
                    container.Add("MaxBPs", 1);
                    wasAdded = true;
                }
                if (!container.ContainsKey("ItemsMin"))
                {
                    container.Add("ItemsMin", slots);
                    wasAdded = true;
                }
                if (!container.ContainsKey("ItemsMax"))
                {
                    container.Add("ItemsMax", slots);
                    wasAdded = true;
                }
                if (!container.ContainsKey("ItemsMax"))
                {
                    container.Add("ItemsMax", slots);
                    wasAdded = true;
                }
                if (!container.ContainsKey("ItemList"))
                {
                    var itemList = new Dictionary<string, object>();
                    if (loot.lootDefinition != null)
                        GetLootSpawn(loot.lootDefinition, ref itemList);
                    else if (loot.LootSpawnSlots.Length > 0)
                    {
                        LootContainer.LootSpawnSlot[] lootSpawnSlots = loot.LootSpawnSlots;
                        for (int i = 0; i < lootSpawnSlots.Length; i++)
                        {
                            LootContainer.LootSpawnSlot lootSpawnSlot = lootSpawnSlots[i];
                            GetLootSpawn(lootSpawnSlot.definition, ref itemList);
                        }
                    }
                    container.Add("ItemList", itemList);
                    wasAdded = true;
                }
                Items.Add(lootTable.Key, new List<string>[5]);
                Blueprints.Add(lootTable.Key, new List<string>[5]);
                for (var i = 0; i < 5; ++i)
                {
                    Items[lootTable.Key][i] = new List<string>();
                    Blueprints[lootTable.Key][i] = new List<string>();
                }
                foreach (var itemEntry in container["ItemList"] as Dictionary<string, object>)
                {
                    bool isBP = itemEntry.Key.EndsWith(".blueprint") ? true : false;
                    var def = ItemManager.FindItemDefinition(itemEntry.Key.Replace(".blueprint", ""));

                    if (def != null)
                    {
                        if (isBP && def.Blueprint != null && def.Blueprint.isResearchable)
                        {
                            int index = (int)def.rarity;
                            if (!Blueprints[lootTable.Key][index].Contains(def.shortname))
                                Blueprints[lootTable.Key][index].Add(def.shortname);
                        }
                        else
                        {
                            int index = 0;
                            object indexoverride;
                            if (_config.Rare.Override.TryGetValue(def.shortname, out indexoverride))
                                index = Convert.ToInt32(indexoverride);
                            else
                                index = (int)def.rarity;
                            if (!Items[lootTable.Key][index].Contains(def.shortname))
                                Items[lootTable.Key][index].Add(def.shortname);
                        }
                    }
                }
                totalItemWeight.Add(lootTable.Key, 0);
                totalBlueprintWeight.Add(lootTable.Key, 0);
                itemWeights.Add(lootTable.Key, new int[5]);
                blueprintWeights.Add(lootTable.Key, new int[5]);
                for (var i = 0; i < 5; ++i)
                {
                    totalItemWeight[lootTable.Key] += (itemWeights[lootTable.Key][i] = ItemWeight(baseItemRarity, i) * Items[lootTable.Key][i].Count);
                    totalBlueprintWeight[lootTable.Key] += (blueprintWeights[lootTable.Key][i] = ItemWeight(baseItemRarity, i) * Blueprints[lootTable.Key][i].Count);
                }

            }
            if (wasAdded || wasRemoved)
            {
                lootTable.Set("LootTables", lootTables);
                lootTable.Save();
            }
            lootTable.Clear();
            Puts($"Using '{activeTypes}' active of '{lootTables.Count}' supported containertypes");
        }

        private void SaveExportNames()
        {
            storedExportNames = Interface.GetMod().DataFileSystem.ReadObject<StoredExportNames>("BetterLoot\\NamesList");
            if (storedExportNames.AllItemsAvailable.Count == 0 || (int)storedExportNames.version != Rust.Protocol.network)
            {
                storedExportNames = new StoredExportNames();
                var exportItems = new List<ItemDefinition>(ItemManager.itemList);
                storedExportNames.version = Rust.Protocol.network;
                foreach (var it in exportItems)
                    storedExportNames.AllItemsAvailable.Add(it.shortname, it.displayName.english);
                Interface.GetMod().DataFileSystem.WriteObject("BetterLoot\\NamesList", storedExportNames);
                Puts($"Exported {storedExportNames.AllItemsAvailable.Count} items to 'NamesList'");
            }
        }
        
        private class StoredBlacklist
        {
            public List<string> ItemList = new List<string>();
        }

        private void LoadBlacklist()
        {
            storedBlacklist = Interface.GetMod().DataFileSystem.ReadObject<StoredBlacklist>("BetterLoot\\Blacklist");
            if (storedBlacklist.ItemList.Count != 0) return;
            Puts("No Blacklist found, creating new file...");
            storedBlacklist = new StoredBlacklist();
            storedBlacklist.ItemList.Add("flare");
            Interface.GetMod().DataFileSystem.WriteObject("BetterLoot\\Blacklist", storedBlacklist);
        }

        private void SaveBlacklist() => Interface.GetMod().DataFileSystem.WriteObject("BetterLoot\\Blacklist", storedBlacklist);
        
        #endregion

        #region Config

        private class PluginConfig : SerializableConfiguration
        {
            public Generic Generic = new Generic();
            public Loot Loot = new Loot();
            [JsonProperty("Rarity")]
            public Rare Rare = new Rare();

            public string ToJson() => JsonConvert.SerializeObject(this);
            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }
        
        private class Generic
        {
            public double blueprintProbability = 0.11;
            public bool listUpdatesOnLoaded = true;
            public bool removeStackedContainers = true;
            public List<object> WatchedPrefabs = new List<object>();
        }

        private class Loot
        {
            public bool enableHammerLootCycle = false;
            public double hammerLootCycleTime = 3.0;
            public int lootMultiplier = 1;
            public int scrapMultiplier = 1;
        }

        private class Rare
        {
            public Dictionary<string, object> Override = new Dictionary<string, object>
            {
                {"autoturret", 4},
                {"lmg.m249", 4},
                {"targeting.computer", 3},
            };
        }
        
        private void CheckConfig()
        {
            foreach (GameManifest.PrefabProperties category in GameManifest.Current.prefabProperties)
            {
                if (!(category.name.Contains("resource/loot") || 
                      category.name.Contains("misc/supply drop/supply_drop") || 
                      category.name.Contains("/npc/m2bradley/bradley_crate") || 
                      category.name.Contains("/npc/patrol helicopter/heli_crate") || 
                      category.name.Contains("/deployable/chinooklockedcrate/chinooklocked") || 
                      category.name.Contains("/deployable/chinooklockedcrate/codelocked") || 
                      category.name.Contains("prefabs/radtown") || 
                      category.name.Contains("props/roadsigns")) ||
                    category.name.Contains("radtown/ore") || 
                    category.name.Contains("static") || 
                    category.name.Contains("/spawners") || 
                    category.name.Contains("radtown/desk") || 
                    category.name.Contains("radtown/loot_component_test")) continue;
                if (!_config.Generic.WatchedPrefabs.Contains(category.name))
                {
                    _config.Generic.WatchedPrefabs.Add(category.name);
                }
            }
            SaveConfig();
        }
        
        protected override void LoadDefaultConfig() => _config = new PluginConfig();
        
        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _config = Config.ReadObject<PluginConfig>();

                if (_config == null)
                {
                    PrintWarning($"Generating Config File for Better Loot");
                    throw new JsonException();
                }

                if (MaybeUpdateConfig(_config))
                {
                    PrintWarning("Configuration appears to be outdated; updating and saving Better Loot");
                    SaveConfig();
                }
            }
            catch (Exception ex)
            {
                PrintWarning("Failed to load Better Loot config file (is the config file corrupt?) (" + ex.Message + ")");
            }
        }
        protected override void SaveConfig()
        {
            //PrintToConsole($"Configuration changes saved to {Name}.json");
            PrintWarning($"Configuration changes saved to {Name}.json");
            Config.WriteObject(_config, true);
        }

        #endregion

        #region Updater

        internal class SerializableConfiguration
        {
            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonHelper.Deserialize(ToJson()) as Dictionary<string, object>;
        }

        private static class JsonHelper
        {
            public static object Deserialize(string json) => ToObject(JToken.Parse(json));

            private static object ToObject(JToken token)
            {
                switch (token.Type)
                {
                    case JTokenType.Object:
                        return token.Children<JProperty>().ToDictionary(prop => prop.Name, prop => ToObject(prop.Value));
                    case JTokenType.Array:
                        return token.Select(ToObject).ToList();

                    default:
                        return ((JValue) token).Value;
                }
            }
        }

        private bool MaybeUpdateConfig(SerializableConfiguration config)
        {
            var currentWithDefaults = config.ToDictionary();
            var currentRaw = Config.ToDictionary(x => x.Key, x => x.Value);
            return MaybeUpdateConfigDict(currentWithDefaults, currentRaw);
        }

        private bool MaybeUpdateConfigDict(Dictionary<string, object> currentWithDefaults, Dictionary<string, object> currentRaw)
        {
            bool changed = false;

            foreach (var key in currentWithDefaults.Keys)
            {
                object currentRawValue;
                if (currentRaw.TryGetValue(key, out currentRawValue))
                {
                    var defaultDictValue = currentWithDefaults[key] as Dictionary<string, object>;
                    var currentDictValue = currentRawValue as Dictionary<string, object>;

                    if (defaultDictValue != null)
                    {
                        if (currentDictValue == null)
                        {
                            currentRaw[key] = currentWithDefaults[key];
                            changed = true;
                        }
                        else if (MaybeUpdateConfigDict(defaultDictValue, currentDictValue))
                            changed = true;
                    }
                }
                else
                {
                    currentRaw[key] = currentWithDefaults[key];
                    changed = true;
                }
            }

            return changed;
        }

        #endregion

        #region Oxide

        private void Init()
        {
            CheckConfig();
            LoadBlacklist();
            _instance = this;
        }

        private void OnServerInitialized()
        {
            ItemManager.Initialize();
            permission.RegisterPermission(Admin, this);
            LoadAllContainers();
            UpdateInternals(_config.Generic.listUpdatesOnLoaded);
        }

        private void Unload()
        {
            var gameObjects = UnityEngine.Object.FindObjectsOfType<HammerHitLootCycle>().ToList();
            if (gameObjects.Count > 0) 
            {
                foreach (var objects in gameObjects) 
                {
                    UnityEngine.Object.Destroy(objects);
                }
            }
            _instance = null;
        }
        
        private object OnLootSpawn(LootContainer container)
        {
            if (!initialized || container == null)
                return null;
            if (CustomLootSpawns != null && (CustomLootSpawns && (bool)CustomLootSpawns?.Call("IsLootBox", container.GetComponent<BaseEntity>())))
                return null;
            if (PopulateContainer(container))
            {
                ItemManager.DoRemoves();
                return true;
            }
            return null;
        }
        
        //void OnLootEntity(BasePlayer player, BaseEntity target)
        //{
        //Puts($"{player.displayName} looted {target.PrefabName}");
        //}

        #endregion

        #region Core
        
        private bool PopulateContainer(LootContainer container)
        {
            Dictionary<string, object> con;
            object containerobj;
            if (!lootTables.TryGetValue(container.PrefabName, out containerobj))
                return false;
            con = containerobj as Dictionary<string, object>;
            if (!(bool)con["Enabled"])
                return false;
            var lootitemcount = (con["ItemList"] as Dictionary<string, object>)?.Count();
            int itemCount = Mathf.RoundToInt(UnityEngine.Random.Range(Convert.ToSingle(Mathf.Min((int)con["ItemsMin"], (int)con["ItemsMax"])) * 100f, Convert.ToSingle(Mathf.Max((int)con["ItemsMin"], (int)con["ItemsMax"])) * 100f) / 100f);
            if (lootitemcount > 0 && itemCount > lootitemcount && lootitemcount < 36)
                itemCount = (int)lootitemcount;
            if (container.inventory == null)
            {
                container.inventory = new ItemContainer();
                container.inventory.ServerInitialize(null, 36);
                container.inventory.GiveUID();
            }
            else
            {
                container.inventory.Clear();
                container.inventory.capacity = 36;
                ItemManager.DoRemoves();
            }
            var items = new List<Item>();
            var itemNames = new List<string>();
            var itemBlueprints = new List<int>();
            var maxRetry = 10;
            for (int i = 0; i < itemCount; ++i)
            {
                if (maxRetry == 0)
                {
                    break;
                }
                var item = MightyRNG(container.PrefabName, itemCount, (bool)(itemBlueprints.Count >= (int)con["MaxBPs"]));

                if (item == null)
                {
                    --maxRetry;
                    --i;
                    continue;
                }
                if (itemNames.Contains(item.info.shortname) || (item.IsBlueprint() && itemBlueprints.Contains(item.blueprintTarget)))
                {
                    item.Remove();
                    --maxRetry;
                    --i;
                    continue;
                }
                else
                    if (item.IsBlueprint())
                    itemBlueprints.Add(item.blueprintTarget);
                else
                    itemNames.Add(item.info.shortname);
                items.Add(item);
                if (storedBlacklist.ItemList.Contains(item.info.shortname)) 
                {
                    items.Remove(item);
					item.Remove(); // broken item fix
                }
            }
            foreach (var item in items.Where(x => x != null && x.IsValid()))
                if (!item.MoveToContainer(container.inventory, -1, false)) { item.DoRemove(); } // broken item fix / fixes full container 
            if ((int)con["Scrap"] > 0)
            {
                int scrapCount = (int)con["Scrap"];
                Item item = ItemManager.Create(ItemManager.FindItemDefinition("scrap"), scrapCount * _config.Loot.scrapMultiplier, 0uL); 
                if (!item.MoveToContainer(container.inventory, -1, false)) { item.DoRemove(); } // broken item fix
            }
            container.inventory.capacity = container.inventory.itemList.Count;
            container.inventory.MarkDirty();
            container.SendNetworkUpdate();
            populatedContainers++;
            return true;
        }

        private void UpdateInternals(bool doLog)
        {
            SaveExportNames();
            if (Changed)
            {
                SaveConfig();
                Changed = false;
            }
            Puts("Updating internals ...");
            populatedContainers = 0;
            NextTick(() =>
            {
                if (_config.Generic.removeStackedContainers)
                    FixLoot();
                foreach (var container in BaseNetworkable.serverEntities.Where(p => p != null && p.GetComponent<BaseEntity>() != null && p is LootContainer).Cast<LootContainer>().ToList())
                {
                    if (container == null)
                        continue;
                    if (CustomLootSpawns != null && (CustomLootSpawns && (bool)CustomLootSpawns?.Call("IsLootBox", container.GetComponent<BaseEntity>())))
                        continue;
                    if (PopulateContainer(container))
                        populatedContainers++;
                }
                
                Puts($"Populated '{populatedContainers}' supported containers.");
                initialized = true;
                populatedContainers = 0;
                ItemManager.DoRemoves();
            });
        }
        
        private void FixLoot()
        {
            var spawns = Resources.FindObjectsOfTypeAll<LootContainer>()
                .Where(c => c.isActiveAndEnabled).
                OrderBy(c => c.transform.position.x).ThenBy(c => c.transform.position.z).ThenBy(c => c.transform.position.z)
                .ToList();

            var count = spawns.Count();
            var racelimit = count * count;

            var antirace = 0;
            var deleted = 0;

            for (var i = 0; i < count; i++)
            {
                var box = spawns[i];
                var pos = new Vector2(box.transform.position.x, box.transform.position.z);

                if (++antirace > racelimit)
                {
                    return;
                }

                var next = i + 1;
                while (next < count)
                {
                    var box2 = spawns[next];
                    var pos2 = new Vector2(box2.transform.position.x, box2.transform.position.z);
                    var distance = Vector2.Distance(pos, pos2);

                    if (++antirace > racelimit)
                    {
                        return;
                    }

                    if (distance < 0.25f)
                    {
                        spawns.RemoveAt(next);
                        count--;
                        (box2 as BaseEntity).KillMessage();
                        deleted++;
                    }
                    else break;
                }
            }

            if (deleted > 0)
                Puts($"Removed {deleted} stacked LootContainer");
            else
                Puts($"No stacked LootContainer found.");
            ItemManager.DoRemoves();
        }
        
        private Item MightyRNG(string type, int itemCount, bool blockBPs = false)
        {
            bool asBP = rng.NextDouble() < _config.Generic.blueprintProbability && !blockBPs;
            List<string> selectFrom;
            int limit = 0;
            string itemName;
            Item item;
            int maxRetry = 10 * itemCount;
            do
            {
                selectFrom = null;
                item = null;
                if (asBP)
                {
                    var r = rng.Next(totalBlueprintWeight[type]);
                    for (var i = 0; i < 5; ++i)
                    {
                        limit += blueprintWeights[type][i];
                        if (r < limit)
                        {
                            selectFrom = Blueprints[type][i];
                            break;
                        }
                    }
                }
                else
                {
                    var r = rng.Next(totalItemWeight[type]);
                    for (var i = 0; i < 5; ++i)
                    {
                        limit += itemWeights[type][i];
                        if (r < limit)
                        {
                            selectFrom = Items[type][i];
                            break;
                        }
                    }
                }
                if (selectFrom == null)
                {
                    if (--maxRetry <= 0)
                        break;
                    continue;
                }
                itemName = selectFrom[rng.Next(0, selectFrom.Count)];
                ItemDefinition itemDef = ItemManager.FindItemDefinition(itemName);
                if (asBP && itemDef.Blueprint != null && itemDef.Blueprint.isResearchable)
                {
                    var blueprintBaseDef = ItemManager.FindItemDefinition("blueprintbase");
                    item = ItemManager.Create(blueprintBaseDef, 1, 0uL);
                    item.blueprintTarget = itemDef.itemid;
                }
                else
                    item = ItemManager.CreateByName(itemName, 1);
                if (item == null || item.info == null)
                    continue;
                break;
            } while (true);
            if (item == null)
                return null;
            object itemOptions;
            if (((lootTables[type] as Dictionary<string, object>)["ItemList"] as Dictionary<string, object>).TryGetValue(item.info.shortname, out itemOptions))
            {
                Dictionary<string, object> options = itemOptions as Dictionary<string, object>;
                item.amount = UnityEngine.Random.Range(Math.Min((int)options["Min"], (int)options["Max"]), Math.Max((int)options["Min"], (int)options["Max"])) * _config.Loot.lootMultiplier;
                //if (options.ContainsKey("SkinId"))
                    //item.skin = (uint)options["SkinId"];

            }
            item.OnVirginSpawn();
            return item;
        }
        
        private bool ItemExists(string name)
        {
            // remove useless loop
            ItemDefinition itemDef = ItemManager.itemList.Find((ItemDefinition x) => x.shortname == name);
            if (itemDef != null) return true;            
            return false;
        }
        
        private bool isSupplyDropActive()
        {
            Dictionary<string, object> con;
            object containerobj;
            if (!lootTables.TryGetValue("assets/prefabs/misc/supply drop/supply_drop.prefab", out containerobj))
                return false;
            con = containerobj as Dictionary<string, object>;
            if ((bool)con["Enabled"])
                return true;
            return false;
        }

        #endregion

        [ChatCommand("blacklist")]
        private void CmdChatBlacklistNew(BasePlayer player, string command, string[] args)
        {
            if (!initialized)
            {
                SendReply(player, BLLang( "initialized")); return;
            }
            if (!permission.UserHasPermission(player.UserIDString, Admin))
            {
                SendReply(player, BLLang("perm")); return;
            }
            if (args.Length == 0)
            {
                if (storedBlacklist.ItemList.Count == 0)
                {
                    SendReply(player, BLLang("none"));
                }
                else
                {
                    var sb = new StringBuilder();
                    foreach (var item in storedBlacklist.ItemList)
                    {
                        if (sb.Length > 0)
                            sb.Append(", ");
                        sb.Append(item);
                    }
                    SendReply(player, BLLang("blocked", player.UserIDString, sb.ToString()));
                }
                return;
            }

            switch (args[0].ToLower())
            {
                case "additem":
                    if (!ItemExists(args[1]))
                    {
                        SendReply(player, BLLang("notvalid", player.UserIDString, args[1]));
                        return;
                    }
                    if (!storedBlacklist.ItemList.Contains(args[1]))
                    {
                        storedBlacklist.ItemList.Add(args[1]);
                        UpdateInternals(false);
                        SendReply(player, BLLang("blockedpass", player.UserIDString, args[1]));
                        SaveBlacklist();
                        return;
                    }
                    SendReply(player, BLLang("blockedtrue", player.UserIDString, args[1]));
                    break;
                case "deleteitem":
                    if (!ItemExists(args[1]))
                    {
                        SendReply(player, BLLang("notvalid", player.UserIDString, args[1]));
                        return;
                    }
                    if (storedBlacklist.ItemList.Contains(args[1]))
                    {
                        storedBlacklist.ItemList.Remove(args[1]);
                        UpdateInternals(false);
                        SendReply(player, BLLang("unblacklisted", player.UserIDString, args[1]));
                        SaveBlacklist();
                        return;
                    }
                    SendReply(player, BLLang("blockedfalse", player.UserIDString, args[1]));
                    break;
                default:
                    SendReply(player, BLLang("syntax"));
                    break;
            }
        }

        #region Hammer loot cycle

        object OnMeleeAttack(BasePlayer player, HitInfo c)
        {
            //Puts($"OnMeleeAttack works! You hit {c.HitEntity.PrefabName}"); DEBUG FOR TESTING
            var item = player.GetActiveItem();
            if (item.hasCondition) return null;
            //Puts($"{item.ToString()}");
            if (!player.IsAdmin || c.HitEntity.GetComponent<LootContainer>() == null || !item.ToString().Contains("hammer") || !_config.Loot.enableHammerLootCycle)  return null;
            var inv = c.HitEntity.GetComponent<StorageContainer>();
            inv.gameObject.AddComponent<HammerHitLootCycle>();
            player.inventory.loot.StartLootingEntity(inv, false);
            player.inventory.loot.AddContainer(inv.inventory);
            player.inventory.loot.SendImmediate();
            player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", inv.panelName);

            //Timer s = timer.Every(1f, () => { PopulateContainer(inv})
            return null;
            }

        private class HammerHitLootCycle : FacepunchBehaviour
        {
            private void Awake()
            {
                if (!_instance.initialized) return;
                InvokeRepeating(Repeater, (float)_config.Loot.hammerLootCycleTime, (float)_config.Loot.hammerLootCycleTime);
            }
            private void Repeater()
            {
                if (!enabled) return;
                LootContainer loot = GetComponent<LootContainer>();
                _instance.Puts($"{loot}");
                _instance.PopulateContainer(loot);
            }
            private void PlayerStoppedLooting(BasePlayer player)
            {
                //_instance.Puts($"Ended looting of the box"); Doesn't call but it works for a reason I don't quite understand
                CancelInvoke(Repeater);
                Destroy(this);
            }
        }

        #endregion

        #region Lang

        private string BLLang(string key, string id = null) => lang.GetMessage(key, this, id);
        private string BLLang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "initialized", "Plugin not enabled"},
                { "perm", "You are not authorized to use this command"},
                { "syntax", "Usage: /blacklist [additem|deleteitem] \"ITEMNAME\""},
                { "none", "There are no blacklisted items"},
                { "blocked", "Blacklisted items: {0}"},
                { "notvalid", "Not a valid item: {0}"},
                {"blockedpass", "The item '{0}' is now blacklisted"},
                {"blockedtrue", "The item '{0}' is already blacklisted}"},
                {"unblacklisted", "The item '{0}' has been unblacklisted"},
                {"blockedfalse", "The item '{0}' is not blacklisted"},
            }, this); //en
        }

        #endregion
    }
}