using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using WNA.WNAModExtension;
using WNA.WNAUtility;

namespace WNA.WNAHarmony
{
    internal class Patch_Roof
    {
        private static T GetPrivateField<T>(Type classType, string fieldName)
        {
            try
            {
                return (T)classType.GetField(fieldName, BindingFlags.NonPublic |
                    BindingFlags.Static)?.GetValue(null);
            }
            catch
            {
                Log.Error($"[WNAHarmony] 无法通过反射获取字段 {fieldName} 的值。");
                return default;
            }
        }
        private static readonly IntRange ThinRoofCrushDamageRange = GetPrivateField<IntRange>(typeof(RoofCollapserImmediate), "ThinRoofCrushDamageRange");
        private static bool IgnoresRoof(Verb verb)
        {
            return TechnoConfig.Get(verb?.GetProjectile())?.ignoreRoof == true;
        }
        // Stands in for VerbUtility.ProjectileFliesOverhead inside try-start-shoot; answering false
        // skips the "overhead projectile + roofed position" guard without changing LOS targeting.
        private static bool ProjectileFliesOverheadForFiring(Verb verb)
        {
            return VerbUtility.ProjectileFliesOverhead(verb) && !IgnoresRoof(verb);
        }
        [HarmonyPatch(typeof(Building_TurretGun), nameof(Building_TurretGun.TryStartShootSomething))]
        internal static class Patch_BuildingTurretGun_TryStartShootSomething
        {
            [HarmonyTranspiler]
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo original = AccessTools.Method(typeof(VerbUtility), nameof(VerbUtility.ProjectileFliesOverhead));
                MethodInfo replacement = AccessTools.Method(typeof(Patch_Roof), nameof(ProjectileFliesOverheadForFiring));
                int replaced = 0;
                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo method && method == original)
                    {
                        instruction.operand = replacement;
                        replaced++;
                    }
                    yield return instruction;
                }
                if (replaced == 0)
                {
                    Log.ErrorOnce("[WNA] ProjectileFliesOverhead call not found in Building_TurretGun.TryStartShootSomething; ignoreRoof firing stays blocked.", 40251313);
                }
            }
        }
        [HarmonyPatch]
        internal static class Patch_BuildingTurretGun_IsMortarOrProjectileFliesOverhead
        {
            [HarmonyTargetMethod]
            internal static MethodBase TargetMethod()
            {
                return AccessTools.PropertyGetter(typeof(Building_TurretGun), "IsMortarOrProjectileFliesOverhead");
            }
            [HarmonyPostfix]
            internal static void Postfix(Building_TurretGun __instance, ref bool __result)
            {
                if (__result && IgnoresRoof(__instance.AttackVerb))
                {
                    __result = false;
                }
            }
        }
        [HarmonyPatch(typeof(Skyfaller), "HitRoof")]
        internal static class Patch_Skyfaller_HitRoof
        {
            internal static bool Prefix(Skyfaller __instance)
            {
                TechnoConfig ext = TechnoConfig.Get(__instance?.def);
                if (ext?.ignoreRoof == true)
                    return false;
                return true;
            }
        }
        [HarmonyPatch(typeof(RoofCollapserImmediate), "DropRoofInCellPhaseOne")]
        internal static class Patch_RoofCollapserImmediate_PhaseOne
        {
            [HarmonyPrefix]
            internal static bool SkipImmunePawns(IntVec3 c, Map map, List<Thing> outCrushedThings)
            {
                if (!c.Roofed(map)) return true;
                var things = c.GetThingList(map);
                bool hasImmunePawns = false;
                var filteredThings = new List<Thing>();
                foreach (var thing in things)
                {
                    if (thing is Pawn pawn && General.IsWNARace(pawn))
                        hasImmunePawns = true;
                    else filteredThings.Add(thing);
                }
                if (!hasImmunePawns)
                    return true;
                foreach (var thing in filteredThings)
                {
                    if (thing.def.destroyable)
                    {
                        outCrushedThings?.Add(thing);
                        float damageAmount = ThinRoofCrushDamageRange.RandomInRange;
                        thing.TakeDamage(new DamageInfo(DamageDefOf.Crush, damageAmount));
                    }
                }
                return false;
            }
        }
    }
}
