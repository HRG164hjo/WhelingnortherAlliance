using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace WNA.WNAMiscs
{
    public class StockGenerator_BuyAnything : StockGenerator
    {
        public const float priceMin = 0.5f;
        public override IEnumerable<Thing> GenerateThings(PlanetTile forTile, Faction faction = null)
        {
            return Enumerable.Empty<Thing>();
        }
        public override bool HandlesThingDef(ThingDef thingDef)
        {
            if (thingDef.tradeability == Tradeability.None) return true;
            float marketValue = thingDef.BaseMarketValue;
            if (marketValue >= 0f) return true;
            return true;
        }
        public override Tradeability TradeabilityFor(ThingDef thingDef)
        {
            if (!HandlesThingDef(thingDef)) return Tradeability.None;
            return Tradeability.All;
        }
    }
}
