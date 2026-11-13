using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using WNA.WNADefOf;
using WNA.WNAHediffClass;
using WNA.WNAModExtension;
using WNA.WNAThingClass;
using WNA.WNAUtility;
using static Verse.PawnCapacityUtility;

namespace WNA.WNAHarmony
{
    internal class WNA_KillDownUtility
    {
        private static float GetHealthPercentage(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null) return 1f;
            float currentHealth = pawn.health.hediffSet.GetNotMissingParts().Sum(x => x.def.GetMaxHealth(pawn));
            float maxHealth = pawn.def.race.body.AllParts.Sum(x => x.def.GetMaxHealth(pawn));
            return maxHealth > 0 ? currentHealth / maxHealth : 1f;
        }
        [HarmonyPatch(typeof(Pawn_HealthTracker), "ShouldBeDowned")]
        internal static class Patch_ShouldBeDowned
        {
            [HarmonyPrefix]
            internal static bool Prefix(Pawn ___pawn, ref bool __result)
            {
                if (General.IsWNARace(___pawn) || ___pawn is ThingClass_BattleUnit)
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(Pawn_HealthTracker), "ShouldBeDeadFromLethalDamageThreshold")]
        internal static class Patch_LethalDamageThreshold
        {
            [HarmonyPostfix]
            internal static void Postfix(ref bool __result)
            {
                __result = false;
            }
        }
        [HarmonyPatch(typeof(Pawn), "Kill")]
        internal static class Patch_Kill
        {
            [HarmonyPrefix]
            internal static bool Prefix(Pawn __instance, DamageInfo? dinfo)
            {
                if (General.IsWNARace(__instance))
                {
                    var extension = __instance.def.GetModExtension<KillDownUtility>();
                    float threshold = (extension?.canDieThreshold ?? 5) / 100f;
                    if (GetHealthPercentage(__instance) > threshold) return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(Pawn_HealthTracker), "AddHediff", new Type[] { typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult) })]
        internal static class Patch_AddHediff
        {
            [HarmonyPrefix]
            internal static bool Prefix(Hediff hediff, Pawn ___pawn)
            {
                if (!(___pawn is ThingClass_BattleUnit) || hediff == null)
                    return true;
                return hediff is Hediff_Injury
                    || hediff is HdfClass_AttachEffect
                    || hediff.def == HediffDefOf.MissingBodyPart
                    || hediff.def == WNAMainDefOf.WNA_MindControlEffect
                    || hediff.def == WNAMainDefOf.WNA_PermaconstActive;
            }
        }
        [HarmonyPatch(typeof(PawnCapacityUtility), "CalculateCapacityLevel")]
        internal static class Patch_PawnCapacityUtility
        {
            [HarmonyPostfix]
            internal static void Postfix(ref float __result, HediffSet diffSet, PawnCapacityDef capacity, List<CapacityImpactor> impactors = null, bool forTradePrice = false)
            {
                if (__result <= 0f || diffSet?.pawn == null) return;
                var pawnDef = diffSet.pawn.def;
                if (pawnDef == null) return;
                var extension = pawnDef.GetModExtension<KillDownUtility>();
                if (extension?.minCapacities == null) return;
                if (extension.minCapacities.TryGetLevel(capacity, out float minLevel))
                    __result = Mathf.Max(__result, minLevel);
            }
        }
    }
}
