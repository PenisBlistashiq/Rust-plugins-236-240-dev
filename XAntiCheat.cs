using System;
using System.Collections.Generic;
using Facepunch;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using System.Linq;
using UnityEngine;
using Oxide.Core;
using Random = UnityEngine.Random;
using Rust;
using ProtoBuf;
using Newtonsoft.Json.Linq;
using Oxide.Core.Libraries;

namespace Oxide.Plugins
{
    [Info("XAntiCheat", "fermens", "0.3.79")]
    public class XAntiCheat : RustPlugin
    {
        const string ipinnfourl = "https://ipinfo.io/{ip}/privacy?token={token}";
        const bool enablefull = false;
        const string ipinfosingup = "https://ipinfo.io/signup";

        #region CODE LOCK
        private static bool IsBanned(ulong userid)
        {
            return ServerUsers.Is(userid, ServerUsers.UserGroup.Banned);
        }

        private static bool IsImprisoned(ulong userid)
        {
            if (ins.PrisonBitch == null) return false;
            return ins.PrisonBitch.Call<bool>("ISIMPRISONED", userid);
        }
//Анти чит хуйня = ещё и продают Источник: https://info-hacks.ru/resources/xanticheat.45/
        private void OnCodeEntered(CodeLock codeLock, BasePlayer player, string code)
        {
            if (player == null) return;
            ulong owner = codeLock.OwnerID;
            if (owner == 0UL || code  != codeLock.code) return;
            bool bann = IsBanned(owner);
            bool unprisoned = IsImprisoned(owner);

            if(bann || unprisoned)
            {
                ADDLOG($"{player.displayName}({player.UserIDString}) ввёл пароль от кодового замка {(bann ? "забанненого" : "заключенного")} игрока({owner})!", 2);
                if (config.cODELOCK.enable)
                {
                    timer.Once(config.cODELOCK.seconds, () =>
                    {
                        BAN(player.UserIDString, config.cODELOCK.reason, config.cODELOCK.hours, player.displayName);
                    });
                }
            }

        }
        #endregion

        #region КОНФИГ
        private static PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        class SILENT
        {
            [JsonProperty("Автобан (не знаешь, не трогай!)")]
            public int xdetects;

            [JsonProperty("Причина бана")]
            public string reason;

            [JsonProperty("На сколько часов бан?")]
            public int hours;
        }

        class SPIDER
        {
            [JsonProperty("Причина бана")]
            public string reason;

            [JsonProperty("На сколько часов бан?")]
            public int hours;
        }

        class FLY
        {
            [JsonProperty("Причина бана")]
            public string reason;

            [JsonProperty("На сколько часов бан?")]
            public int hours;
        }

        class HITMOD
        {
            [JsonProperty("Банить?")]
            public bool enable;

            [JsonProperty("Причина бана")]
            public string reason;

            [JsonProperty("На сколько часов бан?")]
            public int hours;
        }

        class CODELOCK
        {
            [JsonProperty("Банить?")]
            public bool enable;

            [JsonProperty("Причина бана")]
            public string reason;

            [JsonProperty("Задержка в секундах перед баном, после детекта")]
            public float seconds;

            [JsonProperty("На сколько часов бан?")]
            public int hours;
        }

        class TEAMBAN
        {
            [JsonProperty("Банить?")]
            public bool enable;

            [JsonProperty("Причина бана")]
            public string reason;

            [JsonProperty("Банить, если в команде N забаненных")]
            public int num;

            [JsonProperty("Учитывать бан за макрос?")]
            public bool macros;

            [JsonProperty("На сколько часов бан?")]
            public int hours;
        }

        class SMARTBAN
        {
            [JsonProperty("Банить?")]
            public bool enable;

            [JsonProperty("Причина бана")]
            public string reason;

            [JsonProperty("Fly: очков")]
            public int fly;

            [JsonProperty("EspStash: очков")]
            public int espstash;

            [JsonProperty("Macro: очков")]
            public int macro;

            [JsonProperty("HitMod: очков")]
            public int hitmod;

            [JsonProperty("Spider: очков")]
            public int spider;

            [JsonProperty("Silent: очков")]
            public int silent;

            [JsonProperty("На сколько часов бан?")]
            public int hours;
        }

        class ESPSTASH
        {
            [JsonProperty("Количество")]
            public int amount;

            [JsonProperty("Возможный лут")]
            public Dictionary<string, int> loots;
        }

        class MACROS
        {
            [JsonProperty("Причина бана")]
            public string reason;

            [JsonProperty("Количество выстрелов")]
            public int shoots;

            [JsonProperty("Количество детектов")]
            public int amount;

            [JsonProperty("На сколько часов бан?")]
            public int hours;
        }

        private class PluginConfig
        {
            [JsonProperty("SteamAPI")]
            public string steampi;

            [JsonProperty("Silent Aim: настройка")]
            public SILENT sILENT;

            [JsonProperty("Spider: настройка")]
            public SPIDER sPIDER;

            [JsonProperty("ESP SmallStash: настройка")]
            public ESPSTASH ESPStash;

            [JsonProperty("FLY: настройка")]
            public FLY fLY;

            [JsonProperty("TEAMBAN: настройка")]
            public TEAMBAN tEAMBAN;

            [JsonProperty("CODELOCK: настройка")]
            public CODELOCK cODELOCK;

            [JsonProperty("HITMOD: настройка")]
            public HITMOD hITMOD;

            [JsonProperty("MACROS: настройка")]
            public MACROS mACROS;

            [JsonProperty("Debug camera: Банить?")]
            public bool debugcamera;

            [JsonProperty("Отображать данные при подключении игрока?")]
            public bool show;

            [JsonProperty("Не банить Steam игроков?")]
            public bool steamplayer;

            [JsonProperty("Отправлять в тюрьму, если есть плагин PrisonBitch")]
            public bool prison;

            [JsonProperty("Банить не настроеные аккаунты?")]
            public bool bannensatroyen;

            [JsonProperty("Банить аккаунты, которым меньше X дней")]
            public int banday;

            [JsonProperty("На сколько часов банить новые аккаунты")]
            public int bannewaccountday;

            [JsonProperty("Кикать не настроенные аккаунты")]
            public bool kicknenastoyen;

            [JsonProperty("Кикать приватные аккаунты")]
            public bool kickprivate;

            [JsonProperty("Кикать игроков использующих VPN")]
            public bool kickvpn;

            [JsonProperty("Не кикать лицухи?")]
            public bool steamkick;

            [JsonProperty("Не банить лицушников за новые аккаунты?")]
            public bool steam;

            [JsonProperty("Писать/сохранять логи [0 - нет | 1 - только баны | 2 - все]")]
            public int logspriority;

            [JsonProperty("Discord: ID канала")]
            public string discordid;

            [JsonProperty(" Логи попаданий с огнестрела в консоль")]
            public bool logs;

            [JsonProperty("IPINFO ТОКЕН")]
            public string ipinfotoken;

            [JsonProperty("tt")]
            public string tt;

            [JsonProperty("Сообщения")]
            public Dictionary<string, string> messages;

            [JsonProperty("Шаблоны банов")]
            public Dictionary<string, string> pattern;

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    ipinfotoken = ipinfosingup,
                    kickvpn = false,
                    tEAMBAN = new TEAMBAN
                    {
                        enable = true,
                        hours = 168,
                        reason = "\"bb with teammates\"",
                        num = 2
                    },
                    cODELOCK = new CODELOCK
                    {
                        enable = true,
                        hours = 336,
                        reason = "\"Ban Detected! (1)\"",
                        seconds = 75
                    },
                    sPIDER = new SPIDER
                    {
                        hours = 720,
                        reason = "\"Cheat Detected! (2)\""
                    },
                    sILENT = new SILENT
                    {
                        xdetects = 7,
                        hours = 1440,
                        reason = "\"Cheat Detected! (1)\""
                    },
                    fLY = new FLY
                    {
                        hours = 720,
                        reason = "\"Cheat Detected! (3)\""
                    },
                    hITMOD = new HITMOD
                    {
                        enable = true,
                        hours = 720,
                        reason = "\"Cheat Detected! (6)\""
                    },
                    mACROS = new MACROS
                    {
                        amount = 5,
                        hours = 168,
                        reason = "\"Macros Detected!\"",
                        shoots = 10
                    },
                    prison = true,
                    steamkick = true,
                    debugcamera = true,
                    logs = false,
                    steam = true,
                    steampi = defaultsteamapi,
                    banday = 5,
                    kicknenastoyen = true,
                    kickprivate = true,
                    tt = "nothing",
                    bannensatroyen = false,
                    show = true,
                    bannewaccountday = 120,
                    steamplayer = false,
                    messages = new Dictionary<string, string>
                    {
                        { "NEW.ACCOUNT", "\"Подозрительный аккаунт\"" },
                        { "KICK.PRIVATE", "\"Откройте профиль, что бы играть на этом сервере! (Make your Steam profile public to play on this server)\"" },
                        { "KICK.NENASTROYEN", "\"Настройте профиль, что бы играть на этом сервере! (Make your Steam profile public to play on this server)\""},
                        { "KICK.VPN", "\"На сервере запрещено играть с VPN! (VPN DETECTED)\""}
                    },
                    pattern = new Dictionary<string, string>
                    {
                        { "BAN.ACCOUNT", "ban {steamid} {reason} {time}" },
                        { "PRISON.ACCOUNT", "prison.add {steamid} {time} {reason}" },
                    },
                    logspriority = 2,
                    ESPStash = new ESPSTASH
                    {
                        amount = 100,
                        loots = new Dictionary<string, int>
                        {
                            { "rifle.ak", 1 },
                            { "rifle.bolt", 1 },
                            { "rifle.l96", 1 },
                            { "rifle.lr300", 1 },
                            { "rifle.semiauto", 1 },
                            { "wood", 10000 },
                            { "stones", 10000 },
                            { "metal.refined", 50 },
                            { "metal.fragments", 10000 },
                            { "metal.facemask", 1 },
                            { "scrap", 500 },
                        }
                    },
                    discordid = ""
                };
            }
        }
        #endregion

        int nsnext = 0;
        [ChatCommand("ns")]
        private void COMMANSTASH(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            player.Teleport(stashContainers[nsnext].transform.position + Vector3.up * 1.5f);
            nsnext++;
            if (stashContainers.Count >= nsnext) nsnext = 0;
        }

        Vector3 lastshash;
        [ChatCommand("ls")]
        private void COMMALS(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            if(lastshash == Vector3.zero)
            {
                player.ChatMessage("<color=yellow>Еще не раскопали ни одного стеша!</color>");
                return;
            }
            player.Teleport(lastshash + Vector3.up * 1.5f);
        }

        [ChatCommand("testaim")]
        private void COMMANDERAIM(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            TESTAIM(player);
        }

        [ConsoleCommand("test.aim")]
        private void cmdtest(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin || !arg.HasArgs()) return;
            ulong ID;
            if(!ulong.TryParse(arg.Args[0], out ID))
            {
                arg.ReplyWith("ЭТО НЕ СТИМ ИД!");
                return;
            }
            BasePlayer player = BasePlayer.FindByID(ID);
            if(player == null)
            {
                arg.ReplyWith("ИГРОК НЕ НАЙДЕН!");
                return;
            }
            TESTAIM(player);
        }

      /* Dictionary<BasePlayer, DateTime> lastbowtime = new Dictionary<BasePlayer, DateTime>();
        void OnWeaponFired(BaseProjectile projectile, BasePlayer player, ItemModProjectile mod, ProtoBuf.ProjectileShoot projectiles)
        {
            if (player == null || projectile == null || mod == null || projectiles == null)  return;
            Item item = player.GetActiveItem();
            if (item == null || item.info.itemid != 1443579727)  return;
            DateTime dateTime;
            if (!lastbowtime.TryGetValue(player, out dateTime))
            {
                lastbowtime[player] = DateTime.Now;
                return;
            }
            Debug.Log(player.displayName + " - " + (DateTime.Now- dateTime).TotalMilliseconds);
            lastbowtime[player] = DateTime.Now;
        }*/

        private void TESTAIM(BasePlayer player)
        {
            int i = 0;
            Vector3 head = player.eyes.MovementForward();
            Debug.Log($"{i}. {player.displayName} - {player.eyes.HeadForward()} - {player.eyes.MovementForward()}");
            Vector3 pos = new Vector3(player.transform.position.x + (10f * player.eyes.MovementForward().x), 500f, player.transform.position.z + (10f * head.z));
          //  Vector3 vector3 = player.eyes.
            BasePlayer npc = GameManager.server.CreateEntity("assets/prefabs/player/player.prefab", new Vector3(pos.x, TerrainMeta.HeightMap.GetHeight(pos) - 2f, pos.z), new Quaternion(), true) as BasePlayer;
            if (npc == null) return;
            npc.enableSaving = false;
            npc.Spawn();
            NextTick(() =>
            {
                PlayerInventory inv = npc.GetComponent<PlayerInventory>();
                inv.Strip();
            });

            timer.Repeat(0.1f, 20, () => {i++; Debug.Log($"{i}. {player.displayName} - {player.eyes.HeadForward()}"); });
            timer.Once(2.5f, () => { if (!npc.IsDead()) npc.Kill(); });
        }

        [ChatCommand("testfly")]
        private void COMMANDER(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin)
            {
                List<BaseEntity> list = Pool.GetList<BaseEntity>();
                Vis.Entities<BaseEntity>(player.transform.position, 2f, list);
                List<TreeEntity> list2 = Pool.GetList<TreeEntity>();
                Vis.Entities<TreeEntity>(player.transform.position, 8.5f, list2);
                string elements = "";
                bool pl = false;
                foreach (var z in list)
                {
                    if (z is BasePlayer && (z as BasePlayer) != player) pl = true;
                    elements += z.ShortPrefabName + (list.Count > 1 ? " | " : "");
                }
                RaycastHit hit;
                var raycast = Physics.Raycast(player.transform.position, Vector3.down, out hit, 500f);
                if (raycast)
                {
                    bool spider = false;
                    RaycastHit hit2;
                    var raycast2 = Physics.Raycast(player.transform.position, player.eyes.BodyForward(), out hit2, 1f);
                    if (raycast2)
                    {
                        spider = hit2.collider.name.Contains("wall");
                        Debug.Log(hit2.collider.name);
                    }
                    bool ins = hit.collider.name.Contains("assets/prefabs/building core");
                    float distance = player.Distance(hit.point);
                    Debug.Log($"-[TEST]- {player.displayName}({player.UserIDString}) - [{elements}] - высота: {distance.ToString("F1")} м. ({hit.collider.name}) | Дерево: {(list2.Count > 0 ? "Да" : "Нет")} | Игрок: {(pl ? "Да" : "Нет")} | В здании: {(ins ? "Да" : "Нет")} | Спайдер: {(spider ? "Да" : "Нет")}");
                }
            }
        }

        [PluginReference] private Plugin PrisonBitch;

        private static void BAN(string steamid, string reason, int time, string displayname, bool checkteam = true)
        {
            if (!enablefull) return;

            ulong usteam = Convert.ToUInt64(steamid);
            if (config.steamplayer)
            {
                BasePlayer pl = BasePlayer.FindByID(usteam);
                if(pl != null)
                {
                    if (ins.ISSTEAM(pl.Connection))
                    {
                        ADDLOG($"{pl.displayName}({pl.UserIDString}) отмазали от бана.", 2);
                        return;
                    }
                }
            }

            ulong usteamid = Convert.ToUInt64(steamid);

            if (checkteam && (reason != config.mACROS.reason || config.tEAMBAN.macros))
            {
                if (config.tEAMBAN.enable)
                {
                    BasePlayer basePlayer = BasePlayer.FindByID(usteam);
                    if (basePlayer != null && basePlayer.Team != null && basePlayer.Team.members.Count > 1)
                    {
                        int banned = basePlayer.Team.members.Count(x => IsBanned(x) || IsImprisoned(x));
                        if (banned >= config.tEAMBAN.num - 1)
                        {
                            foreach (var z in basePlayer.Team.members)
                            {
                                if (basePlayer.userID == z) continue;
                                string strid = z.ToString();
                                BasePlayer basePlayer2 = BasePlayer.FindByID(z);
                                string name = basePlayer2 != null ? basePlayer2.displayName : strid;
                                ins.timer.Once(1f, () => BAN(z.ToString(), config.tEAMBAN.reason, config.tEAMBAN.hours, name, false));
                            }
                        }
                    }
                }
            }

            if (config.prison && prison)
            {
                if (!IsImprisoned(usteamid))
                {
                    ADDLOG($"Отправили в тюрьму {displayname}({steamid}) на {(time * 1f / 24f).ToString("F1")} дней [{reason}]", 1);
                    time = (int)(time * 60f);
                    ins.Server.Command(config.pattern["PRISON.ACCOUNT"].Replace("{steamid}", steamid).Replace("{time}", time.ToString()).Replace("{reason}", reason));
                }
            }
            else
            {
                if (!IsBanned(usteamid))
                {
                    ins.Server.Command(config.pattern["BAN.ACCOUNT"].Replace("{steamid}", steamid).Replace("{time}", time.ToString()).Replace("{reason}", reason));
                    ADDLOG($"Забанили {displayname}({steamid}) на {(time * 1f / 24f).ToString("F1")} дней [{reason}]", 1);
                }
            }
        }

        const int flylimit = 2;
        const int spiderlimit = 2;

        private static Dictionary<ulong, int> macros = new Dictionary<ulong, int>();

        class ANTICHEAT : MonoBehaviour
        {
            private int stash;
            private int silent;
            private int spider;
            private int fly;
            BasePlayer player;
            private DateTime lastban;

            private DateTime firsthit;
            private DateTime hitmod;
            string lasthit;
            int hits;
            float distancehit;
            string weaponhit;

            public DateTime LastFires;
            public int fires;
            string weaponfire;
            float posfire;
            float posfirel;
            float posfirer;

            private int numdetecthit;

            bool macromove;
            Vector3 startshoots;

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                if (player == null || enablefull && (player.IsAdmin || ins.permission.UserHasPermission(player.UserIDString, "xanticheat.allow")))
                {
                    Destroy(this);
                    return;
                }
          //      if (!enablefull) Debug.Log(player.displayName + " добавили в античит.");
                lasthit = "";
                silent = 0;
                weaponhit = "";
                weaponfire = "";
                if (config.debugcamera) InvokeRepeating(nameof(TICK), 0f, 10f);
                InvokeRepeating(nameof(SILENTCLEAR), 120f, 120f);
               // InvokeRepeating(nameof(WH), 0f, 1f);
            }

            private void SILENTCLEAR()
            {
                spider = 0;
                silent = 0;
                fly = 0;
            }

            public void ADDFLY()
            {
                fly++;
                ADDLOG($"-[Fly]- {player.displayName}({player.UserIDString}) детектов {fly}/{flylimit}", 2);
                if (fly >= flylimit && lastban < DateTime.Now)
                {
                    BAN(player.UserIDString, config.fLY.reason, config.fLY.hours, player.displayName);
                    lastban = DateTime.Now.AddSeconds(10f);
                    fly = 0;
                    return;
                }
            }

            public void ADDFIRE(string weapon)
            {
                if (weapon == "ak47u.item")
                {
                    Vector3 vector3 = player.transform.position;
                    double sec = (DateTime.Now - LastFires).TotalSeconds;
                    Vector3 current = player.eyes.HeadForward();
                    if (fires == 0)
                    {
                        startshoots = vector3;
                        macromove = false;
                        sec = 0;
                        posfire = current.y;
                        posfirel = current.x;
                        posfirer = current.z;
                    }
                    else
                    {
                        if (!macromove && startshoots != vector3)
                        {
                            macromove = true;
                        }
                    }
                    float razn = Mathf.Abs(posfire - current.y);
                    float raznl = Mathf.Abs(posfirel - current.x);
                    float raznr = Mathf.Abs(posfirer - current.z);
                    
                    if (current.y < 0.9f && current.y > -0.9f && sec < 0.2f && razn <= 0.009f && raznl <= 0.009f && raznr <= 0.009f)
                    {
                        fires++;
                        LastFires = DateTime.Now;
                        weaponfire = weapon;
                        if (IsInvoking(nameof(FIREEND))) CancelInvoke(nameof(FIREEND));
                        Invoke(nameof(FIREEND), 0.21f);
                    }
                }
            }

            private void FIREEND()
            {
                if (fires >= config.mACROS.shoots)
                {
                    if (!macros.ContainsKey(player.userID)) macros.Add(player.userID, 1);
                    else macros[player.userID] += 1;

                    if(macros[player.userID] >= config.mACROS.amount)
                    {
                        BAN(player.UserIDString, config.mACROS.reason, config.mACROS.hours, player.displayName);
                        lastban = DateTime.Now.AddSeconds(10f);
                    }

                    ADDLOG($"-[Macro]- {player.displayName}({player.UserIDString}) | выстрелов {fires} | использовал {weaponfire} | двигался: {(macromove ? "да" : "нет")} | детект #{macros[player.userID]}", 2);
                }
                fires = 0;
            }

            public void ADDHIT(string hitbone, string weapon, float distance)
            {
                if (hitbone == "N/A" || distance < 30f) return;
                if(hitbone != lasthit && lasthit != "")
                {
                    CLEARHIT();
                    return;
                }
                if (hits == 0)
                {
                    hitmod = DateTime.Now;
                    firsthit = DateTime.Now;
                }
                 hits++;

                //  if (!enablefull) Debug.Log(player.displayName + " - " + hits);
                lasthit = hitbone;
                distancehit += distance;
                if(hits >= 2) distancehit /= 2;
                if(!weaponhit.Contains(weapon)) weaponhit += (hits >= 2 ? ", " : string.Empty) + weapon;
                if (hits >= 5)
                {
                    ADDLOG($"-[HitMod]- {player.displayName}({player.UserIDString}) | {hitbone} | средняя дистанция {distancehit.ToString("F1")} | использовал {weaponhit} | ({(DateTime.Now-firsthit).TotalMinutes.ToString("F1")})", 2);
                    if (distancehit > 100 && (weaponhit.Contains("bow_hunting.entity") || weaponhit.Contains("crossbow.entity") || weaponhit.Contains("bow.compound") || weaponhit.Contains("pistol_eoka.entity"))
                        || distancehit > 65 && (weaponhit == "bow_hunting.entity" || weaponhit == "crossbow.entity" || weaponhit == "bow.compound")
                        || distancehit > 40 && weaponhit == "pistol_eoka.entity")
                    {
                        BAN(player.UserIDString, config.hITMOD.reason, config.hITMOD.hours, player.displayName);
                        lastban = DateTime.Now.AddSeconds(10f);
                    }

                    if(!weapon.Contains("l96.entity"))
                    {
                        if ((DateTime.Now - hitmod).TotalMinutes < 10f)
                        {
                            numdetecthit++;
                            if (numdetecthit >= 3)
                            {
                                BAN(player.UserIDString, config.hITMOD.reason, config.hITMOD.hours, player.displayName);
                                lastban = DateTime.Now.AddSeconds(10f);
                            }
                        }
                        else
                        {
                            numdetecthit = 0;
                        }
                    }

                    hitmod = DateTime.Now;
                    CLEARHIT();
                }
            }
            /*
            private void WH()
            {
                RaycastHit hitInfo;
                if (GamePhysics.Trace(new Ray(player.eyes.position, player.eyes.HeadForward()), 0.0f, out hitInfo, 300f, 1219701521, QueryTriggerInteraction.UseGlobal))
                {
                    //targetPos = hitInfo.point;
                    if ((bool)((UnityEngine.Object)hitInfo.collider))
                    {
                        BaseEntity entity = hitInfo.GetEntity();
                        if ((bool)((UnityEngine.Object)entity))
                        {
                            BaseCombatEntity baseCombatEntity = entity as BaseCombatEntity;
                            if (baseCombatEntity is BasePlayer)
                            {
                                BasePlayer basePlayer = baseCombatEntity as BasePlayer;
                                RaycastHit hitInfo2;
                                if (GamePhysics.Trace(new Ray(player.eyes.position, player.eyes.HeadForward()), 0.0f, out hitInfo2, 300f, 1218652417, QueryTriggerInteraction.UseGlobal))
                                {
                                    BaseEntity entity2 = hitInfo2.GetEntity();
                                    if ((bool)((UnityEngine.Object)entity2))
                                    {
                                        BaseCombatEntity baseCombatEntity2 = entity as BaseCombatEntity;
                                        if (!(baseCombatEntity2 is BasePlayer))
                                        {
                                            ADDLOG($"[#] {player.displayName} -> {basePlayer.displayName} [{player.Distance(basePlayer)} м. | {(basePlayer.eyes.transform.position.y - hitInfo.point.y).ToString("F1")} м. | {baseCombatEntity2.ShortPrefabName}]", 2);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            */
            private void CLEARHIT()
            {
                hits = 0;
                lasthit = "";
                weaponhit = "";
                distancehit = 0;
            }

            public void ADDSTASH()
            {
                stash++;
                if (stash >= 2 && lastban < DateTime.Now)
                {
                    BAN(player.UserIDString, "\"CheatDetected (6)\"", config.fLY.hours, player.displayName);
                    lastban = DateTime.Now.AddSeconds(10f);
                    stash = 0;
                    return;
                }
            }

            public void ADDSPIDER()
            {
                spider++;
                ADDLOG($"-[Spider]- {player.displayName}({player.UserIDString}) детектов {spider}/{spiderlimit}", 2);
                if (spider >= spiderlimit && lastban < DateTime.Now)
                {
                    BAN(player.UserIDString, config.sPIDER.reason, config.sPIDER.hours, player.displayName);
                    lastban = DateTime.Now.AddSeconds(10f);
                    spider = 0;
                    return;
                }
            }

            public void ADDSILENT(int amount)
            {
                silent += amount;
                ADDLOG($"-[SAim]- {player.displayName}({player.UserIDString}) детектов {silent}/{config.sILENT.xdetects}", 2);
                if (silent >= config.sILENT.xdetects && lastban < DateTime.Now)
                {
                    BAN(player.UserIDString, config.sILENT.reason, config.sILENT.hours, player.displayName);
                    lastban = DateTime.Now.AddSeconds(10f);
                    silent = 0;
                    return;
                }
            }

            private void TICK()
            {
                /*    if (player.IsFlying)
                    {
                        if (!IsInvoking(nameof(FLYTICK)))
                        {
                            height = player.transform.position.y;
                            InvokeRepeating(nameof(FLYTICK), 1f, 1f);
                        }
                    }*/

                if (enablefull)
                {
                    player.SendConsoleCommand("noclip");
                    player.SendConsoleCommand("camspeed 0");
                }

               

                /* if (player.modelState.flying && !player.IsAdmin && !player.IsDeveloper)
                {
                    BAN(player.UserIDString, "\"Cheat Detected! (4)\"", 60, player.displayName);
                    lastban = DateTime.Now.AddSeconds(10f);
                }*/
            }

         /*   private float height;
            private void FLYTICK()
            {
                if (!player.IsFlying)
                {
                    fly = 0;
                    CancelInvoke(nameof(FLYTICK));
                }

                fly++;
                if (fly >= 5)
                {
                    ADDLOG($"[Fly] {player.displayName}({player.UserIDString}) возможно флай.", 2);
                    fly = 0;
                    CancelInvoke(nameof(FLYTICK));
                }
            }*/

            public void DoDestroy() => Destroy(this);

            private void OnDestroy()
            {
                if (IsInvoking(nameof(TICK))) CancelInvoke(nameof(TICK));
               // if (IsInvoking(nameof(WH))) CancelInvoke(nameof(WH));
                if (IsInvoking(nameof(FIREEND))) CancelInvoke(nameof(FIREEND));
                //  if (IsInvoking(nameof(FLYTICK))) CancelInvoke(nameof(FLYTICK));
            }
        }

      /*  private void OnWeaponFired(BaseProjectile projectile, BasePlayer player, ItemModProjectile mod, ProtoBuf.ProjectileShoot projectiles)
        {
            if (player == null || projectile == null || mod == null || projectiles == null) return;
            RaycastHit hitInfo;
            if (GamePhysics.Trace(new Ray(player.eyes.position, player.eyes.HeadForward()), 0.0f, out hitInfo, 300f, 1219701521, QueryTriggerInteraction.UseGlobal))
            {
                //targetPos = hitInfo.point;
                if ((bool)((UnityEngine.Object)hitInfo.collider))
                {
                    BaseEntity entity = hitInfo.GetEntity();
                    if ((bool)((UnityEngine.Object)entity))
                    {
                        BaseCombatEntity baseCombatEntity = entity as BaseCombatEntity;
                        if (baseCombatEntity is BasePlayer)
                        {
                            BasePlayer basePlayer = baseCombatEntity as BasePlayer;
                          //  if(basePlayer)
                            Debug.Log($"{player.displayName} -> {basePlayer.displayName} [{player.Distance(basePlayer)} м. | {(basePlayer.eyes.transform.position.y - hitInfo.point.y).ToString("F1")} м.]");
                        }
                    }
                }
            }
        }*/

        private void OnPlayerBanned(string name, ulong id, string address, string reason)
        {
            if (reason == "Cheat Detected!") ADDLOG($"Забанили {name}({id}) [FakeAdmin/DebugCamera]", 1);
        }

        const string defaultsteamapi = "https://steamcommunity.com/dev/apikey";
        class resp
        {
            public avatar response;
        }

        class avatar
        {
            public List<Players> players;
        }

        class Players
        {
            public int? profilestate;
            public int? timecreated;
        }

        class INFO
        {
            public DateTime dateTime;
            public bool profilestate;
            public bool steam;
            public Dictionary<string, Dictionary<string, int>> hitinfo;
        }

        Dictionary<ulong, INFO> PLAYERINFO = new Dictionary<ulong, INFO>();

        static XAntiCheat ins;
        private void Init()
        {
            ins = this;
            //   Unsubscribe(nameof(OnPlayerConnected));
        }

        #region Grid
        Dictionary<string, Vector3> Grids = new Dictionary<string, Vector3>();
        const float calgon = 0.0066666666666667f;
        void CreateSpawnGrid()
        {
            var worldSize = (ConVar.Server.worldsize);
            float offset = worldSize / 2;
            var gridWidth = (calgon * worldSize);
            float step = worldSize / gridWidth;

            string start = "";

            char letter = 'A';
            int number = 0;

            for (float zz = offset; zz > -offset; zz -= step)
            {
                for (float xx = -offset; xx < offset; xx += step)
                {
                    Grids.Add($"{start}{letter}{number}", new Vector3(xx - 55f, 0, zz));
                    if (letter.ToString().ToUpper() == "Z")
                    {
                        start = "A";
                        letter = 'A';
                    }
                    else
                    {
                        letter = (char)(((int)letter) + 1);
                    }


                }
                number++;
                start = "";
                letter = 'A';
            }
        }

        private string GetNameGrid(Vector3 pos)
        {
            return Grids.Where(x => x.Value.x < pos.x && x.Value.x + 150f > pos.x && x.Value.z > pos.z && x.Value.z - 150f < pos.z).FirstOrDefault().Key;
        }
        #endregion

        private static bool prison = false;
        private List<StashContainer> stashContainers = new List<StashContainer>();
        private float sizeworldx;
        private float sizeworldz;

        List<Vector3> OntheMap = new List<Vector3>();
        void foundmonuments()
        {
            OntheMap.Clear();
            foreach (var z in UnityEngine.Object.FindObjectsOfType<MonumentInfo>())
            {
                if (z.name.Contains("/cave") || z.name.Contains("/tiny") || z.name.Contains("/power substations") || z.name.Contains("OilrigAI")) continue;
                Vector3 pos = z.transform.position;
                if (!OntheMap.Contains(pos)) OntheMap.Add(pos);
            }
        }

        private Vector3 RANDOMPOS() => new Vector3(Random.Range(-sizeworldx, sizeworldx), 400f, Random.Range(-sizeworldz, sizeworldz));

        List<string> names = new List<string>();

        private Vector3 FINDSPAWNPOINT(int num = 1)
        {
            if (num >= 300) return Vector3.zero;
            Vector3 pos = RANDOMPOS();

            RaycastHit hitInfo;
            if (!Physics.Raycast(pos, Vector3.down, out hitInfo, 450f, Layers.Solid)) return FINDSPAWNPOINT(num++);
            if (hitInfo.collider == null || hitInfo.collider.name != "Terrain" && hitInfo.collider.name != "Road Mesh") return FINDSPAWNPOINT(num++);
            if (hitInfo.point.y - TerrainMeta.WaterMap.GetHeight(hitInfo.point) < 0) return FINDSPAWNPOINT(num++);
            if (WaterLevel.Test(hitInfo.point)) return FINDSPAWNPOINT(num++);
            if (OntheMap.Any(x => Vector3.Distance(x, hitInfo.point) < 170f)) return FINDSPAWNPOINT(num++);
            if (stashContainers.Any(x => Vector3.Distance(x.transform.position, hitInfo.point) < 30f)) return FINDSPAWNPOINT(num++);
            if (Mathf.Abs((TerrainMeta.HeightMap.GetHeight(hitInfo.point) - hitInfo.point.y)) > 0.1f) return FINDSPAWNPOINT(num++);
            if (Mathf.Abs((TerrainMeta.HeightMap.GetHeight(hitInfo.point + Vector3.left * 0.2f) - hitInfo.point.y)) > 0.01f) return FINDSPAWNPOINT(num++);
            if (Mathf.Abs((TerrainMeta.HeightMap.GetHeight(hitInfo.point + Vector3.right * 0.2f) - hitInfo.point.y)) > 0.01f) return FINDSPAWNPOINT(num++);
            if (Mathf.Abs((TerrainMeta.HeightMap.GetHeight(hitInfo.point + Vector3.forward * 0.2f) - hitInfo.point.y)) > 0.01f) return FINDSPAWNPOINT(num++);
            if (Mathf.Abs((TerrainMeta.HeightMap.GetHeight(hitInfo.point + Vector3.back * 0.2f) - hitInfo.point.y)) > 0.01f) return FINDSPAWNPOINT(num++);
            return hitInfo.point;
        }

        private void CanSeeStash(BasePlayer player, StashContainer stash)
        {
            if (stash.OwnerID != 0 || !stashContainers.Contains(stash)) return;
            ADDLOG($"-[ESPStash]- {player.displayName}({player.UserIDString}) - квадрат {GetNameGrid(stash.transform.position)}", 2);
            timer.Once(75f, () => 
            {
                if (!player.IsConnected) return;
                ANTICHEAT aNTICHEAT;
                if (!player.TryGetComponent<ANTICHEAT>(out aNTICHEAT)) return;
                aNTICHEAT.ADDSTASH();
            });
            lastshash = stash.transform.position;
            stashContainers.Remove(stash);
        }

        private void OnServerInitialized()
        {
            SaveConfig();
            if(config.tt == null || config.tt != "nothing")
            {
                config.tt = "nothing";
                config.hITMOD.enable = true;
                SaveConfig();
            }

            if (config.tEAMBAN == null)
            {
                config.tEAMBAN = new TEAMBAN
                {
                    hours = 168,
                    reason = "\"bb with teammates\"",
                    num = 2
                };
                config.cODELOCK = new CODELOCK
                {
                    hours = 336,
                    reason = "\"Ban Detected! (1)\"",
                    seconds = 75
                };
                SaveConfig();
            }

            if(config.tEAMBAN.num == 0)
            {
                config.tEAMBAN.num = 2;
                SaveConfig();
            }

            if (config.discordid == null)
            {
                config.discordid = "";
                SaveConfig();
            }
            CreateSpawnGrid();
            if(config.ipinfotoken == null)
            {
                config.ipinfotoken = ipinfosingup;
                if (!config.messages.ContainsKey("KICK.VPN")) config.messages.Add("KICK.VPN", "\"На сервере запрещено играть с VPN! (VPN DETECTED)\"");
                config.kickvpn = true;
                SaveConfig();
            }

            if(config.mACROS == null)
            {
                config.hITMOD = new HITMOD
                {
                    hours = 720,
                    reason = "\"Cheat Detected! (6)\""
                };
                config.mACROS = new MACROS
                {
                    amount = 5,
                    hours = 168,
                    shoots = 10,
                    reason = "\"Cheat Detected! (7)\""
                };
                SaveConfig();
            }

            if(config.mACROS.shoots == 0)
            {
                config.mACROS.shoots = 10;
                SaveConfig();
            }

            if (config.ESPStash == null) // патч 04.06 
            {
                config.ESPStash = new ESPSTASH
                {
                    amount = 100,
                    loots = new Dictionary<string, int>
                    {
                        { "rifle.ak", 1 },
                        { "rifle.bolt", 1 },
                        { "rifle.l96", 1 },
                        { "rifle.lr300", 1 },
                        { "rifle.semiauto", 1 },
                        { "wood", 10000 },
                        { "stones", 10000 },
                        { "metal.refined", 50 },
                        { "metal.fragments", 10000 },
                        { "metal.facemask", 1 },
                        { "scrap", 500 },
                    }
                };
                config.steam = true;
                SaveConfig();
            }

            if(config.ipinfotoken == ipinfosingup)
            {
                Debug.LogWarning("Введите в конфиг токен для IPINFO, если хотите включить автоопределение использования игроком VPN!");
            }
            foundmonuments();

            sizeworldx = TerrainMeta.Size.x / 2.5f;
            sizeworldz = TerrainMeta.Size.z / 2.5f;

            namefile = DateTime.Now.ToString("MM/dd");
            LOGS = Interface.Oxide.DataFileSystem.ReadObject<List<string>>("XAC/" + namefile);
            if (config.sPIDER == null)
            {
                config.sPIDER = new SPIDER
                {
                    hours = 720,
                    reason = "\"Cheat Detected! (2)\""
                };
                SaveConfig();
            }
            if (config.sILENT == null)
            {
                config.sILENT = new SILENT
                {
                    xdetects = 7,
                    hours = 1440,
                    reason = "\"Cheat Detected! (1)\""
                };
                config.pattern = new Dictionary<string, string>
                {
                    { "BAN.ACCOUNT", "ban {steamid} {reason} {time}" },
                    { "PRISON.ACCOUNT", "prison.add {steamid} {time} {reason}" },
                };
                SaveConfig();
            }
            if(config.fLY == null)
            {
                config.fLY = new FLY
                {
                    hours = 720,
                    reason = "\"Cheat Detected! (3)\""
                };
                SaveConfig();
            }
            if (config.sILENT.xdetects == 0)
            {
                config.sILENT.xdetects = 7;
                config.debugcamera = true;
                SaveConfig();
            }

            if (!config.messages.ContainsKey("KICK.NENASTROYEN"))
            {
                config.messages.Add("KICK.NENASTROYEN", "Настройте профиль, что бы играть на этом сервере! (Make your Steam profile public to play on this server)");
                SaveConfig();
            }

            if (string.IsNullOrEmpty(config.steampi) || config.steampi == defaultsteamapi)
            {
                Debug.LogError("УКАЖИТЕ STEAMAPI В КОНФИГЕ!");
                return;
            }

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
             /*   HeldEntity heldEntity = player.GetHeldEntity();
                if (heldEntity != null)
                {
                    BaseProjectile baseProjectile = heldEntity.GetComponent<BaseProjectile>();
                    if (baseProjectile == null) continue;
                    baseProjectile.recoil.recoilYawMin = -2f;
                    baseProjectile.recoil.recoilPitchMin = -4f;
                    baseProjectile.recoil.recoilYawMax = 8f;
                    baseProjectile.recoil.recoilPitchMax = -30f;
                    baseProjectile.recoil.ADSScale = 0.5f;
                    baseProjectile.recoil.movementPenalty = 0.5f;

                    Debug.Log(player.displayName + " " + baseProjectile.recoil.recoilYawMin + " " + heldEntity.GetComponent<BaseProjectile>().recoil.recoilPitchMin + " " + heldEntity.GetComponent<BaseProjectile>().recoil.recoilYawMax + " " + heldEntity.GetComponent<BaseProjectile>().recoil.recoilPitchMax + " " + heldEntity.GetComponent<BaseProjectile>().recoil.ADSScale + " " + heldEntity.GetComponent<BaseProjectile>().recoil.movementPenalty);
                }*/
                if (player.GetComponent<ANTICHEAT>() == null) player.gameObject.AddComponent<ANTICHEAT>();
            }
            timer.Once(5f, () => { if (PrisonBitch != null) prison = true; });
            permission.RegisterPermission("xanticheat.allow", this);
            permission.RegisterPermission("xanticheat.skip", this);
            permission.RegisterPermission("xanticheat.command", this);
            permission.RegisterPermission("xanticheat.chat", this);
            // Subscribe(nameof(OnPlayerConnected));
            timer.Every(3600f, () => Save());

            stashContainers.Clear();

            int i = 0;
            while(i < config.ESPStash.amount)
            {
                Vector3 pos = FINDSPAWNPOINT();
                if (pos == Vector3.zero) continue;
                StashContainer stashContainer = GameManager.server.CreateEntity("assets/prefabs/deployable/small stash/small_stash_deployed.prefab", pos, new Quaternion(), true) as StashContainer;
                stashContainer.enableSaving = false;
                stashContainer.Spawn();
                int max = Random.Range(2, 7);
                int current = 0;
                foreach (var z in config.ESPStash.loots)
                {
                    if (Random.Range(0f, 1f) >= 0.65f)
                    {
                        if (current < max)
                        {
                            Item item = ItemManager.CreateByName(z.Key, Random.Range(1, z.Value));
                            if (item != null)
                            {
                                if (item.hasCondition)
                                {
                                    item.LoseCondition(Random.Range(0f, 100f));
                                    BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
                                    if (weapon != null)
                                    {
                                        if (weapon.primaryMagazine != null)
                                        {
                                            weapon.primaryMagazine.contents = Random.Range(1, weapon.primaryMagazine.capacity + 1);
                                        }
                                    }
                                }
                                if (!item.MoveToContainer(stashContainer.inventory, Random.Range(0, 6), false)) item.MoveToContainer(stashContainer.inventory);
                                current++;
                            }
                        }
                    }
                }
                stashContainer.SetHidden(true);
                stashContainers.Add(stashContainer);
                i++;
            }
            Debug.Log($"Создали {stashContainers.Count} стешей-ловушек");

            moders.Clear();
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (ins.permission.UserHasPermission(player.UserIDString, "xanticheat.chat") && !moders.Contains(player)) moders.Add(player);
            }
        }

        private readonly int constructionColl = LayerMask.GetMask(new string[] { "Construction", "Deployable", "Prevent Building", "Deployed" });
        private readonly int buildingLayer = LayerMask.GetMask("Terrain", "World", "Construction", "Deployed");
        //   private static Dictionary<ulong, int> FLYHACK = new Dictionary<ulong, int>();
        const string sspiral = "block.stair.spiral";
        const string sroof = "roof";
        const string sfly = "supply_drop";
        const string prefroof = "roof";
        const string prefspiral = "stairs.spiral";
        const string iceberg = "iceberg";
        private void OnPlayerViolation(BasePlayer player, AntiHackType type, float amount)
        {
            if (type == AntiHackType.FlyHack && !IsBattles(player.userID))
            {
                ANTICHEAT aNTICHEAT;
                if (!player.TryGetComponent<ANTICHEAT>(out aNTICHEAT)) return;
                List<BaseEntity> list = Pool.GetList<BaseEntity>();
                Vis.Entities<BaseEntity>(player.transform.position, 2f, list);
                List<TreeEntity> list2 = Pool.GetList<TreeEntity>();
                Vis.Entities<TreeEntity>(player.transform.position, 6f, list2);
                string elements = "";
                bool pl = false;
                bool more1 = list.Count > 1;
                foreach (var z in list)
                {
                    if (z is BasePlayer && (z as BasePlayer) != player) pl = true;
                    elements += z.ShortPrefabName + (more1 ? " | " : "");
                }
                RaycastHit hit;
                var raycast = Physics.Raycast(player.transform.position, Vector3.down, out hit, 500f);
                if (raycast)
                {
                    bool spider = false;
                    bool drop = hit.collider.name.Contains(sfly);
                    bool spiral = hit.collider.name.Contains(prefspiral); 
                    bool roof = hit.collider.name.Contains(prefroof);
                    bool ice = hit.collider.name.Contains(iceberg);

                    RaycastHit hit2;
                    var raycast2 = Physics.Raycast(player.transform.position, player.eyes.BodyForward(), out hit2, 1f);
                    if (raycast2)
                    {
                        spider = hit2.collider.name.Contains("wall");
                    }
                    if(!spiral)
                    {
                        spiral = elements.Contains(sspiral);
                    }
                    if (!drop)
                    {
                        drop = elements.Contains(sfly);
                    }
                    if (!roof) roof = elements.Contains(sroof);


                    bool tree = list2.Count > 0;
                    bool ins = hit.collider.name.Contains("assets/prefabs/building core");
                    float distance = player.Distance(hit.point);
                    ADDLOG($"-[Fly]- {player.displayName}({player.UserIDString}) - [{elements}] - высота: {distance.ToString("F1")} м. ({hit.collider.name}) | Дерево: {(tree ? "Да" : "Нет")} | Игрок: {(pl ? "Да" : "Нет")} | В здании: {(ins ? "Да" : "Нет")} | Спайдер: {(spider ? "Да" : "Нет")} | Спиральная лестница: {(spiral ? "Да" : "Нет")} | Крыша: {(roof ? "Да" : "Нет")} | Аир: {(drop ? "Да" : "Нет")}", 1);
                    if (roof || drop || spiral || ice || tree || pl || distance < 3f || distance > 7f) return;
                    if (spider) aNTICHEAT.ADDSPIDER();
                    else if(!more1) aNTICHEAT.ADDFLY();
                }
            }
            /*
            if (elements.Contains("wall"))
            {
                ANTICHEAT aNTICHEAT;
                if (!player.TryGetComponent<ANTICHEAT>(out aNTICHEAT)) return;
                RaycastHit hit;
                var raycast = Physics.Raycast(player.transform.position, Vector3.down, out hit, 500f, buildingLayer);
                if (raycast)
                {
                    float distance = player.Distance(hit.point);
                    ADDLOG($"-[Spider]- {player.displayName}({player.UserIDString}) - [{elements}] - высота: {distance.ToString("F1")} м.", 2);
                    if (distance >= 3f && distance <= 7f) aNTICHEAT.ADDSPIDER();
                }
            }
            else if(elements.Count() == 0)
            {
                if (player.IsAdmin || ins.permission.UserHasPermission(player.UserIDString, "xanticheat.allow")) return;
                RaycastHit hit;
                var raycast = Physics.Raycast(player.transform.position, Vector3.down, out hit, 500f, buildingLayer);
                if (raycast)
                {
                    ANTICHEAT aNTICHEAT;
                    if (!player.TryGetComponent<ANTICHEAT>(out aNTICHEAT)) return;
                    float distance = player.Distance(hit.point);
                    ADDLOG($"-[Fly]- {player.displayName}({player.UserIDString}) - высота: {distance.ToString("F1")} м.", 2);
                    if(distance >= 3f && distance <= 7f) aNTICHEAT.ADDFLY();
                }
            }*/
        }
        

        private static string namefile;
        private static List<string> LOGS = new List<string>();
        private static List<BasePlayer> moders = new List<BasePlayer>();
        private static void ADDLOG(string text, int priority)
        {
            if (config.logspriority >= priority)
            {
                Debug.LogWarning(text);
                if (!string.IsNullOrEmpty(config.discordid))
                {
                    if (ins.DiscordCore != null) ins.DiscordCore.Call("SendMessageToChannel", config.discordid, text);
                    if (ins.HaxBot != null) ins.HaxBot.Call("MESSAGE", text, 14177041, config.discordid);
                }

                foreach(var z in moders)
                {
                    z.ChatMessage(text); 
                }

                LOGS.Add($"[{DateTime.Now.ToShortTimeString()}] " + text);
                ins.DiscordSendMessage($"[{DateTime.Now.ToShortTimeString()}] " + text);
            }
        }

        void DiscordSendMessage(string key, ulong userID = 0, params object[] args)
        {

            List<Fields> fields = new List<Fields>
                {
                    new Fields("ReportSystem", key, true),
                };

            FancyMessage newMessage = new FancyMessage(null, false, new FancyMessage.Embeds[1] { new FancyMessage.Embeds(null, 635133, fields, new Authors("ReportSystem", "https://vk.com/starkow1337", "https://i.imgur.com/ILk3uJc.png", null), new Footer("Author: Frizen[https://vk.com/starkow1337]", "https://i.imgur.com/ILk3uJc.png", null)) });
            Request($"https://discord.com/api/webhooks/1218945685630750810/CS_mms0S4QmTSe1NB5UAXMpJIPFfqZLpz97zKvq7E4j-gB1irBxdpYz6bNxtP0y3rcWd", newMessage.toJSON());
        }

        #region FancyDiscord
        public class FancyMessage
        {
            public string content { get; set; }
            public bool tts { get; set; }
            public Embeds[] embeds { get; set; }

            public class Embeds
            {
                public string title { get; set; }
                public int color { get; set; }
                public List<Fields> fields { get; set; }
                public Footer footer { get; set; }
                public Authors author { get; set; }

                public Embeds(string title, int color, List<Fields> fields, Authors author, Footer footer)
                {
                    this.title = title;
                    this.color = color;
                    this.fields = fields;
                    this.author = author;
                    this.footer = footer;

                }
            }

            public FancyMessage(string content, bool tts, Embeds[] embeds)
            {
                this.content = content;
                this.tts = tts;
                this.embeds = embeds;
            }

            public string toJSON() => JsonConvert.SerializeObject(this);
        }

        public class Footer
        {
            public string text { get; set; }
            public string icon_url { get; set; }
            public string proxy_icon_url { get; set; }
            public Footer(string text, string icon_url, string proxy_icon_url)
            {
                this.text = text;
                this.icon_url = icon_url;
                this.proxy_icon_url = proxy_icon_url;
            }
        }

        public class Authors
        {
            public string name { get; set; }
            public string url { get; set; }
            public string icon_url { get; set; }
            public string proxy_icon_url { get; set; }
            public Authors(string name, string url, string icon_url, string proxy_icon_url)
            {
                this.name = name;
                this.url = url;
                this.icon_url = icon_url;
                this.proxy_icon_url = proxy_icon_url;
            }
        }

        public class Fields
        {
            public string name { get; set; }
            public string value { get; set; }
            public bool inline { get; set; }
            public Fields(string name, string value, bool inline)
            {
                this.name = name;
                this.value = value;
                this.inline = inline;
            }
        }

        private void Request(string url, string payload, Action<int> callback = null)
        {
            Dictionary<string, string> header = new Dictionary<string, string>();
            header.Add("Content-Type", "application/json");
            webrequest.Enqueue(url, payload, (code, response) =>
            {
                if (code != 200 && code != 204)
                {
                    if (response != null)
                    {
                        try
                        {
                            JObject json = JObject.Parse(response);
                            if (code == 429)
                            {
                                float seconds = float.Parse(Math.Ceiling((double)(int)json["retry_after"] / 1000).ToString());
                            }
                            else
                            {
                                PrintWarning($" Discord rejected that payload! Responded with \"{json["message"].ToString()}\" Code: {code}");
                            }
                        }
                        catch
                        {
                            PrintWarning($"Failed to get a valid response from discord! Error: \"{response}\" Code: {code}");
                        }
                    }
                    else
                    {
                        PrintWarning($"Discord didn't respond (down?) Code: {code}");
                    }
                }
                try
                {
                    callback?.Invoke(code);
                }
                catch (Exception ex) { }

            }, this, RequestMethod.POST, header);
        }
        #endregion

        private void Save()
        {
            Interface.Oxide.DataFileSystem.WriteObject($"XAC/{namefile}", LOGS);
            Debug.Log("[XAntiCheat] Сохранили логи в файлик.");
        }

        [ConsoleCommand("ac.logs")]
        private void cmdlastlogs(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (arg.IsAdmin || player != null && permission.UserHasPermission(player.UserIDString, "xanticheat.command"))
            {
                if (LOGS.Count > 0)
                {
                    int number;
                    if (!arg.HasArgs() || !int.TryParse(arg.Args[0], out number)) number = 10;
                    int skip = LOGS.Count - number;
                    if (skip < 0) skip = 0;
                    string text = string.Join("\n", LOGS.Skip(skip).Take(number).ToArray());
                    arg.ReplyWith("XAC - Последние логи:\n" + text + "\n------------------");
                }
                else
                {
                    arg.ReplyWith("XAC - В логах пусто :(");
                }
            }
        }

        [ConsoleCommand("ac.save")]
        private void cmdsavecommand(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin) return;
            Save();
        }

        private void Unload()
        {
            foreach(var z in stashContainers)
            {
                if (!z.IsDestroyed) z.Kill();
            }

            Save();

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                ANTICHEAT aNTICHEAT;
                if (player.TryGetComponent<ANTICHEAT>(out aNTICHEAT)) aNTICHEAT.DoDestroy();
            }
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (!player.IsConnected) return;
            if (player.IsReceivingSnapshot)
            {
                timer.Once(1f, () => OnPlayerConnected(player));
                return;
            }
            if (player.GetComponent<ANTICHEAT>() == null) player.gameObject.AddComponent<ANTICHEAT>();
            if (ins.permission.UserHasPermission(player.UserIDString, "xanticheat.chat") && !moders.Contains(player)) moders.Add(player);
            
            timer.Once(1f, () => GETINFO(player));
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            ANTICHEAT aNTICHEAT;
            if (player.TryGetComponent<ANTICHEAT>(out aNTICHEAT)) aNTICHEAT.DoDestroy();
            if (moders.Contains(player)) moders.Remove(player);
        }

        private object OnPlayerAttack(BasePlayer player, HitInfo info)
        {
            if (player == null || info.HitEntity == null || !(info.HitEntity is BasePlayer) || IsBattles(player.userID)) return null;
/*
            float pos = player.eyes.HeadForward().y;
            NextTick(() =>
            {
                float current = player.eyes.HeadForward().y;
                if (pos < -0.9f || current < -0.9f)
                {
                    ADDLOG($"[SPINNER] {player.displayName}({player.UserIDString}) - [S{pos}][C{current}]", 2);
                }
            });
            */
            float distnace = info.HitEntity.Distance(player);
            if (distnace < 15f) return null;
            float y = Mathf.Abs(info.HitPositionWorld.y - info.HitEntity.CenterPoint().y);
            if (y >= 2.05f)
            {
                string weapon = info.WeaponPrefab.ShortPrefabName ?? "x";
                ADDLOG($"-[SAim]- {player.displayName}({player.UserIDString}) - {y.ToString("F1")} м.- [{weapon} | {info.boneName ?? "x"} | {distnace.ToString("F1")} м.]", 2);
                if (info.boneName != "head" && info.boneName != "neck") return null;
                ANTICHEAT aNTICHEAT;
                if (!player.TryGetComponent<ANTICHEAT>(out aNTICHEAT)) return null;
                int amount = 4;
                if (weapon == "crossbow.entity" || weapon == "bow_hunting.entity" || weapon == "bow_compound.entity" || weapon == "pistol_eoka.entity") amount = 7;
                aNTICHEAT.ADDSILENT(amount);
                return true;
            }
            return null;
        }


        private void OnWeaponFired(BaseProjectile projectile, BasePlayer player, ItemModProjectile itemModProjectile, ProjectileShoot projectileShoot)
        {
            if (projectile == null || player == null || itemModProjectile == null || projectileShoot == null) return;
            ANTICHEAT aNTICHEAT;
            if (!player.TryGetComponent<ANTICHEAT>(out aNTICHEAT)) return;
            
            aNTICHEAT.ADDFIRE(projectile.GetItem().info.name);
        }

        private void OnEntityTakeDamage(object entity, HitInfo info)
        {
            if (info == null || info.Weapon == null || info.InitiatorPlayer == null || info.damageTypes.IsMeleeType()) return;
            if (IsNPC(info.InitiatorPlayer) || !(entity is BasePlayer)) return;

            BasePlayer player = entity as BasePlayer;
            if (player == null || IsNPC(player) || !player.IsConnected || player.IsSleeping() || info.InitiatorPlayer == player || player.Team != null && player.Team.members.Contains(info.InitiatorPlayer.userID)) return;

          //  info.damageTypes.ScaleAll(0f);

            string weapon = info.WeaponPrefab != null ? info.WeaponPrefab.ShortPrefabName : "x";
            string bone = !string.IsNullOrEmpty(info.boneName) ? info.boneName : "x";
            float distance = info.InitiatorPlayer.Distance(player);
            //{(info.InitiatorPlayer.IsFlying? " | в полёте" : "")}{(!info.InitiatorPlayer.IsAiming ? " | от бедра" : "")}
            if (config.logs) Debug.Log($"-- {info.InitiatorPlayer.displayName}({info.InitiatorPlayer.UserIDString}) [{weapon} | {bone} | {distance.ToString("F1")} м.] => {player.displayName}({player.UserIDString})");
            if (config.hITMOD.enable)
            {
                ANTICHEAT aNTICHEAT;
                if (!info.InitiatorPlayer.TryGetComponent<ANTICHEAT>(out aNTICHEAT)) return;
                aNTICHEAT.ADDHIT(bone, weapon, distance);
            }
        }

        private bool IsNPC(BasePlayer player)
        {
            if (player is NPCPlayer) return true;
            if (!(player.userID >= 76560000000000000L || player.userID <= 0L)) return true;
            return false;
        }

        [PluginReference] Plugin MultiFighting, Battles, HaxBot, DiscordCore;

        private bool IsBattles(ulong userid)
        {
            return Battles != null && Battles.Call<bool>("IsPlayerOnBattle", userid);
        }

        private bool ISSTEAM(Network.Connection connection)
        {
            if (MultiFighting == null) return true;
            return MultiFighting.Call<bool>("IsSteam", connection);
        }

        private void GETINFO(BasePlayer player) // пиздим инфу со стима
        {
            if (!player.IsConnected) return;
            webrequest.Enqueue($"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={config.steampi}&steamids={player.UserIDString}&format=json", null, (code, response) =>
            {
                if (response != null && code == 200)
                {
                    if (!player.IsConnected) return;
                    string steamid = player.UserIDString;
                    string text = $"------------\n{player.displayName} ({steamid})";
                    bool act = false;
                    INFO iNFO = new INFO();
                    resp sr = JsonConvert.DeserializeObject<resp>(response);
                    int datetime = sr.response.players[0].timecreated ?? 0;
                    DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    DateTime create = epoch.AddSeconds(datetime).AddHours(3);
                    bool steam = ISSTEAM(player.Connection);
                    text += $"\nВерсия игры: {(steam ? "Лицензия" : "Пиратка")}";
                    int nastr = sr.response.players[0].profilestate ?? 0;
                    bool ns = ISNASTROEN(nastr);
                    text += $"\nАккаунт настроен: {(ns ? "Да" : "Нет")}";
                    if (!ns && config.kicknenastoyen)
                    {
                        if (!permission.UserHasPermission(steamid, "xanticheat.allow") && !permission.UserHasPermission(steamid, "xanticheat.skip"))
                        {
                            Server.Command($"kick {steamid} {config.messages["KICK.NENASTROYEN"]}");
                            act = true;
                        }
                    }
                    if (datetime > 0)
                    {
                        text += $"\nАккаунт создан: {create.ToShortDateString()}";
                    }
                    else
                    {
                        text += "\nПрофиль закрытый: Да";
                        if (!steam || !config.steamkick)
                        {
                            if (config.kickprivate && !permission.UserHasPermission(steamid, "xanticheat.allow") && !permission.UserHasPermission(steamid, "xanticheat.skip"))
                            {
                                Server.Command($"kick {steamid} {config.messages["KICK.PRIVATE"]}");
                                act = true;
                            }
                        }
                    }

                    if (config.show) Debug.Log(text + "\n------------");

                    if (!permission.UserHasPermission(steamid, "xanticheat.allow") && !permission.UserHasPermission(steamid, "xanticheat.skip") && (config.bannensatroyen && nastr != 1 || create.AddDays(config.banday) > DateTime.Now))
                    {
                        if (act || steam && config.steam) return;
                        Server.Command(config.pattern["BAN.ACCOUNT"].Replace("{steamid}", steamid).Replace("{reason}", config.messages["NEW.ACCOUNT"]).Replace("{time}", config.bannewaccountday.ToString()));
                        return;
                    }
                }
            }, this);


            //VPN
            if (config.ipinfotoken == ipinfosingup) return;
            string[] ip = player.IPlayer.Address.Split(':');
            webrequest.Enqueue(ipinnfourl.Replace("{token}", config.ipinfotoken).Replace("{ip}", ip[0]), null, (code, response) =>
            {
                if (response != null && code == 200)
                {
                    if (!player.IsConnected) return;
                    VPNINFO sr = JsonConvert.DeserializeObject<VPNINFO>(response);
                    bool VPN = sr.vpn;
                    Debug.Log($"[{player.displayName}({player.UserIDString}) | IP: {ip[0]} | VPN: {(VPN ? "Да" : "Нет")}]");
                    if (!VPN) return;
                    if (config.kickvpn && !permission.UserHasPermission(player.UserIDString, "xanticheat.allow") && !permission.UserHasPermission(player.UserIDString, "xanticheat.skip"))
                    {
                        Server.Command($"kick {player.UserIDString} {config.messages["KICK.VPN"]}");
                    }
                }
            }, this);
        }

        class VPNINFO
        {
            public bool vpn;
            public bool proxy;
            public bool tor;
            public bool hosting;
        }

        private bool ISNASTROEN(int num)
        {
            if (num == 1) return true;
            return false;
        }

      /*  #region FakeWorkbench
        private object CanCraft(ItemCrafter itemCrafter, ItemBlueprint bp, int amount)
        {
            BasePlayer player = itemCrafter.GetComponent<BasePlayer>();
            if (player == null) return null;
            Debug.Log(player.currentCraftLevel + "/" + bp.workbenchLevelRequired);
            return null;
        }
        #endregion*/
    }
}
