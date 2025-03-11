namespace Oxide.Plugins
{
    [Info("BigBox", "Frizen", "1.0.0")]
    internal class BigBox : RustPlugin
    {
        private void OnServerInitialized()
        {
            var storages = UnityEngine.Object.FindObjectsOfType<StorageContainer>();

            foreach (StorageContainer storage in storages)
            {
                if (storage.name.Contains("box.wooden.large"))
                {
                    OnEntitySpawned(storage);
                }
            }
        }

        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity == null) return;

            if (entity is StorageContainer && entity.name.Contains("box.wooden.large"))
            {
                StorageContainer storageContainer = entity as StorageContainer;

                if (storageContainer.panelName != "genericlarge")
                {
                    storageContainer.panelName = "genericlarge";

                    storageContainer.inventory.capacity = 42;

                    storageContainer.SendNetworkUpdate();
                }
            }
        }
    }
}