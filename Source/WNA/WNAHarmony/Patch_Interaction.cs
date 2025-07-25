using HarmonyLib;
using RimWorld;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNAHarmony
{
    public class Patch_Interaction
    {
        [HarmonyPatch(typeof(InteractionWorker_Breakup), "RandomSelectionWeight")]
        public class Patch_Breakup
        {
            [HarmonyPostfix]
            private static void PostFix(ref float __result, Pawn initiator)
            {
                if (initiator.Ideo?.HasPrecept(WNAMainDefOf.WNA_P_Proselyte) == true)
                {
                    __result = 0f;
                }
            }
        }
        [HarmonyPatch(typeof(InteractionWorker_ConvertIdeoAttempt), "RandomSelectionWeight")]
        public class Patch_ConvertIdeoAttempt
        {
            [HarmonyPostfix]
            private static void PostFix(ref float __result, Pawn initiator, Pawn recipient)
            {
                if (__result != 0f && initiator.Ideo != null && recipient.Ideo != null)
                {
                    if (initiator.Ideo.HasPrecept(WNAMainDefOf.WNA_P_Proselyte) && recipient.Ideo.HasPrecept(WNAMainDefOf.WNA_P_Proselyte))
                    {
                        __result = 0f;
                    }
                    else if (initiator.Ideo.HasPrecept(WNAMainDefOf.WNA_P_Proselyte) && !recipient.Ideo.HasPrecept(WNAMainDefOf.WNA_P_Proselyte))
                    {
                        __result *= 1000f;
                    }
                }
            }
        }
        [HarmonyPatch(typeof(InteractionWorker_DeepTalk), "RandomSelectionWeight")]
        public class Patch_Interaction_DeepTalk
        {
            [HarmonyPostfix]
            private static void PostFix(ref float __result, Pawn initiator)
            {
                if (initiator.Ideo?.HasPrecept(WNAMainDefOf.WNA_P_Proselyte) == true)
                {
                    __result = 1f;
                }
            }
        }
        [HarmonyPatch(typeof(InteractionWorker_InhumanRambling), "RandomSelectionWeight")]
        public class Patch_Interaction_InhumanRambling
        {
            [HarmonyPostfix]
            private static void PostFix(ref float __result, Pawn initiator)
            {
                if (initiator.Ideo?.HasPrecept(WNAMainDefOf.WNA_P_Proselyte) == true)
                {
                    __result = 0f;
                }
            }
        }
        [HarmonyPatch(typeof(InteractionWorker_Insult), "RandomSelectionWeight")]
        public class Patch_Interaction_Insult
        {
            [HarmonyPostfix]
            private static void PostFix(ref float __result, Pawn initiator)
            {
                if (initiator.Ideo?.HasPrecept(WNAMainDefOf.WNA_P_Proselyte) == true)
                {
                    __result = 0f;
                }
            }
        }
        [HarmonyPatch(typeof(InteractionWorker_KindWords), "RandomSelectionWeight")]
        public class Patch_Interaction_KindWords
        {
            [HarmonyPostfix]
            private static void PostFix(ref float __result, Pawn initiator)
            {
                if (initiator.Ideo?.HasPrecept(WNAMainDefOf.WNA_P_Proselyte) == true)
                {
                    __result = 0.5f;
                }
            }
        }
        [HarmonyPatch(typeof(InteractionWorker_MarriageProposal), "RandomSelectionWeight")]
        public class Patch_Interaction_MarriageProposal
        {
            [HarmonyPostfix]
            private static void PostFix(ref float __result, Pawn initiator)
            {
                if (initiator.Ideo?.HasPrecept(WNAMainDefOf.WNA_P_Proselyte) == true)
                {
                    __result = 0f;
                }
            }
        }
        [HarmonyPatch(typeof(InteractionWorker_RomanceAttempt), "RandomSelectionWeight")]
        public class Patch_Interaction_RomanceAttempt
        {
            [HarmonyPostfix]
            private static void PostFix(ref float __result, Pawn initiator)
            {
                if (initiator.Ideo?.HasPrecept(WNAMainDefOf.WNA_P_Proselyte) == true)
                {
                    __result = 0f;
                }
            }
        }
        [HarmonyPatch(typeof(InteractionWorker_Slight), "RandomSelectionWeight")]
        public class Patch_Interaction_Slight
        {
            [HarmonyPostfix]
            private static void PostFix(ref float __result, Pawn initiator)
            {
                if (initiator.Ideo?.HasPrecept(WNAMainDefOf.WNA_P_Proselyte) == true)
                {
                    __result = 0f;
                }
            }
        }
    }
}
