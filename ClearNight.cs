using Facepunch;
using Network;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Clear Night", "Clearshot", "2.3.5")]
    [Description("Always bright nights")]
    class ClearNight : CovalencePlugin
    {
        private PluginConfig _config;
        private EnvSync _envSync;
        private List<DateTime> _fullMoonDates = new List<DateTime> {
            new DateTime(2024, 1, 25),
            new DateTime(2024, 2, 24),
            new DateTime(2024, 3, 25),
            new DateTime(2024, 4, 23),
            new DateTime(2024, 5, 23),
            new DateTime(2024, 6, 21),
            new DateTime(2024, 7, 21),
            new DateTime(2024, 8, 19),
            new DateTime(2024, 9, 17),
            new DateTime(2024, 10, 17),
            new DateTime(2024, 11, 15),
            new DateTime(2024, 12, 15)
        };
        private Dictionary<string, string> _weatherSync = new Dictionary<string, string>();
        private DateTime _date;
        private Climate _climate;
        private int _current = 0;
        private bool _playSound = false;

        private bool IsDay = false;
        private bool IsNight = false;

        [PluginReference("NightVision")]
        Plugin NightVisionRef;
        VersionNumber NightVisionMinVersion = new VersionNumber(1, 4, 0);

        void OnServerInitialized()
        {
            _envSync = BaseNetworkable.serverEntities.OfType<EnvSync>().FirstOrDefault();
            _climate = SingletonComponent<global::Climate>.Instance;
            _date = _fullMoonDates[_current];

            TOD_Sky.Instance.Components.Time.OnDay += OnDay;
            TOD_Sky.Instance.Components.Time.OnSunset += OnSunset;

            if (_envSync == null)
            {
                NextTick(() => {
                    LogError("Unable to find EnvSync! Are you using a custom map?");
                    Interface.Oxide.UnloadPlugin(Name);
                });
                return;
            }

            if (NightVisionRef != null && NightVisionRef.Version < NightVisionMinVersion)
            {
                NextTick(() => {
                    LogError($"NightVision version: v{NightVisionRef.Version}");
                    LogError($"Please update NightVision to v{NightVisionMinVersion} or higher!");
                    Interface.Oxide.UnloadPlugin(Name);
                });
                return;
            }

            timer.Every(_config.syncInterval, () => {
                if (!_envSync.limitNetworking)
                {
                    _envSync.limitNetworking = true;
                }

                if (NightVisionRef != null)
                {
                    NightVisionRef?.CallHook("BlockEnvUpdates", true);
                }

                UpdateCelestials();
                SyncWeather();

                List<Connection> subscribers = _envSync.net.group.subscribers;
                if (subscribers != null && subscribers.Count > 0)
                {
                    for (int i = 0; i < subscribers.Count; i++)
                    {
                        Connection connection = subscribers[i];
                        global::BasePlayer basePlayer = connection.player as global::BasePlayer;

                        if (!(basePlayer == null))
                        {
                            if (NightVisionRef != null && (bool)NightVisionRef?.CallHook("IsPlayerTimeLocked", basePlayer)) continue;

                            UpdatePlayerDateTime(ref connection, _config.freezeMoon && IsNight ? _date : _date.AddHours(TOD_Sky.Instance.Cycle.Hour));

                            if (_playSound)
                            {
                                var effect = new Effect(_config.sound, basePlayer, 0, Vector3.zero, Vector3.forward);
                                EffectNetwork.Send(effect, connection);
                            }
                        }
                    }
                }

                _playSound = false;
            });
        }

        void Unload()
        {
            TOD_Sky.Instance.Components.Time.OnDay -= OnDay;
            TOD_Sky.Instance.Components.Time.OnSunset -= OnSunset;

            if (_envSync != null)
            {
                _envSync.limitNetworking = false;
            }

            if (NightVisionRef != null)
            {
                NightVisionRef?.CallHook("BlockEnvUpdates", false);
            }

            ServerMgr.SendReplicatedVars("weather.");
        }

        void OnDay()
        {
            if (_config.randomizeDates)
            {
                _current = UnityEngine.Random.Range(0, _fullMoonDates.Count - 1);
                _date = _fullMoonDates[_current];
            }
            else
            {
                _current = _current >= _fullMoonDates.Count ? 0 : _current;
                _date = _fullMoonDates[_current];
                _current++;
            }
        }

        void OnSunset()
        {
            _playSound = _config.playSoundAtSunset;
        }

        private void UpdatePlayerDateTime(ref Connection connection, DateTime date)
        {
            if (Net.sv.write.Start())
            {
                connection.validate.entityUpdates = connection.validate.entityUpdates + 1;
                BaseNetworkable.SaveInfo saveInfo = new global::BaseNetworkable.SaveInfo
                {
                    forConnection = connection,
                    forDisk = false
                };
                Net.sv.write.PacketID(Message.Type.Entities);
                Net.sv.write.UInt32(connection.validate.entityUpdates);
                using (saveInfo.msg = Pool.Get<Entity>())
                {
                    _envSync.Save(saveInfo);
                    saveInfo.msg.environment.dateTime = date.ToBinary();
                    saveInfo.msg.environment.fog = 0;
                    saveInfo.msg.environment.rain = 0;
                    saveInfo.msg.environment.clouds = 0;
                    saveInfo.msg.environment.wind = 0;
                    if (saveInfo.msg.baseEntity == null)
                    {
                        LogError(this + ": ToStream - no BaseEntity!?");
                    }
                    if (saveInfo.msg.baseNetworkable == null)
                    {
                        LogError(this + ": ToStream - no baseNetworkable!?");
                    }
                    saveInfo.msg.ToProto(Net.sv.write);
                    _envSync.PostSave(saveInfo);
                    Net.sv.write.Send(new SendInfo(connection));
                }
            }
        }

        private void UpdatePlayerWeather(ref Dictionary<string, string> weatherVars)
        {
            if (Net.sv.write.Start())
            {
				List<Network.Connection> list = Facepunch.Pool.GetList<Network.Connection>();
				foreach (Network.Connection connection in Network.Net.sv.connections)
				{
					if (connection.connected)
					{
						list.Add(connection);
					}
				}
				List<KeyValuePair<string, string>> list2 = Facepunch.Pool.GetList<KeyValuePair<string, string>>();
                list2.AddRange(weatherVars.ToList());
                Net.sv.write.PacketID(Message.Type.ConsoleReplicatedVars);
                Net.sv.write.Int32(list2.Count);
                foreach (var item in list2)
                {
                    Net.sv.write.String(item.Key);
                    Net.sv.write.String(item.Value);
                }
                Net.sv.write.Send(new SendInfo(list));
				Facepunch.Pool.FreeList<KeyValuePair<string, string>>(ref list2);
				Facepunch.Pool.FreeList<Network.Connection>(ref list);
            }
        }

        private void UpdateCelestials()
        {
            float f = 0.0174532924f * TOD_Sky.Instance.World.Latitude;
            float num = Mathf.Sin(f);
            float num2 = Mathf.Cos(f);
            float longitude = TOD_Sky.Instance.World.Longitude;
            float num3 = 1.57079637f;
            int year = _date.Year;
            int month = _date.Month;
            int day = _date.Day;
            float num4 = TOD_Sky.Instance.Cycle.Hour - TOD_Sky.Instance.World.UTC;
            float num5 = (float)(367 * year - 7 * (year + (month + 9) / 12) / 4 + 275 * month / 9 + day - 730530) + num4 / 24f;
            float num7 = 23.4393f - 3.563E-07f * num5;
            float f2 = 0.0174532924f * num7;
            float num8 = Mathf.Sin(f2);
            float num9 = Mathf.Cos(f2);
            float num37 = 282.9404f + 4.70935E-05f * num5;
            float num38 = 0.016709f - 1.151E-09f * num5;
            float num39 = 356.047f + 0.985600233f * num5;
            float num40 = 0.0174532924f * num39;
            float num41 = Mathf.Sin(num40);
            float num42 = Mathf.Cos(num40);
            float f6 = num40 + num38 * num41 * (1f + num38 * num42);
            float num43 = Mathf.Sin(f6);
            float num44 = Mathf.Cos(f6) - num38;
            float num45 = Mathf.Sqrt(1f - num38 * num38) * num43;
            float num46 = 57.29578f * Mathf.Atan2(num45, num44);
            float num47 = Mathf.Sqrt(num44 * num44 + num45 * num45);
            float num48 = num46 + num37;
            float f7 = 0.0174532924f * num48;
            float num49 = Mathf.Sin(f7);
            float num50 = Mathf.Cos(f7);
            float num51 = num47 * num50;
            float num52 = num47 * num49;
            float num53 = num51;
            float num54 = num52 * num9;
            float y2 = num52 * num8;
            float num55 = Mathf.Atan2(num54, num53);
            float f8 = Mathf.Atan2(y2, Mathf.Sqrt(num53 * num53 + num54 * num54));
            float num56 = Mathf.Sin(f8);
            float num57 = Mathf.Cos(f8);
            float num58 = num46 + num37 + 180f + 15f * num4;
            float num59 = 0.0174532924f * (num58 + longitude);
            float f9 = num59 - num55;
            float num60 = Mathf.Sin(f9);
            float num61 = Mathf.Cos(f9) * num57;
            float num62 = num60 * num57;
            float num63 = num56;
            float num64 = num61 * num - num63 * num2;
            float num65 = num62;
            float y3 = num61 * num2 + num63 * num;
            float num67 = Mathf.Atan2(y3, Mathf.Sqrt(num64 * num64 + num65 * num65));
            float num68 = num3 - num67;
            float SunZenith = 57.29578f * num68;
            float LerpValue = Mathf.InverseLerp(105f, 90f, SunZenith);
            if (LerpValue > 0.1f)
            {
                IsDay = true;
                IsNight = false;
            }
            else
            {
                IsDay = false;
                IsNight = true;
            }
        }

        private void SyncWeather()
        {
            if (_config.syncWeather)
            {
                _weatherSync["weather.atmosphere_brightness"] = _climate.WeatherOverrides.Atmosphere.Brightness != -1f ? _climate.WeatherOverrides.Atmosphere.Brightness.ToString() : _climate.WeatherState.Atmosphere.Brightness.ToString();
                _weatherSync["weather.atmosphere_contrast"] = _climate.WeatherOverrides.Atmosphere.Contrast != -1f ? _climate.WeatherOverrides.Atmosphere.Contrast.ToString() : _climate.WeatherState.Atmosphere.Contrast.ToString();
                _weatherSync["weather.atmosphere_directionality"] = _climate.WeatherOverrides.Atmosphere.Directionality != -1f ? _climate.WeatherOverrides.Atmosphere.Directionality.ToString() : _climate.WeatherState.Atmosphere.Directionality.ToString();
                _weatherSync["weather.atmosphere_mie"] = _climate.WeatherOverrides.Atmosphere.MieMultiplier != -1f ? _climate.WeatherOverrides.Atmosphere.MieMultiplier.ToString() : _climate.WeatherState.Atmosphere.MieMultiplier.ToString();
                _weatherSync["weather.atmosphere_rayleigh"] = _climate.WeatherOverrides.Atmosphere.RayleighMultiplier != -1f ? _climate.WeatherOverrides.Atmosphere.RayleighMultiplier.ToString() : _climate.WeatherState.Atmosphere.RayleighMultiplier.ToString();
                _weatherSync["weather.clear_chance"] = _climate.Weather.ClearChance.ToString();
                _weatherSync["weather.cloud_attenuation"] = _climate.WeatherOverrides.Clouds.Attenuation != -1f ? _climate.WeatherOverrides.Clouds.Attenuation.ToString() : _climate.WeatherState.Clouds.Attenuation.ToString();
                _weatherSync["weather.cloud_brightness"] = _climate.WeatherOverrides.Clouds.Brightness != -1f ? _climate.WeatherOverrides.Clouds.Brightness.ToString() : _climate.WeatherState.Clouds.Brightness.ToString();
                _weatherSync["weather.cloud_coloring"] = _climate.WeatherOverrides.Clouds.Coloring != -1f ? _climate.WeatherOverrides.Clouds.Coloring.ToString() : _climate.WeatherState.Clouds.Coloring.ToString();
                _weatherSync["weather.cloud_coverage"] = _climate.WeatherOverrides.Clouds.Coverage != -1f ? _climate.WeatherOverrides.Clouds.Coverage.ToString() : _climate.WeatherState.Clouds.Coverage.ToString();
                _weatherSync["weather.cloud_opacity"] = _climate.WeatherOverrides.Clouds.Opacity != -1f ? _climate.WeatherOverrides.Clouds.Opacity.ToString() : _climate.WeatherState.Clouds.Opacity.ToString();
                _weatherSync["weather.cloud_saturation"] = _climate.WeatherOverrides.Clouds.Saturation != -1f ? _climate.WeatherOverrides.Clouds.Saturation.ToString() : _climate.WeatherState.Clouds.Saturation.ToString();
                _weatherSync["weather.cloud_scattering"] = _climate.WeatherOverrides.Clouds.Scattering != -1f ? _climate.WeatherOverrides.Clouds.Scattering.ToString() : _climate.WeatherState.Clouds.Scattering.ToString();
                _weatherSync["weather.cloud_sharpness"] = _climate.WeatherOverrides.Clouds.Sharpness != -1f ? _climate.WeatherOverrides.Clouds.Sharpness.ToString() : _climate.WeatherState.Clouds.Sharpness.ToString();
                _weatherSync["weather.cloud_size"] = _climate.WeatherOverrides.Clouds.Size != -1f ? _climate.WeatherOverrides.Clouds.Size.ToString() : _climate.WeatherState.Clouds.Size.ToString();
                _weatherSync["weather.dust_chance"] = _climate.Weather.DustChance.ToString();
                _weatherSync["weather.fog"] = _climate.WeatherOverrides.Atmosphere.Fogginess != -1f ? _climate.WeatherOverrides.Atmosphere.Fogginess.ToString() : _climate.WeatherState.Atmosphere.Fogginess.ToString();
                _weatherSync["weather.fog_chance"] = _climate.Weather.FogChance.ToString();
                _weatherSync["weather.overcast_chance"] = _climate.Weather.OvercastChance.ToString();
                _weatherSync["weather.rain"] = _climate.WeatherOverrides.Rain != -1f ? _climate.WeatherOverrides.Rain.ToString() : _climate.WeatherState.Rain.ToString();
                _weatherSync["weather.rain_chance"] = _climate.Weather.RainChance.ToString();
                _weatherSync["weather.rainbow"] = _climate.WeatherOverrides.Rainbow != -1f ? _climate.WeatherOverrides.Rainbow.ToString() : _climate.WeatherState.Rainbow.ToString();
                _weatherSync["weather.storm_chance"] = _climate.Weather.StormChance.ToString();
                _weatherSync["weather.thunder"] = _climate.WeatherOverrides.Thunder != -1f ? _climate.WeatherOverrides.Thunder.ToString() : _climate.WeatherState.Thunder.ToString();
                _weatherSync["weather.wind"] = _climate.WeatherOverrides.Wind != -1f ? _climate.WeatherOverrides.Wind.ToString() : _climate.WeatherState.Wind.ToString();

                if (IsNight)
                {
                    foreach (var pair in _config.weatherAtNight)
                    {
                        _weatherSync[pair.Key] = pair.Value;
                    }
                }

                UpdatePlayerWeather(ref _weatherSync);
            } 
            else if (IsNight && _config.weatherAtNight.Count > 0)
            {
                UpdatePlayerWeather(ref _config.weatherAtNight);
            }
        }

        [Command("clearnight.debug")]
        private void DebugCommand(Core.Libraries.Covalence.IPlayer player, string command, string[] args)
        {
            player.Message("clearnight.debug");
            if (!player.IsAdmin && !player.IsServer) return;

            StringBuilder _sb = new StringBuilder();
            _sb.AppendLine("\n*** DEBUG START ***\n");
            _sb.AppendLine($"ClearNight version: {Version}");
            _sb.AppendLine($"ClearNight date: {(_config.freezeMoon && IsNight ? _date : _date.AddHours(TOD_Sky.Instance.Cycle.Hour))}");
            _sb.AppendLine($"ClearNight IsNight: {IsNight}");
            _sb.AppendLine($"ClearNight IsDay: {IsDay}");

            _sb.AppendLine($"\n[Server date and time]");
            _sb.AppendLine($"Year: {TOD_Sky.Instance.Cycle.Year}");
            _sb.AppendLine($"Month: {TOD_Sky.Instance.Cycle.Month}");
            _sb.AppendLine($"Day: {TOD_Sky.Instance.Cycle.Day}");
            _sb.AppendLine($"Hour: {TOD_Sky.Instance.Cycle.Hour}");
            _sb.AppendLine($"IsNight: {TOD_Sky.Instance.IsNight}");
            _sb.AppendLine($"IsDay: {TOD_Sky.Instance.IsDay}");

            _sb.AppendLine($"\n[Config]");
            _sb.AppendLine(JsonConvert.SerializeObject(_config, Formatting.Indented, Config.Settings));

            _sb.AppendLine($"\nNightVision installed: {NightVisionRef != null}");
            if (NightVisionRef != null)
            {
                _sb.AppendLine($"NightVision version: {NightVisionRef.Version}");
            }

            _sb.AppendLine("\n*** DEBUG END ***");
            Puts(_sb.ToString());
            LogToFile("debug", _sb.ToString(), this);
        }

        #region Config

        protected override void LoadDefaultConfig()
        {
			Puts("Generating new default config file...");
            Config.WriteObject(GetDefaultConfig(), true);
        }

        private PluginConfig GetDefaultConfig()
        {
            PluginConfig defaultConfig = new PluginConfig();

			defaultConfig.fullMoonDates = _fullMoonDates.Select(d => d.ToString("MM/dd/yyyy")).ToArray();
            defaultConfig.weatherAtNight = new Dictionary<string, string> {
                { "weather.atmosphere_brightness", "1" },
                { "weather.atmosphere_contrast", "1.5" },
                { "weather.cloud_coverage", "0" },
                { "weather.cloud_size", "0" },
                { "weather.fog", "0" },
                { "weather.fog_chance", "0" }
            };

            return defaultConfig;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<PluginConfig>();

            bool invalidDates = true;
            if (_config.fullMoonDates.Length > 0)
            {
                List<DateTime> tempDates = new List<DateTime>();
                foreach (string date in _config.fullMoonDates)
                {
                    DateTime dt;
                    if (DateTime.TryParse(date, out dt))
                    {
                        tempDates.Add(dt);
                    }
                    else
                    {
                        Puts($"invalid date: {date}");
                    }
                }

                if (tempDates.Count > 0)
                {
                    invalidDates = false;
                    _fullMoonDates = tempDates;
                    Puts($"registered {_fullMoonDates.Count} {(_fullMoonDates.Count == 1 ? "date" : "dates")} from config");
                }
            }

            if (invalidDates)
            {
                Puts("no valid dates registered, using default dates");
            }

            Config.WriteObject(_config, true);
        }

        private class PluginConfig
        {
            public string[] fullMoonDates;
            public Dictionary<string, string> weatherAtNight;
            public bool syncWeather = true;
            public bool randomizeDates = false;
            public bool freezeMoon = false;
            public bool playSoundAtSunset = false;
            public string sound = "assets/bundled/prefabs/fx/player/howl.prefab";
            public float syncInterval = 5f;
        }

        #endregion
    }
}
