using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
namespace Oxide.Plugins
{
    [Info("Duel", "RustPlugin.ru", "4.2.62")]
    [Description("Automatic Duel (Bets) with GUI, weapons list, auto-created arenas, save players loot and position")]
    class Duel : RustPlugin
    {
        #region Messages&Config

        string notAllowed = "Вызов на дуэль запрещен";
        string duelCommand = "<color=orange>/duel <ник></color> - вызвать игрока на дуэль\n<color=orange><color=#ADFF2F>/duels</color> <ник></color> - <color=#ADFF2F>вызвать игрока на дуэль со ставками</color>\n<color=orange>/duel a</color> - принять вызов\n<color=orange>/duel c</color> - отменить вызов\n<color=orange>/duel stat</color> - статистика\n<color=orange>/duel top</color> - топ\nКомандная дуэль:\n<color=orange>/duel create [2-6]</color> - создать командную дуэль\n<color=orange>/duel join red/blue</color> - подать заявку\n<color=orange>/duel accept</color> - принять в дуэль (для создателя)\n<color=orange>/duel c</color> - покинуть дуэль";
        string dontHaveRequest = "У вас нет заявок на дуэль";
        string notFoundPlayer = "Игрок с именем <color=#7b9ef6>{0}</color> не найден";
        string alreadyHaveDuel = "<color=#7b9ef6>{0}</color> уже имеет активную дуэль";
        string youOnDuel = "Вы уже имеете активную дуэль";
        string noArenas = "Извините, все арены сейчас заняты";
        string noBuildAcess = "Вы должны быть авторизованы в шкафу";
        string cooldownMessage = "Вы сможете вызвать на дуэль через {0} секунд";
        string createRequest = "Вы вызвали <color=#7b9ef6>{0}</color> на дуэль!\n<color=orange>/duel c</color> - отменить вызов";
        string receiveRequest = "<color=#7b9ef6>{0}</color> вызвал вас на дуэль!\n<color=orange>/duel a</color> - принять вызов\n15 секунд до отмены\n<color=orange>/duel c</color> - отменить вызов";
        string youDontHaveRequest = "У вас нет активных вызовов";
        string cantCancelDuel = "Невозможно отменить начавшуюся дуэль.\nПобеди своего противника!";
        static string duelHasBeenCancelled = "Дуэль c <color=#7b9ef6>{0}</color> отменена.";
        static string duelStart = "Дуэль началась!";
        static string playerNotFound = "Игрок с именем {0} не найден";
        static string foundMultiplePlayers = "Найдено несколько игроков: <color=#7b9ef6>{0}</color>";

        static string guiChooseWeapon = "Выберите оружие из списка";
        static string guiYourChoose = "Вы выбрали: {0}";
        static string guiWaitForOpponentChoose = "Противник выбирает оружие";
        static string guiOpponentsWeapon = "Противник выбрал: {0}";
        static string guiStartAboutToBegin = "Начало через несколько секунд";
        static string guiSurrenderButton = "Сдаться";
        static string guiAutoCloseSec = "До выбора случайного оружия: {0}";
        static string guiPlayerSleep = "Ожидаем пока соперник проснётся";

        string statLoss = "Тебе засчитано поражение в дуэли";
        string statWin = "Тебе засчитана победа в дуэли";
        string notificationAboutWin = "[Дуэль] <color=#7b9ef6>{0}</color> vs <color=#7b9ef6>{1}</color>\nПобедитель: <color=#7b9ef6>{2}</color>";
        string cantBuild = "Ты не можешь строить на дуэли";
        string cantUseRecycle = "Ты не можешь использовать переработчик на дуэли";
        string cantUseNexusKits = "Ты не можешь использовать киты на дуэли";
        string cantTrade = "Ты не можешь обмениваться на дуэли";
        string cantRemove = "Ты не можешь ремувать на дуэли";
        string cantTp = "Ты не можешь пользоваться телепортом на дуэли";
        string cantUseKit = "Ты не можешь получить кит на дуэли";
        string cantUseCommand = "Вы не можете использовать /{0} на Duel";
        string cantUseBackPack = "Ты не можешь использовать рюкзак на дуэли";
        string cantUseSkins = "Ты не можешь использовать скины на дуэли";
        string cantUseKill = "Ты не можешь использовать kill на дуэли";
        string yourStat = "Ваша статистика по дуэлям:\nПобед: {0}\nПоражений: {1}\nКомандные дуэли:\nПобед:{2}\nПоражений:{3}";
        string emptyTop = "Статистика пуста как твоя кровать по ночам";
        string topWin = "Топ побед в дуэлях:";
        string topTeamWin = "\n\nТоп побед в командных дуэлях:";
        string topLosses = "\n\nТоп поражений в дуэлях:";
        string topTeamLoss = "\n\nТоп поражений в командных дуэлях:";
        string playerInTop = "\n{0}. <color=#469cd0>{1}</color>: {2}"; // номер. ник: значение

        static string returnPlayerReason = "Дуэль окончена.\nПричина: <color=orange>{0}</color>";
        static string returnReasonSleep = "Кто-то слишком долго спал";
        static string returnReasonGUIFail = "Кто-то слишком долго выбирает оружие";
        static string returnReasonLimitTime = "Время на дуэль вышло({0} секунд)";
        static string returnReasonDisconnect = "Соперник отключился";
        static string returnReasonSurrender = "Кто-то всё же решил сдаться";
        static string returnReasonUnload = "Плагин на время отключен. Попробуйте позже.";
        static string teamDuelCancelled = "Командная дуэль отменена\nПричина: не собрана за {0} сек";

        static string teamPlayerDisconnect = "[Дуэль] <color=#7b9ef6>{0}</color> вышел с сервера!";
        static string teamWinRed = "[Дуэль] Побеждает команда <color=red>RED</color>!";
        static string teamWinBlue = "[Дуэль] Побеждает команда <color=blue>BLUE</color>!";
        static string teamDuellerWounded = "[Дуэль] Один из дуэлянтов ранен.\nДуэль не начнётся, пока он ранен.";
        static string teamArensBusy = "[Дуэль] Пожалуйста, подождите. Все арены заняты.";
        static string teamCooldownToCreate = "Вы сможете создать командную дуэль через {0} сек";
        static string teamAlreadyCreated = "Извините, но командная дуэль уже создана. Присоединиться: /duel join red/blue";
        static string teamCreatedPermPref = "<color=yellow> Турнирную </color>";
        static string teamSucessCreated = "<color=#409ccd>{0}</color> создал{1}командную дуэль <color=#36978e>{2}</color> на <color=#36978e>{2}</color>!\nПодать заявку на участие в команде <color=red>RED</color>: /duel join red\nПодать заявку на участие в команде <color=blue>BLUE</color>: /duel join blue";
        static string teamCancelDuel = "Отменить дуэль: <color=orange>/duel c</color>";
        static string teamNotOwner = "Ты не создатель дуэли";
        static string teamNoSlotsBlue = "Свободных мест в команде blue нет";
        static string teamNoSlotsRed = "Свободных мест в команде red нет";
        static string teamJoinPermPref = "<color=yellow> Турнирная </color>";
        static string teamJoinRedPref = "<color=red>red</color>";
        static string teamJoinBluePref = "<color=blue>blue</color>";
        static string teamAboutToBegin = "[Дуэль] Начало через 5 секунд!";
        static string teamJoinAboutToBeginAnnounce = "[{0}Командная дуэль]\n<color=#7b9ef6>{1}</color> присоединился к команде {2}\nНабор окончен!\nДуэль скоро начнется";
        static string teamJoinAnnounce = "[{0}Командная дуэль]\n<color=#7b9ef6>{1}</color> присоединился к команде {2}\nСвободных мест:\n<color=red>red</color>: {3}\n<color=blue>blue</color>: {4}\nПодать заявку: <color=orange>/duel join red</color> или <color=orange>blue</color>";
        static string teamPlayerWont = "{0} не подавал заявку на дуэль";
        static string teamErrorNoCommand = "Ошибка. Выберите команду: /duel join red / blue";
        static string teamAlreadyRequest = "Вы уже подали заявку на командную дуэль. Ждите одобрения создателя";
        static string teamAlreadyStarted = "Ошибка. Дуэль уже началась!";
        static string teamNoPerm = "Извините, у вас нет доступа к турнирным дуэлям.\nПриобрести можно в магазине сервера.";
        static string teamSucessRequest = "Вы подали заявку.\nОжидайте, пока <color=#409ccd>{0}</color> одобрит её.\n<color=orange>/duel c</color> - отменить заявку.";
        static string teamNewRequest = "<color=#409ccd>{0}</color> подал заявку на вступление в дуэль[<color={1}>{1}</color>].\n<color=orange>/duel accept</color> <color=#409ccd>{0}</color> - принять\nСписок подавших заявку: <color=orange>/duel accept</color>";
        static string teamNoDuelsHowCreate = "Активных командных дуэлей нет. /duel create - создать новую";
        static string teamGuiWeapons = "Оружие дуэлянтов: ";
        static string teamGuiNoWeapon = "не выбрал";
        static string teamGuiBluePlayerColor = "#76b9d6";
        static string teamGuiRedPlayerColor = "red";
        static string teamGuiWeaponColor = "#e0e1e3";
        static string teamGuiWaiting = "Ожидаем других игроков\n60 секунд максимум";
        static string teamDamageTeammate = "<color=#7b9ef6>{0}</color>: Эй! Я твой союзник!";
        static string teamDeath = "[Дуэль] <color=#7b9ef6>{0}</color> [<color={1}>{1}</color>] погиб!\n<color=blue>Team Blue</color>: {2} человек\n<color=red>Team Red</color>: {3} человек";

        static float teamDuelRequestSecToClose = 300;
        float cooldownTeamDuelCreate = 180f;
        float cooldownRequestSec = 60;
        static float requestSecToClose = 20;

        static float duelMaxSec = 300;
        static float chooseWeaponMaxSec = 25f;
        static float teamChooseWeaponMaxSec = 60f;
        int maxWinsTop = 5;
        int maxLoseTop = 5;

        static bool debug = true;

        #endregion

        #region Variables
        [PluginReference]
        Plugin Trade;
        static string duelJoinPermission = "duel.join";
        static string duelCreatePermission = "duel.create";
        private readonly int triggerLayer = LayerMask.GetMask("Trigger");
        bool isIni = false;
        static List<ActiveDuel> createdDuels = new List<ActiveDuel>();
        static List<TeamDuel> createdTeamDuels = new List<TeamDuel>();
        static List<ulong?> toRemoveCorpse = new List<ulong?>();
        Dictionary<ulong, float> lastRequestTime = new Dictionary<ulong, float>();
        Dictionary<ulong, float> lastTeamDuelCreateTime = new Dictionary<ulong, float>();
        static Dictionary<string, ulong> Wears = new Dictionary<string, ulong> //item shortname : skinid
        {
            {"metal.facemask", 0}, //shirt 
            {"metal.plate.torso", 0}, // Hat
            {"shoes.boots", 0}, // Pants
            {"hoodie", 0}, // Boots
			{"pants", 0}, // tolsovka
            {"roadsign.kilt", 0} // dorognie znaki
        };
        static Dictionary<string, ulong> WearsBlue = new Dictionary<string, ulong> //item id : skinid
        {
            {"metal.facemask", 0}, //hoodie
            {"metal.plate.torso", 0}, // Hat
            {"shoes.boots", 0}, // Pants
            {"hoodie", 0}, // Boots
			{"pants", 14178}, // tolsovka
            {"roadsign.kilt", 0} // dorognie znaki
        };
        static Dictionary<string, ulong> WearsRed = new Dictionary<string, ulong> //item id : skinid
        {
            {"metal.facemask", 0}, //hoodie 
            {"metal.plate.torso", 0}, // Hat
            {"shoes.boots", 0}, // Pants
            {"hoodie", 0}, // Boots
			{"pants", 0}, // tolsovka
            {"roadsign.kilt", 0} // dorognie znaki
        };
        private List<BaseEntity> ArenaEntities = new List<BaseEntity>();
        #endregion

        #region Helperss
        [PluginReference]
        Plugin NoEscape;

        bool IsRaidBlock(BasePlayer player)
        {
            if (plugins.Exists("NoEscape"))
            {
                var block = (bool)NoEscape?.Call("IsRaidBlocked", player);
                if (block)
                {
                    SendReply(player, "У Вас рейдблок, Duel запрещена");
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region ChatCommand
        [ChatCommand("duel")]
        void chatduel(BasePlayer player, string command, string[] arg)
        {
            if (!isIni)
            {
                SendReply(player, "<color=#FE5256>Duel не инициирована. Ожидайте........ </color>");
                return;
            }
            if (arg.Length == 0)
            {
                SendReply(player, duelCommand);
                return;
            }
            if (IsRaidBlock(player)) return;
            if (player.metabolism.radiation_poison.value > 5)
            {
                SendReply(player, "У Вас облучение радиацией. Duel запрещена");
                return;
            }
            string aim = (string)arg[0];

            if (aim == "join")
            {
                if (!canAcceptRequest(player)) return;
                if (IsDuelPlayer(player))
                {
                    player.ChatMessage("Вы уже дуэлянт");
                    return;
                }
                if (arg.Length == 2)
                {
                    string team = (string)arg[1];
                    if (team != null)
                        JoinTeamDuel(player, team);
                    return;
                }
                else
                {
                    if (createdTeamDuels.Count > 0)
                    {
                        var teamDuel = createdTeamDuels[0];
                        player.ChatMessage($"Ошибка. Укажите команду, к которой хотите присоединиться\nВ команде red: {teamDuel.playersAmount - teamDuel.teamred.Count} свобоных мест\nВ команде blue: {teamDuel.playersAmount - teamDuel.teamblue.Count} свобоных мест");
                        return;
                    }
                    else
                    {
                        player.ChatMessage("Ошибка. Пишите:\n/duel join red - присоединиться к команде red\n/duel join blue - присоединиться к команде blue");
                        return;
                    }

                }
            }

            if (aim == "accept")
            {
                if (arg.Length == 2)
                {
                    string requester = (string)arg[1];
                    var target = FindPlayersSingle(requester, player);
                    if (target != null)
                        AcceptRequestTeamDuel(player, target);
                    return;
                }
                else
                {
                    if (createdTeamDuels.Count > 0)
                    {
                        string msg = "";
                        var teamDuel = createdTeamDuels[0];
                        if (teamDuel.owner != player)
                        {
                            player.ChatMessage("Ты не создатель дуэли");
                            return;
                        }
                        if (teamDuel.requestPlayers.Count > 0)
                        {
                            foreach (var pl in teamDuel.requestPlayers)
                            {
                                msg += $"<color=#409ccd>{pl.Key.displayName}</color>, ";
                            }
                            player.ChatMessage($"Игроки, подавшие заявку: {msg}\nПринять: <color=orange>/duel accept</color> <color=#409ccd>name</color>");
                            return;
                        }
                        else
                        {
                            player.ChatMessage("Список с заявками пуст.");
                            return;
                        }
                    }
                    else
                    {
                        player.ChatMessage("Командная дуэль не создана");
                        return;
                    }
                }
            }
            if (aim == "create")
            {
                if (!CanCreateDuel(player, true)) return;
                if (arg.Length >= 2)
                {
                    string amount = (string)arg[1];
                    int intamount = 0;
                    if (Int32.TryParse(amount, out intamount))
                    {
                        if (intamount > 1 && intamount <= 6)
                        {
                            if (arg.Length == 3)
                            {
                                string perm = (string)arg[2];
                                if (perm == "perm")
                                {
                                    if (!HavePerm(duelCreatePermission, player.userID)) return;
                                    createTeamDuel(player, intamount, true);
                                    return;
                                }
                            }
                            createTeamDuel(player, intamount);
                            return;
                        }
                        else
                        {
                            player.ChatMessage("Ошибка. Количество игроков в команде должно быть от 2 до 6");
                            return;
                        }
                    }
                    return;
                }
                else
                {
                    player.ChatMessage("Ошибка. Укажите количество участников (от 2 до 6) в каждой команде\n/duel create [2-6]");
                    return;
                }
            }
            if (aim == "top")
            {
                showTop(player);
                return;
            }
            if (aim == "stat")
            {
                showStat(player);
                return;
            }
            if (aim == "c")
            {
                CancelRequest(player);
                return;
            }

            if (aim == "a")
            {
                if (createdTeamDuels.Count > 0)
                {
                    var teamDuel = createdTeamDuels[0];
                    if (teamDuel.requestPlayers.ContainsKey(player))
                    {
                        player.ChatMessage("Ты подал заявку на командную дуэль.\nНачать новую ты не можешь");
                        return;
                    }
                }
                if (!canAcceptRequest(player)) return;
                if (!IsDuelPlayer(player))
                {
                    SendReply(player, dontHaveRequest);
                    return;
                }
                if (IsPlayerOnActiveDuel(player))
                {
                    player.ChatMessage("Вы уже находитесь на дуэли.");
                    return;
                }
                AcceptRequest(player);
                return;
            }
            var victim = FindPlayersSingle(aim, player);
            if (victim != null)
            {
                if (createdTeamDuels.Count > 0)
                {
                    var teamDuel = createdTeamDuels[0];
                    if (teamDuel.requestPlayers.ContainsKey(player))
                    {
                        player.ChatMessage("Ты подал заявку на командную дуэль.\nНачать новую ты не можешь");
                        return;
                    }
                }
                if (createdTeamDuels.Count > 0)
                {
                    var teamDuel = createdTeamDuels[0];
                    if (teamDuel.requestPlayers.ContainsKey(victim))
                    {
                        player.ChatMessage("Невозможно вызвать этого игрока");
                        return;
                    }
                }
                if (createdTeamDuels.Count > 0)
                {
                    var teamDuel = createdTeamDuels[0];
                    if (teamDuel.owner == victim)
                    {
                        player.ChatMessage("Невозможно вызвать этого игрока");
                        return;
                    }
                }
                if (victim == player)
                {
                    player.ChatMessage("Вы не можете вызвать самого себя на дуэль");
                    return;
                }
                if (!CanCreateDuel(player)) return;
                string reason = CanDuel(victim);
                if (reason != null)
                {
                    SendReply(player, reason);
                    return;
                }

                CreateRequest(player, victim);
                return;
            }
        }

        #endregion

        #region Checks
        bool IsArenaZone(Vector3 pos)
        {
            foreach (var arena in arenaList)
            {
                if (Vector3.Distance(pos, arena.pos) < 100)
                    return true;
            }
            return false;
        }
        void RemoveGarbage(BaseEntity entity)
        {
            if (entity == null) return;
            if (entity?.net?.ID == null) return;
            if (!isIni) return;
            if (entity.transform.position.y > 450f && IsArenaZone(entity.transform.position))
            {
                var cont = entity as DroppedItemContainer;
                if (cont != null)
                {
                    cont.ResetRemovalTime(0.1f);
                }
                var corpse = entity as BaseCorpse;
                if (corpse != null)
                {
                    if (toRemoveCorpse.Count == 0) return;
                    if (corpse)
                    {
                        if ((corpse is PlayerCorpse) && corpse?.parentEnt?.ToPlayer())
                        {
                            if (corpse?.parentEnt?.ToPlayer() != null)
                            {
                                if (toRemoveCorpse.Contains(corpse?.parentEnt?.ToPlayer().userID))
                                {
                                    corpse.ResetRemovalTime(0.1f);
                                    toRemoveCorpse.Remove(corpse?.parentEnt?.ToPlayer().userID);
                                    return;
                                }
                            }
                        }
                    }
                }
                if (entity is PlayerCorpse || entity.name.Contains("item_drop_backpack"))
                {
                    NextTick(() =>
                    {
                        if (entity != null && !entity.IsDestroyed)
                        {

                            entity.Kill();
                        }
                    });
                }
                if (entity is WorldItem)
                {
                    if ((entity as WorldItem).item.GetOwnerPlayer() == null) return;
                    var activeDuel = PlayersActiveDuel((entity as WorldItem).item.GetOwnerPlayer().userID);
                    if (activeDuel != null)
                    {
                        activeDuel.dropedWeapons.Add((entity as WorldItem).item);
                        return;
                    }
                    if (NeedToRemoveFromTeamDuel((entity as WorldItem).item.GetOwnerPlayer().userID))
                    {
                        createdTeamDuels[0].droppedWeapons.Add((entity as WorldItem).item);
                    }
                }
            }
        }

        bool NeedToRemoveFromTeamDuel(ulong? userid)
        {
            if (createdTeamDuels.Count > 0)
            {
                if (createdTeamDuels[0].allPlayers.Find(x => x.player.userID == userid))
                    return true;
            }
            return false;
        }

        ActiveDuel PlayersActiveDuel(ulong? userid)
        {
            if (createdDuels.Count == 0) return null;
            foreach (var duel in createdDuels)
            {
                if (duel.player1.player.userID == userid && duel.player1.haveweapon)
                {
                    return duel;
                }
                if (duel.player2.player.userID == userid && duel.player2.haveweapon)
                {
                    return duel;
                }
            }
            return null;
        }

        bool NeedToRemoveGarbage(ulong? userid)
        {
            int createdDuelsN = createdDuels.Count;
            if (createdDuelsN == 0) return false;
            if (createdDuels.Find(x => x.player1.player.userID == userid || x.player2.player.userID == userid)) return true;
            return false;
        }

        bool IsDuelPlayer(BasePlayer player)
        {
            DuelPlayer dueller = player?.GetComponent<DuelPlayer>();
            if (dueller == null) return false;
            return true;
        }

        bool IsPlayerOnActiveDuel(BasePlayer player)
        {
            DuelPlayer dueller = player?.GetComponent<DuelPlayer>();
            if (dueller == null) return false;
            if (!dueller.canDoSomeThings) return true;
            return false;
        }

        string CanDuel(BasePlayer player)
        {
            if (IsDuelPlayer(player))
            {
                return String.Format(alreadyHaveDuel, player.displayName);
            }
            if (busyArena.Count == arenaList.Count)
            {
                return noArenas;
            }
            return null;
        }

        bool canAcceptRequest(BasePlayer player)
        {
            if (player.IsDead())
            {
                return false;
            }
            if (player.IsWounded())
            {
                SendReply(player, notAllowed);
                return false;
            }
            if (busyArena.Count == arenaList.Count)
            {
                SendReply(player, noArenas);
                return false;
            }
            return true;
        }

        bool CanCreateDuel(BasePlayer player, bool isTeamDuel = false)
        {
            float value = 0;
            if (lastRequestTime.TryGetValue(player.userID, out value) && !isTeamDuel)
            {
                if (UnityEngine.Time.realtimeSinceStartup - value < cooldownRequestSec)
                {
                    float when = cooldownRequestSec - (UnityEngine.Time.realtimeSinceStartup - value);
                    player.ChatMessage(String.Format(cooldownMessage, (int)when));
                    return false;
                }
            }
            if (player.IsWounded())
            {
                SendReply(player, notAllowed);
                return false;
            }
            if (IsDuelPlayer(player))
            {
                SendReply(player, youOnDuel);
                return false;
            }
            if (busyArena.Count == arenaList.Count)
            {
                SendReply(player, noArenas);
                return false;
            }
            if (!isTeamDuel)
                lastRequestTime[player.userID] = UnityEngine.Time.realtimeSinceStartup;
            return true;
        }
        #endregion

        #region DuelFunctions
        void CreateRequest(BasePlayer starter, BasePlayer opponent)
        {
            SendReply(starter, String.Format(createRequest, opponent.displayName));
            SendReply(opponent, String.Format(receiveRequest, starter.displayName));
            DuelPlayer dueller1 = starter.GetComponent<DuelPlayer>() ?? starter.gameObject.AddComponent<DuelPlayer>();
            DuelPlayer dueller2 = opponent.GetComponent<DuelPlayer>() ?? opponent.gameObject.AddComponent<DuelPlayer>();
            ActiveDuel activeDuel = starter.gameObject.AddComponent<ActiveDuel>();
            activeDuel.player1 = dueller1;
            activeDuel.player2 = dueller2;
            createdDuels.Add(activeDuel);
        }

        void AcceptRequest(BasePlayer player)
        {
            Arena arena = null;
            foreach (var duel in createdDuels)
            {
                if (duel.player2.player == player)
                {
                    if (!canAcceptRequest(player) || !canAcceptRequest(duel.player1.player))
                    {
                        CancelRequest(player);
                        return;
                    }
                    duel.arena = FreeArena();
                    arena = duel.arena;
                    duel.isRequest = false;
                    duel.timeWhenTp = UnityEngine.Time.realtimeSinceStartup;
                    duel.player1.spawnPos = arena.player1pos;
                    duel.player2.spawnPos = arena.player2pos;
                    Debug($"Началась Дуэль {duel.player1.player.displayName} : {duel.player2.player.displayName} {arena.name} Активных: {busyArena.Count}");
                    toRemoveCorpse.Add(duel.player1.player.userID);
                    toRemoveCorpse.Add(duel.player2.player.userID);
                    duel.player1.PrepairToDuel();
                    duel.player2.PrepairToDuel();
                    Trade?.CallHook("RemovePending", duel.player1.player);
                    Trade?.CallHook("RemovePending", duel.player2.player);
                    break;
                }
            }
        }

        public void CancelRequest(BasePlayer player)
        {
            if (createdTeamDuels.Count > 0)
            {
                var duel = createdTeamDuels[0];
                if (duel.owner == player && !duel.isStarted && !duel.allHere)
                {
                    if (duel.teamblue.Count > 0)
                        foreach (var dueller in duel.teamblue)
                        {
                            dueller.Destroy();
                        }
                    if (duel.teamred.Count > 0)
                        foreach (var dueller in duel.teamred)
                        {
                            dueller.Destroy();
                        }
                    PrintToChat("Командная дуэль отменена создателем");
                    duel.Destroy();
                    return;
                }
                if (duel.requestPlayers.ContainsKey(player))
                {
                    duel.requestPlayers.Remove(player);
                    player.ChatMessage("Вы покинули командную дуэль");
                    duel.owner.ChatMessage($"{player.displayName} отменил заявку на дуэль");
                    return;
                }
                var redplayer = duel.teamred.Find(x => x.player == player);
                if (redplayer != null)
                {
                    if (!redplayer.haveweapon && !duel.isStarted && !duel.allHere)
                    {
                        duel.teamred.Remove(redplayer);
                        redplayer.Destroy();
                        duel.owner.ChatMessage($"{player.displayName} покинул командную дуэль");
                        player.ChatMessage("Вы покинули командную дуэль");
                        return;
                    }
                    else
                    {
                        player.ChatMessage("Вы не можете покинуть начавшуюся дуэль");
                        return;
                    }
                }
                var blueplayer = duel.teamblue.Find(x => x.player == player);
                if (blueplayer != null)
                {
                    if (!blueplayer.haveweapon && !duel.isStarted && !duel.allHere)
                    {
                        duel.teamblue.Remove(blueplayer);
                        blueplayer.Destroy();
                        duel.owner.ChatMessage($"{player.displayName} покинул командную дуэль");
                        player.ChatMessage("Вы покинули командную дуэль");
                        return;
                    }
                    else
                    {
                        player.ChatMessage("Вы не можете покинуть начавшуюся дуэль");
                        return;
                    }
                }
            }
            if (FindOpponent(player) != null)
            {
                var duel = FindDuelByPlayer(player);
                if (duel != null)
                {
                    if (duel.isRequest)
                    {
                        duel.RequestRemove();
                        return;
                    }
                    else
                    {
                        player.ChatMessage(cantCancelDuel);
                        return;
                    }
                }
            }
            player.ChatMessage("Вы не участник дуэли");
        }

        static ActiveDuel FindDuelByPlayer(BasePlayer player)
        {
            foreach (var duel in createdDuels)
            {
                if (duel.player1.player == player)
                {
                    return duel;
                }
                if (duel.player2.player == player)
                {
                    return duel;
                }
            }
            return null;
        }

        public void EndDuel(BasePlayer player, int reason, string UserId, string UserIdLoss)
        {
            DuelPlayer dueller = player?.GetComponent<DuelPlayer>();
            if (dueller != null)
            {
                if (dueller.team != "")
                {
                    if (reason == 6)
                    {
                        if (dueller.savedHome)
                        {
                            dueller.ReturnPlayer(reason);
                        }
                        else
                        {
                            dueller.Destroy();
                        }
                        return;
                    }

                    if (reason == 0 || reason == 4)
                    {
                        dueller.ReturnPlayer(reason);
                    }
                    return;
                }
            }
            if (createdTeamDuels.Count > 0 && reason == 4)
            {
                if (createdTeamDuels[0].owner == player)
                {
                    createdTeamDuels[0].RequestRemove();
                    return;
                }
                if (createdTeamDuels[0].requestPlayers.ContainsKey(player))
                {
                    createdTeamDuels[0].requestPlayers.Remove(player);
                    createdTeamDuels[0].owner.ChatMessage($"[Дуэль] {player.displayName} вышел с сервера!");
                    return;
                }
            }
            if (createdDuels.Count == 0) return;
            DuelPlayer player1 = null;
            DuelPlayer player2 = null;
            foreach (var duel in createdDuels)
            {
                if (duel.player1.player == player)
                {
                    player1 = duel.player1;
                    player2 = duel.player2;
                    break;
                }
                if (duel.player2.player == player)
                {
                    player1 = duel.player1;
                    player2 = duel.player2;
                    break;
                }
            }
            if (reason == 0)
            {
                if (player1 != null)
                    if (player1.player == player)
                    {
                        player1.guiEnabled = false;
                        player1.canMove = true;
                        player1.ReturnPlayer(0);
                        return;
                    }
                if (player2 != null)
                    if (player2.player == player)
                    {
                        player2.guiEnabled = false;
                        player2.canMove = true;
                        player2.ReturnPlayer(0);
                        return;
                    }
            }
            if (reason == 7)
            {
                if (player1 != null)
                    if (player1.player == player)
                    {
                        player2.guiEnabled = false;
                        player2.canMove = true;
                        if (player2.induel)
                            player2.ReturnWithCooldown();
                        player2.induel = false;
                        return;
                    }
                if (player2 != null)
                    if (player2.player == player)
                    {
                        player1.guiEnabled = false;
                        player1.canMove = true;
                        if (player1.induel)
                            player1.ReturnWithCooldown();
                        player1.induel = false;
                        return;
                    }
            }
            if (player1 != null)
            {
                player1.guiEnabled = false;
                player1.canMove = true;
                player1.ReturnPlayer(reason);
            }
            if (player2 != null)
            {
                player2.guiEnabled = false;
                player2.canMove = true;
                player2.ReturnPlayer(reason);
            }
        }
        #endregion

        #region TeamDuel

        #region Class TeamDuel

        class TeamDuel : MonoBehaviour
        {
            public List<DuelPlayer> teamblue = new List<DuelPlayer>();
            public List<DuelPlayer> teamred = new List<DuelPlayer>();
            public List<DuelPlayer> allPlayers = new List<DuelPlayer>();
            public List<BasePlayer> statTeamBlue = new List<BasePlayer>();
            public List<BasePlayer> statTeamRed = new List<BasePlayer>();
            public Dictionary<BasePlayer, string> requestPlayers = new Dictionary<BasePlayer, string>();
            public BasePlayer owner;
            public Arena arena = null;
            public bool isRequest = true;
            public bool isStarted = false;
            public bool needCheckStart;
            public bool isActive = true;
            public bool allHere;
            public bool allReady;
            public bool isPermDuel = false;
            public bool randomWeaponsHasGiven = false;

            public float guiTime;
            public int playersAmount = -1;
            public float startTime;
            public float requestTime;
            public float lastTimeMessage = 0f;
            public List<Item> droppedWeapons = new List<Item>();
            void Awake()
            {
                requestTime = UnityEngine.Time.realtimeSinceStartup;
                allHere = false;
                allReady = false;
            }

            public void CheckOnline()
            {
                int redCount = teamred.Count;
                int blueCount = teamblue.Count;
                List<string> offPlayers = new List<string>();
                if (redCount > 0)
                {
                    for (int i = 0; i < redCount; i++)
                    {
                        if (teamred[i] == null)
                        {
                            offPlayers.Add(teamred[i].player.displayName);
                            allPlayers.Remove(teamred[i]);
                            teamred.Remove(teamred[i]);
                            break;
                        }
                    }
                }
                if (blueCount > 0)
                {
                    for (int i = 0; i < blueCount; i++)
                    {
                        if (teamblue[i] == null)
                        {
                            offPlayers.Add(teamblue[i].player.displayName);
                            allPlayers.Remove(teamblue[i]);
                            teamblue.Remove(teamblue[i]);
                            break;
                        }
                    }
                }
                redCount = teamred.Count;
                blueCount = teamblue.Count;
                int offPlayersCount = offPlayers.Count;
                if (offPlayersCount > 0)
                {
                    if (blueCount > 0)
                    {
                        for (int i = 0; i < blueCount; i++)
                        {
                            for (int j = 0; j < offPlayersCount; j++)
                                teamblue[i].player.ChatMessage(String.Format(teamPlayerDisconnect, offPlayers[j]));
                        }
                    }
                    if (redCount > 0)
                    {
                        for (int i = 0; i < redCount; i++)
                        {
                            for (int j = 0; j < offPlayersCount; j++)
                                teamred[i].player.ChatMessage(String.Format(teamPlayerDisconnect, offPlayers[j]));
                        }
                    }
                }
            }

            void FixedUpdate()
            {
                if (isActive)
                {
                    CheckOnline();
                }
                if (!isStarted && !allHere && teamblue.Count > 0 && teamred.Count > 0)
                {
                    if ((teamblue.Count + teamred.Count) == (playersAmount * 2))
                    {
                        allHere = true;
                        int teamPlayersN = teamblue.Count;
                        for (int i = 0; i < teamPlayersN; i++)
                        {
                            var tmb = teamblue[i];
                            var tmr = teamred[i];
                            statTeamBlue.Add(tmb.player);
                            statTeamRed.Add(tmr.player);
                            allPlayers.Add(tmb);
                            allPlayers.Add(tmr);
                        }
                        Invoke("CheckDuellers", 5f);
                        needCheckStart = true;
                    }
                }

                if (needCheckStart)
                {
                    if (isStarted && isActive)
                    {
                        if (teamblue.Count == 0)
                        {
                            isStarted = false;
                            isActive = false;
                            ConsoleNetwork.BroadcastToAllClients("chat.add", 0, teamWinRed);
                            int statPlayersN = statTeamRed.Count;
                            for (int i = 0; i < statPlayersN; i++)
                            {
                                db.playerStat[statTeamRed[i].userID].teamwins++;
                                db.playerStat[statTeamBlue[i].userID].teamloss++;
                            }
                            Invoke("EndTeamDuelWithWinners", 5f);
                            return;
                        }
                        if (teamred.Count == 0)
                        {
                            isStarted = false;
                            isActive = false;
                            ConsoleNetwork.BroadcastToAllClients("chat.add", 0, teamWinBlue);
                            int statPlayersN = statTeamRed.Count;
                            for (int i = 0; i < statPlayersN; i++)
                            {
                                db.playerStat[statTeamBlue[i].userID].teamwins++;
                                db.playerStat[statTeamRed[i].userID].teamloss++;
                            }
                            Invoke("EndTeamDuelWithWinners", 5f);
                            return;
                        }
                        if (UnityEngine.Time.realtimeSinceStartup - startTime > duelMaxSec)
                        {
                            EndTeamDuel(3);
                            isActive = false;
                        }
                    }
                    if (allReady && !isStarted)
                    {
                        bool go = true;
                        int allPlayersCount = allPlayers.Count;
                        for (int i = 0; i < allPlayersCount; i++)
                        {
                            if (!allPlayers[i].haveweapon) go = false;
                        }
                        if (UnityEngine.Time.realtimeSinceStartup - guiTime > teamChooseWeaponMaxSec && !randomWeaponsHasGiven)
                        {
                            for (int i = 0; i < allPlayersCount; i++)
                            {
                                if (!allPlayers[i].haveweapon) GiveRandomWeapon(allPlayers[i].player);
                            }
                            randomWeaponsHasGiven = true;
                            go = true;
                        }
                        if (go)
                        {
                            startTime = UnityEngine.Time.realtimeSinceStartup;
                            isStarted = true;
                            allReady = false;
                            Invoke("StartTeamDuel", 5f);
                        }
                    }
                }
                if (isRequest && isActive)
                {
                    if (UnityEngine.Time.realtimeSinceStartup - requestTime > teamDuelRequestSecToClose)
                    {
                        RequestRemove();
                        isActive = false;
                    }
                }
            }

            public void CheckDuellers()
            {
                if (allReady) return;
                bool isWound = false;
                int allPlayersCount = allPlayers.Count;
                for (int i = 0; i < allPlayersCount; i++)
                {
                    if (allPlayers[i].player.IsWounded())
                    {
                        isWound = true;
                    }
                }
                if (isWound)
                {
                    if (lastTimeMessage != 0f && UnityEngine.Time.realtimeSinceStartup - lastTimeMessage > 10f)
                    {
                        for (int i = 0; i < allPlayersCount; i++)
                        {
                            allPlayers[i].player.ChatMessage(teamDuellerWounded);
                        }
                        lastTimeMessage = UnityEngine.Time.realtimeSinceStartup;
                        Invoke("CheckDuellers", 0.5f);
                        return;
                    }
                }
                arena = FindFreeTeamDuelArena(playersAmount);
                if (arena == null)
                {
                    if (lastTimeMessage != 0f && UnityEngine.Time.realtimeSinceStartup - lastTimeMessage > 10f)
                    {
                        for (int i = 0; i < allPlayersCount; i++)
                        {
                            allPlayers[i].player.ChatMessage(teamArensBusy);
                        }
                    }
                    lastTimeMessage = UnityEngine.Time.realtimeSinceStartup;
                    Invoke("CheckDuellers", 0.5f);
                    return;
                }
                SetSpawns();
                guiTime = UnityEngine.Time.realtimeSinceStartup;
                allReady = true;
                PrepareDuellers();
            }

            public void PrepareDuellers()
            {
                isRequest = false;
                int allPlayersN = allPlayers.Count;
                for (int i = 0; i < allPlayersN; i++)
                {
                    allPlayers[i].PrepairToDuel();
                }
            }

            public void StartTeamDuel()
            {
                int allPlayersN = allPlayers.Count;
                for (int i = 0; i < allPlayersN; i++)
                {
                    var dueller = allPlayers[i];
                    toRemoveCorpse.Add(dueller.player.userID);
                    dueller.guiEnabled = false;
                    CuiHelper.DestroyUi(dueller.player, "weaponsgui");
                    CuiHelper.DestroyUi(dueller.player, "weaponsguiteamweapons");
                    CuiHelper.DestroyUi(dueller.player, "mouse");
                    dueller.readyForBattle = true;
                    dueller.canMove = true;
                }
            }

            public void RequestRemove()
            {
                if (teamblue.Count > 0)
                {
                    foreach (DuelPlayer teamblueplayer in teamblue)
                    {
                        teamblueplayer.player.ChatMessage(String.Format(teamDuelCancelled, teamDuelRequestSecToClose));
                        teamblueplayer.Destroy();
                    }
                }
                if (teamred.Count > 0)
                {
                    foreach (DuelPlayer teamredplayer in teamred)
                    {
                        teamredplayer.player.ChatMessage(String.Format(teamDuelCancelled, teamDuelRequestSecToClose));
                        teamredplayer.Destroy();
                    }
                }
                Destroy();
            }

            public void EndTeamDuelWithWinners()
            {
                Debug($"Team Дуэль {playersAmount} * {playersAmount} от {owner.displayName} Окончена {arena.name}");
                int allPlayersN = allPlayers.Count;
                if (allPlayersN > 0)
                {
                    for (int i = 0; i < allPlayersN; i++)
                    {
                        allPlayers[i].ReturnPlayer(0);
                    }
                }
                Invoke("Destroy", 2f);
            }

            public void EndTeamDuel(int reason = 0)
            {
                Debug($"Team Дуэль {playersAmount} * {playersAmount} от {owner.displayName} Прервана {arena.name}");
                int allPlayersN = allPlayers.Count;
                if (allPlayersN > 0)
                {
                    for (int i = 0; i < allPlayersN; i++)
                    {
                        var dueller = allPlayers[i];
                        dueller.guiEnabled = false;
                        CuiHelper.DestroyUi(dueller.player, "weaponsgui");
                        CuiHelper.DestroyUi(dueller.player, "weaponsguiteamweapons");
                        CuiHelper.DestroyUi(dueller.player, "mouse");
                        dueller.ReturnPlayer(reason);
                    }
                }
                Invoke("Destroy", 2f);
            }
            public void Destroy()
            {
                int droppedWeaponsN = droppedWeapons.Count;
                if (droppedWeaponsN > 0)
                {
                    for (int i = 0; i < droppedWeaponsN; i++)
                    {
                        var item = droppedWeapons[i];
                        if (item != null) ItemManager.RemoveItem(item, 1f);
                    }
                    droppedWeapons.Clear();
                }
                busyArena.Remove(arena);
                createdTeamDuels.Remove(this);
                UnityEngine.Object.Destroy(this);
            }
        }

        #endregion

        #region TeamDuelFunctions

        public static void SetSpawns()
        {
            int i = 0;
            TeamDuel duel = createdTeamDuels[0];
            foreach (var player in duel.allPlayers)
            {
                if (player.team == "red")
                {
                    player.spawnPos = duel.arena.teamredSpawns[i];
                    i++;
                }
            }
            i = 0;
            foreach (var player in duel.allPlayers)
            {
                if (player.team == "blue")
                {
                    player.spawnPos = duel.arena.teamblueSpawns[i];
                    i++;
                }
            }
        }

        public static Arena FindFreeTeamDuelArena(int slot)
        {
            Arena randomarena = new Arena();
            List<Arena> freeArenas = new List<Arena>();
            Arena value = new Arena();
            foreach (var arena in arenaList)
            {
                if (!busyArena.Contains(arena) && arena.teamblueSpawns.Count >= slot)
                    freeArenas.Add(arena);
            }
            if (freeArenas.Count > 0)
            {
                int random = UnityEngine.Random.Range(0, freeArenas.Count);
                randomarena = freeArenas[random];
                busyArena.Add(randomarena);
                return randomarena;
            }
            return null;
        }

        void createTeamDuel(BasePlayer player, int amount, bool perm = false)
        {
            float value = 0;
            if (lastTeamDuelCreateTime.TryGetValue(player.userID, out value))
            {
                if (UnityEngine.Time.realtimeSinceStartup - value < cooldownTeamDuelCreate)
                {
                    var timetocreate = cooldownTeamDuelCreate - (UnityEngine.Time.realtimeSinceStartup - value);
                    player.ChatMessage(String.Format(teamCooldownToCreate, (int)timetocreate));
                    return;
                }
            }
            lastTeamDuelCreateTime[player.userID] = UnityEngine.Time.realtimeSinceStartup;
            if (createdTeamDuels.Count > 0)
            {
                player.ChatMessage(teamAlreadyCreated);
                return;
            }
            TeamDuel teamDuel = player.gameObject.AddComponent<TeamDuel>();
            teamDuel.owner = player;
            string ispermduel = " ";
            if (perm)
            {
                teamDuel.isPermDuel = true;
                ispermduel = teamCreatedPermPref;
            }
            teamDuel.playersAmount = amount;
            createdTeamDuels.Add(teamDuel);
            PrintToChat(String.Format(teamSucessCreated, player.displayName, ispermduel, amount));
            player.ChatMessage(teamCancelDuel);
            Debug($"Создана Team Дуэль {amount} * {amount} Создатель: {player.displayName}");
        }

        void AcceptRequestTeamDuel(BasePlayer owner, BasePlayer target)
        {
            if (createdTeamDuels.Count == 0)
            {
                owner.ChatMessage(teamNoDuelsHowCreate);
                return;
            }
            var duel = createdTeamDuels[0];
            if (duel.owner != owner)
            {
                owner.ChatMessage(teamNotOwner);
                return;
            }
            if (duel.requestPlayers.ContainsKey(target))
            {
                var team = duel.requestPlayers[target];
                if (team == "blue")
                {
                    if (duel.teamblue.Count == duel.playersAmount)
                    {
                        owner.ChatMessage(teamNoSlotsBlue);
                        return;
                    }
                    DuelPlayer duelPlayer = target.GetComponent<DuelPlayer>() ?? target.gameObject.AddComponent<DuelPlayer>();
                    duelPlayer.team = "blue";
                    duel.teamblue.Add(duelPlayer);
                }
                if (team == "red")
                {
                    if (duel.teamred.Count == duel.playersAmount)
                    {
                        owner.ChatMessage(teamNoSlotsRed);
                        return;
                    }
                    DuelPlayer duelPlayer = target.GetComponent<DuelPlayer>() ?? target.gameObject.AddComponent<DuelPlayer>();
                    duelPlayer.team = "red";
                    duel.teamred.Add(duelPlayer);
                }
                duel.requestPlayers.Remove(target);
                string where = "";
                if (team == "red") where = teamJoinRedPref;
                if (team == "blue") where = teamJoinBluePref;
                string ispermduel = "";
                if (duel.isPermDuel)
                    ispermduel = teamJoinPermPref;

                if (duel.teamblue.Count + duel.teamred.Count == duel.playersAmount * 2)
                {
                    for (int i = 0; i < duel.playersAmount; i++)
                    {
                        duel.teamblue[i].player.ChatMessage(teamAboutToBegin);
                        duel.teamred[i].player.ChatMessage(teamAboutToBegin);
                    }
                    PrintToChat(String.Format(teamJoinAboutToBeginAnnounce, ispermduel, target.displayName, where));
                    Debug($"Начинается Team Дуэль {duel.playersAmount} * {duel.playersAmount} Создатель: {duel.owner.displayName}");
                    return;
                }
                PrintToChat(String.Format(teamJoinAnnounce, ispermduel, target.displayName, where, duel.playersAmount - duel.teamred.Count, duel.playersAmount - duel.teamblue.Count));
            }
            else
            {
                owner.ChatMessage(String.Format(teamPlayerWont, target.displayName));
                return;
            }
        }

        void JoinTeamDuel(BasePlayer player, string team)
        {
            if (team != "blue" && team != "red")
            {
                player.ChatMessage(teamErrorNoCommand);
                return;
            }
            if (createdTeamDuels.Count > 0)
            {
                var teamDuel = createdTeamDuels[0];
                if (teamDuel.owner == player)
                {
                    if (team == "blue")
                    {
                        if (teamDuel.teamblue.Count == teamDuel.playersAmount)
                        {
                            player.ChatMessage(teamNoSlotsBlue);
                            return;
                        }
                        DuelPlayer duelPlayer = player.GetComponent<DuelPlayer>() ?? player.gameObject.AddComponent<DuelPlayer>();
                        duelPlayer.team = "blue";
                        teamDuel.teamblue.Add(duelPlayer);
                    }
                    if (team == "red")
                    {
                        if (teamDuel.teamred.Count == teamDuel.playersAmount)
                        {
                            player.ChatMessage(teamNoSlotsRed);
                            return;
                        }
                        DuelPlayer duelPlayer = player.GetComponent<DuelPlayer>() ?? player.gameObject.AddComponent<DuelPlayer>();
                        duelPlayer.team = "red";
                        teamDuel.teamred.Add(duelPlayer);
                    }
                    string where = "";
                    if (team == "red") where = teamJoinRedPref;
                    if (team == "blue") where = teamJoinBluePref;
                    string ispermduel = "";
                    if (teamDuel.isPermDuel)
                        ispermduel = teamJoinPermPref;
                    if (teamDuel.teamblue.Count + teamDuel.teamred.Count == teamDuel.playersAmount * 2)
                    {
                        for (int i = 0; i < teamDuel.playersAmount; i++)
                        {
                            teamDuel.teamblue[i].player.ChatMessage(teamAboutToBegin);
                            teamDuel.teamred[i].player.ChatMessage(teamAboutToBegin);
                        }
                        PrintToChat(String.Format(teamJoinAboutToBeginAnnounce, ispermduel, player.displayName, where));
                        return;
                    }
                    PrintToChat(String.Format(teamJoinAnnounce, ispermduel, player.displayName, where, teamDuel.playersAmount - teamDuel.teamred.Count, teamDuel.playersAmount - teamDuel.teamblue.Count));
                    return;
                }
                if (teamDuel.requestPlayers.ContainsKey(player))
                {
                    player.ChatMessage(teamAlreadyRequest);
                    return;
                }
                if (teamDuel.isStarted)
                {
                    player.ChatMessage(teamAlreadyStarted);
                    return;
                }
                if (teamDuel.isPermDuel)
                {
                    if (!HavePerm(duelJoinPermission, player.userID))
                    {
                        player.ChatMessage(teamNoPerm);
                        return;
                    }
                }
                if (team == "blue")
                {
                    if (teamDuel.teamblue.Count == teamDuel.playersAmount)
                    {
                        player.ChatMessage(teamNoSlotsBlue);
                        return;
                    }
                    teamDuel.requestPlayers[player] = team;
                }
                if (team == "red")
                {
                    if (teamDuel.teamred.Count == teamDuel.playersAmount)
                    {
                        player.ChatMessage(teamNoSlotsRed);
                        return;
                    }
                    teamDuel.requestPlayers[player] = team;
                }
                player.ChatMessage(String.Format(teamSucessRequest, teamDuel.owner.displayName));
                teamDuel.owner.ChatMessage(String.Format(teamNewRequest, player.displayName, team));
            }
            else
            {
                player.ChatMessage(teamNoDuelsHowCreate);
                return;
            }
        }
        #endregion

        #endregion

        #region Class ActiveDuel
        class ActiveDuel : MonoBehaviour
        {
            public DuelPlayer player1;
            public DuelPlayer player2;
            public bool isStarted;
            public bool aboutToStart;
            public bool isRequest;
            public bool isEnd;
            public bool bothReady = false;

            public float startTime;
            public float requestTime;
            public float guiTimeToRandom = 0f;
            public float timeWhenTp;

            public Arena arena = null;

            public List<Item> dropedWeapons = new List<Item>();
            void Awake()
            {
                isRequest = true;
                requestTime = UnityEngine.Time.realtimeSinceStartup;
                isStarted = false;
                aboutToStart = false;
                isEnd = false;
            }

            void Update()
            {
                float now = UnityEngine.Time.realtimeSinceStartup;
                if (isRequest)
                {
                    if (now - requestTime > requestSecToClose)
                    {
                        RequestRemove();
                        return;
                    }
                    return;
                }

                if (!bothReady && !isStarted && !isEnd)
                {
                    if (player1.isReady && player2.isReady)
                    {
                        guiTimeToRandom = now;
                        bothReady = true;
                    }
                    if (now - timeWhenTp > 60)
                    {
                        EndDuel(9);
                        isEnd = true;
                        return;
                    }
                }

                if (!aboutToStart)
                {
                    if (player1.readyForBattle && player2.readyForBattle)
                    {
                        aboutToStart = true;
                        TimerToStart();
                        return;
                    }
                }

                if (!isRequest && !isEnd)
                {
                    if (isStarted)
                    {
                        if (now - startTime > duelMaxSec)
                        {
                            isEnd = true;
                            EndDuel(3);
                            return;
                        }
                    }
                    if (!player1.player.IsConnected || !player2.player.IsConnected)
                    {
                        isEnd = true;
                        EndDuel(4);
                        return;
                    }
                    if (player1 == null || player2 == null)
                    {
                        isEnd = true;
                        EndDuel();
                        return;
                    }
                }
            }

            public void RequestRemove()
            {
                player1.player.ChatMessage(String.Format(duelHasBeenCancelled, player2.player.displayName));
                player2.player.ChatMessage(String.Format(duelHasBeenCancelled, player1.player.displayName));
                player1.Destroy();
                player2.Destroy();
                Destroy();
            }

            public void TimerToStart()
            {
                aboutToStart = true;
                Invoke("StartDuel", 5f);
            }

            public void StartDuel()
            {
                CancelInvoke("StartDuel");
                if (isStarted) return;
                startTime = UnityEngine.Time.realtimeSinceStartup;
                CuiHelper.DestroyUi(player1.player, "weaponsgui");
                CuiHelper.DestroyUi(player1.player, "mouse");
                CuiHelper.DestroyUi(player2.player, "weaponsgui");
                CuiHelper.DestroyUi(player2.player, "mouse");
                player1.guiEnabled = false;
                player2.guiEnabled = false;
                player1.readyForBattle = false;
                player2.readyForBattle = false;
                player1.canMove = true;
                player2.canMove = true;
                isStarted = true;
                player1.player.InitializeHealth(100, 100);
                player1.player.metabolism.bleeding.@value = 0;
                player2.player.InitializeHealth(100, 100);
                player2.player.metabolism.bleeding.@value = 0;
                player1.player.ChatMessage(duelStart);
                player2.player.ChatMessage(duelStart);
            }
            public void EndDuel(int reason = 0)
            {
                Debug($"Дуэль окончена {player1.player.displayName} и {player2.player.displayName} {arena.name}");
                if (player1 != null)
                {
                    if (!player1.isReturned)
                        player1.ReturnPlayer(reason);
                }
                if (player2 != null)
                {
                    if (!player2.isReturned)
                        player2.ReturnPlayer(reason);
                }
                isStarted = false;
                Destroy();
            }
            public void Destroy()
            {
                int dropedWeaponsN = dropedWeapons.Count;
                if (dropedWeaponsN > 0)
                {
                    for (int i = 0; i < dropedWeaponsN; i++)
                    {
                        var item = dropedWeapons[i];
                        if (item == null) continue;
                        ItemManager.RemoveItem(item, 1f);
                    }
                    dropedWeapons.Clear();
                }
                busyArena.Remove(arena);
                createdDuels.Remove(this);
                UnityEngine.Object.Destroy(this);
            }
        }
        #endregion

        #region Class DuelPlayer
        class DuelPlayer : MonoBehaviour
        {
            public BasePlayer player;

            public float health;
            public float calories;
            public float hydration;
            public float readyTime = 0f;

            public bool savedInventory;
            public bool savedHome;
            public bool canMove = true;
            public bool guiEnabled;
            public bool guiMouseEnabled;
            public bool haveweapon;
            public bool induel = true;
            public bool readyForBattle = false;
            public bool canDoSomeThings;
            public bool isDeath;
            public bool isTeamDuel;
            public bool isReturned = false;
            public bool isReady = false;

            public string currentClass;
            public string weapon = "";
            public string team = "";

            public Vector3 Home;
            public Vector3 spawnPos;

            //public List<ItemsToRestore> InvItems = new List<ItemsToRestore>();

            void Awake()
            {
                isDeath = false;
                canDoSomeThings = true;
                haveweapon = false;
                guiMouseEnabled = false;
                savedInventory = false;
                savedHome = false;
                player = GetComponent<BasePlayer>();
                newStat(player);
            }

            public void StopMove()
            {
                if (canMove) return;
                if (!player.IsConnected)
                {
                    ReturnPlayer(4);
                    return;
                }
                if (player.IsSleeping()) return;
                if (player.IsWounded())
                {
                    player.StopWounded();
                    player.DisablePlayerCollider();

                }
                player.Teleport(spawnPos);
            }

            public void UpdateGUI()
            {
                CancelInvoke("UpdateGUI");
                if (team == "")
                {
                    if (DuellerArena().guiTimeToRandom > 0 && UnityEngine.Time.realtimeSinceStartup - DuellerArena().guiTimeToRandom > chooseWeaponMaxSec && !haveweapon)
                    {
                        GiveRandomWeapon(player);
                    }
                }
                if (!guiEnabled) return;
                //InvokeRepeating("UpdateGUI", 5f, 5f);
                Invoke("UpdateGUI", 1f);
                WeaponsGUI(player);
            }

            private ActiveDuel DuellerArena()
            {
                foreach (var duel in createdDuels)
                {
                    if (duel.player1 == this)
                    {
                        return duel;
                    }
                    if (duel.player2 == this)
                    {
                        return duel;
                    }
                }
                return null;
            }

            public void Stopper() //стопит чела
            {
                if (!canMove)
                {
                    StopMove();
                    Invoke("Stopper", 0.1f);
                }
            }

            public void PrepairToDuel()
            {
                if (player.IsDead())
                {
                    Invoke("PrepairToDuel", 1f);
                    return;
                }
                SavePlayer(player);
                canDoSomeThings = false;
                player.metabolism.Reset();
                player.metabolism.calories.Add(500);
                player.metabolism.hydration.Add(250);
                player.InitializeHealth(100, 100);
                if (player.IsWounded())
                {
                    player.StopWounded();
                    player.DisablePlayerCollider();

                }
                TPPlayer(player, spawnPos);
                canMove = false;
                Invoke("Stopper", 2f);
                CheckReady();
            }

            public void ReturnWithCooldown()
            {
                if (induel)
                {
                    Invoke("ReturnWithCooldown", 5f);
                    induel = false;
                    return;
                }
                else
                {
                    ReturnPlayer(0);
                }
            }

            public void ReturnPlayer(int reason = 0)
            {
                if (isReturned) return;
                player.Respawn();
                SendChatMessage(reason);
                if (!savedHome)
                {
                    Destroy();
                    return;
                }
                if (player.IsWounded())
                {
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.Wounded, false);
                    player.CancelInvoke("WoundingEnd");
                    player.CancelInvoke("WoundingTick");
                    player.SendNetworkUpdateImmediate(false);
                }
                CuiHelper.DestroyUi(player, "weaponsgui");
                CuiHelper.DestroyUi(player, "mouse");
                CuiHelper.DestroyUi(player, "weaponsguiteamweapons");
                canMove = true;
                player.InitializeHealth(health, 100);
                // player.health = health;
                player.metabolism.calories.@value = calories;
                player.metabolism.hydration.@value = hydration;
                player.metabolism.bleeding.@value = 0;
                player.EnablePlayerCollider();
                player.lifeStory.secondsAlive = 0;
                player.SendNetworkUpdateImmediate();
                TeleportHome();
                RestoreInventory(player); //проверить

                isReturned = true;
                Destroy();
            }

            public void SendChatMessage(int reason = 0)
            {
                switch (reason)
                {
                    case 0:
                        break;
                    case 1:
                        player.ChatMessage(String.Format(returnPlayerReason, returnReasonSleep));
                        break;
                    case 2:
                        player.ChatMessage(String.Format(returnPlayerReason, returnReasonGUIFail));
                        break;
                    case 3:
                        player.ChatMessage(String.Format(returnPlayerReason, String.Format(returnReasonLimitTime, duelMaxSec)));
                        break;
                    case 4:
                        player.ChatMessage(String.Format(returnPlayerReason, returnReasonDisconnect));
                        break;
                    case 5:
                        player.ChatMessage(String.Format(returnPlayerReason, returnReasonSurrender));
                        break;
                    case 6:
                        player.ChatMessage(String.Format(returnPlayerReason, returnReasonUnload));
                        break;
                    case 9:
                        player.ChatMessage("Один из дуэлянтов не проснулся за минуту");
                        break;
                }
            }

            public void Destroy()
            {
                if (toRemoveCorpse.Contains(player.userID)) toRemoveCorpse.Remove(player.userID);
                UnityEngine.Object.Destroy(this);
            }

            public void CheckReady()
            {
                if (!player.IsSleeping())
                {
                    guiEnabled = true;
                    isReady = true;
                    UpdateGUI();
                    return;
                }
                Invoke("CheckReady", 1f);
            }

            public void SaveHealth()
            {
                health = player.health;
                calories = player.metabolism.calories.value;
                hydration = player.metabolism.hydration.value;
            }
            public void SaveHome()
            {
                if (!savedHome)
                    Home = player.transform.position;
                savedHome = true;
            }

            public void SavePlayer(BasePlayer player)
            {
                SaveHome();
                SaveHealth();
                SaveInventory(player);
            }

            public void TeleportHome()
            {
                TPPlayer(player, Home);
                savedHome = false;
            }
            class PlayerInfo
            {
                public bool RestoreOnce = false;
                public List<SavedItem> Items;
            }

            static Dictionary<ulong, PlayerInfo> cachedInventories = new Dictionary<ulong, PlayerInfo>();

            bool SaveInventory(BasePlayer player)
            {
                List<SavedItem> items = GetPlayerItems(player);
                if (!cachedInventories.ContainsKey(player.userID))
                    cachedInventories.Add(player.userID, new PlayerInfo { });
                cachedInventories[player.userID].Items = items;
                StringBuilder sb = new StringBuilder(500);
                sb.Append(player.displayName + " SAVE" + '\n');
                foreach (var i in items)
                    sb.Append(i.shortname + '\n');
                sb.Clear();
                return true;
            }

            List<SavedItem> GetPlayerItems(BasePlayer player)
            {
                List<SavedItem> kititems = (from item in player.inventory.containerBelt.itemList where item != null select ProcessItem(item, "belt")).ToList();
                kititems.AddRange(from item in player.inventory.containerWear.itemList where item != null select ProcessItem(item, "wear"));
                kititems.AddRange(from item in player.inventory.containerMain.itemList where item != null select ProcessItem(item, "main"));
                player.inventory.Strip();
                return kititems;
            }

            //private IEnumerable<ItemsToRestore> GetItems(ItemContainer container, string containerName)
            //{

            //    return container.itemList.Select(item => new ItemsToRestore
            //    {
            //        itemid = item.info.itemid,
            //        container = containerName,
            //        amount = item.amount,
            //        ammo = weapon.primaryMagazine.contents,
            //        position = item.position,
            //        skin = item.skin,
            //        condition = item.condition,
            //        bptarget = item.blueprintTarget,
            //        contents = item.contents?.itemList.Select(item1 => new ItemsToRestore
            //        {
            //            itemid = item1.info.itemid,
            //            amount = item1.amount,
            //            condition = item1.condition
            //        }).ToArray()
            //});
            //}

            SavedItem ProcessItem(Item item, string container)
            {
                SavedItem iItem = new SavedItem
                {
                    shortname = item.info?.shortname,
                    amount = item.amount,
                    mods = new List<SavedItem>(),
                    container = container,
                    skinid = item.skin,
                    blueprint = item.blueprintTarget
                };
                if (item.info == null) return iItem;
                iItem.itemid = item.info.itemid;
                iItem.weapon = false;
                if (item.hasCondition)
                    iItem.condition = item.condition;
                FlameThrower flameThrower = item.GetHeldEntity()?.GetComponent<FlameThrower>();
                if (flameThrower != null)
                    iItem.flamefuel = flameThrower.ammo;
                if (item.info.category.ToString() != "Weapon") return iItem;
                BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
                if (weapon == null) return iItem;
                if (weapon.primaryMagazine == null) return iItem;
                iItem.ammoamount = weapon.primaryMagazine.contents;
                iItem.ammotype = weapon.primaryMagazine.ammoType.shortname;
                iItem.weapon = true;
                if (item.contents != null)
                    foreach (var mod in item.contents.itemList)
                        if (mod.info.itemid != 0)
                            iItem.mods.Add(ProcessItem(mod, "noun"));
                return iItem;
            }



            bool RestoreInventory(BasePlayer player)
            {
                if (!cachedInventories.ContainsKey(player.userID))
                    return false;
                player.inventory.Strip();
                StringBuilder sb = new StringBuilder(500);

                sb.Append($"RESTORE {player.displayName}\n");
                foreach (SavedItem kitem in cachedInventories[player.userID].Items)
                {
                    sb.Append($"{kitem.shortname}\n");
                    GiveItem(player, kitem.weapon ? BuildWeapon(kitem) : BuildItem(kitem), kitem.container);
                }
                sb.Clear();
                return true;
            }

            void GiveItem(BasePlayer player, Item item, string container)
            {
                if (item == null) return;
                ItemContainer cont;
                switch (container)
                {
                    case "wear":
                        cont = player.inventory.containerWear;
                        break;
                    case "belt":
                        cont = player.inventory.containerBelt;
                        break;
                    default:
                        cont = player.inventory.containerMain;
                        break;
                }
                item.MoveToContainer(cont);
            }

            Item BuildItem(SavedItem sItem)
            {
                if (sItem.amount < 1) sItem.amount = 1;
                Item item = ItemManager.CreateByItemID(sItem.itemid, sItem.amount, sItem.skinid);
                item.blueprintTarget = sItem.blueprint;
                if (item.hasCondition) item.condition = sItem.condition;
                FlameThrower flameThrower = item.GetHeldEntity()?.GetComponent<FlameThrower>();
                if (flameThrower)
                    flameThrower.ammo = sItem.flamefuel;
                return item;
            }

            Item BuildWeapon(SavedItem sItem)
            {
                Item item = ItemManager.CreateByItemID(sItem.itemid, 1, sItem.skinid);
                if (item.hasCondition)
                    item.condition = sItem.condition;
                var weapon = item.GetHeldEntity() as BaseProjectile;
                if (weapon != null)
                {
                    var def = ItemManager.FindItemDefinition(sItem.ammotype);
                    weapon.primaryMagazine.ammoType = def;
                    weapon.primaryMagazine.contents = sItem.ammoamount;
                }

                if (sItem.mods != null)
                    foreach (var mod in sItem.mods)
                        item.contents.AddItem(BuildItem(mod).info, 1);
                return item;
            }


        }

        #region Class ItemsToRestore
        class SavedItem
        {
            public string shortname;
            public int itemid;
            public string container;
            public float condition;
            public int amount;
            public int ammoamount;
            public string ammotype;
            public int flamefuel;
            public ulong skinid;
            public bool weapon;
            public int blueprint;
            public List<SavedItem> mods;
        }
        #endregion

        #endregion

        #region BasePlayersFunctions
        bool HavePerm(string permis, ulong playerid = 0)
        {
            if (permission.UserHasPermission(playerid.ToString(), permis))
                return true;
            return false;
        }

        public static BasePlayer FindPlayersSingle(string nameOrIdOrIp, BasePlayer player)
        {
            var targets = FindPlayers(nameOrIdOrIp);
            if (targets.Count <= 0)
            {
                player.ChatMessage(String.Format(playerNotFound, nameOrIdOrIp));
                return null;
            }
            if (targets.Count > 1)
            {
                player.ChatMessage(String.Format(foundMultiplePlayers, string.Join(", ", targets.Select(p => p.displayName).ToArray())));
                return null;
            }
            return targets.First();
        }

        public static HashSet<BasePlayer> FindPlayers(string nameOrIdOrIp)
        {
            var players = new HashSet<BasePlayer>();
            if (string.IsNullOrEmpty(nameOrIdOrIp)) return players;
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.UserIDString.Equals(nameOrIdOrIp))
                    players.Add(activePlayer);
                else if (!string.IsNullOrEmpty(activePlayer.displayName) && activePlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.IgnoreCase))
                    players.Add(activePlayer);
                else if (activePlayer.net?.connection != null && activePlayer.net.connection.ipaddress.Equals(nameOrIdOrIp))
                    players.Add(activePlayer);
            }
            return players;
        }

        static void TPPlayer(BasePlayer player, Vector3 destination)
        {
            BaseMountable mount = player.GetMounted();
            if (mount != null)
            {
                mount.DismountPlayer(player);
            }
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "StartLoading");
            StartSleeping(player);
            player.MovePosition(destination);
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "ForcePositionTo", destination);
            //player.TransformChanged();
            if (player.net?.connection != null)
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate(false);
            if (player.net?.connection == null) return;
            try { player.ClearEntityQueue(null); } catch { }
            player.SendFullSnapshot();
        }

        static void StartSleeping(BasePlayer player)
        {
            if (player.IsSleeping())
                return;
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
            if (!BasePlayer.sleepingPlayerList.Contains(player))
                BasePlayer.sleepingPlayerList.Add(player);
            player.CancelInvoke("InventoryUpdate");
        }

        static DuelPlayer FindOpponent(BasePlayer player)
        {
            foreach (var duel in createdDuels)
            {
                if (duel.player1.player == player)
                    return duel.player2;
                if (duel.player2.player == player)
                    return duel.player1;
            }
            return null;
        }
        #endregion

        #region GUI
        [ConsoleCommand("duel")]
        void ccmdremove(ConsoleSystem.Arg arg)
        {
            if (arg.Connection?.player != null)
            {

                var player = arg.Player();


                string[] args = new string[0];
                if (arg.HasArgs()) args = arg.Args;
                if (args.Length == 0)
                {
                    return;
                }
                DuelPlayer dueller = player.GetComponent<DuelPlayer>();
                if (dueller == null) return;
                if (dueller.canDoSomeThings) return;
                if (args[0] == "surrender")
                {
                    db.playerStat[player.userID].losses++;
                    EndDuel(player, 5, "0", "0");
                    return;
                }
                if (dueller.weapon != "") return;
                dueller.weapon = args[0];
                GiveWeapon(player);
            }
        }

        static void WeaponsGUI(BasePlayer player)
        {
            DuelPlayer dueller = player.GetComponent<DuelPlayer>();
            if (dueller == null) return;
            if (!dueller.guiEnabled) return;

            CuiHelper.DestroyUi(player, "weaponsgui");
            if (!dueller.guiMouseEnabled)
            {
                var mouse = new CuiElementContainer();
                var mousepanel = mouse.Add(new CuiPanel
                {
                    Image = { Color = "0.8 0.2 0.2 0" },
                    RectTransform = { AnchorMin = "0.1 0.3", AnchorMax = "0.9 0.7" },
                    CursorEnabled = true
                }, "Hud", "mouse");
                CuiHelper.AddUi(player, mouse);
                dueller.guiMouseEnabled = true;
            }
            var elements = new CuiElementContainer();
            var panel = elements.Add(new CuiPanel
            {
                Image = { Color = "0 0.02 0 0.95" },
                RectTransform = { AnchorMin = "0.1 0.3", AnchorMax = "0.9 0.7" }
            }, "Hud", "weaponsgui");
            if (!dueller.haveweapon)
            {
                elements.Add(new CuiLabel
                {
                    Text = { Text = guiChooseWeapon, FontSize = 22, Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = "0 0.9", AnchorMax = "1 1" }
                }, panel);
                if (dueller.team == "")
                {
                    elements.Add(new CuiButton
                    {
                        Button = { Color = "1 1 1 0", Command = "duel surrender", FadeIn = 0 },
                        RectTransform = { AnchorMin = "0.8 0.0", AnchorMax = "0.99 0.1" },
                        Text = { Text = guiSurrenderButton, Color = "1 1 1 1", FontSize = 26, Align = TextAnchor.LowerRight }
                    }, panel);
                }
                int weaponsCount = weapons.Count;
                for (int i = 0; i < weaponsCount; i++)
                {
                    if (i / 5 == (int)0)
                    {
                        elements.Add(new CuiButton
                        {
                            Button = { Color = "1 1 1 0.2", Command = $"duel {weapons[i]}", FadeIn = 0 },
                            RectTransform = { AnchorMin = $"{i * 0.2f} 0.8", AnchorMax = $"{i * 0.2f + 0.2f} 0.9" },
                            Text = { Text = weapons[i], FontSize = 22, Align = TextAnchor.MiddleCenter }
                        }, panel);
                    }
                    if (i / 5 == (int)1)
                    {
                        elements.Add(new CuiButton
                        {
                            Button = { Color = "1 1 1 0.2", Command = $"duel {weapons[i]}", FadeIn = 0 },
                            RectTransform = { AnchorMin = $"{(i * 0.2f) - 1f} 0.65", AnchorMax = $"{(i * 0.2f + 0.2f) - 1f} 0.75" },
                            Text = { Text = weapons[i], FontSize = 22, Align = TextAnchor.MiddleCenter }
                        }, panel);
                    }
                    if (i / 5 == (int)2)
                    {
                        elements.Add(new CuiButton
                        {
                            Button = { Color = "1 1 1 0.2", Command = $"duel {weapons[i]}", FadeIn = 0 },
                            RectTransform = { AnchorMin = $"{(i * 0.2f - 2f)} 0.50", AnchorMax = $"{(i * 0.2f + 0.2f) - 2f} 0.60" },
                            Text = { Text = weapons[i], FontSize = 22, Align = TextAnchor.MiddleCenter }
                        }, panel);
                    }
                    if (i / 5 == (int)3)
                    {
                        elements.Add(new CuiButton
                        {
                            Button = { Color = "1 1 1 0.2", Command = $"duel {weapons[i]}", FadeIn = 0 },
                            RectTransform = { AnchorMin = $"{(i * 0.2f) - 3f} 0.35", AnchorMax = $"{(i * 0.2f + 0.2f) - 3f} 0.45" },
                            Text = { Text = weapons[i], FontSize = 22, Align = TextAnchor.MiddleCenter }
                        }, panel);
                    }
                    if (i / 5 == (int)4)
                    {
                        elements.Add(new CuiButton
                        {
                            Button = { Color = "1 1 1 0.2", Command = $"duel {weapons[i]}", FadeIn = 0 },
                            RectTransform = { AnchorMin = $"{(i * 0.2f) - 4f} 0.20", AnchorMax = $"{(i * 0.2f + 0.2f) - 4f} 0.30" },
                            Text = { Text = weapons[i], FontSize = 22, Align = TextAnchor.MiddleCenter }
                        }, panel);
                    }
                    if (i / 5 == (int)5)
                    {
                        elements.Add(new CuiButton
                        {
                            Button = { Color = "1 1 1 0.2", Command = $"duel {weapons[i]}", FadeIn = 0 },
                            RectTransform = { AnchorMin = $"{(i * 0.2f) - 5f} 0.05", AnchorMax = $"{(i * 0.2f + 0.2f) - 5f} 0.15" },
                            Text = { Text = weapons[i], FontSize = 22, Align = TextAnchor.MiddleCenter }
                        }, panel);
                    }
                }
            }
            if (dueller.team != "")
            {
                CuiHelper.DestroyUi(player, "weaponsguiteamweapons");
                var elementsteam = new CuiElementContainer();
                var panelteam = elementsteam.Add(new CuiPanel
                {
                    Image = { Color = "0 0.02 0 0.95" },
                    RectTransform = { AnchorMin = "0.0 0.71", AnchorMax = "1 0.9" }
                }, "Hud", "weaponsguiteamweapons");
                var duel = createdTeamDuels[0];
                elementsteam.Add(new CuiLabel
                {
                    Text = { Text = teamGuiWeapons, FontSize = 20, Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = "0.2 0.75", AnchorMax = "0.8 1" }
                }, panelteam);
                int ip = 0;
                int duelAllPlayersCount = duel.allPlayers.Count;
                for (int pli = 0; pli < duelAllPlayersCount; pli++)
                {
                    var pl = duel.allPlayers[pli];
                    string wp = teamGuiNoWeapon;
                    string clr = "";
                    if (pl.team == "blue") clr = teamGuiBluePlayerColor;
                    if (pl.team == "red") clr = teamGuiRedPlayerColor;
                    if (pl.weapon != "") wp = pl.weapon;
                    elementsteam.Add(new CuiLabel
                    {
                        Text = { Text = $"<color={clr}>{pl.player.displayName}</color> : <color={teamGuiWeaponColor}>{wp}</color>", FontSize = 12, Align = TextAnchor.MiddleCenter },
                        RectTransform = { AnchorMin = $"{ip * (1f / duel.allPlayers.Count)} 0.3", AnchorMax = $"{(ip * (1f / duel.allPlayers.Count)) + (1f / duel.allPlayers.Count)} 0.6" }
                    }, panelteam);
                    ip++;
                }
                if (dueller.guiEnabled)
                    CuiHelper.AddUi(player, elementsteam);
            }
            if (dueller.haveweapon && dueller.team == "")
            {
                elements.Add(new CuiLabel
                {
                    Text = { Text = String.Format(guiYourChoose, dueller.weapon), FontSize = 20, Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = "0 0.4", AnchorMax = "0.5 0.6" }
                }, panel);

                #region OpponentsWeapon 
                ActiveDuel playersDuel = null;
                string opponentweapon = "";
                foreach (var duel in createdDuels)
                {
                    if (duel.player1 == dueller)
                    {
                        playersDuel = duel;
                        opponentweapon = duel.player2.weapon;
                        break;
                    }
                    if (duel.player2 == dueller)
                    {
                        playersDuel = duel;
                        opponentweapon = duel.player1.weapon;
                        break;
                    }
                }

                if (opponentweapon == "")
                {
                    elements.Add(new CuiLabel
                    {
                        Text = { Text = guiWaitForOpponentChoose, FontSize = 20, Align = TextAnchor.MiddleCenter },
                        RectTransform = { AnchorMin = "0.5 0.4", AnchorMax = "1 0.6" }
                    }, panel);
                }
                else
                {
                    elements.Add(new CuiLabel
                    {
                        Text = { Text = String.Format(guiOpponentsWeapon, opponentweapon), FontSize = 20, Align = TextAnchor.MiddleCenter },
                        RectTransform = { AnchorMin = "0.5 0.4", AnchorMax = "1 0.6" }
                    }, panel);

                    elements.Add(new CuiLabel
                    {
                        Text = { Text = guiStartAboutToBegin, FontSize = 22, Align = TextAnchor.MiddleCenter },
                        RectTransform = { AnchorMin = "0.3 0.0", AnchorMax = "0.7 0.2" }
                    }, panel);
                    if (dueller.guiEnabled)
                        CuiHelper.AddUi(player, elements);
                    return;
                }
                #endregion
            }
            if (dueller.team == "")
            {
                var opp = FindOpponent(player);
                var thisDuel = FindDuelByPlayer(player);
                int seconds = (int)chooseWeaponMaxSec - (int)(UnityEngine.Time.realtimeSinceStartup - thisDuel.guiTimeToRandom);
                if (seconds < 0) seconds = 25;
                if (opp != null && opp.isReady)
                {
                    elements.Add(new CuiLabel
                    {
                        Text = { Text = String.Format(guiAutoCloseSec, seconds), FontSize = 22, Align = TextAnchor.MiddleCenter },
                        RectTransform = { AnchorMin = "0.3 0.0", AnchorMax = "0.7 0.2" }
                    }, panel);
                }
                else
                {
                    elements.Add(new CuiLabel
                    {
                        Text = { Text = guiPlayerSleep, FontSize = 22, Align = TextAnchor.MiddleCenter },
                        RectTransform = { AnchorMin = "0.3 0.0", AnchorMax = "0.7 0.2" }
                    }, panel);
                }
            }
            if (dueller.team != "" && dueller.haveweapon)
            {
                elements.Add(new CuiLabel
                {
                    Text = { Text = teamGuiWaiting, FontSize = 22, Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = "0.3 0.0", AnchorMax = "0.7 0.2" }
                }, panel);
            }
            if (dueller.guiEnabled)
                CuiHelper.AddUi(player, elements);
        }
        #endregion

        #region Oxide
        void OnPlayerRespawned(BasePlayer player)
        {
            if (player.transform.position.y > 450f && !IsDuelPlayer(player))
            {
                new PluginTimers(this).Once(1, () =>
                {
                    player.Die();
                });
            }

            DuelPlayer dueller = player?.GetComponent<DuelPlayer>();
            if (dueller == null) return;
            if (!dueller.haveweapon) return;
            new PluginTimers(this).Once(1, () =>
            {
                EndDuel(player, 0, "0", "0");
            });
        }


        object CanEntityTakeDamage(BaseCombatEntity victim, HitInfo hitInfo)
        {
            if (victim == null || hitInfo == null) return null;
            //Disabling decay of the arena
            if (hitInfo.damageTypes.Has(Rust.DamageType.Decay))
            {
                if (IsArenaZone(victim.transform.position))
                {
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, 0);
                    return false;
                }
            }

            var attacker = hitInfo.InitiatorPlayer;
            if (attacker == null) return null;


            var victimPlayer = victim.GetComponent<BasePlayer>();
            if (victimPlayer == null) return null;

            if (IsPlayerOnActiveDuel(attacker) && IsPlayerOnActiveDuel(victimPlayer))
            {
                if (FindOpponent(attacker) != null)
                    if (IsDuelPlayer(attacker) && !FindOpponent(attacker).canMove) return false; //отмена на дамаг от телепорта (если он будет)

                 if (FindOpponent(victimPlayer) != null)
                    if (IsDuelPlayer(victimPlayer) && !FindOpponent(victimPlayer).canMove) return false; //отмена на дамаг от телепорта (если он будет)

                return true;
            }

            if (createdTeamDuels.Count > 0)
            {
                if (victim != null)
                {
                    if (victimPlayer != null)
                    {
                        DuelPlayer dueller = victimPlayer?.GetComponent<DuelPlayer>();
                        if (dueller != null)
                            if (!dueller.canMove) return false; //возвращать дамаг

                        return true;
                    }
                }
                var dvictim = createdTeamDuels[0].allPlayers.Find(x => x.player == victimPlayer);
                if (dvictim != null)
                {
                    var dattacker = createdTeamDuels[0].allPlayers.Find(x => x.player == hitInfo.Initiator?.ToPlayer());
                    if (dattacker != null)
                    {
                        if (dvictim.team == dattacker.team)
                        {
                            attacker.ChatMessage(String.Format(teamDamageTeammate, victimPlayer.displayName));
                            return false; //отмена дамага по однотимным
                        }
                        return true;
                    }
                }
            }
            return null;
        }

        object OnEntityTakeDamage(BaseCombatEntity victim, HitInfo hitInfo)
        {
            if (hitInfo.damageTypes.Has(Rust.DamageType.Decay))
            {
                if (IsArenaZone(victim.transform.position))
                {
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, 0);
                    return false;
                }
            }
            var attacker = hitInfo.Initiator?.ToPlayer();
            var victimPlayer = (victim as BasePlayer);
            if (createdTeamDuels.Count > 0)
            {
                if (victim != null)
                {
                    if (victimPlayer != null)
                    {
                        DuelPlayer dueller = victimPlayer?.GetComponent<DuelPlayer>();
                        if (dueller != null)
                            if (!dueller.canMove) return false; //возвращать дамаг
                    }
                }
                if (attacker == null) return null;
                var dvictim = createdTeamDuels[0].allPlayers.Find(x => x.player == victimPlayer);
                var dattacker = createdTeamDuels[0].allPlayers.Find(x => x.player == hitInfo.Initiator?.ToPlayer());
                if (dvictim != null)
                {
                    if (dattacker != null)
                    {
                        if (dvictim.team == dattacker.team)
                        {
                            attacker.ChatMessage(String.Format(teamDamageTeammate, victimPlayer.displayName));
                            return false; //отмена дамага по однотимным
                        }
                    }
                }
                else
                    return null;
            }

            if (attacker != null)
            {
                if (victim != null)
                {
                    if (victimPlayer != null)
                    {
                        if (IsDuelPlayer(attacker) && !IsDuelPlayer(victimPlayer)) return false; //отмена на обычных игроков от дуэлянта
                    }
                    DuelPlayer dueller = attacker?.GetComponent<DuelPlayer>();
                    if (dueller == null) return null;
                    if (!dueller.haveweapon) return null;
                    if (IsDuelPlayer(attacker) && victimPlayer == null) return false; //отмена на всё, кроме baseplayer
                }
            }
            if (victim != null)
            {
                if (victimPlayer != null)
                {
                    if (FindOpponent(victimPlayer) != null)
                    {
                        if (IsDuelPlayer(victimPlayer) && !FindOpponent(victimPlayer).canMove) return false; //отмена на дамаг от телепорта (если он будет)
                    }
                }
            }
            return null;
        }


        object OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            DuelPlayer dueller = player?.GetComponent<DuelPlayer>();
            if (dueller == null) return null;
            if (dueller.team != "" && dueller.haveweapon)
            {
                var duel = createdTeamDuels[0];
                if (dueller.team == "blue")
                {
                    duel.teamblue.Remove(dueller);
                }
                if (dueller.team == "red")
                {
                    duel.teamred.Remove(dueller);
                }
                int allPlayersN = duel.allPlayers.Count;
                for (int i = 0; i < allPlayersN; i++)
                {
                    duel.allPlayers[i].player.ChatMessage(String.Format(teamDeath, player.displayName, dueller.team, duel.teamblue.Count, duel.teamred.Count));
                }
                duel.allPlayers.Remove(dueller);

                dueller.guiEnabled = false;
                dueller.canMove = true;
                if (dueller.induel)
                    dueller.ReturnWithCooldown();
                dueller.induel = false;
                return null;
            }
            var opponent = FindOpponent(player);
            if (opponent != null)
            {
                if (opponent.isDeath || dueller.isDeath || !dueller.haveweapon || !opponent.haveweapon) return null;
                opponent.isDeath = true;
                player.ChatMessage(statLoss);
                var duel = FindDuelByPlayer(player);
                PrintToChat(String.Format(notificationAboutWin, duel.player1.player.displayName, duel.player2.player.displayName, opponent.player.displayName));
                opponent.player.ChatMessage(statWin);
                db.playerStat[opponent.player.userID].wins++;
                db.playerStat[player.userID].losses++;
                var Ts = (from x in Tops where x.SteamId == opponent.player.UserIDString || x.SteamId == player.UserIDString select x);
                foreach (var top in Ts)
                {
                    top.Win = opponent.player.UserIDString;
                }
                EndDuel(player, 7, opponent.player.UserIDString, player.UserIDString);
            }
            return null;
        }

        void OnEntitySpawned(BaseEntity entity) => RemoveGarbage(entity); //remove corpses and etc

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                OnlinePlayer onlinePlayer;
                if (onlinePlayers.TryGetValue(player, out onlinePlayer))
                {
                    if (onlinePlayer.Trade != null)
                    {
                        TradeCloseBoxes(onlinePlayer.Trade);
                    }
                    else if (onlinePlayer.View != null)
                    {
                        CloseBoxView(player, onlinePlayer.View);
                    }
                }
            }


            foreach (var player in BasePlayer.activePlayerList)
            {
                EndDuel(player, 6, "0", "0");
                CuiHelper.DestroyUi(player, "weaponsguiteamweapons");
                CuiHelper.DestroyUi(player, "weaponsgui");
                CuiHelper.DestroyUi(player, "mouse");
                CuiHelper.DestroyUi(player, "mouse");

            }
            if (createdTeamDuels.Count > 0)
                createdTeamDuels[0].Destroy();
            Puts("Арены удалены");
            foreach (var entity in ArenaEntities)
            {
                if (!entity.IsDestroyed)
                    entity.Kill();
            };
            SaveData();
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            OnlinePlayer onlinePlayer;
            if (onlinePlayers.TryGetValue(player, out onlinePlayer))
            {
                if (onlinePlayer.Trade != null)
                {
                    TradeCloseBoxes(onlinePlayer.Trade);
                }
                else if (onlinePlayer.View != null)
                {
                    CloseBoxView(player, onlinePlayer.View);
                }
            }

            DuelPlayer dueller = player?.GetComponent<DuelPlayer>();
            if (dueller == null) return;
            EndDuel(player, 4, "0", "0");
        }

        private object CanBuild(Planner plan, Construction prefab)
        {
            DuelPlayer dueller = plan.GetOwnerPlayer()?.GetComponent<DuelPlayer>();
            if (dueller == null) return null;
            if (dueller.canDoSomeThings) return null;
            SendReply(plan.GetOwnerPlayer(), cantBuild);
            return false;
        }

        private object canRemove(BasePlayer player)
        {
            DuelPlayer dueller = player?.GetComponent<DuelPlayer>();
            if (dueller == null) return null;
            if (dueller.canDoSomeThings) return null;
            SendReply(player, cantRemove);
            return true;
        }

        private object CanTrade(BasePlayer player)
        {
            DuelPlayer dueller = player?.GetComponent<DuelPlayer>();
            if (dueller == null) return null;
            if (dueller.canDoSomeThings) return null;
            return cantTrade;

        }

        private object BackpackItem(BasePlayer player)
        {
            DuelPlayer dueller = player?.GetComponent<DuelPlayer>();
            if (dueller == null) return null;
            if (dueller.canDoSomeThings) return null;
            return cantTrade;

        }
        private object ConsoleAlias(BasePlayer player)
        {
            DuelPlayer dueller = player?.GetComponent<DuelPlayer>();
            if (dueller == null) return null;
            if (dueller.canDoSomeThings) return null;
            return cantTrade;

        }


        private object CanTeleport(BasePlayer player)
        {
            DuelPlayer dueller = player?.GetComponent<DuelPlayer>();
            if (dueller == null) return null;
            if (dueller.canDoSomeThings) return null;
            return cantTp;
        }

        private object inDuel(BasePlayer player) => player?.GetComponent<DuelPlayer>() != null;

        private object canRedeemKit(BasePlayer player)
        {
            DuelPlayer dueller = player?.GetComponent<DuelPlayer>();
            if (dueller == null) return null;
            if (dueller.canDoSomeThings) return null;
            return cantUseKit;
        }

        private object OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (arg.cmd.Name == "kill")
            {
                DuelPlayer dueller = arg.Player()?.GetComponent<DuelPlayer>();
                if (dueller == null) return null;
                if (dueller.canDoSomeThings) return null;
                SendReply(arg.Player(), cantUseKill);
                return false;
            }
            if (arg.cmd?.FullName == "backpack.open")
            {
                DuelPlayer dueller = arg.Player()?.GetComponent<DuelPlayer>();
                if (dueller == null) return null;
                if (dueller.canDoSomeThings) return null;
                SendReply(arg.Player(), cantUseBackPack);
                return false;
            }
            return null;
        }

        public List<string> Commands = new List<string>()
        {
            "bp",
            "backpack",
            "skin",
            "skins",
            "skinbox",
            "rec",
            "tpa",
            "tpr",
            "sethome",
            "home",
            "kit",
            "remove"
        };


        private object OnUserCommand(IPlayer ipl, string command, string[] args)
        {
            if (ipl == null || !ipl.IsConnected) return null;
            var player = ipl.Object as BasePlayer;
            if (player == null) return null;
            DuelPlayer dueller = player.GetComponent<DuelPlayer>();
            if (dueller == null) return null;
            if (dueller.canDoSomeThings) return null;
            if (Commands.Contains(command.ToLower()))
            {
                SendReply(player, cantUseCommand.Replace("{0}", command));
                return false;
            }
            return null;
        }

        void OnServerInitialized(bool serverInitialized)
        {
            if (TruePVE)
                Unsubscribe("OnEntityTakeDamage");
            else
                Subscribe("OnEntityTakeDamage");


            if (!permission.PermissionExists(duelCreatePermission)) permission.RegisterPermission(duelCreatePermission, this);
            if (!permission.PermissionExists(duelJoinPermission)) permission.RegisterPermission(duelJoinPermission, this);

            if (serverInitialized)
            {
                PrintWarning("Инициализация плагина выполнена. Ожидайте 10 секунд для инициализации файлов арен.");
                timer.Once(10f, () =>
                {
                    CreateDuelArena();
                    isIni = true;
                });
            }
            else
            {
                CreateDuelArena();
                isIni = true;
            }
        }
        #endregion

        #region GiveItems

        public static void GiveRandomWeapon(BasePlayer player)
        {
            var rnd = new System.Random();
            string weapon = weapons[UnityEngine.Random.Range(0, weapons.Count)];
            DuelPlayer dueller = player.GetComponent<DuelPlayer>();
            if (dueller != null)
            {
                if (!dueller.haveweapon)
                {
                    dueller.weapon = weapon;
                    GiveWeapon(player);
                }
            }
        }

        public static void GiveWear(BasePlayer player)
        {
            DuelPlayer dueller = player.GetComponent<DuelPlayer>();
            if (dueller.team == "blue")
            {
                foreach (var item in WearsBlue)
                {
                    player.inventory.GiveItem(ItemManager.CreateByName(item.Key, 1, item.Value), player.inventory.containerWear);
                }
                return;
            }
            if (dueller.team == "red")
            {
                foreach (var item in WearsRed)
                {
                    player.inventory.GiveItem(ItemManager.CreateByName(item.Key, 1, item.Value), player.inventory.containerWear);
                }
                return;
            }
            foreach (var item in Wears)
            {
                player.inventory.GiveItem(ItemManager.CreateByName(item.Key, 1, item.Value), player.inventory.containerWear);
            }
        }
        static List<string> weapons = new List<string>
        {
            "M249",
            "LR-300",
            "AK-47",
            "Болт",
            "Берданка",
            "MP5",
            "Томпсон",
            "Смг",
            "Дробовик",
            "Двухстволка",
            "Пайп",
            "Питон",
            "П250",
            "M92",
            "Револьвер",
            "Лук",
            "Копьё",
            "Нож",
            "Арбалет",
            "ЕОКА",
            "Камень",
            "Меч"
        };

        public static void GiveAndShowItem(BasePlayer player, string item, int amount, ulong skindid = 0)
        {
            player.inventory.GiveItem(ItemManager.CreateByName(item, amount, skindid), player.inventory.containerBelt);
            player.Command("note.inv", new object[] { item, amount });
        }

        public static void GiveWeapon(BasePlayer player)
        {
            DuelPlayer dueller = player.GetComponent<DuelPlayer>();
            if (dueller == null) return;
            GiveWear(player);
            switch (dueller.weapon)
            {
                case "M249":
                    GiveAndShowItem(player, "lmg.m249", 1);
                    GiveAndShowItem(player, "bandage", 5);
                    GiveAndShowItem(player, "syringe.medical", 3);
                    GiveAndShowItem(player, "weapon.mod.flashlight", 1); //фонарик 
                    GiveAndShowItem(player, "weapon.mod.holosight", 1); //коллиматор
                    GiveAndShowItem(player, "ammo.rifle", 200); // 5.56
                    GiveAndShowItem(player, "ammo.rifle.hv", 200); // 5.56 мм ВС
                    break;
                case "AK-47":
                    GiveAndShowItem(player, "rifle.ak", 1, 10138);
                    GiveAndShowItem(player, "bandage", 5);
                    GiveAndShowItem(player, "syringe.medical", 3);
                    GiveAndShowItem(player, "weapon.mod.flashlight", 1); //фонарик 
                    GiveAndShowItem(player, "weapon.mod.holosight", 1); //коллиматор
                    GiveAndShowItem(player, "ammo.rifle", 200); // 5.56
                    GiveAndShowItem(player, "ammo.rifle.hv", 200); // 5.56 мм ВС
                    break;
                case "Болт":
                    GiveAndShowItem(player, "rifle.bolt", 1, 10117);
                    GiveAndShowItem(player, "bandage", 5);
                    GiveAndShowItem(player, "syringe.medical", 3);
                    GiveAndShowItem(player, "weapon.mod.flashlight", 1); //фонарик 
                    GiveAndShowItem(player, "weapon.mod.holosight", 1); //коллиматор
                    GiveAndShowItem(player, "ammo.rifle", 100); // 5.56
                    GiveAndShowItem(player, "ammo.rifle.hv", 100); // 5.56 мм ВС
                    break;
                case "LR-300":
                    GiveAndShowItem(player, "rifle.lr300", 1);
                    GiveAndShowItem(player, "bandage", 5);
                    GiveAndShowItem(player, "syringe.medical", 3);
                    GiveAndShowItem(player, "weapon.mod.flashlight", 1); //фонарик 
                    GiveAndShowItem(player, "weapon.mod.holosight", 1); //коллиматор
                    GiveAndShowItem(player, "ammo.rifle", 200); // 5.56
                    GiveAndShowItem(player, "ammo.rifle.hv", 200); // 5.56 мм ВС
                    break;
                case "Берданка":
                    GiveAndShowItem(player, "rifle.semiauto", 1);
                    GiveAndShowItem(player, "bandage", 5);
                    GiveAndShowItem(player, "syringe.medical", 3);
                    GiveAndShowItem(player, "weapon.mod.flashlight", 1); //фонарик 
                    GiveAndShowItem(player, "weapon.mod.holosight", 1); //коллиматор
                    GiveAndShowItem(player, "ammo.rifle", 100); // 5.56
                    GiveAndShowItem(player, "ammo.rifle.hv", 100); // 5.56 мм ВС
                    break;
                case "Питон":
                    GiveAndShowItem(player, "pistol.python", 1);
                    GiveAndShowItem(player, "bandage", 5);
                    GiveAndShowItem(player, "syringe.medical", 3);
                    GiveAndShowItem(player, "weapon.mod.holosight", 1); //коллиматор
                    GiveAndShowItem(player, "ammo.pistol", 100); // pistol bullet
                    GiveAndShowItem(player, "ammo.pistol.hv", 100); // 9 мм ВС
                    break;
                case "П250":
                    GiveAndShowItem(player, "pistol.semiauto", 1, 805925675);
                    GiveAndShowItem(player, "bandage", 5);
                    GiveAndShowItem(player, "syringe.medical", 3);
                    GiveAndShowItem(player, "weapon.mod.flashlight", 1); //фонарик 
                    GiveAndShowItem(player, "weapon.mod.holosight", 1); //коллиматор
                    GiveAndShowItem(player, "ammo.pistol", 100); // pistol bullet
                    GiveAndShowItem(player, "ammo.pistol.hv", 100); // 9 мм ВС
                    break;
                case "M92":
                    GiveAndShowItem(player, "pistol.m92", 1);
                    GiveAndShowItem(player, "bandage", 5);
                    GiveAndShowItem(player, "syringe.medical", 3);
                    GiveAndShowItem(player, "weapon.mod.flashlight", 1); //фонарик 
                    GiveAndShowItem(player, "weapon.mod.holosight", 1); //коллиматор
                    GiveAndShowItem(player, "ammo.pistol", 100); // pistol bullet
                    GiveAndShowItem(player, "ammo.pistol.hv", 100); // 9 мм ВС
                    break;
                case "Револьвер":
                    GiveAndShowItem(player, "pistol.revolver", 1);
                    GiveAndShowItem(player, "bandage", 5);
                    GiveAndShowItem(player, "syringe.medical", 3);
                    GiveAndShowItem(player, "ammo.pistol", 100); // 9 мм
                    GiveAndShowItem(player, "ammo.pistol.hv", 100); // 9 мм ВС
                    break;
                case "Лук":
                    GiveAndShowItem(player, "bow.hunting", 1);
                    GiveAndShowItem(player, "bandage", 5);
                    GiveAndShowItem(player, "syringe.medical", 3);
                    GiveAndShowItem(player, "arrow.wooden", 600); // arrows
                    break;
                case "Копьё":
                    GiveAndShowItem(player, "spear.wooden", 1);
                    GiveAndShowItem(player, "spear.wooden", 1);
                    GiveAndShowItem(player, "spear.wooden", 1);
                    GiveAndShowItem(player, "spear.wooden", 1);
                    GiveAndShowItem(player, "bandage", 5);
                    GiveAndShowItem(player, "syringe.medical", 3);
                    break;
                case "Нож":
                    GiveAndShowItem(player, "knife.bone", 1);
                    GiveAndShowItem(player, "bandage", 5);
                    GiveAndShowItem(player, "syringe.medical", 3);
                    break;
                case "Томпсон":
                    GiveAndShowItem(player, "smg.thompson", 1, 561462394);
                    GiveAndShowItem(player, "bandage", 5);
                    GiveAndShowItem(player, "syringe.medical", 3);
                    GiveAndShowItem(player, "weapon.mod.holosight", 1); //коллиматор
                    GiveAndShowItem(player, "ammo.pistol", 100); // pistol bullet
                    GiveAndShowItem(player, "ammo.pistol.hv", 100); // 9 мм ВС
                    break;
                case "Смг":
                    GiveAndShowItem(player, "smg.2", 1);
                    GiveAndShowItem(player, "bandage", 5);
                    GiveAndShowItem(player, "syringe.medical", 3);
                    GiveAndShowItem(player, "weapon.mod.flashlight", 1); //фонарик 
                    GiveAndShowItem(player, "weapon.mod.holosight", 1); //коллиматор
                    GiveAndShowItem(player, "ammo.pistol", 100); // pistol bullet
                    GiveAndShowItem(player, "ammo.pistol.hv", 100); // 9 мм ВС
                    break;
                case "Арбалет":
                    GiveAndShowItem(player, "crossbow", 1);
                    GiveAndShowItem(player, "bandage", 5);
                    GiveAndShowItem(player, "syringe.medical", 3);
                    GiveAndShowItem(player, "arrow.wooden", 600); // arrows
                    GiveAndShowItem(player, "weapon.mod.flashlight", 1); //фонарик 
                    GiveAndShowItem(player, "weapon.mod.holosight", 1); //коллиматор
                    break;
                case "Дробовик":
                    GiveAndShowItem(player, "shotgun.pump", 1, 731119713);
                    GiveAndShowItem(player, "bandage", 5);
                    GiveAndShowItem(player, "syringe.medical", 3);
                    GiveAndShowItem(player, "weapon.mod.flashlight", 1); //фонарик 
                    GiveAndShowItem(player, "weapon.mod.holosight", 1); //коллиматор
                    GiveAndShowItem(player, "ammo.shotgun", 100); // картечь
                    GiveAndShowItem(player, "ammo.shotgun.slug", 100); // пулевой
                    break;
                case "Пайп":
                    GiveAndShowItem(player, "shotgun.waterpipe", 1);
                    GiveAndShowItem(player, "bandage", 5);
                    GiveAndShowItem(player, "syringe.medical", 3);
                    GiveAndShowItem(player, "weapon.mod.flashlight", 1); //фонарик 
                    GiveAndShowItem(player, "weapon.mod.holosight", 1); //коллиматор
                    GiveAndShowItem(player, "ammo.shotgun", 100); // картечь
                    GiveAndShowItem(player, "ammo.shotgun.slug", 100); // пулевой
                    break;
                case "Двухстволка":
                    GiveAndShowItem(player, "shotgun.double", 1);
                    GiveAndShowItem(player, "bandage", 5);
                    GiveAndShowItem(player, "syringe.medical", 3);
                    GiveAndShowItem(player, "weapon.mod.flashlight", 1); //фонарик 
                    GiveAndShowItem(player, "weapon.mod.holosight", 1); //коллиматор
                    GiveAndShowItem(player, "ammo.shotgun", 100); // картечь
                    GiveAndShowItem(player, "ammo.shotgun.slug", 100); // пулевой
                    break;
                case "ЕОКА":
                    GiveAndShowItem(player, "pistol.eoka", 1);
                    GiveAndShowItem(player, "bandage", 5);
                    GiveAndShowItem(player, "syringe.medical", 3);
                    GiveAndShowItem(player, "ammo.handmade.shell", 100); // fuel
                    break;
                case "Камень":
                    GiveAndShowItem(player, "rock", 1, 807372963);
                    break;
                case "MP5":
                    GiveAndShowItem(player, "smg.mp5", 1, 800974015);
                    GiveAndShowItem(player, "bandage", 5);
                    GiveAndShowItem(player, "syringe.medical", 3);
                    GiveAndShowItem(player, "weapon.mod.flashlight", 1); //фонарик 
                    GiveAndShowItem(player, "weapon.mod.holosight", 1); //коллиматор
                    GiveAndShowItem(player, "ammo.pistol", 100); // pistol bullet
                    break;
                case "Меч":
                    GiveAndShowItem(player, "salvaged.sword", 1);
                    GiveAndShowItem(player, "bandage", 5);
                    GiveAndShowItem(player, "syringe.medical", 3);
                    break;
            }
            dueller.haveweapon = true;
            dueller.readyForBattle = true;
        }
        #endregion

        #region Statistic
        class StoredData
        {
            public Dictionary<ulong, Stat> playerStat = new Dictionary<ulong, Stat>();
            public StoredData() { }
        }
        static StoredData db;
        class Stat
        {
            public string name;
            public int wins;
            public int losses;
            public int teamwins;
            public int teamloss;
        }

        public static void newStat(BasePlayer player)
        {
            Stat value = new Stat();
            if (!db.playerStat.TryGetValue(player.userID, out value))
            {
                Stat stat = new Stat();
                stat.name = player.displayName;
                stat.wins = 0;
                stat.losses = 0;
                stat.teamwins = 0;
                stat.teamloss = 0;
                db.playerStat[player.userID] = stat;
                return;
            }
            if (db.playerStat[player.userID].name != player.displayName)
                db.playerStat[player.userID].name = player.displayName;
        }

        void SaveData()
        {
            if (Tops != null) Interface.Oxide.DataFileSystem.WriteObject("DuelStavki", Tops);
            if (db != null) Interface.Oxide.DataFileSystem.WriteObject("Duel", db);
        }

        void OnServerSave()
        {
            SaveData();
        }

        void showStat(BasePlayer player)
        {
            newStat(player);
            SendReply(player, String.Format(yourStat, db.playerStat[player.userID].wins, db.playerStat[player.userID].losses, db.playerStat[player.userID].teamwins, db.playerStat[player.userID].teamloss));
        }

        void showTop(BasePlayer player)
        {
            if (db.playerStat.Count == 0)
            {
                SendReply(player, emptyTop);
                return;
            }
            string msg = topWin;
            Dictionary<string, int> namewin = new Dictionary<string, int>();
            Dictionary<string, int> namelosses = new Dictionary<string, int>();
            foreach (var pl in db.playerStat)
            {
                namewin[pl.Value.name] = pl.Value.wins;
                namelosses[pl.Value.name] = pl.Value.losses;
            }
            var reply = 0;
            if (reply == 0) { }
            int i = 0;
            int j = 0;
            foreach (var pair in namewin.OrderByDescending(pair => pair.Value))
            {
                i++;
                msg += String.Format(playerInTop, i, pair.Key, pair.Value);
                if (i == maxWinsTop) break;
            }
            msg += topLosses;
            foreach (var pair in namelosses.OrderByDescending(pair => pair.Value))
            {
                j++;
                msg += String.Format(playerInTop, j, pair.Key, pair.Value);
                if (j == maxLoseTop) break;
            }
            msg += topTeamWin;
            foreach (var pl in db.playerStat)
            {
                namewin[pl.Value.name] = pl.Value.teamwins;
                namelosses[pl.Value.name] = pl.Value.teamloss;
            }
            i = 0;
            j = 0;
            foreach (var pair in namewin.OrderByDescending(pair => pair.Value))
            {
                i++;
                msg += String.Format(playerInTop, i, pair.Key, pair.Value);
                if (i == maxWinsTop) break;
            }
            msg += topTeamLoss;
            foreach (var pair in namelosses.OrderByDescending(pair => pair.Value))
            {
                j++;
                msg += String.Format(playerInTop, j, pair.Key, pair.Value);
                if (j == maxLoseTop) break;
            }
            SendReply(player, msg);
        }
        #endregion

        #region Arena

        #region classArena
        public class Arena
        {
            public string name;
            public Vector3 player1pos;
            public Vector3 player2pos;
            public Vector3 pos;
            public List<Vector3> teamblueSpawns = new List<Vector3>();
            public List<Vector3> teamredSpawns = new List<Vector3>();
        }
        #endregion

        Arena FreeArena()
        {
            Arena randomarena = new Arena();
            List<Arena> freeArenas = new List<Arena>();
            Arena value = new Arena();
            foreach (var arena in arenaList)
            {
                if (!busyArena.Contains(arena))
                    freeArenas.Add(arena);
            }
            if (freeArenas.Count > 0)
            {
                int random = UnityEngine.Random.Range(0, freeArenas.Count);
                randomarena = freeArenas[random];
                busyArena.Add(randomarena);
                return randomarena;
            }
            return null;
        }

        static List<Arena> busyArena = new List<Arena>();
        static List<Arena> arenaList = new List<Arena>();
        void CreateDuelArena()
        {
            for (int i = 1; i < 8; i++)
            {
                int x = -3000;
                string path = $"Duel/DuelArena{i}";
                var data = Interface.GetMod().DataFileSystem.GetDatafile(path);
                if (data["default"] == null || data["entities"] == null)
                {
                    PrintError($"Нет файла DuelArena{i}");
                    return;
                }
                Arena arena = new Arena();

                arena.name = $"Арена{i}";

                if (i == 1)
                {
                    arena.player1pos = new Vector3(-2994.1f, 491.0f, 523.6f);
                    arena.player2pos = new Vector3(-2973.1f, 491.0f, 494.6f);

                    arena.teamblueSpawns.Add(new Vector3(-2988.8f, 491.0f, 531.6f));
                    arena.teamblueSpawns.Add(new Vector3(-2993.2f, 491.0f, 523.9f));
                    arena.teamblueSpawns.Add(new Vector3(-2994.6f, 491.0f, 522.8f));
                    arena.teamblueSpawns.Add(new Vector3(-3001.8f, 491.0f, 519.2f));
                    arena.teamblueSpawns.Add(new Vector3(-2996.0f, 491.0f, 526.5f));

                    arena.teamredSpawns.Add(new Vector3(-2970.9f, 491.0f, 491.6f));
                    arena.teamredSpawns.Add(new Vector3(-2973.7f, 491.0f, 494.1f));
                    arena.teamredSpawns.Add(new Vector3(-2972.5f, 491.0f, 495.3f));
                    arena.teamredSpawns.Add(new Vector3(-2978.6f, 491.0f, 489.9f));
                    arena.teamredSpawns.Add(new Vector3(-2967.0f, 491.0f, 498.6f));
                }
                if (i == 2)
                {
                    arena.player1pos = new Vector3(-2995.7f, 492.7f, 1005.0f);
                    arena.player2pos = new Vector3(-2980.1f, 492.7f, 1000.6f);
                }
                if (i == 3)
                {
                    arena.player1pos = new Vector3(-3002.7f, 498.7f, 1508.7f);
                    arena.player2pos = new Vector3(-2994.6f, 498.7f, 1493.6f);
                }
                if (i == 4)
                {
                    arena.player1pos = new Vector3(-3000.3f, 492.0f, 2011.3f);
                    arena.player2pos = new Vector3(-2975.3f, 492.0f, 2001.9f);
                }
                if (i == 5)
                {
                    arena.player1pos = new Vector3(-2985.5f, 491.7f, 2514.1f);
                    arena.player2pos = new Vector3(-2989.3f, 491.7f, 2496.8f);
                }
                if (i == 6)
                {
                    x = -2500;

                    arena.player1pos = new Vector3(-2515.1f, 500.0f, 18.7f);
                    arena.player2pos = new Vector3(-2484.1f, 500.0f, -22.4f);

                    arena.teamblueSpawns.Add(new Vector3(-2494.1f, 500.0f, -29.1f));
                    arena.teamblueSpawns.Add(new Vector3(-2489.5f, 500.0f, -25.2f));
                    arena.teamblueSpawns.Add(new Vector3(-2484.7f, 500.0f, -21.6f));
                    arena.teamblueSpawns.Add(new Vector3(-2479.8f, 500.0f, -18.2f));
                    arena.teamblueSpawns.Add(new Vector3(-2475.1f, 500.0f, -14.4f));

                    arena.teamredSpawns.Add(new Vector3(-2524.1f, 500.0f, 10.6f));
                    arena.teamredSpawns.Add(new Vector3(-2519.3f, 500.0f, 14.2f));
                    arena.teamredSpawns.Add(new Vector3(-2514.5f, 500.0f, 17.7f));
                    arena.teamredSpawns.Add(new Vector3(-2509.7f, 500.0f, 21.4f));
                    arena.teamredSpawns.Add(new Vector3(-2505.0f, 500.0f, 25.0f));
                    arenaList.Add(arena);
                    arena.pos = new Vector3(x, 500, 0);

                    var preloadData1 = PreLoadData(data["entities"] as List<object>, new Vector3(x, 500, 0), 1, true, true);
                    Paste(preloadData1, new Vector3(x, 500, 0), true);

                    continue;
                }
                if (i == 7)
                {
                    x = -2500;

                    arena.player1pos = new Vector3(-2500.6f, 500, 521.1f);
                    arena.player2pos = new Vector3(-2488.4f, 500, 476.5f);

                    arena.teamblueSpawns.Add(new Vector3(-2505.6f, 500, 470.6f));
                    arena.teamblueSpawns.Add(new Vector3(-2503.2f, 500, 468.3f));
                    arena.teamblueSpawns.Add(new Vector3(-2495.4f, 500, 473.7f));
                    arena.teamblueSpawns.Add(new Vector3(-2483.8f, 500, 476.7f));
                    arena.teamblueSpawns.Add(new Vector3(-2470.8f, 500, 480.3f));
                    arena.teamblueSpawns.Add(new Vector3(-2471.4f, 500, 476.9f));

                    arena.teamredSpawns.Add(new Vector3(-2483.6f, 500, 526.3f));
                    arena.teamredSpawns.Add(new Vector3(-2485.6f, 500, 528.7f));
                    arena.teamredSpawns.Add(new Vector3(-2494.3f, 500, 523.3f));
                    arena.teamredSpawns.Add(new Vector3(-2506.7f, 500, 519.9f));
                    arena.teamredSpawns.Add(new Vector3(-2518.2f, 500, 516.6f));
                    arena.teamredSpawns.Add(new Vector3(-2517.6f, 500, 519.9f));
                    PrintWarning("Все спауны созданы");
                    arenaList.Add(arena);
                    arena.pos = new Vector3(x, 500, 500);
                    PrintWarning("Все арены созданы");
                    var preloadData2 = PreLoadData(data["entities"] as List<object>, new Vector3(x, 500, 500), 1, true, true);
                    Paste(preloadData2, new Vector3(x, 500, 500), true);
                    continue;
                }
                arenaList.Add(arena);
                arena.pos = new Vector3(x, 500, i * 500);

                var preloadData = PreLoadData(data["entities"] as List<object>, new Vector3(x, 500, i * 500), 1, true, true);
                Paste(preloadData, new Vector3(x, 500, i * 500), true);
            }

        }

        List<Dictionary<string, object>> PreLoadData(List<object> entities, Vector3 startPos, float RotationCorrection, bool deployables, bool inventories)
        {
            var eulerRotation = new Vector3(0f, RotationCorrection, 0f);
            var quaternionRotation = Quaternion.EulerRotation(eulerRotation);
            var preloaddata = new List<Dictionary<string, object>>();
            foreach (var entity in entities)
            {
                var data = entity as Dictionary<string, object>;
                if (!deployables && !data.ContainsKey("grade")) continue;
                var pos = (Dictionary<string, object>)data["pos"];
                var rot = (Dictionary<string, object>)data["rot"];
                var fixedRotation = Quaternion.EulerRotation(eulerRotation + new Vector3(Convert.ToSingle(rot["x"]), Convert.ToSingle(rot["y"]), Convert.ToSingle(rot["z"])));
                var tempPos = quaternionRotation * (new Vector3(Convert.ToSingle(pos["x"]), Convert.ToSingle(pos["y"]), Convert.ToSingle(pos["z"])));
                Vector3 newPos = tempPos + startPos;
                data.Add("position", newPos);
                data.Add("rotation", fixedRotation);
                if (!inventories && data.ContainsKey("items")) data["items"] = new List<object>();
                preloaddata.Add(data);
            }
            return preloaddata;
        }

        List<BaseEntity> Paste(List<Dictionary<string, object>> entities, Vector3 startPos, bool checkPlaced)
        {
            bool unassignid = true;
            uint buildingid = 0;
            var pastedEntities = new List<BaseEntity>();
            foreach (var data in entities)
            {
                try
                {
                    var prefabname = (string)data["prefabname"];
                    var skinid = ulong.Parse(data["skinid"].ToString());
                    var pos = (Vector3)data["position"];
                    var rot = (Quaternion)data["rotation"];

                    var entity = GameManager.server.CreateEntity(prefabname, pos, rot, true);
                    if (entity != null)
                    {
                        entity.transform.position = pos;
                        entity.transform.rotation = rot;

                        var buildingblock = entity.GetComponentInParent<BuildingBlock>();
                        if (buildingblock != null)
                        {
                            buildingblock.blockDefinition = PrefabAttribute.server.Find<Construction>(buildingblock.prefabID);
                            buildingblock.SetGrade((BuildingGrade.Enum)data["grade"]);
                            if (unassignid)
                            {
                                buildingid = BuildingManager.server.NewBuildingID();
                                unassignid = false;
                            }
                            buildingblock.buildingID = buildingid;
                        }
                        entity.skinID = skinid;
                        entity.Spawn();
                        buildingblock.CancelInvoke("DecayTick");
                        buildingblock.decay = null;
                        buildingblock.SendNetworkUpdateImmediate();
                        bool killed = false;

                        if (killed) continue;

                        var basecombat = entity.GetComponentInParent<BaseCombatEntity>();
                        if (basecombat != null)
                        {
                            basecombat.SetHealth(basecombat.MaxHealth());
                        }

                        var box = entity.GetComponentInParent<StorageContainer>();
                        if (box != null)
                        {
                            box.inventory.Clear();
                            var items = data["items"] as List<object>;
                            var itemlist = new List<ItemAmount>();
                            foreach (var itemDef in items)
                            {
                                var item = itemDef as Dictionary<string, object>;
                                var itemid = Convert.ToInt32(item["id"]);
                                var itemamount = Convert.ToInt32(item["amount"]);
                                var itemskin = ulong.Parse(item["skinid"].ToString());
                                var itemcondition = Convert.ToSingle(item["condition"]);

                                var i = ItemManager.CreateByItemID(itemid, itemamount, itemskin);
                                if (i != null)
                                {
                                    i.condition = itemcondition;

                                    if (item.ContainsKey("magazine"))
                                    {
                                        var magazine = item["magazine"] as Dictionary<string, object>;
                                        var ammotype = int.Parse(magazine.Keys.ToArray()[0]);
                                        var ammoamount = int.Parse(magazine[ammotype.ToString()].ToString());
                                        var heldent = i.GetHeldEntity();
                                        if (heldent != null)
                                        {
                                            var projectiles = heldent.GetComponent<BaseProjectile>();
                                            if (projectiles != null)
                                            {
                                                projectiles.primaryMagazine.ammoType = ItemManager.FindItemDefinition(ammotype);
                                                projectiles.primaryMagazine.contents = ammoamount;
                                            }
                                        }
                                    }
                                    i?.MoveToContainer(box.inventory).ToString();
                                }
                            };
                        }
                        pastedEntities.Add(entity);
                        ArenaEntities.Add(entity);
                    }
                }
                catch (Exception e)
                {
                }
            }
            return pastedEntities;
        }
        #endregion

        #region Debug 

        public static void Debug(string message)
        {
            if (!debug) return;
            Interface.Oxide.LogWarning($"[Duel] {message}");
        }


        public List<DuelStavki> Tops = new List<DuelStavki>();
        public class DuelStavki
        {
            public DuelStavki(string SteamId, string Win, int item, int kolvo, bool weapon, int patron, string ammotype, int flamefuel)
            {
                this.SteamId = SteamId;
                this.Win = Win;
                this.item = item;
                this.kolvo = kolvo;
                this.weapon = weapon;
                this.patron = patron;
                this.ammotype = ammotype;
                this.flamefuel = flamefuel;
            }

            public string SteamId { get; set; }
            public string Win { get; set; }
            public int item { get; set; }
            public int kolvo { get; set; }
            public bool weapon { get; set; }
            public int patron { get; set; }
            public string ammotype { get; set; }
            public int flamefuel { get; set; }

        }
        #endregion

        #region Configuration Data

        private string box;
        private int slots;
        private float cooldownMinutes;
        private float maxRadius;
        private float pendingSeconds;
        private float radiationMax;

        [PluginReference]
        private Plugin Ignore, TruePVE;

        private Dictionary<string, DateTime> tradeCooldowns = new Dictionary<string, DateTime>();

        #endregion

        #region Trade State

        class OnlinePlayer
        {
            public BasePlayer Player;
            public StorageContainer View;
            public OpenTrade Trade;

            public PlayerInventory inventory
            {
                get
                {
                    return Player.inventory;
                }
            }

            public ItemContainer containerMain
            {
                get
                {
                    return Player.inventory.containerMain;
                }
            }

            public OnlinePlayer(BasePlayer player)
            {
            }

            public void Clear()
            {
                View = null;
                Trade = null;
            }
        }

        [OnlinePlayers]
        Hash<BasePlayer, OnlinePlayer> onlinePlayers = new Hash<BasePlayer, OnlinePlayer>();

        class OpenTrade
        {
            public OnlinePlayer source;
            public OnlinePlayer target;

            public BasePlayer sourcePlayer
            {
                get
                {
                    return source.Player;
                }
            }

            public BasePlayer targetPlayer
            {
                get
                {
                    return target.Player;
                }
            }

            public bool complete = false;
            public bool closing = false;

            public bool sourceAccept = false;
            public bool targetAccept = false;

            public OpenTrade(OnlinePlayer source, OnlinePlayer target)
            {
                this.source = source;
                this.target = target;
            }

            public OnlinePlayer GetOther(OnlinePlayer onlinePlayer)
            {
                if (source == onlinePlayer)
                {
                    return target;
                }

                return source;
            }

            public BasePlayer GetOther(BasePlayer player)
            {
                if (sourcePlayer == player)
                {
                    return targetPlayer;
                }

                return sourcePlayer;
            }

            public void ResetAcceptance()
            {
                sourceAccept = false;
                targetAccept = false;
            }

            public bool IsInventorySufficient()
            {
                if ((target.containerMain.capacity - target.containerMain.itemList.Count) < source.View.inventory.itemList.Count ||
                       (source.containerMain.capacity - source.containerMain.itemList.Count) < target.View.inventory.itemList.Count)
                {
                    return true;
                }

                return false;
            }

            public bool IsValid()
            {
                if (IsSourceValid() && IsTargetValid())
                    return true;

                return false;
            }

            public bool IsSourceValid()
            {
                if (sourcePlayer != null && sourcePlayer.IsConnected)
                    return true;

                return false;
            }

            public bool IsTargetValid()
            {
                if (targetPlayer != null && targetPlayer.IsConnected)
                    return true;

                return false;
            }
        }

        class PendingTrade
        {
            public BasePlayer Target;
            public Timer Timer;

            public PendingTrade(BasePlayer target)
            {
                Target = target;
            }

            public void Destroy()
            {
                if (Timer != null && !Timer.Destroyed)
                {
                    Timer.Destroy();
                }
            }
        }

        List<OpenTrade> openTrades = new List<OpenTrade>();
        Dictionary<BasePlayer, PendingTrade> pendingTrades = new Dictionary<BasePlayer, PendingTrade>();
        #endregion

        #region Initialization

        void Init()
        {
            Unsubscribe(nameof(CanNetworkTo));
        }

        void Loaded()
        {
            timer.Repeat(1f, 0, delegate
            {
                var check = (from x in Tops where x.Win != "" select x).Count();
                if (check > 0)
                {
                    DuelStavki con = (from x in Tops where x.Win != "" select x).FirstOrDefault();
                    string n1 = con.Win;
                    int n2 = con.item;
                    int n3 = con.kolvo;
                    bool weapon = con.weapon;
                    var item = ItemManager.CreateByItemID(n2, n3);
                    FlameThrower flameThrower = item.GetHeldEntity()?.GetComponent<FlameThrower>();
                    if (flameThrower)
                        flameThrower.ammo = con.flamefuel;
                    if (weapon)
                    {
                        BaseProjectile weapons = item.GetHeldEntity() as BaseProjectile;
                        weapons.primaryMagazine.contents = con.patron;
                        var def = ItemManager.FindItemDefinition(con.ammotype);
                        weapons.primaryMagazine.ammoType = def;
                    }

                    foreach (var player in BasePlayer.activePlayerList)
                    {
                        if (player.UserIDString.Equals(n1) && !player.IsDead() && !player.IsSleeping() && !player.IsWounded() && !IsDuelPlayer(player))
                        {
                            player.inventory.GiveItem(item);
                            Tops.Remove(con);
                        }
                    }
                }
            });

            timer.Repeat(4f, 0, delegate
            {
                var check2 = (from x in Tops where x.Win == "" select x).Count();
                if (check2 > 0)
                {
                    DuelStavki con = (from x in Tops where x.Win == "" select x).FirstOrDefault();

                    foreach (var player in BasePlayer.activePlayerList)
                    {

                        if (player.UserIDString.Equals(con.SteamId) && !player.IsDead() && !player.IsSleeping() && !player.IsWounded() && !IsDuelPlayer(player))
                        {
                            timer.Once(3f, delegate
                            {
                                if (player.UserIDString.Equals(con.SteamId) && !player.IsDead() && !player.IsSleeping() && !player.IsWounded() && !IsDuelPlayer(player))
                                {
                                    if (con == null) return;
                                    bool weapon = con.weapon;
                                    var item = ItemManager.CreateByItemID(con.item, con.kolvo);
                                    FlameThrower flameThrower = item.GetHeldEntity()?.GetComponent<FlameThrower>();
                                    if (flameThrower)
                                        flameThrower.ammo = con.flamefuel;
                                    if (weapon)
                                    {
                                        BaseProjectile weapons = item.GetHeldEntity() as BaseProjectile;
                                        weapons.primaryMagazine.contents = con.patron;
                                        var def = ItemManager.FindItemDefinition(con.ammotype);
                                        weapons.primaryMagazine.ammoType = def;

                                    }
                                    player.inventory.GiveItem(item);
                                    Tops.Remove(con);
                                }
                            });
                        }
                    }
                }
            });
            Tops = Interface.GetMod().DataFileSystem.ReadObject<List<DuelStavki>>("DuelStavki");
            db = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("Duel");
            LoadMessages();
            CheckConfig();
            box = GetConfig("Settings", "box", "assets/prefabs/deployable/woodenbox/woodbox_deployed.prefab");
            slots = GetConfig("Settings", "slots", 1);
            cooldownMinutes = GetConfig("Settings", "cooldownMinutes", 5f);
            maxRadius = GetConfig("Settings", "maxRadius", 5000f);
            pendingSeconds = GetConfig("Settings", "pendingSeconds", 25f);
            radiationMax = GetConfig("Settings", "radiationMax", 1f);
        }



        protected override void LoadDefaultConfig()
        {
            Config["Settings", "box"] = "assets/prefabs/deployable/woodenbox/woodbox_deployed.prefab";
            Config["Settings", "slots"] = 30;
            Config["Settings", "cooldownMinutes"] = 5;
            Config["Settings", "maxRadius"] = 5000f;
            Config["Settings", "pendingSeconds"] = 25f;
            Config["Settings", "radiationMax"] = 1;
            Config["VERSION"] = Version.ToString();
        }

        void CheckConfig()
        {
            if (Config["VERSION"] == null)
            {
                // FOR COMPATIBILITY WITH INITIAL VERSIONS WITHOUT VERSIONED CONFIG
                ReloadConfig();
            }
            else if (GetConfig<string>("VERSION", "") != Version.ToString())
            {
                // ADDS NEW, IF ANY, CONFIGURATION OPTIONS
                ReloadConfig();
            }
        }

        protected void ReloadConfig()
        {
            Config["VERSION"] = Version.ToString();

            // NEW CONFIGURATION OPTIONS HERE
            Config["Settings", "radiationMax"] = GetConfig("Settings", "radiationMax", 1f);
            // END NEW CONFIGURATION OPTIONS

            PrintToConsole("Upgrading configuration file");
            SaveConfig();
        }

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Inventory: You", "<color=#ADFF2F>[Дуэль]</color> У вас недостаточно места в инвентаре"},
                {"Inventory: Them", "<color=#ADFF2F>[Дуэль]</color> В его инвентаре недостаточно места"},
                {"Inventory: Generic", "<color=#ADFF2F>[Дуэль]</color> Недостаточное пространство инвентаря"},

                {"Player: Not Found", "<color=#ADFF2F>[Дуэль]</color> Не найдено ни одного игрока по этому имени"},
                {"Player: Unknown", "<color=#ADFF2F>[Дуэль]</color> Неизвестно"},
                {"Player: Yourself", "<color=#ADFF2F>[Дуэль]</color> Вы не можете дуэлиться с собой"},

                {"Status: Completing", "<color=#ADFF2F>[Дуэль]</color> Завершение приема ставок ..."},
                {"Status: No Pending", "<color=#ADFF2F>[Дуэль]</color> У вас нет запросов на дуэль"},
                {"Status: Pending", "<color=#ADFF2F>[Дуэль]</color> У него уже есть ожидающий запрос на дуэль"},
                {"Status: Received", "<color=#ADFF2F>[Дуэль]</color> Вы получили запрос на дуэль от {0}. Чтобы принять, введите <color=lime> /duels a </color>."},
                {"Status: They Interrupted", "<color=#ADFF2F>[Дуэль]</color> Игрок двинулся или закрыл окно ставок"},
                {"Status: You Interrupted", "<color=#ADFF2F>[Дуэль]</color> Вы двинулись или закрыли окно ставок"},

                {"Trade: Sent", "<color=#ADFF2F>[Дуэль]</color> Отправлен запрос на дуэль"},
                {"Trade: They Declined", "<color=#ADFF2F>[Дуэль]</color> Он отклонили ваш запрос на дуэль"},
                {"Trade: You Declined", "<color=#ADFF2F>[Дуэль]</color> Вы отклонили его запрос на дуэль"},
                {"Trade: They Accepted", "{0} принял."},
                {"Trade: You Accepted", "Вы приняли."},
                {"Trade: Pending", "Ожидание приема ставок."},

                {"Denied: Permission", "<color=#ADFF2F>[Дуэль]</color> У вас нет разрешения на это"},
                {"Denied: Privilege", "<color=#ADFF2F>[Дуэль]</color> Вы находитесь в чужой билд-зоне"},
                {"Denied: Swimming", "<color=#ADFF2F>[Дуэль]</color> Вы не можете сделать это во время плавания"},
                {"Denied: Falling", "<color=#ADFF2F>[Дуэль]</color> Вы не можете сделать это, падая"},
                {"Denied: Wounded", "<color=#ADFF2F>[Дуэль]</color> Вы не можете сделать это, пока раненые"},
                {"Denied: Irradiated", "<color=#ADFF2F>[Дуэль]</color> Вы не можете этого делать, пока облучаетесь"},
                {"Denied: Generic", "<color=#ADFF2F>[Дуэль]</color> Вы не можете сделать это прямо сейчас"},
                {"Denied: They Busy", "<color=#ADFF2F>[Дуэль]</color> Этот игрок занят"},
                {"Denied: They Ignored You", "О<color=#ADFF2F>[Дуэль]</color> н игнорирует вас"},
                {"Denied: Distance", "<color=#ADFF2F>[Дуэль]</color> Слишком далеко"},
                {"Item: BP", "BP"},
                {"Syntax: Trade Accept", "<color=#ADFF2F>[Дуэль]</color> Недопустимый синтаксис. /duels a"},
                {"Syntax: Trade", "<color=#ADFF2F>[Дуэль]</color> Недопустимый синтаксис. /duels \"ИМЯ ИГРОКА\""},
                {"Cooldown: Seconds", "<color=#ADFF2F>[Дуэль]</color> Вы делаете это слишком часто, повторите попытку через {0} секунд"},
                {"Cooldown: Minutes", "<color=#ADFF2F>[Дуэль]</color> Вы делаете это слишком часто, повторите попытку через {0} минут."}
            }, this);
        }

        #endregion

        #region Oxide Hooks

        object CanNetworkTo(BaseNetworkable entity, BasePlayer target)
        {
            if (entity == null || target == null || entity == target) return null;
            if (target.IsAdmin) return null;

            OnlinePlayer onlinePlayer;
            bool IsMyBox = false;
            if (onlinePlayers.TryGetValue(target, out onlinePlayer))
            {
                if (onlinePlayer.View != null && onlinePlayer.View.net.ID == entity.net.ID)
                {
                    IsMyBox = true;
                }
            }

            if (IsTradeBox(entity) && !IsMyBox) return false;

            return null;
        }

        void OnPlayerConnected(BasePlayer player)
        {
            onlinePlayers[player].View = null;
            onlinePlayers[player].Trade = null;
        }



        void OnPlayerLootEnd(PlayerLoot inventory)
        {
            var player = inventory.GetComponent<BasePlayer>();
            if (player == null)
                return;

            OnlinePlayer onlinePlayer;
            if (onlinePlayers.TryGetValue(player, out onlinePlayer) && onlinePlayer.View != null)
            {
                if (onlinePlayer.View == inventory.entitySource && onlinePlayer.Trade != null)
                {
                    OpenTrade t = onlinePlayer.Trade;

                    if (!t.closing)
                    {
                        t.closing = true;
                        if (!onlinePlayer.Trade.complete)
                        {
                            if (onlinePlayer.Trade.sourcePlayer == player)
                            {
                                TradeReply(t, "Status: They Interrupted", "Status: You Interrupted");
                            }
                            else
                            {
                                TradeReply(t, "Status: You Interrupted", "Status: They Interrupted");
                            }
                        }
                        CloseBoxView(player, (StorageContainer)inventory.entitySource);
                    }
                }
            }
        }

        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (container.playerOwner is BasePlayer)
            {
                OnlinePlayer onlinePlayer;
                if (onlinePlayers.TryGetValue(container.playerOwner, out onlinePlayer) && onlinePlayer.Trade != null)
                {
                    OpenTrade t = onlinePlayers[container.playerOwner].Trade;

                    if (!t.complete)
                    {
                        t.sourceAccept = false;
                        t.targetAccept = false;

                        if (t.IsValid())
                        {
                            ShowTrades(t, "Trade: Pending");
                        }
                        else
                        {
                            TradeCloseBoxes(t);
                        }
                    }
                }
            }
        }

        void OnItemRemovedFromContainer(ItemContainer container, Item item)
        {
            if (container.playerOwner is BasePlayer)
            {
                OnlinePlayer onlinePlayer;
                if (onlinePlayers.TryGetValue(container.playerOwner, out onlinePlayer) && onlinePlayer.Trade != null)
                {
                    OpenTrade t = onlinePlayers[container.playerOwner].Trade;
                    if (!t.complete)
                    {
                        t.sourceAccept = false;
                        t.targetAccept = false;

                        if (t.IsValid())
                        {
                            ShowTrades(t, "Trade: Pending");
                        }
                        else
                        {
                            TradeCloseBoxes(t);
                        }
                    }
                }
            }
        }

        #endregion

        #region Commands

        [ChatCommand("duels")]
        void cmdTrade(BasePlayer player, string command, string[] args)
        {
            if (!isIni)
            {
                SendReply(player, "<color=#FE5256>Duel не инициирована. Ожидайте........ </color>");
                return;
            }
            if (IsDuelPlayer(player))
            {
                player.ChatMessage("Вы уже дуэлянт");
                return;
            }
            if (IsPlayerOnActiveDuel(player))
            {
                player.ChatMessage("Вы уже находитесь на дуэли.");
                return;
            }
            if (IsRaidBlock(player)) return;
            if (player.metabolism.radiation_poison.value > 5)
            {
                SendReply(player, "У Вас облучение радиацией. Duel запрещена");
                return;
            }
            if (args.Length == 1)
            {
                if (args[0] == "a")
                {
                    if (pendingTrades.ContainsKey(player))
                    {
                        SendReply(player, GetMsg("Status: Pending", player));
                        return;
                    }
                    BasePlayer source = null;

                    foreach (KeyValuePair<BasePlayer, PendingTrade> kvp in pendingTrades)
                    {
                        if (kvp.Value.Target == player)
                        {
                            source = kvp.Key;
                            break;
                        }
                    }
                    if (IsDuelPlayer(source))
                    {
                        player.ChatMessage("Ваш противник уже находиться на Duel.");
                        return;
                    }
                    AcceptTrade(player);
                    return;
                }
            }

            /* if (createdTeamDuels.Count > 0)
                {
                    var teamDuel = createdTeamDuels[0];
                    if (teamDuel.requestPlayers.ContainsKey(player))
                    {
                        player.ChatMessage("Ты подал заявку на командную дуэль.\nНачать новую ты не можешь");
                        return;
                    }
                }*/
            //  if (!canAcceptRequest(player)) return;
            /*      if (!IsDuelPlayer(player))
                  {chatCom
                      SendReply(player, dontHaveRequest);
                      return;
                  }*/

            // AcceptRequest(player);

            if (args.Length != 1)
            {
                if (pendingTrades.ContainsKey(player))
                {
                    SendReply(player, GetMsg("Syntax: Trade Accept", player));
                }
                else
                {
                    SendReply(player, GetMsg("Syntax: Trade", player));
                }

                return;
            }

            var targetPlayer = FindPlayerByPartialName(args[0]);
            if (targetPlayer == null)
            {
                SendReply(player, GetMsg("Player: Not Found", player));
                return;
            }

            if (targetPlayer == player)
            {
                SendReply(player, GetMsg("Player: Yourself", player));
                return;
            }

            if (!CheckCooldown(player))
            {
                return;
            }

            if (Ignore != null)
            {
                var IsIgnored = Ignore.Call("IsIgnoredS", player.UserIDString, targetPlayer.UserIDString);
                if ((bool)IsIgnored == true)
                {
                    SendReply(player, GetMsg("Denied: They Ignored You", player));
                    return;
                }
            }

            OnlinePlayer onlineTargetPlayer;
            if (onlinePlayers.TryGetValue(targetPlayer, out onlineTargetPlayer) && onlineTargetPlayer.Trade != null)
            {
                SendReply(player, GetMsg("Denied: They Busy", player));
                return;
            }

            if (maxRadius > 0)
            {
                if (targetPlayer.Distance(player) > maxRadius)
                {
                    SendReply(player, GetMsg("Denied: Distance", player));
                    return;
                }
            }


            if (pendingTrades.ContainsKey(player))
            {
                SendReply(player, GetMsg("Status: Pending", player));
            }
            else
            {
                SendReply(targetPlayer, GetMsg("Status: Received", targetPlayer), player.displayName);
                SendReply(player, GetMsg("Trade: Sent", player));
                var pendingTrade = new PendingTrade(targetPlayer);
                pendingTrades.Add(player, pendingTrade);

                pendingTrade.Timer = timer.In(pendingSeconds, delegate ()
                {
                    if (pendingTrades.ContainsKey(player))
                    {
                        pendingTrades.Remove(player);
                        SendReply(player, GetMsg("Trade: They Declined", player));
                        SendReply(targetPlayer, GetMsg("Trade: You Declined", targetPlayer));
                    }
                });
            }
        }

        [ConsoleCommand("duels")]
        void ccTrade(ConsoleSystem.Arg arg)
        {
            string[] args = new string[0];
            if (arg.HasArgs()) args = arg.Args;
            if (args.Length == 0)
            {
                return;
            }
            cmdTrade(arg.Connection.player as BasePlayer, arg.cmd.Name, arg.Args);

        }

        [ConsoleCommand("duels.decline")]
        void ccTradeDecline(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;

            OnlinePlayer onlinePlayer;
            if (onlinePlayers.TryGetValue(player, out onlinePlayer) && onlinePlayer.Trade != null)
            {
                onlinePlayer.Trade.closing = true;
                var target = onlinePlayer.Trade.GetOther(player);
                SendReply(player, GetMsg("Trade: You Declined", player));
                SendReply(target, GetMsg("Trade: They Declined", target));

                TradeCloseBoxes(onlinePlayer.Trade);
            }
        }

        [ConsoleCommand("duels.accept")]
        void ccTradeAccept(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;

            OnlinePlayer onlinePlayer;
            if (onlinePlayers.TryGetValue(player, out onlinePlayer) && onlinePlayer.Trade != null)
            {
                var t = onlinePlayers[player].Trade;
                if (t.sourcePlayer == player)
                {
                    var i = t.target.View.inventory.itemList.Count;
                    var f = t.source.containerMain.capacity - t.source.containerMain.itemList.Count;
                    if (i > f)
                    {

                        TradeReply(t, "Inventory: Them", "Inventory: You");

                        t.sourceAccept = false;
                        ShowTrades(t, "Inventory: Generic");
                        return;
                    }

                    t.sourceAccept = true;
                }
                else if (t.targetPlayer == player)
                {
                    var i = t.source.View.inventory.itemList.Count;
                    var f = t.target.containerMain.capacity - t.target.containerMain.itemList.Count;
                    if (i > f)
                    {
                        TradeReply(t, "Inventory: You", "Inventory: Them");
                        t.targetAccept = false;
                        ShowTrades(t, "Inventory: Generic");
                        return;
                    }

                    t.targetAccept = true;
                }

                if (t.targetAccept == true && t.sourceAccept == true)
                {
                    if (t.IsInventorySufficient())
                    {
                        t.ResetAcceptance();
                        ShowTrades(t, "Inventory: Generic");
                        return;
                    }
                    if (t.complete)
                    {
                        return;
                    }
                    t.complete = true;

                    TradeCooldown(t);
                    CreateRequest(t.sourcePlayer, t.targetPlayer);
                    AcceptRequest(t.targetPlayer);

                    TradeReply(t, "Status: Completing");

                    timer.In(0.1f, () => FinishTrade(t));
                }
                else
                {
                    ShowTrades(t, "Trade: Pending");
                }
            }
        }

        #endregion

        #region GUI

        public string jsonTrade = @"[{""name"":""TradeMsg"",""parent"":""Overlay"",""components"":[{""type"":""UnityEngine.UI.Image"",""color"":""0 0 0 0.76"",""imagetype"":""Filled""},{""type"":""RectTransform"",""anchormax"":""0.77 0.91"",""anchormin"":""0.24 0.52""}]},{""name"":""SourceLabel{1}"",""parent"":""TradeMsg"",""components"":[{""type"":""UnityEngine.UI.Text"",""text"":""{sourcename}"",""fontSize"":""16"",""align"":""UpperLeft""},{""type"":""RectTransform"",""anchormax"":""0.48 0.98"",""anchormin"":""0.03 0.91""}]},{""name"":""TargetLabel{2}"",""parent"":""TradeMsg"",""components"":[{""type"":""UnityEngine.UI.Text"",""text"":""{targetname}"",""fontSize"":""17""},{""type"":""RectTransform"",""anchormax"":""0.97 0.98"",""anchormin"":""0.52 0.91""}]},{""name"":""SourceItemsPanel{3}"",""parent"":""TradeMsg"",""components"":[{""type"":""UnityEngine.UI.RawImage"",""color"":""0 0 0 0.52"",""imagetype"":""Filled""},{""type"":""RectTransform"",""anchormax"":""0.47 0.9"",""anchormin"":""0.03 0.13""}]},{""name"":""SourceItemsText"",""parent"":""SourceItemsPanel{3}"",""components"":[{""type"":""UnityEngine.UI.Text"",""text"":""{sourceitems}"",""fontSize"":""14"",""align"":""UpperLeft""},{""type"":""RectTransform"",""anchormax"":""0.99 0.99"",""anchormin"":""0.01 0.01""}]},{""name"":""TargetItemsPanel{4}"",""parent"":""TradeMsg"",""components"":[{""type"":""UnityEngine.UI.RawImage"",""color"":""0 0 0 0.52"",""imagetype"":""Filled""},{""type"":""RectTransform"",""anchormax"":""0.96 0.9"",""anchormin"":""0.52 0.13""}]},{""name"":""TargetItemsText"",""parent"":""TargetItemsPanel{4}"",""components"":[{""type"":""UnityEngine.UI.Text"",""text"":""{targetitems}"",""fontSize"":""14"",""align"":""UpperLeft""},{""type"":""RectTransform"",""anchormax"":""0.99 0.99"",""anchormin"":""0.01 0.01""}]},{""name"":""AcceptTradeButton{5}"",""parent"":""TradeMsg"",""components"":[{""type"":""UnityEngine.UI.Button"",""color"":""0 0.95 0.14 0.54"",""command"":""duels.accept""},{""type"":""RectTransform"",""anchormax"":""0.47 0.09"",""anchormin"":""0.35 0.03""}]},{""name"":""AcceptTradeLabel"",""parent"":""AcceptTradeButton{5}"",""components"":[{""type"":""UnityEngine.UI.Text"",""text"":""Принять"",""fontSize"":""13"",""align"":""MiddleCenter""},{""type"":""RectTransform"",""anchormax"":""1 1"",""anchormin"":""0 0""}]},{""name"":""DeclineTradeButton{6}"",""parent"":""TradeMsg"",""components"":[{""type"":""UnityEngine.UI.Button"",""color"":""0.95 0 0.02 0.61"",""command"":""duels.decline""},{""type"":""RectTransform"",""anchormax"":""0.15 0.09"",""anchormin"":""0.03 0.03""}]},{""name"":""DeclineTradeLabel"",""parent"":""DeclineTradeButton{6}"",""components"":[{""type"":""UnityEngine.UI.Text"",""text"":""Отклонить"",""fontSize"":""13"",""align"":""MiddleCenter""},{""type"":""RectTransform"",""anchormax"":""1 1"",""anchormin"":""0 0""}]},{""name"":""TargetStatusLabel{7}"",""parent"":""TradeMsg"",""components"":[{""type"":""UnityEngine.UI.Text"",""text"":""{targetstatus}"",""fontSize"":""14"",""align"":""UpperLeft""},{""type"":""RectTransform"",""anchormax"":""0.97 0.09"",""anchormin"":""0.52 0.01""}]}]
";
        private void ShowTrade(BasePlayer player, OpenTrade trade, string status)
        {
            HideTrade(player);

            OnlinePlayer onlinePlayer;
            if (!onlinePlayers.TryGetValue(player, out onlinePlayer))
            {
                return;
            }

            if (onlinePlayer.View == null)
            {
                return;
            }

            StorageContainer sourceContainer = onlinePlayer.View;
            StorageContainer targetContainer = null;
            BasePlayer target = null;

            if (trade.sourcePlayer == player && trade.target.View != null)
            {
                targetContainer = trade.target.View;
                target = trade.targetPlayer;
                if (target is BasePlayer)
                {
                    if (trade.targetAccept)
                    {
                        status += string.Format(GetMsg("Trade: They Accepted", player), CleanName(target.displayName));
                    }
                    else if (trade.sourceAccept)
                    {
                        status += GetMsg("Trade: You Accepted", player);
                    }
                }
                else
                {
                    return;
                }
            }
            else if (trade.targetPlayer == player && trade.source.View != null)
            {
                targetContainer = trade.source.View;
                target = trade.sourcePlayer;
                if (target is BasePlayer)
                {
                    if (trade.sourceAccept)
                    {
                        status += string.Format(GetMsg("Trade: They Accepted", player), CleanName(target.displayName));
                    }
                    else if (trade.targetAccept)
                    {
                        status += GetMsg("Trade: You Accepted", player);
                    }
                }
                else
                {
                    return;
                }
            }

            if (targetContainer == null || target == null)
            {
                return;
            }

            string send = jsonTrade;
            for (int i = 1; i < 100; i++)
            {
                send = send.Replace("{" + i + "}", Oxide.Core.Random.Range(9999, 99999).ToString());
            }

            send = send.Replace("{sourcename}", CleanName(player.displayName));
            if (target != null)
            {
                send = send.Replace("{targetname}", CleanName(target.displayName));
            }
            else
            {
                send = send.Replace("{targetname}", GetMsg("Player: Unknown", player));
            }
            send = send.Replace("{targetstatus}", status);

            List<string> sourceItems = new List<string>();
            foreach (Item i in sourceContainer.inventory.itemList)
            {
                string n = "";
                if (i.IsBlueprint())
                {
                    n = i.amount + " x <color=lightblue>" + i.blueprintTargetDef.displayName.english + " [" + GetMsg("Item: BP", player) + "]</color>";
                }
                else
                {
                    n = i.amount + " x " + i.info.displayName.english;
                }

                sourceItems.Add(n);
            }

            send = send.Replace("{sourceitems}", string.Join("\n", sourceItems.ToArray()));

            if (player != target)
            {
                List<string> targetItems = new List<string>();
                if (targetContainer != null)
                {
                    foreach (Item i in targetContainer.inventory.itemList)
                    {
                        string n2 = "";
                        if (i.IsBlueprint())
                        {
                            n2 = i.amount + " x <color=lightblue>" + i.blueprintTargetDef.displayName.english + " [" + GetMsg("Item: BP", player) + "]</color>";
                        }
                        else
                        {
                            n2 = i.amount + " x " + i.info.displayName.english;
                        }
                        targetItems.Add(n2);
                    }
                }

                send = send.Replace("{targetitems}", string.Join("\n", targetItems.ToArray()));
            }
            else
            {
                send = send.Replace("{targetitems}", "");
            }



            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "AddUI", send);
        }

        private void HideTrade(BasePlayer player)
        {
            if (player.IsConnected)
            {
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", "TradeMsg");
            }
        }

        #endregion

        #region Core Methods

        bool CheckCooldown(BasePlayer player)
        {
            if (cooldownMinutes > 0)
            {
                DateTime startTime;
                if (tradeCooldowns.TryGetValue(player.UserIDString, out startTime))
                {
                    var endTime = DateTime.Now;

                    var span = endTime.Subtract(startTime);
                    if (span.TotalMinutes > 0 && span.TotalMinutes < Convert.ToDouble(cooldownMinutes))
                    {
                        double timeleft = System.Math.Round(Convert.ToDouble(cooldownMinutes) - span.TotalMinutes, 2);
                        if (timeleft < 1)
                        {
                            double timelefts = System.Math.Round((Convert.ToDouble(cooldownMinutes) * 60) - span.TotalSeconds);
                            SendReply(player, string.Format(GetMsg("Cooldown: Seconds", player), timelefts.ToString()));
                        }
                        else
                        {
                            SendReply(player, string.Format(GetMsg("Cooldown: Minutes", player), System.Math.Round(timeleft).ToString()));
                        }
                        return false;
                    }
                    else
                    {
                        tradeCooldowns.Remove(player.UserIDString);
                    }
                }
            }

            return true;
        }

        void TradeCloseBoxes(OpenTrade trade)
        {
            if (trade.IsSourceValid())
            {
                CloseBoxView(trade.sourcePlayer, trade.source.View);
            }

            if (trade.IsTargetValid())
            {
                CloseBoxView(trade.targetPlayer, trade.target.View);
            }
        }

        void TradeReply(OpenTrade trade, string msg, string msg2 = null)
        {
            if (msg2 == null)
            {
                msg2 = msg;
            }
            SendReply(trade.targetPlayer, GetMsg(msg, trade.targetPlayer));
            SendReply(trade.sourcePlayer, GetMsg(msg2, trade.sourcePlayer));
        }

        void ShowTrades(OpenTrade trade, string msg)
        {
            ShowTrade(trade.sourcePlayer, trade, GetMsg(msg, trade.sourcePlayer));
            ShowTrade(trade.targetPlayer, trade, GetMsg(msg, trade.targetPlayer));
        }

        void TradeCooldown(OpenTrade trade)
        {
            PlayerCooldown(trade.targetPlayer);
            PlayerCooldown(trade.sourcePlayer);
        }

        void PlayerCooldown(BasePlayer player)
        {
            if (player.IsAdmin)
            {
                return;
            }
            if (tradeCooldowns.ContainsKey(player.UserIDString))
            {
                tradeCooldowns.Remove(player.UserIDString);
            }

            tradeCooldowns.Add(player.UserIDString, DateTime.Now);
        }

        void FinishTrade(OpenTrade t)
        {
            var source = t.source.View;
            var target = t.target.View;

            TradeCloseBoxes(t);
        }

        void AcceptTrade(BasePlayer player)
        {
            BasePlayer source = null;

            PendingTrade pendingTrade = null;

            foreach (KeyValuePair<BasePlayer, PendingTrade> kvp in pendingTrades)
            {
                if (kvp.Value.Target == player)
                {
                    pendingTrade = kvp.Value;
                    source = kvp.Key;
                    break;
                }
            }
            if (source != null && pendingTrade != null)
            {
                pendingTrade.Destroy();
                pendingTrades.Remove(source);
                StartTrades(source, player);
            }
            else
            {
                SendReply(player, GetMsg("Status: No Pending", player));
            }
        }

        void StartTrades(BasePlayer source, BasePlayer target)
        {
            var trade = new OpenTrade(onlinePlayers[source], onlinePlayers[target]);
            StartTrade(source, target, trade);
            if (source != target)
            {
                StartTrade(target, source, trade);
            }
        }

        void StartTrade(BasePlayer source, BasePlayer target, OpenTrade trade)
        {
            OpenBox(source, source);

            if (!openTrades.Contains(trade))
            {
                openTrades.Add(trade);
            }
            onlinePlayers[source].Trade = trade;

            timer.In(0.1f, () => ShowTrade(source, trade, GetMsg("Trade: Pending", source)));
        }

        void OpenBox(BasePlayer player, BaseEntity target)
        {
            Subscribe(nameof(CanNetworkTo));
            var ply = onlinePlayers[player];
            if (ply.View == null)
            {
                OpenBoxView(player, target);
                return;
            }

            CloseBoxView(player, ply.View);
            timer.In(1f, () => OpenBoxView(player, target));
        }

        void OpenBoxView(BasePlayer player, BaseEntity targArg)
        {
            var pos = new Vector3(player.transform.position.x, player.transform.position.y - 1, player.transform.position.z);
            var corpse = GameManager.server.CreateEntity(box, pos) as StorageContainer;
            corpse.transform.position = pos;

            if (!corpse) return;

            StorageContainer view = corpse as StorageContainer;
            player.EndLooting();
            if (targArg is BasePlayer)
            {

                BasePlayer target = targArg as BasePlayer;
                ItemContainer container = new ItemContainer();
                container.playerOwner = player;
                container.ServerInitialize((Item)null, slots);
                if ((int)container.uid == 0)
                    container.GiveUID();

                view.enableSaving = false;
                view.Spawn();
                view.inventory = container;

                onlinePlayers[player].View = view;
                timer.In(0.1f, () => view.PlayerOpenLoot(player));
            }
        }

        void CloseBoxView(BasePlayer player, StorageContainer view)
        {

            OnlinePlayer onlinePlayer;
            if (!onlinePlayers.TryGetValue(player, out onlinePlayer)) return;
            if (onlinePlayer.View == null) return;

            HideTrade(player);
            if (onlinePlayer.Trade != null)
            {
                OpenTrade t = onlinePlayer.Trade;
                t.closing = true;

                if (t.sourcePlayer == player && t.targetPlayer != player && t.target.View != null)
                {
                    t.target.Trade = null;
                    CloseBoxView(t.targetPlayer, t.target.View);
                }
                else if (t.targetPlayer == player && t.sourcePlayer != player && t.source.View != null)
                {
                    t.source.Trade = null;
                    CloseBoxView(t.sourcePlayer, t.source.View);
                }

                if (openTrades.Contains(t))
                {
                    openTrades.Remove(t);
                }


            }

            if (view.inventory.itemList.Count > 0)
            {
                foreach (Item item in view.inventory.itemList.ToArray())
                {
                    if (item.position != -1)
                    {
                        BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
                        if (weapon != null)
                        {
                            Tops.Add(new DuelStavki(player.UserIDString, "", item.info.itemid, item.amount, true, weapon.primaryMagazine != null ? weapon.primaryMagazine.contents : 0, weapon.primaryMagazine != null ? weapon.primaryMagazine.ammoType.shortname : "", 0));
                        }
                        else
                        {
                            //FlameThrower flameThrower = item.GetHeldEntity()?.GetComponent<FlameThrower>();
                            //if (flameThrower != null)
                            //    iItem.flamefuel = flameThrower.ammo;
                            FlameThrower flameThrower = item.GetHeldEntity()?.GetComponent<FlameThrower>();
                            Tops.Add(new DuelStavki(player.UserIDString, "", item.info.itemid, item.amount, false, 0, "", flameThrower != null ? flameThrower.ammo : 0));
                        }

                        //item.MoveToContainer(player.inventory.containerMain);
                    }
                }
            }
            player.EndLooting();
            player.inventory.loot.entitySource = null;
            player.inventory.loot.itemSource = null;
            player.inventory.loot.containers = new List<ItemContainer>();
            view.inventory = new ItemContainer();
            onlinePlayer.Clear();
            view.Kill(BaseNetworkable.DestroyMode.None);

            if (onlinePlayers.Values.Count(p => p.View != null) <= 0)
            {
                Unsubscribe(nameof(CanNetworkTo));
            }
        }

        bool CanPlayerTrade(BasePlayer player, string perm)
        {
            if (!permission.UserHasPermission(player.UserIDString, perm))
            {
                SendReply(player, GetMsg("Denied: Permission", player));
                return false;
            }
            if (!player.CanBuild())
            {
                SendReply(player, GetMsg("Denied: Privilege", player));
                return false;
            }
            if (radiationMax > 0 && player.radiationLevel > radiationMax)
            {
                SendReply(player, GetMsg("Denied: Irradiated", player));
                return false;
            }
            if (player.IsSwimming())
            {
                SendReply(player, GetMsg("Denied: Swimming", player));
                return false;
            }
            if (!player.IsOnGround())
            {
                SendReply(player, GetMsg("Denied: Falling", player));
                return false;
            }
            if (player.IsFlying)
            {
                SendReply(player, GetMsg("Denied: Falling", player));
                return false;
            }
            if (player.IsWounded())
            {
                SendReply(player, GetMsg("Denied: Wounded", player));
                return false;
            }

            var canTrade = Interface.Call("CanTrade", player);
            if (canTrade != null)
            {
                if (canTrade is string)
                {
                    SendReply(player, Convert.ToString(canTrade));
                }
                else
                {
                    SendReply(player, GetMsg("Denied: Generic", player));
                }
                return false;
            }

            return true;
        }

        #endregion

        #region HelpText
        private void SendHelpText(BasePlayer player)
        {
            var sb = new StringBuilder()
               .Append("")
               .Append("  ").Append("<color=\"#ffd479\">/duel \"Player Name\"</color> - Кинуть вызов игроку").Append("\n")
               .Append("  ").Append("<color=\"#ffd479\">/duel accept</color> - Принять вызов на дуэль").Append("\n");
            player.ChatMessage(sb.ToString());
        }
        #endregion

        #region Helper methods

        private bool IsTradeBox(BaseNetworkable entity)
        {
            foreach (KeyValuePair<BasePlayer, OnlinePlayer> kvp in onlinePlayers)
            {
                if (kvp.Value.View != null && kvp.Value.View.net.ID == entity.net.ID)
                {
                    return true;
                }
            }

            return false;
        }

        bool hasAccess(BasePlayer player, string permissionname)
        {
            if (player.IsAdmin) return true;
            return permission.UserHasPermission(player.UserIDString, permissionname);
        }

        private BasePlayer FindPlayerByPartialName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            BasePlayer player = null;
            name = name.ToLower();
            var awakePlayers = BasePlayer.activePlayerList.ToArray();
            foreach (var p in awakePlayers)
            {
                if (p.net == null || p.net.connection == null)
                    continue;

                if (p.displayName == name)
                {
                    if (player != null)
                        return null;
                    player = p;
                }
            }

            if (player != null)
                return player;
            foreach (var p in awakePlayers)
            {
                if (p.net == null || p.net.connection == null)
                    continue;

                if (p.displayName.ToLower().IndexOf(name) >= 0)
                {
                    if (player != null)
                        return null;
                    player = p;
                }
            }

            return player;
        }



        private T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null)
            {
                return defaultValue;
            }

            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        private T GetConfig<T>(string name, string name2, T defaultValue)
        {
            if (Config[name, name2] == null)
            {
                return defaultValue;
            }

            return (T)Convert.ChangeType(Config[name, name2], typeof(T));
        }

        string GetMsg(string key, BasePlayer player = null)
        {
            return lang.GetMessage(key, this, player == null ? null : player.UserIDString);
        }

        private string CleanName(string name)
        {
            return JsonConvert.ToString(name.Trim()).Replace("\"", "");
        }

        #endregion
    }
}