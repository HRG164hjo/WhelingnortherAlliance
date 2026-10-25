using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using WNA.WNAUtility;
using WNA.WNAWorldComp;

namespace WNA.WNARitual
{
    public class LordJob_Ritual_Uplifting : LordJob_Ritual
    {
        public Pawn subjectPawn;
        public Thing subjectThing;
        public UpliftSubjectKind subjectKind = UpliftSubjectKind.Invalid;
        public IntVec3 subjectCell = IntVec3.Invalid;
        private int nextMarkerTick = -1;

        public LordJob_Ritual_Uplifting()
        {
        }

        public LordJob_Ritual_Uplifting(
            TargetInfo selectedTarget,
            Precept_Ritual ritual,
            RitualObligation obligation,
            List<RitualStage> allStages,
            RitualRoleAssignments assignments,
            Pawn subjectPawn,
            Thing subjectThing,
            UpliftSubjectKind subjectKind,
            IntVec3 subjectCell)
            : base(selectedTarget, ritual, obligation, allStages, assignments)
        {
            this.subjectPawn = subjectPawn;
            this.subjectThing = subjectThing;
            this.subjectKind = subjectKind;
            this.subjectCell = subjectCell;
            DetachSubjectFromLord();
            RegisterProtection();
        }

        public override void LordJobTick()
        {
            base.LordJobTick();
            KeepSubjectStill();
            DrawSubjectMarker();
        }

        public override void Cleanup()
        {
            UnregisterProtection();
            base.Cleanup();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref subjectPawn, "upliftSubjectPawn");
            Scribe_References.Look(ref subjectThing, "upliftSubjectThing");
            Scribe_Values.Look(ref subjectKind, "upliftSubjectKind", UpliftSubjectKind.Invalid);
            Scribe_Values.Look(ref subjectCell, "upliftSubjectCell", IntVec3.Invalid);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                RegisterProtection();
        }

        public void RegisterProtection()
        {
            WorldComp_UpliftProtection protection = WorldComp_UpliftProtection.Instance;
            protection?.Register(subjectThing, subjectPawn);
        }

        private void DetachSubjectFromLord()
        {
            if (subjectPawn == null || subjectPawn.Destroyed)
                return;
            Lord lord = subjectPawn.GetLord();
            if (lord != null && !(lord.LordJob is LordJob_Ritual))
                lord.Notify_PawnLost(subjectPawn, PawnLostCondition.LeftVoluntarily);
        }

        public void UnregisterProtection()
        {
            WorldComp_UpliftProtection protection = WorldComp_UpliftProtection.Instance;
            protection?.Unregister(subjectThing, subjectPawn);
        }

        private void DrawSubjectMarker()
        {
            int now = GenTicks.TicksGame;
            if (now < nextMarkerTick)
                return;
            nextMarkerTick = now + 20;
            UpliftUtility.SpawnSubjectMarker(subjectThing, subjectPawn, Map, subjectCell);
        }

        private void KeepSubjectStill()
        {
            Pawn pawn = subjectPawn;
            if (pawn == null || pawn.Destroyed || pawn.Dead || !pawn.Spawned)
                return;
            if (pawn.jobs?.curJob != null)
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, startNewJob: false);
            if (pawn.pather != null && pawn.pather.Moving)
                pawn.pather.StopDead();
            pawn.stances?.stunner?.StunFor(10, null, addBattleLog: false, showMote: false);
        }
    }
}
