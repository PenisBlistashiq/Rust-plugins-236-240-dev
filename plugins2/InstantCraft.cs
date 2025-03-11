using Newtonsoft.Json;
using System.Collections.Generic;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("InstantCraft", "Ryamkk", "1.0.0")]
    public class InstantCraft : RustPlugin
    {
        private PluginConfig config;

        class PluginConfig
        {
            [JsonProperty(PropertyName = "Проверять свободное место перед крафтом")]
            public bool checkPlace = false;
            
            [JsonProperty(PropertyName = "Список предметов с обычным временем крафта")]
            public List<string> normal = new List<string>
            {
                "",
                "put item shortname here"
            };

            [JsonProperty(PropertyName = "Список заблокированных предметов")]
            public List<string> blocked = new List<string>
            {
                "",
                "put item shortname here"
            };
            
            [JsonProperty(PropertyName = "Разделять вещи при выдаче на стаки")]
            public bool split = false;
        }
		
        private void Init()
        {
            config = Config.ReadObject<PluginConfig>();
            Config.WriteObject(config);
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(new PluginConfig(), true);
        }
        
        private object OnItemCraft(ItemCraftTask task)
        {
            var player = task.owner;
            var target = task.blueprint.targetItem;
            var name = target.shortname;

            if (IsBlocked(name))
            {
                task.cancelled = true;
                SendReply(player, "Данный предмет заблокирован для крафта!");
                GiveRefund(player, task.takenItems);
                return null;
            }

            var stacks = GetStacks(target, task.amount * task.blueprint.amountToCreate);
            var slots = FreeSlots(player);

            if (HasPlace(slots, stacks) == false)
            {
                task.cancelled = true;
                SendReply(player, "У вас недостаточно места, нужно {0} свободно {1}.", stacks.Count, slots);
                GiveRefund(player, task.takenItems);
                return null;
            }
            
            if (IsNormalItem(name))
            {
                SendReply(player, "Предмет убран из списка мгновенного крафта и будут крафтится с обычной скоростью!");
                return null;
            }
            
            GiveItem(player, task, target, stacks, task.skinID);
            task.cancelled = true;
            return false;
        }

        private void GiveItem(BasePlayer player, ItemCraftTask task, ItemDefinition def, List<int> stacks, int taskSkinID)
        {
            var skin = ItemDefinition.FindSkin(def.itemid, taskSkinID);
            
            if (config.split == false)
            {
                var final = 0;

                foreach (var stack in stacks)
                {
                    final += stack;
                }
                
                var item = ItemManager.Create(def, final, skin);
                player.GiveItem(item);
                Interface.CallHook("OnItemCraftFinished", task, item);
            }
            else
            {
                foreach (var stack in stacks)
                {
                    var item = ItemManager.Create(def, stack, skin);
                    player.GiveItem(item);
                    Interface.CallHook("OnItemCraftFinished", task, item);
                }
            }
        }

        private int FreeSlots(BasePlayer player)
        {
            var slots = player.inventory.containerMain.capacity + player.inventory.containerBelt.capacity;
            var taken = player.inventory.containerMain.itemList.Count + player.inventory.containerBelt.itemList.Count;
            return slots - taken;
        }

        private void GiveRefund(BasePlayer player, List<Item> items)
        {
            foreach (var item in items)
            {
                player.GiveItem(item);
            }
        }

        private List<int> GetStacks(ItemDefinition item, int amount) 
        {
            var list = new List<int>();
            var maxStack = item.stackable;

            while (amount > maxStack)
            {
                amount -= maxStack;
                list.Add(maxStack);
            }
            
            list.Add(amount);
            
            return list; 
        }

        private bool IsNormalItem(string name)
        {
            return config.normal?.Contains(name) ?? false;
        }

        private bool IsBlocked(string name)
        {
            return config.blocked?.Contains(name) ?? false;
        }

        private bool HasPlace(int slots, List<int> stacks)
        {
            if (config.checkPlace == false)
            {
                return true;
            }

            if (config.split && slots - stacks.Count < 0)
            {
                return false;
            }

            return slots > 0;
        }
    }
}
