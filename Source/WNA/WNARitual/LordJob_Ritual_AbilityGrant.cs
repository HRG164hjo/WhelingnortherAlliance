using System.Collections.Generic;
using RimWorld;
using Verse;
using WNA.WNAUtility;

namespace WNA.WNARitual
{
    public class LordJob_Ritual_AbilityGrant : LordJob_Ritual
    {
        private const int TicksBeforeChoice = 3 * GenTicks.TicksPerRealSecond;

        public Pawn recipient;
        public AbilityDef chosenAbility;

        private int ticksWaited;
        private bool selectionMade;
        private Dialog_AbilityChoice openWindow;

        public LordJob_Ritual_AbilityGrant()
        {
        }

        public LordJob_Ritual_AbilityGrant(
            TargetInfo selectedTarget,
            Precept_Ritual ritual,
            RitualObligation obligation,
            List<RitualStage> allStages,
            RitualRoleAssignments assignments)
            : base(selectedTarget, ritual, obligation, allStages, assignments, assignments?.FirstAssignedPawn(AbilityGrantUtility.RecipientRoleId))
        {
            recipient = assignments?.FirstAssignedPawn(AbilityGrantUtility.RecipientRoleId);
            if (recipient == null && assignments != null)
            {
                List<Pawn> participants = assignments.Participants;
                if (participants.Count > 0)
                    recipient = participants[0];
            }
        }

        public override void LordJobTick()
        {
            base.LordJobTick();
            if (ended || selectionMade)
                return;
            if (recipient == null || recipient.Destroyed || recipient.Dead)
            {
                selectionMade = true;
                Cancel();
                return;
            }
            ticksWaited++;
            if (ticksWaited < TicksBeforeChoice)
                return;
            if (openWindow == null || !Find.WindowStack.IsOpen(openWindow))
                OpenChoiceWindow();
        }

        private void OpenChoiceWindow()
        {
            List<AbilityDef> abilities = AbilityGrantUtility.SelectableAbilities(recipient);
            if (abilities.Count == 0)
            {
                Messages.Message("WNA_AbilityRitual_NoAbilities".Translate(recipient.Named("PAWN")), recipient, MessageTypeDefOf.NeutralEvent, false);
                selectionMade = true;
                Cancel();
                return;
            }
            openWindow = new Dialog_AbilityChoice(recipient, abilities, OnAbilityChosen, OnChoiceCancelled);
            Find.WindowStack.Add(openWindow);
        }

        private void OnAbilityChosen(AbilityDef def)
        {
            openWindow = null;
            if (selectionMade || ended)
                return;
            selectionMade = true;
            chosenAbility = def;
            if (recipient != null && !recipient.Dead && recipient.abilities != null)
                recipient.abilities.GainAbility(def);
            ApplyOutcome(1f);
        }

        private void OnChoiceCancelled()
        {
            openWindow = null;
            if (selectionMade || ended)
                return;
            selectionMade = true;
            Cancel();
        }

        public override void Cleanup()
        {
            if (openWindow != null)
            {
                Dialog_AbilityChoice window = openWindow;
                openWindow = null;
                window.Dismiss();
            }
            base.Cleanup();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref recipient, "abilityGrantRecipient");
            Scribe_Defs.Look(ref chosenAbility, "abilityGrantChosen");
            Scribe_Values.Look(ref ticksWaited, "abilityGrantTicksWaited", 0);
            Scribe_Values.Look(ref selectionMade, "abilityGrantSelectionMade", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                openWindow = null;
        }
    }
}
