/*
 ########### README ####################################################
                                                                             
  !!! DON'T EDIT THIS FILE !!!
                                                                     
 ########### CHANGES ###################################################

 1.0.0
    - Plugin release

 #######################################################################
*/

using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Unlock Dlc Items", "rustmods.ru", "1.0.0")]
    [Description("Unlock all DLC items blueprints")]
    class UnlockDlcItems : RustPlugin
    {
        #region [Fields]

        private const string permAllow = "unlockdlcitems.allow";

        private List<ItemBlueprint> dlcItemBlueprintsCache = new List<ItemBlueprint>();

        #endregion

        #region [Oxide Hooks]

        private void OnServerInitialized()
        {
            permission.RegisterPermission(permAllow, this);

            foreach (ItemBlueprint item in ItemManager.bpList)
            {
                if (!item.userCraftable)
                    continue;

                if (!item.NeedsSteamDLC)
                    continue;

                if (!dlcItemBlueprintsCache.Contains(item))
                    dlcItemBlueprintsCache.Add(item);
            }

            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);
        }

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
                ResetDLC(player);
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.Once(1, () => OnPlayerConnected(player));
                return;
            }

            if (!permission.UserHasPermission(player.UserIDString, permAllow))
                return;

            UnlockDLC(player);
        }

        private void OnGroupPermissionGranted(string name, string permName)
        {
            if (permName != permAllow)
                return;

            foreach (var player in BasePlayer.activePlayerList.Where(w => permission.UserHasGroup(w.UserIDString, name)))
            {
                UnlockDLC(player);
            }
        }

        private void OnGroupPermissionRevoked(string name, string permName)
        {
            if (permName != permAllow)
                return;

            foreach (var player in BasePlayer.activePlayerList.Where(w => permission.UserHasGroup(w.UserIDString, name) && !permission.UserHasPermission(w.UserIDString, permAllow)))
            {
                ResetDLC(player);
            }
        }

        private void OnUserPermissionGranted(string id, string permName)
        {
            if (permName != permAllow)
                return;

            var player = BasePlayer.Find(id);
            if (player == null)
                return;

            UnlockDLC(player);
        }

        private void OnUserPermissionRevoked(string id, string permName)
        {
            if (permName != permAllow)
                return;

            var player = BasePlayer.Find(id);
            if (player == null)
                return;

            if (permission.UserHasPermission(player.UserIDString, permAllow))
                return;

            ResetDLC(player);
        }

        #endregion

        #region [Hooks]   

        private void UnlockDLC(BasePlayer player)
        {
            var info = player.PersistantPlayerInfo;
            foreach (ItemBlueprint item in dlcItemBlueprintsCache)
            {
                if (!info.unlockedItems.Contains(item.targetItem.itemid))
                    info.unlockedItems.Add(item.targetItem.itemid);
            }

            player.PersistantPlayerInfo = info;
            player.SendNetworkUpdateImmediate();
            player.ClientRPCPlayer(null, player, "UnlockedBlueprint", 0);
        }

        private void ResetDLC(BasePlayer player)
        {
            var info = player.PersistantPlayerInfo;
            foreach (ItemBlueprint item in dlcItemBlueprintsCache)
            {
                if (!item.targetItem.steamDlc.HasLicense(player.userID) && info.unlockedItems.Contains(item.targetItem.itemid))
                    info.unlockedItems.Remove(item.targetItem.itemid);
            }

            player.PersistantPlayerInfo = info;
            player.SendNetworkUpdateImmediate();
            player.ClientRPCPlayer(null, player, "UnlockedBlueprint", 0);
        }

        #endregion
    }
}