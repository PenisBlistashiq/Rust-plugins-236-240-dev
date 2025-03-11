using ConVar;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;
using Oxide.Core.Plugins;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("UraniumTools", "MoriF#1113", "1.2.1")]
    [Description("Урановые инструменты,добывают сразу готовые ресурсы с возможностью увеличения выпадения,нанося урон радиацией")]
    class UraniumTools : RustPlugin
    {

        
                void OnLoseCondition(Item item, ref float amount)
        {
            if (item == null)
                return;
            var itemCheck = config.UraniumTools.FirstOrDefault(x => x.Value.SkinID == item.skin && x.Value.NotBreaksUse == true).Value;
            if (itemCheck != null)
                amount = 0;
        }

                [PluginReference] Plugin IQChat;
		   		 		  						  	   		  		 			   					  	   		   			
        private Boolean IsRandom(Single Chance) => Chance >= Oxide.Core.Random.Range(0, 100);

        void OnServerInitialized()
        {
            MutationRegistered();
            ReadData();
            WriteData();
        }
        private void ItemSpawnTools(BaseNetworkable entity, LootContainer Container)
        {
            foreach (KeyValuePair<String, Configuration.Tools> configUraniumTool in config.UraniumTools.Where(configUraniumTool => configUraniumTool.Value.UseDroppingItem).Where(configUraniumTool => configUraniumTool.Value.DroppingItems.ContainsKey(entity.ShortPrefabName)))
            {
                if (!IsRandom(configUraniumTool.Value.DroppingItems[entity.ShortPrefabName])) return;

                    Item item = ItemManager.CreateByName(configUraniumTool.Value.Shortname, 1,
                        configUraniumTool.Value.SkinID);
                item.name = configUraniumTool.Value.Name;

                item?.MoveToContainer(Container.inventory);
            }
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        void MutationRegistered()
        {
            Transmutations = ItemManager.GetItemDefinitions().Where(p => MutationItemList.Contains(p.shortname)).ToDictionary(p => p, p => p.GetComponent<ItemModCookable>()?.becomeOnCooked);
            ItemDefinition wood = ItemManager.FindItemDefinition(-151838493);
            ItemDefinition charcoal = ItemManager.FindItemDefinition(-1938052175);
            Transmutations.Add(wood, charcoal);
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();

                foreach (KeyValuePair<String, Configuration.Tools> configUraniumTool in config.UraniumTools)
                {
                    if (configUraniumTool.Value.DroppingItems == null ||
                        configUraniumTool.Value.DroppingItems.Count == 0)
                        configUraniumTool.Value.DroppingItems = new Dictionary<String, Int32>()
                            { ["crate_elite"] = 100 };
                }
                
                NextTick(SaveConfig);
            }
            catch
            {
                PrintWarning(LanguageEn ? "Error #6421" + $"reading config 'oxide/config/{Name}', creating a new config!!" : "Ошибка #6421" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
        }

        
                [JsonProperty(LanguageEn ? "Date with players tools" : "Дата с инструментами игроков")] public List<uint> ItemListBlocked = new List<uint>();
		   		 		  						  	   		  		 			   					  	   		   			
        public void SendChat(string Message, BasePlayer player, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message, config.PrefixChat);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
        private void ItemSpawnController(BaseNetworkable entity)
        {
            LootContainer Container = entity.GetComponent<LootContainer>();
            if (Container == null) return;
            if (Container.inventory.IsFull()) return;

            ItemSpawnTools(entity, Container);
        }
        /// <summary>
        /// Обновленеи 1.2.1
        /// - Добавлена возможность выпадения предметов из ящиков (настраивается в конфигурации)
        /// /// </summary>
        /// 
        private const Boolean LanguageEn = false;
        object OnItemRepair(BasePlayer player, Item item)
        {
            if (item == null) return null;
            if (player == null) return null;
            if (ItemListBlocked.Contains(item.uid))
            {
                SendChat("Данный предмет не подлежит починке!", player);
                return false;
            }
		   		 		  						  	   		  		 			   					  	   		   			
            foreach(KeyValuePair<String, Configuration.Tools> Tool in config.UraniumTools.Where(x => x.Value.SkinID == item.skin && x.Value.Shortname == item.info.shortname))
            {
                if (config.NoRepair)
                {
                    SendChat("Данный предмет не подлежит починке!", player);
                    return false;
                }
                if (config.RepairUse)
                    ItemListBlocked.Add(item.uid);
            }
		   		 		  						  	   		  		 			   					  	   		   			
            return null;
        }
        void OnDispenserBonus(ResourceDispenser disp, BasePlayer player, Item item)
        {
            Item weapon = player?.GetActiveItem();
            if (weapon == null) return;
            UseTools(item, player, weapon.info.shortname, weapon.skin);
        }
        protected override void SaveConfig() => Config.WriteObject(config);
        public List<string> MutationItemList = new List<string>
        {
            "chicken.raw",
            "humanmeat.raw",
            "bearmeat",
            "deermeat.raw",
            "meat.boar",
            "wolfmeat.raw",
            "hq.metal.ore",
            "metal.ore",
            "sulfur.ore"
        };

        
                [ConsoleCommand("ut_give")]
        void UraniumToolGive(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null && !arg.Player().IsAdmin) return;

            BasePlayer basePlayer = BasePlayer.Find(arg.Args[0]);
            if(basePlayer == null)
            {
                if (arg.Player() != null)
                    PrintToConsole(LanguageEn ? "Player not found" : "Игрок не найден");
                else PrintWarning(LanguageEn ? "Player not found" : "");
                return;
            }
            String Key = arg.Args[1];
            if (!config.UraniumTools.ContainsKey(Key))
            {
                if(arg.Player() != null)
                    PrintToConsole(LanguageEn ? $"Could not find tool with given key - {Key}" : $"Не удалось найти инструмент с данным ключем - {Key}");
                else PrintWarning(LanguageEn ? $"Could not find tool with given key - {Key}" : $"Не удалось найти инструмент с данным ключем - {Key}");
                return;
            }
            CreateItem(basePlayer, Key);
        }
        void ReadData()
        {
            try { ItemListBlocked = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<List<uint>>("UraniumTools/ItemListBlocked"); }
            catch { PrintWarning(LanguageEn ? "Error #1" + $"reading datafile" : "Ошибка #1" + $"чтения датафайла"); }
        }

        bool CanRecycle(Recycler recycler, Item item)
        {
            foreach (KeyValuePair<String, Configuration.Tools> Tool in config.UraniumTools.Where(x => x.Value.SkinID == item.skin && x.Value.Shortname == item.info.shortname))
            {
                if (config.NoRecycle)
                    return false;
            }
            return true;
        }
        
                void OnEntitySpawned(BaseNetworkable entity) => ItemSpawnController(entity);

        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (dispenser == null || entity == null || item == null) return;
            BasePlayer player = entity.ToPlayer();
            if (player == null) return;
            Item weapon = player?.GetActiveItem();
            if (weapon == null) return;
            UseTools(item, player, weapon.info.shortname, weapon.skin);
        }
        
                public enum DebuffType
        {
            Radiation,
            Cold,
            Blood,
            Health,
            Calories,
            Hydration,
        }
        void WriteData()
        {
            if (!config.RepairUse) return;
            timer.Every(60f, () => {
                Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("UraniumTools/ItemListBlocked", ItemListBlocked);
            });
        }
        
        void UseTools(Item item, BasePlayer player, string Shortname, ulong SkinID)
        {
            foreach (KeyValuePair<String, Configuration.Tools> Tool in config.UraniumTools.Where(x => x.Value.SkinID == SkinID && x.Value.Shortname == Shortname))
            {
                if (Tool.Value.MutationUse && Transmutations.ContainsKey(item.info))
                    item.info = Transmutations[item.info];

                if (Tool.Value.RateGatherUse)
                    item.amount = (int)(item.amount * Tool.Value.RateGather * 1);

                if(Tool.Value.DebuffVarible.UseDebuff)
                    foreach(Configuration.Tools.Debuff.DebuffSetting Debuffing in Tool.Value.DebuffVarible.debuffSetting)
                        switch (Debuffing.TypesDebuff)
                        {
                            case DebuffType.Radiation:
                                player.metabolism.radiation_poison.value += Debuffing.DebuffRate;
                                break;
                            case DebuffType.Cold:
                                player.metabolism.temperature.value -= Debuffing.DebuffRate;
                                break;
                            case DebuffType.Blood:
                                player.metabolism.bleeding.value += Debuffing.DebuffRate;
                                break;
                            case DebuffType.Health:
                                player.health -= Debuffing.DebuffRate;
                                break;
                            case DebuffType.Calories:
                                player.metabolism.calories.value -= Debuffing.DebuffRate;
                                break;
                            case DebuffType.Hydration:
                                player.metabolism.hydration.value -= Debuffing.DebuffRate;
                                break;
                            default:
                                break;
                        }

            }
        }
        
        
        private static Configuration config = new Configuration();

        void CreateItem(BasePlayer player, string Key)
        {
            Configuration.Tools UraniumTool = config.UraniumTools[Key];
            Item item = ItemManager.CreateByName(UraniumTool.Shortname, 1, UraniumTool.SkinID);
            item.name = UraniumTool.Name;

            player.GiveItem(item);
        }

                private Dictionary<ItemDefinition, ItemDefinition> Transmutations;
        private class Configuration
        {

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    RepairUse = true,
                    NoRepair = true,
                    PrefixChat = LanguageEn ? "<color=#0000FFF><b>[UraniumTools]</b></color>" : "<color=#000FFF><b>[Урановые Инструменты]</b></color>", ///
                    UraniumTools = new Dictionary<String, Tools>
                    {
                       ["uranpickaxe"] = new Tools
                        {
                            Shortname = "pickaxe",
                            Name = LanguageEn ? "Uranium Pickaxe" : "Урановая кирка",
                            SkinID = 859006499,
                            NotBreaksUse = false,
                            MutationUse = true,
                            RateGatherUse = true,
                            RateGather = 2,
                            UseDroppingItem = true,
                            DroppingItems = new Dictionary<String, Int32>()
                            {
                                ["crate_elite"] = 100,
                                ["crate_normal"] = 50,
                            },
                            DebuffVarible = new Tools.Debuff
                            {
                                UseDebuff = true,
                                debuffSetting = new List<Tools.Debuff.DebuffSetting>
                                {
                                    new Tools.Debuff.DebuffSetting
                                    {
                                        DebuffRate = 5,
                                        TypesDebuff = DebuffType.Radiation,
                                    },
                                }
                            }
                        },
                        ["uranphatchet"] = new Tools
                        {
                            Shortname = "hatchet",
                            Name = LanguageEn ? "Uranium Hatchet" : "Урановый топор",
                            SkinID = 860588662,
                            NotBreaksUse = false,
                            MutationUse = false,
                            RateGatherUse = true,
                            RateGather = 5,
                            UseDroppingItem = false,
                            DroppingItems = new Dictionary<String, Int32>()
                            {
                                ["crate_elite"] = 100,
                                ["crate_normal"] = 50,
                            },
                            DebuffVarible = new Tools.Debuff
                            {
                                UseDebuff = true,
                                debuffSetting = new List<Tools.Debuff.DebuffSetting>
                                {
                                    new Tools.Debuff.DebuffSetting
                                    {
                                        DebuffRate = 5,
                                        TypesDebuff = DebuffType.Radiation,
                                    },
                                }
                            }
                        },
                        ["coldpickaxe"] = new Tools
                        {
                            Shortname = "hatchet",
                            Name = LanguageEn ? "Cold Hatchet" : "Леядной топор",
                            SkinID = 1962047190,
                            NotBreaksUse = false,
                            MutationUse = false,
                            RateGatherUse = true,
                            RateGather = 5,
                            UseDroppingItem = false,
                            DroppingItems = new Dictionary<String, Int32>()
                            {
                                ["crate_elite"] = 100,
                                ["crate_normal"] = 50,
                            },
                            DebuffVarible = new Tools.Debuff
                            {
                                UseDebuff = true,
                                debuffSetting = new List<Tools.Debuff.DebuffSetting>
                                {
                                    new Tools.Debuff.DebuffSetting
                                    {
                                        DebuffRate = 5,
                                        TypesDebuff = DebuffType.Cold,
                                    },
                                }
                            }
                        },
                        ["multiaxe"] = new Tools
                        {
                            Shortname = "pickaxe",
                            Name = LanguageEn ? "Multi Pickaxe" : "Мульти-кирка",
                            SkinID = 2576194411,
                            NotBreaksUse = true,
                            MutationUse = true,
                            RateGatherUse = true,
                            RateGather = 15,
                            UseDroppingItem = false,
                            DroppingItems = new Dictionary<String, Int32>()
                            {
                                ["crate_elite"] = 100,
                                ["crate_normal"] = 50,
                            },
                            DebuffVarible = new Tools.Debuff
                            {
                                UseDebuff = true,
                                debuffSetting = new List<Tools.Debuff.DebuffSetting>
                                {
                                    new Tools.Debuff.DebuffSetting
                                    {
                                        DebuffRate = 5,
                                        TypesDebuff = DebuffType.Cold,
                                    },
                                    new Tools.Debuff.DebuffSetting
                                    {
                                        DebuffRate = 5,
                                        TypesDebuff = DebuffType.Radiation,
                                    },
                                    new Tools.Debuff.DebuffSetting
                                    {
                                        DebuffRate = 5,
                                        TypesDebuff = DebuffType.Calories,
                                    },
                                    new Tools.Debuff.DebuffSetting
                                    {
                                        DebuffRate = 5,
                                        TypesDebuff = DebuffType.Hydration,
                                    },
                                    new Tools.Debuff.DebuffSetting
                                    {
                                        DebuffRate = 5,
                                        TypesDebuff = DebuffType.Blood,
                                    },
                                    new Tools.Debuff.DebuffSetting
                                    {
                                        DebuffRate = 5,
                                        TypesDebuff = DebuffType.Health,
                                    },
                                }
                            }
                        },
                    }
                };
            }
            [JsonProperty( LanguageEn ? "Prefix in chat for message(IQChat)" : "Префикс в чате для сообщения(IQChat)")]
            public string PrefixChat;
            [JsonProperty( LanguageEn ? "Setting up uranium instruments (KEY (must be unique) - SETTING)" : "Настройка урановых инструментов (КЛЮЧ(должен быть уникальный) - НАСТРОЙКА)")]
            public Dictionary<String, Tools> UraniumTools = new Dictionary<String,Tools>();
            [JsonProperty( LanguageEn ? "Disable Item Recycling" : "Запретить переработку предмета")]
            public bool NoRecycle;
            [JsonProperty( LanguageEn ? "Enable one-time tool repair" : "Включить единоразовую починку инструментов")]
            public bool RepairUse;
            internal class Tools
            {
                [JsonProperty( LanguageEn ? "Crate Name = Rare" : "Название ящика = шанс")]
                public Dictionary<String, Int32> DroppingItems = new Dictionary<String, Int32>();

                [JsonProperty( LanguageEn ? "Enable item dropout from boxes" : "Включить выпадение предмета из ящиков")]
                public Boolean UseDroppingItem;
                [JsonProperty( LanguageEn ? "Display name" : "Название инструмента")]
                public string Name;
                [JsonProperty( LanguageEn ? "Tool SkinID" : "SkinID инструмента")]
                public ulong SkinID;
                internal class Debuff
                {
                    internal class DebuffSetting
                    {
                        [JsonProperty( LanguageEn ? "Factor" : "Множитель")]
                        public Single DebuffRate;
                        [JsonProperty( LanguageEn ? "Set the type of debuff on hit: 0 - Radiation, 1 - Cold, 2 - Bleeding, 3 - HP Drain, 4 - Calories, 5 - Hydration" : "Установите тип дебаффа при ударе : 0 - Радиация, 1 - Холод, 2 - Кровотечение, 3 - Снятие ХП, 4 - Калории, 5 - Жажда")]
                        public DebuffType TypesDebuff;
                    }
                    [JsonProperty( LanguageEn ? "Use debuff on hit? (true - yes/false - no)" : "Использовать дебафф при ударе? (true - да/false - нет)")]
                    public Boolean UseDebuff;

                    [JsonProperty( LanguageEn ? "Specify what debuffs will be (you can combine them by specifying several types)" : "Укажите какие дебаффы будут (вы можете комбинировать их, указав несколько типов)")]
                    public List<DebuffSetting> debuffSetting = new List<DebuffSetting>();

                }
                [JsonProperty( LanguageEn ? "Disable item breakage ? (Item will not wear out)" : "Отключить поломку предмета ? (Предмет не будет изнашиваться)")]
                public bool NotBreaksUse;
                [JsonProperty( LanguageEn ? "Mutation (Will recycle resource when mined) [true/false]" : "Мутация (При добыче будет перерабатывать ресурс) [true/false]")]
                public bool MutationUse;
                [JsonProperty( LanguageEn ? "Shortname Tool" : "Shortname инструмента")]
                public string Shortname;

                public Debuff DebuffVarible = new Debuff();
                [JsonProperty( LanguageEn ? "Increase loot X times per hit" : "Увеличивать добычу в Х раз за удар")]
                public float RateGather;
                [JsonProperty( LanguageEn ? "Resource multiplication (When hit by a tool, it will increase production by X times) [true/false]" : "Умножение ресурсов (При ударе инструментом будет увеличивать добычу в Х раз) [true/false]")]
                public bool RateGatherUse;
            }
            [JsonProperty( LanguageEn ? "Forbid repair" : "Запретить починку")]
            public bool NoRepair;
        }

            }
}
