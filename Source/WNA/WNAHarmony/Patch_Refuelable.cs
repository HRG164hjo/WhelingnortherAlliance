using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using WNA.DMExtension;

namespace WNA.WNAHarmony
{
    public class Patch_Refuelable
    {
        [HarmonyPatch(typeof(CompRefuelable), nameof(CompRefuelable.Refuel), new[] { typeof(List<Thing>) })]
        public static class Patch_CompRefuelable_RefuelThings
        {
            [HarmonyPrefix]
            public static bool Prefix(CompRefuelable __instance, List<Thing> fuelThings)
            {
                if (__instance == null || fuelThings == null || fuelThings.Count == 0)
                    return true;
                var extraStat = __instance.parent.def.GetModExtension<RefuelableExtraStat>();
                if (extraStat == null)
                    return true;
                float totalFuelAdded = 0f;
                float maxToAdd = __instance.Props.fuelCapacity - __instance.Fuel;
                float difficultyMultiplier = __instance.Props.FuelMultiplierCurrentDifficulty;
                for (int i = fuelThings.Count - 1; i >= 0; i--)
                {
                    Thing fuelThing = fuelThings[i];
                    if (fuelThing == null || fuelThing.Destroyed)
                    {
                        fuelThings.RemoveAt(i);
                        continue;
                    }
                    float statValue = 1f;
                    if (extraStat.useBodySize)
                    {
                        if (fuelThing.def.race != null)
                            statValue = fuelThing.def.race.baseBodySize;
                        else
                        {
                            Log.Warning($"[WNA] Tried to use BodySize but fuel '{fuelThing.def.defName}' has no race props. Using default value.");
                            statValue = 1f;
                        }
                    }
                    if (extraStat.useStat)
                    {
                        StatDef stat = extraStat.multStat ?? StatDefOf.Mass;
                        statValue *= fuelThing.def.GetStatValueAbstract(stat);
                    }
                    float perUnitFuel = statValue * extraStat.multFactor * difficultyMultiplier;
                    if (perUnitFuel <= 0) continue;
                    float remainingNeeded = maxToAdd - totalFuelAdded;
                    if (remainingNeeded <= 0) break;
                    int unitsNeeded = (int)Math.Ceiling(remainingNeeded / perUnitFuel);
                    int unitsToConsume = Math.Min(unitsNeeded, fuelThing.stackCount);
                    float fuelToAdd = unitsToConsume * perUnitFuel;
                    if (totalFuelAdded + fuelToAdd > maxToAdd)
                    {
                        fuelToAdd = maxToAdd - totalFuelAdded;
                        unitsToConsume = (int)Math.Ceiling(fuelToAdd / perUnitFuel);
                        unitsToConsume = Math.Max(1, unitsToConsume);
                    }
                    fuelThing.stackCount -= unitsToConsume;
                    totalFuelAdded += fuelToAdd;
                    if (fuelThing.stackCount <= 0)
                    {
                        fuelThings.RemoveAt(i);
                        fuelThing.Destroy(DestroyMode.Vanish);
                    }
                    if (totalFuelAdded >= maxToAdd) break;
                }
                if (totalFuelAdded > 0)
                    __instance.Refuel(totalFuelAdded);
                return false;
            }
        }
    }
}
