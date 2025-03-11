using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("Teleportation", "OxideBro", "1.4.2")]
    class Teleportation : RustPlugin
    {
        [PluginReference] Plugin Clans;
        [PluginReference] Plugin Friends;
        Dictionary<ulong, Vector3> lastPositions = new Dictionary<ulong, Vector3>();
        Dictionary<BasePlayer, int> spectatingPlayers = new Dictionary<BasePlayer, int>();

        private const string Layer = "TeleportMenu";

        private class ButtonEntry
        {
            public string Name;
            public string Sprite;
            public string Command;
            public string Color;
            public bool Close;
        }

        bool IsClanMember(ulong playerid = 1, ulong targetID = 0) => (bool)(Clans?.Call("HasFriend", playerid, targetID) ?? false);
        bool IsFriends(ulong playerID = 0, ulong friendId = 0)
        {
            return (bool)(Friends?.Call("AreFriends", playerID, friendId) ?? false);
        }

        bool IsTeamate(BasePlayer player, ulong targetID)
        {
            if (player.currentTeam == 0) return false;
            var team = RelationshipManager._instance.FindTeam(player.currentTeam);
            if (team == null) return false;

            var list = RelationshipManager._instance.FindTeam(player.currentTeam).members.Where(p => p == targetID).ToList();
            return list.Count > 0;
        }

        class TP
        {
            public BasePlayer Player;
            public BasePlayer Player2;
            public Vector3 pos;
            public bool EnabledShip;
            public int seconds;
            public bool TPL;
            public TP(BasePlayer player, Vector3 Pos, int Seconds, bool EnabledShip1, bool tpl, BasePlayer player2 = null)
            {
                Player = player;
                pos = Pos;
                seconds = Seconds;
                EnabledShip = EnabledShip1;
                Player2 = player2;
                TPL = tpl;
            }
        }

        int homelimitDefault;
        Dictionary<string, int> homelimitPerms;
        int tpkdDefault;
        Dictionary<string, int> tpkdPerms;
        int tpkdhomeDefault;
        Dictionary<string, int> tpkdhomePerms;
        int teleportSecsDefault;
        int resetPendingTime;
        bool restrictCupboard;
        bool enabledTPR;
        bool homecupboard;
        bool adminsLogs;
        bool foundationOwner;
        bool foundationOwnerFC;

        bool restrictTPRCupboard;
        bool foundationEx;
        bool wipedData;
        bool createSleepingBug;
        string EffectPrefab1;
        string EffectPrefab;
        bool EnabledShipTP;
        bool EnabledBallonTP;
        bool CancelTPMetabolism;
        bool CancelTPCold;
        bool CancelTPRadiation;
        bool FriendsEnabled;
        bool CancelTPWounded;
        bool EnabledTPLForPlayers;
        int TPLCooldown;
        int TplPedingTime;
        bool TPLAdmin;

        static DynamicConfigFile config;
        Dictionary<string, int> teleportSecsPerms;


        private void SendNotify(BasePlayer player, string message, int type = 0)
        {
            if (Notify != null)
                Notify?.Call("SendNotify", player, type, message);
            else
                SendReply(player, message);
        }

        void OnNewSave()
        {
            if (wipedData)
            {
                PrintWarning("Обнаружен вайп. Очищаем данные с data/Teleportation");
                WipeData();
            }
        }
        void WipeData()
        {
            LoadData();
            tpsave = new List<TPList>();
            homes = new Dictionary<ulong, Dictionary<string, Vector3>>();
            SaveData();
        }
        protected override void LoadDefaultConfig()
        {
            GetVariable(Config, "Запрещать отправлять запрос на телепортацию в зоне действия чужого шкафа", out homecupboard, true);
            GetVariable(Config, "Звук уведомления при получение запроса на телепорт (пустое поле = звук отключен)", out EffectPrefab1, "assets/prefabs/locks/keypad/effects/lock.code.unlock.prefab");
            GetVariable(Config, "Звук предупреждения (пустое поле = звук отключен)", out EffectPrefab, "assets/prefabs/locks/keypad/effects/lock.code.denied.prefab");
            GetVariable(Config, "Разрешать сохранять местоположение только на фундаменте", out foundationEx, true);
            GetVariable(Config, "Создавать объект при сохранении местоположения в виде Sleeping Bag", out createSleepingBug, true);
            GetVariable(Config, "Автоматический вайп данных при генерации новой карты", out wipedData, true);
            GetVariable(Config, "Запрещать принимать запрос на телепортацию в зоне действия чужого шкафа", out restrictCupboard, true);
            GetVariable(Config, "Запрещать сохранять местоположение если игрок не является владельцем фундамента", out foundationOwner, true);
            GetVariable(Config, "Разрешать сохранять местоположение если игрок является другом или соклановцем или тимейтом владельца фундамента ", out foundationOwnerFC, true);
            GetVariable(Config, "Логировать использование команд для администраторов", out adminsLogs, true);
            GetVariable(Config, "Включить телепортацию (TPR/TPA) только к друзьям, соклановкам или тимейту", out FriendsEnabled, true);
            GetVariable(Config, "Разрешить команду TPR игрокам (false = /tpr не будет работать)", out enabledTPR, true);
            GetVariable(Config, "Разрешить отправку и приём телепорта и телепорт домой на корабле", out EnabledShipTP, true);
            GetVariable(Config, "Разрешить отправку и приём телепорта на воздушном шаре", out EnabledBallonTP, true);
            GetVariable(Config, "Запрещать отправлять запрос на телепортацию в зоне действия чужого шкафа", out restrictTPRCupboard, true);
            GetVariable(Config, "Отмета телепорта игрока (Home/TP) если у него кровотечение", out CancelTPMetabolism, true);
            GetVariable(Config, "Отмета телепорта игрока (Home/TP) если игрок ранен", out CancelTPWounded, true);
            GetVariable(Config, "Отмета телепорта игрока (Home/TP) если ему холодно", out CancelTPCold, true);
            GetVariable(Config, "Отмета телепорта игрока (Home/TP) если он облучен радиацией", out CancelTPRadiation, true);
            GetVariable(Config, "Время ответа на запрос телепортации (в секундах)", out resetPendingTime, 15);
            GetVariable(Config, "Ограничение на количество сохранённых местоположений", out homelimitDefault, 3);
            GetVariable(Config, "[TPL] Разрешить игрокам использовать TPL", out EnabledTPLForPlayers, false);
            GetVariable(Config, "[TPL] Задержка телепортации игрока на TPL", out TplPedingTime, 15);
            GetVariable(Config, "[TPL] Cooldown телепортации игрока на TPL", out TPLCooldown, 15);
            GetVariable(Config, "[TPL] Телепортировать админа без задержки и кулдауна?", out TPLAdmin, true);
            Config["Ограничение на количество сохранённых местоположений с привилегией"] = homelimitPerms = GetConfig("Ограничение на количество сохранённых местоположений с привилегией", new Dictionary<string, object>() {
                    {
                    "teleportation.vip", 5
                }
            }
            ).ToDictionary(p => p.Key, p => Convert.ToInt32(p.Value));
            PermissionService.RegisterPermissions(this, homelimitPerms.Keys.ToList());
            GetVariable(Config, "Длительность задержки перед телепортацией (в секундах)", out teleportSecsDefault, 15);
            Config["Длительность задержки перед телепортацией с привилегией (в секундах)"] = teleportSecsPerms = GetConfig("Длительность задержки перед телепортацией с привилегией (в секундах)", new Dictionary<string, object>() {
                    {
                    "teleportation.vip", 10
                }
            }
            ).ToDictionary(p => p.Key, p => Convert.ToInt32(p.Value));
            PermissionService.RegisterPermissions(this, teleportSecsPerms.Keys.ToList());
            GetVariable(Config, "Длительность перезарядки телепорта (в секундах)", out tpkdDefault, 300);
            Config["Длительность перезарядки телепорта с привилегией (в секундах)"] = tpkdPerms = GetConfig("Длительность перезарядки телепорта с привилегией (в секундах)", new Dictionary<string, object>() {
                    {
                    "teleportation.vip", 150
                }
            }
            ).ToDictionary(p => p.Key, p => Convert.ToInt32(p.Value));
            PermissionService.RegisterPermissions(this, tpkdPerms.Keys.ToList());
            GetVariable(Config, "Длительность перезарядки телепорта домой (в секундах)", out tpkdhomeDefault, 300);
            Config["Длительность перезарядки телепорта домой с привилегией (в секундах)"] = tpkdhomePerms = GetConfig("Длительность перезарядки телепорта домой с привилегией (в секундах)", new Dictionary<string, object>() {
                    {
                    "teleportation.vip", 150
                }
            }
            ).ToDictionary(p => p.Key, p => Convert.ToInt32(p.Value));
            PermissionService.RegisterPermissions(this, tpkdhomePerms.Keys.ToList());
            SaveConfig();
        }
        T GetConfig<T>(string name, T defaultValue) => Config[name] == null ? defaultValue : (T)Convert.ChangeType(Config[name], typeof(T));
        public static void GetVariable<T>(DynamicConfigFile config, string name, out T value, T defaultValue)
        {
            config[name] = value = config[name] == null ? defaultValue : (T)Convert.ChangeType(config[name], typeof(T));
        }
        public static class PermissionService
        {
            public static Permission permission = Interface.GetMod().GetLibrary<Permission>();
            public static bool HasPermission(ulong uid, string permissionName)
            {
                return !string.IsNullOrEmpty(permissionName) && permission.UserHasPermission(uid.ToString(), permissionName);
            }
            public static void RegisterPermissions(Plugin owner, List<string> permissions)
            {
                if (owner == null) throw new ArgumentNullException("owner");
                if (permissions == null) throw new ArgumentNullException("commands");
                foreach (var permissionName in permissions.Where(permissionName => !permission.PermissionExists(permissionName)))
                {
                    permission.RegisterPermission(permissionName, owner);
                }
            }
        }
        public BasePlayer FindBasePlayer(string nameOrUserId)
        {
            nameOrUserId = nameOrUserId.ToLower();
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.displayName.ToLower().Contains(nameOrUserId) || player.UserIDString == nameOrUserId) return player;
            }
            foreach (var player in BasePlayer.sleepingPlayerList)
            {
                if (player.displayName.ToLower().Contains(nameOrUserId) || player.UserIDString == nameOrUserId) return player;
            }
            return default(BasePlayer);
        }
        private readonly int groundLayer = LayerMask.GetMask("Terrain", "World");
        private readonly int buildingMask = Rust.Layers.Server.Buildings;
        Dictionary<ulong, Dictionary<string, Vector3>> homes;
        List<TPList> tpsave;
        class TPList
        {
            public string Name;
            public Vector3 pos;
        }
        Dictionary<ulong, int> cooldownsTP = new Dictionary<ulong, int>();
        Dictionary<ulong, int> cooldownsHOME = new Dictionary<ulong, int>();
        List<TP> tpQueue = new List<TP>();
        List<TP> pendings = new List<TP>();
        List<ulong> sethomeBlock = new List<ulong>();

        public List<BasePlayer> OpenTeleportMenu = new List<BasePlayer>();

        [ChatCommand("tp.menu")]
        private void cmdChatMap(BasePlayer player)
        {

            if (player == null) return;
            if (OpenTeleportMenu.Contains(player))
            {
                OpenTeleportMenu.Remove(player);
                CuiHelper.DestroyUi(player, Layer);
            }
            else
            {
                OpenTeleportMenu.Add(player);
                DDrawMenu(player);
            }
        }

        private void DDrawMenu(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);

            CuiElementContainer container = new CuiElementContainer();
            List<ButtonEntry> buttons = new List<ButtonEntry>();

            if (CheckGetHomes(player).Count < GetHomeLimit(player.userID))
            {
                var pos = GetGrid(player.transform.position, false);
                buttons.Add(new ButtonEntry
                {
                    Name = "Сохранить дом",
                    Command = $"tp.cmd sethome {pos}",
                    Sprite = "assets/icons/save.png",
                    Color = "#d3a243",
                    Close = true
                });
            }

            if (CheckGetHomes(player).Count > 0)
            {
                foreach (var check in CheckGetHomes(player))
                {
                    buttons.Add(new ButtonEntry
                    {
                        Name = check.Key,
                        Command = $"tp.cmd home {check.Key}",
                        Sprite = "assets/icons/construction.png",
                        Color = "#3889cb",
                        Close = true
                    });
                }
            }

            var friendList = GetFriends(player.userID);
            if (friendList.Count > 0)
            {
                if(AutoTPA.ContainsKey(player.userID) == false)
                {
                    AutoTPA.Add(player.userID, new AutoTPASettings()
                    {
                        Enabled = false,
                        PlayersList = new Dictionary<string, ulong>()
                    });
                }
                
                if (AutoTPA[player.userID].Enabled)
                {
                    buttons.Add(new ButtonEntry
                    {
                        Name = $"<size=11>Автопринятие</size>  <size=10><color=#2abd2a>Включено</color></size>",
                        Command = "tp.cmd atp",
                        Sprite = "assets/icons/electric.png",
                        Color = "#63666f",
                        Close = false
                    });
                }
                else
                {
                    buttons.Add(new ButtonEntry
                    {
                        Name = $"<size=11>Автопринятие</size>  <size=10><color=#bd4c2a>Отключено</color></size>",
                        Command = "tp.cmd atp",
                        Sprite = "assets/icons/electric.png",
                        Color = "#63666f",
                        Close = false
                    });
                }

                foreach (var friend in friendList)
                {
                    var covFriend = covalence.Players.FindPlayerById(friend.ToString());
                    if (covFriend == null) continue;

                    buttons.Add(new ButtonEntry
                    {
                        Name = $"{covFriend.Name}",
                        Command = $"tp.cmd tpr {covFriend.Name}",
                        Sprite = "assets/icons/friends_servers.png",
                        Color = "#8952a3",
                        Close = true
                    });
                }
            }

            foreach (var pend in pendings)
            {
                if (pend.Player2 == player)
                {
                    buttons.Add(new ButtonEntry
                    {
                        Name = "Отклонить телепорт",
                        Command = "tp.cmd tpc",
                        Sprite = "assets/icons/vote_down.png",
                        Color = "#d2414b",
                        Close = true
                    });
                }

                if (pend.Player == player)
                {
                    buttons.Add(new ButtonEntry
                    {
                        Name = "Принять телепорт",
                        Command = "tp.cmd tpa",
                        Sprite = "assets/icons/vote_up.png",
                        Color = "#53ac4f",
                        Close = true
                    });

                    buttons.Add(new ButtonEntry
                    {
                        Name = "Отклонить телепорт",
                        Command = "tp.cmd tpc",
                        Sprite = "assets/icons/vote_down.png",
                        Color = "#d2414b",
                        Close = true
                    });
                }
            }

            foreach (var tpQ in tpQueue)
            {
                if (tpQ.Player2 != null && tpQ.Player2 == player)
                {
                    buttons.Add(new ButtonEntry
                    {
                        Name = "Отклонить телепорт",
                        Command = "tp.cmd tpc",
                        Sprite = "assets/icons/vote_down.png",
                        Color = "#e04f58",
                        Close = true
                    });
                }

                if (tpQ.Player == player)
                {
                    buttons.Add(new ButtonEntry
                    {
                        Name = "Отклонить телепорт",
                        Command = "tp.cmd tpc",
                        Sprite = "assets/icons/vote_down.png",
                        Color = "#e04f58",
                        Close = true
                    });
                }
            }

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5" },
            }, "Hud", Layer);

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiButtonComponent { Color = "0 0 0 0.9", Command = $"tp.cmd close" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-1000 -1000", OffsetMax = "1000 1000" }
                }
            });

            for (var i = 0; i < buttons.Count; i++)
            {
                var button = buttons[i];

                var r = buttons.Count * 10 + 30;
                var c = (double)buttons.Count / 2;
                var pos = i / c * Math.PI;
                var x = r * Math.Sin(pos);
                var y = r * Math.Cos(pos);

                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Name = Layer + $".{i}",
                    Components =
                    {
                        new CuiImageComponent { Sprite = "assets/icons/reddit.png", Color = HexToCuiColor(button.Color) },
                        new CuiRectTransformComponent {AnchorMin = $"{x - 35} {y - 35}", AnchorMax = $"{x + 35} {y + 35}" },
                    },
                });

                container.Add(new CuiElement
                {
                    Parent = Layer + $".{i}",
                    Components =
                    {
                        new CuiImageComponent { Sprite = button.Sprite, Color = HexToCuiColor("#FFFFFF67") },
                        new CuiRectTransformComponent { AnchorMin = "0.275 0.275", AnchorMax = "0.725 0.725" },
                    },
                });

                container.Add(new CuiLabel
                {
                    Text = { Text = button.Name, FontSize = 13, Align = TextAnchor.MiddleCenter, Color = HexToCuiColor("#FFFFFFCA"), Font = "robotocondensed-bold.ttf" },
                    RectTransform = { AnchorMax = "1 1", AnchorMin = "0 0" }
                }, Layer + $".{i}");

                container.Add(new CuiButton
                {
                    Button = { Color = "0 0 0 0", Command = $"{button.Command}", Close = button.Close ? Layer : "" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = "" }
                }, Layer + $".{i}");

                if (button.Sprite == "assets/icons/construction.png")
                {
                    container.Add(new CuiElement
                    {
                        Parent = Layer + $".{i}",
                        Components =
                        {
                            new CuiImageComponent { Sprite = "assets/icons/occupied.png", Color = HexToCuiColor("#F54131BE") },
                            new CuiRectTransformComponent { AnchorMin = "0.85 0.85", AnchorMax = "0.85 0.85", OffsetMin = "-10 -10", OffsetMax = "10 10" },
                        },
                    });

                    container.Add(new CuiButton
                    {
                        Button = { Color = "0 0 0 0", Command = $"tp.cmd removehome {button.Name}", Close = Layer },
                        RectTransform = { AnchorMin = "0.85 0.85", AnchorMax = "0.85 0.85", OffsetMin = "-10 -10", OffsetMax = "10 10" },
                        Text = { Text = "" }
                    }, Layer + $".{i}");
                }
            }

            CuiHelper.AddUi(player, container);
        }

        private static Dictionary<ulong, List<PlayerEntry>> friends = new Dictionary<ulong, List<PlayerEntry>>();
        private class PlayerEntry
        {
            public string name;
            public ulong id;
        }

        public List<ulong> GetFriends(ulong playerid = 2952192)
        {
            if (Friends)
            {
                var friends = Friends?.Call("GetFriends", playerid) as ulong[];
                return friends.ToList();
            }

            return new List<ulong>();
        }
        public RelationshipManager.PlayerTeam API_GetPlayerTeam(BasePlayer player)
        {
            RelationshipManager.PlayerTeam playerTeam = RelationshipManager._instance.FindTeam(player.currentTeam);

            return playerTeam ?? null;
        }

        private static class PlayerHelper
        {
            private static bool FindPlayerPredicate(BasePlayer player, string nameOrUserId)
            {
                return player.displayName.IndexOf(nameOrUserId, StringComparison.OrdinalIgnoreCase) != -1 ||
                       player.UserIDString == nameOrUserId;
            }

            public static bool Find(string nameOrUserId, out BasePlayer target)
            {
                nameOrUserId = nameOrUserId.ToLower();
                foreach (BasePlayer activePlayer in BasePlayer.activePlayerList)
                {
                    if (PlayerHelper.FindPlayerPredicate(activePlayer, nameOrUserId))
                    {
                        target = activePlayer;
                        return true;
                    }
                }

                foreach (BasePlayer sleepingPlayer in BasePlayer.sleepingPlayerList)
                {
                    if (PlayerHelper.FindPlayerPredicate(sleepingPlayer, nameOrUserId))
                    {
                        target = sleepingPlayer;
                        return true;
                    }
                }

                target = (BasePlayer)null;
                return false;
            }

            public static bool FindOnline(string nameOrUserId, out BasePlayer target)
            {
                nameOrUserId = nameOrUserId.ToLower();
                foreach (BasePlayer activePlayer in BasePlayer.activePlayerList)
                {
                    if (PlayerHelper.FindPlayerPredicate(activePlayer, nameOrUserId))
                    {
                        target = activePlayer;
                        return true;
                    }
                }

                target = (BasePlayer)null;
                return false;
            }
        }

        [ConsoleCommand("tp.cmd")]
        void PlayerCMD(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            switch (args.Args[0].ToLower())
            {
                case "tpr":
                    player.Command($"chat.say \"/tpr {args.Args[1]}\"");
                    OpenTeleportMenu.Remove(player);
                    break;
                case "tpc":
                    player.Command($"chat.say \"/tpc\"");
                    OpenTeleportMenu.Remove(player);
                    break;
                case "tpa":
                    player.Command($"chat.say \"/tpa\"");
                    OpenTeleportMenu.Remove(player);
                    break;
                case "home":
                    player.Command($"chat.say \"/home {args.Args[1]}\"");
                    OpenTeleportMenu.Remove(player);
                    break;
                case "sethome":
                    player.Command($"chat.say \"/sethome {args.Args[1]}\"");
                    OpenTeleportMenu.Remove(player);
                    break;
                case "removehome":
                    player.Command($"chat.say \"/removehome {args.Args[1]}\"");
                    OpenTeleportMenu.Remove(player);
                    break;
                case "atp":
                    player.Command("chat.say /atp");
                    break;
                case "close":
                    OpenTeleportMenu.Remove(player);
                    CuiHelper.DestroyUi(player, Layer);
                    break;
            }
        }

        Dictionary<string, Vector3> CheckGetHomes(BasePlayer player)
        {
            var homelist = GetHomes(player.userID) ?? new Dictionary<string, Vector3>();
            return homelist.GroupBy(p => p.Key).ToDictionary(p => p.Key, p => p.First().Value);
        }

        private string GetGrid(Vector3 position, bool addVector)
        {
            var roundedPos = new Vector2(World.Size / 2 + position.x, World.Size / 2 - position.z);
            var grid = $"{NumberToLetter((int)(roundedPos.x / 150))}{(int)(roundedPos.y / 150)}";
            if (addVector) grid += $" {position.ToString().Replace(",", "")}";

            return grid;
        }

        private string NumberToLetter(int num)
        {
            var num2 = Mathf.FloorToInt((float)(num / 26));
            var num3 = num % 26;
            var text = string.Empty;

            if (num2 > 0) for (var i = 0; i < num2; i++)
                    text += Convert.ToChar(65 + i);

            return text + Convert.ToChar(65 + num3);
        }

        private static string HexToCuiColor(string hex)
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

        void AddPlayerAutoTP(BasePlayer player)
        {
            if (AutoTPA.ContainsKey(player.userID) == false)
            {
                AutoTPA.Add(player.userID, new AutoTPASettings());
            }

            if (GetFriends(player.userID).Count >= 0)
            {
                foreach (var friend in GetFriends(player.userID))
                {
                    var covFriend = covalence.Players.FindPlayerById(friend.ToString());
                    if (covFriend == null) continue;

                    var data = AutoTPA[player.userID];

                    var target = covalence.Players.FindPlayers(covFriend.Name);

                    var firstTarget = target.ElementAt(0);

                    if (data.PlayersList.ContainsKey(firstTarget.Name) == false)
                        data.PlayersList.Add(firstTarget.Name, ulong.Parse(firstTarget.Id));
                }
            }
        }

        [ChatCommand("atp")]
        void cmdAutoTPA(BasePlayer player, string com, string[] args)
        {
            if (!AutoTPA.ContainsKey(player.userID))
            {
                AutoTPA.Add(player.userID, new AutoTPASettings());
            }

            if (args == null || args.Length <= 0)
            {
                if (AutoTPA[player.userID].Enabled)
                {
                    AutoTPA[player.userID].Enabled = false;
                    DDrawMenu(player);
                    SendNotify(player, "Вы успешно <color=#851716>отключили</color> автопринятие запроса на телепорт");
                }
                else
                {
                    AutoTPA[player.userID].Enabled = true;
                    DDrawMenu(player);
                    SendNotify(player, "Вы успешно <color=#2abd2a>включили</color> автопринятие запроса на телепорт");
                }
            }
        }



        [ConsoleCommand("sethome")]
        void cmdChatSetHome(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            if (arg.Args == null || arg.Args.Length < 1) return;
            cmdChatSetHome(player, "", new[] {
                    arg.Args[0]
                }
            );
        }
        [ChatCommand("sethome")]
        void cmdChatSetHome(BasePlayer player, string command, string[] args)
        {
            var uid = player.userID;
            var pos = player.PivotPoint();
            var foundation = GetFoundation(pos);
            var bulds = GetBuldings(pos);
            if (foundationEx && foundation == null)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["foundationmissing"]);
                return;
            }
            if (!foundationEx && bulds == null)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["foundationmissing"]);
                return;
            }
            if (args.Length != 1)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["sethomeArgsError"]);
                return;
            }
            if (CancelTPMetabolism && player.metabolism.bleeding.value > 0)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["woundedAction"]);
                return;
            }
            if (CancelTPRadiation && player.radiationLevel > 10)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["Radiation"]);
                return;
            }
            if (player.IsWounded() && CancelTPWounded)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["woundedAction"]);
                return;
            }
            if (sethomeBlock.Contains(player.userID))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["sethomeBlock"]);
                return;
            }

            if (foundationOwnerFC && foundationOwner)
            {
                if (!foundationEx && bulds.OwnerID != uid)
                {
                    if (!IsFriends(bulds.OwnerID, player.userID) && !IsClanMember(bulds.OwnerID, player.userID) && !IsTeamate(player, bulds.OwnerID))
                    {
                        Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                        SendNotify(player, Messages["foundationownerFC"]);
                        return;
                    }
                }
                if (foundationEx && foundation.OwnerID != uid)
                {
                    if (!IsFriends(foundation.OwnerID, player.userID) && !IsClanMember(foundation.OwnerID, player.userID) && !IsTeamate(player, bulds.OwnerID))
                    {
                        Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                        SendNotify(player, Messages["foundationownerFC"]);
                        return;
                    }
                }
            }
            if (foundationOwner)
            {
                if (foundationEx && foundation.OwnerID != uid && foundationOwnerFC == (!IsFriends(foundation.OwnerID, player.userID) && !IsClanMember(foundation.OwnerID, player.userID) && IsTeamate(player, bulds.OwnerID)))
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendNotify(player, Messages["foundationowner"]);
                    return;
                }
                if (!foundationEx && bulds.OwnerID != uid && foundationOwnerFC == (!IsFriends(bulds.OwnerID, player.userID) && !IsClanMember(bulds.OwnerID, player.userID) && IsTeamate(player, bulds.OwnerID)))
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendNotify(player, Messages["foundationowner"]);
                    return;
                }
            }
            var name = args[0];
            SetHome(player, name);
        }

        [ChatCommand("removehome")]
        void cmdChatRemoveHome(BasePlayer player, string command, string[] args)
        {
            if (args.Length != 1)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["removehomeArgsError"]);
                return;
            }
            if (!homes.ContainsKey(player.userID))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["homesmissing"]);
                return;
            }
            var name = args[0];
            var playerHomes = homes[player.userID];
            if (!playerHomes.ContainsKey(name))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["homenotexist"]);
                return;
            }
            foreach (var sleepingBag in SleepingBag.FindForPlayer(player.userID, true))
            {
                if (Vector3.Distance(sleepingBag.transform.position, playerHomes[name]) < 1)
                {
                    sleepingBag.Kill();
                    break;
                }
            }
            Effect.server.Run(EffectPrefab1, player, 0, Vector3.zero, Vector3.forward);
            playerHomes.Remove(name);
            SendNotify(player, string.Format(Messages["removehomesuccess"], name));
        }
        [ConsoleCommand("home")]
        void cmdHome(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            if (arg.Args == null || arg.Args.Length < 1) return;
            cmdChatHome(player, "", new[] {
                    arg.Args[0]
                }
            );
        }
        [ConsoleCommand("tpa")]
        void cmdTpa(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            cmdChatTpa(player, "", new String[0]);
        }
        [ChatCommand("homelist")]
        private void cmdHomeList(BasePlayer player, string command, string[] args)
        {
            if (!homes.ContainsKey(player.userID) || homes[player.userID].Count == 0)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["homesmissing"]);
                return;
            }
            var playerHomes = homes[player.userID];
            var time = (GetHomeLimit(player.userID) - playerHomes.Count);
            var homelist = playerHomes.Select(x => GetSleepingBag(x.Key, x.Value) != null ? $"{x.Key} {x.Value}" : $"Дом: {x.Key} {x.Value}");
            foreach (var home in playerHomes.ToList())
            {
                if (createSleepingBug)
                {
                    if (!GetSleepingBag(home.Key, home.Value)) playerHomes.Remove(home.Key);
                }
            }
            SendNotify(player, string.Format(Messages["homeslist"], time, string.Join("\n", homelist.ToArray())));
        }
        [ChatCommand("home")]
        void cmdChatHome(BasePlayer player, string command, string[] args)
        {
            if (args.Length != 1)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["homeArgsError"]);
                return;
            }
            var ret = Interface.Call("CanTeleport", player) as string;
            if (ret != null)
            {
                SendNotify(player, ret);
                return;
            }
            if (!EnabledShipTP && player.GetParentEntity() is CargoShip)
            {
                SendNotify(player, Messages["PlayerIsOnCargoShip"]);
                return;
            }
            if (!EnabledBallonTP && player.GetParentEntity() is HotAirBalloon)
            {
                SendNotify(player, Messages["PlayerIsOnHotAirBalloon"]);
                return;
            }
            if (player.IsWounded() && CancelTPWounded)
            {
                SendNotify(player, Messages["woundedAction"]);
                return;
            }
            if (player.metabolism.bleeding.value > 0 && CancelTPMetabolism)
            {
                SendNotify(player, Messages["woundedAction"]);
                return;
            }
            if (CancelTPRadiation && player.radiationLevel > 10)
            {
                SendNotify(player, Messages["Radiation"]);
                return;
            }
            int seconds;
            if (cooldownsHOME.TryGetValue(player.userID, out seconds) && seconds > 0)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, string.Format(Messages["tpkd"], TimeToString(seconds)));
                return;
            }
            if (homecupboard)
            {
                var privilege = player.GetBuildingPrivilege(player.WorldSpaceBounds());
                if (privilege != null && !player.IsBuildingAuthed())
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendNotify(player, Messages["tphomecupboard"]);
                    return;
                }
            }
            if (!homes.ContainsKey(player.userID))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["homesmissing"]);
                return;
            }
            var name = args[0];
            var playerHomes = homes[player.userID];
            if (!playerHomes.ContainsKey(name))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["homenotexist"]);
                return;
            }
            var time = GetTeleportTime(player.userID);
            var pos = playerHomes[name];
            SleepingBag bag = GetSleepingBag(name, pos);
            if (createSleepingBug)
            {
                if (bag == null)
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendNotify(player, Messages["sleepingbagmissing"]);
                    playerHomes.Remove(name);
                    return;
                }
            }
            if (!createSleepingBug)
            {
                var bulds = GetBuldings(pos);
                if (bulds == null)
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendNotify(player, Messages["foundationmissingR"]);
                    playerHomes.Remove(name);
                    return;
                }
            }
            if (CancelTPCold && player.metabolism.temperature.value < 0)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["coldplayer"]);
                return;
            }
            if (tpQueue.Any(p => p.Player == player) || pendings.Any(p => p.Player2 == player))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["tpError"]);
                return;
            }
            var lastTp = tpQueue.Find(p => p.Player == player);
            if (lastTp != null)
            {
                tpQueue.Remove(lastTp);
            }
            tpQueue.Add(new TP(player, pos, time, false, false));
            Effect.server.Run(EffectPrefab1, player, 0, Vector3.zero, Vector3.forward);
            SendNotify(player, string.Format(Messages["homequeue"], name, TimeToString(time)));
        }
        [ChatCommand("tpr")]
        void cmdChatTpr(BasePlayer player, string command, string[] args)
        {
            if (!enabledTPR) return;
            if (args.Length != 1)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["tprArgsError"]);
                return;
            }
            var ret = Interface.Call("CanTeleport", player) as string;
            if (ret != null)
            {
                SendNotify(player, ret);
                return;
            }
            if (!EnabledShipTP && player.GetParentEntity() is CargoShip)
            {
                SendNotify(player, Messages["PlayerIsOnCargoShip"]);
                return;
            }
            if (!EnabledBallonTP && player.GetParentEntity() is HotAirBalloon)
            {
                SendNotify(player, Messages["PlayerIsOnHotAirBalloon"]);
                return;
            }
            if (player.IsWounded() && CancelTPWounded)
            {
                SendNotify(player, Messages["woundedAction"]);
                return;
            }
            if (player.metabolism.bleeding.value > 0 && CancelTPMetabolism)
            {
                SendNotify(player, Messages["woundedAction"]);
                return;
            }
            if (CancelTPRadiation && player.radiationLevel > 10)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["Radiation"]);
                return;
            }
            if (restrictTPRCupboard)
            {
                var privilege = player.GetBuildingPrivilege(player.WorldSpaceBounds());
                if (privilege != null && !player.IsBuildingAuthed())
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendNotify(player, Messages["tpcupboard"]);
                    return;
                }
            }
            var name = args[0];
            var target = FindBasePlayer(name);
            if (target == null)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["playermissing"]);
                return;
            }
            if (target == player)
            {
                SendNotify(player, Messages["playerisyou"]);
                return;
            }
            if (FriendsEnabled)
                if (!IsFriends(target.userID, player.userID) && !IsTeamate(player, target.userID) && !IsClanMember(player.userID, target.userID))
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendNotify(player, Messages["PlayerNotFriend"]);
                    return;
                }
            int seconds = 0;
            if (restrictCupboard && player.GetBuildingPrivilege(player.WorldSpaceBounds()) != null && !player.GetBuildingPrivilege(player.WorldSpaceBounds()).authorizedPlayers.Select(p => p.userid).Contains(player.userID))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["tpcupboard"]);
                return;
            }

            if (cooldownsTP.TryGetValue(player.userID, out seconds) && seconds > 0)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, string.Format(Messages["tpkd"], TimeToString(seconds)));
                return;
            }
            if (CancelTPCold && player.metabolism.temperature.value < 0)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["coldplayer"]);
                return;
            }
            if (tpQueue.Any(p => p.Player == player) || pendings.Any(p => p.Player2 == player))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["tpError"]);
                return;
            }

            if (tpQueue.Any(p => p.Player == target) || pendings.Any(p => p.Player2 == target))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["tpError"]);
                return;
            }
            SendNotify(player, string.Format(Messages["tprrequestsuccess"], target.displayName));
            SendNotify(target, string.Format(Messages["tprpending"], player.displayName));
            Effect.server.Run(EffectPrefab1, target, 0, Vector3.zero, Vector3.forward);


            pendings.Add(new TP(target, Vector3.zero, 15, false, false, player));
            if (AutoTPA.ContainsKey(target.userID))
            {
                var key = AutoTPA[target.userID].PlayersList.FirstOrDefault(p => p.Value == player.userID).Key;
                if (AutoTPA[target.userID].Enabled && !string.IsNullOrEmpty(key))
                {
                    target.Command("tp.cmd tpa");
                    SendNotify(target, Messages["TPASuccess"]);
                    return;

                }
            }
        }
        [ChatCommand("tpa")]
        void cmdChatTpa(BasePlayer player, string command, string[] args)
        {
            if (!enabledTPR) return;
            var tp = pendings.Find(p => p.Player == player);
            if (tp == null)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["tpanotexist"]);
                return;
            }
            BasePlayer pendingPlayer = tp.Player2;
            if (pendingPlayer == null)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["tpanotexist"]);
                return;
            }
            if (!EnabledBallonTP && player.GetParentEntity() is HotAirBalloon)
            {
                SendNotify(player, Messages["PlayerIsOnHotAirBalloon"]);
                return;
            }
            if (!EnabledShipTP && player.GetParentEntity() is CargoShip)
            {
                SendNotify(player, Messages["PlayerIsOnCargoShip"]);
                return;
            }
            if (CancelTPCold && player.metabolism.temperature.value < 0)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["coldplayer"]);
                return;
            }
            if (player.IsWounded() && CancelTPWounded)
            {
                SendNotify(player, Messages["woundedAction"]);
                return;
            }
            if (player.metabolism.bleeding.value > 0 && CancelTPMetabolism)
            {
                SendNotify(player, Messages["woundedAction"]);
                return;
            }
            if (CancelTPRadiation && player.radiationLevel > 10)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["Radiation"]);
                return;
            }
            if (restrictCupboard && player.GetBuildingPrivilege(player.WorldSpaceBounds()) != null && !player.GetBuildingPrivilege(player.WorldSpaceBounds()).authorizedPlayers.Select(p => p.userid).Contains(player.userID))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["tpacupboard"]);
                return;
            }
            var ret = Interface.Call("CanTeleport", player) as string;
            if (ret != null)
            {
                SendNotify(player, ret);
                return;
            }
            if (FriendsEnabled)
                if (!IsFriends(pendingPlayer.userID, player.userID) && !IsTeamate(player, pendingPlayer.userID) && !IsClanMember(player.userID, pendingPlayer.userID))
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendNotify(player, Messages["PlayerNotFriend"]);
                    return;
                }
            var time = GetTeleportTime(pendingPlayer.userID);
            pendings.Remove(tp);
            var lastTp = tpQueue.Find(p => p.Player == pendingPlayer);
            if (lastTp != null)
            {
                tpQueue.Remove(lastTp);
            }
            var Enabled = player.GetParentEntity() is CargoShip || player.GetParentEntity() is HotAirBalloon;
            tpQueue.Add(new TP(pendingPlayer, player.transform.position, time, Enabled, false, player));
            Effect.server.Run(EffectPrefab1, player, 0, Vector3.zero, Vector3.forward);
            CuiHelper.DestroyUi(player, "teleportmenu");
            SendNotify(pendingPlayer, string.Format(Messages["tpqueue"], player.displayName, TimeToString(time)));
            if (args.Length <= 0) SendNotify(player, string.Format(Messages["tpasuccess"], pendingPlayer.displayName, TimeToString(time)));
        }
        [ChatCommand("tpc")]
        void cmdChatTpc(BasePlayer player, string command, string[] args)
        {
            var tp = pendings.Find(p => p.Player == player);
            BasePlayer target = tp?.Player2;
            if (target != null)
            {
                pendings.Remove(tp);
                SendNotify(player, Messages["tpc"]);
                SendNotify(target, string.Format(Messages["tpctarget"], player.displayName));
                return;
            }
            if (player.IsWounded() && CancelTPWounded)
            {
                SendNotify(player, Messages["woundedAction"]);
                return;
            }
            if (player.metabolism.bleeding.value > 0 && CancelTPMetabolism)
            {
                SendNotify(player, Messages["woundedAction"]);
                return;
            }
            if (CancelTPRadiation && player.radiationLevel > 10)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["Radiation"]);
                return;
            }
            if (CancelTPCold && player.metabolism.temperature.value < 0)
            {
                SendNotify(player, Messages["coldplayer"]);
                return;
            }
            foreach (var pend in pendings)
            {
                if (pend.Player2 == player)
                {
                    SendNotify(player, Messages["tpc"]);
                    SendNotify(pend.Player, string.Format(Messages["tpctarget"], player.displayName));

                    CuiHelper.DestroyUi(player, "teleportmenu");
                    pendings.Remove(pend);
                    return;
                }
            }
            foreach (var tpQ in tpQueue)
            {
                if (tpQ.Player2 != null && tpQ.Player2 == player)
                {
                    CuiHelper.DestroyUi(player, "teleportmenu");
                    SendNotify(player, Messages["tpc"]);
                    SendNotify(tpQ.Player, string.Format(Messages["tpctarget"], player.displayName));
                    tpQueue.Remove(tpQ);
                    return;
                }
                if (tpQ.Player == player)
                {
                    CuiHelper.DestroyUi(player, "teleportmenu");
                    SendNotify(player, Messages["tpc"]);
                    if (tpQ.Player2 != null) SendNotify(tpQ.Player2, string.Format(Messages["tpctarget"], player.displayName));
                    tpQueue.Remove(tpQ);
                    return;
                }
            }
        }
        void SpectateFinish(BasePlayer player)
        {
            player.Command("camoffset", "0,1,0");
            player.StopSpectating();
            player.SetParent(null);
            player.gameObject.SetLayerRecursive(17);
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, false);
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, false);
            player.SendNetworkUpdateImmediate();
            player.metabolism.Reset();
            player.InvokeRepeating("InventoryUpdate", 1f, 0.1f * UnityEngine.Random.Range(0.99f, 1.01f));
            if (player.net?.connection != null) player.ClientRPCPlayer(null, player, "StartLoading");
            player.StartSleeping();
            if (lastPositions.ContainsKey(player.userID))
            {
                Vector3 lastPosition = lastPositions[player.userID] + Vector3.up;
                player.MovePosition(lastPosition);
                if (player.net?.connection != null) player.ClientRPCPlayer(null, player, "ForcePositionTo", lastPosition);
                lastPositions.Remove(player.userID);

            }

            if (player.net?.connection != null) player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate(false);
            if (player.net?.connection == null) return;
            try
            {
                player.ClearEntityQueue(null);
            }
            catch { }
            player.SendFullSnapshot();

            SendNotify(player, "Слежка закончена!");
        }


        private void OnUserConnected(IPlayer player) => ResetSpectate(player);

        private void OnUserDisconnected(IPlayer player) => ResetSpectate(player);

        private void ResetSpectate(IPlayer player)
        {
            player.Command("camoffset 0,1,0");

            if (lastPositions.ContainsKey(ulong.Parse(player.Id)))
            {
                lastPositions.Remove(ulong.Parse(player.Id));
            }
        }

        object OnPlayerSpectateEnd(BasePlayer player, string spectateFilter)
        {
            player.Command("camoffset", "0,1,0");
            return null;
        }

        [ChatCommand("tpl")]
        void cmdChattpGo(BasePlayer player, string command, string[] args)
        {
            if (!EnabledTPLForPlayers && !player.IsAdmin) return;
            if (args == null || args.Length == 0)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["tpArgsError"]);
                return;
            }
            switch (args[0])
            {
                default:
                    if (tpsave.Count <= 0)
                    {
                        Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                        SendNotify(player, Messages["homesmissing"]);
                        return;
                    }
                    var nametp = args[0];
                    var tp = tpsave.Find(p => p.Name == nametp);
                    if (tp == null)
                    {
                        Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                        SendNotify(player, Messages["homenotexist"]);
                        return;
                    }
                    var position = tp.pos;
                    var ret = Interface.Call("CanTeleport", player) as string;
                    if (ret != null)
                    {
                        SendNotify(player, ret);
                        return;
                    }
                    int seconds;
                    if (cooldownsHOME.TryGetValue(player.userID, out seconds) && seconds > 0)
                    {
                        Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                        SendNotify(player, string.Format(Messages["tpkd"], TimeToString(seconds)));
                        return;
                    }
                    var lastTp = tpQueue.Find(p => p.Player == player);
                    if (lastTp != null) tpQueue.Remove(lastTp);
                    Effect.server.Run(EffectPrefab1, player, 0, Vector3.zero, Vector3.forward);
                    if (TPLAdmin && player.IsAdmin) Teleport(player, position);
                    else
                    {
                        tpQueue.Add(new TP(player, position, TplPedingTime, false, true));
                        SendNotify(player, string.Format(Messages["homequeue"], nametp, TimeToString(TplPedingTime)));
                    }
                    return;
                case "add":
                    if (!player.IsAdmin) return;
                    if (args == null || args.Length == 1)
                    {
                        Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                        SendNotify(player, Messages["settpArgsError"]);
                        return;
                    }
                    var nameAdd = args[1];
                    SetTpSave(player, nameAdd);
                    return;
                case "remove":
                    if (!player.IsAdmin) return;
                    if (args == null || args.Length == 1)
                    {
                        Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                        SendNotify(player, Messages["removetpArgsError"]);
                        return;
                    }
                    nametp = args[1];
                    if (tpsave.Count > 0)
                    {
                        tp = tpsave.Find(p => p.Name == nametp);
                        if (tp == null)
                        {
                            Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                            SendNotify(player, Messages["homesmissing"]);
                            return;
                        }
                        Effect.server.Run(EffectPrefab1, player, 0, Vector3.zero, Vector3.forward);
                        tpsave.Remove(tp);
                        SendNotify(player, string.Format(Messages["removehomesuccess"], nametp));
                    }
                    return;
                case "list":
                    if (tpsave.Count <= 0)
                    {
                        Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                        SendNotify(player, Messages["TPLmissing"]);
                        return;
                    }
                    var tplist = tpsave.Select(x => $"{x.Name} {x.pos}");
                    SendNotify(player, string.Format(Messages["TPLList"], string.Join("\n", tplist.ToArray())));
                    return;
            }
        }
        [ChatCommand("tpspec")]
        void cmdTPSpec(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            if (!player.IsSpectating())
            {
                if (args.Length == 0 || args.Length != 1)
                {
                    SendNotify(player, Messages["tpspecError"]);
                    return;
                }
                string name = args[0];
                BasePlayer target = FindBasePlayer(name);
                if (target == null)
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendNotify(player, Messages["playermissing"]);
                    return;
                }
                switch (args.Length)
                {
                    case 1:
                        if (!target.IsConnected)
                        {
                            SendNotify(player, Messages["playermissingOff"]);
                            return;
                        }
                        if (target.IsDead())
                        {
                            SendNotify(player, Messages["playermissingOrDeath"]);
                            return;
                        }
                        if (ReferenceEquals(target, player))
                        {
                            SendNotify(player, Messages["playerItsYou"]);
                            return;
                        }
                        if (target.IsSpectating())
                        {
                            SendNotify(player, Messages["playerItsSpec"]);
                            return;
                        }
                        spectatingPlayers.Remove(target);
                        lastPositions[player.userID] = player.transform.position;
                        HeldEntity heldEntity = player.GetActiveItem()?.GetHeldEntity() as HeldEntity;
                        heldEntity?.SetHeld(false);
                        player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, true);
                        player.gameObject.SetLayerRecursive(10);
                        player.CancelInvoke("MetabolismUpdate");
                        player.CancelInvoke("InventoryUpdate");
                        player.ClearEntityQueue();
                        player.SendEntitySnapshot(target);
                        player.gameObject.Identity();
                        player.SetParent(target);
                        player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, true);
                        player.Command("camoffset", "0, 1.3, 0");
                        SendNotify(player, $"Вы наблюдаете за игроком {target}! Что бы переключаться между игроками, нажимайте: Пробел\nЧтобы выйти с режима наблюдения, введите: /tpspec");
                        break;
                }
            }
            else SpectateFinish(player);
        }
        [ChatCommand("tp")]
        void cmdTP(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            switch (args.Length)
            {
                case 1:
                    string name = args[0];
                    BasePlayer target = FindBasePlayer(name);
                    if (target == null)
                    {
                        Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                        SendNotify(player, Messages["playermissing"]);
                        return;
                    }
                    Teleport(player, target);
                    break;
                case 2:
                    string name1 = args[0];
                    string name2 = args[1];
                    BasePlayer target1 = FindBasePlayer(name1);
                    BasePlayer target2 = FindBasePlayer(name2);
                    if (target1 == null || target2 == null)
                    {
                        SendNotify(player, Messages["playermissing"]);
                        return;
                    }
                    Teleport(target1, target2);
                    break;
                case 3:
                    float x = float.Parse(args[0].Replace(",", ""));
                    float y = float.Parse(args[1].Replace(",", ""));
                    float z = float.Parse(args[2]);
                    Teleport(player, x, y, z);
                    break;
            }
        }
        [ConsoleCommand("home.wipe")]
        private void CmdTest(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null) return;
            PrintWarning("Запущен ручной вайп. Очищаем данные с data/Teleportation");
            WipeData();
        }
        public string TimeToString(double time)
        {
            TimeSpan elapsedTime = TimeSpan.FromSeconds(time);
            int hours = elapsedTime.Hours;
            int minutes = elapsedTime.Minutes;
            int seconds = elapsedTime.Seconds;
            int days = Mathf.FloorToInt((float)elapsedTime.TotalDays);
            string s = "";
            if (days > 0) s += $"{days} дн.";
            if (hours > 0) s += $"{hours} ч. ";
            if (minutes > 0) s += $"{minutes} мин. ";
            if (seconds > 0) s += $"{seconds} сек.";
            else s = s.TrimEnd(' ');
            return s;
        }
        void OnPlayerDisconnected(BasePlayer player)
        {
            pendings.RemoveAll(p => p.Player == player || p.Player2 == player);
            tpQueue.RemoveAll(p => p.Player == player || p.Player2 == player);
        }

        void OnServerInitialized()
        {
            LoadData();
            LoadDefaultConfig();
            lang.RegisterMessages(Messages, this, "en");
            Messages = lang.GetMessages("en", this);
            timer.Every(1f, TeleportationTimerHandle);
            timer.Every(300, SaveData);

            if (500 > 0)
            {
                timer.Every(500, () => { friends.Clear(); });
            }

            foreach (var player in BasePlayer.activePlayerList.ToList())
            {
                var friendList = GetFriends(player.userID);
                if (friendList.Count > 0)
                {
                    AddPlayerAutoTP(player);
                }
            }
        }
        void Unload() => SaveData();
        void OnEntityBuilt(Planner planner, GameObject gameobject)
        {
            if (planner == null || gameobject == null) return;
            var player = planner.GetOwnerPlayer();
            BaseEntity entity = gameobject.ToBaseEntity();
            if (entity == null) return;
            if (gameobject.name.Contains("foundation"))
            {
                var pos = gameobject.transform.position;
                foreach (var pending in tpQueue)
                {
                    if (Vector3.Distance(pending.pos, pos) < 3)
                    {
                        entity.Kill();
                        SendNotify(planner.GetOwnerPlayer(), "Нельзя, тут телепортируется игрок!");
                        return;
                    }
                }
            }
        }
        [PluginReference] Plugin Duel, Notify;
        bool InDuel(BasePlayer player) => Duel?.Call<bool>("IsPlayerOnActiveDuel", player) ?? false;
        void TeleportationTimerHandle()
        {
            List<ulong> tpkdToRemove = new List<ulong>();
            foreach (var uid in cooldownsTP.Keys.ToList())
            {
                if (--cooldownsTP[uid] <= 0)
                {
                    tpkdToRemove.Add(uid);
                }
            }
            tpkdToRemove.ForEach(p => cooldownsTP.Remove(p));
            List<ulong> tpkdHomeToRemove = new List<ulong>();
            foreach (var uid in cooldownsHOME.Keys.ToList())
            {
                if (--cooldownsHOME[uid] <= 0) tpkdHomeToRemove.Add(uid);
            }
            tpkdHomeToRemove.ForEach(p => cooldownsHOME.Remove(p));
            for (int i = pendings.Count - 1;
            i >= 0;
            i--)
            {
                var pend = pendings[i];
                if (pend.Player != null && pend.Player.IsConnected && pend.Player.IsWounded())
                {
                    CuiHelper.DestroyUi(pend.Player, "teleportmenu");
                    SendNotify(pend.Player, Messages["tpwounded"]);
                    pendings.RemoveAt(i);
                    continue;
                }
                if (--pend.seconds <= 0)
                {
                    pendings.RemoveAt(i);

                    CuiHelper.DestroyUi(pend.Player, "teleportmenu");
                    if (pend.Player2 != null && pend.Player2.IsConnected) SendNotify(pend.Player2, Messages["tppendingcanceled"]);
                    if (pend.Player != null && pend.Player.IsConnected) SendNotify(pend.Player, Messages["tpacanceled"]);
                }
            }
            for (int i = tpQueue.Count - 1;
            i >= 0;
            i--)
            {
                var reply = 1;
                if (reply == 0) { }
                var tp = tpQueue[i];
                if (tp.Player != null)
                {
                    if (tp.Player.IsConnected && (CancelTPWounded && tp.Player.IsWounded()) || (tp.Player.metabolism.bleeding.value > 0 && CancelTPMetabolism) || (CancelTPRadiation && tp.Player.radiationLevel > 10))
                    {
                        SendNotify(tp.Player, Messages["tpwounded"]);
                        if (tp.Player2 != null && tp.Player2.IsConnected) SendNotify(tp.Player2, Messages["tpWoundedTarget"]);
                        tpQueue.RemoveAt(i);
                        continue;
                    }
                    if (InDuel(tp.Player))
                    {
                        SendNotify(tp.Player, Messages["InDuel"]);
                        if (tp.Player2 != null && tp.Player2.IsConnected) SendNotify(tp.Player2, Messages["InDuelTarget"]);
                        tpQueue.RemoveAt(i);
                        continue;
                    }
                    if (restrictTPRCupboard)
                    {
                        var privilege = tp.Player.GetBuildingPrivilege(tp.Player.WorldSpaceBounds());
                        if (privilege != null && !tp.Player.IsBuildingAuthed())
                        {
                            Effect.server.Run(EffectPrefab, tp.Player, 0, Vector3.zero, Vector3.forward);

                            SendNotify(tp.Player, Messages["tpcupboard"]);
                            if (tp.Player2 != null && tp.Player2.IsConnected) SendNotify(tp.Player2, Messages["tpcupboardTarget"]);
                            tpQueue.RemoveAt(i);
                            return;
                        }
                    }
                }

                if (tp.Player2 != null)
                {
                    if (tp.Player2.IsConnected && (tp.Player2.IsWounded() && CancelTPWounded) || (tp.Player2.metabolism.bleeding.value > 0 && CancelTPMetabolism) || (CancelTPRadiation && tp.Player2.radiationLevel > 10))
                    {
                        SendNotify(tp.Player2, Messages["tpwounded"]);
                        if (tp.Player != null && tp.Player.IsConnected) SendNotify(tp.Player, Messages["tpWoundedTarget"]);
                        tpQueue.RemoveAt(i);
                        continue;
                    }
                    if (InDuel(tp.Player2))
                    {
                        SendNotify(tp.Player2, Messages["InDuel"]);
                        if (tp.Player != null && tp.Player.IsConnected) SendNotify(tp.Player, Messages["InDuelTarget"]);
                        tpQueue.RemoveAt(i);
                        continue;
                    }
                    if (restrictTPRCupboard)
                    {
                        var privilege = tp.Player2.GetBuildingPrivilege(tp.Player2.WorldSpaceBounds());
                        if (privilege != null && !tp.Player2.IsBuildingAuthed())
                        {
                            Effect.server.Run(EffectPrefab, tp.Player2, 0, Vector3.zero, Vector3.forward);
                            if (tp.Player != null && tp.Player.IsConnected) SendNotify(tp.Player, Messages["tpcupboardTarget"]);

                            SendNotify(tp.Player2, Messages["tpcupboard"]);
                            return;
                        }
                    }
                }
                if (--tp.seconds <= 0)
                {
                    tpQueue.RemoveAt(i);
                    var ret = Interface.CallHook("CanTeleport", tp.Player) as string;
                    if (ret != null)
                    {
                        SendNotify(tp.Player, ret);
                        continue;
                    }
                    if (CheckInsideInFoundation(tp.pos))
                    {
                        SendNotify(tp.Player, Messages["InsideInFoundationTP"]);
                        continue;
                    }
                    if (tp.Player2 != null)
                    {
                        tp.Player.SetParent(tp.Player2.GetParentEntity());
                        if (tp.EnabledShip) tp.pos = tp.Player2.transform.position;
                    }
                    if (tp.Player2 != null && tp.Player != null && tp.Player.IsConnected && tp.Player2.IsConnected)
                    {
                        var seconds = GetKD(tp.Player.userID);
                        cooldownsTP[tp.Player.userID] = seconds;
                        SendNotify(tp.Player, string.Format(Messages["tpplayersuccess"], tp.Player2.displayName));
                    }
                    else if (tp.Player != null && tp.Player.IsConnected)
                    {
                        tp.Player.SetParent(null);
                        if (tp.TPL)
                        {
                            var seconds = TPLCooldown;
                            cooldownsHOME[tp.Player.userID] = seconds;
                            SendNotify(tp.Player, Messages["tplsuccess"]);
                        }
                        else
                        {
                            var seconds = GetKDHome(tp.Player.userID);
                            cooldownsHOME[tp.Player.userID] = seconds;
                            SendNotify(tp.Player, Messages["tphomesuccess"]);
                        }
                    }
                    Teleport(tp.Player, tp.pos);
                    NextTick(() => Interface.CallHook("OnPlayerTeleported", tp.Player));
                }
            }
        }
        void SetTpSave(BasePlayer player, string name)
        {
            var position = player.transform.position;
            if (tpsave.Count > 0)
            {
                var tp = tpsave.Find(p => p.Name == name);
                if (tp != null)
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendNotify(player, Messages["homeexist"]);
                    return;
                }
            }
            tpsave.Add(new TPList()
            {
                Name = name,
                pos = position
            }
            );
            Effect.server.Run(EffectPrefab1, player, 0, Vector3.zero, Vector3.forward);
            SendNotify(player, string.Format(Messages["homesucces"], name));
            timer.Once(10f, () => sethomeBlock.Remove(player.userID));
        }
        void SetHome(BasePlayer player, string name)
        {
            var uid = player.userID;
            var pos = player.transform.position;
            if (player.GetBuildingPrivilege(player.WorldSpaceBounds()) != null && !player.GetBuildingPrivilege(player.WorldSpaceBounds()).authorizedPlayers.Select(p => p.userid).Contains(player.userID))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["sethomecupboard"]);
                return;
            }
            Dictionary<string, Vector3> playerHomes;
            if (!homes.TryGetValue(uid, out playerHomes)) playerHomes = (homes[uid] = new Dictionary<string, Vector3>());
            if (GetHomeLimit(uid) == playerHomes.Count)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["maxhomes"]);
                return;
            }
            if (playerHomes.ContainsKey(name))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendNotify(player, Messages["homeexist"]);
                return;
            }
            if (CheckInsideInFoundation(player.transform.position))
            {
                SendNotify(player, Messages["InsideInFoundation"]);
                return;
            }
            playerHomes.Add(name, pos);
            if (createSleepingBug)
            {
                CreateSleepingBag(player, pos, name);
            }
            Effect.server.Run(EffectPrefab1, player, 0, Vector3.zero, Vector3.forward);
            SendNotify(player, string.Format(Messages["homesucces"], name));
            sethomeBlock.Add(player.userID);
            timer.Once(10f, () => sethomeBlock.Remove(player.userID));
        }
        private bool CheckInsideInFoundation(Vector3 position)
        {
            foreach (var hit in Physics.RaycastAll(position, Vector3.up, 2f, LayerMask.GetMask("Terrain", "World", "Construction", "Deployed")))
            {
                if (hit.GetCollider().name.Contains("foundation")) return true;
            }
            foreach (var hit in Physics.RaycastAll(position + Vector3.up + Vector3.up + Vector3.up + Vector3.up, Vector3.down, 2f, LayerMask.GetMask("Terrain", "World", "Construction", "Deployed")))
            {
                if (hit.GetCollider().name.Contains("foundation")) return true;
            }
            return false;
        }
        int GetKDHome(ulong uid)
        {
            int min = tpkdhomeDefault;
            foreach (var privilege in tpkdhomePerms) if (PermissionService.HasPermission(uid, privilege.Key)) min = Mathf.Min(min, privilege.Value);
            return min;
        }
        int GetKD(ulong uid)
        {
            int min = tpkdDefault;
            foreach (var privilege in tpkdPerms) if (PermissionService.HasPermission(uid, privilege.Key)) min = Mathf.Min(min, privilege.Value);
            return min;
        }
        int GetHomeLimit(ulong uid)
        {
            int max = homelimitDefault;
            foreach (var privilege in homelimitPerms) if (PermissionService.HasPermission(uid, privilege.Key)) max = Mathf.Max(max, privilege.Value);
            return max;
        }
        int GetTeleportTime(ulong uid)
        {
            int min = teleportSecsDefault;
            foreach (var privilege in teleportSecsPerms) if (PermissionService.HasPermission(uid, privilege.Key)) min = Mathf.Min(min, privilege.Value);
            return min;
        }
        BaseEntity GetBuldings(Vector3 pos)
        {
            RaycastHit hit;
            if (Physics.Raycast(new Ray(pos, Vector3.down), out hit, 0.2f))
            {
                var entity = hit.GetEntity();
                if (entity != null) return entity;
                else return null;
            }
            return null;
        }
        private BaseEntity GetFoundation(Vector3 pos)
        {
            RaycastHit hit;
            if (Physics.Raycast(pos, Vector3.down, out hit, LayerMask.GetMask("Terrain", "World", "Construction", "Deployed")))
            {
                var entity = hit.GetEntity();
                if (entity != null) if (entity.PrefabName.Contains("foundation")) return entity;
            }
            return null;
        }
        SleepingBag GetSleepingBag(string name, Vector3 pos)
        {
            List<SleepingBag> sleepingBags = new List<SleepingBag>();
            Vis.Components(pos, .1f, sleepingBags);
            return sleepingBags.Count > 0 ? sleepingBags[0] : null;
        }
        void CreateSleepingBag(BasePlayer player, Vector3 pos, string name)
        {
            SleepingBag sleepingBag = GameManager.server.CreateEntity("assets/prefabs/deployable/sleeping bag/sleepingbag_leather_deployed.prefab", pos, Quaternion.identity) as SleepingBag;
            if (sleepingBag == null) return;
            sleepingBag.skinID = 1265527678;
            sleepingBag.deployerUserID = player.userID;
            sleepingBag.niceName = name;
            sleepingBag.OwnerID = player.userID;
            sleepingBag.Spawn();
            sleepingBag.SendNetworkUpdate();
        }
        Dictionary<string, Vector3> GetHomes(ulong uid)
        {
            Dictionary<string, Vector3> positions;
            if (!homes.TryGetValue(uid, out positions)) return null;
            return positions.ToDictionary(p => p.Key, p => p.Value);
        }
        public void Teleport(BasePlayer player, BasePlayer target) => Teleport(player, target.transform.position);
        public void Teleport(BasePlayer player, float x, float y, float z) => Teleport(player, new Vector3(x, y, z));
        public void Teleport(BasePlayer player, Vector3 position)
        {
            if (player.IsDead() && player.IsConnected)
            {
                player.RespawnAt(position, Quaternion.identity);
                return;
            }
            var ret = Interface.Call("CanTeleport", player) as string;
            if (ret != null)
            {
                SendNotify(player, ret);
                return;
            }
            BaseMountable mount = player.GetMounted();
            if (mount != null) mount.DismountPlayer(player);
            if (player.net?.connection != null) player.ClientRPCPlayer(null, player, "StartLoading");
            player.StartSleeping();
            player.MovePosition(position);
            if (player.net?.connection != null) player.ClientRPCPlayer(null, player, "ForcePositionTo", position);
            if (player.net?.connection != null) player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate(false);
            if (player.net?.connection == null) return;
            try
            {
                player.ClearEntityQueue(null);
            }
            catch { }
            player.SendFullSnapshot();
        }
        DynamicConfigFile homesFile = Interface.Oxide.DataFileSystem.GetFile("Teleportation/Homes");
        DynamicConfigFile tpsaveFile = Interface.Oxide.DataFileSystem.GetFile("Teleportation/AdminTpSave");
        public Dictionary<ulong, AutoTPASettings> AutoTPA = new Dictionary<ulong, AutoTPASettings>();

        public class AutoTPASettings
        {
            public bool Enabled;
            public Dictionary<string, ulong> PlayersList = new Dictionary<string, ulong>();
        }


        void LoadData()
        {
            try
            {
                tpsave = tpsaveFile.ReadObject<List<TPList>>();
                if (tpsave == null)
                {
                    PrintError("File AdminTpSave is null! Create new data files");
                    tpsave = new List<TPList>();
                }
                homes = homesFile.ReadObject<Dictionary<ulong, Dictionary<string, Vector3>>>();
                if (homes == null)
                {
                    PrintError("File Homes is null! Create new data files");
                    homes = new Dictionary<ulong, Dictionary<string, Vector3>>();
                }
                AutoTPA = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, AutoTPASettings>>($"Teleportation/AutoTPA");

            }
            catch
            {
                tpsave = new List<TPList>();
                homes = new Dictionary<ulong, Dictionary<string, Vector3>>();
                AutoTPA = new Dictionary<ulong, AutoTPASettings>();
            }
        }
        void SaveData()
        {
            if (tpsave != null) tpsaveFile.WriteObject(tpsave);
            if (homes != null) homesFile.WriteObject(homes);
            if (AutoTPA != null) Interface.Oxide.DataFileSystem.WriteObject($"Teleportation/AutoTPA", AutoTPA);

        }

        private string URLEncode(string input)
        {
            if (input.Contains("#")) input = input.Replace("#", "%23");
            if (input.Contains("$")) input = input.Replace("$", "%24");
            if (input.Contains("+")) input = input.Replace("+", "%2B");
            if (input.Contains("/")) input = input.Replace("/", "%2F");
            if (input.Contains(":")) input = input.Replace(":", "%3A");
            if (input.Contains(";")) input = input.Replace(";", "%3B");
            if (input.Contains("?")) input = input.Replace("?", "%3F");
            if (input.Contains("@")) input = input.Replace("@", "%40");
            return input;
        }

        Dictionary<string, string> Messages = new Dictionary<string, string>()
        {
            {"foundationmissing", "Фундамент не найден!"},
            {"InDuel", "Вы на Дуэли. Телепорт запрещен!"},
            {"InDuelTarget", "Игрок на Дуэли. Телепорт запрещен!"},
            {"foundationmissingR", "Фундамент не найден, местоположение было удалено!"},
            {"playerisyou", "Нельзя отправлять телепорт самому себе!"},
            {"maxhomes", "У вас максимальное кол-во местоположений!"},
            {"homeexist", "Такое местоположение уже существует!"},
            {"homesucces", "Местоположение {0} успешно установлено!"},
            {"sethomeArgsError", "Для установки местоположения используйте /sethome ИМЯ"},
            {"settpArgsError", "Для установки местоположения используйте /tpl add ИМЯ"},
            {"homeArgsError", "Для телепортации на местоположение используйте /home ИМЯ"},
            {"tpArgsError", "Для телепортации на местоположение используйте /tpl ИМЯ"},
            {"tpError", "Запрещено! Вы в очереди на телепортацию"},
            {"homenotexist", "Местоположение с таким названием не найдено!"},
            {"homequeue", "Телепортация на {0} будет через {1}"},
            {"tpwounded", "Вы получили ранение! Телепортация отменена!"},
            {"tphomesuccess", "Вы телепортированы домой!"},
            {"tplsuccess", "Вы успешно телепортированы!"},
            {"tptpsuccess", "Вы телепортированы на указаное место!"},
            {"homesmissing", "У вас нет доступных местоположений!"},
            {"TPLmissing", "Для вас нет доступных местоположений!"},
            {"TPLList", "Доступные точки местоположения:\n{0}"},
            {"removehomeArgsError", "Для удаления местоположения используйте /removehome ИМЯ"},
            {"removetpArgsError", "Для удаления местоположения используйте /tpl remove ИМЯ"},
            {"removehomesuccess", "Местоположение {0} успешно удалено"},
            {"sleepingbagmissing", "Спальный мешок не найден, местоположение удалено!"},
            {"tprArgsError", "Для отправки запроса на телепортация используйте /tpr НИК"},
            {"playermissing", "Игрок не найден"},
            {"PlayerNotFriend", "Игрок не являеться Вашим другом! Телепорт запрещен!"},
            {"tpspecError", "Не правильно введена команда. Используйте: /tpspec НИК"},
            {"playermissingOff", "Игрок не в сети"},
            {"playermissingOrDeath", "Игрок не найден, или он мёртв"},
            {"playerItsYou", "Нельзя следить за самым собой"},
            {" playerItsSpec", "Игрок уже за кем то наблюдает"},
            {"tprrequestsuccess", "Запрос {0} успешно отправлен"},
            {"tprpending", "{0} отправил вам запрос на телепортацию\nЧтобы принять используйте /tpa\nЧтобы отказаться используйте /tpc"},
            {"tpanotexist", "У вас нет активных запросов на телепортацию!"},
            {"tpqueue", "{0} принял ваш запрос на телепортацию\nВы будете телепортированы через {1}"},
            {"tpc", "Телепортация успешно отменена!"}, 
            {"tpctarget", "{0} отменил телепортацию!"},
            {"tpplayersuccess", "Вы успешно телепортировались к {0}"},
            {"tpasuccess", "Вы приняли запрос телепортации от {0}\nОн будет телепортирован через {1}"},
            {"tppendingcanceled", "Запрос телепортации отменён"},
            {"tpcupboard", "Телепортация в зоне действия чужого шкафа запрещена!"},
            {"tpcupboardTarget", "Вы или игрок находитесь в зоне действия чужого шкафа!"},
            {"tphomecupboard", "Телепортация домой в зоне действия чужого шкафа запрещена!"},
            {"tpacupboard", "Принятие телепортации в зоне действия чужого шкафа запрещена!"},
            {"sethomecupboard", "Установка местоположения в зоне действия чужого шкафа запрещена!"},
            {"tpacanceled", "Вы не ответили на запрос."},
            {"tpkd", "Телепортация на перезарядке!\nОсталось {0}"},
            {"tpWoundedTarget", "Игрок ранен. Телепортация отменена!"},
            {"woundedAction", "Вы ранены!"},
            {"coldplayer", "Вам холодно!"},
            {"Radiation", "Вы облучены радиацией!"},
            {"sethomeBlock", "Нельзя использовать /sethome слишком часто, попробуйте позже!"},
            {"foundationowner", "Нельзя использовать /sethome не на своих строениях!"},
            {"foundationownerFC", "Создатель обьекта не являеться вашим соклановцем или другом, /sethome запрещен"},
            {"homeslist", "Доступное количество местоположений: {0}\n{1}"},
            {"tplist", "Ваши сохраненные метоположения:\n{0}"},
            {"PlayerIsOnCargoShip", "Вы не можете телепортироваться на грузовом корабле."},
            {"PlayerIsOnHotAirBalloon", "Вы не можете телепортироваться на воздушном шаре."},
            {"InsideInFoundation", "Вы не можете устанавливать местоположение находясь в фундаменте"},
            {"InsideInFoundationTP", "Телепортация запрещена, местоположение находится в фундаменте"},
            {"TPAPerm", "У Вас нету права использовать эту команду"},
            {"TPAEnabled", "Вы успешно <color=#FDAE37>включили</color> автопринятие запроса на телепорт\n{0}"},
            {"TPADisable", "Вы успешно <color=#FDAE37>отключили</color> автопринятие запроса на телепорт"},
            {"TPAEnabledInfo", "Добавление нового игрока <color=#FDAE37>/atp add Name/SteamID</color>\nУдаление игрока <color=#FDAE37>/atp remove Name</color>\nСписок игроков <color=#FDAE37>/apt list</color>"},
            {"TPAEnabledList", "Список игроков для каких у Вас включен автоматический приём телепорта:\n{0}"},
            {"TPAEListNotFound", "Вы пока еще не добавили не одного игрока в список, используйте <color=#FDAE37>/atp add Name/SteamID</color>"},
            {"TPAEAddError", "Вы не указали игрока, используйте <color=#FDAE37>/atp add Name/SteamID</color>"},
            {"TPARemoveError", "Вы не указали игрока, используйте <color=#FDAE37>/atp remove Name</color>"},
            {"TPARemoveNotFound", "Игрока <color=#FDAE37>{0}</color> нету в списке, используйте <color=#FDAE37>/atp remove Name</color>"},
            {"TPAEAddPlayerNotFound", "Игрок не найден! Попробуйте уточнить <color=#FDAE37>имя</color>\n{0}"},
            {"TPAEAddSuccess", "Игрок <color=#FDAE37>{0}</color> успешно добавлен в список"},
            {"TPAEAddContains", "Игрок <color=#FDAE37>{0}</color> уже добавлен в список"},
            {"TPAERemoveSuccess", "Игрок <color=#FDAE37>{0}</color> успешно удален со списока"},
            {"TPAEAddPlayers", "Найдено <color=#FDAE37>несколько</color> игроков с похожим ником:\n{0}" },
            {"TPASuccess", "Вы <color=#FDAE37>автоматически</color> приняли запрос на телепортацию так как у вас игрок в списке разрешенных."}
        };
    }
}