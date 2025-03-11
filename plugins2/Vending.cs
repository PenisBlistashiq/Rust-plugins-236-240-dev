using System;
using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using Newtonsoft.Json.Converters;
using Facepunch;
using VLB;

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oxide.Core.Configuration;

namespace Oxide.Plugins
{
    [Info("Vending", "Frizen", "1.0.0")]
    class Vending : RustPlugin
    {
        static Vending ins;
        PluginConfig config;
        
        public class PluginConfig
        {
            [JsonProperty("Настройка торговых автоматов")]
            public Dictionary<string, Dictionary<string, VendinSetting>> Vending { get; set; }
        }

        public class VendinSetting
        {
            [JsonProperty("Время через которое будет выполняться завоз предметов (в секундах)")]
            public float refillTime;
            [JsonProperty("Настройка товаров")]
            public List<VendingOrder> orders;
        }
         
        public class VendingOrder
        {
            [JsonProperty("Добавить товар?")]
            public bool AddItem;
            [JsonProperty("Название покупаемого товара")]
            public string ItemToBuy { get; set; }
            [JsonProperty("Максимальный стак покупаемого предмета за один раз")]
            public int BuyngItemMaxAmount { get; set; }
            [JsonProperty("Количество покупаемого товара за раз")]
            public ulong BuyngItemSkinID { get; set; }
            [JsonProperty("SKINID покупаемого предмета")]
            public int BuyngItemAmount { get; set; }
            [JsonProperty("Покупаемый товар это чертёж")]
            public bool BuyngItemIsBP { get; set; }
            [JsonProperty("Название платёжного товара")]
            public string PayItemShortName { get; set; }
            [JsonProperty("Количество платёжного товара за раз")]
            public int PayItemAmount { get; set; }
            [JsonProperty("Платёжный товар это чертёж")]
            public bool PayItemIsBP { get; set; }
        }

        protected override void LoadDefaultConfig()
        {
            config = new PluginConfig
            {
                Vending = GetVendingList(),
            };
        }


        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
            if (config.Vending.Count == 0)
            {
                LoadDefaultConfig();
                SaveConfig();
            }
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        private void OnServerInitialized()
        {
            ins = this;
            VendingInit();
        }

        void Unload()
        {
            foreach (var vending in BaseNetworkable.serverEntities.OfType<NPCVendingMachine>().Where(x => x != null && x.OwnerID == 0).ToList())
            {
                vending.InstallFromVendingOrders();
            }
        }

        public Dictionary<string, Dictionary<string, VendinSetting>> GetVendingList()
        {
            Dictionary<string, Dictionary<string, VendinSetting>> vendings = new Dictionary<string, Dictionary<string, VendinSetting>>();
            var vendingList = BaseNetworkable.serverEntities.OfType<NPCVendingMachine>().Where(x => x != null && x.OwnerID == 0).ToList();
            foreach (var vending in vendingList)
            {
                var shopName = vending.shopName;
                if (!vendings.ContainsKey(vending.ShortPrefabName) || !vendings[vending.ShortPrefabName].ContainsKey(shopName))
                {
                    if (!vendings.ContainsKey(vending.ShortPrefabName))
                        vendings.Add(vending.ShortPrefabName, new Dictionary<string, VendinSetting>());
                    vending.InstallFromVendingOrders();
                    if (!vendings[vending.ShortPrefabName].ContainsKey(shopName))
                    {
                        vendings[vending.ShortPrefabName].Add(shopName, new VendinSetting());
                        vendings[vending.ShortPrefabName][shopName].refillTime = 1;
                        vendings[vending.ShortPrefabName][shopName].orders = new List<VendingOrder>();

                        List<VendingOrder> orderlist = new List<VendingOrder>();
                        foreach (var item in vending.vendingOrders.orders)
                        {
                            VendingOrder order = new VendingOrder();
                            order.BuyngItemMaxAmount = 999999;
                            order.ItemToBuy = item.sellItem.shortname;
                            order.BuyngItemAmount = item.sellItemAmount;
                            order.BuyngItemIsBP = item.sellItemAsBP;
                            order.PayItemShortName = item.currencyItem.shortname;
                            order.BuyngItemSkinID = 0;
                            order.PayItemAmount = item.currencyAmount;
                            order.PayItemIsBP = item.currencyAsBP;
                            order.AddItem = true;
                            orderlist.Add(order);
                        }
                        vendings[vending.ShortPrefabName][shopName].orders = orderlist;
                    }
                }

            }

            return vendings;
        }

        void VendingInit()
        {
            var vendingList = BaseNetworkable.serverEntities.OfType<NPCVendingMachine>().Where(x => x != null && x.OwnerID == 0).ToList();

            foreach (var vending in vendingList)
            {
                if (config.Vending.ContainsKey(vending.ShortPrefabName) && config.Vending[vending.ShortPrefabName].ContainsKey(vending.shopName))
                {
                    vending.CancelInvoke(vending.InstallFromVendingOrders);
                    vending.CancelInvoke(vending.Refill);
                    vending.ClearSellOrders();
                    vending.inventory.itemList.Clear();
                    foreach (var item in config.Vending[vending.ShortPrefabName][vending.shopName].orders)
                    {
                        if (!item.AddItem) continue;
                        ItemDefinition itemDefinition = ItemManager.FindItemDefinition(item.ItemToBuy);
                        if (itemDefinition == null)
                        {
                            PrintError($"ItemDefinition from BuyItem ShortName {item.ItemToBuy} not found.");
                            continue;
                        }
                        ItemDefinition x = ItemManager.FindItemDefinition(item.PayItemShortName);
                        if (x == null)
                        {
                            PrintError($"ItemDefinition from PayItem ShortName {item.PayItemAmount} not found.");
                            continue;
                        }
                        AddItemForSale(vending, itemDefinition.itemid, item.BuyngItemAmount, x.itemid, item.PayItemAmount, vending.GetBPState(item.BuyngItemIsBP, item.PayItemIsBP), item.BuyngItemMaxAmount, item.BuyngItemSkinID);
                    }

                    vending.InvokeRepeating(vending.Refill, config.Vending[vending.ShortPrefabName][vending.shopName].refillTime, config.Vending[vending.ShortPrefabName][vending.shopName].refillTime);
                }
            }
        }

        public void AddItemForSale(VendingMachine vending, int itemID, int amountToSell, int currencyID, int currencyPerTransaction, byte bpState, int maxByStack, ulong SkinID)
        {
            vending.AddSellOrder(itemID, amountToSell, currencyID, currencyPerTransaction, bpState);
            vending.transactionActive = true;
            if (bpState == 1 || bpState == 3)
            {
                for (int i = 0; i < maxByStack; i++)
                {
                    global::Item item = ItemManager.CreateByItemID(vending.blueprintBaseDef.itemid, 1, 0UL);
                    item.blueprintTarget = itemID;
                    vending.inventory.Insert(item);
                }
            }   
            else
            {
                var item = ItemManager.Create(ItemManager.FindItemDefinition(itemID), amountToSell * maxByStack, SkinID);
                item.MoveToContainer(vending.inventory);
                //vending.inventory.AddItem(ItemManager.FindItemDefinition(itemID), amountToSell * maxByStack, SkinID);
            }
            vending.transactionActive = false;
            vending.RefreshSellOrderStockLevel(null);
        }
    }
}
