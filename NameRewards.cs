using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("NameRewards", "Skuli", "1.0.1", ResourceId = 0)]
    [Description("Adds players to a group based on phrases in their name")]

    class NameRewards : CovalencePlugin
    {
        ConfigData config;

        class ConfigData
        {
            public Group group;
            public Permission permission;
            public class Group
            {
                public string Name { get; set; }
                public string[] PlayerNick { get; set; }
            }
            public class Permission
            {
                public string Name { get; set; }
                public string[] PlayerNick { get; set; }
            }

        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(new ConfigData
            {
                group = new ConfigData.Group 
                {
                    Name = "Wounded Rust",
                    PlayerNick = new[] { "Oxide", "Example" }
                },
                permission = new ConfigData.Permission
                {
                    Name = "vip",
                    PlayerNick = new[] { "Oxide", "Example" }
                }

            }, true);
        }

        void Init()
        {
            config = Config.ReadObject<ConfigData>();
            if (!permission.GroupExists(config.group.Name))
                permission.CreateGroup(config.group.Name, config.group.Name, 0);
            if (!permission.PermissionExists(config.permission.Name))
                permission.RegisterPermission(config.group.Name, this);
        }

        void OnUserConnected(IPlayer player)
        {
            foreach (var phrase in config.group.PlayerNick)
            {
                if (permission.UserHasGroup(player.Id, config.group.Name)) break;
                if (player.Name.ToLower().Contains(phrase.ToLower()))
                {
                    permission.AddUserGroup(player.Id, config.group.Name);
                    break;
                }
                permission.RemoveUserGroup(player.Id, config.group.Name);
            }
            foreach (var phrase in config.permission.PlayerNick)
            {
                if (permission.UserHasPermission(player.Id, config.permission.Name)) break;
                if (player.Name.ToLower().Contains(phrase.ToLower()))
                {
                    permission.AddUserGroup(player.Id, config.permission.Name);
                    break;
                }
                permission.RemoveUserGroup(player.Id, config.permission.Name);
            }
        }
    }
}