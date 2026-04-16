using RimWorld;
using Verse;
using WNA.WNAUtility;

namespace WNA.WNAAbilityCompProp
{
    public class PropMindControl : CompProperties_AbilityEffect
    {
        public bool permanent = false;
        public PropMindControl()
        {
            compClass = typeof(CompMindControl);
        }
    }
    public class CompMindControl : CompAbilityEffect
    {

        public new PropMindControl Props => (PropMindControl)props;
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn caster = parent.pawn;
            Thing thing = target.Thing;
            base.Apply(target, dest);
            if (thing != null )
                MindControlUtility.TryControl(caster, thing, Props.permanent);
        }
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn caster = parent.pawn;
            Thing thing = target.Thing;
            if (thing != null)
            {
                if (Props.permanent)
                {
                    if (MindControlUtility.CanBeControlled(caster, thing))
                        return true;
                }
                else
                {
                    if (thing is Pawn pawn && MindControlUtility.CanBeControlled(caster, pawn))
                        return true;
                }
            }
            return false;
        }
        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!base.Valid(target)) return false;
            if (!base.CanApplyOn(target, dest)) return false;
            return Valid(target);
        }
    }
}
