using System.Collections.Generic;
using Newtonsoft.Json;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("XL96Plus", "Monster", "1.0.1")]
    class XL96Plus : RustPlugin
	{		
		#region Configuration

        private L96Config config;

        private class L96Config
        {				
			internal class L96Setting
            {
                [JsonProperty("SkinID улучшенной винтовки")]
                public ulong SkinIDL96;
				[JsonProperty("Имя улучшенной винтовки")]
                public string NameL96;						
            }			
			
			internal class SettingsSetting
            {
                [JsonProperty("Убивать игрока при попадании в голову")]
                public bool KillL96;                
				[JsonProperty("Оставлять 1-хп при попадании в в тело")]
                public bool HealL96;
				[JsonProperty("Включить выпадение улучшенной винтовки из ящиков с определенным шансом")] 
                public bool CrateL96;				
				[JsonProperty("Сбивать регенерацию")] 
                public bool PendingHealth;				
            } 

			internal class Crates
			{
				[JsonProperty("Имя ящика")]
                public string NameCrate = "crate_normal";				
				[JsonProperty("Шанс выпадения")]
                public int ChanceDrop;				
				[JsonProperty("Износ улучшенной винтовки. 60.0 - 100%")]
                public float MConditionL96;				
				[JsonProperty("ХП улучшенной винтовки. 60.0 - 100%")]
                public float ConditionL96;	
			}			
            
			[JsonProperty("Настройка улучшенной винтовки")]
            public L96Setting L96 = new L96Setting();			
			[JsonProperty("Общее")]
            public SettingsSetting Settings = new SettingsSetting();
			[JsonProperty("Настройка шанса выпадения в ящиках")]
            public List<Crates> Crate = new List<Crates>();			

			public static L96Config GetNewConfiguration()
            {
                return new L96Config
                {
					L96 = new L96Setting
					{
						SkinIDL96 = 2132508217,
						NameL96 = "ПУШКА",
					},
					Settings = new SettingsSetting
					{
						KillL96 = true,
						HealL96 = true,
						CrateL96 = false,
						PendingHealth = false
					},
					Crate = new List<Crates>
					{
						new Crates
						{
							NameCrate = "foodbox",
							ChanceDrop = 15,
							MConditionL96 = 0.1f,
							ConditionL96 = 0.1f
						},						
						new Crates
						{
							NameCrate = "crate_normal_2",
							ChanceDrop = 5,
							MConditionL96 = 0.1f,
							ConditionL96 = 0.1f
						}
					}
				};
			}			
        }

        protected override void LoadDefaultConfig()
        {
            config = L96Config.GetNewConfiguration();

            PrintWarning("Создание начальной конфигурации плагина!!!");
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();

            config = Config.ReadObject<L96Config>();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion	

        #region Commands
		
		[ConsoleCommand("w_give")]
        void GlovesGive(ConsoleSystem.Arg args)
        {
			if (args.Player() != null) return;
						
			BasePlayer player = BasePlayer.FindByID(ulong.Parse(args.Args[0]));

			if (player == null) return;
			
            Item item = ItemManager.CreateByName("rifle.l96", 1, config.L96.SkinIDL96);
            item.name = config.L96.NameL96;
			item.maxCondition = float.Parse(args.Args[1]);			
			item.condition = float.Parse(args.Args[2]);
			
            player.GiveItem(item);
        }
			
		#endregion
		
		#region Hooks
		
		private void OnServerInitialized()
		{
			PrintWarning("\n-----------------------------\n" +
			"     Author - Monster\n" +
			"     VK - vk.com/idannopol\n" +
			"     Discord - Monster#4837\n" +
			"     Config - v.414\n" +
			"-----------------------------");
		}
		
		void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
		{
			BasePlayer player = info.InitiatorPlayer;
			Item weapon = player?.GetActiveItem();
			var eplayer = entity as BasePlayer;
			
			if (entity == null || info == null || player == null || eplayer == null || weapon == null) return;
			
			if (player == eplayer) return;
			
			if (weapon.skin == config.L96.SkinIDL96)
			{				
				if (eplayer.health < 5) return;
					
				if (info.isHeadshot)
				{
					if (config.Settings.KillL96)
					{
					    info.damageTypes.Set(info.damageTypes.GetMajorityDamageType(), 1000f);
					}
				}
				else
				{
					if (config.Settings.HealL96)
					{
					    info.damageTypes.Set(info.damageTypes.GetMajorityDamageType(), 0f);
					    eplayer.health = 1;
						
						if (config.Settings.PendingHealth) eplayer.metabolism.pending_health.value = 0;
					}
				}
			}
		}
		
		#endregion
		
		#region L96
		
		private void OnLootSpawn(LootContainer lootContainer)
		{
			if (config.Settings.CrateL96)
		    {
			    for (int i = 0; i < config.Crate.Count; i++)
			    {
				    if (config.Crate[i].NameCrate == lootContainer.ShortPrefabName)
				    {
	                    if (config.Crate[i].NameCrate.Contains(lootContainer.ShortPrefabName))
                        {
                            if (UnityEngine.Random.Range(0, 100) <= config.Crate[i].ChanceDrop)
                            {
                                Item item = ItemManager.CreateByName("rifle.l96", 1, config.L96.SkinIDL96);
                                item.name = config.L96.NameL96;
								item.maxCondition = config.Crate[i].MConditionL96;			
			                    item.condition = config.Crate[i].ConditionL96;
								
                                item.MoveToContainer(lootContainer.inventory);
                            }
                        }
				    }
			    }
		    }
		}
		
		#endregion
	}
}