using RimWorld;
using System.Collections.Generic;
using Verse;

namespace WNA.Recipe
{
    public class Inspection : Recipe_Surgery
    {
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
            {
                return;
            }
            string desc;
            SurgicalInspectionOutcome surgicalInspectionOutcome = pawn.DoSurgicalInspection(billDoer, out desc);
            if (surgicalInspectionOutcome != SurgicalInspectionOutcome.DetectedNoLetter)
            {
                TaggedString label = "LetterSurgicallyInspectedLabel".Translate();
                TaggedString text = "LetterSurgicallyInspectedHeader".Translate(billDoer.Named("DOCTOR"), pawn.Named("PATIENT"));
                if (surgicalInspectionOutcome == SurgicalInspectionOutcome.Nothing)
                {
                    text += " " + "LetterSurgicallyInspectedNothing".Translate(billDoer.Named("DOCTOR"), pawn.Named("PATIENT")).CapitalizeFirst();
                }
                else
                {
                    text += desc;
                }
                Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.NeutralEvent, pawn);
            }
            if (billDoer != null)
            {
                TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);
            }
        }
    }
}
