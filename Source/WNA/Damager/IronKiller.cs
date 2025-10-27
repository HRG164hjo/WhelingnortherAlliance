using Verse;
using WNA.WNAUtility;

namespace WNA.Damager
{
    public class IronKiller : DamageWorker_AddInjury
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing thing)
        {
            var res = base.Apply(dinfo, thing);
            if (thing != null && thing.MapHeld != null)
            {
                IntVec3 center = thing.PositionHeld;
                float radius = 0.9f;
                IronCurtainUtility.IronKill(thing.MapHeld, center, radius);
            }
            return res;
        }
    }
}
