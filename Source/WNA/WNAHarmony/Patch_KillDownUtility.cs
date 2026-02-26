using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using WNA.DMExtension;
using static Verse.PawnCapacityUtility;

namespace WNA.WNAHarmony
{
    public class Patch_KillDownUtility
    {
        private static readonly HashSet<string> races = new HashSet<string>
        {
            "WNA_WNThan",
            "WNA_Human"
        };
        private static bool IsTargetRace(Pawn pawn)
        {
            return pawn?.def != null && races.Contains(pawn.def.defName);
        }
        private static float GetHealthPercentage(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null) return 1f;
            float currentHealth = pawn.health.hediffSet.GetNotMissingParts().Sum(x => x.def.GetMaxHealth(pawn));
            float maxHealth = pawn.def.race.body.AllParts.Sum(x => x.def.GetMaxHealth(pawn));
            return maxHealth > 0 ? currentHealth / maxHealth : 1f;
        }
        [HarmonyPatch(typeof(Pawn_HealthTracker), "ShouldBeDowned")]
        public static class Patch_ShouldBeDowned
        {
            [HarmonyPrefix]
            public static bool Prefix(Pawn ___pawn, ref bool __result)
            {
                if (IsTargetRace(___pawn))
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(Pawn), "Kill")]
        public static class Patch_Kill
        {
            [HarmonyPrefix]
            public static bool Prefix(Pawn __instance, DamageInfo? dinfo)
            {
                if (IsTargetRace(__instance))
                {
                    var extension = __instance.def.GetModExtension<KillDownUtility>();
                    float threshold = (extension?.canDieThreshold ?? 5) / 100f;
                    if (GetHealthPercentage(__instance) > threshold) return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(PawnCapacityUtility), "CalculateCapacityLevel")]
        public static class Patch_PawnCapacityUtility
        {
            [HarmonyPostfix]
            public static void Postfix(ref float __result, HediffSet diffSet, PawnCapacityDef capacity, List<CapacityImpactor> impactors = null, bool forTradePrice = false)
            {
                if (__result <= 0f || diffSet?.pawn == null) return;
                var pawnDef = diffSet.pawn.def;
                if (pawnDef == null) return;
                var extension = pawnDef.GetModExtension<KillDownUtility>();
                if (extension?.minCapacities == null) return;
                if (extension.minCapacities.Contains(capacity))
                {
                    __result = Mathf.Max(__result, extension.minLevel);
                }
            }
        }
    }
}
