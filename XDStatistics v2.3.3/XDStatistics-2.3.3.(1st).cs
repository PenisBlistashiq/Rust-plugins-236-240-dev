using Rust;
using UnityEngine;
using Oxide.Core.Libraries;
using System.Collections;
using System.Linq;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using System.Globalization;
using System;

///Скачано с дискорд сервера Rust Edit [PRO+]
///discord.gg/9vyTXsJyKR

/// Исправлена проблема с топ 10 (Иногда страница просто была пустая из за некоректного имени пользователя)
namespace Oxide.Plugins
{
    [Info("XDStatistics", "discord.gg/9vyTXsJyKR", "2.3.3")]
    [Description("Multifunctional statistics for your server!")]
    class XDStatistics : RustPlugin
    {
        private void SaveDataIgnoreList() => Interface.Oxide.DataFileSystem.WriteObject("XDStatistics/IgnorePlayers", IgnoreReservedPlayer);
        private string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary?.Call("GetImage", shortname, skin);
        private void SendImage(BasePlayer player, string imageName, ulong imageId = 0) => ImageLibrary?.Call("SendImage", player, imageName, imageId);

        private void LoadData() => Players = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerInfo>>("XDStatistics/StatsUser");
        public static Dictionary<String, String> KeyImages = new Dictionary<string, string>()
        {
            { "1", "https://i.ibb.co/cD6gHhT/1.png" },
            { "2", "https://i.ibb.co/xY2P00h/2.png" },
            { "3", "https://i.ibb.co/rvttQ2V/3.png" },
            { "4", "https://i.ibb.co/sbZW9r7/4.png" },
            { "5", "https://i.ibb.co/vzwS5xJ/5.png" },
            { "6", "https://i.ibb.co/QvHFDLh/6.png" },
            { "7", "https://i.ibb.co/Gf6rdrP/7.png" },
            { "8", "https://i.ibb.co/rGxcvtn/8.png" },
            { "9", "https://i.ibb.co/bNfPM93/9.png" },
            { "10", "https://i.ibb.co/qyS5fYj/10.png" },
            { "11", "https://i.ibb.co/hyr3nDm/11.png" },
            { "12", "https://i.ibb.co/Xk6P3W6/12.png" },
            { "13", "https://i.ibb.co/K9BTZ77/13.png" },
            { "14", "https://i.ibb.co/RptKqfK/14.png" },
            { "15", "https://i.ibb.co/z73GKWC/15.png" },
            { "16", "https://i.ibb.co/mB4JJbQ/16.png" },
            { "17", "https://i.ibb.co/RcKr3jf/17.png" },
            { "18", "https://i.ibb.co/mhbW5LY/18.png" },
            { "19", "https://i.ibb.co/3v4f7f6/19.png" },
            { "20", "https://i.ibb.co/dDsDHNS/20.png" }
        };
        private class Field
        {
            public Field(string name, string value, bool inline)
            {
                Name = name;
                Value = value;
                Inline = inline;
            }

            [JsonProperty("name")] public string Name { get; set; }
            [JsonProperty("value")] public string Value { get; set; }
            [JsonProperty("inline")] public bool Inline { get; set; }
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["STAT_TOP_FIVE_KILL"] = "Top 5 <color=#4286f4>Killers</color>\n{0}</size>",
                ["STAT_TOP_FIVE_KILL_NPC"] = "Top 5 <color=#4286f4>NPC killers</color>\n{0}</size>",
                ["STAT_TOP_FIVE_FARM"] = "Top 5 <color=#4286f4>Farmers</color>\n{0}</size>",
                ["STAT_TOP_FIVE_EXPLOSION"] = "Top 5 <color=#4286f4>Explosions</color>\n{0}</size>",
                ["STAT_TOP_FIVE_TIMEPLAYED"] = "Top 5 <color=#4286f4>Most Time Played</color>\n{0}</size>",
                ["STAT_TOP_FIVE_BUILDINGS"] = "Top 5 <color=#4286f4>Builders</color>\n{0}</size>",
                ["STAT_TOP_FIVE_SCORE"] = "Top 5 <color=#4286f4>Most points</color>\n{0}</size>",
                ["STAT_TOP_FIVE_FERMER"] = "Top 5 <color=#4286f4>Farmers</color>\n{0}</size>",
                ["STAT_USER_TOTAL_GATHERED"] = "Total Gathered:",
                ["STAT_USER_TOTAL_EXPLODED"] = "Total Explosions:",
                ["STAT_USER_TOTAL_GROWED"] = "Total Farmed:",
                ["STAT_UI_MY_STAT"] = "My Statistics",
                ["STAT_UI_TOP_TEN"] = "Top 10 Players",
                ["STAT_UI_SEARCH"] = "Search",
                ["STAT_UI_INFO"] = "Player Information {0}",
                ["STAT_UI_ACTIVITY"] = "Activity",
                ["STAT_UI_ACTIVITY_TODAY"] = "Today: {0}",
                ["STAT_UI_ACTIVITY_TOTAL"] = "All Time: {0}",
                ["STAT_UI_SETTINGS"] = "Settings",
                ["STAT_UI_PLACE_TOP"] = "Place On Top: {0}",
                ["STAT_UI_SCORE"] = "Score: {0}",
                ["STAT_UI_PVP"] = "PvP Statistics",
                ["STAT_UI_FAVORITE_WEAPON"] = "Favorite Weapon",
                ["STAT_UI_PVP_KILLS"] = "Kills",
                ["STAT_UI_PVP_KILLS_NPC"] = "Kills NPC",
                ["STAT_UI_PVP_DEATH"] = "Deaths",
                ["STAT_UI_PVP_KDR"] = "K/D",
                ["STAT_UI_FAVORITE_WEAPON_KILLS"] = "Kills: {0}\nHits: {1}",
                ["STAT_UI_FAVORITE_WEAPON_NOT_DATA"] = "Data is still being calculated..",
                ["STAT_UI_OTHER_STAT"] = "Other Statistics",
                ["STAT_UI_HIDE_STAT"] = "Public Profile",
                ["STAT_UI_CONFIRM"] = "Are You Sure?",
                ["STAT_UI_CONFIRM_YES"] = "Yes",
                ["STAT_UI_CONFIRM_NO"] = "No",
                ["STAT_UI_RESET_STAT"] = "Reset Statistics",
                ["STAT_UI_CRATE_OPEN"] = "Crates Opened: {0}",
                ["STAT_UI_BARREL_KILL"] = "Barrels Destroyed: {0}",
                ["STAT_UI_ANIMAL_KILL"] = "Animal Kills: {0}",
                ["STAT_UI_HELI_KILL"] = "Helicopter Kills: {0}",
                ["STAT_UI_BRADLEY_KILL"] = "Bradley Kills: {0}",
                ["STAT_UI_NPC_KILL"] = "NPC Kills: {0}",
                ["STAT_UI_BTN_MORE"] = "Show More",
                ["STAT_UI_CATEGORY_GATHER"] = "Gather",
                ["STAT_UI_CATEGORY_EXPLOSED"] = "Explosions",
                ["STAT_UI_CATEGORY_PLANT"] = "Farming",
                ["STAT_UI_CATEGORY_TOP_KILLER"] = "Top 10 Killers",
                ["STAT_UI_CATEGORY_TOP_KILLER_ANIMALS"] = "Top 10 Animal Killers",
                ["STAT_UI_CATEGORY_TOP_NPCKILLER"] = "Top 10 NPC Killers",
                ["STAT_UI_CATEGORY_TOP_TIME"] = "Top 10 Most Time Played",
                ["STAT_UI_CATEGORY_TOP_GATHER"] = "Top 10 Gatherers",
                ["STAT_UI_CATEGORY_TOP_SCORE"] = "Top 10 Most Score",
                ["STAT_UI_CATEGORY_TOP_EXPLOSED"] = "Top 10 Explosions",
                ["STAT_PRINT_WIPE"] = "Wipe Detected. Data was successfully cleared!",
                ["STAT_CMD_1"] = "No Permission!!",
                ["STAT_CMD_2"] = "Usage: stat.ignore <add/remove> <Steam ID|Name>",
                ["STAT_CMD_3"] = "The specified playername could not be found. Please use their SteamID.",
                ["STAT_CMD_4"] = "Found several players with similar names: {0}",
                ["STAT_CMD_5"] = "Player not found!",
                ["STAT_CMD_6"] = "Player {0} is already ignored",
                ["STAT_CMD_7"] = "You have successfully added a player {0} to the ignore list",
                ["STAT_CMD_8"] = "The player {0} is not in the ignore list",
                ["STAT_CMD_9"] = "You have successfully removed the player {0} from the ignore list",
                ["STAT_CMD_10"] = "Player {0} successfully credited {1} score",
                ["STAT_CMD_11"] = "Player {0} successfully removed {1} score",
                ["STAT_ADMIN_HIDE_STAT"] = "You've been added to the ignore list. You will not have access to the statistics. If this is an error, Please contact the Administrator!",
                ["STAT_TOP_PLAYER_WIPE_SCORE"] = "Congratulations!\nYou successfully held the {0} position in the previous wipe in the category <color=#4286f4>HIGHEST SCORE</color>\nYou received a well deserved Reward!",
                ["STAT_TOP_PLAYER_WIPE_TIME"] = "Congratulations!\nYou successfully held the {0} position in the previous wipe in the category <color=#4286f4>MOST TIME PLAYED</color>\nYou received a well deserved Reward!",
                ["STAT_TOP_PLAYER_WIPE_EXP"] = "Congratulations!\nYou successfully held the {0} position in the previous wipe in the category <color=#4286f4>MOST EXPLOSIONS</color>\nYou received a well deserved Reward!",
                ["STAT_TOP_PLAYER_WIPE_FARM"] = "Congratulations!\nYou successfully held the {0} position in the previous wipe in the category <color=#4286f4>MOST CROPS FARMED</color>\nYou received a well deserved Reward!",
                ["STAT_TOP_PLAYER_WIPE_KILL"] = "Congratulations!\nYou successfully held the {0} position in the previous wipe in the category <color=#4286f4>MOST KILLS</color>\nYou received a well deserved Reward!",
                ["STAT_TOP_PLAYER_WIPE_KILL_NPC"] = "Congratulations!\nYou successfully held the {0} position in the previous wipe in the category <color=#4286f4>NPC Killer</color>\nYou received a well deserved Reward!",
                ["STAT_TOP_PLAYER_WIPE_KILL_ANIMAL"] = "Congratulations!\nYou successfully held the {0} position in the previous wipe in the category <color=#4286f4>ANIMAL Killer</color>\nYou received a well deserved Reward!",
                ["STAT_TOP_VK_SCORE"] = "Топ {0} игрока по очкам\n {1}",
                ["STAT_TOP_VK_KILLER"] = "Топ {0} игрока по убийствам\n {1}",
                ["STAT_TOP_VK_TIME"] = "Топ {0} игрока по онлайну\n {1}",
                ["STAT_TOP_VK_FARM"] = "Топ {0} игрока по фарму\n {1}",
                ["STAT_TOP_VK_RAID"] = "Топ {0} игрока по рейдам\n {1}",
                ["STAT_TOP_VK_KILLER_NPC"] = "Топ {0} игрока по убийствам NPC\n {1}",
                ["STAT_TOP_VK_KILLER_ANIMAL"] = "Топ {0} игрока по убийствам животных\n {1}",

            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["STAT_TOP_FIVE_KILL"] = "Топ 5 <color=#4286f4>киллеры</color>\n{0}</size>",
                ["STAT_TOP_FIVE_KILL_NPC"] = "Топ 5 <color=#4286f4>убийцы NPC</color>\n{0}</size>",
                ["STAT_TOP_FIVE_FARM"] = "Топ 5 <color=#4286f4>фармеры</color>\n{0}</size>",
                ["STAT_TOP_FIVE_EXPLOSION"] = "Топ 5 <color=#4286f4>рейдеры</color>\n{0}</size>",
                ["STAT_TOP_FIVE_TIMEPLAYED"] = "Топ 5 <color=#4286f4>долгожителей</color>\n{0}</size>",
                ["STAT_TOP_FIVE_BUILDINGS"] = "Топ 5 <color=#4286f4>строители</color>\n{0}</size>",
                ["STAT_TOP_FIVE_SCORE"] = "Топ 5 по <color=#4286f4>очкам</color>\n{0}</size>",
                ["STAT_TOP_FIVE_FERMER"] = "Топ 5 <color=#4286f4>фермеров</color>\n{0}</size>",
                ["STAT_USER_TOTAL_GATHERED"] = "Всего добыто:",
                ["STAT_USER_TOTAL_EXPLODED"] = "Всего взорвано:",
                ["STAT_USER_TOTAL_GROWED"] = "Всего выращено:",
                ["STAT_UI_MY_STAT"] = "Моя Статистика",
                ["STAT_UI_TOP_TEN"] = "Топ 10 игроков",
                ["STAT_UI_SEARCH"] = "Поиск",
                ["STAT_UI_INFO"] = "Информация о профиле {0}",
                ["STAT_UI_ACTIVITY"] = "Активность",
                ["STAT_UI_ACTIVITY_TODAY"] = "Сегодня: {0}",
                ["STAT_UI_ACTIVITY_TOTAL"] = "За все время: {0}",
                ["STAT_UI_SETTINGS"] = "Настройки",
                ["STAT_UI_PLACE_TOP"] = "Место в топе: {0}",
                ["STAT_UI_SCORE"] = "SCORE: {0}",
                ["STAT_UI_PVP"] = "PVP статистика",
                ["STAT_UI_FAVORITE_WEAPON"] = "Фаворитное оружие",
                ["STAT_UI_PVP_KILLS"] = "Убийств",
                ["STAT_UI_PVP_KILLS_NPC"] = "Убийств NPC",
                ["STAT_UI_PVP_DEATH"] = "Смертей",
                ["STAT_UI_PVP_KDR"] = "K/D",
                ["STAT_UI_FAVORITE_WEAPON_KILLS"] = "Убийств: {0}\nПопаданий: {1}",
                ["STAT_UI_FAVORITE_WEAPON_NOT_DATA"] = "Данные еще собираются...",
                ["STAT_UI_OTHER_STAT"] = "Другая статистика",
                ["STAT_UI_HIDE_STAT"] = "Общедоступный профиль",
                ["STAT_UI_CONFIRM"] = "Вы уверены ?",
                ["STAT_UI_CONFIRM_YES"] = "Да",
                ["STAT_UI_CONFIRM_NO"] = "Нет",
                ["STAT_UI_RESET_STAT"] = "Обнулить статистику",
                ["STAT_UI_CRATE_OPEN"] = "Открыто ящиков: {0}",
                ["STAT_UI_BARREL_KILL"] = "Разбито бочек: {0}",
                ["STAT_UI_ANIMAL_KILL"] = "Убито животных: {0}",
                ["STAT_UI_HELI_KILL"] = "Сбито вертолетов: {0}",
                ["STAT_UI_BRADLEY_KILL"] = "Танков уничтожено: {0}",
                ["STAT_UI_NPC_KILL"] = "Убито NPC: {0}",
                ["STAT_UI_BTN_MORE"] = "Показать еще",
                ["STAT_UI_CATEGORY_GATHER"] = "Добыча",
                ["STAT_UI_CATEGORY_EXPLOSED"] = "Взрывчатка",
                ["STAT_UI_CATEGORY_PLANT"] = "Фермерство",
                ["STAT_UI_CATEGORY_TOP_KILLER"] = "Топ 10 киллеров",
                ["STAT_UI_CATEGORY_TOP_KILLER_ANIMALS"] = "Топ 10 убийц животных",
                ["STAT_UI_CATEGORY_TOP_NPCKILLER"] = "Топ 10 убийц npc",
                ["STAT_UI_CATEGORY_TOP_TIME"] = "Топ 10 по онлайну",
                ["STAT_UI_CATEGORY_TOP_GATHER"] = "Топ 10 фармил",
                ["STAT_UI_CATEGORY_TOP_SCORE"] = "Топ 10 по очкам",
                ["STAT_UI_CATEGORY_TOP_EXPLOSED"] = "Топ 10 рейдеров",
                ["STAT_PRINT_WIPE"] = "Произошел вайп. Данные успешно удалены!",
                ["STAT_CMD_1"] = "Недостаточно прав!",
                ["STAT_CMD_2"] = "Используйте: stat.ignore <add/remove> <Steam ID|Имя>",
                ["STAT_CMD_3"] = "Указанный игрок не найден. Для более точного поиска укажите его SteamID.",
                ["STAT_CMD_4"] = "Найдено несколько игроков с похожим именем: {0}",
                ["STAT_CMD_5"] = "Игрок не найден!",
                ["STAT_CMD_6"] = "Игрок {0} уже игнорируется",
                ["STAT_CMD_7"] = "Вы успешно добавили игрока {0} в игнор лист",
                ["STAT_CMD_8"] = "Игрока {0} нет в списке игнорируемых",
                ["STAT_CMD_9"] = "Вы успешно убрали игрока {0} из игнор листа",
                ["STAT_CMD_10"] = "Игроку {0} успешно зачислено {1} очков",
                ["STAT_CMD_11"] = "Игроку {0} успешно снято {1} очков",
                ["STAT_ADMIN_HIDE_STAT"] = "Вы добавлены в игнор лист. У вас нет доступа к статистики, если это ошибка, свяжитесь с администратором!",
                ["STAT_TOP_PLAYER_WIPE_SCORE"] = "Поздравляю!\nВ прошлом вайпе вы успешно удерживали {0} позицию в категории <color=#4286f4>SCORE</color>\nВы заслужено получаете награду!",
                ["STAT_TOP_PLAYER_WIPE_TIME"] = "Поздравляю!\nВ прошлом вайпе вы успешно удерживали {0} позицию в категории <color=#4286f4>Долгожитель</color>\nВы заслужено получаете награду!",
                ["STAT_TOP_PLAYER_WIPE_EXP"] = "Поздравляю!\nВ прошлом вайпе вы успешно удерживали {0} позицию в категории <color=#4286f4>Рейдер</color>\nВы заслужено получаете награду!",
                ["STAT_TOP_PLAYER_WIPE_FARM"] = "Поздравляю!\nВ прошлом вайпе вы успешно удерживали {0} позицию в категории <color=#4286f4>Добытчик</color>\nВы заслужено получаете награду!",
                ["STAT_TOP_PLAYER_WIPE_KILL"] = "Поздравляю!\nВ прошлом вайпе вы успешно удерживали {0} позицию в категории <color=#4286f4>Киллер</color>\nВы заслужено получаете награду!",
                ["STAT_TOP_PLAYER_WIPE_KILL_NPC"] = "Поздравляю!\nВ прошлом вайпе вы успешно удерживали {0} позицию в категории <color=#4286f4>Убийца нпс</color>\nВы заслужено получаете награду!",
                ["STAT_TOP_PLAYER_WIPE_KILL_ANIMAL"] = "Поздравляю!\nВ прошлом вайпе вы успешно удерживали {0} позицию в категории <color=#4286f4>Убийца животных</color>\nВы заслужено получаете награду!",
                ["STAT_TOP_VK_SCORE"] = "Топ {0} игрока по очкам\n {1}",
                ["STAT_TOP_VK_KILLER"] = "Топ {0} игрока по убийствам\n {1}",
                ["STAT_TOP_VK_TIME"] = "Топ {0} игрока по онлайну\n {1}",
                ["STAT_TOP_VK_FARM"] = "Топ {0} игрока по фарму\n {1}",
                ["STAT_TOP_VK_RAID"] = "Топ {0} игрока по рейдам\n {1}",
                ["STAT_TOP_VK_KILLER_NPC"] = "Топ {0} игрока по убийствам NPC\n {1}",
                ["STAT_TOP_VK_KILLER_ANIMAL"] = "Топ {0} игрока по убийствам животных\n {1}",

            }, this, "ru");
        }
        public static StringBuilder sb;
        private enum CatType
        {
            score,
            killer,
            time,
            farm,
            raid,
            killerNPC,
            killerAnimal
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (!IgnoreReservedPlayer.ContainsKey(player.userID))
            {
                var dataPlayer = PlayerInfo.Find(player.userID);
                dataPlayer.Name = CleanString(player.displayName);
                if (dataPlayer.playedTime.DayNumber != DateTime.Now.ToShortDateString())
                {
                    dataPlayer.playedTime.PlayedToday = 0;
                    dataPlayer.playedTime.DayNumber = DateTime.Now.ToShortDateString();
                }
            }
            if (PrizePlayerData.ContainsKey(player.userID))
                SendMsgRewardWipe(player);
            SteamAvatarAdd(player.UserIDString);
        }

        private void CloseLayer(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, UI_USER_STAT);
            CuiHelper.DestroyUi(player, UI_SEARCH_USER);
            CuiHelper.DestroyUi(player, UI_TOP_TEN_USER);
        }
        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            try
            {
                if (entity == null || info == null)
                    return;
                BasePlayer player = null;
                if (entity is BaseHelicopter)
                {
                    uint id = entity.net.ID;
                    if (!_heliattacker.ContainsKey(id))
                        return;
                    player = BasePlayer.FindByID(_heliattacker[id]);
                    if (player == null)
                        return;
                    PlayerInfo Playerstat = PlayerInfo.Find(player.userID);
                    Playerstat.pVP.HeliKill++;
                    Playerstat.Score += _config.settingsScore.HeliScore;
                    _heliattacker.Remove(id);
                    return;
                }
                if (info.InitiatorPlayer != null)
                    player = info.InitiatorPlayer;
                if (player == null || !player.userID.IsSteamId())
                    return;
                PlayerInfo Playerstat2 = PlayerInfo.Find(player.userID);

                if (entity is ScientistNPC)
                {
                    Playerstat2.pVP.KillsNpc++;
                    Playerstat2.Score += _config.settingsScore.NpcScore;
                    if (_config.settings.npsDeathUse)
                        WeaponProgressAdd(Playerstat2, info, true);
                    return;
                }

                if (entity is BaseAnimalNPC)
                {
                    Playerstat2.otherStat.AnimalsKill++;
                    Playerstat2.Score += _config.settingsScore.AnimalScore;
                    return;
                }

                if (entity is BradleyAPC)
                {
                    Playerstat2.pVP.BradleyKill++;
                    Playerstat2.Score += _config.settingsScore.BradleyScore;
                    return;
                }
            }
            catch (NullReferenceException ex)
            {
            }
        }

        private void OnDispenserBonus(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity.ToPlayer();
            if (player == null)
                return;
            ProgressAdd(player, item.info.shortname, item.amount, true);
        }


        public static string CleanString(string str, string sub = "")
        {
            if (str == null) return null;

            StringBuilder sb = null;
            for (int i = 0; i < str.Length; i++)
            {
                char ch = str[i];
                if (char.IsSurrogate(ch))
                {
                    if (sb == null)
                        sb = new StringBuilder(str, 0, i, str.Length);
                    sb.Append(sub);
                    if (i + 1 < str.Length && char.IsHighSurrogate(ch) && char.IsLowSurrogate(str[i + 1]))
                        i++;
                }
                else if (sb != null)
                    sb.Append(ch);
            }
            return sb == null ? str : sb.ToString();
        }

        private void DiscordPrintTopFive()
        {
            var data = GetRandomTopPlayer();
            SendDiscordMessage(Regex.Replace(lang.GetMessage(data.Item1, this), "<.*?>|{.*?}", string.Empty), new List<string> { data.Item2 }, false);
            timer.Once(_config.discordMessage.discordSendTopTime, DiscordPrintTopFive);
        }
        private List<int> AlowedSeedId = new List<int>() { 1548091822, 1771755747, 1112162468, 1367190888, 858486327, -1962971928, -2086926071, -567909622, 1272194103, 854447607, 1660145984, 1783512007, -858312878 };

        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity.ToPlayer();
            if (player == null)
                return;
            ProgressAdd(player, item.info.shortname, item.amount);
        }
        private bool IsDuel(ulong userID)
        {
            if (Battles)
                return (bool)Battles?.Call("IsPlayerOnBattle", userID);
            else if (Duel)
                return (bool)Duel?.Call("IsPlayerOnActiveDuel", BasePlayer.FindByID(userID));
            else
                return false;
        }



        private void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            PlayerInfo Player = PlayerInfo.Find(task.owner.userID);
            Player.otherStat.AllCraft += item.amount;
            Player.Score += _config.settingsScore.craftScore;
        }
        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject("XDStatistics/StatsUser", Players);
        private readonly Regex _avatarRegex = new Regex(@"<avatarFull><!\[CDATA\[(.*)\]\]></avatarFull>", RegexOptions.Compiled);
        Int32 GetTopScore(ulong userid)
        {
            Int32 Top = 1;
            var RaitingNumber = Players.Where(x => !_config.settings.ignoreList.Contains(x.Key)).OrderByDescending(x => x.Value.Score);

            foreach (var Data in RaitingNumber)
            {
                if (Data.Key == userid)
                    break;
                Top++;
            }
            return Top;
        }

        public const string UI_INTERFACE = "INTERFACE_STATS";
        void OnItemPickup(Item item, BasePlayer player)
        {
            if (item == null || item.skin != 2352567)
                return;
            item.skin = 0;
            if (item.info.shortname == "scrap")
                ProgressAdd(player, item.info.shortname, item.amount);
        }

        private IEnumerator ParseTopUserForPrize()
        {
            PrizePlayerData.Clear();

            List<CatType> catTypes = new List<CatType> { CatType.score, CatType.killer, CatType.time, CatType.farm, CatType.raid, CatType.killerNPC, CatType.killerAnimal };
            foreach (var cat in catTypes)
            {
                switch (cat)
                {
                    case CatType.score:
                        {
                            int countReward = _config.settingsPrize.prizeScore.Count;
                            if (countReward != 0)
                            {
                                var score = Players.Where(x => !_config.settings.ignoreList.Contains(x.Key)).OrderByDescending(x => x.Value.Score).Take(countReward);
                                int top = 1;
                                foreach (var item in score)
                                {
                                    if (!PrizePlayerData.ContainsKey(item.Key))
                                    {
                                        PrizePlayerData.Add(item.Key, new List<PrizePlayer>());
                                    }
                                    var player = PrizePlayerData[item.Key];
                                    player.Add(new PrizePlayer { catType = cat, Name = item.Value.Name, value = (int)item.Value.Score, top = top });
                                    _config.settingsPrize.prizeScore.Find(x => x.top == top)?.GiftPrizePlayer(item.Key.ToString());
                                    top++;
                                    yield return CoroutineEx.waitForSeconds(0.02f);
                                }
                            }
                            break;
                        }
                    case CatType.killer:
                        {
                            int countReward = _config.settingsPrize.prizeKiller.Count;
                            if (countReward != 0)
                            {
                                var killer = Players.Where(x => !_config.settings.ignoreList.Contains(x.Key)).OrderByDescending(x => x.Value.pVP.Kills).Take(countReward);
                                int top = 1;
                                foreach (var item in killer)
                                {
                                    if (!PrizePlayerData.ContainsKey(item.Key))
                                    {
                                        PrizePlayerData.Add(item.Key, new List<PrizePlayer>());
                                    }
                                    var player = PrizePlayerData[item.Key];
                                    player.Add(new PrizePlayer { catType = cat, Name = item.Value.Name, value = (int)item.Value.pVP.Kills, top = top });
                                    _config.settingsPrize.prizeKiller.Find(x => x.top == top)?.GiftPrizePlayer(item.Key.ToString());
                                    top++;
                                    yield return CoroutineEx.waitForSeconds(0.02f);
                                }
                            }
                            break;
                        }
                    case CatType.time:
                        {
                            int countReward = _config.settingsPrize.prizeTime.Count;
                            if (countReward != 0)
                            {
                                var time = Players.Where(x => !_config.settings.ignoreList.Contains(x.Key)).OrderByDescending(x => x.Value.playedTime.PlayedForWipe).Take(countReward);
                                int top = 1;
                                foreach (var item in time)
                                {
                                    if (!PrizePlayerData.ContainsKey(item.Key))
                                    {
                                        PrizePlayerData.Add(item.Key, new List<PrizePlayer>());
                                    }
                                    var player = PrizePlayerData[item.Key];
                                    player.Add(new PrizePlayer { catType = cat, Name = item.Value.Name, value = (int)item.Value.playedTime.PlayedForWipe, top = top });
                                    _config.settingsPrize.prizeTime.Find(x => x.top == top)?.GiftPrizePlayer(item.Key.ToString());
                                    top++;
                                    yield return CoroutineEx.waitForSeconds(0.02f);
                                }
                            }
                            break;
                        }
                    case CatType.farm:
                        {
                            int countReward = _config.settingsPrize.prizeFarm.Count;
                            if (countReward != 0)
                            {
                                var farm = Players.Where(x => !_config.settings.ignoreList.Contains(x.Key)).OrderByDescending(x => x.Value.gather.AllGathered).Take(countReward);
                                int top = 1;
                                foreach (var item in farm)
                                {
                                    if (!PrizePlayerData.ContainsKey(item.Key))
                                    {
                                        PrizePlayerData.Add(item.Key, new List<PrizePlayer>());
                                    }
                                    var player = PrizePlayerData[item.Key];
                                    player.Add(new PrizePlayer { catType = cat, Name = item.Value.Name, value = item.Value.gather.AllGathered, top = top });
                                    _config.settingsPrize.prizeFarm.Find(x => x.top == top)?.GiftPrizePlayer(item.Key.ToString());
                                    top++;
                                    yield return CoroutineEx.waitForSeconds(0.02f);
                                }
                            }
                            break;
                        }
                    case CatType.raid:
                        {
                            int countReward = _config.settingsPrize.prizeRaid.Count;
                            if (countReward != 0)
                            {
                                var raid = Players.Where(x => !_config.settings.ignoreList.Contains(x.Key)).OrderByDescending(x => x.Value.explosion.AllExplosionUsed).Take(countReward);
                                int top = 1;
                                foreach (var item in raid)
                                {
                                    if (!PrizePlayerData.ContainsKey(item.Key))
                                    {
                                        PrizePlayerData.Add(item.Key, new List<PrizePlayer>());
                                    }
                                    var player = PrizePlayerData[item.Key];
                                    player.Add(new PrizePlayer { catType = cat, Name = item.Value.Name, value = item.Value.explosion.AllExplosionUsed, top = top });
                                    _config.settingsPrize.prizeRaid.Find(x => x.top == top)?.GiftPrizePlayer(item.Key.ToString());
                                    top++;
                                    yield return CoroutineEx.waitForSeconds(0.02f);
                                }
                            }
                            break;
                        }
                    case CatType.killerNPC:
                        {
                            int countReward = _config.settingsPrize.prizeNPCKiller.Count;
                            if (countReward != 0)
                            {
                                var killerNpc = Players.Where(x => !_config.settings.ignoreList.Contains(x.Key)).OrderByDescending(x => x.Value.pVP.KillsNpc).Take(countReward);
                                int top = 1;
                                foreach (var item in killerNpc)
                                {
                                    if (!PrizePlayerData.ContainsKey(item.Key))
                                    {
                                        PrizePlayerData.Add(item.Key, new List<PrizePlayer>());
                                    }
                                    var player = PrizePlayerData[item.Key];
                                    player.Add(new PrizePlayer { catType = cat, Name = item.Value.Name, value = item.Value.pVP.KillsNpc, top = top });
                                    _config.settingsPrize.prizeNPCKiller.Find(x => x.top == top)?.GiftPrizePlayer(item.Key.ToString());
                                    top++;
                                    yield return CoroutineEx.waitForSeconds(0.02f);
                                }
                            }
                            break;
                        }
                    case CatType.killerAnimal:
                        {
                            int countReward = _config.settingsPrize.prizeAnimalKiller.Count;
                            if (countReward != 0)
                            {
                                var killerAnimal = Players.Where(x => !_config.settings.ignoreList.Contains(x.Key)).OrderByDescending(x => x.Value.otherStat.AnimalsKill).Take(countReward);
                                int top = 1;
                                foreach (var item in killerAnimal)
                                {
                                    if (!PrizePlayerData.ContainsKey(item.Key))
                                    {
                                        PrizePlayerData.Add(item.Key, new List<PrizePlayer>());
                                    }
                                    var player = PrizePlayerData[item.Key];
                                    player.Add(new PrizePlayer { catType = cat, Name = item.Value.Name, value = item.Value.otherStat.AnimalsKill, top = top });
                                    _config.settingsPrize.prizeAnimalKiller.Find(x => x.top == top)?.GiftPrizePlayer(item.Key.ToString());
                                    top++;
                                    yield return CoroutineEx.waitForSeconds(0.02f);
                                }
                            }
                            break;
                        }
                    default:
                        break;
                }
            }

            rewardPlayerCoroutine = null;
            SaveDataPrize();
            if (_config.settings.wipeData)
            {
                PlayerInfo.ClearDataWipe();
                NextTick(() =>
                {
                    SaveData();
                    SaveDataIgnoreList();
                });
                PrintWarning(GetLang("STAT_PRINT_WIPE"));
            }
        }
        private void OnPlayerAttack(BasePlayer attacker, HitInfo hitinfo)
        {
            if (hitinfo == null || attacker == null || hitinfo.HitEntity == null || !attacker.IsConnected)
                return;
            if ((hitinfo.HitEntity is ScientistNPC && _config.settings.npsDeathUse) || (hitinfo.HitEntity is BasePlayer && (hitinfo.HitEntity as BasePlayer).userID.IsSteamId()))
            {
                if (hitinfo.HitEntity == attacker)
                    return;
                PlayerInfo PlayerstatInitiator = PlayerInfo.Find(attacker.userID);
                WeaponProgressAdd(PlayerstatInitiator, hitinfo);

                PlayerstatInitiator.pVP.Shots++;
                if (hitinfo.isHeadshot)
                    PlayerstatInitiator.pVP.Headshots++;
            }
        }
        private void OnExplosiveThrown(BasePlayer player, BaseEntity entity, ThrownWeapon item)
        {
            if (player == null || item == null)
                return;
            ExplosionProgressAdd(player, entity);
        }


        private class DiscordMessage
        {
            public DiscordMessage(string content, params Embed[] embeds)
            {
                Content = content;
                Embeds = embeds.ToList();
            }

            [JsonProperty("content")] public string Content { get; set; }
            [JsonProperty("embeds")] public List<Embed> Embeds { get; set; }

            public string ToJson()
            {
                return JsonConvert.SerializeObject(this);
            }
        }


        private void CategoryStatUser(BasePlayer player, ulong target = 0, int cat = 0)
        {
            var container = new CuiElementContainer();
            PlayerInfo statInfo = PlayerInfo.Find(target == 0 ? player.userID : target);
            var list = GetCategory(statInfo, cat);
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.58 0.57 0.61 0.43" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"151.69 {181.04 - ((list.Count - 1) * 50.7)}", OffsetMax = $"153.21 225.49" }
            }, UI_USER_STAT_INFO, "STAT_LINE");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.58 0.57 0.61 1" },
                RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = $"-0.7 {-23.3 - ((list.Count - 1) * 30.7)}", OffsetMax = "0.7 0.17" }
            }, "STAT_LINE", "STAT_LINE_CHILD");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-146.988 -71.4", OffsetMax = "146.388 -53.16" }
            }, UI_USER_STAT_INFO, "MENU_USER_STAT");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "0.312 -9.12", OffsetMax = "84.704 9.12" },
                Button = { Command = $"UI_HandlerStat changeCategory {target} 0", Color = "0 0 0 0" },
                Text = { Text = GetLang("STAT_UI_CATEGORY_GATHER", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, }
            }, "MENU_USER_STAT", "Panel_5655");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = cat == 0 ? "0.2 0.6 0.12 0.64" : "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-42.347 0", OffsetMax = "42.196 1.8" }
            }, "Panel_5655", "Panel_8052");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-61.983 -9.12", OffsetMax = "31.232 9.12" },
                Button = { Command = $"UI_HandlerStat changeCategory {target} 1", Color = "0 0 0 0" },
                Text = { Text = GetLang("STAT_UI_CATEGORY_EXPLOSED", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, }
            }, "MENU_USER_STAT", "Panel_56551");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = cat == 1 ? "0.2 0.6 0.12 0.64" : "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-46.608 0", OffsetMax = "46.608 1.8" }
            }, "Panel_56551", "Panel_8052");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-115.459 -9.12", OffsetMax = "0.001 9.12" },
                Button = { Command = $"UI_HandlerStat changeCategory {target} 2", Color = "0 0 0 0" },
                Text = { Text = GetLang("STAT_UI_CATEGORY_PLANT", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, }
            }, "MENU_USER_STAT", "Panel_56553");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = cat == 2 ? "0.2 0.6 0.12 0.64" : "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-57.73 0", OffsetMax = "57.73 1.8" }
            }, "Panel_56553", "Panel_8052");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-146.991 -124.12", OffsetMax = "146.4 226.03" }
            }, "USER_STAT_INFO", "MAIN_LIST_STAT_USER");

            int y = 0;
            string userLang = lang.GetLanguage(player.UserIDString);
            foreach (var item in list)
            {
                if (_config.settingsScore.blackListed.Contains(item.Key))
                    continue;
                float fade = 0.15f * y;
                string itemName = string.Empty;
                if (_itemName.ContainsKey(item.Key))
                {
                    itemName = userLang == "ru" ? _itemName[item.Key].ru : _itemName[item.Key].en;
                }

                container.Add(new CuiElement
                {
                    Name = "STAT_USER_LINE",
                    Parent = "MAIN_LIST_STAT_USER",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 0.03", Png = item.Key == "all" ? ImageUi.GetImage("10") : ImageUi.GetImage("9"), FadeIn = fade },
                    new CuiRectTransformComponent { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = $"-146.118 {-44.9 - (y * 50.7)}", OffsetMax = $"146.6 {-0.7 - (y * 50.7)}" }
                }
                });
                string name = cat == 0 ? item.Value.ToString("0,0", CultureInfo.InvariantCulture) : item.Value.ToString();

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "26.03 -10.5", OffsetMax = "126.03 12.5" },
                    Text = { Text = name, Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleRight, Color = "0.8 0.8 0.8 1", FadeIn = fade }
                }, "STAT_USER_LINE", "STAT_USER_AMOUNT");

                if (item.Key == "all")
                {
                    string langGet = cat == 0 ? "STAT_USER_TOTAL_GATHERED" : cat == 1 ? "STAT_USER_TOTAL_EXPLODED" : "STAT_USER_TOTAL_GROWED";

                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-128 -10.5", OffsetMax = "50 12.5" },
                        Text = { Text = GetLang(langGet, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1", FadeIn = fade }
                    }, "STAT_USER_LINE", "ALL_TOTAL");
                }
                else
                {
                    container.Add(new CuiElement
                    {
                        Name = "IMAGE_ITEM",
                        Parent = "STAT_USER_LINE",
                        Components = {
                            new CuiRawImageComponent { Color = "1 1 1 1",  Png = GetImage(item.Key + 50), FadeIn = fade },
                            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-128 -17.5", OffsetMax = "-93 17.5" }
                        }
                    });

                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-73.17 -10.5", OffsetMax = "50 12.5" },
                        Text = { Text = itemName, Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "0.8 0.8 0.8 1", FadeIn = fade }
                    }, "STAT_USER_LINE");
                }
                y++;
            }


            CuiHelper.DestroyUi(player, "MENU_USER_STAT");
            CuiHelper.DestroyUi(player, "MAIN_LIST_STAT_USER");
            CuiHelper.DestroyUi(player, "STAT_LINE");
            CuiHelper.AddUi(player, container);
        }

        private void SendDiscordMessage(string title, List<string> embeds, bool inline = false)
        {
            Embed embed = new Embed();
            foreach (var item in embeds)
            {
                embed.AddField(title, item, inline, _config.discordMessage.colorLines.GetRandom());
            }
            webrequest.Enqueue(_config.discordMessage.weebHook, new DiscordMessage(_config.discordMessage.message, embed).ToJson(), (code, response) => { },
                this,
                RequestMethod.POST, new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" }
                });
        }
        private bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);

        private void OnGrowableGathered(GrowableEntity plant, Item item, BasePlayer player)
        {
            if (AlowedSeedId.Contains(item.info.itemid))
            {
                NextTick(() =>
                {
                    PlayerInfo Playerstat = PlayerInfo.Find(player.userID);
                    if (!Playerstat.harvesting.HarvestingList.ContainsKey(item.info.shortname))
                    {
                        Playerstat.harvesting.HarvestingList.Add(item.info.shortname, item.amount);
                    }
                    else
                    {
                        Playerstat.harvesting.HarvestingList[item.info.shortname] += item.amount;
                    }
                    Playerstat.harvesting.AllHarvesting += item.amount;
                    Playerstat.Score += _config.settingsScore.PlantScore;
                });
            }
        }
        private bool HasImage(string imageName, ulong imageId = 0) => (bool)ImageLibrary.Call("HasImage", imageName, imageId);
        private void ProgressAdd(BasePlayer player, string shortname, int count, bool scoreGive = false)
        {
            PlayerInfo Playerstat = PlayerInfo.Find(player.userID);
            if (_config.settingsScore.blackListed.Contains(shortname))
                return;
            switch (shortname)
            {

                case "wood":
                    {
                        Playerstat.gather.GatheredTotal[shortname] += count;
                        Playerstat.gather.AllGathered += count;
                        if (scoreGive)
                            Playerstat.Score += _config.settingsScore.GatherScore[shortname];
                        break;
                    }
                case "stones":
                    {
                        Playerstat.gather.GatheredTotal[shortname] += count;
                        Playerstat.gather.AllGathered += count;
                        if (scoreGive)
                            Playerstat.Score += _config.settingsScore.GatherScore[shortname];
                        break;
                    }
                case "metal.ore":
                case "metal.fragments":
                    {
                        Playerstat.gather.GatheredTotal["metal.ore"] += count;
                        Playerstat.gather.AllGathered += count;
                        if (scoreGive)
                            Playerstat.Score += _config.settingsScore.GatherScore["metal.ore"];
                        break;
                    }
                case "sulfur.ore":
                case "sulfur":
                    {
                        Playerstat.gather.GatheredTotal["sulfur.ore"] += count;
                        Playerstat.gather.AllGathered += count;
                        if (scoreGive)
                            Playerstat.Score += _config.settingsScore.GatherScore["sulfur.ore"];
                        break;
                    }
                case "hq.metal.ore":
                case "metal.refined":
                    {
                        Playerstat.gather.GatheredTotal["hq.metal.ore"] += count;
                        Playerstat.gather.AllGathered += count;
                        if (scoreGive)
                            Playerstat.Score += _config.settingsScore.GatherScore["hq.metal.ore"];
                        break;
                    }
                case "scrap":
                    {
                        Playerstat.gather.GatheredTotal[shortname] += count;
                        Playerstat.gather.AllGathered += count;
                        Playerstat.Score += _config.settingsScore.ScrapScore;
                        break;
                    }
            }
        }

        private class PlayerInfo
        {
            public string Name = string.Empty;
            public bool HidedStatistics = false;
            internal string GetPlayerName(ulong steamId) => string.IsNullOrWhiteSpace(Name) ?
                _.covalence.Players.FindPlayerById(steamId.ToString())?.Name ?? "UNKNOWN" : Name;
            public float Score = 0;
            public Harvesting harvesting = new Harvesting();
            public OtherStat otherStat = new OtherStat();
            public Explosion explosion = new Explosion();
            public Gather gather = new Gather();
            public Weapon weapon = new Weapon();
            public PVP pVP = new PVP();
            public PlayedTime playedTime = new PlayedTime();

            internal class PlayedTime
            {
                public string DayNumber = DateTime.Now.ToShortDateString();
                public int PlayedForWipe = 0;
                public int PlayedToday = 0;
            }
            internal class Harvesting
            {
                public Dictionary<string, int> HarvestingList = new Dictionary<string, int>();
                public int AllHarvesting = 0;
            }
            internal class OtherStat
            {
                public int CrateOpen = 0;
                public int BarrelDeath = 0;
                public int AllCraft = 0;
                public int BuildingCrate = 0;
                public int AnimalsKill = 0;
            }
            internal class Explosion
            {
                public Dictionary<string, int> ExplosionUsed = new Dictionary<string, int>()
                {
                    ["explosive.timed"] = 0,
                    ["explosive.satchel"] = 0,
                    ["grenade.beancan"] = 0,
                    ["grenade.f1"] = 0,
                    ["ammo.rocket.basic"] = 0,
                    ["ammo.rocket.hv"] = 0,
                    ["ammo.rocket.fire"] = 0,
                    ["ammo.rifle.explosive"] = 0
                };
                public int AllExplosionUsed = 0;
            }
            internal class Gather
            {
                public Dictionary<string, int> GatheredTotal = new Dictionary<string, int>()
                {
                    ["wood"] = 0,
                    ["stones"] = 0,
                    ["metal.ore"] = 0,
                    ["sulfur.ore"] = 0,
                    ["hq.metal.ore"] = 0,
                    ["scrap"] = 0,
                };
                public int AllGathered = 0;
            }
            internal class Weapon
            {
                public Dictionary<string, WeaponInfo> WeaponUsed = new Dictionary<string, WeaponInfo>();
                internal class WeaponInfo
                {
                    public int Kills = 0;
                    public int Headshots = 0;
                    public int Shots = 0;
                }
            }
            internal class PVP
            {
                public int Kills = 0;
                public int KillsNpc = 0;
                public int Deaths = 0;
                public int Suicides = 0;
                public int Shots = 0;
                public int Headshots = 0;
                public int HeliKill = 0;
                public int BradleyKill = 0;
            }
            public static void AddPlayedTime(ulong id)
            {
                var player = Players.ContainsKey(id) ? Players[id] : null;
                if (player == null)
                    return;
                player.playedTime.PlayedToday++;
                player.playedTime.PlayedForWipe++;
                player.Score += _._config.settingsScore.TimeScore;

                if (player.playedTime.DayNumber != DateTime.Now.ToShortDateString())
                {
                    player.playedTime.PlayedToday = 0;
                    player.playedTime.DayNumber = DateTime.Now.ToShortDateString();
                }
            }
            public static void PlayerClearData(ulong id)
            {
                BasePlayer player = BasePlayer.FindByID(id);
                Players[id] = new PlayerInfo();
                Players[id].Name = player.displayName;
            }
            public static void ClearDataWipe()
            {
                if (IgnoreReservedPlayer != null)
                {
                    IgnoreReservedPlayer.Clear();
                }
                if (Players != null)
                {
                    Players.Clear();
                }
            }
            public static PlayerInfo Find(ulong id)
            {
                if (Players.ContainsKey(id))
                {
                    return Players[id];
                }

                if (IgnoreReservedPlayer.ContainsKey(id))
                {
                    return IgnoreReservedPlayer[id];
                }

                Players.Add(id, new PlayerInfo());

                if (Players.ContainsKey(id))
                {
                    return Players[id];
                }
                return null;
            }
        }
        public const string UI_USER_STAT_INFO = "USER_STAT_INFO";
        private readonly string permReset = "XDStatistics.reset";

        private void DialogConfirmationDropStat(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiElement
            {
                Name = "CONFIRMATIONS",
                Parent = UI_USER_STAT_INFO,
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("6") },
                    new CuiRectTransformComponent {AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "13.64 -104.387", OffsetMax = "160.12 -45.246" }
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-73.24 -30", OffsetMax = "73.24 -0.43" },
                Text = { Text = GetLang("STAT_UI_CONFIRM", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
            }, "CONFIRMATIONS");

            container.Add(new CuiElement
            {
                Name = "CONFIRMATIONS_YES",
                Parent = "CONFIRMATIONS",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("5")},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "11.28 6.7", OffsetMax = "46.28 23.7" }
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-22.5 -11", OffsetMax = "22.5 11" },
                Button = { Command = "UI_HandlerStat confirm_yes", Color = "0 0 0 0" },
                Text = { Text = GetLang("STAT_UI_CONFIRM_YES", player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" }
            }, "CONFIRMATIONS_YES", "LABEL_YES");

            container.Add(new CuiElement
            {
                Name = "CONFIRMATIONS_NO",
                Parent = "CONFIRMATIONS",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("5")},
                    new CuiRectTransformComponent { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-46.8 6.7", OffsetMax = "-11.8 23.7" }
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-22.5 -11", OffsetMax = "22.5 11" },
                Button = { Close = "CONFIRMATIONS", Color = "0 0 0 0" },
                Text = { Text = GetLang("STAT_UI_CONFIRM_NO", player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" }
            }, "CONFIRMATIONS_NO", "LABEL_NO");

            CuiHelper.DestroyUi(player, "CONFIRMATIONS");
            CuiHelper.AddUi(player, container);
        }
        public const string UI_MENU_BUTTON = "MENU_BUTTON";

        private bool IsClans(string userID, string targetID)
        {
            if (Clans)
            {
                String TagUserID = (String)Clans?.Call("GetClanOf", userID);
                String TagTargetID = (String)Clans?.Call("GetClanOf", targetID);
                if (TagUserID == null && TagTargetID == null)
                    return false;
                return (bool)(TagUserID == TagTargetID);
            }
            else
                return false;
        }

        private static Dictionary<ulong, PlayerInfo> Players = new Dictionary<ulong, PlayerInfo>();

        [ConsoleCommand("UI_HandlerStat")]
        private void CmdConsoleHandler(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            PlayerInfo playerInfo = PlayerInfo.Find(player.userID);
            if (player != null && args.HasArgs(1))
            {
                switch (args.Args[0])
                {
                    case "hidestat":
                        {
                            if (_config.settings.availabilityUse && !permission.UserHasPermission(player.UserIDString, permAvailability))
                                return;
                            if (playerInfo.HidedStatistics)
                            {
                                playerInfo.HidedStatistics = false;
                            }
                            else
                                playerInfo.HidedStatistics = true;
                            ButtonHideStat(player, playerInfo);
                            break;
                        }
                    case "confirm":
                        {
                            DialogConfirmationDropStat(player);
                            break;
                        }
                    case "confirm_yes":
                        {
                            if (_config.settings.dropStatUse && !permission.UserHasPermission(player.UserIDString, permReset))
                                return;
                            PlayerInfo.PlayerClearData(player.userID);
                            UserStat(player);
                            break;
                        }
                    case "changeCategory":
                        {
                            ulong target = ulong.Parse(args.Args[1]);
                            int cat = int.Parse(args.Args[2]);
                            CategoryStatUser(player, target, cat);
                            break;
                        }
                    case "ShowMoreStat":
                        {
                            ulong target = ulong.Parse(args.Args[1]);
                            int cat = int.Parse(args.Args[2]);
                            if (cat == 0)
                                OtherStatUser(player, target, 1);
                            else
                                OtherStatUser(player, target, 0);
                            break;
                        }
                    case "listplayer":
                        {
                            if (args.Args.Length > 1)
                            {
                                string Seaecher = args.Args[1].ToLower();
                                SearchPageUser(player, Seaecher);
                            }
                            else
                                SearchPageUser(player);
                            break;
                        }
                    case "GoStatPlayers":
                        {
                            ulong id = ulong.Parse(args.Args[1]);
                            UserStat(player, id);
                            break;
                        }
                    case "Page_swap":
                        {
                            int cat = int.Parse(args.Args[1]);
                            MenuButton(player, cat);
                            break;
                        }
                }
            }
        }
        private static Dictionary<ulong, List<PrizePlayer>> PrizePlayerData = new Dictionary<ulong, List<PrizePlayer>>();

        private void Init()
        {
            _ = this;
            sb = new StringBuilder();
            LoadData();
            LoadDataIgnoreList();
            LoadDataPrize();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null)
                    LoadDefaultConfig();
                ValidateConfig();
                SaveConfig();
            }
            catch (JsonException ex)
            {
                Debug.LogException(ex);
                LoadDefaultConfig();
            }
        }
        private void OnLootEntity(BasePlayer player, LootContainer entity)
        {
            if (entity == null || player == null || entity.OwnerID.IsSteamId() || entity.net == null)
                return;
            if (!LootersListCarte.ContainsKey(player))
                LootersListCarte.Add(player, new List<UInt64> { });
            UInt64 netId = entity.net.ID;
            if (LootersListCarte[player].Contains(netId))
                return;
            PlayerInfo Playerstat = PlayerInfo.Find(player.userID);
            Playerstat.otherStat.CrateOpen++;
            foreach (var item in entity.inventory.itemList)
            {
                if (item.info.shortname == "scrap")
                    ProgressAdd(player, item.info.shortname, item.amount);
            }
            LootersListCarte[player].Add(netId);
        }

        public class items
        {
            public String shortName;
            public String ENdisplayName;
            public String RUdisplayName;
        }
        [ConsoleCommand("stat.topuser")]
        private void CmdTestVkPlayer(ConsoleSystem.Arg arg)
        {
            if (arg == null)
                return;

            var players = arg.Player();
            if (players != null && !permission.UserHasPermission(players.UserIDString, permAdmin))
            {
                PrintToConsole(players, GetLang("STAT_CMD_1", players.UserIDString));
                return;
            }
            List<CatType> catTypes = new List<CatType> { CatType.score, CatType.killer, CatType.time, CatType.farm, CatType.raid, CatType.killerNPC, CatType.killerAnimal };
            string txt = string.Empty;
            foreach (var cat in catTypes)
            {
                switch (cat)
                {
                    case CatType.score:
                        {
                            int countReward = _config.settingsPrize.prizeScore.Count;
                            if (countReward != 0)
                            {
                                var score = Players.Where(x => !_config.settings.ignoreList.Contains(x.Key)).OrderByDescending(x => x.Value.Score).Take(countReward);
                                int top = 1;
                                string infoUser = string.Empty;
                                foreach (var item in score)
                                {
                                    infoUser += $"{top}) {item.Value.Name} : {item.Value.Score.ToString("0.0")}\n";
                                    top++;
                                }
                                txt += GetLang("STAT_TOP_VK_SCORE", null, countReward, infoUser);
                            }
                            break;
                        }
                    case CatType.killer:
                        {
                            int countReward = _config.settingsPrize.prizeKiller.Count;
                            if (countReward != 0)
                            {
                                var killer = Players.Where(x => !_config.settings.ignoreList.Contains(x.Key)).OrderByDescending(x => x.Value.pVP.Kills).Take(countReward);
                                int top = 1;
                                string infoUser = string.Empty;
                                foreach (var item in killer)
                                {
                                    infoUser += $"{top}) {item.Value.Name} : {item.Value.pVP.Kills}\n";
                                    top++;
                                }
                                txt += GetLang("STAT_TOP_VK_KILLER", null, countReward, infoUser);
                            }
                            break;
                        }
                    case CatType.time:
                        {
                            int countReward = _config.settingsPrize.prizeTime.Count;
                            if (countReward != 0)
                            {
                                var time = Players.Where(x => !_config.settings.ignoreList.Contains(x.Key)).OrderByDescending(x => x.Value.playedTime.PlayedForWipe).Take(countReward);
                                int top = 1;
                                string infoUser = string.Empty;
                                foreach (var item in time)
                                {
                                    infoUser += $"{top}) {item.Value.Name} : {TimeHelper.FormatTime(TimeSpan.FromMinutes(item.Value.playedTime.PlayedForWipe), 5)}\n";
                                    top++;
                                }
                                txt += GetLang("STAT_TOP_VK_TIME", null, countReward, infoUser);
                            }
                            break;
                        }
                    case CatType.farm:
                        {
                            int countReward = _config.settingsPrize.prizeFarm.Count;
                            if (countReward != 0)
                            {
                                var farm = Players.Where(x => !_config.settings.ignoreList.Contains(x.Key)).OrderByDescending(x => x.Value.gather.AllGathered).Take(countReward);
                                int top = 1;
                                string infoUser = string.Empty;
                                foreach (var item in farm)
                                {
                                    infoUser += $"{top}) {item.Value.Name} : {item.Value.gather.AllGathered.ToString("0,0", CultureInfo.InvariantCulture)}\n";
                                    top++;
                                }
                                txt += GetLang("STAT_TOP_VK_FARM", null, countReward, infoUser);
                            }
                            break;
                        }
                    case CatType.raid:
                        {
                            int countReward = _config.settingsPrize.prizeRaid.Count;
                            if (countReward != 0)
                            {
                                var raid = Players.Where(x => !_config.settings.ignoreList.Contains(x.Key)).OrderByDescending(x => x.Value.explosion.AllExplosionUsed).Take(countReward);
                                int top = 1;
                                string infoUser = string.Empty;
                                foreach (var item in raid)
                                {
                                    infoUser += $"{top}) {item.Value.Name} : {item.Value.explosion.AllExplosionUsed}\n";
                                    top++;
                                }
                                txt += GetLang("STAT_TOP_VK_RAID", null, countReward, infoUser);
                            }
                            break;
                        }
                    case CatType.killerNPC:
                        {
                            int countReward = _config.settingsPrize.prizeNPCKiller.Count;
                            if (countReward != 0)
                            {
                                var killerNpc = Players.Where(x => !_config.settings.ignoreList.Contains(x.Key)).OrderByDescending(x => x.Value.pVP.KillsNpc).Take(countReward);
                                int top = 1;
                                string infoUser = string.Empty;
                                foreach (var item in killerNpc)
                                {
                                    infoUser += $"{top}) {item.Value.Name} : {item.Value.pVP.KillsNpc}\n";
                                    top++;
                                }
                                txt += GetLang("STAT_TOP_VK_KILLER_NPC", null, countReward, infoUser);
                            }
                            break;
                        }
                    case CatType.killerAnimal:
                        {
                            int countReward = _config.settingsPrize.prizeAnimalKiller.Count;
                            if (countReward != 0)
                            {
                                var killerNpc = Players.Where(x => !_config.settings.ignoreList.Contains(x.Key)).OrderByDescending(x => x.Value.otherStat.AnimalsKill).Take(countReward);
                                int top = 1;
                                string infoUser = string.Empty;
                                foreach (var item in killerNpc)
                                {
                                    infoUser += $"{top}) {item.Value.Name} : {item.Value.otherStat.AnimalsKill}\n";
                                    top++;
                                }
                                txt += GetLang("STAT_TOP_VK_KILLER_ANIMAL", null, countReward, infoUser);
                            }
                            break;
                        }
                    default:
                        break;
                }
            }

            PrintWarning(txt);
        }
        private int? API_GetGathered(ulong id, string shortname)
        {
            Dictionary<string, int> data = API_GetGathered(id);
            int amount = 0;
            if (data?.TryGetValue(shortname, out amount) == true)
                return amount;
            return null;
        }


        private void SendMsgRewardWipe(BasePlayer player)
        {
            var playerGrant = PrizePlayerData[player.userID];

            foreach (var item in playerGrant)
            {
                switch (item.catType)
                {
                    case CatType.score:
                        player.ChatMessage(GetLang("STAT_TOP_PLAYER_WIPE_SCORE", player.UserIDString, item.top));
                        break;
                    case CatType.killer:
                        player.ChatMessage(GetLang("STAT_TOP_PLAYER_WIPE_KILL", player.UserIDString, item.top));
                        break;
                    case CatType.time:
                        player.ChatMessage(GetLang("STAT_TOP_PLAYER_WIPE_TIME", player.UserIDString, item.top));
                        break;
                    case CatType.farm:
                        player.ChatMessage(GetLang("STAT_TOP_PLAYER_WIPE_FARM", player.UserIDString, item.top));
                        break;
                    case CatType.raid:
                        player.ChatMessage(GetLang("STAT_TOP_PLAYER_WIPE_EXP", player.UserIDString, item.top));
                        break;
                    case CatType.killerNPC:
                        player.ChatMessage(GetLang("STAT_TOP_PLAYER_WIPE_KILL_NPC", player.UserIDString, item.top));
                        break;
                    case CatType.killerAnimal:
                        player.ChatMessage(GetLang("STAT_TOP_PLAYER_WIPE_KILL_ANIMAL", player.UserIDString, item.top));
                        break;
                }
            }
            NextTick(() => { PrizePlayerData.Remove(player.userID); SaveDataPrize(); });
        }
        private void ButtonHideStat(BasePlayer player, PlayerInfo info)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "13.8 -8.6", OffsetMax = "160.34 6.2" }
            }, UI_USER_STAT_INFO, "BUTTON_HIDE_STAT");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-120.7 -7.4", OffsetMax = "21.525 9.8" },
                Text = { Text = GetLang("STAT_UI_HIDE_STAT", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "0.8 0.8 0.8 1" }
            }, "BUTTON_HIDE_STAT", "LABEL_HIDE_USER");


            if (!info.HidedStatistics)
            {
                container.Add(new CuiElement
                {
                    Name = "CHECK_BOX_HIDE",
                    Parent = "BUTTON_HIDE_STAT",
                    Components = {
                        new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("4")},
                        new CuiRectTransformComponent { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "1.381 -6.4", OffsetMax = "14.381 6.6" }
                    }
                });
            }
            else
            {
                container.Add(new CuiElement
                {
                    Name = "CHECK_BOX_HIDE",
                    Parent = "BUTTON_HIDE_STAT",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("3")},
                    new CuiRectTransformComponent { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "1.381 -6.4", OffsetMax = "14.381 6.6" }
                }
                });
            }
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = "UI_HandlerStat hidestat", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, "BUTTON_HIDE_STAT");

            CuiHelper.DestroyUi(player, "BUTTON_HIDE_STAT");
            CuiHelper.AddUi(player, container);
        }

        private void ChatPrintTopFive()
        {
            var data = GetRandomTopPlayer();

            foreach (BasePlayer item in BasePlayer.activePlayerList)
            {
                item.ChatMessage(GetLang(data.Item1, item.UserIDString, data.Item2));
            }
            timer.Once(_config.settings.chatSendTopTime, ChatPrintTopFive);
        }
        public const string UI_USER_STAT = "USER_STAT";
        private readonly string permAdmin = "XDStatistics.admin";
        private void SaveDataPrize() => Interface.Oxide.DataFileSystem.WriteObject("XDStatistics/PlayersReward", PrizePlayerData);
        private void OnServerInitialized()
        {
            if (!ImageLibrary)
            {
                NextTick(() =>
                {
                    PrintError($"ERROR! Plugin ImageLibrary not found!");
                    Interface.Oxide.UnloadPlugin(Name);
                });
                return;
            }
            foreach (var itemDef in ItemManager.GetItemDefinitions())
            {
                Item newItem = ItemManager.CreateByName(itemDef.shortname, 1, 0);

                BaseEntity heldEntity = newItem.GetHeldEntity();
                if (heldEntity != null)
                {
                    _prefabID2Item[heldEntity.prefabID] = itemDef.shortname;
                }

                var deployablePrefab = itemDef.GetComponent<ItemModDeployable>()?.entityPrefab?.resourcePath;
                if (string.IsNullOrEmpty(deployablePrefab))
                {
                    continue;
                }

                var shortPrefabName = GameManager.server.FindPrefab(deployablePrefab)?.GetComponent<BaseEntity>()?.ShortPrefabName;
                if (!string.IsNullOrEmpty(shortPrefabName) && !_prefabNameItem.ContainsKey(shortPrefabName))
                {
                    _prefabNameItem.Add(shortPrefabName, itemDef.shortname);
                }
            }
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);

            timer.Once(60f, CheckInMinute);

            if (_config.settings.chatSendTop)
                timer.Once(_config.settings.chatSendTopTime, ChatPrintTopFive);
            if (_config.discordMessage.discordTopFiveUse && !string.IsNullOrEmpty(_config.discordMessage.weebHook))
                timer.Once(_config.discordMessage.discordSendTopTime, DiscordPrintTopFive);


            foreach (var item in ItemManager.itemDictionary)
            {
                if (item.Value.category == ItemCategory.Weapon || item.Value.category == ItemCategory.Resources || item.Value.category == ItemCategory.Food || item.Value.category == ItemCategory.Ammunition || item.Value.category == ItemCategory.Tool)
                    if (HasImage(item.Value.shortname + 50) == false)
                        AddImage($"https://api.skyplugins.ru/api/getimage/{item.Value.shortname}/50", item.Value.shortname + 50);
            }
            if (_config.settingsInterface.UsebackgroundImageUrl)
                AddImage(_config.settingsInterface.backgroundImageUrl, "CustomBackgroundImage");

            ImageUi.DownloadImages();
            AddDisplayName();
            foreach (var cmds in _config.settings.chatCommandOpenStat)
                cmd.AddChatCommand(cmds, this, nameof(MainMenuStat));
            cmd.AddConsoleCommand(_config.settings.consoleCommandOpenStat, this, nameof(ConsoleCommandOpenMenu));

            permission.RegisterPermission(permAdmin, this);
            permission.RegisterPermission(permAvailability, this);
            permission.RegisterPermission(permReset, this);
            timer.Every(_config.settings.dataSaveTime * 60, () => { SaveData(); SaveDataIgnoreList(); SaveDataPrize(); });
        }

        protected override void LoadDefaultConfig()
        {
            _config = new Configuration();
        }
        private void SteamAvatarAdd(string userid)
        {
            if (ImageLibrary == null)
                return;
            if (HasImage(userid))
                return;
            webrequest.Enqueue($"https://steamcommunity.com/profiles/{userid}?xml=1", null,
                (code, response) =>
                {
                    if (response == null || code != 200)
                        return;

                    string avatarUrl = _avatarRegex.Match(response).Groups[1].ToString();
                    if (!string.IsNullOrEmpty(avatarUrl))
                    {
                        AddImage(avatarUrl, userid);
                    }
                }, this);
        }
        private Dictionary<string, int> GetCategory(PlayerInfo statInfo, int cat)
        {
            Dictionary<string, int> result = new Dictionary<string, int>();
            switch (cat)
            {
                case 0:
                    {
                        foreach (var item in statInfo.gather.GatheredTotal.Take(8))
                            result.Add(item.Key, item.Value);

                        result.Add("all", statInfo.gather.AllGathered);
                        return result;
                    }
                case 1:
                    {
                        foreach (var item in statInfo.explosion.ExplosionUsed.Take(8))
                            result.Add(item.Key, item.Value);

                        result.Add("all", statInfo.explosion.AllExplosionUsed);
                        return result;
                    }
                case 2:
                    {
                        foreach (var item in statInfo.harvesting.HarvestingList.OrderByDescending(x => x.Value).Take(9))
                            result.Add(item.Key, item.Value);

                        result.Add("all", statInfo.harvesting.AllHarvesting);
                        return result;
                    }
                default:
                    break;
            }
            return null;
        }

        private void ValidateConfig()
        {
            if (_config.settingsScore.GatherScore.Count == 0)
            {
                _config.settingsScore.GatherScore = new Dictionary<string, float>
                {
                    ["wood"] = 0.3f,
                    ["stones"] = 0.6f,
                    ["metal.ore"] = 1,
                    ["sulfur.ore"] = 1.5f,
                    ["hq.metal.ore"] = 2,
                };
            }
            if (_config.settingsScore.ExplosionScore.Count == 0)
            {
                _config.settingsScore.ExplosionScore = new Dictionary<string, float>
                {
                    ["explosive.timed"] = 2,
                    ["explosive.satchel"] = 0.7f,
                    ["grenade.beancan"] = 0.3f,
                    ["grenade.f1"] = 0.1f,
                    ["ammo.rocket.basic"] = 1,
                    ["ammo.rocket.hv"] = 0.5f,
                    ["ammo.rocket.fire"] = 0.7f,
                    ["ammo.rifle.explosive"] = 0.02f,
                };
            }
            if (!_config.settingsScore.ExplosionScore.ContainsKey("ammo.rifle.explosive"))
            {
                _config.settingsScore.ExplosionScore.Add("ammo.rifle.explosive", 0.02f);
            }

            if (_config.settingsPrize.prizeFarm.Count == 0)
            {
                _config.settingsPrize.prizeFarm = new List<Configuration.SettingsPrize.Prize>
                {
                    new Configuration.SettingsPrize.Prize{commandPrizeList = new List<string>{ "say %STEAMID%" }, balancePlusMess = "За топ 1 в категории фармер" },
                    new Configuration.SettingsPrize.Prize{ top = 2, commandPrizeList = new List<string>{ "say %STEAMID%" }, balancePlusMess = "За топ 2 в категории фармер" },
                };
            }
            if (_config.settingsPrize.prizeKiller.Count == 0)
            {
                _config.settingsPrize.prizeKiller = new List<Configuration.SettingsPrize.Prize>
                {
                    new Configuration.SettingsPrize.Prize{commandPrizeList = new List<string>{ "say %STEAMID%" }, balancePlusMess = "За топ 1 в категории киллер" },
                    new Configuration.SettingsPrize.Prize{ top = 2, commandPrizeList = new List<string>{ "say %STEAMID%" }, balancePlusMess = "За топ 2 в категории киллер" },
                };
            }
            if (_config.settingsPrize.prizeRaid.Count == 0)
            {
                _config.settingsPrize.prizeRaid = new List<Configuration.SettingsPrize.Prize>
                {
                    new Configuration.SettingsPrize.Prize{commandPrizeList = new List<string>{ "say %STEAMID%" }, balancePlusMess = "За топ 1 в категории рейдер" },
                    new Configuration.SettingsPrize.Prize{ top = 2, commandPrizeList = new List<string>{ "say %STEAMID%" }, balancePlusMess = "За топ 2 в категории рейдер" },
                };
            }
            if (_config.settingsPrize.prizeScore.Count == 0)
            {
                _config.settingsPrize.prizeScore = new List<Configuration.SettingsPrize.Prize>
                {
                    new Configuration.SettingsPrize.Prize{commandPrizeList = new List<string>{ "say %STEAMID%" }, balancePlusMess = "За топ 1 в категории больше всего очков" },
                    new Configuration.SettingsPrize.Prize{ top = 2, commandPrizeList = new List<string>{ "say %STEAMID%" }, balancePlusMess = "За топ 2 в категории больше всего очков"  },
                };
            }
            if (_config.settingsPrize.prizeTime.Count == 0)
            {
                _config.settingsPrize.prizeTime = new List<Configuration.SettingsPrize.Prize>
                {
                    new Configuration.SettingsPrize.Prize{commandPrizeList = new List<string>{ "say %STEAMID%" }, balancePlusMess = "За топ 1 в категории большой онлайн"  },
                    new Configuration.SettingsPrize.Prize{ top = 2, commandPrizeList = new List<string>{ "say %STEAMID%" }, balancePlusMess = "За топ 2 в категории большой онлайн" },
                };
            }
            if (_config.settingsPrize.prizeNPCKiller.Count == 0)
            {
                _config.settingsPrize.prizeNPCKiller = new List<Configuration.SettingsPrize.Prize>
                {
                    new Configuration.SettingsPrize.Prize{commandPrizeList = new List<string>{ "say %STEAMID%" }, balancePlusMess = "За топ 1 в категории убийца NPC" },
                    new Configuration.SettingsPrize.Prize{ top = 2, commandPrizeList = new List<string>{ "say %STEAMID%" }, balancePlusMess = "За топ 2 в категории убийца NPC" },
                };
            }
            if (_config.settingsPrize.prizeAnimalKiller.Count == 0)
            {
                _config.settingsPrize.prizeAnimalKiller = new List<Configuration.SettingsPrize.Prize>
                {
                    new Configuration.SettingsPrize.Prize{commandPrizeList = new List<string>{ "say %STEAMID%" }, balancePlusMess = "За топ 1 в категории убийца NPC" },
                    new Configuration.SettingsPrize.Prize{ top = 2, commandPrizeList = new List<string>{ "say %STEAMID%" }, balancePlusMess = "За топ 2 в категории убийца NPC" },
                };
            }
        }
        private Dictionary<string, ItemDisplayName> _itemName = new Dictionary<string, ItemDisplayName>();
        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null)
                return;
            if (entity is BaseHelicopter && info.Initiator is BasePlayer)
            {
                BasePlayer player = info.InitiatorPlayer;
                if (player == null)
                    return;
                _heliattacker[entity.net.ID] = player.userID;
            }
        }

        private void ConsoleCommandOpenMenu(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
                return;
            if (IgnoreReservedPlayer.ContainsKey(arg.Player().userID))
            {
                PrintToChat(arg.Player(), GetLang("STAT_ADMIN_HIDE_STAT", arg.Player().UserIDString));
                return;
            }
            MainMenuStat(arg.Player());
        }
        private int? API_GetAllGathered(ulong id) => PlayerInfo.Find(id)?.gather.AllGathered;
        public static class TimeHelper
        {
            public static string FormatTime(TimeSpan time, int maxSubstr = 5, string language = "ru")
            {
                string result = string.Empty;
                switch (language)
                {
                    case "ru":
                        int i = 0;
                        if (time.Days != 0 && i < maxSubstr)
                        {
                            if (!string.IsNullOrEmpty(result))
                                result += " ";

                            result += $"{Format(time.Days, "д", "д", "д")}";
                            i++;
                        }
                        if (time.Hours != 0 && i < maxSubstr)
                        {
                            if (!string.IsNullOrEmpty(result))
                                result += " ";

                            result += $"{Format(time.Hours, "ч", "ч", "ч")}";
                            i++;
                        }
                        if (time.Minutes != 0 && i < maxSubstr)
                        {
                            if (!string.IsNullOrEmpty(result))
                                result += " ";

                            result += $"{Format(time.Minutes, "м", "м", "м")}";
                            i++;
                        }
                        if (time.Days == 0)
                        {
                            if (time.Seconds != 0 && i < maxSubstr)
                            {
                                if (!string.IsNullOrEmpty(result))
                                    result += " ";

                                result += $"{Format(time.Seconds, "с", "с", "с")}";
                                i++;
                            }
                        }
                        if (string.IsNullOrEmpty(result))
                            result = "0 секунд";
                        break;
                    default:
                        result = string.Format("{0}{1}{2}{3}",
                            time.Duration().Days > 0
                                ? $"{time.Days:0} day{(time.Days == 1 ? String.Empty : "s")}, "
                                : string.Empty,
                            time.Duration().Hours > 0
                                ? $"{time.Hours:0} hour{(time.Hours == 1 ? String.Empty : "s")}, "
                                : string.Empty,
                            time.Duration().Minutes > 0
                                ? $"{time.Minutes:0} minute{(time.Minutes == 1 ? String.Empty : "s")}, "
                                : string.Empty,
                            time.Duration().Seconds > 0
                                ? $"{time.Seconds:0} second{(time.Seconds == 1 ? String.Empty : "s")}"
                                : string.Empty);

                        if (result.EndsWith(", "))
                            result = result.Substring(0, result.Length - 2);

                        if (string.IsNullOrEmpty(result))
                            result = "0 seconds";
                        break;
                }
                return result;
            }

            private static string Format(int units, string form1, string form2, string form3)
            {
                var tmp = units % 10;

                if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                    return $"{units}{form1}";

                if (tmp >= 2 && tmp <= 4)
                    return $"{units}{form2}";

                return $"{units}{form3}";
            }
        }

        private void OnContainerDropItems(ItemContainer container)
        {
            if (container == null)
                return;
            BaseEntity entity = container.entityOwner;
            if (entity == null)
                return;
            if (!entity.ShortPrefabName.Contains("barrel"))
                return;
            foreach (Item lootitem in container.itemList)
            {
                if (lootitem.info.shortname == "scrap")
                    lootitem.skin = 2352567;
            }
        }
        private readonly string permAvailability = "XDStatistics.availability";

        private class Configuration
        {
            [JsonProperty("Настройка выдачи очков | Setting up the issuance of points")]
            public SettingsScore settingsScore = new SettingsScore();
            [JsonProperty("Настройка награды топ игрокам в каждой категории | Customize rewards for top 1 players in each category")]
            public SettingsPrize settingsPrize = new SettingsPrize();
            public class SettingsScore
            {
                [JsonProperty("Очки за крафт | Points for crafting")]
                public float craftScore = 1;
                [JsonProperty("Очки за бочки | Points for barrels")]
                public float barrelScore = 1;
                [JsonProperty("Очки за установку строительных блоков | Points for installing building blocks")]
                public float BuildingScore = 1;
                [JsonProperty("Очки за использования взрывчатых предметов | Points for using explosive items")]
                public Dictionary<string, float> ExplosionScore = new Dictionary<string, float>();
                [JsonProperty("Очки за добычу ресурсов | Points for resource extraction")]
                public Dictionary<string, float> GatherScore = new Dictionary<string, float>();
                [JsonProperty("Очки за найденные скрап | Points for found scraps")]
                public float ScrapScore = 0.5f;
                [JsonProperty("Очки за сбор урожая (с плантации) | Points for harvesting (from the plantation)")]
                public float PlantScore = 0.2f;
                [JsonProperty("Очки за убийство животных | Points for killing animals")]
                public float AnimalScore = 1;
                [JsonProperty("Очки за сбитие вертолета | Points for shooting down a helicopter")]
                public float HeliScore = 5;
                [JsonProperty("Очки за взрыв танка | Points for tank explosion")]
                public float BradleyScore = 5;
                [JsonProperty("Очки за убийство нпс | Points for killing NPCs")]
                public float NpcScore = 5;
                [JsonProperty("Очки за убийство игроков | Points for killing players")]
                public float PlayerScore = 10;
                [JsonProperty("Очки за время (За кажду. минуту игры на сервере) | Points for time (for every minute of the game on the server)")]
                public float TimeScore = 0.2f;
                [JsonProperty("Сколько отнять очков за суицид ? | How many points to take away for suicide ?")]
                public float SuicideScore = 2;
                [JsonProperty("Сколько отнять очков за смерть ? | How many points to take away for death ?")]
                public float DeathScore = 1;
                [JsonProperty("Черный список ресусов и взрывчатых предметов | Blacklist of resources and explosive items", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<string> blackListed = new List<string> { "ammo.rifle.explosive" };
            }
            public class SettingsPrize
            {
                [JsonProperty("Использовать выдачу награды после вайпа ? | Use the award issue after the wipe ?")]
                public bool prizeUse = false;
                [JsonProperty("Награда в категории SCORE | Award in the SCORE category")]
                public List<Prize> prizeScore = new List<Prize>();
                [JsonProperty("Награда в категории Киллер | Award in the Killer category")]
                public List<Prize> prizeKiller = new List<Prize>();
                [JsonProperty("Награда в категории Фармила | Award in the gathering category")]
                public List<Prize> prizeFarm = new List<Prize>();
                [JsonProperty("Награда в категории рейдер | Reward in the Raider category")]
                public List<Prize> prizeRaid = new List<Prize>();
                [JsonProperty("Награда в категории Большой онлайн | Award in the Big Online category")]
                public List<Prize> prizeTime = new List<Prize>();
                [JsonProperty("Награда в категории Убийца НПС | Reward in the NPC Killer category")]
                public List<Prize> prizeNPCKiller = new List<Prize>();
                [JsonProperty("Награда в категории убица животных | Award in the Animal killer category")]
                public List<Prize> prizeAnimalKiller = new List<Prize>();
                [JsonProperty("[RU][GameStores] ID магазина")]
                public string ShopID = "";
                [JsonProperty("[RU][GameStores] ID сервера")]
                public string ServerID = "";
                [JsonProperty("[RU][GameStores] Секретный ключ")]
                public string SecretKey = "";
                internal class Prize
                {
                    [JsonProperty("Использовать комманды в виде приза ? | Use command as a prize ?")]
                    public bool commandPrizeUse = true;
                    [JsonProperty("За какое место давать эту награду ? (от 1 до 3) если не нужно награждать эту категорию. удалите все награды | For which place to give this award ? (from 1 to 3) if you do not need to award this category. remove all rewards")]
                    public int top = 1;
                    [JsonProperty("[RU]Использовать магазин GameStore для выдачи награды")]
                    public bool gamestorePrizeUse = false;
                    [JsonProperty("[RU]Использовать магазин MoscowOVH для выдачи награды")]
                    public bool ovhPrizeUse = false;
                    [JsonProperty("Использовать [IQEconomic или Economics или ServerRewards] для выдачи награды | Use [IQEconomic or Economics or Server Rewards] to issue a reward")]
                    public bool economicPrizeUse = false;
                    [JsonProperty("Команды для приза | Command for the prize")]
                    public List<string> commandPrizeList = new List<string>();
                    [JsonProperty("[RU][GameStores] Сообщения для истории в магазине")]
                    public string balancePlusMess = "За топ 1!!!";
                    [JsonProperty("[RU][GameStores или MoscowOVH] Сколько начислять денег на баланс")]
                    public int balancePlus = 30;
                    [JsonProperty("[IQEconomic или Economics или ServerRewards] Сколько начислять денег на баланс | [IQEconomic or Economics or ServerRewards] How much money to add to the balance")]
                    public int balanceEconomicsPlus = 100;

                    public void GiftPrizePlayer(string player)
                    {
                        if (commandPrizeUse)
                        {
                            foreach (string cmd in commandPrizeList)
                                _.Server.Command(cmd.Replace("%STEAMID%", player));
                        }
                        if (gamestorePrizeUse && _?.GameStoresRUST)
                        {

                            string uri = $"https://gamestores.ru/api?shop_id={_._config.settingsPrize.ShopID}&secret={_._config.settingsPrize.SecretKey}&server={_._config.settingsPrize.ServerID}&action=moneys&type=plus&steam_id={player}&amount={balancePlus}&mess={balancePlusMess}";
                            _.webrequest.Enqueue(uri
                           ,
                           "", (code, response) =>
                           {
                               switch (code)
                               {
                                   case 0:
                                       {
                                           _.PrintError("Api does not responded to a request");
                                           break;
                                       }
                                   case 200:
                                       {
                                           break;
                                       }
                                   case 404:
                                       {
                                           _.PrintError($"Please check your configuration! {code}");
                                           break;
                                       }
                               }
                           }, _);
                        }
                        if (ovhPrizeUse)
                        {
                            if (_?.RustStore)
                            {
                                _.RustStore.CallHook("APIChangeUserBalance", ulong.Parse(player), balancePlus, new Action<string>(result =>
                                {
                                    if (result == "SUCCESS")
                                        return;
                                    Interface.Oxide.LogDebug($"Баланс игрока {ulong.Parse(player)} не был изменен, ошибка: {result}");
                                }));
                            }
                        }
                        if (economicPrizeUse)
                        {
                            if (_?.Economics)
                            {
                                _.Economics.Call("Deposit", ulong.Parse(player), (double)balanceEconomicsPlus);
                            }
                            else if (_?.IQEconomic)
                            {
                                _.IQEconomic.Call("API_SET_BALANCE", ulong.Parse(player), balanceEconomicsPlus);
                            }
                            else if (_?.ServerRewards)
                            {
                                _.ServerRewards.Call("AddPoints", ulong.Parse(player), balanceEconomicsPlus);
                            }
                        }
                    }

                }
            }
            [JsonProperty("Настройки дискорд | Discord Settings")]
            public DiscordMessage discordMessage = new DiscordMessage();

            [JsonProperty("Основные настройки плагина | Basic plugin settings")]
            public Settings settings = new Settings();
            public class DiscordMessage
            {
                [JsonProperty("Отправлять в дискорд топ 5 лучших игроков в разных категориях ? | Send the top 5 best players in different categories to discord ?")]
                public bool discordTopFiveUse = false;
                [JsonProperty("Раз в сколько секунд будет отправлятся сообщение ? | Once in how many seconds will a message be sent ?")]
                public int discordSendTopTime = 600;
                [JsonProperty("ВебХук дискорда | Discord WebHook")]
                public string weebHook = string.Empty;
                [JsonProperty("Цвет линии в сообщения или несколько | The color of the line in the message or several", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public int[] colorLines = new int[] { 53380, 9359868, 11253955 };
                [JsonProperty("Дополнительный текст к сообщению | Additional text to the message")]
                public string message = string.Empty;

            }
            [JsonProperty("Настройки интерфейса | Interface Settings")]
            public SettingsInterface settingsInterface = new SettingsInterface();
            public class Settings
            {
                [JsonProperty("Чат команды для открытия статистики | Chat commands for opening statistics", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<string> chatCommandOpenStat = new List<string> { "stat", "top" };
                [JsonProperty("Консольная команда для открытия статистики | Console command to open statistics")]
                public string consoleCommandOpenStat = "stat";
                [JsonProperty("Отправлять в чат сообщения с топ 5 игроками в разных категориях | Send chat messages with top 5 players in different categories")]
                public bool chatSendTop = true;
                [JsonProperty("Раз в сколько секунд будет отправлятся сообщение ? | Once in how many seconds will a message be sent ?")]
                public int chatSendTopTime = 600;
                [JsonProperty("Включить возможность сбросить свою статистику ? (требуется XDStatistics.reset) | Enable the ability to reset your stats ? (requires XDStatistics.reset)")]
                public bool dropStatUse = false;
                [JsonProperty("Включить возможность скрыть свою статистику от пользователей ? (требуется XDStatistics.availability) | Enable the ability to hide your statistics from users ? (requires XDStatistics.availability)")]
                public bool availabilityUse = true;
                [JsonProperty("очищать данные при вайпе | Clear data when wiped")]
                public bool wipeData = true;
                [JsonProperty("Раз во сколько минут будут сохранятся данные | Once in a rowman, the data will be saved.")]
                public int dataSaveTime = 30;
                [JsonProperty("Учитывать убийство npc для фаворитного оружия ? | Consider killing an NPC for a favorite weapon ?")]
                public bool npsDeathUse = false;
                [JsonProperty("У вас PVE сервер ?  | You have a PVE server?")]
                public bool pveServerMode = false;
                [JsonProperty("Список игроков, которые не будут включены в статистику (SteamID) | List of players who will not be included in the statistics (SteamID)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<ulong> ignoreList = new List<ulong>();
            }
            public class SettingsInterface
            {
                [JsonProperty("Ссылка на свой задний фон (Если нужно) | Link to your background (If necessary)")]
                public string backgroundImageUrl = "";
                [JsonProperty("Цвет плашки заднего фона в топ 10 за 2 место | Background color in the top 10 for 2st place")]
                public string ColorTop2 = "0.7529412 0.7529412 0.7529412 0.49";
                [JsonProperty("Цвет плашки заднего фона в топ 10 за 1 место | Background color in the top 10 for 1st place")]
                public string ColorTop1 = "1 0.8431373 0 0.49";
                [JsonProperty("Использовать свой задний фон ? (указанный снизу)| Use your own background ? (indicated at the bottom)")]
                public bool UsebackgroundImageUrl = false;
                [JsonProperty("Цвет плашки заднего фона в топ 10 за 3 место | Background color in the top 10 for 3st place")]
                public string ColorTop3 = "0.8 0.4 0.2 0.4";

            }
        }
        private void OnRocketLaunched(BasePlayer player, BaseEntity entity)
        {
            if (player == null || entity == null)
                return;
            ExplosionProgressAdd(player, entity);
        }
        private void CheckInMinute()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player == null || !player.userID.IsSteamId() || IgnoreReservedPlayer.ContainsKey(player.userID))
                    continue;
                PlayerInfo.AddPlayedTime(player.userID);
            }

            timer.Once(60f, CheckInMinute);
        }
        private void OnNewSave()
        {
            if (_config.settingsPrize.prizeUse)
            {
                rewardPlayerCoroutine = ServerMgr.Instance.StartCoroutine(ParseTopUserForPrize());
            }
            else if (_config.settings.wipeData)
            {
                PlayerInfo.ClearDataWipe();
                NextTick(() =>
                {
                    SaveData();
                    SaveDataIgnoreList();
                });
                PrintWarning(GetLang("STAT_PRINT_WIPE"));
            }
        }
        public string GetLang(string LangKey, string userID = null, params object[] args)
        {
            sb?.Clear();
            if (args != null)
            {
                sb?.AppendFormat(lang.GetMessage(LangKey, this, userID), args);
                return sb?.ToString();
            }
            return lang.GetMessage(LangKey, this, userID);
        }

        private void SearchPageUser(BasePlayer player, string target = "")
        {
            string SearchName = "";
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "1 1 1 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-378.454 -264.835", OffsetMax = "381.998 266.939" }
            }, UI_INTERFACE, UI_SEARCH_USER);

            container.Add(new CuiElement
            {
                Name = "SEARCH_LINE",
                Parent = UI_SEARCH_USER,
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("18") },
                    new CuiRectTransformComponent {AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-181.67 -31.1", OffsetMax = "149.27 -7.1" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "LOUPE_SEARCH_IMG",
                Parent = "SEARCH_LINE",
                Components = {
                    new CuiImageComponent { Color = "1 1 1 1", Sprite = "assets/icons/examine.png" },
                    new CuiRectTransformComponent { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "13.87 -10", OffsetMax = "33.87 10" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "INPUT_SEARCH",
                Parent = "SEARCH_LINE",
                Components = {
                    new CuiInputFieldComponent { Text = SearchName, Command = $"UI_HandlerStat listplayer {SearchName}", Color = "1 1 1 1", FontSize = 10, Align = TextAnchor.MiddleLeft, NeedsKeyboard = true, CharsLimit = 45 },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-119.86 -9.314", OffsetMax = "129.03 9.591" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-181.67 -1.95", OffsetMax = "149.27 212.35" }
            }, UI_SEARCH_USER, "LIST_USER_SEARCH");

            int y = 0, x = 0;
            foreach (var players in Players.Where(z => z.Value.Name.ToLower().Contains(target) && !_config.settings.ignoreList.Contains(z.Key)))
            {
                string LockStatus = players.Value.HidedStatistics == true ? "assets/icons/lock.png" : "assets/icons/unlock.png";
                string Command = players.Value.HidedStatistics == true ? "" : $"UI_HandlerStat GoStatPlayers {players.Key}";
                string nickName = "<color=white>" + GetCorrectName(players.Value.GetPlayerName(player.userID), 14) + "</color>";
                if (permission.UserHasPermission(player.UserIDString, permAdmin))
                    Command = $"UI_HandlerStat GoStatPlayers {players.Key}";

                container.Add(new CuiElement
                {
                    Name = "USER_IN_SEARCH",
                    Parent = "LIST_USER_SEARCH",
                    Components = {
                        new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("19") },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-164.971 + (x * 112.586)} {84.138 - (y * 26.281)}", OffsetMax = $"{-62.801 + (x * 112.586)} {105.623 - (y * 26.281)}" }
                    }
                });

                container.Add(new CuiElement
                {
                    Name = "USER_HIDE_PROFILE",
                    Parent = "USER_IN_SEARCH",
                    Components = {
                    new CuiImageComponent { Color = "1 1 1 1", Sprite = LockStatus },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-45.68 -7.5", OffsetMax = "-30.68 7.5" }
                }
                });
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-26.8 -10.7", OffsetMax = "48.3 10.7" },
                    Text = { Text = nickName ?? "UNKNOW", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
                }, "USER_IN_SEARCH");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = Command, Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "USER_IN_SEARCH");

                x++;
                if (x == 3)
                {
                    x = 0;
                    y++;
                    if (y == 8)
                        break;
                }
            }
            CloseLayer(player);
            CuiHelper.AddUi(player, container);
        }



        private JObject API_GetAllPlayerStat(ulong id) => JObject.FromObject(PlayerInfo.Find(id));

        private object OnPlayerDeath(BasePlayer player, HitInfo hitInfo)
        {
            if (player != null)
            {
                if (hitInfo == null || !player.userID.IsSteamId())
                    return null;
                PlayerInfo PlayerstatVic = PlayerInfo.Find(player.userID);
                if (hitInfo.InitiatorPlayer != null)
                {
                    if (!hitInfo.InitiatorPlayer.userID.IsSteamId())
                        return null;
                    BasePlayer initiator = hitInfo.InitiatorPlayer;
                    if (IsFriends(initiator.userID, player.userID))
                        return null;
                    if (IsClans(initiator.UserIDString, player.UserIDString))
                        return null;
                    if (IsDuel(initiator.userID))
                        return null;
                    PlayerInfo PlayerstatInitiator = PlayerInfo.Find(initiator.userID);
                    if (hitInfo.damageTypes.GetMajorityDamageType() == DamageType.Suicide)
                    {
                        PlayerstatVic.pVP.Suicides++;
                        PlayerstatVic.pVP.Deaths++;
                        PlayerstatVic.Score -= _config.settingsScore.SuicideScore;
                        return null;
                    }
                    WeaponProgressAdd(PlayerstatInitiator, hitInfo, true);
                    PlayerstatInitiator.pVP.Kills++;
                    PlayerstatInitiator.pVP.Shots++;
                    PlayerstatInitiator.pVP.Headshots += hitInfo.isHeadshot ? 1 : 0;
                    PlayerstatInitiator.Score += _config.settingsScore.PlayerScore;
                    PlayerstatVic.pVP.Deaths++;
                    PlayerstatVic.Score -= _config.settingsScore.DeathScore;
                }
                else
                {
                    if (hitInfo.damageTypes.GetMajorityDamageType() == DamageType.Suicide)
                        return null;
                    PlayerstatVic.pVP.Deaths++;
                    PlayerstatVic.Score -= _config.settingsScore.DeathScore;
                }
            }
            return null;
        }
        private static Dictionary<BasePlayer, List<UInt64>> LootersListCarte = new Dictionary<BasePlayer, List<UInt64>>();

        private void OnServerShutdown() => Unload();

        private class Embed
        {
            public int color
            {
                get; set;
            }
            [JsonProperty("fields")] public List<Field> Fields { get; set; } = new List<Field>();

            public Embed AddField(string name, string value, bool inline, int colors)
            {
                Fields.Add(new Field(name, Regex.Replace(value, "<.*?>", string.Empty), inline));
                color = colors;
                return this;
            }
        }
        [ConsoleCommand("stat.score")]
        void ShopMoneyGive(ConsoleSystem.Arg arg)
        {
            if (arg == null)
                return;

            var player = arg.Player();
            if (player != null && !player.IsAdmin)
            {
                PrintToConsole(player, GetLang("STAT_CMD_1", player.UserIDString));
                return;
            }

            switch (arg.Args[0])
            {
                case "give":
                    {
                        ulong userID = ulong.Parse(arg.Args[1]);
                        int score = Convert.ToInt32(arg.Args[2]);
                        PlayerInfo Player = PlayerInfo.Find(userID);
                        Player.Score += score;
                        Puts(GetLang("STAT_CMD_10", null, userID, score));
                        break;
                    }
                case "remove":
                    {
                        ulong userID = ulong.Parse(arg.Args[1]);
                        int score = Convert.ToInt32(arg.Args[2]);
                        PlayerInfo Player = PlayerInfo.Find(userID);
                        Player.Score -= score;
                        Puts(GetLang("STAT_CMD_11", null, userID, score));
                        break;
                    }
            }
        }

        private void UserStat(BasePlayer player, ulong target = 0)
        {
            PlayerInfo statInfo = PlayerInfo.Find(target == 0 ? player.userID : target);
            ulong userid = target == 0 ? player.userID : target;
            string color = BasePlayer.FindByID(userid) != null ? "0.55 0.78 0.24 1" : "0.8 0.28 0.2 1";
            int kills = _config.settings.pveServerMode ? statInfo.pVP.KillsNpc : statInfo.pVP.Kills;
            string titleKills = _config.settings.pveServerMode ? GetLang("STAT_UI_PVP_KILLS_NPC", player.UserIDString) : GetLang("STAT_UI_PVP_KILLS", player.UserIDString);
            string pageTitle = "<color=white>" + statInfo.Name.ToString() + "</color>";

            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-400.25 -331.5", OffsetMax = "393.9 274.4" }
            }, UI_INTERFACE, UI_USER_STAT);

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-400.2 -331.5", OffsetMax = "393.9 274.4" }
            }, UI_USER_STAT, UI_USER_STAT_INFO);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "14.9 -69.4", OffsetMax = "227.4 -53.1" },
                Text = { Text = GetLang("STAT_UI_INFO", player.UserIDString, pageTitle), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, UI_USER_STAT_INFO, "INFO_USER_NICK");

            container.Add(new CuiElement
            {
                Name = "USER_AVATAR_LAYER",
                Parent = UI_USER_STAT_INFO,
                Components = {
                    new CuiRawImageComponent { Color = color, Png = ImageUi.GetImage("2") },
                    new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "15 -130", OffsetMax = "68 -77" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "AVATAR_ON_STEAM",
                Parent = "USER_AVATAR_LAYER",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1",  Png = GetImage(target == 0 ? player.UserIDString : target.ToString()) },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-25 -25", OffsetMax = "25 25" }
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "15.4 -179.8", OffsetMax = "87.3 -161.7" },
                Text = { Text = GetLang("STAT_UI_ACTIVITY", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, UI_USER_STAT_INFO, "USER_ACTIVE");

            if (target == 0 && (_config.settings.availabilityUse && (permission.UserHasPermission(player.UserIDString, permAvailability) || _config.settings.dropStatUse && permission.UserHasPermission(player.UserIDString, permReset))))
            {
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "15.477 -290.08", OffsetMax = "87.3 -271.9" },
                    Text = { Text = GetLang("STAT_UI_SETTINGS", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
                }, UI_USER_STAT_INFO, "USER_SETINGS");
            }

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "14.8 -151.2", OffsetMax = "227.3 -134.3" },
                Text = { Text = GetLang("STAT_UI_PLACE_TOP", player.UserIDString, GetTopScore(target == 0 ? player.userID : target)), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "0.8 0.8 0.8 1" }
            }, UI_USER_STAT_INFO, "TOP_IN_USER");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "14.6 -201.1", OffsetMax = "227.2 -184.2" },
                Text = { Text = GetLang("STAT_UI_ACTIVITY_TODAY", player.UserIDString, TimeHelper.FormatTime(TimeSpan.FromMinutes(statInfo.playedTime.PlayedToday), 5, lang.GetLanguage(player.UserIDString))), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "0.8 0.8 0.8 1" }
            }, UI_USER_STAT_INFO, "TODAY_ACTIVE_USER");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-382.31 79.2", OffsetMax = "-169.73 96.181" },
                Text = { Text = GetLang("STAT_UI_ACTIVITY_TOTAL", player.UserIDString, TimeHelper.FormatTime(TimeSpan.FromMinutes(statInfo.playedTime.PlayedForWipe), 5, lang.GetLanguage(player.UserIDString))), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "0.8 0.8 0.8 1" }
            }, UI_USER_STAT_INFO, "ALLTIME_ACTIVE_USER");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-382.31 56.919", OffsetMax = "-169.73 73.881" },
                Text = { Text = GetLang("STAT_UI_SCORE", player.UserIDString, statInfo.Score.ToString("0.0")), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "0.8 0.8 0.8 1" }
            }, UI_USER_STAT_INFO, "SCORE_USER");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-209.5 -67.825", OffsetMax = "-130.043 -53.66" },
                Text = { Text = GetLang("STAT_UI_PVP", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, UI_USER_STAT_INFO, "PVP_STAT_USER");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-209.5 36.6", OffsetMax = "-13.9 50.7" },
                Text = { Text = GetLang("STAT_UI_FAVORITE_WEAPON", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, UI_USER_STAT_INFO, "RIFLE_FAVORITE_USER");

            container.Add(new CuiElement
            {
                Name = "KILL_STAT_PLAYER",
                Parent = UI_USER_STAT_INFO,
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("7") },
                    new CuiRectTransformComponent { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-209.4 -123.24", OffsetMax = "-14.4 -78.4" }
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "19 -7.01", OffsetMax = "97 8.4" },
                Text = { Text = titleKills, Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, "KILL_STAT_PLAYER", "LABEL_KILL_AMOUNT");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-98 -7.01", OffsetMax = "-16.8 8.4" },
                Text = { Text = kills.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleRight, Color = "0.8 0.8 0.8 1" }
            }, "KILL_STAT_PLAYER", "LABEL_KILL_AMOUNTTWO");

            container.Add(new CuiElement
            {
                Name = "KILLSHOT_STAT_PLAYER",
                Parent = UI_USER_STAT_INFO,
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("7") },
                    new CuiRectTransformComponent { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-209.4 -173.7", OffsetMax = "-14.4 -128.849" }
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "19 -7.01", OffsetMax = "97 8.4" },
                Text = { Text = GetLang("STAT_UI_PVP_DEATH", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, "KILLSHOT_STAT_PLAYER", "LABEL_KILL_AMOUNT");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-98 -7.01", OffsetMax = "-16.8 8.4" },
                Text = { Text = statInfo.pVP.Deaths.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleRight, Color = "0.8 0.8 0.8 1" }
            }, "KILLSHOT_STAT_PLAYER", "LABEL_KILL_AMOUNTTWO");

            container.Add(new CuiElement
            {
                Name = "DEATCH_STAT_PLAYER",
                Parent = UI_USER_STAT_INFO,
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1",Png = ImageUi.GetImage("7") },
                    new CuiRectTransformComponent { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-209.39 -224.721", OffsetMax = "-14.47 -179.879" }
                }
            });
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "19 -7.01", OffsetMax = "97 8.4" },
                Text = { Text = GetLang("STAT_UI_PVP_KDR", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, "DEATCH_STAT_PLAYER", "LABEL_KILL_AMOUNT");
            float kdr = statInfo.pVP.Deaths == 0 ? kills : (float)Math.Round(((float)kills) / statInfo.pVP.Deaths, 2);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-98 -7.01", OffsetMax = "-16.8 8.4" },
                Text = { Text = kdr.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleRight, Color = "0.8 0.8 0.84919 1" }
            }, "DEATCH_STAT_PLAYER", "LABEL_KILL_AMOUNTTWO");

            container.Add(new CuiElement
            {
                Name = "FAVORITE_WEAPON_STAT_PLAYER",
                Parent = UI_USER_STAT_INFO,
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("8") },
                    new CuiRectTransformComponent { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-210.15 -324.607", OffsetMax = "-13.9 -278.569" }
                }
            });

            var weaponTop = statInfo.weapon.WeaponUsed.OrderByDescending(x => x.Value.Kills).Take(1).FirstOrDefault();
            if (weaponTop.Key != null)
            {
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "46.321 -16.5", OffsetMax = "176.509 16.5" },
                    Text = { Text = GetLang("STAT_UI_FAVORITE_WEAPON_KILLS", player.UserIDString, weaponTop.Value.Kills, weaponTop.Value.Shots), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, "FAVORITE_WEAPON_STAT_PLAYER", "LABEL_KILL_AMOUNT");

                container.Add(new CuiElement
                {
                    Name = "WEAPON_IMG_USER",
                    Parent = "FAVORITE_WEAPON_STAT_PLAYER",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage(weaponTop.Key + 50) },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-88.5 -16.5", OffsetMax = "-55.5 16.5" }
                }
                });
            }
            else
            {
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = GetLang("STAT_UI_FAVORITE_WEAPON_NOT_DATA", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, "FAVORITE_WEAPON_STAT_PLAYER", "LABEL_KILL_AMOUNT");
            }


            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-209.498 -63.3", OffsetMax = "-79.669 -49.2" },
                Text = { Text = GetLang("STAT_UI_OTHER_STAT", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, UI_USER_STAT_INFO, "OTHER_STAT_LABEL");


            CloseLayer(player);
            CuiHelper.DestroyUi(player, "USER_STAT");
            CuiHelper.AddUi(player, container);
            CategoryStatUser(player, target);
            OtherStatUser(player, target);
            if (target == 0)
            {
                if (_config.settings.dropStatUse && permission.UserHasPermission(player.UserIDString, permReset))
                    ButtonDropStat(player, statInfo);
                if (_config.settings.availabilityUse && permission.UserHasPermission(player.UserIDString, permAvailability))
                    ButtonHideStat(player, statInfo);
            }
        }
        public const string UI_SEARCH_USER = "SEARCH_USER";
        private void WeaponProgressAdd(PlayerInfo player, HitInfo hitinfo, bool kill = false)
        {
            if (hitinfo == null || hitinfo.WeaponPrefab == null)
                return;
            string WeaponName = string.Empty;
            if (_prefabID2Item.TryGetValue(hitinfo.WeaponPrefab.prefabID, out WeaponName) == false)
            {
                _prefabNameItem.TryGetValue(hitinfo.WeaponPrefab.ShortPrefabName, out WeaponName);
            }
            if (!string.IsNullOrEmpty(WeaponName))
            {
                if (!player.weapon.WeaponUsed.ContainsKey(WeaponName))
                {
                    player.weapon.WeaponUsed.Add(WeaponName, new PlayerInfo.Weapon.WeaponInfo { Kills = kill ? 1 : 0, Headshots = hitinfo.isHeadshot ? 1 : 0 });
                }
                else
                {
                    var weapon = player.weapon.WeaponUsed[WeaponName];
                    weapon.Headshots += hitinfo.isHeadshot ? 1 : 0;
                    weapon.Shots++;
                    if (kill)
                        weapon.Kills++;
                }
            }
        }
        private void MenuButton(BasePlayer player, int page = 0)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "1 1 1 0" },
                RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-132.2 -75.5", OffsetMax = "122.197 -48.5" }
            }, UI_INTERFACE, UI_MENU_BUTTON);
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "-0.001 -13.4", OffsetMax = "88.1 13.4" },
                Button = { Command = $"UI_HandlerStat Page_swap 0", Color = "0 0 0 0" },
                Text = { Text = GetLang("STAT_UI_MY_STAT", player.UserIDString), FontSize = 10, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter }
            }, UI_MENU_BUTTON, "BUTTON_MY_STAT");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = page == 0 ? "0.2 0.6 0.12 0.64" : "1 1 1 0.3" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-44.057 -13.4", OffsetMax = "44.05 -11.6" }
            }, "BUTTON_MY_STAT", "Panel_8193");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-39.106 -13.4", OffsetMax = "65.745 13.4" },
                Button = { Command = $"UI_HandlerStat Page_swap 1", Color = "0 0 0 0" },
                Text = { Text = GetLang("STAT_UI_TOP_TEN", player.UserIDString), FontSize = 10, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter }
            }, UI_MENU_BUTTON, "BUTTON_TOPTEN_USER");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = page == 1 ? "0.2 0.6 0.12 0.64" : "1 1 1 0.3" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-52.425 -13.4", OffsetMax = "52.425 -11.6" }
            }, "BUTTON_TOPTEN_USER", "Panel_8193");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-61.472 -13.4", OffsetMax = "0.216 13.4" }
            }, UI_MENU_BUTTON, "BUTTON_PAGE_SEARCH");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = page == 2 ? "0.2 0.6 0.12 0.64" : "1 1 1 0.3" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-30.8 -13.4", OffsetMax = "30.8 -11.6" }
            }, "BUTTON_PAGE_SEARCH");
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-30.8 -8.36", OffsetMax = "12.8 8.3" },
                Text = { Text = GetLang("STAT_UI_SEARCH", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
            }, "BUTTON_PAGE_SEARCH");

            container.Add(new CuiElement
            {
                Parent = "BUTTON_PAGE_SEARCH",
                Components = {
                    new CuiImageComponent { Color = "1 1 1 1", Sprite = "assets/icons/examine.png" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "12.862 -6.5", OffsetMax = "25.862 6.5" }
                }
            });
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = $"UI_HandlerStat Page_swap 2", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, "BUTTON_PAGE_SEARCH");

            CuiHelper.DestroyUi(player, UI_MENU_BUTTON);
            CuiHelper.AddUi(player, container);
            if (page == 0)
                UserStat(player);
            else if (page == 1)
                TopTen(player);
            else
                SearchPageUser(player);
        }

        private void MainMenuStat(BasePlayer player)
        {
            if (IgnoreReservedPlayer.ContainsKey(player.userID))
            {
                PrintToChat(player, GetLang("STAT_ADMIN_HIDE_STAT", player.UserIDString));
                return;
            }
            string background = _config.settingsInterface.UsebackgroundImageUrl ? GetImage("CustomBackgroundImage") : ImageUi.GetImage("1");
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
            }, "Overlay", UI_INTERFACE);

            container.Add(new CuiElement
            {
                Name = "BACKGROUND",
                Parent = UI_INTERFACE,
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = background },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });

            container.Add(new CuiElement
            {
                Name = UI_CLOSE_MENU,
                Parent = "BACKGROUND",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("14") },
                    new CuiRectTransformComponent { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-63.354 -36.7", OffsetMax = "-53.2 -27.9" }
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Close = UI_INTERFACE, Color = "0 0 0 0" },
                Text = { Text = "" }
            }, UI_CLOSE_MENU);

            CuiHelper.DestroyUi(player, UI_INTERFACE);
            CuiHelper.AddUi(player, container);
            MenuButton(player);
        }

        private void OnCollectiblePickup(CollectibleEntity collectible, BasePlayer player)
        {
            if (player == null)
                return;
            foreach (ItemAmount item in collectible.itemList)
            {
                NextTick(() =>
                {
                    if (AlowedSeedId.Contains(item.itemDef.itemid))
                    {
                        PlayerInfo Playerstat = PlayerInfo.Find(player.userID);
                        if (!Playerstat.harvesting.HarvestingList.ContainsKey(item.itemDef.shortname))
                        {
                            Playerstat.harvesting.HarvestingList.Add(item.itemDef.shortname, (int)item.amount);
                        }
                        else
                        {
                            Playerstat.harvesting.HarvestingList[item.itemDef.shortname] += (int)item.amount;
                        }
                        Playerstat.harvesting.AllHarvesting += (int)item.amount;
                        Playerstat.Score += _config.settingsScore.PlantScore;
                    }
                    else
                        ProgressAdd(player, item.itemDef.shortname, (int)item.amount, true);
                });
            }
        }
        private string GetCorrectName(string name, int length) => name.ToPrintable(length).EscapeRichText().Trim();
        private Dictionary<string, int> API_GetGathered(ulong id) => PlayerInfo.Find(id)?.gather.GatheredTotal;
        private static Dictionary<ulong, PlayerInfo> IgnoreReservedPlayer = new Dictionary<ulong, PlayerInfo>();
        private void LoadDataPrize() => PrizePlayerData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, List<PrizePlayer>>>("XDStatistics/PlayersReward");

        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            BasePlayer player = plan?.GetOwnerPlayer();
            if (player == null)
                return;

            BaseEntity entity = go?.ToBaseEntity();
            if (entity == null)
                return;

            if (entity.PrefabName.Contains("building core"))
            {
                PlayerInfo Playerstat = PlayerInfo.Find(player.userID);
                Playerstat.otherStat.BuildingCrate++;
                Playerstat.Score += _config.settingsScore.BuildingScore;
            }
        }

        private void TopTen(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "1 1 1 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-640 -360.01", OffsetMax = "640 266.9" }
            }, UI_INTERFACE, UI_TOP_TEN_USER);

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-599.3 -237.2", OffsetMax = "589.334 271.4" }
            }, UI_TOP_TEN_USER, "TOP_10_TABLE");

            if (_config.settings.pveServerMode)
            {
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "9.2 -20.6", OffsetMax = "176.3 -3.5" },
                    Text = { Text = GetLang("STAT_UI_CATEGORY_TOP_KILLER_ANIMALS", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
                }, "TOP_10_TABLE");

                container.Add(new CuiElement
                {
                    Name = "TOP_TABLE_0",
                    Parent = "TOP_10_TABLE",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 0.3",  Png = ImageUi.GetImage("20") },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "9.5 -441.7", OffsetMax = "177.2 -26.8" }
                }
                });
                IEnumerable<KeyValuePair<ulong, PlayerInfo>> AnimalKillerList = Players.Where(x => !_config.settings.ignoreList.Contains(x.Key)).OrderByDescending(x => x.Value.otherStat.AnimalsKill).Take(10);

                int y = 0;

                foreach (var item in AnimalKillerList)
                {
                    string LockStatus = item.Value.HidedStatistics == true ? "assets/icons/lock.png" : "assets/icons/unlock.png";
                    string Command = item.Value.HidedStatistics == true ? "" : $"UI_HandlerStat GoStatPlayers {item.Key}";
                    string nickName = "<color=white>" + GetCorrectName(item.Value.Name, 17) + "</color>";
                    if (permission.UserHasPermission(player.UserIDString, permAdmin))
                        Command = $"UI_HandlerStat GoStatPlayers {item.Key}";
                    string color = y == 0 ? _config.settingsInterface.ColorTop1 : y == 1 ? _config.settingsInterface.ColorTop2 : y == 2 ? _config.settingsInterface.ColorTop3 : "0 0 0 0";

                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = color },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-83.8 {177.2 - (y * 42.8)}", OffsetMax = $"83.8 {207.4 - (y * 42.8)}" }
                    }, "TOP_TABLE_0", "USER_INFO");
                    container.Add(new CuiElement
                    {
                        Name = "USER_STAT_HIDE",
                        Parent = "USER_INFO",
                        Components = {
                    new CuiImageComponent { Color = "1 1 1 1", Sprite = LockStatus },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-80.2 -6.5", OffsetMax = "-67.2 6.5" }
                }
                    });

                    container.Add(new CuiElement
                    {
                        Parent = "USER_INFO",
                        Components = {
                            new CuiTextComponent {Text = nickName, Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1"},
                            new CuiRectTransformComponent {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-64.8 -6.7", OffsetMax = "7.5 6.7" }
                        }
                    });

                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "7.522 -6.7", OffsetMax = "76.9 6.7" },
                        Text = { Text = item.Value.otherStat.AnimalsKill.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleRight, Color = "1 1 1 1" }
                    }, "USER_INFO");

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                        Button = { Command = Command, Color = "0 0 0 0" },
                        Text = { Text = "" }
                    }, "USER_INFO");
                    y++;
                }
            }
            else
            {

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "9.2 -20.655", OffsetMax = "176.3 -3.5" },
                    Text = { Text = GetLang("STAT_UI_CATEGORY_TOP_KILLER", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
                }, "TOP_10_TABLE");

                container.Add(new CuiElement
                {
                    Name = "TOP_TABLE_0",
                    Parent = "TOP_10_TABLE",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 0.3",  Png = ImageUi.GetImage("20") },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "9.5 -441.7", OffsetMax = "177.2 -26.8" }
                }
                });
                IEnumerable<KeyValuePair<ulong, PlayerInfo>> KillerList = Players.Where(x => !_config.settings.ignoreList.Contains(x.Key)).OrderByDescending(x => x.Value.pVP.Kills).Take(10);

                int y = 0;

                foreach (var item in KillerList)
                {
                    string LockStatus = item.Value.HidedStatistics == true ? "assets/icons/lock.png" : "assets/icons/unlock.png";
                    string Command = item.Value.HidedStatistics == true ? "" : $"UI_HandlerStat GoStatPlayers {item.Key}";
                    string nickName = "<color=white>" + GetCorrectName(item.Value.Name, 17) + "</color>";
                    if (permission.UserHasPermission(player.UserIDString, permAdmin))
                        Command = $"UI_HandlerStat GoStatPlayers {item.Key}";
                    string color = y == 0 ? _config.settingsInterface.ColorTop1 : y == 1 ? _config.settingsInterface.ColorTop2 : y == 2 ? _config.settingsInterface.ColorTop3 : "0 0 0 0";

                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = color },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-83.8 {177.2 - (y * 42.8)}", OffsetMax = $"83.8 {207.4 - (y * 42.8)}" }
                    }, "TOP_TABLE_0", "USER_INFO");
                    container.Add(new CuiElement
                    {
                        Name = "USER_STAT_HIDE",
                        Parent = "USER_INFO",
                        Components = {
                    new CuiImageComponent { Color = "1 1 1 1", Sprite = LockStatus },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-80.2 -6.5", OffsetMax = "-67.2 6.5" }
                }
                    });

                    container.Add(new CuiElement
                    {
                        Parent = "USER_INFO",
                        Components = {
                            new CuiTextComponent {Text = nickName, Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1"},
                            new CuiRectTransformComponent {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-64.8 -6.7", OffsetMax = "7.5 6.7" }
                        }
                    });

                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "7.5 -6.7", OffsetMax = "76.9 6.7" },
                        Text = { Text = item.Value.pVP.Kills.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleRight, Color = "1 1 1 1" }
                    }, "USER_INFO");

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                        Button = { Command = Command, Color = "0 0 0 0" },
                        Text = { Text = "" }
                    }, "USER_INFO");
                    y++;
                }
            }

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "209.1 -20.6", OffsetMax = "376.2 -3.5" },
                Text = { Text = GetLang("STAT_UI_CATEGORY_TOP_NPCKILLER", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, "TOP_10_TABLE");

            container.Add(new CuiElement
            {
                Name = "TOP_TABLE_1",
                Parent = "TOP_10_TABLE",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 0.3", Png = ImageUi.GetImage("20") },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "210.2 -441.7", OffsetMax = "377.9 -26.8" }
                }
            });

            IEnumerable<KeyValuePair<ulong, PlayerInfo>> KillerNpcList = Players.Where(x => !_config.settings.ignoreList.Contains(x.Key)).OrderByDescending(x => x.Value.pVP.KillsNpc).Take(10);

            int i = 0;
            foreach (var item in KillerNpcList)
            {
                string LockStatus = item.Value.HidedStatistics == true ? "assets/icons/lock.png" : "assets/icons/unlock.png";
                string Command = item.Value.HidedStatistics == true ? "" : $"UI_HandlerStat GoStatPlayers {item.Key}";
                string nickName = "<color=white>" + GetCorrectName(item.Value.Name, 17) + "</color>";
                if (permission.UserHasPermission(player.UserIDString, permAdmin))
                    Command = $"UI_HandlerStat GoStatPlayers {item.Key}";
                string color = i == 0 ? _config.settingsInterface.ColorTop1 : i == 1 ? _config.settingsInterface.ColorTop2 : i == 2 ? _config.settingsInterface.ColorTop3 : "0 0 0 0";

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = color },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-83.82 {177.2 - (i * 42.8)}", OffsetMax = $"83.8 {207.4 - (i * 42.8)}" }
                }, "TOP_TABLE_1", "USER_INFO");

                container.Add(new CuiElement
                {
                    Name = "USER_STAT_HIDE",
                    Parent = "USER_INFO",
                    Components = {
                    new CuiImageComponent { Color = "1 1 1 1", Sprite = LockStatus },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-80.2 -6.5", OffsetMax = "-67.2 6.5" }
                }
                });

                container.Add(new CuiElement
                {
                    Parent = "USER_INFO",
                    Components = {
                        new CuiTextComponent {Text = nickName, Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1"},
                        new CuiRectTransformComponent {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-64.8 -6.7", OffsetMax = "7.5 6.7" }
                    }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "7.5 -6.7", OffsetMax = "76.9 6.7" },
                    Text = { Text = item.Value.pVP.KillsNpc.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleRight, Color = "1 1 1 1" }
                }, "USER_INFO");
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = Command, Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "USER_INFO");
                i++;
            }


            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "409.229 -20.655", OffsetMax = "576.371 -3.5" },
                Text = { Text = GetLang("STAT_UI_CATEGORY_TOP_TIME", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, "TOP_10_TABLE");

            container.Add(new CuiElement
            {
                Name = "TOP_TABLE_2",
                Parent = "TOP_10_TABLE",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 0.3", Png = ImageUi.GetImage("20") },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "409.231 -441.751", OffsetMax = "576.869 -26.861" }
                }
            });
            IEnumerable<KeyValuePair<ulong, PlayerInfo>> TimeList = Players.Where(x => !_config.settings.ignoreList.Contains(x.Key)).OrderByDescending(x => x.Value.playedTime.PlayedForWipe).Take(10);

            int c = 0;
            foreach (var item in TimeList)
            {
                string LockStatus = item.Value.HidedStatistics == true ? "assets/icons/lock.png" : "assets/icons/unlock.png";
                string Command = item.Value.HidedStatistics == true ? "" : $"UI_HandlerStat GoStatPlayers {item.Key}";
                string nickName = "<color=white>" + GetCorrectName(item.Value.Name, 17) + "</color>";
                if (permission.UserHasPermission(player.UserIDString, permAdmin))
                    Command = $"UI_HandlerStat GoStatPlayers {item.Key}";
                string color = c == 0 ? _config.settingsInterface.ColorTop1 : c == 1 ? _config.settingsInterface.ColorTop2 : c == 2 ? _config.settingsInterface.ColorTop3 : "0 0 0 0";

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = color },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-83.82 {177.277 - (c * 42.863)}", OffsetMax = $"83.82 {207.448 - (c * 42.863)}" }
                }, "TOP_TABLE_2", "USER_INFO");
                container.Add(new CuiElement
                {
                    Name = "USER_STAT_HIDE",
                    Parent = "USER_INFO",
                    Components = {
                    new CuiImageComponent { Color = "1 1 1 1", Sprite = LockStatus },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-80.2 -6.5", OffsetMax = "-67.2 6.5" }
                }
                });
                container.Add(new CuiElement
                {
                    Parent = "USER_INFO",
                    Components = {
                        new CuiTextComponent {Text = nickName, Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1"},
                        new CuiRectTransformComponent {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-64.8 -6.7", OffsetMax = "7.522 6.7" }
                    }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "7.522 -6.7", OffsetMax = "76.9 6.7" },
                    Text = { Text = TimeHelper.FormatTime(TimeSpan.FromMinutes(item.Value.playedTime.PlayedForWipe), 5, lang.GetLanguage(player.UserIDString)), Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleRight, Color = "1 1 1 1" }
                }, "USER_INFO");
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = Command, Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "USER_INFO");
                c++;
            }

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-580.571 -20.655", OffsetMax = "-413.429 -3.5" },
                Text = { Text = GetLang("STAT_UI_CATEGORY_TOP_GATHER", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, "TOP_10_TABLE");

            container.Add(new CuiElement
            {
                Name = "TOP_TABLE_3",
                Parent = "TOP_10_TABLE",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 0.3", Png = ImageUi.GetImage("20") },
                    new CuiRectTransformComponent { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-580.099 -441.751", OffsetMax = "-412.461 -26.861" }
                }
            });

            IEnumerable<KeyValuePair<ulong, PlayerInfo>> FarmList = Players.Where(x => !_config.settings.ignoreList.Contains(x.Key)).OrderByDescending(x => x.Value.gather.AllGathered).Take(10);
            int f = 0;

            foreach (var item in FarmList)
            {
                string LockStatus = item.Value.HidedStatistics == true ? "assets/icons/lock.png" : "assets/icons/unlock.png";
                string Command = item.Value.HidedStatistics == true ? "" : $"UI_HandlerStat GoStatPlayers {item.Key}";
                string nickName = "<color=white>" + GetCorrectName(item.Value.Name, 17) + "</color>";
                if (permission.UserHasPermission(player.UserIDString, permAdmin))
                    Command = $"UI_HandlerStat GoStatPlayers {item.Key}";
                string color = f == 0 ? _config.settingsInterface.ColorTop1 : f == 1 ? _config.settingsInterface.ColorTop2 : f == 2 ? _config.settingsInterface.ColorTop3 : "0 0 0 0";

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = color },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-83.82 {177.277 - (f * 42.863)}", OffsetMax = $"83.82 {207.448 - (f * 42.863)}" }
                }, "TOP_TABLE_3", "USER_INFO");

                container.Add(new CuiElement
                {
                    Name = "USER_STAT_HIDE",
                    Parent = "USER_INFO",
                    Components = {
                    new CuiImageComponent { Color = "1 1 1 1", Sprite = LockStatus },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-80.2 -6.5", OffsetMax = "-67.2 6.5" }
                }
                });

                container.Add(new CuiElement
                {
                    Parent = "USER_INFO",
                    Components = {
                        new CuiTextComponent {Text = nickName, Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1"},
                        new CuiRectTransformComponent {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-64.8 -6.7", OffsetMax = "7.522 6.7" }
                    }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "7.522 -6.7", OffsetMax = "76.9 6.7" },
                    Text = { Text = item.Value.gather.AllGathered.ToString("0,0", CultureInfo.InvariantCulture), Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleRight, Color = "1 1 1 1" }
                }, "USER_INFO");
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = Command, Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "USER_INFO");
                f++;
            }



            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-381.151 -20.655", OffsetMax = "-214.009 -3.5" },
                Text = { Text = GetLang("STAT_UI_CATEGORY_TOP_EXPLOSED", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, "TOP_10_TABLE");


            container.Add(new CuiElement
            {
                Name = "TOP_TABLE_5",
                Parent = "TOP_10_TABLE",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 0.3", Png = ImageUi.GetImage("20") },
                    new CuiRectTransformComponent { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-380.679 -441.751", OffsetMax = "-213.041 -26.861"  }
                }
            });


            IEnumerable<KeyValuePair<ulong, PlayerInfo>> ExpList = Players.Where(x => !_config.settings.ignoreList.Contains(x.Key)).OrderByDescending(x => x.Value.explosion.AllExplosionUsed).Take(10);
            int z = 0;
            foreach (var item in ExpList)
            {
                string LockStatus = item.Value.HidedStatistics == true ? "assets/icons/lock.png" : "assets/icons/unlock.png";
                string Command = item.Value.HidedStatistics == true ? "" : $"UI_HandlerStat GoStatPlayers {item.Key}";
                string nickName = "<color=white>" + GetCorrectName(item.Value.Name, 17) + "</color>";
                if (permission.UserHasPermission(player.UserIDString, permAdmin))
                    Command = $"UI_HandlerStat GoStatPlayers {item.Key}";
                string color = z == 0 ? _config.settingsInterface.ColorTop1 : z == 1 ? _config.settingsInterface.ColorTop2 : z == 2 ? _config.settingsInterface.ColorTop3 : "0 0 0 0";

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = color },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-83.82 {177.277 - (z * 42.863)}", OffsetMax = $"83.82 {207.448 - (z * 42.863)}" }
                }, "TOP_TABLE_5", "USER_INFO");

                container.Add(new CuiElement
                {
                    Name = "USER_STAT_HIDE",
                    Parent = "USER_INFO",
                    Components = {
                        new CuiImageComponent { Color = "1 1 1 1", Sprite = LockStatus },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-80.2 -6.5", OffsetMax = "-67.2 6.5" }
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = "USER_INFO",
                    Components = {
                        new CuiTextComponent {Text = nickName, Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1"},
                        new CuiRectTransformComponent {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-64.8 -6.7", OffsetMax = "7.522 6.7" }
                    }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "7.522 -6.7", OffsetMax = "76.9 6.7" },
                    Text = { Text = item.Value.explosion.AllExplosionUsed.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleRight, Color = "1 1 1 1" }
                }, "USER_INFO");
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = Command, Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "USER_INFO");
                z++;
            }

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-180.8 -20.655", OffsetMax = "-13.7 -3.5" },
                Text = { Text = GetLang("STAT_UI_CATEGORY_TOP_SCORE", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, "TOP_10_TABLE");

            container.Add(new CuiElement
            {
                Name = "TOP_TABLE_4",
                Parent = "TOP_10_TABLE",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 0.3",  Png = ImageUi.GetImage("20") },
                    new CuiRectTransformComponent { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-180.419 -441.751", OffsetMax = "-12.781 -26.861"}
                }
            });

            IEnumerable<KeyValuePair<ulong, PlayerInfo>> ScoreList = Players.Where(x => !_config.settings.ignoreList.Contains(x.Key)).OrderByDescending(x => x.Value.Score).Take(10);
            int s = 0;
            foreach (var item in ScoreList)
            {
                string LockStatus = item.Value.HidedStatistics == true ? "assets/icons/lock.png" : "assets/icons/unlock.png";
                string Command = item.Value.HidedStatistics == true ? "" : $"UI_HandlerStat GoStatPlayers {item.Key}";
                string nickName = "<color=white>" + GetCorrectName(item.Value.Name, 17) + "</color>";
                if (permission.UserHasPermission(player.UserIDString, permAdmin))
                    Command = $"UI_HandlerStat GoStatPlayers {item.Key}";
                string color = s == 0 ? _config.settingsInterface.ColorTop1 : s == 1 ? _config.settingsInterface.ColorTop2 : s == 2 ? _config.settingsInterface.ColorTop3 : "0 0 0 0";
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = color },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-83.82 {177.277 - (s * 42.863)}", OffsetMax = $"83.82 {207.448 - (s * 42.863)}" }
                }, "TOP_TABLE_4", "USER_INFO");

                container.Add(new CuiElement
                {
                    Name = "USER_STAT_HIDE",
                    Parent = "USER_INFO",
                    Components = {
                        new CuiImageComponent { Color = "1 1 1 1", Sprite = LockStatus },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-80.2 -6.5", OffsetMax = "-67.2 6.5" }
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = "USER_INFO",
                    Components = {
                        new CuiTextComponent {Text = nickName, Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1"},
                        new CuiRectTransformComponent {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-64.8 -6.7", OffsetMax = "7.522 6.7" }
                    }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "7.522 -6.7", OffsetMax = "76.9 6.7" },
                    Text = { Text = item.Value.Score.ToString("0.00"), Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleRight, Color = "1 1 1 1" }
                }, "USER_INFO");
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = Command, Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "USER_INFO");
                s++;
            }

            CloseLayer(player);
            CuiHelper.AddUi(player, container);
        }

        private class PrizePlayer
        {
            public string Name = string.Empty;
            public CatType catType;
            public int value;
            public int top;
        }

        private void OnEntityDeath(LootContainer entity, HitInfo info)
        {
            if (entity == null || info == null)
                return;
            BasePlayer player = info.InitiatorPlayer;
            if (player == null)
                return;
            if (entity.ShortPrefabName.Contains("barrel"))
            {
                PlayerInfo Playerstat = PlayerInfo.Find(player.userID);
                Playerstat.otherStat.BarrelDeath++;
                Playerstat.Score += _config.settingsScore.barrelScore;
            }
        }
        private JObject API_GetPlayerPlayedTime(ulong id) => JObject.FromObject(PlayerInfo.Find(id).playedTime);

        [PluginReference] Plugin ImageLibrary, Friends, Clans, Battles, Duel, Economics, IQEconomic, ServerRewards, GameStoresRUST, RustStore;


        private string[] _gatherHooks = {
            "OnDispenserGather",
            "OnDispenserBonus",
            "OnCollectiblePickup",
        };
        private Dictionary<uint, string> _prefabID2Item = new Dictionary<uint, string>();


        private void Unload()
        {
            SaveData();
            SaveDataIgnoreList();
            SaveDataPrize();
            if (rewardPlayerCoroutine != null)
                ServerMgr.Instance.StopCoroutine(rewardPlayerCoroutine);
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, UI_INTERFACE);
            ImageUi.Unload();
            sb = null;
            _ = null;
        }
        private static List<KeyValuePair<ulong, PlayerInfo>> FindPlayers(string name)
        {
            var players = new List<KeyValuePair<ulong, PlayerInfo>>();

            var playersData = Players.Concat(IgnoreReservedPlayer);
            foreach (var activePlayer in playersData)
            {
                if (activePlayer.Value.Name.ToLower() == name)
                    players.Add(activePlayer);
                else if (activePlayer.Value.Name.ToLower().Contains(name))
                    players.Add(activePlayer);
            }

            return players;
        }
        private void AddDisplayName()
        {
            webrequest.Enqueue($"http://api.skyplugins.ru/api/getitemlist", "", (code, response) =>
            {
                if (code == 200)
                {
                    ItemList = JsonConvert.DeserializeObject<List<items>>(response);
                    for (int i = 0; i < ItemList.Count; i++)
                    {
                        items items = ItemList[i];
                        _itemName.Add(items.shortName, new ItemDisplayName { ru = items.RUdisplayName, en = items.ENdisplayName });
                    }
                }
            }, this);
        }
        private Coroutine rewardPlayerCoroutine { get; set; } = null;
        private void LoadDataIgnoreList() => IgnoreReservedPlayer = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerInfo>>("XDStatistics/IgnorePlayers");
        public const string UI_CLOSE_MENU = "CLOSE_MENU";
        private bool IsFriends(ulong userID, ulong targetID)
        {
            if (Friends)
                return (bool)Friends?.Call("HasFriend", userID, targetID);
            else if (RelationshipManager.ServerInstance.playerToTeam.ContainsKey(userID) && RelationshipManager.ServerInstance.playerToTeam[userID].members.Contains(targetID))
                return true;
            else
                return false;
        }
        [ConsoleCommand("stat.ignore")]
        private void CmdIgnorePlayer(ConsoleSystem.Arg arg)
        {
            if (arg == null)
                return;

            var player = arg.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, permAdmin))
            {
                PrintToConsole(player, GetLang("STAT_CMD_1", player.UserIDString));
                return;
            }

            if (arg == null || arg.Args == null || arg.Args.Count() == 0)
            {
                arg.ReplyWith(GetLang("STAT_CMD_2"));
                return;
            }

            ulong steamid = 0;
            if (!UInt64.TryParse(arg.Args[1], out steamid))
            {
                var players = FindPlayers(arg.Args[1].ToLower());

                if (players.Count == 0)
                {
                    arg.ReplyWith(GetLang("STAT_CMD_3"));
                    return;
                }

                if (players.Count > 1)
                {
                    var PlayersMore = "";
                    foreach (var plr in players)
                        PlayersMore = PlayersMore + "\n" + plr.Value.Name + " - " + plr.Key;

                    arg.ReplyWith(GetLang("STAT_CMD_4", null, PlayersMore));
                    return;
                }

                steamid = players[0].Key;
            }

            string name = "";
            for (int ii = 1; ii < arg.Args.Count(); ii++)
                name += arg.Args[ii] + " ";

            name = name.Trim(' ');

            if (string.IsNullOrEmpty(name))
            {
                arg.ReplyWith(GetLang("STAT_CMD_2"));
                return;
            }

            if (!Players.ContainsKey(steamid) && !IgnoreReservedPlayer.ContainsKey(steamid))
            {
                arg.ReplyWith(GetLang("STAT_CMD_5"));
                return;
            }

            PlayerInfo playerInfo = Players.ContainsKey(steamid) ? Players[steamid] : IgnoreReservedPlayer[steamid];

            switch (arg.Args[0])
            {
                case "add":
                case "a":
                    {
                        if (IgnoreReservedPlayer.ContainsKey(steamid))
                        {
                            arg.ReplyWith(GetLang("STAT_CMD_6", null, playerInfo.Name));
                            break;
                        }
                        IgnoreReservedPlayer.Add(steamid, playerInfo);
                        Players.Remove(steamid);
                        arg.ReplyWith(GetLang("STAT_CMD_7", null, playerInfo.Name));
                        break;
                    }
                case "r":
                case "remove":
                    {
                        if (!IgnoreReservedPlayer.ContainsKey(steamid))
                        {
                            arg.ReplyWith(GetLang("STAT_CMD_8", null, playerInfo.Name));
                            break;
                        }
                        PlayerInfo info = IgnoreReservedPlayer[steamid];
                        Players.Add(steamid, info);
                        IgnoreReservedPlayer.Remove(steamid);
                        arg.ReplyWith(GetLang("STAT_CMD_9", null, playerInfo.Name));
                        break;
                    }
                default:
                    break;
            }
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }
        private List<items> ItemList = new List<items>();

        private void ExplosionProgressAdd(BasePlayer player, BaseEntity entity, string shortname = "")
        {
            string WeaponName = string.IsNullOrWhiteSpace(shortname) == true ? string.Empty : shortname;
            if (entity != null)
            {
                if (_prefabID2Item.TryGetValue(entity.prefabID, out WeaponName) == false)
                {
                    _prefabNameItem.TryGetValue(entity.ShortPrefabName, out WeaponName);
                }
            }

            if (!string.IsNullOrEmpty(WeaponName))
            {
                PlayerInfo Playerstat = PlayerInfo.Find(player.userID);
                if (Playerstat.explosion.ExplosionUsed.ContainsKey(WeaponName) && !_config.settingsScore.blackListed.Contains(WeaponName))
                {
                    Playerstat.explosion.ExplosionUsed[WeaponName]++;
                    Playerstat.explosion.AllExplosionUsed++;
                    Playerstat.Score += _config.settingsScore.ExplosionScore[WeaponName];
                }
            }
        }
        Dictionary<uint, ulong> _heliattacker = new Dictionary<uint, ulong>();

        private void ButtonDropStat(BasePlayer player, PlayerInfo info)
        {
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "13.75 -29.559", OffsetMax = "160.23 -15.727" }
            }, UI_USER_STAT_INFO, "BUTTON_REFRESH_STAT");

            container.Add(new CuiElement
            {
                Name = "USER_REFRESH_STAT",
                Parent = "BUTTON_REFRESH_STAT",
                Components = {
                    new CuiImageComponent { Color = "1 1 1 1", Sprite = "assets/icons/clear_list.png" },
                    new CuiRectTransformComponent { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "1.74 -6.5", OffsetMax = "14.74 6.5" }
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-120.55 -6.916", OffsetMax = "21.75 8.202" },
                Text = { Text = GetLang("STAT_UI_RESET_STAT", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "0.8 0.8 0.8 1" }
            }, "BUTTON_REFRESH_STAT", "LABEL_REFRESH_USER");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = "UI_HandlerStat confirm", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, "BUTTON_REFRESH_STAT");

            CuiHelper.DestroyUi(player, "BUTTON_REFRESH_STAT");
            CuiHelper.AddUi(player, container);
        }
        private Tuple<string, string> GetRandomTopPlayer()
        {
            int i = Core.Random.Range(0, 7);
            string playerstat = string.Empty;
            string langmsg = string.Empty;
            int top = 1;

            switch (i)
            {
                case 0:
                    {
                        if (_config.settings.pveServerMode)
                        {
                            var playerList = Players.Where(x => !_config.settings.ignoreList.Contains(x.Key)).OrderByDescending(x => x.Value.pVP.KillsNpc).Take(5);
                            foreach (var playerInfo in playerList)
                            {
                                playerstat += $"<color=#faec84>{top}</color>. {playerInfo.Value.Name} : {playerInfo.Value.pVP.KillsNpc}\n";
                                top++;
                            }
                            langmsg = "STAT_TOP_FIVE_KILL_NPC";
                        }
                        else
                        {
                            var playerList = Players.Where(x => !_config.settings.ignoreList.Contains(x.Key)).OrderByDescending(x => x.Value.pVP.Kills).Take(5);
                            foreach (var playerInfo in playerList)
                            {
                                playerstat += $"<color=#faec84>{top}</color>. {playerInfo.Value.Name} : {playerInfo.Value.pVP.Kills}\n";
                                top++;
                            }
                            langmsg = "STAT_TOP_FIVE_KILL";
                        }
                        break;
                    }
                case 1:
                    {
                        var playerList = Players.Where(x => !_config.settings.ignoreList.Contains(x.Key)).OrderByDescending(x => x.Value.gather.AllGathered).Take(5);
                        foreach (var playerInfo in playerList)
                        {
                            playerstat += $"<color=#faec84>{top}</color>. {playerInfo.Value.Name} : {playerInfo.Value.gather.AllGathered.ToString("0,0", CultureInfo.InvariantCulture)}\n";
                            top++;
                        }
                        langmsg = "STAT_TOP_FIVE_FARM";
                        break;
                    }
                case 2:
                    {
                        var playerList = Players.Where(x => !_config.settings.ignoreList.Contains(x.Key)).OrderByDescending(x => x.Value.explosion.AllExplosionUsed).Take(5);
                        foreach (var playerInfo in playerList)
                        {
                            playerstat += $"<color=#faec84>{top}</color>. {playerInfo.Value.Name} : {playerInfo.Value.explosion.AllExplosionUsed}\n";
                            top++;
                        }
                        langmsg = "STAT_TOP_FIVE_EXPLOSION";
                        break;
                    }
                case 3:
                    {
                        var playerList = Players.Where(x => !_config.settings.ignoreList.Contains(x.Key)).OrderByDescending(x => x.Value.playedTime.PlayedForWipe).Take(5);
                        foreach (var playerInfo in playerList)
                        {
                            playerstat += $"<color=#faec84>{top}</color>. {playerInfo.Value.Name} : {TimeHelper.FormatTime(TimeSpan.FromMinutes(playerInfo.Value.playedTime.PlayedForWipe), 5, lang.GetServerLanguage())}\n";
                            top++;
                        }
                        langmsg = "STAT_TOP_FIVE_TIMEPLAYED";
                        break;
                    }
                case 4:
                    {
                        var playerList = Players.Where(x => !_config.settings.ignoreList.Contains(x.Key)).OrderByDescending(x => x.Value.otherStat.BuildingCrate).Take(5);
                        foreach (var playerInfo in playerList)
                        {
                            playerstat += $"<color=#faec84>{top}</color>. {playerInfo.Value.Name} : {playerInfo.Value.otherStat.BuildingCrate}\n";
                            top++;
                        }
                        langmsg = "STAT_TOP_FIVE_BUILDINGS";
                        break;
                    }
                case 5:
                    {
                        var playerList = Players.Where(x => !_config.settings.ignoreList.Contains(x.Key)).OrderByDescending(x => x.Value.Score).Take(5);
                        foreach (var playerInfo in playerList)
                        {
                            playerstat += $"<color=#faec84>{top}</color>. {playerInfo.Value.Name} : {playerInfo.Value.Score:0.0}\n";
                            top++;
                        }
                        langmsg = "STAT_TOP_FIVE_SCORE";
                        break;
                    }
                case 6:
                    {
                        var playerList = Players.Where(x => !_config.settings.ignoreList.Contains(x.Key)).OrderByDescending(x => x.Value.harvesting.AllHarvesting).Take(5);
                        foreach (var playerInfo in playerList)
                        {
                            playerstat += $"<color=#faec84>{top}</color>. {playerInfo.Value.Name} : {playerInfo.Value.harvesting.AllHarvesting}\n";
                            top++;
                        }
                        langmsg = "STAT_TOP_FIVE_FERMER";
                        break;
                    }
            }
            return new Tuple<string, string>(langmsg, playerstat);
        }
        private void OnWeaponFired(BaseProjectile projectile, BasePlayer player)
        {
            if (projectile == null || player == null)
                return;
            ExplosionProgressAdd(player, null, projectile.primaryMagazine?.ammoType?.shortname);
        }
        public const string UI_TOP_TEN_USER = "TOP_TEN_USER";

        private void OnPluginLoaded(Plugin plugin)
        {
            NextTick(() =>
            {
                foreach (string hook in _gatherHooks)
                {
                    Unsubscribe(hook);
                    Subscribe(hook);
                }
            });
        }
        private Dictionary<string, string> _prefabNameItem = new Dictionary<string, string>()
        {
            ["40mm_grenade_he"] = "multiplegrenadelauncher",
            ["grenade.beancan.deployed"] = "grenade.beancan",
            ["grenade.f1.deployed"] = "grenade.f1",
            ["explosive.satchel.deployed"] = "explosive.satchel",
            ["explosive.timed.deployed"] = "explosive.timed",
            ["rocket_basic"] = "ammo.rocket.basic",
            ["rocket_hv"] = "ammo.rocket.hv",
            ["rocket_fire"] = "ammo.rocket.fire",
            ["survey_charge.deployed"] = "surveycharge"
        };


        private Configuration _config;
        private class ItemDisplayName
        {
            public String ru;
            public String en;
        }

        private void OtherStatUser(BasePlayer player, ulong target = 0, int statType = 0)
        {
            var container = new CuiElementContainer();
            PlayerInfo statInfo = PlayerInfo.Find(target == 0 ? player.userID : target);
            if (statType == 0)
            {
                container.Add(new CuiElement
                {
                    Name = "CRATE_STAT",
                    Parent = UI_USER_STAT_INFO,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("8") },
                    new CuiRectTransformComponent { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-210.146 181.391", OffsetMax = "-13.974 227.429" }
                }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "48.359 -7.01", OffsetMax = "176.509 8.4" },
                    Text = { Text = GetLang("STAT_UI_CRATE_OPEN", player.UserIDString, statInfo.otherStat.CrateOpen), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, "CRATE_STAT");

                container.Add(new CuiElement
                {
                    Parent = "CRATE_STAT",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("13") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-88.5 -16.5", OffsetMax = "-55.5 16.5" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "BARREL_STAT",
                    Parent = UI_USER_STAT_INFO,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("8") },
                    new CuiRectTransformComponent { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-210.146 129.171", OffsetMax = "-13.974 175.209" }
                }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "48.359 -7.01", OffsetMax = "176.509 8.4" },
                    Text = { Text = GetLang("STAT_UI_BARREL_KILL", player.UserIDString, statInfo.otherStat.BarrelDeath), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, "BARREL_STAT");

                container.Add(new CuiElement
                {
                    Parent = "BARREL_STAT",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1",  Png = ImageUi.GetImage("12") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-88.5 -16.5", OffsetMax = "-55.5 16.5" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "ANIMALKILL_STAT",
                    Parent = UI_USER_STAT_INFO,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("8") },
                    new CuiRectTransformComponent { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-210.146 76.951", OffsetMax = "-13.974 122.989" }
                }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "48.359 -7.01", OffsetMax = "176.509 8.4" },
                    Text = { Text = GetLang("STAT_UI_ANIMAL_KILL", player.UserIDString, statInfo.otherStat.AnimalsKill), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, "ANIMALKILL_STAT");

                container.Add(new CuiElement
                {
                    Parent = "ANIMALKILL_STAT",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("17") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-88.5 -16.5", OffsetMax = "-55.5 16.5" }
                }
                });
            }
            else
            {
                container.Add(new CuiElement
                {
                    Name = "Heli_STAT",
                    Parent = UI_USER_STAT_INFO,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("8") },
                    new CuiRectTransformComponent { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-210.146 181.391", OffsetMax = "-13.974 227.429" }
                }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "48.359 -7.01", OffsetMax = "176.509 8.4" },
                    Text = { Text = GetLang("STAT_UI_HELI_KILL", player.UserIDString, statInfo.pVP.HeliKill), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, "Heli_STAT");

                container.Add(new CuiElement
                {
                    Parent = "Heli_STAT",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("15") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-88.5 -16.5", OffsetMax = "-55.5 16.5" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "BRADLEY_STAT",
                    Parent = UI_USER_STAT_INFO,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("8") },
                    new CuiRectTransformComponent { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-210.146 129.171", OffsetMax = "-13.974 175.209" }
                }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "48.359 -7.01", OffsetMax = "176.509 8.4" },
                    Text = { Text = GetLang("STAT_UI_BRADLEY_KILL", player.UserIDString, statInfo.pVP.BradleyKill), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, "BRADLEY_STAT");

                container.Add(new CuiElement
                {
                    Parent = "BRADLEY_STAT",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1",  Png = ImageUi.GetImage("16") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-88.5 -16.5", OffsetMax = "-55.5 16.5" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "NPCKILL_STAT",
                    Parent = UI_USER_STAT_INFO,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("8") },
                    new CuiRectTransformComponent { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-210.146 76.951", OffsetMax = "-13.974 122.989" }
                }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "48.359 -7.01", OffsetMax = "176.509 8.4" },
                    Text = { Text = GetLang("STAT_UI_NPC_KILL", player.UserIDString, statInfo.pVP.KillsNpc), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, "NPCKILL_STAT");

                container.Add(new CuiElement
                {
                    Name = "NPC_IMG_USER",
                    Parent = "NPCKILL_STAT",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("11") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-88.5 -16.5", OffsetMax = "-55.5 16.5" }
                }
                });
            }

            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0.1", Command = $"UI_HandlerStat ShowMoreStat {target} {statType}" },
                Text = { Text = GetLang("STAT_UI_BTN_MORE", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "0.8 0.8 0.8 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "186.97 -254.682", OffsetMax = "383.15 -233.6" }
            }, UI_USER_STAT_INFO, "SHOW_MORE_STAT");

            CuiHelper.DestroyUi(player, "SHOW_MORE_STAT");
            CuiHelper.DestroyUi(player, "CRATE_STAT");
            CuiHelper.DestroyUi(player, "BARREL_STAT");
            CuiHelper.DestroyUi(player, "NPCKILL_STAT");
            CuiHelper.DestroyUi(player, "Heli_STAT");
            CuiHelper.DestroyUi(player, "BRADLEY_STAT");
            CuiHelper.DestroyUi(player, "ANIMALKILL_STAT");
            CuiHelper.AddUi(player, container);
        }

        private class ImageUi
        {
            public static Dictionary<string, string> Images = new Dictionary<string, string>();

            public static void DownloadImages()
            {
                for (int i = 1; i < 21; i++)
                {
                    ServerMgr.Instance.StartCoroutine(AddImage(KeyImages[i.ToString()], i.ToString()));
                }
            }

            private static IEnumerator AddImage(string url, string name)
            {
                UnityWebRequest www = UnityWebRequest.Get(url);

                yield return www.SendWebRequest();
                if (_ == null)
                    yield break;

                if (www.isNetworkError || www.isHttpError)
                {
                    _.PrintWarning(string.Format("Image download error! Error: {0}, Image name: {1}", www.error, name));
                    www.Dispose();

                    yield break;
                }

                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(www.downloadHandler.data);
                if (texture != null)
                {
                    byte[] bytes = texture.EncodeToPNG();

                    var image = FileStorage.server.Store(bytes, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString();
                    if (!Images.ContainsKey(name))
                        Images.Add(name, image);
                    else
                        Images[name] = image;
                    UnityEngine.Object.DestroyImmediate(texture);
                }

                www.Dispose();
                yield break;
            }

            public static string GetImage(String ImgKey)
            {
                if (Images.ContainsKey(ImgKey))
                    return Images[ImgKey];
                return _.GetImage("LOADING");
            }

            public static void Unload()
            {
                foreach (var item in Images)
                    FileStorage.server.RemoveExact(uint.Parse(item.Value), FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID, 0U);
            }
        }


        private static XDStatistics _;
    }
}

