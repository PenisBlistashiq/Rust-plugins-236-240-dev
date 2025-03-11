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
using VLB;

namespace Oxide.Plugins
{
    [Info("PlaceAnything", "David", "1.0.5")]
    [Description("Place any entity you want.")]

    public class PlaceAnything : RustPlugin
    {   
        [PluginReference]
        private Plugin CopyPaste, EntityScaleManager;

        static PlaceAnything plugin;

        private void Init() => plugin = this; 

        #region [Hooks]

        private void OnServerInitialized()
        {
            LoadData();
            AddCompToAll();
        }

        private void Unload()
        {
            KillAllComps();
        }

        private void OnHammerHit(BasePlayer player, HitInfo info)
        {   
            var ent = info.HitEntity.GetComponentInParent<BaseEntity>();
            if (ent == null) return;
            if (ent.Health() != ent.MaxHealth()) return;
            if (ent.OwnerID == player.userID)
            {   
                string itemName = null;
                foreach (string item in placeable.Keys)
                {
                    if (placeable[item].Prefab == ent.PrefabName)
                    {
                        itemName = item;
                        break;
                    }      
                }
                if (itemName == null) return;
                
                if (!placeable[itemName].CanBePickedUp) 
                { SendReply(player, "This item cannot be picked up."); return; }

                if (placeable[itemName].NeedsTCAuth && !player.IsBuildingAuthed())
                { SendReply(player, "You need building authorization to pick up this item."); return; }
            
        
                var run = player.GetComponent<Mono>(); 
                if (run == null) player.gameObject.GetOrAddComponent<Mono>();
                if (run != null) run.RunPickUp(player, ent, itemName);  
                
            }
        }

        void OnEntityBuilt(Planner plan, GameObject go)
        {
            
            var player = plan.GetOwnerPlayer();
            Item item = player.GetActiveItem();
            if (item.name != null) 
            {
                if (placeable.ContainsKey(item.name))
                {
                    BaseEntity initEnt = go.GetComponent<BaseEntity>();
                    
                    var pos = initEnt.transform.position;
                    var rot = initEnt.transform.rotation;
                    NextTick(() =>{
                    
                        initEnt.Kill();

                        if (placeable[item.name].NeedsTCAuth)
                        {
                            if (!player.IsBuildingAuthed())
                            {
                                RefundItem(player, item.name);
                                SendReply(player, "You need building authorization to place this item.\n<size=10>Item was put back into your inventory.</size>");
                                return;
                            }                                    
                        }  

                        if (placeable[item.name].Prefab.Contains("copypaste"))
                        {   
                            string[] split = placeable[item.name].Prefab.Split('/');
                            var options = new List<string>{ "auth", "true" };
                            CopyPaste.Call("TryPasteFromVector3", pos, 0f, $"{split[1]}", options.ToArray());
                            return;
                        }
                        
                        BaseEntity entity = GameManager.server.CreateEntity(placeable[item.name].Prefab, new Vector3(pos.x, pos.y + placeable[item.name].AdjustHeight, pos.z), new Quaternion(rot.x, rot.y, rot.z, rot.w));
                        entity.OwnerID = player.userID;
                        entity._name = item.name;
                        entity.Spawn();
                        
                        if (EntityScaleManager != null && config.main.resize.ContainsKey(item.name))
                           EntityScaleManager.CallHook("API_ScaleEntity", entity, config.main.resize[item.name]);
                           
                    });

                }
            }
        }

        #endregion

        #region [Functions / Methods]

        [ChatCommand("gimme")]
        private void gimme(BasePlayer player)
        {
            if (player == null) return;
            if (!player.IsAdmin) return;
            
            foreach (string itemName in placeable.Keys)
            {
                var item = ItemManager.CreateByName(placeable[itemName].BaseItem, 10);
                item.name = itemName;
                item.skin = placeable[itemName].SkinID;
                item.MarkDirty();
                player.GiveItem(item);
            }
        }

        private void RefundItem(BasePlayer player, string itemName)
        {   
            if (!placeable.ContainsKey(itemName)) return;
            var item = ItemManager.CreateByName($"{placeable[itemName].BaseItem}", 1);
            item.name = itemName;
            item.skin = placeable[itemName].SkinID;
            item.MarkDirty();
            player.GiveItem(item);
        }

        #endregion

        #region [MonoBehavior]

        private void AddCompToAll()
        {
            foreach (var _player in BasePlayer.activePlayerList)
                _player.gameObject.GetOrAddComponent<Mono>();      
        }

        private void KillAllComps()
        {
            foreach (var _player in BasePlayer.activePlayerList)
            {
               var run = _player.GetComponent<Mono>(); 
               UnityEngine.Object.Destroy(run);
            }   
        }

        private class Mono : MonoBehaviour
		{
			BasePlayer player;
            float progress;
            BaseEntity entity;
            string item;
			
			void Awake() => player = GetComponent<BasePlayer>();
            
            void BarProg()
            {   
                if (progress >= 1)
                {   
                    if (IsInvoking(nameof(BarProg)) == true) 
                        CancelInvoke(nameof(BarProg));

                    plugin.DestroyBar(player);
                    progress = 0.0f;
                    entity.Kill();
                    plugin.RefundItem(player, item);
                    return;
                }
               
                if (player.serverInput.IsDown(BUTTON.FIRE_PRIMARY) == true)
                {
                    progress += 0.01f;
                    plugin.CreateProgressBar(player, progress);
                }
                else
                {   
                    progress = 0.0f;
                    if (IsInvoking(nameof(BarProg)) == true) 
                        CancelInvoke(nameof(BarProg));

                    plugin.DestroyBar(player);
                    return;
                }
                
            }
            
            public void RunPickUp(BasePlayer player, BaseEntity _entity, string itemName)
			{   
                if (player == null) return;

                entity = _entity;
                item = itemName;

                if (plugin.config.main.pickup && plugin.config.main.pickup != null)
                {
                    entity.Kill();
                    plugin.RefundItem(player, itemName);
                    return;
                }
               
                plugin.CreatePickUpBar(player);
               
                if (IsInvoking(nameof(BarProg)) == true) 
                {
                    CancelInvoke(nameof(BarProg));
                }
                InvokeRepeating(nameof(BarProg), 0.02f, 0.02f); 
			}

        }

        #endregion

        #region [UI]

        private void CreatePickUpBar(BasePlayer player)
        {   
            var progBar = CUIClass.CreateOverlay("empty", "0 0 0 0", "0 0", "0 0", false, 0.0f, "assets/icons/iconmaterial.mat"); //assets/content/ui/uibackgroundblur.mat
            //offset
            CUIClass.CreatePanel(ref progBar, "progbar_main", "Hud", "1 1 1 0.5", "0.5 0.5", "0.5 0.5", false, 0.0f, 0.0f, "assets/icons/iconmaterial.mat", "-85 -30", "85 -25");
            
            //CUIClass.CreatePanel(ref progBar, "progbar_progress", "progbar_main", "1 1 1 1", "0.0 0", $"{progress} 1", true, 0.0f, 0f, "assets/icons/iconmaterial.mat"); 
            CUIClass.PullFromAssets(ref progBar, "progbar_icon", "progbar_main", "1 1 1 1", "assets/icons/pickup.png", 0.0f, 0f, "0.005 1.4", "0.1 4");
            CUIClass.CreateText(ref progBar, "progbar_text", "progbar_main", "1 1 1 1", $"PICKING UP", 10, "0.1 1.01", "1 3.5", TextAnchor.UpperLeft, $"robotocondensed-bold.ttf", 0.0f);  
          
            DestroyBar(player); 
            CuiHelper.AddUi(player, progBar); 
        }
        
        private void DestroyBar(BasePlayer player)
        {   
          CuiHelper.DestroyUi(player, "empty"); 
          CuiHelper.DestroyUi(player, "progbar_main");
        }

        private void CreateProgressBar(BasePlayer player, float progress)
        {   
            var progBar = CUIClass.CreateOverlay("empty", "0 0 0 0", "0 0", "0 0", false, 0.0f, "assets/icons/iconmaterial.mat"); //assets/content/ui/uibackgroundblur.mat
            
                CUIClass.CreatePanel(ref progBar, "progbar_progress", "progbar_main", "1 1 1 1", "0.0 0", $"{progress} 1", false, 0.0f, 0f, "assets/icons/iconmaterial.mat"); 
            
            CuiHelper.DestroyUi(player, "empty");
            CuiHelper.DestroyUi(player, "progbar_progress"); 
            CuiHelper.AddUi(player, progBar); 
            CuiHelper.DestroyUi(player, "empty"); 
        }

        #endregion

        #region [Data]
        
        private void SaveData()
        {
            if (placeable != null)
            Interface.Oxide.DataFileSystem.WriteObject($"{Name}/Entities", placeable);
        }
        
        private Dictionary<string, Placeable> placeable;
        
        private class Placeable
        {   
            public string BaseItem;
            public ulong SkinID;
            public string Prefab;
            public bool NeedsTCAuth;
            public bool CanBePickedUp;
            public float AdjustHeight;
        }
        
        private void LoadData()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile($"{Name}/Entities"))
            {
                placeable = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, Placeable>>($"{Name}/Entities");
            }
            else
            {
                placeable = new Dictionary<string, Placeable>();
        
                CreateExamples();
                SaveData();
            }
        }
        
        private void CreateExamples()
        {   
            string[] name = {
                "Gambling Wheel",
                "Gambling Terminal",
                "CH47",
                "PumpJack",
                "Water Pump (Ground)",
                "Recycler",
                "Slot Machine",
                "Palm Medium",
                "Palm Short",
                "Mormon Tea Bush",
                "Cactus High",
                "Cactus Small",
                "Pine Tree",
                "Birch Tree Small"

            };

            string[] prefabs = {
                "assets/prefabs/misc/casino/bigwheel/big_wheel.prefab",
                "assets/prefabs/misc/casino/bigwheel/bigwheelbettingterminal.prefab",
                "assets/prefabs/npc/ch47/ch47.entity.prefab",
                "assets/bundled/prefabs/static/pumpjack-static.prefab",
                "assets/prefabs/deployable/playerioents/waterpump/water.pump.deployed.prefab",
                "assets/bundled/prefabs/static/recycler_static.prefab",
                "assets/prefabs/misc/casino/slotmachine/slotmachine.prefab",
                "assets/bundled/prefabs/autospawn/resource/v3_arid_forest/palm_tree_med_a_entity.prefab",
                "assets/bundled/prefabs/autospawn/resource/v3_arid_forest/palm_tree_short_a_entity.prefab",
                "assets/bundled/prefabs/autospawn/resource/v3_bushes_arid_desert/mormon_tea_c.prefab",
                "assets/bundled/prefabs/autospawn/resource/v3_arid_cactus/cactus-1.prefab",
                "assets/bundled/prefabs/autospawn/resource/v3_arid_cactus/cactus-4.prefab",
                "assets/bundled/prefabs/autospawn/resource/v3_tundra_forestside/pine_d.prefab",
                "assets/bundled/prefabs/autospawn/resource/v3_temp_forestside/birch_small_temp.prefab"
            };

            string[] baseItem = {
                "spinner.wheel",
                "mailbox",
                "abovegroundpool",
                "abovegroundpool",
                "box.wooden",
                "box.wooden.large",
                "arcade.machine.chippy",
                "clone.hemp",
                "clone.hemp",
                "clone.hemp",
                "clone.hemp",
                "clone.hemp",
                "clone.hemp",
                "clone.hemp"

            };

            ulong[] skinId = {
                2682493718,
                2682493107,
                2682477500,
                2682458242,
                2682449233,
                2406283172,
                2682435313,
                2681722770,
                2681722770,
                2685219527,//bush
                2685221352,//cactus
                2685221352,//cactus
                2685224593,//tree
                2685224593

            };

            bool[] tc = {
                true,
                true,
                false,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true
            };

            bool[] pick = {
                true,
                true,
                false,
                true,
                false,
                true,
                true,
                false,
                false,
                false,
                false,
                false,
                false,
                false
            };
            
            for (var i = 0; i < 14; i++)
            {   
                float he = 0;
                if (name[i] == "Cactus High") he = -1.5f;
                placeable.Add(name[i], new Placeable());     
                placeable[name[i]].SkinID =  skinId[i];
                placeable[name[i]].BaseItem = baseItem[i];
                placeable[name[i]].Prefab = prefabs[i];
                placeable[name[i]].NeedsTCAuth = tc[i];
                placeable[name[i]].CanBePickedUp = pick[i];
                placeable[name[i]].AdjustHeight = he;
            }
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
            public  Main main { get; set; }
        
            public class Main
            {
                [JsonProperty("Resize Entities - ('item name':'scale 0 to 1') ")]
                public Dictionary<string, float> resize { get; set; }

                [JsonProperty("Pick up on first hit")]
                public bool pickup { get; set; }
            } 
        
            public static Configuration CreateConfig()
            {
                return new Configuration
                {   
                    main = new PlaceAnything.Configuration.Main
                    {   
                        resize = new Dictionary<string, float>
                        {
                            { "PumpJack", 0.5f },
                            { "Gambling Wheel", 0.5f },
                            
                        },
                        pickup = false
                    },
                };
            }
        }
        #endregion
    }
}
