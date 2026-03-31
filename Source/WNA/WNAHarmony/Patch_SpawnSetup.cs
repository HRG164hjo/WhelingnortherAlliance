using HarmonyLib;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNAHarmony
{
    public class Patch_SpawnSetup
    {
        [HarmonyPatch(typeof(Pawn), "SpawnSetup")]
        public static class Patch_Pawn_SpawnSetup
        {
            public static void Postfix(Pawn __instance)
            {
                if (__instance.Faction?.def == WNAMainDefOf.WNA_FactionPCC)
                {
                    if (!__instance.health.hediffSet.HasHediff(WNAMainDefOf.WNA_PermaconstActive))
                        __instance.health.AddHediff(WNAMainDefOf.WNA_PermaconstActive);
                }
            }
        }
    }
}
