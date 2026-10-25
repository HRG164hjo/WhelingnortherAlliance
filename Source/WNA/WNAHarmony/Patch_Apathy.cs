using HarmonyLib;
using RimWorld;
using Verse;
using WNA.WNAUtility;

namespace WNA.WNAHarmony
{
    internal class Patch_Apathy
    {
        [HarmonyPatch(typeof(ThoughtHandler), nameof(ThoughtHandler.MoodOffsetOfGroup))]
        internal class Patch_MoodOffsetOfGroup
        {
            [HarmonyPostfix]
            private static void PostFix(ThoughtHandler __instance, Thought group, ref float __result)
            {
                if (__result == 0f)
                    return;
                if (General.ApathyMoodNullified(__instance.pawn, group?.def))
                    __result = 0f;
            }
        }

        [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.OpinionOf))]
        internal class Patch_OpinionOf
        {
            private static readonly AccessTools.FieldRef<Pawn_RelationsTracker, Pawn> ownerRef =
                AccessTools.FieldRefAccess<Pawn_RelationsTracker, Pawn>("pawn");

            [HarmonyPostfix]
            private static void PostFix(Pawn_RelationsTracker __instance, ref int __result)
            {
                if (__result == 0)
                    return;
                if (General.HasApathyGene(ownerRef(__instance)))
                    __result = 0;
            }
        }
    }
}
