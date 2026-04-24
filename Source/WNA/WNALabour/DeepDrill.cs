using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using WNA.WNAThingCompProp;
using WNA.WNADefOf;

namespace WNA.WNALabour
{
    public class WG_DeepDrill : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);
        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;
        public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            foreach (Building b in pawn.Map.listerBuildings.allBuildingsColonist)
            {
                var comp = b.GetComp<CompStarcoreDriller>();
                if (comp != null)
                    yield return b;
            }
        }
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t.Faction != pawn.Faction) return false;
            var comp = t.TryGetComp<CompStarcoreDriller>();
            if (comp == null || !comp.CanDrillNow() || comp.IsAutoMode()) return false;
            if (!pawn.CanReserve(t, 1, -1, null, forced)) return false;
            if (t.Map.designationManager.DesignationOn(t, DesignationDefOf.Uninstall) != null) return false;
            return true;
        }
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(WNAMainDefOf.WNA_Job_DeepDrilling, t, 1500, checkOverrideOnExpiry: true);
        }
    }
    public class JD_DeepDrill : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnBurningImmobile(TargetIndex.A);
            this.FailOnThingHavingDesignation(TargetIndex.A, DesignationDefOf.Uninstall);
            this.FailOn(() =>
            {
                Thing thing = job.targetA.Thing;
                CompStarcoreDriller comp = thing?.TryGetComp<CompStarcoreDriller>();
                return comp == null || !comp.CanDrillNow();
            });
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            Toil work = ToilMaker.MakeToil("OperateStarcoreDrill");
            work.tickIntervalAction = delta =>
            {
                Pawn actor = work.actor;
                var comp = ((Building)actor.CurJob.targetA.Thing).GetComp<CompStarcoreDriller>();
                comp?.DrillWorkDone(actor, delta);
                actor.skills?.Learn(SkillDefOf.Mining, 0.065f * delta);
            };
            work.defaultCompleteMode = ToilCompleteMode.Never;
            work.WithEffect(EffecterDefOf.Drill, TargetIndex.A);
            work.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            work.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            work.activeSkill = () => SkillDefOf.Mining;
            yield return work;
        }
    }
}
