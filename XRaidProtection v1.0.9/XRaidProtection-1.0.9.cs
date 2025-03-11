using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Oxide.Core.Plugins;
using System.Globalization;

///Скачано с дискорд сервера Rust Edit [PRO+]
///discord.gg/9vyTXsJyKR

namespace Oxide.Plugins
{
    [Info("XRaidProtection", "discord.gg/9vyTXsJyKR", "1.0.9")]
    class XRaidProtection : RustPlugin
	{	
		#region Reference
		
		[PluginReference] private Plugin IQChat;
		
        #endregion
		
	    #region Configuration

        private RaidConfig config;

        private class RaidConfig
        {													
			internal class MessageSetting
			{			
				[JsonProperty("Интервал между сообщениями. Мин - 10 сек")] public int TimeMessage;
			}			
			
			internal class TimeSetting
			{
				[JsonProperty("Начало защиты по МСК | Часы")] public int HourStart;				
				[JsonProperty("Начало защиты по МСК | Минуты")] public int MinuteStart;				
				[JsonProperty("Конец защиты по МСК | Часы")] public int HourEnd;				
				[JsonProperty("Конец защиты по МСК | Минуты")] public int MinuteEnd;
			}			
			
			internal class Settings
			{
				[JsonProperty("Звуковой эффект")] public string Effect;				
				[JsonProperty("Процент защиты для всех игроков. 1.0 - 100%")] public float Damage;				
				[JsonProperty("Включить звуковой эффект")] public bool TEffect;				
				[JsonProperty("Первые N дни активности защиты после вайпа")] public int PDays;				
				[JsonProperty("Защита только в первые N дней после вайпа")] public bool PDay;				
				[JsonProperty("Включить GUI сообщение")] public bool TGUIMessage;				
				[JsonProperty("Включить чат сообщение")] public bool TMessage;				
				[JsonProperty("0 - Защита только для игроков с пермишеном, 1 - Защита для всех игроков, 2 - Защита для игроков с пермишеном и для всех игроков")] public int TypeProtection;
			}			
			
			internal class GUISettings
			{
				[JsonProperty("AnchorMin")] public string AnchorMin;				
				[JsonProperty("AnchorMax")] public string AnchorMax;				
				[JsonProperty("OffsetMin")] public string OffsetMin;				
				[JsonProperty("OffsetMax")] public string OffsetMax;				
				[JsonProperty("Цвет текста")] public string ColorText;				
				[JsonProperty("Размер текста")] public int SizeText;				
				[JsonProperty("Использовать иконки")] public bool Icon;
			}			
			
			internal class PrefabSetting
			{
				[JsonProperty("Префабы")] public List<string> Prefabs;
			}			
			
			internal class PermisssionSetting
			{
				[JsonProperty("Процент защиты по пермишену")] public float PermisssionDamage;
			}
			
			[JsonProperty("Сообщения в чат и GUI")]
            public MessageSetting Message = new MessageSetting();			
			[JsonProperty("Настройка времени действия защиты")]
            public TimeSetting Time = new TimeSetting();			
			[JsonProperty("Общее")]
            public Settings Setting = new Settings();			
			[JsonProperty("Настройка GUI")]
            public GUISettings GUI = new GUISettings();			
			[JsonProperty("Список префабов которые будут под защитой")]
            public PrefabSetting Prefab = new PrefabSetting();
            [JsonProperty("Настройка пермишенов")]			
			public Dictionary<string, PermisssionSetting> Permisssion = new Dictionary<string, PermisssionSetting>();
			[JsonProperty("Дата вайпа")]
            public string DateWipe;
			[JsonProperty("Время по МСК")]			
			public DateTime MSCTime;

			public static RaidConfig GetNewConfiguration()
            {
                return new RaidConfig
                {
					Message = new MessageSetting
					{
						TimeMessage = 30
					},
					Time = new TimeSetting
					{
						HourStart = 22,
						MinuteStart = 0,
						HourEnd = 7,
						MinuteEnd = 0
					},
					Setting = new Settings
					{
						Effect = "assets/bundled/prefabs/fx/invite_notice.prefab",
						Damage = 0.5f,
						TEffect = true,
						PDays = 3,
						PDay = false,
						TGUIMessage = true,
						TMessage = false,
						TypeProtection = 1
						
					},					
					GUI = new GUISettings
					{
						AnchorMin = "0.5 0",
						AnchorMax = "0.5 0",
						OffsetMin = "-194.5 80",
						OffsetMax = "175.5 110",
						ColorText = "1 1 1 0.4",
						SizeText = 12,
						Icon = true
					},
					Prefab = new PrefabSetting
					{
					    Prefabs = new List<string>
					    {
						    "cupboard.tool.deployed",
							"wall.frame.shopfront.metal"
					    }
					},
					Permisssion = new Dictionary<string, PermisssionSetting>
					{
						["xraidprotection.vip"] = new PermisssionSetting
						{
							PermisssionDamage = 0.70f
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
				config = Config.ReadObject<RaidConfig>();
			}
			catch  
			{
				PrintWarning("Ошибка чтения конфигурации! Создание дефолтной конфигурации!");
				LoadDefaultConfig();
			}
			
			SaveConfig();
        }
		protected override void LoadDefaultConfig() => config = RaidConfig.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion
		
		#region Hooks
		
		private void OnServerInitialized()
		{		
			PrintWarning("\n-----------------------------\n" +
			"     Author - Monster\n" +
			"     VK - vk.com/idannopol\n" +
			"     Discord - Monster#4837\n" +
			"     Config - v.2857\n" +
			"-----------------------------");
			
			foreach(var perm in config.Permisssion)
			    permission.RegisterPermission(perm.Key, this);
			
			MSC();
			InitializeLang();
			config.DateWipe = SaveRestore.SaveCreatedTime.ToString("dd/MM/yyyy");
		}
		
		private void OnServerSave() => MSC();
		
		private void MSC()
		{
			webrequest.Enqueue("https://time100.ru/api.php", null, (code, response) =>
            {
                if (code != 200 || response == null) return;
				
			    config.MSCTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime().AddSeconds(double.Parse(response));
			    Config.WriteObject(config, true);
            }, this);
		} 
		
		private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
		{
			if (entity == null || info == null || info.InitiatorPlayer == null) return;
				
			if (config.Setting.TypeProtection == 0 || config.Setting.TypeProtection == 2)
				foreach(var perm in config.Permisssion)
					if(permission.UserHasPermission(entity.OwnerID.ToString(), perm.Key))
					{
					    Protection(entity, info, perm.Value.PermisssionDamage);
						
						return;
					}			
			
			if (config.Setting.TypeProtection == 1 || config.Setting.TypeProtection == 2)
				Protection(entity, info, 1.0f - config.Setting.Damage);		
		}
		
		private void Protection(BaseCombatEntity entity, HitInfo info, float damage)
		{
			if (Time())
			{
				BasePlayer player = info.InitiatorPlayer;
				
				Effect x = new Effect(config.Setting.Effect, player, 0, new Vector3(), new Vector3());
				
		        if (entity is Door || entity is BuildingBlock || entity is SimpleBuildingBlock || config.Prefab.Prefabs.Contains(entity.ShortPrefabName))
			    {				
					info.damageTypes.ScaleAll(damage);
					
					if (Cooldowns.ContainsKey(player))
                        if (Cooldowns[player].Subtract(DateTime.Now).TotalSeconds >= 0) return;
				
		            if (config.Setting.TEffect) EffectNetwork.Send(x, player.Connection);
			        if (config.Setting.TGUIMessage) Message(player);
			        if (config.Setting.TMessage) 
				    	if (IQChat)
				            IQChatPuts(player, lang.GetMessage("CHAT_MESSAGE", this, player.UserIDString));
			            else
			        	    SendReply(player, lang.GetMessage("CHAT_MESSAGE", this, player.UserIDString));
					
				    if (config.Message.TimeMessage < 10)
			    		Cooldowns[player] = DateTime.Now.AddSeconds(10);
				    else
			    		Cooldowns[player] = DateTime.Now.AddSeconds(config.Message.TimeMessage);
			    }
			}
		}
		
		#endregion
		
		#region DateTime
		
		private Dictionary<BasePlayer, DateTime> Cooldowns = new Dictionary<BasePlayer, DateTime>();
		
		private bool Time()
		{ 
			if(config.Setting.PDay)
			{
				DateTime days = config.MSCTime - DateTime.ParseExact(config.DateWipe, "dd/MM/yyyy", CultureInfo.InvariantCulture).Subtract(new DateTime(1970, 1, 1));
				int d = days.Subtract(new DateTime(1970, 1, 1)).Days;
				 
				return config.Setting.PDays >= d;
			}
			else
			{
				var Now = config.MSCTime.TimeOfDay;
				var On = new TimeSpan(config.Time.HourStart, config.Time.MinuteStart, 0);
				var Off = new TimeSpan(config.Time.HourEnd, config.Time.MinuteEnd, 0);
			
				if (On < Off)
					return On <= Now && Now <= Off;
     
				return !(Off < Now && Now < On);
			}
		}
		
		#endregion 
		 
		#region Message

        private void Message(BasePlayer player) 
        {
            CuiHelper.DestroyUi(player, ".MessagePanel");
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiLabel
            {
				FadeOut = 0.75f,
                RectTransform = { AnchorMin = config.GUI.AnchorMin, AnchorMax = config.GUI.AnchorMax, OffsetMin = config.GUI.OffsetMin, OffsetMax = config.GUI.OffsetMax },
                Text = { FadeIn = 0.75f, Text = lang.GetMessage("UI_MESSAGE", this, player.UserIDString), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = config.GUI.SizeText, Color = config.GUI.ColorText }
            }, "Hud", ".MessagePanel");
			
			if (config.GUI.Icon) 
			{
			    container.Add(new CuiButton
                {
				    FadeOut = 0.75f,
                    RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-180 2", OffsetMax = "-154 28" },
                    Button = { Color = "0.8 0.5 0.5 0.8", Sprite = "assets/icons/vote_down.png" },
                    Text = { Text = "" }
                }, ".MessagePanel", ".Icon1");			
			
			    container.Add(new CuiButton
                {
				    FadeOut = 0.75f,
                    RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "154 2", OffsetMax = "180 28" },
                    Button = { Color = "0.8 0.5 0.5 0.8", Sprite = "assets/icons/vote_down.png" },
                    Text = { Text = "" }
                }, ".MessagePanel", ".Icon2");
			}

            CuiHelper.AddUi(player, container);  

            timer.Once(7.5f, () => { CuiHelper.DestroyUi(player, ".MessagePanel"); CuiHelper.DestroyUi(player, ".Icon1"); CuiHelper.DestroyUi(player, ".Icon2"); });
        }

        #endregion
		
		#region Lang
 
        private void InitializeLang()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["UI_MESSAGE"] = "NIGHT RIDING PROTECTION ACTIVE: 50%. FROM 23:15 TO 08:00!",
                ["CHAT_MESSAGE"] = "NIGHT RIDING PROTECTION ACTIVE: 50%. FROM 23:15 TO 08:00!"
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["UI_MESSAGE"] = "АКТИВНА НОЧНАЯ ЗАЩИТА ОТ РЕЙДА: 50%. С 23:15 ДО 08:00!",
                ["CHAT_MESSAGE"] = "АКТИВНА НОЧНАЯ ЗАЩИТА ОТ РЕЙДА: 50%. С 23:15 ДО 08:00!"
            }, this, "ru");
        }

        #endregion
		
		#region IQChat API
		
		private void IQChatPuts(BasePlayer player, string Message) => IQChat?.Call("API_ALERT_PLAYER", player, Message);
		
		#endregion
	}
}