using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using WNA.WNADefOf;

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
            if (__instance.parent == null || __instance.parent.Map == null)
                return true;
            Building b = __instance.parent.Position.GetEdifice(__instance.parent.Map);
            if ((b != null && preserverList.Contains(b.def.defName)) || WNAMainDefOf.WNA_PsychicDawn.IsFinished)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
