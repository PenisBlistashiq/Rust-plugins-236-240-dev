using System;
using UnityEngine;
using System.Globalization;
using Oxide.Game.Rust.Cui;
using Oxide.Core;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("BaseProtector", "https://vk.com/nastroykarust", "1.0.2")]
    public class BaseProtector : RustPlugin
    {
        #region Oxide Hooks

        private void OnServerInitialized()
        {
			LoadConfig();
			BasePlayer.activePlayerList.ForEach(OnPlayerInit);

            if (_config.BaseProtect)
            {
                timer.Every(60, () =>
                {
                    foreach (var players in BasePlayer.activePlayerList)
                    {
                        if (DateTime.Now.Hour - 1 > _config.FirstTime || DateTime.Now.Hour - 1 < _config.SecoundTime)
                        {
                            DrawGUI(players);
                        }
                    }
                });
            }
        }
		
        void OnPlayerInit(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() =>
                {
                    OnPlayerInit(player);
                });
                return;
            }

            if (_config.BaseProtect)
            {
                if (DateTime.Now.Hour - 1 > _config.FirstTime || DateTime.Now.Hour - 1 < _config.SecoundTime)
                {
                    DrawGUI(player);
                }
            }
		}

        private void OnEntityTakeDamage(BaseEntity entity, HitInfo hit)
        {
            if (entity.OwnerID == 0 || hit.InitiatorPlayer == null) return;

            BasePlayer player = BasePlayer.FindByID(entity.OwnerID);
            if (player == null) return;

            if (_config.BaseProtect)
            {
                if (DateTime.Now.Hour - 1 > _config.FirstTime || DateTime.Now.Hour - 1 < _config.SecoundTime)
                {
                    hit.damageTypes.ScaleAll(0.50f);
                }
            }
        }
		
        private void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "BaseProtector_UI");  
            }  
        }

        #endregion
		
		#region UI
		
        void DrawGUI(BasePlayer player)
        {
			string Layer = "BaseProtector_UI";
            CuiHelper.DestroyUi(player, Layer);
            
            var container = new CuiElementContainer();
            var Panel = container.Add(new CuiPanel
            {
                Image = { Color = $"0 0 0 0" },
                RectTransform = { AnchorMin = "0.0320313 0.936111", AnchorMax = "0.2617188 1.006944" },
                CursorEnabled = false,
            }, "Hud", Layer);
			
            container.Add(new CuiElement
            { 
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent { Color = "1 1 1 0.3", Sprite = "assets/icons/weapon.png" },
                    new CuiRectTransformComponent { AnchorMin = "1.19071002 0.2761437", AnchorMax = "1.2689413 0.7271238" }
                }
            });
			
            container.Add(new CuiElement
            { 
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent { Color = "0 0 0 0", Sprite = "assets/icons/market.png" },
                    new CuiRectTransformComponent { AnchorMin = "0.1427508 0.2565358", AnchorMax = "0.2515944 0.7467315" }
                }
            });
			
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components = 
				{
                    new CuiTextComponent() { Color = "1 1 1 0.3", Text = "УРОН ПОНИЖЕН НА 50%", FontSize = 16, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0.5976485 0.3529414", AnchorMax = "1.2183674 0.8235297" },
                }
            });
			
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components = 
				{
                    new CuiTextComponent() { Color = "1 1 1 0.3", Text = "С 23.00 до 11.00 по МСК.", Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0.6760275 0.07843163", AnchorMax = "1.2557822 0.5490193" },
                }
            });
			
			CuiHelper.AddUi(player, container);
		}
		
		#endregion
		
        #region Utils
        
        private static string HexToRustFormat(string hex)
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

            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        #endregion
		
        #region Configuration
        
        private static Configuration _config = new Configuration();

        public class Configuration
        {
            [JsonProperty(PropertyName = "Включить ли защиту?")]
            public bool BaseProtect = true;

            [JsonProperty(PropertyName = "Первое значение (например: защита с 22)")]
            public int FirstTime = 23;
			
            [JsonProperty(PropertyName = "Первое значение (например: защита до 10)")]
            public int SecoundTime = 11;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new Exception();
            }
            catch
            {
                Config.WriteObject(_config, false, $"{Interface.Oxide.ConfigDirectory}/{Name}.jsonError");
                PrintError("The configuration file contains an error and has been replaced with a default config.\n" +
                           "The error configuration file was saved in the .jsonError extension");
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig() => _config = new Configuration();

        protected override void SaveConfig() => Config.WriteObject(_config);
        
        #endregion
    }
}