using System.Collections.Generic;
using Verse;
using Verse.AI;
using WNA.WNAThingCompProp;

namespace WNA.WNALabour
{
    public class JD_SacrificePawn : JobDriver
    {
        private Thing Victim => job.GetTarget(TargetIndex.A).Thing;
        private Building Basin => job.GetTarget(TargetIndex.B).Thing as Building;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Victim, job, 1, 1)
                && pawn.Reserve(Basin, job, 1, 1);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOnBurningImmobile(TargetIndex.B);
            this.FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return Toils_General.Do(() =>
            {
                job.count = 1;
            });
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell);
            Toil doSacrifice = new Toil();
            doSacrifice.initAction = () =>
            {
                if (Basin == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                var comp = Basin.TryGetComp<CompOrganicBasin>();
                if (comp == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                Thing carried = pawn.carryTracker?.CarriedThing;
                if (carried == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                comp.SetTemplateFromThing(carried);
                pawn.carryTracker.DestroyCarriedThing();
            };
            doSacrifice.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return doSacrifice;
        }
    }
}