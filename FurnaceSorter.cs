using System.Collections.Generic;
using System.Linq;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("FurnaceSorter", "https://vk.com/nastroykarust", "1.0.14", ResourceId = 23)]
    class FurnaceSorter : RustPlugin
    {
        #region Declarations

        const string permAllow = "furnacesorter.allow";

        bool _debuggingMode = false;

        Dictionary<string, Timer> _timers = new Dictionary<string, Timer>();

        Dictionary<ulong, BaseOven> _uiInfo = new Dictionary<ulong, BaseOven>();

        string _panelSorter = "PanelSorter";

        string _panelOnScreen = "PanelOnScreen";

        List<ulong> _enabled = new List<ulong>();

        List<ItemContainer> _sorting = new List<ItemContainer>();

        #endregion

        #region Classes

        public class UI
        {
            public static CuiElementContainer CreateOverlayContainer(string panelName, string color, string aMin, string aMax, bool cursor = false)
            {
                return new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image =
                            {
                                Color = color
                            },
                            RectTransform =
                            {
                                AnchorMin = aMin,
                                AnchorMax = aMax
                            },
                            CursorEnabled = cursor
                        },
                        new CuiElement().Parent = "Overlay",
                        panelName
                    }
                };
            }

            public static void CreateButton(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiButton
                {
                    Button =
                    {
                        Color = color,
                        Command = command,
                        FadeIn = 1.0f
                    },
                    RectTransform =
                    {
                        AnchorMin = aMin,
                        AnchorMax = aMax
                    },
                    Text =
                    {
                        Text = text,
                        FontSize = size,
                        Align = align
                    }
                }, panel);
            }

            public static void CreateTextOutline(ref CuiElementContainer element, string panel, string colorText, string colorOutline, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter)
            {
                element.Add(new CuiElement
                {
                    Parent = panel,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Color = colorText,
                            FontSize = size,
                            Align = align,
                            Text = text
                        },
                        new CuiOutlineComponent
                        {
                            Distance = "1 1",
                            Color = colorOutline
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = aMin,
                            AnchorMax = aMax
                        }
                    }
                });
            }
        }

        #endregion

        #region Lang

        void RegisterLang() => lang.RegisterMessages(_messages, this);

        Dictionary<string, string> _messages = new Dictionary<string, string>()
        {
            { "Title", "FurnaceSorter: " },
            { "NoPerm","You do not have permission to use this command" },

            { "FurnaceSorterDisabled", "FurnaceSorter is now disabled." },
            { "FurnaceOnSorterDisabled", "FurnaceSorter cannot be used while the furnace is on." },
            { "FurnaceSorterEnabled", "FurnaceSorter is now enabled." },

            { "ToggleOff", "SORTER: Turn OFF" },
            { "ToggleOn", "SORTER: Turn ON" },

            { "OptimizationUnavailable", "You can not use the Optimizer while the furnace is on!" },
            { "NothingToOptimize", "There is nothing in the furnace to optimize. Optimization Failed!" },
            { "NoWood", "The furnace does not appear to have wood. Optimization Failed!" },
            { "NoAcceptableItems", "The furnace does not appear to have any valid items to optimize wood against. Optimization Failed!" },
            { "WoodRatioGood", "The wood to acceptable item ratio is correct. No Optimization Required!" },
            { "WoodNeeded", "Optimizing found you are short {0} wood" },
            { "ExtraWoodGiven", "Optimizing found you have {0} extra wood. This wood has been placed in your inventory!" },
            { "InventoryFull", "Your furnace has {0} extra wood but it will not fit in your inventory. Optimization Failed!" },
            { "FurnaceOptimized", "Your furnace has been optimized!" },
        };

        #endregion

        #region Config

        float _default_minX = 0.646f;

        float _default_minY = 0.1f;

        float _default_maxX = 0.81f;

        float _defeault_maxY = 0.14f;

        ConfigData _configData;

        class ConfigData
        {
            public float minx { get; set; }

            public float miny { get; set; }

            public float maxx { get; set; }

            public float maxy { get; set; }
        }

        void LoadConfigData()
        {
            LoadConfigVariables();

            if (_configData == null) LoadDefaultConfig();

            SaveConfig();

            if (_configData.maxx == default(float) &&
                _configData.maxy == default(float) &&
                _configData.minx == default(float) &&
                _configData.miny == default(float))
            {
                _configData.minx = _default_minX;
                _configData.miny = _default_minY;
                _configData.maxx = _default_maxX;
                _configData.maxy = _defeault_maxY;

                SaveConfig(_configData);
            }
        }

        void LoadConfigVariables() => _configData = Config.ReadObject<ConfigData>();

        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);

        protected override void LoadDefaultConfig()
        {
            SaveConfig(new ConfigData
            {
                minx = _default_minX,
                miny = _default_minY,
                maxx = _default_maxX,
                maxy = _defeault_maxY,
            });
        }

        #endregion

        #region Hooks

        void Init()
        {
            RegisterLang();

            permission.RegisterPermission(permAllow, this);

            LoadConfigData();

            if (ConVar.Server.hostname == "PsychoTea's Testing Server")
            {
                _debuggingMode = true;
            }
        }

        void Unload()
        {
            BasePlayer.activePlayerList.ForEach(x => CloseUI(x));

            foreach (var entry in _timers)
            {
                entry.Value.Destroy();
            }

            _timers.Clear();
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (!_enabled.Contains(player.userID))
            {
                _enabled.Add(player.userID);
            }
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            CloseUI(player);
        }

        void OnPlayerRespawned(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, _panelOnScreen);
            CuiHelper.DestroyUi(player, _panelSorter);
        }

        void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (player == null || entity == null) return;

            if (!HasPerm(player)) return;

            BaseOven oven = entity as BaseOven;
            if (oven == null) return;

            if (!_uiInfo.ContainsKey(player.userID))
            {
                _uiInfo.Add(player.userID, oven);
            }

            _uiInfo[player.userID] = oven;

            SorterUI(player);
        }

        void OnPlayerLootEnd(PlayerLoot looter)
        {
            if (looter?.entitySource == null) return;

            if (!(looter.entitySource is BaseOven)) return;

            BasePlayer player = looter.GetComponent<BasePlayer>();
            if (player == null) return;

            if (_uiInfo.ContainsKey(player.userID))
            {
                _uiInfo[player.userID] = null;
            }

            CuiHelper.DestroyUi(player, _panelSorter);
        }

        object CanAcceptItem(ItemContainer container, Item item)
        {
            if (container == null ||
                container.entityOwner == null ||
                container.entityOwner.GetComponent<BaseOven>() == null ||
                item == null ||
                item.parent == container ||
                _sorting.Contains(container))
            {
                return null;
            }

            BasePlayer player = item.GetOwnerPlayer();
            if (player == null)
            {
                DebugMessage("No player");
                return null;
            }

            if (!HasPerm(player))
            {
                DebugMessage("No permission");
                return null;
            }

            if (!_enabled.Contains(player.userID))
            {
                DebugMessage("Not enabled");
                return null;
            }

            List<string> AcceptableItems = new List<string>();

            BaseOven oven = container.entityOwner.GetComponent<BaseOven>();
            string ovenName = oven.ShortPrefabName;

            if (ovenName.Contains("campfire") ||
                ovenName.Contains("hobobarrel") ||
                ovenName.Contains("bbq"))
            {
                AcceptableItems = new List<string>
                {
                    "bearmeat",
                    "deermeat.raw",
                    "humanmeat.raw",
                    "meat.boar",
                    "wolfmeat.raw",
                    "can.beans.empty",
                    "can.tuna.empty",
                    "chicken.raw",
                    "fish.raw"
                };
            }
            else if (ovenName.Contains("furnace"))
            {
                AcceptableItems = new List<string>
                {
                    "hq.metal.ore",
                    "metal.ore",
                    "sulfur.ore",
                    "can.beans.empty",
                    "can.tuna.empty"
               };
            }
            else if (ovenName.Contains("refinery"))
            {
                AcceptableItems = new List<string>
                {
                    "crude.oil"
                };
            }

            if (!AcceptableItems.Contains(item.info.shortname))
            {
                DebugMessage($"not acceptable item {item.info.shortname}");
                return null;
            }

            if (container.entityOwner.GetComponent<BaseOven>().IsOn())
            {
                _enabled.Remove(player.userID);
                OnScreen(player, "FurnaceOnSorterDisabled");
                SorterUI(player);
                return null;
            }

            var totalAmount = item.amount;

            DebugMessage($"TotalAmount = {totalAmount}");

            List<Item> lessThanMax = new List<Item>();
            List<Item> items = new List<Item>();

            bool containsCharcoal = false;

            foreach (var entry in container.itemList)
            {
                if (entry.info.displayName.english == "Charcoal")
                {
                    DebugMessage($"Contains Charcoal");

                    containsCharcoal = true;
                }

                if (entry.info.shortname == item.info.shortname)
                {
                    items.Add(entry);
                    totalAmount += entry.amount;

                    if (entry.amount < item.MaxStackable())
                    {
                        lessThanMax.Add(entry);
                    }

                    DebugMessage($"TotalAmount = {totalAmount}");
                }
            }

            var newSlots = container.capacity - container.itemList.Count();
            
            if (lessThanMax.Count == 0)
            {
                if (containsCharcoal && newSlots <= 1)
                {
                    return ItemContainer.CanAcceptResult.CannotAccept;
                }

                if (!containsCharcoal && newSlots <= 2)
                {
                    return ItemContainer.CanAcceptResult.CannotAccept;
                }
            }

            if (ovenName.Contains("refinery"))
            {
                newSlots -= 2;
            }
            else
            {
                newSlots -= containsCharcoal ? 1 : 2;
            }

            if (newSlots > totalAmount) newSlots = totalAmount;

            DebugMessage($"{newSlots} - Available Slots");

            var totalSlots = items.Count();

            if (totalSlots == 0)
            {
                totalSlots += newSlots;
            }
            else
            {
                newSlots = 0;
            }

            if (totalSlots > totalAmount)
            {
                totalSlots = totalAmount;
            }

            var remainder = totalAmount % totalSlots;

            DebugMessage($"Remainder: {remainder}");

            var SplitableAmount = totalAmount - remainder;

            DebugMessage($"SplitAmount: {SplitableAmount}");

            var eachStack = SplitableAmount / totalSlots;

            DebugMessage($"EachStack: {eachStack}");

            if (eachStack > item.MaxStackable())
            {
                eachStack = item.MaxStackable();

                remainder = totalAmount - (eachStack * totalSlots);
            }

            SortFurnace(player, container, item, eachStack, items, item.info.shortname, remainder, newSlots);

            return ItemContainer.CanAcceptResult.CannotAccept;
        }

        private Item FindBurnable(BaseOven oven)
        {
            foreach (var item in oven.inventory.itemList)
            {
                if (item.info.GetComponent<ItemModBurnable>() == null)
                {
                    continue;
                }

                if (oven.fuelType == null)
                {
                    continue;
                }

                if (oven.fuelType != item.info)
                {
                    continue;
                }

                return item;
            }

            return null;
        }

        #endregion

        #region Commands

        [ConsoleCommand("UI_ToggleSorter")]
        void UIToggleSorterConsoleCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null) return;

            if (!HasPerm(player)) return;

            if (!_enabled.Contains(player.userID))
            {
                _enabled.Add(player.userID);

                OnScreen(player, "FurnaceSorterEnabled");
                SorterUI(player);

                return;
            }

            _enabled.Remove(player.userID);

            OnScreen(player, "FurnaceSorterDisabled");
            SorterUI(player);
        }

        #endregion

        #region GUI

        void SorterUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, _panelSorter);

            var element = UI.CreateOverlayContainer(_panelSorter, "0 0 0 0", $"{_configData.minx} {_configData.miny}", $"{_configData.maxx} {_configData.maxy}");

            if (_enabled.Contains(player.userID)) UI.CreateButton(ref element, _panelSorter, "0.584 0.29 0.211 1.0", GetMessage("ToggleOff", player), 12, "0 0", "0.6 1", $"UI_ToggleSorter {3}");

            else UI.CreateButton(ref element, _panelSorter, "0.439 0.509 0.294 1.0", GetMessage("ToggleOn", player), 12, "0 0", "0.6 1", $"UI_ToggleSorter {3}");

            CuiHelper.AddUi(player, element);
        }

        void OnScreen(BasePlayer player, string msg)
        {
            if (_timers.ContainsKey(player.userID.ToString()))
            {
                _timers[player.userID.ToString()].Destroy();
                _timers.Remove(player.userID.ToString());
            }
            CuiHelper.DestroyUi(player, _panelOnScreen);
            var element = UI.CreateOverlayContainer(_panelOnScreen, "0.0 0.0 0.0 0.0", "0.3 0.5", "0.7 0.8");
            UI.CreateTextOutline(ref element, _panelOnScreen, string.Empty, "0 0 0 1", GetMessage(msg, player), 32, "0.0 0.0", "1.0 1.0");
            CuiHelper.AddUi(player, element);
            _timers.Add(player.userID.ToString(), timer.Once(4, () => CuiHelper.DestroyUi(player, _panelOnScreen)));
        }

        void CloseUI(BasePlayer player)
        {
            if (player == null) return;

            if (_uiInfo.ContainsKey(player.userID))
            {
                _uiInfo.Remove(player.userID);
            }

            if (_timers.ContainsKey(player.userID.ToString()))
            {
                _timers[player.userID.ToString()].Destroy();
                _timers.Remove(player.userID.ToString());
            }

            if (_enabled.Contains(player.userID))
            {
                _enabled.Remove(player.userID);
            }

            CuiHelper.DestroyUi(player, _panelOnScreen);

            CuiHelper.DestroyUi(player, _panelSorter);
        }

        #endregion

        #region Functions

        void SortFurnace(BasePlayer player, ItemContainer container, Item originalItem, int stackAmount, List<Item> existingItems, string shortname, int remainder, int newSlots)
        {
            _sorting.Add(container);

            DebugMessage("Starting Sort");

            ItemDefinition def = ItemManager.FindItemDefinition(shortname);

            foreach (var entry in existingItems.Where(x => x != null))
            {
                entry.amount = stackAmount;
            }

            while (newSlots > 0)
            {
                var newItem = ItemManager.Create(def, stackAmount);
                newItem.MoveToContainer(container, -1, false);

                newSlots--;
            }

            if (remainder > 0)
            {
                foreach (var entry in container.itemList.Where(k => k.info.shortname == originalItem.info.shortname))
                {
                    DebugMessage($"Amount: {entry.amount} - Remainder Amount: {remainder}");

                    if (entry.amount != entry.MaxStackable())
                    {
                        entry.amount++;
                        remainder--;

                        DebugMessage($"Amount: {entry.amount} - Remainder Amount: {remainder}");
                    }

                    if (remainder == 0)
                    {
                        DebugMessage($"Remainder = 0");

                        originalItem.RemoveFromContainer();
                        originalItem.Remove(0f);

                        break;
                    }

                    DebugMessage("Continuing...");
                }

                DebugMessage($"Remainder > 0 - Remainder Amount: {remainder}");
                DebugMessage($"OriginalItem Amount - {originalItem.amount}");

                originalItem.amount = remainder;

                DebugMessage($"OriginalItem Amount - {originalItem.amount}");

                originalItem.MarkDirty();
            }
            else
            {
                DebugMessage($"Remainder = 0");

                originalItem.RemoveFromContainer();
                originalItem.Remove(0f);
            }

            _sorting.Remove(container);
            container.MarkDirty();
        }

        #endregion

        #region Helpers

        void SendMessage(BasePlayer player, string key, params object[] args)
        {
            string message = GetMessage(key, player, args);

            SendReply(player, $"<color=orange>{GetMessage("Title", player)}</color><color=#A9A9A9>{message}</color>");
        }

        string GetMessage(string key, BasePlayer player = null, params object[] args)
        {
            return string.Format(lang.GetMessage(key, this, player?.UserIDString), args);
        }

        void DebugMessage(string message)
        {
            if (!_debuggingMode) return;

            Puts(message);
        }

        bool HasPerm(BasePlayer player, string perm = permAllow) => player.IsAdmin || permission.UserHasPermission(player.UserIDString, perm);

        #endregion
    }
}
