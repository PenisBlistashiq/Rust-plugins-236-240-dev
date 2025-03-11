using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Custom Craft Times", "Camoec", "1.1.1")]
    [Description("Allows you to change the crafting times")]

    public class CustomCraftTimes : RustPlugin
    {
        private const string UsePerm = "CustomCraftTimes.use";

        Dictionary<int, BPItem> _restore = new Dictionary<int, BPItem>();
        private PluginConfig _config;

        private class BPItem
        {
            public string shortname;
            public float time;
        }        
        private class PluginConfig
        {
            public Dictionary<int,BPItem> itemdefinitions = new Dictionary<int, BPItem>();
        }

        private void Init()
        {
            permission.RegisterPermission(UsePerm, this);
        }

        protected override void SaveConfig() => Config.WriteObject(_config, true);
        private void _LoadDefaultConfig()
        {
            Puts("Creating new config file");
            _config = new PluginConfig();
            foreach(var bp in ItemManager.bpList)
            {
                _config.itemdefinitions.Add(bp.targetItem.itemid,new BPItem()
                {
                    shortname = bp.targetItem.shortname,
                    time = bp.time
                });
            }
            SaveConfig();
        }
        private void _LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<PluginConfig>();

                if (_config == null)
                    throw new Exception();

                SaveConfig(); // override posible obsolet / outdated config
            }
            catch (Exception)
            {
                PrintError("Loaded default config");

                _LoadDefaultConfig();
            }
            
        }

        [ConsoleCommand("cct")]
        void GlobalSetup(ConsoleSystem.Arg arg)
        {
            if (arg == null && !arg.IsRcon && (arg.Player() != null && !permission.UserHasPermission(arg.Player().UserIDString, UsePerm)))
                return;
            if (arg.Args == null || arg.Args.Length < 2)
            {
                arg.ReplyWith("Use cct [category] [multiplier]");
                return;
            }

            ItemCategory category = 0;
            float mult = 0;

            if(!float.TryParse(arg.Args[1], out mult))
            {
                arg.ReplyWith("Invalid multiplier!");
            }

            if ((arg.Args[0].ToLower() != "all" && !Enum.TryParse<ItemCategory>(arg.Args[0], out category)))
            {
                string availables = "";
                foreach(var e in Enum.GetValues(typeof(ItemCategory)))
                {
                    availables += $"{e} ";
                }
                arg.ReplyWith($"Invalid Category, try with: {availables}");
                return;
            }

            int affected = 0;
            foreach(var bp in _restore)
            {
                var itemDef = ItemManager.FindItemDefinition(bp.Key);
                if(itemDef.category == category || arg.Args[0].ToLower() == "all")
                {
                    _config.itemdefinitions[bp.Key].time = bp.Value.time * mult;
                    affected++;
                }
            }
            SaveConfig();


            arg.ReplyWith($"{affected} affected items, use 'oxide.reload CustomCraftTimes' to reload times");
        }

        void OnServerInitialized(bool initial)
        {
            _LoadConfig();
            Puts("Loading new times");
            
            foreach (var bp in ItemManager.bpList)
            {
                _restore.Add(bp.targetItem.itemid, new BPItem() { time = bp.time, shortname = bp.name });
                if (_config.itemdefinitions.ContainsKey(bp.targetItem.itemid))
                {
                    bp.time = _config.itemdefinitions[bp.targetItem.itemid].time;
                }
            }
        }

        void Unload()
        {
            if (ItemManager.bpList == null)
                return;
            foreach (var bp in ItemManager.bpList)
            {
                bp.time = _restore[bp.targetItem.itemid].time;
            }
        }
    }
}