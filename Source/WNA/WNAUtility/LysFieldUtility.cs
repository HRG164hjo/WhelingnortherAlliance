using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using WNA.DMExtension;
using WNA.WNADefOf;

namespace WNA.WNAUtility
{
    public static class LysisFieldUtility
    {
        private static bool spreading = false;
        public static void SpreadLysisField(Map map, IntVec3 center, int sourceLevel)
        {
            if (map == null || sourceLevel <= 0) return;
            if (spreading) return;
            spreading = true;
            try
            {
                int newLevel = (int)Math.Ceiling(sourceLevel * 0.5f);
                float radius = 4.9f + (float)Math.Sqrt(sourceLevel);
                float scale = Mathf.Clamp01(sourceLevel / 30f);
                FleckMaker.Static(center, map, FleckDefOf.ExplosionFlash, scale);
                foreach (var cell in GenRadial.RadialCellsAround(center, radius, true))
                {
                    if (!cell.InBounds(map)) continue;
                    var things = cell.GetThingList(map);
                    if (things == null || things.Count == 0) continue;
                    foreach (var t in things.ToList())
                    {
                        if (t == null || t.Destroyed) continue;
                        if (!t.def.destroyable) continue;
                        TechnoConfig cfg = TechnoConfig.Get(t.def);
                        if (cfg != null && cfg.immuneToRadiation == true) continue;
                        LysField_GameComp.Instance?.AddOrUpdateField(t, newLevel, 90);
                        DamageInfo dinfo = new DamageInfo(WNAMainDefOf.WNA_LysField, sourceLevel, float.MaxValue);
                        t.TakeDamage(dinfo);
                    }
                }
            }
            finally
            {
                spreading = false;
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
    [HarmonyPatch(typeof(Thing), "Kill")]
    public static class Patch_Thing_Kill
    {
        static void Postfix(Thing __instance)
        {
            if (__instance == null || __instance.MapHeld == null) return;
            var manager = LysField_GameComp.Instance;
            int level = manager?.GetLevel(__instance) ?? 0;
            if (level > 0)
                LysisFieldUtility.SpreadLysisField(__instance.MapHeld, __instance.PositionHeld, level);
        }
    }
}
