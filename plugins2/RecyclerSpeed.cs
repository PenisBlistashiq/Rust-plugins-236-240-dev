using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Oxide.Plugins {

	[Info("Recycler Speed", "Ryz0r/yetzt", "2.0.2")]
	[Description("Easily set the speed at which the recycler... recycles")]

	public class RecyclerSpeed : RustPlugin {

		private const string UsePerm = "recyclerspeed.use";

		#region Config
		private Configuration _config;
		protected override void SaveConfig() => Config.WriteObject(_config);
		protected override void LoadDefaultConfig() => _config = new Configuration();

		private class Configuration {
			[JsonProperty(PropertyName = "Recyler Speed (Lower = Faster) (Seconds)")]
			public float RecyclerSpeed = 5.0f;
		}

		protected override void LoadConfig() {
			base.LoadConfig();
			try
			{
				_config = Config.ReadObject<Configuration>();
				if (_config == null) throw new Exception();

				Convert.ToSingle(_config.RecyclerSpeed);
				SaveConfig();
			}
			catch
			{
				PrintError("Your configuration file contains an error. Using default configuration values.");
				LoadDefaultConfig();
			}
		}
		#endregion

		private void Init() {
			permission.RegisterPermission(UsePerm, this);
		}

		private void OnRecyclerToggle(Recycler recycler, BasePlayer player) {
			if (recycler.IsOn()) return;
			if (!permission.UserHasPermission(player.userID.ToString(), UsePerm)) return;

			recycler.CancelInvoke(nameof(recycler.RecycleThink));
			timer.Once(0.1f, () => recycler.InvokeRepeating(recycler.RecycleThink, _config.RecyclerSpeed - 0.1f, _config.RecyclerSpeed));
		}
	}
}