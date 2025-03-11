using System; 
using System.Text; 
using System.Collections.Generic; 
using System.Linq; 
using UnityEngine;  

namespace Oxide.Plugins 
{ 
    [Info("StacksExtended", "Fujikura", "1.0.2", ResourceId = 35)] 
	
	class StacksExtended : RustPlugin 
	{ 
	
		bool Changed = false; 
		bool _loaded = false;  
	
		List<string> itemCategories = new List<string> (); 
		List<object> itemStackExcludes = new List<object>(); 
		Dictionary<string, List<string>> registeredPermissions = new Dictionary<string, List<string>> (); 
		Dictionary<string,object> containerStacks = new Dictionary<string,object>(); 
		Dictionary<string,object> containerVIP = new Dictionary<string,object>(); 
	
		bool clearOnReboot; 
		int commandsAuthLevel; 
		bool limitPlayerInventory;
		int playerInventoryStacklimit;  
		
		static List<object> defaultItemExcludes() 
		{
			var dp = new List<object>(); 
				dp.Add("water"); 
				dp.Add("water.salt"); 
				dp.Add("blood"); 
				dp.Add("blueprintbase"); 
				dp.Add("coal"); 
				dp.Add("flare"); 
				dp.Add("ammo.rocket.smoke"); 
				dp.Add("generator.wind.scrap"); 
				dp.Add("battery.small"); 
				dp.Add("mining.pumpjack"); 
				dp.Add("building.planner"); 
				dp.Add("door.key"); 
				dp.Add("map"); 
				dp.Add("note"); 
			return dp; 
		}  
		
		object GetConfig(string menu, string datavalue, object defaultValue) 
		{
			var data = Config[menu] as Dictionary<string, object>; 
			
			if (data == null) 
			{
				data = new Dictionary<string, object>(); 
				Config[menu] = data; 
				Changed = true; 
			} 
			
			object value; 
			
			if (!data.TryGetValue(datavalue, out value)) 
			{
				value = defaultValue; 
				data[datavalue] = value; 
				Changed = true; 
			} 
			return value; 
		}  
		void LoadVariables() 
		{
			itemStackExcludes = (List<object>)GetConfig("Settings", "ExcludedItems", defaultItemExcludes()); 
			
			containerStacks = (Dictionary<string, object>)GetConfig("Storages", "Stack", new Dictionary<string, object>()); 
			containerVIP = (Dictionary<string, object>)GetConfig("Storages", "VIP", new Dictionary<string, object>()); 
			clearOnReboot = Convert.ToBoolean(GetConfig("Settings", "clearOnReboot", false)); 
			commandsAuthLevel = Convert.ToInt32(GetConfig("Settings", "commandsAuthLevel", 2)); 
			limitPlayerInventory = Convert.ToBoolean(GetConfig("Settings", "limitPlayerInventory", false)); playerInventoryStacklimit =  Convert.ToInt32(GetConfig("Settings", "playerInventoryStacklimit", 0));  
			
			if (!Changed) 
				return; 
				
			SaveConfig(); 
			Changed = false; 
		}  
		protected override void LoadDefaultConfig() 
		{
			Config.Clear(); 
			LoadVariables(); 
		}  
		void Init() 
		{ 
			LoadVariables(); 
		}  
		void OnTerrainInitialized() 
		{
			_loaded = true; 
		}  
		void OnServerInitialized() 
		{
			var storages = Resources.FindObjectsOfTypeAll<StorageContainer>().Where(c => !c.isActiveAndEnabled && !c.GetComponent<LootContainer>()).Cast<BaseEntity>().Where(b => !b.ShortPrefabName.Contains("_static")).ToList(); 
			
			if (containerVIP == null || containerVIP.Count == 0) 
				CreateContainerVIP(storages); 
			if (containerStacks == null || containerStacks.Count == 0) 
				CreateContainerStacks(storages); 
			if(Config.Get("StackLimits") != null) 
			{
				if (clearOnReboot && _loaded) 
				{
					CreateContainerStacks(storages); 
					Config["Storages", "Stack"] = containerStacks; 
					StackDefaults(); 
				} 
				else StackLoad(); 
			} 
			else 
			{
				StackDefaults(); 
			} 
			
			CreatePermissions(); 
			UpdateContainerStacks(); 
			UpdateQuarryVIP(); 
			
			if (limitPlayerInventory && playerInventoryStacklimit >= 0) 
			{
				foreach(var player in BasePlayer.activePlayerList) UpdatePlayer(player); 
				foreach(var player in BasePlayer.sleepingPlayerList) UpdatePlayer(player); 
			} 
			
			storages.Clear();
		}  
		void OnEntityBuilt(Planner planner, GameObject obj) 
		{
			if (planner == null || planner.GetOwnerPlayer() == null || obj.GetComponent<BaseEntity>() == null || obj.GetComponent<BaseEntity>().OwnerID == 0) 
				return; 
			if (obj.GetComponent<BaseEntity>() is MiningQuarry) 
			{
				OnQuarryBuilt(planner, obj); 
				return; 
			} 
			
			BaseEntity entity = obj.GetComponent<BaseEntity>(); 
			BasePlayer player = planner.GetOwnerPlayer(); 
			
			if (player == null) 
				return; 
				
			var name = entity.ShortPrefabName.Replace(".deployed","").Replace("_deployed",""); 
			
			object containerLimit; 
			
			if (containerStacks.TryGetValue(name, out containerLimit)) 
			{
				if (entity.GetComponent<StorageContainer>().inventory.maxStackSize != (int)containerLimit) 
				{
					entity.GetComponent<StorageContainer>().inventory.maxStackSize = (int)containerLimit; 
					entity.SendNetworkUpdate(); 
				} 
				if (entity.OwnerID != 0) 
				{
					object containerPerms; 
				
					if (containerVIP.TryGetValue(name, out containerPerms)) 
					{
						var perms = (Dictionary<string, object>)containerPerms; 
						
						if (!(bool)perms["Enabled"]) 
							return; 
							
						foreach (var perm in ((Dictionary<string, object>)perms["Permissions"]).Reverse()) 
						{
							if ((int)containerLimit > 0 && (int)perm.Value >= 0 && (int)perm.Value > (int)containerLimit && permission.UserHasPermission(entity.OwnerID.ToString(), this.Title.ToLower()+"."+perm.Key)) 
							{
								entity.GetComponent<StorageContainer>().inventory.maxStackSize = (int)perm.Value; 
								entity.SendNetworkUpdate(); 		
							} 		
						} 		
					} 
				} 
			} 
		}  
		void OnQuarryBuilt(Planner planner, GameObject obj) 
		{
			BaseEntity entity = obj.GetComponent<BaseEntity>(); 
			BasePlayer player = planner.GetOwnerPlayer(); 
			
			if (player == null) 
				return; 
				
			var hopper = (entity as MiningQuarry).hopperPrefab.instance; 
			
			object hopperLimit; 
			
			if (containerStacks.TryGetValue(hopper.ShortPrefabName, out hopperLimit)) 
			{
				if (hopper.GetComponent<StorageContainer>().inventory.maxStackSize != (int)hopperLimit) 
				{
					hopper.GetComponent<StorageContainer>().inventory.maxStackSize = (int)hopperLimit; 
					hopper.SendNetworkUpdate(); 
				} 
			} 
			
			object hopperPerms; 
			
			if (containerVIP.TryGetValue(hopper.ShortPrefabName, out hopperPerms)) 
			{
				var perms = (Dictionary<string, object>)hopperPerms; 
				
				if (!(bool)perms["Enabled"]) 
					return; 
					
				foreach (var perm in ((Dictionary<string, object>)perms["Permissions"]).Reverse()) 
				{
					if ((int)hopperLimit > 0 && (int)perm.Value >= 0 && (int)perm.Value > (int)hopperLimit && permission.UserHasPermission(entity.OwnerID.ToString(), this.Title.ToLower()+"."+perm.Key)) 
					{
						hopper.GetComponent<StorageContainer>().inventory.maxStackSize = (int)perm.Value; 
						hopper.SendNetworkUpdate(); 
					} 
				} 
			} 
			
			var fuelstorage = (entity as MiningQuarry).fuelStoragePrefab.instance; 
			
			object fuelstorageLimit; 
			
			if (containerStacks.TryGetValue(fuelstorage.ShortPrefabName, out fuelstorageLimit)) 
			{
				if (fuelstorage.GetComponent<StorageContainer>().inventory.maxStackSize != (int)fuelstorageLimit) 
				{
					fuelstorage.GetComponent<StorageContainer>().inventory.maxStackSize = (int)fuelstorageLimit; 
					fuelstorage.SendNetworkUpdate(); 
				} 
			} 
			
			object fuelstoragePerms; 
			
			if (containerVIP.TryGetValue(fuelstorage.ShortPrefabName, out fuelstoragePerms)) 
			{
				var perms = (Dictionary<string, object>)fuelstoragePerms; 
				
				if (!(bool)perms["Enabled"]) 
					return; 
					
				foreach (var perm in ((Dictionary<string, object>)perms["Permissions"]).Reverse()) 
				{
					if ((int)fuelstorageLimit > 0 && (int)perm.Value >= 0 && (int)perm.Value > (int)fuelstorageLimit && permission.UserHasPermission(entity.OwnerID.ToString(), this.Title.ToLower()+"."+perm.Key)) 
					{
						fuelstorage.GetComponent<StorageContainer>().inventory.maxStackSize = (int)perm.Value; 
						fuelstorage.SendNetworkUpdate(); 		
					}
				} 		
			} 
		} 
		void UpdatePlayer(BasePlayer player) 
		{
			player.inventory.containerMain.maxStackSize = (int)playerInventoryStacklimit; 
			player.inventory.containerBelt.maxStackSize = (int)playerInventoryStacklimit; 
			player.inventory.SendSnapshot();
		}  
		void OnPlayerRespawned(BasePlayer player) 
		{
			if (player != null && limitPlayerInventory && playerInventoryStacklimit >= 0) 
				UpdatePlayer(player); 
		}  
		void OnPlayerInit(BasePlayer player) 
		{
			if (player != null && limitPlayerInventory && playerInventoryStacklimit >= 0) 
				UpdatePlayer(player); 
		}  
		void CreateContainerVIP(List<BaseEntity> storages) 
		{
			containerVIP = new Dictionary<string, object>(); 
			
			foreach (var storage in storages) 
			{
				var name = storage.ShortPrefabName.Replace(".deployed","").Replace("_deployed",""); 
				
				if (containerVIP.ContainsKey(name)) 
					continue; 
					
				var dp = new Dictionary<string, object>(); 
					dp.Add("Enabled", false); 
					dp.Add("Permissions", new Dictionary<string, object>() {{"vip1",0}, {"vip2",0}, {"vip3",0}} ); 
					
				containerVIP.Add(name, dp); 
			} 
			
			Config["Storages", "VIP"] = containerVIP; 
			SaveConfig(); 
		}  
		void CreateContainerStacks(List<BaseEntity> storages) 
		{
			containerStacks = new Dictionary<string, object>(); 
			
			foreach (var storage in storages) 
			{
				var name = storage.ShortPrefabName.Replace(".deployed","").Replace("_deployed",""); 
				
				if (containerStacks.ContainsKey(name)) 
					continue; 
					
				containerStacks.Add(name, storage.GetComponent<StorageContainer>().maxStackSize); 
			} 
			
			Config["Storages", "Stack"] = containerStacks; 
			SaveConfig(); 
		}  
		void CreatePermissions() 
		{
			foreach (var permSet in containerVIP) 
			{
				var perms = (Dictionary<string, object>)permSet.Value; 
				
				if ((bool)perms["Enabled"]) 
				{
					foreach (var perm in ((Dictionary<string, object>)perms["Permissions"])) 
					{
						if (!registeredPermissions.ContainsKey(this.Title.ToLower()+"."+perm.Key)) 
							registeredPermissions.Add(this.Title.ToLower()+"."+perm.Key, new List<string>()); 
						if (!registeredPermissions[this.Title.ToLower()+"."+perm.Key].Contains(permSet.Key)) 
							registeredPermissions[this.Title.ToLower()+"."+perm.Key].Add(permSet.Key); 
						if (!permission.PermissionExists(this.Title.ToLower()+"."+perm.Key)) 
							permission.RegisterPermission(this.Title.ToLower()+"."+perm.Key, this); 
					} 		
				} 		
			} 		
		}  
		
		Dictionary<string, int> UpdateContainerStacks() 
		{
			var entities = BaseNetworkable.serverEntities.Where(p => (p as BaseEntity).GetComponent<StorageContainer>() != null && (p as BaseEntity).GetComponent<LootContainer>() == null).Cast<BaseEntity>().ToList(); 
			var counter = 0; 
			var vipcounter = 0; 
			var dp = new Dictionary<string, int>(); 
			
			foreach (var entity in entities) 
			{
				var name = entity.ShortPrefabName.Replace(".deployed","").Replace("_deployed",""); 
				
				object containerLimit; 
				
				if (containerStacks.TryGetValue(name, out containerLimit)) 
				{
					if (entity.GetComponent<StorageContainer>().inventory.maxStackSize != (int)containerLimit) 
					{
						entity.GetComponent<StorageContainer>().inventory.maxStackSize = (int)containerLimit; 
						counter++; 
						entity.SendNetworkUpdate(); 
					} 
					if (entity.OwnerID != 0) 
					{
						object containerPerms; 
						
						if (containerVIP.TryGetValue(name, out containerPerms)) 
						{
							var perms = (Dictionary<string, object>)containerPerms; 
							
							if ((bool)perms["Enabled"]) 
							{
								foreach (var perm in ((Dictionary<string, object>)perms["Permissions"]).Reverse()) 
								{
									if ((int)containerLimit > 0 && (int)perm.Value >= 0 && (int)perm.Value > (int)containerLimit && permission.UserHasPermission(entity.OwnerID.ToString(), this.Title.ToLower()+"."+perm.Key)) 
									{
										entity.GetComponent<StorageContainer>().inventory.maxStackSize = (int)perm.Value; 
										vipcounter++; 
										entity.SendNetworkUpdate(); 			
									} 			
								} 			
							} 
						} 			
					} 				
				} 			
			} 
			
			dp.Add("counter", counter); 
			dp.Add("vipcounter", vipcounter); 
			return dp; 
		}  
		
		Dictionary<string, int> UpdateQuarryVIP() 
		{
			var entities = BaseNetworkable.serverEntities.Where(p => (p as BaseEntity) is MiningQuarry).Cast<BaseEntity>().ToList(); 
			var hoppercounter = 0; 
			var fuelstoragecounter = 0; 
			var dp = new Dictionary<string, int>(); 
			
			foreach (var entity in entities) 
			{
				if (entity.OwnerID == 0) 
					continue; 
					
				var hopper = (entity as MiningQuarry).hopperPrefab.instance; 
				
				object hopperPerms; 
				
				if (containerVIP.TryGetValue(hopper.ShortPrefabName, out hopperPerms)) 
				{
					var perms = (Dictionary<string, object>)hopperPerms; 
					
					if ((bool)perms["Enabled"]) 
					{
						foreach (var perm in ((Dictionary<string, object>)perms["Permissions"]).Reverse()) 
						{
							object containerLimit; 
							
							containerStacks.TryGetValue(hopper.ShortPrefabName, out containerLimit); 
							
							if ((int)containerLimit > 0 && (int)perm.Value >= 0 && (int)perm.Value > (int)containerLimit && permission.UserHasPermission(entity.OwnerID.ToString(), this.Title.ToLower()+"."+perm.Key)) 
							{
								hopper.GetComponent<StorageContainer>().inventory.maxStackSize = (int)perm.Value; 
								hoppercounter++; 
								hopper.SendNetworkUpdate(); 
							} 
						} 
					} 
				} 
				
				var fuelstorage = (entity as MiningQuarry).fuelStoragePrefab.instance; 
				
				object fuelstoragePerms; 
				
				if (containerVIP.TryGetValue(fuelstorage.ShortPrefabName, out fuelstoragePerms)) 
				{
					var perms = (Dictionary<string, object>)fuelstoragePerms; 
					
					if ((bool)perms["Enabled"]) 
					{
						foreach (var perm in ((Dictionary<string, object>)perms["Permissions"]).Reverse()) 
						{
							object containerLimit; 
							
							containerStacks.TryGetValue(fuelstorage.ShortPrefabName, out containerLimit); 
							
							if ((int)containerLimit > 0 && (int)perm.Value >= 0 && (int)perm.Value > (int)containerLimit && permission.UserHasPermission(entity.OwnerID.ToString(), this.Title.ToLower()+"."+perm.Key)) 
							{
								fuelstorage.GetComponent<StorageContainer>().inventory.maxStackSize = (int)perm.Value; 
								fuelstoragecounter++;
								fuelstorage.SendNetworkUpdate(); 				
							}				
						}
					} 
				} 
			} 
			
			dp.Add("hoppercounter", hoppercounter); 
			dp.Add("fuelstoragecounter", fuelstoragecounter); 
			return dp; 
		}  
		void StackDefaults() 
		{			
			NextTick(() => 
			{
				var itemList = ItemManager.itemList;
				
				if (itemList == null || itemList.Count == 0) 
				{
					NextTick(StackDefaults); 
					return; 
				} 
				if (clearOnReboot && _loaded) 
				{
					clearOnReboot = false; 
					Config["Settings", "clearOnReboot"] = false; 
					Config.Save(); 
					Puts($"Forced stacksizes reset..."); 
				} 
				int i = 0; 
				
				foreach (var item in itemList) 
				{
					if (item.condition.enabled && item.condition.max > 0 && item.GetComponent<ItemModDeployable>() == null) 
						continue; 
					if (itemStackExcludes.Contains(item.shortname)) 
						continue; 
						
					Config["StackLimits", item.shortname] = item.stackable; 
					
					if (!itemCategories.Contains(item.category.ToString().ToLower())) 
						itemCategories.Add(item.category.ToString().ToLower()); 
					
					i++; 
				} 
				
				Config.Save(); 
				Puts($"Created stacksize file with '{i}' items"); 
			}); 
		}  
		void StackLoad() 
		{
			bool dirty = false; 
			bool changed = false; 
			
			var itemList = ItemManager.itemList; 
			
			if (itemList == null || itemList.Count == 0) 
			{
				NextTick(StackDefaults); 
				return; 
			} 
			int c = 0; 
			
			foreach (var item in itemList) 
			{
				if (itemStackExcludes.Contains(item.shortname)) 
					continue; 
				if (item.condition.max > 0 && item.GetComponent<ItemModDeployable>() == null) 
					continue; 
				if (Config["StackLimits", item.shortname] == null) 
				{
					Config["StackLimits", item.shortname] = item.stackable; 
					dirty = true; 
				} 
				if (item.stackable != (int)Config["StackLimits", item.shortname]) 
				{
					changed = true; 
					c++; 
				} 
				
				item.stackable = (int)Config["StackLimits", item.shortname]; 
				
				if (item.GetComponent<ItemModDeployable>() != null) 
					item.condition.enabled = false; 
				if (!itemCategories.Contains(item.category.ToString().ToLower())) 
					itemCategories.Add(item.category.ToString().ToLower()); 
			} 
			
			if (changed && !_loaded) 
				Puts($"Changed '{c}' stacks with new values"); 
			if (!changed && !dirty) 
				Puts("No stacksize changed"); 
			if (dirty) 
				Puts("Updated stacksize file with new items"); 
			if (dirty) 
				Config.Save(); 
		}  
		[ConsoleCommand("se.reload")] 
		void ccmdReload(ConsoleSystem.Arg arg) 
		{
			if(arg.Connection != null && arg.Connection.authLevel < commandsAuthLevel) 
				return; 
				
			LoadConfig(); 
			LoadVariables(); 
			NextTick(()=> 
			{
				var storages = Resources.FindObjectsOfTypeAll<StorageContainer>().Where(c => !c.isActiveAndEnabled && !c.GetComponent<LootContainer>()).Cast<BaseEntity>().Where(b => !b.ShortPrefabName.Contains("_static")).ToList(); 
				
				if (containerVIP == null || containerVIP.Count == 0) 
					CreateContainerVIP(storages); 
				if (containerStacks == null || containerStacks.Count == 0) 
					CreateContainerStacks(storages); 
					
				storages.Clear(); 
				
				if (limitPlayerInventory && playerInventoryStacklimit > 0) 
				{
					foreach(var player in BasePlayer.activePlayerList) UpdatePlayer(player); 
					foreach(var player in BasePlayer.sleepingPlayerList) UpdatePlayer(player); 
				} 
				
				StackLoad(); 
				registeredPermissions.Clear(); 
				CreatePermissions(); 
				
				var containerUpdates = UpdateContainerStacks(); 
				var quarryUpdates = UpdateQuarryVIP(); 
				
				SendReply(arg, $"Config reloaded and {containerUpdates["counter"]} Storages updated"); 
				SendReply(arg, $"VIP Changes > {containerUpdates["vipcounter"]} Storages | {quarryUpdates["hoppercounter"]} Quarry Output | {quarryUpdates["fuelstoragecounter"]} Quarry Fuel"); 
			}); 
		}  
		[ConsoleCommand("se.clearreload")] 
		private void ccmdStackReload(ConsoleSystem.Arg arg) 
		{
			if(arg.Connection != null && arg.Connection.authLevel < commandsAuthLevel) 
				return; 
			
			Config["Settings", "clearOnReboot"] = true; 
			Config.Save(); 
			SendReply(arg, $"Your stack and container limits will be reverted to the defaults on next startup"); 
		}  
		[ConsoleCommand("se.stackcategory")] 
		void ccmdStackCategory(ConsoleSystem.Arg arg) 
		{
			if(arg.Connection != null && arg.Connection.authLevel < commandsAuthLevel) 
				return; 
				
			bool noInput = false; 
			
			if (arg.Args == null || arg.Args.Length != 2) 
			{
				SendReply(arg, "Syntax Error: Requires 2 arguments. Example: resources 32000"); 
				noInput = true; 
			} 
			if (arg.Args != null && arg.Args.Length > 1 && arg.Args[0].ToLower() != "all") 
				foreach (var cat in itemCategories) 
				{
					if (cat.StartsWith(arg.Args[0].ToLower())) 
					{
						arg.Args[0] = cat; 
						break; 
					} 
				} 
			if (noInput || (arg.Args[0].ToLower() != "all" && !itemCategories.Contains(arg.Args[0].ToLower()))) 
			{
				string cats = ""; 
				
				foreach (var cat in itemCategories) 
				{
					cats += cat+" ";
				} 
				
				if (!noInput) 
					SendReply(arg, $"Category '{arg?.Args[0]}' not found"); 
					
					SendReply(arg, $"Categories: {cats}all"); 
				return; 
			} 
			
			int i = 0; 
			
			var itemList = ItemManager.itemList; 
			
			foreach (var item in itemList) 
			{
				if(arg.Args[0].ToLower() != "all" && item.category.ToString().ToLower() != arg.Args[0].ToLower()) 
					continue; 
				if (Config["StackLimits", item.shortname] == null) 
					continue; 
				if (itemStackExcludes.Contains(item.shortname)) 
					continue;  
					
				Config["StackLimits", item.shortname] = Convert.ToInt32(arg.Args[1]); 
				item.stackable = Convert.ToInt32(arg.Args[1]); 
				i++; 
			}
			
			Config.Save(); 
			SendReply(arg, $"The Stack Size of '{i}' stackable '{arg.Args[0]}' items has been set to '{arg.Args[1]}'"); 
		} 
		[ConsoleCommand("se.stackitem")] 
		void ccmdStackItem(ConsoleSystem.Arg arg) 
		{
			if(arg.Connection != null && arg.Connection.authLevel < commandsAuthLevel) 
				return; 
				
			bool noInput = false; 
			
			if (arg.Args == null || arg.Args.Length != 2) 
			{
				SendReply(arg, "Syntax Error: Requires 2 arguments (shortname + number). Example: wood 32000"); 
				return; 
			} 
			
			var itemDef = ItemManager.FindItemDefinition(arg.Args[0].ToLower()); 
			
			if (itemDef == null) 
			{
				SendReply(arg, $"Item '{arg.Args[0]}' does not exist"); 
				return; 
			} 
			if (itemStackExcludes.Contains(itemDef.shortname))
			{
				SendReply(arg, $"The provided item is excluded from stacking by the config"); 
				return;
			} 
			
			int limit = -1; 
			
			if (!int.TryParse(arg.Args[1], out limit)) 
			{
				SendReply(arg, $"You need to set any number greater then 0"); 
				return; 
			} 
			else 
				itemDef.stackable = limit; 
				
			Config["StackLimits", itemDef.shortname] = limit; 
			Config.Save(); 
			SendReply(arg, $"The stacksize of '{arg.Args[0]}' has been set to '{limit}'"); 
		}  
		[ConsoleCommand("se.listcategory")] 
		void ccmdListCategory(ConsoleSystem.Arg arg) 
		{
			if(arg.Connection != null && arg.Connection.authLevel < commandsAuthLevel) 
				return; 
				
			string cats = ""; 
			
			if (arg.Args == null || arg.Args.Length != 1) 
			{
				SendReply(arg, "Syntax Error: Requires 1 argument. Example: resources"); 
				
				foreach (var cat in itemCategories) 
				{
					cats += cat+" ";
				} 
				
				SendReply(arg, $"Categories are: {cats}all"); 
				return; 
			} 
			
			foreach (var cat in itemCategories) 
			{
				if (cat.StartsWith(arg.Args[0].ToLower())) 
				{
					arg.Args[0] = cat; 
					break; 
				} 
			} 
			if (!itemCategories.Contains(arg.Args[0].ToLower())) 
			{
				SendReply(arg, $"Category '{arg?.Args[0]}' not found"); 
				
				foreach (var cat in itemCategories) 
				{
					cats += cat+" ";
				} 
				
				SendReply(arg, $"Categories: {cats}all"); 
				return; 
			} 
			
			TextTable textTable = new TextTable(); 
				textTable.AddColumn("Shortname"); 
				textTable.AddColumn("DisplayName"); 
				textTable.AddColumn("StackSize"); 
			
			var sb = new StringBuilder(); 
				sb.AppendLine($"\n === Stacksizes for category '{arg.Args[0]}':\n"); 
			
			foreach (var item in ItemManager.GetItemDefinitions()) 
			{
				if (itemStackExcludes.Contains(item.shortname)) 
					continue; 
				if (item.category.ToString().ToLower() != arg.Args[0].ToLower()) 
					continue; 
					
				textTable.AddRow(new string[] 
				{
					item.shortname, 
					item.displayName.english, 
					item.stackable.ToString() 
				}); 
			} 
			
			sb.AppendLine(textTable.ToString()); 
			SendReply(arg, sb.ToString()); 
		}  
		[ConsoleCommand("se.permissions")] 
		void ccmdListPerms(ConsoleSystem.Arg arg) 
		{
			if(arg.Connection != null && arg.Connection.authLevel < commandsAuthLevel) 	
				return; 
				
			TextTable textTable = new TextTable(); 
				textTable.AddColumn("Permission"); 
				textTable.AddColumn("Containers"); 
				
			foreach (var perm in registeredPermissions) 
			{
				string perms = string.Empty; 
				
				foreach ( var cont in perm.Value) 
				{
					perms += cont+"("+(int)((containerVIP[cont] as Dictionary<string,object>)["Permissions"] as Dictionary<string,object>)[perm.Key.Replace(this.Title.ToLower()+".","")]+") "; 
				} 
				
				textTable.AddRow(new string[] { perm.Key.ToString(), perms }); 
			} 
			
			SendReply(arg, "\n"+textTable.ToString()); 
		} 
	} 
}