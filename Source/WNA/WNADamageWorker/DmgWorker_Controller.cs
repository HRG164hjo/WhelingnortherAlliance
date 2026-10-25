using Verse;
using WNA.WNAModExtension;
using WNA.WNAUtility;

namespace WNA.WNADamageWorker
{
    internal class DmgWorker_Controller : DamageWorker_AddGlobal
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing thing)
        {
            if (dinfo.Instigator != null)
                WNAMindControlUtility.TryControl(dinfo.Instigator, thing, dinfo.Def, MindControlConfig.Get(dinfo.Def));
            return new DamageResult();
        }
    }
    public class DmgWorker_Controller_Permanent : DamageWorker_AddGlobal
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing thing)
        {
            if (dinfo.Instigator != null)
                WNAMindControlUtility.TryControl(dinfo.Instigator, thing, dinfo.Def, MindControlConfig.Get(dinfo.Def), permanentOverride: true);
            return new DamageResult();
        }
    }
}
