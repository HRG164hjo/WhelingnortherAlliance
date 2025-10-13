using HarmonyLib;
using RimWorld;

namespace WNA.WNAHarmony
{
    public class Patch_CompSpawner
    {
        [HarmonyPrefix]
        public static bool Prefix(CompSpawner __instance, int interval)
        {
            if (!__instance.PropsSpawner.requiresPower || __instance.parent.GetComp<CompPowerTrader>()?.PowerOn == true)
            {
                CompRefuelable refuelableComp = __instance.parent.GetComp<CompRefuelable>();
                if (refuelableComp != null && !refuelableComp.HasFuel)
                    return false;
                return true;
            }
            return false;
        }
    }
}
