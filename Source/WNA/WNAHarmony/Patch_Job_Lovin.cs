using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using WNA.WNADefOf;

namespace WNA.WNAHarmony
{
    public class Patch_Job_Lovin
    {
        [HarmonyPatch(typeof(JobDriver_Lovin), "GenerateRandomMinTicksToNextLovin")]
        public static class Patch_GenerateRandomMinTicksToNextLovin
        {
            static void Postfix(ref int __result, Pawn pawn)
            {
                if (pawn == null || pawn.Ideo == null)
                    return;
                Pawn partner = GetPartnerForLovin(pawn);
                if (partner == null || partner.Ideo == null)
                    return;
                PreceptDef node = WNAMainDefOf.WNA_P_Proselyte;
                if (node == null)
                    return;
                bool pawnHas = pawn.Ideo.HasPrecept(node);
                bool partnerHas = partner.Ideo.HasPrecept(node);
                if (pawnHas && partnerHas)
                {
                    __result *= 12;
                    return;
                }
                if (pawnHas != partnerHas)
                {
                    __result = int.MaxValue;
                    return;
                }
            }
            static Pawn GetPartnerForLovin(Pawn pawn)
            {
                Job cur = pawn.jobs?.curJob;
                if (cur == null)
                    return null;
                Thing target = cur.GetTarget(TargetIndex.A).Thing;
                return target as Pawn;
            }
        }
    }
}
