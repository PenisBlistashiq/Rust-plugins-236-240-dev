using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Rust;
using System.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Oxide.Core;
using System.Globalization;
using Newtonsoft.Json.Linq;


namespace Oxide.Plugins
{
    [Info("TownWars", "Qbis", "1.0.4")]
    public class TownWars : RustPlugin
    {
        #region [Vars]
        [PluginReference] Plugin ImageLibrary, Clans;

        private List<GameObject> townObjects = new List<GameObject>();
        private Dictionary<string, List<inventoryData>> inventorysCache = new Dictionary<string, List<inventoryData>>();
        private static TownWars plugin;

        public class townsData
        {
            public string OwnerID;
            public string OwnerName;
            public int lastReward;
            public int lastCapture;
        }

        public class inventoryData
        {
            public string shortName;
            public ulong skinID;
            public int amount;
            public string customName;
            public int itemID;
            public string command;
        }
        #endregion

        #region [Config]
        private PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            config = PluginConfig.DefaultConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();

            if (config.PluginVersion < Version)
                UpdateConfigValues();

            Config.WriteObject(config, true);
        }

        private void UpdateConfigValues()
        {
            PluginConfig baseConfig = PluginConfig.DefaultConfig();
            if (config.PluginVersion < Version)
            {
                if(Version == new VersionNumber(1, 0, 1))
                {
                    foreach (var rewards in config.towns)
                        foreach (var reward in rewards.Value.rewards)
                            reward.command = "";

                    config.settings.minPlayers = 0;
                }
                config.PluginVersion = Version;
                PrintWarning("Config checked completed!");
            }
            config.PluginVersion = Version;
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        private class PluginConfig
        {
            [JsonProperty("Общие настройки")]
            public Settings settings;

            [JsonProperty("Настройки маркеров")]
            public MarkersSettings marker;

            [JsonProperty("Настройки РТ")]
            public Dictionary<string, TownSettings> towns;

            [JsonProperty("Настройки UI")]
            public UiSettings ui;

            [JsonProperty("Версия конфига")]
            public VersionNumber PluginVersion = new VersionNumber();

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    settings = new Settings()
                    {
                        typeTeam = "Clans",
                        addRewardSettings = 1800,
                        allCanTakeReward = false,
                        allCanStartCapt = false,
                        clearInventoryAfterWipe = true,
                        delayCapture = 14400,
                        durCapture = 1800,
                        addSphere = true,
                        minPlayers = 0
                    },
                    marker = new MarkersSettings()
                    {
                        markerRadius = 0.5f,
                        markerAlpha = 0.4f,
                        addMarkerWithLast = true,
                        markerColorCanCapture = "#10c916",
                        markerColorCantCapture = "#ffb700",
                        markerColorCapture = "#ed0707"
                    },
                    towns = new Dictionary<string, TownSettings>()
                    {
                        ["assets/bundled/prefabs/autospawn/monument/large/trainyard_1.prefab"] = new TownSettings()
                        {
                            name = "Железнодорожное Депо",
                            capDist = 85,
                            rewards = new List<Reward>()
                            {
                                new Reward()
                                {
                                    shortName = "stones",
                                    amount = 10000,
                                    skinID = 0,
                                    customName = "",
                                    imageUrl = "",
                                    command = ""
                                },
                                new Reward()
                                {
                                    shortName = "sulfur",
                                    amount = 5000,
                                    skinID = 0,
                                    customName = "",
                                    imageUrl = ""
                                },
                                new Reward()
                                {
                                    shortName = "rifle.ak",
                                    amount = 1,
                                    skinID = 0,
                                    customName = "",
                                    imageUrl = "",
                                    command = ""
                                },
                                new Reward()
                                {
                                    shortName = "techparts",
                                    amount = 10,
                                    skinID = 0,
                                    customName = "",
                                    imageUrl = "",
                                    command = ""
                                },
                                new Reward()
                                {
                                    shortName = "lmg.m249",
                                    amount = 1,
                                    skinID = 0,
                                    customName = "",
                                    imageUrl = "",
                                    command = ""
                                },
                                new Reward()
                                {
                                    shortName = "weapon.mod.8x.scope",
                                    amount = 1,
                                    skinID = 0,
                                    customName = "",
                                    imageUrl = "",
                                    command = ""
                                },
                                new Reward()
                                {
                                    shortName = "glue",
                                    amount = 10,
                                    skinID = 2409891781,
                                    customName = "Баксы",
                                    imageUrl = "https://i.imgur.com/jBaVKHu.png",
                                    command = ""
                                }
                            }

                        },
                        ["assets/bundled/prefabs/autospawn/monument/large/excavator_1.prefab"] = new TownSettings()
                        {
                            name = "Большой Екскаватор",
                            capDist = 85,
                            rewards = new List<Reward>()
                            {
                                new Reward()
                                {
                                    shortName = "stones",
                                    amount = 10000,
                                    skinID = 0,
                                    customName = "",
                                    imageUrl = "",
                                    command = ""
                                },
                                new Reward()
                                {
                                    shortName = "sulfur",
                                    amount = 5000,
                                    skinID = 0,
                                    customName = "",
                                    imageUrl = ""
                                },
                                new Reward()
                                {
                                    shortName = "rifle.ak",
                                    amount = 1,
                                    skinID = 0,
                                    customName = "",
                                    imageUrl = "",
                                    command = ""
                                },
                                new Reward()
                                {
                                    shortName = "techparts",
                                    amount = 10,
                                    skinID = 0,
                                    customName = "",
                                    imageUrl = "",
                                    command = ""
                                },
                                new Reward()
                                {
                                    shortName = "lmg.m249",
                                    amount = 1,
                                    skinID = 0,
                                    customName = "",
                                    imageUrl = "",
                                    command = ""
                                },
                                new Reward()
                                {
                                    shortName = "weapon.mod.8x.scope",
                                    amount = 1,
                                    skinID = 0,
                                    customName = "",
                                    imageUrl = "",
                                    command = ""
                                },
                                new Reward()
                                {
                                    shortName = "glue",
                                    amount = 10,
                                    skinID = 2409891781,
                                    customName = "Баксы",
                                    imageUrl = "https://i.imgur.com/jBaVKHu.png",
                                    command = ""
                                }
                            }

                        },
                        ["assets/bundled/prefabs/autospawn/monument/xlarge/launch_site_1.prefab"] = new TownSettings()
                        {
                            name = "Космодром",
                            capDist = 100,
                            rewards = new List<Reward>()
                            {
                                new Reward()
                                {
                                    shortName = "stones",
                                    amount = 10000,
                                    skinID = 0,
                                    customName = "",
                                    imageUrl = "",
                                    command = ""
                                },
                                new Reward()
                                {
                                    shortName = "sulfur",
                                    amount = 5000,
                                    skinID = 0,
                                    customName = "",
                                    imageUrl = "",
                                    command = ""
                                },
                                new Reward()
                                {
                                    shortName = "rifle.ak",
                                    amount = 1,
                                    skinID = 0,
                                    customName = "",
                                    imageUrl = "",
                                    command = ""
                                },
                                new Reward()
                                {
                                    shortName = "glue",
                                    amount = 10,
                                    skinID = 2409891781,
                                    customName = "Баксы",
                                    imageUrl = "https://i.imgur.com/jBaVKHu.png",
                                    command = ""
                                }
                            }

                        },
                        ["assets/bundled/prefabs/autospawn/monument/xlarge/military_tunnel_1.prefab"] = new TownSettings()
                        {
                            name = "Военный тунель",
                            capDist = 50,
                            rewards = new List<Reward>()
                            {
                                new Reward()
                                {
                                    shortName = "stones",
                                    amount = 10000,
                                    skinID = 0,
                                    customName = "",
                                    imageUrl = "",
                                    command = ""
                                },
                                new Reward()
                                {
                                    shortName = "sulfur",
                                    amount = 5000,
                                    skinID = 0,
                                    customName = "",
                                    imageUrl = "",
                                    command = ""
                                },
                                new Reward()
                                {
                                    shortName = "rifle.ak",
                                    amount = 1,
                                    skinID = 0,
                                    customName = "",
                                    imageUrl = "",
                                    command = ""
                                },
                                new Reward()
                                {
                                    shortName = "glue",
                                    amount = 10,
                                    skinID = 2409891781,
                                    customName = "Баксы",
                                    imageUrl = "https://i.imgur.com/jBaVKHu.png",
                                    command = ""
                                }
                            }

                        },
                        ["assets/bundled/prefabs/autospawn/monument/large/powerplant_1.prefab"] = new TownSettings()
                        {
                            name = "Электростанция",
                            capDist = 85,
                            rewards = new List<Reward>()
                            {
                                new Reward()
                                {
                                    shortName = "stones",
                                    amount = 10000,
                                    skinID = 0,
                                    customName = "",
                                    imageUrl = "",
                                    command = ""
                                },
                                new Reward()
                                {
                                    shortName = "sulfur",
                                    amount = 5000,
                                    skinID = 0,
                                    customName = "",
                                    imageUrl = "",
                                    command = ""
                                },
                                new Reward()
                                {
                                    shortName = "rifle.ak",
                                    amount = 1,
                                    skinID = 0,
                                    customName = "",
                                    imageUrl = "",
                                    command = ""
                                },
                                new Reward()
                                {
                                    shortName = "glue",
                                    amount = 10,
                                    skinID = 2409891781,
                                    customName = "Баксы",
                                    imageUrl = "https://i.imgur.com/jBaVKHu.png",
                                    command = ""
                                }
                            }

                        },
                        ["assets/bundled/prefabs/autospawn/monument/large/water_treatment_plant_1.prefab"] = new TownSettings()
                        {
                            name = "Водонапорка",
                            capDist = 85,
                            rewards = new List<Reward>()
                            {
                                new Reward()
                                {
                                    shortName = "stones",
                                    amount = 10000,
                                    skinID = 0,
                                    customName = "",
                                    imageUrl = "",
                                    command = ""
                                },
                                new Reward()
                                {
                                    shortName = "sulfur",
                                    amount = 5000,
                                    skinID = 0,
                                    customName = "",
                                    imageUrl = "",
                                    command = ""
                                },
                                new Reward()
                                {
                                    shortName = "rifle.ak",
                                    amount = 1,
                                    skinID = 0,
                                    customName = "",
                                    imageUrl = "",
                                    command = ""
                                },
                                new Reward()
                                {
                                    shortName = "glue",
                                    amount = 10,
                                    skinID = 2409891781,
                                    customName = "Баксы",
                                    imageUrl = "https://i.imgur.com/jBaVKHu.png",
                                    command = ""
                                }
                            }

                        },
                        ["assets/bundled/prefabs/autospawn/monument/small/sphere_tank.prefab"] = new TownSettings()
                        {
                            name = "Сфера",
                            capDist = 40,
                            rewards = new List<Reward>()
                            {
                                new Reward()
                                {
                                    shortName = "stones",
                                    amount = 10000,
                                    skinID = 0,
                                    customName = "",
                                    imageUrl = "",
                                    command = ""
                                },
                                new Reward()
                                {
                                    shortName = "sulfur",
                                    amount = 5000,
                                    skinID = 0,
                                    customName = "",
                                    imageUrl = "",
                                    command = ""
                                },
                                new Reward()
                                {
                                    shortName = "rifle.ak",
                                    amount = 1,
                                    skinID = 0,
                                    customName = "",
                                    imageUrl = "",
                                    command = ""
                                },
                                new Reward()
                                {
                                    shortName = "glue",
                                    amount = 10,
                                    skinID = 2409891781,
                                    customName = "Баксы",
                                    imageUrl = "https://i.imgur.com/jBaVKHu.png",
                                    command = ""
                                }
                            }

                        },
                        ["assets/bundled/prefabs/autospawn/monument/small/satellite_dish.prefab"] = new TownSettings()
                        {
                            name = "Спутниковая тарелка",
                            capDist = 40,
                            rewards = new List<Reward>()
                            {
                                new Reward()
                                {
                                    shortName = "stones",
                                    amount = 10000,
                                    skinID = 0,
                                    customName = "",
                                    imageUrl = "",
                                    command = ""
                                },
                                new Reward()
                                {
                                    shortName = "sulfur",
                                    amount = 5000,
                                    skinID = 0,
                                    customName = "",
                                    imageUrl = "",
                                    command = ""
                                },
                                new Reward()
                                {
                                    shortName = "rifle.ak",
                                    amount = 1,
                                    skinID = 0,
                                    customName = "",
                                    imageUrl = "",
                                    command = ""
                                },
                                new Reward()
                                {
                                    shortName = "glue",
                                    amount = 10,
                                    skinID = 2409891781,
                                    customName = "Баксы",
                                    imageUrl = "https://i.imgur.com/jBaVKHu.png",
                                    command = ""
                                }
                            }

                        }
                    },
                    ui = new UiSettings()
                    {
                        colorBG = "0 0 0 0.3",
                        colorLines = "0.75 0.60 0.20 1.00",
                        colorButtonCapture = "0.00 0.17 0.33 1",
                        colorButtonInventory = "0.00 0.17 0.33 1"
                    },
                    PluginVersion = new VersionNumber()

                };
            }
        }

        public class Settings
        {
            [JsonProperty("Тип работы плагина Team - обычные команды(зеленка), Solo - для соло захвата, Clans - поддержка кланов ClanReborn(Chaos), ClansUI(RP), Clans(Umod)")]
            public string typeTeam;

            [JsonProperty("Переодичность выдачи наград в секундах")]
            public int addRewardSettings;

            [JsonProperty("Предметы из инвентаря может забрать только глава клана")]
            public bool allCanTakeReward;

            [JsonProperty("Начать захват может только лидер клана или группы")]
            public bool allCanStartCapt;

            [JsonProperty("Удалять ли инвентарь предметов после вайпа")]
            public bool clearInventoryAfterWipe;

            [JsonProperty("Откат до следующего захвата в секундах")]
            public int delayCapture;

            [JsonProperty("Сколько секунд длится захват")]
            public int durCapture;

            [JsonProperty("Добавлять ли видимые сферы, для обозначения границ захвата")]
            public bool addSphere;

            [JsonProperty("Минимум игроков на сервере для начала захвата (0 - выкл)")]
            public int minPlayers;
        }

        public class MarkersSettings
        {
            [JsonProperty("Радиус маркера")]
            public float markerRadius;

            [JsonProperty("Прозрачность маркера")]
            public float markerAlpha;

            [JsonProperty("Цвет маркера когда РТ можно захватить")]
            public string markerColorCanCapture;

            [JsonProperty("Цвет маркера когда РТ захватывают")]
            public string markerColorCapture;

            [JsonProperty("Цвет маркера когда РТ нельзя захваить")]
            public string markerColorCantCapture;

            [JsonProperty("Добавлять ли название на карту маркер с именем, кто последний захватил РТ")]
            public bool addMarkerWithLast;
        }

        public class TownSettings
        {
            [JsonProperty("Название РТ")]
            public string name;

            [JsonProperty("На каком расстояние от центра РТ начислять очки захвата")]
            public int capDist;

            [JsonProperty("Список наград")]
            public List<Reward> rewards;
        }

        public class UiSettings
        {
            [JsonProperty("Цвет фона")]
            public string colorBG;

            [JsonProperty("Цвет обводки")]
            public string colorLines;

            [JsonProperty("Цвет кнопки 'Начать захват'")]
            public string colorButtonCapture;

            [JsonProperty("Цвет кнопки 'Инвентарь'")]
            public string colorButtonInventory;
        }

        public class Reward
        {
            [JsonProperty("Shortname предмета")]
            public string shortName;

            [JsonProperty("Количество предмета")]
            public int amount;

            [JsonProperty("Скин айди предмета")]
            public ulong skinID;

            [JsonProperty("Имя предмета (если кастом)")]
            public string customName;

            [JsonProperty("Ссылка на картинку (если кастом)")]
            public string imageUrl;

            [JsonProperty("Команда для выполнения %STEAMID% (для загрузки картинки придумайте любой номер SkinID и ShortName)")]
            public string command;
        }
        #endregion

        #region [Localization⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠]
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Solo"] = "player",
                ["Team"] = "player's team",
                ["Clans"] = "clan",
                ["CanCapt"] = "{1} under control <color=#B33A00>{0}</color> \n\n <color=#626262> To capture /tw </color>",
                ["CantCapt"] = "{4} under control <color=#B33A00>{0}</color>\nCapturing is possible in <color=#626262>{1}h. {2}m. {3}s.</color>",
                ["Capt"] = "Capturing {6} \nMost points for <color=#B33A00>{0}</color> - <color=#006A00> {1} </color>\nYour team has <color=#004000>{2}</color>\n\nTo end of capture <color=#626262> {3}h. {4}m. {5}sec. </color>",
                ["vendCapt"] = "Идет захват",
                ["StartCapt"] = "<size=20>Started capturing <color=#B33A00>{0}</color></size>",
                ["StopCapt"] = "<size=20><color=#B33A00>{0}</color> captured <color=#B33A00>{1}</color></size>",
                ["UI_Inventory"] = "<color=#DADADA>Inventory</color>",
                ["UI_Return"] = "<color=#DADADA>Return</color>",
                ["UI_Capture"] = "<color=#DADADA>Starting capture {0}</color",
                ["UI_TownName"] = "Name",
                ["UI_OwnerName"] = "Owner",
                ["UI_Status"] = "Status",
                ["UI_Capturing"] = "<color=#ed0707>Capture is underway</color>",
                ["UI_CantCapture"] = "<color=#ffb700>Was recently captured</color>",
                ["UI_CanCapture"] = "<color=#10c916>You can capture</color>",
                ["UI_Solo"] = "Player",
                ["UI_Team"] = "Player's team",
                ["UI_Clans"] = "Clan",
                ["UI_InventoryEmpty"] = "Your inventory is empty, grab the RT first",
                ["UI_NeedClan"] = "To start capturing, you need to create a clan",
                ["UI_OnlyOwner"] = "Only the clan head can do it",
                ["UI_Reward"] = "Reward from {0}",
                ["UI_LimitPlayers"] = "Capture cannot be started when there are less than {0} players on the server"
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Solo"] = "игрока",
                ["Team"] = "команды игрока",
                ["Clans"] = "клана",
                ["CanCapt"] = "{1} под контролем <color=#B33A00>{0}</color>\n\n<color=#626262>Для захвата /tw</color>",
                ["CantCapt"] = "{4} под контролем <color=#B33A00>{0}</color>\nЗахват возможен через <color=#626262>{1}ч. {2}м. {3}с.</color>",
                ["Capt"] = "Идет захват\nБольше всего очков у <color=#B33A00>{0}</color> - <color=#006A00>{1}</color>\nУ вашей команды <color=#004000>{2}</color>\n\nДо конца захвата <color=#626262>{3}ч. {4}м. {5}с.</color>",
                ["vendCapt"] = "Идет захват",
                ["StartCapt"] = "<size=20>Начался захват <color=#B33A00>{0}</color></size>",
                ["StopCapt"] = "<size=20><color=#B33A00>{0}</color> захватил(и) <color=#B33A00>{1}</color></size>",
                ["UI_Inventory"] = "<color=#DADADA>Инвентарь</color>",
                ["UI_Return"] = "<color=#DADADA>Вернуться</color>",
                ["UI_Capture"] = "<color=#DADADA>Начать захват {0}</color>",
                ["UI_TownName"] = "Название",
                ["UI_OwnerName"] = "Владелец",
                ["UI_Status"] = "Состояние",
                ["UI_Capturing"] = "<color=#ed0707>Идет захват</color>",
                ["UI_CantCapture"] = "<color=#ffb700>Был недавно захвачен</color>",
                ["UI_CanCapture"] = "<color=#10c916>Можно захватить</color>",
                ["UI_Solo"] = "Игрок",
                ["UI_Team"] = "Команда игрока",
                ["UI_Clans"] = "Клан",
                ["UI_InventoryEmpty"] = "Ваш инвентарь пуст, сначала захватите РТ",
                ["UI_NeedClan"] = "Для начала захвата нужно создать клан",
                ["UI_OnlyOwner"] = "Только глава клана может сделать это",
                ["UI_Reward"] = "Награды с {0}",
                ["UI_LimitPlayers"] = "Нельзя начать захват, когда на сервере игроков меньше {0}"
            }, this, "ru");
        }
        string GetMsg(string key, BasePlayer player = null) => lang.GetMessage(key, this, player?.UserIDString);
        string GetMsg(string key) => lang.GetMessage(key, this);
        #endregion

        #region [Oxide]
        private void OnServerInitialized()
        {
            LoadTownsData();

            foreach (var towns in config.towns)
                foreach (var reward in towns.Value.rewards)
                    if (!String.IsNullOrEmpty(reward.imageUrl))
                        ImageLibrary?.Call("AddImage", reward.imageUrl, reward.shortName +  reward.skinID);

            foreach (var player in BasePlayer.activePlayerList)
            {
                OnPlayerConnected(player);
            }
        }


        private void Init()
        {
            plugin = this;
        }

        private void Unload()
        {
            SaveInventorys();
            SaveTownsData();
            foreach (var go in townObjects)
            {
                UnityEngine.Object.Destroy(go);
            }
        }

        private void OnNewSave()
        {
            if (config.settings.clearInventoryAfterWipe)
            {
                wipeInventorys();
                PrintWarning("Inventorys data are cleared!");
            }

            clearTownsData();
            PrintWarning("Towns data are cleared!");
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.Once(2, () => OnPlayerConnected(player));
                return;
            }

            var clan = GetPlayerClan(player);
            if (String.IsNullOrEmpty(clan))
                return;

            if (!inventorysCache.ContainsKey(clan))
                AddInventoryToCache(clan);

        }

        private object OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (player == null) return null;

            CheckPlayerInZone(player);
            return null;
        }

        #endregion

        #region [Func]
        private void CheckPlayerInZone(BasePlayer player)
        {
            foreach(var obj in townObjects)
            {
                var capt = obj.GetComponent<Capture>();
                if (capt == null)
                    continue;

                if (capt.removePlayerFromZone(player))
                    return;
            }
        }

        private void BroadCastToStartCapture(string rt)
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (!player.IsConnected)
                    continue;

                player.ChatMessage(String.Format(GetMsg("StartCapt", player), rt));
            }
        }

        private void BroadCastToStopCapture(string owner, string rt)
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (!player.IsConnected)
                    continue;

                player.ChatMessage(String.Format(GetMsg("StopCapt", player), owner, rt));
            }
        }

        private string GetPlayerClan(BasePlayer player)
        {
            switch (config.settings.typeTeam)
            {
                case "Solo": return player.UserIDString;
                case "Team":
                    {
                        if (player.currentTeam == 0UL)
                            return null;

                        return player.currentTeam.ToString();
                    }
                case "Clans":
                    {
                        var clanName = Clans?.Call<string>("GetClanOf", player.userID);
                        return clanName ?? null;
                    }

                default:
                    return null;
            }
        }

        private string GetPlayerClanName(BasePlayer player)
        {
            switch (config.settings.typeTeam)
            {
                case "Solo":
                case "Team":
                    return player.displayName;


                case "Clans":
                    {
                        var clanName = Clans?.Call<string>("GetClanOf", player.userID);
                        return clanName ?? null;
                    }

                default:
                    return null;
            }
        }

        private bool isPlayerOwner(BasePlayer player)
        {
            switch (config.settings.typeTeam)
            {
                case "Solo": return true;
                case "Team":
                    {
                        if (player.currentTeam == 0UL)
                            return false;

                        return player.Team.teamLeader == player.userID ? true : false;
                    }
                case "Clans":
                    {
                        var clanName = Clans?.Call<string>("GetClanOf", player.userID);
                        if (clanName == null)
                            return false;

                        var clan = Clans?.Call<JObject>("GetClan", clanName);
                        var ownerID = clan.GetValue("owner").ToString();
                        if (ownerID == player.UserIDString)
                            return true;

                        return false;
                    }

                default:
                    return false;
            }
        }

        private void AddReward(string id, string town)
        {
            if (!config.towns.ContainsKey(town))
            {
                PrintError($"{town} not in config!");
                return;
            }

            var townRewards = config.towns[town].rewards;
            var reward = townRewards[UnityEngine.Random.Range(0, townRewards.Count)];
            var item = new inventoryData() { shortName = reward.shortName, amount = reward.amount, customName = reward.customName, skinID = reward.skinID, itemID = UnityEngine.Random.Range(1000, 9999) };

            if (!inventorysCache.ContainsKey(id))
                inventorysCache.Add(id, new List<inventoryData>() { item });
            else
                inventorysCache[id].Add(item);

            SaveInventoryItem(id);
        }

        private string isPlayerInZoneAndCapt(BasePlayer player)
        {
            foreach (var obj in townObjects)
            {
                var capt = obj.GetComponent<Capture>();
                if (capt == null)
                    continue;

                if (capt.isPlayerInZone(player))
                {
                    if (capt.isCapture)
                        return capt.monumentName;
                }
            }

            return null;
        }
        #endregion
    
        #region [UI]
        private void CreateTownWarsMainMenu(BasePlayer player, int page = 0)
        {
            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiElement { Parent = "Overlay", Name = "CaptMain", Components = { new CuiImageComponent { Color = "0 0 0 0", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }, new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }, new CuiNeedsCursorComponent() } });
            UI.CreateButton(ref container, "CaptMain", "0 0 0 0.7", "", 0, "0 0", "1 1", "UI_CLOSE_CAPT");
            UI.CreatePanel(ref container, "CaptMask", "CaptMain", "0 0 0 0.9", "0 0", "1 1");
            UI.CreatePanelBlur(ref container, "CaptPanel", "CaptMask", config.ui.colorBG, "0.05 0.18", "0.95 0.95");
            UI.CreateOutLines(ref container, "CaptPanel", config.ui.colorLines);
            UI.CreatePanel(ref container, "CaptPanelTowns", "CaptPanel", "0 0 0 0", "0.01475696 0.1197691", "0.9869792 0.9869792");

            UI.CreatePanelBlur(ref container, "Header", "CaptPanelTowns", "0 0 0 0.4", "0.009821426 0.9006174", "0.9900553 0.9900553");
            UI.CreateOutLines(ref container, "Header", config.ui.colorLines);

            UI.CreateTextOutLine(ref container, "Header", GetMsg("UI_TownName", player), "1 1 1 0.8", $"0.05 0", $"0.37 1", TextAnchor.MiddleLeft, 22);
            UI.CreateTextOutLine(ref container, "Header", GetMsg("UI_OwnerName", player), "1 1 1 0.8", $"0.38 0", $"0.70 1", TextAnchor.MiddleLeft, 22);
            UI.CreateTextOutLine(ref container, "Header", GetMsg("UI_Status", player), "1 1 1 0.8", $"0.71 0", $"0.95 1", TextAnchor.MiddleLeft, 22);

            Capture nearCapt = null;
            int i = 0;

            foreach (var obj in townObjects.Skip(page * 8))
            {
                var capt = obj.GetComponent<Capture>();
                if (capt == null)
                    continue;

                i++;

                UI.CreatePanelBlur(ref container, "town" + i, "CaptPanelTowns", "0 0 0 0.4", $"0.009821426 {0.9006174 - i * 0.1}", $"0.9900553 {0.9900553 - i * 0.1}");
                UI.CreateOutLines(ref container, "town" + i, config.ui.colorLines);

                UI.CreateTextOutLine(ref container, "town" + i, capt.Name, "1 1 1 0.7", $"0.05 0", $"0.37 1", TextAnchor.MiddleLeft, 18);
                UI.CreateTextOutLine(ref container, "town" + i, GetMsg("UI_" + config.settings.typeTeam, player) + " " + capt.OwnerName, "1 1 1 0.7", $"0.38 0", $"0.70 1", TextAnchor.MiddleLeft, 16);
                UI.CreateTextOutLine(ref container, "town" + i, GetMsg(capt.GetStatus(), player), "1 1 1 0.7", $"0.71 0", $"0.95 1", TextAnchor.MiddleLeft, 16);

                UI.CreateButton(ref container, "town" + i, "0 0 0 0.5", "?", 16, $"0.95 0.3", $"0.99 0.8", $"UI_SHOW_CAPTURE_ITEMS {capt.id}", TextAnchor.MiddleCenter, "items");
                UI.CreateOutLines(ref container, "items", config.ui.colorLines);


                if (Vector3.Distance(player.transform.position, obj.transform.position) <= capt.distance)
                    nearCapt = capt;
            }

            if (page > 0)
            {
                UI.CreateButton(ref container, "CaptPanelTowns", "0 0 0 0.5", "<", 16, "0.009821426 0.01663953", "0.06874999 0.08943777", $"UI_CHANGE_MAIN_PAGE {page - 1}", TextAnchor.MiddleCenter, "btn_prev");
                UI.CreateOutLines(ref container, "btn_prev", config.ui.colorLines);
            }

            if ((page + 1) * 8 < townObjects.Count())
            {
                UI.CreateButton(ref container, "CaptPanelTowns", "0 0 0 0.5", ">", 16, "0.93 0.01663953", "0.9900553 0.08943777", $"UI_CHANGE_MAIN_PAGE {page + 1}", TextAnchor.MiddleCenter, "btn_next");
                UI.CreateOutLines(ref container, "btn_next", config.ui.colorLines);
            }

            if (nearCapt != null)
            {
                UI.CreateButton(ref container, "CaptMask", config.ui.colorButtonCapture, String.Format(GetMsg("UI_Capture", player), nearCapt.Name), 16, "0.76 0.1363637", "0.9850931 0.2239058", $"UI_START_CAPT {nearCapt.id}", TextAnchor.MiddleCenter, "btn_capt");
                UI.CreateOutLines(ref container, "btn_capt", config.ui.colorLines);
            }

            UI.CreateButton(ref container, "CaptMask", config.ui.colorButtonInventory, GetMsg("UI_Inventory", player), 24, "0.009937823 0.1363637", "0.2472049 0.2239058", "UI_CHANGE_INV_PAGE 0", TextAnchor.MiddleCenter, "btn_inv");
            UI.CreateOutLines(ref container, "btn_inv", config.ui.colorLines);


            UI.CreateButton(ref container, "CaptMask", config.ui.colorButtonInventory, "✘", 24, "0.9383681 0.9314575", "0.96875 0.9783549", "UI_CLOSE_CAPT", TextAnchor.MiddleCenter, "btn_close");
            UI.CreateOutLines(ref container, "btn_close", config.ui.colorLines);


            CuiHelper.DestroyUi(player, "CaptMain");
            CuiHelper.AddUi(player, container);
        }

        private void CreateTownWarsInventory(BasePlayer player, int page = 0)
        {
            var clanName = GetPlayerClan(player);
            if (clanName == null || !inventorysCache.ContainsKey(GetPlayerClan(player)))
            {
                player.ChatMessage(GetMsg("UI_InventoryEmpty", player));
                return;
            }

            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiElement { Parent = "Overlay", Name = "CaptMain", Components = { new CuiImageComponent { Color = "0 0 0 0", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }, new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }, new CuiNeedsCursorComponent() } });
            UI.CreateButton(ref container, "CaptMain", "0 0 0 0.8", "", 0, "0 0", "1 1", "UI_CLOSE_CAPT");
            UI.CreatePanel(ref container, "CaptMask", "CaptMain", "0 0 0 0", "0.1554688 0.1319444", "0.784375 0.9569445");
            UI.CreatePanelBlur(ref container, "CaptPanel", "CaptMask", config.ui.colorBG, "0.05 0.18", "0.95 0.95");
            UI.CreateOutLines(ref container, "CaptPanel", config.ui.colorLines);
            UI.CreatePanel(ref container, "CaptPanelTowns", "CaptPanel", "0 0 0 0", "0.01475696 0.1197691", "0.9869792 0.9869792");

            UI.CreatePanelBlur(ref container, "Header", "CaptPanelTowns", "0 0 0 0.4", "0.009821426 0.9006174", "0.9900553 0.9900553");
            UI.CreateOutLines(ref container, "Header", config.ui.colorLines);

            UI.CreateTextOutLine(ref container, "Header", GetMsg("UI_Inventory", player), "1 1 1 1", $"0 0", $"1 1", TextAnchor.MiddleCenter, 28);


            int i = 0;
            int j = 0;


            foreach (var item in inventorysCache[clanName].Skip(page * 30))
            {
                var id = item.itemID.ToString();

                UI.CreatePanelBlur(ref container, id, "CaptPanelTowns", $"0 0 0 0.5", $"{0.24 + i * 0.085} {0.7163889 - j * 0.147}", $"{0.32 + i * 0.085} {0.8561111 - j * 0.147}");
                if(String.IsNullOrEmpty(item.customName))
                    CreateImage(ref container, "img", id, "1 1 1 1", item.shortName, "0 0", $"1 1");
                else
                    CreateImage(ref container, "img", id, "1 1 1 1", item.shortName + item.skinID.ToString(), "0 0", $"1 1");

                UI.CreateTextOutLine(ref container, id, "  <size=12>x</size><color=white>" + item.amount + "</color>", $"1 1 1 1", $"0.01 0.01", $"0.95 0.99", TextAnchor.LowerRight, 14);
                UI.CreateOutLines(ref container, id, "0.87 0.44 0.00 0.8");


                UI.CreateButton(ref container, id, "0 0 0 0", "", 16, "0 0", "1 1", $"UI_INV_PAGE_GET_ITEM {item.itemID}", TextAnchor.MiddleCenter, id + "btn");
                i++;
                if (i == 6)
                {

                    i = 0;
                    j++;
                }

                if (j == 5)
                    break;

            }

            if (page > 0)
            {
                UI.CreateButton(ref container, "CaptPanelTowns", "0 0 0 0.5", "<", 16, "0.009821426 0.01663953", "0.06874999 0.08943777", $"UI_CHANGE_INV_PAGE {page - 1}", TextAnchor.MiddleCenter, "btn_prev");
                UI.CreateOutLines(ref container, "btn_prev", config.ui.colorLines);
            }

            if ((page + 1 ) * 30 < inventorysCache[clanName].Count())
            {
                UI.CreateButton(ref container, "CaptPanelTowns", "0 0 0 0.5", ">", 16, "0.93 0.01663953", "0.9900553 0.08943777", $"UI_CHANGE_INV_PAGE {page + 1}", TextAnchor.MiddleCenter, "btn_next");
                UI.CreateOutLines(ref container, "btn_next", config.ui.colorLines);
            }

            UI.CreateButton(ref container, "CaptMask", config.ui.colorButtonInventory, GetMsg("UI_Return", player), 24, "0.009937823 0.1363637", "0.2472049 0.2239058", "UI_RETURN_MAIN", TextAnchor.MiddleCenter, "btn_inv");
            UI.CreateOutLines(ref container, "btn_inv", config.ui.colorLines);

            CuiHelper.DestroyUi(player, "CaptMain");
            CuiHelper.AddUi(player, container);
        }

        private void CreateTownItems(BasePlayer player, int id, int page = 0)
        {
            List<Reward> rewards = new List<Reward>();
            string header = "";

            foreach (var obj in townObjects.Skip(page * 8))
            {
                var capt = obj.GetComponent<Capture>();
                if (capt == null)
                    continue;

                if(capt.id == id)
                {
                    if(config.towns.ContainsKey(capt.monumentName))
                    {
                        header = String.Format(GetMsg("UI_Reward", player), capt.Name);
                        rewards = config.towns[capt.monumentName].rewards;
                    }
                }
            }

            if (rewards.Count <= 0)
                return;

            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiElement { Parent = "Overlay", Name = "CaptMain", Components = { new CuiImageComponent { Color = "0 0 0 0", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }, new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }, new CuiNeedsCursorComponent() } });
            UI.CreateButton(ref container, "CaptMain", "0 0 0 0.8", "", 0, "0 0", "1 1", "UI_CLOSE_CAPT");
            UI.CreatePanel(ref container, "CaptMask", "CaptMain", "0 0 0 0", "0.1554688 0.1319444", "0.784375 0.9569445");
            UI.CreatePanelBlur(ref container, "CaptPanel", "CaptMask", config.ui.colorBG, "0.05 0.18", "0.95 0.95");
            UI.CreateOutLines(ref container, "CaptPanel", config.ui.colorLines);
            UI.CreatePanel(ref container, "CaptPanelTowns", "CaptPanel", "0 0 0 0", "0.01475696 0.1197691", "0.9869792 0.9869792");

            UI.CreatePanelBlur(ref container, "Header", "CaptPanelTowns", "0 0 0 0.4", "0.009821426 0.9006174", "0.9900553 0.9900553");
            UI.CreateOutLines(ref container, "Header", config.ui.colorLines);

            UI.CreateTextOutLine(ref container, "Header", header, "1 1 1 1", $"0 0", $"1 1", TextAnchor.MiddleCenter, 28);


            int i = 0;
            int j = 0;


            foreach (var item in rewards.Skip(page * 30))
            {
                UI.CreatePanelBlur(ref container, "item" + i + j, "CaptPanelTowns", $"0 0 0 0.5", $"{0.24 + i * 0.085} {0.7163889 - j * 0.147}", $"{0.32 + i * 0.085} {0.8561111 - j * 0.147}");
                if (String.IsNullOrEmpty(item.customName))
                    CreateImage(ref container, "img", "item" + i + j, "1 1 1 1", item.shortName, "0 0", $"1 1");
                else
                    CreateImage(ref container, "img", "item" + i + j, "1 1 1 1", item.shortName + item.skinID.ToString(), "0 0", $"1 1");

                UI.CreateTextOutLine(ref container, "item" + i + j, "  <size=12>x</size><color=white>" + item.amount + "</color>", $"1 1 1 1", $"0.01 0.01", $"0.95 0.99", TextAnchor.LowerRight, 14);
                UI.CreateOutLines(ref container, "item" + i + j, "0.87 0.44 0.00 0.8");

                 i++;
                if (i == 6)
                {

                    i = 0;
                    j++;
                }

                if (j == 5)
                    break;

            }

            if (page > 0)
            {
                UI.CreateButton(ref container, "CaptPanelTowns", "0 0 0 0.5", "<", 16, "0.009821426 0.01663953", "0.06874999 0.08943777", $"UI_CHANGE_INV_PAGE {page - 1}", TextAnchor.MiddleCenter, "btn_prev");
                UI.CreateOutLines(ref container, "btn_prev", config.ui.colorLines);
            }

            if ((page + 1) * 30 < rewards.Count())
            {
                UI.CreateButton(ref container, "CaptPanelTowns", "0 0 0 0.5", ">", 16, "0.93 0.01663953", "0.9900553 0.08943777", $"UI_CHANGE_INV_PAGE {page + 1}", TextAnchor.MiddleCenter, "btn_next");
                UI.CreateOutLines(ref container, "btn_next", config.ui.colorLines);
            }

            UI.CreateButton(ref container, "CaptMask", config.ui.colorButtonInventory, GetMsg("UI_Return", player), 24, "0.009937823 0.1363637", "0.2472049 0.2239058", "UI_RETURN_MAIN", TextAnchor.MiddleCenter, "btn_inv");
            UI.CreateOutLines(ref container, "btn_inv", config.ui.colorLines);

            CuiHelper.DestroyUi(player, "CaptMain");
            CuiHelper.AddUi(player, container);
        }


        [ConsoleCommand("UI_CLOSE_CAPT")]
        private void cmd_UI_CLOSE_CAPT(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;

            CuiHelper.DestroyUi(player, "CaptMain");
        }

        [ConsoleCommand("UI_RETURN_MAIN")]
        private void cmd_UI_RETURN_MAIN(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;

            CreateTownWarsMainMenu(player);
        }

        [ConsoleCommand("UI_SHOW_CAPTURE_ITEMS")]
        private void cmd_UI_SHOW_CAPTURE_ITEMS(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;

            CreateTownItems(player, Convert.ToInt32(arg.Args[0]));
        }

        [ConsoleCommand("UI_START_CAPT")]
        private void cmd_UI_START_CAPT(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            if (arg.Args.Length <= 0) return;

            if(BasePlayer.activePlayerList.Count < config.settings.minPlayers)
            {
                player.ChatMessage(String.Format(GetMsg("UI_LimitPlayers", player), config.settings.minPlayers));
                return;
            }

            int id = Convert.ToInt32(arg.Args[0]);
            var clanName = GetPlayerClan(player);
            if(clanName == null)
            {
                player.ChatMessage(GetMsg("UI_NeedClan", player));
                return;
            }

            if (!isPlayerOwner(player) && !config.settings.allCanStartCapt)
            {
                player.ChatMessage(GetMsg("UI_OnlyOwner", player));
                return;
            }

            foreach (var obj in townObjects)
            {
                var capt = obj.GetComponent<Capture>();
                if (capt == null)
                    continue;

                if (capt.id == id && capt.GetStatus() == "UI_CanCapture" )
                    capt.StartCapture(player);
            }

            CuiHelper.DestroyUi(player, "CaptMain");
        }

        [ConsoleCommand("UI_CHANGE_MAIN_PAGE")]
        private void cmd_UI_CHANGE_MAIN_PAGE(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            if (arg.Args.Length <= 0) return;

            CreateTownWarsMainMenu(player, Convert.ToInt32(arg.Args[0]));
        }

        [ConsoleCommand("UI_CHANGE_INV_PAGE")]
        private void cmd_UI_CHANGE_INV_PAGE(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            if (arg.Args.Length <= 0) return;

            CreateTownWarsInventory(player, Convert.ToInt32(arg.Args[0]));
        }

        [ConsoleCommand("UI_INV_PAGE_GET_ITEM")]
        private void cmd_UI_INV_PAGE_GET_ITEM(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            if (arg.Args.Length <= 0) return;


            
            var clanName = GetPlayerClan(player);
            if (clanName == null)
                return;

            if (!isPlayerOwner(player) && !config.settings.allCanTakeReward)
            {
                player.ChatMessage(GetMsg("UI_OnlyOwner", player));
                return;
            }

            var id = Convert.ToInt32(arg.Args[0]);
            var item = inventorysCache[clanName].FirstOrDefault(i => i.itemID == id);

            if (item == null)
                return;


            if (!String.IsNullOrEmpty(item.command)) rust.RunServerCommand(item.command.Replace("%STEAMID%", player.UserIDString));
            else
            {
                var reward = ItemManager.CreateByName(item.shortName, item.amount, item.skinID);
                if (!String.IsNullOrEmpty(item.customName)) reward.name = item.customName;
                inventorysCache[clanName].Remove(item);
                player.GiveItem(reward);
            }

            CuiElementContainer container = new CuiElementContainer();

            UI.CreatePanelBlur(ref container, "greenPanel", arg.Args[0], $"0.00 0.58 0.00 0.8", $"0 0", $"1 1", 0f, 0.5f);
            UI.CreateTextOutLine(ref container, arg.Args[0], "✔", $"1 1 1 0.8", $"0.01 0.01", $"0.95 0.99", TextAnchor.MiddleCenter, 28, "123", 0.5f);

            CuiHelper.DestroyUi(player, arg.Args[0] + "btn");
            CuiHelper.AddUi(player, container);
        }

        #region [UI generator]
        public class UI
        {
            public static void CreateOutLines(ref CuiElementContainer container, string parent, string color)
            {
                //CreatePanel(ref container, "Line", parent, color, "0 0", "0.001 1");
                //CreatePanel(ref container, "Line", parent, color, "0 0", "1 0.001");
                //CreatePanel(ref container, "Line", parent, color, "0.999 0", "1 1");
                //CreatePanel(ref container, "Line", parent, color, "0 0.999", "1 1");
            }

            public static void CreateButton(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter, string name = "button", float FadeIn = 0f)
            {

                container.Add(new CuiButton
                {

                    Button = { Color = color, Command = command, FadeIn = FadeIn },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    Text = { Text = text, FontSize = size, Align = align }

                },
                panel, name);
            }

            public static void CreatePanel(ref CuiElementContainer container, string name, string parent, string color, string aMin, string aMax, float Fadeout = 0f, float Fadein = 0f)
            {

                container.Add(new CuiElement
                {
                    Name = name,
                    Parent = parent,
                    Components =
        {
            new CuiImageComponent { Color = color, FadeIn = Fadein },
            new CuiRectTransformComponent { AnchorMin = aMin, AnchorMax = aMax}
        },
                    FadeOut = Fadeout
                });
            }

            public static void CreatePanelBlur(ref CuiElementContainer container, string name, string parent, string color, string aMin, string aMax, float Fadeout = 0f, float Fadein = 0f)
            {
                container.Add(new CuiPanel()
                {
                    CursorEnabled = true,
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    Image = { Color = color, Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat", FadeIn = Fadein },
                    FadeOut = Fadeout
                }, parent, name);
            }

            public static void CreateText(ref CuiElementContainer container, string parent, string text, string color, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleLeft, int size = 14, string name = "name", float Fadein = 0f)
            {
                container.Add(new CuiElement
                {
                    Parent = parent,
                    Name = name,
                    Components =
        {
            new CuiTextComponent(){ Color = color, Text = text, FontSize = size, Align = align, FadeIn = Fadein },
            new CuiRectTransformComponent{ AnchorMin =  aMin ,AnchorMax = aMax }
        }
                });
            }

            public static void CreateTextOutLine(ref CuiElementContainer container, string parent, string text, string color, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleLeft, int size = 14, string name = "name", float Fadein = 0f)
            {
                container.Add(new CuiElement
                {
                    Parent = parent,
                    Name = name,
                    Components =
        {
            new CuiTextComponent(){ Color = color, Text = text, FontSize = size, Align = align, FadeIn = Fadein },
            new CuiRectTransformComponent{ AnchorMin =  aMin ,AnchorMax = aMax },
            new CuiOutlineComponent{ Color = "0 0 0 1" }
        }
                });
            }
        }

        public void CreateImage(ref CuiElementContainer container, string name, string panel, string color, string image, string aMin, string aMax, float Fadeout = 0f, float Fadein = 0f, ulong skin = 0)
        {
            container.Add(new CuiElement
            {
                Name = name,
                Parent = panel,
                Components =
        {
            new CuiRawImageComponent { Color = color, Png = (string)ImageLibrary.Call("GetImage", image, skin), FadeIn = Fadein },
            new CuiRectTransformComponent { AnchorMin = aMin, AnchorMax = aMax },

        },
                FadeOut = Fadeout
            });
        }
        #endregion
        #endregion

        #region [Data]
        private void SaveTownsData()
        {
            Dictionary<string, townsData> data = new Dictionary<string, townsData>();
            foreach (var obj in townObjects)
            {
                var capture = obj.GetComponent<Capture>();
                if (capture == null)
                    continue;


                data.Add(capture.monumentName, new townsData() { OwnerID = capture.OwnerId, OwnerName = capture.OwnerName, lastCapture = capture.lastCapture, lastReward = capture.lastReward });
            }

            Interface.Oxide.DataFileSystem.WriteObject($"TownWars/townsData/data", data);
        }

        private void LoadTownsData()
        {
            Dictionary<string, townsData> data = new Dictionary<string, townsData>();
            data = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, townsData>>($"TownWars/townsData/data");

            var monuments = UnityEngine.Object.FindObjectsOfType<MonumentInfo>();
            foreach (var mon in monuments)
            {
                if (!config.towns.ContainsKey(mon.name))
                    continue;

                if (!data.ContainsKey(mon.name))
                {
                    data.Add(mon.name, new townsData() { OwnerName = "-", OwnerID = "0", lastCapture = Facepunch.Math.Epoch.Current - config.settings.delayCapture, lastReward = Facepunch.Math.Epoch.Current });
                }

                var go = new GameObject();
                go.transform.position = mon.transform.position;
                townObjects.Add(go);
                var capt = go.AddComponent<Capture>();
                var cfg = config.towns[mon.name];
                var town = data[mon.name];

                capt.Initialize(mon.name, cfg.name, cfg.capDist, town.OwnerID, town.OwnerName, town.lastReward, town.lastCapture);
            }
        }

        private void clearTownsData(bool load = false)
        {
            Dictionary<string, townsData> data = new Dictionary<string, townsData>();
            Interface.Oxide.DataFileSystem.WriteObject($"TownWars/townsData/data", data);

            if (load)
                LoadTownsData();
        }

        private void AddInventoryToCache(string id)
        {
            if (inventorysCache.ContainsKey(id))
                return;

            List<inventoryData> data;
            var exist = Interface.GetMod().DataFileSystem.ExistsDatafile($"TownWars/invData/inventory_{id}");
            if (!exist)
                return;

            data = Interface.GetMod().DataFileSystem.ReadObject<List<inventoryData>>($"TownWars/invData/inventory_{id}");

            inventorysCache.Add(id, data);
        }

        private void SaveInventoryItem(string id)
        {
            if (!inventorysCache.ContainsKey(id))
                return;

            Interface.Oxide.DataFileSystem.WriteObject($"TownWars/invData/inventory_{id}", inventorysCache[id]);
        }


        private void SaveInventorys()
        {
            foreach (var inv in inventorysCache)
            {
                Interface.Oxide.DataFileSystem.WriteObject($"TownWars/invData/inventory_{inv.Key}", inv.Value);
            }
        }

        private void clearInventory(string id)
        {
            if (inventorysCache.ContainsKey(id))
                inventorysCache[id].Clear();

            Interface.Oxide.DataFileSystem.WriteObject($"TownWars/invData/inventory_{id}", new List<inventoryData>());
        }

        private void wipeInventorys()
        {
            foreach (var file in Interface.Oxide.DataFileSystem.GetFiles("TownWars/invData/"))
            {
                var newFile = file.Remove(file.IndexOf('.'));
                Interface.Oxide.DataFileSystem.WriteObject(newFile, new List<inventoryData>());
            }
        }
        #endregion

        #region [Comp]
        public class Capture : MonoBehaviour
        {
            public string monumentName;
            public string Name;
            public float distance;
            public string OwnerId;
            public string OwnerName;
            public int lastReward;
            public int lastCapture;
            public bool isCapture;
            public int id;
            private int timerSec;

            private List<BasePlayer> players;
            private Dictionary<string, teamInfo> capTeams;
            private SphereCollider sphereCollider;
            private BaseEntity[] spheres;
            private MapMarkerGenericRadius mapMarker;
            private VendingMachineMapMarker vendingMarker;


            public void DestroyComp() => OnDestroy();
            private void OnDestroy()
            {
                RemoveMarker();
                RemoveCaptureSphere();
                Destroy(this);
            }

            private void Awake()
            {
                gameObject.layer = (int)Layer.Reserved1;
            }

            public void Initialize(string monName, string townName, int capDist, string oId, string oName, int reward, int capt)
            {
                monumentName = monName;
                Name = townName;
                distance = capDist;
                capTeams = new Dictionary<string, teamInfo>();
                spheres = new BaseEntity[3];
                players = new List<BasePlayer>();
                OwnerId = oId;
                OwnerName = oName;
                lastCapture = capt;
                lastReward = reward;
                id = UnityEngine.Random.Range(1000, 9999);

                if (Facepunch.Math.Epoch.Current - lastCapture < plugin.config.settings.delayCapture)
                    CreateMarker(plugin.config.marker.markerColorCantCapture);
                else
                    CreateMarker(plugin.config.marker.markerColorCanCapture);

                sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.isTrigger = true;
                sphereCollider.radius = capDist;

                InvokeRepeating("Timer", 1f, 1);
            }

            private void OnTriggerEnter(Collider other)
            {
                var target = other.GetComponentInParent<BasePlayer>();
                if (target != null && !players.Contains(target) && target.IsConnected && target.IsAlive())
                    players.Add(target);
            }

            private void OnTriggerExit(Collider other)
            {
                var target = other.GetComponentInParent<BasePlayer>();
                if (target != null && players.Contains(target))
                    players.Remove(target);
            }

            private void Timer()
            {
                foreach (var player in players)
                {
                    if (player == null || !player.IsConnected || !player.IsAlive())
                        continue;

                    var clan = plugin.GetPlayerClan(player);
                    if (String.IsNullOrEmpty(clan))
                        continue;

                    if (isCapture)
                    {
                        if (!capTeams.ContainsKey(clan))
                        {
                            capTeams.Add(clan, new teamInfo() { id = clan, name = plugin.GetPlayerClanName(player), points = 0 });
                        }

                        capTeams[clan].points++;
                    }
                    DrawInfo(player);
                }

                if (isCapture)
                {
                    timerSec++;

                    if (timerSec >= plugin.config.settings.durCapture)
                        StopCapture();
                }
                else if (OwnerId != "0")
                {
                    if (Facepunch.Math.Epoch.Current - lastReward >= plugin.config.settings.addRewardSettings)
                    {
                        plugin.AddReward(OwnerId, monumentName);
                        lastReward = Facepunch.Math.Epoch.Current;
                    }
                }

                if (Facepunch.Math.Epoch.Current - lastCapture == plugin.config.settings.delayCapture)
                    mapMarker.color1 = ConvertToColor(plugin.config.marker.markerColorCanCapture);

                UpdateMarker();
            }

            private void DrawInfo(BasePlayer player)
            {
                string text = "<size=20>";
                string owner = "";

                if (!isCapture)
                    owner = plugin.GetMsg(plugin.config.settings.typeTeam, player) + " " + OwnerName;


                if (isCapture)
                {
                    var team = capTeams.OrderByDescending(p => p.Value.points).FirstOrDefault().Value;
                    if (team == null)
                        return;

                    owner = plugin.GetMsg(plugin.config.settings.typeTeam, player) + " " + team.name;

                    var clan = plugin.GetPlayerClan(player);
                    if (String.IsNullOrEmpty(clan))
                        return;

                    var myPoints = capTeams[clan].points;
                    var captTime = TimeSpan.FromSeconds(plugin.config.settings.durCapture - timerSec);

                    text += String.Format(plugin.GetMsg("Capt", player), owner, team.points, myPoints, captTime.Hours, captTime.Minutes, captTime.Seconds);
                }
                else if (Facepunch.Math.Epoch.Current - lastCapture <= plugin.config.settings.delayCapture)
                {
                    var time = TimeSpan.FromSeconds(lastCapture + plugin.config.settings.delayCapture - Facepunch.Math.Epoch.Current);
                    text += String.Format(plugin.GetMsg("CantCapt", player), owner, time.Hours, time.Minutes, time.Seconds, Name);
                }
                else
                {
                    text += String.Format(plugin.GetMsg("CanCapt", player), owner, Name);
                }

                text += "</size>";

                SetPlayerFlag(player, BasePlayer.PlayerFlags.IsAdmin, true);
                player.SendConsoleCommand("ddraw.text", 1.01f, Color.white, transform.position + new Vector3(0, 50f, 0), text);
                SetPlayerFlag(player, BasePlayer.PlayerFlags.IsAdmin, false);
            }

            public void StartCapture(BasePlayer player)
            {
                isCapture = true;
                timerSec = 0;
                capTeams.Clear();
                if (plugin.config.settings.addSphere)
                    CreateCaptureSphere();

                var clan = plugin.GetPlayerClan(player);
                if (String.IsNullOrEmpty(clan))
                    return;

                var info = new teamInfo()
                {
                    id = clan,
                    name = plugin.GetPlayerClanName(player),
                    points = 0
                };


                capTeams.Add(clan, info);

                mapMarker.color1 = ConvertToColor(plugin.config.marker.markerColorCapture);
                vendingMarker.markerShopName = plugin.GetMsg("vendCapt");
                vendingMarker.SendNetworkUpdate();

                plugin.BroadCastToStartCapture(Name);
            }

            private void StopCapture()
            {

                isCapture = false;
                timerSec = 0;
                lastCapture = Facepunch.Math.Epoch.Current;
                lastReward = Facepunch.Math.Epoch.Current;

                var team = capTeams.OrderByDescending(p => p.Value.points).FirstOrDefault().Value;
                if (team == null)
                {
                    OwnerId = "0";
                    OwnerName = "-";
                    return;
                }

                OwnerName = team.name;
                OwnerId = team.id;

                plugin.SaveTownsData();

                RemoveCaptureSphere();
                mapMarker.color1 = ConvertToColor(plugin.config.marker.markerColorCantCapture);
                vendingMarker.markerShopName = OwnerName;
                vendingMarker.SendNetworkUpdate();


                plugin.BroadCastToStopCapture(OwnerName, Name);
            }

            private void UpdateMarker()
            {
                mapMarker.SendUpdate();
            }

            private void RemoveMarker()
            {
                if (mapMarker != null && !mapMarker.IsDestroyed) mapMarker.Kill();
                if (vendingMarker != null && !vendingMarker.IsDestroyed) vendingMarker.Kill();
            }

            private void CreateMarker(string color)
            {
                RemoveMarker();

                mapMarker = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", transform.position).GetComponent<MapMarkerGenericRadius>();
                vendingMarker = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab", transform.position).GetComponent<VendingMachineMapMarker>();

                mapMarker.radius = plugin.config.marker.markerRadius;
                mapMarker.color1 = ConvertToColor(color);
                var c = ConvertToColor(color);
                mapMarker.alpha = plugin.config.marker.markerAlpha;
                mapMarker.enabled = true;
                mapMarker.OwnerID = 0;
                mapMarker.Spawn();
                mapMarker.SendUpdate();

                vendingMarker.markerShopName = OwnerName;
                vendingMarker.OwnerID = 0;
                vendingMarker.Spawn();
                vendingMarker.enabled = false;
            }

            private void CreateCaptureSphere()
            {
                RemoveCaptureSphere();

                for (int i = 0; i < 3; i++)
                {
                    BaseEntity ent = new BaseEntity();
                    string strPrefab = "assets/prefabs/visualization/sphere.prefab";

                    spheres[i] = GameManager.server.CreateEntity(strPrefab, transform.position, new Quaternion(), true);
                    SphereEntity ball = spheres[i].GetComponent<SphereEntity>();
                    ball.currentRadius = 1f;
                    ball.lerpRadius = 2.0f * distance;
                    ball.lerpSpeed = 100f;
                    spheres[i].SetParent(ent);
                    spheres[i].Spawn();
                }

            }

            private void RemoveCaptureSphere()
            {
                for (int i = 0; i < 3; i++)
                    if (spheres[i] != null)
                        spheres[i].Kill(BaseNetworkable.DestroyMode.None);
            }

            private void SetPlayerFlag(BasePlayer player, BasePlayer.PlayerFlags f, bool b)
            {
                if (plugin.permission.UserHasGroup(player.UserIDString, "admin")) return;

                if (b)
                {
                    if (player.HasPlayerFlag(f)) return;
                    player.playerFlags |= f;
                }
                else
                {
                    if (!player.HasPlayerFlag(f)) return;
                    player.playerFlags &= ~f;
                }
                player.SendNetworkUpdateImmediate(false);
            }

            private Color ConvertToColor(string color)
            {
                if (color.StartsWith("#")) color = color.Substring(1);
                int red = int.Parse(color.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(color.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(color.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return new Color((float)red / 255, (float)green / 255, (float)blue / 255);
            }

            public bool isPlayerInZone(BasePlayer player) => players.Contains(player);

            public bool removePlayerFromZone(BasePlayer player)
            {
                if (players.Contains(player))
                {
                    players.Remove(player);
                    return true;
                }
                return false;
            }

            public string GetStatus()
            {
                if (Facepunch.Math.Epoch.Current - lastCapture < plugin.config.settings.delayCapture)
                    return "UI_CantCapture";
                else if (isCapture)
                    return "UI_Capturing";
                else
                    return "UI_CanCapture";
            }

            public class teamInfo
            {
                public string id;
                public string name;
                public int points;
            }
        }
        #endregion

        #region [Command]
        [ChatCommand("tw")]
        private void command_TownWars(BasePlayer player, string c, string[] a) => CreateTownWarsMainMenu(player);
        #endregion
    }
}
