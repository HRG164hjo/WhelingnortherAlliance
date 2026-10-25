using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using WNA.WNAUtility;
namespace WNA.WNAJobDriver
{
    internal class JobDriver_ForceReeducation : JobDriver
    {
        private const int HitsPerBeat = 6;
        private const int HitIntervalTicks = 36;
        private const int WaitTicks = 150;
        private const int StunTicks = 500;
        private const int LectureTicks = 150;
        private const int CowerMaxWaitTicks = 300;
        private const float MinDamage = 6f;
        private const float MaxDamage = 10f;
        private const float SafetyHealthPercent = 0.4f;
        private int strikesThisBeat;
        private int ticksUntilNextHit;
        private Pawn Prisoner => (Pawn)job.targetA.Thing;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => Prisoner == null || Prisoner.Dead || !Prisoner.IsPrisonerOfColony);
            Toil approach = MakeApproachToil();
            yield return approach;
            Toil beat = MakeBeatToil();
            Toil skipToBeat = ToilMaker.MakeToil("JobDriver_ForceReeducation_SkipToBeat");
            skipToBeat.defaultCompleteMode = ToilCompleteMode.Instant;
            skipToBeat.initAction = delegate
            {
                if (!PrisonerIsAwake() || PrisonerIsInMentalState())
                    JumpToToil(beat);
            };
            yield return skipToBeat;
            yield return Toils_Interpersonal.WaitToBeAbleToInteract(pawn);
            yield return Toils_Interpersonal.GotoInteractablePosition(TargetIndex.A);
            yield return MakeReduceWillToil();
            yield return Toils_General.Wait(WaitTicks, TargetIndex.A);
            Toil approachAgain = MakeApproachToil();
            yield return approachAgain;
            yield return Toils_Interpersonal.GotoInteractablePosition(TargetIndex.A);
            yield return beat;
            yield return MakePrisonerFleeToil();
            yield return MakeWaitForCowerToil();
            Toil approachAfterCower = MakeApproachToil();
            yield return approachAfterCower;
            yield return Toils_Interpersonal.GotoInteractablePosition(TargetIndex.A);
            yield return MakeReduceWillToil();
            Toil loop = ToilMaker.MakeToil("JobDriver_ForceReeducation_Loop");
            loop.defaultCompleteMode = ToilCompleteMode.Instant;
            loop.initAction = delegate
            {
                JumpToToil(approach);
            };
            yield return loop;
        }
        private static Toil MakeApproachToil()
        {
            Toil toil = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            toil.socialMode = RandomSocialMode.Off;
            return toil;
        }
        private Toil MakeReduceWillToil()
        {
            Toil toil = ToilMaker.MakeToil("JobDriver_ForceReeducation_ReduceWill");
            toil.socialMode = RandomSocialMode.Off;
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = LectureTicks;
            toil.activeSkill = () => SkillDefOf.Social;
            toil.handlingFacing = true;
            toil.initAction = delegate
            {
                pawn.rotationTracker.FaceTarget(Prisoner);
                if (InteractionDefOf.ReduceWill != null && pawn.interactions.TryInteractWith(Prisoner, InteractionDefOf.ReduceWill))
                    pawn.records.Increment(RecordDefOf.PrisonersChatted);
            };
            toil.tickIntervalAction = delegate
            {
                pawn.rotationTracker.FaceTarget(Prisoner);
            };
            return toil;
        }
        private Toil MakeBeatToil()
        {
            Toil beat = ToilMaker.MakeToil("JobDriver_ForceReeducation_Beat");
            beat.defaultCompleteMode = ToilCompleteMode.Never;
            beat.socialMode = RandomSocialMode.Off;
            beat.handlingFacing = true;
            beat.initAction = delegate
            {
                strikesThisBeat = 0;
                ticksUntilNextHit = 0;
            };
            beat.tickIntervalAction = delegate(int delta)
            {
                if (Prisoner == null || Prisoner.Dead)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                pawn.rotationTracker.FaceTarget(Prisoner);
                if (Prisoner.Downed)
                {
                    FinishReeducation();
                    return;
                }
                ticksUntilNextHit += delta;
                if (ticksUntilNextHit < HitIntervalTicks)
                    return;
                ticksUntilNextHit = 0;
                Strike();
                if (Prisoner == null || Prisoner.Dead)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                if (Prisoner.Downed)
                {
                    FinishReeducation();
                    return;
                }
                strikesThisBeat++;
                if (strikesThisBeat >= HitsPerBeat)
                {
                    EndPrisonerMentalState();
                    ReadyForNextToil();
                }
            };
            return beat;
        }
        private Toil MakePrisonerFleeToil()
        {
            Toil toil = ToilMaker.MakeToil("JobDriver_ForceReeducation_PrisonerFlee");
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            toil.initAction = delegate
            {
                Pawn prisoner = Prisoner;
                if (prisoner == null || prisoner.Dead || prisoner.Downed || !prisoner.Spawned)
                    return;
                if (prisoner.CurJobDef == JobDefOf.FleeAndCower)
                    return;
                IntVec3 dest = FindCowerDest(prisoner);
                Job flee = JobMaker.MakeJob(JobDefOf.FleeAndCower, dest);
                prisoner.jobs.StartJob(flee, JobCondition.InterruptForced);
            };
            return toil;
        }
        private Toil MakeWaitForCowerToil()
        {
            Toil toil = ToilMaker.MakeToil("JobDriver_ForceReeducation_WaitForCower");
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = CowerMaxWaitTicks;
            toil.initAction = delegate
            {
                if (!PrisonerIsCowering())
                    ReadyForNextToil();
            };
            toil.tickIntervalAction = delegate
            {
                if (!PrisonerIsCowering())
                    ReadyForNextToil();
            };
            return toil;
        }
        private bool PrisonerIsAwake()
        {
            Pawn prisoner = Prisoner;
            return prisoner != null && prisoner.Awake();
        }
        private bool PrisonerIsInMentalState()
        {
            Pawn prisoner = Prisoner;
            return prisoner != null && prisoner.InMentalState;
        }
        private void EndPrisonerMentalState()
        {
            Pawn prisoner = Prisoner;
            if (prisoner == null || !prisoner.InMentalState)
                return;
            prisoner.MentalState?.RecoverFromState();
        }
        private bool PrisonerIsCowering()
        {
            Pawn prisoner = Prisoner;
            return prisoner != null
                && !prisoner.Dead
                && !prisoner.Downed
                && prisoner.Spawned
                && prisoner.CurJobDef == JobDefOf.FleeAndCower;
        }
        private IntVec3 FindCowerDest(Pawn prisoner)
        {
            Map map = prisoner.Map;
            Room room = prisoner.GetRoom();
            if (map == null || room == null)
                return prisoner.Position;
            IntVec3 best = prisoner.Position;
            float bestDist = -1f;
            foreach (IntVec3 cell in room.Cells)
            {
                if (cell == prisoner.Position || cell == pawn.Position)
                    continue;
                if (!cell.Standable(map) || cell.GetTerrain(map).dangerous)
                    continue;
                if (cell.GetFirstPawn(map) != null)
                    continue;
                float dist = cell.DistanceToSquared(pawn.Position);
                if (dist > bestDist)
                {
                    bestDist = dist;
                    best = cell;
                }
            }
            return best;
        }
        private void FinishReeducation()
        {
            EndPrisonerMentalState();
            WNAPsychicDawnUtility.TryRemoveNegativeTrait(Prisoner);
            WNAPsychicDawnUtility.ApplyReeducationAftermath(Prisoner, pawn);
            EndJobWith(JobCondition.Succeeded);
        }
        private void Strike()
        {
            Pawn prisoner = Prisoner;
            prisoner.stances.stunner.StunFor(StunTicks, pawn, addBattleLog: true);
            if (prisoner.health.summaryHealth.SummaryHealthPercent <= SafetyHealthPercent)
            {
                HealthUtility.DamageUntilDowned(prisoner, allowBleedingWounds: false, DamageDefOf.Blunt);
                return;
            }
            DamageInfo dinfo = new DamageInfo(
                DamageDefOf.Blunt,
                Rand.Range(MinDamage, MaxDamage),
                0f,
                -1f,
                pawn,
                null,
                null,
                DamageInfo.SourceCategory.ThingOrUnknown,
                prisoner,
                instigatorGuilty: true,
                spawnFilth: true,
                QualityCategory.Normal,
                checkForJobOverride: false);
            prisoner.TakeDamage(dinfo);
        }
    }
}
