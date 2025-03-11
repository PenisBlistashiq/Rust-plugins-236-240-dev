using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("XHorsePlus", "SkuliDropek", "1.0.1")]
    class XHorsePlus : RustPlugin
    {	
	    #region Configuration

        private HorseConfig config;

        private class HorseConfig
        {								
			internal class HorseSetting
            {				    
				[JsonProperty("SkinID: 2236993578 - 1, 2236994498 - 2")] 
                public ulong HorseSkinID;				
				[JsonProperty("Имя")] 
                public string HorseName;			
            }			
			
			internal class WorkSetting
            {				    
				[JsonProperty("Количество дополнительных мест. True - 1, False - 2")] 
                public bool ChairValue;				
				[JsonProperty("Отрисовывать стул")] 
                public bool Chair;			
            }			
            						
			[JsonProperty("Настройки лошади")]
            public HorseSetting Horse = new HorseSetting();			
			[JsonProperty("Общее")]
            public WorkSetting Settings = new WorkSetting();			
			
			public static HorseConfig GetNewConfiguration()
            {
                return new HorseConfig
                {
					Settings = new WorkSetting
					{
						ChairValue = false,
						Chair = true
					},
					Horse = new HorseSetting
					{
						HorseSkinID = 2236994498,
						HorseName = "ЛОШАДКА"
					}
				};
			}
        }

        protected override void LoadDefaultConfig()
        {
            config = HorseConfig.GetNewConfiguration();

            PrintWarning("Создание начальной конфигурации плагина!!!");
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();

            config = Config.ReadObject<HorseConfig>();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion
		
		#region Commands
		
		[ConsoleCommand("h_give")]
        void cmdConsoleCommand(ConsoleSystem.Arg args)
        {
			if (args.Player() != null) return;
			
			BasePlayer player = BasePlayer.FindByID(ulong.Parse(args.Args[0]));
			
			if (player == null) return;
			
			Item item = ItemManager.CreateByName("woodcross", int.Parse(args.Args[1]), config.Horse.HorseSkinID);
            item.name = config.Horse.HorseName;
			
            player.GiveItem(item);
        }

        #endregion	
	
        #region Hooks
		
		private void OnServerInitialized()
		{		
			PrintWarning("\n-----------------------------\n" +
			"     Author - SkuliDropek\n" +
			"     VK - vk.com/idannopol\n" +
			"    Discord - Skuli Dropek#4816 - KINGSkuliDropek#4837\n" +
			"     Config - v.1459\n" +
			"-----------------------------");
		}
		
        private void OnEntitySpawned(RidableHorse horse)
        {
            if (horse.mountPoints.Count < 2 && horse.ShortPrefabName == "testridablehorse")
            {
				if (config.Settings.ChairValue)
				{
					horse.mountPoints.Add(SpawnPassenger(horse, new Vector3(0.02f, 1.0f, -0.5f), new Vector3(0, 0, 0)));
				}
				else
				{
					horse.mountPoints.Add(SpawnPassenger(horse, new Vector3(0.625f, 0.5f, -0.35f), new Vector3(0, 90, 0)));
                    horse.mountPoints.Add(SpawnPassenger(horse, new Vector3(-0.625f, 0.5f, -0.35f), new Vector3(0, 270, 0)));
				}

                if (config.Settings.Chair)
				{
                    if (config.Settings.ChairValue)
				    {
				    	ChairOne(horse, new Vector3(0.02f, 1.0f, -0.5f), new Vector3(0, 0, 0));
				    }
				    else
				    {
					    ChairOne(horse, new Vector3(0.625f, 0.5f, -0.35f), new Vector3(0, 90, 0));
		                ChairTwo(horse, new Vector3(-0.625f, 0.5f, -0.35f), new Vector3(0, 270, 0));
					}
				}
            }
        }
		
		private void OnEntityBuilt(Planner plan, GameObject go)
        {
            SpawnEntity(go.ToBaseEntity());
        }
		
        #endregion

        #region Entity
		
		private void SpawnEntity(BaseEntity entity)
        {
            if (entity == null) return;
			
            if (entity.skinID == config.Horse.HorseSkinID)
			{
				var horse = GameManager.server.CreateEntity("assets/rust.ai/nextai/testridablehorse.prefab", entity.transform.position, entity.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, 0f)));
                                
                horse.Spawn(); 						
				NextTick(() => { entity.Kill(); });
			}			
        }

        void ChairOne(RidableHorse horse, Vector3 position, Vector3 rotation)
        {
            BaseEntity entity = GameManager.server.CreateEntity("assets/prefabs/vehicle/seats/passengerchair.prefab");
			
            if (entity == null) return;
			
            entity.transform.localPosition = position;
			entity.transform.localRotation = Quaternion.Euler(rotation);

			entity.SetParent(horse);
			entity.Spawn();
        }        
		
		void ChairTwo(RidableHorse horse, Vector3 position, Vector3 rotation)
        {
            BaseEntity entity = GameManager.server.CreateEntity("assets/prefabs/vehicle/seats/passengerchair.prefab");
			
            if (entity == null) return;
			
            entity.transform.localPosition = position;
			entity.transform.localRotation = Quaternion.Euler(rotation);

			entity.SetParent(horse);
			entity.Spawn();
        }            

        RidableHorse.MountPointInfo SpawnPassenger(RidableHorse horse, Vector3 position, Vector3 rotation)
        {
            return new RidableHorse.MountPointInfo
            {
				pos = position,
                rot = rotation,
                prefab = horse.mountPoints[0].prefab,
                mountable = horse.mountPoints[0].mountable,
            };
        }
			
        #endregion
    }
}