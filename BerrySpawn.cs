using System;
using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using Newtonsoft.Json.Converters;
using Facepunch;
using VLB;

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rust;

namespace Oxide.Plugins
{
    [Info("BerrySpawn", "EcoSmile", "1.0.3")]
    class BerrySpawn : RustPlugin
    {
        static BerrySpawn ins;
        PluginConfig config;

        public class PluginConfig
        {
            [JsonProperty("Время респавна кустов (в секундах)")]
            public float RespawnTime;
            [JsonProperty("Минимальное расстояние между кустами (Рекомендуется не менее 20)")]
            public float Distance;
        }

        protected override void LoadDefaultConfig()
        {
            config = new PluginConfig
            {
                RespawnTime = 300,
                Distance = 20
            };
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

        Coroutine coroutine;
        List<Vector3> Spawnpoint = new List<Vector3>();

        void LoadData()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("BerrySpawnPoint"))
            {
                Spawnpoint = Interface.Oxide.DataFileSystem.ReadObject<List<Vector3>>("BerrySpawnPoint");
            }
            else
            {
                Interface.Oxide.DataFileSystem.WriteObject("BerrySpawnPoint", Spawnpoint = new List<Vector3>());
            }
        }

        private void OnServerInitialized()
        {
            ins = this;
            LoadConfig();
            LoadData();
            if (ConVar.Server.level == "Procedural Map") return;
            coroutine = ServerMgr.Instance.StartCoroutine(spawnBerry());
        }

        void Unload() 
        {
            if (coroutine != null)
                ServerMgr.Instance.StopCoroutine(coroutine);

            Interface.Oxide.DataFileSystem.WriteObject("BerrySpawnPoint", Spawnpoint);
        }

        void OnNewSave()
        {
            Interface.Oxide.DataFileSystem.WriteObject("BerrySpawnPoint", Spawnpoint = new List<Vector3>());
        }

        Vector3 RandomCircle(Vector3 center, float radius)
        {
            float ang = UnityEngine.Random.value * 360;
            float distance = UnityEngine.Random.Range(5, radius);
            Vector3 pos;
            pos.x = center.x + distance * Mathf.Sin(ang * Mathf.Deg2Rad);
            pos.z = center.z + distance * Mathf.Cos(ang * Mathf.Deg2Rad);
            pos.y = center.y;
            pos.y = GetGroundPosition(pos);
            return pos;
        }

        float GetGroundPosition(Vector3 pos)
        {
            float y = TerrainMeta.HeightMap.GetHeight(pos);
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(pos.x, pos.y + 200f, pos.z), Vector3.down, out hit, Mathf.Infinity,
                LayerMask.GetMask(new[] { "Terrain" })) && !hit.collider.name.Contains("rock_cliff")) return Mathf.Max(hit.point.y, y);
            return y;
        }
        

        private bool TestPosIsValid(Vector3 position)
        {
            var resources = new List<CollectibleEntity>();
            Vis.Entities(position, config.Distance, resources);
            if (resources.Where(x => x.ShortPrefabName.Contains("berry")).Count() > 0)
                return false;

            return true;
        }
           

        List<string> berryPrefab = new List<string>()
        {
            "assets/bundled/prefabs/autospawn/collectable/berry-white/berry-white-collectable.prefab",
            "assets/bundled/prefabs/autospawn/collectable/berry-yellow/berry-yellow-collectable.prefab",
            "assets/bundled/prefabs/autospawn/collectable/berry-green/berry-green-collectable.prefab",
            "assets/bundled/prefabs/autospawn/collectable/berry-red/berry-red-collectable.prefab",
            "assets/bundled/prefabs/autospawn/collectable/berry-blue/berry-blue-collectable.prefab"
        };


        IEnumerator spawnBerry()
        {
            var biomMap = TerrainMeta.Terrain.GetComponent<TerrainBiomeMap>();
            var tundraEntity = BaseCombatEntity.serverEntities.entityList.Where(x => x.Value.PrefabName.Contains("v3_tundra_forestside"));
            if (tundraEntity.Count() > Spawnpoint.Count)
            {
                Spawnpoint.Clear();
                foreach (var mm in tundraEntity)
                {
                    if (biomMap.GetBiome(mm.Value.transform.position, 4) >= 0.98f)
                    {
                        var pos = RandomCircle(mm.Value.transform.position, 10);
                        Spawnpoint.Add(pos);
                    }
                    yield return null;
                }
            }

            foreach (var berry in Spawnpoint)
            {
                if (TestPosIsValid(berry))
                {
                    var ent = GameManager.server.CreateEntity(berryPrefab.GetRandom(), berry);
                    ent.Spawn();
                }
                yield return null;
            }

            yield return CoroutineEx.waitForSecondsRealtime(config.RespawnTime);

            coroutine = ServerMgr.Instance.StartCoroutine(spawnBerry());
        }
    }
}
