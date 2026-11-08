using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WNA.WNARecipeWorker
{
    internal class Recipe_RiteDemolition : RecipeWorker
    {
        private readonly List<ThingDefCountClass> pendingRefunds = new List<ThingDefCountClass>();

        public override void ConsumeIngredient(Thing ingredient, RecipeDef recipe, Map map)
        {
            CaptureRefund(ingredient);
            base.ConsumeIngredient(ingredient, recipe, map);
        }

        public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
        {
            base.Notify_IterationCompleted(billDoer, ingredients);
            if (pendingRefunds.Count == 0)
                return;
            if (billDoer == null || !billDoer.Spawned || billDoer.Map == null)
            {
                pendingRefunds.Clear();
                return;
            }
            for (int i = 0; i < pendingRefunds.Count; i++)
            {
                ThingDefCountClass refund = pendingRefunds[i];
                if (refund?.thingDef == null || refund.count <= 0)
                    continue;
                Thing thing = ThingMaker.MakeThing(refund.thingDef);
                thing.stackCount = refund.count;
                if (!GenPlace.TryPlaceThing(thing, billDoer.Position, billDoer.Map, ThingPlaceMode.Near))
                {
                    Log.Error("WNA: recipe " + recipe?.defName + " could not place refund " + thing + " near " + billDoer + ".");
                    thing.Destroy();
                }
            }
            pendingRefunds.Clear();
        }

        private void CaptureRefund(Thing ingredient)
        {
            if (ingredient == null)
                return;
            // Read the inner thing before base.ConsumeIngredient destroys the minified wrapper and its contents.
            Thing source = ingredient.GetInnerIfMinified() ?? ingredient;
            ThingDef def = source.def;
            if (def == null)
                return;
            List<ThingDefCountClass> costs = def.CostListAdjusted(def.MadeFromStuff ? source.Stuff : null, errorOnNullStuff: false);
            if (costs.NullOrEmpty())
                return;
            int multiplier = ingredient.stackCount > 0 ? ingredient.stackCount : 1;
            for (int i = 0; i < costs.Count; i++)
            {
                ThingDefCountClass cost = costs[i];
                if (cost?.thingDef == null || cost.count <= 0)
                    continue;
                pendingRefunds.Add(new ThingDefCountClass(cost.thingDef, cost.count * multiplier));
            }
        }
    }
}
