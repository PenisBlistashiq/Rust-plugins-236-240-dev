using Newtonsoft.Json;
using Rust.Modular;
using System.Linq;
using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("IQControllerSpawnCars", "Mercury", "1.0.5")]
    [Description("Хочу следовать за трендами блин")]
    class IQControllerSpawnCars : RustPlugin
    {
        
                private static Configuration config = new Configuration();

        void SpawnFullTier(ModularCar Car)
        {
            var CarSetting = config.TierRare;
            if (Car == null) return;
            
            VehicleModuleEngine engine = Car.GetComponentInChildren<VehicleModuleEngine>();
            if (engine == null) return;
            
            foreach (KeyValuePair<Int32, Int32> Tier in CarSetting)
            {
                if (!IsRare(Tier.Value)) continue;
                engine.AdminFixUp(Tier.Key);
            }

            if (!config.UseLockInventoryCar) return;
            EngineStorage engineStorage = engine.GetContainer() as EngineStorage;
            if (engineStorage == null)
                return;

            engineStorage.inventory.SetLocked(true);
        }

        private void SpawnElementsTier(ModularCar Car)
        {
            if (Car == null) return;
            Dictionary<String, Int32> CarSetting = config.ElementSpawnRare;
            
            VehicleModuleEngine engine = Car.GetComponentInChildren<VehicleModuleEngine>();
            if (engine == null) return;

            EngineStorage engineContainer = engine.GetContainer() as EngineStorage;
            if (engineContainer == null) return;
            
            Int32 LimitElement = (Int32)(config.LimitSpawnElement != 0 ? config.LimitSpawnElement : engineContainer.inventory.capacity);
            for (Int32 j = 0; j < LimitElement; j++)
            {
                Int32 SlotElemt = UnityEngine.Random.Range(0, (Int32)(engineContainer.inventory.capacity));
                foreach (KeyValuePair<String, Int32> TierElement in CarSetting)
                {
                    if (!IsRare(TierElement.Value)) continue;
                    if (engineContainer.inventory.GetSlot(SlotElemt) != null) continue;

                    Item ElementCar = ItemManager.CreateByName(TierElement.Key, 1);
                    if (engineContainer.ItemFilter(ElementCar, j))
                        ElementCar.MoveToContainer(engineContainer.inventory, j, false);
                }
            }
            
            if (!config.UseLockInventoryCar) return;
            engineContainer.inventory.SetLocked(true);
        }
        protected override void SaveConfig() => Config.WriteObject(config);

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Ошибка чтения #6739 конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!"); 
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }
        private class Configuration
        {
            [JsonProperty("Выберите тип спавна. 0 - полный спавн по тирам(настраивайте шансы и тиры в листе)(т.е все детали сразу,в разном виде качества). 1 - Спавн отдельных деталей, с ограничениями в количестве и рандомным качеством в зависимости от шанса")]
            public SpawnType spawnType;
            [JsonProperty("Запретить игрокам доставать детали из машины?")]
            public Boolean UseLockInventoryCar;
            [JsonProperty("Настройка деталей и их шанс спавна.Шортнейм детали и шанс ее спавна")]
            public Dictionary<String, Int32> ElementSpawnRare = new Dictionary<String, Int32>();
            internal class FuelSettings
            {

                internal class RandomFuel
                {
                    [JsonProperty("Минимальное количество топлива")]
                    public Int32 MinFuel = 30;
                    [JsonProperty("Максимальное количество топлива")]
                    public Int32 MaxFuel = 200;
                    [JsonProperty("Включить рандомное количество спавна топлива (true - да/false - нет)")]
                    public Boolean UseRandom = false;
                }

                [JsonProperty("Настройка рандомного спавна топлива")]
                public RandomFuel randomFuel = new RandomFuel();
                [JsonProperty("Включить спавн топлива в машинах (true - да/false - нет)")]
                public Boolean UseFuelSpawned = true;
                [JsonProperty("Статичное количество топлива (Если включен рандом, то этот показатель не будет учитываться)")]
                public Int32 FuelStatic = 100;
                [JsonProperty("Шанс заполнения топливом транспорт (0-100)")]
                public Int32 RareUsing = 100;

                public Int32 GetFuelSpawned()
                {
                    Int32 Fuel = 0;

                    if(UseFuelSpawned)
                        if (Oxide.Core.Random.Range(0, 100) <= RareUsing)
                        {
                            if (randomFuel.UseRandom)
                                Fuel = Oxide.Core.Random.Range(randomFuel.MinFuel, randomFuel.MaxFuel);
                            else Fuel = FuelStatic;
                        }

                    return Fuel;
                }
            }

            [JsonProperty("Настройка заполнения топливом машин")]
            public FuelSettings fuelSettings = new FuelSettings();
            [JsonProperty("Настройки тиров. Номер тира(1-3) и шанс.")]
            public Dictionary<Int32, Int32> TierRare = new Dictionary<Int32, Int32>();

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    spawnType = SpawnType.TierFull,
                    UseLockInventoryCar = false,
                    fuelSettings = new FuelSettings
                    {
                        FuelStatic = 100,
                        RareUsing = 100,
                        UseFuelSpawned = true,
                        randomFuel = new FuelSettings.RandomFuel
                        {
                            MinFuel = 30,
                            MaxFuel = 200,
                            UseRandom = false,
                        }
                    },
                    TierRare = new Dictionary<Int32, Int32>
                    {
                        [1] = 80,
                        [2] = 50,
                        [3] = 25,
                    },
                    ElementSpawnRare = new Dictionary<String, Int32>
                    {
                        ["carburetor1"] = 80,
                        ["crankshaft1"] = 80,
                        ["sparkplug1"] = 80,
                        ["piston1"] = 80,       
                        ["carburetor2"] = 60,
                        ["crankshaft2"] = 60,
                        ["sparkplug2"] = 60,
                        ["piston2"] = 60,
                        ["carburetor3"] = 20,
                        ["crankshaft3"] = 20,
                        ["sparkplug3"] = 20,
                        ["piston3"] = 20, 
                    },
                    LimitSpawnElement = 0,
                };
            }
            [JsonProperty("Ограниченное количество спавна деталей. 0 - без ограничений")]
            public Int32 LimitSpawnElement;
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        private void OnEntityKill(VehicleModuleEngine module)
        {
            if (module == null) return;
            EngineStorage engineContainer = module.GetContainer() as EngineStorage;
            if (engineContainer == null) return;
		   		 		  						  	   		   					  	 				  			 		  		  
            if (!engineContainer.IsLocked()) return;
            engineContainer.inventory.Kill();
        }


                private void CarUpgrade(ModularCar Car)
        {
            if (Car == null) return;
            var CarSetting = config.spawnType;
            switch (CarSetting)
            {
                case SpawnType.TierFull:
                {
                    SpawnFullTier(Car);
                    break;
                }
                case SpawnType.ElementsTier:
                {
                    SpawnElementsTier(Car);
                    break;
                }
                default: { break; }
            }

            FuelSystemSpawned(Car.fuelSystem);
        }
        private object OnVehicleModuleMove(VehicleModuleEngine module)
        {
            EngineStorage engineStorage = module.GetContainer() as EngineStorage;
            if (engineStorage == null)
                return null;

            if (engineStorage.inventory.IsLocked())
                return null;
            
            return true;
        }
		   		 		  						  	   		   					  	 				  			 		  		  
        private void Unload()
        {
            if (!config.UseLockInventoryCar) return;

            foreach(var Entity in BaseNetworkable.serverEntities.entityList.Where(x => x.Value.ShortPrefabName.Contains("module_car")))
            {
                ModularCar Car = Entity.Value as ModularCar;
                if (Car == null) return;
                VehicleModuleEngine engine = Car.GetComponentInChildren<VehicleModuleEngine>();
                if (engine == null) return;

                EngineStorage engineContainer = engine.GetContainer() as EngineStorage;
                if (engineContainer == null) return;

                if (engineContainer.inventory.IsLocked())
                {
                    engineContainer.inventory.Clear();
                    engineContainer.inventory.SetLocked(false);
                }
            }
        }
        /// <summary>
        /// Обновление 1.0.x
        /// - Добавлена возможность заблокировать инвентарь машины в которую были автоматически установлены детали (их не смогут вытащить игроки)
        /// - Изменения в некоторых методах
        /// </summary>
		   		 		  						  	   		   					  	 				  			 		  		  
                public enum SpawnType
        {
            TierFull,
            ElementsTier
        }
        private void FuelSystemSpawned(EntityFuelSystem FuelSystem)
        {
            if (FuelSystem == null) return;
            Int32 FuelAmount = config.fuelSettings.GetFuelSpawned();
            if (FuelAmount == 0) return;

            NextTick(() =>
            {
                StorageContainer FuelContainer = FuelSystem.fuelStorageInstance.Get(true) as StorageContainer;
                if (FuelContainer == null || FuelContainer.inventory == null) 
                {
                    Puts("Не удалось найти контейнер для заправки");
                    return;
                }
                Item FuelItem = ItemManager.CreateByName("lowgradefuel", FuelAmount);
                if (FuelItem == null) return;
                FuelItem.MoveToContainer(FuelContainer.inventory);
                FuelContainer.SendNetworkUpdate();
                FuelContainer.inventory.MarkDirty();
            });
        }
        void OnVehicleModulesAssigned(ModularCar Car, ItemModVehicleModule[] modulePreset) => CarUpgrade(Car);
        public bool IsRare(Int32 Rare)
        {
            if (UnityEngine.Random.Range(0, 100) >= (100 - Rare))
                return true;
            else return false;
        }
        
        private void OnServerInitialized()
        {
            foreach(var Entity in BaseNetworkable.serverEntities.entityList.Where(x => x.Value.ShortPrefabName.Contains("module_car") && x.Value?.GetComponentInChildren<VehicleModuleEngine>()?.GetContainer()?.inventory.itemList.Count == 0))
            {
                ModularCar Car = Entity.Value as ModularCar;
                CarUpgrade(Car);
            }

            if (!config.UseLockInventoryCar)
            {
                Unsubscribe("OnVehicleModuleMove");
                Unsubscribe("Unload");
            }
        }
            }
}
