using HarmonyLib;
using RimWorld;
using Verse;
using WNA.WNAUtility;

namespace WNA.WNAHarmony
{
    public class Patch_Pawn_IdeoTracker
    {
        [HarmonyPatch(typeof(Pawn_IdeoTracker), nameof(Pawn_IdeoTracker.IdeoConversionAttempt))]
        public static class Patch_PawnIdeo_IdeoConversionAttempt
        {
            static readonly AccessTools.FieldRef<Pawn_IdeoTracker, Pawn> PawnRef =
                AccessTools.FieldRefAccess<Pawn_IdeoTracker, Pawn>("pawn");
            static bool Prefix(Pawn_IdeoTracker __instance, ref bool __result)
            {
                var pawn = PawnRef(__instance);
                if (!MindControlUtility.MindControlled(pawn)) return true;
                __result = false;
                return false;
            }
        }
        [HarmonyPatch(typeof(Pawn_IdeoTracker), nameof(Pawn_IdeoTracker.SetIdeo))]
        public static class Patch_PawnIdeo_SetIdeo
        {
            static readonly AccessTools.FieldRef<Pawn_IdeoTracker, Pawn> PawnRef =
                AccessTools.FieldRefAccess<Pawn_IdeoTracker, Pawn>("pawn");
            static bool Prefix(Pawn_IdeoTracker __instance, Ideo ideo)
            {
                var pawn = PawnRef(__instance);
                if (pawn == null) return true;
                if (Current.ProgramState != ProgramState.Playing || PawnGenerator.IsBeingGenerated(pawn))
                    return true;
                if (MindControlUtility.MindControlled(pawn) && pawn.Ideo != ideo)
                    return false;
                return true;
            }
        }
    }
}
