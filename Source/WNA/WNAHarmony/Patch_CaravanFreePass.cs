using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using WNA.WNAUtility;

namespace WNA.WNAHarmony
{
    internal class Patch_CaravanFreePass
    {
        [HarmonyPatch(typeof(Caravan_PathFollower))]
        [HarmonyPatch("CostToMove", new Type[] { typeof(Caravan), typeof(PlanetTile), typeof(PlanetTile), typeof(int?) })]
        internal static class CostToMove_Caravan_Patch
        {
            [HarmonyPrefix]
            internal static void Enter(Caravan caravan)
            {
                CaravanFreePassUtility.Begin(CaravanFreePassUtility.IsMajority(caravan.PawnsListForReading));
            }

            [HarmonyFinalizer]
            internal static Exception Cleanup()
            {
                CaravanFreePassUtility.End();
                return null;
            }
        }

        [HarmonyPatch(typeof(Caravan_PathFollower))]
        [HarmonyPatch("CostToMove", new Type[] { typeof(int), typeof(PlanetTile), typeof(PlanetTile), typeof(int?), typeof(bool), typeof(StringBuilder), typeof(string), typeof(bool) })]
        internal static class CostToMove_Int_Patch
        {
            [HarmonyPrefix]
            internal static bool SkipTerrain(int caravanTicksPerMove, PlanetTile start, PlanetTile end,
                StringBuilder explanation, string caravanTicksPerMoveExplanation, bool immobile, ref int __result)
            {
                if (!CaravanFreePassUtility.IgnoresTerrain || start == end)
                    return true;

                if (explanation != null)
                {
                    explanation.Append(caravanTicksPerMoveExplanation);
                    explanation.AppendLine();
                }

                float terrainFactor = 1f;
                float roadFactor = Find.WorldGrid.GetRoadMovementDifficultyMultiplier(start, end, null);
                if (explanation != null && !immobile)
                {
                    explanation.AppendLine();
                    explanation.Append("TileMovementDifficulty".Translate() + ":");
                    explanation.AppendLine();
                    explanation.Append("  = " + (terrainFactor * roadFactor).ToString("0.#"));
                }

                int value = (int)((float)caravanTicksPerMove * terrainFactor * roadFactor);
                __result = Mathf.Clamp(value, 1, 30000);
                return false;
            }
        }

        [HarmonyPatch(typeof(CaravanArrivalTimeEstimator))]
        [HarmonyPatch("EstimatedTicksToArrive", new Type[] { typeof(PlanetTile), typeof(PlanetTile), typeof(Caravan) })]
        internal static class ArrivalTime_Caravan_Patch
        {
            [HarmonyPrefix]
            internal static void Enter(Caravan caravan)
            {
                CaravanFreePassUtility.Begin(CaravanFreePassUtility.IsMajority(caravan?.PawnsListForReading));
            }

            [HarmonyFinalizer]
            internal static Exception Cleanup()
            {
                CaravanFreePassUtility.End();
                return null;
            }
        }

        [HarmonyPatch(typeof(TilesPerDayCalculator))]
        [HarmonyPatch("ApproxTilesPerDay", new Type[] { typeof(Caravan), typeof(StringBuilder) })]
        internal static class TilesPerDay_Caravan_Patch
        {
            [HarmonyPrefix]
            internal static void Enter(Caravan caravan)
            {
                CaravanFreePassUtility.Begin(CaravanFreePassUtility.IsMajority(caravan?.PawnsListForReading));
            }

            [HarmonyFinalizer]
            internal static Exception Cleanup()
            {
                CaravanFreePassUtility.End();
                return null;
            }
        }

        [HarmonyPatch(typeof(TilesPerDayCalculator))]
        [HarmonyPatch("ApproxTilesPerDay", new Type[] { typeof(List<TransferableOneWay>), typeof(float), typeof(float), typeof(PlanetTile), typeof(PlanetTile), typeof(bool), typeof(StringBuilder) })]
        internal static class TilesPerDay_Transferables_Patch
        {
            [HarmonyPrefix]
            internal static void Enter(List<TransferableOneWay> transferables)
            {
                CaravanFreePassUtility.Begin(CaravanFreePassUtility.IsMajorityInTransferables(transferables, remainingOnly: false));
            }

            [HarmonyFinalizer]
            internal static Exception Cleanup()
            {
                CaravanFreePassUtility.End();
                return null;
            }
        }

        [HarmonyPatch(typeof(TilesPerDayCalculator))]
        [HarmonyPatch("ApproxTilesPerDayLeftAfterTransfer", new Type[] { typeof(List<TransferableOneWay>), typeof(float), typeof(float), typeof(PlanetTile), typeof(PlanetTile), typeof(bool), typeof(StringBuilder) })]
        internal static class TilesPerDay_TransferablesLeftAfter_Patch
        {
            [HarmonyPrefix]
            internal static void Enter(List<TransferableOneWay> transferables)
            {
                CaravanFreePassUtility.Begin(CaravanFreePassUtility.IsMajorityInTransferables(transferables, remainingOnly: true));
            }

            [HarmonyFinalizer]
            internal static Exception Cleanup()
            {
                CaravanFreePassUtility.End();
                return null;
            }
        }

        [HarmonyPatch(typeof(TilesPerDayCalculator))]
        [HarmonyPatch("ApproxTilesPerDay", new Type[] { typeof(List<ThingCount>), typeof(float), typeof(float), typeof(PlanetTile), typeof(PlanetTile), typeof(bool), typeof(StringBuilder) })]
        internal static class TilesPerDay_ThingCounts_Patch
        {
            [HarmonyPrefix]
            internal static void Enter(List<ThingCount> thingCounts)
            {
                CaravanFreePassUtility.Begin(CaravanFreePassUtility.IsMajorityInThingCounts(thingCounts));
            }

            [HarmonyFinalizer]
            internal static Exception Cleanup()
            {
                CaravanFreePassUtility.End();
                return null;
            }
        }
    }
}
