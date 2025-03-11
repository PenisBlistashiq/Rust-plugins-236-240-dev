using System.Collections.Generic;
using Newtonsoft.Json;
using Rust.Modular;

namespace Oxide.Plugins
{
    [Info("IQControllerSpawnCars", "Mercury", "0.0.3")]
    [Description("Хочу следовать за трендами блин")]
    class IQControllerSpawnCars : RustPlugin
    {
        // - Исправил NRE

        public bool Init = false;

        #region Vars
        public enum SpawnType
        {
            TierFull,
            ElementsTier
        }
        #endregion

        #region Configuration 
        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty("Выберите тип спавна. 0 - полный спавн по тирам(настраивайте шансы и тиры в листе)(т.е все детали сразу,в разном виде качества). 1 - Спавн отдельных деталей, с ограничениями в количестве и рандомным качеством в зависимости от шанса")]
            public SpawnType spawnType;
            [JsonProperty("Настройки тиров. Номер тира(1-3) и шанс.")]
            public Dictionary<int, int> TierRare = new Dictionary<int, int>();
            [JsonProperty("Настройка деталей и их шанс спавна.Шортнейм детали и шанс ее спавна")]
            public Dictionary<string, int> ElementSpawnRare = new Dictionary<string, int>();
            [JsonProperty("Ограниченное количество спавна деталей. 0 - без ограничений")]
            public int LimitSpawnElement;

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    spawnType = SpawnType.TierFull,
                    TierRare = new Dictionary<int, int>
                    {
                        [1] = 80,
                        [2] = 50,
                        [3] = 25,
                    },
                    ElementSpawnRare = new Dictionary<string, int>
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
        }

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
                PrintWarning($"Ошибка чтения #57 конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        private void OnServerInitialized() => Init = true;
        void OnEntitySpawned(BaseNetworkable entity)
		{
            if (!Init) return;
            if (entity == null) return;
			if (entity is ModularCar)
			{
                ModularCar Car = entity as ModularCar;
                if (Car == null) return;
                var CarSetting = config.spawnType;
                switch(CarSetting)
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
			}
		}

        #region Metods

        void SpawnFullTier(ModularCar Car)
        {
            var CarSetting = config.TierRare;
            if (Car == null) return;
            foreach (var Tier in CarSetting)
            {
                if (!IsRare(Tier.Value)) continue;
                if (Car.GetComponentInChildren<VehicleModuleEngine>() == null) continue;
                Car?.GetComponentInChildren<VehicleModuleEngine>()?.AdminFixUp(Tier.Key);
            }
        }

        void SpawnElementsTier(ModularCar Car)
        {
            if (Car == null) return;
            var CarSetting = config.ElementSpawnRare;
            int LimitElement = (int)(config.LimitSpawnElement != 0 ? config.LimitSpawnElement : Car?.GetComponentInChildren<VehicleModuleEngine>()?.GetContainer()?.inventory.capacity);
            for (int j = 0; j < LimitElement; j++)
            {
                int SlotElemt = UnityEngine.Random.Range(0, (int)(Car?.GetComponentInChildren<VehicleModuleEngine>()?.GetContainer()?.inventory.capacity));
                foreach (var TierElement in CarSetting)
                {
                    if (!IsRare(TierElement.Value)) continue;
                    if (Car?.GetComponentInChildren<VehicleModuleEngine>()?.GetContainer()?.inventory.GetSlot(SlotElemt) != null) continue;

                    Item ElementCar = ItemManager.CreateByName(TierElement.Key, 1);
                    if (Car.GetComponentInChildren<EngineStorage>().ItemFilter(ElementCar, j))
                        ElementCar.MoveToContainer(Car?.GetComponentInChildren<VehicleModuleEngine>()?.GetContainer()?.inventory, j, false);
                }
            }
        }
        public bool IsRare(int Rare)
        {
            if (UnityEngine.Random.Range(0, 100) >= (100 - Rare))
                return true;
            else return false;
        }
        #endregion
    }
}
