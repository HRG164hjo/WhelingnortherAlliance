using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace WNA.WNAHarmony
{
    public class Patch_Caravan
    {
        [HarmonyPatch(typeof(CaravanTicksPerMoveUtility))]
        [HarmonyPatch("GetTicksPerMove", new Type[] { typeof(Caravan), typeof(StringBuilder) })]
        public static class Caravan_TickMove_Patch
        {
            private static readonly List<float> localCaravanAnimalSpeedFactors = new List<float>();
            public static bool Prefix(Caravan caravan, ref int __result, StringBuilder explanation)
            {
                if (caravan == null)
                    return true;
                var pawns = caravan.PawnsListForReading.Where(p => p != null && !p.Dead).ToList();
                if (!pawns.Any())
                    return true;
                var speeds = pawns.Select(p =>
                {
                    float baseSpeed = p.GetStatValue(StatDefOf.MoveSpeed, true);
                    float massUsage = MassUtility.GearAndInventoryMass(p);
                    float massCapacity = MassUtility.Capacity(p);
                    float encumbranceFactor = Mathf.Lerp(1f, 0.5f, Mathf.Clamp01(massUsage / Mathf.Max(1f, massCapacity)));
                    return baseSpeed * encumbranceFactor;
                })
                .Where(v => v > 0)
                .ToList();
                if (!speeds.Any())
                    return true;
                float max = speeds.Max();
                float avg = speeds.Average();
                float combinedSpeed = (max + avg) / 2f;
                localCaravanAnimalSpeedFactors.Clear();
                int humanlikeCount = 0;
                float caravanBonusFactorSum = 0f;
                int caravanBonusCount = 0;
                foreach (Pawn pawn in pawns)
                {
                    if (pawn.RaceProps.Humanlike)
                        humanlikeCount++;
                    else if (pawn.IsCaravanRideable())
                        localCaravanAnimalSpeedFactors.Add(pawn.GetStatValue(StatDefOf.CaravanRidingSpeedFactor));
                    if (CaravanBonusUtility.HasCaravanBonus(pawn))
                    {
                        caravanBonusFactorSum += pawn.GetStatValue(StatDefOf.CaravanBonusSpeedFactor);
                        caravanBonusCount++;
                    }
                }
                float ridingFactor = 1f;
                if (localCaravanAnimalSpeedFactors.Count > 0 && humanlikeCount > 0)
                {
                    float maxAnimalFactor = localCaravanAnimalSpeedFactors.Max();
                    float avgAnimalFactor = localCaravanAnimalSpeedFactors.Average();
                    ridingFactor = (maxAnimalFactor + avgAnimalFactor) / 2f;
                }
                float bonusFactor = (caravanBonusCount == 0) ? 1f : (caravanBonusFactorSum / (float)caravanBonusCount);
                float totalMassUsage = caravan.MassUsage;
                float totalMassCapacity = caravan.MassCapacity;
                float totalMassFactor = Mathf.Clamp(((totalMassCapacity + 1) / (totalMassUsage + 1)), 0.3f, 2f);
                float finalSpeed = combinedSpeed * ridingFactor * bonusFactor * totalMassFactor;
                int ticksPerMove = Mathf.Max(1, Mathf.RoundToInt(1000f / finalSpeed));
                __result = ticksPerMove;
                if (explanation != null)
                {
                    float tilesPerDay = 60000f / (float)ticksPerMove;
                    explanation.Append("CaravanMovementSpeedFull".Translate() + ":");
                    explanation.AppendLine();
                    explanation.Append("  " + "FinalCaravanPawnsMovementSpeed".Translate() + ": "
                                       + tilesPerDay.ToString("0.#") + " " + "TilesPerDay".Translate());
                    explanation.AppendLine();
                    explanation.Append("  " + "Base move speed combined (Max + Avg) / 2" + ": " + combinedSpeed.ToString("0.##"));
                    explanation.AppendLine();
                    explanation.Append("  " + "Riding Factor (Max + Avg) / 2" + ": " + ridingFactor.ToStringPercent());
                    explanation.AppendLine();
                    explanation.Append("  " + "Bonus Factor" + ": " + bonusFactor.ToStringPercent());
                    explanation.AppendLine();
                    explanation.Append("  " + "Total Mass Factor" + ": " + totalMassFactor.ToString("0.##"));
                    explanation.AppendLine();
                }
                return false;
            }
        }
    }
}