using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using WNA.WNAHediffClass;
using WNA.WNAMiscs;
using WNA.WNAModExtension;

namespace WNA.WNAUtility
{
    internal static class AttachEffectUtility
    {
        [System.ThreadStatic]
        internal static bool applyingSelfDoT;

        internal static bool BlocksAttack(Pawn pawn)
        {
            List<Hediff> hediffs = pawn?.health?.hediffSet?.hediffs;
            if (hediffs == null)
                return false;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i] is HdfClass_AttachEffect ae && ae.Config.disableAttack)
                    return true;
            }
            return false;
        }

        internal static bool BlocksAbility(Pawn pawn)
        {
            List<Hediff> hediffs = pawn?.health?.hediffSet?.hediffs;
            if (hediffs == null)
                return false;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i] is HdfClass_AttachEffect ae && ae.Config.disableAbility)
                    return true;
            }
            return false;
        }

        internal static bool BlocksVerb(Verb verb)
        {
            Pawn pawn = verb?.CasterPawn;
            if (pawn == null)
                return false;
            if (verb is Verb_CastAbility cast)
                return cast.ability != null && BlocksAbility(pawn);
            return BlocksAttack(pawn);
        }

        internal static void TryAbsorb(Pawn pawn, ref DamageInfo dinfo, ref bool absorbed)
        {
            if (absorbed || applyingSelfDoT)
                return;
            List<Hediff> hediffs = pawn?.health?.hediffSet?.hediffs;
            if (hediffs == null || dinfo.Amount <= 0f || dinfo.Def == null || dinfo.Def.ignoreShields)
                return;
            bool hasShield = false;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i] is HdfClass_AttachEffect probe && probe.Config.HasShield)
                {
                    hasShield = true;
                    break;
                }
            }
            if (!hasShield)
                return;

            // Breaking a layer can remove the hediff itself, so iterate a snapshot.
            List<Hediff> snapshot = new List<Hediff>(hediffs);
            List<HdfClass_AttachEffect> touched = null;
            float remaining = dinfo.Amount;
            for (int i = 0; i < snapshot.Count && remaining > 0f; i++)
            {
                if (!(snapshot[i] is HdfClass_AttachEffect ae))
                    continue;
                AttachEffectConfig cfg = ae.Config;
                if (!cfg.HasShield)
                    continue;
                if (!cfg.absorbMelee && !dinfo.Def.isRanged && !dinfo.Def.isExplosive)
                    continue;
                while (remaining > 0f && ae.TryGetOutermostIntactLayer(out AttachEffectShieldLayer layer))
                {
                    float cost = cfg.shieldAbsorbMode == AttachEffectShieldAbsorbMode.ByDamage
                        ? remaining
                        : Mathf.Max(0.0001f, cfg.shieldHitCost);
                    if (layer.energy >= cost)
                    {
                        layer.energy -= cost;
                        remaining = 0f;
                        ae.NotifyAbsorbed(dinfo);
                        break;
                    }
                    if (cfg.absorbOverDamage)
                    {
                        remaining = 0f;
                        ae.NotifyAbsorbed(dinfo);
                        ae.BreakLayer(layer);
                        break;
                    }
                    remaining *= 1f - Mathf.Clamp01(layer.energy / cost);
                    ae.NotifyAbsorbed(dinfo);
                    ae.BreakLayer(layer);
                }
                if (touched == null)
                    touched = new List<HdfClass_AttachEffect>();
                touched.Add(ae);
            }
            if (touched != null)
            {
                for (int i = 0; i < touched.Count; i++)
                    touched[i].ResolveInstantRespawn();
            }
            if (remaining <= 0f)
                absorbed = true;
            else if (remaining < dinfo.Amount)
                dinfo.SetAmount(remaining);
        }
    }
}