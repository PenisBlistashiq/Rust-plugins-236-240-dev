using System;
using System.Collections.Generic;
using System.Drawing;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Steamworks.ServerList;
using UnityEngine;
using Color = UnityEngine.Color;

namespace Oxide.Plugins
{
    [Info("UItemSort", "SkuliDropek", "1.0.2")]
    [Description("Universal Item Sort")]
    class UItemSort : RustPlugin
    {
        #region Classes

        private class PluginConfig
        {
            [JsonProperty("Use button images?")]
            public bool useImages;
            [JsonProperty("Send plugin messages/reply?")]
            public bool pluginReply;
            [JsonProperty("Sort button color.")]
            public string sortBttnColor;
            [JsonProperty("Take similar button color.")]
            public string similarBttnColor;
            [JsonProperty("Take all button color.")]
            public string allBttnColor;
            [JsonProperty("Sort image.")]
            public string sortImg;
            [JsonProperty("Similar image.")]
            public string similarImg;
            [JsonProperty("Take/Put all.")]
            public string allImg;
        }

        private class PluginInterface
        {
            public string ItemSort;
        }
        #endregion

        #region Variables

        private const string permissionUse = "uitemsort.use";
        private const string UI_Layer = "UI_UItemSort";
        private const string UI_LayerMain = "UI_UItemSortMain";

        private static PluginInterface _interface;
        private static PluginConfig _config;
        private static bool _initiated;

        #endregion

        #region Config

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _config = Config.ReadObject<PluginConfig>();
                if (_config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                for (var i = 0; i < 3; i++)
                {
                    PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                }

                LoadDefaultConfig();
                return;
            }

            ValidateConfig();
            SaveConfig();
        }

        private void ValidateConfig()
        {
            if (Interface.Oxide.CallHook("OnConfigValidate") != null)
            {
                PrintWarning("Using default configuration...");
                _config = GetDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }

        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                useImages = false,
                pluginReply = true,
                sortBttnColor = "0.968627453 0.921568632 0.882352948 0.2",
                similarBttnColor = "0.968627453 0.921568632 0.882352948 0.2",
                allBttnColor = "0.968627453 0.921568632 0.882352948 0.2",
                sortImg = "https://i.imgur.com/uEiuf3N.png",
                similarImg = "https://i.imgur.com/3PkbAH8.png",
                allImg = "https://i.imgur.com/beKBAl3.png",
            };
        }

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["UILootSimilar"] = "Такие же вещи, как у вас, были залутаны! (если есть)",
                ["UILootAll"] = "Вы взяли все, что смогли уместить!",
                ["UISort"] = "Предметы успешно отсортированы!",
                ["UIPutSimilar"] = "Вы положили такие же вещи, как в контейнере! (если есть)",
                ["UIPutAll"] = "Вы положили все, что смогло уместиться!"


            }, this, "ru");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["UILootSimilar"] = "Same items, that you already own, were looted! (if exist)",
                ["UILootAll"] = "You've looted everything you could!",
                ["UISort"] = "Items were successfully sorted!",
                ["UIPutSimilar"] = "You've put same items that are in a storage! (if exist)",
                ["UIPutAll"] = "You've put all the items you could!"

            }, this, "en");
        }

        private string GetLocal(string mesKey, string userId = null) => lang.GetMessage(mesKey, this, userId);

        #endregion

        #region Utils
        private void MoveItems(ItemContainer from, ItemContainer to)
        {
            var storageItems = from.itemList;
            var itemList = to.itemList;

            int i = storageItems.Count - 1;
            while (itemList.Count < to.capacity)
            {
                if (i < 0)
                    break;

                var storageItem = storageItems[i];
                storageItem.MoveToContainer(to);
                i--;
            }
        }

        private void MoveSimilarItems(ItemContainer from, ItemContainer to)
        {
            var storageItems = from.itemList;

            for (int i = storageItems.Count - 1; i >= 0; i--)
            {
                var contItem = storageItems[i];
                if (to.GetAmount(contItem.info.itemid, false) > 0)
                {
                    contItem.MoveToContainer(to);
                }

                if (to.itemList.Count >= to.capacity)
                    break; // inventory full;
            }
        }

        private void SortItemContainer(ItemContainer container)
        {
            if (container == null)
                return;
            var storageItems = container.itemList;

            try
            {
                foreach (var storageItem in storageItems.ToArray())
                {
                    storageItem.RemoveFromContainer();
                    storageItem.MoveToContainer(container);
                }

                storageItems.Sort((item, item1) =>
                    String.Compare(item.info.shortname, item1.info.shortname, StringComparison.Ordinal));

                int counter = 0;
                foreach (var storageItem in storageItems.ToArray())
                {
                    storageItem.position = counter++;
                }

                container.MarkDirty();
            }
            catch
            {
                //no check
            }
        }

        public void RegPermission(string name)
        {
            if (permission.PermissionExists(name)) return;
            permission.RegisterPermission(name, this);
        }

        public bool HasPermission(BasePlayer player, string name)
        {
            if (player.IsAdmin)
                return true;

            return permission.UserHasPermission(player.UserIDString, name);
        }

        public static string GetColor(string hex, float alpha = 1f)
        {
            if (hex.Length != 7) hex = "#FFFFFF";
            if (alpha < 0 || alpha > 1f) alpha = 1f;

            var color = ColorTranslator.FromHtml(hex);
            var r = Convert.ToInt16(color.R) / 255f;
            var g = Convert.ToInt16(color.G) / 255f;
            var b = Convert.ToInt16(color.B) / 255f;

            return $"{r} {g} {b} {alpha}";
        }

        #endregion

        #region PluginReference

        [PluginReference] private Plugin ImageLibrary;

        private void AddImage(string url)
        {
            if (ImageLibrary == null)
                return;

            if ((bool)ImageLibrary.Call("HasImage", url) == false)
                ImageLibrary.Call("AddImage", url, url);
        }

        private string GetImage(string name)
        {
            if (ImageLibrary == null)
                return string.Empty;

            return (string)ImageLibrary.Call("GetImage", name);
        }

        private bool IsReady() => (bool)(ImageLibrary?.Call("IsReady") ?? false);

        #endregion

        #region Hooks
        void OnServerInitialized()
        {
            _initiated = false;
            RegPermission(permissionUse);

            if (_config.useImages)
            {
                AddImage(_config.sortImg);
                AddImage(_config.similarImg);
                AddImage(_config.allImg);

                timer.Once(1f, ImagesChecker);
            }
            else
            {
                BuildInterface();
                _initiated = true;
            }
        }

        private void ImagesChecker()
        {
            if (IsReady() == false)
            {
                PrintWarning("Images still loading! Plugin is not ready.");
                timer.Once(5f, ImagesChecker);
                return;
            }

            BuildInterface();
            _initiated = true;
        }

        void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (_initiated == false)
                return;

            if (HasPermission(player, permissionUse) == false)
                return;

            if (entity is BasePlayer || entity is LootableCorpse || entity is BoxStorage)
                DrawUI(player);
        }

        void OnPlayerLootEnd(PlayerLoot inventory)
        {
            if (_initiated == false)
                return;

            BasePlayer player = inventory._baseEntity;

            if (player == null)
                return;

            DestroyUI(player);
        }
        #endregion

        #region Interface
        private void BuildInterface()
        {
            _interface = new PluginInterface();
            CuiElementContainer container = new CuiElementContainer
            {
                {
                    new CuiPanel
                    {
                        RectTransform =
                        {
                            AnchorMin = "0.5 0",
                            AnchorMax = "0.5 0",
                            OffsetMin = "199 86",
                            OffsetMax = "382 108"
                        },
                        Image =
                        {
                            Color = "1 0.52 0 0"
                        },
                    },
                    "Overlay", UI_Layer
                },
                {
                    new CuiElement
                    {
                        Parent = UI_Layer,
                        Name = "Image0",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = GetImage(_config.sortImg)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "0 0",
                                OffsetMin = "0 0",
                                OffsetMax = "22 22"
                            }
                        }
                    }
                },
                {
                    new CuiElement
                    {
                        Parent = UI_Layer,
                        Name = $"{UI_Layer}.Sort",
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Text = "Sort", Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", FontSize = 12
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "0 0",
                                OffsetMin = "{22} 0",
                                OffsetMax = "58 22"
                            },
                            new CuiOutlineComponent
                            {
                                Color = GetColor("#000000"), Distance = "0.5 0.5"
                            }
                        }
                    }
                },
                {
                    new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0 0",
                            OffsetMin = "0 0",
                            OffsetMax = "58 22"
                        },
                        Button = { Color = _config.sortBttnColor, Command = "UI_Sort sortloot"},
                        Text = { Text = "" }
                    }, $"{UI_Layer}", $"{UI_Layer}.2511"
                },
                {
                    new CuiElement
                    {
                        Parent = UI_Layer,
                        Name = "Image1",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = GetImage(_config.similarImg)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "0 0",
                                OffsetMin = "62 0",
                                OffsetMax = "84 22"
                            },
                        }
                    }
                },
                {
                    new CuiElement
                    {
                        Parent = UI_Layer,
                        Name = $"{UI_Layer}.TakeSimilar",
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Text = "Take Similar", Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", FontSize = 9
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "0 0",
                                OffsetMin = "{84} 0",
                                OffsetMax = "120 22"
                            },
                            new CuiOutlineComponent
                            {
                                Color = GetColor("#000000"), Distance = "0.5 0.5"
                            }
                        }
                    }
                },
                {
                    new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0 0",
                            OffsetMin = "62 0",
                            OffsetMax = "120 22"
                        },
                        Button = {Color = _config.similarBttnColor, Command = "UI_Sort takesimilar"},
                        Text = { Text = "" }
                    }, $"{UI_Layer}"
                },
                {
                    new CuiElement
                    {
                        Parent = UI_Layer,
                        Name = "Image2",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = GetImage(_config.allImg)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "0 0",
                                OffsetMin = "123 0",
                                OffsetMax = "145 22"
                            },
                        }
                    }
                },
                {
                    new CuiElement
                    {
                        Parent = UI_Layer,
                        Name = $"{UI_Layer}.TakeAll",
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Text = "Take All", Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", FontSize = 9
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "0 0",
                                OffsetMin = "{145} 0",
                                OffsetMax = "181 22"
                            },
                            new CuiOutlineComponent
                            {
                                Color = GetColor("#000000"), Distance = "0.5 0.5"
                            }
                        }
                    }
                },
                {
                    new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0 0",
                            OffsetMin = "124 0",
                            OffsetMax = "182 22"
                        },
                        Button = {Color = _config.allBttnColor, Command = "UI_Sort takeall"},
                        Text = { Text = "" }
                    }, $"{UI_Layer}"
                },

                {
                    new CuiPanel
                    {
                        RectTransform =
                        {
                            AnchorMin = "0.5 0",
                            AnchorMax = "0.5 0",
                            OffsetMin = "-72 340",
                            OffsetMax = "116 362"
                        },
                        Image =
                        {
                            Color = "1 0.52 0 0"
                        },
                    },
                    "Overlay", UI_LayerMain
                },
                {
                    new CuiElement
                    {
                        Parent = UI_LayerMain,
                        Name = "Image3",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = GetImage(_config.sortImg)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "0 0",
                                OffsetMin = "0 0",
                                OffsetMax = "22 22"
                            }
                        }
                    }
                },
                {
                    new CuiElement
                    {
                        Parent = UI_LayerMain,
                        Name = $"{UI_LayerMain}.SortMain",
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Text = "Sort", Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", FontSize = 12
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "0 0",
                                OffsetMin = "{18} 0",
                                OffsetMax = "60 22"
                            },
                            new CuiOutlineComponent
                            {
                                Color = GetColor("#000000"), Distance = "0.5 0.5"
                            }
                        }
                    }
                },
                {
                    new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0 0",
                            OffsetMin = "0 0",
                            OffsetMax = "60 22"
                        },
                        Button = { Color = _config.sortBttnColor, Command = "UI_Sort sortmain"},
                        Text = { Text = "" }
                    }, $"{UI_LayerMain}"
                },
                {
                    new CuiElement
                    {
                        Parent = UI_LayerMain,
                        Name = "Image4",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = GetImage(_config.similarImg)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "0 0",
                                OffsetMin = "64 0",
                                OffsetMax = "86 22"
                            },
                        }
                    }
                },
                {
                    new CuiElement
                    {
                        Parent = UI_LayerMain,
                        Name = $"{UI_LayerMain}.PutSimilar",
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Text = "Put Similar", Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", FontSize = 9
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "0 0",
                                OffsetMin = "{86} 0",
                                OffsetMax = "124 22"
                            },
                            new CuiOutlineComponent
                            {
                                Color = GetColor("#000000"), Distance = "0.5 0.5"
                            }
                        }
                    }
                },
                {
                    new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0 0",
                            OffsetMin = "64 0",
                            OffsetMax = "124 22"
                        },
                        Button = { Color = _config.similarBttnColor, Command = "UI_Sort movesimilar"},
                        Text = { Text = "" }
                    }, $"{UI_LayerMain}"
                },
                {
                    new CuiElement
                    {
                        Parent = UI_LayerMain,
                        Name = "Image5",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = GetImage(_config.allImg)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "0 0",
                                OffsetMin = "128 0",
                                OffsetMax = "150 22"
                            },
                        }
                    }
                },
                {
                    new CuiElement
                    {
                        Parent = UI_LayerMain,
                        Name = $"{UI_LayerMain}.PutAll",
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Text = "Put All", Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", FontSize = 9
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "0 0",
                                OffsetMin = "{150} 0",
                                OffsetMax = "188 22"
                            },
                            new CuiOutlineComponent
                            {
                                Color = GetColor("#000000"), Distance = "0.5 0.5"
                            }
                        }
                    }
                },
                {
                    new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0 0",
                            OffsetMin = "128 0",
                            OffsetMax = "188 22"
                        },
                        Button = { Color = _config.allBttnColor, Command = "UI_Sort moveall"},
                        Text = { Text = "" }
                    }, $"{UI_LayerMain}"
                },
            };
            _interface.ItemSort = container.ToJson();
        }

        private void DrawUI(BasePlayer player)
        {
            string interfaceHolder = _interface.ItemSort.Replace("{UILootAll}", GetLocal("UILootAll", player.UserIDString));
            if (_config.useImages == false)
            {
                interfaceHolder = interfaceHolder.Replace("{22}", "0");
                interfaceHolder = interfaceHolder.Replace("{84}", "62");
                interfaceHolder = interfaceHolder.Replace("{145}", "123");
                interfaceHolder = interfaceHolder.Replace("{18}", "0");
                interfaceHolder = interfaceHolder.Replace("{86}", "64");
                interfaceHolder = interfaceHolder.Replace("{150}", "128");
            }
            else
            {
                interfaceHolder = interfaceHolder.Replace("{22}", "22");
                interfaceHolder = interfaceHolder.Replace("{84}", "84");
                interfaceHolder = interfaceHolder.Replace("{145}", "145");
                interfaceHolder = interfaceHolder.Replace("{18}", "18");
                interfaceHolder = interfaceHolder.Replace("{86}", "86");
                interfaceHolder = interfaceHolder.Replace("{150}", "150");
            }
            DestroyUI(player);
            CuiHelper.AddUi(player, interfaceHolder);

            if (_config.useImages == false)
            {
                for (int i = 0; i < 6; i++) // 6 - number of buttons
                    CuiHelper.DestroyUi(player, "Image" + i);
            }
        }

        private void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, UI_Layer);
            CuiHelper.DestroyUi(player, UI_LayerMain);
        }

        #endregion

        #region Commands

        [ConsoleCommand("UI_Sort")]
        private void Console_SortItems(ConsoleSystem.Arg arg)
        {
            if (_initiated == false)
                return;

            BasePlayer player = arg?.Player();
            if (player == null)
                return;

            if (arg.Args == null || arg.Args.Length < 1)
                return;

            var containers = player.inventory.loot.containers;
            if (containers == null)
                return;

            switch (arg.Args[0])
            {
                case "sortloot":
                    {
                        for (var i = 0; i < containers.Count; i++)
                            SortItemContainer(containers[i]);

                        if (_config.pluginReply)
                            SendReply(player, GetLocal("UISort", player.UserIDString));
                        break;
                    }
                case "sortmain":
                    {
                        SortItemContainer(player.inventory.containerMain);

                        if (_config.pluginReply)
                            SendReply(player, GetLocal("UISort", player.UserIDString));
                        break;
                    }
                case "takesimilar":
                    {
                        for (var i = 0; i < containers.Count; i++)
                            MoveSimilarItems(containers[i], player.inventory.containerMain);

                        if (_config.pluginReply)
                            SendReply(player, GetLocal("UILootSimilar", player.UserIDString));
                        break;
                    }
                case "takeall":
                    {
                        for (var i = 0; i < containers.Count; i++)
                            MoveItems(containers[i], player.inventory.containerMain);

                        if (_config.pluginReply)
                            SendReply(player, GetLocal("UILootAll", player.UserIDString));
                        break;
                    }
                case "movesimilar":
                    {
                        for (var i = 0; i < containers.Count; i++)
                            MoveSimilarItems(player.inventory.containerMain, containers[i]);

                        if (_config.pluginReply)
                            SendReply(player, GetLocal("UIPutSimilar", player.UserIDString));
                        break;
                    }
                case "moveall":
                    {
                        for (var i = 0; i < containers.Count; i++)
                            MoveItems(player.inventory.containerMain, containers[i]);

                        if (_config.pluginReply)
                            SendReply(player, GetLocal("UIPutAll", player.UserIDString));
                        break;
                    }
            }

        }

        #endregion
    }
}