using Facepunch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using VLB;
using Rust;
using System.Collections;
using System.Globalization;
using Random = Oxide.Core.Random;
using Oxide.Plugins.XDChinookEventExtensionMethods;

///Скачано с дискорд сервера Rust Edit [PRO+]
///discord.gg/9vyTXsJyKR

namespace Oxide.Plugins
{
    [Info("XDChinookEvent", "discord.gg/9vyTXsJyKR", "3.2.3")]
    [Description("Авто ивент особый груз")]
    public class XDChinookEvent : RustPlugin
    {
        //Исправлены ошибки
        //Добавлена возможность менять лут в npc
        //Обновлен под последнию версию NpcSpawn
        private static XDChinookEvent _;
        [PluginReference] Plugin IQChat, NpcSpawn;

        #region Var
        ControllerChinookEvent ChinookEventConroller = null;
        private string langserver = "ru";
        private const int LAND_LAYERS = 1 << 4 | 1 << 8 | 1 << 16 | 1 << 21 | 1 << 23;
        private int maxTry = 100000;
        private HashSet<Vector3> busyPoints3D = new HashSet<Vector3>();
        private const int MaxRadius = 5;
        private Timer SpawnHouseTime;
        private Timer RemoveHouseTime;
        private const bool Ru = false;
        #endregion

        #region Lang
        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ChinookBuild"] = "<color=#E66062>You cannot build in the event area</color>",
                ["ChinookStart"] = "<color=#608FE6>Chinook Special Cargo</color> already poured on the dumping site.\nReset square <color=#6ACB52>{0}</color>",
                ["ChinookFinish"] = "<color=#608FE6>Chinook Special Cargo</color> dumping device (square <color=#6ACB52>{0}</color>),left to wait <color=#3DA9FF>{1}</color>  \nMap mark G",
                ["ChinookIsDrop"] = "<color=#608FE6>Chinook Special Cargo</color> dropped a very valuable load (square <color=#6ACB52>{0}</color>)\n Map mark G",
                ["ChinookIsDropHack"] = "<color=#FFAB3D>Special someone started to crack the load!</color>\nYou have {0} to open it\nSquare (<color=#6ACB52>{0}</color>) Map mark G",
                ["ChinookIsDropHackEnd"] = "<color=#FFAB3D>Special cargo hacked but not looted!</color>\nYou have <color=#FFAB3D>{0}</color> to patch it up",
                ["ChinookIsDropHackEndLoot"] = "Special cargo tied up by player <color=#6ACB52>{0}</color>.",
                ["Chinookcmd1"] = "You don't have enough rights!",
                ["Chinookcmd2"] = "/chinook addspawnpoint - Adds custom chinook spawn position\n/chinook call - Summon the chinook prematurely",
                ["Chinookcmd3"] = "Point added successfully",
                ["Chinookcmd4"] = "Event is already active!",
                ["Chinookcmd5"] = "You summoned a chinook",
                ["ChinookNpcSpawn"] = "You are missing the Npc Spawn plugin. Please install it to work correctly",
                ["ChinookCrateLanded"] = "The cargo has been successfully landed on the ground. You have {0} to crack it",
                ["Chinook_ENTER_PVP"] = "You <color=#ce3f27>entered</color> In the PVP zone, now other players <color=#ce3f27>can</color> do damage to you!",
                ["Chinook_EXIT_PVP"] = "You <color=#738d43>exited</color> from the PVP zone, now other players <color=#738d43>can not</color> do damage to you!",
                ["Chinook_Not_enough_players"] = "Not enough players to launch the event!",
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ChinookBuild"] = "<color=#E66062>Вы не можете строить в зоне ивента</color>",
                ["ChinookStart"] = "<color=#608FE6>Чинук с особым грузом</color> уже вылител на место сброса.\nКвадрат сброса <color=#6ACB52>{0}</color>\nБудте осторожны! Его охраняет патруль",
                ["ChinookFinish"] = "<color=#608FE6>Чинук с особым грузом</color> прилител на место сброса (квадрат <color=#6ACB52>{0}</color>), осталось подождать <color=#3DA9FF>{1}</color>  \n Метка на карте G",
                ["ChinookIsDrop"] = "<color=#608FE6>Чинук с особым грузом</color> сбросил очень ценный груз (квадрат <color=#6ACB52>{0}</color>)\n Метка на карте G",
                ["ChinookIsDropHack"] = "<color=#FFAB3D>Особый груз кто то начал взламывать!</color>\nУ тебя есть {0} до его открытие\nКвадрат (<color=#6ACB52>{1}</color>) Метка на карте G",
                ["ChinookIsDropHackEnd"] = "<color=#FFAB3D>Особый груз взломан но не залутан!</color>\nУ тебя есть <color=#FFAB3D>{0}</color> что бы залутать его",
                ["ChinookIsDropHackEndLoot"] = "Особый груз залутан игроком <color=#6ACB52>{0}</color>.",
                ["Chinookcmd1"] = "У вас недостаточно прав!",
                ["Chinookcmd2"] = "/chinook addspawnpoint - Добавляет кастомную позицию для спавна чинука\n/chinook call - Вызвать чинук преждевременно",
                ["Chinookcmd3"] = "Точка успешно добавлена",
                ["Chinookcmd4"] = "Ивент уже активен!",
                ["Chinookcmd5"] = "Вы вызвали чинук",
                ["ChinookNpcSpawn"] = "У вас отсутсвует плагин NpcSpawn. Пожалуйста установите его для коректной работы",
                ["ChinookCrateLanded"] = "Груз успешно приземлен на земелю. У вас есть {0} что бы взломать его",
                ["Chinook_ENTER_PVP"] = "Вы <color=#ce3f27>вошли</color> в PVP зону, теперь другие игроки <color=#ce3f27>могут</color> наносить вам урон!",
                ["Chinook_EXIT_PVP"] = "Вы <color=#738d43>вышли</color> из PVP зоны, теперь другие игроки <color=#738d43>не могут</color> наносить вам урон!",
                ["Chinook_Not_enough_players"] = "Недостаточно игроков для запуска ивента!",

            }, this, "ru");
        }

        #endregion

        #region Configuration
        private Configuration config;
        private class Configuration
        {
            public class PvpZone
            {
                [JsonProperty(Ru ? "Создавать PVP зону в радиусе ивента ? (Требуется TruePVE)" : "Create a PVP zone within the radius of the event? (Requires TruePVE)")]
                public bool UseZonePVP = false;
                [JsonProperty(Ru ? "Радиус PVP зоны" : "Radius of the PVP zone")]
                public int PvpZoneRadius = 20;
                [JsonProperty(Ru ? "Используете ли вы купол ?" : "Do you use a dome ?")]
                public bool useSphere = false;
                [JsonProperty(Ru ? "Прозрачность купола(чем меньше число тем более он прозрачный.Значения должно быть не более 5)" : "Transparency of the dome (the smaller the number, the more transparent it is. The values should be no more than 5)")]
                public int transperent = 3;
            }
            internal class SpawnPositionGenerateSetting
            {
                [JsonProperty(Ru ? "Разрешить спавн на дорогах ?" : "Allow spawn on the roads ?")]
                public bool spawnOnRoad = true;
                [JsonProperty(Ru ? "Разрешить спавн на реках ?" : "Allow spawn on rivers ?")]
                public bool spawnOnRiver = true;
                [JsonProperty(Ru ? "Радиус обноружения монументов" : "Radius of monument detection")]
                public float monumentFindRadius = 40f;
                [JsonProperty(Ru ? "Радиус обноружения шкафов (Building Block)" : "Detection radius of the tool cupboard (Building Block)")]
                public float buildingBlockFindRadius = 90f;
            }
            public class ChinookSetings
            {
                [JsonProperty(Ru ? "Время до начала ивента (Минимальное в секундах)" : "Time before the start of the event (Minimum in seconds)")]
                public int minSpawnIvent = 3000;
                [JsonProperty(Ru ? "Время до начала ивента (Максимальное в секундах)" : "Time before the start of the event (Maximum in seconds)")]
                public int maxSpawnIvent = 7200;
                [JsonProperty(Ru ? "Время до удаления ивента если никто не откроет ящик (Секунды)" : "Time until the event is deleted if no one opens the box (Seconds)")]
                public int timeRemoveHouse = 900;
                [JsonProperty(Ru ? "Время до удаления ивента после того как разблокируется ящик" : "The time until the event is deleted after the box is unlocked")]
                public int timeRemoveHouse2 = 300;
                [JsonProperty(Ru ? "Высота полета чинука" : "Chinook flight altitude")]
                public float flightAltitude = 240f;
                [JsonProperty(Ru ? "Радиус запрета построек во время ивента" : "Block radius of buildings during the event")]
                public int radius = 65;
                [JsonProperty(Ru ? "Минимальное колличевство игроков для запуска ивента" : "Minimum number of players to start an event")]
                public int PlayersMin = 20;
                [JsonProperty(Ru ? "Использовать кастомные позиции ?" : "Use custom positions?")]
                public bool useCustomPos = false;
                [JsonProperty(Ru ? "Кастомные позиции (/chinook addspawnpoint)" : "Custom positions (/ chinook addspawnpoint)")]
                public List<Vector3> customPos = new List<Vector3>();
            }
            public class CommandReward
            {
                [JsonProperty(Ru ? "Список команд, которые выполняются в консоли (%STEAMID% - игрок который открыл ящик)" : "List of commands that are executed in the console (%STEAMID% -  the player who looted the box)")]
                public List<string> Commands = new List<string>();
                [JsonProperty(Ru ? "Сообщения который игрок получит (Здесь можно написать о том , что получил игрок)" : "Messages that the player will receive (Here you can write about what the player received)")]
                public string MessagePlayerReward = "";
            }

            public class PresetConfig
            {
                [JsonProperty(Ru ? "Минимальное кол-во" : "Minimum")]
                public int Min;
                [JsonProperty(Ru ? "Максимальное кол-во" : "Maximum")]
                public int Max;
                [JsonProperty(Ru ? "Настройки NPC" : "NPCs setting")]
                public NpcConfig Config;
                [JsonProperty(Ru ? "Какую таблицу предметов необходимо использовать? (0 - стандартную; 1 - собственную; 2 - AlphaLoot;" : "Which loot table should the plugin use? (0 - default; 1 - own; 2 - AlphaLoot;")]
                public int TypeLootTable;
                [JsonProperty(Ru ? "Собственная таблица предметов (если тип таблицы предметов - 1)" : "Own loot table (if the loot table type is 1)")]
                public LootTableConfig OwnLootTable = new LootTableConfig();
            }

            internal class NpcConfig
            {
                [JsonProperty(Ru ? "Название" : "Name")]
                public string Name;
                [JsonProperty(Ru ? "Кол-во ХП" : "Health")]
                public float Health;
                [JsonProperty(Ru ? "Дальность патрулирования местности" : "Roam Range")]
                public float RoamRange;
                [JsonProperty(Ru ? "Дальность погони за целью" : "Chase Range")]
                public float ChaseRange;
                [JsonProperty(Ru ? "Множитель радиуса атаки" : "Attack Range Multiplier")]
                public float AttackRangeMultiplier;
                [JsonProperty(Ru ? "Радиус обнаружения цели" : "Sense Range")]
                public float SenseRange;
                [JsonProperty(Ru ? "Длительность памяти цели [sec.]" : "Target Memory Duration [sec.]")]
                public float MemoryDuration;
                [JsonProperty(Ru ? "Множитель урона" : "Scale damage")]
                public float DamageScale;
                [JsonProperty(Ru ? "Множитель разброса" : "Aim Cone Scale")]
                public float AimConeScale;
                [JsonProperty(Ru ? "Обнаруживать цель только в углу обзора NPC? [true/false]" : "Detect the target only in the NPC's viewing vision cone? [true/false]")]
                public bool CheckVisionCone;
                [JsonProperty(Ru ? "Угол обзора" : "Vision Cone")]
                public float VisionCone;
                [JsonProperty(Ru ? "Скорость" : "Speed")]
                public float Speed;
                [JsonProperty(Ru ? "Отключать эффекты рации? [true/false]" : "Disable radio effects? [true/false]")]
                public bool DisableRadio;
                [JsonProperty(Ru ? "Это стационарный NPC? [true/false]" : "Is this a stationary NPC? [true/false]")]
                public bool Stationary;
                [JsonProperty(Ru ? "Удалять труп после смерти? (рекомендуется использовать значение true для повышения производительности) [true/false]" : "Remove a corpse after death? (it is recommended to use the true value to improve performance) [true/false]")]
                public bool IsRemoveCorpse;
                [JsonProperty(Ru ? "Одежда" : "Wear items")]
                public HashSet<NpcWear> WearItems;
                [JsonProperty(Ru ? "Быстрые слоты" : "Belt items")]
                public HashSet<NpcBelt> BeltItems;
                [JsonProperty(Ru ? "Kit" : "Kit")]
                public string Kit;

                internal class NpcBelt
                {
                    [JsonProperty("ShortName")]
                    public string ShortName;
                    [JsonProperty(Ru ? "Кол-во" : "Amount")]
                    public int Amount;
                    [JsonProperty("SkinID (0 - default)")]
                    public ulong SkinID;
                    [JsonProperty(Ru ? "Модификации на оружие" : "Mods")]
                    public HashSet<string> Mods;
                }

                internal class NpcWear
                {
                    [JsonProperty("ShortName")]
                    public string ShortName;
                    [JsonProperty("SkinID (0 - default)")]
                    public ulong SkinID;
                }
            }
            public class LootTableConfig
            {
                [JsonProperty(Ru ? "Минимальное кол-во элементов" : "Minimum numbers of items")]
                public int Min;
                [JsonProperty(Ru ? "Максимальное кол-во элементов" : "Maximum numbers of items")]
                public int Max;
                [JsonProperty(Ru ? "Использовать минимальное и максимальное значение? [true/false]" : "Use minimum and maximum values? [true/false]")]
                public bool UseCount;
                [JsonProperty(Ru ? "Список предметов" : "List of items")]
                public List<ItemConfig> Items = new List<ItemConfig>();
            }

            public class ItemConfig
            {
                [JsonProperty("ShortName")]
                public string ShortName;
                [JsonProperty(Ru ? "Минимальное кол-во" : "Minimum")]
                public int MinAmount;
                [JsonProperty(Ru ? "Максимальное кол-во" : "Maximum")]
                public int MaxAmount;
                [JsonProperty(Ru ? "Шанс выпадения предмета [0.0-100.0]" : "Chance [0.0-100.0]")]
                public float Chance;
                [JsonProperty(Ru ? "Это чертеж? [true/false]" : "Is this a blueprint? [true/false]")]
                public bool IsBluePrint;
                [JsonProperty("SkinID (0 - default)")]
                public ulong SkinID;
                [JsonProperty(Ru ? "Название (empty - default)" : "Name (empty - default)")]
                public string Name;
                [JsonProperty(Ru ? "Умножать количество предмета на количество дней с начала вайпа (на 3й день - лута будет в 3 раза больше)" : "Multiply the amount of the item by the number of days since the beginning of the wipe (on the 3rd day, the loot will be 3 times more)")]
                public bool lootWipePlus = false;
            }

            internal class BoxSetting
            {
                [JsonProperty(Ru ? "Время разблокировки ящиков [sec.]" : "Time to unlock the Crates [sec.]")]
                public float UnlockTime = 900f;
                [JsonProperty(Ru ? "Время ожидания сброса" : "Reset timeout")]
                public int TimeStamp = 60;
                [JsonProperty(Ru ? "Настройка плавности/скорости спуска ящика" : "Adjusting the smoothness / speed of the drawer descent")]
                public float gravity = 0.7f;
                [JsonProperty(Ru ? "Какую таблицу лута необходимо использовать? (0 - стандартную; 1 - собственную; 2 - AlphaLoot; 3 - EcoLootUI; " : "Which loot table should the plugin use? (0 - default; 1 - own; 2 - AlphaLoot; 3 - EcoLootUI;")]
                public int TypeLootTable = 0;
                [JsonProperty(Ru ? "Собственная таблица предметов (если тип таблицы предметов - 1)" : "Own loot table (if the loot table type is 1)")]
                public LootTableConfig OwnLootTable = new LootTableConfig();
                [JsonProperty(Ru ? "Включить сигнализацию при взломе запертого язика ?" : "Turn on the alarm when breaking into a locked box ?")]
                public bool signaling = true;
            }

            public class MapMarker
            {
                [JsonProperty(Ru ? "Отметить ивент на карте G (Требуется https://umod.org/plugins/marker-manager)" : "Mark the event on the G card (Requires FREE https://umod.org/plugins/marker-manager)")]
                public bool MapUse = false;
                [JsonProperty(Ru ? "Текст для карты G" : "Text for map G")]
                public string MapTxt = "Chinook EVENT";
                [JsonProperty(Ru ? "Цвет маркера (без #)" : "Marker color (without #)")]
                public String colorMarker = "f3ecad";
                [JsonProperty(Ru ? "Цвет обводки (без #)" : "Outline color (without #)")]
                public String colorOutline = "ff3535";
            }

            [JsonProperty(Ru ? "Настройки ивента" : "Event Settings")]
            public ChinookSetings chinook = new ChinookSetings();
            [JsonProperty(Ru ? "Настройка PVP зоны (TruePve) и купола" : "Setting up a PVP zone (TruePve) and a dome")]
            public PvpZone pvpZone = new PvpZone();
            [JsonProperty(Ru ? "Настройка отображения на картах" : "Configuring display on maps")]
            public MapMarker mapMarker = new MapMarker();
            [JsonProperty(Ru ? "Настройка запертого ящика" : "Settings a locked crate")]
            public BoxSetting boxSetting = new BoxSetting();
            [JsonProperty(Ru ? "Настройка NPC" : "NPCs setting")]
            public HashSet<PresetConfig> NpcPressets = new HashSet<PresetConfig>();       
            [JsonProperty(Ru ? "Награда в виде команды, игроку который 1 открыл груз" : "Reward in the form of a team to the player who 1 opened the cargo")]
            public CommandReward commandReward = new CommandReward();
            [JsonProperty(Ru ? "Настройка подбора позиций для спавна (Для опытных пользователей)" : "Setting up the selection of positions for spawn (For experienced users)")]
            public SpawnPositionGenerateSetting spawnPositionGenerateSetting = new SpawnPositionGenerateSetting();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                    throw new Exception();
                SaveConfig();
            }
            catch
            {
                for (var i = 0; i < 3; i++)
                    PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                LoadDefaultConfig();
            }
            ValidateConfig();
            SaveConfig();
        }

        private void ValidateConfig()
        {
            if (config.NpcPressets.Count == 0)
            {
                config.NpcPressets = new HashSet<Configuration.PresetConfig>
                {
                    new Configuration.PresetConfig
                    {

                        Min = 3,
                        Max = 7,
                        Config = new Configuration.NpcConfig
                        {
                            Name = "Cobalt Defense",
                            Health = 230f,
                            RoamRange = 35f,
                            ChaseRange = 10f,
                            AttackRangeMultiplier = 3f,
                            SenseRange = 50f,
                            MemoryDuration = 40f,
                            DamageScale = 2f,
                            AimConeScale = 1f,
                            CheckVisionCone = false,
                            VisionCone = 135f,
                            Speed = 8f,
                            DisableRadio = false,
                            Stationary = false,
                            IsRemoveCorpse = false,
                            WearItems = new HashSet<Configuration.NpcConfig.NpcWear>
                            {
                                new Configuration.NpcConfig.NpcWear { ShortName = "hazmatsuit_scientist_peacekeeper", SkinID = 1121447954 }
                            },
                            BeltItems = new HashSet<Configuration.NpcConfig.NpcBelt>
                            {
                                new Configuration.NpcConfig.NpcBelt { ShortName = "pistol.semiauto", Amount = 1, SkinID = 1557105240, Mods = new HashSet<string>() },
                                new Configuration.NpcConfig.NpcBelt { ShortName = "syringe.medical", Amount = 10, SkinID = 0, Mods = new HashSet<string>() },
                            },
                            Kit = ""
                        },
                        TypeLootTable = 1,
                        OwnLootTable = new Configuration.LootTableConfig
                        {
                            Min = 1,
                            Max = 1,
                            UseCount = true,
                            Items =new List<Configuration.ItemConfig>
                            {
                                new Configuration.ItemConfig { ShortName = "scrap", MinAmount = 5, MaxAmount = 10, Chance = 50f, IsBluePrint = false, SkinID = 0, Name = "" },
                                new Configuration.ItemConfig { ShortName = "supply.signal", MinAmount = 1, MaxAmount = 1, Chance = 20f, IsBluePrint = false, SkinID = 0, Name = "" },
                                new Configuration.ItemConfig { ShortName = "syringe.medical", MinAmount = 1, MaxAmount = 2, Chance = 70.0f, IsBluePrint = false, SkinID = 0, Name = "" }
                            }
                        },
                    }
                };
            }
            if (config.boxSetting.OwnLootTable.Items.Count == 0)
            {
                config.boxSetting.OwnLootTable.Items = new List<Configuration.ItemConfig>
                {
                    new Configuration.ItemConfig
                    {
                        ShortName = "pistol.python",
                        SkinID = 0,
                        Name = "",
                        IsBluePrint = false,
                        MinAmount = 1,
                        MaxAmount = 1,
                        Chance = 60,
                    },
                    new Configuration.ItemConfig
                    {
                        ShortName = "multiplegrenadelauncher",
                        SkinID = 0,
                        Name = "",
                        IsBluePrint = false,
                        MinAmount = 1,
                        MaxAmount = 1,
                        Chance = 15,
                    },
                    new Configuration.ItemConfig
                    {
                        ShortName = "sulfur",
                        SkinID = 0,
                        Name = "",
                        IsBluePrint = false,
                        MinAmount = 500,
                        MaxAmount = 800,
                        Chance = 40,
                    },
                    new Configuration.ItemConfig
                    {
                        ShortName = "wall.external.high.ice",
                        SkinID = 0,
                        Name = "",
                        IsBluePrint = false,
                        MinAmount = 1,
                        MaxAmount = 5,
                        Chance = 75,
                    },
                };
            }
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
        }
        #endregion

        #region Classes 
        #region EventConroller
        private class ControllerChinookEvent : FacepunchBehaviour
        {
            public CH47HelicopterAIController cH47HelicopterAI;
            ForceCrate forceCrate;
            public CH47Helicopter cH47;

            public HackableLockedCrate CrateEnt;
            public Vector3 eventPos;
            public List<ScientistNPC> npcs = new List<ScientistNPC>();
            internal List<BasePlayer> playersInZone = Pool.GetList<BasePlayer>();
            private List<BaseEntity> spheres = new List<BaseEntity>();
            float flightAltitude;
            private void Awake()
            {
                gameObject.layer = (int)Rust.Layer.Reserved1;
                enabled = false;
            }
            public void InitEvent(Vector3 pos)
            {
                transform.position = pos;
                transform.rotation = new Quaternion();
                eventPos = pos;
                flightAltitude = _.config.chinook.flightAltitude;
                ChinookHelicopterCall();
            }
            private void UpdateCollider()
            {
                var sphereCollider = gameObject.GetComponent<SphereCollider>() ?? gameObject.AddComponent<SphereCollider>();
                sphereCollider.isTrigger = true;
                sphereCollider.radius = _.config.pvpZone.PvpZoneRadius;
            }
            private void OnTriggerEnter(Collider other)
            {
                BasePlayer player = other.GetComponent<BasePlayer>();
                if (player != null && player.IsNpc == false)
                {     
                    playersInZone.Add(player);
                    if (_.config.pvpZone.UseZonePVP)
                        _.SendChatPlayer(_.GetLang("Chinook_ENTER_PVP"), player);
                }
            }

            private void OnTriggerExit(Collider other)
            {
                BasePlayer player = other.GetComponent<BasePlayer>();
                if (player != null && player.IsNpc == false)
                {
                    playersInZone.Remove(player);
                    if (_.config.pvpZone.UseZonePVP)
                        _.SendChatPlayer(_.GetLang("Chinook_EXIT_PVP"), player);
                }
            }

            void CreateSphere()
            {
                for (int i = 0; i < _.config.pvpZone.transperent; i++)
                {
                    BaseEntity sphere = GameManager.server.CreateEntity("assets/prefabs/visualization/sphere.prefab", transform.position);
                    SphereEntity entity = sphere.GetComponent<SphereEntity>();
                    entity.currentRadius = _.config.pvpZone.PvpZoneRadius * 2;
                    entity.lerpSpeed = 0f;
                    sphere.enableSaving = false;
                    sphere.Spawn();
                    spheres.Add(sphere);
                }
            }
            private void ChinookHelicopterCall()
            {
                #region RandomSpawnPosition
                float x = TerrainMeta.Size.x;
                float y = flightAltitude;
                Vector3 val = Vector3Ex.Range(-1f, 1f);
                val.y = 0f;
                val.Normalize();
                val *= x * 1f;
                val.y = y;
                #endregion
                cH47 = GameManager.server.CreateEntity("assets/prefabs/npc/ch47/ch47scientists.entity.prefab", val + new Vector3(0f, flightAltitude, 0f), new Quaternion(0f, 0f, 0f, 0f), true) as CH47Helicopter;
                if (cH47 == null)
                    return;
                cH47HelicopterAI = cH47.GetComponent<CH47HelicopterAIController>();
                cH47HelicopterAI.OwnerID = 112234;
                cH47HelicopterAI.Spawn();
                cH47HelicopterAI.SetMaxHealth(900000f);
                cH47HelicopterAI.health = 900000f;
                cH47HelicopterAI.Heal(900000f);
                cH47HelicopterAI.SetLandingTarget(eventPos + new Vector3(0f, flightAltitude, 0f));
                InvokeRepeating(nameof(CheckDropped), 2f, 2f);
            }
            void CheckDropped()
            {
                if (cH47HelicopterAI == null)
                    return;
                if (Vector3.Distance(cH47HelicopterAI.transform.position.xz(), eventPos.xz()) < 10f)
                {
                    cH47HelicopterAI.SetLandingTarget(eventPos + new Vector3(0f, flightAltitude / 2, 0f));

                    _.SendChatAll("ChinookFinish", GetGridString(eventPos), FormatTime(TimeSpan.FromSeconds(_.config.boxSetting.TimeStamp), language: _.langserver));
                    CancelInvoke(nameof(CheckDropped));
                    if (_.config.pvpZone.UseZonePVP)
                        UpdateCollider();
                    if (_.config.pvpZone.useSphere)
                        CreateSphere();
                    Invoke(nameof(DroppedCrate), _.config.boxSetting.TimeStamp);
                }
            }

            void DroppedCrate()
            {
                _.SendChatAll("ChinookIsDrop", GetGridString(eventPos));

                CreateCrate();
                cH47HelicopterAI.ClearLandingTarget();
                cH47HelicopterAI.DestroyShared();
            }
            private void CreateCrate()
            {
                Quaternion rot = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                CrateEnt = GameManager.server.CreateEntity("assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab", cH47HelicopterAI.transform.position + Vector3.down * 5f, rot, true) as HackableLockedCrate;
                CrateEnt.enableSaving = false;
                CrateEnt.GetComponent<Rigidbody>().useGravity = false;
                CrateEnt.Spawn();
                CrateEnt.hackSeconds = HackableLockedCrate.requiredHackSeconds - _.config.boxSetting.UnlockTime;
                forceCrate = CrateEnt.gameObject.AddComponent<ForceCrate>();
                forceCrate.crate = CrateEnt;
                if (_.config.boxSetting.TypeLootTable == 1)
                {
                    _.NextTick(() =>
                    {
                        for (int i = CrateEnt.inventory.itemList.Count - 1; i >= 0; i--)
                        {
                            Item item = CrateEnt.inventory.itemList[i];
                            item.RemoveFromContainer();
                            item.Remove();
                        }
                        _.AddToContainerItem(CrateEnt.inventory, _.config.boxSetting.OwnLootTable);
                    });
                }
            }

            void OnDestroy()
            {
                CancelInvoke();
                if (IsAlive(cH47HelicopterAI))
                {
                    cH47HelicopterAI.ClearLandingTarget();
                    cH47HelicopterAI.DestroyShared();
                }
                if (IsAlive(CrateEnt))
                    CrateEnt.Kill();
                if (forceCrate != null)
                    Destroy(forceCrate);
                foreach (ScientistNPC npc in npcs)
                {
                    if (IsAlive(npc))
                        npc.Kill();
                }
                foreach (BaseEntity sphere in spheres)
                    if (IsAlive(sphere))
                        sphere.Kill();
                Pool.FreeList(ref playersInZone);
            }
            bool IsAlive(BaseNetworkable entity) => entity != null && !entity.IsDestroyed;
        }
        #endregion

        #region CrateForce
        private class ForceCrate : FacepunchBehaviour
        {
            public HackableLockedCrate crate;
            public bool isGrounded;
            public Vector3 groundPos;
            private BaseEntity chute;
            private float speed = 1;

            private void Awake()
            {
                crate = GetComponent<HackableLockedCrate>();
                groundPos = new Vector3(crate.transform.position.x, TerrainMeta.HeightMap.GetHeight(crate.transform.position), crate.transform.position.z);
                speed = _.config.boxSetting.gravity;
                isGrounded = false;
                InitChute();
            }

            private void Update()
            {
                if (!isGrounded)
                {
                    if (Physics.Raycast(new Ray(crate.transform.position, Vector3.down), 2f, LAND_LAYERS))
                    {
                        OnLanded();
                        return;
                    }
                    crate.transform.position = new Vector3(crate.transform.position.x, crate.transform.position.y - 0.015f * speed, crate.transform.position.z);
                }
            }

            private void OnDestroy()
            {
                if (IsAlive(chute))
                    chute.Kill();
            }

            private void OnLanded()
            {
                isGrounded = true;
                transform.position = groundPos;
                crate.GetComponent<Rigidbody>().useGravity = true;
                crate.GetComponent<Rigidbody>().drag = 1.5f;
                crate.GetComponent<Rigidbody>().isKinematic = false;
                if (IsAlive(chute))
                    chute.Kill();

                foreach (Configuration.PresetConfig preset in _.config.NpcPressets)
                    _.SpawnPreset(preset, crate.transform.position);

                _.SendChatAll("ChinookCrateLanded", FormatTime(TimeSpan.FromSeconds(_.config.chinook.timeRemoveHouse)));
                _.RemoveHouseTime = _.timer.Once(_.config.chinook.timeRemoveHouse, () =>
                {
                    _.StopEvent();
                });
                Destroy(this);
            }

            private void InitChute()
            {
                chute = GameManager.server.CreateEntity("assets/prefabs/misc/parachute/parachute.prefab", crate.transform.position);
                chute.enableSaving = false;
                chute.Spawn();

                chute.SetParent(crate, false);
                chute.transform.localPosition = Vector3.up;

                Effect.server.Run("assets/bundled/prefabs/fx/smoke_signal_full.prefab", chute, 0, Vector3.zero, Vector3.zero, null, true);
            }
            bool IsAlive(BaseNetworkable entity) => entity != null && !entity.IsDestroyed;
        }

        #endregion
        #endregion

        #region Commands
        [ChatCommand("chinook")]
        void ChinookCommands(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
            {
                SendChatPlayer(GetLang("Chinookcmd1", player.UserIDString), player);
                return;
            }
            if (args.Length == 0)
            {
                SendChatPlayer(GetLang("Chinookcmd2", player.UserIDString), player);
                return;
            }
            switch (args[0])
            {
                case "addspawnpoint":
                    {
                        config.chinook.customPos.Add(player.transform.position);
                        SendChatPlayer(GetLang("Chinookcmd3", player.UserIDString), player);
                        SaveConfig();
                        break;
                    }
                case "call":
                    {
                        if (ChinookEventConroller != null)
                        {
                            if (player != null)
                                SendChatPlayer(GetLang("Chinookcmd4", player.UserIDString), player);
                            return;
                        }
                        else
                        {
                            if (BasePlayer.activePlayerList.Count < config.chinook.PlayersMin)
                            {
                                SendChatPlayer(GetLang("Chinook_Not_enough_players", player.UserIDString), player);
                                return;
                            }
                            StartEvent();
                            SendChatPlayer(GetLang("Chinookcmd5", player.UserIDString), player);
                        }
                        break;
                    }
            }
        }
        #endregion

        #region MainMetods
        private void GenerateIvent()
        {
            if (RemoveHouseTime != null)
                RemoveHouseTime.Destroy();
            if (SpawnHouseTime != null)
                SpawnHouseTime.Destroy();
            SpawnHouseTime = timer.Once(Core.Random.Range(config.chinook.minSpawnIvent, config.chinook.maxSpawnIvent), () =>
            {
                StartEvent();
            });
        }
        void StartEvent()
        {
            if (!NpcSpawn)
            {
                PrintError(GetLang("ChinookNpcSpawn"));
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }
            if (BasePlayer.activePlayerList.Count < config.chinook.PlayersMin)
            {
                PrintWarning(GetLang("Chinook_Not_enough_players"));
                GenerateIvent();
                return;
            }
            if (RemoveHouseTime != null)
                RemoveHouseTime.Destroy();
            if (SpawnHouseTime != null)
                SpawnHouseTime.Destroy();
            Vector3 vector = config.chinook.useCustomPos ? config.chinook.customPos.GetRandom() : (Vector3)GetSpawnPoints();
            GenerateMapMarker(vector);
            SendChatAll("ChinookStart", GetGridString(vector));
            Subscribes();
            ChinookEventConroller = new GameObject().AddComponent<ControllerChinookEvent>();
            ChinookEventConroller.InitEvent(vector);     
        }
        void StopEvent(bool unload = false)
        {
            Unsubscribes();
            if (ChinookEventConroller != null)
                UnityEngine.Object.Destroy(ChinookEventConroller);
            ChinookEventConroller = null;
            if (RemoveHouseTime != null)
                RemoveHouseTime.Destroy();
            if (SpawnHouseTime != null)
                SpawnHouseTime.Destroy();
            RemoveMapMarker();
            if (!unload)
                GenerateIvent();
        }
        #endregion

        #region Hooks

        object CanCh47SpawnNpc(HackableLockedCrate crate)
        {
            if (crate == ChinookEventConroller?.CrateEnt)
            {
                return true;
            }
            else
            {
                return null;
            }
        }
        object OnBotReSpawnCrateDropped(HackableLockedCrate crate)
        {
            if (crate == ChinookEventConroller?.CrateEnt)
            {
                return true;
            }
            else
            {
                return null;
            }
        }
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (hitInfo == null)
            {
                return;
            }

            if (entity?.OwnerID == 342968945867)
            {
                hitInfo.damageTypes.ScaleAll(0);
            }
        }
        void OnEntityDeath(BasePlayer player, HitInfo info)
        {
            if (player == null || info == null)
            {
                return;
            }
            else if (ChinookEventConroller != null && ChinookEventConroller.playersInZone.Contains(player))
            {
                ChinookEventConroller.playersInZone.Remove(player);
            }
        }
        void OnEntityKill(HackableLockedCrate entity)
        {
            if (entity == ChinookEventConroller.CrateEnt)
                StopEvent();
        }
        object CanMountEntity(BasePlayer player, BaseMountable entity)
        {
            BaseEntity parrent = entity.VehicleParent();
            if (parrent != null && player.userID.IsSteamId() && parrent == ChinookEventConroller.cH47)
                return true;
            return null;
        }
        private object OnEntityTakeDamage(CH47HelicopterAIController heli, HitInfo info)
        {
            if (heli != null && heli == ChinookEventConroller.cH47HelicopterAI)
                return false;
            return null;
        }
        private object CanBuild(Planner planner, Construction prefab, Construction.Target target)
        {
            if (Vector3.Distance(planner.transform.position, ChinookEventConroller.eventPos) < config.chinook.radius)
            {
                BasePlayer player = planner?.GetOwnerPlayer();
                if (player != null)
                    SendChatPlayer(GetLang("ChinookBuild", player.UserIDString), player);
                return false;
            }
            return null;
        }
        void OnCrateHack(HackableLockedCrate crate)
        {
            if (crate == ChinookEventConroller.CrateEnt)
            {
                if (RemoveHouseTime != null)
                    RemoveHouseTime.Destroy();
                SendChatAll("ChinookIsDropHack", FormatTime(TimeSpan.FromSeconds(config.boxSetting.UnlockTime)), GetGridString(crate.transform.position));
            }
        }

        void OnCrateHackEnd(HackableLockedCrate crate)
        {
            if (crate == ChinookEventConroller.CrateEnt)
            {
                SendChatAll("ChinookIsDropHackEnd", FormatTime(TimeSpan.FromSeconds(config.chinook.timeRemoveHouse2)));
                if (RemoveHouseTime != null)
                    RemoveHouseTime.Destroy();
                RemoveHouseTime = timer.Once(config.chinook.timeRemoveHouse2, () =>
                {
                    StopEvent();
                });
            }
        }

        void CanLootEntity(BasePlayer player, StorageContainer container)
        {
            if (container is HackableLockedCrate && container == ChinookEventConroller.CrateEnt)
            {
                SendChatAll("ChinookIsDropHackEndLoot", player.displayName);
                if(config.commandReward.Commands.Count > 0)
                {
                    foreach (string command in config.commandReward.Commands)
                        Server.Command(command.Replace("%STEAMID%", $"{player.userID}"));
                    SendChatPlayer(config.commandReward.MessagePlayerReward, player);
                }
                if (RemoveHouseTime != null)
                    RemoveHouseTime.Destroy();
                RemoveHouseTime = timer.Once(300, () =>
                {
                    StopEvent();
                });
                Unsubscribe(nameof(CanLootEntity));
            }
        }
        void Unload()
        {
            if(FindPositions != null)
            {
                ServerMgr.Instance.StopCoroutine(FindPositions);
                FindPositions = null;
            }     
            StopEvent(true);
            _ = null;
        }
        void Init() => Unsubscribes();
        void OnServerInitialized()
        {
            _ = this;
            if (!NpcSpawn)
            {
                NextTick(() =>
                {
                    PrintError(GetLang("ChinookNpcSpawn"));
                    Interface.Oxide.UnloadPlugin(Name);
                });
                return;
            }
            else if (NpcSpawn.Version < new VersionNumber(2, 2, 7))
            {
                NextTick(() => {
                    PrintError("You have an old version of NpcSpawn!\nplease update the plugin to the latest version (2.0.7 or higher) - ReadMe.txt");
                    Interface.Oxide.UnloadPlugin(Name);
                });
                return;
            }
            langserver = lang.GetServerLanguage();
            LoadDefaultMessages();
            if (!config.chinook.useCustomPos)
            {
                FillPatterns();

                NextTick(() =>
                {
                    FindPositions = ServerMgr.Instance.StartCoroutine(GenerateSpawnPoints());
                });
            }   
            GenerateIvent();
        }

        #endregion

        #region SpawnPoint
        #region CheckFlat
        private List<Vector3>[] patternPositionsAboveWater = new List<Vector3>[MaxRadius];
        private List<Vector3>[] patternPositionsUnderWater = new List<Vector3>[MaxRadius];
        private const float MaxElevation = 6.0f;
        private readonly Quaternion[] directions =
        {
            Quaternion.Euler(90, 0, 0),
            Quaternion.Euler(0, 0, 90),
            Quaternion.Euler(0, 0, 180)
        };

        private void FillPatterns()
        {
            Vector3[] startPositions = { new Vector3(1, 0, 1), new Vector3(-1, 0, 1), new Vector3(-1, 0, -1), new Vector3(1, 0, -1) };

            patternPositionsAboveWater[0] = new List<Vector3> { new Vector3(0, MaxElevation, 0) };
            for (int loop = 1; loop < MaxRadius; loop++)
            {
                patternPositionsAboveWater[loop] = new List<Vector3>();

                for (int step = 0; step < loop * 2; step++)
                {
                    for (int pos = 0; pos < 4; pos++)
                    {
                        Vector3 sPos = startPositions[pos] * step;
                        for (int rot = 0; rot < 3; rot++)
                        {
                            Vector3 rPos = directions[rot] * sPos;
                            rPos.y = -MaxElevation;
                            patternPositionsAboveWater[loop].Add(rPos);
                        }
                    }
                }
            }

            for (int i = 0; i < patternPositionsAboveWater.Length; i++)
            {
                patternPositionsUnderWater[i] = new List<Vector3>();
                foreach (var vPos in patternPositionsAboveWater[i])
                {
                    var rPos = new Vector3(vPos.x, MaxElevation, vPos.z);
                    patternPositionsUnderWater[i].Add(rPos);
                }
            }
        }

        [ConsoleCommand("isflat_skykey")]
        private void CmdIsFlat(ConsoleSystem.Arg arg)
        {
            Vector3 pPos = new Vector3(arg.Player().transform.position.x, TerrainMeta.HeightMap.GetHeight(arg.Player().transform.position), arg.Player().transform.position.z);
            var b = IsFlat(ref pPos);
            arg.Player().Teleport(pPos);
        }

        public bool IsFlat(ref Vector3 position)
        {
            List<Vector3>[] AboveWater = new List<Vector3>[MaxRadius];

            Array.Copy(patternPositionsAboveWater, AboveWater, patternPositionsAboveWater.Length);

            for (int i = 0; i < AboveWater.Length; i++)
            {
                for (int j = 0; j < AboveWater[i].Count; j++)
                {
                    Vector3 pPos = AboveWater[i][j];
                    Vector3 resultAbovePos = new Vector3(pPos.x + position.x, position.y + MaxElevation, pPos.z + position.z);
                    Vector3 resultUnderPos = new Vector3(pPos.x + position.x, position.y - MaxElevation, pPos.z + position.z);

                    if (resultAbovePos.y >= TerrainMeta.HeightMap.GetHeight(resultAbovePos) && resultUnderPos.y <= TerrainMeta.HeightMap.GetHeight(resultUnderPos))
                    {
                    }
                    else
                        return false;
                }
            }

            return true;
        }
        #endregion

        #region GenerateSpawnPoint
        private Coroutine FindPositions;
        private void PosValidation(Vector3 pos)
        {
            if (!IsFlat(ref pos))
                return;
            if (TerrainMeta.WaterMap.GetHeight(pos) - TerrainMeta.HeightMap.GetHeight(pos) > -1.0f)
                return;
            if (!Is3DPointValid(pos, 1 << 8 | 1 << 16 | 1 << 18))
                return;

            if (!config.spawnPositionGenerateSetting.spawnOnRiver && ContainsTopology(TerrainTopology.Enum.River | TerrainTopology.Enum.Riverside, pos, 12.5f))
                return;
            if (!config.spawnPositionGenerateSetting.spawnOnRoad && ContainsTopology(TerrainTopology.Enum.Road | TerrainTopology.Enum.Roadside, pos, 12.5f))
                return;

            if (ContainsTopology(TerrainTopology.Enum.Monument, pos, config.spawnPositionGenerateSetting.monumentFindRadius))
                return;
            if (ContainsTopology(TerrainTopology.Enum.Building, pos, 25f))
                return;

            if (pos != Vector3.zero)
            {
                AcceptValue(ref pos);
            }
        }
        private IEnumerator GenerateSpawnPoints()
        {
            int minPos = (int)(World.Size / -2f);
            int maxPos = (int)(World.Size / 2f);
            int checks = 0;

            for (float x = minPos; x < maxPos; x += 20f)
            {
                for (float z = minPos; z < maxPos; z += 20f)
                {
                    var pos = new Vector3(x, 0f, z);

                    pos.y = GetSpawnHeight(pos);

                    PosValidation(pos);

                    if (++checks >= 75)
                    {
                        checks = 0;
                        yield return CoroutineEx.waitForSeconds(0.05f);
                    }
                }
            }
            PrintWarning($"{busyPoints3D.Count} POINTS FOUND!");
            ServerMgr.Instance.StopCoroutine(FindPositions);
            FindPositions = null;
        }
        private static float GetSpawnHeight(Vector3 target, bool flag = true)
        {
            float y = TerrainMeta.HeightMap.GetHeight(target);
            float w = TerrainMeta.WaterMap.GetHeight(target);
            float p = TerrainMeta.HighestPoint.y + 250f;
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(target.x, p, target.z), Vector3.down, out hit, target.y + p, Layers.Mask.World | Layers.Mask.Terrain, QueryTriggerInteraction.Ignore))
            {
                y = Mathf.Max(y, hit.point.y);
            }

            return flag ? Mathf.Max(y, w) : y;
        }
        private bool IsAsset(string value)
        {
            foreach (var asset in assets)
            {
                if (value.Contains(asset))
                {
                    return true;
                }
            }

            return false;
        }
        private readonly List<string> assets = new List<string>
        {
            "/props/", "/structures/", "/building/", "train_", "powerline_", "dune", "candy-cane", "assets/content/nature/", "walkway", "invisible_collider"
        };
        private float GetRockHeight(Vector3 a)
        {
            RaycastHit hit;
            if (Physics.Raycast(a + new Vector3(0f, 50f, 0f), Vector3.down, out hit, a.y + 51f, Layers.Mask.World, QueryTriggerInteraction.Ignore))
            {
                return Mathf.Abs(hit.point.y - a.y);
            }
            return 0f;
        }
        private bool Is3DPointValid(Vector3 point, int layer)
        {
            var colliders = Pool.GetList<Collider>();

            Vis.Colliders(point, 25f, colliders, layer, QueryTriggerInteraction.Collide);

            int count = colliders.Count;

            foreach (var collider in colliders)
            {
                if (collider == null || collider.transform == null)
                {
                    count--;
                    continue;
                }

                if (collider.name.Contains("SafeZone"))
                {
                    count = int.MaxValue;
                    continue;
                }

                var e = collider.ToBaseEntity();

                if (IsAsset(collider.name) && (e == null || e.name.Contains("/treessource/")))
                {
                    count = int.MaxValue;
                    break;
                }

                if (e.IsValid())
                {
                    if (e is BasePlayer)
                    {
                        var player = e as BasePlayer;

                        if (player.IsSleeping())
                        {
                            count--;
                        }
                        else if (player.IsNpc || player.IsFlying)
                        {
                            count--;
                        }
                        else
                        {
                            count = int.MaxValue;
                            break;
                        }
                    }
                    else if (e.OwnerID.IsSteamId())
                    {
                        count--;
                    }
                    else if (e.IsNpc || e is SleepingBag)
                    {
                        count--;
                    }
                    else if (e is BaseOven)
                    {
                        if (e.bounds.size.Max() > 1.6f)
                        {
                            count = int.MaxValue;
                            break;
                        }
                        else
                        {
                            count--;
                        }
                    }
                    else if (e is PlayerCorpse)
                    {
                            count--;
                    }
                    else if (e is DroppedItemContainer && e.prefabID != 545786656)
                    {
                            count--;
                    }
                    else if (e.OwnerID == 0)
                    {
                        if (e is BuildingBlock)
                        {
                            count = int.MaxValue;
                            break;
                        }
                        else
                            count--;
                    }
                    else
                    {
                        count = int.MaxValue;
                        break;
                    }
                }
                else if (collider.gameObject.layer == (int)Layer.World)
                {
                    if (collider.name.Contains("rock_") || collider.name.Contains("formation_", CompareOptions.OrdinalIgnoreCase))
                    {
                        float height = GetRockHeight(collider.transform.position);

                        if (height > 2f)
                        {
                            count = int.MaxValue;
                            break;
                        }
                        else
                            count--;
                    }
                    else if (!config.spawnPositionGenerateSetting.spawnOnRoad && collider.name.StartsWith("road_"))
                    {
                        count = int.MaxValue;
                        break;
                    }
                    else if (collider.name.StartsWith("ice_sheet"))
                    {
                        count = int.MaxValue;
                        break;
                    }
                    else
                        count--;
                }
                else if (collider.gameObject.layer == (int)Layer.Water)
                {
                    if (!config.spawnPositionGenerateSetting.spawnOnRiver && collider.name.StartsWith("River Mesh"))
                    {
                        count = int.MaxValue;
                        break;
                    }

                    count--;
                }
                else
                    count--;
            }

            Pool.FreeList(ref colliders);

            return count == 0;
        }
        private static bool ContainsTopology(TerrainTopology.Enum mask, Vector3 position, float radius)
        {
            return (TerrainMeta.TopologyMap.GetTopology(position, radius) & (int)mask) != 0;
        }

        private void AcceptValue(ref Vector3 point)
        {
            busyPoints3D.Add(point);
        }
        #endregion

        #region GetPosition

        private static bool HasBuildingPrivilege(Vector3 target, float radius)
        {
            var vector = Vector3.zero;
            var list = Pool.GetList<BuildingPrivlidge>();
            Vis.Entities(target, radius, list);
            foreach (var tc in list)
            {
                if (tc.IsValid())
                {
                    vector = tc.transform.position;
                    break;
                }
            }
            Pool.FreeList(ref list);
            return vector == Vector3.zero;
        }
        private object GetSpawnPoints()
        {
            if (busyPoints3D.ToList().Count <= 3)
            {
                if (FindPositions == null)
                {
                    PrintWarning(GetLang("XD_IVENT_CLCONTROLLER_THE_POINTS_ARE_ENDED"));
                    busyPoints3D.Clear();
                    FindPositions = ServerMgr.Instance.StartCoroutine(GenerateSpawnPoints());
                    GenerateIvent();
                }
                return Vector3.zero;
            }

            Vector3 targetPos = busyPoints3D.ToList().GetRandom();
            if (targetPos == Vector3.zero)
            {
                busyPoints3D.Remove(targetPos);
                return GetSpawnPoints();
            }

            bool valid = Is3DPointValid(targetPos, 1 << 8 | 1 << 9 | 1 << 17 | 1 << 21);
            if (!valid || !HasBuildingPrivilege(targetPos, config.spawnPositionGenerateSetting.buildingBlockFindRadius))
            {
                busyPoints3D.Remove(targetPos);
                return GetSpawnPoints();
            }
            busyPoints3D.Remove(targetPos);
            return targetPos;
        }
        #endregion
        #endregion

        #region LootNpcs
        
        object CanUILootSpawn(LootContainer container)
        {
            if (container == null || ChinookEventConroller.CrateEnt == null)
            {
                return null;
            }

            if (container == ChinookEventConroller.CrateEnt)
            {
                if (config.boxSetting.TypeLootTable == 3)
                {
                    return null;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return null;
            }
        }
        private object CanPopulateLoot(LootContainer container)
        {
            if (container == null || ChinookEventConroller.CrateEnt == null)
            {
                return null;
            }

            if (container == ChinookEventConroller.CrateEnt)
            {
                if (config.boxSetting.TypeLootTable == 2)
                {
                    return null;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return null;
            }
        }
        private object CanPopulateLoot(ScientistNPC entity, NPCPlayerCorpse corpse)
        {
            if (entity == null || corpse == null || ChinookEventConroller == null)
            {
                return null;
            }

            if (ChinookEventConroller.npcs.Contains(entity))
            {
                Configuration.PresetConfig preset =  config.NpcPressets.FirstOrDefault(x => x.Config.Name == entity.displayName);
                if (preset.TypeLootTable == 2)
                {
                    return null;
                }
                else
                {
                    return true;
                }
            }
            return null;
        }

        private void OnCorpsePopulate(ScientistNPC entity, NPCPlayerCorpse corpse)
        {
            if (entity == null || ChinookEventConroller == null)
            {
                return;
            }

            if (ChinookEventConroller.npcs.Contains(entity))
            {
                ChinookEventConroller.npcs.Remove(entity);
                Configuration.PresetConfig preset = config.NpcPressets.FirstOrDefault(x => x.Config.Name == entity.displayName);
                NextTick(() =>
                {
                    if (corpse == null)
                    {
                        return;
                    }

                    ItemContainer container = corpse.containers[0];
                    if (preset.TypeLootTable == 0)
                    {
                        for (int i = container.itemList.Count - 1; i >= 0; i--)
                        {
                            Item item = container.itemList[i];
                            if (preset.Config.WearItems.Any(x => x.ShortName == item.info.shortname))
                            {
                                item.RemoveFromContainer();
                                item.Remove();
                            }
                        }
                        return;
                    }
                    if (preset.TypeLootTable == 2)
                    {
                        if (preset.Config.IsRemoveCorpse && !corpse.IsDestroyed)
                        {
                            corpse.Kill();
                        }

                        return;
                    }
                    for (int i = container.itemList.Count - 1; i >= 0; i--)
                    {
                        Item item = container.itemList[i];
                        item.RemoveFromContainer();
                        item.Remove();
                    }
                    if (preset.TypeLootTable == 1)
                    {
                        AddToContainerItem(container, preset.OwnLootTable);
                    }

                    if (preset.Config.IsRemoveCorpse && !corpse.IsDestroyed)
                    {
                        corpse.Kill();
                    }
                });
            }
        }
        private void AddToContainerItem(ItemContainer container, Configuration.LootTableConfig lootTable)
        {
            if (lootTable.UseCount)
            {
                int count = Random.Range(lootTable.Min, lootTable.Max + 1);
                HashSet<int> indexMove = new HashSet<int>();
                while (indexMove.Count < count)
                {
                    foreach (Configuration.ItemConfig item in lootTable.Items)
                    {
                        if (indexMove.Contains(lootTable.Items.IndexOf(item)))
                        {
                            continue;
                        }

                        if (Random.Range(0.0f, 100.0f) <= item.Chance)
                        {
                            Item newItem = item.IsBluePrint ? ItemManager.CreateByName("blueprintbase") : ItemManager.CreateByName(item.ShortName, Random.Range(item.MinAmount, item.MaxAmount + 1), item.SkinID);
                            if (newItem == null)
                            {
                                PrintWarning($"Failed to create item! ({item.ShortName})");
                                continue;
                            }
                            if (item.IsBluePrint)
                            {
                                newItem.blueprintTarget = ItemManager.FindItemDefinition(item.ShortName).itemid;
                            }

                            if (!string.IsNullOrEmpty(item.Name))
                            {
                                newItem.name = item.Name;
                            }

                            if (container.capacity < container.itemList.Count + 1)
                            {
                                container.capacity++;
                            }

                            if (!newItem.MoveToContainer(container))
                            {
                                newItem.Remove();
                            }
                            else
                            {
                                indexMove.Add(lootTable.Items.IndexOf(item));
                                if (indexMove.Count == count)
                                {
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                HashSet<int> indexMove = new HashSet<int>();
                foreach (Configuration.ItemConfig item in lootTable.Items)
                {
                    if (indexMove.Contains(lootTable.Items.IndexOf(item)))
                    {
                        continue;
                    }

                    if (Random.Range(0.0f, 100.0f) <= item.Chance)
                    {
                        Item newItem = item.IsBluePrint ? ItemManager.CreateByName("blueprintbase") : ItemManager.CreateByName(item.ShortName, Random.Range(item.MinAmount, item.MaxAmount + 1), item.SkinID);
                        if (newItem == null)
                        {
                            PrintWarning($"Failed to create item! ({item.ShortName})");
                            continue;
                        }
                        if (item.IsBluePrint)
                        {
                            newItem.blueprintTarget = ItemManager.FindItemDefinition(item.ShortName).itemid;
                        }

                        if (!string.IsNullOrEmpty(item.Name))
                        {
                            newItem.name = item.Name;
                        }

                        if (container.capacity < container.itemList.Count + 1)
                        {
                            container.capacity++;
                        }

                        if (!newItem.MoveToContainer(container))
                        {
                            newItem.Remove();
                        }
                        else
                        {
                            indexMove.Add(lootTable.Items.IndexOf(item));
                        }
                    }
                }
            }
        }
        #endregion

        #region TruePVE

        private object CanEntityTakeDamage(BasePlayer victim, HitInfo hitinfo)
        {
            if (!victim.IsPlayer() || hitinfo == null || ChinookEventConroller.playersInZone == null) return null;

            if (config.pvpZone.UseZonePVP)
            {
                BasePlayer attacker = hitinfo.InitiatorPlayer;
                if (ChinookEventConroller.playersInZone.Contains(victim) && (attacker == null || ChinookEventConroller.playersInZone.Contains(attacker))) return true;
            }
            return null;
        }

        #endregion

        #region HelpMetods

        List<string> hooks = new List<string>
        {
            "CanMountEntity",
            "CanLootEntity",
            "OnCrateHackEnd",
            "OnCrateHack",
            "OnEntityTakeDamage",
            "OnEntityDeath",
            "CanBuild",
            "OnCorpsePopulate",
            "OnEntityKill",
            "CanEntityTakeDamage",
            "CanCh47SpawnNpc",
            "OnBotReSpawnCrateDropped",
            "CanUILootSpawn",
            "CanPopulateLoot", 
        };
        void Unsubscribes() {foreach (string hook in hooks) Unsubscribe(hook); }

        void Subscribes()
        {
            foreach (string hook in hooks)
            {
                if ((hook == "CanEntityTakeDamage" || hook == "OnEntityDeath") && !config.pvpZone.UseZonePVP)
                {
                    continue;
                }
                if (hook == "CanUILootSpawn" && config.boxSetting.TypeLootTable != 3)
                {
                    continue;
                }
                Subscribe(hook);
            }
        }
        public static StringBuilder sb = new StringBuilder();
        public string GetLang(string LangKey, string userID = null, params object[] args)
        {
            sb.Clear();
            if (args != null)
            {
                sb.AppendFormat(lang.GetMessage(LangKey, this, userID), args);
                return sb.ToString();
            }
            return lang.GetMessage(LangKey, this, userID);
        }

        private static string GetGridString(Vector3 position) => PhoneController.PositionToGridCoord(position);

        private void SendChatAll(string Message, params object[] args)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                SendChatPlayer(GetLang(Message, player.UserIDString, args), player);
            }
        }

        public void SendChatPlayer(string Message, BasePlayer player, ConVar.Chat.ChatChannel channel = ConVar.Chat.ChatChannel.Global)
        {
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }    
        #endregion

        #region MapMarker
        private void GenerateMapMarker(Vector3 pos)
        {
            if (config.mapMarker.MapUse)
                Interface.CallHook("API_CreateMarker", pos, "xdchinook", 0, 3f, 0.3f, config.mapMarker.MapTxt, config.mapMarker.colorMarker, config.mapMarker.colorOutline);
        }

        private void RemoveMapMarker()
        {
            if (config.mapMarker.MapUse)
                Interface.CallHook("API_RemoveMarker", "xdchinook");
        }
        #endregion

        #region TimeFormat

        public static string FormatTime(TimeSpan time, int maxSubstr = 5, string language = "ru")
        {
            string result = string.Empty;
            switch (language)
            {
                case "ru":
                    int i = 0;
                    if (time.Days != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result))
                            result += " ";

                        result += $"{Format(time.Days, "дней", "дня", "день")}";
                        i++;
                    }

                    if (time.Hours != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result))
                            result += " ";

                        result += $"{Format(time.Hours, "часов", "часа", "час")}";
                        i++;
                    }

                    if (time.Minutes != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result))
                            result += " ";

                        result += $"{Format(time.Minutes, "минут", "минуты", "минута")}";
                        i++;
                    }

                    if (time.Seconds != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result))
                            result += " ";

                        result += $"{Format(time.Seconds, "секунд", "секунды", "секунд")}";
                        i++;
                    }

                    break;
                default:
                    result = string.Format("{0}{1}{2}{3}",
                    time.Duration().Days > 0 ? $"{time.Days:0} day{(time.Days == 1 ? String.Empty : "s")}, " : string.Empty,
                    time.Duration().Hours > 0 ? $"{time.Hours:0} hour{(time.Hours == 1 ? String.Empty : "s")}, " : string.Empty,
                    time.Duration().Minutes > 0 ? $"{time.Minutes:0} minute{(time.Minutes == 1 ? String.Empty : "s")}, " : string.Empty,
                    time.Duration().Seconds > 0 ? $"{time.Seconds:0} second{(time.Seconds == 1 ? String.Empty : "s")}" : string.Empty);

                    if (result.EndsWith(", "))
                        result = result.Substring(0, result.Length - 2);

                    if (string.IsNullOrEmpty(result))
                        result = "0 seconds";
                    break;
            }
            return result;
        }
        private static string Format(int units, string form1, string form2, string form3)
        {
            var tmp = units % 10;

            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                return $"{units} {form1}";

            if (tmp >= 2 && tmp <= 4)
                return $"{units} {form2}";

            return $"{units} {form3}";
        }

        #endregion

        #region NpcSpawn
        Vector3 RandomCircle(Vector3 center, float radius, int npcCount, int i)
        {
            float ang = 360 / npcCount * i;
            Vector3 pos;
            pos.x = center.x + radius * Mathf.Sin(ang * Mathf.Deg2Rad);
            pos.z = center.z + radius * Mathf.Cos(ang * Mathf.Deg2Rad);
            pos.y = center.y;
            pos.y = GetGroundPosition(pos);

            return pos;
        }

        static float GetGroundPosition(Vector3 pos)
        {
            float y = TerrainMeta.HeightMap.GetHeight(pos);
            RaycastHit hit;

            if (Physics.Raycast(new Vector3(pos.x, pos.y + 200f, pos.z), Vector3.down, out hit, Mathf.Infinity, LayerMask.GetMask(new[] { "Terrain", "World", "Default", "Construction", "Deployed" })) && !hit.collider.name.Contains("rock_cliff"))
                return Mathf.Max(hit.point.y, y);
            return y;
        }
        private void SpawnPreset(Configuration.PresetConfig preset, Vector3 pos)
        {
            int count = Random.Range(preset.Min, preset.Max + 1);
            JObject config = GetObjectConfig(preset.Config);
            for (int i = 0; i < count; i++)
            {
                ScientistNPC npc = (ScientistNPC)NpcSpawn.Call("SpawnNpc", RandomCircle(pos, 11, count, i), config);
                ChinookEventConroller.npcs.Add(npc);
            }
        }

        private static JObject GetObjectConfig(Configuration.NpcConfig config)
        {
            HashSet<string> states = config.Stationary ? new HashSet<string> { "IdleState", "CombatStationaryState" } : new HashSet<string> { "RoamState", "ChaseState", "CombatState" };
            if (config.BeltItems.Any(x => x.ShortName == "rocket.launcher" || x.ShortName == "explosive.timed"))
            {
                states.Add("RaidState");
            }

            return new JObject
            {
                ["Name"] = config.Name,
                ["WearItems"] = new JArray { config.WearItems.Select(x => new JObject { ["ShortName"] = x.ShortName, ["SkinID"] = x.SkinID }) },
                ["BeltItems"] = new JArray { config.BeltItems.Select(x => new JObject { ["ShortName"] = x.ShortName, ["Amount"] = x.Amount, ["SkinID"] = x.SkinID, ["Mods"] = new JArray { x.Mods }, ["Ammo"] = string.Empty }) },
                ["Kit"] = config.Kit,
                ["Health"] = config.Health,
                ["RoamRange"] = config.RoamRange,
                ["ChaseRange"] = config.ChaseRange,
                ["DamageScale"] = config.DamageScale,
                ["TurretDamageScale"] = 1f,
                ["AimConeScale"] = config.AimConeScale,
                ["DisableRadio"] = config.DisableRadio,
                ["CanUseWeaponMounted"] = true,
                ["CanRunAwayWater"] = true,
                ["Speed"] = config.Speed,
                ["AreaMask"] = 1,
                ["AgentTypeID"] = -1372625422,
                ["HomePosition"] = string.Empty,
                ["States"] = new JArray { states },
                ["Sensory"] = new JObject
                {
                    ["AttackRangeMultiplier"] = config.AttackRangeMultiplier,
                    ["SenseRange"] = config.SenseRange,
                    ["MemoryDuration"] = config.MemoryDuration,
                    ["CheckVisionCone"] = config.CheckVisionCone,
                    ["VisionCone"] = config.VisionCone
                }
            };
        }
        #endregion
    }
}

namespace Oxide.Plugins.XDChinookEventExtensionMethods
{
    public static class ExtensionMethods
    {
        public static bool IsPlayer(this BasePlayer player) => player != null && player.userID.IsSteamId();

        public static bool Any<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            using (IEnumerator<TSource> enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (predicate(enumerator.Current))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            using (IEnumerator<TSource> enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (predicate(enumerator.Current))
                    {
                        return enumerator.Current;
                    }
                }
            }

            return default(TSource);
        }

        #region Select
        public static HashSet<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> predicate)
        {
            HashSet<TResult> result = new HashSet<TResult>();
            using (IEnumerator<TSource> enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    result.Add(predicate(enumerator.Current));
                }
            }

            return result;
        }

        public static List<TResult> Select<TSource, TResult>(this IList<TSource> source, Func<TSource, TResult> predicate)
        {
            List<TResult> result = new List<TResult>();
            for (int i = 0; i < source.Count; i++)
            {
                TSource element = source[i];
                result.Add(predicate(element));
            }
            return result;
        }
        #endregion Select
        public static List<TSource> ToList<TSource>(this IEnumerable<TSource> source)
        {
            List<TSource> result = new List<TSource>();
            using (IEnumerator<TSource> enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    result.Add(enumerator.Current);
                }
            }

            return result;
        }

        public static TSource Min<TSource>(this IEnumerable<TSource> source, Func<TSource, float> predicate)
        {
            TSource result = source.ElementAt(0);
            float resultValue = predicate(result);
            using (IEnumerator<TSource> enumerator = source.GetEnumerator())
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
            using (IEnumerator<TSource> enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (movements == index)
                    {
                        return enumerator.Current;
                    }

                    movements++;
                }
            }
            return default(TSource);
        }
    }
}