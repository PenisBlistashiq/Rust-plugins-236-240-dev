using UnityEngine;
using System.Collections.Generic;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Turret Controller", "begemonzazbat", "1.0.0")]
    public class Turret : RustPlugin
    {
        private const string PERMISSION_USE = "turret.use";
        private StoredData storedData;

        private sealed class StoredData
        {
            public HashSet<string> ActiveTurrets = new HashSet<string>();
        }

        private void Init()
        {
            permission.RegisterPermission(PERMISSION_USE, this);
            LoadData();
        }

        private void OnServerInitialized()
        {
            foreach (string turretId in storedData.ActiveTurrets)
            {
                AutoTurret turret = BaseNetworkable.serverEntities.Find(uint.Parse(turretId)) as AutoTurret;
                if (turret?.IsOnline() == false)
                {
                    turret.InitiateStartup();
                }
            }
        }

        private void LoadData()
        {
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name) ?? new StoredData();
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name, storedData);
        }

        private void OnServerSave()
        {
            SaveData();
        }

        [ChatCommand("turret")]
        private void TurretCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERMISSION_USE))
            {
                SendReply(player, "У вас нет прав на использование этой команды");
                return;
            }

            RaycastHit hit;
            if (!Physics.Raycast(player.eyes.HeadRay(), out hit, 3f))
            {
                SendReply(player, "Вы должны смотреть на турель");
                return;
            }

            AutoTurret turret = hit.GetEntity() as AutoTurret;
            if (turret == null)
            {
                SendReply(player, "Вы должны смотреть на турель");
                return;
            }

            if (!turret.IsAuthed(player))
            {
                SendReply(player, "Вы должны быть авторизованы на этой турели");
                return;
            }

            string turretId = turret.net.ID.ToString();

            if (turret.IsOnline())
            {
                turret.InitiateShutdown();
                storedData.ActiveTurrets.Remove(turretId);
                SendReply(player, "Турель выключена");
            }
            else
            {
                turret.InitiateStartup();
                storedData.ActiveTurrets.Add(turretId);
                SendReply(player, "Турель включена");
            }

            SaveData();
        }

        private void Unload()
        {
            SaveData();
        }
    }
}
