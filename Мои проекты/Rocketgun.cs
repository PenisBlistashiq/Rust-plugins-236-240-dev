using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("RocketGun", "begemonzazbat", "1.0.0")]
    [Description("Стрельба ракетами")]
    public class RocketGun : RustPlugin
    {
        private readonly HashSet<ulong> rocketGunPlayers = new HashSet<ulong>();

        private void Init()
        {
            permission.RegisterPermission("rocketgun.use", this);
        }

        private BasePlayer FindPlayer(string nameOrId)
        {
            ulong steamId;
            if (ulong.TryParse(nameOrId, out steamId))
            {
                return BasePlayer.FindByID(steamId);
            }

            // Если не SteamID, ищем по нику
            return BasePlayer.Find(nameOrId);
        }

        [ChatCommand("rocketgun")]
        private void CmdRocketGun(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "rocketgun.use"))
            {
                player.ChatMessage("У вас нет прав на использование этой команды!");
                return;
            }

            if (args.Length != 1)
            {
                player.ChatMessage("Использование: /rocketgun <ник/steamid>");
                return;
            }

            BasePlayer targetPlayer = FindPlayer(args[0]);
            if (targetPlayer == null)
            {
                player.ChatMessage("Игрок не найден!");
                return;
            }

            bool wasRemoved = rocketGunPlayers.Remove(targetPlayer.userID);
            if (wasRemoved)
            {
                targetPlayer.ChatMessage("Режим RocketGun выключен!");
                player.ChatMessage(
                    $"Режим RocketGun выключен для игрока {targetPlayer.displayName} [{targetPlayer.userID}]!"
                );
            }
            else
            {
                rocketGunPlayers.Add(targetPlayer.userID);
                targetPlayer.ChatMessage(
                    "Режим RocketGun включен! Теперь ваше оружие стреляет ракетами!"
                );
                player.ChatMessage(
                    $"Режим RocketGun включен для игрока {targetPlayer.displayName} [{targetPlayer.userID}]!"
                );
            }
        }

        private void OnPlayerAttack(BasePlayer attacker, HitInfo info)
        {
            if (attacker == null || info == null || info.Weapon == null)
            {
                return;
            }

            if (rocketGunPlayers.Contains(attacker.userID))
            {
                Vector3 position = info.HitPositionWorld;
                if (position == default(Vector3))
                {
                    position =
                        info.PointStart + ((info.PointEnd - info.PointStart).normalized * 100f);
                }

                BaseEntity rocket = GameManager.server.CreateEntity(
                    "assets/prefabs/ammo/rocket/rocket_basic.prefab",
                    position
                );

                if (rocket == null)
                {
                    return;
                }

                rocket.SetVelocity(info.ProjectileVelocity);
                rocket.Spawn();
            }
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            if (player == null)
            {
                return;
            }

            rocketGunPlayers.Remove(player.userID);
        }
    }
}
