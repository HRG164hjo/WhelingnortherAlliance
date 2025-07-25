using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace WNA.WNAHarmony
{
    [HarmonyPatch(typeof(CompRottable), "get_Active")]
    public class Patch_CompRottable
    {
        private static readonly HashSet<string> preserverList = new HashSet<string>
        {
            "WNA_LinkShelf"
        };
        [HarmonyPrefix]
        public static bool ActivePrefix(ref bool __result, CompRottable __instance)
        {
            if (__instance.parent == null || __instance.parent.Map == null) return true;
            Building b = __instance.parent.Position.GetEdifice(__instance.parent.Map);
            if (b != null && preserverList.Contains(b.def.defName))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
