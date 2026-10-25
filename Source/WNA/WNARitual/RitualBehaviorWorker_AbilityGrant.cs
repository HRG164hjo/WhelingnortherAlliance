using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;
using WNA.WNAUtility;

namespace WNA.WNARitual
{
    public class RitualBehaviorWorker_AbilityGrant : RitualBehaviorWorker
    {
        public RitualBehaviorWorker_AbilityGrant()
        {
        }

        public RitualBehaviorWorker_AbilityGrant(RitualBehaviorDef def) : base(def)
        {
        }

        public override string CanStartRitualNow(TargetInfo target, Precept_Ritual ritual, Pawn selectedPawn = null, Dictionary<string, Pawn> forcedForRole = null)
        {
            if (!AbilityGrantUtility.RitualAvailable(out string reason))
                return reason;
            return base.CanStartRitualNow(target, ritual, selectedPawn, forcedForRole);
        }

        protected override LordJob CreateLordJob(TargetInfo target, Pawn organizer, Precept_Ritual ritual, RitualObligation obligation, RitualRoleAssignments assignments)
        {
            return new LordJob_Ritual_AbilityGrant(target, ritual, obligation, def.stages, assignments);
        }

        public override bool ShouldInitAsSpectator(Pawn pawn, RitualRoleAssignments assignments)
        {
            return false;
        }
    }
}
