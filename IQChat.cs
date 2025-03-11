using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CompanionServer;
using ConVar;
using Facepunch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using UnityEngine.Networking;

namespace Oxide.Plugins
{
    [Info("IQChat", "SkuliDropek", "2.3.46.2")]
    [Description("Самый приятный чат для вашего сервера из ветки IQ")]
    class IQChat : RustPlugin
    {
        /// <summary>
        /// Обновление 2.2.x
        /// - Исправил возможность использования тегов разметки в /pm и /r
        /// - Для дата-файла был изменен путь, новый путь - IQSystem/IQChat (Плагин сам перенесет все данные и сообщит вам об этом в консоль, после сообщения об успешном перемещении и проверки новых файлов - можно удалять старые файлы) 
        /// - Изменен и улучшен метод поиска "плохих слов" в чате. Добавлено игнорирование регистра слов
        /// - Изменена проверка на выдачу автоматического мута за "плохие слова"
        /// - В форматирование ников добавлена "защита от ссылок" - они будут удаляться
        /// - Изменен и улучшен метод поиска "запрещенных ников". Добавлено игнорирование регистра слов
        /// - Добавлен функционал "Защита от нубов", игроки, которые впервые подключились не смогут писать в чат/pm/r N время, настраивается в конфигурации
        /// - При включении "Защиты от нубов", старые игроки будут перенесены в защиту
        /// Обновление 2.3.х
        /// - Корректировка форматирования CAPS

        #region Reference
        [PluginReference] Plugin ImageLibrary, IQPersonal, IQFakeActive, IQRankSystem;

        #region ImageLibrary
        private String GetImage(String fileName, UInt64 skin = 0)
        {
            var imageId = (String)plugins.Find("ImageLibrary").CallHook("ImageUi.GetImage", fileName, skin);
            if (!string.IsNullOrEmpty(imageId))
                return imageId;
            return String.Empty;
        }
        public Boolean AddImage(String url, String shortname, UInt64 skin = 0) => (Boolean)ImageLibrary?.Call("AddImage", url, shortname, skin);
        #endregion

        #region IQPersonal
        public void IQPersonalSendSetMute(BasePlayer player) => IQPersonal?.CallHook("API_SET_MUTE", player.userID);
        public void IQPersonalSendBadWords(BasePlayer player) => IQPersonal?.CallHook("API_DETECTED_BAD_WORDS", player.userID);
        #endregion

        #region IQFakeActive
        public void RemoveReserved(UInt64 userID)
        {
            if (!IQFakeActive) return;
            IQFakeActive?.Call("RemoveReserver", userID);
        }
        public bool IsFake(UInt64 userID)
        {
            if (!IQFakeActive) return false;
            return (bool)IQFakeActive?.Call("IsFake", userID);
        }
        public bool IsFake(String DisplayName)
        {
            if (!IQFakeActive) return false;

            return (bool)IQFakeActive?.Call("IsFake", DisplayName);
        }
        void SyncReservedFinish(string JSON)
        {
            if (!config.ReferenceSetting.IQFakeActiveSettings.UseIQFakeActive) return;
            List<FakePlayer> ContentDeserialize = JsonConvert.DeserializeObject<List<FakePlayer>>(JSON);
            PlayerBases = ContentDeserialize;

            PrintWarning("IQChat - успешно синхронизирована с IQFakeActive");
            PrintWarning("=============SYNC==================");
        }

        public string FindFakeName(ulong userID) => (string)IQFakeActive?.Call("FindFakeName", userID);
        public List<FakePlayer> PlayerBases = new List<FakePlayer>();
        public class FakePlayer
        {
            public ulong UserID;
            public string DisplayName;
        }
        #endregion

        #region IQRankSystem
        String IQRankGetRank(ulong userID) => (string)(IQRankSystem?.Call("API_GET_RANK_NAME", userID));
        String IQRankGetTimeGame(ulong userID) => (string)(IQRankSystem?.Call("API_GET_TIME_GAME", userID));
        List<String> IQRankListKey(ulong userID) => (List<string>)(IQRankSystem?.Call("API_RANK_USER_KEYS", userID));
        String IQRankGetNameRankKey(string Key) => (string)(IQRankSystem?.Call("API_GET_RANK_NAME", Key));
        void IQRankSetRank(ulong userID, string RankKey) => IQRankSystem?.Call("API_SET_ACTIVE_RANK", userID, RankKey);

        #endregion

        #endregion

        #region Vars
        private static IQChat _;
        static Double CurrentTime => Facepunch.Math.Epoch.Current;
        public enum MuteType
        {
            Chat,
            Voice
        }
        private enum SelectedAction
        {
            Mute,
            Ignore
        }
        private enum SelectedParametres
        {
            DropList,
            Slider
        }
        private enum TakeElementUser
        {
            Prefix,
            Nick,
            Chat,
            Rank,
            MultiPrefix
        }
        private enum ElementsSettingsType
        {
            PM,
            Broadcast,
            Alert,
            Sound
        }
        public Dictionary<BasePlayer, BasePlayer> PMHistory = new Dictionary<BasePlayer, BasePlayer>();
        public Dictionary<BasePlayer, List<String>> LastMessagesChat = new Dictionary<BasePlayer, List<String>>();

        private const String PermissionHideOnline = "iqchat.onlinehide";
        private const String PermissionMute = "iqchat.muteuse";
        private const String PermissionAlert = "iqchat.alertuse";
        private const String PermissionRename = "iqchat.renameuse";
        private const String PermissionAntiSpam = "iqchat.antispamabuse";
        private const String PermissionHideConnection = "iqchat.hideconnection";
        private const String PermissionHideDisconnection = "iqchat.hidedisconnection";
        private const String PermissionMutedAdmin = "iqchat.adminmuted";
        class Response
        {
            [JsonProperty("country")]
            public string Country { get; set; }
        }
        public static StringBuilder sb = new StringBuilder();
        public string GetLang(string LangKey, string userID = null, params object[] args)
        {
            sb.Clear();
            if (args != null)
            {
                sb.AppendFormat(lang.GetMessage(LangKey, this, userID), args);
                return sb.ToString();
            }
            return lang.GetMessage(LangKey, this, userID);
        }
        #endregion

        #region Configuration

        private static Configuration config = new Configuration();
        private class Configuration
        {
            #region Controller Connection
            [JsonProperty("Настройка информации о игроке")]
            public ControllerConnection ControllerConnect = new ControllerConnection();
            internal class ControllerConnection
            {
                [JsonProperty("Перключатели функций")]
                public Turned Turneds = new Turned();
                [JsonProperty("Настройка стандартных значений")]
                public SetupDefault SetupDefaults = new SetupDefault();

                internal class SetupDefault
                {
                    [JsonProperty("Данный префикс установится если игрок впервые зашел на сервер или в случае окончания прав на префикс, который у него стоял ранее")]
                    public String PrefixDefault = "<color=#CC99FF>[ИГРОК]</color>";
                    [JsonProperty("Данный цвет ника установится если игрок впервые зашел на сервер или в случае окончания прав на цвет ника, который у него стоял ранее")]
                    public String NickDefault = "#33CCCC";
                    [JsonProperty("Данный цвет чата установится если игрок впервые зашел на сервер или в случае окончания прав на цвет чата, который у него стоял ранее")]
                    public String MessageDefault = "#0099FF";
                }
                internal class Turned
                {
                    [JsonProperty("Устанавливать автоматически префикс игроку, когда он получил права на него")]
                    public Boolean TurnAutoSetupPrefix;
                    [JsonProperty("Устанавливать автоматически цвет ника игроку, когда он получил права на него")]
                    public Boolean TurnAutoSetupColorNick;
                    [JsonProperty("Устанавливать автоматически цвет чата игроку, когда он получил права на него")]
                    public Boolean TurnAutoSetupColorChat;
                    [JsonProperty("Сбрасывать автоматически префикс при окончании прав на него у игрока")]
                    public Boolean TurnAutoDropPrefix;
                    [JsonProperty("Сбрасывать автоматически цвет ника при окончании прав на него у игрока")]
                    public Boolean TurnAutoDropColorNick;
                    [JsonProperty("Сбрасывать автоматически цвет чата при окончании прав на него у игрока")]
                    public Boolean TurnAutoDropColorChat;
                }
            }
            #endregion

            #region Controller Parameters
            [JsonProperty("Настройка параметров для игрока")]
            public ControllerParameters ControllerParameter = new ControllerParameters();
            internal class ControllerParameters
            {
                [JsonProperty("Настройка отображения параметров для выбора игрока")]
                public VisualSettingParametres VisualParametres = new VisualSettingParametres();
                [JsonProperty("Список и настройка цветов для ника")]
                public List<AdvancedFuncion> NickColorList = new List<AdvancedFuncion>();
                [JsonProperty("Список и настройка цветов для сообщений в чате")]
                public List<AdvancedFuncion> MessageColorList = new List<AdvancedFuncion>();
                [JsonProperty("Список и настройка префиксов в чате")]
                public PrefixSetting Prefixes = new PrefixSetting();
                internal class PrefixSetting
                {
                    [JsonProperty("Включить поддержку нескольких префиксов сразу (true - можно установить несколько префиксов/false - установить можно только 1 на выбор)")]
                    public Boolean TurnMultiPrefixes;
                    [JsonProperty("Максимальное количество префиксов, которое можно установить за раз(Данный параметр работает только если включена установка нескольких префиксов)")]
                    public Int32 MaximumMultiPrefixCount;
                    [JsonProperty("Список префиксов и их настройка")]
                    public List<AdvancedFuncion> Prefixes = new List<AdvancedFuncion>();
                }

                internal class AdvancedFuncion
                {
                    [JsonProperty("Права")]
                    public String Permissions;
                    [JsonProperty("Значение")]
                    public String Argument;
                }

                internal class VisualSettingParametres
                {
                    [JsonProperty("Тип отображения выбора префикса для игрока - (0 - выпадающий список, 1 - слайдер (Учтите, что если у вас включен мульти-префикс, будет установлен выпадающий список))")]
                    public SelectedParametres PrefixType;
                    [JsonProperty("Тип отображения выбора цвета ника для игрока - (0 - выпадающий список, 1 - слайдер)")]
                    public SelectedParametres NickColorType;
                    [JsonProperty("Тип отображения выбора цвета сообщения для игрока - (0 - выпадающий список, 1 - слайдер)")]
                    public SelectedParametres ChatColorType;
                    [JsonProperty("IQRankSystem : Тип отображения выбора ранга для игрока - (0 - выпадающий список, 1 - слайдер)")]
                    public SelectedParametres IQRankSystemType;
                }
            }
            #endregion

            #region Controller Mute
            [JsonProperty("Настройка мута в плагине")]
            public ControllerMute ControllerMutes = new ControllerMute();
            internal class ControllerMute
            {
                [JsonProperty("Настройка автоматического мута")]
                public AutoMute AutoMuteSettings = new AutoMute();
                internal class AutoMute
                {
                    [JsonProperty("Включить автоматический мут по запрещенным словам(true - да/false - нет)")]
                    public Boolean UseAutoMute;
                    [JsonProperty("Причина автоматического мута")]
                    public Muted AutoMuted;
                }
                [JsonProperty("Дополнительная настройка для логирования о мутах в дискорд")]
                public LoggedFuncion LoggedMute = new LoggedFuncion();
                internal class LoggedFuncion
                {
                    [JsonProperty("Поддержка логирования последних N сообщений (Должно быть включено логирование в дискорд о мутах)")]
                    public Boolean UseHistoryMessage;
                    [JsonProperty("Сколько последних сообщений игрока отправлять в логировании")]
                    public Int32 CountHistoryMessage;
                }

                [JsonProperty("Причины для блокировки чата")]
                public List<Muted> MuteChatReasons = new List<Muted>();
                [JsonProperty("Причины для блокировки голоса")]
                public List<Muted> MuteVoiceReasons = new List<Muted>();
                internal class Muted
                {
                    [JsonProperty("Причина для блокировки")]
                    public String Reason;
                    [JsonProperty("Время блокировки(в секундах)")]
                    public Int32 SecondMute;
                }
            }
            #endregion

            #region Controller Message
            [JsonProperty("Настройка обработки сообщений")]
            public ControllerMessage ControllerMessages = new ControllerMessage();
            internal class ControllerMessage
            {
                [JsonProperty("Основная настройка сообщений в чат от плагина")]
                public GeneralSettings GeneralSetting = new GeneralSettings();
                [JsonProperty("Настройка переключения функционала в чате")]
                public TurnedFuncional TurnedFunc = new TurnedFuncional();
                [JsonProperty("Настройка форматирования сообщений игроков")]
                public FormattingMessage Formatting = new FormattingMessage();

                internal class GeneralSettings
                {
                    [JsonProperty("Настройка формата оповещения в чате")]
                    public BroadcastSettings BroadcastFormat = new BroadcastSettings();
                    [JsonProperty("Настройка формата упоминания в чате, через @")]
                    public AlertSettings AlertFormat = new AlertSettings();
                    [JsonProperty("Дополнительная настройка")]
                    public OtherSettings OtherSetting = new OtherSettings();

                    internal class BroadcastSettings
                    {
                        [JsonProperty("Наименование оповещения в чат")]
                        public String BroadcastTitle;
                        [JsonProperty("Цвет сообщения оповещения в чат")]
                        public String BroadcastColor;
                        [JsonProperty("Steam64ID для аватарки в чате")]
                        public String Steam64IDAvatar;
                    }
                    internal class AlertSettings
                    {
                        [JsonProperty("Цвет сообщения упоминания игрока в чате")]
                        public String AlertPlayerColor;
                        [JsonProperty("Звук при при получении и отправки упоминания через @")]
                        public String SoundAlertPlayer;
                    }
                    internal class OtherSettings
                    {
                        [JsonProperty("Время,через которое удалится сообщение с UI от администратора")]
                        public Int32 TimeDeleteAlertUI;

                        [JsonProperty("Размер сообщения от игрока в чате")]
                        public Int32 SizeMessage = 14;
                        [JsonProperty("Размер ника игрока в чате")]
                        public Int32 SizeNick = 14;
                        [JsonProperty("Размер префикса игрока в чате (будет использовано, если в самом префиксе не установвлен <size=N></size>)")]
                        public Int32 SizePrefix = 14;
                    }
                }
                internal class TurnedFuncional
                {
                    [JsonProperty("Настройка защиты от спама")]
                    public AntiSpam AntiSpamSetting = new AntiSpam();
                    [JsonProperty("Настройка временной блокировки чата новичкам (которые только зашли на сервер)")]
                    public AntiNoob AntiNoobSetting = new AntiNoob();
                    [JsonProperty("Настройка личных сообщений")]
                    public PM PMSetting = new PM();

                    internal class AntiNoob
                    {
                        [JsonProperty("Защита от новичка в PM/R")]
                        public Settings AntiNoobPM = new Settings();
                        [JsonProperty("Защита от новичка в глобальном и коммандном чате")]
                        public Settings AntiNoobChat = new Settings();
                        internal class Settings
                        {
                            [JsonProperty("Включить защиту?")]
                            public Boolean AntiNoobActivate = false;
                            [JsonProperty("Время блокировки чата для новичка")]
                            public Int32 TimeBlocked = 1200;
                        }
                    }
                    internal class AntiSpam
                    {
                        [JsonProperty("Включить защиту от спама (Анти-спам)")]
                        public Boolean AntiSpamActivate;
                        [JsonProperty("Время через которое игрок может отправлять сообщение (АнтиСпам)")]
                        public Int32 FloodTime;
                        [JsonProperty("Дополнительная настройка Анти-Спама")]
                        public AntiSpamDuples AntiSpamDuplesSetting = new AntiSpamDuples();
                        internal class AntiSpamDuples
                        {
                            [JsonProperty("Включить дополнительную защиту от спама (Анти-дубликаты, повторяющие сообщения)")]
                            public Boolean AntiSpamDuplesActivate = true;
                            [JsonProperty("Сколько дублирующих сообщений нужно сделать игроку чтобы его замутила система")]
                            public Int32 TryDuples = 3;
                            [JsonProperty("Настройка автоматического мута за дубликаты")]
                            public ControllerMute.Muted MuteSetting = new ControllerMute.Muted
                            {
                                Reason = "Блокировка за дублирующие сообщения (СПАМ)",
                                SecondMute = 300,
                            };
                        }
                    }
                    internal class PM
                    {
                        [JsonProperty("Включить личные сообщения")]
                        public Boolean PMActivate;
                        [JsonProperty("Звук при при получении личного сообщения")]
                        public String SoundPM;
                    }
                    [JsonProperty("Включить игнор ЛС игрокам(/ignore nick или через интерфейс)")]
                    public Boolean IgnoreUsePM;
                    [JsonProperty("Скрыть из чата выдачу предметов Админу")]
                    public Boolean HideAdminGave;
                    [JsonProperty("Переносить мут в командный чат(В случае мута, игрок не сможет писать даже в командный чат)")]
                    public Boolean MuteTeamChat;
                }
                internal class FormattingMessage
                {
                    [JsonProperty("Включить форматирование сообщений [Будет контроллировать капс, формат сообщения] (true - да/false - нет)")]
                    public Boolean FormatMessage;
                    [JsonProperty("Использовать список запрещенных слов (true - да/false - нет)")]
                    public Boolean UseBadWords;
                    [JsonProperty("Слово которое будет заменять запрещенное слово")]
                    public String ReplaceBadWord;
                    [JsonProperty("Список запрещенных слов")]
                    public List<String> BadWords = new List<String>();

                    [JsonProperty("Настройка контроллера ников")]
                    public NickController ControllerNickname = new NickController();
                    internal class NickController
                    {
                        [JsonProperty("Включить форматирование ников игроков (должно быть включено форматирование сообщений)")]
                        public Boolean UseNickController = true;
                        [JsonProperty("Слово которое будет заменять запрещенное слово (Вы можете оставить пустым и будет просто удалять)")]
                        public String ReplaceBadNick = "****";
                        [JsonProperty("Список запрещенных ников")]
                        public List<String> BadNicks = new List<String>();
                    }
                }
            }

            #endregion

            #region Controller Alert

            [JsonProperty("Настройка оповещений в чате")]
            public ControllerAlert ControllerAlertSetting;

            internal class ControllerAlert
            {
                [JsonProperty("Настройка оповещений в чате")]
                public Alert AlertSetting;
                [JsonProperty("Настройка оповещений о статусе сессии игрока")]
                public PlayerSession PlayerSessionSetting;
                [JsonProperty("Настройка оповещений о статусе сессии администратора")]
                public AdminSession AdminSessionSetting;
                [JsonProperty("Настройка персональных оповоещений игроку при коннекте")]
                public PersonalAlert PersonalAlertSetting;
                internal class Alert
                {
                    [JsonProperty("Включить автоматические сообщения в чат (true - да/false - нет)")]
                    public Boolean AlertMessage;
                    [JsonProperty("Тип автоматических сообщений : true - поочередные/false - случайные")]
                    public Boolean AlertMessageType;

                    [JsonProperty("Список автоматических сообщений в чат")]
                    public List<String> MessageList;
                    [JsonProperty("Интервал отправки сообщений в чат (Броадкастер) (в секундах)")]
                    public Int32 MessageListTimer;
                }
                internal class PlayerSession
                {
                    [JsonProperty("При уведомлении о входе/выходе игрока отображать его аватар напротив ника (true - да/false - нет)")]
                    public Boolean ConnectedAvatarUse;

                    [JsonProperty("Уведомлять в чате о входе игрока (true - да/false - нет)")]
                    public Boolean ConnectedAlert;
                    [JsonProperty("Включить случайные уведомления о входе игрока из списка (true - да/false - нет)")]
                    public Boolean ConnectionAlertRandom;
                    [JsonProperty("Отображать страну зашедшего игрока (true - да/false - нет")]
                    public Boolean ConnectedWorld;

                    [JsonProperty("Уведомлять о выходе игрока в чат(выбираются из списка) (true - да/false - нет)")]
                    public Boolean DisconnectedAlert;
                    [JsonProperty("Включить случайные уведомления о выходе игрока (true - да/false - нет)")]
                    public Boolean DisconnectedAlertRandom;
                    [JsonProperty("Отображать причину выхода игрока (true - да/false - нет)")]
                    public Boolean DisconnectedReason;

                    [JsonProperty("Случайные уведомления о входе игрока({0} - ник игрока, {1} - страна(если включено отображение страны)")]
                    public List<String> RandomConnectionAlert = new List<String>();
                    [JsonProperty("Случайные уведомления о выходе игрока({0} - ник игрока, {1} - причина выхода(если включена причина)")]
                    public List<String> RandomDisconnectedAlert = new List<String>();
                }
                internal class AdminSession
                {
                    [JsonProperty("Уведомлять о входе админа на сервер в чат (true - да/false - нет)")]
                    public Boolean ConnectedAlertAdmin;
                    [JsonProperty("Уведомлять о выходе админа на сервер в чат (true - да/false - нет)")]
                    public Boolean DisconnectedAlertAdmin;
                }
                internal class PersonalAlert
                {
                    [JsonProperty("Включить случайное сообщение зашедшему игроку (true - да/false - нет)")]
                    public Boolean UseWelcomeMessage;
                    [JsonProperty("Список сообщений игроку при входе")]
                    public List<String> WelcomeMessage = new List<String>();
                }
            }

            #endregion

            #region Rust Plus
            [JsonProperty("Настройка Rust+")]
            public RustPlus RustPlusSettings;
            internal class RustPlus
            {
                [JsonProperty("Использовать Rust+")]
                public Boolean UseRustPlus;
                [JsonProperty("Название для уведомления Rust+")]
                public String DisplayNameAlert;
            }
            #endregion

            #region Reference Setting
            [JsonProperty("Настройка плагинов поддержки")]
            public ReferenceSettings ReferenceSetting = new ReferenceSettings();
            internal class ReferenceSettings
            {
                [JsonProperty("Настройка IQFakeActive")]
                public IQFakeActive IQFakeActiveSettings = new IQFakeActive();
                [JsonProperty("Настройка IQRankSystem")]
                public IQRankSystem IQRankSystems = new IQRankSystem();
                internal class IQRankSystem
                {
                    [JsonProperty("Формат отображения ранга в чате ( {0} - это ранг юзера, не удаляйте это значение)")]
                    public String FormatRank = "[{0}]";
                    [JsonProperty("Формат отображения времени с IQRankSystem в чате ( {0} - это время юзера, не удаляйте это значение)")]
                    public String FormatRankTime = "[{0}]";
                    [JsonProperty("Использовать поддержку рангов")]
                    public Boolean UseRankSystem;
                    [JsonProperty("Отображать игрокам их отыгранное время рядом с рангом")]
                    public Boolean UseTimeStandart;
                }
                internal class IQFakeActive
                {
                    [JsonProperty("Использовать поддержку IQFakeActive")]
                    public Boolean UseIQFakeActive;
                }
            }
            #endregion

            #region Anwser Setting

            [JsonProperty("Настройка автоответчика")]
            public AnswerMessage AnswerMessages = new AnswerMessage();

            internal class AnswerMessage
            {
                [JsonProperty("Включить автоответчик?(true - да/false - нет)")]
                public bool UseAnswer;
                [JsonProperty("Настройка сообщений [Ключевое слово] = Ответ")]
                public Dictionary<String, String> AnswerMessageList = new Dictionary<String, String>();
            }

            #endregion

            #region Other Setting
            [JsonProperty("Дополнительная настройка")]
            public OtherSettings OtherSetting;

            internal class OtherSettings
            {
                [JsonProperty("Настройка логирования сообщений")]
                public LoggedChat LogsChat = new LoggedChat();
                [JsonProperty("Настройка логирования личных сообщений игроков")]
                public General LogsPMChat = new General();
                [JsonProperty("Настройка логирования блокировок/разблокировок чата/голоса")]
                public General LogsMuted = new General();
                [JsonProperty("Настройка логирования чат-команд от игроков")]
                public General LogsChatCommands = new General();
                internal class LoggedChat
                {
                    [JsonProperty("Настройка логирования общего чата")]
                    public General GlobalChatSettings = new General();
                    [JsonProperty("Настройка логирования тим чата")]
                    public General TeamChatSettings = new General();
                }
                internal class General
                {
                    [JsonProperty("Включить логирование (true - да/false - нет)")]
                    public Boolean UseLogged = false;
                    [JsonProperty("Webhooks канала для логирования")]
                    public String Webhooks = "";
                }
            }
            #endregion

            public Dictionary<String, String> KeyImages = new Dictionary<string, string>();
            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    #region Controller Parameter
                    ControllerParameter = new ControllerParameters
                    {
                        VisualParametres = new ControllerParameters.VisualSettingParametres
                        {
                            PrefixType = SelectedParametres.DropList,
                            ChatColorType = SelectedParametres.DropList,
                            NickColorType = SelectedParametres.Slider,
                            IQRankSystemType = SelectedParametres.Slider,
                        },
                        Prefixes = new ControllerParameters.PrefixSetting
                        {
                            TurnMultiPrefixes = false,
                            MaximumMultiPrefixCount = 5,
                            Prefixes = new List<ControllerParameters.AdvancedFuncion>
                              {
                                  new ControllerParameters.AdvancedFuncion
                                  {
                                      Argument = "<color=#CC99FF>[ИГРОК]</color>",
                                      Permissions = "iqchat.default",
                                  },
                                  new ControllerParameters.AdvancedFuncion
                                  {
                                      Argument = "<color=#ffff99>[VIP]</color>",
                                      Permissions = "iqchat.admin",
                                  },
                                  new ControllerParameters.AdvancedFuncion
                                  {
                                      Argument = "<color=#ff9999>[АДМИН]</color>",
                                      Permissions = "iqchat.admin",
                                  },
                            },
                        },
                        MessageColorList = new List<ControllerParameters.AdvancedFuncion>
                        {
                               new ControllerParameters.AdvancedFuncion
                               {
                                    Argument = "#CC99FF",
                                    Permissions = "iqchat.default",
                               },
                               new ControllerParameters.AdvancedFuncion
                               {
                                    Argument = "#ffff99",
                                    Permissions = "iqchat.admin",
                               },
                               new ControllerParameters.AdvancedFuncion
                               {
                                    Argument = "#ff9999",
                                    Permissions = "iqchat.admin",
                               },
                        },
                        NickColorList = new List<ControllerParameters.AdvancedFuncion>
                        {
                               new ControllerParameters.AdvancedFuncion
                               {
                                    Argument = "#CC99FF",
                                    Permissions = "iqchat.default",
                               },
                               new ControllerParameters.AdvancedFuncion
                               {
                                    Argument = "#ffff99",
                                    Permissions = "iqchat.admin",
                               },
                               new ControllerParameters.AdvancedFuncion
                               {
                                    Argument = "#ff9999",
                                    Permissions = "iqchat.admin",
                               },
                        },
                    },
                    #endregion

                    #region Controller Connect

                    ControllerConnect = new ControllerConnection
                    {
                        SetupDefaults = new ControllerConnection.SetupDefault
                        {
                            PrefixDefault = "<color=#CC99FF>[ИГРОК]</color>",
                            MessageDefault = "#33CCCC",
                            NickDefault = "#0099FF",
                        },
                        Turneds = new ControllerConnection.Turned
                        {
                            TurnAutoDropColorChat = true,
                            TurnAutoDropColorNick = true,
                            TurnAutoDropPrefix = true,
                            TurnAutoSetupColorChat = true,
                            TurnAutoSetupColorNick = true,
                            TurnAutoSetupPrefix = true,
                        }
                    },

                    #endregion

                    #region Controller Mute

                    ControllerMutes = new ControllerMute
                    {
                        LoggedMute = new ControllerMute.LoggedFuncion
                        {
                            UseHistoryMessage = false,
                            CountHistoryMessage = 10,
                        },
                        AutoMuteSettings = new ControllerMute.AutoMute
                        {
                            UseAutoMute = true,
                            AutoMuted = new ControllerMute.Muted
                            {
                                Reason = "Автоматическая блокировка чата",
                                SecondMute = 300,
                            }
                        },
                        MuteChatReasons = new List<ControllerMute.Muted>
                        {
                            new ControllerMute.Muted
                            {
                                Reason = "Агрессивное поведение",
                                SecondMute = 100,
                            },
                            new ControllerMute.Muted
                            {
                                Reason = "Оскорбления",
                                SecondMute = 300,
                            },
                            new ControllerMute.Muted
                            {
                                Reason = "Оскорбление (повторное нарушение)",
                                SecondMute = 1000,
                            },
                            new ControllerMute.Muted
                            {
                                Reason = "Реклама",
                                SecondMute = 5000,
                            },
                            new ControllerMute.Muted
                            {
                                Reason = "Унижение",
                                SecondMute = 300,
                            },
                            new ControllerMute.Muted
                            {
                                Reason = "Спам",
                                SecondMute = 60,
                            },
                        },
                        MuteVoiceReasons = new List<ControllerMute.Muted>
                        {
                            new ControllerMute.Muted
                            {
                                Reason = "Агрессивное поведение",
                                SecondMute = 100,
                            },
                            new ControllerMute.Muted
                            {
                                Reason = "Оскорбления",
                                SecondMute = 300,
                            },
                            new ControllerMute.Muted
                            {
                                Reason = "Срыв мероприятия криками",
                                SecondMute = 300,
                            },
                        }
                    },

                    #endregion

                    #region Controller Message

                    ControllerMessages = new ControllerMessage
                    {
                        Formatting = new ControllerMessage.FormattingMessage
                        {
                            UseBadWords = true,
                            BadWords = new List<String> { "бля", "сука", "говно", "тварь" },
                            FormatMessage = true,
                            ReplaceBadWord = "***",
                            ControllerNickname = new ControllerMessage.FormattingMessage.NickController
                            {
                                BadNicks = new List<String> { "Администратор", "Модератор", "Админ", "Модер", "Овнер", "Mercury Loh", "IQchat" },
                                ReplaceBadNick = "",
                                UseNickController = true,
                            },
                        },
                        TurnedFunc = new ControllerMessage.TurnedFuncional
                        {
                            HideAdminGave = true,
                            IgnoreUsePM = true,
                            MuteTeamChat = true,
                            AntiNoobSetting = new ControllerMessage.TurnedFuncional.AntiNoob
                            {
                                AntiNoobChat = new ControllerMessage.TurnedFuncional.AntiNoob.Settings
                                {
                                    AntiNoobActivate = false,
                                    TimeBlocked = 1200,
                                },
                                AntiNoobPM = new ControllerMessage.TurnedFuncional.AntiNoob.Settings
                                {
                                    AntiNoobActivate = false,
                                    TimeBlocked = 1200,
                                },
                            },
                            AntiSpamSetting = new ControllerMessage.TurnedFuncional.AntiSpam
                            {
                                AntiSpamActivate = true,
                                FloodTime = 10,
                                AntiSpamDuplesSetting = new ControllerMessage.TurnedFuncional.AntiSpam.AntiSpamDuples
                                {
                                    AntiSpamDuplesActivate = true,
                                    MuteSetting = new ControllerMute.Muted
                                    {
                                        Reason = "Повторяющиеся сообщения (СПАМ)",
                                        SecondMute = 300,
                                    },
                                    TryDuples = 3,
                                }
                            },
                            PMSetting = new ControllerMessage.TurnedFuncional.PM
                            {
                                PMActivate = true,
                                SoundPM = "assets/bundled/prefabs/fx/notice/stack.world.fx.prefab",
                            },
                        },
                        GeneralSetting = new ControllerMessage.GeneralSettings
                        {
                            BroadcastFormat = new ControllerMessage.GeneralSettings.BroadcastSettings
                            {
                                BroadcastColor = "#efedee",
                                BroadcastTitle = "<color=#68cacd><b>[ОПОВЕЩЕНИЕ]</b></color>",
                                Steam64IDAvatar = "0",
                            },
                            AlertFormat = new ControllerMessage.GeneralSettings.AlertSettings
                            {
                                AlertPlayerColor = "#efedee",
                                SoundAlertPlayer = "assets/bundled/prefabs/fx/notice/item.select.fx.prefab",
                            },
                            OtherSetting = new ControllerMessage.GeneralSettings.OtherSettings
                            {
                                TimeDeleteAlertUI = 5,
                                SizePrefix = 14,
                                SizeMessage = 14,
                                SizeNick = 14,
                            }
                        },
                    },

                    #endregion

                    #region Controller Alert

                    ControllerAlertSetting = new ControllerAlert
                    {
                        AlertSetting = new ControllerAlert.Alert
                        {
                            AlertMessage = true,
                            AlertMessageType = false,
                            MessageList = new List<String>
                            {
                                "Автоматическое сообщение #1 (Редактировать в конфигурации)",
                                "Автоматическое сообщение #2 (Редактировать в конфигурации)",
                                "Автоматическое сообщение #3 (Редактировать в конфигурации)",
                                "Автоматическое сообщение #4 (Редактировать в конфигурации)",
                                "Автоматическое сообщение #5 (Редактировать в конфигурации)",
                                "Автоматическое сообщение #6 (Редактировать в конфигурации)",
                            },
                            MessageListTimer = 60,
                        },
                        AdminSessionSetting = new ControllerAlert.AdminSession
                        {
                            ConnectedAlertAdmin = false,
                            DisconnectedAlertAdmin = false,
                        },
                        PlayerSessionSetting = new ControllerAlert.PlayerSession
                        {
                            ConnectedAlert = true,
                            ConnectedAvatarUse = true,
                            ConnectedWorld = true,
                            ConnectionAlertRandom = false,

                            DisconnectedAlert = true,
                            DisconnectedAlertRandom = false,
                            DisconnectedReason = true,

                            RandomConnectionAlert = new List<String>
                            {
                                "{0} влетел как дурачок из {1}",
                                "{0} залетел на сервер из {1}, соболезнуем",
                                "{0} прыгнул на сервачок"
                            },
                            RandomDisconnectedAlert = new List<String>
                            {
                                "{0} ушел в мир иной",
                                "{0} вылетел с сервера с причиной {1}",
                                "{0} пошел на другой сервачок"
                            },
                        },
                        PersonalAlertSetting = new ControllerAlert.PersonalAlert
                        {
                            UseWelcomeMessage = true,
                            WelcomeMessage = new List<String>
                            {
                                "Добро пожаловать на сервер SUPERSERVER\nРады,что выбрал именно нас!",
                                "С возвращением на сервер!\nЖелаем тебе удачи",
                                "Добро пожаловать на сервер\nУ нас самые лучшие плагины",
                            }
                        }
                    },

                    #endregion

                    #region Reference Setting

                    ReferenceSetting = new ReferenceSettings
                    {
                        IQFakeActiveSettings = new ReferenceSettings.IQFakeActive
                        {
                            UseIQFakeActive = true,
                        },
                        IQRankSystems = new ReferenceSettings.IQRankSystem
                        {
                            FormatRank = "[{0}]",
                            FormatRankTime = "[{0}]",
                            UseRankSystem = false,
                            UseTimeStandart = true
                        },
                    },

                    #endregion

                    #region Rust Plus

                    RustPlusSettings = new RustPlus
                    {
                        UseRustPlus = true,
                        DisplayNameAlert = "СУПЕР СЕРВЕР",
                    },

                    #endregion

                    #region Anwser Setting

                    AnswerMessages = new AnswerMessage
                    {
                        UseAnswer = true,
                        AnswerMessageList = new Dictionary<string, string>
                        {
                            ["вайп"] = "Вайп будет 27.06",
                            ["wipe"] = "Вайп будет 27.06",
                            ["читер"] = "Нашли читера?Напиши /report и отправь жалобу"
                        }
                    },

                    #endregion

                    #region Other Setting

                    OtherSetting = new OtherSettings
                    {
                        LogsChat = new OtherSettings.LoggedChat
                        {
                            GlobalChatSettings = new OtherSettings.General
                            {
                                UseLogged = false,
                                Webhooks = "",
                            },
                            TeamChatSettings = new OtherSettings.General
                            {
                                UseLogged = false,
                                Webhooks = "",
                            }
                        },
                        LogsChatCommands = new OtherSettings.General
                        {
                            UseLogged = false,
                            Webhooks = "",
                        },
                        LogsPMChat = new OtherSettings.General
                        {
                            UseLogged = false,
                            Webhooks = "",
                        },
                        LogsMuted = new OtherSettings.General
                        {
                            UseLogged = false,
                            Webhooks = "",
                        },
                    },

                    #endregion

                    KeyImages = new Dictionary<String, String>()
                    {
                        { "UI_IQCHAT_CONTEXT_NO_RANK", "https://imgur.com/7AV4eG5.png"},
                        { "UI_IQCHAT_CONTEXT_RANK", "https://imgur.com/dQ0ptBE.png"},
                        { "IQCHAT_INFORMATION_ICON", "https://imgur.com/phcCykw.png"},
                        { "IQCHAT_SETTING_ICON", "https://imgur.com/foT3lUr.png"},
                        { "IQCHAT_IGNORE_INFO_ICON", "https://imgur.com/MEhsbTM.png"},
                        { "IQCHAT_MODERATION_ICON", "https://imgur.com/2vJP8qM.png"},
                        { "IQCHAT_ELEMENT_PANEL_ICON", "https://imgur.com/afRp7O0.png"},
                        { "IQCHAT_ELEMENT_PREFIX_MULTI_TAKE_ICON", "https://imgur.com/6ohxadb.png"},
                        { "IQCHAT_ELEMENT_SLIDER_ICON", "https://imgur.com/cx8ZoA7.png"},
                        { "IQCHAT_ELEMENT_SLIDER_LEFT_ICON", "https://imgur.com/ZZP2kuy.png"},
                        { "IQCHAT_ELEMENT_SLIDER_RIGHT_ICON", "https://imgur.com/g0VTWKD.png"},
                        { "IQCHAT_ELEMENT_DROP_LIST_OPEN_ICON", "https://imgur.com/p9pIfXR.png"},
                        { "IQCHAT_ELEMENT_DROP_LIST_OPEN_ARGUMENT_ICON", "https://imgur.com/HM1AuCF.png"},
                        { "IQCHAT_ELEMENT_DROP_LIST_OPEN_TAKED", "https://imgur.com/gBvkzy0.png"},
                        { "IQCHAT_ELEMENT_SETTING_CHECK_BOX", "https://imgur.com/tjkGj46.png"},
                        { "IQCHAT_ALERT_PANEL", "https://i.imgur.com/sKKsKrs.png"},
                        { "IQCHAT_MUTE_AND_IGNORE_PANEL", "https://imgur.com/bOS905f.png"},
                        { "IQCHAT_MUTE_AND_IGNORE_ICON", "https://imgur.com/15myNzk.png"},
                        { "IQCHAT_MUTE_AND_IGNORE_SEARCH", "https://imgur.com/S4c4Hc9.png"},
                        { "IQCHAT_MUTE_AND_IGNORE_PAGE_PANEL", "https://imgur.com/Yo0You9.png"},
                        { "IQCHAT_MUTE_AND_IGNORE_PLAYER", "https://imgur.com/JfDmg3P.png"},
                        { "IQCHAT_MUTE_AND_IGNORE_PLAYER_STATUS", "https://imgur.com/dLiAv1i.png"},
                        { "IQCHAT_IGNORE_ALERT_PANEL", "https://imgur.com/bJgQV1b.png"},
                        { "IQCHAT_IGNORE_ALERT_ICON", "https://imgur.com/qKTHf8N.png"},
                        { "IQCHAT_IGNORE_ALERT_BUTTON_YES", "https://imgur.com/NjQ9OVL.png"},
                        { "IQCHAT_IGNORE_ALERT_BUTTON_NO", "https://imgur.com/Gzn47GY.png"},
                        { "IQCHAT_MUTE_ALERT_PANEL", "https://imgur.com/f8dBS6M.png"},
                        { "IQCHAT_MUTE_ALERT_ICON", "https://imgur.com/seBCx8G.png"},
                        { "IQCHAT_MUTE_ALERT_PANEL_REASON", "https://imgur.com/OhyZVNY.png"},
                    }
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning("Ошибка #132" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }

        private void RegisteredPermissions()
        {
            Configuration.ControllerParameters Controller = config.ControllerParameter;
            IEnumerable<Configuration.ControllerParameters.AdvancedFuncion> Parametres = Controller.Prefixes.Prefixes.Concat(Controller.NickColorList).Concat(Controller.MessageColorList);

            foreach (Configuration.ControllerParameters.AdvancedFuncion Permission in Parametres.Where(perm => !permission.PermissionExists(perm.Permissions, this)))
                permission.RegisterPermission(Permission.Permissions, this);

            permission.RegisterPermission(PermissionHideOnline, this);
            permission.RegisterPermission(PermissionRename, this);
            permission.RegisterPermission(PermissionMute, this);
            permission.RegisterPermission(PermissionAlert, this);
            permission.RegisterPermission(PermissionAntiSpam, this);
            permission.RegisterPermission(PermissionHideConnection, this);
            permission.RegisterPermission(PermissionHideDisconnection, this);
            permission.RegisterPermission(PermissionMutedAdmin, this);
            PrintWarning("Permissions - completed");
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Data
        public GeneralInformation GeneralInfo = new GeneralInformation();
        public Dictionary<UInt64, User> UserInformation = new Dictionary<UInt64, User>();
        public Dictionary<UInt64, AntiNoob> UserInformationConnection = new Dictionary<UInt64, AntiNoob>();
        internal class AntiNoob
        {
            public DateTime DateConnection = DateTime.UtcNow;

            public Boolean IsNoob(Int32 TimeBlocked)
            {
                System.TimeSpan Time = DateTime.UtcNow.Subtract(DateConnection);
                return Time.TotalSeconds < TimeBlocked;
            }

            public Double LeftTime(Int32 TimeBlocked)
            {
                System.TimeSpan Time = DateTime.UtcNow.Subtract(DateConnection);

                return (TimeBlocked - Time.TotalSeconds);
            }
        }
        public class User
        {
            public Information Info = new Information();
            public Setting Settings = new Setting();
            public Mute MuteInfo = new Mute();
            internal class Information
            {
                public String Prefix;
                public String ColorNick;
                public String ColorMessage;
                public String Rank;

                public List<String> PrefixList = new List<String>();
            }

            internal class Setting
            {
                public Boolean TurnPM = true;
                public Boolean TurnAlert = true;
                public Boolean TurnBroadcast = true;
                public Boolean TurnSound = true;

                public List<UInt64> IgnoreUsers = new List<UInt64>();

                public Boolean IsIgnored(UInt64 TargetID) => IgnoreUsers.Contains(TargetID);
                public void IgnoredAddOrRemove(UInt64 TargetID)
                {
                    if (IsIgnored(TargetID))
                        IgnoreUsers.Remove(TargetID);
                    else IgnoreUsers.Add(TargetID);
                }
            }

            internal class Mute
            {
                public Double TimeMuteChat;
                public Double TimeMuteVoice;

                public Double GetTime(MuteType Type)
                {
                    Double TimeMuted = 0;
                    switch (Type)
                    {
                        case MuteType.Chat:
                            TimeMuted = TimeMuteChat - CurrentTime;
                            break;
                        case MuteType.Voice:
                            TimeMuted = TimeMuteVoice - CurrentTime;
                            break;
                        default:
                            break;
                    }
                    return TimeMuted;
                }
                public void SetMute(MuteType Type, Int32 Time)
                {
                    switch (Type)
                    {
                        case MuteType.Chat:
                            TimeMuteChat = Time + CurrentTime;
                            break;
                        case MuteType.Voice:
                            TimeMuteVoice = Time + CurrentTime;
                            break;
                        default:
                            break;
                    }
                }
                public void UnMute(MuteType Type)
                {
                    switch (Type)
                    {
                        case MuteType.Chat:
                            TimeMuteChat = 0;
                            break;
                        case MuteType.Voice:
                            TimeMuteVoice = 0;
                            break;
                        default:
                            break;
                    }
                }
                public Boolean IsMute(MuteType Type) => GetTime(Type) > 0;
            }
        }

        public class GeneralInformation
        {
            public Boolean TurnMuteAllChat;
            public Boolean TurnMuteAllVoice;

            public Dictionary<UInt64, RenameInfo> RenameList = new Dictionary<UInt64, RenameInfo>();
            internal class RenameInfo
            {
                public String RenameNick;
                public UInt64 RenameID;
            }

            public RenameInfo GetInfoRename(UInt64 UserID)
            {
                if (!RenameList.ContainsKey(UserID)) return null;
                return RenameList[UserID];
            }
        }
        private void MigrateDataToNoob()
        {
            if (config.ControllerMessages.TurnedFunc.AntiNoobSetting.AntiNoobPM.AntiNoobActivate || config.ControllerMessages.TurnedFunc.AntiNoobSetting.AntiNoobChat.AntiNoobActivate)
            {
                if (UserInformationConnection.Count == 0 || UserInformationConnection == null)
                {
                    PrintWarning("Миграция старых игроков в Анти-Нуб..");
                    foreach (KeyValuePair<UInt64, User> InfoUser in UserInformation.Where(x => !UserInformationConnection.ContainsKey(x.Key)))
                        UserInformationConnection.Add(InfoUser.Key, new AntiNoob { DateConnection = new DateTime(2022, 1, 1) });
                    PrintWarning("Миграция старых игроков завершена");
                }
            }
        }
        private void UserConnecteionData(BasePlayer player)
        {
            if (config.ControllerMessages.TurnedFunc.AntiNoobSetting.AntiNoobPM.AntiNoobActivate || config.ControllerMessages.TurnedFunc.AntiNoobSetting.AntiNoobChat.AntiNoobActivate) // Включена ли защита от "нубов"
            {
                if (!UserInformationConnection.ContainsKey(player.userID)) // Есть ли игрок в дата-файле
                    UserInformationConnection.Add(player.userID, new AntiNoob()); // Добавляем игрока в дата-файл, генерируя ему класс в котором данные автоматически подтянутся
            }

            Configuration.ControllerConnection ControllerConntect = config.ControllerConnect;
            Configuration.ControllerParameters ControllerParameter = config.ControllerParameter;
            if (ControllerConntect == null || ControllerParameter == null || UserInformation.ContainsKey(player.userID)) return;

            User Info = new User();
            if (ControllerConntect.Turneds.TurnAutoSetupPrefix) // Включена ли выдача автоматического префикса
            {
                if (ControllerParameter.Prefixes.TurnMultiPrefixes) // Проверка на мультипрефикс
                    Info.Info.PrefixList.Add(ControllerConntect.SetupDefaults.PrefixDefault ?? ""); // Установка мультипрефикса
                else Info.Info.Prefix = ControllerConntect.SetupDefaults.PrefixDefault ?? ""; // Установка одного префикса
            }

            if (ControllerConntect.Turneds.TurnAutoSetupColorNick) // Включена ли выдача автоматического цвета ника
                Info.Info.ColorNick = ControllerConntect.SetupDefaults.NickDefault; // Установка цвета ника

            if (ControllerConntect.Turneds.TurnAutoSetupColorChat) // Включена ли выдача автоматического цвета сообщений
                Info.Info.ColorMessage = ControllerConntect.SetupDefaults.MessageDefault; // Установка цвета сообщений

            Info.Info.Rank = String.Empty;

            UserInformation.Add(player.userID, Info); // Запись в дата-файл
        }

        void ReadData()
        {
            if (!Oxide.Core.Interface.Oxide.DataFileSystem.ExistsDatafile("IQSystem/IQChat/Users") && Oxide.Core.Interface.Oxide.DataFileSystem.ExistsDatafile("IQChat/Users"))
            {
                GeneralInfo = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<GeneralInformation>("IQChat/Information");
                UserInformation = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<UInt64, User>>("IQChat/Users");

                Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQChat/Information", GeneralInfo);
                Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQChat/Users", UserInformation);

                PrintWarning("Ваши данные игроков были перенесены в новую директорию - IQSystem/IQChat , вы можете удалить старые дата-файлы!");
            }

            GeneralInfo = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<GeneralInformation>("IQSystem/IQChat/Information");
            UserInformation = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<UInt64, User>>("IQSystem/IQChat/Users");
            UserInformationConnection = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<UInt64, AntiNoob>>("IQSystem/IQChat/AntiNoob");
        }
        void WriteData()
        {
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQChat/Information", GeneralInfo);
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQChat/Users", UserInformation);
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQChat/AntiNoob", UserInformationConnection);
        }

        #endregion

        #region Hooks     
        private bool OnPlayerChat(BasePlayer player, string message, Chat.ChatChannel channel)
        {
            if (Interface.Oxide.CallHook("CanChatMessage", channel, player, message) != null) return false;

            SeparatorChat(channel, player, message);
            return false;
        }
        private object OnServerMessage(String message, String name)
        {
            if (config.ControllerMessages.TurnedFunc.HideAdminGave)
                if (message.Contains("gave") && name == "SERVER")
                    return true;
            return null;
        }
        void OnPlayerCommand(BasePlayer player, string command, string[] args)
        {
            DiscordLoggCommand(player, command, args);
        }

        #region Auto Setup/Remove Permission

        #region User
        void OnUserPermissionGranted(string id, string permName) => SetupParametres(id, permName);
        void OnUserPermissionRevoked(string id, string permName) => RemoveParametres(id, permName);

        void OnUserGroupAdded(string id, string groupName)
        {
            String[] PermissionsGroup = permission.GetGroupPermissions(groupName);
            if (PermissionsGroup == null) return;
            foreach (String permName in PermissionsGroup)
                SetupParametres(id, permName);
        }

        void OnUserGroupRemoved(string id, string groupName)
        {
            String[] PermissionsGroup = permission.GetGroupPermissions(groupName);
            if (PermissionsGroup == null) return;

            foreach (String permName in PermissionsGroup)
                RemoveParametres(id, permName);
        }

        #endregion

        #region Group 
        void OnGroupPermissionGranted(string name, string perm)
        {
            String[] PlayerGroups = permission.GetUsersInGroup(name);
            if (PlayerGroups == null) return;

            foreach (String playerInfo in PlayerGroups)
            {
                BasePlayer player = BasePlayer.FindByID(UInt64.Parse(playerInfo.Substring(0, 17)));
                if (player == null) return;

                SetupParametres(player.UserIDString, perm);
            }
        }

        void OnGroupPermissionRevoked(string name, string perm)
        {
            String[] PlayerGroups = permission.GetUsersInGroup(name);
            if (PlayerGroups == null) return;

            foreach (String playerInfo in PlayerGroups)
            {
                BasePlayer player = BasePlayer.FindByID(UInt64.Parse(playerInfo.Substring(0, 17)));
                if (player == null) return;

                RemoveParametres(player.UserIDString, perm);
            }
        }
        #endregion

        #endregion

        object OnPlayerVoice(BasePlayer player, Byte[] data)
        {
            if (UserInformation[player.userID].MuteInfo.IsMute(MuteType.Voice))
                return false;
            return null;
        }
        void Init()
        {
            ReadData();
        }
        private void OnServerInitialized()
        {
            _ = this;
            ImageUi.DownloadImages();

            MigrateDataToNoob();

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                UserConnecteionData(player);

            RegisteredPermissions();
            BroadcastAuto();

            CheckValidateUsers();
        }
        private void CheckValidateUsers()
        {
            Configuration.ControllerParameters Controller = config.ControllerParameter;
            Configuration.ControllerConnection ControllerConnection = config.ControllerConnect;

            List<Configuration.ControllerParameters.AdvancedFuncion> Prefixes = Controller.Prefixes.Prefixes;
            List<Configuration.ControllerParameters.AdvancedFuncion> NickColor = Controller.NickColorList;
            List<Configuration.ControllerParameters.AdvancedFuncion> ChatColor = Controller.MessageColorList;

            foreach (KeyValuePair<UInt64, User> Info in UserInformation)
            {
                if (Controller.Prefixes.TurnMultiPrefixes)
                {
                    foreach (String Prefix in Info.Value.Info.PrefixList.Where(prefixList => !Prefixes.Exists(i => i.Argument == prefixList)))
                        NextTick(() => Info.Value.Info.PrefixList.Remove(Prefix));
                }
                else
                {
                    if (!Prefixes.Exists(i => i.Argument == Info.Value.Info.Prefix))
                        Info.Value.Info.Prefix = ControllerConnection.SetupDefaults.PrefixDefault;
                }
                if (!NickColor.Exists(i => i.Argument == Info.Value.Info.ColorNick))
                    Info.Value.Info.ColorNick = ControllerConnection.SetupDefaults.NickDefault;

                if (!ChatColor.Exists(i => i.Argument == Info.Value.Info.ColorMessage))
                    Info.Value.Info.ColorMessage = ControllerConnection.SetupDefaults.MessageDefault;
            }
        }
        void OnPlayerConnected(BasePlayer player)
        {
            UserConnecteionData(player);
            AlertController(player);
        }
        void Unload()
        {
            InterfaceBuilder.DestroyAll();

            WriteData();
            _ = null;
        }

        void OnPlayerDisconnected(BasePlayer player, string reason) => AlertDisconnected(player, reason);
        #endregion

        #region DiscordFunc

        #region Logged Chat

        private void DiscordLoggCommand(BasePlayer player, String Command, String[] Args)
        {
            Configuration.OtherSettings.General Commands = config.OtherSetting.LogsChatCommands;
            if (!Commands.UseLogged) return;

            List<Fields> fields = new List<Fields>
                        {
                            new Fields("Ник", player.displayName, true),
                            new Fields("Steam64ID", player.UserIDString, true),
                            new Fields("Команда", $"/{Command} ", true),
                        };

            String Arguments = String.Join(" ", Args);
            if (Args != null && Arguments != null && Arguments.Length != 0 && !String.IsNullOrWhiteSpace(Arguments))
                fields.Insert(fields.Count, new Fields("Аргументы", Arguments, false));

            FancyMessage newMessage = new FancyMessage(null, false, new FancyMessage.Embeds[1] { new FancyMessage.Embeds(null, 10710525, fields, new Authors("IQChat Command-History", null, "https://i.imgur.com/xiwsg5m.png", null), null) });

            Request($"{Commands.Webhooks}", newMessage.toJSON());
        }
        private void DiscordLoggChat(BasePlayer player, Chat.ChatChannel Channel, String MessageLogged)
        {
            List<Fields> fields = new List<Fields>
                        {
                            new Fields("Ник", player.displayName, true),
                            new Fields("Steam64ID", player.UserIDString, true),
                            new Fields("Канал", Channel == Chat.ChatChannel.Global ? "Глобальный чат" : "Командный чат", true),
                            new Fields("Сообщение", MessageLogged, false),
                        };

            FancyMessage newMessage = new FancyMessage(null, false, new FancyMessage.Embeds[1] { new FancyMessage.Embeds(null, 10710525, fields, new Authors("IQChat Chat-History", null, "https://i.imgur.com/xiwsg5m.png", null), null) });

            switch (Channel)
            {
                
                case Chat.ChatChannel.Global:
                    {
                        Configuration.OtherSettings.General GlobalChat = config.OtherSetting.LogsChat.GlobalChatSettings;
                        if (!GlobalChat.UseLogged) return;
                        Request($"{GlobalChat.Webhooks}", newMessage.toJSON());
                        break;
                    }
                case Chat.ChatChannel.Team:
                    {
                        Configuration.OtherSettings.General TeamChat = config.OtherSetting.LogsChat.TeamChatSettings;
                        if (!TeamChat.UseLogged) return;
                        Request($"{TeamChat.Webhooks}", newMessage.toJSON());
                    }
                    break;
                default:
                    break;
            }
        }

        private void DiscordLoggPM(BasePlayer Sender, BasePlayer Reciepter, String MessageLogged)
        {
            Configuration.OtherSettings.General PMChat = config.OtherSetting.LogsPMChat;
            if (!PMChat.UseLogged) return;

            GeneralInformation.RenameInfo SenderRename = GeneralInfo.GetInfoRename(Sender.userID);
            GeneralInformation.RenameInfo ReciepterRename = GeneralInfo.GetInfoRename(Reciepter.userID);

            UInt64 UserIDSender = SenderRename != null ? SenderRename.RenameID == 0 ? Sender.userID : SenderRename.RenameID : Sender.userID;
            UInt64 UserIDReciepter = ReciepterRename != null ? ReciepterRename.RenameID == 0 ? Reciepter.userID : ReciepterRename.RenameID : Reciepter.userID;
            String SenderName = SenderRename != null ? ReciepterRename.RenameNick ?? Sender.displayName : Sender.displayName;
            String ReciepterName = ReciepterRename != null ? ReciepterRename.RenameNick ?? Reciepter.displayName : Reciepter.displayName;

            List<Fields> fields = new List<Fields>
                        {
                            new Fields("Отправитель", $"{SenderName}({UserIDSender})", true),
                            new Fields("Получатель", $"{ReciepterName}({UserIDReciepter})", true),
                            new Fields("Сообщение", MessageLogged, false),
                        };

            FancyMessage newMessage = new FancyMessage(null, false, new FancyMessage.Embeds[1] { new FancyMessage.Embeds(null, 16608621, fields, new Authors("IQChat PM-History", null, "https://i.imgur.com/xiwsg5m.png", null), null) });

            Request($"{PMChat.Webhooks}", newMessage.toJSON());
        }

        private void DiscordLoggMuted(BasePlayer Target, MuteType Type, String Reason = null, String TimeBlocked = null, BasePlayer Moderator = null)
        {
            Configuration.OtherSettings.General MuteChat = config.OtherSetting.LogsMuted;
            if (!MuteChat.UseLogged) return;

            Configuration.ControllerMute.LoggedFuncion ControllerMuted = config.ControllerMutes.LoggedMute;

            String ActionReason = String.Empty;
            //// CHECK
            ///
            GeneralInformation.RenameInfo RenameSender = GeneralInfo.GetInfoRename(Target.userID);

            UInt64 UserIDModeration = 0;
            String NickModeration = GetLang("IQCHAT_FUNCED_ALERT_TITLE_SERVER", Target.UserIDString);
            if (Moderator != null)
            {
                GeneralInformation.RenameInfo RenameModerator = GeneralInfo.GetInfoRename(Moderator.userID);

                UserIDModeration = RenameModerator != null ? RenameModerator.RenameID == 0 ? Moderator.userID : RenameModerator.RenameID : Moderator.userID;
                NickModeration = RenameModerator != null ? $"{RenameModerator.RenameNick ?? Moderator.displayName}" : Moderator.displayName;
            }

            String NickTarget = RenameSender != null ? $"{RenameSender.RenameNick ?? Target.displayName}" : Target.displayName;
            UInt64 UserIDTarget = RenameSender != null ? RenameSender.RenameID == 0 ? Target.userID : RenameSender.RenameID : Target.userID;

            List<Fields> fields;

            switch (Type)
            {
                case MuteType.Chat:
                    {
                        if (Reason != null)
                            ActionReason = "Блокировка чата";
                        else ActionReason = "Разблокировка чата";
                        break;
                    }
                case MuteType.Voice:
                    {
                        if (Reason != null)
                            ActionReason = "Блокировка голоса";
                        else ActionReason = "Разблокировка голоса";
                        break;
                    }
                default:
                    break;
            }
            Int32 Color = 0;
            if (Reason != null)
            {
                fields = new List<Fields>
                        {
                            new Fields("Ник модератора", NickModeration, true),
                            new Fields("Steam64ID модератора", $"{UserIDModeration}", true),
                            new Fields("Действие", ActionReason, false),
                            new Fields("Причина", Reason, false),
                            new Fields("Время", TimeBlocked, false),
                            new Fields("Ник заблокированного", NickTarget, true),
                            new Fields("Steam64ID заблокированного", $"{UserIDTarget}", true),
                        };



                if (ControllerMuted.UseHistoryMessage)
                {
                    String Messages = GetLastMessage(Target, ControllerMuted.CountHistoryMessage);
                    if (Messages != null && !String.IsNullOrWhiteSpace(Messages))
                        fields.Insert(fields.Count, new Fields($"Последние {ControllerMuted.CountHistoryMessage} сообщений", Messages, false));
                }

                Color = 14357781;
            }
            else
            {
                fields = new List<Fields>
                        {
                            new Fields("Ник модератора", NickModeration, true),
                            new Fields("Steam64ID модератора", $"{UserIDModeration}", true),
                            new Fields("Действие", ActionReason, false),
                            new Fields("Ник заблокированного", NickTarget, true),
                            new Fields("Steam64ID заблокированного", $"{UserIDTarget}", true),
                        };
                Color = 1432346;
            }


            FancyMessage newMessage = new FancyMessage(null, false, new FancyMessage.Embeds[1] { new FancyMessage.Embeds(null, Color, fields, new Authors("IQChat Mute-History", null, "https://i.imgur.com/xiwsg5m.png", null), null) });

            Request($"{MuteChat.Webhooks}", newMessage.toJSON());
        }


        #endregion

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

        #endregion

        #region Funcion

        #region AddOrRemove Auto Parametres

        private void SetupParametres(String ID, String Permissions)
        {
            UInt64 UserID = UInt64.Parse(ID);
            BasePlayer player = BasePlayer.FindByID(UserID);

            Configuration.ControllerConnection.Turned Controller = config.ControllerConnect.Turneds;
            Configuration.ControllerParameters Parameters = config.ControllerParameter;

            if (!UserInformation.ContainsKey(UserID)) return;
            User Info = UserInformation[UserID];

            if (Controller.TurnAutoSetupPrefix)
            {
                Configuration.ControllerParameters.AdvancedFuncion Prefixes = Parameters.Prefixes.Prefixes.FirstOrDefault(prefix => prefix.Permissions == Permissions);
                if (Prefixes == null) return;

                if (Parameters.Prefixes.TurnMultiPrefixes)
                    Info.Info.PrefixList.Add(Prefixes.Argument);
                else Info.Info.Prefix = Prefixes.Argument;

                if (player != null)
                    ReplySystem(player, GetLang("PREFIX_SETUP", player.UserIDString, Prefixes.Argument));

                Log($"Игрок ({UserID}) успешно забрал префикс {Prefixes.Argument}");
            }
            if (Controller.TurnAutoSetupColorNick)
            {
                Configuration.ControllerParameters.AdvancedFuncion ColorNick = Parameters.NickColorList.FirstOrDefault(nick => nick.Permissions == Permissions);
                if (ColorNick == null) return;
                Info.Info.ColorNick = ColorNick.Argument;

                if (player != null)
                    ReplySystem(player, GetLang("COLOR_NICK_SETUP", player.UserIDString, ColorNick.Argument));

                Log($"Игрок ({UserID}) успешно забрал цвет ника {ColorNick.Argument}");
            }
            if (Controller.TurnAutoSetupColorChat)
            {
                Configuration.ControllerParameters.AdvancedFuncion ColorChat = Parameters.MessageColorList.FirstOrDefault(message => message.Permissions == Permissions);
                if (ColorChat == null) return;
                Info.Info.ColorMessage = ColorChat.Argument;

                if (player != null)
                    ReplySystem(player, GetLang("COLOR_CHAT_SETUP", player.UserIDString, ColorChat.Argument));

                Log($"Игрок ({UserID}) успешно забрал цвет чата {ColorChat.Argument}");
            }
        }

        private void RemoveParametres(String ID, String Permissions)
        {
            UInt64 UserID = UInt64.Parse(ID);
            BasePlayer player = BasePlayer.FindByID(UserID);

            Configuration.ControllerConnection Controller = config.ControllerConnect;
            Configuration.ControllerParameters Parameters = config.ControllerParameter;

            if (!UserInformation.ContainsKey(UserID)) return;
            User Info = UserInformation[UserID];

            if (Controller.Turneds.TurnAutoDropPrefix)
            {
                if (Parameters.Prefixes.TurnMultiPrefixes)
                {
                    Configuration.ControllerParameters.AdvancedFuncion Prefixes = Parameters.Prefixes.Prefixes.FirstOrDefault(prefix => Info.Info.PrefixList.Contains(prefix.Argument) && prefix.Permissions == Permissions);
                    if (Prefixes == null) return;

                    Info.Info.PrefixList.Remove(Prefixes.Argument);

                    if (player != null)
                        ReplySystem(player, GetLang("PREFIX_RETURNRED", player.UserIDString, Prefixes.Argument));

                    Log($"У игрока ({UserID}) истек префикс {Prefixes.Argument}");
                }
                else
                {
                    Configuration.ControllerParameters.AdvancedFuncion Prefixes = Parameters.Prefixes.Prefixes.FirstOrDefault(prefix => prefix.Argument == Info.Info.Prefix && prefix.Permissions == Permissions);
                    if (Prefixes == null) return;

                    Info.Info.Prefix = Controller.SetupDefaults.PrefixDefault;

                    if (player != null)
                        ReplySystem(player, GetLang("PREFIX_RETURNRED", player.UserIDString, Prefixes.Argument));

                    Log($"У игрока ({UserID}) истек префикс {Prefixes.Argument}");
                }
            }
            if (Controller.Turneds.TurnAutoSetupColorNick)
            {
                Configuration.ControllerParameters.AdvancedFuncion ColorNick = Parameters.NickColorList.FirstOrDefault(nick => Info.Info.ColorNick == nick.Argument && nick.Permissions == Permissions);
                if (ColorNick == null) return;

                Info.Info.ColorNick = Controller.SetupDefaults.NickDefault;

                if (player != null)
                    ReplySystem(player, GetLang("COLOR_NICK_RETURNRED", player.UserIDString, ColorNick.Argument));

                Log($"У игрока ({UserID}) истек цвет ника {ColorNick.Argument}");
            }
            if (Controller.Turneds.TurnAutoSetupColorChat)
            {
                Configuration.ControllerParameters.AdvancedFuncion ColorChat = Parameters.MessageColorList.FirstOrDefault(message => Info.Info.ColorMessage == message.Argument && message.Permissions == Permissions);
                if (ColorChat == null) return;

                Info.Info.ColorMessage = Controller.SetupDefaults.MessageDefault;

                if (player != null)
                    ReplySystem(player, GetLang("COLOR_CHAT_RETURNRED", player.UserIDString, ColorChat.Argument));

                Log($"У игрока ({UserID}) истек цвет чата {ColorChat.Argument}");
            }
        }

        #endregion

        #region Main Chat Funcion
        void ReplyChat(Chat.ChatChannel channel, BasePlayer player, String OutMessage)
        {
            Configuration.ControllerMessage ControllerMessages = config.ControllerMessages;

            User Info = UserInformation[player.userID];
            GeneralInformation.RenameInfo RenameInfo = GeneralInfo.GetInfoRename(player.userID);
            UInt64 RenameID = RenameInfo != null ? RenameInfo.RenameID != 0 ? RenameInfo.RenameID : player.userID : player.userID;

            if (channel == Chat.ChatChannel.Global)
            {
                foreach (BasePlayer p in BasePlayer.activePlayerList)
                {
                    if (OutMessage.Contains("@"))
                    {
                        String SplittedName = OutMessage.Substring(OutMessage.IndexOf('@')).Replace("@", "").Split(' ')[0];

                        BasePlayer playerTags = GetPlayerNickOrID(SplittedName);

                        if (playerTags != null)
                        {
                            User InfoP = UserInformation[playerTags.userID];

                            if (InfoP.Settings.TurnAlert && p == playerTags)
                            {
                                ReplySystem(p, $"<size=16>{OutMessage.Trim()}</size>", GetLang("IQCHAT_FUNCED_ALERT_TITLE", p.UserIDString), p.UserIDString, ControllerMessages.GeneralSetting.AlertFormat.AlertPlayerColor);
                                if (InfoP.Settings.TurnSound)
                                    Effect.server.Run(ControllerMessages.GeneralSetting.AlertFormat.SoundAlertPlayer, playerTags.GetNetworkPosition());
                            }
                            else p.SendConsoleCommand("chat.add", new object[] { (int)channel, RenameID, OutMessage });
                        }
                        else p.SendConsoleCommand("chat.add", new object[] { (int)channel, RenameID, OutMessage });
                    }
                    else p.SendConsoleCommand("chat.add", new object[] { (int)channel, RenameID, OutMessage });

                    p.ConsoleMessage(OutMessage);
                }
            }
            if (channel == Chat.ChatChannel.Team)
            {
                RelationshipManager.PlayerTeam Team = RelationshipManager._instance.FindTeam(player.currentTeam);
                if (Team == null) return;
                foreach (var FindPlayers in Team.members)
                {
                    BasePlayer TeamPlayer = BasePlayer.FindByID(FindPlayers);
                    if (TeamPlayer == null) continue;

                    TeamPlayer.SendConsoleCommand("chat.add", channel, RenameID, OutMessage);
                }
            }
           /* if (channel == Chat.ChatChannel.Cards)
            {
                if (!player.isMounted)
                    return;

                CardTable cardTable = player.GetMountedVehicle() as CardTable;
                if (cardTable == null || !cardTable.GameController.PlayerIsInGame(player))
                    return;

                List<Network.Connection> PlayersCards = new List<Network.Connection>();
                cardTable.GameController.GetConnectionsInGame(PlayersCards);
                if (PlayersCards == null || PlayersCards.Count == 0)
                    return;

                foreach (Network.Connection PCard in PlayersCards)
                {
                    BasePlayer PlayerInRound = BasePlayer.FindByID(PCard.userid);
                    if (PlayerInRound == null) return;
                    PlayerInRound.SendConsoleCommand("chat.add", channel, RenameID, OutMessage);
                }
            }*/
        }

        void ReplySystem(BasePlayer player, String Message, String CustomPrefix = null, String CustomAvatar = null, String CustomHex = null)
        {
            Configuration.ControllerMessage ControllerMessages = config.ControllerMessages;

            String Prefix = (CustomPrefix == null || String.IsNullOrWhiteSpace(CustomPrefix)) ? (ControllerMessages.GeneralSetting.BroadcastFormat.BroadcastTitle == null || String.IsNullOrWhiteSpace(ControllerMessages.GeneralSetting.BroadcastFormat.BroadcastTitle)) ? "" : ControllerMessages.GeneralSetting.BroadcastFormat.BroadcastTitle : CustomPrefix;
            String AvatarID = (CustomAvatar == null || String.IsNullOrWhiteSpace(CustomAvatar)) ? (ControllerMessages.GeneralSetting.BroadcastFormat.Steam64IDAvatar == null || String.IsNullOrWhiteSpace(ControllerMessages.GeneralSetting.BroadcastFormat.Steam64IDAvatar)) ? "0" : ControllerMessages.GeneralSetting.BroadcastFormat.Steam64IDAvatar : CustomAvatar;
            String Hex = (CustomHex == null || String.IsNullOrWhiteSpace(CustomHex)) ? (ControllerMessages.GeneralSetting.BroadcastFormat.BroadcastColor == null || String.IsNullOrWhiteSpace(ControllerMessages.GeneralSetting.BroadcastFormat.BroadcastColor)) ? "#ffff" : ControllerMessages.GeneralSetting.BroadcastFormat.BroadcastColor : CustomHex;

            player.SendConsoleCommand("chat.add", Chat.ChatChannel.Global, AvatarID, $"{Prefix}<color={Hex}>{Message}</color>");
        }

        void ReplyBroadcast(String Message, String CustomPrefix = null, String CustomAvatar = null, Boolean AdminAlert = false)
        {
            foreach (BasePlayer p in !AdminAlert ? BasePlayer.activePlayerList.Where(p => UserInformation[p.userID].Settings.TurnBroadcast) : BasePlayer.activePlayerList)
                ReplySystem(p, Message, CustomPrefix, CustomAvatar);
        }

        #endregion

        #region Chat Controller
        public Boolean IsNoob(UInt64 userID, Int32 TimeBlocked)
        {
            if (UserInformationConnection.ContainsKey(userID))
                return UserInformationConnection[userID].IsNoob(TimeBlocked);
            return false;
        }
        public void AnwserMessage(BasePlayer player, String Message)
        {
            Configuration.AnswerMessage Anwser = config.AnswerMessages;
            if (!Anwser.UseAnswer) return;
            foreach (KeyValuePair<String, String> Anwsers in Anwser.AnswerMessageList)
                if (Message.Contains(Anwsers.Key.ToLower()))
                    ReplySystem(player, Anwsers.Value);
        }
        private void AddHistoryMessage(BasePlayer player, String Message)
        {
            if (!LastMessagesChat.ContainsKey(player))
                LastMessagesChat.Add(player, new List<String> { Message });
            else LastMessagesChat[player].Add(Message);
        }
        private String GetLastMessage(BasePlayer player, Int32 Count)
        {
            String Messages = String.Empty;

            if (LastMessagesChat.ContainsKey(player))
            {
                foreach (String Message in LastMessagesChat[player].Take(Count))
                    Messages += $"\n{Message}";
            }

            return Messages;
        }

        public Dictionary<UInt64, FlooderInfo> Flooders = new Dictionary<UInt64, FlooderInfo>();
        internal class FlooderInfo
        {
            public Double Time;
            public String LastMessage;
            public Int32 TryFlood;
        }

        private Tuple<String, Boolean> BadWordsCleaner(String FormattingMessage, String ReplaceBadWord, List<String> BadWords)
        {
            String ResultMessage = FormattingMessage;
            Boolean IsBadWords = false;

            for (Int32 i = 0; i < BadWords.Count; i++)
                foreach (String Bad in FormattingMessage.Split(' ').Where(x => BadWords[i].Equals(x, StringComparison.OrdinalIgnoreCase)))
                {
                    ResultMessage = ResultMessage.Replace(Bad, ReplaceBadWord, StringComparison.OrdinalIgnoreCase);
                    IsBadWords = true;
                }

            return Tuple.Create(ResultMessage, IsBadWords);
        }
        private String RemoveLinkText(String text)
        {
            String hrefPattern = "([A-Za-z0-9-А-Яа-я]|https?://)[^ ]+\\.(com|lt|net|org|gg|ru|рф|int|info|ru.com|ru.net|com.ru|net.ru|рус|org.ru|moscow|biz|орг|su)";
            Regex rgx = new Regex(hrefPattern, RegexOptions.IgnoreCase);
            return rgx.Replace(text, "").Trim();
        }

        private void SeparatorChat(Chat.ChatChannel channel, BasePlayer player, String Message)
        {
            Configuration.ControllerMessage.TurnedFuncional.AntiNoob.Settings antiNoob = config.ControllerMessages.TurnedFunc.AntiNoobSetting.AntiNoobChat;
            if (antiNoob.AntiNoobActivate)
                if (IsNoob(player.userID, antiNoob.TimeBlocked))
                {
                    ReplySystem(player, GetLang("IQCHAT_INFO_ANTI_NOOB", player.UserIDString, FormatTime(UserInformationConnection[player.userID].LeftTime(antiNoob.TimeBlocked), player.UserIDString)));
                    return;
                }

            Configuration.ControllerMessage ControllerMessage = config.ControllerMessages;

            if (ControllerMessage.TurnedFunc.AntiSpamSetting.AntiSpamActivate)
                if (!permission.UserHasPermission(player.UserIDString, PermissionAntiSpam))
                {
                    if (!Flooders.ContainsKey(player.userID))
                        Flooders.Add(player.userID, new FlooderInfo { Time = CurrentTime + ControllerMessage.TurnedFunc.AntiSpamSetting.FloodTime, LastMessage = Message });
                    else
                    {
                        if (Flooders[player.userID].Time > CurrentTime)
                        {
                            ReplySystem(player, GetLang("FLOODERS_MESSAGE", player.UserIDString, Convert.ToInt32(Flooders[player.userID].Time - CurrentTime)));
                            return;
                        }

                        if (ControllerMessage.TurnedFunc.AntiSpamSetting.AntiSpamDuplesSetting.AntiSpamDuplesActivate)
                        {
                            if (Flooders[player.userID].LastMessage == Message)
                            {
                                if (Flooders[player.userID].TryFlood >= ControllerMessage.TurnedFunc.AntiSpamSetting.AntiSpamDuplesSetting.TryDuples)
                                {
                                    MutePlayer(player, MuteType.Chat, 0, null, ControllerMessage.TurnedFunc.AntiSpamSetting.AntiSpamDuplesSetting.MuteSetting.Reason, ControllerMessage.TurnedFunc.AntiSpamSetting.AntiSpamDuplesSetting.MuteSetting.SecondMute);
                                    Flooders[player.userID].TryFlood = 0;
                                    return;
                                }
                                Flooders[player.userID].TryFlood++;
                            }
                        }
                    }
                    Flooders[player.userID].Time = ControllerMessage.TurnedFunc.AntiSpamSetting.FloodTime + CurrentTime;
                    Flooders[player.userID].LastMessage = Message;
                }

            GeneralInformation General = GeneralInfo;
            GeneralInformation.RenameInfo RenameInformation = General.GetInfoRename(player.userID);

            User Info = UserInformation[player.userID];

            Configuration.ControllerParameters ControllerParameter = config.ControllerParameter;
            Configuration.ControllerMute ControllerMutes = config.ControllerMutes;
            Configuration.ControllerMessage.GeneralSettings.OtherSettings OtherController = config.ControllerMessages.GeneralSetting.OtherSetting;

            if (General.TurnMuteAllChat)
            {
                ReplySystem(player, GetLang("IQCHAT_FUNCED_NO_SEND_CHAT_MUTED_ALL_CHAT", player.UserIDString));
                return;
            }

            if (Info.MuteInfo.IsMute(MuteType.Chat))
            {
                ReplySystem(player, GetLang("IQCHAT_FUNCED_NO_SEND_CHAT_MUTED", player.UserIDString, FormatTime(Info.MuteInfo.GetTime(MuteType.Chat), player.UserIDString)));
                return;
            }

            String SendFormat = String.Empty;
            String Prefixes = String.Empty;
            String FormattingMessage = Message;

            String DisplayName = player.displayName;

            if (ControllerMessage.Formatting.ControllerNickname.UseNickController)
            {
                Tuple<String, Boolean> GetTupleNick = BadWordsCleaner(DisplayName, ControllerMessage.Formatting.ControllerNickname.ReplaceBadNick, ControllerMessage.Formatting.ControllerNickname.BadNicks);
                DisplayName = GetTupleNick.Item1;

                DisplayName = RemoveLinkText(DisplayName);
            }

            UInt64 UserID = player.userID;
            if (RenameInformation != null)
            {
                DisplayName = RenameInformation.RenameNick;
                UserID = RenameInformation.RenameID;
            }

            String ColorNickPlayer = String.IsNullOrWhiteSpace(Info.Info.ColorNick) ? player.IsAdmin ? "#a8fc55" : "#54aafe" : Info.Info.ColorNick;
            DisplayName = $"<color={ColorNickPlayer}>{DisplayName}</color>";

            String ChannelMessage = channel == Chat.ChatChannel.Team ? "<color=#a5e664>[Team]</color>" : "";

            if (ControllerMessage.Formatting.UseBadWords)
            {
                Tuple<String, Boolean> GetTuple = BadWordsCleaner(Message, ControllerMessage.Formatting.ReplaceBadWord, ControllerMessage.Formatting.BadWords);
                FormattingMessage = GetTuple.Item1;

                if (GetTuple.Item2)
                {
                    if (permission.UserHasPermission(player.UserIDString, PermissionMute))
                        IQPersonalSendBadWords(player);

                    if (ControllerMutes.AutoMuteSettings.UseAutoMute)
                        MutePlayer(player, MuteType.Chat, 0, null, ControllerMutes.AutoMuteSettings.AutoMuted.Reason, ControllerMutes.AutoMuteSettings.AutoMuted.SecondMute);
                }
            }

            if (ControllerMessage.Formatting.FormatMessage)
                FormattingMessage = $"{FormattingMessage.Substring(0, 1).ToUpper()}{FormattingMessage.Remove(0, 1).ToLower()}";

            if (ControllerParameter.Prefixes.TurnMultiPrefixes)
            {
                if (Info.Info.PrefixList != null)
                    Prefixes = String.Join("", Info.Info.PrefixList.Take(ControllerParameter.Prefixes.MaximumMultiPrefixCount));
            }
            else Prefixes = Info.Info.Prefix;

            String ResultMessage = String.IsNullOrWhiteSpace(Info.Info.ColorMessage) ? FormattingMessage : $"<color={Info.Info.ColorMessage}>{FormattingMessage}</color>";

            String Rank = String.Empty;
            String RankTime = String.Empty;
            if (IQRankSystem)
            {
                Configuration.ReferenceSettings.IQRankSystem IQRank = config.ReferenceSetting.IQRankSystems;

                if (IQRank.UseRankSystem)
                {
                    if (IQRank.UseTimeStandart)
                        RankTime = String.IsNullOrWhiteSpace(IQRankGetTimeGame(player.userID)) ? "" : String.Format(IQRank.FormatRank, IQRankGetTimeGame(player.userID));
                    Rank = String.IsNullOrWhiteSpace(IQRankGetRank(player.userID)) ? "" : String.Format(IQRank.FormatRank, IQRankGetRank(player.userID));
                }
            }

            SendFormat = $"{ChannelMessage} {RankTime} {Rank} <size={OtherController.SizePrefix}>{Prefixes}</size> <size={OtherController.SizeNick}>{DisplayName}</size>: <size={OtherController.SizeMessage}>{ResultMessage}</size>";

            if (config.RustPlusSettings.UseRustPlus)
                if (channel == Chat.ChatChannel.Team)
                {
                    RelationshipManager.PlayerTeam Team = RelationshipManager._instance.FindTeam(player.currentTeam);
                    if (Team == null) return;
                    Util.BroadcastTeamChat(player.Team, player.userID, player.displayName, FormattingMessage, Info.Info.ColorMessage);
                }

            if (ControllerMutes.LoggedMute.UseHistoryMessage && config.OtherSetting.LogsMuted.UseLogged)
                AddHistoryMessage(player, FormattingMessage);

            ReplyChat(channel, player, SendFormat);
            AnwserMessage(player, ResultMessage.ToLower());
            Puts($"{player.displayName}({player.UserIDString}): {FormattingMessage}");
            Log($"СООБЩЕНИЕ В ЧАТ : {player}: {ChannelMessage} {FormattingMessage}");
            DiscordLoggChat(player, channel, Message);

            RCon.Broadcast(RCon.LogType.Chat, new Chat.ChatEntry
            {
                Message = $"{player.displayName} : {FormattingMessage}",
                UserId = player.UserIDString,
                Username = player.displayName,
                Channel = channel,
                Time = (DateTime.UtcNow.Hour * 3600) + (DateTime.UtcNow.Minute * 60),
            });
        }

        #endregion

        #region Alert Controller
        public void BroadcastAuto()
        {
            Configuration.ControllerAlert.Alert Broadcast = config.ControllerAlertSetting.AlertSetting;

            if (Broadcast.AlertMessage)
            {
                Int32 IndexBroadkastNow = 0;
                String RandomMsg = String.Empty;

                timer.Every(Broadcast.MessageListTimer, () =>
                 {
                     if (Broadcast.AlertMessageType)
                     {
                         if (IndexBroadkastNow >= Broadcast.MessageList.Count)
                             IndexBroadkastNow = 0;
                         RandomMsg = Broadcast.MessageList[IndexBroadkastNow++];
                     }
                     else RandomMsg = Broadcast.MessageList.GetRandom(); ;
                     ReplyBroadcast(RandomMsg);
                 });
            }
        }

        private void AlertDisconnected(BasePlayer player, String reason)
        {
            Configuration.ControllerAlert.AdminSession AlertSessionAdmin = config.ControllerAlertSetting.AdminSessionSetting;
            Configuration.ControllerAlert.PlayerSession AlertSessionPlayer = config.ControllerAlertSetting.PlayerSessionSetting;
            GeneralInformation.RenameInfo RenameInformation = GeneralInfo.GetInfoRename(player.userID);

            if (AlertSessionPlayer.DisconnectedAlert)
            {
                if (!AlertSessionAdmin.DisconnectedAlertAdmin)
                    if (player.IsAdmin) return;

                String DisplayName = player.displayName;

                Configuration.ControllerMessage ControllerMessage = config.ControllerMessages;

                if (ControllerMessage.Formatting.ControllerNickname.UseNickController)
                    foreach (String DetectedBadNick in DisplayName.Split(' '))
                    {
                        if (ControllerMessage.Formatting.ControllerNickname.BadNicks.Count(x => x.ToLower() == DetectedBadNick.ToLower()) > 0)
                            DisplayName = DisplayName.Replace(DetectedBadNick, ControllerMessage.Formatting.ControllerNickname.ReplaceBadNick);
                    }

                UInt64 UserID = player.userID;
                if (RenameInformation != null)
                {
                    DisplayName = RenameInformation.RenameNick;
                    UserID = RenameInformation.RenameID;
                }

                String Avatar = AlertSessionPlayer.ConnectedAvatarUse ? UserID.ToString() : String.Empty;
                String Message = String.Empty;

                if (AlertSessionPlayer.DisconnectedAlertRandom)
                {
                    sb.Clear();
                    Message = sb.AppendFormat(AlertSessionPlayer.RandomDisconnectedAlert.GetRandom(), DisplayName, reason).ToString();
                }
                else Message = AlertSessionPlayer.DisconnectedReason ? GetLang("LEAVE_PLAYER_REASON", player.UserIDString, DisplayName, reason) : GetLang("LEAVE_PLAYER", player.UserIDString, DisplayName);

                if (!permission.UserHasPermission(player.UserIDString, PermissionHideDisconnection))
                    ReplyBroadcast(Message, "", Avatar);

                Log($"[{player.userID}] {Message}");
            }
        }
        private void AlertController(BasePlayer player)
        {
            Configuration.ControllerAlert.Alert Alert = config.ControllerAlertSetting.AlertSetting;
            Configuration.ControllerAlert.AdminSession AlertSessionAdmin = config.ControllerAlertSetting.AdminSessionSetting;
            Configuration.ControllerAlert.PlayerSession AlertSessionPlayer = config.ControllerAlertSetting.PlayerSessionSetting;
            Configuration.ControllerAlert.PersonalAlert AlertPersonal = config.ControllerAlertSetting.PersonalAlertSetting;
            GeneralInformation.RenameInfo RenameInformation = GeneralInfo.GetInfoRename(player.userID);
            Configuration.ControllerMessage ControllerMessage = config.ControllerMessages;

            String DisplayName = player.displayName;

            if (ControllerMessage.Formatting.ControllerNickname.UseNickController)
                foreach (String DetectedBadNick in DisplayName.Split(' '))
                {
                    if (ControllerMessage.Formatting.ControllerNickname.BadNicks.Count(x => x.ToLower() == DetectedBadNick.ToLower()) > 0)
                        DisplayName = DisplayName.Replace(DetectedBadNick, ControllerMessage.Formatting.ControllerNickname.ReplaceBadNick);
                }

            UInt64 UserID = player.userID;
            if (RenameInformation != null)
            {
                DisplayName = RenameInformation.RenameNick;
                UserID = RenameInformation.RenameID;
            }

            if (AlertSessionPlayer.ConnectedAlert)
            {
                if (!AlertSessionAdmin.ConnectedAlertAdmin)
                    if (player.IsAdmin) return;

                String Avatar = AlertSessionPlayer.ConnectedAvatarUse ? UserID.ToString() : String.Empty;
                String Message = String.Empty;

                if (AlertSessionPlayer.ConnectedWorld)
                {
                    webrequest.Enqueue("http://ip-api.com/json/" + player.net.connection.ipaddress.Split(':')[0], null, (code, response) =>
                    {
                        if (code != 200 || response == null)
                            return;

                        String country = JsonConvert.DeserializeObject<Response>(response).Country;

                        if (AlertSessionPlayer.ConnectionAlertRandom)
                        {
                            sb.Clear();
                            Int32 RandomIndex = UnityEngine.Random.Range(0, AlertSessionPlayer.RandomConnectionAlert.Count);
                            Message = sb.AppendFormat(AlertSessionPlayer.RandomConnectionAlert[RandomIndex], DisplayName, country).ToString();
                        }
                        else Message = GetLang("WELCOME_PLAYER_WORLD", player.UserIDString, DisplayName, country);

                        if (!permission.UserHasPermission(player.UserIDString, PermissionHideConnection))
                            ReplyBroadcast(Message, "", Avatar);

                        Log($"[{player.userID}] {Message}");
                    }, this);
                }
                else
                {
                    if (AlertSessionPlayer.ConnectionAlertRandom)
                    {
                        sb.Clear();
                        Message = sb.AppendFormat(AlertSessionPlayer.RandomConnectionAlert.GetRandom(), DisplayName).ToString();
                    }
                    else Message = GetLang("WELCOME_PLAYER", player.UserIDString, DisplayName);

                    if (!permission.UserHasPermission(player.UserIDString, PermissionHideConnection))
                        ReplyBroadcast(Message, "", Avatar);

                    Log($"[{player.userID}] {Message}");
                }
            }
            if (AlertPersonal.UseWelcomeMessage)
            {
                String WelcomeMessage = AlertPersonal.WelcomeMessage.GetRandom();
                ReplySystem(player, WelcomeMessage);
            }
        }
        #endregion

        #region Mute Controller
        private void MutePlayer(BasePlayer Target, MuteType Type, Int32 ReasonIndex, BasePlayer Moderator = null, String ReasonCustom = null, Int32 TimeCustom = 0, Boolean HideMute = false, Boolean Command = false, UInt64 IDFake = 0)
        {
            Configuration.ControllerMute ControllerMutes = config.ControllerMutes;

            if (IQFakeActive && Target == null && (IQFakeActive && Target == null && IDFake != 0))
            {
                ReplySystem(Moderator, GetLang(Type == MuteType.Chat ? "FUNC_MESSAGE_MUTE_CHAT" : "FUNC_MESSAGE_MUTE_VOICE", Moderator != null ? Moderator.displayName : Moderator.UserIDString, GetLang("IQCHAT_FUNCED_ALERT_TITLE_SERVER"), FindFakeName(IDFake), FormatTime(TimeCustom == 0 ? config.ControllerMutes.MuteChatReasons[ReasonIndex].SecondMute : TimeCustom), ReasonCustom ?? config.ControllerMutes.MuteChatReasons[ReasonIndex].Reason));
                RemoveReserved(IDFake);
                FakePlayer FakeP = PlayerBases.FirstOrDefault(x => x.UserID == IDFake);
                if (FakeP != null)
                    PlayerBases.Remove(FakeP);
                return;
            }

            if (!UserInformation.ContainsKey(Target.userID)) return;
            User Info = UserInformation[Target.userID];

            String LangMessage = String.Empty;
            String Reason = String.Empty;
            Int32 MuteTime = 0;

            String NameModerator = GetLang("IQCHAT_FUNCED_ALERT_TITLE_SERVER", Target.UserIDString);

            if (Moderator != null)
            {
                GeneralInformation.RenameInfo ModeratorRename = GeneralInfo.GetInfoRename(Moderator.userID);
                NameModerator = ModeratorRename != null ? $"{ModeratorRename.RenameNick ?? Moderator.displayName}" : Moderator.displayName;
            }

            GeneralInformation.RenameInfo TagetRename = GeneralInfo.GetInfoRename(Target.userID);
            String TargetName = TagetRename != null ? $"{TagetRename.RenameNick ?? Target.displayName}" : Target.displayName;

            if (Target == null || !Target.IsConnected)
            {
                if (Moderator != null && !Command)
                    ReplySystem(Moderator, GetLang("UI_CHAT_PANEL_MODERATOR_MUTE_PANEL_TAKE_TYPE_CHAT_ACTION_NOT_CONNNECTED", Moderator.UserIDString));
                return;
            }

            if (Moderator != null && !Command)
                if (Info.MuteInfo.IsMute(Type))
                {
                    ReplySystem(Moderator, GetLang("IQCHAT_FUNCED_ALERT_TITLE_ISMUTED", Moderator.UserIDString));
                    return;
                }

            switch (Type)
            {
                case MuteType.Chat:
                    {
                        Reason = ReasonCustom ?? ControllerMutes.MuteChatReasons[ReasonIndex].Reason;
                        MuteTime = TimeCustom == 0 ? ControllerMutes.MuteChatReasons[ReasonIndex].SecondMute : TimeCustom;
                        LangMessage = "FUNC_MESSAGE_MUTE_CHAT";
                        break;
                    }
                case MuteType.Voice:
                    {
                        Reason = ReasonCustom ?? ControllerMutes.MuteVoiceReasons[ReasonIndex].Reason;
                        MuteTime = TimeCustom == 0 ? ControllerMutes.MuteVoiceReasons[ReasonIndex].SecondMute : TimeCustom;
                        LangMessage = "FUNC_MESSAGE_MUTE_VOICE";
                        break;
                    }
            }

            Info.MuteInfo.SetMute(Type, MuteTime);

            if (!HideMute)
                ReplyBroadcast(GetLang(LangMessage, Target.UserIDString, NameModerator, TargetName, FormatTime(MuteTime, Target.UserIDString), Reason));
            else
            {
                if (Target != null)
                    ReplySystem(Target, GetLang(LangMessage, Target.UserIDString, NameModerator, TargetName, FormatTime(MuteTime, Target.UserIDString), Reason));

                if (Moderator != null)
                    ReplySystem(Moderator, GetLang(LangMessage, Target.UserIDString, NameModerator, TargetName, FormatTime(MuteTime, Target.UserIDString), Reason));
            }

            if (Moderator != null && Moderator != Target)
                IQPersonalSendSetMute(Moderator);

            DiscordLoggMuted(Target, Type, Reason, FormatTime(MuteTime, Target.UserIDString), Moderator);
        }

        private void UnmutePlayer(BasePlayer Target, MuteType Type, BasePlayer Moderator = null, Boolean HideUnmute = false, Boolean Command = false)
        {
            if (!UserInformation.ContainsKey(Target.userID)) return;
            User Info = UserInformation[Target.userID];

            GeneralInformation.RenameInfo TargetRename = GeneralInfo.GetInfoRename(Target.userID);
            GeneralInformation.RenameInfo ModeratorRename = GeneralInfo.GetInfoRename(Moderator.userID);

            if (!Info.MuteInfo.IsMute(Type))
            {
                if (Moderator != null)
                    ReplySystem(Moderator, "У игрока нет блокировки");
                else Puts("У игрока нет блокировки!");
                return;
            }

            String TargetName = TargetRename != null ? $"{TargetRename.RenameNick ?? Target.displayName}" : Target.displayName;
            String NameModerator = Moderator == null ? GetLang("IQCHAT_FUNCED_ALERT_TITLE_SERVER", Target.UserIDString) : ModeratorRename != null ? $"{ModeratorRename.RenameNick ?? Moderator.displayName}" : Moderator.displayName;
            String LangMessage = Type == MuteType.Chat ? "FUNC_MESSAGE_UNMUTE_CHAT" : "FUNC_MESSAGE_UNMUTE_VOICE";

            if (!HideUnmute)
                ReplyBroadcast(GetLang(LangMessage, Target.UserIDString, NameModerator, TargetName));
            else
            {
                if (Target != null)
                    ReplySystem(Target, GetLang(LangMessage, Target.UserIDString, NameModerator, TargetName));
                if (Moderator != null)
                    ReplySystem(Moderator, GetLang(LangMessage, Target.UserIDString, NameModerator, TargetName));
            }

            Info.MuteInfo.UnMute(Type);

            DiscordLoggMuted(Target, Type, Moderator: Moderator);
        }

        #endregion

        #region Alert Metods
        void AlertUI(BasePlayer Sender, string[] arg)
        {
            if (_interface == null)
            {
                PrintWarning("Генерируем интерфейс, ожидайте сообщения об успешной генерации");
                return;
            }
            String Message = GetMessageInArgs(Sender, arg);
            if (Message == null) return;

            foreach (BasePlayer PlayerInList in BasePlayer.activePlayerList)
                DrawUI_IQChat_Alert(PlayerInList, Message);
        }
        void AlertUI(BasePlayer Sender, BasePlayer Recipient, string[] arg)
        {
            if (_interface == null)
            {
                PrintWarning("Генерируем интерфейс, ожидайте сообщения об успешной генерации");
                return;
            }
            String Message = GetMessageInArgs(Sender, arg);
            if (Message == null) return;

            DrawUI_IQChat_Alert(Recipient, Message);
        }
        void Alert(BasePlayer Sender, string[] arg, Boolean IsAdmin)
        {
            String Message = GetMessageInArgs(Sender, arg);
            if (Message == null) return;

            ReplyBroadcast(Message, AdminAlert: IsAdmin);

            if (config.RustPlusSettings.UseRustPlus)
                foreach (BasePlayer playerList in BasePlayer.activePlayerList)
                    NotificationList.SendNotificationTo(playerList.userID, NotificationChannel.SmartAlarm, config.RustPlusSettings.DisplayNameAlert, Message, Util.GetServerPairingData());
        }
        void Alert(BasePlayer Sender, BasePlayer Recipient, string[] arg)
        {
            String Message = GetMessageInArgs(Sender, arg);
            if (Message == null) return;

            ReplySystem(Recipient, Message);
        }

        #endregion

        #region ShowPlayersOnline

        private List<String> GetPlayersOnline()
        {
            List<String> PlayerNames = new List<String>();
            Int32 Count = 1;

            foreach (BasePlayer playerInList in BasePlayer.activePlayerList.Where(p => !permission.UserHasPermission(p.UserIDString, PermissionHideOnline)))
            {
                String ResultName = $"{Count} - {GetPlayerFormat(playerInList)}";
                PlayerNames.Add(ResultName);

                Count++;
            }

            if (IQFakeActive)
            {
                foreach (FakePlayer fakePlayer in PlayerBases.Where(x => IsFake(x.UserID)))
                {
                    String ResultName = $"{Count} - {API_GET_DEFAULT_PREFIX()}<color={API_GET_DEFAULT_NICK_COLOR()}>{fakePlayer.DisplayName}</color>";
                    PlayerNames.Add(ResultName);

                    Count++;
                }
            }

            return PlayerNames;
        }

        private String GetPlayerFormat(BasePlayer playerInList)
        {
            GeneralInformation.RenameInfo Renamer = GeneralInfo.GetInfoRename(playerInList.userID);
            String NickNamed = Renamer != null ? $"{Renamer.RenameNick ?? playerInList.displayName}" : playerInList.displayName;

            User Info = UserInformation[playerInList.userID];

            Configuration.ControllerParameters ControllerParameter = config.ControllerParameter;
            Configuration.ControllerMute ControllerMutes = config.ControllerMutes;

            String Prefixes = String.Empty;
            String ColorNickPlayer = String.IsNullOrWhiteSpace(Info.Info.ColorNick) ? playerInList.IsAdmin ? "#a8fc55" : "#54aafe" : Info.Info.ColorNick;

            if (ControllerParameter.Prefixes.TurnMultiPrefixes)
            {
                if (Info.Info.PrefixList != null)
                    Prefixes = String.Join("", Info.Info.PrefixList.Take(ControllerParameter.Prefixes.MaximumMultiPrefixCount));
            }
            else Prefixes = Info.Info.Prefix;

            String ResultName = $"{Prefixes}<color={ColorNickPlayer}>{NickNamed}</color>";

            return ResultName;
        }
        #endregion

        #endregion

        #region IQChat_Menu (Update UI 31.10)

        private class ImageUi
        {
            private static Coroutine coroutineImg = null;
            private static Dictionary<string, string> Images = new Dictionary<string, string>();
            public static void DownloadImages() { coroutineImg = ServerMgr.Instance.StartCoroutine(AddImage()); }

            private static IEnumerator AddImage()
            {
                _.PrintWarning("Генерируем интерфейс, ожидайте ~10-15 секунд!");

                foreach (var imageCfg in config.KeyImages)
                {
                    UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageCfg.Value);
                    yield return www.SendWebRequest();

                    if (_ == null)
                        yield break;
                    if (www.isNetworkError || www.isHttpError)
                    {
                        _.PrintWarning(string.Format("Image download error! Error: {0}, Image name: {1}", www.error, imageCfg.Key));
                        www.Dispose();
                        coroutineImg = null;
                        yield break;
                    }
                    Texture2D texture = DownloadHandlerTexture.GetContent(www);
                    if (texture != null)
                    {
                        byte[] bytes = texture.EncodeToPNG();

                        var image = FileStorage.server.Store(bytes, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString();
                        if (!Images.ContainsKey(imageCfg.Key))
                            Images.Add(imageCfg.Key, image);
                        else
                            Images[imageCfg.Key] = image;
                        UnityEngine.Object.DestroyImmediate(texture);
                    }

                    www.Dispose();
                    yield return CoroutineEx.waitForSeconds(0.02f);
                }
                coroutineImg = null;

                _interface = new InterfaceBuilder();
                _.PrintWarning("Интерфейс успешно загружен!");
            }

            public static string GetImage(String ImgKey)
            {
                if (Images.ContainsKey(ImgKey))
                    return Images[ImgKey];
                return _.GetImage("LOADING");
            }

            public static void Unload()
            {
                coroutineImg = null;
                foreach (var item in Images)
                    FileStorage.server.Remove(uint.Parse(item.Value), FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID);
            }
        }

        private static InterfaceBuilder _interface;
        private Dictionary<BasePlayer, InformationOpenedUI> LocalBase = new Dictionary<BasePlayer, InformationOpenedUI>();
        private class InformationOpenedUI
        {
            public List<Configuration.ControllerParameters.AdvancedFuncion> ElementsPrefix;
            public List<Configuration.ControllerParameters.AdvancedFuncion> ElementsNick;
            public List<Configuration.ControllerParameters.AdvancedFuncion> ElementsChat;
            public List<Configuration.ControllerParameters.AdvancedFuncion> ElementsRanks;
            public Int32 SlideIndexPrefix = 0;
            public Int32 SlideIndexNick = 0;
            public Int32 SlideIndexChat = 0;
            public Int32 SlideIndexRank = 0;
        }

        #region UpdateDisplayName Draw UI
        private void DrawUI_IQChat_Update_DisplayName(BasePlayer player)
        {
            String InterfaceVisualNick = InterfaceBuilder.GetInterface("UI_Chat_Context_Visual_Nick");
            User Info = UserInformation[player.userID];
            Configuration.ControllerParameters Controller = config.ControllerParameter;
            if (Info == null || InterfaceVisualNick == null || Controller == null) return;

            String DisplayNick = String.Empty;

            String Pattern = @"</?size.*?>";
            if (Controller.Prefixes.TurnMultiPrefixes)
            {
                if (Info.Info.PrefixList != null && Info.Info.PrefixList.Count != 0)
                    DisplayNick += Info.Info.PrefixList.Count > 1 ? $"{(Regex.IsMatch(Info.Info.PrefixList[0], Pattern) ? Regex.Replace(Info.Info.PrefixList[0], Pattern, "") : Info.Info.PrefixList[0])}+{Info.Info.PrefixList.Count - 1}" :
                        (Regex.IsMatch(Info.Info.PrefixList[0], Pattern) ? Regex.Replace(Info.Info.PrefixList[0], Pattern, "") : Info.Info.PrefixList[0]);
            }
            else DisplayNick += Regex.IsMatch(Info.Info.Prefix, Pattern) ? Regex.Replace(Info.Info.Prefix, Pattern, "") : Info.Info.Prefix;
            DisplayNick += $"<color={Info.Info.ColorNick ?? "#ffffff"}>{player.displayName}</color>: <color={Info.Info.ColorMessage ?? "#ffffff"}>{GetLang("IQCHAT_CONTEXT_NICK_DISPLAY_MESSAGE", player.UserIDString)}</color>";

            InterfaceVisualNick = InterfaceVisualNick.Replace("%NICK_DISPLAY%", DisplayNick);


            CuiHelper.DestroyUi(player, InterfaceBuilder.UI_Chat_Context_Visual_Nick);
            CuiHelper.AddUi(player, InterfaceVisualNick);
        }
        #endregion

        #region Context Draw UI
        private void DrawUI_IQChat_Context(BasePlayer player)
        {
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_Context");
            User Info = UserInformation[player.userID];
            Configuration.ControllerParameters ControllerParameter = config.ControllerParameter;
            if (Info == null || ControllerParameter == null || Interface == null) return;

            String BackgroundStatic = IQRankSystem && config.ReferenceSetting.IQRankSystems.UseRankSystem ? "UI_IQCHAT_CONTEXT_RANK" : "UI_IQCHAT_CONTEXT_NO_RANK";
            Interface = Interface.Replace("%IMG_BACKGROUND%", ImageUi.GetImage(BackgroundStatic));
            Interface = Interface.Replace("%TITLE%", GetLang("IQCHAT_CONTEXT_TITLE", player.UserIDString));
            Interface = Interface.Replace("%SETTING_ELEMENT%", GetLang("IQCHAT_CONTEXT_SETTING_ELEMENT_TITLE", player.UserIDString));
            Interface = Interface.Replace("%INFORMATION%", GetLang("IQCHAT_CONTEXT_INFORMATION_TITLE", player.UserIDString));
            Interface = Interface.Replace("%SETTINGS%", GetLang("IQCHAT_CONTEXT_SETTINGS_TITLE", player.UserIDString));
            Interface = Interface.Replace("%SETTINGS_PM%", GetLang("IQCHAT_CONTEXT_SETTINGS_PM_TITLE", player.UserIDString));
            Interface = Interface.Replace("%SETTINGS_ALERT%", GetLang("IQCHAT_CONTEXT_SETTINGS_ALERT_TITLE", player.UserIDString));
            Interface = Interface.Replace("%SETTINGS_ALERT_PM%", GetLang("IQCHAT_CONTEXT_SETTINGS_ALERT_PM_TITLE", player.UserIDString));
            Interface = Interface.Replace("%SETTINGS_SOUNDS%", GetLang("IQCHAT_CONTEXT_SETTINGS_SOUNDS_TITLE", player.UserIDString));
            Interface = Interface.Replace("%MUTE_STATUS_TITLE%", GetLang("IQCHAT_CONTEXT_MUTE_STATUS_TITLE", player.UserIDString));
            Interface = Interface.Replace("%IGNORED_STATUS_COUNT%", GetLang("IQCHAT_CONTEXT_IGNORED_STATUS_COUNT", player.UserIDString, Info.Settings.IgnoreUsers.Count));
            Interface = Interface.Replace("%IGNORED_STATUS_TITLE%", GetLang("IQCHAT_CONTEXT_IGNORED_STATUS_TITLE", player.UserIDString));
            Interface = Interface.Replace("%NICK_DISPLAY_TITLE%", GetLang("IQCHAT_CONTEXT_NICK_DISPLAY_TITLE", player.UserIDString));
            Interface = Interface.Replace("%MUTE_STATUS_PLAYER%", Info.MuteInfo.IsMute(MuteType.Chat) ? FormatTime(Info.MuteInfo.GetTime(MuteType.Chat), player.UserIDString) : GetLang("IQCHAT_CONTEXT_MUTE_STATUS_NOT", player.UserIDString));
            Interface = Interface.Replace("%SLIDER_PREFIX_TITLE%", GetLang("IQCHAT_CONTEXT_SLIDER_PREFIX_TITLE", player.UserIDString));
            Interface = Interface.Replace("%SLIDER_NICK_COLOR_TITLE%", GetLang("IQCHAT_CONTEXT_SLIDER_NICK_COLOR_TITLE", player.UserIDString));
            Interface = Interface.Replace("%SLIDER_MESSAGE_COLOR_TITLE%", GetLang("IQCHAT_CONTEXT_SLIDER_MESSAGE_COLOR_TITLE", player.UserIDString));
            Interface = Interface.Replace("%SLIDER_IQRANK_TITLE%", IQRankSystem && config.ReferenceSetting.IQRankSystems.UseRankSystem ? GetLang("IQCHAT_CONTEXT_SLIDER_IQRANK_TITLE", player.UserIDString) : String.Empty);

            CuiHelper.DestroyUi(player, InterfaceBuilder.UI_Chat_Context);
            CuiHelper.AddUi(player, Interface);

            DrawUI_IQChat_Update_DisplayName(player);

            if (ControllerParameter.VisualParametres.PrefixType == SelectedParametres.DropList || ControllerParameter.Prefixes.TurnMultiPrefixes)
                DrawUI_IQChat_DropList(player, "-46.788 67.4", "-14.788 91.4", GetLang("IQCHAT_CONTEXT_SLIDER_PREFIX_TITLE_DESCRIPTION", player.UserIDString), ControllerParameter.Prefixes.TurnMultiPrefixes ? TakeElementUser.MultiPrefix : TakeElementUser.Prefix);
            else DrawUI_IQChat_Sliders(player, "SLIDER_PREFIX", "-140 54", "-16 78", TakeElementUser.Prefix);

            if (ControllerParameter.VisualParametres.NickColorType == SelectedParametres.DropList)
                DrawUI_IQChat_DropList(player, "112.34 67.4", "144.34 91.4", GetLang("IQCHAT_CONTEXT_SLIDER_CHAT_NICK_TITLE_DESCRIPTION", player.UserIDString), TakeElementUser.Nick);
            else DrawUI_IQChat_Sliders(player, "SLIDER_NICK_COLOR", "20 54", "144 78", TakeElementUser.Nick);

            if (ControllerParameter.VisualParametres.ChatColorType == SelectedParametres.DropList)
                DrawUI_IQChat_DropList(player, "-46.787 -0.591", "-14.787 23.409", GetLang("IQCHAT_CONTEXT_SLIDER_CHAT_MESSAGE_TITLE_DESCRIPTION", player.UserIDString), TakeElementUser.Chat);
            else DrawUI_IQChat_Sliders(player, "SLIDER_MESSAGE_COLOR", "-140 -12", "-16 12", TakeElementUser.Chat);


            if (IQRankSystem && config.ReferenceSetting.IQRankSystems.UseRankSystem)
            {
                if (ControllerParameter.VisualParametres.IQRankSystemType == SelectedParametres.DropList)
                    DrawUI_IQChat_DropList(player, "112.34 -0.591", "144.34 23.409", GetLang("IQCHAT_CONTEXT_SLIDER_IQRANK_TITLE_DESCRIPTION", player.UserIDString), TakeElementUser.Rank);
                else DrawUI_IQChat_Sliders(player, "SLIDER_IQRANK", "20 -12", "144 12", TakeElementUser.Rank);
            }

            DrawUI_IQChat_Update_Check_Box(player, ElementsSettingsType.PM, "143.38 -67.9", "151.38 -59.9", Info.Settings.TurnPM);
            DrawUI_IQChat_Update_Check_Box(player, ElementsSettingsType.Broadcast, "143.38 -79.6", "151.38 -71.6", Info.Settings.TurnBroadcast);
            DrawUI_IQChat_Update_Check_Box(player, ElementsSettingsType.Alert, "143.38 -91.6", "151.38 -83.6", Info.Settings.TurnAlert);
            DrawUI_IQChat_Update_Check_Box(player, ElementsSettingsType.Sound, "143.38 -103.6", "151.38 -95.6", Info.Settings.TurnSound);
            DrawUI_IQChat_Context_AdminAndModeration(player);
        }
        private void DrawUI_IQChat_Context_AdminAndModeration(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermissionMute)) return;

            String InterfaceModeration = InterfaceBuilder.GetInterface("UI_Chat_Moderation");
            if (InterfaceModeration == null) return;

            InterfaceModeration = InterfaceModeration.Replace("%TITLE%", GetLang("IQCHAT_TITLE_MODERATION_PANEL", player.UserIDString));
            InterfaceModeration = InterfaceModeration.Replace("%COMMAND_MUTE_MENU%", $"newui.cmd action.mute.ignore open {SelectedAction.Mute}");
            InterfaceModeration = InterfaceModeration.Replace("%TEXT_MUTE_MENU%", GetLang("IQCHAT_BUTTON_MODERATION_MUTE_MENU", player.UserIDString));

            CuiHelper.AddUi(player, InterfaceModeration);

            DrawUI_IQChat_Update_MuteChat_All(player);
            DrawUI_IQChat_Update_MuteVoice_All(player);
        }
        private void DrawUI_IQChat_Update_MuteChat_All(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermissionMutedAdmin)) return;

            String InterfaceAdministratorChat = InterfaceBuilder.GetInterface("UI_Chat_Administation_AllChat");
            if (InterfaceAdministratorChat == null) return;

            InterfaceAdministratorChat = InterfaceAdministratorChat.Replace("%TEXT_MUTE_ALLCHAT%", GetLang(!GeneralInfo.TurnMuteAllChat ? "IQCHAT_BUTTON_MODERATION_MUTE_ALL_CHAT" : "IQCHAT_BUTTON_MODERATION_UNMUTE_ALL_CHAT", player.UserIDString));
            InterfaceAdministratorChat = InterfaceAdministratorChat.Replace("%COMMAND_MUTE_ALLCHAT%", $"newui.cmd action.mute.ignore mute.controller {SelectedAction.Mute} mute.all.chat");

            CuiHelper.DestroyUi(player, "ModeratorMuteAllChat");
            CuiHelper.AddUi(player, InterfaceAdministratorChat);
        }
        private void DrawUI_IQChat_Update_MuteVoice_All(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermissionMutedAdmin)) return;

            String InterfaceAdministratorVoice = InterfaceBuilder.GetInterface("UI_Chat_Administation_AllVoce");
            if (InterfaceAdministratorVoice == null) return;

            InterfaceAdministratorVoice = InterfaceAdministratorVoice.Replace("%TEXT_MUTE_ALLVOICE%", GetLang(!GeneralInfo.TurnMuteAllVoice ? "IQCHAT_BUTTON_MODERATION_MUTE_ALL_VOICE" : "IQCHAT_BUTTON_MODERATION_UNMUTE_ALL_VOICE", player.UserIDString));
            InterfaceAdministratorVoice = InterfaceAdministratorVoice.Replace("%COMMAND_MUTE_ALLVOICE%", $"newui.cmd action.mute.ignore mute.controller {SelectedAction.Mute} mute.all.voice");

            CuiHelper.DestroyUi(player, "ModeratorMuteAllVoice");
            CuiHelper.AddUi(player, InterfaceAdministratorVoice);
        }
        #endregion

        #region MuteAndIgnore Draw UI

        #region Ignore Alert
        private void DrawUI_IQChat_Ignore_Alert(BasePlayer player, BasePlayer Target, UInt64 IDFake = 0)
        {
            String InterfacePanel = InterfaceBuilder.GetInterface("UI_Chat_Mute_And_Ignore_Alert_Panel");
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_Ignore_Alert");
            if (Interface == null || InterfacePanel == null) return;

            GeneralInformation.RenameInfo Renamer = (IQFakeActive && Target == null && IDFake != 0) ? null : GeneralInfo.GetInfoRename(Target.userID);
            String NickNamed = (IQFakeActive && Target == null && IDFake != 0) ? FindFakeName(IDFake) : Renamer != null ? $"{Renamer.RenameNick ?? Target.displayName}" : Target.displayName;

            Interface = Interface.Replace("%TITLE%", GetLang(UserInformation[player.userID].Settings.IsIgnored((IQFakeActive && Target == null && IDFake != 0) ? IDFake : Target.userID) ? "IQCHAT_TITLE_IGNORE_TITLES_UNLOCK" : "IQCHAT_TITLE_IGNORE_TITLES", player.UserIDString, NickNamed));
            Interface = Interface.Replace("%BUTTON_YES%", GetLang("IQCHAT_TITLE_IGNORE_BUTTON_YES", player.UserIDString));
            Interface = Interface.Replace("%BUTTON_NO%", GetLang("IQCHAT_TITLE_IGNORE_BUTTON_NO", player.UserIDString));
            Interface = Interface.Replace("%COMMAND%", $"newui.cmd action.mute.ignore ignore.and.mute.controller {SelectedAction.Ignore} confirm.yes {((IQFakeActive && Target == null && IDFake != 0) ? IDFake : Target.userID)}");

            CuiHelper.DestroyUi(player, "MUTE_AND_IGNORE_PANEL_ALERT");
            CuiHelper.AddUi(player, InterfacePanel);
            CuiHelper.AddUi(player, Interface);
        }
        #endregion

        #region Mute Alert
        private void DrawUI_IQChat_Mute_Alert(BasePlayer player, BasePlayer Target, UInt64 IDFake = 0)
        {
            String InterfacePanel = InterfaceBuilder.GetInterface("UI_Chat_Mute_And_Ignore_Alert_Panel");
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_Mute_Alert");
            if (Interface == null || InterfacePanel == null) return;

            User InfoTarget = (IQFakeActive && Target == null && IDFake != 0) ? null : UserInformation[Target.userID];

            Interface = Interface.Replace("%TITLE%", GetLang("IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT", player.UserIDString));
            Interface = Interface.Replace("%BUTTON_TAKE_CHAT_ACTION%", InfoTarget == null ? GetLang("IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT_CHAT", player.UserIDString) : InfoTarget.MuteInfo.IsMute(MuteType.Chat) ? GetLang("IQCHAT_BUTTON_MODERATION_UNMUTE_MENU_TITLE_ALERT_CHAT", player.UserIDString) : GetLang("IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT_CHAT", player.UserIDString));
            Interface = Interface.Replace("%BUTTON_TAKE_VOICE_ACTION%", InfoTarget == null ? GetLang("IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT_VOICE", player.UserIDString) : InfoTarget.MuteInfo.IsMute(MuteType.Voice) ? GetLang("IQCHAT_BUTTON_MODERATION_UNMUTE_MENU_TITLE_ALERT_VOICE", player.UserIDString) : GetLang("IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT_VOICE", player.UserIDString));
            Interface = Interface.Replace("%COMMAND_TAKE_ACTION_MUTE_CHAT%", InfoTarget == null ? $"newui.cmd action.mute.ignore ignore.and.mute.controller {SelectedAction.Mute} open.reason.mute {IDFake} {MuteType.Chat}" : InfoTarget.MuteInfo.IsMute(MuteType.Chat) ? $"newui.cmd action.mute.ignore ignore.and.mute.controller {SelectedAction.Mute} unmute.yes {Target.UserIDString} {MuteType.Chat}" : $"newui.cmd action.mute.ignore ignore.and.mute.controller {SelectedAction.Mute} open.reason.mute {Target.UserIDString} {MuteType.Chat}");
            Interface = Interface.Replace("%COMMAND_TAKE_ACTION_MUTE_VOICE%", InfoTarget == null ? $"newui.cmd action.mute.ignore ignore.and.mute.controller {SelectedAction.Mute} open.reason.mute {IDFake} {MuteType.Voice}" : InfoTarget.MuteInfo.IsMute(MuteType.Voice) ? $"newui.cmd action.mute.ignore ignore.and.mute.controller {SelectedAction.Mute} unmute.yes {Target.UserIDString} {MuteType.Voice}" : $"newui.cmd action.mute.ignore ignore.and.mute.controller {SelectedAction.Mute} open.reason.mute {Target.UserIDString} {MuteType.Voice}");

            CuiHelper.DestroyUi(player, "MUTE_AND_IGNORE_PANEL_ALERT");
            CuiHelper.AddUi(player, InterfacePanel);
            CuiHelper.AddUi(player, Interface);
        }
        private void DrawUI_IQChat_Mute_Alert_Reasons(BasePlayer player, BasePlayer Target, MuteType Type, UInt64 IDFake = 0)
        {
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_Mute_Alert_DropList_Title");
            if (Interface == null) return;

            Interface = Interface.Replace("%TITLE%", GetLang("IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT_REASON", player.UserIDString));

            CuiHelper.DestroyUi(player, "AlertMuteTitleReason");
            CuiHelper.DestroyUi(player, "PanelMuteReason");
            CuiHelper.AddUi(player, Interface);

            List<Configuration.ControllerMute.Muted> Reasons = Type == MuteType.Chat ? config.ControllerMutes.MuteChatReasons : config.ControllerMutes.MuteVoiceReasons;

            Int32 Y = 0;
            foreach (Configuration.ControllerMute.Muted Reason in Reasons.Take(6))
                DrawUI_IQChat_Mute_Alert_Reasons(player, Target, Reason.Reason, Y++, Type, IDFake);
        }

        private void DrawUI_IQChat_Mute_Alert_Reasons(BasePlayer player, BasePlayer Target, String Reason, Int32 Y, MuteType Type, UInt64 IDFake = 0)
        {
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_Mute_Alert_DropList_Reason");
            if (Interface == null) return;

            Interface = Interface.Replace("%OFFSET_MIN%", $"-147.5 {85.42 - (Y * 40)}");
            Interface = Interface.Replace("%OFFSET_MAX%", $"147.5 {120.42 - (Y * 40)}");
            Interface = Interface.Replace("%REASON%", Reason);
            Interface = Interface.Replace("%COMMAND_REASON%", $"newui.cmd action.mute.ignore ignore.and.mute.controller {SelectedAction.Mute} confirm.yes {((IQFakeActive && Target == null && IDFake != 0) ? IDFake : Target.userID)} {Type} {Y}");
            CuiHelper.AddUi(player, Interface);
        }
        #endregion

        private void DrawUI_IQChat_Mute_And_Ignore(BasePlayer player, SelectedAction Action)
        {
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_Mute_And_Ignore");
            if (Interface == null) return;

            Interface = Interface.Replace("%TITLE%", Action == SelectedAction.Mute ? GetLang("IQCHAT_TITLE_IGNORE_AND_MUTE_MUTED", player.UserIDString) : GetLang("IQCHAT_TITLE_IGNORE_AND_MUTE_IGNORED", player.UserIDString));
            Interface = Interface.Replace("%ACTION_TYPE%", $"{Action}");

            CuiHelper.DestroyUi(player, "MuteAndIgnoredPanel");
            CuiHelper.AddUi(player, Interface);

            DrawUI_IQChat_Mute_And_Ignore_Player_Panel(player, Action);
        }

        private void DrawUI_IQChat_Mute_And_Ignore_Player_Panel(BasePlayer player, SelectedAction Action, Int32 Page = 0, String SearchName = null)
        {
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_Mute_And_Ignore_Panel_Content");
            if (Interface == null) return;

            CuiHelper.DestroyUi(player, "MuteIgnorePanelContent");
            CuiHelper.AddUi(player, Interface);

            if (IQFakeActive)
            {
                var FakePlayerList = Action == SelectedAction.Mute ? SearchName != null ? PlayerBases.Where(p => p.DisplayName.ToLower().Contains(SearchName.ToLower())).OrderByDescending(p => !IsFake(p.UserID) && UserInformation.ContainsKey(p.UserID) && (UserInformation[p.UserID].MuteInfo.IsMute(MuteType.Chat) || UserInformation[p.UserID].MuteInfo.IsMute(MuteType.Voice))) : PlayerBases.OrderByDescending(p => !IsFake(p.UserID) && UserInformation.ContainsKey(p.UserID) && (UserInformation[p.UserID].MuteInfo.IsMute(MuteType.Chat) || UserInformation[p.UserID].MuteInfo.IsMute(MuteType.Voice))) :
                                                                            SearchName != null ? PlayerBases.Where(p => p.DisplayName.ToLower().Contains(SearchName.ToLower())).OrderByDescending(p => !IsFake(p.UserID) && UserInformation.ContainsKey(p.UserID) && (UserInformation[player.userID].Settings.IgnoreUsers.Contains(p.UserID))) : PlayerBases.OrderByDescending(p => !IsFake(p.UserID) && UserInformation.ContainsKey(p.UserID) && (UserInformation[player.userID].Settings.IgnoreUsers.Contains(p.UserID)));

                DrawUI_IQChat_Mute_And_Ignore_Pages(player, (Boolean)(FakePlayerList.Skip(18 * (Page + 1)).Count() > 0), Action, Page);
                DrawUI_IQChat_Mute_And_Ignore_Player(player, Action, null, FakePlayerList.Skip(18 * Page).Take(18));
            }
            else
            {
                IOrderedEnumerable<BasePlayer> PlayerList = Action == SelectedAction.Mute ? SearchName != null ? BasePlayer.activePlayerList.Where(p => UserInformation.ContainsKey(p.userID) && p.displayName.ToLower().Contains(SearchName.ToLower())).OrderBy(p => UserInformation[p.userID].MuteInfo.IsMute(MuteType.Chat) || UserInformation[p.userID].MuteInfo.IsMute(MuteType.Voice)) : BasePlayer.activePlayerList.Where(p => UserInformation.ContainsKey(p.userID)).OrderBy(p => UserInformation[p.userID].MuteInfo.IsMute(MuteType.Chat) || UserInformation[p.userID].MuteInfo.IsMute(MuteType.Voice)) :
                                                                         SearchName != null ? BasePlayer.activePlayerList.Where(p => UserInformation.ContainsKey(p.userID) && p.displayName.ToLower().Contains(SearchName.ToLower())).OrderBy(p => UserInformation[player.userID].Settings.IgnoreUsers.Contains(p.userID)) : BasePlayer.activePlayerList.Where(p => UserInformation.ContainsKey(p.userID)).OrderBy(p => UserInformation[player.userID].Settings.IgnoreUsers.Contains(p.userID));

                DrawUI_IQChat_Mute_And_Ignore_Pages(player, (Boolean)(PlayerList.Skip(18 * (Page + 1)).Count() > 0), Action, Page);
                DrawUI_IQChat_Mute_And_Ignore_Player(player, Action, PlayerList.Skip(18 * Page).Take(18));
            }
        }
        private void DrawUI_IQChat_Mute_And_Ignore_Pages(BasePlayer player, Boolean IsNextPage, SelectedAction Action, Int32 Page = 0)
        {
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_Mute_And_Ignore_Pages");
            if (Interface == null) return;

            String CommandRight = IsNextPage ? $"newui.cmd action.mute.ignore page.controller {Action} {Page + 1}" : String.Empty;
            String ColorRight = String.IsNullOrEmpty(CommandRight) ? "1 1 1 0.1" : "1 1 1 1";

            String CommandLeft = Page > 0 ? $"newui.cmd action.mute.ignore page.controller {Action} {Page - 1}" : String.Empty;
            String ColorLeft = String.IsNullOrEmpty(CommandLeft) ? "1 1 1 0.1" : "1 1 1 1";

            Interface = Interface.Replace("%COMMAND_LEFT%", CommandLeft);
            Interface = Interface.Replace("%COMMAND_RIGHT%", CommandRight);
            Interface = Interface.Replace("%PAGE%", $"{Page}");
            Interface = Interface.Replace("%COLOR_LEFT%", ColorLeft);
            Interface = Interface.Replace("%COLOR_RIGHT%", ColorRight);

            CuiHelper.DestroyUi(player, "PageCount");
            CuiHelper.DestroyUi(player, "LeftPage");
            CuiHelper.DestroyUi(player, "RightPage");
            CuiHelper.AddUi(player, Interface);
        }
        private void DrawUI_IQChat_Mute_And_Ignore_Player(BasePlayer player, SelectedAction Action, IEnumerable<BasePlayer> PlayerList, IEnumerable<FakePlayer> FakePlayerList = null)
        {
            User MyInfo = UserInformation[player.userID];
            if (MyInfo == null) return;
            Int32 X = 0, Y = 0;
            String ColorGreen = "0.5803922 1 0.5372549 1";
            String ColorRed = "0.8962264 0.2578764 0.3087685 1";
            String Color = String.Empty;

            if (IQFakeActive && FakePlayerList != null)
            {
                foreach (var playerInList in FakePlayerList)
                {
                    String Interface = InterfaceBuilder.GetInterface("UI_Chat_Mute_And_Ignore_Player");
                    if (Interface == null) return;

                    String DisplayName = playerInList.DisplayName;
                    if (GeneralInfo.RenameList.ContainsKey(playerInList.UserID))
                        if (!String.IsNullOrWhiteSpace(GeneralInfo.RenameList[playerInList.UserID].RenameNick))
                            DisplayName = GeneralInfo.RenameList[playerInList.UserID].RenameNick;

                    Interface = Interface.Replace("%OFFSET_MIN%", $"{-385.795 - (-281.17 * X)} {97.54 - (46.185 * Y)}");
                    Interface = Interface.Replace("%OFFSET_MAX%", $"{-186.345 - (-281.17 * X)} {132.03 - (46.185 * Y)}");
                    Interface = Interface.Replace("%DISPLAY_NAME%", $"{DisplayName}");
                    Interface = Interface.Replace("%COMMAND_ACTION%", $"newui.cmd action.mute.ignore ignore.and.mute.controller {Action} confirm.alert {playerInList.UserID}");

                    switch (Action)
                    {
                        case SelectedAction.Mute:
                            if (UserInformation.ContainsKey(playerInList.UserID) && UserInformation[playerInList.UserID] != null && (UserInformation[playerInList.UserID].MuteInfo.IsMute(MuteType.Chat) || UserInformation[playerInList.UserID].MuteInfo.IsMute(MuteType.Voice)))
                                Color = ColorRed;
                            else Color = ColorGreen;
                            break;
                        case SelectedAction.Ignore:
                            if (MyInfo.Settings.IsIgnored(playerInList.UserID))
                                Color = ColorRed;
                            else Color = ColorGreen;
                            break;
                        default:
                            break;
                    }

                    Interface = Interface.Replace("%COLOR%", Color);


                    X++;
                    if (X == 3)
                    {
                        X = 0;
                        Y++;
                    }

                    CuiHelper.AddUi(player, Interface);
                }
            }
            else
            {
                foreach (var playerInList in PlayerList)
                {
                    String Interface = InterfaceBuilder.GetInterface("UI_Chat_Mute_And_Ignore_Player");
                    if (Interface == null) return;
                    User Info = UserInformation[playerInList.userID];
                    if (Info == null) continue;

                    String DisplayName = playerInList.displayName;
                    if (GeneralInfo.RenameList.ContainsKey(playerInList.userID))
                        if (!String.IsNullOrWhiteSpace(GeneralInfo.RenameList[playerInList.userID].RenameNick))
                            DisplayName = GeneralInfo.RenameList[playerInList.userID].RenameNick;

                    Interface = Interface.Replace("%OFFSET_MIN%", $"{-385.795 - (-281.17 * X)} {97.54 - (46.185 * Y)}");
                    Interface = Interface.Replace("%OFFSET_MAX%", $"{-186.345 - (-281.17 * X)} {132.03 - (46.185 * Y)}");
                    Interface = Interface.Replace("%DISPLAY_NAME%", $"{DisplayName}");
                    Interface = Interface.Replace("%COMMAND_ACTION%", $"newui.cmd action.mute.ignore ignore.and.mute.controller {Action} confirm.alert {playerInList.userID}");

                    switch (Action)
                    {
                        case SelectedAction.Mute:
                            if (Info.MuteInfo.IsMute(MuteType.Chat) || Info.MuteInfo.IsMute(MuteType.Voice))
                                Color = ColorRed;
                            else Color = ColorGreen;
                            break;
                        case SelectedAction.Ignore:
                            if (MyInfo.Settings.IsIgnored(playerInList.userID))
                                Color = ColorRed;
                            else Color = ColorGreen;
                            break;
                        default:
                            break;
                    }

                    Interface = Interface.Replace("%COLOR%", Color);


                    X++;
                    if (X == 3)
                    {
                        X = 0;
                        Y++;
                    }

                    CuiHelper.AddUi(player, Interface);
                }
            }
        }
        #endregion

        #region CheckBox Draw UI 
        private void DrawUI_IQChat_Update_Check_Box(BasePlayer player, ElementsSettingsType Type, String OffsetMin, String OffsetMax, Boolean StatusCheckBox)
        {
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_Context_CheckBox");
            User Info = UserInformation[player.userID];
            if (Info == null || Interface == null) return;

            String Name = $"{Type}";
            Interface = Interface.Replace("%NAME_CHECK_BOX%", Name);
            Interface = Interface.Replace("%COLOR%", !StatusCheckBox ? "0.4716981 0.4716981 0.4716981 1" : "0.6040971 0.4198113 1 1");
            Interface = Interface.Replace("%OFFSET_MIN%", OffsetMin);
            Interface = Interface.Replace("%OFFSET_MAX%", OffsetMax);
            Interface = Interface.Replace("%COMMAND_TURNED%", $"newui.cmd checkbox.controller {Type}");

            CuiHelper.DestroyUi(player, Name);
            CuiHelper.AddUi(player, Interface);
        }

        #endregion

        #region Sliders Draw UI
        private void DrawUI_IQChat_Sliders(BasePlayer player, String Name, String OffsetMin, String OffsetMax, TakeElementUser ElementType)
        {
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_Slider");
            if (Interface == null) return;

            Interface = Interface.Replace("%OFFSET_MIN%", OffsetMin);
            Interface = Interface.Replace("%OFFSET_MAX%", OffsetMax);
            Interface = Interface.Replace("%NAME%", Name);
            Interface = Interface.Replace("%COMMAND_LEFT_SLIDE%", $"newui.cmd slider.controller {ElementType} -");
            Interface = Interface.Replace("%COMMAND_RIGHT_SLIDE%", $"newui.cmd slider.controller {ElementType} +");

            CuiHelper.DestroyUi(player, Name);
            CuiHelper.AddUi(player, Interface);

            DrawUI_IQChat_Slider_Update_Argument(player, ElementType);
        }
        private void DrawUI_IQChat_Slider_Update_Argument(BasePlayer player, TakeElementUser ElementType)
        {
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_Slider_Update_Argument");
            User Info = UserInformation[player.userID];
            if (Info == null || Interface == null) return;

            String Argument = String.Empty;
            String Name = String.Empty;
            String Parent = String.Empty;

            switch (ElementType)
            {
                case TakeElementUser.Prefix:
                    Argument = Info.Info.Prefix;
                    Parent = "SLIDER_PREFIX";
                    Name = "ARGUMENT_PREFIX";
                    break;
                case TakeElementUser.Nick:
                    Argument = $"<color={Info.Info.ColorNick}>{player.displayName}</color>";
                    Parent = "SLIDER_NICK_COLOR";
                    Name = "ARGUMENT_NICK_COLOR";
                    break;
                case TakeElementUser.Chat:
                    Argument = $"<color={Info.Info.ColorMessage}>{GetLang("IQCHAT_CONTEXT_NICK_DISPLAY_MESSAGE", player.UserIDString)}</color>";
                    Parent = "SLIDER_MESSAGE_COLOR";
                    Name = "ARGUMENT_MESSAGE_COLOR";
                    break;
                case TakeElementUser.Rank:
                    Argument = IQRankGetNameRankKey(Info.Info.Rank) ?? GetLang("IQCHAT_CONTEXT_SLIDER_IQRANK_TITLE_NULLER", player.UserIDString);
                    Parent = "SLIDER_IQRANK";
                    Name = "ARGUMENT_RANK";
                    break;
                default:
                    break;
            }

            String Pattern = @"</?size.*?>";
            String ArgumentRegex = Regex.IsMatch(Argument, Pattern) ? Regex.Replace(Argument, Pattern, "") : Argument;
            Interface = Interface.Replace("%ARGUMENT%", ArgumentRegex);
            Interface = Interface.Replace("%PARENT%", Parent);
            Interface = Interface.Replace("%NAME%", Name);

            CuiHelper.DestroyUi(player, Name);
            CuiHelper.AddUi(player, Interface);

        }

        #endregion

        #region DropList Draw UI
        private void DrawUI_IQChat_DropList(BasePlayer player, String OffsetMin, String OffsetMax, String Title, TakeElementUser ElementType)
        {
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_DropList");
            if (Interface == null) return;

            Interface = Interface.Replace("%TITLE%", Title);
            Interface = Interface.Replace("%OFFSET_MIN%", OffsetMin);
            Interface = Interface.Replace("%OFFSET_MAX%", OffsetMax);
            Interface = Interface.Replace("%BUTTON_DROP_LIST_CMD%", $"newui.cmd droplist.controller open {ElementType}");

            CuiHelper.AddUi(player, Interface);
        }
        private void DrawUI_IQChat_OpenDropList(BasePlayer player, TakeElementUser ElementType, Int32 Page = 0)
        {
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_OpenDropList");
            if (Interface == null) return;

            if (!LocalBase.ContainsKey(player)) return;

            String Title = String.Empty;
            String Description = String.Empty;
            List<Configuration.ControllerParameters.AdvancedFuncion> InfoUI = new List<Configuration.ControllerParameters.AdvancedFuncion>();

            switch (ElementType)
            {
                case TakeElementUser.MultiPrefix:
                case TakeElementUser.Prefix:
                    {
                        InfoUI = LocalBase[player].ElementsPrefix;
                        Title = GetLang("IQCHAT_CONTEXT_SLIDER_PREFIX_TITLE", player.UserIDString);
                        Description = GetLang("IQCHAT_CONTEXT_DESCRIPTION_PREFIX", player.UserIDString);
                        break;
                    }
                case TakeElementUser.Nick:
                    {
                        InfoUI = LocalBase[player].ElementsNick;
                        Title = GetLang("IQCHAT_CONTEXT_SLIDER_NICK_COLOR_TITLE", player.UserIDString);
                        Description = GetLang("IQCHAT_CONTEXT_DESCRIPTION_NICK", player.UserIDString);
                        break;
                    }
                case TakeElementUser.Chat:
                    {
                        InfoUI = LocalBase[player].ElementsChat;
                        Title = GetLang("IQCHAT_CONTEXT_SLIDER_MESSAGE_COLOR_TITLE", player.UserIDString);
                        Description = GetLang("IQCHAT_CONTEXT_DESCRIPTION_CHAT", player.UserIDString);
                        break;
                    }
                case TakeElementUser.Rank:
                    {
                        InfoUI = LocalBase[player].ElementsRanks;
                        Title = GetLang("IQCHAT_CONTEXT_SLIDER_IQRANK_TITLE", player.UserIDString);
                        Description = GetLang("IQCHAT_CONTEXT_DESCRIPTION_RANK", player.UserIDString);
                        break;
                    }
                default:
                    break;
            }

            //  if (InfoUI == null || InfoUI.Count == 0) return;

            Interface = Interface.Replace("%TITLE%", Title);
            Interface = Interface.Replace("%DESCRIPTION%", Description);

            String CommandRight = InfoUI.Skip(9 * (Page + 1)).Count() > 0 ? $"newui.cmd droplist.controller page.controller {ElementType} + {Page}" : String.Empty;
            String CommandLeft = Page != 0 ? $"newui.cmd droplist.controller page.controller {ElementType} - {Page}" : String.Empty;

            Interface = Interface.Replace("%NEXT_BTN%", CommandRight);
            Interface = Interface.Replace("%BACK_BTN%", CommandLeft);

            Interface = Interface.Replace("%COLOR_RIGHT%", String.IsNullOrWhiteSpace(CommandRight) ? "1 1 1 0.1" : "1 1 1 1");
            Interface = Interface.Replace("%COLOR_LEFT%", String.IsNullOrWhiteSpace(CommandLeft) ? "1 1 1 0.1" : "1 1 1 1");

            CuiHelper.DestroyUi(player, "OpenDropList");
            CuiHelper.AddUi(player, Interface);

            Int32 Count = 0;
            Int32 X = 0, Y = 0;
            foreach (Configuration.ControllerParameters.AdvancedFuncion Info in InfoUI.Skip(9 * Page).Take(9))
            {
                DrawUI_IQChat_OpenDropListArgument(player, ElementType, Info, X, Y, Count);

                if (ElementType == TakeElementUser.MultiPrefix && UserInformation[player.userID].Info.PrefixList.Contains(Info.Argument))
                    DrawUI_IQChat_OpenDropListArgument(player, Count);

                Count++;
                X++;
                if (X == 3)
                {
                    X = 0;
                    Y++;
                }
            }
        }

        private void DrawUI_IQChat_OpenDropListArgument(BasePlayer player, TakeElementUser ElementType, Configuration.ControllerParameters.AdvancedFuncion Info, Int32 X, Int32 Y, Int32 Count)
        {
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_OpenDropListArgument");
            if (Interface == null) return;
            String Argument = ElementType == TakeElementUser.MultiPrefix || ElementType == TakeElementUser.Prefix ? Info.Argument :
                    ElementType == TakeElementUser.Nick ? $"<color={Info.Argument}>{player.displayName}</color>" :
                    ElementType == TakeElementUser.Chat ? $"<color={Info.Argument}>{GetLang("IQCHAT_CONTEXT_NICK_DISPLAY_MESSAGE", player.UserIDString)}</color>" :
                    ElementType == TakeElementUser.Rank ? IQRankGetNameRankKey(Info.Argument) : String.Empty;

            Interface = Interface.Replace("%OFFSET_MIN%", $"{-140.329 - (-103 * X)} {-2.243 + (Y * -28)}");
            Interface = Interface.Replace("%OFFSET_MAX%", $"{-65.271 - (-103 * X)} {22.568 + (Y * -28)}");
            Interface = Interface.Replace("%COUNT%", Count.ToString());
            Interface = Interface.Replace("%ARGUMENT%", Argument);
            Interface = Interface.Replace("%TAKE_COMMAND_ARGUMENT%", $"newui.cmd droplist.controller element.take {ElementType} {Count} {Info.Permissions} {Info.Argument}");

            CuiHelper.DestroyUi(player, $"ArgumentDropList_{Count}");
            CuiHelper.AddUi(player, Interface);
        }
        private void DrawUI_IQChat_OpenDropListArgument(BasePlayer player, Int32 Count)
        {
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_OpenDropListArgument_Taked");
            if (Interface == null) return;

            Interface = Interface.Replace("%COUNT%", Count.ToString());

            CuiHelper.DestroyUi(player, $"TAKED_INFO_{Count}");
            CuiHelper.AddUi(player, Interface);
        }

        private void DrawUI_IQChat_Alert(BasePlayer player, String Description, String Title = null)
        {
            if (_interface == null)
            {
                PrintWarning("Генерируем интерфейс, ожидайте сообщения об успешной генерации");
                return;
            }
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_Alert");
            if (Interface == null) return;

            Interface = Interface.Replace("%TITLE%", Title ?? GetLang("IQCHAT_ALERT_TITLE", player.UserIDString));
            Interface = Interface.Replace("%DESCRIPTION%", Description);

            CuiHelper.DestroyUi(player, InterfaceBuilder.UI_Chat_Alert);
            CuiHelper.AddUi(player, Interface);

            player.Invoke(() =>
            {
                CuiHelper.DestroyUi(player, InterfaceBuilder.UI_Chat_Alert);
            }, config.ControllerMessages.GeneralSetting.OtherSetting.TimeDeleteAlertUI);
        }
        #endregion

        private class InterfaceBuilder
        {
            #region Vars

            public static InterfaceBuilder Instance;
            public const String UI_Chat_Context = "UI_IQCHAT_CONTEXT";
            public const String UI_Chat_Context_Visual_Nick = "UI_IQCHAT_CONTEXT_VISUAL_NICK";
            public const String UI_Chat_Alert = "UI_IQCHAT_ALERT";
            public Dictionary<String, String> Interfaces;

            #endregion

            #region Main

            public InterfaceBuilder()
            {
                Instance = this;
                Interfaces = new Dictionary<String, String>();
                BuildingStaticContext();
                BuildingVisualNick();
                BuildingCheckBox();

                BuildingModerationStatic();
                BuildingMuteAllChat();
                BuildingMuteAllVoice();

                BuildingSlider();
                BuildingSliderUpdateArgument();

                BuildingDropList();
                BuildingOpenDropList();
                BuildingElementDropList();
                BuildingElementDropListTakeLine();

                BuildingAlertUI();

                BuildingMuteAndIgnore();
                BuildingMuteAndIgnorePlayerPanel();
                BuildingMuteAndIgnorePlayer();
                BuildingMuteAndIgnorePages();

                BuildingMuteAndIgnorePanelAlert();
                BuildingIgnoreAlert();
                BuildingMuteAlert();
                BuildingMuteAlert_DropList_Title();
                BuildingMuteAlert_DropList_Reason();
            }

            public static void AddInterface(String name, String json)
            {
                if (Instance.Interfaces.ContainsKey(name))
                {
                    _.PrintError($"Error! Tried to add existing cui elements! -> {name}");
                    return;
                }

                Instance.Interfaces.Add(name, json);
            }

            public static string GetInterface(String name)
            {
                string json = string.Empty;
                if (Instance.Interfaces.TryGetValue(name, out json) == false)
                {
                    _.PrintWarning($"Warning! UI elements not found by name! -> {name}");
                }

                return json;
            }

            public static void DestroyAll()
            {
                for (var i = 0; i < BasePlayer.activePlayerList.Count; i++)
                {
                    var player = BasePlayer.activePlayerList[i];
                    CuiHelper.DestroyUi(player, UI_Chat_Context);
                    CuiHelper.DestroyUi(player, UI_Chat_Context_Visual_Nick);
                    CuiHelper.DestroyUi(player, UI_Chat_Alert);
                    CuiHelper.DestroyUi(player, "MUTE_AND_IGNORE_PANEL_ALERT");
                }
            }

            #endregion

            #region VisualNick Building
            private void BuildingVisualNick()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = UI_Chat_Context_Visual_Nick,
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%NICK_DISPLAY%", Font = "robotocondensed-regular.ttf", FontSize = 7, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-135.769 -89.558", OffsetMax = "-12.644 -77.176" }
                }
                });

                AddInterface("UI_Chat_Context_Visual_Nick", container.ToJson());
            }
            #endregion

            #region Context
            private void BuildingStaticContext()
            {
                Configuration.ControllerParameters Controller = config.ControllerParameter;
                if (Controller == null)
                {
                    _.PrintWarning("Ошибка генерации интерфейса, null значение в конфигурации, свяжитесь с разработчиком");
                    return;
                }
                CuiElementContainer container = new CuiElementContainer();
                container.Add(new CuiPanel
                {
                    CursorEnabled = true,
                    RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-379 -217", OffsetMax = "-31 217" },
                    Image = { Color = "0 0 0 0" }
                }, "Overlay", UI_Chat_Context);

                container.Add(new CuiElement
                {
                    Name = "ImageContext",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = "%IMG_BACKGROUND%" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "TitleLabel",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 17, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-149.429 166.408", OffsetMax = "-14.788 189.564" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "DescriptionLabel",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%SETTING_ELEMENT%", Font = "robotocondensed-regular.ttf", FontSize = 8, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-149.429 112.021", OffsetMax = "152.881 131.787" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "InformationLabel",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%INFORMATION%", Font = "robotocondensed-regular.ttf", FontSize = 8, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-149.429 -53.432", OffsetMax = "-32.905 -39.808" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "InformationIcon",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_INFORMATION_ICON") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-14.788 -52.12", OffsetMax = "-3.788 -41.12" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "SettingLabel",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%SETTINGS%", Font = "robotocondensed-regular.ttf", FontSize = 8, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "11.075 -53.432", OffsetMax = "126.125 -39.808" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "SettingIcon",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_SETTING_ICON") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "141.88 -52.12", OffsetMax = "152.88 -41.12" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "SettingPM",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%SETTINGS_PM%", Font = "robotocondensed-regular.ttf", FontSize = 8, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "11.075 -70.712", OffsetMax = "126.125 -57.088" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "SettingAlertChat",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%SETTINGS_ALERT%", Font = "robotocondensed-regular.ttf", FontSize = 8, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "11.075 -82.412", OffsetMax = "126.125 -68.788" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "SettingNoticyChat",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%SETTINGS_ALERT_PM%", Font = "robotocondensed-regular.ttf", FontSize = 8, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "11.075 -94.412", OffsetMax = "126.125 -80.788" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "SettingSoundAlert",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%SETTINGS_SOUNDS%", Font = "robotocondensed-regular.ttf", FontSize = 8, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "11.075 -106.412", OffsetMax = "126.125 -92.788" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "MuteStatus",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%MUTE_STATUS_PLAYER%", Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-143.174 -131.59", OffsetMax = "-120.611 -114.967" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "MuteStatusTitle",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%MUTE_STATUS_TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 7, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-143.174 -141.429", OffsetMax = "-89.127 -132.508" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "CountIgnored",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%IGNORED_STATUS_COUNT%", Font = "robotocondensed-regular.ttf", FontSize = 7, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-66.98 -131.715", OffsetMax = "-11.09 -116.831" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "IgonoredTitle",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%IGNORED_STATUS_TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 7, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-66.98 -142.04", OffsetMax = "-19.967 -132.537" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "IgnoredIcon",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_IGNORE_INFO_ICON") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-19.483 -115.225", OffsetMax = "-11.762 -107.814" }
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = $"newui.cmd action.mute.ignore open {SelectedAction.Ignore}", Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "IgnoredIcon", "CLOSE_IGNORED");

                container.Add(new CuiElement
                {
                    Name = "TitleNickPanel",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%NICK_DISPLAY_TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-135.769 -78.878", OffsetMax = "-85.632 -64.613" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "NickTitle",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%SLIDER_NICK_COLOR_TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "22.591 76.362", OffsetMax = "80.629 92.278" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "ChatMessageTitle",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%SLIDER_MESSAGE_COLOR_TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-136.591 9.362", OffsetMax = "-78.045 24.278" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "PrefixTitle",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%SLIDER_PREFIX_TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-136.591 77.362", OffsetMax = "-89.949 93.278" }
                }
                });


                container.Add(new CuiElement
                {
                    Name = "RankTitle",
                    Parent = UI_Chat_Context,
                    Components = {
                        new CuiTextComponent { Text = "%SLIDER_IQRANK_TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "22.825 9.242", OffsetMax = "81.375 25.158" }
                    }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "123.62 166", OffsetMax = "153.62 196" },
                    Button = { Close = UI_Chat_Context, Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, UI_Chat_Context, "CLOSE_UI_Chat_Context");

                AddInterface("UI_Chat_Context", container.ToJson());
            }

            #endregion

            #region CheckBox Building
            private void BuildingCheckBox()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = "%NAME_CHECK_BOX%",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiRawImageComponent { Color = "%COLOR%", Png = ImageUi.GetImage("IQCHAT_ELEMENT_SETTING_CHECK_BOX") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "%OFFSET_MIN%", OffsetMax = "%OFFSET_MAX%" }
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = "%COMMAND_TURNED%", Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "%NAME_CHECK_BOX%", "CHECK_BOX_TURNED");

                AddInterface("UI_Chat_Context_CheckBox", container.ToJson());
            }
            #endregion

            #region Slider Building
            private void BuildingSlider()
            {
                CuiElementContainer container = new CuiElementContainer();
                String NameSlider = "%NAME%";

                container.Add(new CuiElement
                {
                    Name = NameSlider,
                    Parent = UI_Chat_Context,
                    Components = {
                            new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_ELEMENT_SLIDER_ICON") },
                            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "%OFFSET_MIN%" , OffsetMax = "%OFFSET_MAX%"  }
                        }
                });

                container.Add(new CuiElement
                {
                    Name = "Left",
                    Parent = NameSlider,
                    Components = {
                        new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_ELEMENT_SLIDER_LEFT_ICON") },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-53.9 -4.5", OffsetMax = "-48.9 4.5" }
                    }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = "%COMMAND_LEFT_SLIDE%", Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "Left", "LEFT_SLIDER_BTN");

                container.Add(new CuiElement
                {
                    Name = "Right",
                    Parent = NameSlider,
                    Components = {
                        new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_ELEMENT_SLIDER_RIGHT_ICON") },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "48.92 -4.5", OffsetMax = "53.92 4.5" }
                    }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = "%COMMAND_RIGHT_SLIDE%", Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "Right", "RIGHT_SLIDER_BTN");

                AddInterface("UI_Chat_Slider", container.ToJson());
            }
            private void BuildingSliderUpdateArgument()
            {
                CuiElementContainer container = new CuiElementContainer();
                String ParentSlider = "%PARENT%";
                String NameArgument = "%NAME%";

                container.Add(new CuiElement
                {
                    Name = NameArgument,
                    Parent = ParentSlider,
                    Components = {
                    new CuiTextComponent { Text = "%ARGUMENT%", Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-41.929 -6.801", OffsetMax = "41.929 6.801" }
                }
                });

                AddInterface("UI_Chat_Slider_Update_Argument", container.ToJson());
            }
            #endregion

            #region MuteAndIgnore Bulding

            #region Menu
            private void BuildingMuteAndIgnore()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = "MuteAndIgnoredPanel",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_MUTE_AND_IGNORE_PANEL")},
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-1007.864 -220.114", OffsetMax = "-167.374 219.063" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "TitlesPanel",
                    Parent = "MuteAndIgnoredPanel",
                    Components = {
                    new CuiTextComponent { Text = "%TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.MiddleRight, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "60.217 164.031", OffsetMax = "356.114 190.962" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "IconPanel",
                    Parent = "MuteAndIgnoredPanel",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_MUTE_AND_IGNORE_ICON")},
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "357.5 170", OffsetMax = "373.5 185"  }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "SearchPanel",
                    Parent = "MuteAndIgnoredPanel",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_MUTE_AND_IGNORE_SEARCH")},
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-385.8 161.244", OffsetMax = "-186.349 192.58" }
                }
                });

                string SearchName = "";

                container.Add(new CuiElement
                {
                    Parent = "SearchPanel",
                    Name = "SearchPanel" + ".Input.Current",
                    Components =
                {
                    new CuiInputFieldComponent { Text = SearchName, FontSize = 14,Command = $"newui.cmd action.mute.ignore search.controller %ACTION_TYPE% {SearchName}", Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.5", CharsLimit = 15},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "PanelPages",
                    Parent = "MuteAndIgnoredPanel",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_MUTE_AND_IGNORE_PAGE_PANEL")},
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-179.196 161.242", OffsetMax = "-121.119 192.578" }
                }
                });

                AddInterface("UI_Chat_Mute_And_Ignore", container.ToJson());
            }

            private void BuildingMuteAndIgnorePlayerPanel()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiPanel
                {
                    CursorEnabled = true,
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.85" },
                    Image = { Color = "0 0 0 0" }
                }, "MuteAndIgnoredPanel", "MuteIgnorePanelContent");

                AddInterface("UI_Chat_Mute_And_Ignore_Panel_Content", container.ToJson());
            }
            private void BuildingMuteAndIgnorePlayer()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = "PANEL_PLAYER",
                    Parent = "MuteIgnorePanelContent",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_MUTE_AND_IGNORE_PLAYER") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "%OFFSET_MIN%", OffsetMax = "%OFFSET_MAX%" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "NickName",
                    Parent = "PANEL_PLAYER",
                    Components = {
                    new CuiTextComponent { Text = "%DISPLAY_NAME%", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-77.391 -17.245", OffsetMax = "91.582 17.244" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "StatusPanel",
                    Parent = "PANEL_PLAYER",
                    Components = {
                    new CuiRawImageComponent { Color = "%COLOR%", Png = ImageUi.GetImage("IQCHAT_MUTE_AND_IGNORE_PLAYER_STATUS") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-92.231 -11.655", OffsetMax = "-87.503 10.44" }
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = "%COMMAND_ACTION%", Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "PANEL_PLAYER");

                AddInterface("UI_Chat_Mute_And_Ignore_Player", container.ToJson());
            }
            private void BuildingMuteAndIgnorePages()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = "PageCount",
                    Parent = "PanelPages",
                    Components = {
                    new CuiTextComponent { Text = "%PAGE%", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-11.03 -15.668", OffsetMax = "11.03 15.668" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "LeftPage",
                    Parent = "PanelPages",
                    Components = {
                    new CuiRawImageComponent { Color = "%COLOR_LEFT%", Png = ImageUi.GetImage("IQCHAT_ELEMENT_SLIDER_LEFT_ICON") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-18 -7", OffsetMax = "-13 6" }
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = "%COMMAND_LEFT%", Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "LeftPage");

                container.Add(new CuiElement
                {
                    Name = "RightPage",
                    Parent = "PanelPages",
                    Components = {
                    new CuiRawImageComponent { Color = "%COLOR_RIGHT%", Png = ImageUi.GetImage("IQCHAT_ELEMENT_SLIDER_RIGHT_ICON") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "14 -7", OffsetMax = "19 6" }
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = "%COMMAND_RIGHT%", Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "RightPage");

                AddInterface("UI_Chat_Mute_And_Ignore_Pages", container.ToJson());
            }

            #endregion

            #region Alert Ignore And Mute
            private void BuildingMuteAndIgnorePanelAlert()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiPanel
                {
                    CursorEnabled = true,
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Image = { Color = "0 0 0 0.25", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
                }, "Overlay", "MUTE_AND_IGNORE_PANEL_ALERT");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Close = "MUTE_AND_IGNORE_PANEL_ALERT", Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "MUTE_AND_IGNORE_PANEL_ALERT");

                AddInterface("UI_Chat_Mute_And_Ignore_Alert_Panel", container.ToJson());
            }

            #region Mute

            private void BuildingMuteAlert()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = "AlertMute",
                    Parent = "MUTE_AND_IGNORE_PANEL_ALERT",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_MUTE_ALERT_PANEL") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-199.832 -274.669", OffsetMax = "199.832 274.669" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "AlertMuteIcon",
                    Parent = "AlertMute",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_MUTE_ALERT_ICON") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-67 204.8", OffsetMax = "67 339.8" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "AlertMuteTitles",
                    Parent = "AlertMute",
                    Components = {
                    new CuiTextComponent { Text = "%TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-199.828 142.57", OffsetMax = "199.832 179.43" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "AlertMuteTakeChat",
                    Parent = "AlertMute",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1",Png = ImageUi.GetImage("IQCHAT_IGNORE_ALERT_BUTTON_YES") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-99.998 87.944", OffsetMax = "100.002 117.944" }
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = "%COMMAND_TAKE_ACTION_MUTE_CHAT%", Color = "0 0 0 0" },
                    Text = { Text = "%BUTTON_TAKE_CHAT_ACTION%", Align = TextAnchor.MiddleCenter, FontSize = 18, Color = "0.1294118 0.145098 0.1647059 1" }
                }, "AlertMuteTakeChat", "BUTTON_TAKE_CHAT");

                container.Add(new CuiElement
                {
                    Name = "AlertMuteTakeVoice",
                    Parent = "AlertMute",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1",Png = ImageUi.GetImage("IQCHAT_IGNORE_ALERT_BUTTON_YES") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-100 49.70440", OffsetMax = "100 79.70440" }
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = "%COMMAND_TAKE_ACTION_MUTE_VOICE%", Color = "0 0 0 0" },
                    Text = { Text = "%BUTTON_TAKE_VOICE_ACTION%", Align = TextAnchor.MiddleCenter, FontSize = 18, Color = "0.1294118 0.145098 0.1647059 1" }
                }, "AlertMuteTakeVoice", "BUTTON_TAKE_VOICE");

                AddInterface("UI_Chat_Mute_Alert", container.ToJson());
            }
            private void BuildingMuteAlert_DropList_Title()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = "AlertMuteTitleReason",
                    Parent = "AlertMute",
                    Components = {
                    new CuiTextComponent { Text = "%TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 22, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-199.828 -9.430440", OffsetMax = "199.832 27.430440" }
                }
                });

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "1 1 1 0" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-147.497 -265.5440440", OffsetMax = "147.503 -24.70440" }
                }, "AlertMute", "PanelMuteReason");

                AddInterface("UI_Chat_Mute_Alert_DropList_Title", container.ToJson());
            }

            private void BuildingMuteAlert_DropList_Reason()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = "Reason",
                    Parent = "PanelMuteReason",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_MUTE_ALERT_PANEL_REASON")},
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "%OFFSET_MIN%", OffsetMax = "%OFFSET_MAX%" }
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = "%COMMAND_REASON%", Color = "0 0 0 0" },
                    Text = { Text = "%REASON%", Align = TextAnchor.MiddleCenter, FontSize = 13, Color = "1 1 1 1" }
                }, "Reason");

                AddInterface("UI_Chat_Mute_Alert_DropList_Reason", container.ToJson());
            }
            #endregion

            #region Ignore
            private void BuildingIgnoreAlert()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = "AlertIgnore",
                    Parent = "MUTE_AND_IGNORE_PANEL_ALERT",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_IGNORE_ALERT_PANEL") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-236.5 -134", OffsetMax = "236.5 134" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "AlertIgnoreIcon",
                    Parent = "AlertIgnore",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_IGNORE_ALERT_ICON") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-66.5 64.8", OffsetMax = "66.5 198.8" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "AlertIgnoreTitle",
                    Parent = "AlertIgnore",
                    Components = {
                    new CuiTextComponent { Text = "%TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 22, Align = TextAnchor.UpperCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-231 -55.006", OffsetMax = "229.421 33.981" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "AlertIgnoreYes",
                    Parent = "AlertIgnore",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_IGNORE_ALERT_BUTTON_YES") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-178 -115", OffsetMax = "-22 -77" }
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Close = "MUTE_AND_IGNORE_PANEL_ALERT", Command = "%COMMAND%", Color = "0 0 0 0" },
                    Text = { Text = "%BUTTON_YES%", Align = TextAnchor.MiddleCenter, FontSize = 18, Color = "0.1294118 0.145098 0.1647059 1" }
                }, "AlertIgnoreYes", "BUTTON_YES");

                container.Add(new CuiElement
                {
                    Name = "AlertIgnoreNo",
                    Parent = "AlertIgnore",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_IGNORE_ALERT_BUTTON_NO") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "22 -115", OffsetMax = "178 -77" }
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Close = "MUTE_AND_IGNORE_PANEL_ALERT", Color = "0 0 0 0" },
                    Text = { Text = "%BUTTON_NO%", Align = TextAnchor.MiddleCenter, FontSize = 18 }
                }, "AlertIgnoreNo", "BUTTON_NO");

                AddInterface("UI_Chat_Ignore_Alert", container.ToJson());
            }
            #endregion

            #endregion

            #endregion

            #region DropList Building

            private void BuildingDropList()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = "DropListIcon",
                    Parent = UI_Chat_Context,
                    Components = {
                            new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_ELEMENT_PREFIX_MULTI_TAKE_ICON")},
                      new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "%OFFSET_MIN%", OffsetMax = "%OFFSET_MAX%" }
                        }
                });

                container.Add(new CuiElement
                {
                    Name = "DropListDescription",
                    Parent = "DropListIcon",
                    Components = {
                            new CuiTextComponent { Text = "%TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 8, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-105.5 -13.948", OffsetMax = "-42.615 1.725" }
                        }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = "%BUTTON_DROP_LIST_CMD%", Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "DropListIcon", "DropListIcon_Button");

                AddInterface("UI_Chat_DropList", container.ToJson());
            }

            private void BuildingOpenDropList()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = "OpenDropList",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_ELEMENT_DROP_LIST_OPEN_ICON")},
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-149.429 -17.38", OffsetMax = "155.093 109.1" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "DropListName",
                    Parent = "OpenDropList",
                    Components = {
                    new CuiTextComponent { Text = "%TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-140.329 44.5", OffsetMax = "-40.329 58.312" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "DropListDescription",
                    Parent = "OpenDropList",
                    Components = {
                    new CuiTextComponent { Text = "%DESCRIPTION%", Font = "robotocondensed-regular.ttf", FontSize = 8, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-140.329 32.993", OffsetMax = "-40.329 42.77" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "DropListClose",
                    Parent = "OpenDropList",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_ELEMENT_PREFIX_MULTI_TAKE_ICON") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "113 32.2", OffsetMax = "145 56.2" }
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Close = "OpenDropList", Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "DropListClose", "DropListClose_Button");

                container.Add(new CuiElement
                {
                    Name = "DropListPageRight",
                    Parent = "OpenDropList",
                    Components = {
                    new CuiRawImageComponent { Color = "%COLOR_RIGHT%", Png = ImageUi.GetImage("IQCHAT_ELEMENT_SLIDER_RIGHT_ICON") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "100 38", OffsetMax = "105.2 48" }
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = "%NEXT_BTN%", Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "DropListPageRight", "DropListPageRight_Button");

                container.Add(new CuiElement
                {
                    Name = "DropListPageLeft",
                    Parent = "OpenDropList",
                    Components = {
                    new CuiRawImageComponent { Color ="%COLOR_LEFT%", Png = ImageUi.GetImage("IQCHAT_ELEMENT_SLIDER_LEFT_ICON") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "86 38", OffsetMax = "91.2 48" }
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = "%BACK_BTN%", Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "DropListPageLeft", "DropListPageLeft_Button");

                AddInterface("UI_Chat_OpenDropList", container.ToJson());
            }

            private void BuildingElementDropList()
            {
                CuiElementContainer container = new CuiElementContainer();
                String Name = "ArgumentDropList_%COUNT%";

                container.Add(new CuiElement
                {
                    Name = Name,
                    Parent = "OpenDropList",
                    Components = {
                    new CuiRawImageComponent { FadeIn = 0.3f, Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_ELEMENT_DROP_LIST_OPEN_ARGUMENT_ICON") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "%OFFSET_MIN%", OffsetMax = "%OFFSET_MAX%" }
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-37.529 -12.843", OffsetMax = "37.528 12.842" },
                    Button = { FadeIn = 0.3f, Command = "%TAKE_COMMAND_ARGUMENT%", Color = "0 0 0 0" },
                    Text = { FadeIn = 0.3f, Text = "%ARGUMENT%", Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, Name, "ArgumentButton");

                AddInterface("UI_Chat_OpenDropListArgument", container.ToJson());
            }

            private void BuildingElementDropListTakeLine()
            {
                CuiElementContainer container = new CuiElementContainer();
                String Parent = "ArgumentDropList_%COUNT%";

                container.Add(new CuiElement
                {
                    Name = "TAKED_INFO_%COUNT%",
                    Parent = Parent,
                    Components = {
                    new CuiRawImageComponent { Color = "0.3098039 0.2745098 0.572549 1", Png = ImageUi.GetImage("IQCHAT_ELEMENT_DROP_LIST_OPEN_TAKED") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-25.404 -17.357", OffsetMax = "25.403 -1.584" }
                }
                });

                AddInterface("UI_Chat_OpenDropListArgument_Taked", container.ToJson());
            }


            #region ModerationStatic
            private void BuildingModerationStatic()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = "ModerationLabel",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 8, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "11.075 -126.612", OffsetMax = "126.125 -112.988" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "ModerationIcon",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_MODERATION_ICON") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "141.88 -125.3", OffsetMax = "152.88 -114.3" }
                }
                });


                container.Add(new CuiElement
                {
                    Name = "ModeratorMuteMenu",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_ELEMENT_PANEL_ICON")},
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "11.071 -144.188", OffsetMax = "152.881 -129.752" }
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.95" },
                    Button = { Command = "%COMMAND_MUTE_MENU%", Color = "0 0 0 0" },
                    Text = { Text = "%TEXT_MUTE_MENU%", FontSize = 9, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf" }
                }, "ModeratorMuteMenu", "ModeratorMuteMenu_Btn");


                AddInterface("UI_Chat_Moderation", container.ToJson());
            }
            private void BuildingMuteAllChat()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = "ModeratorMuteAllChat",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_ELEMENT_PANEL_ICON") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "11.07 -161.818", OffsetMax = "152.88 -147.382" }
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.95" },
                    Button = { Command = "%COMMAND_MUTE_ALLCHAT%", Color = "0 0 0 0" },
                    Text = { Text = "%TEXT_MUTE_ALLCHAT%", FontSize = 9, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf" }
                }, "ModeratorMuteAllChat", "ModeratorMuteAllChat_Btn");

                AddInterface("UI_Chat_Administation_AllChat", container.ToJson());
            }
            private void BuildingMuteAllVoice()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = "ModeratorMuteAllVoice",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_ELEMENT_PANEL_ICON") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "11.075 -179.448", OffsetMax = "152.885 -165.012" }
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.95" },
                    Button = { Command = "%COMMAND_MUTE_ALLVOICE%", Color = "0 0 0 0" },
                    Text = { Text = "%TEXT_MUTE_ALLVOICE%", FontSize = 9, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf" }
                }, "ModeratorMuteAllVoice", "ModeratorMuteAllVoice_Btn");

                AddInterface("UI_Chat_Administation_AllVoce", container.ToJson());
            }

            #endregion


            #region DynamicAlert
            private void BuildingAlertUI()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = UI_Chat_Alert,
                    Parent = "Overlay",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_ALERT_PANEL") },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 -136.5", OffsetMax = "434 -51.5" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "AlertTitle",
                    Parent = UI_Chat_Alert,
                    Components = {
                    new CuiTextComponent { Text = "<b>%TITLE%</b>", Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-184.193 9.119", OffsetMax = "189.223 30.925" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "AlertText",
                    Parent = UI_Chat_Alert,
                    Components = {
                    new CuiTextComponent { Text = "%DESCRIPTION%", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-184.193 -27.133", OffsetMax = "189.223 9.119" }
                }
                });

                AddInterface("UI_Chat_Alert", container.ToJson());
            }
            #endregion
            #endregion
        }

        #endregion

        #region Command

        #region Funcion Command
        [ConsoleCommand("newui.cmd")] 
        private void ConsoleCommandFuncional(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            String Action = arg.Args[0];
            if (Action == null || String.IsNullOrWhiteSpace(Action)) return;

            if (!LocalBase.ContainsKey(player))
            {
                PrintError("UI не смог обработать локальную базу (LocalBase) свяжитесь с разработчиком");
                return;
            }
            Configuration.ControllerParameters ControllerParameters = config.ControllerParameter;
            if (ControllerParameters == null)
            {
                PrintError("В конфигурации допущена ошибка! ControllerParameters является null, свяжитесь с разработчиком");
                return;
            }

            switch (Action)
            {
                case "action.mute.ignore":
                    {
                        String ActionMenu = arg.Args[1];
                        SelectedAction ActionType = (SelectedAction)Enum.Parse(typeof(SelectedAction), arg.Args[2]);
                        if(ActionMenu == "search.controller" && arg.Args.Length < 4)
                            return;

                        switch (ActionMenu)
                        {
                            case "mute.controller":
                                {
                                    if (!player.IsAdmin)
                                        if (!permission.UserHasPermission(player.UserIDString, PermissionMute)) return;

                                    String ActionMute = arg.Args[3];
                                    switch (ActionMute)
                                    {
                                        case "mute.all.chat": 
                                            {
                                                if (GeneralInfo.TurnMuteAllChat)
                                                {
                                                    GeneralInfo.TurnMuteAllChat = false;
                                                    ReplyBroadcast(GetLang("IQCHAT_FUNCED_NO_SEND_CHAT_UNMUTED_ALL_CHAT", player.UserIDString), AdminAlert: true);
                                                }
                                                else
                                                {
                                                    GeneralInfo.TurnMuteAllChat = true;
                                                    ReplyBroadcast(GetLang("IQCHAT_FUNCED_NO_SEND_CHAT_MUTED_ALL_CHAT", player.UserIDString), AdminAlert: true);
                                                }

                                                DrawUI_IQChat_Update_MuteChat_All(player);
                                                break;
                                            }
                                        case "mute.all.voice":
                                            {
                                                if (GeneralInfo.TurnMuteAllVoice)
                                                {
                                                    GeneralInfo.TurnMuteAllVoice = false;
                                                    ReplyBroadcast(GetLang("IQCHAT_FUNCED_NO_SEND_CHAT_UMMUTED_ALL_VOICE", player.UserIDString), AdminAlert: true);
                                                }
                                                else
                                                {
                                                    GeneralInfo.TurnMuteAllVoice = true;
                                                    ReplyBroadcast(GetLang("IQCHAT_FUNCED_NO_SEND_CHAT_MUTED_ALL_VOICE", player.UserIDString), AdminAlert: true);
                                                }
                                                DrawUI_IQChat_Update_MuteVoice_All(player);
                                                break;
                                            }
                                        default:
                                            break;
                                    }
                                    break;
                                }
                            case "ignore.and.mute.controller":
                                {
                                    String ActionController = arg.Args[3];
                                    BasePlayer TargetPlayer = BasePlayer.Find(arg.Args[4]);
                                    UInt64 ID = 0;
                                    UInt64.TryParse(arg.Args[4], out ID);

                                    if (TargetPlayer == null && !IsFake(ID))
                                    {
                                        CuiHelper.DestroyUi(player, "MUTE_AND_IGNORE_PANEL_ALERT");
                                        return;
                                    }

                                    switch (ActionController)
                                    {
                                        case "confirm.alert":
                                            {
                                                if (ActionType == SelectedAction.Ignore)
                                                    DrawUI_IQChat_Ignore_Alert(player, TargetPlayer, ID);
                                                else DrawUI_IQChat_Mute_Alert(player, TargetPlayer, ID);
                                                break;
                                            }
                                        case "open.reason.mute": 
                                            {
                                                MuteType Type = (MuteType)Enum.Parse(typeof(MuteType), arg.Args[5]);
                                                DrawUI_IQChat_Mute_Alert_Reasons(player, TargetPlayer, Type, IDFake: ID);
                                                break;
                                            }
                                        case "confirm.yes":
                                            {
                                                if (ActionType == SelectedAction.Ignore)
                                                {
                                                    User Info = UserInformation[player.userID];
                                                    Info.Settings.IgnoredAddOrRemove(IsFake(ID) ? ID : TargetPlayer.userID);

                                                    CuiHelper.DestroyUi(player, "MUTE_AND_IGNORE_PANEL_ALERT");
                                                    DrawUI_IQChat_Mute_And_Ignore_Player_Panel(player, ActionType);
                                                }
                                                else
                                                {
                                                    MuteType Type = (MuteType)Enum.Parse(typeof(MuteType), arg.Args[5]);
                                                    Int32 IndexReason = Int32.Parse(arg.Args[6]);

                                                    MutePlayer(TargetPlayer, Type, IndexReason, player, IDFake: ID);

                                                    CuiHelper.DestroyUi(player, "MUTE_AND_IGNORE_PANEL_ALERT");
                                                    DrawUI_IQChat_Mute_And_Ignore_Player_Panel(player, ActionType);
                                                }
                                                break;
                                            }
                                        case "unmute.yes": 
                                            {
                                                MuteType Type = (MuteType)Enum.Parse(typeof(MuteType), arg.Args[5]);

                                                UnmutePlayer(TargetPlayer, Type, player);

                                                CuiHelper.DestroyUi(player, "MUTE_AND_IGNORE_PANEL_ALERT");
                                                DrawUI_IQChat_Mute_And_Ignore_Player_Panel(player, ActionType);
                                                break;
                                            }
                                    }
                                    break;
                                }
                            case "open":
                                {
                                    DrawUI_IQChat_Mute_And_Ignore(player, ActionType);
                                    break;
                                }
                            case "page.controller":    
                                {
                                    Int32 Page = Int32.Parse(arg.Args[3]);

                                    DrawUI_IQChat_Mute_And_Ignore_Player_Panel(player, ActionType, Page);
                                    break;
                                }
                            case "search.controller":
                                {
                                    String SearchName = arg.Args[3];
                                    DrawUI_IQChat_Mute_And_Ignore_Player_Panel(player, ActionType, SearchName: SearchName);
                                    break;
                                }
                            default:
                                break;
                        }

                        break;
                    }
                case "checkbox.controller":
                    {
                        ElementsSettingsType Type = (ElementsSettingsType)Enum.Parse(typeof(ElementsSettingsType), arg.Args[1]);
                        if (!UserInformation.ContainsKey(player.userID)) return;
                        User Info = UserInformation[player.userID];
                        if (Info == null) return;

                        switch (Type)
                        {
                            case ElementsSettingsType.PM:
                                {
                                    if (Info.Settings.TurnPM)
                                        Info.Settings.TurnPM = false;
                                    else Info.Settings.TurnPM = true;

                                    DrawUI_IQChat_Update_Check_Box(player, Type, "143.38 -67.9", "151.38 -59.9", Info.Settings.TurnPM);
                                    break;
                                }
                            case ElementsSettingsType.Broadcast:
                                {
                                    if (Info.Settings.TurnBroadcast)
                                        Info.Settings.TurnBroadcast = false;
                                    else Info.Settings.TurnBroadcast = true;

                                    DrawUI_IQChat_Update_Check_Box(player, Type, "143.38 -79.6", "151.38 -71.6", Info.Settings.TurnBroadcast);
                                    break;
                                }
                            case ElementsSettingsType.Alert:
                                {
                                    if (Info.Settings.TurnAlert)
                                        Info.Settings.TurnAlert = false;
                                    else Info.Settings.TurnAlert = true;

                                    DrawUI_IQChat_Update_Check_Box(player, Type, "143.38 -91.6", "151.38 -83.6", Info.Settings.TurnAlert);
                                    break;
                                }
                            case ElementsSettingsType.Sound:
                                {
                                    if (Info.Settings.TurnSound)
                                        Info.Settings.TurnSound = false;
                                    else Info.Settings.TurnSound = true;

                                    DrawUI_IQChat_Update_Check_Box(player, Type, "143.38 -103.6", "151.38 -95.6", Info.Settings.TurnSound);
                                    break;
                                }
                            default:
                                break;
                        }
                        break;
                    }
                case "droplist.controller":
                    {
                        String ActionDropList = arg.Args[1];
                        TakeElementUser Element = (TakeElementUser)Enum.Parse(typeof(TakeElementUser), arg.Args[2]);

                        switch (ActionDropList)
                        {
                            case "open":
                                {
                                    DrawUI_IQChat_OpenDropList(player, Element);
                                    break;
                                }
                            case "page.controller":
                                {
                                    String ActionDropListPage = arg.Args[3];
                                    Int32 Page = (Int32)Int32.Parse(arg.Args[4]);
                                    Page = ActionDropListPage == "+" ? Page + 1 : Page - 1;

                                    DrawUI_IQChat_OpenDropList(player, Element, Page);
                                    break;
                                }
                            case "element.take":
                                {
                                    Int32 Count = Int32.Parse(arg.Args[3]);
                                    String Permissions = arg.Args[4];
                                    String Argument = String.Join(" ", arg.Args.Skip(5));
                                    if (!permission.UserHasPermission(player.UserIDString, Permissions)) return;
                                    if (!UserInformation.ContainsKey(player.userID)) return;
                                    User User = UserInformation[player.userID];
                                    if (User == null) return;

                                    switch (Element)
                                    {
                                        case TakeElementUser.MultiPrefix:
                                            {
                                                if (!User.Info.PrefixList.Contains(Argument))
                                                {
                                                    User.Info.PrefixList.Add(Argument);
                                                    DrawUI_IQChat_OpenDropListArgument(player, Count);
                                                }
                                                else
                                                {
                                                    User.Info.PrefixList.Remove(Argument);
                                                    CuiHelper.DestroyUi(player, $"TAKED_INFO_{Count}");
                                                }
                                                break;
                                            }
                                        case TakeElementUser.Prefix:
                                            User.Info.Prefix = Argument;
                                            break;
                                        case TakeElementUser.Nick:
                                            User.Info.ColorNick = Argument;
                                            break;
                                        case TakeElementUser.Chat:
                                            User.Info.ColorMessage = Argument;
                                            break;
                                        case TakeElementUser.Rank:
                                            {
                                                User.Info.Rank = Argument;
                                                IQRankSetRank(player.userID, Argument);
                                            }
                                            break;
                                        default:
                                            break;
                                    }
                                    DrawUI_IQChat_Update_DisplayName(player);
                                    break;
                                }
                        }
                        break;
                    }
                case "slider.controller": // newui.cmd slider.controller 0 +
                    {
                        TakeElementUser Element = (TakeElementUser)Enum.Parse(typeof(TakeElementUser), arg.Args[1]);
                        List<Configuration.ControllerParameters.AdvancedFuncion> SliderElements = new List<Configuration.ControllerParameters.AdvancedFuncion>();
                        User Info = UserInformation[player.userID];
                        if (Info == null) return;

                        InformationOpenedUI InfoUI = LocalBase[player];
                        if (InfoUI == null) return;


                        String ActionSlide = arg.Args[2];

                        switch (Element)
                        {
                            case TakeElementUser.Prefix:
                                {
                                    SliderElements = LocalBase[player].ElementsPrefix;

                                    if (SliderElements == null || SliderElements.Count == 0) return;

                                    if (ActionSlide == "+")
                                    {
                                        InfoUI.SlideIndexPrefix++;

                                        if (InfoUI.SlideIndexPrefix >= SliderElements.Count)
                                            InfoUI.SlideIndexPrefix = 0;
                                    }
                                    else
                                    {
                                        InfoUI.SlideIndexPrefix--;

                                        if (InfoUI.SlideIndexPrefix < 0)
                                            InfoUI.SlideIndexPrefix = SliderElements.Count - 1;
                                    }

                                    Info.Info.Prefix = SliderElements[InfoUI.SlideIndexPrefix].Argument;
                                }
                                break;
                            case TakeElementUser.Nick:
                                {
                                    SliderElements = LocalBase[player].ElementsNick;

                                    if (SliderElements == null || SliderElements.Count == 0) return;

                                    if (ActionSlide == "+")
                                    {
                                        InfoUI.SlideIndexNick++;

                                        if (InfoUI.SlideIndexNick >= SliderElements.Count)
                                            InfoUI.SlideIndexNick = 0;
                                    }
                                    else
                                    {
                                        InfoUI.SlideIndexNick--;

                                        if (InfoUI.SlideIndexNick < 0)
                                            InfoUI.SlideIndexNick = SliderElements.Count - 1;
                                    }
                                    Info.Info.ColorNick = SliderElements[InfoUI.SlideIndexNick].Argument;
                                }
                                break;
                            case TakeElementUser.Chat:
                                {
                                    SliderElements = LocalBase[player].ElementsChat;
                                    if (SliderElements == null || SliderElements.Count == 0) return;

                                    if (ActionSlide == "+")
                                    {
                                        InfoUI.SlideIndexChat++;

                                        if (InfoUI.SlideIndexChat >= SliderElements.Count)
                                            InfoUI.SlideIndexChat = 0;
                                    }
                                    else
                                    {
                                        InfoUI.SlideIndexChat--;

                                        if (InfoUI.SlideIndexChat < 0)
                                            InfoUI.SlideIndexChat = SliderElements.Count - 1;
                                    }
                                    Info.Info.ColorMessage = SliderElements[InfoUI.SlideIndexChat].Argument;
                                }
                                break;
                            case TakeElementUser.Rank:
                                {
                                    SliderElements = LocalBase[player].ElementsRanks;
                                    if (SliderElements == null || SliderElements.Count == 0) return;

                                    if (ActionSlide == "+")
                                    {
                                        InfoUI.SlideIndexRank++;

                                        if (InfoUI.SlideIndexRank >= SliderElements.Count)
                                            InfoUI.SlideIndexRank = 0;
                                    }
                                    else
                                    {
                                        InfoUI.SlideIndexRank--;

                                        if (InfoUI.SlideIndexRank < 0)
                                            InfoUI.SlideIndexRank = SliderElements.Count - 1;
                                    }
                                    Info.Info.Rank = SliderElements[InfoUI.SlideIndexRank].Argument;
                                    IQRankSetRank(player.userID,SliderElements[InfoUI.SlideIndexRank].Argument);
                                }
                                break;
                            default:
                                break;
                        }
                        DrawUI_IQChat_Slider_Update_Argument(player, Element);
                        DrawUI_IQChat_Update_DisplayName(player);
                        break;
                    }
                default:
                    break;
            }
        }
        #endregion

        #region Using Command

        [ChatCommand("chat")]
        private void ChatCommandOpenedUI(BasePlayer player)
        {
            if(_interface == null)
            {
                PrintWarning("Генерируем интерфейс, ожидайте сообщения об успешной генерации");
                return;
            }
            if (player == null) return;

            User Info = UserInformation[player.userID];
            Configuration.ControllerParameters ControllerParameters = config.ControllerParameter;

            if (!LocalBase.ContainsKey(player))
                LocalBase.Add(player, new InformationOpenedUI { });

            LocalBase[player].ElementsPrefix = ControllerParameters.Prefixes.Prefixes.OrderByDescending(arg => arg.Argument.Length).Where(p => permission.UserHasPermission(player.UserIDString, p.Permissions)).ToList();
            LocalBase[player].ElementsNick = ControllerParameters.NickColorList.Where(n => permission.UserHasPermission(player.UserIDString, n.Permissions)).ToList();
            LocalBase[player].ElementsChat = ControllerParameters.MessageColorList.Where(m => permission.UserHasPermission(player.UserIDString, m.Permissions)).ToList();

            if (IQRankSystem && config.ReferenceSetting.IQRankSystems.UseRankSystem)
            {
                List<Configuration.ControllerParameters.AdvancedFuncion> RankList = new List<Configuration.ControllerParameters.AdvancedFuncion>();
                foreach(String Rank in IQRankListKey(player.userID))
                    RankList.Add(new Configuration.ControllerParameters.AdvancedFuncion { Argument = Rank, Permissions = String.Empty });

                LocalBase[player].ElementsRanks = RankList;
            }

            DrawUI_IQChat_Context(player);
        }

        #region Hide Mute

        [ConsoleCommand("hmute")]
        void HideMuteConsole(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null)
                if (!permission.UserHasPermission(arg.Player().UserIDString, PermissionMute)) return;

            if (arg == null || arg.Args == null || arg.Args.Length != 3 || arg.Args.Length > 3)
            {
                if (arg.Player() != null)
                    arg.Player().ConsoleMessage("Неверный синтаксис,используйте : hmute Steam64ID Причина Время(секунды)");
                else PrintWarning("Неверный синтаксис,используйте : hmute Steam64ID Причина Время(секунды)");
                return;
            }
            string NameOrID = arg.Args[0];
            string Reason = arg.Args[1];
            Int32 TimeMute = 0;
            if (!Int32.TryParse(arg.Args[2], out TimeMute))
            {
                if (arg.Player() != null)
                    arg.Player().ConsoleMessage("Введите время цифрами!");
                else PrintWarning("Введите время цифрами!");
                return;
            }
            BasePlayer target = GetPlayerNickOrID(NameOrID);
            if (target == null)
            {
                if (arg.Player() != null)
                    arg.Player().ConsoleMessage("Такого игрока нет на сервере");
                else PrintWarning("Такого игрока нет на сервере");
                return;
            }

            MutePlayer(target, MuteType.Chat, 0, arg.Player(), Reason, TimeMute, true, true);
        }

        [ChatCommand("hmute")]
        void HideMute(BasePlayer Moderator, string cmd, string[] arg)
        {
            if (!permission.UserHasPermission(Moderator.UserIDString, PermissionMute)) return;
            if (arg == null || arg.Length != 3 || arg.Length > 3)
            {
                ReplySystem(Moderator, "Неверный синтаксис,используйте : hmute Steam64ID/Ник Причина Время(секунды)");
                return;
            }
            string NameOrID = arg[0];
            string Reason = arg[1];
            Int32 TimeMute = 0;
            if (!Int32.TryParse(arg[2], out TimeMute))
            {
                ReplySystem(Moderator, "Введите время цифрами!");
                return;
            }
            BasePlayer target = GetPlayerNickOrID(NameOrID);
            if (target == null)
            {
                ReplySystem(Moderator, "Такого игрока нет на сервере");
                return;
            }

            MutePlayer(target, MuteType.Chat, 0, Moderator, Reason, TimeMute, true, true);
        }

        [ConsoleCommand("hunmute")]
        void HideUnMuteConsole(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null)
                if (!permission.UserHasPermission(arg.Player().UserIDString, PermissionMute)) return;
            if (arg == null || arg.Args == null || arg.Args.Length != 1 || arg.Args.Length > 1)
            {
                PrintWarning("Неверный синтаксис,используйте : hunmute Steam64ID");
                return;
            }
            string NameOrID = arg.Args[0];
            BasePlayer target = GetPlayerNickOrID(NameOrID);
            if (target == null)
            {
                PrintWarning("Такого игрока нет на сервере");
                return;
            }

            UnmutePlayer(target, MuteType.Chat, arg.Player(), true, true);
        }

        [ChatCommand("hunmute")]
        void HideUnMute(BasePlayer Moderator, string cmd, string[] arg)
        {
            if (!permission.UserHasPermission(Moderator.UserIDString, PermissionMute)) return;
            if (arg == null || arg.Length != 1 || arg.Length > 1)
            {
                ReplySystem(Moderator, "Неверный синтаксис,используйте : hunmute Steam64ID/Ник");
                return;
            }
            string NameOrID = arg[0];
            BasePlayer target = GetPlayerNickOrID(NameOrID);
            if (target == null)
            {
                ReplySystem(Moderator, "Такого игрока нет на сервере");
                return;
            }

            UnmutePlayer(target, MuteType.Chat, Moderator, true, true);
        }

        #endregion

        #region Mute

        [ConsoleCommand("mute")]
        void MuteCustomAdmin(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null)
                if (!permission.UserHasPermission(arg.Player().UserIDString, PermissionMute)) return;
            if (arg == null || arg.Args == null || arg.Args.Length != 3 || arg.Args.Length > 3)
            {
                PrintWarning("Неверный синтаксис,используйте : mute Steam64ID/Ник Причина Время(секунды)");
                return;
            }
            string NameOrID = arg.Args[0];
            string Reason = arg.Args[1];
            Int32 TimeMute = 0;
            if(!Int32.TryParse(arg.Args[2], out TimeMute))
            {
                PrintWarning("Введите время цифрами!");
                return;
            }
            BasePlayer target = GetPlayerNickOrID(NameOrID);
            if (target == null)
            {
                PrintWarning("Такого игрока нет на сервере");
                return;
            }

            MutePlayer(target, MuteType.Chat, 0, arg.Player(), Reason, TimeMute, false, true);
            Puts("Успешно");
        }

        [ChatCommand("mute")]
        void MuteCustomChat(BasePlayer Moderator, string cmd, string[] arg)
        {
            if (!permission.UserHasPermission(Moderator.UserIDString, PermissionMute)) return;
            if (arg == null || arg.Length != 3 || arg.Length > 3)
            {
                ReplySystem(Moderator, "Неверный синтаксис, используйте : mute Steam64ID/Ник Причина Время(секунды)");
                return;
            }
            string NameOrID = arg[0];
            string Reason = arg[1];
            Int32 TimeMute = 0;
            if (!Int32.TryParse(arg[2], out TimeMute))
            {
                ReplySystem(Moderator, "Введите время цифрами!");
                return;
            }
            BasePlayer target = GetPlayerNickOrID(NameOrID);
            if (target == null)
            {
                ReplySystem(Moderator, "Такого игрока нет на сервере");
                return;
            }

            MutePlayer(target, MuteType.Chat, 0, Moderator, Reason, TimeMute, false, true);
        }

        [ConsoleCommand("unmute")]
        void UnMuteCustomAdmin(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null)
                if (!permission.UserHasPermission(arg.Player().UserIDString, PermissionMute)) return;
            if (arg == null || arg.Args == null || arg.Args.Length != 1 || arg.Args.Length > 1)
            {
                PrintWarning("Неверный синтаксис,используйте : unmute Steam64ID");
                return;
            }
            string NameOrID = arg.Args[0];
            BasePlayer target = GetPlayerNickOrID(NameOrID);
            if (target == null)
            {
                PrintWarning("Такого игрока нет на сервере");
                return;
            }
            UnmutePlayer(target, MuteType.Chat, arg.Player(), false, true);
            Puts("Успешно");
        }

        [ChatCommand("unmute")]
        void UnMuteCustomChat(BasePlayer Moderator, string cmd, string[] arg)
        {
            if (!permission.UserHasPermission(Moderator.UserIDString, PermissionMute)) return;
            if (arg == null || arg.Length != 1 || arg.Length > 1)
            {
                ReplySystem(Moderator, "Неверный синтаксис,используйте : unmute Steam64ID");
                return;
            }
            string NameOrID = arg[0];
            BasePlayer target = GetPlayerNickOrID(NameOrID);
            if (target == null)
            {
                ReplySystem(Moderator, "Такого игрока нет на сервере");
                return;
            }
            UnmutePlayer(target, MuteType.Chat, Moderator, false, true);
        }

        #endregion

        #region ShowOnline
        [ChatCommand("online")]
        private void ShowPlayerOnline(BasePlayer player)
        {
            List<String> PlayerNames = GetPlayersOnline();
            String Message = GetLang("IQCHAT_INFO_ONLINE", player.UserIDString, String.Join($"\n", PlayerNames));
            ReplySystem(player, Message);
        }

        [ConsoleCommand("online")]
        private void ShowPlayerOnlineConsole(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            List<String> PlayerNames = GetPlayersOnline();
            String Message = GetLang("IQCHAT_INFO_ONLINE", player != null ? player.UserIDString : null, String.Join($"\n", PlayerNames));

            if (player != null)
                player.ConsoleMessage(Message);
            else
            {
                String Pattern = @"</?size.*?>|</?color.*?>";
                String Messages = Regex.IsMatch(Message, Pattern) ? Regex.Replace(Message, Pattern, "") : Message;
                Puts(Messages);
            }
        }
        #endregion

        #region Alert Command

        #region Chat Command

        [ChatCommand("alert")]
        private void AlertChatCommand(BasePlayer Sender, String cmd, String[] args)
        {
            if (!permission.UserHasPermission(Sender.UserIDString, PermissionAlert)) return;
            Alert(Sender, args, false);
        }      
        [ChatCommand("adminalert")]
        private void AdminAlertChatCommand(BasePlayer Sender, String cmd, String[] args)
        {
            if (!permission.UserHasPermission(Sender.UserIDString, PermissionAlert)) return;
            Alert(Sender, args, true);
        }

        [ChatCommand("alertui")]
        private void AlertUIChatCommand(BasePlayer Sender, String cmd, String[] args)
        {
            if (!permission.UserHasPermission(Sender.UserIDString, PermissionAlert)) return;
            AlertUI(Sender, args);
        }     
        [ChatCommand("alertuip")]
        private void AlertUIPChatCommand(BasePlayer Sender, String cmd, String[] args)
        {
            if (!permission.UserHasPermission(Sender.UserIDString, PermissionAlert)) return;
            if (args == null || args.Length == 0)
            {
                ReplySystem(Sender, "Вы не указали игрока!");
                return;
            }
            BasePlayer Recipient = BasePlayer.Find(args[0]);
            if (Recipient == null)
            {
                ReplySystem(Sender, "Игрока нет на сервере!");
                return;
            }
            AlertUI(Sender, Recipient, args.Skip(1).ToArray());
        }    
        [ChatCommand("saybro")]
        private void AlertOnlyPlayerChatCommand(BasePlayer Sender, String cmd, String[] args)
        {
            if (!permission.UserHasPermission(Sender.UserIDString, PermissionAlert)) return;
            if (args == null || args.Length == 0)
            {
                ReplySystem(Sender, "Вы не указали игрока!");
                return;
            }
            BasePlayer Recipient = BasePlayer.Find(args[0]);
            if (Recipient == null)
            {
                ReplySystem(Sender, "Игрока нет на сервере!");
                return;
            }
            Alert(Sender, Recipient, args.Skip(1).ToArray());
        }
        #endregion

        #region Console Command

        [ConsoleCommand("alert")]
        private void AlertConsoleCommand(ConsoleSystem.Arg args)
        {
            BasePlayer Sender = args.Player();
            if (Sender != null)
                if (!permission.UserHasPermission(Sender.UserIDString, PermissionAlert)) return;

            Alert(Sender, args.Args, false);
        }
        [ConsoleCommand("adminalert")]
        private void AdminAlertConsoleCommand(ConsoleSystem.Arg args)
        {
            BasePlayer Sender = args.Player();
            if (Sender != null)
                if (!permission.UserHasPermission(Sender.UserIDString, PermissionAlert)) return;
            Alert(Sender, args.Args, true);
        }

        [ConsoleCommand("alertui")]
        private void AlertUIConsoleCommand(ConsoleSystem.Arg args)
        {
            BasePlayer Sender = args.Player();
            if (Sender != null)
                if (!permission.UserHasPermission(Sender.UserIDString, PermissionAlert)) return;
            AlertUI(Sender, args.Args);
        }
        [ConsoleCommand("alertuip")]
        private void AlertUIPConsoleCommand(ConsoleSystem.Arg args)
        {
            BasePlayer Sender = args.Player();
            if (Sender != null)
                if (!permission.UserHasPermission(Sender.UserIDString, PermissionAlert)) return;
            if (args.Args == null || args.Args.Length == 0)
            {
                if (Sender != null)
                    ReplySystem(Sender, "Вы не указали игрока!");
                else PrintWarning("Вы не указали игрока!");
                return;
            }
            BasePlayer Recipient = BasePlayer.Find(args.Args[0]);
            if (Recipient == null)
            {
                if (Sender != null)
                    ReplySystem(Sender, "Игрока нет на сервере!");
                else PrintWarning("Игрока нет на сервере!");
                return;
            }
            AlertUI(Sender, Recipient, args.Args.Skip(1).ToArray());
        }
        [ConsoleCommand("saybro")]
        private void AlertOnlyPlayerConsoleCommand(ConsoleSystem.Arg args)
        {
            BasePlayer Sender = args.Player();
            if (Sender != null)
                if (!permission.UserHasPermission(Sender.UserIDString, PermissionAlert)) return;
                
            if (args.Args == null || args.Args.Length == 0)
            {
                if (Sender != null)
                    ReplySystem(Sender, "Вы не указали игрока!");
                else PrintWarning("Вы не указали игрока!");
                return;
            }
            BasePlayer Recipient = BasePlayer.Find(args.Args[0]);
            if (Recipient == null)
            {
                if (Sender != null)
                    ReplySystem(Sender, "Игрока нет на сервере!");
                else PrintWarning("Игрока нет на сервере!");
                return;
            }
            Alert(Sender, Recipient, args.Args.Skip(1).ToArray());
        }
        #endregion

        #endregion

        #region Admin Command

        #region Rename
        [ChatCommand("rename")]
        private void ChatCommandRename(BasePlayer Renamer, string command, string[] args)
        {
            if (!permission.UserHasPermission(Renamer.UserIDString, PermissionRename)) return;
            GeneralInformation General = GeneralInfo;
            if (General == null) return;

            if (Renamer == null)
            {
                ReplySystem(Renamer,"Вы можете использовать эту команду только находясь на сервере");
                return;
            }
            if (args.Length == 0 || args == null)
            {
                ReplySystem(Renamer, lang.GetMessage("COMMAND_RENAME_NOTARG", this, Renamer.UserIDString));
                return;
            }

            String Name = args[0];
            UInt64 ID = Renamer.userID;
            if(args.Length == 2 && args[1] != null && !String.IsNullOrWhiteSpace(args[1]))
                if(!UInt64.TryParse(args[1], out ID))
                {
                    ReplySystem(Renamer, GetLang("COMMAND_RENAME_NOT_ID", Renamer.UserIDString));
                    return;
                }

            if (General.RenameList.ContainsKey(Renamer.userID))
            {
                General.RenameList[Renamer.userID].RenameNick = Name;
                General.RenameList[Renamer.userID].RenameID = ID;
            }
            else General.RenameList.Add(Renamer.userID, new GeneralInformation.RenameInfo { RenameNick = Name, RenameID = ID });

            ReplySystem(Renamer, GetLang("COMMAND_RENAME_SUCCES", Renamer.UserIDString, Name, ID));
            Renamer.displayName = Name;
        }

        [ConsoleCommand("rename")]
        private void ConsoleCommandRename(ConsoleSystem.Arg args)
        {
            BasePlayer Renamer = args.Player();
            if (Renamer == null)
            {
                PrintWarning("Вы можете использовать эту команду только находясь на сервере");
                return;
            }

            if (!permission.UserHasPermission(Renamer.UserIDString, PermissionRename)) return;
            GeneralInformation General = GeneralInfo;
            if (General == null) return;

            if (args.Args.Length == 0 || args == null)
            {
                ReplySystem(Renamer, lang.GetMessage("COMMAND_RENAME_NOTARG", this, Renamer.UserIDString));
                return;
            }

            String Name = args.Args[0];
            UInt64 ID = Renamer.userID;
            if (args.Args.Length == 2 && args.Args[1] != null && !String.IsNullOrWhiteSpace(args.Args[1]))
                if (!UInt64.TryParse(args.Args[1], out ID))
                {
                    ReplySystem(Renamer, lang.GetMessage("COMMAND_RENAME_NOT_ID", this, Renamer.UserIDString));
                    return;
                }

            if (General.RenameList.ContainsKey(Renamer.userID))
            {
                General.RenameList[Renamer.userID].RenameNick = Name;
                General.RenameList[Renamer.userID].RenameID = ID;
            }
            else General.RenameList.Add(Renamer.userID, new GeneralInformation.RenameInfo { RenameNick = Name, RenameID = ID });

            ReplySystem(Renamer, GetLang("COMMAND_RENAME_SUCCES", Renamer.UserIDString, Name, ID));
            Renamer.displayName = Name;
        }

        #endregion

        [ConsoleCommand("set")]
        private void CommandSet(ConsoleSystem.Arg args)
        {
            BasePlayer Sender = args.Player();

            if (Sender != null) 
                if(!Sender.IsAdmin)
                    return;

            if (args == null || args.Args == null || args.Args.Length != 3)
            {
                if (Sender != null)
                    ReplySystem(Sender, "Используйте правильно ситаксис : set [Steam64ID] [prefix/chat/nick/custom] [Argument]");
                else PrintWarning("Используйте правильно ситаксис : set [Steam64ID] [prefix/chat/nick/custom] [Argument]");
                return;
            }

            UInt64 Steam64ID = 0;
            BasePlayer player = null;

            if (UInt64.TryParse(args.Args[0], out Steam64ID))
                player = BasePlayer.FindByID(Steam64ID);

            if (player == null)
            {
                if (Sender != null)
                    ReplySystem(Sender, "Неверно указан SteamID игрока или ошибка в синтаксисе\nИспользуйте правильно ситаксис : set [Steam64ID] [prefix/chat/nick/custom] [Argument]");
                else PrintWarning("Неверно указан SteamID игрока или ошибка в синтаксисе\nИспользуйте правильно ситаксис : set [Steam64ID] [prefix/chat/nick/custom] [Argument]");
                return;
            }
            if(!UserInformation.ContainsKey(player.userID))
            {
                if (Sender != null)
                    ReplySystem(Sender, $"Игрок не найден!");
                else PrintWarning($"Игрок не найден!");
                return;
            }
            User Info = UserInformation[player.userID];

            Configuration.ControllerParameters ControllerParameter = config.ControllerParameter;

            switch (args.Args[1])
            {
                case "prefix": 
                    {
                        String KeyPrefix = args.Args[2];
                        if(ControllerParameter.Prefixes.Prefixes.Count(prefix => prefix.Argument.Contains(KeyPrefix)) == 0)
                        {
                            if (Sender != null)
                                ReplySystem(Sender, $"Аргумент не найден в вашей конфигурации!");
                            else PrintWarning($"Аргумент не найден в вашей конфигурации");
                            return;
                        }

                        foreach (Configuration.ControllerParameters.AdvancedFuncion Prefix in ControllerParameter.Prefixes.Prefixes.Where(prefix => prefix.Argument.Contains(KeyPrefix)).Take(1))
                        {
                            if (ControllerParameter.Prefixes.TurnMultiPrefixes)
                                Info.Info.PrefixList.Add(Prefix.Argument);
                            else Info.Info.Prefix = Prefix.Argument;

                            if (Sender != null)
                                ReplySystem(Sender,$"Префикс успешно установлен на - {Prefix.Argument}");
                            else Puts($"Префикс успешно установлен на - {Prefix.Argument}");
                        }
                        break;
                    }
                case "chat":
                    {
                        String KeyChatColor = args.Args[2];
                        if (ControllerParameter.MessageColorList.Count(color => color.Argument.Contains(KeyChatColor)) == 0)
                        {
                            if (Sender != null)
                                ReplySystem(Sender, $"Аргумент не найден в вашей конфигурации!");
                            else PrintWarning($"Аргумент не найден в вашей конфигурации");
                            return;
                        }

                        foreach (Configuration.ControllerParameters.AdvancedFuncion ChatColor in ControllerParameter.MessageColorList.Where(color => color.Argument.Contains(KeyChatColor)).Take(1))
                        {
                            Info.Info.ColorMessage = ChatColor.Argument;
                            if (Sender != null)
                                ReplySystem(Sender, $"Цвет сообщения успешно установлен на - {ChatColor.Argument}");
                            else Puts($"Цвет сообщения успешно установлен на - {ChatColor.Argument}");
                        }
                        break;
                    }
                case "nick":
                    {
                        String KeyNickColor = args.Args[2];
                        if (ControllerParameter.NickColorList.Count(color => color.Argument.Contains(KeyNickColor)) == 0)
                        {
                            if (Sender != null)
                                ReplySystem(Sender, $"Аргумент не найден в вашей конфигурации!");
                            else PrintWarning($"Аргумент не найден в вашей конфигурации");
                            return;
                        }

                        foreach (Configuration.ControllerParameters.AdvancedFuncion NickColor in ControllerParameter.NickColorList.Where(color => color.Argument.Contains(KeyNickColor)).Take(1))
                        {
                            Info.Info.ColorNick = NickColor.Argument;
                            if (Sender != null)
                                ReplySystem(Sender, $"Цвет сообщения успешно установлен на - {NickColor.Argument}");
                            else Puts($"Цвет сообщения успешно установлен на - {NickColor.Argument}");
                        }
                        break;
                    }
                case "custom":
                    {
                        String CustomPrefix = args.Args[2];
                        if (ControllerParameter.Prefixes.TurnMultiPrefixes)
                            Info.Info.PrefixList.Add(CustomPrefix);
                        else Info.Info.Prefix = CustomPrefix;
                        if (Sender != null)
                            ReplySystem(Sender,$"Кастомный префикс успешно установлен на - {CustomPrefix}");
                        else Puts($"Кастомный префикс успешно установлен на - {CustomPrefix}");

                        break;
                    }
                default:
                    {
                        if (Sender != null)
                            ReplySystem(Sender,"Используйте правильно ситаксис : set [Steam64ID] [prefix/chat/nick/custom] [Argument]");
                        break;
                    }
            }

        }
        #endregion

        #region PM

        [ChatCommand("pm")]
        void PmChat(BasePlayer Sender, String cmd, String[] arg)
        {
            Configuration.ControllerMessage ControllerMessages = config.ControllerMessages;
            if (!ControllerMessages.TurnedFunc.PMSetting.PMActivate) return;
            if (arg.Length == 0 || arg == null)
            {
                ReplySystem(Sender, lang.GetMessage("COMMAND_PM_NOTARG", this, Sender.UserIDString));
                return;
            }

            Configuration.ControllerMessage.TurnedFuncional.AntiNoob.Settings antiNoob = config.ControllerMessages.TurnedFunc.AntiNoobSetting.AntiNoobPM;
            if (antiNoob.AntiNoobActivate)
                if (IsNoob(Sender.userID, antiNoob.TimeBlocked))
                {
                    ReplySystem(Sender, GetLang("IQCHAT_INFO_ANTI_NOOB_PM", Sender.UserIDString, FormatTime(UserInformationConnection[Sender.userID].LeftTime(antiNoob.TimeBlocked), Sender.UserIDString)));
                    return;
                }

            String NameUser = arg[0];

            if (config.ReferenceSetting.IQFakeActiveSettings.UseIQFakeActive)
                if (IQFakeActive)
                    if (IsFake(NameUser))
                    {
                        ReplySystem(Sender, GetLang("COMMAND_PM_SUCCESS", Sender.UserIDString, string.Join(" ", arg.ToArray()).Replace(NameUser, ""), NameUser));
                        return;
                    }

            BasePlayer TargetUser = GetPlayerNickOrID(NameUser);
            if (TargetUser == null || NameUser == null || !UserInformation.ContainsKey(TargetUser.userID))
            {
                ReplySystem(Sender, GetLang("COMMAND_PM_NOT_USER", Sender.UserIDString));
                return;
            }

            User InfoTarget = UserInformation[TargetUser.userID];
            User InfoSender = UserInformation[Sender.userID];
            if (!InfoTarget.Settings.TurnPM)
            {
                ReplySystem(Sender, GetLang("FUNC_MESSAGE_PM_TURN_FALSE", Sender.UserIDString));
                return;
            }

            if (ControllerMessages.TurnedFunc.IgnoreUsePM)
            {
                if (InfoTarget.Settings.IsIgnored(Sender.userID))
                {
                    ReplySystem(Sender, GetLang("IGNORE_NO_PM", Sender.UserIDString));
                    return;
                }
                if (InfoSender.Settings.IsIgnored(TargetUser.userID))
                {
                    ReplySystem(Sender, GetLang("IGNORE_NO_PM_ME", Sender.UserIDString));
                    return;
                }
            }
            String Message = GetMessageInArgs(Sender, arg.Skip(1).ToArray());

            if (Message == null || Message.Length <= 0)
            {
                ReplySystem(Sender, GetLang("COMMAND_PM_NOT_NULL_MSG", Sender.UserIDString));
                return;
            }
            Message = Message.EscapeRichText();

            if (Message.Length > 125) return;

            PMHistory[TargetUser] = Sender;
            PMHistory[Sender] = TargetUser;

            GeneralInformation.RenameInfo RenamerSender = GeneralInfo.GetInfoRename(Sender.userID);
            GeneralInformation.RenameInfo RenamerTarget = GeneralInfo.GetInfoRename(TargetUser.userID);

            String DisplayNameSender = RenamerSender != null ? RenamerSender.RenameNick ?? Sender.displayName : Sender.displayName;
            String TargetDisplayName = RenamerTarget != null ? RenamerTarget.RenameNick ?? TargetUser.displayName : TargetUser.displayName;
            ReplySystem(TargetUser, GetLang("COMMAND_PM_SEND_MSG", TargetUser.UserIDString, DisplayNameSender, Message));
            ReplySystem(Sender, GetLang("COMMAND_PM_SUCCESS", Sender.UserIDString, Message, TargetDisplayName));

            if (InfoTarget.Settings.TurnSound)
                Effect.server.Run(ControllerMessages.TurnedFunc.PMSetting.SoundPM, TargetUser.GetNetworkPosition());

            Log($"ЛИЧНЫЕ СООБЩЕНИЯ : {Sender.userID}({Sender.displayName}) отправил сообщение игроку - {TargetUser.displayName}({TargetDisplayName})\nСООБЩЕНИЕ : {Message}");
            DiscordLoggPM(Sender, TargetUser, Message);

            RCon.Broadcast(RCon.LogType.Chat, new Chat.ChatEntry
            {
                Message = $"ЛИЧНЫЕ СООБЩЕНИЯ : {Sender.displayName}({Sender.userID}) -> {TargetUser.displayName} : СООБЩЕНИЕ : {Message}",
                UserId = Sender.UserIDString,
                Username = Sender.displayName,
                Channel = Chat.ChatChannel.Global,
                Time = (DateTime.UtcNow.Hour * 3600) + (DateTime.UtcNow.Minute * 60),
                Color = "#3f4bb8",
            });
            PrintWarning($"ЛИЧНЫЕ СООБЩЕНИЯ : {Sender.displayName}({Sender.userID}) -> {TargetUser.displayName} : СООБЩЕНИЕ : {Message}");
        }

        [ChatCommand("r")]
        void RChat(BasePlayer Sender, string cmd, string[] arg)
        {
            Configuration.ControllerMessage ControllerMessages = config.ControllerMessages;
            if (!ControllerMessages.TurnedFunc.PMSetting.PMActivate) return;

            if (arg.Length == 0 || arg == null)
            {
                ReplySystem(Sender, GetLang("COMMAND_R_NOTARG", Sender.UserIDString));
                return;
            }

            Configuration.ControllerMessage.TurnedFuncional.AntiNoob.Settings antiNoob = config.ControllerMessages.TurnedFunc.AntiNoobSetting.AntiNoobPM;
            if (antiNoob.AntiNoobActivate)
                if (IsNoob(Sender.userID, antiNoob.TimeBlocked))
                {
                    ReplySystem(Sender, GetLang("IQCHAT_INFO_ANTI_NOOB_PM", Sender.UserIDString, FormatTime(UserInformationConnection[Sender.userID].LeftTime(antiNoob.TimeBlocked), Sender.UserIDString)));
                    return;
                }

            if (!PMHistory.ContainsKey(Sender))
            {
                ReplySystem(Sender, GetLang("COMMAND_R_NOTMSG", Sender.UserIDString));
                return;
            }

            BasePlayer RetargetUser = PMHistory[Sender];
            if (RetargetUser == null)
            {
                ReplySystem(Sender, GetLang("COMMAND_PM_NOT_USER", Sender.UserIDString));
                return;
            }

            User InfoRetarget = UserInformation[RetargetUser.userID];
            User InfoSender = UserInformation[RetargetUser.userID];

            if (!InfoRetarget.Settings.TurnPM)
            {
                ReplySystem(Sender, GetLang("FUNC_MESSAGE_PM_TURN_FALSE", Sender.UserIDString));
                return;
            }
            if (ControllerMessages.TurnedFunc.IgnoreUsePM)
            {
                if (InfoRetarget.Settings.IsIgnored(Sender.userID))
                {
                    ReplySystem(Sender, GetLang("IGNORE_NO_PM", Sender.UserIDString));
                    return;
                }
                if (InfoSender.Settings.IsIgnored(RetargetUser.userID))
                {
                    ReplySystem(Sender, GetLang("IGNORE_NO_PM_ME", Sender.UserIDString));
                    return;
                }
            }
            
            String Message = GetMessageInArgs(Sender, arg);
            if (Message == null || Message.Length <= 0)
            {
                ReplySystem(Sender, GetLang("COMMAND_PM_NOT_NULL_MSG", Sender.UserIDString));
                return;
            }
            if (Message.Length > 125) return;
            Message = Message.EscapeRichText();

            PMHistory[RetargetUser] = Sender;

            GeneralInformation.RenameInfo RenameSender = GeneralInfo.GetInfoRename(Sender.userID);
            GeneralInformation.RenameInfo RenamerTarget = GeneralInfo.GetInfoRename(RetargetUser.userID);
            String DisplayNameSender = RenameSender!= null? RenameSender.RenameNick ?? Sender.displayName : Sender.displayName;
            String TargetDisplayName = RenamerTarget != null ? RenamerTarget.RenameNick ?? RetargetUser.displayName : RetargetUser.displayName;

            ReplySystem(RetargetUser, GetLang("COMMAND_PM_SEND_MSG", RetargetUser.UserIDString, DisplayNameSender, Message));
            ReplySystem(Sender, GetLang("COMMAND_PM_SUCCESS", Sender.UserIDString, Message, TargetDisplayName));

            if (InfoRetarget.Settings.TurnSound)
                Effect.server.Run(ControllerMessages.TurnedFunc.PMSetting.SoundPM, RetargetUser.GetNetworkPosition());

            Log($"ЛИЧНЫЕ СООБЩЕНИЯ : {Sender.displayName} отправил сообщение игроку - {RetargetUser.displayName}\nСООБЩЕНИЕ : {Message}");
            DiscordLoggPM(Sender, RetargetUser, Message);

            RCon.Broadcast(RCon.LogType.Chat, new Chat.ChatEntry
            {
                Message = $"ЛИЧНЫЕ СООБЩЕНИЯ : {Sender.displayName}({Sender.userID}) -> {RetargetUser.displayName} : СООБЩЕНИЕ : {Message}",
                UserId = Sender.UserIDString,
                Username = Sender.displayName,
                Channel = Chat.ChatChannel.Global,
                Time = (DateTime.UtcNow.Hour * 3600) + (DateTime.UtcNow.Minute * 60),
                Color = "#3f4bb8",
            });
            PrintWarning($"ЛИЧНЫЕ СООБЩЕНИЯ : {Sender.displayName}({Sender.userID}) -> {RetargetUser.displayName} : СООБЩЕНИЕ : {Message}");
        }

        [ChatCommand("ignore")]
        void IgnorePlayerPM(BasePlayer player, String cmd, String[] arg)
        {
            Configuration.ControllerMessage ControllerMessages = config.ControllerMessages;
            if (!ControllerMessages.TurnedFunc.IgnoreUsePM) return;

            User Info = UserInformation[player.userID];

            if (arg.Length == 0 || arg == null)
            {
                ReplySystem(player, GetLang("INGORE_NOTARG", player.UserIDString));
                return;
            }
            String NameUser = arg[0];
            BasePlayer TargetUser = BasePlayer.Find(NameUser);

            if (TargetUser == null || NameUser == null)
            {
                ReplySystem(player, GetLang("COMMAND_PM_NOT_USER", player.UserIDString));
                return;
            }

            String Lang = !Info.Settings.IsIgnored(TargetUser.userID) ? GetLang("IGNORE_ON_PLAYER", player.UserIDString, TargetUser.displayName) : GetLang("IGNORE_OFF_PLAYER", player.UserIDString, TargetUser.displayName);
            ReplySystem(player, Lang);

            Info.Settings.IgnoredAddOrRemove(TargetUser.userID);
        }

        #endregion

        private BasePlayer GetPlayerNickOrID(String Info)
        {
            String NameOrID = String.Empty;

            KeyValuePair<UInt64, GeneralInformation.RenameInfo> RenameInformation = GeneralInfo.RenameList.FirstOrDefault(x => x.Value.RenameNick.Contains(Info) || x.Value.RenameID.ToString() == Info);
            if (RenameInformation.Value == null)
                NameOrID = Info;
            else NameOrID = RenameInformation.Key.ToString();

            foreach (BasePlayer Finder in BasePlayer.activePlayerList)
            {
                if (Finder.displayName.ToLower().Contains(NameOrID.ToLower()) || Finder.userID.ToString() == NameOrID)
                    return Finder;
            }

            return null;
        }

        #endregion

        #endregion

        #region Lang
        private new void LoadDefaultMessages()
        {
            PrintWarning("Языковой файл загружается...");
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["FUNC_MESSAGE_MUTE_CHAT"] = "{0} muted {1}\nDuration : {2}\nReason : {3}",
                ["FUNC_MESSAGE_UNMUTE_CHAT"] = "{0} unmuted {1}",
                ["FUNC_MESSAGE_MUTE_VOICE"] = "{0} muted voice to {1}\nDuration : {2}\nReason : {3}",
                ["FUNC_MESSAGE_UNMUTE_VOICE"] = "{0} unmuted voice to {1}",
                ["FUNC_MESSAGE_MUTE_ALL_CHAT"] = "Chat disabled",
                ["FUNC_MESSAGE_UNMUTE_ALL_CHAT"] = "Chat enabled",
                ["FUNC_MESSAGE_MUTE_ALL_VOICE"] = "Voice chat disabled",
                ["FUNC_MESSAGE_UNMUTE_ALL_VOICE"] = "Voice chat enabled",
                ["FUNC_MESSAGE_MUTE_ALL_ALERT"] = "Блокировка Администратором",
                ["FUNC_MESSAGE_PM_TURN_FALSE"] = "Игрок запретил присылать себе личные сообщения",
                ["FUNC_MESSAGE_ALERT_TURN_FALSE"] = "Игрок запретил уведомлять себя",

                ["FUNC_MESSAGE_NO_ARG_BROADCAST"] = "You can not send an empty broadcast message!",

                ["UI_ALERT_TITLE"] = "<size=14><b>Уведомление</b></size>",
                
                ["COMMAND_NOT_PERMISSION"] = "You dont have permissions to use this command",
                ["COMMAND_RENAME_NOTARG"] = "For rename use : /rename [NewNickname] [NewID (Optional)]",
                ["COMMAND_RENAME_NOT_ID"] = "Неверно указан ID для переименования! Используйте Steam64ID, либо оставьте поле пустым",
                ["COMMAND_RENAME_SUCCES"] = "Вы успешно изменили ник!\nВаш ник : {0}\nВаш ID : {1}",

                ["COMMAND_PM_NOTARG"] = "To send pm use : /pm Nickname Message",
                ["COMMAND_PM_NOT_NULL_MSG"] = "Message is empty!",
                ["COMMAND_PM_NOT_USER"] = "User not found or offline",
                ["COMMAND_PM_SUCCESS"] = "Your private message sent successful\nMessage : {0}\nDelivered : {1}",
                ["COMMAND_PM_SEND_MSG"] = "Message from {0}\n{1}",

                ["COMMAND_R_NOTARG"] = "For reply use : /r Message",
                ["COMMAND_R_NOTMSG"] = "You dont have any private conversations yet!",

                ["FLOODERS_MESSAGE"] = "You're typing too fast! Please Wait {0} seconds",

                ["PREFIX_SETUP"] = "You have successfully removed the prefix {0}, it is already activated and installed",
                ["COLOR_CHAT_SETUP"] = "You have successfully picked up the <color={0}>chat color</color>, it is already activated and installed",
                ["COLOR_NICK_SETUP"] = "You have successfully taken the <color={0}>nickname color</color>, it is already activated and installed",

                ["PREFIX_RETURNRED"] = "Your prefix {0} expired, it was reset automatically",
                ["COLOR_CHAT_RETURNRED"] = "Action of your <color={0}>color chat</color> over, it is reset automatically",
                ["COLOR_NICK_RETURNRED"] = "Action of your <color={0}>color nick</color> over, it is reset automatically",

                ["WELCOME_PLAYER"] = "{0} came online",
                ["LEAVE_PLAYER"] = "{0} left",
                ["WELCOME_PLAYER_WORLD"] = "{0} came online. Country: {1}",
                ["LEAVE_PLAYER_REASON"] = "{0} left. Reason: {1}",

                ["IGNORE_ON_PLAYER"] = "You added {0} in black list",
                ["IGNORE_OFF_PLAYER"] = "You removed {0} from black list",
                ["IGNORE_NO_PM"] = "This player added you in black list. Your message has not been delivered.",
                ["IGNORE_NO_PM_ME"] = "You added this player in black list. Your message has not been delivered.",
                ["INGORE_NOTARG"] = "To ignore a player use : /ignore nickname",

                ["DISCORD_SEND_LOG_CHAT"] = "Player : {0}({1})\nFiltred message : {2}\nMessage : {3}",
                ["DISCORD_SEND_LOG_MUTE"] = "{0}({1}) give mute chat\nSuspect : {2}({3})\nReason : {4}",

                ["TITLE_FORMAT_DAYS"] = "D",
                ["TITLE_FORMAT_HOURSE"] = "H",
                ["TITLE_FORMAT_MINUTES"] = "M",
                ["TITLE_FORMAT_SECONDS"] = "S",

                ["IQCHAT_CONTEXT_TITLE"] = "SETTING UP A CHAT", ///"%TITLE%"
                ["IQCHAT_CONTEXT_SETTING_ELEMENT_TITLE"] = "CUSTOM SETTING", ///"%SETTING_ELEMENT%"
                ["IQCHAT_CONTEXT_INFORMATION_TITLE"] = "INFORMATION", ///"%INFORMATION%"
                ["IQCHAT_CONTEXT_SETTINGS_TITLE"] = "SETTINGS", ///"%SETTINGS%"
                ["IQCHAT_CONTEXT_SETTINGS_PM_TITLE"] = "Private messages", ///"%SETTINGS_PM%"
                ["IQCHAT_CONTEXT_SETTINGS_ALERT_TITLE"] = "Notification in the chat", ///"%SETTINGS_ALERT%"
                ["IQCHAT_CONTEXT_SETTINGS_ALERT_PM_TITLE"] = "Mention in the chat", ///"%SETTINGS_ALERT_PM%"
                ["IQCHAT_CONTEXT_SETTINGS_SOUNDS_TITLE"] = "Sound notification", ///"%SETTINGS_SOUNDS%"
                ["IQCHAT_CONTEXT_MUTE_STATUS_NOT"] = "NO", ///"%MUTE_STATUS_PLAYER%"
                ["IQCHAT_CONTEXT_MUTE_STATUS_TITLE"] = "Blocking the chat", ///"%MUTE_STATUS_TITLE%"
                ["IQCHAT_CONTEXT_IGNORED_STATUS_COUNT"] = "<size=11>{0}</size> human (а)", ///"%IGNORED_STATUS_COUNT%"
                ["IQCHAT_CONTEXT_IGNORED_STATUS_TITLE"] = "Ignoring", ///"%IGNORED_STATUS_TITLE%"
                ["IQCHAT_CONTEXT_NICK_DISPLAY_TITLE"] = "Your nickname", ///"%NICK_DISPLAY_TITLE%"
                ["IQCHAT_CONTEXT_NICK_DISPLAY_MESSAGE"] = "i love iqchat",
                ["IQCHAT_CONTEXT_SLIDER_PREFIX_TITLE"] = "Prefix", /// %SLIDER_PREFIX_TITLE%
                ["IQCHAT_CONTEXT_SLIDER_NICK_COLOR_TITLE"] = "Nick", /// %SLIDER_NICK_COLOR_TITLE%
                ["IQCHAT_CONTEXT_SLIDER_MESSAGE_COLOR_TITLE"] = "Message", /// %SLIDER_MESSAGE_COLOR_TITLE%
                ["IQCHAT_CONTEXT_SLIDER_IQRANK_TITLE"] = "Rank",
                ["IQCHAT_CONTEXT_SLIDER_IQRANK_TITLE_NULLER"] = "Absent",
                ["IQCHAT_CONTEXT_SLIDER_PREFIX_TITLE_DESCRIPTION"] = "Choosing a prefix", /// 
                ["IQCHAT_CONTEXT_SLIDER_CHAT_NICK_TITLE_DESCRIPTION"] = "Choosing a nickname color", /// 
                ["IQCHAT_CONTEXT_SLIDER_CHAT_MESSAGE_TITLE_DESCRIPTION"] = "Chat Color Selection", /// 
                ["IQCHAT_CONTEXT_SLIDER_IQRANK_TITLE_DESCRIPTION"] = "Rank Selection", /// 
                ["IQCHAT_CONTEXT_DESCRIPTION_PREFIX"] = "Prefix Setting",
                ["IQCHAT_CONTEXT_DESCRIPTION_NICK"] = "Setting up a nickname",
                ["IQCHAT_CONTEXT_DESCRIPTION_CHAT"] = "Setting up a message",
                ["IQCHAT_CONTEXT_DESCRIPTION_RANK"] = "Setting up the rank",

                ["IQCHAT_ALERT_TITLE"] = "ALERT", /// %TITLE_ALERT%

                ["IQCHAT_TITLE_IGNORE_AND_MUTE_MUTED"] = "LOCK MANAGEMENT", 
                ["IQCHAT_TITLE_IGNORE_AND_MUTE_IGNORED"] = "IGNORING MANAGEMENT",
                ["IQCHAT_TITLE_IGNORE_TITLES"] = "<b>DO YOU REALLY WANT TO IGNORE\n{0}?</b>",
                ["IQCHAT_TITLE_IGNORE_TITLES_UNLOCK"] = "<b>DO YOU WANT TO REMOVE THE IGNORING FROM THE PLAYER\n{0}?</b>",
                ["IQCHAT_TITLE_IGNORE_BUTTON_YES"] = "<b>YES, I WANT TO</b>",
                ["IQCHAT_TITLE_IGNORE_BUTTON_NO"] = "<b>NO, I CHANGED MY MIND</b>",
                ["IQCHAT_TITLE_MODERATION_PANEL"] = "MODERATOR PANEL",

                ["IQCHAT_BUTTON_MODERATION_MUTE_MENU"] = "Lock Management",
                ["IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT"] = "SELECT AN ACTION",
                ["IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT_REASON"] = "SELECT THE REASON FOR BLOCKING",
                ["IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT_CHAT"] = "Block chat",
                ["IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT_VOICE"] = "Block voice",
                ["IQCHAT_BUTTON_MODERATION_UNMUTE_MENU_TITLE_ALERT_CHAT"] = "Unblock chat",
                ["IQCHAT_BUTTON_MODERATION_UNMUTE_MENU_TITLE_ALERT_VOICE"] = "Unlock voice",
                ["IQCHAT_BUTTON_MODERATION_MUTE_ALL_CHAT"] = "Block all chat",
                ["IQCHAT_BUTTON_MODERATION_UNMUTE_ALL_CHAT"] = "Unblock all chat",
                ["IQCHAT_BUTTON_MODERATION_MUTE_ALL_VOICE"] = "Block everyone's voice",
                ["IQCHAT_BUTTON_MODERATION_UNMUTE_ALL_VOICE"] = "Unlock everyone's voice",

                ["IQCHAT_FUNCED_NO_SEND_CHAT_MUTED"] = "You have an active chat lock : {0}",
                ["IQCHAT_FUNCED_NO_SEND_CHAT_MUTED_ALL_CHAT"] = "The administrator blocked everyone's chat. Expect full unblocking",
                ["IQCHAT_FUNCED_NO_SEND_CHAT_MUTED_ALL_VOICE"] = "The administrator blocked everyone's voice chat. Expect full unblocking",
                ["IQCHAT_FUNCED_NO_SEND_CHAT_UMMUTED_ALL_VOICE"] = "The administrator has unblocked the voice chat for everyone",
                ["IQCHAT_FUNCED_NO_SEND_CHAT_UNMUTED_ALL_CHAT"] = "The administrator has unblocked the chat for everyone",

                ["IQCHAT_FUNCED_ALERT_TITLE"] = "<color=#a7f64f><b>[MENTION]</b></color>",
                ["IQCHAT_FUNCED_ALERT_TITLE_ISMUTED"] = "The player has already been muted!",
                ["IQCHAT_FUNCED_ALERT_TITLE_SERVER"] = "Administrator",

                ["IQCHAT_INFO_ONLINE"] = "Now on the server :\n{0}",

                ["IQCHAT_INFO_ANTI_NOOB"] = "You first connected to the server!\nPlay some more {0}\nTo get access to send messages to the global and team chat!",
                ["IQCHAT_INFO_ANTI_NOOB_PM"] = "You first connected to the server!\nPlay some more {0}\nTo access sending messages to private messages!",

            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["FUNC_MESSAGE_MUTE_CHAT"] = "{0} заблокировал чат игроку {1}\nДлительность : {2}\nПричина : {3}",
                ["FUNC_MESSAGE_UNMUTE_CHAT"] = "{0} разблокировал чат игроку {1}",
                ["FUNC_MESSAGE_MUTE_VOICE"] = "{0} заблокировал голос игроку {1}\nДлительность : {2}\nПричина : {3}",
                ["FUNC_MESSAGE_UNMUTE_VOICE"] = "{0} разблокировал голос игроку {1}",
                ["FUNC_MESSAGE_MUTE_ALL_CHAT"] = "Всем игрокам был заблокирован чат",
                ["FUNC_MESSAGE_UNMUTE_ALL_CHAT"] = "Всем игрокам был разблокирован чат",
                ["FUNC_MESSAGE_MUTE_ALL_VOICE"] = "Всем игрокам был заблокирован голос",
                ["FUNC_MESSAGE_MUTE_ALL_ALERT"] = "Блокировка Администратором",
                ["FUNC_MESSAGE_UNMUTE_ALL_VOICE"] = "Всем игрокам был разблокирован голос",

                ["FUNC_MESSAGE_PM_TURN_FALSE"] = "Игрок запретил присылать себе личные сообщения",
                ["FUNC_MESSAGE_ALERT_TURN_FALSE"] = "Игрок запретил уведомлять себя",

                ["FUNC_MESSAGE_NO_ARG_BROADCAST"] = "Вы не можете отправлять пустое сообщение в оповещение!",

                ["UI_ALERT_TITLE"] = "<size=14><b>Уведомление</b></size>",

                ["COMMAND_NOT_PERMISSION"] = "У вас недостаточно прав для данной команды",
                ["COMMAND_RENAME_NOTARG"] = "Используйте команду так : /rename [НовыйНик] [НовыйID (По желанию)]",
                ["COMMAND_RENAME_NOT_ID"] = "Неверно указан ID для переименования! Используйте Steam64ID, либо оставьте поле пустым",
                ["COMMAND_RENAME_SUCCES"] = "Вы успешно изменили ник!\nВаш ник : {0}\nВаш ID : {1}",

                ["COMMAND_PM_NOTARG"] = "Используйте команду так : /pm Ник Игрока Сообщение",
                ["COMMAND_PM_NOT_NULL_MSG"] = "Вы не можете отправлять пустое сообщение",
                ["COMMAND_PM_NOT_USER"] = "Игрок не найден или не в сети",
                ["COMMAND_PM_SUCCESS"] = "Ваше сообщение успешно доставлено\nСообщение : {0}\nДоставлено : {1}",
                ["COMMAND_PM_SEND_MSG"] = "Сообщение от {0}\n{1}",

                ["COMMAND_R_NOTARG"] = "Используйте команду так : /r Сообщение",
                ["COMMAND_R_NOTMSG"] = "Вам или вы ещё не писали игроку в личные сообщения!",

                ["FLOODERS_MESSAGE"] = "Вы пишите слишком быстро! Подождите {0} секунд",

                ["PREFIX_SETUP"] = "Вы успешно забрали префикс {0}, он уже активирован и установлен",
                ["COLOR_CHAT_SETUP"] = "Вы успешно забрали <color={0}>цвет чата</color>, он уже активирован и установлен",
                ["COLOR_NICK_SETUP"] = "Вы успешно забрали <color={0}>цвет ника</color>, он уже активирован и установлен",

                ["PREFIX_RETURNRED"] = "Действие вашего префикса {0} окончено, он сброшен автоматически",
                ["COLOR_CHAT_RETURNRED"] = "Действие вашего <color={0}>цвета чата</color> окончено, он сброшен автоматически",
                ["COLOR_NICK_RETURNRED"] = "Действие вашего <color={0}>цвет ника</color> окончено, он сброшен автоматически",

                ["WELCOME_PLAYER"] = "{0} зашел на сервер",
                ["LEAVE_PLAYER"] = "{0} вышел с сервера",
                ["WELCOME_PLAYER_WORLD"] = "{0} зашел на сервер.Из {1}",
                ["LEAVE_PLAYER_REASON"] = "{0} вышел с сервера.Причина {1}",

                ["IGNORE_ON_PLAYER"] = "Вы добавили игрока {0} в черный список",
                ["IGNORE_OFF_PLAYER"] = "Вы убрали игрока {0} из черного списка",
                ["IGNORE_NO_PM"] = "Данный игрок добавил вас в ЧС,ваше сообщение не будет доставлено",
                ["IGNORE_NO_PM_ME"] = "Вы добавили данного игрока в ЧС,ваше сообщение не будет доставлено",
                ["INGORE_NOTARG"] = "Используйте команду так : /ignore Ник Игрока",

                ["DISCORD_SEND_LOG_CHAT"] = "Игрок : {0}({1})\nФильтрованное сообщение : {2}\nИзначальное сообщение : {3}",
                ["DISCORD_SEND_LOG_MUTE"] = "{0}({1}) выдал блокировку чата\nИгрок : {2}({3})\nПричина : {4}",

                ["TITLE_FORMAT_DAYS"] = "Д",
                ["TITLE_FORMAT_HOURSE"] = "Ч",
                ["TITLE_FORMAT_MINUTES"] = "М",
                ["TITLE_FORMAT_SECONDS"] = "С",

                ["IQCHAT_CONTEXT_TITLE"] = "НАСТРОЙКА ЧАТА", ///"%TITLE%"
                ["IQCHAT_CONTEXT_SETTING_ELEMENT_TITLE"] = "ПОЛЬЗОВАТЕЛЬСКАЯ НАСТРОЙКА", ///"%SETTING_ELEMENT%"
                ["IQCHAT_CONTEXT_INFORMATION_TITLE"] = "ИНФОРМАЦИЯ", ///"%INFORMATION%"
                ["IQCHAT_CONTEXT_SETTINGS_TITLE"] = "НАСТРОЙКИ", ///"%SETTINGS%"
                ["IQCHAT_CONTEXT_SETTINGS_PM_TITLE"] = "Личные сообщения", ///"%SETTINGS_PM%"
                ["IQCHAT_CONTEXT_SETTINGS_ALERT_TITLE"] = "Оповещение в чате", ///"%SETTINGS_ALERT%"
                ["IQCHAT_CONTEXT_SETTINGS_ALERT_PM_TITLE"] = "Упоминание в чате", ///"%SETTINGS_ALERT_PM%"
                ["IQCHAT_CONTEXT_SETTINGS_SOUNDS_TITLE"] = "Звуковое оповещение", ///"%SETTINGS_SOUNDS%"
                ["IQCHAT_CONTEXT_MUTE_STATUS_NOT"] = "НЕТ", ///"%MUTE_STATUS_PLAYER%"
                ["IQCHAT_CONTEXT_MUTE_STATUS_TITLE"] = "Блокировка чата", ///"%MUTE_STATUS_TITLE%"
                ["IQCHAT_CONTEXT_IGNORED_STATUS_COUNT"] = "<size=11>{0}</size> человек (а)", ///"%IGNORED_STATUS_COUNT%"
                ["IQCHAT_CONTEXT_IGNORED_STATUS_TITLE"] = "Игнорирование", ///"%IGNORED_STATUS_TITLE%"
                ["IQCHAT_CONTEXT_NICK_DISPLAY_TITLE"] = "Ваш ник", ///"%NICK_DISPLAY_TITLE%"
                ["IQCHAT_CONTEXT_NICK_DISPLAY_MESSAGE"] = "люблю iqchat", 
                ["IQCHAT_CONTEXT_SLIDER_PREFIX_TITLE"] = "Префикс", /// %SLIDER_PREFIX_TITLE%
                ["IQCHAT_CONTEXT_SLIDER_NICK_COLOR_TITLE"] = "Ник", /// %SLIDER_NICK_COLOR_TITLE%
                ["IQCHAT_CONTEXT_SLIDER_MESSAGE_COLOR_TITLE"] = "Чат", /// %SLIDER_MESSAGE_COLOR_TITLE%
                ["IQCHAT_CONTEXT_SLIDER_IQRANK_TITLE"] = "Ранг",
                ["IQCHAT_CONTEXT_SLIDER_IQRANK_TITLE_NULLER"] = "Отсутствует",
                ["IQCHAT_CONTEXT_SLIDER_PREFIX_TITLE_DESCRIPTION"] = "Выбор префикса", /// 
                ["IQCHAT_CONTEXT_SLIDER_CHAT_NICK_TITLE_DESCRIPTION"] = "Выбор цвета ника", /// 
                ["IQCHAT_CONTEXT_SLIDER_CHAT_MESSAGE_TITLE_DESCRIPTION"] = "Выбор цвета чата", /// 
                ["IQCHAT_CONTEXT_SLIDER_IQRANK_TITLE_DESCRIPTION"] = "Выбор ранга", /// 
                ["IQCHAT_CONTEXT_DESCRIPTION_PREFIX"] = "Настройка префикса", 
                ["IQCHAT_CONTEXT_DESCRIPTION_NICK"] = "Настройка ника", 
                ["IQCHAT_CONTEXT_DESCRIPTION_CHAT"] = "Настройка сообщения",
                ["IQCHAT_CONTEXT_DESCRIPTION_RANK"] = "Настройка ранга",


                ["IQCHAT_ALERT_TITLE"] = "УВЕДОМЛЕНИЕ", /// %TITLE_ALERT%
                ["IQCHAT_TITLE_IGNORE_AND_MUTE_MUTED"] = "УПРАВЛЕНИЕ БЛОКИРОВКАМИ", 
                ["IQCHAT_TITLE_IGNORE_AND_MUTE_IGNORED"] = "УПРАВЛЕНИЕ ИГНОРИРОВАНИЕМ", 
                ["IQCHAT_TITLE_IGNORE_TITLES"] = "<b>ВЫ ДЕЙСТВИТЕЛЬНО ХОТИТЕ ИГНОРИРОВАТЬ\n{0}?</b>", 
                ["IQCHAT_TITLE_IGNORE_TITLES_UNLOCK"] = "<b>ВЫ ХОТИТЕ СНЯТЬ ИГНОРИРОВАНИЕ С ИГРОКА\n{0}?</b>", 
                ["IQCHAT_TITLE_IGNORE_BUTTON_YES"] = "<b>ДА, ХОЧУ</b>", 
                ["IQCHAT_TITLE_IGNORE_BUTTON_NO"] = "<b>НЕТ, ПЕРЕДУМАЛ</b>", 
                ["IQCHAT_TITLE_MODERATION_PANEL"] = "ПАНЕЛЬ МОДЕРАТОРА",

                ["IQCHAT_BUTTON_MODERATION_MUTE_MENU"] = "Управление блокировками",
                ["IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT"] = "ВЫБЕРИТЕ ДЕЙСТВИЕ",
                ["IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT_REASON"] = "ВЫБЕРИТЕ ПРИЧИНУ БЛОКИРОВКИ",
                ["IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT_CHAT"] = "Заблокировать чат",
                ["IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT_VOICE"] = "Заблокировать голос",
                ["IQCHAT_BUTTON_MODERATION_UNMUTE_MENU_TITLE_ALERT_CHAT"] = "Разблокировать чат",
                ["IQCHAT_BUTTON_MODERATION_UNMUTE_MENU_TITLE_ALERT_VOICE"] = "Разблокировать голос",
                ["IQCHAT_BUTTON_MODERATION_MUTE_ALL_CHAT"] = "Заблокировать всем чат",
                ["IQCHAT_BUTTON_MODERATION_UNMUTE_ALL_CHAT"] = "Разблокировать всем чат",
                ["IQCHAT_BUTTON_MODERATION_MUTE_ALL_VOICE"] = "Заблокировать всем голос",
                ["IQCHAT_BUTTON_MODERATION_UNMUTE_ALL_VOICE"] = "Разблокировать всем голос",

                ["IQCHAT_FUNCED_NO_SEND_CHAT_MUTED"] = "У вас имеется активная блокировка чата : {0}",
                ["IQCHAT_FUNCED_NO_SEND_CHAT_MUTED_ALL_CHAT"] = "Администратор заблокировал всем чат. Ожидайте полной разблокировки",
                ["IQCHAT_FUNCED_NO_SEND_CHAT_MUTED_ALL_VOICE"] = "Администратор заблокировал всем голосоввой чат. Ожидайте полной разблокировки",
                ["IQCHAT_FUNCED_NO_SEND_CHAT_UMMUTED_ALL_VOICE"] = "Администратор разрблокировал всем голосоввой чат",
                ["IQCHAT_FUNCED_NO_SEND_CHAT_UNMUTED_ALL_CHAT"] = "Администратор разрблокировал всем чат",

                ["IQCHAT_FUNCED_ALERT_TITLE"] = "<color=#a7f64f><b>[УПОМИНАНИЕ]</b></color>",
                ["IQCHAT_FUNCED_ALERT_TITLE_ISMUTED"] = "Игрок уже был замучен!",
                ["IQCHAT_FUNCED_ALERT_TITLE_SERVER"] = "Администратор",

                ["IQCHAT_INFO_ONLINE"] = "Сейчас на сервере :\n{0}",

                ["IQCHAT_INFO_ANTI_NOOB"] = "Вы впервые подключились на сервер!\nОтыграйте еще {0}\nЧтобы получить доступ к отправке сообщений в глобальный и командный чат!",
                ["IQCHAT_INFO_ANTI_NOOB_PM"] = "Вы впервые подключились на сервер!\nОтыграйте еще {0}\nЧтобы получить доступ к отправке сообщений в личные сообщения!",

            }, this, "ru");
           
            PrintWarning("Языковой файл загружен успешно");
        }
        #endregion

        #region Helpers
        private void Log(String LoggedMessage) => LogToFile("IQChatLogs", LoggedMessage, this);
        public String FormatTime(Double Second, String UserID = null)
        {
            TimeSpan time = TimeSpan.FromSeconds(Second);
            String Result = String.Empty;
            String Days = GetLang("TITLE_FORMAT_DAYS", UserID);
            String Hourse = GetLang("TITLE_FORMAT_HOURSE", UserID);
            String Minutes = GetLang("TITLE_FORMAT_MINUTES", UserID);
            String Seconds = GetLang("TITLE_FORMAT_SECONDS", UserID);

            if (time.Seconds != 0)
                Result = $"{Format(time.Seconds, Seconds, Seconds, Seconds)}";

            if (time.Minutes != 0)
                Result = $"{Format(time.Minutes, Minutes, Minutes, Minutes)}";

            if (time.Hours != 0)
                Result = $"{Format(time.Hours, Hourse, Hourse, Hourse)}";

            if (time.Days != 0)
                Result = $"{Format(time.Days, Days, Days, Days)}";

            return Result;
        }
        private String GetMessageInArgs(BasePlayer Sender, String[] arg)
        {
            if (arg == null || arg.Length == 0)
            {
                if (Sender != null)
                    ReplySystem(Sender, GetLang("FUNC_MESSAGE_NO_ARG_BROADCAST", Sender.UserIDString));
                else PrintWarning(GetLang("FUNC_MESSAGE_NO_ARG_BROADCAST"));
                return null;
            }
            String Message = String.Empty;
            foreach (String msg in arg)
                Message += " " + msg;

            return Message;
        }

        private String Format(Int32 units, String form1, String form2, String form3)
        {
            var tmp = units % 10;

            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                return $"{units}{form1}";

            if (tmp >= 2 && tmp <= 4)
                return $"{units}{form2}";

            return $"{units}{form3}";
        }
        #endregion

        #region API

        void API_SEND_PLAYER(BasePlayer player, String PlayerFormat, String Message, String Avatar, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            Configuration.ControllerMessage ControllerMessages = config.ControllerMessages;

            String OutMessage = String.Empty; ;

            if (ControllerMessages.Formatting.FormatMessage)
                OutMessage = $"{Message.ToLower().Substring(0, 1).ToUpper()}{Message.Remove(0, 1).ToLower()}";

            if (ControllerMessages.Formatting.UseBadWords)
                foreach (String DetectedMessage in OutMessage.Split(' '))
                    if (ControllerMessages.Formatting.BadWords.Contains(DetectedMessage.ToLower()))
                        OutMessage = OutMessage.Replace(DetectedMessage, ControllerMessages.Formatting.ReplaceBadWord);

            player.SendConsoleCommand("chat.add", channel, ulong.Parse(Avatar), $"{PlayerFormat}: {OutMessage}");
            player.ConsoleMessage($"{PlayerFormat}: {OutMessage}");
        }
        void API_SEND_PLAYER_PM(BasePlayer player, string DisplayName, string Message)
        {
            ReplySystem(player, GetLang("COMMAND_PM_SEND_MSG", player.UserIDString, DisplayName, Message));

            if (UserInformation.ContainsKey(player.userID))
                if (UserInformation[player.userID].Settings.TurnSound)
                    Effect.server.Run(config.ControllerMessages.TurnedFunc.PMSetting.SoundPM, player.GetNetworkPosition());
        }
        void API_SEND_PLAYER_CONNECTED(BasePlayer player, String DisplayName, String country, String userID)
        {
            Configuration.ControllerAlert.PlayerSession AlertSessionPlayer = config.ControllerAlertSetting.PlayerSessionSetting;

            if (AlertSessionPlayer.ConnectedAlert)
            {
                String Avatar = AlertSessionPlayer.ConnectedAvatarUse ? userID : String.Empty;
                if (AlertSessionPlayer.ConnectedWorld)
                     ReplyBroadcast(GetLang("WELCOME_PLAYER_WORLD", player.UserIDString, DisplayName, country), CustomAvatar: Avatar);   
                else ReplyBroadcast(GetLang("WELCOME_PLAYER", player.UserIDString, DisplayName), CustomAvatar: Avatar);
            }
        }
        void API_SEND_PLAYER_DISCONNECTED(BasePlayer player, String DisplayName, String reason, String userID)
        {
            Configuration.ControllerAlert.PlayerSession AlertSessionPlayer = config.ControllerAlertSetting.PlayerSessionSetting;

            if (AlertSessionPlayer.DisconnectedAlert)
            {
                String Avatar = AlertSessionPlayer.ConnectedAvatarUse ? userID : String.Empty;
                String LangLeave = AlertSessionPlayer.DisconnectedReason ? GetLang("LEAVE_PLAYER_REASON",player.UserIDString, DisplayName, reason) : GetLang("LEAVE_PLAYER", player.UserIDString, DisplayName);
                ReplyBroadcast(LangLeave, CustomAvatar: Avatar);
            }
        }
        void API_ALERT(String Message, Chat.ChatChannel channel = Chat.ChatChannel.Global, String CustomPrefix = null, String CustomAvatar = null, String CustomHex = null)
        {
            foreach (BasePlayer p in BasePlayer.activePlayerList)
                ReplySystem(p, Message, CustomPrefix, CustomAvatar, CustomHex);
        }
        void API_ALERT_PLAYER(BasePlayer player, String Message, String CustomPrefix = null, String CustomAvatar = null, String CustomHex = null) => ReplySystem(player, Message, CustomPrefix, CustomAvatar, CustomHex);
        void API_ALERT_PLAYER_UI(BasePlayer player, String Message) => DrawUI_IQChat_Alert(player, Message);
        Boolean API_CHECK_MUTE_CHAT(UInt64 ID)
        {
            if (!UserInformation.ContainsKey(ID)) return false;
            return UserInformation[ID].MuteInfo.IsMute(MuteType.Chat);
        }
        Boolean API_CHECK_VOICE_CHAT(UInt64 ID) 
        {
            if (!UserInformation.ContainsKey(ID)) return false;
            return UserInformation[ID].MuteInfo.IsMute(MuteType.Voice);
        }
        Boolean API_IS_IGNORED(UInt64 UserHas, UInt64 User)
        {
            if (!UserInformation.ContainsKey(UserHas)) return false;
            if (!UserInformation.ContainsKey(User)) return false;

            return UserInformation[UserHas].Settings.IsIgnored(User);
        }
        String API_GET_PREFIX(UInt64 ID)
        {
            if (!UserInformation.ContainsKey(ID)) return String.Empty;
            Configuration.ControllerParameters ControllerParameter = config.ControllerParameter;

            User Info = UserInformation[ID];
            String Prefixes = String.Empty;

            if (ControllerParameter.Prefixes.TurnMultiPrefixes)
                Prefixes = String.Join("", Info.Info.PrefixList.Take(ControllerParameter.Prefixes.MaximumMultiPrefixCount));
            else Prefixes = Info.Info.Prefix;

            return Prefixes;
        }
        String API_GET_CHAT_COLOR(UInt64 ID)
        {
            if (!UserInformation.ContainsKey(ID)) return String.Empty;

            return UserInformation[ID].Info.ColorMessage;
        }
        String API_GET_NICK_COLOR(ulong ID)
        {
            if (!UserInformation.ContainsKey(ID)) return String.Empty;

            return UserInformation[ID].Info.ColorNick;
        }
        String API_GET_DEFAULT_PREFIX() => config.ControllerConnect.SetupDefaults.PrefixDefault;
        String API_GET_DEFAULT_NICK_COLOR() => config.ControllerConnect.SetupDefaults.NickDefault;
        String API_GET_DEFAULT_MESSAGE_COLOR() => config.ControllerConnect.SetupDefaults.MessageDefault;

        #endregion
    }
}