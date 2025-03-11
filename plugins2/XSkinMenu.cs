using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Linq;
using static ItemSkinDirectory;
using static SteamInventoryItem;

namespace Oxide.Plugins
{
	[Info("XSkinMenu", "Mefisto", "1.0.702C")]
	class XSkinMenu : RustPlugin
	{
		#region Reference

		[PluginReference] private Plugin ImageLibrary;

		#endregion

		#region Config

		private SkinConfig config;

		private class SkinConfig
		{
			internal class GeneralSetting
			{
				[JsonProperty("Generate/Check and add new skins accepted by developers or made for twitch drops")] public bool UpdateSkins;
				[JsonProperty("Generate/Check and add new skins added by developers [ For example, a skin for hazmatsuit ]")] public bool UpdateSkinsFacepunch;
				[JsonProperty("Show button to remove all skins")] public bool ButtonClear;
				[JsonProperty("Propagate blacklisted skins to repair bench")] public bool RepairBench;
				[JsonProperty("Enable button to remove skins via UI")] public bool DeleteButton;
				[JsonProperty("Blacklist of skins that cannot be changed. [ For example: fire gloves, fire hatchet ]")] public List<ulong> Blacklist = new List<ulong>();
			}

			internal class GUISetting
			{
				[JsonProperty("Layer UI - [ Overlay - above inventory | Hud - under inventory (to view installed skins without closing the menu) ]")] public string LayerUI = "Overlay";
				[JsonProperty("Refresh UI page after skin selection")] public bool SkinUP;
				[JsonProperty("Refresh UI page after skin removal")] public bool DelSkinUP;
				[JsonProperty("Color_background_1")] public string BColor1;
				[JsonProperty("Color_background_2")] public string BColor2;
				[JsonProperty("Color_background_3")] public string BColor3;
				[JsonProperty("Active category color")] public string ActiveColor;
				[JsonProperty("Inactive category color")] public string InactiveColor;
				[JsonProperty("Category button color")] public string CategoryColor;
				[JsonProperty("Settings buttons color")] public string SettingColor;
				[JsonProperty("Button color (icons)")] public string IconColor;
				[JsonProperty("Item/skin block color")] public string BlockColor;
				[JsonProperty("Selected skin block color")] public string ActiveBlockColor;
				[JsonProperty("Active next/refresh button color")] public string ActiveNextReloadColor;
				[JsonProperty("Color of inactive next/refresh button")] public string InactiveNextReloadColor;
				[JsonProperty("Next/refresh active button text color")] public string ActiveNextReloadColorText;
				[JsonProperty("Text color of inactive next/refresh button")] public string InactiveNextReloadColorText;
				[JsonProperty("Active back button color")] public string ActiveBackColor;
				[JsonProperty("Back button color")] public string InactiveBackColor;
				[JsonProperty("Active back button text color")] public string ActiveBackColorText;
				[JsonProperty("Back button text color")] public string InactiveBackColorText;
			}

			internal class MenuSSetting
			{
				[JsonProperty("Enabled parameter icon")] public string TButtonIcon;
				[JsonProperty("Disabled parameter icon")] public string FButtonIcon;
				[JsonProperty("Enabled parameter color")] public string CTButton;
				[JsonProperty("Disabled parameter color")] public string CFButton;
			}

			[JsonProperty("General settings")]
			public GeneralSetting Setting = new GeneralSetting();
			[JsonProperty("Settings GUI")]
			public GUISetting GUI = new GUISetting();
			[JsonProperty("Settings menu")]
			public MenuSSetting MenuS = new MenuSSetting();
			[JsonProperty("Category settings")]
			public Dictionary<string, List<string>> Category = new Dictionary<string, List<string>>();

			public static SkinConfig GetNewConfiguration()
			{
				return new SkinConfig
				{
					Setting = new GeneralSetting
					{
						UpdateSkins = true,
						UpdateSkinsFacepunch = false,
						ButtonClear = true,
						RepairBench = true,
						DeleteButton = true,
						Blacklist = new List<ulong>
						{
							1742796979,
							841106268
						}
					},
					GUI = new GUISetting
					{
						LayerUI = "Overlay",
						SkinUP = true,
						DelSkinUP = true,
						BColor1 = "0.517 0.521 0.509 0.95",
						BColor2 = "0.217 0.221 0.209 0.95",
						BColor3 = "0.217 0.221 0.209 1",
						ActiveColor = "0.53 0.77 0.35 0.8",
						InactiveColor = "0 0 0 0",
						CategoryColor = "0.517 0.521 0.509 0.5",
						SettingColor = "0.517 0.521 0.509 0.5",
						IconColor = "1 1 1 0.75",
						BlockColor = "0.517 0.521 0.509 0.5",
						ActiveBlockColor = "0.53 0.77 0.35 0.8",
						ActiveNextReloadColor = "0.35 0.45 0.25 1",
						InactiveNextReloadColor = "0.35 0.45 0.25 0.4",
						ActiveNextReloadColorText = "0.75 0.95 0.41 1",
						InactiveNextReloadColorText = "0.75 0.95 0.41 0.4",
						ActiveBackColor = "0.65 0.29 0.24 1",
						InactiveBackColor = "0.65 0.29 0.24 0.4",
						ActiveBackColorText = "0.92 0.79 0.76 1",
						InactiveBackColorText = "0.92 0.79 0.76 0.4"
					},
					MenuS = new MenuSSetting
					{
						TButtonIcon = "assets/icons/check.png",
						FButtonIcon = "assets/icons/close.png",
						CTButton = "0.53 0.77 0.35 0.8",
						CFButton = "1 0.4 0.35 0.8"
					},
					Category = new Dictionary<string, List<string>>
					{
						["weapon"] = new List<string> { "gun.water", "pistol.revolver", "pistol.semiauto", "pistol.python", "pistol.eoka", "shotgun.waterpipe", "shotgun.double", "shotgun.pump", "bow.hunting", "crossbow", "grenade.f1", "smg.2", "smg.thompson", "smg.mp5", "rifle.ak", "rifle.lr300", "lmg.m249", "rocket.launcher", "rifle.semiauto", "rifle.m39", "rifle.bolt", "rifle.l96", "longsword", "salvaged.sword", "knife.combat", "bone.club", "knife.bone" },
						["construction"] = new List<string> { "wall.frame.garagedoor", "door.double.hinged.toptier", "door.double.hinged.metal", "door.double.hinged.wood", "door.hinged.toptier", "door.hinged.metal", "door.hinged.wood", "barricade.concrete", "barricade.sandbags" },
						["item"] = new List<string> { "locker", "vending.machine", "fridge", "furnace", "table", "chair", "box.wooden.large", "box.wooden", "rug.bear", "rug", "sleepingbag", "water.purifier", "target.reactive", "sled", "discofloor", "paddlingpool", "innertube", "boogieboard", "beachtowel", "beachparasol", "beachchair", "skull.trophy", "skullspikes", "skylantern" },
						["attire"] = new List<string> { "metal.facemask", "coffeecan.helmet", "riot.helmet", "bucket.helmet", "deer.skull.mask", "twitch.headset", "sunglasses", "mask.balaclava", "burlap.headwrap", "hat.miner", "hat.beenie", "hat.boonie", "hat.cap", "mask.bandana", "metal.plate.torso", "roadsign.jacket", "roadsign.kilt", "roadsign.gloves", "burlap.gloves", "attire.hide.poncho", "jacket.snow", "jacket", "tshirt.long", "hazmatsuit", "hoodie", "shirt.collared", "tshirt", "burlap.shirt", "attire.hide.vest", "shirt.tanktop", "attire.hide.helterneck", "pants", "burlap.trousers", "pants.shorts", "attire.hide.pants", "attire.hide.skirt", "shoes.boots", "burlap.shoes", "attire.hide.boots" },
						["tool"] = new List<string> { "fun.guitar", "jackhammer", "icepick.salvaged", "pickaxe", "stone.pickaxe", "rock", "hatchet", "stonehatchet", "explosive.satchel", "hammer" },
						["transport"] = new List<string> { "snowmobile" }
					}
				};
			}
		}

		protected override void LoadConfig()
		{
			base.LoadConfig();

			try
			{
				config = Config.ReadObject<SkinConfig>();
			}
			catch
			{
				PrintWarning("Configuration read error! Creating a default configuration!");
				LoadDefaultConfig();
			}

			SaveConfig();
		}
		protected override void LoadDefaultConfig() => config = SkinConfig.GetNewConfiguration();
		protected override void SaveConfig() => Config.WriteObject(config);

		#endregion

		#region Field
		private Dictionary<string, string> Images = new Dictionary<string, string>
		{
			["MainBG"] = "https://i.imgur.com/XUvfm83.png",
			["MainBGIN"] = "https://i.imgur.com/lQdazXv.png",
			["Settings"] = "https://i.imgur.com/BSM6RpH.png",
			["Logotype"] = "https://cdn.discordapp.com/attachments/935163486340804698/1045406828038737980/-2.png",
			["ItemBG"] = "https://i.imgur.com/DsjxggI.png",
			["ButtonBG"] = "https://i.imgur.com/KlzaHPl.png",
			["Refresh"] = "https://i.imgur.com/DxaBZYS.png",
			["SettingsBG"] = "https://i.imgur.com/YzSDvBw.png",
			["Back"] = "https://i.imgur.com/HKyvDDV.png",
			["Next"] = "https://i.imgur.com/SIUWAzi.png"
        };
        #endregion

        private string API_KEY = "LSKINS-fc123213sad85wkasxk74";

		#region Data

		internal class Data
		{
			[JsonProperty("Changing skins in inventory")] public bool ChangeSI = true;
			[JsonProperty("Changing skins on items")] public bool ChangeSE = true;
			[JsonProperty("Changing skins when crafting")] public bool ChangeSC = true;
			[JsonProperty("Changing skins in inventory after deletion")] public bool ChangeSCL = true;
			[JsonProperty("Change of skins when entering the inventory")] public bool ChangeSG;
			[JsonProperty("Skins")] public Dictionary<string, ulong> Skins = new Dictionary<string, ulong>();
		}

		private Dictionary<ulong, Data> StoredData = new Dictionary<ulong, Data>();
		private Dictionary<ulong, bool> StoredDataFriends = new Dictionary<ulong, bool>();
		public Dictionary<string, List<ulong>> StoredDataSkins = new Dictionary<string, List<ulong>>();

		private void LoadData(BasePlayer player)
		{
			var Data = Interface.Oxide.DataFileSystem.ReadObject<Data>($"XSkinMenu/UserSettings/{player.userID}");

			if (!StoredData.ContainsKey(player.userID))
				StoredData.Add(player.userID, new Data());
			if (!StoredDataFriends.ContainsKey(player.userID))
				StoredDataFriends.Add(player.userID, false);

			StoredData[player.userID] = Data ?? new Data();

			if (StoredData[player.userID].Skins.Count == 0)
				foreach (var skin in StoredDataSkins)
					StoredData[player.userID].Skins.Add(skin.Key, 0);
		}

		private void SaveData(BasePlayer player) => Interface.Oxide.DataFileSystem.WriteObject($"XSkinMenu/UserSettings/{player.userID}", StoredData[player.userID]);

		private void Unload()
		{
			foreach (BasePlayer player in BasePlayer.activePlayerList)
			{
				SaveData(player);
				CuiHelper.DestroyUi(player, ".GUIS");
			}

			if (_coroutine != null)
				ServerMgr.Instance.StopCoroutine(_coroutine);

			Interface.Oxide.DataFileSystem.WriteObject("XSkinMenu/Friends", StoredDataFriends);
		}

		#endregion

		private Dictionary<BasePlayer, DateTime> Cooldowns = new Dictionary<BasePlayer, DateTime>();

		#region Commands

		[ChatCommand("skin")]
		private void cmdOpenGUI(BasePlayer player)
		{
			if (!permission.UserHasPermission(player.UserIDString, "xskinmenu.use"))
				SendReply(player, lang.GetMessage("NOPERM", this, player.UserIDString));
			else
				GUI(player);
		}

		[ChatCommand("skinentity")]
		private void cmdSetSkinEntity(BasePlayer player)
		{
			if (!permission.UserHasPermission(player.UserIDString, "xskinmenu.entity"))
			{
				SendReply(player, lang.GetMessage("NOPERM", this, player.UserIDString));
				return;
			}

			if (StoredData[player.userID].ChangeSE)
			{
				RaycastHit rhit;

				if (!Physics.Raycast(player.eyes.HeadRay(), out rhit, 3f, LayerMask.GetMask("Deployed", "Construction", "Prevent Building"))) return;
				var entity = rhit.GetEntity();

				if (entity == null) return;

				if (entity is BaseVehicle)
				{
					var vehicle = entity as BaseVehicle;
					var shortname = vehicle.ShortPrefabName;
					if (!StoredData[player.userID].Skins.ContainsKey(shortname)) return;

					SetSkinTransport(player, vehicle, shortname);
				}
				else
					if (entity.OwnerID == player.userID || player.currentTeam != 0 && player.Team.members.Contains(entity.OwnerID) && StoredDataFriends.ContainsKey(entity.OwnerID) && StoredDataFriends[entity.OwnerID])
					if (shortnamesEntity.ContainsKey(entity.ShortPrefabName))
					{
						var shortname = shortnamesEntity[entity.ShortPrefabName];
						if (!StoredData[player.userID].Skins.ContainsKey(shortname)) return;

						SetSkinEntity(player, entity, shortname);
					}
			}
		}

		[ConsoleCommand("skin_c")]
		private void ccmdCategoryS(ConsoleSystem.Arg args)
		{
			BasePlayer player = args.Player();

			if (!permission.UserHasPermission(player.UserIDString, "xskinmenu.use"))
			{
				SendReply(player, lang.GetMessage("NOPERM", this, player.UserIDString));
				return;
			}

			if (Cooldowns.ContainsKey(player))
				if (Cooldowns[player].Subtract(DateTime.Now).TotalSeconds >= 0) return;

			Effect x = new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", player, 0, new Vector3(), new Vector3());
			Effect z = new Effect("assets/bundled/prefabs/fx/weapons/survey_charge/survey_charge_stick.prefab", player, 0, new Vector3(), new Vector3());

			switch (args.Args[0])
			{
				case "category":
					{
						CategoryGUI(player, int.Parse(args.Args[2]));
						ItemGUI(player, args.Args[1]);
						EffectNetwork.Send(x, player.Connection);
						break;
					}
				case "skin":
					{
						SkinGUI(player, args.Args[1]);
						EffectNetwork.Send(x, player.Connection);

						CuiHelper.DestroyUi(player, ".ItemGUI");
						break;
					}
				case "setskin":
					{
						string item = args.Args[1];
						ulong skin = ulong.Parse(args.Args[2]);

						if (!StoredData[player.userID].Skins.ContainsKey(item)) return;

						Effect y = new Effect("assets/prefabs/deployable/repair bench/effects/skinchange_spraypaint.prefab", player, 0, new Vector3(), new Vector3());
						StoredData[player.userID].Skins[item] = skin;

						if (!permission.UserHasPermission(player.UserIDString, "xskinmenu.inventory"))
							SendReply(player, lang.GetMessage("NOPERM", this, player.UserIDString));
						else
						{
							if (StoredData[player.userID].ChangeSI) SetSkinItem(player, item, skin);
							if (config.GUI.SkinUP) SkinGUI(player, item, int.Parse(args.Args[3]));
						}

						EffectNetwork.Send(y, player.Connection);
						break;
					}
				case "clear":
					{
						string item = args.Args[1];
						StoredData[player.userID].Skins[item] = 0;

						CuiHelper.DestroyUi(player, $".I + {args.Args[2]}");
						if (StoredData[player.userID].ChangeSCL) SetSkinItem(player, item, 0);

						EffectNetwork.Send(z, player.Connection);
						break;
					}
				case "clearall":
					{
						StoredData[player.userID].Skins.Clear();

						foreach (var skin in StoredDataSkins)
							StoredData[player.userID].Skins.Add(skin.Key, 0);

						GUI(player);
						EffectNetwork.Send(z, player.Connection);
						break;
					}
			}

			Cooldowns[player] = DateTime.Now.AddSeconds(0.5f);
		}

		[ConsoleCommand("skin_s")]
		private void ccmdSetting(ConsoleSystem.Arg args)
		{
			BasePlayer player = args.Player();

			if (!permission.UserHasPermission(player.UserIDString, "xskinmenu.setting"))
			{
				SendReply(player, lang.GetMessage("NOPERM", this, player.UserIDString));
				return;
			}

			if (Cooldowns.ContainsKey(player))
				if (Cooldowns[player].Subtract(DateTime.Now).TotalSeconds >= 0) return;

			Effect x = new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", player, 0, new Vector3(), new Vector3());

			switch (args.Args[0])
			{
				case "open":
					{
						SettingGUI(player);
						break;
					}
				case "inventory":
					{
						StoredData[player.userID].ChangeSI = !StoredData[player.userID].ChangeSI;
						SettingGUI(player);
						break;
					}
				case "entity":
					{
						StoredData[player.userID].ChangeSE = !StoredData[player.userID].ChangeSE;
						SettingGUI(player);
						break;
					}
				case "craft":
					{
						StoredData[player.userID].ChangeSC = !StoredData[player.userID].ChangeSC;
						SettingGUI(player);
						break;
					}
				case "clear":
					{
						StoredData[player.userID].ChangeSCL = !StoredData[player.userID].ChangeSCL;
						SettingGUI(player);
						break;
					}
				case "give":
					{
						StoredData[player.userID].ChangeSG = !StoredData[player.userID].ChangeSG;
						SettingGUI(player);
						break;
					}
				case "friends":
					{
						StoredDataFriends[player.userID] = !StoredDataFriends[player.userID];
						SettingGUI(player);
						break;
					}
			}

			EffectNetwork.Send(x, player.Connection);
			Cooldowns[player] = DateTime.Now.AddSeconds(0.5f);
		}

		[ConsoleCommand("page.xskinmenu")]
		private void ccmdPage(ConsoleSystem.Arg args)
		{
			BasePlayer player = args.Player();
			Effect x = new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", player, 0, new Vector3(), new Vector3());

			string item = args.Args[2];
			int Page = int.Parse(args.Args[3]);

			switch (args.Args[0])
			{
				case "item":
					{
						switch (args.Args[1])
						{
							case "next":
								{
									ItemGUI(player, item, Page + 1);
									break;
								}
							case "back":
								{
									ItemGUI(player, item, Page - 1);
									break;
								}
						}
						break;
					}
				case "skin":
					{
						switch (args.Args[1])
						{
							case "next":
								{
									SkinGUI(player, item, Page + 1);
									break;
								}
							case "back":
								{
									SkinGUI(player, item, Page - 1);
									break;
								}
						}
						break;
					}
			}

			EffectNetwork.Send(x, player.Connection);
		}

		[ConsoleCommand("xskin")]
		private void ccmdAdmin(ConsoleSystem.Arg args)
		{
			if (args.Player() == null || args.Player().IsAdmin)
			{
				string item = args.Args[1];

				if (!StoredDataSkins.ContainsKey(item))
				{
					PrintWarning($"No item <{item}> found in the list!");
					return;
				}

				switch (args.Args[0])
				{
					case "add":
						{
							ulong skinID = ulong.Parse(args.Args[2]);

							if (StoredDataSkins[item].Contains(skinID))
								PrintWarning($"The skin <{skinID}> is already in the list of skins for the item <{item}>!");
							else
							{
								StoredDataSkins[item].Add(skinID);
								PrintWarning($"Skin <{skinID}> has been successfully added to the list of skins for item <{item}>!");

								ImageLibrary.Call("AddImage", $"http://api.skyplugins.ru/api/getskin//{skinID}/150", $"{skinID}" + 152);
							}

							break;
						}
					case "remove":
						{
							ulong skinID = ulong.Parse(args.Args[2]);

							if (StoredDataSkins[item].Contains(skinID))
							{
								StoredDataSkins[item].Remove(skinID);
								PrintWarning($"Skin <{skinID}> has been successfully removed from the list of skins for item <{item}>!");
							}
							else
								PrintWarning($"Skin <{skinID}> was not found in the list of skins for item <{item}>!");

							break;
						}
					case "remove_ui":
						{
							ulong skinID = ulong.Parse(args.Args[2]);

							if (StoredDataSkins[item].Contains(skinID))
							{
								BasePlayer player = args.Player();

								if (player != null)
								{
									StoredDataSkins[item].Remove(skinID);
									if (config.GUI.DelSkinUP) SkinGUI(player, item, int.Parse(args.Args[3]));
									EffectNetwork.Send(new Effect("assets/bundled/prefabs/fx/weapons/survey_charge/survey_charge_stick.prefab", player, 0, new Vector3(), new Vector3()), player.Connection);
									PrintWarning($"Skin <{skinID}> has been successfully removed from the list of skins for item <{item}>!");
								}
							}
							else
								PrintWarning($"Skin <{skinID}> was not found in the list of skins for item <{item}>!");

							break;
						}
					case "list":
						{
							if (StoredDataSkins[item].Count == 0)
							{
								PrintWarning($"The list of skins for item <{item}> is empty!");
								return;
							}

							string skinslist = $"List of item skins <{item}>:\n";

							foreach (ulong skinID in StoredDataSkins[item])
								skinslist += $"\n{skinID}";

							PrintWarning(skinslist);

							break;
						}
					case "clearlist":
						{
							if (StoredDataSkins[item].Count == 0)
							{
								PrintWarning($"The list of skins for the item <{item}> is already empty!");
								return;
							}
							else
							{
								StoredDataSkins[item].Clear();
								PrintWarning($"The list of skins for item <{item}> has been cleared successfully!");
							}

							break;
						}
				}

				Interface.Oxide.DataFileSystem.WriteObject("XSkinMenu/Skins", StoredDataSkins);
			}
		}

		[ConsoleCommand("skinimage_reload")]
		private void ccmdReloadIMG(ConsoleSystem.Arg args)
		{
			if (args.Player() == null || args.Player().IsAdmin)
				if (_coroutine == null)
					_coroutine = ServerMgr.Instance.StartCoroutine(ReloadImage());
				else
					PrintWarning("Image loading continues. Wait!");
		}

		#endregion

		private readonly Dictionary<string, string> shortnamesEntity = new Dictionary<string, string>();

		#region Hooks 
		

		private void OnServerInitialized()
		{

			if (Interface.Oxide.DataFileSystem.ExistsDatafile("XSkinMenu/Friends"))
				StoredDataFriends = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, bool>>("XSkinMenu/Friends");
			if (Interface.Oxide.DataFileSystem.ExistsDatafile("XSkinMenu/Skins"))
				StoredDataSkins = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, List<ulong>>>("XSkinMenu/Skins");

			if (_coroutine == null)
				_coroutine = ServerMgr.Instance.StartCoroutine(LoadImage());

			foreach (var item in ItemManager.GetItemDefinitions())
			{
				var prefab = item.GetComponent<ItemModDeployable>()?.entityPrefab?.resourcePath;
				if (string.IsNullOrEmpty(prefab)) continue;

				var shortPrefabName = Utility.GetFileNameWithoutExtension(prefab);
				if (!string.IsNullOrEmpty(shortPrefabName) && !shortnamesEntity.ContainsKey(shortPrefabName))
					shortnamesEntity.Add(shortPrefabName, item.shortname);
			}

            foreach (var img in Images)
            {
                ImageLibrary.Call("AddImage", img.Value, img.Key);
            }

            GenerateItems();

			BasePlayer.activePlayerList.ToList().ForEach(OnPlayerConnected);
			timer.Every(180, () => BasePlayer.activePlayerList.ToList().ForEach(SaveData));
			timer.Every(200, () => Interface.Oxide.DataFileSystem.WriteObject("XSkinMenu/Friends", StoredDataFriends));

			InitializeLang();
			permission.RegisterPermission("xskinmenu.use", this);
			permission.RegisterPermission("xskinmenu.setting", this);
			permission.RegisterPermission("xskinmenu.craft", this);
			permission.RegisterPermission("xskinmenu.entity", this);
			permission.RegisterPermission("xskinmenu.inventory", this);
			permission.RegisterPermission("xskinmenu.give", this);
		}

		private Coroutine _coroutine;

		private IEnumerator LoadImage()
		{
			foreach (var category in config.Category)
				foreach (var item in category.Value)
				{
					if (!ImageLibrary.Call<bool>("HasImage", item + 150))
						ImageLibrary.Call("AddImage", $"http://api.skyplugins.ru/api/getimage/{item}/{150}", item + 150);

					yield return CoroutineEx.waitForSeconds(0.03f);
				}

			foreach (var item in StoredDataSkins)
				foreach (var skin in item.Value)
				{
					if (!ImageLibrary.Call<bool>("HasImage", $"{skin}" + 152))
						ImageLibrary.Call("AddImage", $"http://api.skyplugins.ru/api/getskin//{skin}/150", $"{skin}" + 152);

					yield return CoroutineEx.waitForSeconds(0.03f);
				}
            

            _coroutine = null;
			yield return 0;
		}

		private IEnumerator ReloadImage()
		{
			foreach (var category in config.Category)
				foreach (var item in category.Value)
				{
					ImageLibrary.Call("AddImage", $"http://api.skyplugins.ru/api/getimage/{item}/{150}", item + 150);

					yield return CoroutineEx.waitForSeconds(0.03f);
				}

			foreach (var item in StoredDataSkins)
				foreach (var skin in item.Value)
				{
					ImageLibrary.Call("AddImage", $"http://api.skyplugins.ru/api/getskin//{skin}/150", $"{skin}" + 152);

					yield return CoroutineEx.waitForSeconds(0.03f);
				}
			foreach (var img in Images)
			{
				ImageLibrary.Call("AddImage", img);

				yield return CoroutineEx.waitForSeconds(0.03f);
			}

			_coroutine = null;
			yield return 0;
		}

		private void GenerateItems()
		{
			if (config.Setting.UpdateSkins)
				foreach (var pair in Rust.Workshop.Approved.All)
				{
					if (pair.Value == null || pair.Value.Skinnable == null) continue;

					ulong skinID = pair.Value.WorkshopdId;

					string item = pair.Value.Skinnable.ItemName;
					if (item.Contains("lr300")) item = "rifle.lr300";

					if (!StoredDataSkins.ContainsKey(item))
						StoredDataSkins.Add(item, new List<ulong>());

					if (!StoredDataSkins[item].Contains(skinID))
						StoredDataSkins[item].Add(skinID);
				}

			if (config.Setting.UpdateSkinsFacepunch)
				foreach (ItemDefinition item in ItemManager.GetItemDefinitions())
				{
					foreach (var skinID in ItemSkinDirectory.ForItem(item).Select(skin => Convert.ToUInt64(skin.id)))
					{
						if (!StoredDataSkins.ContainsKey(item.shortname))
							StoredDataSkins.Add(item.shortname, new List<ulong>());

						if (!StoredDataSkins[item.shortname].Contains(skinID))
							StoredDataSkins[item.shortname].Add(skinID);
					}
				}

			Interface.Oxide.DataFileSystem.WriteObject("XSkinMenu/Skins", StoredDataSkins);
		}

		private void OnPlayerConnected(BasePlayer player)
		{
			if (player.IsReceivingSnapshot)
			{
				NextTick(() => OnPlayerConnected(player));
				return;
			}

			LoadData(player);
		}

		private void OnPlayerDisconnected(BasePlayer player)
		{
			if (StoredData.ContainsKey(player.userID))
			{
				SaveData(player);
				StoredData.Remove(player.userID);
			}

			if (Cooldowns.ContainsKey(player))
				Cooldowns.Remove(player);
		}

		public Dictionary<string, string> errorshortnames = new Dictionary<string, string>
		{
			["snowmobiletomaha"] = "tomahasnowmobile"
		};

		public Dictionary<ulong, string> errorskins = new Dictionary<ulong, string>
		{
			[10180] = "hazmatsuit.spacesuit",
			[10201] = "hazmatsuit.nomadsuit",
			[10207] = "hazmatsuit.arcticsuit",
			[13070] = "rifle.ak.ice",
			[13068] = "snowmobiletomaha",
			[10189] = "door.hinged.industrial.a",
			[13050] = "skullspikes.candles",
			[13051] = "skullspikes.pumpkin",
			[13052] = "skull.trophy.jar",
			[13053] = "skull.trophy.jar2",
			[13054] = "skull.trophy.table",
			[13056] = "sled.xmas",
			[13057] = "discofloor.largetiles",
			[10198] = "factorydoor",
			//[] = "sofa.pattern" 
		};

		private void OnItemCraftFinished(ItemCraftTask task, Item item)
		{
			if (task.skinID == 0)
			{
				BasePlayer player = task.owner;

				if (!StoredData[player.userID].Skins.ContainsKey(item.info.shortname) || !permission.UserHasPermission(player.UserIDString, "xskinmenu.craft")) return;
				if (!StoredData[player.userID].ChangeSG && StoredData[player.userID].ChangeSC)
					SetSkinCraftGive(player, item);
			}
		}

		private void SetSkinItem(BasePlayer player, string item, ulong skin)
		{
			foreach (var i in player.inventory.FindItemIDs(ItemManager.FindItemDefinition(item).itemid))
			{
				if (i.skin == skin || config.Setting.Blacklist.Contains(i.skin)) continue;

				if (errorskins.ContainsKey(skin))
				{
					i.UseItem();
					Item newitem = ItemManager.CreateByName(errorskins[skin]);
					newitem.condition = i.condition;
					newitem.maxCondition = i.maxCondition;

					if (i.contents != null)
						foreach (var module in i.contents.itemList)
						{
							Item content = ItemManager.CreateByName(module.info.shortname, module.amount);
							content.condition = module.condition;
							content.maxCondition = module.maxCondition;

							content.MoveToContainer(newitem.contents);
						}

					player.GiveItem(newitem);
				}
				else
				{
					i.skin = skin;
					i.MarkDirty();

					BaseEntity entity = i.GetHeldEntity();
					if (entity != null)
					{
						entity.skinID = skin;
						entity.SendNetworkUpdate();
					}
				}
			}
		}

		private void SetSkinCraftGive(BasePlayer player, Item item)
		{
			string shortname = item.info.shortname;
			ulong skin = StoredData[player.userID].Skins[shortname];

			if (item.skin == skin || config.Setting.Blacklist.Contains(item.skin)) return;

			if (errorskins.ContainsKey(skin))
			{
				item.UseItem();
				Item newitem = ItemManager.CreateByName(errorskins[skin]);
				newitem.condition = item.condition;
				newitem.maxCondition = item.maxCondition;

				if (item.contents != null)
					foreach (var module in item.contents.itemList)
					{
						Item content = ItemManager.CreateByName(module.info.shortname, module.amount);
						content.condition = module.condition;
						content.maxCondition = module.maxCondition;

						content.MoveToContainer(newitem.contents);
					}

				player.GiveItem(newitem);
			}
			else
			{
				item.skin = skin;
				item.MarkDirty();

				BaseEntity entity = item.GetHeldEntity();
				if (entity != null)
				{
					entity.skinID = skin;
					entity.SendNetworkUpdate();
				}
			}
		}

		private void SetSkinEntity(BasePlayer player, BaseEntity entity, string shortname)
		{
			ulong skin = StoredData[player.userID].Skins[shortname];

			if (skin == entity.skinID || skin == 0) return;
			if (errorskins.ContainsKey(skin))
			{
				SendInfo(player, lang.GetMessage("ERRORSKIN", this, player.UserIDString));
				return;
			}

			entity.skinID = skin;
			entity.SendNetworkUpdate();
			Effect.server.Run("assets/prefabs/deployable/repair bench/effects/skinchange_spraypaint.prefab", entity.transform.localPosition);
		}

		private void SetSkinTransport(BasePlayer player, BaseVehicle vehicle, string shortname)
		{
			ulong skin = StoredData[player.userID].Skins[shortname];

			if (skin == vehicle.skinID || skin == 0) return;

			if (errorskins.ContainsKey(skin))
				shortname = errorskins[skin];
			if (errorshortnames.ContainsKey(shortname))
				shortname = errorshortnames[shortname];

			BaseVehicle transport = GameManager.server.CreateEntity($"assets/content/vehicles/snowmobiles/{shortname}.prefab", vehicle.transform.position, vehicle.transform.rotation) as BaseVehicle;
			transport.health = vehicle.health;
			transport.skinID = skin;

			vehicle.Kill();
			transport.Spawn();
			Effect.server.Run("assets/prefabs/deployable/repair bench/effects/skinchange_spraypaint.prefab", transport.transform.localPosition);
		}

		private void OnItemAddedToContainer(ItemContainer container, Item item)
		{
			if (item == null) return;
			if (container?.playerOwner != null)
			{
				BasePlayer player = container.playerOwner;

				if (player == null || player.IsNpc || !player.userID.IsSteamId() || player.IsSleeping()) return;
				if (!permission.UserHasPermission(player.UserIDString, "xskinmenu.give") || !StoredData.ContainsKey(player.userID) || !StoredData[player.userID].Skins.ContainsKey(item.info.shortname)) return;
				if (StoredData[player.userID].ChangeSG)
					SetSkinCraftGive(player, item);
			}
		}

		private object OnItemSkinChange(int skinID, Item item, StorageContainer container, BasePlayer player)
		{
			if (config.Setting.RepairBench && config.Setting.Blacklist.Contains(item.skin))
			{
				EffectNetwork.Send(new Effect("assets/bundled/prefabs/fx/invite_notice.prefab", player, 0, new Vector3(), new Vector3()), player.Connection);

				return false;
			}
			else
				return null;
		}

		#endregion

		#region GUI

		private void GUI(BasePlayer player)
		{

			CuiHelper.DestroyUi(player, ".GUIS");
			CuiHelper.DestroyUi(player, ".Logotype");
            CuiHelper.DestroyUi(player, ".SGUI");
			CuiElementContainer container = new CuiElementContainer();

			container.Add(new CuiElement 
			{
				Name = ".GUIS",
				Parent = config.GUI.LayerUI,
				Components =
				{
					new CuiRawImageComponent {Color = "1 1 1 1", FadeIn = 1f,  Png = (string)ImageLibrary.Call("GetImage", "MainBG")},
					new CuiRectTransformComponent{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-507.5 -260", OffsetMax = "507.5 290"}
				}
			});


            container.Add(new CuiElement
            {
                Name = ".SGUI",
                Parent = ".GUIS",
                Components =
                {
                    new CuiRawImageComponent {Color = "1 1 1 0", Png = (string)ImageLibrary.Call("GetImage", "MainBGIN")},
                    new CuiRectTransformComponent{AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0"}
                }
            });




          
             container.Add(new CuiElement 
             { 
                  Name = ".settings",
                  Parent = ".SGUI",
                  Components =
                  {
                      new CuiRawImageComponent { Color = "1 1 1 1", Png = (string)ImageLibrary.Call("GetImage", "Settings") },
                      new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "414 237.5", OffsetMax = "443.5 264" }
                  }

              });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "416 237.5", OffsetMax = "443.5 264" },
                Button = { Color = "0 0 0 0", Command = "skin_s open" },
                Text = { Text = "" }
            }, ".SGUI");


            container.Add(new CuiElement
            {
                Name = ".refresh",
                Parent = ".SGUI",
                Components =
                  {
                      new CuiRawImageComponent { Color = "1 1 1 1", Png = (string)ImageLibrary.Call("GetImage", "Refresh") },
                      new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "446 237.5", OffsetMax = "473.5 264" }
                  }

            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "446 237.5", OffsetMax = "473.5 264" },
                Button = { Color = "0 0 0 0", Command = "skin_c clearall"},
                Text = { Text = "" }
            }, ".SGUI");

            container.Add(new CuiElement 
			{
				Name = ".Logotype",
                Parent = ".SGUI",
                Components =
                {
                    new CuiRawImageComponent { Color = "1 1 1 1", FadeIn = 1f, Png = (string)ImageLibrary.Call("GetImage", "Logotype") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-500 227.5", OffsetMax = "-300 275" }
                }
            });


			CuiHelper.AddUi(player, container);

			CategoryGUI(player);
			if (config.Category.Count != 0) ItemGUI(player, config.Category.ElementAt(0).Key);
		}

		private void CategoryGUI(BasePlayer player, int page = 0)
		{
			CuiHelper.DestroyUi(player, ".SkinBUTTON");
			CuiElementContainer container = new CuiElementContainer();

			container.Add(new CuiPanel
			{
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-497.5 187.5", OffsetMax = "497.5 222.5" },
				Image = { Color = "0 0 0 0" }
			}, ".SGUI", ".SkinBUTTON");

			int x = 0, count = config.Category.Count;

			foreach (var category in config.Category)
			{
				string color = page == x ? config.GUI.ActiveColor : config.GUI.InactiveColor;
				double offset = -(16 * count--) + -(45.5 * count--);

                container.Add(new CuiElement
                {
					Name = ".BUTTONBG",
                    Parent = ".SkinBUTTON",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "ButtonBG") },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{offset} 32.5", OffsetMax = $"{offset + 120} 58.5"}
                    }
                });

                container.Add(new CuiButton
				{
					RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{offset} 32.5", OffsetMax = $"{offset + 120} 58.5" },
					Button = { Color = "0 0 0 0", Command = $"skin_c category {category.Key} {x}" },
					Text = { Text = lang.GetMessage(category.Key, this, player.UserIDString), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 16, Color = "1 1 1 1" }
				}, ".SkinBUTTON", ".BUTTON");

				container.Add(new CuiPanel
				{
					RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMax = "0 1.5" },
					Image = { Color = color, Material = "assets/icons/greyout.mat" }
				}, ".BUTTON");

				x++;
			}

			CuiHelper.AddUi(player, container);
		}

		private void ItemGUI(BasePlayer player, string category, int Page = 0)
		{
			CuiHelper.DestroyUi(player, ".SettingGUI");
			CuiHelper.DestroyUi(player, ".SkinGUI");
			CuiHelper.DestroyUi(player, ".Item");
			CuiHelper.DestroyUi(player, ".Close");
			CuiHelper.DestroyUi(player, ".ItemGUI");
			CuiElementContainer container = new CuiElementContainer();

			container.Add(new CuiPanel
			{
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-502.5 -270", OffsetMax = "502.5 177.5" },
				Image = { Color = "0 0 0 0" }
			}, ".SGUI", ".ItemGUI");

			int x = 0, y = 0, z = 0;

			foreach (var item in config.Category[category].Skip(Page * 40))
			{
				container.Add(new CuiElement
				{
					Name = ".Item",
					Parent = ".ItemGUI",
					Components =
					{
						new CuiRawImageComponent {FadeIn = 1f, Png = (string) ImageLibrary.Call("GetImage", "ItemBG") },
						new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-447.5 + (x * 100)} {153.25 - (y * 100)}", OffsetMax = $"{-352.5 + (x * 100)} {248.25 - (y * 100)}" }
                    }
				});

				container.Add(new CuiElement
				{
					Parent = ".Item",
					Components =
					{
						new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", item + 150) },
						new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "7.5 7.5", OffsetMax = "-7.5 -7.5" }
					}
				});

				if (StoredDataSkins.ContainsKey(item) && StoredDataSkins[item].Count != 0 && StoredData[player.userID].Skins.ContainsKey(item))
				{
					container.Add(new CuiButton
					{
						RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
						Button = { Color = "0 0 0 0", Command = $"skin_c skin {item}" },
						Text = { Text = "" }
					}, ".Item");

					if (StoredData[player.userID].Skins[item] != 0)
						container.Add(new CuiButton
						{
							RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-20 5", OffsetMax = "-5 20" },
							Button = { Color = "1 1 1 0.75", Sprite = "assets/icons/clear.png", Command = $"skin_c clear {item} {z}" },
							Text = { Text = "" }
						}, ".Item", $".I + {z}");
				}

				x++;
				z++;

				if (x == 9)
				{
					x = 0;
					y++;

					if (y == 4)
						break;
				}
			}

			bool back = Page != 0;
			bool next = config.Category[category].Count > ((Page + 1) * 40);


            container.Add(new CuiElement
            {
                Parent = ".ItemGUI",
                Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "Back") },
                        new CuiRectTransformComponent { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-25 75", OffsetMax = "0 96.75"}
                    }
            });

            container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-25 75", OffsetMax = "0 96.75" },
                    Button = { Color = "0 0 0 0", Command = back ? $"page.xskinmenu item back {category} {Page}" : "" },
                    Text = { Text = ""}
                }, ".ItemGUI");

            container.Add(new CuiElement
            {
                Parent = ".ItemGUI",
                Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "Next") },
                        new CuiRectTransformComponent { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-25 100", OffsetMax = "0 121.75"}
                    }
            });

            container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-25 100", OffsetMax = "0 121.75" },
                    Button = { Color = "0 0 0 0", Command = next ? $"page.xskinmenu item next {category} {Page}" : "" },
                    Text = { Text = ""}
                }, ".ItemGUI");

          

            


            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-155 8", OffsetMax = "-50 35.75" },
                Button = { Color = "255 0 0 1", FadeIn = 1f, Close = ".GUIS" },
                Text = { Text = "Закрыть", Align = TextAnchor.MiddleCenter, FontSize = 16, FadeIn = 1f, Font = "robotocondensed-regular.ttf", Color = "1 1 1 1"}
            }, ".ItemGUI", "Close"); 



			CuiHelper.AddUi(player, container);
		}

		private void SkinGUI(BasePlayer player, string item, int Page = 0)
		{
			CuiElementContainer container = new CuiElementContainer();

			container.Add(new CuiPanel
			{
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-502.5 -270", OffsetMax = "502.5 177.5" },
				Image = { Color = "1 1 1 0" }
			}, ".SGUI", ".SkinGUI");

			int x = 0, y = 0;
			ulong s = StoredData[player.userID].Skins[item];

			foreach (var skin in StoredDataSkins[item].Skip(Page * 40))
			{
				CuiHelper.DestroyUi(player, ".Skin");
                container.Add(new CuiElement
                {
                    Name = ".Skin",
                    Parent = ".SkinGUI",
                    Components =
                    {
                        new CuiRawImageComponent {FadeIn = 1f, Png = (string) ImageLibrary.Call("GetImage", "ItemBG"), Color = s == skin ? config.GUI.ActiveBlockColor : config.GUI.BlockColor },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-447.5 + (x * 100)} {153.25 - (y * 100)}", OffsetMax = $"{-352.5 + (x * 100)} {248.25 - (y * 100)}" }
                    }
                });


				container.Add(new CuiElement
				{
					Parent = ".Skin",
					Components =
					{
						new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", $"{skin}152") },
						new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "7.5 7.5", OffsetMax = "-7.5 -7.5" }
					}
				});

				container.Add(new CuiButton
				{
					RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
					Button = { Color = "0 0 0 0", Command = $"skin_c setskin {item} {skin} {Page}" },
					Text = { Text = "" }
				}, ".Skin");

				if (config.Setting.DeleteButton && player.IsAdmin)
					container.Add(new CuiButton
					{
						RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-20 5", OffsetMax = "-5 20" },
						Button = { Color = "1 1 1 0.75", Sprite = "assets/icons/clear.png", Command = $"xskin remove_ui {item} {skin} {Page}" },
						Text = { Text = "" }
					}, ".Skin");

				x++;

				if (x == 9)
				{
					x = 0;
					y++;

					if (y == 4)
						break;
				}
			}

			bool back = Page != 0;
			bool next = StoredDataSkins[item].Count > ((Page + 1) * 40);


            container.Add(new CuiElement
            {
                Parent = ".SkinGUI",
                Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "Back") },
                        new CuiRectTransformComponent { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-25 75", OffsetMax = "0 96.75"}
                    }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-25 75", OffsetMax = "0 96.75" },
                Button = { Color = "0 0 0 0", Command = back ? $"page.xskinmenu skin back {item} {Page}" : "" },
                Text = { Text = "" }
            }, ".SkinGUI");

            container.Add(new CuiElement
            {
                Parent = ".SkinGUI",
                Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "Next") },
                        new CuiRectTransformComponent { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-25 100", OffsetMax = "0 121.75"}
                    }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-25 100", OffsetMax = "0 121.75" },
                Button = { Color = "0 0 0 0", Command = next ? $"page.xskinmenu skin next {item} {Page}" : "" },
                Text = { Text = "" }
            }, ".SkinGUI");

            

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-155 8", OffsetMax = "-50 35.75" },
                Button = { Color = "255 0 0 1", FadeIn = 1f, Close = ".GUIS" },
                Text = { Text = "Закрыть", Align = TextAnchor.MiddleCenter, FontSize = 16, FadeIn = 1f, Font = "robotocondensed-regular.ttf", Color = "1 1 1 1" }
            }, ".SkinGUI", "Close2");

            CuiHelper.DestroyUi(player, ".SkinGUI");
            CuiHelper.DestroyUi(player, "Close2");

            /*container.Add(new CuiPanel
			{
				RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 36.75", OffsetMax = "0 41.75" },
				Image = { Color = config.GUI.BColor1, Material = "assets/icons/greyout.mat" }
			}, ".SkinGUI");

			container.Add(new CuiPanel
			{
				RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-200 0", OffsetMax = "-195 36.75" },
				Image = { Color = config.GUI.BColor1, Material = "assets/icons/greyout.mat" }
			}, ".SkinGUI");*/

            CuiHelper.AddUi(player, container);
		}

		private void SettingGUI(BasePlayer player)
		{
			CuiHelper.DestroyUi(player, ".SettingGUI");
			CuiHelper.DestroyUi(player, ".SkinGUI");
			CuiHelper.DestroyUi(player, ".ItemGUI");
			CuiElementContainer container = new CuiElementContainer();

			container.Add(new CuiPanel
			{
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-502.5 -270", OffsetMax = "502.5 177.5" },
				Image = { Color = "0 0 0 0" }
			}, ".SGUI", ".SettingGUI");

			container.Add(new CuiLabel
			{
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-300 -25", OffsetMax = "300 0" },
				Text = { Text = lang.GetMessage("SETINFO", this, player.UserIDString), Align = TextAnchor.LowerCenter, Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "0.75 0.75 0.75 0.4" }
			}, ".SettingGUI");

			container.Add(new CuiPanel
			{
				CursorEnabled = true,
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-400 -75", OffsetMax = "400 75" },
				Image = { Color = "0 0 0 0" }
			}, ".SettingGUI", ".SGUIM");

			container.Add(new CuiPanel
			{
				RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" },
				Image = { Color = "0 0 0 0" }
			}, ".SGUIM", ".SGUIMM");

			Dictionary<string, bool> setting = new Dictionary<string, bool>
			{
				["inventory"] = StoredData[player.userID].ChangeSI,
				["entity"] = StoredData[player.userID].ChangeSE,
				["craft"] = StoredData[player.userID].ChangeSC,
				["clear"] = StoredData[player.userID].ChangeSCL,
				["give"] = StoredData[player.userID].ChangeSG,
				["friends"] = StoredDataFriends[player.userID]
			};

			int x = 0, y = 0;

			foreach (var s in setting)
			{
				container.Add(new CuiPanel
				{
					RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-390 + (x * 392.5)} {90 - (y * 45)}", OffsetMax = $"{-2.5 + (x * 392.5)} {130 - (y * 45)}" },
					Image = { Png = (string)ImageLibrary.Call("GetImage", "SettingsBG"), Color = "0.33 0.33 0.41 1" }
				}, ".SGUIMM", ".SM");

				container.Add(new CuiButton
				{
					RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-190 -15", OffsetMax = "-160 15" },
					Button = { Color = s.Value ? config.MenuS.CTButton : config.MenuS.CFButton, Sprite = s.Value ? config.MenuS.TButtonIcon : config.MenuS.FButtonIcon, Command = $"skin_s {s.Key}" },
					Text = { Text = "" }
				}, ".SM");

				container.Add(new CuiLabel
				{
					RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-155 -15", OffsetMax = "190 15" },
					Text = { Text = lang.GetMessage(s.Key, this, player.UserIDString), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 15, Color = "0.75 0.75 0.75 1" }
				}, ".SM");

				x++;

				if (x == 2)
				{
					x = 0;
					y++;
				}
			}

			CuiHelper.AddUi(player, container);
		}

		#endregion

		#region Message

		private void SendInfo(BasePlayer player, string message)
		{
			player.SendConsoleCommand("gametip.showgametip", message);
			timer.Once(5f, () => player.SendConsoleCommand("gametip.hidegametip"));
		}

		#endregion

		#region Lang

		private void InitializeLang()
		{
			lang.RegisterMessages(new Dictionary<string, string>
			{
				["TITLE"] = "COOL SERVER SKINS MENU",
				["SETINFO"] = "YOU CAN CUSTOMIZE THE MENU DEPENDING ON THE SITUATION!",
				["ERRORSKIN"] = "THE SKIN YOU CHOSE CAN BE CHANGED ONLY IN THE INVENTORY OR WHEN CRAFTING!",
				["CLEARALL"] = "UPDATE MY SKIN LIST",
				["NOPERM"] = "No permissions!",
				["NEXT"] = "NEXT",
				["BACK"] = "BACK",
				["weapon"] = "WEAPON",
				["construction"] = "CONSTRUCTION",
				["item"] = "ITEM",
				["attire"] = "ATTIRE",
				["tool"] = "TOOL",
				["transport"] = "TRANSPORT",
				["inventory"] = "CHANGE SKIN IN INVENTORY",
				["entity"] = "CHANGE SKIN ON OBJECTS",
				["craft"] = "CHANGE SKIN WHEN CRAFTING",
				["clear"] = "CHANGE SKIN WHEN DELETING",
				["give"] = "SKIN CHANGE WHEN DROP IN INVENTORY",
				["friends"] = "ALLOW FRIENDS TO CHANGE YOUR SKINS"
			}, this);

			lang.RegisterMessages(new Dictionary<string, string>
			{
				["TITLE"] = "МЕНЮ СКИНОВ КРУТОГО СЕРВЕРА",
				["SETINFO"] = "ВЫ МОЖЕТЕ КАСТОМНО НАСТРАИВАТЬ МЕНЮ В ЗАВИСИМОСТИ ОТ СИТУАЦИИ!",
				["ERRORSKIN"] = "ВЫБРАННЫЙ ВАМИ СКИН МОЖНО ИЗМЕНИТЬ ТОЛЬКО В ИНВЕНТАРЕ ИЛИ ПРИ КРАФТЕ!",
				["CLEARALL"] = "ОБНОВИТЬ МОЙ СПИСОК СКИНОВ",
				["NOPERM"] = "Недостаточно прав!",
				["NEXT"] = "ДАЛЕЕ",
				["BACK"] = "НАЗАД",
				["weapon"] = "ОРУЖИЕ",
				["construction"] = "СТРОИТЕЛЬСТВО",
				["item"] = "ПРЕДМЕТЫ",
				["attire"] = "ОДЕЖДА",
				["tool"] = "ИНСТРУМЕНТЫ",
				["transport"] = "ТРАНСПОРТ",
				["inventory"] = "ПОМЕНЯТЬ СКИН В ИНВЕНТАРЕ",
				["entity"] = "ПОМЕНЯТЬ СКИН НА ПРЕДМЕТАХ",
				["craft"] = "ПОМЕНЯТЬ СКИН ПРИ КРАФТЕ",
				["clear"] = "ПОМЕНЯТЬ СКИН ПРИ УДАЛЕНИИ",
				["give"] = "ПОМЕНЯТЬ СКИН ПРИ ПОПАДАНИИ В ИНВЕНТАРЬ",
				["friends"] = "РАЗРЕШИТЬ ДРУЗЬЯМ ИЗМЕНЯТЬ ВАШИ СКИНЫ"
			}, this, "ru");
		}

		#endregion
	}
}