using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("TreeHit", "begemonzazbat", "1.0.0")]
    [Description("Позволяет игрокам с привилегией всегда получать бонус-добычу при ударе по дереву или камню")]
    internal sealed class TreeHit : RustPlugin
    {
        #region Конфигурация

        private Configuration config;

        private sealed class Configuration
        {
            [JsonProperty("Привилегия для бонус-добычи")]
            public string Permission = "treehit.bonus";

            [JsonProperty("Включить бонус-добычу для деревьев")]
            public bool EnableTreeBonus = true;

            [JsonProperty("Включить бонус-добычу для камней")]
            public bool EnableOreBonus = true;

            [JsonProperty("Сообщение при получении привилегии")]
            public string PermissionGrantedMessage = "Вы получили привилегию на бонус-добычу при ударе по ресурсам!";
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    throw new InvalidOperationException("Configuration is null");
                }

                SaveConfig();
            }
            catch
            {
                PrintError("Error loading configuration, creating new configuration");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion Конфигурация

        #region Локализация

        private readonly Dictionary<string, string> messages = new Dictionary<string, string>
        {
            ["NoPermission"] = "У вас нет разрешения на использование этой команды!",
            ["BonusEnabled"] = "Бонус-добыча активирована!",
            ["BonusDisabled"] = "Бонус-добыча деактивирована!"
        };

        private string Lang(string key, string id = null, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, lang.GetMessage(key, this, id), args);
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(messages, this);
        }

        #endregion Локализация

        #region Oxide Hooks

        private void Init()
        {
            permission.RegisterPermission(config.Permission, this);
            permission.RegisterPermission("treehit.admin", this);
            Puts("TreeHit loaded successfully!");
        }

        private void OnPlayerAttack(BasePlayer player, HitInfo info)
        {
            if (player == null || info == null || info.HitEntity == null)
            {
                return;
            }

            if (!permission.UserHasPermission(player.UserIDString, config.Permission))
            {
                return;
            }

            // Проверка на дерево
            if (config.EnableTreeBonus && info.HitEntity is TreeEntity)
            {
                TreeEntity tree = info.HitEntity as TreeEntity;
                if (tree == null)
                {
                    return;
                }

                // Устанавливаем точку удара в "крестик" (hotspot)
                if (tree.hasBonusGame)
                {
                    // Используем метод DidHitMarker для проверки попадания по маркеру
                    // и устанавливаем точку удара в верхнюю часть дерева, где обычно находится маркер
                    Vector3 treeTop = tree.transform.position + new Vector3(0f, tree.transform.localScale.y * 2f, 0f);
                    info.HitPositionWorld = treeTop;
                    info.HitNormalWorld = (treeTop - tree.transform.position).normalized;
                    info.HitMaterial = StringPool.Get("Wood");
                }
            }

            // Проверка на руду
            if (config.EnableOreBonus && info.HitEntity is OreResourceEntity)
            {
                OreResourceEntity ore = info.HitEntity as OreResourceEntity;
                if (ore == null)
                {
                    return;
                }

                // Устанавливаем точку удара в "блестящую" часть (hotspot)
                if (ore._hotSpot != null)
                {
                    Vector3 hotSpotPosition = ore._hotSpot.transform.position;
                    info.HitPositionWorld = hotSpotPosition;
                    info.HitNormalWorld = (hotSpotPosition - ore.transform.position).normalized;
                }
            }
        }

        #endregion Oxide Hooks

        #region Команды

        [ChatCommand("treehit")]
        private void TreeHitCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "treehit.admin"))
            {
                player.ChatMessage(Lang("NoPermission", player.UserIDString));
                return;
            }

            if (args.Length == 0)
            {
                player.ChatMessage("Использование: /treehit grant <steamid/имя> - выдать привилегию");
                player.ChatMessage("Использование: /treehit revoke <steamid/имя> - забрать привилегию");
                return;
            }

            switch (args[0].ToLower(CultureInfo.InvariantCulture))
            {
                case "grant":
                    if (args.Length < 2)
                    {
                        player.ChatMessage("Использование: /treehit grant <steamid/имя>");
                        return;
                    }

                    BasePlayer target = FindPlayer(args[1]);
                    if (target == null)
                    {
                        player.ChatMessage($"Игрок {args[1]} не найден!");
                        return;
                    }

                    // Используем oxide.grant для выдачи привилегии
                    Server.Command($"oxide.grant user {target.UserIDString} {config.Permission}");
                    player.ChatMessage($"Привилегия выдана игроку {target.displayName}");
                    target.ChatMessage(config.PermissionGrantedMessage);
                    break;

                case "revoke":
                    if (args.Length < 2)
                    {
                        player.ChatMessage("Использование: /treehit revoke <steamid/имя>");
                        return;
                    }

                    BasePlayer targetRevoke = FindPlayer(args[1]);
                    if (targetRevoke == null)
                    {
                        player.ChatMessage($"Игрок {args[1]} не найден!");
                        return;
                    }

                    // Используем oxide.revoke для отзыва привилегии
                    Server.Command($"oxide.revoke user {targetRevoke.UserIDString} {config.Permission}");
                    player.ChatMessage($"Привилегия отозвана у игрока {targetRevoke.displayName}");
                    break;

                default:
                    player.ChatMessage("Использование: /treehit grant <steamid/имя> - выдать привилегию");
                    player.ChatMessage("Использование: /treehit revoke <steamid/имя> - забрать привилегию");
                    break;
            }
        }

        #endregion Команды

        #region Вспомогательные методы

        private BasePlayer FindPlayer(string nameOrId)
        {
            // Поиск по SteamID
            if (nameOrId.StartsWith("7656119", StringComparison.Ordinal) && nameOrId.Length == 17)
            {
                ulong steamId;
                if (ulong.TryParse(nameOrId, out steamId))
                {
                    BasePlayer player = BasePlayer.FindByID(steamId);
                    if (player != null)
                    {
                        return player;
                    }
                }
            }

            // Поиск по имени
            BasePlayer foundPlayer = null;
            foreach (BasePlayer activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.displayName.IndexOf(nameOrId, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (foundPlayer != null)
                    {
                        // Найдено несколько игроков
                        return null;
                    }
                    foundPlayer = activePlayer;
                }
            }
            return foundPlayer;
        }

        #endregion Вспомогательные методы
    }
}
