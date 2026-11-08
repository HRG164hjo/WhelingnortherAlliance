using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using WNA.WNAModExtension;

namespace WNA.WNARecipeWorker
{
    internal class Recipe_ProduceUnit : RecipeWorker
    {
        public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
        {
            base.Notify_IterationCompleted(billDoer, ingredients);
            RecipeExtension_ProduceUnit ext = recipe.GetModExtension<RecipeExtension_ProduceUnit>();
            if (ext?.kindDef == null)
            {
                Log.Error("WNA: Recipe " + recipe.defName + " has no RecipeExtension_ProduceUnit.kindDef.");
                return;
            }
            if (billDoer == null || !billDoer.Spawned || billDoer.Map == null)
                return;
            Pawn unit = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                kind: ext.kindDef,
                faction: billDoer.Faction,
                context: PawnGenerationContext.NonPlayer,
                tile: billDoer.Map.Tile,
                canGeneratePawnRelations: false,
                colonistRelationChanceFactor: 0f,
                allowFood: false,
                allowAddictions: false,
                forceNoIdeo: true,
                forceNoGear: true));
            if (!GenPlace.TryPlaceThing(unit, billDoer.Position, billDoer.Map, ThingPlaceMode.Near))
            {
                Log.Error("WNA: Could not place produced unit " + unit + " near " + billDoer + ".");
                Find.WorldPawns.PassToWorld(unit, PawnDiscardDecideMode.Discard);
            }
        }
    }

    internal class RecipeWorkerCounter_ProduceUnit : RecipeWorkerCounter
    {
        private PawnKindDef UnitKindDef => recipe.GetModExtension<RecipeExtension_ProduceUnit>()?.kindDef;

        public override bool CanCountProducts(Bill_Production bill)
        {
            return UnitKindDef != null;
        }

        public override int CountProducts(Bill_Production bill)
        {
            PawnKindDef kindDef = UnitKindDef;
            if (kindDef == null)
                return 0;
            int num = 0;
            foreach (Pawn pawn in bill.Map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
            {
                if (pawn.kindDef == kindDef)
                    num++;
            }
            return num;
        }

        public override bool CanPossiblyStore(Bill_Production bill, ISlotGroup slotGroup)
        {
            return true;
        }

        public override string ProductsDescription(Bill_Production bill)
        {
            return UnitKindDef?.LabelCap;
        }
    }
}
