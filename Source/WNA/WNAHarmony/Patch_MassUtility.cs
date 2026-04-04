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
            float pawnbase = 0f;
            float pawnbonus = 0f;
            float pawnhealth = 0f;
            var Ext = p.def.GetModExtension<MassCapacity>();
            if (Ext != null)
                pawnbase = Mathf.Max(Ext.massCapacity, 1f);
            if (p.apparel != null)
            {
                foreach (Apparel apparel in p.apparel.WornApparel)
                {
                    var aExt = apparel.def.GetModExtension<MassCapacity>();
                    if (aExt != null)
                        pawnbonus += aExt.massCapacity;
                }
            }
            if (p.health != null && p.health.hediffSet != null)
            {
                foreach (Hediff hediff in p.health.hediffSet.hediffs)
                {
                    var hdef = hediff.def;
                    if (hdef != null)
                    {
                        var hExt = hdef.GetModExtension<MassCapacity>();
                        if (hExt != null)
                            pawnhealth += hExt.massCapacity;
                    }
                }
            }
            __result = p.BodySize * (pawnbase + pawnbonus + pawnhealth);
        }
    }
}
