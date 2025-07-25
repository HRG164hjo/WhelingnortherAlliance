using RimWorld;
using Verse;
using Verse.AI;

namespace WNA.Damager
{
    public class CastTargetEffect : Verb_CastBase
    {
        public override void WarmupComplete()
        {
            base.WarmupComplete();
            if (currentTarget.Thing is Pawn pawn && !pawn.Downed && !pawn.IsColonyMech && CasterIsPawn && CasterPawn.skills != null)
            {
                float num = verbProps.AdjustedFullCycleTime(this, CasterPawn);
                CasterPawn.skills.Learn(SkillDefOf.Shooting, 150f * num);
            }
        }
        protected override bool TryCastShot()
        {
            Pawn casterPawn = CasterPawn;
            Thing thing = currentTarget.Thing;
            if (casterPawn == null || thing == null)
            {
                return false;
            }
            foreach (CompTargetEffect comp in base.EquipmentSource.GetComps<CompTargetEffect>())
            {
                comp.DoEffectOn(casterPawn, thing);
            }
            return true;
        }
    }
}
