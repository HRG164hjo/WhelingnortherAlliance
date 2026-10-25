using System;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using WNA.WNAWorldComp;

namespace WNA.WNAHarmony
{
    internal static class UpliftProtection
    {
        internal static bool IsProtected(Thing thing)
        {
            WorldComp_UpliftProtection comp = WorldComp_UpliftProtection.Instance;
            return comp != null && comp.IsProtected(thing);
        }

        internal static bool IsProtected(Pawn pawn)
        {
            WorldComp_UpliftProtection comp = WorldComp_UpliftProtection.Instance;
            return comp != null && comp.IsProtected(pawn);
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.ThreatDisabled))]
    internal static class Patch_Pawn_ThreatDisabled_Uplift
    {
        private static void Postfix(Pawn __instance, ref bool __result)
        {
            if (!__result && UpliftProtection.IsProtected(__instance))
                __result = true;
        }
    }

    [HarmonyPatch(typeof(AttackTargetFinder), nameof(AttackTargetFinder.IsAutoTargetable))]
    internal static class Patch_AttackTargetFinder_IsAutoTargetable_Uplift
    {
        private static void Postfix(IAttackTarget target, ref bool __result)
        {
            if (__result && target?.Thing != null && UpliftProtection.IsProtected(target.Thing))
                __result = false;
        }
    }

    [HarmonyPatch(typeof(Pawn_CarryTracker), nameof(Pawn_CarryTracker.TryStartCarry), new[] { typeof(Thing) })]
    internal static class Patch_Pawn_CarryTracker_TryStartCarry_Uplift
    {
        private static bool Prefix(Thing item, ref bool __result)
        {
            if (UpliftProtection.IsProtected(item))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn_CarryTracker), nameof(Pawn_CarryTracker.TryStartCarry), new[] { typeof(Thing), typeof(int), typeof(bool) })]
    internal static class Patch_Pawn_CarryTracker_TryStartCarryCount_Uplift
    {
        private static bool Prefix(Thing item, ref int __result)
        {
            if (UpliftProtection.IsProtected(item))
            {
                __result = 0;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(HaulAIUtility), nameof(HaulAIUtility.PawnCanAutomaticallyHaulFast_NewTemp))]
    internal static class Patch_HaulAIUtility_PawnCanAutomaticallyHaulFast_Uplift
    {
        private static void Postfix(Thing t, ref bool __result)
        {
            if (__result && UpliftProtection.IsProtected(t))
                __result = false;
        }
    }

    [HarmonyPatch(typeof(WorkGiver_HunterHunt), nameof(WorkGiver_HunterHunt.JobOnThing))]
    internal static class Patch_WorkGiver_HunterHunt_JobOnThing_Uplift
    {
        private static bool Prefix(Thing t, ref Job __result)
        {
            if (UpliftProtection.IsProtected(t))
            {
                __result = null;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(WorkGiver_PlantsCut), nameof(WorkGiver_PlantsCut.JobOnThing))]
    internal static class Patch_WorkGiver_PlantsCut_JobOnThing_Uplift
    {
        private static bool Prefix(Thing t, ref Job __result)
        {
            if (UpliftProtection.IsProtected(t))
            {
                __result = null;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(WorkGiver_GrowerHarvest), nameof(WorkGiver_GrowerHarvest.JobOnCell))]
    internal static class Patch_WorkGiver_GrowerHarvest_JobOnCell_Uplift
    {
        private static bool Prefix(Pawn pawn, IntVec3 c, ref Job __result)
        {
            if (pawn?.Map == null)
                return true;
            Plant plant = c.GetPlant(pawn.Map);
            if (plant != null && UpliftProtection.IsProtected(plant))
            {
                __result = null;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.TryStartAttack))]
    internal static class Patch_Pawn_TryStartAttack_Uplift
    {
        private static bool Prefix(Pawn __instance, ref bool __result)
        {
            if (UpliftProtection.IsProtected(__instance))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn_MeleeVerbs), nameof(Pawn_MeleeVerbs.TryMeleeAttack))]
    internal static class Patch_Pawn_MeleeVerbs_TryMeleeAttack_Uplift
    {
        private static bool Prefix(Pawn_MeleeVerbs __instance, ref bool __result)
        {
            if (__instance?.Pawn != null && UpliftProtection.IsProtected(__instance.Pawn))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
