using RimWorld;
using Verse;

namespace WNA.WNAMiscs.IngredValGetter
{
    internal class NutriMisc : IngredientValueGetter
    {
        public override float ValuePerUnitOf(ThingDef t)
        {
            float nutritionValue = StatUtility.GetStatValueFromList(t.statBases, StatDefOf.Nutrition, 0f);
            if (nutritionValue >= 0.05f)
                return nutritionValue;
            return 0.05f;
        }
        public override string BillRequirementsDescription(RecipeDef r, IngredientCount ing)
        {
            return ing.GetBaseCount() + "x " + "BillNutrition".Translate() + " (" + ing.filter.Summary + ")";
        }
    }
}
