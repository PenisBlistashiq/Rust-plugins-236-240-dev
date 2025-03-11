using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Rust;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("TruePVE", "nivex", "2.0.6")]
    [Description("Improvement of the default Rust PVE behavior")]
    // Thanks to the original author, ignignokt84.
    class TruePVE : RustPlugin
    {
        #region Variables
        private static TruePVE Instance;

        // config/data container
        private Configuration config = new Configuration();

        [PluginReference]
        Plugin ZoneManager, LiteZones, Clans, Friends;

        // usage information string with formatting
        public string usageString;
        // valid commands
        private enum Command { def, sched, trace, usage, enable, sleepers };

        // default values array

        // flags for RuleSets
        [Flags]
        private enum RuleFlags
        {
            None = 0,
            SuicideBlocked = 1,
            AuthorizedDamage = 1 << 1,
            NoHeliDamage = 1 << 2,
            HeliDamageLocked = 1 << 3,
            NoHeliDamagePlayer = 1 << 4,
            HumanNPCDamage = 1 << 5,
            LockedBoxesImmortal = 1 << 6,
            LockedDoorsImmortal = 1 << 7,
            AdminsHurtSleepers = 1 << 8,
            ProtectedSleepers = 1 << 9,
            CupboardOwnership = 1 << 10,
            SelfDamage = 1 << 11,
            TwigDamage = 1 << 12,
            NoHeliDamageQuarry = 1 << 13,
            TrapsIgnorePlayers = 1 << 14,
            TurretsIgnorePlayers = 1 << 15,
            TurretsIgnoreScientist = 1 << 16,
            TrapsIgnoreScientist = 1 << 17,
            SamSitesIgnorePlayers = 1 << 18,
            TwigDamageRequiresOwnership = 1 << 19,
            AuthorizedDamageRequiresOwnership = 1 << 20,
            VehiclesTakeCollisionDamageWithoutDriver = 1 << 21,
            FriendlyFire = 1 << 22,
            AnimalsIgnoreSleepers = 1 << 23,
            NoHeliDamageRidableHorses = 1 << 24,
            NoHeliDamageSleepers = 1 << 25,
            SamSitesIgnoreMLRS = 1 << 26,
        }

        private Timer scheduleUpdateTimer;                              // timer to check for schedule updates        
        private RuleSet currentRuleSet;                                 // current ruleset        
        private string currentBroadcastMessage;                         // current broadcast message        
        private bool useZones;                                          // internal useZones flag        
        private const string Any = "any";                               // constant "any" string for rules        
        private const string AllZones = "allzones";                     // constant "allzones" string for mappings        
        private const string PermCanMap = "truepve.canmap";             // permission for mapping command
        private bool animalsIgnoreSleepers;                             // toggle flag to protect sleepers        
        private bool trace = false;                                     // trace flag        
        private const string traceFile = "ruletrace";                   // tracefile name        
        private const float traceTimeout = 300f;                        // auto-disable trace after 300s (5m)        
        private Timer traceTimer;                                       // trace timeout timer
        private bool tpveEnabled = true;                                // toggle flag for damage handling
        private HashSet<string> _deployables = new HashSet<string>();
        private List<DamageType> damageTypes = new List<DamageType>
        {
            DamageType.Explosion,
            DamageType.Bullet,
            DamageType.Slash,
            DamageType.Stab,
            DamageType.Blunt
        };
        #endregion

        #region Loading/Unloading
        private void Loaded()
        {
            Instance = this;
            LoadDefaultMessages();
            // register console commands automagically
            foreach (Command command in Enum.GetValues(typeof(Command)))
            {
                AddCovalenceCommand($"tpve.{command}", nameof(CommandDelegator));
            }
            // register chat commands
            cmd.AddChatCommand("tpve_prod", this, nameof(HandleProd));
            cmd.AddChatCommand("tpve_enable", this, nameof(EnableToggle));
            cmd.AddChatCommand("tpve", this, nameof(ChatCommandDelegator));

            // build usage string for console (without sizing)
            usageString = WrapColor("orange", GetMessage("Header_Usage")) + "\n" +
                          WrapColor("cyan", $"tpve.{Command.def}") + $" - {GetMessage("Cmd_Usage_def")}{Environment.NewLine}" +
                          WrapColor("cyan", $"tpve.{Command.trace}") + $" - {GetMessage("Cmd_Usage_trace")}{Environment.NewLine}" +
                          WrapColor("cyan", $"tpve.{Command.sched} [enable|disable]") + $" - {GetMessage("Cmd_Usage_sched")}{Environment.NewLine}" +
                          WrapColor("cyan", $"/tpve_prod") + $" - {GetMessage("Cmd_Usage_prod")}{Environment.NewLine}" +
                          WrapColor("cyan", $"/tpve map") + $" - {GetMessage("Cmd_Usage_map")}";
            permission.RegisterPermission(PermCanMap, this);
        }

        private void Unload()
        {
            if (scheduleUpdateTimer != null)
                scheduleUpdateTimer.Destroy();
            Instance = null;
        }

        private void OnPluginLoaded(Plugin plugin)
        {
            if (plugin.Name == "ZoneManager")
                ZoneManager = plugin;
            if (plugin.Name == "LiteZones")
                LiteZones = plugin;
            if (ZoneManager != null || LiteZones != null)
                useZones = config?.options.useZones ?? true;
        }

        private void OnPluginUnloaded(Plugin plugin)
        {
            if (plugin.Name == "ZoneManager")
                ZoneManager = null;
            if (plugin.Name == "LiteZones")
                LiteZones = null;
            if (ZoneManager == null && LiteZones == null)
                useZones = false;
            traceTimer?.Destroy();
        }

        private void Init()
        {
            Unsubscribe(nameof(CanBeTargeted));
            Unsubscribe(nameof(OnEntityTakeDamage));
            Unsubscribe(nameof(OnPlayerConnected));
            //Unsubscribe(nameof(OnSamSiteTargetScan));
            Unsubscribe(nameof(OnSamSiteTarget));
            Unsubscribe(nameof(OnTrapTrigger));
            Unsubscribe(nameof(OnNpcTarget));
        }

        private void OnServerInitialized(bool isStartup)
        {
            // check for server pve setting
            if (ConVar.Server.pve) WarnPve();
            // load configuration
            config.Init();
            currentRuleSet = config.GetDefaultRuleSet();
            if (currentRuleSet == null)
                PrintWarning(GetMessage("Warning_NoRuleSet"), config.defaultRuleSet);
            useZones = config.options.useZones && (LiteZones != null || ZoneManager != null);
            if (useZones && config.mappings.Count == 1 && config.mappings.First().Key.Equals(config.defaultRuleSet))
                useZones = false;
            if (config.schedule.enabled)
                TimerLoop(true);
            if (config.ruleSets.Any(ruleSet => ruleSet.HasFlag(RuleFlags.AnimalsIgnoreSleepers))) Subscribe(nameof(OnNpcTarget));
            if (currentRuleSet == null) return;
            InitializeDeployables();
            Subscribe(nameof(CanBeTargeted));
            Subscribe(nameof(OnEntityTakeDamage));
            Subscribe(nameof(OnPlayerConnected));
            //Subscribe(nameof(OnSamSiteTargetScan));
            Subscribe(nameof(OnSamSiteTarget));
            Subscribe(nameof(OnTrapTrigger));
        }
        #endregion

        #region Command Handling
        // delegation method for console commands
        private void CommandDelegator(IPlayer user, string command, string[] args)
        {
            // return if user doesn't have access to run console command
            if (!user.IsServer && !(user.Object as BasePlayer).IsAdmin) return;

            switch ((Command)Enum.Parse(typeof(Command), command.Replace("tpve.", string.Empty)))
            {
                case Command.sleepers:
                    HandleSleepers(user);
                    return;
                case Command.def:
                    HandleDef(user);
                    return;
                case Command.sched:
                    HandleScheduleSet(user, args);
                    return;
                case Command.trace:
                    trace = !trace;
                    if (!trace)
                    {
                        tracePlayer = null;
                        traceEntity = null;
                    }
                    else tracePlayer = user.Object as BasePlayer;
                    Message(user, "Notify_TraceToggle", new object[] { trace ? "on" : "off" });
                    if (trace)
                    {
                        traceTimer = timer.In(traceTimeout, () => trace = false);
                    }
                    else traceTimer?.Destroy();
                    return;
                case Command.enable:
                    tpveEnabled = !tpveEnabled;
                    Message(user, "Enable", tpveEnabled);
                    return;
                case Command.usage:
                default:
                    ShowUsage(user);
                    return;
            }
        }

        private void HandleSleepers(IPlayer user)
        {
            if (animalsIgnoreSleepers)
            {
                animalsIgnoreSleepers = false;
                if (!config.ruleSets.Any(ruleSet => ruleSet.HasFlag(RuleFlags.AnimalsIgnoreSleepers))) Unsubscribe(nameof(OnNpcTarget));
                user.Reply("Sleepers are no longer protected from animals.");
            }
            else
            {
                animalsIgnoreSleepers = true;
                Subscribe(nameof(OnNpcTarget));
                user.Reply("Sleepers are now protected from animals.");
            }
        }

        private void EnableToggle(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin)
            {
                tpveEnabled = !tpveEnabled;
                Message(player, "Enable", new object[] { tpveEnabled.ToString() });
            }
        }

        // handle setting defaults
        private void HandleDef(IPlayer user)
        {
            config.options = new ConfigurationOptions();
            Message(user, "Notify_DefConfigLoad");
            LoadDefaultData();
            Message(user, "Notify_DefDataLoad");
            SaveConfig();
        }

        // handle prod command (raycast to determine what player is looking at)
        private void HandleProd(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
            {
                Message(player, "Error_NoPermission");
                return;
            }

            object entity;
            if (!GetRaycastTarget(player, out entity) || entity == null)
            {
                SendReply(player, WrapSize(12, WrapColor("red", GetMessage("Error_NoEntityFound", player.UserIDString))));
                return;
            }
            Message(player, "Notify_ProdResult", new object[] { entity.GetType(), (entity as BaseEntity).ShortPrefabName });
        }

        // delegation method for chat commands
        private void ChatCommandDelegator(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin && !permission.UserHasPermission(player.UserIDString, PermCanMap))
            {
                Message(player, "Error_NoPermission");
                return;
            }

            // assume args[0] is the command (beyond /tpve)
            if (args.Length > 0) command = args[0];

            // shift arguments
            args = args.Length > 1 ? args.Skip(1).ToArray() : new string[0];

            if (command != "map")
            {
                Message(player, "Error_InvalidCommand");
            }
            else if (args.Length == 0)
            {
                Message(player, "Error_InvalidParamForCmd", command);
            }
            else
            {
                // args[0] should be mapping name
                // args[1] if exists should be target ruleset or "exclude"
                // if args[1] is empty, delete mapping
                string from = args[0];
                string to = args.Length == 2 ? args[1] : null;

                if (to != null && !config.ruleSets.Select(r => r.name).Contains(to) && to != "exclude")
                {
                    // target ruleset must exist, or be "exclude"
                    Message(player, "Error_InvalidMapping", from, to);
                }
                else
                {
                    bool dirty = false;
                    if (to != null)
                    {
                        dirty = true;
                        if (config.HasMapping(from))
                        {
                            // update existing mapping
                            string old = config.mappings[from];
                            config.mappings[from] = to;
                            Message(player, "Notify_MappingUpdated", from, old, to);
                        }
                        else
                        {
                            // add new mapping
                            config.mappings.Add(from, to);
                            Message(player, "Notify_MappingCreated", from, to);
                        }
                    }
                    else
                    {
                        if (config.HasMapping(from))
                        {
                            dirty = true;
                            // remove mapping
                            string old = config.mappings[from];
                            config.mappings.Remove(from);
                            Message(player, "Notify_MappingDeleted", from, old);
                        }
                        else
                        {
                            Message(player, "Error_NoMappingToDelete", from);
                        }
                    }

                    if (dirty)
                    {
                        SaveConfig(); // save changes to config file
                    }
                }
            }
        }

        // handles schedule enable/disable
        private void HandleScheduleSet(IPlayer user, string[] args)
        {
            if (args.Length == 0)
            {
                Message(user, "Error_InvalidParamForCmd");
                return;
            }
            if (!config.schedule.valid)
            {
                Message(user, "Notify_InvalidSchedule");
            }
            else if (args[0] == "enable")
            {
                if (config.schedule.enabled) return;
                config.schedule.enabled = true;
                TimerLoop();
                Message(user, "Notify_SchedSetEnabled");
            }
            else if (args[0] == "disable")
            {
                if (!config.schedule.enabled) return;
                config.schedule.enabled = false;
                if (scheduleUpdateTimer != null)
                    scheduleUpdateTimer.Destroy();
                Message(user, "Notify_SchedSetDisabled");
            }
            else
            {
                Message(user, "Error_InvalidParameter", args[0]);
            }
        }
        #endregion

        #region Configuration/Data

        // load config
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
                CheckData();
                SaveConfig();
            }
            catch (Exception ex)
            {
                Puts(ex.Message);
                LoadDefaultConfig();
                return;
            }

            // check config version, update version to current version
            if (config.configVersion == null || !config.configVersion.Equals(Version.ToString()))
            {
                config.configVersion = Version.ToString();
            }
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration
            {
                configVersion = Version.ToString(),
                options = new ConfigurationOptions()
            };
            LoadDefaultData();
        }

        // save data
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        // check rulesets and groups
        private bool CheckData()
        {
            bool dirty = false;
            if ((config.ruleSets == null || config.ruleSets.Count == 0) || (config.groups == null || config.groups.Count == 0))
            {
                dirty = LoadDefaultData();
            }
            if (config.schedule == null)
            {
                config.schedule = new Schedule();
                dirty = true;
            }
            dirty |= CheckMappings();
            return dirty;
        }

        // rebuild mappings
        private bool CheckMappings()
        {
            bool dirty = false;
            foreach (RuleSet rs in config.ruleSets)
            {
                if (!config.mappings.ContainsValue(rs.name))
                {
                    config.mappings[rs.name] = rs.name;
                    dirty = true;
                }
            }
            return dirty;
        }

        // load default data to mappings, rulesets, and groups
        private bool LoadDefaultData()
        {
            config.mappings.Clear();
            config.ruleSets.Clear();
            config.groups.Clear();
            config.schedule = new Schedule();
            config.defaultRuleSet = "default";

            // build groups first
            config.groups.Add(new EntityGroup("barricades")
            {
                members = "Barricade, icewall, GraveYardFence", // "barricade.cover.wood, door_barricade_a, door_barricade_a_large, door_barricade_b, door_barricade_dbl_a, door_barricade_dbl_a_large, door_barricade_dbl_b, door_barricade_dbl_b_large",
                exclusions = "barricade.concrete, barricade.sandbags, barricade.metal, barricade.stone, barricade.wood, barricade.woodwire"
            });

            config.groups.Add(new EntityGroup("dispensers")
            {
                members = "BaseCorpse, HelicopterDebris, PlayerCorpse, NPCPlayerCorpse, HorseCorpse"
            });

            config.groups.Add(new EntityGroup("fire")
            {
                members = "FireBall, FlameExplosive, FlameThrower, BaseOven, FlameTurret, rocket_heli_napalm, napalm, oilfireball2"
            });

            config.groups.Add(new EntityGroup("guards")
            {
                members = "bandit_guard, scientistpeacekeeper, sentry.scientist.static"
            });

            config.groups.Add(new EntityGroup("heli")
            {
                members = "BaseHelicopter"
            });

            config.groups.Add(new EntityGroup("highwalls")
            {
                members = "SimpleBuildingBlock, wall.external.high.ice, gates.external.high.stone, gates.external.high.wood"
            });

            config.groups.Add(new EntityGroup("ridablehorses")
            {
                members = "RidableHorse"
            });

            config.groups.Add(new EntityGroup("cars")
            {
                members = "BasicCar, ModularCar, BaseModularVehicle, BaseVehicleModule, VehicleModuleEngine, VehicleModuleSeating, VehicleModuleStorage, VehicleModuleTaxi, ModularCarSeat"
            });

            config.groups.Add(new EntityGroup("mini")
            {
                members = "minicopter.entity"
            });

            config.groups.Add(new EntityGroup("scrapheli")
            {
                members = "ScrapTransportHelicopter"
            });

            config.groups.Add(new EntityGroup("ch47")
            {
                members = "ch47.entity"
            });

            config.groups.Add(new EntityGroup("npcs")
            {
                members = "ch47scientists.entity, BradleyAPC, HumanNPC, NPCPlayer, ScientistNPC, TunnelDweller, SimpleShark, UnderwaterDweller, Zombie, ZombieNPC"
            });

            config.groups.Add(new EntityGroup("players")
            {
                members = "BasePlayer, FrankensteinPet"
            });

            config.groups.Add(new EntityGroup("resources")
            {
                members = "ResourceEntity, TreeEntity, OreResourceEntity, LootContainer",
                exclusions = "hobobarrel.deployed"
            });

            config.groups.Add(new EntityGroup("samsites")
            {
                members = "sam_site_turret_deployed",
                exclusions = "sam_static"
            });

            config.groups.Add(new EntityGroup("traps")
            {
                members = "AutoTurret, BearTrap, FlameTurret, Landmine, GunTrap, ReactiveTarget, TeslaCoil, spikes.floor"
            });

            config.groups.Add(new EntityGroup("junkyard")
            {
                members = "magnetcrane.entity, carshredder.entity"
            });

            // create default ruleset
            RuleSet defaultRuleSet = new RuleSet(config.defaultRuleSet)
            {
                _flags = RuleFlags.HumanNPCDamage | RuleFlags.LockedBoxesImmortal | RuleFlags.LockedDoorsImmortal | RuleFlags.SamSitesIgnorePlayers | RuleFlags.TrapsIgnorePlayers | RuleFlags.TurretsIgnorePlayers,
                flags = "HumanNPCDamage, LockedBoxesImmortal, LockedDoorsImmortal, SamSitesIgnorePlayers, TrapsIgnorePlayers, TurretsIgnorePlayers"
            };

            // create rules and add to ruleset
            defaultRuleSet.AddRule("anything can hurt dispensers");
            defaultRuleSet.AddRule("anything can hurt resources");
            defaultRuleSet.AddRule("anything can hurt barricades");
            defaultRuleSet.AddRule("anything can hurt traps");
            defaultRuleSet.AddRule("anything can hurt heli");
            defaultRuleSet.AddRule("anything can hurt npcs");
            defaultRuleSet.AddRule("anything can hurt players");
            defaultRuleSet.AddRule("nothing can hurt ch47");
            defaultRuleSet.AddRule("nothing can hurt cars");
            defaultRuleSet.AddRule("nothing can hurt mini");
            //defaultRuleSet.AddRule("nothing can hurt guards");
            defaultRuleSet.AddRule("nothing can hurt ridablehorses");
            defaultRuleSet.AddRule("cars cannot hurt anything");
            defaultRuleSet.AddRule("mini cannot hurt anything");
            defaultRuleSet.AddRule("ch47 cannot hurt anything");
            defaultRuleSet.AddRule("scrapheli cannot hurt anything");
            defaultRuleSet.AddRule("players cannot hurt players");
            defaultRuleSet.AddRule("players cannot hurt traps");
            defaultRuleSet.AddRule("guards cannot hurt players");
            defaultRuleSet.AddRule("fire cannot hurt players");
            defaultRuleSet.AddRule("traps cannot hurt players");
            defaultRuleSet.AddRule("highwalls cannot hurt players");
            defaultRuleSet.AddRule("barricades cannot hurt players");
            defaultRuleSet.AddRule("mini cannot hurt mini");
            defaultRuleSet.AddRule("npcs can hurt players");
            defaultRuleSet.AddRule("junkyard cannot hurt anything");
            defaultRuleSet.AddRule("junkyard can hurt cars");

            config.ruleSets.Add(defaultRuleSet); // add ruleset to rulesets list

            config.mappings[config.defaultRuleSet] = config.defaultRuleSet; // create mapping for ruleset

            return true;
        }

        private bool ResetRules(string key)
        {
            if (string.IsNullOrEmpty(key) || config == null)
            {
                return false;
            }

            string old = config.defaultRuleSet;

            config.defaultRuleSet = key;
            currentRuleSet = config.GetDefaultRuleSet();

            if (currentRuleSet == null)
            {
                config.defaultRuleSet = old;
                currentRuleSet = config.GetDefaultRuleSet();
                return false;
            }

            return true;
        }
        #endregion

        #region Trace
        private BaseEntity traceEntity;
        private BasePlayer tracePlayer;

        private void Trace(string message, int indentation = 0)
        {
            if (config.options.PlayerConsole || config.options.ServerConsole)
            {
                if (traceEntity != null && !traceEntity.IsDestroyed && tracePlayer != null && tracePlayer.IsConnected)
                {
                    if (config.options.MaxTraceDistance == 0 || tracePlayer.Distance(traceEntity) <= config.options.MaxTraceDistance)
                    {
                        if (config.options.PlayerConsole)
                        {
                            tracePlayer.ConsoleMessage(message);
                        }

                        if (config.options.ServerConsole)
                        {
                            Puts(message);
                        }

                        _tsb.AppendLine(string.Empty.PadLeft(indentation, ' ') + message);
                    }
                }
            }
            else _tsb.AppendLine(string.Empty.PadLeft(indentation, ' ') + message);
        }

        private void LogTrace()
        {
            var text = _tsb.ToString();
            traceEntity = null;
            try
            {
                LogToFile(traceFile, text, this);
            }
            catch (IOException)
            {
                timer.Once(1f, () => LogToFile(traceFile, text, this));
                return;
            }
            _tsb.Length = 0;
        }
        private StringBuilder _tsb = new StringBuilder();
        #endregion Trace

        #region Hooks/Handler Procedures
        private void OnPlayerConnected(BasePlayer player)
        {
            if (config.schedule.enabled && config.schedule.broadcast && currentBroadcastMessage != null)
            {
                SendReply(player, GetMessage("Prefix") + currentBroadcastMessage);
            }
        }

        private string CurrentRuleSetName() => currentRuleSet.name;
        private bool IsEnabled() => tpveEnabled;

        // handle damage - if another mod must override TruePVE damages or take priority,
        // set handleDamage to false and reference HandleDamage from the other mod(s)
        private object OnEntityTakeDamage(ResourceEntity entity, HitInfo hitInfo)
        {
            // if default global is not enabled, return true (allow all damage)
            if (hitInfo == null || currentRuleSet == null || currentRuleSet.IsEmpty() || !currentRuleSet.enabled)
            {
                return null;
            }

            // get entity and initiator locations (zones)
            List<string> entityLocations = GetLocationKeys(entity);
            List<string> initiatorLocations = GetLocationKeys(hitInfo.Initiator);
            // check for exclusion zones (zones with no rules mapped)
            if (CheckExclusion(entityLocations, initiatorLocations, trace))
            {
                if (trace) Trace("Exclusion found; allow and return", 1);
                return null;
            }

            if (trace) Trace("No exclusion found - looking up RuleSet...", 1);
            // process location rules
            RuleSet ruleSet = GetRuleSet(entityLocations, initiatorLocations);

            return EvaluateRules(entity, hitInfo, ruleSet) ? (object)null : true;
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity == null || hitInfo == null || AllowKillingSleepers(entity, hitInfo))
            {
                return null;
            }

            object extCanTakeDamage = Interface.CallHook("CanEntityTakeDamage", new object[] { entity, hitInfo });

            if (extCanTakeDamage is bool)
            {
                if ((bool)extCanTakeDamage)
                {
                    return null;
                }

                CancelHit(hitInfo);
                return true;
            }

            if (!config.options.handleDamage)
            {
                return null;
            }

            var majority = hitInfo.damageTypes.GetMajorityDamageType();

            if (majority == DamageType.Decay || majority == DamageType.Fall)
            {
                return null;
            }

            if (!AllowDamage(entity, hitInfo))
            {
                if (trace) LogTrace();
                CancelHit(hitInfo);
                return true;
            }

            if (trace) LogTrace();
            return null;
        }

        private void CancelHit(HitInfo hitInfo)
        {
            hitInfo.damageTypes = new DamageTypeList();
            hitInfo.DidHit = false;
            hitInfo.DoHitEffects = false;
        }

        private bool AllowKillingSleepers(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (animalsIgnoreSleepers && hitInfo.Initiator is BaseNpc) return false;

            return config.AllowKillingSleepers && entity is BasePlayer && entity.ToPlayer().IsSleeping();
        }

        // determines if an entity is "allowed" to take damage
        private bool AllowDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (trace)
            {
                traceEntity = entity;
                _tsb.Length = 0;
            }

            // if default global is not enabled or entity is npc, allow all damage
            if (currentRuleSet == null || currentRuleSet.IsEmpty() || !currentRuleSet.enabled || entity is BaseNpc || entity is BaseHelicopter)
            {
                return true;
            }

            // allow damage to door barricades and covers
            if (entity is Barricade && (entity.ShortPrefabName.Contains("door_barricade") || entity.ShortPrefabName.Contains("cover")))
            {
                return true;
            }

            // if entity is a barrel, trash can, or giftbox, allow damage (exclude water and hobo barrels)
            if (!entity.ShortPrefabName.Equals("waterbarrel") && entity.prefabID != 1748062128)
            {
                if (entity.ShortPrefabName.Contains("barrel") || entity.ShortPrefabName.Equals("loot_trash") || entity.ShortPrefabName.Equals("giftbox_loot"))
                {
                    return true;
                }
            }

            var weapon = hitInfo.Initiator ?? hitInfo.Weapon ?? hitInfo.WeaponPrefab;

            //hitInfo.ProjectilePrefab.sourceWeaponPrefab
            if (trace)
            {
                // Sometimes the initiator is not the attacker (turrets)
                Trace("======================" + Environment.NewLine +
                  "==  STARTING TRACE  ==" + Environment.NewLine +
                  "==  " + DateTime.Now.ToString("HH:mm:ss.fffff") + "  ==" + Environment.NewLine +
                  "======================");
                Trace($"From: {weapon?.GetType().Name ?? "Unknown_Weapon"}, {weapon?.ShortPrefabName ?? "Unknown_Prefab"}", 1);
                Trace($"To: {entity.GetType().Name}, {entity.ShortPrefabName}", 1);
            }

            // get entity and initiator locations (zones)
            List<string> entityLocations = GetLocationKeys(entity);
            List<string> initiatorLocations = GetLocationKeys(weapon);
            // check for exclusion zones (zones with no rules mapped)
            if (CheckExclusion(entityLocations, initiatorLocations, trace))
            {
                if (trace) Trace("Exclusion found; allow and return", 1);
                return true;
            }

            if (trace) Trace("No exclusion found - looking up RuleSet...", 1);

            // process location rules
            RuleSet ruleSet = GetRuleSet(entityLocations, initiatorLocations);

            if (trace) Trace($"Using RuleSet \"{ruleSet.name}\"", 1);

            if (weapon?.ShortPrefabName == "maincannonshell" || weapon is BradleyAPC)
            {
                if (trace) Trace("Initiator is BradleyAPC; evaluating RuleSet rules...", 1);
                return EvaluateRules(entity, weapon, ruleSet);
            }

            if (ruleSet.HasFlag(RuleFlags.VehiclesTakeCollisionDamageWithoutDriver) && entity is BaseMountable && weapon == entity)
            {
                BaseVehicle vehicle = entity.HasParent() ? (entity as BaseMountable).VehicleParent() : entity as BaseVehicle;

                if (vehicle.IsValid())
                {
                    var player = vehicle.GetDriver();

                    if (trace) Trace($"Vehicle collision: { (player == null ? "No driver; allow and return" : "Has driver; continue checks") }", 1);

                    if (player == null)
                    {
                        return true;
                    }
                }
            }

            // Check storage containers and doors for locks
            if (ruleSet.HasFlag(RuleFlags.LockedBoxesImmortal) && entity is StorageContainer || ruleSet.HasFlag(RuleFlags.LockedDoorsImmortal) && entity is Door)
            {
                if (!(entity is LootContainer))
                {
                    // check for lock
                    object hurt = CheckLock(ruleSet, entity, hitInfo);
                    if (trace) Trace($"Door/StorageContainer detected with immortal flag; lock check results: { (hurt == null ? "null (no lock or unlocked); continue checks" : (bool)hurt ? "allow and return" : "block and return") }", 1);
                    if (hurt is bool)
                    {
                        return (bool)hurt;
                    }
                }
            }

            // check heli and turret
            object heli = CheckHeliInitiator(ruleSet, hitInfo);
            bool isVictim = entity is BasePlayer;

            if (heli is bool)
            {
                return HandleHelicopter(ruleSet, entity, heli, isVictim);
            }

            if (weapon is BaseProjectile && hitInfo.Initiator == null)
            {
                var projectile = weapon as BaseProjectile;

                hitInfo.Initiator = projectile.GetOwnerPlayer();
            }

            // after heli check, return true if initiator is null
            if (hitInfo.Initiator == null)
            {
                if (hitInfo.damageTypes.types.Any(type => damageTypes.Contains((DamageType)type)))
                {
                    if (trace) Trace($"Initiator empty for player damage; block and return", 1);
                    return false;
                }
                if (trace) Trace($"Initiator empty; allow and return", 1);
                return true;
            }

            if (ruleSet.HasFlag(RuleFlags.SamSitesIgnorePlayers) && hitInfo.Initiator is SamSite && !entity.IsNpc)
            {
                var player = isVictim ? entity as BasePlayer : GetMountedPlayer(entity as BaseMountable);

                if (player.IsValid() && player.userID.IsSteamId())
                {
                    // check for exclusions in entity groups
                    bool hasExclusion = CheckExclusion(hitInfo.Initiator);
                    if (trace) Trace($"Initiator is samsite, and target is player; {(hasExclusion ? "exclusion found; allow and return" : "no exclusion; block and return")}", 1);
                    return hasExclusion;
                }
            }

            var victim = entity as BasePlayer;

            // handle suicide
            if (isVictim && !entity.IsNpc && victim.userID.IsSteamId() && hitInfo.damageTypes?.Get(DamageType.Suicide) > 0)
            {
                if (trace) Trace($"DamageType is suicide; blocked? { (ruleSet.HasFlag(RuleFlags.SuicideBlocked) ? "true; block and return" : "false; allow and return") }", 1);
                if (ruleSet.HasFlag(RuleFlags.SuicideBlocked))
                {
                    Message(entity as BasePlayer, "Error_NoSuicide");
                    return false;
                }
                return true;
            }

            // allow players to hurt themselves
            if (isVictim && ruleSet.HasFlag(RuleFlags.SelfDamage) && !entity.IsNpc && victim.userID.IsSteamId() && hitInfo.Initiator == entity)
            {
                if (trace) Trace($"SelfDamage flag; player inflicted damage to self; allow and return", 1);
                return true;
            }

            if (hitInfo.Initiator is MiniCopter && entity is BuildingBlock)
            {
                if (trace) Trace("Initiator is minicopter, target is building; evaluate and return", 1);
                return EvaluateRules(entity, hitInfo, ruleSet);
            }

            if (hitInfo.Initiator is BaseNpc)
            {
                // check for sleeper protection - return false if sleeper protection is on (true)
                if (isVictim && ruleSet.HasFlag(RuleFlags.ProtectedSleepers) && (entity as BasePlayer).IsSleeping())
                {
                    if (trace) Trace("Target is sleeping player, with ProtectedSleepers flag set; block and return", 1);
                    return false;
                }

                if (trace) Trace("Initiator is NPC animal; allow and return", 1);
                return true; // allow NPC damage to other entities if sleeper protection is off
            }

            var attacker = hitInfo.Initiator as BasePlayer;

            if (attacker.IsValid())
            {
                if (attacker.isMounted)
                {
                    if (!EvaluateRules(entity, attacker.GetMounted(), ruleSet, false))
                    {
                        return false;
                    }
                }

                if (entity is AdvancedChristmasLights)
                {
                    if (entity.OwnerID == 0)
                    {
                        return attacker.CanBuild();
                    }
                }
                else if (entity is GrowableEntity)
                {
                    if (attacker.CanBuild())
                    {
                        return true;
                    }

                    var ge = entity as GrowableEntity;
                    var planter = ge.GetPlanter();

                    return planter == null || !planter.OwnerID.IsSteamId() || planter.OwnerID == attacker.userID;
                }
                else if (entity is BuildingBlock)
                {
                    var block = entity as BuildingBlock;

                    if (block.grade == BuildingGrade.Enum.Twigs)
                    {
                        if (ruleSet.HasFlag(RuleFlags.TwigDamageRequiresOwnership)) // Allow twig damage by owner or anyone authed if ruleset flag is set
                        {
                            if (entity.OwnerID == attacker.userID)
                            {
                                return true;
                            }

                            return attacker.IsBuildingAuthed(entity.transform.position, entity.transform.rotation, entity.bounds);
                        }

                        if (ruleSet.HasFlag(RuleFlags.TwigDamage)) // Allow twig damage by anyone if ruleset flag is set
                        {
                            return true;
                        }
                    }
                }

                if (isVictim)
                {
                    if (ruleSet.HasFlag(RuleFlags.FriendlyFire) && victim.userID.IsSteamId() && AreAllies(attacker, victim))
                    {
                        if (trace) Trace("Initiator and target are allied players, with FriendlyFire flag set; allow and return", 1);
                        return true;
                    }

                    // allow sleeper damage by admins if configured
                    if (ruleSet.HasFlag(RuleFlags.AdminsHurtSleepers) && attacker.IsAdmin && victim.IsSleeping())
                    {
                        if (trace) Trace("Initiator is admin player and target is sleeping player, with AdminsHurtSleepers flag set; allow and return", 1);
                        return true;
                    }

                    // allow Human NPC damage if configured
                    if (ruleSet.HasFlag(RuleFlags.HumanNPCDamage) && IsHumanNPC(attacker, victim))
                    {
                        if (trace) Trace("Initiator or target is HumanNPC, with HumanNPCDamage flag set; allow and return", 1);
                        return true;
                    }
                }
                else if (ruleSet.HasFlag(RuleFlags.AuthorizedDamage) && !entity.IsNpc) // ignore checks if authorized damage enabled (except for players and npcs)
                {
                    if (ruleSet.HasFlag(RuleFlags.AuthorizedDamageRequiresOwnership) && entity.OwnerID != attacker.userID && CanAuthorize(entity, attacker, ruleSet))
                    {
                        if (trace) Trace("Initiator is player who does not own non-player target; block and return", 1);
                        return false;
                    }

                    if (CheckAuthorized(entity, attacker, ruleSet))
                    {
                        if (entity is SamSite)
                        {
                            if (trace) Trace("Target is SamSite; evaluate and return", 1);
                            return EvaluateRules(entity, hitInfo, ruleSet);
                        }
                        if (trace) Trace("Initiator is player with authorization over non-player target; allow and return", 1);
                        return true;
                    }
                }
            }

            if (trace) Trace("No match in pre-checks; evaluating RuleSet rules...", 1);
            return EvaluateRules(entity, hitInfo, ruleSet);
        }

        private bool HandleHelicopter(RuleSet ruleSet, BaseCombatEntity entity, object heli, bool isVictim)
        {
            if (isVictim)
            {
                var victim = entity as BasePlayer;

                if (ruleSet.HasFlag(RuleFlags.NoHeliDamageSleepers))
                {
                    if (trace) Trace($"Initiator is heli, and target is player; flag check results: { (victim.IsSleeping() ? "victim is sleeping; block and return" : "victim is not sleeping; continue checks") }", 1);
                    if (victim.IsSleeping()) return false;
                }

                if (trace) Trace($"Initiator is heli, and target is player; flag check results: { (ruleSet.HasFlag(RuleFlags.NoHeliDamagePlayer) ? "flag set; block and return" : "flag not set; block and return") }", 1);
                return !ruleSet.HasFlag(RuleFlags.NoHeliDamagePlayer);
            }
            if (entity is MiningQuarry)
            {
                if (trace) Trace($"Initiator is heli, and target is quarry; flag check results: { (ruleSet.HasFlag(RuleFlags.NoHeliDamageQuarry) ? "flag set; block and return" : "flag not set; allow and return") }", 1);
                return !ruleSet.HasFlag(RuleFlags.NoHeliDamageQuarry);
            }
            if (entity is RidableHorse)
            {
                if (trace) Trace($"Initiator is heli, and target is ridablehorse; flag check results: { (ruleSet.HasFlag(RuleFlags.NoHeliDamageRidableHorses) ? "flag set; block and return" : "flag not set; allow and return") }", 1);
                return !ruleSet.HasFlag(RuleFlags.NoHeliDamageRidableHorses);
            }
            if (trace) Trace($"Initiator is heli, target is non-player; results: { ((bool)heli ? "allow and return" : "block and return") }", 1);
            //return EvaluateRules(entity, weapon, ruleSet);
            return (bool)heli;
        }

        public bool AreAllies(BasePlayer attacker, BasePlayer victim)
        {
            if (attacker.currentTeam != 0uL && attacker.Team.members.Contains(victim.userID))
            {
                return true;
            }

            if (Clans != null && Convert.ToBoolean(Clans?.Call("IsMemberOrAlly", attacker.userID, victim.userID)))
            {
                return true;
            }

            if (Friends != null && Convert.ToBoolean(Friends?.Call("AreFriends", attacker.UserIDString, victim.UserIDString)))
            {
                return true;
            }

            return false;
        }

        private const uint WALL_LOW_JPIPE = 310235277;

        private bool CanAuthorize(BaseEntity entity, BasePlayer attacker, RuleSet ruleSet)
        {
            if (entity is BaseVehicle && !EvaluateRules(entity, attacker, ruleSet, false))
            {
                return false;
            }

            if (entity.OwnerID == 0)
            {
                return entity is MiniCopter || entity.prefabID == WALL_LOW_JPIPE && !entity.enableSaving;
            }

            return entity is BuildingBlock || entity.PrefabName.Contains("building") || _deployables.Contains(entity.PrefabName);
        }

        private void InitializeDeployables()
        {
            foreach (var def in ItemManager.GetItemDefinitions())
            {
                var imd = def.GetComponent<ItemModDeployable>();
                if (imd == null || _deployables.Contains(imd.entityPrefab.resourcePath)) continue;
                _deployables.Add(imd.entityPrefab.resourcePath);
            }
        }

        // process rules to determine whether to allow damage
        private bool EvaluateRules(BaseEntity entity, BaseEntity attacker, RuleSet ruleSet, bool returnDefaultValue = true)
        {
            List<string> e0Groups = config.ResolveEntityGroups(attacker);
            List<string> e1Groups = config.ResolveEntityGroups(entity);

            if (trace)
            {
                Trace($"Initator EntityGroup matches: { (e0Groups == null || e0Groups.Count == 0 ? "none" : string.Join(", ", e0Groups.ToArray())) }", 2);
                Trace($"Target EntityGroup matches: { (e1Groups == null || e1Groups.Count == 0 ? "none" : string.Join(", ", e1Groups.ToArray())) }", 2);
            }

            return ruleSet.Evaluate(e0Groups, e1Groups, attacker, returnDefaultValue);
        }

        private bool EvaluateRules(BaseEntity entity, HitInfo hitInfo, RuleSet ruleSet)
        {
            return EvaluateRules(entity, hitInfo.Initiator ?? hitInfo.WeaponPrefab, ruleSet);
        }

        // checks an entity to see if it has a lock
        private object CheckLock(RuleSet ruleSet, BaseEntity entity, HitInfo hitInfo)
        {
            var slot = entity.GetSlot(BaseEntity.Slot.Lock); // check for lock

            if (slot == null || !slot.IsLocked())
            {
                return null; // no lock or unlocked, continue checks
            }

            // if HeliDamageLocked flag is false or NoHeliDamage flag, all damage is cancelled from immortal flag
            if (!ruleSet.HasFlag(RuleFlags.HeliDamageLocked) || ruleSet.HasFlag(RuleFlags.NoHeliDamage))
            {
                return false;
            }

            object heli = CheckHeliInitiator(ruleSet, hitInfo);

            return Convert.ToBoolean(heli); // cancel damage except from heli
        }

        private object CheckHeliInitiator(RuleSet ruleSet, HitInfo hitInfo)
        {
            // Check for heli initiator
            if (hitInfo.Initiator is BaseHelicopter || (hitInfo.Initiator != null && (hitInfo.Initiator.ShortPrefabName.Equals("oilfireballsmall") || hitInfo.Initiator.ShortPrefabName.Equals("napalm"))))
            {
                return !ruleSet.HasFlag(RuleFlags.NoHeliDamage);
            }
            else if (hitInfo.WeaponPrefab != null && (hitInfo.WeaponPrefab.ShortPrefabName.Equals("rocket_heli") || hitInfo.WeaponPrefab.ShortPrefabName.Equals("rocket_heli_napalm")))
            {
                return !ruleSet.HasFlag(RuleFlags.NoHeliDamage);
            }
            return null;
        }

        // checks if the player is authorized to damage the entity
        private bool CheckAuthorized(BaseEntity entity, BasePlayer player, RuleSet ruleSet)
        {
            if (!ruleSet.HasFlag(RuleFlags.CupboardOwnership))
            {
                if (entity.OwnerID == player.userID || entity.OwnerID == 0)
                {
                    return true; // allow damage to entities that the player owns
                }

                return player.IsBuildingAuthed(entity.WorldSpaceBounds());
            }

            // treat entities outside of cupboard range as unowned, and entities inside cupboard range require authorization
            return player.CanBuild(entity.WorldSpaceBounds());
        }

        private bool IsFunTurret(bool isAutoTurret, BaseEntity entity)
        {
            if (!isAutoTurret)
            {
                return false;
            }

            var weapon = (entity as AutoTurret).GetAttachedWeapon()?.GetItem();

            if (weapon?.info?.shortname?.StartsWith("fun.") ?? false)
            {
                return true;
            }

            return false;
        }

        private BasePlayer GetMountedPlayer(BaseMountable m)
        {
            if (m == null || m.IsDestroyed)
            {
                return null;
            }

            if (m.GetMounted())
            {
                return m.GetMounted();
            }

            BaseVehicle vehicle = m.HasParent() ? m.VehicleParent() : m as BaseVehicle;

            if (vehicle.IsValid())
            {
                foreach (var point in vehicle.mountPoints)
                {
                    if (point.mountable.IsValid() && point.mountable.GetMounted())
                    {
                        return point.mountable.GetMounted();
                    }
                }
            }

            return null;
        }

        private BasePlayer GetMountedPlayer(BaseCombatEntity entity)
        {
            var players = Pool.GetList<BasePlayer>();

            Vis.Entities(entity.transform.position, 3f, players);

            var player = players.FirstOrDefault();

            Pool.FreeList(ref players);

            return player;
        }

        private object OnSamSiteTarget(SamSite ss, BaseCombatEntity m)
        {
            var entity = (m is BaseMountable ? GetMountedPlayer(m as BaseMountable) : GetMountedPlayer(m)) ?? m;
            object extCanEntityBeTargeted = Interface.CallHook("CanEntityBeTargeted", new object[] { entity, ss });

            if (extCanEntityBeTargeted is bool)
            {
                if ((bool)extCanEntityBeTargeted)
                {
                    if (trace) Trace($"CanEntityBeTargeted allowed {entity.ShortPrefabName} to be targetted by SamSite", 1);
                    return null;
                }

                if (trace) Trace($"CanEntityBeTargeted blocked {entity.ShortPrefabName} from being targetted by SamSite", 1);
                ss.CancelInvoke(ss.WeaponTick);
                return false;
            }

            RuleSet ruleSet = GetRuleSet(entity, ss);

            if (ruleSet == null)
            {
                if (trace) Trace($"OnSamSiteTarget allowed {entity.ShortPrefabName} to be targetted; no ruleset found.", 1);
                return null;
            }

            if (ruleSet.HasFlag(RuleFlags.SamSitesIgnorePlayers) && entity is BasePlayer || ruleSet.HasFlag(RuleFlags.SamSitesIgnoreMLRS) && entity is MLRS)
            {
                var entityLocations = GetLocationKeys(entity);
                var initiatorLocations = GetLocationKeys(ss);

                // check for exclusion zones (zones with no rules mapped)
                if (CheckExclusion(entityLocations, initiatorLocations, false))
                {
                    if (trace) Trace($"OnSamSiteTarget allowed {entity.ShortPrefabName} to be targetted; exclusion of zone found.", 1);
                    return null;
                }

                // check for exclusions in entity groups
                if (CheckExclusion(ss))
                {
                    if (trace) Trace($"OnSamSiteTarget allowed {entity.ShortPrefabName} to be targetted; exclusion found in entity group.", 1);
                    return null;
                }

                if (trace && entity is BasePlayer) Trace($"SamSitesIgnorePlayers blocked {entity.ShortPrefabName} from being targetted.", 1);
                else if (trace && entity is MLRS) Trace($"SamSitesIgnoreMLRS blocked {entity.ShortPrefabName} from being targetted.", 1);
                ss.CancelInvoke(ss.WeaponTick);
                return false;
            }

            return null;
        }

        private object CanBeTargeted(BaseMountable m, MonoBehaviour mb)
        {
            var player = GetMountedPlayer(m);

            return CanBeTargeted(player, mb);
        }

        // check if entity can be targeted
        private object CanBeTargeted(BasePlayer target, MonoBehaviour mb)
        {
            //if (trace) Trace($"CanBeTargeted called for {target.name}", 2);
            if (target == null || mb == null || mb is HelicopterTurret)
            {
                return null;
            }

            var entity = mb as BaseEntity;
            object extCanEntityBeTargeted = Interface.CallHook("CanEntityBeTargeted", new object[] { target, entity });

            if (extCanEntityBeTargeted is bool)
            {
                if ((bool)extCanEntityBeTargeted)
                {
                    if (trace) Trace($"CanEntityBeTargeted allowed {target.displayName} to be targetted by {entity.ShortPrefabName}", 1);
                    return null;
                }

                if (trace) Trace($"CanEntityBeTargeted allowed {target.displayName} from being targetted by {entity.ShortPrefabName}", 1);
                return false;
            }

            RuleSet ruleSet = GetRuleSet(target, entity);

            if (ruleSet == null)
            {
                return null;
            }

            var isAutoTurret = entity is AutoTurret;

            if (target.IsNpc || !target.userID.IsSteamId())
            {
                if (isAutoTurret)
                {
                    var obj = ruleSet.HasFlag(RuleFlags.TurretsIgnoreScientist) && entity.OwnerID.IsSteamId() ? false : (object)null;
                    if (trace) Trace($"CanBeTargeted {target.ShortPrefabName} targetted by {entity.ShortPrefabName} was {(obj is bool ? "blocked" : "allowed")}", 1);
                    return obj;
                }
                else
                {
                    var obj = ruleSet.HasFlag(RuleFlags.TrapsIgnoreScientist) ? false : (object)null;
                    //if (trace) Trace($"CanBeTargeted {target.ShortPrefabName} targetted by {entity.ShortPrefabName} was {(obj is bool ? "blocked" : "allowed")}", 1);
                    return obj;
                }
            }
            else if (isAutoTurret && ruleSet.HasFlag(RuleFlags.TurretsIgnorePlayers) || !isAutoTurret && ruleSet.HasFlag(RuleFlags.TrapsIgnorePlayers))
            {
                if (IsFunTurret(isAutoTurret, entity))
                {
                    //if (trace) Trace($"CanBeTargeted {target.displayName} targetted by turret with fun weapon was allowed", 1);
                    return null;
                }

                var entityLocations = GetLocationKeys(target);
                var initiatorLocations = GetLocationKeys(entity);

                // check for exclusion zones (zones with no rules mapped)
                if (CheckExclusion(entityLocations, initiatorLocations, trace))
                {
                    //if (trace) Trace($"CanBeTargeted {target.displayName} targetted by {entity.ShortPrefabName} was allowed by zone exclusion", 1);
                    return null;
                }

                // check for exclusions in entity group
                if (CheckExclusion(target, entity) || CheckExclusion(entity))
                {
                    //if (trace) Trace($"CanBeTargeted {target.displayName} targetted by {entity.ShortPrefabName} was allowed by entity group exclusion", 1);
                    return null;
                }

                //var source = isAutoTurret && ruleSet.HasFlag(RuleFlags.TurretsIgnorePlayers) ? "TurretsIgnorePlayers" : "TrapsIgnorePlayers";
                //if (trace) Trace($"CanBeTargeted {target.displayName} targetted by {entity.ShortPrefabName} was blocked by {source}", 1);
                return false;
            }

            return null;
        }

        // ignore players stepping on traps if configured
        private object OnTrapTrigger(BaseTrap trap, GameObject go)
        {
            var player = go.GetComponent<BasePlayer>();

            if (player == null || trap == null)
            {
                return null;
            }

            object extCanEntityTrapTrigger = Interface.CallHook("CanEntityTrapTrigger", new object[] { trap, player });

            if (extCanEntityTrapTrigger is bool)
            {
                if ((bool)extCanEntityTrapTrigger)
                {
                    return null;
                }

                return true;
            }

            var entityLocations = GetLocationKeys(player);
            var initiatorLocations = GetLocationKeys(trap);
            RuleSet ruleSet = GetRuleSet(player, trap);

            if (ruleSet == null)
            {
                return null;
            }

            if ((player.IsNpc || !player.userID.IsSteamId()) && ruleSet.HasFlag(RuleFlags.TrapsIgnoreScientist))
            {
                return true;
            }
            else if (!player.IsNpc && player.userID.IsSteamId() && ruleSet.HasFlag(RuleFlags.TrapsIgnorePlayers))
            {
                // check for exclusion zones (zones with no rules mapped)
                if (CheckExclusion(entityLocations, initiatorLocations, false))
                {
                    return null;
                }

                if (CheckExclusion(trap))
                {
                    return null;
                }

                return true;
            }

            return null;
        }

        private object OnNpcTarget(BaseNpc npc, BasePlayer target)
        {
            if (!target.IsValid() || target.IsNpc || !target.userID.IsSteamId() || !target.IsSleeping())
            {
                return null;
            }

            RuleSet ruleSet = GetRuleSet(target, npc);

            if (ruleSet == null || !ruleSet.HasFlag(RuleFlags.AnimalsIgnoreSleepers) && !animalsIgnoreSleepers)
            {
                return null;
            }

            var entityLocations = GetLocationKeys(target);
            var initiatorLocations = GetLocationKeys(npc);

            // check for exclusion zones (zones with no rules mapped)
            if (CheckExclusion(entityLocations, initiatorLocations, false))
            {
                return null;
            }

            return true;
        }

        // Check for exclusions in entity groups (attacker)
        private bool CheckExclusion(BaseEntity attacker)
        {
            string attackerName = attacker.GetType().Name;

            return config.groups.Any(group => group.IsExclusion(attacker.ShortPrefabName) || group.IsExclusion(attackerName));
        }

        // Check for exclusions in entity groups (target, attacker)
        private bool CheckExclusion(BaseEntity target, BaseEntity attacker)
        {
            string targetName = target.GetType().Name;

            if (!config.groups.Any(group => group.IsMember(target.ShortPrefabName) || group.IsExclusion(targetName)))
            {
                return false;
            }

            string attackerName = attacker.GetType().Name;

            return config.groups.Any(group => group.IsExclusion(attacker.ShortPrefabName) || group.IsExclusion(attackerName));
        }

        private RuleSet GetRuleSet(List<string> vicLocations, List<string> atkLocations)
        {
            RuleSet ruleSet = currentRuleSet;

            if (atkLocations == null) atkLocations = vicLocations; // Allow TruePVE to be used on PVP servers that want to add PVE zones via Zone Manager (just do this inside of Zone Manager instead...)

            if (vicLocations?.Count > 0 && atkLocations?.Count > 0)
            {
                if (trace) Trace($"Beginning RuleSet lookup for [{ (vicLocations.Count == 0 ? "empty" : string.Join(", ", vicLocations.ToArray())) }] and [{ (atkLocations.Count == 0 ? "empty" : string.Join(", ", atkLocations.ToArray())) }]", 2);

                var locations = GetSharedLocations(vicLocations, atkLocations);

                if (trace) Trace($"Shared locations: { (locations.Count == 0 ? "none" : string.Join(", ", locations.ToArray())) }", 3);

                if (locations?.Count > 0)
                {
                    var names = locations.Select(s => config.mappings[s]).ToList();
                    var sets = config.ruleSets.Where(r => names.Contains(r.name)).ToList();

                    if (trace) Trace($"Found {names.Count} location names, with {sets.Count} mapped RuleSets", 3);

                    if (sets.Count == 0 && config.mappings.ContainsKey(AllZones) && config.ruleSets.Any(r => r.name == config.mappings[AllZones]))
                    {
                        sets.Add(config.ruleSets.FirstOrDefault(r => r.name == config.mappings[AllZones]));
                        if (trace) Trace($"Found allzones mapped RuleSet", 3);
                    }

                    if (sets.Count > 1)
                    {
                        if (trace) Trace($"WARNING: Found multiple RuleSets: {string.Join(", ", sets.Select(s => s.name).ToArray())}", 3);
                        PrintWarning(GetMessage("Warning_MultipleRuleSets"), string.Join(", ", sets.Select(s => s.name).ToArray()));
                    }

                    ruleSet = sets.FirstOrDefault();
                    if (trace) Trace($"Found RuleSet: {ruleSet?.name ?? "null"}", 3);
                }
            }

            if (ruleSet == null)
            {
                ruleSet = currentRuleSet;
                if (trace) Trace($"No RuleSet found; assigned current global RuleSet: {ruleSet?.name ?? "null"}", 3);
            }

            return ruleSet;
        }

        private RuleSet GetRuleSet(BaseEntity e0, BaseEntity e1)
        {
            List<string> e0Locations = GetLocationKeys(e0);
            List<string> e1Locations = GetLocationKeys(e1);
            return GetRuleSet(e0Locations, e1Locations);
        }

        // get locations shared between the two passed location lists
        private List<string> GetSharedLocations(List<string> e0Locations, List<string> e1Locations)
        {
            return e0Locations.Intersect(e1Locations).Where(s => config.HasMapping(s)).ToList();
        }

        // Check exclusion for given entity locations
        private bool CheckExclusion(List<string> e0Locations, List<string> e1Locations, bool trace)
        {
            if (e0Locations == null || e1Locations == null)
            {
                if (trace) Trace("No shared locations (empty location) - no exclusions", 3);
                return false;
            }
            if (trace) Trace($"Checking exclusions between [{ (e0Locations.Count == 0 ? "empty" : string.Join(", ", e0Locations.ToArray())) }] and [{ (e1Locations.Count == 0 ? "empty" : string.Join(", ", e1Locations.ToArray())) }]", 2);
            List<string> locations = GetSharedLocations(e0Locations, e1Locations);
            if (trace) Trace($"Shared locations: {(locations.Count == 0 ? "none" : string.Join(", ", locations.ToArray()))}", 3);
            if (locations != null && locations.Count > 0)
            {
                foreach (string loc in locations)
                {
                    if (config.HasEmptyMapping(loc))
                    {
                        if (trace) Trace($"Found exclusion mapping for location: {loc}", 3);
                        return true;
                    }
                }
            }
            if (trace) Trace("No shared locations, or no matching exclusion mapping - no exclusions", 3);
            return false;
        }

        // add or update a mapping
        private bool AddOrUpdateMapping(string key, string ruleset)
        {
            if (string.IsNullOrEmpty(key) || config == null || ruleset == null || (!config.ruleSets.Select(r => r.name).Contains(ruleset) && ruleset != "exclude"))
                return false;

            config.mappings[key] = ruleset;
            SaveConfig();

            return true;
        }

        // remove a mapping
        private bool RemoveMapping(string key)
        {
            if (config.mappings.Remove(key))
            {
                SaveConfig();
                return true;
            }
            return false;
        }
        #endregion

        #region Messaging
        private void Message(BasePlayer player, string key, params object[] args) => SendReply(player, BuildMessage(player, key, args));

        private void Message(IPlayer user, string key, params object[] args) => user.Reply(RemoveFormatting(BuildMessage(null, key, args)));

        // build message string
        private string BuildMessage(BasePlayer player, string key, params object[] args)
        {
            string message = GetMessage(key, player?.UserIDString);
            if (args.Length > 0) message = string.Format(message, args);
            string type = key.Split('_')[0];
            if (player != null)
            {
                string size = GetMessage("Format_" + type + "Size");
                string color = GetMessage("Format_" + type + "Color");
                return WrapSize(size, WrapColor(color, message));
            }
            else
            {
                string color = GetMessage("Format_" + type + "Color");
                return WrapColor(color, message);
            }
        }

        // prints the value of an Option
        private void PrintValue(ConsoleSystem.Arg arg, string text, bool value)
        {
            SendReply(arg, WrapSize(GetMessage("Format_NotifySize"), WrapColor(GetMessage("Format_NotifyColor"), text + ": ") + value));
        }

        // wrap string in <size> tag, handles parsing size string to integer
        private string WrapSize(string size, string input)
        {
            int i;
            if (int.TryParse(size, out i))
                return WrapSize(i, input);
            return input;
        }

        // wrap a string in a <size> tag with the passed size
        private string WrapSize(int size, string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            return "<size=" + size + ">" + input + "</size>";
        }

        // wrap a string in a <color> tag with the passed color
        private string WrapColor(string color, string input)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(color))
                return input;
            return "<color=" + color + ">" + input + "</color>";
        }

        // show usage information
        private void ShowUsage(IPlayer user) => user.Reply(RemoveFormatting(usageString));

        public string RemoveFormatting(string source) => source.Contains(">") ? Regex.Replace(source, "<.*?>", string.Empty) : source;

        // warn that the server is set to PVE mode
        private void WarnPve() => PrintWarning(GetMessage("Warning_PveMode"));
        #endregion

        #region Helper Procedures

        // is player a HumanNPC
        private bool IsHumanNPC(BasePlayer attacker, BasePlayer victim)
        {
            if (attacker.name.Contains("ZombieNPC") || victim.name.Contains("ZombieNPC")) return true;
            
            return attacker.IsNpc || !attacker.userID.IsSteamId() || victim.IsNpc || !victim.userID.IsSteamId();
        }

        // get location keys from ZoneManager (zone IDs) or LiteZones (zone names)
        private List<string> GetLocationKeys(BaseEntity entity)
        {
            if (!useZones || entity == null) return null;
            List<string> locations = new List<string>();
            string zname;
            if (ZoneManager != null)
            {
                List<string> zmloc = new List<string>();
                if (ZoneManager.Version >= new VersionNumber(3, 0, 1))
                {
                    if (entity is BasePlayer)
                    {
                        // BasePlayer fix from chadomat
                        string[] zmlocplr = (string[])ZoneManager.Call("GetPlayerZoneIDs", new object[] { entity as BasePlayer });
                        foreach (string s in zmlocplr)
                        {
                            zmloc.Add(s);
                        }
                    }
                    else if (entity.IsValid())
                    {
                        string[] zmlocent = (string[])ZoneManager.Call("GetEntityZoneIDs", new object[] { entity });
                        foreach (string s in zmlocent)
                        {
                            zmloc.Add(s);
                        }
                    }
                }
                else if (ZoneManager.Version < new VersionNumber(3, 0, 0))
                {
                    if (entity is BasePlayer)
                    {
                        string[] zmlocplr = (string[])ZoneManager.Call("GetPlayerZoneIDs", new object[] { entity as BasePlayer });
                        foreach (string s in zmlocplr)
                        {
                            zmloc.Add(s);
                        }
                    }
                    else if (entity.IsValid())
                    {
                        zmloc = (List<string>)ZoneManager.Call("GetEntityZones", new object[] { entity });
                    }
                }
                else // Skip ZM version 3.0.0
                {
                    zmloc = null;
                }

                if (zmloc != null && zmloc.Count > 0)
                {
                    // Add names into list of ID numbers
                    foreach (string s in zmloc)
                    {
                        locations.Add(s);
                        zname = (string)ZoneManager.Call("GetZoneName", s);
                        if (zname != null) locations.Add(zname);
                        /*if (trace)
                        {
                            string message = $"Found zone {zname}: {s}";
                            if (!_foundMessages.Contains(message))
                            {
                                _foundMessages.Add(message);
                                Puts(message);
                                timer.Once(1f, () => _foundMessages.Remove(message));
                            }
                        }*/
                    }
                }
            }
            if (LiteZones != null)
            {
                List<string> lzloc = (List<string>)LiteZones?.Call("GetEntityZones", new object[] { entity });
                if (lzloc != null && lzloc.Count > 0)
                {
                    locations.AddRange(lzloc);
                }
            }
            //if (locations == null || locations.Count == 0) return null;
            return locations;
        }
        private List<string> _foundMessages = new List<string>();

        // handle raycast from player (for prodding)
        private bool GetRaycastTarget(BasePlayer player, out object closestEntity)
        {
            closestEntity = false;

            RaycastHit hit;
            if (Physics.Raycast(player.eyes.HeadRay(), out hit, 10f))
            {
                closestEntity = hit.GetEntity();
                return true;
            }
            return false;
        }

        // loop to update current ruleset
        private void TimerLoop(bool firstRun = false)
        {
            string ruleSetName;
            config.schedule.ClockUpdate(out ruleSetName, out currentBroadcastMessage);
            if (currentRuleSet.name != ruleSetName || firstRun)
            {
                currentRuleSet = config.ruleSets.FirstOrDefault(r => r.name == ruleSetName);
                if (currentRuleSet == null)
                    currentRuleSet = new RuleSet(ruleSetName); // create empty ruleset to hold name
                if (config.schedule.broadcast && currentBroadcastMessage != null)
                {
                    Server.Broadcast(currentBroadcastMessage, GetMessage("Prefix"));
                    Puts(RemoveFormatting(GetMessage("Prefix") + " Schedule Broadcast: " + currentBroadcastMessage));
                }
            }

            if (config.schedule.enabled)
                scheduleUpdateTimer = timer.Once(config.schedule.useRealtime ? 30f : 3f, () => TimerLoop());
        }

        #endregion

        #region Subclasses
        // configuration and data storage container

        private class ConfigurationOptions
        {
            [JsonProperty(PropertyName = "handleDamage")] // (true) enable TruePVE damage handling hooks
            public bool handleDamage { get; set; } = true;

            [JsonProperty(PropertyName = "useZones")] // (true) use ZoneManager/LiteZones for zone-specific damage behavior (requires modification of ZoneManager.cs)
            public bool useZones { get; set; } = true;

            [JsonProperty(PropertyName = "Trace To Player Console")]
            public bool PlayerConsole { get; set; }

            [JsonProperty(PropertyName = "Trace To Server Console")]
            public bool ServerConsole { get; set; }

            [JsonProperty(PropertyName = "Maximum Distance From Player To Trace")]
            public float MaxTraceDistance { get; set; }
        }

        private class Configuration
        {
            [JsonProperty(PropertyName = "Config Version")]
            public string configVersion = null;
            [JsonProperty(PropertyName = "Default RuleSet")]
            public string defaultRuleSet = "default";
            [JsonProperty(PropertyName = "Configuration Options")]
            public ConfigurationOptions options = new ConfigurationOptions();
            [JsonProperty(PropertyName = "Mappings")]
            public Dictionary<string, string> mappings = new Dictionary<string, string>();
            [JsonProperty(PropertyName = "Schedule")]
            public Schedule schedule = new Schedule();
            [JsonProperty(PropertyName = "RuleSets")]
            public List<RuleSet> ruleSets = new List<RuleSet>();
            [JsonProperty(PropertyName = "Entity Groups")]
            public List<EntityGroup> groups = new List<EntityGroup>();
            [JsonProperty(PropertyName = "Allow Killing Sleepers")]
            public bool AllowKillingSleepers;

            Dictionary<uint, List<string>> groupCache = new Dictionary<uint, List<string>>();

            public void Init()
            {
                schedule.Init();
                foreach (RuleSet rs in ruleSets)
                    rs.Build();
                ruleSets.Remove(null);
            }

            public List<string> ResolveEntityGroups(BaseEntity entity)
            {
                if (entity != null)
                {
                    if (entity.net != null)
                    {
                        List<string> groupList;
                        if (!groupCache.TryGetValue(entity.net.ID, out groupList))
                        {
                            groupList = groups.Where(g => g.Contains(entity)).Select(g => g.name).ToList();
                            groupCache[entity.net.ID] = groupList;
                        }
                        return groupList;
                    }

                    return groups.Where(g => g.Contains(entity)).Select(g => g.name).ToList();
                }

                return null;
            }

            public bool HasMapping(string key)
            {
                return mappings.ContainsKey(key) || mappings.ContainsKey(AllZones);
            }

            public bool HasEmptyMapping(string key)
            {
                if (mappings.ContainsKey(AllZones) && mappings[AllZones].Equals("exclude")) return true; // exlude all zones
                if (!mappings.ContainsKey(key)) return false;
                if (mappings[key].Equals("exclude")) return true;
                RuleSet r = ruleSets.FirstOrDefault(rs => rs.name.Equals(mappings[key]));
                if (r == null) return true;
                return r.IsEmpty();
            }

            public RuleSet GetDefaultRuleSet()
            {
                try
                {
                    return ruleSets.Single(r => r.name == defaultRuleSet);
                }
                catch (Exception)
                {
                    Interface.Oxide.LogWarning($"Warning - duplicate ruleset found for default RuleSet: '{defaultRuleSet}'");
                    return ruleSets.FirstOrDefault(r => r.name == defaultRuleSet);
                }
            }
        }

        private class RuleSet
        {
            public string name;
            public bool enabled = true;
            public bool defaultAllowDamage = false;
            public string flags = string.Empty;
            [JsonIgnore]
            public RuleFlags _flags = RuleFlags.None;

            public HashSet<string> rules = new HashSet<string>();
            HashSet<Rule> parsedRules = new HashSet<Rule>();

            public RuleSet() { }
            public RuleSet(string name) { this.name = name; }

            // evaluate the passed lists of entity groups against rules
            public bool Evaluate(List<string> eg1, List<string> eg2, BaseEntity attacker, bool returnDefaultValue = true)
            {
                if (Instance.trace) Instance.Trace("Evaluating Rules...", 3);
                if (parsedRules == null || parsedRules.Count == 0)
                {
                    if (Instance.trace) Instance.Trace($"No rules found; returning default value: {defaultAllowDamage}", 4);
                    return defaultAllowDamage;
                }
                bool? res;
                if (Instance.trace) Instance.Trace("Checking direct initiator->target rules...", 4);
                // check all direct links
                bool resValue = defaultAllowDamage;
                bool resFound = false;

                if (eg1 != null && eg1.Count > 0 && eg2 != null && eg2.Count > 0)
                {
                    foreach (string s1 in eg1)
                    {
                        foreach (string s2 in eg2)
                        {
                            if ((res = Evaluate(s1, s2)).HasValue)
                            {
                                resValue = res.Value;
                                resFound = true;
                                break;
                            }
                        }
                    }
                }

                if (!resFound && eg1 != null && eg1.Count > 0)
                {
                    if (Instance.trace) Instance.Trace("No direct match rules found; continuing...", 4);

                    foreach (string s1 in eg1)
                    {// check group -> any
                        if ((res = Evaluate(s1, Any)).HasValue)
                        {
                            resValue = res.Value;
                            resFound = true;
                            break;
                        }
                    }
                }

                if (!resFound && eg2 != null && eg2.Count > 0)
                {
                    if (Instance.trace) Instance.Trace("No matching initiator->any rules found; continuing...", 4);

                    foreach (string s2 in eg2)
                    {// check any -> group
                        if ((res = Evaluate(Any, s2)).HasValue)
                        {
                            resValue = res.Value;
                            resFound = true;
                            break;
                        }
                    }
                }

                if (resFound)
                {
                    /*if (attacker.IsValid() && Instance.data.groups.Any(group => group.IsExclusion(attacker.GetType().Name) || group.IsExclusion(attacker.ShortPrefabName)))
                    {
                        if (Instance.trace) Instance.Trace($"Exclusion found; allow damage? {!resValue}", 6);
                        return !resValue;
                    }*/

                    return resValue;
                }

                if (returnDefaultValue)
                {
                    if (Instance.trace) Instance.Trace($"No matching any->target rules found; returning default value: {defaultAllowDamage}", 4);
                    return defaultAllowDamage;
                }

                return true;
            }

            // evaluate two entity groups against rules
            public bool? Evaluate(string eg1, string eg2)
            {
                if (eg1 == null || eg2 == null || parsedRules == null || parsedRules.Count == 0) return null;
                if (Instance.trace) Instance.Trace($"Evaluating \"{eg1}->{eg2}\"...", 5);
                Rule rule = parsedRules.FirstOrDefault(r => r.valid && r.key.Equals(eg1 + "->" + eg2));
                if (rule != null)
                {
                    if (Instance.trace) Instance.Trace($"Match found; allow damage? {rule.hurt}", 6);
                    return rule.hurt;
                }
                if (Instance.trace) Instance.Trace($"No match found", 6);
                return null;
            }

            // build rule strings to rules
            public void Build()
            {
                foreach (string ruleText in rules)
                    parsedRules.Add(new Rule(ruleText));
                parsedRules.Remove(null);
                ValidateRules();
                var values = flags.Contains(",") ? flags.Split(',') : new string[1] { flags };
                foreach (string value in values)
                {
                    RuleFlags flag = ParseType<RuleFlags>(value);

                    if (flag == RuleFlags.None)
                    {
                        Instance.Puts("WARNING - invalid flag: '{0}' (does this flag still exist?)", value.Trim());
                    }
                    else if (!_flags.HasFlag(flag))
                    {
                        _flags |= flag;
                    }
                }
            }

            private T ParseType<T>(string type)
            {
                try
                {
                    return (T)Enum.Parse(typeof(T), type, true);
                }
                catch
                {
                    return default(T);
                }
            }

            public void ValidateRules()
            {
                foreach (Rule rule in parsedRules)
                    if (!rule.valid)
                        Interface.Oxide.LogWarning($"Warning - invalid rule: {rule.ruleText}");
            }

            // add a rule
            public void AddRule(string ruleText)
            {
                rules.Add(ruleText);
                parsedRules.Add(new Rule(ruleText));
            }

            public bool HasAnyFlag(RuleFlags flags) { return (this._flags | flags) != RuleFlags.None; }
            public bool HasFlag(RuleFlags flag) { return (_flags & flag) == flag; }
            public bool IsEmpty() { return (rules == null || rules.Count == 0) && _flags == RuleFlags.None; }
        }

        private class Rule
        {
            public string ruleText;
            [JsonIgnore]
            public string key;
            [JsonIgnore]
            public bool hurt;
            [JsonIgnore]
            public bool valid;

            public Rule() { }
            public Rule(string ruleText)
            {
                this.ruleText = ruleText;
                valid = RuleTranslator.Translate(this);
            }

            public override int GetHashCode() { return key.GetHashCode(); }

            public override bool Equals(object obj)
            {
                if (obj == null) return false;
                if (obj == this) return true;
                if (obj is Rule)
                    return key.Equals((obj as Rule).key);
                return false;
            }
        }

        // helper class to translate rule text to rules
        private class RuleTranslator
        {
            static readonly Regex regex = new Regex(@"\s+");
            static readonly List<string> synonyms = new List<string>() { "anything", "nothing", "all", "any", "none", "everything" };
            public static bool Translate(Rule rule)
            {
                if (rule.ruleText == null || rule.ruleText.Equals("")) return false;
                string str = rule.ruleText;
                string[] splitStr = regex.Split(str);
                // first and last words should be ruleset names
                string rs0 = splitStr[0];
                string rs1 = splitStr[splitStr.Length - 1];
                string[] mid = splitStr.Skip(1).Take(splitStr.Length - 2).ToArray();
                if (mid == null || mid.Length == 0) return false;

                bool canHurt = true;
                foreach (string s in mid)
                    if (s.Equals("cannot") || s.Equals("can't"))
                        canHurt = false;

                // rs0 and rs1 shouldn't ever be "nothing" simultaneously
                if (rs0.Equals("nothing") || rs1.Equals("nothing") || rs0.Equals("none") || rs1.Equals("none")) canHurt = !canHurt;

                if (synonyms.Contains(rs0)) rs0 = Any;
                if (synonyms.Contains(rs1)) rs1 = Any;

                rule.key = rs0 + "->" + rs1;
                rule.hurt = canHurt;
                return true;
            }
        }

        // container for mapping entities
        private class EntityGroup
        {
            private List<string> memberList { get; set; } = new List<string>();
            private List<string> exclusionList { get; set; } = new List<string>();
            public string name { get; set; }

            public string members
            {
                get
                {
                    if (memberList.Count == 0) return string.Empty;
                    return string.Join(", ", memberList.ToArray());
                }
                set
                {
                    if (string.IsNullOrEmpty(value)) return;
                    memberList = value.Split(',').Select(s => s.Trim()).ToList();
                }
            }

            public string exclusions
            {
                get
                {
                    if (exclusionList.Count == 0) return string.Empty;
                    return string.Join(", ", exclusionList.ToArray());
                }
                set
                {
                    if (string.IsNullOrEmpty(value)) return;
                    exclusionList = value.Split(',').Select(s => s.Trim()).ToList();
                }
            }

            public EntityGroup()
            {

            }

            public EntityGroup(string name)
            {
                this.name = name;
            }

            public bool IsMember(string value)
            {
                foreach (var member in memberList)
                {
                    if (member.Equals(value, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }

            public bool IsExclusion(string value)
            {
                foreach (var exclusion in exclusionList)
                {
                    if (exclusion.Equals(value, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }

            public bool Contains(BaseEntity entity)
            {
                if (entity == null) return false;
                return (memberList.Contains(entity.GetType().Name) || memberList.Contains(entity.ShortPrefabName)) && !(exclusionList.Contains(entity.GetType().Name) || exclusionList.Contains(entity.ShortPrefabName));
            }
        }

        // scheduler
        private class Schedule
        {
            public bool enabled = false;
            public bool useRealtime = false;
            public bool broadcast = false;
            public List<string> entries = new List<string>();
            List<ScheduleEntry> parsedEntries = new List<ScheduleEntry>();
            [JsonIgnore]
            public bool valid = false;

            public void Init()
            {
                foreach (string str in entries)
                    parsedEntries.Add(new ScheduleEntry(str));
                // schedule not valid if entries are empty, there are less than 2 entries, or there are less than 2 rulesets defined
                if (parsedEntries == null || parsedEntries.Count == 0 || parsedEntries.Count(e => e.valid) < 2 || parsedEntries.Select(e => e.ruleSet).Distinct().Count() < 2)
                    enabled = false;
                else
                    valid = true;
            }

            // returns delta between current time and next schedule entry
            public void ClockUpdate(out string ruleSetName, out string message)
            {
                TimeSpan time = useRealtime ? new TimeSpan((int)DateTime.Now.DayOfWeek, 0, 0, 0).Add(DateTime.Now.TimeOfDay) : TOD_Sky.Instance.Cycle.DateTime.TimeOfDay;
                try
                {
                    ScheduleEntry se = null;
                    // get the most recent schedule entry
                    if (parsedEntries.Where(t => !t.isDaily).Count() > 0)
                        se = parsedEntries.FirstOrDefault(e => e.time == parsedEntries.Where(t => t.valid && t.time <= time && ((useRealtime && !t.isDaily) || !useRealtime)).Max(t => t.time));
                    // if realtime, check for daily
                    if (useRealtime)
                    {
                        ScheduleEntry daily = null;
                        try
                        {
                            daily = parsedEntries.FirstOrDefault(e => e.time == parsedEntries.Where(t => t.valid && t.time <= DateTime.Now.TimeOfDay && t.isDaily).Max(t => t.time));
                        }
                        catch (Exception)
                        { // no daily entries
                        }
                        if (daily != null && se == null)
                            se = daily;
                        if (daily != null && daily.time.Add(new TimeSpan((int)DateTime.Now.DayOfWeek, 0, 0, 0)) > se.time)
                            se = daily;
                    }
                    ruleSetName = se.ruleSet;
                    message = se.message;
                }
                catch (Exception)
                {
                    ScheduleEntry se = null;
                    // if time is earlier than all schedule entries, use max time
                    if (parsedEntries.Where(t => !t.isDaily).Count() > 0)
                        se = parsedEntries.FirstOrDefault(e => e.time == parsedEntries.Where(t => t.valid && ((useRealtime && !t.isDaily) || !useRealtime)).Max(t => t.time));
                    if (useRealtime)
                    {
                        ScheduleEntry daily = null;
                        try
                        {
                            daily = parsedEntries.FirstOrDefault(e => e.time == parsedEntries.Where(t => t.valid && t.isDaily).Max(t => t.time));
                        }
                        catch (Exception)
                        { // no daily entries
                        }
                        if (daily != null && se == null)
                            se = daily;
                        if (daily != null && daily.time.Add(new TimeSpan((int)DateTime.Now.DayOfWeek, 0, 0, 0)) > se.time)
                            se = daily;
                    }
                    ruleSetName = se?.ruleSet;
                    message = se?.message;
                }
            }
        }

        // helper class to translate schedule text to schedule entries
        private class ScheduleTranslator
        {
            static readonly Regex regex = new Regex(@"\s+");
            public static bool Translate(ScheduleEntry entry)
            {
                if (entry.scheduleText == null || entry.scheduleText.Equals("")) return false;
                string str = entry.scheduleText;
                string[] splitStr = regex.Split(str, 3); // split into 3 parts
                // first word should be a timespan
                string ts = splitStr[0];
                // second word should be a ruleset name
                string rs = splitStr[1];
                // remaining should be message
                string message = splitStr.Length > 2 ? splitStr[2] : null;

                try
                {
                    if (ts.StartsWith("*."))
                    {
                        entry.isDaily = true;
                        ts = ts.Substring(2);
                    }
                    entry.time = TimeSpan.Parse(ts);
                    entry.ruleSet = rs;
                    entry.message = message;
                    return true;
                }
                catch
                { }

                return false;
            }
        }

        private class ScheduleEntry
        {
            public string ruleSet;
            public string message;
            public string scheduleText;
            public bool valid;
            public TimeSpan time { get; set; }
            [JsonIgnore]
            public bool isDaily = false;

            public ScheduleEntry() { }
            public ScheduleEntry(string scheduleText)
            {
                this.scheduleText = scheduleText;
                valid = ScheduleTranslator.Translate(this);
            }
        }
        #endregion

        #region Lang
        // load default messages to Lang
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Prefix", "<color=#FFA500>[ TruePVE ]</color>" },
                {"Enable", "TruePVE enable set to {0}" },

                {"Header_Usage", "---- TruePVE usage ----"},
                {"Cmd_Usage_def", "Loads default configuration and data"},
                {"Cmd_Usage_sched", "Enable or disable the schedule" },
                {"Cmd_Usage_prod", "Show the prefab name and type of the entity being looked at"},
                {"Cmd_Usage_map", "Create/remove a mapping entry" },
                {"Cmd_Usage_trace", "Toggle tracing on/off" },

                {"Warning_PveMode", "ConVar server.pve is TRUE!  TruePVE is designed for PVP mode, and may cause unexpected behavior in PVE mode."},
                {"Warning_NoRuleSet", "No RuleSet found for \"{0}\"" },
                {"Warning_DuplicateRuleSet", "Multiple RuleSets found for \"{0}\"" },

                {"Error_InvalidCommand", "Invalid command" },
                {"Error_InvalidParameter", "Invalid parameter: {0}"},
                {"Error_InvalidParamForCmd", "Invalid parameters for command \"{0}\""},
                {"Error_InvalidMapping", "Invalid mapping: {0} => {1}; Target must be a valid RuleSet or \"exclude\"" },
                {"Error_NoMappingToDelete", "Cannot delete mapping: \"{0}\" does not exist" },
                {"Error_NoPermission", "Cannot execute command: No permission"},
                {"Error_NoSuicide", "You are not allowed to commit suicide"},
                {"Error_NoEntityFound", "No entity found"},

                {"Notify_AvailOptions", "Available Options: {0}"},
                {"Notify_DefConfigLoad", "Loaded default configuration"},
                {"Notify_DefDataLoad", "Loaded default mapping data"},
                {"Notify_ProdResult", "Prod results: type={0}, prefab={1}"},
                {"Notify_SchedSetEnabled", "Schedule enabled" },
                {"Notify_SchedSetDisabled", "Schedule disabled" },
                {"Notify_InvalidSchedule", "Schedule is not valid" },
                {"Notify_MappingCreated", "Mapping created for \"{0}\" => \"{1}\"" },
                {"Notify_MappingUpdated", "Mapping for \"{0}\" changed from \"{1}\" to \"{2}\"" },
                {"Notify_MappingDeleted", "Mapping for \"{0}\" => \"{1}\" deleted" },
                {"Notify_TraceToggle", "Trace mode toggled {0}" },

                {"Format_EnableColor", "#00FFFF"}, // cyan
                {"Format_EnableSize", "12"},
                {"Format_NotifyColor", "#00FFFF"}, // cyan
                {"Format_NotifySize", "12"},
                {"Format_HeaderColor", "#FFA500"}, // orange
                {"Format_HeaderSize", "14"},
                {"Format_ErrorColor", "#FF0000"}, // red
                {"Format_ErrorSize", "12"},
            }, this);
        }

        // get message from Lang
        private string GetMessage(string key, string userId = null) => lang.GetMessage(key, this, userId);
        #endregion
    }
}