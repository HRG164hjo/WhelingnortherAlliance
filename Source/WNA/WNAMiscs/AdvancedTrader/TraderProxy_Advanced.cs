using RimWorld;
using System.Collections.Generic;
using Verse;
using WNA.WNAUtility.AdvancedTrader;

namespace WNA.WNAMiscs.AdvancedTrader
{
    internal class TraderProxy_Advanced : ITrader
    {
        public readonly ITrader inner;
        public readonly AdvancedTradeCart cart;
        public bool rebuilding;
        public System.Action dealResetCallback;

        private readonly Dictionary<Thing, BuyLine> lineOfThing = new Dictionary<Thing, BuyLine>();
        private readonly List<Thing> soldToTrader = new List<Thing>();

        public TraderProxy_Advanced(ITrader inner)
        {
            this.inner = inner;
            cart = new AdvancedTradeCart(new AdvancedTradeContext(inner));
        }
        public static ITrader Wrap(ITrader trader)
        {
            if (trader == null || trader is TraderProxy_Advanced)
                return trader;
            return new TraderProxy_Advanced(trader);
        }
        public static TraderProxy_Advanced Current => TradeSession.trader as TraderProxy_Advanced;
        public TraderKindDef TraderKind => inner.TraderKind;

        public int RandomPriceFactorSeed => inner.RandomPriceFactorSeed;

        public string TraderName => inner.TraderName;

        public bool CanTradeNow => inner.CanTradeNow;

        public float TradePriceImprovementOffsetForPlayer => inner.TradePriceImprovementOffsetForPlayer;

        public Faction Faction => inner.Faction;

        public TradeCurrency TradeCurrency => inner.TradeCurrency;

        public IEnumerable<Thing> ColonyThingsWillingToBuy(Pawn playerNegotiator)
        {
            return inner.ColonyThingsWillingToBuy(playerNegotiator);
        }

        public void GiveSoldThingToTrader(Thing toGive, int countToGive, Pawn playerNegotiator)
        {
            if (toGive == null || countToGive <= 0)
                return;
            if (!HoldsSoldThing(toGive))
            {
                inner.GiveSoldThingToTrader(toGive, countToGive, playerNegotiator);
                return;
            }
            Thing piece = toGive.SplitOff(countToGive);
            if (piece == null)
                return;
            piece.PreTraded(TradeAction.PlayerSells, playerNegotiator, inner);
            soldToTrader.Add(piece);
        }
        // Pawns keep the vanilla path because trader implementations run their own logic for them.
        private bool HoldsSoldThing(Thing toGive)
        {
            return cart.Context.ClearSoldGoodsOnClose && !TradeSession.giftMode && !(toGive is Pawn);
        }
        public int ClearSoldGoods()
        {
            int cleared = 0;
            for (int i = 0; i < soldToTrader.Count; i++)
            {
                Thing thing = soldToTrader[i];
                if (thing == null || thing.Destroyed || thing.stackCount <= 0)
                    continue;
                thing.Destroy(DestroyMode.Vanish);
                cleared++;
            }
            soldToTrader.Clear();
            return cleared;
        }
        public IEnumerable<Thing> Goods
        {
            get
            {
                foreach (Thing thing in cart.AllThings())
                    yield return thing;
                foreach (Thing good in inner.Goods)
                {
                    if (good == null || cart.HasLineFor(good.def, good.Stuff))
                        continue;
                    yield return good;
                }
            }
        }

        public void GiveSoldThingToPlayer(Thing toGive, int countToGive, Pawn playerNegotiator)
        {
            if (toGive == null || countToGive <= 0)
                return;
            if (!cart.Owns(toGive))
            {
                inner.GiveSoldThingToPlayer(toGive, countToGive, playerNegotiator);
                return;
            }
            AdvancedTradeUtility.DeliverToPlayer(inner, toGive, countToGive, playerNegotiator);
        }
        public void NotifyDealReset()
        {
            if (rebuilding)
                return;
            ApplyBuyCounts(TradeSession.deal);
            dealResetCallback?.Invoke();
        }
        public void ApplyBuyCounts(TradeDeal deal)
        {
            if (deal == null || cart.Empty)
                return;
            lineOfThing.Clear();
            List<BuyLine> lines = cart.Lines;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].IsEmpty)
                    continue;
                List<Thing> ts = lines[i].things;
                for (int j = 0; j < ts.Count; j++)
                    lineOfThing[ts[j]] = lines[i];
            }
            List<Tradeable> all = deal.AllTradeables;
            for (int i = 0; i < all.Count; i++)
            {
                Tradeable tradeable = all[i];
                if (tradeable.IsCurrency)
                    continue;
                Thing first = tradeable.FirstThingTrader;
                if (first != null && lineOfThing.TryGetValue(first, out BuyLine line))
                    tradeable.ForceToSource(line.Count);
            }
            lineOfThing.Clear();
        }
    }
}
