using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("RandomSpawns", "S1m0n", "1.1.0")]
    class RandomSpawns : RustPlugin
    {
        #region Fields
        private bool Disabled;
        private int spawnCount;
        private Hash<string, List<Vector3>> spawnPoints = new Hash<string, List<Vector3>>();
        private static readonly Collider[] colBuffer = (Collider[])typeof(Vis).GetField("colBuffer", (BindingFlags.Static | BindingFlags.NonPublic))?.GetValue(null);
        System.Random random = new System.Random();
        #endregion

        #region Oxide Hooks
        [ChatCommand("showspawns")]
        void cmdShowSpawns(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;           
            SendReply(player, "Total spawn count: " + spawnCount);
            foreach (var list in spawnPoints.Values)
            {
                foreach (var position in list)
                    player.SendConsoleCommand("ddraw.box", 30f, Color.magenta, position, 1f);
            }
        }
        
        void OnServerInitialized()
        {           
            Disabled = true;
            LoadVariables();
            GenerateSpawnpoints();
        }
        BasePlayer.SpawnPoint OnFindSpawnPoint()
        {
            if (Disabled) return null;
            if (BasePlayer.activePlayerList.Count < configData.MinimumPlayersToActivate)
                return null;

            string biome = configData.SpawnAreas.Where(x => x.Value == true).ToList().GetRandom().Key.ToLower();
            var targetpos = GetSpawnPoint(biome);
            if (targetpos == null)
                return null;

            BasePlayer.SpawnPoint spawnPoint1 = new BasePlayer.SpawnPoint();
            spawnPoint1.pos = (Vector3)targetpos;
            spawnPoint1.rot = new Quaternion(0f, 0f, 0f, 1f);
            return spawnPoint1;
        }
        string GetMajorityBiome(Vector3 position)
        {
            Dictionary<string, float> biomes = new Dictionary<string, float>
            {
                {"arctic", TerrainMeta.BiomeMap.GetBiome(position, TerrainBiome.ARCTIC) },
                {"arid", TerrainMeta.BiomeMap.GetBiome(position, TerrainBiome.ARID) },
                {"temperate", TerrainMeta.BiomeMap.GetBiome(position, TerrainBiome.TEMPERATE) },
                {"tundra", TerrainMeta.BiomeMap.GetBiome(position, TerrainBiome.TUNDRA) }
            };
            return biomes.OrderByDescending(x => x.Value).ToArray()[0].Key;           
        }
        #endregion

        #region Functions
        private object GetSpawnPoint(string biome = "")
        {            
            if (string.IsNullOrEmpty(biome))
                biome = spawnPoints.ElementAt(random.Next(0, 3)).Key;

            Vector3 targetPos = spawnPoints[biome.ToLower()].GetRandom();

            var entities = Physics.OverlapSphereNonAlloc(targetPos, 10f, colBuffer, LayerMask.GetMask("Construction"));
            if (entities > 0)
            {
                spawnPoints[biome].Remove(targetPos);
                -- spawnCount;
                if (spawnCount < 10)
                {
                    PrintWarning("All current spawnpoints have been overrun by buildings and such. Disabling custom spawnpoints");
                    Disabled = true;
                    return null;
                }
                return GetSpawnPoint();
            }
            return targetPos;
        }
        private void GenerateSpawnpoints()
        {
            PrintWarning("Generating spawnpoints. This may take a moment");
            for (int i = 0; i < 1500; i++)
            {
                float max = TerrainMeta.Size.x / 2;
                var success = FindNewPosition(new Vector3(0,0,0), max);
                if (success is Vector3)
                {
                    Vector3 spawnPoint = (Vector3)success;
                    float height = TerrainMeta.HeightMap.GetHeight(spawnPoint);
                    if (spawnPoint.y >= height && !(spawnPoint.y - height > 1))
                    {
                        string biome = GetMajorityBiome(spawnPoint);
                        if (!spawnPoints.ContainsKey(biome))
                            spawnPoints.Add(biome, new List<Vector3>());
                        spawnPoints[biome].Add(spawnPoint);
                    }
                }
            }            
            foreach (var biome in spawnPoints)
                spawnCount += biome.Value.Count;
            PrintWarning($"{spawnCount} spawn points generated!");
            Disabled = false;
        }
        Vector3 CalculatePoint(Vector3 position, float max)
        {
            var angle = Math.PI * 2.0f * random.NextDouble();
            var radius = Math.Sqrt(random.NextDouble()) * max;
            var x = position.x + radius * Math.Cos(angle);
            var y = position.y + radius * Math.Sin(angle);
            return new Vector3((float)x, 300, (float)y);
        }
        object FindNewPosition(Vector3 position, float max, bool failed = false)
        {
            var targetPos = UnityEngine.Random.insideUnitCircle * max;
            var sourcePos = new Vector3(position.x + targetPos.x, 300, position.z + targetPos.y);
            var hitInfo = RayPosition(sourcePos);
            var success = ProcessRay(hitInfo);
            if (success == null)
            {
                if (failed) return null;
                else return FindNewPosition(position, max, true);
            }
            else if (success is Vector3)
            {
                if (failed) return null;
                else return FindNewPosition(new Vector3(sourcePos.x, ((Vector3)success).y, sourcePos.y), max, true);
            }
            else
            {
                sourcePos.y = Mathf.Max((float)success, TerrainMeta.HeightMap.GetHeight(sourcePos));
                return sourcePos;
            }
        }
        private object ProcessRay(RaycastHit hitInfo)
        {
            if (hitInfo.collider != null)
            {
                if (hitInfo.collider?.gameObject.layer == UnityEngine.LayerMask.NameToLayer("Water"))
                    return null;
                if (hitInfo.collider?.gameObject.layer == UnityEngine.LayerMask.NameToLayer("Prevent Building"))
                    return null;
                if (hitInfo.GetEntity() != null)
                {
                    return hitInfo.point.y;
                }
                if (hitInfo.collider?.name == "areaTrigger")
                    return null;
                if (hitInfo.collider?.GetComponentInParent<SphereCollider>() || hitInfo.collider?.GetComponentInParent<BoxCollider>())
                {
                    return hitInfo.collider.transform.position + new Vector3(0, -1, 0);
                }               
            }
            return hitInfo.point.y;
        }

        RaycastHit RayPosition(Vector3 sourcePos)
        {
            RaycastHit hitInfo;
            Physics.Raycast(sourcePos, Vector3.down, out hitInfo);//, LayerMask.GetMask("Terrain", "World", "Construction"));

            return hitInfo;
        }
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            public Dictionary<string, bool> SpawnAreas { get; set; }
            public int MinimumPlayersToActivate { get; set; }
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                SpawnAreas = new Dictionary<string, bool>
                {
                    { "Arid", true },
                    { "Arctic", true },
                    { "Temperate", true },
                    { "Tundra", true },
                },
                MinimumPlayersToActivate = 1
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion
    }
}
