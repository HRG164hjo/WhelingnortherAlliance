using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using WNA.WNAWorkGiver;

namespace WNA.WNAJobDriver
{
    internal class JobDriver_RepairBattleUnit : JobDriver
    {
        private const int DefaultTicksPerHeal = 120;

        private int ticksToNextRepair;

        protected Pawn Unit => (Pawn)job.GetTarget(TargetIndex.A).Thing;

        protected bool RepairingSelf => Unit == pawn;

        protected int TicksPerHeal => Mathf.RoundToInt(1f / pawn.GetStatValue(StatDefOf.GeneralLaborSpeed) * DefaultTicksPerHeal);

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (RepairingSelf)
                return true;
            return pawn.Reserve(Unit, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnForbidden(TargetIndex.A);
            this.FailOn(() => Unit == null || Unit.Dead || Unit.IsAttacking());
            if (!RepairingSelf)
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil toil;
            if (RepairingSelf)
            {
                toil = ToilMaker.MakeToil("RepairBattleUnitSelf");
                toil.defaultCompleteMode = ToilCompleteMode.Delay;
                toil.defaultDuration = int.MaxValue;
                toil.initAction = delegate
                {
                    pawn.pather.StopDead();
                };
            }
            else
            {
                toil = Toils_General.WaitWith(TargetIndex.A, int.MaxValue, useProgressBar: false, maintainPosture: true, maintainSleep: true, face: TargetIndex.A);
                toil.handlingFacing = true;
            }
            toil.WithEffect(EffecterDefOf.ConstructMetal, TargetIndex.A);
            toil.PlaySustainerOrSound(SoundDefOf.RepairMech_Touch);
            toil.AddPreInitAction(delegate
            {
                ticksToNextRepair = TicksPerHeal;
            });
            toil.tickIntervalAction = delegate(int delta)
            {
                ticksToNextRepair -= delta;
                if (ticksToNextRepair <= 0)
                {
                    RepairTick(Unit);
                    ticksToNextRepair = TicksPerHeal;
                }
                if (!RepairingSelf)
                    pawn.rotationTracker.FaceTarget(Unit);
                if (pawn.skills != null)
                    pawn.skills.Learn(SkillDefOf.Construction, 0.05f * delta);
            };
            if (!RepairingSelf)
            {
                toil.AddFinishAction(delegate
                {
                    if (Unit?.jobs?.curJob != null)
                        Unit.jobs.EndCurrentJob(JobCondition.InterruptForced);
                });
            }
            toil.AddEndCondition(() => WorkGiver_RepairBattleUnit.NeedsRepair(Unit) ? JobCondition.Ongoing : JobCondition.Succeeded);
            toil.activeSkill = () => SkillDefOf.Construction;
            yield return toil;
        }

        private void RepairTick(Pawn unit)
        {
            if (unit.Dead || unit.health?.hediffSet == null)
                return;
            List<Hediff> hediffs = unit.health.hediffSet.hediffs;
            Hediff_Injury target = null;
            float lowestSeverity = float.PositiveInfinity;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i] is Hediff_Injury injury && injury.Severity > 0f && injury.Severity < lowestSeverity && !injury.IsPermanent())
                {
                    lowestSeverity = injury.Severity;
                    target = injury;
                }
            }
            if (target == null)
                return;
            float healAmount = 10f * Mathf.Max(1f, unit.HealthScale) * Mathf.Max(1f, unit.BodySize);
            target.Heal(healAmount);
            if (target.Severity <= 0f)
                unit.health.RemoveHediff(target);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksToNextRepair, "ticksToNextRepair", 0);
        }
    }
}
