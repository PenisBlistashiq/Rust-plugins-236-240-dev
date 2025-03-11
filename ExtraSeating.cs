using System;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins {
    [Info("Extra Seating", "Pho3niX90", "1.0.8")]
    [Description("Allows extra seats on minicopters and horses")]
    class ExtraSeating : RustPlugin {
        #region Config
        public PluginConfig config;
        static ExtraSeating _instance;
        bool debug = false;
        int seats = 0;

        protected override void LoadDefaultConfig() { Config.WriteObject(GetDefaultConfig(), true); }
        public PluginConfig GetDefaultConfig() { return new PluginConfig { EnableMiniSideSeats = true, EnableMiniBackSeat = true, EnableExtraHorseSeat = true }; }
        public class PluginConfig { public bool EnableMiniSideSeats; public bool EnableMiniBackSeat; public bool EnableExtraHorseSeat; }
        #endregion
        private void Init() {
            config = Config.ReadObject<PluginConfig>();
        }

        void LogDebug(string str) {
            if (debug) Puts(str);
        }

        void OnEntitySpawned(BaseNetworkable entity) {
            _instance = this;
            if (entity == null || !(entity is MiniCopter || entity is RidableHorse)) return;
            BaseVehicle vehicle = entity as BaseVehicle;
            seats = vehicle.mountPoints.Count; // default

            if (entity is MiniCopter && entity.ShortPrefabName.Equals("minicopter.entity")) {

                if (_instance.config.EnableMiniSideSeats) seats += 2;
                if (_instance.config.EnableMiniBackSeat) seats += 1;

                if (vehicle.mountPoints.Count < seats)
                    vehicle?.gameObject.AddComponent<Seating>();
            }
            if (entity is RidableHorse) {
                if (_instance.config.EnableExtraHorseSeat) seats += 1;
                if (vehicle.mountPoints.Count < seats)
                    vehicle?.gameObject.AddComponent<Seating>();
            }
        }

        void AddSeat(BaseVehicle ent, Vector3 locPos, Quaternion q) {
            BaseEntity seat = GameManager.server.CreateEntity("assets/prefabs/vehicle/seats/passengerchair.prefab", ent.transform.position, q) as BaseEntity;
            if (seat == null) return;

            seat.SetParent(ent);
            seat.Spawn();
            seat.transform.localPosition = locPos;
            seat.SendNetworkUpdateImmediate(true);
        }

        [ChatCommand("checkmount")]
        private void cmdStats(BasePlayer player, string command, string[] args) {
            RidableHorse entity = player.GetMounted().GetParentEntity() as RidableHorse;
            if (entity == null) {
                _instance.Puts($"entity is null"); return;
            }
            foreach (var s in entity.mountPoints) {
                _instance.Puts($"pos: {s.pos}\n {s.bone}\n {s.mountable}\n IsMounted:{s.mountable.IsMounted()}\n {s.prefab}");
            }
            _instance.Puts($"IsMounted[0]:{entity.mountPoints[0].mountable.IsMounted()}\n IsMounted[1]:{entity.mountPoints[1].mountable.IsMounted()}");
            // _instance.Puts($"HasMountPoints: {entity.HasMountPoints()}");
        }

        BaseVehicle.MountPointInfo CreateMount(Vector3 vec, BaseVehicle.MountPointInfo exampleSeat, Vector3 rotation) {
            return new BaseVehicle.MountPointInfo {
                pos = vec,
                rot = rotation != null ? rotation : new Vector3(0, 0, 0),
                bone = exampleSeat.bone,
                prefab = exampleSeat.prefab,
                mountable = exampleSeat.mountable
            };
        }

        #region Classes
        class HorsePassenger : BaseRidableAnimal {
            override public void PlayerServerInput(InputState inputState, BasePlayer player) {
                if (player.userID == GetDriver().userID) {
                    _instance.Puts("Player is driver");
                    base.PlayerServerInput(inputState, player);
                    return;
                }
                _instance.Puts("Player is NOT driver");
            }
        }

        class Seating : MonoBehaviour {
            public BaseVehicle entity;
            void Awake() {
                entity = GetComponent<BaseVehicle>();
                bool isMini = entity is MiniCopter;
                bool isHorse = entity is RidableHorse;
                Vector3 emptyVector = new Vector3(0, 0, 0);
                if (isMini) {
                    _instance.LogDebug("Minicopter detected");
                }
                if (isHorse) {
                    _instance.LogDebug("Horse detected");
                }

                if (entity == null) { Destroy(this); return; }

                BaseVehicle.MountPointInfo pilot = entity.mountPoints[0];
                //entity.mountPoints.Clear();

                if (entity is RidableHorse) {
                    _instance.LogDebug("Adding passenger seat");
                    Vector3 horseVector = new Vector3(0f, -0.32f, -0.5f);
                    BaseVehicle.MountPointInfo horseBack = _instance.CreateMount(horseVector, pilot, emptyVector);
                    //entity.mountPoints.Add(pilot);
                    entity.mountPoints.Add(horseBack);
                    entity.SendNetworkUpdateImmediate();
                }

                if (entity is MiniCopter) {
                    BaseVehicle.MountPointInfo pFront = entity.mountPoints[1];
                    Vector3 leftVector = new Vector3(0.6f, 0.2f, -0.2f);
                    Vector3 rightVector = new Vector3(-0.6f, 0.2f, -0.2f);
                    Vector3 backVector = new Vector3(0.0f, 0.4f, -1.2f);
                    Vector3 backVector2 = new Vector3(0.0f, 0.4f, -1.45f);

                    Vector3 playerOffsetVector = new Vector3(0f, 0f, -0.25f);
                    Quaternion backQuaternion = Quaternion.Euler(0f, 180f, 0f);

                    if (_instance.config.EnableMiniSideSeats) {
                        _instance.LogDebug("Adding side seats");
                        BaseVehicle.MountPointInfo pLeftSide = _instance.CreateMount(leftVector, pFront, emptyVector);
                        BaseVehicle.MountPointInfo pRightSide = _instance.CreateMount(rightVector, pFront, emptyVector);
                        entity.mountPoints.Add(pLeftSide);
                        entity.mountPoints.Add(pRightSide);
                        _instance.AddSeat(entity, leftVector + playerOffsetVector, new Quaternion());
                        _instance.AddSeat(entity, rightVector + playerOffsetVector, new Quaternion());
                    }

                    if (_instance.config.EnableMiniBackSeat) {
                        _instance.LogDebug("Adding back/rotor seat");
                        BaseVehicle.MountPointInfo pBackReverse = _instance.CreateMount(backVector2, pFront, new Vector3(0f, 180f, 0f));
                        entity.mountPoints.Add(pBackReverse);
                        _instance.AddSeat(entity, backVector, backQuaternion);
                    }
                }

            }
        }
        #endregion
    }
}
