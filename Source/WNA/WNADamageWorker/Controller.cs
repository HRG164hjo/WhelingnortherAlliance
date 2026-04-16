using Verse;
using WNA.WNAUtility;

namespace WNA.WNADamageWorker
{
    public class Controller : DamageWorker_AddGlobal
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing thing)
        {
            if (dinfo.Instigator != null)
                MindControlUtility.TryControl(dinfo.Instigator, thing);
            return new DamageResult();
        }
    }
    public class ControllerPermanent : DamageWorker_AddGlobal
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing thing)
        {
            if (dinfo.Instigator != null)
                MindControlUtility.TryControl(dinfo.Instigator, thing, true);
            return new DamageResult();
        }
    }
}
