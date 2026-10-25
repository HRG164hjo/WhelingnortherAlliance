using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using WNA.WNAUtility;

namespace WNA.WNAHarmony
{
    internal static class Patch_MindReading
    {
        [HarmonyPatch(typeof(PawnUtility), nameof(PawnUtility.ShouldDisplayJobReport))]
        internal static class Patch_PawnUtility_ShouldDisplayJobReport
        {
            internal static void Postfix(Pawn pawn, ref bool __result)
            {
                if (__result || !WNAPsychicDawnUtility.MindReadingActive)
                    return;
                if (!WNAPsychicDawnUtility.IsMindReadingTarget(pawn))
                    return;
                if (pawn.InMentalState || (pawn.IsMutant && pawn.mutant.Def.overrideInspectString))
                    return;
                __result = true;
            }
        }

        [HarmonyPatch(typeof(Pawn), nameof(Pawn.DrawExtraSelectionOverlays))]
        internal static class Patch_Pawn_DrawExtraSelectionOverlays
        {
            internal static void Postfix(Pawn __instance)
            {
                if (!WNAPsychicDawnUtility.MindReadingActive
                    || !WNAPsychicDawnUtility.IsMindReadingTarget(__instance))
                    return;
                if (__instance.Map == null)
                    return;

                Pawn_PathFollower pather = __instance.pather;
                pather?.curPath?.DrawPath(__instance);
                __instance.jobs?.DrawLinesBetweenTargets();
                if (pather != null && pather.Moving && pather.Destination.IsValid)
                    GenDraw.DrawTargetHighlight(pather.Destination);
            }
        }
    }
}
