using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;
using WNA.WNAUtility;

namespace WNA.WNARitual
{
    public class RitualBehaviorWorker_Uplifting : RitualBehaviorWorker
    {
        public RitualBehaviorWorker_Uplifting()
        {
        }
        public RitualBehaviorWorker_Uplifting(RitualBehaviorDef def) : base(def)
        {
        }
        public override string CanStartRitualNow(TargetInfo target, Precept_Ritual ritual, Pawn selectedPawn = null, Dictionary<string, Pawn> forcedForRole = null)
        {
            if (!UpliftUtility.RitualAvailable(out string reason))
                return reason;
            return base.CanStartRitualNow(target, ritual, selectedPawn, forcedForRole);
        }
        protected override LordJob CreateLordJob(TargetInfo target, Pawn organizer, Precept_Ritual ritual, RitualObligation obligation, RitualRoleAssignments assignments)
        {
            if (ritual is Precept_Ritual_Uplifting uplift && uplift.pendingSubjectThing != null)
            {
                LordJob_Ritual_Uplifting job = new LordJob_Ritual_Uplifting(
                    target,
                    ritual,
                    obligation,
                    def.stages,
                    assignments,
                    uplift.pendingSubjectPawn,
                    uplift.pendingSubjectThing,
                    uplift.pendingSubjectKind,
                    uplift.pendingSubjectCell);
                uplift.ClearPendingSubject();
                return job;
            }
            Log.Error("[WNA] Uplifting ritual was started without a selected subject.");
            return base.CreateLordJob(target, organizer, ritual, obligation, assignments);
        }
        public override bool ShouldInitAsSpectator(Pawn pawn, RitualRoleAssignments assignments)
        {
            return false;
        }
    }
}
