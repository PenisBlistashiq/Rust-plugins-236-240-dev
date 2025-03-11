using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;

#region Changelogs and ToDo
/**********************************************************************
* 
* 1.0.0 :       -   Rebranded from AirwolfBlock to VendorBlock
*               -   Added Boat vendor
*               -   Added Stables vendor
*               -   Added permission system
*               -   Language file Updated
*                              
**********************************************************************/
#endregion

namespace Oxide.Plugins
{
    [Info("VendorBlock", "Krungh Crow", "1.0.0")]
    [Description("Disables interaction with the airwolf/boat/stables vendor npc")]
    class VendorBlock : RustPlugin
    {
        #region Variables
        const string Heli_Perm = "vendorblock.heli";
        const string Boat_Perm = "vendorblock.boat";
        const string Horse_Perm = "vendorblock.horse";

        #endregion

        #region LanguageAPI
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["VendorReplyAirwolf"] = "Using the Airwolf Vendor is disabled on this server!",
                ["VendorReplyBoat"] = "Using the Boat Vendor is disabled on this server!",
                ["VendorReplyStables"] = "Using the Horse Vendor is disabled on this server!",
            }, this);
        }
        #endregion

        #region Oxide hooks

        void Init()
        {
            permission.RegisterPermission(Heli_Perm, this);
            permission.RegisterPermission(Boat_Perm, this);
            permission.RegisterPermission(Horse_Perm, this);
        }

        bool? OnNpcConversationStart(VehicleVendor vendor, BasePlayer player, ConversationData conversationData)
        {
            if (conversationData.shortname == "airwolf_heli_vendor" && !permission.UserHasPermission(player.UserIDString, Heli_Perm))
            {
                player.ChatMessage(lang.GetMessage("VendorReplyAirwolf", this, player.UserIDString));
                return false;
            }
            if (conversationData.shortname == "boatvendor" && !permission.UserHasPermission(player.UserIDString, Boat_Perm))
            {
                player.ChatMessage(lang.GetMessage("VendorReplyBoat", this, player.UserIDString));
                return false;
            }
            if (conversationData.shortname == "stablesvendor" && !permission.UserHasPermission(player.UserIDString, Horse_Perm))
            {
                player.ChatMessage(lang.GetMessage("VendorReplyStables", this, player.UserIDString));
                return false;
            }
            return null;
        }
        #endregion
    }
}