using RimWorld;
using Verse;

namespace WNA.WNAMiscs
{
    public class IngredValGetter_MarketValue : IngredientValueGetter
    {
        public override float ValuePerUnitOf(ThingDef t)
        {
            float marketValue = StatUtility.GetStatValueFromList(t.statBases, StatDefOf.MarketValue, 0f);
            if (marketValue >= 0.05f)
                return marketValue;
            return 0.5f;
        }
        public override string BillRequirementsDescription(RecipeDef r, IngredientCount ing)
        {
            return ing.GetBaseCount() + "x " + "WNA_BillMarketValue".Translate() + " (" + ing.filter.Summary + ")";
        }
    }
}
