using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("RatesController", "Vlad-00003", "2.2.4")]
    [Description("Rate and Time controller - can change rates based on current game time")]
    /*
     * Author info:
     *   E-mail: Vlad-00003@mail.ru
     *   Vk: vk.com/vlad_00003
     * v2.1.2:
     *   Оптимизирована функция создания стандартных привелегий
     *   Опитмизирована функия получения текущих рейтов игрока
     * v2.1.3
     *   Исправлена NRE OnEntityTakeDamage в случае, если урон наносился не игроком(огонь от разрывных к примеру)
     *   Улучшение производительности - если Множители лута выключена - плагин полностью отписывается от хуков.
     *   При атаке игроком Аэрдропа лут больше не обновляется.
     * v2.1.4
     *   Небольшая переработка кода, исправлена ошибка при компиляции.
     *   теперь лут в бочках будет умножаться при разрушении бочки, а не при ударе.
     * v2.1.5
     *   Исправлена небольшая ошибка, из-за которой даже при выключенном множетеле лута множитель всё равно работал
     * v2.1.6
     *   Временный фикс ошибки, связанной с изменившемися id предметов
     * v2.1.7
     *   Исправлено ускорение работы печей, а так же настройка "Стандартное время переработки ресурсов (в секундах)"
     * v2.1.8
     *   ИСправлен дюп с умножением предметов по рейтам
     * v2.2.0
     *   Изменён принцип работы списка предметов, на который не действуют множители. Добавлен переключатель, который определяет режим работы списка -"Чёрный или белый список?".
     *      При установке значения в true (изначальное) - список будет функционировать как раньше - как чёрный.
     *        т.е. множители лута будут действовать на всё, кроме внесённого в список.
     *      При установке значения в false - список будет функционировать как белый.
     *        т.е. множители лута будут действовать ТОЛЬКО на то, что внесено в список.
     *   Содержимое старого списка будет автоматически перенесено в новый.
     */

    class RatesController : RustPlugin
    {
        private bool isDay;

        private string PickUpDayString = "Рейт поднимаемых ресурсов днём";
        private string PickUpNightString = "Рейт поднимаемых ресурсов ночью";
        private string GatherDayString = "Рейт добываемых ресурсов днём";
        private string GatherNightString = "Рейт добываемых ресурсов ночью";
        private string QuerryDayString = "Рейт добываемых ресурсов в карьере днём";
        private string QuerryNightString = "Рейт добываемых ресурсов в карьере ночью";
        private string LootDayString = "Рейт лута днём(если включен)";
        private string LootNightString = "Рейт лута ночью(если включен)";
        private string SmeltDayString = "Скорость работы печей днём";
        private string SmeltNightString = "Скорость работы печей ночью";
        private ItemDefinition hqmo;

        private string DefaultRatesCfg = "Общие рейты ресурсов";
        private string CustomRatesCfg = "Изменение рейтов для игроков с привилегиями";
        private string DefaultPickupRatesCfg = "Стандартные рейты поднимаемых ресурсов";
        private string DefaultGatherRatesCfg = "Стандартные рейты добываемых ресурсов";
        private string DefaultSmeltRatesCfg = "Стандартное время переработки ресурсов (в секундах)";
        private string DefaultQuerryRatesCfg = "Стандартные рейты добываемых ресурсов в карьере";
        private string PrefixCfg = "Префикс в чате";
        private string PrefixColorCfg = "Цвет префикса в чате";
        private string UseLootMultyplierCfg = "Использовать умножение лута(выключите для совместимости с контроллерами лута)";
        private string BlacklistedLootCfgOld = "Список лута, на который не действуют множители";
        private string ListedLootCfg = "Список лута";
        private string IsBlacklistCfg =
            "Чёрный или белый список? (true - чёрный - множители действуют на всё, кроме внесённого)";
        private string DayStartCfg = "Час начала дня(игровое время)";
        private string DayLenghtCfg = "Длина дня(в минутах)";
        private string NightStartCfg = "Час начала ночи(игровое время)";
        private string NightLenghtCfg = "Длина ночи(в минутах)";
        private string WarnChatCfg = "Выводить сообщения в чат о начале дня или ночи";
        private string CoalRateDayCfg = "Рейт угля при сжигании дерева днём";
        private string CoalRateNightCfg = "Рейт угля при сжигании дерева ночью";
        private string CoalChanceDayCfg = "Шанс производства угля днём";
        private string CoalChanceNightCfg = "Шанс производства угля ночью";
        private string MoreHQMCfg = "Добавить металл высокого качества во все рудные жилы";

        private List<string> AvaliableMods;
        private Dictionary<string, float> SmeltBackup = new Dictionary<string, float>();
        //Откат обновления ящиков
        Dictionary<uint, EntityLootetrs> CratesCD = new Dictionary<uint, EntityLootetrs>();

        private class EntityLootetrs
        {
            public List<ulong> Looters;
            public DateTime NextUpdate;

            public EntityLootetrs()
            {
                Looters = new List<ulong>();
            }
            
            public bool IsUpdateBlocked(BasePlayer player)
            {
                if (NextUpdate > DateTime.UtcNow)
                    return true;
                if (player?.userID == null)
                    return true;
                return Looters.Contains(player.userID);
            }
            public void AddLooter(ulong looter, int minutes = 2)
            {
                Looters.Add(looter);
                NextUpdate = DateTime.UtcNow.AddMinutes(minutes);
            }
        }

        //Список рудных жил и их бонусов
        //Dictionary<BaseEntity, Dictionary<string, float>> DefaultFinishBonuses = new Dictionary<BaseEntity, Dictionary<string, float>>();

        private double CoalRate = 1f;
        private int CoalChance = 25;
        private double SmeltRate = 1f;
        private double GatherRate = 1f;
        private double PickupRate = 1f;
        private double QuerryRate = 1f;
        private double LootRate = 1f;

        #region config setup

        private string Prefix = "[Rates controller]";//Префикс плагина в чате
        private bool UseLootMultyplier = true; //Использовать множители лута
        private string PrefixColor = "#ff0000";//Цвет префикса
        private float DayStart = 6f;//Час, когда начинается день
        private float NightStart = 18f;//Час, когда начинается ночь
        private uint DayLenght = 30u;//Длинна дня
        private uint NightLenght = 30u;//Длинна ночи
        private bool WarnChat = true;//Выводить ли в чат оповещения о смене дня\ночи и рейтов.
        private bool MoreHQM = true; //Добавить ли во все рудные жилы мвк?

        //Стандартные рейты, для игроков без привелегий
        private Dictionary<string, double> DefaultRates = new Dictionary<string, double>();
        //Рейты для игроков с привелегиями
        private Dictionary<string, Dictionary<string, double>> CustomRates = new Dictionary<string, Dictionary<string, double>>();

        private double CoalRateDay = 1f;
        private double CoalRateNight = 1f;
        private int CoalChanceDay = 25;
        private int CoalChanceNight = 25;
        private Dictionary<string, double> DefaultSmeltRates = new Dictionary<string, double>();
        private Dictionary<string, double> DefaultGatherRates = new Dictionary<string, double>();
        private Dictionary<string, double> DefaultPickupRates = new Dictionary<string, double>();
        private Dictionary<string, double> DefaultQuerryRates = new Dictionary<string, double>();
        private List<string> ListedLoot = new List<string>();
        private bool IsBlackList = true;

        #endregion

        #region Loading config

        //Загрузка стандартного конфиг-файла. Вызывается ТОЛЬКО в случае отсутствия файла PluginName.json в папке config
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Благодарим за приобритение плагина на сайте RustPlugin.ru.\n Если вы приобрели плагин в другом месте - вы теряете все гарантии.");
        }

        void LoadConfigValues()
        {
            AvaliableMods = new List<string>()
            {
                GatherDayString,
                GatherNightString,
                PickUpDayString,
                PickUpNightString,
                QuerryDayString,
                QuerryNightString,
                LootDayString,
                LootNightString,
                SmeltDayString,
                SmeltNightString
            };
            Dictionary<string, object> defaultRates = CreatePerms(AvaliableMods, 1f);
            Dictionary<string, object> customRates = new Dictionary<string, object>()
            {
                ["ratescontroller.premium"] = CreatePerms(AvaliableMods, 3f),
                ["ratescontroller.vip"] = CreatePerms(AvaliableMods, 2f)
            };
            Dictionary<string, object> defaultGatherRates = new Dictionary<string, object>()
            {
                { "Animal Fat", 1.0},
                {"Bear Meat", 1.0},
                {"Bone Fragments", 1.0},
                {"Cloth", 1.0},
                {"High Quality Metal Ore", 1.0},
                {"Human Skull", 1.0},
                {"Leather", 1.0},
                {"Metal Ore", 1.0},
                {"Pork", 1.0},
                {"Raw Chicken Breast", 1.0},
                {"Raw Human Meat", 1.0},
                {"Raw Wolf Meat", 1.0},
                {"Stones", 1.0},
                {"Sulfur Ore", 1.0},
                {"Wolf Skull", 1.0},
                {"Wood", 1.0},
                {"Raw Deer Meat", 1.0 },
                {"Cactus Flesh", 1.0 }
            };
            Dictionary<string, object> defaultPickupRates = new Dictionary<string, object>()
            {
                {"Metal Ore", 1.0},
                {"Stones", 1.0},
                {"Sulfur Ore", 1.0},
                {"Wood", 1.0},
                {"Hemp Seed", 1.0},
                {"Corn Seed", 1.0},
                {"Pumpkin Seed", 1.0},
                {"Cloth", 1.0},
                {"Pumpkin", 1.0},
                {"Corn", 1.0},
                {"Wolf Skull", 1.0}
            };
            Dictionary<string, object> defaultQuerryRates = new Dictionary<string, object>()
            {
                {"High Quality Metal Ore", 1.0},
                {"Metal Fragments", 1.0},
                {"Metal Ore", 1.0},
                {"Stones", 1.0},
                {"Sulfur Ore", 1.0}
            };
            List<object> listedLoot = new List<object>()
            {
                "Rotten Apple",
                "Spoiled Wolf Meat",
                "Spoiled Chicken",
                "Spoiled Human Meat"
            };
            Dictionary<string, object> defaultSmeltRates = new Dictionary<string, object>();
            //var itemDefinitions = ItemManager.itemList;
            var itemDefinitions = ItemManager.GetItemDefinitions();
            foreach (var item in itemDefinitions)
            {
                // Записываем стандартные рейты готовки
                var cookable = item.GetComponent<ItemModCookable>();
                if (cookable == null) continue;
                defaultSmeltRates.Add(item.displayName.english, cookable.cookTime);
                SmeltBackup.Add(item.displayName.english, cookable.cookTime);
            }
            //Older version compatibility
            if (Config[BlacklistedLootCfgOld] != null)
            {
                //PrintWarning("Old config detected. Getting old values....");
                GetConfig(BlacklistedLootCfgOld, ref listedLoot);
                //PrintWarning("Got values: {0}",string.Join(" | ",listedLoot.Select(x => x.ToString())));
                Config.Remove(BlacklistedLootCfgOld);
                //PrintWarning("Old config var removed");
            }

            //PrintWarning("Getting value by the new address with data: {0}",
            //    string.Join(" | ", listedLoot.Select(x => x.ToString())));
            GetConfig(ListedLootCfg,ref listedLoot);
            //PrintWarning("Got values: {0}", string.Join(" | ", listedLoot.Select(x => x.ToString())));

            GetConfig(IsBlacklistCfg,ref IsBlackList);
            GetConfig(DefaultRatesCfg, ref defaultRates);
            GetConfig(CustomRatesCfg, ref customRates);
            GetConfig(DefaultPickupRatesCfg, ref defaultPickupRates);
            GetConfig(DefaultGatherRatesCfg, ref defaultGatherRates);
            GetConfig(DefaultSmeltRatesCfg, ref defaultSmeltRates);
            GetConfig(DefaultQuerryRatesCfg, ref defaultQuerryRates);
            GetConfig(PrefixCfg, ref Prefix);
            GetConfig(PrefixColorCfg, ref PrefixColor);
            GetConfig(UseLootMultyplierCfg, ref UseLootMultyplier);
            GetConfig(DayStartCfg, ref DayStart);
            GetConfig(DayLenghtCfg, ref DayLenght);
            GetConfig(NightStartCfg, ref NightStart);
            GetConfig(NightLenghtCfg, ref NightLenght);
            GetConfig(WarnChatCfg, ref WarnChat);
            GetConfig(CoalRateDayCfg, ref CoalRateDay);
            GetConfig(CoalRateNightCfg, ref CoalRateNight);
            GetConfig(CoalChanceDayCfg, ref CoalChanceDay);
            GetConfig(CoalChanceNightCfg, ref CoalChanceNight);
            GetConfig(MoreHQMCfg, ref MoreHQM);
            SaveConfig();

            ListedLoot = listedLoot.Select(x => x.ToString()).ToList();

            foreach (var item in defaultRates)
            {
                double mod;
                if (!double.TryParse(item.Value.ToString(), out mod))
                {
                    PrintWarning($"Default rates for {item.Key} is incorrect and will not work until it will be resolved.");
                    continue;
                }
                DefaultRates.Add(item.Key, mod);
            }

            foreach (var item in customRates)
            {
                Dictionary<string, object> perms = (Dictionary<string, object>)item.Value;
                Dictionary<string, double> Perms = new Dictionary<string, double>();
                foreach (var p in perms)
                {
                    double mod;
                    if (!double.TryParse(p.Value.ToString(), out mod))
                    {
                        PrintWarning($"Custom rates for {item.Key} - {p.Key} is incorrect and will not work until it will be resolved.");
                        continue;
                    }
                    Perms.Add(p.Key, mod);
                }
                CustomRates.Add(item.Key, Perms);
            }

            foreach (var item in defaultGatherRates)
            {
                double mod;
                if (!double.TryParse(item.Value.ToString(), out mod))
                {
                    PrintWarning($"Default gather rates for {item.Key} is incorrect and will not work until it will be resolved.");
                    continue;
                }
                DefaultGatherRates.Add(item.Key, mod);
            }

            foreach (var item in defaultPickupRates)
            {
                double mod;
                if (!double.TryParse(item.Value.ToString(), out mod))
                {
                    PrintWarning($"Default pickup rates for {item.Key} is incorrect and will not work until it will be resolved.");
                    continue;
                }
                DefaultPickupRates.Add(item.Key, mod);
            }

            foreach (var item in defaultQuerryRates)
            {
                double mod;
                if (!double.TryParse(item.Value.ToString(), out mod))
                {
                    PrintWarning($"Default quarry rates for {item.Key} is incorrect and will not work until it will be resolved.");
                    continue;
                }
                DefaultQuerryRates.Add(item.Key, mod);
            }

            foreach (var item in defaultSmeltRates)
            {
                double mod;
                if (!double.TryParse(item.Value.ToString(), out mod))
                {
                    PrintWarning($"Default smelt rates for {item.Key} is incorrect and will not work until it will be resolved.");
                    continue;
                }
                DefaultSmeltRates.Add(item.Key, mod);
            }

        }
        #endregion

        #region localization
        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["LootRate"] = "Loot from lootboxes x{rates}\n",
                ["CoalRate"] = "Coal producing rates is x{rates}\n",
                ["No Permission"] = "You don't have permission to use this command.",
                ["Day Starts"] = "The Day is starting!\n",
                ["Night Starts"] = "The Night is starting!\n",
                ["PickupRates"] = "Pickup rates was changed to x{rates}\n",
                ["GatherRates"] = "Gather rates was changed to x{rates}\n",
                ["QuerryRates"] = "Quarry gathering rate was changed to x{rates}\n",
                ["Smelt Rate"] = "Smelt Rate was changed to x{rates}\n",
                ["PersonalRates"] = "Your personal rates:\n",
                ["PickUpRatesPers"] = "Your personal Pickup rates is x{rates}\n",
                ["GatherRatesPers"] = "Your personal Gather rates is x{rates}\n",
                ["QueryPers"] = "Your personal Query rates is x{rates}\n",
                ["SmeltPers"] = "Your personal Smelt rates is x{rates}\n",
                ["LootPers"] = "Your personal Loot rates is x{rates}"
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["LootRate"] = "Рейты лута x{rates}\n",
                ["CoalRate"] = "Рейты производства угля x{rates}\n",
                ["No Permission"] = "Недостаточно прав на выполнение данной команды.",
                ["Day Starts"] = "Начинается день!\n",
                ["Night Starts"] = "Начинается ночь!\n",
                ["PickupRates"] = "Рейты подбираемых предметов x{rates}\n",
                ["GatherRates"] = "Рейты добычи изменены на x{rates}\n",
                ["QuerryRates"] = "Рейты добычи ресурсов в карьерах изменены на x{rates}\n",
                ["Smelt Rate"] = "Скорость работы печей x{rates}\n",
                ["PersonalRates"] = "Ваши личные рейты:\n",
                ["PickUpRatesPers"] = "Рейты подбираемых предметов x{rates}\n",
                ["GatherRatesPers"] = "Рейты добычи x{rates}\n",
                ["QueryPers"] = "Рейты работы карьера x{rates}\n",
                ["SmeltPers"] = "Рейты переплавки x{rates}\n",
                ["LootPers"] = "Рейты лута x{rates}"
            }, this, "ru");
        }
        #endregion

        #region initializing
        void OnServerInitialized()
        {
            //Загружаем конфиг из файла
            LoadConfigValues();

            //Подгружаем данные локализации
            LoadMessages();
            hqmo = ItemManager.FindItemDefinition("hq.metal.ore");
            if (!UseLootMultyplier)
            {
                Unsubscribe("OnContainerDropItems");
                Unsubscribe("OnLootEntity");
            }
            //Инициализируем управление временем - получаем компоненту времени
            timer.Once(3, GetTimeComponent);

            foreach (var perm in CustomRates.Keys)
            {
                permission.RegisterPermission($"{perm}".ToLower(), this);
                //PrintWarning($"{Title}.{perm}".ToLower());
            }
            var curtime = covalence.Server.Time;
            //Если при старте на сервере день - ставим в false, дабы вызывалось событие OnDayStart()
            isDay = (!(DayStart <= curtime.Hour) || !(curtime.Hour < NightStart));

            UpdateFurnaces();
        }

        //Вызывается при выгрузке плагина
        void Unload()
        {
            //Очищаем привящку к ивнту
            if (timeComponent != null)
                timeComponent.OnHour -= OnHour;
            //Восстанавливаем стандартное время переплавки
            foreach (var item in ItemManager.GetItemDefinitions())
            {
                if (!SmeltBackup.ContainsKey(item.displayName.english)) continue;
                var cookable = item.GetComponent<ItemModCookable>();
                if (cookable != null)
                {
                    cookable.cookTime = SmeltBackup[item.displayName.english];
                }
            }
        }

        #endregion

        #region Time managment

        //Переменная, для хранения компоненты времени
        private TOD_Time timeComponent = null;

        //Заморожено ли время.
        private bool Frozen = false;

        #region main
        //Колличество попыток определения компоненты
        private uint componentSearchAttempts = 0;

        //Инициализация управления временем
        private void GetTimeComponent()
        {
            //Если Instance == 0,
            if (TOD_Sky.Instance == null)
            {
                //Увеличиваем номер попытки
                ++componentSearchAttempts;
                if (this.componentSearchAttempts < 50)
                {
                    PrintWarning("Restarting timer for GetTimeComponent(). Attempt " + componentSearchAttempts.ToString() + "/10.");
                    timer.Once(3, GetTimeComponent);
                }
                else
                {
                    RaiseError("Could not find required component after 50 attempts. Plugin will not work without it.\nTry to reload it ant if this won't fix the issue contact the developer - https://vk.com/vlad_00003");
                }

                return;
            }

            if (TOD_Sky.Instance != null && componentSearchAttempts >= 0)
            {
                Puts("Found TOD_Time component after attempt " + componentSearchAttempts.ToString() + ".");
            }

            //Записываем компаненту времени
            timeComponent = TOD_Sky.Instance.Components.Time;

            if (timeComponent == null)
            {
                RaiseError("Could not fetch time component. Plugin will not work without it.");
                return;
            }

            //Добавляем ивент к событию
            timeComponent.OnHour += OnHour;

            //Вызываем функцию, дабы узнать текущее время суток.
            OnHour();

        }

        //Идёт ли прогресс времени, данные хватаем из игры
        //Ибо костылями - мир полнится....
        private bool ProgressTime
        {
            get
            {
                return timeComponent.ProgressTime; 
            }
            set
            {
                timeComponent.ProgressTime = value;
            }
        }

        //Обработчик события OnHour
        private void OnHour()
        {
            if (DayStart <= CurrentHour && CurrentHour < NightStart)
            {
                if (isDay) return;
                //Устанавливаем время суток на День
                isDay = true;
                //Вызываем процедуру обновления длинны суток
                UpdateDayLenght(DayLenght, false);
                Interface.Oxide.CallHook("OnDayStart");
            }
            else
            {
                if (!isDay) return;
                //Устанавливаем время суток на ночь
                isDay = false;
                //Вызываем процедуру обновления длинны суток
                UpdateDayLenght(NightLenght, true);
                Interface.Oxide.CallHook("OnNightStart");
            }
        }
		//timeComponent.DayLengthInMinutes = dayLength * (24.0f / (TOD_Sky.Instance.SunsetTime - TOD_Sky.Instance.SunriseTime));
		//timeComponent.DayLengthInMinutes = nightLength * (24.0f / (24.0f - (TOD_Sky.Instance.SunsetTime - TOD_Sky.Instance.SunriseTime)));
        //Функция, обновляющяя длительность суток в зависимости от времени суток в игре.
        void UpdateDayLenght(uint Lenght, bool night)
        {
            float dif = NightStart - DayStart;
            if (night)
            {
                dif = (24 - dif);
            }
            float part = 24.0f / dif;
            float newLenght = part * Lenght;
            if (newLenght <= 0) newLenght = 0.1f;
            timeComponent.DayLengthInMinutes = newLenght;
        }

        #endregion

        #region Helpers

        //Возвращает текущий час
        public float CurrentHour => TOD_Sky.Instance.Cycle.Hour;

        #endregion

        #endregion

        #region Rate controller
        //Бочки с лутом
        //void ClearContainer(LootContainer container)
        //{
        //    while (container.inventory.itemList.Count > 0)
        //    {
        //        var item = container.inventory.itemList[0];
        //        item.RemoveFromContainer();
        //        item.Remove(0f);
        //    }
        //}
        //private void OnEntitySpawned(BaseNetworkable entity)
        //{
        //    if (!UseLootMultyplier) { return; }
        //    if (entity is SupplyDrop) { return; }
        //    if (entity is LockedByEntCrate) { return; }
        //    if (entity is Stocking) { return; }
        //    if (entity is LootContainer)
        //    {
        //        var lootbox = entity as LootContainer;

        //        ClearContainer(lootbox);
        //        lootbox.PopulateLoot();

        //        foreach (Item lootitem in lootbox.inventory.itemList.ToList())
        //        {
        //            if (lootitem.info.stackable > 1)
        //            {
        //                if (BlacklistedLoot.Contains(lootitem.info.displayName.english) || BlacklistedLoot.Contains(lootitem.info.shortname)) continue;
        //                lootitem.amount = (int)(lootitem.amount * LootRate);
        //            }
        //        }
        //    }
        //}
        //private void UpdateLoot()
        //{
        //    foreach (var container in UnityEngine.Object.FindObjectsOfType<LootContainer>())
        //    {
        //        if (container is SupplyDrop) { continue; }
        //        if (container is LockedByEntCrate) { continue; }
        //        if (container is Stocking) { continue; }
        //        ClearContainer(container);
        //        container.PopulateLoot();
        //        foreach (Item lootitem in container.inventory.itemList.ToList())
        //        {
        //            if (lootitem.info.stackable > 1)
        //            {
        //                if (BlacklistedLoot.Contains(lootitem.info.displayName.english) || BlacklistedLoot.Contains(lootitem.info.shortname)) continue;
        //                lootitem.amount = (int)(lootitem.amount * LootRate);
        //            }
        //        }
        //    }
        //}
        //void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        //{
        //    var lootcont = entity as LootContainer;
        //    if (lootcont is SupplyDrop) return;
        //    var player = info.InitiatorPlayer;
        //    if (player == null) return;

        //    if (!lootcont || !lootcont.displayHealth) return;
        //    if (lootcont.OwnerID == player.userID) return;
        //    lootcont.OwnerID = player.userID;

        //    lootcont.SpawnLoot();

        //    double rate;
        //    if (isDay)
        //    {
        //        rate = GetUserRates(player.UserIDString, LootDayString);
        //    }
        //    else
        //    {
        //        rate = GetUserRates(player.UserIDString, LootNightString);
        //    }

        //    foreach (var lootitem in lootcont.inventory.itemList)
        //    {
        //        if (lootitem.info.stackable > 1)
        //        {
        //            if (BlacklistedLoot.Contains(lootitem.info.displayName.english) || BlacklistedLoot.Contains(lootitem.info.shortname)) continue;
        //            var new_amount = (int)(lootitem.amount * rate);
        //            lootitem.amount = new_amount > 1 ? new_amount : 1;
        //        }
        //    }
        //}
        void OnContainerDropItems(ItemContainer container)
        {
            if (container == null)
                return;
            
            var lootcont = container.entityOwner as LootContainer;
            if (lootcont == null || CratesCD.ContainsKey(lootcont.net.ID))
                return;
			
            var basePlayer = lootcont.lastAttacker as BasePlayer;
            var player = basePlayer != null ? basePlayer : null;
            if(player == null) return;
            double rate = GetUserRates(player.UserIDString, isDay ? LootDayString : LootNightString);

            UpdateLoot(lootcont, rate);
        }
        //TODO Пересоздавать контейнер вместо обновления лута
        void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (entity is SupplyDrop) return;
            if (entity is LockedByEntCrate) return;
            if (entity is Stocking) return;

            var container = entity as LootContainer;
            if (!container || container.net?.ID == null) return;
            EntityLootetrs entityLootetrs;
            var netId = container.net.ID;
            if (!CratesCD.TryGetValue(netId, out entityLootetrs))
            {
                entityLootetrs = new EntityLootetrs();
                CratesCD[netId] = entityLootetrs;
            }
            
            if(entityLootetrs.IsUpdateBlocked(player))
                return;
            
            entityLootetrs.AddLooter(player.userID);
            container.SpawnLoot();

            var rate = GetUserRates(player.UserIDString, isDay ? LootDayString : LootNightString);

            UpdateLoot(container, rate);
        }
        private void UpdateLoot(LootContainer container, double rate, ulong pIayerid = 0 )
        {
            //double rate = GetUserRates(player.UserIDString, isDay ? LootDayString : LootNightString);
            //double rate = GetUserRates(pIayerid, isDay ? LootDayString : LootNightString);
            if (container?.inventory?.itemList == null)
                return;
            foreach (var lootItem in container.inventory.itemList)
            {
                if (lootItem.info.stackable <= 1) continue;
                if (IsBlackList && ListedLootContains(lootItem)) continue;
                if (!IsBlackList && !ListedLootContains(lootItem)) continue;
                var newAmount = (int) (lootItem.amount * rate);
                lootItem.amount = newAmount > 1 ? newAmount : 1;
            }
        }

        private bool ListedLootContains(Item item) => ListedLoot.Contains(item.info.displayName.english) || ListedLoot.Contains(item.info.shortname);

        private void UpdateFurnaces()
        {
            foreach (var oven in BaseNetworkable.serverEntities.OfType<BaseOven>())
            {
                if (!oven.HasFlag(BaseEntity.Flags.On))
                    continue;
                double ovenMultiplier = GetUserRates(oven.OwnerID.ToString(), isDay ? SmeltDayString : SmeltNightString);
                if (ovenMultiplier > 10f) ovenMultiplier = 10f;
                if (ovenMultiplier < 0.1f) ovenMultiplier = 0.1f;
                StartCooking(oven, ovenMultiplier);
            }
        }
        float CookingTemperature(BaseOven.TemperatureType temperature)
        {
            switch (temperature)
            {
                case BaseOven.TemperatureType.Warming:
                    return 50f;
                case BaseOven.TemperatureType.Cooking:
                    return 200f;
                case BaseOven.TemperatureType.Smelting:
                    return 1000f;
                case BaseOven.TemperatureType.Fractioning:
                    return 1500f;
                default:
                    return 15f;
            }
        }
        object OnOvenToggle(BaseOven oven, BasePlayer player)
        {
            if (oven.HasFlag(BaseEntity.Flags.On))
                return null;
            double ovenMultiplier = GetUserRates(oven.OwnerID.ToString(), isDay ? SmeltDayString : SmeltNightString);
            if (ovenMultiplier > 10f) ovenMultiplier = 10f;
            if (ovenMultiplier < 0.1f) ovenMultiplier = 0.1f;
            StartCooking(oven, ovenMultiplier);
            return false;
        }
        void StartCooking(BaseOven oven, double ovenMultiplier)
        {
            if (FindBurnable(oven) == null)
                return;
            oven.inventory.temperature = CookingTemperature(oven.temperature);
            oven.UpdateAttachmentTemperature();
            oven.CancelInvoke(oven.Cook);
            oven.InvokeRepeating(oven.Cook, (float)(0.5f / ovenMultiplier), (float)(0.5f / ovenMultiplier));
            //InvokeHandler.InvokeRepeating(oven, oven.Cook, (float)(0.5f / ovenMultiplier), (float)(0.5f / ovenMultiplier));
            oven.SetFlag(BaseEntity.Flags.On, true);
        }
        Item FindBurnable(BaseOven oven)
        {
            if (oven.inventory == null)
                return null;
            foreach (Item current in oven.inventory.itemList)
            {
                ItemModBurnable component = current.info.GetComponent<ItemModBurnable>();
                if (component && (oven.fuelType == null || current.info == oven.fuelType))
                    return current;
            }
            return null;
        }

        private void OnDayStart()
        {
            CoalRate = CoalRateDay;
            CoalChance = CoalChanceDay;
            SmeltRate = DefaultRates[SmeltDayString];
            GatherRate = DefaultRates[GatherDayString];
            PickupRate = DefaultRates[PickUpDayString];
            QuerryRate = DefaultRates[QuerryDayString];
            LootRate = DefaultRates[LootDayString];
            //Обновляем скорость переплавки
            UpdateSmeltTime();
            UpdateFurnaces();

            //Оповещаем игоков о смене времени суток
            if (WarnChat)
            {
                RatesToChat();
            }
        }
        private void OnNightStart()
        {
            CoalRate = CoalRateNight;
            CoalChance = CoalChanceNight;
            SmeltRate = DefaultRates[SmeltNightString];
            GatherRate = DefaultRates[GatherNightString];
            PickupRate = DefaultRates[PickUpNightString];
            QuerryRate = DefaultRates[QuerryNightString];
            LootRate = DefaultRates[LootNightString];
            //Обновляем скорость переплавки
            UpdateSmeltTime();
            UpdateFurnaces();

            //Оповещаем игоков о смене времени суток
            if (WarnChat)
            {
                RatesToChat();
            }
        }
        private void RatesToChat()
        {
            string Message = string.Empty;
            Message = isDay ? GetMsg("Day Starts") : GetMsg("Night Starts");
            Message += GetMsg("GatherRates").Replace("{rates}", GatherRate.ToString());
            Message += GetMsg("PickupRates").Replace("{rates}", PickupRate.ToString());
            Message += GetMsg("QuerryRates").Replace("{rates}", QuerryRate.ToString());
            Message += GetMsg("Smelt Rate").Replace("{rates}", SmeltRate.ToString());
            Message += GetMsg("CoalRate").Replace("{rates}", CoalRate.ToString());
            if (UseLootMultyplier)
                Message += GetMsg("LootRate").Replace("{rates}", LootRate.ToString());
            SendToChat(Message);
        }


        void OnCollectiblePickup(Item item, BasePlayer player)
        {
            double mod = 1f;
            if (DefaultPickupRates.ContainsKey(item.info.displayName.english))
            {
                mod = DefaultPickupRates[item.info.displayName.english];
            }
            int new_amount;
            if (isDay)
            {
                new_amount = (int)(item.amount * GetUserRates(player.UserIDString, PickUpDayString) * mod);
                item.amount = new_amount > 1 ? new_amount : 1;
                return;
            }
            new_amount = (int)(item.amount * GetUserRates(player.UserIDString, PickUpNightString) * mod);
            item.amount = new_amount > 1 ? new_amount : 1;
        }
        void OnGrowableGathered(GrowableEntity plant, Item item, BasePlayer player, ulong playerid = 2952192)
        {
            double mod = 1f;
            if (DefaultPickupRates.ContainsKey(item.info.displayName.english))
            {
                mod = DefaultPickupRates[item.info.displayName.english];
            }
            var newAmount  = (int)(item.amount * GetUserRates(player.UserIDString, isDay ? PickUpDayString : PickUpNightString) * mod);
            var diff = newAmount - item.amount;
            if(diff > 0)
                player.Command("note.inv", item.info.itemid,diff,item.name,BaseEntity.GiveItemReason.ResourceHarvested);
            item.amount = newAmount > 1 ? newAmount : 1;
        }
        void OnEntityKill(BaseNetworkable entity)
        {
            var id = entity?.net?.ID;
            if (id.HasValue)
                CratesCD.Remove(id.Value);
            
        }
        void OnDispenserBonus(ResourceDispenser disp, BasePlayer player, Item item)
        {
            if (player == null) return;
            double mod = 1f;
            if (DefaultGatherRates.ContainsKey(item.info.displayName.english))
            {
                mod = DefaultGatherRates[item.info.displayName.english];
            }
            double rate = GetUserRates(player.UserIDString, isDay ? GatherDayString : GatherNightString);
            var newAmount = (int)(item.amount * rate * mod);
            item.amount = newAmount > 1 ? newAmount : 1;
        }
        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (!entity.ToPlayer()) return;
            double mod = 1f;
            if (DefaultGatherRates.ContainsKey(item.info.displayName.english))
            {
                mod = DefaultGatherRates[item.info.displayName.english];
            }
            double rate = GetUserRates(entity.ToPlayer().UserIDString, isDay ? GatherDayString : GatherNightString);
            var newAmount = (int)(item.amount * rate * mod);
            item.amount = newAmount > 1 ? newAmount : 1;

            if (!MoreHQM || dispenser.gatherType != ResourceDispenser.GatherType.Ore) return;
            bool HaveHQM = dispenser.finishBonus.Any(x => x.itemDef.shortname == "hq.metal.ore");
            var reply = 2883;
            if (!HaveHQM)
            {
                dispenser.finishBonus.Add(new ItemAmount(hqmo, 2f));
            }
        }
        void OnQuarryGather(MiningQuarry quarry, Item item)
        {
            double mod = 1f;
            if (DefaultQuerryRates.ContainsKey(item.info.displayName.english))
            {
                mod = DefaultQuerryRates[item.info.displayName.english];
            }
            int newAmount;
            if (isDay)
            {
                newAmount = (int)(item.amount * GetUserRates(quarry.OwnerID, QuerryDayString) * mod);
                item.amount = newAmount > 1 ? newAmount : 1;
                return;
            }
            newAmount = (int)(item.amount * GetUserRates(quarry.OwnerID, QuerryNightString) * mod);
            item.amount = newAmount > 1 ? newAmount : 1;
        }
        void OnFuelConsume(BaseOven oven, Item fuel, ItemModBurnable burnable)
        {
            if (oven == null) return;
            burnable.byproductAmount = (int)Math.Ceiling(CoalRate);
            burnable.byproductChance = (100 - CoalChance) / 100f;
            if (burnable.byproductChance == 0)
            {
                burnable.byproductChance = -1;
            }
        }
        void UpdateSmeltTime()
        {
            var itemDefinitions = ItemManager.GetItemDefinitions();
            foreach (var item in itemDefinitions)
            {
                var cookable = item.GetComponent<ItemModCookable>();
                if (cookable != null)
                {
                    if (DefaultSmeltRates.ContainsKey(item.displayName.english))
                    {
                        cookable.cookTime = (float)(DefaultSmeltRates[item.displayName.english] / SmeltRate);
                    }
                    else
                    {
                        DefaultSmeltRates.Add(item.displayName.english, cookable.cookTime);
                        cookable.cookTime = (float)(DefaultSmeltRates[item.displayName.english] / SmeltRate);
                    }
                }
            }
        }
        #endregion

        #region Сonsole commands⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠
        [ConsoleCommand("env.freeze")]
        void TimeFreeze(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            if (Frozen)
            {
                //Puts("The time is already frozen!");
                arg.ReplyWith("The time is already frozen!");
                return;
            }
            Frozen = true;
            ProgressTime = false;
            //Puts("The time was frozen.");
            arg.ReplyWith("The time was frozen.");
        }

        [ConsoleCommand("env.unfreeze")]
        void TimeUnFreeze(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            if (!Frozen)
            {
                arg.ReplyWith("The time is not frozen!");
                return;
            }
            Frozen = false;
            ProgressTime = true;
            arg.ReplyWith("The time was unfrozen.");
        }

        [ConsoleCommand("rates.show")]
        void ShowRates(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            if (arg.Args == null)
            {
                arg.ReplyWith("Usage rates.show steamid [type]");
                return;
            }
            var target = covalence.Players.FindPlayer(arg.Args[0]);
            if (target == null)
            {
                arg.ReplyWith("User not found or multiply user mathces");
                return;
            }
            if (arg.Args.Length >= 2)
            {
                arg.ReplyWith(GetUserRates(target.Id, arg.Args[1]).ToString());
                return;
            }
            string reply = $"User '{target.Name}' current rates:\n";
            foreach (var p in AvaliableMods)
            {
                reply += p + ": " + GetUserRates(target.Id, p).ToString() + " \n";
            }
            arg.ReplyWith(reply);
        }
        #endregion

        #region Chat commands
        [ChatCommand("rates")]
        private void ShowRatesChat(BasePlayer player, string command, string[] args)
        {
            string reply = GetMsg("PersonalRates", player.UserIDString);
            if (isDay)
            {
                reply += GetMsg("GatherRatesPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, GatherDayString).ToString());
                reply += GetMsg("PickUpRatesPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, PickUpDayString).ToString());
                reply += GetMsg("QueryPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, QuerryDayString).ToString());
                reply += GetMsg("SmeltPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, SmeltDayString).ToString());
                reply += GetMsg("LootPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, LootDayString).ToString());
                SendToChat(player, reply);
                return;
            }
            reply += GetMsg("GatherRatesPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, GatherNightString).ToString());
            reply += GetMsg("PickUpRatesPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, PickUpNightString).ToString());
            reply += GetMsg("QueryPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, QuerryNightString).ToString());
            reply += GetMsg("SmeltPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, SmeltNightString).ToString());
            reply += GetMsg("LootPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, LootNightString).ToString());
            SendToChat(player, reply);
        }
        #endregion

        #region Helpers
        private void GetConfig<T>(string Key, ref T var)
        {
            if (Config[Key] != null)
            {
                var = (T)Convert.ChangeType(Config[Key], typeof(T));
            }
            Config[Key] = var;
        }

        //Функция, отправляющая сообщение в чат конкретному пользователю, добавляет префикс
        private void SendToChat(BasePlayer Player, string Message)
        {
            PrintToChat(Player, "<color=" + PrefixColor + ">" + Prefix + "</color> " + Message);
        }

        //Перезгрузка функции отправки собщения в чат - отправляет сообщение всем пользователям
        private void SendToChat(string Message)
        {
            PrintToChat("<color=" + PrefixColor + ">" + Prefix + "</color> " + Message);
        }

        //Функция получения строки из языкового файла
        string GetMsg(string key, object userID = null) => lang.GetMessage(key, this, userID?.ToString());
        double GetUserRates(string steamId, string RateType)
        {
            /*
             * Из списка кастомных привелегий выбираем только те, на которые у игрока есть привелегия.
             * выбираем только сами привелегии, без названий. Только содерживое
             * Из них выбираем те, где есть нужный нам тип рейтов. И выбираем только нужные нам типы рейтов.
             */
            var playergroups = CustomRates.Where(i => permission.UserHasPermission(steamId, i.Key)).Select(i => i.Value).
                Where(i => i.ContainsKey(RateType)).Select(i => i[RateType]);
            return playergroups.Any() ? playergroups.Aggregate((i1, i2) => i1 > i2 ? i1 : i2) : DefaultRates[RateType];
        }
        //Перегрузка функции. Ибо мне так будет проще)
        double GetUserRates(ulong steamId, string RateType) => GetUserRates(steamId.ToString(), RateType);

        Dictionary<string, object> CreatePerms(List<string> mods, double rate)
        {
            return mods.ToDictionary(x => x, x => (object)rate);
        }


        #endregion
    }
}
//////////////////////////////////////////////////////////////////////////////////////////////////
