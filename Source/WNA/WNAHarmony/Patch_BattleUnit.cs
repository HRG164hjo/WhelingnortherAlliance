using HarmonyLib;
using RimWorld;
using System;
using Verse;
using WNA.WNAThingClass;

namespace WNA.WNAHarmony
{
    internal class Patch_BattleUnit
    {
        private static bool IsBattleUnitInventory(ThingOwner owner)
        {
            return owner?.Owner is Pawn_InventoryTracker tracker && tracker.pawn is ThingClass_BattleUnit;
        }

        [HarmonyPatch(typeof(ThingOwner<Thing>), nameof(ThingOwner<Thing>.TryAdd), new Type[] { typeof(Thing), typeof(bool) })]
        internal static class Patch_InventoryTryAdd
        {
            [HarmonyPrefix]
            internal static bool Prefix(ThingOwner __instance, ref bool __result)
            {
                if (!IsBattleUnitInventory(__instance))
                    return true;
                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(ThingOwner<Thing>), nameof(ThingOwner<Thing>.TryAdd), new Type[] { typeof(Thing), typeof(int), typeof(bool) })]
        internal static class Patch_InventoryTryAddCount
        {
            [HarmonyPrefix]
            internal static bool Prefix(ThingOwner __instance, ref int __result)
            {
                if (!IsBattleUnitInventory(__instance))
                    return true;
                __result = 0;
                return false;
            }
        }

        [HarmonyPatch(typeof(MechanitorUtility), nameof(MechanitorUtility.InMechanitorCommandRange))]
        internal static class Patch_InMechanitorCommandRange
        {
            [HarmonyPrefix]
            internal static bool Prefix(Pawn mech, ref bool __result)
            {
                if (!(mech is ThingClass_BattleUnit))
                    return true;
                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(PawnAttackGizmoUtility), "CanOrderPlayerPawn")]
        internal static class Patch_CanOrderPlayerPawn
        {
            [HarmonyPostfix]
            internal static void Postfix(Pawn pawn, ref bool __result)
            {
                if (!__result && pawn is ThingClass_BattleUnit && pawn.Faction != null && pawn.Faction.IsPlayer)
                    __result = true;
            }
        }
    }
}
