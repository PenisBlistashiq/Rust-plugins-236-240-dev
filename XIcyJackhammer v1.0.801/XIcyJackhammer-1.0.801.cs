using Newtonsoft.Json;
using UnityEngine;
using System.Linq;
using Oxide.Core.Plugins;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("XIcyJackhammer", "Sempai#3239", "1.0.801")]
	class XIcyJackhammer : RustPlugin
	{
		
		private void OnLootEntity(BasePlayer player, BaseEntity entity)
		{
			if (config.Settings.CrateJackhammer)
			{
				if (config.Settings.PermissionJackhammer)
					if (!permission.UserHasPermission(player.UserIDString, "xicyjackhammer.jackhammer")) return;
			
			    if (!(entity is LootContainer) || player == null || entity.OwnerID != 0) return;
			
			    var inventory = entity.GetComponent<LootContainer>().inventory;
			    var lootcontainer = entity.GetComponent<LootContainer>();
						
				foreach(var crate in config.Crate)
					if(crate.NameCrate == lootcontainer.ShortPrefabName)
						if (UnityEngine.Random.Range(0, 100) <= crate.ChanceDrop)
						{
							Item item = ItemManager.CreateByName("jackhammer", 1, config.Jackhammer.SkinIDJackhammer);
							item.name = config.Jackhammer.NameJackhammer;
								
							item.MoveToContainer(inventory);
							entity.OwnerID = player.userID;
						}
		   		 		  						  	   		  	  			  	 				  			 		  		  
			}
		}
		
				
		
        private void InitializeLang()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ICE"] = "You have mined <color=#00BFFF>ice</color>!"					
            }, this);
		   		 		  						  	   		  	  			  	 				  			 		  		  
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ICE"] = "Вы добыли <color=#00BFFF>глыбу льда</color>!"						
            }, this, "ru");
        }
		
				
				
		private Item OnItemSplit(Item item, int amount)
        {
			if (StackSizeController) return null;
			
            if (item.skin == config.Ice.SkinIDIce)
            {
                item.amount -= amount;
				
                var Item = ItemManager.Create(item.info, amount, item.skin);
					
                Item.name = item.name;
                Item.skin = item.skin;
                Item.amount = amount;
				item.MarkDirty();
				
                return Item;
            }
			
            return null;
        }
				
		[PluginReference] private Plugin StackSizeController;

        		
				
		[ConsoleCommand("j_give")]
        private void IceGive(ConsoleSystem.Arg args)
        {
			if (args.Player() == null || args.Player().IsAdmin)
			{
				BasePlayer player = BasePlayer.FindByID(ulong.Parse(args.Args[0]));
			
				if (player == null) return;
			
				switch (args.Args[1].ToLower())
				{
					case "jackhammer":
					{
						Item item = ItemManager.CreateByName("jackhammer", int.Parse(args.Args[2]), config.Jackhammer.SkinIDJackhammer);
						item.name = config.Jackhammer.NameJackhammer;
						player.GiveItem(item);
			
						break;
					}				
					case "ice":
					{
						Item item = ItemManager.CreateByName("sticks", int.Parse(args.Args[2]), config.Ice.SkinIDIce);
						item.name = config.Ice.NameIce;
						player.GiveItem(item);
					
						break;
					}
				}
			}
        }
		protected override void LoadDefaultConfig() => config = IceConfig.GetNewConfiguration();
		   		 		  						  	   		  	  			  	 				  			 		  		  
		protected override void LoadConfig()
        { 
            base.LoadConfig();
			
			try
			{
				config = Config.ReadObject<IceConfig>();
			}
			catch
			{
				PrintWarning("Ошибка чтения конфигурации! Создание дефолтной конфигурации!");
				LoadDefaultConfig();
			}
			
			SaveConfig();
        }
		
		private object CanCombineDroppedItem(DroppedItem item, DroppedItem targetItem)
		{
            if (item.GetItem().skin == config.Ice.SkinIDIce)
				if (!targetItem.GetItem().skin.Equals(item.GetItem().skin)) return false;
			
            if (targetItem.GetItem().skin == config.Ice.SkinIDIce)
				if (!item.GetItem().skin.Equals(targetItem.GetItem().skin)) return false; 

            return null;
		}

        private class IceConfig
        {		
			
			internal class JackhammerSetting
            {
                [JsonProperty("SkinID бура")] public ulong SkinIDJackhammer;
				[JsonProperty("Имя бура")] public string NameJackhammer;				
            }					
			[JsonProperty("Настройка шанса выпадения ледяного бура в ящиках")]
            public List<Crates> Crate = new List<Crates>();
			
			internal class Crates
			{
				[JsonProperty("Имя ящика")] public string NameCrate;				
				[JsonProperty("Шанс выпадения")] public float ChanceDrop;	
				
				public Crates(string namecrate, float chancedrop)
				{
					NameCrate = namecrate; ChanceDrop = chancedrop;
				}
			}
			
			internal class IceRecycler
            {
			    [JsonProperty("Shortname предмета")] public string ShortNameItem;
				[JsonProperty("Скин предмета")] public ulong SkinIDItem;	
				[JsonProperty("Кастомное имя предмета")] public string ItemNameItem;				
				[JsonProperty("Шанс выпасть при переработки льда. 100.0 - 100%")] public float ChanceItem;			    
				[JsonProperty("Минимальное количество предмета")] public int AmountMinItem;				
				[JsonProperty("Максимальное количество предмета")] public int AmountMaxItem;	
		   		 		  						  	   		  	  			  	 				  			 		  		  
				public IceRecycler(string shortnameitem, ulong skiniditem, string itemnameitem, float chanceitem, int amountminitem, int amountmaxitem)
				{
					ShortNameItem = shortnameitem; SkinIDItem = skiniditem; ItemNameItem = itemnameitem; ChanceItem = chanceitem; AmountMinItem = amountminitem; AmountMaxItem = amountmaxitem;
				}
            }
			
			internal class WorkSetting
            {				
				[JsonProperty("Включить выпадение ледяного бура из ящиков с определенным шансом")] public bool CrateJackhammer;				
				[JsonProperty("Включить добычу глыб льда только в зимнем биоме")] public bool GatherIce;				
				[JsonProperty("Использовать разрешение на шанс найти ледяной бур в ящиках: xicyjackhammer.jackhammer")] public bool PermissionJackhammer;				
				[JsonProperty("Использовать разрешение на шанс добыть глыбу льда: xicyjackhammer.ice")] public bool PermissionIce;				
				[JsonProperty("Ипользовать сообщение в чат при добыче глыбы льда")] public bool Message;		
			}
			[JsonProperty("Переработка")]
            public List<IceRecycler> Recycler = new List<IceRecycler>();
			internal class IceSetting
            {
                [JsonProperty("SkinID льда")] public ulong SkinIDIce;
				[JsonProperty("Имя льда")] public string NameIce;				
            }			
			[JsonProperty("Добыча льда")]
            public List<IceGather> Gather = new List<IceGather>();			
			
			internal class IceGather
            {
			    [JsonProperty("Ресурс вместе с каким будет выпадать лед")] public string TypeGather;			    
				[JsonProperty("Шанс выпасть льда при бонусной добыче")] public float ChanceGather;			    
				[JsonProperty("Минимальное количество льда")] public int AmountMinGather;				
				[JsonProperty("Максимальное количество льда")] public int AmountMaxGather;	

				public IceGather(string typegather, float chancegather, int amountmingather, int amountmaxgather)
				{
					TypeGather = typegather; ChanceGather = chancegather; AmountMinGather = amountmingather; AmountMaxGather = amountmaxgather;
				}
            }			
			[JsonProperty("Ледяной бур")]
            public JackhammerSetting Jackhammer = new JackhammerSetting();									
			[JsonProperty("Общее")]
            public WorkSetting Settings = new WorkSetting();			
            
			[JsonProperty("Глыба льда")]
            public IceSetting Ice = new IceSetting();			
			
			public static IceConfig GetNewConfiguration()
            {
                return new IceConfig
                {
					Ice = new IceSetting
					{
						SkinIDIce = 2215768109,
						NameIce = "ГЛЫБА ЛЬДА"
					},					
					Jackhammer = new JackhammerSetting
					{
						SkinIDJackhammer = 2215780465,
						NameJackhammer = "ЛЕДЯНОЙ БУР"
					},
					Settings = new WorkSetting
					{
						CrateJackhammer = false,
						GatherIce = false,
						PermissionJackhammer = false,
						PermissionIce = false,
						Message = false
					},
					Gather = new List<IceGather>
					{
						new IceGather("stones", 75.0f, 1, 3),
						new IceGather("sulfur.ore", 75.0f, 1, 3),
						new IceGather("metal.ore", 75.0f, 1, 3)
					},
					Recycler = new List<IceRecycler>
					{
						new IceRecycler("stones", 0, "", 50.0f, 5, 50),
						new IceRecycler("lowgradefuel", 0, "", 50.0f, 5, 50)
					},
                    Crate = new List<Crates>
					{
						new Crates("foodbox", 15.0f),
						new Crates("crate_normal_2", 5.0f)
					}
				};
			}
        }
		
				
				
		private void OnServerInitialized()
        {
			
			permission.RegisterPermission("xicyjackhammer.jackhammer", this);
			permission.RegisterPermission("xicyjackhammer.ice", this);
			
			InitializeLang();
        }
		
		private void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
		{
			Item tools = player?.GetActiveItem();
			
			if (dispenser == null || player == null || item == null || tools == null) return;
			
			if (config.Settings.PermissionIce)
				if (!permission.UserHasPermission(player.UserIDString, "xicyjackhammer.ice")) return;
			
			if (tools.skin == config.Jackhammer.SkinIDJackhammer)
			    for (int i = 0; i < config.Gather.Count(); i++)
			        if (item.info.shortname.Contains(config.Gather[i].TypeGather))
			        {
					    if (UnityEngine.Random.Range(0f, 100f) < config.Gather[i].ChanceGather)
					    {
							if (config.Settings.GatherIce)
							{
								if (TerrainMeta.BiomeMap.GetBiome(player.transform.position, TerrainBiome.ARCTIC) > 0.15f )
									GiveIce(player, Random.Range(config.Gather[i].AmountMinGather, config.Gather[i].AmountMaxGather));
							}
							else
								GiveIce(player, Random.Range(config.Gather[i].AmountMinGather, config.Gather[i].AmountMaxGather));
							
							if (config.Settings.Message) SendReply(player, lang.GetMessage("ICE", this, player.UserIDString));
					    }
			        }
		}
		
				
		
        private IceConfig config;
        protected override void SaveConfig() => Config.WriteObject(config);
		
		object OnRecycleItem(Recycler recycler, Item item)
        {
		    if (item.info.shortname.Equals("sticks") && item.skin.Equals(config.Ice.SkinIDIce))
            {
				recycler.inventory.Take(null, ItemManager.FindItemDefinition("sticks").itemid, 1);
				
				foreach(var i in config.Recycler)
					if (UnityEngine.Random.Range(0f, 100f) < i.ChanceItem)
					{
                        Item itemc = ItemManager.CreateByName(i.ShortNameItem, Random.Range(i.AmountMinItem, i.AmountMaxItem), i.SkinIDItem);
						itemc.name = i.ItemNameItem;
						
                        recycler.MoveItemToOutput(itemc);
					}
				
                return false;
            }
				
			return null;
        }
		
		private void GiveIce(BasePlayer player, int amount)
		{
			Item items = ItemManager.CreateByName("sticks", amount, config.Ice.SkinIDIce);
            items.name = config.Ice.NameIce;
            player.GiveItem(items);
		}

        	}
}
