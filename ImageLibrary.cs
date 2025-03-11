//Reference: Facepunch.Sqlite
//Reference: UnityEngine.UnityWebRequestModule
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Oxide.Plugins
{
    [Info("Image Library", "Absolut & K1lly0u", "2.0.60")]
    [Description("Plugin API for downloading and managing images")]
    class ImageLibrary : RustPlugin
    {
        #region Fields

        private ImageIdentifiers imageIdentifiers;
        private ImageURLs imageUrls;
        private SkinInformation skinInformation;
        private DynamicConfigFile identifiers;
        private DynamicConfigFile urls;
        private DynamicConfigFile skininfo;

        private static ImageLibrary il;
        private ImageAssets assets;

        private Queue<LoadOrder> loadOrders = new Queue<LoadOrder>();
        private bool orderPending;
        private bool isInitialized;

        private JsonSerializerSettings errorHandling = new JsonSerializerSettings { Error = (se, ev) => { ev.ErrorContext.Handled = true; } };

        private const string STEAM_API_URL = "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/";
        private const string STEAM_AVATAR_URL = "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={0}&steamids={1}";

        private string[] itemShortNames;

        #endregion Fields

        #region Oxide Hooks

        private void Loaded()
        {
            identifiers = Interface.Oxide.DataFileSystem.GetFile("ImageLibrary/image_data");
            urls = Interface.Oxide.DataFileSystem.GetFile("ImageLibrary/image_urls");
            skininfo = Interface.Oxide.DataFileSystem.GetFile("ImageLibrary/skin_data");

            il = this;
            LoadData();
        }

        private void OnServerInitialized()
        {
            itemShortNames = ItemManager.itemList.Select(x => x.shortname).ToArray();

            foreach (ItemDefinition item in ItemManager.itemList)
            {
                string workshopName = item.displayName.english.ToLower().Replace("skin", "").Replace(" ", "").Replace("-", "");
                if (!workshopNameToShortname.ContainsKey(workshopName))
                    workshopNameToShortname.Add(workshopName, item.shortname);
            }

            AddDefaultUrls();

            CheckForRefresh();

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);
        }
    
        private void OnPlayerConnected(BasePlayer player) => GetPlayerAvatar(player?.UserIDString);

        private void Unload()
        {
            SaveData();
            UnityEngine.Object.Destroy(assets);
            il = null;
        }

        #endregion Oxide Hooks

        #region Functions
        private IEnumerator ProcessLoadOrders()
        {
            yield return new WaitWhile(() => !isInitialized);

            if (loadOrders.Count > 0)
            {
                if (orderPending)
                    yield break;

                LoadOrder nextLoad = loadOrders.Dequeue();
                if (!nextLoad.loadSilent)
                    Puts("Starting order " + nextLoad.loadName);

                if (nextLoad.imageList != null && nextLoad.imageList.Count > 0)
                {
                    foreach (KeyValuePair<string, string> item in nextLoad.imageList)
                        assets.Add(item.Key, item.Value);
                }
                if (nextLoad.imageData != null && nextLoad.imageData.Count > 0)
                {
                    foreach (KeyValuePair<string, byte[]> item in nextLoad.imageData)
                        assets.Add(item.Key, null, item.Value);
                }

                orderPending = true;

                assets.RegisterCallback(nextLoad.callback);

                assets.BeginLoad(nextLoad.loadSilent ? string.Empty : nextLoad.loadName);
            }
        }

        private void GetPlayerAvatar(string userId)
        {
            if (!configData.StoreAvatars || string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(configData.SteamAPIKey) || HasImage(userId, 0))
                return;

            webrequest.Enqueue(string.Format(STEAM_AVATAR_URL, configData.SteamAPIKey, userId), null, (code, response) =>
            {
                if (response != null && code == 200)
                {
                    try
                    {
                        AvatarRoot rootObject = JsonConvert.DeserializeObject<AvatarRoot>(response, errorHandling);
                        if (rootObject?.response?.players?.Length > 0)
                        {
                            string avatarUrl = rootObject.response.players[0].avatarmedium;
                            if (!string.IsNullOrEmpty(avatarUrl))                            
                                AddImage(avatarUrl, userId, 0);                               
                        }                        
                    }
                    catch { }
                }
            }, this);
        }

        private void RefreshImagery()
        {
            imageIdentifiers.imageIds.Clear();
            imageIdentifiers.lastCEID = CommunityEntity.ServerInstance.net.ID;

            AddImage("http://i.imgur.com/sZepiWv.png", "NONE", 0);
            AddImage("http://i.imgur.com/lydxb0u.png", "LOADING", 0);
            foreach (var image in configData.UserImages)
            {
                if (!string.IsNullOrEmpty(image.Value))
                    AddImage(image.Value, image.Key, 0);
            }

            if ((Steamworks.SteamInventory.Definitions?.Length ?? 0) == 0)
            {
                PrintWarning("Waiting for Steamworks to update item definitions....");
                Steamworks.SteamInventory.OnDefinitionsUpdated += GetItemSkins;
            }
            else GetItemSkins();
        }

        private void CheckForRefresh()
        {
            if (assets == null)
                assets = new GameObject("WebObject").AddComponent<ImageAssets>();

            isInitialized = true;

            if (imageIdentifiers.lastCEID != CommunityEntity.ServerInstance.net.ID)
            {
                if (imageIdentifiers.imageIds.Count < 2)
                {
                    RefreshImagery();
                }
                else
                {
                    PrintWarning("The CommunityEntity instance ID has changed! Due to the way CUI works in Rust all previously stored images must be removed and re-stored using the new ID as reference so clients can find the images. These images will be added to a new load order. Interupting this process will result in being required to re-download these images from the web");
                    RestoreLoadedImages();
                }
            }
        }

        private void RestoreLoadedImages()
        {
            orderPending = true;

            try
            {
                Facepunch.Sqlite.Database db = new Facepunch.Sqlite.Database();
                db.Open(string.Concat(ConVar.Server.rootFolder, "/", "sv.files.", Rust.Protocol.save - 1, ".db"));                
                if (db.TableExists("data"))
                {
                    Dictionary<string, byte[]> oldFiles = new Dictionary<string, byte[]>();
                    int failed = 0;

                    for (int i = imageIdentifiers.imageIds.Count - 1; i >= 0; i--)
                    {
                        KeyValuePair<string, string> image = imageIdentifiers.imageIds.ElementAt(i);

                        uint imageId;
                        if (!uint.TryParse(image.Value, out imageId))
                            continue;

                        byte[] bytes = db.QueryBlob("SELECT data FROM data WHERE crc = ? AND filetype = ? AND entid = ? LIMIT 1", new object[] { (int)imageId, 0, imageIdentifiers.lastCEID });
                        if (bytes != null)
                            oldFiles.Add(image.Key, bytes);
                        else
                        {
                            failed++;
                            imageIdentifiers.imageIds.Remove(image.Key);
                        }
                    }

                    if (oldFiles.Count > 0)
                    {
                        loadOrders.Enqueue(new LoadOrder("Image restoration from previous database", oldFiles));
                        PrintWarning($"{imageIdentifiers.imageIds.Count - failed} images queued for restoration from previous image db, {failed} images failed");
                    }

                }
                db.Close();
            }
            catch
            {
                PrintError("Failed to open previous image database. Unable to clone previous image data");
            }
            //Facepunch.Sqlite.Database db = new Facepunch.Sqlite.Database();
            //try
            //{
            //    db.Open($"{ConVar.Server.rootFolder}/sv.files.0.db");
            //    db.Execute("DELETE FROM data WHERE entid = ?", imageIdentifiers.lastCEID);
            //    db.Close();
            //}
            //catch { }

            //loadOrders.Enqueue(new LoadOrder("Image restoration from previous database", oldFiles));
            //PrintWarning($"{imageIdentifiers.imageIds.Count - failed} images queued for restoration, {failed} images failed");
            imageIdentifiers.lastCEID = CommunityEntity.ServerInstance.net.ID;
            SaveData();

            orderPending = false;
            ServerMgr.Instance.StartCoroutine(ProcessLoadOrders());
        }

        #endregion Functions

        #region Workshop Names and Image URLs
        private void AddDefaultUrls()
        {
            foreach (ItemDefinition itemDefinition in ItemManager.itemList)
            {
                string identifier = $"{itemDefinition.shortname}_0";
                if (!imageUrls.URLs.ContainsKey(identifier))
                    imageUrls.URLs.Add(identifier, $"{configData.ImageURL}{itemDefinition.shortname}.png");
                else imageUrls.URLs[identifier] = $"{configData.ImageURL}{itemDefinition.shortname}.png";
            }
            
            SaveUrls();

            LoadInbuiltSkinLookup();
        }

        private void LoadInbuiltSkinLookup()
        {
            const string LOOKUP_TABLE = "https://raw.githubusercontent.com/k1lly0u/Oxide/master/il_inbuilt_skins.json";

            try
            {
                Debug.Log("Loading inbuilt skin manifest from GitHub...");
                webrequest.Enqueue(LOOKUP_TABLE, string.Empty, (int code, string response) =>
                {
                    Dictionary<string, string> collection = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);

                    foreach (ItemSkinDirectory.Skin skin in ItemSkinDirectory.Instance.skins)
                    {
                        if (skin.invItem == null || string.IsNullOrEmpty(skin.invItem.itemname))
                            continue;

                        string filename;
                        if (collection.TryGetValue(skin.name, out filename))
                        {
                            string identifier = $"{skin.invItem.itemname}_{skin.id}";

                            if (!imageUrls.URLs.ContainsKey(identifier))
                                imageUrls.URLs.Add(identifier, $"{configData.ImageURL}{filename}.png");
                            else imageUrls.URLs[identifier] = $"{configData.ImageURL}{filename}.png";
                        }
                    }

                    Debug.Log("Skin manifest imported successfully");
                    SaveUrls();
                }, this);
            }
            catch
            {
                Debug.LogError("Failed to download inbuilt skin manifest from GitHub. Unable to gather inbuilt skin list");
            }
        }

        private readonly Dictionary<string, string> workshopNameToShortname = new Dictionary<string, string>
        {
            {"longtshirt", "tshirt.long" },
            {"cap", "hat.cap" },
            {"beenie", "hat.beenie" },
            {"boonie", "hat.boonie" },
            {"balaclava", "mask.balaclava" },
            {"pipeshotgun", "shotgun.waterpipe" },
            {"woodstorage", "box.wooden" },
            {"ak47", "rifle.ak" },
            {"bearrug", "rug.bear" },
            {"boltrifle", "rifle.bolt" },
            {"bandana", "mask.bandana" },
            {"hideshirt", "attire.hide.vest" },
            {"snowjacket", "jacket.snow" },
            {"buckethat", "bucket.helmet" },
            {"semiautopistol", "pistol.semiauto" },
            {"burlapgloves", "burlap.gloves" },
            {"roadsignvest", "roadsign.jacket" },
            {"roadsignpants", "roadsign.kilt" },
            {"burlappants", "burlap.trousers" },
            {"collaredshirt", "shirt.collared" },
            {"mp5", "smg.mp5" },
            {"sword", "salvaged.sword" },
            {"workboots", "shoes.boots" },
            {"vagabondjacket", "jacket" },
            {"hideshoes", "attire.hide.boots" },
            {"deerskullmask", "deer.skull.mask" },
            {"minerhat", "hat.miner" },
            {"lr300", "rifle.lr300" },
            {"lr300.item", "rifle.lr300" },
            {"burlap.gloves", "burlap.gloves.new"},
            {"leather.gloves", "burlap.gloves"},
            {"python", "pistol.python" },
            {"m39", "rifle.m39"},
            {"woodendoubledoor", "door.double.hinged.wood"}
        };

        #endregion Workshop Names and Image URLs

        #region API

        [HookMethod("AddImage")]
        public bool AddImage(string url, string imageName, ulong imageId, Action callback = null)
        {
            loadOrders.Enqueue(new LoadOrder(imageName, new Dictionary<string, string> { { $"{imageName}_{imageId}", url } }, true, callback));
            if (!orderPending)
                ServerMgr.Instance.StartCoroutine(ProcessLoadOrders());
            return true;
        }

        [HookMethod("AddImageData")]
        public bool AddImageData(string imageName, byte[] array, ulong imageId, Action callback = null)
        {
            loadOrders.Enqueue(new LoadOrder(imageName, new Dictionary<string, byte[]> { { $"{imageName}_{imageId}", array } }, true, callback));
            if (!orderPending)
                ServerMgr.Instance.StartCoroutine(ProcessLoadOrders());
            return true;
        }

        [HookMethod("GetImageURL")]
        public string GetImageURL(string imageName, ulong imageId = 0)
        {
            string identifier = $"{imageName}_{imageId}";
            string value;
            if (imageUrls.URLs.TryGetValue(identifier, out value))
                return value;
            return string.Empty;
        }

        [HookMethod("GetImage")]
        public string GetImage(string imageName, ulong imageId = 0, bool returnUrl = false)
        {
            string identifier = $"{imageName}_{imageId}";
            string value;
            if (imageIdentifiers.imageIds.TryGetValue(identifier, out value))
                return value;
            else
            {                
                if (imageUrls.URLs.TryGetValue(identifier, out value))
                {
                    AddImage(value, imageName, imageId);
                    return imageIdentifiers.imageIds["LOADING_0"];
                }
            }

            if (returnUrl && !string.IsNullOrEmpty(value))
                return value;

            return imageIdentifiers.imageIds["NONE_0"];
        }

        [HookMethod("GetImageList")]
        public List<ulong> GetImageList(string name)
        {
            List<ulong> skinIds = new List<ulong>();
            var matches = imageUrls.URLs.Keys.Where(x => x.StartsWith(name)).ToArray();
            for (int i = 0; i < matches.Length; i++)
            {
                var index = matches[i].IndexOf("_");
                if (matches[i].Substring(0, index) == name)
                {
                    ulong skinID;
                    if (ulong.TryParse(matches[i].Substring(index + 1), out skinID))
                        skinIds.Add(ulong.Parse(matches[i].Substring(index + 1)));
                }
            }
            return skinIds;
        }

        [HookMethod("GetSkinInfo")]
        public Dictionary<string, object> GetSkinInfo(string name, ulong id)
        {
            Dictionary<string, object> skinInfo;
            if (skinInformation.skinData.TryGetValue($"{name}_{id}", out skinInfo))
                return skinInfo;
            return null;
        }

        [HookMethod("HasImage")]
        public bool HasImage(string imageName, ulong imageId)
        {
            string key = $"{imageName}_{imageId}";
            string value;

            if (imageIdentifiers.imageIds.TryGetValue(key, out value) && IsInStorage(uint.Parse(value)))            
                return true;            

            return false;
        }

        public bool IsInStorage(uint crc) => FileStorage.server.Get(crc, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID) != null;

        [HookMethod("IsReady")]
        public bool IsReady() => loadOrders.Count == 0 && !orderPending;

        [HookMethod("ImportImageList")]
        public void ImportImageList(string title, Dictionary<string, string> imageList, ulong imageId = 0, bool replace = false, Action callback = null)
        {
            Dictionary<string, string> newLoadOrder = new Dictionary<string, string>();
            foreach (var image in imageList)
            {
                if (!replace && HasImage(image.Key, imageId))
                    continue;
                newLoadOrder[$"{image.Key}_{imageId}"] = image.Value;
            }
            if (newLoadOrder.Count > 0)
            {
                loadOrders.Enqueue(new LoadOrder(title, newLoadOrder, false, callback));
                if (!orderPending)
                    ServerMgr.Instance.StartCoroutine(ProcessLoadOrders());
            }
            else
            {
                if (callback != null)
                    callback.Invoke();
            }
        }

        [HookMethod("ImportItemList")]
        public void ImportItemList(string title, Dictionary<string, Dictionary<ulong, string>> itemList, bool replace = false, Action callback = null)
        {
            Dictionary<string, string> newLoadOrder = new Dictionary<string, string>();
            foreach (var image in itemList)
            {
                foreach (var skin in image.Value)
                {
                    if (!replace && HasImage(image.Key, skin.Key))
                        continue;
                    newLoadOrder[$"{image.Key}_{skin.Key}"] = skin.Value;
                }
            }
            if (newLoadOrder.Count > 0)
            {
                loadOrders.Enqueue(new LoadOrder(title, newLoadOrder, false, callback));
                if (!orderPending)
                    ServerMgr.Instance.StartCoroutine(ProcessLoadOrders());
            }
            else
            {
                if (callback != null)
                    callback.Invoke();
            }
        }

        [HookMethod("ImportImageData")]
        public void ImportImageData(string title, Dictionary<string, byte[]> imageList, ulong imageId = 0, bool replace = false, Action callback = null)
        {
            Dictionary<string, byte[]> newLoadOrder = new Dictionary<string, byte[]>();
            foreach (var image in imageList)
            {
                if (!replace && HasImage(image.Key, imageId))
                    continue;
                newLoadOrder[$"{image.Key}_{imageId}"] = image.Value;
            }
            if (newLoadOrder.Count > 0)
            {
                loadOrders.Enqueue(new LoadOrder(title, newLoadOrder, false, callback));
                if (!orderPending)
                    ServerMgr.Instance.StartCoroutine(ProcessLoadOrders());
            }
            else
            {
                if (callback != null)
                    callback.Invoke();
            }
        }

        [HookMethod("LoadImageList")]
        public void LoadImageList(string title, List<KeyValuePair<string, ulong>> imageList, Action callback = null)
        {
            Dictionary<string, string> newLoadOrderURL = new Dictionary<string, string>();
            List<KeyValuePair<string, ulong>> workshopDownloads = new List<KeyValuePair<string, ulong>>();

            foreach (KeyValuePair<string, ulong> image in imageList)
            {
                if (HasImage(image.Key, image.Value))                
                    continue;

                string identifier = $"{image.Key}_{image.Value}";

                if (imageUrls.URLs.ContainsKey(identifier) && !newLoadOrderURL.ContainsKey(identifier))
                {
                    newLoadOrderURL.Add(identifier, imageUrls.URLs[identifier]);
                }
                else
                {
                    workshopDownloads.Add(new KeyValuePair<string, ulong>(image.Key, image.Value));
                }
            }

            if (workshopDownloads.Count > 0)
            {
                QueueWorkshopDownload(title, newLoadOrderURL, workshopDownloads, 0, callback);
                return;
            }

            if (newLoadOrderURL.Count > 0)
            {
                loadOrders.Enqueue(new LoadOrder(title, newLoadOrderURL, null, false, callback));
                if (!orderPending)
                    ServerMgr.Instance.StartCoroutine(ProcessLoadOrders());
            }
            else
            {
                if (callback != null)
                    callback.Invoke();
            }
        }

        [HookMethod("RemoveImage")]
        public void RemoveImage(string imageName, ulong imageId)
        {
            if (!HasImage(imageName, imageId))
                return;

            uint crc = uint.Parse(GetImage(imageName, imageId));
            FileStorage.server.Remove(crc, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID);
        }

        [HookMethod("SendImage")]
        public void SendImage(BasePlayer player, string imageName, ulong imageId = 0)
        {
            if (!HasImage(imageName, imageId) || player?.net?.connection == null)
                return;

            uint crc = uint.Parse(GetImage(imageName, imageId));
            byte[] array = FileStorage.server.Get(crc, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID);

            if (array == null)
                return;

            CommunityEntity.ServerInstance.ClientRPCEx<uint, uint, byte[]>(new Network.SendInfo(player.net.connection)
            {
                channel = 2,
                method = Network.SendMethod.Reliable
            }, null, "CL_ReceiveFilePng", crc, (uint)array.Length, array);
        }
        #endregion API

        #region Steam API
        private List<ulong> BuildApprovedItemList()
        {
            List<ulong> list = new List<ulong>();

            foreach (InventoryDef item in Steamworks.SteamInventory.Definitions)
            {
                string shortname = item.GetProperty("itemshortname");
                ulong workshopid;

                if (item == null || string.IsNullOrEmpty(shortname))
                    continue;

                if (workshopNameToShortname.ContainsKey(shortname))
                    shortname = workshopNameToShortname[shortname];

                if (item.Id < 100)
                    continue;

                if (!ulong.TryParse(item.GetProperty("workshopid"), out workshopid))
                    continue;

                if (HasImage(shortname, workshopid))
                    continue;

                list.Add(workshopid);
            }

            return list;
        }

        private string BuildDetailsString(List<ulong> list, int page)
        {            
            int totalPages = Mathf.CeilToInt((float)list.Count / 100f);
            int index = page * 100;
            int limit = Mathf.Min((page + 1) * 100, list.Count);
            string details = string.Format("?key={0}&itemcount={1}", configData.SteamAPIKey, (limit - index));

            for (int i = index; i < limit; i++)            
                details += string.Format("&publishedfileids[{0}]={1}", i - index, list[i]);
            
            return details;
        }

        private string BuildDetailsString(List<ulong> list)
        {            
            string details = string.Format("?key={0}&itemcount={1}", configData.SteamAPIKey, list.Count);

            for (int i = 0; i < list.Count; i++)
                details += string.Format("&publishedfileids[{0}]={1}", i, list[i]);

            return details;
        }

        private bool IsValid(PublishedFileDetails item)
        {
            if (string.IsNullOrEmpty(item.preview_url))
                return false;

            if (item.tags == null)
                return false;

            return true;
        }

        private void GetItemSkins()
        {
            Steamworks.SteamInventory.OnDefinitionsUpdated -= GetItemSkins;

            PrintWarning("Retrieving item skin lists...");

            GetApprovedItemSkins(BuildApprovedItemList(), 0);
        }

        private void QueueFileQueryRequest(string details, Action<PublishedFileDetails[]> callback)
        {
            webrequest.Enqueue(STEAM_API_URL, details, (code, response) =>
            {
                try
                {
                    QueryResponse query = JsonConvert.DeserializeObject<QueryResponse>(response, errorHandling);
                    if (query == null || query.response == null || query.response.publishedfiledetails.Length == 0)
                    {
                        if (code != 200)
                            PrintError($"There was a error querying Steam for workshop item data : Code ({code})\n{details}");
                        return;
                    }
                    else
                    {
                        if (query?.response?.publishedfiledetails?.Length > 0)
                            callback.Invoke(query.response.publishedfiledetails);
                    }
                }
                catch { }
            }, this, Core.Libraries.RequestMethod.POST);
        }

        private void GetApprovedItemSkins(List<ulong> itemsToDownload, int page)
        {
            if (itemsToDownload.Count < 1)
            {
                Puts("Approved skins loaded");

                SaveUrls();
                SaveSkinInfo();

                if (!orderPending)
                    ServerMgr.Instance.StartCoroutine(ProcessLoadOrders());
                return;
            }

            int totalPages = Mathf.CeilToInt((float)itemsToDownload.Count / 100f) - 1;

            string details = BuildDetailsString(itemsToDownload, page);

            QueueFileQueryRequest(details, (PublishedFileDetails[] items) =>
            {
                ServerMgr.Instance.StartCoroutine(ProcessApprovedBlock(itemsToDownload, items, page, totalPages));
            });
        }

        private IEnumerator ProcessApprovedBlock(List<ulong> itemsToDownload, PublishedFileDetails[] items, int page, int totalPages)
        {
            PrintWarning($"Processing approved skins; Page {page + 1}/{totalPages + 1}");

            Dictionary<string, Dictionary<ulong, string>> loadOrder = new Dictionary<string, Dictionary<ulong, string>>();

            foreach (PublishedFileDetails item in items)
            {
                if (!IsValid(item))
                    continue;

                foreach (PublishedFileDetails.Tag tag in item.tags)
                {
                    if (string.IsNullOrEmpty(tag.tag))
                        continue;

                    ulong workshopid = Convert.ToUInt64(item.publishedfileid);

                    string adjTag = tag.tag.ToLower().Replace("skin", "").Replace(" ", "").Replace("-", "").Replace(".item", "");
                    if (workshopNameToShortname.ContainsKey(adjTag))
                    {
                        string shortname = workshopNameToShortname[adjTag];

                        string identifier = $"{shortname}_{workshopid}";

                        if (!imageUrls.URLs.ContainsKey(identifier))
                            imageUrls.URLs.Add(identifier, item.preview_url.Replace("https", "http"));

                        skinInformation.skinData[identifier] = new Dictionary<string, object>
                                {
                                    {"title", item.title },
                                    {"votesup", 0 },
                                    {"votesdown", 0 },
                                    {"description", item.file_description },
                                    {"score", 0 },
                                    {"views", 0 },
                                    {"created", new DateTime() },
                                };
                    }
                }
            }

            yield return CoroutineEx.waitForEndOfFrame;
            yield return CoroutineEx.waitForEndOfFrame;

            if (page < totalPages)
                GetApprovedItemSkins(itemsToDownload, page + 1);
            else
            {
                itemsToDownload.Clear();

                Puts("Approved skins loaded");

                SaveUrls();
                SaveSkinInfo();

                if (!orderPending)
                    ServerMgr.Instance.StartCoroutine(ProcessLoadOrders());
            }
        }

        private void QueueWorkshopDownload(string title, Dictionary<string, string> newLoadOrderURL, List<KeyValuePair<string, ulong>> workshopDownloads, int page = 0, Action callback = null)
        {
            int rangeMin = page * 100;
            int rangeMax = (page + 1) * 100;

            if (rangeMax > workshopDownloads.Count)
                rangeMax = workshopDownloads.Count;

            List<ulong> requestedSkins = workshopDownloads.GetRange(rangeMin, rangeMax - rangeMin).Select(x => x.Value).ToList();

            int totalPages = Mathf.CeilToInt((float)workshopDownloads.Count / 100f) - 1;

            string details = BuildDetailsString(requestedSkins);

            try
            {
                webrequest.Enqueue(STEAM_API_URL, details, (code, response) =>
                {
                    QueryResponse query = JsonConvert.DeserializeObject<QueryResponse>(response, errorHandling);
                    if (query == null || query.response == null || query.response.publishedfiledetails.Length == 0)
                    {
                        if (code != 200)
                            PrintError($"There was a error querying Steam for workshop item data : Code ({code})");

                        if (page < totalPages)
                            QueueWorkshopDownload(title, newLoadOrderURL, workshopDownloads, page + 1, callback);
                        else
                        {
                            if (newLoadOrderURL.Count > 0)
                            {
                                loadOrders.Enqueue(new LoadOrder(title, newLoadOrderURL, null, false, page < totalPages ? null : callback));
                                if (!orderPending)
                                    ServerMgr.Instance.StartCoroutine(ProcessLoadOrders());
                            }
                            else
                            {
                                if (callback != null)
                                    callback.Invoke();
                            }
                        }
                        return;
                    }
                    else
                    {
                        if (query.response.publishedfiledetails.Length > 0)
                        {
                            Dictionary<string, Dictionary<ulong, string>> loadOrder = new Dictionary<string, Dictionary<ulong, string>>();

                            foreach (PublishedFileDetails item in query.response.publishedfiledetails)
                            {
                                if (!string.IsNullOrEmpty(item.preview_url))
                                {
                                    ulong skinId = Convert.ToUInt64(item.publishedfileid);

                                    KeyValuePair<string, ulong>? kvp = workshopDownloads.Find(x => x.Value == skinId);

                                    if (kvp.HasValue)
                                    {
                                        string identifier = $"{kvp.Value.Key}_{kvp.Value.Value}";

                                        if (!newLoadOrderURL.ContainsKey(identifier))
                                            newLoadOrderURL.Add(identifier, item.preview_url);

                                        if (!imageUrls.URLs.ContainsKey(identifier))
                                            imageUrls.URLs.Add(identifier, item.preview_url);

                                        skinInformation.skinData[identifier] = new Dictionary<string, object>
                                        {
                                            {"title", item.title },
                                            {"votesup",  0 },
                                            {"votesdown", 0 },
                                            {"description", item.file_description },
                                            {"score", 0 },
                                            {"views", item.views },
                                            {"created", new DateTime(item.time_created) },
                                        };

                                        requestedSkins.Remove(skinId);
                                    }
                                }
                            }

                            SaveUrls();
                            SaveSkinInfo();

                            if (requestedSkins.Count != 0)
                            {
                                Puts($"{requestedSkins.Count} workshop skin ID's for image batch ({title}) are invalid! They may have been removed from the workshop\nIDs: {requestedSkins.ToSentence()}");
                            }
                        }

                        if (page < totalPages)
                            QueueWorkshopDownload(title, newLoadOrderURL, workshopDownloads, page + 1, callback);
                        else
                        {
                            if (newLoadOrderURL.Count > 0)
                            {
                                loadOrders.Enqueue(new LoadOrder(title, newLoadOrderURL, null, false, page < totalPages ? null : callback));
                                if (!orderPending)
                                    ServerMgr.Instance.StartCoroutine(ProcessLoadOrders());
                            }
                            else
                            {
                                if (callback != null)
                                    callback.Invoke();
                            }
                        }
                    }
                },
                this,
                Core.Libraries.RequestMethod.POST);
            }
            catch { }
        }

        #region JSON Response Classes
        public class QueryResponse
        {
            public Response response;
        }

        public class Response
        {
            public int total;
            public PublishedFileDetails[] publishedfiledetails;
        }

        public class PublishedFileDetails
        {
            public int result;
            public string publishedfileid;
            public string creator;
            public int creator_appid;
            public int consumer_appid;
            public int consumer_shortcutid;
            public string filename;
            public string file_size;
            public string preview_file_size;
            public string file_url;
            public string preview_url;
            public string url;
            public string hcontent_file;
            public string hcontent_preview;
            public string title;
            public string file_description;
            public int time_created;
            public int time_updated;
            public int visibility;
            public int flags;
            public bool workshop_file;
            public bool workshop_accepted;
            public bool show_subscribe_all;
            public int num_comments_public;
            public bool banned;
            public string ban_reason;
            public string banner;
            public bool can_be_deleted;
            public string app_name;
            public int file_type;
            public bool can_subscribe;
            public int subscriptions;
            public int favorited;
            public int followers;
            public int lifetime_subscriptions;
            public int lifetime_favorited;
            public int lifetime_followers;
            public string lifetime_playtime;
            public string lifetime_playtime_sessions;
            public int views;
            public int num_children;
            public int num_reports;
            public Preview[] previews;
            public Tag[] tags;
            public int language;
            public bool maybe_inappropriate_sex;
            public bool maybe_inappropriate_violence;

            public class Tag
            {
                public string tag;
                public bool adminonly;
            }

        }

        public class Preview
        {
            public string previewid;
            public int sortorder;
            public string url;
            public int size;
            public string filename;
            public int preview_type;
            public string youtubevideoid;
            public string external_reference;
        }
        #endregion
        #endregion

        #region Commands

        [ConsoleCommand("cancelstorage")]
        private void cmdCancelStorage(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null || arg.Connection.authLevel > 0)
            {
                if (!orderPending)
                    PrintWarning("No images are currently being downloaded");
                else
                {
                    assets.ClearList();
                    loadOrders.Clear();
                    PrintWarning("Pending image downloads have been cancelled!");
                }
            }
        }

        private List<ulong> pendingAnswers = new List<ulong>();

        [ConsoleCommand("refreshallimages")]
        private void cmdRefreshAllImages(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null || arg.Connection.authLevel > 0)
            {
                SendReply(arg, "Running this command will wipe all of your ImageLibrary data, meaning every registered image will need to be re-downloaded. Are you sure you wish to continue? (type yes or no)");

                ulong userId = arg.Connection == null || arg.IsRcon ? 0U : arg.Connection.userid;
                if (!pendingAnswers.Contains(userId))
                {
                    pendingAnswers.Add(userId);
                    timer.In(5, () =>
                    {
                        if (pendingAnswers.Contains(userId))
                            pendingAnswers.Remove(userId);
                    });
                }
            }
        }

        [ConsoleCommand("yes")]
        private void cmdRefreshAllImagesYes(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null || arg.Connection.authLevel > 0)
            {
                ulong userId = arg.Connection == null || arg.IsRcon ? 0U : arg.Connection.userid;
                if (pendingAnswers.Contains(userId))
                {
                    PrintWarning("Wiping ImageLibrary data and redownloading ImageLibrary specific images. All plugins that have registered images via ImageLibrary will need to be re-loaded!");
                    RefreshImagery();

                    pendingAnswers.Remove(userId);
                }
            }
        }

        [ConsoleCommand("no")]
        private void cmdRefreshAllImagesNo(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null || arg.Connection.authLevel > 0)
            {
                ulong userId = arg.Connection == null || arg.IsRcon ? 0U : arg.Connection.userid;

                if (pendingAnswers.Contains(userId))
                {
                    SendReply(arg, "ImageLibrary data wipe aborted!");
                    pendingAnswers.Remove(userId);
                }
            }
        }

        #endregion Commands

        #region Image Storage

        private struct LoadOrder
        {
            public string loadName;
            public bool loadSilent;

            public Dictionary<string, string> imageList;
            public Dictionary<string, byte[]> imageData;

            public Action callback;

            public LoadOrder(string loadName, Dictionary<string, string> imageList, bool loadSilent = false, Action callback = null)
            {
                this.loadName = loadName;
                this.imageList = imageList;
                this.imageData = null;
                this.loadSilent = loadSilent;
                this.callback = callback;
            }
            public LoadOrder(string loadName, Dictionary<string, byte[]> imageData, bool loadSilent = false, Action callback = null)
            {
                this.loadName = loadName;
                this.imageList = null;
                this.imageData = imageData;
                this.loadSilent = loadSilent;
                this.callback = callback;
            }
            public LoadOrder(string loadName, Dictionary<string, string> imageList, Dictionary<string, byte[]> imageData, bool loadSilent = false, Action callback = null)
            {
                this.loadName = loadName;
                this.imageList = imageList;
                this.imageData = imageData;
                this.loadSilent = loadSilent;
                this.callback = callback;
            }
        }

        private class ImageAssets : MonoBehaviour
        {
            private Queue<QueueItem> queueList = new Queue<QueueItem>();
            private bool isLoading;
            private double nextUpdate;
            private int listCount;
            private string request;

            private Action callback;

            private void OnDestroy()
            {
                queueList.Clear();
            }

            public void Add(string name, string url = null, byte[] bytes = null)
            {
                queueList.Enqueue(new QueueItem(name, url, bytes));
            }

            public void RegisterCallback(Action callback) => this.callback = callback;

            public void BeginLoad(string request)
            {
                this.request = request;
                nextUpdate = UnityEngine.Time.time + il.configData.UpdateInterval;
                listCount = queueList.Count;
                Next();
            }

            public void ClearList()
            {
                queueList.Clear();
                il.orderPending = false;
            }

            private void Next()
            {
                if (queueList.Count == 0)
                {
                    il.orderPending = false;
                    il.SaveData();
                    if (!string.IsNullOrEmpty(request))
                        print($"Image batch ({request}) has been stored successfully");

                    request = string.Empty;
                    listCount = 0;

                    if (callback != null)
                        callback.Invoke();

                    StartCoroutine(il.ProcessLoadOrders());
                    return;
                }
                if (il.configData.ShowProgress && listCount > 1)
                {
                    var time = UnityEngine.Time.time;
                    if (time > nextUpdate)
                    {
                        var amountDone = listCount - queueList.Count;
                        print($"{request} storage process at {Math.Round((amountDone / (float)listCount) * 100, 0)}% ({amountDone}/{listCount})");
                        nextUpdate = time + il.configData.UpdateInterval;
                    }
                }
                isLoading = true;

                QueueItem queueItem = queueList.Dequeue();
                if (!string.IsNullOrEmpty(queueItem.url))
                    StartCoroutine(DownloadImage(queueItem));
                else StoreByteArray(queueItem.bytes, queueItem.name);
            }

            private IEnumerator DownloadImage(QueueItem info)
            {
                UnityWebRequest www = UnityWebRequest.Get(info.url);

                yield return www.SendWebRequest();
                if (il == null) yield break;
                if (www.isNetworkError || www.isHttpError)
                {
                    print(string.Format("Image failed to download! Error: {0} - Image Name: {1} - Image URL: {2}", www.error, info.name, info.url));
                    www.Dispose();
                    isLoading = false;
                    Next();
                    yield break;
                }

                if (www?.downloadHandler?.data != null)
                {
                    Texture2D texture = new Texture2D(2, 2);
                    texture.LoadImage(www.downloadHandler.data);
                    if (texture != null)
                    {
                        bool shouldStore = true;
                        byte[] bytes = texture.EncodeToPNG();

                        if (bytes.Length > 3145728)
                        {
                            Debug.Log($"[ImageLibrary] Failed to store image data for image : {info.name} for equest {request}\nURL: {info.url}\n{bytes.Length} bytes is larger then the allowed transferable size of 3145728 bytes");
                            shouldStore = false;
                        }

                        DestroyImmediate(texture);

                        if (shouldStore)
                            StoreByteArray(bytes, info.name);
                    }
                }
                www.Dispose();
            }

            private void StoreByteArray(byte[] bytes, string name)
            {
                if (bytes != null)
                    il.imageIdentifiers.imageIds[name] = FileStorage.server.Store(bytes, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString();
                isLoading = false;
                Next();
            }

            private class QueueItem
            {
                public byte[] bytes;
                public string url;
                public string name;
                public QueueItem(string name, string url = null, byte[] bytes = null)
                {
                    this.bytes = bytes;
                    this.url = url;
                    this.name = name;
                }
            }
        }

        #endregion Image Storage

        #region Config

        private ConfigData configData;

        class ConfigData
        {
            [JsonProperty(PropertyName = "Avatars - Store player avatars")]
            public bool StoreAvatars { get; set; }

            [JsonProperty(PropertyName = "Steam API key (get one here https://steamcommunity.com/dev/apikey)")]
            public string SteamAPIKey { get; set; }

            [JsonProperty(PropertyName = "URL to web folder containing all item icons")]
            public string ImageURL { get; set; }

            [JsonProperty(PropertyName = "Progress - Show download progress in console")]
            public bool ShowProgress { get; set; }

            [JsonProperty(PropertyName = "Progress - Time between update notifications")]
            public int UpdateInterval { get; set; }
            
            [JsonProperty(PropertyName = "User Images - Manually define images to be loaded")]
            public Dictionary<string, string> UserImages { get; set; }

            public Oxide.Core.VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            configData = Config.ReadObject<ConfigData>();

            if (configData.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(configData, true);
        }

        protected override void LoadDefaultConfig() => configData = GetBaseConfig();

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                ShowProgress = true,
                SteamAPIKey = string.Empty,
                StoreAvatars = false,
                UpdateInterval = 20,
                ImageURL = "https://www.rustedit.io/images/imagelibrary/",
                UserImages = new Dictionary<string, string>(),
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            if (configData.Version < new VersionNumber(2, 0, 47))
                configData = baseConfig;

            if (configData.Version < new VersionNumber(2, 0, 53))
                configData.StoreAvatars = false;

            if (configData.Version < new VersionNumber(2, 0, 55))
                configData.ImageURL = baseConfig.ImageURL;

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion Config

        #region Data Management

        private void SaveData() => identifiers.WriteObject(imageIdentifiers);

        private void SaveSkinInfo() => skininfo.WriteObject(skinInformation);

        private void SaveUrls() => urls.WriteObject(imageUrls);

        private void LoadData()
        {
            try
            {
                imageIdentifiers = identifiers.ReadObject<ImageIdentifiers>();
            }
            catch
            {
                imageIdentifiers = new ImageIdentifiers();
            }
            try
            {
                skinInformation = skininfo.ReadObject<SkinInformation>();
            }
            catch
            {
                skinInformation = new SkinInformation();
            }
            try
            {
                imageUrls = urls.ReadObject<ImageURLs>();
            }
            catch
            {
                imageUrls = new ImageURLs();
            }
            if (skinInformation == null)
                skinInformation = new SkinInformation();
            if (imageIdentifiers == null)
                imageIdentifiers = new ImageIdentifiers();
            if (imageUrls == null)
                imageUrls = new ImageURLs();
        }

        private class ImageIdentifiers
        {
            public uint lastCEID;
            public Hash<string, string> imageIds = new Hash<string, string>();
        }

        private class SkinInformation
        {
            public Hash<string, Dictionary<string, object>> skinData = new Hash<string, Dictionary<string, object>>();
        }

        private class ImageURLs
        {
            public Hash<string, string> URLs = new Hash<string, string>();
        }


        public class AvatarRoot
        {
            public Response response { get; set; }

            public class Response
            {
                public Player[] players { get; set; }

                public class Player
                {
                    public string steamid { get; set; }
                    public int communityvisibilitystate { get; set; }
                    public int profilestate { get; set; }
                    public string personaname { get; set; }
                    public int lastlogoff { get; set; }
                    public string profileurl { get; set; }
                    public string avatar { get; set; }
                    public string avatarmedium { get; set; }
                    public string avatarfull { get; set; }
                    public int personastate { get; set; }
                    public string realname { get; set; }
                    public string primaryclanid { get; set; }
                    public int timecreated { get; set; }
                    public int personastateflags { get; set; }
                }
            }
        }
        #endregion Data Management
    }
}
