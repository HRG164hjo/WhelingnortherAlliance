using RimWorld;
using Verse;

namespace WNA.WNAMiscs
{
    public class IngredValGetter_Mass : IngredientValueGetter
    {
        public override float ValuePerUnitOf(ThingDef t)
        {
            float massValue = StatUtility.GetStatValueFromList(t.statBases, StatDefOf.Mass, 0f);
            if (massValue >= 0.05f)
                return massValue;
            return 0.05f;
        }
        public override string BillRequirementsDescription(RecipeDef r, IngredientCount ing)
        {
            return ing.GetBaseCount() + "x " + "WNA_BillMass".Translate() + " (" + ing.filter.Summary + "kg)";
        }
    }
}
