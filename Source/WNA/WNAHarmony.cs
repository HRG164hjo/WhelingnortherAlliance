using HarmonyLib;
using Verse;

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
        }
    }
}
