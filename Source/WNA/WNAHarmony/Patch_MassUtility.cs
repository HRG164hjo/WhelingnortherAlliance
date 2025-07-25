using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace WNA.WNAHarmony
{
    [HarmonyPatch(typeof(MassUtility), "Capacity")]
    public class Patch_MassUtility
    {
        [HarmonyPostfix]
        private static void PostFix(Pawn p, ref float __result)
        {
            var massCapacityExtension = p.def.GetModExtension<DMExtension.MassCapacity>();
            if (massCapacityExtension != null)
            {
                float f = Mathf.Max(massCapacityExtension.massCapacity, 1f);
                __result = p.BodySize * f;
            }
        }
    }
}
