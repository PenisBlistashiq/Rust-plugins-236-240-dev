using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace Oxide.Plugins
{
    [Info("VehicleNoDamage", "Frizen", "1.0.0")]

    public class VehicleNoDamage : RustPlugin
    {

        private object OnEntityTakeDamage(BaseVehicle entity, HitInfo hitInfo)
        {
            if (entity is BaseVehicle)
            {
                return false;
            }
            return null; 
        }

        bool CanMount = false;

        [ChatCommand("stop")]
        void CmdMountNo(BasePlayer player)
        {
            if (!player.IsAdmin) return;
            CanMount = false;
        }
        [ChatCommand("start")]
        void CmdMountYes(BasePlayer player)
        {
            if (!player.IsAdmin) return;
            CanMount = true;
        }


       
        object CanMountEntity(BasePlayer player, BaseMountable entity)
        {
            if (CanMount == false) return false;
            return null;
        }


       
        object CanMoveItem(Item item, PlayerInventory playerLoot, uint targetContainer, int targetSlot, int amount)
        {
            if (item.info.shortname == "crankshaft3" || item.info.shortname == "carburetor3" || item.info.shortname == "piston3" || item.info.shortname == "sparkplug3" || item.info.shortname == "valve3" || item.info.shortname == "lowgradefuel") return false;
            return null;
        }




        private object OnEntityTakeDamage(BaseVehicleModule entity, HitInfo hitInfo)
        {
            if (entity is BaseVehicleModule)
            {
                return false;
            }
            return null;
        }


    }
}
