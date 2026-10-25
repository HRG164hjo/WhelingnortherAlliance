using HarmonyLib;
using Verse;
using WNA.WNADefOf;
using WNA.WNAUtility;

namespace WNA.WNAHarmony
{
    internal class Patch_SpawnSetup
    {
        [HarmonyPatch(typeof(Pawn), "SpawnSetup")]
        internal static class Patch_Pawn_SpawnSetup
        {
            internal static void Postfix(Pawn __instance)
            {
                if (__instance.Faction?.def == WNAMainDefOf.WNA_FactionPCC)
                    WNAPermaconstUtility.TryActivate(__instance);
            }
        }
    }
}
