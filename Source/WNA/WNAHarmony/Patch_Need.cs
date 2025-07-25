using HarmonyLib;
using RimWorld;
using System.Reflection;
using Verse;

namespace WNA.WNAHarmony
{
    public class Patch_Need
    {
        [HarmonyPatch(typeof(Need_MechEnergy), "NeedInterval")]
        public static class Need_MechEnergy_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(Need_MechEnergy __instance)
            {
                FieldInfo pawnField = typeof(Need).GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance);
                Pawn pawn = (Pawn)pawnField.GetValue(__instance);
                if (pawn.health.hediffSet.HasHediff(HediffDef.Named("WNA_InMechanoid")))
                {
                    __instance.CurLevel = __instance.MaxLevel;
                    return false;
                }
                return true;
            }
        }
    }
}
