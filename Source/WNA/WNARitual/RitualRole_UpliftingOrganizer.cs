using RimWorld;
using Verse;
using Verse.AI.Group;
using WNA.WNAUtility;

namespace WNA.WNARitual
{
    public class RitualRole_UpliftingOrganizer : RitualRole
    {
        public override bool AppliesToRole(Precept_Role role, out string reason, Precept_Ritual ritual = null, Pawn pawn = null, bool skipReason = false)
        {
            reason = null;
            if (pawn == null)
                return false;
            if (pawn.GetLord() != null)
            {
                if (!skipReason)
                    reason = "WNA_UpliftOrganizerBusy".Translate().Resolve();
                return false;
            }
            if (!General.PawnHasIdeo(pawn))
            {
                if (!skipReason)
                    reason = "WNA_UpliftOrganizerNeedsIdeo".Translate().Resolve();
                return false;
            }
            return true;
        }
    }
}
