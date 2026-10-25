using RimWorld;
using Verse;
using Verse.AI.Group;

namespace WNA.WNARitual
{
    public class RitualRole_AbilityRecipient : RitualRoleColonist
    {
        public override bool AppliesToPawn(Pawn p, out string reason, TargetInfo selectedTarget, LordJob_Ritual ritual = null, RitualRoleAssignments assignments = null, Precept_Ritual precept = null, bool skipReason = false)
        {
            if (!base.AppliesToPawn(p, out reason, selectedTarget, ritual, assignments, precept, skipReason))
                return false;
            if (p.GetLord() != null)
            {
                if (!skipReason)
                    reason = "WNA_AbilityRitual_RecipientBusy".Translate().Resolve();
                return false;
            }
            if (p.abilities == null)
            {
                if (!skipReason)
                    reason = "WNA_AbilityRitual_NeedsAbilityTracker".Translate(p.Named("PAWN")).Resolve();
                return false;
            }
            return true;
        }
    }
}
