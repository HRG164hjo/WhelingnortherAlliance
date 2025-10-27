using RimWorld;
using System.Collections.Generic;
using Verse;

namespace WNA.Recipe
{
    public class TargetedHarvest : Recipe_Surgery
    {
        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
        {
            return MedicalRecipesUtility.GetFixedPartsToApplyOn(recipe, pawn,
                (BodyPartRecord record) => !pawn.health.hediffSet.PartIsMissing(record));
        }
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (!bill.recipe.targetsBodyPart)
                bill.recipe.targetsBodyPart = true;
            bool isViolation = IsViolationOnPawn(pawn, part, Faction.OfPlayer);
            if (billDoer != null)
            {
                if (CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
                    return;
                TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);
            }
            Hediff_MissingPart missingPart = (Hediff_MissingPart)HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, pawn, part);
            missingPart.lastInjury = null;
            missingPart.IsFresh = true;
            missingPart.Part = part;
            if (part.parent != null && pawn.health.hediffSet.PartIsMissing(part.parent))
            {
                Log.Warning($"TargetedHarvest: Tried to remove {part.Label} but parent {part.parent.Label} is already missing.");
                return;
            }
            pawn.health.AddHediff(missingPart);
            pawn.Drawer?.renderer?.SetAllGraphicsDirty();
            if (isViolation)
                ReportViolation(pawn, billDoer, pawn.HomeFaction, -70);
        }
        public override bool IsViolationOnPawn(Pawn pawn, BodyPartRecord part, Faction billDoerFaction)
        {
            if ((pawn.Faction == billDoerFaction || pawn.Faction == null) && !pawn.IsQuestLodger())
                return false;
            return true;
        }
        public override string GetLabelWhenUsedOn(Pawn pawn, BodyPartRecord part)
        {
            return recipe.label;
        }
    }
}
