using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using WNA.WNADefOf;

namespace WNA.Damager
{
    public class WNA_DamagerCulling : DamageWorker
    {
        private static readonly HashSet<HediffDef> ExcludedHediffs = new HashSet<HediffDef>
        {
            WNAMainDefOf.WNA_AbilityWisdom,
            WNAMainDefOf.WNA_CastDamage,
            WNAMainDefOf.WNA_Corrosion,
            WNAMainDefOf.WNA_DeathRefusal,
            WNAMainDefOf.WNA_ForceShield,
            WNAMainDefOf.WNA_IllusionCore,
            WNAMainDefOf.WNA_InAnimal,
            WNAMainDefOf.WNA_Inhuman,
            WNAMainDefOf.WNA_RobeBoost,
            WNAMainDefOf.sWNA_DeathRefusal
        };
        private static readonly HashSet<ThingDef> ExcludedRaces = new HashSet<ThingDef>
        {
            WNAMainDefOf.WNA_WNThan,
            WNAMainDefOf.WNA_Human
        };
        public override DamageResult Apply(DamageInfo dinfo, Thing thing)
        {
            if (!(thing is Pawn pawn))
            {
                return base.Apply(dinfo, thing);
            }
            return ApplyToPawn(dinfo, pawn);
        }

        private DamageResult ApplyToPawn(DamageInfo dinfo, Pawn pawn)
        {
            DamageResult damageResult = new DamageResult();
            if (pawn.Destroyed || pawn.Dead)
            {
                return damageResult;
            }
            if (dinfo.Amount <= 0f || (!DebugSettings.enablePlayerDamage && pawn.Faction == Faction.OfPlayer))
            {
                return damageResult;
            }
            if (pawn.Destroyed || pawn.Dead)
            {
                return damageResult;
            }
            if (ExcludedRaces.Contains(pawn.def))
            {
                return new DamageResult { totalDamageDealt = 0f };
            }
            else
            {
                if (!pawn.Dead && !pawn.Destroyed)
                {
                    PlayWoundedVoiceSound(dinfo, pawn);
                    RemoveNonBadHediffs(pawn);
                    DestroyAllParts(pawn, dinfo);
                }
            }
            return damageResult;
        }

        private void DestroyAllParts(Pawn pawn, DamageInfo dinfo)
        {
            if (pawn.Destroyed || pawn.Dead) return;
            List<BodyPartRecord> partlist = pawn.health.hediffSet.GetNotMissingParts().ToList();
            foreach (BodyPartRecord part in partlist)
            {
                if (part == null) continue;
                if (pawn.Destroyed || pawn.Dead) break;
                DamageInfo dpartinfo = new DamageInfo(WNAMainDefOf.WNA_CastMelee, float.PositiveInfinity, 999f, -1f, null, part);
                pawn.TakeDamage(dpartinfo);
            }
        }
        private static void PlayWoundedVoiceSound(DamageInfo dinfo, Pawn pawn)
        {
            if (!pawn.Dead && pawn.SpawnedOrAnyParentSpawned && dinfo.Def.ExternalViolenceFor(pawn))
            {
                LifeStageUtility.PlayNearestLifestageSound(pawn, lifeStage => lifeStage.soundWounded, gene => gene.soundWounded, mutantDef => mutantDef.soundWounded);
            }
        }
        private void RemoveNonBadHediffs(Pawn pawn)
        {
            List<Hediff> hediffsToRemove = new List<Hediff>();
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                if (!hediff.def.isBad && !ExcludedHediffs.Contains(hediff.def))
                {
                    hediffsToRemove.Add(hediff);
                }
            }
            for (int i = hediffsToRemove.Count - 1; i >= 0; i--)
            {
                pawn.health.RemoveHediff(hediffsToRemove[i]);
            }
        }
    }
}
