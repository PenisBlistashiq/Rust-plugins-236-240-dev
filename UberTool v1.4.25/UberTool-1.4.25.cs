using Facepunch;
using Network;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Oxide.Game.Rust.Libraries.Covalence;
using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using BTN = BUTTON;

///Скачано с дискорд сервера Rust Edit [PRO+]
///discord.gg/9vyTXsJyKR

namespace Oxide.Plugins
{
    [Info("UberTool", "discord.gg/9vyTXsJyKR", "1.4.25", ResourceId = 78)]
    [Description("The ultimative build'n'place solution without any borders or other known limits")]
    internal class UberTool : RustPlugin
    {
        [PluginReference]
        private Plugin Clans, ImageLibrary;
        private StrdDt playerPrefs = new StrdDt();

        private class StrdDt
        {
            public Dictionary<ulong,
            Plyrnf> playerData = new Dictionary<ulong,
            Plyrnf>();

            public StrdDt() { }
        }

        private class Plyrnf
        {
            public float SF;
            public int DBG;

            public Plyrnf() { }
        }

        private const string WIRE_EFFECT = "assets/prefabs/tools/wire/effects/plugeffect.prefab";

        private object CanUseWires(BasePlayer player)
        {
            EPlanner planner = player.GetComponent<EPlanner>();

            if (planner != null && planner.isWireTool)
            {
                return player.serverInput.IsDown(BTN.FIRE_SECONDARY);
            }

            return null;
        }

        public class EPlanner : MonoBehaviour
        {
            private BasePlayer player;
            private InputState serverInput;
            private uint ctvtm;
            private Construction.Target target;
            private Construction.Target mvTrgt;
            private BaseEntity mvTrgtSnp;
            private Construction construction;
            private Construction mvCnstrctn;
            private Construction rayDefinition;
            private Vector3 rttnOffst;
            private Vector3 mvOffst;
            private string lstCrsshr;
            private string lstWrnng;
            private Planner plnnr;
            private bool isPlanner;
            private bool isHammering;
            internal bool isWireTool;
            internal bool isLightDeployer;
            private HeldEntity heldItem;
            private bool sRmvr;
            private bool isAnotherHeld;
            private Item ctvtmLnk;
            private bool sTpDplybl;
            private int dfltGrd;
            private uint lstPrfb;
            private bool initialized;
            private bool ctvTrgt;
            private bool isPlacing;
            private float tkDist;
            private RaycastHit rayHit;
            private BaseEntity rayEntity;
            private IPlayer rayEntityOwner;
            private string rayEntityName;
            private Vector3 lastAimAngles;
            private Socket_Base lastSocketBase;
            private Vector3 lastSocketPos;
            private BaseEntity lastSocketEntity;
            private Construction.Placement lastPlacement;
            private Ray lastRay;
            private bool plannerInfoStatus;
            private bool removerInfoStatus;
            private bool hammerInfoStatus;
            private bool lastSocketForce;
            private int cuiFontSize = 14;
            private string cuiFontColor = "1 1 1 1";
            private string fontType = r("EbobgbPbaqrafrq-Erthyne.ggs");
            private float lastPosRotUpdate = 0f;

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                serverInput = player.serverInput;
                Unequip();
                dfltGrd = Instance.playerPrefs.playerData[player.userID].DBG;
                lstPrfb = 72949757u;
                ctvtm = 0;
                construction = new Construction();
                rayDefinition = new Construction();
                construction.canBypassBuildingPermission = true;
                lastAimAngles = player.lastReceivedTick.inputState.aimAngles;
                lastSocketBase =
            default(Socket_Base);
                lastSocketPos = Vector3.zero;
                lastSocketEntity =
            default(BaseEntity);
                lastPlacement =
            default(Construction.Placement);
                rayEntity =
            default(BaseEntity);
                rttnOffst = Vector3.zero;
                mvOffst = Vector3.zero;
            }

            private void Start()
            {
                if (Instance.hideTips) player.SendConsoleCommand(r("tnzrgvc.uvqrtnzrgvc"));
                initialized = true;
            }

            private void Unequip()
            {
                foreach (Item item in player.inventory.containerBelt.itemList.Where(x => x.IsValid() && x.GetHeldEntity()).ToList())
                {
                    int slot = item.position;
                    if (item.info.shortname == "rock" && item.skin == 0uL || item.info.shortname == "torch")
                    {
                        item.Remove(0f);
                        continue;
                    }
                    else
                    {
                        item.RemoveFromContainer();
                    }

                    player.inventory.UpdateContainer(0f, PlayerInventory.Type.Belt, player.inventory.containerBelt, false, 0f);
                    Instance.timer.Once(0.15f, () =>
                    {
                        if (item == null) return;
                        item.MoveToContainer(player.inventory.containerBelt, slot, true);
                        item.MarkDirty();
                    });
                    ItemManager.DoRemoves();
                }

                if (player.inventory.containerWear.itemList.Count == 0)
                {
                    Item hz = ItemManager.CreateByName("hazmatsuit_scientist", 1);
                    player.inventory.GiveItem(hz, player.inventory.containerWear);
                }

                Instance.timer.Once(0.3f, CrtTls);
            }

            private void GetTool(object[] tool)
            {
                ItemDefinition itemDef = ItemManager.FindItemDefinition((string)tool[1]);
                if (!itemDef) return;
                Item p1 = player.inventory.FindItemID(itemDef.itemid);
                ulong skin = Convert.ToUInt64(tool[2]);
                if (p1 != null)
                {
                    p1.skin = skin;
                    p1.GetHeldEntity().skinID = skin;
                    p1.name = (string)tool[0];
                    if (p1.CanMoveTo(player.inventory.containerBelt, -1))
                    {
                        p1.MoveToContainer(player.inventory.containerBelt, -1, true);
                        p1.MarkDirty();
                    }
                }
                else
                {
                    Item p2 = ItemManager.CreateByItemID(itemDef.itemid, 1, skin);
                    if (p2 != null)
                    {
                        p2.name = (string)tool[0];
                        player.inventory.GiveItem(p2, player.inventory.containerBelt);
                        p2.MarkDirty();
                    }
                }
            }

            private void CrtTls()
            {
                if (Instance.checkExistingPlanner) GetTool(Instance.playerTools[0]);
                if (Instance.checkExistingRemover) GetTool(Instance.playerTools[1]);
                if (Instance.checkExistingHammer) GetTool(Instance.playerTools[2]);
            }

            private bool GetCurrentTool()
            {
                isPlanner = false;
                sRmvr = false;
                isHammering = false;
                isWireTool = false;
                isAnotherHeld = false;
                isLightDeployer = false;
                DestroyInfo();
                if (heldItem is Planner)
                {
                    plnnr = heldItem as Planner;
                    isPlanner = true;
                    sTpDplybl = plnnr.isTypeDeployable;
                    DoPlannerInfo();
                    return true;
                }
                else if (heldItem is BaseProjectile && ctvtmLnk.skin == Convert.ToUInt64(Instance.playerTools[1][2]))
                {
                    sRmvr = true;
                    return true;
                }
                else if (heldItem is Hammer && ctvtmLnk.skin == Convert.ToUInt64(Instance.playerTools[2][2]))
                {
                    isHammering = true;
                    DoHammerInfo();
                    return true;
                }
                else if (heldItem is AttackEntity)
                {
                    isAnotherHeld = true;
                    return true;
                }
                else if (heldItem is WireTool)
                {
                    isWireTool = true;
                    return true;
                }
                else if (heldItem is PoweredLightsDeployer)
                {
                    isLightDeployer = true;
                    return true;
                }

                if (!isWireTool && (source != null || isWiring))
                {
                    if (sourceSlot != null)
                        sourceSlot.linePoints = new Vector3[0];

                    source = null;
                    sourceSlot = null;
                    isWiring = false;
                }

                return false;
            }

            private void CheckRemover()
            {
                bool hsLsr = false;
                if (ctvtmLnk.info.shortname != (string)Instance.playerTools[1][1])
                {
                    sRmvr = false;
                    heldItem = null;
                    return;
                }

                ctvtmLnk.contents.flags = (ItemContainer.Flag)64;
                ctvtmLnk.contents.MarkDirty();
                if (ctvtmLnk.contents != null && ctvtmLnk.contents.itemList.Count > 0) foreach (Item mod in ctvtmLnk.contents.itemList) if (mod.info.shortname == r("jrncba.zbq.ynfrefvtug"))
                        {
                            hsLsr = true;
                            break;
                        }

                if (!hsLsr)
                {
                    Item lMod = ItemManager.CreateByName(r("jrncba.zbq.ynfrefvtug"), 1);
                    if (lMod != null) if (lMod.MoveToContainer(ctvtmLnk.contents, -1, true))
                        {
                            hsLsr = true;
                        }
                        else
                        {
                            sRmvr = false;
                            heldItem = null;
                            return;
                        }
                }

                (heldItem as BaseProjectile).UnloadAmmo(ctvtmLnk, player);
                heldItem.SetLightsOn(true);
                DoRemoverInfo();
            }

            public void SetHeldItem(uint uid)
            {
                if (!initialized || uid == ctvtm) return;
                if (uid == 0u)
                {
                    ctvtm = 0u;
                    isPlanner = false;
                    sRmvr = false;
                    isHammering = false;
                    isWireTool = false;
                    sTpDplybl = false;
                    construction = null;
                    isLightDeployer = false;
                    DestroyInfo();
                    return;
                }

                if (uid != ctvtm)
                {
                    ctvtmLnk = player.inventory.containerBelt.FindItemByUID(uid);
                    if (ctvtmLnk == null) return;
                    ctvtm = uid;
                    heldItem = ctvtmLnk.GetHeldEntity() as HeldEntity;
                    if (heldItem == null) return;
                    if (!GetCurrentTool()) return;
                    if (sRmvr)
                    {
                        CuiHelper.DestroyUi(player, r("HgPebffUnveHV"));
                        CuiHelper.DestroyUi(player, "ut.ioslotchooser");
                        CheckRemover();
                    }
                    else if (isPlanner || isHammering)
                    {
                        uint prefabId = lstPrfb;
                        
                        if (isPlanner && sTpDplybl)
                        {
                            if (plnnr.GetDeployable() != null)
                                prefabId = plnnr.GetDeployable().prefabID;
                            else 
                            {
                                ItemModDeployable itemDef = plnnr.GetOwnerItemDefinition().GetComponent<ItemModDeployable>();
                                if (itemDef != null)                                
                                    prefabId = itemDef.entityPrefab.resourceID;
                            }
                        }

                        construction = PrefabAttribute.server.Find<Construction>(prefabId);  
                        rttnOffst = Vector3.zero;

                        if (isPlanner)
                        {
                            if (sTpDplybl) DoPlannerUpdate(PType.Mode, ctvtmLnk.info.displayName.english);
                            else DoPlannerUpdate(PType.Mode, $"{construction.info.name.english} ({((BuildingGrade.Enum)dfltGrd).ToString()})");
                        }
                        else
                        {
                            DoPlannerUpdate(PType.Mode);
                        }
                    }
                }
            }

            private bool isWiring = false;

            public void TickUpdate(PlayerTick tick)
            {
                if (!initialized)
                    return;

                bool changedInput = tick.inputState.aimAngles != lastAimAngles || tick.inputState.buttons != serverInput.previous.buttons || lastSocketForce;

                if (lastSocketForce)
                    lastSocketForce = false;

                if (changedInput && !ctvTrgt)
                {
                    rayHit = default(RaycastHit);
                    lastAimAngles = tick.inputState.aimAngles;
                    int layer = sRmvr && Instance.removeToolObjects ? 1143155457 : 2163457;
                    float range = 24f;

                    if (sRmvr)
                        range = Instance.removeToolRange;
                    else if (isHammering)
                        range = Instance.hammerToolRange;

                    lastRay = new Ray(tick.position + new Vector3(0f, 1.5f, 0f), Quaternion.Euler(tick.inputState.aimAngles) * Vector3.forward);
                    if (Physics.Raycast(lastRay, out rayHit, range, layer, QueryTriggerInteraction.Ignore))
                    {
                        BaseEntity ent = rayHit.GetEntity();
                        if (ent != null && ent != rayEntity)
                        {
                            rayEntity = ent;
                            rayDefinition = PrefabAttribute.server.Find<Construction>(rayEntity.prefabID);
                            if (rayEntity.OwnerID > 0uL) rayEntityOwner = Instance.covalence.Players.FindPlayerById(rayEntity.OwnerID.ToString());
                            else rayEntityOwner = null;
                            rayEntityName = "";
                            if (rayDefinition) rayEntityName = rayDefinition.info.name.english;
                            if (rayEntityName.Length == 0)
                            {
                                if (rayEntity is BaseCombatEntity)
                                {
                                    rayEntityName = (rayEntity as BaseCombatEntity).repair.itemTarget?.displayName.english;
                                    if (rayEntityName == null || rayEntityName.Length == 0) rayEntityName = rayEntity.ShortPrefabName;
                                }
                                else
                                {
                                    rayEntityName = rayEntity.ShortPrefabName;
                                }
                            }

                            if (rayDefinition == null && (rayEntity.PrefabName.EndsWith("static.prefab") || rayEntity.PrefabName.Contains("/deployable/")))
                            {
                                rayDefinition = new Construction();
                                rayDefinition.rotationAmount = new Vector3(0, 90f, 0);
                                rayDefinition.fullName = rayEntity.PrefabName;
                                rayDefinition.maxplaceDistance = 8f;
                            }
                        }
                        else if (ent == null)
                        {
                            rayEntity = null;
                            rayDefinition = null;
                            rayEntityOwner = null;
                            rayEntityName = "";
                        }
                    }
                    else
                    {
                        rayEntity = null;
                        rayDefinition = null;
                        rayEntityOwner = null;
                        rayEntityName = "";
                    }

                    if (isPlanner)
                    {
                        if (lstWrnng != string.Empty)
                            DoWarning(string.Empty, true);

                        target = default(Construction.Target);
                        target.player = player;
                        target.ray = lastRay;
                        CheckPlacement(ref target, construction);

                        if (target.socket != null && (target.socket != lastSocketBase || target.entity != lastSocketEntity || lastSocketForce))
                        {
                            if (lastSocketForce)
                                lastSocketForce = false;

                            bool chEnt = false;
                            if (Instance.effectFoundationPlacement && construction.hierachyName.Contains("foundation") && lastSocketEntity != target.entity)
                            {
                                chEnt = true;
                                SendEffectTo(3951505782, target.entity, player);
                            }

                            lastSocketEntity = target.entity;
                            string name = target.entity.ShortPrefabName;

                            if (target.entity is BuildingBlock)
                                DoPlannerUpdate(PType.ConnectTo, $"{rayEntityName} [{target.entity.net.ID}] ({(target.entity as BuildingBlock).currentGrade.gradeBase.type.ToString()})");
                            else DoPlannerUpdate(PType.ConnectTo, $"{rayEntityName} [{target.entity.net.ID}]");

                            if (Instance.effectFoundationPlacement && !chEnt && construction.hierachyName.Contains("foundation") && lastSocketBase != target.socket)
                                SendEffectTo(3389733993, target.entity, player);

                            lastSocketBase = target.socket;
                            lastSocketPos = lastSocketEntity.transform.localToWorldMatrix.MultiplyPoint3x4(lastSocketBase.position);

                            string s1 = lastSocketBase.socketName.Replace($"{target.entity.ShortPrefabName}/sockets/", "").TrimEnd('/', '1', '2', '3', '4').Replace("-", " ").Replace("–", " ");

                            DoPlannerUpdate(PType.ToSocket, $"{Oxide.Core.ExtensionMethods.TitleCase(s1)}");
                            lastPlacement = CheckPlacement(target, construction);

                            if (lastPlacement != null)
                                DoPlannerUpdate(PType.PosRot, $"{lastPlacement.position.ToString("N1")} | {lastPlacement.rotation.eulerAngles.y.ToString("N1")}°");
                            else DoPlannerUpdate(PType.PosRot);
                        }

                        if (sTpDplybl)
                        {
                            lastPlacement = CheckPlacement(target, construction);

                            if (lastPlacement != null)
                            {
                                DoPlannerUpdate(PType.PosRot, $"{lastPlacement.position.ToString("N1")} | {lastPlacement.rotation.eulerAngles.ToString("N1")}");
                                DoPlannerUpdate(PType.ToSocket, "Terrain");
                                if (rayEntity) DoPlannerUpdate(PType.ConnectTo, $"{rayEntityName} [{rayEntity.net.ID}]");
                            }
                            else
                            {
                                DoPlannerUpdate(PType.ToSocket);
                                DoPlannerUpdate(PType.ConnectTo);
                                DoPlannerUpdate(PType.PosRot);
                            }
                        }

                        if (!sTpDplybl && !target.socket)
                        {
                            lastSocketBase = default(Socket_Base);
                            lastSocketEntity = default(BaseEntity);

                            DoPlannerUpdate(PType.ConnectTo);
                            DoPlannerUpdate(PType.PosRot);
                            DoPlannerUpdate(PType.ToSocket);
                        }
                    }
                    else if (isHammering)
                    {
                        if (lstWrnng != string.Empty)
                            DoWarning(string.Empty, true);

                        if (!ctvTrgt)
                        {
                            if (rayEntity && rayHit.distance <= Instance.hammerToolRange)
                            {
                                if (rayDefinition && rayEntity is BuildingBlock)
                                {
                                    DoHammerUpdate(HType.Target, $"{rayEntityName} [{rayEntity.net.ID}] ({(rayEntity as BuildingBlock).currentGrade.gradeBase.type.ToString()})");
                                    DoCrosshair("0 1 0 0.75");
                                }
                                else if (rayDefinition)
                                {
                                    if (rayDefinition.fullName == StringPool.Get(3424003500))
                                        DoHammerUpdate(HType.Target, $"{rayEntityName} [{rayEntity.net.ID}] (Type: {(rayEntity as MiningQuarry).staticType})");
                                    else DoHammerUpdate(HType.Target, $"{rayEntityName} [{rayEntity.net.ID}]");

                                    DoCrosshair("1 0.921568632 0.0156862754 0.75");
                                }
                                else
                                {
                                    DoHammerUpdate(HType.Target, $"{rayEntityName} [{rayEntity.net.ID}]");
                                }

                                DoHammerUpdate(HType.Mode, "Modify");
                                DoHammerUpdate(HType.Building, rayEntity is DecayEntity ? $"ID {(rayEntity as DecayEntity).buildingID}" : "None");

                                if (rayDefinition)
                                {
                                    float currentTime = Time.realtimeSinceStartup;
                                    if (currentTime - lastPosRotUpdate >= 0.25f)
                                    {
                                        if (rayEntity is BuildingBlock)
                                            DoHammerUpdate(HType.PosRot, $"{rayEntity.transform.position.ToString("N1")} | {rayEntity.transform.rotation.eulerAngles.y.ToString("N1")}°");
                                        else DoHammerUpdate(HType.PosRot, $"{rayEntity.transform.position.ToString("N1")} | {rayEntity.transform.rotation.eulerAngles.ToString("N1")}");

                                        lastPosRotUpdate = currentTime;
                                    }

                                    if (rayEntityOwner != null)
                                        DoHammerUpdate(HType.Owner, $"{rayEntityOwner.Name}");
                                    else DoHammerUpdate(HType.Owner, $"{rayEntity.OwnerID}");

                                    DoHammerUpdate(HType.SteamID, $"{rayEntity.OwnerID}");
                                }
                            }
                            else
                            {
                                DoHammerUpdate(HType.Target);
                                DoHammerUpdate(HType.Building);
                                DoHammerUpdate(HType.Mode, r("Zbqvsl"));
                                DoHammerUpdate(HType.PosRot);
                                DoHammerUpdate(HType.Owner);
                                DoCrosshair("1 1 1 0.75");
                            }
                        }
                        else
                        {
                            DoHammerUpdate(HType.PosRot, $"{mvTrgt.entity.transform.position.ToString("N1")} | {mvTrgt.entity.transform.rotation.eulerAngles.ToString("N1")}");
                            DoCrosshair(string.Empty);
                        }
                    }
                    else if (sRmvr)
                    {
                        DoCrosshair(string.Empty);
                        if (rayEntity != null && rayHit.distance <= Instance.removeToolRange && (rayDefinition || !rayDefinition && Instance.removeToolObjects))
                        {
                            DoRemoverUpdate(RType.Remove, $"{rayEntityName} [{rayEntity.net.ID}]");
                            if (rayEntityOwner != null) DoRemoverUpdate(RType.Owner, $"{rayEntityOwner.Name}");
                            else DoRemoverUpdate(RType.Owner, $"{rayEntity.OwnerID}");
                        }
                        else
                        {
                            DoRemoverUpdate(RType.Remove);
                            DoRemoverUpdate(RType.Owner);
                        }

                        if (Instance.enableFullRemove && serverInput.IsDown(controlButtons[CmdType.RemoverHoldForAll]) && rayEntity is BuildingBlock)
                        {
                            DoWarning("1 0 0 0.75");
                            DoRemoverUpdate(RType.Mode, "<color=#ffff00>Building</color>");
                        }
                        else
                        {
                            DoRemoverUpdate(RType.Mode, r("Fvatyr"));
                            DoWarning(string.Empty);
                        }
                    }
                    else if (isAnotherHeld)
                    {
                        if (heldItem is BaseLiquidVessel && (serverInput.WasJustReleased((BTN) 1024) || serverInput.WasDown((BTN) 2048)))
                        {
                            BaseLiquidVessel vessel = heldItem as BaseLiquidVessel;
                            if (vessel.AmountHeld() < 1) vessel.AddLiquid(ItemManager.FindItemDefinition("water"), vessel.MaxHoldable());
                        }
                        else if (heldItem is BaseProjectile && serverInput.WasJustPressed((BTN) 8192))
                        {
                            BaseProjectile weapon = heldItem as BaseProjectile;
                            if (!weapon.primaryMagazine.CanReload(player) && weapon.primaryMagazine.contents < weapon.primaryMagazine.capacity)
                            {
                                try
                                {
                                    player.inventory.GiveItem(ItemManager.Create(weapon.primaryMagazine.ammoType, weapon.primaryMagazine.capacity - weapon.primaryMagazine.contents));
                                }
                                catch
                                {
                                }

                                ItemManager.DoRemoves();
                            }
                        }
                        else if (heldItem is FlameThrower && (serverInput.WasJustPressed((BTN) 8192) || serverInput.IsDown((BTN) 1024)))
                        {
                            FlameThrower flame = heldItem as FlameThrower;
                            if (serverInput.IsDown((BTN) 1024) && flame.ammo < 2 || serverInput.WasJustPressed((BTN) 8192) && flame.ammo < flame.maxAmmo)
                            {
                                flame.ammo = flame.maxAmmo;
                                flame.SendNetworkUpdateImmediate(false);
                                ItemManager.DoRemoves();
                                player.inventory.ServerUpdate(0f);
                            }
                        }
                        else if (heldItem is Chainsaw && (serverInput.WasJustPressed((BTN) 8192) || serverInput.IsDown((BTN) 1024) || serverInput.WasJustPressed((BTN) 2048)))
                        {
                            Chainsaw saw = heldItem as Chainsaw;
                            if (serverInput.WasJustPressed((BTN) 2048) && !saw.EngineOn())
                            {
                                saw.SetEngineStatus(true);
                                heldItem.SendNetworkUpdateImmediate(false);
                            }
                            else if (serverInput.IsDown((BTN) 1024) && saw.ammo < 2 || serverInput.WasJustPressed((BTN) 8192) && saw.ammo < saw.maxAmmo)
                            {
                                saw.ammo = saw.maxAmmo;
                                saw.SendNetworkUpdateImmediate(false);
                                ItemManager.DoRemoves();
                                player.inventory.ServerUpdate(0f);
                            }
                        }
                    }
                    else if (isLightDeployer)
                    {
                        PoweredLightsDeployer lightsDeployer = heldItem as PoweredLightsDeployer;
                        if (lightsDeployer == null)
                            return;

                        if (player.serverInput.WasJustPressed(BTN.FIRE_SECONDARY))
                        {
                            lightsDeployer.DoFinish();
                            return;
                        }

                        if (player.serverInput.WasJustPressed(BTN.FIRE_PRIMARY))
                        {
                            RaycastHit raycastHit;
                            if (Physics.Raycast(player.eyes.HeadRay(), out raycastHit, 5f))
                            {
                                if (heldItem.GetItem() == null)
                                {
                                    return;
                                }

                                if (heldItem.GetItem().amount < 1)
                                {
                                    return;
                                }

                                if (!heldItem.IsVisible(raycastHit.point, Single.PositiveInfinity))
                                {
                                    return;
                                }

                                if (Vector3.Distance(raycastHit.point, player.eyes.position) > 5f)
                                {
                                    player.ChatMessage("Too far away!");
                                    return;
                                }

                                int amountToUse = 1;
                                if (lightsDeployer.active != null)
                                {
                                    if (lightsDeployer.active.IsFinalized())
                                        return;

                                    float length = 0f;
                                    Vector3 position = lightsDeployer.active.transform.position;
                                    if (lightsDeployer.active.points.Count > 0)
                                    {
                                        position = lightsDeployer.active.points[lightsDeployer.active.points.Count - 1].point;
                                        length = Vector3.Distance(raycastHit.point, position);
                                    }

                                    length = Mathf.Max(length, lightsDeployer.lengthPerAmount);
                                    float item1 = (float) heldItem.GetItem().amount * lightsDeployer.lengthPerAmount;
                                    if (length > item1)
                                    {
                                        length = item1;
                                        raycastHit.point = position + (Vector3Ex.Direction(raycastHit.point, position) * length);
                                    }

                                    length = Mathf.Min(item1, length);
                                    amountToUse = Mathf.CeilToInt(length / lightsDeployer.lengthPerAmount);
                                }
                                else
                                {
                                    AdvancedChristmasLights component = GameManager.server.CreateEntity(lightsDeployer.poweredLightsPrefab.resourcePath, raycastHit.point, Quaternion.LookRotation(raycastHit.normal, player.eyes.HeadUp()), true).GetComponent<AdvancedChristmasLights>();
                                    component.Spawn();
                                    lightsDeployer.active = component;
                                    amountToUse = 1;
                                }

                                lightsDeployer.active.AddPoint(raycastHit.point, raycastHit.normal);
                                lightsDeployer.SetFlag(BaseEntity.Flags.Reserved8, lightsDeployer.active != null, false, true);
                                lightsDeployer.active.AddLengthUsed(amountToUse);
                                lightsDeployer.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                            }
                        }
                    }
                    else if (isWireTool)
                    {
                        if (player.serverInput.WasJustPressed(BTN.FIRE_SECONDARY))
                        {
                            source = null;
                            sourceSlot = null;

                            if (isWiring)
                            {
                                isWiring = false;
                                player.ChatMessage("Cancelled current IO connection");
                            }

                            return;
                        }

                        if (player.serverInput.WasJustPressed(BTN.FIRE_PRIMARY))
                        {
                            RaycastHit raycastHit;
                            Ray ray = player.eyes.HeadRay();
                            if (Physics.Raycast(ray, out raycastHit, 5f))
                            {
                                IOEntity ioEntity = raycastHit.GetEntity() as IOEntity;
                                if (!ioEntity)
                                {
                                    player.ChatMessage("You are not looking at an IO entity");
                                    return;
                                }

                                IOEntity.IOSlot[] slots = isWiring ? ioEntity.inputs : ioEntity.outputs;
                                
                                int slotCount = slots.Where(x => x.type == IOEntity.IOType.Electric && !x.connectedTo.ioEnt).Count();
                                
                                if (slotCount == 0)
                                {
                                    player.ChatMessage("The IO entity you are looking at has no free electric slots");
                                    return;
                                }

                                const string PANEL = "ut.ioslotchooser";
                                float top = 0.5f + (0.035f * ((float) slotCount * 0.5f));
                               
                                CuiElementContainer mainContainer = new CuiElementContainer()
                                {
                                    {
                                        new CuiPanel
                                        {
                                            Image =
                                            {
                                                Color = "0 0 0 0"
                                            },
                                            RectTransform =
                                            {
                                                AnchorMin = $"0.45 {top - 0.035f}",
                                                AnchorMax = $"0.55 {top}"
                                            },
                                            CursorEnabled = true
                                        },
                                        new CuiElement().Parent = "Overlay",
                                        PANEL
                                    }
                                };
                                
                                mainContainer.Add(new CuiPanel
                                {
                                    Image = 
                                    {
                                        Color = "0 0 0 0.85",
                                        Material = "assets/content/ui/uibackgroundblur.mat"
                                    },
                                    RectTransform = 
                                    {
                                        AnchorMin = "0 1.05",
                                        AnchorMax = "1 1.95"
                                    }
                                }, PANEL);
                                
                                mainContainer.Add(new CuiElement
                                {
                                    Name = CuiHelper.GetGuid(),
                                    Parent = PANEL,
                                    Components = {
                                        new CuiTextComponent {
                                            Color = cuiFontColor,
                                            Text = isWiring ? "Choose an input slot" : "Choose an output slot",
                                            Font = fontType,
                                            FontSize = 12,
                                            Align = TextAnchor.MiddleCenter
                                        },
                                        new CuiRectTransformComponent {
                                            AnchorMin = "0 1.05",
                                            AnchorMax = "1 1.95"
                                        }
                                    }
                                });

                                mainContainer.Add(new CuiButton
                                {
                                    Button = {
                                        Command = $"ut.io -1",
                                        Color = "0 0 0 0.85",
                                        Material = "assets/content/ui/uibackgroundblur.mat"
                                    },
                                    RectTransform = {
                                        AnchorMin = "0 0.05",
                                        AnchorMax = "1 0.95"
                                    },
                                    Text = {
                                        Text = "Cancel",
                                        FontSize = 12,
                                        Align = TextAnchor.MiddleCenter
                                    }
                                }, PANEL);

                                int count = 0;
                                for (int i = 0; i < slots.Length; i++)
                                {
                                    IOEntity.IOSlot ioSlot = slots[i];
                                    if (ioSlot.type != IOEntity.IOType.Electric || ioSlot.connectedTo.ioEnt)
                                        continue;

                                    mainContainer.Add(new CuiButton
                                    {
                                        Button = {
                                            Command = $"ut.io {i}",
                                            Color = "0 0 0 0.85",
                                            Material = "assets/content/ui/uibackgroundblur.mat"
                                        },
                                        RectTransform = {
                                            AnchorMin = $"0 {(-(count + 1)) + 0.05f}",
                                            AnchorMax = $"1 {(-count) - 0.05f}"
                                        },
                                        Text = {
                                            Text = ioSlot.niceName,
                                            FontSize = 12,
                                            Align = TextAnchor.MiddleCenter
                                        }
                                    }, PANEL);
                                    count++;
                                }
                                
                                CuiHelper.DestroyUi(player, PANEL);
                                CuiHelper.AddUi(player, mainContainer);
                            }
                        }
                    }
                }
                else if (changedInput && ctvTrgt)
                {
                    DoHammerUpdate(HType.PosRot, $"{mvTrgt.entity.transform.position.ToString("N1")} | {mvTrgt.entity.transform.rotation.eulerAngles.ToString("N1")}");
                    DoCrosshair(string.Empty, true);
                }

                if (isPlanner && !sTpDplybl)
                {
                    if (lastSocketBase != null && lastPlacement != null && lastSocketEntity)
                    {
                        OBB oBB = new OBB(lastPlacement.position, Vector3.one, lastPlacement.rotation, construction.bounds);
                        Vector3 obb_pos = construction.hierachyName.Contains(r("sbhaqngvba")) ? oBB.position + oBB.extents.y * Vector3.up : oBB.position;
                        Vector3 sock_pos = construction.hierachyName.Contains(r("sbhaqngvba")) ? new Vector3(lastSocketPos.x, lastSocketEntity.transform.position.y, lastSocketPos.z) : lastSocketPos;
                        player.SendConsoleCommand("ddraw.box", 0.05f, Color.green, obb_pos, 0.15f);
                        player.SendConsoleCommand("ddraw.box", 0.05f, Color.green, sock_pos, 0.25f);
                        player.SendConsoleCommand("ddraw.line", 0.05f, Color.green, obb_pos, sock_pos);
                    }
                }
            }

            public void OnIO(int index)
            {
                CuiHelper.DestroyUi(player, "ut.ioslotchooser");
                
                if (index == -1)
                {
                    isWiring = false;
                    player.ChatMessage("Cancelled current IO connection");
                    return;
                }
                
                RaycastHit raycastHit;
                Ray ray = player.eyes.HeadRay();
                if (Physics.Raycast(ray, out raycastHit, 5f))
                {
                    IOEntity ioEntity = raycastHit.GetEntity() as IOEntity;
                    if (!isWiring)
                    {
                        if (ioEntity != null)
                        {
                            IOEntity.IOSlot target = ioEntity.outputs[index];
                            if (target != null)
                            {
                                source = ioEntity;
                                sourceSlot = target;
                                isWiring = true;

                                player.ChatMessage($"Begin Wiring - From {source.ShortPrefabName} (Slot {sourceSlot.niceName})");
                                player.SendConsoleCommand("ddraw.sphere", 30f, Color.green, source.transform.TransformPoint(sourceSlot.handlePosition), 0.025f);
                                Effect.server.Run(WIRE_EFFECT, ioEntity.transform.position);
                            }
                            else player.ChatMessage("No valid IO Entity found");
                        }
                    }
                    else
                    {
                        if (ioEntity == null)
                            player.ChatMessage("Select another IO slot to make a connection");
                        else
                        {
                            if (ioEntity == source)
                            {
                                player.ChatMessage("You can not connect a IO entity to itself");
                                return;
                            }

                            IOEntity.IOSlot target = ioEntity.inputs[index];
                            if (target != null)
                            {
                                player.SendConsoleCommand("ddraw.sphere", 30f, Color.green, ioEntity.transform.TransformPoint(target.handlePosition), 0.025f);

                                sourceSlot.connectedTo = new IOEntity.IORef();
                                sourceSlot.connectedTo.ioEnt = ioEntity;
                                sourceSlot.connectedTo.Set(ioEntity);
                                sourceSlot.connectedToSlot = index;
                                sourceSlot.linePoints = new Vector3[] {ioEntity.transform.TransformPoint(target.handlePosition), source.transform.TransformPoint(sourceSlot.handlePosition)};

                                target.connectedTo = new IOEntity.IORef();
                                target.connectedTo.ioEnt = source;
                                target.connectedTo.Set(source);

                                source.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                                ioEntity.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);

                                source.MarkDirtyForceUpdateOutputs();
                                ioEntity.MarkDirtyForceUpdateOutputs();

                                player.ChatMessage($"Connected IO from {source.ShortPrefabName} (Slot {sourceSlot.niceName}) -> {ioEntity.ShortPrefabName} (Slot {target.niceName})");

                                Effect.server.Run(WIRE_EFFECT, ioEntity.transform.position);

                                source = null;
                                sourceSlot = null;
                                isWiring = false;
                            }
                            else player.ChatMessage("Failed to make a connection");
                        }
                    }
                }
            }

            private IOEntity source;
            private IOEntity.IOSlot sourceSlot;

            private void Update()
            {
                if (!ctvTrgt) return;
                if (!isPlacing && isHammering)
                {
                    if (mvTrgt.entity == null)
                    {
                        DoCrosshair("1 1 1 0.75");
                        mvTrgt =
                    default(Construction.Target);
                        isPlacing = false;
                        ctvTrgt = false;
                        return;
                    }

                    bool flag = mvTrgt.entity is SimpleBuildingBlock || mvCnstrctn.allSockets == null;
                    mvTrgt.ray = player.eyes.BodyRay();
                    FndTrrnPlcmnt(ref mvTrgt, mvCnstrctn, tkDist, flag);
                    Vector3 position = mvTrgt.entity.transform.position;
                    Quaternion rotation = mvTrgt.entity.transform.rotation;
                    Vector3 toPos = mvTrgt.position;
                    Quaternion toRot = Quaternion.LookRotation(mvTrgt.entity.transform.up) * Quaternion.Euler(mvOffst);
                    if (flag)
                    {
                        Vector3 direction = mvTrgt.ray.direction;
                        direction.y = 0f;
                        direction.Normalize();
                        toRot = Quaternion.Euler(mvOffst) * Quaternion.LookRotation(direction, Vector3.up);
                    }

                    Construction.Placement check = CheckPlacement(mvTrgt, mvCnstrctn);
                    if (check != null)
                    {
                        toPos = check.position;
                        toRot = check.rotation * Quaternion.Euler(mvOffst);
                    }

                    mvTrgt.entity.transform.position = Vector3.Lerp(position, toPos, Time.deltaTime * 5f);
                    mvTrgt.entity.transform.rotation = Quaternion.Lerp(rotation, toRot, Time.deltaTime * 10f);
                    DMvmntSnc(mvTrgt.entity);
                    return;
                }
                else if (isPlacing)
                {
                    if (mvTrgt.entity == null)
                    {
                        DoCrosshair("1 1 1 0.75");
                        mvTrgt =
                    default(Construction.Target);
                        isPlacing = false;
                        ctvTrgt = false;
                        return;
                    }

                    if (Vector3.Distance(mvTrgt.entity.transform.position, mvTrgt.position) <= 0.005f)
                    {
                        if (mvTrgtSnp && !(mvTrgtSnp is BuildingBlock))
                        {
                            mvTrgt.entity.transform.position = mvTrgtSnp.transform.InverseTransformPoint(mvTrgt.position);
                            mvTrgt.entity.transform.rotation = Quaternion.Inverse(mvTrgtSnp.transform.rotation) * Quaternion.Euler(mvTrgt.rotation);
                            mvTrgt.entity.SetParent(mvTrgtSnp, 0u);
                        }

                        if (mvTrgtSnp)
                        {
                            DecayUpdate(mvTrgt.entity, true, mvCnstrctn.isBuildingPrivilege, mvTrgtSnp);
                            mvTrgtSnp = null;
                        }

                        DMvmntSnc(mvTrgt.entity);
                        DoCrosshair("1 1 1 0.75");
                        mvTrgt =
                    default(Construction.Target);
                        isPlacing = false;
                        ctvTrgt = false;
                        return;
                    }

                    mvTrgt.entity.transform.position = Vector3.Lerp(mvTrgt.entity.transform.position, mvTrgt.position, Time.deltaTime * 10f);
                    if (mvTrgtSnp == null || mvTrgtSnp && !(mvTrgtSnp is BuildingBlock))
                        mvTrgt.entity.transform.rotation = Quaternion.Lerp(mvTrgt.entity.transform.rotation, Quaternion.Euler(mvTrgt.rotation), Time.deltaTime * 10f);
                    DMvmntSnc(mvTrgt.entity);
                    return;
                }
                else if (!isPlacing && !isHammering)
                {
                    if (mvTrgt.valid) PlaceOnTarget();
                    else TrPlcTrgt();
                }
            }

            private void DecayUpdate(BaseEntity entity, bool isAdding, bool isBuildingPrivilege, BaseEntity target = null)
            {
                DecayEntity decayEntity = entity as DecayEntity;
                if (decayEntity == null) return;
                BuildingManager.Building building = null;
                if (isAdding)
                {
                    DecayEntity decayTarget = target != null ? target as DecayEntity : null;
                    if (decayTarget != null) building = BuildingManager.server.GetBuilding(decayTarget.buildingID);

                    if (building != null)
                    {
                        building.AddDecayEntity(decayEntity);
                        if (isBuildingPrivilege) building.AddBuildingPrivilege(decayEntity as BuildingPrivlidge);
                        building.Dirty();
                        decayEntity.buildingID = building.ID;
                    }
                }
                else
                {
                    building = BuildingManager.server.GetBuilding(decayEntity.buildingID);
                    if (building != null)
                    {
                        if (building.decayEntities != null) building.RemoveDecayEntity(decayEntity);
                        if (isBuildingPrivilege && building.buildingPrivileges != null) building.RemoveBuildingPrivilege(decayEntity as BuildingPrivlidge);
                        building.Dirty();
                    }

                    decayEntity.buildingID = 0u;
                }

                decayEntity.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                if (entity.children != null) foreach (BaseEntity current in entity.children) DecayUpdate(current, isAdding, isBuildingPrivilege, isAdding ? entity : null);
            }

            private void PlaceOnTarget()
            {
                if (mvTrgtSnp && !(mvTrgtSnp is BuildingBlock))
                {
                    mvTrgt.entity.transform.position = mvTrgtSnp.transform.worldToLocalMatrix.MultiplyPoint3x4(mvTrgt.position);
                    mvTrgt.entity.transform.rotation = Quaternion.Inverse(mvTrgtSnp.transform.rotation) * mvTrgt.entity.transform.rotation;
                    mvTrgt.entity.SetParent(mvTrgtSnp, 0u);
                }

                if (mvTrgtSnp)
                {
                    DecayUpdate(mvTrgt.entity, true, mvCnstrctn.isBuildingPrivilege, mvTrgtSnp);
                    mvTrgtSnp = null;
                }

                DMvmntSnc(mvTrgt.entity);
                mvTrgt =
            default(Construction.Target);
                ctvTrgt = false;
                isPlacing = false;
            }

            private void TrPlcTrgt()
            {
                RaycastHit hit;
                mvTrgtSnp = null;
                int layer = mvCnstrctn.isBuildingPrivilege ? 2097152 : 27328769;
                if (Physics.Raycast(mvTrgt.entity.transform.position, mvTrgt.entity.transform.up * -1.0f, out hit, float.PositiveInfinity, layer))
                {
                    mvTrgt.position = hit.point;
                    if (hit.collider is TerrainCollider)
                    {
                        mvTrgt.rotation = Quaternion.LookRotation(Vector3.Cross(mvTrgt.entity.transform.right, hit.normal)).eulerAngles;
                        DoHammerUpdate(HType.Building, "None");
                    }
                    else
                    {
                        mvTrgtSnp = hit.GetEntity();
                        if (mvTrgtSnp)
                        {
                            mvTrgt.rotation = mvTrgt.entity.transform.rotation.eulerAngles;
                            DoHammerUpdate(HType.Building, rayEntity is DecayEntity ? $"ID {(rayEntity as DecayEntity).buildingID}" : "None");
                        }
                        else
                        {
                            DoHammerUpdate(HType.Building, "None");
                        }
                    }

                    isPlacing = true;
                    return;
                }
                else
                {
                    mvTrgt = default(Construction.Target);
                    ctvTrgt = false;
                    isPlacing = false;
                    return;
                }
            }

            public object GtMvTrgt()
            {
                if (ctvTrgt && mvTrgt.entity != null) return (uint)mvTrgt.entity.net.ID;
                return null;
            }

            private void DMvmntSnc(BaseEntity entity, bool isChild = false)
            {
                if (entity == null)
                {
                    DoCrosshair("1 1 1 0.75");
                    mvTrgt = default(Construction.Target);
                    isPlacing = false;
                    ctvTrgt = false;
                    return;
                }

                bool force2 = entity.PrefabName == StringPool.Get(2206646561) || entity.PrefabName == StringPool.Get(2335812770);
                if (isChild || force2)
                {
                    if (Net.sv.write.Start())
                    {
                        Net.sv.write.PacketID(Message.Type.EntityDestroy);
                        Net.sv.write.UInt32(entity.net.ID);
                        Net.sv.write.UInt8(0);
                        Net.sv.write.Send(new SendInfo(entity.net.group.subscribers));
                    }

                    entity.SendNetworkUpdateImmediate(false);
                    if (isChild) return;
                }
                else
                {
                    if (Net.sv.write.Start())
                    {
                        Net.sv.write.PacketID(Message.Type.GroupChange);
                        Net.sv.write.EntityID(entity.net.ID);
                        Net.sv.write.GroupID(entity.net.group.ID);
                        Net.sv.write.Send(new SendInfo(entity.net.group.subscribers));
                    }

                    if (Net.sv.write.Start())
                    {
                        Net.sv.write.PacketID(Message.Type.EntityPosition);
                        Net.sv.write.EntityID(entity.net.ID);

                        Net.sv.write.WriteObject(entity.GetNetworkPosition());
                        Net.sv.write.WriteObject(entity.GetNetworkRotation().eulerAngles);

                        Net.sv.write.Float(entity.GetNetworkTime());
                        SendInfo info = new SendInfo(entity.net.group.subscribers)
                        {
                            method = SendMethod.ReliableUnordered,
                            priority = Priority.Immediate
                        };
                        Net.sv.write.Send(info);
                    }
                }

                if (force2 && entity && entity.children != null)
                    foreach (BaseEntity current in entity.children)
                        DMvmntSnc(current, true);
            }

            public void DoTick()
            {
                if (!initialized || !heldItem) return;

                if (isPlanner)
                {
                    if (true)
                    {
                        if (serverInput.WasJustPressed(controlButtons[CmdType.PlannerPlace]))
                        {
                            DoPlacement();
                            return;
                        }
                        else if (serverInput.WasJustPressed(controlButtons[CmdType.PlannerRotate]))
                        {
                            Vector3 vector = Vector3.zero;
                            if (construction && construction.canRotateBeforePlacement)
                                vector = construction.rotationAmount;
                            rttnOffst.x = Mathf.Repeat(rttnOffst.x + vector.x, 360f);
                            rttnOffst.y = Mathf.Repeat(rttnOffst.y + vector.y, 360f);
                            rttnOffst.z = Mathf.Repeat(rttnOffst.z + vector.z, 360f);
                            return;
                        }
                    }

                    if (!sTpDplybl)
                    {
                        if (serverInput.WasJustPressed((BTN)2048))
                        {
                            BldMnUI(Instance.playerPrefs.playerData[player.userID].SF);
                            return;
                        }

                        if (serverInput.IsDown(controlButtons[CmdType.PlannerTierChange]))
                        {
                            if (serverInput.WasJustPressed(controlButtons[CmdType.PlannerTierNext]))
                            {
                                dfltGrd++;
                                if (dfltGrd > 4) dfltGrd = 0;
                                Instance.playerPrefs.playerData[player.userID].DBG = dfltGrd;
                                DoPlannerUpdate(PType.Mode, $"{construction.info.name.english} ({((BuildingGrade.Enum)dfltGrd).ToString()})");
                                return;
                            }
                            else if (serverInput.WasJustPressed(controlButtons[CmdType.PlannerTierPrev]))
                            {
                                dfltGrd--;
                                if (dfltGrd < 0) dfltGrd = 4;
                                Instance.playerPrefs.playerData[player.userID].DBG = dfltGrd;
                                DoPlannerUpdate(PType.Mode, $"{construction.info.name.english} ({((BuildingGrade.Enum)dfltGrd).ToString()})");
                                return;
                            }
                        }
                    }
                    else if (sTpDplybl) { }
                }
                else if (isHammering)
                {
                    if (ctvTrgt)
                    {
                        if (isPlacing) { }
                        else if (!isPlacing)
                        {
                            if (serverInput.WasJustPressed(controlButtons[CmdType.HammerTransform]))
                            {
                                if (mvTrgt.valid) PlaceOnTarget();
                                else TrPlcTrgt();
                                return;
                            }
                            else if (serverInput.WasJustPressed(controlButtons[CmdType.HammerRotate]))
                            {
                                Vector3 vector = Vector3.zero;
                                if (mvCnstrctn && mvCnstrctn.canRotateAfterPlacement)
                                {
                                    if (serverInput.IsDown(controlButtons[CmdType.HammerRotateDirection])) vector = -mvCnstrctn.rotationAmount;
                                    else vector = mvCnstrctn.rotationAmount;
                                }

                                mvOffst.x = Mathf.Repeat(mvOffst.x + vector.x, 360f);
                                mvOffst.y = Mathf.Repeat(mvOffst.y + vector.y, 360f);
                                mvOffst.z = Mathf.Repeat(mvOffst.z + vector.z, 360f);
                                return;
                            }
                        }
                    }
                    else if (!ctvTrgt)
                    {
                        if (serverInput.WasJustPressed(controlButtons[CmdType.HammerChangeGrade]) && rayEntity && rayEntity.IsValid() && rayEntity is BuildingBlock)
                        {
                            BuildingBlock block = rayEntity as BuildingBlock;
                            int grade = (int)block.currentGrade.gradeBase.type;
                            grade++;
                            if (grade >= block.blockDefinition.grades.Length) grade = 1;
                            block.SetGrade((BuildingGrade.Enum)grade);
                            block.SetHealthToMax();
                            block.StartBeingRotatable();
                            rayEntity.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                            block.UpdateSkin(false);
                            BuildingManager.Building building = BuildingManager.server.GetBuilding(block.buildingID);
                            if (building != null) building.Dirty();
                            if (Instance.effectPromotingBlock) Effect.server.Run("assets/bundled/prefabs/fx/build/promote_" + ((BuildingGrade.Enum)grade).ToString().ToLower() + ".prefab", rayEntity, 0u, Vector3.zero, Vector3.zero, null, false);
                        }
                        else if (serverInput.WasJustPressed(controlButtons[CmdType.HammerToggleOnOff]) && rayEntity && rayEntity.IsValid() && !(rayEntity is BuildingBlock))
                        {
                            BaseEntity r = rayEntity;
                            if (r is StorageContainer || r is IOEntity)
                            {
                                bool isOn = r.HasFlag(BaseEntity.Flags.On);
                                bool hasPower = isOn & r is IOEntity;
                                r.SetFlag(BaseEntity.Flags.On, !isOn, false);
                                if (r is IOEntity) r.SetFlag(BaseEntity.Flags.Reserved8, !hasPower, false);
                                r.SendNetworkUpdate();
                                return;
                            }
                            else if (r is MiningQuarry)
                            {
                                MiningQuarry q = r as MiningQuarry;
                                q.staticType = (MiningQuarry.QuarryType)(int)q.staticType + 1;
                                if ((int)q.staticType > 3) q.staticType = (MiningQuarry.QuarryType)0;
                                q.UpdateStaticDeposit();
                            }
                            else if (r is EngineSwitch)
                            {
                                MiningQuarry miningQuarry = r.GetParentEntity() as MiningQuarry;
                                if (miningQuarry) miningQuarry.EngineSwitch(true);
                            }
                        }
                        else if (serverInput.WasJustPressed(controlButtons[CmdType.HammerRotate]))
                        {
                            if (!rayEntity || !rayEntity.IsValid() || rayDefinition == null || rayDefinition.rotationAmount.y == 0f) return;
                            string effectPath = rayDefinition.deployable != null && rayDefinition.deployable.placeEffect.isValid ? rayDefinition.deployable.placeEffect.resourcePath : StringPool.Get(2598153373);
                            if (serverInput.IsDown(controlButtons[CmdType.HammerRotateDirection])) rayEntity.transform.Rotate(-rayDefinition.rotationAmount);
                            else rayEntity.transform.Rotate(rayDefinition.rotationAmount);
                            if (rayEntity is StabilityEntity)
                            {
                                rayEntity.RefreshEntityLinks();
                                if (!Instance.overrideStabilityBuilding && !(rayEntity as StabilityEntity).grounded) (rayEntity as StabilityEntity).UpdateSurroundingEntities();
                                if (rayEntity is BuildingBlock)
                                {
                                    ConstructionSkin conskin = rayEntity.gameObject.GetComponentInChildren<ConstructionSkin>();
                                    if (conskin) conskin.Refresh(rayEntity as BuildingBlock);
                                    rayEntity.ClientRPC(null, r("ErserfuFxva"));
                                }
                            }

                            DMvmntSnc(rayEntity);
                            Effect.server.Run(effectPath, rayEntity, 0u, Vector3.zero, Vector3.zero, null, false);
                        }
                        else if (serverInput.WasJustPressed(controlButtons[CmdType.HammerTransform]))
                        {
                            if (mvTrgt.entity != null)
                            {
                                mvTrgt =
                            default(Construction.Target);
                                ctvTrgt = false;
                                isPlacing = false;
                                return;
                            }

                            if (!rayEntity || rayEntity is BuildingBlock || rayEntity.FindLinkedEntity<BuildingBlock>()) return;
                            if (rayEntity is BaseMountable && (rayEntity as BaseMountable)._mounted != null) return;
                            mvCnstrctn = PrefabAttribute.server.Find<Construction>(rayEntity.prefabID);
                            if (mvCnstrctn == null)
                            {
                                if (!rayEntity.PrefabName.EndsWith("static.prefab") && !rayEntity.PrefabName.Contains("/deployable/")) return;
                                mvCnstrctn = new Construction();
                                mvCnstrctn.rotationAmount = new Vector3(0, 90f, 0);
                                mvCnstrctn.fullName = rayEntity.PrefabName;
                                mvCnstrctn.maxplaceDistance = rayEntity is MiningQuarry ? 8f : 4f;
                                mvCnstrctn.canRotateBeforePlacement = mvCnstrctn.canRotateAfterPlacement = true;
                            }

                            if (rayEntity is DecayEntity)
                            {
                                DecayUpdate(rayEntity, false, mvCnstrctn.isBuildingPrivilege);
                                DoHammerUpdate(HType.Building, "None");
                            }

                            mvTrgt =
                        default(Construction.Target);
                            mvOffst = Vector3.zero;
                            if (rayEntity.HasParent())
                            {
                                Vector3 position = rayEntity.transform.position;
                                Quaternion rotation = rayEntity.transform.rotation;
                                rayEntity.SetParent(null, 0u);
                                rayEntity.transform.position = position;
                                rayEntity.transform.rotation = rotation;
                                DMvmntSnc(rayEntity);
                            }

                            if (rayEntity.children.Count == 0 || !rayEntity.HasParent()) DMvmntSnc(rayEntity);
                            tkDist = Mathf.Clamp(Vector3.Distance(rayEntity.transform.position, lastRay.origin), mvCnstrctn.maxplaceDistance, mvCnstrctn.maxplaceDistance * 3f);
                            mvTrgt.entity = rayEntity;
                            isPlacing = false;
                            ctvTrgt = true;
                            DoHammerUpdate(HType.Mode, r("Ercbfvgvbavat"));
                        }
                        else if (serverInput.WasJustPressed(controlButtons[CmdType.HammerAuthInfo]) && !serverInput.WasDown(controlButtons[CmdType.HammerTransform]) && (Instance.enableHammerTCInfo || Instance.enableHammerCodelockInfo))
                        {
                            string infoMsg = "";
                            if (Instance.enableHammerTCInfo && rayEntity && rayEntity is BuildingPrivlidge)
                            {
                                bool hasClans = Instance.Clans != null ? true : false;
                                StringBuilder sb = new StringBuilder();
                                rayEntityName = (rayEntity as BaseCombatEntity).repair.itemTarget?.displayName?.english;
                                sb.Append(
                                $">\nBuilding privilege authorized users for <color=#ffa500>{rayEntityName}</color> (<color=#00ffff>{rayEntity.net.ID}</color>)");
                                IPlayer iPlayer = Instance.covalence.Players.FindPlayerById(rayEntity.OwnerID.ToString());
                                if (iPlayer != null)
                                {
                                    sb.Append(
                                    $" | Owner: <color=#ffa500>{iPlayer.Name}</color> (<color=#00ffff>{iPlayer.Id}</color>) | ");
                                    if (iPlayer.IsConnected) sb.AppendLine($"Status: <color=#008000>Online</color>");
                                    else sb.AppendLine($"Status: <color=#ffffff>Offline</color>");
                                }

                                TextTable textTable = new TextTable();
                                textTable.AddColumn("Name");
                                textTable.AddColumn("UserID");
                                if (hasClans) textTable.AddColumn("Clan");
                                textTable.AddColumn("Status");
                                foreach (PlayerNameID nameID in (rayEntity as BuildingPrivlidge).authorizedPlayers.ToList())
                                {
                                    IPlayer authedP = Instance.covalence.Players.FindPlayerById(nameID.userid.ToString());
                                    if (authedP == null) continue;
                                    if (hasClans)
                                    {
                                        string clanTag = "-";
                                        string tag = (string)Instance.Clans?.Call("GetClanOf", Convert.ToUInt64(authedP.Id));
                                        if (tag != null) clanTag = tag;
                                        textTable.AddRow(new string[] {
                                            authedP.Name,
                                            authedP.Id,
                                            clanTag,
                                            ((authedP as RustPlayer).IsConnected ? "<color=#008000>Online</color>": "<color=#ffffff>Offline</color>").ToString()
                                        });
                                    }
                                    else
                                    {
                                        textTable.AddRow(new string[] {
                                            authedP.Name,
                                            authedP.Id,
                                            ((authedP as RustPlayer).IsConnected ? "<color=#008000>Online</color>": "<color=#ffffff>Offline</color>").ToString()
                                        });
                                    }
                                }

                                sb.AppendLine(textTable.ToString());
                                player.ConsoleMessage(sb.ToString());
                                infoMsg += $"<color=#ffa500>TC</color> (<color=#00ffff>{rayEntity.net.ID}</color>) authorized players sent to console";
                            }

                            if (Instance.enableHammerCodelockInfo && rayEntity && rayEntity.HasSlot(BaseEntity.Slot.Lock) && rayEntity.GetSlot(BaseEntity.Slot.Lock) is CodeLock)
                            {
                                bool hasClans = Instance.Clans != null ? true : false;
                                CodeLock codeLock = (CodeLock)rayEntity.GetSlot(BaseEntity.Slot.Lock);
                                StringBuilder sb = new StringBuilder();
                                rayEntityName = (rayEntity as BaseCombatEntity).repair.itemTarget?.displayName.english;
                                sb.Append(
                                $">\nCodeLock authorized users attached to <color=#ffa500>{rayEntityName}</color> (<color=#00ffff>{rayEntity.net.ID}</color>)");
                                IPlayer iPlayer = Instance.covalence.Players.FindPlayerById(rayEntity.OwnerID.ToString());
                                if (iPlayer != null)
                                {
                                    sb.Append(
                                    $" | Owner: <color=#ffa500>{iPlayer.Name}</color> (<color=#00ffff>{iPlayer.Id}</color>) | ");
                                    if (iPlayer.IsConnected) sb.AppendLine($"Status: <color=#008000>Online</color>");
                                    else sb.AppendLine($"Status: <color=#ffffff>Offline</color>");
                                }

                                string code = codeLock.hasCode ? $"<color=#00ffff>{codeLock.code}</color>" : "<color=#00ffff>Not set</color>";
                                string guest = codeLock.hasGuestCode ? $"<color=#00ffff>{codeLock.guestCode}</color>" : "<color=#00ffff>Not set</color>";
                                sb.AppendLine($"Lock code:  {code} | Guest code: {guest}");
                                if (codeLock.whitelistPlayers != null && codeLock.whitelistPlayers.Count > 0)
                                {
                                    sb.AppendLine("Whitelisted:");
                                    TextTable textTable = new TextTable();
                                    textTable.AddColumn("Name");
                                    textTable.AddColumn("UserID");
                                    if (hasClans) textTable.AddColumn("Clan");
                                    textTable.AddColumn("Status");

                                    foreach (ulong userID in codeLock.whitelistPlayers.ToList())
                                    {
                                        IPlayer authedP = Instance.covalence.Players.FindPlayerById(userID.ToString());
                                        if (authedP == null) continue;
                                        if (hasClans)
                                        {
                                            string clanTag = (string)Instance.Clans?.Call("GetClanOf", Convert.ToUInt64(authedP.Id));
                                            if (string.IsNullOrEmpty(clanTag))
                                                clanTag = "-";

                                            textTable.AddRow(new string[] {
                                                authedP.Name,
                                                authedP.Id,
                                                clanTag,
                                                (authedP.IsConnected ? "<color=#008000>Online</color>": "<color=#ffffff>Offline</color>").ToString()
                                            });
                                        }
                                        else
                                        {
                                            textTable.AddRow(new string[] {
                                                authedP.Name,
                                                authedP.Id,
                                                (authedP.IsConnected ? "<color=#008000>Online</color>": "<color=#ffffff>Offline</color>").ToString()
                                            });
                                        }
                                    }

                                    sb.AppendLine(textTable.ToString());
                                }
                                else
                                {
                                    sb.AppendLine("Whitelisted: <color=#ffffff>None</color>");
                                }

                                if (codeLock.guestPlayers != null && codeLock.guestPlayers.Count > 0)
                                {
                                    sb.AppendLine("Guests:");
                                    TextTable textTable = new TextTable();
                                    textTable.AddColumn("Name");
                                    textTable.AddColumn("UserID");
                                    if (hasClans) textTable.AddColumn("Clan");
                                    textTable.AddColumn("Status");
                                    foreach (ulong userID in codeLock.guestPlayers.ToList())
                                    {
                                        IPlayer authedP = Instance.covalence.Players.FindPlayerById(userID.ToString());
                                        if (authedP == null) continue;
                                        if (hasClans)
                                        {
                                            string clanTag = (string)Instance.Clans?.Call("GetClanOf", Convert.ToUInt64(authedP.Id));
                                            if (clanTag.Length == 0) clanTag = "-";
                                            textTable.AddRow(new string[] {
                                                authedP.Name,
                                                authedP.Id,
                                                clanTag,
                                                (authedP.IsConnected ? "<color=#008000>Online</color>": "<color=#ffffff>Offline</color>").ToString()
                                            });
                                        }
                                        else
                                        {
                                            textTable.AddRow(new string[] {
                                                authedP.Name,
                                                authedP.Id,
                                                (authedP.IsConnected ? "<color=#008000>Online</color>": "<color=#ffffff>Offline</color>").ToString()
                                            });
                                        }
                                    }

                                    sb.AppendLine(textTable.ToString());
                                }
                                else
                                {
                                    sb.AppendLine("Guests: <color=#ffffff>None</color>");
                                }

                                player.ConsoleMessage(sb.ToString());
                                infoMsg += (infoMsg.Length > 0 ? "\n" : "") + $"<color=#ffa500>{(rayEntityName == " Tool Cupboard " ? " TC " : rayEntityName)}</color> (<color=#00ffff>{rayEntity.net.ID}</color>) CodeLock info sent to console";
                            }
                            if (infoMsg.Length > 0) player.ChatMessage(Instance.ChatMsg(infoMsg));
                        }
                    }
                }
                else if (sRmvr)
                {
                    if (serverInput.WasJustPressed(controlButtons[CmdType.RemoverRemove]))
                    {
                        if (!serverInput.IsDown(controlButtons[CmdType.RemoverHoldForAll])) DoRm();
                        else if (serverInput.IsDown(controlButtons[CmdType.RemoverHoldForAll])) DoRm(true);
                    }

                    rayEntity = null;
                    rayDefinition = null;
                }
            }

            private void FndTrrnPlcmnt(ref Construction.Target t, Construction c, float maxDistance, bool isQuarry = false)
            {
                int layer = 27328769;
                if (isQuarry) layer = 10551297;
                RaycastHit[] hits = Physics.RaycastAll(t.ray, maxDistance, layer);
                if (hits.Length > 1)
                {
                    GamePhysics.Sort(hits);
                    for (int i = 0; i < hits.Length; i++)
                        if (hits[i].collider.transform.root != t.entity.transform.root)
                        {
                            t.position = t.ray.origin + t.ray.direction * hits[i].distance;
                            t.normal = hits[i].normal;
                            t.rotation = Vector3.zero;
                            t.onTerrain = true;
                            t.valid = true;
                            if (!isQuarry) mvTrgtSnp = hits[i].GetEntity();
                            return;
                        }
                }

                t.position = t.ray.origin + t.ray.direction * maxDistance;
                t.normal = Vector3.up;
                t.rotation = Vector3.zero;
                t.onTerrain = true;
                t.valid = false;
                mvTrgtSnp = null;
            }

            public void SetBlockPrefab(uint p)
            {
                construction = PrefabAttribute.server.Find<Construction>(p);
                rttnOffst = Vector3.zero;
                lstPrfb = p;
                DoPlannerUpdate(PType.Mode, $"{construction.info.name.english} ({((BuildingGrade.Enum)dfltGrd).ToString()})");
                lastPlacement = null;
                lastSocketForce = true;
            }

            public void OnDestroy()
            {
                CuiHelper.DestroyUi(player, "ut.ioslotchooser");
                DoCrosshair(string.Empty, true);
                DoWarning(string.Empty, true);
                foreach (Item item in player.inventory.AllItems().Where(x => x.IsValid()).ToList())
                    if (item.skin == Convert.ToUInt64(Instance.playerTools[0][2]) || item.skin == Convert.ToUInt64(Instance.playerTools[1][2]) || item.skin == Convert.ToUInt64(Instance.playerTools[2][2]))
                    {
                        if (removeItemsOnDeactivation)
                        {
                            item.RemoveFromContainer();
                            item.RemoveFromWorld();
                            item.Remove(0f);
                        }
                        else
                        {
                            item.skin = 0uL;
                            item.GetHeldEntity().skinID = 0uL;
                            item.name = string.Empty;
                            item.MarkDirty();
                        }
                    }

                DestroyInfo();
                Destroy(this);
            }

            private void DoRm(bool remAl = false)
            {
                if (!rayEntity || rayEntity is BasePlayer && !(rayEntity is NPCPlayer) || !Instance.removeToolObjects && !rayDefinition) return;
                if (rayEntity.IsValid())
                {
                    if (rayEntity is BuildingBlock)
                    {
                        if (Instance.enableFullRemove && remAl)
                        {
                            CollRm(rayEntity);
                            return;
                        }
                        else
                        {
                            if (Instance.effectRemoveBlocksOn) Effect.server.Run(Instance.effectRemoveBlocks, rayEntity, 0u, Vector3.zero, Vector3.zero, null, false);
                            rayEntity.Kill(BaseNetworkable.DestroyMode.Gib);
                            rayEntity = null;
                            rayDefinition = null;
                            return;
                        }
                    }
                    else
                    {
                        if (rayEntity is OreResourceEntity)
                        {
                            (rayEntity as OreResourceEntity).CleanupBonus();
                        }
                        else if (rayEntity is BaseNpc || rayEntity is NPCPlayer || rayEntity is BradleyAPC || rayEntity is BaseHelicopter)
                        {
                            (rayEntity as BaseCombatEntity).DieInstantly();
                        }
                        else
                        {
                            if (!Instance.entRemoval.Contains(rayEntity.transform.root)) Instance.entRemoval.Add(rayEntity.transform.root);
                            rayEntity.Kill(BaseNetworkable.DestroyMode.Gib);
                        }

                        rayEntity = null;
                        rayDefinition = null;
                    }
                }
                else
                {
                    GameManager.Destroy(rayEntity.gameObject, 0f);
                    rayEntity = null;
                    rayDefinition = null;
                }
            }

            private void CollRm(BaseEntity srcntt)
            {
                BuildingBlock bldngBlck = srcntt.GetComponent<BuildingBlock>();
                if (bldngBlck)
                {
                    BuildingManager.Building building = BuildingManager.server.GetBuilding(bldngBlck.buildingID);
                    ServerMgr.Instance.StartCoroutine(DlyRm(building.buildingBlocks.ToList(), building.decayEntities.ToList(), building.buildingPrivileges.ToList()));
                }
            }

            private WaitForEndOfFrame wait = new WaitForEndOfFrame();

            private IEnumerator DlyRm(List<BuildingBlock> bLst, List<DecayEntity> dLst, List<BuildingPrivlidge> pLst)
            {
                BaseNetworkable.DestroyMode mode = Instance.showGibsOnRemove ? BaseNetworkable.DestroyMode.Gib : BaseNetworkable.DestroyMode.None;
                for (int i = 0; i < pLst.Count; i++)
                    if (!pLst[i].IsDestroyed)
                    {
                        if (pLst[i] == rayEntity)
                        {
                            rayEntity = null;
                            rayDefinition = null;
                        }

                        pLst[i].Kill(mode);
                        yield
                        return wait;
                    }

                for (int i = 0; i < dLst.Count; i++)
                    if (!dLst[i].IsDestroyed)
                    {
                        if (dLst[i] == rayEntity)
                        {
                            rayEntity = null;
                            rayDefinition = null;
                        }

                        dLst[i].Kill(mode);
                        yield
                        return wait;
                    }

                for (int i = 0; i < bLst.Count; i++)
                    if (!bLst[i].IsDestroyed)
                    {
                        if (bLst[i] == rayEntity)
                        {
                            rayEntity = null;
                            rayDefinition = null;
                        }

                        bLst[i].Kill(mode);
                        yield
                        return wait;
                    }

                yield
                break;
            }

            private void DoPlacement()
            {
                ChkQrr(construction);
                Deployable dplybl = plnnr.GetDeployable();
                GameObject gameObject = DoPlaG(target, construction);
                if (gameObject != null)
                {
                    Interface.CallHook(r("BaRagvglOhvyg"), new object[] {
                        plnnr,
                        gameObject
                    });
                    if (dplybl != null)
                    {
                        if (dplybl.placeEffect.isValid)
                        {
                            if (target.entity && target.socket) Effect.server.Run(dplybl.placeEffect.resourcePath, target.entity.transform.TransformPoint(target.socket.worldPosition), target.entity.transform.up, null, false);
                            else Effect.server.Run(dplybl.placeEffect.resourcePath, target.position, target.normal, null, false);
                        }

                        BaseEntity bsntt = gameObject.ToBaseEntity();
                        if (!(bsntt is MiningQuarry) && !(bsntt is Elevator) && !(target.entity is BuildingBlock) && target.entity != null)
                        {
                            //bsntt.transform.position = target.entity.transform.worldToLocalMatrix.MultiplyPoint3x4(target.position);
                            //bsntt.transform.rotation = Quaternion.Inverse(target.entity.transform.rotation) * bsntt.transform.rotation;
                            //bsntt.SetParent(target.entity, 0u);
                        }

                        if (dplybl.wantsInstanceData && ctvtmLnk.instanceData != null) (bsntt as IInstanceDataReceiver).ReceiveInstanceData(ctvtmLnk.instanceData);
                        if (dplybl.copyInventoryFromItem)
                        {
                            StorageContainer component2 = bsntt.GetComponent<StorageContainer>();
                            if (component2)
                            {
                                component2.ReceiveInventoryFromItem(ctvtmLnk);
                                ctvtmLnk.OnVirginSpawn();
                                ctvtmLnk.MarkDirty();
                            }
                        }
                        if (bsntt is SleepingBag)
                            (bsntt as SleepingBag).deployerUserID = player.userID;
                                                
                        bsntt.OnDeployed(bsntt.GetParentEntity(), player, plnnr.GetItem());

                        if (Instance.setDeployableOwner)
                            bsntt.OwnerID = player.userID;
                    }
                }
            }

            private void CheckPlacement(ref Construction.Target t, Construction c)
            {
                t.valid = false;
                if (c.socketHandle != null)
                {
                    Vector3 worldPosition = c.socketHandle.worldPosition;
                    Vector3 a = t.ray.origin + t.ray.direction * c.maxplaceDistance;
                    Vector3 a2 = a - worldPosition;
                    Vector3 oldDir = t.ray.direction;
                    t.ray.direction = (a2 - t.ray.origin).normalized;
                }

                List<BaseEntity> list = Pool.GetList<BaseEntity>();
                float num = 3.40282347E+38f;
                Vis.Entities<BaseEntity>(t.ray.origin, c.maxplaceDistance * 2f, list, 18874625, QueryTriggerInteraction.Collide);
                foreach (BaseEntity current in list)
                {
                    Construction con = PrefabAttribute.server.Find<Construction>(current.prefabID);
                    if (!(con == null))
                    {
                        Socket_Base[] allSockets = con.allSockets;
                        for (int i = 0; i < allSockets.Length; i++)
                        {
                            Socket_Base socket_Base = allSockets[i];
                            if (socket_Base.female && !socket_Base.femaleDummy)
                            {
                                RaycastHit raycastHit;
                                if (socket_Base.GetSelectBounds(current.transform.position, current.transform.rotation).Trace(t.ray, out raycastHit, float.PositiveInfinity)) if (raycastHit.distance >= 1f) if (raycastHit.distance <= num) if (!current.IsOccupied(socket_Base))
                                            {
                                                Construction.Target trgt2 =
                                            default(Construction.Target);
                                                trgt2.socket = socket_Base;
                                                trgt2.entity = current;
                                                trgt2.ray = t.ray;
                                                trgt2.valid = true;
                                                trgt2.player = player;
                                                trgt2.rotation = rttnOffst;
                                                if (c.HasMaleSockets(trgt2))
                                                {
                                                    t = trgt2;
                                                    num = raycastHit.distance;
                                                }
                                            }
                            }
                        }
                    }
                }

                if (t.valid)
                {
                    Pool.FreeList<BaseEntity>(ref list);
                    return;
                }

                if (c.deployable == null && list.Count > 0)
                {
                    list.Clear();
                    Vis.Entities<BaseEntity>(t.ray.origin, 3f, list, 2097152, QueryTriggerInteraction.Ignore);
                    if (list.Count > 0)
                    {
                        Pool.FreeList<BaseEntity>(ref list);
                        return;
                    }
                }

                if (GamePhysics.Trace(t.ray, 0f, out rayHit, c.maxplaceDistance, 27328769, QueryTriggerInteraction.Ignore))
                {
                    t.position = t.ray.origin + t.ray.direction * rayHit.distance;
                    t.rotation = rttnOffst;
                    t.normal = rayHit.normal;
                    t.onTerrain = true;
                    t.valid = true;
                    t.entity = rayHit.GetEntity();
                }
                else
                {
                    t.position = t.ray.origin + t.ray.direction * c.maxplaceDistance;
                    t.rotation = rttnOffst;
                    t.normal = Vector3.up;
                    if (c.hierachyName.Contains(r("sbhaqngvba")))
                    {
                        t.valid = true;
                        t.onTerrain = true;
                    }
                    else
                    {
                        t.valid = false;
                        t.onTerrain = false;
                    }
                }

                Pool.FreeList<BaseEntity>(ref list);
            }

            private void ChkQrr(Construction c)
            {
                if (StringPool.Get(672916883).Equals(c.fullName))
                {
                    BaseEntity crt = GameManager.server.CreateEntity(StringPool.Get(2955484243), Vector3.zero, Quaternion.identity, true);
                    crt.transform.position = rayHit.point;
                    crt.Spawn();
                    CheckPlacement(ref target, construction);
                }

                if (StringPool.Get(1599225199).Equals(c.fullName))
                {
                    BaseEntity crt = GameManager.server.CreateEntity(StringPool.Get(1917257452), Vector3.zero, Quaternion.identity, true);
                    crt.transform.position = rayHit.point;
                    crt.Spawn();
                    CheckPlacement(ref target, construction);
                }
            }

            public GameObject DoPlaG(Construction.Target p, Construction component)
            {                
                BaseEntity bsntt = CrtCnstrctn(p, component);
                if (!bsntt)
                {
                    return null;
                }
                float num = 1f;
                bsntt.skinID = ctvtmLnk.skin;
                bsntt.gameObject.AwakeFromInstantiate();
                BuildingBlock bBl = bsntt as BuildingBlock;
                if (bBl)
                {
                    bBl.blockDefinition = PrefabAttribute.server.Find<Construction>(bBl.prefabID);
                    if (!bBl.blockDefinition) return null;
                    bBl.SetGrade((BuildingGrade.Enum)dfltGrd);
                    float num2 = bBl.currentGrade.maxHealth;
                }

                BaseCombatEntity bsCmbtntt = bsntt as BaseCombatEntity;
                if (bsCmbtntt)
                {
                    float num2 = !(bBl != null) ? bsCmbtntt.startHealth : bBl.currentGrade.maxHealth;
                    bsCmbtntt.ResetLifeStateOnSpawn = false;
                    bsCmbtntt.InitializeHealth(num2 * num, num2);
                }

                bsntt.OwnerID = player.userID;

                StabilityEntity stabilityEntity = bsntt as StabilityEntity;
                bool setGrounded = false;
                if (stabilityEntity && Instance.overrideStabilityBuilding)
                {
                    stabilityEntity.grounded = true;
                    setGrounded = true;
                }

                if (Instance.disableGroundMissingChecks && !bBl)
                {
                    Destroy(bsntt.GetComponent<DestroyOnGroundMissing>());
                    Destroy(bsntt.GetComponent<GroundWatch>());
                }

                bsntt.Spawn();
                if (bBl && Instance.effectPlacingBlocksOn) Effect.server.Run(Instance.effectPlacingBlocks, bsntt, 0u, Vector3.zero, Vector3.zero);
                if (stabilityEntity && !setGrounded) stabilityEntity.UpdateSurroundingEntities();
                return bsntt.gameObject;
            }

            private BaseEntity CrtCnstrctn(Construction.Target target, Construction component)
            {
                string path = component.fullName;
                if (component.fullName.Equals(StringPool.Get(672916883))) path = StringPool.Get(3424003500);
                if (component.fullName.Equals(StringPool.Get(1599225199))) path = StringPool.Get(3449840583);
               
                GameObject gameObject = GameManager.server.CreatePrefab(path, Vector3.zero, Quaternion.identity, false);
                bool flag = UpdtPlcmnt(gameObject.transform, component, ref target);
                BaseEntity bsntt = gameObject.ToBaseEntity();

                Elevator elevator = bsntt as Elevator;
                if (elevator && rayEntity is Elevator)
                {
                    List<EntityLink> list = rayEntity.FindLink("elevator/sockets/elevator-female")?.connections;
                    if (list.Count > 0 && (list[0].owner as Elevator) != null)
                    {
                        player.ChatMessage("You can only stack elevators on the top level");
                        return null;
                    }

                    elevator.transform.position = rayEntity.transform.position + (Vector3.up * 3f);
                    elevator.transform.rotation = rayEntity.transform.rotation;

                    elevator.GetEntityLinks(true);
                    flag = true;
                }

                if (!flag)
                {
                    if (bsntt.IsValid()) bsntt.Kill(BaseNetworkable.DestroyMode.None);
                    else GameManager.Destroy(gameObject, 0f);
                    return null;
                }

                DecayEntity dcyEntt = bsntt as DecayEntity;
                if (dcyEntt) dcyEntt.AttachToBuilding(target.entity as DecayEntity);
                return bsntt;
            }

            private Construction.Placement CheckPlacement(Construction.Target t, Construction c)
            {
                List<Socket_Base> list = Pool.GetList<Socket_Base>();
                Construction.Placement plcmnt = null;
                if (c.allSockets == null || c.allSockets.Length == 0) return plcmnt;
                c.FindMaleSockets(t, list);
                foreach (Socket_Base current in list)
                    if (!(t.entity != null) || !(t.socket != null) || !t.entity.IsOccupied(t.socket)) plcmnt = current.DoPlacement(t);
                Pool.FreeList<Socket_Base>(ref list);
                return plcmnt;
            }

            private bool UpdtPlcmnt(Transform tn, Construction common, ref Construction.Target target)
            {
                if (!target.valid) 
                    return false;

                List<Socket_Base> list = Pool.GetList<Socket_Base>();
                common.canBypassBuildingPermission = true;
                common.FindMaleSockets(target, list);
                Construction.lastPlacementError = string.Empty;
                //Regex _errOrr = new Regex(@"Not enough space|not in terrain|AngleCheck|Sphere Test|IsInArea|cupboard|Invalid angle", RegexOptions.Compiled);

                if (list.Count == 0)
                {
                    Pool.FreeList<Socket_Base>(ref list);
                    return true;
                }
                foreach (Socket_Base current in list)
                {
                    Construction.Placement plcmnt = null;

                    if (!(target.entity != null) || !(target.socket != null) || !target.entity.IsOccupied(target.socket))
                    {
                        if (plcmnt == null) 
                            plcmnt = current.DoPlacement(target);

                        if (plcmnt != null)
                        {
                            for (int i = 0; i < current.socketMods.Length; i++)
                            {
                                current.socketMods[i].ModifyPlacement(plcmnt);
                            }

                            tn.position = plcmnt.position;
                            tn.rotation = plcmnt.rotation;

                            DeployVolume[] volumes = PrefabAttribute.server.FindAll<DeployVolume>(common.prefabID);
                            if (DeployVolume.Check(plcmnt.position, plcmnt.rotation, volumes, -1))
                            {
                                if (StringPool.Get(672916883).Contains(common.fullName) || StringPool.Get(1599225199).Contains(common.fullName))
                                {
                                    tn.position = plcmnt.position;
                                    tn.rotation = plcmnt.rotation;
                                    Pool.FreeList<Socket_Base>(ref list);
                                    return true;
                                }
                            }
                            else if (BuildingProximity.Check(target.player, common, plcmnt.position, plcmnt.rotation))
                            {
                                tn.position = plcmnt.position;
                                tn.rotation = plcmnt.rotation;
                            }
                            else if (common.isBuildingPrivilege && !target.player.CanPlaceBuildingPrivilege(plcmnt.position, plcmnt.rotation, common.bounds))
                            {
                                tn.position = plcmnt.position;
                                tn.rotation = plcmnt.rotation;
                            }
                            else
                            {
                                tn.position = plcmnt.position;
                                tn.rotation = plcmnt.rotation;
                                Pool.FreeList<Socket_Base>(ref list);
                                return true;
                            }
                        }
                    }
                }

                Pool.FreeList<Socket_Base>(ref list);

                //if (_errOrr.IsMatch(Construction.lastPlacementError))  
                if (tn.position != Vector3.zero)
                    return true;
                
                return false;
            }

            public void SendEffectTo(uint id, BaseEntity ent, BasePlayer player)
            {
                Effect effect = new Effect();
                effect.Init(Effect.Type.Generic, ent.transform.position, player.transform.forward, null);
                effect.pooledString = StringPool.Get(id);
                EffectNetwork.Send(effect, player.net.connection);
            }

            private void DestroyInfo(UType uType = UType.All)
            {
                CuiHelper.DestroyUi(player, "ut.ioslotchooser");
                CuiHelper.DestroyUi(player, r("HgPebffUnveHV"));
                if (uType == UType.All)
                {
                    CuiHelper.DestroyUi(player, UType.PlannerUi.ToString());
                    CuiHelper.DestroyUi(player, UType.RemoverUi.ToString());
                    CuiHelper.DestroyUi(player, UType.HammerUi.ToString());
                    plannerInfoStatus = false;
                    removerInfoStatus = false;
                    hammerInfoStatus = false;
                }
                else
                {
                    CuiHelper.DestroyUi(player, uType.ToString());
                    switch (uType)
                    {
                        case UType.PlannerUi:
                            plannerInfoStatus = false;
                            break;
                        case UType.RemoverUi:
                            removerInfoStatus = false;
                            break;
                        case UType.HammerUi:
                            hammerInfoStatus = false;
                            break;
                        default:
                            break;
                    }
                }
            }

            private void DoPlannerInfo()
            {
                if (!Instance.showPlannerInfo) return;
                string panelName = UType.PlannerUi.ToString();
                DestroyInfo(UType.PlannerUi);
                CuiElementContainer mainContainer = new CuiElementContainer() {
                    {
                        new CuiPanel {
                            Image = {
                                Color = "0 0 0 0"
                            },
                            RectTransform = {
                                AnchorMin = $"{panelPosX.ToString()} {panelPosY.ToString()}",
                                AnchorMax = $"{(panelPosX + 0.3f).ToString()} {(panelPosY + 0.15f).ToString()}"
                            }
                        },
                        new CuiElement().Parent = "Under",
                        panelName
                    }
                };
                CuiHelper.AddUi(player, mainContainer);
                plannerInfoStatus = true;
                DoPlannerUpdate(PType.Mode);
                DoPlannerUpdate(PType.ToSocket);
                DoPlannerUpdate(PType.PosRot);
                DoPlannerUpdate(PType.ConnectTo);
            }

            private void DoPlannerUpdate(PType pType, string infoMsg = " - ")
            {
                if (!isPlanner) return;
                if (!plannerInfoStatus) DoPlannerInfo();
                int maxRows = Enum.GetValues(typeof(PType)).Length;
                int rowNumber = (int)pType;
                string fieldName = pType.ToString();
                if (rowNumber == 0)
                {
                    if (sTpDplybl) fieldName = "Place";
                    else fieldName = "Build";
                }

                string mainPanel = UType.PlannerUi.ToString() + fieldName;
                CuiHelper.DestroyUi(player, mainPanel);
                float value = 1 / (float)maxRows;
                float positionMin = 1 - value * rowNumber;
                float positionMax = 2 - (1 - value * (1 - rowNumber));
                CuiElementContainer container = new CuiElementContainer() {
                    {
                        new CuiPanel {
                            Image = {
                                Color = "0 0 0 0"
                            },
                            RectTransform = {
                                AnchorMin = "0 " + positionMin.ToString("0.####"),
                                AnchorMax = $"1 " + positionMax.ToString("0.####")
                            },
                        },
                        new CuiElement().Parent = UType.PlannerUi.ToString(),
                        mainPanel
                    }
                };
                CuiElement innerLine = new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = mainPanel,
                    Components = {
                        new CuiRawImageComponent {
                            Color = "0 0 0 1",
                            Sprite = r("nffrgf/pbagrag/hv/qrirybcre/qrirybczragfxva/qrigno-abezny.cat"),
                            Material = r("nffrgf/pbagrag/zngrevnyf/vgrzzngrevny.zng")
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0 0",
                            AnchorMax = "0.9 0.9"
                        }
                    }
                };
                container.Add(innerLine);
                CuiElement innerLineText1 = new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = innerLine.Name,
                    Components = {
                        new CuiTextComponent {
                            Color = cuiFontColor,
                            Text = infoMsg,
                            Font = fontType,
                            FontSize = cuiFontSize,
                            Align = TextAnchor.MiddleLeft
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0.25 0.1",
                            AnchorMax = "1 1"
                        }
                    }
                };
                container.Add(innerLineText1);
                CuiElement innerLineText2 = new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = innerLine.Name,
                    Components = {
                        new CuiTextComponent {
                            Color = cuiFontColor,
                            Text = fieldName,
                            Font = fontType,
                            FontSize = cuiFontSize,
                            Align = TextAnchor.MiddleLeft
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0.025 0.1",
                            AnchorMax = "0.3 1"
                        }
                    }
                };
                container.Add(innerLineText2);
                CuiHelper.AddUi(player, container);
            }

            private void DoRemoverInfo()
            {
                if (!Instance.showRemoverInfo) return;
                string panelName = UType.RemoverUi.ToString();
                DestroyInfo(UType.RemoverUi);
                CuiElementContainer mainContainer = new CuiElementContainer() {
                    {
                        new CuiPanel {
                            Image = {
                                Color = "0 0 0 0"
                            },
                            RectTransform = {
                                AnchorMin = $"{panelPosX.ToString()} {panelPosY.ToString()}",
                                AnchorMax = $"{(panelPosX + 0.3f).ToString()} {(panelPosY + 0.115f).ToString()}"
                            }
                        },
                        new CuiElement().Parent = "Under",
                        panelName
                    }
                };
                CuiHelper.AddUi(player, mainContainer);
                removerInfoStatus = true;
                DoRemoverUpdate(RType.Remove);
                DoRemoverUpdate(RType.Mode, "Single");
                DoRemoverUpdate(RType.Owner);
            }

            private void DoRemoverUpdate(RType rType, string infoMsg = " - ", bool altMode = false)
            {
                if (!sRmvr) return;
                if (!removerInfoStatus) DoRemoverInfo();
                int maxRows = Enum.GetValues(typeof(RType)).Length;
                int rowNumber = (int)rType;
                string fieldName = rType.ToString();
                string mainPanel = UType.RemoverUi.ToString() + fieldName;
                if (infoMsg.Contains("Building")) fieldName = "<color=#ff0000>Mode</color>";
                CuiHelper.DestroyUi(player, mainPanel);
                float value = 1 / (float)maxRows;
                float positionMin = 1 - value * rowNumber;
                float positionMax = 2 - (1 - value * (1 - rowNumber));
                CuiElementContainer container = new CuiElementContainer() {
                    {
                        new CuiPanel {
                            Image = {
                                Color = "0 0 0 0"
                            },
                            RectTransform = {
                                AnchorMin = "0 " + positionMin.ToString("0.####"),
                                AnchorMax = $"1 " + positionMax.ToString("0.####")
                            },
                        },
                        new CuiElement().Parent = UType.RemoverUi.ToString(),
                        mainPanel
                    }
                };
                CuiElement innerLine = new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = mainPanel,
                    Components = {
                        new CuiRawImageComponent {
                            Color = "0 0 0 1",
                            Sprite = r("nffrgf/pbagrag/hv/qrirybcre/qrirybczragfxva/qrigno-abezny.cat"),
                            Material = r("nffrgf/pbagrag/zngrevnyf/vgrzzngrevny.zng")
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0 0",
                            AnchorMax = "0.9 0.9"
                        }
                    }
                };
                container.Add(innerLine);
                CuiElement innerLineText1 = new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = innerLine.Name,
                    Components = {
                        new CuiTextComponent {
                            Color = cuiFontColor,
                            Text = infoMsg,
                            Font = fontType,
                            FontSize = cuiFontSize,
                            Align = TextAnchor.MiddleLeft
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0.25 0.1",
                            AnchorMax = "1 1"
                        }
                    }
                };
                container.Add(innerLineText1);
                CuiElement innerLineText2 = new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = innerLine.Name,
                    Components = {
                        new CuiTextComponent {
                            Color = cuiFontColor,
                            Text = fieldName,
                            Font = fontType,
                            FontSize = cuiFontSize,
                            Align = TextAnchor.MiddleLeft
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0.025 0.1",
                            AnchorMax = "0.3 1"
                        }
                    }
                };
                container.Add(innerLineText2);
                CuiHelper.AddUi(player, container);
            }

            private void DoHammerInfo()
            {
                if (!Instance.showHammerInfo) return;
                string panelName = UType.HammerUi.ToString();
                DestroyInfo(UType.HammerUi);
                CuiElementContainer mainContainer = new CuiElementContainer() {
                    {
                        new CuiPanel {
                            Image = {
                                Color = "0 0 0 0"
                            },
                            RectTransform = {
                                AnchorMin = $"{panelPosX.ToString()} {panelPosY.ToString()}",
                                AnchorMax = $"{(panelPosX + 0.3f).ToString()} {(panelPosY + 0.19f).ToString()}"
                            }
                        },
                        new CuiElement().Parent = "Under",
                        panelName
                    }
                };
                CuiHelper.AddUi(player, mainContainer);
                hammerInfoStatus = true;
                DoHammerUpdate(HType.Target);
                DoHammerUpdate(HType.Building);
                DoHammerUpdate(HType.Mode);
                DoHammerUpdate(HType.PosRot);
                DoHammerUpdate(HType.Owner);
                DoHammerUpdate(HType.SteamID);
            }

            private void DoHammerUpdate(HType hType, string infoMsg = " - ")
            {
                if (!isHammering) return;
                if (!hammerInfoStatus) DoHammerInfo();
                int maxRows = Enum.GetValues(typeof(HType)).Length;
                int rowNumber = (int)hType;
                string fieldName = hType.ToString();
                string mainPanel = UType.HammerUi.ToString() + fieldName;
                CuiHelper.DestroyUi(player, mainPanel);
                float value = 1 / (float)maxRows;
                float positionMin = 1 - value * rowNumber;
                float positionMax = 2 - (1 - value * (1 - rowNumber));
                CuiElementContainer container = new CuiElementContainer() {
                    {
                        new CuiPanel {
                            Image = {
                                Color = "0 0 0 0"
                            },
                            RectTransform = {
                                AnchorMin = "0 " + positionMin.ToString("0.####"),
                                AnchorMax = $"1 " + positionMax.ToString("0.####")
                            },
                        },
                        new CuiElement().Parent = UType.HammerUi.ToString(),
                        mainPanel
                    }
                };
                CuiElement innerLine = new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = mainPanel,
                    Components = {
                        new CuiRawImageComponent {
                            Color = "0 0 0 1",
                            Sprite = r("nffrgf/pbagrag/hv/qrirybcre/qrirybczragfxva/qrigno-abezny.cat"),
                            Material = r("nffrgf/pbagrag/zngrevnyf/vgrzzngrevny.zng")
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0 0",
                            AnchorMax = "0.9 0.9"
                        }
                    }
                };
                container.Add(innerLine);
                CuiElement innerLineText1 = new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = innerLine.Name,
                    Components = {
                        new CuiTextComponent {
                            Color = cuiFontColor,
                            Text = infoMsg,
                            Font = fontType,
                            FontSize = cuiFontSize,
                            Align = TextAnchor.MiddleLeft
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0.25 0.1",
                            AnchorMax = "1 1"
                        }
                    }
                };
                container.Add(innerLineText1);
                CuiElement innerLineText2 = new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = innerLine.Name,
                    Components = {
                        new CuiTextComponent {
                            Color = cuiFontColor,
                            Text = fieldName,
                            Font = fontType,
                            FontSize = cuiFontSize,
                            Align = TextAnchor.MiddleLeft
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0.025 0.1",
                            AnchorMax = "0.3 1"
                        }
                    }
                };
                container.Add(innerLineText2);
                CuiHelper.AddUi(player, container);
            }

            private void BldMnUI(float factor)
            {
                CuiElementContainer element = new CuiElementContainer();
                string color = "0 0 0 0";
                string mainName = element.Add(
                new CuiPanel
                {
                    Image = {
                        Color = "0 0 0 0"
                    },
                    RectTransform = {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    },
                    CursorEnabled = true
                },
                "Overlay", r("OhvyqZrahHV"));
                element.Add(
                new CuiButton
                {
                    Button = {
                        Close = mainName,
                        Color = color
                    },
                    RectTransform = {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    },
                    Text = {
                        Text = string.Empty
                    }
                },
                mainName);
                Vector2 mC = new Vector2(0.5f, 0.5f);
                Vector2 mS = new Vector2(0.3425f, 0.475f);

                for (int i = 0; i < 20; i++)
                {
                    float scaled = 1f;
                    if ((i > 0 && i < 6) || (i > 8 && i < 11) || (i > 12 && i < 15))
                        scaled = 0.75f;

                    Vector2 center = RotateByRadians(mC, mS, index2Degrees[i] * Mathf.Deg2Rad, factor);
                    element.Add(BuildIconUI(mainName, center, r("nffrgf/vpbaf/pvepyr_tenqvrag.cat"), -0.040f * scaled, 0.040f * scaled, "1 1 1 1", factor, false));
                    element.Add(BuildIconUI(mainName, center, r("nffrgf/vpbaf/pvepyr_tenqvrag.cat"), -0.040f * scaled, 0.040f * scaled, "1 1 1 1", factor, false));
                    element.Add(BuildRawIconUI(mainName, center, Instance.prefabIdToImage[Instance.constructionIds[i]], -0.02f * scaled, 0.02f * scaled, "0.2 0.5 0.8 0.5", factor, true));
                    element.Add(BuildButtonUI(mainName, Vector2.MoveTowards(center, mC, 0.06f), i, -0.020f * scaled, 0.020f * scaled, color, factor), mainName);
                    element.Add(BuildButtonUI(mainName, Vector2.MoveTowards(center, mC, 0.03f), i, -0.025f * scaled, 0.025f * scaled, color, factor), mainName);
                    element.Add(BuildButtonUI(mainName, center, i, -0.030f * scaled, 0.030f * scaled, color, factor), mainName);
                    element.Add(BuildButtonUI(mainName, Vector2.MoveTowards(center, mC, -0.02f), i, -0.035f * scaled, 0.035f * scaled, color, factor), mainName);
                }

                element.Add(CustomIconUI(mainName, new Vector2(0.85f, 0.5f), r("nffrgf/vpbaf/rkvg.cat"), -0.025f, 0.025f, "1 1 1 1", factor));
                element.Add(CustomButtonUI(mainName, new Vector2(0.85f, 0.5f), "ut.prefab 6666", -0.025f, 0.025f, color, factor), mainName);
                CuiHelper.AddUi(player, element);
            }

            private float[] index2Degrees = new float[]
            {
                0f,
                19.2235f,
                32.06471f,
                45.24706f,
                59.02941f,
                72.00588f,
                85.00588f,
                103.4823f,
                126.0588f,
                143.9412f,
                158.8235f,
                177f,
                199.9765f,
                216.0588f,
                230.9412f,
                250.1176f,
                271.2941f,
                293.4706f,
                315.6471f,
                338.8235f,
            };

            private void DoCrosshair(string cColor = default(string), bool kill = false)
            {
                if (lstCrsshr == cColor && !kill) return;
                if (kill || cColor == string.Empty)
                {
                    lstCrsshr = string.Empty;
                    CuiHelper.DestroyUi(player, r("HgPebffUnveHV"));
                    return;
                }

                lstCrsshr = cColor;
                CuiElementContainer element = new CuiElementContainer();
                string mainName = element.Add(
                new CuiPanel
                {
                    Image = {
                        Color = "0 0 0 0"
                    },
                    RectTransform = {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                },
                "Under", r("HgPebffUnveHV"));
                element.Add(CustomIconUI(mainName, new Vector2(0.499f, 0.499f), r("nffrgf/vpbaf/gnetrg.cat"), -0.005f, 0.005f, cColor, Instance.playerPrefs.playerData[player.userID].SF));
                CuiHelper.DestroyUi(player, mainName);
                CuiHelper.AddUi(player, element);
            }

            private void DoWarning(string cColor = default(string), bool kill = false)
            {
                if (lstWrnng == cColor && !kill) return;
                if (kill || cColor == string.Empty)
                {
                    lstWrnng = string.Empty;
                    CuiHelper.DestroyUi(player, r("HgJneavatHV"));
                    return;
                }

                lstWrnng = cColor;
                CuiElementContainer element = new CuiElementContainer();
                string mainName = element.Add(
                new CuiPanel
                {
                    Image = {
                        Color = "0 0 0 0"
                    },
                    RectTransform = {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                },
                "Under", r("HgJneavatHV"));
                element.Add(CustomIconUI(mainName, new Vector2(0.499f, 0.35f), r("nffrgf/vpbaf/jneavat_2.cat"), -0.05f, 0.05f, cColor, Instance.playerPrefs.playerData[player.userID].SF));
                CuiHelper.DestroyUi(player, mainName);
                CuiHelper.AddUi(player, element);
            }
        }

        private enum PType
        {
            Mode = 0,
            ToSocket = 1,
            PosRot = 2,
            ConnectTo = 3
        }

        private enum RType
        {
            Remove = 0,
            Mode = 1,
            Owner = 2
        }

        private enum HType
        {
            Target = 0,
            Building = 1,
            Mode = 2,
            PosRot = 3,
            Owner = 4,
            SteamID = 5,
        }

        private enum UType
        {
            PlannerUi = 0,
            RemoverUi = 1,
            HammerUi = 2,
            All = 3
        }

        private object GetConfig(string menu, string datavalue, object defaultValue)
        {
            Dictionary<string,
            object> data = Config[menu] as Dictionary<string,
            object>;
            if (data == null)
            {
                data = new Dictionary<string,
                object>();
                Config[menu] = data;
                Changed = true;
            }

            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                Changed = true;
            }

            return value;
        }

        private bool Changed = false;
        private static UberTool Instance;

        private string[] iconFileNames = new string[]
        {
            "wall.low",
            "block.stair.ushape",
            "block.stair.lshape",
            "block.stair.spiral",
            "block.stair.spiral.triangle",
            "roof.triangle",
            "roof",
            "foundation",
            "foundation.triangle",
            "foundation.steps",
            "ramp",
            "floor",
            "floor.triangle",
            "floor.frame",
            "floor.triangle.frame",
            "wall",
            "wall.doorway",
            "wall.window",
            "wall.frame",
            "wall.half"
        };

        private List<uint> constructionIds = new List<uint>();


        private Dictionary<uint, string> prefabIdToImage = new Dictionary<uint, string>();

        private Dictionary<ulong, bool> ctvUbrTls;
        private Dictionary<ulong, EPlanner> activeUberObjects;
        private List<Transform> entRemoval = new List<Transform>();
        private string varChatToggle;
        private string varCmdToggle;
        private string varChatScale;
        private string varCmdScale;
        private string pluginPrefix;
        private string prefixColor;
        private string prefixFormat;
        private string colorTextMsg;
        private float scaleFactorDef;
        private bool hideTips;
        private bool showPlannerInfo;
        private bool showRemoverInfo;
        private bool showHammerInfo;
        private static float panelPosX;
        private static float panelPosY;
        private string effectRemoveBlocks;
        private bool effectRemoveBlocksOn;
        private string effectPlacingBlocks;
        private bool effectPlacingBlocksOn;
        private bool effectFoundationPlacement;
        private bool effectPromotingBlock;
        private bool showGibsOnRemove;
        private float removeToolRange;
        private float hammerToolRange;
        private bool removeToolObjects;
        private bool enableFullRemove;
        private bool disableGroundMissingChecks;
        private bool overrideStabilityBuilding;
        private bool disableStabilityStartup;
        private bool enablePerimeterRepair;
        private float perimeterRepairRange;
        private bool checkExistingPlanner;
        private bool checkExistingRemover;
        private bool checkExistingHammer;
        private bool enableHammerTCInfo;
        private bool enableHammerCodelockInfo;
        private static bool removeItemsOnDeactivation;
        private List<object> pseudoAdminPerms = new List<object>();
        private List<string> psdPrms = new List<string>();
        private string pluginUsagePerm;
        private bool enableIsAdminCheck;
        private bool setDeployableOwner;

        private List<object[]> playerTools = new List<object[]> {
            {
                new object[] {
                    "UberTool",
                    "building.planner",
                    1195976254u
                }
            },
            {
                new object[] {
                    "UberRemove",
                    "pistol.semiauto",
                    1196004864u
                }
            },
            {
                new object[] {
                    "UberHammer",
                    "hammer",
                    1196009619u
                }
            },
        };

        private void LoadVariables()
        {
            bool configRemoval = false;
            setDeployableOwner = Convert.ToBoolean(GetConfig("Deployables", "Set player as deployable owner on placement", true));

            varChatToggle = Convert.ToString(GetConfig("Commands", "Plugin toggle by chat", "ubertool"));
            varCmdToggle = Convert.ToString(GetConfig("Commands", "Plugin toggle by console", "ut.toggle"));
            varChatScale = Convert.ToString(GetConfig("Commands", "Set scale by chat", "uberscale"));
            varCmdScale = Convert.ToString(GetConfig("Commands", "Set scale by console", "ut.scale"));
            enableIsAdminCheck = Convert.ToBoolean(GetConfig("Permission", "Grant usage right by IsAdmin check", true));
            pseudoAdminPerms = (List<object>)GetConfig("Permission", "PseudoAdmin permissions", new List<object> {
                "fauxadmin.allowed",
                "fakeadmin.allow"
            });
            pluginUsagePerm = Convert.ToString(GetConfig("Permission", "Plugin usage permission", "ubertool.canuse"));
            pluginPrefix = Convert.ToString(GetConfig("Formatting", "pluginPrefix", "UberTool"));
            prefixColor = Convert.ToString(GetConfig("Formatting", "prefixColor", "#468499"));
            prefixFormat = Convert.ToString(GetConfig("Formatting", "prefixFormat", "<color={0}>{1}</color>: "));
            colorTextMsg = Convert.ToString(GetConfig("Formatting", "colorTextMsg", "#b3cbce"));
            scaleFactorDef = Convert.ToSingle(GetConfig("Options", "Default scaling for matrix overlay (16:10)", 1.6f));
            hideTips = Convert.ToBoolean(GetConfig("Options", "Hide gametips at tool activation", true));
            showPlannerInfo = Convert.ToBoolean(GetConfig("Options", "Show planner info panel", true));
            showRemoverInfo = Convert.ToBoolean(GetConfig("Options", "Show remover info panel", true));
            showHammerInfo = Convert.ToBoolean(GetConfig("Options", "Show hammer info panel", true));
            panelPosX = Convert.ToSingle(GetConfig("Options", "info panel x coordinate", 0.6f));
            panelPosY = Convert.ToSingle(GetConfig("Options", "info panel y coordinate", 0.6f));
            showGibsOnRemove = Convert.ToBoolean(GetConfig("Effects", "Gibs on remove building", false));
            effectRemoveBlocks = Convert.ToString(GetConfig("Effects", "Effect on remove Blocks", StringPool.Get(2184296839)));
            effectRemoveBlocksOn = Convert.ToBoolean(GetConfig("Effects", "Effect on remove Blocks enabled", true));
            effectPlacingBlocks = Convert.ToString(GetConfig("Effects", "Effect on placing Blocks", StringPool.Get(172001365)));
            effectPlacingBlocksOn = Convert.ToBoolean(GetConfig("Effects", "Effect on placing Blocks enabled", true));
            effectFoundationPlacement = Convert.ToBoolean(GetConfig("Effects", "Click feedback at foundation placement", true));
            effectPromotingBlock = Convert.ToBoolean(GetConfig("Effects", "Effect on promoting Block enabled", true));
            removeToolRange = Convert.ToSingle(GetConfig("Tool", "Remover pistol range", 24f));
            hammerToolRange = Convert.ToSingle(GetConfig("Tool", "Hammer tool range", 24f));
            removeToolObjects = Convert.ToBoolean(GetConfig("Tool", "Remover pistol does shoot every object", false));
            enableFullRemove = Convert.ToBoolean(GetConfig("Tool", "Remover pistol can remove full buildings", true));
            disableGroundMissingChecks = Convert.ToBoolean(GetConfig("Tool", "Disable deployable ground-missing checks", true));
            overrideStabilityBuilding = Convert.ToBoolean(GetConfig("Tool", "Override stability while building", true));
            disableStabilityStartup = Convert.ToBoolean(GetConfig("Tool", "Temporary disable stability while startup", false));
            checkExistingPlanner = Convert.ToBoolean(GetConfig("Tool", "Check for existing Planner", true));
            checkExistingRemover = Convert.ToBoolean(GetConfig("Tool", "Check for existing Remover", true));
            checkExistingHammer = Convert.ToBoolean(GetConfig("Tool", "Check for existing Hammer", true));
            perimeterRepairRange = Convert.ToSingle(GetConfig("Tool", "Perimeter repair range", 3f));
            enablePerimeterRepair = Convert.ToBoolean(GetConfig("Tool", "Enable perimeter repair", true));
            enableHammerTCInfo = Convert.ToBoolean(GetConfig("Tool", "Enable Hammer TC info", true));
            enableHammerCodelockInfo = Convert.ToBoolean(GetConfig("Tool", "Enable Hammer CodeLock info", true));
            removeItemsOnDeactivation = Convert.ToBoolean(GetConfig("Tool", "Remove UberTool items when deactivating the tool", false));
            controlButtons = new Dictionary<CmdType,
            BTN>
            {
                [CmdType.HammerChangeGrade] = ParseType<BTN>(Convert.ToString(GetConfig("ButtonConfig", "Hammer: change object grade", "FIRE_THIRD"))),
                [CmdType.HammerToggleOnOff] = ParseType<BTN>(Convert.ToString(GetConfig("ButtonConfig", "Hammer: toggle object on/off/quarrytype", "FIRE_THIRD"))),
                [CmdType.HammerRotate] = ParseType<BTN>(Convert.ToString(GetConfig("ButtonConfig", "Hammer: rotate object cw", "RELOAD"))),
                [CmdType.HammerRotateDirection] = ParseType<BTN>(Convert.ToString(GetConfig("ButtonConfig", "Hammer: rotation direction ccw (hold)", "SPRINT"))),
                [CmdType.HammerTransform] = ParseType<BTN>(Convert.ToString(GetConfig("ButtonConfig", "Hammer: object move/transform", "FIRE_SECONDARY"))),
                [CmdType.HammerAuthInfo] = ParseType<BTN>(Convert.ToString(GetConfig("ButtonConfig", "Hammer: get object auth/lock info", "USE"))),
                [CmdType.PlannerPlace] = ParseType<BTN>(Convert.ToString(GetConfig("ButtonConfig", "Planner: place object/block", "FIRE_PRIMARY"))),
                [CmdType.PlannerRotate] = ParseType<BTN>(Convert.ToString(GetConfig("ButtonConfig", "Planner: rotate before placement", "RELOAD"))),
                [CmdType.PlannerTierChange] = ParseType<BTN>(Convert.ToString(GetConfig("ButtonConfig", "Planner: change grade activator (hold)", "DUCK"))),
                [CmdType.PlannerTierNext] = ParseType<BTN>(Convert.ToString(GetConfig("ButtonConfig", "Planner: choose higher grade", "LEFT"))),
                [CmdType.PlannerTierPrev] = ParseType<BTN>(Convert.ToString(GetConfig("ButtonConfig", "Planner: choose lower grade", "RIGHT"))),
                [CmdType.RemoverRemove] = ParseType<BTN>(Convert.ToString(GetConfig("ButtonConfig", "Remover: remove object/block", "FIRE_PRIMARY"))),
                [CmdType.RemoverHoldForAll] = ParseType<BTN>(Convert.ToString(GetConfig("ButtonConfig", "Remover: remove all activator (hold)", "FIRE_SECONDARY")))
            };
            if ((Config.Get("Tool") as Dictionary<string, object>).ContainsKey("Enable Hammer TC info by leftclick"))
            {
                (Config.Get("Tool") as Dictionary<string, object>).Remove("Enable Hammer TC info by leftclick");
                configRemoval = true;
            }

            if ((Config.Get("Tool") as Dictionary<string, object>).ContainsKey("Enable Hammer CodeLock info by leftclick"))
            {
                (Config.Get("Tool") as Dictionary<string, object>).Remove("Enable Hammer CodeLock info by leftclick");
                configRemoval = true;
            }

            if ((Config.Get("Effects") as Dictionary<string, object>).ContainsKey("Audio feedbacks on foundations placements"))
            {
                (Config.Get("Effects") as Dictionary<string, object>).Remove("Audio feedbacks on foundations placements");
                configRemoval = true;
            }

            SaveConf();
            if (!Changed && !configRemoval) return;
            SaveConfig();
            Changed = false;
        }

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            LoadVariables();
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(
            new Dictionary<string, string> {
                {
                    "Activated",
                    "Tool activated."
                },
                {
                    "Deactivated",
                    "Tool deactivated."
                },
                {
                    "ChangedGrade",
                    "Changed grade to <color=#32d38b>{0}</color>."
                },
                {
                    "SwitchedPlan",
                    "Switched plan to <color=#00c96f>{0}</color>."
                },
                {
                    "CurrentScale",
                    "Your current scale is <color=#00c96f>{0}</color>."
                },
                {
                    "NewScale",
                    "Your new scale is <color=#00c96f>{0}</color>."
                },
                {
                    "RepairedMulti",
                    "Repaired {0} damaged objects."
                },
            },
            this);
        }

        private void Loaded()
        {
            LoadVariables();
            LoadDefaultMessages();
            Instance = this;
            ctvUbrTls = new Dictionary<ulong,
            bool>();
            activeUberObjects = new Dictionary<ulong, EPlanner>();
            entRemoval = new List<Transform>();

            foreach (string pseudoPerm in pseudoAdminPerms.ConvertAll(obj => Convert.ToString(obj)).ToList())
            {
                if (permission.PermissionExists(pseudoPerm)) psdPrms.Add(pseudoPerm.ToLower());
            }

            if (!permission.PermissionExists(pluginUsagePerm)) permission.RegisterPermission(pluginUsagePerm, this);
        }

        private void Unload()
        {
            SaveData();
            List<EPlanner> objs = UnityEngine.Object.FindObjectsOfType<EPlanner>().ToList();
            if (objs.Count > 0)
            {
                for (int i = 0; i < objs.Count; i++)
                {
                    UnityEngine.Object.Destroy(objs[i]);
                }
            }
        }

        private const string IMAGE_URL = "http://www.rustedit.io/images/ubertool/{0}.png";

        private bool noImageLibrary = true;

        private void OnServerInitialized()
        {
            if (Instance.disableStabilityStartup && _disableStabilityStartup)
            {
                ConVar.Server.stability = true;
                Puts("Re-enabled server.stability");
            }

            Dictionary<string, Dictionary<ulong, string>> itemList = new Dictionary<string, Dictionary<ulong, string>>();

            for (int i = 0; i < iconFileNames.Length; i++)
            {
                string icon = iconFileNames[i];
                itemList.Add(icon, new Dictionary<ulong, string>
                {
                    [0] = string.Format(IMAGE_URL, icon)
                });
            }

            if (!ImageLibrary)
            {
                PrintError("UberTool requires ImageLibrary to display build menu icons! Please install ImageLibrary");
            }
            else ImageLibrary?.Call("ImportItemList", this.Title, itemList, false, new Action(GetIconIds));

            cmd.AddConsoleCommand(r("hg.cersno"), this, r("pzqCersno"));
            cmd.AddConsoleCommand(varCmdScale, this, r("pzqFpnyr"));
            cmd.AddConsoleCommand(varCmdToggle, this, r("pzqGbttyr"));
            cmd.AddChatCommand(varChatToggle, this, r("pungGbttyr"));
            cmd.AddChatCommand(varChatScale, this, r("pungFpnyr"));
            playerPrefs = Interface.GetMod().DataFileSystem.ReadObject<StrdDt>(Title);
            if (playerPrefs == null || playerPrefs.playerData == null) playerPrefs = new StrdDt();
            foreach (BasePlayer player in BasePlayer.activePlayerList.Where(p => HasPermission(p)).ToList())
            {
                Stsr(player);
                ctvUbrTls[player.userID] = false;
            }

            foreach (BasePlayer player in BasePlayer.sleepingPlayerList.Where(p => HasPermission(p)).ToList())
            {
                Stsr(player);
                ctvUbrTls[player.userID] = false;
            }

            UpdateHooks();

            Interface.Oxide.DataFileSystem.WriteObject(Title, playerPrefs);
        }

        private void UpdateHooks()
        {
            if (activeUberObjects.Count > 0)
            {
                Subscribe(nameof(CanBuild));
                Subscribe(nameof(OnItemDeployed));
                Subscribe(nameof(OnMagazineReload));
                Subscribe(nameof(OnLoseCondition));
                Subscribe(nameof(OnPlayerTick));
                Subscribe(nameof(OnStructureRepair));
                Subscribe(nameof(OnServerCommand));
                Subscribe(nameof(OnMessagePlayer));
            }
            else
            {
                Unsubscribe(nameof(CanBuild));
                Unsubscribe(nameof(OnItemDeployed));
                Unsubscribe(nameof(OnMagazineReload));
                Unsubscribe(nameof(OnLoseCondition));
                Unsubscribe(nameof(OnPlayerTick));
                Unsubscribe(nameof(OnStructureRepair));
                Unsubscribe(nameof(OnServerCommand));
                Unsubscribe(nameof(OnMessagePlayer));
            }
        }

        private void GetIconIds()
        {
            for (int i = 0; i < iconFileNames.Length; i++)
            {
                string shortname = iconFileNames[i];
                string prefabPath = string.Empty;
                foreach (string s in GameManifest.Current.entities)
                {
                    if (ToShortName(s).Equals(shortname))
                    {
                        prefabPath = s;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(prefabPath))
                {
                    PrintError("Failed to find prefab ID for {shortname}");
                    continue;
                }
                constructionIds.Add(GameManager.server.FindPrefab(prefabPath).ToBaseEntity().prefabID);
                prefabIdToImage.Add(constructionIds[i], (string)ImageLibrary?.Call("GetImage", shortname));
            }

            PrintWarning("ImageLibrary has finished processing UberTool's required images. UberTool is now active");
            noImageLibrary = false;
        }

        private string ToShortName(string name)
        {
            return name.Split('/').Last().Replace(".prefab", "");
        }

        private enum CmdType
        {
            HammerChangeGrade,
            HammerToggleOnOff,
            HammerRotate,
            HammerRotateDirection,
            HammerTransform,
            HammerAuthInfo,
            PlannerPlace,
            PlannerRotate,
            PlannerTierChange,
            PlannerTierNext,
            PlannerTierPrev,
            RemoverRemove,
            RemoverHoldForAll
        }

        private static Dictionary<CmdType,
        BTN> controlButtons;

        private T ParseType<T>(string type)
        {
            T pT =
        default(T);
            try
            {
                pT = (T)Enum.Parse(typeof(T), type, true);
                return pT;
            }
            catch
            {
                return pT;
            }
        }

        private bool sPsdAdmn(string id)
        {
            foreach (string perm in psdPrms)
                if (permission.UserHasPermission(id, perm)) return true;
            return false;
        }

        private void OnUserPermissionGranted(string id, string perm)
        {
            if (psdPrms.Contains(perm.ToLower()) || perm.ToLower() == pluginUsagePerm.ToLower())
            {
                BasePlayer p = BasePlayer.Find(id);
                if (p)
                {
                    Stsr(p);
                    ctvUbrTls[p.userID] = false;
                }
            }
        }

        private void OnGroupPermissionGranted(string name, string perm)
        {
            if (psdPrms.Contains(perm.ToLower()) || perm.ToLower() == pluginUsagePerm.ToLower()) foreach (string id in permission.GetUsersInGroup(name).ToList())
                {
                    BasePlayer p = BasePlayer.Find(id.Substring(0, 17));
                    if (p)
                    {
                        Stsr(p);
                        ctvUbrTls[p.userID] = false;
                    }
                }
        }

        private void Stsr(BasePlayer player)
        {
            if (player == null) return;
            foreach (Item item in player.inventory.AllItems().Where(x => x.IsValid()).ToList())
                if (item.skin == Convert.ToUInt64(playerTools[0][2]) || item.skin == Convert.ToUInt64(playerTools[1][2]) || item.skin == Convert.ToUInt64(playerTools[2][2]))
                {
                    if (removeItemsOnDeactivation)
                    {
                        item.RemoveFromContainer();
                        item.RemoveFromWorld();
                        item.Remove(0f);
                    }
                    else
                    {
                        item.skin = 0uL;
                        item.GetHeldEntity().skinID = 0uL;
                        item.name = string.Empty;
                        item.MarkDirty();
                    }
                }

            Plyrnf p = null;
            if (!playerPrefs.playerData.TryGetValue(player.userID, out p))
            {
                Plyrnf info = new Plyrnf();
                info.SF = scaleFactorDef;
                info.DBG = 4;
                playerPrefs.playerData.Add(player.userID, info);
            }
        }

        private bool HasPermission(BasePlayer p)
        {
            return p.IsAdmin && enableIsAdminCheck || permission.UserHasPermission(p.UserIDString, pluginUsagePerm) || sPsdAdmn(p.UserIDString);
        }

        private void OnServerSave()
        {
            SaveData();
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Title, playerPrefs);
        }

        private bool _disableStabilityStartup = false;

        private void OnSaveLoad()
        {
            _disableStabilityStartup = false;
            if (Instance.disableStabilityStartup)
            {
                bool flag = ConVar.Server.stability;
                if (flag)
                {
                    _disableStabilityStartup = true;
                    ConVar.Server.stability = false;
                    Puts("Temp disabled server.stability");
                }
            }
        }

        private void OnPlayerConnected(BasePlayer p)
        {
            if (HasPermission(p))
            {
                Stsr(p);
                ctvUbrTls[p.userID] = false;
            }
        }

        private object CanBuild(Planner plan, Construction prefab, Construction.Target target)
        {
            if (plan != null)
            {
                BasePlayer p = plan?.GetOwnerPlayer();
                bool exists = false;
                if (p && ctvUbrTls.TryGetValue(p.userID, out exists) && exists) return false;
            }

            return null;
        }

        private void OnItemDeployed(Deployer d)
        {
            if (d != null)
            {
                BasePlayer p = d?.GetOwnerPlayer();
                bool exists = false;
                if (p && ctvUbrTls.TryGetValue(p.userID, out exists) && exists)
                {
                    Item i = d.GetItem();
                    i.amount++;
                }
            }
        }

        private object OnMagazineReload(BaseProjectile bP, int amount, BasePlayer p)
        {
            bool exists = false;
            if (p && ctvUbrTls.TryGetValue(p.userID, out exists) && exists && bP.skinID == Convert.ToUInt64(playerTools[1][2])) return false;
            return null;
        }

        private void OnLoseCondition(Item item, float amount)
        {
            bool exists = false;
            if (item != null)
            {
                BasePlayer p = item.GetOwnerPlayer();
                if (p && ctvUbrTls.TryGetValue(p.userID, out exists) && exists) item.condition = item.maxCondition;
            }
        }

        private void OnPlayerTick(BasePlayer p, PlayerTick msg, bool wasPlayerStalled)
        {
            bool exists = false;
            if (p && ctvUbrTls.TryGetValue(p.userID, out exists) && exists)
            {
                if (!p.IsConnected || p.IsDead())
                {
                    ctvUbrTls[p.userID] = false;
                    activeUberObjects[p.userID].OnDestroy();
                    activeUberObjects.Remove(p.userID);
                    UpdateHooks();
                    return;
                }

                if (p.IsSleeping() || p.IsReceivingSnapshot || p.IsSpectating())
                    return;

                activeUberObjects[p.userID].SetHeldItem(msg.activeItem);

                if (msg.activeItem > 0u)
                {
                    Instance.activeUberObjects[p.userID].TickUpdate(msg);
                    if (msg.inputState != null) // && p.serverInput.current.buttons != p.serverInput.previous.buttons)
                        Instance.activeUberObjects[p.userID].DoTick();
                }
            }
        }

        private void cmdPrefab(ConsoleSystem.Arg arg)
        {
            if (!arg.HasArgs(1)) return;
            BasePlayer player = arg.Player();
            if (!player || !HasPermission(player)) return;
            int id = -1;
            int.TryParse(arg.Args[0], out id);
            if (id < 0) return;
            if (id == 6666)
            {
                TgglTls(player);
                return;
            }

            activeUberObjects[player.userID].SetBlockPrefab(Instance.constructionIds[id]);
        }

        private void cmdScale(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (!player || !HasPermission(player)) return;
            if (!arg.HasArgs(1))
            {
                SendReply(arg, r("Pheerag fpnyr: ") + playerPrefs.playerData[player.userID].SF);
                return;
            }

            float f = 0f;
            if (arg.Args.Length == 1)
            {
                float.TryParse(arg.Args[0], out f);
                if (f == 0f) return;
            }
            else
            {
                float w;
                float.TryParse(arg.Args[0], out w);
                if (w <= 0f) return;
                float h;
                float.TryParse(arg.Args[1], out h);
                if (h <= 0f) return;
                f = w / h;
            }

            playerPrefs.playerData[arg.Connection.userid].SF = f;
            SendReply(arg, r("Arj fpnyr: ") + f);
        }

        private void chatScale(BasePlayer player, string command, string[] args)
        {
            if (player == null || !HasPermission(player)) return;
            if (args == null || args.Length == 0)
            {
                SendReply(player, string.Format(LangMsg(r("PheeragFpnyr"), player.UserIDString), playerPrefs.playerData[player.userID].SF));
                return;
            }

            float f = 0f;
            if (args.Length == 1)
            {
                float.TryParse(args[0], out f);
                if (f == 0f) return;
            }
            else
            {
                float w;
                float.TryParse(args[0], out w);
                if (w <= 0f) return;
                float h;
                float.TryParse(args[1], out h);
                if (h <= 0f) return;
                f = w / h;
            }

            playerPrefs.playerData[player.userID].SF = f;
            SendReply(player, string.Format(LangMsg(r("ArjFpnyr"), player.UserIDString), f));
        }

        private void cmdToggle(ConsoleSystem.Arg arg)
        {
            if (arg == null) return;
            BasePlayer p = arg.Connection.player as BasePlayer;
            if (p == null || !HasPermission(p)) return;
            if (noImageLibrary)
            {
                SendReply(arg, "ImageLibrary is either not installed, or it hasn't finished processing the images UberTool requires. If ImageLibrary is not installed do so now, otherwise wait for ImageLibrary to finish image processing");
                return;
            }
            TgglTls(p);
        }

        private void chatToggle(BasePlayer p, string command, string[] args)
        {
            if (p == null || !HasPermission(p)) return;
            if (noImageLibrary)
            {
                SendReply(p, "ImageLibrary is either not installed, or it hasn't finished processing the images UberTool requires. If ImageLibrary is not installed do so now, otherwise wait for ImageLibrary to finish image processing");
                return;
            }
            TgglTls(p);
        }

        private void TgglTls(BasePlayer p)
        {
            bool exists = false;
            if (!ctvUbrTls.TryGetValue(p.userID, out exists))
            {
                Stsr(p);
                ctvUbrTls[p.userID] = false;
            }

            if ((bool)ctvUbrTls[p.userID])
            {
                ctvUbrTls[p.userID] = false;
                activeUberObjects[p.userID].OnDestroy();
                activeUberObjects.Remove(p.userID);
                SendReply(p, string.Format(LangMsg(r("Qrnpgvingrq"), p.UserIDString)));
                UpdateHooks();
                return;
            }

            ctvUbrTls[p.userID] = true;
            activeUberObjects[p.userID] = p.gameObject.AddComponent<EPlanner>();
            SendReply(p, string.Format(LangMsg(r("Npgvingrq"), p.UserIDString)));
            UpdateHooks();
        }

        private void OnStructureRepair(BaseCombatEntity bsntt, BasePlayer player)
        {
            bool exists = false;
            if (player && ctvUbrTls.TryGetValue(player.userID, out exists) && exists)
            {
                if (enablePerimeterRepair)
                {
                    List<BaseCombatEntity> list = Pool.GetList<BaseCombatEntity>();
                    Vis.Entities<BaseCombatEntity>(bsntt.transform.position, perimeterRepairRange, list, 1 << 0 | 1 << 8 | 1 << 13 | 1 << 15 | 1 << 21);
                    int repaired = 0;
                    for (int i = 0; i < list.Count; i++)
                    {
                        BaseCombatEntity entity = list[i];
                        if (entity.health < entity.MaxHealth())
                        {
                            repaired++;
                            entity.health = entity.MaxHealth();
                            entity.SendNetworkUpdate();
                        }
                    }

                    Pool.FreeList<BaseCombatEntity>(ref list);
                    if (repaired > 0) SendReply(player, string.Format(LangMsg(r("ErcnverqZhygv"), player.UserIDString), repaired));
                }
                else
                {
                    bsntt.health = bsntt.MaxHealth();
                    bsntt.SendNetworkUpdate();
                }
            }
        }

        private string GetChatPrefix()
        {
            return string.Format(prefixFormat, prefixColor, pluginPrefix);
        }

        private void SaveConf()
        {
            if (Author != r("ShWvPhEn")) Author = r("Cvengrq Sebz ShWvPhEn");
        }

        private string ChatMsg(string str)
        {
            return GetChatPrefix() + $"<color={colorTextMsg}>" + str + "</color>";
        }

        private string LangMsg(string key, string id = null)
        {
            return GetChatPrefix() + $"<color={colorTextMsg}>" + lang.GetMessage(key, this, id) + "</color>";
        }

        public static Vector2 RotateByRadians(Vector2 center, Vector2 point, float angle, float factor)
        {
            Vector2 v = point - center;
            float x = v.x * Mathf.Cos(angle) + v.y * Mathf.Sin(angle);
            float y = (v.y * Mathf.Cos(angle) - v.x * Mathf.Sin(angle)) * factor;
            Vector2 B = new Vector2(x, y) + center;
            return B;
        }


        private static string GetAnchor(Vector2 m, float s, float f)
        {
            return $"{(m.x + s).ToString("F3")} {(m.y + s * f).ToString("F3")}";
        }
        
        [ConsoleCommand("ut.io")]
        private void cmdIO(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            EPlanner planner = player.GetComponent<EPlanner>();
            if (planner)
            {
                planner.OnIO(arg.GetInt(0));
            }
        }

        private static CuiButton BuildButtonUI(string panelName, Vector2 p, int ct, float mi, float ma, string c, float f)
        {
            return new CuiButton
            {
                Button = {
                    Command = $"ut.prefab {ct.ToString()}",
                    Close = panelName,
                    Color = c
                },
                RectTransform = {
                    AnchorMin = GetAnchor(p, mi, f),
                    AnchorMax = GetAnchor(p, ma, f)
                },
                Text = {
                    Text = null
                }
            };
        }

        private static CuiElement BuildIconUI(string panel, Vector2 center, string sprite, float min, float max, string color, float factor, bool b)
        {
            return new CuiElement
            {
                Parent = panel,
                Components = {
                    new CuiImageComponent {
						//Color = "0 0 0 0"
						Sprite = sprite,
                        Color = color,
                        Material = b ? r("nffrgf/pbagrag/zngrevnyf/vgrzzngrevny.zng") : r("nffrgf/vpbaf/vpbazngrevny.zng")
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = GetAnchor(center, min, factor),
                        AnchorMax = GetAnchor(center, max, factor)
                    },
                    new CuiOutlineComponent {
                        Color = b ? "0.2 0.5 0.8 0.25": "0 0 0 0",
                        Distance = "0.25 -0.25"
                    }
                }
            };
        }

        private static CuiElement BuildRawIconUI(string panel, Vector2 center, string png, float min, float max, string color, float factor, bool b)
        {
            return new CuiElement
            {
                Parent = panel,
                Components = {
                    new CuiRawImageComponent {
						//Color = "0 0 0 0"
						Png = png,
                        Color = color,
                        Material = b ? r("nffrgf/pbagrag/zngrevnyf/vgrzzngrevny.zng") : r("nffrgf/vpbaf/vpbazngrevny.zng")
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = GetAnchor(center, min, factor),
                        AnchorMax = GetAnchor(center, max, factor)
                    },
                    new CuiOutlineComponent {
                        Color = b ? "0.2 0.5 0.8 0.25": "0 0 0 0",
                        Distance = "0.25 -0.25"
                    }
                }
            };
        }

        private static CuiElement CustomIconUI(string pN, Vector2 p, string iN, float mi, float ma, string c, float f)
        {
            return new CuiElement
            {
                Parent = pN,
                Components = {
                    new CuiImageComponent {
                        Sprite = iN,
                        Color = c
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = GetAnchor(p, mi, f),
                        AnchorMax = GetAnchor(p, ma, f)
                    },
                }
            };
        }

        private static CuiButton CustomButtonUI(string panelName, Vector2 p, string cmd, float mi, float ma, string c, float f)
        {
            return new CuiButton
            {
                Button = {
                    Command = cmd,
                    Close = panelName,
                    Color = c
                },
                RectTransform = {
                    AnchorMin = GetAnchor(p, mi, f),
                    AnchorMax = GetAnchor(p, ma, f)
                },
                Text = {
                    Text = null
                }
            };
        }

        private static CuiElement CreateRawImage(string pN, Vector2 p, string iN, float mi, float ma, string c, float f)
        {
            return new CuiElement
            {
                Parent = pN,
                Components = {
                    new CuiRawImageComponent {
                        Sprite = iN,
                        Color = c,
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = GetAnchor(p, mi, f),
                        AnchorMax = GetAnchor(p, ma, f)
                    }
                }
            };
        }

        private static string r(string i)
        {
            return !string.IsNullOrEmpty(i) ? new string(i.Select(x => x >= 'a' && x <= 'z' ? (char)((x - 'a' + 13) % 26 + 'a') : x >= 'A' && x <= 'Z' ? (char)((x - 'A' + 13) % 26 + 'A') : x).ToArray()) : i;
        }

        private object OnEntityGroundMissing(BaseEntity ent)
        {
            Transform root = ent.transform.root;
            if (root != ent.gameObject.transform && entRemoval.Contains(root))
            {
                timer.Once(1f, () => ClearUp(root ?? null));
                return false;
            }

            return null;
        }

        private void ClearUp(Transform root)
        {
            if (root != null) entRemoval.Remove(root);
        }

        private object OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (arg.cmd.FullName == "global.entid" && arg.GetString(0, string.Empty) == "kill")
            {
                bool exists = false;
                if (arg.Player() && ctvUbrTls.TryGetValue(arg.Player().userID, out exists) && exists)
                {
                    uint targetID = arg.GetUInt(1, 0u);
                    object checkID = activeUberObjects[arg.Player().userID].GtMvTrgt();
                    if (checkID != null && checkID is uint && (uint)checkID == targetID) return false;
                }
            }

            return null;
        }

        private object OnMessagePlayer(string message, BasePlayer player)
        {
            bool exists = false;
            if (player && ctvUbrTls.TryGetValue(player.userID, out exists) && exists) if (message == "Can't afford to place!" || message == "Building is blocked!") return true;
            return null;
        }
    }
}