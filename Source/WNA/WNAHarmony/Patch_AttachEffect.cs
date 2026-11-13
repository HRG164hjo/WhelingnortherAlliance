using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using Verse;
using WNA.WNAUtility;

namespace WNA.WNAHarmony
{
    internal static class Patch_AttachEffect
    {
        [HarmonyPatch(typeof(Pawn), nameof(Pawn.PreApplyDamage))]
        internal static class Patch_Pawn_PreApplyDamage
        {
            [HarmonyPostfix]
            internal static void Postfix(Pawn __instance, ref DamageInfo dinfo, ref bool absorbed)
            {
                AttachEffectUtility.TryAbsorb(__instance, ref dinfo, ref absorbed);
            }
        }

        [HarmonyPatch(typeof(Verb), nameof(Verb.TryStartCastOn), new Type[]
        {
            typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(bool), typeof(bool), typeof(bool), typeof(bool)
        })]
        internal static class Patch_Verb_TryStartCastOn
        {
            [HarmonyPrefix]
            internal static bool Prefix(Verb __instance, ref bool __result)
            {
                if (!AttachEffectUtility.BlocksVerb(__instance))
                    return true;
                __result = false;
                return false;
            }
        }

        // Non-virtual choke point: catches verb subclasses whose Available/TryStartCastOn overrides skip base.
        [HarmonyPatch(typeof(Verb), "TryCastNextBurstShot")]
        internal static class Patch_Verb_TryCastNextBurstShot
        {
            [HarmonyPrefix]
            internal static bool Prefix(Verb __instance)
            {
                if (!AttachEffectUtility.BlocksVerb(__instance))
                    return true;
                __instance.Reset();
                return false;
            }
        }

        [HarmonyPatch(typeof(Verb), nameof(Verb.Available))]
        internal static class Patch_Verb_Available
        {
            [HarmonyPrefix]
            internal static bool Prefix(Verb __instance, ref bool __result)
            {
                if (!AttachEffectUtility.BlocksVerb(__instance))
                    return true;
                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(Pawn), nameof(Pawn.TryStartAttack))]
        internal static class Patch_Pawn_TryStartAttack
        {
            [HarmonyPrefix]
            internal static bool Prefix(Pawn __instance, ref bool __result)
            {
                if (!AttachEffectUtility.BlocksAttack(__instance))
                    return true;
                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(Pawn_MeleeVerbs), nameof(Pawn_MeleeVerbs.TryMeleeAttack))]
        internal static class Patch_Pawn_MeleeVerbs_TryMeleeAttack
        {
            [HarmonyPrefix]
            internal static bool Prefix(Pawn_MeleeVerbs __instance, ref bool __result)
            {
                if (!AttachEffectUtility.BlocksAttack(__instance?.Pawn))
                    return true;
                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(Ability), "get_CanCast")]
        internal static class Patch_Ability_CanCast
        {
            [HarmonyPrefix]
            internal static bool Prefix(Ability __instance, ref AcceptanceReport __result)
            {
                if (__instance?.pawn == null || !AttachEffectUtility.BlocksAbility(__instance.pawn))
                    return true;
                __result = "WNA_AE_BlockedAbility".Translate();
                return false;
            }
        }

        [HarmonyPatch(typeof(Ability), nameof(Ability.GizmoDisabled))]
        internal static class Patch_Ability_GizmoDisabled
        {
            [HarmonyPrefix]
            internal static bool Prefix(Ability __instance, ref bool __result, ref string reason)
            {
                if (__instance?.pawn == null || !AttachEffectUtility.BlocksAbility(__instance.pawn))
                    return true;
                reason = "WNA_AE_BlockedAbility".Translate();
                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(Ability), nameof(Ability.Activate), new Type[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) })]
        internal static class Patch_Ability_ActivateLocal
        {
            [HarmonyPrefix]
            internal static bool Prefix(Ability __instance, ref bool __result)
            {
                if (__instance?.pawn == null || !AttachEffectUtility.BlocksAbility(__instance.pawn))
                    return true;
                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(Ability), nameof(Ability.Activate), new Type[] { typeof(GlobalTargetInfo) })]
        internal static class Patch_Ability_ActivateGlobal
        {
            [HarmonyPrefix]
            internal static bool Prefix(Ability __instance, ref bool __result)
            {
                if (__instance?.pawn == null || !AttachEffectUtility.BlocksAbility(__instance.pawn))
                    return true;
                __result = false;
                return false;
            }
        }
    }
}