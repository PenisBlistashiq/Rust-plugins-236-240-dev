using Facepunch;
using Newtonsoft.Json;
using Rust;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Oxide.Plugins
{
    [Info("Horse Spawns", "OxideBro", "0.1.0")]
    public class HorseSpawns : RustPlugin
    {
        #region Configuration
        private PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            PrintWarning("No configuration, create a new one. Thanks for download plugins in RustPlugin.ru");
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        private class PluginConfig
        {
            [JsonProperty("Популяция лошадей на один квадратный километр")]
            public int HorsePopulation;

            [JsonProperty("[MAP]: Включить отображение лошадей на стандартной карте")]
            public bool EnabledMapMarker;

            [JsonProperty("[MAP]: Радиус отметки на карте")]
            public float MarketRadius;

            [JsonProperty("[MAP]: Текст на отметке на карты лошадей")]
            public string MarkerDescription;
            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    HorsePopulation = 2,
                    EnabledMapMarker = true,
                    MarketRadius = 0.1f,
                    MarkerDescription = "Horse"
                };
            }
        }
        #endregion

        #region Oxide
        static HorseSpawns ins;

        private void OnServerInitialized()
        {
            ins = this;
            LoadConfig();
            RidableHorse.Population = config.HorsePopulation;
            StartSpawnHorse();
            PrintWarning($"Server size map: {TerrainMeta.Size.x}, Ridable Horses population {RidableHorse.Population}, Loaded {UnityEngine.Object.FindObjectsOfType<RidableHorse>().Count()} Horses");

        }
        void Unload()
        {
            var markers = GameObject.FindObjectsOfType<HorseMarker>();
            foreach (var marker in markers)
            {
                if (marker != null)
                    UnityEngine.Object.Destroy(marker);
            }
            var ents = UnityEngine.Object.FindObjectsOfType<RidableHorse>();
            foreach (var marker in ents)
            {
                if (marker != null)
                    marker.Kill();
            }
        }
        void OnEntityKill(BaseNetworkable entity)
        {
            if (entity == null || entity?.net?.ID == null) return;
            try
            {
                if (entity is RidableHorse) NextTick(() => StartSpawnHorse());
            }
            catch (NullReferenceException) { }
        }
        #endregion

        #region Spawn
        void StartSpawnHorse()
        {
            var ents = UnityEngine.Object.FindObjectsOfType<RidableHorse>();

            if (ents != null)
            {
                var entcount = ents.Count();
                var count = RidableHorse.Population * TerrainMeta.Size.x / 1000 * 2;
                if (count - entcount > 0) PrintWarning($"At the moment we will create {count - entcount} horses");
                for (int i = 0; i < count - entcount; i++)
                {
                    Vector3 vector = GetEventPosition();
                    RidableHorse rHorse = GameManager.server.CreateEntity("assets/rust.ai/nextai/testridablehorse.prefab", vector, new Quaternion(), true) as RidableHorse;
                    rHorse.enableSaving = true;
                    rHorse.Spawn();
                    if (config.EnabledMapMarker) rHorse.gameObject.AddComponent<HorseMarker>();
                }
            }
        }

        class HorseMarker : BaseEntity
        {
            RidableHorse horse;
            MapMarkerGenericRadius mapmarker;
            VendingMachineMapMarker MarkerName;
            SphereCollider sphereCollider;

            void Awake()
            {
                horse = GetComponent<RidableHorse>();
                sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.gameObject.layer = (int)Layer.Reserved1;
                sphereCollider.isTrigger = true;
                sphereCollider.radius = 10f;
                SpawnMapMarkers();
            }

            public void SpawnMapMarkers()
            {
                MarkerName = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab", horse.transform.position, Quaternion.identity, true) as VendingMachineMapMarker;
                MarkerName.markerShopName = ins.config.MarkerDescription;
                MarkerName.Spawn();
                mapmarker = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", horse.transform.position, Quaternion.identity, true) as MapMarkerGenericRadius;
                mapmarker.Spawn();
                mapmarker.radius = ins.config.MarketRadius;
                mapmarker.alpha = 1f;
                Color color = new Color(1.00f, 0.50f, 0.00f, 1.00f);
                Color color2 = new Color(0, 0, 0, 0);
                mapmarker.color1 = color;
                mapmarker.color2 = color2;
                mapmarker.SendUpdate();
            }

            private void OnTriggerEnter(Collider col)
            {
                var target = col.GetComponentInParent<BasePlayer>();
                if (target != null)
                    Destroy();
            }

            void OnDestroy()
            {
                if (mapmarker != null) mapmarker.Invoke("KillMessage", 0.1f);
                if (MarkerName != null) MarkerName.Invoke("KillMessage", 0.1f);
            }

            public void Destroy()
            {
                if (mapmarker != null) mapmarker.Invoke("KillMessage", 0.1f);
                if (MarkerName != null) MarkerName.Invoke("KillMessage", 0.1f);
            }
        }

        SpawnFilter filter = new SpawnFilter();
        List<Vector3> monuments = new List<Vector3>();

        static float GetGroundPosition(Vector3 pos)
        {
            float y = TerrainMeta.HeightMap.GetHeight(pos);
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(pos.x, pos.y + 200f, pos.z), Vector3.down, out hit, Mathf.Infinity, LayerMask.GetMask(new[] { "Terrain", "World", "Default", "Construction", "Deployed" })) && !hit.collider.name.Contains("rock_cliff"))
                return Mathf.Max(hit.point.y, y);
            return y;
        }

        public Vector3 RandomDropPosition()
        {
            var vector = Vector3.zero;
            float num = 1000f, x = TerrainMeta.Size.x / 3;

            do
            {
                vector = Vector3Ex.Range(-x, x);
            }
            while (filter.GetFactor(vector) == 0f && (num -= 1f) > 0f);
            float max = TerrainMeta.Size.x / 2;
            float height = TerrainMeta.HeightMap.GetHeight(vector);
            vector.y = height;
            return vector;
        }

        List<int> BlockedLayers = new List<int> { (int)Layer.Water, (int)Layer.Construction, (int)Layer.Trigger, (int)Layer.Prevent_Building, (int)Layer.Deployed, (int)Layer.Tree };
        static int blockedMask = LayerMask.GetMask(new[] { "Player (Server)", "Trigger", "Prevent Building" });

        public Vector3 GetSafeDropPosition(Vector3 position)
        {
            RaycastHit hit;
            position.y += 200f;

            if (Physics.Raycast(position, Vector3.down, out hit))
            {
                if (hit.collider?.gameObject == null)
                    return Vector3.zero;
                string ColName = hit.collider.name;

                if (!BlockedLayers.Contains(hit.collider.gameObject.layer) && ColName != "MeshColliderBatch" && ColName != "iceberg_3" && ColName != "iceberg_2" && !ColName.Contains("rock_cliff"))
                {
                    position.y = Mathf.Max(hit.point.y, TerrainMeta.HeightMap.GetHeight(position));
                    var colliders = Pool.GetList<Collider>();
                    Vis.Colliders(position, 1, colliders, blockedMask, QueryTriggerInteraction.Collide);
                    bool blocked = colliders.Count > 0;
                    Pool.FreeList<Collider>(ref colliders);
                    if (!blocked)
                        return position;
                }
            }
            return Vector3.zero;
        }

        public Vector3 GetEventPosition()
        {
            var eventPos = Vector3.zero;
            int maxRetries = 100;
            monuments = UnityEngine.Object.FindObjectsOfType<MonumentInfo>().Select(monument => monument.transform.position).ToList();
            do
            {
                eventPos = GetSafeDropPosition(RandomDropPosition());

                foreach (var monument in monuments)
                {
                    if (Vector3.Distance(eventPos, monument) < 150f)
                    {
                        eventPos = Vector3.zero;
                        break;
                    }
                }
            } while (eventPos == Vector3.zero && --maxRetries > 0);

            eventPos.y = GetGroundPosition(eventPos);

            if (eventPos.y < 0)
                GetEventPosition();
            return eventPos;
        }
        #endregion
    }
}