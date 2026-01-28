using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using System.Reflection;
using Verse;
using static WNA.WNAHarmony.Patch_Plant;

namespace WNAHarmony
{
    [StaticConstructorOnStartup]
    public class WNAHarmony
    {
        static WNAHarmony()
        {
            var harmony = new Harmony("hrg164hjo.whelingnorther.alliance");
            harmony.PatchAll();
            Log.Message("[WNA] Harmony patch success.");
            /* ######## */
            MethodInfo originalGetter = AccessTools.PropertyGetter(typeof(Plant), nameof(Plant.GrowthRate));
            MethodInfo prefix = AccessTools.Method(typeof(ForGrowthRate), nameof(ForGrowthRate.Prefix));
            foreach (Type type in GenTypes.AllTypes.Where(t => t.IsSubclassOf(typeof(Plant))))
            {
                PropertyInfo prop = type.GetProperty(nameof(Plant.GrowthRate), BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
                if (prop != null)
                {
                    MethodInfo subGetter = prop.GetGetMethod();
                    if (subGetter != null)
                    {
                        harmony.Patch(subGetter, prefix: new HarmonyMethod(prefix));
                    }
                }
            }
            harmony.Patch(originalGetter, prefix: new HarmonyMethod(prefix));
            harmony.Patch(typeof(Pawn_HealthTracker).GetMethod("ShouldBeDeadFromLethalDamageThreshold"), null, new HarmonyMethod(typeof(WNAHarmony), "LethalDamageThreshold"));
        }
        public static void LethalDamageThreshold(ref bool __result)
        {
            __result = false;
        }
    }
}
