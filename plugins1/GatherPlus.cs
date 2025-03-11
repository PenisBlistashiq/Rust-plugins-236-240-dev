using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
namespace Oxide.Plugins
{
    [Info("GatherPlus", "Nimant", "1.0.5")]
    class GatherPlus : RustPlugin
    {
        private Dictionary<ulong, List<string>> UONNSlDROwmKGBeSoUv =
            new Dictionary<ulong, List<string>>();
        private bool atUQdwQgWQwDVVxnVH = false;
        private void Init()
        {
            kCTDaHDLMefg();
            if (qTtgPjwYKnWYBUNyiuvPvoseE.PUVWEFwfSJClOtd == null)
            {
                qTtgPjwYKnWYBUNyiuvPvoseE.PsFLvHeYGRGANi = 1f;
                qTtgPjwYKnWYBUNyiuvPvoseE.gZzyZdaaHkeIpTcdloHeZGOfZFXCoM = 1f;
                qTtgPjwYKnWYBUNyiuvPvoseE.PUVWEFwfSJClOtd =
                    new Dictionary<string, float>(){{"High Quality Metal Ore", 1f},
                                            {"Metal Fragments", 1f},
                                            {"Metal Ore", 1f},
                                            {"Stones", 1f},
                                            {"Sulfur Ore", 1f}};
                KfNCtHhEvItseKXhSI(qTtgPjwYKnWYBUNyiuvPvoseE);
            }
            LoadDefaultMessages();
            foreach (var SBgXTKRbCcfGhtDAvDo in qTtgPjwYKnWYBUNyiuvPvoseE
                         .idzSnoRLkDWmagCKFRjFD.Keys)
                permission.RegisterPermission(SBgXTKRbCcfGhtDAvDo.ToLower(), this);
        }
        private void OnPlayerConnected(BasePlayer JzaicyHgTyvBaIawgTGFZreSPKQ) =>
            LBkxdbJgEcUHBmisnUfsEj(JzaicyHgTyvBaIawgTGFZreSPKQ.UserIDString);
        private void OnPlayerDisconnected(BasePlayer JzaicyHgTyvBaIawgTGFZreSPKQ,
                                          string PnmeyTOhWfqraLCYNdSSVTLe) =>
            UONNSlDROwmKGBeSoUv.Remove(JzaicyHgTyvBaIawgTGFZreSPKQ.userID);
        private void OnServerInitialized()
        {
            vPJdKZTUlvkhAExigsEikpEssMhro(true);
            foreach (var JzaicyHgTyvBaIawgTGFZreSPKQ in BasePlayer.activePlayerList)
                OnPlayerConnected(JzaicyHgTyvBaIawgTGFZreSPKQ);
        }
        private void OnDispenserGather(ResourceDispenser jXnmhrPeEOmBqrvOAt,
                                       BaseEntity exHsywFmCNoEdM,
                                       Item qjPZQlhETObKtkMQEiMPn)
        {
            if (exHsywFmCNoEdM == null || qjPZQlhETObKtkMQEiMPn == null) return;
            BasePlayer JzaicyHgTyvBaIawgTGFZreSPKQ = exHsywFmCNoEdM.ToPlayer();
            if (JzaicyHgTyvBaIawgTGFZreSPKQ == null) return;
            sCxPBgYnMvjhPkcQGjyGHxikeT(qjPZQlhETObKtkMQEiMPn);
            var LETNtWMlElVINMgtBiKEiXDeLRl =
                UehfgiVlDcZveNuSnaelNyC(JzaicyHgTyvBaIawgTGFZreSPKQ);
            var rddqaMSDcDdMgicgBsqhWpspImKIpB =
                ywcySQqgapryxMGgSCLdHHXmsqDqB(JzaicyHgTyvBaIawgTGFZreSPKQ.userID);
            if (rddqaMSDcDdMgicgBsqhWpspImKIpB < 0) return;
            DADrbIgFzOcZQS(qjPZQlhETObKtkMQEiMPn,
                           rddqaMSDcDdMgicgBsqhWpspImKIpB *
                               qTtgPjwYKnWYBUNyiuvPvoseE.MUOsODqppJydk
                                   [qjPZQlhETObKtkMQEiMPn.info.displayName.english] *
                               (LETNtWMlElVINMgtBiKEiXDeLRl !=
                                null ? qTtgPjwYKnWYBUNyiuvPvoseE
                                    .xMafmgITlbhQDRH[LETNtWMlElVINMgtBiKEiXDeLRl.info
                                                         .displayName.english] : 1f));
        }
        private void OnDispenserBonus(ResourceDispenser jXnmhrPeEOmBqrvOAt,
                                      BasePlayer JzaicyHgTyvBaIawgTGFZreSPKQ,
                                      Item qjPZQlhETObKtkMQEiMPn)
        {
            if (JzaicyHgTyvBaIawgTGFZreSPKQ == null || qjPZQlhETObKtkMQEiMPn == null)
                return;
            sCxPBgYnMvjhPkcQGjyGHxikeT(qjPZQlhETObKtkMQEiMPn);
            var LETNtWMlElVINMgtBiKEiXDeLRl =
                UehfgiVlDcZveNuSnaelNyC(JzaicyHgTyvBaIawgTGFZreSPKQ);
            var rddqaMSDcDdMgicgBsqhWpspImKIpB =
                ywcySQqgapryxMGgSCLdHHXmsqDqB(JzaicyHgTyvBaIawgTGFZreSPKQ.userID);
            if (rddqaMSDcDdMgicgBsqhWpspImKIpB < 0) return;
            DADrbIgFzOcZQS(qjPZQlhETObKtkMQEiMPn,
                           rddqaMSDcDdMgicgBsqhWpspImKIpB *
                               qTtgPjwYKnWYBUNyiuvPvoseE.MUOsODqppJydk
                                   [qjPZQlhETObKtkMQEiMPn.info.displayName.english] *
                               (LETNtWMlElVINMgtBiKEiXDeLRl !=
                                null ? qTtgPjwYKnWYBUNyiuvPvoseE
                                    .xMafmgITlbhQDRH[LETNtWMlElVINMgtBiKEiXDeLRl.info
                                                         .displayName.english] : 1f));
        }
        private void OnCollectiblePickup(Item qjPZQlhETObKtkMQEiMPn,
                                         BasePlayer JzaicyHgTyvBaIawgTGFZreSPKQ)
        {
            if (JzaicyHgTyvBaIawgTGFZreSPKQ == null || qjPZQlhETObKtkMQEiMPn == null)
                return;
            PkGOpnkoiOGCvhALH(qjPZQlhETObKtkMQEiMPn);
            var rddqaMSDcDdMgicgBsqhWpspImKIpB =
                pDrCSrAbdExdwngdVODpvtb(JzaicyHgTyvBaIawgTGFZreSPKQ.userID);
            if (rddqaMSDcDdMgicgBsqhWpspImKIpB < 0) return;
            DADrbIgFzOcZQS(qjPZQlhETObKtkMQEiMPn,
                           rddqaMSDcDdMgicgBsqhWpspImKIpB *
                               qTtgPjwYKnWYBUNyiuvPvoseE.vaxPCujkLFIzsCAJxXNfamVa
                                   [qjPZQlhETObKtkMQEiMPn.info.displayName.english]);
        }
        private void OnGrowableGathered(GrowableEntity HzZHaXgaMeLSOfoRmoQsiIPdHd,
                                        Item qjPZQlhETObKtkMQEiMPn,
                                        BasePlayer JzaicyHgTyvBaIawgTGFZreSPKQ)
        {
            if (HzZHaXgaMeLSOfoRmoQsiIPdHd == null ||
                JzaicyHgTyvBaIawgTGFZreSPKQ == null || qjPZQlhETObKtkMQEiMPn == null)
                return;
            PkGOpnkoiOGCvhALH(qjPZQlhETObKtkMQEiMPn);
            var rddqaMSDcDdMgicgBsqhWpspImKIpB =
                pDrCSrAbdExdwngdVODpvtb(JzaicyHgTyvBaIawgTGFZreSPKQ.userID);
            if (rddqaMSDcDdMgicgBsqhWpspImKIpB < 0) return;
            DADrbIgFzOcZQS(qjPZQlhETObKtkMQEiMPn,
                           rddqaMSDcDdMgicgBsqhWpspImKIpB *
                               qTtgPjwYKnWYBUNyiuvPvoseE.vaxPCujkLFIzsCAJxXNfamVa
                                   [qjPZQlhETObKtkMQEiMPn.info.displayName.english]);
        }
        private void OnQuarryGather(MiningQuarry BhLwELTCFcBwWERJflH,
                                    Item qjPZQlhETObKtkMQEiMPn)
        {
            if (BhLwELTCFcBwWERJflH == null || qjPZQlhETObKtkMQEiMPn == null) return;
            ISKKUbZJRksAU(qjPZQlhETObKtkMQEiMPn);
            var rddqaMSDcDdMgicgBsqhWpspImKIpB =
                SgGHoCHrldghcbMajVaX(BhLwELTCFcBwWERJflH.OwnerID);
            if (rddqaMSDcDdMgicgBsqhWpspImKIpB < 0) return;
            DADrbIgFzOcZQS(qjPZQlhETObKtkMQEiMPn,
                           rddqaMSDcDdMgicgBsqhWpspImKIpB *
                               qTtgPjwYKnWYBUNyiuvPvoseE.XNwjoDSIKjufWWBKHcwbNzcIEk
                                   [qjPZQlhETObKtkMQEiMPn.info.displayName.english]);
        }
        private void OnExcavatorGather(ExcavatorArm FCdGzmrUYReaHZlIVaGqfCu,
                                       Item qjPZQlhETObKtkMQEiMPn)
        {
            if (FCdGzmrUYReaHZlIVaGqfCu == null || qjPZQlhETObKtkMQEiMPn == null)
                return;
            QczwIfgoxUXcfIUFiRhqijMfStT(qjPZQlhETObKtkMQEiMPn);
            var rddqaMSDcDdMgicgBsqhWpspImKIpB = GetExcavRate();
            if (rddqaMSDcDdMgicgBsqhWpspImKIpB < 0) return;
            DADrbIgFzOcZQS(qjPZQlhETObKtkMQEiMPn,
                           rddqaMSDcDdMgicgBsqhWpspImKIpB *
                               qTtgPjwYKnWYBUNyiuvPvoseE.PUVWEFwfSJClOtd
                                   [qjPZQlhETObKtkMQEiMPn.info.displayName.english]);
        }
        private void OnUserPermissionGranted(string gLxXOvxsQqMXia,
                                             string SBgXTKRbCcfGhtDAvDo)
        {
            if (qTtgPjwYKnWYBUNyiuvPvoseE.idzSnoRLkDWmagCKFRjFD
                    .Where(x => x.Key.ToLower() == SBgXTKRbCcfGhtDAvDo.ToLower())
                    .Count() > 0)
                timer.Once(0.1f, () => LBkxdbJgEcUHBmisnUfsEj(gLxXOvxsQqMXia));
        }
        private void OnUserPermissionRevoked(string gLxXOvxsQqMXia,
                                             string SBgXTKRbCcfGhtDAvDo)
        {
            if (qTtgPjwYKnWYBUNyiuvPvoseE.idzSnoRLkDWmagCKFRjFD
                    .Where(x => x.Key.ToLower() == SBgXTKRbCcfGhtDAvDo.ToLower())
                    .Count() > 0)
                timer.Once(0.1f, () => LBkxdbJgEcUHBmisnUfsEj(gLxXOvxsQqMXia));
        }
        private void OnUserGroupAdded(string gLxXOvxsQqMXia,
                                      string jlcXHUDXTlmOMIKOEfKvvivI) =>
            timer.Once(0.1f, () => LBkxdbJgEcUHBmisnUfsEj(gLxXOvxsQqMXia));
        private void OnUserGroupRemoved(string gLxXOvxsQqMXia,
                                        string jlcXHUDXTlmOMIKOEfKvvivI) =>
            timer.Once(0.1f, () => LBkxdbJgEcUHBmisnUfsEj(gLxXOvxsQqMXia));
        private void OnGroupPermissionGranted(string jlcXHUDXTlmOMIKOEfKvvivI,
                                              string SBgXTKRbCcfGhtDAvDo)
        {
            if (qTtgPjwYKnWYBUNyiuvPvoseE.idzSnoRLkDWmagCKFRjFD
                    .Where(x => x.Key.ToLower() == SBgXTKRbCcfGhtDAvDo.ToLower())
                    .Count() > 0)
                timer.Once(0.1f, () => CKOJUQKDOVWuTDHLiZcTjtEUppocEV(
                                     jlcXHUDXTlmOMIKOEfKvvivI));
        }
        private void OnGroupPermissionRevoked(string jlcXHUDXTlmOMIKOEfKvvivI,
                                              string SBgXTKRbCcfGhtDAvDo)
        {
            if (qTtgPjwYKnWYBUNyiuvPvoseE.idzSnoRLkDWmagCKFRjFD
                    .Where(x => x.Key.ToLower() == SBgXTKRbCcfGhtDAvDo.ToLower())
                    .Count() > 0)
                timer.Once(0.1f, () => CKOJUQKDOVWuTDHLiZcTjtEUppocEV(
                                     jlcXHUDXTlmOMIKOEfKvvivI));
        }
        private void sCxPBgYnMvjhPkcQGjyGHxikeT(Item qjPZQlhETObKtkMQEiMPn)
        {
            if (!qTtgPjwYKnWYBUNyiuvPvoseE.MUOsODqppJydk.ContainsKey(
                    qjPZQlhETObKtkMQEiMPn.info.displayName.english))
            {
                qTtgPjwYKnWYBUNyiuvPvoseE.MUOsODqppJydk.Add(
                    qjPZQlhETObKtkMQEiMPn.info.displayName.english, 1f);
                KfNCtHhEvItseKXhSI(qTtgPjwYKnWYBUNyiuvPvoseE);
                PrintWarning(string.Format(
                    "В конфигурационный файл, в раздел добываемых ресурсов добавлен новый ресурс '{0}'",
                    qjPZQlhETObKtkMQEiMPn.info.displayName.english));
            }
        }
        private Item UehfgiVlDcZveNuSnaelNyC(
            BasePlayer JzaicyHgTyvBaIawgTGFZreSPKQ)
        {
            if (JzaicyHgTyvBaIawgTGFZreSPKQ == null) return null;
            var qjPZQlhETObKtkMQEiMPn = JzaicyHgTyvBaIawgTGFZreSPKQ.GetActiveItem();
            if (qjPZQlhETObKtkMQEiMPn == null) return null;
            if (qTtgPjwYKnWYBUNyiuvPvoseE.xMafmgITlbhQDRH == null)
                qTtgPjwYKnWYBUNyiuvPvoseE.xMafmgITlbhQDRH =
                    new Dictionary<string, float>();
            if (!qTtgPjwYKnWYBUNyiuvPvoseE.xMafmgITlbhQDRH.ContainsKey(
                    qjPZQlhETObKtkMQEiMPn.info.displayName.english))
            {
                qTtgPjwYKnWYBUNyiuvPvoseE.xMafmgITlbhQDRH.Add(
                    qjPZQlhETObKtkMQEiMPn.info.displayName.english, 1f);
                KfNCtHhEvItseKXhSI(qTtgPjwYKnWYBUNyiuvPvoseE);
                PrintWarning(string.Format(
                    "В конфигурационный файл, в раздел инструментов добычи добавлен новый ресурс '{0}'",
                    qjPZQlhETObKtkMQEiMPn.info.displayName.english));
            }
            return qjPZQlhETObKtkMQEiMPn;
        }
        private void PkGOpnkoiOGCvhALH(Item qjPZQlhETObKtkMQEiMPn)
        {
            if (!qTtgPjwYKnWYBUNyiuvPvoseE.vaxPCujkLFIzsCAJxXNfamVa.ContainsKey(
                    qjPZQlhETObKtkMQEiMPn.info.displayName.english))
            {
                qTtgPjwYKnWYBUNyiuvPvoseE.vaxPCujkLFIzsCAJxXNfamVa.Add(
                    qjPZQlhETObKtkMQEiMPn.info.displayName.english, 1f);
                KfNCtHhEvItseKXhSI(qTtgPjwYKnWYBUNyiuvPvoseE);
                PrintWarning(string.Format(
                    "В конфигурационный файл, в раздел поднимаемых ресурсов добавлен новый ресурс '{0}'",
                    qjPZQlhETObKtkMQEiMPn.info.displayName.english));
            }
        }
        private void ISKKUbZJRksAU(Item qjPZQlhETObKtkMQEiMPn)
        {
            if (!qTtgPjwYKnWYBUNyiuvPvoseE.XNwjoDSIKjufWWBKHcwbNzcIEk.ContainsKey(
                    qjPZQlhETObKtkMQEiMPn.info.displayName.english))
            {
                qTtgPjwYKnWYBUNyiuvPvoseE.XNwjoDSIKjufWWBKHcwbNzcIEk.Add(
                    qjPZQlhETObKtkMQEiMPn.info.displayName.english, 1f);
                KfNCtHhEvItseKXhSI(qTtgPjwYKnWYBUNyiuvPvoseE);
                PrintWarning(string.Format(
                    "В конфигурационный файл, в раздел добываемых ресурсов в карьере добавлен новый ресурс '{0}'",
                    qjPZQlhETObKtkMQEiMPn.info.displayName.english));
            }
        }
        private void QczwIfgoxUXcfIUFiRhqijMfStT(Item qjPZQlhETObKtkMQEiMPn)
        {
            if (!qTtgPjwYKnWYBUNyiuvPvoseE.PUVWEFwfSJClOtd.ContainsKey(
                    qjPZQlhETObKtkMQEiMPn.info.displayName.english))
            {
                qTtgPjwYKnWYBUNyiuvPvoseE.PUVWEFwfSJClOtd.Add(
                    qjPZQlhETObKtkMQEiMPn.info.displayName.english, 1f);
                KfNCtHhEvItseKXhSI(qTtgPjwYKnWYBUNyiuvPvoseE);
                PrintWarning(string.Format(
                    "В конфигурационный файл, в раздел добываемых ресурсов экскаватором добавлен новый ресурс '{0}'",
                    qjPZQlhETObKtkMQEiMPn.info.displayName.english));
            }
        }
        private void DADrbIgFzOcZQS(Item qjPZQlhETObKtkMQEiMPn,
                                    float rddqaMSDcDdMgicgBsqhWpspImKIpB)
        {
            if (rddqaMSDcDdMgicgBsqhWpspImKIpB <= 0 ||
                qjPZQlhETObKtkMQEiMPn.amount <= 0)
            {
                qjPZQlhETObKtkMQEiMPn.RemoveFromContainer();
                qjPZQlhETObKtkMQEiMPn.Remove(0f);
            }
            else
            {
                var tVsWeTpwXbKbtTRbZHefuCWldbxuo = (int)Math.Round(
                    rddqaMSDcDdMgicgBsqhWpspImKIpB * qjPZQlhETObKtkMQEiMPn.amount);
                if (tVsWeTpwXbKbtTRbZHefuCWldbxuo <= 0)
                {
                    qjPZQlhETObKtkMQEiMPn.RemoveFromContainer();
                    qjPZQlhETObKtkMQEiMPn.Remove(0f);
                }
                else
                    qjPZQlhETObKtkMQEiMPn.amount = tVsWeTpwXbKbtTRbZHefuCWldbxuo;
            }
        }
        private float ywcySQqgapryxMGgSCLdHHXmsqDqB(ulong BgSUxMUYUlmjOoqYSeo)
        {
            float ywONrObzvTH =
                (qTtgPjwYKnWYBUNyiuvPvoseE.LXWbkykjGflIuDeAmwFZADbszXfGK &&
                 atUQdwQgWQwDVVxnVH)
                    ? qTtgPjwYKnWYBUNyiuvPvoseE.CBJrDKzahpDuGOWZsPulSotqJw
                    : qTtgPjwYKnWYBUNyiuvPvoseE.yMrwvjSeqkeL;
            if (!UONNSlDROwmKGBeSoUv.ContainsKey(BgSUxMUYUlmjOoqYSeo))
                return ywONrObzvTH;
            else
            {
                float rddqaMSDcDdMgicgBsqhWpspImKIpB = -1;
                foreach (var TxWqFWkSWdC in qTtgPjwYKnWYBUNyiuvPvoseE
                             .idzSnoRLkDWmagCKFRjFD.Where(
                                 x => UONNSlDROwmKGBeSoUv[BgSUxMUYUlmjOoqYSeo]
                                          .Contains(x.Key.ToLower())))
                {
                    if (qTtgPjwYKnWYBUNyiuvPvoseE.LXWbkykjGflIuDeAmwFZADbszXfGK &&
                        atUQdwQgWQwDVVxnVH)
                    {
                        if (rddqaMSDcDdMgicgBsqhWpspImKIpB <
                            TxWqFWkSWdC.Value.CBJrDKzahpDuGOWZsPulSotqJw)
                            rddqaMSDcDdMgicgBsqhWpspImKIpB =
                                TxWqFWkSWdC.Value.CBJrDKzahpDuGOWZsPulSotqJw;
                    }
                    else
                    {
                        if (rddqaMSDcDdMgicgBsqhWpspImKIpB < TxWqFWkSWdC.Value.yMrwvjSeqkeL)
                            rddqaMSDcDdMgicgBsqhWpspImKIpB = TxWqFWkSWdC.Value.yMrwvjSeqkeL;
                    }
                }
                return rddqaMSDcDdMgicgBsqhWpspImKIpB >= 0
                           ? rddqaMSDcDdMgicgBsqhWpspImKIpB
                           : ywONrObzvTH;
            }
            return ywONrObzvTH;
        }
        private float pDrCSrAbdExdwngdVODpvtb(ulong BgSUxMUYUlmjOoqYSeo)
        {
            float ywONrObzvTH =
                (qTtgPjwYKnWYBUNyiuvPvoseE.LXWbkykjGflIuDeAmwFZADbszXfGK &&
                 atUQdwQgWQwDVVxnVH)
                    ? qTtgPjwYKnWYBUNyiuvPvoseE.JjzcYPVFRHrgcNhWxsdxOSvyoPQBc
                    : qTtgPjwYKnWYBUNyiuvPvoseE.YWnNZGIjQfWbNXrrltThy;
            if (!UONNSlDROwmKGBeSoUv.ContainsKey(BgSUxMUYUlmjOoqYSeo))
                return ywONrObzvTH;
            else
            {
                float rddqaMSDcDdMgicgBsqhWpspImKIpB = -1;
                foreach (var TxWqFWkSWdC in qTtgPjwYKnWYBUNyiuvPvoseE
                             .idzSnoRLkDWmagCKFRjFD.Where(
                                 x => UONNSlDROwmKGBeSoUv[BgSUxMUYUlmjOoqYSeo]
                                          .Contains(x.Key.ToLower())))
                {
                    if (qTtgPjwYKnWYBUNyiuvPvoseE.LXWbkykjGflIuDeAmwFZADbszXfGK &&
                        atUQdwQgWQwDVVxnVH)
                    {
                        if (rddqaMSDcDdMgicgBsqhWpspImKIpB <
                            TxWqFWkSWdC.Value.JjzcYPVFRHrgcNhWxsdxOSvyoPQBc)
                            rddqaMSDcDdMgicgBsqhWpspImKIpB =
                                TxWqFWkSWdC.Value.JjzcYPVFRHrgcNhWxsdxOSvyoPQBc;
                    }
                    else
                    {
                        if (rddqaMSDcDdMgicgBsqhWpspImKIpB <
                            TxWqFWkSWdC.Value.YWnNZGIjQfWbNXrrltThy)
                            rddqaMSDcDdMgicgBsqhWpspImKIpB =
                                TxWqFWkSWdC.Value.YWnNZGIjQfWbNXrrltThy;
                    }
                }
                return rddqaMSDcDdMgicgBsqhWpspImKIpB >= 0
                           ? rddqaMSDcDdMgicgBsqhWpspImKIpB
                           : ywONrObzvTH;
            }
            return ywONrObzvTH;
        }
        private float SgGHoCHrldghcbMajVaX(ulong BgSUxMUYUlmjOoqYSeo)
        {
            float ywONrObzvTH =
                (qTtgPjwYKnWYBUNyiuvPvoseE.LXWbkykjGflIuDeAmwFZADbszXfGK &&
                 atUQdwQgWQwDVVxnVH)
                    ? qTtgPjwYKnWYBUNyiuvPvoseE.oQfBQfdbHrbFsxhaVZcalAbe
                    : qTtgPjwYKnWYBUNyiuvPvoseE.jBzPkWhIDwWHNidjgonsGnrhHxfm;
            if (BgSUxMUYUlmjOoqYSeo == 0) return ywONrObzvTH;
            List<string> WTjvdImYYai = new List<string>();
            foreach (var OCWErEycrpPoe in qTtgPjwYKnWYBUNyiuvPvoseE
                         .idzSnoRLkDWmagCKFRjFD.Keys)
                if (permission.UserHasPermission(BgSUxMUYUlmjOoqYSeo.ToString(),
                                                 OCWErEycrpPoe.ToLower()))
                    WTjvdImYYai.Add(OCWErEycrpPoe.ToLower());
            if (WTjvdImYYai.Count == 0) return ywONrObzvTH;
            float rddqaMSDcDdMgicgBsqhWpspImKIpB = -1;
            foreach (var TxWqFWkSWdC in qTtgPjwYKnWYBUNyiuvPvoseE
                         .idzSnoRLkDWmagCKFRjFD.Where(
                             x => WTjvdImYYai.Contains(x.Key.ToLower())))
            {
                if (qTtgPjwYKnWYBUNyiuvPvoseE.LXWbkykjGflIuDeAmwFZADbszXfGK &&
                    atUQdwQgWQwDVVxnVH)
                {
                    if (rddqaMSDcDdMgicgBsqhWpspImKIpB <
                        TxWqFWkSWdC.Value.oQfBQfdbHrbFsxhaVZcalAbe)
                        rddqaMSDcDdMgicgBsqhWpspImKIpB =
                            TxWqFWkSWdC.Value.oQfBQfdbHrbFsxhaVZcalAbe;
                }
                else
                {
                    if (rddqaMSDcDdMgicgBsqhWpspImKIpB <
                        TxWqFWkSWdC.Value.jBzPkWhIDwWHNidjgonsGnrhHxfm)
                        rddqaMSDcDdMgicgBsqhWpspImKIpB =
                            TxWqFWkSWdC.Value.jBzPkWhIDwWHNidjgonsGnrhHxfm;
                }
            }
            return rddqaMSDcDdMgicgBsqhWpspImKIpB >= 0
                       ? rddqaMSDcDdMgicgBsqhWpspImKIpB
                       : ywONrObzvTH;
        }
        private float GetExcavRate()
        {
            float ywONrObzvTH =
                (qTtgPjwYKnWYBUNyiuvPvoseE.LXWbkykjGflIuDeAmwFZADbszXfGK &&
                 atUQdwQgWQwDVVxnVH)
                    ? qTtgPjwYKnWYBUNyiuvPvoseE.gZzyZdaaHkeIpTcdloHeZGOfZFXCoM
                    : qTtgPjwYKnWYBUNyiuvPvoseE.PsFLvHeYGRGANi;
            return ywONrObzvTH;
        }
        private void LBkxdbJgEcUHBmisnUfsEj(string userIdString)
        {
            List<string> eBODtjETJqoSzLmWXZpQvJhkWeNVe = new List<string>();
            var OXIQIfcZdayKvIjLqhPCQTYPpRurj = "{DarkPluginsID}";
            foreach (var OCWErEycrpPoe in qTtgPjwYKnWYBUNyiuvPvoseE
                         .idzSnoRLkDWmagCKFRjFD.Keys)
                if (permission.UserHasPermission(userIdString, OCWErEycrpPoe.ToLower()))
                    eBODtjETJqoSzLmWXZpQvJhkWeNVe.Add(OCWErEycrpPoe.ToLower());
            ulong BgSUxMUYUlmjOoqYSeo = (ulong)Convert.ToInt64(userIdString);
            if (eBODtjETJqoSzLmWXZpQvJhkWeNVe.Count == 0)
            {
                if (UONNSlDROwmKGBeSoUv.ContainsKey(BgSUxMUYUlmjOoqYSeo))
                    UONNSlDROwmKGBeSoUv.Remove(BgSUxMUYUlmjOoqYSeo);
            }
            else
            {
                if (!UONNSlDROwmKGBeSoUv.ContainsKey(BgSUxMUYUlmjOoqYSeo))
                    UONNSlDROwmKGBeSoUv.Add(BgSUxMUYUlmjOoqYSeo,
                                            eBODtjETJqoSzLmWXZpQvJhkWeNVe);
                else
                    UONNSlDROwmKGBeSoUv[BgSUxMUYUlmjOoqYSeo] =
                        eBODtjETJqoSzLmWXZpQvJhkWeNVe;
            }
        }
        private void CKOJUQKDOVWuTDHLiZcTjtEUppocEV(
            string VcUqqMkHproqbZzPwvspQtJug)
        {
            List<string> yQmYAlyldGkAfVTTGrGiuPeux =
                permission.GetUsersInGroup(VcUqqMkHproqbZzPwvspQtJug)
                    .Select(x => x.Substring(0, x.IndexOf(" ")))
                    .ToList();
            foreach (var JzaicyHgTyvBaIawgTGFZreSPKQ in BasePlayer.activePlayerList
                         .Where(
                             x => yQmYAlyldGkAfVTTGrGiuPeux.Contains(x.UserIDString)))
                LBkxdbJgEcUHBmisnUfsEj(JzaicyHgTyvBaIawgTGFZreSPKQ.UserIDString);
        }
        private void vPJdKZTUlvkhAExigsEikpEssMhro(
            bool HGTxIzqBcpQPLteUoUZDfsnW = false)
        {
            var YdbtnLWuVqJ = TOD_Sky.Instance.Cycle.Hour;
            bool gkvfLGoYUn = atUQdwQgWQwDVVxnVH;
            if ((qTtgPjwYKnWYBUNyiuvPvoseE.HopsIssktXtTFAvHkRLNikGfVmN <=
                     qTtgPjwYKnWYBUNyiuvPvoseE.LaSsnnwtCnRusBnmOGkVqXSjBjZ &&
                 YdbtnLWuVqJ >=
                     qTtgPjwYKnWYBUNyiuvPvoseE.HopsIssktXtTFAvHkRLNikGfVmN &&
                 YdbtnLWuVqJ <
                     qTtgPjwYKnWYBUNyiuvPvoseE.LaSsnnwtCnRusBnmOGkVqXSjBjZ) ||
                (qTtgPjwYKnWYBUNyiuvPvoseE.HopsIssktXtTFAvHkRLNikGfVmN >
                     qTtgPjwYKnWYBUNyiuvPvoseE.LaSsnnwtCnRusBnmOGkVqXSjBjZ &&
                 YdbtnLWuVqJ >=
                     qTtgPjwYKnWYBUNyiuvPvoseE.HopsIssktXtTFAvHkRLNikGfVmN &&
                 YdbtnLWuVqJ <
                     qTtgPjwYKnWYBUNyiuvPvoseE.LaSsnnwtCnRusBnmOGkVqXSjBjZ + 24) ||
                (qTtgPjwYKnWYBUNyiuvPvoseE.HopsIssktXtTFAvHkRLNikGfVmN >
                     qTtgPjwYKnWYBUNyiuvPvoseE.LaSsnnwtCnRusBnmOGkVqXSjBjZ &&
                 YdbtnLWuVqJ + 24 >=
                     qTtgPjwYKnWYBUNyiuvPvoseE.HopsIssktXtTFAvHkRLNikGfVmN &&
                 YdbtnLWuVqJ < qTtgPjwYKnWYBUNyiuvPvoseE.LaSsnnwtCnRusBnmOGkVqXSjBjZ))
                atUQdwQgWQwDVVxnVH = true;
            else
                atUQdwQgWQwDVVxnVH = false;
            if (!HGTxIzqBcpQPLteUoUZDfsnW && gkvfLGoYUn != atUQdwQgWQwDVVxnVH)
                VokNRJJqQJmhbe();
            timer.Once(3f, () => vPJdKZTUlvkhAExigsEikpEssMhro());
        }
        private void VokNRJJqQJmhbe()
        {
            if (qTtgPjwYKnWYBUNyiuvPvoseE.zTaNEVAohdrZ)
            {
                foreach (var JzaicyHgTyvBaIawgTGFZreSPKQ in BasePlayer.activePlayerList)
                    SendReply(
                        JzaicyHgTyvBaIawgTGFZreSPKQ,
                        string.Format(
                            atUQdwQgWQwDVVxnVH ? EbPZSMinhGKIJWLDocoOmrolREC(
                                "INFO.NIGHT.START")
                            : EbPZSMinhGKIJWLDocoOmrolREC("INFO.NIGHT.END"),
                              ywcySQqgapryxMGgSCLdHHXmsqDqB(
                                  JzaicyHgTyvBaIawgTGFZreSPKQ.userID),
                              pDrCSrAbdExdwngdVODpvtb(JzaicyHgTyvBaIawgTGFZreSPKQ.userID),
                              SgGHoCHrldghcbMajVaX(JzaicyHgTyvBaIawgTGFZreSPKQ.userID)));
            }
        }
        private void LoadDefaultMessages()
        {
            lang.RegisterMessages(
                new Dictionary<string, string>{
              {"INFO.NIGHT.START",
               "Наступает ночь\nРейты добываемых ресурсов: X{0}\nРейты поднимаемых ресурсов: X{1}\nРейты ресурсов в карьере: X{2}"},
              {"INFO.NIGHT.END",
               "Наступает день\nРейты добываемых ресурсов: X{0}\nРейты поднимаемых ресурсов: X{1}\nРейты ресурсов в карьере: X{2}"}},
                this);
        }
        private string EbPZSMinhGKIJWLDocoOmrolREC(
            string RGkIUXCCRTSVjPgtGfPOYF,
            string yFbKaaUXamiAhWxDxkKEjfEvQYsSpF = null) =>
            lang.GetMessage(RGkIUXCCRTSVjPgtGfPOYF, this,
                            yFbKaaUXamiAhWxDxkKEjfEvQYsSpF);
        private static bloJbEbBcdx qTtgPjwYKnWYBUNyiuvPvoseE;
        private class SdwccsnfSXRGRvOcwfFXsMC
        {
            [JsonProperty(PropertyName = "Общий рейт добываемых ресурсов")]
            public float yMrwvjSeqkeL;
            [JsonProperty(PropertyName = "Общий рейт добываемых ресурсов ночью")]
            public float CBJrDKzahpDuGOWZsPulSotqJw;
            [JsonProperty(PropertyName = "Общий рейт поднимаемых ресурсов")]
            public float YWnNZGIjQfWbNXrrltThy;
            [JsonProperty(PropertyName = "Общий рейт поднимаемых ресурсов ночью")]
            public float JjzcYPVFRHrgcNhWxsdxOSvyoPQBc;
            [JsonProperty(PropertyName = "Общий рейт добываемых ресурсов в карьере")]
            public float jBzPkWhIDwWHNidjgonsGnrhHxfm;
            [JsonProperty(PropertyName =
                              "Общий рейт добываемых ресурсов в карьере ночью")]
            public float oQfBQfdbHrbFsxhaVZcalAbe;
        }
        private class bloJbEbBcdx
        {
            [JsonProperty(PropertyName = "Общий рейт добываемых ресурсов")]
            public float yMrwvjSeqkeL;
            [JsonProperty(PropertyName = "Общий рейт добываемых ресурсов ночью")]
            public float CBJrDKzahpDuGOWZsPulSotqJw;
            [JsonProperty(PropertyName = "Стандартные рейты добываемых ресурсов")]
            public Dictionary<string, float> MUOsODqppJydk;
            [JsonProperty(PropertyName = "Стандартные рейты инструментов добычи")]
            public Dictionary<string, float> xMafmgITlbhQDRH;
            [JsonProperty(PropertyName = "Общий рейт поднимаемых ресурсов")]
            public float YWnNZGIjQfWbNXrrltThy;
            [JsonProperty(PropertyName = "Общий рейт поднимаемых ресурсов ночью")]
            public float JjzcYPVFRHrgcNhWxsdxOSvyoPQBc;
            [JsonProperty(PropertyName = "Стандартные рейты поднимаемых ресурсов")]
            public Dictionary<string, float> vaxPCujkLFIzsCAJxXNfamVa;
            [JsonProperty(PropertyName = "Общий рейт добываемых ресурсов в карьере")]
            public float jBzPkWhIDwWHNidjgonsGnrhHxfm;
            [JsonProperty(PropertyName =
                              "Общий рейт добываемых ресурсов в карьере ночью")]
            public float oQfBQfdbHrbFsxhaVZcalAbe;
            [JsonProperty(PropertyName =
                              "Стандартные рейты добываемых ресурсов в карьере")]
            public Dictionary<string, float> XNwjoDSIKjufWWBKHcwbNzcIEk;
            [JsonProperty(PropertyName =
                              "Общий рейт добываемых ресурсов экскаватором")]
            public float PsFLvHeYGRGANi;
            [JsonProperty(PropertyName =
                              "Общий рейт добываемых ресурсов экскаватором ночью")]
            public float gZzyZdaaHkeIpTcdloHeZGOfZFXCoM;
            [JsonProperty(PropertyName =
                              "Стандартные рейты добываемых ресурсов экскаватором")]
            public Dictionary<string, float> PUVWEFwfSJClOtd;
            [JsonProperty(PropertyName =
                              "Включить разделение рейтов на ночной и дневной режим")]
            public bool LXWbkykjGflIuDeAmwFZADbszXfGK;
            [JsonProperty(
                PropertyName =
                    "Час игрового времени с которого включается ночной режим")]
            public int HopsIssktXtTFAvHkRLNikGfVmN;
            [JsonProperty(
                PropertyName =
                    "Час игрового времени с которого выключается ночной режим")]
            public int LaSsnnwtCnRusBnmOGkVqXSjBjZ;
            [JsonProperty(
                PropertyName =
                    "Выводить оповещение в чат при смене ночного и дневного режима")]
            public bool zTaNEVAohdrZ;
            [JsonProperty(PropertyName =
                              "Изменение рейтов для игроков с привилегиями")]
            public Dictionary<string, SdwccsnfSXRGRvOcwfFXsMC> idzSnoRLkDWmagCKFRjFD;
        }
        private void kCTDaHDLMefg() => qTtgPjwYKnWYBUNyiuvPvoseE =
            Config.ReadObject<bloJbEbBcdx>();
        protected override void LoadDefaultConfig()
        {
            qTtgPjwYKnWYBUNyiuvPvoseE = new bloJbEbBcdx
            {
                yMrwvjSeqkeL = 1f,
                CBJrDKzahpDuGOWZsPulSotqJw = 1f,
                MUOsODqppJydk =
                    new Dictionary<string, float>(){{"Animal Fat", 1f},
                                              {"Bear Meat", 1f},
                                              {"Bone Fragments", 1f},
                                              {"Cloth", 1f},
                                              {"High Quality Metal Ore", 1f},
                                              {"Human Skull", 1f},
                                              {"Leather", 1f},
                                              {"Metal Ore", 1f},
                                              {"Pork", 1f},
                                              {"Raw Bear Meat", 1f},
                                              {"Raw Chicken Breast", 1f},
                                              {"Raw Human Meat", 1f},
                                              {"Raw Wolf Meat", 1f},
                                              {"Stones", 1f},
                                              {"Sulfur Ore", 1f},
                                              {"Wolf Skull", 1f},
                                              {"Wood", 1f}},
                xMafmgITlbhQDRH =
                    new Dictionary<string, float>(){{"Bone Club", 1f},
                                              {"Candy Cane Club", 1f},
                                              {"Chainsaw", 1f},
                                              {"Hatchet", 1f},
                                              {"Pickaxe", 1f},
                                              {"Rock", 1f},
                                              {"Salvaged Axe", 1f},
                                              {"Salvaged Hammer", 1f},
                                              {"Salvaged Icepick", 1f},
                                              {"Stone Hatchet", 1f},
                                              {"Stone Pickaxe", 1f},
                                              {"Jackhammer", 1f}},
                YWnNZGIjQfWbNXrrltThy = 1f,
                JjzcYPVFRHrgcNhWxsdxOSvyoPQBc = 1f,
                vaxPCujkLFIzsCAJxXNfamVa =
                    new Dictionary<string, float>(){{"Metal Ore", 1f},
                                              {"Stones", 1f},
                                              {"Sulfur Ore", 1f},
                                              {"Wood", 1f},
                                              {"Hemp Seed", 1f},
                                              {"Corn Seed", 1f},
                                              {"Pumpkin Seed", 1f},
                                              {"Cloth", 1f},
                                              {"Pumpkin", 1f},
                                              {"Corn", 1f}},
                jBzPkWhIDwWHNidjgonsGnrhHxfm = 1f,
                oQfBQfdbHrbFsxhaVZcalAbe = 1f,
                XNwjoDSIKjufWWBKHcwbNzcIEk =
                    new Dictionary<string, float>(){{"High Quality Metal Ore", 1f},
                                              {"Metal Fragments", 1f},
                                              {"Metal Ore", 1f},
                                              {"Stones", 1f},
                                              {"Sulfur Ore", 1f}},
                PsFLvHeYGRGANi = 1f,
                gZzyZdaaHkeIpTcdloHeZGOfZFXCoM = 1f,
                PUVWEFwfSJClOtd =
                    new Dictionary<string, float>(){{"High Quality Metal Ore", 1f},
                                              {"Metal Fragments", 1f},
                                              {"Metal Ore", 1f},
                                              {"Stones", 1f},
                                              {"Sulfur Ore", 1f}},
                LXWbkykjGflIuDeAmwFZADbszXfGK = false,
                HopsIssktXtTFAvHkRLNikGfVmN = 19,
                LaSsnnwtCnRusBnmOGkVqXSjBjZ = 8,
                zTaNEVAohdrZ = true,
                idzSnoRLkDWmagCKFRjFD =
                    new Dictionary<string, SdwccsnfSXRGRvOcwfFXsMC>(){
                  {"gatherplus.vip",
                   new SdwccsnfSXRGRvOcwfFXsMC(){
                       yMrwvjSeqkeL = 2f, CBJrDKzahpDuGOWZsPulSotqJw = 2f,
                       YWnNZGIjQfWbNXrrltThy = 2f,
                       JjzcYPVFRHrgcNhWxsdxOSvyoPQBc = 2f,
                       jBzPkWhIDwWHNidjgonsGnrhHxfm = 2f,
                       oQfBQfdbHrbFsxhaVZcalAbe = 2f}},
                  {"gatherplus.premium",
                   new SdwccsnfSXRGRvOcwfFXsMC(){
                       yMrwvjSeqkeL = 3f, CBJrDKzahpDuGOWZsPulSotqJw = 3f,
                       YWnNZGIjQfWbNXrrltThy = 3f,
                       JjzcYPVFRHrgcNhWxsdxOSvyoPQBc = 3f,
                       jBzPkWhIDwWHNidjgonsGnrhHxfm = 3f,
                       oQfBQfdbHrbFsxhaVZcalAbe = 3f}}}
            };
            KfNCtHhEvItseKXhSI(qTtgPjwYKnWYBUNyiuvPvoseE);
            timer.Once(0.1f, () => KfNCtHhEvItseKXhSI(qTtgPjwYKnWYBUNyiuvPvoseE));
        }
        private void KfNCtHhEvItseKXhSI(bloJbEbBcdx wuOXwRaQxVw) =>
            Config.WriteObject(wuOXwRaQxVw, true);
    }
}
