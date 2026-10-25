using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNAHarmony
{
    [HarmonyPatch(typeof(CompRottable), "get_Active")]
    internal class Patch_CompRottable
    {
        private static readonly HashSet<string> buildingList = new HashSet<string>
        {
            "WNA_LinkShelf"
        };
        private static readonly HashSet<string> terrainList = new HashSet<string>
        {
            "WNA_FocusSpring",
            "WNA_FocusFloor",
            "WNA_FocusFloorPlain",
            "WNA_FocusFloorDim",
            "WNA_FocusFloorPatchwork",
            "WNA_FocusFloorTwistedVoid",
            "WNA_FocusFloorMosaic",
            "WNA_FocusFloorCarve",
            "WNA_FocusFloorDarkCarve",
            "WNA_FocusPlatform",
            "WNA_FocusPlatformPlain",
            "WNA_FocusPlatformDim",
            "WNA_FocusPlatformPatchwork",
            "WNA_FocusPlatformTwistedVoid",
            "WNA_FocusPlatformMosaic",
            "WNA_FocusPlatformCarve",
            "WNA_FocusPlatformDarkCarve"
        };
        [HarmonyPrefix]
        public static bool ActivePrefix(ref bool __result, CompRottable __instance)
        {
            if (__instance.parent == null || __instance.parent.Map == null)
                return true;
            Building b = __instance.parent.Position.GetEdifice(__instance.parent.Map);
            TerrainDef t = __instance.parent.Position.GetTerrain(__instance.parent.Map);
            if ((b != null && buildingList.Contains(b.def.defName))
                || (t != null && terrainList.Contains(t.defName))
                || WNAMainDefOf.WNA_PsychicDawn.IsFinished)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
