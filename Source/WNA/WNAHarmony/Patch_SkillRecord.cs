using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNAHarmony
{
    [HarmonyPatch(typeof(SkillRecord), "Interval")]
    public class Patch_SkillRecord
    {
        private const float expAdd = 100000000f;
        private const int MaxLevelLimit = 9999;
        private static readonly HashSet<string> wiseList = new HashSet<string>
        {
            "WNA_WNThan",
            "WNA_Human"
        };
        [HarmonyPrefix]
        private static bool Prefix(SkillRecord __instance)
        {
            Pawn pawn = __instance.Pawn;
            if (IsCertainPawn(pawn))
            {
                if (__instance.levelInt >= MaxLevelLimit)
                {
                    __instance.levelInt = MaxLevelLimit;
                    return false;
                }
                __instance.Learn(expAdd, direct: true, ignoreLearnRate: true);
                if (__instance.levelInt > MaxLevelLimit)
                    __instance.levelInt = MaxLevelLimit;
                return false;
            }
            return true;
        }
        private static bool IsCertainPawn(Pawn pawn)
        {
            if (pawn == null)
                return false;
            bool isRace = wiseList.Contains(pawn.def.defName);
            bool hasPrecept = pawn.Ideo?.HasPrecept(WNAMainDefOf.WNA_P_Proselyte) == true;
            return isRace || hasPrecept;
        }
    }
}
