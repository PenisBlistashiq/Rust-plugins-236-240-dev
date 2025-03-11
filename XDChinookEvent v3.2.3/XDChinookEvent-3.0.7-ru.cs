using Facepunch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VLB;
using Rust;
using System.Collections;
using System.Globalization;

namespace Oxide.Plugins
{
    [Info("XDChinookEvent", "DezLife", "3.0.7")]
    [Description("Авто ивент особый груз")]
    public class XDChinookEvent : RustPlugin
    {
        ///Оптимизирован метод поиск позиций.
        ///Исправлена проблема с одинаковыми никами NPC
        ///
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
            public class ItemsDrop
            {
                [JsonProperty("Short name")]
                public string Shortname;
                [JsonProperty("Skin ID ")]
                public ulong SkinID;
                [JsonProperty("item name")]
                public string DisplayName;
                /*3*/[JsonProperty("Чертеж?")]
                ////[JsonProperty("BluePrint?")]
                public bool BluePrint;
                /*3*/[JsonProperty("минимальное количество")]
                ////[JsonProperty("minimal amount")]
                public int MinimalAmount;
                /*3*/[JsonProperty("максимальное количество")]
                ////[JsonProperty("maximum amount")]
                public int MaximumAmount;
                /*3*/[JsonProperty("Шанс выпадения предмета")]
                ////[JsonProperty("Item drop chance")]
                public int DropChance;
            }
            public class PvpZone
            {
                /*3*/[JsonProperty("Создавать PVP зону в радиусе ивента ? (Требуется TruePVE)")] 
                ////[JsonProperty("Create a PVP zone within the radius of the event? (Requires TruePVE)")] 
                public bool UseZonePVP = false;
                /*3*/[JsonProperty("Радиус PVP зоны")]
                ////[JsonProperty("Radius of the PVP zone")] 
                public int PvpZoneRadius = 20;
                /*3*/[JsonProperty("Используете ли вы купол ?")]
                ////[JsonProperty("Do you use a dome ?")] 
                public bool useSphere = false;
                /*3*/[JsonProperty("Прозрачность купола (чем меньше число тем более он прозрачный. Значения должно быть не более 5)")]
                ////[JsonProperty("Transparency of the dome (the smaller the number, the more transparent it is. The values should be no more than 5)")] 
                public int transperent = 3;
            }
            internal class SpawnPositionGenerateSetting
            {
                /*3*/[JsonProperty("Разрешить спавн на дорогах ?")]
                ////[JsonProperty("Allow spawn on the roads ?")]
                public bool spawnOnRoad = true;
                /*3*/[JsonProperty("Разрешить спавн на реках ?")]
                ////[JsonProperty("Allow spawn on rivers ?")]
                public bool spawnOnRiver = true;
                /*3*/[JsonProperty("Радиус обноружения монументов")]
                ////[JsonProperty("Radius of monument detection")]
                public float monumentFindRadius = 40f;
                /*3*/[JsonProperty("Радиус обноружения шкафов (Building Block)")]
                ////[JsonProperty("Detection radius of the tool cupboard (Building Block)")]
                public float buildingBlockFindRadius = 90f;
            }
            public class ChinookSetings
            {
                /*3*/[JsonProperty("Время до начала ивента (Минимальное в секундах)")]
                ////[JsonProperty("Time before the start of the event (Minimum in seconds)")]
                public int minSpawnIvent = 3000;
                /*3*/[JsonProperty("Время до начала ивента (Максимальное в секундах)")]
                ////[JsonProperty("Time before the start of the event (Maximum in seconds)")]
                public int maxSpawnIvent = 7200;
                /*3*/[JsonProperty("Время до удаления ивента если никто не откроет ящик (Секунды)")]
                ////[JsonProperty("Time until the event is deleted if no one opens the box (Seconds)")]
                public int timeRemoveHouse = 900;
                /*3*/[JsonProperty("Время до удаления ивента после того как разблокируется ящик")]
                ////[JsonProperty("The time until the event is deleted after the box is unlocked")]
                public int timeRemoveHouse2 = 300;
                /*3*/[JsonProperty("Время разблокировки ящика (Сек)")]
                ////[JsonProperty("Box unlocking time (Sec)")]
                public int unBlockTime = 900;
                /*3*/[JsonProperty("Высота полета чинука")]
                ////[JsonProperty("Chinook flight altitude")]
                public float flightAltitude = 240f;
                /*3*/[JsonProperty("Настройка плавности/скорости спуска ящика")]
                ////[JsonProperty("Adjusting the smoothness / speed of the drawer descent")]
                public float gravity = 0.7f;
                /*3*/[JsonProperty("Радиус запрета построек во время ивента")]
                ////[JsonProperty("Block radius of buildings during the event")]
                public int radius = 65;
                /*3*/[JsonProperty("Время ожидания сброса")]
                ////[JsonProperty("Reset timeout")]
                public int TimeStamp = 60;
                /*3*/[JsonProperty("Минимальное колличевство игроков для запуска ивента")]
                ////[JsonProperty("Minimum number of players to start an event")]
                public int PlayersMin = 20;
                /*3*/[JsonProperty("Максимум предметов в 1 ящике")]
                ////[JsonProperty("Maximum items in 1 box")]
                public int MaxItem = 7;
                /*3*/[JsonProperty("Использовать стандартный лут в ящике (Если false, то будет выпадать ваш лут)")]
                ////[JsonProperty("Use standard loot in the box (If false, your loot will drop out)")]
                public bool customLoot = true;
                /*3*/[JsonProperty("Использовать кастомные позиции ?")]
                ////[JsonProperty("Use custom positions?")]
                public bool useCustomPos = false;
                /*3*/[JsonProperty("Кастомные позиции (/chinook addspawnpoint)")]
                ////[JsonProperty("Custom positions (/ chinook addspawnpoint)")]
                public List<Vector3> customPos = new List<Vector3>();
            }
            public class CommandReward
            {
                /*3*/[JsonProperty("Список команд, которые выполняются в консоли (%STEAMID% - the player who looted the box)")]
                ////[JsonProperty("List of commands that are executed in the console (%STEAMID% -)")]
                public List<string> Commands =  new List<string>();
                /*3*/[JsonProperty("Сообщения который игрок получит (Здесь можно написать о том , что получил игрок)")]
                ////[JsonProperty("Messages that the player will receive (Here you can write about what the player received)")]
                public string MessagePlayerReward = "";
            }
            public class NpcChinook
            {
                /*3*/[JsonProperty("Количество нпс")]
                ////[JsonProperty("Number of npc")]
                public int npcCount = 6;
                /*3*/[JsonProperty("Настройка NPC")]
                ////[JsonProperty("NPC setup")]
                public NpcConfig npcController = new NpcConfig();
            }
            internal class NpcConfig
            {
                /*3*/[JsonProperty("Рандомные ники нпс", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                ////[JsonProperty("Random nicknames npc", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<string> nameNPC = new List<string> { "Chinook" };
                /*3*/[JsonProperty("Одежда", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                ////[JsonProperty("Wear items", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<ItemNpc> WearItems = new List<ItemNpc>
                    {
                        new ItemNpc
                        {
                            ShortName = "hazmatsuit_scientist_peacekeeper",
                            SkinID = 0,
                            Amount = 1
                        },
                    };
                /*3*/[JsonProperty("Быстрые слоты", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                ////[JsonProperty("Belt items", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<NpcBelt> BeltItems = new List<NpcBelt>
                    {
                        new NpcBelt
                        {
                            ShortName = "rifle.lr300",
                            SkinID = 1837473292,
                            Amount = 1,
                            Mods = new List<string>()
                        },
                        new NpcBelt
                        {
                            ShortName = "grenade.f1",
                            SkinID = 0,
                            Amount = 3,
                            Mods = new List<string>()
                        },
                        new NpcBelt
                        {
                            ShortName = "syringe.medical",
                            SkinID = 0,
                            Amount = 10,
                            Mods = new List<string>()
                        },
                    };
                /*3*/[JsonProperty("Кит")] 
                ////[JsonProperty("Kit")] 
                public string Kit = "";
                /*3*/[JsonProperty("Кол-во ХП")]
                ////[JsonProperty("Health")] 
                public float Health = 150f;
                /*3*/[JsonProperty("Дальность патрулирования местности")]
                ////[JsonProperty("Roam Range")] 
                public float RoamRange = 20f;
                /*3*/[JsonProperty("Дальность погони за целью")]
                ////[JsonProperty("Chase Range")] 
                public float ChaseRange = 50f;
                /*3*/[JsonProperty("Множитель радиуса атаки")]
                ////[JsonProperty("Attack Range Multiplier")] 
                public float AttackRangeMultiplier = 2f;
                /*3*/[JsonProperty("Радиус обнаружения цели")]
                ////[JsonProperty("Sense Range")] 
                public float SenseRange = 50f;
                /*3*/[JsonProperty("Время которое npc будет помнить цель (секунды)")]
                ////[JsonProperty("Target Memory Duration [sec.]")] 
                public float targetDuration = 300f;
                /*3*/[JsonProperty("Множитель урона")]
                ////[JsonProperty("Scale damage")] 
                public float DamageScale = 1f;
                /*3*/[JsonProperty("Множитель разброса")]
                ////[JsonProperty("Aim Cone Scale")] 
                public float AimConeScale = 1f;
                /*3*/[JsonProperty("Обнаруживать цель только в углу обзора NPC?")]
                ////[JsonProperty("Detect the target only in the NPC's viewing vision cone?")] 
                public bool CheckVisionCone = false;
                /*3*/[JsonProperty("Угол обзора")]
                ////[JsonProperty("Vision Cone")] 
                public float VisionCone = 135f;
                /*3*/[JsonProperty("Скорость")]
                ////[JsonProperty("Speed")] 
                public float speed = 7f;
                /*3*/[JsonProperty("Отключить радио эфект ?")]
                ////[JsonProperty("Disable radio effects?")] 
                public bool radioEffect = true;
                internal class ItemNpc
                {
                    [JsonProperty("ShortName")]
                    public string ShortName;
                    [JsonProperty("Amount")]
                    public int Amount;
                    [JsonProperty("SkinID (0 - default)")]
                    public ulong SkinID;
                }
                internal class NpcBelt
                {
                    [JsonProperty("ShortName")]
                    public string ShortName;
                    [JsonProperty("Amount")]
                    public int Amount;
                    [JsonProperty("SkinID (0 - default)")]
                    public ulong SkinID;
                    [JsonProperty("Mods")]
                    public List<string> Mods = new List<string>();
                }
            }

            public class MapMarker
            {
                /*3*/[JsonProperty("Отметить ивент на карте G (Требуется https://skyplugins.ru/resources/428/)")]
                ////[JsonProperty("Mark the event on the G card (Requires FREE https://skyplugins.ru/resources/428/)")]
                public bool MapUse = false;
                /*3*/[JsonProperty("Текст для карты G")]
                ////[JsonProperty("Text for map G")]
                public string MapTxt = "Chinook EVENT";
                /*3*/[JsonProperty("Цвет маркера (без #)")]
                ////[JsonProperty("Marker color (without #)")]
                public String colorMarker = "f3ecad";
                /*3*/[JsonProperty("Цвет обводки (без #)")]
                ////[JsonProperty("Outline color (without #)")]
                public String colorOutline = "ff3535";
            }


            /*3*/[JsonProperty("Настройки")]
            ////[JsonProperty("Settings")]
            public ChinookSetings chinook = new ChinookSetings();
            /*3*/[JsonProperty("Настройка PVP зоны (TruePve) и купола")]
            ////[JsonProperty("Setting up a PVP zone (TruePve) and a dome")]
            public PvpZone pvpZone = new PvpZone();
            /*3*/[JsonProperty("Настройка отображения на картах")]
            ////[JsonProperty("Configuring display on maps")]
            public MapMarker mapMarker = new MapMarker();
            /*3*/[JsonProperty("Настройка нпс")]
            ////[JsonProperty("Setting up npc")]
            public NpcChinook npcChinook = new NpcChinook();
            /*3*/[JsonProperty("Награда в виде команды, игроку который 1 открыл груз")]
            ////[JsonProperty("Reward in the form of a team to the player who 1 opened the cargo")]
            public CommandReward commandReward = new CommandReward();
            /*3*/[JsonProperty("Выпадаемые предметы")]
            ////[JsonProperty("Drop items")]
            public List<ItemsDrop> itemsDrops = new List<ItemsDrop>();
            /*3*/[JsonProperty("Настройка подбора позиций для спавна (Для опытных пользователей)")]
            ////[JsonProperty("Setting up the selection of positions for spawn (For experienced users)")]
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
            if (config.itemsDrops.Count == 0)
            {
                config.itemsDrops = new List<Configuration.ItemsDrop>
                {
                    new Configuration.ItemsDrop{Shortname = "halloween.surgeonsuit", SkinID = 0, DisplayName = "", BluePrint = false, MinimalAmount = 1, MaximumAmount = 1, DropChance = 70 },
                    new Configuration.ItemsDrop{Shortname = "metal.facemask", SkinID = 1886184322, DisplayName = "", BluePrint = false, MinimalAmount = 1, MaximumAmount = 1, DropChance = 20 },
                    new Configuration.ItemsDrop{Shortname = "door.double.hinged.metal", SkinID = 191100000, DisplayName = "", BluePrint = false, MinimalAmount = 1, MaximumAmount = 2, DropChance = 60 },
                    new Configuration.ItemsDrop{Shortname = "rifle.bolt", SkinID = 0, DisplayName = "", BluePrint = true, MinimalAmount = 1, MaximumAmount = 1, DropChance = 10 },
                    new Configuration.ItemsDrop{Shortname = "rifle.lr300", SkinID = 0, DisplayName = "", BluePrint = false, MinimalAmount = 1, MaximumAmount = 1, DropChance = 15 },
                    new Configuration.ItemsDrop{Shortname = "pistol.revolver", SkinID = 0, DisplayName = "", BluePrint = false, MinimalAmount = 1, MaximumAmount = 3, DropChance = 60 },
                    new Configuration.ItemsDrop{Shortname = "supply.signal", SkinID = 0, DisplayName = "", BluePrint = false, MinimalAmount = 1, MaximumAmount = 3, DropChance = 20 },
                    new Configuration.ItemsDrop{Shortname = "explosive.satchel", SkinID = 0, DisplayName = "", BluePrint = false, MinimalAmount = 1, MaximumAmount = 3, DropChance = 5 },
                    new Configuration.ItemsDrop{Shortname = "grenade.smoke", SkinID = 0, DisplayName = "", BluePrint = false, MinimalAmount = 1, MaximumAmount = 20, DropChance = 45 },
                    new Configuration.ItemsDrop{Shortname = "ammo.rifle", SkinID = 0, DisplayName = "", BluePrint = false, MinimalAmount = 50, MaximumAmount = 120, DropChance = 35 },
                    new Configuration.ItemsDrop{Shortname = "scrap", SkinID = 0, DisplayName = "", BluePrint = false, MinimalAmount = 100, MaximumAmount = 500, DropChance = 20 },
                   // new Configuration.ItemsDrop{Shortname = "giantcandycanedecor", SkinID = 3613, DisplayName = "Новый год", BluePrint = false, MinimalAmount = 1, MaximumAmount = 5, DropChance = 70 },
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

                    _.SendChatAll("ChinookFinish", GetGridString(eventPos), FormatTime(TimeSpan.FromSeconds(_.config.chinook.TimeStamp), language: _.langserver));
                    CancelInvoke(nameof(CheckDropped));
                    if (_.config.pvpZone.UseZonePVP)
                        UpdateCollider();
                    if (_.config.pvpZone.useSphere)
                        CreateSphere();
                    Invoke(nameof(DroppedCrate), _.config.chinook.TimeStamp);
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
                CrateEnt.inventory.capacity = 36;
                forceCrate = CrateEnt.gameObject.AddComponent<ForceCrate>();
                forceCrate.crate = CrateEnt;
                if (!_.config.chinook.customLoot)
                {
                    CrateEnt.inventory.itemList.Clear();
                    for (int i = 0; i < _.config.itemsDrops.Count; i++)
                    {
                        var cfg = _.config.itemsDrops[i];
                        bool goodChance = UnityEngine.Random.Range(0, 100) >= (100 - cfg.DropChance);
                        if (goodChance && CrateEnt.inventory.itemList.Count <= _.config.chinook.MaxItem)
                        {
                            if (cfg.BluePrint)
                            {
                                var bp = ItemManager.Create(ItemManager.blueprintBaseDef);
                                bp.blueprintTarget = ItemManager.FindItemDefinition(cfg.Shortname).itemid;
                                bp.MoveToContainer(CrateEnt.inventory);
                            }
                            else
                            {
                                Item GiveItem = ItemManager.CreateByName(cfg.Shortname, Oxide.Core.Random.Range(cfg.MinimalAmount, cfg.MaximumAmount), cfg.SkinID);
                                if (!string.IsNullOrEmpty(cfg.DisplayName))
                                { GiveItem.name = cfg.DisplayName; }
                                GiveItem.MoveToContainer(CrateEnt.inventory);
                            }
                        }
                    }    
                }
                CrateEnt.inventory.capacity = CrateEnt.inventory.itemList.Count;
                CrateEnt.inventory.MarkDirty();
                CrateEnt.SendNetworkUpdate();
                CrateEnt.hackSeconds = HackableLockedCrate.requiredHackSeconds - _.config.chinook.unBlockTime;
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
                speed = _.config.chinook.gravity;
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
                _.SpawnNpcs(crate.transform.position);
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
                SendChatAll("ChinookIsDropHack", FormatTime(TimeSpan.FromSeconds(config.chinook.unBlockTime)), GetGridString(crate.transform.position));
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
            else if (NpcSpawn.Version < new VersionNumber(2, 0, 7))
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

        [ConsoleCommand("isflat")]
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

        #region TruePVE
        object CanEntityTakeDamage(BasePlayer victim, HitInfo hitinfo)
        {
            if (!config.pvpZone.UseZonePVP || victim == null || hitinfo == null || ChinookEventConroller == null)
                return null;
            BasePlayer attacker = hitinfo.InitiatorPlayer;
            if (ChinookEventConroller.playersInZone.Contains(victim) && (attacker == null || (attacker != null && ChinookEventConroller.playersInZone.Contains(attacker))))
                return true;
            else
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
            "CanBuild",
            "OnEntityKill",
            "CanEntityTakeDamage",
        };
        void Unsubscribes() {foreach (string hook in hooks) Unsubscribe(hook); }

        void Subscribes()
        {
            foreach (string hook in hooks)
            {
                if (hook == "CanEntityTakeDamage" && !config.pvpZone.UseZonePVP)
                    continue;
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
        private void SpawnNpcs(Vector3 pos)
        {
            JObject configNpc = new JObject()
            {
                ["Name"] = "",
                ["WearItems"] = new JArray { config.npcChinook.npcController.WearItems.Select(x => new JObject { ["ShortName"] = x.ShortName, ["SkinID"] = x.SkinID }) },
                ["BeltItems"] = new JArray { config.npcChinook.npcController.BeltItems.Select(x => new JObject { ["ShortName"] = x.ShortName, ["Amount"] = x.Amount, ["SkinID"] = x.SkinID, ["Mods"] = new JArray { x.Mods.Select(y => y) } }) },
                ["Kit"] = config.npcChinook.npcController.Kit,
                ["Health"] = config.npcChinook.npcController.Health,
                ["RoamRange"] = config.npcChinook.npcController.RoamRange,
                ["ChaseRange"] = config.npcChinook.npcController.ChaseRange,
                ["DamageScale"] = config.npcChinook.npcController.DamageScale,
                ["AimConeScale"] = config.npcChinook.npcController.AimConeScale,
                ["DisableRadio"] = config.npcChinook.npcController.radioEffect,
                ["Stationary"] = false,
                ["CanUseWeaponMounted"] = false,
                ["CanRunAwayWater"] = true,
                ["Speed"] = config.npcChinook.npcController.speed,
                ["Sensory"] = new JObject()
                {
                    ["AttackRangeMultiplier"] = config.npcChinook.npcController.AttackRangeMultiplier,
                    ["SenseRange"] = config.npcChinook.npcController.SenseRange,
                    ["CheckVisionCone"] = config.npcChinook.npcController.CheckVisionCone,
                    ["MemoryDuration"] = config.npcChinook.npcController.targetDuration,
                    ["VisionCone"] = config.npcChinook.npcController.VisionCone,
                }
            };

            for (int i = 0; i < config.npcChinook.npcCount; i++)
            {
                configNpc["Name"] = config.npcChinook.npcController.nameNPC.GetRandom();
                ScientistNPC npc = (ScientistNPC)NpcSpawn.Call("SpawnNpc", RandomCircle(pos, 11, config.npcChinook.npcCount, i), configNpc);
                ChinookEventConroller.npcs.Add(npc);
            }
        }
        #endregion
    }
}
