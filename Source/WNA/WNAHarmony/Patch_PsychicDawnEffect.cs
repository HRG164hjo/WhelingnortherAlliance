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
                TerrainDef terrain = __instance.Position.GetTerrain(__instance.Map);
                Building building = __instance.Position.GetEdifice(__instance.Map);
                float fertility = terrain?.fertility ?? 0f;
                if (building != null)
                    fertility += building?.def?.fertility ?? 0f;
                __result = Mathf.Max(1f, fertility * 2f);
                return false;
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
