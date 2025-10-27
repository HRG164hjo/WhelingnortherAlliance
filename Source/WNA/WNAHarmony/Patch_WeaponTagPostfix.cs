using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace WNA.WNAHarmony
{
    public class Patch_WeaponTagPostfix
    {
        [HarmonyPatch(typeof(VerbProperties), nameof(VerbProperties.AdjustedMeleeDamageAmount))]
        [HarmonyPatch(new Type[] { typeof(Verb), typeof(Pawn) })]
        public static class Patch_AdjustedMeleeDamageAmount
        {
            public static void Postfix(ref float __result, Verb ownerVerb, Pawn attacker)
            {
                if (attacker == null || ownerVerb.EquipmentSource == null)
                    return;
                ThingDef weaponDef = ownerVerb.EquipmentSource.def;
                if (weaponDef.weaponTags == null || !weaponDef.weaponTags.Contains("Tag_WNACivilian"))
                    return;
                float consciousness = attacker.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness);
                float damageFactor = Math.Max(1f, consciousness);
                __result *= damageFactor;
            }
        }
    }
}
