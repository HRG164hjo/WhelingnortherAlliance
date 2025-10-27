using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using WNA.WNADefOf;

namespace WNA.Damager
{
    public class DamagerCulling : DamageWorker
    {
        private static readonly HashSet<HediffDef> ExcludedHediffs = new HashSet<HediffDef>
        {
            WNAMainDefOf.WNA_AbilityWisdom,
            WNAMainDefOf.WNA_CastDamage,
            WNAMainDefOf.WNA_Corrosion,
            WNAMainDefOf.WNA_DeathRefusal,
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
            if (!(thing is Pawn pawn)) return base.Apply(dinfo, thing);
            return ApplyToPawn(dinfo, pawn);
        }

        private DamageResult ApplyToPawn(DamageInfo dinfo, Pawn pawn)
        {
            DamageResult damageResult = new DamageResult();
            if (pawn.Dead || pawn.Destroyed) return damageResult;
            if (dinfo.Amount <= 0f) return damageResult;
            if (!DebugSettings.enablePlayerDamage &&
                pawn.Faction == Faction.OfPlayer) return damageResult;
            if (ExcludedRaces.Contains(pawn.def)) return new DamageResult { totalDamageDealt = 0f };
            PlayWoundedVoiceSound(dinfo, pawn);
            RemoveNonBadHediffs(pawn);
            DestroyAllParts(pawn, dinfo, damageResult);
            damageResult.wounded = true;
            return damageResult;
        }

        private void DestroyAllParts(Pawn pawn, DamageInfo dinfo, DamageResult damageResult)
        {
            if (pawn.Destroyed || pawn.Dead) return;
            List<BodyPartRecord> partlist = pawn.health.hediffSet.GetNotMissingParts().ToList();
            foreach (BodyPartRecord part in partlist)
            {
                if (pawn.Dead) break;
                if (pawn.health.hediffSet.PartIsMissing(part)) continue;
                Hediff_MissingPart missingPart = (Hediff_MissingPart)HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, pawn, part);
                pawn.health.AddHediff(missingPart, part, dinfo, damageResult);
                damageResult.AddPart(pawn, part);
            }
            if (!pawn.Dead)
            {
                pawn.Kill(dinfo);
                if (!pawn.Dead) pawn.health.SetDead();
            }
            damageResult.totalDamageDealt = float.MaxValue;
        }
        private static void PlayWoundedVoiceSound(DamageInfo dinfo, Pawn pawn)
        {
            if (!pawn.Dead && pawn.SpawnedOrAnyParentSpawned &&
                dinfo.Def.ExternalViolenceFor(pawn)) LifeStageUtility.PlayNearestLifestageSound(pawn, lifeStage => lifeStage.soundWounded, gene => gene.soundWounded, mutantDef => mutantDef.soundWounded);
        }
        private void RemoveNonBadHediffs(Pawn pawn)
        {
            List<Hediff> hediffsToRemove = new List<Hediff>();
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                if (!hediff.def.isBad &&
                    !ExcludedHediffs.Contains(hediff.def)) hediffsToRemove.Add(hediff);
            }
            for (int i = hediffsToRemove.Count - 1; i >= 0; i--) pawn.health.RemoveHediff(hediffsToRemove[i]);
        }
    }
}
