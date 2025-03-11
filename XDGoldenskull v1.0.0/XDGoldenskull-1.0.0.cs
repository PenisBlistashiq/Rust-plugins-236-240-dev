using System.Collections.Generic;
using ConVar;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("XD Golden skull", "DezLife", "1.0.0")]
    public class XDGoldenskull : RustPlugin
    {
        [PluginReference] Plugin IQChat;
        private const string ReplaceShortName = "skull.human";

        #region Classes/cfg

        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty("Отображаемое имя")]
            public string DisplayName;
            [JsonProperty("Стак предмета")]
            public int StackItem;
            [JsonProperty("Скин ID черепа")]
            public ulong ReplaceID;
            [JsonProperty("Из каких бочек будет падать и процент выпадения")]
            public Dictionary<string, int> barellList = new Dictionary<string, int>();
            [JsonProperty("Из каких ящиков будет падать и процент выпадения")]
            public Dictionary<string, int> cratelList = new Dictionary<string, int>();
            [JsonProperty("Призы за переработку")]
            public List<string> itemsrec = new List<string>();
            [JsonProperty("Призы за потрошения")]
            public List<string> itempot = new List<string>();
            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    DisplayName = "Золотой череп",
                    StackItem = 5,
                    ReplaceID = 1683645276,
                    barellList = new Dictionary<string, int>
                    {
                        ["loot-barrel-1"] = 50,
                        ["loot-barrel-2"] = 20,
                    },
                    cratelList = new Dictionary<string, int>
                    {
                        ["bradley_crate"] = 50,
                        ["codelockedhackablecrate_oilrig"] = 20,
                        ["crate_elite"] = 20,
                    },
                    itemsrec = new List<string>
                    {
                        "weapon.mod.small.scope",
                        "rifle.ak",
                        "rifle.l96",
                        "smg.thompson",
                        "rifle.semiauto",
                        "pistol.revolver",
                        "rifle.lr300",
                    },
                    itempot = new List<string>
                    {
                        "shotgun.double",
                        "grenade.f1",
                        "smg.2",
                        "shotgun.pump",
                        "pistol.semiauto",
                        "pistol.python",
                        "weapon.mod.lasersight",
                        "weapon.mod.muzzlebrake",
                    },
                };
            }

            public int GetItemId() => ItemManager.FindItemDefinition(ReplaceShortName).itemid;

            public Item Copy(int amount = 1)
            {
                Item x = ItemManager.CreateByPartialName(ReplaceShortName, amount);
                x.skin = ReplaceID;
                x.name = DisplayName;
                x.info.stackable = StackItem;

                return x;
            }

            public void CreateItem(BasePlayer player, Vector3 position, int amount)
            {
                Item x = ItemManager.CreateByPartialName(ReplaceShortName, amount);
                x.skin = ReplaceID;
                x.name = DisplayName;
                x.info.stackable = StackItem;

                if (player != null)
                {
                    if (player.inventory.containerMain.itemList.Count < 24)
                        x.MoveToContainer(player.inventory.containerMain);
                    else
                        x.Drop(player.transform.position, Vector3.zero);
                    return;
                }

                if (position != Vector3.zero)
                {
                    x.Drop(position, Vector3.down);
                    return;
                }
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning("Ошибка #245" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);


        #endregion

        #region command

        [ChatCommand("g.give")]
        private void cmdChatEmerald(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            config.CreateItem(player, Vector3.zero, 10);
        }

        [ConsoleCommand("goldenskul")]
        void FishCommand(ConsoleSystem.Arg arg)
        {

            BasePlayer player = BasePlayer.Find(arg.Args[0]);
            if (player == null || !player.IsConnected)
            {
                Puts("Игрок не найден");
                return;
            }
            int count = int.Parse(arg.Args[1]);
            config.CreateItem(player, Vector3.zero, count);
            SendChat(player, $"Вы успешно получили {config.DisplayName}");
            Puts($"Игроку выдана {config.DisplayName}");
        }

        #endregion

        #region hooks
        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity.GetComponent<LootContainer>() == null) return;
            foreach (var crate in config.cratelList)
            {
                if (entity.PrefabName.Contains(crate.Key))
                {
                    bool goodChance = random.Next(0, 100) >= (100 - crate.Value);
                    if (goodChance)
                    {
                        var item = (Item)CreateItem();
                        item?.MoveToContainer(entity.GetComponent<LootContainer>().inventory);
                    }
                }
            }   
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            foreach (var Barrel in config.barellList)
            {
                if (entity.PrefabName.Contains(Barrel.Key))
                {
                    bool goodChance = random.Next(0, 100) >= (100 - Barrel.Value);
                    if (goodChance)
                    {
                        config.CreateItem(null, entity.transform.position, 1);
                    }
                }
            }
        }

        object CanRecycle(Recycler recycler, Item item)
        {
            if (item.info.shortname == ReplaceShortName && item.skin == config.ReplaceID)
                return true;
            return null;
        }
        object OnRecycleItem(Recycler recycler, Item item)
        {
            if (item.info.shortname == ReplaceShortName && item.skin == config.ReplaceID)
            {
                item.UseItem(1);
                int RandomItem = random.Next(config.itemsrec.Count);
                recycler.MoveItemToOutput(ItemManager.CreateByName(config.itemsrec[RandomItem], 1));
                return true;
            }
            return null;
        }

        object OnItemAction(Item item, string action, BasePlayer player)
        {
            if (action == "crush" && item.skin == config.ReplaceID)
            {
                Item itemS = ItemManager.CreateByName(config.itempot[random.Next(config.itempot.Count)], 1, 0);
                player.GiveItem(itemS, BaseEntity.GiveItemReason.PickedUp);
                ItemRemovalThink(item, player, 1);
                return false;
            }
            return null;
        }

        #endregion

        #region Help
        private Item CreateItem()
        {
            return config.Copy(1);
        }
        private static void ItemRemovalThink(Item item, BasePlayer player, int itemsToTake)
        {
            if (item.amount == itemsToTake)
            {
                item.RemoveFromContainer();
                item.Remove();
            }
            else
            {
                item.amount = item.amount - itemsToTake;
                player.inventory.SendSnapshot();
            }
        }

        private static System.Random random = new System.Random();
        public void SendChat(BasePlayer player, string Message, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message, "");
            else
                player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
        #endregion
    }
}
