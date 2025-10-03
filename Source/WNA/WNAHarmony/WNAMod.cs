using HarmonyLib;
using System.Reflection;
using Verse;

namespace WNA.WNAHarmony
{
    [StaticConstructorOnStartup]
    public class WNAMod : Mod
    {
        public WNAMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("harmony.hrg164hjo.whelingnorther.alliance");
            MethodInfo original = HarmonyLib.AccessTools.Method(
                typeof(Verse.AI.JobDriver),
                "EndJobWith",
                new System.Type[] { typeof(Verse.AI.JobCondition) }
            );
            MethodInfo postfix = HarmonyLib.AccessTools.Method(
                typeof(Patch_JobMaditate.JobDriver_Meditate_EndJobWith),
                "Postfix"
            );
            if (original != null && postfix != null)
                harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            else
                Log.Error("[WNA] Harmony Patch Failed: Could not find original or patch method for EndJobWith.");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
