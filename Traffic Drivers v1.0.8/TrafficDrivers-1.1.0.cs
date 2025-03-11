using Oxide.Core.Plugins;
using Oxide.Core;
using Newtonsoft.Json;
using UnityEngine;
using System.Collections.Generic;

namespace Oxide.Plugins 
{
    [Info("Traffic Drivers", "walkinrey", "1.1.0")]
    public class TrafficDrivers : RustPlugin
    {
        [PluginReference] private Plugin SpawnModularCar, Convoy;

        private SpawnManager _spawner;
        private List<Vector3[]> _roadPathes = new List<Vector3[]>();

        private Dictionary<uint, TrafficCompanion> _driverCompanions = new Dictionary<uint, TrafficCompanion>();
        private Dictionary<uint, TrafficDriver> _carsDrivers = new Dictionary<uint, TrafficDriver>();

        private Dictionary<ulong, PlayerPathRecorder> _recorders = new Dictionary<ulong, PlayerPathRecorder>();

        private TrafficCompanion GetCompanion(uint playerNetID) 
        {
            if(_driverCompanions.ContainsKey(playerNetID))
            {
                var value = _driverCompanions[playerNetID];
                if(value == null) return null;

                return value;
            }

            return null;
        }

        private TrafficDriver GetDriver(uint playerNetID)
        {
            if(_carsDrivers.ContainsKey(playerNetID))
            {
                var value = _carsDrivers[playerNetID];
                if(value == null) return null;

                return value;
            }

            return null;
        }

        #region Конфиг

        private Configuration _config;

        public class Configuration 
        {   
            [JsonProperty("Включить запись пути?")]
            public bool enableRecording = false;

            [JsonProperty("Пресеты автомобилей")]
            public Dictionary<string, CarPreset> carPresets = new Dictionary<string, CarPreset>();

            [JsonProperty("Пресеты водителей")]
            public Dictionary<string, DriverPreset> driverPresets = new Dictionary<string, DriverPreset>();

            [JsonProperty("Настройка лимитов, спавна и взаимодействий")]
            public Limits limits = new Limits();

            public class ConvoySupport
            {
                [JsonProperty("Включить поддержку плагина Convoy?")]
                public bool enableConvoySupport = false;

                [JsonProperty("Уничтожать машины трафика при начале ивента плагина Convoy?")]
                public bool destroyCarsOnEvent = false;

                [JsonProperty("Уничтожать машины трафика при столкновении с машинами ивента плагина Convoy?")]
                public bool destroyCarsOnHit = false;
            }

            public class Limits 
            {
                [JsonProperty("Максимальное количество автомобилей")]
                public MinMax maxCars = new MinMax();

                [JsonProperty("Минимальная длина дороги для использования трафиком")]
                public int minRoadWidth = 100; 

                [JsonProperty("Через сколько секунд удалять застрявшую машину?")]
                public float stuckDespawnTime = 60f;

                [JsonProperty("Задержка между спавном следующего автомобиля")]
                public int spawnCooldown = 5;

                [JsonProperty("Запретить ботам атаковать водителей и компаньонов?")]
                public bool blockTrafficTarget = true;

                [JsonProperty("Поддержка плагина Convoy")]
                public ConvoySupport convoy = new ConvoySupport();
            }

            public class DriverPreset 
            {
                [JsonProperty("Отображаемое имя водителя")]
                public string name = "Водитель Ярик";

                [JsonProperty("Скин бота (0 - рандом)")]
                public ulong skin = 0;

                [JsonProperty("Здоровье бота")]
                public MinMax health = new MinMax();

                [JsonProperty("Лут в боте")]
                public List<LootInfo> loot = new List<LootInfo>();

                [JsonProperty("Водитель будет продолжать движение (1) или продолжать движение ускорившись (2) при атаке?")]
                public int attackBehaviour = 1;

                [JsonProperty("Рейт получаемого урона")]
                public float damageReceiveRate = 0.5f;

                [JsonProperty("Одежда")]
                public List<ClothInfo> clothes = new List<ClothInfo>();

                [JsonProperty("Компаньон")]
                public CompanionSetup companion = new CompanionSetup();

                public class CompanionSetup 
                {
                    [JsonProperty("Спавнить компаньона для водителя? (садится пассажиром, стреляет и защищает водителя)")]
                    public bool enableCompanion = false;

                    [JsonProperty("Отображаемое имя компаньона")]
                    public string name = "Компаньон-защитник";

                    [JsonProperty("Здоровье компаньона")]
                    public MinMax health = new MinMax();

                    [JsonProperty("Одежда")]
                    public List<ClothInfo> clothes = new List<ClothInfo>();

                    [JsonProperty("Оружие")]
                    public List<ClothInfo> weapons = new List<ClothInfo>();
                    
                    [JsonProperty("Рейт получаемого урона")]
                    public float damageReceiveRate = 0.5f;

                    [JsonProperty("Рейт наносимого урона")]
                    public float damageHurtRate = 2f;
                }

                public class LootInfo : ClothInfo
                {
                    [JsonProperty("Название предмета", Order = -1)]
                    public string name;

                    [JsonProperty("Шанс спавна", Order = 3)]
                    public float chance;

                    [JsonProperty("Количество предмета", Order = 4)]
                    public MinMax amount;

                    [JsonProperty("Контейнер куда поместить предмет (main, belt, wear)", Order = 5)]
                    public string container;
                }

                public class ClothInfo 
                {
                    [JsonProperty("Шортнейм предмета")]
                    public string shortname;

                    [JsonProperty("Скин предмета")]
                    public ulong skin;
                }
            }

            public class CarPreset 
            {
                [JsonProperty("Модули")]
                public string[] modules = new string[] {};

                [JsonProperty("Добавить кодовый замок?")]
                public bool addCodeLock = false;

                [JsonProperty("Добавить дверной замок?")]
                public bool addDoorLock = false;

                [JsonProperty("Уровень запчастей (0-3, 0 чтобы заспавнить без запчастей)")]
                public int enginePartsTier = 3;

                [JsonProperty("Количество топлива (-1 для максимального кол-ва)")]
                public int fuelAmount = -1;

                [JsonProperty("Количество воды в цистерне (если она есть, -1 для максимального кол-ва)")]
                public int waterAmount = 0;

                [JsonProperty("Максимальная скорость")]
                public float maxSpeed = 15;

                [JsonProperty("Включить бесконечное топливо")]
                public bool infiniteFuel = true;

                [JsonProperty("Название пресета водителя (оставьте пустым, чтобы спавнить рандомного водителя)")]
                public string driverPreset = "";

                [JsonProperty("Удалять машину после смерти водителя?")]
                public bool destroyCarAfterDeath = false;

                [JsonProperty("Блокировать доступ к запчастям автомобиля?")]
                public bool blockEngineLooting = true;

                [JsonProperty("Удалять запчасти автомобиля после смерти бота?")]
                public bool destroyEngineParts = true;

                [JsonProperty("Добавлять мигающие синие огни к водительскому модулю автомобиля?")]
                public bool enableBlueLights = false;

                [JsonProperty("Время неуязвимости автомобиля и пассажиров после спавна")]
                public float immortalTime = 5f;

                [JsonProperty("Сделать машину неуязвимой?")]
                public bool enableImmortal = false;

                [JsonProperty("Детонатор для деспавна авто")]
                public Detonator detonator = new Detonator();

                [JsonProperty("Настройка лута в модуле хранения")]
                public StorageLoot storage = new StorageLoot();

                public class Detonator 
                {
                    [JsonProperty("Добавлять детонатор к автомобилю после смерти водителя (используется для деспавна авто)")]
                    public bool enableDetonator = true;

                    [JsonProperty("Через сколько секунд детонатор взорвет весь автомобиль?")]
                    public float timer = 300f;
                }

                public class StorageLoot 
                {
                    [JsonProperty("Добавлять лут в модуль хранения лута?")]
                    public bool enableLoot = true;

                    [JsonProperty("Лут в контейнере")]
                    public List<ItemInfo> lootInfo = new List<ItemInfo>();

                    public struct ItemInfo 
                    {
                        [JsonProperty("Шортнейм предмета")]
                        public string shortname;

                        [JsonProperty("Скин предмета")]
                        public ulong skin;

                        [JsonProperty("Название предмета (если не нужно оставьте пустым)")]
                        public string name;

                        [JsonProperty("Шанс спавна")]
                        public float chance;

                        [JsonProperty("Количество предмета")]
                        public MinMax amount;
                    }
                }
            
                public Dictionary<string, object> ConvertForAPI()
                {
                    var dict = new Dictionary<string, object>();

                    dict.Add("Modules", modules);

                    dict.Add("CodeLock", addCodeLock);
                    dict.Add("KeyLock", addDoorLock);

                    dict.Add("EnginePartsTier", enginePartsTier);
                    dict.Add("FuelAmount", fuelAmount);
                    dict.Add("FreshWaterAmount", waterAmount);

                    return dict;
                }
             }

            public class MinMax 
            {
                [JsonProperty("Минимум")]
                public float min = 1;

                [JsonProperty("Максимум")]
                public float max = 5;

                [JsonIgnore]
                public float randomized;

                public MinMax(float minimum = 1, float maximum = 5)
                {
                    min = minimum; 
                    max = maximum;
                }

                public float Randomize() 
                {
                    if(randomized != 0) return randomized;
                    else 
                    {
                        randomized = UnityEngine.Random.Range(min, max);
                        return randomized;
                    }
                }
            }
        }

        protected override void LoadDefaultConfig() 
        {
            _config = new Configuration();

            _config.carPresets.Add("3-х модульный транспорт", new Configuration.CarPreset
            {
                modules = new string[]
                {
                    "vehicle.1mod.engine",
                    "vehicle.1mod.storage",
                    "vehicle.1mod.cockpit.with.engine"
                },
                maxSpeed = 10,
                storage = new Configuration.CarPreset.StorageLoot
                {
                    enableLoot = true,
                    lootInfo = new List<Configuration.CarPreset.StorageLoot.ItemInfo>
                    {
                        new Configuration.CarPreset.StorageLoot.ItemInfo 
                        {
                            shortname = "wood",
                            chance = 100,
                            amount = new Configuration.MinMax
                            {
                                min = 1000,
                                max = 10000,
                            }
                        },
                        new Configuration.CarPreset.StorageLoot.ItemInfo
                        {
                            shortname = "stones",
                            chance = 100,
                            amount = new Configuration.MinMax
                            {
                                min = 5000,
                                max = 50000
                            }
                        }
                    }
                }
            });

            _config.driverPresets.Add("Водитель Ярик", new Configuration.DriverPreset
            {
                attackBehaviour = 2,
                health = new Configuration.MinMax(100, 150),
                loot = new List<Configuration.DriverPreset.LootInfo>
                {
                    new Configuration.DriverPreset.LootInfo
                    {
                        shortname = "rifle.ak",
                        amount = new Configuration.MinMax(1, 1),
                        chance = 100,
                        container = "belt"
                    },

                    new Configuration.DriverPreset.LootInfo
                    {
                        shortname = "hatchet",
                        amount = new Configuration.MinMax(1, 1),
                        chance = 100,
                        container = "main"
                    }
                },
                clothes = new List<Configuration.DriverPreset.ClothInfo>
                {
                    new Configuration.DriverPreset.ClothInfo
                    {
                        shortname = "hazmatsuit"
                    }
                },
                companion = new Configuration.DriverPreset.CompanionSetup
                {
                    enableCompanion = true,
                    health = new Configuration.MinMax(100, 150),
                    clothes = new List<Configuration.DriverPreset.ClothInfo>
                    {
                        new Configuration.DriverPreset.ClothInfo
                        {
                            shortname = "attire.banditguard"
                        }
                    },
                    weapons = new List<Configuration.DriverPreset.ClothInfo>
                    {
                        new Configuration.DriverPreset.ClothInfo
                        {
                            shortname = "rifle.ak"
                        }
                    }
                }
            });
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new System.Exception();

                SaveConfig();
            }
            catch (System.Exception ex)
            {
                PrintError("{0}", ex);
                LoadDefaultConfig();
            }
        }

        #endregion

        #region Дата

        private DataFile _data;

        private class DataFile 
        {
            [JsonProperty("Recorded pathes | Записанные пути")]
            public Dictionary<int, List<Vector3>> pathes = new Dictionary<int, List<Vector3>>();
        }

        private void LoadData() => _data = Interface.Oxide.DataFileSystem.ReadObject<DataFile>("TrafficDrivers_Pathes");
        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject("TrafficDrivers_Pathes", _data);

        #endregion

        #region Хуки

        private void OnTrafficDestroyed() => _spawner.OnTrafficDestroyed();

        private void Loaded()
        {
            NextTick(() =>
            {
                if(SpawnModularCar == null)
                {
                    PrintError("Плагин SpawnModularCar не установлен / SpawnModularCar plugin is not installed");
                    Interface.Oxide.UnloadPlugin(Title);

                    return;
                }

                if(!_config.enableRecording) Unsubscribe("OnPlayerInput");
                else Subscribe("OnPlayerInput");

                if(_config.limits.convoy.enableConvoySupport && _config.limits.convoy.destroyCarsOnEvent)
                {
                    Subscribe("OnConvoyStart");
                    Subscribe("OnConvoyStop");
                }
                else 
                {
                    Unsubscribe("OnConvoyStart");
                    Unsubscribe("OnConvoyStop");
                }

                LoadData();
            });
        }

        private void OnConvoyStart()
        {
            if(_spawner) _spawner.Pause();

            foreach(var driver in _carsDrivers.Values)
            {
                if(driver == null) continue;

                var bot = driver.GetComponent<BasePlayer>();
                var mountable = bot?.GetMountedVehicle();

                if(mountable != null) mountable.Kill();
                if(bot != null) bot.Kill();
            }
        }

        private void OnConvoyStop()
        {
            if(_spawner) _spawner.Resume(true);
        }

        private void Unload()
        {
            UnityEngine.Object.Destroy(_spawner);

            foreach(var driver in _carsDrivers.Values)
            {
                if(driver == null) continue;

                var bot = driver.GetComponent<BasePlayer>();
                var mountable = bot?.GetMountedVehicle();

                if(mountable != null) mountable.Kill();
                if(bot != null) bot.Kill();
            }

            foreach(var recorder in _recorders.Values)
            {
                if(recorder == null) return;
                UnityEngine.Object.Destroy(recorder);
            }
        }

        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            if(!input.WasJustPressed(BUTTON.RELOAD)) return;
            if(!_recorders.ContainsKey(player.userID)) return;

            var recorder = _recorders[player.userID];
            if(recorder == null) return;

            recorder.Input();
        }

        private object OnEntityTakeDamage(BaseVehicleModule module, HitInfo info)
        {
            if(module == null) return null;

            var car = module.Vehicle;

            if(car == null) return null;

            var driver = car.GetDriver();
            
            if(driver)
            {
                var traffic = GetDriver(driver.net.ID);

                if(traffic)
                {
                    if(traffic.IsCarImmortal) 
                    {
                        info.damageTypes = new Rust.DamageTypeList();
                        info.DidHit = false;
                        info.DoHitEffects = false;

                        return false;
                    }
                }
            }
            
            return null;
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if(entity == null || info == null || info?.damageTypes == null) return null;

            if(entity is BaseVehicleModule) OnEntityTakeDamage(entity as BaseVehicleModule, info);

            if(entity is BasePlayer)
            {
                var player = entity.ToPlayer();

                if(player != null)
                {
                    if(!player.userID.IsSteamId() && player.net != null)
                    {
                        var driver = GetDriver(player.net.ID);

                        if(driver) 
                        {
                            if(driver.IsImmortal) return false;
                            info.damageTypes.ScaleAll(driver.GetDamageReceiveRate());
                        }
                        else 
                        {
                            var companion = GetCompanion(player.net.ID);

                            if(companion) 
                            {
                                var companionDriver = GetDriver(companion.GetDriverID());

                                if(companionDriver)
                                {
                                    if(companionDriver.IsImmortal) return false;
                                }

                                info.damageTypes.ScaleAll(companion.GetDamageReceiveRate());
                            }
                        }
                    }
                }
            }
            else 
            {
                if(_config.limits.convoy.enableConvoySupport)
                {
                    if(_config.limits.convoy.destroyCarsOnHit)
                    {
                        if(Convoy)
                        {
                            if(info.Initiator is ModularCar)
                            {
                                var driverMounted = ((ModularCar)info.Initiator).GetDriver();

                                if(driverMounted != null)
                                {
                                    var driver = GetDriver(driverMounted.net.ID);

                                    if(driver != null)
                                    {
                                        if(Convoy.Call<bool>("IsConvoyVehicle", entity))
                                        {
                                            UnityEngine.Object.Destroy(driver);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                BaseModularVehicle vehicle = null;

                if(entity is BaseVehicleModule)
                {
                    var module = (BaseVehicleModule)entity;
                    if(module.Vehicle != null) vehicle = module.Vehicle;
                }

                if(entity is BaseModularVehicle) vehicle = (BaseModularVehicle)entity;

                if(vehicle != null)
                {
                    var driverMounted = vehicle.GetDriver();
                    if(driverMounted == null || driverMounted?.net == null) return null;

                    var driver = GetDriver(driverMounted.net.ID);
                    if(driver == null) return null;

                    if(driver.IsImmortal) return false;

                    if(info.InitiatorPlayer != null) driver.OnAttacked(info.InitiatorPlayer);
                    else if(info.Initiator != null)
                    {
                        if(info.Initiator is BaseVehicle && info.Initiator != entity)
                        {
                            var attackerVehicle = (BaseVehicle)info.Initiator;

                            if(attackerVehicle != null)
                            {
                                if(attackerVehicle.HasDriver())
                                {
                                    driver.OnAttacked(attackerVehicle.GetDriver());
                                    return null;
                                }
                            }
                        }

                        if(info.damageTypes.Total() > 2f) driver.OnPhysicAttacked();
                    }
                }
            }

            if(info.InitiatorPlayer)
            {
                var initiatorCompanion = GetCompanion(info.InitiatorPlayer.net.ID);

                if(initiatorCompanion)
                {
                    if(_carsDrivers.ContainsKey(entity.net.ID)) return false;
                }
            }

            return null;
        }

        private void OnServerInitialized()
        {
            if(_data == null) LoadData();

            if(_data.pathes.Count == 0)
            {
                Puts("Записанные пути не найдены, загружаем стандартные дороги Rust / Recorded paths not found, load standard Rust roads");

                foreach(var path in TerrainMeta.Path.Roads)
                {
                    if(path.Path.Points.Length < _config.limits.minRoadWidth || path.Splat == 1) continue;

                    _roadPathes.Add(path.Path.Points);
                }

                if(_roadPathes.Count == 0)
                {
                    PrintError(@"Не найдено ни одной подходящей дороги! Уменьшите минимальную длину дороги для трафика в конфиге. 
                    / No suitable roads found! Decrease the minimum road length for traffic in the config.");

                    Interface.Oxide.UnloadPlugin(Title);
                    return;
                }
            }
            else 
            {
                Puts("Найдены записанные пути в дата-файле, загружаем их / Recorded paths found in data file, load them");

                foreach(var path in _data.pathes)
                {
                    _roadPathes.Add(path.Value.ToArray());
                }
            }
            

            _spawner = new GameObject("Traffic SpawnManager", typeof(SpawnManager)).GetComponent<SpawnManager>();
            _spawner.Init(_config.limits, this, _roadPathes);

            Puts($"Найдено подходящих дорог: {_roadPathes.Count} / Finded suitable roads: {_roadPathes.Count}");
        }

        private object OnNpcTarget(BaseEntity npc, BaseEntity entity)
        {
            if(npc == null || entity == null) return null;

            if(_config.limits.blockTrafficTarget)
            {
                var driver = GetDriver(entity.net.ID);
                if(driver != null) return true;

                var companionBlock = GetCompanion(entity.net.ID);
                if(companionBlock != null) return true;
            }

            var companion = GetCompanion(npc.net.ID);

            if(companion != null)
            {
                if(!companion.CanTargetEntity(entity)) return true;
            }

            return null;
        }

        #endregion

        #region Методы

        [ConsoleCommand("trafficdrivers.debug")]
        private void showDebugInfo(ConsoleSystem.Arg arg)
        {
            if(arg.Player() != null) return;

            var drivers = _spawner.GetDrivers();
            string msg = $"Всего машин трафика: {drivers.Count} / Traffic cars amount: {drivers.Count}";

            foreach(var driver in drivers)
            {
                if(driver == null) continue;
                msg += $"\n{driver.CarPresetName}: {PhoneController.PositionToGridCoord(driver.transform.position)}";
            }

            Puts(msg);
        }

        [ChatCommand("trafficdrivers")]
        private void chatAdminCommand(BasePlayer player, string command, string[] args)
        {
            if(!player.IsAdmin) return;
            if(args?.Length == 0) return;

            if(args[0] == "recorder")
            {
                if(!_config.enableRecording)
                {
                    player.ChatMessage("Recorder disabled in config!");
                    return;
                }

                PlayerPathRecorder recorder;
                
                if(_recorders.ContainsKey(player.userID))
                {
                    recorder = _recorders[player.userID];
                    if(recorder != null) UnityEngine.Object.Destroy(recorder);

                    _recorders.Remove(player.userID);
                }

                recorder = player.gameObject.AddComponent<PlayerPathRecorder>();
                recorder.Init(this);

                _recorders.Add(player.userID, recorder);

                switch(lang.GetLanguage(player.UserIDString))
                {
                    case "ru":
                        player.ChatMessage("Рекордер добавлен. Чтобы начать/закончить запись пути, нажмите кнопку R");
                        break;

                    default:
                        player.ChatMessage("Recorder added. To start/stop recording a path, press R button");
                        break;
                }

                return;
            }
        }

        private TrafficDriver SpawnTrafficCarParams(Vector3[] path, Vector3 position, TrafficDriver.DriveSide side)
        {
            var presetsList = new List<Configuration.CarPreset>(_config.carPresets.Values);
            var driversList = new List<Configuration.DriverPreset>(_config.driverPresets.Values);

            var carPreset = presetsList[UnityEngine.Random.Range(0, presetsList.Count)]; 
            var driverPreset = !string.IsNullOrEmpty(carPreset.driverPreset) ? _config.driverPresets[carPreset.driverPreset] : driversList[UnityEngine.Random.Range(0, driversList.Count)];

            BasePlayer bot = GameManager.server.CreateEntity("assets/prefabs/player/player.prefab", position, Quaternion.identity) as BasePlayer;
            
            bot.displayName = driverPreset.name;

            bot.userID = driverPreset.skin == 0 ? (ulong)UnityEngine.Random.Range(1, 99999) : driverPreset.skin;
            bot.UserIDString = bot.userID.ToString();

            bot.enableSaving = false;

            bot.Spawn();
            bot.InitializeHealth(driverPreset.health.min, driverPreset.health.max);

            foreach(var cloth in driverPreset.clothes) bot.inventory.containerWear.Insert(ItemManager.CreateByName(cloth.shortname, 1, cloth.skin));

            foreach(var loot in driverPreset.loot)
            {
                if(UnityEngine.Random.Range(0, 100) < loot.chance)
                {
                    Item item = ItemManager.CreateByName(loot.shortname, (int)loot.amount.Randomize(), loot.skin);

                    if(item == null) continue;
                    if(!string.IsNullOrEmpty(loot.name)) item.name = loot.name;

                    if(loot.container == "main") bot.inventory.containerMain.Insert(item);
                    if(loot.container == "belt") bot.inventory.containerBelt.Insert(item);
                    if(loot.container == "wear") bot.inventory.containerWear.Insert(item);
                }
            }

            ModularCar car = SpawnModularCar.Call<ModularCar>("API_SpawnPresetCar", bot, carPreset.ConvertForAPI());

            car.enableSaving = false;

            if(carPreset.infiniteFuel)
            {
                var container = car.GetFuelSystem().fuelStorageInstance.Get(true);
                
                if (container != null)
                {
                    var item = ItemManager.CreateByName("lowgradefuel", 200, 12345);

                    item.OnDirty += delegate(Item item1)
                    {
                        item1.amount = 200;
                    };

                    container.inventory.Insert(item);
                    item.SetFlag(global::Item.Flag.IsLocked, true);
                    
                    container.dropsLoot = false;
                    container.inventory.SetFlag(ItemContainer.Flag.IsLocked, true);

                    container.SetFlag(BaseEntity.Flags.Locked, true);
                }
            }
            
            VehicleModuleStorage storage = null;
            BaseMountable driverPlace = null, companionPlace = null;

            foreach(var module in car.AttachedModuleEntities)
            {
                if(carPreset.storage.enableLoot && storage == null)
                {
                    if(module is VehicleModuleStorage && module.ShortPrefabName == "1module_storage")
                    {
                        storage = module as VehicleModuleStorage;
                        var container = storage.GetContainer();

                        if(container.inventory.entityOwner is StorageContainer) 
                        {
                            ((StorageContainer)container.inventory.entityOwner).dropsLoot = false;
                        }
                    
                        foreach(var item in carPreset.storage.lootInfo)
                        {
                            if(UnityEngine.Random.Range(0, 100) > item.chance) continue;

                            Item loot = ItemManager.CreateByName(item.shortname, (int)item.amount.Randomize(), item.skin);
                            if(!string.IsNullOrEmpty(item.name)) loot.name = item.name;

                            container.inventory.Insert(loot);
                        }
                    }
                }

                if(carPreset.blockEngineLooting)
                {
                    if(module is VehicleModuleEngine) 
                    {
                        var container = ((VehicleModuleEngine)module).GetContainer();
                        container.inventory.SetLocked(true);

                        if(container.inventory.entityOwner is StorageContainer) ((StorageContainer)container.inventory.entityOwner).dropsLoot = false;
                    }
                }

                if(module is VehicleModuleSeating)
                {
                    var seating = module as VehicleModuleSeating;

                    foreach(var info in seating.mountPoints)
                    {
                        if(driverPlace != null)
                        {
                            if(companionPlace == null && !driverPreset.companion.enableCompanion) break;
                            if(companionPlace != null) break;
                        }

                        if(info.isDriver && driverPlace == null) 
                        {
                            driverPlace = info.mountable;
                            if(carPreset.enableBlueLights) AddFlashingLights(seating);
                        }
                        else if(!info.isDriver && companionPlace == null && driverPreset.companion.enableCompanion)
                        {
                            companionPlace = info.mountable;
                        }
                    }
                }
            }

            driverPlace.MountPlayer(bot);

            ScientistNPC companion = null;

            if(driverPreset.companion.enableCompanion)
            {
                companion = GameManager.server.CreateEntity("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_any.prefab", position, Quaternion.identity) as ScientistNPC;
                companion.Spawn();

                companion.enableSaving = false;

                var trafficCompanion = companion.gameObject.AddComponent<TrafficCompanion>();
                trafficCompanion.Init(driverPreset.companion, bot.net.ID);

                companionPlace.MountPlayer(companion);
                _driverCompanions.Add(companion.net.ID, trafficCompanion);
            }

            var driver = bot.gameObject.AddComponent<TrafficDriver>();  
            driver.AssignCar(car, path, side, driverPreset, carPreset, GetKeyFromValue(_config.carPresets, carPreset), _config.limits.stuckDespawnTime, companion != null ? companion.GetComponent<TrafficCompanion>() : null);

            _carsDrivers.Add(bot.net.ID, driver);

            Puts($"Машина для трафика была успешно заспавнена! | Traffic car was sucessfully spawned!");

            return driver;
        }

        private void AddFlashingLights(VehicleModuleSeating module)
        {
            BaseEntity light1 = GameManager.server.CreateEntity("assets/prefabs/deployable/playerioents/lights/flasherlight/electric.flasherlight.deployed.prefab", module.transform.position), light2 = GameManager.server.CreateEntity("assets/prefabs/deployable/playerioents/lights/flasherlight/electric.flasherlight.deployed.prefab", module.transform.position);
            
            DestroyOnGroundMissing groundMissing;
            if(light1.TryGetComponent<DestroyOnGroundMissing>(out groundMissing)) UnityEngine.Object.Destroy(groundMissing);
            if(light2.TryGetComponent<DestroyOnGroundMissing>(out groundMissing)) UnityEngine.Object.Destroy(groundMissing);

            GroundWatch groundWatch;
            if(light1.TryGetComponent<GroundWatch>(out groundWatch)) UnityEngine.Object.Destroy(groundWatch);
            if(light2.TryGetComponent<GroundWatch>(out groundWatch)) UnityEngine.Object.Destroy(groundWatch);

            StabilityEntity stabilityEntity;
            if(light1.TryGetComponent<StabilityEntity>(out stabilityEntity)) UnityEngine.Object.Destroy(stabilityEntity);
            if(light2.TryGetComponent<StabilityEntity>(out stabilityEntity)) UnityEngine.Object.Destroy(stabilityEntity);

            light1.Spawn();
            light2.Spawn();

            light1.SetParent(module, true);
            light2.SetParent(module, true);

            module.AddChild(light1);
            module.AddChild(light2);

            light1.transform.localPosition += new Vector3(-0.5f, 1.35f, -0.4f);
            light2.transform.localPosition += new Vector3(0.5f, 1.35f, -0.4f);

            light1.SetFlag(BaseEntity.Flags.Reserved8, true);
            light2.SetFlag(BaseEntity.Flags.Reserved8, true);

            light1.SendNetworkUpdateImmediate(true);
            light2.SendNetworkUpdateImmediate(true);

            module.SendNetworkUpdateImmediate(true);
        }

        private TrafficDriver SpawnTrafficCar()
        {
            var road = _roadPathes[UnityEngine.Random.Range(0, _roadPathes.Count)];

            var side = UnityEngine.Random.Range(-5, 5) > 0 ? TrafficDriver.DriveSide.Right : TrafficDriver.DriveSide.Left;
            var position = road[side == TrafficDriver.DriveSide.Right ? 0 : road.Length - 1];

            return SpawnTrafficCarParams(road, position, side);
        }

        public static string GetKeyFromValue(Dictionary<string, Configuration.CarPreset> dictionary, Configuration.CarPreset value)
        {
            foreach (string keyVar in dictionary.Keys)
            {
                if (dictionary[keyVar] == value) return keyVar;
            }

            return null;
        }

        #endregion

        #region Запись пути

        private class PlayerPathRecorder : FacepunchBehaviour
        {
            private TrafficDrivers _pluginInstance;

            private BasePlayer _player;
            private List<Vector3> _path = new List<Vector3>();

            private bool _isRecording = false;

            private void Start() => _player = GetComponent<BasePlayer>();
            
            public virtual void Init(TrafficDrivers plugin) => _pluginInstance = plugin;

            public void Input()
            {
                if(_isRecording)
                {
                    StopRecording();
                    _path.Add(_player.transform.position);

                    _pluginInstance._data.pathes.Add(_pluginInstance._data.pathes.Keys.Count, _path);

                    switch(_pluginInstance.lang.GetLanguage(_player.UserIDString))
                    {
                        case "ru":
                            _player.ChatMessage($"Путь записан и сохранен в дата-файл. Индекс: {_pluginInstance._data.pathes.Keys.Count - 1}");
                            break;
                        default:
                            _player.ChatMessage($"Path recorded and saved to data. Index: {_pluginInstance._data.pathes.Keys.Count - 1}");
                            break;
                    }

                    _pluginInstance.SaveData();
                    Destroy(this);
                }
                else 
                {
                    StartRecording();

                    switch(_pluginInstance.lang.GetLanguage(_player.UserIDString))
                    {
                        case "ru":
                            _player.ChatMessage("Запись пути начата! Нажмите R чтобы остановить запись.");
                            break;
                        default:
                            _player.ChatMessage("Path recording started! Press R to stop recording.");
                            break;
                    }
                }
            }

            public void StartRecording()
            {
                _isRecording = true;
                InvokeRepeating(() => Record(), 0.1f, 1f);
            }

            public void StopRecording() => _isRecording = false;

            public virtual void Record()
            {
                if(!_isRecording) return;

                _path.Add(_player.transform.position);

                switch(_pluginInstance.lang.GetLanguage(_player.UserIDString))
                {
                    case "ru":
                        _player.ChatMessage($"Добавлена точка {_player.transform.position}, индекс {_path.Count - 1}");
                        break;
                    default:
                        _player.ChatMessage($"Added point {_player.transform.position}, index {_path.Count - 1}");
                        break;
                }
            }
        }

        #endregion

        #region Поведение

        public class TrafficCompanion : FacepunchBehaviour
        {
            private ScientistNPC _npc;
            private Configuration.DriverPreset.CompanionSetup _setup;
            private uint _driverNetID;

            private List<uint> _attackers = new List<uint>();

            public virtual bool CanTargetEntity(BaseEntity ent) => _driverNetID != ent.net.ID && _attackers.Contains(ent.net.ID);
            public virtual float GetDamageReceiveRate() => _setup.damageReceiveRate;

            public virtual uint GetDriverID() => _driverNetID;

            public virtual void Kill() => _npc.Kill();

            public virtual void AddAttacker(uint id) 
            {
                if(_attackers.Contains(id)) return;
                _attackers.Add(id);
            }
 
            public virtual void Init(Configuration.DriverPreset.CompanionSetup companionSetup, uint driverNet)
            {
                _driverNetID = driverNet;
                _setup = companionSetup;

                _npc = GetComponent<ScientistNPC>();

                var navigator = GetComponent<BaseNavigator>();

                navigator.CanUseNavMesh = false;
                navigator.MaxRoamDistanceFromHome = navigator.BestMovementPointMaxDistance = navigator.BestRoamPointMaxDistance = 0f;
                navigator.DefaultArea = "Not Walkable";     

                _npc.displayName = _setup.name;
                _npc._health = companionSetup.health.Randomize();
            
                _npc.inventory.containerWear.Clear();
                _npc.Brain = _npc.GetComponent<ScientistBrain>();

                _npc.CancelInvoke(_npc.EquipTest);

                foreach(var item in _npc.inventory.containerBelt.itemList) item.Remove();
                foreach(var weapon in _setup.weapons) ItemManager.CreateByName(weapon.shortname, 1, weapon.skin).MoveToContainer(_npc.inventory.containerBelt);

                foreach(var item in _npc.inventory.containerBelt.itemList)
                {
                    if(item.info.category == ItemCategory.Weapon)
                    {
                        var held = item.GetHeldEntity();
                        
                        if(held != null)
                        {
                            if(held is BaseProjectile)
                            {
                                var projectile = held as BaseProjectile;
                                projectile.damageScale += _setup.damageHurtRate;
                            }
                        }
                    }
                }

                foreach(var cloth in _setup.clothes) ItemManager.CreateByName(cloth.shortname, 1, cloth.skin).MoveToContainer(_npc.inventory.containerWear);

                _npc.EquipWeapon();
            }
        }

        public class TrafficDriver : FacepunchBehaviour
        {
            public enum Movement 
            {
                Idle = 0, Forward = 2, Backward = 4, Left = 8, Right = 16
            }

            public enum DriveSide 
            {
                Left = -1, Right = 1
            }

            private ModularCar _car;
            private BasePlayer _driver;

            private TrafficCompanion _companion;

            private DriveSide _side;
            private Vector3[] _path;
            
            private Configuration.DriverPreset _driverPreset;
            private Configuration.CarPreset _carPreset;
            
            private float _lastObstacleTime;
            private int _currentPathIndex = 0;

            private float _lastIncreaseSpeedTime, _stuckTime, _lastDestinationTime, _spawnTime, _lastTimeBackwards;

            public bool IsImmortal => _spawnTime + _carPreset.immortalTime > Time.realtimeSinceStartup;

            public bool IsCarImmortal => _carPreset.enableImmortal;
            public string CarPresetName;

            public virtual float GetDamageReceiveRate() => _driverPreset.damageReceiveRate;

            public virtual void Start() 
            {
                _driver = GetComponent<BasePlayer>();

                _spawnTime = Time.realtimeSinceStartup;
                _lastDestinationTime = Time.realtimeSinceStartup;

                InvokeRepeating(() => 
                {
                    var instance = TOD_Sky.Instance;

                    if(instance)
                    {
                        if(_car)
                        {
                            _car.SetFlag(BaseEntity.Flags.Reserved5, (instance.Cycle.Hour < 8 && instance.Cycle.Hour > 0));
                        }
                    }
                }, 5f, 5f);
            }

            public virtual void AssignCar(ModularCar car, Vector3[] path, DriveSide side, Configuration.DriverPreset driverPreset, Configuration.CarPreset carPreset, string carPresetName, float stuckTime, TrafficCompanion companion) 
            {
                CarPresetName = carPresetName;
                _companion = companion;

                _stuckTime = stuckTime;
                _car = car;

                _side = side;
                _path = path;

                _carPreset = carPreset;
                _driverPreset = driverPreset;
                
                _currentPathIndex = ((int)_side) > 0 ? 0 : _path.Length - 1;
            }

            public virtual void OnAttacked(BaseEntity attacker)
            {
                if(_companion) _companion.AddAttacker(attacker.net.ID);
                if(_driverPreset.attackBehaviour == 3) _lastIncreaseSpeedTime = UnityEngine.Time.realtimeSinceStartup + 10;
            }

            public virtual void OnPhysicAttacked() => _lastObstacleTime = UnityEngine.Time.realtimeSinceStartup + 3;

            public virtual void Update()
            {
                if(_car == null)
                {
                    if(_companion) _companion.Kill();
                    _driver.Kill();

                    return;
                }

                if(_lastDestinationTime + _stuckTime < Time.realtimeSinceStartup)
                {
                    if(_companion) _companion.Kill();

                    foreach(var module in _car.AttachedModuleEntities)
                    {
                        if(module is VehicleModuleStorage)
                        {
                            var storage = module as VehicleModuleStorage;
                            var container = storage.GetContainer();

                            for (int i = container.inventory.itemList.Count - 1; i >= 0; i--)
                            {
                                var item = container.inventory.itemList[i];
                                container.inventory.Remove(item);
                            }
                        }
                    }

                    _car.Kill();

                    return;
                }
                else if(_lastDestinationTime + (_stuckTime / 3) < Time.realtimeSinceStartup && _lastObstacleTime < Time.realtimeSinceStartup && _lastTimeBackwards < Time.realtimeSinceStartup)
                {
                    _lastTimeBackwards = Time.realtimeSinceStartup + 10;
                    OnPhysicAttacked();
                }

                Vector3 destination = Vector3.zero;

                if(_side == DriveSide.Left) destination = _path[_currentPathIndex] + new Vector3(2, 1, -4.5f * (int)TrafficDriver.DriveSide.Left - 2);
                else destination = _path[_currentPathIndex] + new Vector3(-2, 1, -3 * (int)TrafficDriver.DriveSide.Right + 2);

                if(Vector3.Distance(_car.transform.position, destination) < 10f) 
                {
                    _lastDestinationTime = Time.realtimeSinceStartup;
                    _currentPathIndex += (int)_side;

                    if(_side == DriveSide.Left)
                    {
                        if(_currentPathIndex < 0) 
                        {
                            _currentPathIndex = 0;
                            _side = DriveSide.Right;
                        }
                    }
                    else 
                    {
                        if(_currentPathIndex > _path.Length - 1) 
                        {
                            _currentPathIndex = _path.Length - 1;
                            _side = DriveSide.Left;
                        }
                    }
                }

                Movement verticalMovement = Movement.Idle, horizontalMovement = Movement.Idle;  

                Vector3 lhs = BradleyAPC.Direction2D(destination, _car.transform.position);
                float dotRight = Vector3.Dot(lhs, _car.transform.right);

                var turning =  Vector3.Dot(lhs, -_car.transform.forward) <= dotRight ? Mathf.Clamp(dotRight * 3f, -1f, 1f) : (dotRight < Vector3.Dot(lhs, -_car.transform.right) ? -1f : 1f);
                var throttle = (0.1f + Mathf.InverseLerp(0f, 20f, Vector3.Distance(_car.transform.position, destination)) * 1) * (1f - Mathf.InverseLerp(0f, 0.3f, Mathf.Abs(turning))) + Mathf.InverseLerp(0.1f, 0.4f, Vector3.Dot(_car.transform.forward, Vector3.up));
                
                if(_lastObstacleTime > UnityEngine.Time.realtimeSinceStartup) verticalMovement = Movement.Backward;
                else if(GetSpeed() < _carPreset.maxSpeed) verticalMovement = Movement.Forward;

                if(verticalMovement != Movement.Backward)
                {
                    if(turning < -0.6f) horizontalMovement = Movement.Left;
                    else if(turning > 0.6f) horizontalMovement = Movement.Right;
                }

                _car.PlayerServerInput(new InputState
                {
                    current = new InputMessage
                    {
                        buttons = (int)(verticalMovement | horizontalMovement)
                    }
                }, _driver);
            }

            private float GetSpeed() => _car.GetSpeed() - ((_lastIncreaseSpeedTime > UnityEngine.Time.realtimeSinceStartup) ? 5 : 0);

            private void OnDestroy()
            {
                Interface.CallHook("OnTrafficDestroyed");

                if(_car != null)
                {
                    var fuelStorage = _car.GetFuelSystem().fuelStorageInstance.Get(true);

                    if(fuelStorage != null)
                    {
                        fuelStorage.inventory.Clear();

                        fuelStorage.dropsLoot = true;
                        fuelStorage.SetFlag(BaseEntity.Flags.Locked, false);
                        fuelStorage.inventory.SetFlag(ItemContainer.Flag.IsLocked, false);
                    }

                    foreach(var module in _car.AttachedModuleEntities)
                    {
                        if(module is VehicleModuleEngine)
                        {
                            var engine = module as VehicleModuleEngine;

                            if(_carPreset.blockEngineLooting) engine.GetContainer().inventory.SetLocked(false);
                            if(_carPreset.destroyEngineParts) 
                            {
                                var container = engine.GetContainer();

                                for (int i = container.inventory.itemList.Count - 1; i >= 0; i--) container.inventory.Remove(container.inventory.itemList[i]);
                            }
                        }
                    }

                    if(_carPreset.destroyCarAfterDeath)
                    {
                        _car.Kill();

                        return;
                    }
                    else if(_carPreset.detonator.enableDetonator)
                    {
                        TimedExplosive detonator = GameManager.server.CreateEntity("assets/prefabs/tools/c4/explosive.timed.deployed.prefab", _car.transform.position, Quaternion.identity) as TimedExplosive;
                      
                        detonator.Spawn();
                        detonator.DoStick(_car.transform.position + new Vector3(0, 0.5f, 0), Vector3.zero, _car, null);
                        
                        detonator.SetFuse(_carPreset.detonator.timer);
                        detonator.Invoke(new System.Action(() =>
                        {
                            if(_car != null)
                            {
                                if(!_car.IsDestroyed)
                                {
                                    _car.Kill();
                                }
                            }
                        }), _carPreset.detonator.timer);
                    }

                }
                else if(_companion) _companion.Kill();
            }
        }

        #endregion

        #region Спавнер

        public class SpawnManager : FacepunchBehaviour
        {
            private Configuration.Limits _limits;
            private TrafficDrivers _pluginInstance;

            private List<Vector3[]> _roads;
            private List<TrafficDriver> _currentDrivers = new List<TrafficDriver>();

            private bool _isPaused = false;

            public virtual List<TrafficDriver> GetDrivers() 
            {
                for(int i = _currentDrivers.Count - 1; i >= 0; i--)
                {
                    if(_currentDrivers[i] == null) _currentDrivers.RemoveAt(i);
                }

                return _currentDrivers;
            }

            public virtual void Init(Configuration.Limits limits, TrafficDrivers plugin, List<Vector3[]> roads)
            {
                _roads = roads;
                _limits = limits;
                _pluginInstance = plugin;

                int cooldown = 0;

                for(int i = 0; i < (int)_limits.maxCars.Randomize(); i++)
                {
                    if(cooldown != 0)
                    {
                        Invoke("DelayedSpawn", cooldown);

                        cooldown += limits.spawnCooldown;
                    }
                    else 
                    {
                        TrafficDriver driver = _pluginInstance.Call<TrafficDriver>("SpawnTrafficCar");
                        _currentDrivers.Add(driver);

                        cooldown += limits.spawnCooldown;
                    }
                }

                Debug.Log($"[Traffic Drivers] Машин в очереди спавна: {cooldown / limits.spawnCooldown} | Cars in spawn queue: {cooldown / limits.spawnCooldown}");
            }

            private void DelayedSpawn() 
            {
                if(_isPaused) return;

                _currentDrivers.Add(_pluginInstance.Call<TrafficDriver>("SpawnTrafficCar"));
            }

            public virtual void OnTrafficDestroyed()
            {
                if(_isPaused) return;

                for(int i = 0; i < (_roads.Count == 1 ? 1 : 50); i++)
                {
                    var road = _roads[UnityEngine.Random.Range(0, _roads.Count)];
                    
                    Vector3 startPos = road[0];

                    List<BasePlayer> vis = new List<BasePlayer>();
                    Vis.Entities(startPos, 20f, vis, LayerMask.GetMask("Player (Server)"));

                    if(vis?.Count != 0)
                    {
                        if(HasConnectedPlayers(vis))
                        {
                            startPos = road[road.Length - 1];
                            Vis.Entities(startPos, 20f, vis, LayerMask.GetMask("Player (Server)"));

                            if(HasConnectedPlayers(vis)) continue;
                        }
                    }

                    _currentDrivers.Add(_pluginInstance.Call<TrafficDriver>("SpawnTrafficCarParams", road, startPos, road[0] == startPos ? TrafficDriver.DriveSide.Right : TrafficDriver.DriveSide.Left));

                    break;
                }
            }

            public bool HasConnectedPlayers(List<BasePlayer> list)
            {
                foreach(var player in list)
                {
                    if(player == null) continue;
                    if(player.IsConnected && !player.IsSleeping()) return true;
                }

                return false;
            }

            public virtual void Resume(bool callInit = true)
            {
                _isPaused = false;

                Init(_limits, _pluginInstance, _roads);
            }

            public virtual void Pause()
            {
                _isPaused = true;
            }
        }

        #endregion
    }
}