using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using WNA.DMExtension;

namespace WNA.WNAHarmony
{
    [HarmonyPatch(typeof(MassUtility), "Capacity")]
    public class Patch_MassUtility
    {
        [HarmonyPostfix]
        private static void PostFix(Pawn p, ref float __result)
        {
            float pawnbase = __result;
            var Ext = p.def.GetModExtension<MassCapacity>();
            if (Ext != null)
            {
                float f = Mathf.Max((float)Ext.massCapacity, 1f);
                pawnbase = p.BodySize * f;
            }
            float pawnbonus = 0f;
            if (p.apparel != null)
            {
                foreach (Apparel apparel in p.apparel.WornApparel)
                {
                    var aExt = apparel.def.GetModExtension<MassCapacity>();
                    if (aExt != null)
                        pawnbonus += aExt.massCapacity;
                }
            }
            if (p.equipment != null)
            {
                foreach (ThingWithComps equipment in p.equipment.AllEquipmentListForReading)
                {
                    var eqExt = equipment.def.GetModExtension<MassCapacity>();
                    if (eqExt != null)
                        pawnbonus += eqExt.massCapacity;
                }
            }
            __result = pawnbase + pawnbonus;
        }
    }
}
