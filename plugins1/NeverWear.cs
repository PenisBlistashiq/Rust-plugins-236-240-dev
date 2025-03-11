using System.Collections.Generic;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("NeverWear", "k1lly0u / rostov114", "0.2.0")]
    class NeverWear : RustPlugin
    {
        #region Configuration
        private Configuration _config;
        public class Configuration
        {
            public bool useWeapons = false;
            public bool useTools = true;
            public bool useAttire = false;
            public bool useWhiteList = false;
            public List<string> WhitelistedItems = new List<string>();
            public bool useBlackList = false;
            public List<string> BlacklistedItems = new List<string>();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                SaveConfig();
            }
            catch
            {
                PrintError("Error reading config, please check!");

                Unsubscribe(nameof(OnLoseCondition));
            }
        }

        protected override void LoadDefaultConfig()
        {
            _config = new Configuration()
            {
                useWeapons = false,
                useTools = true,
                useAttire = false,
                useWhiteList = false,
                WhitelistedItems = new List<string>()
                {
                    "hatchet",
                    "pickaxe",
                    "rifle.bolt",
                    "rifle.ak"
                },
                useBlackList = false,
                BlacklistedItems = new List<string>()
                {
                    "pickaxe",
                    "hatchet#65535",
                }
            };

            SaveConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }
        #endregion

        #region Oxide Hooks
        private void Loaded()
        {
            permission.RegisterPermission("neverwear.use", this);
            permission.RegisterPermission("neverwear.attire", this);
            permission.RegisterPermission("neverwear.weapons", this);
            permission.RegisterPermission("neverwear.tools", this);
        }

        private void OnLoseCondition(Item item, ref float amount)
        {
            if (item == null || !item.hasCondition || item.info == null)
                return;

            if (_config.useBlackList && (_config.BlacklistedItems.Contains($"{item.info.shortname}#{item.skin}") || _config.BlacklistedItems.Contains(item.info.shortname)))
                return;

            BasePlayer player = GetPlayer(item);
            if (player == null)
                return;

            if ((_config.useWhiteList && (_config.WhitelistedItems.Contains($"{item.info.shortname}#{item.skin}") || _config.WhitelistedItems.Contains(item.info.shortname)) && HasPerm(player, "neverwear.use"))
                || (item.info.category == ItemCategory.Weapon && _config.useWeapons && HasPerm(player, "neverwear.weapons"))
                || (item.info.category == ItemCategory.Attire && _config.useAttire && HasPerm(player, "neverwear.attire"))
                || (item.info.category == ItemCategory.Tool && _config.useTools && HasPerm(player, "neverwear.tools")))
            {
                object result = Interface.CallHook("OnNeverWear", item, amount);
                amount = (result is float) ? (float)result : 0f;
            }
        }
        #endregion

        #region Helpers
        public bool HasPerm(BasePlayer player, string perm)
        {
            return permission.UserHasPermission(player.UserIDString, perm);
        }

        public BasePlayer GetPlayer(Item item)
        {
            BasePlayer player = item.GetOwnerPlayer();
            if (player == null)
            {
                if (!item.info.shortname.Contains("mod"))
                    return null;

                player = item?.GetRootContainer()?.GetOwnerPlayer();
            }

            return (player == null || string.IsNullOrEmpty(player.UserIDString)) ? null : player;
        }
        #endregion
    }
}