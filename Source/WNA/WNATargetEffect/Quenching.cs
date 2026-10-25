using RimWorld;
using Verse;
using WNA.WNAUtility;

namespace WNA.WNATargetEffect
{
    internal class Quenching : CompTargetEffect
    {
        public override void DoEffectOn(Pawn user, Thing target)
        {
            bool hunting = (user.CurJobDef == JobDefOf.Hunt);
            if (target is Pawn pawn &&
                (!pawn.Dead || !pawn.Destroyed))
            {
                if (!hunting)
                    General.TotalRemoving(pawn, true);
                else pawn.Kill(null, null);
            }
            else if (!target.Destroyed)
                target.Destroy(DestroyMode.KillFinalize);
        }
        public override bool CanApplyOn(Thing target)
        {
            if (target is Pawn pawn)
            {
                if (General.IsWNARace(pawn))
                    return false;
            }
            return true;
        }
    }
}
