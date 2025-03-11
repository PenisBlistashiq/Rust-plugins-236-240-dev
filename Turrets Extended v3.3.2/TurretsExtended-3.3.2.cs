using System;
using System.Collections.Generic;
using Facepunch;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Turrets Extended", "https://discord.gg/9vyTXsJyKR", "3.3.2")]
    [Description("Allows players to toggle on/off the turrets/sam sites without the need of electricity")]
    public class TurretsExtended : RustPlugin
    {
        #region Class Fields

        private readonly Vector3 _turretPos = new Vector3(0f, -0.64f, 0.3f);
        private readonly Vector3 _samPos = new Vector3(0f, -0.6f, -0.92f);
        private readonly Vector3 _npcPos = new Vector3(0f, -0.8f, 0.9f);
        
        private const string SwitchPrefab = "assets/prefabs/deployable/playerioents/simpleswitch/switch.prefab";
        private const string TurretPrefab = "assets/prefabs/npc/autoturret/autoturret_deployed.prefab";
        private const string SamPrefab = "assets/prefabs/npc/sam_site_turret/sam_site_turret_deployed.prefab";

        private const string UsePermission = "turretsextended.use";

        #endregion

        #region Hooks

        private void OnServerInitialized()
        {
            permission.RegisterPermission(UsePermission, this);
            foreach (var turret in UnityEngine.Object.FindObjectsOfType<ContainerIOEntity>())
            {
                if (turret == null)
                {
                    continue;
                }
                
                if (!permission.UserHasPermission(turret.OwnerID.ToString(), UsePermission))
                {
                    continue;
                }
                
                if (turret.OwnerID != 0)
                {
                    AddSwitch(turret);
                }
            }
        }

        private void Unload()
        {
            foreach (var turret in UnityEngine.Object.FindObjectsOfType<ContainerIOEntity>())
            {
                var sw = turret.GetComponentInChildren<ElectricSwitch>();
                if (turret.OwnerID != 0)
                { 
                    if (sw != null) 
                    { 
                        sw.SetParent(null); 
                        sw.Kill();
                    }
                }
            }
        }
        
        private object CanPickupEntity(BasePlayer player, ElectricSwitch entity)
        {
            if (entity.HasParent())
            {
                return false;
            }
            
            return null;
        }
        
        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            if (!permission.UserHasPermission(plan.GetOwnerPlayer().UserIDString, UsePermission))
            {
                return;
            }
            
            if (go.name != TurretPrefab && go.name != SamPrefab)
            {
                return;
            }

            ContainerIOEntity entity = go.GetComponent<ContainerIOEntity>();
            if (entity == null)
            {
                return;
            }
            
            NextTick(() => AddSwitch(entity));
        }
        
        private object OnSwitchToggle(ElectricSwitch switchz, BasePlayer player)
        {
            var entity = switchz.GetComponentInParent<ContainerIOEntity>();
            if (!switchz.HasParent())
            {
                return null;
            }
            
            if (entity is NPCAutoTurret)	
            {	
                return null;	
            }
            
            if (entity is AutoTurret)
            {
                var turret = switchz.GetComponentInParent<AutoTurret>();
                var isAuthed = turret.IsAuthed(player);
                if (entity == null || !player.IsBuildingAuthed() || !isAuthed)
                {
                    if (_config.gameTip)
                    {
                        player.SendConsoleCommand("gametip.showgametip", Lang("NoAuth", player.UserIDString));
                        timer.Once(_config.gameTipTime, () => player.Command("gametip.hidegametip"));
                    }
                    
                    if (_config.chatMessage)
                    {
                        player.ChatMessage(Lang("NoAuthChat", player.UserIDString));
                    }
                    
                    return true;
                }
            }
            
            if (entity == null || !player.IsBuildingAuthed())
            {
                if (_config.gameTip)
                {
                    player.SendConsoleCommand("gametip.showgametip", Lang("NoAuth", player.UserIDString));
                    timer.Once(_config.gameTipTime, () => player.Command("gametip.hidegametip"));
                }

                if (_config.chatMessage)
                {
                    player.ChatMessage(Lang("NoAuthChat", player.UserIDString));
                }
                
                return true;
            }
            
            Toggle(entity);
            return null;
        }
        
        private object OnEntityTakeDamage(ElectricSwitch swtichz, HitInfo info)
        {
            if (swtichz != null)
            {
                var turret = swtichz.GetComponentInParent<AutoTurret>();
                if (turret != null)
                {
                    info.damageTypes.ScaleAll(0.01f);
                    turret.Hurt(info);
                    return true;
                }
            }
            
            return null;
        }
        
        private object OnEntityGroundMissing(ContainerIOEntity turret)
        {
            if (OnGround(turret))
            {
                return true;
            }
            
            return null;
        }

        #endregion

        #region Core Methods

        private void Toggle(ContainerIOEntity entity)
        {
            if (entity is AutoTurret)
            {
                ToggleTurret(entity as AutoTurret);
            }
            else if (entity is SamSite)
            {
                ToggleSam(entity as SamSite);
            }
        }
        
        private void ToggleTurret(AutoTurret turret)
        {
            if (turret.IsOnline())
            {
                turret.SetIsOnline(false);
            }
            else
            {
                turret.SetIsOnline(true);
            }
            
            turret.SendNetworkUpdateImmediate();
        }

        private void ToggleSam(SamSite sam)
        {
            if (sam.IsPowered())
            {
                sam.UpdateHasPower(0, 1);
            }
            else
            {
                sam.UpdateHasPower(sam.ConsumptionAmount(), 1);
            }
            
            sam.SendNetworkUpdateImmediate();
        }

        private void AddSwitch(ContainerIOEntity entity)
        {
            if (entity == null)
            {
                return;
            }
            Vector3 spawnPos = Vector3.zero;
            spawnPos = entity.transform.position;//_turretPos;
            if (entity.name.Contains("autoturret_deployed"))
            {
                if (!_config.enableAutoTurret)
                {
                    return;
                }
                
                
                ElectricSwitch sw = GameManager.server.CreateEntity(SwitchPrefab, spawnPos) as ElectricSwitch;
                if (sw == null)
                {
                    return;
                }
                
                sw.Spawn();
                sw.SetParent(entity);
				sw.transform.localPosition = _turretPos;
                DestroyGroundWatch(sw);
                sw.SetFlag(BaseEntity.Flags.On, entity.IsOn());
                sw.UpdateHasPower(30, 0);
				sw.SendNetworkUpdateImmediate();	
            }
            else if (entity is SamSite)
            {
                if (!_config.enableSamSite)
                {
                    return;
                }
                ElectricSwitch sw = GameManager.server.CreateEntity(SwitchPrefab, spawnPos, Quaternion.Euler(0, 180, 0)) as ElectricSwitch;
                if (sw == null)
                {
                    return;
                }
                
                sw.Spawn();
                sw.SetParent(entity);
				sw.transform.localPosition = _samPos;
                DestroyGroundWatch(sw);
                sw.SetFlag(BaseEntity.Flags.On, entity.IsPowered());
                sw.UpdateHasPower(30, 0);
            }
        }
        
        private void DestroyGroundWatch(ElectricSwitch entity)
        {
            DestroyOnGroundMissing missing = entity.GetComponent<DestroyOnGroundMissing>();
            if (missing != null)
            {
                GameObject.Destroy(missing);
            }
            
            GroundWatch watch = entity.GetComponent<GroundWatch>();
            if (watch != null)
            {
                GameObject.Destroy(watch);
            }
        }

        private bool OnGround(ContainerIOEntity turret)
        {
            BaseEntity component = turret;
            GroundWatch watch = turret.GetComponent<GroundWatch>();
            if (component)
            {
                Construction construction = PrefabAttribute.server.Find<Construction>(component.prefabID);
                if (construction)
                {
                    Socket_Base[] socketBaseArray = construction.allSockets;
                    for (int i = 0; i < socketBaseArray.Length; i++)
                    {
                        SocketMod[] socketModArray = socketBaseArray[i].socketMods;
                        for (int j = 0; j < socketModArray.Length; j++)
                        {
                            SocketMod_AreaCheck socketModAreaCheck = socketModArray[j] as SocketMod_AreaCheck;
                            if (socketModAreaCheck && socketModAreaCheck.wantsInside && !socketModAreaCheck.DoCheck(component.transform.position, component.transform.rotation))
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            List<Collider> list = Pool.GetList<Collider>();
            Vis.Colliders(component.transform.TransformPoint(watch.groundPosition), watch.radius, list, watch.layers);
            List<Collider>.Enumerator enumerator = list.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    BaseEntity baseEntity = enumerator.Current.gameObject.ToBaseEntity();
                    if (baseEntity && (baseEntity == component || baseEntity.IsDestroyed || baseEntity.isClient))
                    {
                        continue;
                    }

                    DecayEntity decayEntity = component as DecayEntity;
                    DecayEntity decayEntity1 = baseEntity as DecayEntity;
                    if (decayEntity && decayEntity.buildingID != 0 && decayEntity1 && decayEntity1.buildingID != 0 &&
                        decayEntity.buildingID != decayEntity1.buildingID)
                    {
                        continue;
                    }

                    Pool.FreeList(ref list);
                    return true;
                }

                Pool.FreeList(ref list);
                return false;
            }
            finally
            {
                ((IDisposable) enumerator).Dispose();
            }
        }

        #endregion

        #region Configuration

        private Configuration _config;

        private class Configuration
        {
            [JsonProperty(PropertyName = "Enable Gametip message")]
            public bool gameTip = true;
            
            [JsonProperty(PropertyName = "Gametip message time")]
            public float gameTipTime = 5f;
            
            [JsonProperty(PropertyName = "Enable Chat message")]
            public bool chatMessage = true;

            [JsonProperty(PropertyName = "Enable Auto Turret")]
            public bool enableAutoTurret = true;

            [JsonProperty(PropertyName = "Enable Sam Site")]
            public bool enableSamSite = true;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new Exception();
                SaveConfig();
            }
            catch
            {
                PrintError("Ваш конфигурационный файл содержит ошибку. Использование значений конфигурации по умолчанию.");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        protected override void LoadDefaultConfig() => _config = new Configuration();

        #endregion
        
        #region Lang

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoAuth"] = "У вас нет права на строительство или вы не имеете права на башню!",
                ["NoAuthChat"] = "<color=#ce422b>У вас нет права на строительство или вы не имеете права на башню!</color>"
            }, this);
        }
        
        #endregion
        
        #region Helpers

        string Lang(string key, string id, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        #endregion
    }
}