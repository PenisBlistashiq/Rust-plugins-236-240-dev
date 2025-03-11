using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("DeathMessages", "https://topplugin.ru / https://discord.com/invite/5DPTsRmd3G", "1.3.4")]
    [Description("DeathMessages")]
    public class DeathMessages : RustPlugin
    {
        #region Var's
        [PluginReference]
        private Plugin ImageLibrary, Clans;

        private bool AddImage(string url, string imageName, ulong imageId = 0, Action callback = null) => (bool)ImageLibrary.Call("AddImage", url, imageName, imageId, callback);
        private string GetImage(string imageName, ulong imageId = 0, bool returnUrl = false) => (string)ImageLibrary.Call("GetImage", imageName, imageId, returnUrl);

        private Dictionary<BaseEntity, HitInfo> lastHitInfo = new Dictionary<BaseEntity, HitInfo>();
        private Dictionary<uint, string> prefabID2Item = new Dictionary<uint, string>();
        private Dictionary<string, string> prefabName2Item = new Dictionary<string, string>()
        {
            ["40mm_grenade_he"] = "multiplegrenadelauncher",
            ["grenade.beancan.deployed"] = "grenade.beancan",
            ["grenade.f1.deployed"] = "grenade.f1",
            ["explosive.satchel.deployed"] = "explosive.satchel",
            ["explosive.timed.deployed"] = "explosive.timed",
            ["rocket_basic"] = "rocket.launcher",
            ["rocket_admin"] = "1429",
            ["rocket_hv"] = "rocket.launcher",
            ["rocket_fire"] = "rocket.launcher",
            ["survey_charge.deployed"] = "surveycharge"
        };

        public static DeathMessages Instance;
        #endregion

        #region Configuration
        public PluginConfig Configuration;
        public class PluginConfig
        {
            public class ColorNickName
            {
                [JsonProperty("Цвет ника если игрока убили (hex)")]
                public string ColorDeath;
                [JsonProperty("Цвет ника если игрок убил (hex)")]
                public string ColorKill;
            }

            public class UISettings
            {
                [JsonProperty("Цвет задней панели убийства")]
                public string BackgroundColor;
                [JsonProperty("Цвет панели с дистанцией")]
                public string DistanceColor;
                [JsonProperty("Отступ сверху")]
                public int OffsetY;
            }

            [JsonProperty("Максимальное количество уведомлений")]
            public int MaxKillsForBar;
            [JsonProperty("Показывать моды оружия")]
            public bool ShowWeaponMods;
            [JsonProperty("Показывать смерть животных")]
            public bool ShowAnimalsDeath;
            [JsonProperty("Показывать хедшоты")]
            public bool ShowHeadShots;
            [JsonProperty("Цвет ника по привилегиям (По стандарту белый)")]
            public Dictionary<string, ColorNickName> ColorsNamePlayer = new Dictionary<string, ColorNickName>();
            [JsonProperty("Настройка интерфейса")]
            public UISettings UI = new UISettings();
        }

        public PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                ShowWeaponMods = true,
                ShowHeadShots = true,
                ShowAnimalsDeath = true,
                MaxKillsForBar = 3,
                ColorsNamePlayer = new Dictionary<string, PluginConfig.ColorNickName>
                {
                    { "deathmessages.premium", new PluginConfig.ColorNickName { ColorDeath = "#55ff8a", ColorKill = "#55ff8a" } },
                    { "deathmessages.vip", new PluginConfig.ColorNickName { ColorDeath = "#f9ff55", ColorKill = "#f9ff55" } },
                    { "deathmessages.deluxe", new PluginConfig.ColorNickName { ColorDeath = "#7303c0", ColorKill = "#7303c0" } },
                    { "deathmessages.godlike", new PluginConfig.ColorNickName { ColorDeath = "#ff0000", ColorKill = "#ff0000" } }
                },
                UI = new PluginConfig.UISettings
                {
                    BackgroundColor = "#00000080",
                    DistanceColor = "#FFFFFF00",
                    OffsetY = 0
                }
            };
        }

        private void Init()
        {
            Configuration = Config.ReadObject<PluginConfig>();
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(GetDefaultConfig(), true);
        }

        #endregion

        #region U'mod Hook's
        private void OnServerInitialized()
        {
            Instance = this;

            foreach (var itemDef in ItemManager.GetItemDefinitions())
            {
                Item newItem = ItemManager.CreateByName(itemDef.shortname, 1, 0);

                BaseEntity heldEntity = newItem.GetHeldEntity();
                if (heldEntity != null)
                {
                    prefabID2Item[heldEntity.prefabID] = itemDef.shortname;
                }

                var deployablePrefab = itemDef.GetComponent<ItemModDeployable>()?.entityPrefab?.resourcePath;
                if (string.IsNullOrEmpty(deployablePrefab))
                {
                    continue;
                }

                var shortPrefabName = GameManager.server.FindPrefab(deployablePrefab)?.GetComponent<BaseEntity>()?.ShortPrefabName;
                if (!string.IsNullOrEmpty(shortPrefabName) && !prefabName2Item.ContainsKey(shortPrefabName))
                {
                    prefabName2Item.Add(shortPrefabName, itemDef.shortname);
                }
            }

            foreach (var item in ItemManager.itemDictionary)
            {
                AddImage($"http://api.skyplugins.ru/api/getimage/{item.Value.shortname}/16", item.Value.shortname, 666);
            }

            AddImage($"https://i.imgur.com/aK1fE31.png", "headshot", 1336);

            foreach (var perm in Configuration.ColorsNamePlayer)
                permission.RegisterPermission(perm.Key, this);


            if (Configuration.ShowAnimalsDeath == false)
            {
                Unsubscribe("OnEntityDeath");
            }
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["npcplayer"] = "НПЦ",
                ["guntrap.deployed"] = "Гантрап",
                ["landmine"] = "Мина",
                ["beartrap"] = "Капкан",
                ["flameturret.deployed"] = "Огненная турель",
                ["flameturret_fireball"] = "Огненная турель",
                ["autoturret_deployed"] = "Турель",
                ["sentry.scientist.static"] = "Турель NPC",
                ["sentry.bandit.static"] = "Турель NPC",
                ["spikes.floor"] = "Шипы",
                ["spikes_static"] = "Шипы",
                ["teslacoil.deployed"] = "Тесла",
                ["barricade.wood"] = "Баррикада",
                ["barricade.woodwire"] = "Баррикада",
                ["barricade.metal"] = "Баррикада",
                ["bradleyapc"] = "Танк",
                ["gates.external.high.wood"] = "Ворота",
                ["gates.external.high.stone"] = "Ворота",
                ["icewall"] = "Ледяная стена",
                ["wall.external.high.ice"] = "Ледяная стена",
                ["wall.external.high.stone"] = "Стена",
                ["wall.external.high.wood"] = "Стена",
                ["campfire"] = "Костер",
                ["skull_fire_pit"] = "Костер",
                ["lock.code"] = "Замок",
                ["boar"] = "Кабан",
                ["bear"] = "Медведь",
                ["wolf"] = "Волк",
                ["stag"] = "Олень",
                ["chicken"] = "Курица",
                ["horse"] = "Конь",
                ["minicopter.entity"] = "Миникоптер",
                ["scraptransporthelicopter"] = "Транспортный вертолет",
                ["patrolhelicopter"] = "Патрульный вертолет",
                ["napalm"] = "Напалм",
                ["fireball_small"] = "Огонь",
                ["fireball_small_shotgun"] = "Огонь",
                ["fireball_small_arrow"] = "Огонь",
                ["sam_site_turret_deployed"] = "ПВО",
                ["cactus-1"] = "Кактус",
                ["cactus-2"] = "Кактус",
                ["cactus-3"] = "Кактус",
                ["cactus-4"] = "Кактус",
                ["cactus-5"] = "Кактус",
                ["cactus-6"] = "Кактус",
                ["cactus-7"] = "Кактус",
                ["hotairballoon"] = "Воздушный шар",
                ["cave_lift_trigger"] = "Лифт"
            }, this, "ru");
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["npcplayer"] = "NPC",
                ["guntrap.deployed"] = "Guntrap",
                ["landmine"] = "Landmine",
                ["beartrap"] = "Bear trap",
                ["flameturret.deployed"] = "Flame turret",
                ["flameturret_fireball"] = "Flame turret",
                ["autoturret_deployed"] = "Turret",
                ["sentry.scientist.static"] = "Turret NPC",
                ["sentry.bandit.static"] = "Turret NPC",
                ["spikes.floor"] = "Spikes",
                ["spikes_static"] = "Spikes",
                ["teslacoil.deployed"] = "Tesla",
                ["barricade.wood"] = "Barricade",
                ["barricade.woodwire"] = "Barricade",
                ["barricade.metal"] = "Barricade",
                ["bradleyapc"] = "BradleyAPC",
                ["gates.external.high.wood"] = "Gates",
                ["gates.external.high.stone"] = "Gates",
                ["icewall"] = "Ice wall",
                ["wall.external.high.ice"] = "Ice wall",
                ["wall.external.high.stone"] = "Wall",
                ["wall.external.high.wood"] = "Wall",
                ["campfire"] = "Campfire",
                ["skull_fire_pit"] = "Campfire",
                ["lock.code"] = "Codelock",
                ["boar"] = "Boar",
                ["bear"] = "Bear",
                ["wolf"] = "Wolf",
                ["stag"] = "Stag",
                ["chicken"] = "Chicken",
                ["horse"] = "Horse",
                ["minicopter.entity"] = "Minicopter",
                ["scraptransporthelicopter"] = "Transport helicopter",
                ["patrolhelicopter"] = "Patrol helicopter",
                ["napalm"] = "Napalm",
                ["fireball_small"] = "Fire",
                ["fireball_small_shotgun"] = "Fire",
                ["fireball_small_arrow"] = "Fire",
                ["sam_site_turret_deployed"] = "SAM",
                ["cactus-1"] = "Cactus",
                ["cactus-2"] = "Cactus",
                ["cactus-3"] = "Cactus",
                ["cactus-4"] = "Cactus",
                ["cactus-5"] = "Cactus",
                ["cactus-6"] = "Cactus",
                ["cactus-7"] = "Cactus",
                ["hotairballoon"] = "Hot air balloon",
                ["cave_lift_trigger"] = "Lift"
            }, this, "en");
        }

        private void Unload()
        {
            if (DeathNotesTimer != null)
                DeathNotesTimer.Destroy();

            DeathNote.DeathNotes = new List<DeathNote>();

            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "DeathMessages");
            }
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity != null && hitInfo != null)
            {
                if (entity is BasePlayer || entity is BaseAnimalNPC)
                {
                    lastHitInfo[entity] = hitInfo;
                }
            }

            return null;
        }

        private object OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity is BaseAnimalNPC)
            {
                if (hitInfo == null)
                {
                    if (lastHitInfo.TryGetValue(entity, out hitInfo) == false)
                    {
                        return null;
                    }
                }

                if (hitInfo.InitiatorPlayer != null)
                {
                    OnDeath(entity, hitInfo.InitiatorPlayer, hitInfo);
                }
            }
            return null;
        }

        private object OnPlayerDeath(BasePlayer player, HitInfo hitInfo)
        {
            if (player != null)
            {
                if (hitInfo == null)
                {
                    if (lastHitInfo.TryGetValue(player, out hitInfo) == false)
                    {
                        return null;
                    }
                }

                if (hitInfo.InitiatorPlayer != null)
                {
                    OnDeath(player, hitInfo.InitiatorPlayer, hitInfo);
                }
                else
                {
                    if (hitInfo.Initiator != null)
                    {
                        OnDeath(player, hitInfo.Initiator, hitInfo);
                    }
                }
            }
            return null;
        }
        #endregion

        #region Death Logic

        public struct WeaponInfo
        {
            public string WeaponName;
            public string[] WeaponMods;
            public bool IsHeadShot;
        }

        private void OnDeath(BasePlayer victim, BasePlayer initiator, HitInfo hitInfo)
        {
            string victimDisplayName = string.Empty;
            string victimColorName = string.Empty;
            string initiatorDisplayName = string.Empty;
            string initiatorColorName = string.Empty;

            bool needRenameVictim = false;
            bool needRenameInitiator = false;

            WeaponInfo weaponInfo = GetWeaponInfo(hitInfo);

            float fDistance = Vector3.Distance(victim.transform.position, initiator.transform.position);
            float sDistance = fDistance > 300 ? 300 : fDistance;

            weaponInfo.IsHeadShot = hitInfo.HitBone == 698017942;

            if(victim == initiator)
            {
                return;
            }

            if (initiator.IsNpc == true || initiator.userID.IsSteamId() == false)
            {
                initiatorDisplayName = "npcplayer";
                needRenameInitiator = true;
                return;
            }
            else
            {
                /*string clanTag = string.Empty;
                if (Clans != null)
                {
                    clanTag = (string)Clans?.Call("GetClanOf", initiator);
                    if (!string.IsNullOrEmpty(clanTag))
                        clanTag = "[" + clanTag + "] ";
                }*/

                initiatorDisplayName = initiator.displayName;
            }

            if (victim.IsNpc == true || victim.userID.IsSteamId() == false)
            {
                victimDisplayName = "npcplayer";
                needRenameVictim = true;
                return;
            }
            else
            {
                /*string clanTag = string.Empty;
                if (Clans != null)
                {
                    clanTag = (string)Clans?.Call("GetClanOf", victim);
                    if(!string.IsNullOrEmpty(clanTag))
                        clanTag = "[" + clanTag + "] ";
                }*/

                victimDisplayName = victim.displayName;
            }

            foreach (var perm in Configuration.ColorsNamePlayer)
            {
                if (victim.IsNpc == false)
                {
                    if (permission.UserHasPermission(victim.UserIDString, perm.Key))
                    {
                        victimColorName = perm.Value.ColorDeath;
                    }
                }

                if (initiator.IsNpc == false)
                {
                    if (permission.UserHasPermission(initiator.UserIDString, perm.Key))
                    {
                        initiatorColorName = perm.Value.ColorKill;
                    }
                }
            }

            new DeathNote(victimDisplayName, initiatorDisplayName, weaponInfo, sDistance, victimColorName, initiatorColorName, needRenameVictim, needRenameInitiator);
        }

        private void OnDeath(BaseEntity victim, BasePlayer initiator, HitInfo hitInfo)
        {
            string victimDisplayName = string.Empty;
            string victimColorName = string.Empty;
            string initiatorDisplayName = string.Empty;
            string initiatorColorName = string.Empty;

            bool needRenameVictim = false;
            bool needRenameInitiator = false;

            WeaponInfo weaponInfo = GetWeaponInfo(hitInfo);

            float fDistance = Vector3.Distance(victim.transform.position, initiator.transform.position);
            float sDistance = fDistance > 300 ? 300 : fDistance;

            weaponInfo.IsHeadShot = hitInfo.HitBone == 698017942;

            if (victim == initiator)
            {
                // не отображать суицид
                return;
            }
            if (initiator.IsNpc == true || initiator.userID.IsSteamId() == false)
            {
               
                initiatorDisplayName = "npcplayer";
                needRenameInitiator = true;

                // не отображать нпц
                return;
            }
            else
            {
                /*string clanTag = string.Empty;
                if (Clans != null)
                {
                    clanTag = (string)Clans?.Call("GetClanOf", initiator);
                    if (!string.IsNullOrEmpty(clanTag))
                        clanTag = "[" + clanTag + "] ";
                }*/

                initiatorDisplayName = initiator.displayName;
            }

            victimDisplayName = victim.ShortPrefabName;
            needRenameVictim = true;

            foreach (var perm in Configuration.ColorsNamePlayer)
            {
                if (initiator.IsNpc == false)
                {
                    if (permission.UserHasPermission(initiator.UserIDString, perm.Key))
                    {
                        initiatorColorName = perm.Value.ColorKill;
                    }
                }
            }

            new DeathNote(victimDisplayName, initiatorDisplayName, weaponInfo, sDistance, victimColorName, initiatorColorName, needRenameVictim, needRenameInitiator);
        }

        private WeaponInfo GetWeaponInfo(HitInfo hitInfo)
        {
            WeaponInfo weaponInfo = new WeaponInfo()
            {
                WeaponName = string.Empty,
                WeaponMods = new string[] { },
                IsHeadShot = false
            };

            if (hitInfo.Weapon != null)
            {
                Item itemWeapon = hitInfo.Weapon.GetItem();
                if (itemWeapon != null)
                {
                    weaponInfo.WeaponName = itemWeapon.info.shortname;
                    if (itemWeapon.contents != null)
                    {
                        weaponInfo.WeaponMods = itemWeapon.contents.itemList.Count > 0 ? itemWeapon.contents.itemList.Select(x => x.info.shortname).ToArray() : new string[] { };
                    }
                }
            }

            if (string.IsNullOrEmpty(weaponInfo.WeaponName) == true && hitInfo.ProjectilePrefab != null)
            {
                if (hitInfo.ProjectilePrefab.sourceWeaponPrefab != null)
                {
                    Item itemWeapon = hitInfo.ProjectilePrefab.sourceWeaponPrefab.GetItem();
                    if (itemWeapon != null)
                    {
                        weaponInfo.WeaponName = itemWeapon.info.shortname;
                        if (itemWeapon.contents != null)
                        {
                            weaponInfo.WeaponMods = itemWeapon.contents.itemList.Count > 0 ? itemWeapon.contents.itemList.Select(x => x.info.shortname).ToArray() : new string[] { };
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(weaponInfo.WeaponName) == true && hitInfo.WeaponPrefab != null)
            {
                if (prefabID2Item.TryGetValue(hitInfo.WeaponPrefab.prefabID, out weaponInfo.WeaponName) == false)
                {
                    prefabName2Item.TryGetValue(hitInfo.WeaponPrefab.ShortPrefabName, out weaponInfo.WeaponName);
                }
            }

            if (string.IsNullOrEmpty(weaponInfo.WeaponName))
            {
                weaponInfo.WeaponName = hitInfo.damageTypes.GetMajorityDamageType().ToString();
            }

            return weaponInfo;
        }

        private void OnDeath(BasePlayer victim, BaseEntity initiator, HitInfo hitInfo)
        {
            string victimDisplayName = string.Empty;
            string victimColorName = string.Empty;
            string initiatorDisplayName = string.Empty;
            bool needRenameVictim = false;
            bool needRenameInitiator = false;

            WeaponInfo weaponInfo = GetWeaponInfoFromPrefab(initiator);

            float fDistance = Vector3.Distance(victim.transform.position, initiator.transform.position);
            float sDistance = fDistance > 300 ? 300 : fDistance;

            if (initiator != null)
            {
                initiatorDisplayName = initiator.ShortPrefabName;

                if (lang.GetMessage(initiatorDisplayName, this, null) == initiatorDisplayName)
                {
                    LogToFile("DeathMessages", "InitiatorDisplayName: " + initiator.ShortPrefabName, this, true);
                    return;
                }

                needRenameInitiator = true;
            }

            if(victim == initiator)
            {
                // суицид
                return;
            }

            if (victim.IsNpc == true || victim.userID.IsSteamId() == false)
            {
                // нпц
                victimDisplayName = "npcplayer";
                needRenameVictim = true;
                return;
            }

            if (victim.IsNpc == false)
            {
                victimDisplayName = victim.displayName;
            }

            foreach (var perm in Configuration.ColorsNamePlayer)
            {
                if (!victim.IsNpc)
                {
                    if (permission.UserHasPermission(victim.UserIDString, perm.Key))
                    {
                        victimColorName = perm.Value.ColorDeath;
                    }
                }
            }

            new DeathNote(victimDisplayName, initiatorDisplayName, weaponInfo, sDistance, victimColorName, "#FFFFF", needRenameVictim, needRenameInitiator);
        }

        private WeaponInfo GetWeaponInfoFromPrefab(BaseEntity initiator)
        {
            WeaponInfo weaponInfo = new WeaponInfo()
            {
                WeaponName = string.Empty,
                WeaponMods = new string[0],
                IsHeadShot = false
            };

            if (initiator is AutoTurret)
            {
                AutoTurret autoTurret = initiator as AutoTurret;
                Item itemWeapon = autoTurret.inventory.itemList.Where(x => x.info.category == ItemCategory.Weapon).FirstOrDefault();
                if (itemWeapon != null)
                {
                    if (itemWeapon != null)
                    {
                        weaponInfo.WeaponName = itemWeapon.info.shortname;
                        if (itemWeapon.contents != null)
                        {
                            weaponInfo.WeaponMods = itemWeapon.contents.itemList.Count > 0 ? itemWeapon.contents.itemList.Select(x => x.info.shortname).ToArray() : new string[] { };
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(weaponInfo.WeaponName) == true)
            {
                if (prefabID2Item.TryGetValue(initiator.prefabID, out weaponInfo.WeaponName) == false)
                {
                    prefabName2Item.TryGetValue(initiator.ShortPrefabName, out weaponInfo.WeaponName);
                }
            }

            if (string.IsNullOrEmpty(weaponInfo.WeaponName) == true)
            {
                weaponInfo.WeaponName = initiator.ShortPrefabName;
            }

            return weaponInfo;
        }

        public static double GetStringWidth(string message, string font, int fontSize)
        {
            if (message.Contains("</color>"))
            {
                message = message.Substring(16);
                message = message.Replace("</color>", "");
            }

            System.Drawing.Font stringFont = new System.Drawing.Font(font, fontSize, System.Drawing.FontStyle.Bold);
            using (System.Drawing.Bitmap tempImage = new System.Drawing.Bitmap(200, 200))
            {
                System.Drawing.SizeF stringSize = System.Drawing.Graphics.FromImage(tempImage).MeasureString(message, stringFont);
                return stringSize.Width;
            }
        }

        public static int GetStringLength(string message)
        {
            if (message.Contains("</color>"))
            {
                message = message.Substring(16);
                message = message.Replace("</color>", "");
            }

            return message.Length;
        }
        #endregion

        #region KillFeed Controler
        private static Timer DeathNotesTimer;

        public class DeathNote
        {
            public string VictimName { get; set; }
            public string InitiatorName { get; set; }

            public WeaponInfo WeaponInfo { get; set; }

            public bool RenameVictim { get; set; }
            public bool RenameInitiator { get; set; }

            public Dictionary<string, string> UIStrings = new Dictionary<string, string>();

            public string Distance { get; set; }

            public static List<DeathNote> DeathNotes = new List<DeathNote>();

            public DeathNote(string victimName, string initiatorName, WeaponInfo weaponInfo, float distance, string colorVictim = "", string colorInitiator = "", bool needRenameVictim = false, bool needRenameInitiator = false)
            {
                if (needRenameVictim)
                    this.VictimName = $"{victimName}";
                else
                    this.VictimName = $"<color={(string.IsNullOrEmpty(colorVictim) ? "#ffffff" : colorVictim)}>{victimName}</color>";

                if (needRenameInitiator)
                    this.InitiatorName = $"{initiatorName}";
                else
                    this.InitiatorName = $"<color={(string.IsNullOrEmpty(colorInitiator) ? "#ffffff" : colorInitiator)}>{initiatorName}</color>";

                this.WeaponInfo = weaponInfo;
                this.Distance = distance.ToString("0.0");
                this.RenameVictim = needRenameVictim;
                this.RenameInitiator = needRenameInitiator;

                if (DeathNotes.Count > Instance.Configuration.MaxKillsForBar - 1)
                    DeathNotes.Remove(DeathNotes.LastOrDefault());

                if (DeathNotes.Count == 0)
                    DeathNotes.Add(this);
                else
                    DeathNotes.Insert(0, this);

                UpdateUI();
                UpdateTimer();
            }
        }

        public static void UpdateUI()
        {
            string deathContainer = Instance.GetUIContainerString();
            Dictionary<string, string[]> deathNotes = new Dictionary<string, string[]>();
            for (int j = 0; j < Instance.lang.GetLanguages(Instance).Length; j++)
            {
                string lang = Instance.lang.GetLanguages(Instance).ElementAt(j);
                deathNotes[lang] = new string[Instance.Configuration.MaxKillsForBar];
                for (int i = 0; i < DeathNote.DeathNotes.Count; i++)
                {
                    string initiatorName = DeathNote.DeathNotes[i].InitiatorName;
                    string victimName = DeathNote.DeathNotes[i].VictimName;

                    if (DeathNote.DeathNotes[i].RenameInitiator)
                    {
                        if (Instance.lang.GetMessages(lang, Instance).ContainsKey(initiatorName))
                            initiatorName = Instance.lang.GetMessages(lang, Instance)[initiatorName];
                    }

                    if (DeathNote.DeathNotes[i].RenameVictim)
                    {
                        if (Instance.lang.GetMessages(lang, Instance).ContainsKey(victimName))
                            victimName = Instance.lang.GetMessages(lang, Instance)[victimName];
                    }

                    double initiatorWidth = GetStringWidth(initiatorName, "Roboto condensed", 12);
                    double victimWidth = GetStringWidth(victimName, "Roboto condensed", 12);

                    int initiatorLength = GetStringLength(initiatorName);
                    if (initiatorLength <= 2)
                    {
                        initiatorWidth *= 2;
                    }
                    else if (initiatorLength <= 5)
                    {
                        initiatorWidth *= 1;
                    }
                    else if (initiatorLength <= 10)
                    {
                        initiatorWidth *= 0.8;
                    }
                    else
                    {
                        initiatorWidth *= 0.7;
                    }

                    int victimLength = GetStringLength(victimName);
                    if (victimLength <= 2)
                    {
                        victimWidth *= 2;
                    }
                    else if (victimLength <= 5)
                    {
                        victimWidth *= 1;
                    }
                    else if (victimLength <= 10)
                    {
                        victimWidth *= 0.8;
                    }
                    else
                    {
                        victimWidth *= 0.7;
                    }

                    deathNotes[lang][i] = Instance.GetReplacedString(i,
                    victimName,
                    initiatorName,
                    DeathNote.DeathNotes[i].WeaponInfo.WeaponName,
                    DeathNote.DeathNotes[i].WeaponInfo.WeaponMods,
                    DeathNote.DeathNotes[i].WeaponInfo.IsHeadShot,
                    DeathNote.DeathNotes[i].Distance,
                    victimWidth,
                    initiatorWidth);
                }
            }

            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "DeathMessages");
                CuiHelper.AddUi(player, deathContainer);

                string playerLang = Instance.lang.GetLanguage(player.UserIDString);

                if (deathNotes.ContainsKey(playerLang) == false)
                    playerLang = "en";

                for (int i = 0; i < deathNotes[playerLang].Length; i++)
                {
                    CuiHelper.AddUi(player, deathNotes[playerLang][i]);
                }
            }
        }

        public static void UpdateTimer()
        {
            if (DeathNotesTimer != null)
                DeathNotesTimer.Destroy();

            DeathNotesTimer = Instance.timer.In(5f, () =>
            {
                if (DeathNote.DeathNotes.Count > 0)
                {
                    DeathNote.DeathNotes.Remove(DeathNote.DeathNotes.LastOrDefault());

                    UpdateUI();
                    UpdateTimer();
                }
                else
                {
                    DeathNotesTimer.Destroy();
                }
            });
        }
        #endregion

        #region UI
        private string defaultString = string.Empty;

        private string defaultUIContainerString = string.Empty;

        private string defaultUIHeadShotString = string.Empty;

        private string defaultUIModString = string.Empty;

        private string GetReplacedString(int stringPosition, string victimName, string initiatorName, string weaponName, string[] weaponMods, bool isHeadShot, string distance, double victimWidth, double initiatorWidth)
        {
            string killString = GetUIString();
            double iconsOffset = 0.0;

            if (Configuration.ShowHeadShots && isHeadShot)
            {
                string headShotString = GetUIHeadShotString();
                iconsOffset += 21.0;
                headShotString = headShotString.Replace($"[{{i}}]", stringPosition.ToString());
                headShotString = headShotString.Replace($"{{WeaponImage}}", GetImage("headshot", 1336));
                headShotString = headShotString.Replace($"{{WeaponOffsetMinX}}", $"{5 + initiatorWidth + iconsOffset}");
                headShotString = headShotString.Replace($"{{WeaponOffsetMaxX}}", $"{5 + initiatorWidth + iconsOffset + 18}");
                headShotString = headShotString.Substring(1, headShotString.Length - 2);
                headShotString = "," + headShotString;
                killString = killString.Insert(killString.Length - 1, headShotString);
            }

            if (Configuration.ShowWeaponMods && weaponMods.Length > 0)
            {
                foreach (var weaponMod in weaponMods)
                {
                    string weaponModString = GetUIModString();
                    iconsOffset += 18.0;
                    weaponModString = weaponModString.Replace($"[{{i}}]", stringPosition.ToString());
                    weaponModString = weaponModString.Replace($"{{WeaponImage}}", GetImage(weaponMod, 666));
                    weaponModString = weaponModString.Replace($"{{WeaponOffsetMinX}}", $"{5 + initiatorWidth + iconsOffset + 6}");
                    weaponModString = weaponModString.Replace($"{{WeaponOffsetMaxX}}", $"{5 + initiatorWidth + iconsOffset + 18}");
                    weaponModString = weaponModString.Substring(1, weaponModString.Length - 2);
                    weaponModString = "," + weaponModString;
                    killString = killString.Insert(killString.Length - 1, weaponModString);
                }
            }

            ItemDefinition item;
            if (ItemManager.itemDictionaryByName.TryGetValue(weaponName, out item) == false)
            {
                weaponName = "skull.human";
            }

            killString = killString.Replace($"[{{i}}]", stringPosition.ToString());

            killString = killString.Replace($"{{MainOffsetMinX}}", $"-{victimWidth + initiatorWidth + iconsOffset + 18 + 15 + 6 + 50}");
            killString = killString.Replace($"{{MainOffsetMinY}}", $"{-66 + (Configuration.UI.OffsetY) - stringPosition * 25}");
            killString = killString.Replace($"{{MainOffsetMaxY}}", $"{-46 + (Configuration.UI.OffsetY) - stringPosition * 25}");
            killString = killString.Replace($"{{InitiatorName}}", initiatorName);
            killString = killString.Replace($"{{InitiatorOffsetMaxX}}", initiatorWidth.ToString());
            killString = killString.Replace($"{{VictimName}}", victimName);
            killString = killString.Replace($"{{VictimOffsetMinX}}", $"{5 + initiatorWidth + iconsOffset + 18 + 5}");
            killString = killString.Replace($"{{VictimOffsetMaxX}}", $"{5 + initiatorWidth + iconsOffset + 18 + 5 + victimWidth}");
            killString = killString.Replace($"{{DistanceOffsetMinX}}", $"{5 + initiatorWidth + iconsOffset + 18 + 5 + victimWidth}");
            killString = killString.Replace($"{{DistanceOffsetMaxX}}", $"{5 + initiatorWidth + iconsOffset + 18 + 5 + victimWidth + 60}");
            killString = killString.Replace($"{{DistanceM}}", distance + "m");
            killString = killString.Replace($"{{DistanceMOffsetMinX}}", $"{5 + initiatorWidth + iconsOffset + 18 + 5 + victimWidth}");
            killString = killString.Replace($"{{DistanceMOffsetMaxX}}", $"{5 + initiatorWidth + iconsOffset + 18 + 5 + victimWidth + 60}");
            killString = killString.Replace($"{{WeaponImage}}", GetImage(weaponName, 666));
            killString = killString.Replace($"{{WeaponOffsetMinX}}", $"{5 + initiatorWidth}");
            killString = killString.Replace($"{{WeaponOffsetMaxX}}", $"{5 + initiatorWidth + 18}");


            return killString;
        }

        private string GetUIContainerString()
        {
            if (string.IsNullOrEmpty(defaultUIContainerString))
            {
                defaultUIContainerString = CreateUIContainerString();
            }

            return defaultUIContainerString;
        }

        private string GetUIString()
        {
            if (string.IsNullOrEmpty(defaultString))
            {
                defaultString = CreateUIString();
            }

            return defaultString;
        }

        private string GetUIHeadShotString()
        {
            if (string.IsNullOrEmpty(defaultUIHeadShotString))
            {
                defaultUIHeadShotString = CreateUIHeadShotString();
            }

            return defaultUIHeadShotString;
        }

        private string GetUIModString()
        {
            if (string.IsNullOrEmpty(defaultUIModString))
            {
                defaultUIModString = CreateUIModString();
            }

            return defaultUIModString;
        }

        private string CreateUIContainerString()
        {
            CuiElementContainer Container = new CuiElementContainer();
            Container.Add(new CuiElement
            {
                Name = "DeathMessages",
                Parent = "Hud",
                Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0 0 0 0",
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "1 1",
                            AnchorMax = "1 1",
                        }
                    }
            });

            return Container.ToJson();
        }

        private string CreateUIString()
        {
            CuiElementContainer Container = new CuiElementContainer();
            Container.Add(new CuiElement
            {
                Name = $"DeathMessages.[{{i}}]",
                Parent = "DeathMessages",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat(Configuration.UI.BackgroundColor),
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "1 1",
                        AnchorMax = "1 1",
                        OffsetMin = $"{{MainOffsetMinX}} {{MainOffsetMinY}}",
                        OffsetMax = $"-6 {{MainOffsetMaxY}}",
                    }
                }
            });
            Container.Add(new CuiElement
            {
                Name = "DeathMessages.Initiator",
                Parent = $"DeathMessages.[{{i}}]",
                Components =
                        {
                            new CuiTextComponent
                            {
                                Text = $"<color=#FFFFFF>{{InitiatorName}}</color>",
                                Align = TextAnchor.MiddleCenter,
                                Font = "robotocondensed-bold.ttf",
                                FontSize = 12
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0 0.5",
                                AnchorMax = $"0 0.5",
                                OffsetMin = $"5 -10",
                                OffsetMax = $"{{InitiatorOffsetMaxX}} 10",
                            },
                            new CuiOutlineComponent
                            {
                                Color = "0 0 0 1",
                                Distance = "-0.5 0.5"
                            }
                        }
            });
            Container.Add(new CuiElement
            {
                Name = "DeathMessages.Victim",
                Parent = $"DeathMessages.[{{i}}]",
                Components =
                        {
                            new CuiTextComponent
                            {
                                Text = $"<color=#FFFFFF>{{VictimName}}</color>",
                                Align = TextAnchor.MiddleCenter,
                                Font = "robotocondensed-bold.ttf",
                                FontSize = 12
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0 0.5",
                                AnchorMax = $"0 0.5",
                                OffsetMin = $"{{VictimOffsetMinX}} -10",
                                OffsetMax = $"{{VictimOffsetMaxX}} 10",
                            },
                            new CuiOutlineComponent
                            {
                                Color = "0 0 0 1",
                                Distance = "-0.5 0.5"
                            }
                        }
            });
            Container.Add(new CuiElement
            {
                Name = $"DeathMessages.Distance",
                Parent = $"DeathMessages.[{{i}}]",
                Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Color = HexToRustFormat(Configuration.UI.DistanceColor),
                                    Material = "assets/icons/iconmaterial.mat",
                                    Sprite = $"assets/icons/subtract.png",
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"0 0.5",
                                    AnchorMax = $"0 0.5",
                                    OffsetMin = $"{{DistanceOffsetMinX}} -30",
                                    OffsetMax = $"{{DistanceOffsetMaxX}} 30",
                                }
                            }
            });
            Container.Add(new CuiElement
            {
                Name = "DeathMessages.DistanceM",
                Parent = $"DeathMessages.[{{i}}]",
                Components =
                        {
                            new CuiTextComponent
                            {
                                Text = $"<color=#404040>{{DistanceM}}</color>",
                                Align = TextAnchor.MiddleCenter,
                                Font = "robotocondensed-bold.ttf",
                                FontSize = 12
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0 0.5",
                                AnchorMax = $"0 0.5",
                                OffsetMin = $"{{DistanceMOffsetMinX}} -10",
                                OffsetMax = $"{{DistanceMOffsetMaxX}} 10",
                            },
                            new CuiOutlineComponent
                            {
                                Color = "1 1 1 1",
                                Distance = "-0.5 0.5"
                            }
                        }
            });
            Container.Add(new CuiElement
            {
                Name = $"DeathMessages.WeaponImage",
                Parent = $"DeathMessages.[{{i}}]",
                Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = $"{{WeaponImage}}",
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"0 0.5",
                                    AnchorMax = $"0 0.5",
                                    OffsetMin = $"{{WeaponOffsetMinX}} -9",
                                    OffsetMax = $"{{WeaponOffsetMaxX}} 9",
                                },
                                new CuiOutlineComponent
                                {
                                    Color = "0 0 0 1",
                                    Distance = "-0.5 0.5"
                                }
                            }
            });

            return Container.ToJson();
        }

        private string CreateUIModString()
        {
            CuiElementContainer Container = new CuiElementContainer();

            Container.Add(new CuiElement
            {
                Name = $"DeathMessages.WeaponMod",
                Parent = $"DeathMessages.[{{i}}]",
                Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = $"{{WeaponImage}}",
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"0 0.5",
                                    AnchorMax = $"0 0.5",
                                    OffsetMin = $"{{WeaponOffsetMinX}} -6",
                                    OffsetMax = $"{{WeaponOffsetMaxX}} 6",
                                },
                                new CuiOutlineComponent
                                {
                                    Color = "0 0 0 1",
                                    Distance = "-0.5 0.5"
                                }
                            }
            });

            return Container.ToJson();
        }

        private string CreateUIHeadShotString()
        {
            CuiElementContainer Container = new CuiElementContainer();

            Container.Add(new CuiElement
            {
                Name = $"DeathMessages.WeaponHeadshot",
                Parent = $"DeathMessages.[{{i}}]",
                Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = $"{{WeaponImage}}",
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"0 0.5",
                                    AnchorMax = $"0 0.5",
                                    OffsetMin = $"{{WeaponOffsetMinX}} -9",
                                    OffsetMax = $"{{WeaponOffsetMaxX}} 9",
                                },
                                new CuiOutlineComponent
                                {
                                    Color = "0 0 0 1",
                                    Distance = "-0.5 0.5"
                                }
                            }
            });

            return Container.ToJson();
        }
        #endregion

        #region Help
        public static StringBuilder sb = new StringBuilder();
        private static string HexToRustFormat(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            sb.Clear();
            return sb.AppendFormat("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a).ToString();
        }
        #endregion
    }
}
