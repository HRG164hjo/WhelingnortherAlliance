using HarmonyLib;
using Verse;

namespace WNAHarmony
{
    [StaticConstructorOnStartup]
    public class HarmonyPatch
    {
        static HarmonyPatch()
        {
            new Harmony("hrg164hjo.whelingnorther.alliance").PatchAll();
        }
    }
}
