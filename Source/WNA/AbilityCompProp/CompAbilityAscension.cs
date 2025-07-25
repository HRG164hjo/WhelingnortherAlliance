using RimWorld;
using Verse;
using WNA.WNADefOf;

namespace WNA.AbilityCompProp
{
    public class CompAbilityAscension : CompProperties_AbilityEffect
    {
        public CompAbilityAscension()
        {
            compClass = typeof(AbilityAscension);
        }
    }
    public class AbilityAscension : CompAbilityEffect
    {

        public new CompAbilityAscension Props => (CompAbilityAscension)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn pawn = target.Pawn;
            base.Apply(target, dest);
            DamageInfo dkill = new DamageInfo(WNAMainDefOf.WNA_CastRange, float.PositiveInfinity, float.PositiveInfinity, -1f, parent.pawn);
            pawn.Kill(dkill);
            pawn.Corpse?.DeSpawn();
        }

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            return false;
        }
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn pawn = target.Pawn;
            if (pawn == null || pawn.def == WNAMainDefOf.WNA_WNThan)
            {
                return false;
            }
            return true;
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return Valid(target);
        }
    }
}
