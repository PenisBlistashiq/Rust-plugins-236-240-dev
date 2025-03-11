using System;
using System.Collections.Generic;

using Rust;

using UnityEngine;

using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("No Decay", "0x89A", "1.4.6")]
    [Description("Scales or disables decay of items and deployables")]
    class NoDecay : CovalencePlugin
    {
        private Configuration config;

        void Init() => permission.RegisterPermission(config.General.permission, this);

        void Output(string text)
        {
            if (config.General.Output.rconOutput) Puts(text);
            if (config.General.Output.logToFile) LogToFile(config.General.Output.logFileName, text, this);
        }

        #region -Oxide Hooks-

        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (info == null || info.damageTypes == null || entity == null || !info.damageTypes.Has(DamageType.Decay)) return null;

            BuildingPrivlidge priv = entity.GetBuildingPrivilege();

            if (config.General.usePermission && entity.OwnerID != 0 && !permission.UserHasPermission(priv == null ? entity.OwnerID.ToString() : GetOwnerPlayer(priv, entity.OwnerID), config.General.permission))
            {
                Output(lang.GetMessage("NoPermission", this).Replace("{0}", $"({entity.OwnerID})"));

                return null;
            }
            else if (config.General.usePermission && !config.General.decayNoOwner && entity.OwnerID == 0) return true;

            if (config.General.CupboardSettings.requireTC && !AnyToolCupboards(entity))
            {
                Output(lang.GetMessage("OutOfRange", this).Replace("{0}", entity.ShortPrefabName).Replace("{1}", $"{entity.transform.position}"));
                return null;
            }

            if (entity is BuildingBlock)
            {
                info.damageTypes.ScaleAll(config.buildingMultipliers[(int)((BuildingBlock)entity).grade]);

                Output(lang.GetMessage("DecayBlocked", this).Replace("{0}", entity.ShortPrefabName).Replace("{1}", $"{entity.transform.position}"));
                if (!info.hasDamage) return true;

                return null;
            }

            string matchingType = null;
            if (config.multipliers.ContainsKey(entity.ShortPrefabName) || IsOfType(entity, out matchingType))
            {
                if (config.General.excludeOthers) return null;
                else
                {
                    info.damageTypes.ScaleAll(config.multipliers[matchingType != null ? matchingType : entity.ShortPrefabName]);

                    Output(lang.GetMessage("DecayBlocked", this).Replace("{0}", entity.ShortPrefabName).Replace("{1}", $"{entity.transform.position}"));
                    if (!info.hasDamage) return true;
                }
            }

            if (config.General.disableAll) return true;

            return null;
        }

        object CanMoveItem(Item item, PlayerInventory playerLoot, uint targetContainer, int targetSlot, int amount)
        {
            BaseEntity entity = playerLoot.FindContainer(targetContainer)?.entityOwner;

            if (!(entity is BuildingPrivlidge)) return null;

            bool flag = false;

            //rip target-type conditional expression, fuck C# 7, 9.0 the move
            switch (item.info.shortname)
            {
                case "wood":
                    if (config.General.CupboardSettings.blockWood) flag = true;
                    break;

                case "stones":
                    if (config.General.CupboardSettings.blockStone) flag = true;
                    break;

                case "metal.fragments":
                    if (config.General.CupboardSettings.blockMetal) flag = true;
                    break;

                case "metal.refined":
                    if (config.General.CupboardSettings.blockHighQ) flag = true;
                    break;
            }

            if (flag)
            {
                Output(lang.GetMessage("ItemMoveBlocked", this).Replace("{0}", item.info.shortname).Replace("{1}", $"{entity.ShortPrefabName}"));
                return true;
            }

            return null;
        }

        private bool IsOfType(BaseCombatEntity entity, out string matchingType)
        {
            matchingType = null;

            foreach (var pair in config.multipliers)
            {
                if (pair.Key == entity.GetType()?.Name)
                {
                    matchingType = pair.Key;
                    return true;
                }
            }

            return false;
        }

        private bool AnyToolCupboards(BaseEntity entity)
        {
            if (entity is BuildingBlock)
            {
                BuildingBlock block = (BuildingBlock)entity;

                BuildingManager.Building building = block?.GetBuilding();
                if (building != null && building.GetDominatingBuildingPrivilege() != null)
                    return true;
            }

            Collider[] hits = Physics.OverlapSphere(entity.transform.position, config.General.CupboardSettings.cupboardRange, Layers.Mask.Deployed);

            if (hits.Length > 0)
            {
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i]?.ToBaseEntity() is BuildingPrivlidge)
                        return true;
                }
            }

            return false;
        }

        private string GetOwnerPlayer(BuildingPrivlidge priv, ulong id = 0UL)
        {
            if (priv == null || !priv.AnyAuthed()) return string.Empty;

            if (config.General.CupboardSettings.anyAuthed)
            {
                for (int i = 0; i < priv.authorizedPlayers.Count; i++)
                {
                    var player = priv.authorizedPlayers[i];

                    if (player != null && permission.UserHasPermission(player.userid.ToString(), config.General.permission))
                        return player.userid.ToString();
                }
            }
            else
            {
                var player = priv.authorizedPlayers.Find(x => x.userid == id);

                if (player != null)
                    return player.userid.ToString();
            }

            return string.Empty;
        }

        #endregion

        #region -Configuration-

        private class Configuration
        {
            [JsonProperty(PropertyName = "General")]
            public GeneralSettings General = new GeneralSettings();

            [JsonProperty(PropertyName = "Building grade multipliers")]
            public TierMultipliers BuildingTiers = new TierMultipliers();

            [JsonProperty(PropertyName = "Other multipliers")]
            public Dictionary<string, float> multipliers = new Dictionary<string, float>();

            #region -Classes-

            public class GeneralSettings
            {
                [JsonProperty(PropertyName = "Disable decay for all entities")]
                public bool disableAll = false;

                [JsonProperty(PropertyName = "Exclude \"Other Multipliers\"")]
                public bool excludeOthers = false;

                [JsonProperty(PropertyName = "Use permission")]
                public bool usePermission = true;

                [JsonProperty(PropertyName = "Decay if there is no owner (and perms enabled)")]
                public bool decayNoOwner = false;

                [JsonProperty(PropertyName = "Permission")]
                public string permission = "nodecay.use";

                [JsonProperty(PropertyName = "Output")]
                public OutputClass Output = new OutputClass();

                [JsonProperty(PropertyName = "Cupboard Settings")]
                public CupboardSettingsClass CupboardSettings = new CupboardSettingsClass();

                public class OutputClass
                {
                    [JsonProperty(PropertyName = "Output to server console")]
                    public bool rconOutput = false;

                    [JsonProperty(PropertyName = "Log to file")]
                    public bool logToFile = true;

                    [JsonProperty(PropertyName = "Log file name")]
                    public string logFileName = "NoDecay-Log";
                }

                public class CupboardSettingsClass
                {
                    [JsonProperty(PropertyName = "Disable No Decay if resources placed in TC")]
                    public bool disableOnResources = false;

                    [JsonProperty(PropertyName = "Require Tool Cupboard")]
                    public bool requireTC = false;

                    [JsonProperty(PropertyName = "Any authed on TC")]
                    public bool anyAuthed = false;

                    [JsonProperty(PropertyName = "Cupboard Range")]
                    public float cupboardRange = 30f;

                    [JsonProperty(PropertyName = "Block cupboard wood")]
                    public bool blockWood = false;

                    [JsonProperty(PropertyName = "Block cupboard stone")]
                    public bool blockStone = false;

                    [JsonProperty(PropertyName = "Block cupbard metal")]
                    public bool blockMetal = false;

                    [JsonProperty(PropertyName = "Block cupboard high quality")]
                    public bool blockHighQ = false;
                }
            }

            public class TierMultipliers
            {
                [JsonProperty(PropertyName = "Twig multiplier")]
                public float twig = 1f;

                [JsonProperty(PropertyName = "Wood multiplier")]
                public float wood = 0f;

                [JsonProperty(PropertyName = "Stone multiplier")]
                public float stone = 0f;

                [JsonProperty(PropertyName = "Sheet Metal multiplier")]
                public float metal = 0f;

                [JsonProperty(PropertyName = "Armoured multiplier")]
                public float armoured = 0f;
            }

            #endregion -Classes-

            [JsonIgnore]
            public float[] buildingMultipliers;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) throw new Exception();
                config.buildingMultipliers = new float[5] { config.BuildingTiers.twig, config.BuildingTiers.wood, config.BuildingTiers.stone, config.BuildingTiers.metal, config.BuildingTiers.armoured };
                SaveConfig();
            }
            catch
            {
                PrintError("Error loading config, using default values");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig() => config = new Configuration();

        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion -Configuration-

        #region -Localisation-

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "{0} does not have permission",
                ["ItemMoveBlocked"] = "{0} was blocked from being added to TC at {1}",
                ["DecayBlocked"] = "Decay was overriden on {0} at {1}",
                ["OutOfRange"] = "{0} was out of TC range, at {1}",
            }, this);
        }

        #endregion -Localisation-
    }
}
