using RimWorld;
using Verse;
using WNA.WNADefOf;
using WNA.WNAUtility;

namespace WNA.TargetEffect
{
    public class Ascension : CompTargetEffect
    {
        public override void DoEffectOn(Pawn user, Thing target)
        {
            if (target is Pawn pawn && (!pawn.Dead || !pawn.Destroyed))
            {
                General.TotalRemoving(pawn, true);
            }
            else if (!target.Destroyed)
            {
                target.Destroy(DestroyMode.KillFinalize);
            }
        }
        public override bool CanApplyOn(Thing target)
        {
            if (target is Pawn pawn)
            {
                if (pawn.def == WNAMainDefOf.WNA_WNThan || pawn.def == WNAMainDefOf.WNA_Human)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
