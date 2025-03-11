using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info( "Stack Size Controller", "AnExiledGod", "3.4.3" )]
    [Description( "Allows configuration of most items max stack size." )]
    class StackSizeController : CovalencePlugin
    {
        private ConfigData _config;
        private ItemIndex _data;
        private Dictionary<string, int> _vanillaDefaults;

        private readonly List<string> _ignoreList = new List<string>
        {
            "water",
            "water.salt",
            "cardtable",
            "hat.bunnyhat",
            "rustige_egg_e"
        };

        private void Init()
        {
            _config = Config.ReadObject<ConfigData>();
            _data = Interface.Oxide.DataFileSystem.ReadObject<ItemIndex>( nameof( StackSizeController ) );
            _vanillaDefaults =
                Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, int>>( nameof( StackSizeController ) +
                    "_vanilla-defaults" );

            if ( _config == null )
            {
                Puts( "Generating Default Config File." );

                LoadDefaultConfig();
            }

            EnsureConfigIntegrity();

            if ( _data.IsUnityNull() || _data.ItemCategories.IsUnityNull() )
            {
                Puts( "Generating Data File." );

                CreateItemIndex();
                _data.VersionNumber = Version;

                SaveData();
            }

            // Data File Migrations - TODO: Setup full implementation
            if ( _data.VersionNumber <= new VersionNumber( 3, 1, 2 ) )
            {
                Puts( "Datafile version number is less than or equal to v3.1.2. Migration necessary. " +
                     "Backing up old datafile..." );

                Interface.Oxide.DataFileSystem.WriteObject( nameof( StackSizeController ) + "_backup",
                    _data );

                if ( Interface.Oxide.DataFileSystem.ExistsDatafile( nameof( StackSizeController ) ) )
                {
                    foreach ( KeyValuePair<string, List<ItemInfo>> items in _data.ItemCategories )
                    {
                        foreach ( ItemInfo itemInfo in items.Value )
                        {
                            if ( itemInfo.VanillaStackSize == itemInfo.CustomStackSize )
                            {
                                itemInfo.CustomStackSize = 0;
                            }
                        }
                    }

                    _data.VersionNumber = Version;

                    SaveData();

                    Puts( "Datafile migration complete. Notice: Backup files must be manually deleted." );
                }
                else
                {
                    Puts( "Datafile backup failed. Migration failed, report to developer." );
                }
            }

            AddCovalenceCommand( "stacksizecontroller.regendatafile", nameof( RegenerateDataFileCommand ),
                "stacksizecontroller.regendatafile" );
            AddCovalenceCommand( "stacksizecontroller.setstack", nameof( SetStackCommand ),
                "stacksizecontroller.setstack" );
            AddCovalenceCommand( "stacksizecontroller.setstackcat", nameof( SetStackCategoryCommand ),
                "stacksizecontroller.setstackcat" );
            AddCovalenceCommand( "stacksizecontroller.setallstacks", nameof( SetAllStacksCommand ),
                "stacksizecontroller.setallstacks" );
            AddCovalenceCommand( "stacksizecontroller.itemsearch", nameof( ItemSearchCommand ),
                "stacksizecontroller.itemsearch" );
            AddCovalenceCommand( "stacksizecontroller.listcategories", nameof( ListCategoriesCommand ),
                "stacksizecontroller.listcategories" );
            AddCovalenceCommand( "stacksizecontroller.listcategoryitems", nameof( ListCategoryItemsCommand ),
                "stacksizecontroller.listcategoryitems" );
        }

        private void OnServerInitialized()
        {
            if ( _vanillaDefaults.IsUnityNull() || _vanillaDefaults.Count == 0 )
            {
                MaintainVanillaStackSizes();
            }

            SetStackSizes();
        }

        private void OnTerrainInitialized()
        {
            Puts( "Ensuring VanillaStackSize integrity." );

            MaintainVanillaStackSizes( true );
            UpdateItemIndex();
        }

        private void Unloaded()
        {
            if ( _config.RevertStackSizesToVanillaOnUnload )
            {
                RevertStackSizes();
            }
        }

        #region Configuration

        private class ConfigData
        {
            public bool RevertStackSizesToVanillaOnUnload = true;
            public bool AllowStackingItemsWithDurability = true;
            public bool PreventStackingDifferentSkins;
            public bool HidePrefixWithPluginNameInMessages;
            public bool DisableDupeFixAndLeaveWeaponMagsAlone;

            public float GlobalStackMultiplier = 1;
            public Dictionary<string, float> CategoryStackMultipliers = GetCategoriesAndDefaults( 1 )
                .ToDictionary( k => k.Key,
                    k => Convert.ToSingle( k.Value ) );
            public Dictionary<string, float> IndividualItemStackMultipliers = new Dictionary<string, float>();

            public Dictionary<string, int> CategoryStackHardLimits = GetCategoriesAndDefaults( 0 )
                    .ToDictionary( x => x.Key,
                        x => Convert.ToInt32( x.Value ) );
            public Dictionary<string, int> IndividualItemStackHardLimits = new Dictionary<string, int>();

            public VersionNumber VersionNumber;
        }

        protected override void SaveConfig()
        {
            Config.WriteObject( _config );
        }

        protected override void LoadDefaultConfig()
        {
            ConfigData defaultConfig = GetDefaultConfig();
            defaultConfig.VersionNumber = Version;

            Config.WriteObject( defaultConfig );

            _config = Config.ReadObject<ConfigData>();
        }

        private void EnsureConfigIntegrity()
        {
            ConfigData configDefault = new ConfigData();

            if ( _config.RevertStackSizesToVanillaOnUnload == null )
            {
                _config.RevertStackSizesToVanillaOnUnload = configDefault.RevertStackSizesToVanillaOnUnload;
            }

            if ( _config.AllowStackingItemsWithDurability == null )
            {
                _config.AllowStackingItemsWithDurability = configDefault.AllowStackingItemsWithDurability;
            }

            if ( _config.PreventStackingDifferentSkins == null )
            {
                _config.PreventStackingDifferentSkins = configDefault.PreventStackingDifferentSkins;
            }

            if ( _config.HidePrefixWithPluginNameInMessages == null )
            {
                _config.HidePrefixWithPluginNameInMessages = configDefault.HidePrefixWithPluginNameInMessages;
            }

            if ( _config.DisableDupeFixAndLeaveWeaponMagsAlone == null )
            {
                _config.DisableDupeFixAndLeaveWeaponMagsAlone = configDefault.DisableDupeFixAndLeaveWeaponMagsAlone;
            }

            if ( _config.GlobalStackMultiplier == null )
            {
                _config.GlobalStackMultiplier = configDefault.GlobalStackMultiplier;
            }

            if ( _config.CategoryStackMultipliers == null )
            {
                _config.CategoryStackMultipliers = configDefault.CategoryStackMultipliers;
            }

            if ( _config.IndividualItemStackMultipliers == null )
            {
                _config.IndividualItemStackMultipliers = configDefault.IndividualItemStackMultipliers;
            }

            if ( _config.CategoryStackHardLimits == null )
            {
                _config.IndividualItemStackHardLimits = configDefault.IndividualItemStackHardLimits;
            }

            if ( _config.IndividualItemStackHardLimits == null )
            {
                _config.IndividualItemStackHardLimits = configDefault.IndividualItemStackHardLimits;
            }

            _config.VersionNumber = Version;
            SaveConfig();
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData();
        }

        private void UpdateIndividualItemStackMultiplier( int itemId, float multiplier )
        {
            if ( _config.IndividualItemStackMultipliers.ContainsKey( itemId.ToString() ) )
            {
                _config.IndividualItemStackMultipliers[itemId.ToString()] = multiplier;

                SaveConfig();

                return;
            }

            _config.IndividualItemStackMultipliers.Add( GetIndexedItem( itemId ).Shortname, multiplier );

            SaveConfig();
        }

        private void UpdateIndividualItemStackMultiplier( string shortname, float multiplier )
        {
            if ( _config.IndividualItemStackMultipliers.ContainsKey( shortname ) )
            {
                _config.IndividualItemStackMultipliers[shortname] = multiplier;

                SaveConfig();

                return;
            }

            _config.IndividualItemStackMultipliers.Add( shortname, multiplier );

            SaveConfig();
        }

        private void UpdateIndividualItemHardLimit( int itemId, int stackLimit )
        {
            if ( _config.IndividualItemStackHardLimits.ContainsKey( itemId.ToString() ) )
            {
                _config.IndividualItemStackHardLimits[itemId.ToString()] = stackLimit;

                SaveConfig();

                return;
            }

            _config.IndividualItemStackHardLimits.Add( GetIndexedItem( itemId ).Shortname, stackLimit );

            SaveConfig();
        }

        private void UpdateIndividualItemHardLimit( string shortname, int stackLimit )
        {
            if ( _config.IndividualItemStackHardLimits.ContainsKey( shortname ) )
            {
                _config.IndividualItemStackHardLimits[shortname] = stackLimit;

                SaveConfig();

                return;
            }

            _config.IndividualItemStackHardLimits.Add( shortname, stackLimit );

            SaveConfig();
        }

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages( new Dictionary<string, string>
            {
                ["NotEnoughArguments"] = "This command requires {0} arguments.",
                ["InvalidItemShortnameOrId"] =
                    "Item shortname or id is incorrect. Try stacksizecontroller.itemsearch [partial item name]",
                ["InvalidCategory"] = "Category not found. Try stacksizecontroller.listcategories",
                ["OperationSuccessful"] = "Operation completed successfully.",
            }, this );
        }

        private string GetMessage( string key, string playerId )
        {
            if ( _config.HidePrefixWithPluginNameInMessages || playerId == "server_console" )
            {
                return lang.GetMessage( key, this, playerId );
            }

            return $"<color=#ff760d><b>[{nameof( StackSizeController )}]</b></color> " +
                   lang.GetMessage( key, this, playerId );
        }

        #endregion

        #region Data Handling

        private class ItemIndex
        {
            public Dictionary<string, List<ItemInfo>> ItemCategories;
            public VersionNumber VersionNumber;
        }

        private class ItemInfo
        {
            public int ItemId;
            public string Shortname;
            public bool HasDurability;
            public int VanillaStackSize;
            public int CustomStackSize;
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject( nameof( StackSizeController ), _data );
        }

        private void CreateItemIndex()
        {
            _data = new ItemIndex
            {
                ItemCategories = new Dictionary<string, List<ItemInfo>>()
            };

            // Create categories
            foreach ( string category in Enum.GetNames( typeof( ItemCategory ) ) )
            {
                if ( category == "All" ) { continue; }

                _data.ItemCategories.Add( category, new List<ItemInfo>() );
            }

            // Iterate and categorize items
            foreach ( ItemDefinition itemDefinition in ItemManager.GetItemDefinitions() )
            {
                _data.ItemCategories[itemDefinition.category.ToString()].Add(
                    new ItemInfo
                    {
                        ItemId = itemDefinition.itemid,
                        Shortname = itemDefinition.shortname,
                        HasDurability = itemDefinition.condition.enabled,
                        VanillaStackSize = GetVanillaStackSize( itemDefinition ),
                        CustomStackSize = 0
                    } );
            }

            _data.VersionNumber = Version;

            SaveData();
        }

        private void UpdateItemIndex()
        {
            foreach ( ItemDefinition itemDefinition in ItemManager.GetItemDefinitions() )
            {
                if ( !_data.ItemCategories[itemDefinition.category.ToString()]
                    .Exists( itemInfo => itemInfo.ItemId == itemDefinition.itemid ) )
                {
                    _data.ItemCategories[itemDefinition.category.ToString()].Add(
                        new ItemInfo
                        {
                            ItemId = itemDefinition.itemid,
                            Shortname = itemDefinition.shortname,
                            HasDurability = itemDefinition.condition.enabled,
                            VanillaStackSize = GetVanillaStackSize( itemDefinition ),
                            CustomStackSize = 0
                        } );
                }
            }

            SaveData();
        }

        private ItemInfo AddItemToIndex( int itemId )
        {
            ItemDefinition itemDefinition = ItemManager.FindItemDefinition( itemId );

            ItemInfo item = new ItemInfo
            {
                ItemId = itemId,
                Shortname = itemDefinition.shortname,
                HasDurability = itemDefinition.condition.enabled,
                VanillaStackSize = GetVanillaStackSize( itemDefinition ),
                CustomStackSize = 0
            };

            _data.ItemCategories[itemDefinition.category.ToString()].Add( item );

            SaveData();

            return item;
        }

        private ItemInfo GetIndexedItem( int itemId )
        {
            ItemInfo indexedItem = null;
            foreach ( List<ItemInfo> itemInfo in _data.ItemCategories.Values )
            {
                indexedItem = itemInfo.Find( x => x.ItemId == itemId );

                if ( indexedItem != null )
                {
                    break;
                }
            }

            return indexedItem;
        }

        private ItemInfo GetIndexedItem( ItemCategory itemCategory, int itemId )
        {
            ItemInfo itemInfo = _data.ItemCategories[itemCategory.ToString()].First( item => item.ItemId == itemId ) ??
                                AddItemToIndex( itemId );

            return itemInfo;
        }

        private void MaintainVanillaStackSizes( bool refreshDataFile = false )
        {
            SortedDictionary<string, int> vanillaStackSizes = new SortedDictionary<string, int>();

            foreach ( ItemDefinition itemDefinition in ItemManager.GetItemDefinitions() )
            {
                vanillaStackSizes.Add( itemDefinition.shortname, itemDefinition.stackable );

                if ( refreshDataFile )
                {
                    ItemInfo existingItemInfo = _data.ItemCategories[itemDefinition.category.ToString()]
                        .Find( itemInfo => itemInfo.ItemId == itemDefinition.itemid );

                    existingItemInfo.VanillaStackSize = GetVanillaStackSize( itemDefinition );

                    SaveData();
                }
            }

            Interface.Oxide.DataFileSystem.WriteObject( nameof( StackSizeController ) + "_vanilla-defaults",
                vanillaStackSizes );

            _vanillaDefaults = new Dictionary<string, int>( vanillaStackSizes );
        }

        #endregion

        #region Commands

        /*
         * dumpitemlist command
         */
        private void RegenerateDataFileCommand( IPlayer player, string command, string[] args )
        {
            CreateItemIndex();

            player.Reply( GetMessage( "OperationSuccessful", player.Id ) );
        }

        private void SetStackCommand( IPlayer player, string command, string[] args )
        {
            if ( args.Length != 2 )
            {
                player.Reply(
                    string.Format( GetMessage( "NotEnoughArguments", player.Id ), 2 ) );

                return;
            }

            ItemDefinition itemDefinition = ItemManager.FindItemDefinition( args[0] );
            string stackSizeString = args[1];

            if ( itemDefinition == null )
            {
                player.Reply( GetMessage( "InvalidItemShortnameOrId", player.Id ) );

                return;
            }

            if ( stackSizeString.Substring( stackSizeString.Length - 1 ) == "x" )
            {
                UpdateIndividualItemStackMultiplier( itemDefinition.itemid,
                    Convert.ToSingle( stackSizeString.TrimEnd( 'x' ) ) );
                SetStackSizes();
                player.Reply( GetMessage( "OperationSuccessful", player.Id ) );

                return;
            }

            UpdateIndividualItemHardLimit( itemDefinition.shortname, Convert.ToInt32( stackSizeString.TrimEnd( 'x' ) ) );
            SetStackSizes();
            player.Reply( GetMessage( "OperationSuccessful", player.Id ) );
        }

        private void SetAllStacksCommand( IPlayer player, string command, string[] args )
        {
            if ( args.Length != 1 )
            {
                player.Reply(
                    string.Format( GetMessage( "NotEnoughArguments", player.Id ), 1 ) );
            }

            foreach ( string category in _config.CategoryStackMultipliers.Keys.ToList() )
            {
                _config.CategoryStackMultipliers[category] = Convert.ToInt32( args[0] );
            }

            SaveConfig();
            SetStackSizes();

            player.Reply( GetMessage( "OperationSuccessful", player.Id ) );
        }

        private void SetStackCategoryCommand( IPlayer player, string command, string[] args )
        {
            if ( args.Length != 2 )
            {
                player.Reply(
                    string.Format( GetMessage( "NotEnoughArguments", player.Id ), 2 ) );
            }

            ItemCategory itemCategory = (ItemCategory)Enum.Parse( typeof( ItemCategory ), args[0], true );

            if ( itemCategory == null )
            {
                player.Reply( GetMessage( "InvalidCategory", player.Id ) );
            }

            _config.CategoryStackMultipliers[itemCategory.ToString()] = Convert.ToInt32( args[1].TrimEnd( 'x' ) );

            SaveConfig();
            SetStackSizes();

            player.Reply( GetMessage( "OperationSuccessful", player.Id ) );
        }

        private void ItemSearchCommand( IPlayer player, string command, string[] args )
        {
            if ( args.Length != 1 )
            {
                player.Reply(
                    string.Format( GetMessage( "NotEnoughArguments", player.Id ), 1 ) );
            }

            List<ItemDefinition> itemDefinitions = ItemManager.itemList.Where( itemDefinition =>
                     itemDefinition.displayName.english.Contains( args[0] ) ||
                     itemDefinition.displayDescription.english.Contains( args[0] ) ||
                     itemDefinition.shortname.Equals( args[0] ) ||
                     itemDefinition.shortname.Contains( args[0] ) )
                .ToList();

            TextTable output = new TextTable();
            output.AddColumns( "Unique Id", "Shortname", "Category", "Vanilla Stack", "Custom Stack" );

            foreach ( ItemDefinition itemDefinition in itemDefinitions )
            {
                ItemInfo itemInfo = GetIndexedItem( itemDefinition.category, itemDefinition.itemid );

                output.AddRow( itemDefinition.itemid.ToString(), itemDefinition.shortname,
                    itemDefinition.category.ToString(), itemInfo.VanillaStackSize.ToString( "N0" ),
                    Mathf.Clamp( GetStackSize( itemDefinition ), 0, int.MaxValue ).ToString( "N0" ) );
            }

            player.Reply( output.ToString() );
        }

        private void ListCategoriesCommand( IPlayer player, string command, string[] args )
        {
            TextTable output = new TextTable();
            output.AddColumns( "Category Name", "Items In Category" );

            foreach ( string category in Enum.GetNames( typeof( ItemCategory ) ) )
            {
                output.AddRow( category, _data.ItemCategories[category].Count.ToString() );
            }

            player.Reply( output.ToString() );
        }

        private void ListCategoryItemsCommand( IPlayer player, string command, string[] args )
        {
            if ( args.Length != 1 )
            {
                player.Reply( string.Format( GetMessage( "NotEnoughArguments", player.Id ), 1 ) );
            }

            ItemCategory itemCategory = (ItemCategory)Enum.Parse( typeof( ItemCategory ), args[0] );

            if ( itemCategory == null )
            {
                player.Reply( GetMessage( "InvalidCategory", player.Id ) );
            }

            TextTable output = new TextTable();
            output.AddColumns( "Unique Id", "Shortname", "Category", "Vanilla Stack", "Custom Stack", "Multiplier" );

            foreach ( ItemDefinition itemDefinition in ItemManager.GetItemDefinitions()
                .Where( itemDefinition => itemDefinition.category == itemCategory ) )
            {
                ItemInfo itemInfo = GetIndexedItem( itemDefinition.category, itemDefinition.itemid );

                output.AddRow( itemDefinition.itemid.ToString(), itemDefinition.shortname,
                    itemDefinition.category.ToString(), itemInfo.VanillaStackSize.ToString( "N0" ),
                    Mathf.Clamp( GetStackSize( itemDefinition ), 0, int.MaxValue ).ToString( "N0" ),
                    _config.CategoryStackMultipliers[itemDefinition.category.ToString()].ToString() );
            }

            player.Reply( output.ToString() );
        }

        #endregion

        #region Hooks

        object CanMoveItem( Item item, PlayerInventory playerLoot, uint targetContainer, int targetSlot, int amount )
        {
            if ( _config.DisableDupeFixAndLeaveWeaponMagsAlone )
            {
                return null;
            }

            ItemContainer container = playerLoot?.FindContainer( targetContainer ) ?? null;
            Item targetItem = container?.GetSlot( targetSlot ) ?? null;

            if ( item == null || targetItem == null || container == null || !(container is ContainerIOEntity) )
            {
                return null;
            }

            if ( item.contents?.itemList.Count > 0 )
            {
                foreach ( Item containedItem in item.contents.itemList )
                {
                    if ( containedItem.info.itemType == ItemContainer.ContentsType.Liquid ) { continue; }

                    container.AddItem( containedItem.info, containedItem.amount, containedItem.skin );
                    containedItem.Remove();
                }
            }

            // Return contents
            if ( targetItem.info.itemid == item.info.itemid && targetItem.contents?.itemList.Count > 0 )
            {
                foreach ( Item containedItem in targetItem.contents.itemList )
                {
                    if ( containedItem.info.itemType == ItemContainer.ContentsType.Liquid ) { continue; }

                    container.AddItem( containedItem.info, containedItem.amount, containedItem.skin );
                    containedItem.Remove();
                }
            }

            BaseProjectile.Magazine itemMag =
                item.GetHeldEntity()?.GetComponent<BaseProjectile>()?.primaryMagazine;

            // Return ammo
            if ( itemMag != null )
            {
                if ( itemMag.contents > 0 )
                {
                    container.AddItem( itemMag.ammoType, itemMag.contents );

                    itemMag.contents = 0;
                }
            }

            if ( targetItem.GetHeldEntity() is FlameThrower )
            {
                FlameThrower flameThrower = item.GetHeldEntity().GetComponent<FlameThrower>();

                if ( flameThrower.ammo > 0 )
                {
                    container.AddItem( flameThrower.fuelType, flameThrower.ammo );

                    flameThrower.ammo = 0;
                }
            }

            if ( targetItem.GetHeldEntity() is Chainsaw )
            {
                Chainsaw chainsaw = item.GetHeldEntity().GetComponent<Chainsaw>();

                if ( chainsaw.ammo > 0 )
                {
                    container.AddItem( chainsaw.fuelType, chainsaw.ammo );

                    chainsaw.ammo = 0;
                }
            }

            return null;
        }

        private Item OnItemSplit( Item item, int amount )
        {
            if ( _config.DisableDupeFixAndLeaveWeaponMagsAlone )
            {
                return null;
            }

            Item newItem = ItemManager.CreateByItemID( item.info.itemid, amount, item.skin );
            BaseProjectile.Magazine newItemMag =
                newItem.GetHeldEntity()?.GetComponent<BaseProjectile>()?.primaryMagazine;

            if ( newItem.contents?.itemList.Count == 0 &&
                ( _config.DisableDupeFixAndLeaveWeaponMagsAlone || ( newItem.contents?.itemList.Count == 0 && newItemMag?.contents == 0 ) ) )
            {
                return null;
            }

            item.amount -= amount;
            newItem.name = item.name;
            newItem.skin = item.skin;

            if ( item.IsBlueprint() )
            {
                newItem.blueprintTarget = item.blueprintTarget;
            }

            if ( item.info.amountType == ItemDefinition.AmountType.Genetics && item.instanceData != null &&
                item.instanceData.dataInt != 0 )
            {
                newItem.instanceData = new ProtoBuf.Item.InstanceData()
                {
                    dataInt = item.instanceData.dataInt,
                    ShouldPool = false
                };
            }

            // Remove default contents (fuel, etc)
            if ( newItem.contents?.itemList.Count > 0 )
            {
                foreach ( Item containedItem in item.contents.itemList )
                {
                    if ( containedItem.info.itemType == ItemContainer.ContentsType.Liquid ) { continue; }

                    containedItem.Remove();
                }
            }

            item.MarkDirty();

            // Remove default ammo
            if ( newItemMag != null )
            {
                newItemMag.contents = 0;
            }

            if ( newItem.GetHeldEntity() is FlameThrower )
            {
                newItem.GetHeldEntity().GetComponent<FlameThrower>().ammo = 0;
            }

            if ( newItem.GetHeldEntity() is Chainsaw )
            {
                newItem.GetHeldEntity().GetComponent<Chainsaw>().ammo = 0;
            }

            return newItem;
        }

        #endregion

        #region Helpers

        private int GetStackSize( int itemId )
        {
            return GetStackSize( ItemManager.FindItemDefinition( itemId ) );
        }

        private int GetStackSize( ItemDefinition itemDefinition )
        {
            ItemInfo customStackInfo = _data.ItemCategories[itemDefinition.category.ToString()]
                .Find( itemInfo => itemInfo.ItemId == itemDefinition.itemid );

            if ( customStackInfo == null )
            {
                customStackInfo = AddItemToIndex( itemDefinition.itemid );
            }

            if ( _ignoreList.Contains( itemDefinition.shortname ) )
            {
                return GetVanillaStackSize( itemDefinition );
            }

            // Individual Limit set by shortname
            if ( _config.IndividualItemStackHardLimits.ContainsKey( itemDefinition.shortname ) )
            {
                return _config.IndividualItemStackHardLimits[itemDefinition.shortname];
            }

            // Individual Limit set by item id
            if ( _config.IndividualItemStackHardLimits.ContainsKey( itemDefinition.itemid.ToString() ) )
            {
                return _config.IndividualItemStackHardLimits[itemDefinition.itemid.ToString()];
            }

            // Custom stack exists
            if ( customStackInfo.CustomStackSize > 0 )
            {
                return Mathf.RoundToInt( customStackInfo.CustomStackSize * _config.GlobalStackMultiplier );
            }

            // Individual Multiplier set by shortname
            int stackable = _vanillaDefaults.ContainsKey( itemDefinition.shortname ) ? _vanillaDefaults[itemDefinition.shortname] : itemDefinition.stackable;
            if ( _config.IndividualItemStackMultipliers.ContainsKey( itemDefinition.shortname ) )
            {
                return Mathf.RoundToInt( stackable * _config.IndividualItemStackMultipliers[itemDefinition.shortname] );
            }

            // Individual Multiplier set by item id
            if ( _config.IndividualItemStackMultipliers.ContainsKey( itemDefinition.itemid.ToString() ) )
            {
                return Mathf.RoundToInt( stackable * _config.IndividualItemStackMultipliers[itemDefinition.itemid.ToString()] );
            }

            // Category stack limit defined
            if ( _config.CategoryStackHardLimits.ContainsKey( itemDefinition.category.ToString() ) &&
                _config.CategoryStackHardLimits[itemDefinition.category.ToString()] > 0 )
            {
                return _config.CategoryStackHardLimits[itemDefinition.category.ToString()];
            }

            // Category stack multiplier defined
            if ( _config.CategoryStackMultipliers.ContainsKey( itemDefinition.category.ToString() ) &&
                _config.CategoryStackMultipliers[itemDefinition.category.ToString()] > 1.0f )
            {
                return Mathf.RoundToInt(
                    stackable * _config.CategoryStackMultipliers[itemDefinition.category.ToString()] );
            }

            return Mathf.RoundToInt( stackable * _config.GlobalStackMultiplier );
        }

        private int GetVanillaStackSize( ItemDefinition itemDefinition )
        {
            return _vanillaDefaults.ContainsKey( itemDefinition.shortname )
                ? _vanillaDefaults[itemDefinition.shortname]
                : itemDefinition.stackable;
        }

        private void SetStackSizes()
        {
            foreach ( ItemDefinition itemDefinition in ItemManager.GetItemDefinitions() )
            {
                if ( itemDefinition.condition.enabled && !_config.AllowStackingItemsWithDurability )
                {
                    continue;
                }

                if ( _ignoreList.Contains( itemDefinition.shortname ) )
                {
                    continue;
                }

                itemDefinition.stackable = Mathf.Clamp( GetStackSize( itemDefinition ), 1, int.MaxValue );
            }
        }

        private void RevertStackSizes()
        {
            foreach ( ItemDefinition itemDefinition in ItemManager.GetItemDefinitions() )
            {
                itemDefinition.stackable = GetVanillaStackSize( itemDefinition );
            }
        }

        private static Dictionary<string, object> GetCategoriesAndDefaults( object defaultValue )
        {
            Dictionary<string, object> categoryDefaults = new Dictionary<string, object>();

            foreach ( string category in Enum.GetNames( typeof( ItemCategory ) ) )
            {
                categoryDefaults.Add( category, defaultValue );
            }

            return categoryDefaults;
        }

        #endregion
    }
}