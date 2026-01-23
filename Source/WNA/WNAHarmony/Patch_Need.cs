using HarmonyLib;
using RimWorld;
using System.Reflection;
using Verse;
using WNA.WNADefOf;

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
                if (pawn.health.hediffSet.HasHediff(HediffDef.Named("WNA_InMechanoid")) ||
                    pawn.health.hediffSet.HasHediff(HediffDef.Named("WNA_RobeBoost")) ||
                    pawn.health.hediffSet.HasHediff(HediffDef.Named("WNA_RobeBoostLite")) ||
                    (pawn?.Faction?.IsPlayer == true &&
                    pawn.Faction?.ideos?.PrimaryIdeo?.HasPrecept(WNAMainDefOf.WNA_P_Proselyte) == true)
                    )
                {
                    __instance.CurLevel = __instance.MaxLevel;
                    return false;
                }
                return true;
            }
        }
    }
}
