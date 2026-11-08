using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using WNA.WNADefOf;
using WNA.WNAUtility;

namespace WNA.WNAHarmony
{
    internal static class Patch_PsychicDawnEffect
    {
        [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
        internal static class Patch_Thing_TakeDamage
        {
            internal static bool Prefix(Thing __instance, ref DamageWorker.DamageResult __result)
            {
                if (!WNAPsychicDawnUtility.IsProtected(__instance))
                    return true;
                WNAPsychicDawnUtility.HealToFull(__instance);
                __result = new DamageWorker.DamageResult();
                return false;
            }
        }

        [HarmonyPatch(typeof(Plant), nameof(Plant.GrowthRate), MethodType.Getter)]
        internal static class Patch_Plant_GrowthRate
        {
            internal static bool Prefix(Plant __instance, ref float __result)
            {
                if (!WNAPsychicDawnUtility.Active || !WNAPsychicDawnUtility.IsInGrowingZone(__instance))
                    return true;
                __result += WNAPsychicDawnUtility.GrowthMultiplierFor(__instance);
                return false;
            }
        }

        // Subclasses may override the virtual GrowthRate getter, which bypasses the base patch.
        // GrowthPerTick is non-virtual, so patching it covers every Plant subclass using vanilla growth.
        [HarmonyPatch]
        internal static class Patch_Plant_GrowthPerTick
        {
            private static readonly System.Reflection.PropertyInfo RestingProperty =
                typeof(Plant).GetProperty("Resting",
                    System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.Public);

            internal static System.Reflection.MethodBase TargetMethod()
            {
                return AccessTools.PropertyGetter(typeof(Plant), "GrowthPerTick");
            }

            internal static void Postfix(Plant __instance, ref float __result)
            {
                if (!WNAPsychicDawnUtility.Active || !WNAPsychicDawnUtility.IsInGrowingZone(__instance))
                    return;
                // A zero GrowthRate from subclass overrides (e.g. VEF fertility restrictions) must not block the effect,
                // so the state checks of the vanilla getter are mirrored here instead of trusting __result.
                if (__instance.LifeStage != PlantLifeStage.Growing || __instance.Blighted)
                    return;
                if (RestingProperty != null && (bool)RestingProperty.GetValue(__instance, null))
                    return;
                if (!__instance.def.plant.vacuumResistant && __instance.Position.GetVacuum(__instance.Map) >= 0.5f)
                    return;
                float growDays = __instance.def?.plant?.growDays ?? 0f;
                if (growDays <= 0f)
                    return;
                __result = 1f / (60000f * growDays) * WNAPsychicDawnUtility.GrowthMultiplierFor(__instance);
            }
        }

        [HarmonyPatch(typeof(PollutionGrid), nameof(PollutionGrid.SetPolluted))]
        internal static class Patch_PollutionGrid_SetPolluted
        {
            private static readonly AccessTools.FieldRef<PollutionGrid, Map> MapField =
                AccessTools.FieldRefAccess<PollutionGrid, Map>("map");

            internal static bool Prefix(PollutionGrid __instance, IntVec3 cell, bool isPolluted)
            {
                if (!isPolluted)
                    return true;
                Map map = MapField(__instance);
                return !WNAPsychicDawnUtility.IsInPollutionClearArea(map, cell);
            }
        }

        [HarmonyPatch(typeof(Area), "Set")]
        internal static class Patch_Area_Set
        {
            internal static void Postfix(Area __instance, IntVec3 c, bool val)
            {
                if (!val || !(__instance is Area_PollutionClear))
                    return;
                WNAPsychicDawnUtility.ClearPollutionAt(__instance.Map, c);
            }
        }

        [HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.FinishProject))]
        internal static class Patch_ResearchManager_FinishProject
        {
            internal static void Postfix(ResearchProjectDef proj)
            {
                if (proj == WNAMainDefOf.WNA_PsychicDawn)
                    WNAPsychicDawnUtility.OnResearchCompleted();
            }
        }

        [HarmonyPatch(typeof(ContainmentUtility), nameof(ContainmentUtility.InitiateEscapeMtbDays))]
        internal static class Patch_ContainmentUtility_InitiateEscapeMtbDays
        {
            internal static void Postfix(Pawn pawn, ref float __result)
            {
                if (WNAPsychicDawnUtility.Active && pawn != null && pawn.IsEntity)
                    __result = -1f;
            }
        }

        [HarmonyPatch(typeof(CompHoldingPlatformTarget), nameof(CompHoldingPlatformTarget.Escape))]
        internal static class Patch_CompHoldingPlatformTarget_Escape
        {
            internal static void Postfix(CompHoldingPlatformTarget __instance, bool initiator)
            {
                if (!initiator || !WNAPsychicDawnUtility.Active)
                    return;
                if (__instance.parent is Pawn pawn && pawn.IsEntity)
                    WNAPsychicDawnUtility.ApplyCatatonicBreakdown(pawn);
            }
        }
    }
}
