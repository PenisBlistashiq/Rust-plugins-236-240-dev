using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;
using Oxide.Game.Rust.Cui;
using Newtonsoft.Json;
using Oxide.Core.Plugins;

  
///Скачано с дискорд сервера Rust Edit [PRO+]
///discord.gg/9vyTXsJyKR

namespace Oxide.Plugins
{
    [Info("XInfoMenu", "discord.gg/9vyTXsJyKR", "1.0.3")]
    class XInfoMenu : RustPlugin 
    { 
		#region Reference
		
		[PluginReference] private Plugin ImageLibrary;
		
		#endregion
		
		#region Configuration

        private InfoMenuConfig config; 

        private class InfoMenuConfig
        {		
		    internal class GeneralSetting
			{
				[JsonProperty("Открывать меню когда игрок зашел на сервер")] public bool ConnectOpen; 
				[JsonProperty("Перезагружать картинки после перезагрузки плагина")] public bool ReloadImage; 
				[JsonProperty("Отображать кнопки переключения страниц если в категории только одна страница")] public bool ButtonNext; 
				[JsonProperty("КД действий с меню")] public float Cooldowns;
			}	 

            internal class MessageSetting
			{
				[JsonProperty("Список уникальных имен сообщений - [ Настройка текста в lang ]")] public List<string> ListMessage;
				[JsonProperty("Интервал сообщений в чат")] public float TimeMessage;
				[JsonProperty("SteamID профиля для кастомной аватарки")] public ulong SteamID;
				[JsonProperty("Включить сообщения в чат")] public bool MessageUse;
			}            
			
			internal class PanelMessageSetting
			{
				[JsonProperty("Список уникальных имен сообщений - [ Настройка текста в lang ]")] public List<string> ListMessage;
				[JsonProperty("Интервал сообщений под слотами")] public float TimeMessage;
				[JsonProperty("Включить сообщения под слотами")] public bool MessageUse;
			}			    
			
			internal class GUISetting
			{					
				[JsonProperty("Цвет фона_1")] public string ColorBackgroundO;					
				[JsonProperty("Цвет фона_2")] public string ColorBackgroundT;
				[JsonProperty("Материал фона_1")] public string MaterialBackgroundO;
				[JsonProperty("Материал фона_2")] public string MaterialBackgroundT;
				[JsonProperty("Цвет кнопок")] public string ColorButton;
				[JsonProperty("Цвет активной кнопки")] public string ColorAButton;
			}			

			internal class ElementSetting      
			{
				[JsonProperty("Цвет элемента")] public string ColorElement;
				[JsonProperty("Материал элемента")] public string MaterialElement;
				[JsonProperty("OffsetMin")] public string OffsetMinElement;
				[JsonProperty("OffsetMax")] public string OffsetMaxElement;
				
				public ElementSetting(string color, string material, string omin, string omax)
				{
					ColorElement = color; MaterialElement = material; OffsetMinElement = omin; OffsetMaxElement = omax;
				}
			}				
				
			internal class ImageSetting
			{
				[JsonProperty("Уникальное имя картинки")] public string NameImage;
				[JsonProperty("Ссылка на картинку")] public string URLImage;
				[JsonProperty("OffsetMin")] public string OffsetMinImage;
				[JsonProperty("OffsetMax")] public string OffsetMaxImage;
			
				public ImageSetting(string nameimage, string urlimage, string omin, string omax)
				{
					NameImage = nameimage; URLImage = urlimage; OffsetMinImage = omin; OffsetMaxImage = omax;
				}
			}				
				
			internal class TextSetting
			{
				[JsonProperty("Уникальное имя текста блока - [ Настройка текста в lang ]")] public string NameText;
				[JsonProperty("OffsetMin")] public string OffsetMinText;
				[JsonProperty("OffsetMax")] public string OffsetMaxText;
				[JsonProperty("TextAnchor [ Выравнивание текста ] | 0 - 8")] public int TextAnchor;
				[JsonProperty("Цвет текста")] public string TextColor;
					
				public TextSetting(string nametext, string omin, string omax, int ta, string tcolor)
				{
					NameText = nametext; OffsetMinText = omin; OffsetMaxText = omax; TextAnchor = ta; TextColor = tcolor;
				}
			} 

            internal class ButtonSetting
			{
				[JsonProperty("Уникальное имя кнопки - [ Настройка текста в lang ]")] public string NameButton;
				[JsonProperty("OffsetMin")] public string OffsetMinButton;
				[JsonProperty("OffsetMax")] public string OffsetMaxButton;
				[JsonProperty("TextAnchor [ Выравнивание текста ] | 0 - 8")] public int ButtonAnchor;
				[JsonProperty("Команда")] public string ButtonCommand;
				[JsonProperty("Цвет кнопки")] public string ButtonColor;
				[JsonProperty("Цвет текста")] public string TextColor;
				
				public ButtonSetting(string namebutton, string omin, string omax, int ba, string bc,string bcolor, string tcolor)
				{
					NameButton = namebutton; OffsetMinButton = omin; OffsetMaxButton = omax; ButtonAnchor = ba; ButtonCommand = bc; ButtonColor = bcolor; TextColor = tcolor;
				}
			}			

            internal class IMenuSetting 
			{	
			    [JsonProperty("Настройка элементов")] public List<ElementSetting> Element;
				[JsonProperty("Настройка картинки")] public List<ImageSetting> Image;
				[JsonProperty("Настройка блока текста")] public List<TextSetting> Text;
				[JsonProperty("Настройка кнопок")] public List<ButtonSetting> Button;
				
				public IMenuSetting(List<ElementSetting> element, List<ImageSetting> image, List<TextSetting> text, List<ButtonSetting> button)
				{
					Element = element; Image = image; Text = text; Button = button;
				}
			}    			
			
			[JsonProperty("Общие настройки")]
            public GeneralSetting Setting;	
			[JsonProperty("Настройка сообщений в чат")]
            public MessageSetting Message;			
			[JsonProperty("Настройка сообщений под слотами")]
            public PanelMessageSetting PanelMessage;				
			[JsonProperty("Настройки GUI")]
            public GUISetting GUI;			           
            [JsonProperty("Настройка кнопок и страниц")]
            public Dictionary<string, List<IMenuSetting>> IMenu;            	
			
			public static InfoMenuConfig GetNewConfiguration()
            {
                return new InfoMenuConfig  
                {
					Setting = new GeneralSetting      
					{
						ConnectOpen = false,
						ReloadImage = true,  
						ButtonNext = true,  
						Cooldowns = 0.75f 
					},    
					Message = new MessageSetting
					{
						ListMessage = new List<string>
						{
							"СООБЩЕНИЕ1.1", "СООБЩЕНИЕ1.2", "СООБЩЕНИЕ1.3"
						}, 
						TimeMessage = 300.0f,
						SteamID = 0,
						MessageUse = true
					},					
					PanelMessage = new PanelMessageSetting
					{
						ListMessage = new List<string>
						{
							"СООБЩЕНИЕ2.1", "СООБЩЕНИЕ2.2", "СООБЩЕНИЕ2.3"
						}, 
						TimeMessage = 30.0f,
						MessageUse = false
					},
					GUI = new GUISetting   
					{
						ColorBackgroundO = "0.517 0.521 0.509 0.95",
						ColorBackgroundT = "0.217 0.221 0.209 0.95",
						MaterialBackgroundO = "assets/icons/greyout.mat", 
						MaterialBackgroundT = "",
						ColorButton = "0.517 0.521 0.509 0.5",
						ColorAButton = "0.53 0.77 0.35 1"
					},
					IMenu = new Dictionary<string, List<IMenuSetting>> 
					{
						["ПРАВИЛА"] = new List<IMenuSetting>
						{
							new IMenuSetting(new List<ElementSetting>{ new ElementSetting("0.517 0.521 0.509 0.95", "assets/icons/greyout.mat", "-190 -11.25", "-185 198.75"), new ElementSetting("0.517 0.521 0.509 0.95", "assets/icons/greyout.mat", "-395 -11.25", "395 -6.25") }, new List<ImageSetting>{ new ImageSetting("ПРАВИЛА1.1", "https://i.imgur.com/VSrY1ZK.png", "-395 -1.25", "-195 198.75") }, new List<TextSetting>{ new TextSetting("ПРАВИЛА2.1", "-180 -1.25", "395 198.75", 4, "0.75 0.75 0.75 0.75"), new TextSetting("ПРАВИЛА2.2", "-395 -169", "395 -16.75", 4, "0.75 0.75 0.75 0.75") }, new List<ButtonSetting>{})
						},												
						["ИНФА"] = new List<IMenuSetting>
						{
							new IMenuSetting(new List<ElementSetting>{ new ElementSetting("0.517 0.521 0.509 0.95", "assets/icons/greyout.mat", "-190 -11.25", "-185 198.75"), new ElementSetting("0.517 0.521 0.509 0.95", "assets/icons/greyout.mat", "-395 -11.25", "395 -6.25") }, new List<ImageSetting>{ new ImageSetting("ИНФА1.1", "https://i.imgur.com/VSrY1ZK.png", "-395 -1.25", "-195 198.75") }, new List<TextSetting>{ new TextSetting("ИНФА2.1", "-180 -1.25", "395 198.75", 4, "0.75 0.75 0.75 0.75"), new TextSetting("ИНФА2.2", "-395 -169", "395 -16.75", 4, "0.75 0.75 0.75 0.75") }, new List<ButtonSetting>{})
						},												
						["ПЛАГИНЫ"] = new List<IMenuSetting>
						{
							new IMenuSetting(new List<ElementSetting>{ new ElementSetting("0.517 0.521 0.509 0.95", "assets/icons/greyout.mat", "-190 -11.25", "-185 198.75"), new ElementSetting("0.517 0.521 0.509 0.95", "assets/icons/greyout.mat", "-395 -11.25", "395 -6.25") }, new List<ImageSetting>{ new ImageSetting("ПЛАГИНЫ1.1", "https://i.imgur.com/VSrY1ZK.png", "-395 -1.25", "-195 198.75") }, new List<TextSetting>{ new TextSetting("ПЛАГИНЫ2.1", "-180 -1.25", "395 198.75", 4, "0.75 0.75 0.75 0.75"), new TextSetting("ПЛАГИНЫ2.2", "-395 -169", "395 -16.75", 4, "0.75 0.75 0.75 0.75") }, new List<ButtonSetting>{ new ButtonSetting("ПЛАГИНЫ3.1", "-180 168.75", "-100 198.75", 4, "", "0.517 0.521 0.509 0.5", "0.75 0.75 0.75 0.75") })
						}
					}
				};
			}
        }
  
        protected override void LoadConfig()
        {
            base.LoadConfig();
			
			try
			{
				config = Config.ReadObject<InfoMenuConfig>(); 
			}
			catch
			{
				PrintWarning("Ошибка чтения конфигурации! Создание дефолтной конфигурации!");
				LoadDefaultConfig();
			}
			
			SaveConfig();
        }
		protected override void LoadDefaultConfig() => config = InfoMenuConfig.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion
		
		private Dictionary<BasePlayer, DateTime> Cooldowns = new Dictionary<BasePlayer, DateTime>();
		
		#region Commands
		
		[ChatCommand("help")]
		private void cmdOpenGUI(BasePlayer player) => GUIBackground(player);		
		
		[ChatCommand("info")]
		private void cmdOpenGUII(BasePlayer player) => GUIBackground(player);
		
		[ConsoleCommand("page")]
		private void ccmdPage(ConsoleSystem.Arg args)
		{
			BasePlayer player = args.Player();     
			if (Cooldowns.ContainsKey(player))
				if (Cooldowns[player].Subtract(DateTime.Now).TotalSeconds >= 0) return;
			
			Effect x = new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", player, 0, new Vector3(), new Vector3());
			
			int page = int.Parse(args.Args[1]);
			int Page = int.Parse(args.Args[2]);
			
			switch (args.Args[0])
			{
				case "page_b":
				{
					GUIPage(player, int.Parse(args.Args[1]), 0);
					GUIButton(player, int.Parse(args.Args[1]));
					break;
				}
				case "next":
				{
					GUIPage(player, page, Page + 1);	
					break;
				}						
				case "back":
				{
					GUIPage(player, page, Page - 1);
					break;
				}
			}
			
			Cooldowns[player] = DateTime.Now.AddSeconds(config.Setting.Cooldowns); 
			EffectNetwork.Send(x, player.Connection);
		}
		
		#endregion
		
		#region Hooks
		
		private void OnServerInitialized()
		{
			PrintWarning("\n-----------------------------\n" +
			"     Author - Monster\n" +
			"     VK - vk.com/idannopol\n" +
			"     Discord - Monster#4837\n" +
			"     Config - v.1113\n" +
			"-----------------------------");
			
			foreach (var image in config.IMenu)   
			    foreach(var iimage in image.Value)
			        iimage.Image.ForEach(i =>
                    { 
						if (!config.Setting.ReloadImage)
						{
					        if (!ImageLibrary.Call<bool>("HasImage", i.NameImage + 300))
						        ImageLibrary.Call("AddImage", i.URLImage, i.NameImage + 300);
						}
						else
							ImageLibrary.Call("AddImage", i.URLImage, i.NameImage + 300);
                    });
				
			if (config.Message.MessageUse)
				Broadcast();			
			if (config.PanelMessage.MessageUse)
				PanelBroadcast();
			
			InitializeLang();   
		}
		 
		private void Unload()
		{
			foreach (BasePlayer player in BasePlayer.activePlayerList)
			{
			    CuiHelper.DestroyUi(player, ".IMenuGUI");
				CuiHelper.DestroyUi(player, ".MainText");
			}
		} 
		
		private void Broadcast()     
		{
			foreach (BasePlayer player in BasePlayer.activePlayerList)
			    Player.Reply(player, lang.GetMessage(config.Message.ListMessage.GetRandom(), this, player.UserIDString), config.Message.SteamID); 
			
			timer.Once(config.Message.TimeMessage, Broadcast);
		}
		
		private void PanelBroadcast() 
		{
			foreach (BasePlayer player in BasePlayer.activePlayerList)
			    PanelMGUI(player);
			
			timer.Once(config.PanelMessage.TimeMessage, PanelBroadcast);
		}
		
		private void OnPlayerConnected(BasePlayer player)
		{
			if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerConnected(player));
                return;
            }
			
			if (config.Setting.ConnectOpen)
			    GUIBackground(player);
			if (config.PanelMessage.MessageUse)
				PanelMGUI(player);
		}
		
		#endregion
		
		#region GUI

        private void GUIBackground(BasePlayer player)
		{
			CuiHelper.DestroyUi(player, ".IMenuGUI");
            CuiElementContainer container = new CuiElementContainer();

			container.Add(new CuiPanel
            {
				CursorEnabled = true,
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-405 -212.5", OffsetMax = "405 290" },
                Image = { Color = config.GUI.ColorBackgroundO, Material = config.GUI.MaterialBackgroundO } 
            }, "Overlay", ".IMenuGUI");			
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" },
                Image = { Color = config.GUI.ColorBackgroundT, Material = config.GUI.MaterialBackgroundT } 
            }, ".IMenuGUI", ".IMenuTGUI");			

			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "366.5 213.75", OffsetMax = "395 241.25" },
                Button = { Color = "1 1 1 0.75", Sprite = "assets/icons/close.png", Close = ".IMenuGUI" },
                Text = { Text = "" }
            }, ".IMenuGUI");			

		    container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-400 210", OffsetMax = "361.5 245" },
                Text = { Text = lang.GetMessage("Title", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 18, Color = "1 1 1 0.75" }
            }, ".IMenuGUI");				
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "356.5 205", OffsetMax = "361.5 250" },
                Image = { Color = config.GUI.ColorBackgroundO, Material = config.GUI.MaterialBackgroundO }
            }, ".IMenuTGUI");			
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-400 -196.25", OffsetMax = "400 -191.25" },
                Image = { Color = config.GUI.ColorBackgroundO, Material = config.GUI.MaterialBackgroundO }
            }, ".IMenuTGUI");			
			  
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-400 203.75", OffsetMax = "400 208.75" },
                Image = { Color = config.GUI.ColorBackgroundO, Material = config.GUI.MaterialBackgroundO }              
            }, ".IMenuTGUI");
			  
			CuiHelper.AddUi(player, container);
			
			GUIPage(player, 0, 0);
			GUIButton(player, 0);
		}

		private void GUIPage(BasePlayer player, int page, int Page)
		{
			CuiHelper.DestroyUi(player, ".Page");
            CuiElementContainer container = new CuiElementContainer();
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0" }
            }, ".IMenuTGUI", ".Page");	
			
			foreach(var i in config.IMenu.ElementAt(page).Value.Skip(Page * 1).Take(1)) 
			{
				foreach(var text in i.Text) 
				    container.Add(new CuiLabel
                    { 
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = text.OffsetMinText, OffsetMax = text.OffsetMaxText },
                        Text = { Text = lang.GetMessage(text.NameText, this, player.UserIDString), Align = (TextAnchor)text.TextAnchor, Font = "robotocondensed-regular.ttf", FontSize = 15, Color = text.TextColor }
                    }, ".Page");							
				 
				foreach(var image in i.Image)
				{
				    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = image.OffsetMinImage, OffsetMax = image.OffsetMaxImage },
                        Image = { Color = config.GUI.ColorButton, Material = config.GUI.MaterialBackgroundO }
                    }, ".Page", ".Image");

			        container.Add(new CuiPanel 
                    { 
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "25 25", OffsetMax = "-25 -25" },
                        Image = { Png = (string) ImageLibrary.Call("GetImage", image.NameImage + 300), Color = "1 1 1 1", Material = "assets/icons/greyout.mat" }
                    }, ".Image");
				}				
				
				foreach(var element in i.Element)   
				    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = element.OffsetMinElement, OffsetMax = element.OffsetMaxElement },
                        Image = { Color = element.ColorElement, Material = element.MaterialElement } 
                    }, ".Page");				
					
				foreach(var button in i.Button)
			    	container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = button.OffsetMinButton, OffsetMax = button.OffsetMaxButton },
                        Button = { Color = button.ButtonColor, Command = button.ButtonCommand },
                        Text = { Text = lang.GetMessage(button.NameButton, this, player.UserIDString), Align = (TextAnchor)button.ButtonAnchor, Font = "robotocondensed-regular.ttf", FontSize = 15, Color = button.TextColor }
                    }, ".Page");				
				
				#region Page
				
				if(!config.Setting.ButtonNext)
				{
					if(config.IMenu.ElementAt(page).Value.Count > 1)
						NextTick(() => GUIPageNext(player, page, Page));
				}
				else
					NextTick(() => GUIPageNext(player, page, Page));
				
				#endregion
			}
			
			CuiHelper.AddUi(player, container);
		}
		
		private void GUIPageNext(BasePlayer player, int page, int Page)  
		{
			CuiElementContainer container = new CuiElementContainer();
			
				bool back = Page != 0;
				bool next = config.IMenu.ElementAt(page).Value.Count > (Page + 1);
				
				container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 -200", OffsetMax = "0 -160" },
                    Button = { Color = "0 0 0 0", Command = back ? $"page back {page} {Page}" : "" },
                    Text = { Text = "<", Align = TextAnchor.MiddleCenter, FontSize = 26, Font = "robotocondensed-regular.ttf", Color = back ? "1 1 1 0.75" : "1 1 1 0.1" }
                }, ".Page");	

			    container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-15 -200", OffsetMax = "15 -160" },
                    Text = { Text = $"{Page + 1}", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 18, Color = "1 1 1 0.75" }
                }, ".Page");					
			
			    container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "0 -200", OffsetMax = "50 -160" },
                    Button = { Color = "0 0 0 0", Command = next ? $"page next {page} {Page}" : "" },
                    Text = { Text = ">", Align = TextAnchor.MiddleCenter, FontSize = 26, Font = "robotocondensed-regular.ttf", Color = next ? "1 1 1 0.75" : "1 1 1 0.1" }
                }, ".Page");
			
			CuiHelper.AddUi(player, container);
		}
		
		private void GUIButton(BasePlayer player, int page)  
		{
			CuiHelper.DestroyUi(player, ".Button");
            CuiElementContainer container = new CuiElementContainer();
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "0 -241.25", OffsetMax = "0 -201.25" },
                Image = { Color = "0 0 0 0" }
            }, ".IMenuTGUI", ".Button"); 
			  
			int x = 0;
			int count = config.IMenu.Count;
			 
			foreach(var button in config.IMenu)
			{
				string color = page == x ? config.GUI.ColorAButton : "0 0 0 0";
				double offset = -(63.75 * count--) - (2.5 * count--);
				
				container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{127.5 + offset - 127.5} -20", OffsetMax = $"{127.5 + offset} 20" },
                    Button = { Color = config.GUI.ColorButton, Command = $"page page_b {x} 0" },
                    Text = { Text = lang.GetMessage(button.Key, this, player.UserIDString), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 16, Color = "0.75 0.75 0.75 0.75" }
                }, ".Button", ".BButton");
 
			    container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMax = "0 1" },
                    Image = { Color = color, Material = "assets/icons/greyout.mat" }
                }, ".BButton");				
				
				x++;
			}
			
			CuiHelper.AddUi(player, container);
		}
		
		private void PanelMGUI(BasePlayer player)
		{
			CuiHelper.DestroyUi(player, ".MainText");
            CuiElementContainer container = new CuiElementContainer();
			
			container.Add(new CuiElement
            {
                Parent = "Hud",
				Name = ".MainText",
                Components =
                {
			        new CuiTextComponent { Text = lang.GetMessage(config.PanelMessage.ListMessage.GetRandom(), this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-234.5 0", OffsetMax = "215.5 17.5" },
					new CuiOutlineComponent { Color = "0 0 0 1", Distance = "0.5 0.5" }
                }
            });
			
			CuiHelper.AddUi(player, container);
		}
		
		#endregion
		
		#region Lang
 
        void InitializeLang()
        {
			Dictionary<string, string> llang = new Dictionary<string, string>();		
				
			foreach(var imenu in config.IMenu)
			{
				llang.Add(imenu.Key, "BUTTON");
				
			    foreach(var menu in imenu.Value)
				{
				    foreach(var i in menu.Text)
			            llang.Add(i.NameText, "TEXT TEXT TEXT TEXT");	
					foreach(var i in menu.Button)
					    llang.Add(i.NameButton, "BUTTON");	
				}
			}
			
			foreach(var message in config.Message.ListMessage)
			    llang.Add(message, "MESSAGE MESSAGE MESSAGE");			
			foreach(var message in config.PanelMessage.ListMessage)
			    llang.Add(message, "MESSAGE MESSAGE MESSAGE");
			
			llang.Add("Title", "TITLE TITLE TITLE");	
			 
            lang.RegisterMessages(llang, this);
            lang.RegisterMessages(llang, this, "ru");
            lang.RegisterMessages(llang, this, "es-ES");
        }

        #endregion
	}
}