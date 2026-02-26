using HarmonyLib;
using RimWorld;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNAHarmony
{
    public class Patch_PawnGenerator
    {
        [HarmonyPatch(typeof(PawnGenerator), "PostProcessGeneratedGear")]
        public static class PostProcessGeneratedGearPatch
        {
            public static void Postfix(Thing gear, Pawn pawn)
            {
                CompQuality compQuality = gear.TryGetComp<CompQuality>();
                if (compQuality != null && pawn.Faction != null)
                {
                    if (pawn.Faction.def == WNAMainDefOf.WNA_FactionWNA)
                    {
                        QualityCategory currentQuality = compQuality.Quality;
                        if (currentQuality < QualityCategory.Legendary) compQuality.SetQuality(QualityCategory.Legendary, ArtGenerationContext.Outsider);
                    }
                }
            }
        }
    }
}
