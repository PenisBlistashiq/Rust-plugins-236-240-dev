using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Rust Lax", "Colon Blow", "1.0.3")]
    [Description("Control when Horses drop dung in minutes")]

    public class RustLax : CovalencePlugin
    {

        #region Load

        private const string permAdmin = "rustlax.admin";
        private const string permMounted = "rustlax.mounted";

        private void Init()
        {
            permission.RegisterPermission(permAdmin, this);
            permission.RegisterPermission(permMounted, this);
            config = Config.ReadObject<PluginConfig>();
        }

        private void OnServerInitialized()
        {
            ProcessExistingAnimals(config.dungTimeGlobal);
        }

        #endregion

        #region Configuration

        private PluginConfig config;

        private class PluginConfig
        {
            [JsonProperty(PropertyName = "Dung Time - Number of Minutes between dung drops : ")] public float dungTimeGlobal { get; set; }
            [JsonProperty(PropertyName = "Dung Time Mounted - Mounted players with rustlax.mounted perms, will change dung drop time to (Minutes) : ")] public float dungTimeMounted { get; set; }
        }

        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                dungTimeGlobal = 15f,
                dungTimeMounted = 1f
            };
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(GetDefaultConfig(), true);
        }

        private void SaveConfig()
        {
            Config.WriteObject(config, true);
        }

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["notallowed"] = "You are not authorized to do that.",
                ["resettxt"] = "All dung droppings have been reset to Rust Defualts"
            }, this);
        }

        #endregion

        #region Commands

        [Command("rustlax.reset")]
        private void cmdRustLaxReset(IPlayer player, string command, string[] args)
        {
            if (player.HasPermission(permAdmin))
            {
                ProcessExistingAnimals(600f);
                player.Message(lang.GetMessage("resettxt", this, player.Id));
                return;
            }
            player.Message(lang.GetMessage("notallowed", this, player.Id));
        }

        #endregion

        #region Hooks

        private void OnEntitySpawned(BaseRidableAnimal animal)
        {
            ProcessAnimal(animal, config.dungTimeGlobal);
        }

        private void OnEntityMounted(BaseMountable entity, BasePlayer player)
        {
            var animal = entity.GetComponentInParent<BaseRidableAnimal>() ?? null;
            if (animal != null && permission.UserHasPermission(player.UserIDString, permMounted)) ProcessAnimal(animal, config.dungTimeMounted);
        }

        private void OnEntityDismounted(BaseMountable entity, BasePlayer player)
        {
            var animal = entity.GetComponentInParent<BaseRidableAnimal>() ?? null;
            if (animal != null && permission.UserHasPermission(player.UserIDString, permMounted)) ProcessAnimal(animal, config.dungTimeGlobal);
        }

        private void ProcessExistingAnimals(float dungAdjustment)
        {
            var animalList = BaseNetworkable.serverEntities.OfType<BaseRidableAnimal>();
            foreach (var ridableAnimal in animalList)
            {
                ProcessAnimal(ridableAnimal, dungAdjustment);
            }
        }

        private void ProcessAnimal(BaseRidableAnimal animal, float dungAdjustment)
        {
            animal.DungProducedPerCalorie = (0.6f / dungAdjustment);
        }

        #endregion
    }
}
