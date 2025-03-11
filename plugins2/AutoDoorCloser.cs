using System;
using Oxide.Core;
using System.Linq;
using UnityEngine;
using System.Text;
using System.Reflection;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using Oxide.Core.Configuration;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("AutoDoorCloser", "Ryamkk", "1.0.5", ResourceId = 1924)]
      //  Слив плагинов server-rust by Apolo YouGame
	[Description("Переработка плагина AutoDoor с oxide. Почти схожая версия плагина с Moscow.ovh :)")]
	/*
	* Это моя первая пробная работа (доделка плагина) не судите строго :)
	*
	* Планы на следующие обновления:
	* Добавить конфигурацию. ✔
    * Добавить блокировку автоматического закрытия дверей при реидблоке. ✔
    * Добавить блокировку автоматического закрытия дверей при смерти.
    * Добавить отдельный параметр закрытия ворот и ставень.
	*
	*/
    class AutoDoorCloser : RustPlugin
    {
	    [PluginReference]
        Plugin NoEscape;
		
		#region Variables
        readonly DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetFile("AutoDoorCloser");
        List<string> playerPrefs = new List<string>();
		bool CancelOnKill = false;
		#endregion
		
		#region Variables Configuration
		private string Permission; // Названия привилегии для использования команды.
		private bool PermissionSupport; // Разрешить использования функционала плагина только тем игрокам у которых есть привилегия.
		private int DelayDoor; // Задержка перед автоматическим закрытием дверей (в секундах).
		private bool UseRaidBlocker; //Разрешить/Запретить использования автоматического закрытия дверей при РеидБлоке.
		#endregion
		
		#region Configuration
        protected override void LoadDefaultConfig()
        {
            GetVariable(Config, "A. Задержка перед автоматическим закрытием дверей (в секундах).", out DelayDoor, 10);
            GetVariable(Config, "B. (Разрешить/Запретить) использования автоматического закрытия дверей при РеидБлоке.", out UseRaidBlocker, false);
			GetVariable(Config, "C. (Разрешить/Запретить) использования функционала плагина только тем игрокам у которых есть привилегия.", out PermissionSupport, false);
			GetVariable(Config, "D. Названия привилегии для использования команды.", out Permission, "autodoorcloser.access");	
            SaveConfig();
        }
        #endregion

		#region OxideCore
		void OnDoorOpened(Door door, BasePlayer player)
        {
            if (door == null || !door.IsOpen() || door.OwnerID == 0 || door.LookupPrefab().name.Contains("shutter")) return;
            if (PermissionSupport && !IsAllowed(player.UserIDString, Permission)) return;
			if (IsRaidBlocked(player.UserIDString))
			{
                SendReply(player, GetMessage("RAID.BLOCKED", player));
                return;				
			}

            if (playerPrefs.Contains(player.UserIDString)) return;

            timer.Once(DelayDoor, () =>
            {
                if (!door || !door.IsOpen()) return;
                if (CancelOnKill && player.IsDead()) return;

                door.SetFlag(BaseEntity.Flags.Open, false);
                door.SendNetworkUpdateImmediate();
            });
        }
		#endregion
		
		#region Oxide
		void OnServerInitialized() => LoadDefaultConfig();
        void Init()
        {
            LoadDefaultMessages();
            permission.RegisterPermission(Permission, this);
            playerPrefs = dataFile.ReadObject<List<string>>();
        }
		
		bool IsRaidBlocked(string targetId) => UseRaidBlocker && (bool)(NoEscape?.Call("IsRaidBlockedS", targetId) ?? false);
		#endregion

		#region ChatCommand
        [ChatCommand("ad")]
        void AutoDoorCommand(BasePlayer player, string command, string[] args)
        {
            if(PermissionSupport)
			{
			    if (!IsAllowed(player.UserIDString, Permission))
                {
                    SendReply(player, GetMessage("NO.ACCESS", player, command));
                    return;
                }
			}
			
			if (IsRaidBlocked(player.UserIDString))
			{
                SendReply(player, GetMessage("RAID.BLOCKED", player));
                return;				
			}

            if (args.Length == 0)
            {
                SendReply(player, GetMessage("CMD.AD.HELP", player, playerPrefs.Contains(player.UserIDString) ? "<color=#ff0000>Выключен</color>" : "<color=#00ff00>Включено</color>"));
                return;
            }
            if (args.Length == 1)
            {
                switch (args[0])
                {
                    case "on":
                        if (!playerPrefs.Contains(player.UserIDString))
                        {
							SendReply(player, GetMessage("ALREADY.ON", player));
                            return;
                        }

                        playerPrefs.Remove(player.UserIDString);
						SendReply(player, GetMessage("AUTO.CLOSE.ON", player));
                        break;
                    case "off":
                        if (playerPrefs.Contains(player.UserIDString))
                        {
							SendReply(player, GetMessage("ALREADY.OFF", player));
                            return;
                        }
                        playerPrefs.Add(player.UserIDString);
						SendReply(player, GetMessage("AUTO.CLOSE.OFF", player));
                        break;
                }

                dataFile.WriteObject(playerPrefs);
            }
        }
		#endregion
		
	    #region Localization
        void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NO.ACCESS"] = "У вас нет доступа к этой команде.",
				
				["RAID.BLOCKED"] = "Активирован РеидБлок автоматическое закрытия дверей недоступно!",
				
				["ALREADY.ON"] = "Автоматическое закрытие дверей уже включено.", 
                ["AUTO.CLOSE.ON"] = "Вы включили автоматическое закрытие дверей.",
				
				["ALREADY.OFF"] = "Автоматическое закрытия дверей уже выключено.",
                ["AUTO.CLOSE.OFF"] = "Вы выключили автоматическое закрытие дверей.",
				
                ["CMD.AD.HELP"] = "<size=17>Текущее состояние: {0}</size>\n" +
				                  "ДОСТУПНЫЕ КОМАНДЫ:\n" +
				                  "/ad on - Включить автоматическое закрытие дверей.\n" +
				                  "/ad off - Выключить автоматическое закрытие дверей.",
            }, this);
        }
		#endregion
		
		#region Helpers
        string GetMessage(string key, BasePlayer player, params string[] args) => String.Format(lang.GetMessage(key, this, player.UserIDString), args);
		bool IsAllowed(string id, string perm) => permission.UserHasPermission(id, perm);
		T GetConfig<T>(string name, T defaultValue) => Config[name] == null ? defaultValue : (T)Convert.ChangeType(Config[name], typeof(T));
        public static void GetVariable<T>(DynamicConfigFile config, string name, out T value, T defaultValue)
        {
            config[name] = value = config[name] == null ? defaultValue : (T)Convert.ChangeType(config[name], typeof(T));
        }
		#endregion
    }
}
