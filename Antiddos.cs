using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Antiddos", "Bizlich", "1.2.3")]
    public class Antiddos : RustPlugin
    {
        private Coroutine _cleanup;
        private CleanupData _cleanupData;

        private void Loaded()
        {
            _cleanupData = new CleanupData();
        }

        private void OnServerSave()
        {
            if (ServerMgr.Instance.IsInvoking(OnServerSave))
                ServerMgr.Instance.CancelInvoke(OnServerSave);

            if (_cleanup != null || SaveRestore.IsSaving)
            {
                ServerMgr.Instance.Invoke(OnServerSave, 5f);
                return;
            }

            _cleanup = ServerMgr.Instance.StartCoroutine(PerformCleanup());
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

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            CheckAndRemoveDuplicateItems(entity as BaseEntity);
        }

        private void CheckAndRemoveDuplicateItems(BaseEntity entity)
        {
            if (entity == null || !(entity is DroppedItem))
                return;

            Vector3 position = entity.transform.position;
            List<DroppedItem> nearbyItems = new List<DroppedItem>();
            Vis.Entities(position, 30f, nearbyItems);

            var itemCounts = new Dictionary<uint, List<DroppedItem>>();

            foreach (var nearbyItem in nearbyItems.OfType<DroppedItem>())
            {
                uint prefabId = nearbyItem.prefabID;
                if (!itemCounts.ContainsKey(prefabId))
                {
                    itemCounts[prefabId] = new List<DroppedItem>();
                }
                itemCounts[prefabId].Add(nearbyItem);
            }

            foreach (var kvp in itemCounts)
            {
                if (kvp.Value.Count >= 30)
                {
                    foreach (var itemToRemove in kvp.Value)
                    {
                        itemToRemove.Kill();
                        _cleanupData.Removed++;
                    }
                }
            }
        }


        #region CleanupData

        private class CleanupData
        {
            private readonly Stopwatch _stopwatch = new Stopwatch();
            public readonly HashSet<ulong> HeldEntities = new HashSet<ulong>();
            public readonly Dictionary<ulong, Item[]> Items = new Dictionary<ulong, Item[]>();
            public int Removed;

            public void Add(IItemGetter selector, BaseNetworkable[] array)
            {
                foreach (var entity in selector.Entities(array))
                {
                    var stage = 0;
                    try
                    {
                        stage = 1;
                        if (!entity || entity?.isSpawned != true)
                        {
                            stage = 2;
                            continue;
                        }

                        stage = 3;
                        var items = selector.GetItems(entity);
                        stage = 4;
                        if (items != null)
                        {
                            stage = 5;
                            Items[entity.net.ID] = items.ToArray();
                        }

                        stage = 6;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"CleanupData.Add. Selector: {selector.TypeName}. Entity: {entity}. Stage: {stage}", ex);
                    }
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
                return $"Предметов удалено: {Removed}\n" +
                       $"Очистка заняла:{_stopwatch.ElapsedMilliseconds}\n";
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

            public IEnumerable<Item> GetItems(BaseNetworkable obj)
            {
                IEnumerable<Item> result;
                try
                {
                    result = _getter(obj as T);
                }
                catch (Exception e)
                {
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

        private static bool HasHeldEntity(Item item)
        {
            var mods = item.info.itemMods;
            var count = mods.Length;
            for (var i = 0; i < count; i++)
            {
                var held = mods[i] as ItemModEntity;
                if (held)
                    return true;
            }
            return false;
        }

        private static readonly List<IItemGetter> ItemGetters = new List<IItemGetter>
        {
            new ItemGetter<StorageContainer>(x => x.inventory?.itemList.Where(HasHeldEntity)),
            new ItemGetter<LootableCorpse>(x => x.containers?.SelectMany(y => y?.itemList).Where(HasHeldEntity)),
            new ItemGetter<DroppedItemContainer>(x => x.inventory?.itemList.Where(HasHeldEntity)),
            new ItemGetter<DroppedItem>(x => new[] {x.item}.Where(HasHeldEntity)),
            new ItemGetter<BasePlayer>(x => x.inventory?.AllItems().Where(HasHeldEntity))
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
                }
            }
        }

        private IEnumerator PerformCleanup()
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
                PrintError("При очистке возникло исключение:\n{0}", ex);
            }
            PrintWarning($"Очистка завершена:\n{_cleanupData}");
            _cleanupData.Clear();
            _cleanup = null;
        }

        private IEnumerator ActualCleanup()
        {
            yield return CoroutineEx.waitForSeconds(2);

            PrintWarning("Очистка запущена");

            var array = BaseNetworkable.serverEntities.Where(x => x != null).ToArray();
            yield return null;

            _cleanupData.StartNew();

            foreach (var itemGetter in ItemGetters)
            {
                _cleanupData.Add(itemGetter, array);
            }
            yield return CoroutineEx.waitForEndOfFrame;

            foreach (var item in _cleanupData.Items)
            {
                CheckLost(item.Value);
                yield return CoroutineEx.waitForEndOfFrame;
            }

            _cleanupData.Stop();
            yield return null;
        }

        #endregion
    }
}