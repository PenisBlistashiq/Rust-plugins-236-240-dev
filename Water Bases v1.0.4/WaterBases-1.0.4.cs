using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("WaterBases", "Nikedemos", VERSION)]
    [Description("Allows vanilla-like constructions on water surface - and underwater too!")]

    public class WaterBases : RustPlugin
    {
        private static WaterBases Instance;
        #region CONST & STATIC

        public const string VERSION = "1.0.4";

        public const string PREFAB_FLOOR_SQUARE = "assets/prefabs/building core/floor/floor.prefab";
        public const string PREFAB_FLOOR_TRIANGLE = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab";
        public const string PREFAB_BARREL = "assets/prefabs/resource/diesel barrel/diesel_barrel_world.prefab";

        public const string PREFAB_NETTING = "assets/prefabs/building/wall.frame.netting/wall.frame.netting.prefab";

        public const string PREFAB_BARREL_COLLECTABLE = "assets/content/structures/excavator/prefabs/diesel_collectable.prefab";

        public const string PREFAB_OILRIG_1 = "OilrigAI";
        public const string PREFAB_OILRIG_2 = "OilrigAI2";

        public const string PREFAB_POOL_BIG = "assets/prefabs/misc/summer_dlc/abovegroundpool/abovegroundpool.deployed.prefab";
        public const string PREFAB_POOL_SMALL = "assets/prefabs/misc/summer_dlc/paddling_pool/paddlingpool.deployed.prefab";

        public const string PREFAB_INNER_TUBE = "assets/prefabs/misc/summer_dlc/inner_tube/innertube.deployed.prefab";

        public const string PREFAB_BUILD_EFFECT = "assets/bundled/prefabs/fx/build/frame_place.prefab";

        public const int ITEM_INNER_TUBE = -697981032;
        public const int ITEM_BUILDING_PLANNER = 1525520776;

        public const ulong SKIN_FOUNDATION_SQUARE = 2484982352;
        public const ulong SKIN_FOUNDATION_TRIANGLE = 2485021365;

        //used to identify stuff
        public const ulong SKIN_WATER_BARREL = 1337424001;
        public const ulong SKIN_GENERIC_DECAY_ENTITY = 1337424002;

        //used to identify stuff
        public const ulong SKIN_INVERSE_FOUNDATION_SQUARE = 1337424003;
        public const ulong SKIN_INVERSE_FOUNDATION_TRIANGLE = 1337424004;
        public const ulong SKIN_INVERSE_DECAY_ENTITY = 1337424002;

        public static LayerMask DeployedMask = LayerMask.GetMask("Deployed");
        public static LayerMask ConstructionMask = LayerMask.GetMask("Construction");

        private static object ReusableObject;
        private static bool ReusableBool;
        private static string ReusableString;
        private static float ReusableFloat;

        private static bool ReusableBool2;

        private static string ReusableString2;
        private static float ReusableFloat2;
        private static int ReusableInt2;
        private static ulong ReusableUlong2;

        public static readonly List<string> FramePrefabs = new List<string>
        {
            "assets/prefabs/building core/floor.frame/floor.frame.prefab",
            "assets/prefabs/building core/floor.triangle.frame/floor.triangle.frame.prefab",
            "assets/prefabs/building core/wall.frame/wall.frame.prefab"
        };

        public static readonly List<ulong> RelevantSkins = new List<ulong>
        {
            SKIN_FOUNDATION_SQUARE,
            SKIN_FOUNDATION_TRIANGLE,
            SKIN_GENERIC_DECAY_ENTITY,
            SKIN_WATER_BARREL,
            SKIN_INVERSE_DECAY_ENTITY,
            SKIN_INVERSE_FOUNDATION_SQUARE,
            SKIN_INVERSE_FOUNDATION_TRIANGLE
        };

        public static List<Vector3> FloorBarrelPositionsSquare = new List<Vector3>
        {
            new Vector3(0F, -0.4F, 0.33F),
            new Vector3(0F, -0.4F, -0.33F),                                                        
            new Vector3(-0.33F, -0.4F, 1.12F),
            new Vector3(-0.33F, -0.4F, 0.36F),
            new Vector3(-0.33F, -0.4F, -0.36F),
            new Vector3(-0.33F, -0.4F, -1.12F),
            new Vector3(0.33F, -0.4F, 1.12F),
            new Vector3(0.33F, -0.4F, 0.36F),
            new Vector3(0.33F, -0.4F, -1.12F),
            new Vector3(0.33F, -0.4F, -0.36F)
        };

        public static List<Vector3> FloorBarrelRotationsSquare = new List<Vector3>
        {
            new Vector3(0F, 90F, 90F),
            new Vector3(0F, 90F, 270F),
            new Vector3(0F, 0F, 90F),
            new Vector3(0F, 0F, 90F),
            new Vector3(0F, 0F, 90F),
            new Vector3(0F, 0F, 90F),
            new Vector3(0F, 0F, 270F),
            new Vector3(0F, 0F, 270F),
            new Vector3(0F, 0F, 270F),
            new Vector3(0F, 0F, 270F)
        };

        public static List<Vector3> FloorBarrelPositionsTriangle= new List<Vector3>
        {
            new Vector3(0F, -0.4F, -0.6F+1.5F),
            new Vector3(0.42F, -0.4F, -0.42F+1.5F),
            new Vector3(-0.42F, -0.4F, -0.42F+1.5F),
            new Vector3(0F, -0.4F, -1.17F+1.5F),
            new Vector3(0F, -0.4F, -1.17F+1.5F)
        };

        public static List<Vector3> FloorBarrelRotationsTriangle = new List<Vector3>
        {
            new Vector3(0F, 90F, 90F),
            new Vector3(0F, 0F, 90F),
            new Vector3(0F, 180F, 90F),
            new Vector3(0F, 180F, 90F),
            new Vector3(0F, 0F, 90F)
        };
        #endregion

        #region LANG
        public const string MSG_ITEM_NAME_TRIANGLE = "MSG_ITEM_NAME_TRIANGLE";
        public const string MSG_ITEM_NAME_SQUARE = "MSG_ITEM_NAME_SQUARE";

        public const string MSG_CHAT_PREFIX = "MSG_CHAT_PREFIX";
        public const string MSG_DEPLOY_ERROR = "MSG_DEPLOY_ERROR";
        public const string MSG_CRAFT = "MSG_CRAFT";

        public const string MSG_DEPLOY_RESULT_BUILDING_BLOCKED = "MSG_DEPLOY_RESULT_BUILDING_BLOCKED";
        public const string MSG_DEPLOY_RESULT_TOPOLOGY_MISMATCH = "MSG_DEPLOY_RESULT_TOPOLOGY_MISMATCH";
        public const string MSG_DEPLOY_RESULT_NO_PERM_DEPLOY = "MSG_DEPLOY_RESULT_NO_PERM_DEPLOY";
        public const string MSG_DEPLOY_RESULT_NO_PERM_EXPAND = "MSG_DEPLOY_RESULT_NO_PERM_EXPAND";
        public const string MSG_DEPLOY_RESULT_TOO_SHALLOW = "MSG_DEPLOY_RESULT_TOO_SHALLOW";
        public const string MSG_DEPLOY_RESULT_TOO_DEEP = "MSG_DEPLOY_RESULT_TOO_DEEP";
        public const string MSG_DEPLOY_RESULT_OUTSIDE_GRID = "MSG_DEPLOY_RESULT_OUTSIDE_GRID";
        public const string MSG_DEPLOY_RESULT_TOO_CLOSE_TO_OILRIG = "MSG_DEPLOY_RESULT_TOO_CLOSE_TO_OILRIG";
        public const string MSG_DEPLOY_RESULT_TOO_CLOSE_TO_SHORE = "MSG_DEPLOY_RESULT_TOO_CLOSE_TO_SHORE";
        public const string MSG_DEPLOY_RESULT_TOO_FAR_FROM_SHORE = "MSG_DEPLOY_RESULT_TOO_FAR_FROM_SHORE";
        public const string MSG_DEPLOY_RESULT_TOO_MANY_FOUNDATIONS = "MSG_DEPLOY_RESULT_TOO_MANY_FOUNDATIONS";
        public const string MSG_DEPLOY_RESULT_IN_POOL = "MSG_DEPLOY_RESULT_IN_POOL";
        public const string MSG_DEPLOY_RESULT_LACKS_ITEM = "MSG_DEPLOY_RESULT_LACKS_ITEM";

        public const string MSG_EXPAND_REINFORCEMENT_NOT_ALLOWED = "MSG_EXPAND_REINFORCEMENT_NOT_ALLOWED";
        public const string MSG_REINFORCEMENT_NOT_ALLOWED = "MSG_REINFORCEMENT_NOT_ALLOWED";
        public const string MSG_REINFORCEMENT_LACKS_ITEM = "MSG_REINFORCEMENT_LACKS_ITEM";
        public const string MSG_EXPAND_REINFORCEMENT_WRONG_ABOVE = "MSG_EXPAND_REINFORCEMENT_WRONG_ABOVE";
        public const string MSG_EXPAND_REINFORCEMENT_LACKS_ITEM = "MSG_EXPAND_REINFORCEMENT_LACKS_ITEM";

        public const string MSG_UNDERWATER_NETTING_NO_PERM = "MSG_UNDERWATER_NETTING_NO_PERM";
        public const string MSG_CRAFTING_NO_PERM = "MSG_CRAFTING_NO_PERM";
        public const string MSG_CRAFTING_NO_MATERIALS = "MSG_CRAFTING_NO_MATERIALS";
        public const string MSG_CRAFTING_INSUFFICIENT_WB_LEVEL = "MSG_CRAFTING_INSUFFICIENT_WB_LEVEL";

        public const string MSG_MAX_TIER_REACHED_MAIN = "MSG_MAX_TIER_REACHED_MAIN";
        public const string MSG_MAX_TIER_REACHED_FRAME = "MSG_MAX_TIER_REACHED_FRAME";
        public const string MSG_MAX_TIER_REACHED_FOUNDATION = "MSG_MAX_TIER_REACHED_FOUNDATION";

        public const string MSG_CARGO_PATH_WARNING = "MSG_CARGO_PATH_WARNING";
        public const string MSG_CARGO_PATH_PREVENTION = "MSG_CARGO_PATH_PREVENTION";

        public const string MSG_VALUE_HAS_BEEN_SET = "MSG_VALUE_HAS_BEEN_SET";
        public const string MSG_CFG_RUNDOWN_FORMAT = "MSG_CFG_RUNDOWN_FORMAT";
        public const string MSG_CFG_DETAILS_FORMAT = "MSG_CFG_DETAILS_FORMAT";
        public const string MSG_CFG_NO_SETTING_FOUND = "MSG_CFG_NO_SETTING_FOUND";
        public const string MSG_CFG_DEFAULT = "MSG_CFG_DEFAULT";

        private Dictionary<string, string> LangMessages = new Dictionary<string, string>
        {
            [MSG_CHAT_PREFIX] = "<color=#4897ce>[Water Bases]</color>",
            [MSG_DEPLOY_ERROR] = "ERROR:",
            [MSG_ITEM_NAME_SQUARE] = "Water Foundation (SQUARE)",
            [MSG_ITEM_NAME_TRIANGLE] = "Water Foundation (TRIANGLE)",
            [MSG_CRAFT] = "Craft",

            [MSG_DEPLOY_RESULT_BUILDING_BLOCKED] = "You're building blocked.",
            [MSG_DEPLOY_RESULT_TOPOLOGY_MISMATCH] = "You're not allowed to build on this topology.",
            [MSG_DEPLOY_RESULT_NO_PERM_DEPLOY] = "You're not allowed to deploy new water foundations.",
            [MSG_DEPLOY_RESULT_NO_PERM_EXPAND] = "You're not allowed to expand existing water foundations.",
            [MSG_DEPLOY_RESULT_TOO_SHALLOW] = "You're not allowed to build in water this shallow.",
            [MSG_DEPLOY_RESULT_TOO_DEEP] = "You're not allowed to build in water this deep.",
            [MSG_DEPLOY_RESULT_OUTSIDE_GRID] = "You're not allowed to build outside of the map grid.",
            [MSG_DEPLOY_RESULT_TOO_CLOSE_TO_OILRIG] = "You're not allowed to build this close to the Oil Rig.",
            [MSG_DEPLOY_RESULT_TOO_CLOSE_TO_SHORE] = "You're not allowed to build this close to shore.",
            [MSG_DEPLOY_RESULT_TOO_FAR_FROM_SHORE] = "You're not allowed to build this far from shore.",
            [MSG_DEPLOY_RESULT_TOO_MANY_FOUNDATIONS] = "You're not allowed this many water foundations per building.",
            [MSG_DEPLOY_RESULT_IN_POOL] = "Nice try, but building in swimming pools would be a little bit much.",

            [MSG_MAX_TIER_REACHED_MAIN] = "You're not allowed to upgrade walls/floors/windows/doorways to tier this high.",
            [MSG_MAX_TIER_REACHED_FRAME] = "You're not allowed to upgrade floor frames/wall frames to tier this high.",
            [MSG_MAX_TIER_REACHED_FOUNDATION] = "You're not allowed to upgrade water foundations to tier this high.",

            [MSG_CARGO_PATH_PREVENTION] = "You're not allowed to build this close to the path of the Cargo Ship.",
            [MSG_CARGO_PATH_WARNING] = "You're building in the path of the Cargo Ship. Your structure is at risk.",

            [MSG_EXPAND_REINFORCEMENT_NOT_ALLOWED] = "You're not allowed to expand reinforcements like this. Use a hammer on the fully repaired Water Foundation instead.",
            [MSG_REINFORCEMENT_LACKS_ITEM] = "You need a proper Water Foundation item in your inventory to reinforce this water foundation.",
            [MSG_EXPAND_REINFORCEMENT_LACKS_ITEM] = "You need 2 proper Water Foundation items in your inventory to expand this reinforced water foundation.",
            [MSG_REINFORCEMENT_NOT_ALLOWED] = "You're not allowed to reinforce Water Foundations.",
            [MSG_UNDERWATER_NETTING_NO_PERM] = "You're not allowed to deploy underwater netting.",
            [MSG_CRAFTING_NO_MATERIALS] = "You need {0} to craft a {1}.",
            [MSG_CRAFTING_NO_PERM] = "You're not allowed to craft Water Foundations.",
            [MSG_CRAFTING_INSUFFICIENT_WB_LEVEL] = "You need a Level {0} Workbench to craft Water Foundations.",
            [MSG_DEPLOY_RESULT_LACKS_ITEM] = "You need a proper Water Foundation item in your inventory to expand this water foundation.",
            [MSG_EXPAND_REINFORCEMENT_WRONG_ABOVE] = "You can't expand like that. The shapes of the reinforcement and the water foundation above it must match.",

            [MSG_VALUE_HAS_BEEN_SET] = "Config value <color=yellow>{0}</color> has been set to {1} by an admin.",
            [MSG_CFG_DEFAULT] = "Here's the current settings. Type <color=yellow>/wb_cfg settingName</color> with no parameters to see the description, the current value and what the accepted arguments are. Type <color=yellow>/wb_cfg settingName acceptedValue</color> to change the setting.",
            [MSG_CFG_RUNDOWN_FORMAT] = "/wb_cfg <color=yellow>{0}</color> (currently: {1})",
            [MSG_CFG_DETAILS_FORMAT] = "<color=green>{0}</color>:\n{1} ({2})\nThis value is currently set to: {3}\n",
            [MSG_CFG_NO_SETTING_FOUND] = "No setting with that name has been found. Type /wb_cfg to get a rundown.",
        };

        private static string MSG(string msg, string userID = null, params object[] args)
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

        private static string MSG_RESULT(DeploymentResult result, string userID = null)
        {
            switch (result)
            {
                default:
                case DeploymentResult.Success:
                    {
                        return null;
                    }
                case DeploymentResult.BuildingBlocked:
                    {
                        return $"{MSG(MSG_DEPLOY_ERROR, userID)} {MSG(MSG_DEPLOY_RESULT_BUILDING_BLOCKED, userID)}";
                    }
                case DeploymentResult.TopologyMismatch:
                    {
                        return $"{MSG(MSG_DEPLOY_ERROR, userID)} {MSG(MSG_DEPLOY_RESULT_TOPOLOGY_MISMATCH, userID)}";
                    }
                case DeploymentResult.NoPermissionToDeploy:
                    {
                        return $"{MSG(MSG_DEPLOY_ERROR, userID)} {MSG(MSG_DEPLOY_RESULT_NO_PERM_DEPLOY, userID)}";
                    }
                case DeploymentResult.NoPermissionToExpand:
                    {
                        return $"{MSG(MSG_DEPLOY_ERROR, userID)} {MSG(MSG_DEPLOY_RESULT_NO_PERM_EXPAND, userID)}";
                    }
                case DeploymentResult.WaterTooShallow:
                    {
                        return $"{MSG(MSG_DEPLOY_ERROR, userID)} {MSG(MSG_DEPLOY_RESULT_TOO_SHALLOW, userID)}";
                    }
                case DeploymentResult.WaterTooDeep:
                    {
                        return $"{MSG(MSG_DEPLOY_ERROR, userID)} {MSG(MSG_DEPLOY_RESULT_TOO_DEEP, userID)}";
                    }
                case DeploymentResult.DeployedInPool:
                    {
                        return $"{MSG(MSG_DEPLOY_ERROR, userID)} {MSG(MSG_DEPLOY_RESULT_IN_POOL, userID)}";
                    }
                case DeploymentResult.TooCloseToOilrig:
                    {
                        return $"{MSG(MSG_DEPLOY_ERROR, userID)} {MSG(MSG_DEPLOY_RESULT_TOO_CLOSE_TO_OILRIG, userID)}";
                    }
                case DeploymentResult.TooCloseToShore:
                    {
                        return $"{MSG(MSG_DEPLOY_ERROR, userID)} {MSG(MSG_DEPLOY_RESULT_TOO_CLOSE_TO_SHORE, userID)}";
                    }
                case DeploymentResult.TooFarFromShore:
                    {
                        return $"{MSG(MSG_DEPLOY_ERROR, userID)} {MSG(MSG_DEPLOY_RESULT_TOO_FAR_FROM_SHORE, userID)}";
                    }
                case DeploymentResult.TooManyFoundations:
                    {
                        return $"{MSG(MSG_DEPLOY_ERROR, userID)} {MSG(MSG_DEPLOY_RESULT_TOO_MANY_FOUNDATIONS, userID)}";
                    }
                case DeploymentResult.OnCargoShipPath:
                    {
                        return $"{MSG(MSG_DEPLOY_ERROR, userID)} {MSG(Instance.configData.CargoShipPathHandling == CargoPathHandling.Prevent ? MSG_CARGO_PATH_PREVENTION : MSG_CARGO_PATH_WARNING, userID)}";
                    }
                case DeploymentResult.NoRequiredItem:
                    {
                        return $"{MSG(MSG_DEPLOY_ERROR, userID)} {MSG(MSG_DEPLOY_RESULT_LACKS_ITEM, userID)}";
                    }
                case DeploymentResult.OutsideMapGrid:
                    {
                        return $"{MSG(MSG_DEPLOY_ERROR, userID)} {MSG(MSG_DEPLOY_RESULT_OUTSIDE_GRID, userID)}";
                    }
            }
        }
        #endregion

        #region PERMISSIONS
        public const string PERM_ADMIN = "waterbases.admin";
        public const string PERM_VIP1 = "waterbases.vip1";
        public const string PERM_VIP2 = "waterbases.vip2";
        public const string PERM_VIP3 = "waterbases.vip3";
        public const string PERM_VIP4 = "waterbases.vip4";
        public const string PERM_VIP5 = "waterbases.vip5";
        #endregion

        #region HOOKS
        void OnServerInitialized()
        {
            Instance = this;

            lang.RegisterMessages(LangMessages, this);

            permission.RegisterPermission(PERM_ADMIN, this);
            permission.RegisterPermission(PERM_VIP1, this);
            permission.RegisterPermission(PERM_VIP2, this);
            permission.RegisterPermission(PERM_VIP3, this);
            permission.RegisterPermission(PERM_VIP4, this);
            permission.RegisterPermission(PERM_VIP5, this);

            LoadConfigData();
            ProcessConfigData();

            ConfigValues = new Dictionary<string, InteractiveConfigValue>();

            GenerateAllInteractiveConfigValues();

            RecalculateUnderwaterLootWeights();

            GuiManager.GenerateGUI();

            ActivateSpecialStuff();
        }

        void Unload()
        {
            Instance = null;

            foreach (var helper in UnityEngine.Object.FindObjectsOfType<InverseFoundationHelper>())
            {
                UnityEngine.Object.DestroyImmediate(helper);
            }

            foreach (var helper in UnityEngine.Object.FindObjectsOfType<UnderwaterNetHelper>())
            {
                UnityEngine.Object.DestroyImmediate(helper);
            }

            foreach (var player in BasePlayer.activePlayerList)
            {
                GuiManager.HideGUI(player);
            }
        }

        void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            GuiManager.HideGUI(player);
        }

        void OnItemRemovedFromContainer(ItemContainer container, Item item)
        {
            if (item.info.itemid != ITEM_BUILDING_PLANNER)
            {
                return;
            }

            var player = container.GetOwnerPlayer();

            if (player == null)
            {
                return;
            }

            if (player.svActiveItemID == item.uid)
            {
                GuiManager.HideGUI(player);
            }
        }

        void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (newItem != null)
            {
                if (newItem.info.itemid == ITEM_BUILDING_PLANNER)
                {
                    if (oldItem?.info.itemid != ITEM_BUILDING_PLANNER || oldItem == null)
                    {
                        GuiManager.ShowGUI(player);
                    }
                }
                else
                {
                    if (oldItem != null)
                    {
                        if (oldItem.info.itemid == ITEM_BUILDING_PLANNER)
                        {
                            GuiManager.HideGUI(player);
                        }
                    }
                }
            }
            else
            {
                if (oldItem != null)
                {
                    if (oldItem.info.itemid == ITEM_BUILDING_PLANNER)
                    {
                        GuiManager.HideGUI(player);
                    }
                }
            }


        }
        object OnRecycleItem(Recycler recycler, Item item)
        {
            if (!IsSkinRelevant(item.skin))
            {
                return null;
            }

            List<SerializableItemAmount> costList;

            if (item.skin == SKIN_FOUNDATION_SQUARE)
            {
                costList = Instance.configData.CraftingCostSquare;
            }
            else
            {
                costList = Instance.configData.CraftingCostTriangle;
            }

            foreach (var cost in costList)
            {
                recycler.MoveItemToOutput(ItemManager.CreateByName(cost.Shortname, Mathf.CeilToInt((float)cost.Amount/2F)));
            }

            item.UseItem(1);
            item.MarkDirty();

            return true;

        }

        object OnStructureUpgrade(BuildingBlock block, BasePlayer player, BuildingGrade.Enum grade)
        {
            if (IsInWaterBase(block))
            {
                var prof = GetPermissionProfile(player);

                bool isFrame = false;
                bool isWaterFoundation = false;

                var maxTier = prof.MaxBuildingGradeGeneric;

                if (IsWaterFoundation(block))
                {
                    maxTier = prof.MaxBuildingGradeWaterFoundations;
                    isWaterFoundation = true;
                }
                else
                {
                    if (FramePrefabs.Contains(block.PrefabName))
                    {
                        maxTier = prof.MaxBuildingGradeFrames;
                        isFrame = true;
                    }
                }

                //check if the grade has not exceeded config max
                if ((int)grade > (int)maxTier)
                {
                    TellMessage(player, MSG(isWaterFoundation ? MSG_MAX_TIER_REACHED_FOUNDATION : isFrame ? MSG_MAX_TIER_REACHED_FRAME : MSG_MAX_TIER_REACHED_MAIN, player.UserIDString));
                    return true;
                }

            }

            return null;
        }

        object OnHammerHit(BasePlayer player, HitInfo info)
        {
            if (player.IsBuildingBlocked()) return null;

            //see if the player has enough resources to create inverse...

            if (info.HitEntity == null) return null;

            var buildingBlock = info.HitEntity as BuildingBlock;

            if (buildingBlock == null)
            {
                return null;
            }

            if (buildingBlock.health != buildingBlock.MaxHealth())
            {
                return null;
            }

            if (!IsWaterFoundation(buildingBlock))
            {
                return null;
            }

            var allChildren = buildingBlock.GetComponentsInChildren<BuildingBlock>();

            BuildingBlock inverseFloorAlready = null;

            foreach (var block in allChildren)
            {
                if (block.net.ID == buildingBlock.net.ID) continue;

                inverseFloorAlready = block;
                break;
            }

            if (inverseFloorAlready == null)
            {
                var needed = FindNeededFoundationItemsInInventory(player, buildingBlock.PrefabName == PREFAB_FLOOR_SQUARE ? SKIN_FOUNDATION_SQUARE : SKIN_FOUNDATION_TRIANGLE);

                if (needed == null)
                {
                    TellMessage(player, $"{MSG(MSG_DEPLOY_ERROR)} {MSG(MSG_REINFORCEMENT_LACKS_ITEM, player.UserIDString)}");
                    return true;
                }

                ConsumeFromSpecificItemList(ref needed);

                BuildInverseFoundationAtWaterFoundation(player, buildingBlock);
                return true;
            }

            return null;
        }
        void OnEntityBuilt(Planner planner, GameObject gameObject)
        {
            var maybeInflatable = gameObject.GetComponent<WaterInflatable>();

            //normal building, not from inflatable
            if (maybeInflatable == null)
            {
                var maybeDecayEntity = gameObject.GetComponent<DecayEntity>();

                if (maybeDecayEntity == null)
                {
                    return;
                }

                var newBuildingBlock = gameObject.GetComponent<BuildingBlock>();


                var player = planner.GetOwnerPlayer();

                if (newBuildingBlock != null)
                {
                    if (IsInWaterBase(newBuildingBlock))
                    {
                        //should this BECOME a water foundation? or an inverse foundation?
                        if (newBuildingBlock.PrefabName == PREFAB_FLOOR_SQUARE || newBuildingBlock.PrefabName == PREFAB_FLOOR_TRIANGLE)
                        {
                            bool isInverse;
                            var nearby = TryGetAdjacentSpecialFoundation(newBuildingBlock, out isInverse);

                            if (nearby == null)
                            {
                                //for now, maybe let's leave it as generic?
                                newBuildingBlock.skinID = Mathf.Abs(newBuildingBlock.transform.localEulerAngles.z)<0.1F ? SKIN_GENERIC_DECAY_ENTITY : SKIN_INVERSE_DECAY_ENTITY;
                                return;
                            }

                            if (!isInverse)
                            {
                                if (newBuildingBlock.PrefabName == PREFAB_FLOOR_SQUARE)
                                {
                                    newBuildingBlock.skinID = SKIN_FOUNDATION_SQUARE;
                                }
                                else
                                {
                                    newBuildingBlock.skinID = SKIN_FOUNDATION_TRIANGLE;
                                }

                                TurnFloorIntoWaterFoundation(newBuildingBlock, player, false);
                                return;
                            }
                            else
                            {
                                if (newBuildingBlock.PrefabName == PREFAB_FLOOR_SQUARE)
                                {
                                    newBuildingBlock.skinID = SKIN_INVERSE_FOUNDATION_SQUARE;
                                }
                                else
                                {
                                    newBuildingBlock.skinID = SKIN_INVERSE_FOUNDATION_TRIANGLE;
                                }

                                var maybeAboveYou = TryFindWaterFoundationAboveYou(newBuildingBlock);

                                if (maybeAboveYou == null)
                                {
                                    var prof = GetPermissionProfile(player);
                                    //first and foremost: how many water foundation does the building have?
                                    //if you don't do this check, you're gonna have a REALLY bad time

                                    var waterFoundations = CountBuildingWaterFoundations(newBuildingBlock);

                                    if (waterFoundations +1 >= prof.MaxWaterFoundationsPerBuilding)
                                    {
                                        TellMessage(player, $"{MSG(MSG_DEPLOY_ERROR, player.UserIDString)} {MSG(MSG_DEPLOY_RESULT_TOO_MANY_FOUNDATIONS, player.UserIDString)}");
                                        newBuildingBlock.Kill(BaseNetworkable.DestroyMode.Gib);
                                        return;
                                    }

                                    if (!prof.CanExpandWaterFoundations)
                                    {
                                        TellMessage(player, $"{MSG(MSG_DEPLOY_ERROR, player.UserIDString)} {MSG(MSG_EXPAND_REINFORCEMENT_NOT_ALLOWED, player.UserIDString)}");
                                        newBuildingBlock.Kill(BaseNetworkable.DestroyMode.Gib);
                                        return;
                                    }

                                    //do you have enough of needed kind? you need two!

                                    var neededItems = FindNeededFoundationItemsInInventory(player, newBuildingBlock.PrefabName == PREFAB_FLOOR_SQUARE ? SKIN_FOUNDATION_SQUARE : SKIN_FOUNDATION_TRIANGLE, 2);

                                    if (neededItems == null)
                                    {
                                        TellMessage(player, $"{MSG(MSG_DEPLOY_ERROR, player.UserIDString)} {MSG(MSG_EXPAND_REINFORCEMENT_LACKS_ITEM, player.UserIDString)}");
                                        newBuildingBlock.Kill(BaseNetworkable.DestroyMode.Gib);
                                        return;
                                    }

                                    //consume the item
                                    ConsumeFromSpecificItemList(ref neededItems);

                                    //let's make a foundation first. relative please.
                                    var transf = GetWorldTransformRelativeTo(newBuildingBlock.transform, Vector3.down * 1.5F, new Vector3(0F, 0F, -180F));

                                    maybeAboveYou = BuildWaterFoundationAt(player, newBuildingBlock.PrefabName == PREFAB_FLOOR_SQUARE ? SKIN_FOUNDATION_SQUARE : SKIN_FOUNDATION_TRIANGLE, transf.Item1, transf.Item2);

                                    maybeAboveYou.AttachToBuilding(nearby);

                                    newBuildingBlock.skinID = newBuildingBlock.PrefabName == PREFAB_FLOOR_SQUARE ? SKIN_INVERSE_FOUNDATION_SQUARE : SKIN_INVERSE_FOUNDATION_TRIANGLE;
                                    newBuildingBlock.AttachToBuilding(nearby);

                                }
                                else
                                {
                                    //is the foundation above you the same shape as you?
                                    if (!((maybeAboveYou.skinID == SKIN_FOUNDATION_SQUARE && newBuildingBlock.skinID == SKIN_INVERSE_FOUNDATION_SQUARE) || (maybeAboveYou.skinID == SKIN_FOUNDATION_TRIANGLE && newBuildingBlock.skinID == SKIN_INVERSE_FOUNDATION_TRIANGLE)))
                                    {
                                        TellMessage(player, $"{MSG(MSG_EXPAND_REINFORCEMENT_WRONG_ABOVE, player.UserIDString)}");
                                        newBuildingBlock.Kill(BaseNetworkable.DestroyMode.Gib);
                                        return;
                                    }

                                    //do you have enough of needed kind?

                                    var neededItems = FindNeededFoundationItemsInInventory(player, newBuildingBlock.PrefabName == PREFAB_FLOOR_SQUARE ? SKIN_FOUNDATION_SQUARE : SKIN_FOUNDATION_TRIANGLE);

                                    if (neededItems == null)
                                    {
                                        TellMessage(player, $"{MSG(MSG_DEPLOY_ERROR, player.UserIDString)} {MSG(MSG_EXPAND_REINFORCEMENT_LACKS_ITEM, player.UserIDString)}");
                                        newBuildingBlock.Kill(BaseNetworkable.DestroyMode.Gib);
                                        return;
                                    }

                                    //consume the item
                                    ConsumeFromSpecificItemList(ref neededItems);
                                }

                                TurnFloorIntoInverseFoundation(newBuildingBlock, maybeAboveYou, player);
                                return;

                            }


                        }
                        else
                        {
                            //give it generic skin - but still, inverse or not?
                            //for now, maybe let's leave it as generic?
                            newBuildingBlock.skinID = Mathf.Abs(newBuildingBlock.transform.localEulerAngles.z) < 0.1F ? SKIN_GENERIC_DECAY_ENTITY : SKIN_INVERSE_DECAY_ENTITY;
                            //still a building block
                            ApplyUpkeepMultiplier(newBuildingBlock);
                        }
                    }

                }
                else
                {

                    //not a building block, so it must be a regular decay entity
                    if (IsInWaterBase(maybeDecayEntity))
                    {
                        var maybeStability = maybeDecayEntity as StabilityEntity;

                        if (maybeStability != null)
                        {
                            if (IsUnderwaterNetting(maybeStability, true))
                            {
                                //maybeStability.skinID = SKIN_INVERSE_DECAY_ENTITY;
                                if (Instance.configData.UnderwaterNetsCollectJunk)
                                {
                                    var prof = GetPermissionProfile(player);
                                    if (prof.CanDeployUnderwaterNets)
                                    {
                                        MonoiseUnderwaterNetting(maybeStability);
                                    }
                                    else
                                    {
                                        TellMessage(player, $"{MSG(MSG_DEPLOY_ERROR, player.UserIDString)} {MSG(MSG_UNDERWATER_NETTING_NO_PERM, player.UserIDString)}");
                                        maybeStability.Kill(BaseNetworkable.DestroyMode.Gib);
                                        return;
                                    }
                                }
                            }
                        }
                        ApplyUpkeepMultiplier(maybeDecayEntity);
                    }
                }


            }
            else //from inflatable
            {
                if (!IsSkinRelevant(maybeInflatable.skinID))
                {
                    return;
                }

                var player = planner.GetOwnerPlayer();

                Instance.NextTick(() =>
                {
                    BuildWaterFoundationAt(player, maybeInflatable.skinID, maybeInflatable.transform.position, Quaternion.Euler(maybeInflatable.transform.eulerAngles.WithX(0).WithZ(0)));

                    maybeInflatable.Kill(BaseNetworkable.DestroyMode.None);


                });
            }

        }

        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (Instance == null) return;
            if (container == null) return;
            if (item == null) return;
            if (item.info.itemid != ITEM_INNER_TUBE) return;

            if (!IsSkinRelevant(item.skin)) return;

            EnsureFoundationItemName(item);
        }
        #endregion

        #region MONO

        public class UnderwaterNetHelper : MonoBehaviour
        {
            public StabilityEntity netting;

            public float NextUpdateAt = NextRandomTime();

            public void Prepare(StabilityEntity netting)
            {
                this.netting = netting;
            }

            private void FixedUpdate()
            {
                if (UnityEngine.Time.realtimeSinceStartup < NextUpdateAt)
                {
                    return;
                }

                NextUpdateAt = NextRandomTime();

                if (netting.gameObject.GetComponentsInChildren<DroppedItem>().Length >= Instance.configData.UnderwaterNetsItemLimit)
                {
                    return;
                }

                CreateRandomUnderwaterJunk(netting);
            }

            public static float NextRandomTime()
            {
                return UnityEngine.Time.realtimeSinceStartup + UnityEngine.Random.Range(Instance.configData.UnderwaterNetsRandomTimerMin, Instance.configData.UnderwaterNetsRandomTimerMin);
            }
        }

        public class InverseFoundationHelper : MonoBehaviour
        {
            public BuildingBlock inverseFoundation;
            public BuildingBlock waterFoundationParent;

            public static float UPDATE_RATE_MIN = 3F;
            public static float UPDATE_RATE_MAX = 5F;

            public float UpdateRateCurrent = RandomUpdateRate();
            public float UpdatedLast = float.MinValue;

            public BuildingManager.Building myBuilding;
            public BuildingManager.Building otherBuilding;

            public void Prepare(BuildingBlock inverseFoundation)
            {
                this.inverseFoundation = inverseFoundation;
                this.waterFoundationParent = inverseFoundation.GetParentEntity() as BuildingBlock;
            }

            private void FixedUpdate()
            {
                if (UnityEngine.Time.realtimeSinceStartup < UpdatedLast + UpdateRateCurrent)
                {
                    return;
                }

                UpdatedLast = UnityEngine.Time.realtimeSinceStartup;


                if (inverseFoundation == null) return;
                if (waterFoundationParent == null) return;

                if (inverseFoundation.buildingID != waterFoundationParent.buildingID)
                {
                    myBuilding = inverseFoundation.GetBuilding();

                    if (myBuilding == null) return;

                    otherBuilding = waterFoundationParent.GetBuilding();

                    if (otherBuilding == null) return;

                    while (myBuilding.HasDecayEntities())
                    {
                        myBuilding.decayEntities[0].AttachToBuilding(otherBuilding.ID);
                    }

                    if (ConVar.AI.nav_carve_use_building_optimization)
                    {
                        myBuilding.isNavMeshCarvingDirty = true;
                        otherBuilding.isNavMeshCarvingDirty = true;
                        int ticks = 3;
                        BuildingManager.server.UpdateNavMeshCarver(myBuilding, ref ticks, 0);
                        BuildingManager.server.UpdateNavMeshCarver(otherBuilding, ref ticks, 0);
                    }
                    UpdateRateCurrent = RandomUpdateRate();
                }
            }

            public static float RandomUpdateRate()
            {
                return UnityEngine.Random.Range(UPDATE_RATE_MIN, UPDATE_RATE_MAX);
            }
        }
        #endregion

        #region GUI
        [ConsoleCommand("wb_craft.square")]
        private void cmdConsoleWBCraftSquare(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();

            if (player == null)
            {
                return;
            }

            TryCraftingWaterFoundation(player, SKIN_FOUNDATION_SQUARE);
        }
        [ConsoleCommand("wb_craft.triangle")]
        private void cmdConsoleWBCraftTriangle(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();

            if (player == null)
            {
                return;
            }

            TryCraftingWaterFoundation(player, SKIN_FOUNDATION_TRIANGLE);
        }

        public class ColorCode
        {
            public string hexValue;
            public UnityEngine.Color rustValue;
            public string rustString;
            public ColorCode(string hex)
            {
                hex = hex.ToUpper();

                hexValue = "#" + hex;

                //extract the R, G, B
                var r = (float)short.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber) / 255;
                var g = (float)short.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber) / 255;
                var b = (float)short.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber) / 255;

                rustValue = new UnityEngine.Color(r, g, b);
                rustString = $"{r} {g} {b}";
            }
        }

        public static class GuiManager
        {
            public static CuiElementContainer ContainerMain;
            public static string ContainerMainJson;

            public static void GenerateGUI()
            {
                ContainerMain = new CuiElementContainer();

                ContainerMain.Add(new CuiPanel
                {
                    RectTransform =
                    {
                        AnchorMin = $"{Instance.configData.GuiAnchorALeft} {Instance.configData.GuiAnchorBBottom}",
                        AnchorMax = $"{Instance.configData.GuiAnchorCRight} {Instance.configData.GuiAnchorDTop}",
                    }
                }, "Overlay", "wbgui.panel");

                ContainerMain.Add(new CuiButton
                {
                    Button = 
                    {
                        Color = $"{new ColorCode(Instance.configData.GuiButtonColor).rustString} {Instance.configData.GuiButtonAlpha}",
                        Command = "wb_craft.square",
                    },
                    Text =
                    {
                        FontSize = Instance.configData.GuiTextSize,
                        Color = $"{new ColorCode(Instance.configData.GuiTextColor).rustString} {Instance.configData.GuiTextAlpha}",
                        Align = TextAnchor.MiddleCenter,
                        Text = $"{MSG(MSG_CRAFT)} {MSG(MSG_ITEM_NAME_SQUARE)}",
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 0.5"
                    }

                }, "wbgui.panel", "wbgui.button.craft.square");

                ContainerMain.Add(new CuiButton
                {
                    Button =
                    {
                        Color = new ColorCode(Instance.configData.GuiButtonColor).rustString,
                        Command = "wb_craft.triangle",
                    },
                    Text =
                    {
                        FontSize = Instance.configData.GuiTextSize,
                        Color = new ColorCode(Instance.configData.GuiTextColor).rustString,
                        Align = TextAnchor.MiddleCenter,
                        Text = $"{MSG(MSG_CRAFT)} {MSG(MSG_ITEM_NAME_TRIANGLE)}"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0.5",
                        AnchorMax = "1 1"
                    }

                }, "wbgui.panel", "wbgui.button.craft.triangle");

                ContainerMainJson = ContainerMain.ToJson();
            }

            public static void ShowGUI(BasePlayer player)
            {
                HideGUI(player);

                if (!GetPermissionProfile(player).CanCraftWaterFoundations)
                {
                    return;
                }

                CuiHelper.AddUi(player, ContainerMainJson);
            }

            public static void HideGUI(BasePlayer player)
            {
                CuiHelper.DestroyUi(player, "wbgui.panel");
            }
        }
        #endregion

        #region CHAT & CONSOLE
        [ChatCommand("copypaste_prepare")]
        private void ChatCommandCopypastePrepare(BasePlayer player, string command, string[] args)
        {
            if (!HasAdminPermission(player)) return;

            var tc = player.GetBuildingPrivilege();

            if (tc == null)
            {
                player.ChatMessage("You're not near a TC.");
                return;
            }

            if (!IsInWaterBase(tc))
            {
                player.ChatMessage("This is not a Water Base.");
                return;
            }

            foreach (var ent in tc.GetBuilding().buildingBlocks)
            {
                if (!IsWaterFoundation(ent) && !IsInverseFoundation(ent))
                {
                    continue;
                }

                var bebes = ent.GetComponentsInChildren<CollectibleEntity>();

                foreach (var bebe in bebes)
                {
                    bebe.Kill(BaseNetworkable.DestroyMode.None);
                }

            }

            player.ChatMessage("Killed all barrels. Try copying now.");
        }

        [ChatCommand("wb_cfg")]
        private void ChatCommandWBCFG(BasePlayer player, string command, string[] args)
        {
            if (!HasAdminPermission(player)) return;

            if (args.Length == 0)
            {
                //display all possible config keys and their values
                ReusableString = $"{MSG(MSG_CFG_DEFAULT, player.UserIDString)}\n";
                foreach (var entry in ConfigValues)
                {
                    ReusableString += $"{MSG(MSG_CFG_RUNDOWN_FORMAT, player.UserIDString, entry.Key, entry.Value.formatter(entry.Value.GetSet))}\n";
                }

                TellMessage(player, ReusableString);
            }
            else
            {

                if (ConfigValues.ContainsKey(args[0]))
                {
                    //has argument[1] been provided? if not, display the full description.
                    if (args.Length > 1)
                    {
                        ConfigValues[args[0]].GetSet = args[1];
                    }
                    else
                    {
                        TellMessage(player, $"{MSG(MSG_CFG_DETAILS_FORMAT, player.UserIDString, ConfigValues[args[0]].name, ConfigValues[args[0]].description, ConfigValues[args[0]].valueType, ConfigValues[args[0]].formatter(ConfigValues[args[0]].GetSet))}. Accepted values are {ConfigValues[args[0]].FormatAcceptable()}");
                    }

                }
                else
                {
                    TellMessage(player, MSG(MSG_CFG_NO_SETTING_FOUND, player.UserIDString));
                }
            }
        }
        [ChatCommand("draw_cargo")]
        private void ChatCommandDrawCargo(BasePlayer player, string command, string[] args)
        {
            //if (!HasAdminPermission(player)) return;

            //stagger to avoid kicking

            float timer = 0.02F;

            foreach (var oceanNode in TerrainMeta.Path.OceanPatrolFar)
            {
                Instance.timer.Once(timer, () =>
                {
                    bool setAdmin = false;

                    if (!player.HasPlayerFlag(BasePlayer.PlayerFlags.IsAdmin))
                    {
                        player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
                        player.SendNetworkUpdateImmediate();
                        setAdmin = true;
                    }

                    player.SendConsoleCommand("ddraw.sphere", 15F, Color.red, oceanNode, Instance.configData.MinDistanceFromCargoShipNode);

                    if (setAdmin)
                    {
                        player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                        player.SendNetworkUpdateImmediate();
                    }
                });

                timer += 0.02F;

            }
        }

        [ChatCommand("grid_pos")]
        private void ChatCommandGridPos(BasePlayer player, string command, string[] args)
        {
            if (!HasAdminPermission(player)) return;

            player.ChatMessage($"OUTSIDE MAP GRID: {IsPositionOutsideMapGrid(player.transform.position)}");
        }

        [ChatCommand("shore_distance")]
        private void ChatCommandShoreDistance(BasePlayer player, string command, string[] args)
        {
            if (!HasAdminPermission(player)) return;

            player.ChatMessage(GetCoarseDistanceToShore(player.transform.position).ToString());
        }

        [ChatCommand("give_square")]
        private void ChatCommandGiveSquare(BasePlayer player, string command, string[] args)
        {
            if (!HasAdminPermission(player)) return;

            int amount = 1;

            if (args.Length > 0)
            {
                if (!int.TryParse(args[0], out amount))
                {
                    amount = 1;
                }
            }

            if (amount<0)
            {
                amount = 1;
            }

            player.GiveItem(MakeFoundationItem(SKIN_FOUNDATION_SQUARE, amount), BaseEntity.GiveItemReason.Generic);
        }

        [ChatCommand("give_triangle")]
        private void ChatCommandGiveTriangle(BasePlayer player, string command, string[] args)
        {
            if (!HasAdminPermission(player)) return;

            int amount = 1;

            if (args.Length > 0)
            {
                if (!int.TryParse(args[0], out amount))
                {
                    amount = 1;
                }
            }

            if (amount < 0)
            {
                amount = 1;
            }

            player.GiveItem(MakeFoundationItem(SKIN_FOUNDATION_TRIANGLE, amount), BaseEntity.GiveItemReason.Generic);

        }
        #endregion


        #region CONFIG
        public class InteractiveConfigValue
        {
            private Func<object> getter;
            private Action<object> setter;

            private Func<object, bool> validator;
            public Func<object, string> formatter;
            private Func<object, object> parser;

            public Type valueType;

            private double lowerLimit;
            private double upperLimit;

            //name also identifies it in the dic
            public string name;
            public string description;

            public InteractiveConfigValue(string name, string description, Func<object> getter, Action<object> setter, Type validatorType = null, double lowerLimit = double.NegativeInfinity, double upperLimit = double.PositiveInfinity)
            {
                this.getter = getter;
                this.setter = setter;
                this.name = name;
                this.description = description;
                this.lowerLimit = lowerLimit;
                this.upperLimit = upperLimit;

                formatter = FormatDefault;

                if (validatorType == null || validatorType == typeof(bool))
                {
                    validator = ValidateBool;
                    parser = ParseBool;
                    formatter = FormatBool;
                }
                else if (validatorType == typeof(float))
                {
                    validator = ValidateFloat;
                    parser = ParseFloat;
                }
                else if (validatorType == typeof(int))
                {
                    validator = ValidateInt;
                    parser = ParseInt;
                }
                else if (validatorType == typeof(ulong))
                {
                    validator = ValidateUlong;
                    parser = ParseUlong;
                }
                else if (validatorType == typeof(string))
                {
                    validator = ValidateString;
                    parser = ParseString;
                    formatter = FormatString;
                }
                else if (validatorType == typeof(CargoPathHandling))
                {
                    validator = ValidateEnumCargo;
                    parser = ParseEnumCargo;
                    formatter = FormatEnumCargo;
                }

                valueType = validatorType;
            }

            public object GetSet
            {
                get
                {
                    return getter();
                }
                set
                {
                    //validate first
                    if (validator(value))
                    {
                        ReusableObject = parser(value);
                        setter(ReusableObject);
                        //tell the players

                        TellMessage(null, MSG(MSG_VALUE_HAS_BEEN_SET, null, name, formatter(ReusableObject)));
                        Instance.SaveConfigData();

                    }
                    else
                    {
                        ReusableString = upperLimit != double.PositiveInfinity || lowerLimit != double.NegativeInfinity ? $"The value for {name} is either too low or too high. Try {FormatAcceptable()}" : $"Incorrect value for {name}. You need to enter {FormatAcceptable()}";

                        TellMessage(null, $"{ReusableString} \nThe value remains as {formatter(getter())}.");
                    }

                }
            }

            public string FormatAcceptable()
            {
                return valueType == typeof(CargoPathHandling) ? "Warn, Prevent or None" : valueType == typeof(bool) ? "logical values (<color=green>true</color> or <color=red>false</color>)" : valueType == typeof(string) ? FormatStrings() : FormatNumericLimits();
            }

            private string FormatNumericLimits()
            {
                ReusableString2 = valueType == typeof(float) ? "<color=green>fractions (like 1.2345)</color>" : "<color=purple>integers (like 12345)</color>";

                return $"{ReusableString2} between <color=red>{lowerLimit.ToString("0.00")}</color> and <color=blue>{upperLimit.ToString("0.00")}";
            }

            private string FormatStrings()
            {
                return "hexadecimal numbers WITHOUT preceeding # (like 3db4b3)";
                //return "strings (like ThisIsAString) - if they have a space, in quotes (like \"This Is A String\")";
            }

            private string FormatString(object value)
            {
                return $"<color=yellow>{value?.ToString() ?? "NULL"}</color>";
            }

            private string FormatEnumCargo(object value)
            {
                return $"<color=red>{value.ToString() ?? "NULL"}</color>";
            }

            private string FormatBool(object value)
            {
                return value.Equals(true) ? "<color=green>true</color>" : "<color=red>false</color>";
            }

            private object ParseBool(object value)
            {
                ReusableString2 = value.ToString().ToLower();

                ReusableBool2 = !(ReusableString2.Contains("f") || ReusableString2.Contains("0") || ReusableString2.Contains("no"));

                return ReusableBool2;
            }

            private object ParseEnumCargo(object value)
            {
                ReusableString2 = value.ToString().ToLower();

                if (ReusableString2.Contains("none"))
                {
                    return CargoPathHandling.None;
                }

                if (ReusableString2.Contains("warn"))
                {
                    return CargoPathHandling.Warn;
                }

                if (ReusableString2.Contains("prevent"))
                {
                    return CargoPathHandling.Prevent;
                }

                return null;
            }
            private bool ValidateBool(object value)
            {
                //whatever it is, it can be always treated as bool
                return true;
            }

            private object ParseFloat(object value)
            {
                //it's already in ReusableFloat! neat, huh.
                //since we're using it, it must've been validated, so it's still there eh.
                return ReusableFloat2;
            }

            private object ParseString(object value)
            {
                return ReusableString;
            }

            private object ParseInt(object value)
            {
                return ReusableInt2;
            }

            private object ParseUlong(object value)
            {
                return ReusableUlong2;
            }

            private bool ValidateFloat(object value)
            {
                if (float.TryParse(value.ToString(), out ReusableFloat2))
                {
                    return (ReusableFloat2 >= lowerLimit && ReusableFloat2 <= upperLimit);
                }
                else return false;
            }

            private bool ValidateInt(object value)
            {
                if (int.TryParse(value.ToString(), out ReusableInt2))
                {
                    return (ReusableInt2 >= lowerLimit && ReusableInt2 <= upperLimit);
                }
                else return false;
            }

            private bool ValidateUlong(object value)
            {
                if (ulong.TryParse(value.ToString(), out ReusableUlong2))
                {
                    return (ReusableUlong2 >= lowerLimit && ReusableUlong2 <= upperLimit);
                }
                else return false;
            }

            private bool ValidateString(object value)
            {
                ReusableString = value.ToString();

                if (ReusableString.ToLower() == "null")
                {
                    ReusableString = null;
                }
                return true;
            }

            private bool ValidateEnumCargo(object value)
            {
                ReusableString = value.ToString().ToLower();

                if (ReusableString == "" || ReusableString == string.Empty)
                {
                    return false;
                }

                if (!(ReusableString.Contains("warn") || ReusableString.Contains("prevent") || ReusableString.Contains("none")))
                {
                    return false;
                }

                return true;

            }

            private string FormatDefault(object value)
            {
                return $"<color=#00FFFF>{value.ToString()}</color>";
            }

        }

        private static Dictionary<string, InteractiveConfigValue> ConfigValues = null;

        private static void AddInteractiveConfigValue(string name, string description, Func<object> getter, Action<object> setter, Type validator = null, double lowerLimit = double.NegativeInfinity, double upperLimit = double.PositiveInfinity)
        {
            ConfigValues.Add(name, new InteractiveConfigValue(name, description, getter, setter, validator, lowerLimit, upperLimit));
        }


        private static void GenerateAllInteractiveConfigValues()
        {
            AddInteractiveConfigValue("RelyOnShoreDistance", $"If true, shore distance will be checked when players try to place water foundation. Maps, especially custom, might not have topology maps defined correctly - if you see deployment errors that don't make sense according to your config, disable this check and rely on water depth instead (the farther from shore, the deeper).", () => Instance.configData.RelyOnShoreDistance, val => { Instance.configData.RelyOnShoreDistance = val.Equals(true); }, typeof(bool));

            AddInteractiveConfigValue("CargoShipPathHandling", $"If a player tries building/deploying a Water Foundation too close to the path of the Cargo Ship, they will get, respectively, a warning (but the foundation will stay there anyway), they will be prevented from doing so, or nothing will happen.", () => Instance.configData.CargoShipPathHandling, val => { Instance.configData.CargoShipPathHandling = (CargoPathHandling)val; }, typeof(CargoPathHandling));

            AddInteractiveConfigValue("MinDistanceFromCargoShipNode", $"The lower this value (in meters), the closer to Cargo Ship paths players will be able to build water foundations (they will just get a warning, won't be able to build, or nothing will happen - see CargoShipPathHandling).", () => Instance.configData.MinDistanceFromCargoShipNode, val => { Instance.configData.MinDistanceFromCargoShipNode = (float)val; }, typeof(float), 0F, 10000F);

            AddInteractiveConfigValue("RestrictBuildingToMapGrid", $"If true, players will only be able to deploy water foundations within the boundaries of the named squares of the map grid", () => Instance.configData.RestrictBuildingToMapGrid, val => { Instance.configData.RestrictBuildingToMapGrid = val.Equals(true); }, typeof(bool));

            AddInteractiveConfigValue("UnderwaterNetsCollectJunk", $"If true, Nettings deployed in wall frames underwater will accumulate random low-tier items. The full loot table (items and their weights) can be edited in the config file.", () => Instance.configData.UnderwaterNetsCollectJunk, val => { Instance.configData.UnderwaterNetsCollectJunk = val.Equals(true); }, typeof(bool));

            AddInteractiveConfigValue("UnderwaterNetsRandomTimerMin", $"Minimum duration of time before the next random item spawns in a net (in seconds)", () => Instance.configData.UnderwaterNetsRandomTimerMin, val => { Instance.configData.UnderwaterNetsRandomTimerMin = (float)val; }, typeof(float), 1F, 100000F);

            AddInteractiveConfigValue("UnderwaterNetsRandomTimerMax", $"Maximum duration of time before the next random item spawns in a net (in seconds)", () => Instance.configData.UnderwaterNetsRandomTimerMax, val => { Instance.configData.UnderwaterNetsRandomTimerMax = (float)val; }, typeof(float), 1F, 100000F);

            AddInteractiveConfigValue("UnderwaterNetsItemLimit", $"If the amount of items currently caught in the net is larger than this number, don't catch new items until room is made (or one of the items despawns)", () => Instance.configData.UnderwaterNetsItemLimit, val => { Instance.configData.UnderwaterNetsItemLimit = (int)val; }, typeof(int), 0, 100);

            AddInteractiveConfigValue("GuiAnchorALeft", $"Building plan crafting GUI anchor min x (left)", () => Instance.configData.GuiAnchorALeft, val => { Instance.configData.GuiAnchorALeft = (float)val; GuiManager.GenerateGUI(); }, typeof(float), 0F, 1F);

            AddInteractiveConfigValue("GuiAnchorBBottom", $"Building plan crafting GUI anchor min y (bottom)", () => Instance.configData.GuiAnchorBBottom, val => { Instance.configData.GuiAnchorBBottom = (float)val; GuiManager.GenerateGUI(); }, typeof(float), 0F, 1F);

            AddInteractiveConfigValue("GuiAnchorCRight", $"Building plan crafting GUI anchor max x (right)", () => Instance.configData.GuiAnchorCRight, val => { Instance.configData.GuiAnchorCRight = (float)val; GuiManager.GenerateGUI(); }, typeof(float), 0F, 1F);

            AddInteractiveConfigValue("GuiAnchorDTop", $"Building plan crafting GUI anchor max y (top)", () => Instance.configData.GuiAnchorDTop, val => { Instance.configData.GuiAnchorDTop = (float)val; GuiManager.GenerateGUI(); }, typeof(float), 0F, 1F);

            AddInteractiveConfigValue("GuiTextSize", $"Building plan crafting GUI text size", () => Instance.configData.GuiTextSize, val => { Instance.configData.GuiTextSize = (int)val; GuiManager.GenerateGUI(); }, typeof(int), 4, 72);

            AddInteractiveConfigValue("GuiButtonColor", $"Building plan crafting GUI button background colour (hex number, no preceeding hash)", () => Instance.configData.GuiButtonColor, val => { Instance.configData.GuiButtonColor = (string)val; GuiManager.GenerateGUI(); }, typeof(string));

            AddInteractiveConfigValue("GuiTextColor", $"Building plan crafting GUI text colour (hex number, no preceeding hash)", () => Instance.configData.GuiTextColor, val => { Instance.configData.GuiTextColor = (string)val; GuiManager.GenerateGUI(); }, typeof(string));

            AddInteractiveConfigValue("GuiButtonAlpha", $"Building plan crafting GUI button alpha (0 = fully transparent, 1 = fully opaque)", () => Instance.configData.GuiButtonAlpha, val => { Instance.configData.GuiButtonAlpha = (float)val; GuiManager.GenerateGUI(); }, typeof(float), 0F, 1F);

            AddInteractiveConfigValue("GuiTextAlpha", $"Building plan crafting GUI text alpha (0 = fully transparent, 1 = fully opaque)", () => Instance.configData.GuiTextAlpha, val => { Instance.configData.GuiTextAlpha = (float)val; GuiManager.GenerateGUI(); }, typeof(float), 0F, 1F);

            AddInteractiveConfigValue("EnableBarrelEntities", $"If true, water foundation and reinforcements will spawn barrels for visuals. If you're worried about potential server lag with extra entities, set to false, but it will fully expose your foundation/reinforcement soft sides. Changes will take effect for newly deployed foundations or after server restart.", () => Instance.configData.EnableBarrelEntities, val => { Instance.configData.EnableBarrelEntities = val.Equals(true); }, typeof(bool));
        }
        public enum DeploymentResult
        {
            Success,
            BuildingBlocked,
            DeployedInPool,

            TooManyFoundations,
            TooCloseToOilrig,

            NoPermissionToDeploy,
            NoPermissionToExpand,

            TopologyMismatch,

            WaterTooShallow,
            WaterTooDeep,

            TooFarFromShore,
            TooCloseToShore,

            OnCargoShipPath,
            NoRequiredItem,

            OutsideMapGrid

        }

        public enum CargoPathHandling
        {
            None,
            Warn,
            Prevent
        }

        public enum Topology
        {
            Field = 1,
            Cliff = 2,
            Summit = 4,
            Beachside = 8,
            Beach = 0x10,
            Forest = 0x20,
            Forestside = 0x40,
            Ocean = 0x80,
            Oceanside = 0x100,
            Decor = 0x200,
            Monument = 0x400,
            Road = 0x800,
            Roadside = 0x1000,
            Swamp = 0x2000,
            River = 0x4000,
            Riverside = 0x8000,
            Lake = 0x10000,
            Lakeside = 0x20000,
            Offshore = 0x40000,
            Powerline = 0x80000,
            Plain = 0x100000,
            Building = 0x200000,
            Cliffside = 0x400000,
            Mountain = 0x800000,
            Clutter = 0x1000000,
            Alt = 0x2000000,
            Tier0 = 0x4000000,
            Tier1 = 0x8000000,
            Tier2 = 0x10000000,
            Mainland = 0x20000000,
            Hilltop = 0x40000000
        }

        private class SerializableItemAmount
        {
            public string Shortname = null;
            public int Amount = 1;
        }

        private class PermissionProfile
        {
            public string PermissionRequired = "";

            [JsonConverter(typeof(StringEnumConverter))]
            public BuildingGrade.Enum MaxBuildingGradeGeneric = BuildingGrade.Enum.Wood;

            [JsonConverter(typeof(StringEnumConverter))]
            public BuildingGrade.Enum MaxBuildingGradeFrames = BuildingGrade.Enum.Stone;

            [JsonConverter(typeof(StringEnumConverter))]
            public BuildingGrade.Enum MaxBuildingGradeWaterFoundations = BuildingGrade.Enum.Metal;

            public float WaterDepthMin = 1F;
            public float WaterDepthMax = 250F;

            public int MaxWaterFoundationsPerBuilding = 25;

            public float MinDistanceFromOilrig = 100F;

            public float MinDistanceFromShore = 10F;
            public float MaxDistanceFromShore = 1000F;

            public bool CanDeployWaterFoundations = true;
            public bool CanReinforceWaterFoundations = true;
            public bool CanExpandWaterFoundations = true;
            public bool CanExpandReinforcedFoundations = true;
            public bool CanCraftWaterFoundations = false;

            public bool RequireMaterialsForCrafting = true;

            public float WorkbenchLevelRequired = 1F;

            public bool CanDeployUnderwaterNets = true;

            public Dictionary<Topology, bool> AllowedTopologies = new Dictionary<Topology, bool>();
        }

        private class UnderwaterLootDefinition
        {
            public string Shortname = null;
            public string CustomName = null;
            public ulong SkinID = 0;
            public float RandomChanceWeight = 1F;
            public int MinRandomAmount = 1;
            public int MaxRandomAmount = 1;
        }

        private class ConfigData
        {
            public string Version = VERSION;

            public bool RelyOnShoreDistance = true;

            [JsonConverter(typeof(StringEnumConverter))]
            public CargoPathHandling CargoShipPathHandling = CargoPathHandling.Warn;

            public float MinDistanceFromCargoShipNode = 40F;

            public bool UnderwaterNetsCollectJunk = true;
            public float UnderwaterNetsRandomTimerMin = 600;
            public float UnderwaterNetsRandomTimerMax = 1200;
            public int UnderwaterNetsItemLimit = 10;

            public bool RestrictBuildingToMapGrid = false;

            public bool EnableBarrelEntities = true;

            public List<UnderwaterLootDefinition> UnderwaterLoot = new List<UnderwaterLootDefinition>();

            public float UpkeepMultiplier = 1F;

            public List<SerializableItemAmount> CraftingCostSquare = new List<SerializableItemAmount>();
            public List<SerializableItemAmount> CraftingCostTriangle = new List<SerializableItemAmount>();

            public Dictionary<string, PermissionProfile> PermissionProfiles = new Dictionary<string, PermissionProfile>();

            public float GuiAnchorALeft = 0.645833F;
            public float GuiAnchorBBottom = 0.02592F;
            public float GuiAnchorCRight = 0.83125F;
            public float GuiAnchorDTop = 0.108333F;
            public int GuiTextSize = 14;
            public string GuiButtonColor = "4897ce";
            public string GuiTextColor = "f6eae0";
            public float GuiButtonAlpha = 1.0F;
            public float GuiTextAlpha = 1.0F;

            [JsonIgnore]
            public Dictionary<string, int> ProfileTopologyMask = new Dictionary<string, int>();

            [JsonIgnore]
            public Dictionary<Vector3, string> OilrigPositions = new Dictionary<Vector3, string>();

            //needs recalculation
            [JsonIgnore]
            public float UnderwaterLootWeightSum;

        }

        private ConfigData configData;

        protected override void LoadDefaultConfig()
        {
            RestoreDefaultConfig();
        }

        private void ProcessConfigData()
        {
            if (configData.Version != VERSION)
            {
                var version = configData.Version;

                if (version == "1.0.2" && VERSION == "1.0.3" || VERSION == "1.0.4")
                {
                    //fix typo
                    foreach (var lootEntry in configData.UnderwaterLoot)
                    {
                        if (lootEntry.Shortname == "planer.large")
                        {
                            lootEntry.Shortname = "planter.large";
                        }
                    }
                }

                configData.Version = VERSION;
                Instance.PrintWarning($"\n\nYou have succesfully updated from {version} to {VERSION}\n");
                SaveConfigData();
            }

            GenerateOilrigPositionCache();
            RegenerateProfileTopologyCache();
        }

        private void LoadConfigData()
        {
            PrintWarning("Loading configuration file...");
            try
            {
                configData = Config.ReadObject<ConfigData>();
                PrintWarning("Success.");
            }
            catch
            {
                RestoreDefaultConfig();
                PrintWarning("Loading failed, generating new...");

            }

        }
        private void SaveConfigData()
        {
            PrintWarning("Saving config...");
            Config.WriteObject(configData, true);
        }

        private void RestoreDefaultConfig()
        {
            PrintWarning("Generating default config...");

            configData = new ConfigData();

            configData.PermissionProfiles = new Dictionary<string, PermissionProfile>
            {
                [PERM_ADMIN] = AdminPermissionProfile(),

                ["default"] = DefaultPermissionProfile(),

                [PERM_VIP1] = VIPPermissionProfile(),

            };

            configData.UnderwaterLoot = DefaultUnderwaterLoot();

            configData.CraftingCostSquare = DefaultCostSquare();
            configData.CraftingCostTriangle = DefaultCostTriangle();

            SaveConfigData();
        }
        private void GenerateOilrigPositionCache()
        {
            configData.OilrigPositions.Clear();

            foreach (var info in TerrainMeta.Path.Monuments)
            {
                if (!(info.name == PREFAB_OILRIG_1 || info.name == PREFAB_OILRIG_2))
                {
                    return;
                }

                configData.OilrigPositions.Add(info.transform.position, info.name);
            }
        }

        private void RegenerateProfileTopologyCache()
        {
            configData.ProfileTopologyMask.Clear();

            foreach (var entry in configData.PermissionProfiles)
            {
                configData.ProfileTopologyMask.Add(entry.Key, GetTopologyMaskFromDictionary(entry.Value.AllowedTopologies));
            }
        }
        private List<SerializableItemAmount> DefaultCostSquare()
        {
            return new List<SerializableItemAmount>
            {
                new SerializableItemAmount
                {
                    Shortname = "wood",
                    Amount = 100
                },
                new SerializableItemAmount
                {
                    Shortname = "metal.fragments",
                    Amount = 200
                }
            };
        }

        private List<SerializableItemAmount> DefaultCostTriangle()
        {
            return new List<SerializableItemAmount>
            {
                new SerializableItemAmount
                {
                    Shortname = "wood",
                    Amount = 50
                },
                new SerializableItemAmount
                {
                    Shortname = "metal.fragments",
                    Amount = 100
                }
            };
        }

        private List<UnderwaterLootDefinition> DefaultUnderwaterLoot()
        {
            List<UnderwaterLootDefinition> result = new List<UnderwaterLootDefinition>
            {
                new UnderwaterLootDefinition
                {
                    Shortname = "scrap",
                    MinRandomAmount = 1,
                    MaxRandomAmount = 20,
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "metalblade",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "rope",
                    MinRandomAmount = 1,
                    MaxRandomAmount = 2,
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "propanetank",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "tarp",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "sewingkit",
                    MinRandomAmount = 3,
                    MaxRandomAmount = 4,
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "fuse",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "roadsigns",
                    MinRandomAmount = 2,
                    MaxRandomAmount = 3,
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "sheetmetal",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "metalspring",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "gears",
                    MinRandomAmount = 1,
                    MaxRandomAmount = 2,
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "metalpipe",
                    MinRandomAmount = 1,
                    MaxRandomAmount = 4,
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "semibody",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "barricade.wood",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "planter.large",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "paddle",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "bucket.water",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "mailbox",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "hat.cap",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "fun.guitar",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "can.tuna.empty",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "fireplace.stone",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "mask.bandana",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "barricade.woodwire",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "sign.wooden.large",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "electric.igniter",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "hat.boonie",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "spinner.wheel",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "rug.bear",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "table",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "shelves",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "water.barrel",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "spikes.floor",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "shirt.tanktop",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "pants.shorts",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "shutter.wood.a",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "barricade.stone",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "tool.binoculars",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "burlap.gloves",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "mask.balaclava",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "sign.wooden.huge",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "hat.beenie",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "rug",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "fish.minnows",
                    MinRandomAmount = 1,
                    MaxRandomAmount = 32
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "fish.troutsmall",
                    MinRandomAmount = 1,
                    MaxRandomAmount = 8,
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "innertube",
                    CustomName = null,
                    SkinID = SKIN_FOUNDATION_SQUARE,
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "innertube",
                    CustomName = null,
                    SkinID = SKIN_FOUNDATION_TRIANGLE,
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "diving.fins",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "diving.mask",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "diving.tank",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "diving.wetsuit",
                },
                new UnderwaterLootDefinition
                {
                    Shortname = "smallwaterbottle",
                }
            };

            return result;
        }

        private PermissionProfile AdminPermissionProfile()
        {
            return new PermissionProfile
            {
                PermissionRequired = PERM_ADMIN,
                CanDeployWaterFoundations = true,
                CanExpandWaterFoundations = true,
                CanExpandReinforcedFoundations = true,
                CanReinforceWaterFoundations = true,
                AllowedTopologies = DefaultAllowedTopologies(),
                MinDistanceFromShore = float.NegativeInfinity,
                MaxDistanceFromShore = float.PositiveInfinity,
                MinDistanceFromOilrig = float.NegativeInfinity,
                MaxBuildingGradeGeneric = BuildingGrade.Enum.TopTier,
                MaxBuildingGradeFrames = BuildingGrade.Enum.TopTier,
                MaxBuildingGradeWaterFoundations = BuildingGrade.Enum.TopTier,
                MaxWaterFoundationsPerBuilding = int.MaxValue,
                WaterDepthMin = float.NegativeInfinity,
                WaterDepthMax = float.PositiveInfinity,
                RequireMaterialsForCrafting = false,
                CanCraftWaterFoundations = true,
                WorkbenchLevelRequired = 0F
            };
        }

        private PermissionProfile DefaultPermissionProfile()
        {
            return new PermissionProfile
            {
                PermissionRequired = "default",
                CanDeployWaterFoundations = true,
                CanExpandWaterFoundations = true,
                CanExpandReinforcedFoundations = true,
                CanReinforceWaterFoundations = true,
                AllowedTopologies = DefaultAllowedTopologies(),
                MinDistanceFromShore = 2F,
                MaxDistanceFromShore = 20F,
                MinDistanceFromOilrig = 200F,
                MaxBuildingGradeGeneric = BuildingGrade.Enum.Wood,
                MaxBuildingGradeFrames = BuildingGrade.Enum.Stone,
                MaxBuildingGradeWaterFoundations = BuildingGrade.Enum.Metal,
                MaxWaterFoundationsPerBuilding = 25,
                WaterDepthMin = 2F,
                WaterDepthMax = 100F,
                RequireMaterialsForCrafting = true,
                CanCraftWaterFoundations = true,
                WorkbenchLevelRequired = 2F
            };
        }

        private PermissionProfile VIPPermissionProfile()
        {
            return new PermissionProfile
            {
                PermissionRequired = PERM_VIP1,
                CanDeployWaterFoundations = true,
                CanExpandWaterFoundations = true,
                CanExpandReinforcedFoundations = true,
                CanReinforceWaterFoundations = true,
                AllowedTopologies = DefaultAllowedTopologies(),
                MinDistanceFromShore = 1F,
                MaxDistanceFromShore = 30F,
                MinDistanceFromOilrig = 100F,
                MaxBuildingGradeGeneric = BuildingGrade.Enum.Stone,
                MaxBuildingGradeFrames = BuildingGrade.Enum.Metal,
                MaxBuildingGradeWaterFoundations = BuildingGrade.Enum.TopTier,
                MaxWaterFoundationsPerBuilding = 100,
                WaterDepthMin = 1F,
                WaterDepthMax = 250F,
                RequireMaterialsForCrafting = true,
                CanCraftWaterFoundations = true,
                WorkbenchLevelRequired = 1F
            };
        }

        private Dictionary<Topology, bool> DefaultAllowedTopologies()
        {
            return new Dictionary<Topology, bool>
            {
                [Topology.Alt] = true,
                [Topology.Beach] = true,
                [Topology.Beachside] = true,
                [Topology.Building] = false,
                [Topology.Cliff] = true,
                [Topology.Cliffside] = true,
                [Topology.Clutter] = true,
                [Topology.Decor] = true,
                [Topology.Field] = true,
                [Topology.Forest] = true,
                [Topology.Forestside] = true,
                [Topology.Hilltop] = true,
                [Topology.Lake] = true,
                [Topology.Lakeside] = true,
                [Topology.Mainland] = true,
                [Topology.Monument] = false,
                [Topology.Mountain] = true,
                [Topology.Ocean] = true,
                [Topology.Oceanside] = true,
                [Topology.Offshore] = true,
                [Topology.Plain] = true,
                [Topology.Powerline] = true,
                [Topology.River] = true,
                [Topology.Riverside] = true,
                [Topology.Road] = true,
                [Topology.Roadside] = true,
                [Topology.Summit] = true,
                [Topology.Swamp] = true,
                [Topology.Tier0] = true,
                [Topology.Tier1] = true,
                [Topology.Tier2] = true
            };
        }
        #endregion

        #region HELPERS
        private static bool HasAdminPermission(BasePlayer player)
        {
            if (player.IsDeveloper) return true;
            if (player.IsAdmin) return true;
            if (Instance.permission.UserHasPermission(player.UserIDString, PERM_ADMIN)) return true;

            return false;
        }

        private static Tuple<Vector3, Quaternion> GetWorldTransformRelativeTo(Transform transform, Vector3 localPosition, Vector3 localRotation)
        {
            return new Tuple<Vector3, Quaternion>(transform.TransformPoint(localPosition), transform.rotation * Quaternion.Euler(localRotation));
        }

        private static bool TryTakeNeededMaterialsFromPlayer(BasePlayer player, ulong foundationSkin, int amount = 1)
        {
            List<SerializableItemAmount> itemAmounts = foundationSkin == SKIN_FOUNDATION_SQUARE ? Instance.configData.CraftingCostSquare : Instance.configData.CraftingCostTriangle;

            ItemDefinition def;

            bool soFarSoGood = true;

            string needed = "";

            int counter = 0;

            foreach (var itemEntry in itemAmounts)
            {
                def = ItemManager.FindItemDefinition(itemEntry.Shortname);

                if (def == null)
                {
                    //ignore this item
                    continue;
                }

                needed += $"{def.displayName.translated} x {itemEntry.Amount * amount}";

                if (counter != itemAmounts.Count-1)
                {
                    needed += $", ";
                }

                if (player.inventory.GetAmount(def.itemid) < itemEntry.Amount * amount)
                {
                    soFarSoGood = false;
                }

                counter++;
            }

            if (!soFarSoGood)
            {
                TellMessage(player, MSG(MSG_CRAFTING_NO_MATERIALS, player.UserIDString, needed, $"{amount} {(foundationSkin == SKIN_FOUNDATION_SQUARE ? MSG(MSG_ITEM_NAME_SQUARE, player.UserIDString) : MSG(MSG_ITEM_NAME_TRIANGLE, player.UserIDString))}"));
                return false;
            }
            else
            {
                //now you can take
                foreach (var itemEntry in itemAmounts)
                {
                    def = ItemManager.FindItemDefinition(itemEntry.Shortname);

                    if (def == null)
                    {
                        continue;
                    }

                    player.inventory.Take(null, def.itemid, itemEntry.Amount*amount);
                    player.Command("note.inv", def.itemid, -itemEntry.Amount*amount);
                }

                return true;

            }
        }

        private static bool TryCraftingWaterFoundation(BasePlayer player, ulong skin, bool skipChecks = false, int amount = 1)
        {
            bool result = false;

            if (!skipChecks)
            {
                var prof = GetPermissionProfile(player);

                if (!prof.CanCraftWaterFoundations)
                {
                    TellMessage(player, MSG(MSG_CRAFTING_NO_PERM, player.UserIDString));
                    result = false;
                }

                if (player.currentCraftLevel < prof.WorkbenchLevelRequired)
                {
                    TellMessage(player, MSG(MSG_CRAFTING_INSUFFICIENT_WB_LEVEL, player.UserIDString, prof.WorkbenchLevelRequired.ToString("0")));
                    result = false;
                }
                else
                {
                    if (prof.RequireMaterialsForCrafting)
                    {
                        if (TryTakeNeededMaterialsFromPlayer(player, skin, amount))
                        {
                            //this already displays notification
                            result = true;
                        }
                        else
                        {
                            result = false;
                        }
                    }
                    else
                    {
                        result = true;
                    }
                }
            }

            if (result == true)
            {
                player.GiveItem(MakeFoundationItem(skin), BaseEntity.GiveItemReason.Crafted);
            }

            return result;
        }

        private static DroppedItem CreateRandomUnderwaterJunk(StabilityEntity netting)
        {
            DroppedItem result = null;

            var diceThrow = UnityEngine.Random.Range(0F, Instance.configData.UnderwaterLootWeightSum);

            var sumSoFar = 0F;

            int foundID = -1;

            for (var i = 0; i < Instance.configData.UnderwaterLoot.Count; i++)
            {
                sumSoFar += Instance.configData.UnderwaterLoot[i].RandomChanceWeight;

                if (sumSoFar >= diceThrow)
                {
                    foundID = i;
                    break;
                }
            }

            if (foundID !=-1)
            {
                var tableEntry = Instance.configData.UnderwaterLoot[foundID];

                var newItem = ItemManager.CreateByName(tableEntry.Shortname, tableEntry.MinRandomAmount == tableEntry.MaxRandomAmount ? tableEntry.MinRandomAmount : UnityEngine.Random.Range(tableEntry.MinRandomAmount, tableEntry.MaxRandomAmount), tableEntry.SkinID);

                if (tableEntry.CustomName != null)
                {
                    newItem.name = tableEntry.CustomName;
                }

                newItem.MarkDirty();

                //figure out where to drop...
                var transf = GetWorldTransformRelativeTo(netting.transform, new Vector3(UnityEngine.Random.Range(-0.2F, 0.2F), UnityEngine.Random.Range(0.1F, 2.9F), UnityEngine.Random.Range(-1.4F, 1.4F)), new Vector3(UnityEngine.Random.Range(0, 360F), UnityEngine.Random.Range(0, 360F), UnityEngine.Random.Range(0, 360F)));

                result = newItem.Drop(transf.Item1, Vector3.zero, transf.Item2) as DroppedItem;

                var rigid = result.gameObject.GetComponent<Rigidbody>();
                rigid.useGravity = false;
                rigid.isKinematic = true;

                //parent...
                result.SetParent(netting, true, false);
            }


            return result;
        }

        private static void ConsumeFromSpecificItemList(ref List<Item> specificList, int amount = 1)
        {
            var consumedSoFar = 0;

            foreach (var item in specificList)
            {
                for (var i=0; i<item.amount; i++)
                {
                    item.UseItem(1);
                    consumedSoFar++;

                    if (consumedSoFar >= amount)
                    {
                        break;
                    }
                }
            }

            Facepunch.Pool.FreeList(ref specificList);
        }

        private static List<Item> FindNeededFoundationItemsInInventory(BasePlayer player, ulong foundationSkin, int neededAmount = 1)
        {
            List<Item> result = Facepunch.Pool.GetList<Item>();

            var foundSoFar = 0;

            foreach (var item in player.inventory.containerMain.itemList)
            {
                if (item.info.itemid != ITEM_INNER_TUBE) continue;
                if (item.skin == foundationSkin)
                {
                    foundSoFar += item.amount;
                    result.Add(item);

                    if (foundSoFar >= neededAmount)
                    {

                        return result;
                    }

                }
            }

            foreach (var item in player.inventory.containerBelt.itemList)
            {
                if (item.info.itemid != ITEM_INNER_TUBE) continue;

                if (item.skin == foundationSkin)
                {
                    foundSoFar += item.amount;
                    result.Add(item);

                    if (foundSoFar >= neededAmount)
                    {
                        return result;
                    }

                }
            }

            return null;
        }
        private static BuildingBlock TryFindWaterFoundationAboveYou(BuildingBlock inverseFoundation)
        {
            BuildingBlock result = null;

            var trans = GetWorldTransformRelativeTo(inverseFoundation.transform, Vector3.forward*0.2F, Vector3.zero);

            var hits = Physics.RaycastAll(trans.Item1, Vector3.up, 2F, ConstructionMask);

            BaseEntity ent;
            BuildingBlock bui;
            RaycastHit hit;

            if (hits != null)
            {
                for (var h = 0; h < hits.Length; h++)
                {
                    hit = hits[h];
                    ent = hit.GetEntity();
                    if (ent == null)
                    {
                        continue;
                    }

                    if (ent.net.ID == inverseFoundation.net.ID)
                    {
                        continue;
                    }

                    bui = ent as BuildingBlock;

                    if (bui == null)
                    {
                        continue;
                    }

                    if (IsWaterFoundation(bui))
                    {
                        result = bui;
                        break;
                    }
                }
            }

            return result;
        }

        private static void MonoiseUnderwaterNetting(StabilityEntity underwaterNetting)
        {
            if (underwaterNetting.gameObject.HasComponent<UnderwaterNetHelper>())
            {
                return;
            }

            underwaterNetting.gameObject.AddComponent<UnderwaterNetHelper>().Prepare(underwaterNetting);
        }

        private static void MonoiseInverseFoundation(BuildingBlock inverseFoundation)
        {
            if (inverseFoundation.gameObject.HasComponent<InverseFoundationHelper>())
            {
                return;
            }

            inverseFoundation.gameObject.AddComponent<InverseFoundationHelper>().Prepare(inverseFoundation);
        }

        private static void Barrelise(BuildingBlock specialFoundationBlock, bool restabilise = false)
        {
            List<CollectibleEntity> currentBarrelList = null;

            if (restabilise)
            {
                var expected = specialFoundationBlock.PrefabName == PREFAB_FLOOR_SQUARE ? 10 : 5;

                if (!Instance.configData.EnableBarrelEntities)
                {
                    expected = 0;
                }

                currentBarrelList = Facepunch.Pool.GetList<CollectibleEntity>();

                //only direct children, not children of children
                currentBarrelList.AddRange(specialFoundationBlock.GetComponentsInChildren<CollectibleEntity>().Where( e => e.GetParentEntity() == specialFoundationBlock));

                if (currentBarrelList.Count != expected)
                {
                    foreach (var oldBarrel in currentBarrelList)
                    {
                        oldBarrel.Kill(BaseNetworkable.DestroyMode.None);
                    }
                }
            }

            if (!Instance.configData.EnableBarrelEntities)
            {
                if (currentBarrelList != null)
                {
                    Facepunch.Pool.FreeList(ref currentBarrelList);
                }
                return;
            }

            List<Vector3> listToUsePos = FloorBarrelPositionsSquare;
            List<Vector3> listToUseRot = FloorBarrelRotationsSquare;

            if (specialFoundationBlock.PrefabName == PREFAB_FLOOR_TRIANGLE)
            {
                listToUsePos = FloorBarrelPositionsTriangle;
                listToUseRot = FloorBarrelRotationsTriangle;
            }

            if (listToUsePos.Count != listToUseRot.Count)
            {
                if (currentBarrelList != null)
                {
                    Facepunch.Pool.FreeList(ref currentBarrelList);
                }
                return;
            }

            CollectibleEntity barrel;

            Tuple<Vector3, Quaternion> transf;

            for (var i = 0; i < listToUsePos.Count; i++)
            {
                transf = GetWorldTransformRelativeTo(specialFoundationBlock.transform, listToUsePos[i], listToUseRot[i]);

                barrel = GameManager.server.CreateEntity(PREFAB_BARREL_COLLECTABLE, transf.Item1, transf.Item2) as CollectibleEntity;

                barrel.Spawn();

                barrel.skinID = SKIN_WATER_BARREL;

                MakeStable(barrel);

                barrel.itemList = null;

                barrel.SetParent(specialFoundationBlock, true, false);
            }

            if (currentBarrelList != null)
            {
                Facepunch.Pool.FreeList(ref currentBarrelList);
            }

        }

        private static void RecalculateUnderwaterLootWeights()
        {
            Instance.configData.UnderwaterLootWeightSum = 0F;

            foreach (var entry in Instance.configData.UnderwaterLoot.ToArray())
            {
                //remove non existing while you're at it
                if (!ItemManager.FindItemDefinition(entry.Shortname))
                {
                    Instance.configData.UnderwaterLoot.Remove(entry);
                    continue;
                }

                Instance.configData.UnderwaterLootWeightSum += entry.RandomChanceWeight;
            }
        }

        private static bool IsUnderwaterNetting(StabilityEntity stabilityEntity, bool inWaterBaseAlreadyChecked = false)
        {
            if (stabilityEntity.PrefabName != PREFAB_NETTING)
            {
                return false;
            }
            
            if (Mathf.Abs(stabilityEntity.transform.eulerAngles.z) <= 0.1F)
            {
                return false;
            }

            if (inWaterBaseAlreadyChecked)
            {
                return true;
            }

            if (!IsInWaterBase(stabilityEntity))
            {
                return false;
            }

            return true;
        }

        private static void ActivateSpecialStuff()
        {
            foreach (var stability in UnityEngine.Object.FindObjectsOfType<StabilityEntity>())
            {
                var buildingBlock = stability as BuildingBlock;

                if (buildingBlock == null)
                {
                    if (Instance.configData.UnderwaterNetsCollectJunk)
                    {
                        if (IsUnderwaterNetting(stability))
                        {
                            MonoiseUnderwaterNetting(stability);
                        }
                    }

                    continue;
                }

                if (!IsWaterFoundation(buildingBlock))
                {
                    if (IsInverseFoundation(buildingBlock))
                    {
                        MakeStable(buildingBlock);
                        MonoiseInverseFoundation(buildingBlock);
                        Barrelise(buildingBlock, true);
                    }
                    continue;
                }

                MakeStable(buildingBlock);
                Barrelise(buildingBlock, true);
            }
        }

        private static void ApplyUpkeepMultiplier(DecayEntity decayEntity)
        {
            //decayEntity.upkeep.upkeepMultiplier = Instance.configData.UpkeepMultiplier;
            //Instance.PrintToChat($"UPKEEP MULTIPLIER: {decayEntity.upkeep.upkeepMultiplier}");
        }

        private static bool IsPositionOutsideMapGrid(Vector3 pos)
        {
            //for y/z, 0.0 is the bottom, like in GUIs

            var normalised = new Vector2(TerrainMeta.NormalizeX(pos.x), TerrainMeta.NormalizeZ(pos.z));

            if (normalised.x < 0F)
            {
                return true;
            }

            if (normalised.x > 1F)
            {
                return true;
            }

            if (normalised.y < 0F)
            {
                return true;
            }

            if (normalised.y > 1F)
            {
                return true;
            }

            return false;
        }

        private static float GetCoarseDistanceToShore(Vector3 pos) => TerrainMeta.Texturing.GetCoarseDistanceToShore(pos);
        private static float GetDistanceToNearestOilrig(Vector3 pos)
        {
            float minDistance = float.MaxValue;
            float curDistance;
            foreach (var entry in Instance.configData.OilrigPositions)
            {
                curDistance = Vector3.Distance(entry.Key, pos);
                if (curDistance < minDistance)
                {
                    minDistance = curDistance;
                }
            }
            return minDistance;
        }

        private static PermissionProfile GetPermissionProfile(BasePlayer player)
        {
            foreach (var prof in Instance.configData.PermissionProfiles)
            {
                if (prof.Key == "default") continue;

                if (Instance.permission.UserHasPermission(player.UserIDString, prof.Key))
                {
                    return prof.Value;
                }
            }

            //we got here so that means nothing found.

            if (!Instance.configData.PermissionProfiles.ContainsKey("default"))
            {
                //oops, data must be fucked.
                Instance.configData.PermissionProfiles.Add("default", Instance.DefaultPermissionProfile());
                Instance.SaveConfigData();
            }

            return Instance.configData.PermissionProfiles["default"];
        }

        private static void TellMessage(BasePlayer player, string message, bool alsoPrintWarning = false)
        {
            string result = $"{MSG(MSG_CHAT_PREFIX)} {message}";

            if (player == null)
            {
                Instance.PrintToChat(result);
                if (alsoPrintWarning)
                {
                    Instance.PrintWarning(Instance.StripTags(result));
                }
            }
            else
            {
                player.ChatMessage(result);
                if (alsoPrintWarning)
                {
                    Instance.PrintWarning(Instance.StripTags($"{player.displayName} has been told: {result}"));
                }
            }
        }
        public string StripTags(string inputString)
        {
            return System.Text.RegularExpressions.Regex.Replace(inputString, "<[^>]+>|</[^>]+>", string.Empty);
        }

        public static int CountBuildingWaterFoundations(BuildingBlock blockInBuilding)
        {
            if (!IsInWaterBase(blockInBuilding)) return 0;

            var building = blockInBuilding.GetBuilding();
            if (building == null) return 0;

            var count = 0;

            foreach (var block in building.buildingBlocks)
            {
                if (IsWaterFoundation(block))
                {
                    count++;
                }
            }

            return count;
        }

        public static bool IsInWaterBase(DecayEntity decayEntity)
        {
            if (IsSkinRelevant(decayEntity.skinID))
            {
                return true;
            }

            var maybeBuildingBlock = decayEntity as BuildingBlock;
            if (maybeBuildingBlock != null)
            {
                if (IsWaterFoundation(maybeBuildingBlock))
                {
                    return true;
                }
            }

            var maybeBuilding = decayEntity.GetBuilding();

            if (maybeBuilding != null)
            {
                foreach (var thisBlock in maybeBuilding.buildingBlocks)
                {
                    if (thisBlock.net.ID == decayEntity.net.ID)
                    {
                        continue;
                    }

                    if (IsSkinRelevant(thisBlock.skinID))
                    {
                        return true;
                    }

                    if (IsWaterFoundation(thisBlock))
                    {
                        return true;
                    }

                }
            }

            return false;
        }

        public static bool IsInverseFoundation(BuildingBlock block)
        {
            return block.skinID == SKIN_INVERSE_FOUNDATION_SQUARE || block.skinID == SKIN_INVERSE_FOUNDATION_TRIANGLE;
        }

        public static UnderwaterNetHelper TryGetUnderwaterNetHelper(BaseEntity entity)
        {
            if (entity.PrefabName != PREFAB_NETTING) return null;

            var maybeCompo = entity.GetComponent<UnderwaterNetHelper>();

            return maybeCompo;
        }

        public static bool IsWaterFoundation(BuildingBlock block)
        {
            return block.skinID == SKIN_FOUNDATION_SQUARE || block.skinID == SKIN_FOUNDATION_TRIANGLE;
            /*
            if (block.PrefabName == PREFAB_FLOOR_SQUARE || block.PrefabName == PREFAB_FLOOR_TRIANGLE)
            {
                if (block.gameObject.GetComponentInChildren<CollectibleEntity>() != null)
                {
                    return true;
                }
            }*/

            //return false;
        }

        public static bool TopologyCheckMasks(int inputMask, int comparisonMask)
        {
            for (var bitPos = 0; bitPos<32; bitPos++)
            {
                int topologyValue = 2 ^ bitPos;

                int bitValueInput = inputMask & topologyValue;

                if (bitValueInput == 0)
                {
                    //can't have that

                    int bitValueComparison = comparisonMask & topologyValue;

                    if (bitValueComparison == 1)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static int GetTopologyMaskFromDictionary(Dictionary<Topology, bool> input)
        {
            int result = 0;

            foreach (var entry in input)
            {
                result |= entry.Value ? (int)entry.Key : 0;
            }

            return result;
        }

        public static BuildingBlock TryGetAdjacentSpecialFoundation(BuildingBlock checker, out bool isInverse)
        {
            isInverse = false;

            float num = float.MaxValue;
            BuildingBlock result = null;
            Vector3 position = checker.PivotPoint();
            List<BuildingBlock> obj = Facepunch.Pool.GetList<BuildingBlock>();
            Vis.Entities(position, 3F, obj, /*2097152*/ConstructionMask);
            for (int i = 0; i < obj.Count; i++)
            {
                BuildingBlock buildingBlock = obj[i];

                if (buildingBlock == checker)
                {
                    continue;
                }

                if (!(buildingBlock.PrefabName == PREFAB_FLOOR_SQUARE || buildingBlock.PrefabName == PREFAB_FLOOR_TRIANGLE))
                {
                    continue;
                }


                //only take into account stuff with the same orientation
                if (Mathf.Abs(buildingBlock.transform.eulerAngles.z - buildingBlock.transform.eulerAngles.z) > 170F)
                {
                    continue;
                }

                if (Mathf.Abs(checker.transform.position.y - buildingBlock.transform.position.y) > 0.1F)
                {
                    continue;
                }

                if (!IsWaterFoundation(buildingBlock))
                {
                    if (!IsInverseFoundation(buildingBlock))
                    {
                        continue;
                    }
                    else
                    {
                        isInverse = true;
                    }
                }

                if (buildingBlock.isServer == checker.isServer)
                {
                    float num2 = buildingBlock.SqrDistance(position);
                    if (!buildingBlock.grounded)
                    {
                        num2 += 1f;
                    }
                    if (num2 < num)
                    {
                        num = num2;
                        result = buildingBlock;
                        break;
                    }
                }
            }
            Facepunch.Pool.FreeList(ref obj);
            return result;
        }
        public static void EnsureFoundationItemName(Item item)
        {
            if (!IsSkinRelevant(item.skin)) return;

            var properSkinName = item.skin == SKIN_FOUNDATION_SQUARE ? MSG(MSG_ITEM_NAME_SQUARE) : MSG(MSG_ITEM_NAME_TRIANGLE);

            if (item.name == properSkinName)
            {
                return;
            }

            item.name = properSkinName;
            item.MarkDirty();

        }

        public static Item MakeFoundationItem(ulong skin, int amount = 1)
        {
            if (!IsSkinRelevant(skin))
            {
                return null;
            }

            var newItem = ItemManager.CreateByItemID(ITEM_INNER_TUBE, amount, skin);

            EnsureFoundationItemName(newItem);

            return newItem;
        }

        public static bool IsSkinRelevant(ulong skin)
        {
            return RelevantSkins.Contains(skin);
        }

        public static void MakeStable(BaseEntity entity)
        {
            //make it 100% stable, remove groundwatch and destroy on ground missing.
            var groundWatch = entity.gameObject.GetComponent<GroundWatch>();

            if (groundWatch != null)
            {
                UnityEngine.Object.DestroyImmediate(groundWatch);
            }

            var groundMissing = entity.gameObject.GetComponent<DestroyOnGroundMissing>();

            if (groundMissing != null)
            {
                UnityEngine.Object.DestroyImmediate(groundMissing);
            }

            var maybeStability = entity as StabilityEntity;
            if (maybeStability != null)
            {
                maybeStability.grounded = true;
            }
        }

        public static void PostDeploymentChecks(BasePlayer player, BuildingBlock newWaterFoundation, bool deployedFromInflatable, out DeploymentResult result, int neededItemAmount = 1)
        {
            result = DeploymentResult.Success;

            if (player.IsBuildingBlocked())
            {
                result = DeploymentResult.BuildingBlocked;
                return;
            }

            if (Instance.configData.RestrictBuildingToMapGrid)
            {
                if (IsPositionOutsideMapGrid(newWaterFoundation.transform.position))
                {
                    result = DeploymentResult.OutsideMapGrid;
                    return;
                }
            }

            var prof = GetPermissionProfile(player);

            bool consume = false;
            List<Item> neededItems = null;

            if (deployedFromInflatable)
            {
                if (!prof.CanDeployWaterFoundations)
                {
                    result = DeploymentResult.NoPermissionToDeploy;
                    return;
                }
            }
            else
            {
                if (!prof.CanExpandWaterFoundations)
                {
                    result = DeploymentResult.NoPermissionToExpand;
                    return;
                }

                if (neededItemAmount > 0)
                {
                    neededItems = FindNeededFoundationItemsInInventory(player, newWaterFoundation.skinID, neededItemAmount);

                    if (neededItems == null)
                    {
                        result = DeploymentResult.NoRequiredItem;
                        return;
                    }
                    else
                    {
                        consume = true;
                    }
                }

            }

            if (!TopologyCheckMasks(Instance.configData.ProfileTopologyMask[prof.PermissionRequired], TerrainMeta.TopologyMap.GetTopologyFast(new Vector2(newWaterFoundation.transform.position.x, newWaterFoundation.transform.position.z))))
            {
                result = DeploymentResult.TopologyMismatch;
                return;
            }

            var depth = TerrainMeta.WaterMap.GetDepth(newWaterFoundation.transform.position);

            if (prof.WaterDepthMin != float.NegativeInfinity)
            {
                if (depth < prof.WaterDepthMin)
                {
                    result = DeploymentResult.WaterTooShallow;
                    return;
                }
            }

            if (prof.WaterDepthMax != float.PositiveInfinity)
            {
                if (depth > prof.WaterDepthMax)
                {
                    result = DeploymentResult.WaterTooDeep;
                    return;
                }
            }

            if (Instance.configData.RelyOnShoreDistance)
            {
                var distanceToShore = GetCoarseDistanceToShore(newWaterFoundation.transform.position);
                if (prof.MinDistanceFromShore != float.NegativeInfinity)
                {
                    if (distanceToShore < prof.MinDistanceFromShore)
                    {
                        result = DeploymentResult.TooCloseToShore;
                        return;
                    }
                }

                if (prof.MaxDistanceFromShore != float.PositiveInfinity)
                {
                    if (distanceToShore > prof.MaxDistanceFromShore)
                    {
                        result = DeploymentResult.TooFarFromShore;
                        return;
                    }
                }
            }


            var isInWaterVolume = false;
            var hits = Physics.RaycastAll(newWaterFoundation.transform.position, Vector3.down, 3F, DeployedMask);

            BaseEntity ent;
            RaycastHit hit;

            if (hits != null)
            {
                for (var h = 0; h < hits.Length; h++)
                {
                    hit = hits[h];
                    ent = hit.GetEntity();
                    if (ent == null)
                    {
                        continue;
                    }

                    if (ent.PrefabName == PREFAB_POOL_BIG || ent.PrefabName == PREFAB_POOL_SMALL)
                    {
                        isInWaterVolume = true;
                        break;
                    }
                }
            }

            if (isInWaterVolume)
            {
                result = DeploymentResult.DeployedInPool;
                return;
            }

            if (prof.MinDistanceFromOilrig != float.NegativeInfinity)
            {
                if (GetDistanceToNearestOilrig(newWaterFoundation.transform.position) < prof.MinDistanceFromOilrig)
                {
                    result = DeploymentResult.TooCloseToOilrig;
                    return;
                }
            }

            if (prof.MaxWaterFoundationsPerBuilding != int.MaxValue)
            {
                if (CountBuildingWaterFoundations(newWaterFoundation) > prof.MaxWaterFoundationsPerBuilding)
                {
                    result = DeploymentResult.TooManyFoundations;
                    return;
                }
            }

            if (Instance.configData.CargoShipPathHandling != CargoPathHandling.None)
            {
                if (TerrainMeta.Path.OceanPatrolFar?.Count > 0)
                {
                    foreach (var oceanNode in TerrainMeta.Path.OceanPatrolFar)
                    {
                        if (Vector3Ex.Distance2D(oceanNode, newWaterFoundation.transform.position) < Instance.configData.MinDistanceFromCargoShipNode)
                        {
                            result = DeploymentResult.OnCargoShipPath;
                            return;
                        }
                    }
                }
            }

            if (consume)
            {
                if (neededItems != null)
                {
                    ConsumeFromSpecificItemList(ref neededItems, neededItemAmount);
                }
            }
        }

        public static void TurnFloorIntoInverseFoundation(BuildingBlock inverseFloor, BuildingBlock waterFoundationAbove, BasePlayer buildingPlayer)
        {
            MakeStable(inverseFloor);

            inverseFloor.skinID = waterFoundationAbove.skinID == SKIN_FOUNDATION_SQUARE ? SKIN_INVERSE_FOUNDATION_SQUARE : SKIN_INVERSE_FOUNDATION_TRIANGLE;

            inverseFloor.SetParent(waterFoundationAbove, true, true);

            MonoiseInverseFoundation(inverseFloor);

            //are post deployment checks even needed?

            Barrelise(inverseFloor);
        }

        public static void TurnFloorIntoWaterFoundation(BuildingBlock floor, BasePlayer buildingPlayer, bool deployedFromInflatable)
        {
            MakeStable(floor);
            //parent some barrels on the bottom, make those stable.

            DeploymentResult result;

            PostDeploymentChecks(buildingPlayer, floor, deployedFromInflatable, out result);

            bool actuallySuccess = false;

            if (result != DeploymentResult.Success)
            {
                //if the result is OnCargoShip, but the handling is Warn, don't kill
                if (result == DeploymentResult.OnCargoShipPath && Instance.configData.CargoShipPathHandling == CargoPathHandling.Warn)
                {
                    actuallySuccess = true;
                }
            }
            else
            {
                actuallySuccess = true;
            }

            if (!actuallySuccess)
            {
                TellMessage(buildingPlayer, MSG_RESULT(result, buildingPlayer.UserIDString));

                if (deployedFromInflatable)
                {
                    //inverse foundation give you a double refund
                    buildingPlayer.GiveItem(MakeFoundationItem(floor.PrefabName == PREFAB_FLOOR_SQUARE ? SKIN_FOUNDATION_SQUARE : SKIN_FOUNDATION_TRIANGLE, IsInverseFoundation(floor) ? 2 : 1));
                }

                floor.Kill(BaseNetworkable.DestroyMode.Gib);
            }
            else
            {
                //must be warning, so still display
                if (result == DeploymentResult.OnCargoShipPath)
                {
                    TellMessage(buildingPlayer, MSG_RESULT(result, buildingPlayer.UserIDString));
                }
                //now if success spawn some stuff

                Barrelise(floor);

                ApplyUpkeepMultiplier(floor);
            }
        }

        public static BuildingBlock BuildInverseFoundationAtWaterFoundation(BasePlayer deployingPlayer, BuildingBlock waterFoundation)
        {

            var transf = GetWorldTransformRelativeTo(waterFoundation.transform, Vector3.down * 1.5F, new Vector3(0F, 0F, 180F));
            //spawn either a square or triangle floor...
            BuildingBlock inverseFloor = GameManager.server.CreateEntity(waterFoundation.skinID == SKIN_FOUNDATION_SQUARE ? PREFAB_FLOOR_SQUARE : PREFAB_FLOOR_TRIANGLE, transf.Item1, transf.Item2) as BuildingBlock;

            inverseFloor.OwnerID = deployingPlayer.userID;

            inverseFloor.AttachToBuilding(waterFoundation);

            inverseFloor.Spawn();
            inverseFloor.SetHealthToMax();

            Effect.server.Run(PREFAB_BUILD_EFFECT, inverseFloor, 0u, Vector3.zero, Vector3.zero);

            TurnFloorIntoInverseFoundation(inverseFloor, waterFoundation, deployingPlayer);

            return inverseFloor;
        }

        public static BuildingBlock BuildWaterFoundationAt(BasePlayer deployingPlayer, ulong foundationSkin, Vector3 position, Quaternion rotation)
        {
            if (!IsSkinRelevant(foundationSkin))
            {
                return null;
            }

            //spawn either a square or triangle floor...
            BuildingBlock newFloor = GameManager.server.CreateEntity(foundationSkin == SKIN_FOUNDATION_SQUARE ? PREFAB_FLOOR_SQUARE : PREFAB_FLOOR_TRIANGLE, position, rotation) as BuildingBlock;

            newFloor.OwnerID = deployingPlayer.userID;
            newFloor.skinID = foundationSkin;

            newFloor.AttachToBuilding(BuildingManager.server.NewBuildingID());

            newFloor.Spawn();
            newFloor.SetHealthToMax();

            Effect.server.Run(PREFAB_BUILD_EFFECT, newFloor, 0u, Vector3.zero, Vector3.zero);

            //var building = new BuildingManager.Building { ID = newFloor.buildingID };

            //BuildingManager.server.buildingDictionary.Add(newFloor.buildingID, building);

            //building.AddBuildingBlock(newFloor);
            //building.Dirty();

            TurnFloorIntoWaterFoundation(newFloor, deployingPlayer, true);
            return newFloor;
        }
        #endregion

    }
}
