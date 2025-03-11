using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("WipeCountdown", "David", "1.0.2")]
    [Description("Simple Wipe Countdown with API")]

    public class WipeCountdown : RustPlugin
    { 
        #region [Fields]

        private long lastbroadcast;

        #endregion

        #region [Hooks]
        
        private void OnServerInitialized()
        {       
            LoadConfig();  
            LoadTimer(); 
            StartCount();  
        }

        
        private void OnNewSave(string filename)
        {   
            LoadConfig(); 
            LoadTimer();
            if(config.generalSettings._onNewSave)
            {
                ClearTimer();
                SaveTimer(); 
                StartCount();    
            }  
        }
        
        private object OnUserChat(IPlayer player, string message)
        {   
            if (!config.generalSettings._bcEnabled) return null;
            //message
            string _message = message.ToLower();
            string _getCountdown = GetCountdownFormated_API();
            string _getBroadcastMsg = config.generalSettings._bcText.Replace("{countdown}", _getCountdown);
            //cooldown
            long timeNow = DateTimeOffset.Now.ToUnixTimeSeconds();
            long calCd = timeNow - lastbroadcast;

            if (calCd == 0)
            {   
                if (_message.Contains($"{config.generalSettings._keyword}"))
                {   
                    Server.Broadcast(_getBroadcastMsg, config.generalSettings._bcIcon);
                    lastbroadcast = DateTimeOffset.Now.ToUnixTimeSeconds();
                    return null;
                }
            }
            if (calCd < config.generalSettings._bcCd)
            {   
                return null;
            }
            if (_message.Contains($"{config.generalSettings._keyword}"))
            {   
                Server.Broadcast(_getBroadcastMsg, config.generalSettings._bcIcon);
                lastbroadcast = DateTimeOffset.Now.ToUnixTimeSeconds();
                
            }
            return null;
        }

        #endregion

        #region [Commands]
        
        [ChatCommand("wipe")]
        private void wipecount_timeleft(BasePlayer player)
        {   
            string _getCountdown = GetCountdownFormated_API();
            string _getBroadcastMsg = config.generalSettings._bcText.Replace("{countdown}", _getCountdown);
            SendReply(player, _getBroadcastMsg);
             
        }
        
        [ConsoleCommand("countdown_reset")]
        private void wipecount_timereset(ConsoleSystem.Arg arg)
        {   
            ClearTimer();
            StartCount();
        }
       
        #endregion 

        #region [Time Data]

        private void LoadTimer()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile($"{Name}/Timer"))
            {
                _timerLoader = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, TimeLoader>>($"{Name}/Timer");
            }
            else
            {
                _timerLoader = new Dictionary<string, TimeLoader>();
                SaveTimer();
            }
        }

        private void SaveTimer()
        {
            if (_timerLoader != null)
                Interface.Oxide.DataFileSystem.WriteObject($"{Name}/Timer", _timerLoader);
        }

        private Dictionary<string, TimeLoader> _timerLoader;

        private class TimeLoader
        {
            public long _timeLeft;
        }


        private void ClearTimer()
        {
            _timerLoader.Clear();
            SaveTimer();
        }

        private void StartCount()
        {
            int count = _timerLoader.Count();
            if (count == 0)
            {   
                long timeNow = DateTimeOffset.Now.ToUnixTimeSeconds();
                long wipeLenght = config.generalSettings._wipeCd;
                _timerLoader.Add("wipeTimer", new TimeLoader());
                _timerLoader["wipeTimer"]._timeLeft = timeNow + wipeLenght;
                Puts($"No countdown saved. Starting new one.");
                SaveTimer();
            } else {

                long timeleft = TimeLeftLong();
                TimeSpan countdownFormated = TimeSpan.FromSeconds(timeleft); 
                Puts($"Countdown continues at {countdownFormated}.");
            }
        }

        private long TimeLeftLong()
        {
            long timeNow = DateTimeOffset.Now.ToUnixTimeSeconds();
            long timeDif = timeNow - _timerLoader["wipeTimer"]._timeLeft;
            long converted = ~timeDif + 1;
            return converted;
        }

        #endregion
       
        #region [Config] 

        private Configuration config;
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<Configuration>();
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            config = Configuration.CreateConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(config);

        

        class Configuration
        {   

            [JsonProperty(PropertyName = "General Settings")]
            public GeneralSettings generalSettings { get; set; }

            public class GeneralSettings
            {   
                [JsonProperty("Countdown (seconds)")]
                public long _wipeCd { get; set; }

                [JsonProperty("Global Response Enabled")]
                public bool _bcEnabled { get; set; }

                [JsonProperty("Global Response Text")]
                public string _bcText { get; set; }

                [JsonProperty("Global Response Icon")]
                public ulong _bcIcon { get; set; }

                [JsonProperty("Global Response Cooldown")]
                public long _bcCd { get; set; }

                [JsonProperty("Keyword To Trigger Response")]
                public string _keyword { get; set; }

                [JsonProperty("Reset Countdown on Map Change")]
                public bool _onNewSave { get; set; }

 
            } 

            public static Configuration CreateConfig()
            {
                return new Configuration
                {   

                    generalSettings = new WipeCountdown.Configuration.GeneralSettings
                    {   
                       _wipeCd = 604800,
                       _bcEnabled = false,
                       _bcText = "<size=20><color=#FF5733>RUSTDOME</color>.NET</size> \n Next map wipe will be in <color=#FFE49B>{countdown}</color>",
                       _bcIcon = 76561199136807947,
                       _bcCd = 3600,
                       _keyword = "wipe",
                       _onNewSave = true,
                    },
                };
            }
        }

        #endregion

        #region [API]

        private string GetCountdownFormated_API()
        {   
            long timeleft = TimeLeftLong();
            TimeSpan countdownFormated = TimeSpan.FromSeconds(timeleft);
            if (timeleft < 86400)
            {   
                string countDownText = string.Format("{0:D2} HOURS {1:D2} MINUTES", countdownFormated.Hours, countdownFormated.Minutes);
                return countDownText;
            }   else    {    
                            string countDownText = string.Format("{0:D1} DAYS {1:D2} HOURS {2:D2} MINUTES", countdownFormated.Days, countdownFormated.Hours, countdownFormated.Minutes);
                            return countDownText;
                        }
        }

        private long GetCountdownSeconds_API()
        {
            long timeleft = TimeLeftLong();
            return timeleft;
        }

        #endregion
        
    }
}