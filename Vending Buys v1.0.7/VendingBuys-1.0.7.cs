using Newtonsoft.Json;
using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;

/* 1.0.7
 * NRE check fixed when removing weapon attachments.
 */

/* 1.0.6
 * Performance and appearance improvements.
 */

namespace Oxide.Plugins
{
    [Info("Vending Buys", "Gt403cyl2", "1.0.7")]
    [Description("Displays and logs detailed information about vending machine purchases.")]
    public class VendingBuys : RustPlugin
    {
        #region Fields

        private const string Admin = "vendingbuys.admin";
        private const string Buyer = "vendingbuys.buyer";
        private const string Seller = "vendingbuys.seller";
        private const string Prefab = "marketterminal";

        #endregion Fields

        #region config

        private ConfigData configData;

        private class ConfigData
        {
            [JsonProperty("Chat Prefix")]
            public string ChatPrefix = "<color=#32CD32>Vending Buys</color>: ";

            [JsonProperty(PropertyName = "Command to clear Vending Buys data file")]
            public string ClearData = "clrvb";

            [JsonProperty(PropertyName = "Ignore NPC Vending Machines (true / false) ")]
            public bool inpc = false;

            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }

        private void Init()
        {
            permission.RegisterPermission(Admin, this);
            permission.RegisterPermission(Buyer, this);
            permission.RegisterPermission(Seller, this);
        }

        protected override void LoadDefaultConfig() => configData = new ConfigData();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                configData = Config.ReadObject<ConfigData>();
                if (configData == null)
                {
                    throw new JsonException();
                }
                if (!configData.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys))
                {
                    PrintWarning("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch
            {
                PrintWarning($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            PrintWarning($"Configuration changes saved to {Name}.json");
            Config.WriteObject(configData, true);
        }

        #endregion config

        #region Data

        private StoredData storedData;

        private class StoredData
        {
            public List<string> Bought = new List<string>();
            public List<string> Sold = new List<string>();
            public List<string> NpcSold = new List<string>();
        }

        private void Loaded()
        {
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("VendingBuys");
            Interface.Oxide.DataFileSystem.WriteObject("VendingBuys", storedData);
            cmd.AddChatCommand(configData.ClearData, this, nameof(Clear));
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject("VendingBuys", storedData);

        private void OnNewSave(string filename)
        {
            storedData.Bought.Clear();
            storedData.Sold.Clear();
            storedData.NpcSold.Clear();
            SaveData();
            Puts("Wipe Detected, All Vending Data Cleared.");
        }

        #endregion Data

        #region Hooks

        private void OnBuyVendingItem(VendingMachine machine, BasePlayer player, int sellOrderId, int numberOfTransactions)
        {
            if (configData.inpc && machine.OwnerID == 0) return;

            var sellOrder = machine.sellOrders.sellOrders[sellOrderId];
            var currency = sellOrder.currencyID;
            var currencyAmount = sellOrder.currencyAmountPerItem * numberOfTransactions;
            var currencyName = ItemManager.FindItemDefinition(currency).displayName.english;
            var item = sellOrder.itemToSellID;
            var itemAmount = sellOrder.itemToSellAmount * numberOfTransactions;
            var itemName = ItemManager.FindItemDefinition(item).displayName.english;

            if (machine.OwnerID.IsSteamId())
            {
                foreach (BasePlayer seller in BasePlayer.activePlayerList)
                {
                    if (seller.userID == machine.OwnerID && player.userID != machine.OwnerID)
                    {
                        if (permission.UserHasPermission(seller.UserIDString, Seller))
                            seller.ChatMessage(VendingBuysMsg("vendingresponse2", player.UserIDString, itemAmount, itemName, machine.shopName, currencyAmount, currencyName, player.displayName));
                    }
                }
            }

            if (machine.OwnerID.IsSteamId())
            {
                foreach (BasePlayer buyer in BasePlayer.allPlayerList)
                {
                    if (buyer.userID == machine.OwnerID)
                    {
                        storedData.Sold.Add(DataMsg("datasold", null, buyer.displayName, buyer.userID, itemAmount, itemName, machine.shopName, currencyAmount, currencyName, player.displayName, player.userID));
                        storedData.Bought.Add(DataMsg("databought", null, player.displayName, player.userID, itemAmount, itemName, machine.shopName, currencyAmount, currencyName, buyer.displayName, buyer.userID));
                        SaveData();
                        if (permission.UserHasPermission(player.UserIDString, Buyer))
                            player.ChatMessage(VendingBuysMsg("vendingresponse", player.UserIDString, itemAmount, itemName, machine.shopName, buyer.displayName, currencyAmount, currencyName));
                    }
                }
            }
            else
            {
                storedData.NpcSold.Add(DataMsg("databoughtnpc", null, machine.shopName, itemAmount, itemName, player.displayName, player.userID, currencyAmount, currencyName));
                SaveData();
                if (permission.UserHasPermission(player.UserIDString, Buyer))
                    player.ChatMessage(VendingBuysMsg("vendingresponse3", player.UserIDString, itemAmount, itemName, machine.shopName, currencyAmount, currencyName));
            }
        }

        private static BasePlayer GetPlayerFromContainer(ItemContainer container, Item item) => item.GetOwnerPlayer() ??
        BasePlayer.activePlayerList.FirstOrDefault(p => p.inventory.loot.IsLooting() && p.inventory.loot.entitySource == container.entityOwner);

        private void OnItemRemovedFromContainer(ItemContainer container, Item item)
        {
            try
            {
                if (item.amount == 0 || item == null || container == null) return;
                var player = GetPlayerFromContainer(container, item);
                var containerOwner = container.GetOwnerPlayer() ?? container.entityOwner;
                string conto = containerOwner.ShortPrefabName;
                if (player && conto == Prefab)
                {
                    if (permission.UserHasPermission(player.UserIDString, Buyer))
                    {
                        player.ChatMessage(VendingBuysMsg("databoughtnpcmarket2", null, item.amount, item.info.displayName.english));
                        storedData.NpcSold.Add(DataMsg("databoughtnpcmarket", null, player.displayName, player.UserIDString, item.amount, item.info.displayName.english, conto));
                        SaveData();
                    }
                }
                else return;
            }
            catch
            {
                return;
            }
        }

        #endregion Hooks

        #region Commands

        private void Clear(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, Admin) || !player.IsAdmin)
            {
                player.ChatMessage(PermsMsg("perms"));
                return;
            }
            else
            {
                storedData.Bought.Clear();
                storedData.Sold.Clear();
                storedData.NpcSold.Clear();
                SaveData();
                Puts($"{player.displayName} has cleared all vending data file.");
                player.ChatMessage(ClrvbMsg("clrvb"));
            }
        }

        #endregion Commands

        #region Lang

        private string VendingBuysMsg(string key, string id = null, params object[] args) => string.Format(configData.ChatPrefix + lang.GetMessage(key, this, id), args);

        private string PermsMsg(string key, string id = null, params object[] args) => string.Format(configData.ChatPrefix + lang.GetMessage(key, this, id), args);

        private string ClrvbMsg(string key, string id = null, params object[] args) => string.Format(configData.ChatPrefix + lang.GetMessage(key, this, id), args);

        private string DataMsg(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"perms","You don't have permission to clear Vending Buys data."},
                {"clrvb","Vending Data has been cleared."},
                {"vendingresponse", "You bought {0} {1}(s) from the vending machine named {2} owned by {3} for {4} {5}."},
                {"vendingresponse2", "You sold {0} {1}(s) from the vending machine named {2} for {3} {4} to {5}."},
                {"vendingresponse3", "You bought {0} {1}(s) from the vending machine named {2} for {3} {4}."},
                {"databoughtnpc", "{0} sold {1} {2}(s) to {3} ({4}) for {5} {6}."},
                {"databoughtnpcmarket", "{0} ({1}) bought {2} {3}(s) from the {4}."},
                {"databoughtnpcmarket2", "You bought {0} {1}(s) from the drone market."},
                {"databought", "{0} ({1}) bought {2} {3}(s) from {4} for {5} {6} from {7} ({8})."},
                {"datasold", "{0} ({1}) sold {2} {3}(s) from {4} for {5} {6} to {7} ({8})."},
            }, this);
        }

        #endregion Lang
    }
}