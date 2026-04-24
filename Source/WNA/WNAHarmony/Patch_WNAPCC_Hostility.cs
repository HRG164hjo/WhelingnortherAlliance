using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using WNA.WNADefOf;

namespace WNA.WNAHarmony
{
    public class Patch_WNAPCC_Hostility
    {
        private static class InternalPeaceUtility
        {
            public static bool IsPeaceFaction(Faction f)
            {
                return f?.def != null
                    && (f.def == WNAMainDefOf.WNA_FactionWNA
                        || f.def == WNAMainDefOf.WNA_FactionPCC);
            }
            public static bool IsProtectedPair(Faction a, Faction b)
            {
                return a != null && b != null && IsPeaceFaction(a) && IsPeaceFaction(b);
            }
            public static bool IsProtectedPair(Thing a, Thing b)
            {
                if (a == null || b == null) return false;
                return IsProtectedPair(a.Faction, b.Faction);
            }
            public static bool IsProtectedPair(Pawn a, Pawn b)
            {
                if (a == null || b == null) return false;
                return IsProtectedPair(a.Faction, b.Faction);
            }
        }
        [HarmonyPatch(typeof(GenHostility), nameof(GenHostility.HostileTo), new[] { typeof(Thing), typeof(Thing) })]
        public static class Patch_GenHostility_HostileTo_ThingThing
        {
            private static void Postfix(Thing a, Thing b, ref bool __result)
            {
                if (!__result) return;
                if (InternalPeaceUtility.IsProtectedPair(a, b))
                    __result = false;
            }
        }
        [HarmonyPatch(typeof(GenHostility), nameof(GenHostility.HostileTo), new[] { typeof(Thing), typeof(Faction) })]
        public static class Patch_GenHostility_HostileTo_ThingFaction
        {
            private static void Postfix(Thing t, Faction fac, ref bool __result)
            {
                if (!__result) return;
                if (t == null || fac == null) return;
                if (InternalPeaceUtility.IsProtectedPair(t.Faction, fac))
                    __result = false;
            }
        }
        [HarmonyPatch(typeof(JobGiver_ReactToCloseMeleeThreat), "TryGiveJob")]
        public static class Patch_JobGiver_ReactToCloseMeleeThreat_TryGiveJob
        {
            private static void Postfix(Pawn pawn, ref Job __result)
            {
                if (pawn == null) return;
                Pawn threat = pawn.mindState?.meleeThreat;
                if (threat == null) return;
                if (InternalPeaceUtility.IsProtectedPair(pawn, threat))
                {
                    pawn.mindState.meleeThreat = null;
                    if (__result != null && __result.def == JobDefOf.AttackMelee)
                        __result = null;
                }
            }
        }
        [HarmonyPatch(typeof(Verb_MeleeAttack), "TryCastShot")]
        public static class Patch_Verb_MeleeAttack_TryCastShot
        {
            private static void Postfix(Verb_MeleeAttack __instance)
            {
                Pawn caster = __instance?.CasterPawn;
                if (caster == null) return;
                LocalTargetInfo currentTarget = Traverse.Create(__instance).Field("currentTarget").GetValue<LocalTargetInfo>();
                if (!(currentTarget.Thing is Pawn targetPawn))
                    return;
                if (InternalPeaceUtility.IsProtectedPair(caster, targetPawn))
                {
                    if (targetPawn.mindState != null && targetPawn.mindState.meleeThreat == caster)
                        targetPawn.mindState.meleeThreat = null;
                }
            }
        }
    }
}
