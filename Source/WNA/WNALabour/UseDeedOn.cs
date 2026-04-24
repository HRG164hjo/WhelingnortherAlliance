using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.Sound;
using WNA.WNAThingCompProp;
using WNA.WNADefOf;

namespace WNA.WNALabour
{
    public class JD_UseDeedOn : JobDriver
    {
        private Pawn TargetPawn => job.GetTarget(TargetIndex.B).Pawn;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnIncapable(PawnCapacityDefOf.Manipulation);
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => TargetPawn == null || !TargetPawn.Spawned || TargetPawn.Dead || TargetPawn.Downed);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            var use = ToilMaker.MakeToil("UseDeedOnPawn");
            use.initAction = () =>
            {
                Pawn actor = use.actor;
                Thing deed = actor.CurJob.targetA.Thing;
                Pawn target = actor.CurJob.targetB.Pawn;

                var comp = deed.TryGetComp<CompFactionDeed>();
                if (comp == null || target == null || target.Dead || target.Downed) return;
                if (target.Faction == Faction.OfPlayer) return;
                if (!target.RaceProps.Humanlike) return;

                var targetFaction = Find.FactionManager.FirstFactionOfDef(comp.DeedProps.targetFactionDef);
                if (targetFaction == null) return;
                if (targetFaction.RelationKindWith(Faction.OfPlayer) != FactionRelationKind.Ally) return;
                if (target.Faction != targetFaction) return;

                RecruitUtility.Recruit(target, Faction.OfPlayer, actor);
                if (target.MapHeld != null)
                {
                    FleckMaker.Static(target.PositionHeld, target.MapHeld, FleckDefOf.PsycastAreaEffect, 2.5f);
                    var castSound = DefDatabase<SoundDef>.GetNamedSilentFail("LetterArrive_Good");
                    castSound?.PlayOneShot(new TargetInfo(target.PositionHeld, target.MapHeld));
                }

                if (target.ideo != null && actor.ideo != null)
                {
                    if (actor.Ideo.HasPrecept(WNAMainDefOf.WNA_P_Proselyte) &&
                        !target.Ideo.HasPrecept(WNAMainDefOf.WNA_P_Proselyte))
                    {
                        target.ideo.SetIdeo(actor.Ideo);
                    }
                }

                Faction.OfPlayer.TryAffectGoodwillWith(
                    targetFaction,
                    comp.DeedProps.goodwillCost,
                    canSendMessage: true,
                    canSendHostilityLetter: true,
                    reason: comp.DeedProps.goodwillReason
                );

                deed.SplitOff(1).Destroy();
            };
            use.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return use;
        }
    }
}
