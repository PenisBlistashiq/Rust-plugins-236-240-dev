using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("XHeliHealth", "perx", "1.0.0")]
    internal class XHeliHealth : RustPlugin
    {
		#region Configuration

        private HealthConfig config;

        private class HealthConfig
        {				
			internal class HeliSetting
            {
                [JsonProperty("ХП корпуса вертолета")]
                public int HousingHealth;
				[JsonProperty("ХП переднего винта")]
                public int BladeHealth;				
				[JsonProperty("ХП заднего винта")]
                public int TailHealth;				
				[JsonProperty("Включить кастомное ХП вертолета")]
                public bool HeliHealth;						
            }			
			
			internal class BradleySetting
            {
				[JsonProperty("ХП танка")]
                public int APCHealth;				
				[JsonProperty("Включить кастомное ХП танка")]
                public bool BradleyHealth;
            } 			
            
			[JsonProperty("Настройка кастомного ХП вертолета")]
            public HeliSetting Heli = new HeliSetting();			
			[JsonProperty("Настройка кастомного ХП танка")]
            public BradleySetting Bradley = new BradleySetting();		

			public static HealthConfig GetNewConfiguration()
            {
                return new HealthConfig
                {
					Heli = new HeliSetting
					{
						HousingHealth = 10000,
						BladeHealth = 900,
						TailHealth = 500,
						HeliHealth = true
					},
					Bradley = new BradleySetting
					{
						APCHealth = 1000,
						BradleyHealth = true
					}
				};
			}			
        }

        protected override void LoadDefaultConfig()
        {
            config = HealthConfig.GetNewConfiguration();

            PrintWarning("Создание начальной конфигурации плагина!!!");
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();

            config = Config.ReadObject<HealthConfig>();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion	
		
		#region Hooks
		
		private void OnServerInitialized()
		{
			PrintWarning("\n-----------------------------\n" +
			"     Author - Monster\n" +
			"     VK - vk.com/idannopol\n" +
			"     Discord - Monster#4837\n" +
			"     Config - v.2469\n" +
			"-----------------------------");
		}
		
		private void OnEntitySpawned(BaseEntity entity)
		{
			if (entity == null || entity.net == null) return;
			
			if (entity is BaseHelicopter)
			{
				if (config.Heli.HeliHealth) Heli(entity as BaseHelicopter);
			}
			
			if (entity is BradleyAPC)
			{
				if (config.Bradley.BradleyHealth) Bradley(entity as BradleyAPC);
			}
		}
		
		private void Heli(BaseHelicopter heli)
		{
			if (heli == null) return;
			
			heli.InitializeHealth(config.Heli.HousingHealth, config.Heli.HousingHealth);
			
			heli.weakspots[0].health = config.Heli.BladeHealth;
			heli.weakspots[1].health = config.Heli.TailHealth;
		}
		
		private void Bradley(BradleyAPC apc)
		{
			if (apc == null) return;
			
			apc.InitializeHealth(config.Bradley.APCHealth, config.Bradley.APCHealth);
		}
		
		#endregion
	}
}