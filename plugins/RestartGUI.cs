using System;
using System.Globalization;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Console = ConVar.Console;
using Time = Oxide.Core.Libraries.Time;

namespace Oxide.Plugins
{
    [Info("RestartGUI", "Foxyd | topplugin.ru", "1.0.0")]
    public class RestartGUI : RustPlugin
    {
        private const string Layer = "RestartLayer";

        private static class RestartConfig
        {
            public static int Time = 0;

            public static bool Restart = false;

            public static bool First = true;
        }

        #region Config

        private Configuration config = new Configuration();

        private class Configuration
        {
            [JsonProperty("Время рестарта")]
            public string Time = "4:55";

            [JsonProperty("Время до рестарта")]
            public int RestartTime = 300;

            [JsonProperty("Сообщение о рестарте")]
            public string RestartTimerText = "Сервер перезагрузится через {0} секунд";

            [JsonProperty("Звук")]
            public string Effect = "assets/bundled/prefabs/fx/notice/item.select.fx.prefab";

        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) throw new Exception();
            }
            catch
            {
                Config.WriteObject(config, false, $"{Interface.Oxide.ConfigDirectory}/{Name}.jsonError");
                PrintError("Ошибка в конфигурации плагина RestartGUI");

                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig() => config = new Configuration();

        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Hook

        private void OnServerInitialized()
        {
            LoadConfig();
        }

        private void OnTick()
        {
            if (DateTime.Now.ToString("t") != config.Time || RestartConfig.Restart) return;

            RestartConfig.Restart = true;
            ExecuteRestart(config.RestartTime);
        }

        #endregion

        #region Command

        [ConsoleCommand("restart")]
        private void ConsoleReload(ConsoleSystem.Arg arg)
        {
            if (!arg.IsServerside) return;

            if (arg.Connection != null)
            {
                var player = arg.Connection.player as global::BasePlayer;
                if (player != null)
                    return;
            }

            var args = arg.Args ?? new string[0];

            switch (args.Length)
            {
                case 0:
                    {
                        if (RestartConfig.Restart)
                        {
                            ResetConfig();
                            return;
                        }

                        RestartConfig.Restart = true;
                        ExecuteRestart(config.RestartTime);
                        return;
                    }
                case 1:
                    {
                        if (RestartConfig.Restart || args[0].ToLower() == "stop")
                        {
                            ResetConfig();
                            return;
                        }

                        int time;

                        if (!int.TryParse(args[0], out time)) return;

                        ResetConfig();
                        RestartConfig.Restart = true;
                        ExecuteRestart(time);
                        break;
                    }
            }
        }

        [ChatCommand("restart")]
        private void CMDReload(BasePlayer player, string cmd, string[] args)
        {
            if (!player.IsAdmin) return;

            switch (args.Length)
            {
                case 0:
                    {
                        if (RestartConfig.Restart)
                        {
                            ResetConfig();
                            return;
                        }

                        RestartConfig.Restart = true;
                        ExecuteRestart(config.RestartTime);
                        return;
                    }
                case 1:
                    {
                        if (RestartConfig.Restart || args[0].ToLower() == "stop")
                        {
                            ResetConfig();
                            return;
                        }

                        int time;

                        if (!int.TryParse(args[0], out time)) return;

                        ResetConfig();
                        RestartConfig.Restart = true;
                        ExecuteRestart(time);
                        break;
                    }
            }
        }

        #endregion

        #region Method

        private void ExecuteRestart(int time)
        {
            if (!RestartConfig.Restart)
            {
                DestroyGUI();
                ResetConfig();
                return;
            }

            if (RestartConfig.First)
            {
                RestartConfig.Time = time;
                RestartConfig.First = false;
            }

            if (RestartConfig.Time == 0)
            {
                ConVar.Global.quit(null);
                return;
            }

            CuiElementContainer container = new CuiElementContainer
            {
                {
                    new CuiPanel
                    {
                        Image = { Color = HexToRustFormat("#00000070") },
                        RectTransform = { AnchorMin = "0 0.7694442", AnchorMax = "1 0.8249997" }
                    }, "Hud", Layer
                },
                new CuiElement
                {
                    Parent = Layer,
                    Components =
                    {
                        new CuiTextComponent{ Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", FontSize = 20, Text = string.Format(config.RestartTimerText, $"{RestartConfig.Time}")},
                        new CuiOutlineComponent { Color = "0 0 0 1", Distance = "0.7 0.7" },
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1" }
                    }
                }
            };

            DestroyGUI();
            DrawGUI(container);

            foreach (var player in BasePlayer.activePlayerList)
                EffectNetwork.Send(new Effect(config.Effect, player, 0, Vector3.zero, Vector3.forward), player.net.connection);

            RestartConfig.Time--;

            timer.Once(1f, () => { ExecuteRestart(RestartConfig.Time); });
        }

        private void ResetConfig()
        {
            RestartConfig.Time = 0;
            RestartConfig.Restart = false;
            RestartConfig.First = true;
        }

        private void DrawGUI(CuiElementContainer container)
        {
            foreach (var x in BasePlayer.activePlayerList)
                CuiHelper.AddUi(x, container);
        }
        private void DestroyGUI()
        {
            foreach (var x in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(x, Layer);
        }
        #endregion

        #region HEX

        private string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }

            var str = hex.Trim('#');

            if (str.Length == 6)
                str += "FF";

            if (str.Length != 8)
            {
                throw new Exception(hex);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);

            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }

        #endregion
    }
}