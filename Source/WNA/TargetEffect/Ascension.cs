using RimWorld;
using Verse;
using WNA.WNADefOf;

namespace WNA.TargetEffect
{
    public class Ascension : CompTargetEffect
    {
        public override void DoEffectOn(Pawn user, Thing target)
        {
            if (target is Pawn pawn && !pawn.Dead)
            {
                pawn.Kill(new DamageInfo(WNAMainDefOf.WNA_CastRange, 1000000f, 0f, -1f));
                if (!pawn.Dead || !pawn.Destroyed)
                {
                    Hediff hediff = HediffMaker.MakeHediff(WNAMainDefOf.WNA_Corrosion, pawn);
                    pawn.health.AddHediff(hediff);
                }
                if (!pawn.Dead || !pawn.Destroyed)
                {
                    pawn.Destroy(DestroyMode.KillFinalize);
                }
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
