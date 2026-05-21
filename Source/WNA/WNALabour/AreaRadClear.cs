using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using WNA.WNAUtility;

namespace WNA.WNALabour
{
    /*public class WG_AreaRadClear : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;
        public override IEnumerable<IntVec3> PotentialWorkCellsGlobal(Pawn pawn)
            => pawn.Map.areaManager.RadiationClear().ActiveCells;

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
            => pawn.Map.areaManager.RadiationClear().TrueCount == 0;

        public override bool HasJobOnCell(Pawn pawn, IntVec3 c, bool forced = false)
        {
            var comp = pawn.Map.GetComponent<MapComp_RadField>();
            if (comp == null) return false;
            if (comp.GetRad(c) <= 0) return false; // 你需要有这个接口
            if (!pawn.CanReserve(c, 1, -1, null, forced)) return false;
            return true;
        }

        public override Job JobOnCell(Pawn pawn, IntVec3 c, bool forced = false)
            => JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("ClearRadiation"), c);
    }
    public class JD_AreaRadClear : JobDriver
    {
        private float workDone;
        private const float WorkAmount = 500f; // 自定

        public override bool TryMakePreToilReservations(bool errorOnFailed)
            => pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => !base.Map.areaManager.RadiationClear()[base.TargetLocA]);
            this.FailOn(() => base.Map.GetComponent<MapComp_RadField>()?.GetRad(base.TargetLocA) <= 0);

            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);

            Toil toil = ToilMaker.MakeToil();
            toil.tickAction = delegate
            {
                workDone += pawn.GetStatValue(StatDefOf.GeneralLaborSpeed);
                if (workDone >= WorkAmount)
                {
                    var comp = pawn.Map.GetComponent<MapComp_RadField>();
                    comp?.AddRad(base.TargetLocA, -5000);
                    ReadyForNextToil();
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.PlaySustainerOrSound(() => SoundDefOf.Interact_CleanFilth);
            toil.WithProgressBar(TargetIndex.A, () => workDone / WorkAmount);
            toil.WithEffect(EffecterDefOf.Clean, TargetIndex.A);
            yield return toil;
        }
    }*/
}
