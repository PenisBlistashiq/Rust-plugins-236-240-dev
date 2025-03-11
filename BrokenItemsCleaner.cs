//#define TESTING
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Oxide.Plugins
{
    [Info("BrokenItemsCleaner", "EcoSmile/Vlad-00003", "1.3.5")]
    public class BrokenItemsCleaner : RustPlugin
    {
        [PluginReference("Backpacks")]
        private RustPlugin Backpacks;

        private Coroutine _cleanup;
        private CleanupData _cleanupData;

        private void Loaded()
        {
            _cleanupData = new CleanupData();
#if TESTING
            StopwatchWrapper.OnComplete = DebugMessage;
#endif
        }

        private void OnServerSave()
        {
            if(ServerMgr.Instance.IsInvoking(OnServerSave))
                ServerMgr.Instance.CancelInvoke(OnServerSave);

            if (_cleanup != null || SaveRestore.IsSaving)
            {
                ServerMgr.Instance.Invoke(OnServerSave, 5f);
                return;
            }

            _cleanup = ServerMgr.Instance.StartCoroutine(PreformCleanup());
        }
        
        private void Unload()
        {
            if (ServerMgr.Instance == null)
                return;
            ServerMgr.Instance.CancelInvoke(OnServerSave);

            if (_cleanup == null)
                return;
            ServerMgr.Instance.StopCoroutine(_cleanup);
            _cleanupData.Clear();
        }


        #region CleanupData

        private class CleanupData
        {
            private readonly Stopwatch _stopwatch = new Stopwatch();
            public readonly HashSet<ulong> HeldEntities = new HashSet<ulong>();
            public readonly Dictionary<ulong, Item[]> Items = new Dictionary<ulong, Item[]>();
            public int Removed;
            public int Repaired;

            public void Add(IItemGetter selector, BaseNetworkable[] array)
            {
                foreach (var entity in selector.Entities(array))
                {
                    if(!entity)
                        continue;
                    var items = selector.GetItems(entity);
                    if (items != null)
                        Items[entity.net.ID] = items.ToArray();
                }
            }

            public void Add(ulong userId, ItemContainer container)
            {
                var items = container?.itemList;
                if (items != null)
                    Items[userId] = items.ToArray();
            }

            public bool InvalidOrListed(HeldEntity entity)
            {
                return !entity || !entity.IsValid() || HeldEntities.Contains(entity.net.ID);
            }

            public void Clear()
            {
                HeldEntities.Clear();
                Items.Clear();
                Repaired = 0;
                Removed = 0;
            }

            public void StartNew()
            {
                _stopwatch.Restart();
            }

            public void Stop()
            {
                _stopwatch.Stop();
            }

            public override string ToString()
            {
                return $"Предметов удалено/Entity Removed: {Removed}\n" +
                       $"Предметов восстановлено/Entity Repaired: {Repaired}\n" +
                       $"Очистка заняла/Cleanup took:{_stopwatch.ElapsedMilliseconds}\n";
            }
        }

        #endregion

        #region Cleanup

        #region ItemGetter

        private interface IItemGetter
        {
            string TypeName { get; }
            IEnumerable<Item> GetItems(BaseNetworkable obj);
            IEnumerable<BaseNetworkable> Entities(BaseNetworkable[] array);
        }

        private class ItemGetter<T> : IItemGetter where T : BaseNetworkable
        {
            private readonly Func<T, IEnumerable<Item>> _getter;

            public ItemGetter(Func<T, IEnumerable<Item>> getter)
            {
                _getter = getter;
                TypeName = typeof(T).FullName;
            }

            [CanBeNull]
            public IEnumerable<Item> GetItems(BaseNetworkable obj)
            {
                IEnumerable<Item> result;
                try
                {
                    result = _getter(obj as T);
                }
                catch (Exception e)
                {
#if TESTING
                    Debug.LogErrorFormat("Can't get items from {0}",obj);
                    Debug.LogException(e);
#endif
                    return null;
                }

                return result;
            }

            public IEnumerable<BaseNetworkable> Entities(BaseNetworkable[] array)
            {
                return array.OfType<T>();
            }

            public string TypeName { get; }
        }

        #endregion


        private static readonly List<IItemGetter> ItemGetters = new List<IItemGetter>
        {
            new ItemGetter<StorageContainer>(x => x.inventory?.itemList),
            new ItemGetter<LootableCorpse>(x => x.containers?.SelectMany(y => y?.itemList)),
            new ItemGetter<DroppedItemContainer>(x => x.inventory?.itemList),
            new ItemGetter<ContainerIOEntity>(x => x.inventory?.itemList),
            new ItemGetter<BaseRidableAnimal>(x => x.inventory?.itemList),
            new ItemGetter<DroppedItem>(x => new[] {x.item}),
            new ItemGetter<BasePlayer>(x => x.inventory?.AllItems())
        };


        private void CheckLost(IEnumerable<Item> items)
        {
            foreach (var item in items)
            {
                if (item?.IsValid() != true)
                    continue;

                var held = item.GetHeldEntity() as HeldEntity;
                if (held?.IsValid() == true)
                {
                    _cleanupData.HeldEntities.Add(held.net.ID);
                    continue;
                }

                item.OnItemCreated();
                item.MarkDirty();
                held = item.GetHeldEntity() as HeldEntity;
                if (held?.IsValid() == true)
                {
                    _cleanupData.HeldEntities.Add(held.net.ID);
                    _cleanupData.Repaired++;
                }
            }
        }

        private IEnumerator PreformCleanup()
        {
            var enumerator = ActualCleanup();
            while (true)
            {
                object current;
                try
                {
                    if (enumerator.MoveNext() == false)
                        break;
                    current = enumerator.Current;
                }
                catch (Exception ex)
                {
                    OnCleanupComplete(ex);
                    yield break;
                }
                yield return current;
            }

            OnCleanupComplete();
        }

        private void OnCleanupComplete(Exception ex = null)
        {
            if (ex != null)
            {
                Debug.LogException(ex);
                PrintError("При очистке возникло исключение/Exception during cleanup:\n{0}",ex);
            }
            PrintWarning($"Очистка завершена/Cleanup Completed:\n{_cleanupData}");
            _cleanupData.Clear();
            _cleanup = null;
        }

        private IEnumerator ActualCleanup()
        {
            yield return CoroutineEx.waitForSeconds(2);

            PrintWarning("Очистка запущена/Cleaning started");

            var array = BaseNetworkable.serverEntities.Where(x=>!(x is MixingTable)).ToArray();
            yield return null;

            _cleanupData.StartNew();

            foreach (var itemGetter in ItemGetters)
            {
#if TESTING
                using (new StopwatchWrapper($"Lookup for items in entities of type {itemGetter.TypeName} took {{0}}ms."))
#endif
                {
                    _cleanupData.Add(itemGetter, array);
                }
            }
            yield return CoroutineEx.waitForEndOfFrame;
            //Backpacks workaround
#if TESTING
            using (new StopwatchWrapper($"Getting all items from existing backpacks took {{0}}ms."))
#endif
            {
                var backpacks = Backpacks?.CallHook("API_GetExistingBackpacks") as Dictionary<ulong, ItemContainer>;
                if (backpacks != null)
                {
                    foreach (var backpack in backpacks)
                    {
                        _cleanupData.Add(backpack.Key,backpack.Value);
                    }
                }
            }
            yield return CoroutineEx.waitForEndOfFrame;

#if TESTING
            using (new StopwatchWrapper("Checking all items took {0}ms."))
#endif
            {
                foreach (var item in _cleanupData.Items)
                {
                    CheckLost(item.Value);
                    yield return CoroutineEx.waitForEndOfFrame;
                }
            }

#if TESTING
            var text = "";
            using (new StopwatchWrapper("Clearing leftover HeldEntities took {0}ms."))
#endif
            {
                foreach (var entity in array.OfType<HeldEntity>())
                {
                    if (_cleanupData.InvalidOrListed(entity))
                        continue;
#if TESTING
                    var parentEntity = entity.GetParentEntity();
                    text +=
                        $"Removing entity {entity}, parent: {(parentEntity == null ? "null" : parentEntity.ToString())}, ";
#endif
                    entity.Kill();
#if TESTING
                    text += $"removed? {entity.IsDestroyed}\n";
#endif
                    _cleanupData.Removed++;
                    yield return null;
                }
            }
#if TESTING
            LogToFile("DeletedItems", text, this);
#endif
            _cleanupData.Stop();
            yield return null;
        }

        #endregion

        #region Testing functions

#if TESTING
        private void DebugMessage(string format, long time)
        {
            PrintWarning(format, time);
        }

        private class StopwatchWrapper : IDisposable
        {
            public StopwatchWrapper(string format)
            {
                Sw = new Stopwatch();
                Sw.Start();
                Format = format;
            }

            public static Action<string, long> OnComplete { private get; set; }

            private string Format { get; }
            private Stopwatch Sw { get; }

            public long Time { get; private set; }

            public void Dispose()
            {
                Sw.Stop();
                Time = Sw.ElapsedMilliseconds;
                OnComplete(Format, Time);
            }
        }

#endif

        #endregion
    }
}