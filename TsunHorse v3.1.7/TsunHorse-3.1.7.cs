using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using UnityEngine;
using System.Linq;
using System.Globalization;
using Facepunch;
using UnityEngine.AI;

namespace Oxide.Plugins
{
    [Info("TsunHorse", "MoriF#1113", "3.1.7")]
    [Description("Allows players to mount and ride animals")]
    class TsunHorse : RustPlugin
    {
        #region Fields  
        private StoredData storedData;
        private DynamicConfigFile data;

        private List<Controller> controllers = new List<Controller>();
        private static TsunHorse ins = null;

        const string CHAIR_PREFAB = "assets/prefabs/deployable/chair/chair.deployed.prefab";
        const string ANIMAL_PREFAB = "assets/rust.ai/agents/{0}/{0}.prefab";
        const string HORSE_PREFAB = "assets/rust.ai/nextai/testridablehorse.prefab";

        const int ROCK_MASK = 1 << 16;

        protected static readonly RaycastHit[] raycastHits = new RaycastHit[64];
        #endregion

        #region Oxide Hooks
        private void Loaded()
        {            
            permission.RegisterPermission("tsunhorse.stophorse", this);
            permission.RegisterPermission("tsunhorse.ridehorse", this);
            permission.RegisterPermission("tsunhorse.spawnhorse", this);

            for (int i = 0; i < configData.Settings.AllowedTypes.Length; i++)
            {
                string str = configData.Settings.AllowedTypes[i];
                permission.RegisterPermission($"tsunhorse.ridehorse.{str}", this);
                permission.RegisterPermission($"tsunhorse.spawnhorse.{str}", this);
            }            
            
            lang.RegisterMessages(Messages, this);

            data = Interface.Oxide.DataFileSystem.GetFile("tsunhorse_cooldowns");

            ins = this;

            LoadData();
        }

        private void OnServerSave() => SaveData();
     
        private void Unload()
        {
            for (int i = controllers.Count - 1; i >= 0; i--)
            {
                Controller controller = controllers[i];
                controller.Player.DismountObject();
                UnityEngine.Object.Destroy(controller);
            }
            ins = null;
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null)
                return null;

            BaseNpc baseNpc = entity.GetComponent<BaseNpc>();
            if (baseNpc == null)
                return null;

            AnimalController animalController = entity.GetComponent<AnimalController>();
            if (animalController != null)
            {
                if (configData.Command.Invulnerable)
                {
                    if (!baseNpc.GetNavAgent.enabled)
                        return true;
                }

                if (configData.Settings.Invulnerable)
                    return true;

                if (info?.InitiatorPlayer == animalController.Human)
                    return true;

                return null;
            }            

            return null;
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null)
                return;

            AnimalController animalController = entity.GetComponent<AnimalController>();
            if (animalController != null)
            {
                if (animalController.Human != null)
                {
                    animalController.Human.Player.DismountObject();
                    UnityEngine.Object.Destroy(animalController.Human);
                }
                UnityEngine.Object.Destroy(animalController);
                return;
            }

            Controller controller = entity.GetComponent<Controller>();
            if (controller != null)
            {
                controller.Player.DismountObject();
                UnityEngine.Object.Destroy(controller);
            }
        }

        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (player == null || player.isMounted) return;

            if (input.WasJustPressed(BUTTON.USE))
            {
                RaycastHit hit;
                if (Physics.SphereCast(player.eyes.position, 0.5f, Quaternion.Euler(player.serverInput.current.aimAngles) * Vector3.forward, out hit, 1.5f))
                {
                    BaseAnimalNPC baseNpc = hit.GetEntity()?.GetComponent<BaseAnimalNPC>();
                    if (baseNpc == null)
                        return;

                    if (!permission.UserHasPermission(player.UserIDString, "tsunhorse.ridehorse") && !permission.UserHasPermission(player.UserIDString, $"tsunhorse.ridehorse.{baseNpc.ShortPrefabName}"))
                        return;

                    if (!configData.Settings.AllowedTypes.Any(x => x.Contains(baseNpc.ShortPrefabName, CompareOptions.IgnoreCase)))
                    {
                        SendReply(player, msg("Warning.InvalidType", player.UserIDString));
                        return;
                    }

                    if (baseNpc.health <= 1)
                    {
                        SendReply(player, msg("Warning.NearDeath", player.UserIDString));
                        return;
                    }

                    if (baseNpc.GetComponent<AnimalController>())
                    {
                        SendReply(player, msg("Warning.InUse", player.UserIDString));
                        return;
                    }

                    Controller controller = player.gameObject.AddComponent<Controller>();
                    controller.MountToNpc(baseNpc);
                    controllers.Add(controller);
                }
            }            
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            Controller controller = player.GetComponent<Controller>();
            if (controller == null)
                return;

            controllers.Remove(controller);
            UnityEngine.Object.Destroy(controller);
        }

        private void OnEntityDismounted(BaseMountable mountable, BasePlayer player)
        {
            Controller controller = player.GetComponent<Controller>();
            if (controller == null)
                return;
           
            controllers.Remove(controller);
            UnityEngine.Object.Destroy(controller);
        }

        private object CanAnimalAttack(BaseNpc entity, BasePlayer player)
        {
            AnimalController animalController = entity.GetComponent<AnimalController>();
            if (animalController != null)
                return false;
            return null;
        }

        private object CanNPCEat(BaseNpc entity, BaseEntity target)
        {
            AnimalController animalController = entity.GetComponent<AnimalController>();
            if (animalController != null)
                return false;
            return null;
        }

        private object OnNpcTarget(BaseNpc entity, BaseEntity target)
        {
            AnimalController animalController = entity.GetComponent<AnimalController>();
            if (animalController != null)
                return false;
            return null;
        }
        #endregion

        #region Functions
        private static double CurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;

        private string FormatTime(double time)
        {
            TimeSpan dateDifference = TimeSpan.FromSeconds((float)time);
            int days = dateDifference.Days;
            int hours = dateDifference.Hours;
            int mins = dateDifference.Minutes;
            int secs = dateDifference.Seconds;

            if (days > 0)
                return string.Format("{0:00}d:{1:00}h:{2:00}m:{3:00}s", days, hours, mins, secs);
            else if (hours > 0)
                return string.Format("{0:00}h:{1:00}m:{2:00}s", hours, mins, secs);
            else if (mins > 0)
                return string.Format("{0:00}m:{1:00}s", mins, secs);
            else return string.Format("{0}s", secs);
        }

        private bool SpawnAnimal(string type, Vector3 position, out BaseNpc baseNpc)
        {
            Vector3 point;
            if (FindPointOnNavmesh(position, 1, out point))
            {
                baseNpc = InstantiateEntity(string.Format(ANIMAL_PREFAB, type), point) as BaseNpc;
                if (baseNpc == null)
                    return false;

                baseNpc.Spawn();                
                return true;
            }
            baseNpc = null;
            return false;
        }

        private bool SpawnRideableHorse(Vector3 position, out RidableHorse ridableHorse)
        {
            ridableHorse = InstantiateEntity(HORSE_PREFAB, position) as RidableHorse;
            if (ridableHorse == null)
                return false;

            ridableHorse.Spawn();
            return true;
        }

        private BaseEntity InstantiateEntity(string type, Vector3 position)
        {
            var gameObject = Instantiate.GameObject(GameManager.server.FindPrefab(type), position, new Quaternion());
            gameObject.name = type;

            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gameObject, Rust.Server.EntityScene);

            UnityEngine.Object.Destroy(gameObject.GetComponent<Spawnable>());

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            BaseEntity component = gameObject.GetComponent<BaseEntity>();
            return component;
        }

        private bool FindPointOnNavmesh(Vector3 center, float range, out Vector3 result)
        {
            for (int i = 0; i < 30; i++)
            {
                Vector3 randomPoint = center + UnityEngine.Random.insideUnitSphere * range;
                UnityEngine.AI.NavMeshHit hit;
                if (UnityEngine.AI.NavMesh.SamplePosition(randomPoint, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    result = hit.position;
                    return true;
                }
            }
            result = Vector3.zero;
            return false;
        }
        #endregion

        #region Controller         
        private class Controller : MonoBehaviour
        {            
            public BasePlayer Player { get; private set; }

            public AnimalController Animal { get; private set; }

            public ConfigData.RidingSettings config;

            private float acceleration = 0f;

            private float steering = 0f;

            private float currentSpeed;

            private float accelerationSpeed;

            private float turnSpeedMultiplier;

            private float maxSpeed;

            private bool sprint = false;

            private Vector3 targetPosition;

            private Vector3 currentPosition;

            private Vector3 steerPos;

            private Vector3 accelPos;

            private Vector3 lookPosition;

            private Quaternion targetRotation;

            private NavMeshHit navHit;

            private void Awake()
            {
                Player = GetComponent<BasePlayer>();
                enabled = false;

                config = ins.configData.Settings;
                InvokeHandler.InvokeRepeating(this, BlockTargeting, 5f, 5f);
            }

            private void BlockTargeting()
            {
                Animal.Npc.BlockEnemyTargeting(5f);
                Animal.Npc.BlockFoodTargeting(5f);

                InvokeHandler.Invoke(this, BlockTargeting, 5f);
            }

            private void Update()
            {
                if (Player == null || Animal == null || Animal.Npc == null)
                    return;

                accelerationSpeed = acceleration > 0 ? (sprint ? Time.deltaTime * 5f : currentSpeed > Animal.stats.Walk ? -Time.deltaTime * 5f : Time.deltaTime * 5f) : currentSpeed > 0 ? -Time.deltaTime * 5f : 0;

                maxSpeed = acceleration > 0 ? (sprint || currentSpeed > Animal.stats.Walk ? Animal.stats.Sprint : Animal.stats.Walk) : sprint ? Animal.stats.Sprint : Animal.stats.Walk;

                currentSpeed = Mathf.Clamp(currentSpeed + accelerationSpeed, 0, maxSpeed);

                turnSpeedMultiplier = currentSpeed / maxSpeed;

                if (acceleration == 0 && steering == 0)
                {
                    if (currentSpeed > 0)
                    {
                        targetPosition = Animal.Transform.position + (Animal.Transform.rotation * (Vector3.forward * currentSpeed));

                        currentPosition = Vector3.Lerp(Animal.Npc.ServerPosition, targetPosition, Time.deltaTime);

                        if (NavMesh.SamplePosition(currentPosition, out navHit, 5f, NavMesh.AllAreas))
                            currentPosition = Vector3.Lerp(currentPosition, navHit.position, Vector3.Distance(currentPosition, navHit.position) / 2f);
                        else currentPosition = Animal.Npc.ServerPosition;

                        Animal.Npc.ServerPosition = currentPosition;
                    }
                    return;
                }

                steerPos = steering == 0 ? Vector3.zero : (Animal.Transform.rotation * (Vector3.right * (sprint && acceleration > 0 ? steering * 5f : steering)));

                accelPos = acceleration == 0 ? Vector3.zero : (Animal.Transform.rotation * (Vector3.forward * currentSpeed));

                targetPosition = Animal.Transform.position + steerPos + (acceleration > 0 ? accelPos : Animal.Transform.forward);

                lookPosition = targetPosition - Animal.Npc.ServerPosition;

                targetRotation = Quaternion.LookRotation(lookPosition);

                Animal.Npc.ServerRotation = Quaternion.Slerp(Animal.Npc.ServerRotation, targetRotation, Time.deltaTime * (sprint ? Mathf.Clamp(5f * turnSpeedMultiplier, 2.5f, 5f) : 2.5f));

                currentPosition = Vector3.Lerp(Animal.Npc.ServerPosition, targetPosition, Time.deltaTime);

                if (NavMesh.SamplePosition(currentPosition, out navHit, 5f, NavMesh.AllAreas))
                {
                    currentPosition = Vector3.Lerp(currentPosition, navHit.position, Vector3.Distance(currentPosition, navHit.position) / 2f);

                    int num = Physics.SphereCastNonAlloc(new Ray(currentPosition, Vector3.up), 1f, raycastHits, 1f, ROCK_MASK);
                    if (num == 0)
                    {
                        Animal.Npc.ServerPosition = currentPosition;
                        return;
                    }
                }

                currentSpeed = 0;
            }

            public void LateUpdate()
            {
                InputState input = Player.serverInput;

                if (input.WasJustPressed(BUTTON.JUMP))
                {
                    enabled = false;
                    Destroy(this);
                    return;
                }

                sprint = input.IsDown(BUTTON.SPRINT);
                acceleration = input.IsDown(BUTTON.FORWARD) ? 1f : 0f;
                steering = input.IsDown(BUTTON.LEFT) ? -1f : input.IsDown(BUTTON.RIGHT) ? 1f : 0f;

                if (input.WasJustPressed(BUTTON.FIRE_THIRD))
                    Player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, !Player.HasPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode));
            }

            public void OnDestroy()
            {
                if (Player != null)
                {
                    if (Player.isMounted)
                        Player.DismountObject();
                    Player.EnsureDismounted();
                    Player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, false);
                    Player.MovePosition(Animal.transform.position + (Animal.transform.right * 1.5f));
                }

                if (ins.controllers.Contains(this))
                    ins.controllers.Remove(this);

                Destroy(Animal);
            }

            public void MountToNpc(BaseAnimalNPC baseNpc)
            {
                Animal = baseNpc.gameObject.AddComponent<AnimalController>();
                Animal.CreateMountable(this);
                Player.MountObject(Animal.Mountable);
                Animal.Npc.Resume();
                enabled = true;

                Player.ChatMessage(msg("Help.Controls1", Player.UserIDString));

                BlockTargeting();

                InvokeHandler.InvokeRepeating(this, UpdatePosition, 0, 2);
            }

            private void UpdatePosition()
            {
                if (Player == null || Animal?.Mountable == null)
                    return;

                Player.transform.position = Animal.Mountable.transform.position;
            }
        }

        private class AnimalController : MonoBehaviour
        {
            public BaseAnimalNPC Npc { get; private set; }

            public BaseMountable Mountable { get; private set; }

            public Controller Human { get; private set; }

            public Transform Transform { get; private set; }

            public ConfigData.SpeedSettings stats;

            private void Awake()
            {
                Npc = GetComponent<BaseAnimalNPC>();
                Transform = Npc.transform;

                Npc.GetNavAgent.autoRepath = false;
                enabled = false;
                Npc.Pause();
                Npc.NewAI = false;

                Npc.CancelInvoke(Npc.TickAi);

                AIThinkManager.RemoveAnimal(Npc);               

                stats = ins.configData.Speeds[Npc.ShortPrefabName];
            }

            public void CreateMountable(Controller controller)
            {
                this.Human = controller;

                Mountable = GameManager.server.CreateEntity(CHAIR_PREFAB, Npc.transform.position, new Quaternion()) as BaseMountable;
                Mountable.enableSaving = false;
                Mountable.isMobile = true;
                Mountable.pickup.enabled = false;
                Mountable.skinID = 1169930802;
                Mountable.maxMountDistance = 1.5f;

                Mountable.Spawn();
                Mountable.isMobile = true;

                Destroy(Mountable.GetComponent<DestroyOnGroundMissing>());
                Destroy(Mountable.GetComponent<GroundWatch>());
                Mountable.GetComponent<MeshCollider>().convex = true;

                Mountable.SetParent(Npc);

                KeyValuePair<Vector3, Vector3> offset;
                if (ins.mountingPositions.TryGetValue(Npc.ShortPrefabName, out offset))
                {
                    Mountable.transform.localPosition = offset.Key;
                    Mountable.transform.localEulerAngles = offset.Value;
                }
            }

            private void OnDestroy()
            {
                if (Mountable != null && !Mountable.IsDestroyed)
                {
                    if (Mountable.IsMounted())
                        Mountable.DismountAllPlayers();

                    Mountable.Kill(BaseNetworkable.DestroyMode.None);
                }

                if (Npc != null && !Npc.IsDestroyed)
                {
                    Npc.StopMoving();
                    Npc.Pause();

                    Npc.Invoke(() =>
                    {
                        if (Npc == null)
                            return;

                        if (Npc.GetComponent<AnimalController>())
                            return;

                        NavMeshHit navHit;
                        if (NavMesh.SamplePosition(Npc.ServerPosition, out navHit, 5f, NavMesh.AllAreas))
                        {
                            Npc.ServerPosition = navHit.position;

                            AIThinkManager.AddAnimal(Npc);

                            Npc.InvokeRandomized(Npc.TickAi, 0.1f, 0.1f, 0.00500000035f);

                            Npc.Resume();
                        }
                        else Npc.DieInstantly();
                    }, 30f);
                }
            }
        }

        private Dictionary<string, KeyValuePair<Vector3, Vector3>> mountingPositions = new Dictionary<string, KeyValuePair<Vector3, Vector3>>
        {
            ["horse"] = new KeyValuePair<Vector3, Vector3>(new Vector3(0, 0.65f, -0.1f), new Vector3(0, 0, 0)),
            ["stag"] = new KeyValuePair<Vector3, Vector3>(new Vector3(0, 0.3f, -0.1f), new Vector3(0, 0, 0)),
            ["chicken"] = new KeyValuePair<Vector3, Vector3>(new Vector3(0, -0.2f, 0), new Vector3(0, 0, 0)),
            ["wolf"] = new KeyValuePair<Vector3, Vector3>(new Vector3(0, 0.05f, 0.1f), new Vector3(0, 0, 0)),
            ["bear"] = new KeyValuePair<Vector3, Vector3>(new Vector3(0, 0.6f, 0), new Vector3(0, 0, 0)),
            ["boar"] = new KeyValuePair<Vector3, Vector3>(new Vector3(0, 0.1f, 0f), new Vector3(0, 0, 0)),
        };
        #endregion

        #region Commands
        [ChatCommand("spawnhorse")]
        private void cmdSpawn(BasePlayer player, string command, string[] args)
        {            
            if (args.Length == 0)
            {
                SendReply(player, msg("Help.Spawn", player.UserIDString));
                SendReply(player, string.Format(msg("Help.AvailableTypes", player.UserIDString), configData.Settings.AllowedTypes.ToSentence()));
                return;
            }

            string type = args[0].ToLower();
            if (!configData.Settings.AllowedTypes.Contains(type))
            {
                SendReply(player, msg("Error.InvalidAnimal", player.UserIDString) + " " + string.Format(msg("Help.AvailableTypes", player.UserIDString), configData.Settings.AllowedTypes.ToSentence()));
                return;
            }

            if (!permission.UserHasPermission(player.UserIDString, "tsunhorse.spawnhorse") && !permission.UserHasPermission(player.UserIDString, $"tsunhorse.spawnhorse.{type}"))
            {
                SendReply(player, msg("Error.NoPermission", player.UserIDString));
                return;
            }

            double remaining = 0;
            double current = CurrentTime();

            StoredData.Cooldown cooldown;

            if (storedData.cooldowns.TryGetValue(player.userID, out cooldown))
            {
                remaining = cooldown.GetValue(true);

                if (remaining > current)
                {
                    double time = remaining - current;
                    SendReply(player, string.Format(msg("Error.Cooldown", player.UserIDString), FormatTime(time)));
                    return;
                }
            }

            if (type == "horse")
            {
                RidableHorse ridableHorse;
                if (!SpawnRideableHorse(player.transform.position + (Quaternion.Euler(0, player.serverInput.current.aimAngles.y, 0) * Vector3.forward), out ridableHorse))
                {
                    SendReply(player, msg("Error.InvalidPosition", player.UserIDString));
                    return;
                }

                ridableHorse.walkSpeed = configData.Speeds["horse"].Walk;
                ridableHorse.runSpeed = configData.Speeds["horse"].Sprint;
            }
            else
            {
                BaseNpc baseNpc;
                if (!SpawnAnimal(type, player.transform.position, out baseNpc))
                {
                    SendReply(player, msg("Error.InvalidPosition", player.UserIDString));
                    return;
                }

                baseNpc.StopMoving();
                baseNpc.Pause();

                Timer t = null;
                t = timer.Repeat(1f, configData.Command.StopTime, () =>
                {
                    if (baseNpc == null || baseNpc.GetComponent<AnimalController>())
                    {
                        t?.Destroy();
                        return;
                    }

                    baseNpc.StopMoving();
                    baseNpc.Pause();
                });
            }

            if (storedData.cooldowns.ContainsKey(player.userID))
                storedData.cooldowns[player.userID].Spawn = current + configData.Command.SpawnCooldown;
            else storedData.cooldowns.Add(player.userID, new StoredData.Cooldown(0, configData.Command.SpawnCooldown));

            SendReply(player, string.Format(msg("Msg.SpawnSuccess", player.UserIDString), type));
        }

        [ChatCommand("stop")]
        private void cmdStop(BasePlayer player, string command, string[] args)
        {
            if (!configData.Command.Command)
                return;

            if (!permission.UserHasPermission(player.UserIDString, "tsunhorse.stophorse"))
            {
                SendReply(player, msg("Error.NoPermission", player.UserIDString));
                return;
            }

            double remaining = 0;
            double current = CurrentTime();

            StoredData.Cooldown cooldown;

            if (storedData.cooldowns.TryGetValue(player.userID, out cooldown))
            {
                remaining = cooldown.GetValue(false);

                if (remaining > current)
                {
                    double time = remaining - current;
                    SendReply(player, string.Format(msg("Error.Cooldown", player.UserIDString), FormatTime(time)));
                    return;
                }
            }
          
            List<BaseNpc> entities = Pool.GetList<BaseNpc>();
            Vis.Entities<BaseNpc>(player.transform.position, configData.Command.StopRadius, entities);
            int count = 0;
            foreach(BaseNpc baseNpc in entities.Distinct())
            {
                if (baseNpc.GetComponent<AnimalController>())
                    continue;

                if (configData.Settings.AllowedTypes.Any(x => x.Contains(baseNpc.ShortPrefabName, CompareOptions.IgnoreCase)))
                {
                    count++;
                    baseNpc.StopMoving();
                    baseNpc.Pause();

                    timer.In(configData.Command.StopTime, () => 
                    {
                        if (baseNpc == null || baseNpc.GetComponent<AnimalController>())                        
                            return;                        
                        
                        baseNpc.Resume();
                    });
                }
            }
            Pool.FreeList(ref entities);
            if (count > 0)
            {
                if (storedData.cooldowns.ContainsKey(player.userID))
                    storedData.cooldowns[player.userID].Stop = current + configData.Command.StopCooldown;
                else storedData.cooldowns.Add(player.userID, new StoredData.Cooldown(configData.Command.StopCooldown, 0));

                SendReply(player, string.Format(msg("Help.StoppedAnimals", player.UserIDString), count));
            }
            else SendReply(player, msg("Help.NoStoppedAnimals", player.UserIDString));
        }

        [ConsoleCommand("spawnhorse")]
        private void ccmdSpawn(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null)
                return;

            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "spawnhorse <type> <x> <z> - Spawn the specified animal at the target coordinates");
                SendReply(arg, "spawnhorse <type> <playername or id> - Spawn the specified animal at the target players position");
                return;
            }

            string type = arg.Args[0].ToLower();
            if (!configData.Settings.AllowedTypes.Contains(type))
            {
                SendReply(arg, "Invalid animal type selected. Options are: " + configData.Settings.AllowedTypes.ToSentence());
                return;
            }

            Vector3 position = Vector3.zero;
            if (arg.Args.Length == 3)
            {
                float x, z;
                if (!float.TryParse(arg.Args[1], out x) || !float.TryParse(arg.Args[2], out z))
                {
                    SendReply(arg, "Invalid co-ordinates set. You must enter number values for X and Z");
                    return;
                }
                else
                {
                    position = new Vector3(x, 0, z);
                    position.y = TerrainMeta.HeightMap.GetHeight(position) + 0.25f;
                }
            }
            else if (arg.Args.Length == 2)
            {
                List<BasePlayer> players = FindPlayer(arg.Args[1]);
                if (players.Count > 1)
                {
                    SendReply(arg, "Multiple players found");
                    return;
                }
                else if (players.Count == 0)
                {
                    SendReply(arg, "No players found");
                    return;
                }
                else position = players[0].transform.position;
            }

            if (position == Vector3.zero)
            {
                SendReply(arg, "Invalid spawn position set");
                return;
            }

            if (type == "horse")
            {
                RidableHorse ridableHorse;
                if (!SpawnRideableHorse(position, out ridableHorse))
                {
                    SendReply(arg, "Unable to spawn a animal at the specified position");
                    return;
                }

                ridableHorse.walkSpeed = configData.Speeds["horse"].Walk;
                ridableHorse.runSpeed = configData.Speeds["horse"].Sprint;
            }
            else
            {
                BaseNpc baseNpc;
                if (!SpawnAnimal(type, position, out baseNpc))
                {
                    SendReply(arg, "Unable to spawn a animal at the specified position");
                    return;
                }

                baseNpc.StopMoving();
                baseNpc.Pause();

                Timer t = null;
                t = timer.Repeat(1f, configData.Command.StopTime, () =>
                {
                    if (baseNpc == null || baseNpc.GetComponent<AnimalController>())
                    {
                        t?.Destroy();
                        return;
                    }

                    baseNpc.StopMoving();
                    baseNpc.Pause();
                });
            }
        }

        private List<BasePlayer> FindPlayer(string arg)
        {
            List<BasePlayer> foundPlayers = new List<BasePlayer>();

            ulong steamid;
            ulong.TryParse(arg, out steamid);

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (steamid != 0L)
                {
                    if (player.userID == steamid)
                    {
                        foundPlayers.Clear();
                        foundPlayers.Add(player);
                        return foundPlayers;
                    }
                }
                if (player.displayName.ToLower().Contains(arg.ToLower()))
                    foundPlayers.Add(player);
            }
            return foundPlayers;
        }
        #endregion

        #region Config        
        private ConfigData configData;
        private class ConfigData
        {
            public RidingSettings Settings { get; set;}
            public CommandSettings Command { get; set; }
            public Dictionary<string, SpeedSettings> Speeds { get; set; }

            public class RidingSettings
            {
                [JsonProperty(PropertyName = "Allowed animal types (Horse, Bear, Boar, Chicken, Stag, Wolf)")]
                public string[] AllowedTypes { get; set; }
                [JsonProperty(PropertyName = "Are animals invincible when being ridden")]
                public bool Invulnerable { get; set; }
                [JsonProperty(PropertyName = "Allow third person toggle (middle mouse button)")]
                public bool ThirdPersonHotkey { get; set; } 
            }

            public class CommandSettings
            {
                [JsonProperty(PropertyName = "Stop command cooldown time (seconds)")]
                public int StopCooldown { get; set; }
                [JsonProperty(PropertyName = "Spawn command cooldown time (seconds)")]
                public int SpawnCooldown { get; set; }
                [JsonProperty(PropertyName = "Enable command to stop animals within radius")]
                public bool Command { get; set; }
                [JsonProperty(PropertyName = "Radius in which to stop nearby animals")]
                public int StopRadius { get; set; }
                [JsonProperty(PropertyName = "Are animals invincible whilst stopped")]
                public bool Invulnerable { get; set; }
                [JsonProperty(PropertyName = "Amount of time animals stay stopped")]
                public int StopTime { get; set; }
            }

            public class SpeedSettings
            {
                [JsonProperty(PropertyName = "Walking speed")]
                public float Walk;
                [JsonProperty(PropertyName = "Sprinting speed")]
                public float Sprint;                
            }
            public Oxide.Core.VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            configData = Config.ReadObject<ConfigData>();

            if (configData.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(configData, true);
        }

        protected override void LoadDefaultConfig() => configData = GetBaseConfig();

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                Command = new ConfigData.CommandSettings
                {
                    Command = true,
                    StopCooldown = 30,
                    SpawnCooldown = 3600,
                    StopRadius = 10,
                    Invulnerable = true,
                    StopTime = 10
                },
                Settings = new ConfigData.RidingSettings
                {
                    AllowedTypes = new string[] { "horse", "chicken" },
                    Invulnerable = true,                    
                    ThirdPersonHotkey = true,
                },
                Speeds = new Dictionary<string, ConfigData.SpeedSettings>
                {
                    ["bear"] = new ConfigData.SpeedSettings
                    {
                        Walk = 1.8f,
                        Sprint = 8f,
                    },
                    ["boar"] = new ConfigData.SpeedSettings
                    {
                        Walk = 1.4f,
                        Sprint = 7f,
                    },
                    ["chicken"] = new ConfigData.SpeedSettings
                    {
                        Walk = 0.4f,
                        Sprint = 2f,
                    },
                    ["horse"] = new ConfigData.SpeedSettings
                    {
                        Walk = 2.4f,
                        Sprint = 12f,
                    },
                    ["stag"] = new ConfigData.SpeedSettings
                    {
                        Walk = 2.2f,
                        Sprint = 11f,
                    },
                    ["wolf"] = new ConfigData.SpeedSettings
                    {
                        Walk = 1.8f,
                        Sprint = 9f,
                    }
                },
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            if (configData.Version < new VersionNumber(3, 0, 1))
            {
                configData.Command.Invulnerable = true;
                configData.Command.StopTime = 10;
                configData.Command.SpawnCooldown = 3600;
                configData.Command.StopCooldown = 30;

                configData.Speeds = baseConfig.Speeds;
            }

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion

        #region Data Management
        private void SaveData() => data.WriteObject(storedData);

        private void LoadData()
        {
            try
            {
                storedData = data.ReadObject<StoredData>();
            }
            catch
            {
                storedData = new StoredData();
            }
        }

        private class StoredData
        {
            public Dictionary<ulong, Cooldown> cooldowns = new Dictionary<ulong, Cooldown>();
            
            public class Cooldown
            {
                public double Stop { get; set; }
                public double Spawn { get; set; }

                public Cooldown() { }

                public Cooldown(double stop, double spawn)
                {
                    this.Stop = stop;
                    this.Spawn = spawn;
                }

                public double GetValue(bool isSpawn) => isSpawn ? Spawn : Stop;
            }
        }
        #endregion

        #region Localization
        private static string msg(string key, string playerId = null) => ins.lang.GetMessage(key, ins, playerId);

        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            ["Help.Controls1"] = "<color=#ce422b>Animal Controls:\nForward</color> - Move Forward\n<color=#ce422b>Left/Right</color> - Turn\n<color=#ce422b>Sprint</color> - Sprint\n<color=#ce422b>Jump</color> - Dismount animal",
            ["Help.StoppedAnimals"] = "You have temporarily stopped <color=#ce422b>{0}</color> animal(s)!",
            ["Help.NoStoppedAnimals"] = "<color=#ce422b>There are no animals nearby...</color>",
            ["Help.Spawn"] = "<color=#ce422b>/spawnhorse <type></color> - Spawn a animal you can ride",
            ["Help.AvailableTypes"] = "Available types are; <color=#ce422b>{0}</color>",
            ["Msg.SpawnSuccess"] = "You have spawned a <color=#ce422b>{0}</color>! You can mount it by looking at it and pressing the <color=#ce422b>USE</color> key",
            ["Error.InvalidPosition"] = "<color=#ce422b>Unable to spawn a animal at this position</color>",
            ["Error.InvalidAnimal"] = "<color=#ce422b>Invalid animal type selected!</color>",
            ["Error.NoPermission"] = "<color=#ce422b>You do not have permission to use this command</color>",
            ["Error.Cooldown"] = "You have a cooldown of <color=#ce422b>{0}</color> remaining",
            ["Warning.InvalidType"] = "<color=#ce422b>That animal is not rideable</color>",
            ["Warning.NearDeath"] = "<color=#ce422b>That animal is nearing death...</color>",
            ["Warning.InUse"] = "<color=#ce422b>That animal already has someone riding it</color>"
        };
        #endregion
    }   
}
