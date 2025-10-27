using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace WNA.WNAHarmony
{
    public class Patch_Placeworker
    {
        [HarmonyPatch(typeof(PlaceWorker_ShowTurretRadius))]
        [HarmonyPatch("AllowsPlacing")]
        public static class PatchPlaceWorker_ShowTurretRadius
        {
            private static readonly List<Type> CustomVerbTypes = new List<Type>
            {
                typeof(Verb_Shoot),
                typeof(Verb_Spray),
                typeof(WNAMiscs.VerbType_Burst), 
            };
            public static bool Prefix(BuildableDef checkingDef, IntVec3 loc, ref AcceptanceReport __result)
            {
                ThingDef buildingDef = checkingDef as ThingDef;
                ThingDef gunDef = buildingDef?.building?.turretGunDef;
                if (gunDef == null) return true;
                VerbProperties vanillaVerb = gunDef.Verbs.FirstOrDefault(v =>
                    v.verbClass == typeof(Verb_Shoot) ||
                    typeof(Verb_Spray).IsAssignableFrom(v.verbClass));
                if (vanillaVerb != null) return true;
                VerbProperties customVerb = gunDef.Verbs.FirstOrDefault(v =>
                    CustomVerbTypes.Any(baseType =>
                    baseType.IsAssignableFrom(v.verbClass)));
                if (customVerb != null)
                {
                    if (customVerb.range > 0f) GenDraw.DrawRadiusRing(loc, customVerb.range);
                    if (customVerb.minRange > 0f) GenDraw.DrawRadiusRing(loc, customVerb.minRange);
                    __result = true;
                    return false;
                }
                return true;
            }
        }
    }
}
