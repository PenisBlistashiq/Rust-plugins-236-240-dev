using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Oxide.Core;
using Newtonsoft.Json.Linq;
using Oxide.Core.Plugins;
using Facepunch;
using Oxide.Plugins.WaterPatrolExtensionMethods;

namespace Oxide.Plugins
{
    [Info("WaterPatrol", "KpucTaJl", "1.0.2")]
    internal class WaterPatrol : RustPlugin
    {
        #region Config
        private const bool En = true;

        private PluginConfig _config;

        protected override void LoadDefaultConfig()
        {
            Puts("Creating a default config...");
            _config = PluginConfig.DefaultConfig();
            _config.PluginVersion = Version;
            SaveConfig();
            Puts("Creation of the default config completed!");
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<PluginConfig>();
            if (_config.PluginVersion < Version) UpdateConfigValues();
        }

        private void UpdateConfigValues()
        {
            Puts("Config update detected! Updating config values...");
            if (_config.PluginVersion < new VersionNumber(1, 0, 2))
            {
                foreach (BoatConfig config in _config.Boats)
                {
                    config.Marker = new MarkerConfig
                    {
                        Enabled = false,
                        Name = "Water Patrol",
                        Radius = 0.4f,
                        Alpha = 0.6f,
                        Color = new ColorConfig { R = 0.81f, G = 0.25f, B = 0.15f }
                    };
                    config.EnginePower = 2000f;
                }
                _config.TimeKillGround = 3f;
                _config.PreventDamageRange = true;
                _config.WaterDepthSpawn = 15f;
            }
            _config.PluginVersion = Version;
            Puts("Config update completed!");
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        public class ItemConfig
        {
            [JsonProperty("ShortName")] public string ShortName { get; set; }
            [JsonProperty(En ? "Minimum" : "Минимальное кол-во")] public int MinAmount { get; set; }
            [JsonProperty(En ? "Maximum" : "Максимальное кол-во")] public int MaxAmount { get; set; }
            [JsonProperty(En ? "Chance [0.0-100.0]" : "Шанс выпадения предмета [0.0-100.0]")] public float Chance { get; set; }
            [JsonProperty(En ? "Is this a blueprint? [true/false]" : "Это чертеж? [true/false]")] public bool IsBluePrint { get; set; }
            [JsonProperty("SkinID (0 - default)")] public ulong SkinID { get; set; }
            [JsonProperty(En ? "Name (empty - default)" : "Название (empty - default)")] public string Name { get; set; }
        }

        public class LootTableConfig
        {
            [JsonProperty(En ? "Minimum numbers of items" : "Минимальное кол-во элементов")] public int Min { get; set; }
            [JsonProperty(En ? "Maximum numbers of items" : "Максимальное кол-во элементов")] public int Max { get; set; }
            [JsonProperty(En ? "Use minimum and maximum values? [true/false]" : "Использовать минимальное и максимальное значение? [true/false]")] public bool UseCount { get; set; }
            [JsonProperty(En ? "List of items" : "Список предметов")] public List<ItemConfig> Items { get; set; }
        }

        public class PrefabConfig
        {
            [JsonProperty(En ? "Chance [0.0-100.0]" : "Шанс выпадения [0.0-100.0]")] public float Chance { get; set; }
            [JsonProperty(En ? "The path to the prefab" : "Путь к prefab-у")] public string PrefabDefinition { get; set; }
        }

        public class PrefabLootTableConfig
        {
            [JsonProperty(En ? "Minimum numbers of prefabs" : "Минимальное кол-во prefab-ов")] public int Min { get; set; }
            [JsonProperty(En ? "Maximum numbers of prefabs" : "Максимальное кол-во prefab-ов")] public int Max { get; set; }
            [JsonProperty(En ? "Use minimum and maximum values? [true/false]" : "Использовать минимальное и максимальное значение? [true/false]")] public bool UseCount { get; set; }
            [JsonProperty(En ? "List of prefabs" : "Список prefab-ов")] public List<PrefabConfig> Prefabs { get; set; }
        }

        public class CrateConfig
        {
            [JsonProperty(En ? "Which loot table should the plugin use? (0 - own; 1 - loot table of the Rust objects; 2 - combine the 1 and 2 methods)" : "Какую таблицу лута необходимо использовать? (0 - собственную; 1 - таблица предметов объектов Rust; 2 - совместить 1 и 2 методы)")] public int TypeLootTable { get; set; }
            [JsonProperty(En ? "Loot table from prefabs (if the loot table type is 1 or 2)" : "Таблица предметов из prefab-ов (если тип таблицы предметов - 1 или 2)")] public PrefabLootTableConfig PrefabLootTable { get; set; }
            [JsonProperty(En ? "Own loot table (if the loot table type is 0 or 2)" : "Собственная таблица предметов (если тип таблицы предметов - 0 или 2)")] public LootTableConfig OwnLootTable { get; set; }
        }

        public class NpcBelt
        {
            [JsonProperty("ShortName")] public string ShortName { get; set; }
            [JsonProperty(En ? "Amount" : "Кол-во")] public int Amount { get; set; }
            [JsonProperty("SkinID (0 - default)")] public ulong SkinID { get; set; }
            [JsonProperty(En ? "Mods" : "Модификации на оружие")] public HashSet<string> Mods { get; set; }
            [JsonProperty(En ? "Ammo" : "Боеприпасы")] public string Ammo { get; set; }
        }

        public class NpcWear
        {
            [JsonProperty("ShortName")] public string ShortName { get; set; }
            [JsonProperty("SkinID (0 - default)")] public ulong SkinID { get; set; }
        }

        public class NpcConfig
        {
            [JsonProperty(En ? "Name" : "Название")] public string Name { get; set; }
            [JsonProperty(En ? "Health" : "Кол-во ХП")] public float Health { get; set; }
            [JsonProperty(En ? "Attack Range Multiplier" : "Множитель радиуса атаки")] public float AttackRangeMultiplier { get; set; }
            [JsonProperty(En ? "Target Memory Duration [sec.]" : "Длительность памяти цели [sec.]")] public float MemoryDuration { get; set; }
            [JsonProperty(En ? "Scale damage" : "Множитель урона")] public float DamageScale { get; set; }
            [JsonProperty(En ? "Aim Cone Scale" : "Множитель разброса")] public float AimConeScale { get; set; }
            [JsonProperty(En ? "Wear items" : "Одежда")] public HashSet<NpcWear> WearItems { get; set; }
            [JsonProperty(En ? "Belt items" : "Быстрые слоты")] public HashSet<NpcBelt> BeltItems { get; set; }
            [JsonProperty(En ? "Kit (it is recommended to use the previous 2 settings to improve performance)" : "Kit (рекомендуется использовать предыдущие 2 пункта настройки для повышения производительности)")] public string Kit { get; set; }
        }

        public class DriverConfig
        {
            [JsonProperty(En ? "Name" : "Название")] public string Name { get; set; }
            [JsonProperty(En ? "Health" : "Кол-во ХП")] public float Health { get; set; }
            [JsonProperty(En ? "Wear items" : "Одежда")] public HashSet<NpcWear> WearItems { get; set; }
        }

        public class BoatConfig
        {
            [JsonProperty(En ? "Chase Range" : "Дальность погони за целью")] public float ChaseRange { get; set; }
            [JsonProperty(En ? "Sense Range" : "Радиус обнаружения цели")] public float SenseRange { get; set; }
            [JsonProperty(En ? "Minimum" : "Минимальное кол-во")] public int MinAmount { get; set; }
            [JsonProperty(En ? "Maximum" : "Максимальное кол-во")] public int MaxAmount { get; set; }
            [JsonProperty(En ? "Marker configuration on the map" : "Настройка маркера на карте")] public MarkerConfig Marker { get; set; }
            [JsonProperty(En ? "Engine power" : "Мощность двигателя")] public float EnginePower { get; set; }
            [JsonProperty(En ? "Crate setting" : "Настройка ящика")] public CrateConfig Crate { get; set; }
            [JsonProperty(En ? "Driver setting" : "Настройки водителя")] public DriverConfig Driver { get; set; }
            [JsonProperty(En ? "NPCs setting - 1" : "Настройки NPC - 1")] public NpcConfig Npc1 { get; set; }
            [JsonProperty(En ? "NPCs setting - 2" : "Настройки NPC - 2")] public NpcConfig Npc2 { get; set; }
            [JsonProperty(En ? "NPCs setting - 3" : "Настройки NPC - 3")] public NpcConfig Npc3 { get; set; }
            [JsonProperty(En ? "NPCs setting - 4" : "Настройки NPC - 4")] public NpcConfig Npc4 { get; set; }
            [JsonProperty(En ? "NPCs setting - 5" : "Настройки NPC - 5")] public NpcConfig Npc5 { get; set; }
        }

        public class GuiAnnouncementsConfig
        {
            [JsonProperty(En ? "Do you use the GUI Announcements? [true/false]" : "Использовать ли GUI Announcements? [true/false]")] public bool IsGuiAnnouncements { get; set; }
            [JsonProperty(En ? "Banner color" : "Цвет баннера")] public string BannerColor { get; set; }
            [JsonProperty(En ? "Text color" : "Цвет текста")] public string TextColor { get; set; }
            [JsonProperty(En ? "Adjust Vertical Position" : "Отступ от верхнего края")] public float ApiAdjustVPosition { get; set; }
        }

        public class NotifyConfig
        {
            [JsonProperty(En ? "Do you use the Notify? [true/false]" : "Использовать ли Notify? [true/false]")] public bool IsNotify { get; set; }
            [JsonProperty(En ? "Type" : "Тип")] public string Type { get; set; }
        }

        public class ColorConfig
        {
            [JsonProperty("r")] public float R { get; set; }
            [JsonProperty("g")] public float G { get; set; }
            [JsonProperty("b")] public float B { get; set; }
        }

        public class MarkerConfig
        {
            [JsonProperty(En ? "Enabled? [true/false]" : "Включен? [true/false]")] public bool Enabled { get; set; }
            [JsonProperty(En ? "Name" : "Название")] public string Name { get; set; }
            [JsonProperty(En ? "Radius" : "Радиус")] public float Radius { get; set; }
            [JsonProperty(En ? "Alpha" : "Прозрачность")] public float Alpha { get; set; }
            [JsonProperty(En ? "Marker color" : "Цвет маркера")] public ColorConfig Color { get; set; }
        }

        private class PluginConfig
        {
            [JsonProperty(En ? "List of patrol boat presets" : "Список пресетов патрульных лодок")] public List<BoatConfig> Boats { get; set; }
            [JsonProperty(En ? "NPC Turret Damage Multiplier" : "Множитель урона от турелей по NPC")] public float TurretDamageScale { get; set; }
            [JsonProperty(En ? "Prefix of chat messages" : "Префикс сообщений в чате")] public string Prefix { get; set; }
            [JsonProperty(En ? "Do you use the chat? [true/false]" : "Использовать ли чат? [true/false]")] public bool IsChat { get; set; }
            [JsonProperty(En ? "GUI Announcements setting" : "Настройка GUI Announcements")] public GuiAnnouncementsConfig GuiAnnouncements { get; set; }
            [JsonProperty(En ? "Notify setting" : "Настройка Notify")] public NotifyConfig Notify { get; set; }
            [JsonProperty(En ? "The time before the patrol boat explodes when it hits the ground [sec.]" : "Время до взрыва патрульной лодки при ударе о землю [sec.]")] public float TimeKillGround { get; set; }
            [JsonProperty(En ? "The time of existence of the patrol boat before its destroy [sec.]" : "Время существования патрульной лодки до ее уничтожения [sec.]")] public float TimeKillLoot { get; set; }
            [JsonProperty(En ? "Prevent players from dealing damage to NPCs if they are out of the patrol boat's Sense Range? [true/false]" : "Запретить игрокам наносить урон по NPC, если они находятся вне радиуса обзора патрульной лодки? [true/false]")] public bool PreventDamageRange { get; set; }
            [JsonProperty(En ? "Minimum water depth where a patrol boat can appear" : "Минимальная глубина воды, где может появиться патрульная лодка")] public float WaterDepthSpawn { get; set; }
            [JsonProperty(En ? "Configuration version" : "Версия конфигурации")] public VersionNumber PluginVersion { get; set; }

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    Boats = new List<BoatConfig>
                    {
                        new BoatConfig
                        {
                            ChaseRange = 400f,
                            SenseRange = 200f,
                            MinAmount = 6,
                            MaxAmount = 6,
                            Marker = new MarkerConfig
                            {
                                Enabled = false,
                                Name = "Water Patrol",
                                Radius = 0.4f,
                                Alpha = 0.6f,
                                Color = new ColorConfig { R = 0.81f, G = 0.25f, B = 0.15f }
                            },
                            EnginePower = 2000f,
                            Crate = new CrateConfig
                            {
                                TypeLootTable = 2,
                                PrefabLootTable = new PrefabLootTableConfig
                                {
                                    Min = 1,
                                    Max = 1,
                                    UseCount = false,
                                    Prefabs = new List<PrefabConfig>
                                    {
                                        new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_oilrig.prefab" },
                                        new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/bundled/prefabs/radtown/crate_normal_2.prefab" },
                                        new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/bundled/prefabs/radtown/underwater_labs/tech_parts_2.prefab" },
                                        new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/bundled/prefabs/radtown/underwater_labs/crate_fuel.prefab" },
                                        new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/bundled/prefabs/radtown/crate_normal.prefab" }
                                    }
                                },
                                OwnLootTable = new LootTableConfig
                                {
                                    Min = 1,
                                    Max = 1,
                                    UseCount = false,
                                    Items = new List<ItemConfig>
                                    {
                                        new ItemConfig { ShortName = "diesel_barrel", MinAmount = 1, MaxAmount = 1, Chance = 50f, IsBluePrint = false, SkinID = 0, Name = "" },
                                        new ItemConfig { ShortName = "scrap", MinAmount = 50, MaxAmount = 100, Chance = 100f, IsBluePrint = false, SkinID = 0, Name = "" },
                                        new ItemConfig { ShortName = "syringe.medical", MinAmount = 1, MaxAmount = 2, Chance = 100.0f, IsBluePrint = false, SkinID = 0, Name = "" }
                                    }
                                }
                            },
                            Driver = new DriverConfig
                            {
                                Name = "Driver",
                                Health = 200f,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "shoes.boots", SkinID = 826587881 },
                                    new NpcWear { ShortName = "pants", SkinID = 1552705077 },
                                    new NpcWear { ShortName = "hoodie", SkinID = 1552703337 },
                                    new NpcWear { ShortName = "burlap.gloves", SkinID = 1552705918 },
                                    new NpcWear { ShortName = "roadsign.kilt", SkinID = 1624102935 },
                                    new NpcWear { ShortName = "metal.plate.torso", SkinID = 2843462369 },
                                    new NpcWear { ShortName = "metal.facemask", SkinID = 2843464348 }
                                }
                            },
                            Npc1 = new NpcConfig
                            {
                                Name = "Patrolman",
                                Health = 125f,
                                AttackRangeMultiplier = 1.5f,
                                MemoryDuration = 20f,
                                DamageScale = 0.7f,
                                AimConeScale = 1.5f,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "shoes.boots", SkinID = 826587881 },
                                    new NpcWear { ShortName = "pants", SkinID = 1552705077 },
                                    new NpcWear { ShortName = "hoodie", SkinID = 1552703337 },
                                    new NpcWear { ShortName = "burlap.gloves", SkinID = 1552705918 },
                                    new NpcWear { ShortName = "roadsign.kilt", SkinID = 1624102935 },
                                    new NpcWear { ShortName = "roadsign.jacket", SkinID = 1624100124 },
                                    new NpcWear { ShortName = "coffeecan.helmet", SkinID = 1624104393 }
                                },
                                BeltItems = new HashSet<NpcBelt>
                                {
                                    new NpcBelt { ShortName = "rifle.lr300", Amount = 1, SkinID = 0, Mods = new HashSet<string> { "weapon.mod.flashlight", "weapon.mod.holosight" }, Ammo = string.Empty },
                                    new NpcBelt { ShortName = "rocket.launcher", Amount = 1, SkinID = 0, Mods = new HashSet<string>(), Ammo = string.Empty },
                                    new NpcBelt { ShortName = "syringe.medical", Amount = 3, SkinID = 0, Mods = new HashSet<string>(), Ammo = string.Empty }
                                },
                                Kit = ""
                            },
                            Npc2 = new NpcConfig
                            {
                                Name = "Patrolman",
                                Health = 125f,
                                AttackRangeMultiplier = 1.5f,
                                MemoryDuration = 20f,
                                DamageScale = 0.7f,
                                AimConeScale = 1.5f,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "shoes.boots", SkinID = 826587881 },
                                    new NpcWear { ShortName = "pants", SkinID = 1552705077 },
                                    new NpcWear { ShortName = "hoodie", SkinID = 1552703337 },
                                    new NpcWear { ShortName = "burlap.gloves", SkinID = 1552705918 },
                                    new NpcWear { ShortName = "roadsign.kilt", SkinID = 1624102935 },
                                    new NpcWear { ShortName = "roadsign.jacket", SkinID = 1624100124 },
                                    new NpcWear { ShortName = "coffeecan.helmet", SkinID = 1624104393 }
                                },
                                BeltItems = new HashSet<NpcBelt>
                                {
                                    new NpcBelt { ShortName = "rifle.lr300", Amount = 1, SkinID = 0, Mods = new HashSet<string> { "weapon.mod.flashlight", "weapon.mod.holosight" }, Ammo = string.Empty },
                                    new NpcBelt { ShortName = "rocket.launcher", Amount = 1, SkinID = 0, Mods = new HashSet<string>(), Ammo = string.Empty },
                                    new NpcBelt { ShortName = "syringe.medical", Amount = 3, SkinID = 0, Mods = new HashSet<string>(), Ammo = string.Empty }
                                },
                                Kit = ""
                            },
                            Npc3 = new NpcConfig
                            {
                                Name = "Patrolman",
                                Health = 125f,
                                AttackRangeMultiplier = 1.5f,
                                MemoryDuration = 20f,
                                DamageScale = 0.7f,
                                AimConeScale = 1.5f,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "shoes.boots", SkinID = 826587881 },
                                    new NpcWear { ShortName = "pants", SkinID = 1552705077 },
                                    new NpcWear { ShortName = "hoodie", SkinID = 1552703337 },
                                    new NpcWear { ShortName = "burlap.gloves", SkinID = 1552705918 },
                                    new NpcWear { ShortName = "roadsign.kilt", SkinID = 1624102935 },
                                    new NpcWear { ShortName = "roadsign.jacket", SkinID = 1624100124 },
                                    new NpcWear { ShortName = "coffeecan.helmet", SkinID = 1624104393 }
                                },
                                BeltItems = new HashSet<NpcBelt>
                                {
                                    new NpcBelt { ShortName = "rifle.lr300", Amount = 1, SkinID = 0, Mods = new HashSet<string> { "weapon.mod.flashlight", "weapon.mod.holosight" }, Ammo = string.Empty },
                                    new NpcBelt { ShortName = "rocket.launcher", Amount = 1, SkinID = 0, Mods = new HashSet<string>(), Ammo = string.Empty },
                                    new NpcBelt { ShortName = "syringe.medical", Amount = 3, SkinID = 0, Mods = new HashSet<string>(), Ammo = string.Empty }
                                },
                                Kit = ""
                            },
                            Npc4 = new NpcConfig
                            {
                                Name = "Patrolman",
                                Health = 125f,
                                AttackRangeMultiplier = 1.5f,
                                MemoryDuration = 20f,
                                DamageScale = 0.7f,
                                AimConeScale = 1.5f,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "shoes.boots", SkinID = 826587881 },
                                    new NpcWear { ShortName = "pants", SkinID = 1552705077 },
                                    new NpcWear { ShortName = "hoodie", SkinID = 1552703337 },
                                    new NpcWear { ShortName = "burlap.gloves", SkinID = 1552705918 },
                                    new NpcWear { ShortName = "roadsign.kilt", SkinID = 1624102935 },
                                    new NpcWear { ShortName = "roadsign.jacket", SkinID = 1624100124 },
                                    new NpcWear { ShortName = "coffeecan.helmet", SkinID = 1624104393 }
                                },
                                BeltItems = new HashSet<NpcBelt>
                                {
                                    new NpcBelt { ShortName = "rifle.lr300", Amount = 1, SkinID = 0, Mods = new HashSet<string> { "weapon.mod.flashlight", "weapon.mod.holosight" }, Ammo = string.Empty },
                                    new NpcBelt { ShortName = "rocket.launcher", Amount = 1, SkinID = 0, Mods = new HashSet<string>(), Ammo = string.Empty },
                                    new NpcBelt { ShortName = "syringe.medical", Amount = 3, SkinID = 0, Mods = new HashSet<string>(), Ammo = string.Empty }
                                },
                                Kit = ""
                            },
                            Npc5 = new NpcConfig
                            {
                                Name = "Patrolman",
                                Health = 125f,
                                AttackRangeMultiplier = 1.5f,
                                MemoryDuration = 20f,
                                DamageScale = 0.7f,
                                AimConeScale = 1.5f,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "shoes.boots", SkinID = 826587881 },
                                    new NpcWear { ShortName = "pants", SkinID = 1552705077 },
                                    new NpcWear { ShortName = "hoodie", SkinID = 1552703337 },
                                    new NpcWear { ShortName = "burlap.gloves", SkinID = 1552705918 },
                                    new NpcWear { ShortName = "roadsign.kilt", SkinID = 1624102935 },
                                    new NpcWear { ShortName = "roadsign.jacket", SkinID = 1624100124 },
                                    new NpcWear { ShortName = "coffeecan.helmet", SkinID = 1624104393 }
                                },
                                BeltItems = new HashSet<NpcBelt>
                                {
                                    new NpcBelt { ShortName = "rifle.lr300", Amount = 1, SkinID = 0, Mods = new HashSet<string> { "weapon.mod.flashlight", "weapon.mod.holosight" }, Ammo = string.Empty },
                                    new NpcBelt { ShortName = "rocket.launcher", Amount = 1, SkinID = 0, Mods = new HashSet<string>(), Ammo = string.Empty },
                                    new NpcBelt { ShortName = "syringe.medical", Amount = 3, SkinID = 0, Mods = new HashSet<string>(), Ammo = string.Empty }
                                },
                                Kit = ""
                            }
                        },
                        new BoatConfig
                        {
                            ChaseRange = 400f,
                            SenseRange = 200f,
                            MinAmount = 4,
                            MaxAmount = 4,
                            Marker = new MarkerConfig
                            {
                                Enabled = false,
                                Name = "Water Patrol",
                                Radius = 0.4f,
                                Alpha = 0.6f,
                                Color = new ColorConfig { R = 0.81f, G = 0.25f, B = 0.15f }
                            },
                            EnginePower = 2000f,
                            Crate = new CrateConfig
                            {
                                TypeLootTable = 2,
                                PrefabLootTable = new PrefabLootTableConfig
                                {
                                    Min = 1,
                                    Max = 1,
                                    UseCount = false,
                                    Prefabs = new List<PrefabConfig>
                                    {
                                        new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_oilrig.prefab" },
                                        new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/bundled/prefabs/radtown/crate_elite.prefab" },
                                        new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/bundled/prefabs/radtown/underwater_labs/tech_parts_2.prefab" },
                                        new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/bundled/prefabs/radtown/underwater_labs/crate_fuel.prefab" },
                                        new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/bundled/prefabs/radtown/crate_normal.prefab" }
                                    }
                                },
                                OwnLootTable = new LootTableConfig
                                {
                                    Min = 1,
                                    Max = 1,
                                    UseCount = false,
                                    Items = new List<ItemConfig>
                                    {
                                        new ItemConfig { ShortName = "diesel_barrel", MinAmount = 1, MaxAmount = 1, Chance = 100f, IsBluePrint = false, SkinID = 0, Name = "" },
                                        new ItemConfig { ShortName = "scrap", MinAmount = 50, MaxAmount = 100, Chance = 100f, IsBluePrint = false, SkinID = 0, Name = "" },
                                        new ItemConfig { ShortName = "syringe.medical", MinAmount = 1, MaxAmount = 2, Chance = 100.0f, IsBluePrint = false, SkinID = 0, Name = "" }
                                    }
                                }
                            },
                            Driver = new DriverConfig
                            {
                                Name = "Driver",
                                Health = 400f,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "shoes.boots", SkinID = 971729488 },
                                    new NpcWear { ShortName = "pants", SkinID = 963501284 },
                                    new NpcWear { ShortName = "hoodie", SkinID = 963496340 },
                                    new NpcWear { ShortName = "burlap.gloves", SkinID = 1552705918 },
                                    new NpcWear { ShortName = "roadsign.kilt", SkinID = 1624102935 },
                                    new NpcWear { ShortName = "metal.plate.torso", SkinID = 1192804139 },
                                    new NpcWear { ShortName = "metal.facemask", SkinID = 1121237616 }
                                }
                            },
                            Npc1 = new NpcConfig
                            {
                                Name = "Patrolman",
                                Health = 200f,
                                AttackRangeMultiplier = 1.5f,
                                MemoryDuration = 20f,
                                DamageScale = 0.7f,
                                AimConeScale = 1.5f,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "shoes.boots", SkinID = 971729488 },
                                    new NpcWear { ShortName = "pants", SkinID = 963501284 },
                                    new NpcWear { ShortName = "hoodie", SkinID = 963496340 },
                                    new NpcWear { ShortName = "burlap.gloves", SkinID = 1552705918 },
                                    new NpcWear { ShortName = "roadsign.kilt", SkinID = 1624102935 },
                                    new NpcWear { ShortName = "metal.plate.torso", SkinID = 1192804139 },
                                    new NpcWear { ShortName = "metal.facemask", SkinID = 1121237616 }
                                },
                                BeltItems = new HashSet<NpcBelt>
                                {
                                    new NpcBelt { ShortName = "rifle.ak.ice", Amount = 1, SkinID = 0, Mods = new HashSet<string> { "weapon.mod.flashlight", "weapon.mod.holosight" }, Ammo = string.Empty },
                                    new NpcBelt { ShortName = "rocket.launcher", Amount = 1, SkinID = 0, Mods = new HashSet<string>(), Ammo = string.Empty },
                                    new NpcBelt { ShortName = "syringe.medical", Amount = 1, SkinID = 0, Mods = new HashSet<string>(), Ammo = string.Empty }
                                },
                                Kit = ""
                            },
                            Npc2 = new NpcConfig
                            {
                                Name = "Patrolman",
                                Health = 200f,
                                AttackRangeMultiplier = 1.5f,
                                MemoryDuration = 20f,
                                DamageScale = 0.7f,
                                AimConeScale = 1.5f,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "shoes.boots", SkinID = 971729488 },
                                    new NpcWear { ShortName = "pants", SkinID = 963501284 },
                                    new NpcWear { ShortName = "hoodie", SkinID = 963496340 },
                                    new NpcWear { ShortName = "burlap.gloves", SkinID = 1552705918 },
                                    new NpcWear { ShortName = "roadsign.kilt", SkinID = 1624102935 },
                                    new NpcWear { ShortName = "metal.plate.torso", SkinID = 1192804139 },
                                    new NpcWear { ShortName = "metal.facemask", SkinID = 1121237616 }
                                },
                                BeltItems = new HashSet<NpcBelt>
                                {
                                    new NpcBelt { ShortName = "rifle.ak.ice", Amount = 1, SkinID = 0, Mods = new HashSet<string> { "weapon.mod.flashlight", "weapon.mod.holosight" }, Ammo = string.Empty },
                                    new NpcBelt { ShortName = "rocket.launcher", Amount = 1, SkinID = 0, Mods = new HashSet<string>(), Ammo = string.Empty },
                                    new NpcBelt { ShortName = "syringe.medical", Amount = 1, SkinID = 0, Mods = new HashSet<string>(), Ammo = string.Empty }
                                },
                                Kit = ""
                            },
                            Npc3 = null,
                            Npc4 = new NpcConfig
                            {
                                Name = "Patrolman",
                                Health = 200f,
                                AttackRangeMultiplier = 1.5f,
                                MemoryDuration = 20f,
                                DamageScale = 0.7f,
                                AimConeScale = 1.5f,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "shoes.boots", SkinID = 971729488 },
                                    new NpcWear { ShortName = "pants", SkinID = 963501284 },
                                    new NpcWear { ShortName = "hoodie", SkinID = 963496340 },
                                    new NpcWear { ShortName = "burlap.gloves", SkinID = 1552705918 },
                                    new NpcWear { ShortName = "roadsign.kilt", SkinID = 1624102935 },
                                    new NpcWear { ShortName = "metal.plate.torso", SkinID = 1192804139 },
                                    new NpcWear { ShortName = "metal.facemask", SkinID = 1121237616 }
                                },
                                BeltItems = new HashSet<NpcBelt>
                                {
                                    new NpcBelt { ShortName = "multiplegrenadelauncher", Amount = 1, SkinID = 0, Mods = new HashSet<string> { "weapon.mod.flashlight", "weapon.mod.holosight" }, Ammo = string.Empty },
                                    new NpcBelt { ShortName = "rocket.launcher", Amount = 1, SkinID = 0, Mods = new HashSet<string>(), Ammo = string.Empty },
                                    new NpcBelt { ShortName = "syringe.medical", Amount = 1, SkinID = 0, Mods = new HashSet<string>(), Ammo = string.Empty }
                                },
                                Kit = ""
                            },
                            Npc5 = null
                        },
                        new BoatConfig
                        {
                            ChaseRange = 200f,
                            SenseRange = 100f,
                            MinAmount = 8,
                            MaxAmount = 8,
                            Marker = new MarkerConfig
                            {
                                Enabled = false,
                                Name = "Water Patrol",
                                Radius = 0.4f,
                                Alpha = 0.6f,
                                Color = new ColorConfig { R = 0.81f, G = 0.25f, B = 0.15f }
                            },
                            EnginePower = 2000f,
                            Crate = new CrateConfig
                            {
                                TypeLootTable = 2,
                                PrefabLootTable = new PrefabLootTableConfig
                                {
                                    Min = 1,
                                    Max = 1,
                                    UseCount = false,
                                    Prefabs = new List<PrefabConfig>
                                    {
                                        new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_oilrig.prefab" },
                                        new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/bundled/prefabs/radtown/crate_normal_2.prefab" },
                                        new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/bundled/prefabs/radtown/underwater_labs/crate_fuel.prefab" },
                                        new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/bundled/prefabs/radtown/crate_tools.prefab" }
                                    }
                                },
                                OwnLootTable = new LootTableConfig
                                {
                                    Min = 1,
                                    Max = 1,
                                    UseCount = false,
                                    Items = new List<ItemConfig>
                                    {
                                        new ItemConfig { ShortName = "scrap", MinAmount = 25, MaxAmount = 50, Chance = 100f, IsBluePrint = false, SkinID = 0, Name = "" },
                                        new ItemConfig { ShortName = "syringe.medical", MinAmount = 1, MaxAmount = 2, Chance = 100.0f, IsBluePrint = false, SkinID = 0, Name = "" }
                                    }
                                }
                            },
                            Driver = new DriverConfig
                            {
                                Name = "Driver",
                                Health = 150f,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "shoes.boots", SkinID = 826587881 },
                                    new NpcWear { ShortName = "pants", SkinID = 1552705077 },
                                    new NpcWear { ShortName = "hoodie", SkinID = 1552703337 },
                                    new NpcWear { ShortName = "burlap.gloves", SkinID = 1552705918 },
                                    new NpcWear { ShortName = "roadsign.kilt", SkinID = 1624102935 },
                                    new NpcWear { ShortName = "metal.plate.torso", SkinID = 2843462369 },
                                    new NpcWear { ShortName = "metal.facemask", SkinID = 2843464348 }
                                }
                            },
                            Npc1 = new NpcConfig
                            {
                                Name = "Patrolman",
                                Health = 100f,
                                AttackRangeMultiplier = 1.5f,
                                MemoryDuration = 20f,
                                DamageScale = 0.7f,
                                AimConeScale = 1.5f,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "shoes.boots", SkinID = 2570215282 },
                                    new NpcWear { ShortName = "burlap.shirt", SkinID = 2556987808 },
                                    new NpcWear { ShortName = "burlap.trousers", SkinID = 2556988996 },
                                    new NpcWear { ShortName = "hat.boonie", SkinID = 2557702256 }
                                },
                                BeltItems = new HashSet<NpcBelt>
                                {
                                    new NpcBelt { ShortName = "smg.thompson", Amount = 1, SkinID = 0, Mods = new HashSet<string> { "weapon.mod.flashlight", "weapon.mod.holosight" }, Ammo = string.Empty },
                                    new NpcBelt { ShortName = "rocket.launcher", Amount = 1, SkinID = 0, Mods = new HashSet<string>(), Ammo = string.Empty },
                                    new NpcBelt { ShortName = "syringe.medical", Amount = 3, SkinID = 0, Mods = new HashSet<string>(), Ammo = string.Empty }
                                },
                                Kit = ""
                            },
                            Npc2 = new NpcConfig
                            {
                                Name = "Patrolman",
                                Health = 100f,
                                AttackRangeMultiplier = 1.5f,
                                MemoryDuration = 20f,
                                DamageScale = 0.7f,
                                AimConeScale = 1.5f,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "shoes.boots", SkinID = 2570215282 },
                                    new NpcWear { ShortName = "burlap.shirt", SkinID = 2556987808 },
                                    new NpcWear { ShortName = "burlap.trousers", SkinID = 2556988996 },
                                    new NpcWear { ShortName = "hat.boonie", SkinID = 2557702256 }
                                },
                                BeltItems = new HashSet<NpcBelt>
                                {
                                    new NpcBelt { ShortName = "smg.thompson", Amount = 1, SkinID = 0, Mods = new HashSet<string> { "weapon.mod.flashlight", "weapon.mod.holosight" }, Ammo = string.Empty },
                                    new NpcBelt { ShortName = "rocket.launcher", Amount = 1, SkinID = 0, Mods = new HashSet<string>(), Ammo = string.Empty },
                                    new NpcBelt { ShortName = "syringe.medical", Amount = 3, SkinID = 0, Mods = new HashSet<string>(), Ammo = string.Empty }
                                },
                                Kit = ""
                            },
                            Npc3 = new NpcConfig
                            {
                                Name = "Patrolman",
                                Health = 100f,
                                AttackRangeMultiplier = 1.5f,
                                MemoryDuration = 20f,
                                DamageScale = 0.7f,
                                AimConeScale = 1.5f,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "shoes.boots", SkinID = 2570215282 },
                                    new NpcWear { ShortName = "burlap.shirt", SkinID = 2556987808 },
                                    new NpcWear { ShortName = "burlap.trousers", SkinID = 2556988996 },
                                    new NpcWear { ShortName = "hat.boonie", SkinID = 2557702256 }
                                },
                                BeltItems = new HashSet<NpcBelt>
                                {
                                    new NpcBelt { ShortName = "smg.thompson", Amount = 1, SkinID = 0, Mods = new HashSet<string> { "weapon.mod.flashlight", "weapon.mod.holosight" }, Ammo = string.Empty },
                                    new NpcBelt { ShortName = "rocket.launcher", Amount = 1, SkinID = 0, Mods = new HashSet<string>(), Ammo = string.Empty },
                                    new NpcBelt { ShortName = "syringe.medical", Amount = 3, SkinID = 0, Mods = new HashSet<string>(), Ammo = string.Empty }
                                },
                                Kit = ""
                            },
                            Npc4 = null,
                            Npc5 = null
                        }
                    },
                    TurretDamageScale = 1f,
                    Prefix = "[WaterPatrol]",
                    IsChat = true,
                    GuiAnnouncements = new GuiAnnouncementsConfig
                    {
                        IsGuiAnnouncements = false,
                        BannerColor = "Orange",
                        TextColor = "White",
                        ApiAdjustVPosition = 0.03f
                    },
                    Notify = new NotifyConfig
                    {
                        IsNotify = false,
                        Type = "0"
                    },
                    TimeKillGround = 3f,
                    TimeKillLoot = 120f,
                    PreventDamageRange = true,
                    WaterDepthSpawn = 15f,
                    PluginVersion = new VersionNumber()
                };
            }
        }
        #endregion Config

        #region Lang
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoDamage"] = "{0} You <color=#ce3f27>cannot</color> damage an NPC from your position!",
                ["KillBoat"] = "{0} The patrol boat <color=#ce3f27>will be destroyed</color> in <color=#55aaff>{1} sec.</color>!"
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoDamage"] = "{0} Вы <color=#ce3f27>не можете</color> нанести урон NPC с текущей позиции!",
                ["KillBoat"] = "{0} Через <color=#55aaff>{1} сек.</color> патрульная лодка будет <color=#ce3f27>уничтожена</color>!"
            }, this, "ru");
        }

        private string GetMessage(string langKey, string userID) => lang.GetMessage(langKey, _ins, userID);

        private string GetMessage(string langKey, string userID, params object[] args) => (args.Length == 0) ? GetMessage(langKey, userID) : string.Format(GetMessage(langKey, userID), args);
        #endregion Lang

        #region Controller
        private void SpawnBoat(int index)
        {
            RHIB rhib = GameManager.server.CreateEntity("assets/content/vehicles/boats/rhib/rhib.prefab", GetRandomNavMeshPos(), Quaternion.identity) as RHIB;
            rhib.enableSaving = false;
            rhib.Spawn();

            ControllerRHIB controller = rhib.gameObject.AddComponent<ControllerRHIB>();
            controller.InitBoat(index);
            _controllers.Add(controller);
        }

        private readonly HashSet<ControllerRHIB> _controllers = new HashSet<ControllerRHIB>();

        internal class ControllerRHIB : FacepunchBehaviour
        {
            internal RHIB Rhib;

            internal int IndexConfig = 0;

            private float _chaseRange = 0f;
            internal float SenseRange = 0f;

            internal Vector3 HomePosition = Vector3.zero;
            private bool _isGoHome = false;

            private BasePlayer _targetBP = null;
            private float _lastDistance = 0f;
            private Vector3 _targetPos = Vector3.zero;
            private List<Vector3> _path = new List<Vector3>();

            private float _steeringScale = 0f;
            private float _engineThrust = 0f;
            private float _enginePower = 0f;

            private float _timeKill = 0f;

            internal CustomSphereColliderPlayer SphereColliderPlayer = null;
            private CustomSphereColliderJunkPile SphereColliderJunkPile = null;

            internal StorageContainer Crate = null;
            private BaseVehicleSeat _driverSeat = null;
            internal readonly List<BaseVehicleSeat> PassengerSeats = new List<BaseVehicleSeat>();

            internal ScientistNPC Driver = null;
            internal List<ScientistNPC> Passengers = new List<ScientistNPC>();

            private MapMarkerGenericRadius _mapmarker;
            private VendingMachineMapMarker _vendingMarker;

            public float DistanceFromBase => Vector3.Distance(transform.position, HomePosition);

            public float DistanceToTarget => Vector3.Distance(transform.position, _targetBP.transform.position);

            private bool IsNewPath => Vector3.Distance(transform.position, _path[1]) < Vector3.Distance(_path[0], _path[1]) || Vector3.Distance(transform.position, _targetPos) < 5f;

            private void Awake()
            {
                Rhib = GetComponent<RHIB>();
                HomePosition = transform.position;
            }

            internal void InitBoat(int index)
            {
                IndexConfig = index;
                BoatConfig config = _ins._config.Boats[IndexConfig];

                _chaseRange = config.ChaseRange;
                SenseRange = config.SenseRange;

                _enginePower = config.EnginePower;

                foreach (BaseEntity entity in Rhib.children)
                {
                    if (entity.ShortPrefabName == "fuel_storage")
                    {
                        SphereColliderPlayer = entity.gameObject.AddComponent<CustomSphereColliderPlayer>();
                        SphereColliderPlayer.Init(SenseRange);
                    }
                    else if (entity.ShortPrefabName == "rhib_storage") Crate = entity as StorageContainer;
                    else if (entity.ShortPrefabName == "standingdriver")
                    {
                        _driverSeat = entity as BaseVehicleSeat;
                        SphereColliderJunkPile = entity.gameObject.AddComponent<CustomSphereColliderJunkPile>();
                    }
                    else if (entity.ShortPrefabName == "smallboatpassenger") PassengerSeats.Add(entity as BaseVehicleSeat);
                }

                SpawnDriver(config.Driver);

                for (int i = 0; i < PassengerSeats.Count; i++)
                {
                    NpcConfig npcConfig = i == 0 ? config.Npc1 : i == 1 ? config.Npc2 : i == 2 ? config.Npc3 : i == 3 ? config.Npc4 : config.Npc5;
                    if (npcConfig != null) SpawnPassenger(npcConfig, PassengerSeats[i]);
                }

                if (config.Marker.Enabled) SpawnMapMarker(config.Marker);
            }

            private void OnDestroy()
            {
                CancelInvoke(UpdateMapMarker);
                if (_mapmarker.IsExists()) _mapmarker.Kill();
                if (_vendingMarker.IsExists()) _vendingMarker.Kill();

                if (Driver.IsExists()) Driver.Kill();
                foreach (ScientistNPC npc in Passengers) if (npc.IsExists()) npc.Kill();

                Destroy(SphereColliderPlayer);
                Destroy(SphereColliderJunkPile);
            }

            private void FixedUpdate()
            {
                if (Driver == null) return;

                if (Rhib.buoyancy.submergedFraction < 0.35f)
                {
                    _timeKill += Time.deltaTime;
                    if (_timeKill > _ins._config.TimeKillGround) _ins.CrashBoat(this);
                    return;
                }

                _timeKill = 0f;

                if (CanTargetBasePlayer(_targetBP))
                {
                    if (DistanceToTarget < _ins.DISTANCE_PER_SQUARE) return;
                    if (Math.Abs(DistanceToTarget - _lastDistance) < 5f) return;
                }

                if (DistanceFromBase < _ins.DISTANCE_PER_SQUARE)
                {
                    if (SphereColliderPlayer.Players.Count == 0) return;
                    if (SphereColliderPlayer.Players.Count == 1 && !CanTargetBasePlayer(SphereColliderPlayer.Players[0])) return;
                }

                if (_targetPos == Vector3.zero)
                {
                    ClearTarget();

                    if (!_isGoHome && DistanceFromBase > _chaseRange) _isGoHome = true;

                    if (_isGoHome) _path = _ins.GetPath(Rhib, HomePosition);
                    else
                    {
                        _targetBP = SphereColliderPlayer.Players.WhereMin(CanTargetBasePlayer, x => Vector3.Distance(x.transform.position, transform.position));
                        if (_targetBP != null) _path = _ins.GetPath(Rhib, _targetBP.transform.position);
                        else if (DistanceFromBase > _ins.DISTANCE_PER_SQUARE)
                        {
                            _isGoHome = true;
                            _path = _ins.GetPath(Rhib, HomePosition);
                        }
                    }

                    int count = _path.Count;

                    if (count == 0)
                    {
                        if (_isGoHome) _targetPos = HomePosition;
                        return;
                    }
                    else if (count == 1) _targetPos = _path[0];
                    else if (count == 2) _targetPos = _path[1];
                    else _targetPos = _path[2];
                }
                else
                {
                    if (_isGoHome && DistanceFromBase < _ins.DISTANCE_PER_SQUARE)
                    {
                        _isGoHome = false;
                        ClearTarget();
                        return;
                    }
                    if (_path.Count == 1)
                    {
                        if (Vector3.Distance(transform.position, _targetPos) < _ins.DISTANCE_PER_SQUARE)
                        {
                            if (_targetBP != null)
                            {
                                _lastDistance = DistanceToTarget;
                                _targetPos = Vector3.zero;
                                _path.Clear();
                            }
                            else ClearTarget();
                            return;
                        }
                    }
                    else if (_path.Count > 1)
                    {
                        if (IsNewPath)
                        {
                            ClearTarget();
                            return;
                        }
                    }
                }

                if (WaterLevel.Test(Rhib.thrustPoint.position, true, Rhib))
                {
                    float dot = Vector3.Dot(transform.forward, (_targetPos - transform.position).normalized);
                    _steeringScale = -0.225f * dot + 0.275f;
                    _engineThrust = _enginePower / 4f * dot + 3f / 4f * _enginePower;
                    float dotRight = Vector3.Dot(transform.right, (_targetPos - transform.position).normalized);
                    float steering = dotRight > 0 ? -1f : dotRight < 0 ? 1f : 0f;
                    Vector3 vector3 = (Rhib.transform.forward + Rhib.transform.right * steering * _steeringScale).normalized * _engineThrust;
                    Rhib.rigidBody.AddForceAtPosition(vector3, Rhib.thrustPoint.position, ForceMode.Force);
                }
            }

            private void ClearTarget()
            {
                _targetBP = null;
                _lastDistance = 0f;
                _targetPos = Vector3.zero;
                _path.Clear();
            }

            internal bool CanTargetBasePlayer(BasePlayer basePlayer)
            {
                if (!basePlayer.IsPlayer() || basePlayer.IsDead()) return false;
                if (basePlayer.IsSleeping() || basePlayer.IsWounded() || basePlayer.IsSwimming()) return false;
                if (TerrainMeta.HeightMap.GetHeight(basePlayer.transform.position) > 1f) return false;
                if (basePlayer.InSafeZone() || !_ins.IsValidPosToMonuments(basePlayer.transform.position)) return false;
                if (basePlayer._limitedNetworking) return false;
                return true;
            }

            internal void SpawnDriver(DriverConfig config)
            {
                Driver = (ScientistNPC)_ins.NpcSpawn.Call("SpawnNpc", transform.position, GetObjectConfigDriver(config));
                _driverSeat.AttemptMount(Driver, false);
                if (Driver.NavAgent.enabled)
                {
                    Driver.NavAgent.destination = Driver.transform.position;
                    Driver.NavAgent.isStopped = true;
                    Driver.NavAgent.enabled = false;
                }
            }

            internal void SpawnPassenger(NpcConfig config, BaseVehicleSeat seat)
            {
                ScientistNPC npc = (ScientistNPC)_ins.NpcSpawn.Call("SpawnNpc", transform.position, GetObjectConfig(config));
                seat.AttemptMount(npc, false);
                if (npc.NavAgent.enabled)
                {
                    npc.NavAgent.destination = npc.transform.position;
                    npc.NavAgent.isStopped = true;
                    npc.NavAgent.enabled = false;
                }
                Passengers.Add(npc);
            }

            private JObject GetObjectConfig(NpcConfig config)
            {
                HashSet<string> states = new HashSet<string> { "IdleState", "CombatStationaryState" };
                if (config.BeltItems.Any(x => x.ShortName == "rocket.launcher" || x.ShortName == "explosive.timed")) states.Add("RaidState");
                return new JObject
                {
                    ["Name"] = config.Name,
                    ["WearItems"] = new JArray { config.WearItems.Select(x => new JObject { ["ShortName"] = x.ShortName, ["SkinID"] = x.SkinID }) },
                    ["BeltItems"] = new JArray { config.BeltItems.Select(x => new JObject { ["ShortName"] = x.ShortName, ["Amount"] = x.Amount, ["SkinID"] = x.SkinID, ["Mods"] = new JArray { x.Mods }, ["Ammo"] = x.Ammo }) },
                    ["Kit"] = config.Kit,
                    ["Health"] = config.Health,
                    ["RoamRange"] = 0f,
                    ["ChaseRange"] = 0f,
                    ["DamageScale"] = config.DamageScale,
                    ["TurretDamageScale"] = _ins._config.TurretDamageScale,
                    ["AimConeScale"] = config.AimConeScale,
                    ["DisableRadio"] = true,
                    ["CanUseWeaponMounted"] = true,
                    ["CanRunAwayWater"] = false,
                    ["Speed"] = 0f,
                    ["AreaMask"] = 1,
                    ["AgentTypeID"] = -1372625422,
                    ["HomePosition"] = string.Empty,
                    ["States"] = new JArray { states },
                    ["Sensory"] = new JObject
                    {
                        ["AttackRangeMultiplier"] = config.AttackRangeMultiplier,
                        ["SenseRange"] = SenseRange,
                        ["MemoryDuration"] = config.MemoryDuration,
                        ["CheckVisionCone"] = false,
                        ["VisionCone"] = 135f
                    }
                };
            }

            private JObject GetObjectConfigDriver(DriverConfig config)
            {
                HashSet<string> states = new HashSet<string> { "IdleState" };
                return new JObject
                {
                    ["Name"] = config.Name,
                    ["WearItems"] = new JArray { config.WearItems.Select(x => new JObject { ["ShortName"] = x.ShortName, ["SkinID"] = x.SkinID }) },
                    ["BeltItems"] = new JArray(),
                    ["Kit"] = string.Empty,
                    ["Health"] = config.Health,
                    ["RoamRange"] = 0f,
                    ["ChaseRange"] = 0f,
                    ["DamageScale"] = 0f,
                    ["TurretDamageScale"] = _ins._config.TurretDamageScale,
                    ["AimConeScale"] = 0f,
                    ["DisableRadio"] = false,
                    ["CanUseWeaponMounted"] = false,
                    ["CanRunAwayWater"] = false,
                    ["Speed"] = 0f,
                    ["AreaMask"] = 1,
                    ["AgentTypeID"] = -1372625422,
                    ["HomePosition"] = string.Empty,
                    ["States"] = new JArray { states },
                    ["Sensory"] = new JObject
                    {
                        ["AttackRangeMultiplier"] = 0f,
                        ["SenseRange"] = 0f,
                        ["MemoryDuration"] = 0f,
                        ["CheckVisionCone"] = false,
                        ["VisionCone"] = 135f
                    }
                };
            }

            internal void SpawnLoot()
            {
                Crate.inventory.ClearItemsContainer();
                CrateConfig config = _ins._config.Boats[IndexConfig].Crate;
                if (config.TypeLootTable == 1 || config.TypeLootTable == 2) _ins.AddToContainerPrefab(Crate.inventory, config.PrefabLootTable);
                if (config.TypeLootTable == 0 || config.TypeLootTable == 2) _ins.AddToContainerItem(Crate.inventory, config.OwnLootTable);
            }

            private void SpawnMapMarker(MarkerConfig config)
            {
                _mapmarker = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", transform.position) as MapMarkerGenericRadius;
                _mapmarker.Spawn();
                _mapmarker.radius = config.Radius;
                _mapmarker.alpha = config.Alpha;
                _mapmarker.color1 = new Color(config.Color.R, config.Color.G, config.Color.B);

                _vendingMarker = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab", transform.position) as VendingMachineMapMarker;
                _vendingMarker.Spawn();
                _vendingMarker.markerShopName = config.Name;

                InvokeRepeating(UpdateMapMarker, 0, 1f);
            }

            private void UpdateMapMarker()
            {
                _mapmarker.transform.position = transform.position;
                _mapmarker.SendUpdate();
                _mapmarker.SendNetworkUpdate();

                _vendingMarker.transform.position = transform.position;
                _vendingMarker.SendNetworkUpdate();
            }
        }

        internal class CustomSphereColliderPlayer : FacepunchBehaviour
        {
            internal SphereCollider SphereCollider;
            internal List<BasePlayer> Players = new List<BasePlayer>();

            internal void Init(float radius)
            {
                gameObject.layer = 3;
                SphereCollider = gameObject.AddComponent<SphereCollider>();
                SphereCollider.isTrigger = true;
                SphereCollider.radius = radius;
            }

            private void OnTriggerEnter(Collider other)
            {
                BasePlayer player = other.GetComponentInParent<BasePlayer>();
                if (player.IsPlayer() && !Players.Contains(player)) Players.Add(player);
            }

            private void OnTriggerExit(Collider other)
            {
                BasePlayer player = other.GetComponentInParent<BasePlayer>();
                if (player.IsPlayer()) Players.Remove(player);
            }

            internal void PlayerDeath(BasePlayer player) { if (Players.Contains(player)) Players.Remove(player); }
        }

        internal class CustomSphereColliderJunkPile : FacepunchBehaviour
        {
            internal SphereCollider SphereCollider;

            private void Awake()
            {
                gameObject.layer = 3;
                SphereCollider = gameObject.AddComponent<SphereCollider>();
                SphereCollider.isTrigger = true;
                SphereCollider.radius = 5f;
            }

            private void OnTriggerEnter(Collider other)
            {
                JunkPileWater junkPileWater = other.GetComponentInParent<JunkPileWater>();
                if (junkPileWater.IsExists()) junkPileWater.Kill();
            }
        }

        internal void CrashBoat(ControllerRHIB controller)
        {
            _controllers.Remove(controller);
            BoatConfig config = _config.Boats[controller.IndexConfig];
            int amountChance = UnityEngine.Random.Range(config.MinAmount, config.MaxAmount + 1);
            int amountInWorld = _controllers.Where(x => x.IndexConfig == controller.IndexConfig).Count;
            if (amountInWorld < amountChance) SpawnBoat(controller.IndexConfig);
            RHIB rhib = controller.Rhib;
            UnityEngine.Object.Destroy(controller);
            if (rhib.IsExists())
            {
                Effect.server.Run("assets/prefabs/npc/patrol helicopter/effects/heli_explosion.prefab", rhib.transform.position, Vector3.up, null, true);
                Effect.server.Run("assets/prefabs/npc/patrol helicopter/damage_effect_debris.prefab", rhib.transform.position, Vector3.up, null, true);
                rhib.Kill(BaseNetworkable.DestroyMode.Gib);
            }
        }
        #endregion Controller

        #region Oxide Hooks
        [PluginReference] private readonly Plugin NpcSpawn;

        private static WaterPatrol _ins;

        private void Init() => _ins = this;

        private void OnServerInitialized()
        {
            LoadDefaultMessages();
            CheckAllLootTables();
            CheckAllMonuments();
            FindNavMesh();
            for (int i = 0; i < _config.Boats.Count; i++)
            {
                BoatConfig config = _config.Boats[i];
                int amount = UnityEngine.Random.Range(config.MinAmount, config.MaxAmount + 1);
                for (int j = 0; j < amount; j++) SpawnBoat(i);
            }
        }

        private void Unload()
        {
            foreach (ControllerRHIB controller in _controllers) if (controller.Rhib.IsExists()) controller.Rhib.Kill();
            foreach (KeyValuePair<uint, RHIB> dic in _deathBoats) if (dic.Value.IsExists()) dic.Value.Kill();
            _ins = null;
        }

        private object OnEntityTakeDamage(ScientistNPC npc, HitInfo info)
        {
            if (npc == null || info == null) return null;
            BasePlayer attacker = info.InitiatorPlayer;
            if (!attacker.IsPlayer()) return null;
            ControllerRHIB controller = _controllers.FirstOrDefault(x => x.Passengers.Contains(npc) || x.Driver == npc);
            if (controller == null) return null;
            if (controller.CanTargetBasePlayer(attacker) && controller.SphereColliderPlayer.Players.Contains(attacker)) info.damageTypes.ScaleAll(10f);
            else
            {
                if (_config.PreventDamageRange)
                {
                    AlertToPlayer(attacker, GetMessage("NoDamage", attacker.UserIDString, _config.Prefix));
                    return true;
                }
                else info.damageTypes.ScaleAll(10f);
            }
            return null;
        }

        private object OnEntityTakeDamage(RHIB rhib, HitInfo info)
        {
            if (rhib == null || info == null) return null;
            if (_controllers.Any(x => x.Rhib == rhib)) return true;
            else return null;
        }

        private void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (!player.IsPlayer()) return;
            foreach (ControllerRHIB controller in _controllers) controller.SphereColliderPlayer.PlayerDeath(player);
        }

        private void OnCorpsePopulate(ScientistNPC entity, NPCPlayerCorpse corpse)
        {
            if (entity == null) return;
            ControllerRHIB controller = _controllers.FirstOrDefault(x => x.Passengers.Contains(entity) || x.Driver == entity);
            if (controller != null)
            {
                bool spawnDriver = false;
                bool spawnPassenger = false;
                if (controller.Driver == entity)
                {
                    if (controller.Passengers.Count > 1)
                    {
                        ScientistNPC npc = controller.Passengers.GetRandom();
                        controller.Passengers.Remove(npc);
                        if (npc.IsExists()) npc.Kill();
                        spawnDriver = true;
                    }
                }
                else
                {
                    controller.Passengers.Remove(entity);
                    if (controller.Passengers.Count == 0)
                    {
                        if (controller.Driver.IsExists())
                        {
                            controller.Driver.Kill();
                            spawnPassenger = true;
                        }
                        else
                        {
                            controller.SpawnLoot();
                            _deathBoats.Add(controller.Crate.net.ID, controller.Rhib);
                            _controllers.Remove(controller);
                            BoatConfig config = _config.Boats[controller.IndexConfig];
                            int amountChance = UnityEngine.Random.Range(config.MinAmount, config.MaxAmount + 1);
                            int amountInWorld = _controllers.Where(x => x.IndexConfig == controller.IndexConfig).Count;
                            if (amountInWorld < amountChance) SpawnBoat(controller.IndexConfig);
                            UnityEngine.Object.Destroy(controller);
                        }
                    }
                }
                NextTick(() =>
                {
                    if (corpse == null) return;
                    corpse.containers[0].ClearItemsContainer();
                    if (!corpse.IsDestroyed) corpse.Kill();
                    if (spawnDriver) controller.SpawnDriver(_config.Boats[controller.IndexConfig].Driver);
                    if (spawnPassenger)
                    {
                        BoatConfig boatConfig = _config.Boats[controller.IndexConfig];
                        if (boatConfig.Npc1 != null) controller.SpawnPassenger(boatConfig.Npc1, controller.PassengerSeats[0]);
                        else if (boatConfig.Npc2 != null) controller.SpawnPassenger(boatConfig.Npc2, controller.PassengerSeats[1]);
                        else if (boatConfig.Npc3 != null) controller.SpawnPassenger(boatConfig.Npc3, controller.PassengerSeats[2]);
                        else if (boatConfig.Npc4 != null) controller.SpawnPassenger(boatConfig.Npc4, controller.PassengerSeats[3]);
                        else if (boatConfig.Npc5 != null) controller.SpawnPassenger(boatConfig.Npc5, controller.PassengerSeats[4]);
                    }
                });
            }
        }

        private readonly Dictionary<uint, RHIB> _deathBoats = new Dictionary<uint, RHIB>();

        private void OnLootEntity(BasePlayer player, StorageContainer container)
        {
            if (!player.IsPlayer() || !container.IsExists()) return;
            RHIB rhib = null;
            if (_deathBoats.TryGetValue(container.net.ID, out rhib))
            {
                _deathBoats.Remove(container.net.ID);
                AlertToPlayer(player, GetMessage("KillBoat", player.UserIDString, _config.Prefix, (int)_config.TimeKillLoot));
                timer.In(_config.TimeKillLoot, () => rhib.Kill());
            }
        }
        #endregion Oxide Hooks

        #region NavMesh
        private readonly int DISTANCE_PER_SQUARE = 10;

        private readonly Dictionary<string, float> SizeOfMonuments = new Dictionary<string, float>
        {
            ["Small Harbor"] = 120f,
            ["Large Harbor"] = 145f,
            ["Oil Rig"] = 35f,
            ["Large Oil Rig"] = 55f,
            ["Lighthouse"] = 55f,
            ["Fishing Village A"] = 65f,
            ["Fishing Village B"] = 50f,
            ["Fishing Village C"] = 40f
        };

        private readonly Dictionary<Vector3, float> MonumentsAreas = new Dictionary<Vector3, float>();

        private static string GetNameMonument(MonumentInfo monument)
        {
            if (monument.name.Contains("harbor_1")) return "Small " + monument.displayPhrase.english.Replace("\n", string.Empty);
            if (monument.name.Contains("harbor_2")) return "Large " + monument.displayPhrase.english.Replace("\n", string.Empty);
            if (monument.name.Contains("fishing_village_a")) return "Fishing Village A";
            if (monument.name.Contains("fishing_village_b")) return "Fishing Village B";
            if (monument.name.Contains("fishing_village_c")) return "Fishing Village C";
            return monument.displayPhrase.english.Replace("\n", string.Empty);
        }

        private void CheckAllMonuments()
        {
            foreach (MonumentInfo monument in TerrainMeta.Path.Monuments)
            {
                string name = GetNameMonument(monument);
                if (SizeOfMonuments.ContainsKey(name)) MonumentsAreas.Add(monument.transform.position, SizeOfMonuments[name]);
            }
        }

        private bool IsValidPosToMonuments(Vector3 vector3) => !MonumentsAreas.Any(x => Vector3.Distance(vector3, x.Key) < x.Value);

        private readonly int ACCEPTED_TOPOLOGY = (int)(TerrainTopology.Enum.Ocean);
        private readonly int BLOCKED_TOPOLOGY = (int)(TerrainTopology.Enum.Decor | TerrainTopology.Enum.Building | TerrainTopology.Enum.Clutter);

        private bool IsValidPosToTopology(Vector3 vector3)
        {
            int topology = TerrainMeta.TopologyMap.GetTopology(vector3);
            if ((topology & ACCEPTED_TOPOLOGY) == 0 || (topology & BLOCKED_TOPOLOGY) != 0) return false;
            else return true;
        }

        private bool IsValidPosToHeight(Vector3 vector3)
        {
            float height = TerrainMeta.HeightMap.GetHeight(vector3);
            if (height > 0f || Math.Abs(height) < 1f) return false;
            else return true;
        }

        public class PointNavMesh { public Vector3 Position; public bool Enabled; }

        private readonly Dictionary<int, Dictionary<int, PointNavMesh>> _navMeshBoat = new Dictionary<int, Dictionary<int, PointNavMesh>>();

        private void NearPoint(Vector3 position, out Vector3 point_out, out int i_out, out int j_out)
        {
            point_out = Vector3.zero; i_out = 0; j_out = 0;
            for (int i = 0; i < _navMeshBoat.Count; i++)
            {
                for (int j = 0; j < _navMeshBoat[i].Count; j++)
                {
                    PointNavMesh pointNavMesh = _navMeshBoat[i][j];
                    if (!pointNavMesh.Enabled) continue;
                    if (point_out == Vector3.zero || Vector3.Distance(position, pointNavMesh.Position) < Vector3.Distance(position, point_out))
                    {
                        point_out = pointNavMesh.Position;
                        i_out = i;
                        j_out = j;
                    }
                }
            }
        }

        private Vector3 GetNearPoint(Vector3 position)
        {
            HashSet<Vector3> results = new HashSet<Vector3>();

            Vector3 point; int i, j;
            NearPoint(position, out point, out i, out j);

            for (int scale = 1; scale <= 5; scale++)
            {
                for (int i_mod = i - scale; i_mod <= i + scale; i_mod++)
                {
                    if (i_mod < 0 || i_mod > _ins._navMeshBoat.Count - 1) continue;
                    for (int j_mod = j - scale; j_mod <= j + scale; j_mod++)
                    {
                        if (j_mod < 0 || j_mod > _ins._navMeshBoat[i_mod].Count - 1) continue;
                        if (i_mod == i && j_mod == j) continue;
                        PointNavMesh pointNavMesh = _ins._navMeshBoat[i_mod][j_mod];
                        if (IsValidPointNavMesh(pointNavMesh)) results.Add(pointNavMesh.Position);
                    }
                }
                if (results.Count > 0) return results.Min(x => Vector3.Distance(x, position));
            }

            return Vector3.zero;
        }

        private void FindNavMesh()
        {
            int size = (int)(World.Size / 2), i = 0, j = 0;
            for (int z = -size; z <= size; z += DISTANCE_PER_SQUARE)
            {
                if (!_navMeshBoat.ContainsKey(i)) _navMeshBoat.Add(i, new Dictionary<int, PointNavMesh>());
                for (int x = -size; x <= size; x += DISTANCE_PER_SQUARE)
                {
                    Vector3 position = new Vector3(x, -0.75f, z);
                    if (!_navMeshBoat[i].ContainsKey(j)) _navMeshBoat[i].Add(j, new PointNavMesh { Position = position, Enabled = true });
                    if (!IsValidPosToTopology(position) || !IsValidPosToHeight(position) || !IsValidPosToMonuments(position)) _navMeshBoat[i][j].Enabled = false;
                    j++;
                }
                j = 0; i++;
            }
        }

        private Vector3 GetRandomNavMeshPos()
        {
            int attempts = 0;
            while (attempts < 100)
            {
                attempts++;
                int i = UnityEngine.Random.Range(0, _navMeshBoat.Count);
                int j = UnityEngine.Random.Range(0, _navMeshBoat[i].Count);
                PointNavMesh point = _navMeshBoat[i][j];
                if (!IsValidPointNavMesh(point)) continue;
                if (_controllers.Any(x => Vector3.Distance(point.Position, x.HomePosition) < x.SenseRange)) continue;
                if (Math.Abs(TerrainMeta.HeightMap.GetHeight(point.Position)) < _config.WaterDepthSpawn) continue;
                return point.Position;
            }
            PrintError("No place found on the map for the patrol boat to appear");
            return Vector3.zero;
        }

        private bool IsValidPointNavMesh(PointNavMesh pointNavMesh)
        {
            if (!pointNavMesh.Enabled) return false;
            List<BuildingBlock> list = Pool.GetList<BuildingBlock>();
            Vis.Entities<BuildingBlock>(pointNavMesh.Position, 5f, list, 1 << 21);
            int count = list.Count;
            Pool.FreeList(ref list);
            if (count > 0) return false;
            return true;
        }

        private Vector3 GetNextTargetPosition(Vector3 startPos, Vector3 targetPos)
        {
            HashSet<Vector3> results = new HashSet<Vector3>();

            Vector3 point; int i, j;
            NearPoint(startPos, out point, out i, out j);

            for (int i_mod = i - 1; i_mod <= i + 1; i_mod++)
            {
                if (i_mod < 0 || i_mod > _ins._navMeshBoat.Count - 1) continue;
                for (int j_mod = j - 1; j_mod <= j + 1; j_mod++)
                {
                    if (j_mod < 0 || j_mod > _ins._navMeshBoat[i_mod].Count - 1) continue;
                    if (i_mod == i && j_mod == j) continue;
                    PointNavMesh pointNavMesh = _ins._navMeshBoat[i_mod][j_mod];
                    if (IsValidPointNavMesh(pointNavMesh)) results.Add(pointNavMesh.Position);
                }
            }

            if (results.Count > 0) return results.Min(x => Vector3.Distance(x, targetPos));
            else return Vector3.zero;
        }

        private List<Vector3> GetPath(RHIB rhib, Vector3 targetPos)
        {
            List<Vector3> path = new List<Vector3>();

            Vector3 startNavMesh = GetNearPoint(rhib.transform.position);
            Vector3 endNavMesh = GetNearPoint(targetPos);

            if (Vector3.Distance(startNavMesh, endNavMesh) < 1f) return path;

            if (Vector3.Distance(rhib.transform.position, startNavMesh) > DISTANCE_PER_SQUARE) path.Add(startNavMesh);

            Vector3 startPos = startNavMesh;

            int attempts = 0;

            while (path.Count < 4 && attempts < 3)
            {
                attempts++;
                Vector3 next = GetNextTargetPosition(startPos, targetPos);
                if (Vector3.Distance(next, endNavMesh) < 1f) break;
                if (Vector3.Distance(rhib.transform.position, next) > DISTANCE_PER_SQUARE && !path.Contains(next))
                {
                    attempts = 0;
                    path.Add(next);
                }
                startPos = next;
            }
            path.Add(endNavMesh);

            return path;
        }
        #endregion NavMesh

        #region Spawn Loot
        private void AddToContainerPrefab(ItemContainer container, PrefabLootTableConfig lootTable)
        {
            HashSet<string> prefabsInContainer = new HashSet<string>();
            if (lootTable.UseCount)
            {
                int count = UnityEngine.Random.Range(lootTable.Min, lootTable.Max + 1);
                while (prefabsInContainer.Count < count)
                {
                    foreach (PrefabConfig prefab in lootTable.Prefabs)
                    {
                        if (prefabsInContainer.Contains(prefab.PrefabDefinition)) continue;
                        if (UnityEngine.Random.Range(0.0f, 100.0f) <= prefab.Chance)
                        {
                            if (_allLootSpawnSlots.ContainsKey(prefab.PrefabDefinition))
                            {
                                LootContainer.LootSpawnSlot[] lootSpawnSlots = _allLootSpawnSlots[prefab.PrefabDefinition];
                                foreach (LootContainer.LootSpawnSlot lootSpawnSlot in lootSpawnSlots)
                                    for (int j = 0; j < lootSpawnSlot.numberToSpawn; j++)
                                        if (UnityEngine.Random.Range(0f, 1f) <= lootSpawnSlot.probability)
                                            lootSpawnSlot.definition.SpawnIntoContainer(container);
                            }
                            else _allLootSpawn[prefab.PrefabDefinition].SpawnIntoContainer(container);
                            prefabsInContainer.Add(prefab.PrefabDefinition);
                            if (prefabsInContainer.Count == count) return;
                        }
                    }
                }
            }
            else
            {
                foreach (PrefabConfig prefab in lootTable.Prefabs)
                {
                    if (prefabsInContainer.Contains(prefab.PrefabDefinition)) continue;
                    if (UnityEngine.Random.Range(0.0f, 100.0f) <= prefab.Chance)
                    {
                        if (_allLootSpawnSlots.ContainsKey(prefab.PrefabDefinition))
                        {
                            LootContainer.LootSpawnSlot[] lootSpawnSlots = _allLootSpawnSlots[prefab.PrefabDefinition];
                            foreach (LootContainer.LootSpawnSlot lootSpawnSlot in lootSpawnSlots)
                                for (int j = 0; j < lootSpawnSlot.numberToSpawn; j++)
                                    if (UnityEngine.Random.Range(0f, 1f) <= lootSpawnSlot.probability)
                                        lootSpawnSlot.definition.SpawnIntoContainer(container);
                        }
                        else _allLootSpawn[prefab.PrefabDefinition].SpawnIntoContainer(container);
                        prefabsInContainer.Add(prefab.PrefabDefinition);
                    }
                }
            }
        }

        private void AddToContainerItem(ItemContainer container, LootTableConfig lootTable)
        {
            HashSet<int> indexMove = new HashSet<int>();
            if (lootTable.UseCount)
            {
                int count = UnityEngine.Random.Range(lootTable.Min, lootTable.Max + 1);
                while (indexMove.Count < count)
                {
                    foreach (ItemConfig item in lootTable.Items)
                    {
                        if (indexMove.Contains(lootTable.Items.IndexOf(item))) continue;
                        if (UnityEngine.Random.Range(0.0f, 100.0f) <= item.Chance)
                        {
                            Item newItem = item.IsBluePrint ? ItemManager.CreateByName("blueprintbase") : ItemManager.CreateByName(item.ShortName, UnityEngine.Random.Range(item.MinAmount, item.MaxAmount + 1), item.SkinID);
                            if (newItem == null)
                            {
                                PrintWarning($"Failed to create item! ({item.ShortName})");
                                continue;
                            }
                            if (item.IsBluePrint) newItem.blueprintTarget = ItemManager.FindItemDefinition(item.ShortName).itemid;
                            if (!string.IsNullOrEmpty(item.Name)) newItem.name = item.Name;
                            if (container.capacity < container.itemList.Count + 1) container.capacity++;
                            if (!newItem.MoveToContainer(container)) newItem.Remove();
                            else
                            {
                                indexMove.Add(lootTable.Items.IndexOf(item));
                                if (indexMove.Count == count) return;
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (ItemConfig item in lootTable.Items)
                {
                    if (indexMove.Contains(lootTable.Items.IndexOf(item))) continue;
                    if (UnityEngine.Random.Range(0.0f, 100.0f) <= item.Chance)
                    {
                        Item newItem = item.IsBluePrint ? ItemManager.CreateByName("blueprintbase") : ItemManager.CreateByName(item.ShortName, UnityEngine.Random.Range(item.MinAmount, item.MaxAmount + 1), item.SkinID);
                        if (newItem == null)
                        {
                            PrintWarning($"Failed to create item! ({item.ShortName})");
                            continue;
                        }
                        if (item.IsBluePrint) newItem.blueprintTarget = ItemManager.FindItemDefinition(item.ShortName).itemid;
                        if (!string.IsNullOrEmpty(item.Name)) newItem.name = item.Name;
                        if (container.capacity < container.itemList.Count + 1) container.capacity++;
                        if (!newItem.MoveToContainer(container)) newItem.Remove();
                        else indexMove.Add(lootTable.Items.IndexOf(item));
                    }
                }
            }
        }

        private void CheckAllLootTables()
        {
            foreach (BoatConfig config in _config.Boats)
            {
                CheckLootTable(config.Crate.OwnLootTable);
                CheckPrefabLootTable(config.Crate.PrefabLootTable);
            }

            SaveConfig();
        }

        private void CheckLootTable(LootTableConfig lootTable)
        {
            lootTable.Items = lootTable.Items.OrderBy(x => x.Chance);
            if (lootTable.Max > lootTable.Items.Count) lootTable.Max = lootTable.Items.Count;
            if (lootTable.Min > lootTable.Max) lootTable.Min = lootTable.Max;
        }

        private void CheckPrefabLootTable(PrefabLootTableConfig lootTable)
        {
            HashSet<PrefabConfig> prefabs = new HashSet<PrefabConfig>();
            foreach (PrefabConfig prefabConfig in lootTable.Prefabs)
            {
                if (prefabs.Any(x => x.PrefabDefinition == prefabConfig.PrefabDefinition)) PrintWarning($"Duplicate prefab removed from loot table! ({prefabConfig.PrefabDefinition})");
                else
                {
                    GameObject gameObject = GameManager.server.FindPrefab(prefabConfig.PrefabDefinition);
                    global::HumanNPC humanNpc = gameObject.GetComponent<global::HumanNPC>();
                    ScarecrowNPC scarecrowNPC = gameObject.GetComponent<ScarecrowNPC>();
                    LootContainer lootContainer = gameObject.GetComponent<LootContainer>();
                    if (humanNpc != null && humanNpc.LootSpawnSlots.Length != 0)
                    {
                        if (!_allLootSpawnSlots.ContainsKey(prefabConfig.PrefabDefinition)) _allLootSpawnSlots.Add(prefabConfig.PrefabDefinition, humanNpc.LootSpawnSlots);
                        prefabs.Add(prefabConfig);
                    }
                    else if (scarecrowNPC != null && scarecrowNPC.LootSpawnSlots.Length != 0)
                    {
                        if (!_allLootSpawnSlots.ContainsKey(prefabConfig.PrefabDefinition)) _allLootSpawnSlots.Add(prefabConfig.PrefabDefinition, scarecrowNPC.LootSpawnSlots);
                        prefabs.Add(prefabConfig);
                    }
                    else if (lootContainer != null && lootContainer.LootSpawnSlots.Length != 0)
                    {
                        if (!_allLootSpawnSlots.ContainsKey(prefabConfig.PrefabDefinition)) _allLootSpawnSlots.Add(prefabConfig.PrefabDefinition, lootContainer.LootSpawnSlots);
                        prefabs.Add(prefabConfig);
                    }
                    else if (lootContainer != null && lootContainer.lootDefinition != null)
                    {
                        if (!_allLootSpawn.ContainsKey(prefabConfig.PrefabDefinition)) _allLootSpawn.Add(prefabConfig.PrefabDefinition, lootContainer.lootDefinition);
                        prefabs.Add(prefabConfig);
                    }
                    else PrintWarning($"Unknown prefab removed! ({prefabConfig.PrefabDefinition})");
                }
            }
            lootTable.Prefabs = prefabs.OrderBy(x => x.Chance);
            if (lootTable.Max > lootTable.Prefabs.Count) lootTable.Max = lootTable.Prefabs.Count;
            if (lootTable.Min > lootTable.Max) lootTable.Min = lootTable.Max;
        }

        private readonly Dictionary<string, LootSpawn> _allLootSpawn = new Dictionary<string, LootSpawn>();

        private readonly Dictionary<string, LootContainer.LootSpawnSlot[]> _allLootSpawnSlots = new Dictionary<string, LootContainer.LootSpawnSlot[]>();
        #endregion Spawn Loot

        #region Alerts
        [PluginReference] private readonly Plugin GUIAnnouncements, DiscordMessages;

        private string ClearColorAndSize(string message)
        {
            message = message.Replace("</color>", string.Empty);
            message = message.Replace("</size>", string.Empty);
            while (message.Contains("<color="))
            {
                int index = message.IndexOf("<color=", StringComparison.Ordinal);
                message = message.Remove(index, message.IndexOf(">", index, StringComparison.Ordinal) - index + 1);
            }
            while (message.Contains("<size="))
            {
                int index = message.IndexOf("<size=", StringComparison.Ordinal);
                message = message.Remove(index, message.IndexOf(">", index, StringComparison.Ordinal) - index + 1);
            }
            if (!string.IsNullOrEmpty(_config.Prefix)) message = message.Replace(_config.Prefix + " ", string.Empty);
            return message;
        }

        private void AlertToPlayer(BasePlayer player, string message)
        {
            if (_config.IsChat) PrintToChat(player, message);
            if (_config.GuiAnnouncements.IsGuiAnnouncements) GUIAnnouncements?.Call("CreateAnnouncement", ClearColorAndSize(message), _config.GuiAnnouncements.BannerColor, _config.GuiAnnouncements.TextColor, player, _config.GuiAnnouncements.ApiAdjustVPosition);
            if (_config.Notify.IsNotify) player.SendConsoleCommand($"notify.show {_config.Notify.Type} {ClearColorAndSize(message)}");
        }
        #endregion Alerts
    }
}

namespace Oxide.Plugins.WaterPatrolExtensionMethods
{
    public static class ExtensionMethods
    {
        public static bool Any<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (predicate(enumerator.Current)) return true;
            return false;
        }

        public static HashSet<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            HashSet<TSource> result = new HashSet<TSource>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (predicate(enumerator.Current)) result.Add(enumerator.Current);
            return result;
        }

        public static TSource WhereMin<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate1, Func<TSource, float> predicate2)
        {
            TSource result = default(TSource);
            float resultValue = 0f;
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    TSource element = enumerator.Current;
                    if (predicate1(element))
                    {
                        float elementValue = predicate2(element);
                        if ((result != null && elementValue < resultValue) || result == null)
                        {
                            result = element;
                            resultValue = elementValue;
                        }
                    }
                }
            }
            return result;
        }

        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (predicate(enumerator.Current)) return enumerator.Current;
            return default(TSource);
        }

        public static HashSet<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> predicate)
        {
            HashSet<TResult> result = new HashSet<TResult>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) result.Add(predicate(enumerator.Current));
            return result;
        }

        public static List<TSource> ToList<TSource>(this IEnumerable<TSource> source)
        {
            List<TSource> result = new List<TSource>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) result.Add(enumerator.Current);
            return result;
        }

        public static List<TSource> OrderBy<TSource>(this IEnumerable<TSource> source, Func<TSource, float> predicate)
        {
            List<TSource> result = source.ToList();
            for (int i = 0; i < result.Count; i++)
            {
                for (int j = 0; j < result.Count - 1; j++)
                {
                    if (predicate(result[j]) > predicate(result[j + 1]))
                    {
                        TSource z = result[j];
                        result[j] = result[j + 1];
                        result[j + 1] = z;
                    }
                }
            }
            return result;
        }

        public static TSource Min<TSource>(this IEnumerable<TSource> source, Func<TSource, float> predicate)
        {
            TSource result = source.ElementAt(0);
            float resultValue = predicate(result);
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    TSource element = enumerator.Current;
                    float elementValue = predicate(element);
                    if (elementValue < resultValue)
                    {
                        result = element;
                        resultValue = elementValue;
                    }
                }
            }
            return result;
        }

        public static TSource ElementAt<TSource>(this IEnumerable<TSource> source, int index)
        {
            int movements = 0;
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (movements == index) return enumerator.Current;
                    movements++;
                }
            }
            return default(TSource);
        }

        public static bool IsPlayer(this BasePlayer player) => player != null && player.userID.IsSteamId();

        public static bool IsExists(this BaseNetworkable entity) => entity != null && !entity.IsDestroyed;

        public static void ClearItemsContainer(this ItemContainer container)
        {
            for (int i = container.itemList.Count - 1; i >= 0; i--)
            {
                Item item = container.itemList[i];
                item.RemoveFromContainer();
                item.Remove();
            }
        }
    }
}
