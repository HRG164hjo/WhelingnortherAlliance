using System.Collections.Generic;
using RimWorld;
using Verse;
using WNA.WNAUtility;

namespace WNA.WNARitual
{
    public class RitualOutcomeEffectWorker_WorldPawnWarp : RitualOutcomeEffectWorker
    {
        public override bool ApplyOnFailure => false;

        public override bool SupportsAttachableOutcomeEffect => false;

        public override bool ShowQuality => false;

        public RitualOutcomeEffectWorker_WorldPawnWarp()
        {
        }

        public RitualOutcomeEffectWorker_WorldPawnWarp(RitualOutcomeEffectDef def) : base(def)
        {
        }

        public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
        {
            Map map = jobRitual?.Map ?? Find.AnyPlayerHomeMap;
            IntVec3 cell = IntVec3.Invalid;
            if (jobRitual != null && jobRitual.selectedTarget.IsValid)
                cell = jobRitual.selectedTarget.Cell;
            WorldPawnWarpUtility.BeginSelection(map, cell);
        }

        public override void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                FillCompData();
        }
    }
}
