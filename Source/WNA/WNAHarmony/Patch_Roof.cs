using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace WNA.WNAHarmony
{
    public class Patch_Roof
    {
        private static readonly HashSet<string> ImmuneRaces = new HashSet<string>
            {
                "WNA_WNThan",
                "WNA_Human"
            };
        private static T GetPrivateField<T>(Type classType, string fieldName)
        {
            try
            {
                return (T)classType
                    .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)
                    ?.GetValue(null);
            }
            catch
            {
                Log.Error($"[WNAHarmony] 无法通过反射获取字段 {fieldName} 的值。");
                return default;
            }
        }
        private static readonly IntRange ThinRoofCrushDamageRange = GetPrivateField<IntRange>(typeof(RoofCollapserImmediate), "ThinRoofCrushDamageRange");
        [HarmonyPatch(typeof(Skyfaller), "HitRoof")]
        public static class Patch_Skyfaller_HitRoof
        {
            public static bool Prefix(Skyfaller __instance)
            {
                if (__instance?.def?.defName == "WNA_WARPIN")
                {
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(RoofCollapserImmediate), "DropRoofInCellPhaseOne")]
        public static class Patch_RoofCollapserImmediate_PhaseOne
        {
            [HarmonyPrefix]
            public static bool SkipImmunePawns(IntVec3 c, Map map, List<Thing> outCrushedThings)
            {
                if (!c.Roofed(map)) return true;
                var things = c.GetThingList(map);
                bool hasImmunePawns = false;
                var filteredThings = new List<Thing>();
                foreach (var thing in things)
                {
                    if (thing is Pawn pawn && ImmuneRaces.Contains(pawn.def.defName)) hasImmunePawns = true;
                    else filteredThings.Add(thing);
                }
                if (!hasImmunePawns) return true;
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
