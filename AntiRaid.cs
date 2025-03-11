using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("AntiRaid", "walkinrey", "1.0.8")]
    public class AntiRaid : RustPlugin
    {
        private const string _permission = "antiraid.use";

        #region Конфиг

        private Configuration _config;

        private class Configuration
        {
            [JsonProperty("Сообщение если у игрока есть антирейд")] 
            public string msgAntiRaid = "У этого игрока активен антирейд, его невозможно зарейдить.";

            [JsonProperty("Белый список объектов, на которые будет действовать антирейд")] 
            public string[] whiteList = {"wall.frame.cell", "wall.window.glass.reinforced", "wall.window.bars.toptier", "floor.grill", "floor.triangle.grill", "gates.external.high.stone", "wall.external.high.stone", "gates.external.high.wood", "wall.external.high"};
            
            [JsonProperty("Черный список объектов, на которые не будет действовать антирейд")] 
            public string[] blackList = {"wall.frame.cell.gate"};
        }

        protected override void LoadConfig()
		{
			base.LoadConfig();

			try
			{
				_config = Config.ReadObject<Configuration>();
			}
			catch
			{
				LoadDefaultConfig();
			}

			Config.WriteObject(_config, true);
        }

        protected override void LoadDefaultConfig() => _config = new Configuration();

        #endregion

        #region Хуки

        private void Loaded() => permission.RegisterPermission(_permission, this);

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if(entity == null || info == null || _config.blackList.Contains(entity?.ShortPrefabName) || info?.InitiatorPlayer == null) return null;

            if(entity is BuildingBlock || entity is Door || _config.whiteList.Contains(entity.ShortPrefabName))
            {
                if(entity.OwnerID != info.InitiatorPlayer.userID && permission.UserHasPermission(entity.OwnerID.ToString(), _permission))
                {
                    info.InitiatorPlayer.ChatMessage(_config.msgAntiRaid);
                    return false;
                }
            } 
            
            return null;
        }

        #endregion
    }
}