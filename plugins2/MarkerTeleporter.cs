using UnityEngine;
using ProtoBuf;
using System.Collections.Generic;
using Oxide.Core.Configuration;
using Oxide.Core;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("MarkerTeleporter", "walkinrey", "1.0.0")]
    class MarkerTeleporter : RustPlugin
    {
        #region References
        DynamicConfigFile data;
        List<string> optionActive = new List<string>();
        string currentStatus;
        #endregion
        #region Hooks
        void OnMapMarkerAdded(BasePlayer player, MapNote note)
        {
            if(optionActive == null || player == null || note == null || !optionActive.Contains(player.userID.ToString()) || !player.IPlayer.HasPermission("markerteleporter.use")) return;
            Vector3 pos = note.worldPosition;
            pos.y = GetCorrectPos(pos);
            player.Teleport(pos);
            SendReply(player, "");
        }
        void Init() 
        {
            permission.RegisterPermission("markerteleporter.use", this); 
            data = Interface.Oxide.DataFileSystem.GetDatafile("MarkerTeleporter"); 
            string stringOptions = (string)data["optionList"]; 
            cmd.AddChatCommand("mt", this, "cmdChat");
            if(stringOptions == null || stringOptions == "") return;
            optionActive = new List<string>(stringOptions.Split(','));
            if(optionActive == null) optionActive = new List<string>();
        }
        #endregion
        #region Helpers
        float GetCorrectPos(Vector3 pos)
        { 
            float y = TerrainMeta.HeightMap.GetHeight(pos); 
            RaycastHit hit; 
            if (Physics.Raycast(new Vector3(pos.x, pos.y + 200f, pos.z), Vector3.down, out hit, Mathf.Infinity, LayerMask.GetMask(new[] { "Terrain", "World", "Default", "Construction", "Deployed" } )) && !hit.collider.name.Contains("rock_cliff")) 
            {
                return Mathf.Max(hit.point.y, y);
            }
            else
            {
                return y;
            }
        }
        #endregion
        #region Methods
        void cmdChat(BasePlayer player, string command, string[] args)
        {
            if(!player.IPlayer.HasPermission("markerteleporter.use")) return;
            if(args == null || args?.Length == 0) 
            {
                if(optionActive.Contains(player.userID.ToString())) currentStatus = "включен.";
                if(!optionActive.Contains(player.userID.ToString())) currentStatus = "выключен.";
                SendReply(player, "Команды плагина MarkerTeleporter\n/mt - открыть информацию по командам\n/mt off - отключить телепорт по карте\n/mt on - включить телепорт по карте\nВ настоящий момент ваш телепорт по карте: " + currentStatus); 
                return;
            }
            switch(args[0].ToLower())
            {
                case "off":
                    DisactivateFunction(player);
                    break;
                case "on":
                    ActivateFunction(player);
                    break;
            }
        }
        void ActivateFunction(BasePlayer player)
        {
            if(optionActive.Contains(player.userID.ToString())) {SendReply(player, "У вас уже включена функция телепорта!"); return;}
            if(!optionActive.Contains(player.userID.ToString())) optionActive.Add(player.userID.ToString());
            string listAsString = string.Join(",", optionActive.ToArray());
            data["optionList"] = listAsString;
            data.Save();
            SendReply(player, "Вы активировали телепорт по карте используя маркер. Чтобы отключить его, введите /mt off");
        }
        void DisactivateFunction(BasePlayer player)
        {
            if(!optionActive.Contains(player.userID.ToString())) {SendReply(player, "У вас уже отключена функция телепорта!"); return;}
            if(optionActive.Contains(player.userID.ToString())) {optionActive.Remove(player.userID.ToString());}
            string listAsString = string.Join(",", optionActive.ToArray());
            data["optionList"] = listAsString;
            data.Save();
            SendReply(player, "Вы отключили телепорт по карте используя маркер. Чтобы включить его, введите /mt on");
        }
        #endregion
    }
}