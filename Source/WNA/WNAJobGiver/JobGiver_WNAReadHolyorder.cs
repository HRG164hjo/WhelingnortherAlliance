using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using WNA.WNADefOf;

namespace WNA.WNAJobGiver
{
    public class JobGiver_WNAReadHolyorder : ThinkNode_JobGiver
    {
        public static bool CanRead(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned || pawn.Dead || pawn.Downed || pawn.Drafted)
                return false;
            if (pawn.InMentalState || pawn.IsBurning())
                return false;
            if (pawn.CurrentBed() != null || pawn.GetPosture() != PawnPosture.Standing)
                return false;
            if (!pawn.IsColonist)
                return false;
            if (pawn.carryTracker?.CarriedThing != null)
                return false;
            if (pawn.apparel == null)
                return false;

            return pawn.apparel.WornApparel.Any(a => a.def == WNAMainDefOf.WNA_Holyorder);
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!CanRead(pawn))
                return null;

            Apparel holyorder = pawn.apparel.WornApparel
                .FirstOrDefault(a => a.def == WNAMainDefOf.WNA_Holyorder);

            if (holyorder == null)
                return null;

            Job job = JobMaker.MakeJob(WNAMainDefOf.WNA_ReadHolyorder, holyorder);
            job.ignoreJoyTimeAssignment = true;
            return job;
        }
    }
}
