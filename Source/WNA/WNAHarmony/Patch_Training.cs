using HarmonyLib;
using RimWorld;
using System.Reflection;
using Verse;

namespace WNA.WNAHarmony
{
    [HarmonyPatch(typeof(Pawn_TrainingTracker), "TrainingTrackerTickRare")]
    public class Patch_Training
    {
        private static readonly FieldInfo _countDecayFrom = AccessTools.Field(typeof(Pawn_TrainingTracker), "countDecayFrom");
        private static readonly FieldInfo _steps = AccessTools.Field(typeof(Pawn_TrainingTracker), "steps");
        private static readonly FieldInfo _learned = AccessTools.Field(typeof(Pawn_TrainingTracker), "learned");
        [HarmonyPrefix]
        static bool Prefix(Pawn_TrainingTracker __instance)
        {
            Pawn pawn = __instance.pawn;
            if (pawn?.def != null &&
                pawn.health.hediffSet.HasHediff(HediffDef.Named("WNA_InAnimal")))
            {
                DefMap<TrainableDef, int> steps = (DefMap<TrainableDef, int>)_steps.GetValue(__instance);
                DefMap<TrainableDef, bool> learned = (DefMap<TrainableDef, bool>)_learned.GetValue(__instance);

                foreach (TrainableDef td in DefDatabase<TrainableDef>.AllDefsListForReading)
                {
                    steps[td] = td.steps;
                    learned[td] = true;
                }
                _steps.SetValue(__instance, steps);
                _learned.SetValue(__instance, learned);
                _countDecayFrom.SetValue(__instance, Find.TickManager.TicksGame);
                return false;
            }
            return true;
        }
    }
}
