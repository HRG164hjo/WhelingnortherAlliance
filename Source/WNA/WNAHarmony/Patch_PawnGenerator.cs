using HarmonyLib;
using RimWorld;
using Verse;
using WNA.WNAUtility;

namespace WNA.WNAHarmony
{
    internal class Patch_PawnGenerator
    {
        [HarmonyPatch(typeof(PawnGenerator), "PostProcessGeneratedGear")]
        internal static class PostProcessGeneratedGearPatch
        {
            internal static void Postfix(Thing gear, Pawn pawn)
            {
                CompQuality compQuality = gear.TryGetComp<CompQuality>();
                if (compQuality != null && pawn.Faction != null)
                {
                    if (pawn.Faction == General.WNAFaction || (pawn.kindDef == PawnKindDefOf.AncientSoldier || pawn.kindDef == PawnKindDefOf.AncientSoldier_Leader))
                    {
                        QualityCategory currentQuality = compQuality.Quality;
                        if (currentQuality < QualityCategory.Legendary) compQuality.SetQuality(QualityCategory.Legendary, ArtGenerationContext.Outsider);
                    }
                }
            }
        }
    }
}
