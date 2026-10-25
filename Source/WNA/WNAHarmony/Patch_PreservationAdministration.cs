using HarmonyLib;
using RimWorld;
using Verse;
using WNA.WNAUtility;

namespace WNA.WNAHarmony
{
    internal static class Patch_PreservationAdministration
    {
        private static readonly AccessTools.FieldRef<Need, Pawn> NeedPawnField =
            AccessTools.FieldRefAccess<Need, Pawn>("pawn");

        [HarmonyPatch(typeof(Need_Suppression), nameof(Need_Suppression.NeedInterval))]
        internal static class Patch_Need_Suppression_NeedInterval
        {
            internal static bool Prefix(Need_Suppression __instance)
            {
                if (!WNAPsychicDawnUtility.PreservationAdministrationActive)
                    return true;
                Pawn pawn = NeedPawnField(__instance);
                if (!WNAPsychicDawnUtility.IsPlayerSlave(pawn))
                    return true;
                __instance.CurLevel = __instance.MaxLevel;
                return false;
            }
        }

        [HarmonyPatch(typeof(Need_MechEnergy), nameof(Need_MechEnergy.NeedInterval))]
        internal static class Patch_Need_MechEnergy_NeedInterval
        {
            internal static bool Prefix(Need_MechEnergy __instance)
            {
                if (!WNAPsychicDawnUtility.PreservationAdministrationActive)
                    return true;
                Pawn pawn = NeedPawnField(__instance);
                if (!WNAPsychicDawnUtility.IsPlayerMech(pawn))
                    return true;
                __instance.CurLevel = __instance.MaxLevel;
                WNAPsychicDawnUtility.RemoveSelfShutdown(pawn);
                return false;
            }
        }

        [HarmonyPatch(typeof(SlaveRebellionUtility), nameof(SlaveRebellionUtility.CanParticipateInSlaveRebellion))]
        internal static class Patch_SlaveRebellionUtility_CanParticipate
        {
            internal static void Postfix(Pawn pawn, ref bool __result)
            {
                if (__result && WNAPsychicDawnUtility.PreservationAdministrationActive && WNAPsychicDawnUtility.IsPlayerSlave(pawn))
                    __result = false;
            }
        }

        [HarmonyPatch(typeof(SlaveRebellionUtility), nameof(SlaveRebellionUtility.InitiateSlaveRebellionMtbDays))]
        internal static class Patch_SlaveRebellionUtility_InitiateMtb
        {
            internal static void Postfix(Pawn pawn, ref float __result)
            {
                if (WNAPsychicDawnUtility.PreservationAdministrationActive && WNAPsychicDawnUtility.IsPlayerSlave(pawn))
                    __result = -1f;
            }
        }
    }
}
