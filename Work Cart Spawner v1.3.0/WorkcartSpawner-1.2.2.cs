using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
    [Info("Workcart Spawner", "https://discord.gg/9vyTXsJyKR", "1.2.2")]
    [Description("Auto monitor and replace Workcarts for custom maps.")]
    class WorkcartSpawner : RustPlugin
    {
        #region Config
        class CustomWK {
            [JsonProperty("Enabled")]
            public bool Enabled = false;

            [JsonProperty("Max Health (default 1000)")]
            public float maxHealth = 1000f;

            [JsonProperty("Engine Force (default 35000)")]
            public float engineForce = 35000f;

            [JsonProperty("Maximum Speed (default 21)")]
            public float maxSpeed = 21f;

            [JsonProperty("Fuel Storage Store All Items (max slots must be > 1)")]
            public bool Store = false;

            [JsonProperty("Fuel Storage Maximum Slots (default 1)")]
            public int Slots = 1;

            [JsonProperty("Max Fuel per second (default 0.075)")]
            public float maxFuelPerSec = 0.075f;

            [JsonProperty("Idle Fuel per second (default 0.025)")]
            public float idleFuelPerSec = 0.025f;

            [JsonProperty("Driver Protection Density (default 1) (Range 0-100)")]
            public float protDensity = 1f;

            [JsonProperty("Engine Startup Time (default 0.25)")]
            public float engineStartupTime = 0.25f;

            [JsonProperty("Engine Damage to Slow (default 200)")]
            public float engineDamageToSlow = 200f;

            [JsonProperty("Engine Damage Time Frame (default 10)")]
            public float engineDamageTimeframe = 10f;

            [JsonProperty("Engine Slow Time (default 8)")]
            public float engineSlowedTime = 8f;

            [JsonProperty("Engine Slowed max Velocity (default 4)")]
            public float engineSlowedMaxVel = 4f;
        }

        private Configuration Settings;

        private class Configuration {
            [JsonProperty("Workcart Prefabs")]
            public List<string> WorkPrefabs;

            [JsonProperty("Find Prefabs Name")]
            public List<string> Prefabs;

            [JsonProperty("Terrain Passing Collision Enabled")]
            public bool TerrColl = true;

            [JsonProperty("Default spawn distance from the Prefab")]
            public float Distance  = 5f;

            [JsonProperty("Time to respawn after Death (seconds)")]
            public int Time;

            [JsonProperty("Customise Work Carts")]
            public CustomWK Custom = new CustomWK();

            public static Configuration Generate() {
                return new Configuration {
                    Prefabs = new List<string>() {
                        "assets/content/structures/train_tracks/train_track_3x3_end.prefab"
                    },
                    WorkPrefabs = new List<string>() {
                        "assets/content/vehicles/workcart/workcart_aboveground.entity.prefab",
                        "assets/content/vehicles/workcart/workcart_aboveground2.entity.prefab"
                    }
                };
            }
        }

        protected override void SaveConfig() => Config.WriteObject(Settings);

        protected override void LoadDefaultConfig() {
            Settings = Configuration.Generate();
            SaveConfig();
        }

        protected override void LoadConfig() {
            base.LoadConfig();
            try {
                Settings = Config.ReadObject<Configuration>();
                if (Settings?.WorkPrefabs == null) LoadDefaultConfig();
                SaveConfig();
            } catch {
                PrintError("Error reading config, please check !");
            }

            if (Settings.Time < 1) {
                Settings.Time = 10;
                SaveConfig();
                PrintError("Respawn Time under 1 sec. ! Reverted to 10.");
            }
        }
        #endregion

        #region Data
        static readonly int LowGradeFuel = -946369541;
        static int passLayer = ToLayer(LayerMask.GetMask("Trigger")); // 18
        private readonly Dictionary<GameObject, Timer> Timers = new Dictionary<GameObject, Timer>();
        private Dictionary<GameObject, TrainEngine> TrackObjects = new Dictionary<GameObject, TrainEngine>();

        void Unload() {
            foreach (KeyValuePair<GameObject, TrainEngine> entry in TrackObjects) 
                if (entry.Value != null) entry.Value.Kill();
            TrackObjects.Clear();

            foreach (KeyValuePair<GameObject, Timer> entry in Timers) entry.Value.Destroy();
            Timers.Clear();
        }

        void OnServerInitialized() {
            SpawnAllWorkCarts();
        }
        #endregion

        #region Methods
        private void SpawnAllWorkCarts() {
            if (Settings.WorkPrefabs.Count == 0) {
                PrintError("No Workcart Prefabs !");
                return;
            }

            foreach (GameObject Track in UnityEngine.Object.FindObjectsOfType<GameObject>().Where(o => Settings.Prefabs.Contains(o.name)))
                RespawnWorkCart(Track);
        }

        private void SetupCustomisation(TrainEngine trainEngine) {
            //string info = $"HP: [{trainEngine.MaxHealth()}] EForce: [{trainEngine.engineForce}] MSpeed: [{trainEngine.maxSpeed}] MFuPerSec: [{trainEngine.maxFuelPerSec}]" +
            //    $" IdleFuPerSec: [{trainEngine.idleFuelPerSec}] EngStTime: [{trainEngine.engineStartupTime}] EngDmgToSlow: [{trainEngine.engineDamageToSlow}]" +
            //    $" EngDmgTimeFrame: [{trainEngine.engineDamageTimeframe}] EngSlwTime: [{trainEngine.engineSlowedTime}] EngSlwMaxVel: [{trainEngine.engineSlowedMaxVel}]" +
            //    $" DriverProtDens: [{trainEngine.driverProtection.density}]";
            //Debug.LogWarning(info);

            trainEngine.SetMaxHealth(Settings.Custom.maxHealth);
            trainEngine.SetHealth(Settings.Custom.maxHealth);
            trainEngine.engineForce = Settings.Custom.engineForce;
            trainEngine.maxSpeed = Settings.Custom.maxSpeed;
            trainEngine.maxFuelPerSec = Settings.Custom.maxFuelPerSec;
            trainEngine.idleFuelPerSec = Settings.Custom.idleFuelPerSec;
            trainEngine.engineStartupTime = Settings.Custom.engineStartupTime;
            trainEngine.engineDamageToSlow = Settings.Custom.engineDamageToSlow;
            trainEngine.engineDamageTimeframe = Settings.Custom.engineDamageTimeframe;
            trainEngine.engineSlowedTime = Settings.Custom.engineSlowedTime;
            trainEngine.engineSlowedMaxVel = Settings.Custom.engineSlowedMaxVel;
            trainEngine.driverProtection.density = Settings.Custom.protDensity;

            if (Settings.Custom.Slots > 1) {
                StorageContainer storage = trainEngine.engineController.FuelSystem.fuelStorageInstance.Get(true);
                storage.panelName = "generic";
                storage.inventory.capacity = Settings.Custom.Slots;

                if (Settings.Custom.Store) {
                    storage.allowedItem = null;
                    storage.inventory.onlyAllowedItems = new ItemDefinition[0];
                }
            }
        }

        private void SetupCart(TrainCar train) {
            if (train == null) return;
            TrainEngine trainEngine = train?.GetComponent<TrainEngine>();
            train.FrontTrackSection.isStation = true;
            //trainEngine.CancelInvoke(trainEngine.DecayTick);

            if (Settings.TerrColl) {
                foreach (TriggerTrainCollisions Trigger in train.GetComponentsInChildren<TriggerTrainCollisions>()) {
                    Trigger.triggerCollider.gameObject.layer = passLayer;
                }
            }

            if (Settings.Custom.Enabled && trainEngine != null) SetupCustomisation(trainEngine);
        }

        private void RespawnWorkCart(GameObject Track) {
            Vector3 atPos = Track.transform.position + Track.transform.forward * Settings.Distance; atPos.y += 0.1f;
            int index = Random.Range(1, Settings.WorkPrefabs.Count+1) - 1;
            TrainEngine workCart = GameManager.server.CreateEntity(Settings.WorkPrefabs[index], atPos, Track.transform.rotation) as TrainEngine;
            workCart?.Spawn(); TrackObjects[Track] = workCart;
        }
        #endregion

        #region Hooks
        bool CustomFuel => Settings.Custom.Enabled && Settings.Custom.Slots > 1;

        object CanUseFuel(EntityFuelSystem fuelSystem, StorageContainer fuelContainer, float seconds, float fuelUsedPerSecond) {
            if (!CustomFuel) return null;

            GameObject Track = TrackObjects.FirstOrDefault(x => x.Value == fuelSystem.fuelStorageInstance.Get(true)?.GetParentEntity()).Key;
            if (Track.IsUnityNull() || fuelContainer.IsUnityNull()) return null;

            Item slot = fuelContainer?.inventory?.FindItemByItemID(LowGradeFuel);
            if (slot == null || slot.amount < 1) return 0;
 
            fuelSystem.pendingFuel += seconds * fuelUsedPerSecond;
            if (fuelSystem.pendingFuel >= 1f) {
                int num = Mathf.FloorToInt(fuelSystem.pendingFuel);
                slot.UseItem(num);
                fuelSystem.pendingFuel -= num;
                return num;
            }

            return 0;
        }

        object OnFuelItemCheck(EntityFuelSystem fuelSystem, StorageContainer fuelContainer) {
            if (!CustomFuel) return null;

            GameObject Track = TrackObjects.FirstOrDefault(x => x.Value == fuelSystem.fuelStorageInstance.Get(true)?.GetParentEntity()).Key;
            if (Track.IsUnityNull() || fuelContainer.IsUnityNull()) return null;

            List<Item> totItems = fuelContainer?.inventory?.FindItemsByItemID(LowGradeFuel);
            Item item;

            if (totItems == null) {
                item = ItemManager.CreateByItemID(LowGradeFuel);
                item.amount = 0;
            } else {
                item = ItemManager.CreateByItemID(LowGradeFuel); item.amount -= 1;
                totItems.ForEach(itm => { if (itm != item) item.amount += itm.amount; });
            }

            return item;
        }

        void OnEntitySpawned(TrainCar entity) {
            if (entity == null) return;
            NextTick(() => SetupCart(entity));
        }

        void OnEntityKill(TrainEngine entity, HitInfo info) {
            if (entity == null) return;

            GameObject Track = TrackObjects.FirstOrDefault(x => x.Value == entity).Key;
            if (Track != null) {
                KillTimer(Track);
                Timers.Add(Track, timer.Once(Settings.Time, () => RespawnWorkCart(Track)));
            }
        }
        #endregion

        #region Helpers
        void KillTimer(GameObject Track) {
            Timer timer; Timers.TryGetValue(Track, out timer);
            if (timer == null) return;
            timer.Destroy();
            Timers.Remove(Track);
        }

        public static int ToLayer(int bitmask) {
            int result = bitmask > 0 ? 0 : 31;
            while (bitmask > 1) {
                bitmask = bitmask >> 1;
                result++;
            }
            return result;
        }
        #endregion
    }
}
