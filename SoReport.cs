using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Facepunch.Extend;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SoReport", "LAGZYA", "1.0.7")]
    public class SoReport : RustPlugin
    {
        #region Data

        private ConfigData cfg { get; set; }

        private class ConfigData
        {
            [JsonProperty("Demonic RUST")] public string Lable;
            [JsonProperty("Включить дискорд?")] public bool discord;

            [JsonProperty("Dircord Report WebHook")]
            public string discordhook;

            [JsonProperty("Dircord PlayerSayDiscord WebHook")]
            public string discordhook2;
            [JsonProperty("Отправлять ли уведомления в телеграмм ?")]
            public bool TelegramUse = false; 
            [JsonProperty("Название чата(Пригласить своего бота в чат)")]
            public string chatid = "НАЗВАНИЕ ЧАТА";
            [JsonProperty("Создать своего бота через BotFather и скопировать сюда токен")]
            public string botToken = "ТОКЕН";
            [JsonProperty("Отправлять ли уведомления в вконтакте ?")]
            public bool Vkontakte = false;
            [JsonProperty("Вк админа")] public string vkadmin = "";
            [JsonProperty("VK Token группы")]
            public string vkAcces = "";

            [JsonProperty("Cooldown")] public int cooldown;
            [JsonProperty("Причина 1")] public string res1 = "МАКРОСЫ";
            [JsonProperty("Причина 2")] public string res2 = "ЧИТЫ";
            [JsonProperty("Причина 3")] public string res3 = "БАГОЮЗ";
            [JsonProperty("Причина 4")] public string res4 = "Игра +3";

 
            [JsonProperty("Кол-во репортов для появление в панели модератора")]
            public int kolreport = 2;

            [JsonProperty("Кол-во репортов для подсветки красный в панели модератора")]
            public int redcolor = 5;

            [JsonProperty("Кол-во проверки для подсветки зеленым в панели модератора")]
            public int greencolor = 3;

            public static ConfigData GetNewConf()
            {
                var newConfig = new ConfigData();
                newConfig.Lable = "SoReport";
                newConfig.discord = true;
                newConfig.cooldown = 30; 
                newConfig.discordhook = "https://discord.com/api/webhooks/940857028740522055/LPrIUHpQFyzDNhoYBbXKOQpZ8_XPbZteie_m_KxASfuqsWvFRJG4IxRJ3ADQfs9dCyjO";
                newConfig.discordhook2 = "https://discord.com/api/webhooks/940858719212830771/okHGbhHobKSaM2m4UOKmnRRYijgzeox4j2ZTWJ6YWDYBXvyo9fn1_LVVT_P3BgI5o4G7";
                return newConfig;
            }
        }

        protected override void LoadDefaultConfig()
        {
            cfg = ConfigData.GetNewConf();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(cfg);
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                cfg = Config.ReadObject<ConfigData>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        public Dictionary<ulong, PlayerData> _playerData = new Dictionary<ulong, PlayerData>();

        public class PlayerData
        {
            public string UserName;
            public int ReportCount;
            public int AlertCount;
            public double ReportCD = CurrentTime();
            public double IsCooldown => Math.Max(ReportCD - CurrentTime(), 0);
        }

        #endregion
        #region UI

        private static string Layer = "UiSoReport";
        private string Hud = "Hud";
        private string Overlay = "Overlay";
        private string regular = "robotocondensed-regular.ttf";
        private static string Sharp = "assets/content/ui/ui.background.tile.psd";
        private static string Blur = "assets/content/ui/uibackgroundblur.mat";
        private string radial = "assets/content/ui/ui.background.transparent.radial.psd";

        private CuiPanel _fon = new CuiPanel()
        {
            RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
            Image = {Color = "0 0 0 0.87", Material = "assets/content/ui/uibackgroundblur.mat"}
        };

        private CuiPanel _mainFon = new CuiPanel()
        {
            RectTransform =
                {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-1920 -1080", OffsetMax = "1920 1080"},
            CursorEnabled = true,
            Image = {Color = "0.24978750 0.2312312 0.312312312 0"}
        };

        private CuiPanel _redPanel = new CuiPanel()
        {
            RectTransform = {AnchorMin = "0.4189235 0.3333334", AnchorMax = "0.4930556 0.666358"},
            Image = {Color = HexToRustFormat("#82433bCC")}
        };

        private CuiPanel _modPanel = new CuiPanel()
        {
            RectTransform = {AnchorMin = "0.333513 0.3333334", AnchorMax = "0.4076451 0.666358"},
            Image = {Color = HexToRustFormat("#222222CC")}
        };
        private CuiPanel _mod2Panel = new CuiPanel()
        {
            RectTransform = {AnchorMin = "0.333513 0.3333334", AnchorMax = "0.4076451 0.666358"},
            Image = {Color = HexToRustFormat("#222222CC"), Material = Blur }
        };
        private CuiPanel _playersPanel = new CuiPanel()
        {
            RectTransform = {AnchorMin = "0.4933972 0.3333334", AnchorMax = "0.6559027 0.666358"},
            Image = {Color = HexToRustFormat("#222222CC")}
        };

        private CuiButton _close = new CuiButton()
        {
            RectTransform = {AnchorMin = "0.6567709 0.6496913", AnchorMax = "0.6659723 0.6645062"},
            Button = {Close = Layer, Sprite = "assets/icons/vote_down.png", Color = "0.64 0.64 0.64 0.86",},
            Text = {Text = ""}
        };

        [ChatCommand("report")]
        private void StartUi(BasePlayer player)
        {
            var cont = new CuiElementContainer();
            cont.Add(_fon, Overlay, Layer);
            cont.Add(_mainFon, Layer, Layer + "Main");
            cont.Add(_close, Layer + "Main");
            if (permission.UserHasPermission(player.UserIDString, "soreport.admin"))
                cont.Add(new CuiButton()
                {
                    Text = {Text = "МОДЕРАТОР ПАНЕЛЬ", Align = TextAnchor.MiddleCenter},
                    Button = {Color = "0.64 0.64 0.64 0.35", Command = "uisoreport modmenu open"},
                    RectTransform = {AnchorMin = "0.384375 0.6555555", AnchorMax = "0.41875 0.6654318"}
                }, Layer + "Main", Layer + "BMod");
            CuiHelper.AddUi(player, cont);
            PlayerListLoad(player, 1);
            ReportFon(player);
        }
        private void Alert(ulong targetId)
        {
            var targetPlayer = BasePlayer.FindByID(targetId);
            if (targetPlayer == null) return;
            var cont = new CuiElementContainer();
            cont.Add(new CuiPanel()
            {
                RectTransform =
                    {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-300 100", OffsetMax = "300 200"},
                Image = {Color = HexToRustFormat("#26567CFF")}
            }, Overlay, "AlerUISo");
            cont.Add(new CuiElement()
            {
                Parent = "AlerUISo",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 15,
                        Font = "robotocondensed-bold.ttf",
                        Text =
                            "Вы подозреваетесь в использовании запрещённого ПО. Администратор вызвал вас на проверку. Напишите свой дискорд через команду /discord. (Пример: /discord никнейм#1234). У вас есть 2 минуты, за выход с сервера вы получите бан за отказ от проверки! Если ваш дискорд с пробелом - напишите его в общий чат"
                    },
                    new CuiRectTransformComponent() {AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "10 0", OffsetMax = "590 100"}
                }
            });
            CuiHelper.AddUi(targetPlayer, cont);
        }

        private void ModeratorInterface(BasePlayer player, int page)
        {
            var cont = new CuiElementContainer();
            List<ulong> f;
            CuiHelper.DestroyUi(player, Layer + "Mod");
            cont.Add(_modPanel, Layer + "Main", Layer + "Mod");
            //Лейбл
            PlayerData playerData;
            if (!_playerData.TryGetValue(player.userID, out playerData)) return;
            if (page > 1)
            {
                cont.Add(new CuiButton()
                {
                    Button =
                        {Command = $"uisoreport nextpage mod {page - 1}", Color = HexToRustFormat("#4b602aCC")},
                    Text = {Text = "<", Color = HexToRustFormat("#a1e432"), Align = TextAnchor.MiddleCenter},
                    RectTransform = {AnchorMin = "0.4305381 0.01297505", AnchorMax = "0.4722021 0.05097298"}
                }, Layer + "Mod");
            }
            else
            {
                cont.Add(new CuiButton()
                {
                    Button = {Command = $"", Color = HexToRustFormat($"#24291dCC")},
                    Text = {Text = "<", Color = HexToRustFormat("#a1e432"), Align = TextAnchor.MiddleCenter},
                    RectTransform = {AnchorMin = "0.4305381 0.01297505", AnchorMax = "0.4722021 0.05097298"}
                }, Layer + "Mod");
            }
            cont.Add(new CuiButton()
            {
                Button = {Color = "0 0 0 0.64"},
                Text =
                    {Text = page.ToString(), Color = HexToRustFormat("#a1e432"), Align = TextAnchor.MiddleCenter},
                RectTransform = {AnchorMin = "0.4786112 0.01297505", AnchorMax = "0.5202752 0.05097298"}
            }, Layer + "Mod");
            cont.Add(new CuiButton()
            {
                Button = {Command = $"uisoreport nextpage mod {page + 1}", Color = HexToRustFormat("#4b602aCC")},
                Text = {Text = ">", Color = HexToRustFormat("#a1e432"), Align = TextAnchor.MiddleCenter},
                RectTransform = {AnchorMin = "0.5256162 0.01297505", AnchorMax = "0.5672802 0.05097298"}
            }, Layer + "Mod");
            cont.Add(new CuiElement()
            {
                Parent = Layer + "Mod",
                Components =
                {
                    new CuiTextComponent() {Text = "МОДЕРАТОР", Color = "0.64 0.64 0.64 0.86", FontSize = 30},
                    new CuiRectTransformComponent()
                        {AnchorMin = "0.03044713 0.9258574", AnchorMax = "0.9742392 0.9766592"}
                }
            });
            foreach (var players in _playerData.Where(p => p.Value.ReportCount >= cfg.kolreport)
                .Where(p => BasePlayer.FindByID(p.Key) != null)
                .Select((i, t) => new {A = i, B = t - (page - 1) * 14}).Skip((page - 1) * 14).Take(14))
            {
                var target = BasePlayer.FindByID(players.A.Key);
                if (players.A.Value.AlertCount >= cfg.greencolor)
                {
                    cont.Add(new CuiElement()
                    {
                        Parent = Layer + "Mod",
                        Name = Layer + "Mod" + players.B,
                        Components =
                        {
                            new CuiImageComponent()
                            {
                                Color = "0.00 0.78 0.00 0.25",
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin =
                                    $"{0.03278887} {0.8387395 - Math.Floor((double) players.B / 1) * 0.058}",
                                AnchorMax = $"{0.976581} {0.8923218 - Math.Floor((double) players.B / 1) * 0.058}"
                            }
                        }
                    });
                }
                else if (players.A.Value.ReportCount >= cfg.redcolor)
                {
                    cont.Add(new CuiElement()
                    {
                        Parent = Layer + "Mod",
                        Name = Layer + "Mod" + players.B,
                        Components =
                        {
                            new CuiImageComponent()
                            {
                                Color = "0.50 0.00 0.00 0.35",
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin =
                                    $"{0.03278887} {0.8387395 - Math.Floor((double) players.B / 1) * 0.058}",
                                AnchorMax = $"{0.976581} {0.8923218 - Math.Floor((double) players.B / 1) * 0.058}"
                            }
                        }
                    });
                }
                else
                {

                    cont.Add(new CuiElement()
                    {
                        Parent = Layer + "Mod",
                        Name = Layer + "Mod" + players.B,
                        Components =
                        {
                            new CuiImageComponent()
                            {
                                Color = "0.35 0.35 0.35 0.45",
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin =
                                    $"{0.03278887} {0.8387395 - Math.Floor((double) players.B / 1) * 0.058}",
                                AnchorMax = $"{0.976581} {0.8923218 - Math.Floor((double) players.B / 1) * 0.058}"
                            }
                        }
                    });
                }

                cont.Add(new CuiElement()
                {
                    Parent = Layer + "Mod" + players.B,
                    Components =
                    {
                        new CuiRawImageComponent() {Png = GetImage(target.UserIDString)},
                        new CuiRectTransformComponent() {AnchorMin = "0 0", AnchorMax = "0.1464023 0.9851253"}
                    }
                });
                cont.Add(new CuiElement()
                { 
                    Parent = Layer + "Mod" + players.B,
                    Components =
                    {
                        new CuiTextComponent()
                        {
                            Text = $" {target.displayName.ToUpper().Substring(0, target.displayName.Length > 15 ? 15 :target.displayName.Length)}", Align = TextAnchor.MiddleLeft,
                            FontSize = 16
                        },
                        new CuiRectTransformComponent()
                            {AnchorMin = "0.1994236 0.2736453", AnchorMax = "1.003585 0.9851253"}
                    }
                });
                cont.Add(new CuiElement()
                {
                    Parent = Layer + "Mod" + players.B,
                    Components =
                    {
                        new CuiTextComponent()
                        {
                            Text = $"  {target.userID}", Color = "0.64 0.64 0.64 0.64",
                            Align = TextAnchor.UpperLeft, FontSize = 10, Font = regular
                        },
                        new CuiRectTransformComponent()
                            {AnchorMin = "0.1994236 0.01242494", AnchorMax = "1.003585 0.4013513"}
                    }
                });
                cont.Add(new CuiButton()
                {
                    Button =
                    {
                        Color = "0.64 0.64 0.64 0.86", Command = $"UISoReport profile {target.userID}",
                        Sprite = "assets/icons/tools.png"
                    },
                    RectTransform = {AnchorMin = "0.8615944 0", AnchorMax = "0.9975222 1"},
                    Text = {Text = ""}
                }, Layer + "Mod" + players.B);
            }

            CuiHelper.AddUi(player, cont);
        }

        private void LoadProfile(BasePlayer player, ulong targetId)
        {
            PlayerData f;
            if (!_playerData.TryGetValue(targetId, out f)) return;
            var cont = new CuiElementContainer();
            CuiHelper.DestroyUi(player, Layer + "Profile");
            cont.Add(_mod2Panel, Layer + "Main", Layer + "Profile");
            cont.Add(new CuiElement()
            {
                Parent = Layer + "Profile",
                Components =
                {
                    new CuiTextComponent()
                        {Align = TextAnchor.MiddleCenter, Text = f.UserName.ToUpper(), FontSize = 25},
                    new CuiRectTransformComponent()
                        {AnchorMin = "0.03270523 0.9434662", AnchorMax = "0.927317 0.9870253"}
                }
            });
            cont.Add(new CuiElement()
            {
                Parent = Layer + "Profile",
                Components =
                {
                    new CuiRawImageComponent() {Png = GetImage(targetId.ToString())},
                    new CuiRectTransformComponent()
                        {AnchorMin = "0.3160769 0.8257645", AnchorMax = "0.6369192 0.9416128"}
                }
            });
            cont.Add(new CuiElement()
            {
                Parent = Layer + "Profile",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Align = TextAnchor.MiddleCenter, Text = "Кол-во репортов: " + f.ReportCount,
                        Color = "0.64 0.64 0.64 0.64", FontSize = 10
                    },
                    new CuiRectTransformComponent()
                        {AnchorMin = "0.316077 0.7886934", AnchorMax = "0.6369191 0.8239112"}
                }
            });
            cont.Add(new CuiElement()
            {
                Parent = Layer + "Profile",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Align = TextAnchor.MiddleCenter, Text = "Кол-во проверок: " + f.AlertCount,
                        Color = "0.64 0.64 0.64 0.64", FontSize = 10
                    },
                    new CuiRectTransformComponent()
                        {AnchorMin = "0.316077 0.7738647", AnchorMax = "0.6369191 0.8090826"}
                }
            });
            cont.Add(new CuiButton()
            {
                Button = {Color = "0.65 0.65 0.65 0.45", Command = $"uisoreport alert {targetId}"},
                Text =
                {
                    Text = "ВЫЗВАТЬ НА ПРОВЕРКУ", Color = "0.64 0.64 0.64 0.87", Align = TextAnchor.MiddleCenter
                },
                RectTransform = {AnchorMin = "0.09125336 0.7442076", AnchorMax = "0.8570592 0.7738647"}
            }, Layer + "Profile");
            cont.Add(new CuiButton()
            {
                Button = {Color = "1 1 1 1", Sprite = "assets/icons/circle_open.png", Close = Layer + "Profile"},
                Text = {Text = ""},
                RectTransform = {AnchorMin = "0.4120954 0.7015755", AnchorMax = "0.5151403 0.7405006"}
            }, Layer + "Profile");
            cont.Add(new CuiButton()
            {
                Button = {Color = "1 1 1 1", Sprite = "assets/icons/vote_down.png", Close = Layer + "Profile"},
                Text = {Text = ""},
                RectTransform = {AnchorMin = "0.4120954 0.7015755", AnchorMax = "0.5151403 0.7405006"}
            }, Layer + "Profile");
            CuiHelper.AddUi(player, cont);
        }

        private void ReportFon(BasePlayer player)
        {
            var cont = new CuiElementContainer();
            List<ulong> f;
            CuiHelper.DestroyUi(player, Layer + "Red");
            cont.Add(_redPanel, Layer + "Main", Layer + "Red");
            //Лейбл
            PlayerData playerData;
            if (!_playerData.TryGetValue(player.userID, out playerData)) return;
            cont.Add(new CuiElement()
            {
                Parent = Layer + "Red",
                Components =
                {
                    new CuiTextComponent() {Text = "РЕПОРТ", Color = "0.64 0.64 0.64 0.86", FontSize = 30},
                    new CuiRectTransformComponent()
                        {AnchorMin = "0.03044713 0.9258574", AnchorMax = "0.9742392 0.9766592"}
                }
            });
            if (playerData.IsCooldown > 0)
            {
                cont.Add(new CuiElement()
                {
                    Parent = Layer + "Red",
                    Components =
                    {
                        new CuiTextComponent()
                        {
                            Text = "Подождите: " + FormatTime(TimeSpan.FromSeconds(playerData.IsCooldown), "ru"),
                            Color = "0.64 0.64 0.64 0.86", FontSize = 15, Align = TextAnchor.MiddleRight, Font = regular
                        },
                        new CuiRectTransformComponent()
                            {AnchorMin = "0.03044713 0.9258574", AnchorMax = "0.9742392 0.9766592"}
                    }
                });
            }

            //Инфо
            cont.Add(new CuiElement()
            {
                Parent = Layer + "Red",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = "Не забывайте что жалобу можно отравить не только на <b>одного человека</b>, а сразу на <b>шестерых</b>.", Color = "0.64 0.64 0.64 0.86", FontSize = 12,
                        Font = regular
                    },
                    new CuiRectTransformComponent() {AnchorMin = "0.03513062 0.146427", AnchorMax = "0.976581 0.214087"}
                }
            });
            //Первая причина
            cont.Add(new CuiButton()
            {
                Text = {Text = cfg.res1, Color = "0.64 0.64 0.64 0.86", Align = TextAnchor.MiddleCenter},
                Button = {Color = "0 0 0 0.64", Command = $"UiSoReport reportsend {cfg.res1}"},
                RectTransform = {AnchorMin = "0.03747275 0.07877186", AnchorMax = "0.4941462 0.1390175"}
            }, Layer + "Red");
            //Вторая причина
            cont.Add(new CuiButton()
            {
                Text = {Text = cfg.res2, Color = "0.64 0.64 0.64 0.86", Align = TextAnchor.MiddleCenter},
                Button = {Color = "0 0 0 0.64", Command = $"UiSoReport reportsend {cfg.res2}"},
                RectTransform = {AnchorMin = "0.5128826 0.07877186", AnchorMax = "0.9695571 0.1390175"}
            }, Layer + "Red");
            //3 причина
            cont.Add(new CuiButton()
            {
                Text = {Text = cfg.res3, Color = "0.64 0.64 0.64 0.86", Align = TextAnchor.MiddleCenter},
                Button = {Color = "0 0 0 0.64", Command = $"UiSoReport reportsend {cfg.res3}"},
                RectTransform = {AnchorMin = "0.5128826 0.01204333", AnchorMax = "0.9695571 0.07228888"}
            }, Layer + "Red");
            //4 причина
            cont.Add(new CuiButton()
            {
                Text = {Text = cfg.res4, Color = "0.64 0.64 0.64 0.86", Align = TextAnchor.MiddleCenter},
                Button = {Color = "0 0 0 0.64", Command = $"UiSoReport reportsend {cfg.res4}"},
                RectTransform = {AnchorMin = "0.03981462 0.01204333", AnchorMax = "0.4964881 0.07228888"}
            }, Layer + "Red");
            if (_reportList.TryGetValue(player.userID, out f))
            {
                foreach (var players in f.Select((i, t) => new {A = i, B = t}))
                {
                    var target = BasePlayer.FindByID(players.A);
                    if (target == null) continue;
                    cont.Add(new CuiElement()
                    {
                        Parent = Layer + "Red",
                        Name = Layer + "Red" + players.B,
                        Components =
                        {
                            new CuiImageComponent()
                            {
                                Color = "0.64 0.64 0.64 0.45",
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin =
                                    $"{0.03278887} {0.8387395 - Math.Floor((double) players.B / 1) * 0.058}",
                                AnchorMax = $"{0.976581} {0.8923218 - Math.Floor((double) players.B / 1) * 0.058}"
                            }
                        }
                    });
                    cont.Add(new CuiElement()
                    {
                        Parent = Layer + "Red" + players.B,
                        Components =
                        {
                            new CuiRawImageComponent() {Png = GetImage(target.UserIDString)},
                            new CuiRectTransformComponent() {AnchorMin = "0 0", AnchorMax = "0.1464023 0.9851253"}
                        }
                    });
                    cont.Add(new CuiElement()
                    {
                        Parent = Layer + "Red" + players.B,
                        Components =
                        {
                            new CuiTextComponent()
                            {
                                Text = $" {target.displayName.ToUpper()}", Align = TextAnchor.MiddleLeft,
                                FontSize = 16
                            },
                            new CuiRectTransformComponent()
                                {AnchorMin = "0.1994236 0.2736453", AnchorMax = "1.003585 0.9851253"}
                        }
                    });
                    cont.Add(new CuiElement()
                    {
                        Parent = Layer + "Red" + players.B,
                        Components =
                        {
                            new CuiTextComponent()
                            {
                                Text = $"  {target.userID}", Color = "0.64 0.64 0.64 0.64",
                                Align = TextAnchor.UpperLeft, FontSize = 10, Font = regular
                            },
                            new CuiRectTransformComponent()
                                {AnchorMin = "0.1994236 0.01242494", AnchorMax = "1.003585 0.4013513"}
                        }
                    });
                    cont.Add(new CuiButton()
                    {
                        Button =
                        {
                            Color = "0.64 0.64 0.64 0.86", Command = $"UISoReport remove {target.userID}",
                            Sprite = "assets/icons/vote_down.png"
                        },
                        RectTransform = {AnchorMin = "0.8515944 0", AnchorMax = "0.9975222 1"},
                        Text = {Text = ""}
                    }, Layer + "Red" + players.B);
                }
            }

            CuiHelper.AddUi(player, cont);
        }

        private void PlayerListLoad(BasePlayer player, int page, string find = "")
        {
            var cont = new CuiElementContainer();
            CuiHelper.DestroyUi(player, Layer + "Players");
            cont.Add(_playersPanel, Layer + "Main", Layer + "Players");
            ;
            if (page > 1)
            {
                cont.Add(new CuiButton()
                {
                    Button =
                    {
                        Command = $"uisoreport nextpage players {page - 1}", Color = HexToRustFormat("#4b602aCC")
                    },
                    Text = {Text = "<", Color = HexToRustFormat("#a1e432"), Align = TextAnchor.MiddleCenter},
                    RectTransform = {AnchorMin = "0.4305381 0.01297505", AnchorMax = "0.4722021 0.05097298"}
                }, Layer + "Players");
            }
            else
            {
                cont.Add(new CuiButton()
                {
                    Button = {Command = $"", Color = HexToRustFormat($"#24291dCC")},
                    Text = {Text = "<", Color = HexToRustFormat("#a1e432"), Align = TextAnchor.MiddleCenter},
                    RectTransform = {AnchorMin = "0.4305381 0.01297505", AnchorMax = "0.4722021 0.05097298"}
                }, Layer + "Players");
            }

            cont.Add(new CuiButton()
            {
                Button = {Color = "0 0 0 0.64"},
                Text =
                    {Text = page.ToString(), Color = HexToRustFormat("#a1e432"), Align = TextAnchor.MiddleCenter},
                RectTransform = {AnchorMin = "0.4786112 0.01297505", AnchorMax = "0.5202752 0.05097298"}
            }, Layer + "Players");
            cont.Add(new CuiButton()
            {
                Button =
                    {Command = $"uisoreport nextpage players {page + 1}", Color = HexToRustFormat("#4b602aCC")},
                Text = {Text = ">", Color = HexToRustFormat("#a1e432"), Align = TextAnchor.MiddleCenter},
                RectTransform = {AnchorMin = "0.5256162 0.01297505", AnchorMax = "0.5672802 0.05097298"}
            }, Layer + "Players");
            cont.Add(new CuiElement()
            {
                Parent = Layer + "Players",
                Components =
                {
                    new CuiTextComponent() {Text = "НАЙДИ ИГРОКА(-ОВ)", Color = "0.64 0.64 0.64 0.86", FontSize = 15},
                    new CuiRectTransformComponent()
                        {AnchorMin = "0.02672157 0.9277115", AnchorMax = "0.3984902 0.9785135"}
                }
            });
            cont.Add(new CuiElement()
            {
                Parent = Layer + "Players",
                Components =
                {
                    new CuiTextComponent()
                        {Text = "НА КОТОРОГО ХОЧЕШЬ ПОЖАЛОВАТЬСЯ", Color = "0.64 0.64 0.64 0.34", FontSize = 9},
                    new CuiRectTransformComponent()
                        {AnchorMin = "0.02776662 0.9341984", AnchorMax = "0.3963884 0.9564412"}
                }
            });
            //Инфо
            cont.Add(new CuiElement()
            {
                Parent = Layer + "Players",
                Components =
                {
                    new CuiImageComponent() {Color = "0.64 0.64 0.64 0.35"},
                    new CuiRectTransformComponent()
                        {AnchorMin = "0.5256127 0.936052", AnchorMax = "0.9710997 0.9785135"}
                }
            });
            cont.Add(new CuiElement()
            {
                Parent = Layer + "Players",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = "   НАЙТИ ИГРОКА ПО НИКУ/STEAMID", Align = TextAnchor.MiddleLeft,
                        Color = "0.64 0.64 0.64 0.10"
                    },
                    new CuiRectTransformComponent()
                        {AnchorMin = "0.5256127 0.936052", AnchorMax = "0.9710997 0.9785135"}
                }
            });

            cont.Add(new CuiElement()
            {
                Parent = Layer + "Players",
                Components =
                {
                    new CuiInputFieldComponent()
                    {
                        Text = "", Color = "0.64 0.64 0.64 0.64", Align = TextAnchor.MiddleLeft,
                        Command = $"UISoReport find {page} "
                    },
                    new CuiRectTransformComponent()
                        {AnchorMin = "0.5256127 0.936052", AnchorMax = "0.9710997 0.9785135"}
                }
            });
            cont.Add(new CuiElement()
            {
                Parent = Layer + "Players",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Sprite = "assets/icons/examine.png",
                        Color = "0.64 0.64 0.64 0.64"
                    },
                    new CuiRectTransformComponent()
                        {AnchorMin = "0.9207384 0.936052", AnchorMax = "0.9710997 0.9785135"}
                }
            });
            if (find != "" && BasePlayer.Find(find) != null)
            {
                var targetPlayer = BasePlayer.Find(find);
                cont.Add(new CuiElement()
                {
                    Parent = Layer + "Players",
                    Name = Layer + "Players" + "0",
                    Components =
                    { 
                        new CuiImageComponent() {Color = "0.64 0.64 0.64 0.25"},
                        new CuiRectTransformComponent
                        {
                            AnchorMin =
                                "0.02964282 0.8609824",
                            AnchorMax =
                                "0.3365291 0.9117843"
                        }
                    }
                });

                cont.Add(new CuiElement()
                {
                    Parent = Layer + "Players" + "0",
                    Components =
                    {
                        new CuiRawImageComponent() {Png = GetImage(targetPlayer.UserIDString)},
                        new CuiRectTransformComponent() {AnchorMin = "0 0", AnchorMax = "0.199423 0.9851253"}
                    }
                });
                cont.Add(new CuiElement()
                {
                    Parent = Layer + "Players" + "0",
                    Components =
                    {
                        new CuiTextComponent()
                        {
                            Text = $" {targetPlayer.displayName.ToUpper().Substring(0, targetPlayer.displayName.Length > 15 ? 15 : targetPlayer.displayName.Length )}", Align = TextAnchor.MiddleLeft,
                            FontSize = 16
                        },
                        new CuiRectTransformComponent()
                            {AnchorMin = "0.1994236 0.2736453", AnchorMax = "1.003585 0.9851253"}
                    }
                });
                cont.Add(new CuiElement()
                {
                    Parent = Layer + "Players" + "0",
                    Components =
                    {
                        new CuiTextComponent()
                        {
                            Text = $"  {targetPlayer.userID}", Color = "0.64 0.64 0.64 0.64",
                            Align = TextAnchor.UpperLeft, FontSize = 10, Font = regular
                        },
                        new CuiRectTransformComponent()
                            {AnchorMin = "0.1994236 0.01242494", AnchorMax = "1.003585 0.4013513"}
                    }
                });

                cont.Add(new CuiButton()
                {
                    Button = {Color = "0 0 0 0", Command = $"UISoReport add {page} {targetPlayer.userID}"},
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                    Text = {Text = ""}
                }, Layer + "Players" + "0");
            }
            else
            {
                foreach (var players in BasePlayer.activePlayerList
                    .Select((i, t) => new {A = i, B = t - (page - 1) * 45}).Skip((page - 1) * 45).Take(45))
                {
                    List<ulong> f;
                    if (_reportList.TryGetValue(player.userID, out f) && f.Contains(players.A.userID))
                    {
                        cont.Add(new CuiElement()
                        {
                            Parent = Layer + "Players",
                            Name = Layer + "Players" + players.B,
                            Components =
                            {
                                new CuiImageComponent()
                                {
                                    Color = HexToRustFormat("#93c9415a")
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin =
                                        $"{0.02964282 + players.B * 0.315 - Math.Floor((double) players.B / 3) * 3 * 0.315} {0.8609824 - Math.Floor((double) players.B / 3) * 0.058}",
                                    AnchorMax =
                                        $"{0.3365291 + players.B * 0.315 - Math.Floor((double) players.B / 3) * 3 * 0.315} {0.9117843 - Math.Floor((double) players.B / 3) * 0.058}"
                                }
                            }
                        });
                    }
                    else
                    {
                        cont.Add(new CuiElement()
                        {
                            Parent = Layer + "Players",
                            Name = Layer + "Players" + players.B,
                            Components =
                            {
                                new CuiImageComponent()
                                    {Color = "0.64 0.64 0.64 0.25"},
                                new CuiRectTransformComponent
                                {
                                    AnchorMin =
                                        $"{0.02964282 + players.B * 0.315 - Math.Floor((double) players.B / 3) * 3 * 0.315} {0.8609824 - Math.Floor((double) players.B / 3) * 0.058}",
                                    AnchorMax =
                                        $"{0.3365291 + players.B * 0.315 - Math.Floor((double) players.B / 3) * 3 * 0.315} {0.9117843 - Math.Floor((double) players.B / 3) * 0.058}"
                                }
                            }
                        });
                    }

                    cont.Add(new CuiElement()
                    {
                        Parent = Layer + "Players" + players.B,
                        Components =
                        {
                            new CuiRawImageComponent() {Png = GetImage(players.A.UserIDString)},
                            new CuiRectTransformComponent() {AnchorMin = "0 0", AnchorMax = "0.199423 0.9851253"}
                        }
                    });
                    cont.Add(new CuiElement()
                    {
                        Parent = Layer + "Players" + players.B,
                        Components =
                        {
                            new CuiTextComponent() 
                            {
                                Text = $" {players.A.displayName.ToUpper().Substring(0, players.A.displayName.Length > 15 ? 15 : players.A.displayName.Length)}", Align = TextAnchor.MiddleLeft,
                                FontSize = 16
                            },
                            new CuiRectTransformComponent()
                                {AnchorMin = "0.1994236 0.2736453", AnchorMax = "1.003585 0.9851253"}
                        }
                    });
                    cont.Add(new CuiElement()
                    {
                        Parent = Layer + "Players" + players.B,
                        Components =
                        {
                            new CuiTextComponent()
                            {
                                Text = $"  {players.A.userID}", Color = "0.64 0.64 0.64 0.64",
                                Align = TextAnchor.UpperLeft, FontSize = 10, Font = regular
                            },
                            new CuiRectTransformComponent()
                                {AnchorMin = "0.1994236 0.01242494", AnchorMax = "1.003585 0.4013513"}
                        }
                    });
                    cont.Add(new CuiButton()
                    {
                        Button = {Color = "0 0 0 0", Command = $"UISoReport add {page} {players.A.userID}"},
                        RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                        Text = {Text = ""}
                    }, Layer + "Players" + players.B);
                }
            }

            CuiHelper.AddUi(player, cont);
        }

        #endregion

        #region Commands

        [ChatCommand("discord")]
        private void SayDiscord(BasePlayer player, string c, string[] a)
        {
            if (!_alertList.ContainsValue(player.userID))
            {
                ReplySend(player, "Вы не на проверке!");
                return;
            }
            if (a.Length < 1)
            {
                ReplySend(player, "/discord {ВАШ ДИСКОРД}");
                return;
            }
            var arg = string.Join(" ", a.ToArray());
            var admin = BasePlayer.FindByID(_alertList.First(p => p.Value == player.userID).Key);
            ReplySend(player, "Дискорд успешно отправлен!");
            if (admin != null)
                ReplySend(admin, $"Игрок {player.displayName}[{player.userID}] отправил свой дискорд: {arg}");
            if (cfg.Vkontakte) 
                webrequest.Enqueue( 
                    "https://api.vk.com/method/messages.send?user_ids=" + cfg.vkadmin + "&message=" + $"Игрок {player.displayName}[{player.userID}] отправил свой дискорд: {arg}" +
                    "&v=5.103" + "&random_id=" + UnityEngine.Random.Range(Int32.MinValue, Int32.MaxValue) +
                    "&access_token=" + cfg.vkAcces, null, (code, response) => { }, this);
            if(cfg.TelegramUse) webrequest.Enqueue($"https://api.telegram.org/bot{cfg.botToken}/sendMessage?chat_id=@{cfg.chatid}&text=Игрок {player.displayName}[{player.userID}] отправил свой дискорд: {arg}", null, (code, response) => { if(code == 400) Puts("Chat not found");},this, RequestMethod.POST);
            if (cfg.discord) SendDiscord("SendPlayerDiscord", $"Игрок {player.displayName}[{player.userID}] отправил свой дискорд: {arg}", cfg.discordhook2);
        }

        [ConsoleCommand("UISoReport")]
        private void SoCommands(ConsoleSystem.Arg arg)
        {
            var targetPlayer = arg.Player();
            switch (arg.Args[0])
            {
                case "nextpage":
                    if (arg.Args[1] == "players")
                        PlayerListLoad(targetPlayer, arg.Args[2].ToInt());
                    if (arg.Args[1] == "mod")
                        ModeratorInterface(targetPlayer, arg.Args[2].ToInt());
                    break;
                case "alert":
                    ulong ss;
                    if (_alertList.TryGetValue(targetPlayer.userID, out ss))
                    {
                        var target = BasePlayer.FindByID(ss);
                        ReplySend(target, $"Проверка окончена!");
                        ReplySend(targetPlayer, $"Вы закончили проверку игрока {target.displayName}");
                        CuiHelper.DestroyUi(target, "AlerUISo");
                        _alertList.Remove(targetPlayer.userID);
                    }
                    else if (!_alertList.ContainsValue(ulong.Parse(arg.Args[1])))
                    {
                        PlayerData data;
                        if (!_playerData.TryGetValue(ulong.Parse(arg.Args[1]), out data)) return;
                        data.AlertCount += 1;
                        _alertList.Add(targetPlayer.userID, ulong.Parse(arg.Args[1]));
                        Alert(ulong.Parse(arg.Args[1]));
                    }

                    break;
                case "find":
                    if(arg.Args.Length < 3) return;
                    PlayerListLoad(targetPlayer, arg.Args[1].ToInt(), arg.Args[2]);
                    break;
                case "add":
                    List<ulong> f;

                    if (_reportList.TryGetValue(targetPlayer.userID, out f))
                    {
                        if (!f.Contains(ulong.Parse(arg.Args[2])))
                            if (f.Count < 6)
                                f.Add(ulong.Parse(arg.Args[2]));
                    }
                    else
                        _reportList.Add(targetPlayer.userID, new List<ulong>()
                        {
                            ulong.Parse(arg.Args[2]),
                        });

                    PlayerListLoad(targetPlayer, arg.Args[1].ToInt());
                    ReportFon(targetPlayer);
                    break;
                case "remove":
                    if (_reportList.TryGetValue(targetPlayer.userID, out f))
                        f.Remove(ulong.Parse(arg.Args[1]));

                    PlayerListLoad(targetPlayer, 1);
                    ReportFon(targetPlayer);
                    break;
                case "reportsend":
                    PlayerData playerDat;
                    if (!_playerData.TryGetValue(targetPlayer.userID, out playerDat)) return;
                    if (playerDat.IsCooldown > 0) return;
                    if (_reportList.TryGetValue(targetPlayer.userID, out f))
                    {
                        var reportReasone = arg.Args.ToList();
                        reportReasone.RemoveAt(0);
                        string text =
                            $"Игрок {targetPlayer.displayName}[{targetPlayer.userID}] пожаловался по причине {string.Join(" ", reportReasone.ToArray())} на игрока(-ов):";
                        if (f.Count < 1) return;
                        foreach (var reportSend in f)
                        {
                            Interface.CallHook("RA_ReportSend", targetPlayer.userID.ToString(), reportSend.ToString(), string.Join(" ", reportReasone.ToArray()));
                            var target = BasePlayer.FindByID(reportSend);
                            if (target == null) continue;
                            PlayerData dataPlayer;
                            if (_playerData.TryGetValue(target.userID, out dataPlayer))
                            {
                                dataPlayer.ReportCount += 1;
                                text +=
                                    $"\n{target.displayName} [{target.userID}] \nКол-во репортов: {dataPlayer.ReportCount}\nSteam: [КЛИК](https://steamcommunity.com/profiles/{target.userID})";
                            }
                        }

                        f.Clear();
                        if (cfg.discord) SendDiscord("REPORT", text, cfg.discordhook);
                        if (cfg.Vkontakte) webrequest.Enqueue("https://api.vk.com/method/messages.send?user_ids=" + cfg.vkadmin + "&message=" + text + "&v=5.92" + "&random_id=" + UnityEngine.Random.Range(Int32.MinValue, Int32.MaxValue) + "&access_token=" + cfg.vkAcces, null, (code, response) => { }, this);
                        if(cfg.TelegramUse) webrequest.Enqueue($"https://api.telegram.org/bot{cfg.botToken}/sendMessage?chat_id=@{cfg.chatid}&text={text}", null, (code, response) => { if(code == 400) Puts("Chat not found");},this, RequestMethod.POST);
                        Puts(text);
                        playerDat.ReportCD = cfg.cooldown + CurrentTime();
                    }

                    PlayerListLoad(targetPlayer, 1);
                    ReportFon(targetPlayer);
                    break;
                case "modmenu":
                    if (arg.Args[1] == "open")
                    {
                        CuiHelper.DestroyUi(targetPlayer, Layer + "BMod");
                        ModeratorInterface(targetPlayer, 1);
                    }

                    break;
                case "profile":
                    LoadProfile(targetPlayer, ulong.Parse(arg.Args[1]));
                    break;
            }
        }
        [ChatCommand("alert")]
        private void AlertCommand(BasePlayer player, string c, string[] a)
        {
            if (permission.UserHasPermission(player.UserIDString, "soreport.admin"))
            {
                if (a.Length < 1)
                {
                    ReplySend(player, "/alert {НИК/STEAMID}");
                    return;
                }

                var arg = String.Join(" ", a.ToArray());
                var target = BasePlayer.Find(arg);
                if (target == null)
                {
                    ReplySend(player, "Игрок не найден");
                    return;
                }

                ulong ss;
                if (_alertList.TryGetValue(player.userID, out ss))
                {
                    _alertList.Remove(player.userID);
                    var targes = BasePlayer.FindByID(ss);
                    CuiHelper.DestroyUi(targes, "AlerUISo");
                    
                    ReplySend(targes, $"Проверка окончена!");
                    ReplySend(player, $"Вы закончили проверку игрока {targes.displayName}");
                    return;
                }
                PlayerData data;
                if(!_playerData.TryGetValue(target.userID, out data)) return;
                data.AlertCount += 1;
                _alertList.Add(player.userID, target.userID);
                Alert(target.userID);
                ReplySend(player, $"Вы вызвали игрока {target.displayName} на проверку");
            }
        }

        #endregion
        
        #region DS

        #region discord

        private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings();
        private static SoReport _instance;

        private readonly Dictionary<string, string> _headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json"
        };

        private class FancyMessage
        {
            [JsonProperty("content")] private string Content { get; set; }
            [JsonProperty("tts")] private bool TextToSpeech { get; set; }
            [JsonProperty("embeds")] private EmbedBuilder[] Embeds { get; set; }

            public FancyMessage WithContent(string value)
            {
                Content = value;
                return this;
            }

            public FancyMessage SetEmbed(EmbedBuilder value)
            {
                Embeds = new[] {value};
                return this;
            }

            public string ToJson()
            {
                return JsonConvert.SerializeObject(this, _instance._jsonSettings);
            }
        }

        public class EmbedBuilder
        {
            public EmbedBuilder()
            {
                Fields = new List<Field>();
            }

            [JsonProperty("title")] private string Title { get; set; }
            [JsonProperty("color")] private int Color { get; set; }
            [JsonProperty("fields")] private List<Field> Fields { get; }

            public EmbedBuilder WithTitle(string title)
            {
                Title = title;
                return this;
            }

            public EmbedBuilder SetColor(int color)
            {
                Color = color;
                return this;
            }

            public EmbedBuilder AddField(Field field)
            {
                Fields.Add(field);
                return this;
            }

            public class Field
            {
                public Field(string name, object value)
                {
                    Name = name;
                    Value = value;
                }

                [JsonProperty("name")] public string Name { get; set; }
                [JsonProperty("value")] public object Value { get; set; }
            }
        }

        private class Request
        {
            private readonly string _payload;
            private readonly Plugin _plugin;
            private readonly string _url;

            public void Send()
            {
                _instance.webrequest.Enqueue(_url, _payload, (code, rawResponse) => { }, _instance, RequestMethod.POST,
                    _instance._headers);
            }

            public static void Send(string url, FancyMessage message, Plugin plugin = null)
            {
                new Request(url, message, plugin).Send();
            }

            private Request(string url, FancyMessage message, Plugin plugin = null)
            {
                _url = url;
                _payload = message.ToJson();
                _plugin = plugin;
            }
        }

        #endregion

        private void SendDiscord(string type, string reason, string hook)
        {
            var fields = new List<EmbedBuilder.Field>();
            fields.Add(new EmbedBuilder.Field(type, reason));
            var serializedObject = JsonConvert.SerializeObject(fields);
            var builder = new EmbedBuilder().SetColor(104403);
            foreach (var field in JsonConvert.DeserializeObject<EmbedBuilder.Field[]>(serializedObject))
                builder.AddField(field);
            var payload = new FancyMessage().WithContent(cfg.Lable).SetEmbed(builder);
            Request.Send(hook, payload, this);
        }

        #endregion

        #region L

        private Dictionary<ulong, ulong> _alertList = new Dictionary<ulong, ulong>();
        private Dictionary<ulong, List<ulong>> _reportList = new Dictionary<ulong, List<ulong>>();

        #endregion

        #region Hooks

        void OnUserDisconnected(IPlayer player)
        {
            var basePlayer = player.Object as BasePlayer;
            if (basePlayer != null)
            {
                if (_reportList.ContainsKey(basePlayer.userID))
                    _reportList.Remove(basePlayer.userID);
                var find = _reportList.ToList().Where(p => p.Value.Contains(basePlayer.userID));
                foreach (var keyValuePair in find)
                {
                    keyValuePair.Value.Remove(basePlayer.userID);
                }

            }
        }
        private void OnPlayerConnected(BasePlayer player)
        {
            if (!_playerData.ContainsKey(player.userID))
                _playerData.Add(player.userID, new PlayerData() {UserName = player.displayName, ReportCount = 0});
        }

        private void OnServerInitialized()
        {
            permission.RegisterPermission("soreport.admin", this);
            _instance = this;
            _playerData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerData>>("SoReport/Players");
            foreach (var basePlayer in BasePlayer.activePlayerList)
            {
                OnPlayerConnected(basePlayer);
            }
        }
 
        private void Unload()
        {
            Interface.Oxide.DataFileSystem.WriteObject("SoReport/Players", _playerData);
            foreach (var basePlayer in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(basePlayer, Layer);
                CuiHelper.DestroyUi(basePlayer, "AlerUISo");
            }
        }

        #endregion

        #region Help

        private static string Format(int units, string form1, string form2, string form3)
        {
            var tmp = units % 10;
            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9) return $"{units} {form1}";
            if (tmp >= 2 && tmp <= 4) return $"{units} {form2}";
            return $"{units} {form3}";
        }

        public static string FormatTime(TimeSpan time, string language, int maxSubstr = 5)
        {
            var result = string.Empty;
            switch (language)
            {
                case "ru":
                    var i = 0;
                    if (time.Days != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result)) result += " ";
                        result += $"{Format(time.Days, "дней", "дня", "день")}";
                        i++;
                    }

                    if (time.Hours != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result)) result += " ";
                        result += $"{Format(time.Hours, "часов", "часа", "час")}";
                        i++;
                    }

                    if (time.Minutes != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result)) result += " ";
                        result += $"{Format(time.Minutes, "минут", "минуты", "минута")}";
                        i++;
                    }

                    if (time.Seconds != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result)) result += " ";
                        result += $"{Format(time.Seconds, "сек", "сек", "сек")}";
                        i++;
                    }

                    break;
                case "en":
                {
                    var i2 = 0;
                    if (time.Days != 0 && i2 < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result)) result += " ";
                        result += $"{Format(time.Days, "days'", "day's", "day")}";
                        i2++;
                    }

                    if (time.Hours != 0 && i2 < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result)) result += " ";
                        result += $"{Format(time.Hours, "hours'", "hour's", "hour")}";
                        i2++;
                    }

                    if (time.Minutes != 0 && i2 < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result)) result += " ";
                        result += $"{Format(time.Minutes, "minutes", "minutes", "minute")}";
                        i2++;
                    }

                    if (time.Seconds != 0 && i2 < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result)) result += " ";
                        result += $"{Format(time.Seconds, "second", "seconds", "second")}";
                        i2++;
                    }

                    break;
                }
            }

            return result;
        }

        [PluginReference] private Plugin ImageLibrary;

        public string GetImage(string shortname, ulong skin = 0) =>
            (string) ImageLibrary.Call("GetImage", shortname, skin);

        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex)) hex = "#FFFFFFFF";
            var str = hex.Trim('#');
            if (str.Length == 6) str += "FF";
            if (str.Length != 8)
            {
                throw new Exception(hex);
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);
            Color color = new Color32(r, g, b, a);
            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }

        private void ReplySend(BasePlayer player, string message) => player.SendConsoleCommand("chat.add 0",
            new object[2]
                {76561199015371818, $"<size=18><color=purple>SoReport</color></size>\n{message}"});

        private static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        private static double CurrentTime() => DateTime.UtcNow.Subtract(epoch).TotalSeconds;

        #endregion
    }
}
