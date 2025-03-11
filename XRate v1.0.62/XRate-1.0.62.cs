using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Random = UnityEngine.Random;
using System;
using Oxide.Core;
using ConVar;
using ru = Oxide.Game.Rust;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("XRate", "POPSI -RUST", "1.0.62")]
    public class XRate : RustPlugin
    {
        #region Config
        private PluginConfig config;
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

        class rateset
        {
            [JsonProperty("Поднимаемые ресурсы")]
            public float grab;

            [JsonProperty("Добываемые ресурсы")]
            public float gather;

            [JsonProperty("Сульфур")]
            public float sulfur;

            [JsonProperty("С карьера")]
            public float carier;

            [JsonProperty("С ящиков/бочек")]
            public float box;

            [JsonProperty("Запертый ящик")]
            public float lockbox;

            [JsonProperty("С ученых")]
            public float npc;

            [JsonProperty("Скорость переплавки")]
            public float speed;

            [JsonProperty("Ресурсы переплавки")] public int oven;
        }

        class daynight
        {
            [JsonProperty("Включить?")]
            public bool enable;

            [JsonProperty("Длина ночи")]
            public float night;

            [JsonProperty("Длина дня")]
            public float day;

            [JsonProperty("Автопропуск ночи")]
            public bool skipnight;

            [JsonProperty("Голосование за пропуск ночи")]
            public bool vote;

            [JsonProperty("Ночное увелечение рейтов (прим. 1.0 - на 100%, 0 - выключить)")]
            public float upnight;
        }

        private class PluginConfig
        {
            [JsonProperty("Экспериментально. Не трогать!")]
            public bool exp;

            [JsonProperty("Рейты у обычных игроков")]
            public rateset rates;

            [JsonProperty("Настройка дня и ночи")]
            public daynight daynight;

            [JsonProperty("Сообщения")]
            public List<string> messages;

            [JsonProperty("Привилегии")]
            public Dictionary<string, rateset> privilige;

            [JsonProperty("На что не увеличивать рейты?")]
            public string[] blacklist;

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    privilige = new Dictionary<string, rateset>()
                    {
                        { "xrate.x3", new rateset{ box = 3f, carier = 3f, gather = 3f, grab = 3f, npc = 3f, speed = 4f, sulfur = 2.5f } },
                        { "xrate.x4", new rateset{ box = 4f, carier = 4f, gather = 4f, grab = 4f, npc = 4f, speed = 4f, sulfur = 2.5f } }
                    },
                    rates = new rateset { box = 2f, carier = 2f, gather = 2f, grab = 2f, npc = 2f, speed = 2f, sulfur = 2f },
                    daynight = new daynight
                    {
                        day = 50f,
                        night = 10f,
                        enable = true,
                        skipnight = false,
                        upnight = 0f,
                        vote = false
                    },
                    exp = false,
                    messages = new List<string>
                    {
                        "<size=15><color=#ccff33>Наступила ночь</color>, рейты добычи увеличены на <color=#ccff33>{num}%</color>!</size>\n<size=10><color=#ccff33>/rate</color> - узнать текущие ваши рейты.</size>",
                        "<size=15><color=#ccff33>Наступил день</color>, рейты добычи стали прежними!</size>\n<size=10><color=#ccff33>/rate</color> - узнать текущие ваши рейты.</size>",
                        "<color=#ccff33>INFORATE | {name}</color>\nПоднимаемые: x<color=#F0E68C>{0}</color>\nДобываемые: x<color=#F0E68C>{1}</color> <size=10>(cульфур: x<color=#F0E68C>{6}</color>)</size>\nКарьер: x<color=#F0E68C>{2}</color>\nЯщики/бочки: x<color=#F0E68C>{3}</color>\nNPC: x<color=#F0E68C>{4}</color>\nСкорость переплавки: x<color=#F0E68C>{5}</color>"
                    },
                    blacklist = new string[]
                    {
                        "sticks",
                        "flare"
                    }
                };
            }
        }
        #endregion

        #region getrate
        Dictionary<string, rateset> cash = new Dictionary<string, rateset>();
        Dictionary<ulong, float> cashcariers = new Dictionary<ulong, float>();
        static XRate ins;
        void Init()
        {
            ins = this;
        }

        bool skip;
        bool isday;
        void OnHour()
        {
            if (TOD_Sky.Instance.Cycle.Hour > TOD_Sky.Instance.SunriseTime && TOD_Sky.Instance.Cycle.Hour <= 19f && !isday) OnSunrise();
            else if ((TOD_Sky.Instance.Cycle.Hour >= 19f || TOD_Sky.Instance.Cycle.Hour < TOD_Sky.Instance.SunriseTime) && isday) OnSunset();
        }

        void OnSunrise()
        {
            TOD_Sky.Instance.Components.Time.DayLengthInMinutes = daytime;
            isday = true;
            if (upnight > 1f)
            {
                Server.Broadcast(config.messages[1]);
                nightupdate();
            }
        }

        #region ГОЛОСОВАНИЕ
        const string REFRESHGUI = "[{\"name\":\"daytext\",\"parent\":\"day\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{day}\",\"fontSize\":18,\"align\":\"MiddleCenter\",\"color\":\"1 1 1 0.7921728\",\"fadeIn\":0.5},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.392941\",\"distance\":\"0.5 0.5\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmax\":\"0 0\"}]},{\"name\":\"neighttext\",\"parent\":\"night\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{night}\",\"fontSize\":18,\"align\":\"MiddleCenter\",\"color\":\"1 1 1 0.7921569\",\"fadeIn\":0.5},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3948711\",\"distance\":\"0.5 0.5\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmax\":\"0 0\"}]}]";
        const string GUI = "[{\"name\":\"Main\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0 0 0 0.2035446\",\"fadeIn\":0.5},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 1\",\"anchormax\":\"0.5 1\",\"offsetmin\":\"-100 -65\",\"offsetmax\":\"100 -35\"}]},{\"name\":\"day\",\"parent\":\"Main\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"chat.say /voteday\",\"color\":\"1 1 1 0.3929416\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"0.5 1\",\"offsetmax\":\"0 0\"}]},{\"name\":\"daytext\",\"parent\":\"day\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{day}\",\"fontSize\":18,\"align\":\"MiddleCenter\",\"color\":\"1 1 1 0.7921728\",\"fadeIn\":0.5},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.392941\",\"distance\":\"0.5 0.5\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmax\":\"0 0\"}]},{\"name\":\"night\",\"parent\":\"Main\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"chat.say /votenight\",\"color\":\"0 0 0 0.3929408\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0\",\"anchormax\":\"1 1\",\"offsetmax\":\"0 0\"}]},{\"name\":\"neighttext\",\"parent\":\"night\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{night}\",\"fontSize\":18,\"align\":\"MiddleCenter\",\"color\":\"1 1 1 0.7921569\",\"fadeIn\":0.5},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3948711\",\"distance\":\"0.5 0.5\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmax\":\"0 0\"}]}]";
        static string CONSTVOTE = "";

        void CLEARVOTE()
        {
            Vtimer?.Destroy();
            Vday = 0;
            Vnight = 0;
            voted.Clear();
        }

        void StartVote()
        {
            activevote = true;
            CLEARVOTE();
            Debug.LogWarning("-Голосование за пропуск ночи-");
            Server.Broadcast("<color=yellow>Начато голосование за пропуск ночи. Нажмите на ДЕНЬ или НОЧЬ или пропишите в чат /voteday - за день или /votenight - за ночь.</color>");
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = Network.Net.sv.connections }, null, "DestroyUI", "Main");
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = Network.Net.sv.connections }, null, "AddUI", CONSTVOTE);
            Vtimer = timer.Once(30f, () => EndVote());
        }

        void EndVote()
        {
            activevote = false;
            if (Vday > Vnight)
            {
                TOD_Sky.Instance.Cycle.Hour += (24 - TOD_Sky.Instance.Cycle.Hour) + TOD_Sky.Instance.SunriseTime;
                OnSunrise();
                Server.Broadcast("<color=yellow>Большинство проголосовало за день. Пропускаем ночь...</color>");
                Debug.LogWarning("-Пропускаем ночь-");
            }
            else
            {
                Debug.LogWarning("-Ночь остается-");
                Server.Broadcast("<color=yellow>— Да будет свет! — сказал электрик и перерезал провода.</color>");
            }
            CLEARVOTE();
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = Network.Net.sv.connections }, null, "DestroyUI", "Main");
        }

        Timer Vtimer;
        bool activevote;
        static int Vday;
        static int Vnight;
        static List<ulong> voted = new List<ulong>();

        private void REFRESHME()
        {
            List<Network.Connection> sendto = Network.Net.sv.connections.Where(x => voted.Contains(x.userid)).ToList();
            string RGUI = REFRESHGUI.Replace("{day}", Vday.ToString()).Replace("{night}", Vnight.ToString());
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = sendto }, null, "DestroyUI", "daytext");
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = sendto }, null, "DestroyUI", "neighttext");
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = sendto }, null, "AddUI", RGUI);
        }

        private void cmdvoteday(BasePlayer player, string command, string[] args)
        {
            if (!CHECKPOINT(player)) return;

            player.ChatMessage("<color=yellow>Голос за ДЕНЬ успешно принят.</color>");
            Vday++;
            voted.Add(player.userID);
            REFRESHME();
            if (Vday > BasePlayer.activePlayerList.Count * 0.6f) EndVote();
        }

        private void cmdvotenight(BasePlayer player, string command, string[] args)
        {
            if (!CHECKPOINT(player)) return;

            player.ChatMessage("<color=yellow>Голос за НОЧЬ успешно принят.</color>");
            Vnight++;
            voted.Add(player.userID);
            REFRESHME();
            if (Vnight > BasePlayer.activePlayerList.Count * 0.6f) EndVote();
        }

        bool CHECKPOINT(BasePlayer player)
        {
            if (!activevote)
            {
                player.ChatMessage("<color=yellow>ГОЛОСОВАНИЕ НЕ АКТИВНО!</color>");
                return false;
            }

            if (voted.Contains(player.userID))
            {
                player.ChatMessage("<color=yellow>ВЫ УЖЕ ГОЛОСОВАЛИ!</color>");
                return false;
            }

            return true;
        }
        #endregion

        void OnSunset()
        {
            if (skip) return;
            if (config.daynight.skipnight)
            {
                Env.time = 23.99f;
                skip = true;
                timer.Once(8f, () =>
                {
                    Env.time = TOD_Sky.Instance.SunriseTime;
                    skip = false;
                });
                Debug.Log("Пропускаем ночь.");
                return;
            }
            else if (config.daynight.vote) StartVote();

            TOD_Sky.Instance.Components.Time.DayLengthInMinutes = nighttime;
            isday = false;
            if (upnight > 1f)
            {
                Server.Broadcast(config.messages[0].Replace("{num}", (config.daynight.upnight * 100f).ToString()));
                nightupdate();
            }
        }

        void nightupdate()
        {
            if (cash.Count > 0) foreach (var id in cash.ToList()) getuserrate(id.Key);
            if (cashcariers.Count > 0) foreach (var id in cashcariers.ToList()) CashCarier(id.Key);
        }

        float daytime;
        float nighttime;
        float upnight;
        TOD_Time comp;
        void OnServerInitialized()
        {
            if(config.blacklist == null || config.blacklist.Length == 0)
            {
                config.blacklist = new string[]
                {
                    "sticks",
                    "flare"
                };
                SaveConfig();
            }

            if(config.daynight == null)
            {
                config.daynight = new daynight
                {
                    day = 50f,
                    night = 10f,
                    enable = true,
                    skipnight = false,
                    upnight = 0f,
                    vote = false
                };
                SaveConfig();
            }

            if (config.rates.lockbox == 0f)
            {
                config.rates.lockbox = config.rates.box;
                foreach (var x in config.privilige) x.Value.lockbox = x.Value.box;
                SaveConfig();
            }

            if (config.daynight.enable)
            {
                if (config.daynight.vote)
                {
                    CONSTVOTE = GUI.Replace("{day}", "ДЕНЬ").Replace("{night}", "НОЧЬ");
                    Interface.Oxide.GetLibrary<ru.Libraries.Command>(null).AddChatCommand("voteday", this, "cmdvoteday");
                    Interface.Oxide.GetLibrary<ru.Libraries.Command>(null).AddChatCommand("votenight", this, "cmdvotenight");
                }
                daytime = config.daynight.day * 24f / (19f - TOD_Sky.Instance.SunriseTime);
                nighttime = config.daynight.night * 24f / (24f - (19f - TOD_Sky.Instance.SunriseTime));
                upnight = 1f + config.daynight.upnight;
                comp = TOD_Sky.Instance.Components.Time;
                comp.ProgressTime = true;
                comp.UseTimeCurve = false;
                comp.OnSunrise += OnSunrise;
                comp.OnSunset += OnSunset;
                comp.OnHour += OnHour;

                if (TOD_Sky.Instance.Cycle.Hour > TOD_Sky.Instance.SunriseTime && TOD_Sky.Instance.Cycle.Hour <= 19f) OnSunrise();
                else OnSunset();
            }

            var ovens = UnityEngine.Object.FindObjectsOfType<BaseOven>();
            for (var i = 0; i < ovens.Length; i++)
            {
                OnEntitySpawned(ovens[i]);
            }
            timer.Once(1f, () =>
            {
                foreach (BaseOven oven in ovens)
                {
                    var component = oven.GetComponent<FurnaceController>();
                    if (oven == null || oven.IsDestroyed || !oven.IsOn()) continue;
                    component.StartCooking();
                }
            });
            foreach (string perm in config.privilige.Keys) permission.RegisterPermission(perm, this);
            foreach (BasePlayer player in BasePlayer.activePlayerList) getuserrate(player.UserIDString);
        }

        void OnGroupPermissionGranted(string name, string perm)
        {
            foreach (BasePlayer player in BasePlayer.allPlayerList)
            {
                if (permission.UserHasGroup(player.UserIDString, name)) getuserrate(player.UserIDString);
            }
        }

        void OnGroupPermissionRevoked(string name, string perm)
        {
            foreach (BasePlayer player in BasePlayer.allPlayerList)
            {
                if (permission.UserHasGroup(player.UserIDString, name)) getuserrate(player.UserIDString);
            }
        }

        void OnUserGroupRemoved(string id, string groupName)
        {
            getuserrate(id);
        }

        void OnUserGroupAdded(string id, string groupName)
        {
            getuserrate(id);
        }

        void OnUserPermissionGranted(string id, string permName)
        {
            getuserrate(id);
        }

        void OnUserPermissionRevoked(string id, string permName)
        {
            getuserrate(id);
        }

        void OnPlayerConnected(BasePlayer player)
        {
            getuserrate(player.UserIDString);
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (cash.ContainsKey(player.UserIDString)) cash.Remove(player.UserIDString);
        }
        #endregion

        #region Rates
        [ChatCommand("rate")]
        private void cmdRATE(BasePlayer player, string command, string[] args)
        {
            player.ChatMessage(config.messages[2].Replace("{name}", player.displayName).Replace("{0}", cash[player.UserIDString].grab.ToString()).Replace("{1}", cash[player.UserIDString].gather.ToString()).Replace("{2}", cash[player.UserIDString].carier.ToString()).Replace("{3}", cash[player.UserIDString].box.ToString()).Replace("{4}", cash[player.UserIDString].npc.ToString()).Replace("{5}", cash[player.UserIDString].speed.ToString()).Replace("{6}", cash[player.UserIDString].sulfur.ToString()));
        }

        [PluginReference] private Plugin ZREWARDME;

        void getuserrate(string id, float bonus = 0f)
        {
            rateset rate = config.privilige.LastOrDefault(x => permission.UserHasPermission(id, x.Key)).Value ?? config.rates;
            if (!cash.ContainsKey(id)) cash[id] = new rateset();
            if (ZREWARDME != null && bonus == 0f) bonus = ZREWARDME.Call<float>("APIBONUS", id);
            if (upnight > 1f && !isday)
            {
                cash[id].box = rate.box * upnight;
                cash[id].carier = rate.carier * upnight;
                cash[id].gather = rate.gather * upnight;
                cash[id].grab = rate.grab * upnight;
                cash[id].lockbox = rate.lockbox * upnight;
                cash[id].npc = rate.npc * upnight;
                cash[id].sulfur = rate.sulfur * upnight;
            }
            else
            {
                cash[id].box = rate.box;
                cash[id].carier = rate.carier;
                cash[id].gather = rate.gather;
                cash[id].grab = rate.grab;
                cash[id].lockbox = rate.lockbox;
                cash[id].npc = rate.npc;
                cash[id].sulfur = rate.sulfur;
            }

            if(bonus > 0f)
            {
                cash[id].gather += bonus;
                cash[id].grab += bonus;
                cash[id].sulfur += bonus;
            }

            cash[id].speed = rate.speed;
        }

        void OnCollectiblePickup(Item item, BasePlayer player)
        {
            if (config.blacklist.Contains(item.info.shortname)) return;
            item.amount = (int)(item.amount * cash[player.UserIDString].grab);
        }

        void OnGrowableGather(GrowableEntity plant, Item item, BasePlayer player)
        {
            if (player == null || item == null) return;
            if (config.blacklist.Contains(item.info.shortname)) return;
            item.amount = (int)(item.amount * cash[player.UserIDString].grab);
        }

        void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (config.blacklist.Contains(item.info.shortname)) return;
            if (item.info.itemid.Equals(-1157596551)) item.amount = (int)(item.amount * cash[player.UserIDString].sulfur);
            else item.amount = (int)(item.amount * cash[player.UserIDString].gather);
        }

        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (config.blacklist.Contains(item.info.shortname)) return;
            BasePlayer player = entity.ToPlayer();
            if (player != null)
            {
                if (item.info.itemid.Equals(-1157596551)) item.amount = (int)(item.amount * cash[player.UserIDString].sulfur);
                else item.amount = (int)(item.amount * cash[player.UserIDString].gather);
            }
            else
            {
                if (item.info.itemid.Equals(-1157596551)) item.amount *= (int)(item.amount * config.rates.sulfur);
                else item.amount *= (int)(item.amount * config.rates.gather);
            }
        }

        private void CashCarier(ulong id)
        {
            rateset rate = config.privilige.LastOrDefault(x => permission.UserHasPermission(id.ToString(), x.Key)).Value ?? config.rates;
            if (!isday && upnight > 1f) cashcariers[id] = rate.carier * upnight;
            else cashcariers[id] = rate.carier;
        }

        private object OnExcavatorGather(ExcavatorArm arm, Item item)
        {
            if (config.blacklist.Contains(item.info.shortname)) return null;
            item.amount = (int)(item.amount * config.rates.carier);
            return null;
        }

        private void OnQuarryToggled(BaseEntity entity, BasePlayer player)
        {
            CashCarier(entity.OwnerID);
        }

        void OnQuarryGather(MiningQuarry quarry, Item item)
        {
            if (config.blacklist.Contains(item.info.shortname)) return;
            float rate;
            if (!cashcariers.TryGetValue(quarry.OwnerID, out rate))
            {
                CashCarier(quarry.OwnerID);
                rate = cashcariers[quarry.OwnerID];
            }
            item.amount = (int)(item.amount * rate);
        }
        /*
        object OnQuarryGather(MiningQuarry quarry, List<ResourceDepositManager.ResourceDeposit.ResourceDepositEntry> itemList)
        {
            if (!cashcariers.ContainsKey(quarry.OwnerID)) CashCarier(quarry.OwnerID);
            foreach (ResourceDepositManager.ResourceDeposit.ResourceDepositEntry resource in itemList)
            {
                if ((quarry.canExtractLiquid || !resource.isLiquid) && (quarry.canExtractSolid || resource.isLiquid))
                {
                    resource.workDone += quarry.workToAdd;
                    if ((double)resource.workDone >= (double)resource.workNeeded)
                    {
                        int iAmount = Mathf.FloorToInt(resource.workDone / resource.workNeeded);
                        resource.workDone -= (float)iAmount * resource.workNeeded;
                        Item obj = ItemManager.Create(resource.type, (int)(iAmount * cashcariers[quarry.OwnerID]), 0UL);
                        if (!obj.MoveToContainer(quarry.hopperPrefab.instance.GetComponent<StorageContainer>().inventory, -1, true))
                        {
                            obj.Remove(0.0f);
                            quarry.SetOn(false);
                        }
                    }
                }
            }
            if (!quarry.FuelCheck()) quarry.SetOn(false);
            return false;
        }*/

        void OnContainerDropItems(ItemContainer container)
        {
            LootContainer lootcont = container.entityOwner as LootContainer;
            if (lootcont == null || lootcont.OwnerID != 0) return;
            var player = lootcont?.lastAttacker?.ToPlayer();
            if (lootcont.HasFlag(BaseEntity.Flags.Reserved7) || lootcont.HasFlag(BaseEntity.Flags.Reserved8)) return;
            
            if (player != null && cash.ContainsKey(player.UserIDString))
            {
                foreach (var item in lootcont.inventory.itemList)
                {
                    int maxstack = item.MaxStackable();
                    if (maxstack  == 1 || config.blacklist.Contains(item.info.shortname) || item.IsBlueprint()) continue;
                    item.amount = (int)(item.amount * cash[player.UserIDString].box);
                    if (item.amount > maxstack) item.amount = maxstack;
                }
            }
            else
            {
                foreach (var item in lootcont.inventory.itemList.Where(x => x.info.stackable > 1))
                {
                    int maxstack = item.MaxStackable();
                    if (maxstack == 1 || config.blacklist.Contains(item.info.shortname) || item.IsBlueprint()) continue;
                    item.amount = (int)(item.amount * config.rates.box);
                    if (item.amount > maxstack) item.amount = maxstack;
                }
            }
        }

        private void OnEntityDeath(BaseNetworkable entity, HitInfo info)
        {
            if (entity is BaseHelicopter && config.exp)
            {
                HackableLockedCrate ent = (HackableLockedCrate)GameManager.server.CreateEntity("assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab", entity.transform.position, entity.transform.rotation);
                ent.Spawn();
            }
        }

        private void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (player == null || entity == null) return;
            if (entity is NPCPlayerCorpse)
            {
                if (entity.HasFlag(BaseEntity.Flags.Reserved7) || entity.HasFlag(BaseEntity.Flags.Reserved8)) return;
                ItemContainer cont = entity.GetComponent<NPCPlayerCorpse>().containers.FirstOrDefault();
                foreach (var item in cont.itemList.Where(x => x.info.stackable > 1))
                {
                    int maxstack = item.MaxStackable();
                    if (maxstack == 1 || config.blacklist.Contains(item.info.shortname) || item.IsBlueprint()) continue;
                    item.amount = (int)(item.amount * cash[player.UserIDString].npc);
                    if (item.amount > maxstack) item.amount = maxstack;
                }
                entity.SetFlag(BaseEntity.Flags.Reserved7, true);
            }
            else if (entity is LootContainer)
            {
                LootContainer lootcont = entity.GetComponent<LootContainer>();
                if (lootcont == null || lootcont.HasFlag(BaseEntity.Flags.Reserved7) || lootcont.HasFlag(BaseEntity.Flags.Reserved8)) return;
                if (entity is HackableLockedCrate)
                {
                    foreach (var item in lootcont.inventory.itemList.Where(x => x.info.stackable > 1))
                    {
                        int maxstack = item.MaxStackable();
                        if (maxstack == 1 || config.blacklist.Contains(item.info.shortname) || item.IsBlueprint()) continue;
                        item.amount = (int)(item.amount * cash[player.UserIDString].lockbox);
                        if (item.amount > maxstack) item.amount = maxstack;
                    }
                }
                else
                {
                    foreach (var item in lootcont.inventory.itemList.Where(x => x.info.stackable > 1))
                    {
                        int maxstack = item.MaxStackable();
                        if (maxstack == 1 || config.blacklist.Contains(item.info.shortname) || item.IsBlueprint()) continue;
                        item.amount = (int)(item.amount * cash[player.UserIDString].box);
                        if (item.amount > maxstack) item.amount = maxstack;
                    }
                }
                lootcont.SetFlag(BaseEntity.Flags.Reserved7, true);
            }
        }
        #endregion

        #region Smelt
        private void Unload()
        {

            if (comp != null)
            {
                comp.OnSunrise -= OnSunrise;
                comp.OnSunset -= OnSunset;
                comp.OnHour -= OnHour;
            }
            if (config.daynight.vote) CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = Network.Net.sv.connections }, null, "DestroyUI", "Main");
            
            var ovens = UnityEngine.Object.FindObjectsOfType<BaseOven>();

            foreach (BaseOven oven in ovens)
            {
                var component = oven.GetComponent<FurnaceController>();
                if (oven.IsOn())
                {
                    component.StopCooking();
                    oven.StartCooking();
                }
                UnityEngine.Object.Destroy(component);
            }
        }

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity == null) return;
            else if (entity is BaseOven) entity?.gameObject?.AddComponent<FurnaceController>();
        }

        private object OnOvenToggle(StorageContainer oven, BasePlayer player)
        {
            if (oven is BaseFuelLightSource) return null;
            FurnaceController component = oven.GetComponent<FurnaceController>();
            if (component == null) component = oven.gameObject.AddComponent<FurnaceController>();
            if (oven.IsOn())
            {
                component.StopCooking();
            }
            else
            {
                component.StartCooking();
                component.SetSpeed(cash[player.UserIDString].speed);
            }
            return false;
        }

        public class FurnaceController : FacepunchBehaviour
        {
            private BaseOven _oven;
            private BaseOven Furnace
            {
                get
                {
                    if (_oven == null) _oven = GetComponent<BaseOven>();
                    return _oven;
                }
            }
            private float _speedMultiplier;
            private int amountmultiplier;
            private int amount;

            private void Awake()
            {
                SetSpeed(ins.config.rates.speed);
                amount = amountmultiplier;
            }

            public void SetSpeed(float newspeed)
            {
                _speedMultiplier = newspeed;
                amountmultiplier = ins.config.rates.oven;
            }

            private Item FindBurnable()
            {
                if (Furnace.inventory == null) return null;

                foreach (var item in Furnace.inventory.itemList)
                {
                    var component = item.info.GetComponent<ItemModBurnable>();
                    if (component && (Furnace.fuelType == null || item.info == Furnace.fuelType))
                    {
                        return item;
                    }
                }

                return null;
            }

            public void Cook()
            {
                var item = FindBurnable();
                if (item == null)
                {
                    StopCooking();
                    return;
                }

                SmeltItems();
                var slot = Furnace.GetSlot(BaseEntity.Slot.FireMod);
                if (slot) slot.SendMessage("Cook", 0.5f, SendMessageOptions.DontRequireReceiver);

                var component = item.info.GetComponent<ItemModBurnable>();
                item.fuel -= 5f;
                if (!item.HasFlag(global::Item.Flag.OnFire))
                {
                    item.SetFlag(global::Item.Flag.OnFire, true);
                    item.MarkDirty();
                }

                if (item.fuel <= 0f) ConsumeFuel(item, component);
            }

            private void ConsumeFuel(Item fuel, ItemModBurnable burnable)
            {
                if (Furnace.allowByproductCreation && burnable.byproductItem != null && Random.Range(0f, 1f) > burnable.byproductChance)
                {
                    var def = burnable.byproductItem;
                    var item = ItemManager.Create(def, burnable.byproductAmount * amountmultiplier);
                    if (!item.MoveToContainer(Furnace.inventory))
                    {
                        StopCooking();
                        item.Drop(Furnace.inventory.dropPosition, Furnace.inventory.dropVelocity);
                    }
                }
                amount = amountmultiplier;
                if (fuel.amount <= amountmultiplier)
                {
                    fuel.Remove();
                    return;
                }
                fuel.amount -= amountmultiplier;
                fuel.fuel = burnable.fuelAmount;
                fuel.MarkDirty();
            }
            private Dictionary<Item, float> cook = new Dictionary<Item, float>();
            private void SmeltItems()
            {
                for (var i = 0; i < Furnace.inventory.itemList.Count; i++)
                {
                    var item = Furnace.inventory.itemList[i];
                    if (item == null || !item.IsValid()) continue;

                    var cookable = item.info.GetComponent<ItemModCookable>();
                    if (cookable == null) continue;

                    var temperature = item.temperature;
                    if ((temperature < cookable.lowTemp || temperature > cookable.highTemp))
                    {
                        if (!cookable.setCookingFlag || !item.HasFlag(global::Item.Flag.Cooking)) continue;
                        item.SetFlag(global::Item.Flag.Cooking, false);
                        item.MarkDirty();
                        continue;
                    }
                    if (cook.ContainsKey(item)) cook[item] += 0.5f;
                    else cook[item] = 0.5f;
                    if (cook[item] < (cookable.cookTime / _speedMultiplier)) continue;
                    cook[item] = 0f;
                    if (cookable.setCookingFlag && !item.HasFlag(global::Item.Flag.Cooking))
                    {
                        item.SetFlag(global::Item.Flag.Cooking, true);
                        item.MarkDirty();
                    }
                    int position = item.position;
                    int amount2 = item.amount;
                    if (amount2 > amount)
                    {
                        item.amount -= amount;
                        item.MarkDirty();
                    }
                    else
                    {
                        item.Remove();
                    }

                    if (cookable.becomeOnCooked == null) continue;
                    int newamount = cookable.amountOfBecome * amount;
                    var item2 = ItemManager.Create(cookable.becomeOnCooked, amount2 < newamount ? amount2 : newamount);

                    if (item2 == null || item2.MoveToContainer(item.parent, position) || item2.MoveToContainer(item.parent)) continue;
                    item2.Drop(item.parent.dropPosition, item.parent.dropVelocity);
                    if (!item.parent.entityOwner) continue;
                    StopCooking();
                }
            }

            public void StartCooking()
            {
                if (FindBurnable() == null) return;

                StopCooking();

                Furnace.inventory.temperature = Furnace.cookingTemperature;
                Furnace.UpdateAttachmentTemperature();

                Furnace.InvokeRepeating(Cook, 0.5f, 0.5f);
                Furnace.SetFlag(BaseEntity.Flags.On, true);
            }

            public void StopCooking()
            {
                cook.Clear();
                Furnace.CancelInvoke(Cook);
                Furnace.StopCooking();
            }
        }
        #endregion
    }
}