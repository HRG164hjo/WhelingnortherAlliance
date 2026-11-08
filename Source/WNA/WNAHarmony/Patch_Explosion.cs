using HarmonyLib;
using System;
using System.Collections.Generic;
using Verse;

namespace WNA.WNAHarmony
{
    internal class Patch_Explosion
    {
        private static readonly HashSet<string> damageList = new HashSet<string>
        {
            "BombSuper",
            "WNA_ChimingBlaze",
            "WNA_RadBurn",
            "WNA_Hypothermal",
            "WNA_Psychedelic"
        };
        [HarmonyPatch(typeof(DamageWorker), nameof(DamageWorker.ExplosionAffectCell))]
        internal static class ExplosionAffectCell_Patch
        {
            // Roofs are cleared one cell at a time as the blast reaches it, instead of one extra
            // radial sweep before the explosion starts. This skips cells the blast never affects
            // (out of bounds, excludeRadius, maps removed mid-expansion) and spreads the roof-grid
            // notifications over the expansion ticks.
            static void Prefix(Explosion explosion, IntVec3 c)
            {
                if (!damageList.Contains(explosion.damType.defName))
                {
                    return;
                }
                Map map = explosion.Map;
                if (map == null || map != Find.CurrentMap)
                {
                    return;
                }
                if (map.roofGrid.RoofAt(c) != null)
                {
                    map.roofGrid.SetRoof(c, null);
                }
            }
        }
        [HarmonyPatch(typeof(DamageWorker), nameof(DamageWorker.ExplosionCellsToHit),
            new Type[] { typeof(Explosion) })]
        internal static class Patch_ExplosionCellsToHit
        {
            static bool Prefix(DamageWorker __instance, Explosion explosion, ref IEnumerable<IntVec3> __result)
            {
                if (damageList.Contains(explosion.damType.defName))
                {
                    __result = GenRadial.RadialCellsAround(explosion.Position, explosion.radius, true);
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(Explosion), nameof(Explosion.StartExplosion))]
        internal static class Explosion_StartExplosion_Patch
        {
            static void Prefix(Explosion __instance)
            {
                if (damageList.Contains(__instance.damType.defName))
                {
                    __instance.needLOSToCell1 = null;
                    __instance.needLOSToCell2 = null;
                    __instance.applyDamageToExplosionCellsNeighbors = true;
                }
            }
        }
    }
}
