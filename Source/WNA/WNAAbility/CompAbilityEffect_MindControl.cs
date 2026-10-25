using RimWorld;
using Verse;
using WNA.WNAModExtension;
using WNA.WNAUtility;

namespace WNA.WNAAbility
{
    public class CompAbilityEffect_MindControl : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            if (target.Pawn == null)
                return;
            MindControlConfig config = MindControlConfig.Get(parent.def);
            WNAMindControlUtility.TryControl(parent.pawn, target.Pawn, parent.def, config);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages))
                return false;
            Pawn pawn = target.Pawn;
            if (pawn == null)
                return false;
            if (pawn.RaceProps.intelligence != Intelligence.Humanlike)
                return true;

            MindControlConfig config = MindControlConfig.Get(parent.def);
            if (!WNAMindControlUtility.CanBeControlled(parent.pawn, pawn, parent.def, config, out string reason))
            {
                if (throwMessages)
                    Messages.Message(reason, pawn, MessageTypeDefOf.RejectInput, false);
                return false;
            }
            return true;
        }

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            if (parent.pawn == null || parent.pawn.Faction == Faction.OfPlayer)
                return false;
            Pawn pawn = target.Pawn;
            if (pawn == null || pawn.RaceProps.intelligence != Intelligence.Humanlike)
                return false;
            MindControlConfig config = MindControlConfig.Get(parent.def);
            return WNAMindControlUtility.CanBeControlled(parent.pawn, pawn, parent.def, config, out _);
        }
    }
}
