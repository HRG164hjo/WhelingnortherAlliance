using HarmonyLib;
using System.Linq;
using Verse;
using WNA.GameCond;

namespace WNA.WNAHarmony
{
    public class Patch_Vacuum
    {
        private static bool HasClimateControl(Map map)
        {
            return map.gameConditionManager.ActiveConditions.Any(gc => gc is GameCond_ClimateControl);
        }
        [HarmonyPatch(typeof(VacuumComponent), "ActiveOnMap", MethodType.Getter)]
        private static class Patch_VacuumComponent_ActiveOnMap
        {
            public static bool Prefix(VacuumComponent __instance, ref bool __result)
            {
                if (HasClimateControl(__instance.map))
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(VacuumUtility), "EverInVacuum")]
        private static class Patch_VacuumUtility_EverInVacuum
        {
            public static bool Prefix(Map map, ref bool __result)
            {
                if (map != null && HasClimateControl(map))
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(VacuumComponent), nameof(VacuumComponent.MapComponentDraw))]
        private static class Patch_VacuumComponent_MapComponentDraw
        {
            public static bool Prefix(VacuumComponent __instance)
            {
                if (HasClimateControl(__instance.map))
                    return false;
                return true;
            }
        }
    }
}
