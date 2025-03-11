﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;
 using VLB;

 namespace Oxide.Plugins
{
    [Info("RFTeleports", "LAGZYA", "1.0.7")]
    public class RFTeleports : RustPlugin
    {
        #region Cfg
        private ConfigData cfg { get; set; }  
        private class ConfigData
        {
            [JsonProperty("Разрешить телепорт c коптера?(true = да)")] public bool blockcopter = true;
            [JsonProperty("Разрешить телепорт c лошади?(true = да)")] public bool blockhorse = true;
            [JsonProperty("Разрешить телепорт c каргошипа?(true = да)")] public bool blockcargo = true;
            [JsonProperty("Разрешить телепорт c воздушного шара?(true = да)")] public bool blockhot = true;
            [JsonProperty("Разрешить телепорт во время плавания?(true = да)")] public bool blockswim = true;
            [JsonProperty("Разрешить телепорт c кровотечением?(true = да)")] public bool blockblood = true;
            [JsonProperty("Кол-во кровотечения для блока")] public int blood = 25;
            [JsonProperty("Разрешить телепорт если жарко?(true = да)")] public bool blocktemp = true;
            [JsonProperty("Кол-во тепла для блока")] public int temp = 15;
            [JsonProperty("Разрешить телепорт если холодно?(true = да)")]public bool blockсcold = true;
            [JsonProperty("Кол-во холода для блока")] public int cold = 25;
            [JsonProperty("Разрешить телепорт если радиация?(true = да)")]public bool blockrad = true;
            [JsonProperty("Кол-во радиации для блока")] public int rad = 25;
            [JsonProperty("Разрешить телепорт в запрете строительства?(true = да)")] public bool blockbuild = true;
            [JsonProperty("Скорость телепорта на спальнике(Чем больше число тем быстрее телепортация)")] public int sleepbeg = 1;
            [JsonProperty("Скорость телепорта на кровате(Чем больше число тем быстрее телепортация))")] public int bad = 2;
            [JsonProperty("Время перезарядки на спальнике(Секунды)")] public int sleepcd = 300;
            [JsonProperty("Время перезарядки на кровате(Секунды)")] public int badcd = 90;
            [JsonProperty("Включить сетхом (true = да)")] public bool hometeleport = true;
            [JsonProperty("Включить телепорт к игрокам (true = да)")] public bool playerteleport = true;

            [JsonProperty("Привелегия: кол-во домов")]
            public Dictionary<string, int> permHomeLimit;

            [JsonProperty("Привелегия: Ускорение телепорта(Чем больше число тем быстрее телепортация)")]
            public Dictionary<string, int> permTPRTIME;
            
            [JsonProperty("Привелегия: время перезарядки тп к игроку")]
            public Dictionary<string, int> permTPRCD;
            public static ConfigData GetNewConf()
            {
                var newConfig = new ConfigData();
                newConfig.permTPRCD =new Dictionary<string, int>()
                {
                    ["rfteleports.default"] = 1,
                    ["rfteleports.vip"] = 3,
                };
                newConfig.permHomeLimit = new Dictionary<string, int>()
                {
                    ["rfteleports.default"] = 300,
                    ["rfteleports.vip"] = 150,
                }; 
                newConfig.permTPRTIME = new Dictionary<string, int>()
                {
                    ["rfteleports.default"] = 1,
                    ["rfteleports.vip"] = 2,
                }; 
                return newConfig;
            }
        } 

        protected override void LoadDefaultConfig()
        {
            cfg = ConfigData.GetNewConf();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(cfg);
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                cfg = Config.ReadObject<ConfigData>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }
        
        int GetHomeLimit(ulong uid)
        {
            int max = 0;
            foreach (var privilege in cfg.permHomeLimit) if (permission.UserHasPermission(uid.ToString(), privilege.Key)) max = Mathf.Max(max, privilege.Value);
            return max;
        }

        #endregion

        private Dictionary<ulong, PlayerData> _homeList = new Dictionary<ulong, PlayerData>();

        void OnServerSave()
        { 
            Interface.Oxide.DataFileSystem.WriteObject("RFTeleport", _homeList);
            Puts("Произошло сохранение даты!");
        }
        private void OnServerInitialized()
        {
            _homeList = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerData>>("RFTeleport");
            foreach (var tt in cfg.permHomeLimit)
            {
                if(!permission.PermissionExists(tt.Key))
                    permission.RegisterPermission(tt.Key, this);
            }
            foreach (var tt in cfg.permTPRCD)
            {
                if(!permission.PermissionExists(tt.Key))
                    permission.RegisterPermission(tt.Key, this);
            }
            permission.RegisterPermission("rfteleports.admin", this);
            foreach (var tt in cfg.permTPRTIME)
            {
                if(!permission.PermissionExists(tt.Key))
                    permission.RegisterPermission(tt.Key, this);
            }
            foreach (var basePlayer in BasePlayer.activePlayerList)
            {
                OnPlayerConnected(basePlayer);
            }
        } 

        void OnPlayerConnected(BasePlayer player)
        {
            if(_homeList.ContainsKey(player.userID)) return;
            _homeList.Add(player.userID, new PlayerData()
            {
                _homeList = new Dictionary<int, Homes>(),
                HOMECD = CurrentTime(),
                TPRCD = CurrentTime(),
                NickName = player.displayName,
            });
        }
        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            PlayerData f;
            if (item.info.shortname != "rf.detonator") return;
            var player = container.playerOwner;
            if (player != null && container == player.inventory.containerMain || player != null && container == player.inventory.containerBelt)
            { 
                if (!_homeList.TryGetValue(player.userID, out f)) return;
                var racia = item.GetHeldEntity().GetComponent<Detonator>();
                if (f._homeList.Count <= 0 || racia.frequency != 0) return;
                racia.frequency = f._homeList.ToList()[0].Key;
                player.SendNetworkUpdate();
            }
        }
        private void Unload()
        {
            Interface.Oxide.DataFileSystem.WriteObject("RFTeleport", _homeList);
            foreach (var basePlayer in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(basePlayer, AcceptLayer);
            }
            foreach (var rfTeleport in UnityEngine.Object.FindObjectsOfType<RFTeleport>())
            {
                rfTeleport.OnDestroy();
            }
            foreach (var rfTeleport in UnityEngine.Object.FindObjectsOfType<RFPager>())
            {
                rfTeleport.OnDestroy();
            }
        }

        class PlayerData
        {
            public string NickName;
            public double HOMECD;
            
            public double TPRCD;
            public Dictionary<int, Homes> _homeList = new Dictionary<int, Homes>();
            public double IsTPR()
            {
                return Math.Max(TPRCD - CurrentTime(), 0);
            }
            public double IsHome()
            {
                return Math.Max(HOMECD - CurrentTime(), 0);
            }
        }

        class Homes
        {
            public Vector3 position;
            public string ShortName;
            public uint netId;
        }

        public static RFTeleports ins;

        private void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (player == null || newItem == null) return;
            if (newItem.info.shortname == "rf.detonator") player.gameObject.GetOrAddComponent<RFTeleport>();
            if(newItem.info.shortname == "rf_pager" && cfg.playerteleport) player.gameObject.GetOrAddComponent<RFPager>();
        }
        private string AcceptLayer = "AcceptLayerSH";
        void AcceptHouse(BasePlayer player, uint numid )
        { 
            CuiHelper.DestroyUi(player, AcceptLayer);
            var cont = new CuiElementContainer();
            cont.Add(new CuiPanel()
            {
                Image =
                {
                    Color = "0.25 0.25 0.25 0.56", FadeIn = 1f,
                    Sprite = "assets/content/ui/ui.background.transparent.linearltr.tga"
                },
                RectTransform =
                {
                    AnchorMin = "0 0.5", 
                    AnchorMax = "0 0.5", 
                    OffsetMin = "0 -25", 
                    OffsetMax = "250 25"
                }
            }, "Overlay", AcceptLayer);
            cont.Add(new CuiButton()
            {
                Text =
                {
                    FadeIn = 1f, Text = "<size=14>Телепортация</size>\nЧтобы сохранить точку телепорта на этом спальнике/кровати, <color=#FFFF00><size=16>нажмите сюда</size></color>.", Align = TextAnchor.MiddleLeft, FontSize = 12
                },
                RectTransform =
                {
                    AnchorMin = "0.04 0", AnchorMax = "2 1.3"
                },
                Button =
                {
                    Color = "0 0 0 0",
                    Command = $"rfteleport sethome {numid}",
                    Close = AcceptLayer
                }
            }, AcceptLayer);
            CuiHelper.AddUi(player, cont);
            timer.Once(10f, () => CuiHelper.DestroyUi(player, AcceptLayer));
        }
        void HomeGenerate(uint nument, BasePlayer player)
        {
            if(!cfg.hometeleport) return;
            var entity = BaseNetworkable.serverEntities.Find(nument) as SleepingBag;
            if(entity == null) return;
            PlayerData f;
            int number = Core.Random.Range(1, 9999);
            if (_homeList.TryGetValue(player.userID, out f))
            {
                if (f._homeList.ContainsKey(number))
                {
                    HomeGenerate(entity.net.ID, player);
                    return;
                }

                f._homeList.Add(number, new Homes()
                {
                    ShortName = entity.ShortPrefabName,
                    position = entity.transform.position + Vector3.up,
                    netId = entity.net.ID
                });
            }

            entity.niceName = $"HOME: {number}";
            entity.SendNetworkUpdate();
            ReplySend(player, "Вы поставили спальник или кровать. Ваш номер для телепорта через рацию: " + number);
            Effect x = new Effect("assets/prefabs/misc/easter/painted eggs/effects/gold_open.prefab", player, 0, new Vector3(), new Vector3());
            EffectNetwork.Send(x, player.Connection);
        }

        void OnEntitySpawned(SleepingBag entity)
        {
            if(!cfg.hometeleport) return;
            PlayerData playerData;
            var player = BasePlayer.FindByID(entity.OwnerID);
            if (player == null) return;
            if(player.IsBuildingAuthed())
                if(_homeList.TryGetValue(player.userID, out playerData))
                {
                    if (GetHomeLimit(player.userID) > playerData._homeList.Count)
                        AcceptHouse(player, entity.net.ID);
                }
                else
                {
                    AcceptHouse(player, entity.net.ID);
                }
        }
        void Init()
        {
            ins = this;
        }

        [ConsoleCommand("rfteleport")]
        void RFCommand(ConsoleSystem.Arg arg)
        {
            PlayerData playerData;
            var player = arg.Player();
            switch (arg.Args[0])
            {
                case "sethome": 
                    HomeGenerate(uint.Parse(arg.Args[1]), player);
                    CuiHelper.DestroyUi(player, AcceptLayer);
                    break;
            }
        }
        private static string Blur = "assets/content/ui/uibackgroundblur.mat";
        Dictionary<ulong, int> _pagerList = new Dictionary<ulong, int>();
        class RFPager : MonoBehaviour
        {
            private BasePlayer player;
            private static PagerEntity pager;
            private int num;
            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                var ent = BaseNetworkable.serverEntities.Find(player.GetActiveItem().instanceData.subEntity);
                if(player.GetActiveItem() == null || ent == null)
                {
                    Destroy(this);
                    return;
                } 
                 
                pager = ent.GetComponent<PagerEntity>();
                if (ins._pagerList.ContainsValue(pager.GetFrequency()))
                {
                    ins.ReplySend(player, "Введите другую частоту!");
                    Destroy(this);
                    return;
                }
                ins._pagerList.Add(player.userID, pager.GetFrequency());
                num = pager.GetFrequency();
                ins.ReplySend(player, "Вы взяли пейджер.Сообщите свою частоту человеку,чтобы тот мог телепортироваться!");
            }

            private void Update()
            {
                if(player.GetActiveItem() == null ||player.GetActiveItem().info.itemid != -566907190)
                {
                      OnDestroy();
                      return;
                }
                if (ins._pagerList.ContainsKey(player.userID) && ins._pagerList[player.userID] != num)
                    ins._pagerList[player.userID] = num;

            }

            public void OnDestroy()
            {
                ins._pagerList.Remove(player.userID);
                Destroy(this);
            } 
        }
        public class RFTeleport : MonoBehaviour
        {
            private BasePlayer playerTarget;
            private Item rftrans;
            private Detonator racia;
            private BasePlayer player;
            private double procent = 0;
            private PlayerData playerData;
            private Dictionary<int, Homes> homes;
            private string color = ins.HexToRustFormat("#2df79b8A");
            private int tprtime;

            private string text = "stop";

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                rftrans = player.GetActiveItem();
                racia = rftrans.GetHeldEntity().GetComponent<Detonator>();
                if (!ins._homeList.TryGetValue(player.userID, out playerData)) OnDestroy();
                else homes = playerData._homeList;
                tprtime = GetTPRTime(player.userID);
            } 

            public void OnDestroy()
            {
                CuiHelper.DestroyUi(player, "ProgressBar");
                Destroy(this);
            }

            private void StartUI()
            {
                CuiHelper.DestroyUi(player, "ProgressBar");
                var cont = new CuiElementContainer();
                cont.Add(new CuiPanel()
                {
                    Image =
                    {
                        Color = "0.25 0.25 0.25 0.45"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5",
                        OffsetMin = "-50 -275", OffsetMax = "50 -260"
                    }
                }, "Hud", "ProgressBar");
                if (playerTarget != null)
                {
                    cont.Add(new CuiElement()
                    {
                        Parent = "ProgressBar",
                        Components =
                        {
                            new CuiTextComponent()
                            {
                                Text = $"ТЕЛЕПОРТАЦИЯ {playerTarget.displayName.ToUpper()}", Align = TextAnchor.MiddleCenter, FontSize = 9,
                                Font = "robotocondensed-regular.ttf"
                            },
                            new CuiRectTransformComponent()
                            {
                                AnchorMin = "0 0", AnchorMax = $"1 1"
                            }
                        }
                    });  
                }
                else
                {
                    cont.Add(new CuiElement()
                    {
                        Parent = "ProgressBar",
                        Components =
                        {
                            new CuiTextComponent()
                            {
                                Text = $"ТЕЛЕПОРТАЦИЯ", Align = TextAnchor.MiddleCenter, FontSize = 9,
                                Font = "robotocondensed-regular.ttf"
                            },
                            new CuiRectTransformComponent()
                            {
                                AnchorMin = "0 0", AnchorMax = $"1 1"
                            }
                        }
                    }); 
                }

                CuiHelper.AddUi(player, cont);
            }
            private void DrawUi(int time, int max)
            { 
                CuiHelper.DestroyUi(player, "Progress");
                CuiHelper.DestroyUi(player, "time");
                var cont = new CuiElementContainer();
                cont.Add(new CuiElement()
                {
                    FadeOut = 0.05f,
                    Parent = "ProgressBar",
                    Name = "Progress",
                    Components =
                    {
                        new CuiImageComponent()
                        {
                            Color = color
                        },
                        new CuiRectTransformComponent()
                        {
                            AnchorMin = "0 0", AnchorMax = $"{Math.Min(0.05 + ((float) time / max), 0.99)} 0.99"
                        }
                    }
                });
                cont.Add(new CuiElement()
                {
                    Parent = "ProgressBar",
                    Name = "time",
                    Components =
                    {
                        new CuiTextComponent()
                        {
                            Text = $"{time}%", Align = TextAnchor.MiddleCenter, FontSize = 8,
                            Font = "robotocondensed-regular.ttf"
                        },
                        new CuiRectTransformComponent()
                        {
                            AnchorMin = "0 1", AnchorMax = $"1 2"
                        }
                    }
                });
                CuiHelper.AddUi(player, cont);
            }

            private void DrawUiCD(int time, int max)
            {
                
                CuiHelper.DestroyUi(player, "Progress");
                CuiHelper.DestroyUi(player, "time");
                var cont = new CuiElementContainer();
                cont.Add(new CuiElement()
                {
                    FadeOut = 0.05f,
                    Parent = "ProgressBar",
                    Name = "Progress",
                    Components =
                    {
                        new CuiImageComponent()
                        {
                            Color = color
                        },
                        new CuiRectTransformComponent()
                        {
                            AnchorMin = "0 0", AnchorMax = $"{Math.Min(0.05 + ((float) time / max), 0.99)} 0.99"
                        }
                    }
                });
                cont.Add(new CuiElement()
                {
                    Parent = "ProgressBar",
                    Name = "time",
                    Components =
                    {
                        new CuiTextComponent()
                        {
                            Text = $"{time} сек", Align = TextAnchor.MiddleCenter, FontSize = 8,
                            Font = "robotocondensed-regular.ttf"
                        },
                        new CuiRectTransformComponent()
                        {
                            AnchorMin = "0 1", AnchorMax = $"1 2"
                        }
                    } 
                });
                CuiHelper.AddUi(player, cont);
            }

            bool CheckPlayer(BasePlayer basePlayer)
            {
                if (basePlayer.GetMounted() != null)
                {
                    if (basePlayer.GetMounted().ShortPrefabName == "minihelipassenger" && ins.cfg.blockcopter || basePlayer.GetMounted().ShortPrefabName == "saddletest" && ins.cfg.blockhorse|| basePlayer.GetMounted().ShortPrefabName == "transporthelicopilot" && ins.cfg.blockcopter)
                        return false;
                } 
                
                if (basePlayer.GetComponentInParent<ScrapTransportHelicopter>() && ins.cfg.blockcopter)
                    return false;
                if (basePlayer.GetComponentInParent<CargoShip>() && ins.cfg.blockcargo)
                    return false;
                if (basePlayer.GetComponentInParent<HotAirBalloon>() && ins.cfg.blockhot)
                    return false;
                if (basePlayer.IsBuildingBlocked() && ins.cfg.blockbuild)
                    return false;
                if (basePlayer.metabolism.temperature.value <=  -ins.cfg.cold && ins.cfg.blockсcold)
                    return false;
                if (basePlayer.metabolism.heartrate.value >=  ins.cfg.temp && ins.cfg.blocktemp)
                    return false;
                if (basePlayer.metabolism.radiation_poison.value >=  ins.cfg.rad && ins.cfg.blockrad)
                    return false;
                if (basePlayer.metabolism.bleeding.value >= ins.cfg.blood && ins.cfg.blockblood)
                    return false;
                if (basePlayer.IsSwimming() && ins.cfg.blockswim)
                    return false;
                if (basePlayer.IsWounded())
                    return false;
                return true;
            }
            int GetTprCD(ulong uid)
            {
                int min = 300;
                foreach (var privilege in ins.cfg.permTPRCD)
                {
                    if (ins.permission.UserHasPermission(uid.ToString(), privilege.Key))
                        min = Mathf.Min(min, privilege.Value);
                }
                return min;
            }
            int GetTPRTime(ulong uid)
            {
                int max = 1;
                foreach (var privilege in ins.cfg.permTPRTIME) if (ins.permission.UserHasPermission(uid.ToString(), privilege.Key)) max = Mathf.Max(max, privilege.Value);
                return max;
            }
            private void Update()
            {
                if (player.GetActiveItem() == null)
                {
                    OnDestroy();
                    return;
                } 
                if (player.GetActiveItem().info.shortname != "rf.detonator")
                {
                    OnDestroy();
                    return;
                }
                if(ins._pagerList.ContainsValue(racia.frequency))
                {
                    var getPlayer = ins._pagerList.First(p => p.Value == racia.frequency).Key;
                    playerTarget = BasePlayer.FindByID(getPlayer);
                } 
                else
                {
                    playerTarget = null;
                }

                Homes f;
                InputState input = player.serverInput;

                if (text == "start")
                { 
                    if (!CheckPlayer(player))
                    { 
                        if (procent >= 100) procent = 0;
                        procent += 0.02;
                        color = ins.HexToRustFormat("#ff665e8A");
                        DrawUi((int) procent, 100); 
                    }
                    else if (homes.TryGetValue(racia.frequency, out f))
                    {
                        if (playerData.IsHome() > 0)
                        {
                            color = ins.HexToRustFormat("#ff665e8A");
                            procent += 0.02;
                            DrawUiCD((int) playerData.IsHome(), 1);
                        }
                        else
                        { 
                            color = ins.HexToRustFormat("#2df79b8A");
                            if(f.ShortName == "sleepingbag_leather_deployed") procent += ins.cfg.sleepbeg * 0.02;
                            else procent += ins.cfg.bad * 0.02;
                            DrawUi((int) procent, 100);
                            if (procent >= 100)
                            {
                        
                                procent = 0;
                                RaycastHit hit;

                                if (!Physics.Linecast(f.position, f.position + Vector3.down, out hit))
                                {
                                    ins.ReplySend(player, "Сетхом разуршен.");
                                    playerData._homeList.Remove(racia.frequency);
                                    return;
                                }
                                if (hit.GetEntity() == null)
                                {
                                    ins.ReplySend(player, "Сетхом разуршен.");
                                    playerData._homeList.Remove(racia.frequency);
                                    return;
                                }
                                else if (hit.GetEntity().net.ID == f.netId)
                                {
                                    if (!CheckPlayer(player))
                                        return;
                                    Teleport(player, f.position);
                                    if(f.ShortName == "bed_deployed") playerData.HOMECD = CurrentTime() + ins.cfg.badcd;
                                    else playerData.HOMECD = CurrentTime() + ins.cfg.sleepcd;
                                    OnDestroy();
                                    return;
                                }
                                else
                                {
                                    ins.ReplySend(player, "Сетхом разуршен.");
                                    playerData._homeList.Remove(racia.frequency);
                                    
                                    return;
                                }
                            } 
                        }
                    }
                    else if (ins._pagerList.ContainsValue(racia.frequency))
                    {
                        if(playerTarget == null) return;
                        if (playerData.IsTPR() > 0)
                        {
                            color = ins.HexToRustFormat("#ff665e8A");
                            DrawUiCD((int) playerData.IsTPR(), 1);
                        }
                        else if (procent >= 100) 
                        {
                            Teleport(player, playerTarget.transform.position);
                            playerData.TPRCD = CurrentTime() + GetTprCD(player.userID);
                            procent = 0;
                            ins.ReplySend(playerTarget, $"К вам успешно телепортировался <color=#2df79b>{player.displayName}</color>");
                            ins.ReplySend(player, $"Вы успешно телепортировались к <color=#2df79b>{playerTarget.displayName}</color>");
                            OnDestroy();
                        } 
                        else
                        {  
                            if(!CheckPlayer(playerTarget))
                            {
                                if (procent >= 100) procent = 0;
                                procent += 0.1 * tprtime;
                                color = ins.HexToRustFormat("#ff665e8A");
                                DrawUi((int) procent, 100);
                                return;
                            }
                            procent += 0.1 * tprtime;
                            color = ins.HexToRustFormat("#2df79b8A");
                            DrawUi((int) procent, 100);
                        }
                    } 
                    else if(!homes.TryGetValue(racia.frequency, out f))
                    {
                        if (procent >= 100) procent = 0;
                        procent += 0.1;
                        color = ins.HexToRustFormat("#ff665e8A");
                        DrawUi((int) procent, 100); 
                    }

                }
                if (input.WasDown(BUTTON.FIRE_PRIMARY) && text == "stop")
                {
                    StartUI();
                    text = "start";
                }
                if (!input.IsDown(BUTTON.FIRE_PRIMARY) && text == "start")
                {
                    text = "stop";
                    procent = 0;
                    CuiHelper.DestroyUi(player, "ProgressBar");
                }
            }

            private void Teleport(BasePlayer player, Vector3 pos)
            {
                if (player == null || !player.IsConnected || player.IsDead()) return;
                player.StartSleeping();
                player.MovePosition(pos);
                player.UpdateNetworkGroup();
                player.SendNetworkUpdateImmediate(false);
                if (player.net?.connection != null) player.ClientRPCPlayer(null, player, "ForcePositionTo", pos);
                if (player.net?.connection != null)
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
                if (player.net?.connection == null) return;
                try
                {
                    player.ClearEntityQueue(null);
                }
                catch
                {
                    // ignored
                }

                player.SendFullSnapshot();
                player.SetParent(null, true, true);
                player.SendNetworkUpdate();
            }
        }

        #region Commands

        [ChatCommand("sethome")] 
        void SetHome(BasePlayer player)
        {
            if(!cfg.hometeleport) return;
            RaycastHit hit;
            PlayerData playerData;
            if(!player.IsBuildingAuthed())
            {
                ReplySend(player, "Запрещенная территория для установки дома!");
                return;
            }
            if (!Physics.Linecast(player.transform.position, player.transform.position + Vector3.down, out hit))
                ReplySend(player, "Встаньте на кровать или спальник!");
            else if (hit.GetEntity() == null)
                ReplySend(player, "Встаньте на кровать или спальник!");
            else if (hit.GetEntity().ShortPrefabName != "sleepingbag_leather_deployed" && hit.GetEntity().ShortPrefabName !="bed_deployed")
                ReplySend(player, "Встаньте на кровать или спальник!");
            else if(hit.GetEntity().OwnerID != player.userID)
                ReplySend(player, "Это не ваш спальник или кровать!");
            else 
            if(_homeList.TryGetValue(player.userID, out playerData))
            {
                if (playerData._homeList.Count >= 1 && playerData._homeList.First(p => p.Value.netId == hit.GetEntity().net.ID).Value != null) return;
                if (GetHomeLimit(player.userID) > playerData._homeList.Count)
                    HomeGenerate(hit.GetEntity().net.ID, player);
                else ReplySend(player, "У вас максимальное кол-во домов.");
            }
            else
            {
                HomeGenerate(hit.GetEntity().net.ID, player);
            }
        }
        private void Teleport(BasePlayer player, Vector3 pos)
        {
            if (player == null || !player.IsConnected || player.IsDead()) return;
            player.StartSleeping();
            player.MovePosition(pos);
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate(false);
            if (player.net?.connection != null) player.ClientRPCPlayer(null, player, "ForcePositionTo", pos);
            if (player.net?.connection != null)
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            if (player.net?.connection == null) return;
            try
            {
                player.ClearEntityQueue(null);
            }
            catch
            {
            }

            player.SendFullSnapshot();
            player.SetParent(null, true, true);
            player.SendNetworkUpdate();
        }
        [ChatCommand("tp")]
        void AdminTeleport(BasePlayer p, string c, string[] a)
        {
            if(!permission.UserHasPermission(p.UserIDString, "rfteleports.admin")) return;
            
            var targetPlayer = BasePlayer.Find(string.Join(" ", a.ToArray()));
            if (targetPlayer == null) 
            {
                ReplySend(p, "Игрок не найден"); 
                return;
            }
            Teleport(p, targetPlayer.transform.position + Vector3.up);
        }
        [ChatCommand("tpr")]
        void Tpr(BasePlayer p, string c, string[] a)
        {
            ReplySend(p, "Прочитайте о нашей системе телепортации в информации о сервере.");
        }
        [ChatCommand("tpa")]
        void Tpa(BasePlayer p, string c, string[] a)
        {
            ReplySend(p, "Прочитайте о нашей системе телепортации в информации о сервере.");
        }
        [ChatCommand("tpc")]
        void Tpc(BasePlayer p, string c, string[] a)
        {
            ReplySend(p, "Прочитайте о нашей системе телепортации в информации о сервере.");
        }
        [ChatCommand("home")]
        void Home(BasePlayer p, string c, string[] a)
        {
            if(!cfg.hometeleport) return;
            PlayerData playerData;
            Homes home;
            if(a.Length < 1) return;
            switch (a[0])
            {
                case "list":
                    if(!_homeList.TryGetValue(p.userID, out playerData)) return;
                    var text = "Список Домов:\n";
                    foreach (var HomeList in playerData._homeList)
                        text += $"Номер: <color=#E10394>{HomeList.Key}</color> Квадрат: <color=#00ffcc>{getGrid(HomeList.Value.position)}</color>\n";
                    ReplySend(p, text);
                    PrintToConsole(p, text);
                    break;
                case "remove":
                    if(!_homeList.TryGetValue(p.userID, out playerData)) return;
                    int num;
                    if (a.Length < 2) 
                    {
                        ReplySend(p, "Введите номер дома. Посмотреть можно здесь /home list");
                        PrintToConsole(p, "Введите номер дома. Посмотреть можно здесь /home list");
                        return;
                    }
                    if (!int.TryParse(a[1], out num))
                    {
                        ReplySend(p, "Введите номер дома. Посмотреть можно здесь /home list");
                        PrintToConsole(p, "Введите номер дома. Посмотреть можно здесь /home list");
                        return;
                    }

                    if(playerData._homeList.TryGetValue(num, out home))
                    {
                        playerData._homeList.Remove(num);
                        ReplySend(p, $"Дом {num} удален!");
                        PrintToConsole(p, $"Дом {num} удален!");
                    }
                    else
                    {
                        ReplySend(p, $"Дом {num} не найден!");
                        PrintToConsole(p, $"Дом {num} не найден!"); 
                    }
                    break;
            }
        }
        #endregion
        #region Help
        string getGrid(Vector3 pos) {
            char letter = 'A';
            var x = Mathf.Floor((pos.x+(ConVar.Server.worldsize/2)) / 146.3f)%26;
            var z = (Mathf.Floor(ConVar.Server.worldsize/146.3f)-1)-Mathf.Floor((pos.z+(ConVar.Server.worldsize/2)) / 146.3f);
            letter = (char)(((int)letter)+x);
            return $"{letter}{z}";
        }
        //35969400
        private void ReplySend(BasePlayer player, string message) => player.SendConsoleCommand("chat.add 0",
            new object[2]
                {76561199015371818, $"<size=18><color=purple>Телепортация</color></size>\n{message}"});

        private static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);

        private string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex)) hex = "#FFFFFFFF";
            var str = hex.Trim('#');
            if (str.Length == 6) str += "FF";
            if (str.Length != 8)
            {
                throw new Exception(hex);
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);
            Color color = new Color32(r, g, b, a);
            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }

        private static double CurrentTime() => DateTime.UtcNow.Subtract(epoch).TotalSeconds;

        #endregion
    }
}