using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Stacks", "Nimant", "2.0.4")]
    public class Stacks : RustPlugin
    {
        [PluginReference] private Plugin FurnaceSplitter;
        private static Dictionary<string, int> jXuGEdXFJIzIUWQOpYOZNIJ = new Dictionary<string, int>();

        private static Dictionary<string, int> XAXfVFPbfUI = new Dictionary<string, int>()
            {{"Attire", 2}, {"Tool", 1}, {"Weapon", 1}};

        private void Init()
        {
            MFbOuyfXAqPnQUkGOaKBezo();
            var yIhMaLVxgmbLicchecoXJfdGI = false;
            if (HGJfIWGOkTcdLbGIFiAb.RSjYaftfLwPnVesiMQltPxD == null)
            {
                HGJfIWGOkTcdLbGIFiAb.RSjYaftfLwPnVesiMQltPxD = new List<string>();
                yIhMaLVxgmbLicchecoXJfdGI = true;
            }

            if (HGJfIWGOkTcdLbGIFiAb.yBPzMbzsaiBVjicaXFqsmEjTwwvjd == null)
            {
                HGJfIWGOkTcdLbGIFiAb.yBPzMbzsaiBVjicaXFqsmEjTwwvjd = new List<string>()
                    {"Blue Keycard", "Green Keycard", "Red Keycard"};
                yIhMaLVxgmbLicchecoXJfdGI = true;
            }

            if (HGJfIWGOkTcdLbGIFiAb.eyEnBlUwiSVEbqTYsOoAWSlbYAKqY == null)
            {
                HGJfIWGOkTcdLbGIFiAb.eyEnBlUwiSVEbqTYsOoAWSlbYAKqY = new List<ulong>();
                yIhMaLVxgmbLicchecoXJfdGI = true;
            }

            if (HGJfIWGOkTcdLbGIFiAb.SUMgbOLYXEWqWGNauhbgEajpmt == null)
            {
                HGJfIWGOkTcdLbGIFiAb.SUMgbOLYXEWqWGNauhbgEajpmt = new List<string>();
                yIhMaLVxgmbLicchecoXJfdGI = true;
            }

            if (yIhMaLVxgmbLicchecoXJfdGI) qqOsjhDfbcTCbeFmh(HGJfIWGOkTcdLbGIFiAb);
        }

        private void OnServerInitialized()
        {
            var gnvHnEcTiZI = ItemManager.itemList.ToList();
            List<string> iYVmINDpHhehYOadREWmaW = new List<string>();
            List<string> CBVPeybAMYQDADFiPvmfOMFv = new List<string>();
            foreach (var mCDDdYSVLSWfHUkXrtG in gnvHnEcTiZI.OrderBy(r => r.category))
            {
                var lRpcAWHnInDrkWHzVhePfu = mCDDdYSVLSWfHUkXrtG.category.ToString();
                var YYvmAiPUeXEYGpFzwLxLNVypVAW = mCDDdYSVLSWfHUkXrtG.displayName.english;
                if (!HGJfIWGOkTcdLbGIFiAb.AqfUFopQwAbfNey.ContainsKey(lRpcAWHnInDrkWHzVhePfu))
                {
                    HGJfIWGOkTcdLbGIFiAb.AqfUFopQwAbfNey.Add(lRpcAWHnInDrkWHzVhePfu,
                        new Dictionary<string, int> {{YYvmAiPUeXEYGpFzwLxLNVypVAW, mCDDdYSVLSWfHUkXrtG.stackable}});
                    iYVmINDpHhehYOadREWmaW.Add(
                        $"'{YYvmAiPUeXEYGpFzwLxLNVypVAW}' в категории '{lRpcAWHnInDrkWHzVhePfu}'");
                }
                else if (!HGJfIWGOkTcdLbGIFiAb.AqfUFopQwAbfNey[lRpcAWHnInDrkWHzVhePfu]
                    .ContainsKey(YYvmAiPUeXEYGpFzwLxLNVypVAW))
                {
                    HGJfIWGOkTcdLbGIFiAb.AqfUFopQwAbfNey[lRpcAWHnInDrkWHzVhePfu]
                        .Add(YYvmAiPUeXEYGpFzwLxLNVypVAW, mCDDdYSVLSWfHUkXrtG.stackable);
                    iYVmINDpHhehYOadREWmaW.Add(
                        $"'{YYvmAiPUeXEYGpFzwLxLNVypVAW}' в категории '{lRpcAWHnInDrkWHzVhePfu}'");
                }

                if (!jXuGEdXFJIzIUWQOpYOZNIJ.ContainsKey(lRpcAWHnInDrkWHzVhePfu + "|" + YYvmAiPUeXEYGpFzwLxLNVypVAW))
                    jXuGEdXFJIzIUWQOpYOZNIJ.Add(lRpcAWHnInDrkWHzVhePfu + "|" + YYvmAiPUeXEYGpFzwLxLNVypVAW,
                        mCDDdYSVLSWfHUkXrtG.stackable);
                mCDDdYSVLSWfHUkXrtG.stackable =
                    HGJfIWGOkTcdLbGIFiAb.AqfUFopQwAbfNey[lRpcAWHnInDrkWHzVhePfu][YYvmAiPUeXEYGpFzwLxLNVypVAW];
            }

            foreach (var aDLTPabOlfgxA in HGJfIWGOkTcdLbGIFiAb.AqfUFopQwAbfNey.Keys)
            {
                foreach (var mCDDdYSVLSWfHUkXrtG in HGJfIWGOkTcdLbGIFiAb.AqfUFopQwAbfNey[aDLTPabOlfgxA]
                    .ToDictionary(x => x.Key, x => x.Value))
                {
                    if (!gnvHnEcTiZI.Exists(x =>
                        x.displayName.english == mCDDdYSVLSWfHUkXrtG.Key && x.category.ToString() == aDLTPabOlfgxA))
                    {
                        HGJfIWGOkTcdLbGIFiAb.AqfUFopQwAbfNey[aDLTPabOlfgxA].Remove(mCDDdYSVLSWfHUkXrtG.Key);
                        CBVPeybAMYQDADFiPvmfOMFv.Add($"'{mCDDdYSVLSWfHUkXrtG.Key}' из категории '{aDLTPabOlfgxA}'");
                    }
                }

                if (HGJfIWGOkTcdLbGIFiAb.AqfUFopQwAbfNey[aDLTPabOlfgxA].Count == 0)
                    HGJfIWGOkTcdLbGIFiAb.AqfUFopQwAbfNey.Remove(aDLTPabOlfgxA);
            }

            if (iYVmINDpHhehYOadREWmaW.Count == 0 && CBVPeybAMYQDADFiPvmfOMFv.Count == 0) return;
            qqOsjhDfbcTCbeFmh(HGJfIWGOkTcdLbGIFiAb);
            if (iYVmINDpHhehYOadREWmaW.Count > 0)
            {
                PrintWarning("В конфигурационный файл были добавлены новые предметы:");
                foreach (var mCDDdYSVLSWfHUkXrtG in iYVmINDpHhehYOadREWmaW) PrintWarning(mCDDdYSVLSWfHUkXrtG);
            }

            if (CBVPeybAMYQDADFiPvmfOMFv.Count > 0)
            {
                PrintWarning("Конфигурационный файл был очищен от устаревших предметов:");
                foreach (var mCDDdYSVLSWfHUkXrtG in CBVPeybAMYQDADFiPvmfOMFv) PrintWarning(mCDDdYSVLSWfHUkXrtG);
            }
        }

        private void Unload()
        {
            foreach (var mCDDdYSVLSWfHUkXrtG in ItemManager.itemList)
                mCDDdYSVLSWfHUkXrtG.stackable =
                    jXuGEdXFJIzIUWQOpYOZNIJ[
                        mCDDdYSVLSWfHUkXrtG.category.ToString() + "|" + mCDDdYSVLSWfHUkXrtG.displayName.english];
        }

        private ItemContainer.CanAcceptResult? CanAcceptItem(ItemContainer unsgxNVqXKqqECsXQrG,
            Item YjlEoVwLrCFlMZxGRUgOwaQjuF, int targetPos)
        {
            if (unsgxNVqXKqqECsXQrG == null || YjlEoVwLrCFlMZxGRUgOwaQjuF == null ||
                unsgxNVqXKqqECsXQrG.playerOwner == null) return null;
            if (HGJfIWGOkTcdLbGIFiAb.RSjYaftfLwPnVesiMQltPxD.Contains(YjlEoVwLrCFlMZxGRUgOwaQjuF.info.displayName
                    .english) ||
                HGJfIWGOkTcdLbGIFiAb.RSjYaftfLwPnVesiMQltPxD.Contains(YjlEoVwLrCFlMZxGRUgOwaQjuF.info.shortname))
                return null;
            if (unsgxNVqXKqqECsXQrG.playerOwner.inventory.containerBelt == unsgxNVqXKqqECsXQrG)
            {
                if (tcChzNBoJCMHBaBQGcpnAUGuamBU(YjlEoVwLrCFlMZxGRUgOwaQjuF.info) &&
                    ((HGJfIWGOkTcdLbGIFiAb.yBPzMbzsaiBVjicaXFqsmEjTwwvjd.Contains(YjlEoVwLrCFlMZxGRUgOwaQjuF.info
                          .displayName.english) ||
                      HGJfIWGOkTcdLbGIFiAb.yBPzMbzsaiBVjicaXFqsmEjTwwvjd.Contains(YjlEoVwLrCFlMZxGRUgOwaQjuF.info
                          .shortname)) ||
                        XAXfVFPbfUI.ContainsKey(YjlEoVwLrCFlMZxGRUgOwaQjuF.info.category.ToString()) &&
                        XAXfVFPbfUI[YjlEoVwLrCFlMZxGRUgOwaQjuF.info.category.ToString()] == 1))
                {
                    if (YjlEoVwLrCFlMZxGRUgOwaQjuF.amount > 1 ||
                        PVhltAKgoQ(unsgxNVqXKqqECsXQrG, YjlEoVwLrCFlMZxGRUgOwaQjuF, targetPos))
                        return ItemContainer.CanAcceptResult.CannotAccept;
                }
            }

            if (unsgxNVqXKqqECsXQrG.playerOwner.inventory.containerWear == unsgxNVqXKqqECsXQrG)
            {
                if ((HGJfIWGOkTcdLbGIFiAb.yBPzMbzsaiBVjicaXFqsmEjTwwvjd.Contains(YjlEoVwLrCFlMZxGRUgOwaQjuF.info
                         .displayName.english) ||
                     HGJfIWGOkTcdLbGIFiAb.yBPzMbzsaiBVjicaXFqsmEjTwwvjd.Contains(YjlEoVwLrCFlMZxGRUgOwaQjuF.info
                         .shortname)) || XAXfVFPbfUI.ContainsKey(YjlEoVwLrCFlMZxGRUgOwaQjuF.info.category.ToString()) &&
                    XAXfVFPbfUI[YjlEoVwLrCFlMZxGRUgOwaQjuF.info.category.ToString()] == 2)
                {
                    if (YjlEoVwLrCFlMZxGRUgOwaQjuF.amount > 1 ||
                        PVhltAKgoQ(unsgxNVqXKqqECsXQrG, YjlEoVwLrCFlMZxGRUgOwaQjuF, targetPos))
                        return ItemContainer.CanAcceptResult.CannotAccept;
                }
            }

            return null;
        }

        private bool? CanStackItem(Item YjlEoVwLrCFlMZxGRUgOwaQjuF, Item mCDDdYSVLSWfHUkXrtG)
        {
            if (HGJfIWGOkTcdLbGIFiAb.eyEnBlUwiSVEbqTYsOoAWSlbYAKqY.Contains(YjlEoVwLrCFlMZxGRUgOwaQjuF.skin) ||
                HGJfIWGOkTcdLbGIFiAb.eyEnBlUwiSVEbqTYsOoAWSlbYAKqY.Contains(mCDDdYSVLSWfHUkXrtG.skin)) return null;
            if (HGJfIWGOkTcdLbGIFiAb.SUMgbOLYXEWqWGNauhbgEajpmt.Contains(YjlEoVwLrCFlMZxGRUgOwaQjuF.info.displayName
                    .english) ||
                HGJfIWGOkTcdLbGIFiAb.SUMgbOLYXEWqWGNauhbgEajpmt.Contains(mCDDdYSVLSWfHUkXrtG.info.displayName.english))
                return null;
            if (HGJfIWGOkTcdLbGIFiAb.SUMgbOLYXEWqWGNauhbgEajpmt.Contains(YjlEoVwLrCFlMZxGRUgOwaQjuF.info.shortname) ||
                HGJfIWGOkTcdLbGIFiAb.SUMgbOLYXEWqWGNauhbgEajpmt.Contains(mCDDdYSVLSWfHUkXrtG.info.shortname))
                return null;
            if (YjlEoVwLrCFlMZxGRUgOwaQjuF == mCDDdYSVLSWfHUkXrtG) return false;
            if (YjlEoVwLrCFlMZxGRUgOwaQjuF.info.stackable <= 1 || mCDDdYSVLSWfHUkXrtG.info.stackable <= 1) return false;
            if (YjlEoVwLrCFlMZxGRUgOwaQjuF.info.itemid != mCDDdYSVLSWfHUkXrtG.info.itemid) return false;
            if ((YjlEoVwLrCFlMZxGRUgOwaQjuF.hasCondition || mCDDdYSVLSWfHUkXrtG.hasCondition) &&
                YjlEoVwLrCFlMZxGRUgOwaQjuF.condition != mCDDdYSVLSWfHUkXrtG.condition) return false;
            if (YjlEoVwLrCFlMZxGRUgOwaQjuF.skin != mCDDdYSVLSWfHUkXrtG.skin) return false;
            if (YjlEoVwLrCFlMZxGRUgOwaQjuF.name != mCDDdYSVLSWfHUkXrtG.name) return false;
            if (!YjlEoVwLrCFlMZxGRUgOwaQjuF.IsValid()) return false;
            if (YjlEoVwLrCFlMZxGRUgOwaQjuF.IsBlueprint() &&
                YjlEoVwLrCFlMZxGRUgOwaQjuF.blueprintTarget != mCDDdYSVLSWfHUkXrtG.blueprintTarget) return false;
            return true;
        }

        private bool? CanCombineDroppedItem(DroppedItem drItem, DroppedItem anotherDrItem)
        {
            var item = drItem.item;
            var anotherItem = anotherDrItem.item;
            return CanStackItem(item, anotherItem) == false ? false : (bool?) null;
        }

        private Item OnItemSplit(Item YjlEoVwLrCFlMZxGRUgOwaQjuF, int amount)
        {
            if (HGJfIWGOkTcdLbGIFiAb.eyEnBlUwiSVEbqTYsOoAWSlbYAKqY.Contains(YjlEoVwLrCFlMZxGRUgOwaQjuF.skin))
                return null;
            if (HGJfIWGOkTcdLbGIFiAb.SUMgbOLYXEWqWGNauhbgEajpmt.Contains(YjlEoVwLrCFlMZxGRUgOwaQjuF.info.displayName
                .english)) return null;
            if (HGJfIWGOkTcdLbGIFiAb.SUMgbOLYXEWqWGNauhbgEajpmt.Contains(YjlEoVwLrCFlMZxGRUgOwaQjuF.info.shortname))
                return null;
            YjlEoVwLrCFlMZxGRUgOwaQjuF.amount = YjlEoVwLrCFlMZxGRUgOwaQjuF.amount - amount;
            Item AHeQIspHDiVUzImA = ItemManager.CreateByItemID(YjlEoVwLrCFlMZxGRUgOwaQjuF.info.itemid, 1,
                YjlEoVwLrCFlMZxGRUgOwaQjuF.skin);
            AHeQIspHDiVUzImA.amount = amount;
            AHeQIspHDiVUzImA.condition = YjlEoVwLrCFlMZxGRUgOwaQjuF.condition;
            AHeQIspHDiVUzImA.name = YjlEoVwLrCFlMZxGRUgOwaQjuF.name;
            if (YjlEoVwLrCFlMZxGRUgOwaQjuF.IsBlueprint())
                AHeQIspHDiVUzImA.blueprintTarget = YjlEoVwLrCFlMZxGRUgOwaQjuF.blueprintTarget;
            YjlEoVwLrCFlMZxGRUgOwaQjuF.MarkDirty();
            return AHeQIspHDiVUzImA;
        }

        private object CanMoveItem(Item YjlEoVwLrCFlMZxGRUgOwaQjuF, PlayerInventory GSTUBUXwFSqBtKpaHlcmtUFsMoMGO,
            ItemContainerId unsgxNVqXKqqECsXQrG, int eXWOKZqloRYeEsnBgGstPrs, int qwsSuJWipQvJCvxZUC)
        {
            if (YjlEoVwLrCFlMZxGRUgOwaQjuF == null || GSTUBUXwFSqBtKpaHlcmtUFsMoMGO == null ||
                YjlEoVwLrCFlMZxGRUgOwaQjuF.amount < UInt16.MaxValue ||
                !HGJfIWGOkTcdLbGIFiAb.DUEChqBZGepQiXogDYihyz) return null;
            ItemContainer RuRhvPvkhs = GSTUBUXwFSqBtKpaHlcmtUFsMoMGO.FindContainer(unsgxNVqXKqqECsXQrG);
            if (RuRhvPvkhs == null) return null;
            ItemContainer bSfvWepLNmkebpSjeqBmmZKFzuf =
                GSTUBUXwFSqBtKpaHlcmtUFsMoMGO.GetContainer(PlayerInventory.Type.Main);
            BasePlayer UmdgJUdsICQjCLNCmVpJFPpCG = bSfvWepLNmkebpSjeqBmmZKFzuf?.GetOwnerPlayer();
            if (UmdgJUdsICQjCLNCmVpJFPpCG != null && FurnaceSplitter != null)
            {
                bool pcmqfqqQKjmfuGgPcn = true;
                bool pgPvNLEKEGR = false;
                bool UalgdHChFvtGdlJe = true;
                try
                {
                    pgPvNLEKEGR = (bool) FurnaceSplitter?.CallHook("GetEnabled", UmdgJUdsICQjCLNCmVpJFPpCG);
                    UalgdHChFvtGdlJe = (bool) FurnaceSplitter?.CallHook("HasPermission", UmdgJUdsICQjCLNCmVpJFPpCG);
                }
                catch
                {
                    pcmqfqqQKjmfuGgPcn = false;
                }

                if (pcmqfqqQKjmfuGgPcn && pgPvNLEKEGR && UalgdHChFvtGdlJe)
                {
                    BaseEntity opShpkXtBmBzygHxpNSnNeWg = RuRhvPvkhs.entityOwner;
                    if (opShpkXtBmBzygHxpNSnNeWg is BaseOven &&
                        (opShpkXtBmBzygHxpNSnNeWg as BaseOven).inventory.capacity > 1) return null;
                }
            }

            bool CeVnlKlOajCsMbMtjfSTzgDBpwWq = false;
            int rsibjVgyWEwINPELZzHUqRHhBWXPS =
                HGJfIWGOkTcdLbGIFiAb.AqfUFopQwAbfNey[YjlEoVwLrCFlMZxGRUgOwaQjuF.info.category.ToString()][
                    YjlEoVwLrCFlMZxGRUgOwaQjuF.info.displayName.english];
            if (YjlEoVwLrCFlMZxGRUgOwaQjuF.amount > rsibjVgyWEwINPELZzHUqRHhBWXPS) CeVnlKlOajCsMbMtjfSTzgDBpwWq = true;
            if (qwsSuJWipQvJCvxZUC + YjlEoVwLrCFlMZxGRUgOwaQjuF.amount / UInt16.MaxValue ==
                YjlEoVwLrCFlMZxGRUgOwaQjuF.amount % UInt16.MaxValue)
            {
                if (CeVnlKlOajCsMbMtjfSTzgDBpwWq)
                {
                    Item mCDDdYSVLSWfHUkXrtG = YjlEoVwLrCFlMZxGRUgOwaQjuF.SplitItem(rsibjVgyWEwINPELZzHUqRHhBWXPS);
                    if (!mCDDdYSVLSWfHUkXrtG.MoveToContainer(RuRhvPvkhs, eXWOKZqloRYeEsnBgGstPrs, true))
                    {
                        YjlEoVwLrCFlMZxGRUgOwaQjuF.amount += mCDDdYSVLSWfHUkXrtG.amount;
                        mCDDdYSVLSWfHUkXrtG.Remove(0f);
                    }

                    ItemManager.DoRemoves();
                    GSTUBUXwFSqBtKpaHlcmtUFsMoMGO.ServerUpdate(0f);
                    return true;
                }

                YjlEoVwLrCFlMZxGRUgOwaQjuF.MoveToContainer(RuRhvPvkhs, eXWOKZqloRYeEsnBgGstPrs, true);
                var KUmjsLKQxNcXUdjHocfeMAHh = "{DarkPluginsID}";
                return true;
            }
            else if (qwsSuJWipQvJCvxZUC + (YjlEoVwLrCFlMZxGRUgOwaQjuF.amount / 2) / UInt16.MaxValue ==
                     (YjlEoVwLrCFlMZxGRUgOwaQjuF.amount / 2) % UInt16.MaxValue + YjlEoVwLrCFlMZxGRUgOwaQjuF.amount % 2)
            {
                if (CeVnlKlOajCsMbMtjfSTzgDBpwWq)
                {
                    Item QBoFejCxYuHtQXGaNeODbDRTaq;
                    if (rsibjVgyWEwINPELZzHUqRHhBWXPS > YjlEoVwLrCFlMZxGRUgOwaQjuF.amount / 2)
                        QBoFejCxYuHtQXGaNeODbDRTaq =
                            YjlEoVwLrCFlMZxGRUgOwaQjuF.SplitItem(Convert.ToInt32(YjlEoVwLrCFlMZxGRUgOwaQjuF.amount) /
                                                                 2);
                    else
                        QBoFejCxYuHtQXGaNeODbDRTaq =
                            YjlEoVwLrCFlMZxGRUgOwaQjuF.SplitItem(rsibjVgyWEwINPELZzHUqRHhBWXPS);
                    if (!QBoFejCxYuHtQXGaNeODbDRTaq.MoveToContainer(RuRhvPvkhs, eXWOKZqloRYeEsnBgGstPrs, true))
                    {
                        YjlEoVwLrCFlMZxGRUgOwaQjuF.amount += QBoFejCxYuHtQXGaNeODbDRTaq.amount;
                        QBoFejCxYuHtQXGaNeODbDRTaq.Remove(0f);
                    }

                    ItemManager.DoRemoves();
                    GSTUBUXwFSqBtKpaHlcmtUFsMoMGO.ServerUpdate(0f);
                    return true;
                }

                Item mCDDdYSVLSWfHUkXrtG = YjlEoVwLrCFlMZxGRUgOwaQjuF.SplitItem(YjlEoVwLrCFlMZxGRUgOwaQjuF.amount / 2);
                if ((YjlEoVwLrCFlMZxGRUgOwaQjuF.amount + mCDDdYSVLSWfHUkXrtG.amount) % 2 != 0)
                {
                    mCDDdYSVLSWfHUkXrtG.amount++;
                    YjlEoVwLrCFlMZxGRUgOwaQjuF.amount--;
                }

                if (!mCDDdYSVLSWfHUkXrtG.MoveToContainer(RuRhvPvkhs, eXWOKZqloRYeEsnBgGstPrs, true))
                {
                    YjlEoVwLrCFlMZxGRUgOwaQjuF.amount += mCDDdYSVLSWfHUkXrtG.amount;
                    mCDDdYSVLSWfHUkXrtG.Remove(0f);
                }

                ItemManager.DoRemoves();
                GSTUBUXwFSqBtKpaHlcmtUFsMoMGO.ServerUpdate(0f);
                return true;
            }

            return null;
        }

        private static bool tcChzNBoJCMHBaBQGcpnAUGuamBU(ItemDefinition DedPMrncJeUMztmDCKZLnpLDRqmdZ)
        {
            if (DedPMrncJeUMztmDCKZLnpLDRqmdZ == null) return false;
            return DedPMrncJeUMztmDCKZLnpLDRqmdZ.condition.enabled && DedPMrncJeUMztmDCKZLnpLDRqmdZ.condition.max > 0f;
        }

        private bool PVhltAKgoQ(ItemContainer unsgxNVqXKqqECsXQrG, Item mCDDdYSVLSWfHUkXrtG, int targetPos)
        {
            foreach (var item in unsgxNVqXKqqECsXQrG.itemList.Where(x =>
                x != null && (targetPos == -1 || targetPos == x.position)))
            {
                if (CanStackItem(item, mCDDdYSVLSWfHUkXrtG) == true) return true;
            }

            return false;
        }

        private Dictionary<string, Dictionary<string, int>> ifyKXmaPzFnpcsUnGeoC()
        {
            var wJtvQOWXkUHbZbbWCpdvQNDUoSEu = new Dictionary<string, Dictionary<string, int>>();
            var VrMWZJkfdoElpJhbWIEDRDKd = ItemCategory.Weapon;
            var kvbKJrUQRpGKuFuEXXg = new Dictionary<string, int>();
            foreach (var mCDDdYSVLSWfHUkXrtG in ItemManager.itemList.OrderBy(r => r.category))
            {
                if (VrMWZJkfdoElpJhbWIEDRDKd != mCDDdYSVLSWfHUkXrtG.category && kvbKJrUQRpGKuFuEXXg.Count > 0)
                {
                    wJtvQOWXkUHbZbbWCpdvQNDUoSEu.Add($"{VrMWZJkfdoElpJhbWIEDRDKd}",
                        new Dictionary<string, int>(kvbKJrUQRpGKuFuEXXg.OrderBy(x => x.Key)));
                    kvbKJrUQRpGKuFuEXXg.Clear();
                }

                if (!kvbKJrUQRpGKuFuEXXg.ContainsKey(mCDDdYSVLSWfHUkXrtG.displayName.english))
                    kvbKJrUQRpGKuFuEXXg.Add(mCDDdYSVLSWfHUkXrtG.displayName.english, mCDDdYSVLSWfHUkXrtG.stackable);
                VrMWZJkfdoElpJhbWIEDRDKd = mCDDdYSVLSWfHUkXrtG.category;
            }

            if (kvbKJrUQRpGKuFuEXXg.Count > 0)
                wJtvQOWXkUHbZbbWCpdvQNDUoSEu.Add($"{VrMWZJkfdoElpJhbWIEDRDKd}",
                    new Dictionary<string, int>(kvbKJrUQRpGKuFuEXXg));
            return wJtvQOWXkUHbZbbWCpdvQNDUoSEu.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        }

        private static mnGUBGwBziEpAOYoFxtHiEXpYHzc HGJfIWGOkTcdLbGIFiAb;

        private class mnGUBGwBziEpAOYoFxtHiEXpYHzc
        {
            [JsonProperty(PropertyName = "Разрешить корректный перенос стаков больше 64К")]
            public bool DUEChqBZGepQiXogDYihyz;

            [JsonProperty(PropertyName = "Стаки предметов по категориям")]
            public Dictionary<string, Dictionary<string, int>> AqfUFopQwAbfNey;

            [JsonProperty(PropertyName =
                "Предметы которым принудительно разрешено стакаться в слотах быстрого доступа")]
            public List<string> RSjYaftfLwPnVesiMQltPxD;

            [JsonProperty(PropertyName =
                "Предметы которым принудительно запрещено стакаться в слотах быстрого доступа")]
            public List<string> yBPzMbzsaiBVjicaXFqsmEjTwwvjd;

            [JsonProperty(PropertyName =
                "Скины предметов которые не нужно обрабатывать плагином при стаке и разделении (для исключения конфликтов)")]
            public List<ulong> eyEnBlUwiSVEbqTYsOoAWSlbYAKqY;

            [JsonProperty(PropertyName =
                "Названия предметов которые не нужно обрабатывать плагином при стаке и разделении (для исключения конфликтов)")]
            public List<string> SUMgbOLYXEWqWGNauhbgEajpmt;
        }

        private void MFbOuyfXAqPnQUkGOaKBezo() => HGJfIWGOkTcdLbGIFiAb = Config.ReadObject<mnGUBGwBziEpAOYoFxtHiEXpYHzc>();

        protected override void LoadDefaultConfig()
        {
            if (ItemManager.itemList == null)
            {
                timer.Once(5f, () => LoadDefaultConfig());
                return;
            }

            HGJfIWGOkTcdLbGIFiAb = new mnGUBGwBziEpAOYoFxtHiEXpYHzc
            {
                DUEChqBZGepQiXogDYihyz = true, AqfUFopQwAbfNey = ifyKXmaPzFnpcsUnGeoC(),
                RSjYaftfLwPnVesiMQltPxD = new List<string>(),
                yBPzMbzsaiBVjicaXFqsmEjTwwvjd = new List<string>() {"Blue Keycard", "Green Keycard", "Red Keycard"},
                eyEnBlUwiSVEbqTYsOoAWSlbYAKqY = new List<ulong>(), SUMgbOLYXEWqWGNauhbgEajpmt = new List<string>()
            };
            qqOsjhDfbcTCbeFmh(HGJfIWGOkTcdLbGIFiAb);
            timer.Once(0.1f, () => qqOsjhDfbcTCbeFmh(HGJfIWGOkTcdLbGIFiAb));
        }

        private void qqOsjhDfbcTCbeFmh(mnGUBGwBziEpAOYoFxtHiEXpYHzc pnkxntVfNGmAQWTSSYCf) =>
        Config.WriteObject(pnkxntVfNGmAQWTSSYCf, true);
    }
}