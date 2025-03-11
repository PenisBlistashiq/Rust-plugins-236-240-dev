using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Facepunch;
using Network;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Rust;
using UnityEngine;
using UnityEngine.SceneManagement;
using static NPCPlayerApex;
using Time = UnityEngine.Time;

namespace Oxide.Plugins
{
    [Info("XDCobaltLaboratory", "DezLife", "1.3.0")]
    public class XDCobaltLaboratory : RustPlugin
    {
        #region Var
        [PluginReference] Plugin CopyPaste, IQChat, XDNotifications, RustMap, ImageLibrary;
        private static XDCobaltLaboratory _;

        private HashSet<Vector3> busyPoints3D = new HashSet<Vector3>();
        private List<BaseEntity> HouseCobaltLab = new List<BaseEntity>();
        private int maxTry = 250000;
        private const int MaxRadius = 5;
        public Timer SpawnHouseTime;
        public Timer RemoveHouseTime;
        public static DateTime TimeCreatedSave = SaveRestore.SaveCreatedTime.Date;
        public static DateTime RealTime = DateTime.Now.Date;
        public static int SaveCreated = RealTime.Subtract(TimeCreatedSave).Days;
        public string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary?.Call("GetImage", shortname, skin);
        public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);
        public void SendImage(BasePlayer player, string imageName, ulong imageId = 0) => ImageLibrary?.Call("SendImage", player, imageName, imageId);
        //private void AddNotify(BasePlayer player, string title, string description, string command = "", string cmdyes = "", string cmdno = "")
        //{
        //    var Setting = config.xDNotification;
        //    Interface.Oxide.CallHook("AddNotify", player, title, description, Setting.Color, Setting.AlertDelete, Setting.SoundEffect, command, cmdyes, cmdno);
        //}
        #endregion

        #region Lang
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["XD_IVENT_START"] = "Ученые разбили на этом острове свою лабораторию под названием Кобальт,скорее всего там находится ценные вещи, ведь он охраняется!\nКвадрат : {0}",       
                ["XD_IVENT_STARTUI"] = "Ученые разбили свою лабораторию под названием Кобальт!\nКвадрат : {0}",       
                ["XD_IVENT_NOPLAYER"] = "Ученые закончили свой эксперимент и успешно покинули остров без происшествий",
                ["XD_IVENT_CRATEHACK"] = "В лаборатории кобальт {0} начал взлом секретного ящика в квадрате {1}\nСоберитесь с силами и отбейте его",
                ["XD_IVENT_CRATEHACKHELP"] = "В лаборатории кобальт {0} начал взлом секретного ящика в квадрате {1}\nСоберитесь с силами и отбейте его\nНа это место уже прибыла подмога! Будте осторожней",
                ["XD_IVENT_CRATEHACKEND"] = "В лаборатории кобальт был взломан секретный ящик, ученые начинают эвакуацию с острова, у вас осталось {0} минут, чтобы забрать его!",
                ["XD_IVENT_CRATELOOTFOUND"] = " В лаборатории кобальт никто не успел залутать взломанный ящик, лаборатория была эвакуирована и постройка разрушена",
                ["XD_IVENT_CRATELOOTPLAYER"] = "{0}  успешно ограбил лабораторию кобальт и забрал ценные вещи с секретного ящика",
                ["XD_IVENT_HOUSECOBALT"] = "Cobalt laboratory",
            }, this);
        }

        #endregion

        #region api
        int ApiGetTimeToStart()
        {
            if (SpawnHouseTime.Destroyed)
                return 0;
            int time = (int)(SpawnHouseTime.Delay - DateTime.Now.Second);
            if (time <= 0)
                return 0;
            return time;
        }

        #endregion 
        
        #region Data
        public void LoadDataCopyPaste()
        {
            if (!Interface.Oxide.DataFileSystem.ExistsDatafile("copypaste/HouseCobalt"))
            {
                PrintError($"Файл постройки не найден!\nНачинаем импортировать...");
                webrequest.Enqueue("http://utilite.skyplugins.ru/XDCasinoOutPost/HouseCobalt.json", null, (i, s) =>
                {
                    {
                        if (i == 200)
                        {
                            PasteData obj = JsonConvert.DeserializeObject<PasteData>(s);
                            Interface.Oxide.DataFileSystem.WriteObject("copypaste/HouseCobalt", obj);
                            PrintWarning("Постройка успешно загружена");
                        }
                        else
                        {
                            PrintError("Ошибка при загрузке постройки!\nПробуем загрузить еще раз");
                            timer.Once(10f, () => LoadDataCopyPaste());
                            return;
                        }
                    }
                }, this, RequestMethod.GET);
            }
        }
        public class PasteData
        {
            public Dictionary<string, object> @default;
            public ICollection<Dictionary<string, object>> entities;
            public Dictionary<string, object> protocol;
        }
        #endregion

        #region Configuration
        public class LootNpcOrBox
        {
            [JsonProperty("ShortName")]
            public string Shortname;
            [JsonProperty("SkinID")]
            public ulong SkinID;
            [JsonProperty("Имя предмета")]
            public string DisplayName;
            [JsonProperty("Чертеж?")]
            public bool BluePrint;
            [JsonProperty("Минимальное количество")]
            public int MinimalAmount;
            [JsonProperty("максимальное количество")]
            public int MaximumAmount;
            [JsonProperty("Шанс выпадения предмета")]
            public int DropChance;
            [JsonProperty("Умножать этот предмета на день вайпа ?")]
            public bool wipeCheck;
        }
        private static Configuration config = new Configuration();
        private class Configuration
        {
            
            [JsonProperty("Настройка постройки для ивента (CopyPaste)")]
            public BuildingPasteSettings pasteSettings = new BuildingPasteSettings();
            [JsonProperty("Настройка запуска и остановки ивента")]
            public IventController iventController = new IventController();
            [JsonProperty("Настройка уведомлений")]
            public NotiferSettings notiferSettings = new NotiferSettings();
            [JsonProperty("Настройка радиации в зоне ивента")]
            public RadiationConroller radiationConroller = new RadiationConroller();
            [JsonProperty("Отображения ивента на картах")]
            public MapMarkers mapMarkers = new MapMarkers();
            [JsonProperty("Настройка NPC")]
            public NpcController npcController = new NpcController();
            [JsonProperty("Настройка ящика")]
            public BoxSetting boxSetting = new BoxSetting();
            //[JsonProperty(PropertyName = "Настройка XDNotifications(Если есть)")]
            //public XDNotification xDNotification = new XDNotification();

            //public class XDNotification
            //{
            //    [JsonProperty("Включить поддержку XDNotifications(UI уведомления будут заменены на уведомление с XDNotifications)")]
            //    public bool UseXDNotifications;
            //    [JsonProperty("Цвет заднего фона уведомления(HEX)")]
            //    public string Color;
            //    [JsonProperty("Через сколько удалиться уведомление")]
            //    public int AlertDelete;
            //    [JsonProperty("Звуковой эффект")]
            //    public string SoundEffect;
            //}

            internal class RadiationConroller
            {
                [JsonProperty("Включить радиацию ?")]
                public bool radUse;
                [JsonProperty("Количество радиационных частиц")]
                public int radCount;
            }
            internal class MapMarkers
            {
                [JsonProperty("Отметить ивент на карте RustMap?")]
                public bool rustMapUse;
                [JsonProperty("Иконка для карты RustMap")]
                public string rustMapIcon;
                [JsonProperty("Текст для карты RustMap")]
                public string rustMapTxt;
                [JsonProperty("Отметить ивент на карте G (Требуется https://umod.org/plugins/marker-manager)")]
                public bool MapUse;
                [JsonProperty("Текст для карты G")]
                public string MapTxt;
            }
            internal class IventController
            {
                [JsonProperty("Минимальное количество игроков для запуска ивента")]
                public int minPlayedPlayers;
                [JsonProperty("Время до начала ивента (Минимальное в секундах)")]
                public int minSpawnIvent;
                [JsonProperty("Время до начала ивента (Максимальное в секундах)")]
                public int maxSpawnIvent;
                [JsonProperty("Время до удаления ивента если никто не откроет ящик (Секунды)")]
                public int timeRemoveHouse;
                [JsonProperty("Время до удаления ивента после того как разблокируется ящик")]
                public int timeRemoveHouse2;
            }
            internal class NpcController
            {
                [JsonProperty("Спавнить NPC вокруг дома ?")]
                public bool useSpawnNPC;
                [JsonProperty("Колличевство NPC")]
                public int countSpawnNpc;
                [JsonProperty("ХП NPC")]
                public int healthNPC;
                [JsonProperty("Спавнить ли подмогу после взлома ящика ? (НПС)")]
                public bool helpBot;
                [JsonProperty("Колличевство нпс (Подмога)")]
                public int helpCount;
                [JsonProperty("Рандомные ники нпс")]
                public List<string> nameNPC = new List<string>();
                [JsonProperty("Одежда для NPC")]
                public List<ItemNpc> wearNpc = new List<ItemNpc>();
                [JsonProperty("Варианты оружия для NPC")]
                public List<ItemNpc> beltNpc = new List<ItemNpc>();
                [JsonProperty("Использовать свой лут в нпс ?")]
                public bool useCustomLoot;
                [JsonProperty("Настройка лута в NPC (Если выключенно то будет стандартный) /cl.botitems")]
                public List<LootNpcOrBox> lootNpcs = new List<LootNpcOrBox>();

                internal class ItemNpc
                {
                    [JsonProperty("ShortName")]
                    public string Shortname;
                    [JsonProperty("SkinID")]
                    public ulong SkinID;
                }
            }

            internal class BuildingPasteSettings
            {
                [JsonProperty("Настройка высоты постройки (Требуется в настройке, если вы хотите ставить свою постройку)")]
                public int heightBuilding;
                [JsonProperty("файл в папке /oxide/data/copypaste с вашей постройкой(Если не указать загрузится стандартная)")]
                public string housepath;
                [JsonProperty("радиус для обнаружения построек игроков")]
                public int radiusClear;
            }

            internal class BoxSetting
            {
                [JsonProperty("Настройка лута в ящике /cl.items")]
                public List<LootNpcOrBox> lootBoxes = new List<LootNpcOrBox>();

                [JsonProperty("Макcимальное количество предметов в ящике")]
                public int maxItemCount;
                [JsonProperty("умножать количество лута на количество дней с начала вайпа (на 3й день - лута будет в 3 раза больше)")]
                public bool lootWipePlus;
                [JsonProperty("Включить сигнализацию *?")]
                public bool signaling;
            }
            internal class NotiferSettings
            {
                [JsonProperty("ВебХук дискорда (Если не нужны уведомления в дискорд, оставьте поле пустым)")]
                public string weebHook;
                [JsonProperty("Включить UI Уведомления ?")]
                public bool useUiNotifi;
            }

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    notiferSettings =  new NotiferSettings
                    {
                        weebHook = string.Empty,
                        useUiNotifi = true
                    },
                    pasteSettings = new BuildingPasteSettings
                    {
                        housepath = "",
                        radiusClear = 25,
                        heightBuilding = 2,
                    },
                    iventController = new IventController
                    {
                        minPlayedPlayers = 0,
                        minSpawnIvent = 3000,
                        maxSpawnIvent = 7200,
                        timeRemoveHouse = 900,
                        timeRemoveHouse2 = 300
                    },
                    boxSetting = new BoxSetting
                    {
                        lootWipePlus = false,
                        maxItemCount = 10,
                        lootBoxes = new List<LootNpcOrBox>
                        {
                           new LootNpcOrBox
                           {
                               Shortname = "pistol.python",
                               SkinID = 0UL,
                               DisplayName = "",
                               BluePrint = false,
                               MinimalAmount = 1,
                               MaximumAmount = 1,
                               DropChance = 60,
                               wipeCheck = false
                           },
                           new LootNpcOrBox
                           {
                               Shortname = "multiplegrenadelauncher",
                               SkinID = 0UL,
                               DisplayName = "",
                               BluePrint = false,
                               MinimalAmount = 1,
                               MaximumAmount = 1,
                               DropChance = 15,
                               wipeCheck = false
                           },
                           new LootNpcOrBox
                           {
                               Shortname = "sulfur",
                               SkinID = 0UL,
                               DisplayName = "",
                               BluePrint = false,
                               MinimalAmount = 500,
                               MaximumAmount = 800,
                               DropChance = 40,
                               wipeCheck = true
                           },
                           new LootNpcOrBox
                           {
                               Shortname = "gunpowder",
                               SkinID = 0UL,
                               DisplayName = "",
                               BluePrint = false,
                               MinimalAmount = 300,
                               MaximumAmount = 400,
                               DropChance = 10,
                               wipeCheck = true
                           },
                           new LootNpcOrBox
                           {
                               Shortname = "door.hinged.toptier",
                               SkinID = 0UL,
                               DisplayName = "",
                               BluePrint = true,
                               MinimalAmount = 1,
                               MaximumAmount = 1,
                               DropChance = 15,
                               wipeCheck = false
                           },
                           new LootNpcOrBox
                           {
                               Shortname = "wall.external.high.ice",
                               SkinID = 0UL,
                               DisplayName = "",
                               BluePrint = false,
                               MinimalAmount = 1,
                               MaximumAmount = 5,
                               DropChance = 75,
                               wipeCheck = false
                           },
                           new LootNpcOrBox
                           {
                               Shortname = "ammo.rocket.basic",
                               SkinID = 0UL,
                               DisplayName = "",
                               BluePrint = false,
                               MinimalAmount = 1,
                               MaximumAmount = 3,
                               DropChance = 25,
                               wipeCheck = false
                           },
                           new LootNpcOrBox
                           {
                               Shortname = "ammo.grenadelauncher.smoke",
                               SkinID = 0UL,
                               DisplayName = "",
                               BluePrint = false,
                               MinimalAmount = 3,
                               MaximumAmount = 10,
                               DropChance = 70,
                               wipeCheck = false
                           },
                           new LootNpcOrBox
                           {
                               Shortname = "ammo.grenadelauncher.he",
                               SkinID = 0UL,
                               DisplayName = "",
                               BluePrint = false,
                               MinimalAmount = 2,
                               MaximumAmount = 5,
                               DropChance = 10,
                               wipeCheck = false
                           },
                           new LootNpcOrBox
                           {
                               Shortname = "metal.facemask",
                               SkinID = 0UL,
                               DisplayName = "",
                               BluePrint = true,
                               MinimalAmount = 1,
                               MaximumAmount = 1,
                               DropChance = 15,
                               wipeCheck = false
                           },
                           new LootNpcOrBox
                           {
                               Shortname = "metal.plate.torso",
                               SkinID = 0UL,
                               DisplayName = "",
                               BluePrint = true,
                               MinimalAmount = 1,
                               MaximumAmount = 1,
                               DropChance = 10,
                               wipeCheck = false
                           },
                           new LootNpcOrBox
                           {
                               Shortname = "clatter.helmet",
                               SkinID = 0UL,
                               DisplayName = "",
                               BluePrint = false,
                               MinimalAmount = 1,
                               MaximumAmount = 1,
                               DropChance = 70,
                               wipeCheck = false
                           },
                           new LootNpcOrBox
                           {
                               Shortname = "carburetor3",
                               SkinID = 0UL,
                               DisplayName = "",
                               BluePrint = false,
                               MinimalAmount = 1,
                               MaximumAmount = 1,
                               DropChance = 20,
                               wipeCheck = false
                           },
                           new LootNpcOrBox
                           {
                               Shortname = "crankshaft3",
                               SkinID = 0UL,
                               DisplayName = "",
                               BluePrint = false,
                               MinimalAmount = 1,
                               MaximumAmount = 1,
                               DropChance = 10,
                               wipeCheck = false
                           },
                           new LootNpcOrBox
                           {
                               Shortname = "techparts",
                               SkinID = 0UL,
                               DisplayName = "",
                               BluePrint = false,
                               MinimalAmount = 5,
                               MaximumAmount = 15,
                               DropChance = 35,
                               wipeCheck = false
                           },
                           new LootNpcOrBox
                           {
                               Shortname = "xmas.lightstring.advanced",
                               SkinID = 0UL,
                               DisplayName = "",
                               BluePrint = false,
                               MinimalAmount = 30,
                               MaximumAmount = 70,
                               DropChance = 45,
                               wipeCheck = false
                           },
                           new LootNpcOrBox
                           {
                               Shortname = "largemedkit",
                               SkinID = 0UL,
                               DisplayName = "",
                               BluePrint = false,
                               MinimalAmount = 3,
                               MaximumAmount = 5,
                               DropChance = 70,
                               wipeCheck = false
                           },
                           new LootNpcOrBox
                           {
                               Shortname = "largemedkit",
                               SkinID = 0UL,
                               DisplayName = "",
                               BluePrint = true,
                               MinimalAmount = 3,
                               MaximumAmount = 5,
                               DropChance = 70,
                               wipeCheck = false
                           },
                           new LootNpcOrBox
                           {
                               Shortname = "metal.fragments",
                               SkinID = 0UL,
                               DisplayName = "",
                               BluePrint = false,
                               MinimalAmount = 1000,
                               MaximumAmount = 2000,
                               DropChance = 70,
                               wipeCheck = true
                           },
                           new LootNpcOrBox
                           {
                               Shortname = "explosives",
                               SkinID = 0UL,
                               DisplayName = "",
                               BluePrint = false,
                               MinimalAmount = 10,
                               MaximumAmount = 50,
                               DropChance = 30,
                               wipeCheck = false
                           },
                           new LootNpcOrBox
                           {
                               Shortname = "autoturret",
                               SkinID = 0UL,
                               DisplayName = "",
                               BluePrint = false,
                               MinimalAmount = 1,
                               MaximumAmount = 1,
                               DropChance = 60,
                               wipeCheck = false
                           },
                           new LootNpcOrBox
                           {
                               Shortname = "explosive.timed",
                               SkinID = 0UL,
                               DisplayName = "",
                               BluePrint = false,
                               MinimalAmount = 1,
                               MaximumAmount = 1,
                               DropChance = 5,
                               wipeCheck = true
                           },
                        },
                        signaling = true
                    },
                    npcController = new NpcController
                    {
                        useSpawnNPC = true,
                        countSpawnNpc = 8,
                        healthNPC = 170,
                        helpBot = true, 
                        helpCount = 4,
                        nameNPC = new List<string> { "Cobalt guard", "Cobalt defense" },
                        wearNpc = new List<NpcController.ItemNpc>
                       {
                           new NpcController.ItemNpc
                           {
                               Shortname = "roadsign.kilt",
                               SkinID = 1121447954
                           },
                           new NpcController.ItemNpc
                           {
                               Shortname = "burlap.shirt",
                               SkinID = 2076298726
                           },
                           new NpcController.ItemNpc
                           {
                               Shortname = "shoes.boots",
                               SkinID = 0
                           },
                           new NpcController.ItemNpc
                           {
                               Shortname = "roadsign.gloves",
                               SkinID = 0
                           },
                           new NpcController.ItemNpc
                           {
                               Shortname = "burlap.trousers",
                               SkinID = 2076292007
                           },
                           new NpcController.ItemNpc
                           {
                               Shortname = "metal.facemask",
                               SkinID = 835028125
                           },
                       },
                        beltNpc = new List<NpcController.ItemNpc>
                       {
                           new NpcController.ItemNpc
                           {
                               Shortname = "rifle.lr300",
                               SkinID = 1975712725
                           },
                           new NpcController.ItemNpc
                           {
                               Shortname = "rifle.lr300",
                               SkinID = 1837473292
                           },
                           new NpcController.ItemNpc
                           {
                               Shortname = "pistol.semiauto",
                               SkinID = 1557105240
                           },
                           new NpcController.ItemNpc
                           {
                               Shortname = "rifle.semiauto",
                               SkinID = 1845735432
                           },
                           new NpcController.ItemNpc
                           {
                               Shortname = "rifle.ak",
                               SkinID = 1352726257
                           },
                       },
                        useCustomLoot = true,
                        lootNpcs = new List<LootNpcOrBox>
                        {
                            new LootNpcOrBox{Shortname = "halloween.surgeonsuit", SkinID = 0, DisplayName = "", BluePrint = false, MinimalAmount = 1, MaximumAmount = 1, DropChance = 70 },
                            new LootNpcOrBox{Shortname = "metal.facemask", SkinID = 1886184322, DisplayName = "", BluePrint = false, MinimalAmount = 1, MaximumAmount = 1, DropChance = 20 },
                            new LootNpcOrBox{Shortname = "door.double.hinged.metal", SkinID = 191100000, DisplayName = "", BluePrint = false, MinimalAmount = 1, MaximumAmount = 2, DropChance = 60 },
                            new LootNpcOrBox{Shortname = "rifle.bolt", SkinID = 0, DisplayName = "", BluePrint = true, MinimalAmount = 1, MaximumAmount = 1, DropChance = 10 },
                            new LootNpcOrBox{Shortname = "rifle.lr300", SkinID = 0, DisplayName = "", BluePrint = false, MinimalAmount = 1, MaximumAmount = 1, DropChance = 15 },
                            new LootNpcOrBox{Shortname = "pistol.revolver", SkinID = 0, DisplayName = "", BluePrint = false, MinimalAmount = 1, MaximumAmount = 3, DropChance = 60 },
                            new LootNpcOrBox{Shortname = "supply.signal", SkinID = 0, DisplayName = "", BluePrint = false, MinimalAmount = 1, MaximumAmount = 3, DropChance = 20 },
                            new LootNpcOrBox{Shortname = "explosive.satchel", SkinID = 0, DisplayName = "", BluePrint = false, MinimalAmount = 1, MaximumAmount = 3, DropChance = 5 },
                            new LootNpcOrBox{Shortname = "grenade.smoke", SkinID = 0, DisplayName = "", BluePrint = false, MinimalAmount = 1, MaximumAmount = 20, DropChance = 45 },
                            new LootNpcOrBox{Shortname = "ammo.rifle", SkinID = 0, DisplayName = "", BluePrint = false, MinimalAmount = 50, MaximumAmount = 120, DropChance = 35 },
                            new LootNpcOrBox{Shortname = "scrap", SkinID = 0, DisplayName = "", BluePrint = false, MinimalAmount = 100, MaximumAmount = 500, DropChance = 20 },
                            new LootNpcOrBox{Shortname = "giantcandycanedecor", SkinID = 0, DisplayName = "Новый год", BluePrint = false, MinimalAmount = 1, MaximumAmount = 5, DropChance = 70 },
                        }
                    },
                    radiationConroller = new RadiationConroller
                    {
                        radUse = true,
                        radCount = 20
                    },
                    mapMarkers = new MapMarkers
                    {
                        rustMapUse = true,
                        rustMapIcon = "https://i.imgur.com/bwg6de6.png",
                        rustMapTxt = "Лабаратория кобальт",
                        MapUse = false,
                        MapTxt = "Лабаратория кобальт"
                    }
                };

            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                    LoadDefaultConfig();
            }
            catch
            {
                PrintWarning("Ошибка #132" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region SpawnPoint

        #region CheckFlat
        private List<Vector3>[] patternPositionsAboveWater = new List<Vector3>[MaxRadius];
        private List<Vector3>[] patternPositionsUnderWater = new List<Vector3>[MaxRadius];

        private readonly Quaternion[] directions =
        {
            Quaternion.Euler(90, 0, 0),
            Quaternion.Euler(0, 0, 90),
            Quaternion.Euler(0, 0, 180)
        };

        private void FillPatterns()
        {
            Vector3[] startPositions = { new Vector3(1, 0, 1), new Vector3(-1, 0, 1), new Vector3(-1, 0, -1), new Vector3(1, 0, -1) };

            patternPositionsAboveWater[0] = new List<Vector3> { new Vector3(0, -1.0f, 0) };
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
                            rPos.y = -1.0f;
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
                    var rPos = new Vector3(vPos.x, 1.0f, vPos.z);
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
                    Vector3 resultAbovePos = new Vector3(pPos.x + position.x, position.y + 1.0f, pPos.z + position.z);
                    Vector3 resultUnderPos = new Vector3(pPos.x + position.x, position.y - 1.0f, pPos.z + position.z);

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

        public bool IsDistancePoint(Vector3 point)
        {
            bool result = busyPoints3D.Count(x => Vector3.Distance(point, x) < 20f) == 0;
            return result;
        }
        private void GenerateSpawnPoints()
        {
            for (int i = 0; i < 100; i++)
            {
                maxTry -= 1;
                Vector3 point3D = new Vector3();
                Vector2 point2D = new Vector3(UnityEngine.Random.Range(-TerrainMeta.Size.x / 2, TerrainMeta.Size.x / 2), UnityEngine.Random.Range(-TerrainMeta.Size.z / 2, TerrainMeta.Size.z / 2));

                point3D.x = point2D.x;
                point3D.z = point2D.y;
                point3D.y = TerrainMeta.HeightMap.GetHeight(point3D);

                if (!IsFlat(ref point3D))
                    continue;

                if (!Is3DPointValid(ref point3D))
                    continue;

                if (!IsDistancePoint(point3D))
                    continue;

                if (point3D != Vector3.zero)
                {
                    AcceptValue(ref point3D);
                }
            }
            if (maxTry > 0)
            {
                NextTick(() =>
                {
                    GenerateSpawnPoints();
                });
            }
            else
            {
                PrintWarning($"{busyPoints3D.Count} точек сгенерированно!");
                maxTry = 250000;
            }
        }
        private bool Is3DPointValid(ref Vector3 point)
        {
            if (point.y <= ConVar.Env.oceanlevel)
                return false;
            List<BuildingPrivlidge> cupboards = new List<BuildingPrivlidge>();
            Vis.Entities(point, config.pasteSettings.radiusClear, cupboards);
            if (Physics.CheckSphere(point, config.pasteSettings.radiusClear, LayerMask.GetMask("Construction", "Default", "Deployed", "World", "Trigger", "Prevent Building")) || cupboards.Count > 0)
            {
                return false;
            }
            return true;
        }
      
        private void AcceptValue(ref Vector3 point)
        {
            busyPoints3D.Add(point);  
        }
        #endregion

        #region GetPosition
        private object GetSpawnPoints()
        {
            if (busyPoints3D.ToList().Count <= 3)
            {
                PrintWarning("Все точки закончены!\n" +
                            "Начинаем генерировать новые...");
                busyPoints3D.Clear();
                GenerateSpawnPoints();
                GenerateIvent();
                return Vector3.zero;
            }

            Vector3 targetPos = busyPoints3D.ToList().GetRandom();
            if (targetPos == Vector3.zero)
            {
                busyPoints3D.Remove(targetPos);
                return GetSpawnPoints();
            }

            bool valid = Is3DPointValid(ref targetPos);

            if (!valid)
            {
                busyPoints3D.Remove(targetPos);
                return GetSpawnPoints();
            }
            busyPoints3D.Remove(targetPos);
            return targetPos;
        }
        #endregion

        #endregion

        #region Hooks
        void OnEntityMounted(BaseMountable entity, BasePlayer player)
        {

            if (HouseCobaltLab.Contains(entity?.GetParentEntity()))
            {
                HouseCobaltLab.Remove(entity.GetParentEntity());
            }
        }

        void Unload()
        {
            foreach (BaseEntity iventEnt in HouseCobaltLab)
            {
                if (!iventEnt.IsDestroyed)
                    iventEnt?.Kill();
            }
            HouseCobaltLab.Clear();
            if (SpawnHouseTime != null)
                SpawnHouseTime.Destroy();
            if (RemoveHouseTime != null)
                RemoveHouseTime.Destroy();
            DestroyAll<NpcZones>();
            DestroyAll<NPCMonitor>();
            RemoveMapMarker();
            Cui.DestroyAllPlayer();
        }
        private void OnServerInitialized()
        {
            if (!CopyPaste)
            {
                PrintError("Проверьте установлен ли у вас плагин 'CopyPaste'");
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }
            else if (CopyPaste.Version < new VersionNumber(4, 1, 27))
            {
                PrintError("У вас старая версия CopyPaste!\nПожалуйста обновите плагин до последней версии (4.1.27 или выше) - https://umod.org/plugins/copy-paste");
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }
            AddImage("https://cdn-icons.flaticon.com/png/512/3305/premium/3305168.png?token=exp=1652409621~hmac=2e45ec6818fa6a44a0dc785d7a200e2d", "pngForUi");
            _ = this;
            FillPatterns();
            NextTick(() =>
            {
                GenerateSpawnPoints();
            });
            GenerateIvent();
            if (string.IsNullOrEmpty(config.pasteSettings.housepath))
                LoadDataCopyPaste();
        }
        private void CanHackCrate(BasePlayer player, HackableLockedCrate crate)
        {
            if (crate.OwnerID == 3566257)
            {
                if (RemoveHouseTime != null)
                    RemoveHouseTime.Destroy();
                if (config.boxSetting.signaling)
                {
                    var Alarm = GameManager.server.CreateEntity("assets/prefabs/deployable/playerioents/alarms/audioalarm.prefab", crate.transform.position, default(Quaternion), true);
                    Alarm.Spawn();
                    Alarm.SetFlag(BaseEntity.Flags.Reserved8, true);
                    Alarm.gameObject.Identity();
                    Alarm.SetParent(crate);

                    var Light = GameManager.server.CreateEntity("assets/prefabs/deployable/playerioents/lights/sirenlight/electric.sirenlight.deployed.prefab", crate.transform.position, Quaternion.identity, false);
                    Light.enableSaving = true;
                    Light.Spawn();
                    Light.SetParent(crate);
                    Light.transform.localPosition = new Vector3(0.4f, 1.45f, -0.3f);
                    Light.transform.hasChanged = true;
                    Light.SendNetworkUpdate();

                    Light.SetFlag(BaseEntity.Flags.Reserved8, true);
                }
                SendChatAll(config.npcController.helpBot ? "XD_IVENT_CRATEHACKHELP" : "XD_IVENT_CRATEHACK", player.displayName, GetGridString(crate.transform.position));
                if (config.npcController.helpBot)
                    SpawnBots(crate, true);
            }
        }
        void OnCrateHackEnd(HackableLockedCrate crate)
        {
            if (crate.OwnerID == 3566257)
            {
                if (RemoveHouseTime != null)
                    RemoveHouseTime.Destroy();
                SendChatAll("XD_IVENT_CRATEHACKEND", (config.iventController.timeRemoveHouse2 / 60));
                RemoveHouseTime = timer.Once(config.iventController.timeRemoveHouse2, () =>
                {
                    SendChatAll("XD_IVENT_CRATELOOTFOUND");
                    StopIvent();
                });
            }
        }
        void CanLootEntity(BasePlayer player, StorageContainer container)
        {
            if (container is HackableLockedCrate && container.OwnerID == 3566257)
            {
                SendChatAll("XD_IVENT_CRATELOOTPLAYER", player.displayName);
                if (RemoveHouseTime != null)
                    RemoveHouseTime.Destroy();
                RemoveHouseTime = timer.Once(300, () =>
                {
                    StopIvent();
                });
                container.OwnerID = 123425345634634;
            }
        }
        void OnCorpsePopulate(Scientist npc, NPCPlayerCorpse corpse)
        {
            if (npc?.GetComponent<NPCMonitor>() != null && corpse != null)
            {
                if (config.npcController.useCustomLoot && config.npcController.lootNpcs?.Count > 0)
                {
                    corpse.containers[0].itemList.Clear();
                    for (int i = 0; i < config.npcController.lootNpcs.Count; i++)
                    {
                        var main = config.npcController.lootNpcs[i];
                        if (corpse.containers[0].IsFull())
                            break;
                        bool goodChance = random.Next(0, 100) >= (100 - main.DropChance);
                        if (goodChance)
                        {
                            if (main.BluePrint)
                            {
                                Item item = ItemManager.Create(ResearchTable.GetBlueprintTemplate(), 1, 0UL);
                                item.blueprintTarget = ItemManager.FindItemDefinition(main.Shortname).itemid;
                                if (!item.MoveToContainer(corpse.containers[0]))
                                    item.Remove();
                            }
                            else
                            {
                                Item item = ItemManager.CreateByName(main.Shortname, random.Next(main.MinimalAmount, main.MaximumAmount), main.SkinID);
                                if (!string.IsNullOrEmpty(main.DisplayName))
                                {
                                    item.name = main.DisplayName;
                                }
                                if (!item.MoveToContainer(corpse.containers[0]))
                                    item.Remove();
                            }
                        }
                    }
                    corpse.containers[0].capacity = corpse.containers[0].itemList.Count;
                    corpse.containers[1].capacity = 0;
                    corpse.containers[2].capacity = 0;
                    corpse.containers[0].MarkDirty();
                    corpse.SendNetworkUpdate();
                }
            }
        }
        void OnEntityTakeDamage(BaseCombatEntity victim, HitInfo info)
        {
            try
            {
                if (victim?.OwnerID == 342968945867)
                {
                    info.damageTypes.ScaleAll(0);
                }
            }
            catch (NullReferenceException) { }
        }
        #endregion

        #region MetodsPasteBuild
        void GenerateBuilding()
        {
            string[] options = { "stability", "true", "deployables", "true", "autoheight", "false", "entityowner", "false" };
            Vector3 resultVector = (Vector3)GetSpawnPoints();
            if (resultVector == null || resultVector == Vector3.zero)
                return;

            var success = CopyPaste.Call("TryPasteFromVector3", new Vector3(resultVector.x, resultVector.y + config.pasteSettings.heightBuilding, resultVector.z), 0f, !string.IsNullOrWhiteSpace(config.pasteSettings.housepath) ? config.pasteSettings.housepath : "HouseCobalt", options);

            if (success is string)
            {
                PrintWarning("Ошибка #1 \nПлагин не будет работать, Обратитесь к разработчику");
                GenerateIvent();
                return;
            }
        }
       
        void OnPasteFinished(List<BaseEntity> pastedEntities, string fileName)
        {
            if (fileName != "HouseCobalt" && fileName != config.pasteSettings.housepath)
                return;

            HouseCobaltLab = pastedEntities;
            BaseEntity box = null;
            List<CCTV_RC> cam = new List<CCTV_RC>();
            ComputerStation comp = null;
            foreach (BaseEntity ent in pastedEntities)
            {
                if (ent is MiniCopter)
                {
                    MiniCopter copter = (ent as MiniCopter);
                    copter.fuelSystem.AddStartingFuel(50);
                    copter.transform.position = new Vector3(copter.transform.position.x, copter.transform.position.y + 3f, copter.transform.position.z);
                    continue;
                }
                ent.OwnerID = 342968945867;
                if (ent is Signage)
                {
                    var ents = ent as Signage;
                    if (ents == null)
                        continue;
                    ents?.SetFlag(BaseEntity.Flags.Locked, true);
                    ents.SendNetworkUpdate(global::BasePlayer.NetworkQueue.Update);
                }
                if (ent is Workbench || ent is ResearchTable || ent is MixingTable || ent is BaseArcadeMachine
                  || ent is IOEntity || ent is ComputerStation || ent is CCTV_RC)
                {
                    if(ent is IOEntity)
                        ent.SetFlag(BaseEntity.Flags.Reserved8, true);
                    if (ent is ComputerStation)
                        comp = ent as ComputerStation;
                    if (ent is CCTV_RC)
                        cam.Add(ent as CCTV_RC);           
                    var ents = ent as BaseCombatEntity;
                    if (ents == null)
                        continue;
                    ents.pickup.enabled = false;
                    continue;
                }
                if (ent is VendingMachine)
                {
                    var ents = ent as VendingMachine;
                    if (ents == null)
                        continue;
                    ents.SetFlag(BaseEntity.Flags.Reserved4, false);
                    ents.UpdateMapMarker();
                }
                if (ent is FogMachine)
                {
                    var ents = ent as FogMachine;
                    if (ents == null)
                        continue;
                    ents.SetFlag(BaseEntity.Flags.Reserved8, true);
                    ents.SetFlag(BaseEntity.Flags.Reserved7, false);
                    ents.SetFlag(BaseEntity.Flags.Reserved6, false);
                }
                if (ent.name == "assets/prefabs/deployable/large wood storage/box.wooden.large.prefab")
                {
                    box = CrateHackableLocked(ent);
                    NextTick(() => { HouseCobaltLab.Remove(ent); ent.Kill(); HouseCobaltLab.Add(box); });
                    continue;
                }
                if (ent is Door)
                {
                    var ents = ent as Door;
                    if (ents == null)
                        continue;
                    ents.pickup.enabled = false;
                    ents.canTakeLock = false;
                    ents.canTakeCloser = false;
                    continue;
                }
                if (ent is ElectricGenerator)
                {
                    (ent as ElectricGenerator).electricAmount = 400;
                }
                if (ent as BuildingBlock)
                {
                    var build = ent as BuildingBlock;
                    build?.SetFlag(BaseEntity.Flags.Reserved1, false);
                    build?.SetFlag(BaseEntity.Flags.Reserved2, false);
                }
                ent?.SetFlag(BaseEntity.Flags.Busy, true);
                ent?.SetFlag(BaseEntity.Flags.Locked, true);
            }
            if (comp != null && cam.Count > 0)
            {
                foreach (CCTV_RC sd in cam)
                    comp.controlBookmarks.Add(sd.GetIdentifier(), sd.net.ID);
            }
            if (box == null)
            {
                PrintError("Ошибка #3, В постройке не найден ящик");
                StopIvent();
                GenerateIvent();
                return;
            }
            NpcZones Zone = new GameObject().AddComponent<NpcZones>();
            Zone.Activate(box, 15, config.radiationConroller.radUse);
            if (config.npcController.useSpawnNPC)
                SpawnBots(box);
            GenerateMapMarker(box.transform.position);
            SendChatAll("XD_IVENT_START", GetGridString(box.transform.position));
            if(config.notiferSettings.useUiNotifi)
                Cui.CreateUIAllPlayer(json, box.transform.position);
            RemoveHouseTime = timer.Once(config.iventController.timeRemoveHouse, () =>
            {
                SendChatAll("XD_IVENT_NOPLAYER");
                StopIvent();
            });
        }

        private void GenerateMapMarker(Vector3 pos)
        {
            if(config.mapMarkers.rustMapUse)
                RustMap?.Call("ApiAddPointUrl", config.mapMarkers.rustMapIcon, Title, pos, config.mapMarkers.rustMapTxt);

            if(config.mapMarkers.MapUse)
                Interface.CallHook("API_CreateMarker", pos, "xdcobaltlab", 0, 3f, 0.3f, config.mapMarkers.MapTxt, "f3ecad", "ff3535");
        }
        private void RemoveMapMarker()
        {
            if (config.mapMarkers.rustMapUse)
                RustMap?.Call("ApiRemovePointUrl", Title);
            if (config.mapMarkers.MapUse)
                Interface.CallHook("API_RemoveMarker", "xdcobaltlab");
        }
        #endregion

        #region MainMetods
        private void GenerateIvent()
        {
            if (RemoveHouseTime != null)
                RemoveHouseTime.Destroy();
            if (SpawnHouseTime != null)
                SpawnHouseTime.Destroy();
            SpawnHouseTime = timer.Once(GenerateSpawnIventTime(), () =>
            {
                StartIvent();
            });
        }
        private BaseEntity CrateHackableLocked(BaseEntity box)
        {
            HackableLockedCrate CrateEnt = GameManager.server.CreateEntity("assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab", new Vector3(box.transform.position.x, box.transform.position.y + 1f, box.transform.position.z), box.transform.rotation, true) as HackableLockedCrate;
            CrateEnt.enableSaving = false;
            CrateEnt.Spawn();
            CrateEnt.OwnerID = 3566257;
            CrateEnt.inventory.itemList.Clear();
            for (int i = 0; i < config.boxSetting.lootBoxes.Count; i++)
            {
                if (CrateEnt.inventory.itemList.Count >= config.boxSetting.maxItemCount)
                    break;
                var cfg = config.boxSetting.lootBoxes[i];
                bool goodChance = UnityEngine.Random.Range(0, 100) >= (100 - cfg.DropChance);

                if (goodChance)
                {
                    if (cfg.BluePrint)
                    {
                        Item bp = ItemManager.Create(ResearchTable.GetBlueprintTemplate());
                        bp.blueprintTarget = ItemManager.FindItemDefinition(cfg.Shortname).itemid;
                        if (!bp.MoveToContainer(CrateEnt.inventory))
                            bp.Remove();
                    }
                    else
                    {

                        int s = random.Next(cfg.MinimalAmount, cfg.MaximumAmount);
                        if (config.boxSetting.lootWipePlus && cfg.wipeCheck)
                            s = s * 2;

                        Item GiveItem = ItemManager.CreateByName(cfg.Shortname, s, cfg.SkinID);
                        if (!string.IsNullOrEmpty(cfg.DisplayName))
                        {
                            GiveItem.name = cfg.DisplayName;
                        }
                        if (!GiveItem.MoveToContainer(CrateEnt.inventory))
                            GiveItem.Remove();
                    }
                }
            }
            CrateEnt.inventory.capacity = CrateEnt.inventory.itemList.Count;
            CrateEnt.inventory.MarkDirty();
            CrateEnt.SendNetworkUpdate();
            return CrateEnt;
        }

        private void StartIvent()
        {
            if (BasePlayer.activePlayerList.Count < config.iventController.minPlayedPlayers)
            {
                Puts("Недостаточно игроков для запуска ивента!");
                GenerateIvent();
                return;
            }
            if (RemoveHouseTime != null)
                RemoveHouseTime.Destroy();
            if (SpawnHouseTime != null)
                SpawnHouseTime.Destroy();

            GenerateBuilding();
        }

        private void StopIvent()
        {
            foreach (BaseEntity iventEnt in HouseCobaltLab)
                if (!iventEnt.IsDestroyed)
                    iventEnt?.Kill();
            if (config.notiferSettings.useUiNotifi)
                Cui.DestroyAllPlayer();

            HouseCobaltLab.Clear();
            if (SpawnHouseTime != null)
                SpawnHouseTime.Destroy();
            if (RemoveHouseTime != null)
                RemoveHouseTime.Destroy();
            DestroyAll<NpcZones>();
            DestroyAll<NPCMonitor>();
            RemoveMapMarker();
            GenerateIvent();
        }

        #region Method By OxideBro
        Vector3 RandomCircle(Vector3 center, float radius = 2)
        {
            float ang = UnityEngine.Random.value * 360;
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

        private NPCPlayerApex InstantiateEntity(string type, Vector3 position)
        {
            position.y = GetGroundPosition(position);
            var gameObject = Instantiate.GameObject(GameManager.server.FindPrefab(type), position, new Quaternion());
            gameObject.name = type;
            SceneManager.MoveGameObjectToScene(gameObject, Rust.Server.EntityScene);
            UnityEngine.Object.Destroy(gameObject.GetComponent<Spawnable>());
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            NPCPlayerApex component = gameObject.GetComponent<NPCPlayerApex>();
            return component;
        }
        void SpawnBots(BaseEntity box, bool help = false)
        {
            int count = config.npcController.countSpawnNpc;
            if (help && config.npcController.helpBot)
                if (config.npcController.helpCount > 0)
                    count = config.npcController.helpCount;

            for (int i = 0; i < count; i++)
            {
                NPCPlayerApex entity = null;
                entity = InstantiateEntity("assets/prefabs/npc/scientist/scientist.prefab", RandomCircle(box.transform.position, 10));
                entity.enableSaving = false;
                entity.Spawn();
                entity.IsInvinsible = false;
                entity.startHealth = config.npcController.healthNPC;
                entity.InitializeHealth(entity.startHealth, entity.startHealth);
                ControllerInventory(entity);
                entity.Stats.AggressionRange = entity.Stats.DeaggroRange = 150;
                entity.CommunicationRadius = 0;
                entity.displayName = config.npcController.nameNPC.GetRandom();
                entity.GetComponent<Scientist>().LootPanelName = entity.displayName;
                entity.CancelInvoke(entity.EquipTest);
                Equip(entity);
                entity.Stats.MaxRoamRange = 75f;
                entity.NeverMove = true;

                NPCMonitor npcMonitor = entity.gameObject.AddComponent<NPCMonitor>();
                npcMonitor.Initialize(box);
            }
        }

        public HeldEntity GetFirstWeapon(BasePlayer player)
        {
            foreach (Item item in player.inventory.containerBelt.itemList)
            {
                if (item.CanBeHeld() && (item.info.category == ItemCategory.Weapon))
                {
                    BaseProjectile projectile = item.GetHeldEntity() as BaseProjectile;
                    if (projectile != null)
                    {
                        global::Item items = projectile?.GetItem();
                        if (item != null && items.contents != null)
                        {
                            if (UnityEngine.Random.Range(0, 2) == 0)
                            {
                                global::Item item2 = global::ItemManager.CreateByName("weapon.mod.flashlight", 1, 0UL);
                                if (!item2.MoveToContainer(items.contents, -1, true))
                                    item2.Remove(0f); 
                            }
                            else
                            {
                                global::Item item3 = global::ItemManager.CreateByName("weapon.mod.lasersight", 1, 0UL);
                                if (!item3.MoveToContainer(items.contents, -1, true))
                                    item3.Remove(0f);
                            }
                        } 
                        projectile.primaryMagazine.contents = projectile.primaryMagazine.capacity;
                        projectile.SendNetworkUpdateImmediate();
                        return item.GetHeldEntity() as HeldEntity;
                    }
                }
            }
            return null;
        }
        private void Equip(BasePlayer player)
        {
            HeldEntity weapon = GetFirstWeapon(player);
            if (weapon != null)
            {
                weapon.SetHeld(true);
                weapon.SetLightsOn(false);
            }
        }

        private void ControllerInventory(BasePlayer player)
        {
            if (player == null)
                return;
            player.inventory.Strip();
            if (config?.npcController?.beltNpc?.Count > 0)
            {
                var index = random.Next(0, config.npcController.beltNpc.Count);
                ItemManager.CreateByName(config.npcController.beltNpc[index].Shortname, 1, config.npcController.beltNpc[index].SkinID).MoveToContainer(player.inventory.containerBelt);
            }
            if (config?.npcController?.wearNpc?.Count > 0)
            {
                for (int i = 0; i < config.npcController.wearNpc.Count; i++)
                {
                    if (player.inventory.containerWear.IsFull())
                        break;

                    var wear = config.npcController.wearNpc[i];
                    ItemManager.CreateByName(wear.Shortname, 1, wear.SkinID).MoveToContainer(player.inventory.containerWear);
                }
            }
        }
        public class NPCMonitor : FacepunchBehaviour
        {
            public NPCPlayerApex player
            {
                get; private set;
            }
            private List<Vector3> patrolPositions = new List<Vector3>();
            private Vector3 homePosition;
            private int lastPatrolIndex = 0;
            private void Awake()
            {
                player = GetComponent<NPCPlayerApex>();
                InvokeRepeating(UpdateDestination, 0f, 5.0f);
                checkNight();
                InvokeRandomized(new Action(checkNight), 0f, 30f, 5f);
            }
            private void checkNight()
            {
                HeldEntity heldEntity1 = player.GetActiveItem()?.GetHeldEntity() as HeldEntity;
                if (heldEntity1 != null)
                    heldEntity1.SetLightsOn(TOD_Sky.Instance.IsNight ? true : false);
            }

            public void Initialize(BaseEntity box)
            {
                this.homePosition = box.transform.position;
                GeneratePatrolPositions();
            }
            private void UpdateDestination()
            {
                if (player.AttackTarget == null)
                {
                    player.NeverMove = true;
                    float distance = (player.transform.position - homePosition).magnitude;
                    bool tooFar = distance > 20;

                    if (player.GetNavAgent == null || !player.GetNavAgent.isOnNavMesh)
                        player.finalDestination = patrolPositions[lastPatrolIndex];
                    else
                    {
                        if (Vector3.Distance(player.transform.position, patrolPositions[lastPatrolIndex]) < 5)
                            lastPatrolIndex++;
                        if (lastPatrolIndex >= patrolPositions.Count)
                            lastPatrolIndex = 0;
                        player.SetDestination(patrolPositions[lastPatrolIndex]);
                    }

                    player.SetDestination(patrolPositions[lastPatrolIndex]);
                    player.SetFact(NPCPlayerApex.Facts.Speed, tooFar ? (byte)NPCPlayerApex.SpeedEnum.Run : (byte)NPCPlayerApex.SpeedEnum.Walk, true, true);
                }
                else
                {
                    player.NeverMove = false;
                    player.IsStopped = false;

                    var attacker = player.AttackTarget as BasePlayer;
                    if (attacker == null)
                        return;

                    if (attacker.IsDead())
                        Forget();
                }
            }
            private void Forget()
            {
                player.lastDealtDamageTime = Time.time - 21f;
                player.SetFact(Facts.HasEnemy, 0, true, true);
                player.SetFact(Facts.EnemyRange, 3, true, true);
                player.SetFact(Facts.AfraidRange, 1, true, true);
                player.AiContext.EnemyNpc = null;
                player.AiContext.EnemyPlayer = null;
                player.AttackTarget = null;
                player.lastAttacker = null;
                player.lastAttackedTime = Time.time - 31f;
                player.LastAttackedDir = Vector3.zero;
                player.SetDestination(patrolPositions[lastPatrolIndex]);
            }
            private void OnDestroy()
            {
                if (player != null && !player.IsDestroyed)
                    player.Kill();
                Destroy(gameObject);
            }
            private void GeneratePatrolPositions()
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector3 position = homePosition + (UnityEngine.Random.onUnitSphere * 20f);
                    position.y = TerrainMeta.HeightMap.GetHeight(position);
                    patrolPositions.Add(position);
                }
                enabled = true;
            }
        }
        #region NpcZonesOrRadiation
        public class NpcZones : MonoBehaviour
        {
            private Vector3 Position;
            private float Radius;
            private void Awake()
            {
                gameObject.layer = (int)Layer.Reserved1;
                gameObject.name = "NpcZonesOrRadiation";
                var rigidbody = gameObject.AddComponent<Rigidbody>();
                rigidbody.useGravity = false;
                rigidbody.isKinematic = true;
            }
            public void Activate(BaseEntity box, float radius, bool rad)
            {
                Position = box.transform.position;
                Radius = radius;
                transform.position = Position;
                transform.rotation = new Quaternion();
                UpdateCollider();
                gameObject.SetActive(true);
                enabled = true;
                if (rad)
                {
                    UpdateCollider();
                    gameObject.SetActive(true);
                    enabled = true;
                    var Rads = gameObject.GetComponent<TriggerRadiation>();
                    Rads = Rads ?? gameObject.AddComponent<TriggerRadiation>();
                    Rads.RadiationAmountOverride = config.radiationConroller.radCount;
                    Rads.interestLayers = LayerMask.GetMask("Player (Server)");
                    Rads.enabled = true;
                }
            }
            private void OnDestroy()
            {
                Destroy(gameObject);
            }
            public void Kill()
            {
                Destroy(gameObject);
            }

            private void UpdateCollider()
            {
                var sphereCollider = gameObject.GetComponent<SphereCollider>();
                {
                    if (sphereCollider == null)
                    {
                        sphereCollider = gameObject.AddComponent<SphereCollider>();
                        sphereCollider.isTrigger = true;
                    }
                    sphereCollider.radius = Radius;
                }
            }
        }
        #endregion
        #endregion

        #endregion

        #region HelpMetods

        #region Узнаем квадрат
        string GetGridString(Vector3 pos)
        {
            char[] alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

            pos.z = -pos.z;
            pos += new Vector3(TerrainMeta.Size.x, 0, TerrainMeta.Size.z) * .5f;

            var cubeSize = 146.14f;

            int xCube = (int)(pos.x / cubeSize);
            int zCube = (int)(pos.z / cubeSize);
            //int yNumber = 1596;
            int firstLetterIndex = (int)(xCube / alpha.Length) - 1;
            string firstLetter = "";
            if (firstLetterIndex >= 0)
                firstLetter = $"{alpha[firstLetterIndex]}";

            var xStr = $"{firstLetter}{alpha[xCube % 26]}";
            var zStr = $"{zCube}";

            return $"{xStr}{zStr}";
        }

        private string NumberToString(int number)
        {
            bool a = number > 25;
            Char c = (Char)(65 + (a ? number - 26 : number));
            return a ? "A" + c : c.ToString();
        }
        #endregion

        #region discord

        #region FancyDiscord
        public class FancyMessage
        {
            public string content
            {
                get; set;
            }
            public bool tts
            {
                get; set;
            }
            public Embeds[] embeds
            {
                get; set;
            }

            public class Embeds
            {
                public string title
                {
                    get; set;
                }
                public int color
                {
                    get; set;
                }
                public List<Fields> fields
                {
                    get; set;
                }

                public Embeds(string title, int color, List<Fields> fields)
                {
                    this.title = title;
                    this.color = color;
                    this.fields = fields;
                }
            }

            public FancyMessage(string content, bool tts, Embeds[] embeds)
            {
                this.content = content;
                this.tts = tts;
                this.embeds = embeds;
            }

            public string toJSON() => JsonConvert.SerializeObject(this);
        }

        public class Fields
        {
            public string name
            {
                get; set;
            }
            public string value
            {
                get; set;
            }
            public bool inline
            {
                get; set;
            }
            public Fields(string name, string value, bool inline)
            {
                this.name = name;
                this.value = value;
                this.inline = inline;
            }
        }

        private void Request(string url, string payload, Action<int> callback = null)
        {
            Dictionary<string, string> header = new Dictionary<string, string>();
            header.Add("Content-Type", "application/json");
            webrequest.Enqueue(url, payload, (code, response) =>
            {
                if (code != 200 && code != 204)
                {
                    if (response != null)
                    {
                        try
                        {
                            JObject json = JObject.Parse(response);
                            if (code == 429)
                            {
                                float seconds = float.Parse(Math.Ceiling((double)(int)json["retry_after"] / 1000).ToString());
                            }
                            else
                            {
                                PrintWarning($" Discord rejected that payload! Responded with \"{json["message"].ToString()}\" Code: {code}");
                            }
                        }
                        catch
                        {
                            PrintWarning($"Failed to get a valid response from discord! Error: \"{response}\" Code: {code}");
                        }
                    }
                    else
                    {
                        PrintWarning($"Discord didn't respond (down?) Code: {code}");
                    }
                }
                try
                {
                    callback?.Invoke(code);
                }
                catch (Exception ex) { }

            }, this, RequestMethod.POST, header);
        }
        #endregion

        void SendDiscordMsg(string msg)
        {
            List<Fields> fields = new List<Fields>
            {
                new Fields(lang.GetMessage("XD_IVENT_HOUSECOBALT", this), msg, true),
            };
            FancyMessage newMessage = new FancyMessage(null, false, new FancyMessage.Embeds[1] { new FancyMessage.Embeds(null, 16775936, fields) });
            Request(config.notiferSettings.weebHook, newMessage.toJSON());
        }

        #endregion

        private static System.Random random = new System.Random();
        private int GenerateSpawnIventTime() => random.Next(config.iventController.minSpawnIvent, config.iventController.maxSpawnIvent);

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
        public void SendChatAll(string Message, params object[] args)
        {
            if (!String.IsNullOrEmpty(config.notiferSettings.weebHook))
            {
                string msg = GetLang(Message, null, args);
                SendDiscordMsg(GetLang(Message, null, args));
            }       
            //if (config.xDNotification.UseXDNotifications)
            //{
            //    foreach (BasePlayer player in BasePlayer.activePlayerList)
            //        AddNotify(player, "ОСОБЫЙ ГРУЗ", GetLang(Message, player.UserIDString, args).Replace("\n", " "));
            //    return;
            //}
            if (IQChat)
                IQChat?.Call("API_ALERT", GetLang(Message, null, args));
            else
                BasePlayer.activePlayerList.ToList().ForEach(p => p.SendConsoleCommand("chat.add",ConVar.Chat.ChatChannel.Global, 0, GetLang(Message, p.UserIDString, args)));
        }
        public void SendChatPlayer(string Message, BasePlayer player, ConVar.Chat.ChatChannel channel = ConVar.Chat.ChatChannel.Global)
        {
            //if (config.xDNotification.UseXDNotifications)
            //{
            //    AddNotify(player, "ОСОБЫЙ ГРУЗ", Message.Replace("\n", " "));
            //    return;
            //}
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message);
            else
                player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
        private static string HexToRustFormat(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            sb.Clear();
            return sb.AppendFormat("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a).ToString();
        }
        private void DestroyAll<T>()
        {

            UnityEngine.Object.FindObjectsOfType(typeof(T))
                .ToList()
                .ForEach(UnityEngine.Object.Destroy);
        }
        #endregion

        #region Command
        [ChatCommand("cl")]
        void CLCommand(BasePlayer player, string cmd, string[] Args)
        {
            if (!player.IsAdmin)
                return;
            if (Args == null || Args.Length == 0)
            {
                SendChatPlayer($"Используйте:\n/cl start - Запуск ивента досрочно\n/cl stop - отменить ивент досрочно", player);
                return;
            }
            switch (Args[0])
            {
                case "start":
                    {
                        if (SpawnHouseTime.Destroyed)
                        {
                            PrintToChat(player, "Ивент уже активен!");
                        }
                        else
                        {
                            SpawnHouseTime.Destroy();
                            StartIvent();
                        }
                        break;
                    }
                case "stop":
                    {
                        if (SpawnHouseTime.Destroyed)
                        {
                            StopIvent();
                            SendChatAll("Ивент окончен досрочно администратором!");
                        }
                        else
                        {
                            
                            SendChatPlayer("Нет активных ивентов", player);
                        }
                        break;
                    }
            }

        }
        #endregion

        #region Command itemAddOrReset
        [ChatCommand("cl.items")]
        void BoxItemCommand(BasePlayer player, string cmd, string[] Args)
        {
            if (Args == null || Args.Length == 0)
            {
                SendChatPlayer("Используйте:\n/cl.items add - добавить лут к существующему\n/cl.items reset - заменить старый лут на новый", player);
                return;
            }
            switch (Args[0])
            {
                case "add":
                    {
                        foreach (var item in player.inventory.containerMain.itemList)
                        {
                            config.boxSetting.lootBoxes.Add(new LootNpcOrBox
                            {
                                BluePrint = item.IsBlueprint(),
                                Shortname = item.IsBlueprint() ? item.blueprintTargetDef.shortname : item.info.shortname,
                                SkinID = item.skin,
                                DisplayName = string.Empty,
                                DropChance = 30,
                                MinimalAmount = 1,
                                MaximumAmount = 1
                            });
                        }
                        SaveConfig();
                        SendChatPlayer("Вы успешно добавили новые предметы для ящика.\nОбязательно настройте их в конфиге", player);
                        break;
                    }
                case "reset":
                    {
                        config.boxSetting.lootBoxes.Clear();
                        foreach (var item in player.inventory.containerMain.itemList)
                        {
                            config.boxSetting.lootBoxes.Add(new LootNpcOrBox
                            {
                                BluePrint = item.IsBlueprint(),
                                Shortname = item.IsBlueprint() ? item.blueprintTargetDef.shortname : item.info.shortname,
                                SkinID = item.skin,
                                DisplayName = string.Empty,
                                DropChance = 30,
                                MinimalAmount = 1,
                                MaximumAmount = 1
                            });
                        }
                        SaveConfig();
                        SendChatPlayer("Вы успешно заменили все предметы на новые.\nОбязательно настройте их в конфиге", player);
                        break;
                    }
            }
        }
        [ChatCommand("cl.botitems")]
        void NpcLootCommand(BasePlayer player, string cmd, string[] Args)
        {
            if (Args == null || Args.Length == 0)
            {
                SendChatPlayer("Используйте:\n/cl.botitems add - добавить лут к существующему\n/cl.botitems reset - заменить старый лут на новый", player);
                return;
            }
            switch (Args[0])
            {
                case "add":
                    {
                        foreach (var item in player.inventory.containerMain.itemList)
                        {
                            config.npcController.lootNpcs.Add(new LootNpcOrBox
                            {
                                BluePrint = item.IsBlueprint(),
                                Shortname = item.IsBlueprint() ? item.blueprintTargetDef.shortname : item.info.shortname,
                                SkinID = item.skin,
                                DisplayName = string.Empty,
                                DropChance = 30,
                                MinimalAmount = 1,
                                MaximumAmount = 1
                            });
                        }
                        SaveConfig();
                        SendChatPlayer("Вы успешно добавили новые предметы для npc.\nОбязательно настройте их в конфиге", player);
                        break;
                    }
                case "reset":
                    {
                        config.npcController.lootNpcs.Clear();
                        foreach (var item in player.inventory.containerMain.itemList)
                        {
                            config.npcController.lootNpcs.Add(new LootNpcOrBox
                            {
                                BluePrint = item.IsBlueprint(),
                                Shortname = item.IsBlueprint() ? item.blueprintTargetDef.shortname : item.info.shortname,
                                SkinID = item.skin,
                                DisplayName = string.Empty,
                                DropChance = 30,
                                MinimalAmount = 1,
                                MaximumAmount = 1
                            });
                        }
                        SaveConfig();
                        SendChatPlayer("Вы успешно заменили все предметы на новые.\nОбязательно настройте их в конфиге", player);
                        break;
                    }
            }
        }
        #endregion

        #region ui

        string json = "[{\"name\":\"MainUI\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"material\":\"assets/content/ui/uibackgroundblur-ingamemenu.mat\",\"color\":\"0.8 0.28 0.2 0.5\"},{\"type\":\"RectTransform\",\"anchormin\":\"1 0\",\"anchormax\":\"1 0\",\"offsetmin\":\"-1200 17\",\"offsetmax\":\"-910 80\"}]},{\"name\":\"title\",\"parent\":\"MainUI\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{Title}\",\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.6299891\",\"anchormax\":\"1 1\",\"offsetmax\":\"0 0\"}]},{\"name\":\"maintxt\",\"parent\":\"MainUI\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{txt}\",\"fontSize\":13,\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 0.6299891\",\"offsetmax\":\"0 0\"}]},{\"name\":\"png\",\"parent\":\"MainUI\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"assets/content/textures/generic/fulltransparent.tga\",\"png\":\"{png}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.008230452 0.6416668\",\"anchormax\":\"0.1179698 0.9750001\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]}]";

        public static class Cui
        {
            public static void CreateUIAllPlayer(string json, Vector3 pos)
            {
                DestroyAllPlayer();
                string newJson;
                newJson = json.Replace("{Title}", _.lang.GetMessage("XD_IVENT_HOUSECOBALT", _).ToUpper());
                newJson = newJson.Replace("{txt}", string.Format(_.lang.GetMessage("XD_IVENT_STARTUI", _), _.GetGridString(pos)));
                newJson = newJson.Replace("{png}", _.GetImage("pngForUi"));
                CommunityEntity.ServerInstance.ClientRPCEx(new SendInfo(Net.sv.connections), null, "AddUI", newJson);
            } 

            public static void DestroyAllPlayer()
                => CommunityEntity.ServerInstance.ClientRPCEx(new SendInfo(Net.sv.connections), null, "DestroyUI", "MainUI");            
        }
        #endregion
    }
}