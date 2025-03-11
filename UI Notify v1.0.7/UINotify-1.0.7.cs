using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("UI Notify", "Mevent", "1.0.7")]
    [Description("Adds custom notifications for players")]
    public class UINotify : CovalencePlugin
    {
        #region Fields

        [PluginReference] private Plugin ImageLibrary = null;

        private const string Layer = "UI.Notify";

        private static UINotify _instance;

        private readonly Dictionary<BasePlayer, NotifyComponent> _notifications =
            new Dictionary<BasePlayer, NotifyComponent>();

        private class NotifyData
        {
            public string Message;

            public int Type;

            public readonly string Uid = CuiHelper.GetGuid();

            public float StartTime;
        }

        private const string
            PermSeeNotify = "uinotify.see",
            PermNotify = "uinotify.notify",
            PermPlayerNotify = "uinotify.player",
            PermAllPlayersNotify = "uinotify.allplayer";

        #endregion

        #region Config

        private static Configuration _config;

        private class Configuration
        {
            [JsonProperty(PropertyName = "Display type (Overlay/Hud)")]
            public string DisplayType = "Overlay";

            [JsonProperty(PropertyName = "Height")]
            public float Height = 50;

            [JsonProperty(PropertyName = "Width")] public float Width = 260;

            [JsonProperty(PropertyName = "X Margin")]
            public float XMargin = 20;

            [JsonProperty(PropertyName = "Y Margin")]
            public float YMargin = 5;

            [JsonProperty(PropertyName = "Y Indent")]
            public float ConstYSwitch = -50f;

            [JsonProperty(PropertyName = "Notify Cooldown")]
            public float Cooldown = 10f;

            [JsonProperty(PropertyName = "Notifications (type - settings)",
                ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<int, TypeConf> Types = new Dictionary<int, TypeConf>
            {
                [0] = new TypeConf
                {
                    BackgroundColor = new IColor("#000000", 98),
                    EnableGradient = true,
                    GradientColor = new IColor("#4B68FF", 35),
                    Sprite = "assets/content/ui/ui.background.transparent.linearltr.tga",
                    Material = "Assets/Icons/IconMaterial.mat",
                    IconColor = new IColor("#4B68FF", 100),
                    IconText = "!",
                    TitleKey = "Notification",
                    TitleColor = new IColor("#FFFFFF", 50),
                    TextColor = new IColor("#FFFFFF", 100),
                    FadeIn = 0.1f,
                    FadeOut = 1f,
                    IconTextColor = new IColor("#FFFFFF", 100),
                    Effect = "assets/bundled/prefabs/fx/notice/item.select.fx.prefab",
                    Image = new ImageSettings
                    {
                        AnchorMin = "0 0", AnchorMax = "0 0",
                        OffsetMin = "12.5 12.5",
                        OffsetMax = "37.5 37.5",
                        Enabled = false,
                        Image = string.Empty
                    }
                },
                [1] = new TypeConf
                {
                    BackgroundColor = new IColor("#000000", 98),
                    EnableGradient = true,
                    GradientColor = new IColor("#FF6060", 35),
                    Sprite = "assets/content/ui/ui.background.transparent.linearltr.tga",
                    Material = "Assets/Icons/IconMaterial.mat",
                    IconColor = new IColor("#FF6060", 100),
                    IconText = "X",
                    TitleKey = "Error",
                    TitleColor = new IColor("#FFFFFF", 50),
                    TextColor = new IColor("#FFFFFF", 100),
                    FadeIn = 0.1f,
                    FadeOut = 1f,
                    IconTextColor = new IColor("#FFFFFF", 100),
                    Effect = "assets/bundled/prefabs/fx/notice/item.select.fx.prefab",
                    Image = new ImageSettings
                    {
                        AnchorMin = "0 0", AnchorMax = "0 0",
                        OffsetMin = "12.5 12.5",
                        OffsetMax = "37.5 37.5",
                        Enabled = false,
                        Image = string.Empty
                    }
                }
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new Exception();
                SaveConfig();
            }
            catch
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
            }
        }

        private class TypeConf
        {
            [JsonProperty(PropertyName = "Background Color")]
            public IColor BackgroundColor;

            [JsonProperty(PropertyName = "Enable Gradient?")]
            public bool EnableGradient;

            [JsonProperty(PropertyName = "Gradient Color")]
            public IColor GradientColor;

            [JsonProperty(PropertyName = "Sprite")]
            public string Sprite;

            [JsonProperty(PropertyName = "Material")]
            public string Material;

            [JsonProperty(PropertyName = "Icon Color")]
            public IColor IconColor;

            [JsonProperty(PropertyName = "Icon Text")]
            public string IconText;

            [JsonProperty(PropertyName = "Icon Text Color")]
            public IColor IconTextColor;

            [JsonProperty(PropertyName = "Title Key (lang)")]
            public string TitleKey;

            [JsonProperty(PropertyName = "Title Color")]
            public IColor TitleColor;

            [JsonProperty(PropertyName = "Text Color")]
            public IColor TextColor;

            [JsonProperty(PropertyName = "Fade Out")]
            public float FadeOut;

            [JsonProperty(PropertyName = "Fade In")]
            public float FadeIn;

            [JsonProperty(PropertyName = "Sound Effect (empty - disable)")]
            public string Effect;

            [JsonProperty(PropertyName = "Image Settings")]
            public ImageSettings Image;

            public void Get(BasePlayer player, ref CuiElementContainer container, NotifyData data, float ySwitch)
            {
                container.Add(new CuiPanel
                {
                    RectTransform =
                    {
                        AnchorMin = "0 0", AnchorMax = "1 1",
                        OffsetMin = $"{-_config.Width - _config.XMargin} {ySwitch - _config.Height}",
                        OffsetMax = $"{-_config.XMargin} {ySwitch}"
                    },
                    Image =
                    {
                        Color = BackgroundColor.Get(),
                        FadeIn = FadeIn
                    },
                    FadeOut = FadeOut
                }, Layer, Layer + $".Notify.{data.Uid}");

                #region Gradient

                if (EnableGradient)
                    container.Add(new CuiPanel
                    {
                        RectTransform =
                        {
                            AnchorMin = "0 0", AnchorMax = "1 1"
                        },
                        Image =
                        {
                            Color = GradientColor.Get(),
                            Sprite = Sprite,
                            Material = Material,
                            FadeIn = FadeIn
                        },
                        FadeOut = FadeOut
                    }, Layer + $".Notify.{data.Uid}");

                #endregion

                #region Icon

                if (Image != null && Image.Enabled && !string.IsNullOrEmpty(Image.Image))
                {
                    container.Add(new CuiElement
                    {
                        Name = Layer + $".Notify.{data.Uid}.Icon",
                        Parent = Layer + $".Notify.{data.Uid}",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = _instance.ImageLibrary.Call<string>("GetImage", Image.Image)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = Image.AnchorMin,
                                AnchorMax = Image.AnchorMax,
                                OffsetMin = Image.OffsetMin,
                                OffsetMax = Image.OffsetMax
                            }
                        }
                    });
                }
                else
                {
                    container.Add(new CuiPanel
                    {
                        RectTransform =
                        {
                            AnchorMin = "0 0", AnchorMax = "0 0",
                            OffsetMin = "12.5 12.5",
                            OffsetMax = "37.5 37.5"
                        },
                        Image =
                        {
                            Color = IconColor.Get(),
                            FadeIn = FadeIn
                        },
                        FadeOut = FadeOut
                    }, Layer + $".Notify.{data.Uid}", Layer + $".Notify.{data.Uid}.Icon");

                    container.Add(new CuiLabel
                    {
                        RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                        Text =
                        {
                            Text = $"{IconText}",
                            Align = TextAnchor.MiddleCenter,
                            Font = "robotocondensed-regular.ttf",
                            FontSize = 12,
                            Color = IconTextColor.Get(),
                            FadeIn = FadeIn
                        },
                        FadeOut = FadeOut
                    }, Layer + $".Notify.{data.Uid}.Icon");
                }

                #endregion

                #region Title

                container.Add(new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = "0 0.5", AnchorMax = "1 1",
                        OffsetMin = "47.5 0", OffsetMax = "0 0"
                    },
                    Text =
                    {
                        Text = _instance.Msg(player, TitleKey),
                        Align = TextAnchor.LowerLeft,
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 12,
                        Color = TitleColor.Get(),
                        FadeIn = FadeIn
                    },
                    FadeOut = FadeOut
                }, Layer + $".Notify.{data.Uid}");

                #endregion

                #region Message

                container.Add(new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = "0 0", AnchorMax = "1 0.5",
                        OffsetMin = "47.5 0", OffsetMax = "0 0"
                    },
                    Text =
                    {
                        Text = $"{data.Message}",
                        Align = TextAnchor.UpperLeft,
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 10,
                        Color = TextColor.Get(),
                        FadeIn = FadeIn
                    },
                    FadeOut = FadeOut
                }, Layer + $".Notify.{data.Uid}");

                #endregion
            }
        }

        private class InterfacePosition
        {
            public string AnchorMin;

            public string AnchorMax;

            public string OffsetMin;

            public string OffsetMax;
        }

        private class ImageSettings : InterfacePosition
        {
            [JsonProperty(PropertyName = "Enabled")]
            public bool Enabled;

            [JsonProperty(PropertyName = "Image")] public string Image;
        }

        private class IColor
        {
            [JsonProperty(PropertyName = "HEX")] public string Hex;

            [JsonProperty(PropertyName = "Opacity (0 - 100)")]
            public float Alpha;

            public string Get()
            {
                if (string.IsNullOrEmpty(Hex)) Hex = "#FFFFFF";

                var str = Hex.Trim('#');
                if (str.Length != 6) throw new Exception(Hex);
                var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
                var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
                var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);

                return $"{(double) r / 255} {(double) g / 255} {(double) b / 255} {Alpha / 100}";
            }

            public IColor(string hex, float alpha)
            {
                Hex = hex;
                Alpha = alpha;
            }
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        protected override void LoadDefaultConfig()
        {
            _config = new Configuration();
        }

        #endregion

        #region Hooks

        private void OnServerInitialized()
        {
            _instance = this;

            LoadImages();

            RegisterPermissions();

            AddCovalenceCommand("uinotify.show", nameof(CmdShowNotify));
            AddCovalenceCommand("uinotify.player", nameof(CmdShowPlayerNotify));
            AddCovalenceCommand("uinotify.allplayers", nameof(CmdShowAllPlayerNotify));
        }

        private void Unload()
        {
            _notifications.Values.ToList().ForEach(notify =>
            {
                if (notify != null)
                    notify.Kill();
            });

            foreach (var player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, Layer);

            _instance = null;
            _config = null;
        }

        #endregion

        #region Commands

        private void CmdShowNotify(IPlayer cov, string command, string[] args)
        {
            if (!cov.IsServer && !cov.HasPermission(PermNotify)) return;

            var player = cov.Object as BasePlayer;
            if (player == null) return;

            int type;
            if (args.Length < 2 || !int.TryParse(args[0], out type))
            {
                cov.Reply(Msg(cov.Id, SyntaxNotify, command));
                return;
            }

            var message = string.Join(" ", args.Skip(1));
            if (string.IsNullOrEmpty(message)) return;

            SendNotify(player, type, message);
        }

        private void CmdShowPlayerNotify(IPlayer cov, string command, string[] args)
        {
            if (!cov.IsServer && !cov.HasPermission(PermPlayerNotify)) return;

            int type;
            if (args.Length < 3 || !int.TryParse(args[1], out type))
            {
                cov.Reply(Msg(cov.Id, SyntaxPlayerNotify, command));
                return;
            }

            var target = covalence.Players.FindPlayer(args[0])?.Object as BasePlayer;
            if (target == null)
            {
                cov.Reply(Msg(cov.Id, NotFound, args[0]));
                return;
            }

            var message = string.Join(" ", args.Skip(2));
            if (string.IsNullOrEmpty(message)) return;

            SendNotify(target, type, message);
        }

        private void CmdShowAllPlayerNotify(IPlayer cov, string command, string[] args)
        {
            if (!cov.IsServer && !cov.HasPermission(PermAllPlayersNotify)) return;

            int type;
            if (args.Length < 2 || !int.TryParse(args[0], out type))
            {
                cov.Reply(Msg(cov.Id, SyntaxAllPlayerNotify, command));
                return;
            }

            var message = string.Join(" ", args.Skip(1));
            if (string.IsNullOrEmpty(message)) return;

            foreach (var target in BasePlayer.activePlayerList)
                SendNotify(target, type, message);
        }

        #endregion

        #region Component

        private class NotifyComponent : FacepunchBehaviour
        {
            #region Fields

            private BasePlayer _player;

            private readonly List<NotifyData> _notifies = new List<NotifyData>();

            #endregion

            #region Main

            private void Awake()
            {
                _player = GetComponent<BasePlayer>();

                _instance._notifications[_player] = this;

                Invoke(NotificationsController, 1);
            }

            private void OnDestroy()
            {
                CancelInvoke();

                CuiHelper.DestroyUi(_player, Layer);

                _instance?._notifications.Remove(_player);
            }

            public void Kill()
            {
                DestroyImmediate(this);
            }

            #endregion

            #region Utils

            private void MainUi()
            {
                var ySwitch = _config.ConstYSwitch;

                var container = new CuiElementContainer();

                container.Add(new CuiPanel
                {
                    RectTransform = {AnchorMin = "1 1", AnchorMax = "1 1"},
                    Image = {Color = "0 0 0 0"}
                }, _config.DisplayType, Layer);

                _notifies.ForEach(notify =>
                {
                    NotifyUi(ref container, notify, ySwitch);

                    ySwitch = ySwitch - _config.Height - _config.YMargin;
                });

                CuiHelper.DestroyUi(_player, Layer);
                CuiHelper.AddUi(_player, container);
            }

            private void NotifyUi(ref CuiElementContainer container, NotifyData data, float ySwitch)
            {
                _config.Types[data.Type]?.Get(_player, ref container, data, ySwitch);
            }

            public void AddNotify(NotifyData data)
            {
                if (!_config.Types.ContainsKey(data.Type)) return;

                _notifies.Add(data);

                if (_notifies.Count == 1)
                    _notifies[0].StartTime = Time.time;

                MainUi();

                if (!string.IsNullOrEmpty(_config.Types[data.Type].Effect))
                    SendEffect(_config.Types[data.Type].Effect);
            }

            private void RemoveNotify(int index = 0)
            {
                _notifies.RemoveAt(index);

                if (_notifies.Count == 0)
                {
                    Kill();
                    return;
                }

                _notifies[0].StartTime = Time.time;

                MainUi();
            }

            private void NotificationsController()
            {
                CancelInvoke(NotificationsController);
                if (_notifies.Count == 0)
                {
                    Kill();
                    return;
                }

                if (Time.time - _notifies[0].StartTime >= _config.Cooldown) RemoveNotify();

                Invoke(NotificationsController, 1);
            }

            private void SendEffect(string effect)
            {
                EffectNetwork.Send(new Effect(effect, _player, 0, new Vector3(), new Vector3()), _player.Connection);
            }

            #endregion
        }

        #endregion

        #region Utils

        private void LoadImages()
        {
            if (!ImageLibrary.IsLoaded)
            {
                PrintError("IMAGE LIBRARY IS NOT INSTALLED!");
            }
            else
            {
                var imagesList = new Dictionary<string, string>();

                foreach (var conf in _config.Types.Values.Where(conf => conf.Image != null && conf.Image.Enabled
                             && !string.IsNullOrEmpty(conf.Image.Image)
                             && !imagesList.ContainsKey(conf.Image.Image)))
                    imagesList.Add(conf.Image.Image, conf.Image.Image);

                ImageLibrary?.Call("ImportImageList", Title, imagesList, 0UL, true);
            }
        }

        private void RegisterPermissions()
        {
            permission.RegisterPermission(PermSeeNotify, this);
            permission.RegisterPermission(PermNotify, this);
            permission.RegisterPermission(PermPlayerNotify, this);
            permission.RegisterPermission(PermAllPlayersNotify, this);
        }

        #endregion

        #region API

        private void SendNotify(string userId, int type, string message)
        {
            SendNotify(BasePlayer.FindByID(ulong.Parse(userId)), type, message);
        }

        private void SendNotify(ulong userId, int type, string message)
        {
            SendNotify(BasePlayer.FindByID(userId), type, message);
        }

        private void SendNotify(BasePlayer player, int type, string message)
        {
            if (player == null) return;

            if (!permission.UserHasPermission(player.UserIDString, PermSeeNotify))
            {
                player.ChatMessage(message);
                return;
            }

            var notify = GetComponent(player);
            if (notify == null) return;

            var data = new NotifyData
            {
                Type = type,
                Message = message
            };

            notify.AddNotify(data);
        }

        private NotifyComponent GetComponent(BasePlayer player)
        {
            NotifyComponent component;
            return _notifications.TryGetValue(player, out component)
                ? component
                : player.gameObject.AddComponent<NotifyComponent>();
        }

        #endregion

        #region Lang

        private const string
            NotFound = "NotFound",
            SyntaxAllPlayerNotify = "SyntaxAllPlayerNotify",
            SyntaxPlayerNotify = "SyntaxPlayerNotify",
            SyntaxNotify = "SyntaxNotify";

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Notification"] = "Notification",
                ["Error"] = "Error",
                [SyntaxNotify] = "Error syntax! Use: /{0} [type] [message]",
                [SyntaxPlayerNotify] = "Error syntax! Use: /{0} [steamid] [type] [message]",
                [SyntaxAllPlayerNotify] = "Error syntax! Use: /{0} [type] [message]",
                [NotFound] = "Player '{args[0]}' not found!"
            }, this);
        }

        private string Msg(BasePlayer player, string key, params object[] obj)
        {
            return string.Format(lang.GetMessage(key, this, player.UserIDString), obj);
        }

        private string Msg(string player, string key, params object[] obj)
        {
            return string.Format(lang.GetMessage(key, this, player), obj);
        }

        #endregion
    }
}