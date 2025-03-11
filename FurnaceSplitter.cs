using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{	
    [Info("Furnace Splitter", "Nimant", "1.0.10")]    
    public class FurnaceSplitter : RustPlugin
    {        
				
		#region Variables 		

        private static FurnaceSplitter instance;
		
		private readonly static Dictionary<string, List<string>> SortItems = new Dictionary<string, List<string>>()
		{
			{ "furnace", new List<string>() { "hq.metal.ore", "metal.ore", "sulfur.ore" } },
			{ "furnace.large", new List<string>() { "hq.metal.ore", "metal.ore", "sulfur.ore" } },
			{ "furnace_static", new List<string>() { "hq.metal.ore", "metal.ore", "sulfur.ore" } },
			{ "refinery_small_deployed", new List<string>() { "crude.oil" } },
			{ "small_refinery_static", new List<string>() { "crude.oil" } },						
			
			{ "campfire", new List<string>() { "bearmeat", "chicken.raw", "deermeat.raw", "fish.raw", "horsemeat.raw", "humanmeat.raw", "meat.boar", "wolfmeat.raw" } },			
            { "fireplace.deployed", new List<string>() { "bearmeat", "chicken.raw", "deermeat.raw", "fish.raw", "horsemeat.raw", "humanmeat.raw", "meat.boar", "wolfmeat.raw" } },			
            { "hobobarrel_static", new List<string>() { "bearmeat", "chicken.raw", "deermeat.raw", "fish.raw", "horsemeat.raw", "humanmeat.raw", "meat.boar", "wolfmeat.raw" } },			
            { "skull_fire_pit", new List<string>() { "bearmeat", "chicken.raw", "deermeat.raw", "fish.raw", "horsemeat.raw", "humanmeat.raw", "meat.boar", "wolfmeat.raw" } },			
			{ "bbq.deployed", new List<string>() { "bearmeat", "chicken.raw", "deermeat.raw", "fish.raw", "horsemeat.raw", "humanmeat.raw", "meat.boar", "wolfmeat.raw" } },
			{ "cursedcauldron.deployed", new List<string>() { "bearmeat", "chicken.raw", "deermeat.raw", "fish.raw", "horsemeat.raw", "humanmeat.raw", "meat.boar", "wolfmeat.raw" } }
		};
		
		private readonly static Dictionary<string, string> MainRemains = new Dictionary<string, string>()
		{
			{ "furnace", "charcoal" },
			{ "furnace.large", "charcoal" },
			{ "furnace_static", "charcoal" },
			{ "refinery_small_deployed", "charcoal" },
			{ "small_refinery_static", "charcoal" },
			
			{ "campfire", "charcoal" },			
            { "fireplace.deployed", "charcoal" },            
            { "hobobarrel_static", "charcoal" },            
            { "skull_fire_pit", "charcoal" },
			{ "bbq.deployed", "charcoal" },
			{ "cursedcauldron.deployed", "charcoal" }
		};
		
		private readonly static Dictionary<string, string> OreRemains = new Dictionary<string, string>()
		{
			{ "hq.metal.ore", "metal.refined" },
			{ "metal.ore", "metal.fragments" },
			{ "sulfur.ore", "sulfur" },
			{ "crude.oil", "lowgradefuel" },
			
			{ "bearmeat", "bearmeat.cooked" },
			{ "chicken.raw", "chicken.cooked" },
			{ "deermeat.raw", "deermeat.cooked" },
			{ "fish.raw", "fish.cooked" },
			{ "horsemeat.raw", "horsemeat.cooked" },
			{ "humanmeat.raw", "humanmeat.cooked" },
			{ "meat.boar", "meat.pork.cooked" },
			{ "wolfmeat.raw", "wolfmeat.cooked" }
		};
		
		private readonly static Dictionary<string, List<string>> SortOrder = new Dictionary<string, List<string>>()
		{
			{ "furnace", new List<string>() { "wood", "metal.ore", "sulfur.ore", "hq.metal.ore", "metal.fragments", "sulfur", "metal.refined", "charcoal" } },
			{ "furnace.large", new List<string>() { "wood", "metal.ore", "sulfur.ore", "hq.metal.ore", "metal.fragments", "sulfur", "metal.refined", "charcoal" } },
			{ "furnace_static", new List<string>() { "wood", "metal.ore", "sulfur.ore", "hq.metal.ore", "metal.fragments", "sulfur", "metal.refined", "charcoal" } },
			{ "refinery_small_deployed", new List<string>() { "wood", "crude.oil", "lowgradefuel", "charcoal" } },
			{ "small_refinery_static", new List<string>() { "wood", "crude.oil", "lowgradefuel", "charcoal" } },
			
			{ "campfire", new List<string>() { "wood", "bearmeat", "chicken.raw", "deermeat.raw", "fish.raw", "horsemeat.raw", "humanmeat.raw", "meat.boar", "wolfmeat.raw", "bearmeat.cooked", "chicken.cooked", "deermeat.cooked", "fish.cooked", "horsemeat.cooked", "humanmeat.cooked", "meat.pork.cooked", "wolfmeat.cooked", "charcoal" } },			
            { "fireplace.deployed", new List<string>() { "wood", "bearmeat", "chicken.raw", "deermeat.raw", "fish.raw", "horsemeat.raw", "humanmeat.raw", "meat.boar", "wolfmeat.raw", "bearmeat.cooked", "chicken.cooked", "deermeat.cooked", "fish.cooked", "horsemeat.cooked", "humanmeat.cooked", "meat.pork.cooked", "wolfmeat.cooked", "charcoal" } },			
            { "hobobarrel_static", new List<string>() { "wood", "bearmeat", "chicken.raw", "deermeat.raw", "fish.raw", "horsemeat.raw", "humanmeat.raw", "meat.boar", "wolfmeat.raw", "bearmeat.cooked", "chicken.cooked", "deermeat.cooked", "fish.cooked", "horsemeat.cooked", "humanmeat.cooked", "meat.pork.cooked", "wolfmeat.cooked", "charcoal" } },			
            { "skull_fire_pit", new List<string>() { "wood", "bearmeat", "chicken.raw", "deermeat.raw", "fish.raw", "horsemeat.raw", "humanmeat.raw", "meat.boar", "wolfmeat.raw", "bearmeat.cooked", "chicken.cooked", "deermeat.cooked", "fish.cooked", "horsemeat.cooked", "humanmeat.cooked", "meat.pork.cooked", "wolfmeat.cooked", "charcoal" } },			
			{ "bbq.deployed", new List<string>() { "wood", "bearmeat", "chicken.raw", "deermeat.raw", "fish.raw", "horsemeat.raw", "humanmeat.raw", "meat.boar", "wolfmeat.raw", "bearmeat.cooked", "chicken.cooked", "deermeat.cooked", "fish.cooked", "horsemeat.cooked", "humanmeat.cooked", "meat.pork.cooked", "wolfmeat.cooked", "charcoal" } },
			{ "cursedcauldron.deployed", new List<string>() { "wood", "bearmeat", "chicken.raw", "deermeat.raw", "fish.raw", "horsemeat.raw", "humanmeat.raw", "meat.boar", "wolfmeat.raw", "bearmeat.cooked", "chicken.cooked", "deermeat.cooked", "fish.cooked", "horsemeat.cooked", "humanmeat.cooked", "meat.pork.cooked", "wolfmeat.cooked", "charcoal" } }
		};
		
		private class ItemNfo
		{			
			public int totalAmount;
			public List<int> positions;
			public bool isBurnable;
			public int minSlotsNeed;
			public bool needSplit;
			public float percent;
			public int newSlots;
			public ulong skin;
		}						

		#endregion
		
		#region Hooks		                          

		private void OnServerInitialized() => instance = this;
		
		private object CanMoveItem(Item item, PlayerInventory inventory, uint targetContainer, int targetSlot)
        {            
			if (item == null || inventory == null) 
				return null;
            
			ItemContainer container = inventory.FindContainer(targetContainer);            
                            
			if (container == null || (item.GetRootContainer() != null && container == item.GetRootContainer())) 
				return null;						

			BaseOven oven = container.entityOwner as BaseOven;			

			if (oven == null) 
				return null;												
			
			if (!SortItems.ContainsKey(oven.ShortPrefabName))
				return null;
								
			if (MoveSplitItem(item, oven))
			{																											
				AutoAddFuel(inventory, oven);				
				SortItemsInContainer(oven);				
				return false;
			}						
			
			return null;
        }
		
		#endregion
		
		#region Main
		
		private static bool MoveSplitItem(Item item, BaseOven oven)
        {
			if (item == null || oven == null) 
				return false;						
			
			if (!item.MoveToContainer(oven.inventory, -1, false)) 			
				return false;
			
			var type = oven.ShortPrefabName;
			var pos = oven.transform.position;
			
			oven.inventory.MarkDirty();
		
			var ovenItems = GetOvenItemsInfo(oven);									
			
			if (!IsMinSlotsAvailable(oven, ovenItems))
				return false;
						
			CalcItemsPercent(ref ovenItems);
			
			int freeSlots = CalcFreeSlots(oven, ovenItems);																			
						
			if (freeSlots <= 0) 
				return false;
			
			if (!IsCalcNewItemsSlots(ref ovenItems, freeSlots))
				return false;						
			
			List<int> usedPos = new List<int>();
			List<Item> newItems = new List<Item>();						
			
			foreach(var pair in ovenItems.OrderBy(x=> !x.Value.isBurnable).ThenByDescending(x=> x.Value.percent))
			{
				var slots = pair.Value.newSlots == 0 ? pair.Value.minSlotsNeed : pair.Value.newSlots;				
				int totalAmount = pair.Value.totalAmount;
				
				for(int ii=0;ii<slots;ii++)
				{		
					if (totalAmount <= 0) break;																														
					if (oven == null)
					{
						instance.PrintWarning($"Неожиданное исчезновение контейнера '{type}', в точке {pos}. Итерация 1.");
						return false;
					}
					
					if (!IsUsedItemFromPosition(oven, ref usedPos, ref totalAmount, pair.Key, pair.Value, slots-ii))					
						if (!IsCreateNewItem(ref newItems, ref totalAmount, pair.Key, pair.Value, slots-ii))
							return false;
				}
				
				if (oven == null)
				{
					instance.PrintWarning($"Неожиданное исчезновение контейнера '{type}', в точке {pos}. Итерация 2.");
					return false;
				}
				DeleteLeftItemsFromPositions(oven, usedPos, pair.Value);											
			}
			
			if (oven == null)
			{
				instance.PrintWarning($"Неожиданное исчезновение контейнера '{type}', в точке {pos}. Итерация 3.");
				return false;
			}
			MoveNewItemsToContainer(ref newItems, oven);						
					
            return true;
        }
						
		private static Dictionary<string, ItemNfo> GetOvenItemsInfo(BaseOven oven)
		{						
			ItemContainer container = oven.inventory;
			var result = new Dictionary<string, ItemNfo>();
					
			foreach(var item in container.itemList)
			{								
				var key = $"{item.info.shortname}|{item.skin}";
				if (!result.ContainsKey(key))
				{															
					ItemNfo nfo = new ItemNfo();					
					nfo.totalAmount = item.amount;
					nfo.positions = new List<int>() { item.position };
					nfo.isBurnable = oven.fuelType != null && oven.fuelType == item.info;			
					nfo.needSplit = SortItems[oven.ShortPrefabName].Contains(item.info.shortname) && !item.IsBlueprint();
					nfo.skin = item.skin;
					result.Add(key, nfo);																												
				}
				else
				{
					result[key].totalAmount += item.amount;
					result[key].positions.Add(item.position);
				}								
			}
			
			foreach(var pair in result)	
			{
				var info = ItemManager.FindItemDefinition(pair.Key.Split('|')[0]);
				
				if (!IsNoStack(pair.Key))
					pair.Value.minSlotsNeed = (int)Math.Ceiling((float)pair.Value.totalAmount / info.stackable);			
				else
					pair.Value.minSlotsNeed = container.itemList.Where(x=> x.info.shortname == pair.Key.Split('|')[0] && x.skin.ToString() == pair.Key.Split('|')[1]).Count();
			}
			
			return result;
		}				
		
		private static bool IsNoStack(string key)
		{						
			var itemName = key.Split('|')[0];
			if (itemName == "blueprintbase") return true;
			
			if (MainRemains.Values.Contains(itemName))
				return true;
			
			if (OreRemains.Values.Contains(itemName))
				return true;
			
			return false;
		}
		
        private static bool IsMinSlotsAvailable(BaseOven oven, Dictionary<string, ItemNfo> ovenItems)
        {
            int totalMinSlots = 0;			
			foreach(var pair in ovenItems)			
				totalMinSlots += pair.Value.minSlotsNeed;							
			
			if (totalMinSlots > oven.inventory.capacity) 
				return false;
			
			return true;
        }		

        private static void CalcItemsPercent(ref Dictionary<string, ItemNfo> ovenItems)
		{
			int totalAmount_ = 0;
			foreach(var pair in ovenItems)
			{			
				if (pair.Value.needSplit)
					totalAmount_ += pair.Value.totalAmount;								
			}						
			
			foreach(var pair in ovenItems)
			{
				if (pair.Value.needSplit)
					pair.Value.percent = (100f * pair.Value.totalAmount) / totalAmount_;
			}
		}
		
		private static bool IsBurnableExists(Dictionary<string, ItemNfo> ovenItems)
		{
			bool wasBurnable = false;			
			foreach(var pair in ovenItems)		
			{											
				if (pair.Value.isBurnable)
					wasBurnable = true;
			}										
			
			return wasBurnable;				
		}
		
		private static int CalcFreeSlots(BaseOven oven, Dictionary<string, ItemNfo> ovenItems)
		{
			int freeSlots = oven.inventory.capacity;						
			
			if (!IsBurnableExists(ovenItems))
				freeSlots -= 1;		
			
			if (MainRemains.ContainsKey(oven.ShortPrefabName))			
				if (ovenItems.Where(x=> MainRemains[oven.ShortPrefabName] == x.Key.Split('|')[0]).Count()==0)				
					freeSlots -= 1;																		
			
			foreach(var pair in ovenItems)		
			{			
				if (!pair.Value.needSplit)
					freeSlots -= pair.Value.minSlotsNeed;
				
				if (pair.Value.needSplit && OreRemains.ContainsKey(pair.Key.Split('|')[0]))				
					if (ovenItems.Where(x=> OreRemains[pair.Key.Split('|')[0]] == x.Key.Split('|')[0]).Count()==0)											
						freeSlots -= 1;																										
			}																												
			
			return freeSlots;
		}
		
		private static bool IsCalcNewItemsSlots(ref Dictionary<string, ItemNfo> ovenItems, int freeSlots)
		{
			int occupNewSlots = 0;
			foreach(var pair in ovenItems.OrderBy(x=> x.Value.percent))			
			{
				if (pair.Value.needSplit)
				{					
					pair.Value.newSlots = (int)Math.Round(freeSlots * (pair.Value.percent / 100f));
					pair.Value.newSlots = pair.Value.newSlots <= 0 ? 1 : pair.Value.newSlots;
					occupNewSlots += pair.Value.newSlots;
				}
			}
			
			int terminate = 100;
			while((occupNewSlots - freeSlots) > 0)
			{
				if (terminate <= 0)
				{
					instance.PrintError("Обнаружен бесконечный цикл!");
					return false;
				}
				
				foreach(var nfo in ovenItems.OrderByDescending(x=> x.Value.newSlots).Select(x=> x.Value).ToList())			
				{
					if (nfo.needSplit)
					{
						if (nfo.newSlots-1 <= 0)
							return false;
						
						nfo.newSlots -= 1;
						occupNewSlots -= 1;
						break;
					}
				}
				terminate--;
			}
			
			return true;
		}				
		
		private static bool IsUsedItemFromPosition(BaseOven oven, ref List<int> usedPos, ref int totalAmount, string key, ItemNfo nfo, int passSlots)
		{						
			var info = ItemManager.FindItemDefinition(key.Split('|')[0]);
			
			for(int jj=0;jj<nfo.positions.Count;jj++)
			{												
				if (usedPos.Contains(nfo.positions[jj])) continue;
				if (totalAmount <= 0) break;
										
				var uItem = oven.inventory.itemList.FirstOrDefault(x=>x.position == nfo.positions[jj]);
				if (uItem == null) continue;
				
				if (!nfo.needSplit && !IsNoStack(key))
				{
					if (totalAmount >= uItem.info.stackable)
					{
						uItem.amount = uItem.info.stackable;
						totalAmount -= uItem.info.stackable;
					}
					else
					{
						uItem.amount = totalAmount;						
						totalAmount = 0;
					}
				}
				else
				{
					int splitAmount = (int)Math.Ceiling((float)totalAmount / (passSlots));
					
					if (splitAmount > info.stackable)
						return false;
					
					if (totalAmount >= splitAmount)
					{
						uItem.amount = splitAmount;
						totalAmount -= splitAmount;
					}
					else
					{
						uItem.amount = totalAmount;
						totalAmount = 0;
					}
				}
				
				usedPos.Add(nfo.positions[jj]);
				return true;
			}						
			
			return false;
		}
		
		private static bool IsCreateNewItem(ref List<Item> newItems, ref int totalAmount, string key, ItemNfo nfo, int passSlots)
		{
			var info = ItemManager.FindItemDefinition(key.Split('|')[0]);
			
			if (!nfo.needSplit)
			{
				if (totalAmount >= info.stackable)
				{																								
					var newItem = ItemManager.Create(info, info.stackable, nfo.skin);								
					newItems.Add(newItem);								
					totalAmount -= info.stackable;
				}
				else
				{
					var newItem = ItemManager.Create(info, totalAmount, nfo.skin);								
					newItems.Add(newItem);								
					totalAmount = 0;
				}
			}
			else
			{
				int splitAmount = (int)Math.Ceiling((float)totalAmount / passSlots);
				
				if (splitAmount > info.stackable)
					return false;
				
				if (totalAmount >= splitAmount)
				{
					var newItem = ItemManager.Create(info, splitAmount, nfo.skin);								
					newItems.Add(newItem);								
					totalAmount -= splitAmount;
				}
				else
				{
					var newItem = ItemManager.Create(info, totalAmount, nfo.skin);
					newItems.Add(newItem);								
					totalAmount = 0;
				}
			}
			
			return true;
		}
		
		private static void DeleteLeftItemsFromPositions(BaseOven oven, List<int> usedPos, ItemNfo nfo)
		{
			if (oven == null) return;
			
			for(int jj=0;jj<nfo.positions.Count;jj++)
			{
				if (usedPos.Contains(nfo.positions[jj])) continue;
				
				var uItem = oven.inventory.itemList.FirstOrDefault(x=>x.position == nfo.positions[jj]);
				if (uItem != null)
				{
					uItem.RemoveFromContainer();
					uItem.Remove(0f);
				}
			}	
		}
		
		private static void MoveNewItemsToContainer(ref List<Item> newItems, BaseOven oven)
		{			
			if (oven == null) return;
			
			foreach(var newItem in newItems)
			{
				if (newItem == null || oven == null) continue;
				
				if (!newItem.MoveToContainer(oven.inventory, -1, false))					
				{					
					instance.PrintWarning("Предмет не влез в контейнер и был выброшен!");
					newItem.Drop(oven.transform.position, oven.dropVelocity + Vector3Ex.Range(-1f, 1f)); 					
				}
			}
			
			newItems.Clear();
			
			if (oven != null) 			
				oven.inventory.MarkDirty();            
		}				
		
		private static bool IsFirstItemMore(Item item1, Item item2, BaseOven oven)
		{
			foreach(var itemName in SortOrder[oven.ShortPrefabName])
			{								
				if (item1.info.shortname == itemName)
				{
					if (item2.info.shortname == itemName)
					{
						if (item1.amount >= item2.amount)
							return true;
						else
							return false;
					}
					else
						return true;
				}
				else				
					if (item2.info.shortname == itemName)
						return false;					
			}
						
			return false;
		}
		
		private static void SwapItems(Item item1, Item item2)
		{
			if (item1 == null || item2 == null) return;
			var container1 = item1.parent;
			var container2 = item2.parent;
			if (container1 == null || container2 == null) return;
			var slot1 = item1.position;
			var slot2 = item2.position;
			item1.RemoveFromContainer();
			item2.RemoveFromContainer();
			item1.MoveToContainer(container2, slot2);
			item2.MoveToContainer(container1, slot1);
		}
		
		private static void SortItemsInContainer(BaseOven oven)
		{
			if (oven == null) return;
				
			List<Item> Items = oven.inventory.itemList.ToList();												
			
			for(int jj=0;jj<Items.Count()-1;jj++)
			{				
				for(int ii=jj+1;ii<Items.Count();ii++)
				{
					if (!IsFirstItemMore(Items[jj], Items[ii], oven))
					{																														
						var tempItem = Items[ii];
						Items[ii] = Items[jj];
						Items[jj] = tempItem;						
					}					
				}
			}									
									
			for(int pos=0;pos<Items.Count;pos++)
			{
				var item = oven.inventory.itemList.FirstOrDefault(x=>x.position == pos);
				if (item != null)
				{
					if (Items[pos] != item)
						SwapItems(Items[pos], item);										
				}
				else
				{
					Items[pos].RemoveFromContainer();
					Items[pos].MoveToContainer(oven.inventory, pos);
				}
			}						
			
			Items.Clear();
			oven.inventory.MarkDirty();  
		}
                
		#endregion
		
		#region AutoAddFuel
		
		private static Dictionary<ItemDefinition, float> GetSmeltTimes(BaseOven oven)
        {
            ItemContainer container = oven.inventory;
            var cookables = container.itemList.Where(item =>
            {
                ItemModCookable cookable = item.info.GetComponent<ItemModCookable>();
                return cookable != null && CanCook(cookable, oven);
            }).ToList();

            if (cookables.Count == 0)
                return new Dictionary<ItemDefinition, float>();

            var distinctCookables = cookables.GroupBy(item => item.info, item => item).ToList();
            Dictionary<ItemDefinition, int> amounts = new Dictionary<ItemDefinition, int>();

            foreach (var group in distinctCookables)
            {
                int biggestAmount = group.Max(item => item.amount);
                amounts.Add(group.Key, biggestAmount);
            }

            var smeltTimes = amounts.ToDictionary(kv => kv.Key, kv => GetSmeltTime(kv.Key.GetComponent<ItemModCookable>(), kv.Value));
            return smeltTimes;
        }

		private static float GetSmeltTime(ItemModCookable cookable, int amount)
        {
            float smeltTime = cookable.cookTime * amount;
            return smeltTime;
        }   
		
        private static bool CanCook(ItemModCookable cookable, BaseOven oven)
        {
            return oven.cookingTemperature >= cookable.lowTemp && oven.cookingTemperature <= cookable.highTemp;
        }
		
        private static void AutoAddFuel(PlayerInventory playerInventory, BaseOven oven)
        {
            int neededFuel = (int)Math.Ceiling(GetOvenFuelNeed(oven));
            neededFuel -= oven.inventory.GetAmount(oven.fuelType.itemid, false);
            var playerFuel = playerInventory.FindItemIDs(oven.fuelType.itemid);

            if (neededFuel <= 0 || playerFuel.Count <= 0)
                return;

            foreach (Item fuelItem in playerFuel)
            {
                if (oven.inventory.CanAcceptItem(fuelItem, -1) != ItemContainer.CanAcceptResult.CanAccept)
                    break;

                Item largestFuelStack = oven.inventory.itemList.Where(item => item.info == oven.fuelType).OrderByDescending(item => item.amount).FirstOrDefault();
                int toTake = Math.Min(neededFuel, oven.fuelType.stackable - (largestFuelStack?.amount ?? 0));

                if (toTake > fuelItem.amount)
                    toTake = fuelItem.amount;

                if (toTake <= 0)
                    break;

                neededFuel -= toTake;

                if (toTake >= fuelItem.amount)
                {
                    fuelItem.MoveToContainer(oven.inventory);
                }
                else
                {
                    Item splitItem = fuelItem.SplitItem(toTake);
                    if (!splitItem.MoveToContainer(oven.inventory)) // Break if oven is full
                        break;
                }

                if (neededFuel <= 0)
                    break;
            }
        }         
		
		private static float GetOvenFuelNeed(BaseOven oven)
        {
            float result = 0;
            var smeltTimes = GetSmeltTimes(oven); 

            if (smeltTimes.Count > 0)
            {
                var longestStack = smeltTimes.OrderByDescending(kv => kv.Value).First();
                float fuelUnits = oven.fuelType.GetComponent<ItemModBurnable>().fuelAmount * configData.SmeltRate; 
                float neededFuel = (float)Math.Ceiling(longestStack.Value * (oven.cookingTemperature / 200.0f) / fuelUnits);

                result = neededFuel;                
            }

            return result;
        } 
		
		#endregion
		
		#region Config        
		
		private void Init() => LoadVariables();
		
        private static ConfigData configData;
		
        private class ConfigData
        {            
			[JsonProperty(PropertyName = "Рейт плавки")]
			public float SmeltRate;
        }
		
        private void LoadVariables() => configData = Config.ReadObject<ConfigData>();        
		
        protected override void LoadDefaultConfig()
        {
            configData = new ConfigData
            {
                SmeltRate = 1f
            };
            SaveConfig(configData);
			timer.Once(0.1f, ()=> SaveConfig(configData));
        }        
		
        private void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
		
        #endregion
       
    }
}
