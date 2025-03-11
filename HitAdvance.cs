using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using VLB;
using WebSocketSharp;

namespace Oxide.Plugins
{
    [Info("HitAdvance", "Хуюган", "2.0.4")]
    [Description("Скачано на Oxide-Russia.ru")]
    public class HitAdvance : RustPlugin
    {
        #region eNums

        private enum HitType
        {
            None,
            Line,
            Text,
            LineAndText,
            Icon
        }

        #endregion
        
        #region Classes

        private class PlayerMarker : MonoBehaviour
        {
            public BasePlayer Player;
            
            public void Awake()
            {
                Player = GetComponent<BasePlayer>();
            }

            public void ShowHit(BaseEntity target, HitInfo info)
            {
                CuiElementContainer container = new CuiElementContainer();
                var obj = target.GetComponent<BaseCombatEntity>();
                if (obj == null) return;
                
                switch (GetSettings().CurrentType)
                {
                    case HitType.Line:
                    {
                        float curHealth = obj.health;
                        float maxHealth = obj._maxHealth;
                        if (target is BuildingBlock)
                        {
                            var curGrade = (target as BuildingBlock).currentGrade;
                            if (curGrade == null) return;

                            maxHealth = curGrade.maxHealth;
                        }

                        var color = GetGradientColor((int) curHealth, (int) maxHealth);
                        float decreaseLength = (180.5f + 199.5f) / 2f * (curHealth / maxHealth); 
                        
                        container.Add(new CuiPanel
                        {
                            FadeOut = 0.5f,
                            RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = $"{-10 - decreaseLength} 80", OffsetMax = $"{-9 + decreaseLength} 85" },
                            Image         = { Color     = color }
                        }, "Hud", Layer); 
                        
                        DestroyHit();
                        CuiHelper.AddUi(Player, container);
                        
                        
                        if (IsInvoking(nameof(DestroyHit))) CancelInvoke(nameof(DestroyHit));
                        Invoke(nameof(DestroyHit), Settings.DestroyTime); 
                        break;
                    }
                    case HitType.Text:
                    {
                        var pos = GetRandomTextPosition();
                        float curHealth = obj.health;
                        float maxHealth = obj.MaxHealth();
                        string textDamage = info.damageTypes.Total().ToString("F0");
                        
                        if (Mathf.FloorToInt(info.damageTypes.Total()) == 0)
                            return;

                        float division = 1 - curHealth / maxHealth;
                        if (target is BasePlayer)
                        {
                            var targetPlayer = target as BasePlayer;
                            if (info.isHeadshot)
                                textDamage = $"<color=#DC143C>{textDamage}</color>";
                            if (targetPlayer.IsWounded())
                            {
                                textDamage = "<color=#DC143C>УПАЛ</color>";
                                if (info.isHeadshot)
                                    textDamage += " <color=#DC143C>ГОЛОВА</color>";
                            }
        
                            if (Player.currentTeam == targetPlayer.currentTeam && Player.currentTeam != 0)
                            {
                                textDamage = "<color=#32915a>ДРУГ</color>";
                                division = 1;
                            }
                        }


                        var hitId = CuiHelper.GetGuid();
                        container.Add(new CuiElement()
                        {
                            Name = hitId,
                            Parent = "Hud",
                            FadeOut = 0.5f,
                            Components =
                            {
                                new CuiTextComponent { Text = $"<b>{textDamage}</b>", Color = HexToRustFormat("#FFFFFFFF"), Font = "robotocondensed-bold.ttf", FontSize = (int) Mathf.Lerp(15, 30, division), Align = TextAnchor.MiddleCenter, },
                                new CuiOutlineComponent() {Color = "0 0 0 1", Distance = "0.155004182 0.15505041812"},
                                new CuiRectTransformComponent() { AnchorMin = $"{pos.x} {pos.y}", AnchorMax = $"{pos.x} {pos.y}", OffsetMin = "-100 -100", OffsetMax = "100 100" }
        
                            }
                        });
        
                        CuiHelper.AddUi(Player, container);
                        StartCoroutine(DestroyHit(hitId));
                        break;
                    }
                    case HitType.Icon:
                    {
                        var hitId = CuiHelper.GetGuid();

                        string color = "1 1 1 0.5";
                        string image = "assets/icons/close.png";
                        float margin = 10;
                        
                        if (target is BasePlayer)
                        {
                            var targetPlayer = target as BasePlayer;
                            if (targetPlayer.IsWounded())
                            {
                                margin = 20;
                                image = "assets/icons/fall.png";
                            }
                            if (targetPlayer.IsWounded() || targetPlayer.IsDead())
                                color = "1 0.2041845 0.204181 0.5";
                        }

                        if (info.isHeadshot) color = "1 0.2 0.2 0.5";

                        container.Add(new CuiButton
                        {
                            FadeOut = 0.3f,
                            RectTransform = {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-{margin} -{margin}", OffsetMax = $"{margin} {margin}"},
                            Button        = {Color     = color, Sprite = image },
                            Text          = {Text      = ""}
                        }, "Hud", hitId);
        
                        CuiHelper.AddUi(Player, container);
                        CuiHelper.DestroyUi(Player, hitId);
                        break;
                    }
                    case HitType.LineAndText:
                    {
                        GetSettings().CurrentType = HitType.Line;
                        ShowHit(target, info);
                        GetSettings().CurrentType = HitType.Text;
                        ShowHit(target, info);
                        GetSettings().CurrentType = HitType.LineAndText;
                        break;
                    }
                }

                
            }

            public const string Layer = "UI_HitAdvance_Layer";
            public void DestroyHit() => CuiHelper.DestroyUi(Player, Layer);
            public IEnumerator DestroyHit(string ID, float delay = 0.5f)
            {
                yield return new WaitForSeconds(delay);
                
                CuiHelper.DestroyUi(Player, ID);
            }
            public PlayerSettings GetSettings() => PlayerSettingses[Player.userID];
        }

        private class PlayerSettings
        {
            public HitType CurrentType;
            public bool BuildingHit;
            
            public PlayerSettings() {}
            public static PlayerSettings Generate()
            {
                return new PlayerSettings
                {
                    CurrentType = Settings.HitSettings.FirstOrDefault(p => p.Value.IsDefault).Key,
                    BuildingHit = Settings.DefaultBuildingDamage
                };
            }
        }

        private class HitSetting
        {
            [JsonProperty("Название маркера")]
            public string DisplayName;
            [JsonProperty("Разрешение, с которым его можно выбрать")]
            public string Permission;

            [JsonProperty("Включён у игроков изначально")]
            public bool IsDefault;
        }
        
        private class Configuration
        {
            [JsonProperty("Настройки различных маркеров")]
            public Hash<HitType, HitSetting> HitSettings = new Hash<HitType, HitSetting>();

            [JsonProperty("Включить отображение урона по постройкам изначально")]
            public bool DefaultBuildingDamage = true; 
            [JsonProperty("Показывать урон по НПС")]
            public bool ShowNPCDamage = true;
            [JsonProperty("Показывать урон по животным")]
            public bool ShowAnimalDamage = false;
            [JsonProperty("Время удаления маркера (если отсутвует другой урон)")]
            public float DestroyTime = 0.25f;
            [JsonProperty("Укажите название команды для изменения маркера")]
            public string CommandName = "marker";
 
            public static Configuration LoadDefault()
            {
                return new Configuration
                {
                    HitSettings = new Hash<HitType, HitSetting>
                    {
                        [HitType.None] = new HitSetting
                        {
                            DisplayName = "<b><size=20>Маркер полностью отключён</size></b>\n" +
                                    "Вы не будете понимать, когда попадаете \nпо врагу!",
                            Permission = string.Empty,
                            
                            IsDefault = false
                        },
                        [HitType.Line] = new HitSetting
                        {
                            DisplayName = "<b><size=20>Полоса со здоровьем</size></b>\n" +
                                    "Над слотами появляется полоса, она" +
                                    "\nотображает <b>оставшееся</b> здоровье у врага",
                            Permission = "HitAdvance.Line",
                            
                            IsDefault = false
                        },
                        [HitType.Text] = new HitSetting
                        {
                            DisplayName = "<b><size=20>Текст с уроном</size></b>\nПо центру экрана будут всплывать \nцифры с <b>нанесенным</b> уроном!<size=%id%></size>",
                            Permission  = "HitAdvance.Text",
                            
                            IsDefault = false
                        },
                        [HitType.LineAndText] = new HitSetting
                        {
                            DisplayName = "<b><size=20>Текст и полоса</size></b>\nДва предыдущих маркера срабатывают <b>одновременно</b>!",
                            Permission  = "HitAdvance.TextAndLine",
                            
                            IsDefault = true
                        },
                        [HitType.Icon] = new HitSetting
                        {
                            DisplayName = "<b><size=20>Иконка попадания</size></b>\nПривычная всем иконка попадения, меняет \nцвет при выстреле <b>в голову</b>!",
                            Permission  = "HitAdvance.Icon",
                            
                            IsDefault = false
                        }
                    }
                };
            }
        }

        #endregion
        
        #region Variables

        private static Configuration Settings = Configuration.LoadDefault();
        private static Hash<ulong, PlayerSettings> PlayerSettingses = new Hash<ulong, PlayerSettings>();

        #endregion

        #region Initialization
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                Settings = Config.ReadObject<Configuration>();
            }
            catch
            {
                PrintWarning($"Error reading config, creating one new config!");
                LoadDefaultConfig();
            }
            
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => Settings = Configuration.LoadDefault();
        protected override void SaveConfig() => Config.WriteObject(Settings);
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       
        private void OnServerInitialized()               
        {                         
            if (Interface.Oxide.DataFileSystem.ExistsDatafile(Name))
                PlayerSettingses = Interface.Oxide.DataFileSystem.ReadObject<Hash<ulong, PlayerSettings>>(Name);
            
            foreach (var check in Settings.HitSettings.Where(p => !p.Value.Permission.IsNullOrEmpty()))
                permission.RegisterPermission(check.Value.Permission, this);
            
            cmd.AddChatCommand(Settings.CommandName, this, nameof(ChatCommandMarker)); 
            BasePlayer.activePlayerList.ToList().ForEach(OnPlayerConnected);
            timer.Every(60, SaveData);
        }

        private void Unload() => SaveData();
        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, PlayerSettingses);

        private void OnPlayerConnected(BasePlayer player)
        {
            player.GetOrAddComponent<PlayerMarker>(); 
            
            if (!PlayerSettingses.ContainsKey(player.userID))
                PlayerSettingses.Add(player.userID, PlayerSettings.Generate());
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            var obj = player.GetComponent<PlayerMarker>();
            if (obj != null) UnityEngine.Object.Destroy(obj);
        }

        #endregion

        #region Hooks

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            var initiator = info?.InitiatorPlayer?.GetComponent<PlayerMarker>();
            if (initiator == null || info.damageTypes.Total() < 1) return;
            int Max = 2742;

            if (entity is BuildingBlock && initiator.GetSettings().BuildingHit)
            {
                NextTick(() =>
                {
                    if (entity != null && !entity.IsDestroyed)
                        initiator.ShowHit(entity, info);  
                });
            }
        }
        
        private void OnPlayerAttack(BasePlayer attacker, HitInfo info)
        {
            var target = info.HitEntity;
            if (attacker.IsNpc || target == null) return;

            if (target is BaseAnimalNPC && !Settings.ShowAnimalDamage || target is BuildingBlock) return;
            if (target is HumanNPC && !Settings.ShowNPCDamage) return;
            
            NextTick(() =>
            {
                if (target != null && !target.IsDestroyed)
                    attacker.GetComponent<PlayerMarker>().ShowHit(target, info);
            });
        }

        #endregion

        #region Commands

        private void ChatCommandMarker(BasePlayer player, string command, string[] args) => UI_DrawInterface(player);

        [ConsoleCommand("UI_HitAdvance")]
        private void consoleCmdChange(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player == null || !args.HasArgs(1)) return;

            switch (args.Args[0].ToLower())
            {
                case "choose":
                {
                    if (!args.HasArgs(2)) return;

                    HitType type = HitType.None;
                    if (!Enum.TryParse(args.Args[1], out type)) return;

                    if (!Settings.HitSettings[type].Permission.IsNullOrEmpty() && !permission.UserHasPermission(player.UserIDString, Settings.HitSettings[type].Permission)) return;

                    player.GetComponent<PlayerMarker>().GetSettings().CurrentType = type;
                    UI_DrawInterface(player);
                    break;
                }
                case "toggle":
                {
                    var set = player.GetComponent<PlayerMarker>()?.GetSettings();
                    if (set == null) return;
                    
                    set.BuildingHit = !set.BuildingHit;
                    UI_DrawInterface(player);
                    break;
                }
            }
        }

        #endregion

        #region GUI

        private const string Layer = "UI_HitAdvance_Settings";
        private void UI_DrawInterface(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.7" }
            }, "Overlay", Layer); 

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Close = Layer, Color = "0 0 0 0" },
                Text = { Text = "" }
            }, Layer);

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 0.75", AnchorMax                                                                   = "1 0.85", OffsetMax              = "0 0"},
                Text          = {Text      = "Вы можете настроить вид хит-маркера под себя, либо полностью отключить его!", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", Color = "1 1 1 0.5", FontSize = 28}
            }, Layer);

            var obj = player.GetComponent<PlayerMarker>();
            if (obj == null) return;
            
            for (int i = 0; i < 6; i += 3)
            {
                float leftPosition = 0 - Settings.HitSettings.Skip(i).Take(3).Count() / 2f * 300 - (Settings.HitSettings.Take(3).Count() - 1) / 2f * 20;

                foreach (var check in Settings.HitSettings.Skip(i).Take(3))
                {
                    string text = "ВЫБРАТЬ МАРКЕР";
                    string color = "1 1 1 0.12";
                    if (obj.GetSettings().CurrentType == check.Key)
                    {
                        text = "МАРКЕР ВЫБРАН";
                        color = "0.70418 1 0.7 0.12";  
                    }
                    else if (!check.Value.Permission.IsNullOrEmpty() && !permission.UserHasPermission(player.UserIDString, check.Value.Permission))
                    {
                        color = "1 0.7 0.7 0.12";
                        text = "НЕДОСТУПЕН ДЛЯ ВЫБОРА";
                    }
                    
                    container.Add(new CuiPanel
                    {
                        RectTransform = {AnchorMin = $"0.5 {0.62 - i * 0.08}", AnchorMax = $"0.5 {0.62 - i * 0.08}", OffsetMin = $"{leftPosition} -50", OffsetMax = $"{leftPosition + 300} 50"},
                        Image         = {Color     = "1 1 1 0.03"}
                    }, Layer, Layer + check.Key);

                    container.Add(new CuiLabel
                    {
                        RectTransform = {AnchorMin = "0 0", OffsetMax              = "1 1", OffsetMin                        = "0 0"},
                        Text          = {Text      = check.Value.DisplayName, Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1"}
                    }, Layer + check.Key);

                    container.Add(new CuiButton
                    {
                        RectTransform = {AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 -50", OffsetMax = "0 -10"},
                        Button        = {Color     = color, Command = $"UI_HitAdvance choose {(int) check.Key}" },
                        Text          = {Text      = text, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 22, Color = "1 1 1 0.4"}
                    }, Layer + check.Key);

                    leftPosition += 320;
                }
            }
                
            string toggleText  = "УРОН ПО ПОСТРОЙКАМ ВЫКЛЮЧЕН";
            string toggleColor = "1 0.7 0.7 0.17";
            if (obj.GetSettings().BuildingHit)
            {
                toggleText  = "УРОН ПО ПОСТРОЙКАМ ВКЛЮЧЕН";
                toggleColor = "0.70418 1 0.7 0.17";
            }
            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.5 0.1", AnchorMax = "0.5 0.1", OffsetMin = "-210 10", OffsetMax = "190 50"},
                Button        = {Color     = toggleColor, Command = $"UI_HitAdvance toggle" },
                Text          = {Text      = toggleText, Align    = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 22, Color = "1 1 1 0.4"}
            }, Layer);

            CuiHelper.AddUi(player, container); 
        }
        
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

        #region Utils

        public static Vector2 GetRandomTextPosition()
        {
            float x = (float) Oxide.Core.Random.Range(45, 55) / 100;
            float y = (float) Oxide.Core.Random.Range(40, 60) / 100;
            
            return new Vector2(x, y);
        }
        
        public static string GetGradientColor(int count, int max)
        {
            if (count > max)
                count = max;
            float n = max > 0 ? (float)ColorsGradientDB.Length / max : 0;
            var index = (int) (count * n);
            if (index > 0) index--;
            return ColorsGradientDB[ index ];
        }
        
        private static string[] ColorsGradientDB = new string[100]
        {
            "0.2000 0.8000 0.2000 1.0000",
            "0.2471 0.7922 0.1961 1.0000",
            "0.2824 0.7843 0.1922 1.0000",
            "0.3176 0.7725 0.1843 1.0000",
            "0.3451 0.7647 0.1804 1.0000",
            "0.3686 0.7569 0.1765 1.0000",
            "0.3922 0.7490 0.1725 1.0000",
            "0.4118 0.7412 0.1686 1.0000",
            "0.4314 0.7333 0.1647 1.0000",
            "0.4471 0.7216 0.1608 1.0000",
            "0.4667 0.7137 0.1569 1.0000",
            "0.4784 0.7059 0.1529 1.0000",
            "0.4941 0.6980 0.1490 1.0000",
            "0.5098 0.6902 0.1412 1.0000",
            "0.5216 0.6824 0.1373 1.0000",
            "0.5333 0.6706 0.1333 1.0000",
            "0.5451 0.6627 0.1294 1.0000",
            "0.5569 0.6549 0.1255 1.0000",
            "0.5647 0.6471 0.1216 1.0000",
            "0.5765 0.6392 0.1176 1.0000",
            "0.5843 0.6314 0.1137 1.0000",
            "0.5922 0.6235 0.1137 1.0000",
            "0.6039 0.6118 0.1098 1.0000",
            "0.6118 0.6039 0.1059 1.0000",
            "0.6196 0.5961 0.1020 1.0000",
            "0.6275 0.5882 0.0980 1.0000",
            "0.6314 0.5804 0.0941 1.0000",
            "0.6392 0.5725 0.0902 1.0000",
            "0.6471 0.5647 0.0863 1.0000",
            "0.6510 0.5569 0.0824 1.0000",
            "0.6588 0.5451 0.0784 1.0000",
            "0.6627 0.5373 0.0784 1.0000",
            "0.6667 0.5294 0.0745 1.0000",
            "0.6745 0.5216 0.0706 1.0000",
            "0.6784 0.5137 0.0667 1.0000",
            "0.6824 0.5059 0.0627 1.0000",
            "0.6863 0.4980 0.0588 1.0000",
            "0.6902 0.4902 0.0588 1.0000",
            "0.6941 0.4824 0.0549 1.0000",
            "0.6980 0.4745 0.0510 1.0000",
            "0.7020 0.4667 0.0471 1.0000",
            "0.7020 0.4588 0.0471 1.0000",
            "0.7059 0.4471 0.0431 1.0000",
            "0.7098 0.4392 0.0392 1.0000",
            "0.7098 0.4314 0.0392 1.0000",
            "0.7137 0.4235 0.0353 1.0000",
            "0.7176 0.4157 0.0314 1.0000",
            "0.7176 0.4078 0.0314 1.0000",
            "0.7216 0.4000 0.0275 1.0000",
            "0.7216 0.3922 0.0275 1.0000",
            "0.7216 0.3843 0.0235 1.0000",
            "0.7255 0.3765 0.0235 1.0000",
            "0.7255 0.3686 0.0196 1.0000",
            "0.7255 0.3608 0.0196 1.0000",
            "0.7255 0.3529 0.0196 1.0000",
            "0.7294 0.3451 0.0157 1.0000", 
            "0.7294 0.3373 0.0157 1.0000",
            "0.7294 0.3294 0.0157 1.0000",
            "0.7294 0.3216 0.0118 1.0000",
            "0.7294 0.3137 0.0118 1.0000",
            "0.7294 0.3059 0.0118 1.0000",
            "0.7294 0.2980 0.0118 1.0000",
            "0.7294 0.2902 0.0078 1.0000",
            "0.7255 0.2824 0.0078 1.0000",
            "0.7255 0.2745 0.0078 1.0000",
            "0.7255 0.2667 0.0078 1.0000",
            "0.7255 0.2588 0.0078 1.0000",
            "0.7255 0.2510 0.0078 1.0000",
            "0.7216 0.2431 0.0078 1.0000",
            "0.7216 0.2353 0.0039 1.0000",
            "0.7176 0.2275 0.0039 1.0000",
            "0.7176 0.2196 0.0039 1.0000",
            "0.7176 0.2118 0.0039 1.0000",
            "0.7137 0.2039 0.0039 1.0000",
            "0.7137 0.1961 0.0039 1.0000",
            "0.7098 0.1882 0.0039 1.0000",
            "0.7098 0.1804 0.0039 1.0000",
            "0.7059 0.1725 0.0039 1.0000",
            "0.7020 0.1647 0.0039 1.0000",
            "0.7020 0.1569 0.0039 1.0000",
            "0.6980 0.1490 0.0039 1.0000",
            "0.6941 0.1412 0.0039 1.0000",
            "0.6941 0.1333 0.0039 1.0000",
            "0.6902 0.1255 0.0039 1.0000",
            "0.6863 0.1176 0.0039 1.0000",
            "0.6824 0.1098 0.0039 1.0000",
            "0.6784 0.1020 0.0039 1.0000",
            "0.6784 0.0941 0.0039 1.0000",
            "0.6745 0.0863 0.0039 1.0000",
            "0.6706 0.0784 0.0039 1.0000",
            "0.6667 0.0706 0.0039 1.0000",
            "0.6627 0.0627 0.0039 1.0000",
            "0.6588 0.0549 0.0039 1.0000",
            "0.6549 0.0431 0.0039 1.0000",
            "0.6510 0.0353 0.0000 1.0000",
            "0.6471 0.0275 0.0000 1.0000",
            "0.6392 0.0196 0.0000 1.0000",
            "0.6353 0.0118 0.0000 1.0000",
            "0.6314 0.0039 0.0000 1.0000",
            "0.6275 0.0000 0.0000 1.0000",
        };

        #endregion
    }
}