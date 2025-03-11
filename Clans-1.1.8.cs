//#define TESTING

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Facepunch;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Oxide.Plugins.ClansExtensionMethods;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
	[Info("Clans", "Mevent", "1.1.8")]
	public class Clans : RustPlugin
	{
		//TODO: Added UI options for clan management

		#region Fields

		[PluginReference] private Plugin
			ImageLibrary = null,
			BetterChat = null,
			ZoneManager = null,
			PlayTimeRewards = null,
			PlayerDatabase = null;

		private const string Layer = "UI.Clans";

		private const string ModalLayer = "UI.Clans.Modal";

		private const string PermAdmin = "clans.admin";

		private static Clans _instance;

		private readonly List<ItemDefinition> _defaultItems = new List<ItemDefinition>();

		private Coroutine _actionAvatars;

		private Coroutine _actionConvert;

		private Coroutine _handleTop;

		private readonly Dictionary<uint, ulong> _looters = new Dictionary<uint, ulong>();

		private const string COLORED_LABEL = "<color={0}>{1}</color>";

		private Regex _tagFilter;

		private Regex _hexFilter = new Regex("^([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$");

		private Dictionary<string, ClanData> _playerToClan = new Dictionary<string, ClanData>();

		#region Colors

		private string Color1;
		private string Color2;
		private string Color3;
		private string Color4;
		private string Color5;
		private string Color6;

		#endregion

		#endregion

		#region Config

		private static Configuration _config;

		private class Configuration
		{
			[JsonProperty(PropertyName = "Default Avatar")]
			public string DefaultAvatar = "https://i.imgur.com/nn7Lcm2.png";

			[JsonProperty(PropertyName = "Commands", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<string> Commands = new List<string>
			{
				"clan", "clans"
			};

			[JsonProperty(PropertyName = "Clan Info Commands", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public string[] ClanInfoCommands = {"cinfo"};

			[JsonProperty(PropertyName = "Maximum clan description characters")]
			public int DescriptionMax = 256;

			[JsonProperty(PropertyName = "Clan tag in player name")]
			public bool TagInName = true;

			[JsonProperty(PropertyName = "Automatic team creation")]
			public bool AutoTeamCreation = true;

			[JsonProperty(PropertyName = "Allow players to leave their clan by using Rust's leave team button")]
			public bool ClanTeamLeave = true;

			[JsonProperty(PropertyName =
				"Allow players to kick members from their clan using Rust's kick member button")]
			public bool ClanTeamKick = true;

			[JsonProperty(PropertyName =
				"Allow players to invite other players to their clan via Rust's team invite system")]
			public bool ClanTeamInvite = true;

			[JsonProperty(PropertyName = "Allow players to promote other clan members via Rust's team promote button")]
			public bool ClanTeamPromote = true;

			[JsonProperty(PropertyName = "Allow players to accept a clan invite using the Rust invite accept button")]
			public bool ClanTeamAcceptInvite = true;

			[JsonProperty(PropertyName = "Show clan creation interface when creating a team?")]
			public bool ClanCreateTeam = false;

			[JsonProperty(PropertyName = "Force to create a clan when creating a team?")]
			public bool ForceClanCreateTeam = false;

			[JsonProperty(PropertyName = "Use Friendly Fire?")]
			public bool UseFriendlyFire = true;

			[JsonProperty(PropertyName = "Use Friendly Fire for Turrets?")]
			public bool UseTurretsFF = false;

			[JsonProperty(PropertyName = "General friendly fire (only the leader of the clan can enable/disable it)")]
			public bool GeneralFriendlyFire = false;

			[JsonProperty(PropertyName = "Can moderators toggle general friendly fire?")]
			public bool ModersGeneralFF = false;

			[JsonProperty(PropertyName = "Can players toggle general friendly fire?")]
			public bool PlayersGeneralFF = false;

			[JsonProperty(PropertyName = "Friendly Fire Default Value")]
			public bool FriendlyFire = false;

			[JsonProperty(PropertyName = "Top refresh rate")]
			public float TopRefreshRate = 15f;

			[JsonProperty(PropertyName = "Default value for the resource standarts")]
			public int DefaultValStandarts = 100000;

			[JsonProperty(PropertyName = "Chat Settings")]
			public ChatSettings ChatSettings = new ChatSettings
			{
				Enabled = true,
				TagFormat = "<color=#{color}>[{tag}]</color>"
			};

			[JsonProperty(PropertyName = "Permission Settings")]
			public PermissionSettings PermissionSettings = new PermissionSettings
			{
				UseClanCreating = false,
				ClanCreating = "clans.cancreate",
				UseClanJoining = false,
				ClanJoining = "clans.canjoin",
				UseClanLeave = false,
				ClanLeave = "clans.canleave",
				UseClanDisband = false,
				ClanDisband = "clans.candisband",
				UseClanKick = false,
				ClanKick = "clans.cankick",
				UseClanSkins = false,
				ClanSkins = "clans.canskins",
				ClanInfoAuthLevel = 0
			};

			[JsonProperty(PropertyName = "Alliance Settings")]
			public AllianceSettings AllianceSettings = new AllianceSettings
			{
				Enabled = true,
				UseFF = true,
				DefaultFF = false
			};

			[JsonProperty(PropertyName = "Purge Settings")]
			public PurgeSettings PurgeSettings = new PurgeSettings
			{
				Enabled = true,
				OlderThanDays = 14,
				ListPurgedClans = true,
				WipeOnNewSave = false
			};

			[JsonProperty(PropertyName = "Limit Settings")]
			public LimitSettings LimitSettings = new LimitSettings
			{
				MemberLimit = 8,
				ModeratorLimit = 2,
				AlliancesLimit = 2
			};

			[JsonProperty(PropertyName = "Resources",
				ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<string> Resources = new List<string>
			{
				"stones", "sulfur.ore", "metal.ore", "hq.metal.ore", "wood"
			};

			[JsonProperty(PropertyName = "Score Table (shortname - score)",
				ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<string, float> ScoreTable = new Dictionary<string, float>
			{
				["kills"] = 1,
				["deaths"] = -1,
				["stone-ore"] = 0.1f,
				["supply_drop"] = 3f,
				["crate_normal"] = 0.3f,
				["crate_elite"] = 0.5f,
				["bradley_crate"] = 5f,
				["heli_crate"] = 5f,
				["bradley"] = 10f,
				["helicopter"] = 15f,
				["barrel"] = 0.1f,
				["scientistnpc"] = 0.5f,
				["heavyscientist"] = 2f,
				["sulfur.ore"] = 0.5f,
				["metal.ore"] = 0.5f,
				["hq.metal.ore"] = 0.5f,
				["stones"] = 0.5f,
				["cupboard.tool.deployed"] = 1f
			};

			[JsonProperty(PropertyName = "Available items for resource standarts",
				ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<string> AvailableStandartItems = new List<string>
			{
				"gears", "metalblade", "metalpipe", "propanetank", "roadsigns", "rope", "sewingkit", "sheetmetal",
				"metalspring", "tarp", "techparts", "riflebody", "semibody", "smgbody", "fat.animal", "cctv.camera",
				"charcoal", "cloth", "crude.oil", "diesel_barrel", "gunpowder", "hq.metal.ore", "leather",
				"lowgradefuel", "metal.fragments", "metal.ore", "scrap", "stones", "sulfur.ore", "sulfur",
				"targeting.computer", "wood"
			};

			[JsonProperty(PropertyName = "Pages", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<PageSettings> Pages = new List<PageSettings>
			{
				new PageSettings
				{
					ID = 0,
					Key = "aboutclan",
					Enabled = true,
					Permission = string.Empty
				},
				new PageSettings
				{
					ID = 1,
					Key = "memberslist",
					Enabled = true,
					Permission = string.Empty
				},
				new PageSettings
				{
					ID = 2,
					Key = "clanstop",
					Enabled = true,
					Permission = string.Empty
				},
				new PageSettings
				{
					ID = 3,
					Key = "playerstop",
					Enabled = true,
					Permission = string.Empty
				},
				new PageSettings
				{
					ID = 4,
					Key = "resources",
					Enabled = true,
					Permission = string.Empty
				},
				new PageSettings
				{
					ID = 6,
					Key = "skins",
					Enabled = true,
					Permission = string.Empty
				},
				new PageSettings
				{
					ID = 5,
					Key = "playerslist",
					Enabled = true,
					Permission = string.Empty
				},
				new PageSettings
				{
					ID = 7,
					Key = "alianceslist",
					Enabled = true,
					Permission = string.Empty
				}
			};

			[JsonProperty(PropertyName = "Interface")]
			public InterfaceSettings UI = new InterfaceSettings
			{
				Color1 = "#0E0E10",
				Color2 = "#4B68FF",
				Color3 = "#161617",
				Color4 = "#324192",
				Color5 = "#303030",
				Color6 = "#FF4B4B",
				ValueAbbreviation = true,
				TopClansColumns = new List<ColumnSettings>
				{
					new ColumnSettings
					{
						Width = 75,
						Key = "top",
						LangKey = TopTitle,
						TextAlign = TextAnchor.MiddleCenter,
						TitleFontSize = 10,
						FontSize = 12,
						TextFormat = "#{0}"
					},
					new ColumnSettings
					{
						Width = 165,
						Key = "name",
						LangKey = NameTitle,
						TextAlign = TextAnchor.MiddleCenter,
						TitleFontSize = 12,
						FontSize = 12,
						TextFormat = "{0}"
					},
					new ColumnSettings
					{
						Width = 70,
						Key = "leader",
						LangKey = LeaderTitle,
						TextAlign = TextAnchor.MiddleCenter,
						TitleFontSize = 12,
						FontSize = 12,
						TextFormat = "{0}"
					},
					new ColumnSettings
					{
						Width = 90,
						Key = "members",
						LangKey = MembersTitle,
						TextAlign = TextAnchor.MiddleCenter,
						TitleFontSize = 12,
						FontSize = 12,
						TextFormat = "{0}"
					},
					new ColumnSettings
					{
						Width = 80,
						Key = "score",
						LangKey = ScoreTitle,
						TextAlign = TextAnchor.MiddleCenter,
						TitleFontSize = 12,
						FontSize = 12,
						TextFormat = "{0}"
					}
				},
				TopPlayersColumns = new List<ColumnSettings>
				{
					new ColumnSettings
					{
						Width = 75,
						Key = "top",
						LangKey = TopTitle,
						TextAlign = TextAnchor.MiddleCenter,
						TitleFontSize = 10,
						FontSize = 12,
						TextFormat = "#{0}"
					},
					new ColumnSettings
					{
						Width = 185,
						Key = "name",
						LangKey = NameTitle,
						TextAlign = TextAnchor.MiddleCenter,
						TitleFontSize = 12,
						FontSize = 12,
						TextFormat = "{0}"
					},
					new ColumnSettings
					{
						Width = 70,
						Key = "kills",
						LangKey = KillsTitle,
						TextAlign = TextAnchor.MiddleCenter,
						TitleFontSize = 12,
						FontSize = 12,
						TextFormat = "{0}"
					},
					new ColumnSettings
					{
						Width = 70,
						Key = "resources",
						LangKey = ResourcesTitle,
						TextAlign = TextAnchor.MiddleCenter,
						TitleFontSize = 12,
						FontSize = 12,
						TextFormat = "{0}"
					},
					new ColumnSettings
					{
						Width = 80,
						Key = "score",
						LangKey = ScoreTitle,
						TextAlign = TextAnchor.MiddleCenter,
						TitleFontSize = 12,
						FontSize = 12,
						TextFormat = "{0}"
					}
				},
				ProfileButtons = new List<BtnConf>
				{
					new BtnConf
					{
						Enabled = false,
						CloseMenu = true,
						AnchorMin = "0 1", AnchorMax = "0 1",
						OffsetMin = "270 -55",
						OffsetMax = "360 -30",
						Align = TextAnchor.MiddleCenter,
						Font = "robotocondensed-regular.ttf",
						FontSize = 10,
						TextColor = new IColor("#FFFFFF", 100),
						Color = new IColor("#324192", 100),
						Title = "TP",
						Command = "tpr {target}"
					},
					new BtnConf
					{
						Enabled = false,
						CloseMenu = true,
						AnchorMin = "0 1", AnchorMax = "0 1",
						OffsetMin = "370 -55",
						OffsetMax = "460 -30",
						Align = TextAnchor.MiddleCenter,
						Font = "robotocondensed-regular.ttf",
						FontSize = 10,
						TextColor = new IColor("#FFFFFF", 100),
						Color = new IColor("#324192", 100),
						Title = "TRADE",
						Command = "trade {target}"
					}
				},
				ClanMemberProfileFields = new List<ColumnSettings>
				{
					new ColumnSettings
					{
						Width = 140,
						Key = "gather",
						LangKey = GatherTitle,
						TextAlign = TextAnchor.MiddleCenter,
						TitleFontSize = 10,
						FontSize = 12,
						TextFormat = "{0}%"
					},
					new ColumnSettings
					{
						Width = 140,
						Key = "lastlogin",
						LangKey = LastLoginTitle,
						TextAlign = TextAnchor.MiddleCenter,
						TitleFontSize = 10,
						FontSize = 10,
						TextFormat = "{0}"
					}
				},
				TopPlayerProfileFields = new List<ColumnSettings>
				{
					new ColumnSettings
					{
						Width = 300,
						Key = "clanname",
						LangKey = ClanNameTitle,
						TextAlign = TextAnchor.MiddleCenter,
						TitleFontSize = 10,
						FontSize = 12,
						TextFormat = "{0}"
					},
					new ColumnSettings
					{
						Width = 140,
						Key = "rating",
						LangKey = RatingTitle,
						TextAlign = TextAnchor.MiddleCenter,
						TitleFontSize = 10,
						FontSize = 12,
						TextFormat = "{0}"
					},
					new ColumnSettings
					{
						Width = 140,
						Key = "score",
						LangKey = ScoreTitle,
						TextAlign = TextAnchor.MiddleCenter,
						TitleFontSize = 10,
						FontSize = 12,
						TextFormat = "{0}"
					},
					new ColumnSettings
					{
						Width = 140,
						Key = "kills",
						LangKey = KillsTitle,
						TextAlign = TextAnchor.MiddleCenter,
						TitleFontSize = 10,
						FontSize = 12,
						TextFormat = "{0}"
					},
					new ColumnSettings
					{
						Width = 140,
						Key = "deaths",
						LangKey = DeathsTitle,
						TextAlign = TextAnchor.MiddleCenter,
						TitleFontSize = 10,
						FontSize = 12,
						TextFormat = "{0}"
					},
					new ColumnSettings
					{
						Width = 140,
						Key = "kd",
						LangKey = KDTitle,
						TextAlign = TextAnchor.MiddleCenter,
						TitleFontSize = 10,
						FontSize = 12,
						TextFormat = "{0}"
					}
				}
			};

			[JsonProperty(PropertyName = "Skins Settings")]
			public SkinsSettings Skins = new SkinsSettings
			{
				ItemSkins = new Dictionary<string, List<ulong>>
				{
					["metal.facemask"] = new List<ulong>(),
					["hoodie"] = new List<ulong>(),
					["metal.plate.torso"] = new List<ulong>(),
					["pants"] = new List<ulong>(),
					["roadsign.kilt"] = new List<ulong>(),
					["shoes.boots"] = new List<ulong>(),
					["rifle.ak"] = new List<ulong>(),
					["rifle.bolt"] = new List<ulong>()
				},
				UseSkinBox = false,
				UsePlayerSkins = false,
				CanCustomSkin = true,
				Permission = string.Empty,
				DisableSkins = false,
				DefaultValueDisableSkins = true
			};

			[JsonProperty(PropertyName = "Statistics Settings")]
			public StatisticsSettings Statistics = new StatisticsSettings
			{
				Kills = true,
				Gather = true,
				Loot = true,
				Entities = true,
				Craft = true
			};

			[JsonProperty(PropertyName = "Colos Settings")]
			public ColorsSettings Colors = new ColorsSettings
			{
				Member = "#fcf5cb",
				Moderator = "#74c6ff",
				Owner = "#a1ff46"
			};

			[JsonProperty(PropertyName = "PlayerDatabase")]
			public PlayerDatabaseConf PlayerDatabase = new PlayerDatabaseConf(false, "Clans");

			[JsonProperty(PropertyName = "ZoneManager Settings")]
			public ZoneManagerSettings ZMSettings = new ZoneManagerSettings
			{
				Enabled = false,
				FFAllowlist = new List<string>
				{
					"92457",
					"4587478545"
				}
			};

			[JsonProperty(PropertyName = "Clan Tag Settings")]
			public TagSettings Tags = new TagSettings
			{
				TagMin = 2,
				TagMax = 6,
				BlockedWords = new List<string>
				{
					"admin", "mod", "owner"
				},
				CheckingCharacters = true,
				AllowedCharacters = "!Â²Â³",
				TagColor = new TagSettings.TagColorSettings
				{
					Enabled = true,
					DefaultColor = "AAFF55",
					Owners = true,
					Moderators = false,
					Players = false
				}
			};

			public VersionNumber Version;
		}

		private class TagSettings
		{
			[JsonProperty(PropertyName = "Blocked Words", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<string> BlockedWords;

			[JsonProperty(PropertyName = "Minimum clan tag characters")]
			public int TagMin;

			[JsonProperty(PropertyName = "Maximum clan tag characters")]
			public int TagMax;

			[JsonProperty(PropertyName = "Enable character checking in tags?")]
			public bool CheckingCharacters;

			[JsonProperty(PropertyName = "Special characters allowed in tags")]
			public string AllowedCharacters;

			[JsonProperty(PropertyName = "Tag Color Settings")]
			public TagColorSettings TagColor;

			public class TagColorSettings
			{
				[JsonProperty(PropertyName = "Enabled")]
				public bool Enabled;

				[JsonProperty(PropertyName = "DefaultColor")]
				public string DefaultColor;

				[JsonProperty(PropertyName = "Can the owner change the color?")]
				public bool Owners;

				[JsonProperty(PropertyName = "Can the moderators change the color?")]
				public bool Moderators;

				[JsonProperty(PropertyName = "Can the players change the color?")]
				public bool Players;
			}
		}

		private class ZoneManagerSettings
		{
			[JsonProperty(PropertyName = "Enabled")]
			public bool Enabled;

			[JsonProperty(PropertyName = "Zones with allowed Friendly Fire",
				ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<string> FFAllowlist;
		}

		private class ColorsSettings
		{
			[JsonProperty(PropertyName = "Clan owner color (hex)")]
			public string Owner;

			[JsonProperty(PropertyName = "Clan moderator color (hex)")]
			public string Moderator;

			[JsonProperty(PropertyName = "Clan member color (hex)")]
			public string Member;
		}

		private class PlayerDatabaseConf
		{
			[JsonProperty(PropertyName = "Enabled")]
			public readonly bool Enabled;

			[JsonProperty(PropertyName = "Table")] public readonly string Field;

			public PlayerDatabaseConf(bool enabled, string field)
			{
				Enabled = enabled;
				Field = field;
			}
		}

		private class StatisticsSettings
		{
			[JsonProperty(PropertyName = "Kills")] public bool Kills;

			[JsonProperty(PropertyName = "Gather")]
			public bool Gather;

			[JsonProperty(PropertyName = "Loot")] public bool Loot;

			[JsonProperty(PropertyName = "Entities")]
			public bool Entities;

			[JsonProperty(PropertyName = "Craft")] public bool Craft;
		}

		private class SkinsSettings
		{
			[JsonProperty(PropertyName = "Item Skins", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<string, List<ulong>> ItemSkins;

			[JsonProperty(PropertyName = "Use skins from SkinBox?")]
			public bool UseSkinBox;

			[JsonProperty(PropertyName = "Use skins from PlayerSkins?")]
			public bool UsePlayerSkins;

			[JsonProperty(PropertyName = "Can players install custom skins?")]
			public bool CanCustomSkin;

			[JsonProperty(PropertyName = "Permission to install custom skin")]
			public string Permission;

			[JsonProperty(PropertyName = "Option to disable clan skins?")]
			public bool DisableSkins;

			[JsonProperty(PropertyName = "Default value to disable skins")]
			public bool DefaultValueDisableSkins;
		}

		private class InterfaceSettings
		{
			[JsonProperty(PropertyName = "Color 1")]
			public string Color1;

			[JsonProperty(PropertyName = "Color 2")]
			public string Color2;

			[JsonProperty(PropertyName = "Color 3")]
			public string Color3;

			[JsonProperty(PropertyName = "Color 4")]
			public string Color4;

			[JsonProperty(PropertyName = "Color 5")]
			public string Color5;

			[JsonProperty(PropertyName = "Color 6")]
			public string Color6;

			[JsonProperty(PropertyName = "Use value abbreviation?")]
			public bool ValueAbbreviation;

			[JsonProperty(PropertyName = "Top Clans Columns", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<ColumnSettings> TopClansColumns;

			[JsonProperty(PropertyName = "Top Players Columns",
				ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<ColumnSettings> TopPlayersColumns;

			[JsonProperty(PropertyName = "Profile Buttons",
				ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<BtnConf> ProfileButtons;

			[JsonProperty(PropertyName = "Clan Member Profile Fields",
				ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<ColumnSettings> ClanMemberProfileFields;

			[JsonProperty(PropertyName = "Top Player Profile Fields",
				ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<ColumnSettings> TopPlayerProfileFields;
		}

		private abstract class InterfacePosition
		{
			public string AnchorMin;

			public string AnchorMax;

			public string OffsetMin;

			public string OffsetMax;
		}

		private class BtnConf : InterfacePosition
		{
			[JsonProperty(PropertyName = "Enabled")]
			public bool Enabled;

			[JsonProperty(PropertyName = "Close Menu?")]
			public bool CloseMenu;

			[JsonProperty(PropertyName = "Command")]
			public string Command;

			[JsonProperty(PropertyName = "Color")] public IColor Color;

			[JsonProperty(PropertyName = "Title")] public string Title;

			[JsonProperty(PropertyName = "Font Size")]
			public int FontSize;

			[JsonProperty(PropertyName = "Font")] public string Font;

			[JsonProperty(PropertyName = "Align")] [JsonConverter(typeof(StringEnumConverter))]
			public TextAnchor Align;

			[JsonProperty(PropertyName = "Text Color")]
			public IColor TextColor;

			public void Get(ref CuiElementContainer container, ulong target, string parent, string close)
			{
				if (!Enabled) return;

				container.Add(new CuiButton
				{
					RectTransform =
					{
						AnchorMin = AnchorMin, AnchorMax = AnchorMax,
						OffsetMin = OffsetMin, OffsetMax = OffsetMax
					},
					Text =
					{
						Text = $"{Title}",
						Align = Align,
						Font = Font,
						FontSize = FontSize,
						Color = TextColor.Get()
					},
					Button =
					{
						Command = $"clans.sendcmd {Command.Replace("{target}", target.ToString())}",
						Color = Color.Get(),
						Close = CloseMenu ? close : string.Empty
					}
				}, parent);
			}
		}

		private class ColumnSettings
		{
			[JsonProperty(PropertyName = "Width")] public float Width;

			[JsonProperty(PropertyName = "Lang Key")]
			public string LangKey;

			[JsonProperty(PropertyName = "Key")] public string Key;

			[JsonProperty(PropertyName = "Text Format")]
			public string TextFormat;

			[JsonProperty(PropertyName = "Text Align")] [JsonConverter(typeof(StringEnumConverter))]
			public TextAnchor TextAlign;

			[JsonProperty(PropertyName = "Title Font Size")]
			public int TitleFontSize;

			[JsonProperty(PropertyName = "Font Size")]
			public int FontSize;

			public string GetFormat(int top, string values)
			{
				switch (Key)
				{
					case "top":
						return string.Format(TextFormat, top);

					default:
						return string.Format(TextFormat, values);
				}
			}
		}

		private class IColor
		{
			[JsonProperty(PropertyName = "HEX")] public string Hex;

			[JsonProperty(PropertyName = "Opacity (0 - 100)")]
			public readonly float Alpha;

			public string Get()
			{
				if (string.IsNullOrEmpty(Hex)) Hex = "#FFFFFF";

				var str = Hex.Trim('#');
				if (str.Length != 6) throw new Exception(Hex);
				var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
				var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
				var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);

				return $"{(double) r / 255} {(double) g / 255} {(double) b / 255} {Alpha / 100}";
			}

			public IColor(string hex, float alpha)
			{
				Hex = hex;
				Alpha = alpha;
			}
		}

		private class PageSettings
		{
			[JsonProperty(PropertyName = "ID (DON'T CHANGE)")]
			public int ID;

			[JsonProperty(PropertyName = "Key (DON'T CHANGE)")]
			public string Key;

			[JsonProperty(PropertyName = "Enabled?")]
			public bool Enabled;

			[JsonProperty(PropertyName = "Permission")]
			public string Permission;
		}

		private class LimitSettings
		{
			[JsonProperty(PropertyName = "Member Limit")]
			public int MemberLimit;

			[JsonProperty(PropertyName = "Moderator Limit")]
			public int ModeratorLimit;

			[JsonProperty(PropertyName = "Alliances Limit")]
			public int AlliancesLimit;
		}

		private class PurgeSettings
		{
			[JsonProperty(PropertyName = "Enable clan purging")]
			public bool Enabled;

			[JsonProperty(PropertyName = "Purge clans that havent been online for x amount of day")]
			public int OlderThanDays;

			[JsonProperty(PropertyName = "List purged clans in console when purging")]
			public bool ListPurgedClans;

			[JsonProperty(PropertyName = "Wipe clans on new map save")]
			public bool WipeOnNewSave;
		}

		private class ChatSettings
		{
			[JsonProperty(PropertyName = "Enable clan tags in chat?")]
			public bool Enabled;

			[JsonProperty(PropertyName = "Tag format")]
			public string TagFormat;
		}

		private class PermissionSettings
		{
			[JsonProperty(PropertyName = "Use permission to create a clan")]
			public bool UseClanCreating;

			[JsonProperty(PropertyName = "Permission to create a clan")]
			public string ClanCreating;

			[JsonProperty(PropertyName = "Use permission to join a clan")]
			public bool UseClanJoining;

			[JsonProperty(PropertyName = "Permission to join a clan")]
			public string ClanJoining;

			[JsonProperty(PropertyName = "Use permission to kick a clan member")]
			public bool UseClanKick;

			[JsonProperty(PropertyName = "Clan kick permission")]
			public string ClanKick;

			[JsonProperty(PropertyName = "Use permission to leave a clan")]
			public bool UseClanLeave;

			[JsonProperty(PropertyName = "Clan leave permission")]
			public string ClanLeave;

			[JsonProperty(PropertyName = "Use permission to disband a clan")]
			public bool UseClanDisband;

			[JsonProperty(PropertyName = "Clan disband permission")]
			public string ClanDisband;

			[JsonProperty(PropertyName = "Use permission to clan skins")]
			public bool UseClanSkins;

			[JsonProperty(PropertyName = "Use clan skins permission")]
			public string ClanSkins;

			[JsonProperty(PropertyName =
				"Minimum auth level required to view clan info (0 = player, 1 = moderator, 2 = owner)")]
			public int ClanInfoAuthLevel;
		}

		private class AllianceSettings
		{
			[JsonProperty(PropertyName = "Enable clan alliances")]
			public bool Enabled;

			[JsonProperty(PropertyName = "Enable friendly fire (allied clans)")]
			public bool UseFF;

			[JsonProperty(PropertyName = "Default friendly fire value")]
			public bool DefaultFF;

			[JsonProperty(PropertyName = "General friendly fire (only the leader of the clan can enable/disable it)")]
			public readonly bool GeneralFriendlyFire = false;

			[JsonProperty(PropertyName = "Can moderators toggle general friendly fire?")]
			public readonly bool ModersGeneralFF = false;

			[JsonProperty(PropertyName = "Can players toggle general friendly fire?")]
			public readonly bool PlayersGeneralFF = false;
		}

		protected override void LoadConfig()
		{
			base.LoadConfig();
			try
			{
				_config = Config.ReadObject<Configuration>();
				if (_config == null) throw new Exception();

				if (_config.Version < Version)
					UpdateConfigValues();

				SaveConfig();
			}
			catch (Exception ex)
			{
				PrintError($"Your configuration file contains an error. Using default configuration values.\n{ex}");

				LoadDefaultConfig();
			}
		}

		private void UpdateConfigValues()
		{
			PrintWarning("Config update detected! Updating config values...");

			var baseConfig = new Configuration();

			if (_config.Version < new VersionNumber(1, 0, 15))
			{
				_config.Skins.DisableSkins = baseConfig.Skins.DisableSkins;
				_config.Skins.DefaultValueDisableSkins = baseConfig.Skins.DefaultValueDisableSkins;

				_config.PermissionSettings.UseClanSkins = baseConfig.PermissionSettings.UseClanSkins;
				_config.PermissionSettings.ClanSkins = baseConfig.PermissionSettings.ClanSkins;
			}

			if (_config.Version < new VersionNumber(1, 1, 0)) StartConvertOldData();

			_config.Version = Version;
			PrintWarning("Config update completed!");
		}

		protected override void SaveConfig()
		{
			Config.WriteObject(_config);
		}

		protected override void LoadDefaultConfig()
		{
			_config = new Configuration();
		}

		#endregion

		#region Data

		private Dictionary<string, ClanData> _clanByTag = new Dictionary<string, ClanData>();

		private List<ClanData> _clansList = new List<ClanData>();

		private void SaveClans()
		{
			Interface.Oxide.DataFileSystem.WriteObject($"{Name}/ClansList", _clansList);
		}

		private void LoadClans()
		{
			try
			{
				_clansList = Interface.Oxide.DataFileSystem.ReadObject<List<ClanData>>($"{Name}/ClansList");
			}
			catch (Exception e)
			{
				PrintError(e.ToString());
			}

			if (_clansList == null) _clansList = new List<ClanData>();

			_clansList.ForEach(clan => clan.Load());
		}

		private class InviteData
		{
			[JsonProperty(PropertyName = "Inviter Name")]
			public string InviterName;

			[JsonProperty(PropertyName = "Inviter Id")]
			public ulong InviterId;
		}

		private class ClanData
		{
			[JsonProperty(PropertyName = "Clan Tag")]
			public string ClanTag;

			[JsonProperty(PropertyName = "Tag Color")]
			public string TagColor;

			[JsonProperty(PropertyName = "Avatar")]
			public string Avatar;

			[JsonProperty(PropertyName = "Leader ID")]
			public ulong LeaderID;

			[JsonProperty(PropertyName = "Leader Name")]
			public string LeaderName;

			[JsonProperty(PropertyName = "Description")]
			public string Description;

			[JsonProperty(PropertyName = "Creation Time")]
			public DateTime CreationTime;

			[JsonProperty(PropertyName = "Last Online Time")]
			public DateTime LastOnlineTime;

			[JsonProperty(PropertyName = "Friendly Fire")]
			public bool FriendlyFire;

			[JsonProperty(PropertyName = "Ally Friendly Fire")]
			public bool AllyFriendlyFire;

			[JsonProperty(PropertyName = "Moderators", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<ulong> Moderators = new List<ulong>();

			[JsonProperty(PropertyName = "Members", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<ulong> Members = new List<ulong>();

			[JsonProperty(PropertyName = "Resource Standarts", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public readonly Dictionary<int, ResourceStandart> ResourceStandarts =
				new Dictionary<int, ResourceStandart>();

			[JsonProperty(PropertyName = "Skins", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public readonly Dictionary<string, ulong> Skins = new Dictionary<string, ulong>();

			[JsonProperty(PropertyName = "Alliances", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public readonly List<string> Alliances = new List<string>();

			[JsonProperty(PropertyName = "Team ID")]
			public ulong TeamID;

			[JsonIgnore] public int Top;

			[JsonIgnore]
			private RelationshipManager.PlayerTeam Team =>
				RelationshipManager.ServerInstance.FindTeam(TeamID) ?? FindOrCreateTeam();

			#region Info

			public bool IsOwner(string userId)
			{
				return IsOwner(ulong.Parse(userId));
			}

			public bool IsOwner(ulong userId)
			{
				return LeaderID == userId;
			}

			public bool IsModerator(string userId)
			{
				return IsModerator(ulong.Parse(userId));
			}

			public bool IsModerator(ulong userId)
			{
				return Moderators.Contains(userId) || IsOwner(userId);
			}

			public bool IsMember(string userId)
			{
				return IsMember(ulong.Parse(userId));
			}

			public bool IsMember(ulong userId)
			{
				return Members.Contains(userId);
			}

			public string GetRoleColor(ulong userId)
			{
				return IsOwner(userId) ? _config.Colors.Owner :
					IsModerator(userId) ? _config.Colors.Moderator : _config.Colors.Member;
			}

			public string GetHexTagColor()
			{
				return string.IsNullOrEmpty(TagColor) ? _config.Tags.TagColor.DefaultColor : TagColor;
			}

			public bool CanEditTagColor(ulong userId)
			{
				if (_config.Tags.TagColor.Owners)
					if (IsOwner(userId))
						return true;

				if (_config.Tags.TagColor.Moderators)
					if (IsModerator(userId))
						return true;

				if (_config.Tags.TagColor.Players)
					if (IsMember(userId))
						return true;

				return false;
			}

			#endregion

			#region Create

			public static ClanData CreateNewClan(string clanTag, BasePlayer leader)
			{
				var clan = new ClanData
				{
					ClanTag = clanTag,
					LeaderID = leader.userID,
					LeaderName = leader.displayName,
					Avatar = _config.DefaultAvatar,
					Members = new List<ulong>
					{
						leader.userID
					},
					CreationTime = DateTime.Now,
					LastOnlineTime = DateTime.Now,
					Top = _instance.TopClans.Count + 1
				};

				#region Invites

				_invites.RemovePlayerInvites(leader.userID);

				#endregion

				_instance._clansList.Add(clan);
				_instance._clanByTag[clanTag] = clan;

				if (_config.TagInName)
					leader.displayName = $"[{clanTag}] {_instance.GetPlayerName(leader)}";

				if (_config.AutoTeamCreation)
					clan.FindOrCreateTeam();

				ClanCreate(clanTag);

				_instance.NextTick(() => _instance.HandleTop());
				return clan;
			}

			#endregion

			#region Main

			public void Rename(string newName)
			{
				if (string.IsNullOrEmpty(newName)) return;

				var oldName = ClanTag;
				ClanTag = newName;

				_invites.AllianceInvites.ToList().ForEach(invite =>
				{
					if (invite.SenderClanTag == oldName) invite.SenderClanTag = newName;

					if (invite.TargetClanTag == oldName) invite.TargetClanTag = newName;
				});

				foreach (var check in Alliances)
				{
					var clan = _instance.FindClanByTag(check);
					if (clan != null)
					{
						clan.Alliances.Remove(oldName);
						clan.Alliances.Add(newName);
					}
				}

				_invites.PlayersInvites.ForEach(invite =>
				{
					if (invite.ClanTag == oldName)
						invite.ClanTag = newName;
				});

				foreach (var player in Players)
					_instance?.OnPlayerConnected(player);

				ClanUpdate(ClanTag);
			}

			public void Disband()
			{
				var memberUserIDs = Members.Select(x => x.ToString());

				ClanDisbanded(memberUserIDs);
				ClanDisbanded(ClanTag, memberUserIDs);

				Members.ToList().ForEach(member => Kick(member, true));

				ClanDestroy(ClanTag);

				_instance?._clansList.ForEach(clanData =>
				{
					clanData.Alliances.Remove(ClanTag);

					_invites.RemoveAllyInvite(ClanTag);
				});

				if (_config.AutoTeamCreation)
					Team?.members.ToList().ForEach(member =>
					{
						Team.RemovePlayer(member);

						var player = RelationshipManager.FindByID(member);
						if (player != null)
							player.ClearTeam();
					});

				_instance?._clanByTag.Remove(ClanTag);
				_instance?._clansList.Remove(this);

				_instance?.NextTick(() => _instance.HandleTop());
			}

			public void Join(BasePlayer player)
			{
#if TESTING
				using (new StopwatchWrapper("Clan join took {0}ms."))
#endif
				{
					Members.Add(player.userID);

					if (_config.TagInName)
						player.displayName = $"[{ClanTag}] {player.displayName}";

					if (_config.AutoTeamCreation)
					{
						player.Team?.RemovePlayer(player.userID);

						Team?.AddPlayer(player);
					}

					if (Members.Count >= _config.LimitSettings.MemberLimit) _invites.RemovePlayerClanInvites(ClanTag);

					_invites.RemovePlayerInvites(player.userID);

					ClanMemberJoined(player.UserIDString, ClanTag);

					ClanMemberJoined(player.UserIDString, Members.Select(x => x.ToString()));

					ClanUpdate(ClanTag);
				}
			}

			public void Kick(ulong target, bool disband = false)
			{
				var targetStringId = target.ToString();

				Members.Remove(target);
				Moderators.Remove(target);

				_instance?._playerToClan.Remove(targetStringId);

				if (_config.TagInName)
				{
					var data = PlayerData.GetOrLoad(targetStringId);
					if (data != null)
					{
						var player = BasePlayer.FindByID(target);
						if (player != null)
							player.displayName = data.DisplayName;
					}
				}

				if (!disband)
				{
					if (_config.AutoTeamCreation && Team != null) Team.RemovePlayer(target);

					if (Members.Count == 0)
					{
						Disband();
					}
					else
					{
						if (LeaderID == target)
							SetLeader((Moderators.Count > 0 ? Moderators : Members).GetRandom());
					}
				}

				ClanMemberGone(targetStringId, Members.Select(x => x.ToString()));

				ClanMemberGone(targetStringId, ClanTag);

				ClanUpdate(ClanTag);
			}

			public void SetModer(ulong target)
			{
				if (!Moderators.Contains(target))
					Moderators.Add(target);

				ClanUpdate(ClanTag);
			}

			public void UndoModer(ulong target)
			{
				Moderators.Remove(target);

				ClanUpdate(ClanTag);
			}

			public void SetLeader(ulong target)
			{
				var data = PlayerData.GetOrLoad(target.ToString());
				if (data != null)
					LeaderName = data.DisplayName;

				LeaderID = target;

				if (_config.AutoTeamCreation)
					Team.SetTeamLeader(target);

				ClanUpdate(ClanTag);
			}

			#endregion

			#region Additionall

			[JsonIgnore] public float TotalScores;

			[JsonIgnore] public float TotalFarm;

			public void Load()
			{
				_instance._clanByTag[ClanTag] = this;

				UpdateScore();

				UpdateTotalFarm();
			}

			public void UpdateScore()
			{
				TotalScores = GetScore();
			}

			public void UpdateTotalFarm()
			{
				TotalFarm = GetTotalFarm();
			}

			public RelationshipManager.PlayerTeam FindOrCreateTeam()
			{
				var leaderTeam = RelationshipManager.ServerInstance.FindPlayersTeam(LeaderID);
				if (leaderTeam != null)
				{
					if (leaderTeam.teamLeader == LeaderID)
					{
						TeamID = leaderTeam.teamID;
						return leaderTeam;
					}

					leaderTeam.RemovePlayer(LeaderID);
				}

				return CreateTeam();
			}

			public RelationshipManager.PlayerTeam CreateTeam()
			{
				var team = RelationshipManager.ServerInstance.CreateTeam();
				team.teamLeader = LeaderID;
				AddPlayer(LeaderID, team);

				TeamID = team.teamID;

				return team;
			}

			public void AddPlayer(ulong member, RelationshipManager.PlayerTeam team = null)
			{
				if (team == null)
					team = Team;

				if (!team.members.Contains(member))
					team.members.Add(member);

				if (member == LeaderID)
					team.teamLeader = LeaderID;

				RelationshipManager.ServerInstance.playerToTeam[member] = team;

				var player = RelationshipManager.FindByID(member);
				if (player != null)
				{
					if (player.Team != null && player.Team.teamID != team.teamID)
					{
						player.Team.RemovePlayer(player.userID);
						player.ClearTeam();
					}

					player.currentTeam = team.teamID;

					team.MarkDirty();
					player.SendNetworkUpdate();
				}
			}

			public float GetScore()
			{
				return Members.Sum(member => PlayerData.GetOrLoad(member.ToString())?.Score ?? 0f);
			}

			public string Scores()
			{
				return GetValue(TotalScores);
			}

			public float GetTotalFarm()
			{
				var sum = 0f;

				PlayerData data;
				Members.ForEach(member =>
				{
					if ((data = PlayerData.GetOrLoad(member.ToString())) != null)
						sum += data.GetTotalFarm(this);
				});

				return (float) Math.Round(sum / Members.Count, 3);
			}

			public JObject ToJObject()
			{
				return new JObject
				{
					["tag"] = ClanTag,
					["description"] = Description,
					["owner"] = LeaderID,
					["moderators"] = new JArray(Moderators),
					["members"] = new JArray(Members),
					["invitedallies"] = new JArray(_invites.PlayersInvites.FindAll(x => x.ClanTag == ClanTag))
				};
			}

			public void SetSkin(string shortName, ulong skin)
			{
				Skins[shortName] = skin;

				foreach (var player in Players.Where(x => _instance.CanUseSkins(x)))
				foreach (var item in player.inventory.AllItems())
					if (item.info.shortname == shortName)
						ApplySkinToItem(item, skin);
			}

			public string GetParams(string value)
			{
				switch (value)
				{
					case "name":
						return ClanTag;
					case "leader":
						return LeaderName;
					case "members":
						return Members.Count.ToString();
					case "score":
						return Scores();
					default:
						return Math.Round(
								Members.Sum(member => PlayerData.GetOrLoad(member.ToString())?.GetValue(value) ?? 0f))
							.ToString(CultureInfo.InvariantCulture);
				}
			}

			public void UpdateLeaderName(string name)
			{
				LeaderName = name;
			}

			#endregion

			#region Utils

			[JsonIgnore]
			public IEnumerable<BasePlayer> Players
			{
				get { return Members.Select(BasePlayer.FindByID).Where(player => player != null); }
			}

			public void Broadcast(string key, params object[] obj)
			{
				foreach (var player in Players) _instance.Reply(player, key, obj);
			}

			#endregion

			#region Clan Info

			public string GetClanInfo(BasePlayer player)
			{
				var str = new StringBuilder();
				str.Append(_instance.Msg(player, ClanInfoTitle));
				str.Append(_instance.Msg(player, ClanInfoTag, ClanTag));

				if (!string.IsNullOrEmpty(Description))
					str.Append(_instance.Msg(player, ClanInfoDescription, Description));

				var online = Pool.GetList<string>();
				var offline = Pool.GetList<string>();

				foreach (var kvp in Members)
				{
					var member = string.Format(COLORED_LABEL, GetRoleColor(kvp),
						PlayerData.GetOrLoad(kvp.ToString()).DisplayName);

					if (IsOnline(kvp))
						online.Add(member);
					else offline.Add(member);
				}

				if (online.Count > 0)
					str.Append(_instance.Msg(player, ClanInfoOnline, online.ToSentence()));

				if (offline.Count > 0)
					str.Append(_instance.Msg(player, ClanInfoOffline, offline.ToSentence()));

				Pool.FreeList(ref online);
				Pool.FreeList(ref offline);

				str.Append(_instance.Msg(player, ClanInfoEstablished, CreationTime));
				str.Append(_instance.Msg(player, ClanInfoLastOnline, LastOnlineTime));

				if (_config.AllianceSettings.Enabled)
					str.Append(_instance.Msg(player, ClanInfoAlliances,
						Alliances.Count > 0 ? Alliances.ToSentence() : _instance.Msg(player, ClanInfoAlliancesNone)));

				return str.ToString();
			}

			#endregion
		}

		private class ResourceStandart
		{
			public string ShortName;

			public int Amount;

			[JsonIgnore] private int _itemId = -1;

			[JsonIgnore]
			public int itemId
			{
				get
				{
					if (_itemId == -1)
						_itemId = ItemManager.FindItemDefinition(ShortName)?.itemid ?? -1;

					return _itemId;
				}
			}

			[JsonIgnore] private ICuiComponent _image;

			public CuiElement GetImage(string aMin, string aMax, string oMin, string oMax, string parent,
				string name = null)
			{
				if (_image == null)
					_image = new CuiImageComponent
					{
						ItemId = itemId
					};

				return new CuiElement
				{
					Name = string.IsNullOrEmpty(name) ? CuiHelper.GetGuid() : name,
					Parent = parent,
					Components =
					{
						_image,
						new CuiRectTransformComponent
						{
							AnchorMin = aMin, AnchorMax = aMax,
							OffsetMin = oMin, OffsetMax = oMax
						}
					}
				};
			}
		}

		#region Stats

		private void AddToStats(ulong member, string shortName, int amount = 1)
		{
			if (!member.IsSteamId() || !PlayerHasClan(member)) return;

			var data = PlayerData.GetOrCreate(member.ToString());
			if (data == null) return;

			if (data.Stats.ContainsKey(shortName))
				data.Stats[shortName] += amount;
			else
				data.Stats.Add(shortName, amount);

			if (_config.ScoreTable.ContainsKey(shortName))
			{
				var clanData = data.GetClan();
				if (clanData != null)
					clanData.TotalScores += (float) Math.Round(amount * _config.ScoreTable[shortName]);
			}
		}

		private float GetStatsValue(ulong member, string shortname)
		{
			var data = PlayerData.GetOrCreate(member.ToString());
			if (data == null) return 0;

			switch (shortname)
			{
				case "total":
				{
					return data.Score;
				}
				case "kd":
				{
					return data.KD;
				}
				case "resources":
				{
					return data.Resources;
				}
				default:
				{
					float result;
					return data.Stats.TryGetValue(shortname, out result) ? result : 0;
				}
			}
		}

		#endregion

		#endregion

		#region Hooks

		private void Init()
		{
			_instance = this;

			LoadClans();

			LoadInvites();

			UnsubscribeHooks();

			RegisterCommands();

			RegisterPermissions();

			PurgeClans();

#if TESTING
			StopwatchWrapper.OnComplete = DebugMessage;
#endif
		}

		private void OnServerInitialized()
		{
			LoadImages();

			FillingStandartItems();

			LoadSkins();

			LoadColors();

			if (_config.ChatSettings.Enabled)
				BetterChat?.Call("API_RegisterThirdPartyTitle", this,
					new Func<IPlayer, string>(BetterChat_FormattedClanTag));

			if (_config.Tags.CheckingCharacters)
				_tagFilter = new Regex($"[^a-zA-Z0-9{_config.Tags.AllowedCharacters}]");

			foreach (var player in BasePlayer.activePlayerList)
				OnPlayerConnected(player);

			FillingTeams();

			Puts($"Loaded {_clansList.Count} clans!");

			NextTick(HandleTop);

			timer.Every(_config.TopRefreshRate, HandleTop);
		}

		private void OnServerSave()
		{
			timer.In(Random.Range(2f, 10f), PlayerData.Save);

			timer.In(Random.Range(2f, 10f), SaveClans);
		}

		private void Unload()
		{
			if (_actionAvatars != null)
				ServerMgr.Instance.StopCoroutine(_actionAvatars);

			if (_actionConvert != null)
				ServerMgr.Instance.StopCoroutine(_actionConvert);

			if (_handleTop != null)
				ServerMgr.Instance.StopCoroutine(_handleTop);

			foreach (var player in BasePlayer.activePlayerList)
			{
				CuiHelper.DestroyUi(player, Layer);
				CuiHelper.DestroyUi(player, ModalLayer);

				PlayerData.SaveAndUnload(player.UserIDString);

				if (_config.TagInName)
				{
					var data = PlayerData.GetOrLoad(player.UserIDString);
					if (data != null)
						player.displayName = data.DisplayName;
				}
			}

			SaveClans();

			SaveInvites();

			_instance = null;
			_config = null;
			_invites = null;
		}

		private void OnNewSave(string filename)
		{
			if (!_config.PurgeSettings.WipeOnNewSave) return;

			_clansList.Clear();

			SaveClans();
		}

		private void OnPlayerConnected(BasePlayer player)
		{
			if (player == null || !player.userID.IsSteamId()) return;

			GetAvatar(player.UserIDString,
				avatar => ImageLibrary?.Call("AddImage", avatar, $"avatar_{player.UserIDString}"));

			var data = PlayerData.GetOrCreate(player.UserIDString);
			if (data == null) return;

			data.DisplayName = GetPlayerName(player);
			data.LastLogin = DateTime.Now;

			PlayerData.Save(player.UserIDString);

			var clan = data.GetClan();
			if (clan == null)
			{
				if (_config.ForceClanCreateTeam && player.Team != null) CreateClanUi(player);

				return;
			}

			clan.LastOnlineTime = DateTime.Now;

			if (_config.TagInName)
				player.displayName = $"[{clan.ClanTag}] {data.DisplayName}";

			if (_config.AutoTeamCreation) clan.AddPlayer(player.userID);

			if (clan.IsOwner(player.userID))
				clan.UpdateLeaderName(data.DisplayName);
		}

		private void OnPlayerDisconnected(BasePlayer player)
		{
			if (player == null || !player.userID.IsSteamId()) return;

			var clan = FindClanByPlayer(player.UserIDString);
			if (clan != null)
				clan.LastOnlineTime = DateTime.Now;

			PlayerData.SaveAndUnload(player.UserIDString);
		}

		#region Stats

		#region Kills

		private void OnPlayerDeath(BasePlayer player, HitInfo info)
		{
			if (player == null || info == null ||
			    (player.ShortPrefabName == "player" && !player.userID.IsSteamId())) return;

			var attacker = info.InitiatorPlayer;
			if (attacker == null || !attacker.userID.IsSteamId()
			                     || IsTeammates(player.userID, attacker.userID)) return;

			if (player.userID.IsSteamId())
			{
				AddToStats(attacker.userID, "kills");
				AddToStats(player.userID, "deaths");
			}
			else
			{
				AddToStats(attacker.userID, player.ShortPrefabName);
			}
		}

		#endregion

		#region Gather

		private void OnCollectiblePickup(CollectibleEntity collectible, BasePlayer player)
		{
			if (collectible == null || collectible.itemList == null) return;

			foreach (var itemAmount in collectible.itemList)
				if (itemAmount.itemDef != null)
					OnGather(player, itemAmount.itemDef.shortname, (int) itemAmount.amount);
		}

		private void OnCropGather(GrowableEntity plant, Item item, BasePlayer player)
		{
			OnGather(player, item.info.shortname, item.amount);
		}

		private void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
		{
			OnGather(player, item.info.shortname, item.amount);
		}

		private void OnDispenserGather(ResourceDispenser dispenser, BasePlayer player, Item item)
		{
			OnGather(player, item.info.shortname, item.amount);
		}

		private void OnGather(BasePlayer player, string shortname, int amount)
		{
			if (player == null || string.IsNullOrEmpty(shortname) || amount <= 0) return;

			AddToStats(player.userID, shortname, amount);
		}

		#endregion

		#region Loot

		private void OnItemRemovedFromContainer(ItemContainer container, Item item)
		{
			ulong id = 0U;
			if (container.entityOwner != null)
				id = container.entityOwner.OwnerID;
			else if (container.playerOwner != null)
				id = container.playerOwner.userID;

			if (!_looters.ContainsKey(item.uid))
				_looters.Add(item.uid, id);
		}

		private void CanMoveItem(Item item, PlayerInventory playerLoot, uint targetContainer, int targetSlot,
			int amount)
		{
			if (item == null || playerLoot == null) return;

			var player = playerLoot.GetComponent<BasePlayer>();
			if (player == null) return;

			if (!(item.GetRootContainer()?.entityOwner is LootContainer))
				return;

			if (targetContainer == 0 && targetSlot == -1) AddToStats(player.userID, item.info.shortname, item.amount);
		}

		private void OnItemPickup(Item item, BasePlayer player)
		{
			if (item == null || player == null) return;

			if (_looters.ContainsKey(item.uid))
			{
				if (_looters[item.uid] != player.userID)
				{
					AddToStats(player.userID, item.info.shortname, item.amount);
					_looters.Remove(item.uid);
				}
			}
			else
			{
				_looters.Add(item.uid, player.userID);
			}
		}

		#endregion

		#region Entity Death

		private readonly Dictionary<uint, BasePlayer> _lastHeli = new Dictionary<uint, BasePlayer>();

		private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
		{
			var helicopter = entity as BaseHelicopter;
			if (helicopter != null && helicopter.net != null && info.InitiatorPlayer != null)
				_lastHeli[helicopter.net.ID] = info.InitiatorPlayer;

			if (_config.UseFriendlyFire)
			{
				var player = entity as BasePlayer;
				if (player == null) return;

				var initiatorPlayer = info.InitiatorPlayer;
				if (initiatorPlayer == null || player == initiatorPlayer) return;

				var data = PlayerData.GetOrLoad(initiatorPlayer.UserIDString);
				var clan = data?.GetClan();
				if (clan == null) return;

				if (_config.ZMSettings.Enabled && ZoneManager != null)
				{
					var playerZones = ZM_GetPlayerZones(player);
					if (playerZones.Any(x => _config.ZMSettings.FFAllowlist.Contains(x)))
						return;
				}

				var value = _config.GeneralFriendlyFire ? clan.FriendlyFire : data.FriendlyFire;
				if (!value && clan.IsMember(player.userID))
				{
					info.damageTypes.ScaleAll(0);

					Reply(initiatorPlayer, CannotDamage);
				}

				value = _config.AllianceSettings.GeneralFriendlyFire ? clan.AllyFriendlyFire : data.AllyFriendlyFire;
				if (!value && clan.Alliances.Select(FindClanByTag).Any(x => x.IsMember(player.userID)))
				{
					info.damageTypes.ScaleAll(0);

					Reply(initiatorPlayer, AllyCannotDamage);
				}
			}
		}

		private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
		{
			if (entity == null || info == null) return;

			if (entity is BaseHelicopter)
			{
				if (_lastHeli.ContainsKey(entity.net.ID))
				{
					var basePlayer = _lastHeli[entity.net.ID];
					if (basePlayer != null)
						AddToStats(basePlayer.userID, "helicopter");
				}

				return;
			}

			var player = info.InitiatorPlayer;
			if (player == null) return;

			if (entity is BradleyAPC)
				AddToStats(player.userID, "bradley");
			else if (entity.name.Contains("barrel"))
				AddToStats(player.userID, "barrel");
			else if (_config.ScoreTable.ContainsKey(entity.ShortPrefabName))
				AddToStats(player.userID, entity.ShortPrefabName);
		}

		#endregion

		#region FF Turrets

		private object CanBeTargeted(BasePlayer target, AutoTurret turret)
		{
			if (target.IsNull() ||
			    turret.IsNull() ||
			    target.limitNetworking ||
			    (turret is NPCAutoTurret && !target.userID.IsSteamId()) ||
			    target.userID == turret.OwnerID)
				return null;

			var data = PlayerData.GetOrLoad(turret.OwnerID.ToString());
			var clan = data?.GetClan();
			if (clan == null) return null;

			var value = _config.GeneralFriendlyFire ? clan.FriendlyFire : data.FriendlyFire;
			if (!value && clan.IsMember(target.userID)) return false;

			value = _config.AllianceSettings.GeneralFriendlyFire ? clan.AllyFriendlyFire : data.AllyFriendlyFire;
			if (!value && clan.Alliances.Select(FindClanByTag).Any(x => x.IsMember(target.userID))) return false;

			return null;
		}

		#endregion

		#region Craft

		private void OnItemCraftFinished(ItemCraftTask task, Item item)
		{
			var player = task?.owner;
			if (player == null || item == null) return;

			AddToStats(player.userID, item.info.shortname, item.amount);
		}

		#endregion

		#endregion

		#region Skins

		private void OnSkinBoxSkinsLoaded(Hash<string, HashSet<ulong>> skins)
		{
			if (skins == null) return;

			_config.Skins.ItemSkins = skins.ToDictionary(x => x.Key, y => y.Value.ToList());

			if (_config.Skins.ItemSkins.Count > 0)
				SaveConfig();
		}

		private void OnItemAddedToContainer(ItemContainer container, Item item)
		{
			if (container == null || item == null) return;

			var player = container.GetOwnerPlayer();
			if (player == null) return;

			var clan = FindClanByPlayer(player.UserIDString);
			if (clan == null) return;

			if (CanUseSkins(player) && _config.Skins.ItemSkins.ContainsKey(item.info.shortname) &&
			    clan.Skins.ContainsKey(item.info.shortname))
			{
				var skin = clan.Skins[item.info.shortname];
				if (skin != 0)
				{
					if (item.info.category == ItemCategory.Attire)
					{
						if (container == player.inventory.containerWear) ApplySkinToItem(item, skin);
					}
					else
					{
						ApplySkinToItem(item, skin);
					}
				}
			}

			if (_config.Statistics.Loot)
			{
				if (_looters.ContainsKey(item.uid))
				{
					if (container.playerOwner != null)
						if (_looters[item.uid] != container.playerOwner.userID)
						{
							AddToStats(player.userID, item.info.shortname, item.amount);
							_looters.Remove(item.uid);
						}
				}
				else if (container.playerOwner != null)
				{
					_looters.Add(item.uid, container.playerOwner.userID);
				}
			}
		}

		#endregion

		#region Team

		private void OnTeamCreate(BasePlayer player)
		{
			if (player == null) return;

			CreateClanUi(player);
		}

		private void OnTeamLeave(RelationshipManager.PlayerTeam team, BasePlayer player)
		{
			if (team == null || player == null) return;

			FindClanByPlayer(player.UserIDString)?.Kick(player.userID);
		}

		private void OnTeamKick(RelationshipManager.PlayerTeam team, BasePlayer player, ulong target)
		{
			if (team == null || player == null) return;

			FindClanByPlayer(player.UserIDString)?.Kick(target);
		}

		private void OnTeamInvite(BasePlayer inviter, BasePlayer target)
		{
			if (inviter == null || target == null) return;

			SendInvite(inviter, target.userID);
		}

		private void OnTeamPromote(RelationshipManager.PlayerTeam team, BasePlayer newLeader)
		{
			if (team == null || newLeader == null) return;

			FindClanByPlayer(team.teamLeader.ToString())?.SetLeader(newLeader.userID);
		}

		private object OnTeamAcceptInvite(RelationshipManager.PlayerTeam team, BasePlayer player)
		{
			if (team == null || player == null) return null;

#if TESTING
			using (new StopwatchWrapper("Checking for an invite took {0}ms."))
#endif
			{
				if (!HasInvite(player)) return null;

				var data = PlayerData.GetOrLoad(player.UserIDString);
				if (data == null) return null;

				if (data.GetClan() != null)
				{
					Reply(player, AlreadyClanMember);
					return true;
				}

				var clan = FindClanByPlayer(team.teamLeader.ToString());
				if (clan == null) return true;

				if (clan.Members.Count >= _config.LimitSettings.MemberLimit)
				{
					Reply(player, ALotOfMembers);
					return true;
				}

				var inviteData = data.GetInviteByTag(clan.ClanTag);
				if (inviteData == null) return true;

				clan.Join(player);
				Reply(player, ClanJoined, clan.ClanTag);

				var inviter = BasePlayer.FindByID(inviteData.InviterId);
				if (inviter != null)
					Reply(inviter, WasInvited, data.DisplayName);
			}

			return null;
		}

		#endregion

		#region Chat

		private void OnPluginLoaded(Plugin plugin)
		{
			if (plugin.Name == "BetterChat")
				Interface.CallHook("API_RegisterThirdPartyTitle", this,
					new Func<IPlayer, string>(BetterChat_FormattedClanTag));
		}

		#endregion

		#endregion

		#region Commands

		private void CmdClans(IPlayer cov, string command, string[] args)
		{
			var player = cov?.Object as BasePlayer;
			if (player == null) return;

			if (args.Length == 0)
			{
				var clan = FindClanByPlayer(player.UserIDString);

				MainUi(player, clan == null ? 3 : 0, first: true);
				return;
			}

			switch (args[0])
			{
				case "create":
				{
					if (args.Length < 2)
					{
						SendReply(player, $"Error syntax! Use: /{command} {args[0]} <clan tag>");
						return;
					}

					if (_config.PermissionSettings.UseClanCreating &&
					    !string.IsNullOrEmpty(_config.PermissionSettings.ClanCreating) &&
					    !player.HasPermission(_config.PermissionSettings.ClanCreating))
					{
						Reply(player, NoPermCreateClan);
						return;
					}

					if (PlayerHasClan(player.userID))
					{
						Reply(player, AlreadyClanMember);
						return;
					}

					var tag = string.Join(" ", args.Skip(1));
					if (string.IsNullOrEmpty(tag) || tag.Length < _config.Tags.TagMin ||
					    tag.Length > _config.Tags.TagMax)
					{
						Reply(player, ClanTagLimit, _config.Tags.TagMin, _config.Tags.TagMax);
						return;
					}

					tag = tag.Replace(" ", "");

					if (_config.Tags.BlockedWords.Exists(word => tag.Contains(word, CompareOptions.OrdinalIgnoreCase)))
					{
						Reply(player, ContainsForbiddenWords);
						return;
					}

					if (_config.Tags.CheckingCharacters && _tagFilter.IsMatch(tag))
					{
						Reply(player, ContainsForbiddenWords);
						return;
					}

					var clan = FindClanByTag(tag);
					if (clan != null)
					{
						Reply(player, ClanExists);
						return;
					}

					clan = ClanData.CreateNewClan(tag, player);
					if (clan == null) return;

					Reply(player, ClanCreated, tag);
					break;
				}

				case "disband":
				{
					var clan = FindClanByPlayer(player.UserIDString);
					if (clan == null)
					{
						Reply(player, NotClanMember);
						return;
					}

					if (!clan.IsOwner(player.userID))
					{
						Reply(player, NotClanLeader);
						return;
					}

					if (_config.PermissionSettings.UseClanDisband &&
					    !string.IsNullOrEmpty(_config.PermissionSettings.ClanDisband) &&
					    !player.HasPermission(_config.PermissionSettings.ClanDisband))
					{
						Reply(player, NoPermDisbandClan);
						return;
					}

					clan.Disband();
					Reply(player, ClanDisbandedTitle);
					break;
				}

				case "leave":
				{
					var clan = FindClanByPlayer(player.UserIDString);
					if (clan == null)
					{
						Reply(player, NotClanMember);
						return;
					}

					if (_config.PermissionSettings.UseClanLeave &&
					    !string.IsNullOrEmpty(_config.PermissionSettings.ClanLeave) &&
					    !player.HasPermission(_config.PermissionSettings.ClanLeave))
					{
						Reply(player, NoPermLeaveClan);
						return;
					}

					clan.Kick(player.userID);
					Reply(player, ClanLeft);
					break;
				}

				case "promote":
				{
					if (args.Length < 2)
					{
						SendReply(player, $"Error syntax! Use: /{command} {args[0]} <name/steamid>");
						return;
					}

					var clan = FindClanByPlayer(player.UserIDString);
					if (clan == null)
					{
						Reply(player, NotClanMember);
						return;
					}

					if (!clan.IsOwner(player.userID))
					{
						Reply(player, NotClanLeader);
						return;
					}

					var target = covalence.Players.FindPlayer(args[1]);
					if (target == null)
					{
						Reply(player, PlayerNotFound, args[1]);
						return;
					}

					if (clan.IsModerator(target.Id))
					{
						Reply(player, ClanAlreadyModer, target.Name);
						return;
					}

					if (clan.Moderators.Count >= _config.LimitSettings.ModeratorLimit)
					{
						Reply(player, ALotOfModers);
						return;
					}

					clan.SetModer(ulong.Parse(target.Id));
					Reply(player, PromotedToModer, target.Name);
					break;
				}

				case "demote":
				{
					if (args.Length < 2)
					{
						SendReply(player, $"Error syntax! Use: /{command} {args[0]} <name/steamid>");
						return;
					}

					var clan = FindClanByPlayer(player.UserIDString);
					if (clan == null)
					{
						Reply(player, NotClanMember);
						return;
					}

					if (!clan.IsOwner(player.userID))
					{
						Reply(player, NotClanLeader);
						return;
					}

					var target = covalence.Players.FindPlayer(args[1]);
					if (target == null)
					{
						Reply(player, PlayerNotFound, args[1]);
						return;
					}

					if (!clan.IsModerator(target.Id))
					{
						Reply(player, NotClanModer, target.Name);
						return;
					}

					clan.UndoModer(ulong.Parse(target.Id));
					Reply(player, DemotedModer, target.Name);
					break;
				}

				case "invite":
				{
					if (args.Length < 2)
					{
						SendReply(player, $"Error syntax! Use: /{command} {args[0]} <name/steamid>");
						return;
					}

					var clan = FindClanByPlayer(player.UserIDString);
					if (clan == null)
					{
						Reply(player, NotClanMember);
						return;
					}

					var target = covalence.Players.FindPlayer(args[1]);
					if (target == null)
					{
						Reply(player, PlayerNotFound, args[1]);
						return;
					}

					SendInvite(player, ulong.Parse(target.Id));
					break;
				}

				case "withdraw":
				{
					if (args.Length < 2)
					{
						SendReply(player, $"Error syntax! Use: /{command} {args[0]} <name/steamid>");
						return;
					}

					var clan = FindClanByPlayer(player.UserIDString);
					if (clan == null)
					{
						Reply(player, NotClanMember);
						return;
					}

					var target = covalence.Players.FindPlayer(args[1]);
					if (target == null)
					{
						Reply(player, PlayerNotFound, args[1]);
						return;
					}

					WithdrawInvite(player, ulong.Parse(target.Id));
					break;
				}

				case "kick":
				{
					if (args.Length < 2)
					{
						SendReply(player, $"Error syntax! Use: /{command} {args[0]} <name/steamid>");
						return;
					}

					if (_config.PermissionSettings.UseClanKick &&
					    !string.IsNullOrEmpty(_config.PermissionSettings.ClanKick) &&
					    !player.HasPermission(_config.PermissionSettings.ClanKick))
					{
						Reply(player, _config.PermissionSettings.ClanKick);
						return;
					}

					var clan = FindClanByPlayer(player.UserIDString);
					if (clan == null)
					{
						Reply(player, NotClanMember);
						return;
					}

					var target = covalence.Players.FindPlayer(args[1]);
					if (target == null)
					{
						Reply(player, PlayerNotFound, args[1]);
						return;
					}

					if (!clan.IsModerator(player.userID))
					{
						Reply(player, NotModer);
						return;
					}

					clan.Kick(ulong.Parse(target.Id));
					Reply(player, SuccsessKick, target.Name);

					var targetPlayer = target.Object as BasePlayer;
					if (targetPlayer != null)
						Reply(targetPlayer, WasKicked);
					break;
				}

				case "ff":
				{
					var data = PlayerData.GetOrCreate(player.UserIDString);
					if (data == null) return;

					bool value;

					if (_config.GeneralFriendlyFire)
					{
						var clan = FindClanByPlayer(player.UserIDString);
						if (clan == null) return;

						if (!_config.PlayersGeneralFF)
						{
							if (_config.ModersGeneralFF && !clan.IsModerator(player.userID))
							{
								Reply(player, NotModer);
								return;
							}

							if (!clan.IsOwner(player.userID))
							{
								Reply(player, NotClanLeader);
								return;
							}
						}

						clan.FriendlyFire = !clan.FriendlyFire;
						value = clan.FriendlyFire;
					}
					else
					{
						data.FriendlyFire = !data.FriendlyFire;
						value = data.FriendlyFire;
					}

					Reply(player, value ? FFOn : FFOff);
					break;
				}

				case "allyff":
				{
					if (!_config.AllianceSettings.Enabled || !_config.AllianceSettings.UseFF) return;

					var data = PlayerData.GetOrCreate(player.UserIDString);
					if (data == null) return;

					bool value;
					if (_config.AllianceSettings.GeneralFriendlyFire)
					{
						var clan = FindClanByPlayer(player.UserIDString);
						if (clan == null) return;

						if (!_config.AllianceSettings.PlayersGeneralFF)
						{
							if (_config.AllianceSettings.ModersGeneralFF && !clan.IsModerator(player.userID))
							{
								Reply(player, NotModer);
								return;
							}

							if (!clan.IsOwner(player.userID))
							{
								Reply(player, NotClanLeader);
								return;
							}
						}

						clan.AllyFriendlyFire = !clan.AllyFriendlyFire;
						value = clan.AllyFriendlyFire;
					}
					else
					{
						data.AllyFriendlyFire = !data.AllyFriendlyFire;
						value = data.AllyFriendlyFire;
					}

					Reply(player, value ? AllyFFOn : AllyFFOff);
					break;
				}

				case "allyinvite":
				{
					if (!_config.AllianceSettings.Enabled) return;

					if (args.Length < 2)
					{
						SendReply(player, $"Error syntax! Use: /{command} {args[0]} <clanTag>");
						return;
					}

					var clan = FindClanByPlayer(player.UserIDString);
					if (clan == null)
					{
						Reply(player, NotClanMember);
						return;
					}

					if (!clan.IsModerator(player.userID))
					{
						Reply(player, NotModer);
						return;
					}

					var targetClan = FindClanByTag(args[1]);
					if (targetClan == null)
					{
						Reply(player, ClanNotFound, args[1]);
						return;
					}

					AllySendInvite(player, targetClan.ClanTag);
					break;
				}

				case "allywithdraw":
				{
					if (!_config.AllianceSettings.Enabled) return;

					if (args.Length < 2)
					{
						SendReply(player, $"Error syntax! Use: /{command} {args[0]} <clanTag>");
						return;
					}

					var clan = FindClanByPlayer(player.UserIDString);
					if (clan == null)
					{
						Reply(player, NotClanMember);
						return;
					}

					if (!clan.IsModerator(player.userID))
					{
						Reply(player, NotModer);
						return;
					}

					var targetClan = FindClanByTag(args[1]);
					if (targetClan == null)
					{
						Reply(player, ClanNotFound, args[1]);
						return;
					}

					AllyWithdrawInvite(player, targetClan.ClanTag);
					break;
				}

				case "allyaccept":
				{
					if (!_config.AllianceSettings.Enabled) return;

					if (args.Length < 2)
					{
						SendReply(player, $"Error syntax! Use: /{command} {args[0]} <clanTag>");
						return;
					}

					var clan = FindClanByPlayer(player.UserIDString);
					if (clan == null)
					{
						Reply(player, NotClanMember);
						return;
					}

					if (!clan.IsModerator(player.userID))
					{
						Reply(player, NotModer);
						return;
					}

					var targetClan = FindClanByTag(args[1]);
					if (targetClan == null)
					{
						Reply(player, ClanNotFound, args[1]);
						return;
					}

					AllyAcceptInvite(player, targetClan.ClanTag);
					break;
				}

				case "allycancel":
				{
					if (!_config.AllianceSettings.Enabled) return;

					if (args.Length < 2)
					{
						SendReply(player, $"Error syntax! Use: /{command} {args[0]} <clanTag>");
						return;
					}

					var clan = FindClanByPlayer(player.UserIDString);
					if (clan == null)
					{
						Reply(player, NotClanMember);
						return;
					}

					if (!clan.IsModerator(player.userID))
					{
						Reply(player, NotModer);
						return;
					}

					var targetClan = FindClanByTag(args[1]);
					if (targetClan == null)
					{
						Reply(player, ClanNotFound, args[1]);
						return;
					}

					AllyCancelInvite(player, targetClan.ClanTag);
					break;
				}

				case "allyrevoke":
				{
					if (!_config.AllianceSettings.Enabled) return;

					if (args.Length < 2)
					{
						SendReply(player, $"Error syntax! Use: /{command} {args[0]} <clanTag>");
						return;
					}

					var clan = FindClanByPlayer(player.UserIDString);
					if (clan == null)
					{
						Reply(player, NotClanMember);
						return;
					}

					if (!clan.IsModerator(player.userID))
					{
						Reply(player, NotModer);
						return;
					}

					var targetClan = FindClanByTag(args[1]);
					if (targetClan == null)
					{
						Reply(player, ClanNotFound, args[1]);
						return;
					}

					AllyRevoke(player, targetClan.ClanTag);
					break;
				}

				case "description":
				{
					if (args.Length < 2)
					{
						SendReply(player, $"Error syntax! Use: /{command} {args[0]} <description>");
						return;
					}

					var description = string.Join(" ", args.Skip(1));
					if (string.IsNullOrEmpty(description)) return;

					if (description.Length > _config.DescriptionMax)
					{
						Reply(player, MaxDescriptionSize);
						return;
					}

					var clan = FindClanByPlayer(player.UserIDString);
					if (clan == null)
					{
						Reply(player, NotClanMember);
						return;
					}

					if (!clan.IsOwner(player.userID))
					{
						Reply(player, NotClanLeader);
						return;
					}

					clan.Description = description;
					Reply(player, SetDescription);
					break;
				}

				case "join":
				{
					if (FindClanByPlayer(player.UserIDString) != null)
					{
						Reply(player, AlreadyClanMember);
						return;
					}

					MainUi(player, 45, first: true);
					break;
				}

				case "tagcolor":
				{
					if (!_config.Tags.TagColor.Enabled) return;

					if (args.Length < 2)
					{
						SendReply(player, $"Error syntax! Use: /{command} {args[0]} <tag color>");
						return;
					}

					var hexColor = string.Join(" ", args.Skip(1));
					if (string.IsNullOrEmpty(hexColor)) return;

					hexColor = hexColor.Replace("#", "");

					if (hexColor.Length < 6 || hexColor.Length > 6 || !_hexFilter.IsMatch(hexColor))
					{
						Reply(player, TagColorFormat);
						return;
					}

					var clan = FindClanByPlayer(player.UserIDString);
					if (clan == null) return;

					if (!clan.CanEditTagColor(player.userID))
					{
						Reply(player, NoPermissions);
						return;
					}

					var oldTagColor = clan.GetHexTagColor();
					if (!string.IsNullOrEmpty(oldTagColor) && oldTagColor.Equals(hexColor))
						return;

					clan.TagColor = hexColor;

					Reply(player, TagColorInstalled, hexColor);
					break;
				}

				default:
				{
					var msg = Msg(player, Help);

					var clan = FindClanByPlayer(player.UserIDString);
					if (clan != null)
					{
						if (clan.IsModerator(player.userID))
							msg += Msg(player, ModerHelp);

						if (clan.IsOwner(player.userID))
							msg += Msg(player, AdminHelp);
					}

					SendReply(player, msg);
					break;
				}
			}
		}

		private void CmdAdminClans(IPlayer cov, string command, string[] args)
		{
			if (!(cov.IsServer || cov.HasPermission(PermAdmin))) return;

			if (args.Length == 0)
			{
				var sb = new StringBuilder();
				sb.AppendLine("Clans management help:");
				sb.AppendLine($"{command} list - lists all clans, their owners and their member-count");
				sb.AppendLine($"{command} listex - lists all clans, their owners/members and their on-line status");
				sb.AppendLine(
					$"{command} show [name/userId] - lists the chosen clan (or clan by user) and the members with status");
				sb.AppendLine($"{command} msg [clanTag] [message] - sends a clan message");

				sb.AppendLine($"{command} create [name/userId] [clanTag] - creates a clan");
				sb.AppendLine($"{command} rename [oldTag] [newTag] - renames a clan");
				sb.AppendLine($"{command} disband [clanTag] - disbands a clan");

				sb.AppendLine($"{command} invite [clanTag] [name/userId] - sends clan invitation to a player");
				sb.AppendLine($"{command} join [clanTag] [name/userId] - joins a player into a clan");
				sb.AppendLine($"{command} kick [clanTag] [name/userId] - kicks a member from a clan");
				sb.AppendLine($"{command} owner [clanTag] [name/userId] - sets a new owner");
				sb.AppendLine($"{command} promote [clanTag] [name/userId] - promotes a member");
				sb.AppendLine($"{command} demote [clanTag] [name/userId] - demotes a member");

				cov.Reply(sb.ToString());
				return;
			}

			switch (args[0].ToLower())
			{
				case "list":
				{
					var textTable = new TextTable();
					textTable.AddColumn("Tag");
					textTable.AddColumn("Owner");
					textTable.AddColumn("SteamID");
					textTable.AddColumn("Count");
					textTable.AddColumn("On");

					_clansList.ForEach(clan =>
					{
						if (clan == null) return;

						textTable.AddRow(clan.ClanTag ?? "UNKNOWN", clan.LeaderName ?? "UNKNOWN",
							clan.LeaderID.ToString(),
							clan.Members?.Count.ToString() ?? "UNKNOWN",
							clan.Players?.Count().ToString() ?? "UNKNOWN");
					});

					cov.Reply("\n>> Current clans <<\n" + textTable);
					break;
				}

				case "listex":
				{
					var textTable = new TextTable();
					textTable.AddColumn("Tag");
					textTable.AddColumn("Role");
					textTable.AddColumn("Name");
					textTable.AddColumn("SteamID");
					textTable.AddColumn("Status");

					_clansList.ForEach(clan =>
					{
						clan.Members.ForEach(member =>
						{
							var role = clan.IsOwner(member) ? "leader" :
								clan.IsModerator(member) ? "moderator" : "member";

							textTable.AddRow(clan.ClanTag ?? "UNKNOWN", role,
								PlayerData.GetOrLoad(member.ToString())?.DisplayName ?? "UNKNOWN", member.ToString(),
								BasePlayer.FindByID(member) != null ? "Online" : "Offline");
						});

						textTable.AddRow();
					});

					cov.Reply("\n>> Current clans with members <<\n" + textTable);
					break;
				}

				case "show":
				{
					if (args.Length < 2)
					{
						cov.Reply($"Error syntax! Use: /{command} {args[0]} [name/userId]");
						return;
					}

					var clan = FindClanByTag(args[1]);
					if (clan == null)
					{
						var player = BasePlayer.FindAwakeOrSleeping(args[1]);
						if (player != null) clan = FindClanByPlayer(player.UserIDString);
					}

					if (clan == null)
					{
						cov.Reply($"Clan/Member's clan ({args[1]}) not found!");
						return;
					}

					var sb = new StringBuilder();
					sb.AppendLine($"\n>> Show clan [{clan.ClanTag}] <<");
					sb.AppendLine($"Description: {clan.Description}");
					sb.AppendLine($"Time created: {clan.CreationTime}");
					sb.AppendLine($"Last online: {clan.LastOnlineTime}");
					sb.AppendLine($"Member count: {clan.Members.Count}");

					var textTable = new TextTable();
					textTable.AddColumn("Role");
					textTable.AddColumn("Name");
					textTable.AddColumn("SteamID");
					textTable.AddColumn("Status");
					sb.AppendLine();

					clan.Members.ForEach(member =>
					{
						var role = clan.IsOwner(member) ? "leader" :
							clan.IsModerator(member) ? "moderator" : "member";

						textTable.AddRow(role, PlayerData.GetOrLoad(member.ToString())?.DisplayName ?? "UNKNOWN",
							member.ToString(),
							BasePlayer.FindByID(member) != null ? "Online" : "Offline");
					});

					sb.AppendLine(textTable.ToString());

					cov.Reply(sb.ToString());
					cov.Reply($"Allied Clans: {clan.Alliances.ToSentence()}");
					break;
				}

				case "msg":
				{
					if (args.Length < 3)
					{
						cov.Reply($"Error syntax! Use: /{command} {args[0]} [clanTag] [message]");
						return;
					}

					var clan = FindClanByTag(args[1]);
					if (clan == null)
					{
						cov.Reply($"Clan/Member's clan ({args[1]}) not found!");
						return;
					}

					var message = string.Join(" ", args.Skip(2));
					if (string.IsNullOrEmpty(message)) return;

					clan.Broadcast(AdminBroadcast, message);
					break;
				}

				case "create":
				{
					if (args.Length < 3)
					{
						cov.Reply($"Error syntax! Use: /{command} {args[0]} [name/userId] [clanTag]");
						return;
					}

					var player = BasePlayer.FindAwakeOrSleeping(args[1]);
					if (player == null)
					{
						cov.Reply($"Player '{args[1]}' not found!");
						return;
					}

					var data = PlayerData.GetOrCreate(player.UserIDString);
					if (data == null) return;

					var clanTag = string.Join(" ", args.Skip(2));
					if (string.IsNullOrEmpty(clanTag) || clanTag.Length < _config.Tags.TagMin ||
					    clanTag.Length > _config.Tags.TagMax)
					{
						Reply(cov, ClanTagLimit, _config.Tags.TagMin, _config.Tags.TagMax);
						return;
					}

					var checkTag = clanTag.Replace(" ", "");
					if (_config.Tags.BlockedWords.Exists(word =>
						    checkTag.Contains(word, CompareOptions.OrdinalIgnoreCase)))
					{
						Reply(cov, ContainsForbiddenWords);
						return;
					}

					if (_config.Tags.CheckingCharacters && _tagFilter.IsMatch(checkTag))
					{
						Reply(player, ContainsForbiddenWords);
						return;
					}

					var clan = FindClanByTag(clanTag);
					if (clan != null)
					{
						Reply(cov, ClanExists);
						return;
					}

					if (FindClanByPlayer(player.UserIDString) != null)
					{
						cov.Reply("The player is already in a clan");
						return;
					}

					clan = ClanData.CreateNewClan(clanTag, player);
					if (clan == null) return;

					ClanCreating.Remove(player);
					Reply(player, ClanCreated, clanTag);

					cov.Reply($"You created the clan {clanTag} and set {player.displayName} as the owner");
					break;
				}

				case "rename":
				{
					if (args.Length < 3)
					{
						cov.Reply($"Error syntax! Use: /{command} {args[0]} [oldTag] [newTag]");
						return;
					}

					var clan = FindClanByTag(args[1]);
					if (clan == null)
					{
						cov.Reply($"Clan/Member's clan ({args[1]}) not found!");
						return;
					}

					var oldTag = clan.ClanTag;

					var clanTag = args[2];
					if (string.IsNullOrEmpty(clanTag) || clanTag.Length < _config.Tags.TagMin ||
					    clanTag.Length > _config.Tags.TagMax)
					{
						Reply(cov, ClanTagLimit, _config.Tags.TagMin, _config.Tags.TagMax);
						return;
					}

					if (_config.Tags.CheckingCharacters && _tagFilter.IsMatch(clanTag))
					{
						Reply(cov, ContainsForbiddenWords);
						return;
					}

					if (FindClanByTag(clanTag) != null)
					{
						cov.Reply("Clan with that tag already exists!");
						return;
					}

					clan.Rename(clanTag);
					clan.Broadcast(AdminRename, clanTag);

					cov.Reply($"You have changed {oldTag} tag to {clanTag}");
					break;
				}

				case "join":
				{
					if (args.Length < 3)
					{
						cov.Reply($"Error syntax! Use: /{command} {args[0]} [clanTag] [name/userId]");
						return;
					}

					var clan = FindClanByTag(args[1]);
					if (clan == null)
					{
						cov.Reply($"Clan '{args[1]}' not found!");
						return;
					}

					var player = BasePlayer.FindAwakeOrSleeping(args[2]);
					if (player == null)
					{
						cov.Reply($"Player '{args[2]}' not found!");
						return;
					}

					var data = PlayerData.GetOrCreate(player.UserIDString);
					if (data == null) return;

					if (data.GetClan() != null)
					{
						cov.Reply("The player is already in a clan");
						return;
					}

					var inviteData = _invites.GetClanInvite(player.userID, clan.ClanTag);
					if (inviteData == null)
					{
						cov.Reply("The player does not have a invite to that clan");
						return;
					}

					if (clan.Members.Count >= _config.LimitSettings.MemberLimit)
					{
						cov.Reply("The clan is already at capacity");
						return;
					}

					clan.Join(player);
					Reply(player, AdminJoin, clan.ClanTag);
					break;
				}

				case "kick":
				{
					if (args.Length < 3)
					{
						cov.Reply($"Error syntax! Use: /{command} {args[0]} [clanTag] [name/userId]");
						return;
					}

					var clan = FindClanByTag(args[1]);
					if (clan == null)
					{
						cov.Reply($"Clan '{args[1]}' not found!");
						return;
					}

					var player = BasePlayer.FindAwakeOrSleeping(args[2]);
					if (player == null)
					{
						cov.Reply($"Player '{args[2]}' not found!");
						return;
					}

					var data = PlayerData.GetOrCreate(player.UserIDString);
					if (data == null) return;

					if (!clan.IsMember(player.userID))
					{
						cov.Reply("The player is not in that clan");
						return;
					}

					clan.Kick(player.userID);

					Reply(player, AdminKick, clan.ClanTag);
					clan.Broadcast(AdminKickBroadcast, player.displayName);
					break;
				}

				case "owner":
				{
					if (args.Length < 3)
					{
						cov.Reply($"Error syntax! Use: /{command} {args[0]} [clanTag] [name/userId]");
						return;
					}

					var clan = FindClanByTag(args[1]);
					if (clan == null)
					{
						cov.Reply($"Clan '{args[1]}' not found!");
						return;
					}

					var player = BasePlayer.FindAwakeOrSleeping(args[2]);
					if (player == null)
					{
						cov.Reply($"Player '{args[2]}' not found!");
						return;
					}

					var data = PlayerData.GetOrCreate(player.UserIDString);
					if (data == null) return;

					if (!clan.IsMember(player.userID))
					{
						cov.Reply("The player is not a member of that clan");
						return;
					}

					if (clan.IsOwner(player.userID))
					{
						cov.Reply("The player is already the clan owner");
						return;
					}

					clan.SetLeader(player.userID);

					clan.Broadcast(AdminSetLeader, player.userID);
					break;
				}

				case "invite":
				{
					if (args.Length < 3)
					{
						cov.Reply($"Error syntax! Use: /{command} {args[0]} [clanTag] [name/userId]");
						return;
					}

					var clan = FindClanByTag(args[1]);
					if (clan == null)
					{
						cov.Reply($"Clan '{args[1]}' not found!");
						return;
					}

					var player = BasePlayer.FindAwakeOrSleeping(args[2]);
					if (player == null)
					{
						cov.Reply($"Player '{args[2]}' not found!");
						return;
					}

					var data = PlayerData.GetOrCreate(player.UserIDString);
					if (data == null) return;

					if (data.GetClan() != null)
					{
						cov.Reply("The player is already a member of the clan.");
						return;
					}

					if (clan.IsMember(player.userID))
					{
						cov.Reply("The player is already a member of the clan.");
						return;
					}

					var inviteData = _invites.GetClanInvite(player.userID, clan.ClanTag);
					if (inviteData != null)
					{
						cov.Reply("The player already has a invitation to join that clan");
						return;
					}

					if (clan.Members.Count >= _config.LimitSettings.MemberLimit)
					{
						cov.Reply("The clan is already at capacity");
						return;
					}

					_invites.AddPlayerInvite(player.userID, 0, "ADMIN", clan.ClanTag);

					Reply(player, SuccessInvitedSelf, "ADMIN", clan.ClanTag);

					clan.Broadcast(AdminInvite, player.displayName);
					break;
				}

				case "promote":
				{
					if (args.Length < 3)
					{
						cov.Reply($"Error syntax! Use: /{command} {args[0]} [clanTag] [name/userId]");
						return;
					}

					var clan = FindClanByTag(args[1]);
					if (clan == null)
					{
						cov.Reply($"Clan '{args[1]}' not found!");
						return;
					}

					var player = BasePlayer.FindAwakeOrSleeping(args[2]);
					if (player == null)
					{
						cov.Reply($"Player '{args[2]}' not found!");
						return;
					}

					if (clan.IsOwner(player.userID))
					{
						cov.Reply("You can not demote the clan owner");
						return;
					}

					if (clan.IsMember(player.userID))
					{
						cov.Reply("The player is already at the lowest rank");
						return;
					}

					clan.SetModer(player.userID);

					clan.Broadcast(AdminPromote, player.displayName);
					break;
				}

				case "demote":
				{
					if (args.Length < 3)
					{
						cov.Reply($"Error syntax! Use: /{command} {args[0]} [clanTag] [name/userId]");
						return;
					}

					var clan = FindClanByTag(args[1]);
					if (clan == null)
					{
						cov.Reply($"Clan '{args[1]}' not found!");
						return;
					}

					var player = BasePlayer.FindAwakeOrSleeping(args[2]);
					if (player == null)
					{
						cov.Reply($"Player '{args[2]}' not found!");
						return;
					}

					if (clan.IsOwner(player.userID))
					{
						cov.Reply("You can not demote the clan owner");
						return;
					}

					if (clan.IsMember(player.userID))
					{
						cov.Reply("The player is already at the lowest rank");
						return;
					}

					clan.UndoModer(player.userID);

					clan.Broadcast(AdminDemote, player.displayName);
					break;
				}

				case "disband":
				{
					if (args.Length < 2)
					{
						cov.Reply($"Error syntax! Use: /{command} {args[0]} [clanTag]");
						return;
					}

					var clan = FindClanByTag(args[1]);
					if (clan == null)
					{
						cov.Reply($"Clan '{args[1]}' not found!");
						return;
					}

					clan.Broadcast(AdminDisbandClan);
					clan.Disband();

					cov.Reply("You have successfully disbanded the clan");
					break;
				}
			}
		}

		private void CmdClanInfo(IPlayer cov, string command, string[] args)
		{
			var player = cov?.Object as BasePlayer;
			if (player == null) return;

			if (player.net.connection.authLevel < _config.PermissionSettings.ClanInfoAuthLevel)
			{
				Reply(player, NoPermissions);
				return;
			}

			if (args.Length < 1)
			{
				SendReply(player, $"Error syntax! Use: /{command} <clan tag>");
				return;
			}

			var targetClan = FindClanByTag(args[0]);
			if (targetClan == null)
			{
				Reply(player, ClanNotFound, args[0]);
				return;
			}

			SendReply(player, targetClan.GetClanInfo(player));
		}

		[ConsoleCommand("UI_Clans")]
		private void CmdConsoleClans(ConsoleSystem.Arg arg)
		{
			var player = arg?.Player();
			if (player == null || !arg.HasArgs()) return;

			switch (arg.Args[0])
			{
				case "close_ui":
				{
					ClanCreating.Remove(player);
					break;
				}

				case "page":
				{
					int page;
					if (!arg.HasArgs(2) || !int.TryParse(arg.Args[1], out page)) return;

					var zPage = 0;
					if (arg.HasArgs(3))
						int.TryParse(arg.Args[2], out zPage);

					var search = string.Empty;
					if (arg.HasArgs(4))
					{
						search = string.Join(" ", arg.Args.Skip(3));

						if (string.IsNullOrEmpty(search) || search.Equals(Msg(player, EnterLink)))
							return;
					}

					MainUi(player, page, zPage, search);
					break;
				}

				case "inputpage":
				{
					int pages, page, zPage;
					if (!arg.HasArgs(4) || !int.TryParse(arg.Args[1], out pages) ||
					    !int.TryParse(arg.Args[2], out page) || !int.TryParse(arg.Args[3], out zPage)) return;

					if (zPage < 0)
						zPage = 0;

					if (zPage >= pages)
						zPage = pages - 1;

					MainUi(player, page, zPage);
					break;
				}

				case "changeavatar":
				{
					if (!arg.HasArgs(2)) return;

					var url = string.Join(" ", arg.Args.Skip(1));
					if (string.IsNullOrEmpty(url)) return;

					if (!IsValidURL(url))
						return;

					var clan = FindClanByPlayer(player.UserIDString);
					if (clan == null || !clan.IsOwner(player.userID)) return;

					clan.Avatar = url;
					ImageLibrary.Call("AddImage", url, url);

					MainUi(player);
					break;
				}

				case "invite":
				{
					if (!arg.HasArgs(2)) return;

					switch (arg.Args[1])
					{
						case "accept":
						{
							if (!arg.HasArgs(3)) return;

							var tag = string.Join(" ", arg.Args.Skip(2));
							if (string.IsNullOrEmpty(tag)) return;

							AcceptInvite(player, tag);
							break;
						}
						case "cancel":
						{
							if (!arg.HasArgs(3)) return;

							var tag = string.Join(" ", arg.Args.Skip(2));
							if (string.IsNullOrEmpty(tag)) return;

							CancelInvite(player, tag);
							break;
						}
						case "send":
						{
							ulong targetId;
							if (!arg.HasArgs(3) || !ulong.TryParse(arg.Args[2], out targetId)) return;

							SendInvite(player, targetId);

							MainUi(player, 5);
							break;
						}

						case "withdraw":
						{
							ulong targetId;
							if (!arg.HasArgs(3) || !ulong.TryParse(arg.Args[2], out targetId)) return;

							WithdrawInvite(player, targetId);

							MainUi(player, 65);
							break;
						}
					}

					break;
				}

				case "allyinvite":
				{
					if (!arg.HasArgs(2)) return;

					switch (arg.Args[1])
					{
						case "accept":
						{
							if (!arg.HasArgs(3)) return;

							AllyAcceptInvite(player, arg.Args[2]);
							break;
						}

						case "cancel":
						{
							if (!arg.HasArgs(3)) return;

							AllyCancelInvite(player, arg.Args[2]);
							break;
						}

						case "send":
						{
							if (!arg.HasArgs(3)) return;

							AllySendInvite(player, arg.Args[2]);

							MainUi(player, 71);
							break;
						}

						case "withdraw":
						{
							if (!arg.HasArgs(3)) return;

							AllyWithdrawInvite(player, arg.Args[2]);

							MainUi(player, 71);
							break;
						}

						case "revoke":
						{
							if (!arg.HasArgs(3)) return;

							AllyRevoke(player, arg.Args[2]);

							MainUi(player, 7);
							break;
						}
					}

					break;
				}

				case "createclan":
				{
					if (arg.HasArgs(2))
						switch (arg.Args[1])
						{
							case "name":
							{
								if (!arg.HasArgs(3)) return;

								var tag = string.Join(" ", arg.Args.Skip(2));
								if (string.IsNullOrEmpty(tag) || tag.Length < _config.Tags.TagMin ||
								    tag.Length > _config.Tags.TagMax)
								{
									Reply(player, ClanTagLimit, _config.Tags.TagMin, _config.Tags.TagMax);
									return;
								}

								CreateClanData creatingData;
								if (ClanCreating.TryGetValue(player, out creatingData))
								{
									var oldTag = creatingData.Tag;
									if (!string.IsNullOrEmpty(oldTag) && oldTag.Equals(tag))
										return;
								}

								ClanCreating[player].Tag = tag;
								break;
							}
							case "avatar":
							{
								if (!arg.HasArgs(3)) return;

								var avatar = string.Join(" ", arg.Args.Skip(2));
								if (string.IsNullOrEmpty(avatar)) return;

								CreateClanData creatingData;
								if (ClanCreating.TryGetValue(player, out creatingData))
								{
									var oldAvatar = creatingData.Avatar;
									if (!string.IsNullOrEmpty(oldAvatar))
										if (oldAvatar.Equals(Msg(player, UrlTitle)) ||
										    oldAvatar.Equals(avatar))
											return;
								}

								if (!IsValidURL(avatar))
									return;

								ClanCreating[player].Avatar = avatar;
								break;
							}
							case "create":
							{
								if (!ClanCreating.ContainsKey(player)) return;

								var clanTag = ClanCreating[player].Tag;
								if (string.IsNullOrEmpty(clanTag))
								{
									if (_config.ForceClanCreateTeam)
										CreateClanUi(player);
									return;
								}

								if (_config.PermissionSettings.UseClanCreating &&
								    !string.IsNullOrEmpty(_config.PermissionSettings.ClanCreating) &&
								    !player.HasPermission(_config.PermissionSettings.ClanCreating))
								{
									Reply(player, NoPermCreateClan);

									if (_config.ForceClanCreateTeam)
										CreateClanUi(player);
									return;
								}

								var clan = FindClanByTag(clanTag);
								if (clan != null)
								{
									Reply(player, ClanExists);

									if (_config.ForceClanCreateTeam)
										CreateClanUi(player);
									return;
								}

								var checkTag = clanTag.Replace(" ", "");
								if (_config.Tags.BlockedWords.Exists(word =>
									    checkTag.Contains(word, CompareOptions.OrdinalIgnoreCase)))
								{
									Reply(player, ContainsForbiddenWords);

									if (_config.ForceClanCreateTeam)
										CreateClanUi(player);
									return;
								}

								if (_config.Tags.CheckingCharacters && _tagFilter.IsMatch(checkTag))
								{
									Reply(player, ContainsForbiddenWords);
									return;
								}

								clan = ClanData.CreateNewClan(clanTag, player);
								if (clan == null)
								{
									if (_config.ForceClanCreateTeam)
										CreateClanUi(player);
									return;
								}

								var avatar = ClanCreating[player].Avatar;
								if (!string.IsNullOrEmpty(avatar) &&
								    !avatar.Equals(Msg(player, UrlTitle)) &&
								    IsValidURL(avatar))
									ImageLibrary.Call("AddImage", avatar,
										avatar);

								ClanCreating.Remove(player);
								Reply(player, ClanCreated, clanTag);
								return;
							}
						}

					CreateClanUi(player);
					break;
				}

				case "edititem":
				{
					int slot;
					if (!arg.HasArgs(2) || !int.TryParse(arg.Args[1], out slot)) return;

					SelectItemUi(player, slot);
					break;
				}

				case "selectpages":
				{
					int slot, page, amount;
					if (!arg.HasArgs(4) || !int.TryParse(arg.Args[1], out slot) ||
					    !int.TryParse(arg.Args[2], out page) || !int.TryParse(arg.Args[3], out amount)) return;

					var search = string.Empty;
					if (arg.HasArgs(5))
						search = string.Join(" ", arg.Args.Skip(4));

					SelectItemUi(player, slot, page, amount, search);
					break;
				}

				case "setamountitem":
				{
					int slot, amount;
					if (!arg.HasArgs(3) || !int.TryParse(arg.Args[1], out slot) ||
					    !int.TryParse(arg.Args[2], out amount) || amount <= 0) return;

					var clan = FindClanByPlayer(player.UserIDString);
					if (clan == null || !clan.IsOwner(player.userID))
						return;

					if (clan.ResourceStandarts.ContainsKey(slot))
						clan.ResourceStandarts[slot].Amount = amount;

					SelectItemUi(player, slot, amount: amount);
					break;
				}

				case "selectitem":
				{
					int slot, amount;
					if (!arg.HasArgs(4) || !int.TryParse(arg.Args[1], out slot) ||
					    !int.TryParse(arg.Args[3], out amount)) return;

					var shortName = arg.Args[2];
					if (string.IsNullOrEmpty(shortName)) return;

					var clan = FindClanByPlayer(player.UserIDString);
					if (clan == null || !clan.IsOwner(player.userID)) return;

					if (clan.ResourceStandarts.ContainsKey(slot))
					{
						clan.ResourceStandarts[slot].ShortName = shortName;
						clan.ResourceStandarts[slot].Amount = amount;
					}
					else
					{
						clan.ResourceStandarts.Add(slot, new ResourceStandart
						{
							Amount = amount,
							ShortName = shortName
						});
					}

					MainUi(player, 4);
					break;
				}

				case "editskin":
				{
					if (!arg.HasArgs(2)) return;

					var page = 0;
					if (arg.HasArgs(3))
						int.TryParse(arg.Args[2], out page);

					SelectSkinUi(player, arg.Args[1], page);
					break;
				}

				case "setskin":
				{
					ulong skin;
					if (!arg.HasArgs(3) ||
					    !ulong.TryParse(arg.Args[2], out skin)) return;

					var clan = FindClanByPlayer(player.UserIDString);
					if (clan == null) return;

					clan.SetSkin(arg.Args[1], skin);

					SelectSkinUi(player, arg.Args[1]);
					break;
				}

				case "selectskin":
				{
					ulong skin;
					if (!arg.HasArgs(3) ||
					    !ulong.TryParse(arg.Args[2], out skin)) return;

					var clan = FindClanByPlayer(player.UserIDString);
					if (clan == null) return;

					clan.SetSkin(arg.Args[1], skin);

					MainUi(player, 6);
					break;
				}

				case "showprofile":
				{
					ulong target;
					if (!arg.HasArgs(2) || !ulong.TryParse(arg.Args[1], out target)) return;

					ProfileUi(player, target);
					break;
				}

				case "showclanprofile":
				{
					ulong target;
					if (!arg.HasArgs(2) || !ulong.TryParse(arg.Args[1], out target)) return;

					ClanMemberProfileUi(player, target);
					break;
				}

				case "moder":
				{
					if (!arg.HasArgs(2)) return;

					var clan = FindClanByPlayer(player.UserIDString);
					if (clan == null) return;

					switch (arg.Args[1])
					{
						case "set":
						{
							ulong target;
							if (!arg.HasArgs(3) || !ulong.TryParse(arg.Args[2], out target)) return;

							if (clan.Moderators.Count >= _config.LimitSettings.ModeratorLimit)
							{
								CuiHelper.DestroyUi(player, Layer);

								Reply(player, ALotOfModers);
								return;
							}

							clan.SetModer(target);

							ClanMemberProfileUi(player, target);
							break;
						}

						case "undo":
						{
							ulong target;
							if (!arg.HasArgs(3) || !ulong.TryParse(arg.Args[2], out target)) return;

							clan.UndoModer(target);

							ClanMemberProfileUi(player, target);
							break;
						}
					}

					break;
				}

				case "leader":
				{
					if (!arg.HasArgs(2)) return;

					var clan = FindClanByPlayer(player.UserIDString);
					if (clan == null) return;

					switch (arg.Args[1])
					{
						case "tryset":
						{
							ulong target;
							if (!arg.HasArgs(3) || !ulong.TryParse(arg.Args[2], out target)) return;

							AcceptSetLeader(player, target);
							break;
						}

						case "set":
						{
							ulong target;
							if (!arg.HasArgs(3) || !ulong.TryParse(arg.Args[2], out target)) return;

							clan.SetLeader(target);

							ClanMemberProfileUi(player, target);
							break;
						}
					}

					break;
				}

				case "kick":
				{
					ulong target;
					if (!arg.HasArgs(2) || !ulong.TryParse(arg.Args[1], out target)) return;

					if (_config.PermissionSettings.UseClanKick &&
					    !string.IsNullOrEmpty(_config.PermissionSettings.ClanKick) &&
					    !player.HasPermission(_config.PermissionSettings.ClanKick))
					{
						Reply(player, _config.PermissionSettings.ClanKick);
						return;
					}

					var clan = FindClanByPlayer(player.UserIDString);
					if (clan == null) return;

					clan.Kick(target);

					MainUi(player, 1);
					break;
				}

				case "showclan":
				{
					if (!arg.HasArgs(2)) return;

					var tag = arg.Args[1];
					if (string.IsNullOrEmpty(tag)) return;

					ClanProfileUi(player, tag);
					break;
				}

				case "ff":
				{
					var data = PlayerData.GetOrCreate(player.UserIDString);
					if (data == null) return;

					if (_config.GeneralFriendlyFire)
					{
						var clan = data.GetClan();
						if (clan == null) return;

						if (_config.PlayersGeneralFF || (_config.ModersGeneralFF && clan.IsModerator(player.userID)) ||
						    clan.IsOwner(player.userID))
							clan.FriendlyFire = !clan.FriendlyFire;
					}
					else
					{
						data.FriendlyFire = !data.FriendlyFire;
					}

					var container = new CuiElementContainer();
					ButtonFriendlyFire(ref container, player, data);
					CuiHelper.AddUi(player, container);
					break;
				}

				case "allyff":
				{
					var data = PlayerData.GetOrCreate(player.UserIDString);
					if (data == null) return;

					if (_config.AllianceSettings.GeneralFriendlyFire)
					{
						var clan = data.GetClan();
						if (clan == null) return;

						if (_config.AllianceSettings.PlayersGeneralFF || (_config.AllianceSettings.ModersGeneralFF &&
						                                                  clan.IsModerator(player.userID)) ||
						    clan.IsOwner(player.userID))
							clan.AllyFriendlyFire = !clan.AllyFriendlyFire;
					}
					else
					{
						data.AllyFriendlyFire = !data.AllyFriendlyFire;
					}

					var container = new CuiElementContainer();
					ButtonAlly(ref container, player, data);
					CuiHelper.AddUi(player, container);
					break;
				}

				case "description":
				{
					if (!arg.HasArgs(2)) return;

					var description = string.Join(" ", arg.Args.Skip(1));
					if (string.IsNullOrEmpty(description)) return;

					if (description.Equals(Msg(player, NotDescription)))
						return;

					if (description.Length > _config.DescriptionMax)
					{
						Reply(player, MaxDescriptionSize);
						return;
					}

					var clan = FindClanByPlayer(player.UserIDString);
					if (clan == null)
					{
						Reply(player, NotClanMember);
						return;
					}

					if (!clan.IsOwner(player.userID))
					{
						Reply(player, NotClanLeader);
						return;
					}

					if (!string.IsNullOrEmpty(clan.Description) && clan.Description.Equals(description))
						return;

					clan.Description = description;

					MainUi(player);

					Reply(player, SetDescription);
					break;
				}

				case "clanskins":
				{
					var data = PlayerData.GetOrCreate(player.UserIDString);
					if (data == null) return;

					if (_config.PermissionSettings.UseClanSkins &&
					    !string.IsNullOrEmpty(_config.PermissionSettings.ClanSkins) &&
					    !player.HasPermission(_config.PermissionSettings.ClanSkins))
					{
						Reply(player, NoPermClanSkins);
						return;
					}

					data.ClanSkins = !data.ClanSkins;

					var container = new CuiElementContainer();
					ButtonClanSkins(ref container, player, data);
					CuiHelper.AddUi(player, container);
					break;
				}

				case "settagcolor":
				{
					if (!arg.HasArgs(2)) return;

					var hexColor = arg.Args[1];
					if (string.IsNullOrEmpty(hexColor)) return;

					hexColor = hexColor.Replace("#", "");

					if (hexColor.Length < 6 || hexColor.Length > 6 || !_hexFilter.IsMatch(hexColor))
					{
						Reply(player, TagColorFormat);
						return;
					}

					var clan = FindClanByPlayer(player.UserIDString);
					if (clan == null || !clan.CanEditTagColor(player.userID)) return;

					var oldTagColor = clan.GetHexTagColor();
					if (!string.IsNullOrEmpty(oldTagColor) && oldTagColor.Equals(hexColor))
						return;

					clan.TagColor = hexColor;

					MainUi(player);
					break;
				}
			}

#if TESTING
				Puts($"Main command used with: {string.Join(", ", arg.Args)}");
#endif
		}

		[ConsoleCommand("clans.loadavatars")]
		private void CmdConsoleLoadAvatars(ConsoleSystem.Arg arg)
		{
			if (!arg.IsAdmin) return;

			StartLoadingAvatars();
		}

		[ConsoleCommand("clans.refreshtop")]
		private void CmdRefreshTop(ConsoleSystem.Arg arg)
		{
			if (!arg.IsAdmin) return;

			HandleTop();
		}

		[ConsoleCommand("clans.refreshskins")]
		private void CmdConsoleRefreshSkins(ConsoleSystem.Arg arg)
		{
			if (!arg.IsAdmin) return;

			foreach (var itemSkin in _config.Skins.ItemSkins)
				itemSkin.Value.Clear();

			LoadSkins();

			Puts(
				$"{_config.Skins.ItemSkins.Sum(x => x.Value.Count)} skins for {_config.Skins.ItemSkins.Count} items uploaded successfully!");
		}

		[ConsoleCommand("clans.sendcmd")]
		private void SendCMD(ConsoleSystem.Arg args)
		{
			var player = args.Player();
			if (player == null || !args.HasArgs()) return;

			var convertcmd =
				$"{args.Args[0]}  \" {string.Join(" ", args.Args.ToList().GetRange(1, args.Args.Length - 1))}\" 0";
			player.SendConsoleCommand(convertcmd);
		}

		#endregion

		#region Interface

		private void MainUi(BasePlayer player, int page = 0, int zPage = 0, string search = "", bool first = false)
		{
			#region Fields

			float xSwitch;
			float ySwitch;
			float height;
			float width;
			float margin;
			int amountOnString;
			int strings;
			int totalAmount;

			var data = PlayerData.GetOrCreate(player.UserIDString);

			var clan = data.GetClan();

			#endregion

			var container = new CuiElementContainer();

			#region Background

			if (first)
			{
				CuiHelper.DestroyUi(player, Layer);

				container.Add(new CuiPanel
				{
					RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
					Image =
					{
						Color = "0 0 0 0.9",
						Material = "assets/content/ui/uibackgroundblur.mat"
					},
					CursorEnabled = true
				}, "Overlay", Layer);

				container.Add(new CuiButton
				{
					RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
					Text = {Text = ""},
					Button =
					{
						Color = "0 0 0 0",
						Close = Layer
					}
				}, Layer);
			}

			#endregion

			#region Main

			container.Add(new CuiPanel
			{
				RectTransform =
				{
					AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5",
					OffsetMin = "-340 -215",
					OffsetMax = "340 220"
				},
				Image =
				{
					Color = Color1
				}
			}, Layer, Layer + ".Main");

			#region Header

			HeaderUi(ref container, player, clan, page, Msg(player, ClansMenuTitle));

			#endregion

			#region Menu

			MenuUi(ref container, player, page, clan);

			#endregion

			#region Content

			container.Add(new CuiPanel
			{
				RectTransform =
				{
					AnchorMin = "0 0", AnchorMax = "1 1",
					OffsetMin = "195 0", OffsetMax = "0 -55"
				},
				Image =
				{
					Color = "0 0 0 0"
				}
			}, Layer + ".Main", Layer + ".Second.Main");

			container.Add(new CuiPanel
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
				Image = {Color = "0 0 0 0"}
			}, Layer + ".Second.Main", Layer + ".Content");

			// ReSharper disable PossibleNullReferenceException
			if (clan != null || page == 45 || page == 2 || page == 3)
				switch (page)
				{
					case 0:
					{
						#region Title

						container.Add(new CuiLabel
						{
							RectTransform =
							{
								AnchorMin = "0 1", AnchorMax = "0 1",
								OffsetMin = "2.5 -30", OffsetMax = "225 0"
							},
							Text =
							{
								Text = Msg(player, AboutClan),
								Align = TextAnchor.MiddleLeft,
								Font = "robotocondensed-regular.ttf",
								FontSize = 12,
								Color = "1 1 1 1"
							}
						}, Layer + ".Content");

						#endregion

						#region Avatar

						container.Add(new CuiElement
						{
							Parent = Layer + ".Content",
							Components =
							{
								new CuiRawImageComponent {Png = ImageLibrary.Call<string>("GetImage", clan.Avatar)},
								new CuiRectTransformComponent
								{
									AnchorMin = "0 1", AnchorMax = "0 1",
									OffsetMin = "0 -170", OffsetMax = "140 -30"
								}
							}
						});

						if (clan.IsOwner(player.userID))
						{
							#region Change avatar

							container.Add(new CuiButton
							{
								RectTransform =
								{
									AnchorMin = "0 1", AnchorMax = "0 1",
									OffsetMin = "0 -200", OffsetMax = "140 -175"
								},
								Text =
								{
									Text = Msg(player, ChangeAvatar),
									Align = TextAnchor.MiddleCenter,
									Font = "robotocondensed-regular.ttf",
									FontSize = 12,
									Color = "1 1 1 1"
								},
								Button =
								{
									Color = Color2,
									Command = $"UI_Clans changeavatar {search}"
								}
							}, Layer + ".Content");

							#endregion

							#region Input URL

							container.Add(new CuiPanel
							{
								RectTransform =
								{
									AnchorMin = "0 1", AnchorMax = "0 1",
									OffsetMin = "0 -230", OffsetMax = "140 -205"
								},
								Image =
								{
									Color = Color3
								}
							}, Layer + ".Content", Layer + ".Avatar.Input");

							container.Add(new CuiElement
							{
								Parent = Layer + ".Avatar.Input",
								Components =
								{
									new CuiInputFieldComponent
									{
										FontSize = 12,
										Align = TextAnchor.MiddleCenter,
										Font = "robotocondensed-regular.ttf",
										Command = $"UI_Clans page {page} 0 ",
										Color = "1 1 1 0.65",
										CharsLimit = 128,
										NeedsKeyboard = true,
										Text = string.IsNullOrEmpty(search) ? Msg(player, EnterLink) : $"{search}"
									},
									new CuiRectTransformComponent
									{
										AnchorMin = "0 0", AnchorMax = "1 1"
									}
								}
							});

							#endregion
						}

						#endregion

						#region Clan Name

						container.Add(new CuiLabel
						{
							RectTransform =
							{
								AnchorMin = "0 1", AnchorMax = "0 1",
								OffsetMin = "160 -50", OffsetMax = "400 -30"
							},
							Text =
							{
								Text = $"{clan.ClanTag}",
								Align = TextAnchor.MiddleLeft,
								Font = "robotocondensed-bold.ttf",
								FontSize = 16,
								Color = "1 1 1 1"
							}
						}, Layer + ".Content");

						#endregion

						#region Clan Leader

						container.Add(new CuiPanel
						{
							RectTransform =
							{
								AnchorMin = "0 1", AnchorMax = "0 1",
								OffsetMin = "160 -105",
								OffsetMax = $"{(_config.Tags.TagColor.Enabled ? 300 : 460)} -75"
							},
							Image =
							{
								Color = Color3
							}
						}, Layer + ".Content", Layer + ".Clan.Leader");

						container.Add(new CuiLabel
						{
							RectTransform =
							{
								AnchorMin = "0 1", AnchorMax = "1 1",
								OffsetMin = "0 0", OffsetMax = "0 20"
							},
							Text =
							{
								Text = Msg(player, LeaderTitle),
								Align = TextAnchor.MiddleLeft,
								Font = "robotocondensed-regular.ttf",
								FontSize = 10,
								Color = "1 1 1 1"
							}
						}, Layer + ".Clan.Leader");

						container.Add(new CuiLabel
						{
							RectTransform =
							{
								AnchorMin = "0 0", AnchorMax = "1 1",
								OffsetMin = "10 0", OffsetMax = "0 0"
							},
							Text =
							{
								Text = $"{clan.LeaderName}",
								Align = TextAnchor.MiddleLeft,
								Font = "robotocondensed-bold.ttf",
								FontSize = 12,
								Color = "1 1 1 1"
							}
						}, Layer + ".Clan.Leader");

						#endregion

						#region Clan Tag

						if (_config.Tags.TagColor.Enabled)
						{
							var tagColor = clan.GetHexTagColor();

							container.Add(new CuiPanel
							{
								RectTransform =
								{
									AnchorMin = "0 1", AnchorMax = "0 1",
									OffsetMin = "320 -105", OffsetMax = "460 -75"
								},
								Image =
								{
									Color = Color3
								}
							}, Layer + ".Content", Layer + ".Clan.ClanTag");

							container.Add(new CuiLabel
							{
								RectTransform =
								{
									AnchorMin = "0 1", AnchorMax = "1 1",
									OffsetMin = "0 0", OffsetMax = "0 20"
								},
								Text =
								{
									Text = Msg(player, TagColorTitle),
									Align = TextAnchor.MiddleLeft,
									Font = "robotocondensed-regular.ttf",
									FontSize = 10,
									Color = "1 1 1 1"
								}
							}, Layer + ".Clan.ClanTag");

							#region Line

							container.Add(new CuiPanel
							{
								RectTransform =
								{
									AnchorMin = "0 0",
									AnchorMax = "1 0",
									OffsetMin = "0 0",
									OffsetMax = "0 4"
								},
								Image =
								{
									Color = HexToCuiColor($"#{tagColor}")
								}
							}, Layer + ".Clan.ClanTag");

							#endregion

							if (clan.CanEditTagColor(player.userID))
								container.Add(new CuiElement
								{
									Parent = Layer + ".Clan.ClanTag",
									Components =
									{
										new CuiInputFieldComponent
										{
											Font = "robotocondensed-bold.ttf",
											FontSize = 12,
											Color = "1 1 1 1",
											Align = TextAnchor.MiddleCenter,
											Command = "UI_Clans settagcolor ",
											CharsLimit = 7,
											NeedsKeyboard = true,
											Text = $"#{tagColor}"
										},
										new CuiRectTransformComponent
										{
											AnchorMin = "0 0", AnchorMax = "1 1"
										}
									}
								});
							else
								container.Add(new CuiLabel
								{
									RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
									Text =
									{
										Text = $"#{tagColor}",
										Align = TextAnchor.MiddleCenter,
										Font = "robotocondensed-bold.ttf",
										FontSize = 12,
										Color = "1 1 1 1"
									}
								}, Layer + ".Clan.ClanTag");
						}

						#endregion

						#region Farm

						container.Add(new CuiPanel
						{
							RectTransform =
							{
								AnchorMin = "0 1", AnchorMax = "0 1",
								OffsetMin = "160 -165",
								OffsetMax = "460 -135"
							},
							Image =
							{
								Color = Color3
							}
						}, Layer + ".Content", Layer + ".Clan.Farm");

						container.Add(new CuiLabel
						{
							RectTransform =
							{
								AnchorMin = "0 1", AnchorMax = "1 1",
								OffsetMin = "0 0", OffsetMax = "0 20"
							},
							Text =
							{
								Text = Msg(player, GatherTitle),
								Align = TextAnchor.MiddleLeft,
								Font = "robotocondensed-regular.ttf",
								FontSize = 10,
								Color = "1 1 1 1"
							}
						}, Layer + ".Clan.Farm");

						var progress = clan.GetTotalFarm();

						if (progress > 0)
							container.Add(new CuiPanel
							{
								RectTransform = {AnchorMin = "0 0", AnchorMax = $"{progress} 0.95"},
								Image =
								{
									Color = Color2
								}
							}, Layer + ".Clan.Farm", Layer + ".Clan.Farm.Progress");

						container.Add(new CuiLabel
						{
							RectTransform =
							{
								AnchorMin = "0 0", AnchorMax = "1 1",
								OffsetMin = "0 0", OffsetMax = "-5 0"
							},
							Text =
							{
								Text = $"{(progress > 0 ? Math.Round(progress * 100f) : 0)}%",
								Align = TextAnchor.MiddleCenter,
								Font = "robotocondensed-bold.ttf",
								FontSize = 12,
								Color = "1 1 1 1"
							}
						}, Layer + ".Clan.Farm");

						#endregion

						#region Rating

						container.Add(new CuiPanel
						{
							RectTransform =
							{
								AnchorMin = "0 1", AnchorMax = "0 1",
								OffsetMin = "160 -225", OffsetMax = "300 -195"
							},
							Image =
							{
								Color = Color3
							}
						}, Layer + ".Content", Layer + ".Clan.Rating");

						container.Add(new CuiLabel
						{
							RectTransform =
							{
								AnchorMin = "0 1", AnchorMax = "1 1",
								OffsetMin = "0 0", OffsetMax = "0 20"
							},
							Text =
							{
								Text = Msg(player, RatingTitle),
								Align = TextAnchor.MiddleLeft,
								Font = "robotocondensed-regular.ttf",
								FontSize = 10,
								Color = "1 1 1 1"
							}
						}, Layer + ".Clan.Rating");

						container.Add(new CuiLabel
						{
							RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
							Text =
							{
								Text = $"{clan.Top}",
								Align = TextAnchor.MiddleCenter,
								Font = "robotocondensed-bold.ttf",
								FontSize = 12,
								Color = "1 1 1 1"
							}
						}, Layer + ".Clan.Rating");

						#endregion

						#region Members

						container.Add(new CuiPanel
						{
							RectTransform =
							{
								AnchorMin = "0 1", AnchorMax = "0 1",
								OffsetMin = "320 -225", OffsetMax = "460 -195"
							},
							Image =
							{
								Color = Color3
							}
						}, Layer + ".Content", Layer + ".Clan.Members");

						container.Add(new CuiLabel
						{
							RectTransform =
							{
								AnchorMin = "0 1", AnchorMax = "1 1",
								OffsetMin = "0 0", OffsetMax = "0 20"
							},
							Text =
							{
								Text = Msg(player, MembersTitle),
								Align = TextAnchor.MiddleLeft,
								Font = "robotocondensed-regular.ttf",
								FontSize = 10,
								Color = "1 1 1 1"
							}
						}, Layer + ".Clan.Members");

						container.Add(new CuiLabel
						{
							RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
							Text =
							{
								Text = $"{clan.Members.Count}",
								Align = TextAnchor.MiddleCenter,
								Font = "robotocondensed-bold.ttf",
								FontSize = 12,
								Color = "1 1 1 1"
							}
						}, Layer + ".Clan.Members");

						#endregion

						#region Task

						container.Add(new CuiPanel
						{
							RectTransform =
							{
								AnchorMin = "0 0", AnchorMax = "0 0",
								OffsetMin = "0 10", OffsetMax = "460 90"
							},
							Image =
							{
								Color = Color3
							}
						}, Layer + ".Content", Layer + ".Clan.Task");

						container.Add(new CuiLabel
						{
							RectTransform =
							{
								AnchorMin = "0 1", AnchorMax = "1 1",
								OffsetMin = "0 0", OffsetMax = "0 20"
							},
							Text =
							{
								Text = Msg(player, DescriptionTitle),
								Align = TextAnchor.MiddleLeft,
								Font = "robotocondensed-regular.ttf",
								FontSize = 10,
								Color = "1 1 1 1"
							}
						}, Layer + ".Clan.Task");

						if (clan.IsOwner(player.userID))
							container.Add(new CuiElement
							{
								Parent = Layer + ".Clan.Task",
								Components =
								{
									new CuiInputFieldComponent
									{
										FontSize = 12,
										Align = TextAnchor.MiddleCenter,
										Font = "robotocondensed-regular.ttf",
										Command = "UI_Clans description ",
										Color = "1 1 1 0.85",
										CharsLimit = _config.DescriptionMax,
										NeedsKeyboard = true,
										Text = string.IsNullOrEmpty(clan.Description)
											? Msg(player, NotDescription)
											: $"{clan.Description}"
									},
									new CuiRectTransformComponent
									{
										AnchorMin = "0 0", AnchorMax = "1 1",
										OffsetMin = "5 5", OffsetMax = "-5 -5"
									}
								}
							});
						else
							container.Add(new CuiLabel
							{
								RectTransform =
								{
									AnchorMin = "0 0", AnchorMax = "1 1",
									OffsetMin = "5 5", OffsetMax = "-5 -5"
								},
								Text =
								{
									Text = string.IsNullOrEmpty(clan.Description)
										? Msg(player, NotDescription)
										: $"{clan.Description}",
									Align = TextAnchor.MiddleCenter,
									Font = "robotocondensed-bold.ttf",
									FontSize = 12,
									Color = "1 1 1 0.85"
								}
							}, Layer + ".Clan.Task");

						#endregion

						break;
					}

					case 1:
					{
						amountOnString = 2;
						strings = 8;
						totalAmount = amountOnString * strings;
						ySwitch = 0f;
						height = 35f;
						width = 237.5f;
						margin = 5f;

						var z = 1;

						var availablePlayers = clan.Members.FindAll(member =>
						{
							var memberData = PlayerData.GetOrLoad(member.ToString());
							if (memberData == null) return false;

							return string.IsNullOrEmpty(search) ||
							       search.Length <= 2 ||
							       memberData.DisplayName.StartsWith(search) ||
							       memberData.DisplayName.Contains(search) ||
							       memberData.DisplayName.EndsWith(search);
						}).ToArray();

						foreach (var member in availablePlayers
							         .Skip(zPage * totalAmount).Take(totalAmount))
						{
							xSwitch = z % amountOnString == 0
								? margin + width
								: 0;

							container.Add(new CuiPanel
							{
								RectTransform =
								{
									AnchorMin = "0 1", AnchorMax = "0 1",
									OffsetMin = $"{xSwitch} {ySwitch - height}",
									OffsetMax = $"{xSwitch + width} {ySwitch}"
								},
								Image =
								{
									Color = Color3
								}
							}, Layer + ".Content", Layer + $".Player.{member}");

							container.Add(new CuiElement
							{
								Parent = Layer + $".Player.{member}",
								Components =
								{
									new CuiRawImageComponent
										{Png = ImageLibrary.Call<string>("GetImage", $"avatar_{member}")},
									new CuiRectTransformComponent
									{
										AnchorMin = "0 0", AnchorMax = "0 0",
										OffsetMin = "0 0", OffsetMax = $"{height} {height}"
									}
								}
							});

							#region Display Name

							container.Add(new CuiLabel
							{
								RectTransform =
								{
									AnchorMin = "0 0.5", AnchorMax = "0 1",
									OffsetMin = "40 1",
									OffsetMax = "95 0"
								},
								Text =
								{
									Text = Msg(player, NameTitle),
									Align = TextAnchor.LowerLeft,
									Font = "robotocondensed-regular.ttf",
									FontSize = 10,
									Color = "1 1 1 1"
								}
							}, Layer + $".Player.{member}");

							container.Add(new CuiLabel
							{
								RectTransform =
								{
									AnchorMin = "0 0", AnchorMax = "0 0.5",
									OffsetMin = "40 0",
									OffsetMax = "100 -1"
								},
								Text =
								{
									Text = $"{PlayerData.GetOrLoad(member.ToString())?.DisplayName}",
									Align = TextAnchor.UpperLeft,
									Font = "robotocondensed-bold.ttf",
									FontSize = 10,
									Color = "1 1 1 1"
								}
							}, Layer + $".Player.{member}");

							#endregion

							#region SteamId

							container.Add(new CuiLabel
							{
								RectTransform =
								{
									AnchorMin = "0 0.5", AnchorMax = "0 1",
									OffsetMin = "95 1",
									OffsetMax = "210 0"
								},
								Text =
								{
									Text = Msg(player, SteamIdTitle),
									Align = TextAnchor.LowerLeft,
									Font = "robotocondensed-regular.ttf",
									FontSize = 10,
									Color = "1 1 1 1"
								}
							}, Layer + $".Player.{member}");

							container.Add(new CuiLabel
							{
								RectTransform =
								{
									AnchorMin = "0 0", AnchorMax = "0 0.5",
									OffsetMin = "95 0",
									OffsetMax = "210 -1"
								},
								Text =
								{
									Text = $"{member}",
									Align = TextAnchor.UpperLeft,
									Font = "robotocondensed-bold.ttf",
									FontSize = 10,
									Color = "1 1 1 1"
								}
							}, Layer + $".Player.{member}");

							#endregion

							#region Button

							container.Add(new CuiButton
							{
								RectTransform =
								{
									AnchorMin = "1 0.5", AnchorMax = "1 0.5",
									OffsetMin = "-45 -8", OffsetMax = "-5 8"
								},
								Text =
								{
									Text = Msg(player, ProfileTitle),
									Align = TextAnchor.MiddleCenter,
									Font = "robotocondensed-regular.ttf",
									FontSize = 10,
									Color = "1 1 1 1"
								},
								Button =
								{
									Color = Color2,
									Command = $"UI_Clans showclanprofile {member}"
								}
							}, Layer + $".Player.{member}");

							#endregion

							if (z % amountOnString == 0) ySwitch = ySwitch - height - margin;

							z++;
						}

						#region Search

						container.Add(new CuiPanel
						{
							RectTransform =
							{
								AnchorMin = "0.5 0", AnchorMax = "0.5 0",
								OffsetMin = "-140 20",
								OffsetMax = "60 55"
							},
							Image =
							{
								Color = Color4
							}
						}, Layer + ".Content", Layer + ".Search");

						container.Add(new CuiElement
						{
							Parent = Layer + ".Search",
							Components =
							{
								new CuiInputFieldComponent
								{
									FontSize = 12,
									Align = TextAnchor.MiddleCenter,
									Font = "robotocondensed-regular.ttf",
									Command = $"UI_Clans page {page} 0 ",
									Color = "1 1 1 0.65",
									CharsLimit = 32,
									NeedsKeyboard = true,
									Text = string.IsNullOrEmpty(search) ? Msg(player, SearchTitle) : $"{search}"
								},
								new CuiRectTransformComponent
								{
									AnchorMin = "0 0", AnchorMax = "1 1"
								}
							}
						});

						#endregion

						#region Pages

						container.Add(new CuiButton
						{
							RectTransform =
							{
								AnchorMin = "0.5 0", AnchorMax = "0.5 0",
								OffsetMin = "65 20",
								OffsetMax = "100 55"
							},
							Text =
							{
								Text = Msg(player, BackPage),
								Align = TextAnchor.MiddleCenter,
								Font = "robotocondensed-regular.ttf",
								FontSize = 12,
								Color = "1 1 1 1"
							},
							Button =
							{
								Color = Color4,
								Command = zPage != 0 ? $"UI_Clans page {page} {zPage - 1} {search}" : ""
							}
						}, Layer + ".Content");

						container.Add(new CuiButton
						{
							RectTransform =
							{
								AnchorMin = "0.5 0", AnchorMax = "0.5 0",
								OffsetMin = "105 20",
								OffsetMax = "140 55"
							},
							Text =
							{
								Text = Msg(player, NextPage),
								Align = TextAnchor.MiddleCenter,
								Font = "robotocondensed-regular.ttf",
								FontSize = 12,
								Color = "1 1 1 1"
							},
							Button =
							{
								Color = Color2,
								Command = availablePlayers.Length > (zPage + 1) * totalAmount
									? $"UI_Clans page {page} {zPage + 1} {search}"
									: ""
							}
						}, Layer + ".Content");

						#endregion


						break;
					}

					case 2:
					{
						#region Title

						container.Add(new CuiLabel
						{
							RectTransform =
							{
								AnchorMin = "0 1", AnchorMax = "0 1",
								OffsetMin = "2.5 -30", OffsetMax = "225 0"
							},
							Text =
							{
								Text = Msg(player, TopClansTitle),
								Align = TextAnchor.MiddleLeft,
								Font = "robotocondensed-regular.ttf",
								FontSize = 12,
								Color = "1 1 1 1"
							}
						}, Layer + ".Content");

						#endregion

						#region Head

						ySwitch = 0;

						_config.UI.TopClansColumns.ForEach(column =>
						{
							container.Add(new CuiLabel
							{
								RectTransform =
								{
									AnchorMin = "0 1", AnchorMax = "0 1",
									OffsetMin = $"{ySwitch} -50", OffsetMax = $"{ySwitch + column.Width} -30"
								},
								Text =
								{
									Text = Msg(player, column.LangKey),
									Align = column.TextAlign,
									Font = "robotocondensed-regular.ttf",
									FontSize = column.TitleFontSize,
									Color = "1 1 1 1"
								}
							}, Layer + ".Content");

							ySwitch += column.Width;
						});

						#endregion

						#region Table

						ySwitch = -50;
						height = 37.5f;
						margin = 2.5f;
						totalAmount = 7;

						var i = 0;
						foreach (var topClan in TopClans.Skip(zPage * totalAmount).Take(totalAmount))
						{
							var top = zPage * totalAmount + i + 1;

							container.Add(new CuiPanel
							{
								RectTransform =
								{
									AnchorMin = "0 1", AnchorMax = "0 1",
									OffsetMin = $"0 {ySwitch - height}",
									OffsetMax = $"480 {ySwitch}"
								},
								Image =
								{
									Color = Color3
								}
							}, Layer + ".Content", Layer + $".TopClan.{i}");

							var localSwitch = 0f;
							_config.UI.TopClansColumns.ForEach(column =>
							{
								container.Add(new CuiLabel
								{
									RectTransform =
									{
										AnchorMin = "0 0", AnchorMax = "0 1",
										OffsetMin = $"{localSwitch} 0",
										OffsetMax = $"{localSwitch + column.Width} 0"
									},
									Text =
									{
										Text = $"{column.GetFormat(top, topClan.GetParams(column.Key))}",
										Align = column.TextAlign,
										Font = "robotocondensed-bold.ttf",
										FontSize = column.FontSize,
										Color = "1 1 1 1"
									}
								}, Layer + $".TopClan.{i}");

								localSwitch += column.Width;
							});

							container.Add(new CuiButton
							{
								RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
								Text = {Text = ""},
								Button =
								{
									Color = "0 0 0 0",
									Command = topClan == clan
										? "UI_Clans page 0"
										: $"UI_Clans showclan {topClan.ClanTag}"
								}
							}, Layer + $".TopClan.{i}");

							ySwitch = ySwitch - height - margin;

							i++;
						}

						#endregion

						#region Pages

						PagesUi(ref container, player, (int) Math.Ceiling((double) TopClans.Count / totalAmount), page,
							zPage);

						#endregion

						break;
					}

					case 3:
					{
						#region Title

						container.Add(new CuiLabel
						{
							RectTransform =
							{
								AnchorMin = "0 1", AnchorMax = "0 1",
								OffsetMin = "2.5 -30", OffsetMax = "225 0"
							},
							Text =
							{
								Text = Msg(player, TopPlayersTitle),
								Align = TextAnchor.MiddleLeft,
								Font = "robotocondensed-regular.ttf",
								FontSize = 12,
								Color = "1 1 1 1"
							}
						}, Layer + ".Content");

						#endregion

						#region Head

						ySwitch = 0;
						_config.UI.TopPlayersColumns.ForEach(column =>
						{
							container.Add(new CuiLabel
							{
								RectTransform =
								{
									AnchorMin = "0 1", AnchorMax = "0 1",
									OffsetMin = $"{ySwitch} -50", OffsetMax = $"{ySwitch + column.Width} -30"
								},
								Text =
								{
									Text = Msg(player, column.LangKey),
									Align = column.TextAlign,
									Font = "robotocondensed-regular.ttf",
									FontSize = column.TitleFontSize,
									Color = "1 1 1 1"
								}
							}, Layer + ".Content");

							ySwitch += column.Width;
						});

						#endregion

						#region Table

						ySwitch = -50;
						height = 37.5f;
						margin = 2.5f;
						totalAmount = 7;

						var i = 0;
						foreach (var topPlayer in TopPlayers.Skip(zPage * totalAmount).Take(totalAmount))
						{
							var top = zPage * totalAmount + i + 1;

							container.Add(new CuiPanel
							{
								RectTransform =
								{
									AnchorMin = "0 1", AnchorMax = "0 1",
									OffsetMin = $"0 {ySwitch - height}",
									OffsetMax = $"480 {ySwitch}"
								},
								Image =
								{
									Color = Color3
								}
							}, Layer + ".Content", Layer + $".TopPlayer.{i}");

							var localSwitch = 0f;
							_config.UI.TopPlayersColumns.ForEach(column =>
							{
								var param = topPlayer.Value.GetParams(column.Key);
								container.Add(new CuiLabel
								{
									RectTransform =
									{
										AnchorMin = "0 0", AnchorMax = "0 1",
										OffsetMin = $"{localSwitch} 0",
										OffsetMax = $"{localSwitch + column.Width} 0"
									},
									Text =
									{
										Text = $"{column.GetFormat(top, param)}",
										Align = column.TextAlign,
										Font = "robotocondensed-bold.ttf",
										FontSize = column.FontSize,
										Color = "1 1 1 1"
									}
								}, Layer + $".TopPlayer.{i}");

								localSwitch += column.Width;
							});

							container.Add(new CuiButton
							{
								RectTransform =
								{
									AnchorMin = "0 0", AnchorMax = "1 1"
								},
								Text =
								{
									Text = ""
								},
								Button =
								{
									Color = "0 0 0 0",
									Command = $"UI_Clans showprofile {topPlayer.Key}"
								}
							}, Layer + $".TopPlayer.{i}");

							ySwitch = ySwitch - height - margin;

							i++;
						}

						#endregion

						#region Pages

						PagesUi(ref container, player, (int) Math.Ceiling((double) TopPlayers.Count / totalAmount),
							page, zPage);

						#endregion

						break;
					}

					case 4:
					{
						amountOnString = 4;
						strings = 3;
						totalAmount = amountOnString * strings;

						height = 115;
						width = 115;
						margin = 5;

						xSwitch = 0;
						ySwitch = 0;

						if (clan.IsOwner(player.userID))
						{
							for (var i = 0; i < totalAmount; i++)
							{
								var founded = clan.ResourceStandarts.ContainsKey(i);

								container.Add(new CuiPanel
								{
									RectTransform =
									{
										AnchorMin = "0 1", AnchorMax = "0 1",
										OffsetMin = $"{xSwitch} {ySwitch - height}",
										OffsetMax = $"{xSwitch + width} {ySwitch}"
									},
									Image =
									{
										Color = founded ? Color3 : Color4
									}
								}, Layer + ".Content", Layer + $".ResourсeStandart.{i}");

								if (founded)
								{
									var standart = clan.ResourceStandarts[i];
									if (standart == null) continue;

									container.Add(standart.GetImage("0.5 1", "0.5 1", "-30 -70", "30 -10",
										Layer + $".ResourсeStandart.{i}"));

									#region Progress Text

									var done = data.GetValue(standart.ShortName);

									if (done < standart.Amount)
									{
										container.Add(new CuiLabel
										{
											RectTransform =
											{
												AnchorMin = "0.5 1", AnchorMax = "0.5 1",
												OffsetMin = "-55 -85", OffsetMax = "55 -75"
											},
											Text =
											{
												Text = Msg(player, LeftTitle),
												Align = TextAnchor.MiddleLeft,
												Font = "robotocondensed-regular.ttf",
												FontSize = 10,
												Color = "1 1 1 0.35"
											}
										}, Layer + $".ResourсeStandart.{i}");

										container.Add(new CuiLabel
										{
											RectTransform =
											{
												AnchorMin = "0.5 1", AnchorMax = "0.5 1",
												OffsetMin = "-55 -100", OffsetMax = "55 -85"
											},
											Text =
											{
												Text = $"{done} / {standart.Amount}",
												Align = TextAnchor.MiddleCenter,
												Font = "robotocondensed-bold.ttf",
												FontSize = 12,
												Color = "1 1 1 1"
											}
										}, Layer + $".ResourсeStandart.{i}");
									}

									#endregion

									#region Progress Bar

									container.Add(new CuiPanel
									{
										RectTransform =
										{
											AnchorMin = "0 0", AnchorMax = "1 0",
											OffsetMin = "0 0", OffsetMax = "0 10"
										},
										Image =
										{
											Color = Color4
										}
									}, Layer + $".ResourсeStandart.{i}", Layer + $".ResourсeStandart.{i}.Progress");

									var progress = done < standart.Amount ? done / standart.Amount : 1f;
									if (progress > 0)
										container.Add(new CuiPanel
										{
											RectTransform =
											{
												AnchorMin = "0 0", AnchorMax = $"{progress} 0.95"
											},
											Image =
											{
												Color = Color2
											}
										}, Layer + $".ResourсeStandart.{i}.Progress");

									#endregion

									#region Edit

									if (clan.IsOwner(player.userID))
										container.Add(new CuiButton
										{
											RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
											Text = {Text = ""},
											Button =
											{
												Color = "0 0 0 0",
												Command = $"UI_Clans edititem {i}"
											}
										}, Layer + $".ResourсeStandart.{i}");

									#endregion
								}
								else
								{
									container.Add(new CuiLabel
									{
										RectTransform =
										{
											AnchorMin = "0.5 1", AnchorMax = "0.5 1",
											OffsetMin = "-30 -70", OffsetMax = "30 -10"
										},
										Text =
										{
											Text = "?",
											Align = TextAnchor.MiddleCenter,
											FontSize = 24,
											Font = "robotocondensed-bold.ttf",
											Color = "1 1 1 0.5"
										}
									}, Layer + $".ResourсeStandart.{i}");

									container.Add(new CuiButton
									{
										RectTransform =
										{
											AnchorMin = "0 0", AnchorMax = "1 0",
											OffsetMin = "0 0", OffsetMax = "0 25"
										},
										Text =
										{
											Text = Msg(player, EditTitle),
											Align = TextAnchor.MiddleCenter,
											Font = "robotocondensed-regular.ttf",
											FontSize = 10,
											Color = "1 1 1 1"
										},
										Button =
										{
											Color = Color2,
											Command = $"UI_Clans edititem {i}"
										}
									}, Layer + $".ResourсeStandart.{i}");
								}

								if ((i + 1) % amountOnString == 0)
								{
									xSwitch = 0;
									ySwitch = ySwitch - height - margin;
								}
								else
								{
									xSwitch += width + margin;
								}
							}
						}
						else
						{
							var z = 1;
							foreach (var standart in clan.ResourceStandarts)
							{
								container.Add(new CuiPanel
								{
									RectTransform =
									{
										AnchorMin = "0 1", AnchorMax = "0 1",
										OffsetMin = $"{xSwitch} {ySwitch - height}",
										OffsetMax = $"{xSwitch + width} {ySwitch}"
									},
									Image =
									{
										Color = Color3
									}
								}, Layer + ".Content", Layer + $".ResourсeStandart.{z}");

								container.Add(standart.Value.GetImage("0.5 1", "0.5 1", "-30 -70", "30 -10",
									Layer + $".ResourсeStandart.{z}"));

								#region Progress Text

								var done = data.GetValue(standart.Value.ShortName);

								if (done < standart.Value.Amount)
								{
									container.Add(new CuiLabel
									{
										RectTransform =
										{
											AnchorMin = "0.5 1", AnchorMax = "0.5 1",
											OffsetMin = "-55 -85", OffsetMax = "55 -75"
										},
										Text =
										{
											Text = Msg(player, LeftTitle),
											Align = TextAnchor.MiddleLeft,
											Font = "robotocondensed-regular.ttf",
											FontSize = 10,
											Color = "1 1 1 0.35"
										}
									}, Layer + $".ResourсeStandart.{z}");

									container.Add(new CuiLabel
									{
										RectTransform =
										{
											AnchorMin = "0.5 1", AnchorMax = "0.5 1",
											OffsetMin = "-55 -100", OffsetMax = "55 -85"
										},
										Text =
										{
											Text = $"{done} / {standart.Value.Amount}",
											Align = TextAnchor.MiddleCenter,
											Font = "robotocondensed-bold.ttf",
											FontSize = 12,
											Color = "1 1 1 1"
										}
									}, Layer + $".ResourсeStandart.{z}");
								}

								#endregion

								#region Progress Bar

								container.Add(new CuiPanel
								{
									RectTransform =
									{
										AnchorMin = "0 0", AnchorMax = "1 0",
										OffsetMin = "0 0", OffsetMax = "0 10"
									},
									Image =
									{
										Color = Color4
									}
								}, Layer + $".ResourсeStandart.{z}", Layer + $".ResourсeStandart.{z}.Progress");

								var progress = done < standart.Value.Amount ? done / standart.Value.Amount : 1f;
								if (progress > 0)
									container.Add(new CuiPanel
									{
										RectTransform =
										{
											AnchorMin = "0 0", AnchorMax = $"{progress} 0.95"
										},
										Image =
										{
											Color = Color2
										}
									}, Layer + $".ResourсeStandart.{z}.Progress");

								#endregion

								if (z % amountOnString == 0)
								{
									xSwitch = 0;
									ySwitch = ySwitch - height - margin;
								}
								else
								{
									xSwitch += width + margin;
								}

								z++;
							}
						}

						break;
					}

					case 5:
					{
						amountOnString = 2;
						strings = 8;
						totalAmount = amountOnString * strings;
						ySwitch = 0f;
						height = 35f;
						width = 237.5f;
						margin = 5f;

						var z = 1;
						var availablePlayers = BasePlayer.allPlayerList.Where(member =>
						{
							if (!_invites.CanSendInvite(member.userID, clan.ClanTag))
								return false;

							var memberData = PlayerData.GetOrLoad(member.UserIDString);
							if (memberData?.GetClan() != null)
								return false;

							return string.IsNullOrEmpty(search) || search.Length <= 2 || member.displayName == search ||
							       member.displayName.StartsWith(search, StringComparison.CurrentCultureIgnoreCase) ||
							       member.displayName.Contains(search);
						}).ToArray();

						foreach (var member in availablePlayers
							         .Skip(zPage * totalAmount).Take(totalAmount))
						{
							xSwitch = z % amountOnString == 0
								? margin * 2 + width
								: margin;

							container.Add(new CuiPanel
							{
								RectTransform =
								{
									AnchorMin = "0 1", AnchorMax = "0 1",
									OffsetMin = $"{xSwitch} {ySwitch - height}",
									OffsetMax = $"{xSwitch + width} {ySwitch}"
								},
								Image =
								{
									Color = Color3
								}
							}, Layer + ".Content", Layer + $".Player.{member.userID}");

							container.Add(new CuiElement
							{
								Parent = Layer + $".Player.{member.userID}",
								Components =
								{
									new CuiRawImageComponent
										{Png = ImageLibrary.Call<string>("GetImage", $"avatar_{member.userID}")},
									new CuiRectTransformComponent
									{
										AnchorMin = "0 0", AnchorMax = "0 0",
										OffsetMin = "0 0", OffsetMax = $"{height} {height}"
									}
								}
							});

							#region Display Name

							container.Add(new CuiLabel
							{
								RectTransform =
								{
									AnchorMin = "0 0.5", AnchorMax = "0 1",
									OffsetMin = "40 1",
									OffsetMax = "110 0"
								},
								Text =
								{
									Text = Msg(player, NameTitle),
									Align = TextAnchor.LowerLeft,
									Font = "robotocondensed-regular.ttf",
									FontSize = 10,
									Color = "1 1 1 1"
								}
							}, Layer + $".Player.{member.userID}");

							container.Add(new CuiLabel
							{
								RectTransform =
								{
									AnchorMin = "0 0", AnchorMax = "0 0.5",
									OffsetMin = "40 0",
									OffsetMax = "95 -1"
								},
								Text =
								{
									Text = $"{member.displayName}",
									Align = TextAnchor.UpperLeft,
									Font = "robotocondensed-bold.ttf",
									FontSize = 10,
									Color = "1 1 1 1"
								}
							}, Layer + $".Player.{member.userID}");

							#endregion

							#region SteamId

							container.Add(new CuiLabel
							{
								RectTransform =
								{
									AnchorMin = "0 0.5", AnchorMax = "0 1",
									OffsetMin = "95 1",
									OffsetMax = "210 0"
								},
								Text =
								{
									Text = Msg(player, SteamIdTitle),
									Align = TextAnchor.LowerLeft,
									Font = "robotocondensed-regular.ttf",
									FontSize = 10,
									Color = "1 1 1 1"
								}
							}, Layer + $".Player.{member.userID}");

							container.Add(new CuiLabel
							{
								RectTransform =
								{
									AnchorMin = "0 0", AnchorMax = "0 0.5",
									OffsetMin = "95 0",
									OffsetMax = "210 -1"
								},
								Text =
								{
									Text = $"{member.userID}",
									Align = TextAnchor.UpperLeft,
									Font = "robotocondensed-bold.ttf",
									FontSize = 10,
									Color = "1 1 1 1"
								}
							}, Layer + $".Player.{member.userID}");

							#endregion

							#region Button

							container.Add(new CuiButton
							{
								RectTransform =
								{
									AnchorMin = "1 0.5", AnchorMax = "1 0.5",
									OffsetMin = "-45 -8", OffsetMax = "-5 8"
								},
								Text =
								{
									Text = Msg(player, InviteTitle),
									Align = TextAnchor.MiddleCenter,
									Font = "robotocondensed-regular.ttf",
									FontSize = 10,
									Color = "1 1 1 1"
								},
								Button =
								{
									Color = Color2,
									Command = $"UI_Clans invite send {member.userID}"
								}
							}, Layer + $".Player.{member.userID}");

							#endregion

							if (z % amountOnString == 0) ySwitch = ySwitch - height - margin;

							z++;
						}

						#region Search

						container.Add(new CuiPanel
						{
							RectTransform =
							{
								AnchorMin = "0.5 0", AnchorMax = "0.5 0",
								OffsetMin = "-140 20",
								OffsetMax = "60 55"
							},
							Image =
							{
								Color = Color4
							}
						}, Layer + ".Content", Layer + ".Search");

						container.Add(new CuiElement
						{
							Parent = Layer + ".Search",
							Components =
							{
								new CuiInputFieldComponent
								{
									FontSize = 12,
									Align = TextAnchor.MiddleCenter,
									Font = "robotocondensed-regular.ttf",
									Command = $"UI_Clans page {page} 0 ",
									Color = "1 1 1 0.65",
									CharsLimit = 32,
									NeedsKeyboard = true,
									Text = string.IsNullOrEmpty(search) ? Msg(player, SearchTitle) : $"{search}"
								},
								new CuiRectTransformComponent
								{
									AnchorMin = "0 0", AnchorMax = "1 1"
								}
							}
						});

						#endregion

						#region Pages

						container.Add(new CuiButton
						{
							RectTransform =
							{
								AnchorMin = "0.5 0", AnchorMax = "0.5 0",
								OffsetMin = "65 20",
								OffsetMax = "100 55"
							},
							Text =
							{
								Text = Msg(player, BackPage),
								Align = TextAnchor.MiddleCenter,
								Font = "robotocondensed-regular.ttf",
								FontSize = 12,
								Color = "1 1 1 1"
							},
							Button =
							{
								Color = Color4,
								Command = zPage != 0 ? $"UI_Clans page {page} {zPage - 1} {search}" : ""
							}
						}, Layer + ".Content");

						container.Add(new CuiButton
						{
							RectTransform =
							{
								AnchorMin = "0.5 0", AnchorMax = "0.5 0",
								OffsetMin = "105 20",
								OffsetMax = "140 55"
							},
							Text =
							{
								Text = Msg(player, NextPage),
								Align = TextAnchor.MiddleCenter,
								Font = "robotocondensed-regular.ttf",
								FontSize = 12,
								Color = "1 1 1 1"
							},
							Button =
							{
								Color = Color2,
								Command = availablePlayers.Length > (zPage + 1) * totalAmount
									? $"UI_Clans page {page} {zPage + 1} {search}"
									: ""
							}
						}, Layer + ".Content");

						#endregion

						break;
					}

					case 6:
					{
						#region List

						amountOnString = 4;
						strings = 3;
						totalAmount = amountOnString * strings;

						height = 110;
						width = 110;
						margin = 5;

						xSwitch = 0;
						ySwitch = 0;

						var isOwner = clan.IsOwner(player.userID);

						var i = 0;
						_config.Skins.ItemSkins.Keys.ToList().Skip(totalAmount * zPage).Take(totalAmount).ForEach(
							item =>
							{
								container.Add(new CuiPanel
								{
									RectTransform =
									{
										AnchorMin = "0 1", AnchorMax = "0 1",
										OffsetMin = $"{xSwitch} {ySwitch - height}",
										OffsetMax = $"{xSwitch + width} {ySwitch}"
									},
									Image =
									{
										Color = Color3
									}
								}, Layer + ".Content", Layer + $".SkinItem.{i}");

								container.Add(new CuiElement
								{
									Parent = Layer + $".SkinItem.{i}",
									Components =
									{
										new CuiImageComponent
										{
											ItemId = FindItemID(item),
											SkinId = GetItemSkin(item, clan)
										},
										new CuiRectTransformComponent
										{
											AnchorMin = isOwner ? "0.5 1" : "0.5 0.5",
											AnchorMax = isOwner ? "0.5 1" : "0.5 0.5",
											OffsetMin = isOwner ? "-30 -70" : "-30 -30",
											OffsetMax = isOwner ? "30 -10" : "30 30"
										}
									}
								});

								#region Edit

								if (isOwner)
									container.Add(new CuiButton
									{
										RectTransform =
										{
											AnchorMin = "0 0", AnchorMax = "1 0",
											OffsetMin = "0 0", OffsetMax = "0 25"
										},
										Text =
										{
											Text = Msg(player, EditTitle),
											Align = TextAnchor.MiddleCenter,
											Font = "robotocondensed-regular.ttf",
											FontSize = 10,
											Color = "1 1 1 1"
										},
										Button =
										{
											Color = Color2,
											Command = $"UI_Clans editskin {item}"
										}
									}, Layer + $".SkinItem.{i}");

								#endregion

								if ((i + 1) % amountOnString == 0)
								{
									xSwitch = 0;
									ySwitch = ySwitch - height - margin - margin;
								}
								else
								{
									xSwitch += width + margin;
								}

								i++;
							});

						#endregion

						#region Pages

						PagesUi(ref container, player,
							(int) Math.Ceiling((double) _config.Skins.ItemSkins.Keys.Count / totalAmount), page, zPage);

						#endregion

						#region Header

						if (_config.Skins.DisableSkins)
							ButtonClanSkins(ref container, player, data);

						#endregion

						break;
					}

					case 7:
					{
						if (clan.Alliances.Count == 0)
						{
							container.Add(new CuiLabel
							{
								RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
								Text =
								{
									Text = Msg(player, NoAllies),
									Align = TextAnchor.MiddleCenter,
									FontSize = 34,
									Font = "robotocondensed-bold.ttf",
									Color = Color5
								}
							}, Layer + ".Content");
						}
						else
						{
							amountOnString = 2;
							strings = 8;
							totalAmount = amountOnString * strings;
							ySwitch = 0f;
							height = 35f;
							width = 237.5f;
							margin = 5f;

							var z = 1;

							foreach (var alliance in clan.Alliances
								         .Skip(zPage * totalAmount).Take(totalAmount))
							{
								xSwitch = z % amountOnString == 0
									? margin + width
									: 0;

								var allianceClan = FindClanByTag(alliance);
								if (allianceClan == null) continue;

								container.Add(new CuiPanel
								{
									RectTransform =
									{
										AnchorMin = "0 1", AnchorMax = "0 1",
										OffsetMin = $"{xSwitch} {ySwitch - height}",
										OffsetMax = $"{xSwitch + width} {ySwitch}"
									},
									Image =
									{
										Color = Color3
									}
								}, Layer + ".Content", Layer + $".Player.{alliance}");

								container.Add(new CuiElement
								{
									Parent = Layer + $".Player.{alliance}",
									Components =
									{
										new CuiRawImageComponent
											{Png = ImageLibrary.Call<string>("GetImage", $"{allianceClan.Avatar}")},
										new CuiRectTransformComponent
										{
											AnchorMin = "0 0", AnchorMax = "0 0",
											OffsetMin = "0 0", OffsetMax = $"{height} {height}"
										}
									}
								});

								#region Display Name

								container.Add(new CuiLabel
								{
									RectTransform =
									{
										AnchorMin = "0 0.5", AnchorMax = "0 1",
										OffsetMin = "40 1",
										OffsetMax = "110 0"
									},
									Text =
									{
										Text = Msg(player, NameTitle),
										Align = TextAnchor.LowerLeft,
										Font = "robotocondensed-regular.ttf",
										FontSize = 10,
										Color = "1 1 1 1"
									}
								}, Layer + $".Player.{alliance}");

								container.Add(new CuiLabel
								{
									RectTransform =
									{
										AnchorMin = "0 0", AnchorMax = "0 0.5",
										OffsetMin = "40 0",
										OffsetMax = "95 -1"
									},
									Text =
									{
										Text = $"{allianceClan.ClanTag}",
										Align = TextAnchor.UpperLeft,
										Font = "robotocondensed-bold.ttf",
										FontSize = 10,
										Color = "1 1 1 1"
									}
								}, Layer + $".Player.{alliance}");

								#endregion

								#region SteamId

								container.Add(new CuiLabel
								{
									RectTransform =
									{
										AnchorMin = "0 0.5", AnchorMax = "0 1",
										OffsetMin = "95 1",
										OffsetMax = "210 0"
									},
									Text =
									{
										Text = Msg(player, MembersTitle),
										Align = TextAnchor.LowerLeft,
										Font = "robotocondensed-regular.ttf",
										FontSize = 10,
										Color = "1 1 1 1"
									}
								}, Layer + $".Player.{alliance}");

								container.Add(new CuiLabel
								{
									RectTransform =
									{
										AnchorMin = "0 0", AnchorMax = "0 0.5",
										OffsetMin = "95 0",
										OffsetMax = "210 -1"
									},
									Text =
									{
										Text = $"{allianceClan.Members.Count}",
										Align = TextAnchor.UpperLeft,
										Font = "robotocondensed-bold.ttf",
										FontSize = 10,
										Color = "1 1 1 1"
									}
								}, Layer + $".Player.{alliance}");

								#endregion

								#region Button

								container.Add(new CuiButton
								{
									RectTransform =
									{
										AnchorMin = "1 0.5", AnchorMax = "1 0.5",
										OffsetMin = "-45 -8", OffsetMax = "-5 8"
									},
									Text =
									{
										Text = Msg(player, ProfileTitle),
										Align = TextAnchor.MiddleCenter,
										Font = "robotocondensed-regular.ttf",
										FontSize = 10,
										Color = "1 1 1 1"
									},
									Button =
									{
										Color = Color2,
										Command = $"UI_Clans showclan {alliance}"
									}
								}, Layer + $".Player.{alliance}");

								#endregion

								if (z % amountOnString == 0) ySwitch = ySwitch - height - margin;

								z++;
							}

							#region Pages

							container.Add(new CuiButton
							{
								RectTransform =
								{
									AnchorMin = "0.5 0", AnchorMax = "0.5 0",
									OffsetMin = "-37.5 20",
									OffsetMax = "-2.5 55"
								},
								Text =
								{
									Text = Msg(player, BackPage),
									Align = TextAnchor.MiddleCenter,
									Font = "robotocondensed-regular.ttf",
									FontSize = 12,
									Color = "1 1 1 1"
								},
								Button =
								{
									Color = Color4,
									Command = zPage != 0 ? $"UI_Clans page {page} {zPage - 1} {search}" : ""
								}
							}, Layer + ".Content");

							container.Add(new CuiButton
							{
								RectTransform =
								{
									AnchorMin = "0.5 0", AnchorMax = "0.5 0",
									OffsetMin = "2.5 20",
									OffsetMax = "37.5 55"
								},
								Text =
								{
									Text = Msg(player, NextPage),
									Align = TextAnchor.MiddleCenter,
									Font = "robotocondensed-regular.ttf",
									FontSize = 12,
									Color = "1 1 1 1"
								},
								Button =
								{
									Color = Color2,
									Command = clan.Alliances.Count > (zPage + 1) * totalAmount
										? $"UI_Clans page {page} {zPage + 1} {search}"
										: ""
								}
							}, Layer + ".Content");

							#endregion
						}

						break;
					}

					case 45:
					{
						var invites = _invites.GetPlayerClanInvites(player.userID);

						if (invites.Count == 0)
						{
							container.Add(new CuiLabel
							{
								RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
								Text =
								{
									Text = Msg(player, NoInvites),
									Align = TextAnchor.MiddleCenter,
									FontSize = 34,
									Font = "robotocondensed-bold.ttf",
									Color = Color5
								}
							}, Layer + ".Content");
						}
						else
						{
							ySwitch = 0f;
							height = 48.5f;
							margin = 5f;
							totalAmount = 7;

							foreach (var invite in invites.Skip(zPage * totalAmount).Take(totalAmount))
							{
								container.Add(new CuiPanel
								{
									RectTransform =
									{
										AnchorMin = "0 1", AnchorMax = "0 1",
										OffsetMin = $"0 {ySwitch - height}",
										OffsetMax = $"450 {ySwitch}"
									},
									Image =
									{
										Color = Color3
									}
								}, Layer + ".Content", Layer + $".Invite.{invite.ClanTag}");

								var targetClan = FindClanByTag(invite.ClanTag);
								if (targetClan != null && !string.IsNullOrEmpty(targetClan.Avatar))
									container.Add(new CuiElement
									{
										Parent = Layer + $".Invite.{invite.ClanTag}",
										Components =
										{
											new CuiRawImageComponent
												{Png = ImageLibrary.Call<string>("GetImage", targetClan.Avatar)},
											new CuiRectTransformComponent
											{
												AnchorMin = "0 0", AnchorMax = "0 0",
												OffsetMin = "0 0", OffsetMax = $"{height} {height}"
											}
										}
									});

								#region Clan Name

								container.Add(new CuiLabel
								{
									RectTransform =
									{
										AnchorMin = "0 0.5", AnchorMax = "0 1",
										OffsetMin = "55 1", OffsetMax = "135 0"
									},
									Text =
									{
										Text = Msg(player, ClanInvitation),
										Align = TextAnchor.LowerLeft,
										Font = "robotocondensed-regular.ttf",
										FontSize = 12,
										Color = "1 1 1 1"
									}
								}, Layer + $".Invite.{invite.ClanTag}");

								container.Add(new CuiLabel
								{
									RectTransform =
									{
										AnchorMin = "0 0", AnchorMax = "0 0.5",
										OffsetMin = "55 0", OffsetMax = "135 -1"
									},
									Text =
									{
										Text = $"{invite.ClanTag}",
										Align = TextAnchor.UpperLeft,
										Font = "robotocondensed-bold.ttf",
										FontSize = 12,
										Color = "1 1 1 1"
									}
								}, Layer + $".Invite.{invite.ClanTag}");

								#endregion

								#region Inviter

								container.Add(new CuiLabel
								{
									RectTransform =
									{
										AnchorMin = "0 0.5", AnchorMax = "0 1",
										OffsetMin = "160 1", OffsetMax = "315 0"
									},
									Text =
									{
										Text = Msg(player, InviterTitle),
										Align = TextAnchor.LowerLeft,
										Font = "robotocondensed-regular.ttf",
										FontSize = 12,
										Color = "1 1 1 1"
									}
								}, Layer + $".Invite.{invite.ClanTag}");

								container.Add(new CuiLabel
								{
									RectTransform =
									{
										AnchorMin = "0 0", AnchorMax = "0 0.5",
										OffsetMin = "160 0", OffsetMax = "315 -1"
									},
									Text =
									{
										Text = $"{invite.InviterName}",
										Align = TextAnchor.UpperLeft,
										Font = "robotocondensed-bold.ttf",
										FontSize = 12,
										Color = "1 1 1 1"
									}
								}, Layer + $".Invite.{invite.ClanTag}");

								#endregion

								#region Buttons

								container.Add(new CuiButton
								{
									RectTransform =
									{
										AnchorMin = "1 0.5", AnchorMax = "1 0.5",
										OffsetMin = "-95 -12.5", OffsetMax = "-15 12.5"
									},
									Text =
									{
										Text = Msg(player, AcceptTitle),
										Align = TextAnchor.MiddleCenter,
										Font = "robotocondensed-regular.ttf",
										FontSize = 12,
										Color = "1 1 1 1"
									},
									Button =
									{
										Color = Color2,
										Command = $"UI_Clans invite accept {invite.ClanTag}",
										Close = Layer
									}
								}, Layer + $".Invite.{invite.ClanTag}");

								container.Add(new CuiButton
								{
									RectTransform =
									{
										AnchorMin = "1 0.5", AnchorMax = "1 0.5",
										OffsetMin = "-185 -12.5", OffsetMax = "-105 12.5"
									},
									Text =
									{
										Text = Msg(player, CancelTitle),
										Align = TextAnchor.MiddleCenter,
										Font = "robotocondensed-regular.ttf",
										FontSize = 12,
										Color = "1 1 1 1"
									},
									Button =
									{
										Color = Color6,
										Command = $"UI_Clans invite cancel {invite.ClanTag}",
										Close = Layer
									}
								}, Layer + $".Invite.{invite.ClanTag}");

								#endregion

								ySwitch = ySwitch - height - margin;
							}

							#region Pages

							PagesUi(ref container, player,
								(int) Math.Ceiling((double) invites.Count / totalAmount), page, zPage);

							#endregion
						}

						break;
					}

					case 65:
					{
						var invites = _invites.GetClanPlayersInvites(clan.ClanTag);
						if (invites.Count == 0)
						{
							container.Add(new CuiLabel
							{
								RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
								Text =
								{
									Text = Msg(player, NoInvites),
									Align = TextAnchor.MiddleCenter,
									FontSize = 34,
									Font = "robotocondensed-bold.ttf",
									Color = Color5
								}
							}, Layer + ".Content");
						}
						else
						{
							ySwitch = 0f;
							height = 48.5f;
							margin = 5f;
							totalAmount = 7;

							foreach (var invite in invites.Skip(zPage * totalAmount).Take(totalAmount))
							{
								container.Add(new CuiPanel
								{
									RectTransform =
									{
										AnchorMin = "0 1", AnchorMax = "0 1",
										OffsetMin = $"0 {ySwitch - height}",
										OffsetMax = $"450 {ySwitch}"
									},
									Image =
									{
										Color = Color3
									}
								}, Layer + ".Content", Layer + $".Invite.{invite.RetrieverId}");

								container.Add(new CuiElement
								{
									Parent = Layer + $".Invite.{invite.RetrieverId}",
									Components =
									{
										new CuiRawImageComponent
										{
											Png = ImageLibrary.Call<string>("GetImage", $"avatar_{invite.RetrieverId}")
										},
										new CuiRectTransformComponent
										{
											AnchorMin = "0 0", AnchorMax = "0 0",
											OffsetMin = "0 0", OffsetMax = $"{height} {height}"
										}
									}
								});

								#region Player Name

								container.Add(new CuiLabel
								{
									RectTransform =
									{
										AnchorMin = "0 0.5", AnchorMax = "0 1",
										OffsetMin = "75 1", OffsetMax = "195 0"
									},
									Text =
									{
										Text = Msg(player, PlayerTitle),
										Align = TextAnchor.LowerLeft,
										Font = "robotocondensed-regular.ttf",
										FontSize = 12,
										Color = "1 1 1 1"
									}
								}, Layer + $".Invite.{invite.RetrieverId}");

								container.Add(new CuiLabel
								{
									RectTransform =
									{
										AnchorMin = "0 0", AnchorMax = "0 0.5",
										OffsetMin = "75 0", OffsetMax = "195 -1"
									},
									Text =
									{
										Text = $"{PlayerData.GetOrLoad(invite.RetrieverId.ToString())?.DisplayName}",
										Align = TextAnchor.UpperLeft,
										Font = "robotocondensed-bold.ttf",
										FontSize = 12,
										Color = "1 1 1 1"
									}
								}, Layer + $".Invite.{invite.RetrieverId}");

								#endregion

								#region Inviter

								container.Add(new CuiLabel
								{
									RectTransform =
									{
										AnchorMin = "0 0.5", AnchorMax = "0 1",
										OffsetMin = "195 1", OffsetMax = "315 0"
									},
									Text =
									{
										Text = Msg(player, InviterTitle),
										Align = TextAnchor.LowerLeft,
										Font = "robotocondensed-regular.ttf",
										FontSize = 12,
										Color = "1 1 1 1"
									}
								}, Layer + $".Invite.{invite.RetrieverId}");

								container.Add(new CuiLabel
								{
									RectTransform =
									{
										AnchorMin = "0 0", AnchorMax = "0 0.5",
										OffsetMin = "195 0", OffsetMax = "315 -1"
									},
									Text =
									{
										Text = $"{invite.InviterName}",
										Align = TextAnchor.UpperLeft,
										Font = "robotocondensed-bold.ttf",
										FontSize = 12,
										Color = "1 1 1 1"
									}
								}, Layer + $".Invite.{invite.RetrieverId}");

								#endregion

								#region Buttons

								container.Add(new CuiButton
								{
									RectTransform =
									{
										AnchorMin = "1 0.5", AnchorMax = "1 0.5",
										OffsetMin = "-185 -12.5", OffsetMax = "-15 12.5"
									},
									Text =
									{
										Text = Msg(player, CancelTitle),
										Align = TextAnchor.MiddleCenter,
										Font = "robotocondensed-regular.ttf",
										FontSize = 12,
										Color = "1 1 1 1"
									},
									Button =
									{
										Color = Color6,
										Command = $"UI_Clans invite withdraw {invite.RetrieverId}"
									}
								}, Layer + $".Invite.{invite.RetrieverId}");

								#endregion

								ySwitch = ySwitch - height - margin;
							}

							#region Pages

							PagesUi(ref container, player,
								(int) Math.Ceiling((double) invites.Count / totalAmount), page, zPage);

							#endregion
						}


						break;
					}

					case 71: //ally invites
					{
						var invites = _invites.GetAllyTargetInvites(clan.ClanTag);
						if (invites.Count == 0)
						{
							container.Add(new CuiLabel
							{
								RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
								Text =
								{
									Text = Msg(player, NoInvites),
									Align = TextAnchor.MiddleCenter,
									FontSize = 34,
									Font = "robotocondensed-bold.ttf",
									Color = Color5
								}
							}, Layer + ".Content");
						}
						else
						{
							ySwitch = 0f;
							height = 48.5f;
							margin = 5f;
							totalAmount = 7;

							foreach (var invite in invites.Skip(zPage * totalAmount).Take(totalAmount))
							{
								var targetClan = FindClanByTag(invite.SenderClanTag);
								if (targetClan == null) continue;

								container.Add(new CuiPanel
								{
									RectTransform =
									{
										AnchorMin = "0 1", AnchorMax = "0 1",
										OffsetMin = $"0 {ySwitch - height}",
										OffsetMax = $"450 {ySwitch}"
									},
									Image =
									{
										Color = Color3
									}
								}, Layer + ".Content", Layer + $".Invite.{invite.TargetClanTag}");

								container.Add(new CuiElement
								{
									Parent = Layer + $".Invite.{invite.TargetClanTag}",
									Components =
									{
										new CuiRawImageComponent
											{Png = ImageLibrary.Call<string>("GetImage", $"{targetClan.Avatar}")},
										new CuiRectTransformComponent
										{
											AnchorMin = "0 0", AnchorMax = "0 0",
											OffsetMin = "0 0", OffsetMax = $"{height} {height}"
										}
									}
								});

								#region Title

								container.Add(new CuiLabel
								{
									RectTransform =
									{
										AnchorMin = "0 0.5", AnchorMax = "0 1",
										OffsetMin = "75 1", OffsetMax = "195 0"
									},
									Text =
									{
										Text = Msg(player, ClanTitle),
										Align = TextAnchor.LowerLeft,
										Font = "robotocondensed-regular.ttf",
										FontSize = 12,
										Color = "1 1 1 1"
									}
								}, Layer + $".Invite.{invite.TargetClanTag}");

								container.Add(new CuiLabel
								{
									RectTransform =
									{
										AnchorMin = "0 0", AnchorMax = "0 0.5",
										OffsetMin = "75 0", OffsetMax = "195 -1"
									},
									Text =
									{
										Text = $"{targetClan.ClanTag}",
										Align = TextAnchor.UpperLeft,
										Font = "robotocondensed-bold.ttf",
										FontSize = 12,
										Color = "1 1 1 1"
									}
								}, Layer + $".Invite.{invite.TargetClanTag}");

								#endregion

								#region Inviter

								container.Add(new CuiLabel
								{
									RectTransform =
									{
										AnchorMin = "0 0.5", AnchorMax = "0 1",
										OffsetMin = "195 1", OffsetMax = "315 0"
									},
									Text =
									{
										Text = Msg(player, InviterTitle),
										Align = TextAnchor.LowerLeft,
										Font = "robotocondensed-regular.ttf",
										FontSize = 12,
										Color = "1 1 1 1"
									}
								}, Layer + $".Invite.{invite.TargetClanTag}");

								container.Add(new CuiLabel
								{
									RectTransform =
									{
										AnchorMin = "0 0", AnchorMax = "0 0.5",
										OffsetMin = "195 0", OffsetMax = "315 -1"
									},
									Text =
									{
										Text = $"{invite.SenderName}",
										Align = TextAnchor.UpperLeft,
										Font = "robotocondensed-bold.ttf",
										FontSize = 12,
										Color = "1 1 1 1"
									}
								}, Layer + $".Invite.{invite.TargetClanTag}");

								#endregion

								#region Buttons

								container.Add(new CuiButton
								{
									RectTransform =
									{
										AnchorMin = "1 0.5", AnchorMax = "1 0.5",
										OffsetMin = "-185 -12.5", OffsetMax = "-15 12.5"
									},
									Text =
									{
										Text = Msg(player, CancelTitle),
										Align = TextAnchor.MiddleCenter,
										Font = "robotocondensed-regular.ttf",
										FontSize = 12,
										Color = "1 1 1 1"
									},
									Button =
									{
										Color = Color6,
										Command = $"UI_Clans allyinvite withdraw {invite.TargetClanTag}"
									}
								}, Layer + $".Invite.{invite.TargetClanTag}");

								#endregion

								ySwitch = ySwitch - height - margin;
							}

							#region Pages

							PagesUi(ref container, player,
								(int) Math.Ceiling((double) invites.Count / totalAmount), page, zPage);

							#endregion
						}

						break;
					}

					case 72: //incoming ally
					{
						var invites = _invites.GetAllyIncomingInvites(clan.ClanTag);
						if (invites.Count == 0)
						{
							container.Add(new CuiLabel
							{
								RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
								Text =
								{
									Text = Msg(player, NoInvites),
									Align = TextAnchor.MiddleCenter,
									FontSize = 34,
									Font = "robotocondensed-bold.ttf",
									Color = Color5
								}
							}, Layer + ".Content");
						}
						else
						{
							ySwitch = 0f;
							height = 48.5f;
							margin = 5f;
							totalAmount = 7;

							foreach (var invite in invites.Skip(zPage * totalAmount).Take(totalAmount))
							{
								var targetClan = FindClanByTag(invite.SenderClanTag);
								if (targetClan == null) continue;

								container.Add(new CuiPanel
								{
									RectTransform =
									{
										AnchorMin = "0 1", AnchorMax = "0 1",
										OffsetMin = $"0 {ySwitch - height}",
										OffsetMax = $"450 {ySwitch}"
									},
									Image =
									{
										Color = Color3
									}
								}, Layer + ".Content", Layer + $".Invite.{invite.SenderClanTag}");

								container.Add(new CuiElement
								{
									Parent = Layer + $".Invite.{invite.SenderClanTag}",
									Components =
									{
										new CuiRawImageComponent
											{Png = ImageLibrary.Call<string>("GetImage", $"{targetClan.Avatar}")},
										new CuiRectTransformComponent
										{
											AnchorMin = "0 0", AnchorMax = "0 0",
											OffsetMin = "0 0", OffsetMax = $"{height} {height}"
										}
									}
								});

								#region Title

								container.Add(new CuiLabel
								{
									RectTransform =
									{
										AnchorMin = "0 0.5", AnchorMax = "0 1",
										OffsetMin = "75 1", OffsetMax = "195 0"
									},
									Text =
									{
										Text = Msg(player, ClanTitle),
										Align = TextAnchor.LowerLeft,
										Font = "robotocondensed-regular.ttf",
										FontSize = 12,
										Color = "1 1 1 1"
									}
								}, Layer + $".Invite.{invite.SenderClanTag}");

								container.Add(new CuiLabel
								{
									RectTransform =
									{
										AnchorMin = "0 0", AnchorMax = "0 0.5",
										OffsetMin = "75 0", OffsetMax = "195 -1"
									},
									Text =
									{
										Text = $"{targetClan.ClanTag}",
										Align = TextAnchor.UpperLeft,
										Font = "robotocondensed-bold.ttf",
										FontSize = 12,
										Color = "1 1 1 1"
									}
								}, Layer + $".Invite.{invite}");

								#endregion

								#region Buttons

								container.Add(new CuiButton
								{
									RectTransform =
									{
										AnchorMin = "1 0.5", AnchorMax = "1 0.5",
										OffsetMin = "-95 -12.5", OffsetMax = "-15 12.5"
									},
									Text =
									{
										Text = Msg(player, AcceptTitle),
										Align = TextAnchor.MiddleCenter,
										Font = "robotocondensed-regular.ttf",
										FontSize = 12,
										Color = "1 1 1 1"
									},
									Button =
									{
										Color = Color2,
										Command = $"UI_Clans allyinvite accept {invite.SenderClanTag}",
										Close = Layer
									}
								}, Layer + $".Invite.{invite.SenderClanTag}");

								container.Add(new CuiButton
								{
									RectTransform =
									{
										AnchorMin = "1 0.5", AnchorMax = "1 0.5",
										OffsetMin = "-185 -12.5", OffsetMax = "-105 12.5"
									},
									Text =
									{
										Text = Msg(player, CancelTitle),
										Align = TextAnchor.MiddleCenter,
										Font = "robotocondensed-regular.ttf",
										FontSize = 12,
										Color = "1 1 1 1"
									},
									Button =
									{
										Color = Color6,
										Command = $"UI_Clans allyinvite cancel {invite.SenderClanTag}",
										Close = Layer
									}
								}, Layer + $".Invite.{invite.SenderClanTag}");

								#endregion


								ySwitch = ySwitch - height - margin;
							}

							#region Pages

							PagesUi(ref container, player,
								(int) Math.Ceiling((double) invites.Count / totalAmount), page, zPage);

							#endregion
						}

						break;
					}
				}
			else
				container.Add(new CuiLabel
				{
					RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
					Text =
					{
						Text = Msg(player, NotMemberOfClan),
						Align = TextAnchor.MiddleCenter,
						FontSize = 34,
						Font = "robotocondensed-bold.ttf",
						Color = Color5
					}
				}, Layer + ".Content");

			#endregion

			#endregion

			CuiHelper.DestroyUi(player, Layer + ".Main");
			CuiHelper.AddUi(player, container);
		}

		private void SelectItemUi(BasePlayer player, int slot, int page = 0, int amount = 0, string search = "",
			bool first = false)
		{
			#region Fields

			var clan = FindClanByPlayer(player.UserIDString);

			var itemsList = _defaultItems.FindAll(item =>
				string.IsNullOrEmpty(search) || search.Length <= 2 ||
				item.shortname.Contains(search) ||
				item.displayName.english.Contains(search));

			if (amount == 0)
				amount = clan.ResourceStandarts.ContainsKey(slot)
					? clan.ResourceStandarts[slot].Amount
					: _config.DefaultValStandarts;

			var amountOnString = 10;
			var strings = 5;
			var totalAmount = amountOnString * strings;

			var Height = 115f;
			var Width = 110f;
			var Margin = 10f;

			var constSwitchX = -(amountOnString * Width + (amountOnString - 1) * Margin) / 2f;

			var xSwitch = constSwitchX;
			var ySwitch = -75f;

			#endregion

			var container = new CuiElementContainer();

			#region Background

			if (first)
			{
				CuiHelper.DestroyUi(player, Layer);

				container.Add(new CuiPanel
				{
					RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
					Image =
					{
						Color = "0 0 0 0.9",
						Material = "assets/content/ui/uibackgroundblur.mat"
					},
					CursorEnabled = true
				}, "Overlay", Layer);

				container.Add(new CuiButton
				{
					RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
					Text = {Text = ""},
					Button =
					{
						Color = "0 0 0 0",
						Close = Layer
					}
				}, Layer);
			}

			#endregion

			#region Main

			container.Add(new CuiPanel
			{
				RectTransform =
				{
					AnchorMin = "0 0", AnchorMax = "1 1"
				},
				Image =
				{
					Color = Color1
				}
			}, Layer, Layer + ".Main");

			#region Header

			container.Add(new CuiPanel
			{
				RectTransform =
				{
					AnchorMin = "0 1", AnchorMax = "1 1",
					OffsetMin = "0 -55", OffsetMax = "0 0"
				},
				Image =
				{
					Color = Color3
				}
			}, Layer + ".Main", Layer + ".Header");

			#region Title

			container.Add(new CuiLabel
			{
				RectTransform =
				{
					AnchorMin = "0 0", AnchorMax = "1 1",
					OffsetMin = "25 0", OffsetMax = "0 0"
				},
				Text =
				{
					Text = Msg(player, SelectItemTitle),
					Align = TextAnchor.MiddleLeft,
					Font = "robotocondensed-bold.ttf",
					FontSize = 14,
					Color = "1 1 1 1"
				}
			}, Layer + ".Header");

			#endregion

			#region Search

			container.Add(new CuiPanel
			{
				RectTransform =
				{
					AnchorMin = "0 0.5", AnchorMax = "0 0.5",
					OffsetMin = "160 -17.5", OffsetMax = "410 17.5"
				},
				Image =
				{
					Color = Color4
				}
			}, Layer + ".Header", Layer + ".Header.Search");

			container.Add(new CuiElement
			{
				Parent = Layer + ".Header.Search",
				Components =
				{
					new CuiInputFieldComponent
					{
						FontSize = 12,
						Align = TextAnchor.MiddleCenter,
						Font = "robotocondensed-regular.ttf",
						Command = $"UI_Clans selectpages {slot} 0 {amount} ",
						Color = "1 1 1 0.65",
						CharsLimit = 32,
						NeedsKeyboard = true,
						Text = string.IsNullOrEmpty(search) ? Msg(player, SearchTitle) : $"{search}"
					},
					new CuiRectTransformComponent
					{
						AnchorMin = "0 0", AnchorMax = "1 1"
					}
				}
			});

			#endregion

			#region Amount

			container.Add(new CuiPanel
			{
				RectTransform =
				{
					AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5",
					OffsetMin = "-35 -17.5", OffsetMax = "95 17.5"
				},
				Image =
				{
					Color = Color4
				}
			}, Layer + ".Header", Layer + ".Header.Amount");

			container.Add(new CuiElement
			{
				Parent = Layer + ".Header.Amount",
				Components =
				{
					new CuiInputFieldComponent
					{
						FontSize = 12,
						Align = TextAnchor.MiddleCenter,
						Font = "robotocondensed-regular.ttf",
						Command = $"UI_Clans setamountitem {slot} ",
						Color = "1 1 1 0.65",
						CharsLimit = 32,
						NeedsKeyboard = true,
						Text = $"{amount}"
					},
					new CuiRectTransformComponent
					{
						AnchorMin = "0 0", AnchorMax = "1 1"
					}
				}
			});

			#endregion

			#region Pages

			container.Add(new CuiButton
			{
				RectTransform =
				{
					AnchorMin = "0 0.5", AnchorMax = "0 0.5",
					OffsetMin = "415 -17.5", OffsetMax = "450 17.5"
				},
				Text =
				{
					Text = Msg(player, BackPage),
					Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-regular.ttf",
					FontSize = 12,
					Color = "1 1 1 1"
				},
				Button =
				{
					Color = Color4,
					Command = page != 0 ? $"UI_Clans selectpages {slot} {page - 1} {amount} {search}" : ""
				}
			}, Layer + ".Header");

			container.Add(new CuiButton
			{
				RectTransform =
				{
					AnchorMin = "0 0.5", AnchorMax = "0 0.5",
					OffsetMin = "455 -17.5", OffsetMax = "490 17.5"
				},
				Text =
				{
					Text = Msg(player, NextPage),
					Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-regular.ttf",
					FontSize = 12,
					Color = "1 1 1 1"
				},
				Button =
				{
					Color = Color2,
					Command = itemsList.Count > (page + 1) * totalAmount
						? $"UI_Clans selectpages {slot} {page + 1} {amount} {search}"
						: ""
				}
			}, Layer + ".Header");

			#endregion

			#region Close

			container.Add(new CuiButton
			{
				RectTransform =
				{
					AnchorMin = "1 0.5", AnchorMax = "1 0.5",
					OffsetMin = "-35 -12.5",
					OffsetMax = "-10 12.5"
				},
				Text =
				{
					Text = Msg(player, CloseTitle),
					Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-bold.ttf",
					FontSize = 10,
					Color = "1 1 1 1"
				},
				Button =
				{
					Color = Color2,
					Command = "UI_Clans page 4"
				}
			}, Layer + ".Header");

			#endregion

			#endregion

			#region Items

			var i = 1;
			foreach (var def in itemsList
				         .Skip(page * totalAmount)
				         .Take(totalAmount))
			{
				container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0.5 1", AnchorMax = "0.5 1",
						OffsetMin = $"{xSwitch} {ySwitch - Height}",
						OffsetMax = $"{xSwitch + Width} {ySwitch}"
					},
					Image =
					{
						Color = clan.ResourceStandarts.ContainsKey(slot) &&
						        clan.ResourceStandarts[slot].ShortName == def.shortname
							? Color4
							: Color3
					}
				}, Layer + ".Main", Layer + $".Item.{i}");

				container.Add(new CuiElement
				{
					Parent = Layer + $".Item.{i}",
					Components =
					{
						new CuiImageComponent
						{
							ItemId = def.itemid
						},
						new CuiRectTransformComponent
						{
							AnchorMin = "0.5 1", AnchorMax = "0.5 1",
							OffsetMin = "-35 -80", OffsetMax = "35 -10"
						}
					}
				});

				container.Add(new CuiButton
				{
					RectTransform =
					{
						AnchorMin = "0 0", AnchorMax = "1 0",
						OffsetMin = "0 0", OffsetMax = "0 25"
					},
					Text =
					{
						Text = Msg(player, SelectTitle),
						Align = TextAnchor.MiddleCenter,
						Font = "robotocondensed-regular.ttf",
						FontSize = 10,
						Color = "1 1 1 1"
					},
					Button =
					{
						Color = Color2,
						Command = $"UI_Clans selectitem {slot} {def.shortname} {amount}"
					}
				}, Layer + $".Item.{i}");

				if (i % amountOnString == 0)
				{
					xSwitch = constSwitchX;
					ySwitch = ySwitch - Height - Margin;
				}
				else
				{
					xSwitch += Width + Margin;
				}

				i++;
			}

			#endregion

			#endregion

			CuiHelper.DestroyUi(player, Layer + ".Main");
			CuiHelper.AddUi(player, container);
		}

		private void CreateClanUi(BasePlayer player)
		{
			if (!ClanCreating.ContainsKey(player)) ClanCreating.Add(player, new CreateClanData());

			var clanTag = ClanCreating[player].Tag;
			var avatar = ClanCreating[player].Avatar;

			var container = new CuiElementContainer();

			#region Background

			container.Add(new CuiPanel
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
				Image =
				{
					Color = "0 0 0 0.9",
					Material = "assets/content/ui/uibackgroundblur.mat"
				},
				CursorEnabled = true
			}, "Overlay", Layer);

			container.Add(new CuiButton
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
				Text = {Text = ""},
				Button =
				{
					Color = "0 0 0 0",
					Close = !_config.ForceClanCreateTeam ? Layer : ""
				}
			}, Layer);

			#endregion

			#region Main

			container.Add(new CuiPanel
			{
				RectTransform =
				{
					AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5",
					OffsetMin = "-340 -215",
					OffsetMax = "340 220"
				},
				Image =
				{
					Color = Color1
				}
			}, Layer, Layer + ".Main");

			#region Header

			HeaderUi(ref container, player, null, 0, Msg(player, ClanCreationTitle),
				showClose: !_config.ForceClanCreateTeam);

			#endregion

			#region Name

			container.Add(new CuiPanel
			{
				RectTransform =
				{
					AnchorMin = "0.5 1", AnchorMax = "0.5 1",
					OffsetMin = "-150 -140", OffsetMax = "150 -110"
				},
				Image =
				{
					Color = Color3
				}
			}, Layer + ".Main", Layer + ".Clan.Creation.Name");

			container.Add(new CuiLabel
			{
				RectTransform =
				{
					AnchorMin = "0 1", AnchorMax = "1 1",
					OffsetMin = "0 0", OffsetMax = "0 20"
				},
				Text =
				{
					Text = Msg(player, ClanNameTitle),
					Align = TextAnchor.MiddleLeft,
					Font = "robotocondensed-bold.ttf",
					FontSize = 12,
					Color = "1 1 1 1"
				}
			}, Layer + ".Clan.Creation.Name");

			container.Add(new CuiElement
			{
				Parent = Layer + ".Clan.Creation.Name",
				Components =
				{
					new CuiInputFieldComponent
					{
						FontSize = 10,
						Align = TextAnchor.MiddleLeft,
						Font = "robotocondensed-regular.ttf",
						Command = "UI_Clans createclan name ",
						Color = "1 1 1 0.8",
						CharsLimit = _config.Tags.TagMax,
						NeedsKeyboard = true,
						Text = string.IsNullOrEmpty(clanTag) ? string.Empty : $"{clanTag}"
					},
					new CuiRectTransformComponent
						{AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5"}
				}
			});

			#endregion

			#region Avatar

			container.Add(new CuiPanel
			{
				RectTransform =
				{
					AnchorMin = "0.5 1", AnchorMax = "0.5 1",
					OffsetMin = "-150 -210", OffsetMax = "150 -180"
				},
				Image =
				{
					Color = Color3
				}
			}, Layer + ".Main", Layer + ".Clan.Creation.Avatar");

			container.Add(new CuiLabel
			{
				RectTransform =
				{
					AnchorMin = "0 1", AnchorMax = "1 1",
					OffsetMin = "0 0", OffsetMax = "0 20"
				},
				Text =
				{
					Text = Msg(player, AvatarTitle),
					Align = TextAnchor.MiddleLeft,
					Font = "robotocondensed-bold.ttf",
					FontSize = 12,
					Color = "1 1 1 1"
				}
			}, Layer + ".Clan.Creation.Avatar");

			container.Add(new CuiElement
			{
				Parent = Layer + ".Clan.Creation.Avatar",
				Components =
				{
					new CuiInputFieldComponent
					{
						FontSize = 10,
						Align = TextAnchor.MiddleLeft,
						Font = "robotocondensed-regular.ttf",
						Command = "UI_Clans createclan avatar ",
						Color = "1 1 1 0.8",
						CharsLimit = 128,
						NeedsKeyboard = true,
						Text = string.IsNullOrEmpty(avatar) ? Msg(player, UrlTitle) : $"{avatar}"
					},
					new CuiRectTransformComponent
						{AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5"}
				}
			});

			#endregion

			#region Create Clan

			container.Add(new CuiButton
			{
				RectTransform =
				{
					AnchorMin = "0.5 1", AnchorMax = "0.5 1",
					OffsetMin = "-75 -295", OffsetMax = "75 -270"
				},
				Text =
				{
					Text = Msg(player, CreateTitle),
					Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-regular.ttf",
					FontSize = 12,
					Color = "1 1 1 1"
				},
				Button =
				{
					Color = Color2,
					Command = "UI_Clans createclan create",
					Close = Layer
				}
			}, Layer + ".Main");

			#endregion

			#endregion

			CuiHelper.DestroyUi(player, Layer);
			CuiHelper.AddUi(player, container);
		}

		private void HeaderUi(ref CuiElementContainer container, BasePlayer player, ClanData clan, int page,
			string headTitle,
			string backPage = "",
			bool showClose = true)
		{
			container.Add(new CuiPanel
			{
				RectTransform =
				{
					AnchorMin = "0 1", AnchorMax = "1 1",
					OffsetMin = "0 -45", OffsetMax = "0 0"
				},
				Image =
				{
					Color = Color3
				}
			}, Layer + ".Main", Layer + ".Header");

			#region Title

			container.Add(new CuiLabel
			{
				RectTransform =
				{
					AnchorMin = "0 0", AnchorMax = "1 1",
					OffsetMin = "12.5 0", OffsetMax = "0 0"
				},
				Text =
				{
					Text = $"{headTitle}",
					Align = TextAnchor.MiddleLeft,
					Font = "robotocondensed-bold.ttf",
					FontSize = 14,
					Color = "1 1 1 1"
				}
			}, Layer + ".Header");

			#endregion

			#region Close

			if (showClose)
				container.Add(new CuiButton
				{
					RectTransform =
					{
						AnchorMin = "1 1", AnchorMax = "1 1",
						OffsetMin = "-35 -37.5",
						OffsetMax = "-10 -12.5"
					},
					Text =
					{
						Text = Msg(player, CloseTitle),
						Align = TextAnchor.MiddleCenter,
						Font = "robotocondensed-bold.ttf",
						FontSize = 10,
						Color = "1 1 1 1"
					},
					Button =
					{
						Close = Layer,
						Color = Color2
					}
				}, Layer + ".Header");

			#endregion

			#region Back

			var hasBack = !string.IsNullOrEmpty(backPage);

			if (hasBack)
				container.Add(new CuiButton
				{
					RectTransform =
					{
						AnchorMin = "1 1", AnchorMax = "1 1",
						OffsetMin = "-65 -37.5",
						OffsetMax = "-40 -12.5"
					},
					Text =
					{
						Text = Msg(player, BackPage),
						Align = TextAnchor.MiddleCenter,
						Font = "robotocondensed-regular.ttf",
						FontSize = 12,
						Color = "1 1 1 1"
					},
					Button =
					{
						Color = Color2,
						Command = $"{backPage}"
					}
				}, Layer + ".Header");

			#endregion

			#region Invites

			if (clan != null && clan.IsModerator(player.userID))
			{
				if (page == 65 || page == 71 || page == 72)
				{
					if (_config.AllianceSettings.Enabled)
					{
						container.Add(new CuiButton
						{
							RectTransform =
							{
								AnchorMin = "1 1", AnchorMax = "1 1",
								OffsetMin = "-470 -37.5",
								OffsetMax = "-330 -12.5"
							},
							Text =
							{
								Text = Msg(player, AllyInvites),
								Align = TextAnchor.MiddleCenter,
								Font = "robotocondensed-regular.ttf",
								FontSize = 12,
								Color = "1 1 1 1"
							},
							Button =
							{
								Color = page == 71 ? Color2 : Color4,
								Command = "UI_Clans page 71"
							}
						}, Layer + ".Header");

						container.Add(new CuiButton
						{
							RectTransform =
							{
								AnchorMin = "1 1", AnchorMax = "1 1",
								OffsetMin = "-325 -37.5",
								OffsetMax = "-185 -12.5"
							},
							Text =
							{
								Text = Msg(player, IncomingAllyTitle),
								Align = TextAnchor.MiddleCenter,
								Font = "robotocondensed-regular.ttf",
								FontSize = 12,
								Color = "1 1 1 1"
							},
							Button =
							{
								Color = page == 72 ? Color2 : Color4,
								Command = "UI_Clans page 72"
							}
						}, Layer + ".Header");
					}

					container.Add(new CuiButton
					{
						RectTransform =
						{
							AnchorMin = "1 1", AnchorMax = "1 1",
							OffsetMin = "-180 -37.5",
							OffsetMax = "-40 -12.5"
						},
						Text =
						{
							Text = Msg(player, ClanInvitesTitle),
							Align = TextAnchor.MiddleCenter,
							Font = "robotocondensed-regular.ttf",
							FontSize = 12,
							Color = "1 1 1 1"
						},
						Button =
						{
							Color = page == 65 ? Color2 : Color4,
							Command = "UI_Clans page 65"
						}
					}, Layer + ".Header");
				}
				else
				{
					container.Add(new CuiButton
					{
						RectTransform =
						{
							AnchorMin = "1 1", AnchorMax = "1 1",
							OffsetMin = $"{(hasBack ? -220 : -180)} -37.5",
							OffsetMax = $"{(hasBack ? -70 : -40)} -12.5"
						},
						Text =
						{
							Text = Msg(player, InvitesTitle),
							Align = TextAnchor.MiddleCenter,
							Font = "robotocondensed-regular.ttf",
							FontSize = 12,
							Color = "1 1 1 1"
						},
						Button =
						{
							Color = Color4,
							Command = "UI_Clans page 65"
						}
					}, Layer + ".Header");
				}
			}

			#endregion

			#region Notify

			if (HasInvite(player))
			{
				container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "1 1", AnchorMax = "1 1",
						OffsetMin = "-205 -37.5",
						OffsetMax = "-40 -12.5"
					},
					Image =
					{
						Color = Color2
					}
				}, Layer + ".Header", Layer + ".Header.Invite");

				container.Add(new CuiLabel
				{
					RectTransform =
					{
						AnchorMin = "0 0", AnchorMax = "1 1",
						OffsetMin = "5 0", OffsetMax = "-5 0"
					},
					Text =
					{
						Text = Msg(player, InvitedToClan),
						Align = TextAnchor.MiddleLeft,
						Font = "robotocondensed-regular.ttf",
						FontSize = 12,
						Color = "1 1 1 1"
					}
				}, Layer + ".Header.Invite");

				container.Add(new CuiLabel
				{
					RectTransform =
					{
						AnchorMin = "0 0", AnchorMax = "1 1",
						OffsetMin = "5 0", OffsetMax = "-5 0"
					},
					Text =
					{
						Text = Msg(player, NextPage),
						Align = TextAnchor.MiddleRight,
						Font = "robotocondensed-regular.ttf",
						FontSize = 12,
						Color = "1 1 1 1"
					}
				}, Layer + ".Header.Invite");

				container.Add(new CuiButton
				{
					RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
					Text = {Text = ""},
					Button =
					{
						Color = "0 0 0 0",
						Command = "UI_Clans page 45"
					}
				}, Layer + ".Header.Invite");
			}

			#endregion

			CuiHelper.DestroyUi(player, Layer + ".Header");
		}

		private void ClanMemberProfileUi(BasePlayer player, ulong target)
		{
			#region Fields

			var data = PlayerData.GetOrCreate(target.ToString());

			var clan = data?.GetClan();
			if (clan == null) return;

			#endregion

			var container = new CuiElementContainer();

			#region Background

			container.Add(new CuiPanel
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
				Image = {Color = "0 0 0 0"}
			}, Layer + ".Second.Main", Layer + ".Content");

			#endregion

			#region Header

			HeaderUi(ref container, player, clan, 1, Msg(player, ClansMenuTitle), "UI_Clans page 1");

			#endregion

			#region Title

			container.Add(new CuiLabel
			{
				RectTransform =
				{
					AnchorMin = "0 1", AnchorMax = "0 1",
					OffsetMin = "2.5 -30", OffsetMax = "225 0"
				},
				Text =
				{
					Text = Msg(player, ProfileTitle),
					Align = TextAnchor.MiddleLeft,
					Font = "robotocondensed-regular.ttf",
					FontSize = 12,
					Color = "1 1 1 1"
				}
			}, Layer + ".Content");

			#endregion

			#region Avatar

			container.Add(new CuiElement
			{
				Parent = Layer + ".Content",
				Components =
				{
					new CuiRawImageComponent {Png = ImageLibrary.Call<string>("GetImage", $"avatar_{target}")},
					new CuiRectTransformComponent
					{
						AnchorMin = "0 1", AnchorMax = "0 1",
						OffsetMin = "0 -170", OffsetMax = "140 -30"
					}
				}
			});

			#endregion

			#region Name

			container.Add(new CuiLabel
			{
				RectTransform =
				{
					AnchorMin = "0 1", AnchorMax = "0 1",
					OffsetMin = "160 -50", OffsetMax = "400 -30"
				},
				Text =
				{
					Text = $"{data.DisplayName}",
					Align = TextAnchor.MiddleLeft,
					Font = "robotocondensed-bold.ttf",
					FontSize = 16,
					Color = "1 1 1 1"
				}
			}, Layer + ".Content");

			#endregion

			#region Fields

			var ySwitch = -45f;
			var xSwitch = 0f;
			var maxWidth = 0f;
			var height = 30f;
			var widthMargin = 10f;
			var heightMargin = 20f;

			for (var i = 0; i < _config.UI.ClanMemberProfileFields.Count; i++)
			{
				var field = _config.UI.ClanMemberProfileFields[i];

				if (maxWidth == 0 || maxWidth < field.Width)
				{
					ySwitch = ySwitch - height - heightMargin;

					var hasAvatar = ySwitch < -30 && ySwitch > -170f;

					maxWidth = hasAvatar ? 300f : 460f;
					xSwitch = hasAvatar ? 160f : 0f;
				}

				container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0 1", AnchorMax = "0 1",
						OffsetMin = $"{xSwitch} {ySwitch - height}",
						OffsetMax = $"{xSwitch + field.Width} {ySwitch}"
					},
					Image =
					{
						Color = Color3
					}
				}, Layer + ".Content", Layer + $".Content.{i}");

				if (field.Key == "gather")
				{
					var progress = data.GetTotalFarm(clan);

					if (progress > 0)
						container.Add(new CuiPanel
						{
							RectTransform = {AnchorMin = "0 0", AnchorMax = $"{progress} 0.9"},
							Image =
							{
								Color = Color2
							}
						}, Layer + $".Content.{i}", Layer + $".Content.{i}.Progress");
				}

				container.Add(new CuiLabel
				{
					RectTransform =
					{
						AnchorMin = "0 1", AnchorMax = "1 1",
						OffsetMin = "0 0", OffsetMax = "0 20"
					},
					Text =
					{
						Text = Msg(player, field.LangKey),
						Align = TextAnchor.MiddleLeft,
						Font = "robotocondensed-regular.ttf",
						FontSize = 10,
						Color = "1 1 1 1"
					}
				}, Layer + $".Content.{i}");

				container.Add(new CuiLabel
				{
					RectTransform =
					{
						AnchorMin = "0 0", AnchorMax = "1 1",
						OffsetMin = "0 0", OffsetMax = "-5 0"
					},
					Text =
					{
						Text = $"{field.GetFormat(0, data.GetParams(field.Key, clan))}",
						Align = field.TextAlign,
						Font = "robotocondensed-bold.ttf",
						FontSize = field.FontSize,
						Color = "1 1 1 1"
					}
				}, Layer + $".Content.{i}");

				xSwitch += field.Width + widthMargin;

				maxWidth -= field.Width;
			}

			#endregion

			#region Owner Buttons

			if (clan.IsOwner(player.userID))
			{
				var width = 70f;
				height = 20f;
				var margin = 5f;

				xSwitch = 460f;
				ySwitch = 0;

				var isModerator = clan.IsModerator(target);

				if (player.userID != target)
				{
					container.Add(new CuiButton
					{
						RectTransform =
						{
							AnchorMin = "0 1", AnchorMax = "0 1",
							OffsetMin = $"{xSwitch - width} {ySwitch - height}",
							OffsetMax = $"{xSwitch} {ySwitch}"
						},
						Text =
						{
							Text = Msg(player, KickTitle),
							Align = TextAnchor.MiddleCenter,
							Font = "robotocondensed-regular.ttf",
							FontSize = 10,
							Color = "1 1 1 1"
						},
						Button =
						{
							Color = Color6,
							Command = player.userID != target ? $"UI_Clans kick {target}" : ""
						}
					}, Layer + ".Content");

					xSwitch = xSwitch - width - margin;
				}

				container.Add(new CuiButton
				{
					RectTransform =
					{
						AnchorMin = "0 1", AnchorMax = "0 1",
						OffsetMin = $"{xSwitch - width} {ySwitch - height}",
						OffsetMax = $"{xSwitch} {ySwitch}"
					},
					Text =
					{
						Text = Msg(player, PromoteLeaderTitle),
						Align = TextAnchor.MiddleCenter,
						Font = "robotocondensed-regular.ttf",
						FontSize = 10,
						Color = "1 1 1 1"
					},
					Button =
					{
						Color = Color4,
						Command = $"UI_Clans leader tryset {target}"
					}
				}, Layer + ".Content");

				xSwitch = xSwitch - width - margin;

				container.Add(new CuiButton
				{
					RectTransform =
					{
						AnchorMin = "0 1", AnchorMax = "0 1",
						OffsetMin = $"{xSwitch - width} {ySwitch - height}",
						OffsetMax = $"{xSwitch} {ySwitch}"
					},
					Text =
					{
						Text = isModerator ? Msg(player, DemoteModerTitle) : Msg(player, PromoteModerTitle),
						Align = TextAnchor.MiddleCenter,
						Font = "robotocondensed-regular.ttf",
						FontSize = 10,
						Color = "1 1 1 1"
					},
					Button =
					{
						Color = isModerator ? Color4 : Color2,
						Command = isModerator ? $"UI_Clans moder undo {target}" : $"UI_Clans moder set {target}"
					}
				}, Layer + ".Content");
			}

			_config.UI?.ProfileButtons?.ForEach(btn => btn?.Get(ref container, target, Layer + ".Content", Layer));

			#endregion

			#region Farm

			if (clan.ResourceStandarts.Count > 0)
			{
				#region Title

				container.Add(new CuiLabel
				{
					RectTransform =
					{
						AnchorMin = "0 1", AnchorMax = "0 1",
						OffsetMin = "2.5 -200", OffsetMax = "225 -185"
					},
					Text =
					{
						Text = Msg(player, GatherRatesTitle),
						Align = TextAnchor.MiddleLeft,
						Font = "robotocondensed-regular.ttf",
						FontSize = 10,
						Color = "1 1 1 1"
					}
				}, Layer + ".Content");

				#endregion

				ySwitch = -205f;
				var amountOnString = 6;

				xSwitch = 0f;
				var Height = 75f;
				var Width = 75f;
				var Margin = 5f;

				var z = 1;
				foreach (var standart in clan.ResourceStandarts)
				{
					container.Add(new CuiPanel
					{
						RectTransform =
						{
							AnchorMin = "0 1", AnchorMax = "0 1",
							OffsetMin = $"{xSwitch} {ySwitch - Height}",
							OffsetMax = $"{xSwitch + Width} {ySwitch}"
						},
						Image =
						{
							Color = Color3
						}
					}, Layer + ".Content", Layer + $".Standarts.{z}");

					container.Add(standart.Value.GetImage("0.5 1", "0.5 1", "-20 -45", "20 -5",
						Layer + $".Standarts.{z}"));

					#region Progress

					var one = data.GetValue(standart.Value.ShortName);
					var two = standart.Value.Amount;

					var progress = one / two;

					container.Add(new CuiPanel
					{
						RectTransform =
						{
							AnchorMin = "0 0", AnchorMax = "1 0",
							OffsetMin = "0 0", OffsetMax = "0 5"
						},
						Image =
						{
							Color = Color4
						}
					}, Layer + $".Standarts.{z}", Layer + $".Standarts.{z}.Progress");

					if (progress > 0)
						container.Add(new CuiPanel
						{
							RectTransform = {AnchorMin = "0 0", AnchorMax = $"{progress} 0", OffsetMax = "0 5"},
							Image =
							{
								Color = Color2
							}
						}, Layer + $".Standarts.{z}");

					container.Add(new CuiLabel
					{
						RectTransform =
						{
							AnchorMin = "0 0", AnchorMax = "1 0",
							OffsetMin = "0 0", OffsetMax = "0 20"
						},
						Text =
						{
							Text = $"{one}/<b>{two}</b>",
							Align = TextAnchor.UpperCenter,
							Font = "robotocondensed-regular.ttf",
							FontSize = 10,
							Color = "1 1 1 1"
						}
					}, Layer + $".Standarts.{z}");

					#endregion

					if (z % amountOnString == 0)
					{
						xSwitch = 0;
						ySwitch = ySwitch - Margin - Height;
					}
					else
					{
						xSwitch += Margin + Width;
					}

					z++;
				}
			}

			#endregion

			CuiHelper.DestroyUi(player, Layer + ".Content");
			CuiHelper.AddUi(player, container);
		}

		private void ProfileUi(BasePlayer player, ulong target)
		{
			var data = GetTopDataById(target);
			if (data == null) return;

			var container = new CuiElementContainer();

			var clan = FindClanByPlayer(player.UserIDString);

			#region Menu

			if (player.userID == target) MenuUi(ref container, player, 3, clan);

			#endregion

			#region Background

			container.Add(new CuiPanel
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
				Image = {Color = "0 0 0 0"}
			}, Layer + ".Second.Main", Layer + ".Content");

			#endregion

			#region Header

			HeaderUi(ref container, player, clan, 3, Msg(player, ClansMenuTitle), "UI_Clans page 3");

			#endregion

			#region Title

			container.Add(new CuiLabel
			{
				RectTransform =
				{
					AnchorMin = "0 1", AnchorMax = "0 1",
					OffsetMin = "2.5 -30", OffsetMax = "225 0"
				},
				Text =
				{
					Text = Msg(player, ProfileTitle),
					Align = TextAnchor.MiddleLeft,
					Font = "robotocondensed-regular.ttf",
					FontSize = 12,
					Color = "1 1 1 1"
				}
			}, Layer + ".Content");

			#endregion

			#region Avatar

			container.Add(new CuiElement
			{
				Parent = Layer + ".Content",
				Components =
				{
					new CuiRawImageComponent {Png = ImageLibrary.Call<string>("GetImage", $"avatar_{target}")},
					new CuiRectTransformComponent
					{
						AnchorMin = "0 1", AnchorMax = "0 1",
						OffsetMin = "0 -170", OffsetMax = "140 -30"
					}
				}
			});

			#endregion

			#region Name

			container.Add(new CuiLabel
			{
				RectTransform =
				{
					AnchorMin = "0 1", AnchorMax = "0 1",
					OffsetMin = "160 -50", OffsetMax = "400 -30"
				},
				Text =
				{
					Text = $"{data.Data.DisplayName}",
					Align = TextAnchor.MiddleLeft,
					Font = "robotocondensed-bold.ttf",
					FontSize = 16,
					Color = "1 1 1 1"
				}
			}, Layer + ".Content");

			#endregion

			#region Fields

			var ySwitch = -45f;
			var xSwitch = 0f;
			var maxWidth = 0f;
			var height = 30f;
			var widthMargin = 20f;
			var heightMargin = 20f;

			for (var i = 0; i < _config.UI.TopPlayerProfileFields.Count; i++)
			{
				var field = _config.UI.TopPlayerProfileFields[i];

				if (maxWidth == 0 || maxWidth < field.Width)
				{
					ySwitch = ySwitch - height - heightMargin;

					var hasAvatar = ySwitch < -30 && ySwitch > -170f;

					maxWidth = hasAvatar ? 300f : 460f;
					xSwitch = hasAvatar ? 160f : 0f;
				}

				container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0 1", AnchorMax = "0 1",
						OffsetMin = $"{xSwitch} {ySwitch - height}",
						OffsetMax = $"{xSwitch + field.Width} {ySwitch}"
					},
					Image =
					{
						Color = Color3
					}
				}, Layer + ".Content", Layer + $".Content.{i}");

				container.Add(new CuiLabel
				{
					RectTransform =
					{
						AnchorMin = "0 1", AnchorMax = "1 1",
						OffsetMin = "0 0", OffsetMax = "0 20"
					},
					Text =
					{
						Text = Msg(player, field.LangKey),
						Align = TextAnchor.MiddleLeft,
						Font = "robotocondensed-regular.ttf",
						FontSize = 10,
						Color = "1 1 1 1"
					}
				}, Layer + $".Content.{i}");

				container.Add(new CuiLabel
				{
					RectTransform =
					{
						AnchorMin = "0 0", AnchorMax = "1 1",
						OffsetMin = "0 0", OffsetMax = "-5 0"
					},
					Text =
					{
						Text =
							field.Key == "clanname" ? $"{FindClanByPlayer(target.ToString())?.ClanTag}" :
							field.Key == "rating" ? $"{data.Top}" :
							$"{field.GetFormat(0, data.Data.GetParams(field.Key, clan))}",
						Align = field.TextAlign,
						Font = "robotocondensed-bold.ttf",
						FontSize = field.FontSize,
						Color = "1 1 1 1"
					}
				}, Layer + $".Content.{i}");

				xSwitch += field.Width + widthMargin;

				maxWidth -= field.Width;
			}

			#endregion

			CuiHelper.DestroyUi(player, Layer + ".Content");
			CuiHelper.AddUi(player, container);
		}

		private void MenuUi(ref CuiElementContainer container, BasePlayer player, int page, ClanData clan = null)
		{
			var data = PlayerData.GetOrCreate(player.UserIDString);

			container.Add(new CuiPanel
			{
				RectTransform =
				{
					AnchorMin = "0 0", AnchorMax = "0 0",
					OffsetMin = "10 10",
					OffsetMax = "185 380"
				},
				Image =
				{
					Color = Color3
				}
			}, Layer + ".Main", Layer + ".Menu");

			#region Pages

			var ySwitch = 0f;
			var Height = 35f;
			var Margin = 0f;

			foreach (var pageSettings in _config.Pages)
			{
				if (!pageSettings.Enabled || (!string.IsNullOrEmpty(pageSettings.Permission) &&
				                              !player.HasPermission(pageSettings.Permission)))
					continue;

				if (clan == null)
					switch (pageSettings.ID)
					{
						case 2:
						case 3:
							break;
						default:
							continue;
					}

				switch (pageSettings.ID)
				{
					case 5:
						if (clan != null && !clan.IsModerator(player.userID)) continue;
						break;
					case 7:
						if (!_config.AllianceSettings.Enabled) continue;
						break;
				}

				container.Add(new CuiButton
				{
					RectTransform =
					{
						AnchorMin = "0 1", AnchorMax = "1 1",
						OffsetMin = $"0 {ySwitch - Height}",
						OffsetMax = $"0 {ySwitch}"
					},
					Text =
					{
						Text = $"     {Msg(player, pageSettings.Key)}",
						Align = TextAnchor.MiddleLeft,
						Font = "robotocondensed-regular.ttf",
						FontSize = 12,
						Color = "1 1 1 1"
					},
					Button =
					{
						Color = pageSettings.ID == page ? HexToCuiColor(_config.UI.Color2, 33) : "0 0 0 0",
						Command = $"UI_Clans page {pageSettings.ID}"
					}
				}, Layer + ".Menu", Layer + $".Menu.Page.{pageSettings.Key}");

				if (pageSettings.ID == page)
					container.Add(new CuiPanel
					{
						RectTransform =
						{
							AnchorMin = "0 0", AnchorMax = "0 1",
							OffsetMin = "0 0", OffsetMax = "5 0"
						},
						Image =
						{
							Color = Color2
						}
					}, Layer + $".Menu.Page.{pageSettings.Key}");

				ySwitch = ySwitch - Height - Margin;
			}

			#endregion

			#region Notify

			if (clan == null)
			{
				container.Add(new CuiButton
				{
					RectTransform =
					{
						AnchorMin = "0.5 0", AnchorMax = "0.5 0",
						OffsetMin = "-75 10", OffsetMax = "75 40"
					},
					Text =
					{
						Text = Msg(player, CreateClanTitle),
						Align = TextAnchor.MiddleCenter,
						Font = "robotocondensed-regular.ttf",
						FontSize = 12,
						Color = "1 1 1 1"
					},
					Button =
					{
						Color = Color2,
						Command = "UI_Clans createclan"
					}
				}, Layer + ".Menu");
			}
			else
			{
				container.Add(new CuiButton
				{
					RectTransform =
					{
						AnchorMin = "0.5 0", AnchorMax = "0.5 0",
						OffsetMin = "-75 10", OffsetMax = "75 40"
					},
					Text =
					{
						Text = Msg(player, ProfileTitle),
						Align = TextAnchor.MiddleCenter,
						Font = "robotocondensed-regular.ttf",
						FontSize = 12,
						Color = "1 1 1 1"
					},
					Button =
					{
						Color = Color2,
						Command = $"UI_Clans showprofile {player.userID}"
					}
				}, Layer + ".Menu");

				if (_config.UseFriendlyFire)
				{
					if (!_config.GeneralFriendlyFire || _config.PlayersGeneralFF ||
					    (_config.ModersGeneralFF && clan.IsModerator(player.userID)) ||
					    clan.IsOwner(player.userID)) ButtonFriendlyFire(ref container, player, data);

					if (_config.AllianceSettings.Enabled && _config.AllianceSettings.UseFF &&
					    (!_config.AllianceSettings.GeneralFriendlyFire || _config.AllianceSettings.PlayersGeneralFF ||
					     (_config.AllianceSettings.ModersGeneralFF && clan.IsModerator(player.userID)) ||
					     clan.IsOwner(player.userID)))
						ButtonAlly(ref container, player, data);
				}
			}

			#endregion

			CuiHelper.DestroyUi(player, Layer + ".Menu");
		}

		private void AcceptSetLeader(BasePlayer player, ulong target)
		{
			var container = new CuiElementContainer
			{
				{
					new CuiPanel
					{
						RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
						Image = {Color = HexToCuiColor(_config.UI.Color1, 99)}
					},
					"Overlay", ModalLayer
				},
				{
					new CuiLabel
					{
						RectTransform =
						{
							AnchorMin = "0.5 0.5",
							AnchorMax = "0.5 0.5",
							OffsetMin = "-70 40",
							OffsetMax = "70 60"
						},
						Text =
						{
							Text = Msg(player, LeaderTransferTitle),
							Align = TextAnchor.MiddleCenter,
							Font = "robotocondensed-bold.ttf",
							FontSize = 12,
							Color = "1 1 1 1"
						}
					},
					ModalLayer
				},
				{
					new CuiButton
					{
						RectTransform =
						{
							AnchorMin = "0.5 0.5",
							AnchorMax = "0.5 0.5",
							OffsetMin = "-70 10",
							OffsetMax = "70 40"
						},
						Text =
						{
							Text = Msg(player, AcceptTitle),
							Align = TextAnchor.MiddleCenter,
							Font = "robotocondensed-regular.ttf",
							FontSize = 10,
							Color = "1 1 1 1"
						},
						Button =
						{
							Color = Color2,
							Command = $"UI_Clans leader set {target}",
							Close = ModalLayer
						}
					},
					ModalLayer
				},
				{
					new CuiButton
					{
						RectTransform =
						{
							AnchorMin = "0.5 0.5",
							AnchorMax = "0.5 0.5",
							OffsetMin = "-70 -22.5",
							OffsetMax = "70 7.5"
						},
						Text =
						{
							Text = Msg(player, CancelTitle),
							Align = TextAnchor.MiddleCenter,
							Font = "robotocondensed-regular.ttf",
							FontSize = 10,
							Color = "1 1 1 1"
						},
						Button = {Color = HexToCuiColor(_config.UI.Color2, 33), Close = ModalLayer}
					},
					ModalLayer
				}
			};


			CuiHelper.DestroyUi(player, ModalLayer);
			CuiHelper.AddUi(player, container);
		}

		private void ClanProfileUi(BasePlayer player, string clanTag)
		{
			var clan = FindClanByTag(clanTag);
			if (clan == null) return;

			var playerClan = FindClanByPlayer(player.UserIDString);

			var container = new CuiElementContainer();

			#region Menu

			MenuUi(ref container, player, 2, playerClan);

			#endregion

			#region Background

			container.Add(new CuiPanel
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
				Image = {Color = "0 0 0 0"}
			}, Layer + ".Second.Main", Layer + ".Content");

			#endregion

			#region Header

			HeaderUi(ref container, player, playerClan, 2, Msg(player, ClansMenuTitle), "UI_Clans page 2");

			#endregion

			#region Title

			container.Add(new CuiLabel
			{
				RectTransform =
				{
					AnchorMin = "0 1", AnchorMax = "0 1",
					OffsetMin = "2.5 -30", OffsetMax = "225 0"
				},
				Text =
				{
					Text = Msg(player, AboutClan),
					Align = TextAnchor.MiddleLeft,
					Font = "robotocondensed-regular.ttf",
					FontSize = 12,
					Color = "1 1 1 1"
				}
			}, Layer + ".Content");

			#endregion

			#region Avatar

			container.Add(new CuiElement
			{
				Parent = Layer + ".Content",
				Components =
				{
					new CuiRawImageComponent {Png = ImageLibrary.Call<string>("GetImage", clan.Avatar)},
					new CuiRectTransformComponent
					{
						AnchorMin = "0 1", AnchorMax = "0 1",
						OffsetMin = "0 -170", OffsetMax = "140 -30"
					}
				}
			});

			#endregion

			#region Clan Name

			container.Add(new CuiLabel
			{
				RectTransform =
				{
					AnchorMin = "0 1", AnchorMax = "0 1",
					OffsetMin = "160 -50", OffsetMax = "400 -30"
				},
				Text =
				{
					Text = $"{clan.ClanTag}",
					Align = TextAnchor.MiddleLeft,
					Font = "robotocondensed-bold.ttf",
					FontSize = 16,
					Color = "1 1 1 1"
				}
			}, Layer + ".Content");

			#endregion

			#region Clan Leader

			container.Add(new CuiPanel
			{
				RectTransform =
				{
					AnchorMin = "0 1", AnchorMax = "0 1",
					OffsetMin = "160 -105",
					OffsetMax = "460 -75"
				},
				Image =
				{
					Color = Color3
				}
			}, Layer + ".Content", Layer + ".Clan.Leader");

			container.Add(new CuiLabel
			{
				RectTransform =
				{
					AnchorMin = "0 1", AnchorMax = "1 1",
					OffsetMin = "0 0", OffsetMax = "0 20"
				},
				Text =
				{
					Text = Msg(player, LeaderTitle),
					Align = TextAnchor.MiddleLeft,
					Font = "robotocondensed-regular.ttf",
					FontSize = 10,
					Color = "1 1 1 1"
				}
			}, Layer + ".Clan.Leader");

			container.Add(new CuiLabel
			{
				RectTransform =
				{
					AnchorMin = "0 0", AnchorMax = "1 1",
					OffsetMin = "10 0", OffsetMax = "0 0"
				},
				Text =
				{
					Text = $"{clan.LeaderName}",
					Align = TextAnchor.MiddleLeft,
					Font = "robotocondensed-bold.ttf",
					FontSize = 12,
					Color = "1 1 1 1"
				}
			}, Layer + ".Clan.Leader");

			#endregion

			#region Rating

			container.Add(new CuiPanel
			{
				RectTransform =
				{
					AnchorMin = "0 1", AnchorMax = "0 1",
					OffsetMin = "160 -165", OffsetMax = "300 -135"
				},
				Image =
				{
					Color = Color3
				}
			}, Layer + ".Content", Layer + ".Clan.Rating");

			container.Add(new CuiLabel
			{
				RectTransform =
				{
					AnchorMin = "0 1", AnchorMax = "1 1",
					OffsetMin = "0 0", OffsetMax = "0 20"
				},
				Text =
				{
					Text = Msg(player, RatingTitle),
					Align = TextAnchor.MiddleLeft,
					Font = "robotocondensed-regular.ttf",
					FontSize = 10,
					Color = "1 1 1 1"
				}
			}, Layer + ".Clan.Rating");

			container.Add(new CuiLabel
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
				Text =
				{
					Text = $"{clan.Top}",
					Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-bold.ttf",
					FontSize = 12,
					Color = "1 1 1 1"
				}
			}, Layer + ".Clan.Rating");

			#endregion

			#region Members

			container.Add(new CuiPanel
			{
				RectTransform =
				{
					AnchorMin = "0 1", AnchorMax = "0 1",
					OffsetMin = "320 -165", OffsetMax = "460 -135"
				},
				Image =
				{
					Color = Color3
				}
			}, Layer + ".Content", Layer + ".Clan.Members");

			container.Add(new CuiLabel
			{
				RectTransform =
				{
					AnchorMin = "0 1", AnchorMax = "1 1",
					OffsetMin = "0 0", OffsetMax = "0 20"
				},
				Text =
				{
					Text = Msg(player, MembersTitle),
					Align = TextAnchor.MiddleLeft,
					Font = "robotocondensed-regular.ttf",
					FontSize = 10,
					Color = "1 1 1 1"
				}
			}, Layer + ".Clan.Members");

			container.Add(new CuiLabel
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
				Text =
				{
					Text = $"{clan.Members.Count}",
					Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-bold.ttf",
					FontSize = 12,
					Color = "1 1 1 1"
				}
			}, Layer + ".Clan.Members");

			#endregion

			#region Ally

			if (_config.AllianceSettings.Enabled && playerClan != null)
			{
				if (playerClan.IsModerator(player.userID) &&
				    _invites.CanSendAllyInvite(clanTag, playerClan.ClanTag)
				   )
					container.Add(new CuiButton
					{
						RectTransform =
						{
							AnchorMin = "0 1", AnchorMax = "0 1",
							OffsetMin = "0 -200", OffsetMax = "140 -175"
						},
						Text =
						{
							Text = Msg(player, SendAllyInvite),
							Align = TextAnchor.MiddleCenter,
							Font = "robotocondensed-regular.ttf",
							FontSize = 12,
							Color = "1 1 1 1"
						},
						Button =
						{
							Color = Color2,
							Command = $"UI_Clans allyinvite send {clanTag}",
							Close = Layer
						}
					}, Layer + ".Content");

				if (playerClan.Alliances.Contains(clanTag))
					container.Add(new CuiButton
					{
						RectTransform =
						{
							AnchorMin = "0 1", AnchorMax = "0 1",
							OffsetMin = "0 -200", OffsetMax = "140 -175"
						},
						Text =
						{
							Text = Msg(player, AllyRevokeTitle),
							Align = TextAnchor.MiddleCenter,
							Font = "robotocondensed-regular.ttf",
							FontSize = 12,
							Color = "1 1 1 1"
						},
						Button =
						{
							Color = Color6,
							Command = $"UI_Clans allyinvite revoke {clanTag}",
							Close = Layer
						}
					}, Layer + ".Content");
			}

			#endregion

			CuiHelper.DestroyUi(player, Layer + ".Content");
			CuiHelper.AddUi(player, container);
		}

		private void SelectSkinUi(BasePlayer player, string shortName, int page = 0, bool First = false)
		{
			#region Fields

			var clan = FindClanByPlayer(player.UserIDString);

			var nowSkin = clan.Skins.ContainsKey(shortName) ? clan.Skins[shortName] : 0;

			var amountOnString = 10;
			var strings = 5;
			var totalAmount = amountOnString * strings;

			var Height = 115f;
			var Width = 110f;
			var Margin = 10f;

			var constSwitchX = -(amountOnString * Width + (amountOnString - 1) * Margin) / 2f;

			var ySwitch = -75f;

			var canCustomSkin = _config.Skins.CanCustomSkin && (string.IsNullOrEmpty(_config.Skins.Permission) ||
			                                                    player.HasPermission(_config.Skins.Permission));

			#endregion

			var container = new CuiElementContainer();

			#region Background

			if (First)
			{
				CuiHelper.DestroyUi(player, Layer);

				container.Add(new CuiPanel
				{
					RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
					Image =
					{
						Color = "0 0 0 0.9",
						Material = "assets/content/ui/uibackgroundblur.mat"
					},
					CursorEnabled = true
				}, "Overlay", Layer);

				container.Add(new CuiButton
				{
					RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
					Text = {Text = ""},
					Button =
					{
						Color = "0 0 0 0",
						Close = Layer
					}
				}, Layer);
			}

			#endregion

			#region Main

			container.Add(new CuiPanel
			{
				RectTransform =
				{
					AnchorMin = "0 0", AnchorMax = "1 1"
				},
				Image =
				{
					Color = Color1
				}
			}, Layer, Layer + ".Main");

			#region Header

			container.Add(new CuiPanel
			{
				RectTransform =
				{
					AnchorMin = "0 1", AnchorMax = "1 1",
					OffsetMin = "0 -55", OffsetMax = "0 0"
				},
				Image =
				{
					Color = Color3
				}
			}, Layer + ".Main", Layer + ".Header");

			#region Title

			container.Add(new CuiLabel
			{
				RectTransform =
				{
					AnchorMin = "0 0", AnchorMax = "1 1",
					OffsetMin = "25 0", OffsetMax = "0 0"
				},
				Text =
				{
					Text = Msg(player, SelectSkinTitle),
					Align = TextAnchor.MiddleLeft,
					Font = "robotocondensed-bold.ttf",
					FontSize = 14,
					Color = "1 1 1 1"
				}
			}, Layer + ".Header");

			#endregion

			#region Enter Skin

			if (canCustomSkin)
			{
				container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0 0.5", AnchorMax = "0 0.5",
						OffsetMin = "160 -17.5", OffsetMax = "410 17.5"
					},
					Image =
					{
						Color = Color4
					}
				}, Layer + ".Header", Layer + ".Header.EnterSkin");

				container.Add(new CuiElement
				{
					Parent = Layer + ".Header.EnterSkin",
					Components =
					{
						new CuiInputFieldComponent
						{
							FontSize = 12,
							Align = TextAnchor.MiddleCenter,
							Font = "robotocondensed-regular.ttf",
							Command = $"UI_Clans setskin {shortName} ",
							Color = "1 1 1 0.65",
							CharsLimit = 32,
							NeedsKeyboard = true,
							Text = nowSkin == 0 ? Msg(player, EnterSkinTitle) : $"{nowSkin}"
						},
						new CuiRectTransformComponent
						{
							AnchorMin = "0 0", AnchorMax = "1 1"
						}
					}
				});
			}

			#endregion

			#region Pages

			var xSwitch = canCustomSkin ? 415f : 160f;

			container.Add(new CuiButton
			{
				RectTransform =
				{
					AnchorMin = "0 0.5", AnchorMax = "0 0.5",
					OffsetMin = $"{xSwitch} -17.5", OffsetMax = $"{xSwitch + 35} 17.5"
				},
				Text =
				{
					Text = Msg(player, BackPage),
					Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-regular.ttf",
					FontSize = 12,
					Color = "1 1 1 1"
				},
				Button =
				{
					Color = Color4,
					Command = page != 0 ? $"UI_Clans editskin {shortName} {page - 1}" : ""
				}
			}, Layer + ".Header");

			xSwitch += 40;

			container.Add(new CuiButton
			{
				RectTransform =
				{
					AnchorMin = "0 0.5", AnchorMax = "0 0.5",
					OffsetMin = $"{xSwitch} -17.5", OffsetMax = $"{xSwitch + 35} 17.5"
				},
				Text =
				{
					Text = Msg(player, NextPage),
					Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-regular.ttf",
					FontSize = 12,
					Color = "1 1 1 1"
				},
				Button =
				{
					Color = Color2,
					Command = _config.Skins.ItemSkins[shortName].Count > (page + 1) * totalAmount
						? $"UI_Clans editskin {shortName} {page + 1}"
						: ""
				}
			}, Layer + ".Header");

			#endregion

			#region Close

			container.Add(new CuiButton
			{
				RectTransform =
				{
					AnchorMin = "1 0.5", AnchorMax = "1 0.5",
					OffsetMin = "-35 -12.5",
					OffsetMax = "-10 12.5"
				},
				Text =
				{
					Text = Msg(player, CloseTitle),
					Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-bold.ttf",
					FontSize = 10,
					Color = "1 1 1 1"
				},
				Button =
				{
					Color = Color2,
					Command = "UI_Clans page 6"
				}
			}, Layer + ".Header");

			#endregion

			#endregion

			#region Items

			xSwitch = constSwitchX;

			var i = 1;
			foreach (var def in _config.Skins.ItemSkins[shortName]
				         .Skip(page * totalAmount)
				         .Take(totalAmount))
			{
				container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0.5 1", AnchorMax = "0.5 1",
						OffsetMin = $"{xSwitch} {ySwitch - Height}",
						OffsetMax = $"{xSwitch + Width} {ySwitch}"
					},
					Image =
					{
						Color = nowSkin == def
							? Color4
							: Color3
					}
				}, Layer + ".Main", Layer + $".Item.{i}");

				container.Add(new CuiElement
				{
					Parent = Layer + $".Item.{i}",
					Components =
					{
						new CuiImageComponent
						{
							ItemId = FindItemID(shortName),
							SkinId = def
						},
						new CuiRectTransformComponent
						{
							AnchorMin = "0.5 1", AnchorMax = "0.5 1",
							OffsetMin = "-35 -80", OffsetMax = "35 -10"
						}
					}
				});

				container.Add(new CuiButton
				{
					RectTransform =
					{
						AnchorMin = "0 0", AnchorMax = "1 0",
						OffsetMin = "0 0", OffsetMax = "0 25"
					},
					Text =
					{
						Text = Msg(player, SelectTitle),
						Align = TextAnchor.MiddleCenter,
						Font = "robotocondensed-regular.ttf",
						FontSize = 10,
						Color = "1 1 1 1"
					},
					Button =
					{
						Color = Color2,
						Command = $"UI_Clans selectskin {shortName} {def}"
					}
				}, Layer + $".Item.{i}");

				if (i % amountOnString == 0)
				{
					xSwitch = constSwitchX;
					ySwitch = ySwitch - Height - Margin;
				}
				else
				{
					xSwitch += Width + Margin;
				}

				i++;
			}

			#endregion

			#endregion

			CuiHelper.DestroyUi(player, Layer + ".Main");
			CuiHelper.AddUi(player, container);
		}

		private void PagesUi(ref CuiElementContainer container, BasePlayer player, int pages, int page, int zPage)
		{
			container.Add(new CuiPanel
			{
				RectTransform =
				{
					AnchorMin = "0.5 0", AnchorMax = "0.5 0",
					OffsetMin = "-25 10",
					OffsetMax = "25 35"
				},
				Image =
				{
					Color = Color4
				}
			}, Layer + ".Content", Layer + ".Pages");

			container.Add(new CuiElement
			{
				Parent = Layer + ".Pages",
				Components =
				{
					new CuiInputFieldComponent
					{
						FontSize = 12,
						Align = TextAnchor.MiddleCenter,
						Font = "robotocondensed-regular.ttf",
						Command = $"UI_Clans inputpage {pages} {page} ",
						Color = "1 1 1 0.65",
						CharsLimit = 32,
						NeedsKeyboard = true,
						Text = $"{zPage + 1}"
					},
					new CuiRectTransformComponent
					{
						AnchorMin = "0 0", AnchorMax = "1 1"
					}
				}
			});

			container.Add(new CuiButton
			{
				RectTransform =
				{
					AnchorMin = "0.5 0", AnchorMax = "0.5 0",
					OffsetMin = "-55 10",
					OffsetMax = "-30 35"
				},
				Text =
				{
					Text = Msg(player, BackPage),
					Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-regular.ttf",
					FontSize = 12,
					Color = "1 1 1 1"
				},
				Button =
				{
					Color = Color4,
					Command = zPage != 0 ? $"UI_Clans page {page} {zPage - 1}" : ""
				}
			}, Layer + ".Content");

			container.Add(new CuiButton
			{
				RectTransform =
				{
					AnchorMin = "0.5 0", AnchorMax = "0.5 0",
					OffsetMin = "30 10",
					OffsetMax = "55 35"
				},
				Text =
				{
					Text = Msg(player, NextPage),
					Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-regular.ttf",
					FontSize = 12,
					Color = "1 1 1 1"
				},
				Button =
				{
					Color = Color2,
					Command = pages > zPage + 1 ? $"UI_Clans page {page} {zPage + 1}" : ""
				}
			}, Layer + ".Content");
		}

		private void ButtonFriendlyFire(ref CuiElementContainer container, BasePlayer player, PlayerData data)
		{
			var clan = FindClanByPlayer(player.UserIDString);

			var allyEnabled = _config.AllianceSettings.Enabled && _config.AllianceSettings.UseFF &&
			                  (!_config.AllianceSettings.GeneralFriendlyFire ||
			                   _config.AllianceSettings.PlayersGeneralFF ||
			                   (_config.AllianceSettings.ModersGeneralFF && clan.IsModerator(player.userID)) ||
			                   clan.IsOwner(player.userID));

			var value = _config.GeneralFriendlyFire ? clan.FriendlyFire : data.FriendlyFire;

			container.Add(new CuiButton
			{
				RectTransform =
				{
					AnchorMin = "0.5 0", AnchorMax = "0.5 0",
					OffsetMin = "-75 50",
					OffsetMax = $"{(allyEnabled ? 15 : 75)} 80"
				},
				Text =
				{
					Text = Msg(player, FriendlyFireTitle),
					Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-regular.ttf",
					FontSize = 12,
					Color = "1 1 1 1"
				},
				Button =
				{
					Color = value ? Color2 : Color4,
					Command = "UI_Clans ff"
				}
			}, Layer + ".Menu", Layer + ".Menu.Button.FF");

			CuiHelper.DestroyUi(player, Layer + ".Menu.Button.FF");
		}

		private void ButtonAlly(ref CuiElementContainer container, BasePlayer player, PlayerData data)
		{
			var value = _config.AllianceSettings.GeneralFriendlyFire
				? FindClanByPlayer(player.UserIDString).AllyFriendlyFire
				: data.AllyFriendlyFire;

			container.Add(new CuiButton
			{
				RectTransform =
				{
					AnchorMin = "0.5 0", AnchorMax = "0.5 0",
					OffsetMin = "20 50",
					OffsetMax = "75 80"
				},
				Text =
				{
					Text = Msg(player, AllyFriendlyFireTitle),
					Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-regular.ttf",
					FontSize = 12,
					Color = "1 1 1 1"
				},
				Button =
				{
					Color = value ? Color4 : Color6,
					Command = "UI_Clans allyff"
				}
			}, Layer + ".Menu", Layer + ".Menu.Button.Ally");

			CuiHelper.DestroyUi(player, Layer + ".Menu.Button.Ally");
		}

		private void ButtonClanSkins(ref CuiElementContainer container, BasePlayer player, PlayerData data)
		{
			container.Add(new CuiButton
			{
				RectTransform =
				{
					AnchorMin = "1 1", AnchorMax = "1 1",
					OffsetMin = "-285 -37.5",
					OffsetMax = "-185 -12.5"
				},
				Text =
				{
					Text = Msg(player, UseClanSkins),
					Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-regular.ttf",
					FontSize = 12,
					Color = "1 1 1 1"
				},
				Button =
				{
					Color = data.ClanSkins ? Color2 : Color4,
					Command = "UI_Clans clanskins"
				}
			}, Layer + ".Header", Layer + ".Header.Use.ClanSkins");

			CuiHelper.DestroyUi(player, Layer + ".Header.Use.ClanSkins");
		}

		#endregion

		#region Utils

		#region PlayerSkins

		private bool LoadSkinsFromPlayerSkins()
		{
			if (!_config.Skins.UsePlayerSkins) return false;

			Dictionary<string, Dictionary<ulong, PlayerSkinsSkinData>> skinData;
			try
			{
				skinData = Interface.Oxide.DataFileSystem
					.ReadObject<Dictionary<string, Dictionary<ulong, PlayerSkinsSkinData>>>("PlayerSkins/skinlist");
			}
			catch
			{
				skinData = new Dictionary<string, Dictionary<ulong, PlayerSkinsSkinData>>();
			}

			if (skinData != null)
			{
				_config.Skins.ItemSkins = skinData.ToDictionary(x => x.Key, x => x.Value.Keys.ToList());
				return true;
			}

			return false;
		}

		private class PlayerSkinsSkinData
		{
			public string permission = string.Empty;
			public int cost = 1;
			public bool isDisabled = false;
		}

		#endregion

		#region PlayTime

		private double GetPlayTime(string playerid)
		{
			return Convert.ToDouble(PlayTimeRewards?.Call("FetchPlayTime", playerid));
		}

		private static string FormatTime(double seconds)
		{
			var time = TimeSpan.FromSeconds(seconds);

			var result =
				$"{(time.Duration().Days > 0 ? $"{time.Days:0} Day{(time.Days == 1 ? string.Empty : "s")} " : string.Empty)}{(time.Duration().Hours > 0 ? $"{time.Hours:0} Hour{(time.Hours == 1 ? string.Empty : "s")} " : string.Empty)}{(time.Duration().Minutes > 0 ? $"{time.Minutes:0} Min " : string.Empty)}{(time.Duration().Seconds > 0 ? $"{time.Seconds:0} Sec" : string.Empty)}";

			if (result.EndsWith(", ")) result = result.Substring(0, result.Length - 2);

			if (string.IsNullOrEmpty(result)) result = "0 Seconds";

			return result;
		}

		#endregion

		private bool IsValidURL(string url)
		{
			Uri uriResult;
			return Uri.TryCreate(url, UriKind.Absolute, out uriResult)
			       && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
		}

		private void UnsubscribeHooks()
		{
			if (!_config.ClanCreateTeam)
				Unsubscribe(nameof(OnTeamCreate));

			if (!_config.ClanTeamLeave)
				Unsubscribe(nameof(OnTeamLeave));

			if (!_config.ClanTeamKick)
				Unsubscribe(nameof(OnTeamKick));

			if (!_config.ClanTeamInvite)
				Unsubscribe(nameof(OnTeamInvite));

			if (!_config.ClanTeamPromote)
				Unsubscribe(nameof(OnTeamPromote));

			if (!_config.ClanTeamInvite || !_config.ClanTeamAcceptInvite)
				Unsubscribe(nameof(OnTeamAcceptInvite));

			if (!_config.Skins.UseSkinBox)
				Unsubscribe(nameof(OnSkinBoxSkinsLoaded));

			if (!_config.Statistics.Kills)
				Unsubscribe(nameof(OnPlayerDeath));

			if (!_config.Statistics.Gather)
			{
				Unsubscribe(nameof(OnCollectiblePickup));
				Unsubscribe(nameof(OnCropGather));
				Unsubscribe(nameof(OnDispenserBonus));
				Unsubscribe(nameof(OnDispenserGather));
			}

			if (!_config.Statistics.Loot)
			{
				Unsubscribe(nameof(OnItemRemovedFromContainer));
				Unsubscribe(nameof(CanMoveItem));
				Unsubscribe(nameof(OnItemPickup));
			}

			if (!_config.Statistics.Entities) Unsubscribe(nameof(OnEntityDeath));

			if (!_config.Statistics.Craft) Unsubscribe(nameof(OnItemCraftFinished));

			if (!_config.UseTurretsFF) Unsubscribe(nameof(CanBeTargeted));
		}

		private static bool IsOnline(ulong member)
		{
			var player = BasePlayer.FindByID(member);
			return player != null && player.IsConnected;
		}

		private string GetPlayerName(BasePlayer player)
		{
			if (player == null || !player.userID.IsSteamId())
				return string.Empty;

			var covPlayer = player.IPlayer;

			if (player.net?.connection == null)
			{
				if (covPlayer != null)
					return covPlayer.Name;

				return player.UserIDString;
			}

			var value = player.net.connection.username;
			var str = value.ToPrintable(32).EscapeRichText().Trim();
			if (string.IsNullOrWhiteSpace(str))
			{
				str = covPlayer.Name;
				if (string.IsNullOrWhiteSpace(str))
					str = player.UserIDString;
			}

			return str;
		}

		private bool CanUseSkins(BasePlayer player)
		{
			var data = PlayerData.GetOrLoad(player.UserIDString);
			if (data == null) return false;

			if (_config.Skins.DisableSkins)
				return data.ClanSkins && (!_config.PermissionSettings.UseClanSkins ||
				                          string.IsNullOrEmpty(_config.PermissionSettings.ClanSkins) ||
				                          player.HasPermission(_config.PermissionSettings.ClanSkins));

			return true;
		}

		private void RegisterCommands()
		{
			AddCovalenceCommand(_config.Commands.ToArray(), nameof(CmdClans));

			AddCovalenceCommand(_config.ClanInfoCommands, nameof(CmdClanInfo));

			AddCovalenceCommand("clans.manage", nameof(CmdAdminClans));
		}

		private void RegisterPermissions()
		{
			permission.RegisterPermission(PermAdmin, this);

			if (_config.PermissionSettings.UseClanCreating &&
			    !string.IsNullOrEmpty(_config.PermissionSettings.ClanCreating) &&
			    !permission.PermissionExists(_config.PermissionSettings.ClanCreating))
				permission.RegisterPermission(_config.PermissionSettings.ClanCreating, this);

			if (_config.PermissionSettings.UseClanJoining &&
			    !string.IsNullOrEmpty(_config.PermissionSettings.ClanJoining) &&
			    !permission.PermissionExists(_config.PermissionSettings.ClanJoining))
				permission.RegisterPermission(_config.PermissionSettings.ClanJoining, this);

			if (_config.PermissionSettings.UseClanKick && !string.IsNullOrEmpty(_config.PermissionSettings.ClanKick) &&
			    !permission.PermissionExists(_config.PermissionSettings.ClanKick))
				permission.RegisterPermission(_config.PermissionSettings.ClanKick, this);

			if (_config.PermissionSettings.UseClanLeave &&
			    !string.IsNullOrEmpty(_config.PermissionSettings.ClanLeave) &&
			    !permission.PermissionExists(_config.PermissionSettings.ClanLeave))
				permission.RegisterPermission(_config.PermissionSettings.ClanLeave, this);

			if (_config.PermissionSettings.UseClanDisband &&
			    !string.IsNullOrEmpty(_config.PermissionSettings.ClanDisband) &&
			    !permission.PermissionExists(_config.PermissionSettings.ClanDisband))
				permission.RegisterPermission(_config.PermissionSettings.ClanDisband, this);

			if (_config.PermissionSettings.UseClanSkins &&
			    !string.IsNullOrEmpty(_config.PermissionSettings.ClanSkins) &&
			    !permission.PermissionExists(_config.PermissionSettings.ClanSkins))
				permission.RegisterPermission(_config.PermissionSettings.ClanSkins, this);

			if (_config.Skins.CanCustomSkin &&
			    !string.IsNullOrEmpty(_config.Skins.Permission) &&
			    !permission.PermissionExists(_config.Skins.Permission))
				permission.RegisterPermission(_config.Skins.Permission, this);

			_config.Pages.ForEach(page =>
			{
				if (!string.IsNullOrEmpty(page.Permission) && !permission.PermissionExists(page.Permission))
					permission.RegisterPermission(page.Permission, this);
			});
		}

		private void LoadColors()
		{
			Color1 = HexToCuiColor(_config.UI.Color1);
			Color2 = HexToCuiColor(_config.UI.Color2);
			Color3 = HexToCuiColor(_config.UI.Color3);
			Color4 = HexToCuiColor(_config.UI.Color4);
			Color5 = HexToCuiColor(_config.UI.Color5);
			Color6 = HexToCuiColor(_config.UI.Color6);
		}

		private void PurgeClans()
		{
			if (_config.PurgeSettings.Enabled)
			{
				var toRemove = Pool.GetList<ClanData>();

				_clansList.ForEach(clan =>
				{
					if (DateTime.Now.Subtract(clan.LastOnlineTime).Days > _config.PurgeSettings.OlderThanDays)
						toRemove.Add(clan);
				});

				if (_config.PurgeSettings.ListPurgedClans)
				{
					var str = string.Join("\n",
						toRemove.Select(clan =>
							$"Purged - [{clan.ClanTag}] | Owner: {clan.LeaderID} | Last Online: {clan.LastOnlineTime}"));

					if (!string.IsNullOrEmpty(str))
						Puts(str);
				}

				toRemove.ForEach(clan =>
				{
					_clanByTag.Remove(clan.ClanTag);
					_clansList.Remove(clan);
				});

				Pool.FreeList(ref toRemove);
			}
		}

		private void LoadImages()
		{
			if (!ImageLibrary)
			{
				PrintWarning("IMAGE LIBRARY IS NOT INSTALLED");
			}
			else
			{
				var imagesList = new Dictionary<string, string>
				{
					[_config.DefaultAvatar] = _config.DefaultAvatar
				};

				_clansList.ForEach(clan =>
				{
					if (!string.IsNullOrEmpty(clan.Avatar))
						imagesList[clan.Avatar] = clan.Avatar;
				});

				ImageLibrary?.Call("ImportImageList", Title, imagesList, 0UL, true);
			}
		}

		private void FillingTeams()
		{
			if (_config.AutoTeamCreation)
			{
				RelationshipManager.maxTeamSize = _config.LimitSettings.MemberLimit;

				_clansList.ForEach(clan =>
				{
					clan.FindOrCreateTeam();

					clan.Members.ForEach(member => clan.AddPlayer(member));
				});
			}
		}

		private static string HexToCuiColor(string HEX, float Alpha = 100)
		{
			if (string.IsNullOrEmpty(HEX)) HEX = "#FFFFFF";

			var str = HEX.Trim('#');
			if (str.Length != 6) throw new Exception(HEX);
			var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
			var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
			var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);

			return $"{(double) r / 255} {(double) g / 255} {(double) b / 255} {Alpha / 100f}";
		}

		private string BetterChat_FormattedClanTag(IPlayer player)
		{
			var clan = FindClanByPlayer(player.Id);
			return clan == null
				? string.Empty
				: $"{_config.ChatSettings.TagFormat.Replace("{color}", clan.GetHexTagColor()).Replace("{tag}", clan.ClanTag)}";
		}

		private bool IsTeammates(ulong player, ulong friend)
		{
			return player == friend ||
			       RelationshipManager.ServerInstance.FindPlayersTeam(player)?.members?.Contains(friend) == true ||
			       FindClanByPlayer(player.ToString())?.IsMember(friend) == true;
		}

		#region Avatar

		private readonly Regex Regex = new Regex(@"<avatarFull><!\[CDATA\[(.*)\]\]></avatarFull>");

		private void GetAvatar(string userId, Action<string> callback)
		{
			if (callback == null) return;

			try
			{
				webrequest.Enqueue($"http://steamcommunity.com/profiles/{userId}?xml=1", null, (code, response) =>
				{
					if (code != 200 || response == null)
						return;

					var avatar = Regex.Match(response).Groups[1].ToString();
					if (string.IsNullOrEmpty(avatar))
						return;

					callback.Invoke(avatar);
				}, this);
			}
			catch (Exception e)
			{
				PrintError($"{e.Message}");
			}
		}

		private void StartLoadingAvatars()
		{
			Puts("Loading avatars started!");

			_actionAvatars = ServerMgr.Instance.StartCoroutine(LoadAvatars());
		}

		private IEnumerator LoadAvatars()
		{
			foreach (var player in covalence.Players.All)
			{
				GetAvatar(player.Id,
					avatar => ImageLibrary?.Call("AddImage", avatar, $"avatar_{player.Id}"));

				yield return CoroutineEx.waitForSeconds(0.5f);
			}

			Puts("Uploading avatars is complete!");
		}

		#endregion

		private void FillingStandartItems()
		{
			_config.AvailableStandartItems.ForEach(shortName =>
			{
				var def = ItemManager.FindItemDefinition(shortName);
				if (def == null) return;

				_defaultItems.Add(def);
			});
		}

		private static void ApplySkinToItem(Item item, ulong Skin)
		{
			item.skin = Skin;
			item.MarkDirty();

			var heldEntity = item.GetHeldEntity();
			if (heldEntity == null) return;

			heldEntity.skinID = Skin;
			heldEntity.SendNetworkUpdate();
		}

		private static string GetValue(float value)
		{
			if (!_config.UI.ValueAbbreviation)
				return Mathf.Round(value).ToString(CultureInfo.InvariantCulture);

			var t = string.Empty;
			while (value > 1000)
			{
				t += "K";
				value /= 1000;
			}

			return Mathf.Round(value) + t;
		}

		private string[] ZM_GetPlayerZones(BasePlayer player)
		{
			return ZoneManager?.Call<string[]>("GetPlayerZoneIDs", player) ?? new string[] { };
		}

		#endregion

		#region API

		private static void ClanCreate(string tag)
		{
			Interface.CallHook("OnClanCreate", tag);
		}

		private static void ClanUpdate(string tag)
		{
			Interface.CallHook("OnClanUpdate", tag);
		}

		private static void ClanDestroy(string tag)
		{
			Interface.CallHook("OnClanDestroy", tag);
		}

		private static void ClanDisbanded(List<string> memberUserIDs)
		{
			Interface.CallHook("OnClanDisbanded", memberUserIDs);
		}

		private static void ClanDisbanded(string tag, List<string> memberUserIDs)
		{
			Interface.CallHook("OnClanDisbanded", tag, memberUserIDs);
		}

		private static void ClanMemberJoined(string userID, string tag)
		{
			Interface.CallHook("OnClanMemberJoined", userID, tag);
		}

		private static void ClanMemberJoined(string userID, List<string> memberUserIDs)
		{
			Interface.CallHook("OnClanMemberJoined", userID, memberUserIDs);
		}

		private static void ClanMemberGone(string userID, List<string> memberUserIDs)
		{
			Interface.CallHook("OnClanMemberGone", userID, memberUserIDs);
		}

		private static void ClanMemberGone(string userID, string tag)
		{
			Interface.CallHook("OnClanMemberGone", userID, tag);
		}

		private static void ClanTopUpdated()
		{
			Interface.CallHook("OnClanTopUpdated");
		}

		private ClanData FindClanByPlayer(string userId)
		{
			return PlayerData.GetOrLoad(userId)?.GetClan();
		}

		private ClanData FindClanByUserID(string userId)
		{
			return FindClanByUserID(ulong.Parse(userId));
		}

		private ClanData FindClanByUserID(ulong userId)
		{
			return _clansList.Find(clan => clan.IsMember(userId));
		}

		private ClanData FindClanByTag(string tag)
		{
			ClanData clan;
			return _clanByTag.TryGetValue(tag, out clan) ? clan : null;
		}

		private bool PlayerHasClan(ulong userId)
		{
			return FindClanByPlayer(userId.ToString()) != null;
		}

		private bool IsClanMember(string playerId, string otherId)
		{
			return IsClanMember(ulong.Parse(playerId), ulong.Parse(otherId));
		}

		private bool IsClanMember(ulong playerId, ulong otherId)
		{
			var clan = FindClanByPlayer(playerId.ToString());
			return clan != null && clan.IsMember(otherId);
		}

		private JObject GetClan(string tag)
		{
			return FindClanByTag(tag)?.ToJObject();
		}

		private string GetClanOf(BasePlayer target)
		{
			return GetClanOf(target.userID);
		}

		private string GetClanOf(string target)
		{
			return GetClanOf(ulong.Parse(target));
		}

		private string GetClanOf(ulong target)
		{
			return FindClanByPlayer(target.ToString())?.ClanTag;
		}

		private JArray GetAllClans()
		{
			return new JArray(_clansList.Select(x => x.ToJObject()));
		}

		private List<string> GetClanMembers(string target)
		{
			return GetClanMembers(ulong.Parse(target));
		}

		private List<string> GetClanMembers(ulong target)
		{
			return FindClanByPlayer(target.ToString())?.Members.Select(x => x.ToString());
		}

		private List<string> GetClanAlliances(string playerId)
		{
			return GetClanAlliances(ulong.Parse(playerId));
		}

		private List<string> GetClanAlliances(ulong playerId)
		{
			var clan = FindClanByPlayer(playerId.ToString());
			return clan == null ? new List<string>() : new List<string>(clan.Alliances);
		}

		private bool IsAllyPlayer(string playerId, string otherId)
		{
			return IsAllyPlayer(ulong.Parse(playerId), ulong.Parse(otherId));
		}

		private bool IsAllyPlayer(ulong playerId, ulong otherId)
		{
			var playerClan = FindClanByPlayer(playerId.ToString());
			if (playerClan == null)
				return false;

			var otherClan = FindClanByPlayer(otherId.ToString());
			if (otherClan == null)
				return false;

			return playerClan.Alliances.Contains(otherClan.ClanTag);
		}

		private bool IsMemberOrAlly(string playerId, string otherId)
		{
			return IsMemberOrAlly(ulong.Parse(playerId), ulong.Parse(otherId));
		}

		private bool IsMemberOrAlly(ulong playerId, ulong otherId)
		{
			var playerClan = FindClanByPlayer(playerId.ToString());
			if (playerClan == null)
				return false;

			var otherClan = FindClanByPlayer(otherId.ToString());
			if (otherClan == null)
				return false;

			return playerClan.ClanTag.Equals(otherClan.ClanTag) || playerClan.Alliances.Contains(otherClan.ClanTag);
		}

		private Dictionary<int, string> GetTopClans()
		{
			return TopClans.ToDictionary(y => y.Top, x => x.ClanTag);
		}

		#endregion

		#region Invites

		#region Players

		private void SendInvite(BasePlayer inviter, ulong target)
		{
			if (inviter == null) return;

			var clan = FindClanByPlayer(inviter.UserIDString);
			if (clan == null) return;

			if (!clan.IsModerator(inviter.userID))
			{
				Reply(inviter, NotModer);
				return;
			}

			if (clan.Members.Count >= _config.LimitSettings.MemberLimit)
			{
				Reply(inviter, ALotOfMembers);
				return;
			}

			var targetClan = FindClanByPlayer(target.ToString());
			if (targetClan != null)
			{
				Reply(inviter, HeAlreadyClanMember);
				return;
			}

			var data = PlayerData.GetOrCreate(target.ToString());
			if (data == null) return;

			if (!_invites.CanSendInvite(target, clan.ClanTag))
			{
				Reply(inviter, AlreadyInvitedInClan);
				return;
			}

			var inviterName =
				$"{PlayerData.GetOrLoad(inviter.UserIDString)?.DisplayName ?? inviter.Connection.username}";

			_invites.AddPlayerInvite(target, inviter.userID, inviterName, clan.ClanTag);

			Reply(inviter, SuccessInvited, data.DisplayName, clan.ClanTag);

			var targetPlayer = BasePlayer.FindByID(target);
			if (targetPlayer != null)
				Reply(targetPlayer, SuccessInvitedSelf, inviterName, clan.ClanTag);
		}

		private void AcceptInvite(BasePlayer player, string tag)
		{
			if (player == null || string.IsNullOrEmpty(tag)) return;

			if (_config.PermissionSettings.UseClanJoining &&
			    !string.IsNullOrEmpty(_config.PermissionSettings.ClanJoining) &&
			    !player.HasPermission(_config.PermissionSettings.ClanJoining))
			{
				Reply(player, NoPermJoinClan);
				return;
			}

			var data = PlayerData.GetOrCreate(player.UserIDString);
			if (data == null) return;

			var clan = data.GetClan();
			if (clan != null)
			{
				Reply(player, AlreadyClanMember);
				return;
			}

			clan = FindClanByTag(tag);
			if (clan == null)
			{
				_invites.RemovePlayerClanInvites(tag);
				return;
			}

			if (clan.Members.Count >= _config.LimitSettings.MemberLimit)
			{
				Reply(player, ALotOfMembers);
				return;
			}

			var inviteData = _invites.GetClanInvite(player.userID, tag);
			if (inviteData == null)
				return;

			clan.Join(player);
			Reply(player, ClanJoined, clan.ClanTag);

			var inviter = BasePlayer.FindByID(inviteData.InviterId);
			if (inviter != null)
				Reply(inviter, WasInvited, PlayerData.GetOrLoad(player.UserIDString).DisplayName);
		}

		private void CancelInvite(BasePlayer player, string tag)
		{
			if (player == null || string.IsNullOrEmpty(tag)) return;

			var data = PlayerData.GetOrCreate(player.UserIDString);
			if (data == null) return;

			var inviteData = _invites.GetClanInvite(player.userID, tag);
			if (inviteData == null) return;

			_invites.RemovePlayerClanInvites(inviteData);

			Reply(player, DeclinedInvite, tag);

			var inviter = BasePlayer.FindByID(inviteData.InviterId);
			if (inviter != null)
				Reply(inviter, DeclinedInviteSelf);
		}

		private void WithdrawInvite(BasePlayer inviter, ulong target)
		{
			var inviterData = PlayerData.GetOrLoad(inviter.UserIDString);

			var clan = inviterData?.GetClan();
			if (clan == null) return;

			if (!clan.IsModerator(inviter.userID))
			{
				Reply(inviter, NotModer);
				return;
			}

			var data = PlayerData.GetOrCreate(target.ToString());
			if (data == null) return;

			var inviteData = _invites.GetClanInvite(target, clan.ClanTag);
			if (inviteData == null)
			{
				Reply(inviter, DidntReceiveInvite, data.DisplayName);
				return;
			}

			var clanInviter = inviteData.InviterId;
			if (clanInviter != inviter.userID)
			{
				var clanInviterPlayer = BasePlayer.FindByID(clanInviter);
				if (clanInviterPlayer != null)
					Reply(clanInviterPlayer, YourInviteDeclined, data.DisplayName,
						inviterData.DisplayName);
			}

			_invites.RemovePlayerClanInvites(inviteData);

			var targetPlayer = BasePlayer.FindByID(target);
			if (targetPlayer != null)
				Reply(targetPlayer, CancelledInvite, clan.ClanTag);

			Reply(inviter, CancelledYourInvite, data.DisplayName);
		}

		private bool HasInvite(BasePlayer player)
		{
			if (player == null) return false;

			return _invites?.GetPlayerClanInvites(player.userID).Count > 0;
		}

		#endregion

		#region Alliances

		private void AllySendInvite(BasePlayer player, string clanTag)
		{
			if (player == null || string.IsNullOrEmpty(clanTag)) return;

			var clan = FindClanByPlayer(player.UserIDString);
			if (clan == null) return;

			if (!clan.IsModerator(player.userID))
			{
				Reply(player, NotModer);
				return;
			}

			var targetClan = FindClanByTag(clanTag);
			if (targetClan == null) return;

			if (clan.Alliances.Count >= _config.LimitSettings.AlliancesLimit ||
			    targetClan.Alliances.Count >= _config.LimitSettings.AlliancesLimit)
			{
				Reply(player, ALotOfAlliances);
				return;
			}

			var invites = _invites.GetAllyTargetInvites(clan.ClanTag);
			if (invites.Exists(invite => invite.TargetClanTag == clanTag))
			{
				Reply(player, AllInviteExist);
				return;
			}

			if (targetClan.Alliances.Contains(clan.ClanTag))
			{
				Reply(player, AlreadyAlliance);
				return;
			}

			invites = _invites.GetAllyIncomingInvites(clanTag);
			if (invites.Exists(x => x.SenderClanTag == clanTag))
			{
				AllyAcceptInvite(player, clanTag);
				return;
			}

			_invites.AddAllyInvite(player.userID, player.displayName, clan.ClanTag, targetClan.ClanTag);

			clan.Members.FindAll(member => member != player.userID).ForEach(member =>
				Reply(BasePlayer.FindByID(member), AllySendedInvite, player.displayName, targetClan.ClanTag));

			Reply(player, YouAllySendedInvite, targetClan.ClanTag);

			targetClan.Members.ForEach(member =>
				Reply(BasePlayer.FindByID(member), SelfAllySendedInvite, clan.ClanTag));
		}

		private void AllyAcceptInvite(BasePlayer player, string clanTag)
		{
			if (player == null || string.IsNullOrEmpty(clanTag)) return;

			var clan = FindClanByPlayer(player.UserIDString);
			if (clan == null) return;

			var targetClan = FindClanByTag(clanTag);
			if (targetClan == null) return;

			if (clan.Alliances.Count >= _config.LimitSettings.AlliancesLimit ||
			    targetClan.Alliances.Count >= _config.LimitSettings.AlliancesLimit)
			{
				Reply(player, ALotOfAlliances);
				return;
			}

			var invites = _invites.GetAllyIncomingInvites(clan.ClanTag);
			if (invites.Exists(invite => invite.SenderClanTag == targetClan.ClanTag))
			{
				Reply(player, NoFoundInviteAlly, targetClan.ClanTag);
				return;
			}

			_invites.RemoveAllyInviteByClan(clan.ClanTag, targetClan.ClanTag);

			clan.Alliances.Add(targetClan.ClanTag);
			targetClan.Alliances.Add(clan.ClanTag);

			clan.Members.ForEach(member =>
				Reply(BasePlayer.FindByID(member), AllyAcceptInviteTitle, targetClan.ClanTag));
			targetClan.Members.ForEach(member =>
				Reply(BasePlayer.FindByID(member), AllyAcceptInviteTitle, clan.ClanTag));
		}

		private void AllyCancelInvite(BasePlayer player, string clanTag)
		{
			if (player == null || string.IsNullOrEmpty(clanTag)) return;

			var clan = FindClanByPlayer(player.UserIDString);
			if (clan == null) return;

			var targetClan = FindClanByTag(clanTag);
			if (targetClan == null) return;

			_invites.RemoveAllyInviteByClan(clan.ClanTag, targetClan.ClanTag);

			clan.Members.ForEach(member =>
				Reply(BasePlayer.FindByID(member), RejectedInviteTitle, targetClan.ClanTag));
			targetClan.Members.ForEach(member =>
				Reply(BasePlayer.FindByID(member), SelfRejectedInviteTitle, clan.ClanTag));
		}

		private void AllyWithdrawInvite(BasePlayer player, string clanTag)
		{
			if (player == null || string.IsNullOrEmpty(clanTag)) return;

			var clan = FindClanByPlayer(player.UserIDString);
			if (clan == null) return;

			var targetClan = FindClanByTag(clanTag);
			if (targetClan == null) return;

			_invites.RemoveAllyInviteByClan(clan.ClanTag, targetClan.ClanTag);

			clan.Members.ForEach(member =>
				Reply(BasePlayer.FindByID(member), WithdrawInviteTitle, targetClan.ClanTag));
			targetClan.Members.ForEach(member =>
				Reply(BasePlayer.FindByID(member), SelfWithdrawInviteTitle, clan.ClanTag));
		}

		private void AllyRevoke(BasePlayer player, string clanTag)
		{
			if (player == null || string.IsNullOrEmpty(clanTag)) return;

			var clan = FindClanByPlayer(player.UserIDString);
			if (clan == null) return;

			var targetClan = FindClanByTag(clanTag);
			if (targetClan == null) return;

			if (!clan.Alliances.Contains(clanTag))
			{
				Reply(player, NoAlly, clanTag);
				return;
			}

			clan.Alliances.Remove(targetClan.ClanTag);
			targetClan.Alliances.Remove(clan.ClanTag);

			clan.Members.ForEach(member => Reply(BasePlayer.FindByID(member), SelfBreakAlly, targetClan.ClanTag));

			targetClan.Members.ForEach(member => Reply(BasePlayer.FindByID(member), BreakAlly, clan.ClanTag));
		}

		private bool HasAllyInvite(ClanData clan, string clanTag)
		{
			return _invites.CanSendAllyInvite(clan.ClanTag, clanTag);
		}

		private bool HasAllyIncomingInvite(ClanData clan, string clanTag)
		{
			return
				_invites.CanSendAllyInvite(clanTag,
					clan.ClanTag);
		}

		#endregion

		#endregion

		#region Clan Creating

		private readonly Dictionary<BasePlayer, CreateClanData> ClanCreating =
			new Dictionary<BasePlayer, CreateClanData>();

		private class CreateClanData
		{
			public string Tag;

			public string Avatar;
		}

		#endregion

		#region Rating

		private List<ClanData> TopClans = new List<ClanData>();

		private readonly Dictionary<ulong, TopPlayerData> TopPlayers = new Dictionary<ulong, TopPlayerData>();

		private class TopPlayerData
		{
			public readonly ulong UserId;

			public int Top;

			public readonly PlayerData Data;

			public float Score()
			{
				return Data.Score;
			}

			public string GetParams(string value)
			{
				switch (value)
				{
					case "name":
						return Data.DisplayName;
					case "score":
						return GetValue(Score());
					case "resources":
						return GetValue(Data.Resources);
					default:
						return GetValue(Data.GetValue(value));
				}
			}

			public TopPlayerData(PlayerData data)
			{
				ulong.TryParse(data.SteamID, out UserId);
				Data = data;
			}
		}

		private TopPlayerData GetTopDataById(ulong target)
		{
			TopPlayerData data;
			return TopPlayers.TryGetValue(target, out data) ? data : null;
		}

		private void HandleTop()
		{
			if (_handleTop != null) return;

			_handleTop = ServerMgr.Instance.StartCoroutine(PlayerData.TopCoroutine());
		}

		private void SortPlayers(List<TopPlayerData> topPlayers)
		{
			topPlayers.Sort((x, y) => y.Score().CompareTo(x.Score()));

			for (var i = 0; i < topPlayers.Count; i++)
			{
				var member = topPlayers[i];

				member.Top = i + 1;

				TopPlayers[member.UserId] = member;
			}
		}

		private void SortClans()
		{
			TopClans = _clansList.ToList();

			TopClans.Sort((x, y) => y.TotalScores.CompareTo(x.TotalScores));

			for (var i = 0; i < TopClans.Count; i++)
			{
				TopClans[i].Top = i + 1;

				TopClans[i].UpdateTotalFarm();
			}
		}

		#endregion

		#region Item Skins

		private void LoadSkins()
		{
			if (LoadClansSkins() || LoadSkinsFromPlayerSkins())
				SaveConfig();
		}

		private bool LoadClansSkins()
		{
			if (!_config.Pages.Exists(page => page.ID == 6 && page.Enabled)) return false;

			var any = false;
			_config.Skins.ItemSkins.ToList().FindAll(itemSkin => itemSkin.Value.Count == 0).ForEach(itemSkin =>
			{
				_config.Skins.ItemSkins[itemSkin.Key] =
					ImageLibrary?.Call<List<ulong>>("GetImageList", itemSkin.Key) ??
					new List<ulong>();

				any = true;
			});

			return any;
		}

		private Dictionary<string, int> _itemIds = new Dictionary<string, int>();

		private int FindItemID(string shortName)
		{
			int val;
			if (_itemIds.TryGetValue(shortName, out val))
				return val;

			var definition = ItemManager.FindItemDefinition(shortName);
			if (definition == null) return 0;

			val = definition.itemid;
			_itemIds[shortName] = val;
			return val;
		}

		private ulong GetItemSkin(string shortName, ClanData clan)
		{
			ulong skin;
			return clan.Skins.TryGetValue(shortName, out skin) ? skin : 0;
		}

		#endregion

		#region Lang

		private const string
			PlayTimeTitle = "PlayTimeTitle",
			TagColorTitle = "TagColorTitle",
			TagColorInstalled = "TagColorInstalled",
			TagColorFormat = "TagColorFormat",
			NoPermissions = "NoPermissions",
			ClanInfoAlliancesNone = "ClanInfoAlliancesNone",
			ClanInfoAlliances = "ClanInfoAlliances",
			ClanInfoLastOnline = "ClanInfoLastOnline",
			ClanInfoEstablished = "ClanInfoEstablished",
			ClanInfoOffline = "ClanInfoOffline",
			ClanInfoOnline = "ClanInfoOnline",
			ClanInfoDescription = "ClanInfoDescription",
			ClanInfoTag = "ClanInfoTag",
			ClanInfoTitle = "ClanInfoTitle",
			AdminRename = "AdminRename",
			AdminSetLeader = "AdminSetLeader",
			AdminKickBroadcast = "AdminKickBroadcast",
			AdminBroadcast = "AdminBroadcast",
			AdminJoin = "AdminJoin",
			AdminKick = "AdminKick",
			AdminInvite = "AdminInvite",
			AdminPromote = "AdminPromote",
			AdminDemote = "AdminDemote",
			AdminDisbandClan = "AdminDisbandClan",
			UseClanSkins = "UseClanSkins",
			ClansMenuTitle = "ClansMenuTitle",
			AboutClan = "AboutClan",
			ChangeAvatar = "ChangeAvatar",
			EnterLink = "EnterLink",
			LeaderTitle = "LeaderTitle",
			GatherTitle = "GatherTitle",
			RatingTitle = "RatingTitle",
			MembersTitle = "MembersTitle",
			DescriptionTitle = "DescriptionTitle",
			NameTitle = "NameTitle",
			SteamIdTitle = "SteamIdTitle",
			ProfileTitle = "ProfileTitle",
			InvitedToClan = "InvitedToClan",
			BackPage = "BackPage",
			NextPage = "NextPage",
			TopClansTitle = "TopClansTitle",
			TopPlayersTitle = "TopPlayersTitle",
			TopTitle = "TopTitle",
			ScoreTitle = "ScoreTitle",
			KillsTitle = "KillsTitle",
			DeathsTitle = "DeathsTitle",
			KDTitle = "KDTitle",
			ResourcesTitle = "ResourcesTitle",
			LeftTitle = "LeftTitle",
			EditTitle = "EditTitle",
			InviteTitle = "InviteTitle",
			SearchTitle = "SearchTitle",
			ClanInvitation = "ClanInvitation",
			InviterTitle = "InviterTitle",
			AcceptTitle = "AcceptTitle",
			CancelTitle = "CancelTitle",
			PlayerTitle = "PlayerTitle",
			ClanTitle = "ClanTitle",
			NotMemberOfClan = "NotMemberOfClan",
			SelectItemTitle = "SelectItemTitle",
			CloseTitle = "CloseTitle",
			SelectTitle = "SelectTitle",
			ClanCreationTitle = "ClanCreationTitle",
			ClanNameTitle = "ClanNameTitle",
			AvatarTitle = "AvatarTitle",
			UrlTitle = "UrlTitle",
			CreateTitle = "CreateTitle",
			LastLoginTitle = "LastLoginTitle",
			DemoteModerTitle = "DemoteModerTitle",
			PromoteModerTitle = "PromoteModerTitle",
			PromoteLeaderTitle = "PromoteLeaderTitle",
			KickTitle = "KickTitle",
			GatherRatesTitle = "GatherRatesTitle",
			CreateClanTitle = "CreateClanTitle",
			FriendlyFireTitle = "FriendlyFireTitle",
			AllyFriendlyFireTitle = "AllyFriendlyFireTitle",
			InvitesTitle = "InvitesTitle",
			AllyInvites = "AllyInvites",
			ClanInvitesTitle = "ClanInvitesTitle",
			IncomingAllyTitle = "IncomingAllyTitle",
			LeaderTransferTitle = "LeaderTransferTitle",
			SelectSkinTitle = "SelectSkinTitle",
			EnterSkinTitle = "EnterSkinTitle",
			NotModer = "NotModer",
			SuccsessKick = "SuccsessKick",
			WasKicked = "WasKicked",
			NotClanMember = "NotClanMember",
			NotClanLeader = "NotClanLeader",
			AlreadyClanMember = "AlreadyClanMember",
			ClanTagLimit = "ClanTagLimit",
			ClanExists = "ClanExists",
			ClanCreated = "ClanCreated",
			ClanDisbandedTitle = "ClanDisbandedTitle",
			ClanLeft = "ClanLeft",
			PlayerNotFound = "PlayerNotFound",
			ClanNotFound = "ClanNotFound",
			ClanAlreadyModer = "ClanAlreadyModer",
			PromotedToModer = "PromotedToModer",
			NotClanModer = "NotClanModer",
			DemotedModer = "DemotedModer",
			FFOn = "FFOn",
			AllyFFOn = "AllyFFOn",
			FFOff = "FFOff",
			AllyFFOff = "AllyFFOff",
			Help = "Help",
			ModerHelp = "ModerHelp",
			AdminHelp = "AdminHelp",
			HeAlreadyClanMember = "HeAlreadyClanMember",
			AlreadyInvitedInClan = "AlreadyInvitedInClan",
			SuccessInvited = "SuccessInvited",
			SuccessInvitedSelf = "SuccessInvitedSelf",
			ClanJoined = "ClanJoined",
			WasInvited = "WasInvited",
			DeclinedInvite = "DeclinedInvite",
			DeclinedInviteSelf = "DeclinedInviteSelf",
			DidntReceiveInvite = "DidntReceiveInvite",
			YourInviteDeclined = "YourInviteDeclined",
			CancelledInvite = "CancelledInvite",
			CancelledYourInvite = "CancelledYourInvite",
			CannotDamage = "CannotDamage",
			AllyCannotDamage = "AllyCannotDamage",
			SetDescription = "SetDescription",
			MaxDescriptionSize = "MaxDescriptionSize",
			NotDescription = "NotDescription",
			ContainsForbiddenWords = "ContainsForbiddenWords",
			NoPermCreateClan = "NoPermCreateClan",
			NoPermJoinClan = "NoPermJoinClan",
			NoPermKickClan = "NoPermKickClan",
			NoPermLeaveClan = "NoPermLeaveClan",
			NoPermDisbandClan = "NoPermDisbandClan",
			NoPermClanSkins = "NoPermClanSkins",
			NoAllies = "NoAllies",
			NoInvites = "NoInvites",
			AllInviteExist = "AllInviteExist",
			AlreadyAlliance = "AlreadyAlliance",
			AllySendedInvite = "AllySendedInvite",
			YouAllySendedInvite = "YouAllySendedInvite",
			SelfAllySendedInvite = "SelfAllySendedInvite",
			NoFoundInviteAlly = "NoFoundInviteAlly",
			AllyAcceptInviteTitle = "AllyAcceptInviteTitle",
			RejectedInviteTitle = "RejectedInviteTitle",
			SelfRejectedInviteTitle = "SelfRejectedInviteTitle",
			WithdrawInviteTitle = "WithdrawInviteTitle",
			SelfWithdrawInviteTitle = "SelfWithdrawInviteTitle",
			SendAllyInvite = "SendAllyInvite",
			CancelAllyInvite = "CancelAllyInvite",
			WithdrawAllyInvite = "WithdrawAllyInvite",
			ALotOfMembers = "ALotOfMembers",
			ALotOfModers = "ALotOfModers",
			ALotOfAlliances = "ALotOfAlliances",
			NextBtn = "NextBtn",
			BackBtn = "BackBtn",
			NoAlly = "NoAlly",
			BreakAlly = "BreakAlly",
			SelfBreakAlly = "SelfBreakAlly",
			AllyRevokeTitle = "AllyRevokeTitle";

		protected override void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>
			{
				[ClansMenuTitle] = "Clans menu",
				[AboutClan] = "About Clan",
				[ChangeAvatar] = "Change avatar",
				[EnterLink] = "Enter link",
				[LeaderTitle] = "Leader",
				[GatherTitle] = "Gather",
				[RatingTitle] = "Rating",
				[MembersTitle] = "Members",
				[DescriptionTitle] = "Description",
				[NameTitle] = "Name",
				[SteamIdTitle] = "SteamID",
				[ProfileTitle] = "Profile",
				[InvitedToClan] = "You were invited to the clan",
				[BackPage] = "<",
				[NextPage] = ">",
				[TopClansTitle] = "Top Clans",
				[TopPlayersTitle] = "Top Players",
				[TopTitle] = "Top",
				[ScoreTitle] = "Score",
				[KillsTitle] = "Kills",
				[DeathsTitle] = "Deaths",
				[KDTitle] = "K/D",
				[ResourcesTitle] = "Resources",
				[LeftTitle] = "Left",
				[EditTitle] = "Edit",
				[InviteTitle] = "Invite",
				[SearchTitle] = "Search...",
				[ClanInvitation] = "Clan invitation",
				[InviterTitle] = "Inviter",
				[AcceptTitle] = "Accept",
				[CancelTitle] = "Cancel",
				[PlayerTitle] = "Player",
				[ClanTitle] = "Clan",
				[NotMemberOfClan] = "You are not a member of a clan :(",
				[SelectItemTitle] = "Select item",
				[CloseTitle] = "✕",
				[SelectTitle] = "Select",
				[ClanCreationTitle] = "Clan creation",
				[ClanNameTitle] = "Clan name",
				[AvatarTitle] = "Avatar",
				[UrlTitle] = "http://...",
				[CreateTitle] = "Create",
				[LastLoginTitle] = "Last login",
				[DemoteModerTitle] = "Demote moder",
				[PromoteModerTitle] = "Promote moder",
				[PromoteLeaderTitle] = "Promote leader",
				[KickTitle] = "Kick",
				[GatherRatesTitle] = "Gather rates",
				[CreateClanTitle] = "Create a clan",
				[FriendlyFireTitle] = "Friendly Fire",
				[AllyFriendlyFireTitle] = "Ally FF",
				[InvitesTitle] = "Invites",
				[AllyInvites] = "Ally Invites",
				[ClanInvitesTitle] = "Clan Invites",
				[IncomingAllyTitle] = "Incoming Ally",
				[LeaderTransferTitle] = "Leadership Transfer Confirmation",
				[SelectSkinTitle] = "Select skin",
				[EnterSkinTitle] = "Enter skin...",
				[NotModer] = "You are not a clan moderator!",
				[SuccsessKick] = "You have successfully kicked player '{0}' from the clan!",
				[WasKicked] = "You have been kicked from the clan :(",
				[NotClanMember] = "You are not a member of a clan!",
				[NotClanLeader] = "You are not a clan leader!",
				[AlreadyClanMember] = "You are already a member of the clan!",
				[ClanTagLimit] = "Clan tag must contain from {0} to {1} characters!",
				[ClanExists] = "Clan with that tag already exists!",
				[ClanCreated] = "Clan '{0}' has been successfully created!",
				[ClanDisbandedTitle] = "You have successfully disbanded the clan",
				[ClanLeft] = "You have successfully left the clan!",
				[PlayerNotFound] = "Player `{0}` not found!",
				[ClanNotFound] = "Clan `{0}` not found!",
				[ClanAlreadyModer] = "Player `{0}` is already a moderator!",
				[PromotedToModer] = "You've promoted `{0}` to moderator!",
				[NotClanModer] = "Player `{0}` is not a moderator!",
				[DemotedModer] = "You've demoted `{0}` to member!",
				[FFOn] = "Friendly Fire turned <color=#7FFF00>on</color>!",
				[AllyFFOn] = "Ally Friendly Fire turned <color=#7FFF00>on</color>!",
				[FFOff] = "Friendly Fire turned <color=#FF0000>off</color>!",
				[AllyFFOff] = "Ally Friendly Fire turned <color=#FF0000>off</color>!",
				[Help] =
					"Available commands:\n/clan - display clan menu\n/clan create \n/clan leave - Leave your clan\n/clan ff - Toggle friendlyfire status",
				[ModerHelp] =
					"\nModerator commands:\n/clan invite <name/steamid> - Invite a player\n/clan withdraw <name/steamid> - Cancel an invite\n/clan kick <name/steamid> - Kick a member\n/clan allyinvite <clanTag> - Invite the clan an alliance\n/clan allywithdraw <clanTag> - Cancel the invite of an alliance of clans\n/clan allyaccept <clanTag> - Accept the invite of an alliance with the clan\n/clan allycancel <clanTag> - Cancel the invite of an alliance with the clan\n/clan allyrevoke <clanTag> - Revoke an allyiance with the clan",
				[AdminHelp] =
					"\nOwner commands:\n/clan promote <name/steamid> - Promote a member\n/clan demote <name/steamid> - Demote a member\n/clan disband - Disband your clan",
				[HeAlreadyClanMember] = "The player is already a member of the clan.",
				[AlreadyInvitedInClan] = "The player has already been invited to your clan!",
				[SuccessInvited] = "You have successfully invited the player '{0}' to the '{1}' clan",
				[SuccessInvitedSelf] = "Player '{0}' invited you to the '{1}' clan",
				[ClanJoined] = "Congratulations! You have joined the clan '{0}'.",
				[WasInvited] = "Player '{0}' has accepted your invitation to the clan!",
				[DeclinedInvite] = "You have declined an invitation to join the '{0}' clan",
				[DeclinedInviteSelf] = "Player '{0}' declined the invitation to the clan!",
				[DidntReceiveInvite] = "Player `{0}` did not receive an invitation from your clan",
				[YourInviteDeclined] = "Your invitation to player '{0}' to the clan was declined by `{1}`",
				[CancelledInvite] = "Clan '{0}' canceled the invitation",
				[CancelledYourInvite] = "You canceled the invitation to the clan for the player '{0}'",
				[CannotDamage] = "You cannot damage your clanmates! (<color=#7FFF00>/clan ff</color>)",
				[AllyCannotDamage] = "You cannot damage your ally clanmates! (<color=#7FFF00>/clan allyff</color>)",
				[SetDescription] = "You have set a new clan description",
				[MaxDescriptionSize] = "The maximum number of characters for describing a clan is {0}",
				[NotDescription] = "Clan leader didn't set description",
				[ContainsForbiddenWords] = "The title contains forbidden words!",
				[NoPermCreateClan] = "You do not have permission to create a clan",
				[NoPermJoinClan] = "You do not have permission to join a clan",
				[NoPermKickClan] = "You do not have permission to kick clan members",
				[NoPermLeaveClan] = "You do not have permission to leave this clan",
				[NoPermDisbandClan] = "You do not have permission to disband this clan",
				[NoPermClanSkins] = "You do not have permission to use clan skins",
				[NoAllies] = "Unfortunately\nYou have no allies :(",
				[NoInvites] = "No invitations :(",
				[AllInviteExist] = "Invitation has already been sent to this clan",
				[AlreadyAlliance] = "You already have an alliance with this clan",
				[AllySendedInvite] = "'{0}' invited the '{1}' clan to join an alliance",
				[YouAllySendedInvite] = "You invited the '{0}' clan to join an alliance",
				[SelfAllySendedInvite] = "Clan '{0}' invited you to join an alliance",
				[NoFoundInviteAlly] = "'{0}' clan invitation not found",
				[AllyAcceptInviteTitle] = "You have formed an alliance with the '{0}' clan",
				[RejectedInviteTitle] = "Your clan has rejected an alliance invite from the '{0}' clan",
				[SelfRejectedInviteTitle] = "'{0}' clan rejects the alliance proposal",
				[WithdrawInviteTitle] = "Your clan has withdrawn an invitation to an alliance with the '{0}' clan",
				[SelfWithdrawInviteTitle] = "'{0}' clan withdrew invitation to alliance",
				[SendAllyInvite] = "Send Invite",
				[CancelAllyInvite] = "Cancel Invite",
				[WithdrawAllyInvite] = "Withdraw Invite",
				[ALotOfMembers] = "The clan has the maximum amount of players!",
				[ALotOfModers] = "The clan has the maximum amount of moderators!",
				[ALotOfAlliances] = "The clan has the maximum amount of alliances!",
				[NextBtn] = "▼",
				[BackBtn] = "▲",
				[NoAlly] = "You have no alliance with the '{0}' clan",
				[SelfBreakAlly] = "Your clan has breaking its alliance with the '{0}' clan",
				[BreakAlly] = "Clan '{0}' broke an alliance with your clan",
				[AllyRevokeTitle] = "Revoke Ally",
				[UseClanSkins] = "Use clan skins",
				[AdminDisbandClan] = "An administrator has disbanded your clan",
				[AdminDemote] = "An administrator has demoted {0} to member",
				[AdminPromote] = "An administrator has promoted {0} to moderator",
				[AdminInvite] = "An administrator has invited {0} to join your clan",
				[AdminKick] = "An administrator has kicked you from <color=#74884A>[{0}]</color>",
				[AdminKickBroadcast] = "An administrator has kicked <color=#B43D3D>[{0}]</color> from your clan",
				[AdminJoin] = "An administrator has forced you to join <color=#74884A>[{0}]</color>",
				[AdminBroadcast] = "<color=#B43D3D>[ADMIN]</color>: {0}",
				[AdminSetLeader] = "An administrator has set {0} as the clan leader",
				[AdminRename] = "An administrator changed your clan tag to <color=#74884A>[{0}]</color>",
				[ClanInfoTitle] =
					"<size=18><color=#ffa500>Clans</color></size>",
				[ClanInfoTag] = "\nClanTag: <color=#b2eece>{0}</color>",
				[ClanInfoDescription] = "\nDescription: <color=#b2eece>{0}</color>",
				[ClanInfoOnline] = "\nMembers Online: {0}",
				[ClanInfoOffline] = "\nMembers Offline: {0}",
				[ClanInfoEstablished] = "\nEstablished: <color=#b2eece>{0}</color>",
				[ClanInfoLastOnline] = "\nLast Online: <color=#b2eece>{0}</color>",
				[ClanInfoAlliances] = "\nAlliances: <color=#b2eece>{0}</color>",
				[ClanInfoAlliancesNone] = "None",
				[NoPermissions] = "You have insufficient permission to use that command",
				[TagColorFormat] = "The hex string must be 6 characters long, and be a valid hex color",
				[TagColorInstalled] = "You have set a new clan tag color: #{0}!",
				[TagColorTitle] = "Tag Color",
				[PlayTimeTitle] = "Play Time",
				["aboutclan"] = "About Clan",
				["memberslist"] = "Members",
				["clanstop"] = "Top Clans",
				["playerstop"] = "Top Players",
				["resources"] = "Gather Rates",
				["skins"] = "Skins",
				["playerslist"] = "Players List",
				["alianceslist"] = "Aliances"
			}, this);
		}

		private string Msg(BasePlayer player, string key, params object[] obj)
		{
			return string.Format(lang.GetMessage(key, this, player.UserIDString), obj);
		}

		private void Reply(BasePlayer player, string key, params object[] obj)
		{
			if (player == null) return;

			SendReply(player, Msg(player, key, obj));
		}

		private void Reply(IPlayer player, string key, params object[] obj)
		{
			player?.Reply(string.Format(lang.GetMessage(key, this, player.Id), obj));
		}

		#endregion

		#region Convert

		#region uMod Clans

		private readonly DateTime _epoch = new DateTime(1970, 1, 1);

		private readonly double _maxUnixSeconds = (DateTime.MaxValue - new DateTime(1970, 1, 1)).TotalSeconds;

		[ConsoleCommand("clans.convert")]
		private void CmdConsoleConvertOldClans(ConsoleSystem.Arg arg)
		{
			if (!arg.IsAdmin) return;

			_actionConvert = ServerMgr.Instance.StartCoroutine(ConvertOldClans());
		}

		private IEnumerator ConvertOldClans()
		{
			OldStoredData oldClans = null;

			try
			{
				oldClans = Interface.Oxide.DataFileSystem.ReadObject<OldStoredData>("Clans");
			}
			catch (Exception e)
			{
				PrintError(e.ToString());
			}

			if (oldClans == null) yield break;

			foreach (var check in oldClans.clans)
			{
				var newClan = new ClanData
				{
					ClanTag = check.Key,
					LeaderID = check.Value.OwnerID,
					LeaderName = covalence.Players.FindPlayer(check.Value.OwnerID.ToString())?.Name,
					Avatar = _config.DefaultAvatar,
					Members = check.Value.ClanMembers.Keys.ToList(),
					Moderators = check.Value.ClanMembers.Where(x => x.Value.Role == MemberRole.Moderator)
						.Select(x => x.Key).ToList(),
					Top = TopClans.Count,
					CreationTime = ConvertOldTime(check.Value.CreationTime),
					LastOnlineTime = ConvertOldTime(check.Value.LastOnlineTime)
				};

				if (_config.AutoTeamCreation)
				{
					var leader = BasePlayer.FindByID(check.Value.OwnerID) ??
					             BasePlayer.FindSleeping(check.Value.OwnerID);
					if (leader != null) newClan.FindOrCreateTeam();
				}

				_clansList.Add(newClan);
				_clanByTag[newClan.ClanTag] = newClan;

				yield return null;
			}

			yield return CoroutineEx.waitForFixedUpdate;

			Puts($"{oldClans.clans.Count} clans was converted!");
		}

		private DateTime ConvertOldTime(double lastTime)
		{
			return lastTime > _maxUnixSeconds
				? _epoch.AddMilliseconds(lastTime)
				: _epoch.AddSeconds(lastTime);
		}

		private class OldStoredData
		{
			public readonly Hash<string, OldClan> clans = new Hash<string, OldClan>();

			public int timeSaved;

			public Hash<ulong, List<string>> playerInvites = new Hash<ulong, List<string>>();
		}

		private class OldClan
		{
			public string Tag;

			public string Description;

			public ulong OwnerID;

			public double CreationTime;

			public double LastOnlineTime;

			public readonly Hash<ulong, OldMember> ClanMembers = new Hash<ulong, OldMember>();

			public HashSet<string> Alliances = new HashSet<string>();

			public Hash<string, double> AllianceInvites = new Hash<string, double>();

			public HashSet<string> IncomingAlliances = new HashSet<string>();

			public string TagColor = string.Empty;
		}

		private class OldMember
		{
			public string DisplayName = string.Empty;

			public MemberRole Role;

			public bool MemberFFEnabled;

			public bool AllyFFEnabled;
		}

		private enum MemberRole
		{
			Owner,
			Council,
			Moderator,
			Member
		}

		#endregion

		#region Data 2.0

		[ConsoleCommand("clans.convert.olddata")]
		private void CmdConvertOldData(ConsoleSystem.Arg arg)
		{
			if (!arg.IsAdmin) return;

			StartConvertOldData();
		}

		private void StartConvertOldData()
		{
			var data = LoadOldData();
			if (data != null)
				timer.In(0.3f, () =>
				{
					CondertOldData(data);

					PrintWarning($"{data.Count} players was converted!");
				});
		}

		private Dictionary<ulong, OldData> LoadOldData()
		{
			Dictionary<ulong, OldData> players = null;
			try
			{
				players = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, OldData>>($"{Name}/PlayersList");
			}
			catch (Exception e)
			{
				PrintError(e.ToString());
			}

			return players ?? new Dictionary<ulong, OldData>();
		}

		private void CondertOldData(Dictionary<ulong, OldData> players)
		{
			foreach (var check in players)
			{
				var userId = check.Key.ToString();

				var data = PlayerData.GetOrCreate(userId);
				data.LastWipe = DateTime.UtcNow;
				data.SteamID = userId;
				data.DisplayName = check.Value.DisplayName;
				data.LastLogin = check.Value.LastLogin;
				data.FriendlyFire = check.Value.FriendlyFire;
				data.AllyFriendlyFire = check.Value.AllyFriendlyFire;
				data.ClanSkins = check.Value.ClanSkins;
				data.Stats = check.Value.Stats;

				PlayerData.SaveAndUnload(userId);
			}
		}

		private class OldData
		{
			[JsonProperty(PropertyName = "Display Name")]
			public string DisplayName;

			[JsonProperty(PropertyName = "Last Login")]
			public DateTime LastLogin;

			[JsonProperty(PropertyName = "Friendly Fire")]
			public bool FriendlyFire;

			[JsonProperty(PropertyName = "Ally Friendly Fire")]
			public bool AllyFriendlyFire;

			[JsonProperty(PropertyName = "Use Clan Skins")]
			public bool ClanSkins;

			[JsonProperty(PropertyName = "Stats", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<string, float> Stats = new Dictionary<string, float>();

			[JsonProperty(PropertyName = "Invites", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<string, InviteData> Invites = new Dictionary<string, InviteData>();
		}

		#endregion

		#endregion

		#region Data 2.0

		#region Player

		private Dictionary<string, PlayerData> _usersData = new Dictionary<string, PlayerData>();

		private class PlayerData
		{
			#region Main

			#region Fields

			[JsonProperty(PropertyName = "Display Name")]
			public string DisplayName;

			[JsonProperty(PropertyName = "Steam ID")]
			public string SteamID;

			[JsonProperty(PropertyName = "Last Login")]
			public DateTime LastLogin;

			[JsonProperty(PropertyName = "Friendly Fire")]
			public bool FriendlyFire;

			[JsonProperty(PropertyName = "Ally Friendly Fire")]
			public bool AllyFriendlyFire;

			[JsonProperty(PropertyName = "Use Clan Skins")]
			public bool ClanSkins;

			[JsonProperty(PropertyName = "Stats", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<string, float> Stats = new Dictionary<string, float>();

			#endregion

			#region Stats

			[JsonIgnore]
			public float Kills
			{
				get
				{
					float kills;
					Stats.TryGetValue("kills", out kills);
					return float.IsNaN(kills) || float.IsInfinity(kills) ? 0 : kills;
				}
			}

			[JsonIgnore]
			public float Deaths
			{
				get
				{
					float deaths;
					Stats.TryGetValue("deaths", out deaths);
					return float.IsNaN(deaths) || float.IsInfinity(deaths) ? 0 : deaths;
				}
			}

			[JsonIgnore]
			public float KD
			{
				get
				{
					var kd = Kills / Deaths;
					return float.IsNaN(kd) || float.IsInfinity(kd) ? 0 : kd;
				}
			}

			[JsonIgnore]
			public float Resources
			{
				get
				{
					var resources = Stats.Where(x => _config.Resources.Contains(x.Key)).Sum(x => x.Value);
					return float.IsNaN(resources) || float.IsInfinity(resources) ? 0 : resources;
				}
			}

			[JsonIgnore]
			public float Score
			{
				get
				{
					return (float) Math.Round(Stats
						.Where(x => _config.ScoreTable.ContainsKey(x.Key))
						.Sum(x => x.Value * _config.ScoreTable[x.Key]));
				}
			}

			public float GetValue(string key)
			{
				float val;
				Stats.TryGetValue(key, out val);
				return float.IsNaN(val) || float.IsInfinity(val) ? 0 : Mathf.Round(val);
			}

			public float GetTotalFarm(ClanData clan)
			{
				return (float) Math.Round(
					clan.ResourceStandarts.Values.Sum(check =>
					{
						var result = GetValue(check.ShortName) / check.Amount;
						return result > 1 ? 1 : result;
					}) /
					clan.ResourceStandarts.Count, 3);
			}

			public string GetParams(string key, ClanData clan)
			{
				switch (key)
				{
					case "gather":
					{
						var progress = GetTotalFarm(clan);
						return $"{(progress > 0 ? Math.Round(progress * 100f) : 0)}";
					}

					case "lastlogin":
					{
						return $"{LastLogin:g}";
					}

					case "playtime":
					{
						return $"{FormatTime(_instance.GetPlayTime(SteamID))}";
					}

					case "score":
					{
						return Score.ToString(CultureInfo.InvariantCulture);
					}

					case "kills":
					{
						return Kills.ToString(CultureInfo.InvariantCulture);
					}

					case "deaths":
					{
						return Deaths.ToString(CultureInfo.InvariantCulture);
					}

					case "kd":
					{
						return KD.ToString(CultureInfo.InvariantCulture);
					}

					default:
						return GetValue(key).ToString(CultureInfo.InvariantCulture);
				}
			}

			#endregion

			#region Utils

			public ClanData GetClan()
			{
				ClanData clan;
				if (_instance._playerToClan.TryGetValue(SteamID, out clan))
					return clan;

				if ((clan = _instance.FindClanByUserID(SteamID)) != null)
					return _instance._playerToClan[SteamID] = clan;

				return null;
			}

			public ClanInviteData GetInviteByTag(string clanTag)
			{
				return _invites.GetClanInvite(ulong.Parse(SteamID), clanTag);
			}

			#endregion

			#endregion

			#region Data.Helpers

			private static string BaseFolder()
			{
				return "Clans" + Path.DirectorySeparatorChar + "Players" + Path.DirectorySeparatorChar;
			}

			public static PlayerData GetOrLoad(string userId)
			{
				if (!userId.IsSteamId()) return null;

				if (_config.PlayerDatabase.Enabled) return _instance.LoadPlayerDatabaseData(userId);

				var data = GetOrLoad(BaseFolder(), userId);

				TryToWipe(userId, ref data);

				return data;
			}

			public static PlayerData GetOrLoad(string baseFolder, string userId, bool load = true)
			{
				PlayerData data;
				if (_instance._usersData.TryGetValue(userId, out data)) return data;

				try
				{
					data = ReadOnlyObject(baseFolder + userId);
				}
				catch (Exception e)
				{
					Interface.Oxide.LogError(e.ToString());
				}

				return load
					? _instance._usersData[userId] = data
					: data;
			}

			public static PlayerData GetOrCreate(string userId)
			{
				if (!userId.IsSteamId()) return null;

				if (_config.PlayerDatabase.Enabled) return _instance.LoadPlayerDatabaseData(userId);

				return GetOrLoad(userId) ?? (_instance._usersData[userId] = new PlayerData
				{
					SteamID = userId,
					ClanSkins = _config.Skins.DefaultValueDisableSkins,
					FriendlyFire = _config.FriendlyFire,
					AllyFriendlyFire = _config.AllianceSettings.DefaultFF
				});
			}

			public static bool IsLoaded(string userId)
			{
				return _instance._usersData.ContainsKey(userId);
			}

			public static void Save()
			{
				_instance?._usersData?.Keys.ToList().ForEach(Save);
			}

			public static void Save(string userId)
			{
				if (_config.PlayerDatabase.Enabled)
				{
					_instance.SaveData(userId, _instance.LoadPlayerDatabaseData(userId));
					return;
				}

				PlayerData data;
				if (!_instance._usersData.TryGetValue(userId, out data))
					return;

				Interface.Oxide.DataFileSystem.WriteObject(BaseFolder() + userId, data);
			}

			public static void SaveAndUnload(string userId)
			{
				Save(userId);

				Unload(userId);
			}

			public static void Unload(string userId)
			{
				_instance._usersData.Remove(userId);
			}

			#endregion

			#region Data.Utils

			public static string[] GetFiles(string baseFolder)
			{
				try
				{
					var json = ".json".Length;
					var paths = Interface.Oxide.DataFileSystem.GetFiles(baseFolder);
					for (var i = 0; i < paths.Length; i++)
					{
						var path = paths[i];
						var separatorIndex = path.LastIndexOf(Path.DirectorySeparatorChar);

						// We have to do this since GetFiles returns paths instead of filenames
						// And other methods require filenames
						paths[i] = path.Substring(separatorIndex + 1, path.Length - separatorIndex - 1 - json);
					}

					return paths;
				}
				catch
				{
					return Array.Empty<string>();
				}
			}

			private static PlayerData ReadOnlyObject(string name)
			{
				return Interface.Oxide.DataFileSystem.ExistsDatafile(name)
					? Interface.Oxide.DataFileSystem.GetFile(name).ReadObject<PlayerData>()
					: null;
			}

			#endregion

			#region Data.Wipe

			[JsonProperty(PropertyName = "Last Wipe")]
			public DateTime LastWipe;

			public static void TryToWipe(string userId, ref PlayerData data)
			{
				if (_config.PurgeSettings.WipeOnNewSave && data != null &&
				    SaveRestore.SaveCreatedTime.ToUniversalTime() > data.LastWipe.ToUniversalTime())
				{
					_instance._usersData[userId] = data = new PlayerData
					{
						LastWipe = DateTime.UtcNow,
						SteamID = userId,
						ClanSkins = _config.Skins.DefaultValueDisableSkins,
						FriendlyFire = _config.FriendlyFire,
						AllyFriendlyFire = _config.AllianceSettings.DefaultFF
					};

					Save(userId);
				}
			}

			#endregion

			#region All Players

			public static void StartAll(Action<PlayerData> action)
			{
				var users = GetFiles(BaseFolder());

				foreach (var userId in users)
				{
					var loaded = _instance._usersData.ContainsKey(userId);

					var data = GetOrLoad(userId);
					if (data == null) continue;

					action.Invoke(data);

					Save(userId);

					if (!loaded)
						Unload(userId);
				}
			}

			public static List<PlayerData> GetAll()
			{
				var users = GetFiles(BaseFolder());

				var list = new List<PlayerData>();

				foreach (var userId in users)
				{
					var data = GetOrLoad(userId);
					if (data == null) continue;

					list.Add(data);
				}

				return list;
			}

			public static IEnumerator TopCoroutine()
			{
				var users =
					_config.PlayerDatabase.Enabled
						? _instance.covalence.Players.All.Select(x => x.Id).ToArray()
						: GetFiles(BaseFolder());

				yield return CoroutineEx.waitForFixedUpdate;

				var list = new List<PlayerData>();

				for (var i = 0; i < users.Length; i++)
				{
					var data = GetOrLoad(users[i]);
					if (data == null) continue;

					list.Add(data);

					if (i % 100 == 0)
						yield return CoroutineEx.waitForFixedUpdate;
				}

				yield return CoroutineEx.waitForFixedUpdate;

				_instance.SortPlayers(list.Select(x => new TopPlayerData(x)));

				yield return CoroutineEx.waitForFixedUpdate;

				_instance.SortClans();

				ClanTopUpdated();

				_instance._handleTop = null;
			}

			#endregion
		}

		#region PlayerDatabase

		private PlayerData LoadPlayerDatabaseData(string userId)
		{
			PlayerData data;
			if (_usersData.TryGetValue(userId, out data))
				return data;

			var success =
				PlayerDatabase?.Call<string>("GetPlayerDataRaw", userId, _config.PlayerDatabase.Field);
			if (string.IsNullOrEmpty(success))
			{
				data = new PlayerData
				{
					SteamID = userId,
					ClanSkins = _config.Skins.DefaultValueDisableSkins,
					FriendlyFire = _config.FriendlyFire,
					AllyFriendlyFire = _config.AllianceSettings.DefaultFF
				};

				SaveData(userId, data);
				return _usersData[userId] = data;
			}

			if ((data = JsonConvert.DeserializeObject<PlayerData>(success)) == null)
			{
				data = new PlayerData
				{
					SteamID = userId,
					ClanSkins = _config.Skins.DefaultValueDisableSkins,
					FriendlyFire = _config.FriendlyFire,
					AllyFriendlyFire = _config.AllianceSettings.DefaultFF
				};

				SaveData(userId, data);
				return _usersData[userId] = data;
			}

			return _usersData[userId] = data;
		}

		private void SaveData(string userId, PlayerData data)
		{
			if (data == null) return;

			var serializeObject = JsonConvert.SerializeObject(data);
			if (serializeObject == null) return;

			PlayerDatabase?.Call("SetPlayerData", userId, _config.PlayerDatabase.Field, serializeObject);
		}

		#endregion

		#endregion

		#region Invites

		private static InvitesData _invites;

		private void SaveInvites()
		{
			Interface.Oxide.DataFileSystem.WriteObject($"{Name}/Invites", _invites);
		}

		private void LoadInvites()
		{
			try
			{
				_invites = Interface.Oxide.DataFileSystem.ReadObject<InvitesData>($"{Name}/Invites");
			}
			catch (Exception e)
			{
				PrintError(e.ToString());
			}

			if (_invites == null) _invites = new InvitesData();
		}

		private class InvitesData
		{
			#region Player Invites

			[JsonProperty(PropertyName = "Player Invites", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<ClanInviteData> PlayersInvites =
				new List<ClanInviteData>();

			public bool CanSendInvite(ulong userId, string clanTag)
			{
				return !PlayersInvites.Exists(invite => invite.RetrieverId == userId && invite.ClanTag == clanTag);
			}

			public ClanInviteData GetClanInvite(ulong userId, string clanTag)
			{
				return PlayersInvites.Find(x => x.RetrieverId == userId && x.ClanTag == clanTag);
			}

			public List<ClanInviteData> GetPlayerClanInvites(ulong userId)
			{
				return PlayersInvites.FindAll(x => x.RetrieverId == userId);
			}

			public List<ClanInviteData> GetClanPlayersInvites(string clanTag)
			{
				return PlayersInvites.FindAll(x => x.ClanTag == clanTag);
			}

			public void AddPlayerInvite(ulong userId, ulong senderId, string senderName, string clanTag)
			{
				PlayersInvites.Add(new ClanInviteData
				{
					InviterId = senderId,
					InviterName = senderName,
					RetrieverId = userId,
					ClanTag = clanTag
				});
			}

			public void RemovePlayerInvites(ulong userId)
			{
				PlayersInvites.RemoveAll(x => x.RetrieverId == userId);
			}

			public void RemovePlayerClanInvites(string tag)
			{
				PlayersInvites.RemoveAll(x => x.ClanTag == tag);
			}

			public void RemovePlayerClanInvites(ClanInviteData data)
			{
				PlayersInvites.Remove(data);
			}

			#endregion

			#region Alliance Invites

			[JsonProperty(PropertyName = "Alliance Invites", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<AllyInviteData> AllianceInvites =
				new List<AllyInviteData>();

			public List<AllyInviteData> GetAllyTargetInvites(string clanTag)
			{
				return AllianceInvites.FindAll(invite => invite.SenderClanTag == clanTag);
			}

			public List<AllyInviteData> GetAllyIncomingInvites(string clanTag)
			{
				return AllianceInvites.FindAll(invite => invite.TargetClanTag == clanTag);
			}

			public bool CanSendAllyInvite(string senderClanTag, string retrivierClanTag)
			{
				if (AllianceInvites.Exists(invite =>
					    invite.TargetClanTag == retrivierClanTag &&
					    invite.SenderClanTag == senderClanTag))
					return false;

				return true;
			}

			public void AddAllyInvite(ulong senderId, string senderName, string senderClanTag, string retrivierClanTag)
			{
				AllianceInvites.Add(new AllyInviteData
				{
					SenderId = senderId,
					SenderName = senderName,
					SenderClanTag = senderClanTag,
					TargetClanTag = retrivierClanTag
				});
			}

			public void RemoveAllyInvite(string retrivierClanTag)
			{
				AllianceInvites.RemoveAll(invite => invite.TargetClanTag == retrivierClanTag);
			}

			public void RemoveAllyInviteByClan(string retrivierClanTag, string senderClan)
			{
				AllianceInvites.RemoveAll(invite =>
					invite.TargetClanTag == retrivierClanTag &&
					invite.SenderClanTag == senderClan);
			}

			#endregion
		}

		private class AllyInviteData
		{
			[JsonProperty(PropertyName = "Sender ID")]
			public ulong SenderId;

			[JsonProperty(PropertyName = "Sender Name")]
			public string SenderName;

			[JsonProperty(PropertyName = "Sender Clan Tag")]
			public string SenderClanTag;

			[JsonProperty(PropertyName = "Retriever Clan Tag")]
			public string TargetClanTag;
		}

		private class ClanInviteData
		{
			[JsonProperty(PropertyName = "Inviter ID")]
			public ulong InviterId;

			[JsonProperty(PropertyName = "Inviter Name")]
			public string InviterName;

			[JsonProperty(PropertyName = "Retriever ID")]
			public ulong RetrieverId;

			[JsonProperty(PropertyName = "Clan Tag")]
			public string ClanTag;
		}

		#endregion

		#endregion

		#region Testing functions

#if TESTING
		private void DebugMessage(string format, long time)
		{
			PrintWarning(format, time);
		}

		private class StopwatchWrapper : IDisposable
		{
			public StopwatchWrapper(string format)
			{
				Sw = Stopwatch.StartNew();
				Format = format;
			}

			public static Action<string, long> OnComplete { private get; set; }

			private string Format { get; }
			private Stopwatch Sw { get; }

			public long Time { get; private set; }

			public void Dispose()
			{
				Sw.Stop();
				Time = Sw.ElapsedMilliseconds;
				OnComplete(Format, Time);
			}
		}

#endif

		#endregion
	}
}

#region Extension Methods

namespace Oxide.Plugins.ClansExtensionMethods
{
	// ReSharper disable ForCanBeConvertedToForeach
	// ReSharper disable LoopCanBeConvertedToQuery
	public static class ExtensionMethods
	{
		internal static Permission p;

		public static bool All<T>(this IList<T> a, Func<T, bool> b)
		{
			for (var i = 0; i < a.Count; i++)
				if (!b(a[i]))
					return false;
			return true;
		}

		public static int Average(this IList<int> a)
		{
			if (a.Count == 0) return 0;
			var b = 0;
			for (var i = 0; i < a.Count; i++) b += a[i];
			return b / a.Count;
		}

		public static T ElementAt<T>(this IEnumerable<T> a, int b)
		{
			using (var c = a.GetEnumerator())
			{
				while (c.MoveNext())
				{
					if (b == 0) return c.Current;
					b--;
				}
			}

			return default(T);
		}

		public static bool Exists<T>(this IEnumerable<T> a, Func<T, bool> b = null)
		{
			using (var c = a.GetEnumerator())
			{
				while (c.MoveNext())
					if (b == null || b(c.Current))
						return true;
			}

			return false;
		}

		public static T FirstOrDefault<T>(this IEnumerable<T> a, Func<T, bool> b = null)
		{
			using (var c = a.GetEnumerator())
			{
				while (c.MoveNext())
					if (b == null || b(c.Current))
						return c.Current;
			}

			return default(T);
		}

		public static int RemoveAll<T, V>(this IDictionary<T, V> a, Func<T, V, bool> b)
		{
			var c = new List<T>();
			using (var d = a.GetEnumerator())
			{
				while (d.MoveNext())
					if (b(d.Current.Key, d.Current.Value))
						c.Add(d.Current.Key);
			}

			c.ForEach(e => a.Remove(e));
			return c.Count;
		}

		public static List<TResult> Select<T, TResult>(this List<T> source, Func<T, TResult> selector)
		{
			if (source == null || selector == null) return new List<TResult>();

			var r = new List<TResult>(source.Count);
			for (var i = 0; i < source.Count; i++) r.Add(selector(source[i]));

			return r;
		}

		public static List<TResult> Select<T, TResult>(this IEnumerable<T> source, Func<T, TResult> selector)
		{
			if (source == null || selector == null) return new List<TResult>();

			var r = new List<TResult>();

			using (var item = source.GetEnumerator())
			{
				while (item.MoveNext())
				{
					var converted = selector(item.Current);
					if (converted != null)
						r.Add(converted);
				}
			}

			return r;
		}

		public static string[] Skip(this string[] a, int count)
		{
			if (a.Length == 0) return Array.Empty<string>();
			var c = new string[a.Length - count];
			var n = 0;
			for (var i = 0; i < a.Length; i++)
			{
				if (i < count) continue;
				c[n] = a[i];
				n++;
			}

			return c;
		}

		public static List<T> Skip<T>(this IList<T> source, int count)
		{
			if (count < 0)
				count = 0;

			if (source == null || count > source.Count)
				return new List<T>();

			var result = new List<T>(source.Count - count);
			for (var i = count; i < source.Count; i++)
				result.Add(source[i]);
			return result;
		}

		public static Dictionary<T, V> Skip<T, V>(
			this IDictionary<T, V> source,
			int count)
		{
			var result = new Dictionary<T, V>();
			using (var iterator = source.GetEnumerator())
			{
				for (var i = 0; i < count; i++)
					if (!iterator.MoveNext())
						break;

				while (iterator.MoveNext()) result.Add(iterator.Current.Key, iterator.Current.Value);
			}

			return result;
		}

		public static List<T> Take<T>(this IList<T> a, int b)
		{
			var c = new List<T>();
			for (var i = 0; i < a.Count; i++)
			{
				if (c.Count == b) break;
				c.Add(a[i]);
			}

			return c;
		}

		public static Dictionary<T, V> Take<T, V>(this IDictionary<T, V> a, int b)
		{
			var c = new Dictionary<T, V>();
			foreach (var f in a)
			{
				if (c.Count == b) break;
				c.Add(f.Key, f.Value);
			}

			return c;
		}

		public static Dictionary<T, V> ToDictionary<S, T, V>(this IEnumerable<S> a, Func<S, T> b, Func<S, V> c)
		{
			var d = new Dictionary<T, V>();
			using (var e = a.GetEnumerator())
			{
				while (e.MoveNext()) d[b(e.Current)] = c(e.Current);
			}

			return d;
		}

		public static List<T> ToList<T>(this IEnumerable<T> a)
		{
			var b = new List<T>();
			using (var c = a.GetEnumerator())
			{
				while (c.MoveNext()) b.Add(c.Current);
			}

			return b;
		}

		public static HashSet<T> ToHashSet<T>(this IEnumerable<T> a)
		{
			return new HashSet<T>(a);
		}

		public static List<T> Where<T>(this List<T> source, Predicate<T> predicate)
		{
			if (source == null)
				return new List<T>();

			if (predicate == null)
				return new List<T>();

			return source.FindAll(predicate);
		}

		public static List<T> Where<T>(this List<T> source, Func<T, int, bool> predicate)
		{
			if (source == null)
				return new List<T>();

			if (predicate == null)
				return new List<T>();

			var r = new List<T>();
			for (var i = 0; i < source.Count; i++)
				if (predicate(source[i], i))
					r.Add(source[i]);
			return r;
		}

		public static List<T> Where<T>(this IEnumerable<T> source, Func<T, bool> predicate)
		{
			var c = new List<T>();

			using (var d = source.GetEnumerator())
			{
				while (d.MoveNext())
					if (predicate(d.Current))
						c.Add(d.Current);
			}

			return c;
		}

		public static List<T> OfType<T>(this IEnumerable<BaseNetworkable> a) where T : BaseEntity
		{
			var b = new List<T>();
			using (var c = a.GetEnumerator())
			{
				while (c.MoveNext())
					if (c.Current is T)
						b.Add(c.Current as T);
			}

			return b;
		}

		public static int Sum<T>(this IList<T> a, Func<T, int> b)
		{
			var c = 0;
			for (var i = 0; i < a.Count; i++)
			{
				var d = b(a[i]);
				if (!float.IsNaN(d)) c += d;
			}

			return c;
		}

		public static int Sum(this IList<int> a)
		{
			var c = 0;
			for (var i = 0; i < a.Count; i++)
			{
				var d = a[i];
				if (!float.IsNaN(d)) c += d;
			}

			return c;
		}

		public static bool HasPermission(this string a, string b)
		{
			if (p == null) p = Interface.Oxide.GetLibrary<Permission>();
			return !string.IsNullOrEmpty(a) && p.UserHasPermission(a, b);
		}

		public static bool HasPermission(this BasePlayer a, string b)
		{
			return a.UserIDString.HasPermission(b);
		}

		public static bool HasPermission(this ulong a, string b)
		{
			return a.ToString().HasPermission(b);
		}

		public static bool IsReallyConnected(this BasePlayer a)
		{
			return a.IsReallyValid() && a.net.connection != null;
		}

		public static bool IsKilled(this BaseNetworkable a)
		{
			return (object) a == null || a.IsDestroyed;
		}

		public static bool IsNull<T>(this T a) where T : class
		{
			return a == null;
		}

		public static bool IsNull(this BasePlayer a)
		{
			return (object) a == null;
		}

		public static bool IsReallyValid(this BaseNetworkable a)
		{
			return !((object) a == null || a.IsDestroyed || a.net == null);
		}

		public static void SafelyKill(this BaseNetworkable a)
		{
			if (a.IsKilled()) return;
			a.Kill();
		}

		public static bool CanCall(this Plugin o)
		{
			return o != null && o.IsLoaded;
		}

		public static bool IsInBounds(this OBB o, Vector3 a)
		{
			return o.ClosestPoint(a) == a;
		}

		public static bool IsHuman(this BasePlayer a)
		{
			return !(a.IsNpc || !a.userID.IsSteamId());
		}

		public static BasePlayer ToPlayer(this IPlayer user)
		{
			return user.Object as BasePlayer;
		}

		public static List<TResult> SelectMany<TSource, TResult>(this List<TSource> source,
			Func<TSource, List<TResult>> selector)
		{
			if (source == null || selector == null)
				return new List<TResult>();

			var result = new List<TResult>(source.Count);
			source.ForEach(i => selector(i).ForEach(j => result.Add(j)));
			return result;
		}

		public static IEnumerable<TResult> SelectMany<TSource, TResult>(
			this IEnumerable<TSource> source,
			Func<TSource, IEnumerable<TResult>> selector)
		{
			using (var item = source.GetEnumerator())
			{
				while (item.MoveNext())
					using (var result = selector(item.Current).GetEnumerator())
					{
						while (result.MoveNext()) yield return result.Current;
					}
			}
		}

		public static int Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
		{
			var sum = 0;

			using (var element = source.GetEnumerator())
			{
				while (element.MoveNext()) sum += selector(element.Current);
			}

			return sum;
		}

		public static double Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
		{
			var sum = 0.0;

			using (var element = source.GetEnumerator())
			{
				while (element.MoveNext()) sum += selector(element.Current);
			}

			return sum;
		}

		public static float Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
		{
			var sum = 0f;

			using (var element = source.GetEnumerator())
			{
				while (element.MoveNext()) sum += selector(element.Current);
			}

			return sum;
		}

		public static bool Any<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
		{
			if (source == null) return false;

			using (var element = source.GetEnumerator())
			{
				while (element.MoveNext())
					if (predicate(element.Current))
						return true;
			}

			return false;
		}

		public static int Count<TSource>(this IEnumerable<TSource> source)
		{
			if (source == null) return 0;

			var collectionOfT = source as ICollection<TSource>;
			if (collectionOfT != null)
				return collectionOfT.Count;

			var collection = source as ICollection;
			if (collection != null)
				return collection.Count;

			var count = 0;
			using (var e = source.GetEnumerator())
			{
				checked
				{
					while (e.MoveNext()) count++;
				}
			}

			return count;
		}

		public static List<TSource> OrderByDescending<TSource, TKey>(this List<TSource> source,
			Func<TSource, TKey> keySelector, IComparer<TKey> comparer = null)
		{
			if (source == null) return new List<TSource>();

			if (keySelector == null) return new List<TSource>();

			if (comparer == null) comparer = Comparer<TKey>.Default;

			var result = new List<TSource>(source);
			var lambdaComparer = new ReverseLambdaComparer<TSource, TKey>(keySelector, comparer);
			result.Sort(lambdaComparer);
			return result;
		}

		internal sealed class ReverseLambdaComparer<T, U> : IComparer<T>
		{
			private IComparer<U> comparer;
			private Func<T, U> selector;

			public ReverseLambdaComparer(Func<T, U> selector, IComparer<U> comparer)
			{
				this.comparer = comparer;
				this.selector = selector;
			}

			public int Compare(T x, T y)
			{
				return comparer.Compare(selector(y), selector(x));
			}
		}
	}
}

#endregion Extension Methods