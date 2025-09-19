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
                // 1. 空引用检查
                if (__instance == null || fuelThings == null || fuelThings.Count == 0)
                {
                    return true; // 执行原版逻辑
                }

                var extraStat = __instance.parent.def.GetModExtension<RefuelableExtraStat>();
                if (extraStat == null)
                {
                    return true; // 无扩展时执行原版逻辑
                }

                float totalFuelAdded = 0f;
                float maxToAdd = __instance.Props.fuelCapacity - __instance.Fuel;
                float difficultyMultiplier = __instance.Props.FuelMultiplierCurrentDifficulty;

                // 使用索引循环而非foreach，避免修改集合时的异常
                for (int i = fuelThings.Count - 1; i >= 0; i--)
                {
                    Thing fuelThing = fuelThings[i];
                    // 检查物品是否有效
                    if (fuelThing == null || fuelThing.Destroyed)
                    {
                        fuelThings.RemoveAt(i);
                        continue;
                    }

                    float statValue = 1f;

                    // 3. 处理useBodySize和useStat的并存逻辑
                    // 优先级：如果两者都启用，先计算体型再乘以统计值
                    if (extraStat.useBodySize)
                    {
                        if (fuelThing.def.race != null)
                        {
                            statValue = fuelThing.def.race.baseBodySize;
                        }
                        else
                        {
                            Log.Warning($"[WNA] Tried to use BodySize but fuel '{fuelThing.def.defName}' has no race props. Using default value.");
                            statValue = 1f; // 使用默认值
                        }
                    }

                    // 如果启用统计值计算，应用统计值（可与体型叠加）
                    if (extraStat.useStat && extraStat.multStat != null)
                    {
                        // 4. 简化统计值获取逻辑，移除冗余检查
                        statValue *= fuelThing.def.GetStatValueAbstract(extraStat.multStat);
                    }

                    // 计算单位燃料值
                    float perUnitFuel = statValue * extraStat.multFactor * difficultyMultiplier;

                    // 5. 优化循环效率：一次性计算需要消耗的数量
                    if (perUnitFuel <= 0)
                    {
                        continue; // 无效燃料值，跳过
                    }

                    // 计算还需要多少燃料才能填满
                    float remainingNeeded = maxToAdd - totalFuelAdded;
                    if (remainingNeeded <= 0)
                    {
                        break; // 已经加满，无需继续处理
                    }

                    // 计算需要消耗的物品数量
                    int unitsNeeded = (int)Math.Ceiling(remainingNeeded / perUnitFuel);
                    int unitsToConsume = Math.Min(unitsNeeded, fuelThing.stackCount);

                    // 计算实际添加的燃料
                    float fuelToAdd = unitsToConsume * perUnitFuel;
                    // 确保不超过最大容量
                    if (totalFuelAdded + fuelToAdd > maxToAdd)
                    {
                        fuelToAdd = maxToAdd - totalFuelAdded;
                        // 重新计算实际需要消耗的数量（可能有小数部分）
                        unitsToConsume = (int)Math.Ceiling(fuelToAdd / perUnitFuel);
                        unitsToConsume = Math.Max(1, unitsToConsume); // 至少消耗1个单位
                    }

                    // 消耗物品
                    fuelThing.stackCount -= unitsToConsume;
                    totalFuelAdded += fuelToAdd;

                    // 5. 处理物品销毁
                    if (fuelThing.stackCount <= 0)
                    {
                        fuelThings.RemoveAt(i);
                        fuelThing.Destroy(DestroyMode.Vanish); // 使用更安全的销毁方式
                    }

                    // 检查是否已经加满
                    if (totalFuelAdded >= maxToAdd)
                    {
                        break;
                    }
                }

                if (totalFuelAdded > 0)
                {
                    __instance.Refuel(totalFuelAdded);
                }
                return false;
            }
        }
    }
}
