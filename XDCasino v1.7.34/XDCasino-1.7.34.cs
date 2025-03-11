using Newtonsoft.Json;
using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using VLB;
using Rust;
using System.Text;

namespace Oxide.Plugins
{
    [Info("XDCasino", "rustmods.ru", "1.7.34")]
    [Description("Casino, play for resources :3")]
    public class XDCasino : RustPlugin
    {
        #region Var
        private const string TerminalLayer = "UI_TerminalLayer";
        private const string NotiferLayer = "UI_NotiferLayers";
        private readonly ulong Id = 23556438051;
        private const string FileName = "CasinoRoomNew1";
        List<BaseEntity> CasinoEnt = new List<BaseEntity>();
        BigWheelGame bigWheelGame;
        BaseEntity WoodenTrigger;
        MonumentInfo monument;
        [PluginReference] Plugin CopyPaste;
        #endregion

        #region Configuration
        private Configuration config;
        private class Configuration
        {
            [JsonProperty("Запретить вставать игроку со стула если он учавствует в ставке | Forbid a player to get up from a chair if he participates in a bet")]
            public bool mountUse = true;
            [JsonProperty("Нужен ли шкаф в постройке ? | Do you need a closet in the building?")]
            public bool cupboardSpawn = false;
            [JsonProperty("Включить ли радио в здании ? | Should I turn on the radio in the building ?")]
            public bool useRadio = true;
            [JsonProperty("Включите этот параметр если у вас гниет постройка | Enable this option if your building is rotting")]
            public Boolean useDecay = false;
            [JsonProperty("Ссылка на радио станцию которая будет играть в доме | Link to the radio station that will play in the house")]
            public String RadioStation = "http://radio.skyplugins.ru:8030/casino.mp3";
            [JsonProperty("идентификатор для подключения к камере | ID to connect to the camera")]
            public string identifier = "casino";
            [JsonProperty("Список предметов для ставок (ShortName/максимальное количество за 1 ставку) | List of items for bets (ShortName / maximum quantity for 1 bet)")]
            public Dictionary<string, int> casinoItems = new Dictionary<string, int>();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                    throw new Exception();
                SaveConfig();
            }
            catch
            {
                for (var i = 0; i < 3; i++)
                    PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                LoadDefaultConfig();
            }
            ValidateConfig();
            SaveConfig();
        }

        private void ValidateConfig()
        {
            if (config.casinoItems.Count == 0)
            {
                config.casinoItems = new Dictionary<string, int>
                {
                    ["cloth"] = 100,
                    ["metal.refined"] = 10,
                    ["lowgradefuel"] = 50,
                    ["wood"] = 1000,
                    ["stones"] = 1000,
                    ["metal.fragments"] = 300,
                };
            }
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
        }
        #endregion

        #region Lang
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CASINO_PRIZE"] = "Collect your winnings first!",
                ["CASINO_NOTGAMEITEM"] = "This item is not on the approved list.",
                ["CASINO_ITEMAMOUNTFULL"] = "You are trying to put more than the allowed amount (Maximum for this item {0})",
                ["CASINO_MOUNTNOT"] = "You cannot get up during an active bet! Also, don't forget to take your prize",
                ["CASINO_ERROR"] = "Something went wrong. Move the item to another slot and try again!",
                ["CASINO_UITITLE"] = "<b>Roulette for resources</b>",
                ["CASINO_KEYAUTH"] = "The plugin did not pass the authentication on the server!\nCheck the plugin version or contact the developer\nSkuliDropek#1480\nvk.com/dezlife",
                ["CASINO_UITITLEITEM"] = "<b><color=#EAD093FF>ALLOWED ITEMS AND RESTRICTIONS</color></b>\n",
                ["CASINO_UIRULES"] = "<b><color=#EAD093FF>REGULATIONS</color></b>\n" +
                "1. You will not be able to place a new bet without collecting your winnings.\n" +
                "2. You can only use certain resources for betting,\nthere is also a limit on the maximum rate.",
                ["CASINO_UIRULES3"] = "\n3. You cannot get up from your chair during a bet or if you have not collected your winnings",
                ["CASINO_NOT_COPYPASTE"] = "Check if you have installed the CopyPaste plugin",
                ["CASINO_V_COPYPASTE"] = "You have an old version of CopyPaste!\nPlease update the plugin to the latest version (4.1.27 or higher) - https://umod.org/plugins/copy-paste",
                ["CASINO_NOT_OUTPOST"] = "You do not have the 'OUTPOST' on the campaign !\nPlease contact the developer\nSkuliDropek#1480\nvk.com/dezlife",
                ["CASINO_NOT_SPAWN_BUILDING"] = "Error #1 \nPlugin won't work, Contact the developer\nSkuliDropek#1480\nvk.com/dezlife",
                ["CASINO_PASTE_SUCC"] = "Construction processed successfully {0}",
                ["CASINO_BUILDING_SETUP_ERROR"] = "Error loading building! Details in the log file!!\nContact the developer\nSkuliDropek#1480\nvk.com/dezlife",
                ["CASINO_ServerNotResponse"] = "Unable to load the file {0}, Server response: {1}. Retrying to download...",
                ["CASINO_FileNotLoad"] = "Downloading the file {0} was unsuccessful. Contact the developer\nSkuliDropek#1480\nvk.com/dezlife",
                ["CASINO_BuildingLoad"] = "Initializing building for casino...",
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CASINO_PRIZE"] = "Сначала забери выигрыш!",
                ["CASINO_NOTGAMEITEM"] = "Этого предмета нет в списке разрешенных",
                ["CASINO_ITEMAMOUNTFULL"] = "Вы пытаетесь положить больше чем разрешено (Максимум для этого предмета {0})",
                ["CASINO_MOUNTNOT"] = "Нельзя вставать во время активной ставки! Так же не забудьте забрать приз",
                ["CASINO_ERROR"] = "Что то пошло не так. Перенесите предмет в другой слот и попробуйте еще раз!",
                ["CASINO_UITITLE"] = "<b>Рулетка на ресурсы</b>",
                ["CASINO_KEYAUTH"] = "Плагин не смог пройти аунтефикацию на сервере!\n Сверьте версию плагина или свяжитесь с разработчиком\nSkuliDropek#1480\nvk.com/dezlife",
                ["CASINO_UITITLEITEM"] = "<b><color=#EAD093FF>РАЗРЕШЕННЫЕ ПРЕДМЕТЫ И ОГРАНИЧЕНИЯ</color></b>\n",
                ["CASINO_UIRULES"] = "<b><color=#EAD093FF>ПРАВИЛА</color></b>\n" +
                "1. Вы не сможете сделать новую ставку не забрав выигрыш.\n" +
                "2. Для ставок вы сможете использовать только определенные ресурсы,\nтак же существует ограничения на максимальную ставку.",
                ["CASINO_UIRULES3"] = "\n3. Вы не можете вставать со стула во время ставки или если вы не забрали свой выигрыш",
                ["CASINO_NOT_COPYPASTE"] = "Проверьте установлен ли у вас плагин 'CopyPaste'",
                ["CASINO_V_COPYPASTE"] = "У вас старая версия CopyPaste!\nПожалуйста обновите плагин до последней версии (4.1.27 или выше) - https://umod.org/plugins/copy-paste",
                ["CASINO_NOT_OUTPOST"] = "Походу у вас отсутствует 'Город НПС' !\nПожалуйста обратитесь к разработчику\nSkuliDropek#1480\nvk.com/dezlife",
                ["CASINO_NOT_SPAWN_BUILDING"] = "Ошибка #1 \nПлагин не будет работать, Обратитесь к разработчику\nSkuliDropek#1480\nvk.com/dezlife",
                ["CASINO_PASTE_SUCC"] = "Постройка обработана успешно {0}",
                ["CASINO_BUILDING_SETUP_ERROR"] = "Ошибка при загрузке постройки! Подробности в лог файле!!\nОбратитесь к разработчику\nSkuliDropek#1480\nvk.com/dezlife",
                ["CASINO_ServerNotResponse"] = "Невозможно загрузить файл {0}, Ответ сервера: {1}. Пробуем повторить загрузку...",
                ["CASINO_FileNotLoad"] = "Повторная загрузка файла {0}, не увенчалась успехом. Обратитесь к разработчику\nSkuliDropek#1480\nvk.com/dezlife",
                ["CASINO_BuildingLoad"] = "Инициализация постройки для казино...",



            }, this, "ru");
        }

        #endregion

        #region Hooks
        void Init()
        {
            Unsubscribe("CanDismountEntity");
            Unsubscribe("OnEntityTakeDamage");
        }
        private void OnServerInitialized()
        {
            monument = TerrainMeta.Path.Monuments.FirstOrDefault(p => p.name.ToLower() == "assets/bundled/prefabs/autospawn/monument/medium/compound.prefab");
            if (!CopyPaste)
            {
                PrintError(GetLang("CASINO_NOT_COPYPASTE"));
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }
            else if (CopyPaste.Version < new VersionNumber(4, 1, 27))
            {
                PrintError(GetLang("CASINO_V_COPYPASTE"));
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }
            if (monument == null)
            {
                PrintError(GetLang("CASINO_NOT_OUTPOST"));
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }
            LoadDataCopyPaste();
           
            if (config.mountUse)
                Subscribe("CanDismountEntity");
            if (config.useDecay)
                Subscribe(nameof(OnEntityTakeDamage));
        }

        void OnEntityTakeDamage(BaseCombatEntity victim, HitInfo info)
        {
            if (info.damageTypes.Has(DamageType.Decay))
            {
                if (victim?.OwnerID == Id)
                {
                    info.damageTypes.Scale(DamageType.Decay, 0);
                }
            }
        }
        ItemContainer.CanAcceptResult? CanAcceptItem(ItemContainer container, Item item, int targetPos)
        {
            if (container?.entityOwner is BigWheelBettingTerminal && container?.entityOwner?.OwnerID == Id && !container.IsLocked())
            {
                BasePlayer player = container.playerOwner;          
                if (player == null)
                    return ItemContainer.CanAcceptResult.CannotAccept;

                if (targetPos == 5)
                    return ItemContainer.CanAcceptResult.CannotAccept;
                
                if (!config.casinoItems.ContainsKey(item.info.shortname))
                {
                    HelpUiNottice(player, GetLang("CASINO_NOTGAMEITEM",player.UserIDString));
                    return ItemContainer.CanAcceptResult.CannotAccept;
                }
                int maxAmount = config.casinoItems[item.info.shortname];
                if (container.GetSlot(5) != null)
                {
                    HelpUiNottice(player, GetLang("CASINO_PRIZE", player.UserIDString));
                    return ItemContainer.CanAcceptResult.CannotAccept;
                }
                int s = 0;
                for (int i = 0; i < 5; i++)
                {
                    Item slot = container.GetSlot(i);
                    if (slot != null)
                    {
                        if (slot.info.shortname == item.info.shortname)
                            s += slot.amount;
                    }
                }
                if (item.GetRootContainer()?.entityOwner?.OwnerID == Id)
                {
                    if (item.GetRootContainer()?.GetSlot(5) != null || targetPos == 5)
                        return ItemContainer.CanAcceptResult.CannotAccept;
                }
                else if (item.amount + s > maxAmount)
                {
                    HelpUiNottice(player, GetLang("CASINO_ITEMAMOUNTFULL", player.UserIDString, maxAmount));
                    return ItemContainer.CanAcceptResult.CannotAccept;
                }
            }
            return null;
        }

        object CanDismountEntity(BasePlayer player, BaseMountable entity)
        {
            if (entity?.OwnerID == Id)
            {
                foreach (var item in bigWheelGame?.terminals?.Where(x => x.skinID == player.userID))
                {
                    for (int i = 0; i < 6; i++)
                    {
                        var s = item.inventory.GetSlot(i);
                        if (s != null)
                        {
                            HelpUiNottice(player, GetLang("CASINO_MOUNTNOT", player.UserIDString));
                            return false;
                        }
                    }
                    item.skinID = 0;
                }
            }
            return null;
        }
        void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (entity is BigWheelBettingTerminal && entity.OwnerID == Id)
            {
                if (player == null)
                    return;
                var sss = entity as BigWheelBettingTerminal;
                sss.GetComponent<StorageContainer>().inventory.playerOwner = player;
                entity.skinID = player.userID;

                CuiHelper.DestroyUi(player, TerminalLayer);
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "192 475", OffsetMax = "573 660" },
                    Image = { Color = "0.968627453 0.921631568632 0.882352948 0.03529412", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                }, "Overlay", TerminalLayer);

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.8090092", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Text = { Text = GetLang("CASINO_UITITLE", player.UserIDString), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 16, Color = HexToRustFormat("#d6ccc3"), }
                }, TerminalLayer);

                string rules = GetLang("CASINO_UIRULES", player.UserIDString);
                if (config.mountUse)
                    rules += GetLang("CASINO_UIRULES3", player.UserIDString);
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.01924771 0.3405407", AnchorMax = "0.9833772 0.8018017", OffsetMax = "0 0" },
                    Text = { Text = rules, Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf", FontSize = 12, Color = HexToRustFormat("#FFFFFFFF"), }
                }, TerminalLayer);

                string itemRules = GetLang("CASINO_UITITLEITEM", player.UserIDString);
                int i = 0;
                foreach (var cfg in config.casinoItems)
                {
                    i++;
                    string Zapitaya = i == config.casinoItems.Count ? "" : ",";
                    itemRules += ItemManager.itemList.First(x => x.shortname == cfg.Key).displayName.english + $":{cfg.Value}{Zapitaya} ";
                }

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.01924774 0.01081081", AnchorMax = "0.9833772 0.3369368", OffsetMax = "0 0" },
                    Text = { Text = itemRules, Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf", FontSize = 12, Color = HexToRustFormat("#FFFFFFFF"), }
                }, TerminalLayer);

                CuiHelper.AddUi(player, container);
            }
        }
        private void OnLootEntityEnd(BasePlayer player, BaseCombatEntity entity) 
        {
            CuiHelper.DestroyUi(player, TerminalLayer); 
            CuiHelper.DestroyUi(player, NotiferLayer);
            CuiHelper.DestroyUi(player, "NotiferLayer2");
            CuiHelper.DestroyUi(player, "NotiferLayer1");
        }
        void Unload()
        {
            foreach (BasePlayer player in BasePlayer.allPlayerList)
            {
                CuiHelper.DestroyUi(player, TerminalLayer);
                CuiHelper.DestroyUi(player, NotiferLayer);        
            }
            foreach (BaseEntity ent in CasinoEnt)
            {
                if (ent == null || ent.IsDestroyed)
                    continue;
                ent.Kill();
            }
        }

        #endregion

        #region UiNotifer
        private void HelpUiNottice(BasePlayer player, string msg, string sprite = "assets/icons/info.png")
        {
            CuiHelper.DestroyUi(player, NotiferLayer);
            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                FadeOut = 0.30f,
                RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "192 664", OffsetMax = "573 710" },
                Image = { Color = "0.968627453 0.921631568632 0.882352948 0.035294121455", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat", FadeIn = 0.40f },
            }, "Overlay", NotiferLayer);
           
            container.Add(new CuiElement
            {
                Parent = NotiferLayer,
                Name = "NotiferLayer1",
                FadeOut = 0.30f,
                Components =
                {
                    new CuiImageComponent {Sprite = sprite, Color = HexToRustFormat("#AA7575FF"), FadeIn = 0.45f },
                    new CuiRectTransformComponent{ AnchorMin = "0.02672293 0.192029", AnchorMax = "0.09671418 0.7717391"},
                }
            });

            container.Add(new CuiLabel
            {
                FadeOut = 0.30f,
                RectTransform = { AnchorMin = "0.1139241 0.08999", AnchorMax = "0.9423349 0.89999", OffsetMax = "0 0" },
                Text = { Text = msg, Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf", FontSize = 14, Color = HexToRustFormat("#d6ccc3"), FadeIn = 0.50f }
            }, NotiferLayer, "NotiferLayer2");

            CuiHelper.AddUi(player, container);
            timer.Once(3.5f, () => 
            { 
                CuiHelper.DestroyUi(player, NotiferLayer); 
                CuiHelper.DestroyUi(player, "NotiferLayer1"); 
                CuiHelper.DestroyUi(player, "NotiferLayer2"); 
            });
        }
        #endregion

        #region Metods
        void GenerateBuilding()
        {
            var options = new List<string> { "stability", "true", "deployables", "true", "autoheight", "false", "entityowner", "false" };

            Vector3 resultVector = GetResultVector();

            WoodenTrigger = GameManager.server.CreateEntity("assets/prefabs/deployable/woodenbox/woodbox_deployed.prefab", new Vector3(resultVector.x - 2.3f, resultVector.y + 6.0f, resultVector.z - 2.1f));
            WoodenTrigger.OwnerID = Id;
            WoodenTrigger.Spawn();
            UnityEngine.Object.Destroy(WoodenTrigger.GetComponent<DestroyOnGroundMissing>());
            UnityEngine.Object.Destroy(WoodenTrigger.GetComponent<GroundWatch>());
            CasinoEnt.Add(WoodenTrigger);

            var success = CopyPaste.Call("TryPasteFromVector3", resultVector, (monument.transform.rotation.eulerAngles * Mathf.Deg2Rad).y - 1.69f, FileName, options.ToArray());

            if (success is string)
            {
                PrintWarning(GetLang("CASINO_NOT_SPAWN_BUILDING"));
                return;
            }  
        }
        void PreSpawnEnt()
        {
            List<BaseEntity> obj = new List<BaseEntity>();
            Vis.Entities(GetResultVector(), 25f, obj, LayerMask.GetMask("Construction", "Deployable", "Deployed", "Debris"));
            foreach (BaseEntity item in obj?.Where(x => x.OwnerID == Id))
            {
                if (item == null) continue;
                item.Kill();
            }
           NextTick(() => { GenerateBuilding(); });
        }

        void OnPasteFinished(List<BaseEntity> pastedEntities, string fileName)
        {
            try
            {
                if (fileName != FileName)
                    return;
                CasinoEnt.AddRange(pastedEntities);
                foreach (BaseEntity item in CasinoEnt)
                {
                    if (item == null) continue;
                    item.OwnerID = Id;
                    if (item is CardTable)
                        continue;
                    if (item.prefabID == 1560881570 && item != WoodenTrigger)
                    {
                        SlotMachine slotM = GameManager.server.CreateEntity("assets/prefabs/misc/casino/slotmachine/slotmachine.prefab") as SlotMachine;
                        slotM.transform.position = item.transform.position;
                        slotM.transform.rotation = item.transform.rotation;
                        slotM.Spawn();
                        NextTick(() => { CasinoEnt.Add(slotM); CasinoEnt.Remove(item); item?.Kill(); });
                        continue;
                    }
                    if (item is BaseChair || item is BigWheelBettingTerminal || item is RepairBench || item is BaseArcadeMachine)
                    {
                        var ent = item as BaseCombatEntity;
                        if (ent == null) continue;
                        ent.pickup.enabled = false;
                        continue;
                    }
                    if (item is DeployableBoomBox)
                    {
                        if (config.useRadio)
                        {
                            NextTick(() => {
                                var boomBox = item as DeployableBoomBox;
                                boomBox.BoxController.CurrentRadioIp = config.RadioStation;
                                boomBox.BoxController.ConditionLossRate = 0;
                                boomBox.BoxController.baseEntity.ClientRPC(null, "OnRadioIPChanged", boomBox.BoxController.CurrentRadioIp);
                                if (!boomBox.BoxController.IsOn())
                                {
                                    boomBox.BoxController.ServerTogglePlay(true);
                                }
                                boomBox.BoxController.baseEntity.SendNetworkUpdate(global::BasePlayer.NetworkQueue.Update);
                            });
                        }
                    }
                    if (item.prefabID == 2476970476)
                    {
                        if (!config.cupboardSpawn)
                        {
                            item?.AdminKill();
                            continue;
                        }
                    }
                    if (item is DecayEntity)
                    {
                        var decayEntety = item as DecayEntity;
                        decayEntety.decay = null;
                        decayEntety.decayVariance = 0;
                        decayEntety.ResetUpkeepTime();
                        decayEntety.DecayTouch();
                    }
                    if (item is CCTV_RC)
                    {
                        var ent = item as CCTV_RC;
                        if (ent == null)
                            continue;
                        ent.UpdateIdentifier(config.identifier);
                        ent.pickup.enabled = false;
                        ent?.SetFlag(BaseEntity.Flags.Reserved8, true);
                        continue;
                    }
                    if (item is ElectricGenerator)
                    {
                        WoodenTrigger.transform.position = item.transform.position;
                        WoodenTrigger.transform.rotation = item.transform.rotation;
                        WoodenTrigger.SendNetworkUpdate();
                    }
                    if (item is Door)
                    {
                        var ent = item as Door;
                        if (ent == null)
                            continue;
                        ent.pickup.enabled = false;
                        ent.canTakeLock = false;
                        ent.canTakeCloser = false;
                        continue;
                    }
                    if (item is BuildingBlock)
                    {
                        var build = item as BuildingBlock;
                        build?.SetFlag(BaseEntity.Flags.Reserved1, false);
                        build?.SetFlag(BaseEntity.Flags.Reserved2, false);
                    }
                    if (item as ElectricalHeater)
                    {
                        item?.SetFlag(BaseEntity.Flags.Reserved8, true);
                    }
                    if (item as HBHFSensor)
                    {
                        bigWheelGame = GameManager.server.CreateEntity("assets/prefabs/misc/casino/bigwheel/big_wheel.prefab") as BigWheelGame;
                        bigWheelGame.SetParent(item, false, true);
                        bigWheelGame.transform.position = item.transform.position;
                        bigWheelGame.transform.rotation = item.transform.rotation * Quaternion.Euler(0f, 270f, -90f);
                        bigWheelGame.gameObject.GetOrAddComponent<SphereCollider>();
                        bigWheelGame.Spawn();
                    }
                    else if (item.prefabID == 1392608348 || item.prefabID == 3887352222 || item.prefabID == 3953213470 || item is NeonSign)
                    {
                        item.enableSaving = true;
                        item?.SendNetworkUpdate();
                        item?.SetFlag(BaseEntity.Flags.Reserved8, true);
                        item?.SetFlag(BaseEntity.Flags.On, true);
                    }
                    item?.SetFlag(BaseEntity.Flags.Busy, true);
                    item?.SetFlag(BaseEntity.Flags.Locked, true);
                }
                PrintWarning(GetLang("CASINO_PASTE_SUCC", null, CasinoEnt.Count));
                NextTick(() =>
                {
                    CheckEnt();
                });
            }
            catch (Exception ex)
            {
                PrintError(GetLang("CASINO_BUILDING_SETUP_ERROR"));
                Log($"exception={ex}", "LogError");
            }
        }

        private void CheckEnt()
        {
            foreach (var item in CasinoEnt)
            {
                if (item.PrefabName == "assets/prefabs/misc/xmas/pookie/pookie_deployed.prefab")
                {
                    BigWheelBettingTerminal bettingTerminal = GameManager.server.CreateEntity("assets/prefabs/misc/casino/bigwheel/bigwheelbettingterminal.prefab") as BigWheelBettingTerminal;
                    bettingTerminal.allowedItem = null;
                    bettingTerminal.OwnerID = Id;
                    bettingTerminal.SetParent(WoodenTrigger, false, true);
                    bettingTerminal.transform.position = item.transform.position;
                    bettingTerminal.transform.rotation = item.transform.rotation;
                    bigWheelGame.terminals.Add(bettingTerminal);
                    bettingTerminal.Spawn();
                    bettingTerminal.SendNetworkUpdate();
                    item.Kill();
                }
            }
        }
        #endregion

        #region Data
        private void LoadDataCopyPaste(Boolean repeat = false)
        {
            if (!Interface.Oxide.DataFileSystem.ExistsDatafile("copypaste/" + FileName))
            {
                PrintError(GetLang("CASINO_BuildingLoad"));
                webrequest.Enqueue($"https://xdquest.skyplugins.ru/api/getbuilding/SDBfgs094siTPasF", null, (code, response) =>
                {
                    switch (code)
                    {
                        case 200:
                            {
                                PasteData obj = JsonConvert.DeserializeObject<PasteData>(response);
                                Interface.Oxide.DataFileSystem.WriteObject("copypaste/" + FileName, obj);
                                NextTick(() => {
                                    PreSpawnEnt();
                                });
                                break;
                            }
                        case 502:
                            {
                                PrintError(GetLang("CASINO_KEYAUTH"));
                                break;
                            }
                        default:
                            {
                                if (!repeat)
                                {
                                    PrintError(GetLang("CASINO_FileNotLoad", null, "CasinoRoomNew1.json"));
                                    Log(code.ToString(), "LogError");
                                    timer.Once(10f, () => LoadDataCopyPaste());
                                }
                                else
                                {
                                    PrintError(GetLang("CASINO_ServerNotResponse", null, "CasinoRoomNew1.json", code));
                                }
                                return;
                            }
                    }
                }, this, RequestMethod.GET);
            }
            else
            {
                PreSpawnEnt();
            }
        }  

        public class PasteData
        {
            public Dictionary<string, object> @default;
            public ICollection<Dictionary<string, object>> entities;
            public Dictionary<string, object> protocol;
        }

        #endregion

        #region Helps
        private static string HexToRustFormat(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }
        void Log(string msg, string file)
        {
            LogToFile(file, $"[{DateTime.Now}] {msg}", this);
        }
        private Vector3 GetResultVector()
        {
            return monument.transform.position + monument.transform.rotation * new Vector3(-30.62f, 1.87f, 20.95f);
        }
        public static StringBuilder sb = new StringBuilder();
        public string GetLang(string LangKey, string userID = null, params object[] args)
        {
            sb.Clear();
            if (args != null)
            {
                sb.AppendFormat(lang.GetMessage(LangKey, this, userID), args);
                return sb.ToString();
            }
            return lang.GetMessage(LangKey, this, userID);
        }
        #endregion
    }
}
