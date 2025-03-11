using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Oxide.Plugins.XDShopExtensionMethods;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Oxide.Plugins
{
    [Info("XDShop", "DezLife", "1.2.2")]
    [Description("Large easily customizable store")]
    internal class XDShop : RustPlugin
    {
        #region Var
        private static XDShop Instance;
        private static Coroutine ApiLoadImage;
        public static StringBuilder StringBuilderInstance;
        private const bool Ru = true;
        private readonly HashSet<string> _exclude = new HashSet<string>
        {
            "vehicle.chassis","vehicle.module", "tool.camera", "fishing.tackle", "blood", "wolfmeat.spoiled", "apple.spoiled", "humanmeat.spoiled","chicken.spoiled", "meat.pork.burned", "chicken.burned", "deermeat.burned", "wolfmeat.burned", "horsemeat.burned", "humanmeat.burned", "bearmeat.burned", "ammo.rocket.smoke", "blueprintbase", "captainslog", "minihelicopter.repair", "note", "photo", "scraptransportheli.repair", "spiderweb", "spookyspeaker", "habrepair", "door.key", "car.key", "bleach", "ducttape", "glue", "sticks", "skullspikes", "skull.trophy", "map", "battery.small", "coal", "can.beans.empty", "can.tuna.empty", "skull.human", "paper", "researchpaper", "water", "hazmatsuit_scientist_arctic", "attire.banditguard", "scientistsuit_heavy", "frankensteins.monster.01.head", "frankensteins.monster.01.legs", "frankensteins.monster.01.torso", "frankensteins.monster.02.legs", "frankensteins.monster.02.head", "frankensteins.monster.02.torso", "snowmobile", "snowmobiletomaha", "rowboat", "mlrs", "workcart", "submarinesolo", "submarineduo", "rhib", "vehicle.chassis.2mod", "vehicle.chassis.3mod", "vehicle.chassis.4mod", "electric.cabletunnel", "water.salt", "geiger.counter"
        };
        #endregion

        #region ReferencePlugins
        [PluginReference] private readonly Plugin ImageLibrary, Economics, ServerRewards, IQEconomic, Kits, IQKits, Battles, Duel, ItemCostCalculator, NoEscape;

        #region NoEscape
        private bool IsRaid(BasePlayer player)
        {
            if (NoEscape == null)
                return false;
            return (bool)NoEscape?.Call("IsBlocked", player);
        }
        #endregion

        #region Duel
        private bool IsDuel(BasePlayer player)
        {
            if (Battles)
                return (bool)Battles?.Call("IsPlayerOnBattle", player.userID);
            else if (Duel)
                return (bool)Duel?.Call("IsPlayerOnActiveDuel", player);
            else
                return false;
        }
        #endregion

        #region ImageLibrary
        private string GetImage(string fileName, ulong skin = 0)
        {
            string imageId = (string)plugins.Find("ImageLibrary").CallHook("GetImage", fileName, skin);
            if (!string.IsNullOrEmpty(imageId))
                return imageId;
            return string.Empty;
        }
        #endregion
        #endregion

        #region Types
        public enum EconomicsType
        {
            Economics,
            ServerRewards,
            IQEconomic,
            Item
        }
        public enum NotificationType
        {
            Error,
            Warning,
            Success
        }
        public enum ThemeType
        {
            Light,
            Dark
        }
        public enum ItemType
        {
            Item,
            Blueprint,
            CustomItem,
            Command,
            Kit
        }
        #endregion

        #region Configuration OLD       
        private static ConfigurationOld configOld = new ConfigurationOld();
        private class ConfigurationOld
        {
            [JsonProperty("Предметы")]
            public Dictionary<string, List<ItemStores>> itemstores;
            internal class ItemStores
            {
                [JsonProperty("Тип предмета(0 - Предмет, 1 - Чертёж, 2 - кастомный предмет, 3 - Команда)")]
                public ItemType type;
                [JsonProperty("Shortame")]
                public string ShortName;
                [JsonProperty("Цена")]
                public int Price;
                [JsonProperty("Количество при покупке")]
                public int Amount;
                [JsonProperty("Кастом имя предмета (Использовать с типом предмета 2 и 3)")]
                public string Name;
                [JsonProperty("SkinID предмета (Использовать с типом предмета 2)")]
                public ulong SkinID;
                [JsonProperty("Команда(Использовать с типом предмета 3)")]
                public string Command;
                [JsonProperty("URL картинки (Использовать с типом предмета 3)")]
                public string Url;
            }
        }
        #endregion

        #region Configuration
        private Configuration config;
        private class Configuration
        {
            [JsonProperty(Ru ? "Основные настройки" : "Basic Settings")]
            public MainSettings mainSettings = new MainSettings();
            [JsonProperty(Ru ? "Настройка экономики" : "Economics")]
            public EconomicsCustomization economicsCustomization = new EconomicsCustomization();
            [JsonProperty(Ru ? "Настройка Human NPC" : "Setting up a Human NPC")]
            public HumanNpcs humanNpcs = new HumanNpcs();
            [JsonProperty(Ru ? "Скидки по пермешенам" : "Discounts on permissions")]
            public DiscountStores discountStores = new DiscountStores();
            [JsonProperty(Ru ? "Настройка интерфейса" : "Configuring the interface")]
            public InterfaceSettings interfaceSettings = new InterfaceSettings();
            [JsonProperty(Ru ? "Настройка категорий и товаров" : "Setting up categories and products")]
            public List<CategoryShop> product = new List<CategoryShop>();
            internal class MainSettings
            {
                [JsonProperty(Ru ? "Команды для открытия магазина (чат)" : "Commands for opening a store (chat)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public string[] commands = { "shop" };

                [JsonProperty(Ru ? "Запрещать покупать/продавать во время рейд/комбат блока?" : "Prohibit buying/selling during a raid/combat block?")]
                public bool raidBlock = true;

                [JsonProperty(Ru ? "Разрешения для использования шопа (Оставьте пустым если хотите дать доступ всем игрокам)(пример xdshop.use)" : "Permissions to use the shop (Leave empty if you want to give access to all players)(example xdshop.use)")]
                public string permissionUseShop = "";
            }
            internal class InterfaceSettings
            {
                [JsonProperty(Ru ? "Включить возможность менять тему?" : "Enable the ability to change the theme?")]
                public bool useChangeTheme = true;
                [JsonProperty(Ru ? "Тема по умолчанию (0 - светлая, 1 - темная)" : "Default theme (0 - light, 1 - dark)")]
                public ThemeType themeTypeDefault = ThemeType.Dark;

                [JsonProperty(Ru ? "Настройки светлой темы UI" : "Light Theme UI Settings")]
                public ThemeCustomization lightTheme = new ThemeCustomization
                {
                    colorMainBG = "1 1 1 1",
                    colorTextTitle = "0 0 0 1",
                    colorImgTitle = "0 0 0 1",
                    colorImgDiscount = "0.20 0.85 0.15 1.00",
                    colorTextDiscount = "0 0 0 1",
                    colorImgBalance = "0.26 0.53 0.80 1",
                    colorTextBalance = "0 0 0 1",
                    colortext1 = "0 0 0 1",
                    color1 = "0.51 0.51 0.51 1.00",
                    colortext2 = "0.627451 0.6313726 0.6392157 1",
                    colortext3 = "0 0 0 1",
                    colortext6 = "0.97 0.97 0.98 1.00",
                    colortext7 = "0 0 0 1",
                    colortext8 = "0.38 0.77 0.43 1.00",
                    colortext9 = "0.8588235 0.345098 0.3372549 1",
                    color2 = "0.5607843 0.8901961 0.4705883 0.6",
                    color3 = "0.4078432 0.4313726 0.4392157 0.6",
                    colortext10 = "0 0 0 1",
                    colortext4 = "0.51 0.51 0.51 1.00",
                    colortext5 = "0.55 0.55 0.55 1.00",
                    closeBtnColor = "0.8392158 0.3647059 0.3568628 1",
                    closeBtnColor2 = "0.8392158 0.3647059 0.3568628 1"
                };
                [JsonProperty(Ru ? "Настройки темной темы UI" : "Dark Theme UI Settings")]
                public ThemeCustomization darkTheme = new ThemeCustomization
                {
                    colorMainBG = "0.13 0.15 0.16 1.00",
                    colorTextTitle = "0.87 0.87 0.87 1.00",
                    colorImgTitle = "0.62 0.63 0.64 1.00",
                    colorImgDiscount = "0.20 0.85 0.15 1.00",
                    colorTextDiscount = "1 1 1 1",
                    colorImgBalance = "0.26 0.53 0.80 1",
                    colorTextBalance = "1 1 1 1",
                    colortext1 = "0.87 0.87 0.87 1.00",
                    color1 = "0.51 0.51 0.51 1.00",
                    colortext2 = "0.627451 0.6313726 0.6392157 1",
                    colortext3 = "0.87 0.87 0.87 1.00",
                    colortext6 = "0.17 0.18 0.21 1.00",
                    colortext7 = "0.87 0.87 0.87 1.00",
                    colortext8 = "0.38 0.77 0.43 1.00",
                    colortext9 = "0.8588235 0.345098 0.3372549 1",
                    color2 = "0.5607843 0.8901961 0.4705883 0.6",
                    color3 = "0.4078432 0.4313726 0.4392157 0.6",
                    colortext10 = "1 1 1 1",
                    colortext4 = "0.51 0.51 0.51 1.00",
                    colortext5 = "0.55 0.55 0.55 1.00",
                    closeBtnColor = "0.8392158 0.3647059 0.3568628 1",
                    closeBtnColor2 = "0.8392158 0.3647059 0.3568628 1",
                };

                internal class ThemeCustomization
                {
                    [JsonProperty(Ru ? "Цвет основного фона магазина" : "The color of the main background of the store")]
                    public string colorMainBG = "1 1 1 1";
                    [JsonProperty(Ru ? "[TITLE] Цвет текста" : "[TITLE] Text color")]
                    public string colorTextTitle = "0 0 0 1";
                    [JsonProperty(Ru ? "[TITLE] Цвет картинки" : "[TITLE] Picture Color")]
                    public string colorImgTitle = "0 0 0 1";
                    [JsonProperty(Ru ? "Цвет картинки скидки" : "Color of the discount picture")]
                    public string colorImgDiscount = "0.20 0.85 0.15 1.00";
                    [JsonProperty(Ru ? "Цвет текста скидки" : "Discount text color")]
                    public string colorTextDiscount = "0 0 0 1";
                    [JsonProperty(Ru ? "Цвет картинки баланса" : "Balance picture color")]
                    public string colorImgBalance = "0.26 0.53 0.80 1";
                    [JsonProperty(Ru ? "Цвет текста баланса" : "Balance text color")]
                    public string colorTextBalance = "0 0 0 1";
                    [JsonProperty(Ru ? "[PAGE] Цвет текста номера страниц" : "[PAGE] Page number text color")]
                    public string colortext1 = "0 0 0 1";
                    [JsonProperty(Ru ? "[PAGE] Цвет кнопок переключения страниц" : "[PAGE] Color of the page switching buttons")]
                    public string color1 = "0.51 0.51 0.51 1.00";
                    [JsonProperty(Ru ? "[PRODUCT] Цвет количества предметов (цифры)" : "[PRODUCT] color of the number of items (digits)")]
                    public string colortext2 = "0.627451 0.6313726 0.6392157 1";
                    [JsonProperty(Ru ? "[PRODUCT] Цвет фона товара" : "[PRODUCT] product background color")]
                    public string colortext6 = "0.97 0.97 0.98 1.00";
                    [JsonProperty(Ru ? "[PRODUCT] Цвет текста названия товара" : "[PRODUCT] Text color of the product name")]
                    public string colortext7 = "0 0 0 1";
                    [JsonProperty(Ru ? "[PRODUCT] Цвет кнопки купить" : "[PRODUCT] Buy button color")]
                    public string colortext8 = "0.38 0.77 0.43 1.00";
                    [JsonProperty(Ru ? "[PRODUCT] Цвет кнопки продать" : "[PRODUCT] Color of the sell button")]
                    public string colortext9 = "0.8588235 0.345098 0.3372549 1";
                    [JsonProperty(Ru ? "[PRODUCT] Цвет кнопки закрыть" : "[PRODUCT] Close button color")]
                    public string closeBtnColor2 = "0.8392158 0.3647059 0.3568628 1";
                    [JsonProperty(Ru ? "[CATEGORY] цвет названия активной категории" : "[CATEGORY] color of the name of the active category")]
                    public string colortext3 = "0 0 0 1";
                    [JsonProperty(Ru ? "[CATEGORY] Цвет названия неактивной категорий" : "[CATEGORY] Color of the inactive category name")]
                    public string colortext4 = "0.51 0.51 0.51 1.00";
                    [JsonProperty(Ru ? "[CATEGORY] Цвет полосы активной категории" : "[CATEGORY] Band color of the active category")]
                    public string color2 = "0.5607843 0.8901961 0.4705883 0.6";
                    [JsonProperty(Ru ? "[CATEGORY] Цвет полосы неактивной категории" : "[CATEGORY] Color of the inactive category stripe")]
                    public string color3 = "0.4078432 0.4313726 0.4392157 0.6";
                    [JsonProperty(Ru ? "[NOTIFICATIONS] Цвет текста в уведомлении" : "[NOTIFICATIONS] The color of the text in the notification")]
                    public string colortext10 = "0 0 0 1";
                    [JsonProperty(Ru ? "Цвет дополнительного серого текста" : "Color of additional gray text")]
                    public string colortext5 = "0.55 0.55 0.55 1.00";
                    [JsonProperty(Ru ? "Цвет кнопки выход из UI" : "The color of the exit UI button")]
                    public string closeBtnColor = "0.8392158 0.3647059 0.3568628 1";
                }
            }

            internal class HumanNpcs
            {
                [JsonProperty(Ru ? "Включить поддержку Human NPC ?" : "Enable Human NPC support ?")]
                public bool useHumanNpcs = false;
                [JsonProperty(PropertyName = "Human NPC  [NPC ID | list shop category]")]
                public Dictionary<string, List<string>> NPCs = new Dictionary<string, List<string>>();
            }
            internal class DiscountStores
            {
                [JsonProperty(Ru ? "Пермешен/Скидка в %" : "Permissions/Discount in %", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public Dictionary<string, float> DiscountPerm = new Dictionary<string, float>
                {
                    ["XDShop.vip"] = 10.0f
                };
            }
            internal class EconomicsCustomization
            {
                [JsonProperty(Ru ? "Экономика (0 - Economics, 1 - ServerRewards, 2 - IQEconomic, 3 - Item)" : "Economics (0 - Economics, 1 - ServerRewards, 2 - IQEconomic, 3 - Item)")]
                public EconomicsType typeEconomic = EconomicsType.IQEconomic;
                [JsonProperty(Ru ? "Приставка к балансу (например RP или $ - Не более 2 символов)" : "Prefix to the balance (for example, RP or $ - No more than 2 characters)")]
                public string prefixBalance = "$";
                [JsonProperty(Ru ? "ShortName предмета (Использовать с типом 3)" : "Item shortname (Use with type 3)")]
                public string economicShortname = "";
                [JsonProperty(Ru ? "SkinId предмета (Использовать с типом 3)" : "Item SkinId (Use with Type 3)")]
                public ulong economicSkinId = 0;

            }
            internal class CategoryShop
            {
                [JsonProperty(Ru ? "Названия категории" : "Category names")]
                public string CategoryName;
                [JsonProperty(Ru ? "Разрешения для доступа к категории" : "Permissions to access the category")]
                public string PermissionCategory;
                [JsonProperty(Ru ? "Список товаров в данной категории" : "List of products in this category")]
                public List<Product> product = new List<Product>();
                internal class Product
                {
                    [JsonProperty(Ru ? "Тип предмета (0 - Предмет, 1 - Чертёж, 2 - Кастомный предмет, 3 - Команда, 4 - Кит)" : "Item Type (0 - Item, 1 - BluePrint, 2 - Custom item, 3 - Commands, 4 - Kit)")]
                    public ItemType type;
                    [JsonProperty(Ru ? "Уникальный ID (НЕ ТРОГАТЬ)" : "Unique ID (DO NOT TOUCH)")]
                    public int ID = 0;
                    [JsonProperty(Ru ? "Shortame" : "Shortame")]
                    public string ShortName;
                    [JsonProperty(Ru ? "Описания" : "Descriptions")]
                    public string Descriptions;
                    [JsonProperty(Ru ? "Цена" : "Price")]
                    public float Price;
                    [JsonProperty(Ru ? "Цена продажи (Если не нужно то оставьте 0)" : "Sale price (If not necessary, leave 0)")]
                    public float PriceSales;
                    [JsonProperty(Ru ? "Количество" : "Quantity")]
                    public int Amount;
                    [JsonProperty(Ru ? "Количество для продажи" : "Quantity for sale")]
                    public int AmountSales;
                    [JsonProperty(Ru ? "Кастом имя предмета (Использовать с типом предмета 2 , 3 и 4)" : "Custom item name (Use with item type 2, 3 and 4)")]
                    public string Name;
                    [JsonProperty(Ru ? "SkinID предмета (Использовать с типом предмета 2)" : "Item SkinID (Use with item type 2)")]
                    public ulong SkinID;
                    [JsonProperty(Ru ? "Команды (Использовать с типом предмета 3)" : "Commands (Use with item type 3)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                    public List<string> Commands = new List<string>();
                    [JsonProperty(Ru ? "URL картинки" : "Image URL")]
                    public string Url;
                    [JsonProperty(Ru ? "Кит (Kits - kit name. IQKits - kit key)" : "Kits (Kits - kit name. IQKits - kit key)")]
                    public string KitName;
                    [JsonProperty(Ru ? "Задержка покупки (0 - неограниченно)" : "Purchase delay (0 - unlimited)")]
                    public float BuyCooldown;
                    [JsonProperty(Ru ? "Задержка продажи (0 - неограниченно)" : "Sale delay (0 - unlimited)")]
                    public float SellCooldown;
                    [JsonProperty(Ru ? "Максимальное количество лотов за 1 покупку (Максимум 99)" : "Maximum number of lots for 1 purchase (Maximum 99)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                    public Dictionary<string, int> BuyLimits = new Dictionary<string, int>();
                    [JsonProperty(Ru ? "Максимальное количество лотов за 1 продажу (Максимум 99)" : "Maximum number of lots for 1 sale (Maximum 99)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                    public Dictionary<string, int> SellLimits = new Dictionary<string, int>();
                    [JsonProperty(Ru ? "Максимальное количество покупаемых лотов за вайп (0 - неограниченно)" : "The maximum number of purchased lots per wipe (0 - unlimited)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                    public Dictionary<string, int> BuyLimitsWipe = new Dictionary<string, int>();
                    [JsonProperty(Ru ? "Максимальное количество продаваемых лотов за вайп (0 - неограниченно)" : "The maximum number of lots sold per wipe (0 - unlimited)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                    public Dictionary<string, int> SellLimitsWipe = new Dictionary<string, int>();
                    [JsonIgnore]
                    public ItemDefinition Definition;
                    [JsonIgnore]
                    public bool ItemsErorred = false;

                    public string GetProductName(string UserId)
                    {
                        string userLang = Instance.lang.GetLanguage(UserId);

                        if (!string.IsNullOrWhiteSpace(Name))
                            return Name;

                        if (!string.IsNullOrWhiteSpace(ShortName) && Instance.ItemName.ContainsKey(ShortName))
                            return userLang == "ru" ? Instance.ItemName[ShortName].ru : Instance.ItemName[ShortName].en;

                        return string.Empty;
                    }
                    public string GetProductDescription()
                    {
                        if (!string.IsNullOrWhiteSpace(Descriptions))
                            return Descriptions;
                        if (!string.IsNullOrWhiteSpace(ShortName) && Instance.ItemName.ContainsKey(ShortName) && !string.IsNullOrWhiteSpace(Instance.ItemName[ShortName].description))
                            return Instance.ItemName[ShortName].description;

                        return string.Empty;
                    }
                    public int GetLimitLot(BasePlayer player, bool purchase)
                    {
                        Dictionary<string, int> dict = purchase ? BuyLimits : SellLimits;
                        int Limit = 0;

                        if (dict.Count == 0)
                            return Limit;

                        foreach (KeyValuePair<string, int> Discount in dict)
                            if (Instance.permission.UserHasPermission(player.UserIDString, Discount.Key))
                                if (Limit < Discount.Value)
                                    Limit = Discount.Value;
                        return Limit;
                    }

                    public int GetLimitLotWipe(BasePlayer player, bool purchase)
                    {
                        Dictionary<string, int> dict = purchase ? BuyLimitsWipe : SellLimitsWipe;
                        int Limit = 0;
                        if (dict.Count == 0)
                            return Limit;

                        foreach (KeyValuePair<string, int> Discount in dict)
                            if (Instance.permission.UserHasPermission(player.UserIDString, Discount.Key))
                                if (Limit < Discount.Value)
                                    Limit = Discount.Value;
                        return Limit;
                    }
                }
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                    throw new Exception();
                SaveConfig();
            }
            catch
            {
                for (int i = 0; i < 3; i++)
                    PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                LoadDefaultConfig();
            }
            ValidateConfig();
            CheckOnDuplicates();
            SaveConfig();
        }
        private void ValidateConfig()
        {
            foreach (Configuration.CategoryShop category in config.product)
            {
                foreach (Configuration.CategoryShop.Product product in category.product)
                {
                    foreach (KeyValuePair<string, int> BuyLimits in product.BuyLimits)
                        if (!permission.PermissionExists(BuyLimits.Key))
                            permission.RegisterPermission(BuyLimits.Key, this);
                    foreach (KeyValuePair<string, int> SellLimits in product.SellLimits)
                        if (!permission.PermissionExists(SellLimits.Key))
                            permission.RegisterPermission(SellLimits.Key, this);
                    foreach (KeyValuePair<string, int> BuyLimitsWipe in product.BuyLimitsWipe)
                        if (!permission.PermissionExists(BuyLimitsWipe.Key))
                            permission.RegisterPermission(BuyLimitsWipe.Key, this);
                    foreach (KeyValuePair<string, int> SellLimitsWipe in product.SellLimitsWipe)
                        if (!permission.PermissionExists(SellLimitsWipe.Key))
                            permission.RegisterPermission(SellLimitsWipe.Key, this);
                }
                if (!permission.PermissionExists(category.PermissionCategory))
                    permission.RegisterPermission(category.PermissionCategory, this);
            }
        }

        private void CheckingProducts()
        {
            foreach (Configuration.CategoryShop category in config.product)
            {
                foreach (Configuration.CategoryShop.Product product in category.product)
                {
                    if (product.ID == 0)
                        product.ID = Core.Random.Range(int.MinValue, int.MaxValue);

                    if (product.type == ItemType.Item || product.type == ItemType.Blueprint || product.type == ItemType.CustomItem)
                    {
                        product.Definition = ItemManager.FindItemDefinition(product.ShortName);
                        if (product.Definition == null)
                        {
                            product.ItemsErorred = true;
                            PrintError(GetLang("XDSHOP_SERVICE_CONFIG_1", args: product.ID));
                            continue;
                        }
                    }
                    if (product.Price <= 0)
                    {
                        product.ItemsErorred = true;
                        PrintError(GetLang("XDSHOP_SERVICE_CONFIG_2", args: product.ID));
                        continue;
                    }
                }
            }
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
        }
        #endregion

        #region Lang
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["XDSHOP_UI_TITLE"] = "Каталог товаров",
                ["XDSHOP_UI_BTN_BUY"] = "Купить",
                ["XDSHOP_UI_BTN_SALLE"] = "Продать",
                ["XDSHOP_UI_PRODUCT_INFO"] = "Цена покупки: {0}\nКоличество: {1}\nВаш лимит: {2}",
                ["XDSHOP_UI_PRODUCT_INFO_EXIT"] = "ЗАКРЫТЬ",
                ["XDSHOP_UI_PRODUCT_SELL_INFO"] = "Цена продажи: {0}\nКоличество : {1}\nВаш лимит: {2}",
                ["XDSHOP_UI_NOTIFICATION_IS_DUEL"] = "Вы не можете приобрести предмет во время дуэли!",
                ["XDSHOP_UI_NOTIFICATION_IS_RAID"] = "Вы не можете приобрести предмет во время рейд/комбат блока!",
                ["XDSHOP_UI_NOTIFICATION_NOT_ENOUGH_SPACE"] = "Недостаточно места в инвентаре",
                ["XDSHOP_UI_NOTIFICATION_INSUFFICIENT_FUNDS"] = "У вас недостаточно средств для данной покупки",
                ["XDSHOP_UI_NOTIFICATION_BUY_RECHARGE"] = "Вы не можете приобрести '{0}'. Вам нужно подождать еще {1}",
                ["XDSHOP_UI_NOTIFICATION_BUY_LIMIT"] = "Вы больше не можете приобрести '{0}'. Вы превысили лимит за WIPE",
                ["XDSHOP_UI_NOTIFICATION_BUY_LIMIT_1"] = "Вы не можете приобрести '{0}' в таком количестве. Вы можете купить еще {1} лот(ов)",
                ["XDSHOP_UI_NOTIFICATION_SUCCESSFUL_PURCHASE"] = "Вы успешно приобрели {0}",
                ["XDSHOP_UI_NOTIFICATION_SELL_IS_DUEL"] = "Вы не можете продать предмет во время дуэли!",
                ["XDSHOP_UI_NOTIFICATION_SELL_IS_RAID"] = "Вы не можете продать предмет во время рейд/комбат блока!",
                ["XDSHOP_UI_NOTIFICATION_NOT_ENOUGH_ITEM"] = "Недостаточно предмета для продажи",
                ["XDSHOP_UI_NOTIFICATION_SELL_RECHARGE"] = "Вы не можете продать '{0}'. Вам нужно подождать еще {1}",
                ["XDSHOP_UI_NOTIFICATION_SELL_LIMIT"] = "Вы больше не можете продать '{0}'. Вы превысили лимит продаж за WIPE",
                ["XDSHOP_UI_NOTIFICATION_SELL_LIMIT_1"] = "Вы не можете продать '{0}' в таком количестве. Вы можете продать еще {1} лот(ов)",
                ["XDSHOP_UI_NOTIFICATION_SUCCESSFUL_SALE"] = "Вы успешно продали {0}",
                ["XDSHOP_SERVICE_EXIST_ECONOMICS"] = "У вас отсутствует выбранная экономика. Пожалуйста! Проверьте настройки экономики в конфигурации",
                ["XDSHOP_SERVICE_CONFIG_1"] = "В товаре с ID '{0}' присутствует ошибка. Товар будет скрыт, проверьте правильно ли вы указали ShortName!",
                ["XDSHOP_SERVICE_CONFIG_2"] = "В товаре с ID '{0}' присутствует ошибка. Товар будет скрыт, у товара отсутствует или неверная цена!",
                ["XDSHOP_SERVICE_CMD_REFILL"] = "Выполнения данной команды повлечет за собой изменения конфигурации товаров и категорий. Убедитесь что вы сохранили конфигурацию перед данной операцией. Вы уверены, что хотите продолжить? (xdshop.yes или xdshop.no)",
                ["XDSHOP_SERVICE_CMD_REFILL_YES"] = "Происходит перезаполнение категорий и товаров. Пожалуйста, ожидайте...",
                ["XDSHOP_SERVICE_CMD_REFILL_NO"] = "Действие успешно отменено.",
                ["XDSHOP_SERVICE_CMD_REFILL_SUCCESSFULLY"] = "Заполнения магазина товарами прошло успешно. Перезагружаем плагин для более корректной работы.",
                ["XDSHOP_SERVICE_CMD_LOCK_CATEGORY"] = "Данная категория не доступна для вас!",
                ["XDSHOP_SERVICE_UPDATE_PLUGINS"] = "Плагин работает некорректно! Обновите плагин до последней версии.",
                ["XDSHOP_SERVICE_1"] = "Что то пошло не так. Кажется страрая конфигурация сломана. Обратитесь к разработчику!",
                ["XDSHOP_SERVICE_2"] = "В старой конфигурации отсутсвуют товары!",
                ["XDSHOP_SERVICE_3"] = "Перестраиваем конфигурацию...",
                ["XDSHOP_SERVICE_4"] = "Список товаров успешно перестроен! Перезагружаем плагин...",
                ["XDSHOP_SERVICE_5"] = "У вас недостаточно прав для использования данной команды!",
                ["XDSHOP_SERVICE_6"] = "В данный момент магазин пуст.",
                ["XDSHOP_UI_PATTERN"] = "Отсутствует",
            }, this, "ru");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["XDSHOP_UI_TITLE"] = "Catalog",
                ["XDSHOP_UI_BTN_BUY"] = "Buy",
                ["XDSHOP_UI_BTN_SALLE"] = "Sell",
                ["XDSHOP_UI_PRODUCT_INFO"] = "Purchase Price: {0}\nQuantity: {1}\nYour limit : {2}",
                ["XDSHOP_UI_PRODUCT_INFO_EXIT"] = "CLOSE",
                ["XDSHOP_UI_PRODUCT_SELL_INFO"] = "Selling Price: {0}\nQuantity: {1}\nYour limit : {2}",
                ["XDSHOP_UI_NOTIFICATION_IS_DUEL"] = "You cannot purchase an item during a duel!",
                ["XDSHOP_UI_NOTIFICATION_IS_RAID"] = "You cannot purchase an item during a raid/combat block!",
                ["XDSHOP_UI_NOTIFICATION_NOT_ENOUGH_SPACE"] = "Insufficient inventory space",
                ["XDSHOP_UI_NOTIFICATION_INSUFFICIENT_FUNDS"] = "You do not have enough funds for this purchase",
                ["XDSHOP_UI_NOTIFICATION_BUY_RECHARGE"] = "You cannot purchase '{0}'. You need to wait some more {1}",
                ["XDSHOP_UI_NOTIFICATION_BUY_LIMIT"] = "You can no longer purchase '{0}'. You have exceeded the limit for WIPE",
                ["XDSHOP_UI_NOTIFICATION_BUY_LIMIT_1"] = "You cannot purchase '{0}' in such quantity. You can buy more {1} lot(s)",
                ["XDSHOP_UI_NOTIFICATION_SUCCESSFUL_PURCHASE"] = "You have successfully purchased {0}",
                ["XDSHOP_UI_NOTIFICATION_SELL_IS_DUEL"] = "You cannot sell an item during a duel!",
                ["XDSHOP_UI_NOTIFICATION_SELL_IS_RAID"] = "You cannot sell an item during a raid/combat block!",
                ["XDSHOP_UI_NOTIFICATION_NOT_ENOUGH_ITEM"] = "Not enough item to sell",
                ["XDSHOP_UI_NOTIFICATION_SELL_RECHARGE"] = "You cannot sell '{0}'. You need to wait some more {1}",
                ["XDSHOP_UI_NOTIFICATION_SELL_LIMIT"] = "You can no longer sell '{0}'. You have exceeded the sales limit for WIPE",
                ["XDSHOP_UI_NOTIFICATION_SELL_LIMIT_1"] = "You cannot sell '{0}' in such quantity. you can sell more {1} lot(s)",
                ["XDSHOP_UI_NOTIFICATION_SUCCESSFUL_SALE"] = "You have successfully sold {0}",
                ["XDSHOP_SERVICE_EXIST_ECONOMICS"] = "You are missing the selected economics. Please! Check the economics settings in the configuration",
                ["XDSHOP_SERVICE_CONFIG_1"] = "Enter product with ID '{0}' There is an error. The product will be hidden, check if you have entered the Short Name correctly!",
                ["XDSHOP_SERVICE_CONFIG_2"] = "Enter product with ID '{0}' There is an error. The product will be hidden, the product ID is missing or the price is wrong!",
                ["XDSHOP_SERVICE_CMD_REFILL"] = "Execution of this command will entail changes in the configuration of products and categories. Make sure you save the configuration before this operation. Are you sure you want to continue? (xdshop.yes or xdshop.no)",
                ["XDSHOP_SERVICE_CMD_REFILL_YES"] = "The categories and products are refilled. Please wait...",
                ["XDSHOP_SERVICE_CMD_REFILL_NO"] = "Action canceled successfully.",
                ["XDSHOP_SERVICE_CMD_REFILL_SUCCESSFULLY"] = "Filling the store with goods was successful. Reload the plugin for more correct work.",
                ["XDSHOP_SERVICE_CMD_LOCK_CATEGORY"] = "This category is not available to you!",
                ["XDSHOP_SERVICE_UPDATE_PLUGINS"] = "The plugin is not working correctly! Update the plugin to the latest version.",
                ["XDSHOP_SERVICE_1"] = "Something went wrong. It seems the old configuration is broken. Contact the developer!",
                ["XDSHOP_SERVICE_2"] = "There are no products in the old configuration!",
                ["XDSHOP_SERVICE_3"] = "Rebuilding the configuration...",
                ["XDSHOP_SERVICE_4"] = "The list of products has been successfully rebuilt! Reloading the plugin...",
                ["XDSHOP_SERVICE_5"] = "You don't have enough rights to use this command!",
                ["XDSHOP_SERVICE_6"] = "В данный момент магазин пуст.",
                ["XDSHOP_UI_PATTERN"] = "Absent",
            }, this);
        }
        public string GetLang(string LangKey, string userID = null, params object[] args)
        {
            StringBuilderInstance.Clear();
            if (args != null)
            {
                StringBuilderInstance.AppendFormat(lang.GetMessage(LangKey, this, userID), args);
                return StringBuilderInstance.ToString();
            }
            return lang.GetMessage(LangKey, this, userID);
        }
        #endregion

        #region HumanNPC
        private void SubAndUnSubHumanNpcHook(bool sub = false)
        {
            if (!sub)
            {
                Unsubscribe(nameof(OnUseNPC));
                Unsubscribe(nameof(OnLeaveNPC));
                Unsubscribe(nameof(OnKillNPC));
            }
            else
            {
                Subscribe(nameof(OnUseNPC));
                Subscribe(nameof(OnLeaveNPC));
                Subscribe(nameof(OnKillNPC));
            }
        }

        private void OnUseNPC(BasePlayer npc, BasePlayer player)
        {
            if (config.humanNpcs.NPCs.ContainsKey(npc.UserIDString) && !humanNpcPlayerOpen.ContainsKey(player.userID))
            {
                humanNpcPlayerOpen.Add(player.userID, npc.UserIDString);
                XDShopUI(player);
            }
        }

        private void OnLeaveNPC(BasePlayer npc, BasePlayer player)
        {
            if (config.humanNpcs.NPCs.ContainsKey(npc.UserIDString))
            {
                if (humanNpcPlayerOpen.ContainsKey(player.userID))
                {
                    humanNpcPlayerOpen.Remove(player.userID);
                    if (uiPlayerUseNow.Contains(player.userID))
                        uiPlayerUseNow.Remove(player.userID);
                    CuiHelper.DestroyUi(player, "NOTIFICATION_POLOSA");
                    CuiHelper.DestroyUi(player, "NOTIFICATION_IMG");
                    CuiHelper.DestroyUi(player, "NOTIFICATION_TEXT");
                    CuiHelper.DestroyUi(player, NOTIFICATION_MAIN);
                    CuiHelper.DestroyUi(player, MAIN_LAYER);
                }
            }
        }

        private void OnKillNPC(BasePlayer npc, BasePlayer player)
        {
            if (config.humanNpcs.NPCs.ContainsKey(npc.UserIDString))
            {
                foreach (KeyValuePair<ulong, string> item in humanNpcPlayerOpen.Where(x => x.Value == npc.UserIDString))
                {
                    if (humanNpcPlayerOpen.ContainsKey(item.Key))
                    {
                        humanNpcPlayerOpen.Remove(item.Key);
                        if (uiPlayerUseNow.Contains(player.userID))
                            uiPlayerUseNow.Remove(player.userID);
                        CuiHelper.DestroyUi(player, "NOTIFICATION_POLOSA");
                        CuiHelper.DestroyUi(player, "NOTIFICATION_IMG");
                        CuiHelper.DestroyUi(player, "NOTIFICATION_TEXT");
                        CuiHelper.DestroyUi(player, NOTIFICATION_MAIN);
                        CuiHelper.DestroyUi(player, MAIN_LAYER);
                    }
                }
            }
        }

        #endregion

        #region Hooks
        private void Unload()
        {
            SaveLimits();
            SaveCooldowns();
            SavePlayerData();
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, MAIN_LAYER);
            ImageUi.Unload();
            if (ApiLoadImage != null)
            {
                ServerMgr.Instance.StopCoroutine(ApiLoadImage);
                ApiLoadImage = null;
            }
            StringBuilderInstance = null;
            Instance = null;
        }
        private void Init()
        {
            Instance = this;
            SubAndUnSubHumanNpcHook();
            LoadData();

            foreach (KeyValuePair<string, float> discount in config.discountStores.DiscountPerm)
                permission.RegisterPermission(discount.Key, this);
            foreach (string command in config.mainSettings.commands)
                cmd.AddChatCommand(command, this, nameof(XDShopUI));
        }
        private void OnServerInitialized()
        {
            StringBuilderInstance = new StringBuilder();
            if (!ExistEconomics())
            {
                NextTick(() =>
                {
                    PrintError(GetLang("XDSHOP_SERVICE_EXIST_ECONOMICS"));
                    Interface.Oxide.UnloadPlugin(Name);
                });
                return;
            }
            AddDisplayName();
            ImageUi.DownloadImages();
            ApiLoadImage = ServerMgr.Instance.StartCoroutine(DownloadImages());
            CheckingProducts();
            if (!string.IsNullOrWhiteSpace(config.mainSettings.permissionUseShop) && !permission.PermissionExists(config.mainSettings.permissionUseShop))
                permission.RegisterPermission(config.mainSettings.permissionUseShop, this);
            if (config.humanNpcs.useHumanNpcs)
                SubAndUnSubHumanNpcHook(true);
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (!PlayerThemeSelect.ContainsKey(player.userID))
            {
                PlayerThemeSelect.Add(player.userID, config.interfaceSettings.themeTypeDefault);
            }
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (uiPlayerUseNow.Contains(player.userID))
                uiPlayerUseNow.Remove(player.userID);
            if (config.humanNpcs.useHumanNpcs)
                if (humanNpcPlayerOpen.ContainsKey(player.userID))
                    humanNpcPlayerOpen.Remove(player.userID);
        }
        private void OnNewSave(string filename)
        {
            PlayerDataLimits.Players.Clear();
            PlayerDataCooldowns.Clear();
        }

        #endregion

        #region UI
        private readonly List<ulong> uiPlayerUseNow = new List<ulong>();
        private readonly Dictionary<ulong, string> humanNpcPlayerOpen = new Dictionary<ulong, string>();
        #region Layers
        private const string MAIN_LAYER = "MAIN_SHOP_LAYER";
        private const string PANEL_PRODUCT = "MAIN_PANEL_PRODUCTS";
        private const string CATEGORY_LAYER = "CATEGORY_LAYER";
        private const string PRODUCTS_LAYER = "PRODUCTS_LAYER";
        private const string PAGE_PRODUCTS = "PAGE_PRODUCTS";
        private const string PRODUCT_AMOUNT_CHANGE_BG = "PRODUCT_AMOUNT_CHANGE_BG";
        private const string NOTIFICATION_MAIN = "NOTIFICATION_MAIN";
        private const string CONFIRMATIONS_PANEL = "CONFIRMATIONS_PANEL";
        private const string CONFIRMATIONS_PANEL_SELLING = "CONFIRMATIONS_PANEL_SELLING";
        #endregion

        #region Main
        private void XDShopUI(BasePlayer player)
        {
            if(!string.IsNullOrWhiteSpace(config.mainSettings.permissionUseShop) && !permission.UserHasPermission(player.UserIDString, config.mainSettings.permissionUseShop))
            {
                player.ChatMessage(GetLang("XDSHOP_SERVICE_5", player.UserIDString));
                return;
            }
            if(GetCategories(player).Count == 0)
            {
                player.ChatMessage(GetLang("XDSHOP_SERVICE_6", player.UserIDString));
                return;
            }
            if (!uiPlayerUseNow.Contains(player.userID))
                uiPlayerUseNow.Add(player.userID);
            else return;
            if (!PlayerThemeSelect.ContainsKey(player.userID))
                PlayerThemeSelect.Add(player.userID, config.interfaceSettings.themeTypeDefault);
            ThemeType themeType = PlayerThemeSelect[player.userID];

            CuiElementContainer container = new CuiElementContainer
            {
                {
                    new CuiPanel
                    {
                        CursorEnabled = true,
                        Image = { Color = "1 1 1 0" },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-528.963 -263.276", OffsetMax = "504.037 268.724" }
                    },
                    "Overlay",
                    MAIN_LAYER
                }
            };
            CuiHelper.DestroyUi(player, MAIN_LAYER);
            CuiHelper.AddUi(player, container);
            UIMAIN(player, themeType);
        }

        private void UIMAIN(BasePlayer player, ThemeType themetype)
        {
            Configuration.InterfaceSettings.ThemeCustomization theme = themetype == ThemeType.Dark ? config.interfaceSettings.darkTheme : config.interfaceSettings.lightTheme;
            float discount = GetUserDiscount(player);
            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiElement
            {
                Name = PANEL_PRODUCT,
                Parent = MAIN_LAYER,
                Components = {
                    new CuiRawImageComponent { Color = theme.colorMainBG, Png = ImageUi.GetImage("0") },
                    new CuiRectTransformComponent {  AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0", Command = "UI_HandlerShop CLOSE_ALL_UI" },
                Text = { Text = "x", Font = "robotocondensed-bold.ttf", FontSize = 13, Align = TextAnchor.MiddleCenter, Color = theme.closeBtnColor },
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-27.467 -24.554", OffsetMax = "-6.946 -5.983" }
            }, PANEL_PRODUCT, "CLOSE_MENU_BTN");

            #region TITLE

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "115.291 -40.197", OffsetMax = "248.709 -21.203" },
                Text = { Text = GetLang("XDSHOP_UI_TITLE", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = theme.colorTextTitle }
            }, PANEL_PRODUCT, "LABEL_TITLE");

            container.Add(new CuiElement
            {
                Name = "IMAGE_TITLE",
                Parent = PANEL_PRODUCT,
                Components = {
                    new CuiImageComponent { Color = theme.colorImgTitle, Png = ImageUi.GetImage("7") },
                    new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "90.223 -40.197", OffsetMax = "107.083 -23.441" }
                }
            });

            #endregion

            #region DISCOUNT

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "1 1 1 0" },
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-113.69 -46.75", OffsetMax = "-61.91 -18.068" }
            }, PANEL_PRODUCT, "DISCOUNT_INFO");

            container.Add(new CuiElement
            {
                Name = "DISCOUNT_IMAGE",
                Parent = "DISCOUNT_INFO",
                Components = {
                    new CuiRawImageComponent { Color = theme.colorImgDiscount, Png = ImageUi.GetImage("5") },
                    new CuiRectTransformComponent { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "3 -7.5", OffsetMax = "18 10.5" }
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-1.576 -7.5", OffsetMax = "25.89 7.051" },
                Text = { Text = $"{discount}%", Font = "robotocondensed-bold.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = theme.colorTextDiscount }
            }, "DISCOUNT_INFO", "DISCOUNT");

            #endregion

            CuiHelper.DestroyUi(player, PANEL_PRODUCT);
            CuiHelper.AddUi(player, container);
            UIPRODUCT(player, themetype);
            UICATEGORY(player, themetype);
            UpdateBalance(player, themetype);
            if (config.interfaceSettings.useChangeTheme)
                ThemeSwitch(player, themetype);
        }
        #endregion

        #region Theme switcher

        private void ThemeSwitch(BasePlayer player, ThemeType themetype)
        {
            string img = themetype == ThemeType.Dark ? ImageUi.GetImage("12") : ImageUi.GetImage("11");
            CuiElementContainer container = new CuiElementContainer
            {
                new CuiElement
                {
                    Name = "THEME_BTN",
                    Parent = PANEL_PRODUCT,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = img },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "16.5 12.8", OffsetMax = "38.5 25.8" }
                }
                },

                {
                    new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                        Button = { Command = $"UI_HandlerShop THEME_SWITCH {themetype}", Color = "0 0 0 0" },
                        Text = { Text = "", }
                    },
                    "THEME_BTN"
                }
            };
            CuiHelper.DestroyUi(player, "THEME_BTN");
            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Update Balance
        private void UpdateBalance(BasePlayer player, ThemeType themetype)
        {
            Configuration.InterfaceSettings.ThemeCustomization theme = themetype == ThemeType.Dark ? config.interfaceSettings.darkTheme : config.interfaceSettings.lightTheme;
            CuiElementContainer container = new CuiElementContainer
            {
                {
                    new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = "1 1 1 0" },
                        RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-208.947 -46.751", OffsetMax = "-113.693 -18.067" }
                    },
                    PANEL_PRODUCT,
                    "BALANCE_INFO"
                },

                new CuiElement
                {
                    Name = "BALANCE_IMAGE",
                    Parent = "BALANCE_INFO",
                    Components = {
                    new CuiImageComponent { Color = theme.colorImgBalance, Png = ImageUi.GetImage("6") },
                    new CuiRectTransformComponent { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "7.9 -7", OffsetMax = "28.9 7" }
                }
                },
                {
                    new CuiLabel
                    {
                        RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-61.563 -9.46", OffsetMax = "-0.372 7.46" },
                        Text = { Text = GetBalance(player).ToString("0.0") + config.economicsCustomization.prefixBalance, Font = "robotocondensed-bold.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = theme.colorTextBalance }
                    },
                    "BALANCE_INFO",
                    "BALANCE"
                }
            };
            CuiHelper.DestroyUi(player, "BALANCE_INFO");
            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Category UI		
        private void UICATEGORY(BasePlayer player, ThemeType themetype, int page = 0, int categoryIndex = 0)
        {
            Configuration.InterfaceSettings.ThemeCustomization theme = themetype == ThemeType.Dark ? config.interfaceSettings.darkTheme : config.interfaceSettings.lightTheme;
            CuiElementContainer container = new CuiElementContainer
            {
                {
                    new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = "1 1 1 0" },
                        RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-425.607 -77.449", OffsetMax = "454.613 -46.751" }
                    },
                    PANEL_PRODUCT,
                    CATEGORY_LAYER
                }
            };

            int i = 0, catIndex = page * 6;
            List<Configuration.CategoryShop> category = GetCategories(player);
            foreach (Configuration.CategoryShop cat in category.Page(page, 6))
            {
                bool thisCat = catIndex == categoryIndex;
                string colorLine = thisCat ? theme.color2 : theme.color3;
                container.Add(new CuiElement
                {
                    Name = $"CATEGORY_{i}",
                    Parent = CATEGORY_LAYER,
                    Components = {
                        new CuiRawImageComponent { Color = colorLine, Png = ImageUi.GetImage("18") },
                        new CuiRectTransformComponent { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = $"{0.912 + (i * 133)} -13.949", OffsetMax = $"{95.559 + (i * 133)} 5.349" }
                    }
                });

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = colorLine, Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-47.324 -9.649", OffsetMax = "47.324 -8.649" }
                }, $"CATEGORY_{i}", "CATEGORY_LINE");

                container.Add(new CuiButton
                {
                    Button = { Command = thisCat ? "" : $"UI_HandlerShop CHANGE_CATEGORY {page} {catIndex} {themetype}", Color = "0 0 0 0" },
                    Text = { Text = cat.CategoryName, Font = thisCat ? "robotocondensed-bold.ttf" : "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.UpperCenter, Color = thisCat ? theme.colortext3 : theme.colortext4 },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-47.323 -9.649", OffsetMax = "47.323 9.649" }
                }, $"CATEGORY_{i}");
                catIndex++;
                if (i >= 5)
                    break;
                i++;
            }

            #region CATEGORY_PAGE
            if (category.Count > 6)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = "1 1 1 0" },
                    RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-77.294 -30.698", OffsetMax = "0 0" }
                }, CATEGORY_LAYER, "CATEGORY_PAGE");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-10.74 -6.965", OffsetMax = "10.74 6.965" },
                    Text = { Text = $"{page + 1}", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = theme.colortext1 },
                }, "CATEGORY_PAGE");

                if (page > 0)
                {
                    container.Add(new CuiElement
                    {
                        Name = "PAGE_LEFT",
                        Parent = "CATEGORY_PAGE",
                        Components = {
                        new CuiRawImageComponent { Color = theme.color1, Png = ImageUi.GetImage("3") },
                        new CuiRectTransformComponent { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "13 -6.31", OffsetMax = "20 4.69" }
                    }
                    });
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-38.647 -15.349", OffsetMax = "0 15.349" },
                        Button = { Command = $"UI_HandlerShop PAGE_CATEGORY {page - 1} {themetype} {(page - 1) * 6}", Color = "0 0 0 0" },
                        Text = { Text = "" }
                    }, "CATEGORY_PAGE", "PAGE_LEFT_BTN");
                }
                if (page + 1 < (int)Math.Ceiling((double)category.Count / 6))
                {
                    container.Add(new CuiElement
                    {
                        Name = "PAGE_RIGHTS",
                        Parent = "CATEGORY_PAGE",
                        Components = {
                            new CuiRawImageComponent { Color = theme.color1, Png = ImageUi.GetImage("4") },
                            new CuiRectTransformComponent { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-20 -6.31", OffsetMax = "-13 4.69" }
                        }
                    });
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "0 -15.349", OffsetMax = "38.646 15.349" },
                        Button = { Command = $"UI_HandlerShop PAGE_CATEGORY {page + 1} {themetype} {catIndex}", Color = "0 0 0 0" },
                        Text = { Text = "" }
                    }, "CATEGORY_PAGE", "PAGE_RIGHT_BTN");
                }
            }
            #endregion

            CuiHelper.DestroyUi(player, CATEGORY_LAYER);
            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region UpdUiAmount
        private void UPDATEAMOUNTUI(BasePlayer player, int index, int cat, int amount, ThemeType themetype)
        {
            Configuration.InterfaceSettings.ThemeCustomization theme = themetype == ThemeType.Dark ? config.interfaceSettings.darkTheme : config.interfaceSettings.lightTheme;
            Configuration.CategoryShop.Product item = GetCategories(player)[cat].product.Find(x => x.ID == index);
            if (amount <= 0 || (item.GetLimitLot(player, true) != 0 && amount > item.GetLimitLot(player, true)) || amount >= 100)
                return;
            float discount = GetUserDiscount(player);
            CuiElementContainer container = new CuiElementContainer
            {
                {
                    new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-10.064 -8", OffsetMax = "9.372 8" },
                        Text = { Text = amount.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleCenter, Color = themetype == ThemeType.Dark ? "1 1 1 1" : "0 0 0 1" },
                    },
                    PRODUCT_AMOUNT_CHANGE_BG + $"_{index}",
                    $"PRODUCT_LOT_COUNT_{index}"
                },
                {
                    new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-53.25 68.035", OffsetMax = "38.45 83.165" },
                        Text = { Text = "x" + (item.Amount * amount), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = theme.colortext2 },
                    },
                    $"PRODUCT_BACKGROUND_{index}",
                    $"PRODUCT_AMOUNT_{index}"
                },
                {
                    new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-63 0", OffsetMax = "63 15" },
                        Button = { Command = $"UI_HandlerShop PRODUCT_BUY_CONFIRMATION {index} {cat} {amount} {themetype}", Color = theme.colortext8 },
                        Text = { Text = GetLang("XDSHOP_UI_BTN_BUY", player.UserIDString), FontSize = 12, Align = TextAnchor.MiddleCenter }
                    },
                    $"PRODUCT_BACKGROUND_{index}",
                    $"PRODUCT_BUY_BTN_{index}"
                },
                {
                    new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "0 -8", OffsetMax = "14.936 8" },
                        Button = { Command = $"UI_HandlerShop AMOUNT_CHANGE_MINUS {index} {cat} {amount} {themetype} 0", Color = "0 0 0 0" },
                        Text = { Text = "" }
                    },
                    PRODUCT_AMOUNT_CHANGE_BG + $"_{index}",
                    $"PRODUCT_AMOUNT_MINUS_{index}"
                },

                {
                    new CuiButton
                    {
                        RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-15.628 -8", OffsetMax = "0 8" },
                        Button = { Command = $"UI_HandlerShop AMOUNT_CHANGE_PLUS {index} {cat} {amount} {themetype} 0", Color = "0 0 0 0" },
                        Text = { Text = "" }
                    },
                    PRODUCT_AMOUNT_CHANGE_BG + $"_{index}",
                    $"PRODUCT_AMOUNT_PLUS_{index}"
                },

                {
                    new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-41.852 -55.831", OffsetMax = "5.011 -34.169" },
                        Text = { Text = $"{GetDiscountedPrice((item.Price * amount), discount)}{config.economicsCustomization.prefixBalance}", Font = "robotocondensed-bold.ttf", FontSize = 13, Align = TextAnchor.MiddleLeft, Color = "0.24 0.48 0.71 1.00" },
                    },
                    $"PRODUCT_BACKGROUND_{index}",
                    $"PRODUCT_PRICE_{index}"
                }
            };

            CuiHelper.DestroyUi(player, $"PRODUCT_AMOUNT_{index}");
            CuiHelper.DestroyUi(player, $"PRODUCT_LOT_COUNT_{index}");
            CuiHelper.DestroyUi(player, $"PRODUCT_PRICE_{index}");
            CuiHelper.DestroyUi(player, $"PRODUCT_AMOUNT_PLUS_{index}");
            CuiHelper.DestroyUi(player, $"PRODUCT_AMOUNT_PLUS_{index}");
            CuiHelper.DestroyUi(player, $"PRODUCT_AMOUNT_MINUS_{index}");
            CuiHelper.DestroyUi(player, $"PRODUCT_BUY_BTN_{index}");
            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Notification UI
        private void NotificationUI(BasePlayer player, NotificationType type, string msg)
        {
            string color = type == NotificationType.Error ? "1.00 0.30 0.31 1.00" : type == NotificationType.Warning ? "0.98 0.68 0.08 1.00" : "0.32 0.77 0.10 1.00";
            ThemeType themeType = PlayerThemeSelect[player.userID];
            Configuration.InterfaceSettings.ThemeCustomization theme = themeType == ThemeType.Dark ? config.interfaceSettings.darkTheme : config.interfaceSettings.lightTheme;

            CuiElementContainer container = new CuiElementContainer
            {
                new CuiElement
                {
                    FadeOut = 0.30f,
                    Name = NOTIFICATION_MAIN,
                    Parent = "Overlay",
                    Components = {
                        new CuiRawImageComponent { Color = theme.colorMainBG,  Png = ImageUi.GetImage("16"), FadeIn = 0.30f },
                        new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "112 -78", OffsetMax = "383 -10" }
                    }
                },

                new CuiElement
                {
                    FadeOut = 0.30f,
                    Name = "NOTIFICATION_POLOSA",
                    Parent = NOTIFICATION_MAIN,
                    Components = {
                        new CuiRawImageComponent { Color = color, Png = ImageUi.GetImage("17") , FadeIn = 0.30f},
                        new CuiRectTransformComponent { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-95.99 -1.97", OffsetMax = "75.01 2.03" }
                    }
                },

                new CuiElement
                {
                    FadeOut = 0.30f,
                    Name = "NOTIFICATION_IMG",
                    Parent = NOTIFICATION_MAIN,
                    Components = {
                        new CuiRawImageComponent { Color = color, Png = ImageUi.GetImage("10") , FadeIn = 0.30f},
                        new CuiRectTransformComponent { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "13.5 -32.47", OffsetMax = "73.5 28.53" }
                    }
                },

                {
                    new CuiLabel
                    {
                        FadeOut = 0.30f,
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-58 -32.47", OffsetMax = "119.546 28.53" },
                        Text = { Text = msg, Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = theme.colortext10, FadeIn = 0.30f },
                    },
                    NOTIFICATION_MAIN,
                    "NOTIFICATION_TEXT"
                }
            };

            CuiHelper.DestroyUi(player, "NOTIFICATION_POLOSA");
            CuiHelper.DestroyUi(player, "NOTIFICATION_IMG");
            CuiHelper.DestroyUi(player, "NOTIFICATION_TEXT");
            CuiHelper.DestroyUi(player, NOTIFICATION_MAIN);
            CuiHelper.AddUi(player, container);

            DeleteNotification(player);
        }
        private readonly Dictionary<BasePlayer, Timer> PlayerTimer = new Dictionary<BasePlayer, Timer>();

        private void DeleteNotification(BasePlayer player)
        {
            Timer timers = timer.Once(3.5f, () =>
            {
                CuiHelper.DestroyUi(player, "NOTIFICATION_POLOSA");
                CuiHelper.DestroyUi(player, "NOTIFICATION_IMG");
                CuiHelper.DestroyUi(player, "NOTIFICATION_TEXT");
                CuiHelper.DestroyUi(player, NOTIFICATION_MAIN);
            });

            if (PlayerTimer.ContainsKey(player))
            {
                if (PlayerTimer[player] != null && !PlayerTimer[player].Destroyed) PlayerTimer[player].Destroy();
                PlayerTimer[player] = timers;
            }
            else PlayerTimer.Add(player, timers);
        }
        #endregion

        #region Products UI
        private void UIPRODUCT(BasePlayer player, ThemeType themetype, int page = 0, int categoryIndex = 0)
        {
            Configuration.InterfaceSettings.ThemeCustomization theme = themetype == ThemeType.Dark ? config.interfaceSettings.darkTheme : config.interfaceSettings.lightTheme;
            float discount = GetUserDiscount(player);
            List<Configuration.CategoryShop.Product> products = GetCategories(player)[categoryIndex].product;

            CuiElementContainer container = new CuiElementContainer
            {
                {
                    new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = "1 1 1 0" },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-425.609 -220.265", OffsetMax = "446.688 164.959" }
                    },
                    PANEL_PRODUCT,
                    PRODUCTS_LAYER
                }
            };
            int i = 0, u = 0;
            foreach (Configuration.CategoryShop.Product item in products.Where(x => !x.ItemsErorred).Page(page, 12))
            {
                string imageProduct = !string.IsNullOrWhiteSpace(item.Url) ? GetImage(item.Url) : item.type == ItemType.CustomItem ? GetImage(item.ShortName + 60, item.SkinID) : GetImage(item.ShortName + 60);
                container.Add(new CuiElement
                {
                    Name = $"PRODUCT_BACKGROUND_{item.ID}",
                    Parent = PRODUCTS_LAYER,
                    Components = {
                        new CuiRawImageComponent { Color = theme.colortext6,  Png = ImageUi.GetImage("8") },
                        new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"{1.67 + (i * 149)} {-181.44 - (u * 205)}", OffsetMax = $"{127.67 + (i * 149)} {-2.44 - (u * 205)}" }
                    }
                });
                if (item.type == ItemType.Blueprint)
                {
                    container.Add(new CuiElement
                    {
                        Name = "PRODUCT_IMAGE",
                        Parent = $"PRODUCT_BACKGROUND_{item.ID}",
                        Components = {
                            new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("blueprintbase60") },
                            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-30 10.5", OffsetMax = "30 70.5"  }
                        }
                    });
                }
                container.Add(new CuiElement
                {
                    Name = "PRODUCT_IMAGE",
                    Parent = $"PRODUCT_BACKGROUND_{item.ID}",
                    Components = {
                        new CuiRawImageComponent { Color = "1 1 1 1", Png = imageProduct },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-30 10.5", OffsetMax = "30 70.5"  }
                    }
                });
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-41.852 -34.169", OffsetMax = "50.482 5.587" },
                    Text = { Text = item.GetProductName(player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = theme.colortext7 },
                }, $"PRODUCT_BACKGROUND_{item.ID}", "PRODUCT_NAME");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-41.852 -55.831", OffsetMax = "5.011 -34.169" },
                    Text = { Text = $"{GetDiscountedPrice(item.Price, discount)}{config.economicsCustomization.prefixBalance}", Font = "robotocondensed-bold.ttf", FontSize = 13, Align = TextAnchor.MiddleLeft, Color = "0.26 0.53 0.80 1.00" },
                }, $"PRODUCT_BACKGROUND_{item.ID}", $"PRODUCT_PRICE_{item.ID}");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-53.25 68.035", OffsetMax = "38.45 83.165" },
                    Text = { Text = $"x{item.Amount}", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = theme.colortext2 },
                }, $"PRODUCT_BACKGROUND_{item.ID}", $"PRODUCT_AMOUNT_{item.ID}");

                if (item.PriceSales != 0)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-63 15", OffsetMax = "63 30" },
                        Button = { Command = $"UI_HandlerShop PRODUCT_SALE_CONFIRMATION {item.ID} {categoryIndex} 1 {themetype}", Color = theme.colortext9 },
                        Text = { Text = GetLang("XDSHOP_UI_BTN_SALLE", player.UserIDString), FontSize = 12, Align = TextAnchor.MiddleCenter }
                    }, $"PRODUCT_BACKGROUND_{item.ID}", "PRODUCT_SELL_BTN");
                }
                if (item.Price != 0)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-63 0", OffsetMax = "63 15" },
                        Button = { Command = $"UI_HandlerShop PRODUCT_BUY_CONFIRMATION {item.ID} {categoryIndex} 1 {themetype}", Color = theme.colortext8 },
                        Text = { Text = GetLang("XDSHOP_UI_BTN_BUY", player.UserIDString), FontSize = 12, Align = TextAnchor.MiddleCenter }
                    }, $"PRODUCT_BACKGROUND_{item.ID}", $"PRODUCT_BUY_BTN_{item.ID}");
                }
                #region AmountSwitcher
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "1 1 1 0" },
                    RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-57.245 35.021", OffsetMax = "-2.249 53.979" }
                }, $"PRODUCT_BACKGROUND_{item.ID}", "PRODUCT_AMOUNT_CHANGE");
                container.Add(new CuiElement
                {
                    Name = PRODUCT_AMOUNT_CHANGE_BG + $"_{item.ID}",
                    Parent = "PRODUCT_AMOUNT_CHANGE",
                    Components = {
                    new CuiRawImageComponent { Color = themetype == ThemeType.Dark ? "1 1 1 0.2" : "1 1 1 1", Png = ImageUi.GetImage("9") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-25 -8", OffsetMax = "25 8" }
                }
                });
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "0 -8", OffsetMax = "14.936 8" },
                    Button = { Command = $"UI_HandlerShop AMOUNT_CHANGE_MINUS {item.ID} {categoryIndex} {0} {themetype} 0", Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, PRODUCT_AMOUNT_CHANGE_BG + $"_{item.ID}", $"PRODUCT_AMOUNT_MINUS_{item.ID}");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-15.628 -8", OffsetMax = "0 8" },
                    Button = { Command = $"UI_HandlerShop AMOUNT_CHANGE_PLUS {item.ID} {categoryIndex} {1} {themetype} 0", Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, PRODUCT_AMOUNT_CHANGE_BG + $"_{item.ID}", $"PRODUCT_AMOUNT_PLUS_{item.ID}");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-10.064 -8", OffsetMax = "9.372 8" },
                    Text = { Text = "1", Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleCenter, Color = themetype == ThemeType.Dark ? "1 1 1 1" : "0 0 0 1" },
                }, PRODUCT_AMOUNT_CHANGE_BG + $"_{item.ID}", $"PRODUCT_LOT_COUNT_{item.ID}");

                #endregion
                i++;
                if (i >= 6)
                {
                    u++;
                    i = 0;
                }

            }

            #region PAGE
            if (products.Count > 12)
            {
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0 0 0 0" },
                    RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "18.175 -74.012", OffsetMax = "68.404 51.812" }
                }, PANEL_PRODUCT, PAGE_PRODUCTS);

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-25.114 -12.948", OffsetMax = "25.115 11.548" },
                    Text = { Text = $"{page + 1}", Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = theme.colortext1 },
                }, PAGE_PRODUCTS, "PAGE_NUMBER");

                if (page > 0)
                {
                    container.Add(new CuiElement
                    {
                        Name = "PAGE_UP",
                        Parent = PAGE_PRODUCTS,
                        Components = {
                    new CuiRawImageComponent { Color = theme.color1, Png = ImageUi.GetImage("1") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-11.25 -25.14", OffsetMax = "9.75 -12.14" }
                }
                    });

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-25.115 -38.835", OffsetMax = "25.115 -0.089" },
                        Button = { Command = $"UI_HandlerShop PAGE_PRODUCT {page - 1} {categoryIndex} {themetype}", Color = "0 0 0 0" },
                        Text = { Text = "" }
                    }, PAGE_PRODUCTS, "PAGE_UP_BTN");
                }
                if (page + 1 < (int)Math.Ceiling((double)products.Count / 12))
                {
                    container.Add(new CuiElement
                    {
                        Name = "PAGE_DOWN",
                        Parent = PAGE_PRODUCTS,
                        Components = {
                    new CuiRawImageComponent { Color = theme.color1, Png = ImageUi.GetImage("2") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-11.25 11.4", OffsetMax = "9.75 24.4" }
                }
                    });

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-25.115 0.088", OffsetMax = "25.115 38.834" },
                        Button = { Command = $"UI_HandlerShop PAGE_PRODUCT {page + 1} {categoryIndex} {themetype}", Color = "0 0 0 0" },
                        Text = { Text = "" }
                    }, PAGE_PRODUCTS, "PAGE_DOWN_BTN");
                }
            }
            #endregion

            CuiHelper.DestroyUi(player, PAGE_PRODUCTS);
            CuiHelper.DestroyUi(player, PRODUCTS_LAYER);
            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region CONFIRMATIONS UI
        private void CONFIRMATIONSUI(BasePlayer player, int itemID, int indexCategory, int amount, ThemeType themetype)
        {
            Configuration.InterfaceSettings.ThemeCustomization theme = themetype == ThemeType.Dark ? config.interfaceSettings.darkTheme : config.interfaceSettings.lightTheme;
            Configuration.CategoryShop.Product product = GetCategories(player)[indexCategory].product.Find(x => x.ID == itemID);
            string imageProduct = !string.IsNullOrWhiteSpace(product.Url) ? GetImage(product.Url) : product.type == ItemType.CustomItem ? GetImage(product.ShortName + 60, product.SkinID) : GetImage(product.ShortName + 60);
            float discount = GetUserDiscount(player);
            int playerLim = GetLimit(player, product, true);
            string limit = playerLim == -1 ? GetLang("XDSHOP_UI_PATTERN", player.UserIDString) : playerLim + "/" + product.GetLimitLotWipe(player, true);
            CuiElementContainer container = new CuiElementContainer
            {
                new CuiElement
                {
                    Name = CONFIRMATIONS_PANEL,
                    Parent = PANEL_PRODUCT,
                    Components = {
                    new CuiRawImageComponent { Color = theme.colorMainBG, Png = ImageUi.GetImage("13") },
                    new CuiRectTransformComponent {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-230.488 -109.615", OffsetMax = "261.007 77.799"}
                }
                },

                new CuiElement
                {
                    Name = "IMAGE_PRODUCT_BACKGROUND",
                    Parent = CONFIRMATIONS_PANEL,
                    Components = {
                    new CuiRawImageComponent { Color = theme.colortext6, Png = ImageUi.GetImage("15") },
                    new CuiRectTransformComponent {  AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "46.52 -114.22", OffsetMax = "122.52 -42.22" }
                }
                }
            };

            if (product.type == ItemType.Blueprint)
            {
                container.Add(new CuiElement
                {
                    Name = "PRODUCT_IMAGE",
                    Parent = "IMAGE_PRODUCT_BACKGROUND",
                    Components = {
                        new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("blueprintbase60") },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-30 -30", OffsetMax = "30 30" }
                    }
                });
            }

            container.Add(new CuiElement
            {
                Name = "PRODUCT_IMAGE",
                Parent = "IMAGE_PRODUCT_BACKGROUND",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = imageProduct },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-30 -30", OffsetMax = "30 30" }
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-118.134 -68.265", OffsetMax = "8.346 -39.543" },
                Text = { Text = product.GetProductName(player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 11, Align = TextAnchor.LowerLeft, Color = theme.colortext7 },
            }, CONFIRMATIONS_PANEL);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-118.134 -137.191", OffsetMax = "8.346 -70.689" },
                Text = { Text = GetLang("XDSHOP_UI_PRODUCT_INFO", player.UserIDString, GetDiscountedPrice(product.Price * amount, discount) + config.economicsCustomization.prefixBalance, product.Amount * amount, limit), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = theme.colortext5 },
            }, CONFIRMATIONS_PANEL);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-228.122 -8.202", OffsetMax = "-44.678 54.164" },
                Text = { Text = product.GetProductDescription(), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.UpperLeft, Color = theme.colortext5 },
            }, CONFIRMATIONS_PANEL);

            container.Add(new CuiElement
            {
                Name = "PRODUCT_BUY_BTN",
                Parent = CONFIRMATIONS_PANEL,
                Components = {
                    new CuiRawImageComponent { Color = theme.colortext8, Png = ImageUi.GetImage("14") },
                    new CuiRectTransformComponent {AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-162.4 42.9", OffsetMax = "-109.4 63.9"}
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = $"UI_HandlerShop PRODUCT_BUY {itemID} {indexCategory} {amount} {themetype}", Color = "0 0 0 0" },
                Text = { Text = GetLang("XDSHOP_UI_BTN_BUY", player.UserIDString).ToUpper(), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleCenter }
            }, "PRODUCT_BUY_BTN");

            container.Add(new CuiElement
            {
                Name = "PRODUCT_CLOSE_BTN",
                Parent = CONFIRMATIONS_PANEL,
                Components = {
                    new CuiRawImageComponent { Color = theme.closeBtnColor2, Png = ImageUi.GetImage("14") },
                    new CuiRectTransformComponent {AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-100 42.9", OffsetMax = "-47 63.9"  }
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Close = "CONFIRMATIONS_PANEL", Color = "0 0 0 0" },
                Text = { Text = GetLang("XDSHOP_UI_PRODUCT_INFO_EXIT", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleCenter }
            }, "PRODUCT_CLOSE_BTN");

            CuiHelper.DestroyUi(player, CONFIRMATIONS_PANEL);
            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region CONFIRMATION SALE UI
        private void CONFIRMATIONSSALEUI(BasePlayer player, int itemId, int indexCategory, int amount, ThemeType themetype)
        {
            Configuration.InterfaceSettings.ThemeCustomization theme = themetype == ThemeType.Dark ? config.interfaceSettings.darkTheme : config.interfaceSettings.lightTheme;
            Configuration.CategoryShop.Product product = GetCategories(player)[indexCategory].product.Find(x => x.ID == itemId);
            string imageProduct = !string.IsNullOrWhiteSpace(product.Url) ? GetImage(product.Url) : product.type == ItemType.CustomItem ? GetImage(product.ShortName + 60, product.SkinID) : GetImage(product.ShortName + 60);
            int playerLim = GetLimit(player, product, false);
            string limit = playerLim == -1 ? GetLang("XDSHOP_UI_PATTERN", player.UserIDString) : playerLim + "/" + product.GetLimitLotWipe(player, false);
            CuiElementContainer container = new CuiElementContainer
            {
                new CuiElement
                {
                    Name = CONFIRMATIONS_PANEL_SELLING,
                    Parent = PANEL_PRODUCT,
                    Components = {
                    new CuiRawImageComponent { Color = theme.colorMainBG, Png = ImageUi.GetImage("13") },
                    new CuiRectTransformComponent {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-230.488 -109.615", OffsetMax = "261.007 77.799"}
                }
                },

                new CuiElement
                {
                    Name = "IMAGE_PRODUCT_BACKGROUND",
                    Parent = CONFIRMATIONS_PANEL_SELLING,
                    Components = {
                    new CuiRawImageComponent { Color = theme.colortext6, Png = ImageUi.GetImage("15") },
                    new CuiRectTransformComponent {  AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "46.52 -114.22", OffsetMax = "122.52 -42.22" }
                }
                }
            };

            if (product.type == ItemType.Blueprint)
            {
                container.Add(new CuiElement
                {
                    Name = "PRODUCT_IMAGE",
                    Parent = "IMAGE_PRODUCT_BACKGROUND",
                    Components = {
                        new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("blueprintbase60") },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-30 -30", OffsetMax = "30 30" }
                    }
                });
            }

            container.Add(new CuiElement
            {
                Name = "PRODUCT_IMAGE",
                Parent = "IMAGE_PRODUCT_BACKGROUND",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = imageProduct },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-30 -30", OffsetMax = "30 30" }
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-118.134 -68.265", OffsetMax = "8.346 -39.543" },
                Text = { Text = product.GetProductName(player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 11, Align = TextAnchor.LowerLeft, Color = theme.colortext7 },
            }, CONFIRMATIONS_PANEL_SELLING);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-118.134 -137.191", OffsetMax = "8.346 -70.689" },
                Text = { Text = GetLang("XDSHOP_UI_PRODUCT_SELL_INFO", player.UserIDString, GetDiscountedPrice(product.PriceSales * amount, GetUserDiscount(player)) + config.economicsCustomization.prefixBalance, product.AmountSales * amount, limit), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = theme.colortext5 },
            }, CONFIRMATIONS_PANEL_SELLING, "PRODUCT_SELLING_DESC");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-228.122 -8.202", OffsetMax = "-44.678 54.164" },
                Text = { Text = product.GetProductDescription(), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.UpperLeft, Color = theme.colortext5 },
            }, CONFIRMATIONS_PANEL_SELLING);

            container.Add(new CuiElement
            {
                Name = "PRODUCT_BUY_BTN",
                Parent = CONFIRMATIONS_PANEL_SELLING,
                Components = {
                    new CuiRawImageComponent { Color = theme.colortext8, Png = ImageUi.GetImage("14") },
                    new CuiRectTransformComponent {AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-162.4 42.9", OffsetMax = "-109.4 63.9"}
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = $"UI_HandlerShop PRODUCT_SALE {itemId} {indexCategory} {amount} {themetype}", Color = "0 0 0 0" },
                Text = { Text = GetLang("XDSHOP_UI_BTN_SALLE", player.UserIDString).ToUpper(), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleCenter }
            }, "PRODUCT_BUY_BTN", "PRODUCT_BUY_GO");

            container.Add(new CuiElement
            {
                Name = "PRODUCT_CLOSE_BTN",
                Parent = CONFIRMATIONS_PANEL_SELLING,
                Components = {
                    new CuiRawImageComponent { Color = theme.closeBtnColor2, Png = ImageUi.GetImage("14") },
                    new CuiRectTransformComponent {AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-100 42.9", OffsetMax = "-47 63.9"  }
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Close = CONFIRMATIONS_PANEL_SELLING, Color = "0 0 0 0" },
                Text = { Text = GetLang("XDSHOP_UI_PRODUCT_INFO_EXIT", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleCenter }
            }, "PRODUCT_CLOSE_BTN");

            #region AmountSwitcher
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "1 1 1 0" },
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-434.85 50.21796", OffsetMax = "-379.11 69.179" }
            }, CONFIRMATIONS_PANEL_SELLING, "PRODUCT_AMOUNT_CHANGE");
            container.Add(new CuiElement
            {
                Name = PRODUCT_AMOUNT_CHANGE_BG,
                Parent = "PRODUCT_AMOUNT_CHANGE",
                Components = {
                    new CuiRawImageComponent { Color = themetype == ThemeType.Dark ? "1 1 1 0.2" : "1 1 1 1", Png = ImageUi.GetImage("9") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-25 -8", OffsetMax = "25 8" }
                }
            });
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "0 -8", OffsetMax = "14.936 8" },
                Button = { Command = $"UI_HandlerShop AMOUNT_CHANGE_MINUS {itemId} {indexCategory} {amount} {themetype} 1", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, PRODUCT_AMOUNT_CHANGE_BG, $"PRODUCT_AMOUNT_MINUS");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-15.628 -8", OffsetMax = "0 8" },
                Button = { Command = $"UI_HandlerShop AMOUNT_CHANGE_PLUS {itemId} {indexCategory} {amount} {themetype} 1", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, PRODUCT_AMOUNT_CHANGE_BG, $"PRODUCT_AMOUNT_PLUS");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-10.064 -8", OffsetMax = "9.372 8" },
                Text = { Text = "1", Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleCenter, Color = themetype == ThemeType.Dark ? "1 1 1 1" : "0 0 0 1" },
            }, PRODUCT_AMOUNT_CHANGE_BG, $"PRODUCT_LOT_COUNT");

            #endregion

            CuiHelper.DestroyUi(player, CONFIRMATIONS_PANEL_SELLING);
            CuiHelper.AddUi(player, container);
        }

        private void UPDATE_UI_SELLING(BasePlayer player, int itemId, int indexCategory, int amount, ThemeType themetype)
        {
            CuiElementContainer container = new CuiElementContainer();
            Configuration.CategoryShop.Product product = GetCategories(player)[indexCategory].product.Find(x => x.ID == itemId);
            if (amount <= 0 || (product.GetLimitLot(player, false) != 0 && amount > product.GetLimitLot(player, false)) || amount >= 100)
                return;
            int playerLim = GetLimit(player, product, false);
            string limit = playerLim == -1 ? GetLang("XDSHOP_UI_PATTERN", player.UserIDString) : playerLim + "/" + product.GetLimitLotWipe(player, false);
            Configuration.InterfaceSettings.ThemeCustomization theme = themetype == ThemeType.Dark ? config.interfaceSettings.darkTheme : config.interfaceSettings.lightTheme;

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = $"UI_HandlerShop PRODUCT_SALE {itemId} {indexCategory} {amount} {themetype}", Color = "0 0 0 0" },
                Text = { Text = GetLang("XDSHOP_UI_BTN_SALLE", player.UserIDString).ToUpper(), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleCenter }
            }, "PRODUCT_BUY_BTN", "PRODUCT_BUY_GO");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-10.064 -8", OffsetMax = "9.372 8" },
                Text = { Text = amount.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleCenter, Color = themetype == ThemeType.Dark ? "1 1 1 1" : "0 0 0 1" },
            }, PRODUCT_AMOUNT_CHANGE_BG, $"PRODUCT_LOT_COUNT");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "0 -8", OffsetMax = "14.936 8" },
                Button = { Command = $"UI_HandlerShop AMOUNT_CHANGE_MINUS {itemId} {indexCategory} {amount} {themetype} 1", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, PRODUCT_AMOUNT_CHANGE_BG, $"PRODUCT_AMOUNT_MINUS");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-15.628 -8", OffsetMax = "0 8" },
                Button = { Command = $"UI_HandlerShop AMOUNT_CHANGE_PLUS {itemId} {indexCategory} {amount} {themetype} 1", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, PRODUCT_AMOUNT_CHANGE_BG, $"PRODUCT_AMOUNT_PLUS");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-118.134 -137.191", OffsetMax = "8.346 -70.689" },
                Text = { Text = GetLang("XDSHOP_UI_PRODUCT_SELL_INFO", player.UserIDString, GetDiscountedPrice(product.PriceSales * amount, GetUserDiscount(player)) + config.economicsCustomization.prefixBalance, product.AmountSales * amount, limit), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = theme.colortext5 },
            }, CONFIRMATIONS_PANEL_SELLING, "PRODUCT_SELLING_DESC");

            CuiHelper.DestroyUi(player, "PRODUCT_BUY_GO");
            CuiHelper.DestroyUi(player, "PRODUCT_AMOUNT_MINUS");
            CuiHelper.DestroyUi(player, "PRODUCT_AMOUNT_PLUS");
            CuiHelper.DestroyUi(player, "PRODUCT_LOT_COUNT");
            CuiHelper.DestroyUi(player, "PRODUCT_SELLING_DESC");
            CuiHelper.AddUi(player, container);
        }

        #endregion
        #endregion

        #region Metods

        #region Purchasing
        private void GiveProduct(BasePlayer player, Configuration.CategoryShop.Product product, int amount)
        {
            switch (product.type)
            {
                case ItemType.Item:
                    GetStacks(product.Definition, product.Amount * amount)?.ForEach(stack =>
                    {
                        Item newItem = ItemManager.CreateByPartialName(product.ShortName, stack);
                        player.GiveItem(newItem, BaseEntity.GiveItemReason.PickedUp);
                    });
                    break;
                case ItemType.Blueprint:
                    GetStacks(product.Definition, product.Amount * amount)?.ForEach(stack =>
                    {
                        Item itemBp = ItemManager.CreateByItemID(-996920608, stack);
                        if (itemBp.instanceData == null)
                            itemBp.instanceData = new ProtoBuf.Item.InstanceData();
                        itemBp.instanceData.ShouldPool = false;
                        itemBp.instanceData.blueprintAmount = 1;
                        itemBp.instanceData.blueprintTarget = product.Definition.itemid;
                        itemBp.MarkDirty();
                        player.GiveItem(itemBp, BaseEntity.GiveItemReason.PickedUp);
                    });
                    break;
                case ItemType.CustomItem:
                    GetStacks(product.Definition, product.Amount * amount)?.ForEach(stack =>
                    {
                        Item customItem = ItemManager.CreateByPartialName(product.ShortName, stack, product.SkinID);
                        customItem.name = product.Name;
                        player.GiveItem(customItem, BaseEntity.GiveItemReason.PickedUp);
                    });
                    break;
                case ItemType.Command:
                    foreach (string cammand in product.Commands)
                        Server.Command(cammand.Replace("%STEAMID%", player.UserIDString));
                    break;
                case ItemType.Kit:
                    if (Kits)
                        Kits.Call("GiveKit", player, product.KitName);
                    else if (IQKits)
                        IQKits.Call("API_KIT_GIVE", player, product.KitName);
                    break;
            }
        }

        private List<int> GetStacks(ItemDefinition items, int amount)
        {
            List<int> list = new List<int>();
            int maxStack = items.stackable;

            while (amount > maxStack)
            {
                amount -= maxStack;
                list.Add(maxStack);
            }

            list.Add(amount);

            return list;
        }
        private void Purchasing(BasePlayer player, int itemId, int category, int amount, ThemeType themetype)
        {
            Configuration.CategoryShop.Product product = GetCategories(player)[category].product.Find(x => x.ID == itemId);

            if (product != null)
            {
                if (IsDuel(player))
                {
                    NotificationUI(player, NotificationType.Error, GetLang("XDSHOP_UI_NOTIFICATION_IS_DUEL", player.UserIDString));
                    return;
                }
                if (config.mainSettings.raidBlock && IsRaid(player))
                {
                    NotificationUI(player, NotificationType.Error, GetLang("XDSHOP_UI_NOTIFICATION_IS_RAID", player.UserIDString));
                    return;
                }
                if (product.type == ItemType.Blueprint || product.type == ItemType.CustomItem || product.type == ItemType.Item)
                {
                    List<int> stack = GetStacks(product.Definition, product.Amount * amount);
                    int slots = player.inventory.containerBelt.capacity -
                                   player.inventory.containerBelt.itemList.Count +
                                   (player.inventory.containerMain.capacity -
                                    player.inventory.containerMain.itemList.Count);
                    if (slots < stack.Count)
                    {
                        NotificationUI(player, NotificationType.Warning, GetLang("XDSHOP_UI_NOTIFICATION_NOT_ENOUGH_SPACE", player.UserIDString));
                        return;
                    }
                }

                float price = GetDiscountedPrice(product.Price * amount, GetUserDiscount(player));
                if (GetBalance(player) < price)
                {
                    NotificationUI(player, NotificationType.Error, GetLang("XDSHOP_UI_NOTIFICATION_INSUFFICIENT_FUNDS", player.UserIDString));
                    return;
                }
                if (product.BuyCooldown > 0)
                {
                    int cooldown = GetCooldownTime(player.userID, product, true);
                    if (cooldown > 0)
                    {
                        NotificationUI(player, NotificationType.Warning, GetLang("XDSHOP_UI_NOTIFICATION_BUY_RECHARGE", player.UserIDString, product.GetProductName(player.UserIDString), TimeHelper.FormatTime(TimeSpan.FromSeconds(cooldown), 5, lang.GetLanguage(player.UserIDString))));
                        return;
                    }
                    SetCooldown(player, product, true);
                }

                int limit = GetLimit(player, product, true);
                if (limit != -1)
                {
                    if (limit == 0)
                    {
                        NotificationUI(player, NotificationType.Warning, GetLang("XDSHOP_UI_NOTIFICATION_BUY_LIMIT", player.UserIDString, product.GetProductName(player.UserIDString)));
                        return;
                    }
                    else if (amount > limit)
                    {
                        NotificationUI(player, NotificationType.Warning, GetLang("XDSHOP_UI_NOTIFICATION_BUY_LIMIT_1", player.UserIDString, product.GetProductName(player.UserIDString), limit));
                        return;
                    }
                    AddLimit(player, product, amount, true);
                }

                switch (config.economicsCustomization.typeEconomic)
                {
                    case EconomicsType.Economics:
                        if ((bool)Economics?.Call("Withdraw", player.userID, (double)price))
                        {
                            GiveProduct(player, product, amount);
                            UpdateBalance(player, themetype);
                            NotificationUI(player, NotificationType.Success, GetLang("XDSHOP_UI_NOTIFICATION_SUCCESSFUL_PURCHASE", player.UserIDString, product.GetProductName(player.UserIDString)));
                        }
                        break;
                    case EconomicsType.ServerRewards:
                        if (ServerRewards?.Call<object>("TakePoints", player.userID, (int)price) != null)
                        {
                            GiveProduct(player, product, amount);
                            UpdateBalance(player, themetype);
                            NotificationUI(player, NotificationType.Success, GetLang("XDSHOP_UI_NOTIFICATION_SUCCESSFUL_PURCHASE", player.UserIDString, product.GetProductName(player.UserIDString)));
                        }
                        break;
                    case EconomicsType.IQEconomic:
                        IQEconomic?.Call("API_REMOVE_BALANCE", player.userID, (int)price);
                        GiveProduct(player, product, amount);
                        UpdateBalance(player, themetype);
                        NotificationUI(player, NotificationType.Success, GetLang("XDSHOP_UI_NOTIFICATION_SUCCESSFUL_PURCHASE", player.UserIDString, product.GetProductName(player.UserIDString)));
                        break;
                    case EconomicsType.Item:
                        TakeItem(player.inventory.AllItems(), config.economicsCustomization.economicShortname, (int)price, config.economicsCustomization.economicSkinId);
                        GiveProduct(player, product, amount);
                        UpdateBalance(player, themetype);
                        NotificationUI(player, NotificationType.Success, GetLang("XDSHOP_UI_NOTIFICATION_SUCCESSFUL_PURCHASE", player.UserIDString, product.GetProductName(player.UserIDString)));
                        break;
                }
            }
        }
        #endregion

        #region Selling methods
        private static void TakeItem(Item[] playerItems, string shortname, int amount, ulong skinid = 0, bool blueprint = false)
        {
            List<Item> acceptedItems = Pool.GetList<Item>();
            int itemAmount = 0;

            foreach (Item item in playerItems.Where(x => x != null && (blueprint ? x.blueprintTargetDef?.shortname == shortname : x.info?.shortname == shortname) && (skinid == 0 || x.skin == skinid)))
            {
                if (item.isBroken || item.hasCondition && item.condition < item.info.condition.max) continue;
                acceptedItems.Add(item);
                itemAmount += item.amount;
            }

            foreach (Item use in acceptedItems)
            {
                if (use.amount == amount)
                {
                    use.RemoveFromContainer();
                    use.Remove();
                    amount = 0;
                    break;
                }
                if (use.amount > amount)
                {
                    use.MarkDirty();
                    use.amount -= amount;
                    amount = 0;
                    break;
                }
                if (use.amount < amount)
                {
                    amount -= use.amount;
                    use.RemoveFromContainer();
                    use.Remove();
                }
            }
            Pool.FreeList(ref acceptedItems);
        }
        private static int GetItemAmount(Item[] itemsInput, string shortname, ulong skin, bool blueprint = false)
        {
            List<Item> items = new List<Item>();
            foreach (Item item in itemsInput.Where(x => x != null && (blueprint ? x.blueprintTargetDef?.shortname == shortname : x.info?.shortname == shortname) && (skin == 0 || x.skin == skin)))
            {
                if (item.isBroken || item.hasCondition && item.condition < item.info.condition.max) continue;
                items.Add(item);
            }

            return items.Sum(item => item.amount);
        }
        private void Selling(BasePlayer player, int ItemId, int category, int amount, ThemeType themetype)
        {
            Configuration.CategoryShop.Product product = GetCategories(player)[category].product.Find(x => x.ID == ItemId);
            if (product != null)
            {
                if (IsDuel(player))
                {
                    NotificationUI(player, NotificationType.Error, GetLang("XDSHOP_UI_NOTIFICATION_SELL_IS_DUEL", player.UserIDString));
                    return;
                }
                if (config.mainSettings.raidBlock && IsRaid(player))
                {
                    NotificationUI(player, NotificationType.Error, GetLang("XDSHOP_UI_NOTIFICATION_IS_RAID", player.UserIDString));
                    return;
                }
                Item[] playerItems = player.inventory.AllItems();
                int needAmount = product.AmountSales * amount;
                bool blueprint = product.type == ItemType.Blueprint;
                if (blueprint || product.type == ItemType.CustomItem || product.type == ItemType.Item)
                {
                    if (GetItemAmount(playerItems, product.ShortName, product.SkinID, blueprint) < needAmount)
                    {
                        NotificationUI(player, NotificationType.Warning, GetLang("XDSHOP_UI_NOTIFICATION_NOT_ENOUGH_ITEM", player.UserIDString));
                        return;
                    }
                }

                float price = GetDiscountedPrice(product.PriceSales * amount, GetUserDiscount(player));

                if (product.SellCooldown > 0)
                {
                    int cooldown = GetCooldownTime(player.userID, product, false);
                    if (cooldown > 0)
                    {
                        NotificationUI(player, NotificationType.Warning, GetLang("XDSHOP_UI_NOTIFICATION_SELL_RECHARGE", player.UserIDString, product.GetProductName(player.UserIDString), TimeHelper.FormatTime(TimeSpan.FromSeconds(cooldown), 5, lang.GetLanguage(player.UserIDString))));
                        return;
                    }
                    SetCooldown(player, product, false);
                }

                int limit = GetLimit(player, product, false);
                if (limit != -1)
                {
                    if (limit == 0)
                    {
                        NotificationUI(player, NotificationType.Warning, GetLang("XDSHOP_UI_NOTIFICATION_SELL_LIMIT", player.UserIDString, product.GetProductName(player.UserIDString)));
                        return;
                    }
                    else if (amount > limit)
                    {
                        NotificationUI(player, NotificationType.Warning, GetLang("XDSHOP_UI_NOTIFICATION_SELL_LIMIT_1", player.UserIDString, product.GetProductName(player.UserIDString), limit));
                        return;
                    }
                    AddLimit(player, product, amount, false);
                }

                switch (config.economicsCustomization.typeEconomic)
                {
                    case EconomicsType.Economics:
                        if ((bool)Economics?.Call("Deposit", player.userID, (double)price))
                        {
                            TakeItem(playerItems, product.ShortName, needAmount, product.SkinID, blueprint);
                            UpdateBalance(player, themetype);
                            NotificationUI(player, NotificationType.Success, GetLang("XDSHOP_UI_NOTIFICATION_SUCCESSFUL_SALE", player.UserIDString, product.GetProductName(player.UserIDString)));
                        }
                        break;
                    case EconomicsType.ServerRewards:
                        if (ServerRewards?.Call<object>("AddPoints", player.userID, (int)price) != null)
                        {
                            TakeItem(playerItems, product.ShortName, needAmount, product.SkinID, blueprint);
                            UpdateBalance(player, themetype);
                            NotificationUI(player, NotificationType.Success, GetLang("XDSHOP_UI_NOTIFICATION_SUCCESSFUL_SALE", player.UserIDString, product.GetProductName(player.UserIDString)));
                        }
                        break;
                    case EconomicsType.IQEconomic:
                        IQEconomic?.Call("API_SET_BALANCE", player.userID, (int)price);
                        TakeItem(playerItems, product.ShortName, needAmount, product.SkinID, blueprint);
                        UpdateBalance(player, themetype);
                        NotificationUI(player, NotificationType.Success, GetLang("XDSHOP_UI_NOTIFICATION_SUCCESSFUL_SALE", player.UserIDString, product.GetProductName(player.UserIDString)));
                        break;
                    case EconomicsType.Item:
                        Item item = ItemManager.CreateByName(config.economicsCustomization.economicShortname, (int)price, config.economicsCustomization.economicSkinId);
                        player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
                        TakeItem(playerItems, product.ShortName, needAmount, product.SkinID, blueprint);
                        UpdateBalance(player, themetype);
                        NotificationUI(player, NotificationType.Success, GetLang("XDSHOP_UI_NOTIFICATION_SUCCESSFUL_SALE", player.UserIDString, product.GetProductName(player.UserIDString)));
                        break;
                }
            }
        }

        #endregion

        #region Economics
        private float GetBalance(BasePlayer player)
        {
            switch (config.economicsCustomization.typeEconomic)
            {
                case EconomicsType.Economics:
                    if (!Economics)
                        return 0f;
                    return (float)Economics?.Call<double>("Balance", player.userID);
                case EconomicsType.ServerRewards:
                    if (!ServerRewards)
                        return 0f;
                    return (int)ServerRewards?.Call("CheckPoints", player.userID);
                case EconomicsType.IQEconomic:
                    if (!IQEconomic)
                        return 0f;
                    return (int)IQEconomic?.Call("API_GET_BALANCE", player.userID);
                case EconomicsType.Item:
                    return GetItemAmount(player.inventory.AllItems(), config.economicsCustomization.economicShortname, config.economicsCustomization.economicSkinId);
            }
            return 0f;
        }
        private bool ExistEconomics()
        {
            switch (config.economicsCustomization.typeEconomic)
            {
                case EconomicsType.Economics:
                    if (!Economics)
                        return false;
                    return true;
                case EconomicsType.ServerRewards:
                    if (!ServerRewards)
                        return false;
                    return true;
                case EconomicsType.IQEconomic:
                    if (!IQEconomic)
                        return false;
                    return true;
                case EconomicsType.Item:
                    return true;
            }
            return false;
        }
        #endregion

        #region RefillItems

        private void RefillItems()
        {
            config.product.Clear();
            if (ApiLoadImage != null)
            {
                ServerMgr.Instance.StopCoroutine(ApiLoadImage);
                ApiLoadImage = null;
            }
            Dictionary<string, List<ItemDefinition>> itemDefinitionsList = new Dictionary<string, List<ItemDefinition>>();

            foreach (ItemDefinition itemDefinition in ItemManager.itemList)
            {
                string categoryName = itemDefinition.category.ToString();

                if (itemDefinitionsList.ContainsKey(categoryName))
                    itemDefinitionsList[categoryName].Add(itemDefinition);
                else
                    itemDefinitionsList.Add(categoryName, new List<ItemDefinition> { itemDefinition });
            }
            int itemId = 1;
            foreach (KeyValuePair<string, List<ItemDefinition>> item in itemDefinitionsList)
            {
                Configuration.CategoryShop category = new Configuration.CategoryShop
                {
                    CategoryName = item.Key,
                    PermissionCategory = string.Empty,
                    product = new List<Configuration.CategoryShop.Product>()
                };

                foreach (ItemDefinition itemDef in item.Value)
                {
                    if (_exclude.Contains(itemDef.shortname))
                        continue;
                    double itemPrice = ItemCostCalculator?.Call<double>("GetItemCost", itemDef) ?? 1;

                    category.product.Add(new Configuration.CategoryShop.Product
                    {

                        type = ItemType.Item,
                        ID = itemId++,
                        ShortName = itemDef.shortname,
                        Descriptions = string.Empty,
                        Price = (float)itemPrice,
                        PriceSales = (float)itemPrice / 2.0f,
                        Amount = 1,
                        AmountSales = 1,
                        Name = string.Empty,
                        SkinID = 0,
                        Commands = new List<string>(),
                        Url = string.Empty,
                        KitName = string.Empty,
                        BuyCooldown = 0,
                        SellCooldown = 0,
                        BuyLimits = new Dictionary<string, int>(),
                        SellLimits = new Dictionary<string, int>(),
                        BuyLimitsWipe = new Dictionary<string, int>(),
                        SellLimitsWipe = new Dictionary<string, int>(),

                    });
                }
                config.product.Add(category);
            }
            SaveConfig();
            PrintWarning(GetLang("XDSHOP_SERVICE_CMD_REFILL_SUCCESSFULLY"));
            NextTick(() => { Interface.Oxide.ReloadPlugin(Name); });
        }

        #endregion

        private void CheckOnDuplicates()
        {
            HashSet<int> itemsSeen = new HashSet<int>();
            foreach (Configuration.CategoryShop item in config.product)
                foreach (Configuration.CategoryShop.Product items in item.product)
                    if (!itemsSeen.Add(items.ID))
                    {
                        items.ID = Core.Random.Range(int.MinValue, int.MaxValue);
                    }
        }

        private List<Configuration.CategoryShop> GetCategories(BasePlayer player)
        {
            string npcId = humanNpcPlayerOpen.ContainsKey(player.userID) ? humanNpcPlayerOpen[player.userID] : string.Empty;
            if (npcId == string.Empty)
            {
                return config.product.FindAll(cat => cat != null && (string.IsNullOrEmpty(cat.PermissionCategory) || permission.UserHasPermission(player.UserIDString, cat.PermissionCategory)));
            }
            else
            {
                return config.product.FindAll(cat => cat != null && config.humanNpcs.NPCs.ContainsKey(npcId) && config.humanNpcs.NPCs[npcId].Contains(cat.CategoryName) || config.humanNpcs.NPCs[npcId].Count == 0);
            }
        }
        #endregion

        #region Help
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
                        break;
                    default:
                        result = string.Format("{0}{1}{2}{3}",
                            time.Duration().Days > 0
                                ? $"{time.Days:0} day{(time.Days == 1 ? string.Empty : "s")}, "
                                : string.Empty,
                            time.Duration().Hours > 0
                                ? $"{time.Hours:0} hour{(time.Hours == 1 ? string.Empty : "s")}, "
                                : string.Empty,
                            time.Duration().Minutes > 0
                                ? $"{time.Minutes:0} minute{(time.Minutes == 1 ? string.Empty : "s")}, "
                                : string.Empty,
                            time.Duration().Seconds > 0
                                ? $"{time.Seconds:0} second{(time.Seconds == 1 ? string.Empty : "s")}"
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
                int tmp = units % 10;

                if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                    return $"{units}{form1}";

                if (tmp >= 2 && tmp <= 4)
                    return $"{units}{form2}";

                return $"{units}{form3}";
            }
        }
        private float GetDiscountedPrice(float price, float discount)
        {
            float dicsount = (price * discount / 100);
            return (float)Math.Round(price - dicsount, 2);
        }
        private float GetUserDiscount(BasePlayer player)
        {
            float Discounts = 0f;
            foreach (KeyValuePair<string, float> Discount in config.discountStores.DiscountPerm)
            {
                if (permission.UserHasPermission(player.UserIDString, Discount.Key))
                    if (Discounts < Discount.Value)
                        Discounts = Discount.Value;
            }
            return Discounts;
        }
        #region Classes
        private List<items> ItemList = new List<items>();
        private readonly Dictionary<string, ItemDisplayName> ItemName = new Dictionary<string, ItemDisplayName>();
        public class items
        {
            public string shortName;
            public string ENdisplayName;
            public string RUdisplayName;
        }
        private class ItemDisplayName
        {
            public string ru;
            public string en;
            public string description;
        }
        #endregion
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
                        ItemName.Add(items.shortName, new ItemDisplayName { ru = items.RUdisplayName, en = items.ENdisplayName, description = ItemManager.FindItemDefinition(items.shortName).displayDescription.english });
                    }
                }
                else
                {
                    foreach (ItemDefinition item in ItemManager.itemList)
                    {
                        ItemName.Add(item?.shortname, new ItemDisplayName { ru = item.displayName.english, en = item.displayName.english, description = item.displayDescription.english });
                    }
                }
            }, this);
        }


        #endregion

        #region Cooldowns
        private PlayerCooldown GetCooldown(ulong player)
        {
            PlayerCooldown cooldown;
            return PlayerDataCooldowns.TryGetValue(player, out cooldown) ? cooldown : null;
        }
        private int GetCooldownTime(ulong player, Configuration.CategoryShop.Product item, bool purchase)
        {
            return GetCooldown(player)?.GetCooldownTime(item, purchase) ?? -1;
        }

        private void SetCooldown(BasePlayer player, Configuration.CategoryShop.Product item, bool purchase)
        {
            if ((purchase ? item.BuyCooldown : item.SellCooldown) <= 0)
                return;

            if (PlayerDataCooldowns.ContainsKey(player.userID))
                PlayerDataCooldowns[player.userID].SetCooldown(item, purchase);
            else
                PlayerDataCooldowns.Add(player.userID, new PlayerCooldown().SetCooldown(item, purchase));
        }

        private Dictionary<ulong, PlayerCooldown> PlayerDataCooldowns = new Dictionary<ulong, PlayerCooldown>();

        private class PlayerCooldown
        {
            [JsonProperty(PropertyName = "Player Limits", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public readonly Dictionary<int, CooldownData> ItemsCooldowns = new Dictionary<int, CooldownData>();

            public CooldownData GetCooldown(Configuration.CategoryShop.Product item)
            {
                CooldownData data;
                return ItemsCooldowns.TryGetValue(item.ID, out data) ? data : null;
            }

            public int GetCooldownTime(Configuration.CategoryShop.Product item, bool buy)
            {
                CooldownData data = GetCooldown(item);
                if (data == null)
                    return -1;

                return (int)((buy ? data.Buy : data.Sell).AddSeconds(
                    buy ? item.BuyCooldown : item.SellCooldown) - DateTime.Now).TotalSeconds;
            }
            public PlayerCooldown SetCooldown(Configuration.CategoryShop.Product item, bool buy)
            {
                if (!ItemsCooldowns.ContainsKey(item.ID))
                    ItemsCooldowns.Add(item.ID, new CooldownData());

                if (buy)
                    ItemsCooldowns[item.ID].Buy = DateTime.Now;
                else
                    ItemsCooldowns[item.ID].Sell = DateTime.Now;

                return this;
            }
        }

        private class CooldownData
        {
            public DateTime Buy = new DateTime(1970, 1, 1, 0, 0, 0);

            public DateTime Sell = new DateTime(1970, 1, 1, 0, 0, 0);
        }

        #endregion

        #region Limits

        private PlayerLimits PlayerDataLimits;

        private class PlayerLimits
        {
            [JsonProperty(PropertyName = "List of players", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<ulong, PlayerLimitData> Players = new Dictionary<ulong, PlayerLimitData>();

            public static PlayerLimitData GetOrAddPlayer(ulong playerId)
            {
                if (!Instance.PlayerDataLimits.Players.ContainsKey(playerId))
                    Instance.PlayerDataLimits.Players.Add(playerId, new PlayerLimitData());

                return Instance.PlayerDataLimits.Players[playerId];
            }
        }

        private class PlayerLimitData
        {
            [JsonProperty(PropertyName = "Player Limits", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public readonly Dictionary<int, ItemLimitData> ItemsLimits = new Dictionary<int, ItemLimitData>();

            public void AddItem(Configuration.CategoryShop.Product item, int lots, bool purchase)
            {
                if (!ItemsLimits.ContainsKey(item.ID))
                    ItemsLimits.Add(item.ID, new ItemLimitData());

                if (purchase)
                    ItemsLimits[item.ID].Buy += lots;
                else
                    ItemsLimits[item.ID].Sell += lots;
            }

            public int GetLimit(Configuration.CategoryShop.Product item, bool purchase)
            {
                ItemLimitData data;
                if (ItemsLimits.TryGetValue(item.ID, out data))
                    return purchase ? data.Buy : data.Sell;

                return 0;
            }
            internal class ItemLimitData
            {
                public int Sell;

                public int Buy;
            }
        }


        private void AddLimit(BasePlayer player, Configuration.CategoryShop.Product item, int lots, bool purchase)
        {
            PlayerLimits.GetOrAddPlayer(player.userID).AddItem(item, lots, purchase);
        }
        private int GetLimit(BasePlayer player, Configuration.CategoryShop.Product item, bool purchase)
        {
            int limit = item.GetLimitLotWipe(player, purchase);
            if (limit == 0)
                return -1;

            int used = PlayerLimits.GetOrAddPlayer(player.userID).GetLimit(item, purchase);

            return limit - used;
        }

        #endregion

        #region DATA
        private Dictionary<ulong, ThemeType> PlayerThemeSelect = new Dictionary<ulong, ThemeType>();
        private void SavePlayerData()
        {
            Interface.Oxide.DataFileSystem.WriteObject($"{Name}/Players", PlayerThemeSelect);
        }
        private void SaveLimits()
        {
            Interface.Oxide.DataFileSystem.WriteObject($"{Name}/Player_Limits", PlayerDataLimits);
        }
        private void SaveCooldowns()
        {
            Interface.Oxide.DataFileSystem.WriteObject($"{Name}/Player_Cooldowns", PlayerDataCooldowns);
        }
        private void LoadData()
        {
            try
            {
                PlayerDataCooldowns = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerCooldown>>($"{Name}/Player_Cooldowns");
                PlayerDataLimits = Interface.Oxide.DataFileSystem.ReadObject<PlayerLimits>($"{Name}/Player_Limits");
                PlayerThemeSelect = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, ThemeType>>($"{Name}/Players");
            }
            catch (Exception e)
            {
                PrintError(e.ToString());
            }

            if (PlayerDataCooldowns == null)
                PlayerDataCooldowns = new Dictionary<ulong, PlayerCooldown>();
            if (PlayerDataLimits == null)
                PlayerDataLimits = new PlayerLimits();
            if (PlayerThemeSelect == null)
                PlayerThemeSelect = new Dictionary<ulong, ThemeType>();
        }
        #endregion

        #region Commands
        [ConsoleCommand("UI_HandlerShop")]
        private void CmdConsoleHandler(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player != null && args.HasArgs(1))
            {
                switch (args.Args[0])
                {
                    case "PAGE_CATEGORY":
                        {
                            int page = int.Parse(args.Args[1]);
                            ThemeType theme = (ThemeType)Enum.Parse(typeof(ThemeType), args.Args[2]);
                            int category = int.Parse(args.Args[3]);
                            UICATEGORY(player, theme, page, category);
                            UIPRODUCT(player, theme, 0, category);
                            break;
                        }
                    case "CHANGE_CATEGORY":
                        {
                            int page = int.Parse(args.Args[1]);
                            int category = int.Parse(args.Args[2]);
                            ThemeType theme = (ThemeType)Enum.Parse(typeof(ThemeType), args.Args[3]);
                            CuiHelper.DestroyUi(player, CONFIRMATIONS_PANEL);
                            CuiHelper.DestroyUi(player, CONFIRMATIONS_PANEL_SELLING);
                            UICATEGORY(player, theme, page, category);
                            UIPRODUCT(player, theme, 0, category);
                            break;
                        }
                    case "PAGE_PRODUCT":
                        {
                            int page = int.Parse(args.Args[1]);
                            int category = int.Parse(args.Args[2]);
                            ThemeType theme = (ThemeType)Enum.Parse(typeof(ThemeType), args.Args[3]);
                            CuiHelper.DestroyUi(player, CONFIRMATIONS_PANEL);
                            CuiHelper.DestroyUi(player, CONFIRMATIONS_PANEL_SELLING);
                            UIPRODUCT(player, theme, page, category);
                            break;
                        }
                    case "AMOUNT_CHANGE_PLUS":
                        {
                            int index = int.Parse(args.Args[1]);
                            int indexCategory = int.Parse(args.Args[2]);
                            int amount = int.Parse(args.Args[3]);
                            ThemeType theme = (ThemeType)Enum.Parse(typeof(ThemeType), args.Args[4]);
                            int type = int.Parse(args.Args[5]);
                            if (type == 0)
                            {
                                CuiHelper.DestroyUi(player, CONFIRMATIONS_PANEL);
                                UPDATEAMOUNTUI(player, index, indexCategory, amount + 1, theme);
                            }
                            else
                            {
                                UPDATE_UI_SELLING(player, index, indexCategory, amount + 1, theme);
                            }
                            break;
                        }
                    case "AMOUNT_CHANGE_MINUS":
                        {
                            int index = int.Parse(args.Args[1]);
                            int indexCategory = int.Parse(args.Args[2]);
                            int amount = int.Parse(args.Args[3]);
                            ThemeType theme = (ThemeType)Enum.Parse(typeof(ThemeType), args.Args[4]);
                            int type = int.Parse(args.Args[5]);
                            if (type == 0)
                            {
                                CuiHelper.DestroyUi(player, CONFIRMATIONS_PANEL);
                                UPDATEAMOUNTUI(player, index, indexCategory, amount - 1, theme);
                            }
                            else
                            {
                                UPDATE_UI_SELLING(player, index, indexCategory, amount - 1, theme);
                            }
                            break;
                        }
                    case "PRODUCT_BUY_CONFIRMATION":
                        {
                            int indexItem = int.Parse(args.Args[1]);
                            int indexCategory = int.Parse(args.Args[2]);
                            int amount = int.Parse(args.Args[3]);
                            ThemeType theme = (ThemeType)Enum.Parse(typeof(ThemeType), args.Args[4]);
                            CuiHelper.DestroyUi(player, CONFIRMATIONS_PANEL_SELLING);
                            CONFIRMATIONSUI(player, indexItem, indexCategory, amount, theme);
                            break;
                        }
                    case "PRODUCT_SALE_CONFIRMATION":
                        {
                            int indexItem = int.Parse(args.Args[1]);
                            int indexCategory = int.Parse(args.Args[2]);
                            int amount = int.Parse(args.Args[3]);
                            ThemeType theme = (ThemeType)Enum.Parse(typeof(ThemeType), args.Args[4]);
                            CuiHelper.DestroyUi(player, CONFIRMATIONS_PANEL);
                            CONFIRMATIONSSALEUI(player, indexItem, indexCategory, amount, theme);
                            break;
                        }
                    case "PRODUCT_BUY":
                        {
                            int indexItem = int.Parse(args.Args[1]);
                            int indexCategory = int.Parse(args.Args[2]);
                            int amount = int.Parse(args.Args[3]);
                            ThemeType theme = (ThemeType)Enum.Parse(typeof(ThemeType), args.Args[4]);
                            Purchasing(player, indexItem, indexCategory, amount, theme);
                            CuiHelper.DestroyUi(player, CONFIRMATIONS_PANEL);
                            break;
                        }
                    case "PRODUCT_SALE":
                        {
                            int ItemId = int.Parse(args.Args[1]);
                            int indexCategory = int.Parse(args.Args[2]);
                            int amount = int.Parse(args.Args[3]);
                            ThemeType theme = (ThemeType)Enum.Parse(typeof(ThemeType), args.Args[4]);
                            Selling(player, ItemId, indexCategory, amount, theme);
                            CuiHelper.DestroyUi(player, CONFIRMATIONS_PANEL_SELLING);
                            break;
                        }
                    case "THEME_SWITCH":
                        {
                            ThemeType theme = (ThemeType)Enum.Parse(typeof(ThemeType), args.Args[1]);
                            UIMAIN(player, theme == ThemeType.Dark ? ThemeType.Light : ThemeType.Dark);
                            PlayerThemeSelect[player.userID] = theme == ThemeType.Dark ? ThemeType.Light : ThemeType.Dark;
                            break;
                        }
                    case "CLOSE_ALL_UI":
                        {
                            if (config.humanNpcs.useHumanNpcs && humanNpcPlayerOpen.ContainsKey(player.userID))
                                humanNpcPlayerOpen.Remove(player.userID);
                            if (uiPlayerUseNow.Contains(player.userID))
                                uiPlayerUseNow.Remove(player.userID);
                            CuiHelper.DestroyUi(player, "NOTIFICATION_POLOSA");
                            CuiHelper.DestroyUi(player, "NOTIFICATION_IMG");
                            CuiHelper.DestroyUi(player, "NOTIFICATION_TEXT");
                            CuiHelper.DestroyUi(player, NOTIFICATION_MAIN);
                            CuiHelper.DestroyUi(player, MAIN_LAYER);
                            break;
                        }
                }
            }
        }

        private readonly List<ulong> pdpw = new List<ulong>();

        [ConsoleCommand("shopsystemrevolution_transfer_config_to_xdshop")]
        private void cmdTransferConfig(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null || arg.Connection.authLevel > 0)
            {
                try
                {
                    configOld = Config.ReadObject<ConfigurationOld>($"{Interface.Oxide.ConfigDirectory}{Path.DirectorySeparatorChar}ShopSystemRevolution.json");
                }
                catch (Exception)
                {
                    SendReply(arg, GetLang("XDSHOP_SERVICE_1"));
                    return;
                }

                config.product.Clear();

                if (configOld.itemstores.Count == 0)
                {
                    SendReply(arg, GetLang("XDSHOP_SERVICE_2"));
                    return;
                }
                SendReply(arg, GetLang("XDSHOP_SERVICE_3"));
                int itemId = 1;
                foreach (KeyValuePair<string, List<ConfigurationOld.ItemStores>> oldCategory in configOld.itemstores)
                {
                    Configuration.CategoryShop category = new Configuration.CategoryShop
                    {
                        CategoryName = oldCategory.Key,
                        PermissionCategory = string.Empty,
                        product = new List<Configuration.CategoryShop.Product>()
                    };

                    foreach (ConfigurationOld.ItemStores oldShop in oldCategory.Value)
                    {
                        category.product.Add(new Configuration.CategoryShop.Product
                        {
                            type = oldShop.type,
                            ID = itemId++,
                            ShortName = oldShop.ShortName,
                            Descriptions = string.Empty,
                            Price = oldShop.Price,
                            PriceSales = 0f,
                            Amount = oldShop.Amount,
                            AmountSales = 0,
                            Name = oldShop.Name,
                            SkinID = oldShop.SkinID,
                            Commands = new List<string>() { oldShop.Command },
                            Url = oldShop.Url,
                            KitName = string.Empty,
                            BuyCooldown = 0,
                            SellCooldown = 0,
                            BuyLimits = new Dictionary<string, int>(),
                            SellLimits = new Dictionary<string, int>(),
                            BuyLimitsWipe = new Dictionary<string, int>(),
                            SellLimitsWipe = new Dictionary<string, int>(),
                        });
                    }
                    config.product.Add(category);
                }
                SaveConfig();
                PrintWarning(GetLang("XDSHOP_SERVICE_4"));
                NextTick(() => { Interface.Oxide.ReloadPlugin(Name); });
            }
        }

        [ConsoleCommand("xdshop.refill")]
        private void cmdShopItemsRefill(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null || arg.Connection.authLevel > 0)
            {
                SendReply(arg, GetLang("XDSHOP_SERVICE_CMD_REFILL"));
                ulong userId = arg.Connection == null || arg.IsRcon ? 0U : arg.Connection.userid;
                if (!pdpw.Contains(userId))
                {
                    pdpw.Add(userId);
                    timer.In(15, () =>
                    {
                        if (pdpw.Contains(userId))
                            pdpw.Remove(userId);
                    });
                }
            }
        }

        [ConsoleCommand("xdshop.yes")]
        private void cmdShopItemsRefillYes(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null || arg.Connection.authLevel > 0)
            {
                ulong userId = arg.Connection == null || arg.IsRcon ? 0U : arg.Connection.userid;
                if (pdpw.Contains(userId))
                {
                    PrintWarning(GetLang("XDSHOP_SERVICE_CMD_REFILL_YES"));
                    RefillItems();

                    pdpw.Remove(userId);
                }
            }
        }

        [ConsoleCommand("xdshop.no")]
        private void cmdShopItemsRefillNo(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null || arg.Connection.authLevel > 0)
            {
                ulong userId = arg.Connection == null || arg.IsRcon ? 0U : arg.Connection.userid;
                if (pdpw.Contains(userId))
                {
                    SendReply(arg, GetLang("XDSHOP_SERVICE_CMD_REFILL_NO"));
                    pdpw.Remove(userId);
                }
            }
        }
        #endregion

        #region ApiMethods
        private IEnumerator DownloadImages()
        {
            if (!(bool)ImageLibrary?.Call("HasImage", "blueprintbase" + 60))
                ImageLibrary.Call("AddImage", $"http://api.skyplugins.ru/api/getimage/blueprintbase/120", "blueprintbase" + 60);
            PrintWarning("Loading icon for item....");
            foreach (Configuration.CategoryShop img in config.product)
            {
                for (int i = 0; i < img.product.Count; i++)
                {
                    Configuration.CategoryShop.Product typeimg = img.product[i];
                    if (typeimg.ItemsErorred)
                        continue;
                    if (!string.IsNullOrWhiteSpace(typeimg.Url))
                    {
                        if (!(bool)ImageLibrary?.Call("HasImage", typeimg.Url))
                            ImageLibrary.Call("AddImage", typeimg.Url, typeimg.Url);
                    }
                    else if (typeimg.type == ItemType.CustomItem)
                    {
                        if (!(bool)ImageLibrary?.Call("HasImage", typeimg.ShortName + 60, typeimg.SkinID))
                            ImageLibrary.Call("AddImage", $"http://api.skyplugins.ru/api/getskin/{typeimg.SkinID}/120", typeimg.ShortName + 60, typeimg.SkinID);
                    }
                    else if (typeimg.type == ItemType.Item || typeimg.type == ItemType.Blueprint)
                    {
                        if (!(bool)ImageLibrary?.Call("HasImage", typeimg.ShortName + 60))
                            ImageLibrary.Call("AddImage", $"http://api.skyplugins.ru/api/getimage/{typeimg.ShortName}/120", typeimg.ShortName + 60);
                    }
                    yield return CoroutineEx.waitForSeconds(0.02f);
                }
            }
            PrintWarning("All icon load!");
            ApiLoadImage = null;
            yield return 0;
        }
        private class ImageUi
        {
            private static Coroutine coroutineImg = null;
            private static readonly Dictionary<string, string> Images = new Dictionary<string, string>();
            public static void DownloadImages() { coroutineImg = ServerMgr.Instance.StartCoroutine(AddImage()); }

            private static IEnumerator AddImage()
            {
                for (int i = 0; i < 19; i++)
                {
                    if (Instance == null)
                    {
                        coroutineImg = null;
                        yield break;
                    }
                    string uri = $"https://xdquest.skyplugins.ru/api/getimagexdshop/{i}/b85m858b8nb8n8nb8m8";
                    UnityWebRequest www = UnityWebRequestTexture.GetTexture(uri);
                    yield return www.SendWebRequest();

                    if (www.isNetworkError || www.isHttpError)
                    {
                        if (www.responseCode == 401)
                        {
                            for (int y = 0; y < 3; y++)
                            {
                                Instance.PrintError(Instance.GetLang("XDSHOP_SERVICE_UPDATE_PLUGINS"));
                            }
                            coroutineImg = null;
                            yield break;
                        }
                        Instance.PrintWarning(string.Format("Image download error! Error: {0}, Image name: {1}", www.error, i));
                        www.Dispose();
                        yield break;
                    }
                    Texture2D texture = DownloadHandlerTexture.GetContent(www);
                    if (texture != null)
                    {
                        byte[] bytes = texture.EncodeToPNG();

                        string image = FileStorage.server.Store(bytes, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString();
                        if (!Images.ContainsKey(i.ToString()))
                            Images.Add(i.ToString(), image);
                        else
                            Images[i.ToString()] = image;
                        UnityEngine.Object.DestroyImmediate(texture);
                        yield return CoroutineEx.waitForSeconds(0.01f);
                    }
                    www.Dispose();
                }
                coroutineImg = null;
            }

            public static string GetImage(string ImgKey)
            {
                if (Images.ContainsKey(ImgKey))
                    return Images[ImgKey];
                return Instance.GetImage("LOADING");
            }
            public static void Unload()
            {
                coroutineImg = null;
                foreach (KeyValuePair<string, string> item in Images)
                    FileStorage.server.RemoveExact(uint.Parse(item.Value), FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID, 0U);
            }
        }

        #endregion
    }
}

namespace Oxide.Plugins.XDShopExtensionMethods
{
    public static class ExtensionMethods
    {
        #region Where
        public static HashSet<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            HashSet<TSource> result = new HashSet<TSource>();
            using (IEnumerator<TSource> enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (predicate(enumerator.Current)) result.Add(enumerator.Current);
            return result;
        }
        #endregion

        #region Pagination
        public static IEnumerable<TSource> Page<TSource>(this IEnumerable<TSource> source, int page, int pageSize)
        {
            return source.Skip((page) * pageSize).Take(pageSize);
        }
        #endregion

        #region Take
        public static IEnumerable<TSource> Take<TSource>(this IEnumerable<TSource> source, int count)
        {
            if (count > 0)
            {
                foreach (TSource element in source)
                {
                    yield return element;
                    count--;
                    if (count == 0)
                        break;
                }
            }
        }
        #endregion

        #region Skip
        public static IEnumerable<TSource> Skip<TSource>(this IEnumerable<TSource> source, int count)
        {
            if (source is IList<TSource>)
            {
                IList<TSource> list = (IList<TSource>)source;
                for (int i = count; i < list.Count; i++)
                {
                    yield return list[i];
                }
            }
            else if (source is IList)
            {
                IList list = (IList)source;
                for (int i = count; i < list.Count; i++)
                {
                    yield return (TSource)list[i];
                }
            }
            else
            {
                // .NET framework
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    while (count > 0 && e.MoveNext())
                        count--;
                    if (count <= 0)
                    {
                        while (e.MoveNext())
                            yield return e.Current;
                    }
                }
            }
        }
        #endregion Skip

        #region Sum
        public static int Sum<TSource>(this IList<TSource> source, Func<TSource, int> predicate)
        {
            int result = 0;
            for (int i = 0; i < source.Count; i++)
                result += predicate(source[i]);
            return result;
        }
        #endregion Sum
    }
}