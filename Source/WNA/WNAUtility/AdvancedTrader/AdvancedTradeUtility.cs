using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;
using WNA.WNAMiscs.AdvancedTrader;
using WNA.WNAModExtension;

namespace WNA.WNAUtility.AdvancedTrader
{
    internal class AdvancedTradeUtility
    {
        private static readonly Dictionary<(ThingDef def, ThingDef stuff), Tradeable> priceReps =
            new Dictionary<(ThingDef def, ThingDef stuff), Tradeable>();
        public static void BeginPricingSession()
        {
            priceReps.Clear();
        }
        public static void EndPricingSession()
        {
            priceReps.Clear();
        }
        public static float UnitBuyPrice(ThingDef def, ThingDef stuff, AdvancedTradeContext context)
        {
            if (def == null || !TradeSession.Active)
            {
                return 0f;
            }
            var key = (def, stuff);
            if (!priceReps.TryGetValue(key, out Tradeable rep))
            {
                Thing thing = MakePriceRepThing(def, stuff);
                if (thing == null)
                {
                    return 0f;
                }
                if (context != null && context.UseFixedQuality)
                {
                    CompQuality quality = thing.TryGetComp<CompQuality>();
                    if (quality != null)
                        quality.SetQuality(context.FixedQuality, ArtGenerationContext.Outsider);
                }
                rep = new Tradeable();
                rep.AddThing(thing, Transactor.Trader);
                priceReps[key] = rep;
            }
            return rep.GetPriceFor(TradeAction.PlayerBuys);
        }
        private static Thing MakePriceRepThing(ThingDef def, ThingDef stuff)
        {
            ThingDef stuffArg = def.MadeFromStuff ? stuff : null;
            if (def.Minifiable)
            {
                // MakeThing on a minifiable def yields an empty MinifiedThing, and Tradeable resolves
                // AnyThing through its inner thing, so the inner thing has to be filled in.
                Thing inner = ThingMaker.MakeThing(def, stuffArg);
                Thing outer = ThingMaker.MakeThing(def.minifiedDef);
                if (inner == null || outer is not MinifiedThing minified)
                {
                    return null;
                }
                minified.InnerThing = inner;
                return minified;
            }
            Thing thing = ThingMaker.MakeThing(def, stuffArg);
            if (thing == null)
            {
                return null;
            }
            thing.stackCount = 1;
            return thing;
        }
        public static bool IsAdvancedTrader(ITrader trader)
        {
            return trader?.TraderKind?.GetModExtension<ExtendedTrader>() != null;
        }
        public static PlanetTile ResolveTraderTile(ITrader trader)
        {
            if (trader is Thing thing)
            {
                PlanetTile t = thing.Tile;
                if (t.Valid)
                    return t;
                if (thing.SpawnedOrAnyParentSpawned && thing.MapHeld != null)
                    return thing.MapHeld.Tile;
            }
            if (trader is WorldObject worldObject && worldObject.Tile.Valid)
                return worldObject.Tile;
            Pawn negotiator = TradeSession.playerNegotiator;
            if (negotiator != null && negotiator.Tile.Valid)
                return negotiator.Tile;
            Map map = Find.AnyPlayerHomeMap;
            return (map != null) ? map.Tile : PlanetTile.Invalid;
        }
        public static void DeliverToPlayer(ITrader inner, Thing toGive, int countToGive, Pawn playerNegotiator)
        {
            Thing piece = toGive.SplitOff(countToGive);
            if (piece == null)
                return;
            piece.PreTraded(TradeAction.PlayerBuys, playerNegotiator, inner);
            Caravan caravan = playerNegotiator?.GetCaravan();
            if (caravan != null)
            {
                if (piece is Pawn pawn)
                {
                    caravan.AddPawn(pawn, addCarriedPawnToWorldPawnsIfAny: true);
                    return;
                }
                CaravanInventoryUtility.GiveThing(caravan, piece);
                return;
            }
            Map map = playerNegotiator?.MapHeld;
            IntVec3 cell = IntVec3.Invalid;
            if (inner is Thing innerThing && innerThing.SpawnedOrAnyParentSpawned)
            {
                cell = innerThing.PositionHeld;
                map = innerThing.MapHeld ?? map;
            }
            if (map == null)
                map = Find.CurrentMap;
            if (map == null)
            {
                Log.Error("[AdvancedTrade] Could not deliver bought thing " + piece + ": no map and no caravan.");
                piece.Destroy(DestroyMode.Vanish);
                return;
            }
            if (inner is TradeShip || !cell.IsValid)
            {
                cell = DropCellFinder.TradeDropSpot(map);
                TradeUtility.SpawnDropPod(cell, map, piece);
                return;
            }
            PlaceChunked(piece, cell, map);
        }
        private static void PlaceChunked(Thing piece, IntVec3 cell, Map map)
        {
            int limit = piece.def.stackLimit;
            while (limit > 0 && piece != null && piece.stackCount > limit)
            {
                Thing chunk = piece.SplitOff(limit);
                if (!GenPlace.TryPlaceThing(chunk, cell, map, ThingPlaceMode.Near))
                    TradeUtility.SpawnDropPod(DropCellFinder.TradeDropSpot(map), map, chunk);
            }
            if (piece == null)
                return;
            if (GenPlace.TryPlaceThing(piece, cell, map, ThingPlaceMode.Near))
                return;
            TradeUtility.SpawnDropPod(DropCellFinder.TradeDropSpot(map), map, piece);
        }
    }
}
