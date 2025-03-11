using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins

{
    [Info("VehicleAirdrops", "Nikedemos", VERSION)]
    [Description("Adds new types of airdrops and supply signals")]

    public class VehicleAirdrops : RustPlugin
    {
        #region META
        public const string VERSION = "1.0.8";

        public const string PREFAB_FIREWORK_BLUE = "assets/prefabs/deployable/fireworks/mortarblue.prefab";
        public const string PREFAB_FIREWORK_CHAMPAGNE = "assets/prefabs/deployable/fireworks/mortarchampagne.prefab";
        public const string PREFAB_FIREWORK_GREEN = "assets/prefabs/deployable/fireworks/mortargreen.prefab";
        public const string PREFAB_FIREWORK_ORANGE = "assets/prefabs/deployable/fireworks/mortarorange.prefab";
        public const string PREFAB_FIREWORK_VIOLET = "assets/prefabs/deployable/fireworks/mortarviolet.prefab";
        public const string PREFAB_FIREWORK_RED = "assets/prefabs/deployable/fireworks/mortarred.prefab";

        public const string PREFAB_ROMAN_BLUE = "assets/prefabs/deployable/fireworks/romancandle-blue.prefab";
        public const string PREFAB_ROMAN_GREEN = "assets/prefabs/deployable/fireworks/romancandle-green.prefab";
        public const string PREFAB_ROMAN_VIOLET = "assets/prefabs/deployable/fireworks/romancandle-violet.prefab";

        public const string PREFAB_ROMAN_RED = "assets/prefabs/deployable/fireworks/romancandle.prefab";

        #endregion

        #region LANG
        public static string MSG(string msg, string userID = null, params object[] args)
        {
            if (args == null)
            {
                return Instance.lang.GetMessage(msg, Instance, userID);
            }
            else
            {
                return string.Format(Instance.lang.GetMessage(msg, Instance, userID), args);
            }

        }
        public const string MSG_DROP_NAME_CAR = "MSG_DROP_NAME_CAR";
        public const string MSG_DROP_NAME_CARS = "MSG_DROP_NAME_CARS";

        public const string MSG_NO_PERMISSION = "MSG_NO_PERMISSION";

        public const string MSG_DROP_NAME_NORMAL = "MSG_DROP_NAME_NORMAL";

        public const string MSG_DROP_NAME_MINICOPTER = "MSG_DROP_NAME_MINICOPTER";
        public const string MSG_DROP_NAME_MINICOPTERS = "MSG_DROP_NAME_MINICOPTERS";

        public const string MSG_DROP_NAME_SCRAPHELI = "MSG_DROP_NAME_SCRAPHELI";
        public const string MSG_DROP_NAME_SCRAPHELIS = "MSG_DROP_NAME_SCRAPHELIS";

        public const string MSG_DROP_NAME_ROWBOAT = "MSG_DROP_NAME_ROWBOAT";
        public const string MSG_DROP_NAME_ROWBOATS = "MSG_DROP_NAME_ROWBOATS";

        public const string MSG_DROP_NAME_RHIB = "MSG_DROP_NAME_RHIB";
        public const string MSG_DROP_NAME_RHIBS = "MSG_DROP_NAME_RHIBS";

        public const string MSG_DROP_NAME_CRATE = "MSG_DROP_NAME_CRATE";
        public const string MSG_DROP_NAME_CRATES = "MSG_DROP_NAME_CRATES";

        public const string MSG_DROP_NAME_SOLO_SUB = "MSG_DROP_NAME_SOLO_SUB";
        public const string MSG_DROP_NAME_DUO_SUB = "MSG_DROP_NAME_DUO_SUB";

        public const string MSG_DROP_NAME_SOLO_SUBS = "MSG_DROP_NAME_SOLO_SUBS";
        public const string MSG_DROP_NAME_DUO_SUBS = "MSG_DROP_NAME_DUO_SUBS";

        public const string MSG_PLEASE_WAIT_TILL_LANDS = "MSG_PLEASE_WAIT_TILL_LANDS";
        public const string MSG_PLEASE_WAIT_TILL_X_LANDS = "MSG_PLEASE_WAIT_TILL_X_LANDS";
        public const string MSG_UNLOCK_AFTER = "MSG_UNLOCK_AFTER";
        public const string MSG_BELONGS_TO_SOMEONE_ELSE = "MSG_BELONGS_TO_SOMEONE_ELSE";
        public const string MSG_WILL_UNLOCK_IN = "MSG_WILL_UNLOCK_IN";

        public const string MSG_VABUY_DISABLED = "MSG_VABUY_DISABLED";
        public const string MSG_VABUY_COST = "MSG_VABUY_COST";
        public const string MSG_VABUY_NO_SIGNALS = "MSG_VABUY_NO_SIGNALS";
        public const string MSG_VABUY_YOU_JUST_BOUGHT = "MSG_VABUY_YOU_JUST_BOUGHT";
        public const string MSG_VABUY_ECONOMICS_CURRENCY = "MSG_VABUY_ECONOMICS_CURRENCY";
        public const string MSG_VABUY_SERVER_REWARDS_CURRENCY = "MSG_VABUY_SERVER_REWARDS_CURRENCY";
        public const string MSG_VABUY_YOU_CAN_BUY = "MSG_VABUY_YOU_CAN_BUY";

        public const string MSG_VABUY_YOU_CANT_BUY = "MSG_VABUY_YOU_CANT_BUY";
        public const string MSG_VABUY_WRONG_DROP = "MSG_VABUY_WRONG_DROP";

        public const string MSG_YOU_GAVE_SIGNAL_TO_SOMEONE = "MSG_YOU_GAVE_SIGNAL_TO_SOMEONE";

        public const string MSG_YOU_RECEIVED_SIGNAL = "MSG_YOU_RECEIVED_SIGNAL";
        public const string MSG_YOU_GAVE_YOURSELF = "MSG_YOU_GAVE_YOURSELF";
        public const string MSG_THIS_KIND_DOESNT_EXIST = "MSG_THIS_KIND_DOESNT_EXIST";

        public const string MSG_PLEASE_SPECIFY = "MSG_PLEASE_SPECIFY_NEW";
        public const string MSG_YOU_HAVE_DROPPED = "MSG_YOU_HAVE_DROPPED";
        public const string MSG_DIRECTION_ABOVE = "MSG_DIRECTION_ABOVE";
        public const string MSG_DIRECTION_BELOW = "MSG_DIRECTION_BELOW";

        public const string MSG_ANNOUNCE_MESSAGE_FORMAT_KIND = "MSG_ANNOUNCE_MESSAGE_FORMAT_KIND";
        public const string MSG_ANNOUNCE_MESSAGE_FULL = "MSG_ANNOUNCE_MESSAGE_FULL";
        public const string MSG_ANNOUNCE_MESSAGE_FULL_PRIVATE = "MSG_ANNOUNCE_MESSAGE_FULL_PRIVATE";
        public const string MSG_NOT_ENOUGH_INVENTORY_SPACE = "MSG_NOT_ENOUGH_INVENTORY_SPACE";



        public static Dictionary<string, string> LangMessages = new Dictionary<string, string>
        {
            [MSG_DROP_NAME_CAR] = "Modular Car",
            [MSG_DROP_NAME_CARS] = "Modular Cars",

            [MSG_NO_PERMISSION] = "You don't have permission to use this command",

            [MSG_DROP_NAME_NORMAL] = "Normal Drop",

            [MSG_DROP_NAME_MINICOPTER] = "Minicopter",
            [MSG_DROP_NAME_MINICOPTERS] = "Minicopters",

            [MSG_DROP_NAME_SCRAPHELI] = "Scrapheli",
            [MSG_DROP_NAME_SCRAPHELIS] = "Scraphelis",

            [MSG_DROP_NAME_ROWBOAT] = "Rowboat",
            [MSG_DROP_NAME_ROWBOATS] = "Rowboats",

            [MSG_DROP_NAME_RHIB] = "RHIB",
            [MSG_DROP_NAME_RHIBS] = "RHIBs",

            [MSG_DROP_NAME_CRATE] = "Codelock Crate",
            [MSG_DROP_NAME_CRATES] = "Codelock Crates",

            [MSG_DROP_NAME_DUO_SUB] = "Duo Sub",
            [MSG_DROP_NAME_SOLO_SUB] = "Solo Sub",
            [MSG_DROP_NAME_DUO_SUBS] = "Duo Subs",
            [MSG_DROP_NAME_SOLO_SUBS] = "Solo Subs",

            [MSG_PLEASE_WAIT_TILL_LANDS] = "Please wait till the drop lands.",
            [MSG_PLEASE_WAIT_TILL_X_LANDS] = "Please wait till the {0} lands.",
            [MSG_UNLOCK_AFTER] = "After it lands, it will unlock for all players in {0} seconds.",
            [MSG_BELONGS_TO_SOMEONE_ELSE] = "This drop belongs to someone else. ",
            [MSG_WILL_UNLOCK_IN] = "It will unlock in {0} seconds.",

            [MSG_VABUY_DISABLED] = "The chat command shop is currently disabled on the server.",
            [MSG_VABUY_COST] = "<color=green>/vabuy</color> <color=yellow>{0}</color>          <i>COST: <color=red>{1}</color></i>",
            [MSG_VABUY_NO_SIGNALS] = "<color=red>There's no supply signals available to buy through this command.</color>",
            [MSG_VABUY_YOU_JUST_BOUGHT] = "<color=green>You just bought a</color> <color=yellow>{0}</color> Supply Signal.",
            [MSG_VABUY_ECONOMICS_CURRENCY] = "¤",
            [MSG_VABUY_SERVER_REWARDS_CURRENCY] = "RP",
            [MSG_VABUY_YOU_CAN_BUY] = "You can buy these supply signals with <color=red>{0}</color> (you currently have <color=red>{1}</color>):",

            [MSG_VABUY_YOU_CANT_BUY] = "<color=red>You can't buy a</color> <color=yellow>{0}</color> <color=red>Supply Signal - either you can't afford it or don't have the permission.</color> Type <color=green>/vabuy</color> to see if there's anything else you can purchase.",
            [MSG_VABUY_WRONG_DROP] = "<color=red>Wrong kind of drop.</color> Type <color=green>/vabuy</color> to get the full list of names and prices.",

            [MSG_YOU_GAVE_SIGNAL_TO_SOMEONE] = "You have given {0} a <color=yellow>{1}</color> Supply Signal",

            [MSG_YOU_RECEIVED_SIGNAL] = "You have received a <color=yellow>{0}</color> Supply Signal from an admin",
            [MSG_YOU_GAVE_YOURSELF] = "You gave yourself a <color=yellow>{0}</color> Supply Signal.",
            [MSG_THIS_KIND_DOESNT_EXIST] = "This kind doesn't exist.",
            [MSG_PLEASE_SPECIFY] = "Please specify the kind: <color=yellow>random</color>, <color=yellow>minicopter</color>, <color=yellow>scrapheli</color>, <color=yellow>rowboat</color>, <color=yellow>rhib</color>, <color=yellow>car</color>, <color=yellow>solosub</color>, <color=yellow>duosub</color>  or <color=yellow>crate</color>.",
            [MSG_YOU_HAVE_DROPPED] = "You have spawned a <color=yellow>{0}</color> drop {1}m {2} where you are.",
            [MSG_DIRECTION_ABOVE] = "above",
            [MSG_DIRECTION_BELOW] = "below",

            [MSG_ANNOUNCE_MESSAGE_FORMAT_KIND] = "<color=yellow>{0}</color>",
            [MSG_ANNOUNCE_MESSAGE_FULL] = "Cargo Plane with a {0} drop inbound!",
            [MSG_ANNOUNCE_MESSAGE_FULL_PRIVATE] = "Cargo Plane with a {0} drop inbound! <color=red>(PRIVATE, unlocks after {1}s)</color>",
            [MSG_NOT_ENOUGH_INVENTORY_SPACE] = "You don't have enough space in your inventory.",

        };

        #endregion

        #region 3RD PARTY
        [PluginReference]
        private Plugin ServerRewards;
        [PluginReference]
        private Plugin Economics;

        #endregion
        #region ImageLibrary
        /*
        [PluginReference] private Plugin ImageLibrary;

        public string GetImage(string name)
        {
            string cached = (string)ImageLibrary?.Call("GetImage", GetNameFromURL(name));
            if (cached == null)
            {
                return ParseURL(name);
            }
            else return cached;
        }

        public void AddImage(string name)
        {
            if (!(bool)ImageLibrary.Call("HasImage", GetNameFromURL(name), (ulong)ResourceId))
            {
                ImageLibrary.Call("AddImage", name, GetNameFromURL(name), (ulong)ResourceId, new Action(DoneLoadingImage));
            }
            else
            {
                DoneLoadingImage();
            }
        }

        public static string GetNameFromURL(string url)
        {
            var splitted = url.Split('/');
            var endUrl = splitted[splitted.Length - 1];
            var name = endUrl.Split('.')[0];
            return name;
        }

        public static string ParseURL(string url)
        {
            var ret = $"{System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(HTTPS))}{url}";
            return ret;
        }

        public void LoadImages()
        {
            if (ImageLibrary != null)
            {
                AddImage(ParseURL("https://i.imgur.com/1unAPZK.png"));
            }
        }*/

        public void DoneLoadingImage()
        {
            Instance.PrintWarning("Loading GUI...");
            GUI = new GuiManager();
        }
        #endregion
        #region PERMISSIONS
        public static readonly string PERMISSION_ADMIN = "vehicleairdrops.admin";
        public static readonly string PERMISSION_VIP = "vehicleairdrops.vip";

        public bool HasPermission(BasePlayer player, string permissionName)
        {
            if (player?.net?.connection?.authLevel > 0) return true;

            return permission.UserHasPermission(player.UserIDString, permissionName);
        }
        #endregion
        #region STATIC
        private static VehicleAirdrops Instance;

        public static LayerMask CollisionLayer = LayerMask.GetMask("Water", "Tree", "Debris", "Clutter", "Default", "Resource", "Construction", "Terrain", "World", "Deployed");

        public static Dictionary<int, string> ItemIDtoItemName = new Dictionary<int, string>
        {
            [1553078977] = "bleach",
            [1776460938] = "blood",
            [1401987718] = "ducttape",
            [-1899491405] = "glue",
            [642482233] = "sticks",
            [-1779183908] = "paper",
            [-751151717] = "chickenspoiled",
            [-1167031859] = "wolfmeatspoiled",

            [656371026] = "carburetor3",
            [1158340332] = "crankshaft3",
            [1883981800] = "pistons3",
            [1072924620] = "sparkplugs3",
            [-1802083073] = "valves3",

            [656371027] = "carburetor2",
            [1158340331] = "crankshaft2",
            [1883981801] = "pistons2",
            [-493159321] = "sparkplugs2",
            [926800282] = "valves2",

            [656371028] = "carburetor1",
            [1158340334] = "crankshaft1",
            [1883981798] = "pistons1",
            [-89874794] = "sparkplugs1",
            [1330084809] = "valves1",

        };
        //BOTH I4 AND V8 ENGINES
        public static readonly int MODULAR_SLOT_CRANKSHAFT = 0;
        public static readonly int MODULAR_SLOT_CARBURETOR = 1;
        public static readonly int MODULAR_SLOT_SPARKPLUGS_A = 2;
        public static readonly int MODULAR_SLOT_VALVES_A = 3;
        public static readonly int MODULAR_SLOT_PISTONS_A = 4;
        //ONLY THE V8 ENGINE HAS THOSE
        public static readonly int MODULAR_SLOT_SPARKPLUGS_B = 5;
        public static readonly int MODULAR_SLOT_VALVES_B = 6;
        public static readonly int MODULAR_SLOT_PISTONS_B = 7;


        public static Dictionary<string, int> ItemNameToItemID = new Dictionary<string, int>
        {
            ["bleach"] = 1553078977,
            ["blood"] = 1776460938,
            ["ducttape"] = 1401987718,
            ["glue"] = -1899491405,
            ["sticks"] = 642482233,
            ["paper"] = -1779183908,
            ["chickenspoiled"] = -751151717,
            ["wolfmeatspoiled"] = -1167031859,

            ["carburetor3"] = 656371026,
            ["crankshaft3"] = 1158340332,
            ["pistons3"] = 1883981800,
            ["sparkplugs3"] = 1072924620,
            ["valves3"] = -1802083073,

            ["carburetor2"] = 656371027,
            ["crankshaft2"] = 1158340331,
            ["pistons2"] = 1883981801,
            ["sparkplugs2"] = -493159321,
            ["valves2"] = 926800282,

            ["carburetor1"] = 656371028,
            ["crankshaft1"] = 1158340334,
            ["pistons1"] = 1883981798,
            ["sparkplugs1"] = -89874794,
            ["valves1"] = 1330084809
        };

        public static Dictionary<string, string> KindToFakeItemName = new Dictionary<string, string>
        {
            ["minicopter"] = "bleach",
            ["scrapheli"] = "blood",
            ["rowboat"] = "ducttape",
            ["rhib"] = "glue",
            ["car"] = "sticks",
            ["crate"] = "paper",
            ["solosub"] = "chickenspoiled",
            ["duosub"] = "wolfmeatspoiled"
        };

        public static Dictionary<string, string> FakeItemNameToKind = new Dictionary<string, string>
        {
            ["bleach"] = "minicopter",
            ["blood"] = "scrapheli",
            ["ducttape"] = "rowboat",
            ["glue"] = "rhib",
            ["sticks"] = "car",
            ["paper"] = "crate",
            ["chickenspoiled"] = "solosub",
            ["wolfmeatspoiled"] = "duosub"
        };

        /**/

        public static Dictionary<ulong, string> SkinIDToItemName = new Dictionary<ulong, string>
        {
            [2144524645] = "minicopter",
            [2144547783] = "scrapheli",
            [2144555007] = "rowboat",
            [2144558893] = "rhib",
            [2144560388] = "car",
            //[2146662467] = "horse",
            [2146665840] = "crate",
            [2567551241] = "solosub",
            [2567552797] = "duosub",

        };

        public static Dictionary<string, ulong> ItemNameToSkinID = new Dictionary<string, ulong>
        {
            ["minicopter"] = 2144524645,
            ["scrapheli"] = 2144547783,
            ["rowboat"] = 2144555007,
            ["rhib"] = 2144558893,
            ["car"] = 2144560388,
            //[2146662467] = "horse",
            ["crate"] = 2146665840,
            ["solosub"] = 2567551241,
            ["duosub"] = 2567552797,
        };

        public static Dictionary<string, string> ShortPrefabName = new Dictionary<string, string>
        {
            ["minicopter"] = "assets/content/vehicles/minicopter/minicopter.entity.prefab",
            ["scrapheli"] = "assets/content/vehicles/scrap heli carrier/scraptransporthelicopter.prefab",
            ["rowboat"] = "assets/content/vehicles/boats/rowboat/rowboat.prefab",
            ["rhib"] = "assets/content/vehicles/boats/rhib/rhib.prefab",
            ["car"] = "assets/content/vehicles/sedan_a/sedantest.entity.prefab",
            //["horse"] = "assets/rust.ai/nextai/testridablehorse.prefab",
            ["crate"] = "assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab",
            ["parachute"] = "assets/prefabs/misc/parachute/parachute.prefab",
            ["2module"] = "assets/content/vehicles/modularcar/2module_car_spawned.entity.prefab",
            ["3module"] = "assets/content/vehicles/modularcar/3module_car_spawned.entity.prefab",
            ["4module"] = "assets/content/vehicles/modularcar/4module_car_spawned.entity.prefab",
            ["solosub"] = "assets/content/vehicles/submarine/submarinesolo.entity.prefab",
            ["duosub"] = "assets/content/vehicles/submarine/submarineduo.entity.prefab"
        };

        public static class SignalDefinitions
        {
            public static Dictionary<string, SignalDefinition> definitions;

            public static void GetTranslatedDefinitions()
            {
                definitions = new Dictionary<string, SignalDefinition>
                {
                    ["minicopter"] = new SignalDefinition { itemSuffix = MSG(MSG_DROP_NAME_MINICOPTER), skinID = 2144524645 },
                    ["scrapheli"] = new SignalDefinition { itemSuffix = MSG(MSG_DROP_NAME_SCRAPHELI), skinID = 2144547783 },
                    ["rowboat"] = new SignalDefinition { itemSuffix = MSG(MSG_DROP_NAME_ROWBOAT), skinID = 2144555007 },
                    ["rhib"] = new SignalDefinition { itemSuffix = MSG(MSG_DROP_NAME_RHIB), skinID = 2144558893 },
                    ["car"] = new SignalDefinition { itemSuffix = MSG(MSG_DROP_NAME_CAR), skinID = 2144560388 },
                    //["horse"] = new SignalDefinition { itemSuffix = "Horse", skinID = 2146662467 },
                    ["crate"] = new SignalDefinition { itemSuffix = MSG(MSG_DROP_NAME_CRATE), skinID = 2146665840 },
                    ["solosub"] = new SignalDefinition { itemSuffix = MSG(MSG_DROP_NAME_SOLO_SUB), skinID = 2567551241 },
                    ["duosub"] = new SignalDefinition { itemSuffix = MSG(MSG_DROP_NAME_DUO_SUB), skinID = 2567552797 },
                    ["normal"] = new SignalDefinition { itemSuffix = MSG(MSG_DROP_NAME_NORMAL), skinID = 0}
                };
            }
        }

        public struct SignalDefinition
        {
            public ulong skinID;
            public string itemSuffix;
        }

        //lookups

        //public static Dictionary<uint, CustomSignal> CustomSignals;// = new Dictionary<uint, CustomSignal>();
        public static Dictionary<BaseEntity, string> CustomCargoPlanes;// = new Dictionary<BaseEntity, string>();
        public static Dictionary<uint, CustomDrop> EntityIDToCustomDrop;// = new Dictionary<uint, CustomDrop>();
        //same as above, but used for quick iterating when unloading
        public static Dictionary<uint, CustomDrop> JustMainEntityIDToCustomDrop;// = new Dictionary<uint, CustomDrop>();
        public static Dictionary<uint, CustomNormalDrop> EntityIDToNormalDrop;// = new Dictionary<uint, CustomNormalDrop>();

        #endregion
        #region CONFIG
        private ConfigData configData;

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Generating default config...");
            configData = new ConfigData();
            SaveConfigData();
        }
        private void ProcessConfigData()
        {
            bool updateConfig = false;
            string oldVersion = configData.version;

            bool migrateStage1 = false;
            bool migrateStage2 = false;
            bool migrateStage3 = false;
            bool migrateStage4 = false;
            bool migrateStage5 = false;

            if (configData.version == "1.0.0")
            {
                migrateStage1 = true;
                migrateStage2 = true;
            }
            else if (configData.version == "1.0.1")
            {
                migrateStage2 = true;
            }
            else if (configData.version == "1.0.2")
            {
                migrateStage3 = true;
            }
            else if (configData.version == "1.0.3")
            {
                migrateStage4 = true;
            }
            else if (configData.version == "1.0.4")
            {
                migrateStage5 = true;
            }


            if (migrateStage1)
            {
                //add plane speed and plane height in 1.0.1
                configData.planeSpeed = 1.0F;
                configData.planeHeight = 250F;
            }

            if (migrateStage2)
            {
                //add locked crate timer and option to announce drops in chat in 1.0.2
                configData.crateSeconds = 900F;
                configData.announceDropsInChat = true;
                configData.chatIcon = 0;
                configData.use3rdPartyStacking = false;
            }

            //migrateStage3 does nothing

            if (migrateStage4)
            {
                configData.enableFireworks = true;
                configData.enableFireworksForPrivateToo = false;
                configData.launchFireworkEveryXSec = 3F;
                configData.maxFireworkShowDuration = 180F;
                configData.maxNumberOfFireworks = 40;
                configData.fireworkPrebabEnabled = new Dictionary<string, bool>
                {
                    [PREFAB_FIREWORK_BLUE] = true,
                    [PREFAB_FIREWORK_RED] = true,
                    [PREFAB_FIREWORK_GREEN] = true,
                    [PREFAB_FIREWORK_VIOLET] = true,
                    [PREFAB_FIREWORK_ORANGE] = true,
                    [PREFAB_FIREWORK_CHAMPAGNE] = true,
                    [PREFAB_ROMAN_BLUE] = true,
                    [PREFAB_ROMAN_GREEN] = true,
                    [PREFAB_ROMAN_VIOLET] = true
                };

            }

            if (migrateStage5)
            {
                if (!configData.dropConfigs.ContainsKey("solosub"))
                {
                    configData.dropConfigs.Add("solosub", new DropConfig
                    {
                        name = "solosub",
                        applyToNatural = false,
                        priceCurrency = 200,
                        priceEconomics = 200,
                        priceServerRewards = 2,
                        randomDropWeight = 0F,
                        randomLootWeight = 1F,
                        comesWithFuel = 50,
                        comesWithHealth = 1.0F,
                        enableInVendingMachine = false,
                        enableInCommandShop = true,
                        currencyItemID = -932201673,
                        vendingMachineRestockQuantity = 10,
                        permission = null
                    });
                }

                if (!configData.dropConfigs.ContainsKey("duosub"))
                {
                    configData.dropConfigs.Add("duosub", new DropConfig
                    {
                        name = "duosub",
                        priceCurrency = 400,
                        priceEconomics = 400,
                        priceServerRewards = 4,
                        randomDropWeight = 0F,
                        randomLootWeight = 1F,
                        comesWithFuel = 100,
                        comesWithHealth = 1.0F,
                        enableInVendingMachine = false,
                        enableInCommandShop = true,
                        currencyItemID = -932201673,
                        vendingMachineRestockQuantity = 10,
                        permission = null
                    });
                }
            }

            updateConfig = migrateStage1 || migrateStage2 || migrateStage3 || migrateStage4 || migrateStage5 || configData.version == "1.0.5";

            if (updateConfig)
            {
                //update the version
                configData.version = VERSION;

                //and save config data
                SaveConfigData();
                Instance.PrintWarning($"Config data was migrated from {oldVersion} to {VERSION} succesfully.");
            }

            configData.fireworkPrefabList = configData.fireworkPrebabEnabled.Where(e => e.Value == true).ToDictionary(e => e.Key, e => e.Value).Keys.ToList();
        }
        private void LoadConfigData()
        {
            PrintWarning("Loading configuration file...");
            try
            {
                configData = Config.ReadObject<ConfigData>();
            }
            catch
            {
                configData = new ConfigData();
            }


            //SaveConfigData();
        }

        private void SaveConfigData()
        {
            Config.WriteObject(configData, true);
        }

        public class ExtraSettingsModular
        {
            [JsonProperty("Weighted chance that the dropped vehicle will have a 2-module chassis")]
            public float randomWeight2modules;
            [JsonProperty("Weighted chance that the dropped vehicle will have a 3-module chassis")]
            public float randomWeight3modules;
            [JsonProperty("Weighted chance that the dropped vehicle will have a 4-module chassis")]
            public float randomWeight4modules;

            //small engine + cockpit module
            [JsonProperty("Cockpit module (small engine) comes with random parts")]
            public bool moduleCockpitEngineComesWithParts; //small engine
            [JsonProperty("Cockpit module's weighted chances of dropping with this crankshaft: (crankshaft1=low, crankshaft2=medium, crankshaft3=high quality, none=no crankshaft")]
            public Dictionary<string, float> moduleCockpitEngineCrankshaftChances;
            [JsonProperty("Cockpit module's weighted chances of dropping with this carburetor: (carburetor1=low, carburetor2=medium, carburetor3=high quality, none=no carburetor")]
            public Dictionary<string, float> moduleCockpitEngineCarburetorChances;
            [JsonProperty("Cockpit module's weighted chances of dropping with these valves: (valves1=low, valves2=medium, valves3=high quality, none=no valves")]
            public Dictionary<string, float> moduleCockpitEngineValvesChances;
            [JsonProperty("Cockpit module's weighted chances of dropping with these sparkplugs: (sparkplugs1=low, sparkplugs2=medium, sparkplugs3=high quality, none=no sparkplugs")]
            public Dictionary<string, float> moduleCockpitEngineSparkplugsChances;
            [JsonProperty("Cockpit module's weighted chances of dropping with these pistons: (pistons1=low, pistons2=medium, pistons3=high quality, none=no pistons")]
            public Dictionary<string, float> moduleCockpitEnginePistonsChances;

            [JsonProperty("Engine module (big engine, so it has double valves, pistons and sparkplugs!) comes with random parts")]
            public bool moduleBigEngineComesWithParts; //big engine - double valves, pistons and sparkplugs
            //[JsonProperty("Engine module's weighted chances of dropping with this crankshaft: (crankshaft1=low, crankshaft2=medium, crankshaft3=high quality, none=no crankshaft")]
            //public Dictionary<string, float> moduleBigEngineCrankshaftChances;
            //[JsonProperty("Engine module's weighted chances of dropping with this carburetor: (carburetor1=low, carburetor2=medium, carburetor3=high quality, none=no carburetor")]
            //public Dictionary<string, float> moduleBigEngineCarburetorChances;
            //[JsonProperty("Engine module's weighted chances of dropping with these valves A: (valves1=low, valves2=medium, valves3=high quality, none=no valves")]
            //public Dictionary<string, float> moduleBigEngineValvesAChances;
            [JsonProperty("Engine module's weighted chances of dropping with these valves B: (valves1=low, valves2=medium, valves3=high quality, none=no valves")]
            public Dictionary<string, float> moduleBigEngineValvesBChances;
            //[JsonProperty("Engine module's weighted chances of dropping with these sparkplugs A: (sparkplugs1=low, sparkplugs2=medium, sparkplugs3=high quality, none=no sparkplugs")]
            //public Dictionary<string, float> moduleBigEngineSparkplugsAChances;
            [JsonProperty("Engine module's weighted chances of dropping with these sparkplugs B: (sparkplugs1=low, sparkplugs2=medium, sparkplugs3=high quality, none=no sparkplugs")]
            public Dictionary<string, float> moduleBigEngineSparkplugsBChances;
            [JsonProperty("Engine module's weighted chances of dropping with these pistons A: (pistons1=low, pistons2=medium, pistons3=high quality, none=no pistons")]
            //public Dictionary<string, float> moduleBigEnginePistonsAChances;
            //[JsonProperty("Engine module's weighted chances of dropping with these pistons B: (pistons1=low, pistons2=medium, pistons3=high quality, none=no pistons")]
            public Dictionary<string, float> moduleBigEnginePistonsBChances;
            //needs to be recalculated when config changes/loads
            [JsonIgnore]
            public float randomWeightModulesSum;
            [JsonIgnore]
            public float randomWeightCrankshaftSum;
            [JsonIgnore]
            public float randomWeightCarburetorSum;
            [JsonIgnore]
            public float randomWeightSparkplugsASum;
            [JsonIgnore]
            public float randomWeightSparkplugsBSum;
            [JsonIgnore]
            public float randomWeightValvesASum;
            [JsonIgnore]
            public float randomWeightValvesBSum;
            [JsonIgnore]
            public float randomWeightPistonsASum;
            [JsonIgnore]
            public float randomWeightPistonsBSum;
        }

        public class DropConfig
        {
            [JsonProperty("Price (Currency)")]
            public int priceCurrency;

            [JsonProperty("Price (ServerRewards)")]
            public int priceServerRewards;

            [JsonProperty("Price (Economics)")]
            public int priceEconomics;

            [JsonProperty("Currency item ID (Outpost Vending Machine), default -932201673 (scrap)")]
            public int currencyItemID;

            [JsonProperty("Amount of fuel it drops with (vehicles only)")]
            public int comesWithFuel;
            [JsonProperty("Amount of health it drops with (vehicles only, default 1.0 = full health)")]
            public float comesWithHealth;

            [JsonProperty("Cargo event random weight")]
            public float randomDropWeight;
            [JsonProperty("Loot crate random weight")]
            public float randomLootWeight;

            [JsonProperty("Enable selling in the dedicated vending machine")]
            public bool enableInVendingMachine;

            [JsonProperty("Enable selling through the /vabuy command - if disabled or price is 0, it won't shop up")]
            public bool enableInCommandShop;

            [JsonProperty("Amount when re-stocking the vending machine (0 disables it)")]
            public int vendingMachineRestockQuantity;

            [JsonProperty("Permission needed to use/purchase this kind")]
            public string permission;

            [JsonProperty("DROP NAME ID, LEAVE THIS ALONE OR YOU'RE GONNA HAVE BAD TIME")]
            public string name;

            [JsonProperty("Apply fuel/health/extra settings to vehicles spawned naturally")]
            public bool applyToNatural;

            [JsonProperty("!Extra settings for the drop, only applies to Modular Vehicles")]
            public ExtraSettingsModular extraSettings;
        }

        public class ConfigData
        {
            [JsonProperty("Use 3RD party plugin to handle stack sizes, like StacksExtended, Stack Size Controller etc")]
            public bool use3rdPartyStacking = false;

            [JsonProperty("!version")]
            public string version = VERSION;
            [JsonProperty("Can mount/loot drops when the parachute is still attached")]
            public bool usableWithParachute = false;

            [JsonProperty("Vehicles don't take damage when the parachute is still attached")]
            public bool indestructibleWithParachute = true;

            [JsonProperty("Vehicles take decay damage when the parachute is still attached")]
            public bool decaysWithParachute = false;

            [JsonProperty("When the drop is shot/damaged, detach the parachute")]
            public bool damageDetachesParachute = false;

            [JsonProperty("How far from the thrown signal a custom drop will land, 0 is precise position, 20 is Rust default")]
            public float inaccuracy = 20f;

            [JsonProperty("After a custom drop called with a supply signal lands, only authorised players can mount/loot it for a time period")]
            public bool enablePrivateSignals = true;

            [JsonProperty("Private drops are indestructible until claimed/unlocked from expiration")]
            public bool privateDropsIndestructible = false;

            [JsonProperty("After a custom drop called is made private, authorised players will include the caller's teammates, too")]
            public bool privateSignalsIncludeTeammates = true;

            [JsonProperty("How long for the unauthorised access protection to wear off (in seconds)")]
            public float unlockPrivateDropAfter = 60F;

            [JsonProperty("Fall speed for the drop, default 1.0 is the same speed as normal supply drops")]
            public float fallSpeedWithParachute = 1.0F;

            [JsonProperty("Enable a /vabuy chat command shop for players")]
            public bool enableCommandShop = true;

            [JsonProperty("Default method of payment for the chat command shop (Currency/ServerRewards/Economics), default Currency")]
            public string defaultPayment = "Currency";

            [JsonProperty("Add a vending machine that sells custom supply signals at Outpost")]
            public bool enableVendingMachine = true;

            [JsonProperty("Vending machine at Outpost also sells normal supply signal")]
            public bool vendingMachineHasNormal = true;

            [JsonProperty("Command shop accessed by /vabuy has a normal supply signal in stock")]
            public bool commandShopHasNormal = true;

            [JsonProperty("Price of the normal supply signal (currency, applies both to /vabuy and vending machines)")]
            public int priceNormalCurrency = 400;

            [JsonProperty("Price of the normal supply signal (ServerRewards)")]
            public int priceNormalServerRewards = 4;

            [JsonProperty("Price of the normal supply signal (Economics)")]
            public int priceNormalEconomics = 400;

            [JsonProperty("Permission needed to buy a normal drop (leave null to let everyone buy)")]
            public string permissionNormal = null;

            [JsonProperty("Currency (item ID) of the normal supply signal sold at Outpost, default -932201673 (scrap)")]
            public int normalCurrencyItemID = -932201673;

            [JsonProperty("Amount of the normal supply signal sold at Outpost each time the machine restocks")]
            public int vendingMachineNormalQuantity = 10;

            [JsonProperty("Vending machine location coordinates are relative to first Outpost found")]
            public bool vendingMachineLocationIsRelative = true;

            [JsonProperty("Outpost vending machine name, something like Supply Signals will do")]
            public string vendingMachineCustomName = "Surprise! Supplies";

            [JsonProperty("Vending machine position x")]
            public float vendingMachinePosX = 31.87F;
            [JsonProperty("Vending machine position y")]
            public float vendingMachinePosY = 1.485F;
            [JsonProperty("Vending machine position z")]
            public float vendingMachinePosZ = -11.75F;

            [JsonProperty("Vending machine rotation x")]
            public float vendingMachineRotX = 0F;
            [JsonProperty("Vending machine rotation y")]
            public float vendingMachineRotY = 270F;
            [JsonProperty("Vending machine rotation z")]
            public float vendingMachineRotZ = 0F;

            [JsonProperty("How often to re-stock the vending machine (in minutes)")]
            public uint vendingMachineRestockEvery = 60;

            [JsonProperty("Enable custom drops for random cargo plane events")]
            public bool enableRandomDrop = true;
            [JsonProperty("Enable custom supply signals spawning in loot crates")]
            public bool enableRandomLoot = true;
            [JsonProperty("Chance for a random carge plane drop to be custom (default 0.5 = 50%)")]
            public float customDropChance = 0.5F;
            [JsonProperty("Chance for a supply signal in loot crates to be custom (default 0.5 = 50%)")]
            public float customLootChance = 0.5F;

            [JsonProperty("Custom drop settings by their kind")]
            public Dictionary<string, DropConfig> dropConfigs = new Dictionary<string, DropConfig>
            {
                ["solosub"] = new DropConfig
                {
                    name = "solosub",
                    applyToNatural = false,
                    priceCurrency = 200,
                    priceEconomics = 200,
                    priceServerRewards = 2,
                    randomDropWeight = 0F,
                    randomLootWeight = 1F,
                    comesWithFuel = 50,
                    comesWithHealth = 1.0F,
                    enableInVendingMachine = false,
                    enableInCommandShop = true,
                    currencyItemID = -932201673,
                    vendingMachineRestockQuantity = 10,
                    permission = null
                },

                ["duosub"] = new DropConfig
                {
                    name = "duosub",
                    priceCurrency = 400,
                    priceEconomics = 400,
                    priceServerRewards = 4,
                    randomDropWeight = 0F,
                    randomLootWeight = 1F,
                    comesWithFuel = 100,
                    comesWithHealth = 1.0F,
                    enableInVendingMachine = false,
                    enableInCommandShop = true,
                    currencyItemID = -932201673,
                    vendingMachineRestockQuantity = 10,
                    permission = null
                },

                ["rowboat"] = new DropConfig
                {
                    name = "rowboat",
                    applyToNatural = false,
                    priceCurrency = 100,
                    priceEconomics = 100,
                    priceServerRewards = 1,
                    randomDropWeight = 0F,
                    randomLootWeight = 1F,
                    comesWithFuel = 10,
                    comesWithHealth = 1.0F,
                    enableInVendingMachine = true,
                    enableInCommandShop = true,
                    currencyItemID = -932201673,
                    vendingMachineRestockQuantity = 10,
                    permission = null
                },

                ["rhib"] = new DropConfig
                {
                    name = "rhib",
                    priceCurrency = 500,
                    priceEconomics = 500,
                    priceServerRewards = 5,
                    randomDropWeight = 0F,
                    randomLootWeight = 1F,
                    comesWithFuel = 80,
                    comesWithHealth = 1.0F,
                    enableInVendingMachine = true,
                    enableInCommandShop = true,
                    currencyItemID = -932201673,
                    vendingMachineRestockQuantity = 10,
                    permission = null
                },

                ["minicopter"] = new DropConfig
                {
                    name = "minicopter",
                    applyToNatural = false,
                    priceCurrency = 800,
                    priceEconomics = 800,
                    priceServerRewards = 8,
                    randomDropWeight = 1F,
                    randomLootWeight = 1F,
                    comesWithFuel = 100,
                    comesWithHealth = 1.0F,
                    enableInVendingMachine = true,
                    enableInCommandShop = true,
                    currencyItemID = -932201673,
                    vendingMachineRestockQuantity = 10,
                    permission = null
                },

                ["scrapheli"] = new DropConfig
                {
                    name = "scrapheli",
                    applyToNatural = false,
                    priceCurrency = 1200,
                    priceEconomics = 1200,
                    priceServerRewards = 12,
                    randomDropWeight = 1F,
                    randomLootWeight = 1F,
                    comesWithFuel = 100,
                    comesWithHealth = 1.0F,
                    enableInVendingMachine = true,
                    enableInCommandShop = true,
                    currencyItemID = -932201673,
                    vendingMachineRestockQuantity = 10,
                    permission = null
                },

                ["crate"] = new DropConfig
                {
                    name = "crate",
                    applyToNatural = false,
                    priceCurrency = 2000,
                    priceEconomics = 2000,
                    priceServerRewards = 20,
                    randomDropWeight = 1F,
                    randomLootWeight = 1F,
                    comesWithFuel = 0,
                    comesWithHealth = 1.0F,
                    enableInVendingMachine = true,
                    enableInCommandShop = true,
                    currencyItemID = -932201673,
                    vendingMachineRestockQuantity = 10,
                    permission = null
                },

                //big engines have double pistons, double spark plugs and double valves

                ["car"] = new DropConfig
                {
                    name = "car",
                    applyToNatural = true,
                    priceCurrency = 1000,
                    priceEconomics = 1000,
                    priceServerRewards = 10,
                    randomDropWeight = 1F,
                    randomLootWeight = 1F,
                    comesWithFuel = 40,
                    comesWithHealth = 1.0F,
                    enableInVendingMachine = true,
                    enableInCommandShop = true,
                    currencyItemID = -932201673,
                    vendingMachineRestockQuantity = 10,
                    permission = null,
                    extraSettings = new ExtraSettingsModular
                    {
                        randomWeight2modules = 0.33F,
                        randomWeight3modules = 0.33F,
                        randomWeight4modules = 0.33F,
                        moduleCockpitEngineComesWithParts = true,
                        moduleCockpitEngineCarburetorChances = new Dictionary<string, float>
                        {
                            ["none"] = 0F,
                            ["carburetor1"] = 0.5F,
                            ["carburetor2"] = 0.3F,
                            ["carburetor3"] = 0.2F
                        },
                        moduleCockpitEngineCrankshaftChances = new Dictionary<string, float>
                        {
                            ["none"] = 0F,
                            ["crankshaft1"] = 0.5F,
                            ["crankshaft2"] = 0.3F,
                            ["crankshaft3"] = 0.2F
                        },
                        moduleCockpitEnginePistonsChances = new Dictionary<string, float>
                        {
                            ["none"] = 0F,
                            ["pistons1"] = 0.5F,
                            ["pistons2"] = 0.3F,
                            ["pistons3"] = 0.2F
                        },
                        moduleCockpitEngineSparkplugsChances = new Dictionary<string, float>
                        {
                            ["none"] = 0F,
                            ["sparkplugs1"] = 0.5F,
                            ["sparkplugs2"] = 0.3F,
                            ["sparkplugs3"] = 0.2F
                        },
                        moduleCockpitEngineValvesChances = new Dictionary<string, float>
                        {
                            ["none"] = 0F,
                            ["valves1"] = 0.5F,
                            ["valves2"] = 0.3F,
                            ["valves3"] = 0.2F
                        },
                        moduleBigEngineComesWithParts = true,
                        moduleBigEnginePistonsBChances = new Dictionary<string, float>
                        {
                            ["none"] = 0F,
                            ["pistons1"] = 0.5F,
                            ["pistons2"] = 0.3F,
                            ["pistons3"] = 0.2F
                        },
                        moduleBigEngineSparkplugsBChances = new Dictionary<string, float>
                        {
                            ["none"] = 0F,
                            ["sparkplugs1"] = 0.5F,
                            ["sparkplugs2"] = 0.3F,
                            ["sparkplugs3"] = 0.2F
                        },
                        moduleBigEngineValvesBChances = new Dictionary<string, float>
                        {
                            ["none"] = 0F,
                            ["valves1"] = 0.5F,
                            ["valves2"] = 0.3F,
                            ["valves3"] = 0.2F
                        }

                    }
                },

                //["horse"] = new DropConfig { price = 0, randomDropWeight = 0F, randomLootWeight = 0F, permission = null }
            };

            //1.0.1
            //
            [JsonProperty("Height of the plane, how many meters above map's highest point, 250F is default Rust height")]
            public float planeHeight = 250F;

            [JsonProperty("Speed of the plane, 1.0 is the default Rust speed, 2.0 is twice as fast, etc (0.01F to positive infinity)")]
            public float planeSpeed = 1.0F;

            //1.0.2
            [JsonProperty("How long for a locked crate to unlock, in whole seconds (900 is Rust default)")]
            public float crateSeconds = 900F;
            [JsonProperty("Announce random cargo plane events and called custom supply signals in chat?")]
            public bool announceDropsInChat = true;
            [JsonProperty("Icon ID for the chat (long steam user ID, starting with 7)")]
            public ulong chatIcon = 0;

            //1.0.4
            [JsonProperty("Enable periodical fireworks after the drop has landed to indicate the drop location?")]
            public bool enableFireworks = true;
            [JsonProperty("Fireworks are enabled for private drops, too?")]
            public bool enableFireworksForPrivateToo = false;

            [JsonProperty("When fireworks are enabled, launch a new firework every X amount of seconds")]
            public float launchFireworkEveryXSec = 3F;

            [JsonProperty("Stop launching fireworks after this amount of seconds")]
            public float maxFireworkShowDuration = 180F;

            [JsonProperty("Stop launching fireworks after launching this number of them")]
            public int maxNumberOfFireworks = 40;

            [JsonProperty("Enable Notify plugin capabilities (messages printed in the chat will also be sent there)")]
            public bool enableNotifyPlugin = true;

            [JsonProperty("Choose from the following list of firework prefabs at random, set to false to disable certain kinds")]
            public Dictionary<string, bool> fireworkPrebabEnabled = new Dictionary<string, bool>
            {
                [PREFAB_FIREWORK_BLUE] = true,
                [PREFAB_FIREWORK_RED] = true,
                [PREFAB_FIREWORK_GREEN] = true,
                [PREFAB_FIREWORK_VIOLET] = true,
                [PREFAB_FIREWORK_ORANGE] = true,
                [PREFAB_FIREWORK_CHAMPAGNE] = true,
                [PREFAB_ROMAN_BLUE] = true,
                [PREFAB_ROMAN_GREEN] = true,
                [PREFAB_ROMAN_VIOLET] = true
            };

            [JsonIgnore] //this needs to be re-calculated after loading config
            public List<string> fireworkPrefabList = null;

            //these needs re-calculated when something about them changes
            [JsonIgnore]
            public float weightSumDrop;
            [JsonIgnore]
            public float weightSumLoot;
            [JsonIgnore]
            public Vector3 vendingMachinePos;
            [JsonIgnore]
            public Vector3 vendingMachineRot;
            [JsonIgnore]
            public VendingMachine dedicatedVendingMachine = null;
            [JsonIgnore]
            public DroppedItem dedicatedVendingMachineDummy = null;

        }

        #endregion
        #region MONO
        //attach to a new gameobject after landing
        public class FireworkLauncher : MonoBehaviour
        {
            public int launchedSoFar = 0;
            public float stopLaunchingAt = UnityEngine.Time.realtimeSinceStartup + Instance.configData.maxFireworkShowDuration;

            public float updateRate = Instance.configData.launchFireworkEveryXSec;
            public float lastUpdate = UnityEngine.Time.realtimeSinceStartup;

            private void FixedUpdate()
            {
                if (UnityEngine.Time.realtimeSinceStartup <= lastUpdate + updateRate)
                {
                    return;
                }

                lastUpdate = UnityEngine.Time.realtimeSinceStartup;

                if (UnityEngine.Time.realtimeSinceStartup > stopLaunchingAt)
                {
                    DestroyImmediate(gameObject);
                    return;
                }

                //launch firework

                var randomPrefab = RandomFireworkPrefab();
                if (randomPrefab != null)
                {
                    RaycastHit rayHit;
                    var cast = Physics.Raycast(new Vector3(transform.position.x, transform.position.y + 200, transform.position.z), Vector3.down, out rayHit, 300F, int.MaxValue);

                    if (cast)
                    {
                        var firework = GameManager.server.CreateEntity(randomPrefab, rayHit.point + Vector3.up * 2F) as RepeatingFirework;

                        firework.transform.up = Vector3.up;

                        firework.Spawn();

                        firework.enableSaving = false;

                        firework.ClientRPC(null, "RPCFire");

                        firework.Kill();
                    }

                }

                launchedSoFar++;

                if (launchedSoFar >= Instance.configData.maxNumberOfFireworks)
                {
                    DestroyImmediate(gameObject);
                }
            }
        }

        public class CustomPlane : MonoBehaviour
        {
            public string kind;
            public ulong ownerID;
            public CargoPlane plane;

            void Awake()
            {
                plane = gameObject.GetComponent<CargoPlane>();
                ReplacementUpdateDropPosition();
            }

            public void ReplacementUpdateDropPosition()
            {
                var newDropPosition = plane.dropPosition;

                float x = TerrainMeta.Size.x;
                float num = TerrainMeta.HighestPoint.y + Instance.configData.planeHeight;

                plane.startPos = Vector3Ex.Range(-1f, 1f);
                plane.startPos.y = 0.0f;
                plane.startPos.Normalize();
                plane.startPos *= x * 2f;
                plane.startPos.y = num;
                plane.endPos = plane.startPos * -1f;
                plane.endPos.y = plane.startPos.y;
                plane.startPos += newDropPosition;
                plane.endPos += newDropPosition;
                plane.secondsToTake = Vector3.Distance(plane.startPos, plane.endPos) / 50f;
                plane.secondsToTake *= UnityEngine.Random.Range(0.95f, 1.05f);

                if (Instance.configData.planeSpeed < 0.01F)
                {
                    Instance.configData.planeSpeed = 0.01F;
                }

                plane.secondsToTake /= Instance.configData.planeSpeed;
                plane.transform.position = plane.startPos;
                plane.transform.rotation = Quaternion.LookRotation(plane.endPos - plane.startPos);
                plane.dropPosition = newDropPosition;
                plane.SendNetworkUpdateImmediate();
            }
        }

        public class CustomNormalDrop : MonoBehaviour
        {
            public ulong ownerID;
            public float lastUpdated;
            public float currentTime;
            public float updateEvery = 0.1F / Instance.configData.fallSpeedWithParachute;
            public SupplyDrop supplyDrop;
            public Rigidbody rigidbody;
            public float landDrag;

            public bool alreadyDestroying = false;
            public float destroyTimer = 1.0F;

            public float shouldUnlockAtTime;

            public bool active = false;

            public bool landed = false;

            public uint personalGarbage;
            void Awake()
            {
                supplyDrop = gameObject.GetComponent<SupplyDrop>();
                rigidbody = supplyDrop.gameObject.GetComponent<Rigidbody>();

                personalGarbage = supplyDrop.net.ID;
                EntityIDToNormalDrop.Add(personalGarbage, this);

                var quotient = Instance.configData.fallSpeedWithParachute;
                if (quotient < 0.01F) quotient = 0.01F;

                var calculatedDrag = 2 / quotient;
                landDrag = rigidbody.drag;
                rigidbody.drag = calculatedDrag;
            }

            void OnDestroy()
            {
                EntityIDToNormalDrop.Remove(personalGarbage);
            }

            private void OnCollisionEnter(Collision collision)
            {
                alreadyDestroying = true;
                rigidbody.velocity = Vector3.zero;
                rigidbody.drag = 0; //drop.landDrag;
            }

            void FixedUpdate()
            {
                //activate after 5 seconds of dropping down
                var takeUpdateRate = alreadyDestroying ? 1.0F : updateEvery;// isModularVehicle ? updateEvery : (active ? (alreadyDestroying ? destroyTimer : updateEvery) : 5.0F);

                currentTime = Time.realtimeSinceStartup;

                if (!landed)
                {

                    if (currentTime < lastUpdated + takeUpdateRate) return;

                    if (!active)
                    {
                        active = true;
                    }
                    else
                    {
                        if (supplyDrop == null) DestroyImmediate(this);

                        if (!alreadyDestroying)
                        {

                            var vecDown = Vector3.down;
                            var radius = 1F;

                            var colliders = Physics.OverlapSphere(transform.position + vecDown, radius, CollisionLayer);

                            if (colliders.Any())
                            {
                                alreadyDestroying = true;
                                rigidbody.velocity = Vector3.zero;
                                rigidbody.drag = 0; //drop.landDrag;
                                //if you're a modular vehicle, kinematic false, use gravity true.
                            }

                        }
                        else
                        {
                            Instance.DetachParachute(this);
                        }
                    }
                }
                else
                {
                    if (currentTime > shouldUnlockAtTime)
                    {
                        DestroyImmediate(this);
                    }
                }

                lastUpdated = currentTime;
            }
        }

        public class CustomDrop : MonoBehaviour
        {
            public float lastUpdated;
            public float currentTime;

            public string kind;

            public ulong ownerID;

            public float updateEvery = 0.1F / Instance.configData.fallSpeedWithParachute;

            public BaseVehicle vehicle;

            public ModularCar modularCar;

            public HackableLockedCrate crate;
            public Rigidbody crateRigidbody;

            public BaseEntity parachute;
            public DroppedItem dummyPivot;

            public bool useDummyPivot = false;

            public float landDrag;
            public float waterDrag;

            public Vector3 horseCorrection = Vector3.zero;

            public bool isCrate = false;

            public bool isModularVehicle = false;
            public BasePlayer dummyPlayer = null;

            public bool isScrapHeli = false;

            public bool alreadyDestroying = false;
            public float destroyTimer = 1.0F;

            public float shouldUnlockAtTime;

            public bool active = false;

            public bool landed = false;

            public List<uint> personalGarbage = new List<uint>();

            void OnDestroy()
            {
                JustMainEntityIDToCustomDrop.Remove(personalGarbage.First());

                foreach (var rubbish in personalGarbage)
                {
                    EntityIDToCustomDrop.Remove(rubbish);
                }

            }

            void Awake()
            {
                vehicle = gameObject.GetComponent<BaseVehicle>();

                MotorRowboat maybeBoat = null;
                ModularCar maybeModular = null;
                ScrapTransportHelicopter maybeScrapheli = null;

                BaseSubmarine maybeSubmarine = null;

                if (vehicle == null)
                {
                    isCrate = true;
                    crate = gameObject.GetComponent<HackableLockedCrate>();
                }
                else
                {
                    maybeBoat = vehicle as MotorRowboat;
                    maybeModular = vehicle as ModularCar;
                    maybeScrapheli = vehicle as ScrapTransportHelicopter;
                    maybeSubmarine = vehicle as BaseSubmarine;
                    

                    if (maybeScrapheli != null)
                    {
                        isScrapHeli = true;
                    }
                    else
                    {
                        isScrapHeli = false;
                    }
                }
                //boats need more drag for some reason, otherwise they just plummet down

                var quotient = Instance.configData.fallSpeedWithParachute;
                if (quotient < 0.01F) quotient = 0.01F;


                var calculatedDrag = 2 / quotient;

                if (maybeBoat != null)
                {
                    landDrag = maybeBoat.landDrag;
                    waterDrag = maybeBoat.waterDrag;

                    maybeBoat.landDrag *= 5 * calculatedDrag;
                    maybeBoat.waterDrag *= 5 * calculatedDrag;
                }
                else
                {
                    if (maybeModular != null)
                    {
                        isModularVehicle = true;
                        this.modularCar = maybeModular;

                        maybeModular.rigidBody.useGravity = false;
                        //maybeModular.rigidBody.isKinematic = false;
                        maybeModular.rigidBody.drag = 0;
                    }
                    else
                    if (isCrate)
                    {
                        //useDummyPivot = true;
                        //dummyPivot = Instance.DummyCreate(crate.transform.position + horseCorrection, crate.transform.eulerAngles);


                        //crate.SetParent(dummyPivot);

                        //crate.transform.localPosition = -horseCorrection;

                        //crate.SendNetworkUpdateImmediate();
                        crateRigidbody = crate.GetComponent<Rigidbody>();
                        landDrag = crateRigidbody.drag;
                        crateRigidbody.drag = calculatedDrag;

                        //crate timer?
                        //crate.StartHacking();

                        crate.hackSeconds = HackableLockedCrate.requiredHackSeconds - Instance.configData.crateSeconds;
                    }
                    else
                    {
                        if (vehicle.rigidBody == null)
                        {
                            //need to use a dummy pivot
                            useDummyPivot = true;
                            dummyPivot = Instance.DummyCreate(vehicle.transform.position + horseCorrection, vehicle.transform.eulerAngles);

                            vehicle.SetParent(dummyPivot, false, true);

                            vehicle.transform.localPosition = -horseCorrection;

                            vehicle.transform.hasChanged = true;

                            //vehicle.SendNetworkUpdateImmediate();
                        }
                        else
                        {
                            vehicle.SendNetworkUpdateImmediate();
                            vehicle.UpdateNetworkGroup();

                            if (isModularVehicle)
                            {

                            }
                            else
                            {
                                landDrag = vehicle.rigidBody.drag;
                                vehicle.rigidBody.drag = calculatedDrag;
                            }
                        }
                    }
                }


                parachute = GameManager.server.CreateEntity("assets/prefabs/misc/parachute/parachute.prefab", new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 0), true);

                BaseEntity parent = vehicle;

                if (isCrate)
                {
                    parent = crate;
                }

                parachute.Spawn();

                parachute.SetParent(parent);

                parachute.transform.localPosition = Vector3.zero;
                parachute.SendNetworkUpdateImmediate();


                lastUpdated = Time.realtimeSinceStartup;

            }

            void FixedUpdate()
            {
                //activate after 5 seconds of dropping down
                var takeUpdateRate = alreadyDestroying ? 1.0F : updateEvery;// isModularVehicle ? updateEvery : (active ? (alreadyDestroying ? destroyTimer : updateEvery) : 5.0F);

                currentTime = Time.realtimeSinceStartup;

                if (!landed)
                {

                    if (currentTime < lastUpdated + takeUpdateRate) return;

                    if (!active)
                    {
                        active = true;
                    }
                    else
                    {
                        if (isModularVehicle)
                        {
                            if (modularCar.rigidBody.useGravity == false)
                            {
                                //modularCar.rigidBody.transform.position += new Vector3(0, -0.5F, 0);
                                //modularCar.rigidBody.MovePosition(modularCar.rigidBody.transform.position + new Vector3(0, -0.5F, 0));
                                //modularCar.rigidBody.transform.hasChanged = true;
                                modularCar.rigidBody.velocity = Instance.configData.fallSpeedWithParachute * 5 * Vector3.down;
                            }
                        }

                        if (!alreadyDestroying)
                        {
                            /*
                            if (vehicle != null) { vehicle.SendNetworkUpdateImmediate(); vehicle.UpdateNetworkGroup(); }
                            else if (crate != null) { crate.SendNetworkUpdateImmediate(); crate.UpdateNetworkGroup(); }
                            */
                            var vecDown = Vector3.zero;
                            var radius = 1F;

                            if (isModularVehicle)
                            {
                                //vecDown *= 1 + Instance.configData.fallSpeedWithParachute / 5;
                                radius *= 1 + Instance.configData.fallSpeedWithParachute / 10F;
                            }
                            /*
                            foreach (var player in BasePlayer.activePlayerList)
                            {
                                player.SendConsoleCommand("ddraw.sphere", updateEvery, "1 0 0", transform.position + vecDown, radius);
                            }*/


                            var colliders = Physics.OverlapSphere(transform.position + vecDown, radius, CollisionLayer);

                            if (colliders.Any())
                            {
                                alreadyDestroying = true;

                                if (isModularVehicle)
                                {
                                    //modularCar.rigidBody.isKinematic = true;
                                    modularCar.rigidBody.useGravity = true;
                                }

                                if (vehicle != null)
                                {
                                    if (vehicle.rigidBody != null)
                                    {
                                        vehicle.rigidBody.velocity = Vector3.zero;
                                    }
                                }

                                //if you're a modular vehicle, kinematic false, use gravity true.
                            }
                        }
                        else
                        {
                            Instance.DetachParachute(this);
                        }
                    }
                }
                else
                {
                    if (currentTime > shouldUnlockAtTime)
                    {
                        DestroyImmediate(this);
                    }
                }

                lastUpdated = currentTime;
            }
        }

        public class CustomSignal : MonoBehaviour
        {
            public SupplySignal signal;
            public ulong skin;
            public string kind;

            public ulong ownerID = 0;

            void Awake()
            {
                signal = gameObject.GetComponent<SupplySignal>();

                signal.CancelInvoke(new Action(signal.Explode));

                ReplacementSetFuse(signal.GetRandomTimerTime());

            }

            public void ReplacementSetFuse(float fuseLength)
            {
                //if (Interface.CallHook("OnExplosiveFuseSet", (object)this.signal, (object)(fuseLength)) is float num)
                //fuseLength = num;

                Invoke("ReplacementExplode", fuseLength);
            }

            public void ReplacementExplode()
            {
                signal.Invoke(new Action(signal.FinishUp), 210F); //210 is the default time

                BaseEntity plane = GameManager.server.CreateEntity("assets/prefabs/npc/cargo plane/cargo_plane.prefab", new Vector3(), new Quaternion(), true);

                var offset = Instance.configData.inaccuracy / 2;

                Vector3 vector3 = new Vector3(UnityEngine.Random.Range(-offset, offset), 0F, UnityEngine.Random.Range(-offset, offset));

                plane.SendMessage("InitDropPosition", (signal.transform.position + vector3), SendMessageOptions.DontRequireReceiver);

                plane.Spawn();

                signal.SetFlag(BaseEntity.Flags.On, true, false, true);
                signal.SendNetworkUpdateImmediate(false);

                Instance.CustomizePlane(plane as CargoPlane, kind, ownerID);

                //after exploding, no need for the mono
                DestroyImmediate(this);
            }
        }

        #endregion
        #region HOOKS
        void Init()
        {
            permission.RegisterPermission(PERMISSION_ADMIN, this);
            permission.RegisterPermission(PERMISSION_VIP, this);

            lang.RegisterMessages(LangMessages, this);
        }

        private void OnServerInitialized(bool serverInitialized)
        {
            Instance = this;

            lang.RegisterMessages(LangMessages, this);

            //CustomSignals = new Dictionary<uint, CustomSignal>();
            CustomCargoPlanes = new Dictionary<BaseEntity, string>();
            EntityIDToCustomDrop = new Dictionary<uint, CustomDrop>();

            JustMainEntityIDToCustomDrop = new Dictionary<uint, CustomDrop>();
            EntityIDToNormalDrop = new Dictionary<uint, CustomNormalDrop>();

            SignalDefinitions.GetTranslatedDefinitions();

            LoadConfigData();
            ProcessConfigData();

            //calculate weight sums
            CalculateWeightSumsDrop();
            CalculateWeightSumsLoot();
            CalculateWeightSumsEngine();

            //wait a bit. do that in 3 seconds, just in case you reload the plugin too quickly.
            Instance.timer.Once(3.0F, () =>
            {
                CreateVendingMachine();
            });

            //LoadImages();
            DoneLoadingImage();

            //check if stack splitting is handled by a 3rd party plugin
            if (configData.use3rdPartyStacking)
            {
                Instance.PrintWarning("Using a 3RD party plugin to handle stack sizes. If you don't have such a plugin, splitting skinned signal items might not work correctly.");
                Unsubscribe(nameof(OnItemSplit));
            }

        }
        private void KillVendingMachine()
        {
            if (configData.dedicatedVendingMachine != null)
            {
                ClearRestockTimer();
                //RefreshVendingMachine();
                configData.dedicatedVendingMachine.AdminKill();
            }
        }

        private void RefreshVendingMachine()
        {
            if (configData.dedicatedVendingMachine == null) return;

            foreach (var player in BasePlayer.activePlayerList)
            {
                if (Network.Net.sv.write.Start())
                {
                    Network.Net.sv.write.PacketID(Network.Message.Type.EntityDestroy);
                    Network.Net.sv.write.EntityID(configData.dedicatedVendingMachine.net.ID);
                    Network.Net.sv.write.Send(new Network.SendInfo(player.net.connection));
                }
            }

            configData.dedicatedVendingMachine.SendNetworkUpdateImmediate();
            configData.dedicatedVendingMachine.UpdateNetworkGroup();
        }

        private void CreateVendingMachine()
        {

            if (configData.enableVendingMachine)
            {
                configData.vendingMachinePos = new Vector3(configData.vendingMachinePosX, configData.vendingMachinePosY, configData.vendingMachinePosZ);
                configData.vendingMachineRot = new Vector3(configData.vendingMachineRotX, configData.vendingMachineRotY, configData.vendingMachineRotZ);

                if (!configData.vendingMachineLocationIsRelative)
                {
                    configData.dedicatedVendingMachineDummy = DummyCreate(configData.vendingMachinePos, configData.vendingMachineRot, false);
                }
                else
                {
                    //gotta find first outpost.

                    List<MonumentInfo> monumentList = TerrainMeta.Path.Monuments;

                    if (monumentList.Any())
                    {
                        monumentList = monumentList.Where(m => m.name.Contains("compound.prefab")).ToList();

                        if (monumentList.Any())
                        {
                            //just use the first one
                            var outpostMonument = monumentList.FirstOrDefault();

                            configData.dedicatedVendingMachineDummy = DummyCreate(outpostMonument.transform.position + new Vector3(0F, -100F, 0F), outpostMonument.transform.eulerAngles, false);
                        }
                        else
                        {
                            PrintError("ERROR PLACING VENDING MACHINE: The location is supposed to be relative to first Outpost monument, but no Outpost was found on the map! You can still place a supply signal vending machine, just provide your own position/rotation and make sure it's NOT relative ");
                        }

                    }
                    else
                    {
                        PrintError("ERROR PLACING VENDING MACHINE: The location is supposed to be relative to first Outpost monument, but no monuments whatsoever were found on the map! You can still place a supply signal vending machine, just provide your own position/rotation and make sure it's NOT relative ");

                    }
                }


                if (configData.dedicatedVendingMachineDummy != null)
                {
                    var spawnPos = !configData.vendingMachineLocationIsRelative ? configData.vendingMachinePos : Vector3.zero;
                    var spawnQuat = !configData.vendingMachineLocationIsRelative ? Quaternion.Euler(configData.vendingMachineRot) : Quaternion.Euler(Vector3.zero);

                    //spawn a new machine
                    configData.dedicatedVendingMachine = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vendingmachine.deployed.prefab",/*"assets/prefabs/deployable/vendingmachine/npcvendingmachine.prefab"*/ spawnPos, spawnQuat) as VendingMachine;

                    //cancel the invoke
                    //configData.dedicatedVendingMachine.CancelInvoke((Action)Delegate.CreateDelegate(typeof(Action), configData.dedicatedVendingMachine, "InstallFromVendingOrders"));

                    //if it's not relative, move it now

                    configData.dedicatedVendingMachine.Spawn();
                    var groundWatch = configData.dedicatedVendingMachine.gameObject.GetComponent<GroundWatch>();

                    if (groundWatch != null)
                    {
                        UnityEngine.Object.Destroy(groundWatch);
                    }

                    var destroyOnGroundMissing = configData.dedicatedVendingMachine.gameObject.GetComponent<DestroyOnGroundMissing>();

                    if (destroyOnGroundMissing != null)
                    {
                        UnityEngine.Object.Destroy(destroyOnGroundMissing);
                    }
                    //entity.enableSaving = false;

                    configData.dedicatedVendingMachine.RemoveFromTriggers();

                    if (!configData.vendingMachineLocationIsRelative)
                    {
                        configData.dedicatedVendingMachine.transform.SetPositionAndRotation(spawnPos, spawnQuat);

                        configData.dedicatedVendingMachine.SetParent(configData.dedicatedVendingMachineDummy, true, true);
                    }
                    else
                    {
                        configData.dedicatedVendingMachine.SetParent(configData.dedicatedVendingMachineDummy, false, true);
                        configData.dedicatedVendingMachine.transform.localPosition = configData.vendingMachinePos + new Vector3(0F, 100F, 0);
                        configData.dedicatedVendingMachine.transform.localEulerAngles = configData.vendingMachineRot;
                    }


                    //de-parent it from the dummy (keeping position/rotation), destroy the dummy
                    configData.dedicatedVendingMachine.SetParent(null, true, true);
                    configData.dedicatedVendingMachine.transform.hasChanged = true;

                    configData.dedicatedVendingMachineDummy.Kill();
                    RestockVendingMachine();
                    //configData.dedicatedVendingMachine.RefreshAndSendNetworkUpdate();
                    //configData.dedicatedVendingMachine.UpdateNetworkGroup();
                    //RefreshVendingMachine();

                }
            }

        }

        private void Unload()
        {
            //if (configData.enableVendingMachine)
            KillVendingMachine();

            foreach (var drop in MonoBehaviour.FindObjectsOfType<CustomDrop>())
            {

                if (!drop.landed)
                {
                    DetachParachute(drop);
                }
                UnityEngine.Object.DestroyImmediate(drop);
            }

            foreach (var drop in MonoBehaviour.FindObjectsOfType<CustomNormalDrop>())
            {
                if (!drop.landed)
                {
                    DetachParachute(drop);
                }
                UnityEngine.Object.DestroyImmediate(drop);
            }

            foreach (var launcher in MonoBehaviour.FindObjectsOfType<FireworkLauncher>())
            {
                UnityEngine.Object.DestroyImmediate(launcher.gameObject);
            }

            //kill drop monos

            /*
            var toDestroy = new List<CustomDrop>();


            foreach (var drop in JustMainEntityIDToCustomDrop)
            {
                //if it hasn't landed yet, detach the parachute!
                
                if (!drop.Value.landed)
                {
                    DetachParachute(drop.Value);
                }

                toDestroy.Add(drop.Value);
            }

            foreach (var bye in toDestroy)
            {
                UnityEngine.Object.Destroy(bye);
            }
                */
            //null-out Static

            //CustomSignals = null;
            CustomCargoPlanes = null;
            EntityIDToCustomDrop = null;

            JustMainEntityIDToCustomDrop = null;
            EntityIDToNormalDrop = null;

            //ImageLibrary?.Call("RemoveImage", GetNameFromURL(ParseURL("https://i.imgur.com/lcrMqDl.png")), (ulong)ResourceId);

            Instance = null;
        }
        void OnItemDropped(Item item, BaseEntity entity)
        {
            if (Instance == null) return;

            if (item.skin == 0) return;
            if (!ItemIDtoItemName.ContainsKey(item.info.itemid)) return;

            if (!SkinIDToItemName.ContainsKey(item.skin)) return;

            var kind = SkinIDToItemName[item.skin];

            var dropped = entity as DroppedItem;
            var pos = dropped.transform.position;
            var rot = dropped.transform.rotation;


            var rigid = dropped.GetComponent<Rigidbody>();
            var velo = rigid.velocity;

            var amount = dropped.item.amount;

            //now kill the dropped item...
            dropped.Kill();

            //create a new item (supply signal) with the given amount and drop it
            var newItem = ItemManager.CreateByItemID(1397052267, amount, 0);
            TurnSignalIntoCustom(newItem, kind);

            //drop it
            newItem.Drop(pos, velo, rot);

            return;
        }

        object OnItemSplit(Item item, int amount)
        {
            if (Instance == null) return null;
            //did you just split a custom drop?
            if (item.skin == 0) return null;

            //if the item.skin is not in the dictionary, nothing happens
            if (!SkinIDToItemName.ContainsKey(item.skin)) return null;

            var skin = item.skin;
            var name = item.name;
            var itemID = item.info.itemid;

            item.amount -= amount;
            item.MarkDirty();

            var newItem = ItemManager.CreateByItemID(itemID, amount, skin);
            newItem.name = name;

            return newItem;

        }
        /*
        object CanMoveItem(Item item, PlayerInventory playerLoot, uint targetContainer, int targetSlot, int amount)
        {
            if (item.skin == 0) return null;
            if (item.info.itemid != 1397052267) return null;

            if (SkinIDToItemName.ContainsKey(item.skin))
            {
                return false;
            }

            return null;
        }*/

        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (Instance == null) return;
            if (item.skin == 0) return;
            //translate to-from vending machine

            var maybeVendingMachine = container.entityOwner as VendingMachine;
            if (maybeVendingMachine != null)
            {
                if (item.info.itemid != 1397052267) return;
                //it's not a real supply signal, no need to go further

                //what's the item skin? does it have one?
                if (item.skin != 0)
                {
                    if (SkinIDToItemName.ContainsKey(item.skin))
                    {
                        Instance.NextFrame(() =>
                        {
                            item = ReplaceSignalItemWithFakeItemInContainer(container, item);
                        });
                    }
                }
            }
            else
            {
                //see if you have it in your skins...
                if (SkinIDToItemName.ContainsKey(item.skin))
                {
                    if (item.info.itemid == 1397052267)
                    {
                        var kind = SkinIDToItemName[item.skin];
                        Instance.NextFrame(() =>
                        {
                            if (SignalDefinitions.definitions.ContainsKey(kind))
                            {

                                item.name = $"{item.info.displayName.translated} ({SignalDefinitions.definitions[kind].itemSuffix})";
                            }
                            else
                            {
                                Instance.PrintError($"ERROR: \"{kind}\" is not a registered signal definition!");

                                item.name = $"{item.info.displayName.translated} ({kind.ToUpper()})";
                            }
                            item.MarkDirty();
                            container.MarkDirty();
                        });
                    }
                    else
                    {
                        Instance.NextFrame(() =>
                        {
                            item = ReplaceFakeItemWithSignalItemInContainer(container, item);
                        });
                    }
                }
            }
        }

        void OnExplosiveThrown(BasePlayer player, SupplySignal entity, ThrownWeapon item)
        {
            if (Instance == null) return;
            var actualItem = item.GetItem();

            if (actualItem == null) return;

            if (SkinIDToItemName.ContainsKey(actualItem.skin) || actualItem.skin == 0)
            {


                //give it a component
                entity.gameObject.AddComponent<CustomSignal>();

                var compo = entity.gameObject.GetComponent<CustomSignal>();

                //add to the registry
                //CustomSignals.Add(entity.net.ID, compo);

                compo.skin = actualItem.skin;

                if (actualItem.skin == 0)
                {
                    compo.kind = "normal";
                }
                else
                {
                    compo.kind = SkinIDToItemName[actualItem.skin];
                }

                compo.ownerID = player.userID;
            }
        }

        void OnExplosiveDropped(BasePlayer player, SupplySignal entity, ThrownWeapon item) => OnExplosiveThrown(player, entity, item);

        //If you want a hook for planes carrying a custom drop,
        //use OnCustomAirdrop(CargoPlane plane, Vector3 dropPosition, string kind, ulong ownerID)
        [PluginReference]
        private Plugin PlaneCrash;


        void OnAirdrop(CargoPlane plane, Vector3 dropPosition)
        {
            if (Instance == null) return;

            Instance.timer.Once(0.5F, () =>
            {
                if (plane == null) return;

                if (PlaneCrash?.Call<bool>("IsCrashPlane", plane) ?? false)
                {
                    return;
                }

                if (Interface.Call("isAFPlane", plane)?.Equals(true) ?? false)
                {
                    return;
                }

                if (plane.gameObject.GetComponent<CustomPlane>() == null)
                {
                    //what's the chance of turning this random airdrop into a custom drop?
                    if (!configData.enableRandomDrop || configData.customDropChance == 0F)
                    {
                        CustomizePlane(plane, "normal", 0);
                        return;
                    }

                    var rnd = UnityEngine.Random.Range(0F, 1F);
                    if (rnd <= configData.customDropChance)
                    {
                        var rndKind = WeightedRandomKind(false);
                        CustomizePlane(plane, rndKind, 0);
                    }
                    else
                    {
                        CustomizePlane(plane, "normal", 0);
                    }
                }
            });
        }

        object OnRotateVendingMachine(VendingMachine machine, BasePlayer player)
        {
            if (Instance == null) return null;
            //only for server administrators
            if (configData.enableVendingMachine)
            {
                if (machine == configData.dedicatedVendingMachine)
                {
                    return false;
                }
                else return null;
            }
            else return null;
        }
        object CanAdministerVending(BasePlayer player, VendingMachine machine)
        {
            if (Instance == null) return null;
            //only for server administrators
            if (configData.enableVendingMachine)
            {
                if (configData.dedicatedVendingMachine != null)
                {
                    if (machine == configData.dedicatedVendingMachine)
                    {
                        return false;
                    }
                    else return true;
                }
                else return true;
            }
            else return true;
        }

        //only for administrators
        /*
        void OnToggleVendingBroadcast(VendingMachine machine, BasePlayer player)
        {
            Puts("OnToggleVendingBroadcast works!");
        } */

        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (Instance == null) return null;

            if (entity == null) return null;

            if (info == null) return null;

            if (entity.net == null) return null;

            if (entity as VendingMachine != null && configData.enableVendingMachine)
            {
                if (configData.dedicatedVendingMachine != null)
                {
                    if (entity == configData.dedicatedVendingMachine)
                    {
                        return true;
                    }
                }
            }

            //only applies to dropped things

            var maybeSupplyDrop = entity as SupplyDrop;
            if (maybeSupplyDrop != null)
            {
                /*
                if (!EntityIDToNormalDrop.ContainsKey(entity.net.ID))
                {
                    return null;
                }

                var maybeNormalDrop = EntityIDToNormalDrop[entity.net.ID];

                if (maybeNormalDrop != null)
                {
                    if (!maybeNormalDrop.landed)
                    {
                        if (configData.damageDetachesParachute)
                        {
                            maybeNormalDrop.supplyDrop.RemoveParachute();
                            maybeNormalDrop.supplyDrop.MakeLootable();
                            Interface.CallHook("OnSupplyDropLanded", (object)maybeNormalDrop.supplyDrop);
                            Instance.DetachParachute(maybeNormalDrop);
                        }
                    }
                } */

                return null;
            }
            else
            {
                if (!EntityIDToCustomDrop.ContainsKey(entity.net.ID))
                {
                    return null;
                }


                //ignore decay damage
                var maybeVehicle = entity as BaseVehicle;
                var maybeModule = entity as BaseVehicleModule;

                if (maybeVehicle == null)
                {
                    var maybeCrate = entity as HackableLockedCrate;

                    if (maybeCrate == null && maybeModule == null)
                    {
                        return null;
                    }
                }

                var maybeCustomDrop = EntityIDToCustomDrop[entity.net.ID];

                if (maybeCustomDrop != null)
                {
                    if (!maybeCustomDrop.landed)
                    {
                        if (info.damageTypes.GetMajorityDamageType() == Rust.DamageType.Decay)
                        {
                            if (configData.decaysWithParachute)
                            {
                                return null;
                                //don't detach parachute from decay damage
                            }
                            else
                            {
                                return true;
                                //don't detach and prevent decay damage
                            }
                        }

                        if (configData.damageDetachesParachute)
                        {
                            Instance.DetachParachute(maybeCustomDrop);
                        }

                        if (configData.indestructibleWithParachute)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (maybeCustomDrop.ownerID != 0 & Instance.configData.enablePrivateSignals)
                        {
                            if (Instance.configData.privateDropsIndestructible)
                            {
                                return true;
                            }
                        }
                    }
                }

                return null;
            }
        }
        void OnEntitySpawned(BaseVehicle vehicle)
        {
            if (Instance == null) return;
            if (configData == null) return;

            Instance.NextTick(() =>
            {
                var maybeRhib = vehicle as RHIB;
                if (maybeRhib != null)
                {
                    EquipVehicle(maybeRhib, "rhib", false);
                }
                else
                {
                    var maybeSub = vehicle as BaseSubmarine;
                    if (maybeSub != null)
                    {
                        if (maybeSub.PrefabName.Contains("duo"))
                        {

                            EquipVehicle(maybeSub, "duosub", false);
                        }
                        else
                        {
                            EquipVehicle(maybeSub, "solosub", false);
                        }
                    }
                    else
                    {
                        var maybeBoat = vehicle as MotorRowboat;
                        if (maybeBoat != null)
                        {
                            EquipVehicle(maybeBoat, "rowboat", false);
                        }
                        else
                        {
                            var maybeScrap = vehicle as ScrapTransportHelicopter;
                            if (maybeScrap != null)
                            {
                                EquipVehicle(maybeScrap, "scrapheli", false);
                            }
                            else
                            {
                                var maybeMini = vehicle as MiniCopter;
                                if (maybeMini != null)
                                {
                                    EquipVehicle(maybeMini, "minicopter", false);
                                }
                                else
                                {
                                    var maybeModular = vehicle as ModularCar;
                                    if (maybeModular != null)
                                    {
                                        EquipVehicle(maybeModular, "car", false);
                                    }
                                }
                            }
                        }
                    }
                }
            });
        }

        void OnEntitySpawned(LootContainer entity)
        {
            if (Instance == null) return;
            if (configData == null) return;

            if (!configData.enableRandomLoot) return;
            if (configData.customLootChance == 0F) return;

            //ignore player corpses (but not scientist corpses)
            if (entity.ShortPrefabName == "player_corpse") return;

            //and backpacks
            if (entity.ShortPrefabName == "item_drop_backpack") return;// && !IsHumanNPC(entity.OwnerID)) return;

            //if the inventory's empty, return
            if (!entity.inventory.itemList.Any()) return;

            //if the inventory doesn't contain any supply signals, return
            if (entity.inventory.GetAmount(1397052267, false) == 0) return;

            float rnd;

            for (var i = 0; i < entity.inventory.itemList.Count; i++)
            {
                if (entity.inventory.itemList[i].info.itemid != 1397052267) continue;

                rnd = UnityEngine.Random.Range(0F, 1F);

                if (rnd <= configData.customLootChance)
                {
                    TurnSignalIntoCustom(entity.inventory.itemList[i], null, true);
                }

            }
        }

        void OnEntitySpawned(SupplyDrop entity)
        {
            if (Instance == null) return;
            if (configData == null) return;

            if (!CustomCargoPlanes.Where(p => p.Key != null).Any()) return;

            var findPlane = CustomCargoPlanes.Where(p => p.Key != null).OrderBy(p => Vector3.Distance(entity.transform.position, p.Key.transform.position)).First().Key;

            if (findPlane != null)
            {
                //found it
                var planeCompo = findPlane.gameObject.GetComponent<CustomPlane>();

                if (planeCompo != null)
                {
                    if (planeCompo.kind != "normal")
                    //Vector3 extra = planeCompo.kind == "horse" ? (Vector3.down * 5F) : Vector3.zero;
                    {
                        SpawnCustomDrop(planeCompo.kind, entity.transform.position, new Vector3(0F, UnityEngine.Random.Range(0F, 360F), 0F), planeCompo.ownerID);
                        entity.Kill();
                    }
                    else
                    {
                        var compo = entity.gameObject.AddComponent<CustomNormalDrop>();
                        compo.ownerID = planeCompo.ownerID;
                    }

                    //remove
                    CustomCargoPlanes.Remove(findPlane);

                    GameManager.DestroyImmediate(planeCompo);

                }
            }
        }

        object CanLootEntity(BasePlayer player, StorageContainer container) => CanAccessDrop(player, container);

        object CanHackCrate(BasePlayer player, HackableLockedCrate crate)
        {
            if (Instance == null) return null;
            if (player == null) return null;
            if (crate == null) return null;

            return CanAccessDrop(player, crate as BaseNetworkable);
        }
        object CanMountEntity(BasePlayer player, BaseMountable entity)
        {
            if (Instance == null) return null;
            if (player == null) return null;
            if (entity == null) return null;

            return CanAccessDrop(player, entity);
        }

        public object CanAccessDrop(BasePlayer player, BaseNetworkable entity)
        {
            if (Instance == null) return null;
            if (player == null) return null;
            if (entity == null) return null;

            if (!EntityIDToCustomDrop.ContainsKey(entity.net.ID) && !EntityIDToNormalDrop.ContainsKey(entity.net.ID))
            {
                return null;
            }


            CustomDrop dropComponent = null;

            if (EntityIDToCustomDrop.ContainsKey(entity.net.ID)) dropComponent = EntityIDToCustomDrop[entity.net.ID];

            if (dropComponent == null)
            {
                var normalComponent = EntityIDToNormalDrop[entity.net.ID];
                if (normalComponent == null)
                {
                    return null;
                }
                else
                {
                    object goAhead = null;

                    bool alsoDetach = false;

                    if (!normalComponent.landed)
                    {
                        if (!configData.usableWithParachute)
                        {
                            Instance.TellMessage(player, MSG(MSG_PLEASE_WAIT_TILL_LANDS, player.UserIDString));
                            goAhead = false;
                        }
                        else
                        {
                            alsoDetach = true;
                        }
                    }

                    if (goAhead == null)
                    {
                        if (configData.enablePrivateSignals)
                        {
                            if (normalComponent.ownerID != 0)
                            {
                                if (normalComponent.ownerID != player.userID)
                                {
                                    if (configData.privateSignalsIncludeTeammates)
                                    {
                                        if (player.Team != null)
                                        {
                                            if (!player.Team.members.Contains(normalComponent.ownerID))
                                            {
                                                goAhead = false;
                                            }
                                        }
                                        else
                                        {
                                            goAhead = false;
                                        }
                                    }
                                    else
                                    {
                                        goAhead = false;
                                    }
                                }
                            }
                        }
                    }

                    if (goAhead != null)
                    {
                        var unlockMessage = MSG(MSG_UNLOCK_AFTER, player.UserIDString, configData.unlockPrivateDropAfter.ToString("0.0"));
                        var ownershipMessage = "";

                        var teamStuff = false;

                        if (player.Team != null)
                        {
                            teamStuff = player.Team.members.Contains(normalComponent.ownerID);
                        }


                        if (normalComponent.ownerID != 0 && !(player.userID == normalComponent.ownerID || teamStuff))
                        {
                            ownershipMessage = MSG(MSG_BELONGS_TO_SOMEONE_ELSE, player.UserIDString);
                        }

                        if (normalComponent.landed)
                        {
                            unlockMessage = MSG(MSG_WILL_UNLOCK_IN, player.UserIDString, (normalComponent.shouldUnlockAtTime - normalComponent.currentTime).ToString("0.0"));
                        }

                        Instance.TellMessage(player, $"{ownershipMessage}{unlockMessage}");
                    }
                    else
                    {
                        if (alsoDetach)
                        {
                            DetachParachute(normalComponent);
                        }
                    }

                    return goAhead;

                }
            }
            else
            {
                object goAhead = null;

                bool alsoDetach = false;

                if (!dropComponent.landed)
                {
                    if (!configData.usableWithParachute)
                    {
                        Instance.TellMessage(player, MSG(MSG_PLEASE_WAIT_TILL_X_LANDS, player.UserIDString, SignalDefinitions.definitions[dropComponent.kind].itemSuffix));
                        goAhead = false;
                    }
                    else
                    {
                        alsoDetach = true;
                    }
                }

                if (goAhead == null)
                {
                    if (configData.enablePrivateSignals)
                    {
                        if (dropComponent.ownerID != 0)
                        {
                            if (dropComponent.ownerID != player.userID)
                            {
                                if (configData.privateSignalsIncludeTeammates)
                                {
                                    if (player.Team != null)
                                    {
                                        if (!player.Team.members.Contains(dropComponent.ownerID))
                                        {
                                            goAhead = false;
                                        }
                                    }
                                    else
                                    {
                                        goAhead = false;
                                    }
                                }
                                else
                                {
                                    goAhead = false;
                                }
                            }
                        }
                    }
                }

                if (goAhead != null)
                {
                    var unlockMessage = MSG(MSG_UNLOCK_AFTER, player.UserIDString, configData.unlockPrivateDropAfter.ToString("0.0"));
                    var ownershipMessage = "";

                    var teamStuff = false;

                    if (player.Team != null)
                    {
                        teamStuff = player.Team.members.Contains(dropComponent.ownerID);
                    }


                    if (dropComponent.ownerID != 0 && !(player.userID == dropComponent.ownerID || teamStuff))
                    {
                        ownershipMessage = MSG(MSG_BELONGS_TO_SOMEONE_ELSE, player.UserIDString);
                    }

                    if (dropComponent.landed)
                    {
                        unlockMessage = MSG(MSG_WILL_UNLOCK_IN, player.UserIDString, (dropComponent.shouldUnlockAtTime - dropComponent.currentTime).ToString("0.0"));
                    }

                    Instance.TellMessage(player, $"{ownershipMessage}{unlockMessage}");
                }
                else
                {
                    if (alsoDetach)
                    {
                        DetachParachute(dropComponent);
                    }
                }

                return goAhead;
            }
        }
        #endregion
        #region CHAT
        [ChatCommand("vabuy")]
        private void cmdChatVaBuy(BasePlayer player, string command, string[] args)
        {
            if (configData.enableCommandShop == false)
            {
                Instance.TellMessage(player, MSG(MSG_VABUY_DISABLED, player.UserIDString));
                return;
            }
            var buildChatMessage = "";

            double balance = 0;

            int currencyItemID = configData.normalCurrencyItemID;
            string currencyName;
            string buildAmount;

            switch (configData.defaultPayment)
            {
                case "ServerRewards":
                    {
                        var checkRewardPoints = ServerRewards?.Call("CheckPoints", player.userID);
                        if (checkRewardPoints != null)
                        {
                            balance = Convert.ToDouble(checkRewardPoints);
                        }

                        currencyName = MSG(MSG_VABUY_SERVER_REWARDS_CURRENCY, player.UserIDString);
                        buildAmount = $"{balance} {currencyName}";
                    }
                    break;
                case "Economics":
                    {
                        var checkRewardPoints = Economics?.Call("Balance", player.userID);
                        if (checkRewardPoints != null)
                        {
                            balance = Convert.ToDouble(checkRewardPoints);
                        }
                        currencyName = MSG(MSG_VABUY_ECONOMICS_CURRENCY, player.UserIDString);
                        buildAmount = $"{currencyName}{balance}";
                    }
                    break;
                default: //fall back to currency
                    {
                        balance += player.inventory.containerBelt.GetAmount(currencyItemID, true);
                        balance += player.inventory.containerMain.GetAmount(currencyItemID, true);

                        currencyName = ItemManager.FindItemDefinition(currencyItemID).displayName.translated;
                        buildAmount = $"{balance} {currencyName}";
                    }
                    break;
            }

            //let's do a pre-check here. have a little cache.

            Dictionary<string, double> buyableCache = new Dictionary<string, double>();

            buildChatMessage += MSG(MSG_VABUY_YOU_CAN_BUY, player.UserIDString, configData.defaultPayment, buildAmount) + "\n";

            double cost = 0;

            bool atLeastOneFound = false;

            //don't forget the normal drop first.
            if (configData.commandShopHasNormal)
            {
                atLeastOneFound = true;
                var hasPermission = false;
                var noPermission = configData.permissionNormal == null;
                if (!noPermission)
                {
                    hasPermission = HasPermission(player, configData.permissionNormal);
                }

                if (noPermission || hasPermission)
                {
                    cost = configData.priceNormalCurrency;
                    currencyItemID = configData.normalCurrencyItemID;

                    string buildCostAmount;

                    if (configData.defaultPayment == "ServerRewards")
                    {
                        cost = configData.priceNormalServerRewards;
                        buildCostAmount = $"{cost} {currencyName}";
                    }
                    else if (configData.defaultPayment == "Economics")
                    {
                        cost = configData.priceNormalEconomics;
                        buildCostAmount = $"{currencyName}{cost}";
                    }
                    else
                    {
                        currencyName = ItemManager.FindItemDefinition(currencyItemID).displayName.translated;
                        cost = configData.priceNormalCurrency;
                        buildCostAmount = $"{cost} {currencyName}";
                    }

                    //skip drops that cost nothing
                    if (cost != 0)
                    {

                        if (cost <= balance)
                        {
                            buyableCache.Add("normal", cost);
                        }
                        buildChatMessage += MSG(MSG_VABUY_COST, player.UserIDString, "normal", buildCostAmount) + "\n";
                    }
                }
            }


            foreach (var entry in configData.dropConfigs)
            {
                if (!entry.Value.enableInCommandShop) continue;
                if (entry.Value.permission != null)
                {
                    if (!HasPermission(player, entry.Value.permission))
                    {
                        continue;
                    }
                }

                atLeastOneFound = true;

                cost = entry.Value.priceCurrency;
                currencyItemID = entry.Value.currencyItemID;

                string buildCostAmount;

                if (configData.defaultPayment == "ServerRewards")
                {
                    cost = entry.Value.priceServerRewards;
                    buildCostAmount = $"{cost} {currencyName}";
                }
                else if (configData.defaultPayment == "Economics")
                {
                    cost = entry.Value.priceEconomics;
                    buildCostAmount = $"{currencyName}{cost}";
                }
                else
                {
                    currencyName = ItemManager.FindItemDefinition(currencyItemID).displayName.translated;
                    cost = entry.Value.priceCurrency;
                    buildCostAmount = $"{cost} {currencyName}";
                }

                //skip drops that cost nothing
                if (cost == 0) continue;

                if (cost <= balance)
                {
                    buyableCache.Add(entry.Key, cost);
                }

                //check if permission is needed to see this entry, if so, check if the player has that permission
                buildChatMessage += MSG(MSG_VABUY_COST, player.UserIDString, entry.Key, buildCostAmount) + "\n";
            }
            if (!atLeastOneFound)
            {
                buildChatMessage = MSG(MSG_VABUY_NO_SIGNALS, player.UserIDString);//
            }
            else
            //check if player has permission to buy
            if (args.Length == 0)
            {
                //just print what was premonitioned before... later

            }
            else
            {
                //check if arg[0] is inside the config!
                if (configData.dropConfigs.ContainsKey(args[0]) || args[0] == "normal")
                {
                    //check the cache. buyable or not?
                    if (buyableCache.ContainsKey(args[0]))
                    {
                        if (GiveSignal(player, args[0], 1))
                        {
                            //success. buy that shit.
                            switch (configData.defaultPayment)
                            {
                                case "ServerRewards":
                                    {
                                        //take the reward points and tell the player
                                        if (args[0] == "normal")
                                        {
                                            ServerRewards?.Call("TakePoints", player.userID, configData.priceNormalServerRewards);

                                        }
                                        else
                                        {
                                            ServerRewards?.Call("TakePoints", player.userID, configData.dropConfigs[args[0]].priceServerRewards);
                                        }

                                    }
                                    break;
                                case "Economics":
                                    {
                                        if (args[0] == "normal")
                                        {
                                            Economics?.Call("Withdraw", player.userID, (double)configData.priceNormalEconomics);
                                        }
                                        else
                                        {
                                            Economics?.Call("Withdraw", player.userID, (double)configData.dropConfigs[args[0]].priceEconomics);
                                        }
                                        //take the money and tell the player
                                    }
                                    break;
                                default:
                                    {
                                        if (args[0] == "normal")
                                        {
                                            player.inventory.Take(null, configData.normalCurrencyItemID, configData.priceNormalCurrency);
                                        }
                                        else
                                        {
                                            player.inventory.Take(null, configData.dropConfigs[args[0]].currencyItemID, configData.dropConfigs[args[0]].priceCurrency);
                                        }
                                    }
                                    break;
                            }
                            buildChatMessage = MSG(MSG_VABUY_YOU_JUST_BOUGHT, player.UserIDString, args[0]);
                        }
                    }
                    else
                    {
                        buildChatMessage = MSG(MSG_VABUY_YOU_CANT_BUY, player.UserIDString, args[0]);
                    }
                }
                else
                {
                    buildChatMessage = MSG(MSG_VABUY_WRONG_DROP, player.UserIDString);
                }

            }

            Instance.TellMessage(player, buildChatMessage);
        }

        [ChatCommand("signal")]
        private void cmdChatSignal(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player, PERMISSION_ADMIN))
            {
                Instance.TellMessage(player, MSG(MSG_NO_PERMISSION, player.UserIDString));
                return;
            }

            if (args.Length > 0)
            {
                var kind = args[0];

                if (kind == "random")
                {
                    kind = WeightedRandomKind(true);
                }

                if (SignalDefinitions.definitions.ContainsKey(kind))
                {
                    if (args.Length > 1)
                    {
                        var playerToGive = BasePlayer.activePlayerList.Where(p => p.displayName.ToLower().Contains(args[1].ToLower())).FirstOrDefault();
                        GiveSignal(playerToGive, kind, 1);
                        if (player != playerToGive)
                        {
                            Instance.TellMessage(player, MSG(MSG_YOU_GAVE_SIGNAL_TO_SOMEONE, player.UserIDString, playerToGive.displayName, SignalDefinitions.definitions[kind].itemSuffix));
                            Instance.TellMessage(playerToGive, MSG(MSG_YOU_RECEIVED_SIGNAL, playerToGive.UserIDString, SignalDefinitions.definitions[kind].itemSuffix));
                        }
                        else
                        {
                            Instance.TellMessage(player, MSG(MSG_YOU_GAVE_YOURSELF, player.UserIDString, SignalDefinitions.definitions[kind].itemSuffix));
                        }
                    }
                    else
                    {
                        Instance.TellMessage(player, MSG(MSG_YOU_GAVE_YOURSELF, player.UserIDString, SignalDefinitions.definitions[kind].itemSuffix));
                        GiveSignal(player, kind, 1);
                    }
                }
                else
                {
                    Instance.TellMessage(player, MSG(MSG_THIS_KIND_DOESNT_EXIST, player.UserIDString));
                }
            }
            else
            {
                Instance.TellMessage(player, MSG(MSG_PLEASE_SPECIFY, player.UserIDString));
            }

        }

        [ChatCommand("drop")]
        private void cmdChatDrop(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player, PERMISSION_ADMIN))
            {
                Instance.TellMessage(player, MSG(MSG_NO_PERMISSION, player.UserIDString));
                return;
            }

            if (args.Length > 0)
            {
                var kind = args[0];

                if (kind == "random")
                {
                    kind = WeightedRandomKind(false);
                }

                if (ShortPrefabName.ContainsKey(kind))
                {
                    var dir = MSG(MSG_DIRECTION_ABOVE, player.UserIDString);// "above";
                    var amount = 750F - player.transform.position.y;

                    if (player.transform.position.y > 750F)
                    {
                        dir = MSG(MSG_DIRECTION_BELOW, player.UserIDString);//"below";
                        amount *= -1;
                    }
                    Instance.TellMessage(player, MSG(MSG_YOU_HAVE_DROPPED, player.UserIDString, SignalDefinitions.definitions[kind].itemSuffix, amount, dir));
                    SpawnCustomDrop(kind, new Vector3(player.transform.position.x, 100F, player.transform.position.z), new Vector3(0, UnityEngine.Random.Range(0, 360F), 0));
                }
                else
                {
                    Instance.TellMessage(player, MSG(MSG_THIS_KIND_DOESNT_EXIST, player.UserIDString));
                }
            }
            else
            {
                Instance.TellMessage(player, MSG(MSG_PLEASE_SPECIFY, player.UserIDString));
            }
        }
        #endregion
        #region COLOR
        public class ColorCode
        {
            public string hexValue;
            public Color rustValue;
            public string rustString;
            public ColorCode(string hex)
            {
                hex = hex.ToUpper();

                hexValue = "#" + hex;

                //extract the R, G, B
                var r = (float)short.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber) / 255;
                var g = (float)short.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber) / 255;
                var b = (float)short.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber) / 255;

                rustValue = new Color(r, g, b);
                rustString = $"{r} {g} {b}";
            }
        }

        public static class ColorPalette
        {
            public static ColorCode RedLight = new ColorCode("b95f57");
            public static ColorCode YellowLight = new ColorCode("948825");
            public static ColorCode LimeLight = new ColorCode("9ab957");
            public static ColorCode GreenLight = new ColorCode("3f9425");
            public static ColorCode AquaLight = new ColorCode("25948d");
            public static ColorCode BlueLight = new ColorCode("256194");
            public static ColorCode PurpleLight = new ColorCode("732594");

            public static ColorCode OrangeLight = new ColorCode("fa5800");
            public static ColorCode PissYellowLight = new ColorCode("e5fa00");

            public static ColorCode OrangeDark = new ColorCode("802d00");
            public static ColorCode PissYellowDark = new ColorCode("758000");//

            public static ColorCode RedDark = new ColorCode("5c2f2b");
            public static ColorCode YellowDark = new ColorCode("4a4413");
            public static ColorCode LimeDark = new ColorCode("4c5c2b");
            public static ColorCode GreenDark = new ColorCode("1f4a12");
            public static ColorCode AquaDark = new ColorCode("124a46");
            public static ColorCode BlueDark = new ColorCode("12304a");
            public static ColorCode PurpleDark = new ColorCode("39124a");



            public static ColorCode Grey = new ColorCode("a4a6a7");
            public static ColorCode White = new ColorCode("f6eae1");
            public static ColorCode Black = new ColorCode("1e2020");
        }
        #endregion
        #region GUI
        public static GuiManager GUI;
        public class GuiManager
        {
            //store a dictionary of those at creation, where key is displayName

            public class GuiPage
            {
                public GuiManager manager;

                public CuiElementContainer pageContainer = new CuiElementContainer();
                public string title = "DEFAULT PAGE TITLE";
                public GuiPage(GuiManager manager, string title)
                {
                    this.manager = manager;
                    this.title = title;
                }
                public virtual void GuiPageShow(BasePlayer player)
                {
                    manager.SetTitle(title);
                    manager.TitleHide(player);
                    manager.TitleShow(player);
                    CuiHelper.AddUi(player, pageContainer);
                }
                public virtual void DefineOptions()
                {

                }

                public virtual void GuiPageHide(BasePlayer player)
                {
                    //destroy all the elements associated with it
                }
            }

            public class GuiPageCarExtra : GuiPage
            {
                public string page;

                CuiElement elementPistonsBText = new CuiElement
                {
                    Name = "vagui.cfg.pistonsb.text",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            FontSize = 14,
                            Text = $"This will change",
                            Color = $"{ColorPalette.White.rustString}"
                        },

                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{leftT.ToString()} {(bottomB-(heightLine+paddingV2)*9).ToString()}",
                            AnchorMax = $"{col3left.ToString()} {(topB-(heightLine+paddingV2)*9).ToString()}"
                        }

                    },
                    Parent = "Overlay"
                };

                CuiPanel elementPistons0BPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.RedDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section9left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 9).ToString()}", AnchorMax = $"{rightP.ToString()} {(-0 + topB - (heightLine + paddingV2) * 9).ToString()}" },
                };

                CuiElement elementPistons0BInput = new CuiElement
                {
                    Name = "vagui.cfg.pistons0b.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA pistons0b "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.pistons0b.panel"
                };

                CuiPanel elementPistons3BPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.BlueDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section8left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 9).ToString()}", AnchorMax = $"{section9left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 9).ToString()}" },
                };

                CuiElement elementPistons3BInput = new CuiElement
                {
                    Name = "vagui.cfg.pistons3b.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA pistons3b "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.pistons3b.panel"
                };

                CuiPanel elementPistons2BPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.GreenDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section7left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 9).ToString()}", AnchorMax = $"{section8left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 9).ToString()}" },
                };

                CuiElement elementPistons2BInput = new CuiElement
                {
                    Name = "vagui.cfg.pistons2b.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA pistons2b "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.pistons2b.panel"
                };

                CuiPanel elementPistons1BPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.YellowDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section6left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 9).ToString()}", AnchorMax = $"{section7left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 9).ToString()}" },
                };

                CuiElement elementPistons1BInput = new CuiElement
                {
                    Name = "vagui.cfg.pistons1b.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA pistons1b "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.pistons1b.panel"
                };


                CuiElement elementPistonsAText = new CuiElement
                {
                    Name = "vagui.cfg.pistonsa.text",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            FontSize = 14,
                            Text = $"This will change",
                            Color = $"{ColorPalette.White.rustString}"
                        },

                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{leftT.ToString()} {(bottomB-(heightLine+paddingV2)*8).ToString()}",
                            AnchorMax = $"{col3left.ToString()} {(topB-(heightLine+paddingV2)*8).ToString()}"
                        }

                    },
                    Parent = "Overlay"
                };

                CuiPanel elementPistons0APanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.RedDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section9left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 8).ToString()}", AnchorMax = $"{rightP.ToString()} {(-0 + topB - (heightLine + paddingV2) * 8).ToString()}" },
                };

                CuiElement elementPistons0AInput = new CuiElement
                {
                    Name = "vagui.cfg.pistons0a.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA pistons0a "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.pistons0a.panel"
                };

                CuiPanel elementPistons3APanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.BlueDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section8left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 8).ToString()}", AnchorMax = $"{section9left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 8).ToString()}" },
                };

                CuiElement elementPistons3AInput = new CuiElement
                {
                    Name = "vagui.cfg.pistons3a.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA pistons3a "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.pistons3a.panel"
                };

                CuiPanel elementPistons2APanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.GreenDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section7left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 8).ToString()}", AnchorMax = $"{section8left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 8).ToString()}" },
                };

                CuiElement elementPistons2AInput = new CuiElement
                {
                    Name = "vagui.cfg.pistons2a.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA pistons2a "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.pistons2a.panel"
                };

                CuiPanel elementPistons1APanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.YellowDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section6left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 8).ToString()}", AnchorMax = $"{section7left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 8).ToString()}" },
                };

                CuiElement elementPistons1AInput = new CuiElement
                {
                    Name = "vagui.cfg.pistons1a.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA pistons1a "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.pistons1a.panel"
                };


                CuiElement elementSparkplugsBText = new CuiElement
                {
                    Name = "vagui.cfg.sparkplugsb.text",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            FontSize = 14,
                            Text = $"This will change",
                            Color = $"{ColorPalette.White.rustString}"
                        },

                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{leftT.ToString()} {(bottomB-(heightLine+paddingV2)*7).ToString()}",
                            AnchorMax = $"{col3left.ToString()} {(topB-(heightLine+paddingV2)*7).ToString()}"
                        }

                    },
                    Parent = "Overlay"
                };

                CuiPanel elementSparkplugs0BPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.RedDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section9left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 7).ToString()}", AnchorMax = $"{rightP.ToString()} {(-0 + topB - (heightLine + paddingV2) * 7).ToString()}" },
                };

                CuiElement elementSparkplugs0BInput = new CuiElement
                {
                    Name = "vagui.cfg.sparkplugs0b.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA sparkplugs0b "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.sparkplugs0b.panel"
                };

                CuiPanel elementSparkplugs3BPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.BlueDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section8left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 7).ToString()}", AnchorMax = $"{section9left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 7).ToString()}" },
                };

                CuiElement elementSparkplugs3BInput = new CuiElement
                {
                    Name = "vagui.cfg.sparkplugs3b.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA sparkplugs3b "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.sparkplugs3b.panel"
                };

                CuiPanel elementSparkplugs2BPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.GreenDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section7left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 7).ToString()}", AnchorMax = $"{section8left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 7).ToString()}" },
                };

                CuiElement elementSparkplugs2BInput = new CuiElement
                {
                    Name = "vagui.cfg.sparkplugs2b.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA sparkplugs2b "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.sparkplugs2b.panel"
                };

                CuiPanel elementSparkplugs1BPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.YellowDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section6left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 7).ToString()}", AnchorMax = $"{section7left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 7).ToString()}" },
                };

                CuiElement elementSparkplugs1BInput = new CuiElement
                {
                    Name = "vagui.cfg.sparkplugs1b.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA sparkplugs1b "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.sparkplugs1b.panel"
                };


                CuiElement elementSparkplugsAText = new CuiElement
                {
                    Name = "vagui.cfg.sparkplugsa.text",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            FontSize = 14,
                            Text = $"This will change",
                            Color = $"{ColorPalette.White.rustString}"
                        },

                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{leftT.ToString()} {(bottomB-(heightLine+paddingV2)*6).ToString()}",
                            AnchorMax = $"{col3left.ToString()} {(topB-(heightLine+paddingV2)*6).ToString()}"
                        }

                    },
                    Parent = "Overlay"
                };

                CuiPanel elementSparkplugs0APanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.RedDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section9left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 6).ToString()}", AnchorMax = $"{rightP.ToString()} {(-0 + topB - (heightLine + paddingV2) * 6).ToString()}" },
                };

                CuiElement elementSparkplugs0AInput = new CuiElement
                {
                    Name = "vagui.cfg.sparkplugs0a.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA sparkplugs0a "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.sparkplugs0a.panel"
                };

                CuiPanel elementSparkplugs3APanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.BlueDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section8left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 6).ToString()}", AnchorMax = $"{section9left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 6).ToString()}" },
                };

                CuiElement elementSparkplugs3AInput = new CuiElement
                {
                    Name = "vagui.cfg.sparkplugs3a.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA sparkplugs3a "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.sparkplugs3a.panel"
                };

                CuiPanel elementSparkplugs2APanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.GreenDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section7left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 6).ToString()}", AnchorMax = $"{section8left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 6).ToString()}" },
                };

                CuiElement elementSparkplugs2AInput = new CuiElement
                {
                    Name = "vagui.cfg.sparkplugs2a.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA sparkplugs2a "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.sparkplugs2a.panel"
                };

                CuiPanel elementSparkplugs1APanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.YellowDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section6left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 6).ToString()}", AnchorMax = $"{section7left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 6).ToString()}" },
                };

                CuiElement elementSparkplugs1AInput = new CuiElement
                {
                    Name = "vagui.cfg.sparkplugs1a.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA sparkplugs1a "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.sparkplugs1a.panel"
                };

                CuiElement elementValvesBText = new CuiElement
                {
                    Name = "vagui.cfg.valvesb.text",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            FontSize = 14,
                            Text = $"This will change",
                            Color = $"{ColorPalette.White.rustString}"
                        },

                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{leftT.ToString()} {(bottomB-(heightLine+paddingV2)*5).ToString()}",
                            AnchorMax = $"{col3left.ToString()} {(topB-(heightLine+paddingV2)*5).ToString()}"
                        }

                    },
                    Parent = "Overlay"
                };

                CuiPanel elementValves0BPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.RedDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section9left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 5).ToString()}", AnchorMax = $"{rightP.ToString()} {(-0 + topB - (heightLine + paddingV2) * 5).ToString()}" },
                };

                CuiElement elementValves0BInput = new CuiElement
                {
                    Name = "vagui.cfg.valves0b.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA valves0b "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.valves0b.panel"
                };

                CuiPanel elementValves3BPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.BlueDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section8left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 5).ToString()}", AnchorMax = $"{section9left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 5).ToString()}" },
                };

                CuiElement elementValves3BInput = new CuiElement
                {
                    Name = "vagui.cfg.valves3b.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA valves3b "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.valves3b.panel"
                };

                CuiPanel elementValves2BPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.GreenDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section7left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 5).ToString()}", AnchorMax = $"{section8left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 5).ToString()}" },
                };

                CuiElement elementValves2BInput = new CuiElement
                {
                    Name = "vagui.cfg.valves2b.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA valves2b "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.valves2b.panel"
                };

                CuiPanel elementValves1BPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.YellowDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section6left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 5).ToString()}", AnchorMax = $"{section7left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 5).ToString()}" },
                };

                CuiElement elementValves1BInput = new CuiElement
                {
                    Name = "vagui.cfg.valves1b.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA valves1b "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.valves1b.panel"
                };


                CuiElement elementValvesAText = new CuiElement
                {
                    Name = "vagui.cfg.valvesa.text",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            FontSize = 14,
                            Text = $"This will change",
                            Color = $"{ColorPalette.White.rustString}"
                        },

                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{leftT.ToString()} {(bottomB-(heightLine+paddingV2)*4).ToString()}",
                            AnchorMax = $"{col3left.ToString()} {(topB-(heightLine+paddingV2)*4).ToString()}"
                        }

                    },
                    Parent = "Overlay"
                };

                CuiPanel elementValves0APanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.RedDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section9left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 4).ToString()}", AnchorMax = $"{rightP.ToString()} {(-0 + topB - (heightLine + paddingV2) * 4).ToString()}" },
                };

                CuiElement elementValves0AInput = new CuiElement
                {
                    Name = "vagui.cfg.valves0a.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA valves0a "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.valves0a.panel"
                };

                CuiPanel elementValves3APanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.BlueDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section8left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 4).ToString()}", AnchorMax = $"{section9left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 4).ToString()}" },
                };

                CuiElement elementValves3AInput = new CuiElement
                {
                    Name = "vagui.cfg.valves3a.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA valves3a "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.valves3a.panel"
                };

                CuiPanel elementValves2APanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.GreenDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section7left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 4).ToString()}", AnchorMax = $"{section8left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 4).ToString()}" },
                };

                CuiElement elementValves2AInput = new CuiElement
                {
                    Name = "vagui.cfg.valves2a.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA valves2a "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.valves2a.panel"
                };

                CuiPanel elementValves1APanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.YellowDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section6left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 4).ToString()}", AnchorMax = $"{section7left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 4).ToString()}" },
                };

                CuiElement elementValves1AInput = new CuiElement
                {
                    Name = "vagui.cfg.valves1a.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA valves1a "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.valves1a.panel"
                };


                CuiElement elementCarburetorText = new CuiElement
                {
                    Name = "vagui.cfg.carburetor.text",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            FontSize = 14,
                            Text = $"This will change",
                            Color = $"{ColorPalette.White.rustString}"
                        },

                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{leftT.ToString()} {(bottomB-(heightLine+paddingV2)*3).ToString()}",
                            AnchorMax = $"{col3left.ToString()} {(topB-(heightLine+paddingV2)*3).ToString()}"
                        }

                    },
                    Parent = "Overlay"
                };

                CuiPanel elementCarburetor0Panel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.RedDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section9left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 3).ToString()}", AnchorMax = $"{rightP.ToString()} {(-0 + topB - (heightLine + paddingV2) * 3).ToString()}" },
                };

                CuiElement elementCarburetor0Input = new CuiElement
                {
                    Name = "vagui.cfg.carburetor0.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA carburetor0 "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.carburetor0.panel"
                };

                CuiPanel elementCarburetor3Panel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.BlueDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section8left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 3).ToString()}", AnchorMax = $"{section9left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 3).ToString()}" },
                };

                CuiElement elementCarburetor3Input = new CuiElement
                {
                    Name = "vagui.cfg.carburetor3.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA carburetor3 "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.carburetor3.panel"
                };

                CuiPanel elementCarburetor2Panel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.GreenDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section7left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 3).ToString()}", AnchorMax = $"{section8left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 3).ToString()}" },
                };

                CuiElement elementCarburetor2Input = new CuiElement
                {
                    Name = "vagui.cfg.carburetor2.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA carburetor2 "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.carburetor2.panel"
                };

                CuiPanel elementCarburetor1Panel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.YellowDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section6left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 3).ToString()}", AnchorMax = $"{section7left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 3).ToString()}" },
                };

                CuiElement elementCarburetor1Input = new CuiElement
                {
                    Name = "vagui.cfg.carburetor1.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA carburetor1 "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.carburetor1.panel"
                };


                CuiElement elementCrankshaftText = new CuiElement
                {
                    Name = "vagui.cfg.crankshaft.text",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            FontSize = 14,
                            Text = $"This will change",
                            Color = $"{ColorPalette.White.rustString}"
                        },

                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{leftT.ToString()} {(bottomB-(heightLine+paddingV2)*2).ToString()}",
                            AnchorMax = $"{col3left.ToString()} {(topB-(heightLine+paddingV2)*2).ToString()}"
                        }

                    },
                    Parent = "Overlay"
                };

                CuiPanel elementCrankshaft0Panel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.RedDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section9left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 2).ToString()}", AnchorMax = $"{rightP.ToString()} {(-0 + topB - (heightLine + paddingV2) * 2).ToString()}" },
                };

                CuiElement elementCrankshaft0Input = new CuiElement
                {
                    Name = "vagui.cfg.crankshaft0.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA crankshaft0 "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.crankshaft0.panel"
                };

                CuiPanel elementCrankshaft3Panel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.BlueDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section8left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 2).ToString()}", AnchorMax = $"{section9left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 2).ToString()}" },
                };

                CuiElement elementCrankshaft3Input = new CuiElement
                {
                    Name = "vagui.cfg.crankshaft3.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA crankshaft3 "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.crankshaft3.panel"
                };

                CuiPanel elementCrankshaft2Panel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.GreenDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section7left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 2).ToString()}", AnchorMax = $"{section8left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 2).ToString()}" },
                };

                CuiElement elementCrankshaft2Input = new CuiElement
                {
                    Name = "vagui.cfg.crankshaft2.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA crankshaft2 "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.crankshaft2.panel"
                };

                CuiPanel elementCrankshaft1Panel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.YellowDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section6left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 2).ToString()}", AnchorMax = $"{section7left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 2).ToString()}" },
                };

                CuiElement elementCrankshaft1Input = new CuiElement
                {
                    Name = "vagui.cfg.crankshaft1.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA crankshaft1 "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.crankshaft1.panel"
                };


                CuiButton elementSmallEnginePartsButton = new CuiButton
                {
                    Button =
                    {
                        Color = $"{ColorPalette.GreenLight.rustString}", //get the actual config value
                        Command = "vagui_option 6EXTRA smallengineparts {true/false}", //get the opposite of what the config value is
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 20,
                        Text = "This will be changed, don't worry", //get the actual config value
                        Color = $"{ColorPalette.White.rustString}"
                    },
                    RectTransform = { AnchorMin = $"{leftT.ToString()} {(bottomB - (heightLine + paddingV2) * 1).ToString()}", AnchorMax = $"{section4right.ToString()} {(topB - (heightLine + paddingV2) * 1).ToString()}" },
                };

                CuiButton elementBigEnginePartsButton = new CuiButton
                {
                    Button =
                    {
                        Color = $"{ColorPalette.GreenLight.rustString}", //get the actual config value
                        Command = "vagui_option 6EXTRA bigengineparts {true/false}", //get the opposite of what the config value is
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 20,
                        Text = "This will be changed, don't worry", //get the actual config value
                        Color = $"{ColorPalette.White.rustString}"
                    },
                    RectTransform = { AnchorMin = $"{section5left.ToString()} {(bottomB - (heightLine + paddingV2) * 1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - (heightLine + paddingV2) * 1).ToString()}" },
                };

                CuiElement elementModulesText = new CuiElement
                {
                    Name = "vagui.cfg.modules.text",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            FontSize = 14,
                            Text = $"This will change",
                            Color = $"{ColorPalette.White.rustString}"
                        },

                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{leftT.ToString()} {(bottomB-(heightLine+paddingV2)*0).ToString()}",
                            AnchorMax = $"{col3left.ToString()} {(topB-(heightLine+paddingV2)*0).ToString()}"
                        }

                    },
                    Parent = "Overlay"
                };

                CuiPanel elementModulesTwoPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.RedDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section1left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 0).ToString()}", AnchorMax = $"{section2left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 0).ToString()}" },
                };

                CuiElement elementModulesTwoInput = new CuiElement
                {
                    Name = "vagui.cfg.twomodules.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA twomodules "
                        },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.twomodules.panel"
                };

                CuiPanel elementModulesThreePanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.GreenDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section2left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 0).ToString()}", AnchorMax = $"{section3left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 0).ToString()}" },
                };

                CuiElement elementModulesThreeInput = new CuiElement
                {
                    Name = "vagui.cfg.threemodules.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA threemodules "
                        },
                        //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.threemodules.panel"
                };
                CuiPanel elementModulesFourPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.BlueDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section3left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 0).ToString()}", AnchorMax = $"{rightP.ToString()} {(-0 + topB - (heightLine + paddingV2) * 0).ToString()}" },
                };

                CuiElement elementModulesFourInput = new CuiElement
                {
                    Name = "vagui.cfg.fourmodules.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option 6EXTRA fourmodules "
                        },
                        //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.fourmodules.panel"
                };


                public GuiPageCarExtra(GuiManager manager, string title, string page) : base(manager, title)
                {
                    this.manager = manager;
                    this.title = title;
                    this.page = page;

                    DefineOptions();
                }

                public override void DefineOptions()
                {
                    base.DefineOptions();
                    //add elements to the container
                    pageContainer.Add(elementModulesText);
                    pageContainer.Add(elementModulesTwoPanel, "Overlay", "vagui.cfg.twomodules.panel");
                    pageContainer.Add(elementModulesTwoInput);
                    pageContainer.Add(elementModulesThreePanel, "Overlay", "vagui.cfg.threemodules.panel");
                    pageContainer.Add(elementModulesThreeInput);
                    pageContainer.Add(elementModulesFourPanel, "Overlay", "vagui.cfg.fourmodules.panel");
                    pageContainer.Add(elementModulesFourInput);

                    pageContainer.Add(elementSmallEnginePartsButton, "Overlay", "vagui.cfg.smallengineparts.button");
                    pageContainer.Add(elementBigEnginePartsButton, "Overlay", "vagui.cfg.bigengineparts.button");

                    pageContainer.Add(elementCrankshaftText);
                    pageContainer.Add(elementCrankshaft0Panel, "Overlay", "vagui.cfg.crankshaft0.panel");
                    pageContainer.Add(elementCrankshaft0Input);
                    pageContainer.Add(elementCrankshaft1Panel, "Overlay", "vagui.cfg.crankshaft1.panel");
                    pageContainer.Add(elementCrankshaft1Input);
                    pageContainer.Add(elementCrankshaft2Panel, "Overlay", "vagui.cfg.crankshaft2.panel");
                    pageContainer.Add(elementCrankshaft2Input);
                    pageContainer.Add(elementCrankshaft3Panel, "Overlay", "vagui.cfg.crankshaft3.panel");
                    pageContainer.Add(elementCrankshaft3Input);

                    pageContainer.Add(elementCarburetorText);
                    pageContainer.Add(elementCarburetor0Panel, "Overlay", "vagui.cfg.carburetor0.panel");
                    pageContainer.Add(elementCarburetor0Input);
                    pageContainer.Add(elementCarburetor1Panel, "Overlay", "vagui.cfg.carburetor1.panel");
                    pageContainer.Add(elementCarburetor1Input);
                    pageContainer.Add(elementCarburetor2Panel, "Overlay", "vagui.cfg.carburetor2.panel");
                    pageContainer.Add(elementCarburetor2Input);
                    pageContainer.Add(elementCarburetor3Panel, "Overlay", "vagui.cfg.carburetor3.panel");
                    pageContainer.Add(elementCarburetor3Input);

                    pageContainer.Add(elementValvesAText);
                    pageContainer.Add(elementValves0APanel, "Overlay", "vagui.cfg.valves0a.panel");
                    pageContainer.Add(elementValves0AInput);
                    pageContainer.Add(elementValves1APanel, "Overlay", "vagui.cfg.valves1a.panel");
                    pageContainer.Add(elementValves1AInput);
                    pageContainer.Add(elementValves2APanel, "Overlay", "vagui.cfg.valves2a.panel");
                    pageContainer.Add(elementValves2AInput);
                    pageContainer.Add(elementValves3APanel, "Overlay", "vagui.cfg.valves3a.panel");
                    pageContainer.Add(elementValves3AInput);

                    pageContainer.Add(elementValvesBText);
                    pageContainer.Add(elementValves0BPanel, "Overlay", "vagui.cfg.valves0b.panel");
                    pageContainer.Add(elementValves0BInput);
                    pageContainer.Add(elementValves1BPanel, "Overlay", "vagui.cfg.valves1b.panel");
                    pageContainer.Add(elementValves1BInput);
                    pageContainer.Add(elementValves2BPanel, "Overlay", "vagui.cfg.valves2b.panel");
                    pageContainer.Add(elementValves2BInput);
                    pageContainer.Add(elementValves3BPanel, "Overlay", "vagui.cfg.valves3b.panel");
                    pageContainer.Add(elementValves3BInput);

                    pageContainer.Add(elementSparkplugsAText);
                    pageContainer.Add(elementSparkplugs0APanel, "Overlay", "vagui.cfg.sparkplugs0a.panel");
                    pageContainer.Add(elementSparkplugs0AInput);
                    pageContainer.Add(elementSparkplugs1APanel, "Overlay", "vagui.cfg.sparkplugs1a.panel");
                    pageContainer.Add(elementSparkplugs1AInput);
                    pageContainer.Add(elementSparkplugs2APanel, "Overlay", "vagui.cfg.sparkplugs2a.panel");
                    pageContainer.Add(elementSparkplugs2AInput);
                    pageContainer.Add(elementSparkplugs3APanel, "Overlay", "vagui.cfg.sparkplugs3a.panel");
                    pageContainer.Add(elementSparkplugs3AInput);

                    pageContainer.Add(elementSparkplugsBText);
                    pageContainer.Add(elementSparkplugs0BPanel, "Overlay", "vagui.cfg.sparkplugs0b.panel");
                    pageContainer.Add(elementSparkplugs0BInput);
                    pageContainer.Add(elementSparkplugs1BPanel, "Overlay", "vagui.cfg.sparkplugs1b.panel");
                    pageContainer.Add(elementSparkplugs1BInput);
                    pageContainer.Add(elementSparkplugs2BPanel, "Overlay", "vagui.cfg.sparkplugs2b.panel");
                    pageContainer.Add(elementSparkplugs2BInput);
                    pageContainer.Add(elementSparkplugs3BPanel, "Overlay", "vagui.cfg.sparkplugs3b.panel");
                    pageContainer.Add(elementSparkplugs3BInput);

                    pageContainer.Add(elementPistonsAText);
                    pageContainer.Add(elementPistons0APanel, "Overlay", "vagui.cfg.pistons0a.panel");
                    pageContainer.Add(elementPistons0AInput);
                    pageContainer.Add(elementPistons1APanel, "Overlay", "vagui.cfg.pistons1a.panel");
                    pageContainer.Add(elementPistons1AInput);
                    pageContainer.Add(elementPistons2APanel, "Overlay", "vagui.cfg.pistons2a.panel");
                    pageContainer.Add(elementPistons2AInput);
                    pageContainer.Add(elementPistons3APanel, "Overlay", "vagui.cfg.pistons3a.panel");
                    pageContainer.Add(elementPistons3AInput);

                    pageContainer.Add(elementPistonsBText);
                    pageContainer.Add(elementPistons0BPanel, "Overlay", "vagui.cfg.pistons0b.panel");
                    pageContainer.Add(elementPistons0BInput);
                    pageContainer.Add(elementPistons1BPanel, "Overlay", "vagui.cfg.pistons1b.panel");
                    pageContainer.Add(elementPistons1BInput);
                    pageContainer.Add(elementPistons2BPanel, "Overlay", "vagui.cfg.pistons2b.panel");
                    pageContainer.Add(elementPistons2BInput);
                    pageContainer.Add(elementPistons3BPanel, "Overlay", "vagui.cfg.pistons3b.panel");
                    pageContainer.Add(elementPistons3BInput);
                }

                public override void GuiPageShow(BasePlayer player)
                {
                    GuiPageHide(player);
                    //update the buttons etc with right values

                    //iterate through all the page elements
                    foreach (var entry in pageContainer)
                    {
                        //input - maybe not needed?!

                        if (entry.Name.Contains(".text"))
                        {
                            var text = entry.Components[0] as CuiTextComponent;
                            switch (entry.Name)
                            {
                                case "vagui.cfg.modules.text":
                                    {
                                        text.Text = $"Chances of spawning as\n<color={ColorPalette.RedLight.hexValue}>2 MODULES = <i>{Instance.configData.dropConfigs["car"].extraSettings.randomWeight2modules.ToString("0.00")}</i></color>, <color={ColorPalette.GreenLight.hexValue}>3 MODULES = <i>{Instance.configData.dropConfigs["car"].extraSettings.randomWeight3modules.ToString("0.00")}</i></color>, <color={ColorPalette.BlueLight.hexValue}>4 MODULES = <i>{Instance.configData.dropConfigs["car"].extraSettings.randomWeight4modules.ToString("0.00")}</i></color> out of <i>{Instance.configData.dropConfigs["car"].extraSettings.randomWeightModulesSum.ToString("0.00")}</i>";
                                    }
                                    break;
                                case "vagui.cfg.crankshaft.text":
                                    {
                                        text.Text = $"Chances of spawning with a <color={ColorPalette.AquaLight.hexValue}>Crankshaft</color>\n<color={ColorPalette.YellowLight.hexValue}>LOW = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleCockpitEngineCrankshaftChances["crankshaft1"].ToString("0.00")}</i></color>, <color={ColorPalette.GreenLight.hexValue}>MED = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleCockpitEngineCrankshaftChances["crankshaft2"].ToString("0.00")}</i></color>, <color={ColorPalette.BlueLight.hexValue}>HIGH = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleCockpitEngineCrankshaftChances["crankshaft3"].ToString("0.00")}</i></color>, <color={ColorPalette.RedLight.hexValue}>NONE = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleCockpitEngineCrankshaftChances["none"].ToString("0.00")}</i></color> out of <i>{Instance.configData.dropConfigs["car"].extraSettings.randomWeightCrankshaftSum.ToString("0.00")}</i>";
                                    }
                                    break;
                                case "vagui.cfg.carburetor.text":
                                    {
                                        text.Text = $"Chances of spawning with a <color={ColorPalette.AquaLight.hexValue}>Carburetor</color>\n<color={ColorPalette.YellowLight.hexValue}>LOW = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleCockpitEngineCarburetorChances["carburetor1"].ToString("0.00")}</i></color>, <color={ColorPalette.GreenLight.hexValue}>MED = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleCockpitEngineCarburetorChances["carburetor2"].ToString("0.00")}</i></color>, <color={ColorPalette.BlueLight.hexValue}>HIGH = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleCockpitEngineCarburetorChances["carburetor3"].ToString("0.00")}</i></color>, <color={ColorPalette.RedLight.hexValue}>NONE = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleCockpitEngineCarburetorChances["none"].ToString("0.00")}</i></color> out of <i>{Instance.configData.dropConfigs["car"].extraSettings.randomWeightCarburetorSum.ToString("0.00")}</i>";
                                    }
                                    break;
                                case "vagui.cfg.valvesa.text":
                                    {
                                        text.Text = $"Chances of spawning with <color={ColorPalette.AquaLight.hexValue}>Valves</color>\n<color={ColorPalette.YellowLight.hexValue}>LOW = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleCockpitEngineValvesChances["valves1"].ToString("0.00")}</i></color>, <color={ColorPalette.GreenLight.hexValue}>MED = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleCockpitEngineValvesChances["valves2"].ToString("0.00")}</i></color>, <color={ColorPalette.BlueLight.hexValue}>HIGH = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleCockpitEngineValvesChances["valves3"].ToString("0.00")}</i></color>, <color={ColorPalette.RedLight.hexValue}>NONE = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleCockpitEngineValvesChances["none"].ToString("0.00")}</i></color> out of <i>{Instance.configData.dropConfigs["car"].extraSettings.randomWeightValvesASum.ToString("0.00")}</i>";
                                    }
                                    break;
                                case "vagui.cfg.valvesb.text":
                                    {
                                        text.Text = $"Chances of spawning with <color={ColorPalette.AquaLight.hexValue}>Valves B</color> (BIG V8 ENGINE ONLY)\n<color={ColorPalette.YellowLight.hexValue}>LOW = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleBigEngineValvesBChances["valves1"].ToString("0.00")}</i></color>, <color={ColorPalette.GreenLight.hexValue}>MED = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleBigEngineValvesBChances["valves2"].ToString("0.00")}</i></color>, <color={ColorPalette.BlueLight.hexValue}>HIGH = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleBigEngineValvesBChances["valves3"].ToString("0.00")}</i></color>, <color={ColorPalette.RedLight.hexValue}>NONE = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleBigEngineValvesBChances["none"].ToString("0.00")}</i></color> out of <i>{Instance.configData.dropConfigs["car"].extraSettings.randomWeightValvesBSum.ToString("0.00")}</i>";
                                    }
                                    break;
                                case "vagui.cfg.sparkplugsa.text":
                                    {
                                        text.Text = $"Chances of spawning with <color={ColorPalette.AquaLight.hexValue}>Sparkplugs</color>\n<color={ColorPalette.YellowLight.hexValue}>LOW = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleCockpitEngineSparkplugsChances["sparkplugs1"].ToString("0.00")}</i></color>, <color={ColorPalette.GreenLight.hexValue}>MED = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleCockpitEngineSparkplugsChances["sparkplugs2"].ToString("0.00")}</i></color>, <color={ColorPalette.BlueLight.hexValue}>HIGH = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleCockpitEngineSparkplugsChances["sparkplugs3"].ToString("0.00")}</i></color>, <color={ColorPalette.RedLight.hexValue}>NONE = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleCockpitEngineSparkplugsChances["none"].ToString("0.00")}</i></color> out of <i>{Instance.configData.dropConfigs["car"].extraSettings.randomWeightSparkplugsASum.ToString("0.00")}</i>";
                                    }
                                    break;
                                case "vagui.cfg.sparkplugsb.text":
                                    {
                                        text.Text = $"Chances of spawning with <color={ColorPalette.AquaLight.hexValue}>Sparkplugs B</color> (BIG V8 ENGINE ONLY)\n<color={ColorPalette.YellowLight.hexValue}>LOW = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleBigEngineSparkplugsBChances["sparkplugs1"].ToString("0.00")}</i></color>, <color={ColorPalette.GreenLight.hexValue}>MED = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleBigEngineSparkplugsBChances["sparkplugs2"].ToString("0.00")}</i></color>, <color={ColorPalette.BlueLight.hexValue}>HIGH = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleBigEngineSparkplugsBChances["sparkplugs3"].ToString("0.00")}</i></color>, <color={ColorPalette.RedLight.hexValue}>NONE = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleBigEngineSparkplugsBChances["none"].ToString("0.00")}</i></color> out of <i>{Instance.configData.dropConfigs["car"].extraSettings.randomWeightSparkplugsBSum.ToString("0.00")}</i>";
                                    }
                                    break;
                                case "vagui.cfg.pistonsa.text":
                                    {
                                        text.Text = $"Chances of spawning with <color={ColorPalette.AquaLight.hexValue}>Pistons</color>\n<color={ColorPalette.YellowLight.hexValue}>LOW = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleCockpitEnginePistonsChances["pistons1"].ToString("0.00")}</i></color>, <color={ColorPalette.GreenLight.hexValue}>MED = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleCockpitEnginePistonsChances["pistons2"].ToString("0.00")}</i></color>, <color={ColorPalette.BlueLight.hexValue}>HIGH = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleCockpitEnginePistonsChances["pistons3"].ToString("0.00")}</i></color>, <color={ColorPalette.RedLight.hexValue}>NONE = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleCockpitEnginePistonsChances["none"].ToString("0.00")}</i></color> out of <i>{Instance.configData.dropConfigs["car"].extraSettings.randomWeightPistonsASum.ToString("0.00")}</i>";
                                    }
                                    break;
                                case "vagui.cfg.pistonsb.text":
                                    {
                                        text.Text = $"Chances of spawning with <color={ColorPalette.AquaLight.hexValue}>Pistons B</color> (BIG V8 ENGINE ONLY)\n<color={ColorPalette.YellowLight.hexValue}>LOW = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleBigEnginePistonsBChances["pistons1"].ToString("0.00")}</i></color>, <color={ColorPalette.GreenLight.hexValue}>MED = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleBigEnginePistonsBChances["pistons2"].ToString("0.00")}</i></color>, <color={ColorPalette.BlueLight.hexValue}>HIGH = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleBigEnginePistonsBChances["pistons3"].ToString("0.00")}</i></color>, <color={ColorPalette.RedLight.hexValue}>NONE = <i>{Instance.configData.dropConfigs["car"].extraSettings.moduleBigEnginePistonsBChances["none"].ToString("0.00")}</i></color> out of <i>{Instance.configData.dropConfigs["car"].extraSettings.randomWeightPistonsBSum.ToString("0.00")}</i>";
                                    }
                                    break;
                            }
                        }

                        //button and button text
                        if (entry.Name.Contains(".button"))
                        {
                            var button = entry.Components[0] as CuiButtonComponent;
                            switch (entry.Name)
                            {
                                case "vagui.cfg.smallengineparts.button":
                                    {
                                        var boolean = Instance.configData.dropConfigs["car"].extraSettings.moduleCockpitEngineComesWithParts;
                                        button.Color = Instance.GetBoolColor(boolean);
                                        button.Command = $"vagui_option {page} smallengineparts {(!boolean).ToString()}"; //reverse of what it is
                                    }
                                    break;
                                case "vagui.cfg.bigengineparts.button":
                                    {
                                        var boolean = Instance.configData.dropConfigs["car"].extraSettings.moduleBigEngineComesWithParts;
                                        button.Color = Instance.GetBoolColor(boolean);
                                        button.Command = $"vagui_option {page} bigengineparts {(!boolean).ToString()}"; //reverse of what it is
                                    }
                                    break;
                            }
                        }
                        //text
                        if (entry.Parent.Contains(".button"))
                        {
                            var text = entry.Components[0] as CuiTextComponent;

                            switch (entry.Parent)
                            {
                                case "vagui.cfg.smallengineparts.button":
                                    {
                                        text.Text = Instance.configData.dropConfigs["car"].extraSettings.moduleCockpitEngineComesWithParts ? "Small engine (I4) spawns with parts" : "Small engine (I4) has no parts";
                                    }
                                    break;
                                case "vagui.cfg.bigengineparts.button":
                                    {
                                        text.Text = Instance.configData.dropConfigs["car"].extraSettings.moduleBigEngineComesWithParts ? "Big engine (V8) spawns with parts" : "Big engine (V8) has no parts";
                                    }
                                    break;
                            }
                        }

                    }
                    //after everything's ready and updated, show the page

                    base.GuiPageShow(player);
                }

                public override void GuiPageHide(BasePlayer player)
                {
                    base.GuiPageHide(player);

                    CuiHelper.DestroyUi(player, "vagui.cfg.modules.text");
                    CuiHelper.DestroyUi(player, "vagui.cfg.twomodules.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.twomodules.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.threemodules.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.threemodules.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.fourmodules.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.fourmodules.input");

                    CuiHelper.DestroyUi(player, "vagui.cfg.smallengineparts.button");
                    CuiHelper.DestroyUi(player, "vagui.cfg.bigengineparts.button");

                    CuiHelper.DestroyUi(player, "vagui.cfg.crankshaft.text");
                    CuiHelper.DestroyUi(player, "vagui.cfg.crankshaft1.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.crankshaft1.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.crankshaft2.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.crankshaft2.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.crankshaft3.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.crankshaft3.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.crankshaft0.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.crankshaft0.input");

                    CuiHelper.DestroyUi(player, "vagui.cfg.carburetor.text");
                    CuiHelper.DestroyUi(player, "vagui.cfg.carburetor1.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.carburetor1.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.carburetor2.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.carburetor2.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.carburetor3.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.carburetor3.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.carburetor0.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.carburetor0.input");

                    CuiHelper.DestroyUi(player, "vagui.cfg.valvesa.text");
                    CuiHelper.DestroyUi(player, "vagui.cfg.valves1a.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.valves1a.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.valves2a.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.valves2a.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.valves3a.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.valves3a.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.valves0a.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.valves0a.input");

                    CuiHelper.DestroyUi(player, "vagui.cfg.valvesb.text");
                    CuiHelper.DestroyUi(player, "vagui.cfg.valves1b.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.valves1b.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.valves2b.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.valves2b.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.valves3b.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.valves3b.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.valves0b.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.valves0b.input");

                    CuiHelper.DestroyUi(player, "vagui.cfg.pistonsa.text");
                    CuiHelper.DestroyUi(player, "vagui.cfg.pistons1a.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.pistons1a.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.pistons2a.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.pistons2a.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.pistons3a.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.pistons3a.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.pistons0a.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.pistons0a.input");

                    CuiHelper.DestroyUi(player, "vagui.cfg.pistonsb.text");
                    CuiHelper.DestroyUi(player, "vagui.cfg.pistons1b.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.pistons1b.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.pistons2b.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.pistons2b.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.pistons3b.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.pistons3b.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.pistons0b.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.pistons0b.input");

                    CuiHelper.DestroyUi(player, "vagui.cfg.sparkplugsa.text");
                    CuiHelper.DestroyUi(player, "vagui.cfg.sparkplugs1a.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.sparkplugs1a.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.sparkplugs2a.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.sparkplugs2a.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.sparkplugs3a.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.sparkplugs3a.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.sparkplugs0a.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.sparkplugs0a.input");

                    CuiHelper.DestroyUi(player, "vagui.cfg.sparkplugsb.text");
                    CuiHelper.DestroyUi(player, "vagui.cfg.sparkplugs1b.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.sparkplugs1b.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.sparkplugs2b.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.sparkplugs2b.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.sparkplugs3b.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.sparkplugs3b.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.sparkplugs0b.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.sparkplugs0b.input");
                }
            }

            public class GuiPageParticularDrop : GuiPage
            {
                string kind;
                bool extra;

                CuiButton elementApplyButton = new CuiButton
                {
                    Button =
                    {
                        Color = $"{ColorPalette.YellowLight.rustString}",
                        Command = "vagui_option {page} apply ",
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 20,
                        Text = "This will change",
                        Color = $"{ColorPalette.White.rustString}"
                    },
                    RectTransform = { AnchorMin = $"{leftT.ToString()} {(bottomB - (heightLine + paddingV2) * 9).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - (heightLine + paddingV2) * 9).ToString()}" },
                };

                CuiButton elementGotoExtraButton = new CuiButton
                {
                    Button =
                    {
                        Color = $"{ColorPalette.YellowLight.rustString}",
                        Command = "vagui_page 6EXTRA", //HARDCODED IN!
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 20,
                        Text = "Edit chances of modular car's engine to spawn with components...", //ALSO HARDCODED IN
                        Color = $"{ColorPalette.White.rustString}"
                    },
                    RectTransform = { AnchorMin = $"{leftT.ToString()} {(bottomB - (heightLine + paddingV2) * 8).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - (heightLine + paddingV2) * 8).ToString()}" },
                };

                CuiPanel elementHealthPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.White.rustString,
                    },
                    //AnchorMin = $"{leftP.ToString()} {(bottomB - 2 * (paddingV + heightButton)).ToString()}", AnchorMax = $"{rightL.ToString()} {(topB - 2 * (paddingV + heightButton)).ToString()
                    RectTransform = { AnchorMin = $"{col3left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 7).ToString()}", AnchorMax = $"{rightP.ToString()} {(-0 + topB - (heightLine + paddingV2) * 7).ToString()}" },
                };

                CuiElement elementHealthInput = new CuiElement
                {
                    Name = "vagui.cfg.health.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.LimeDark.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option {page} health " //THIS IS A SPECIAL CASE INPUT. FUCK. LOOK IN THE CONSTRUCTOR, THEN.
                        },
                        //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.health.panel"
                };

                //elements specific to that page
                CuiElement elementHealthText = new CuiElement
                {
                    Name = "vagui.cfg.health.text",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            FontSize = 20,
                            Text = $"This will change",
                            Color = $"{ColorPalette.White.rustString}"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{leftT.ToString()} {(bottomB-(heightLine+paddingV2)*7).ToString()}",
                            AnchorMax = $"{col3left.ToString()} {(topB-(heightLine+paddingV2)*7).ToString()}"
                        }
                    },
                    Parent = "Overlay"
                };


                CuiPanel elementFuelPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.White.rustString,
                    },
                    //AnchorMin = $"{leftP.ToString()} {(bottomB - 2 * (paddingV + heightButton)).ToString()}", AnchorMax = $"{rightL.ToString()} {(topB - 2 * (paddingV + heightButton)).ToString()
                    RectTransform = { AnchorMin = $"{col3left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 6).ToString()}", AnchorMax = $"{rightP.ToString()} {(-0 + topB - (heightLine + paddingV2) * 6).ToString()}" },
                };

                CuiElement elementFuelInput = new CuiElement
                {
                    Name = "vagui.cfg.fuel.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.LimeDark.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option {page} fuel " //THIS IS A SPECIAL CASE INPUT. FUCK. LOOK IN THE CONSTRUCTOR, THEN.
                        },
                        //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.fuel.panel"
                };

                //elements specific to that page
                CuiElement elementFuelText = new CuiElement
                {
                    Name = "vagui.cfg.fuel.text",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            FontSize = 20,
                            Text = $"This will change",
                            Color = $"{ColorPalette.White.rustString}"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{leftT.ToString()} {(bottomB-(heightLine+paddingV2)*6).ToString()}",
                            AnchorMax = $"{col3left.ToString()} {(topB-(heightLine+paddingV2)*6).ToString()}"
                        }
                    },
                    Parent = "Overlay"
                };

                CuiElement elementWeightsText = new CuiElement
                {
                    Name = "vagui.cfg.weights.text",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            FontSize = 14,
                            Text = "This will change",
                            Color = $"{ColorPalette.White.rustString}"
                        },

                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{leftT.ToString()} {(bottomB-(heightLine+paddingV2)*5).ToString()}",
                            AnchorMax = $"{col3left.ToString()} {(topB-(heightLine+paddingV2)*5).ToString()}"
                        }

                    },
                    Parent = "Overlay"
                };

                CuiPanel elementLootWeightPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.PurpleDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section1Bleft.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 5).ToString()}", AnchorMax = $"{section2Bleft.ToString()} {(-0 + topB - (heightLine + paddingV2) * 5).ToString()}" },
                };

                CuiElement elementLootWeightInput = new CuiElement
                {
                    Name = "vagui.cfg.lootweight.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option {page} lootweight "
                        },
                        //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.lootweight.panel"
                };

                CuiPanel elementDropWeightPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.GreenDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section2Bleft.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 5).ToString()}", AnchorMax = $"{rightP.ToString()} {(-0 + topB - (heightLine + paddingV2) * 5).ToString()}" },
                };

                CuiElement elementDropWeightInput = new CuiElement
                {
                    Name = "vagui.cfg.dropweight.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option {page} dropweight "
                        },
                        //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.dropweight.panel"
                };


                CuiPanel elementPermissionPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.White.rustString,
                    },
                    //AnchorMin = $"{leftP.ToString()} {(bottomB - 2 * (paddingV + heightButton)).ToString()}", AnchorMax = $"{rightL.ToString()} {(topB - 2 * (paddingV + heightButton)).ToString()
                    RectTransform = { AnchorMin = $"{col3left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 4).ToString()}", AnchorMax = $"{rightP.ToString()} {(-0 + topB - (heightLine + paddingV2) * 4).ToString()}" },
                };

                CuiElement elementPermissionInput = new CuiElement
                {
                    Name = "vagui.cfg.permission.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.LimeDark.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option {page} permission " //THIS IS A SPECIAL CASE INPUT. FUCK. LOOK IN THE CONSTRUCTOR, THEN.
                        },
                        //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.permission.panel"
                };

                //elements specific to that page
                CuiElement elementPermissionText = new CuiElement
                {
                    Name = "vagui.cfg.permission.text",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            FontSize = 20,
                            Text = $"This will change",
                            Color = $"{ColorPalette.White.rustString}"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{leftT.ToString()} {(bottomB-(heightLine+paddingV2)*4).ToString()}",
                            AnchorMax = $"{col3left.ToString()} {(topB-(heightLine+paddingV2)*4).ToString()}"
                        }
                    },
                    Parent = "Overlay"
                };

                CuiPanel elementCurrencyItemPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.White.rustString,
                    },
                    //AnchorMin = $"{leftP.ToString()} {(bottomB - 2 * (paddingV + heightButton)).ToString()}", AnchorMax = $"{rightL.ToString()} {(topB - 2 * (paddingV + heightButton)).ToString()
                    RectTransform = { AnchorMin = $"{col3left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 3).ToString()}", AnchorMax = $"{rightP.ToString()} {(-0 + topB - (heightLine + paddingV2) * 3).ToString()}" },
                };

                CuiElement elementCurrencyItemInput = new CuiElement
                {
                    Name = "vagui.cfg.currencyitem.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.LimeDark.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option {page} currencyitem " //THIS IS A SPECIAL CASE INPUT. FUCK. LOOK IN THE CONSTRUCTOR, THEN.
                        },
                        //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.currencyitem.panel"
                };

                //elements specific to that page
                CuiElement elementCurrencyItemText = new CuiElement
                {
                    Name = "vagui.cfg.currencyitem.text",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            FontSize = 20,
                            Text = $"This will change",
                            Color = $"{ColorPalette.White.rustString}"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{leftT.ToString()} {(bottomB-(heightLine+paddingV2)*3).ToString()}",
                            AnchorMax = $"{col3left.ToString()} {(topB-(heightLine+paddingV2)*3).ToString()}"
                        }
                    },
                    Parent = "Overlay"
                };


                CuiElement elementPriceText = new CuiElement
                {
                    Name = "vagui.cfg.price.text",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            FontSize = 16,
                            Text = "This will change",
                            Color = $"{ColorPalette.White.rustString}"
                        },

                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{leftT.ToString()} {(bottomB-(heightLine+paddingV2)*2).ToString()}",
                            AnchorMax = $"{col3left.ToString()} {(topB-(heightLine+paddingV2)*2).ToString()}"
                        }

                    },
                    Parent = "Overlay"
                };

                CuiPanel elementPriceCurrencyPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.YellowDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section1left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 2).ToString()}", AnchorMax = $"{section2left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 2).ToString()}" },
                };

                CuiElement elementPriceCurrencyInput = new CuiElement
                {
                    Name = "vagui.cfg.pricecurrency.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option {page} pricecurrency "
                        },
                        //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.pricecurrency.panel"
                };

                CuiPanel elementPriceServerRewardsPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.RedDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section2left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 2).ToString()}", AnchorMax = $"{section3left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 2).ToString()}" },
                };

                CuiElement elementPriceServerRewardsInput = new CuiElement
                {
                    Name = "vagui.cfg.priceserverrewards.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option {page} priceserverrewards "
                        },
                        //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.priceserverrewards.panel"
                };

                CuiPanel elementPriceEconomicsPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.BlueDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section3left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 2).ToString()}", AnchorMax = $"{rightP.ToString()} {(-0 + topB - (heightLine + paddingV2) * 2).ToString()}" },
                };

                CuiElement elementPriceEconomicsInput = new CuiElement
                {
                    Name = "vagui.cfg.priceeconomics.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.White.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option {page} priceeconomics " //will have to be dynamic
                        },
                        //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.priceeconomics.panel"
                };


                CuiPanel elementRestockAmountPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.White.rustString,
                    },
                    //AnchorMin = $"{leftP.ToString()} {(bottomB - 2 * (paddingV + heightButton)).ToString()}", AnchorMax = $"{rightL.ToString()} {(topB - 2 * (paddingV + heightButton)).ToString()
                    RectTransform = { AnchorMin = $"{col3left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 1).ToString()}", AnchorMax = $"{rightP.ToString()} {(-0 + topB - (heightLine + paddingV2) * 1).ToString()}" },
                };

                CuiElement elementRestockAmountInput = new CuiElement
                {
                    Name = "vagui.cfg.vendingrestockamount.input",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            CharsLimit = 32,
                            Align = TextAnchor.MiddleCenter,
                            Color = ColorPalette.LimeDark.rustString,
                            FontSize = 20,
                            Text = "",
                            IsPassword = false,
                            Command = "vagui_option {page} vendingrestockamount " //THIS IS A SPECIAL CASE INPUT. FUCK. LOOK IN THE CONSTRUCTOR, THEN.
                        },
                        //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                    },
                    Parent = "vagui.cfg.vendingrestockamount.panel"
                };

                //elements specific to that page
                CuiElement elementRestockAmountText = new CuiElement
                {
                    Name = "vagui.cfg.vendingrestockamount.text",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            FontSize = 20,
                            Text = $"This will change",
                            Color = $"{ColorPalette.White.rustString}"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{leftT.ToString()} {(bottomB-(heightLine+paddingV2)*1).ToString()}",
                            AnchorMax = $"{col3left.ToString()} {(topB-(heightLine+paddingV2)*1).ToString()}"
                        }
                    },
                    Parent = "Overlay"
                };


                CuiButton elementVendingEnable = new CuiButton
                {
                    Button =
                    {
                        Color = $"{ColorPalette.GreenLight.rustString}", //get the actual config value
                        Command = "vagui_option {kind} parachutedamagedetaches {lenghty value}", //get the opposite of what the config value is
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 20,
                        Text = "This will be changed, don't worry", //get the actual config value
                        Color = $"{ColorPalette.White.rustString}"
                    },
                    RectTransform = { AnchorMin = $"{leftT.ToString()} {(bottomB - (heightLine + paddingV2) * 0).ToString()}", AnchorMax = $"{section4right.ToString()} {(topB - (heightLine + paddingV2) * 0).ToString()}" },
                };

                CuiButton elementVaBuyEnable = new CuiButton
                {
                    Button =
                    {
                        Color = $"{ColorPalette.GreenLight.rustString}", //get the actual config value
                        Command = "vagui_option {kind} parachutedecay {lenghty value}", //get the opposite of what the config value is
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 20,
                        Text = "This will be changed, don't worry", //get the actual config value
                        Color = $"{ColorPalette.White.rustString}"
                    },
                    RectTransform = { AnchorMin = $"{section5left.ToString()} {(bottomB - (heightLine + paddingV2) * 0).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - (heightLine + paddingV2) * 0).ToString()}" },
                };

                string page;

                public GuiPageParticularDrop(GuiManager manager, string title, string kind, string page, bool extra = false) : base(manager, title)
                {
                    this.manager = manager;
                    this.title = title;
                    this.kind = kind;
                    this.page = page;
                    this.extra = extra;

                    DefineOptions();
                }
                public override void DefineOptions()
                {
                    base.DefineOptions();
                    //add all the elements to the container here
                    pageContainer.Add(elementVendingEnable, "Overlay", "vagui.cfg.vendingenable.button");
                    pageContainer.Add(elementVaBuyEnable, "Overlay", "vagui.cfg.vabuyenable.button");

                    pageContainer.Add(elementRestockAmountText);
                    pageContainer.Add(elementRestockAmountPanel, "Overlay", "vagui.cfg.vendingrestockamount.panel");

                    //do this with every input
                    var inputRestockAmount = elementRestockAmountInput.Components[0] as CuiInputFieldComponent;
                    inputRestockAmount.Command = inputRestockAmount.Command.Replace("{page}", page);
                    pageContainer.Add(elementRestockAmountInput);

                    pageContainer.Add(elementPriceText);

                    pageContainer.Add(elementPriceCurrencyPanel, "Overlay", "vagui.cfg.pricecurrency.panel");

                    var priceCurrency = elementPriceCurrencyInput.Components[0] as CuiInputFieldComponent;
                    priceCurrency.Command = priceCurrency.Command.Replace("{page}", page);
                    pageContainer.Add(elementPriceCurrencyInput);

                    pageContainer.Add(elementPriceServerRewardsPanel, "Overlay", "vagui.cfg.priceserverrewards.panel");

                    var priceServerRewards = elementPriceServerRewardsInput.Components[0] as CuiInputFieldComponent;
                    priceServerRewards.Command = priceServerRewards.Command.Replace("{page}", page);
                    pageContainer.Add(elementPriceServerRewardsInput);

                    pageContainer.Add(elementPriceEconomicsPanel, "Overlay", "vagui.cfg.priceeconomics.panel");

                    var priceEconomics = elementPriceEconomicsInput.Components[0] as CuiInputFieldComponent;
                    priceEconomics.Command = priceEconomics.Command.Replace("{page}", page);
                    pageContainer.Add(elementPriceEconomicsInput);

                    pageContainer.Add(elementCurrencyItemText);
                    pageContainer.Add(elementCurrencyItemPanel, "Overlay", "vagui.cfg.currencyitem.panel");

                    //do this with every input
                    var inputCurrencyItem = elementCurrencyItemInput.Components[0] as CuiInputFieldComponent;
                    inputCurrencyItem.Command = inputCurrencyItem.Command.Replace("{page}", page);
                    pageContainer.Add(elementCurrencyItemInput);

                    pageContainer.Add(elementPermissionText);
                    pageContainer.Add(elementPermissionPanel, "Overlay", "vagui.cfg.permission.panel");

                    //do this with every input
                    var inputPermission = elementPermissionInput.Components[0] as CuiInputFieldComponent;
                    inputPermission.Command = inputPermission.Command.Replace("{page}", page);
                    pageContainer.Add(elementPermissionInput);
                    //do this with every input

                    if (kind != "normal")
                    {
                        pageContainer.Add(elementWeightsText);
                        pageContainer.Add(elementDropWeightPanel, "Overlay", "vagui.cfg.dropweight.panel");
                        pageContainer.Add(elementLootWeightPanel, "Overlay", "vagui.cfg.lootweight.panel");

                        var inputDrop = elementDropWeightInput.Components[0] as CuiInputFieldComponent;
                        inputDrop.Command = inputDrop.Command.Replace("{page}", page);
                        pageContainer.Add(elementDropWeightInput);

                        var inputLoot = elementLootWeightInput.Components[0] as CuiInputFieldComponent;
                        inputLoot.Command = inputLoot.Command.Replace("{page}", page);
                        pageContainer.Add(elementLootWeightInput);

                        if (kind != "crate")
                        {
                            pageContainer.Add(elementFuelText);
                            pageContainer.Add(elementFuelPanel, "Overlay", "vagui.cfg.fuel.panel");

                            var inputFuel = elementFuelInput.Components[0] as CuiInputFieldComponent;
                            inputFuel.Command = inputFuel.Command.Replace("{page}", page);
                            pageContainer.Add(elementFuelInput);

                            pageContainer.Add(elementHealthText);
                            pageContainer.Add(elementHealthPanel, "Overlay", "vagui.cfg.health.panel");

                            var inputHealth = elementHealthInput.Components[0] as CuiInputFieldComponent;
                            inputHealth.Command = inputHealth.Command.Replace("{page}", page);
                            pageContainer.Add(elementHealthInput);

                            if (kind == "car")
                            {
                                pageContainer.Add(elementGotoExtraButton, "Overlay", "vagui.cfg.gotoextra.button");
                            }

                            pageContainer.Add(elementApplyButton, "Overlay", "vagui.cfg.apply.button");
                        }
                    }
                }

                public override void GuiPageShow(BasePlayer player)
                {
                    GuiPageHide(player);
                    //update the buttons etc with right values

                    //iterate through all the page elements
                    foreach (var entry in pageContainer)
                    {
                        //input - maybe not needed?!

                        if (entry.Name.Contains(".text"))
                        {
                            var text = entry.Components[0] as CuiTextComponent;
                            switch (entry.Name)
                            {
                                case "vagui.cfg.vendingrestockamount.text":
                                    {
                                        var amount = kind == "normal" ? Instance.configData.vendingMachineNormalQuantity : Instance.configData.dropConfigs[kind].vendingMachineRestockQuantity;
                                        text.Text = $"Amount when re-stocking auto vending machine: <color={ColorPalette.LimeLight.hexValue}><i>{amount}</i></color>";
                                    }
                                    break;
                                case "vagui.cfg.price.text":
                                    {
                                        var price1 = kind == "normal" ? Instance.configData.priceNormalCurrency : Instance.configData.dropConfigs[kind].priceCurrency;
                                        var price2 = kind == "normal" ? Instance.configData.priceNormalServerRewards : Instance.configData.dropConfigs[kind].priceServerRewards;
                                        var price3 = kind == "normal" ? Instance.configData.priceNormalEconomics : Instance.configData.dropConfigs[kind].priceEconomics;
                                        text.Text = $"Prices: <color={ColorPalette.YellowLight.hexValue}><i>{price1}</i> (Currency)</color>, <color={ColorPalette.RedLight.hexValue}><i>{price2}</i> (ServerRewards)</color>, <color={ColorPalette.BlueLight.hexValue}><i>{price3}</i> (Economics)</color>";
                                    }
                                    break;
                                case "vagui.cfg.currencyitem.text":
                                    {
                                        var itemID = kind == "normal" ? Instance.configData.normalCurrencyItemID : Instance.configData.dropConfigs[kind].currencyItemID;
                                        string itemName = ItemManager.FindItemDefinition(itemID).displayName.translated;
                                        text.Text = $"Currency item: <color={ColorPalette.LimeLight.hexValue}><i>{itemName}</i> (ID: {itemID})</color>";
                                    }
                                    break;
                                case "vagui.cfg.permission.text":
                                    {
                                        var permission = kind == "normal" ? Instance.configData.permissionNormal : Instance.configData.dropConfigs[kind].permission;

                                        var txt = "[none needed]";

                                        if (permission?.ToString() != null)
                                        {
                                            txt = permission;
                                        }

                                        text.Text = $"<color={ColorPalette.AquaLight.hexValue}>/vabuy</color> permission: <color={ColorPalette.LimeLight.hexValue}><i>{txt}</i></color>";
                                    }
                                    break;
                                case "vagui.cfg.weights.text":
                                    {
                                        text.Text = $"Chances of occurring\n<color={ColorPalette.PurpleLight.hexValue}><i>{Instance.configData.dropConfigs[kind].randomLootWeight.ToString("0.00")}</i> in <i>{Instance.configData.weightSumLoot.ToString("0.00")}</i> (loot crates)</color>, <color={ColorPalette.GreenLight.hexValue}><i>{Instance.configData.dropConfigs[kind].randomDropWeight.ToString("0.00")}</i> in <i>{Instance.configData.weightSumDrop.ToString("0.00")}</i> (cargo plane event)</color>";
                                    }
                                    break;
                                case "vagui.cfg.fuel.text":
                                    {
                                        var fuelNeed = Instance.configData.dropConfigs[kind].comesWithFuel > 0 ? $"<i>{Instance.configData.dropConfigs[kind].comesWithFuel}</i> Low Grade Fuel" : $"<i>[no fuel]</i>";
                                        text.Text = $"Fill the fuel tank with: <color={ColorPalette.LimeLight.hexValue}>{fuelNeed}</color>";
                                    }
                                    break;
                                case "vagui.cfg.health.text":
                                    {
                                        //you're sending out 100 the actual value
                                        text.Text = $"Initial vehicle health: <i><color={ColorPalette.LimeLight.hexValue}>{(Instance.configData.dropConfigs[kind].comesWithHealth * 100F).ToString("0.00")}%</color></i>";
                                    }
                                    break;
                            }
                        }

                        //button and button text
                        if (entry.Name.Contains(".button"))
                        {
                            var button = entry.Components[0] as CuiButtonComponent;
                            switch (entry.Name)
                            {
                                case "vagui.cfg.vendingenable.button":
                                    {
                                        var boolean = kind == "normal" ? Instance.configData.vendingMachineHasNormal : Instance.configData.dropConfigs[kind].enableInVendingMachine;
                                        button.Color = Instance.GetBoolColor(boolean);
                                        button.Command = $"vagui_option {page} vendingenable {(!boolean).ToString()}"; //reverse of what it is
                                    }
                                    break;
                                case "vagui.cfg.vabuyenable.button":
                                    {
                                        var boolean = kind == "normal" ? Instance.configData.commandShopHasNormal : Instance.configData.dropConfigs[kind].enableInCommandShop;
                                        button.Color = Instance.GetBoolColor(boolean);
                                        button.Command = $"vagui_option {page} vabuyenable {(!boolean).ToString()}"; //reverse of what it is
                                    }
                                    break;
                                case "vagui.cfg.apply.button":
                                    {
                                        var boolean = Instance.configData.dropConfigs[kind].applyToNatural;
                                        button.Color = Instance.GetBoolColor(boolean);
                                        button.Command = $"vagui_option {page} apply {(!boolean).ToString()}"; //reverse of what it is
                                    }
                                    break;
                            }
                        }
                        //text
                        if (entry.Parent.Contains(".button"))
                        {
                            var text = entry.Components[0] as CuiTextComponent;

                            switch (entry.Parent)
                            {
                                case "vagui.cfg.vendingenable.button":
                                    {
                                        var boolean = kind == "normal" ? Instance.configData.vendingMachineHasNormal : Instance.configData.dropConfigs[kind].enableInVendingMachine;
                                        text.Text = boolean ? "Available in auto Vending Machine" : "Not available in auto Vending Machine";
                                    }
                                    break;
                                case "vagui.cfg.vabuyenable.button":
                                    {
                                        var boolean = kind == "normal" ? Instance.configData.commandShopHasNormal : Instance.configData.dropConfigs[kind].enableInCommandShop;
                                        var vabuy = $"<color={ColorPalette.AquaDark.hexValue}>/vabuy</color>";
                                        text.Text = boolean ? $"Available through {vabuy}" : $"Not available through {vabuy}";
                                    }
                                    break;
                                case "vagui.cfg.apply.button":
                                    {
                                        var whatSettings = kind == "car" ? " fuel, health & engine component" : " fuel & health";
                                        text.Text = Instance.configData.dropConfigs[kind].applyToNatural ? $"Apply{whatSettings} settings to all newly spawned {kind}s" : $"Apply{whatSettings} settings to airdropped {kind}s only";
                                    }
                                    break;
                            }
                        }

                    }
                    //after everything's ready and updated, show the page

                    base.GuiPageShow(player);
                }

                public override void GuiPageHide(BasePlayer player)
                {
                    base.GuiPageHide(player);
                    //hide all the elements
                    CuiHelper.DestroyUi(player, "vagui.cfg.vendingenable.button");
                    CuiHelper.DestroyUi(player, "vagui.cfg.vabuyenable.button");
                    CuiHelper.DestroyUi(player, "vagui.cfg.vendingrestockamount.text");
                    CuiHelper.DestroyUi(player, "vagui.cfg.vendingrestockamount.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.vendingrestockamount.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.price.text");
                    CuiHelper.DestroyUi(player, "vagui.cfg.pricecurrency.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.pricecurrency.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.priceserverrewards.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.priceserverrewards.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.priceeconomics.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.priceeconomics.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.currencyitem.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.currencyitem.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.currencyitem.text");
                    CuiHelper.DestroyUi(player, "vagui.cfg.permission.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.permission.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.permission.text");

                    CuiHelper.DestroyUi(player, "vagui.cfg.weights.text");
                    CuiHelper.DestroyUi(player, "vagui.cfg.dropweight.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.dropweight.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.lootweight.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.lootweight.input");

                    CuiHelper.DestroyUi(player, "vagui.cfg.fuel.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.fuel.text");
                    CuiHelper.DestroyUi(player, "vagui.cfg.fuel.panel");

                    CuiHelper.DestroyUi(player, "vagui.cfg.health.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.health.text");
                    CuiHelper.DestroyUi(player, "vagui.cfg.health.panel");

                    CuiHelper.DestroyUi(player, "vagui.cfg.gotoextra.button");
                    CuiHelper.DestroyUi(player, "vagui.cfg.apply.button");
                }
            }

            public class GuiPageAllDrops : GuiPage
            {
                CuiPanel elementDropInaccuracyPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.White.rustString,
                    },
                    RectTransform = { AnchorMin = $"{col3left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 9).ToString()}", AnchorMax = $"{rightP.ToString()} {(-0 + topB - (heightLine + paddingV2) * 9).ToString()}" },
                };

                CuiElement elementDropInaccuracyInput = new CuiElement
                {
                    Name = "vagui.cfg.dropinaccuracy.input",
                    Components =
                {
                    new CuiInputFieldComponent
                    {
                        CharsLimit = 32,
                        Align = TextAnchor.MiddleCenter,
                        Color = ColorPalette.LimeDark.rustString,
                        FontSize = 20,
                        Text = "",
                        IsPassword = false,
                        Command = "vagui_option 1 dropinaccuracy "
                    },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                },
                    Parent = "vagui.cfg.dropinaccuracy.panel"
                };

                //elements specific to that page
                CuiElement elementDropInaccuracyText = new CuiElement
                {
                    Name = "vagui.cfg.dropinaccuracy.text",
                    Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 20,
                        Text = $"This will change",
                        Color = $"{ColorPalette.White.rustString}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"{leftT.ToString()} {(bottomB-(heightLine+paddingV2)*9).ToString()}",
                        AnchorMax = $"{col3left.ToString()} {(topB-(heightLine+paddingV2)*9).ToString()}"
                    }
                },
                    Parent = "Overlay"
                };

                CuiPanel elementDropSpeedPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.White.rustString,
                    },
                    RectTransform = { AnchorMin = $"{col3left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 8).ToString()}", AnchorMax = $"{rightP.ToString()} {(-0 + topB - (heightLine + paddingV2) * 8).ToString()}" },
                };

                CuiElement elementDropSpeedInput = new CuiElement
                {
                    Name = "vagui.cfg.dropspeed.input",
                    Components =
                {
                    new CuiInputFieldComponent
                    {
                        CharsLimit = 32,
                        Align = TextAnchor.MiddleCenter,
                        Color = ColorPalette.LimeDark.rustString,
                        FontSize = 20,
                        Text = "",
                        IsPassword = false,
                        Command = "vagui_option 1 dropspeed "
                    },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                },
                    Parent = "vagui.cfg.dropspeed.panel"
                };

                //elements specific to that page
                CuiElement elementDropSpeedText = new CuiElement
                {
                    Name = "vagui.cfg.dropspeed.text",
                    Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 20,
                        Text = $"This will change",
                        Color = $"{ColorPalette.White.rustString}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"{leftT.ToString()} {(bottomB-(heightLine+paddingV2)*8).ToString()}",
                        AnchorMax = $"{col3left.ToString()} {(topB-(heightLine+paddingV2)*8).ToString()}"
                    }
                },
                    Parent = "Overlay"
                };

                CuiButton elementParachuteDamageDetachesButton = new CuiButton
                {
                    Button =
                {
                    Color = $"{ColorPalette.GreenLight.rustString}", //get the actual config value
                    Command = "vagui_option 1 parachutedamagedetaches {lenghty value}", //get the opposite of what the config value is
                },
                    Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 20,
                    Text = "This will be changed, don't worry", //get the actual config value
                    Color = $"{ColorPalette.White.rustString}"
                },
                    RectTransform = { AnchorMin = $"{leftT.ToString()} {(bottomB - (heightLine + paddingV2) * 7).ToString()}", AnchorMax = $"{section4right.ToString()} {(topB - (heightLine + paddingV2) * 7).ToString()}" },
                };

                CuiButton elementParachuteDecayButton = new CuiButton
                {
                    Button =
                {
                    Color = $"{ColorPalette.GreenLight.rustString}", //get the actual config value
                    Command = "vagui_option 1 parachutedecay {lenghty value}", //get the opposite of what the config value is
                },
                    Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 20,
                    Text = "This will be changed, don't worry", //get the actual config value
                    Color = $"{ColorPalette.White.rustString}"
                },
                    RectTransform = { AnchorMin = $"{section5left.ToString()} {(bottomB - (heightLine + paddingV2) * 7).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - (heightLine + paddingV2) * 7).ToString()}" },
                };

                CuiButton elementParachuteUseButton = new CuiButton
                {
                    Button =
                {
                    Color = $"{ColorPalette.GreenLight.rustString}", //get the actual config value
                    Command = "vagui_option 1 parachuteuse {lenghty value}", //get the opposite of what the config value is
                },
                    Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 20,
                    Text = "This will be changed, don't worry", //get the actual config value
                    Color = $"{ColorPalette.White.rustString}"
                },
                    RectTransform = { AnchorMin = $"{leftT.ToString()} {(bottomB - (heightLine + paddingV2) * 6).ToString()}", AnchorMax = $"{section4right.ToString()} {(topB - (heightLine + paddingV2) * 6).ToString()}" },
                };

                CuiButton elementParachuteIndestructibleButton = new CuiButton
                {
                    Button =
                {
                    Color = $"{ColorPalette.GreenLight.rustString}", //get the actual config value
                    Command = "vagui_option 1 parachuteindestructible {lenghty value}", //get the opposite of what the config value is
                },
                    Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 20,
                    Text = "This will be changed, don't worry", //get the actual config value
                    Color = $"{ColorPalette.White.rustString}"
                },
                    RectTransform = { AnchorMin = $"{section5left.ToString()} {(bottomB - (heightLine + paddingV2) * 6).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - (heightLine + paddingV2) * 6).ToString()}" },
                };

                CuiPanel elementPrivateTimePanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.White.rustString,
                    },
                    RectTransform = { AnchorMin = $"{col3left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 5).ToString()}", AnchorMax = $"{rightP.ToString()} {(-0 + topB - (heightLine + paddingV2) * 5).ToString()}" },
                };

                CuiElement elementPrivateTimeInput = new CuiElement
                {
                    Name = "vagui.cfg.privatetime.input",
                    Components =
                {
                    new CuiInputFieldComponent
                    {
                        CharsLimit = 32,
                        Align = TextAnchor.MiddleCenter,
                        Color = ColorPalette.LimeDark.rustString,
                        FontSize = 20,
                        Text = "",
                        IsPassword = false,
                        Command = "vagui_option 1 privatetime "
                    },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                },
                    Parent = "vagui.cfg.privatetime.panel"
                };

                //elements specific to that page
                CuiElement elementPrivateTimeText = new CuiElement
                {
                    Name = "vagui.cfg.privatetime.text",
                    Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 20,
                        Text = $"This will change",
                        Color = $"{ColorPalette.White.rustString}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"{leftT.ToString()} {(bottomB-(heightLine+paddingV2)*5).ToString()}",
                        AnchorMax = $"{col3left.ToString()} {(topB-(heightLine+paddingV2)*5).ToString()}"
                    }
                },
                    Parent = "Overlay"
                };


                CuiButton elementPrivateEnableButton = new CuiButton
                {
                    Button =
                {
                    Color = $"{ColorPalette.GreenLight.rustString}", //get the actual config value
                    Command = "vagui_option 1 privateenable {lenghty value}", //get the opposite of what the config value is
                },
                    Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 20,
                    Text = "This will be changed, don't worry", //get the actual config value
                    Color = $"{ColorPalette.White.rustString}"
                },
                    RectTransform = { AnchorMin = $"{leftT.ToString()} {(bottomB - (heightLine + paddingV2) * 4).ToString()}", AnchorMax = $"{section4right.ToString()} {(topB - (heightLine + paddingV2) * 4).ToString()}" },
                };

                CuiButton elementPrivateTeamButton = new CuiButton
                {
                    Button =
                {
                    Color = $"{ColorPalette.GreenLight.rustString}", //get the actual config value
                    Command = "vagui_option 1 privateteam {lenghty value}", //get the opposite of what the config value is
                },
                    Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 20,
                    Text = "This will be changed, don't worry", //get the actual config value
                    Color = $"{ColorPalette.White.rustString}"
                },
                    RectTransform = { AnchorMin = $"{section5left.ToString()} {(bottomB - (heightLine + paddingV2) * 4).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - (heightLine + paddingV2) * 4).ToString()}" },
                };


                CuiElement elementLootChanceText = new CuiElement
                {
                    Name = "vagui.cfg.lootchance.text",
                    Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 20,
                        Text = $"Chance of Supply Signal in Crates to be custom: X%",
                        Color = $"{ColorPalette.White.rustString}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"{leftT.ToString()} {(bottomB-(heightLine+paddingV2)*3).ToString()}",
                        AnchorMax = $"{col3left.ToString()} {(topB-(heightLine+paddingV2)*3).ToString()}"
                    }
                },
                    Parent = "Overlay"
                };

                CuiPanel elementLootChancePanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.White.rustString,
                    },
                    RectTransform = { AnchorMin = $"{col3left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 3).ToString()}", AnchorMax = $"{rightP.ToString()} {(-0 + topB - (heightLine + paddingV2) * 3).ToString()}" },
                };

                CuiElement elementLootChanceInput = new CuiElement
                {
                    Name = "vagui.cfg.lootchance.input",
                    Components =
                {
                    new CuiInputFieldComponent
                    {
                        CharsLimit = 32,
                        Align = TextAnchor.MiddleCenter,
                        Color = ColorPalette.LimeDark.rustString,
                        FontSize = 20,
                        Text = "",
                        IsPassword = false,
                        Command = "vagui_option 1 lootchance "
                    },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                },
                    Parent = "vagui.cfg.lootchance.panel"
                };

                //elements specific to that page
                CuiElement elementEnableLootText = new CuiElement
                {
                    Name = "vagui.cfg.lootenable.text",
                    Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 20,
                        Text = $"Enable custom Supply Signals spawning in crates",
                        Color = $"{ColorPalette.White.rustString}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"{leftT.ToString()} {(bottomB-(heightLine+paddingV2)*2).ToString()}",
                        AnchorMax = $"{col3left.ToString()} {(topB-(heightLine+paddingV2)*2).ToString()}"
                    }
                },
                    Parent = "Overlay"
                };

                CuiButton elementEnableLootButton = new CuiButton
                {
                    Button =
                {
                    Color = $"{ColorPalette.GreenLight.rustString}", //get the actual config value
                    Command = "vagui_option 1 lootenable {true/false}", //get the opposite of what the config value is
                },
                    Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 20,
                    Text = "True", //get the actual config value
                    Color = $"{ColorPalette.White.rustString}"
                },
                    RectTransform = { AnchorMin = $"{col3left.ToString()} {(bottomB - (heightLine + paddingV2) * 2).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - (heightLine + paddingV2) * 2).ToString()}" },
                };

                CuiElement elementDropChanceText = new CuiElement
                {
                    Name = "vagui.cfg.dropchance.text",
                    Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 20,
                        Text = $"Cargo Plane event chance of custom drop: X%",
                        Color = $"{ColorPalette.White.rustString}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"{leftT.ToString()} {(bottomB-(heightLine+paddingV2)*1).ToString()}",
                        AnchorMax = $"{col3left.ToString()} {(topB-(heightLine+paddingV2)*1).ToString()}"
                    }
                },
                    Parent = "Overlay"
                };

                CuiPanel elementDropChancePanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.White.rustString,
                    },
                    RectTransform = { AnchorMin = $"{col3left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 1).ToString()}", AnchorMax = $"{rightP.ToString()} {(-0 + topB - (heightLine + paddingV2) * 1).ToString()}" },
                };

                CuiElement elementDropChanceInput = new CuiElement
                {
                    Name = "vagui.cfg.dropchance.input",
                    Components =
                {
                    new CuiInputFieldComponent
                    {
                        CharsLimit = 32,
                        Align = TextAnchor.MiddleCenter,
                        Color = ColorPalette.LimeDark.rustString,
                        FontSize = 20,
                        Text = "",
                        IsPassword = false,
                        Command = "vagui_option 1 dropchance "
                    },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                },
                    Parent = "vagui.cfg.dropchance.panel"
                };

                //elements specific to that page
                CuiElement elementEnableDropText = new CuiElement
                {
                    Name = "vagui.cfg.dropenable.text",
                    Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 20,
                        Text = $"Enable custom drops for Cargo Plane events",
                        Color = $"{ColorPalette.White.rustString}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"{leftT.ToString()} {(bottomB-(heightLine+paddingV2)*0).ToString()}",
                        AnchorMax = $"{col3left.ToString()} {(topB-(heightLine+paddingV2)*0).ToString()}"
                    }
                },
                    Parent = "Overlay"
                };

                CuiButton elementEnableDropButton = new CuiButton
                {
                    Button =
                {
                    Color = $"{ColorPalette.GreenLight.rustString}", //get the actual config value
                    Command = "vagui_option 1 dropenable {true/false}", //get the opposite of what the config value is
                },
                    Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 20,
                    Text = "True", //get the actual config value
                    Color = $"{ColorPalette.White.rustString}"
                },
                    RectTransform = { AnchorMin = $"{col3left.ToString()} {(bottomB - (heightLine + paddingV2) * 0).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - (heightLine + paddingV2) * 0).ToString()}" },
                };

                public GuiPageAllDrops(GuiManager manager, string title) : base(manager, title)
                {
                    this.manager = manager;
                    this.title = title;
                    DefineOptions();
                }
                public override void DefineOptions()
                {
                    base.DefineOptions();
                    //add all the elements to the container here

                    pageContainer.Add(elementEnableDropText);
                    pageContainer.Add(elementEnableDropButton, "Overlay", "vagui.cfg.dropenable.button");

                    pageContainer.Add(elementDropChanceText);
                    pageContainer.Add(elementDropChancePanel, "Overlay", "vagui.cfg.dropchance.panel");
                    pageContainer.Add(elementDropChanceInput);


                    pageContainer.Add(elementEnableLootText);
                    pageContainer.Add(elementEnableLootButton, "Overlay", "vagui.cfg.lootenable.button");

                    pageContainer.Add(elementLootChanceText);
                    pageContainer.Add(elementLootChancePanel, "Overlay", "vagui.cfg.lootchance.panel");
                    pageContainer.Add(elementLootChanceInput);

                    pageContainer.Add(elementPrivateEnableButton, "Overlay", "vagui.cfg.privateenable.button");
                    pageContainer.Add(elementPrivateTeamButton, "Overlay", "vagui.cfg.privateteam.button");

                    pageContainer.Add(elementPrivateTimeText);
                    pageContainer.Add(elementPrivateTimePanel, "Overlay", "vagui.cfg.privatetime.panel");
                    pageContainer.Add(elementPrivateTimeInput);

                    pageContainer.Add(elementParachuteUseButton, "Overlay", "vagui.cfg.parachuteuse.button");
                    pageContainer.Add(elementParachuteIndestructibleButton, "Overlay", "vagui.cfg.parachuteindestructible.button");

                    pageContainer.Add(elementParachuteDamageDetachesButton, "Overlay", "vagui.cfg.parachutedamagedetaches.button");
                    pageContainer.Add(elementParachuteDecayButton, "Overlay", "vagui.cfg.parachutedecay.button");

                    pageContainer.Add(elementDropSpeedText);
                    pageContainer.Add(elementDropSpeedPanel, "Overlay", "vagui.cfg.dropspeed.panel");
                    pageContainer.Add(elementDropSpeedInput);

                    pageContainer.Add(elementDropInaccuracyText);
                    pageContainer.Add(elementDropInaccuracyPanel, "Overlay", "vagui.cfg.dropinaccuracy.panel");
                    pageContainer.Add(elementDropInaccuracyInput);
                }

                public override void GuiPageShow(BasePlayer player)
                {
                    GuiPageHide(player);
                    //update the buttons etc with right values

                    //iterate through all the page elements
                    foreach (var entry in pageContainer)
                    {
                        //input - maybe not needed?!

                        if (entry.Name.Contains(".text"))
                        {
                            var text = entry.Components[0] as CuiTextComponent;
                            switch (entry.Name)
                            {
                                case "vagui.cfg.dropchance.text":
                                    {
                                        text.Text = $"Cargo Plane event chance of custom drop: <color={ColorPalette.LimeLight.hexValue}><i>{(Instance.configData.customDropChance * 100F).ToString("0.0")}%</i></color>";
                                    }
                                    break;
                                case "vagui.cfg.lootchance.text":
                                    {
                                        text.Text = $"Crate chance of Supply Signal to be custom: <color={ColorPalette.LimeLight.hexValue}><i>{(Instance.configData.customLootChance * 100F).ToString("0.0")}%</i></color>";
                                    }
                                    break;
                                case "vagui.cfg.privatetime.text":
                                    {
                                        text.Text = $"Private drop protection wears off after: <color={ColorPalette.LimeLight.hexValue}><i>{(Instance.configData.unlockPrivateDropAfter).ToString("0.0")} seconds</i></color>";
                                    }
                                    break;
                                case "vagui.cfg.dropspeed.text":
                                    {
                                        text.Text = $"Falling speed (1.0 = normal): <color={ColorPalette.LimeLight.hexValue}><i>{(Instance.configData.fallSpeedWithParachute).ToString("0.0")}</i></color>";
                                    }
                                    break;
                                case "vagui.cfg.dropinaccuracy.text":
                                    {
                                        text.Text = $"Position inaccuracy (20.0 = normal): <color={ColorPalette.LimeLight.hexValue}><i>{(Instance.configData.inaccuracy).ToString("0.0")} meters</i></color>";
                                    }
                                    break;
                            }
                        }


                        //button and button text
                        if (entry.Name.Contains(".button"))
                        {
                            var button = entry.Components[0] as CuiButtonComponent;
                            switch (entry.Name)
                            {
                                case "vagui.cfg.dropenable.button":
                                    {
                                        var boolean = Instance.configData.enableRandomDrop;
                                        button.Color = Instance.GetBoolColor(boolean);
                                        button.Command = $"vagui_option 1 dropenable {(!boolean).ToString()}"; //reverse of what it is
                                    }
                                    break;
                                case "vagui.cfg.lootenable.button":
                                    {
                                        var boolean = Instance.configData.enableRandomLoot;
                                        button.Color = Instance.GetBoolColor(boolean);
                                        button.Command = $"vagui_option 1 lootenable {(!boolean).ToString()}"; //reverse of what it is
                                    }
                                    break;
                                case "vagui.cfg.privateenable.button":
                                    {
                                        var boolean = Instance.configData.enablePrivateSignals;
                                        button.Color = Instance.GetBoolColor(!boolean);
                                        button.Command = $"vagui_option 1 privateenable {(!boolean).ToString()}"; //reverse of what it is
                                    }
                                    break;
                                case "vagui.cfg.privateteam.button":
                                    {
                                        var boolean = Instance.configData.privateSignalsIncludeTeammates;
                                        button.Color = Instance.GetBoolColor(!boolean);
                                        button.Command = $"vagui_option 1 privateteam {(!boolean).ToString()}"; //reverse of what it is
                                    }
                                    break;
                                case "vagui.cfg.parachuteuse.button":
                                    {
                                        var boolean = Instance.configData.usableWithParachute;
                                        button.Color = Instance.GetBoolColor(boolean);
                                        button.Command = $"vagui_option 1 parachuteuse {(!boolean).ToString()}"; //reverse of what it is
                                    }
                                    break;
                                case "vagui.cfg.parachuteindestructible.button":
                                    {
                                        var boolean = Instance.configData.indestructibleWithParachute;
                                        button.Color = Instance.GetBoolColor(boolean);
                                        button.Command = $"vagui_option 1 parachuteindestructible {(!boolean).ToString()}"; //reverse of what it is
                                    }
                                    break;
                                case "vagui.cfg.parachutedamagedetaches.button":
                                    {
                                        var boolean = Instance.configData.damageDetachesParachute;
                                        button.Color = Instance.GetBoolColor(!boolean); //REVERSE COLOR
                                        button.Command = $"vagui_option 1 parachutedamagedetaches {(!boolean).ToString()}"; //reverse of what it is
                                    }
                                    break;
                                case "vagui.cfg.parachutedecay.button":
                                    {
                                        var boolean = Instance.configData.decaysWithParachute;
                                        button.Color = Instance.GetBoolColor(!boolean); //REVERSE COLOR
                                        button.Command = $"vagui_option 1 parachutedecay {(!boolean).ToString()}"; //reverse of what it is
                                    }
                                    break;
                            }
                        }
                        //text
                        if (entry.Parent.Contains(".button"))
                        {
                            var text = entry.Components[0] as CuiTextComponent;

                            switch (entry.Parent)
                            {
                                case "vagui.cfg.dropenable.button":
                                    {
                                        var boolean = Instance.configData.enableRandomDrop;
                                        text.Text = boolean.ToString();
                                    }
                                    break;
                                case "vagui.cfg.lootenable.button":
                                    {
                                        var boolean = Instance.configData.enableRandomLoot;
                                        text.Text = boolean.ToString();
                                    }
                                    break;
                                case "vagui.cfg.privateenable.button":
                                    {
                                        var boolean = Instance.configData.enablePrivateSignals;
                                        text.Text = boolean ? "Called drops are private" : "Anyone can access called drops";
                                    }
                                    break;
                                case "vagui.cfg.privateteam.button":
                                    {
                                        var boolean = Instance.configData.privateSignalsIncludeTeammates;
                                        text.Text = boolean ? "Private drops for caller only" : "Private drops include teammates";
                                    }
                                    break;
                                case "vagui.cfg.parachuteuse.button":
                                    {
                                        var boolean = Instance.configData.usableWithParachute;
                                        text.Text = boolean ? "Can access drop while dropping" : "Cannot access drops while dropping";
                                    }
                                    break;
                                case "vagui.cfg.parachuteindestructible.button":
                                    {
                                        var boolean = Instance.configData.indestructibleWithParachute;
                                        text.Text = !boolean ? "Can damage drop while dropping" : "Cannot damage drop while dropping";
                                    }
                                    break;
                                case "vagui.cfg.parachutedamagedetaches.button":
                                    {
                                        var boolean = Instance.configData.damageDetachesParachute;
                                        text.Text = boolean ? "Damage removes parachute" : "Damage doesn't remove parachute";
                                    }
                                    break;
                                case "vagui.cfg.parachutedecay.button":
                                    {
                                        var boolean = Instance.configData.decaysWithParachute;
                                        text.Text = boolean ? "Drops decay while dropping" : "Drops don't decay while dropping";
                                    }
                                    break;
                            }
                        }

                    }
                    //after everything's ready and updated, show the page

                    base.GuiPageShow(player);
                }

                public override void GuiPageHide(BasePlayer player)
                {
                    base.GuiPageHide(player);

                    CuiHelper.DestroyUi(player, "vagui.cfg.dropenable.button");
                    CuiHelper.DestroyUi(player, "vagui.cfg.dropenable.text");

                    CuiHelper.DestroyUi(player, "vagui.cfg.dropchance.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.dropchance.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.dropchance.text");

                    CuiHelper.DestroyUi(player, "vagui.cfg.lootenable.button");
                    CuiHelper.DestroyUi(player, "vagui.cfg.lootenable.text");

                    CuiHelper.DestroyUi(player, "vagui.cfg.lootchance.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.lootchance.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.lootchance.text");

                    CuiHelper.DestroyUi(player, "vagui.cfg.privateenable.button");
                    CuiHelper.DestroyUi(player, "vagui.cfg.privateteam.button");

                    CuiHelper.DestroyUi(player, "vagui.cfg.privatetime.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.privatetime.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.privatetime.text");

                    CuiHelper.DestroyUi(player, "vagui.cfg.parachuteuse.button");
                    CuiHelper.DestroyUi(player, "vagui.cfg.parachuteindestructible.button");
                    CuiHelper.DestroyUi(player, "vagui.cfg.parachutedamagedetaches.button");
                    CuiHelper.DestroyUi(player, "vagui.cfg.parachutedecay.button");

                    CuiHelper.DestroyUi(player, "vagui.cfg.dropspeed.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.dropspeed.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.dropspeed.text");

                    CuiHelper.DestroyUi(player, "vagui.cfg.dropinaccuracy.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.dropinaccuracy.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.dropinaccuracy.text");
                }

            }
            public class GuiPageShopConfig : GuiPage
            {
                CuiElement elementVendingRestockText = new CuiElement
                {
                    Name = "vagui.cfg.vendingrestock.text",
                    Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 20,
                        Text = $"Restock machine every: X minutes",
                        Color = $"{ColorPalette.White.rustString}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"{leftT.ToString()} {(bottomB-(heightLine+paddingV2)*2).ToString()}",
                        AnchorMax = $"{col3left.ToString()} {(topB-(heightLine+paddingV2)*2).ToString()}"
                    }
                },
                    Parent = "Overlay"
                };

                CuiPanel elementVendingRestockPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.White.rustString,
                    },
                    RectTransform = { AnchorMin = $"{col3left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 2).ToString()}", AnchorMax = $"{rightP.ToString()} {(-0 + topB - (heightLine + paddingV2) * 2).ToString()}" },
                };

                CuiElement elementVendingRestockInput = new CuiElement
                {
                    Name = "vagui.cfg.vendingrestock.input",
                    Components =
                {
                    new CuiInputFieldComponent
                    {
                        CharsLimit = 32,
                        Align = TextAnchor.MiddleCenter,
                        Color = ColorPalette.LimeDark.rustString,
                        FontSize = 20,
                        Text = "",
                        IsPassword = false,
                        Command = "vagui_option 0 vendingrestock "
                    },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                },
                    Parent = "vagui.cfg.vendingrestock.panel"
                };

                CuiElement elementVendingNameText = new CuiElement
                {
                    Name = "vagui.cfg.vendingname.text",
                    Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 20,
                        Text = $"Vending machine name: NAME",
                        Color = $"{ColorPalette.White.rustString}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"{leftT.ToString()} {(bottomB-(heightLine+paddingV2)*1).ToString()}",
                        AnchorMax = $"{col3left.ToString()} {(topB-(heightLine+paddingV2)*1).ToString()}"
                    }
                },
                    Parent = "Overlay"
                };

                CuiPanel elementVendingNamePanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.White.rustString,
                    },
                    RectTransform = { AnchorMin = $"{col3left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 1).ToString()}", AnchorMax = $"{rightP.ToString()} {(-0 + topB - (heightLine + paddingV2) * 1).ToString()}" },
                };

                CuiElement elementVendingNameInput = new CuiElement
                {
                    Name = "vagui.cfg.vendingname.input",
                    Components =
                {
                    new CuiInputFieldComponent
                    {
                        CharsLimit = 32,
                        Align = TextAnchor.MiddleCenter,
                        Color = ColorPalette.LimeDark.rustString,
                        FontSize = 20,
                        Text = "",
                        IsPassword = false,
                        Command = "vagui_option 0 vendingname "
                    },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                },
                    Parent = "vagui.cfg.vendingname.panel"
                };

                CuiElement elementPaymentVabuyText = new CuiElement
                {
                    Name = "vagui.cfg.vabuypayment.text",
                    Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 20,
                        Text = $"Command shop payment method",
                        Color = $"{ColorPalette.White.rustString}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"{leftT.ToString()} {(bottomB-(heightLine+paddingV2)*7).ToString()}",
                        AnchorMax = $"{col3left.ToString()} {(topB-(heightLine+paddingV2)*7).ToString()}"
                    }
                },
                    Parent = "Overlay"
                };

                CuiButton elementPaymentVabuyButton = new CuiButton
                {
                    Button =
                {
                    Color = $"{ColorPalette.GreenLight.rustString}", //get the actual config value
                    Command = "vagui_option 0 vabuypayment {Currency/ServerRewards/Economics}", //get the opposite of what the config value is
                },
                    Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 20,
                    Text = "True", //get the actual config value
                    Color = $"{ColorPalette.White.rustString}"
                },
                    RectTransform = { AnchorMin = $"{col3left.ToString()} {(bottomB - (heightLine + paddingV2) * 7).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - (heightLine + paddingV2) * 7).ToString()}" },
                };

                //elements specific to that page
                CuiElement elementEnableVabuyText = new CuiElement
                {
                    Name = "vagui.cfg.shopenable.text",
                    Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 20,
                        Text = $"Enable a <color={ColorPalette.AquaLight.hexValue}>/vabuy</color> chat command shop for players",
                        Color = $"{ColorPalette.White.rustString}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"{leftT.ToString()} {(bottomB-(heightLine+paddingV2)*6).ToString()}",
                        AnchorMax = $"{col3left.ToString()} {(topB-(heightLine+paddingV2)*6).ToString()}"
                    }
                },
                    Parent = "Overlay"
                };

                CuiButton elementEnableVabuyButton = new CuiButton
                {
                    Button =
                {
                    Color = $"{ColorPalette.GreenLight.rustString}", //get the actual config value
                    Command = "vagui_option 0 enablevabuy {true/false}", //get the opposite of what the config value is
                },
                    Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 20,
                    Text = "True", //get the actual config value
                    Color = $"{ColorPalette.White.rustString}"
                },
                    RectTransform = { AnchorMin = $"{col3left.ToString()} {(bottomB - (heightLine + paddingV2) * 6).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - (heightLine + paddingV2) * 6).ToString()}" },
                };

                CuiElement elementVendingRelativeText = new CuiElement
                {
                    Name = "vagui.cfg.vendingrelative.text",
                    Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 20,
                        Text = $"Vending machine position and rotation are",
                        Color = $"{ColorPalette.White.rustString}"
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"{leftT.ToString()} {(bottomB-(heightLine+paddingV2)*3).ToString()}",
                        AnchorMax = $"{col3left.ToString()} {(topB-(heightLine+paddingV2)*3).ToString()}"
                    }

                    },
                    Parent = "Overlay"
                };

                CuiButton elementVendingRelativeButton = new CuiButton
                {
                    Button =
                {
                    Color = $"{ColorPalette.GreenLight.rustString}", //get the actual config value
                    Command = "vagui_option 0 vendingrelative {true/false}", //get the opposite of what the config value is
                },
                    Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 20,
                    Text = "True", //get the actual config value
                    Color = $"{ColorPalette.White.rustString}"
                },
                    RectTransform = { AnchorMin = $"{col3left.ToString()} {(bottomB - (heightLine + paddingV2) * 3).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - (heightLine + paddingV2) * 3).ToString()}" },

                };

                CuiElement elementVendingPosText = new CuiElement
                {
                    Name = "vagui.cfg.vendingpos.text",
                    Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 20,
                        Text = $"Vending machine position <color={ColorPalette.RedLight.hexValue}>X</color>, <color={ColorPalette.GreenLight.hexValue}>Y</color>, <color={ColorPalette.BlueLight.hexValue}>Z</color>: POS",
                        Color = $"{ColorPalette.White.rustString}"
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"{leftT.ToString()} {(bottomB-(heightLine+paddingV2)*4).ToString()}",
                        AnchorMax = $"{col3left.ToString()} {(topB-(heightLine+paddingV2)*4).ToString()}"
                    }

                    },
                    Parent = "Overlay"
                };

                CuiPanel elementVendingPosXPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.RedDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section1left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 4).ToString()}", AnchorMax = $"{section2left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 4).ToString()}" },
                };

                CuiElement elementVendingPosXInput = new CuiElement
                {
                    Name = "vagui.cfg.vendingposx.input",
                    Components =
                {
                    new CuiInputFieldComponent
                    {
                        CharsLimit = 32,
                        Align = TextAnchor.MiddleCenter,
                        Color = ColorPalette.White.rustString,
                        FontSize = 20,
                        Text = "",
                        IsPassword = false,
                        Command = "vagui_option 0 vendingposx "
                    },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                },
                    Parent = "vagui.cfg.vendingposx.panel"
                };
                CuiPanel elementVendingPosYPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.GreenDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section2left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 4).ToString()}", AnchorMax = $"{section3left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 4).ToString()}" },
                };

                CuiElement elementVendingPosYInput = new CuiElement
                {
                    Name = "vagui.cfg.vendingposy.input",
                    Components =
                {
                    new CuiInputFieldComponent
                    {
                        CharsLimit = 32,
                        Align = TextAnchor.MiddleCenter,
                        Color = ColorPalette.White.rustString,
                        FontSize = 20,
                        Text = "",
                        IsPassword = false,
                        Command = "vagui_option 0 vendingposy "
                    },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                },
                    Parent = "vagui.cfg.vendingposy.panel"
                };
                CuiPanel elementVendingPosZPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.BlueDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section3left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 4).ToString()}", AnchorMax = $"{rightP.ToString()} {(-0 + topB - (heightLine + paddingV2) * 4).ToString()}" },
                };

                CuiElement elementVendingPosZInput = new CuiElement
                {
                    Name = "vagui.cfg.vendingposz.input",
                    Components =
                {
                    new CuiInputFieldComponent
                    {
                        CharsLimit = 32,
                        Align = TextAnchor.MiddleCenter,
                        Color = ColorPalette.White.rustString,
                        FontSize = 20,
                        Text = "",
                        IsPassword = false,
                        Command = "vagui_option 0 vendingposz "
                    },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                },
                    Parent = "vagui.cfg.vendingposz.panel"
                };


                CuiElement elementVendingRotText = new CuiElement
                {
                    Name = "vagui.cfg.vendingrot.text",
                    Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 20,
                        Text = $"Vending machine rotation <color={ColorPalette.RedLight.hexValue}>X</color>, <color={ColorPalette.GreenLight.hexValue}>Y</color>, <color={ColorPalette.BlueLight.hexValue}>Z</color>: POS",
                        Color = $"{ColorPalette.White.rustString}"
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"{leftT.ToString()} {(bottomB-(heightLine+paddingV2)*5).ToString()}",
                        AnchorMax = $"{col3left.ToString()} {(topB-(heightLine+paddingV2)*5).ToString()}"
                    }

                    },
                    Parent = "Overlay"
                };

                CuiPanel elementVendingRotXPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.RedDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section1left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 5).ToString()}", AnchorMax = $"{section2left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 5).ToString()}" },
                };

                CuiElement elementVendingRotXInput = new CuiElement
                {
                    Name = "vagui.cfg.vendingrotx.input",
                    Components =
                {
                    new CuiInputFieldComponent
                    {
                        CharsLimit = 32,
                        Align = TextAnchor.MiddleCenter,
                        Color = ColorPalette.White.rustString,
                        FontSize = 20,
                        Text = "",
                        IsPassword = false,
                        Command = "vagui_option 0 vendingrotx "
                    },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                },
                    Parent = "vagui.cfg.vendingrotx.panel"
                };
                CuiPanel elementVendingRotYPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.GreenDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section2left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 5).ToString()}", AnchorMax = $"{section3left.ToString()} {(-0 + topB - (heightLine + paddingV2) * 5).ToString()}" },
                };

                CuiElement elementVendingRotYInput = new CuiElement
                {
                    Name = "vagui.cfg.vendingroty.input",
                    Components =
                {
                    new CuiInputFieldComponent
                    {
                        CharsLimit = 32,
                        Align = TextAnchor.MiddleCenter,
                        Color = ColorPalette.White.rustString,
                        FontSize = 20,
                        Text = "",
                        IsPassword = false,
                        Command = "vagui_option 0 vendingroty "
                    },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                },
                    Parent = "vagui.cfg.vendingroty.panel"
                };
                CuiPanel elementVendingRotZPanel = new CuiPanel
                {
                    CursorEnabled = true,
                    Image = new CuiImageComponent
                    {
                        Color = ColorPalette.BlueDark.rustString,
                    },
                    RectTransform = { AnchorMin = $"{section3left.ToString()} {(0 + bottomB - (heightLine + paddingV2) * 5).ToString()}", AnchorMax = $"{rightP.ToString()} {(-0 + topB - (heightLine + paddingV2) * 5).ToString()}" },
                };

                CuiElement elementVendingRotZInput = new CuiElement
                {
                    Name = "vagui.cfg.vendingrotz.input",
                    Components =
                {
                    new CuiInputFieldComponent
                    {
                        CharsLimit = 32,
                        Align = TextAnchor.MiddleCenter,
                        Color = ColorPalette.White.rustString,
                        FontSize = 20,
                        Text = "",
                        IsPassword = false,
                        Command = "vagui_option 0 vendingrotz "
                    },
                    //new CuiRectTransformComponent {AnchorMin = $"{middleT.ToString()} {(bottomB - heightButton *1).ToString()}", AnchorMax = $"{rightP.ToString()} {(topB - heightButton *1).ToString()}" },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"}
                },
                    Parent = "vagui.cfg.vendingrotz.panel"
                };



                CuiElement elementEnableVendingText = new CuiElement
                {
                    Name = "vagui.cfg.enablevending.text",
                    Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 20,
                        Text = $"Auto-add a vending machine that sells supply signals",
                        Color = $"{ColorPalette.White.rustString}"
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"{leftT.ToString()} {bottomB.ToString()}",
                        AnchorMax = $"{col3left.ToString()} {topB.ToString()}"
                    }

                    },
                    Parent = "Overlay"
                };

                CuiButton elementEnableVendingButton = new CuiButton
                {
                    Button =
                {
                    Color = $"{ColorPalette.GreenLight.rustString}", //get the actual config value
                    Command = "vagui_option 0 enablevending {true/false}", //get the opposite of what the config value is
                },
                    Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 20,
                    Text = "True", //get the actual config value
                    Color = $"{ColorPalette.White.rustString}"
                },
                    RectTransform = { AnchorMin = $"{col3left.ToString()} {bottomB.ToString()}", AnchorMax = $"{rightP.ToString()} {topB.ToString()}" },
                };

                public GuiPageShopConfig(GuiManager manager, string title) : base(manager, title)
                {
                    this.manager = manager;
                    this.title = title;
                    DefineOptions();
                }
                public override void DefineOptions()
                {
                    base.DefineOptions();
                    pageContainer.Add(elementEnableVabuyText);
                    pageContainer.Add(elementEnableVabuyButton, "Overlay", "vagui.cfg.shopenable.button");

                    pageContainer.Add(elementEnableVendingText);
                    pageContainer.Add(elementEnableVendingButton, "Overlay", "vagui.cfg.enablevending.button");

                    pageContainer.Add(elementVendingRelativeText);
                    pageContainer.Add(elementVendingRelativeButton, "Overlay", "vagui.cfg.vendingrelative.button");

                    pageContainer.Add(elementPaymentVabuyText);
                    pageContainer.Add(elementPaymentVabuyButton, "Overlay", "vagui.cfg.vabuypayment.button");

                    pageContainer.Add(elementVendingNameText);
                    pageContainer.Add(elementVendingNamePanel, "Overlay", "vagui.cfg.vendingname.panel");
                    pageContainer.Add(elementVendingNameInput);

                    pageContainer.Add(elementVendingRestockText);
                    pageContainer.Add(elementVendingRestockPanel, "Overlay", "vagui.cfg.vendingrestock.panel");
                    pageContainer.Add(elementVendingRestockInput);

                    pageContainer.Add(elementVendingPosText);

                    pageContainer.Add(elementVendingPosXPanel, "Overlay", "vagui.cfg.vendingposx.panel");
                    pageContainer.Add(elementVendingPosXInput);

                    pageContainer.Add(elementVendingPosYPanel, "Overlay", "vagui.cfg.vendingposy.panel");
                    pageContainer.Add(elementVendingPosYInput);

                    pageContainer.Add(elementVendingPosZPanel, "Overlay", "vagui.cfg.vendingposz.panel");
                    pageContainer.Add(elementVendingPosZInput);

                    pageContainer.Add(elementVendingRotText);

                    pageContainer.Add(elementVendingRotXPanel, "Overlay", "vagui.cfg.vendingrotx.panel");
                    pageContainer.Add(elementVendingRotXInput);

                    pageContainer.Add(elementVendingRotYPanel, "Overlay", "vagui.cfg.vendingroty.panel");
                    pageContainer.Add(elementVendingRotYInput);

                    pageContainer.Add(elementVendingRotZPanel, "Overlay", "vagui.cfg.vendingrotz.panel");
                    pageContainer.Add(elementVendingRotZInput);


                }

                public override void GuiPageShow(BasePlayer player)
                {
                    GuiPageHide(player);
                    //update the buttons etc with right values

                    //iterate through all the page elements
                    foreach (var entry in pageContainer)
                    {
                        //input - maybe not needed?!

                        if (entry.Name.Contains(".text"))
                        {
                            var text = entry.Components[0] as CuiTextComponent;
                            switch (entry.Name)
                            {
                                case "vagui.cfg.vendingname.text":
                                    {
                                        text.Text = $"Vending machine name: <color={ColorPalette.LimeLight.hexValue}><i>{Instance.configData.vendingMachineCustomName}</i></color>";
                                    }
                                    break;
                                case "vagui.cfg.vendingrestock.text":
                                    {
                                        text.Text = $"Restock machine every: <color={ColorPalette.LimeLight.hexValue}><i>{Instance.configData.vendingMachineRestockEvery}</i></color> minutes";
                                    }
                                    break;
                                case "vagui.cfg.vendingpos.text":
                                    {
                                        text.Text = $"Vending machine position: (<color={ColorPalette.RedLight.hexValue}>{Instance.configData.vendingMachinePosX.ToString("0.00")}</color>, <color={ColorPalette.GreenLight.hexValue}>{Instance.configData.vendingMachinePosY.ToString("0.00")}</color>, <color={ColorPalette.BlueLight.hexValue}>{Instance.configData.vendingMachinePosZ.ToString("0.00")}</color>)";
                                    }
                                    break;
                                case "vagui.cfg.vendingrot.text":
                                    {
                                        text.Text = $"Vending machine rotation: (<color={ColorPalette.RedLight.hexValue}>{Instance.configData.vendingMachineRotX.ToString("0.00")}</color>, <color={ColorPalette.GreenLight.hexValue}>{Instance.configData.vendingMachineRotY.ToString("0.00")}</color>, <color={ColorPalette.BlueLight.hexValue}>{Instance.configData.vendingMachineRotZ.ToString("0.00")}</color>)";
                                    }
                                    break;
                            }
                        }


                        //button and button text
                        if (entry.Name.Contains(".button"))
                        {
                            var button = entry.Components[0] as CuiButtonComponent;
                            switch (entry.Name)
                            {
                                case "vagui.cfg.shopenable.button":
                                    {
                                        var boolean = Instance.configData.enableCommandShop;
                                        button.Color = Instance.GetBoolColor(boolean);
                                        button.Command = $"vagui_option 0 enablevabuy {(!boolean).ToString()}"; //reverse of what it is
                                    }
                                    break;
                                case "vagui.cfg.enablevending.button":
                                    {
                                        var boolean = Instance.configData.enableVendingMachine;
                                        button.Color = Instance.GetBoolColor(boolean);
                                        button.Command = $"vagui_option 0 enablevending {(!boolean).ToString()}"; //reverse of what it is
                                    }
                                    break;
                                case "vagui.cfg.vendingrelative.button":
                                    {
                                        var boolean = Instance.configData.vendingMachineLocationIsRelative;
                                        button.Color = Instance.GetBoolColor(boolean);
                                        button.Command = $"vagui_option 0 vendingrelative {(!boolean).ToString()}"; //reverse of what it is
                                    }
                                    break;
                                case "vagui.cfg.vabuypayment.button":
                                    {
                                        //command should point to the next one! what's the current?
                                        string next;
                                        string color;

                                        switch (Instance.configData.defaultPayment)
                                        {
                                            default:
                                            case "Currency": { next = "ServerRewards"; color = ColorPalette.YellowDark.rustString; } break;
                                            case "ServerRewards": { next = "Economics"; color = ColorPalette.RedDark.rustString; } break;
                                            case "Economics": { next = "Currency"; color = ColorPalette.BlueDark.rustString; } break;
                                        }

                                        button.Color = color;
                                        button.Command = $"vagui_option 0 vabuypayment {next}";
                                    }
                                    break;
                            }
                        }
                        //text
                        if (entry.Parent.Contains(".button"))
                        {
                            var text = entry.Components[0] as CuiTextComponent;

                            switch (entry.Parent)
                            {
                                case "vagui.cfg.shopenable.button":
                                    {
                                        var boolean = Instance.configData.enableCommandShop;
                                        text.Text = boolean.ToString();
                                    }
                                    break;
                                case "vagui.cfg.enablevending.button":
                                    {
                                        var boolean = Instance.configData.enableVendingMachine;
                                        text.Text = boolean.ToString();
                                    }
                                    break;
                                case "vagui.cfg.vendingrelative.button":
                                    {
                                        var boolean = Instance.configData.vendingMachineLocationIsRelative;
                                        text.Text = boolean ? "Relative to Outpost" : "Absolute (0,0,0)";
                                    }
                                    break;
                                case "vagui.cfg.vabuypayment.button":
                                    {
                                        text.Text = Instance.configData.defaultPayment;
                                    }
                                    break;
                            }
                        }

                    }
                    //after everything's ready and updated, show the page

                    base.GuiPageShow(player);


                }
                public override void GuiPageHide(BasePlayer player)
                {
                    base.GuiPageHide(player);
                    CuiHelper.DestroyUi(player, "vagui.cfg.shopenable.text");
                    CuiHelper.DestroyUi(player, "vagui.cfg.shopenable.button");
                    CuiHelper.DestroyUi(player, "vagui.cfg.enablevending.text");
                    CuiHelper.DestroyUi(player, "vagui.cfg.enablevending.button");
                    CuiHelper.DestroyUi(player, "vagui.cfg.vendingrelative.text");
                    CuiHelper.DestroyUi(player, "vagui.cfg.vendingrelative.button");
                    CuiHelper.DestroyUi(player, "vagui.cfg.vabuypayment.text");
                    CuiHelper.DestroyUi(player, "vagui.cfg.vabuypayment.button");
                    CuiHelper.DestroyUi(player, "vagui.cfg.vendingname.text");
                    CuiHelper.DestroyUi(player, "vagui.cfg.vendingname.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.vendingname.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.vendingrestock.text");
                    CuiHelper.DestroyUi(player, "vagui.cfg.vendingrestock.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.vendingrestock.input");

                    CuiHelper.DestroyUi(player, "vagui.cfg.vendingpos.text");
                    CuiHelper.DestroyUi(player, "vagui.cfg.vendingposx.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.vendingposx.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.vendingposy.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.vendingposy.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.vendingposz.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.vendingposz.panel");

                    CuiHelper.DestroyUi(player, "vagui.cfg.vendingrot.text");
                    CuiHelper.DestroyUi(player, "vagui.cfg.vendingrotx.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.vendingrotx.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.vendingroty.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.vendingroty.panel");
                    CuiHelper.DestroyUi(player, "vagui.cfg.vendingrotz.input");
                    CuiHelper.DestroyUi(player, "vagui.cfg.vendingrotz.panel");
                }
            }

            public void SetTitle(string title)
            {
                var titleComponent = containerTitle[0].Components[0] as CuiTextComponent;
                titleComponent.Text = title;
            }

            public static readonly float left = 0.15F;
            public static readonly float bottom = 0.13F;
            public static readonly float right = 0.85F;
            public static readonly float top = 0.87F;

            public static readonly float paddingV = 0.0148F; //vertical padding
            public static readonly float paddingH = 0.00833F; //horizontal padding

            public static readonly float paddingV2 = paddingV;

            public static readonly float leftP = left + paddingH; //padded left
            public static readonly float bottomP = bottom + paddingV; //padded bottom
            public static readonly float rightP = right - paddingH; //padded right
            public static readonly float topP = top - paddingV; //padded top

            public static readonly float heightLine = 0.0474F;

            public static readonly float heightMenuButton = 0.0374F;

            public static readonly float bottomH = 0.7829F; //bottom of the header: logo, title, X button

            public static readonly float topB = 0.74629F; //top of the first button AND the page content
            public static readonly float bottomB = topB - heightLine; //bottom of the first button

            public static readonly float rightL = 0.3177F; //right of the logo

            public static readonly float leftT = 0.3265F; //left of the title (and the page too!)
            public static readonly float rightT = 0.7906F; //right of the title

            public static readonly float widthTitle = (rightP - leftT);

            public static readonly float col3width = widthTitle / 3; //where the text ends and the buttons/input fields begin
            public static readonly float col3left = leftT + col3width * 2;
            public static readonly float widthRight = rightP - col3left;

            public static readonly float section1left = col3left;
            public static readonly float section2left = col3left + (0.33333F) * widthRight;
            //public static readonly float section2left = middleT + 0.1F;
            public static readonly float section3left = col3left + (0.66666F) * widthRight;

            public static readonly float section1Bleft = col3left;
            public static readonly float section2Bleft = col3left + (0.5F) * widthRight;

            //section 4 and 5 are only buttons in the line
            //middleS divides up the title/page content width in 2
            public static readonly float middleS = leftT + widthTitle / 2;
            //section4left is the same as leftT
            public static readonly float section4right = middleS - paddingH / 2;
            public static readonly float section5left = middleS + paddingH / 2;

            public static readonly float section6left = col3left;
            public static readonly float section7left = col3left + (col3width / 4F);
            public static readonly float section8left = col3left + 2 * (col3width / 4F);
            public static readonly float section9left = col3left + 3 * (col3width / 4F);
            //section5 right is the same as rightP

            public static readonly float leftX = 0.8F; //left of the title (and the page too!)
            public static readonly float rightX = 0.8411F; //right of the X button

            //backdrop, logo, X button
            public CuiElementContainer containerMain = new CuiElementContainer();

            //title
            public CuiElementContainer containerTitle = new CuiElementContainer();

            //navigation buttons
            public CuiElementContainer containerNav = new CuiElementContainer();

            public CuiElement elementMainBackdrop = new CuiElement
            {
                Name = "vagui.backdrop",
                Components =
                    {
                    new CuiImageComponent {Color = $"{ColorPalette.Black.rustString} 0.97", FadeIn = 0, Material = "assets/content/ui/uibackgroundblur.mat" },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"{left.ToString()} {bottom.ToString()}",
                        AnchorMax = $"{right.ToString()} {top.ToString()}",
                    },
                    new CuiNeedsCursorComponent()
                },
                FadeOut = 0,
                Parent = "Overlay"
            };
            /*
            public CuiElement elementLogo = new CuiElement
            {
                Name = "vagui.logo",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = Instance.GetImage(GetNameFromURL(ParseURL("https://i.imgur.com/lcrMqDl.png"))),
                        Color = $"{ColorPalette.White.rustString}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"{leftP.ToString()} {bottomH.ToString()}",
                        AnchorMax = $"{rightL.ToString()} {topP.ToString()}",
                    }
                },
                Parent = "Overlay"
            }; */

            public CuiElement elementTitle = new CuiElement
            {
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 40,
                        Text = "Dialog title",
                        Color = $"{ColorPalette.White.rustString}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"{leftT.ToString()} {bottomH.ToString()}",
                        AnchorMax = $"{rightT.ToString()} {topP.ToString()}",
                    }
                },
                Name = "vagui.title",
                Parent = "Overlay"
            };

            public CuiButton elementX = new CuiButton
            {
                Button =
                {
                    Color = $"{ColorPalette.RedLight.rustString}",
                    Command = "vagui_close",
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 40,
                    Text = "X",
                    Color = $"{ColorPalette.White.rustString}"
                },
                RectTransform = { AnchorMin = $"{leftX.ToString()} {bottomH.ToString()}", AnchorMax = $"{rightX.ToString()} {topP.ToString()}" },
            };

            public CuiButton elementButtonPage0 = new CuiButton
            {
                Button =
                {
                    Color = $"{ColorPalette.Grey.rustString}",
                    Command = "vagui_page 0",
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 25,
                    Text = "Shop Config",
                    Color = $"{ColorPalette.White.rustString}"
                },
                RectTransform = { AnchorMin = $"{leftP.ToString()} {bottomB.ToString()}", AnchorMax = $"{rightL.ToString()} {topB.ToString()}" },
            };

            public CuiButton elementButtonPage1 = new CuiButton
            {
                Button =
                {
                    Color = $"{ColorPalette.Grey.rustString}",
                    Command = "vagui_page 1",
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 25,
                    Text = "All Drops",
                    Color = $"{ColorPalette.White.rustString}"
                },
                RectTransform = { AnchorMin = $"{leftP.ToString()} {(bottomB - 1 * (paddingV + heightMenuButton)).ToString()}", AnchorMax = $"{rightL.ToString()} {(topB - 1 * (paddingV + heightMenuButton)).ToString()}" },
            };
            public CuiButton elementButtonPage2 = new CuiButton
            {
                Button =
                {
                    Color = $"{ColorPalette.PurpleDark.rustString}",
                    Command = "vagui_page 2",
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 25,
                    Text = "Normal Drop",
                    Color = $"{ColorPalette.White.rustString}"
                },
                RectTransform = { AnchorMin = $"{leftP.ToString()} {(bottomB - 2 * (paddingV + heightMenuButton)).ToString()}", AnchorMax = $"{rightL.ToString()} {(topB - 2 * (paddingV + heightMenuButton)).ToString()}" },
            };
            public CuiButton elementButtonPage3 = new CuiButton
            {
                Button =
                {
                    Color = $"{ColorPalette.RedDark.rustString}",
                    Command = "vagui_page 3",
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 25,
                    Text = "Minicopter",
                    Color = $"{ColorPalette.White.rustString}"
                },
                RectTransform = { AnchorMin = $"{leftP.ToString()} {(bottomB - 3 * (paddingV + heightMenuButton)).ToString()}", AnchorMax = $"{rightL.ToString()} {(topB - 3 * (paddingV + heightMenuButton)).ToString()}" },
            };
            public CuiButton elementButtonPage4 = new CuiButton
            {
                Button =
                {
                    Color = $"{ColorPalette.YellowDark.rustString}",
                    Command = "vagui_page 4",
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 25,
                    Text = "Scrap Heli",
                    Color = $"{ColorPalette.White.rustString}"
                },
                RectTransform = { AnchorMin = $"{leftP.ToString()} {(bottomB - 4 * (paddingV + heightMenuButton)).ToString()}", AnchorMax = $"{rightL.ToString()} {(topB - 4 * (paddingV + heightMenuButton)).ToString()}" },
            };
            public CuiButton elementButtonPage5 = new CuiButton
            {
                Button =
                {
                    Color = $"{ColorPalette.LimeDark.rustString}",
                    Command = "vagui_page 5",
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 25,
                    Text = "Locked Crate",
                    Color = $"{ColorPalette.White.rustString}"
                },
                RectTransform = { AnchorMin = $"{leftP.ToString()} {(bottomB - 5 * (paddingV + heightMenuButton)).ToString()}", AnchorMax = $"{rightL.ToString()} {(topB - 5 * (paddingV + heightMenuButton)).ToString()}" },
            };
            public CuiButton elementButtonPage6 = new CuiButton
            {
                Button =
                {
                    Color = $"{ColorPalette.GreenDark.rustString}",
                    Command = "vagui_page 6",
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 25,
                    Text = "Modular Car",
                    Color = $"{ColorPalette.White.rustString}"
                },
                RectTransform = { AnchorMin = $"{leftP.ToString()} {(bottomB - 6 * (paddingV + heightMenuButton)).ToString()}", AnchorMax = $"{rightL.ToString()} {(topB - 6 * (paddingV + heightMenuButton)).ToString()}" },
            };
            public CuiButton elementButtonPage7 = new CuiButton
            {
                Button =
                {
                    Color = $"{ColorPalette.AquaDark.rustString}",
                    Command = "vagui_page 7",
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 25,
                    Text = "Rowboat",
                    Color = $"{ColorPalette.White.rustString}"
                },
                RectTransform = { AnchorMin = $"{leftP.ToString()} {(bottomB - 7 * (paddingV + heightMenuButton)).ToString()}", AnchorMax = $"{rightL.ToString()} {(topB - 7 * (paddingV + heightMenuButton)).ToString()}" },
            };
            public CuiButton elementButtonPage8 = new CuiButton
            {
                Button =
                {
                    Color = $"{ColorPalette.BlueDark.rustString}",
                    Command = "vagui_page 8",
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 25,
                    Text = "RHIB",
                    Color = $"{ColorPalette.White.rustString}"
                },
                RectTransform = { AnchorMin = $"{leftP.ToString()} {(bottomB - 8 * (paddingV + heightMenuButton)).ToString()}", AnchorMax = $"{rightL.ToString()} {(topB - 8 * (paddingV + heightMenuButton)).ToString()}" },
            };

            public CuiButton elementButtonPage9 = new CuiButton
            {
                Button =
                {
                    Color = $"{ColorPalette.OrangeDark.rustString}",
                    Command = "vagui_page 9",
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 25,
                    Text = "Solo Sub",
                    Color = $"{ColorPalette.White.rustString}"
                },
                RectTransform = { AnchorMin = $"{leftP.ToString()} {(bottomB - 9 * (paddingV + heightMenuButton)).ToString()}", AnchorMax = $"{rightL.ToString()} {(topB - 9 * (paddingV + heightMenuButton)).ToString()}" },
            };

            public CuiButton elementButtonPage10 = new CuiButton
            {
                Button =
                {
                    Color = $"{ColorPalette.PissYellowDark.rustString}",
                    Command = "vagui_page 10",
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 25,
                    Text = "Duo Sub",
                    Color = $"{ColorPalette.White.rustString}"
                },
                RectTransform = { AnchorMin = $"{leftP.ToString()} {(bottomB - 10 * (paddingV + heightMenuButton)).ToString()}", AnchorMax = $"{rightL.ToString()} {(topB - 10 * (paddingV + heightMenuButton)).ToString()}" },
            };


            public GuiManager()
            {
                PrepareCommon();
            }

            public GuiPage pageShopConfig;
            public GuiPage pageAllDrops;
            public GuiPage pageNormal;
            public GuiPage pageMinicopter;
            public GuiPage pageScrapheli;
            public GuiPage pageCrate;
            public GuiPage pageCar;
            public GuiPage pageCarExtra;
            public GuiPage pageRowboat;
            public GuiPage pageRhib;

            public GuiPage pageSolosub;
            public GuiPage pageDuosub;

            public void PrepareCommon()
            {
                //add main container elements - these will always stay there
                containerMain.Add(elementMainBackdrop);
                //containerMain.Add(elementLogo);
                containerMain.Add(elementX, "Overlay", "vagui.x");

                containerNav.Add(elementButtonPage0, "Overlay", "vagui.button0");
                containerNav.Add(elementButtonPage1, "Overlay", "vagui.button1");
                containerNav.Add(elementButtonPage2, "Overlay", "vagui.button2");
                containerNav.Add(elementButtonPage3, "Overlay", "vagui.button3");
                containerNav.Add(elementButtonPage4, "Overlay", "vagui.button4");
                containerNav.Add(elementButtonPage5, "Overlay", "vagui.button5");
                containerNav.Add(elementButtonPage6, "Overlay", "vagui.button6");
                containerNav.Add(elementButtonPage7, "Overlay", "vagui.button7");
                containerNav.Add(elementButtonPage8, "Overlay", "vagui.button8");
                containerNav.Add(elementButtonPage9, "Overlay", "vagui.button9");
                containerNav.Add(elementButtonPage10, "Overlay", "vagui.button10");

                containerTitle.Add(elementTitle);

                //generate pages
                pageShopConfig = new GuiPageShopConfig(this, "Shop Config"); //0
                pageAllDrops = new GuiPageAllDrops(this, "All Drops"); //1
                pageNormal = new GuiPageParticularDrop(this, "Normal Drop", "normal", "2");
                pageMinicopter = new GuiPageParticularDrop(this, "Minicopter", "minicopter", "3", true);
                pageScrapheli = new GuiPageParticularDrop(this, "Scrap Heli", "scrapheli", "4", true);
                pageCrate = new GuiPageParticularDrop(this, "Locked Crate", "crate", "5");
                pageCar = new GuiPageParticularDrop(this, "Modular Car", "car", "6", true);
                pageCarExtra = new GuiPageCarExtra(this, "Modular Car: Engine Components", "6EXTRA");
                pageRowboat = new GuiPageParticularDrop(this, "Rowboat", "rowboat", "7", true);
                pageRhib = new GuiPageParticularDrop(this, "RHIB", "rhib", "8", true);

                pageSolosub = new GuiPageParticularDrop(this, "Solo Sub", "solosub", "9", true);
                pageDuosub = new GuiPageParticularDrop(this, "Duo Sub", "duosub", "10", true);

            }

            public void GuiClose(BasePlayer player)
            {
                //hide all containers
                PagesHide(player);
                MainHide(player);
                TitleHide(player);
                ButtonsHide(player);
            }

            public void GuiOpen(BasePlayer player)
            {
                //shows the main container,
                //nav container,
                //default page container
                GuiClose(player);

                //show main container...
                MainShow(player);
                TitleShow(player);
                ButtonsShow(player);
                PageShow(player, pageAllDrops);
            }

            public void MainShow(BasePlayer player)
            {
                CuiHelper.AddUi(player, containerMain);
            }

            public void MainHide(BasePlayer player)
            {
                CuiHelper.DestroyUi(player, "vagui.backdrop");
                CuiHelper.DestroyUi(player, "vagui.logo");
                CuiHelper.DestroyUi(player, "vagui.x");
            }

            public void TitleShow(BasePlayer player)
            {
                CuiHelper.AddUi(player, containerTitle);
            }

            public void TitleHide(BasePlayer player)
            {
                CuiHelper.DestroyUi(player, "vagui.title");
            }

            public void PageShow(BasePlayer player, GuiPage page)
            {
                //hide the page container
                PagesHide(player);
                //and show the new one
                page.GuiPageShow(player);
            }

            public void ButtonsShow(BasePlayer player)
            {
                CuiHelper.AddUi(player, containerNav);
            }

            public void ButtonsHide(BasePlayer player)
            {
                //hide buttons
                CuiHelper.DestroyUi(player, "vagui.button0");
                CuiHelper.DestroyUi(player, "vagui.button1");
                CuiHelper.DestroyUi(player, "vagui.button2");
                CuiHelper.DestroyUi(player, "vagui.button3");
                CuiHelper.DestroyUi(player, "vagui.button4");
                CuiHelper.DestroyUi(player, "vagui.button5");
                CuiHelper.DestroyUi(player, "vagui.button6");
                CuiHelper.DestroyUi(player, "vagui.button7");
                CuiHelper.DestroyUi(player, "vagui.button8");
                CuiHelper.DestroyUi(player, "vagui.button9");
                CuiHelper.DestroyUi(player, "vagui.button10");
            }

            public void PagesHide(BasePlayer player)
            {
                //hide all the possible page elements
                pageShopConfig.GuiPageHide(player);
                pageAllDrops.GuiPageHide(player);
                pageNormal.GuiPageHide(player);
                pageMinicopter.GuiPageHide(player);
                pageScrapheli.GuiPageHide(player);
                pageCrate.GuiPageHide(player);
                pageCar.GuiPageHide(player);
                pageCarExtra.GuiPageHide(player);
                pageRowboat.GuiPageHide(player);
                pageRhib.GuiPageHide(player);
                pageSolosub.GuiPageHide(player);
                pageDuosub.GuiPageHide(player);
            }
        }

        [ConsoleCommand("signal_give")]
        private void cmdConsoleSignalGive(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin || arg.IsRcon)
            {
                if (configData == null) return;
                ulong playerID;
                if (ulong.TryParse(arg.Args[0], out playerID))
                {
                    var player = BasePlayer.FindByID(playerID);
                    if (player != null)
                    {
                        if (arg.Args.Length > 1)
                        {
                            if (SignalDefinitions.definitions.ContainsKey(arg.Args[1]) || arg.Args[1] == "normal")
                            {
                                var kind = arg.Args[1];
                                int amount = 1;
                                if (arg.Args.Length > 2)
                                {
                                    if (!int.TryParse(arg.Args[2], out amount))
                                    {
                                        amount = 1;
                                    }
                                }
                                Instance.PrintWarning($"Gave {player.displayName} {amount} {kind} signal.");
                                GiveSignal(player, kind, amount);
                            }
                        }
                    }
                }
            }
        }

        [ConsoleCommand("vagui_close")]
        private void cmdConsoleGuiClose(ConsoleSystem.Arg arg)
        {
            if (configData == null) return;
            var player = arg.Player();
            if (player == null) return;
            if (GUI == null) return;
            GUI.GuiClose(player);
        }
        [ConsoleCommand("vagui_option")]
        private void cmdConsoleGuiOption(ConsoleSystem.Arg arg)
        {
            if (configData == null) return;

            var player = arg.Player();
            if (player == null) return;
            if (!HasPermission(player, PERMISSION_ADMIN)) return;

            bool nullArg2 = false;

            if (arg.Args.Length < 3)
            {
                if (arg.Args.Length == 2)
                {
                    nullArg2 = true;
                }
                else
                {
                    return;
                }
            }

            if (arg.Args.Length > 3)
            {
                //glue all the args to arg[2]
                for (var a = 3; a < arg.Args.Length; a++)
                {
                    arg.Args[2] += $" {arg.Args[a]}";
                }
            }

            var displayPage = arg.Args[0];

            string kind = null;

            int maybeKind = -1;
            if (Int32.TryParse(displayPage, out maybeKind))
            {
                if (maybeKind > 1)
                {
                    switch (displayPage)
                    {
                        case "2":
                            {
                                kind = "normal";
                            }
                            break;
                        case "3":
                            {
                                kind = "minicopter";
                            }
                            break;
                        case "4":
                            {
                                kind = "scrapheli";
                            }
                            break;
                        case "5":
                            {
                                kind = "crate";
                            }
                            break;
                        case "6":
                            {
                                kind = "car";
                            }
                            break;
                        case "7":
                            {
                                kind = "rowboat";
                            }
                            break;
                        case "8":
                            {
                                kind = "rhib";
                            }
                            break;
                        case "9":
                            {
                                kind = "solosub";
                            }
                            break;
                        case "10":
                            {
                                kind = "duosub";
                            }
                            break;
                    }
                }
            }

            var option = arg.Args[1];


            var value = nullArg2 ? "" : arg.Args[2];

            var restartVendingMachine = false;
            var recalculateWeights = false;
            var recalculateExtra = false;

            //what's the option?
            switch (option)
            {
                //PAGE 0
                case "enablevabuy":
                    {
                        configData.enableCommandShop = StringToBool(value);
                    }
                    break;
                case "enablevending":
                    {
                        configData.enableVendingMachine = StringToBool(value);
                        restartVendingMachine = true;
                    }
                    break;
                case "vendingrelative":
                    {
                        configData.vendingMachineLocationIsRelative = StringToBool(value);
                        restartVendingMachine = true;
                    }
                    break;
                case "vabuypayment":
                    {
                        configData.defaultPayment = value;
                    }
                    break;
                case "vendingname":
                    {
                        if (nullArg2) break;
                        configData.vendingMachineCustomName = value;
                        restartVendingMachine = true;
                    }
                    break;
                case "vendingrestock":
                    {
                        //check if value is a uint
                        uint maybeUint;
                        if (uint.TryParse(value, out maybeUint))
                        {
                            if (maybeUint > 0)
                            {
                                configData.vendingMachineRestockEvery = maybeUint;
                                restartVendingMachine = true;
                            }
                        }
                    }
                    break;
                case "vendingposx":
                    {
                        if (nullArg2) break;
                        //check if value is a uint
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            configData.vendingMachinePosX = maybeFloat;
                            restartVendingMachine = true;
                        }
                    }
                    break;
                case "vendingposy":
                    {
                        if (nullArg2) break;
                        //check if value is a uint
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            configData.vendingMachinePosY = maybeFloat;
                            restartVendingMachine = true;
                        }
                    }
                    break;
                case "vendingposz":
                    {
                        if (nullArg2) break;
                        //check if value is a uint
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            configData.vendingMachinePosZ = maybeFloat;
                            restartVendingMachine = true;
                        }
                    }
                    break;
                case "vendingrotx":
                    {
                        if (nullArg2) break;
                        //check if value is a uint
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            configData.vendingMachineRotX = maybeFloat;
                            restartVendingMachine = true;
                        }
                    }
                    break;
                case "vendingroty":
                    {
                        if (nullArg2) break;
                        //check if value is a uint
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            configData.vendingMachineRotY = maybeFloat;
                            restartVendingMachine = true;
                        }
                    }
                    break;
                case "vendingrotz":
                    {
                        if (nullArg2) break;
                        //check if value is a uint
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            configData.vendingMachineRotZ = maybeFloat;
                            restartVendingMachine = true;
                        }
                    }
                    break;

                //PAGE 1 STEFANO
                case "dropenable":
                    {
                        configData.enableRandomDrop = StringToBool(value);
                    }
                    break;
                case "dropchance":
                    {
                        //get rid of % if any
                        value = value.Replace("%", "");
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            //divide it by 100, check if it's smaller than 0 or larger than 1!
                            if (maybeFloat <= 100 && maybeFloat > 0)
                            {
                                configData.customDropChance = maybeFloat / 100F;
                            }
                        }

                    }
                    break;
                case "lootenable":
                    {
                        configData.enableRandomLoot = StringToBool(value);
                    }
                    break;
                case "lootchance":
                    {
                        value = value.Replace("%", "");
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            //divide it by 100, check if it's smaller than 0 or larger than 1!
                            if (maybeFloat <= 100 && maybeFloat > 0)
                            {
                                configData.customLootChance = maybeFloat / 100F;
                            }
                        }
                    }
                    break;
                case "privateenable":
                    {
                        configData.enablePrivateSignals = StringToBool(value);
                    }
                    break;
                case "privateteam":
                    {
                        configData.privateSignalsIncludeTeammates = StringToBool(value);
                    }
                    break;
                case "privatetime":
                    {
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat > 0)
                            {
                                configData.unlockPrivateDropAfter = maybeFloat;
                            }
                        }
                    }
                    break;
                case "parachutedamagedetaches":
                    {
                        configData.damageDetachesParachute = StringToBool(value);
                    }
                    break;
                case "parachuteuse":
                    {
                        configData.usableWithParachute = StringToBool(value);
                    }
                    break;
                case "parachuteindestructible":
                    {
                        configData.indestructibleWithParachute = StringToBool(value);
                    }
                    break;
                case "parachutedecay":
                    {
                        configData.decaysWithParachute = StringToBool(value);
                    }
                    break;
                case "dropspeed":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat > 0.1 && maybeFloat <= 10F)
                            {
                                configData.fallSpeedWithParachute = maybeFloat;
                            }
                        }
                    }
                    break;
                case "dropinaccuracy":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0 && maybeFloat <= 500F)
                            {
                                configData.inaccuracy = maybeFloat;
                            }
                        }
                    }
                    break;

                //pages 2 - 8, take kind into account
                case "vendingenable":
                    {
                        var val = StringToBool(value);
                        if (kind == "normal")
                        {
                            configData.vendingMachineHasNormal = val;
                        }
                        else
                        {
                            configData.dropConfigs[kind].enableInVendingMachine = val;
                        }
                        restartVendingMachine = true;
                    }
                    break;
                case "vendingrestockamount":
                    {
                        if (nullArg2) break;
                        int maybeInt;
                        if (int.TryParse(value, out maybeInt))
                        {
                            if (maybeInt >= 0 && maybeInt <= 1000)
                            {
                                var val = maybeInt;

                                if (kind == "normal")
                                {
                                    configData.vendingMachineNormalQuantity = val;
                                }
                                else
                                {
                                    configData.dropConfigs[kind].vendingMachineRestockQuantity = val;
                                }
                                restartVendingMachine = true;
                            }
                        }
                    }
                    break;
                case "vabuyenable":
                    {
                        var val = StringToBool(value);
                        if (kind == "normal")
                        {
                            configData.commandShopHasNormal = val;
                        }
                        else
                        {
                            configData.dropConfigs[kind].enableInCommandShop = val;
                        }
                    }
                    break;
                case "pricecurrency":
                    {
                        if (nullArg2) break;
                        int maybeInt;
                        if (int.TryParse(value, out maybeInt))
                        {
                            if (maybeInt >= 0 && maybeInt <= 100000)
                            {
                                var val = maybeInt;

                                if (kind == "normal")
                                {
                                    configData.priceNormalCurrency = val;
                                }
                                else
                                {
                                    configData.dropConfigs[kind].priceCurrency = val;
                                }
                            }
                            restartVendingMachine = true;
                        }

                    }
                    break;
                case "priceserverrewards":
                    {
                        if (nullArg2) break;
                        int maybeInt;
                        if (int.TryParse(value, out maybeInt))
                        {
                            if (maybeInt >= 0)
                            {
                                var val = maybeInt;

                                if (kind == "normal")
                                {
                                    configData.priceNormalServerRewards = val;
                                }
                                else
                                {
                                    configData.dropConfigs[kind].priceServerRewards = val;
                                }
                            }
                        }
                    }
                    break;
                case "priceeconomics":
                    {
                        if (nullArg2) break;
                        int maybeInt;
                        if (int.TryParse(value, out maybeInt))
                        {
                            if (maybeInt >= 0)
                            {
                                var val = maybeInt;

                                if (kind == "normal")
                                {
                                    configData.priceNormalEconomics = val;
                                }
                                else
                                {
                                    configData.dropConfigs[kind].priceEconomics = val;
                                }
                            }
                        }
                    }
                    break;
                case "currencyitem":
                    {
                        if (nullArg2) break;
                        var itemId = 0;

                        ItemDefinition def = ItemManager.FindItemDefinition(value.ToLower());

                        if (def != null)
                        {
                            itemId = def.itemid;
                        }
                        else
                        {
                            int maybeInt;

                            if (int.TryParse(value, out maybeInt))
                            {
                                //check if that item exists
                                if (ItemManager.FindItemDefinition(maybeInt))
                                {
                                    itemId = maybeInt;
                                }
                            }
                        }

                        if (itemId != 0)
                        {
                            if (kind == "normal")
                            {
                                configData.normalCurrencyItemID = itemId;
                            }
                            else
                            {
                                configData.dropConfigs[kind].currencyItemID = itemId;
                            }

                            restartVendingMachine = true;
                        }
                    }
                    break;
                case "permission":
                    {
                        if (kind == "normal")
                        {
                            configData.permissionNormal = value;
                        }
                        else
                        {
                            configData.dropConfigs[kind].permission = value;
                        }
                    }
                    break;
                case "dropweight":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            var val = maybeFloat;
                            if (val >= 0F)
                            {
                                if (kind == "normal")
                                {
                                    //do nothing
                                }
                                else
                                {
                                    configData.dropConfigs[kind].randomDropWeight = val;
                                    recalculateWeights = true;
                                }
                            }
                        }
                    }
                    break;
                case "lootweight":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            var val = maybeFloat;
                            if (val >= 0F)
                            {
                                if (kind == "normal")
                                {
                                    //do nothing
                                }
                                else
                                {
                                    configData.dropConfigs[kind].randomLootWeight = val;
                                    recalculateWeights = true;
                                }
                            }
                        }
                    }
                    break;
                case "fuel":
                    {
                        if (nullArg2) break;
                        int maybeInt;
                        if (int.TryParse(value, out maybeInt))
                        {
                            var val = maybeInt;

                            if (val >= 0 && val <= 500)
                            {
                                if (kind == "normal")
                                {
                                    //do nothing
                                }
                                else
                                {
                                    configData.dropConfigs[kind].comesWithFuel = val;
                                }
                            }
                        }
                    }
                    break;
                case "health":
                    {
                        if (nullArg2) break;
                        //divide the input by 100!

                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            var val = maybeFloat / 100;

                            if (val >= 0F && val <= 1F)
                            {
                                if (kind == "normal")
                                {
                                    //do nothing
                                }
                                else
                                {
                                    configData.dropConfigs[kind].comesWithHealth = val;
                                }
                            }
                        }
                    }
                    break;
                case "gotoextra":
                    {
                        //show particular page:
                        GUI.PageShow(player, PageIDtoInstance(displayPage + "EXTRA"));
                        return;
                    }
                case "apply":
                    {
                        var val = StringToBool(value);
                        if (kind == "normal")
                        {
                            //do nothing
                        }
                        else
                        {
                            configData.dropConfigs[kind].applyToNatural = val;
                        }
                    }
                    break;

                //page extra
                case "twomodules":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.randomWeight2modules = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "threemodules":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.randomWeight3modules = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "fourmodules":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.randomWeight4modules = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "smallengineparts":
                    {
                        configData.dropConfigs["car"].extraSettings.moduleCockpitEngineComesWithParts = StringToBool(value);
                    }
                    break;
                case "bigengineparts":
                    {
                        configData.dropConfigs["car"].extraSettings.moduleBigEngineComesWithParts = StringToBool(value);
                    }
                    break;
                case "crankshaft1":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleCockpitEngineCrankshaftChances["crankshaft1"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;

                case "crankshaft2":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleCockpitEngineCrankshaftChances["crankshaft2"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "crankshaft3":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleCockpitEngineCrankshaftChances["crankshaft3"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "crankshaft0":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleCockpitEngineCrankshaftChances["none"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "carburetor1":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleCockpitEngineCarburetorChances["carburetor1"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "carburetor2":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleCockpitEngineCarburetorChances["carburetor2"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "carburetor3":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleCockpitEngineCarburetorChances["carburetor3"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "carburetor0":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleCockpitEngineCarburetorChances["none"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "valves1a":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleCockpitEngineValvesChances["valves1"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "valves2a":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleCockpitEngineValvesChances["valves2"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "valves3a":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleCockpitEngineValvesChances["valves3"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "valves0a":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleCockpitEngineValvesChances["none"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "valves1b":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleBigEngineValvesBChances["valves1"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "valves2b":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleBigEngineValvesBChances["valves2"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "valves3b":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleBigEngineValvesBChances["valves3"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "valves0b":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleBigEngineValvesBChances["none"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "sparkplugs1a":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleCockpitEngineSparkplugsChances["sparkplugs1"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "sparkplugs2a":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleCockpitEngineSparkplugsChances["sparkplugs2"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "sparkplugs3a":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleCockpitEngineSparkplugsChances["sparkplugs3"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "sparkplugs0a":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleCockpitEngineSparkplugsChances["none"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "sparkplugs1b":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleBigEngineSparkplugsBChances["sparkplugs1"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "sparkplugs2b":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleBigEngineSparkplugsBChances["sparkplugs2"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "sparkplugs3b":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleBigEngineSparkplugsBChances["sparkplugs3"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "sparkplugs0b":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleBigEngineSparkplugsBChances["none"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "pistons1a":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleCockpitEnginePistonsChances["pistons1"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "pistons2a":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleCockpitEnginePistonsChances["pistons2"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "pistons3a":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleCockpitEnginePistonsChances["pistons3"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "pistons0a":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleCockpitEnginePistonsChances["none"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "pistons1b":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleBigEnginePistonsBChances["pistons1"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "pistons2b":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleBigEnginePistonsBChances["pistons2"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "pistons3b":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleBigEnginePistonsBChances["pistons3"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
                case "pistons0b":
                    {
                        if (nullArg2) break;
                        float maybeFloat;
                        if (float.TryParse(value, out maybeFloat))
                        {
                            if (maybeFloat >= 0F)
                            {
                                configData.dropConfigs["car"].extraSettings.moduleBigEnginePistonsBChances["none"] = maybeFloat;
                                recalculateExtra = true;
                            }
                        }
                    }
                    break;
            }

            if (recalculateWeights)
            {
                CalculateWeightSumsDrop();
                CalculateWeightSumsLoot();
            }

            if (recalculateExtra)
            {
                CalculateWeightSumsEngine();
            }

            SaveConfigData();

            if (restartVendingMachine)
            {
                KillVendingMachine();
                Instance.NextTick(CreateVendingMachine);
            }



            //display what page next?
            GUI.PageShow(player, PageIDtoInstance(displayPage));
        }

        public GuiManager.GuiPage PageIDtoInstance(string pageID)
        {
            GuiManager.GuiPage page = null;

            switch (pageID)
            {
                default:
                case "0":
                    {
                        page = GUI.pageShopConfig;
                    }
                    break;
                case "1":
                    {
                        page = GUI.pageAllDrops;
                    }
                    break;
                case "2":
                    {
                        page = GUI.pageNormal;
                    }
                    break;
                case "3":
                    {
                        page = GUI.pageMinicopter;
                    }
                    break;
                case "4":
                    {
                        page = GUI.pageScrapheli;
                    }
                    break;
                case "5":
                    {
                        page = GUI.pageCrate;
                    }
                    break;
                case "6":
                    {
                        page = GUI.pageCar;
                    }
                    break;
                case "6EXTRA":
                    {
                        page = GUI.pageCarExtra;
                    }
                    break;
                case "7":
                    {
                        page = GUI.pageRowboat;
                    }
                    break;
                case "8":
                    {
                        page = GUI.pageRhib;
                    }
                    break;
                case "9":
                    {
                        page = GUI.pageSolosub;
                    }
                    break;
                case "10":
                    {
                        page = GUI.pageDuosub;
                    }
                    break;
            }   
            
            return page;
        }

        [ConsoleCommand("vagui_page")]
        private void cmdConsoleGuiPage(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            if (!HasPermission(player, PERMISSION_ADMIN)) return;

            if (arg.Args.Length == 0) return;

            GuiManager.GuiPage toShow = PageIDtoInstance(arg.Args[0]);


            GUI.PageShow(player, toShow);
        }

        [ConsoleCommand("vagui_open")]
        private void cmdConsoleGuiOpen(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            if (!HasPermission(player, PERMISSION_ADMIN)) return;

            GUI.GuiOpen(player);
        }

        [ChatCommand("vagui")]
        private void cmdChatVaGui(BasePlayer player, string command, string[] args)
        {
            if (GUI == null) return;
            if (HasPermission(player, PERMISSION_ADMIN))
            {
                GUI.GuiOpen(player);
            }
        }
        #endregion
        #region HELPERS
        public static FireworkLauncher CreateFireworkLauncher(Vector3 position)
        {
            var newGameObject = new GameObject("FireworkLauncher");
            newGameObject.SetActive(true);
            newGameObject.transform.position = position;
            newGameObject.transform.hasChanged = true;
            newGameObject.layer = (int)Rust.Layer.Reserved1;

            var newLauncher = newGameObject.AddComponent<FireworkLauncher>();

            return newLauncher;
        }

        public static string RandomFireworkPrefab()
        {
            if (Instance.configData.fireworkPrefabList.Count == 0) return null;

            var rndIndex = UnityEngine.Random.Range(0, Instance.configData.fireworkPrefabList.Count);

            return Instance.configData.fireworkPrefabList[rndIndex];
        }

        public void RidiculousWorkaround(CustomDrop drop)
        {
            //Instance.PrintToChat("RIDICULOUS WORKAROUND");
            //the stupidest hack ever (if it's stupid and it works, it's not stupid)
            if (drop.dummyPlayer == null)
            {
                drop.dummyPlayer = GameManager.server.CreateEntity("assets/prefabs/player/player.prefab", drop.modularCar.transform.position, drop.modularCar.transform.rotation, true).ToPlayer();
                drop.dummyPlayer.limitNetworking = true;
                drop.dummyPlayer.Spawn();

                if (drop.dummyPlayer != null)
                {
                    var seat = drop.modularCar.gameObject.GetComponentsInChildren<BaseVehicleSeat>().FirstOrDefault();

                    drop.dummyPlayer.MountObject(seat);
                    drop.modularCar.SendNetworkUpdateImmediate();

                    Instance.timer.Once(0.15F, () =>
                    {
                        drop.dummyPlayer.limitNetworking = false;
                        drop.dummyPlayer.EnsureDismounted();
                        drop.dummyPlayer.Kill(BaseNetworkable.DestroyMode.None);
                        drop.dummyPlayer = null;
                    });
                }
            }
        }

        public static bool StringToBool(string boolString)
        {
            string lowboi = boolString.ToLower();

            return (lowboi.Contains("t") || lowboi.Contains("1") || lowboi.Contains("on") || lowboi.Contains("enable"));
        }
        public bool IsHumanNPC(ulong userID)
        {
            return !(userID >= 76560000000000000L || userID <= 0L);
        }

        public Timer restockTimer = null;

        public void EquipVehicle(BaseEntity entity, string kind, bool isDrop = true)
        {
            if (isDrop || Instance.configData.dropConfigs[kind].applyToNatural)
            {
                if (Instance.configData.dropConfigs[kind].comesWithFuel > 0)
                {

                    var fuel = ItemManager.CreateByItemID(-946369541, Instance.configData.dropConfigs[kind].comesWithFuel, 0);


                    var maybeBoat = entity as MotorRowboat;

                    StorageContainer container;

                    if (maybeBoat != null)
                    {
                        container = maybeBoat.GetFuelSystem().fuelStorageInstance.Get(true);

                        if (container != null)
                            fuel.MoveToContainer(container.inventory, -1, true);

                    }
                    else
                    {
                        var maybeSub = entity as BaseSubmarine;
                        if (maybeSub != null)
                        {
                            container = maybeSub.GetFuelSystem().fuelStorageInstance.Get(true);

                            if (container != null)
                            {
                                fuel.MoveToContainer(container.inventory, -1, true);
                            }
                        }
                        else
                        {
                            var maybeMinicopter = entity as MiniCopter;
                            if (maybeMinicopter != null)
                            {
                                container = maybeMinicopter.GetFuelSystem().fuelStorageInstance.Get(true);

                                if (container != null)
                                    fuel.MoveToContainer(container.inventory, -1, true);
                            }
                            else
                            {
                                var maybeModular = entity as ModularCar;
                                if (maybeModular != null)
                                {
                                    var storages = maybeModular.GetComponentsInChildren<StorageContainer>();

                                    StorageContainer fuelContainer = null;

                                    ExtraSettingsModular extra = configData.dropConfigs["car"].extraSettings as ExtraSettingsModular;

                                    foreach (var thisStorage in storages)
                                    {

                                        switch (thisStorage.ShortPrefabName)
                                        {
                                            case "modular_car_fuel_storage":
                                                {
                                                    fuelContainer = thisStorage;
                                                }
                                                break;
                                            case "modular_car_i4_engine_storage":
                                                {
                                                    if (extra.moduleCockpitEngineComesWithParts)
                                                    {
                                                        var carburetor = WeightedRandomDictionaryKey(extra.moduleCockpitEngineCarburetorChances);
                                                        if (carburetor != "none")
                                                        {
                                                            var tempItem = ItemManager.CreateByItemID(ItemNameToItemID[carburetor]);
                                                            tempItem.MoveToContainer(thisStorage.inventory, MODULAR_SLOT_CARBURETOR);

                                                            tempItem.MarkDirty();
                                                        }
                                                        var crankshaft = WeightedRandomDictionaryKey(extra.moduleCockpitEngineCrankshaftChances);
                                                        if (crankshaft != "none")
                                                        {
                                                            var tempItem = ItemManager.CreateByItemID(ItemNameToItemID[crankshaft]);
                                                            tempItem.MoveToContainer(thisStorage.inventory, MODULAR_SLOT_CRANKSHAFT);
                                                            tempItem.MarkDirty();
                                                        }
                                                        var valves = WeightedRandomDictionaryKey(extra.moduleCockpitEngineValvesChances);
                                                        if (valves != "none")
                                                        {
                                                            var tempItem = ItemManager.CreateByItemID(ItemNameToItemID[valves]);
                                                            tempItem.MoveToContainer(thisStorage.inventory, MODULAR_SLOT_VALVES_A);
                                                            tempItem.MarkDirty();
                                                        }
                                                        var sparkplugs = WeightedRandomDictionaryKey(extra.moduleCockpitEngineSparkplugsChances);
                                                        if (sparkplugs != "none")
                                                        {
                                                            var tempItem = ItemManager.CreateByItemID(ItemNameToItemID[sparkplugs]);
                                                            tempItem.MoveToContainer(thisStorage.inventory, MODULAR_SLOT_SPARKPLUGS_A);
                                                            tempItem.MarkDirty();
                                                        }
                                                        var pistons = WeightedRandomDictionaryKey(extra.moduleCockpitEnginePistonsChances);
                                                        if (pistons != "none")
                                                        {
                                                            var tempItem = ItemManager.CreateByItemID(ItemNameToItemID[pistons]);
                                                            tempItem.MoveToContainer(thisStorage.inventory, MODULAR_SLOT_PISTONS_A);
                                                            tempItem.MarkDirty();
                                                        }
                                                        thisStorage.inventory.MarkDirty();
                                                        thisStorage.SendNetworkUpdateImmediate();

                                                    }
                                                }
                                                break;
                                            case "modular_car_v8_engine_storage":
                                                {
                                                    if (extra.moduleBigEngineComesWithParts)
                                                    {
                                                        var carburetor = WeightedRandomDictionaryKey(extra.moduleCockpitEngineCarburetorChances);
                                                        if (carburetor != "none")
                                                        {
                                                            var tempItem = ItemManager.CreateByItemID(ItemNameToItemID[carburetor]);
                                                            tempItem.MoveToContainer(thisStorage.inventory, MODULAR_SLOT_CARBURETOR);
                                                            tempItem.MarkDirty();
                                                        }

                                                        var crankshaft = WeightedRandomDictionaryKey(extra.moduleCockpitEngineCrankshaftChances);
                                                        if (crankshaft != "none")
                                                        {
                                                            var tempItem = ItemManager.CreateByItemID(ItemNameToItemID[crankshaft]);
                                                            tempItem.MoveToContainer(thisStorage.inventory, MODULAR_SLOT_CRANKSHAFT);
                                                            tempItem.MarkDirty();
                                                        }

                                                        var valvesA = WeightedRandomDictionaryKey(extra.moduleCockpitEngineValvesChances);
                                                        if (valvesA != "none")
                                                        {
                                                            var tempItem = ItemManager.CreateByItemID(ItemNameToItemID[valvesA]);
                                                            tempItem.MoveToContainer(thisStorage.inventory, MODULAR_SLOT_VALVES_A);
                                                            tempItem.MarkDirty();
                                                        }
                                                        var valvesB = WeightedRandomDictionaryKey(extra.moduleBigEngineValvesBChances);
                                                        if (valvesB != "none")
                                                        {
                                                            var tempItem = ItemManager.CreateByItemID(ItemNameToItemID[valvesB]);
                                                            tempItem.MoveToContainer(thisStorage.inventory, MODULAR_SLOT_VALVES_B);
                                                            tempItem.MarkDirty();
                                                        }

                                                        var sparkplugsA = WeightedRandomDictionaryKey(extra.moduleCockpitEngineSparkplugsChances);
                                                        if (sparkplugsA != "none")
                                                        {
                                                            var tempItem = ItemManager.CreateByItemID(ItemNameToItemID[sparkplugsA]);
                                                            tempItem.MoveToContainer(thisStorage.inventory, MODULAR_SLOT_SPARKPLUGS_A);
                                                            tempItem.MarkDirty();
                                                        }
                                                        var sparkplugsB = WeightedRandomDictionaryKey(extra.moduleBigEngineSparkplugsBChances);
                                                        if (sparkplugsB != "none")
                                                        {
                                                            var tempItem = ItemManager.CreateByItemID(ItemNameToItemID[sparkplugsB]);
                                                            tempItem.MoveToContainer(thisStorage.inventory, MODULAR_SLOT_SPARKPLUGS_B);
                                                            tempItem.MarkDirty();
                                                        }

                                                        var pistonsA = WeightedRandomDictionaryKey(extra.moduleCockpitEnginePistonsChances);
                                                        if (pistonsA != "none")
                                                        {
                                                            var tempItem = ItemManager.CreateByItemID(ItemNameToItemID[pistonsA]);
                                                            tempItem.MoveToContainer(thisStorage.inventory, MODULAR_SLOT_PISTONS_A);
                                                            tempItem.MarkDirty();
                                                        }

                                                        var pistonsB = WeightedRandomDictionaryKey(extra.moduleBigEnginePistonsBChances);
                                                        if (pistonsB != "none")
                                                        {
                                                            var tempItem = ItemManager.CreateByItemID(ItemNameToItemID[pistonsB]);
                                                            tempItem.MoveToContainer(thisStorage.inventory, MODULAR_SLOT_PISTONS_B);
                                                            tempItem.MarkDirty();
                                                        }
                                                        thisStorage.inventory.MarkDirty();
                                                        thisStorage.SendNetworkUpdateImmediate();
                                                    }
                                                }
                                                break;
                                        }
                                    }

                                    if (fuelContainer != null)

                                        fuel.MoveToContainer(fuelContainer.inventory, -1, true);

                                    Instance.timer.Once(0.2F, () =>
                                    {
                                        foreach (var entry in maybeModular.AttachedModuleEntities)
                                        {
                                            entry.InitializeHealth(configData.dropConfigs[kind].comesWithHealth * entry.MaxHealth(), entry.MaxHealth());
                                            entry.SendNetworkUpdateImmediate();
                                        }
                                    });
                                }
                            }
                        }


                    }
                }
            }
        }

        public static readonly string HTTPS = "aHR0cHM6Ly93ZWVrbHlydXN0LmNvbS9pbWFnZW4vc2hvd19pbWFnZS5waHA/aW1nPQ==";

        public void RestockVendingMachine()
        {
            if (configData.dedicatedVendingMachine == null) return;

            configData.dedicatedVendingMachine.sellOrders.sellOrders.Clear();

            foreach (Item obj in configData.dedicatedVendingMachine.inventory.itemList.ToArray())
            {
                obj.Remove(0.0f);
            }

            //are we adding normal supply signal too?

            if (configData.vendingMachineHasNormal)
            {
                if (configData.priceNormalCurrency > 0)
                {
                    configData.dedicatedVendingMachine.AddSellOrder(1397052267, configData.vendingMachineNormalQuantity, configData.normalCurrencyItemID, configData.priceNormalCurrency, 0);
                    //and add this many plain supply drops. NO SKIN.
                    var normalDrop = ItemManager.CreateByItemID(1397052267, configData.vendingMachineNormalQuantity, 0);
                    normalDrop.MoveToContainer(configData.dedicatedVendingMachine.inventory, -1, true);
                    normalDrop.MarkDirty();

                }
            }

            foreach (var drop in configData.dropConfigs.OrderBy(d => d.Value.priceCurrency))
            {
                if (drop.Value.enableInVendingMachine)
                {
                    if (drop.Value.priceCurrency > 0)
                    {
                        if (drop.Value.vendingMachineRestockQuantity > 0)
                        {
                            //what fake item is this gonna be? check the kind.
                            var kind = drop.Value.name;

                            var itemName = KindToFakeItemName[kind];

                            var fakeItemID = ItemNameToItemID[itemName];

                            configData.dedicatedVendingMachine.AddSellOrder(fakeItemID, 1, drop.Value.currencyItemID, drop.Value.priceCurrency, 0);
                            //and add this many items for this kind, they're gonna auto-magically turn into "fake" items!

                            var customDrop = ItemManager.CreateByItemID(1397052267, drop.Value.vendingMachineRestockQuantity, 0);
                            //now turn it!
                            TurnSignalIntoCustom(customDrop, drop.Value.name);

                            customDrop.MoveToContainer(configData.dedicatedVendingMachine.inventory, -1, true);
                            customDrop.MarkDirty();
                        }
                    }
                }
            }

            configData.dedicatedVendingMachine.inventory.MarkDirty();

            configData.dedicatedVendingMachine.shopName = configData.vendingMachineCustomName;
            configData.dedicatedVendingMachine.OwnerID = 0;

            configData.dedicatedVendingMachine.skinID = 861142659UL;

            configData.dedicatedVendingMachine.SetFlag(VendingMachine.VendingMachineFlags.Broadcasting, false, false, true);
            configData.dedicatedVendingMachine.UpdateMapMarker();

            Instance.NextTick(() =>
            {
                configData.dedicatedVendingMachine.SetFlag(VendingMachine.VendingMachineFlags.Broadcasting, true, false, true);
                configData.dedicatedVendingMachine.UpdateMapMarker();
                configData.dedicatedVendingMachine.SendSellOrders();
                configData.dedicatedVendingMachine.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
            });

            //and on, and on, and on...
            SetRestockTimer();
        }

        public void ClearRestockTimer()
        {
            if (restockTimer != null)
            {
                restockTimer.Destroy();
                restockTimer = null;
            }
        }

        public void SetRestockTimer()
        {
            ClearRestockTimer();

            restockTimer = timer.Once(60.0F * configData.vendingMachineRestockEvery, RestockVendingMachine);
        }

        public string GetBoolColor(bool boolean)
        {
            return boolean ? ColorPalette.GreenLight.rustString : ColorPalette.RedLight.rustString;
        }

        public void DetachParachute(CustomNormalDrop drop)
        {
            if (drop == null) return;
            if (!drop.landed)
            {

                //drop.alreadyDestroying = true;
                //drop.active = true;

                if (drop.rigidbody != null)
                {
                    drop.rigidbody.drag = 0; //drop.landDrag;
                    drop.rigidbody.velocity = Vector3.zero;
                }

                if (drop.supplyDrop != null)
                {
                    drop.supplyDrop.RemoveParachute();
                    drop.supplyDrop.SendNetworkUpdateImmediate();
                }


                if (configData.enableFireworks && (drop.ownerID == 0 || (drop.ownerID != 0 && configData.enableFireworksForPrivateToo)))
                {
                    CreateFireworkLauncher(drop.transform.position);
                }

                //just so we don't update too often

                //no need to keep it in memory if it already comes unlocked
                if (!configData.enablePrivateSignals || drop.ownerID == 0)
                {

                    UnityEngine.Object.Destroy(drop);
                }
                else
                {
                    drop.shouldUnlockAtTime = Time.realtimeSinceStartup + configData.unlockPrivateDropAfter;
                }
                drop.landed = true;

            }
        }

        public void DetachParachute(CustomDrop drop)
        {
            if (!drop.landed)
            {
                if (drop.isCrate)
                {
                    drop.crateRigidbody.drag = drop.landDrag;
                }
                else
                {
                    if (drop.isModularVehicle)
                    {
                        drop.modularCar.rigidBody.useGravity = true;
                        //drop.modularCar.rigidBody.drag = drop.landDrag;
                        //drop.modularCar.rigidBody.isKinematic = false;
                        drop.modularCar.rigidBody.velocity = Vector3.zero;
                        RidiculousWorkaround(drop);
                    }
                    else
                    {
                        if (drop.useDummyPivot)
                        {

                            drop.vehicle.SetParent(null, true, true);
                            drop.dummyPivot.Kill();
                        }

                        var maybeBoat = drop.vehicle as MotorRowboat;

                        if (maybeBoat != null)
                        {
                            //restore updating drag
                            maybeBoat.waterDrag = drop.waterDrag;
                            maybeBoat.landDrag = drop.landDrag;
                        }
                        else
                        {
                            var maybeCopter = drop.vehicle as MiniCopter;

                            if (maybeCopter != null)
                            {
                                drop.vehicle.rigidBody.drag = drop.landDrag;
                                maybeCopter.rigidBody.velocity = Vector3.zero;
                            }
                            else
                            {
                                if (!drop.useDummyPivot)
                                {
                                    drop.vehicle.rigidBody.drag = drop.landDrag;
                                }
                            }
                        }
                    }

                }

                drop.parachute.Kill(BaseNetworkable.DestroyMode.None);
                drop.landed = true;
                drop.alreadyDestroying = true;
                drop.active = true;


                if (configData.enableFireworks && (drop.ownerID == 0 || (drop.ownerID != 0 && configData.enableFireworksForPrivateToo)))
                {
                    CreateFireworkLauncher(drop.transform.position);
                }
                //just so we don't update too often

                //no need to keep it in memory if it already comes unlocked
                if (!configData.enablePrivateSignals || drop.ownerID == 0)
                {
                    UnityEngine.Object.DestroyImmediate(drop);
                }
                else
                {
                    drop.shouldUnlockAtTime = Time.realtimeSinceStartup + configData.unlockPrivateDropAfter;
                }

                //drop.DestroyImmediate(this);

                //you're still alive! just updating slower.
            }
        }
        public void CalculateWeightSumsEngine()
        {
            var cfg = configData.dropConfigs["car"].extraSettings;

            cfg.randomWeightModulesSum = cfg.randomWeight2modules + cfg.randomWeight3modules + cfg.randomWeight4modules;

            if (cfg.randomWeightModulesSum == 0F)
            {
                cfg.randomWeight2modules = 1.0F;
                cfg.randomWeight3modules = 1.0F;
                cfg.randomWeight4modules = 1.0F;

                cfg.randomWeightModulesSum = cfg.randomWeight2modules + cfg.randomWeight3modules + cfg.randomWeight4modules;

                SaveConfigData();
            }

            cfg.randomWeightCarburetorSum = cfg.moduleCockpitEngineCarburetorChances["none"] + cfg.moduleCockpitEngineCarburetorChances["carburetor1"] + cfg.moduleCockpitEngineCarburetorChances["carburetor2"] + cfg.moduleCockpitEngineCarburetorChances["carburetor3"];
            if (cfg.randomWeightCarburetorSum == 0F)
            {
                cfg.moduleCockpitEngineCarburetorChances["none"] = 0.0F;
                cfg.moduleCockpitEngineCarburetorChances["carburetor1"] = 1.0F;
                cfg.moduleCockpitEngineCarburetorChances["carburetor2"] = 1.0F;
                cfg.moduleCockpitEngineCarburetorChances["carburetor3"] = 1.0F;

                cfg.randomWeightCarburetorSum = cfg.moduleCockpitEngineCarburetorChances["none"] + cfg.moduleCockpitEngineCarburetorChances["carburetor1"] + cfg.moduleCockpitEngineCarburetorChances["carburetor2"] + cfg.moduleCockpitEngineCarburetorChances["carburetor3"];

                SaveConfigData();
            }


            cfg.randomWeightCrankshaftSum = cfg.moduleCockpitEngineCrankshaftChances["none"] + cfg.moduleCockpitEngineCrankshaftChances["crankshaft1"] + cfg.moduleCockpitEngineCrankshaftChances["crankshaft2"] + cfg.moduleCockpitEngineCrankshaftChances["crankshaft3"];
            if (cfg.randomWeightCrankshaftSum == 0F)
            {
                cfg.moduleCockpitEngineCrankshaftChances["none"] = 0.0F;
                cfg.moduleCockpitEngineCrankshaftChances["crankshaft1"] = 1.0F;
                cfg.moduleCockpitEngineCrankshaftChances["crankshaft2"] = 1.0F;
                cfg.moduleCockpitEngineCrankshaftChances["crankshaft3"] = 1.0F;

                cfg.randomWeightCrankshaftSum = cfg.moduleCockpitEngineCrankshaftChances["none"] + cfg.moduleCockpitEngineCrankshaftChances["crankshaft1"] + cfg.moduleCockpitEngineCrankshaftChances["crankshaft2"] + cfg.moduleCockpitEngineCrankshaftChances["crankshaft3"];

                SaveConfigData();
            }
            cfg.randomWeightValvesASum = cfg.moduleCockpitEngineValvesChances["none"] + cfg.moduleCockpitEngineValvesChances["valves1"] + cfg.moduleCockpitEngineValvesChances["valves2"] + cfg.moduleCockpitEngineValvesChances["valves3"];
            if (cfg.randomWeightValvesASum == 0F)
            {
                cfg.moduleCockpitEngineValvesChances["none"] = 0.0F;
                cfg.moduleCockpitEngineCrankshaftChances["valves1"] = 1.0F;
                cfg.moduleCockpitEngineCrankshaftChances["valves2"] = 1.0F;
                cfg.moduleCockpitEngineCrankshaftChances["valves3"] = 1.0F;

                cfg.randomWeightValvesASum = cfg.moduleCockpitEngineValvesChances["none"] + cfg.moduleCockpitEngineValvesChances["valves1"] + cfg.moduleCockpitEngineValvesChances["valves2"] + cfg.moduleCockpitEngineValvesChances["valves3"];

                SaveConfigData();
            }

            cfg.randomWeightValvesBSum = cfg.moduleBigEngineValvesBChances["none"] + cfg.moduleBigEngineValvesBChances["valves1"] + cfg.moduleBigEngineValvesBChances["valves2"] + cfg.moduleBigEngineValvesBChances["valves3"];
            if (cfg.randomWeightValvesBSum == 0F)
            {
                cfg.moduleBigEngineValvesBChances["none"] = 0.0F;
                cfg.moduleBigEngineValvesBChances["valves1"] = 1.0F;
                cfg.moduleBigEngineValvesBChances["valves2"] = 1.0F;
                cfg.moduleBigEngineValvesBChances["valves3"] = 1.0F;

                cfg.randomWeightValvesBSum = cfg.moduleBigEngineValvesBChances["none"] + cfg.moduleBigEngineValvesBChances["valves1"] + cfg.moduleBigEngineValvesBChances["valves2"] + cfg.moduleBigEngineValvesBChances["valves3"];

                SaveConfigData();
            }

            cfg.randomWeightSparkplugsASum = cfg.moduleCockpitEngineSparkplugsChances["none"] + cfg.moduleCockpitEngineSparkplugsChances["sparkplugs1"] + cfg.moduleCockpitEngineSparkplugsChances["sparkplugs2"] + cfg.moduleCockpitEngineSparkplugsChances["sparkplugs3"];
            if (cfg.randomWeightSparkplugsASum == 0F)
            {
                cfg.moduleCockpitEngineSparkplugsChances["none"] = 0.0F;
                cfg.moduleCockpitEngineSparkplugsChances["sparkplugs1"] = 1.0F;
                cfg.moduleCockpitEngineSparkplugsChances["sparkplugs2"] = 1.0F;
                cfg.moduleCockpitEngineSparkplugsChances["sparkplugs3"] = 1.0F;

                cfg.randomWeightSparkplugsASum = cfg.moduleCockpitEngineSparkplugsChances["none"] + cfg.moduleCockpitEngineSparkplugsChances["sparkplugs1"] + cfg.moduleCockpitEngineSparkplugsChances["sparkplugs2"] + cfg.moduleCockpitEngineSparkplugsChances["sparkplugs3"];

                SaveConfigData();
            }
            cfg.randomWeightSparkplugsBSum = cfg.moduleBigEngineSparkplugsBChances["none"] + cfg.moduleBigEngineSparkplugsBChances["sparkplugs1"] + cfg.moduleBigEngineSparkplugsBChances["sparkplugs2"] + cfg.moduleBigEngineSparkplugsBChances["sparkplugs3"];
            if (cfg.randomWeightSparkplugsBSum == 0F)
            {
                cfg.moduleBigEngineSparkplugsBChances["none"] = 0.0F;
                cfg.moduleBigEngineSparkplugsBChances["sparkplugs1"] = 1.0F;
                cfg.moduleBigEngineSparkplugsBChances["sparkplugs2"] = 1.0F;
                cfg.moduleBigEngineSparkplugsBChances["sparkplugs3"] = 1.0F;

                cfg.randomWeightSparkplugsBSum = cfg.moduleBigEngineSparkplugsBChances["none"] + cfg.moduleBigEngineSparkplugsBChances["sparkplugs1"] + cfg.moduleBigEngineSparkplugsBChances["sparkplugs2"] + cfg.moduleBigEngineSparkplugsBChances["sparkplugs3"];

                SaveConfigData();
            }

            cfg.randomWeightPistonsASum = cfg.moduleCockpitEnginePistonsChances["none"] + cfg.moduleCockpitEnginePistonsChances["pistons1"] + cfg.moduleCockpitEnginePistonsChances["pistons2"] + cfg.moduleCockpitEnginePistonsChances["pistons3"];
            if (cfg.randomWeightSparkplugsBSum == 0F)
            {
                cfg.moduleCockpitEnginePistonsChances["none"] = 0.0F;
                cfg.moduleCockpitEnginePistonsChances["pistons1"] = 1.0F;
                cfg.moduleCockpitEnginePistonsChances["pistons2"] = 1.0F;
                cfg.moduleCockpitEnginePistonsChances["pistons3"] = 1.0F;

                cfg.randomWeightPistonsASum = cfg.moduleCockpitEnginePistonsChances["none"] + cfg.moduleCockpitEnginePistonsChances["pistons1"] + cfg.moduleCockpitEnginePistonsChances["pistons2"] + cfg.moduleCockpitEnginePistonsChances["pistons3"];

                SaveConfigData();
            }
            cfg.randomWeightPistonsBSum = cfg.moduleBigEnginePistonsBChances["none"] + cfg.moduleBigEnginePistonsBChances["pistons1"] + cfg.moduleBigEnginePistonsBChances["pistons2"] + cfg.moduleBigEnginePistonsBChances["pistons3"];
            if (cfg.randomWeightPistonsBSum == 0F)
            {
                cfg.moduleBigEnginePistonsBChances["none"] = 0.0F;
                cfg.moduleBigEnginePistonsBChances["pistons1"] = 1.0F;
                cfg.moduleBigEnginePistonsBChances["pistons2"] = 1.0F;
                cfg.moduleBigEnginePistonsBChances["pistons3"] = 1.0F;

                cfg.randomWeightPistonsBSum = cfg.moduleBigEnginePistonsBChances["none"] + cfg.moduleBigEnginePistonsBChances["pistons1"] + cfg.moduleBigEnginePistonsBChances["pistons2"] + cfg.moduleBigEnginePistonsBChances["pistons3"];

                SaveConfigData();
            }
        }

        public void CalculateWeightSumsLoot()
        {
            float sum = 0;

            foreach (var entry in configData.dropConfigs)
            {
                sum += entry.Value.randomLootWeight;
            }

            configData.weightSumLoot = sum;
        }

        public void CustomizePlane(CargoPlane plane, string kind, ulong ownerID = 0)
        {
            CustomCargoPlanes.Add(plane, kind);
            plane.gameObject.AddComponent<CustomPlane>();
            var compo = plane.gameObject.GetComponent<CustomPlane>();
            compo.kind = kind;
            compo.ownerID = ownerID;
            plane.OwnerID = ownerID;

            if (Instance.configData.announceDropsInChat)
            {

                string colKind = MSG(MSG_ANNOUNCE_MESSAGE_FORMAT_KIND, null, kind == "normal" ? kind : SignalDefinitions.definitions[kind].itemSuffix);
                string msg = MSG(MSG_ANNOUNCE_MESSAGE_FULL, null, colKind);

                if (ownerID != 0 && configData.enablePrivateSignals)
                {
                    msg = MSG(MSG_ANNOUNCE_MESSAGE_FULL_PRIVATE, null, colKind, Instance.configData.unlockPrivateDropAfter);
                }

                TellMessage(null, msg);
            }

            Interface.Call("OnCustomAirdrop", plane, plane.GetDropPosition(), kind, ownerID);
        }

        public void CalculateWeightSumsDrop()
        {
            float sum = 0;

            foreach (var entry in configData.dropConfigs)
            {
                sum += entry.Value.randomDropWeight;
            }

            configData.weightSumDrop = sum;
        }

        public string WeightedRandomDictionaryKey(Dictionary<string, float> dictionary)
        {
            var result = "none";

            float weightSum = 0;

            Dictionary<string, float> filtered = new Dictionary<string, float>();

            foreach (var entry in dictionary)
            {
                if (entry.Value > 0)
                {
                    filtered.Add(entry.Key, entry.Value);
                    weightSum += entry.Value;
                }
            }

            //now roll a random value between 0 and weightSum
            var roll = UnityEngine.Random.Range(0, weightSum);

            float calcSum = 0;
            //another foreach through reverse
            foreach (var entry in filtered)
            {
                calcSum += entry.Value;
                if (roll <= calcSum)
                {
                    result = entry.Key;
                    break;
                }
            }

            return result;
        }

        public string WeightedRandomKind(bool loot)
        {
            string result = null;
            //if loot = true, we take loot weights, otherwise, drop weights

            float weightSumToUse = loot ? configData.weightSumLoot : configData.weightSumDrop;

            //step 1: we have the weights already pre-calculated.

            //roll a random between 0 and weightSumToUse
            float randomRoll = UnityEngine.Random.Range(0F, weightSumToUse);

            float sumSoFar = 0;
            float weightToUse;

            foreach (var element in configData.dropConfigs)
            {
                weightToUse = loot ? element.Value.randomLootWeight : element.Value.randomDropWeight;

                if (weightToUse == 0F) continue;

                sumSoFar += weightToUse;

                //check if the random is less than sumSoFar
                if (randomRoll <= sumSoFar)
                {
                    result = element.Key;
                    break;
                }
                //otherwise keep going through the loop
            }

            return result;
        }

        public void ReplaceFakeDroppedItemWithSignalDroppedItem(DroppedItem droppedItem)
        {

        }

        public Item ReplaceFakeItemWithSignalItemInContainer(ItemContainer container, Item item)
        {
            var skin = item.skin;

            /*
            var slot = 0;

            foreach (var entry in container.itemList)
            {
                if (container.GetSlot(slot) == item)
                {
                    break;
                }
                //no? try next slot
                slot++;
            }*/
            var slot = item.position;

            var name = item.name;

            var amount = item.amount;

            var kind = SkinIDToItemName[item.skin];

            item.RemoveFromContainer();
            item.Remove(0.0f);

            //item = ItemManager.CreateByItemID(fakeItemID);
            item = ItemManager.CreateByItemID(1397052267, amount);
            TurnSignalIntoCustom(item, SkinIDToItemName[skin]);

            item.MoveToContainer(container, slot, false);
            item.MarkDirty();
            container.MarkDirty();

            return item;
        }


        public Item ReplaceSignalItemWithFakeItemInContainer(ItemContainer container, Item item)
        {
            //what slot is it?
            var skin = item.skin;

            //find the ID!

            /*
            var slot = 0;

            foreach (var entry in container.itemList)
            {
                if (container.GetSlot(slot) == item)
                {
                    break;
                }
                //no? try next slot
                slot++;
            }
            */
            var slot = item.position;

            var name = item.name;

            var amount = item.amount;
            var kind = SkinIDToItemName[item.skin];

            //kill that shit, create new item
            item.RemoveFromContainer();
            item.Remove(0.0f);

            var fakeItemName = KindToFakeItemName[kind];

            var fakeItemID = ItemNameToItemID[fakeItemName];

            item = ItemManager.CreateByItemID(fakeItemID);
            item.name = name;
            item.amount = amount;
            item.skin = skin;

            //put it in the container in that slot
            item.MoveToContainer(container, slot, false);
            item.MarkDirty();

            container.MarkDirty();

            return item;
        }

        DroppedItem DummyCreate(Vector3 position, Vector3 rotation, bool movable = true)
        {
            //THANKS KARUZA!
            var newItem = ItemManager.CreateByItemID(963906841);

            var dropped = newItem.Drop(position, Vector3.zero, Quaternion.Euler(rotation));

            var worldModel = dropped.GetComponent<DroppedItem>();

            var rigid = worldModel.GetComponent<Rigidbody>();

            if (rigid != null)
            {
                if (movable)
                {
                    rigid.isKinematic = false;
                    rigid.useGravity = true;
                    rigid.drag = 2.0F;
                }
                else
                {
                    rigid.isKinematic = true;
                    rigid.useGravity = false;
                }

            }

            //worldModel.EnableGlobalBroadcast(true);
            //worldModel.syncPosition = true;
            worldModel.enableSaving = false;

            worldModel.allowPickup = true;

            // no despawn
            worldModel.Invoke("IdleDestroy", float.MaxValue);
            worldModel.CancelInvoke((Action)Delegate.CreateDelegate(typeof(Action), worldModel, "IdleDestroy"));

            return worldModel;
        }

        private BaseEntity SpawnCustomDrop(string kind, Vector3 position, Vector3 eulerAngles, ulong ownerID = 0)
        {
            string toCreate;
            if (kind != "car")
            {
                toCreate = ShortPrefabName[kind];
            }
            else
            {
                var sumWeights = configData.dropConfigs["car"].extraSettings.randomWeight2modules + configData.dropConfigs["car"].extraSettings.randomWeight3modules + configData.dropConfigs["car"].extraSettings.randomWeight4modules;

                var rnd = UnityEngine.Random.Range(0, configData.dropConfigs["car"].extraSettings.randomWeightModulesSum);
                if (rnd < configData.dropConfigs["car"].extraSettings.randomWeight2modules)
                {
                    toCreate = ShortPrefabName["2module"];
                }
                else if (rnd < configData.dropConfigs["car"].extraSettings.randomWeight2modules + configData.dropConfigs["car"].extraSettings.randomWeight3modules)
                {
                    toCreate = ShortPrefabName["3module"];
                }
                else toCreate = ShortPrefabName["4module"];
            }

            BaseEntity newDrop = GameManager.server.CreateEntity(toCreate, new Vector3(), new Quaternion());//Quaternion.Euler(eulerAngles), true);

            newDrop.syncPosition = true;
            newDrop.EnableGlobalBroadcast(true);
            newDrop.OwnerID = ownerID;
            newDrop.Spawn();

            newDrop.transform.position = position;
            newDrop.transform.eulerAngles = eulerAngles;

            newDrop.transform.hasChanged = true;

            if (kind != "crate" && kind != "car")
            {
                var veh = newDrop as BaseVehicle;

                veh.InitializeHealth(configData.dropConfigs[kind].comesWithHealth * veh.MaxHealth(), veh.MaxHealth());
            }

            newDrop.SendNetworkUpdateImmediate(true);

            newDrop.gameObject.AddComponent<CustomDrop>();
            var compo = newDrop.gameObject.GetComponent<CustomDrop>();

            //add to the cache..

            if (!EntityIDToCustomDrop.ContainsKey(newDrop.net.ID))
            {
                EntityIDToCustomDrop.Add(newDrop.net.ID, compo);
            }

            //again, this will help with fast lookup
            if (!JustMainEntityIDToCustomDrop.ContainsKey(newDrop.net.ID))
            {
                JustMainEntityIDToCustomDrop.Add(newDrop.net.ID, compo);
            }

            //and add to personal garbage
            compo.personalGarbage.Add(newDrop.net.ID);


            compo.ownerID = ownerID;
            compo.kind = kind;


            Instance.NextTick(() =>
            {
                var tryNormalSeats = newDrop.GetComponentsInChildren<BaseEntity>();

                if (tryNormalSeats != null)
                {
                    foreach (var entity in tryNormalSeats)
                    {
                        entity.OwnerID = ownerID;

                        if (!EntityIDToCustomDrop.ContainsKey(entity.net.ID))
                        {
                            EntityIDToCustomDrop.Add(entity.net.ID, compo);
                        }
                        //give the seat the same OwnerID
                        if (!compo.personalGarbage.Contains(entity.net.ID))
                        {
                            compo.personalGarbage.Add(entity.net.ID);
                        }

                    }
                }


                //add some fuel based on kind
                if (Instance.configData.dropConfigs[kind].comesWithFuel > 0)
                {
                    //this is going to be done automatically on vehicle spawn.
                    //you don't wanna do it twice.
                    //so only do this if the equip settings DON'T apply to naturally spawned vehicles.
                    if (!Instance.configData.dropConfigs[kind].applyToNatural)
                    {
                        EquipVehicle(newDrop, kind, true);
                    }
                }
            });

            return newDrop;
        }

        private void TurnSignalIntoCustom(Item item, string signalKind = null, bool lootWeights = false)
        {
            if (item == null) return;

            //if it's not a supply signal, return
            if (item.info.itemid != 1397052267) return;
            if (signalKind == "normal")
            {
                return;
            }

            if (signalKind == null)
            {
                //for now, no rands, just make it rhib
                signalKind = WeightedRandomKind(lootWeights);
            }

            if (!SignalDefinitions.definitions.ContainsKey(signalKind)) return;

            item.skin = SignalDefinitions.definitions[signalKind].skinID;
            item.name = $"{item.info.displayName.translated} ({SignalDefinitions.definitions[signalKind].itemSuffix})";

            item.MarkDirty();


            //if the kind is null, pick a random kind based on loot weights

        }

        private bool GiveSignal(BasePlayer player, string signalKind, int amount)
        {
            var success = false;

            if (player == null) return false;
            if (!player.IsConnected) return false;
            if (signalKind != "normal")
            {
                if (!SignalDefinitions.definitions.ContainsKey(signalKind)) return false;
            }

            var item = ItemManager.CreateByItemID(1397052267, amount);
            if (item == null) return false;

            TurnSignalIntoCustom(item, signalKind);

            if (!item.MoveToContainer(player.inventory.containerBelt, -1, false))
            {
                if (!item.MoveToContainer(player.inventory.containerMain, -1, false))
                {
                    Instance.TellMessage(player, MSG(MSG_NOT_ENOUGH_INVENTORY_SPACE, player.UserIDString));
                }
                else
                {
                    success = true;
                    player.inventory.SendUpdatedInventory(PlayerInventory.Type.Main, player.inventory.containerMain);
                }
            }
            else
            {
                success = true;
                player.inventory.SendUpdatedInventory(PlayerInventory.Type.Belt, player.inventory.containerBelt);
            }

            return success;
        }
        [PluginReference]
        private Plugin Notify;

        public void TellMessage(BasePlayer player, string msg)
        {
            var msgFormatted = $"<color={ColorPalette.LimeLight.hexValue}>[Vehicle Airdrops]</color> {msg}";
            if (player != null)
            {
                if (player.IsConnected)
                {
                    PlayerMessageCommon(player, msgFormatted);
                }
            }
            else
            {
                //tell all
                foreach (var playah in BasePlayer.activePlayerList)
                {
                    if (playah.IsConnected)
                    {
                        PlayerMessageCommon(playah, msgFormatted);
                    }
                }
            }
        }

        public void PlayerMessageCommon(BasePlayer playah, string msgFormatted)
        {
            Player.Message(playah, msgFormatted, null, configData.chatIcon);

            if (Instance.configData.enableNotifyPlugin)
            {
                Notify?.Call("SendNotify", playah, (int)0, msgFormatted);
            }
        }

        #endregion
    }
}
