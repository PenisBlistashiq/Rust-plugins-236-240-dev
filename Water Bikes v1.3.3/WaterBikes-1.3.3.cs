using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Network;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("WaterBikes", "senyaa", "1.3.3")]
    [Description("Turns snowmobiles into waterbikes")]
    class WaterBikes : RustPlugin
    {
        #region Optional Dependencies
        [PluginReference]
        private Plugin ServerRewards;
        #endregion

        #region Constants
        public const int BEACHSIDE_TOPOLOGY = 16;
        public const int BEACH_TOPOLOGY = 8;
        public const int OCEANSIDE_TOPOLOGY = 256;
        #endregion

        #region Fields
        public static WaterBikes Instance;
        private static Dictionary<BaseNetworkable, WaterBikeComponent> waterbikes;
        private Dictionary<BasePlayer, Snowmobile> playerWaterbikes;
        private List<BasePlayer> cooldownList;
        private ItemDefinition priceDef;
        private Vector3 lastKilledSnowmobilePos; // workaround to get water bikes work properly with spray can reskinning
        #endregion

        #region Configuration
        private class Configuration : SerializableConfiguration
        {
            [JsonProperty("(0) Spawn cooldown (in seconds)")]
            public int Cooldown = 120;
            [JsonProperty("(1) Waterbike price item short name:amount (0 - free) (Use 'ServerRewards' as a key to use it')")]
            public KeyValuePair<string, int> Price = new KeyValuePair<string, int>("scrap", 0);
            [JsonProperty("(2) Waterbike prefab")]
            public string WaterbikePrefab = "assets/content/vehicles/snowmobiles/tomahasnowmobile.prefab";
            [JsonProperty("(3) Allow only 1 water bike per player")]
            public bool AllowOnlyOneWaterBikePerPlayer = false;
            [JsonProperty("(4) Starting fuel")]
            public int startingFuel = 0;
            [JsonProperty("(5) Make all snowmobiles waterbikes")]
            public bool Make_All_Snowmobiles_Waterbikes = true;
            [JsonProperty("(6) Allow waterbikes to drive on land")]
            public bool Allow_To_Move_On_Land = true;
            [JsonProperty("(7) Spawn permission name")]
            public string Spawn_Permission = "waterbikes.spawn";
            [JsonProperty("(8) This permission allows players to spawn waterbikes for free")]
            public string NoPay_Permission = "waterbikes.free";
            [JsonProperty("(9) Water bike despawn permission")]
            public string Despawn_Permission = "waterbikes.despawn";
            [JsonProperty(".(10) Engine thrust")]
            public int engineThrust = 5000;
            [JsonProperty(".(11) Engine thrust on land")]
            public int engineThrustOnLand = 49;
            [JsonProperty(".(12) Move slowly on grass or roads")]
            public bool slowlyOnGrass = true;
            [JsonProperty(".(13) Steering scale")]
            public float steeringScale = 0.05f;
            [JsonProperty(".(14) Allow spawning water bikes only on beaches")]
            public bool spawnOnlyOnBeaches = false;
            [JsonProperty(".(15) Automatically flip water bikes")]
            public bool autoFlip = false;
            [JsonProperty(".(16) Off axis drag")]
            public float offAxisDrag = 0.35f;
            [JsonProperty(".(17) Buoyancy force")]
            public float buoyancyForce = 730f;
            [JsonProperty(".(18) Enable 'boost' button (Left Shift)")]
            public bool enableBoostButton = false;
            [JsonProperty(".(19) 'Boost' button thrust")]
            public float boostButtonThrust = 10000f;
            [JsonProperty("..(20) 'Boost' duration (seconds)")]
            public float boostDuration = 5f;
            [JsonProperty("..(21) 'Boost' cooldown (seconds)")]
            public float boostCooldown = 30f;
            [JsonProperty("Thrust point position")]
            public Vector3 ThrustPoint = new Vector3(-0.001150894f, 0.055f, -1.125f);
            [JsonProperty("Buoyancy points")]
            public SerializedBuoyancyPoint[] BuoyancyPoints = new SerializedBuoyancyPoint[]
            {
                new SerializedBuoyancyPoint(new Vector3(-0.62f, 0.09f, -1.284f), 1.3f),
                new SerializedBuoyancyPoint(new Vector3(0.5f, 0.09f, -1.284f), 1.3f),
                new SerializedBuoyancyPoint(new Vector3(-0.68f, 0.09f, -0.028f), 1.3f),
                new SerializedBuoyancyPoint(new Vector3(0.54f, 0.09f, -0.028f), 1.3f),
                new SerializedBuoyancyPoint(new Vector3(-0.64f, 0.09f, 1.283f), 1.3f),
                new SerializedBuoyancyPoint(new Vector3(0.53f, 0.09f, 1.283f), 1.3f),
                new SerializedBuoyancyPoint(new Vector3(-0.05f, 0.148f, 3.015f), 1.3f),
                new SerializedBuoyancyPoint(new Vector3(-0.05f, 0.129f, 1.81f), 1.3f),
                new SerializedBuoyancyPoint(new Vector3(-0.05f, 0.529f, -0.828f), 1.3f)
            };
        }
        #endregion

        #region Configuration Boilerplate
        static Configuration config;

        private class SerializableConfiguration
        {
            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonHelper.Deserialize(ToJson()) as Dictionary<string, object>;
        }

        private static class JsonHelper
        {
            public static object Deserialize(string json) => ToObject(JToken.Parse(json));

            private static object ToObject(JToken token)
            {
                switch (token.Type)
                {
                    case JTokenType.Object:
                        return token.Children<JProperty>()
                                    .ToDictionary(prop => prop.Name,
                                                  prop => ToObject(prop.Value));

                    case JTokenType.Array:
                        return token.Select(ToObject).ToList();

                    default:
                        return ((JValue)token).Value;
                }
            }
        }

        private bool ValidateConfig(Dictionary<string, object> currentWithDefaults, Dictionary<string, object> currentRaw)
        {
            var changed = false;
            var oldKeys = new List<string>();

            foreach (var key in currentRaw.Keys)
            {
                if (currentWithDefaults.Keys.Contains(key)) continue;
                changed = true;
                oldKeys.Add(key);
            }

            foreach (var key in oldKeys)
            {
                currentRaw.Remove(key);
            }

            foreach (var key in currentWithDefaults.Keys)
            {
                object currentRawValue;
                if (currentRaw.TryGetValue(key, out currentRawValue))
                {
                    var defaultDictValue = currentWithDefaults[key] as Dictionary<string, object>;
                    var currentDictValue = currentRawValue as Dictionary<string, object>;

                    if (defaultDictValue != null)
                    {
                        if (currentDictValue == null)
                        {
                            currentRaw[key] = currentWithDefaults[key];
                            changed = true;
                            continue;
                        }

                        if (ValidateConfig(defaultDictValue, currentDictValue))
                            changed = true;
                    }
                    continue;
                }

                currentRaw[key] = currentWithDefaults[key];
                changed = true;
            }


            return changed;
        }

        protected override void LoadDefaultConfig() => config = new Configuration();

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    throw new JsonException();
                }

                var currentWithDefaults = config.ToDictionary();
                var currentRaw = Config.ToDictionary(x => x.Key, x => x.Value);

                if (ValidateConfig(currentWithDefaults, currentRaw))
                {
                    PrintWarning("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch
            {
                PrintWarning($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            Puts($"Configuration changes saved to {Name}.json");
            Config.WriteObject(config, true);
        }
        #endregion

        #region Localization
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You don't have permission to run this command",
                ["Spawned"] = "Spawned waterbike",
                ["NoWaterbike"] = "You aren't on a waterbike",
                ["Showing"] = "Showing buoyancy points...",
                ["NotEnough"] = "You don't have enough to buy a waterbike",
                ["Converted"] = "Snowmobile converted into waterbike",
                ["Cooldown"] = "You are on cooldown!",
                ["onlyBeach"] = "You can spawn water bikes only on beaches",
                ["onlyOne"] = "You can have only 1 water bike",
                ["removed"] = "Waterbike is removed",
                ["waterbikeDoesntExist"] = "You don't have a water bike",
                ["boostToast"] = "You can press LSHIFT to boost",
                ["lookat"] = "Look at a water bike"
            }, this, "en");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "У вас нет прав для выполнения этой команды",
                ["Spawned"] = "Заспаунен гидроцикл",
                ["NoWaterbike"] = "Не на гидроцикле",
                ["Showing"] = "Показываются точки плавучести...",
                ["NotEnough"] = "У вас не хватает ресурсов для покупки гидроцикла",
                ["Converted"] = "Снегоход переделан в гидроцикл",
                ["Cooldown"] = "Вы не можете спаунить гидроциклы так быстро",
                ["onlyBeach"] = "Вы можете заспаунить гидроцикл только на пляже",
                ["onlyOne"] = "Только 1 гидроцикл на игрока",
                ["removed"] = "Гидроцикл удалён",
                ["waterbikeDoesntExist"] = "У вас нет гидроцикла",
                ["boostToast"] = "Вы можете нажать LSHIFT для ускорения",
                ["lookat"] = "Смотрите на гидроцикл"
            }, this, "ru");
        }
        #endregion

        #region Types
        private struct SerializedBuoyancyPoint
        {
            public Vector3 Position;
            public float Size;
            public SerializedBuoyancyPoint(Vector3 Position, float Size)
            {
                this.Position = Position;
                this.Size = Size;
            }
        }

        private class WaterBikeComponent : FacepunchBehaviour
        {
            private Snowmobile _snowmobile;
            private Buoyancy _buoyancy;
            private Rigidbody _rigidbody;
            public GameObject thrustPoint;

            private GameObject parentPoint;

            private float gasPedal;
            private float steering;

            private int framesSinceLastFlip;
            private int framesSinceLastInWater;

            private bool boosting;
            private bool boostCooldown;


            private Vector3 _waterLoggedPointLocalPosition;

            void Awake()
            {
                _snowmobile = GetComponent<Snowmobile>();
                _rigidbody = _snowmobile.gameObject.GetComponent<Rigidbody>();

                _snowmobile.engineKW = config.Allow_To_Move_On_Land ? config.engineThrustOnLand : -1;

                if (_snowmobile.waterloggedPoint != null && _snowmobile.waterloggedPoint.parent != null)
                {
                    _waterLoggedPointLocalPosition = _snowmobile.waterloggedPoint.localPosition;
                    _snowmobile.waterloggedPoint.SetParent(null);
                    _snowmobile.waterloggedPoint.position = new Vector3(0, 2000, 0);
                }

                thrustPoint = new GameObject("ThrustPoint");
                thrustPoint.transform.SetParent(_snowmobile.transform, false);
                thrustPoint.transform.localPosition = config.ThrustPoint;
                InitBuoyancy();
                _snowmobile.SetFlag(BaseEntity.Flags.Reserved10, true, true, true);
                waterbikes.Add(_snowmobile, this);

                _snowmobile.SendNetworkUpdateImmediate();
            }

            void OnDestroy()
            {
                Destroy(_buoyancy);
                Destroy(parentPoint);
                Destroy(thrustPoint);
                if (_snowmobile.IsDestroyed) return;
                _snowmobile.waterloggedPoint.SetParent(_snowmobile.transform);
                _snowmobile.waterloggedPoint.transform.localPosition = _waterLoggedPointLocalPosition;
                _snowmobile.engineKW = 49;
                _snowmobile.SendNetworkUpdateImmediate();
                waterbikes.Remove(_snowmobile);
            }

            private void InitBuoyancy()
            {
                _buoyancy = _snowmobile.gameObject.AddComponent<Buoyancy>();

                _buoyancy.forEntity = _snowmobile;
                _buoyancy.rigidBody = _rigidbody;

                _buoyancy.doEffects = true;

                _buoyancy.requiredSubmergedFraction = 0f;

                _buoyancy.useUnderwaterDrag = true;
                _buoyancy.underwaterDrag = 20f;

                _buoyancy.waveHeightScale = 500f;

                var points = new BuoyancyPoint[config.BuoyancyPoints.Length];

                parentPoint = new GameObject("buoyancy");

                for (int i = 0; i < points.Length; i++)
                {
                    var go = new GameObject($"buoyancyPoint_{i}");

                    go.transform.SetParent(parentPoint.transform, false);
                    go.transform.localPosition = config.BuoyancyPoints[i].Position;

                    var point = go.AddComponent<BuoyancyPoint>();

                    point.buoyancyForce = config.buoyancyForce;
                    point.size = config.BuoyancyPoints[i].Size;
                    points[i] = point;
                }

                parentPoint.transform.SetParent(_snowmobile.transform, false);
                parentPoint.transform.localPosition = new Vector3(0.046f, -0.15f, -0.853f);

                _buoyancy.points = points;
            }
            void FixedUpdate()
            {
                var isInWater = WaterLevel.Test(thrustPoint.transform.position, true, _snowmobile);
                if (isInWater)
                    framesSinceLastInWater = 0;
                else
                    framesSinceLastInWater += 1;

                framesSinceLastFlip += 1;
                if (config.autoFlip && IsFlipped() && framesSinceLastFlip > 30f && isInWater)
                {
                    Flip();
                    framesSinceLastFlip = 0;
                    return;
                }

                if (IsFlipped() && _snowmobile.engineController.IsOn)
                {
                    _snowmobile.engineController.StopEngine();
                    Flip();
                    return;
                }
                _snowmobile.SetFlag(BaseEntity.Flags.Reserved7, _rigidbody.IsSleeping() && !_snowmobile.AnyMounted(), false, true);
                if (!_snowmobile.engineController.IsOn)
                {
                    gasPedal = 0f;
                    steering = 0f;
                }
                _snowmobile.SetFlag(BaseEntity.Flags.Reserved7, _rigidbody.IsSleeping() && !_snowmobile.AnyMounted(), false, true);

                if (gasPedal != 0f && isInWater && _buoyancy.submergedFraction > 0.3f)
                {
                    var force = (transform.forward + transform.right * steering * config.steeringScale).normalized * gasPedal * (boosting ? config.boostButtonThrust : config.engineThrust);
                    _rigidbody.AddForceAtPosition(force, thrustPoint.transform.position, ForceMode.Force);
                    _snowmobile.engineKW = 65;
                }
                else
                    _snowmobile.engineKW = config.Allow_To_Move_On_Land ? config.engineThrustOnLand : 1;


                if (!config.slowlyOnGrass || framesSinceLastInWater < 100 || TerrainMeta.TopologyMap.GetTopology(_snowmobile.transform.position, OCEANSIDE_TOPOLOGY))
                {
                    _rigidbody.drag = 0.2f + 0.6f * Mathf.InverseLerp(0f, 1f, _buoyancy.submergedFraction);
                    _rigidbody.angularDrag = 0.5f + 0.005f * Mathf.InverseLerp(0f, 2f, _rigidbody.velocity.SqrMagnitude2D());
                }

                parentPoint.transform.rotation = _snowmobile.transform.rotation;
                if (config.offAxisDrag > 0f)
                {
                    var value2 = Vector3.Dot(transform.forward, _rigidbody.velocity.normalized);
                    var num2 = Mathf.InverseLerp(0.98f, 0.92f, value2);
                    _rigidbody.drag += num2 * config.offAxisDrag * _buoyancy.submergedFraction;
                }

                var x = Mathf.InverseLerp(1f, 10f, _rigidbody.velocity.Magnitude2D()) * 0.5f * _snowmobile.healthFraction;

                if (!_snowmobile.engineController.IsOn)
                    x = 0f;

                var y = 1f - 0.3f * (1f - _snowmobile.healthFraction);

                _buoyancy.buoyancyScale = (1f + x) * y;
            }
            public void OnPlayerInput(InputState inputState)
            {
                if (!_snowmobile.AnyMounted()) return;

                if (inputState.IsDown(BUTTON.SPRINT) && !boosting && !boostCooldown && config.enableBoostButton)
                {
                    boosting = true;
                    boostCooldown = true;
                    Instance.timer.Once(config.boostDuration, () => { boosting = false; });
                    Instance.timer.Once(config.boostCooldown, () => { boostCooldown = false; });
                    SendEffect("assets/prefabs/tools/pager/effects/beep.prefab", _snowmobile.GetDriver(), transform.position);
                }


                if (inputState.IsDown(BUTTON.FORWARD))
                    gasPedal = 1f;
                else if (inputState.IsDown(BUTTON.BACKWARD))
                    gasPedal = -0.5f;
                else
                    gasPedal = 0f;

                if (inputState.IsDown(BUTTON.LEFT))
                {
                    steering = 1f;
                    return;
                }

                if (inputState.IsDown(BUTTON.RIGHT))
                {
                    steering = -1f;
                    return;
                }
                steering = 0f;
            }

            public bool IsFlipped()
            {
                return Vector3.Dot(Vector3.up, transform.up) <= 0f;
            }

            public void Flip()
            {
                _rigidbody.AddRelativeTorque(Vector3.right * 4f, ForceMode.VelocityChange);
                _rigidbody.AddForce(Vector3.up * 4f, ForceMode.VelocityChange);
            }
        }
        #endregion

        #region Hooks
        void Init()
        {
            Instance = this;
            waterbikes = new Dictionary<BaseNetworkable, WaterBikeComponent>();
            playerWaterbikes = new Dictionary<BasePlayer, Snowmobile>();
            cooldownList = Facepunch.Pool.GetList<BasePlayer>();
            permission.RegisterPermission(config.Spawn_Permission, this);
            permission.RegisterPermission(config.NoPay_Permission, this);
            permission.RegisterPermission(config.Despawn_Permission, this);
        }

        void Loaded()
        {
            if (config.Price.Key.ToLower() == "serverrewards" && ServerRewards == null)
            {
                PrintError("ServerRewards points are set as a price, but ServerRewards plugin is not installed! Players won't be charged for waterbikes!");
                return;
            }

            if (config.Price.Key.ToLower() == "serverrewards" && ServerRewards != null)
            {
                Puts("Using ServerRewards points as a price");
                return;
            }

            try
            {
                priceDef = ItemManager.FindItemDefinition(config.Price.Key);
                if (priceDef == null)
                    throw new NullReferenceException();
            }
            catch (Exception)
            {
                PrintError("Item name is invalid! Players won't be charged for water bikes");
            }
        }

        void Unload()
        {
            foreach (var obj in UnityEngine.Object.FindObjectsOfType<WaterBikeComponent>())
                UnityEngine.Object.DestroyImmediate(obj);

            waterbikes = null;
            playerWaterbikes = null;
            Facepunch.Pool.FreeList(ref cooldownList);
            config = null;
            Instance = null;
        }

        void OnServerInitialized(bool initial)
        {
            var count = 0;

            foreach (var ent in BaseNetworkable.serverEntities)
            {
                if (!(ent is Snowmobile)) continue;

                if ((config.Make_All_Snowmobiles_Waterbikes || (ent as BaseEntity).HasFlag(BaseEntity.Flags.Reserved10)) && !waterbikes.ContainsKey(ent))
                {
                    ent.gameObject.AddComponent<WaterBikeComponent>();
                    var snowmobile = ent as Snowmobile;
                    if (snowmobile.OwnerID != 0)
                    {
                        var player = BasePlayer.FindByID(snowmobile.OwnerID);
                        if (player == null) continue;
                        if (playerWaterbikes.ContainsKey(player))
                            playerWaterbikes[player] = snowmobile;
                        else
                            playerWaterbikes.Add(player, snowmobile);
                    }
                    count++;
                }
            }

            Puts($"Loaded {count} waterbikes");
        }

        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (player == null) return;
            var mounted = player.GetMountedVehicle();
            if (mounted != null && waterbikes.ContainsKey(mounted))
            {

                waterbikes[mounted].OnPlayerInput(input);
            }
        }

        void OnEngineStarted(BaseVehicle vehicle, BasePlayer driver)
        {
            if (driver == null || vehicle == null) return;
            if (!(vehicle is Snowmobile)) return;
            if (!waterbikes.ContainsKey(vehicle)) return;

            if (config.enableBoostButton)
                driver.ShowToast(GameTip.Styles.Blue_Normal, lang.GetMessage("boostToast", this, driver.UserIDString));

            if (config.Allow_To_Move_On_Land) return;

            if (!WaterLevel.Test(waterbikes[vehicle].thrustPoint.transform.position, true, vehicle))
            {
                timer.Once(0.1f, () =>
                {
                    if (vehicle != null)
                        (vehicle as Snowmobile).engineController.StopEngine();
                });
            }
        }

        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (!(entity is Snowmobile)) return;

            if (lastKilledSnowmobilePos == entity.transform.position)
            {
                entity.gameObject.AddComponent<WaterBikeComponent>();
                lastKilledSnowmobilePos = new Vector3();
                return;
            }

            if (!config.Make_All_Snowmobiles_Waterbikes) return;
            if (waterbikes.ContainsKey(entity)) return;
            entity.gameObject.AddComponent<WaterBikeComponent>();
        }

        void OnEntityKill(BaseNetworkable entity)
        {
            if (entity == null) return;
            if (!waterbikes.ContainsKey(entity)) return;
            lastKilledSnowmobilePos = entity.transform.position;
            var snowmobile = entity as Snowmobile;

            if (!playerWaterbikes.ContainsValue(snowmobile)) return;
            var player = BasePlayer.FindByID(snowmobile.OwnerID);
            if (player != null)
                playerWaterbikes.Remove(player);
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (!cooldownList.Contains(player)) return;

            cooldownList.Remove(player);
        }
        #endregion

        #region API
        [HookMethod("SpawnWaterbike")]
        public BaseEntity SpawnWaterbike(Vector3 position, Quaternion rotation, BasePlayer ownerPlayer = null)
        {
            var waterBike = GameManager.server.CreateEntity(config.WaterbikePrefab, position, rotation) as Snowmobile;
            waterBike.gameObject.AddComponent<WaterBikeComponent>();
            waterBike.Spawn();

            if (config.AllowOnlyOneWaterBikePerPlayer && ownerPlayer != null)
            {
                waterBike.OwnerID = ownerPlayer.userID;
                playerWaterbikes.Add(ownerPlayer, waterBike);
            }

            if (config.startingFuel > 0)
                waterBike.GetFuelSystem().AddStartingFuel(config.startingFuel);

            return waterBike;
        }
        #endregion

        #region Methods
        public Vector3 GetSpawnPosition(BasePlayer player)
        {
            RaycastHit hit;
            Vector3 position;
            if (Physics.Raycast(player.eyes.position, player.eyes.HeadForward(), out hit, 15f))
                position = hit.point + new Vector3(0, 1.2f, 0);
            else
                position = player.transform.position + new Vector3(0, 0.2f, 0);

            return position;
        }

        public void StartCooldown(BasePlayer player)
        {
            if (player == null) return;
            if (config.Cooldown <= 0) return;
            if (cooldownList.Contains(player)) return;

            cooldownList.Add(player);

            timer.Once(config.Cooldown, () =>
            {
                if (cooldownList.Contains(player))
                {
                    cooldownList.Remove(player);
                }
            });
        }

        bool CanAffordWaterBike(BasePlayer player)
        {
            if (config.Price.Value <= 0)
                return true;
            if (permission.UserHasPermission(player.UserIDString, config.NoPay_Permission))
                return true;

            if (config.Price.Key.ToLower() == "serverrewards")
            {
                var points = ServerRewards.Call("CheckPoints", player.userID);
                if (points == null) return false;
                if ((int)points < config.Price.Value) return false;
                return true;
            }

            if (priceDef == null)
                return true;

            return player.inventory.GetAmount(priceDef.itemid) >= config.Price.Value;

        }
        void SubtractPrice(BasePlayer player)
        {
            if (config.Price.Value <= 0)
                return;
            if (permission.UserHasPermission(player.UserIDString, config.NoPay_Permission))
                return;

            if (config.Price.Key.ToLower() == "serverrewards")
            {
                ServerRewards.Call("TakePoints", player.userID, config.Price.Value);
                return;
            }

            player.inventory.Take(null, priceDef.itemid, config.Price.Value);
        }

        public static void SendEffect(string effectPath, BasePlayer player, Vector3 position)
        {
            if (player == null) return;

            var effect = new Effect(effectPath, position, position);

            effect.pooledstringid = StringPool.Get(effect.pooledString);

            Net.sv.write.Start();
            Net.sv.write.PacketID(Message.Type.Effect);
            effect.WriteToStream(Net.sv.write);
            Net.sv.write.Send(new SendInfo(player.net.connection));
        }
        #endregion

        #region Commands
        [ChatCommand("waterbike_debug")]
        private void WaterbikeDebugCommand(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
            {
                PrintToChat(player, lang.GetMessage("NoPermission", this, player.UserIDString));
                return;
            }
            var vehicle = player.GetMountedVehicle();

            if (vehicle == null || !waterbikes.ContainsKey(vehicle))
            {
                PrintToChat(player, lang.GetMessage("NoWaterbike", this, player.UserIDString));
                return;
            }

            var buoy = vehicle.gameObject.GetComponent<Buoyancy>();
            var waterbike = waterbikes[vehicle];

            PrintToChat(player, lang.GetMessage("Showing", this, player.UserIDString));

            foreach (var point in buoy.points)
            {
                player.SendConsoleCommand("ddraw.text", 30f, Color.green, point.transform.position, $"<size=13>Force - {point.buoyancyForce}\nSize = {point.size}</size>");
                player.SendConsoleCommand("ddraw.box", 30f, Color.green, point.transform.position, point.size);
            }

            player.SendConsoleCommand("ddraw.box", 30f, Color.blue, waterbike.thrustPoint.transform.position, 0.1f);
            player.SendConsoleCommand("ddraw.text", 30f, Color.blue, waterbike.thrustPoint.transform.position, "<size=13>Thrust point</size>");
        }

        [ChatCommand("waterbike")]
        private void WaterbikeCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, config.Spawn_Permission))
            {
                PrintToChat(player, lang.GetMessage("NoPermission", this, player.UserIDString));
                return;
            }

            if (args.Length == 1 && args[0].ToLower() == "remove")
            {
                if (!permission.UserHasPermission(player.UserIDString, config.Despawn_Permission))
                {
                    PrintToChat(player, lang.GetMessage("NoPermission", this, player.UserIDString));
                    return;
                }

                if (config.AllowOnlyOneWaterBikePerPlayer)
                {
                    if (!playerWaterbikes.ContainsKey(player) || playerWaterbikes[player] == null)
                    {
                        PrintToChat(player, lang.GetMessage("waterbikeDoesntExist", this, player.UserIDString));
                        return;
                    }

                    playerWaterbikes[player].DismountAllPlayers();
                    playerWaterbikes[player].Kill(BaseNetworkable.DestroyMode.Gib);
                    PrintToChat(player, lang.GetMessage("removed", this, player.UserIDString));
                    return;
                }

                RaycastHit despawn_hit;
                if (Physics.Raycast(player.eyes.position, player.eyes.HeadForward(), out despawn_hit, 15f))
                {
                    var hit_ent = despawn_hit.GetEntity();
                    if (hit_ent != null && hit_ent is Snowmobile && waterbikes.ContainsKey(hit_ent))
                    {
                        hit_ent.Kill(BaseNetworkable.DestroyMode.Gib);
                        PrintToChat(player, lang.GetMessage("removed", this, player.UserIDString));
                        return;
                    }
                }
                PrintToChat(player, lang.GetMessage("lookat", this, player.UserIDString));
                return;
            }

            if (config.AllowOnlyOneWaterBikePerPlayer && playerWaterbikes.ContainsKey(player) && playerWaterbikes[player] != null)
            {
                PrintToChat(player, lang.GetMessage("onlyOne", this, player.UserIDString));
                return;
            }

            if (cooldownList.Contains(player))
            {
                PrintToChat(player, lang.GetMessage("Cooldown", this, player.UserIDString));
                return;
            }

            if (config.spawnOnlyOnBeaches && !(TerrainMeta.TopologyMap.GetTopology(player.transform.position, BEACH_TOPOLOGY) || TerrainMeta.TopologyMap.GetTopology(player.transform.position, BEACHSIDE_TOPOLOGY)))
            {
                PrintToChat(player, lang.GetMessage("onlyBeach", this, player.UserIDString));
                return;
            }

            RaycastHit hit;
            BaseEntity ent = null;

            if (Physics.Raycast(player.eyes.position, player.eyes.HeadForward(), out hit, 15f))
            {
                var hit_ent = hit.GetEntity();
                if (hit_ent != null && hit_ent is Snowmobile && !waterbikes.ContainsKey(hit_ent))
                {
                    ent = hit_ent;
                }
            }

            if (CanAffordWaterBike(player))
            {
                SubtractPrice(player);

                if (ent != null)
                {
                    ent.gameObject.AddComponent<WaterBikeComponent>();
                    PrintToChat(player, lang.GetMessage("Converted", this, player.UserIDString));
                }
                else
                {
                    SpawnWaterbike(GetSpawnPosition(player), player.eyes.rotation, player);
                    StartCooldown(player);
                    PrintToChat(player, lang.GetMessage("Spawned", this, player.UserIDString));
                }

                return;
            }

            PrintToChat(player, lang.GetMessage("NotEnough", this, player.UserIDString));
        }
        #endregion
    }
}
