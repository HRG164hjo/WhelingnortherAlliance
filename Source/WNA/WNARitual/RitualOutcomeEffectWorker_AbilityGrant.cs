using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace WNA.WNARitual
{
    public class RitualOutcomeEffectWorker_AbilityGrant : RitualOutcomeEffectWorker
    {
        public RitualOutcomeEffectWorker_AbilityGrant()
        {
        }

        public RitualOutcomeEffectWorker_AbilityGrant(RitualOutcomeEffectDef def) : base(def)
        {
        }

        public override bool ApplyOnFailure => false;

        public override bool SupportsAttachableOutcomeEffect => false;

        public override bool ShowQuality => false;

        public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
        {
            LordJob_Ritual_AbilityGrant job = jobRitual as LordJob_Ritual_AbilityGrant;
            Pawn recipient = job?.recipient;
            AbilityDef def = job?.chosenAbility;
            if (recipient == null || def == null || recipient.Dead || recipient.abilities == null)
                return;
            if (!recipient.abilities.AllAbilitiesForReading.Any((Ability a) => a.def == def))
                recipient.abilities.GainAbility(def);
            Messages.Message("WNA_AbilityRitual_Granted".Translate(recipient.Named("PAWN"), def.LabelCap.Named("ABILITY")), recipient, MessageTypeDefOf.PositiveEvent, false);
        }
    }
}
