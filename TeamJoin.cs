using CompanionServer.Handlers;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;



namespace Oxide.Plugins
{
    [Info("TeamJoin", "Frizen", "1.0.0")]
    public class TeamJoin : RustPlugin
    {
       
        private Locker _locker;


        [JsonProperty(PropertyName = "Def players", ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<ulong> _defside = new List<ulong>();

        [JsonProperty(PropertyName = "Admin Position", ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<Vector3> positionAdmin = new List<Vector3>();

        [JsonProperty(PropertyName = "Players Position", ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<Vector3> positionplayers = new List<Vector3>();

        [JsonProperty(PropertyName = "Lockers Position", ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<Vector3> lockerspos = new List<Vector3>();

        [JsonProperty(PropertyName = "Lockers List", ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public HashSet<BaseEntity> lockersList = new HashSet<BaseEntity>();




        private RelationshipManager.PlayerTeam TeamPl;
        private RelationshipManager.PlayerTeam TeamAd;
        void OnServerInitialized()
        {
           
            var teamadm = RelationshipManager.Instance.CreateTeam();
            var teampl = RelationshipManager.Instance.CreateTeam();
            TeamPl = teampl;
            TeamAd = teamadm;
            foreach (var players in BasePlayer.activePlayerList)
            {
                if (players.IsAdmin || _defside.Contains(players.userID))
                {
                    TeamAd.AddPlayer(players);
                }
                else
                {
                    TeamPl.AddPlayer(players);
                }
            }
            InvokeHandler.Instance.InvokeRepeating(RefreshTeam, 60f, 60f);
            Server.Command("relationshipmanager.maxteamsize 150");
        }

        
        void SpawnLocker()
        {
            for (int i = 0; i < lockerspos.Count; i++)
            {
                var LockerM = GameManager.server.CreateEntity("assets/prefabs/deployable/locker/locker.deployed.prefab", lockerspos[i], Quaternion.identity);
                LockerM.Spawn();
                lockersList.Add(LockerM);
            }
           
        }



       
            public void Teleport(BasePlayer player, BasePlayer target) => Teleport(player, target.transform.position);
        public void Teleport(BasePlayer player, float x, float y, float z) => Teleport(player, new Vector3(x, y, z));
        public void Teleport(BasePlayer player, Vector3 position)
        {
            if (player.IsDead() && player.IsConnected)
            {
                player.RespawnAt(position, Quaternion.identity);
                return;
            }
            var ret = Interface.Call("CanTeleport", player) as string;
            if (ret != null)
            {
                SendReply(player, ret);
                return;
            }
            BaseMountable mount = player.GetMounted();
            if (mount != null) mount.DismountPlayer(player);
            if (player.net?.connection != null) player.ClientRPCPlayer(null, player, "StartLoading");
            player.StartSleeping();
            player.MovePosition(position);
            if (player.net?.connection != null) player.ClientRPCPlayer(null, player, "ForcePositionTo", position);
            if (player.net?.connection != null) player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate(false);
            if (player.net?.connection == null) return;
            try
            {
                player.ClearEntityQueue(null);
            }
            catch { }
            player.SendFullSnapshot();
        }






        [ChatCommand("event")]
        void cmdCommandMain(BasePlayer player, string command, string[] args)
        {
            var pos = player.transform.position;
            if (args.Length == 0)
            {
                SendReply(player, "Input err");
                return;
            }
            if (player.Connection.authLevel < 2) return;
            switch (args[0])
            {
                case "DefA":
                    string name = args[1];
                    BasePlayer target = FindBasePlayer(name);
                    if (!_defside.Contains(target.userID))
                        _defside.Add(target.userID);
                    break;
                case "DefR":
                    string namer = args[1];
                    BasePlayer targetr = FindBasePlayer(namer);
                    if (!_defside.Contains(targetr.userID))
                        _defside.Remove(targetr.userID);
                    break;
                case "Plpos":
                    if (!positionplayers.Contains(pos))
                        positionplayers.Add(pos);
                    break;
                case "Admpos":
                    if (!positionAdmin.Contains(pos))
                        positionAdmin.Add(pos);
                    SendReply(player, "Успешное добавление позиции");
                    break;
                case "Clearpos":
                    if (positionAdmin != null && positionplayers != null)
                    {
                        positionAdmin.Clear();
                        positionplayers.Clear();
                    }
                    break;
                case "tpall":
                    foreach (var p in BasePlayer.activePlayerList)
                    {
                        if (p != null && !p.IsDead() && !p.IsSleeping() && !p.IsWounded() && !(p == player))
                        {
                            Teleport(p, player);
                        } 
                    }
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
           
            var randomIndexAdm = new System.Random().Next(0, positionAdmin.Count);
            var randomIndexpl = new System.Random().Next(0, positionplayers.Count);
            if (_defside.Contains(player.userID) || player.IsAdmin)
            {
                if (positionAdmin != null)
                    return new BasePlayer.SpawnPoint
                    {
                        pos = positionAdmin[randomIndexAdm]
                    };
            }
            else
            {
                if (positionplayers != null)
                    return new BasePlayer.SpawnPoint
                    {
                        pos = positionplayers[randomIndexpl],
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
            if (team == TeamPl || team == TeamAd)
                return false;
            return null;
        }
            

        void OnPlayerConnected(BasePlayer player)
        {
            if (player.IsAdmin || _defside.Contains(player.userID) && TeamAd != null) 
            {
                TeamAd.AddPlayer(player);
            }
            else if (TeamPl != null)
                TeamPl.AddPlayer(player);
           
        }


        void OnPlayerDisconnected(BasePlayer player)
        {
            if (player.IsAdmin || _defside.Contains(player.userID) && TeamAd != null)
            {
                TeamAd.RemovePlayer(player.userID);
            }
            else if (TeamPl != null)
                TeamPl.RemovePlayer(player.userID);

        }

        void DestroyLocker()
        {
            if (lockersList.Count > 0)
            {
                foreach (var bases in lockersList)
                {
                    if (bases != null && !bases.IsDestroyed)
                        bases.Kill();
                }
                lockersList?.Clear();
            }
        }

        void RefreshTeam()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.IsAdmin || _defside.Contains(player.userID) && TeamAd != null)
                {
                    if(player.Team != TeamAd)
                        TeamAd.AddPlayer(player);
                }
                else if (TeamPl != null && player.Team != TeamPl)
                    TeamPl.AddPlayer(player);
            }
        }
        
        void Unload()
        {
            
            InvokeHandler.Instance.CancelInvoke(RefreshTeam);
            DestroyLocker();
            TeamPl.Disband();
            TeamAd.Disband();
        }




    }
}