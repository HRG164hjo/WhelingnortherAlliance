using HarmonyLib;
using RimWorld;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNAHarmony
{
    [HarmonyPatch(typeof(WorkGiver_HunterHunt), "HasHuntingWeapon")]
    public class Patch_HunterHunt
    {
        public static void Postfix(ref bool __result, Pawn p)
        {
            if (p.equipment.Primary != null && p.equipment.Primary.def == WNAMainDefOf.WNA_Weapon_RiteLance)
            {
                __result = true;
            }
        }
    }
}
 