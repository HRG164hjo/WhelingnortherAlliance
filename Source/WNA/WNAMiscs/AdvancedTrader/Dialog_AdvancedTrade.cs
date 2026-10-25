using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using WNA.WNAUtility.AdvancedTrader;

namespace WNA.WNAMiscs.AdvancedTrader
{
    internal class Dialog_AdvancedTrade : Dialog_Trade
    {
        private const float PanelGap = 12f;
        private const float MinRightPanelWidth = 700f;
        private const float MinLeftPanelWidth = 260f;
        private const float WindowChrome = 36f;
        private const float MinWindowWidth = MinRightPanelWidth + MinLeftPanelWidth + PanelGap + WindowChrome;
        private readonly TraderProxy_Advanced proxy;
        private readonly AdvancedTradeCatalogState catalogState = new AdvancedTradeCatalogState();
        private readonly Dictionary<BuyLine, Tradeable> lineTradeables = new Dictionary<BuyLine, Tradeable>();
        private readonly Dictionary<Thing, int> sellMemo = new Dictionary<Thing, int>();

        public Dialog_AdvancedTrade(Pawn playerNegotiator, ITrader trader, bool giftsOnly = false)
            : base(playerNegotiator, TraderProxy_Advanced.Wrap(trader), giftsOnly)
        {
            proxy = TradeSession.trader as TraderProxy_Advanced;
            if (proxy != null)
            {
                proxy.dealResetCallback = OnDealReset;
            }
            catalogState.structureChanged = RebuildDeal;
            catalogState.countsChanged = ApplyCountsOnly;
        }
        public override Vector2 InitialSize
        {
            get
            {
                float minWidth = Mathf.Min(MinWindowWidth, UI.screenWidth);
                float width = Mathf.Clamp(UI.screenWidth * 0.94f, minWidth, 1920f);
                float height = Mathf.Clamp(UI.screenHeight * 0.96f, 640f, UI.screenHeight);
                return new Vector2(width, height);
            }
        }
        private static float LeftPanelWidth(float totalWidth)
        {
            float right = Mathf.Max(MinRightPanelWidth, totalWidth * 0.6f);
            return Mathf.Max(MinLeftPanelWidth, totalWidth - right - PanelGap);
        }
        protected override Rect QuickSearchWidgetRect(Rect winRect, Rect inRect)
        {
            float leftWidth = LeftPanelWidth(inRect.width);
            return new Rect(inRect.x + 6f, inRect.y + 6f,
                Mathf.Min(QuickSearchSize.x, leftWidth - 12f), QuickSearchSize.y);
        }
        public override void Notify_CommonSearchChanged()
        {
            base.Notify_CommonSearchChanged();
            catalogState.MarkDirty();
        }

        public override void PreOpen()
        {
            base.PreOpen();
            AdvancedTradeUtility.BeginPricingSession();
            StuffSelectionUtility.BeginSession();
            if (proxy != null)
            {
                catalogState.matchedCount = AdvancedTradeCatalog.Search(
                    CommonSearchWidget.filter, catalogState.category,
                    proxy.cart.Context.config, catalogState.results);
                catalogState.dirty = false;
                CacheLineTradeables();
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            float leftWidth = LeftPanelWidth(inRect.width);
            Rect leftRect = new Rect(inRect.x, inRect.y, leftWidth, inRect.height);
            Rect rightRect = new Rect(leftRect.xMax + PanelGap, inRect.y,
                inRect.xMax - leftRect.xMax - PanelGap, inRect.height);
            if (proxy != null)
                AdvancedTradeUI.DrawCatalogPanel(leftRect, catalogState, CommonSearchWidget.filter, proxy.cart);
            GUI.BeginGroup(rightRect);
            try
            {
                base.DoWindowContents(new Rect(0f, 0f, rightRect.width, rightRect.height));
            }
            finally
            {
                GUI.EndGroup();
            }
            SyncLineCountsFromDeal();
        }

        public override void Close(bool doCloseSound = true)
        {
            ITrader inner = proxy?.inner;
            if (proxy != null)
            {
                proxy.dealResetCallback = null;
                proxy.cart.Clear();
            }
            lineTradeables.Clear();
            AdvancedTradeUtility.EndPricingSession();
            StuffSelectionUtility.EndSession();
            base.Close(doCloseSound);
            if (inner is Pawn pawn && pawn.mindState.hasQuest)
                TradeUtility.ReceiveQuestFromTrader(pawn, TradeSession.playerNegotiator);
        }
        private void RebuildDeal()
        {
            TradeDeal deal = TradeSession.deal;
            if (deal == null)
                return;
            sellMemo.Clear();
            List<Tradeable> all = deal.AllTradeables;
            for (int i = 0; i < all.Count; i++)
            {
                Tradeable tradeable = all[i];
                if (tradeable.IsCurrency || tradeable.CountToTransfer == 0)
                    continue;
                List<Thing> things = tradeable.thingsColony;
                for (int j = 0; j < things.Count; j++)
                    sellMemo[things[j]] = tradeable.CountToTransfer;
            }
            proxy.rebuilding = true;
            try
            {
                deal.Reset();
            }
            finally
            {
                proxy.rebuilding = false;
            }
            all = deal.AllTradeables;
            for (int i = 0; i < all.Count; i++)
            {
                Tradeable tradeable = all[i];
                if (tradeable.IsCurrency)
                    continue;
                List<Thing> things = tradeable.thingsColony;
                for (int j = 0; j < things.Count; j++)
                {
                    if (sellMemo.TryGetValue(things[j], out int count) && count != 0)
                    {
                        tradeable.ForceTo(count);
                        break;
                    }
                }
            }
            proxy.ApplyBuyCounts(deal);
            deal.UpdateCurrencyCount();
            CacheLineTradeables();
            base.Notify_CommonSearchChanged();
        }
        private void ApplyCountsOnly()
        {
            TradeDeal deal = TradeSession.deal;
            if (deal == null)
                return;
            proxy.ApplyBuyCounts(deal);
            deal.UpdateCurrencyCount();
            CacheLineTradeables();
        }
        private void OnDealReset()
        {
            CacheLineTradeables();
        }
        private void CacheLineTradeables()
        {
            lineTradeables.Clear();
            TradeDeal deal = TradeSession.deal;
            if (deal == null || TradeSession.giftMode)
                return;
            List<Tradeable> all = deal.AllTradeables;
            for (int i = 0; i < all.Count; i++)
            {
                Tradeable tradeable = all[i];
                if (tradeable.IsCurrency || tradeable.thingsTrader.Count == 0)
                    continue;
                BuyLine line = proxy.cart.LineOf(tradeable.FirstThingTrader);
                if (line != null)
                    lineTradeables[line] = tradeable;
            }
        }
        private void SyncLineCountsFromDeal()
        {
            if (lineTradeables.Count == 0 || TradeSession.giftMode)
                return;
            foreach (KeyValuePair<BuyLine, Tradeable> pair in lineTradeables)
            {
                Tradeable tradeable = pair.Value;
                if (tradeable == null || tradeable.CountToTransfer < 0)
                    continue;
                pair.Key.SyncCountFrom(tradeable.CountToTransfer);
            }
        }
    }
}
