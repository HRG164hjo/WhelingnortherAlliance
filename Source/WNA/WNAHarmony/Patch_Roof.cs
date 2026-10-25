using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using WNA.WNAModExtension;
using WNA.WNAUtility;

namespace WNA.WNAHarmony
{
    internal class Patch_Roof
    {
        private static T GetPrivateField<T>(Type classType, string fieldName)
        {
            try
            {
                return (T)classType.GetField(fieldName, BindingFlags.NonPublic |
                    BindingFlags.Static)?.GetValue(null);
            }
            catch
            {
                Log.Error($"[WNAHarmony] 无法通过反射获取字段 {fieldName} 的值。");
                return default;
            }
        }
        private static readonly IntRange ThinRoofCrushDamageRange = GetPrivateField<IntRange>(typeof(RoofCollapserImmediate), "ThinRoofCrushDamageRange");
        [HarmonyPatch(typeof(Skyfaller), "HitRoof")]
        internal static class Patch_Skyfaller_HitRoof
        {
            internal static bool Prefix(Skyfaller __instance)
            {
                TechnoConfig ext = TechnoConfig.Get(__instance?.def);
                if (ext?.ignoreRoof == true)
                    return false;
                return true;
            }
        }
        [HarmonyPatch(typeof(RoofCollapserImmediate), "DropRoofInCellPhaseOne")]
        internal static class Patch_RoofCollapserImmediate_PhaseOne
        {
            [HarmonyPrefix]
            internal static bool SkipImmunePawns(IntVec3 c, Map map, List<Thing> outCrushedThings)
            {
                if (!c.Roofed(map)) return true;
                var things = c.GetThingList(map);
                bool hasImmunePawns = false;
                var filteredThings = new List<Thing>();
                foreach (var thing in things)
                {
                    if (thing is Pawn pawn && General.IsWNARace(pawn))
                        hasImmunePawns = true;
                    else filteredThings.Add(thing);
                }
                if (!hasImmunePawns)
                    return true;
                foreach (var thing in filteredThings)
                {
                    if (thing.def.destroyable)
                    {
                        outCrushedThings?.Add(thing);
                        float damageAmount = ThinRoofCrushDamageRange.RandomInRange;
                        thing.TakeDamage(new DamageInfo(DamageDefOf.Crush, damageAmount));
                    }
                }
                return false;
            }
        }
    }
}
