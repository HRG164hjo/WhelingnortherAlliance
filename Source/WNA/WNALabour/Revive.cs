using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using WNA.WNAMiscs;

namespace WNA.WNALabour
{
    public class JD_Revive : JobDriver
    {
        private Mote warmupMote;
        private Thing Target => job.GetTarget(TargetIndex.A).Thing;
        private Thing Item => job.GetTarget(TargetIndex.B).Thing;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (pawn.Reserve(Target, job, 1, -1, null, errorOnFailed))
                return pawn.Reserve(Item, job, 1, -1, null, errorOnFailed);
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
                CompUsable compUsable = Item.TryGetComp<CompUsable>();
                if (compUsable != null && warmupMote == null && compUsable.Props.warmupMote != null)
                    warmupMote = MoteMaker.MakeAttachedOverlay(Target, compUsable.Props.warmupMote, Vector3.zero);
                warmupMote?.Maintain();
            };
            yield return toil;
            yield return Toils_General.Do(ReviveFunc);
        }

        private void ReviveFunc()
        {
            if (Target is Pawn pawn && !pawn.Dead)
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
            else if (Target is Corpse corpse && !corpse.Destroyed)
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
            Item.SplitOff(1).Destroy();
        }
    }
}
