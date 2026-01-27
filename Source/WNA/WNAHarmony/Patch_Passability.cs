using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using WNA.WNADefOf;

namespace WNA.WNAHarmony
{
    public static class PawnCheck
    {
        private static readonly HashSet<string> freeRaces = new HashSet<string>
        {
            "WNA_WNThan",
            "WNA_Human",
            "WNA_DimBoo",
            "WNA_ThornBoo",
            "WNA_FerosBoo"
        };
        private static readonly HashSet<string> freeHediffs = new HashSet<string>
        {
            "WNA_RobeBoost",
            "WNA_RobeBoostLite",
            "WNA_Inhuman",
            "WNA_InAnimal",
            "WNA_InMechanoid",
            "WNA_DeathEnd",
            "sWNA_DeathEnd"
        };
        public static bool IsValidPawn(Pawn pawn)
        {
            if (pawn == null || pawn.Faction == null)
                return false;
            if (pawn?.def != null && freeRaces.Contains(pawn.def.defName))
                return true;
            if (pawn.health?.hediffSet?.hediffs?.Any(h => h.def != null && freeHediffs.Contains(h.def.defName)) == true)
                return true;
            if (pawn.Ideo != null && pawn.Ideo.HasPrecept(WNAMainDefOf.WNA_P_Proselyte) && (pawn.Faction.def.defName == "WNA_FactionWNA" || (pawn.Faction == Faction.OfPlayer && WNAMainDefOf.WNA_TheEnlightment.IsFinished)))
                return true;
            return false;
        }
    }
    public static class LocomotionUtility
    {
        public static float ApplyLocomotionUrgency(Pawn pawn, float baseTicks)
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
    [HarmonyPatch(new Type[] { typeof(Pawn), typeof(IntVec3) })]
    public static class Patch_Passability
    {
        [HarmonyPostfix]
        public static void AllowFreeTerrainTraversal(Pawn pawn, IntVec3 c, ref float __result)
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
