using HarmonyLib;
using System;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using WNA.WNAModExtension;
using WNA.WNAUtility;

namespace WNA.WNAHarmony
{
    internal class Patch_Passability
    {
        internal static class PawnCheck
        {
            internal static bool IsValidPawn(Pawn pawn)
            {
                if (pawn == null || pawn.Faction == null)
                    return false;
                if (General.IsWNARace(pawn))
                    return true;
                if (pawn.def?.GetModExtension<TechnoConfig>()?.canFreePass == true)
                    return true;
                if (pawn.health?.hediffSet?.hediffs?.Any(h =>
                    h.def?.GetModExtension<TechnoConfig>()?.canFreePass == true) == true)
                    return true;
                if (General.PawnHasIdeo(pawn) && General.IsInternalFaction(pawn.Faction))
                    return true;
                return false;
            }
        }
        internal static class LocomotionUtility
        {
            internal static float ApplyLocomotionUrgency(Pawn pawn, float baseTicks)
            {
                if (pawn?.CurJob == null)
                    return baseTicks;

                switch (pawn.CurJob.locomotionUrgency)
                {
                    case LocomotionUrgency.Amble:
                        baseTicks *= 3f;
                        if (baseTicks < 60f) baseTicks = 60f;
                        break;
                    case LocomotionUrgency.Walk:
                        baseTicks *= 2f;
                        if (baseTicks < 50f) baseTicks = 50f;
                        break;
                    case LocomotionUrgency.Sprint:
                        baseTicks = Mathf.RoundToInt(baseTicks * 0.75f);
                        break;
                }
                return baseTicks;
            }
        }
        [HarmonyPatch(typeof(Pawn_PathFollower))]
        [HarmonyPatch("CostToMoveIntoCell")]
        [HarmonyPatch([typeof(Pawn), typeof(IntVec3)])]
        internal static class Patch_PathFollower
        {
            [HarmonyPostfix]
            internal static void AllowFreeTerrainTraversal(Pawn pawn, IntVec3 c, ref float __result)
            {
                try
                {
                    if (!PawnCheck.IsValidPawn(pawn))
                        return;
                    if (pawn.Map == null || !c.InBounds(pawn.Map))
                        return;
                    float baseTicks = (c.x != pawn.Position.x && c.z != pawn.Position.z)
                        ? pawn.TicksPerMoveDiagonal
                        : pawn.TicksPerMoveCardinal;
                    float moveTicks = LocomotionUtility.ApplyLocomotionUrgency(pawn, baseTicks);
                    __result = Mathf.Max(moveTicks, 1f);
                }
                catch (Exception ex)
                {
                    Log.Error($"[WNA] Patch_Passability error: {ex}");
                }
            }
        }
    }
}
