using RimWorld;
using RimWorld.Planet;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using WNA.WNADefOf;

namespace WNA.Damager
{
    public class WNA_DamagerBasic : DamageWorker
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing thing)
        {
            if (!(thing is Pawn pawn)) return base.Apply(dinfo, thing);
            return ApplyToPawn(dinfo, pawn);
        }
        private DamageResult ApplyToPawn(DamageInfo dinfo, Pawn pawn)
        {
            DamageResult damageResult = new DamageResult();
            if (dinfo.Amount <= 0f) return damageResult;
            if (!DebugSettings.enablePlayerDamage && pawn.Faction == Faction.OfPlayer) return damageResult;
            void ApplySingleDamageToPart(DamageInfo info)
            {
                ApplyDamageToPart(info, pawn, damageResult);
            }
            if (dinfo.ApplyAllDamage) HandleApplyAllDamage(dinfo, damageResult, ApplySingleDamageToPart);
            else if (dinfo.AllowDamagePropagation && dinfo.Amount >= (float)dinfo.Def.minDamageToFragment) HandleFragmentedDamage(dinfo, ApplySingleDamageToPart);
            else
            {
                ApplySingleDamageToPart(dinfo);
                ApplySmallPawnDamagePropagation(dinfo, pawn, damageResult);
            }
            ApplyPostDamageEffects(dinfo, pawn, damageResult);
            return damageResult;
        }
        #region Damage Handling
        private void HandleApplyAllDamage(DamageInfo dinfo, DamageResult damageResult, Action<DamageInfo> applySingleDamage)
        {
            float remainingDamage = dinfo.Amount;
            const int MaxDamageIterations = 25;
            int numPartsToPropagate = dinfo.DamagePropagationPartsRange.RandomInRange;
            float maxSingleDamage = remainingDamage / (float)Math.Max(1, numPartsToPropagate);
            for (int i = 0; i < MaxDamageIterations && remainingDamage > 0f; i++)
            {
                DamageInfo currentDinfo = dinfo;
                currentDinfo.SetAmount(Mathf.Min(remainingDamage, maxSingleDamage));
                applySingleDamage(currentDinfo);
                remainingDamage -= currentDinfo.Amount;
                remainingDamage -= damageResult.totalDamageDealt - (damageResult.totalDamageDealt - currentDinfo.Amount);
            }
        }
        private void HandleFragmentedDamage(DamageInfo dinfo, Action<DamageInfo> applySingleDamage)
        {
            int numParts = Math.Max(1, dinfo.DamagePropagationPartsRange.RandomInRange);
            float damagePerPart = dinfo.Amount / (float)numParts;
            for (int i = 0; i < numParts; i++)
            {
                DamageInfo currentDinfo = dinfo;
                currentDinfo.SetAmount(damagePerPart);
                applySingleDamage(currentDinfo);
            }
        }
        #endregion
        #region Damage Effect
        private void ApplyPostDamageEffects(DamageInfo dinfo, Pawn pawn, DamageResult damageResult)
        {
            bool spawnedOrAnyParentSpawned = pawn.SpawnedOrAnyParentSpawned;
            if (damageResult.wounded)
            {
                PlayWoundedVoiceSound(dinfo, pawn);
                pawn.Drawer.Notify_DamageApplied(dinfo);
                HandleWoundedEffecter(dinfo, pawn);
            }
            if (damageResult.headshot && pawn.Spawned)
            {
                MoteMaker.ThrowText(new Vector3(pawn.Position.x + 1f, pawn.Position.y, pawn.Position.z + 1f), pawn.Map, "Headshot".Translate(), Color.white);
                if (dinfo.Instigator is Pawn instigatorPawn) instigatorPawn.records.Increment(RecordDefOf.Headshots);
            }
            if ((damageResult.deflected || damageResult.diminished) && spawnedOrAnyParentSpawned) HandleDeflectionOrDiminishedEffecter(dinfo, pawn, damageResult);
            else if (!damageResult.deflected && spawnedOrAnyParentSpawned) ImpactSoundUtility.PlayImpactSound(pawn, dinfo.Def.impactSoundType, pawn.MapHeld);
        }
        private void HandleWoundedEffecter(DamageInfo dinfo, Pawn pawn)
        {
            EffecterDef raceDamageEffecter = pawn.RaceProps.FleshType.damageEffecter;
            if (raceDamageEffecter != null)
            {
                if (pawn.health.woundedEffecter == null || pawn.health.woundedEffecter.def != raceDamageEffecter)
                {
                    pawn.health.woundedEffecter?.Cleanup();
                    pawn.health.woundedEffecter = raceDamageEffecter.Spawn();
                }
                pawn.health.woundedEffecter.Trigger(pawn, dinfo.Instigator ?? pawn);
            }
            if (dinfo.Def.damageEffecter != null)
            {
                Effecter effecter = dinfo.Def.damageEffecter.Spawn();
                effecter.Trigger(pawn, pawn);
                effecter.Cleanup();
            }
        }
        private void HandleDeflectionOrDiminishedEffecter(DamageInfo dinfo, Pawn pawn, DamageResult damageResult)
        {
            EffecterDef effecterDef;
            if (damageResult.deflected)
            {
                if (damageResult.deflectedByMetalArmor && dinfo.Def.canUseDeflectMetalEffect) effecterDef = (dinfo.Def == DamageDefOf.Bullet) ? EffecterDefOf.Deflect_Metal_Bullet : EffecterDefOf.Deflect_Metal;
                else effecterDef = (dinfo.Def == DamageDefOf.Bullet) ? EffecterDefOf.Deflect_General_Bullet : EffecterDefOf.Deflect_General;
                pawn.Drawer.Notify_DamageDeflected(dinfo);
            }
            else effecterDef = (damageResult.diminishedByMetalArmor) ? EffecterDefOf.DamageDiminished_Metal : EffecterDefOf.DamageDiminished_General;
            if (pawn.health.deflectionEffecter == null || pawn.health.deflectionEffecter.def != effecterDef)
            {
                pawn.health.deflectionEffecter?.Cleanup();
                pawn.health.deflectionEffecter = effecterDef.Spawn();
            }
            pawn.health.deflectionEffecter.Trigger(pawn, dinfo.Instigator ?? pawn);
        }
        #endregion
        private void ApplySmallPawnDamagePropagation(DamageInfo dinfo, Pawn pawn, DamageResult result)
        {
            if (dinfo.AllowDamagePropagation && result.LastHitPart != null &&
                    dinfo.Def.harmsHealth && result.LastHitPart != pawn.RaceProps.body.corePart &&
                    result.LastHitPart.parent != null && pawn.health.hediffSet.GetPartHealth(result.LastHitPart.parent) > 0f &&
                    result.LastHitPart.parent.coverageAbs > 0f && dinfo.Amount >= 10f &&
                    pawn.HealthScale <= 0.5001f)
            {
                DamageInfo dinfo2 = dinfo;
                dinfo2.SetHitPart(result.LastHitPart.parent);
                ApplyDamageToPart(dinfo2, pawn, result);
            }
        }
        private void ApplyDamageToPart(DamageInfo dinfo, Pawn pawn, DamageResult result)
        {
            BodyPartRecord exactPart = GetExactPartFromDamageInfo(dinfo, pawn);
            if (exactPart == null) return;
            dinfo.SetHitPart(exactPart);
            float amount = dinfo.Amount;
            if (amount <= 0f)
            {
                result.AddPart(pawn, dinfo.HitPart);
                result.deflected = true;
                return;
            }
            if (IsHeadshot(dinfo, pawn))
            {
                result.headshot = true;
                BodyPartRecord dpart = dinfo.HitPart;
                if (pawn.def == WNAMainDefOf.WNA_WNThan || pawn.def == WNAMainDefOf.WNA_Human) return;
                else
                {
                    if (!pawn.Dead)
                    {
                        DamageInfo kinfo = dinfo;
                        kinfo.SetAmount(float.MaxValue);
                        kinfo.SetHitPart(dpart);
                        FinalizeAndAddInjury(pawn, float.MaxValue, kinfo, result);
                        if (!pawn.Dead) pawn.health.SetDead();
                    }
                }
                return;
            }
            bool isStandardInjuryApplicable = !dinfo.InstantPermanentInjury ||
                (HealthUtility.GetHediffDefFromDamage(dinfo.Def, pawn, dinfo.HitPart).CompPropsFor(typeof(HediffComp_GetsPermanent)) != null &&
                dinfo.HitPart.def.permanentInjuryChanceFactor != 0f &&
                !pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(dinfo.HitPart));
            if (isStandardInjuryApplicable)
            {
                if (!dinfo.AllowDamagePropagation) FinalizeAndAddInjury(pawn, dinfo.Amount, dinfo, result);
                else ApplySpecialEffectsToPart(pawn, dinfo.Amount, dinfo, result);
            }
        }
        protected virtual void ApplySpecialEffectsToPart(Pawn pawn, float totalDamage, DamageInfo dinfo, DamageResult result)
        {
            totalDamage = ReduceDamageToPreserveOutsideParts(totalDamage, dinfo, pawn);
            FinalizeAndAddInjury(pawn, totalDamage, dinfo, result);
            CheckDuplicateDamageToOuterParts(dinfo, pawn, totalDamage, result);
        }
        protected float FinalizeAndAddInjury(Pawn pawn, float totalDamage, DamageInfo dinfo, DamageResult result)
        {
            if (pawn.health.hediffSet.PartIsMissing(dinfo.HitPart)) return 0f;
            Pawn pawn2 = dinfo.Instigator as Pawn;
            HediffDef hediffDefFromDamage = HealthUtility.GetHediffDefFromDamage(dinfo.Def, pawn, dinfo.HitPart);
            Hediff_Injury hediff_Injury = (Hediff_Injury)HediffMaker.MakeHediff(hediffDefFromDamage, pawn);
            hediff_Injury.Part = dinfo.HitPart;
            hediff_Injury.sourceDef = dinfo.Weapon;
            if (pawn2 != null && pawn2.IsMutant && dinfo.Weapon == ThingDefOf.Human) hediff_Injury.sourceLabel = pawn2.mutant.Def.label;
            else hediff_Injury.sourceLabel = dinfo.Weapon?.label ?? "";
            hediff_Injury.sourceBodyPartGroup = dinfo.WeaponBodyPartGroup;
            hediff_Injury.sourceHediffDef = dinfo.WeaponLinkedHediff;
            hediff_Injury.sourceToolLabel = dinfo.Tool?.labelNoLocation ?? dinfo.Tool?.label;
            hediff_Injury.Severity = totalDamage;
            if (pawn2 != null && pawn2.CurJobDef == JobDefOf.SocialFight) hediff_Injury.destroysBodyParts = false;
            if (dinfo.InstantPermanentInjury)
            {
                HediffComp_GetsPermanent hediffComp_GetsPermanent =
                    hediff_Injury.TryGetComp<HediffComp_GetsPermanent>();
                if (hediffComp_GetsPermanent != null) hediffComp_GetsPermanent.IsPermanent = true;
                else Log.Error(string.Concat("Tried to create instant permanent injury on Hediff without a GetsPermanent comp: ", hediffDefFromDamage, " on ", pawn));
            }
            return FinalizeAndAddInjury(pawn, hediff_Injury, dinfo, result);
        }
        protected float FinalizeAndAddInjury(Pawn pawn, Hediff_Injury injury, DamageInfo dinfo, DamageResult result)
        {
            injury.TryGetComp<HediffComp_GetsPermanent>()?.PreFinalizeInjury();
            float partHealth = pawn.health.hediffSet.GetPartHealth(injury.Part);
            pawn.health.AddHediff(injury, null, dinfo, result);
            float num3 = Mathf.Min(injury.Severity, partHealth);
            result.totalDamageDealt += num3;
            result.wounded = true;
            result.AddPart(pawn, injury.Part);
            result.AddHediff(injury);
            return num3;
        }
        private void CheckDuplicateDamageToOuterParts(DamageInfo dinfo, Pawn pawn, float totalDamage, DamageResult result)
        {
            if (!dinfo.AllowDamagePropagation || !dinfo.Def.harmAllLayersUntilOutside || dinfo.HitPart.depth != BodyPartDepth.Inside) return;
            BodyPartRecord parent = dinfo.HitPart.parent;
            do
            {
                if (parent == null) break;
                if (pawn.health.hediffSet.GetPartHealth(parent) > 0f && parent.coverageAbs > 0f)
                {
                    Hediff_Injury hediff_Injury = (Hediff_Injury)HediffMaker.MakeHediff(HealthUtility.GetHediffDefFromDamage(dinfo.Def, pawn, parent), pawn);
                    hediff_Injury.Part = parent;
                    hediff_Injury.sourceDef = dinfo.Weapon;
                    hediff_Injury.sourceBodyPartGroup = dinfo.WeaponBodyPartGroup;
                    hediff_Injury.Severity = totalDamage;
                    if (hediff_Injury.Severity <= 0f) hediff_Injury.Severity = 1f;
                    FinalizeAndAddInjury(pawn, hediff_Injury, dinfo, result);
                }
                if (parent.depth == BodyPartDepth.Outside) break;
                parent = parent.parent;
            }
            while (true);
        }
        private static bool IsHeadshot(DamageInfo dinfo, Pawn pawn)
        {
            if (dinfo.InstantPermanentInjury) return false;
            bool vitalHit = dinfo.HitPart.groups.Contains(BodyPartGroupDefOf.FullHead) ||
                              dinfo.HitPart.groups.Contains(BodyPartGroupDefOf.Torso) ||
                              dinfo.HitPart.IsCorePart;
            return dinfo.Def.isRanged && vitalHit;
        }
        private BodyPartRecord GetExactPartFromDamageInfo(DamageInfo dinfo, Pawn pawn)
        {
            if (dinfo.HitPart != null)
            {
                if (!pawn.health.hediffSet.GetNotMissingParts().Any(x => x == dinfo.HitPart)) return null;
                return dinfo.HitPart;
            }
            BodyPartRecord bodyPartRecord = ChooseHitPart(dinfo, pawn);
            if (bodyPartRecord == null) Log.Warning("ChooseHitPart returned null (any part).");
            return bodyPartRecord;
        }
        protected virtual BodyPartRecord ChooseHitPart(DamageInfo dinfo, Pawn pawn)
        {
            return pawn.health.hediffSet.GetRandomNotMissingPart(dinfo.Def, dinfo.Height, dinfo.Depth);
        }

        private static void PlayWoundedVoiceSound(DamageInfo dinfo, Pawn pawn)
        {
            if (!pawn.Dead && !dinfo.InstantPermanentInjury &&
                pawn.SpawnedOrAnyParentSpawned &&
                dinfo.Def.ExternalViolenceFor(pawn)) LifeStageUtility.PlayNearestLifestageSound(pawn, lifeStage => lifeStage.soundWounded, gene => gene.soundWounded, mutantDef => mutantDef.soundWounded);
        }
        protected float ReduceDamageToPreserveOutsideParts(float postArmorDamage, DamageInfo dinfo, Pawn pawn)
        {
            return postArmorDamage;
        }
    }
}
