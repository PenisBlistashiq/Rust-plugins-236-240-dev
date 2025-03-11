using UnityEngine;
using System.Collections.Generic;

namespace Oxide.Plugins 
{
    [Info("Simple Scan", "walkinrey", "1.0.0")]
    class SimpleScan : RustPlugin 
    {
        [ChatCommand("simplescan")] 
        void ScanCommand(BasePlayer player) 
        {
            if(player.IsAdmin || player.IPlayer.HasPermission("simplescan.use")) 
            {
                RaycastHit hitInfo;
                if (!Physics.Raycast(player.eyes.position, player.eyes.HeadForward(), out hitInfo, 5f)) return;
                var ent = hitInfo.GetEntity();
                if (ent == null) return;
                if(ent is Door) {
                    Door door = (Door)ent;
                    SendReply(player, $"<size=18><color=yellow>Объект: дверь ({ent._name})</color></size>\nВладелец: {GetOwner(ent)}\nВладелец онлайн: {isOnline(GetOwner(ent))}");
                    var slot = ((Door) ent).GetSlot(BaseEntity.Slot.Lock);
                    if(slot == null) return;
                    SendReply(player, $"<size=18><color=yellow>Слот: кодовый замок</color></size>\nВладелец: {GetOwner(ent)}\nВладелец онлайн: {isOnline(GetOwner(ent))}\nЗамок: {IsCodeLockLocked(((CodeLock)slot))}\nКод: {((CodeLock)slot).code}\nГостевой код: {((CodeLock)slot).guestCode}");
                    return;
                }
                if(ent is KeyLock) {
                    KeyLock keyLock = (KeyLock)ent;
                    SendReply(player, $"<size=18><color=yellow>Объект: кодовый замок</color></size>\nВладелец: {GetOwner(ent)}\nВладелец онлайн: {isOnline(GetOwner(ent))}\nЗамок: {IsKeyLockLocked(keyLock)}\nКод: {keyLock.keyCode}");
                    return;
                }
                if(ent is BuildingPrivlidge) {
                    SendReply(player, $"<size=18><color=yellow>Объект: шкаф</color></size>\nВладелец: {GetOwner(ent)}\nВладелец онлайн: {isOnline(GetOwner(ent))}\nЗащищен на: {((BuildingPrivlidge)ent).GetProtectedMinutes()} минут\n");
                    string message = "<size=18><color=yellow>Авторизованные игроки:</color></size>";
                    foreach(var playerClass in ((BuildingPrivlidge)ent).authorizedPlayers) 
                    {
                        message = message + $"\nИмя: {playerClass.username}\nSteam ID: {playerClass.userid}\nОнлайн: {isOnline(playerClass.userid)}";
                    }
                    SendReply(player, message);
                    return;
                }
                SendReply(player, $"<size=18><color=yellow>Объект: {ent._name}</color></size>\nВладелец: {GetOwner(ent)}\nВладелец онлайн: {isOnline(GetOwner(ent))}");
            }
        }
        void Init() => permission.RegisterPermission("simplescan.use", this);
        string IsKeyLockLocked(KeyLock locker) 
        {
            if(locker.HasFlag(BaseEntity.Flags.Locked)) return "закрыт";
            else return "открыт";
        }
        string IsCodeLockLocked(CodeLock codeLock) 
        {
            if(codeLock.IsLocked()) return "закрыт";
            else return "открыт";
        }
        string isOnline(ulong id) 
        {
            BasePlayer playerOnline = null;
            foreach(var playerFind in BasePlayer.activePlayerList) 
            {
                if(playerFind.userID == id) 
                {
                    playerOnline = playerFind;
                    break;
                }
            }
            if(playerOnline == null) return "нет";
            else return $"да\nИмя владельца: {playerOnline.displayName}";
        }
        ulong GetOwner(BaseEntity entity) 
        {
            if(entity.OwnerID == 0) return 0;
            else return entity.OwnerID;
        }
    }
}