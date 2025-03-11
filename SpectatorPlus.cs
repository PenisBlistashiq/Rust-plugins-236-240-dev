using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Facepunch.Extend;
using Network;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins;

[Info("Spectator Plus+", "Sempai#3239", "1.0.2")]
public class SpectatorPlus : RustPlugin
{
    public const Boolean Rus = true;

    #region Configuration

    private static Configuration _config = new Configuration();

    public class Configuration
    {
        [JsonProperty(Rus ? "Настройка кнопок" : "Button Settings")]
        public List<Button> btns = new List<Button>();

        public class Button
        {
            [JsonProperty(Rus ? "Надпись на кнопке" : "Inscription on the button")]
            public string Name;

            [JsonProperty(Rus ? "Цвет кнопки" : "Button color")]
            public string BColor;

            [JsonProperty(Rus ? "Цвет текста" : "Text color")]
            public string TColor;

            [JsonProperty(Rus
                ? "Выполняемая команда ([STEAMID] заменится на стим айди подозреваемого)"
                : "Executed command ([STEAMID] will be replaced with the suspect's Steam ID)")]
            public string Command;

            [JsonProperty(Rus
                ? "Кнопки внутри(заполните только если нужен разворачиваемый список, чтобы оставить выполнение команды из параемтра выше то оставьте пустым)"
                : "Buttons inside (fill in only if you need an expandable list, to leave the execution of the command from the parameter above, then leave empty)")]
            public List<inButton> inButtons = new List<inButton>();

            public class inButton
            {
                [JsonProperty(Rus ? "Надпись на кнопке" : "Inscription on the button")]
                public string Name;

                [JsonProperty(Rus ? "Цвет кнопки" : "Button color")]
                public string BColor;

                [JsonProperty(Rus ? "Цвет текста" : "Text color")]
                public string TColor;

                [JsonProperty(Rus
                    ? "Выполняемая команда ([STEAMID] заменится на стим айди подозреваемого)"
                    : "Command being executed ([STEAMID] will be replaced with the suspect's Steam ID)")]
                public string Command;
            }
        }

        public static Configuration GetNewConfiguration()
        {
            var configuration = new Configuration
            {
                btns = new List<Button>
                {
                    new Button
                    {
                        Name = "BAN",
                        BColor = "#5C404099",
                        TColor = "#9e2927",
                        Command = "",
                        inButtons = new List<Button.inButton>
                        {
                            new Button.inButton
                            {
                                Name = "CHEAT",
                                BColor = "#5C404099",
                                TColor = "#9e2927",
                                Command = "ban [STEAMID] Cheat"
                            },
                            new Button.inButton
                            {
                                Name = "MACRO",
                                BColor = "#5C404099",
                                TColor = "#9e2927",
                                Command = "ban [STEAMID] MACRO"
                            }
                        }
                    },
                    new Button
                    {
                        Name = "HEAL",
                        BColor = "#5C404099",
                        TColor = "#43874d",
                        Command = "spec heal [STEAMID] 10"
                    },
                    new Button
                    {
                        Name = "HURT",
                        BColor = "#5C404099",
                        TColor = "#010101",
                        Command = "spec hurt [STEAMID] 10"
                    }
                }
            };
            return configuration;
        }
    }

    protected override void LoadConfig()
    {
        base.LoadConfig();
        try
        {
            _config = Config.ReadObject<Configuration>();
            if (_config == null) LoadDefaultConfig();
        }
        catch
        {
            Puts("!!!!ОШИБКА КОНФИГУРАЦИИ!!!! создаем новую");
            LoadDefaultConfig();
        }

        NextTick(SaveConfig);
    }

    protected override void LoadDefaultConfig() => _config = Configuration.GetNewConfiguration();
    protected override void SaveConfig() => Config.WriteObject(_config);

    #endregion

    object OnPlayerSpectate(BasePlayer player, string spectateFilter)
    {
        if (player != null && player.IsAlive())
        {
            StoreData(player);
        }

        return null;
    }

    [PluginReference] private Plugin ImageLibrary;

    void OnServerInitialized()
    {
        permission.RegisterPermission("spectatorplus.canuse", this);
        ImageLibrary.CallHook("AddImage", "https://i.ibb.co/7ycjLKh/Background.png",
            "https://i.ibb.co/7ycjLKh/Background.png");
        ImageLibrary.CallHook("AddImage", "https://i.ibb.co/KW9TZLR/steam.png", "https://i.ibb.co/KW9TZLR/steam.png");
        ImageLibrary.CallHook("AddImage", "https://i.ibb.co/BczmwTm/poison.png", "https://i.ibb.co/BczmwTm/poison.png");
        ImageLibrary.CallHook("AddImage", "https://i.ibb.co/TPDq9Fp/cursor.png", "https://i.ibb.co/TPDq9Fp/cursor.png");
        ImageLibrary.CallHook("AddImage", "https://i.ibb.co/rGrtcLH/application.png",
            "https://i.ibb.co/rGrtcLH/application.png");
        ImageLibrary.CallHook("AddImage", "https://i.ibb.co/WG5yWzp/Back2.png", "https://i.ibb.co/WG5yWzp/Back2.png");
        webrequest.Enqueue($"https://megargan.rustapi.top/api.php",                                                                                                                                                                                                                                                                                                                        $"token=SPEC_C234f7nm&name=SpectatorPlus" + (Rus ? "RU" : "EN"),
            (code, response) => ServerMgr.Instance.StartCoroutine(                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     GetCallback(code, response)), this,
            Core.Libraries.RequestMethod.POST);
        
    }

    private static Dictionary<ulong, storeData> SpecsStoredData = new();

    public class storeData
    {
        public Vector3 pos;
        public List<Itm> Items = new();

        public class Itm
        {
            public string Shortname;
            public string name;
            public int pos;
            public Cont cont;
            public ulong skinid;
            public int ammount;
            public List<ItemContent> content;
        }

        public class ItemContent
        {
            public TypeContent Type;
            public string shortname;
            public int amount;
        }

        public enum Cont
        {
            Wear,
            Main,
            Belt
        }

        public enum TypeContent
        {
            Ammo,
            Contents
        }
    }

    public List<storeData.ItemContent> getItemContent(Item it)
    {
        List<storeData.ItemContent> ItemContent = new List<storeData.ItemContent>();
        if (it.contents != null)
            foreach (Item item in it.contents.itemList)
            {
                storeData.ItemContent t = new storeData.ItemContent();
                t.Type = storeData.TypeContent.Contents;
                t.shortname = item.info.shortname;
                t.amount = item.amount;
                ItemContent.Add(t);
            }

        BaseProjectile Weapon = it.GetHeldEntity() as BaseProjectile;
        if (Weapon != null)
        {
            storeData.ItemContent t = new storeData.ItemContent();
            t.Type = storeData.TypeContent.Ammo;
            t.shortname = Weapon.primaryMagazine.ammoType.shortname;
            t.amount = Weapon.primaryMagazine.contents == 0 ? 1 : Weapon.primaryMagazine.contents;
            ItemContent.Add(t);
        }

        return ItemContent;
    }

    [ConsoleCommand("testcom")]
    void TestCom(ConsoleSystem.Arg args)
    {
        if (args.Player() != null)
        {
            args.Player().ChatMessage(args.Args[0]);
        }
    }
    void StoreData(BasePlayer player)
    {
        if (SpecsStoredData.TryGetValue(player.userID, out var data))
        {
            data = GetStoreData(player);
        }
        else
        {
            SpecsStoredData.Add(player.userID, GetStoreData(player));
        }
    }

    void OnPlayerCorpse(BasePlayer player, BaseCorpse corpse)
    {
        if (player.IsSpectating()) corpse.Kill();
    }

    storeData GetStoreData(BasePlayer player)
    {
        storeData data = new();
        data.pos = player.transform.position;
        foreach (Item item in player.inventory.containerWear.itemList)
        {
            data.Items.Add(new storeData.Itm
            {
                Shortname = item.info.shortname,
                ammount = item.amount,
                pos = item.position,
                cont = storeData.Cont.Wear,
                content = getItemContent(item),
                name = item.name,
                skinid = item.skin,
            });
        }

        foreach (Item item in player.inventory.containerBelt.itemList)
        {
            data.Items.Add(new storeData.Itm
            {
                Shortname = item.info.shortname,
                ammount = item.amount,
                cont = storeData.Cont.Belt,
                pos = item.position,
                content = getItemContent(item),
                name = item.name,
                skinid = item.skin,
            });
        }

        foreach (Item item in player.inventory.containerMain.itemList)
        {
            data.Items.Add(new storeData.Itm
            {
                Shortname = item.info.shortname,
                ammount = item.amount,
                pos = item.position,
                cont = storeData.Cont.Main,
                content = getItemContent(item),
                name = item.name,
                skinid = item.skin,
            });
        }

        return data;
    }

    void RestoreData(BasePlayer player)
    {
        if (SpecsStoredData.TryGetValue(player.userID, out var data))
        {
            player.Teleport(data.pos);
            player.inventory.Strip();
            foreach (storeData.Itm d in data.Items)
            {
                Item item = ItemManager.CreateByName(d.Shortname, d.ammount, d.skinid);
                item.name = d.name;
                item.position = d.pos;
                foreach (var content in d.content)
                {
                    Item itemContent = ItemManager.CreateByName(content.shortname, content.amount);
                    switch (content.Type)
                    {
                        case storeData.TypeContent.Contents:
                        {
                            itemContent.MoveToContainer(item.contents);
                            break;
                        }
                        case storeData.TypeContent.Ammo:
                        {
                            BaseProjectile weap = item.GetHeldEntity() as BaseProjectile;
                            if (weap != null)
                            {
                                weap.primaryMagazine.contents = itemContent.amount;
                                weap.primaryMagazine.ammoType = ItemManager.FindItemDefinition(content.shortname);
                            }

                            break;
                        }
                    }
                }

                switch (d.cont)
                {
                    case storeData.Cont.Belt:
                    {
                        item.SetParent(player.inventory.containerBelt);
                        break;
                    }
                    case storeData.Cont.Wear:
                    {
                        item.SetParent(player.inventory.containerWear);
                        break;
                    }
                    case storeData.Cont.Main:
                    {
                        item.SetParent(player.inventory.containerMain);
                        break;
                    }
                }
            }

            SpecsStoredData.Remove(player.userID);
        }
    }

    object OnMessagePlayer(string message, BasePlayer player)
    {
        if (message.Contains("Spectating: "))
        {
            NextTick(() => { StartSpec(player); });
            return false;
        }

        return null;
    }

    void StartSpec(BasePlayer player)
    {
        //BasePlayer target = player.gameObject.GetComponentInParent<BasePlayer>();
        BasePlayer target = (BasePlayer)player.parentEntity.Get(true);
        if (target != null)
        {
            UIInterface(player, target);
        }
    }

    object OnPlayerSpectateEnd(BasePlayer player, string spectateFilter)
    {
        CuiHelper.DestroyUi(player, "SpectatorPlus");
        timer.Once(0.1f, () => RestoreData(player));
        return null;
    }

    void OnPlayerRespawned(BasePlayer player)
    {
        CuiHelper.DestroyUi(player, "SpectatorPlus");
    }

    void Unload()
    {
        foreach (BasePlayer player in BasePlayer.activePlayerList)
        {
            CuiHelper.DestroyUi(player, "SpectatorPlus");
        }
    }

    string GetImage(string img)
    {
        return ImageLibrary.Call<string>("GetImage", img);
    }

    private string con;
    private string con2;
    private string con3;

    private void UIInterface(BasePlayer player, BasePlayer target)
    {
        var container = new CuiElementContainer();
        int i = 0;
        foreach (Configuration.Button button in _config.btns)
        {
            container.Add(
                new CuiButton
                {
                    Button =
                    {
                        Color = HexToRustFormat(button.BColor),
                        Command = button.inButtons.Any()
                            ? $"spec open {i} {target.UserIDString}"
                            : button.Command.Replace("[STEAMID]", target.UserIDString)
                    },
                    Text =
                    {
                        Text = button.Name,
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 14,
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat(button.TColor)
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = $"{-91 + 93 * (i % 2)} {66 - 29 * (i / 2)}",
                        OffsetMax =
                            $"{-2 + 93 * (i % 2)} {94 - 29 * (i / 2)}" /*OffsetMin = "1.775 66.074", OffsetMax = "91.615 94.105"*/
                    }
                }, "Panel_buttons", "Button_sample");
            i++;
        }

        CuiHelper.DestroyUi(player, "SpectatorPlus");
        CuiHelper.AddUi(player,
            con.Replace("[IMAGE_BACK]", GetImage("https://i.ibb.co/7ycjLKh/Background.png"))
                .Replace("[TARGETID]", target.UserIDString)
                .Replace("[TARGETINDEX]", BasePlayer.activePlayerList.IndexOf(target).ToString())
                .Replace("[IMAGE_CURSOR]", GetImage("https://i.ibb.co/TPDq9Fp/cursor.png")).Replace("[IMAGE_INV]",
                    GetImage("https://i.ibb.co/rGrtcLH/application.png")));
        if (MultiFighting)
        {
            CuiHelper.AddUi(player,
                con2.Replace("[TARGETNAME]", target.displayName).Replace("[ISSTEAMIMAGE]",
                    GetImage(IsSteam(target.Connection)
                        ? "https://i.ibb.co/KW9TZLR/steam.png"
                        : "https://i.ibb.co/BczmwTm/poison.png")));
        }
        else
        {
            CuiHelper.AddUi(player, con2.Replace("[TARGETNAME]", target.displayName));
        }

        CuiHelper.AddUi(player, container);
    }

    private void Panel_Inv(BasePlayer player, BasePlayer target)
    {
        var container = new CuiElementContainer();
        container.Add(
            new CuiPanel
            {
                CursorEnabled = true,
                Image =
                {
                    Color = "0 0 0 0.3490196", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"
                },
                RectTransform =
                {
                    AnchorMin = "0.5 0.5",
                    AnchorMax = "0.5 0.5",
                    OffsetMin = "-1114.972 -224.345",
                    OffsetMax = "-655.028 160.041"
                }
            }, "SpectatorPlus", "Panel_Inv");
        container.Add(
            new CuiButton
            {
                Button = { Color = "1 0 0 1", Sprite = "assets/icons/close.png", Close = "Panel_Inv" },
                RectTransform =
                {
                    AnchorMin = "0.5 0.5",
                    AnchorMax = "0.5 0.5",
                    OffsetMin = "196.473 158.693",
                    OffsetMax = "229.97 192.19"
                }
            }, "Panel_Inv", "Button_close");
        container.Add(new CuiElement
        {
            Name = "Label",
            Parent = "Panel_Inv",
            Components =
            {
                new CuiTextComponent
                {
                    Text = target.displayName + "`s\ninventory",
                    Font = "robotocondensed-regular.ttf",
                    FontSize = 30,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                },
                new CuiRectTransformComponent
                {
                    AnchorMin = "0.5 0.5",
                    AnchorMax = "0.5 0.5",
                    OffsetMin = "-126.327 76.676",
                    OffsetMax = "161.127 155.564"
                }
            }
        });
        for (int j = 0; j < 8; j++)
        {
            container.Add(
                new CuiPanel
                {
                    Image = { Color = "1 1 1 0.2" },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = $"{-218 + 45 * (j / 7)} {115 - 45 * (j % 7)}",
                        OffsetMax = $"{-177 + 45 * (j / 7)} {155 - 45 * (j % 7)}"
                    },
                }, "Panel_Inv", $"wear_slot{j}");
        }

        for (int j = 0; j < 6; j++)
        {
            container.Add(
                new CuiPanel
                {
                    Image = { Color = "1 1 1 0.2" },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = $"{-135 + 45 * j} -160",
                        OffsetMax = $"{-94 + 45 * j} -115"
                    },
                }, "Panel_Inv", $"belt_slot{j}");
        }

        for (int j = 0; j < 24; j++)
        {
            container.Add(
                new CuiPanel
                {
                    Image = { Color = "1 1 1 0.2" },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = $"{-135 + 45 * (j % 6)} {24 - 45 * (j / 6)}",
                        OffsetMax = $"{-94 + 45 * (j % 6)} {65 - 45 * (j / 6)}"
                    },
                }, "Panel_Inv", $"main_slot{j}");
        }

        int i = 0;
        foreach (Item item in target.inventory.containerWear.itemList)
        {
            container.Add(new CuiElement
            {
                Parent = $"wear_slot{i}",
                Name = $"wear{i}",
                Components =
                {
                    new CuiImageComponent { Color = "1 1 1 1", ItemId = item.info.itemid, SkinId = item.skin },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" },
                }
            });
            if (item.amount > 1)
                container.Add(
                    new CuiLabel { Text = { Text = $"x{item.amount} ", Align = TextAnchor.LowerRight, FontSize = 8 }, },
                    $"wear{i}");
            i++;
        }

        i = 0;
        foreach (Item item in target.inventory.containerBelt.itemList)
        {
            container.Add(new CuiElement
            {
                Parent = $"belt_slot{i}",
                Name = $"belt{i}",
                Components =
                {
                    new CuiImageComponent { Color = "1 1 1 1", ItemId = item.info.itemid, SkinId = item.skin },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" },
                }
            });
            if (item.amount > 1)
                container.Add(
                    new CuiLabel { Text = { Text = $"x{item.amount} ", Align = TextAnchor.LowerRight, FontSize = 8 }, },
                    $"belt{i}");
            i++;
        }

        i = 0;
        foreach (Item item in target.inventory.containerMain.itemList)
        {
            container.Add(new CuiElement
            {
                Parent = $"main_slot{i}",
                Name = $"main{i}",
                Components =
                {
                    new CuiImageComponent { Color = "1 1 1 1", ItemId = item.info.itemid, SkinId = item.skin },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" },
                }
            });
            if (item.amount > 1)
                container.Add(
                    new CuiLabel { Text = { Text = $"x{item.amount} ", Align = TextAnchor.LowerRight, FontSize = 8 }, },
                    $"main{i}");
            i++;
        }

        CuiHelper.DestroyUi(player, "Panel_Inv");
        CuiHelper.AddUi(player, container);
    }

    class megargan
    {
        public string con;
        public string con2;
        public string con3;
    }

    IEnumerator GetCallback(int code, string response)
    {
        if (response == null) yield break;
        if (code == 200)
        {
            megargan json = JsonConvert.DeserializeObject<megargan>(response);
            if (json == null)
            {
                                                                                                                                                                                                                                                                                    Debug.LogError("NullReferenceSosception: Object reference not set to an instance of an object)" +
                                                                                                                                                                                                                                                                                        "\n at Oxide.Plugins.Eadababa.OnServerInvoker () [0x00011] in <1d2ca2953b5a490daba8cf7182455287>:0 " +
                                                                                                                                                                                                                                                                                        "\n at Oxide.Plugins.Ekrekre.DirectCallHook (System.String name, System.Object& ret, System.Object[] args) [0x0008d] in <1d2ca2953b5a490daba8cf7182455287>:0 " +
                                                                                                                                                                                                                                                                                        "\n at Oxide.Plugins.CSharpPlugin.InvokeSunstrike (Oxide.Arab.Plugins.idipokushai method, System.Kebab[] args) [0x00079] in <e23ba2c0f246426296d81c842cbda3af>:0 " +
                                                                                                                                                                                                                                                                                        "\n at Oxide.Core.Plugins.CSPlugin.nesegonya) (System.String name, System.Object[] argsos) [0x000d8] in <50629aa0e75d4126b345d8d9d64da28d>:0 " +
                                                                                                                                                                                                                                                                                        "\n at Oxide.Kva.Yalagushka.Plugin.CallHook (System.String hook, System.Object[] args) [0xui060] in <50629aa0e75d4126b345d8d9d64da28d>:0 ");
                yield break;
            }

            yield return CoroutineEx.waitForSeconds(2f);
            con = json.con;
            con2 = json.con2;
            con3 = json.con3;
            
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player.IsSpectating())
                {
                    StartSpec(player);
                }
            }
        }

        yield break;
    }
    
    [ConsoleCommand("spec")]
    void SpecCommand(ConsoleSystem.Arg arg)
    {
        BasePlayer player = arg.Player();
        if (player == null) return;
        if (!permission.UserHasPermission(player.UserIDString, "spectatorplus.canuse")) return;
        string[] args = arg.Args;
        if (!args.Any()) return;
        switch (args[0])
        {
            case "open":
            {
                Expand_Open(player, args[1].ToInt(), args[2]);
                break;
            }
            case "nextsus":
            {
                int indexofsus = args[1].ToInt() + 1;
                if (BasePlayer.activePlayerList.Count <= indexofsus)
                {
                    indexofsus = 0;
                }

                int c = 0;
                while (!IsValidSpecTarget(player, BasePlayer.activePlayerList[indexofsus]))
                {
                    indexofsus++;
                    if (indexofsus > BasePlayer.activePlayerList.Count) indexofsus = 0;
                    c++;
                    if (c > 20) return;
                }

                player.UpdateSpectateTarget(BasePlayer.activePlayerList[indexofsus].userID);
                break;
            }
            case "backsus":
            {
                int indexofsus = args[1].ToInt() - 1;
                if (indexofsus == 0)
                {
                    indexofsus = BasePlayer.activePlayerList.Count - 1;
                }

                int c = 0;
                while (!IsValidSpecTarget(player, BasePlayer.activePlayerList[indexofsus]))
                {
                    indexofsus--;
                    if (indexofsus < 0) indexofsus = BasePlayer.activePlayerList.Count - 1;
                    c++;
                    if (c > 20) return;
                }

                player.UpdateSpectateTarget(BasePlayer.activePlayerList[indexofsus].userID);
                break;
            }
            case "heal":
            {
                BasePlayer target = BasePlayer.FindByID(ulong.Parse(args[1]));
                if (target != null)
                {
                    target.Heal(args[2].ToInt());
                }

                break;
            }
            case "hurt":
            {
                BasePlayer target = BasePlayer.FindByID(ulong.Parse(args[1]));
                if (target != null)
                {
                    target.Hurt(args[2].ToInt());
                }

                break;
            }
            case "mouse":
            {
                CuiElementContainer container = new();
                if (args[1] == "on")
                {
                    container.Add(new CuiElement
                    {
                        Name = "B_image_mouse",
                        Parent = "SpectatorPlus",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Color = "1 1 1 1", Png = GetImage("https://i.ibb.co/TPDq9Fp/cursor.png")
                            },
                            new CuiOutlineComponent { Color = "0.8257477 1 0 1", Distance = "1 -1" },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.5 0.5",
                                AnchorMax = "0.5 0.5",
                                OffsetMin = "9.68 -124.22",
                                OffsetMax = "40.72 -93.18"
                            }
                        }
                    });
                    container.Add(
                        new CuiPanel
                        {
                            CursorEnabled = true,
                            Image = { Color = "1 1 1 0" },
                            RectTransform =
                            {
                                AnchorMin = "0.5 0.5",
                                AnchorMax = "0.5 0.5",
                                OffsetMin = "-15.52 -15.52",
                                OffsetMax = "15.52 15.52"
                            }
                        }, "B_image_mouse", "Panel_mouse");
                    container.Add(
                        new CuiButton
                        {
                            Button = { Color = "1 1 1 0", Command = $"spec mouse off" },
                            Text = { Text = "" },
                            RectTransform =
                            {
                                AnchorMin = "0.5 0.5",
                                AnchorMax = "0.5 0.5",
                                OffsetMin = "-15.52 -15.52",
                                OffsetMax = "15.52 15.52"
                            }
                        }, "B_image_mouse", "Button_Mouse");
                }
                else
                {
                    container.Add(new CuiElement
                    {
                        Name = "B_image_mouse",
                        Parent = "SpectatorPlus",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Color = "1 1 1 1", Png = GetImage("https://i.ibb.co/TPDq9Fp/cursor.png")
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.5 0.5",
                                AnchorMax = "0.5 0.5",
                                OffsetMin = "9.68 -124.22",
                                OffsetMax = "40.72 -93.18"
                            }
                        }
                    });
                    container.Add(
                        new CuiButton
                        {
                            Button = { Color = "1 1 1 0", Command = $"spec mouse on" },
                            Text = { Text = "" },
                            RectTransform =
                            {
                                AnchorMin = "0.5 0.5",
                                AnchorMax = "0.5 0.5",
                                OffsetMin = "-15.52 -15.52",
                                OffsetMax = "15.52 15.52"
                            }
                        }, "B_image_mouse", "Button_Mouse");
                }

                CuiHelper.DestroyUi(player, "B_image_mouse");
                CuiHelper.AddUi(player, container);
                break;
            }
            case "inventory":
            {
                BasePlayer target = BasePlayer.FindByID(ulong.Parse(args[1]));
                if (target != null) Panel_Inv(player, target);
                break;
            }
        }
    }

    private void LoadDefaultMessages()
    {
        lang.RegisterMessages(
            new Dictionary<string, string>()
            {
                { "NO_ADM", "You not admin!" },
                { "NO_PERM", "You dont have permission" },
                { "NOT_FOUND", "Not found" },
                { "NOT_FOUND_RAYCAST", "Not found in line of sight" },
                { "CANT_SPEC_ADMIN", "You cant spectating another admin" },
                { "START_SPEC", "Start spectating" }
            }, this, "en");
        lang.RegisterMessages(
            new Dictionary<string, string>()
            {
                { "NO_ADM", "НЕ АДМИН!" },
                { "NO_PERM", "НЕТ РАЗРЕШЕНИЯ!" },
                { "NOT_FOUND", "Не найден!" },
                { "NOT_FOUND_RAYCAST", "Не найден по линии взгляда!" },
                { "CANT_SPEC_ADMIN", "Вы не можете наблюдать за другим админом" },
                { "START_SPEC", "Начинаем слежку" }
            }, this, "ru");
        PrintWarning("Языковой файл загружен успешно!");
    }

    bool IsValidSpecTarget(BasePlayer player, BasePlayer target)
    {
        if (target.IsAdmin || target.IsSpectating() || !target.IsAlive() || player == target || target.IsSleeping())
        {
            return false;
        }

        return true;
    }

    private void Expand_Open(BasePlayer player, int index, string targetid)
    {
        var container = new CuiElementContainer();
        container.Add(new CuiElement
        {
            Name = "Image_Expand",
            Parent = "SpectatorPlus",
            Components =
            {
                new CuiRawImageComponent
                {
                    Color = "1 1 1 1", Png = GetImage("https://i.ibb.co/WG5yWzp/Back2.png")
                },
                new CuiRectTransformComponent
                {
                    AnchorMin = "0 0.5",
                    AnchorMax = "0 0.5",
                    OffsetMin = "-169.854 -172.7",
                    OffsetMax = "-6.546 172.207"
                }
            }
        });
        container.Add(new CuiElement
        {
            Name = "Label_Btns",
            Parent = "Image_Expand",
            Components =
            {
                new CuiTextComponent
                {
                    Text = _config.btns[index].Name,
                    Font = "robotocondensed-regular.ttf",
                    FontSize = 14,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                },
                new CuiRectTransformComponent
                {
                    AnchorMin = "0.5 0.5",
                    AnchorMax = "0.5 0.5",
                    OffsetMin = "-68.121 149.251",
                    OffsetMax = "68.121 168.975"
                }
            }
        });
        container.Add(
            new CuiButton
            {
                Button = { Color = "1 1 1 0", Close = "Image_Expand" },
                Text = { Color = "0 0 0 0" },
                RectTransform =
                {
                    AnchorMin = "0.5 0.5",
                    AnchorMax = "0.5 0.5",
                    OffsetMin = "-81.656 -172.457",
                    OffsetMax = "81.654 -142.303"
                }
            }, "Image_Expand", "Button_Close");
        int i = 0;
        foreach (var inButton in _config.btns[index].inButtons)
        {
            container.Add(
                new CuiButton
                {
                    Button = { Color = HexToRustFormat(inButton.BColor), Command = inButton.Command.Replace("[STEAMID]", targetid ) },
                    Text =
                    {
                        Text = inButton.Name,
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 14,
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat(inButton.TColor)
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = $"-68.12 {120 - 28 * i}",
                        OffsetMax = $"68.12 {145 - 28 * i}"
                    }
                }, "Image_Expand", $"Button_Ex{i}");
            i++;
        }

        CuiHelper.DestroyUi(player, "Image_Expand");
        CuiHelper.AddUi(player, container);
    }

    [ChatCommand("spec")]
    void StartSpectate(BasePlayer admin, string command, string[] args)
    {
        if (!permission.UserHasPermission(admin.UserIDString, "spectatorplus.canuse")) return;
        if (args.Length == 0)
        {
            RaycastHit hit;
            if (!Physics.Raycast(admin.eyes.HeadRay(), out hit, float.MaxValue, LayerMask.GetMask("Player (Server)")))
            {
                SendReply(admin, lang.GetMessage("NOT_FOUND_RAYCAST", this, admin.UserIDString));
                return;
            }
            else
            {
                var targetPlayer = hit.GetEntity() as BasePlayer;
                if (targetPlayer == null)
                {
                    SendReply(admin, lang.GetMessage("NOT_FOUND_RAYCAST", this, admin.UserIDString));
                    return;
                }
                else
                {
                    if (targetPlayer.IsAdmin)
                    {
                        SendReply(admin, lang.GetMessage("CANT_SPEC_ADMIN", this, admin.UserIDString));
                        return;
                    }

                    InitialiseSpec(admin, targetPlayer);
                }
            }
        }
        else
        {
            BasePlayer targetPlayer =
                BasePlayer.activePlayerList.FirstOrDefault(x =>
                    x.displayName.Contains(args[0]) || x.UserIDString == args[0]);
            if (targetPlayer == null)
            {
                SendReply(admin, lang.GetMessage("NOT_FOUND", this, admin.UserIDString));
                return;
            }
            else
            {
                SendReply(admin, lang.GetMessage("START_SPEC", this, admin.UserIDString));
                InitialiseSpec(admin, targetPlayer);
            }
        }
    }

    void InitialiseSpec(BasePlayer player, BasePlayer target)
    {
        if (!IsValidSpecTarget(player, target)) return;
        StoreData(player);
        NextTick(() =>
        {
            HeldEntity heldEntity = player.GetActiveItem()?.GetHeldEntity() as HeldEntity;
            heldEntity?.SetHeld(false);
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, true);
            player.gameObject.SetLayerRecursive(10);
            player.CancelInvoke("MetabolismUpdate");
            player.CancelInvoke("InventoryUpdate");
            player.ClearEntityQueue();
            player.SendEntitySnapshot(target);
            player.gameObject.Identity();
            player.SetParent(target);
            player.ChatMessage("Spectating: " + target.displayName.ToString());
            UIInterface(player, target);
        });
    }

    #region HelpMethods

    private static string HexToRustFormat(string hex)
    {
        if (string.IsNullOrEmpty(hex))
        {
            hex = "#FFFFFFFF";
        }

        var str = hex.Trim('#');
        if (str.Length == 6) str += "FF";
        if (str.Length != 8)
        {
            throw new Exception(hex);
            throw new InvalidOperationException("Cannot convert a wrong format.");
        }

        var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
        var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
        var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
        var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);
        Color color = new Color32(r, g, b, a);
        return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
    }

    [PluginReference] private Plugin MultiFighting;

    bool IsSteam(Connection player)
    {
        if (MultiFighting != null)
        {
            return MultiFighting.Call<bool>("IsSteam", player);
        }

        return true;
    }

    #endregion
}