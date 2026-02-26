using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using WNA.WNAMiscs;

namespace WNA.JobDriverClass
{
    public class Revive : JobDriver
    {
        private Mote warmupMote;
        private Thing target => job.GetTarget(TargetIndex.A).Thing;
        private Thing item => job.GetTarget(TargetIndex.B).Thing;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (pawn.Reserve(target, job, 1, -1, null, errorOnFailed))
                return pawn.Reserve(item, job, 1, -1, null, errorOnFailed);
            return false;
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.B).FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.A);
            Toil toil = Toils_General.Wait(30);
            toil.WithProgressBarToilDelay(TargetIndex.A);
            toil.FailOnDespawnedOrNull(TargetIndex.A);
            toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            toil.tickAction = delegate
            {
                CompUsable compUsable = item.TryGetComp<CompUsable>();
                if (compUsable != null && warmupMote == null && compUsable.Props.warmupMote != null)
                    warmupMote = MoteMaker.MakeAttachedOverlay(target, compUsable.Props.warmupMote, Vector3.zero);
                warmupMote?.Maintain();
            };
            yield return toil;
            yield return Toils_General.Do(ReviveFunc);
        }

        private void ReviveFunc()
        {
            if (target is Pawn pawn && !pawn.Dead)
            {
                HediffSet hediffSet = pawn.health.hediffSet;
                List<Hediff> hediffsToRemove = new List<Hediff>();
                foreach (Hediff hediff in hediffSet.hediffs)
                {
                    if (hediff is Hediff_Injury ||
                        (hediff.Severity > 0 && hediff.def.isBad) ||
                        HedifferRemover.includedHediffs.Contains(hediff.def.defName))
                        hediffsToRemove.Add(hediff);
                }
                foreach (Hediff hediff in hediffsToRemove)
                    pawn.health.RemoveHediff(hediff);
                    
            }
            else if (target is Corpse corpse && !corpse.Destroyed)
            {
                Pawn innerPawn = corpse.InnerPawn;
                ResurrectionUtility.TryResurrect(innerPawn);
                if (innerPawn.Faction.HostileTo(Faction.OfPlayer))
                    innerPawn.health.AddHediff(HediffDefOf.CatatonicBreakdown);
            }
            else
            {
                Log.Error("[WNA.JobDriverClass.Revive] revive target is invalid!");
                return;
            }
            item.SplitOff(1).Destroy();
        }
    }
}
