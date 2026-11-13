using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace WNA.WNAHarmony
{
    internal class Patch_Caravan
    {
        [HarmonyPatch(typeof(CaravanTicksPerMoveUtility))]
        [HarmonyPatch("GetTicksPerMove", new Type[] { typeof(List<Pawn>), typeof(float), typeof(float), typeof(bool), typeof(StringBuilder) })]
        internal static class Caravan_TickMove_Patch
        {
            private const float MaxLoadSpeedBonus = 2f;

            private static readonly List<float> tmpRideableSpeedFactors = new List<float>();

            internal static bool Prefix(List<Pawn> pawns, float massUsage, float massCapacity, bool isShuttle,
                StringBuilder explanation, ref int __result)
            {
                if (pawns == null || pawns.Count == 0)
                    return true;
                if (isShuttle)
                {
                    explanation?.Append("CaravanMovementSpeedShuttle".Translate());
                    __result = 0;
                    return false;
                }

                float referenceSpeed = ThingDefOf.Human.GetStatValueAbstract(StatDefOf.MoveSpeed);
                if (referenceSpeed <= 0f)
                    return true;

                int humanlikeCount = 0;
                int rideableCount = 0;
                int bonusCount = 0;
                int speedCount = 0;
                float maxSpeed = 0f;
                float speedSum = 0f;
                float bonusSum = 0f;
                tmpRideableSpeedFactors.Clear();

                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn pawn = pawns[i];
                    if (pawn == null)
                        continue;

                    if (pawn.RaceProps.Humanlike)
                    {
                        humanlikeCount++;
                    }
                    else if (pawn.IsCaravanRideable())
                    {
                        rideableCount++;
                        tmpRideableSpeedFactors.Add(pawn.GetStatValue(StatDefOf.CaravanRidingSpeedFactor));
                    }

                    if (CaravanBonusUtility.HasCaravanBonus(pawn))
                    {
                        bonusSum += pawn.GetStatValue(StatDefOf.CaravanBonusSpeedFactor);
                        bonusCount++;
                    }

                    if (pawn.Dead || pawn.Downed)
                        continue;

                    float speed = pawn.GetStatValue(StatDefOf.MoveSpeed);
                    if (speed <= 0f)
                        continue;

                    speedCount++;
                    speedSum += speed;
                    if (speed > maxSpeed)
                        maxSpeed = speed;
                }

                if (speedCount == 0)
                    return true;

                float combinedSpeed = (maxSpeed + speedSum / speedCount) / 2f;
                float speedFactor = combinedSpeed / referenceSpeed;
                float loadFactor = LoadSpeedFactor(massUsage, massCapacity);
                float ridingFactor = RidingSpeedFactor(humanlikeCount);
                float bonusFactor = bonusCount == 0 ? 1f : bonusSum / bonusCount;

                float baseTicks = BaseHumanlikeTicksPerCell() * CaravanTicksPerMoveUtility.CellToTilesConversionRatio;
                __result = Mathf.Max(1, Mathf.RoundToInt(baseTicks / (speedFactor * loadFactor * ridingFactor * bonusFactor)));

                if (explanation != null)
                {
                    AppendExplanation(explanation, baseTicks, combinedSpeed, referenceSpeed, loadFactor,
                        ridingFactor, bonusFactor, rideableCount, humanlikeCount, __result);
                }
                return false;
            }

            private static float LoadSpeedFactor(float massUsage, float massCapacity)
            {
                if (massCapacity <= 0f)
                    return 1f;
                // 空载最快为 2 倍；下限防止极端超载时出现负系数（原版此时会算出 1 tick）。
                float factor = Mathf.Lerp(MaxLoadSpeedBonus, 1f, massUsage / massCapacity);
                return Mathf.Clamp(factor, 0.1f, MaxLoadSpeedBonus);
            }

            private static float RidingSpeedFactor(int humanlikeCount)
            {
                if (tmpRideableSpeedFactors.Count == 0 || humanlikeCount <= 0)
                    return 1f;

                tmpRideableSpeedFactors.Sort();
                tmpRideableSpeedFactors.Reverse();
                if (tmpRideableSpeedFactors.Count > humanlikeCount)
                {
                    tmpRideableSpeedFactors.RemoveRange(humanlikeCount, tmpRideableSpeedFactors.Count - humanlikeCount);
                }

                float sum = 0f;
                for (int i = 0; i < humanlikeCount; i++)
                {
                    sum += i < tmpRideableSpeedFactors.Count ? tmpRideableSpeedFactors[i] : 1f;
                }
                return sum / humanlikeCount;
            }

            private static int BaseHumanlikeTicksPerCell()
            {
                float cellsPerSecond = ThingDefOf.Human.GetStatValueAbstract(StatDefOf.MoveSpeed) / 60f;
                return Mathf.RoundToInt(1f / cellsPerSecond);
            }

            private static void AppendExplanation(StringBuilder sb, float baseTicks, float combinedSpeed, float referenceSpeed,
                float loadFactor, float ridingFactor, float bonusFactor, int rideableCount, int humanlikeCount, int ticks)
            {
                sb.Append("CaravanMovementSpeedFull".Translate() + ":");
                sb.AppendLine();
                sb.Append("  " + "StatsReport_BaseValue".Translate() + ": " + (60000f / baseTicks).ToString("0.#") + " " + "TilesPerDay".Translate());
                sb.AppendLine();
                sb.Append("  Combined pawn speed (max + avg) / 2: " + combinedSpeed.ToString("0.##") + " / " + referenceSpeed.ToString("0.##"));
                sb.AppendLine();
                sb.Append("  " + "RideableAnimalsPerPeople".Translate() + $": {rideableCount} / {humanlikeCount}");
                if (!Mathf.Approximately(ridingFactor, 1f))
                {
                    sb.AppendLine();
                    sb.Append("  " + "MultiplierFromRiddenAnimals".Translate() + ": " + ridingFactor.ToStringPercent());
                }
                if (!Mathf.Approximately(bonusFactor, 1f))
                {
                    sb.AppendLine();
                    sb.Append("  " + "MultiplierFromMoveSpeedBonus".Translate() + ": " + bonusFactor.ToStringPercent());
                }
                sb.AppendLine();
                sb.Append("  " + "MultiplierForCarriedMass".Translate(loadFactor.ToStringPercent()));
                sb.AppendLine();
                sb.Append("  " + "FinalCaravanPawnsMovementSpeed".Translate() + ": " + (60000f / ticks).ToString("0.#") + " " + "TilesPerDay".Translate());
            }
        }
    }
}
