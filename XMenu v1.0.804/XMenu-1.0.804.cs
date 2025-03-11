using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Oxide.Game.Rust.Cui;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
 
namespace Oxide.Plugins
{
    [Info("XMenu", "Monster", "1.0.804")]
    class XMenu : RustPlugin
    {
		public bool eCargoShip; 
		public bool eCargoPlane;
		public bool eBradleyAPC;
		public bool eBaseHelicopter;
		public bool eCH47Helicopter;
		
		public List<BasePlayer> players = new List<BasePlayer>();
		
		#region Reference
		
		[PluginReference] private Plugin ImageLibrary, IQFakeActive, FGS;
		
		#endregion
		
		#region IQFakeActive
		
		int FakeOnline => (int)IQFakeActive?.Call("GetOnline");
		
		void SyncReservedFinish()
        {
            PrintWarning($"{Name} - успешно синхронизирована с IQFakeActive");
            PrintWarning("=============SYNC==================");
        }
		
		#endregion
		
		#region Configuration

        private MenuConfig config;
 
        private class MenuConfig
        {		
		    internal class GeneralSetting
			{
                [JsonProperty("Открытое меню после подключения")] public bool Connect;
                [JsonProperty("Отображать информацию других плагинов")] public bool APluginsInfo;
                [JsonProperty("Отображать ивенты")] public bool AEvents;
                [JsonProperty("Обновлять меню [ Обновляется только открытое меню ]")] public bool Reload;
                [JsonProperty("Обновлять информацию других плагинов [ Обновляется только при открытом меню ]")] public bool ReloadPluginsInfo;
                [JsonProperty("Интервал обновления открытого меню")] public float IReload;
                [JsonProperty("Фейк онлайн от плагина - [ Default - 0 | FGS - 1 | IQFakeActive - 2]")] public int FakeOnline;
			}			   

            internal class LogoSetting
			{
                [JsonProperty("Ссылка на картинку логотипа")] public string LogoURL;
                [JsonProperty("Цвет логотипа")] public string LogoColor;
                [JsonProperty("Материал логотипа")] public string LogoMaterial;
                [JsonProperty("Лого - AnchorMin")] public string AnchorMin;
                [JsonProperty("Лого - AnchorMax")] public string AnchorMax;
                [JsonProperty("Лого - OffsetMin")] public string OffsetMin;
                [JsonProperty("Лого - OffsetMax")] public string OffsetMax;                
				[JsonProperty("Лого в меню - AnchorMin")] public string MAnchorMin;
                [JsonProperty("Лого в меню - AnchorMax")] public string MAnchorMax;
                [JsonProperty("Лого в меню - OffsetMin")] public string MOffsetMin;
                [JsonProperty("Лого в меню - OffsetMax")] public string MOffsetMax;
			}	
		    
            internal class MenuSetting
			{
				[JsonProperty("Цвет меню")] public string MenuColor;
				[JsonProperty("Материал меню")] public string MenuMaterial;
				[JsonProperty("Цвет кнопок")] public string ButtonColor;
				[JsonProperty("Цвет текста кнопок")] public string ButtonTextColor;
				[JsonProperty("Размер текста кнопок")] public int ButtonSize;
				[JsonProperty("Закрывать меню после нажатия одной из кнопок")] public bool CloseMenu;
				[JsonProperty("Меню - AnchorMin")] public string MAnchorMin;
                [JsonProperty("Меню - AnchorMax")] public string MAnchorMax;
                [JsonProperty("Меню - OffsetMin")] public string MOffsetMin;
                [JsonProperty("Меню - OffsetMax")] public string MOffsetMax;				
				[JsonProperty("Инфа плагинов - AnchorMin")] public string PAnchorMin;
                [JsonProperty("Инфа плагинов - AnchorMax")] public string PAnchorMax;
                [JsonProperty("Инфа плагинов - OffsetMin")] public string POffsetMin;
                [JsonProperty("Инфа плагинов - OffsetMax")] public string POffsetMax;
			}    

		    internal class EventSetting
			{
				[JsonProperty("Ссылка на картинку ивента")] public string EventURL;
				[JsonProperty("Цвет активного ивента")] public string EventAColor;
				[JsonProperty("Цвет неактивного ивента")] public string EventDColor;
			}	 

            internal class EventsSetting	
			{
				[JsonProperty("Цвет меню ивентов")] public string EMenuColor;
				[JsonProperty("Материал меню ивентов")] public string EMenuMaterial;
				[JsonProperty("Цвет фона иконок ивентов")] public string EBackgroundColor;
				
				[JsonProperty("Настройка иконок ивентов")]
                public Dictionary<string, EventSetting> Events; 
			}            
			 
			internal class PluginsInfoSetting	
			{
                [JsonProperty("Название плагина")] public string PluginName;				
                [JsonProperty("Название метода(API)")] public string HookName;				
                [JsonProperty("Тип параметра хука - [ player | userID ]")] public string Parameter;				

                public PluginsInfoSetting(string pluginname, string hookname, string parameter)
				{
					PluginName = pluginname; HookName = hookname; Parameter = parameter;
				}				
			}			
			
			[JsonProperty("Общие настройки")]
            public GeneralSetting Setting;			
			[JsonProperty("Настройка логотипа")]
            public LogoSetting Logo;				
			[JsonProperty("Настройка меню")]
            public MenuSetting Menu;				
            [JsonProperty("Настройка кнопок [ Текст | Команда ]")] 
            public Dictionary<string, string> Button;            
            [JsonProperty("Настройка дополнительных кнопок [ Команда | Иконка ]")]
            public Dictionary<string, string> ButtonP;            
			[JsonProperty("Настройка ивентов")]
            public EventsSetting Event;				
			[JsonProperty("Настройка информации других плагинов. [ Хуки c типом параметра - player(BasePlayer) | userID(ulong) ]")]
            public List<PluginsInfoSetting> PluginsInfo;		
			
			public static MenuConfig GetNewConfiguration()
            {
                return new MenuConfig
                {
                    Setting = new GeneralSetting
                    {
						Connect = false,
						APluginsInfo = false,
						AEvents = true,
						Reload = false,
						ReloadPluginsInfo = false,
						IReload = 12.5f,
						FakeOnline = 0
                    },
					Logo = new LogoSetting
					{
						LogoURL = "https://i.imgur.com/Hh7W3hz.png",
						LogoColor = "1 1 1 1",
                        LogoMaterial = "assets/icons/greyout.mat",
						AnchorMin = "0 1",
						AnchorMax = "0 1",
						OffsetMin = "10 -78",
						OffsetMax = "80 -8",						
						MAnchorMin = "0 1",
						MAnchorMax = "0 1",
						MOffsetMin = "-35 -65.5",
						MOffsetMax = "35 4.5"
					},
                    Menu = new MenuSetting
                    {
						MenuColor = "1 0.27 0 0.5",
						MenuMaterial = "assets/icons/greyout.mat",
						ButtonColor = "0.217 0.221 0.209 0.75",
						ButtonTextColor = "1 1 1 1",
						ButtonSize = 9,
						CloseMenu = false,
						MAnchorMin = "0 1",
						MAnchorMax = "0 1",
						MOffsetMin = "45 -72.5",
						MOffsetMax = "400 -12.5",						
						PAnchorMin = "0 1",
						PAnchorMax = "0 1",
						POffsetMin = "357.5 -60",
						POffsetMax = "445 0"
                    },
					Button = new Dictionary<string, string>
					{
						["НАГРАДЫ"] = "chat.say /reward",
			            ["КАЛЕНДАРЬ"] = "chat.say /calendar",
			            ["МАГАЗИН"] = "chat.say /s",
			            ["КРАФТ"] = "chat.say /craft",  
			            ["ИНФА"] = "chat.say /info" 
					},					
					ButtonP = new Dictionary<string, string> 
					{
			            ["store"] = "assets/icons/open.png",
			            ["chat.say /s"] = "assets/icons/community_servers.png",
			            ["chat.say /stats"] = "assets/icons/market.png"
					},
					Event = new EventsSetting 
					{
						EMenuColor = "1 0.27 0 0.5",
						EMenuMaterial = "assets/icons/greyout.mat",
						EBackgroundColor = "0.217 0.221 0.209 0.75",
						Events = new Dictionary<string, EventSetting>
						{
							["CargoPlane"] = new EventSetting
							{
								EventURL = "https://i.imgur.com/UctMlPy.png",
								EventAColor = "1 0.5 0.5 1",
								EventDColor = "1 1 1 1"
							},
							["BaseHelicopter"] = new EventSetting
							{
								EventURL = "https://i.imgur.com/BrJrI8Q.png",
								EventAColor = "1 0.5 1 1",
								EventDColor = "1 1 1 1"
							},
				            ["CargoShip"] = new EventSetting
						    {
							    EventURL = "https://i.imgur.com/ff7ZCBI.png",
							    EventAColor = "0.5 0.5 1 1",
							    EventDColor = "1 1 1 1"
						    },
							["CH47Helicopter"] = new EventSetting
							{
						    	EventURL = "https://i.imgur.com/2PkTyzw.png",
						    	EventAColor = "0.5 1 1 1",
						    	EventDColor = "1 1 1 1"
						    },							
							["BradleyAPC"] = new EventSetting
							{
								EventURL = "https://i.imgur.com/Hzu44wb.png",
								EventAColor = "1 1 0.5 1",
								EventDColor = "1 1 1 1"
							}												
						}
					},
					PluginsInfo = new List<PluginsInfoSetting>
					{
						new PluginsInfoSetting("XShop", "API_GetBalance", "player"),
						new PluginsInfoSetting("XLevels", "API_GetLevel", "player")
					}
				};
			}
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
			
			try
			{
				config = Config.ReadObject<MenuConfig>(); 
			}
			catch
			{
				PrintWarning("Ошибка чтения конфигурации! Создание дефолтной конфигурации!");
				LoadDefaultConfig();
			}
			
			SaveConfig();
        }
		protected override void LoadDefaultConfig() => config = MenuConfig.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion
		
		#region Hooks
		
		private void OnServerInitialized()
		{
			PrintWarning("\n-----------------------------\n" +
			"     Author - Monster\n" +
			"     VK - vk.com/idannopol\n" +
			"     Discord - Monster#4837\n" +
			"     Config - v.2850\n" + 
			"-----------------------------");
			
			ImageLibrary.Call("AddImage", config.Logo.LogoURL, ".LogoIMG");
			foreach (var image in config.Event.Events)
			    ImageLibrary.Call("AddImage", image.Value.EventURL, $".{image.Key}");
			
			foreach (var entity in BaseNetworkable.serverEntities)
			{
				if (entity is CargoShip) 
					eCargoShip = true;				
				if (entity is CargoPlane)
					eCargoPlane = true;				
				if (entity is BradleyAPC)
					eBradleyAPC = true;				
				if (entity is BaseHelicopter)
					eBaseHelicopter = true;				
				if (entity is CH47Helicopter)
					eCH47Helicopter = true;
			}
			
			BasePlayer.activePlayerList.ToList().ForEach(OnPlayerConnected);
			
			if (config.Setting.Reload)
			    timer.Every(config.Setting.IReload, () => { 
			        foreach(var i in players)
				    {
					    GUIMenuInfo(i);
						if (config.Setting.AEvents)
					        GUIEvent(i);
						if(config.Setting.APluginsInfo && config.Setting.ReloadPluginsInfo) 
				            GUIPluginsInfo(i);
				    }
			    });
				
			InitializeLang();
		}
		
		private void Unload()
		{
			foreach (BasePlayer player in BasePlayer.activePlayerList)
			{
			    CuiHelper.DestroyUi(player, ".LogoGUI");
			    CuiHelper.DestroyUi(player, ".MenuGUI");
			}
		}
		
		private void OnPlayerConnected(BasePlayer player)
		{
			if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerConnected(player));
                return;
            }
			
			if(config.Setting.Connect)
			{
				GUILogo(player);
				GUIMenu(player);
					
				if (config.Setting.Reload)
					players.Add(player);
			}
			else
			    GUILogo(player);
		}
		
		private void OnPlayerDisconnected(BasePlayer player)
		{
			if (config.Setting.Reload)
			    players.Remove(player);
		}
		
		private void OnEntitySpawned(BaseNetworkable entity)
		{
			if (entity is CargoShip)
				eCargoShip = true;				
			if (entity is CargoPlane)
				eCargoPlane = true;				
			if (entity is BradleyAPC)
				eBradleyAPC = true;				
			if (entity is BaseHelicopter)
				eBaseHelicopter = true;				
			if (entity is CH47Helicopter)
				eCH47Helicopter = true;
		}		
		
		private void OnEntityKill(BaseNetworkable entity)
		{
			if (entity is CargoShip)
				eCargoShip = false;				
			if (entity is CargoPlane)
				eCargoPlane = false;				
			if (entity is BradleyAPC)
				eBradleyAPC = false;				
			if (entity is BaseHelicopter)
				eBaseHelicopter = false;				
			if (entity is CH47Helicopter)
				eCH47Helicopter = false;
		}
		
		#endregion
		
		#region Commands
		
		[ConsoleCommand("ui_menu")]
		void cmdOpenGUI(ConsoleSystem.Arg args)
		{
			BasePlayer player = args.Player();
			Effect x = new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", player, 0, new Vector3(), new Vector3());
			
			switch(args.Args[0])
			{
				case "open":
				{
					GUIMenu(player);
					
					if (config.Setting.Reload)
					    players.Add(player);
					break;
				}				 
				case "close":
				{
					if (config.Setting.Reload)
					    players.Remove(player); 
					break;
				}
			}
			
			EffectNetwork.Send(x, player.Connection);
		}
		
		#endregion
		
		#region GUI 
		
		private void GUILogo(BasePlayer player)
		{
			CuiHelper.DestroyUi(player, ".LogoGUI");
            CuiElementContainer container = new CuiElementContainer();
			
			MenuConfig.LogoSetting logo = config.Logo;
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = logo.AnchorMin, AnchorMax = logo.AnchorMax, OffsetMin = logo.OffsetMin, OffsetMax = logo.OffsetMax },
                Image = { Png = (string) ImageLibrary.Call("GetImage", ".LogoIMG"), Color = logo.LogoColor, Material = logo.LogoMaterial }
            }, "Overlay", ".LogoGUI");
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Command = "ui_menu open" },
                Text = { Text = "" }
            }, ".LogoGUI");
			
			CuiHelper.AddUi(player, container);
		}
		
		private void GUIMenu(BasePlayer player)
		{	
			CuiHelper.DestroyUi(player, ".MenuGUI");
            CuiElementContainer container = new CuiElementContainer();
			
			MenuConfig.MenuSetting menu = config.Menu;
			MenuConfig.LogoSetting logo = config.Logo;
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = menu.MAnchorMin, AnchorMax = menu.MAnchorMax, OffsetMin = menu.MOffsetMin, OffsetMax = menu.MOffsetMax },
                Image = { Color = menu.MenuColor, Material = menu.MenuMaterial }
            }, "Overlay", ".MenuGUI");			
			
			container.Add(new CuiPanel
            { 
                RectTransform = { AnchorMin = logo.MAnchorMin, AnchorMax = logo.MAnchorMax, OffsetMin = logo.MOffsetMin, OffsetMax = logo.MOffsetMax },
                Image = { Png = (string) ImageLibrary.Call("GetImage", ".LogoIMG"), Color = logo.LogoColor, Material = logo.LogoMaterial }
            }, ".MenuGUI");	
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "-35 -64.5", OffsetMax = "35 4.5" },
                Button = { Color = "0 0 0 0", Close = ".MenuGUI", Command = "ui_menu close" },
                Text = { Text = "" }
            }, ".MenuGUI");							
			
			container.Add(new CuiPanel  
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "40 -55", OffsetMax = "42.5 -5" },
                Image = { Color = "1 1 1 1" }
            }, ".MenuGUI");				
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "322.5 -55", OffsetMax = "325 -5" },
                Image = { Color = "1 1 1 1" }
            }, ".MenuGUI"); 
			
			int count = config.Button.Count, count1 = config.ButtonP.Count;
			
			foreach(var i in config.Button)
			{
				double offset = -(26 * count--) - (1.25 * count--);
				
				container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{offset + 5} -25", OffsetMax = $"{offset + 57} -10" },
                    Button = { Color = menu.ButtonColor, Command = i.Value, Close = menu.CloseMenu ? ".MenuGUI" : "" },
                    Text = { Text = "" }
                }, ".MenuGUI", ".BUTTON");
				
			    container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Text = { Text = lang.GetMessage(i.Key, this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = menu.ButtonSize, Color = menu.ButtonTextColor }
                }, ".BUTTON");
			}
			
			foreach(var i in config.ButtonP)
			{
				double offset = (6.5 * count1--) + (2.5 * count1--); 
				 
				container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"155 {offset - 14.25}", OffsetMax = $"170 {offset + 0.75}" },
                    Button = { Color = "1 1 1 1", Sprite = i.Value, Command = i.Key, Close = menu.CloseMenu ? ".MenuGUI" : "" },
                    Text = { Text = "" }
                }, ".MenuGUI", ".BUTTON");
			}
			
			CuiHelper.AddUi(player, container);
			
			GUIMenuInfo(player);
			if (config.Setting.AEvents)
			    GUIEvent(player);
			if(config.Setting.APluginsInfo)
				GUIPluginsInfo(player);
		}
		
		private void GUIMenuInfo(BasePlayer player)
		{	
			CuiHelper.DestroyUi(player, ".MenuInfoGUI");
            CuiElementContainer container = new CuiElementContainer();
			
			int online = config.Setting.FakeOnline == 1 && IQFakeActive ? FakeOnline : config.Setting.FakeOnline == 2 && FGS ? BasePlayer.activePlayerList.Count + (int)FGS?.CallHook("getFakes") : BasePlayer.activePlayerList.Count;
			
			container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "47.5 -40", OffsetMax = "318 -5" },
                Text = { Text = string.Format(lang.GetMessage("TITLE", this, player.UserIDString), online, ConVar.Server.maxplayers, ServerMgr.Instance.connectionQueue.Joining, BasePlayer.sleepingPlayerList.Count, TOD_Sky.Instance.Cycle.DateTime.ToString("HH:mm")), Align = TextAnchor.UpperCenter, FontSize = 13, Color = "1 1 1 1" }
            }, ".MenuGUI", ".MenuInfoGUI");
			
			CuiHelper.AddUi(player, container);
		}
		
		private void GUIEvent(BasePlayer player)
		{
			CuiHelper.DestroyUi(player, ".EventGUI"); 
            CuiElementContainer container = new CuiElementContainer();
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "112.25 -90", OffsetMax = "242.75 -62.5" },
                Image = { Color = config.Event.EMenuColor, Material = config.Event.EMenuMaterial }
            }, ".MenuGUI", ".EventGUI");
			
			int count = config.Event.Events.Count;
			
			foreach(var i in config.Event.Events) 
			{
				double offset = -(11.25 * count--) - (1.5 * count--);
				
				container.Add(new CuiPanel 
                { 
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{offset} -11.25", OffsetMax = $"{offset + 22.5} 11.25" },
                    Image = { Color = config.Event.EBackgroundColor }
                }, ".EventGUI", $".{i.Key}");
			}  
			
			if(config.Event.Events.ContainsKey("CargoShip"))
			    container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "1.75 1.75", OffsetMax = "-1.75 -1.75" },
                    Image = { Png = (string) ImageLibrary.Call("GetImage", ".CargoShip"), Color = eCargoShip ? config.Event.Events["CargoShip"].EventAColor : config.Event.Events["CargoShip"].EventDColor }
                }, ".CargoShip");			
			
			if(config.Event.Events.ContainsKey("CargoPlane"))
			    container.Add(new CuiPanel 
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "1.75 1.75", OffsetMax = "-1.75 -1.75" },
                    Image = { Png = (string) ImageLibrary.Call("GetImage", ".CargoPlane"), Color = eCargoPlane ? config.Event.Events["CargoPlane"].EventAColor : config.Event.Events["CargoPlane"].EventDColor }
                }, ".CargoPlane");			
			
			if(config.Event.Events.ContainsKey("BradleyAPC"))
			    container.Add(new CuiPanel
                { 
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "1.75 1.75", OffsetMax = "-1.75 -1.75" },
                    Image = { Png = (string) ImageLibrary.Call("GetImage", ".BradleyAPC"), Color = eBradleyAPC ? config.Event.Events["BradleyAPC"].EventAColor : config.Event.Events["BradleyAPC"].EventDColor }
                }, ".BradleyAPC");		 	 	
			
			if(config.Event.Events.ContainsKey("BaseHelicopter"))
			    container.Add(new CuiPanel  
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "1.75 1.75", OffsetMax = "-1.75 -1.75" },
                    Image = { Png = (string) ImageLibrary.Call("GetImage", ".BaseHelicopter"), Color = eBaseHelicopter ? config.Event.Events["BaseHelicopter"].EventAColor : config.Event.Events["BaseHelicopter"].EventDColor }
                }, ".BaseHelicopter");				
			
			if(config.Event.Events.ContainsKey("CH47Helicopter"))
			    container.Add(new CuiPanel 
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "1.75 1.75", OffsetMax = "-1.75 -1.75" },
                    Image = { Png = (string) ImageLibrary.Call("GetImage", ".CH47Helicopter"), Color = eCH47Helicopter ? config.Event.Events["CH47Helicopter"].EventAColor : config.Event.Events["CH47Helicopter"].EventDColor }
                }, ".CH47Helicopter");	 
			
			CuiHelper.AddUi(player, container); 
		}
		
		private void GUIPluginsInfo(BasePlayer player)
		{
			CuiHelper.DestroyUi(player, ".PluginsInfoGUI"); 
            CuiElementContainer container = new CuiElementContainer();			
			
			MenuConfig.MenuSetting menu = config.Menu;
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = menu.PAnchorMin, AnchorMax = menu.PAnchorMax, OffsetMin = menu.POffsetMin, OffsetMax = menu.POffsetMax },
                Image = { Color = "0 0 0 0" }
            }, ".MenuGUI", ".PluginsInfoGUI");						

            int y = 0, count = config.PluginsInfo.Count; 

            foreach(var plugininfo in config.PluginsInfo.Where(p => plugins.Find(p.PluginName)))
			{
				container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = $"0 {-18 - (y * 21)}", OffsetMax = $"0 {0 - (y * 21)}" },
                    Image = { Color = config.Menu.MenuColor, Material = config.Menu.MenuMaterial }
                }, ".PluginsInfoGUI", ".InfoText");
				
				if(plugininfo.Parameter == "player")
				    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                        Text = { Text = string.Format(lang.GetMessage(plugininfo.PluginName, this, player.UserIDString), plugins.Find(plugininfo.PluginName).CallHook(plugininfo.HookName, player)), Align = TextAnchor.MiddleCenter, FontSize = 11 }
                    }, ".InfoText");
				else if(plugininfo.Parameter == "userID")
					container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                        Text = { Text = string.Format(lang.GetMessage(plugininfo.PluginName, this, player.UserIDString), plugins.Find(plugininfo.PluginName).CallHook(plugininfo.HookName, player.userID)), Align = TextAnchor.MiddleCenter, FontSize = 11 }
                    }, ".InfoText");
					
				y++;
			}			
			
			CuiHelper.AddUi(player, container);
		}
		
		#endregion
		
		#region Lang
 
        void InitializeLang()
        {
			Dictionary<string, string> llang = new Dictionary<string, string>();		
				
			foreach(var button in config.Button)
				llang.Add(button.Key, button.Key);			
			foreach(var plugininfo in config.PluginsInfo)
				llang.Add(plugininfo.PluginName, "БАЛАНС: {0}₽");
			
			llang.Add("TITLE", "ДОБРО ПОЖАЛОВАТЬ НА МОЙ КРУТОЙ СЕРВЕР\n<size=10>ОНЛАЙН: {0}/{1} | ЗАХОДЯТ: {2} | СЛИПЕРЫ: {3} | ВРЕМЯ: {4}</size>");	
			 
            lang.RegisterMessages(llang, this);
            lang.RegisterMessages(llang, this, "ru");
        }

        #endregion
	}
}