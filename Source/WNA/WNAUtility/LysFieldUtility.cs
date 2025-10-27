using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using WNA.DMExtension;

namespace WNA.WNAUtility
{
    public static class LysisFieldUtility
    {
        public static void SpreadLysisField(Map map, IntVec3 center, int sourceLevel)
        {
            if (map == null || sourceLevel <= 0) return;

            int newLevel = (int)Math.Ceiling(sourceLevel * 0.5f);
            float radius = 4.9f;

            foreach (var cell in GenRadial.RadialCellsAround(center, radius, true))
            {
                if (!cell.InBounds(map)) continue;
                List<Thing> things = cell.GetThingList(map);
                if (things == null || things.Count == 0) continue;

                foreach (var t in things)
                {
                    if (t == null || t.Destroyed) continue;
                    if (t.def.category == ThingCategory.Building && t.def.passability == Traversability.Impassable)
                        continue;
                    LysField_GameComp.Instance?.AddOrUpdateField(t, newLevel, 90);
                }
            }
        }
        public static int GetLysisLevel(Thing thing)
        {
            return LysField_GameComp.Instance?.GetLevel(thing) ?? 0;
        }
        public static void ClearLysisField(Thing thing)
        {
            LysField_GameComp.Instance?.Remove(thing);
        }
    }
    [HarmonyPatch(typeof(Thing), "TakeDamage")]
    public static class Patch_Thing_TakeDamage
    {
        static void Prefix(ref DamageInfo dinfo, Thing __instance)
        {
            var manager = LysField_GameComp.Instance;
            if (manager == null) return;
            int level = manager.GetLevel(__instance);
            if (level <= 0) return;
            if (TechnoConfig.Get(__instance.def)?.immuneToRadiation.GetValueOrDefault(false) == true)
                return;
            float multiplier = (float)Math.Pow(2, level);
            dinfo.SetAmount(dinfo.Amount * multiplier);
            if (level >= 31)
                if (__instance.MapHeld != null) __instance.Destroy(DestroyMode.KillFinalize);
        }
    }
    [HarmonyPatch(typeof(Thing), "Destroy")]
    public static class Patch_Thing_Destroy
    {
        static void Postfix(Thing __instance, DestroyMode mode)
        {
            if (__instance == null || __instance.MapHeld == null) return;
            var manager = LysField_GameComp.Instance;
            int level = manager?.GetLevel(__instance) ?? 0;
            if (level > 0)
            {
                float intensity = Mathf.Clamp01(level / 30f);
                float scale = 1.5f + 4f * intensity;
                FleckMaker.Static(__instance.PositionHeld, __instance.MapHeld, FleckDefOf.ExplosionFlash, scale);
                FleckMaker.ThrowSmoke(__instance.DrawPos, __instance.MapHeld, 0.8f + 1.2f * intensity);
                manager.Spread(__instance.MapHeld, __instance.PositionHeld, level);
                manager.Remove(__instance);
            }
        }
    }
}
