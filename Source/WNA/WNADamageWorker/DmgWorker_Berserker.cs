using RimWorld;
using Verse;
using Verse.AI;
using WNA.WNADefOf;

namespace WNA.WNADamageWorker
{
    internal class DmgWorker_Berserker : DamageWorker
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            if (victim is Pawn pawn && !pawn.Dead && !pawn.Destroyed)
            {
                SetMentalState(pawn);
            }
            return new DamageResult();
        }

        internal static void SetMentalState(Pawn pawn)
        {
            MentalStateHandler handler = pawn.mindState?.mentalStateHandler;
            if (handler == null || handler.InMentalState || HasTrait(pawn, WNAMainDefOf.WNA_Trait_Unshakable))
            {
                return;
            }

            pawn.mindState.inspirationHandler?.Reset();

            if (!pawn.Awake())
            {
                RestUtility.WakeUp(pawn);
            }

            MentalStateDef[] candidates = GetCandidates(pawn);
            for (int i = 0; i < candidates.Length; i++)
            {
                if (handler.TryStartMentalState(candidates[i], null, forced: false, forceWake: true))
                {
                    return;
                }
            }

            TryGiveCatatonicBreakdown(pawn);
        }

        private static MentalStateDef[] GetCandidates(Pawn pawn)
        {
            if (pawn.RaceProps.IsMechanoid)
            {
                return new[] { MentalStateDefOf.BerserkPermanent, MentalStateDefOf.ManhunterPermanent };
            }
            if (!pawn.RaceProps.Humanlike)
            {
                return new[] { MentalStateDefOf.ManhunterPermanent, MentalStateDefOf.BerserkPermanent };
            }
            if (HasTrait(pawn, TraitDefOf.Wimp))
            {
                return new[] { MentalStateDefOf.PanicFlee, MentalStateDefOf.Wander_Psychotic };
            }
            MentalStateDef rage = Rand.Chance(0.6f) ? MentalStateDefOf.BerserkPermanent : MentalStateDefOf.ManhunterPermanent;
            MentalStateDef otherRage = (rage == MentalStateDefOf.BerserkPermanent) ? MentalStateDefOf.ManhunterPermanent : MentalStateDefOf.BerserkPermanent;
            return new[] { rage, otherRage, MentalStateDefOf.PanicFlee, MentalStateDefOf.Wander_Psychotic };
        }

        private static bool HasTrait(Pawn pawn, TraitDef trait)
        {
            TraitSet traits = pawn.story?.traits;
            return traits != null && traits.HasTrait(trait);
        }

        private static void TryGiveCatatonicBreakdown(Pawn pawn)
        {
            if (!pawn.IsColonist || !pawn.Spawned || pawn.Downed || !pawn.Awake()
                || pawn.mindState.mentalStateHandler.MentalBreaksBlocked())
            {
                return;
            }
            if (!pawn.health.hediffSet.HasHediff(HediffDefOf.CatatonicBreakdown))
            {
                pawn.health.AddHediff(HediffDefOf.CatatonicBreakdown);
            }
        }
    }
}
