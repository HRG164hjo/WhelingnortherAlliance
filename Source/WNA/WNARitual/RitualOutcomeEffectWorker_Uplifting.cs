using System.Collections.Generic;
using RimWorld;
using Verse;
using WNA.WNAUtility;

namespace WNA.WNARitual
{
    public class RitualOutcomeEffectWorker_Uplifting : RitualOutcomeEffectWorker
    {
        public override bool ApplyOnFailure => false;

        public override bool SupportsAttachableOutcomeEffect => false;

        public override bool ShowQuality => false;

        public RitualOutcomeEffectWorker_Uplifting()
        {
        }

        public RitualOutcomeEffectWorker_Uplifting(RitualOutcomeEffectDef def) : base(def)
        {
        }

        public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
        {
            UpliftUtility.CompleteUplift(jobRitual as LordJob_Ritual_Uplifting);
        }

        public override void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                FillCompData();
        }
    }
}
