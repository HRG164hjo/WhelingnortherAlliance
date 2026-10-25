using HarmonyLib;
using RimWorld;
using Verse;
using WNA.WNAUtility;

namespace WNA.WNAHarmony
{
    internal class Patch_SocialInteraction
    {
        internal class Patch_Interaction
        {
            [HarmonyPatch(typeof(InteractionWorker_Breakup), "RandomSelectionWeight")]
            internal class Patch_Breakup
            {
                [HarmonyPostfix]
                private static void PostFix(ref float __result, Pawn initiator)
                {
                    if (General.PawnHasIdeo(initiator))
                        __result = 0f;
                }
            }
            [HarmonyPatch(typeof(InteractionWorker_ConvertIdeoAttempt), "RandomSelectionWeight")]
            internal class Patch_ConvertIdeoAttempt
            {
                [HarmonyPostfix]
                private static void PostFix(ref float __result, Pawn initiator, Pawn recipient)
                {
                    if (__result != 0f && initiator.Ideo != null && recipient.Ideo != null)
                    {
                        bool init = General.PawnHasIdeo(initiator);
                        bool reci = General.PawnHasIdeo(recipient);
                        if (init && reci) __result = 0f;
                        else if (init && !reci) __result *= 9999f;
                    }
                    if (WNAMindControlUtility.MindControlled(recipient))
                        __result = 0f;
                }
            }
            [HarmonyPatch(typeof(InteractionWorker_DeepTalk), "RandomSelectionWeight")]
            internal class Patch_Interaction_DeepTalk
            {
                [HarmonyPostfix]
                private static void PostFix(ref float __result, Pawn initiator)
                {
                    if (General.PawnHasIdeo(initiator))
                        __result = 1f;
                }
            }
            [HarmonyPatch(typeof(InteractionWorker_InhumanRambling), "RandomSelectionWeight")]
            internal class Patch_Interaction_InhumanRambling
            {
                [HarmonyPostfix]
                private static void PostFix(ref float __result, Pawn initiator)
                {
                    if (General.PawnHasIdeo(initiator)) __result = 0f;
                }
            }
            [HarmonyPatch(typeof(InteractionWorker_Insult), "RandomSelectionWeight")]
            internal class Patch_Interaction_Insult
            {
                [HarmonyPostfix]
                private static void PostFix(ref float __result, Pawn initiator)
                {
                    if (General.PawnHasIdeo(initiator)) __result = 0f;
                }
            }
            [HarmonyPatch(typeof(InteractionWorker_KindWords), "RandomSelectionWeight")]
            internal class Patch_Interaction_KindWords
            {
                [HarmonyPostfix]
                private static void PostFix(ref float __result, Pawn initiator)
                {
                    if (General.PawnHasIdeo(initiator)) __result = 1f;
                }
            }
            [HarmonyPatch(typeof(InteractionWorker_MarriageProposal), "RandomSelectionWeight")]
            internal class Patch_Interaction_MarriageProposal
            {
                [HarmonyPostfix]
                private static void PostFix(ref float __result, Pawn initiator)
                {
                    if (General.PawnHasIdeo(initiator)) __result = 0f;
                }
            }
            [HarmonyPatch(typeof(InteractionWorker_RomanceAttempt), "RandomSelectionWeight")]
            internal class Patch_Interaction_RomanceAttempt
            {
                [HarmonyPostfix]
                private static void PostFix(ref float __result, Pawn initiator)
                {
                    if (General.PawnHasIdeo(initiator)) __result = 0f;
                }
            }
            [HarmonyPatch(typeof(InteractionWorker_Slight), "RandomSelectionWeight")]
            internal class Patch_Interaction_Slight
            {
                [HarmonyPostfix]
                private static void PostFix(ref float __result, Pawn initiator)
                {
                    if (General.PawnHasIdeo(initiator)) __result = 0f;
                }
            }
        }
    }
}
