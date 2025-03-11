using System.Collections.Generic;
using Oxide.Core;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("XNote", "rustmods.ru", "1.0.0")]
    class XNote : RustPlugin
    {
		#region Configuration

        private NoteConfig config;

        private class NoteConfig
        {				
		    internal class GeneralSetting
			{
				[JsonProperty("Сколько раз будет выдана записка. [ После каждого коннекта на сервер ]")] public int Amount;
			}		
			
			[JsonProperty("Общие настройки")]
            public GeneralSetting Setting;			
			
			public static NoteConfig GetNewConfiguration()
            {
                return new NoteConfig
                {
					Setting = new GeneralSetting
					{
						Amount = 2
					}
				};
			}
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
			
			try
			{
				config = Config.ReadObject<NoteConfig>();
			}
			catch
			{
				PrintWarning("Ошибка чтения конфигурации! Создание дефолтной конфигурации!");
				LoadDefaultConfig();
			}
			
			SaveConfig();
        }
		protected override void LoadDefaultConfig() => config = NoteConfig.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion
		
		#region Data
		
		private Dictionary<ulong, int> StoredData = new Dictionary<ulong, int>();
		
		private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject("XNote", StoredData);
		private void Unload() => SaveData();
		
		#endregion
		
		#region Hooks
		
		private void OnServerInitialized()
		{
			PrintWarning("XNote от форума rustmods.ru");
			
			if (Interface.Oxide.DataFileSystem.ExistsDatafile("XNote"))
                StoredData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, int>>("XNote");
			
			InitializeLang();
		}
		
		private void OnPlayerConnected(BasePlayer player)
		{
			if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerConnected(player));
                return;
            }
			
			if (!StoredData.ContainsKey(player.userID))
				StoredData.Add(player.userID, 0);
			
			if (StoredData[player.userID] < config.Setting.Amount)
				GiveNote(player);
		}
		
		private void GiveNote(BasePlayer player)
		{	
			Item item = ItemManager.CreateByName("note", 1, 0);
			item.name = lang.GetMessage("Name", this, player.UserIDString);
			item.text = string.Format(lang.GetMessage("Info", this, player.UserIDString), player.displayName);		
				
			if (!player.inventory.containerBelt.IsFull())
			    item.MoveToContainer(player.inventory.containerBelt);
			else if (!player.inventory.containerMain.IsFull())
				item.MoveToContainer(player.inventory.containerMain);
			
			StoredData[player.userID]++;
			SaveData();
		}
		
		#endregion
		
		#region Lang

        void InitializeLang()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {			
                ["Name"] = "INTERESTING INFO",				
                ["Info"] = "Hello, {0}!"			
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {				
                ["Name"] = "ИНТЕРЕСНАЯ ИНФА",				
                ["Info"] = "Привет, {0}!"			
            }, this, "ru");            
        }

        #endregion
	}
}