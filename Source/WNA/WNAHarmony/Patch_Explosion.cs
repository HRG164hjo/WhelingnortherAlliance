using HarmonyLib;
using System;
using System.Collections.Generic;
using Verse;

namespace WNA.WNAHarmony
{
    public class Patch_Explosion
    {
        private static readonly HashSet<string> damageList = new HashSet<string>
        {
            "BombSuper",
            "WNA_CastBomb",
            "WNA_ChimingBlaze",
            "WNA_KineBomb",
            "WNA_RadBurn"
        };
        private static void RemoveRoofsInExplosion(Explosion explosion)
        {
            Map map = explosion.Map;
            if (map == null) return;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(explosion.Position, explosion.radius, true))
            {
                if (map != Find.CurrentMap || !cell.InBounds(map)) continue;
                RoofDef roof = map.roofGrid.RoofAt(cell);
                if (roof != null)
                {
                    map.roofGrid.SetRoof(cell, null);
                    RoofCollapserImmediate.DropRoofInCells(cell, map);
                }
            }
        }
        [HarmonyPatch(typeof(DamageWorker), nameof(DamageWorker.ExplosionCellsToHit),
            new Type[] { typeof(Explosion) })]
        public static class Patch_ExplosionCellsToHit
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
        public static class Explosion_StartExplosion_Patch
        {
            static void Prefix(Explosion __instance)
            {
                if (damageList.Contains(__instance.damType.defName))
                {
                    __instance.needLOSToCell1 = null;
                    __instance.needLOSToCell2 = null;
                    __instance.applyDamageToExplosionCellsNeighbors = true;
                    RemoveRoofsInExplosion(__instance);
                }
            }
        }
    }
}
