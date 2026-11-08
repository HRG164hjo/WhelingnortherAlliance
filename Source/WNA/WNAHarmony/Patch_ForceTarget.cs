using HarmonyLib;
using RimWorld;
using Verse;
using WNA.WNAThingComp;

namespace WNA.WNAHarmony
{
    // CanSetForcedTarget and CanToggleHoldFire are hard-coded to require CompMannable.
    // Turrets carrying CompForceTargetable opt in without becoming mannable.
    [HarmonyPatch(typeof(Building_TurretGun), "get_CanSetForcedTarget")]
    internal static class Patch_BuildingTurretGun_CanSetForcedTarget
    {
        [HarmonyPostfix]
        private static void Postfix(Building_TurretGun __instance, ref bool __result)
        {
            if (!__result && __instance.TryGetComp<CompForceTargetable>() != null)
            {
                __result = true;
            }
        }
    }
    [HarmonyPatch(typeof(Building_TurretGun), "get_CanToggleHoldFire")]
    internal static class Patch_BuildingTurretGun_CanToggleHoldFire
    {
        [HarmonyPostfix]
        private static void Postfix(Building_TurretGun __instance, ref bool __result)
        {
            if (!__result && __instance.TryGetComp<CompForceTargetable>() != null)
            {
                __result = true;
            }
        }
    }
}
