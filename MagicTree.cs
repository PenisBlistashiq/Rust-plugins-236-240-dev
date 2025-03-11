using Newtonsoft.Json;
using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Oxide.Plugins
{
    [Info("MagicTree", "Drop Dead", "1.0.4")]
    public class MagicTree : RustPlugin
    {
        #region Configuration
        public class Seed
        {
            public string shortname;
            public string name;
            public ulong skinId;
        }

        public class Wood
        {
            [JsonProperty("UID Дерева")]
            public uint woodId;
            [JsonProperty("Осталось времени")]
            public int NeedTime;

            [JsonProperty("Оставшееся время до разрушения")]
            public int NeedTimeToDestroy = -1;
            [JsonProperty("Этап")]
            public int CurrentStage;
            [JsonProperty("Позиция")]
            public Vector3 woodPos;
            [JsonProperty("Ящики")]
            public List<uint> BoxListed = new List<uint>();
            [JsonIgnore] public List<BaseEntity> boxes = new List<BaseEntity>();
        }

        public class BoxItemsList
        {
            [JsonProperty("Shortname выпадаемого предмета")]
            public string ShortName;
            [JsonProperty("Минимальное количество выпадаемого предмета")]
            public int MinAmount;
            [JsonProperty("Максимальное количество выпадаемого предмета")]
            public int MaxAmount;
            [JsonProperty("Шанс выпадения этого предмета")]
            public int Change;
            [JsonProperty("SkinID выпадаемого предмета")]
            public ulong SkinID;
            [JsonProperty("Кастомное название выпадаемого предмета (оставьте поле пустым чтобы использовать стандартное название)")]
            public string Name;
            [JsonProperty("Это чертеж? (true - да, false - нет)")]
            public bool IsBlueprnt;
        }


        public Dictionary<ulong, Dictionary<uint, Wood>> WoodsList = new Dictionary<ulong, Dictionary<uint, Wood>>();

        public Dictionary<string, string> Messages = new Dictionary<string, string>()
        {
            {"CmdError", "Ошибка в синтаксисе команды." },
            {"Cupboard", "Запрещено садить семечко без шкафа" },
            {"WaterBlock", "Запрещено садить семечко в воде!" },
            {"DisablePlantSeed", "Семена разрешено садить только в землю!" },
            {"CountError", "Вы указали неверное количество" },
            {"Permission", "У вас нет прав на использование этой команды" },
            {"SeedGived", "Вам выпала семечка магического дерева!\nПосадите ее и у вас вырастет необычное дерево на котором растут ящики с ценными предметами!" },
            {"Wood", "Вы посадили магическое дерево\nСкоро оно вырастет и даст плоды.." },
            {"InfoTextFull",  "<size=28><b>Магическое дерево</b></size>\n<size=17>\nПЛОДЫ ДОЗРЕЛИ, ВЫ МОЖЕТЕ ИХ СОБРАТЬ</size>"},
            {"InfoDdraw", "<size=28><b>Магическое дерево</b></size>\n<size=17>Этап созревания дерева: {0}/{1}\nВремя до полного созревания: {2}</size>" }
        };

        private PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();

            if (config.PluginVersion < Version)
                UpdateConfigValues();

            Config.WriteObject(config, true);
        }

        private void UpdateConfigValues()
        {
            PluginConfig baseConfig = PluginConfig.DefaultConfig();
            if (config.PluginVersion < Version)
            {
                PrintWarning("Config update detected! Updating config values...");
                PrintWarning("Config update completed!");
            }
            config.PluginVersion = Version;
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        private class PluginConfig
        {
            [JsonProperty("Время до полного роста дерева в секундах")]
            public int Time = 10;

            [JsonProperty("Время существования дерева после полного созревания")]
            public int TimetoDestroy = 3600;
            [JsonProperty("Запретить игрокам садить семечку в воде?")]
            public bool waterblock = true;

            [JsonProperty("Посадка деревьев разрешена только в земле (запрещены плантации и прочее)? (true - да, false - нет)")]
            public bool PlanterBoxDisable = true;

            [JsonProperty("Множитель добычи при финальной срубке магического дерева")]
            public int Bonus = 1;

            [JsonProperty("Количество вещей в ящике")]
            public int ItemsCount;

            [JsonProperty("Количество ящиков на дереве")]
            public int BoxCount;

            [JsonProperty("Пермишн для доступа к команде /seed (должен начинаться с MagicTree.)")]
            public string Permission = "MagicTree.Seed";

            [JsonProperty("Шанс выпадения семечки с дерева (макс-100)")]
            public int Chance;

            [JsonProperty("Настройка лута в ящиках", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<BoxItemsList> casesItems;
            [JsonProperty("Настройка зерна")]
            public Seed seed;
            [JsonProperty("Версия конфигурации")]
            public VersionNumber PluginVersion = new VersionNumber();

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    PluginVersion = new VersionNumber(),
                    ItemsCount = 2,
                    Permission = "MagicTree.perm",
                    BoxCount = 4,
                    Chance = 5,
                    Time = 10,
                    waterblock = true,
                    seed = new Seed()
                    {
                        shortname = "seed.hemp",
                        name = "Семена магического дерева",
                        skinId = 1787823357
                    },
                    casesItems = new List<BoxItemsList>()
                    {
                        new BoxItemsList
                        {
                            ShortName = "stones",
                            MinAmount = 300,
                            MaxAmount = 1000,
                            Change = 100,
                            Name = "",
                            SkinID = 0,
                            IsBlueprnt = false
                        },
                        new BoxItemsList
                        {
                            ShortName = "wood",
                            MinAmount = 1000,
                            MaxAmount = 5000,
                            Change = 100,
                            Name = "",
                            SkinID = 0,
                            IsBlueprnt = false
                        },
                    },
                };
            }
        }

        string CrateBasic = "assets/bundled/prefabs/radtown/crate_underwater_basic.prefab";
        string SucEffect = "assets/prefabs/misc/xmas/candy cane club/effects/hit.prefab";
        string ErrorEffect = "assets/prefabs/locks/keypad/effects/lock.code.denied.prefab";
        List<string> Stages = new List<string>()
        {
            "assets/prefabs/plants/hemp/hemp.entity.prefab",
            "assets/bundled/prefabs/autospawn/resource/v2_tundra_forest_small/american_beech_e_dead.prefab",
            "assets/bundled/prefabs/autospawn/resource/v2_tundra_forest_small/american_beech_d_dead.prefab",
            "assets/bundled/prefabs/autospawn/resource/v2_tundra_forest_small/oak_a_tundra.prefab",
            "assets/bundled/prefabs/autospawn/resource/v2_tundra_forest/oak_b_tundra.prefab"
        };

        #endregion

        #region Oxide

        void LoadData()
        {
            try
            {
                WoodsList = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, Dictionary<uint, Wood>>>($"{Title}/Players");
                if (WoodsList == null) WoodsList = new Dictionary<ulong, Dictionary<uint, Wood>>();
            }
            catch { WoodsList = new Dictionary<ulong, Dictionary<uint, Wood>>(); }
        }

        void SaveData()
        {
            if (WoodsList != null) Interface.Oxide.DataFileSystem.WriteObject($"{Title}/Players", WoodsList);
        }

        public static MagicTree ins;

        void OnEntityKill(TreeEntity entity)
        {
            if (entity == null || entity?.net.ID == null || entity.OwnerID == 0) return;
            if (entity.GetComponent<TreeEntity>() != null && entity.GetComponent<TreeConponent>() != null)
            {
                var tree = entity.GetComponent<TreeEntity>();
                if (WoodsList.ContainsKey(tree.OwnerID) && WoodsList[tree.OwnerID].ContainsKey(tree.net.ID)) WoodsList[tree.OwnerID].Remove(tree.net.ID);
            }
        }

        void OnServerSave()
        {
            SaveData();
        }

        void Loaded()
        {
            ins = this;
            permission.RegisterPermission(config.Permission, this);
            lang.RegisterMessages(Messages, this, "en");
            Messages = lang.GetMessages("en", this);
            LoadData();
        }

        private void OnServerInitialized()
        {
            foreach (var tree in WoodsList)
            {
                foreach (var entity in tree.Value.Keys)
                {
                    BaseNetworkable entitys = BaseNetworkable.serverEntities.Find(entity);
                    if (entitys != null && entitys is TreeEntity)
                        AddOrRemoveComponent("add", null, entitys.GetComponent<TreeEntity>(), entitys.GetComponent<TreeEntity>().OwnerID);
                    else if (entitys != null && entitys is GrowableEntity)
                        AddOrRemoveComponent("add", null, entitys.GetComponent<GrowableEntity>(), entitys.GetComponent<GrowableEntity>().OwnerID);
                    else
                        NextTick(() => { tree.Value.Remove(entity); });
                }
            }
        }


        private List<TreeConponent> treeConponents = new List<TreeConponent>();


        void AddOrRemoveComponent(string type = "", TreeConponent component = null, BaseEntity tree = null, ulong playerid = 0)
        {
            if (!WoodsList.ContainsKey(playerid)) return;
            switch (type)
            {
                case "add":
                    if (!WoodsList[playerid].ContainsKey(tree.net.ID)) return;
                    var data = WoodsList[playerid][tree.net.ID];
                    if (tree != null && data != null)
                    {
                        if (WoodsList[playerid][tree.net.ID].CurrentStage > 2 && WoodsList[playerid][tree.net.ID].BoxListed.Count > 0)
                        {
                            GameObject treeObject = new GameObject();
                            treeObject.transform.position = tree.transform.position;
                            treeObject.AddComponent<TreeConponent>().Init(WoodsList[playerid][tree.net.ID], tree);
                            treeConponents.Add(treeObject.GetComponent<TreeConponent>());
                            SpawnBox(data, WoodsList[playerid][tree.net.ID].BoxListed.Count, tree, playerid, WoodsList[playerid][tree.net.ID].BoxListed.Count);
                            return;
                        }
                        else
                        {
                            GameObject treeObject = new GameObject();
                            treeObject.transform.position = tree.transform.position;
                            treeObject.AddComponent<TreeConponent>().Init(WoodsList[playerid][tree.net.ID], tree);
                            treeConponents.Add(treeObject.GetComponent<TreeConponent>());
                        }
                    }
                    break;
                case "remove":

                    if (component == null || component.IsDestroyed) return;
                    if (WoodsList[playerid][tree.net.ID].BoxListed.Count > 0)
                    {
                        foreach (var ent in WoodsList[playerid][tree.net.ID].boxes)
                        {
                            if (ent != null && !ent.IsDestroyed)
                                ent.Kill();
                        }
                        WoodsList[playerid][tree.net.ID].boxes.Clear();
                        if (component != null)
                            component.DestroyComponent();
                    }
                    else
                    {
                        if (component != null)
                            component.DestroyComponent();
                    }
                    break;
            }
        }

        void OnEntityBuilt(Planner planner, GameObject gameobject, Vector3 Pos)
        {
            if (planner == null || gameobject == null) return;
            var player = planner.GetOwnerPlayer();
            if (player == null) return;
            BaseEntity entity = gameobject.ToBaseEntity();
            if (entity == null) return;
            if (entity.skinID == config.seed.skinId)
            {
                NextTick(() =>
                {
                    if (entity != null && !entity.IsDestroyed)
                    {
                        if (entity.GetBuildingPrivilege() == null)
                        {
                            if (player == null) return;
                            player.ChatMessage(Messages["Cupboard"]);
                            NextTick(() => entity.Kill());
							AddSeed(player, 1);
                            return;
                        }
                        if (config.PlanterBoxDisable && entity.GetParentEntity() != null)
                        {
                            if (player == null) return;
                            player.ChatMessage(Messages["DisablePlantSeed"]);
                            NextTick(() => entity.Kill());
							AddSeed(player, 1);
                            return;
                        }
                        if (config.waterblock && entity.WaterFactor() >= 0.1)
                        {
                            if (player == null) return;
                            player.ChatMessage(Messages["WaterBlock"]);
                            NextTick(() => entity.Kill());
							AddSeed(player, 1);
                            return;
                        }

                        SpawnWood(player.userID, entity.transform.position, null, entity, null);
                        player.ChatMessage(string.Format(Messages["Wood"]));
                    }
                });
            }
        }

        object OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (dispenser == null || player == null || item == null) return null;
            switch (item.info.shortname)
            {
                case "wood":
                    if (UnityEngine.Random.Range(0f, 100f) < config.Chance)
                    {
                        var activeitem = player.GetActiveItem();
                        if (activeitem != null && !activeitem.info.shortname.Contains("chainsaw"))
                            AddSeed(player, 1);
                    }
                    if (config.Bonus > 1)
                    {
                        TreeEntity wood1 = dispenser.GetComponentInParent<TreeEntity>();
                        if (wood1 == null) return null;
                        if (!treeConponents.Any(p => p.tree == wood1)) return null;
                        var treeComponent = treeConponents.Find(p => p.tree == wood1);
                        if (wood1 != null && treeComponent != null)
                        {
                            item.amount = item.amount * config.Bonus;
                        }
                    }
                    break;
            }
            return null;
        }
        object OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (dispenser == null || item == null) return null;
            BasePlayer player = entity?.ToPlayer();
            if (player == null) return null;
            TreeEntity wood1 = dispenser.GetComponentInParent<TreeEntity>();
            if (wood1 == null || wood1.OwnerID == 0 || !treeConponents.Any(p => p.tree == wood1)) return null;

            switch (item.info.shortname)
            {
                case "wood":

                    var treeComponent = treeConponents.Find(p => p.tree == wood1);
                    if (wood1 != null && treeComponent != null)
                    {
                        var component = treeComponent;
                        if (component.data.boxes.Count > 0 && component.data.boxes.Count < 5)
                        {
                            var box = component.data.boxes.Last();
                            if (box != null && component.data.CurrentStage == Stages.Count)
                            {
                                box.SetFlag(BaseEntity.Flags.Busy, false, true);
                                box.gameObject.AddComponent<RigidbodyChecker>();
                                component.data.BoxListed.Remove(box.net.ID);
                                component.data.boxes.Remove(box);
                            }
                            return false;
                        }
                        else if (component.data.BoxListed.Count > 5 && component.data.CurrentStage == Stages.Count)
                        {
                            foreach (var box in component.data.boxes)
                            {
                                if (box != null)
                                {
                                    box.SetFlag(BaseEntity.Flags.Busy, false, true);
                                    box.gameObject.AddComponent<RigidbodyChecker>();
                                    component.data.BoxListed.Remove(box.net.ID);
                                    component.data.boxes.Remove(box);
                                }
                            }
                            component.data.BoxListed.Clear();
                            component.data.boxes.Clear();
                            return false;
                        }
                        else
                        {
                            if (component.data.CurrentStage == Stages.Count)
                            {
                                dispenser.AssignFinishBonus(player, 1);
                                HitInfo hitInfo = new HitInfo(player, wood1, Rust.DamageType.Generic, wood1.Health(), wood1.transform.position);
                                wood1.OnAttacked(hitInfo);

                                return false;
                            }

                        }
                    }
                    break;
            }
            return null;
        }

        public class RigidbodyChecker : MonoBehaviour
        {
            BaseEntity check;
            public void Awake()
            {
                check = GetComponent<BaseEntity>();
                InvokeRepeating(nameof(Untie), 0f, 2f);
            }

            public void Untie()
            {
                check.SetFlag(BaseEntity.Flags.Reserved8, false, false);
                var body = check.GetComponent<Rigidbody>();

                if (body != null)
                {
                    body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                    if (!IsOnGround(check.transform.position) && body.detectCollisions)
                    {
                        body.isKinematic = true;

                    }
                    else
                    {
                        Destroy(this);
                        return;
                    }

                    body.isKinematic = false;
                    body.mass = 10f;

                }
            }

            bool IsOnGround(Vector3 pos)
            {
                float y = TerrainMeta.HeightMap.GetHeight(pos);
                if ((pos.y - y) < 0.1f)
                    return true;

                return false;

            }
        }


        void Unload()
        {
            foreach (var tree in treeConponents) AddOrRemoveComponent("remove", tree, tree.tree, tree.tree.OwnerID);
            SaveData();
        }

        #endregion

        #region MyMethods

        public void SpawnWood(ulong player, Vector3 pos, BaseEntity tree, BaseEntity seed, TreeConponent oldComponent)
        {
            if (tree == null)
            {
                if (!WoodsList.ContainsKey(player))

                    WoodsList.Add(player, new Dictionary<uint, Wood>()
                    {
                        [seed.net.ID] = new Wood() { woodId = seed.net.ID, CurrentStage = 0, NeedTime = config.Time / Stages.Count, woodPos = seed.transform.position }
                    });

                else
                    WoodsList[player].Add(seed.net.ID, new Wood() { woodId = seed.net.ID, CurrentStage = 0, NeedTime = config.Time / Stages.Count, woodPos = seed.transform.position });

                GameObject treeObject = new GameObject();
                treeObject.transform.position = seed.transform.position;
                treeObject.AddComponent<TreeConponent>().Init(WoodsList[player][seed.net.ID], seed);
                treeConponents.Add(treeObject.GetComponent<TreeConponent>());

            }
            else
            {
                if (tree == null) return;
                var old = WoodsList[player][tree.net.ID];
                var current = ++old.CurrentStage;   
                TreeEntity Wood = GameManager.server.CreateEntity(Stages[current], pos) as TreeEntity;

                Wood.Spawn();
                Wood.GetComponent<TreeEntity>().OwnerID = player;
                if (WoodsList[player].ContainsKey(tree.net.ID)) WoodsList[player].Remove(tree.net.ID);

                WoodsList[player].Add(Wood.net.ID, new Wood() { woodId = Wood.net.ID, CurrentStage = current, NeedTime = config.Time / Stages.Count, woodPos = Wood.transform.position });

                GameObject treeObject = new GameObject();
                treeObject.transform.position = tree.transform.position;

                treeObject.AddComponent<TreeConponent>().Init(WoodsList[player][Wood.net.ID], Wood);
                Wood.SendNetworkUpdateImmediate();
                treeConponents.Add(treeObject.GetComponent<TreeConponent>());
                tree.KillMessage();
                if (oldComponent != null && treeConponents.Contains(oldComponent)) treeConponents.Remove(oldComponent);
            }
        }


        [ChatCommand("seed")]
        void GiveSeed(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin || permission.UserHasPermission(player.UserIDString, config.Permission))
            {
                if (args.Length == 1)
                {
                    int amount;
                    if (!int.TryParse(args[0], out amount))
                    {
                        player.ChatMessage("Вы не указали количество, используйте /seed AMOUNT");

                        return;
                    }
                    AddSeed(player, amount, false);
                    return;
                }
                if (args.Length > 0 && args.Length == 2)
                {
                    var target = BasePlayer.Find(args[0]);
                    if (target == null)
                    {
                        player.ChatMessage("Игрок не найден, попробуйте уточнить имя или SteamID, используйте /seed TARGETNAME/ID AMOUNT");
                        return;
                    }

                    int amount;
                    if (!int.TryParse(args[1], out amount))
                    {
                        player.ChatMessage("Вы не указали количество, используйте /seed TARGETNAME/ID AMOUNT");
                        return;
                    }
                    AddSeed(target, amount);
                }
            }
            else
            {
                player.ChatMessage(string.Format(Messages["Permission"]));
                Effect.server.Run(ErrorEffect, player, 0, Vector3.zero, Vector3.forward);
            }
        }

        void AddSeed(BasePlayer player, int amount, bool messages = true)
        {
            if (player == null) return;
            Item sd = ItemManager.CreateByName(config.seed.shortname, amount, config.seed.skinId);
            sd.name = config.seed.name;
            player.GiveItem(sd, BaseEntity.GiveItemReason.Crafted);
            if (messages) player.ChatMessage(string.Format(Messages["SeedGived"]));
            Effect.server.Run(SucEffect, player, 0, Vector3.zero, Vector3.forward);
        }

        public void SpawnBox(Wood wood, int i, BaseEntity tree, ulong ownerID, int countBox = 0)
        {
            if (wood == null || tree == null) return;
            if (wood != null)
            {
                wood.BoxListed.Clear();
                wood.boxes.Clear();
                if (countBox == 0) countBox = config.BoxCount;
                for (int count = 0; count < countBox; count++)
                {
                    Vector3 pos = new Vector3(UnityEngine.Random.Range(-5, 5), UnityEngine.Random.Range(5f, 9.0f), UnityEngine.Random.Range(-5, 5));
                    var boxed = GameManager.server.CreateEntity(CrateBasic, tree.transform.position + pos, new Quaternion());
                    if (boxed == null) return;
                    boxed.enableSaving = false;
                    boxed.GetComponent<LootContainer>().initialLootSpawn = false;
                    boxed.Spawn();
                    //boxed.SetFlag(BaseEntity.Flags.Reserved8, false, true);
                    //boxed.SetFlag(BaseEntity.Flags.Busy, true, true);
                    wood.BoxListed.Add(boxed.net.ID);
                    wood.boxes.Add(boxed);

                    var lc = boxed.GetComponent<LootContainer>();
                    if (lc != null) AddLoot(lc);
                }
            }
        }

        public void AddLoot(LootContainer box)
        {
            if (box == null) return;
            box.inventory.itemList.Clear();

            List<string> itemz = new List<string>();
            for (int i = 0; i < config.casesItems.Count; i++)
            {
                if (box.inventory.itemList.Count < config.ItemsCount)
                {
                    var radomitem = Oxide.Core.Random.Range(0, config.casesItems.Count - 1);
                    var item = config.casesItems[radomitem];
                    if (item == null) continue;
                    if (itemz.Contains(item.ShortName)) continue;
                    if (UnityEngine.Random.Range(0f, 100f) < item.Change)
                    {
                        var amount = UnityEngine.Random.Range(item.MinAmount, item.MaxAmount);
                        var newItem = item.IsBlueprnt ? ItemManager.CreateByName("blueprintbase") : ItemManager.CreateByName(item.ShortName, amount, item.SkinID);
                        if (newItem == null)
                        {
                            PrintError($"Предмет {item.ShortName} не найден!");
                            continue;
                        }

                        if (item.IsBlueprnt)
                        {
                            var bpItemDef = ItemManager.FindItemDefinition(ItemManager.CreateByName(item.ShortName, amount, item.SkinID).info.itemid);
                            if (bpItemDef == null)
                            {
                                PrintError($"Предмет {item.ShortName} для создания чертежа не найден!");
                                continue;
                            }
                            newItem.blueprintTarget = bpItemDef.itemid;
                        }

                        if (!string.IsNullOrEmpty(item.Name)) newItem.name = item.Name;
                        if (box.inventory.IsFull()) box.inventory.capacity++;
                        newItem.MoveToContainer(box.inventory);
                        itemz.Add(item.ShortName);
                    }
                }
            }
        }

        public class TreeConponent : BaseEntity
        {
            public BaseEntity tree;
            SphereCollider sphereCollider;
            public Wood data;

            void Awake()
            {
                sphereCollider = gameObject.GetComponent<SphereCollider>() ?? gameObject.AddComponent<SphereCollider>();
                sphereCollider.gameObject.layer = (int)Rust.Layer.Reserved1;
                sphereCollider.isTrigger = true;
                sphereCollider.radius = 6f;
            }

            public void Init(Wood wood, BaseEntity entity)
            {
                if (entity == null)
                {
                    Destroy(this);
                    return;
                }
                tree = entity;
                data = wood;
                InvokeRepeating(DrawInfo, 1f, 1);
            }

            private void OnTriggerStay(Collider other)
            {
                if (data == null || tree == null) return;
                if (other.GetComponentInParent<BasePlayer>() == null) return;
                var target = other.GetComponentInParent<BasePlayer>();
                if (target != null) DdrawInfo(target);
            }

            void DrawInfo()
            {
                if (data == null || tree == null)
                {
                    Destroy(this);
                    return;
                }

                if (data.NeedTime <= 0 && data.CurrentStage == ins.Stages.FindIndex(x => x == ins.Stages.Last()) && data.BoxListed.ToList().Count <= 0)
                {
                    ins.SpawnBox(data, 3, tree, tree.OwnerID);
                    data.CurrentStage = ins.Stages.Count;
                }
                if (data.NeedTime <= 0 && data.CurrentStage < ins.Stages.FindIndex(x => x == ins.Stages.Last()))
                {
                    ins.SpawnWood(tree.OwnerID, tree.transform.position, tree, null, this);
                }

                if (data.CurrentStage == ins.Stages.Count && data.BoxListed.ToList().Count > 0)
                {
                    data.NeedTimeToDestroy++;
                    if (data.NeedTimeToDestroy > ins.config.TimetoDestroy)
                    {
                        ins.WoodsList[tree.OwnerID].Remove(tree.net.ID);
                        HitInfo hitInfo = new HitInfo(new BaseEntity(), tree, Rust.DamageType.Generic, tree.Health(), tree.transform.position);
                        tree.OnAttacked(hitInfo);
                        Destroy(this);
                    }
                }
                data.NeedTime--;
            }


            void DdrawInfo(BasePlayer player)
            {
                if (player == null || !player.IsConnected || data == null || tree == null) return;
                if (data.CurrentStage == ins.Stages.Count && data.BoxListed.ToList().Count > 0)
                {
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true); 
                            
                    player.SendEntityUpdate();   
                    player.SendConsoleCommand("ddraw.text", Time.deltaTime + 0.005f, Color.white, tree.transform.position + Vector3.up, ins.Messages["InfoTextFull"]);
                    player.SendConsoleCommand("camspeed 0");
                            
                    if (player.Connection.authLevel < 2)
                        player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                    else player.SendConsoleCommand("camspeed 1");
                            
                    player.SendEntityUpdate();
                    return;
                }

                if (data.NeedTime > 0)
                {
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true); 
                            
                    player.SendEntityUpdate();   
                    player.SendConsoleCommand("ddraw.text", Time.deltaTime + 0.005f, Color.white, tree.transform.position + Vector3.up, string.Format(ins.Messages["InfoDdraw"], data.CurrentStage + 1, ins.Stages.Count, FormatShortTime(TimeSpan.FromSeconds(data.NeedTime))));
                    player.SendConsoleCommand("camspeed 0");
                            
                    if (player.Connection.authLevel < 2)
                        player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                    else player.SendConsoleCommand("camspeed 1");
                            
                    player.SendEntityUpdate();
                }
            }

            public static string FormatShortTime(TimeSpan time)
            {
                string result = string.Empty;
                result += $"{time.Hours.ToString("00")}:";
                result += $"{time.Minutes.ToString("00")}:";
                result += $"{time.Seconds.ToString("00")}";
                return result;
            }

            private static string Format(int units, string form1 = "", string form2 = "", string form3 = "⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠")
            {
                var tmp = units % 10;
                if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                    return $"{units} {form1}";
                if (tmp >= 2 && tmp <= 4)
                    return $"{units} {form2}";
                return $"{units} {form3}";
            }

            public void DestroyComponent() => Destroy(this);

            void OnDestroy()
            {
                if (data != null && data.BoxListed != null && data.BoxListed.Count > 0)
                foreach (var box in data.boxes.Where(p => p != null))
                {
                    box.SetFlag(BaseEntity.Flags.Busy, false, true);
                    box.gameObject.AddComponent<RigidbodyChecker>();
                }
            }
        }

        #endregion
    }
}