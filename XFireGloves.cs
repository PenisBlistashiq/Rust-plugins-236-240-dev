using UnityEngine;
using Newtonsoft.Json;
using System.Linq;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("XFireGloves", "Monster", "1.0.14")]
	class XFireGloves : RustPlugin
	{
		
		void OnDispenserBonus(ResourceDispenser disp, BasePlayer player, Item item)
		{
			if (player == null) return;
			
			for (int i = 0; i < config.Gloves.Count; i++)
			{
				foreach (var gloves in player.inventory.containerWear.itemList)
				{
					if (gloves.skin == config.Gloves[i].SkinIDGloves)
					{
						if (config.Gloves[i].GatherGloves) item.amount = (int)(item.amount * config.Gloves[i].GatherValue);
						if (config.Gloves[i].RadiationBonus) player.metabolism.radiation_poison.value += config.Gloves[i].RadiationBonusValue;
						
						if (item.info.shortname == "wood" && config.Gloves[i].SmeltWood == false) continue;
						else if (Smelting.ContainsKey(item.info) && config.Gloves[i].SmeltGather) item.info = Smelting[item.info];
						
						if (config.Gloves[i].Remove	) 
							if (RemoveGloves(player, gloves)) break;
					}
				}
			}
		}
		
				
				
		object OnRecycleItem(Recycler recycler, Item item)
        {
			for (int i = 0; i < config.Gloves.Count; i++)
			{
				if (item.info.shortname.Equals("burlap.gloves") && item.skin.Equals(config.Gloves[i].SkinIDGloves))
				{
					foreach (var items in config.Gloves[i].ItemList)
                    {
						if (UnityEngine.Random.Range(0, 100) <= items.ChanceDrop)
						{
							Item itemc = ItemManager.CreateByName(items.Shortname, UnityEngine.Random.Range(items.Min, items.Max), items.SkinID);
							itemc.name = items.Name;
							itemc.text = items.Text;
							itemc.busyTime = -items.UseValue;
							
							recycler.MoveItemToOutput(itemc);
						}
                    }
				
                    item.RemoveFromWorld(); 
				    item.RemoveFromContainer();
				
                    return false;
				}
			}
			
			return null;
        }
        public List<string> SmeltingItemList = new List<string>
        {
            "chicken.raw",
            "humanmeat.raw",
            "bearmeat",
            "deermeat.raw",
            "meat.boar",
            "wolfmeat.raw",
			"horsemeat.raw",
            "hq.metal.ore",
            "metal.ore",
            "sulfur.ore"
        };
		   		 		  						  	   		  		  		  	 				  	   		  	 		
        private class GlovesConfig
        {					
            
			[JsonProperty("Общее")]
            public SettingSetting Setting = new SettingSetting();			
			
			internal class ItemSetting
			{
				[JsonProperty("Шортнейм предмета")] public string Shortname;
                [JsonProperty("Скин предмета")] public ulong SkinID;  				
                [JsonProperty("Имя предмета")] public string Name;
				[JsonProperty("Шанс выпадения [ 100.0 - 100% ]")] public int ChanceDrop;
				[JsonProperty("Текст [ Если это записка ]")] public string Text;
				[JsonProperty("Кол-во юзов [ Если это перчатки ]")] public int UseValue;
				[JsonProperty("Минимальное количество")] public int Min;				
				[JsonProperty("Максимальное количество")] public int Max;
				
				public ItemSetting(string shortname, ulong skinid, string name, int chancedrop, string text, int usevalue, int min, int max)
				{
					Shortname = shortname; SkinID = skinid; Name = name; ChanceDrop = chancedrop; Text = text; UseValue = usevalue; Min = min; Max = max;
				}
			}
			internal class SettingSetting
			{
				[JsonProperty("Использовать разрешение на шанс найти перчатки в ящиках: xfiregloves.glovesloot")] 
                public bool Permission;
			}
			 
			public static GlovesConfig GetNewConfiguration()
            {
                return new GlovesConfig
                {
					Setting = new SettingSetting
					{
						Permission = true
					},
					Gloves = new List<GlovesSetting>
					{
						new GlovesSetting(1742796979, "Огненные перчатки", 1.0f, 1.0f, 0.0f, 0.0f, 25, new List<ItemSetting> { new ItemSetting("wood", 0, "", 50, "", 0, 50, 100) }, false, true, true, true, true, true, true, true, true, true, new Dictionary<string, float>{ ["crate_tools"] = 50.0f, ["crate_normal_2"] = 50.0f }),
						new GlovesSetting(841106268, "Огненные перчатки", 1.0f, 1.0f, 0.0f, 0.0f, 25, new List<ItemSetting> { new ItemSetting("pistol.revolver", 0, "", 50, "", 0, 1, 1) }, false, false, true, true, true, true, true, true, true, true, new Dictionary<string, float>{ ["crate_tools"] = 50.0f, ["crate_normal_2"] = 50.0f })
					}
				};
			}
			[JsonProperty("Список огненных перчаток")] 
            public List<GlovesSetting> Gloves = new List<GlovesSetting>();				 
			
			internal class GlovesSetting
			{
				[JsonProperty("SkinID огненных перчаток")]
                public ulong SkinIDGloves;
				[JsonProperty("Имя огненных перчаток")]
                public string NameGloves;
				[JsonProperty("Рейты добываемых ресурсов в перчатках")]
                public float GatherValue;                
				[JsonProperty("Рейты подбираемых ресурсов в перчатках")]
                public float PickupValue;
				[JsonProperty("Кол-во радиации при подборе ресурсов")]
                public float RadiationPickupValue;					
				[JsonProperty("Кол-во радиации при бонусной добыче")]
                public float RadiationBonusValue;				
				[JsonProperty("Кол-во юзов")]
                public int UseValue;
				[JsonProperty("Список кастомных предметов после переработки")]
                public List<ItemSetting> ItemList;
				
				[JsonProperty("Включить удаление перчаток после N юзов.")]
                public bool Remove;				
				[JsonProperty("Включить переплавку дерева")]
                public bool SmeltWood;		
			    [JsonProperty("Включить переплавку добываемых ресурсов")]
                public bool SmeltGather;			
			    [JsonProperty("Включить переплавку подбираемых ресурсов")] 
                public bool SmeltPickup;			    
				[JsonProperty("Включить рейты добываемых ресурсов")] 
                public bool GatherGloves;			    
				[JsonProperty("Включить рейты подбираемых ресурсов")] 
                public bool PickupGloves;				    
				[JsonProperty("Включить кастомные предметы после переработке огненных перчаток")] 
                public bool RecyclerGloves;					
				[JsonProperty("Включить выпадение перчаток из ящиков с определенным шансом")]  
                public bool CrateGloves;				
				[JsonProperty("Включить накопление радиации при подборе ресурсов.")] 
                public bool RadiationPickup;				
				[JsonProperty("Включить накопление радиации при бонусной добыче.")]
                public bool RadiationBonus;
				
				[JsonProperty("Настройка шанса выпадения из ящиков и бочек. Имя ящика/бочки | Шанс выпадения: 100.0 - 100%")]
                public Dictionary<string, float> Crate;	
				
				public GlovesSetting(ulong s, string n, float gv, float pv, float rpv, float rbv, int uv, List<ItemSetting> il, bool rr, bool sw, bool sg, bool sp, bool gg, bool pg, bool rg, bool cg, bool rp, bool rb, Dictionary<string, float> c)
				{
					SkinIDGloves = s; NameGloves = n; GatherValue = gv; PickupValue = pv; RadiationPickupValue = rpv; RadiationBonusValue = rbv; UseValue = uv; ItemList = il; Remove = rr; SmeltWood = sw; SmeltGather = sg; SmeltPickup = sp; GatherGloves = gg; PickupGloves = pg; RecyclerGloves = rg; CrateGloves = cg; RadiationPickup = rp; RadiationBonus = rb; Crate = c;
				}
			}
        }
		   		 		  						  	   		  		  		  	 				  	   		  	 		
        protected override void LoadConfig()
        {
            base.LoadConfig();
			
			try
			{
				config = Config.ReadObject<GlovesConfig>(); 
			}
			catch
			{
				PrintWarning("Ошибка чтения конфигурации! Создание дефолтной конфигурации!");
				LoadDefaultConfig();
			}
			
			SaveConfig();
        }
		   		 		  						  	   		  		  		  	 				  	   		  	 		
        		
				
		void OnServerInitialized()
        {
			PrintWarning("\n-----------------------------\n" +
			"     Author - Monster\n" +
			"     VK - vk.com/idannopol\n" +
			"     Discord - Monster#4837\n" +
			"     Config - v.762\n" +
			"-----------------------------");
			
			permission.RegisterPermission("xfiregloves.glovesloot", this);
			
            Smelt();
        }
		
		private bool RemoveGloves(BasePlayer player, Item gloves)
		{
			gloves.busyTime += 1;
			
			if (gloves.busyTime >= 0)
			{
				Effect x = new Effect("assets/bundled/prefabs/fx/impacts/slash/cloth/cloth1.prefab", player, 0, new Vector3(), new Vector3());
				
                gloves.RemoveFromWorld(); 
				gloves.RemoveFromContainer();
				
				EffectNetwork.Send(x, player.Connection);
		   		 		  						  	   		  		  		  	 				  	   		  	 		
                return true;				
		    }
			
			return false;
		}
        
        private GlovesConfig config;
		
				
				
		void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
			var player = entity.ToPlayer();
			if (player == null) return;
			
			for (int i = 0; i < config.Gloves.Count; i++)
			{
				foreach (var gloves in player.inventory.containerWear.itemList)
				{
					if (gloves.skin == config.Gloves[i].SkinIDGloves)
					{
						if (config.Gloves[i].GatherGloves) item.amount = (int)(item.amount * config.Gloves[i].GatherValue);
						if (config.Gloves[i].RadiationBonus) player.metabolism.radiation_poison.value += config.Gloves[i].RadiationBonusValue;
						
						if (item.info.shortname == "wood" && config.Gloves[i].SmeltWood == false) continue;
						else if (Smelting.ContainsKey(item.info) && config.Gloves[i].SmeltGather) item.info = Smelting[item.info];
					}
				}
			}
		}
        protected override void SaveConfig() => Config.WriteObject(config);

        
        		
		[ConsoleCommand("gl_give")]
        void GlovesGive(ConsoleSystem.Arg args)
        {
			if (args.Player() != null) return;
			
			BasePlayer player = BasePlayer.FindByID(ulong.Parse(args.Args[0]));
			int number = int.Parse(args.Args[1]);
			
			if (player == null) return;
			
            Item item = ItemManager.CreateByName("burlap.gloves", 1, config.Gloves[number].SkinIDGloves);
            item.name = config.Gloves[number].NameGloves;
			if (config.Gloves[number].Remove) 
				item.busyTime = -config.Gloves[number].UseValue;
		    player.GiveItem(item);
        }
		
		void OnCollectiblePickup(Item item, BasePlayer player)
		{
			if (player == null) return;
			
			for (int i = 0; i < config.Gloves.Count; i++)
			{
				foreach (var gloves in player.inventory.containerWear.itemList)
				{
					if (gloves.skin == config.Gloves[i].SkinIDGloves)
					{			
						if (config.Gloves[i].PickupGloves) item.amount = (int)(item.amount * config.Gloves[i].PickupValue);
						if (config.Gloves[i].RadiationPickup) player.metabolism.radiation_poison.value += config.Gloves[i].RadiationPickupValue;
						
						if (item.info.shortname == "wood" && config.Gloves[i].SmeltWood == false) continue;
						else if (Smelting.ContainsKey(item.info) && config.Gloves[i].SmeltPickup) item.info = Smelting[item.info];
						
						if (config.Gloves[i].Remove) 
							if (RemoveGloves(player, gloves)) break;
					}
				}
			}
		}
		protected override void LoadDefaultConfig() => config = GlovesConfig.GetNewConfiguration();
		
        private Dictionary<ItemDefinition, ItemDefinition> Smelting;
		
				
				
		void OnLootEntity(BasePlayer player, BaseEntity entity)
		{
			if (config.Setting.Permission)
			{
				if (!permission.UserHasPermission(player.UserIDString, "xfiregloves.glovesloot")) return;
			}
			
			if (!(entity is LootContainer) || player == null || entity.OwnerID != 0) return;
			
		    var inventory = entity.GetComponent<LootContainer>().inventory;
			var lootcontainer = entity.GetComponent<LootContainer>();
			
			for (int i = 0; i < config.Gloves.Count; i++)
			{
				foreach(var crate in config.Gloves[i].Crate)
				{
					if (crate.Key == lootcontainer.ShortPrefabName)
					{
						if (UnityEngine.Random.Range(0, 100) <= crate.Value)
						{
							Item item = ItemManager.CreateByName("burlap.gloves", 1, config.Gloves[i].SkinIDGloves);
                            item.name = config.Gloves[i].NameGloves;
							if (config.Gloves[i].Remove) 
								item.busyTime = -config.Gloves[i].UseValue;
							
                            item.MoveToContainer(inventory);
						}
					}
				}
			}
			
			entity.OwnerID = player.userID;
		}
		
		void Smelt()
		{
			Smelting = ItemManager.GetItemDefinitions().Where(i => SmeltingItemList.Contains(i.shortname)).ToDictionary(i => i, i => i.GetComponent<ItemModCookable>()?.becomeOnCooked);
            Smelting.Add(ItemManager.FindItemDefinition("wood"), ItemManager.FindItemDefinition("charcoal"));
		}
		
			}
}
