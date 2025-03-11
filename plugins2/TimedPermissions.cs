using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System;

namespace Oxide.Plugins
{
    [Info("Timed Permissions", "LaserHydra", "1.6.0")]
    [Description("Allows you to grant permissions or groups for a specific time")]
    class TimedPermissions : CovalencePlugin
    {
        private const string AdminPermission = "timedpermissions.use";
        private const string AdvancedAdminPermission = "timedpermissions.advanced";
        
        private static TimedPermissions _plugin;
        private static List<PlayerInformation> _playerInformationCollection = new List<PlayerInformation>();

        private Regex _timeSpanPattern = new Regex(@"(?:(?<days>\d{1,3})d)?(?:(?<hours>\d{1,3})h)?(?:(?<minutes>\d{1,3})m)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Configuration _config;
        
        #region Hooks & Loading

        private void Loaded()
        {
            _plugin = this;

            LoadData(ref _playerInformationCollection);

            if (_playerInformationCollection == null)
            {
                _playerInformationCollection = new List<PlayerInformation>();
                SaveData(_playerInformationCollection);
            }

            _plugin.timer.Repeat(60, 0, () =>
            {
                for (int i = _playerInformationCollection.Count - 1; i >= 0; i--)
                {
                    PlayerInformation playerInformation = _playerInformationCollection[i];
                    playerInformation.Update();
                }
            });
        }

        private void OnUserConnected(IPlayer player)
        {
            PlayerInformation.Get(player.Id)?.EnsureAllAccess();
        }

        private void OnNewSave(string filename)
        {
            LoadConfig(); // Ensure config is loaded at this point

            if (_config.WipeDataOnNewSave)
            {
                string backupFileName;
                ResetAllAccess(out backupFileName);

                PrintWarning($"New save file detected: all groups and permissions revoked and data cleared. Backup created at {backupFileName}.json");
            }
        }

        #endregion

        #region Commands

        [Command("pinfo")]
        private void CmdPlayerInfo(IPlayer player, string cmd, string[] args)
        {
            IPlayer target;

            if (args.Length == 0 || !player.HasPermission(AdminPermission))
                target = player;
            else
                target = FindPlayer(args[0], player);

            if (target == null)
                return;

            var information = PlayerInformation.Get(target.Id);

            if (information == null)
            {
                player.Reply(GetMessage("Player Has No Info", player.Id));
            }
            else
            {
                string msg = GetMessage("Player Info", player.Id);

                msg = msg.Replace("{player}", $"{information.Name} ({information.Id})");
                msg = msg.Replace("{groups}", string.Join(", ", (from g in information.Groups select $"{g.Value} until {g.ExpireDate.ToLongDateString() + " " + g.ExpireDate.ToShortTimeString()} UTC").ToArray()));
                msg = msg.Replace("{permissions}", string.Join(", ", (from p in information.Permissions select $"{p.Value} until {p.ExpireDate.ToLongDateString() + " " + p.ExpireDate.ToShortTimeString()} UTC").ToArray()));

                player.Reply(msg);
            }
        }

        [Command("grantperm"), Permission(AdminPermission)]
        private void CmdGrantPerm(IPlayer player, string cmd, string[] args)
        {
            if (args.Length != 3)
            {
                player.Reply(GetMessage("Syntax : grantperm", player.Id));
                return;
            }

            IPlayer target = FindPlayer(args[0], player);
            TimeSpan duration;

            if (target == null)
                return;

            if (!TryParseTimeSpan(args[2], out duration))
            {
                player.Reply(GetMessage("Invalid Time Format", player.Id));
                return;
            }

            PlayerInformation.GetOrCreate(target).AddPermission(args[1].ToLower(), DateTime.UtcNow + duration);
        }

        [Command("revokeperm"), Permission(AdminPermission)]
        private void CmdRevokePerm(IPlayer player, string cmd, string[] args)
        {
            if (args.Length != 2)
            {
                player.Reply(GetMessage("Syntax : revokeperm", player.Id));
                return;
            }

            IPlayer target = FindPlayer(args[0], player);

            if (target == null)
                return;

            PlayerInformation information = PlayerInformation.Get(target.Id);
            
            if (information == null || !information.Permissions.Any(p => p.Value == args[1].ToLower()))
            {
                player.Reply(GetMessage("User Doesn't Have Permission", player.Id).Replace("{target}", target.Name).Replace("{permission}", args[1].ToLower()));
                return;
            }

            information.RemovePermission(args[1].ToLower());
        }

        [Command("addgroup"), Permission(AdminPermission)]
        private void CmdAddGroup(IPlayer player, string cmd, string[] args)
        {
            if (args.Length != 3)
            {
                player.Reply(GetMessage("Syntax : addgroup", player.Id));
                return;
            }

            IPlayer target = FindPlayer(args[0], player);
            TimeSpan duration;

            if (target == null)
                return;

            if (!TryParseTimeSpan(args[2], out duration))
            {
                player.Reply(GetMessage("Invalid Time Format", player.Id));
                return;
            }

            PlayerInformation.GetOrCreate(target).AddGroup(args[1], DateTime.UtcNow + duration);
        }

        [Command("removegroup"), Permission(AdminPermission)]
        private void CmdRemoveGroup(IPlayer player, string cmd, string[] args)
        {
            if (args.Length != 2)
            {
                player.Reply(GetMessage("Syntax : removegroup", player.Id));
                return;
            }

            IPlayer target = FindPlayer(args[0], player);

            if (target == null)
                return;

            PlayerInformation information = PlayerInformation.Get(target.Id);

            if (information == null || !information.Groups.Any(p => p.Value == args[1].ToLower()))
            {
                player.Reply(GetMessage("User Isn't In Group", player.Id).Replace("{target}", target.Name).Replace("{group}", args[1].ToLower()));
                return;
            }

            information.RemoveGroup(args[1].ToLower());
        }

        [Command("timedpermissions_resetaccess"), Permission(AdvancedAdminPermission)]
        private void CmdResetAccess(IPlayer player, string cmd, string[] args)
        {
            if (args.Length != 1 || !args[0].Equals("yes", StringComparison.InvariantCultureIgnoreCase))
            {
                player.Reply(GetMessage("Syntax : resetaccess", player.Id));
                player.Reply(GetMessage("Reset Access Warning", player.Id));

                return;
            }

            string backupFileName;
            ResetAllAccess(out backupFileName);

            player.Reply(GetMessage("Access Reset Successfully", player.Id).Replace("{filename}", backupFileName));
        }

        #endregion

        #region Helper Methods

        private void ResetAllAccess(out string backupFileName)
        {
            backupFileName = $"{nameof(TimedPermissions)}_Backups/{DateTime.UtcNow.Date:yyyy-MM-dd}_{DateTime.UtcNow:T}";
            SaveData(_playerInformationCollection, backupFileName); // create backup of current data

            foreach (PlayerInformation playerInformation in _playerInformationCollection)
                playerInformation.RemoveAllAccess();

            _playerInformationCollection = new List<PlayerInformation>();
            SaveData(_playerInformationCollection);
        }

        #region Time Helper

        private bool TryParseTimeSpan(string source, out TimeSpan date)
        {
            var match = _timeSpanPattern.Match(source);

            if (!match.Success)
            {
                date = default(TimeSpan);
                return false;
            }

            if (!match.Groups[0].Value.Equals(source))
            {
                date = default(TimeSpan);
                return false;
            }

            Group daysGroup = match.Groups["days"];
            Group hoursGroup = match.Groups["hours"];
            Group minutesGroup = match.Groups["minutes"];

            int days = daysGroup.Success
                ? int.Parse(daysGroup.Value)
                : 0;
            int hours = hoursGroup.Success
                ? int.Parse(hoursGroup.Value)
                : 0;
            int minutes = minutesGroup.Success
                ? int.Parse(minutesGroup.Value)
                : 0;

            if (days + hours + minutes == 0)
            {
                date = default(TimeSpan);
                return false;
            }

            date = new TimeSpan(days, hours, minutes, 0);
            return true;
        }

        #endregion

        #region Finding Helper

        private IPlayer FindPlayer(string nameOrId, IPlayer player)
        {
            if (IsConvertibleTo<ulong>(nameOrId) && nameOrId.StartsWith("7656119") && nameOrId.Length == 17)
            {
                IPlayer result = players.All.ToList().Find(p => p.Id == nameOrId);

                if (result == null)
                    player.Reply($"Could not find player with ID '{nameOrId}'");

                return result;
            }

            List<IPlayer> foundPlayers = new List<IPlayer>();

            foreach (IPlayer current in players.Connected)
            {
                if (string.Equals(current.Name, nameOrId, StringComparison.CurrentCultureIgnoreCase))
                    return current;

                if (current.Name.ToLower().Contains(nameOrId.ToLower()))
                    foundPlayers.Add(current);
            }

            switch (foundPlayers.Count)
            {
                case 0:
                    player.Reply($"Could not find player with name '{nameOrId}'");
                    break;

                case 1:
                    return foundPlayers[0];

                default:
                    string[] names = (from current in foundPlayers select current.Name).ToArray();
                    player.Reply("Multiple matching players found: \n" + string.Join(", ", names));
                    break;
            }

            return null;
        }

        #endregion

        #region Conversion Helper

        private static bool IsConvertibleTo<T>(object obj)
        {
            try
            {
                var temp = (T)Convert.ChangeType(obj, typeof(T));
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Data Helper

        private static void LoadData<T>(ref T data, string filename = null) =>
            data = Interface.Oxide.DataFileSystem.ReadObject<T>(filename ?? nameof(TimedPermissions));

        private static void SaveData<T>(T data, string filename = null) =>
            Interface.Oxide.DataFileSystem.WriteObject(filename ?? nameof(TimedPermissions), data);

        #endregion

        #region Message Wrapper

        public static string GetMessage(string key, string id) => _plugin.lang.GetMessage(key, _plugin, id);

        #endregion

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["No Permission"] = "You don't have permission to use this command.",
                ["Invalid Time Format"] = "Invalid Time Format: Ex: 1d12h30m | d = days, h = hours, m = minutes",
                ["Player Has No Info"] = "There is no info about this player.",
                ["Player Info"] = "Information for <color=#C4FF00>{player}</color>:" + Environment.NewLine +
                                  "<color=#C4FF00>Groups</color>: {groups}" + Environment.NewLine +
                                  "<color=#C4FF00>Permissions</color>: {permissions}",
                ["User Doesn't Have Permission"] = "{target} does not have permission '{permission}'.",
                ["User Isn't In Group"] = "{target} isn't in group '{group}'.",
                ["Reset Access Warning"] = "This command will reset all access data and create a backup. Please confirm by calling the command with 'yes' as parameter",
                ["Access Reset Successfully"] = "All groups and permissions revoked and data cleared. Backup created at {filename}.json",
                ["Syntax : revokeperm"] = "Syntax: revokeperm <player|steamid> <permission>",
                ["Syntax : grantperm"] = "Syntax: removegroup <player|steamid> <group>",
                ["Syntax : removegroup"] = "Syntax: removegroup <player|steamid> <group>",
                ["Syntax : addgroup"] = "Syntax: addgroup <player|steamid> <group> <time Ex: 1d12h30m>",
                ["Syntax : resetaccess"] = "Syntax: timedpermissions_resetaccess [yes]",

            }, this);
        }

        #endregion

        #region Configuration

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();
            SaveConfig();
        }

        protected override void LoadDefaultConfig() => _config = new Configuration();

        protected override void SaveConfig() => Config.WriteObject(_config);

        private class Configuration
        {
            [JsonProperty("Wipe Data on New Save (Limited to Certain Games)")]
            public bool WipeDataOnNewSave { get; private set; } = false;
        }

        #endregion

        #region Data Structures

        private class PlayerInformation
        {
            [JsonProperty("Id")]
            public string Id { get; set; }

            [JsonProperty("Name")]
            public string Name { get; set; }

            [JsonProperty("Permissions")]
            private readonly List<ExpiringAccessValue> _permissions = new List<ExpiringAccessValue>();

            [JsonProperty("Groups")]
            private readonly List<ExpiringAccessValue> _groups = new List<ExpiringAccessValue>();

            [JsonIgnore]
            public ReadOnlyCollection<ExpiringAccessValue> Permissions => _permissions.AsReadOnly();

            [JsonIgnore]
            public ReadOnlyCollection<ExpiringAccessValue> Groups => _groups.AsReadOnly();

            public static PlayerInformation Get(string id) => _playerInformationCollection.FirstOrDefault(p => p.Id == id);

            public static PlayerInformation GetOrCreate(IPlayer player)
            {
                PlayerInformation information = Get(player.Id);

                if (information == null)
                {
                    information = new PlayerInformation(player);

                    _playerInformationCollection.Add(information);
                    SaveData(_playerInformationCollection);
                }

                return information;
            }

            #region Permissions

            public void AddPermission(string permission, DateTime expireDate)
            {
                ExpiringAccessValue existingAccess = _permissions.FirstOrDefault(p => p.Value == permission);

                if (existingAccess != null)
                {
                    Interface.CallHook("OnTimedPermissionExtended", Id, permission, existingAccess.ExpireDate - DateTime.UtcNow);

                    existingAccess.ExpireDate += expireDate - DateTime.UtcNow;

                    _plugin.Puts($"{Name} ({Id}) - Permission time extended: {permission} to {existingAccess.ExpireDate - DateTime.UtcNow}");
                }
                else
                {
                    Interface.CallHook("OnTimedPermissionGranted", Id, permission, expireDate - DateTime.UtcNow);

                    _permissions.Add(new ExpiringAccessValue(permission, expireDate));

                    _plugin.permission.GrantUserPermission(Id, permission, null);

                    _plugin.Puts($"{Name} ({Id}) - Permission granted: {permission} for {expireDate - DateTime.UtcNow}");
                }

                SaveData(_playerInformationCollection);
            }

            public void RemovePermission(string permission)
            {
                ExpiringAccessValue accessValue = _permissions.FirstOrDefault(p => p.Value == permission);

                if (accessValue == null)
                    throw new ArgumentException("Player does not have access to the given permission", nameof(permission));

                _permissions.Remove(accessValue);
                _plugin.permission.RevokeUserPermission(Id, accessValue.Value);

                _plugin.Puts($"{Name} ({Id}) - Permission removed: {accessValue.Value}");

                if (_groups.Count == 0 && _permissions.Count == 0)
                    _playerInformationCollection.Remove(this);

                SaveData(_playerInformationCollection);
            }

            #endregion

            #region Groups

            public void AddGroup(string group, DateTime expireDate)
            {
                ExpiringAccessValue existingAccess = _groups.FirstOrDefault(g => g.Value == group);

                if (existingAccess != null)
                {
                    Interface.CallHook("OnTimedGroupExtended", Id, group, existingAccess.ExpireDate - DateTime.UtcNow);

                    existingAccess.ExpireDate += expireDate - DateTime.UtcNow;

                    _plugin.Puts($"{Name} ({Id}) - Group time extended: {group} to {existingAccess.ExpireDate - DateTime.UtcNow}");
                }
                else
                {
                    Interface.CallHook("OnTimedGroupAdded", Id, group, expireDate - DateTime.UtcNow);

                    _groups.Add(new ExpiringAccessValue(group, expireDate));

                    _plugin.permission.AddUserGroup(Id, group);

                    _plugin.Puts($"{Name} ({Id}) - Added to group: {group} for {expireDate - DateTime.UtcNow}");
                }

                SaveData(_playerInformationCollection);
            }

            public void RemoveGroup(string group)
            {
                var accessValue = _groups.FirstOrDefault(g => g.Value == group);

                if (accessValue == null)
                    throw new ArgumentException("Player does not have access to the given group", nameof(group));

                _groups.Remove(accessValue);
                _plugin.permission.RemoveUserGroup(Id, accessValue.Value);

                _plugin.Puts($"{Name} ({Id}) - Removed from group: {accessValue.Value}");

                if (_groups.Count == 0 && _permissions.Count == 0)
                    _playerInformationCollection.Remove(this);

                SaveData(_playerInformationCollection);
            }

            #endregion

            #region Other

            public void RemoveAllAccess()
            {
                foreach (ExpiringAccessValue permission in _permissions)
                    _plugin.permission.RevokeUserPermission(Id, permission.Value);

                _permissions.Clear();

                foreach (ExpiringAccessValue group in _groups)
                    _plugin.permission.RemoveUserGroup(Id, group.Value);

                _groups.Clear();
            }

            public void EnsureAllAccess()
            {
                foreach (ExpiringAccessValue permission in _permissions)
                    _plugin.permission.GrantUserPermission(Id, permission.Value, null);

                foreach (ExpiringAccessValue group in _groups)
                    _plugin.permission.AddUserGroup(Id, group.Value);
            }

            public void Update()
            {
                foreach (ExpiringAccessValue permission in _permissions.ToList())
                    if (permission.IsExpired)
                        RemovePermission(permission.Value);

                foreach (ExpiringAccessValue group in _groups.ToList())
                    if (group.IsExpired)
                        RemoveGroup(group.Value);
            }

            #endregion

            public override int GetHashCode() => Id.GetHashCode();

            private PlayerInformation(IPlayer player)
            {
                Id = player.Id;
                Name = player.Name;
            }

            [JsonConstructor]
            private PlayerInformation()
            {
            }
        }

        private class ExpiringAccessValue
        {
            [JsonProperty]
            public string Value { get; private set; }

            [JsonProperty]
            public DateTime ExpireDate { get; set; }

            [JsonIgnore]
            public bool IsExpired => DateTime.Compare(DateTime.UtcNow, ExpireDate) > 0;

            public override int GetHashCode() => Value.GetHashCode();

            public ExpiringAccessValue(string value, DateTime expireDate)
            {
                Value = value;
                ExpireDate = expireDate;
            }

            public ExpiringAccessValue()
            {
            }
        }

        #endregion
    }
}