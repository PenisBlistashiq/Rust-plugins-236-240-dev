using System;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Oxide.Plugins
{
    [Info("XMoneyReward", "Monster", "1.0.3")]
    class XMoneyReward : RustPlugin
    {	
        #region Reference
		
		[PluginReference] private Plugin ImageLibrary, RustStore;
		
		#endregion
		
		#region Configuration

        private MoneyConfig config;

        private class MoneyConfig
        {		
			internal class StoreSetting
            {
				[JsonProperty("Секретный ключ: (Оставьте пустым если магазин OVH)")] public string StoreKey;				
				[JsonProperty("ID магазина: (Оставьте пустым если магазин OVH)")] public string StoreID;
				[JsonProperty("0 - GameStores | 1 - OVH | 2 - плагин экономики, наград или внутриигровой магазин. К примеру XShop")] public int Store;	
                [JsonProperty("Название плагина")] public string PluginName;				
                [JsonProperty("Название метода(API)")] public string HookName;				
                [JsonProperty("Тип параметра хука - [ player | userID ]")] public string Parameter;				
            }			
			
			internal class RewardSetting
            {
				[JsonProperty("Время онлайна для доступа к выводам")] public int RewardTimeTake;				
				[JsonProperty("Интервал выдачи бонуса (в сек.)")] public int RewardTime;								
				[JsonProperty("Максимальный баланс для вывода")] public int RewardMax;				
				[JsonProperty("Вывод раз в сутки")] public bool TakeDay;
            }	

            internal class LogoSetting	
            {
				[JsonProperty("AnchorMin")] public string AnchorMin;					
				[JsonProperty("AnchorMax")] public string AnchorMax;					
				[JsonProperty("OffsetMin")] public string OffsetMin;					
				[JsonProperty("OffsetMax")] public string OffsetMax;				
				[JsonProperty("Цвет картинки")] public string ColorLogo;				
				[JsonProperty("Цвет текста")] public string ColorText;				
				[JsonProperty("Картинка логотипа")] public string ImageLogo;	
			}
			
			internal class GUISetting	
			{
				[JsonProperty("AnchorMin")] public string AnchorMin;					
				[JsonProperty("AnchorMax")] public string AnchorMax;					
				[JsonProperty("OffsetMin")] public string OffsetMin;					
				[JsonProperty("OffsetMax")] public string OffsetMax;
				[JsonProperty("Цвет панели")] public string ColorPanel;				
				[JsonProperty("Цвет кнопок")] public string ColorButton;
			}  			
            
			[JsonProperty("Настройка пермишенов. [ Размер бонуса | Пермишен ]")]
            public Dictionary<string, float> Permisssion;
			[JsonProperty("Данные магазина")]
            public StoreSetting Store = new StoreSetting();			
			[JsonProperty("Деньги за онлайн")] 
            public RewardSetting Reward = new RewardSetting();			
			[JsonProperty("Настройка логотипа")]
            public LogoSetting LogoGUI = new LogoSetting();			
			[JsonProperty("Настройка меню")]
            public GUISetting GUI = new GUISetting();					
			 
			public static MoneyConfig GetNewConfiguration()
            {
                return new MoneyConfig
                {
					Permisssion = new Dictionary<string, float>
					{
						["xmoneyreward.default"] = 2
					},
					Store = new StoreSetting
					{
						StoreKey = "",
						StoreID = "",  
						Store = 2,
						PluginName = "XShop",
						HookName = "API_GiveBalance",
						Parameter = "player"
						
					},
					Reward = new RewardSetting
					{
						RewardTimeTake = 18000,
						RewardTime = 3600,
						RewardMax = 100,
						TakeDay = true
					},
					LogoGUI = new LogoSetting
					{
						AnchorMin = "0 1",
						AnchorMax = "0 1",
						OffsetMin = "5 -50",
						OffsetMax = "55 -5",
						ColorLogo = "1 1 1 0.25",
						ColorText = "1 1 1 0.3",
						ImageLogo = "https://i.imgur.com/R7gWOsP.png"
					},
					GUI = new GUISetting
					{
						AnchorMin = "0 1",
						AnchorMax = "0 1",
						OffsetMin = "10 -155",
						OffsetMax = "170 -65",
						ColorPanel = "0.5 0.5 0.5 0.75",
						ColorButton = "0 0 0 0.75"
					}
				};
			}
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
			
			try
			{
				config = Config.ReadObject<MoneyConfig>();
			}
			catch
			{
				PrintWarning("Ошибка чтения конфигурации! Создание дефолтной конфигурации!");
				LoadDefaultConfig();
			}
			
			SaveConfig();
        }
		protected override void LoadDefaultConfig() => config = MoneyConfig.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion
		
		#region Data
		
		private class XMoneyRewardData
        {
			[JsonProperty("Время игры")] public int TimePlay;			
			[JsonProperty("Деньги")] public float Money;			
			[JsonProperty("День последнего вывода")] public int MoneyTake;													
        }
		
		private Dictionary<ulong, XMoneyRewardData> StoredData = new Dictionary<ulong, XMoneyRewardData>();
		
		#endregion
		
		#region Hooks
		
		private void OnServerInitialized()
        {
			
			ImageLibrary.Call("AddImage", config.LogoGUI.ImageLogo, "LogoImage");
			
			if (Interface.Oxide.DataFileSystem.ExistsDatafile("XMoneyReward"))
                StoredData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, XMoneyRewardData>>("XMoneyReward");
 
            timer.Every(config.Reward.RewardTime, () => { BasePlayer.activePlayerList.ToList().ForEach(GiveMoney); });
			timer.Every(120, () => { Interface.Oxide.DataFileSystem.WriteObject("XMoneyReward", StoredData); });
			
            BasePlayer.activePlayerList.ToList().ForEach(OnPlayerConnected);	
            InitializeLang();

            foreach (var perm in config.Permisssion)
                permission.RegisterPermission(perm.Key, this);			
        }
		
		private void Unload()
		{
			Interface.Oxide.DataFileSystem.WriteObject("XMoneyReward", StoredData);
			
			foreach (BasePlayer player in BasePlayer.activePlayerList)
			{			
			    CuiHelper.DestroyUi(player, ".Logo");
				CuiHelper.DestroyUi(player, ".Money");
			}
		}
				
		private void OnPlayerConnected(BasePlayer player)
		{
			if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerConnected(player));
                return;
            }
			
			if (!StoredData.ContainsKey(player.userID))
				StoredData.Add(player.userID, new XMoneyRewardData());			
			
			LogoGUI(player);
		}
		
		private void OnPlayerDisconnected(BasePlayer player, string reason)
		{
			if (player == null) return;
			if (!StoredData.ContainsKey(player.userID)) return;

			StoredData[player.userID].TimePlay += Convert.ToInt32(player.Connection.GetSecondsConnected());
		}
		
		private void GiveMoney(BasePlayer player)
		{
			if (StoredData[player.userID].Money == config.Reward.RewardMax) return;
		
			foreach (var perm in config.Permisssion)
                if (permission.UserHasPermission(player.UserIDString, perm.Key))
				{
					StoredData[player.userID].Money += perm.Value;
					
					if (StoredData[player.userID].Money >= config.Reward.RewardMax) StoredData[player.userID].Money = config.Reward.RewardMax;
			
			        LogoGUI(player);
					break;
				}
		}		
		
		private void TakeMoney(BasePlayer player)
		{
			if (StoredData[player.userID].Money == 0)
			{
				Message(player, lang.GetMessage("CHATNOM", this, player.UserIDString));
				return;
			}
			
			if (config.Reward.TakeDay)
			{
			    if (StoredData[player.userID].MoneyTake == DateTime.Now.Day)
			    {
				    Message(player, lang.GetMessage("CHATDE", this, player.UserIDString));
				    return;
			    }
			}
			if (StoredData[player.userID].TimePlay + Convert.ToInt32(player.Connection.GetSecondsConnected()) <= config.Reward.RewardTimeTake)
				Message(player, string.Format(lang.GetMessage("CHATT", this, player.UserIDString), TimeSpan.FromSeconds(config.Reward.RewardTimeTake - (StoredData[player.userID].TimePlay + Convert.ToInt32(player.Connection.GetSecondsConnected())))));
			else
			{
				double money = StoredData[player.userID].Money;
				
				switch (config.Store.Store)
			    {				
				    case 0:
				    {
					    Dictionary<string, string> dictionary = new Dictionary<string, string>
			            {
				            {"action", "moneys"},
				            {"type", "plus"},
				            {"steam_id", player.UserIDString},
				            {"amount", money.ToString()},
                            {"mess", "Деньги за онлайн"}
			            };
					 
						string url = $"https://gamestores.ru/api?shop_id={config.Store.StoreID}&secret={config.Store.StoreKey}{string.Join("", dictionary.Select(arg => $"&{arg.Key}={arg.Value}").ToArray())}";
					
					    webrequest.EnqueueGet(url, (i, s) => 
			            {
					    	if(JsonConvert.DeserializeObject<JObject>(s)["code"].ToString() == "100") 
						    	TakeMoneyData(player, money);
						    else
						    	Message(player, lang.GetMessage("ERROR", this, player.UserIDString));
			            }, this);
						
				    	break;
				    }				
				    case 1:
				    {
				    	if (RustStore) RustStore?.CallHook("APIChangeUserBalance", player.userID, (int)money, new Action<string>((result) =>
                        {    
                            if (result == "SUCCESS")
								TakeMoneyData(player, money);
							else
								Message(player, lang.GetMessage("ERROR", this, player.UserIDString));
                        }));
					    break;
				    }				
			    	case 2:
			    	{
				    	if(plugins.Find(config.Store.PluginName))
						{
							if(config.Store.Parameter == "player")
							{
							    plugins.Find(config.Store.PluginName).Call(config.Store.HookName, player, money);
								TakeMoneyData(player, money);
							}
							else if(config.Store.Parameter == "userID")
							{
								plugins.Find(config.Store.PluginName).Call(config.Store.HookName, player.userID, money);
								TakeMoneyData(player, money);
							}
						}
						else
							Message(player, lang.GetMessage("ERROR", this, player.UserIDString));
				    	break;
				    }
			    }
			}
		}
		
		private void TakeMoneyData(BasePlayer player, double money)
		{
			PrintWarning($"Игрок [{player.displayName} | {player.userID}] вывел на магазин {money}₽ !!!");				
						
	        StoredData[player.userID].Money = 0f;
			StoredData[player.userID].MoneyTake = DateTime.Now.Day;
						
			MoneyGUI(player);
			LogoGUI(player);
						
			Message(player, string.Format(lang.GetMessage("CHATE", this, player.UserIDString), money));
		}
		
		#endregion
		
		#region Commands
		
		[ConsoleCommand("money")]
		private void cmdConsoleCommand(ConsoleSystem.Arg args)
		{
            BasePlayer player = args.Player();
			
			Effect x = new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", player, 0, new Vector3(), new Vector3());
			EffectNetwork.Send(x, player.Connection);
			
			switch (args.Args[0].ToLower())
			{
				case "money":
				{
                    MoneyGUI(player);
					break;
				}				
				case "moneyinfo":
				{
                    Message(player, lang.GetMessage("CHATL", this, player.UserIDString));
					break;
				}				
				case "takemoney":
				{
					TakeMoney(player);
					break;
				}				
			}	
		}
		
		#endregion
		
		#region GUI
		
		private void LogoGUI(BasePlayer player)
		{
			CuiHelper.DestroyUi(player, ".Logo");
            CuiElementContainer container = new CuiElementContainer();
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = config.LogoGUI.AnchorMin, AnchorMax = config.LogoGUI.AnchorMax, OffsetMin = config.LogoGUI.OffsetMin, OffsetMax = config.LogoGUI.OffsetMax },
                Image = { Color = "0 0 0 0" }
            }, "Overlay", ".Logo");			
			
			container.Add(new CuiElement
            {
                Parent = ".Logo",
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "LogoImage"), Color = config.LogoGUI.ColorLogo },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" },
                }
            });
			
			container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 9.5" },
                Text = { Text = $"{StoredData[player.userID].Money}", Align = TextAnchor.MiddleCenter, FontSize = 14, Color = config.LogoGUI.ColorText }
            }, ".Logo");		
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"  },
                Button = { Color = "0 0 0 0", Command = "money money" },
                Text = { Text = "" }
            }, ".Logo");			
			
			CuiHelper.AddUi(player, container);
		}
		
		private void MoneyGUI(BasePlayer player)
		{
			CuiHelper.DestroyUi(player, ".Money");
            CuiElementContainer container = new CuiElementContainer();
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = config.GUI.AnchorMin, AnchorMax = config.GUI.AnchorMax, OffsetMin = config.GUI.OffsetMin, OffsetMax = config.GUI.OffsetMax },
                Image = { Color = config.GUI.ColorPanel }
            }, "Overlay", ".Money");
			
			container.Add(new CuiPanel 
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "2.5 67.5", OffsetMax = "-137.5 -2.5" },
                Image = { Color = config.GUI.ColorButton }
            }, ".Money", ".MoneyInfo");
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "2.5 2.5", OffsetMax = "-2.5 -2.5" },
                Button = { Color = "1 1 1 1", Sprite = "assets/icons/info.png", Command = "money moneyinfo" },
                Text = { Text = "" }
            }, ".MoneyInfo");			
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "137.5 67.5", OffsetMax = "-2.5 -2.5" },
                Image = { Color = config.GUI.ColorButton }
            }, ".Money", ".MoneyClose");
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "2.5 2.5", OffsetMax = "-2.5 -2.5" },
                Button = { Color = "1 1 1 1", Sprite = "assets/icons/close.png", Close = ".Money" },
                Text = { Text = "" }
            }, ".MoneyClose");
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "25 67.5", OffsetMax = "-25 -2.5"  },
                Button = { Color = config.GUI.ColorButton },
                Text = { Text = lang.GetMessage("GUIT", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 14 }
            }, ".Money");			
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "35 35", OffsetMax = "-35 -30"  },
                Button = { Color = config.GUI.ColorButton },
                Text = { Text = $"{StoredData[player.userID].Money}₽", Align = TextAnchor.MiddleCenter, FontSize = 16 }
            }, ".Money", ".MoneyValue");

			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 -22.5" },
                Image = { Color = "0.55 0.55 0.55 0.75" }
            }, ".MoneyValue", ".MoneyProgress");			
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = $"{1.0 / config.Reward.RewardMax * StoredData[player.userID].Money} 1", OffsetMax = "0 0" },
                Image = { Color = "0.55 0.85 0.55 0.75" }
            }, ".MoneyProgress");			
			
			container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "35 21.75", OffsetMax = "-35 -54.25" },
                Text = { Text = lang.GetMessage("GUIM", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 8 }
            }, ".Money");
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "32.5 5", OffsetMax = "-32.5 -70"  },
                Button = { Color = config.GUI.ColorButton, Command = "money takemoney" },
                Text = { Text = lang.GetMessage("GUIE", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 12 }
            }, ".Money");		
			
			CuiHelper.AddUi(player, container);
		}
		
		private void Message(BasePlayer player, string Messages)
        {
            CuiHelper.DestroyUi(player, ".Message");
            CuiElementContainer container = new CuiElementContainer();

			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 -50", OffsetMax = "0 -92.5" },
                Image = { Color = config.GUI.ColorPanel }
            }, ".Money", ".Message");			
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" },
                Image = { Color = config.GUI.ColorButton }
            }, ".Message", ".Main");
			
			container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Text = { Text = $"{Messages}", Align = TextAnchor.MiddleCenter, FontSize = 11 }
            }, ".Main", ".MainText");

            CuiHelper.AddUi(player, container);
        }
		
		#endregion
		
		#region Lang

        private void InitializeLang()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CHATNOM"] = "Not enough money for withdrawal!",						
                ["CHATDE"] = "A second conclusion can only be made tomorrow!",						
                ["CHATT"] = "Play again: {0}",						
                ["CHATE"] = "Successfully withdrawт {0}₽",						
                ["CHATL"] = "Do not forget to log in to the store before withdrawing!",
                ["ERROR"] = "Error! Maybe you are not authorized in the store.",					
                ["GUIT"] = "Money for online",						
                ["GUIM"] = "AVAILABLE",						
                ["GUIE"] = "RECEIVE"					
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CHATNOM"] = "Недостаточно денег для вывода!",						
                ["CHATDE"] = "Повторный вывод можно сделать только завтра!",						
                ["CHATT"] = "Играйте ещё: {0}",						
                ["CHATE"] = "Успешно выведено {0}₽",						
                ["CHATL"] = "Перед выводом не забудьте авторизоваться в магазине!",						
                ["ERROR"] = "Ошибка! Возможно вы не авторизованы в магазине.",						
                ["GUIT"] = "Деньги за онлайн",						
                ["GUIM"] = "ДОСТУПНО",						
                ["GUIE"] = "ПОЛУЧИТЬ"						
            }, this, "ru");
        }

        #endregion
	}
}