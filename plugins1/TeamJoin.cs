using CompanionServer.Handlers;
using Newtonsoft.Json;
using Oxide.Core;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.UIElements;


namespace Oxide.Plugins
{
    [Info("TeamJoin", "Frizen", "1.0.0")]
    public class TeamJoin : RustPlugin
    {
       
        private Locker _locker;

        #region Data

        private PluginData _data;

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name, _data);
        }

        private void LoadData()
        {
            try
            {
                _data = Interface.Oxide.DataFileSystem.ReadObject<PluginData>(Name);
            }
            catch (Exception e)
            {
                PrintError(e.ToString());
            }

            if (_data == null) _data = new PluginData();
        }

        private class PluginData
        {
            [JsonProperty(PropertyName = "Def players", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<ulong> _defside = new List<ulong>();

            [JsonProperty(PropertyName = "Admin Position", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<Vector3> positionAdmin = new List<Vector3>();

            [JsonProperty(PropertyName = "Players Position", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<Vector3> positionplayers = new List<Vector3>();

            [JsonProperty(PropertyName = "Lockers Position", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Vector3 lockerspos;

        }

        #endregion
        private RelationshipManager.PlayerTeam Team;
        void OnServerInitialized()
        {
            LoadData();
           var team = RelationshipManager.Instance.CreateTeam();
           Team = team;
           foreach (var players in BasePlayer.activePlayerList)
           {
               Team.AddPlayer(players);
           }
            InvokeHandler.Instance.InvokeRepeating(RefreshTeam, 60f, 60f);
            Server.Command("relationshipmanager.maxteamsize 150");
        }

        
        void SpawnLocker()
        {
            var LockerM = GameManager.server.CreateEntity("assets/prefabs/deployable/locker/locker.deployed.prefab", _data.lockerspos, Quaternion.identity);
            LockerM.Spawn();
            NextTick(() =>
            {
                _locker = LockerM.gameObject.GetComponent<Locker>();
                var lc = LockerM.gameObject.GetComponent<LootContainer>();
                var itemdef = ItemManager.FindItemDefinition(-194953424);
                lc.inventory.AddItem(itemdef, 1 ,0);
            });

        }

       
       

        [ChatCommand("event")]
        void cmdCommandMain(BasePlayer player, string command, string[] args)
        {
            var pos = player.transform.position;
            if (args.Length == 0 || args.Length != 1)
            {
                SendReply(player, "Input err");
                return;
            }
            switch (args[0])
            {
                case "DefA":
                    string name = args[1];
                    BasePlayer target = FindBasePlayer(name);
                    if (!_data._defside.Contains(target.userID))
                        _data._defside.Add(target.userID);
                    break;
                case "DefR":
                    string namer = args[1];
                    BasePlayer targetr = FindBasePlayer(namer);
                    if (!_data._defside.Contains(targetr.userID))
                        _data._defside.Remove(targetr.userID);
                    break;
                case "Plpos":
                    if (!_data.positionAdmin.Contains(pos))
                        _data.positionAdmin.Add(pos);
                    break;
                case "Admpos":
                    if (!_data.positionAdmin.Contains(pos))
                        _data.positionAdmin.Add(pos);
                    SendReply(player, "Успешное добавление позиции");
                    break;
                case "Clearpos":
                    if (_data.positionAdmin != null && _data.positionplayers != null)
                    {
                        _data.positionAdmin.Clear();
                        _data.positionplayers.Clear();
                    }
                    break;
                case "SetL":
                    _data.lockerspos = pos;
                    SpawnLocker();
                    break;

            }
        }





        public BasePlayer FindBasePlayer(string nameOrUserId)
        {
            nameOrUserId = nameOrUserId.ToLower();
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.displayName.ToLower().Contains(nameOrUserId) || player.UserIDString == nameOrUserId) return player;
            }
            foreach (var player in BasePlayer.sleepingPlayerList)
            {
                if (player.displayName.ToLower().Contains(nameOrUserId) || player.UserIDString == nameOrUserId) return player;
            }
            return default(BasePlayer);
        }

        object OnPlayerRespawn(BasePlayer player)
        {
           
            var randomIndexAdm = new System.Random().Next(0, _data.positionAdmin.Count);
            var randomIndexpl = new System.Random().Next(0, _data.positionplayers.Count);
            if (_data._defside.Contains(player.userID) || player.IsAdmin)
            {
                if (_data.positionAdmin != null)
                    return new BasePlayer.SpawnPoint
                    {
                        pos = _data.positionAdmin[randomIndexAdm]
                    };
            }
            else
            {
                if (_data.positionplayers != null)
                    return new BasePlayer.SpawnPoint
                    {
                        pos = _data.positionplayers[randomIndexAdm],
                    };
            } 

            return null;
        }

        

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hit)
        {
            
            try
            {
                if (entity == null || hit == null) return;
                if (Vector3.Distance(sphereposition, entity.transform.position) < 35)
                {
                    hit.damageTypes.ScaleAll(0f);
                }
                var target = entity as BasePlayer;
                if (hit.InitiatorPlayer == target) return;

               
                if (target.Team == hit.InitiatorPlayer.Team)
                {
                   hit.damageTypes.ScaleAll(0f);
                }  
            }
            catch (NullReferenceException)
            { }
        }

        private Vector3 sphereposition;

        [ChatCommand("r")]
        private void SpherePos(BasePlayer p)
        {
            if (p.net.connection.authLevel < 2)
            {
                return;
            }
            sphereposition = p.GetNetworkPosition();

        }



        object OnTeamLeave(RelationshipManager.PlayerTeam team, BasePlayer player)
        {
            if (player.IsAdmin) return true;
            if (team == Team)
                return false;
            return null;
        }
            

        void OnPlayerConnected(BasePlayer player)
        {
            if(Team != null)
            {
                Team.AddPlayer(player);
            }
        }


        void OnPlayerDisconnected(BasePlayer player)
        {
            if(Team != null)
            {
                if (player.Team == Team)
                {
                    Team.RemovePlayer(player.userID);
                }
            }
           
        }

        void RefreshTeam()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.Team == Team) return;
                Team.AddPlayer(player);
            }
        }
        
        void Unload()
        {
            SaveData();
            InvokeHandler.Instance.CancelInvoke(RefreshTeam);
            _locker?.Kill();
            Team.Disband();
        }




    }
}