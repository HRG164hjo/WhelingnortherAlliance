using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using WNA.WNAUtility;

namespace WNA.WNAHarmony
{
    internal class WNA_PCC_Hostility
    {
        private static bool IsProtectedPair(Faction a, Faction b)
        {
            return a != null && b != null && General.IsInternalFaction(a) && General.IsInternalFaction(b);
        }
        private static bool IsProtectedPair(Thing a, Thing b)
        {
            if (a == null || b == null) return false;
            return IsProtectedPair(a.Faction, b.Faction);
        }
        private static bool IsProtectedPair(Pawn a, Pawn b)
        {
            if (a == null || b == null) return false;
            return IsProtectedPair(a.Faction, b.Faction);
        }
        [HarmonyPatch(typeof(GenHostility), nameof(GenHostility.HostileTo), new[] { typeof(Thing), typeof(Thing) })]
        internal static class Patch_GenHostility_HostileTo_ThingThing
        {
            private static void Postfix(Thing a, Thing b, ref bool __result)
            {
                if (!__result) return;
                if (IsProtectedPair(a, b))
                    __result = false;
            }
        }
        [HarmonyPatch(typeof(GenHostility), nameof(GenHostility.HostileTo), new[] { typeof(Thing), typeof(Faction) })]
        internal static class Patch_GenHostility_HostileTo_ThingFaction
        {
            private static void Postfix(Thing t, Faction fac, ref bool __result)
            {
                if (!__result) return;
                if (t == null || fac == null) return;
                if (IsProtectedPair(t.Faction, fac))
                    __result = false;
            }
        }
        [HarmonyPatch(typeof(JobGiver_ReactToCloseMeleeThreat), "TryGiveJob")]
        internal static class Patch_JobGiver_ReactToCloseMeleeThreat_TryGiveJob
        {
            private static void Postfix(Pawn pawn, ref Job __result)
            {
                if (pawn == null) return;
                Pawn threat = pawn.mindState?.meleeThreat;
                if (threat == null) return;
                if (IsProtectedPair(pawn, threat))
                {
                    pawn.mindState.meleeThreat = null;
                    if (__result != null && __result.def == JobDefOf.AttackMelee)
                        __result = null;
                }
            }
        }
        [HarmonyPatch(typeof(Verb_MeleeAttack), "TryCastShot")]
        internal static class Patch_Verb_MeleeAttack_TryCastShot
        {
            private static void Postfix(Verb_MeleeAttack __instance)
            {
                Pawn caster = __instance?.CasterPawn;
                if (caster == null) return;
                LocalTargetInfo currentTarget = Traverse.Create(__instance).Field("currentTarget").GetValue<LocalTargetInfo>();
                if (!(currentTarget.Thing is Pawn targetPawn))
                    return;
                if (IsProtectedPair(caster, targetPawn))
                {
                    if (targetPawn.mindState != null && targetPawn.mindState.meleeThreat == caster)
                        targetPawn.mindState.meleeThreat = null;
                }
            }
        }
    }
}
