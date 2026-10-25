using HarmonyLib;
using RimWorld;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNAHarmony
{
    internal class Patch_HunterHunt
    {
        [HarmonyPatch(typeof(WorkGiver_HunterHunt), "HasHuntingWeapon")]
        internal class Patch_HasHuntingWeapon
        {
            internal static void Postfix(ref bool __result, Pawn p)
            {
                if (p.equipment.Primary != null)
                    if (p.equipment.Primary.def == WNAMainDefOf.WNA_Weapon_RiteLance)
                        __result = true;
            }
        }
    }
}
