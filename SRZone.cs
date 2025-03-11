using Oxide.Core.Plugins;
using System.Collections.Generic;
using UnityEngine;
using System;
using Rust;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Stop Raid Zone", "King", "1.0.0")]
    public class SRZone : RustPlugin
    {
            [PluginReference] private Plugin 
        ImageLibrary = null, MenuAlerts = null;

        private Single ZoneRaidus = 140f;
        private static SRZone plugin;

        #region [Oxide]
        private void OnServerInitialized()
        {
            plugin = this;

            ImageLibrary?.Call("AddImage", "https://cdn.discordapp.com/attachments/1071443662736732230/1134607859473186916/icons8--50_1.png", "button_sr_zone");
        }

        private void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                PlayerComponent Component = PlayerComponent.GetPlayer(player);
                if (Component != null)
                {
                    MenuAlerts?.Call("RemoveAlertMenu", player, $"{Name}");
                    UnityEngine.Object.Destroy(Component);
                }
            }

            foreach (SRZComponent Component in SphereList)
            {
                UnityEngine.Object.Destroy(Component);
            }
        }
        #endregion

        #region [Rust]
		private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
		{
			if (entity == null || info?.InitiatorPlayer == null) return;
			
			BasePlayer player = info.InitiatorPlayer;
            if (player == null || !IsStopRaidPlayer(player)) return;

            info.damageTypes.ScaleAll(0f);
		}

        private object CanBuild(Planner planner, Construction prefab, Construction.Target target)
        {
            var player = planner.GetOwnerPlayer();
            if (player == null || !IsStopRaidPlayer(player)) return null;

            PlayerComponent component = PlayerComponent.GetPlayer(player);
            if (component != null || component.Component != null)
            {
                player.ChatMessage($"Вы не можете строится в стоп рейде еще: {TimeSpan.FromSeconds(component.Component.ZoneTime - component.Component.TotalTime)}");
                return false;
            }

            return null;
        }

        private object CanMountEntity(BasePlayer player, BaseMountable entity)
        {
            if (player == null || !IsStopRaidPlayer(player) || entity == null) return null;

            PlayerComponent component = PlayerComponent.GetPlayer(player);
            if (component != null || component.Component != null) return false;

            return null;
        }
        #endregion

        #region [ChatCommand]
        [ChatCommand("sz")]
        private void cmdStartZone(BasePlayer player, String command, String[] args)
        {
            if (!player.IsAdmin || args == null || args.Length == 0) return;
            
            switch (args[0])
            {
                case "start":
                {
                    Vector3 position = player.transform.position;
                    position.y = GetGroundPosition(position);
                    Int32 ComponentId = UnityEngine.Random.Range(10000, 99999);
                    GameObject obj = new GameObject();
                    obj.transform.position = position;
                    SRZComponent newComponent = obj.AddComponent<SRZComponent>();
                    newComponent.GetComponent<SRZComponent>().Init(player, ComponentId, Int32.Parse(args[1]));
                    SphereList.Add(newComponent);
                    player.ChatMessage($"Вы успешно начали стоп рейд в квадрате: {GetGrid(position)}. Айди вашего стоп рейда: {ComponentId}.");
                    break;
                }
                case "stop":
                {
                    SRZComponent findComponent = SphereList.FirstOrDefault(p => p.ZoneId == Int32.Parse(args[1]));
                    if (findComponent == null)
                    {
                        player.ChatMessage("Стоп рейд под этим айди не найден.");
                        return;
                    }
                    findComponent.DestroyComp();
                    player.ChatMessage($"Вы успешно отменили стоп рейд под айди: {findComponent.ZoneId}");
                    break;
                }
                case "addtime":
                {
                    SRZComponent findComponent = SphereList.FirstOrDefault(p => p.ZoneId == Int32.Parse(args[1]));
                    if (findComponent == null)
                    {
                        player.ChatMessage("Стоп рейд под этим айди не найден.");
                        return;
                    }
                    findComponent.ZoneTime += Int32.Parse(args[2]);
                    List<PlayerComponent> playerList = PlayerList.Where(p => p.Component == findComponent).ToList();
                    if (playerList.Count > 0)
                    {
                        foreach (PlayerComponent playerComponent in playerList)
                        {
                            MenuAlerts?.Call("UpdateCooldownTimeMenu", playerComponent.player, findComponent.ZoneTime, $"{Name}");
                        }
                    }
                    player.ChatMessage("Вы успешно добавили время стоп рейда!");
                    break;
                }
                case "list":
                {
                    String Text = String.Empty;
                    if (SphereList.Count == 0)
                    {
                        Text += "В данный момент нету активных стоп рейдов!";
                    }
                    else
                    {
                        foreach (SRZComponent Component in SphereList)
                        {
                            Text += $"Айди: {Component.ZoneId}, квадрат {GetGrid(Component.transform.position)}";
                        }
                    }
                    player.ChatMessage(Text);
                    break;
                }
            }
        }
        #endregion

        #region [Sphere-Component]
        private static List<SRZComponent> SphereList = new List<SRZComponent>();

        private class SRZComponent : FacepunchBehaviour
        {
            private BasePlayer playerInit;
            private SphereCollider sphereCollider;
            private BaseEntity sphereEntity;

            public Int32 ZoneId;
            public Int32 TotalTime;
            public Int32 ZoneTime;

            private void Awake()
            {
                gameObject.layer = (int)Layer.Reserved1;
                sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.isTrigger = true;
                sphereCollider.radius = plugin.ZoneRaidus;
                InvokeRepeating(UpdateTime, 1f, 1);
            }

            private void OnTriggerEnter(Collider other)
            {
                BasePlayer target = other.GetComponentInParent<BasePlayer>();
                if (target == null || target.IsAdmin) return;
                
                PlayerComponent.GetPlayer(target).Init(this);
            }

            private void OnTriggerExit(Collider other)
            {
                BasePlayer target = other.GetComponentInParent<BasePlayer>();
                if (target == null && PlayerComponent.GetPlayer(target).Component != this) return;

                PlayerComponent.GetPlayer(target).DInit();
            }

            public void Init(BasePlayer player, Int32 Id, Int32 Time)
            {
                playerInit = player;
                ZoneId = Id;
                ZoneTime = Time;

                String Name = plugin.covalence.Players.FindPlayerById(player.UserIDString)?.Name;
                plugin.Server.Broadcast($"Администратор {Name}, начал стоп рейд по зоне в квадрате: {plugin.GetGrid(transform.position)}");

                SpawnSphere();
            }

            private void SpawnSphere()
            {
                sphereEntity = GameManager.server.CreateEntity("assets/prefabs/visualization/sphere.prefab", transform.position, new Quaternion(), true);
                SphereEntity ball = sphereEntity.GetComponent<SphereEntity>();
                ball.currentRadius = 1f;
                ball.lerpRadius = 2.0f * plugin.ZoneRaidus + 3f;
                ball.lerpSpeed = 150f;
                sphereEntity.Spawn();
            }

            private void DespawnSphere()
            {
                if (sphereEntity != null && !sphereEntity.IsDestroyed)
                    sphereEntity.Kill();
            }

            public void DestroyComp() => OnDestroy();
            private void OnDestroy()
            {
                DespawnSphere();
                CancelInvoke(UpdateTime);
                if (SphereList.Contains(this))
                    SphereList.Remove(this);
                Destroy(this);
            }

            private void UpdateTime()
            {
                TotalTime++;
                if (TotalTime >= ZoneTime)
                {
                    OnDestroy();
                }
            }
        }
        #endregion

        #region [Player-Component]
        private static List<PlayerComponent> PlayerList = new List<PlayerComponent>();

        private class PlayerComponent : FacepunchBehaviour
        {
            public BasePlayer player;
            private Vector3 position;

            public SRZComponent Component;

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
            }

            private void UpdateTime()
            {
                if (Component.TotalTime >= Component.ZoneTime || Component == null)
                {
                    DInit();
                }
                else
                {
                    player.MovePosition(position);
                }
            }

            public void Init(SRZComponent Comp)
            {
                Component = Comp;
                position = player.transform.position;
                plugin.MenuAlerts?.Call("SendAlertMenu", player, Facepunch.Math.Epoch.Current - Component.TotalTime, Component.ZoneTime, "STOP RAID", "Вы зашли в зону стоп рейда.Теперь вам недоступны некоторые функции сервера, телепорт, строительство, трейд и т.д.", true, "button_sr_zone", $"{plugin.Name}");
                InvokeRepeating(UpdateTime, 0f, 0.3f);
                PlayerList.Add(this);
            }

            public void DInit()
            {
                if (player == null)
                {
                    Destroy(this);
                    if (PlayerList.Contains(this))
                        PlayerList.Remove(this);
                    return;
                }

                CancelInvoke(UpdateTime);
                plugin.MenuAlerts?.Call("RemoveAlertMenu", player, $"{plugin.Name}");
                if (PlayerList.Contains(this))
                    PlayerList.Remove(this);
                Destroy(this);
            }

            public static PlayerComponent GetPlayer(BasePlayer player) => player.GetComponent<PlayerComponent>() ?? player.gameObject.AddComponent<PlayerComponent>();
        }
        #endregion

        #region [Position]
        public string GetGrid(Vector3 pos)
        {
            char letter = 'A';
            Single x = Mathf.Floor((pos.x + (ConVar.Server.worldsize / 2)) / 146.3f) % 26;
            Single z = (Mathf.Floor(ConVar.Server.worldsize / 146.3f)) - Mathf.Floor((pos.z + (ConVar.Server.worldsize / 2)) / 146.3f);
            letter = (char)(((int)letter) + x);
            return $"{letter}{z}";
        }

        static float GetGroundPosition(Vector3 pos)
        {
            float y = TerrainMeta.HeightMap.GetHeight(pos);
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(pos.x, pos.y + 200f, pos.z), Vector3.down, out hit, Mathf.Infinity, LayerMask.GetMask(new[] { "Terrain", "World", "Default", "Construction", "Deployed" } )) && !hit.collider.name.Contains("rock_cliff")) 
                return Mathf.Max(hit.point.y, y);
            return y;
        }
        #endregion

        #region [Api]
        private Boolean IsStopRaidPlayer(BasePlayer player) => PlayerComponent.GetPlayer(player)?.Component != null;
        private Boolean IsStopRaid(Vector3 position)
        {
            SRZComponent Component = SphereList.Where(p => Vector3.Distance(p.transform.position, position) < ZoneRaidus).FirstOrDefault();
            if (Component != null)
                return true;
            return false;
        }

        private Boolean IsStopRaid(BasePlayer player)
        {
            Boolean IsStopRaided = IsStopRaidPlayer(player);
            if (IsStopRaided)
                return true;
            return false;
        }
        #endregion
    }
}