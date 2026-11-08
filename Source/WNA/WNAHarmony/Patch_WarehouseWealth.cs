using HarmonyLib;
using RimWorld;
using Verse;
using WNA.WNAThingComp;

namespace WNA.WNAHarmony
{
    internal class Patch_WealthWatcher
    {
        [HarmonyPatch(typeof(WealthWatcher), "WealthItemsFilter")]
        internal class Patch_WealthItemsFilter
        {
            [HarmonyPostfix]
            private static void PostFix(IThingHolder x, ref bool __result)
            {
                if (!__result)
                {
                    return;
                }
                CompStorageCargo hold = x as CompStorageCargo;
                if (hold == null)
                {
                    hold = (x as ThingWithComps)?.TryGetComp<CompStorageCargo>();
                }
                if (hold != null && !hold.Props.countForWealth)
                {
                    __result = false;
                }
            }
        }
    }
}
