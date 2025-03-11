using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Vehicle Decay Protection", "WhiteThunder", "1.0.2")]
    [Description("Protects vehicles from decay around tool cupboards and when recently used.")]
    internal class VehicleDecayProtection : CovalencePlugin
    {
        #region Fields

        private VehicleDecayConfig PluginConfig;

        #endregion

        #region Hooks

        private void Init()
        {
            PluginConfig = Config.ReadObject<VehicleDecayConfig>();
        }

        // Using separate hooks to theoretically improve performance by reducing hook calls
        private object OnEntityTakeDamage(BaseVehicle entity, HitInfo hitInfo) =>
            ProcessDecayDamage(entity, hitInfo);

        private object OnEntityTakeDamage(HotAirBalloon entity, HitInfo hitInfo) =>
            ProcessDecayDamage(entity, hitInfo);

        private object OnEntityTakeDamage(BaseVehicleModule entity, HitInfo hitInfo) =>
            ProcessDecayDamage(entity, hitInfo);

        #endregion

        #region Helper Methods

        private object ProcessDecayDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (!hitInfo.damageTypes.Has(Rust.DamageType.Decay)) return null;

            var vehicleConfig = GetVehicleConfig(entity);
            if (vehicleConfig == null) return null;

            float multiplier = 1;

            var lastUsedTime = GetVehicleLastUsedTime(entity);
            if (lastUsedTime != -1 && Time.time < lastUsedTime + 60 * vehicleConfig.ProtectionMinutesAfterUse)
                multiplier = 0;
            else if (entity.GetBuildingPrivilege() != null)
                multiplier = vehicleConfig.DecayMultiplierNearTC;

            if (multiplier != 1)
            {
                hitInfo.damageTypes.Scale(Rust.DamageType.Decay, multiplier);

                // If no damage, return true to prevent the vehicle being considered attacked (which prevents repair)
                if (!hitInfo.hasDamage)
                    return true;
            }

            return null;
        }

        private VehicleConfig GetVehicleConfig(BaseCombatEntity entity)
        {
            if (entity is HotAirBalloon)
                return PluginConfig.Vehicles.HotAirBalloon;

            // Must go before MiniCopter
            if (entity is ScrapTransportHelicopter)
                return PluginConfig.Vehicles.ScrapTransportHelicopter;

            if (entity is MiniCopter)
                return PluginConfig.Vehicles.Minicopter;

            // Must go before MotorRowboat
            if (entity is RHIB)
                return PluginConfig.Vehicles.RHIB;

            if (entity is MotorRowboat)
                return PluginConfig.Vehicles.Rowboat;

            if (entity is BaseVehicleModule)
                return PluginConfig.Vehicles.ModularCar;

            return null;
        }

        private float GetVehicleLastUsedTime(BaseCombatEntity entity)
        {
            if (entity is HotAirBalloon)
                return (entity as HotAirBalloon).lastBlastTime;

            if (entity is MiniCopter)
                return (entity as MiniCopter).lastEngineTime;

            if (entity is ModularCar)
                return (entity as ModularCar).lastEngineTime;


            if (entity is BaseVehicleModule)
            {
                var car = (entity as BaseVehicleModule).Vehicle as ModularCar;
                if (car != null)
                    return car.lastEngineTime;
            }

            return -1;
        }

        #endregion

        #region Configuration

        protected override void LoadDefaultConfig() => Config.WriteObject(new VehicleDecayConfig(), true);

        internal class VehicleDecayConfig
        {
            [JsonProperty("Vehicles")]
            public VehicleConfigMap Vehicles = new VehicleConfigMap();
        }

        internal class VehicleConfigMap
        {
            [JsonProperty("HotAirBalloon")]
            public VehicleConfig HotAirBalloon = new VehicleConfig();

            [JsonProperty("Minicopter")]
            public VehicleConfig Minicopter = new VehicleConfig();

            [JsonProperty("ModularCar")]
            public VehicleConfig ModularCar = new VehicleConfig();

            [JsonProperty("RHIB")]
            public VehicleConfig RHIB = new VehicleConfig();

            [JsonProperty("Rowboat")]
            public VehicleConfig Rowboat = new VehicleConfig();

            [JsonProperty("ScrapTransportHelicopter")]
            public VehicleConfig ScrapTransportHelicopter = new VehicleConfig();
        }

        internal class VehicleConfig
        {
            [JsonProperty("DecayMultiplierNearTC")]
            public float DecayMultiplierNearTC = 1;

            [JsonProperty("ProtectionMinutesAfterUse")]
            public float ProtectionMinutesAfterUse = 10;
        }

        #endregion
    }
}
