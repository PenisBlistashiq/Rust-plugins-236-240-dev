using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("HBheal", "Frizen", "1.0.0")]
    [Description("Хп бредли и верта")]
    public class HBheal : RustPlugin

    {
        #region config

        public float GlobalDamageMultiplier = 0.5f;

        private Configuration config;

        public class Configuration
        {


            [JsonProperty("Хп танка")] public float bradleyhp = 2000f;



        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            string configPath = $"{Interface.Oxide.ConfigDirectory}{Path.DirectorySeparatorChar}{Name}.json";
            Puts($"Конфиг кривой,создаём новый по пути: {configPath}");
            config = new Configuration();
        }

        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion Configuration

        private void OnBradleyApcInitialize(BradleyAPC bradley)
        {
            bradley._maxHealth = config.bradleyhp;
            bradley.health = bradley._maxHealth;
        }

        private void OnPlayerAttack(BasePlayer attacker, HitInfo hitInfo)
        {
            if (attacker == null || hitInfo?.HitEntity == null) return;

            if (hitInfo?.HitEntity is BaseHelicopter)
            {
                if (GlobalDamageMultiplier != 1f && GlobalDamageMultiplier >= 0)
                {
                    hitInfo?.damageTypes?.ScaleAll(GlobalDamageMultiplier);
                    return;
                }
            }
        }
    }
}