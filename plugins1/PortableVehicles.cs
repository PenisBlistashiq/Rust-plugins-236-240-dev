/*
 ########### README ####################################################
                                                                             
  !!! DON'T EDIT THIS FILE !!!
  Orange is the original developer of this plugin
                                                                     
 ########### CHANGES ###################################################

 1.1.2
    - Rewrited config
    - Added chat command
    - Added chat icon option
    - Added more localization messages
    - Added option blacklist pickupable vehicles
    - Added item name vehicle
    - Fixed Pickupable Hot Air Balloon
    - Added submarinesolo
    - Added submarineduo
    - Added snowmobile
    - Added option what item you will be placing

 #######################################################################
*/

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;
using VLB;

namespace Oxide.Plugins
{
    [Info("Portable Vehicles", "Paulsimik", "1.1.2")]
    [Description("Give vehicles as item to your players")]
    public class PortableVehicles : RustPlugin
    {
        #region [Fields]

        private static Configuration config;
        private const string permUse = "portablevehicles.use";
        private const string permAdmin = "portablevehicles.admin";
        private const string permPickup = "portablevehicles.pickup";
        private string[] chatCommands = { "pv", "portablevehicles", "portablevehicle" };

        #endregion

        #region [Oxide Hooks]

        private void Init()
        {
            permission.RegisterPermission(permUse, this);
            permission.RegisterPermission(permAdmin, this);
            permission.RegisterPermission(permPickup, this);

            foreach (var command in chatCommands)
                cmd.AddChatCommand(command, this, nameof(cmdPortableVehicles));
        }

        private void OnEntityBuilt(Planner plan, GameObject go) => CheckPlacement(plan, go);

        private object OnHammerHit(BasePlayer player, HitInfo info)
        {
            if (info.HitEntity is BaseVehicle)
            {
                if (CheckPickup(player, info.HitEntity as BaseVehicle))
                    return true;
            }

            if (info.HitEntity is HotAirBalloon)
            {
                if (CheckPickupBalloon(player, info.HitEntity as HotAirBalloon))
                    return true;
            }

            return null;
        }

        #endregion

        #region [Hooks]   

        private void CheckPlacement(Planner plan, GameObject go)
        {
            var entity = go.ToBaseEntity();
            if (entity == null)
                return;

            var player = plan.GetOwnerPlayer();
            var info = AllVehicles.FirstOrDefault(x => x.skinId == entity.skinID);
            if (info == null)
                return;

            var transform = entity.transform;
            var position = transform.position;
            var rotation = transform.rotation;
            var owner = entity.OwnerID;
            var skin = entity.skinID;

            transform.position = new Vector3();
            entity.TransformChanged();
            timer.Once(1f, () =>
            {
                if (entity.IsValid() && !entity.IsDestroyed)
                {
                    entity.Kill();
                }
            });

            var time = 0.5f;
            if (info.bigModel)
            {
                time = 5f;
                Message(player, "BigModel");
            }

            timer.Once(time, () =>
            {
                var vehicle = GameManager.server.CreateEntity(info.prefab, position, rotation);
                if (vehicle != null)
                {
                    vehicle.skinID = skin;
                    vehicle.OwnerID = owner;
                    vehicle.Spawn();

                    if (config.autoMount)
                    {
                        var mountable = vehicle as BaseVehicle;
                        if (mountable != null && mountable.mountPoints != null && mountable.mountPoints.Count > 0)
                        {
                            var driverSeat = mountable.mountPoints.FirstOrDefault()?.mountable;
                            if (driverSeat != null)
                            {
                                driverSeat.MountPlayer(player);
                                player.SendNetworkUpdate();
                            }
                        }
                    }
                }
                else
                {
                    SendMessage(player, "Failed to spawn vehicle!");
                }
            });
        }

        private bool CheckPickup(BasePlayer player, BaseVehicle entity)
        {
            if (entity == null)
                return false;

            if (entity.skinID == 0)
                return false;

            if (!permission.UserHasPermission(player.UserIDString, permPickup))
                return false;

            var time = entity.SecondsSinceAttacked;
            if (time < 30)
            {
                Message(player, "Recently Attacked", (30 - time).ToString("0.0"));
                return true;
            }

            var diff = (Mathf.Abs(entity.MaxHealth() - entity.Health()));
            if (diff > 5f)
            {
                Message(player, "Durability");
                return false;
            }

            if (config.pickupableBlacklist.Contains(entity.ShortPrefabName))
            {
                Message(player, "Pickupable");
                return true;
            }

            if (entity.OwnerID != player.userID)
            {
                Message(player, "Pickup Ownership");
                return true;
            }

            if (!player.CanBuild())
            {
                Message(player, "Cupboard");
                return true;
            }

            var containers = entity.GetComponentsInChildren<StorageContainer>();
            if (containers.Any(x => x.inventory.itemList.Count > 0))
            {
                Message(player, "Not Empty");
                return true;
            }

            var fs = entity.GetFuelSystem();
            if (fs != null && !fs.fuelStorageInstance.Get(true).IsLocked() && fs.HasFuel())
            {
                Message(player, "Fuel");
                return true;
            }

            var script = entity.GetOrAddComponent<PickupScript>();
            script.AddHit();
            var left = script.GetHitsLeft();
            if (left > 0)
            {
                Message(player, "Hits", script.GetHitsLeft());
                return true;
            }

            foreach (var value in AllVehicles)
            {
                if (value.prefab == entity.PrefabName)
                {
                    entity.Kill();
                    GiveItem(player, value.skinId, value.displayName, value.isWaterVehicle);
                    return true;
                }
            }

            return false;
        }

        private bool CheckPickupBalloon(BasePlayer player, HotAirBalloon balloon)
        {
            if (balloon == null)
                return false;

            if (balloon.skinID == 0)
                return false;

            if (!permission.UserHasPermission(player.UserIDString, permPickup))
                return false;

            var time = balloon.SecondsSinceAttacked;
            if (time < 30)
            {
                Message(player, "Recently Attacked", (30 - time).ToString("0.0"));
                return true;
            }

            var diff = (Mathf.Abs(balloon.MaxHealth() - balloon.Health()));
            if (diff > 5f)
            {
                Message(player, "Durability");
                return false;
            }

            if (config.pickupableBlacklist.Contains(balloon.ShortPrefabName))
            {
                Message(player, "Pickupable");
                return true;
            }

            if (balloon.OwnerID != player.userID)
            {
                Message(player, "Pickup Ownership");
                return true;
            }

            if (!player.CanBuild())
            {
                Message(player, "Cupboard");
                return true;
            }

            var containers = balloon.GetComponentsInChildren<StorageContainer>();
            if (containers.Any(x => x.inventory.itemList.Count > 0))
            {
                Message(player, "Not Empty");
                return true;
            }

            var fs = balloon.fuelSystem;
            if (fs != null && !fs.fuelStorageInstance.Get(true).IsLocked() && fs.HasFuel())
            {
                Message(player, "Fuel");
                return true;
            }

            var script = balloon.GetOrAddComponent<PickupScript>();
            script.AddHit();
            var left = script.GetHitsLeft();
            if (left > 0)
            {
                Message(player, "Hits", script.GetHitsLeft());
                return true;
            }

            foreach (var value in AllVehicles)
            {
                if (value.prefab == balloon.PrefabName)
                {
                    balloon.Kill();
                    GiveItem(player, value.skinId, value.displayName, value.isWaterVehicle);
                    return true;
                }
            }

            return false;
        }

        private void GiveItem(BasePlayer player, ulong skin)
        {
            var vehicle = AllVehicles.FirstOrDefault(x => x.skinId == skin);
            if (vehicle == null)
                return;

            GiveItem(player, skin, vehicle.displayName, vehicle.isWaterVehicle);
        }

        private void GiveItem(BasePlayer player, ulong skinID, string name, bool isWaterVehicle)
        {
            if (string.IsNullOrEmpty(name))
                name = "Portable Vehicle";

            var shortname = isWaterVehicle ? config.waterEntityShortName : config.groundEntityShortName;
            if (string.IsNullOrEmpty(shortname))
                shortname = "box.wooden.large";

            Item item = ItemManager.CreateByName(shortname, 1, skinID);
            if (item != null)
            {
                item.name = name;
                player.GiveItem(item);
                Message(player, "Received", name);
            }
        }

        private ulong GetSkin(string name)
        {
            switch (name.ToLower())
            {
                case "rhib":
                case "militaryboat":
                case "military":
                    return 1742627792;

                case "boat":
                case "rowboat":
                case "motorboat":
                    return 1742651766;

                case "copter":
                case "mini":
                case "minicopter":
                    return 1742653197;

                case "balloon":
                case "hotairballoon":
                    return 1771792987;

                case "ch":
                case "ch47":
                case "chinook":
                    return 1771792500;

                case "horse":
                case "testridablehorse":
                    return 1773898864;

                case "scrap":
                case "scrapheli":
                case "scraphelicopter":
                case "helicopter":
                    return 1856165291;

                case "car":
                case "car1":
                case "sedan":
                    return 1742652663;

                case "car2":
                    return 2160249787;

                case "car3":
                    return 2160250208;

                case "car4":
                    return 2160251723;

                case "submarinesolo":
                    return 2566928797;

                case "submarineduo":
                    return 2566928707;

                case "snowmobile":
                    return 2741703605;

                default:
                    return 0;
            }
        }

        #endregion

        #region [Chat Commands]

        private void cmdPortableVehicles(BasePlayer player, string command, string[] args)
        {
            var value = args.Length > 0 ? args[0] : null;
            var value2 = args.Length > 1 ? args[1] : null;
            if (value == null)
            {
                Message(player, "Invalid Syntax");
                return;
            }

            if (value2 == null)
            {
                if (!permission.UserHasPermission(player.UserIDString, permUse))
                {
                    Message(player, "Permission");
                    return;
                }

                var skin = GetSkin(value);
                if (skin == 0)
                {
                    Message(player, "Invalid Vehicle");
                    return;
                }

                GiveItem(player, skin);
            }
            else
            {
                if (!IsAdmin(player))
                {
                    Message(player, "Permission");
                    return;
                }

                var target = FindPlayer(player, value);
                if (target == null)
                    return;

                var skin = GetSkin(value2);
                if (skin == 0)
                {
                    Message(player, "Invalid Vehicle");
                    return;
                }

                GiveItem(target, skin);
            }
        }

        #endregion

        #region [Console Commands]

        [ConsoleCommand("portablevehicles.give")]
        private void cmdGiveConsole(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null && !IsAdmin(player))
            {
                Message(player, "Permission");
                return;
            }

            var args = arg.Args;
            if (args == null || args.Length < 2)
            {
                Message(arg, "Usage");
                return;
            }

            var target = FindPlayer(arg, args[0]);
            if (target == null)
                return;

            var skin = GetSkin(args[1]);
            if (skin == 0)
            {
                Message(arg, "Invalid Vehicle");
                return;
            }

            GiveItem(target, skin);
        }

        #endregion

        #region [Classes]

        private class Configuration
        {
            [JsonProperty(PropertyName = "Chat Icon")]
            public uint chatIcon;

            [JsonProperty("Hits count to pickup vehicle")]
            public int hitsToPickup;

            [JsonProperty("Automatically mount players")]
            public bool autoMount;

            [JsonProperty(PropertyName = "Item shortname for water entity")]
            public string waterEntityShortName;

            [JsonProperty(PropertyName = "Item shortname for ground entity")]
            public string groundEntityShortName;

            [JsonProperty(PropertyName = "Blacklist pickupable vehicles shortname")]
            public List<string> pickupableBlacklist = new List<string>();

            public VersionNumber version;
        }

        private class VehicleEntry
        {
            public ulong skinId;
            public string displayName;
            public string prefab;
            public bool bigModel;
            public bool isWaterVehicle;
        }

        private VehicleEntry[] AllVehicles = new[]
        {
            new VehicleEntry
            {
                skinId = 1742627792,
                displayName = "Rhib",
                prefab = "assets/content/vehicles/boats/rhib/rhib.prefab",
                isWaterVehicle = true
            },
            new VehicleEntry
            {
                skinId = 1742651766,
                displayName = "Boat",
                prefab = "assets/content/vehicles/boats/rowboat/rowboat.prefab",
                isWaterVehicle = true
            },
            new VehicleEntry
            {
                skinId = 1742653197,
                displayName = "MiniCopter",
                prefab = "assets/content/vehicles/minicopter/minicopter.entity.prefab",
            },
            new VehicleEntry
            {
                skinId = 1742652663,
                displayName = "Sedan",
                prefab = "assets/content/vehicles/sedan_a/sedantest.entity.prefab",
            },
            new VehicleEntry
            {
                skinId = 1771792500,
                displayName = "Chinook",
                prefab = "assets/prefabs/npc/ch47/ch47.entity.prefab",
                bigModel = true,
            },
            new VehicleEntry
            {
                skinId = 1771792987,
                displayName = "Hot Air Balloon",
                prefab = "assets/prefabs/deployable/hot air balloon/hotairballoon.prefab",
                bigModel = true,
            },
            new VehicleEntry
            {
                skinId = 1773898864,
                displayName = "Horse",
                prefab = "assets/rust.ai/nextai/testridablehorse.prefab",
            },
            new VehicleEntry
            {
                skinId = 1856165291,
                displayName = "Scrap Transport Helicopter",
                prefab = "assets/content/vehicles/scrap heli carrier/scraptransporthelicopter.prefab",
                bigModel = true,
            },
            new VehicleEntry
            {
                skinId = 2160249787,
                displayName = "2 Module Car",
                prefab = "assets/content/vehicles/modularcar/2module_car_spawned.entity.prefab",
            },
            new VehicleEntry
            {
                skinId = 2160250208,
                displayName = "3 Module Car",
                prefab = "assets/content/vehicles/modularcar/3module_car_spawned.entity.prefab",
            },
            new VehicleEntry
            {
                skinId = 2160251723,
                displayName = "4 Module Car",
                prefab = "assets/content/vehicles/modularcar/4module_car_spawned.entity.prefab",
            },
            new VehicleEntry
            {
                skinId = 2566928797,
                displayName = "Submarine Solo",
                prefab = "assets/content/vehicles/submarine/submarinesolo.entity.prefab",
                isWaterVehicle = true
            },
            new VehicleEntry
            {
                skinId = 2566928707,
                displayName = "Submarine Duo",
                prefab = "assets/content/vehicles/submarine/submarineduo.entity.prefab",
                isWaterVehicle = true
            },
            new VehicleEntry
            {
                skinId = 2741703605,
                displayName = "Snowmobile",
                prefab = "assets/content/vehicles/snowmobiles/snowmobile.prefab",
            }
        };

        private class PickupScript : MonoBehaviour
        {
            private BaseEntity entity;
            private int hits;

            private void Awake()
            {
                entity = GetComponent<BaseEntity>();
            }

            public void AddHit()
            {
                if (entity.Health() < entity.MaxHealth())
                    return;

                hits++;
                CancelInvoke(nameof(ResetHits));
                Invoke(nameof(ResetHits), 60);
            }

            private void ResetHits()
            {
                hits = 0;
            }

            public int GetHitsLeft()
            {
                return config.hitsToPickup - hits;
            }
        }

        #endregion

        #region [Config]

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                chatIcon = 0,
                hitsToPickup = 5,
                autoMount = false,
                waterEntityShortName = "innertube",
                groundEntityShortName = "box.wooden.large",
                pickupableBlacklist = new List<string>
                {
                    "rhib",
                    "rowboat",
                    "minicopter.entity",
                    "sedantest.entity",
                    "ch47.entity",
                    "hotairballoon",
                    "testridablehorse",
                    "scraptransporthelicopter",
                    "2module_car_spawned.entity",
                    "3module_car_spawned.entity",
                    "4module_car_spawned.entity",
                    "submarinesolo.entity",
                    "submarineduo.entity",
                    "snowmobile"
                },
                version = Version
            };
        }

        protected override void LoadDefaultConfig()
        {
            config = GetDefaultConfig();
            Puts("Generating new configuration file........");
        }

        protected override void SaveConfig() => Config.WriteObject(config);

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<Configuration>();

                if (config == null)
                    LoadDefaultConfig();
            }
            catch
            {
                for (var i = 0; i < 3; i++)
                {
                    PrintError("######### Configuration file is not valid! #########");
                }
                return;
            }

            SaveConfig();
        }

        #endregion

        #region [Localization]

        private string GetLang(string key, string playerID, params object[] args) => string.Format(lang.GetMessage(key, this, playerID), args);

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Usage", "Usage: portablevehicles.give 'steamID/player name' 'vehicle name'\n"},
                {"Invalid Syntax", "Invalid Syntax!\n/pv 'vehicle name'\n/pv 'steamID/player name' 'vehicle name'"},
                {"Permission", "You don't have permission to use that!"},
                {"Received", "You received '{0}'!"},
                {"No Player", "There are no players with that Name or steamID!"},
                {"Multiple Players", "There are many players with that Name:\n{0}"},
                {"Pickup Ownership", "Only owner can pickup vehicles!"},
                {"Fuel", "You need to remove fuel from vehicle first!"},
                {"Recently Attacked", "Vehicle was recently attacked! {0}s left"},
                {"Durability", "You need to repair vehicles fully!"},
                {"Not Empty", "Vehicle is not empty! Check fuel or storages!"},
                {"Hits", "You need to do more {0} hits!"},
                {"Cupboard", "You need to have building privilege to do that!"},
                {"BigModel", "That vehicle have big model and can kill you. Run away! It will be spawned in 5 seconds"},
                {"Pickupable", "This vehicle cannot be picked up"},
                {"Invalid Vehicle", "Vehicle name is invalid!"},
            }, this);
        }

        #endregion

        #region [Helpers]

        private bool IsAdmin(BasePlayer player)
        {
            if (player.IsAdmin || permission.UserHasPermission(player.UserIDString, permAdmin))
                return true;

            return false;
        }

        private BasePlayer FindPlayer(ConsoleSystem.Arg arg, string nameOrID)
        {
            var targets = BasePlayer.activePlayerList.Where(x => x.UserIDString == nameOrID || x.displayName.ToLower().Contains(nameOrID.ToLower())).ToList();
            if (targets.Count == 0)
            {
                Message(arg, "No Player");
                return null;
            }

            if (targets.Count > 1)
            {
                Message(arg, "Multiple Players");
                return null;
            }

            return targets[0];
        }

        private BasePlayer FindPlayer(BasePlayer player, string nameOrID)
        {
            var targets = BasePlayer.activePlayerList.Where(x => x.UserIDString == nameOrID || x.displayName.ToLower().Contains(nameOrID.ToLower())).ToList();
            if (targets.Count == 0)
            {
                Message(player, "No Player");
                return null;
            }

            if (targets.Count > 1)
            {
                Message(player, "Multiple Players");
                return null;
            }

            return targets[0];
        }

        private void Message(ConsoleSystem.Arg arg, string messageKey, params object[] args)
        {
            var message = GetLang(messageKey, null, args);
            var player = arg.Player();
            if (player != null)
            {
                SendMessage(player, message);
            }
            else
            {
                SendReply(arg, message);
            }
        }

        private void Message(BasePlayer player, string messageKey, params object[] args)
        {
            if (player == null)
                return;

            var message = GetLang(messageKey, player.UserIDString, args);
            SendMessage(player, message);
        }

        private void SendMessage(BasePlayer player, string msg) => Player.Message(player, msg, config.chatIcon);

        #endregion
    }
}