using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace WNA.WNAHarmony
{
    public static class Patch_Plant
    {
        private static readonly HashSet<string> terrainList = new HashSet<string>
        {
            "WNA_FocusSoil",
            "WNA_BridgeSoil"
        };
        private static readonly MethodInfo TargetMethod = AccessTools.Method(typeof(Plant), "CheckMakeLeafless", new System.Type[] { });
        [HarmonyPatch]
        public static class ForCheckMakeLeafless
        {
            static MethodBase TargetMethod() => Patch_Plant.TargetMethod;
            public static bool Prefix(Plant __instance)
            {
                if (!__instance.Spawned) return true;

                TerrainDef terrain = __instance.Map.terrainGrid.TerrainAt(__instance.Position);
                if (terrain != null && terrainList.Contains(terrain.defName))
                    return false;
                return true;
            }
        }
        [HarmonyPatch(typeof(Plant), nameof(Plant.CurrentDyingDamagePerTick), MethodType.Getter)]
        public static class ForCurrentDyingDamagePerTick
        {
            public static void Postfix(Plant __instance, ref float __result)
            {
                if (!__instance.Spawned) return;
                TerrainDef terrain = __instance.Map.terrainGrid.TerrainAt(__instance.Position);
                if (terrain != null && terrainList.Contains(terrain.defName))
                    __result = 0f;
            }
        }
        [HarmonyPatch(typeof(Plant), nameof(Plant.Dying), MethodType.Getter)]
        public static class ForDying
        {
            public static bool Prefix(Plant __instance, ref bool __result)
            {
                if (!__instance.Spawned) return true;

                TerrainDef terrain = __instance.Map.terrainGrid.TerrainAt(__instance.Position);
                if (terrain != null && terrainList.Contains(terrain.defName))
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }
        public static class ForGrowthRate
        {
            public static bool Prefix(Plant __instance, ref float __result)
            {
                if (!__instance.Spawned) return true;
                TerrainDef terrain = __instance.Map.terrainGrid.TerrainAt(__instance.Position);
                if (terrain != null && terrainList.Contains(terrain.defName))
                {
                    __result += terrain.fertility;
                    return false;
                }
                return true;
            }
        }
    }
}
