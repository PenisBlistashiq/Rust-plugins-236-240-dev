using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Obj = UnityEngine.Object;


namespace Oxide.Plugins
{
    [Info("Clean", "Frizen", "1.0.0")]

    public class Clean : CovalencePlugin
    {

   
        void OnServerInitialized()
        {
			DoClean();
            InvokeHandler.Instance.InvokeRepeating(DoClean, 60f, 60f);
        }

   
        void Unload()
        {
            InvokeHandler.Instance.CancelInvoke(DoClean);
        }


        void DoClean()
        { 
                foreach (PlayerCorpse corpse_a in Obj.FindObjectsOfType<PlayerCorpse>())
                    corpse_a?.Kill();
                foreach (LootableCorpse corpse_b in Obj.FindObjectsOfType<LootableCorpse>())
                    corpse_b?.Kill();
                foreach (WorldItem witems in Obj.FindObjectsOfType<WorldItem>())
                    witems?.Kill();
                foreach (ModularCar car in Obj.FindObjectsOfType<ModularCar>())
                car?.Kill();
                GC.Collect();
       
        }
    }
}
