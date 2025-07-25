using HarmonyLib;
using RimWorld;
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
                var extraStat = __instance.parent.def.GetModExtension<RefuelableExtraStat>();
                if (extraStat == null)
                {
                    // 调用原始方法
                    return true;
                }

                float totalFuelAdded = 0f;
                float maxToAdd = __instance.Props.fuelCapacity - __instance.Fuel;
                float difficultyMultiplier = __instance.Props.FuelMultiplierCurrentDifficulty;

                foreach (Thing fuelThing in fuelThings.ListFullCopy())
                {
                    float statValue = 1f;

                    if (extraStat.useBodySize)
                    {
                        if (fuelThing.def.race != null)
                        {
                            statValue = fuelThing.def.race.baseBodySize;
                        }
                        else
                        {
                            Log.Warning($"[WNA] Tried to use BodySize but fuel '{fuelThing.def.defName}' has no race props.");
                        }
                    }
                    else if (extraStat.multStat != null)
                    {
                        if (fuelThing.def.statBases != null && fuelThing.def.statBases.Any(s => s.stat == extraStat.multStat))
                        {
                            statValue = fuelThing.def.GetStatValueAbstract(extraStat.multStat);
                        }
                        else
                        {
                            Log.Warning($"[WNA] Tried to use stat '{extraStat.multStat.defName}' on '{fuelThing.def.defName}', but it's not defined.");
                        }
                    }

                    float perUnitFuel = statValue * extraStat.multFactor * difficultyMultiplier;

                    while (fuelThing.stackCount > 0 && totalFuelAdded < maxToAdd)
                    {
                        fuelThing.stackCount--;
                        totalFuelAdded += perUnitFuel;
                    }

                    if (fuelThing.stackCount == 0)
                    {
                        fuelThings.Remove(fuelThing);
                        fuelThing.Destroy();
                    }
                }

                __instance.Refuel(totalFuelAdded);
                return false; // 阻止原方法执行
            }
        }

    }
}
