using HarmonyLib;
using RimWorld;
using Verse;
using WNA.WNAUtility;

namespace WNA.WNAHarmony
{
    public class Patch_GenGuest
    {
        [HarmonyPatch(typeof(GenGuest), nameof(GenGuest.TryEnslavePrisoner))]
        public static class Patch_GenGuest_TryEnslavePrisoner
        {
            static bool Prefix(Pawn warden, Pawn prisoner, ref bool __result)
            {
                if (!MindControlUtility.MindControlled(prisoner)) return true;
                __result = false;
                return false;
            }
        }
    }
}
