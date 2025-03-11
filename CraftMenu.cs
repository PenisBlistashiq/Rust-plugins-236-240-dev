using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using UnityEngine;
using Oxide.Game.Rust.Cui;
using Facepunch;
using Network;
using VLB;

namespace Oxide.Plugins
{
    [Info("CraftMenu", "David", "1.0.91")]
    [Description("Simple craft menu for uncraftable items.")]

    public class CraftMenu : RustPlugin
    {   
        #region [Fields]

        int _bpsTotal = 0;
        int _bpsDefault = 0;
        string click = "assets/bundled/prefabs/fx/notice/item.select.fx.prefab";
        string pageChange = "assets/bundled/prefabs/fx/notice/loot.copy.fx.prefab";
        string research = "assets/prefabs/deployable/research table/effects/research-success.prefab";
        string craft = "assets/bundled/prefabs/fx/notice/loot.start.fx.prefab";


        //category button anchors
        string[] ca = {
            "0.01 0", "0.075 1", //default all cat
            "0.09 0", "0.19 1",
            "0.19 0", "0.29 1",
            "0.29 0", "0.39 1",
            "0.39 0", "0.49 1",
            "0.49 0", "0.59 1",
            "0.59 0", "0.69 1" 
        };
        //assets img anchors
        string[] ia = {
            "0.22 0.17", "0.78 0.83",   // default
            "0.22 0.17", "0.78 0.83",   // assets/icons/construction.png = 0 1
            "0.24 0.14", "0.76 0.82",   // assets/icons/clothing.png = 2 3 
            "0.24 0.14", "0.76 0.82",   // assets/icons/bullet.png = 4 5
            "0.24 0.14", "0.76 0.82"    // assets/icons/medical.png = 6 7
        };
        //blueprint anchors
        private string[] ba = {
            "indexing",
            "0.0 0.85", "1 1",
            "0.0 0.69", "1 0.84",
            "0.0 0.53", "1 0.68",
            "0.0 0.36", "1 0.52",
            "0.0 0.2", "1 0.35",
            "0.0 0.04", "1 0.19"
        };

        #endregion

        #region [Hooks]

        private void OnServerInitialized()
        {   
            LoadConfig();
            LoadData();
            LoadPlayerData();  
            LoadNamesData(); 
            DownloadImages();
            ImageQueCheck();
            //update config for 1.0.4
            if (config.main.perms == null)
            {
                config.main.perms = false;
                SaveConfig();
            }
            //check config
            if (config.ct == null)
            {   
                Puts($"\n*******************************************************************\nConfig update is required. Make backup of your old one, delete it from config folder and reload plugin again.\n*******************************************************************");
                Interface.Oxide.UnloadPlugin("CraftMenu");
                return;
            }

            RegisterPerms();

            permission.RegisterPermission($"craftmenu.use", this); 

            //count bps
            _bpsTotal = bps.Count(); 
            foreach (string bp in bps.Keys)
            {
                if (bps[bp].ResearchCost == 0)
                _bpsDefault++;
            }

            AddMonoComponent();
        }

        void Unload() 
        { 
            foreach (var _player in BasePlayer.activePlayerList)
                DestroyCui(_player); 

            DestroyMonoComponent();
        }

        void OnNewSave()
        {   
            if (config.main.wipe)
                playerBps.Clear();

            SavePlayerData();
        }

        void OnPlayerDisconnected(BasePlayer player)
        {   
            if (!config.ct.craftQue) return;
            var run = player.GetComponent<CraftingQue>(); 
            if (run == null) 
                return;
            else
                run.CancelAll(player);
         
            UnityEngine.Object.Destroy(run);
        }

        void OnPlayerConnected(BasePlayer player) 
        {   
            if (config.ct.craftQue)
                player.gameObject.GetOrAddComponent<CraftingQue>();   
        }
        

        void OnEntityDeath(BasePlayer player, HitInfo info)
        {   
            if (player == null) return;
            if (!config.ct.craftQue) return;

            var run = player.GetComponent<CraftingQue>(); 
            if (run == null) 
                return;
            else
                run.CancelAll(player);
        }
    
        private void OnLootEntity(BasePlayer player, Workbench bench)
        {     
            
            if (!permission.UserHasPermission(player.UserIDString, "craftmenu.use")) return;

            if (!config.ct.craftQue)
                player.gameObject.GetOrAddComponent<CraftingQue>();

            if (!playerBps.ContainsKey(player.userID))
            {
                playerBps.Add(player.userID, new PlayerBps());  
                SavePlayerData();
            }
            if (bench.PrefabName.Contains("1"))  {
                CreateBaseCui(player, 1);   
                ShowBps(player, 1, "all");
            }   
            if (bench.PrefabName.Contains("2"))   {
                CreateBaseCui(player, 2);
                ShowBps(player, 2, "all"); 
            }
            if (bench.PrefabName.Contains("3"))   {  
                CreateBaseCui(player, 3);
                ShowBps(player, 3, "all");
            }
        }

        private void OnLootEntityEnd(BasePlayer player, Workbench bench)
        {      
            DestroyCui(player); 
        }

        #endregion

        #region [Functions/Methods]

        private void RegisterPerms()
        {   
            if (config.main.perms)
            {
                foreach (string item in config.main.cat.Keys)
                    permission.RegisterPermission($"craftmenu.{item}", this);   
            }
        }

        [ConsoleCommand("craftmenu_admin")]
        private void craftmenu_admin(ConsoleSystem.Arg arg)
        {
            var player = arg?.Player();
            var args = arg.Args;
            if (arg.Player() != null) 
            {
                if (!player.IsAdmin)
                    return;   
            }
            
            if (args[0] == "wipe")
            {
                if (args.Length > 1) 
                {   
                    if (!playerBps.ContainsKey(Convert.ToUInt64(args[1])))
                    {
                        Puts($"Player {args[1]} have no blueprints to wipe.");
                        return;
                    }

                    playerBps.Remove(Convert.ToUInt64(args[1]));
                    Puts($"BPs for player {args[1]} wiped.");
                    return;
                }

                Puts($"Blueprints has been wiped.");
                playerBps.Clear();
                SavePlayerData();
                return;
            } 
        }

        [ConsoleCommand("craftmenu_cmd")]
        private void craftmenu_cmd(ConsoleSystem.Arg arg)
        {
            var player = arg?.Player();
            var args = arg.Args;
            if (arg.Player() == null) return;
            if (args == null) return;

            if (args[0] == "select")
            {                      //tier selection     //shortname   //position index        //selection highlight
                CreateBp(player, Convert.ToInt32(args[1]), args[2], Convert.ToInt32(args[3]), true);
                PlayFx(player, click);
                return;
            }
            if (args[0] == "category")
            {
                ShowBps(player, Convert.ToInt32(args[2]), args[1]);
                PlayFx(player, pageChange);
                return;
            }
            if (args[0] == "pageup")
            {   
                if (Convert.ToInt32(args[3]) < 0) return;
                               //tier                //category   //page
                ShowBps(player, Convert.ToInt32(args[2]), args[1], Convert.ToInt32(args[3]));
                PlayFx(player, pageChange);
                return;  
            }
            if (args[0] == "pagedown")
            {                   //tier                //category   //page
                ShowBps(player, Convert.ToInt32(args[2]), args[1], Convert.ToInt32(args[3]));
                PlayFx(player, pageChange);
                return;
            }
            if (args[0] == "craft")
            {                       //tier                  //shortname         //index      
                CraftItem(player, Convert.ToInt32(args[1]), args[2], Convert.ToInt32(args[3]));
                return;
            }

            if (args[0] == "research")
            {                                   //tier          //shortname         //index      
                ResearchItem(player, Convert.ToInt32(args[1]), args[2], Convert.ToInt32(args[3]));
                return;                
            }
        }
        
        private List<string> CreateBpOrder(BasePlayer player, int tier, string category)
        {   
            List<string> bpOrder = new List<string>();
            List<string> lockedBps = new List<string>();
            //add available blueprints
            foreach (string item in bps.Keys)
            {   
                if (category == "all")
                {
                    if (playerBps[player.userID].bp.Contains(item))
                    {   
                        if (bps[item].Tier <= tier)
                        {
                            //bpOrder.Add(item);
                            bpOrder.Insert(0, item);
                        }
                        else 
                        {
                            bpOrder.Add(item);
                        }
                        continue;
                    }
                    else
                    {
                        lockedBps.Add(item);
                        continue;
                    }
                }
                if (bps[item].Category == category)
                {
                    if (playerBps[player.userID].bp.Contains(item))
                    {   
                        if (bps[item].Tier <= tier)
                        {
                            //bpOrder.Add(item);
                            bpOrder.Insert(0, item);
                        }
                        else 
                        {
                            bpOrder.Add(item);
                        }
                    }
                    else
                    {
                        lockedBps.Add(item);
                        continue;
                    }
                }
            }
            //add locked blueprints at last
            foreach (string item in lockedBps)
            {
                bpOrder.Add(item);
            }
            //insert index string
            bpOrder.Insert(0, "null");

            return bpOrder;
        }

        private bool CheckInv(BasePlayer player, string shortName, int amount)
        {
            var itemDef = ItemManager.FindItemDefinition(shortName);
            if (itemDef == null) return false;
           
            int invAmount = player.inventory.GetAmount(itemDef.itemid);
            if (invAmount < amount) return false;
            
            return true;
        }

        private bool _CanCraft(BasePlayer player, string shortName)
        {
            foreach (string item in bps[shortName].Resources.Keys)
            {   
                var itemDef = ItemManager.FindItemDefinition(item);
                int invAmount = player.inventory.GetAmount(itemDef.itemid);
                if (invAmount < bps[shortName].Resources[item]) 
                    return false;
            }
            return true;
        }
    
        private void CraftItem(BasePlayer player, int tier, string shortName, int index)
        {   
            string _shortname = shortName;
            if (shortName.Contains("{"))
            {
                int charsToRemove = shortName.Length - shortName.IndexOf("{");
                _shortname = shortName.Remove(shortName.Length - charsToRemove);
            }

            if (!_CanCraft(player, shortName))
            {//check resources
                //refresh craft button
                CreateBp(player, tier, shortName, index, true);
                PlayFx(player, click);
                return;
            }
            else
            {
                foreach (string _item in bps[shortName].Resources.Keys)
                {//take resources 
                    var itemDef = ItemManager.FindItemDefinition(_item);
                    if (itemDef == null)
                    {
                        SendReply(player, $" <color=#C2291D>!</color> '{_item}' <color=#C2291D>is not correct shortname.</color>");
                        return;
                    }
                    player.inventory.Take(null, itemDef.itemid, bps[shortName].Resources[_item]);
                }

                var item = ItemManager.CreateByName(_shortname, 1, bps[shortName].SkinID);
                if (item != null)
                {//give crafted item

                    if (config.ct.craftQue) 
                    {
                        var run = player.GetComponent<CraftingQue>(); 
                        if (run != null)
                            run.AddToQue(player, shortName); 
                        else
                            RefundItem(player, shortName);

                        PlayFx(player, craft);
                    }
                    else
                    {   
                        item.name = bps[shortName].Name;
                        player.GiveItem(item);
                        CreateBp(player, tier, shortName, index, true);
                        PlayFx(player, craft);
                        return;
                    }
                }
                else
                {
                    SendReply(player, $" <color=#C2291D>!</color> '{item}' <color=#C2291D>is not correct shortname.</color>");
                    return;
                }
            }
        }

        private void ResearchItem(BasePlayer player, int tier, string shortName, int index)
        {  
            if (!CheckInv(player, "scrap", bps[shortName].ResearchCost))
            {//check resources
                //refresh craft button
                CreateBp(player, tier, shortName, index, true);
                PlayFx(player, click);
                return;
            }
            else
            { 
                var itemDef = ItemManager.FindItemDefinition("scrap");
                if (itemDef == null)
                {
                    SendReply(player, $" <color=#C2291D>!</color> Error, please contact developer.</color>");
                    return;
                }
                player.inventory.Take(null, itemDef.itemid, bps[shortName].ResearchCost);
                playerBps[player.userID].bp.Add(shortName);
                SavePlayerData();
                CreateBp(player, tier, shortName, index, true);
                PlayFx(player, research);
            }
            
        }

        private void RefundItem(BasePlayer player, string shortName)
        {
            foreach (string item in bps[shortName].Resources.Keys)
            {   
                var _item = ItemManager.CreateByName($"{item}", bps[shortName].Resources[item]);
                player.GiveItem(_item);
            }
        }

        private void CreateItemMono(BasePlayer player, string shortName)
        {   
            string _shortname = shortName;
            if (shortName.Contains("{"))
            {
                int charsToRemove = shortName.Length - shortName.IndexOf("{");
                _shortname = shortName.Remove(shortName.Length - charsToRemove);
            }

            var item = ItemManager.CreateByName(_shortname, 1, bps[shortName].SkinID);
            if (item != null)
            {
                item.name = bps[shortName].Name;
                player.GiveItem(item);
            }
            
        }

        [ConsoleCommand("craftmenu_cancel")]
        private void craftmenu_cancel(ConsoleSystem.Arg arg)
        {
            var player = arg?.Player();
            if (arg.Player() == null) return;
            var args = arg.Args;
            if (args[0] == null) return;
            int index = Convert.ToInt32(args[0]);

            var run = player.GetComponent<CraftingQue>(); 
            if (run == null) player.gameObject.GetOrAddComponent<CraftingQue>();
            if (run != null) { 
                run.CancelCraft(player, index);
            }
        }

        [ConsoleCommand("craftmenu_addtoque")]
        private void craftmenu_addtoque(ConsoleSystem.Arg arg)
        {   
            
            var player = arg?.Player();
            if (arg.Player() == null) return;
            if (!player.IsAdmin) return;
            var args = arg.Args;
            if (args[0] == null) return;

            var run = player.GetComponent<CraftingQue>(); 
            if (run == null) player.gameObject.GetOrAddComponent<CraftingQue>();
            if (run != null) run.AddToQue(player, args[0]);
        
        }


        [ConsoleCommand("craftmenu_openquepanel")]
        private void craftmenu_openquepanel(ConsoleSystem.Arg arg)
        {   
            var player = arg?.Player();
            if (arg.Player() == null) return;
            
            var run = player.GetComponent<CraftingQue>(); 
            if (run == null) player.gameObject.GetOrAddComponent<CraftingQue>();
            if (run != null) run.OpenQuePanel(player);
        }

        private void PlayFx(BasePlayer player, string fx)
        {   
            if (!config.main.fx) return;     
            if (player == null) return;
            var EffectInstance = new Effect();
            EffectInstance.Init(Effect.Type.Generic, player, 0, Vector3.up, Vector3.zero);
            EffectInstance.pooledstringid = StringPool.Get(fx);
            Network.Net.sv.write.Start();
            Network.Net.sv.write.PacketID(Message.Type.Effect);
            EffectInstance.WriteToStream(Network.Net.sv.write);
            Network.Net.sv.write.Send(new SendInfo(player.net.connection));
            EffectInstance.Clear();
        }

        #endregion

        #region [MonoBehaviour]

        static CraftMenu plugin;

        private void Init() => plugin = this; 

        private void AddMonoComponent()
        {
            foreach (var _player in BasePlayer.activePlayerList) 
                _player.gameObject.GetOrAddComponent<CraftingQue>();      
        }

        private void DestroyMonoComponent()
        {
            foreach (var _player in BasePlayer.activePlayerList)
            {   
                var run = _player.GetComponent<CraftingQue>(); 
                if (run == null) 
                    return;
                else
                    run.CancelAll(_player);
         
               
               UnityEngine.Object.Destroy(run);
            }   
        }

        private class CraftingQue : MonoBehaviour
		{
			BasePlayer player;
            List<string> craftOrder = new List<string>();
            bool craftPanelOpen = false;
            int progress;

			void Awake() => player = GetComponent<BasePlayer>();

            public void CancelAll(BasePlayer player)
            {   
                if (craftOrder.Count != 0) {
                    foreach (string item in craftOrder)
                    {   
                        plugin.RefundItem(player, item);
                    }
                    CuiHelper.DestroyUi(player, "ql_base"); 
                    CancelInvoke(nameof(CraftProgress));
                }
                craftOrder.Clear();
            }

            public void OpenQuePanel(BasePlayer player)
            {   
                if (!craftPanelOpen)
                {
                    plugin.CreateQuePanel(player, craftOrder);
                    craftPanelOpen = true;
                }
                else
                {
                    CuiHelper.DestroyUi(player, "qPanel_panel");
                    craftPanelOpen = false;
                }
            }

            public void CancelCraft(BasePlayer player, int index)
            {   
                plugin.RefundItem(player, craftOrder[index]);
                craftOrder.RemoveAt(index);
                plugin.CreateQueButton(player, craftOrder.Count);
                if (craftOrder.Count <= 0)
                {   
                    CuiHelper.DestroyUi(player, "ql_base"); 
                    CancelInvoke(nameof(CraftProgress));
                }
                else
                {   
                    if (craftPanelOpen)
                        plugin.CreateQuePanel(player, craftOrder);

                    if (craftOrder.Count <= 1)
                    {   
                        CuiHelper.DestroyUi(player, "qPanel_panel");
                    }
                    

                    if (index == 0)
                    {   
                        CancelInvoke(nameof(CraftProgress));
            
                        if (plugin.config.ct.excp.ContainsKey(craftOrder[0]))
                            progress = plugin.config.ct.excp[craftOrder[0]];
                        else
                            progress = plugin.config.ct.defaultTime;

                        InvokeRepeating(nameof(CraftProgress), 1f, 1f);
                    }
                }
                
            }
            
            void CraftProgress()
            {   

                progress -= 1;
                plugin.CreateTimer(player, craftOrder[0], progress);

                if (progress <= 0)
                {   
                    plugin.CreateItemMono(player, craftOrder[0]);
                    craftOrder.RemoveAt(0);
                    int itemsLeft = craftOrder.Count();

                    if (itemsLeft <= 1) {
                        CuiHelper.DestroyUi(player, "ql_base_quetext_btn");
                        CuiHelper.DestroyUi(player, "qPanel_panel");
                    }
                    else {
                        plugin.CreateQueButton(player, itemsLeft);
                        if (craftPanelOpen)
                            plugin.CreateQuePanel(player, craftOrder);
                    }

                    if(itemsLeft <= 0) {
                        CancelInvoke(nameof(CraftProgress));
                        CuiHelper.DestroyUi(player, "ql_base"); 
                    }
                    else 
                    {   
                        CuiHelper.DestroyUi(player, "qTimer");
                        CuiHelper.DestroyUi(player, "qTimerName");
                        if (plugin.config.ct.excp.ContainsKey(craftOrder[0]))
                            progress = plugin.config.ct.excp[craftOrder[0]];
                        else
                            progress = plugin.config.ct.defaultTime;
                    }
                        
                }  
            }
            
            public void AddToQue(BasePlayer player, string itemName)
			{   
                if (player == null) return;

                
                if (craftOrder == null)
                {
                    //ignore
                }
                craftOrder.Add(itemName);
               
                if (IsInvoking(nameof(CraftProgress)) == false) 
                {   
                    if (plugin.config.ct.excp.ContainsKey(craftOrder[0]))
                            progress = plugin.config.ct.excp[craftOrder[0]];
                        else
                            progress = plugin.config.ct.defaultTime;
                    InvokeRepeating(nameof(CraftProgress), 0.1f, 1f);
                    

                        plugin.CreateCraftQueLayout(player);
                }
                if (craftOrder.Count > 1)
                {
                    plugin.CreateQueButton(player, craftOrder.Count);

                    if (craftPanelOpen)
                        plugin.CreateQuePanel(player, craftOrder);
                }

			}

        }
        
        #endregion

        #region [UI]

        private void CreateBaseCui(BasePlayer player, int tier = 1)
        {   
            var _baseCraftCui = CUIClass.CreateOverlay("empty", "0 0 0 0", "0 0", "0 0", false, 0.0f, "assets/icons/iconmaterial.mat"); //assets/content/ui/uibackgroundblur.mat
            //offset
            CUIClass.CreatePanel(ref _baseCraftCui, "baseCraft_main", "Overlay", "0 0 0 0", "0.5 0.5", "0.5 0.5", false, 0.1f, 0f, "assets/icons/iconmaterial.mat", "193 -104", "573 266");
                //title
                CUIClass.CreateText(ref _baseCraftCui, "baseCraft_title_text", "baseCraft_main", "1 1 1 0.6", $"<size=21><b>BLUEPRINTS</b></size>", 12, "0.00 1", "1 1.2", TextAnchor.LowerLeft, $"robotocondensed-regular.ttf", 0.1f);  
                CUIClass.CreateText(ref _baseCraftCui, "baseCraft_title_count", "baseCraft_main", "1 1 1 0.6", $"UNLOCKED {playerBps[player.userID].bp.Count() + _bpsDefault}/{_bpsTotal}", 12, "0.00 1.01", "0.99 1.2", TextAnchor.LowerRight, $"robotocondensed-regular.ttf", 0.1f);  
                //categories
                CUIClass.CreatePanel(ref _baseCraftCui, "baseCraft_category_panel", "baseCraft_main", "0.70 0.67 0.65 0.17", "0.0 0.93", "1 1", true, 0.1f, 0f, "assets/content/ui/uibackgroundblur.mat"); 
                    //all default
                    CUIClass.CreateButton(ref _baseCraftCui, "baseCraft_category_btnAll", "baseCraft_category_panel", "0.70 0.67 0.65 0.0", "", 11, ca[0], ca[1], $"craftmenu_cmd category all {tier}", "", "1 1 1 0.7", 0.1f, TextAnchor.MiddleCenter, $"robotocondensed-bold.ttf", "assets/icons/iconmaterial.mat");
                        CUIClass.PullFromAssets(ref _baseCraftCui, "bc_ci_all", "baseCraft_category_btnAll", "1 1 1 0.5", "assets/icons/community_servers.png", 0.1f, 0f, "0.1 0.15", "0.96 0.83");
                    //optional
                    int index = 1;
                    foreach (string category in config.main.cat.Keys)
                    {   
                        int a1 = 0 + index;
                        int a2 = 1 + index;
                        if (index == 1) { a1 = 2; a2 = 3; }
                        if (index == 2) { a1 = 4; a2 = 5; }
                        if (index == 3) { a1 = 6; a2 = 7; }
                        if (index == 4) { a1 = 8; a2 = 9; }
                        if (index == 5) { a1 = 10; a2 = 11; }
                        if (index == 6) { a1 = 12; a2 = 13; }

                        CUIClass.CreateButton(ref _baseCraftCui, $"baseCraft_category_btn{index}", "baseCraft_category_panel", "0.70 0.67 0.65 0.0", "", 11, ca[a1], ca[a2], $"craftmenu_cmd category {category} {tier}", "", "1 1 1 0.7", 0.1f, TextAnchor.MiddleCenter, $"robotocondensed-bold.ttf", "assets/icons/iconmaterial.mat");
                        
                        if (config.main.cat[category].StartsWith("assets"))
                        {   
                            int b1 = 0; int b2 = 1;
                            if (config.main.cat[category] == "assets/icons/construction.png") {b1 = 2; b2 = 3;}
                            if (config.main.cat[category] == "assets/icons/clothing.png") {b1 = 4; b2 = 5;}
                            if (config.main.cat[category] == "assets/icons/bullet.png") {b1 = 6; b2 = 7;}
                            if (config.main.cat[category] == "assets/icons/medical.png") {b1 = 8; b2 = 9;}
                            
                            CUIClass.PullFromAssets(ref _baseCraftCui, $"category_btn_asset{index}", $"baseCraft_category_btn{index}", "1 1 1 0.4", config.main.cat[category], 0.1f, 0f, ia[b1], ia[b2]);
                        }
                        else
                            CUIClass.CreateImage(ref _baseCraftCui, $"category_btn_asset{index}", $"baseCraft_category_btn{index}", Img($"{config.main.cat[category]}"), "0 0", "1 1", 0.1f);
                
                        index++;
                    }
                //blueprint container
                CUIClass.CreatePanel(ref _baseCraftCui, "baseCraft_blueprints_panel", "baseCraft_main", "0.70 0.67 0.65 0.0", "0.0 0.0", "1 0.92", false, 0.1f, 0f, "assets/icons/iconmaterial.mat"); 
           
            DestroyCui(player); 
            CuiHelper.AddUi(player, _baseCraftCui);
            CuiHelper.DestroyUi(player, "empty");  
        }
        
        private void DestroyCui(BasePlayer player)
        {   
            CuiHelper.DestroyUi(player, "empty"); 
            CuiHelper.DestroyUi(player, "baseCraft_main");
        }

        private void CreatePageBtns(BasePlayer player, string category, int tier, int currentPage)
        {   
            var _pageBtns = CUIClass.CreateOverlay("empty", "0 0 0 0", "0 0", "0 0", false, 0.0f, "assets/icons/iconmaterial.mat"); //assets/content/ui/uibackgroundblur.mat
            int pageup = currentPage - 1;
            int pagedown = 1 + currentPage;

            CUIClass.CreateButton(ref _pageBtns, "craft_page_up", "baseCraft_category_panel", "0.80 0.25 0.16 0.0", "▲", 11, "0.86 0.16", $"0.89 0.84", $"craftmenu_cmd pageup {category} {tier} {pageup}", "", "1 1 1 0.4", 0.1f, TextAnchor.MiddleCenter, $"robotocondensed-bold.ttf", "assets/icons/iconmaterial.mat");
            CUIClass.CreateButton(ref _pageBtns, "craft_page_down", "baseCraft_category_panel", "0.80 0.25 0.16 0.0", "▼", 11, "0.89 0.16", $"0.94 0.84", $"craftmenu_cmd pageup {category} {tier} {pagedown}", "", "1 1 1 0.4", 0.1f, TextAnchor.MiddleCenter, $"robotocondensed-bold.ttf", "assets/icons/iconmaterial.mat");
            CUIClass.CreateText(ref _pageBtns, "bp_page_count", "baseCraft_category_panel", "1 1 1 0.4", $"{currentPage + 1}", 11, "0.94 0.0", "0.98 1", TextAnchor.MiddleCenter, $"robotocondensed-regular.ttf", 0.1f);  
            
            CuiHelper.DestroyUi(player, "craft_page_up"); 
            CuiHelper.DestroyUi(player, "craft_page_down");  
            CuiHelper.DestroyUi(player, "bp_page_count");
            CuiHelper.AddUi(player, _pageBtns); 
            CuiHelper.DestroyUi(player, "empty"); 
        }
    
        private void ShowBps(BasePlayer player, int tier, string category, int page = 0)
        {
            List<string> bpOrder = CreateBpOrder(player, tier, category);
            int index = 6 * page;
            
            for (int i = 1; i < 7; i++)
                CuiHelper.DestroyUi(player, $"baseCraft_bp_{i}");
            
            int totalItems = bpOrder.Count() - 1;

            for (int i = 1; i < 7; i++)
            {   
                if (totalItems < index + i) 
                    break;

                CreateBp(player, tier, bpOrder[index + i], i);
            }

            if (bpOrder.Count() > 7)
                CreatePageBtns(player, category, tier, page);
            else
            {
                CuiHelper.DestroyUi(player, "craft_page_up"); 
                CuiHelper.DestroyUi(player, "craft_page_down"); 
                CuiHelper.DestroyUi(player, "bp_page_count");
            }
        }
        
        private void CreateBp(BasePlayer player, int tier, string shortName, int index, bool selected = false)
        {  
            string bpUi = $"baseCraft_bp_{index}"; 
            string anchorMin = ba[1];
            string anchorMax = ba[2];
            if (index == 2) { anchorMin = ba[3]; anchorMax = ba[4]; }
            if (index == 3) { anchorMin = ba[5]; anchorMax = ba[6]; }
            if (index == 4) { anchorMin = ba[7]; anchorMax = ba[8]; }
            if (index == 5) { anchorMin = ba[9]; anchorMax = ba[10]; }
            if (index == 6) { anchorMin = ba[11]; anchorMax = ba[12]; }
            //rustlabs image
            string img = $"{bps[shortName].Image}";
            if (!img.StartsWith("http"))
                img = "https://rustlabs.com/img/items180/" + img;
            //resources
            string resource = "";
            foreach (string item in bps[shortName].Resources.Keys)
            {    
                var itemDef = ItemManager.FindItemDefinition(item);
                if (itemDef == null) 
                {
                    SendReply(player, $" <color=#C2291D>!</color> '{item}' <color=#C2291D>is not correct shortname.</color>");
                    return;
                }
                string itemDisplayName = itemDef.displayName.translated;
           
                if (nameReplace.ContainsKey(item))
                    itemDisplayName = nameReplace[item];

                if (CheckInv(player, item, bps[shortName].Resources[item]))
                    resource = resource + $"{bps[shortName].Resources[item]} {itemDisplayName}, ";
                else
                    resource = resource + $"<color=#d0b255>{bps[shortName].Resources[item]} {itemDisplayName}</color>, ";   
            }
            resource = resource.Remove(resource.Length-2);
            //UI
            var _createBps = CUIClass.CreateOverlay("empty", "0 0 0 0", "0 0", "0 0", false, 0.0f, "assets/icons/iconmaterial.mat"); //assets/content/ui/uibackgroundblur.mat
            if (selected)
            {
                if (bps[shortName].Tier > tier)
                {//if tier required
                    if (config.main.perms && !permission.UserHasPermission(player.UserIDString, $"craftmenu.{bps[shortName].Category}"))
                    {
                        CUIClass.CreateText(ref _createBps, "selected_btn", bpUi, "1 1 1 0.25", $"<size=10>YOU CAN'T CRAFT THIS ITEM</size>", 12, "0.8 0.19", $"0.98 0.83", TextAnchor.MiddleCenter, $"robotocondensed-regular.ttf", 0.1f);  
                    }
                    else
                    {
                        CUIClass.CreateText(ref _createBps, "selected_btn", bpUi, "1 1 1 0.25", $"<b><size=15>TIER {bps[shortName].Tier}</size></b>\nREQUIRED", 12, "0.8 0.19", $"0.98 0.83", TextAnchor.MiddleCenter, $"robotocondensed-regular.ttf", 0.1f);      
                    }
                }    
                else
                {   


                    if (bps[shortName].ResearchCost != 0)
                    {//if needs to be reseached
                        if (playerBps[player.userID].bp.Contains(shortName))
                        {// if player own blueprint
                            if (_CanCraft(player, shortName))
                            {// if player has enough resources
                                //craft button
                                CUIClass.CreateButton(ref _createBps, "selected_btn", bpUi, "0.38 0.51 0.16 0.85", "     CRAFT", 11, "0.8 0.19", $"0.98 0.83", $"craftmenu_cmd craft {tier} {shortName} {index}", "", "1 1 1 0.6", 0.1f, TextAnchor.MiddleCenter, $"robotocondensed-bold.ttf", "assets/content/ui/uibackgroundblur.mat");
                                CUIClass.PullFromAssets(ref _createBps, "bp_craftBtn_icon", "selected_btn", "1 1 1 0.65", "assets/icons/tools.png", 0.1f, 0f, "0.14 0.34", "0.32 0.67");   
                            }
                            else
                            {// not enough resources
                                CUIClass.CreateButton(ref _createBps, "selected_btn", bpUi, "0.70 0.67 0.65 0.17", "NOT ENOUGH\nRESOURCES", 11, "0.8 0.19", $"0.98 0.83", $"", "", "1 1 1 0.4", 0.1f, TextAnchor.MiddleCenter, $"robotocondensed-bold.ttf", "assets/content/ui/uibackgroundblur.mat");
                            }
                        }
                        else
                        {//research button
                            if (config.main.perms && !permission.UserHasPermission(player.UserIDString, $"craftmenu.{bps[shortName].Category}"))
                            {
                                CUIClass.CreateText(ref _createBps, "selected_btn", bpUi, "1 1 1 0.25", $"<size=10>YOU CAN'T CRAFT THIS ITEM</size>", 12, "0.8 0.19", $"0.98 0.83", TextAnchor.MiddleCenter, $"robotocondensed-regular.ttf", 0.1f);  
                            }
                            else
                            {
                                CUIClass.CreateButton(ref _createBps, "selected_btn", bpUi, "0.70 0.67 0.65 0.17", "RESEARCH\n", 11, "0.8 0.19", $"0.98 0.83", $"craftmenu_cmd research {tier} {shortName} {index}", "", "1 1 1 0.6", 0.1f, TextAnchor.MiddleCenter, $"robotocondensed-regular.ttf", "assets/content/ui/uibackgroundblur.mat");
                                CUIClass.CreateImage(ref _createBps, "btn_research_scrapImg", "selected_btn", Img("https://rustlabs.com/img/items180/scrap.png"), "0.16 0.12", "0.43 0.48", 0.1f);
                                CUIClass.CreateText(ref _createBps, "btn_research_cost", "selected_btn", "1 1 1 0.4", $"{bps[shortName].ResearchCost}", 12, "0.3 0.0", "0.80 0.5", TextAnchor.UpperRight, $"robotocondensed-bold.ttf", 0.1f);      
                            }
                        }
                    }
                    else
                    {
                        if (_CanCraft(player, shortName))
                        {// if player has enough resources
                            //craft button
                            CUIClass.CreateButton(ref _createBps, "selected_btn", bpUi, "0.38 0.51 0.16 0.85", "     CRAFT", 11, "0.8 0.19", $"0.98 0.83", $"craftmenu_cmd craft {tier} {shortName} {index}", "", "1 1 1 0.6", 0.1f, TextAnchor.MiddleCenter, $"robotocondensed-bold.ttf", "assets/content/ui/uibackgroundblur.mat");
                            CUIClass.PullFromAssets(ref _createBps, "bp_craftBtn_icon", "selected_btn", "1 1 1 0.65", "assets/icons/tools.png", 0.1f, 0f, "0.14 0.34", "0.32 0.67");   
                        }
                        else
                        {// not enough resources
                            CUIClass.CreateButton(ref _createBps, "selected_btn", bpUi, "0.70 0.67 0.65 0.17", "NOT ENOUGH\nRESOURCES", 11, "0.8 0.19", $"0.98 0.83", $"", "", "1 1 1 0.4", 0.1f, TextAnchor.MiddleCenter, $"robotocondensed-bold.ttf", "assets/content/ui/uibackgroundblur.mat");
                        }
                    }
                }         
            }  
            else
            {//BASE
                CUIClass.CreatePanel(ref _createBps, bpUi, "baseCraft_blueprints_panel", "0.70 0.67 0.65 0.07", anchorMin, anchorMax, false, 0.1f, 0f, "assets/content/ui/uibackgroundblur.mat"); 
                //data
                CUIClass.CreateImage(ref _createBps, "bp_image", bpUi, Img($"{img}"), "0.02 0.12", "0.14 0.88", 0.1f);
                CUIClass.CreateText(ref _createBps, "bp_name", bpUi, "1 1 1 0.4", $"{bps[shortName].Name}", 17, "0.17 0.45", "0.80 0.88", TextAnchor.MiddleLeft, $"robotocondensed-bold.ttf", 0.1f);  
                CUIClass.CreateText(ref _createBps, "bp_resource", bpUi, "1 1 1 0.4", resource, 11, "0.17 0.14", "0.80 0.46", TextAnchor.UpperLeft, $"robotocondensed-regular.ttf", 0.1f);  
            
                if (bps[shortName].ResearchCost != 0)
                {//if needs to be reseached
                    if (playerBps[player.userID].bp.Contains(shortName))
                    {//bp available
                        CUIClass.PullFromAssets(ref _createBps, "bp_available", bpUi, "1 1 1 0.18", "assets/icons/check.png", 0.1f, 0f, "0.85 0.22", "0.945 0.87");      
                    }
                    else
                    {//locked
                         CUIClass.PullFromAssets(ref _createBps, "bp_lock", bpUi, "1 1 1 0.18", "assets/icons/bp-lock.png", 0.1f, 0f, "0.85 0.22", "0.945 0.87");
                    }
                }
                else
                {//bp available
                    CUIClass.PullFromAssets(ref _createBps, "bp_available", bpUi, "1 1 1 0.18", "assets/icons/check.png", 0.1f, 0f, "0.85 0.22", "0.945 0.87");
                }
                
                //select btn
                CUIClass.CreateButton(ref _createBps, "select_btn", bpUi, "0 0 0 0", "", 11, "0 0", $"1 1", $"craftmenu_cmd select {tier} {shortName} {index}", "", "1 1 1 0.4", 1f, TextAnchor.MiddleCenter, $"robotocondensed-bold.ttf", "assets/icons/iconmaterial.mat");
            }      
            
            if (!selected)
                CuiHelper.DestroyUi(player, bpUi);
            else
                CuiHelper.DestroyUi(player, "selected_btn");

            CuiHelper.AddUi(player, _createBps);
            CuiHelper.DestroyUi(player, "empty");
        }

        private void DestroyBps(BasePlayer player)
        {   
            CuiHelper.DestroyUi(player, "empty"); 
            CuiHelper.DestroyUi(player, "baseCraft_main");
        }

        string[] anchors = {
            "index",
            "0.86 0.0-0.98 1",
            "0.72 0.0-0.84 1",
            "0.58 0.0-0.70 1",
            "0.44 0.0-0.56 1",
            "0.30 0.0-0.42 1",
            "0.16 0.0-0.28 1",
            "0.02 0.0-0.14 1",
        
        };

        private void CreateCraftQueLayout(BasePlayer player, string itemName = "default")
        {   
          
            var qL = CUIClass.CreateOverlay("empty", "0 0 0 0", "0 0", "0 0", false, 0.0f, "assets/icons/iconmaterial.mat"); //assets/content/ui/uibackgroundblur.mat
         
            CUIClass.CreatePanel(ref qL, "ql_base", "Overlay", "0.10 0.40 0.60 1", "0.5 0.5", "0.5 0.5", false, 0.3f, 0f, "assets/content/ui/uibackgroundblur.mat", "245 -344", "423 -317");
            CUIClass.PullFromAssets(ref qL, "ql_base_gearicon", "ql_base", "0.20 0.6 0.8 1", "assets/icons/gear.png", 0.3f, 0f, "0.02 0.25", "0.11 0.75");
         
            CUIClass.CreateButton(ref qL, "ql_base_btn_cancel_current", "ql_base", "0.70 0.67 0.65 0.17", "", 11, "-0.15 0.02", $"-0.01 0.98", $"craftmenu_cancel 0", "", "1 1 1 0.7", 1f, TextAnchor.MiddleCenter, $"robotocondensed-bold.ttf", "assets/content/ui/uibackgroundblur.mat");
                CUIClass.PullFromAssets(ref qL, "ql_base_crossicon1", "ql_base_btn_cancel_current", "1 1 1 0.45", "assets/icons/vote_down.png", 0.1f, 0f, "0.0 0", "1 1");
            
            CuiHelper.DestroyUi(player, "empty");
            CuiHelper.DestroyUi(player, "ql_base");
            CuiHelper.AddUi(player, qL); 
            CuiHelper.DestroyUi(player, "empty");
        }
                    
        private void CreateTimer(BasePlayer player, string itemName, int seconds)
        {   
            string displayName = bps[itemName].Name;
            if (displayName.Length > 16)
            {
                displayName = displayName.Remove(16);
                displayName += "."; 
            }
            string s = "king";
            string result = s.Remove(s.Length-1);
            var qTimer = CUIClass.CreateOverlay("empty", "0 0 0 0", "0 0", "0 0", false, 0.0f, "assets/icons/iconmaterial.mat"); //assets/content/ui/uibackgroundblur.mat
            CUIClass.CreateText(ref qTimer, "qTimer", "ql_base", "1 1 1 0.6", $"{seconds}s", 13, "0.13 0.00", "0.96 1", TextAnchor.MiddleRight, $"robotocondensed-regular.ttf", 0.0f);  
            CUIClass.CreateText(ref qTimer, "qTimerName", "ql_base", "1 1 1 0.8", $"{displayName.ToUpper()}", 13, "0.13 0.00", "1 1", TextAnchor.MiddleLeft, $"robotocondensed-bold.ttf", 0.0f);  
            
            CuiHelper.DestroyUi(player, "qTimer");
            CuiHelper.DestroyUi(player, "qTimerName");
            CuiHelper.AddUi(player, qTimer); 
            CuiHelper.DestroyUi(player, "empty");
        }

        private void CreateQueButton(BasePlayer player, int count)
        {
            var qBtn = CUIClass.CreateOverlay("empty", "0 0 0 0", "0 0", "0 0", false, 0.0f, "assets/icons/iconmaterial.mat"); //assets/content/ui/uibackgroundblur.mat
            if (count > 1)
                CUIClass.CreateButton(ref qBtn, "ql_base_quetext_btn", "ql_base", "0 0 0 0", $"  {count - 1} more items in queue, click to cancel", 9, "0 -0.4", "1.3 3.0", $"craftmenu_openquepanel", "", "1 1 1 0.9", 0.2f, TextAnchor.LowerLeft, $"robotocondensed-regular.ttf", "assets/icons/iconmaterial.mat");
            
            CuiHelper.DestroyUi(player, "ql_base_quetext_btn");
            CuiHelper.AddUi(player, qBtn); 
            CuiHelper.DestroyUi(player, "empty");
        }

        private void CreateQuePanel(BasePlayer player, List<string> craftingQue)
        {   
            var qPanel = CUIClass.CreateOverlay("empty", "0 0 0 0", "0 0", "0 0", false, 0.0f, "assets/icons/iconmaterial.mat"); //assets/content/ui/uibackgroundblur.mat
            if (craftingQue.Count > 1)
            {
                int forLenght = craftingQue.Count;
                if (forLenght > 8)
                    forLenght = 8;

                CUIClass.CreatePanel(ref qPanel, "qPanel_panel", "ql_base", "0.70 0.67 0.65 0.17", "-0.155 1.1", "1 2.2", false, 0.1f, 0f, "assets/content/ui/uibackgroundblur.mat");
                for (var i = 1; i < forLenght; i++)
                {   
                    string img = $"{bps[craftingQue[i]].Image}";
                    if (!img.StartsWith("http"))
                    img = "https://rustlabs.com/img/items180/" + img;


                    string[] splitA = anchors[i].Split('-');
                    CUIClass.CreateButton(ref qPanel, $"ql_quebtn{i}", "qPanel_panel", "0 0 0 0", "", 11, splitA[0], splitA[1], $"craftmenu_cancel {i}", "", "1 1 1 0.7", 0.2f, TextAnchor.MiddleCenter, $"robotocondensed-bold.ttf", "assets/icons/iconmaterial.mat");
                    CUIClass.CreateImage(ref qPanel, $"ql_base_que_img{i}", $"ql_quebtn{i}", $"{Img(img)}", "0 0.1", "1 0.9", 0.2f);
                
                }
            }
            
            CuiHelper.DestroyUi(player, "qPanel_panel");
            CuiHelper.AddUi(player, qPanel); 
            CuiHelper.DestroyUi(player, "empty");
        }
        


        #endregion
        
        #region [CUI Class]

        public class CUIClass
        {
            public static CuiElementContainer CreateOverlay(string _name, string _color, string _anchorMin, string _anchorMax, bool _cursorOn = false, float _fade = 0f, string _mat ="")
            {   
                var _element = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = { Color = _color, Material = _mat, FadeIn = _fade},
                            RectTransform = { AnchorMin = _anchorMin, AnchorMax = _anchorMax },
                            CursorEnabled = _cursorOn
                        },
                        new CuiElement().Parent = "Overlay",
                        _name
                    }
                };
                return _element;
            }

            public static void CreatePanel(ref CuiElementContainer _container, string _name, string _parent, string _color, string _anchorMin, string _anchorMax, bool _cursorOn = false, float _fadeIn = 0f, float _fadeOut = 0f, string _mat2 ="", string _OffsetMin = "", string _OffsetMax = "" )
            {
                _container.Add(new CuiPanel
                {
                    Image = { Color = _color, Material = _mat2, FadeIn = _fadeIn },
                    RectTransform = { AnchorMin = _anchorMin, AnchorMax = _anchorMax, OffsetMin = _OffsetMin, OffsetMax = _OffsetMax },
                    FadeOut = _fadeOut, CursorEnabled = _cursorOn
                },
                _parent,
                _name);
            }

            public static void CreateImage(ref CuiElementContainer _container, string _name, string _parent, string _image, string _anchorMin, string _anchorMax, float _fadeIn = 0f, float _fadeOut = 0f, string _OffsetMin = "", string _OffsetMax = "")
            {
                if (_image.StartsWith("http") || _image.StartsWith("www"))
                {
                    _container.Add(new CuiElement
                    {   
                        Name = _name,
                        Parent = _parent,
                        FadeOut = _fadeOut,
                        Components =
                        {
                            new CuiRawImageComponent { Url = _image, Sprite = "assets/content/textures/generic/fulltransparent.tga", FadeIn = _fadeIn},
                            new CuiRectTransformComponent { AnchorMin = _anchorMin, AnchorMax = _anchorMax, OffsetMin = _OffsetMin, OffsetMax = _OffsetMax }
                        }
                        
                    });
                }
                else
                {
                    _container.Add(new CuiElement
                    {
                        Parent = _parent,
                        Components =
                        {
                            new CuiRawImageComponent { Png = _image, Sprite = "assets/content/textures/generic/fulltransparent.tga", FadeIn = _fadeIn},
                            new CuiRectTransformComponent { AnchorMin = _anchorMin, AnchorMax = _anchorMax }
                        }
                    });
                }
            }

            public static void PullFromAssets(ref CuiElementContainer _container, string _name, string _parent, string _color, string _sprite, float _fadeIn = 0f, float _fadeOut = 0f, string _anchorMin = "0 0", string _anchorMax = "1 1", string _material = "assets/icons/iconmaterial.mat")
            { 
                //assets/content/textures/generic/fulltransparent.tga MAT
                _container.Add(new CuiElement
                {   
                    Parent = _parent,
                    Name = _name,
                    Components =
                            {
                                new CuiImageComponent { Material = _material, Sprite = _sprite, Color = _color, FadeIn = _fadeIn},
                                new CuiRectTransformComponent {AnchorMin = _anchorMin, AnchorMax = _anchorMax}
                            },
                    FadeOut = _fadeOut
                });
            }

            public static void CreateInput(ref CuiElementContainer _container, string _name, string _parent, string _color, int _size, string _anchorMin, string _anchorMax, string _font = "permanentmarker.ttf", string _command = "command.processinput", TextAnchor _align = TextAnchor.MiddleCenter)
            {
                _container.Add(new CuiElement
                {
                    Parent = _parent,
                    Name = _name,

                    Components =
                    {
                        new CuiInputFieldComponent
                        {

                            Text = "0",
                            CharsLimit = 250,
                            Color = _color,
                            IsPassword = false,
                            Command = _command,
                            Font = _font,
                            FontSize = _size,
                            Align = _align
                        },

                        new CuiRectTransformComponent
                        {
                            AnchorMin = _anchorMin,
                            AnchorMax = _anchorMax

                        }

                    },
                });
            }

            public static void CreateText(ref CuiElementContainer _container, string _name, string _parent, string _color, string _text, int _size, string _anchorMin, string _anchorMax, TextAnchor _align = TextAnchor.MiddleCenter, string _font = "robotocondensed-bold.ttf", float _fadeIn = 0f, float _fadeOut = 0f, string _outlineColor = "0 0 0 0", string _outlineScale ="0 0")
            {   
                _container.Add(new CuiElement
                {
                    Parent = _parent,
                    Name = _name,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = _text,
                            FontSize = _size,
                            Font = _font,
                            Align = _align,
                            Color = _color,
                            FadeIn = _fadeIn,
                        },

                        new CuiOutlineComponent
                        {
                            
                            Color = _outlineColor,
                            Distance = _outlineScale
                            
                        },

                        new CuiRectTransformComponent
                        {
                             AnchorMin = _anchorMin,
                             AnchorMax = _anchorMax
                        }
                    },
                    FadeOut = _fadeOut
                });
            }

            public static void CreateButton(ref CuiElementContainer _container, string _name, string _parent, string _color, string _text, int _size, string _anchorMin, string _anchorMax, string _command = "", string _close = "", string _textColor = "0.843 0.816 0.78 1", float _fade = 1f, TextAnchor _align = TextAnchor.MiddleCenter, string _font = "", string _material = "assets/content/ui/uibackgroundblur-ingamemenu.mat")
            {       
               
                _container.Add(new CuiButton
                {
                    Button = { Close = _close, Command = _command, Color = _color, Material = _material, FadeIn = _fade},
                    RectTransform = { AnchorMin = _anchorMin, AnchorMax = _anchorMax },
                    Text = { Text = _text, FontSize = _size, Align = _align, Color = _textColor, Font = _font, FadeIn = _fade}
                },
                _parent,
                _name);
            }
        }
        #endregion
        
        #region [Blueprint Data]
        
        private void SaveData()
        {
            if (bps != null)
            Interface.Oxide.DataFileSystem.WriteObject($"{Name}/Blueprints", bps);
        }
        
        private Dictionary<string, Bps> bps;
        
        private class Bps
        {   
            public string Name;
            public string Image;
            public ulong SkinID;
            public string Category;
            public int Tier;
            public int ResearchCost;
            public Dictionary<string, int> Resources = new Dictionary<string, int>{}; 
        }
        
        private void LoadData()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile($"{Name}/Blueprints"))
            {
                bps = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, Bps>>($"{Name}/Blueprints");
            }
            else
            {
                bps = new Dictionary<string, Bps>();
        
                CreateExamples();
                SaveData();
            }
        }
        
        private void CreateExamples()
        {   
            bps.Add("multiplegrenadelauncher", new Bps());
            bps["multiplegrenadelauncher"].Name = "Grenade Launcher";
            bps["multiplegrenadelauncher"].Image = "multiplegrenadelauncher.png";
            bps["multiplegrenadelauncher"].SkinID = 0;
            bps["multiplegrenadelauncher"].Category = "weapons";
            bps["multiplegrenadelauncher"].Tier = 3;
            bps["multiplegrenadelauncher"].Resources.Add("metal.fragments", 750);
            bps["multiplegrenadelauncher"].Resources.Add("metalpipe", 6);
            bps["multiplegrenadelauncher"].Resources.Add("metal.refined", 150);

            SaveData();
        }   
        
        #endregion

        #region [Player Data]
        
        private void SavePlayerData()
        {
            if (playerBps != null)
            Interface.Oxide.DataFileSystem.WriteObject($"{Name}/PlayerBlueprints", playerBps);
        }
        
        private Dictionary<ulong, PlayerBps> playerBps;
        
        private class PlayerBps
        {   
            public List<string> bp = new List<string>{};  
        }
        
        private void LoadPlayerData()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile($"{Name}/PlayerBlueprints"))
            {
                playerBps = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerBps>>($"{Name}/PlayerBlueprints");
            }
            else
            {
                playerBps = new Dictionary<ulong, PlayerBps>();
        
                CreatePlayerExamples();
                SavePlayerData();
            }
        }
        
        private void CreatePlayerExamples()
        {   
            playerBps.Add(76561198207548749, new PlayerBps());  
            playerBps[76561198207548749].bp.Add("rifle.lr300");
            playerBps[76561198207548749].bp.Add("fun.boomboxportable");
            SavePlayerData();
        }
        
        #endregion

        #region [Names Data]
        
        private void SaveNamesData()
        {
            if (nameReplace != null)
            Interface.Oxide.DataFileSystem.WriteObject($"{Name}/CustomNames_Resources", nameReplace);
        }
        
        private Dictionary<string, string> nameReplace;
        
        
        
        private void LoadNamesData()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile($"{Name}/CustomNames_Resources"))
            {
                nameReplace = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, string>>($"{Name}/CustomNames_Resources");
            }
            else
            {
                nameReplace = new Dictionary<string, string>();
        
                CreateNameReplacements();
                SaveNamesData();
            }
        }
        
        private void CreateNameReplacements()
        {   
                nameReplace.Add("metal.refined", "HQM");
                nameReplace.Add("propanetank", "Propane Tank");
                nameReplace.Add("metalpipe", "Pipes");
                nameReplace.Add("wiretool", "Wires");
        }
        
        #endregion

        #region [Image Handling]

        [PluginReference] Plugin ImageLibrary;

        //list for load order
        private List<string> imgList = new List<string>();

        private void DownloadImages()
        {   
            if (ImageLibrary == null) 
            { Puts($"(! MISSING) ImageLibrary not found, image load speed will be significantly slower."); return; }
            
            //add to load order
            imgList.Add("https://rustlabs.com/img/items180/rifle.lr300.png");
            imgList.Add("https://rustplugins.net/products/craftmenu/blueprint.png");
            imgList.Add("https://rustplugins.net/products/craftmenu/mini.png");
            ImageLibrary.Call("AddImage", "https://rustplugins.net/products/craftmenu/blueprint.png", "https://rustplugins.net/products/craftmenu/blueprint.png");
            ImageLibrary.Call("AddImage", "https://rustlabs.com/img/items180/rifle.lr300.png", "https://rustlabs.com/img/items180/rifle.lr300.png");
            
            string prefix = "https://rustlabs.com/img/items180/";
            //add item images
            foreach (string item in bps.Keys) 
            {   
                if (!bps[item].Image.StartsWith("http"))
                {
                    ImageLibrary.Call("AddImage", prefix + bps[item].Image, prefix + bps[item].Image);
                    if (!imgList.Contains(bps[item].Image))
                        imgList.Add(prefix + bps[item].Image);
                }
                else
                {
                    ImageLibrary.Call("AddImage", bps[item].Image, bps[item].Image);
                    if (!imgList.Contains(bps[item].Image))
                        imgList.Add(bps[item].Image);
                }
            }
            //add category images
            foreach (string category in config.main.cat.Keys) 
            { 
                if (!config.main.cat[category].StartsWith("assets"))
                {
                    ImageLibrary.Call("AddImage", config.main.cat[category], config.main.cat[category]);
                    imgList.Add(config.main.cat[category]);
                }
            }
            //call load order
            ImageLibrary.Call("ImportImageList", "CraftMenu", imgList);
        }

        private void ImageQueCheck()
        {   
            int imgCount = imgList.Count();
            int downloaded = 0;
            foreach (string img in imgList)
            {
                if ((bool) ImageLibrary.Call("HasImage", img))
                    downloaded++; 
            }

            if (imgCount > downloaded)
                Puts($"(!) Stored Images ({downloaded}/{imgCount}). Reload ImageLibrary and then CraftMenu plugin to start download order.");
            
            if (imgCount == downloaded)
                Puts($"Stored Images ({downloaded}). All images has been successfully stored in image library.");
        }

        private string Img(string url)
        {   //img url been used as image names
            if (ImageLibrary != null) 
            {   
                if (!(bool) ImageLibrary.Call("HasImage", url))
                    return url;
                else
                    return (string) ImageLibrary?.Call("GetImage", url);
            }
            else 
                return url;
        }

        #endregion
        
        #region [Config] 
        
        private Configuration config;
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<Configuration>();
            SaveConfig();
        }
        
        protected override void LoadDefaultConfig()
        {
            config = Configuration.CreateConfig();
        }
        
        protected override void SaveConfig() => Config.WriteObject(config);     
        
        class Configuration
        {   
            [JsonProperty(PropertyName = "Main Settings")]
            public MainSet main { get; set; }
        
            public class MainSet
            {
                [JsonProperty("Wipe Blueprints at Map Wipe")]
                public bool wipe {get; set;}

                [JsonProperty("Sound Effects")]
                public bool fx {get; set;}
        
                [JsonProperty("Categories (Max 6)")]
                public Dictionary<string, string> cat { get; set; }

                [JsonProperty("Permissions required for each category")]
                public bool perms { get; set; }
            } 

            [JsonProperty(PropertyName = "Crafting Time")]
            public CT ct { get; set; }
        
            public class CT
            {
                [JsonProperty("Enabled")]
                public bool craftQue {get; set;}

                [JsonProperty("Default Craft Time for all items (in seconds)")]
                public int defaultTime {get; set;}
        
                [JsonProperty("Specific Craft Time (in seconds)")]
                public Dictionary<string, int> excp { get; set; }
            }

            public static Configuration CreateConfig()
            {
                return new Configuration
                {   
                    main = new CraftMenu.Configuration.MainSet
                    {       
                        wipe = false,
                        fx = true,
                        cat = new Dictionary<string, string>
                        { 
                            { "construction", "assets/icons/construction.png" },
                            { "weapons", "assets/icons/bullet.png" },
                            { "clothing", "assets/icons/clothing.png" },
                            { "electrical", "assets/icons/electric.png" },
                            { "vehicles", "assets/icons/horse_ride.png" },
                            { "dlc", "assets/icons/download.png" },
                        }, 
                        perms = false,
                    },

                    ct = new CraftMenu.Configuration.CT
                    {        
                        craftQue = false,
                        defaultTime = 5,
                        excp = new Dictionary<string, int>
                        { 
                            { "rifle.m39", 60 },
                            { "pistol.m92", 45 },
                            { "rifle.lr300", 80 },
                            { "rifle.l96", 120 },
                            { "lmg.m249", 200 },
                            { "multiplegrenadelauncher", 200 },
                        }
                    },
                };
            }
        }
        #endregion
        
    }
}