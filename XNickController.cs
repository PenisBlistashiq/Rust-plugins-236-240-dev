using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using System;
using Oxide.Core.Plugins;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("XNickController", "Monster", "1.0.0")]
    public class XNickController : RustPlugin
    {
		#region Configuration

        private NickConfig config;

        private class NickConfig
        {		
			internal class SymbolSetting
            {
                [JsonProperty("?_?")]
                public List<string> SymbolList = new List<string>();           				
            }			
            
			[JsonProperty("Список символов/слов которые нужно удалять из ника")]
            public SymbolSetting Symbol = new SymbolSetting();										
			
			public static NickConfig GetNewConfiguration()
            {
                return new NickConfig
                {
					Symbol = new SymbolSetting
					{
						SymbolList = new List<string>
						{
							"#XRUST",
							"#LALARUST",
							"#XRUST RUST",
							".ua",
							".ru",
							".com"
						}
					},
				};
			}
        }

        protected override void LoadDefaultConfig()
        {
            config = NickConfig.GetNewConfiguration();

            PrintWarning("Создание начальной конфигурации плагина!!!");
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();

            config = Config.ReadObject<NickConfig>();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion
		
		#region Hooks
		
		private void OnServerInitialized()
		{
			PrintWarning("\n-----------------------------\n" +
			"     Author - Monster\n" +
			"     VK - vk.com/idannopol\n" +
			"     Discord - Monster#4837\n" +
			"     Config - v.2867\n" +
			"-----------------------------");
			
			foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
				foreach (var symbol in config.Symbol.SymbolList)
			    {
				    if (player.displayName.Contains(symbol))
				    {
					    player.displayName = player.displayName.Replace(symbol, "");
				    }
			    }
            }
		}
		
		void OnPlayerConnected(BasePlayer player)
		{
			if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerConnected(player));
                return;
            }
			
			foreach (var symbol in config.Symbol.SymbolList)
			{
				if (player.displayName.Contains(symbol))
				{
					player.displayName = player.displayName.Replace(symbol, "");
				}
			}
		}
		
		#endregion
	}
}