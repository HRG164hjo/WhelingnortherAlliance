using HarmonyLib;
using RimWorld;
using Verse;
using WNA.WNAUtility;

namespace WNA.WNAHarmony
{
    public class Patch_RecruitUtility
    {
        [HarmonyPatch(typeof(RecruitUtility), nameof(RecruitUtility.Recruit))]
        public static class Patch_RecruitUtility_Recruit
        {
            static bool Prefix(Pawn pawn, Faction faction, Pawn recruiter = null)
            {
                if (!MindControlUtility.MindControlled(pawn)) return true;
                return false;
            }
        }
    }
}
