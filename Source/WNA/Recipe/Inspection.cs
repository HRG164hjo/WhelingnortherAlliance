using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace WNA.Recipe
{
    public class Inspection : Recipe_Surgery
    {
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            TaggedString label = "WNA.Inspection.LetterLabel".Translate();
            if (CheckSurgeryFail(billDoer, pawn, ingredients, part, bill)) return;
            var hediffs = pawn.health.hediffSet.hediffs;
            StringBuilder sb = new StringBuilder();
            var visible = hediffs.Where(h => h.Visible &&
                !(h is Hediff_AddedPart) &&
                !(h is Hediff_Implant) &&
                !(h is Hediff_MissingPart) &&
                !(h is Hediff_Injury)).ToList();
            if (visible.Any())
            {
                sb.AppendLine("WNA.Inspection.Visible".Translate(pawn.Named("PAWN")));
                foreach (var h in visible.OrderBy(h => h.LabelCap.ResolveTags()))
                {
                    string labelv = h.LabelCap.NullOrEmpty() ? h.def.label : h.LabelCap;
                    sb.AppendLine($"\t{labelv} ({h.def.defName})");
                }
            }
            var hidden = hediffs.Where(h => !h.Visible).ToList();
            if (hidden.Any())
            {
                sb.AppendLine("WNA.Inspection.Hidden".Translate(pawn.Named("PAWN")));
                foreach (var h in hidden.OrderBy(h => h.LabelCap.ResolveTags()))
                {
                    string labelh = h.LabelCap.NullOrEmpty() ? h.def.label : h.LabelCap;
                    sb.AppendLine($"\t{labelh} ({h.def.defName})");
                }
            }
            if (visible.Any() || hidden.Any())
            {
                TaggedString text = sb.ToString();
                Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.NeutralEvent, pawn);
            }
            else
                Find.LetterStack.ReceiveLetter(label,
                    "WNA.Inspection.None".Translate(pawn.Named("PAWN")),
                    LetterDefOf.NeutralEvent, pawn);
            if (billDoer != null) TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);
        }
    }
}
