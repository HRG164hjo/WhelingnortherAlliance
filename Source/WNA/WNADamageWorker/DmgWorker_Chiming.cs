using RimWorld;
using Verse;
using WNA.WNADefOf;
using WNA.WNAUtility;

namespace WNA.WNADamageWorker
{
    internal class DmgWorker_Chiming : DamageWorker_AddInjury
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing thing)
        {
            DamageResult result = base.Apply(dinfo, thing);
            DamageInfo cul = new DamageInfo(WNAMainDefOf.WNA_Culling, float.MaxValue);
            if (thing is Pawn pawn)
            {
                if (!General.IsWNARace(pawn) && pawn.GetStatValue(StatDefOf.IncomingDamageFactor) <=0)
                    Apply(cul, pawn);
                else
                    return result;
            }
            else if (!thing.DestroyedOrNull())
            {
                if (thing is not Pawn && thing.def.useHitPoints == false)
                    General.DevDestroy(thing, DestroyMode.KillFinalize);
                else
                    return result;
            }
            return thing.DestroyedOrNull() ? new DamageResult() : result;
        }
    }
}
