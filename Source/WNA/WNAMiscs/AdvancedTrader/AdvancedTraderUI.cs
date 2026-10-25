using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using WNA.WNAUtility.AdvancedTrader;

namespace WNA.WNAMiscs.AdvancedTrader
{
    [StaticConstructorOnStartup]
    internal static class AdvancedTradeUI
    {
        public const float RowHeight = 30f;
        public const float SearchBar = 34f;
        private const float HeaderHeight = 22f;
        private const float TabHeight = 24f;
        private const float TabGap = 4f;
        private const float TabsPerRow = 4f;
        private const float FooterHeight = 28f;
        private const float IconSize = 26f;
        private const float Gap = 2f;
        private const float SmallButtonHeight = 22f;
        private const float StuffWidth = 64f;
        private const float PriceWidth = 68f;
        private const float OrderButtonWidth = 64f;
        private const float QtyWidth = 84f;
        private const float QtyButtonWidth = 22f;
        private static readonly Color SelectedTabColor = new Color(1f, 1f, 1f, 0.16f);
        private static readonly Color OrderedRowColor = new Color(0.35f, 0.9f, 0.4f, 0.09f);

        public static void DrawCatalogPanel(Rect panelRect, AdvancedTradeCatalogState state,
            QuickSearchFilter filter, AdvancedTradeCart cart)
        {
            AdvancedTradeContext context = cart.Context;
            if (state.dirty)
            {
                state.dirty = false;
                state.matchedCount = AdvancedTradeCatalog.Search(filter, state.category, context.config, state.results);
            }
            Widgets.BeginGroup(panelRect);
            Rect rect = panelRect.AtZero();
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            float y = SearchBar;
            DrawHeader(new Rect(rect.x, y, rect.width, HeaderHeight), state);
            y += HeaderHeight;
            y = DrawCategoryTabs(new Rect(rect.x, y, rect.width, TabHeight * 2f + TabGap), state);
            Rect footerRect = new Rect(rect.x, rect.yMax - FooterHeight, rect.width, FooterHeight);
            DrawFooter(footerRect, state, cart, context);
            Rect listRect = new Rect(rect.x, y + TabGap, rect.width, footerRect.y - TabGap - y - TabGap);
            DrawList(listRect, state, cart, context);
            GUI.color = Color.gray;
            Widgets.DrawLineVertical(rect.xMax - 1f, rect.y, rect.height);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.EndGroup();
        }

        private static void DrawHeader(Rect rect, AdvancedTradeCatalogState state)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(rect, "ATUI_CatalogTitle".Translate() + "   " +
                "ATUI_CatalogCount".Translate(state.matchedCount.ToStringCached(), AdvancedTradeCatalog.BaseCount.ToStringCached()));
            TooltipHandler.TipRegionByKey(rect, "ATUI_CatalogTitleTip");
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static float DrawCategoryTabs(Rect rect, AdvancedTradeCatalogState state)
        {
            AdvancedTradeCategory[] categories = AdvancedTradeCatalog.AllCategories;
            float tabWidth = (rect.width - TabGap * (TabsPerRow - 1f)) / TabsPerRow;
            for (int i = 0; i < categories.Length; i++)
            {
                AdvancedTradeCategory category = categories[i];
                int col = i % (int)TabsPerRow;
                int row = i / (int)TabsPerRow;
                Rect tabRect = new Rect(rect.x + (float)col * (tabWidth + TabGap),
                    rect.y + (float)row * (TabHeight + TabGap), tabWidth, TabHeight);
                bool selected = state.category == category;
                if (selected)
                {
                    Widgets.DrawBoxSolid(tabRect, SelectedTabColor);
                }
                string label = ("ATUI_Cat_" + category).Translate();
                if (Widgets.ButtonText(tabRect, label, drawBackground: !selected))
                {
                    if (state.category != category)
                    {
                        state.category = category;
                        state.MarkDirty();
                    }
                }
            }
            return rect.y + TabHeight * 2f + TabGap;
        }

        private static void DrawFooter(Rect rect, AdvancedTradeCatalogState state,
            AdvancedTradeCart cart, AdvancedTradeContext context)
        {
            List<BuyLine> lines = cart.Lines;
            int totalUnits = 0;
            for (int i = 0; i < lines.Count; i++)
                totalUnits += lines[i].Count;
            Rect labelRect = new Rect(rect.x, rect.y, rect.width - 74f, rect.height);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = (lines.Count > 0) ? Color.white : Color.gray;
            Widgets.Label(labelRect, "ATUI_OrderSummary".Translate(lines.Count.ToStringCached(), totalUnits.ToStringCached()));
            GUI.color = Color.white;
            Rect clearRect = new Rect(rect.xMax - 70f, rect.y + (rect.height - SmallButtonHeight) / 2f, 70f, SmallButtonHeight);
            if (Widgets.ButtonText(clearRect, "ATUI_ClearOrder".Translate(), active: lines.Count > 0))
            {
                cart.Clear();
                state.structureChanged?.Invoke();
            }
            TooltipHandler.TipRegionByKey(clearRect, "ATUI_ClearOrderTip");
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static void DrawList(Rect listRect, AdvancedTradeCatalogState state,
            AdvancedTradeCart cart, AdvancedTradeContext context)
        {
            if (listRect.height <= 8f)
            {
                return;
            }
            List<ThingDef> results = state.results;
            if (results.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.gray;
                Widgets.Label(listRect, "ATUI_NoResults".Translate());
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }
            Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, (float)results.Count * RowHeight + 6f);
            Widgets.BeginScrollView(listRect, ref state.scrollPosition, viewRect);
            float rowY = 3f;
            float minY = state.scrollPosition.y - RowHeight;
            float maxY = state.scrollPosition.y + listRect.height;
            for (int i = 0; i < results.Count; i++)
            {
                if (rowY > minY && rowY < maxY)
                    DrawCatalogRow(new Rect(0f, rowY, viewRect.width, RowHeight), results[i], i, state, cart, context);
                rowY += RowHeight;
            }
            Widgets.EndScrollView();
        }
        private static void DrawCatalogRow(Rect rect, ThingDef def, int index,
            AdvancedTradeCatalogState state, AdvancedTradeCart cart, AdvancedTradeContext context)
        {
            ThingDef stuff = state.StuffFor(def, cart, context);
            BuyLine line = cart.Find(def, stuff);
            if (index % 2 == 1)
                Widgets.DrawLightHighlight(rect);
            if (line != null)
                Widgets.DrawBoxSolid(rect, OrderedRowColor);
            float xMax = rect.xMax - Gap;
            Rect actionRect;
            if (line != null)
                actionRect = new Rect(xMax - QtyWidth, rect.y + (RowHeight - SmallButtonHeight) / 2f, QtyWidth, SmallButtonHeight);
            else
                actionRect = new Rect(xMax - OrderButtonWidth, rect.y + (RowHeight - SmallButtonHeight) / 2f, OrderButtonWidth, SmallButtonHeight);
            xMax = actionRect.x - Gap;
            Rect priceRect = new Rect(xMax - PriceWidth, rect.y, PriceWidth, RowHeight);
            xMax = priceRect.x - Gap;
            bool showStuff = StuffSelectionUtility.HasStuffChoice(def, context);
            Rect stuffRect = Rect.zero;
            if (showStuff)
            {
                stuffRect = new Rect(xMax - StuffWidth, rect.y + (RowHeight - SmallButtonHeight) / 2f, StuffWidth, SmallButtonHeight);
                xMax = stuffRect.x - Gap;
            }
            Rect iconRect = new Rect(rect.x + Gap, rect.y + (RowHeight - IconSize) / 2f, IconSize, IconSize);
            Rect nameRect = new Rect(iconRect.xMax + Gap, rect.y, Mathf.Max(20f, xMax - iconRect.xMax - Gap), RowHeight);
            Widgets.ThingIcon(iconRect, def, stuff);
            TooltipHandler.TipRegion(iconRect, new TipSignal(delegate
            {
                return def.LabelCap.RawText + "\n\n" + def.description;
            }, def.shortHash * 297));
            if (Widgets.ButtonInvisible(iconRect))
                Find.WindowStack.Add(new Dialog_InfoCard(def, stuff));
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(nameRect.x, nameRect.y + 1f, nameRect.width, 17f),
                def.LabelCap.RawText.Truncate(nameRect.width));
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Widgets.Label(new Rect(nameRect.x, nameRect.y + 15f, nameRect.width, 14f),
                SubLabelFor(def, stuff).Truncate(nameRect.width));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            float unitPrice = AdvancedTradeUtility.UnitBuyPrice(def, stuff, context);
            Text.Anchor = TextAnchor.MiddleRight;
            if (unitPrice > 0f)
            {
                Widgets.Label(priceRect, unitPrice.ToStringMoney());
            }
            else
            {
                GUI.color = Color.gray;
                Widgets.Label(priceRect, "-");
                GUI.color = Color.white;
            }
            TooltipHandler.TipRegionByKey(priceRect, "ATUI_PriceTip");
            Text.Anchor = TextAnchor.MiddleLeft;
            if (showStuff)
            {
                if (Widgets.ButtonText(stuffRect, "ATUI_StuffButton".Translate()))
                {
                    ThingDef def1 = def;
                    ThingDef current = stuff;
                    StuffSelectionUtility.OpenStuffMenu(def, current, context, delegate (ThingDef picked)
                    {
                        state.SetStuffChoice(def1, picked);
                        BuyLine existing = cart.Find(def1, current);
                        if (existing != null)
                        {
                            cart.ChangeStuff(existing, picked);
                            state.structureChanged?.Invoke();
                        }
                    });
                }
                TooltipHandler.TipRegionByKey(stuffRect, "ATUI_StuffButtonTip");
            }
            if (line == null)
            {
                if (Widgets.ButtonText(actionRect, "ATUI_Order".Translate()))
                {
                    cart.Add(def, stuff);
                    state.structureChanged?.Invoke();
                }
                TooltipHandler.TipRegion(actionRect, new TipSignal(delegate
                {
                    return "ATUI_OrderTip".Translate(def.LabelCap.RawText);
                }, def.shortHash * 299));
            }
            else
            {
                int step = StepFor(def, context);
                Rect minusRect = new Rect(actionRect.x, actionRect.y, QtyButtonWidth, SmallButtonHeight);
                Rect numRect = new Rect(minusRect.xMax, actionRect.y, actionRect.width - QtyButtonWidth * 2f, SmallButtonHeight);
                Rect plusRect = new Rect(numRect.xMax, actionRect.y, QtyButtonWidth, SmallButtonHeight);
                if (Widgets.ButtonText(minusRect, "-"))
                {
                    Notify(state, cart.SetCount(line, line.Count - step));
                }
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(numRect, line.Count.ToStringCached());
                Text.Anchor = TextAnchor.MiddleLeft;
                if (Widgets.ButtonText(plusRect, "+"))
                {
                    Notify(state, cart.SetCount(line, line.Count + step));
                }
                TooltipHandler.TipRegion(numRect, new TipSignal(delegate
                {
                    return "ATUI_QtyTip".Translate(line.Count.ToStringCached(),
                        line.Capacity.ToStringCached(), line.Cap.ToStringCached());
                }, def.shortHash * 301));
            }

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        private static void Notify(AdvancedTradeCatalogState state, bool structureChanged)
        {
            if (structureChanged)
            {
                state.structureChanged?.Invoke();
            }
            else
            {
                state.countsChanged?.Invoke();
            }
        }
        private static string SubLabelFor(ThingDef def, ThingDef stuff)
        {
            string sub = (stuff != null) ? stuff.LabelCap.RawText : "";
            if (def.Minifiable)
            {
                string tag = "ATUI_TagMinified".Translate();
                sub = sub.NullOrEmpty() ? tag : (sub + " · " + tag);
            }
            return sub.NullOrEmpty() ? def.defName : sub;
        }
        private static int StepFor(ThingDef def, AdvancedTradeContext context)
        {
            if (def.Minifiable || def.stackLimit <= 1)
            {
                return 1;
            }
            int configured = context.config?.defaultStackableCount ?? 25;
            return Mathf.Clamp(configured, 1, 100);
        }
    }
    internal class AdvancedTradeCatalogState
    {
        public AdvancedTradeCategory category = AdvancedTradeCategory.All;
        public readonly List<ThingDef> results = new List<ThingDef>();
        public readonly Dictionary<ThingDef, ThingDef> stuffChoices = new Dictionary<ThingDef, ThingDef>();
        public Vector2 scrollPosition;
        public bool dirty = true;
        public int matchedCount;
        public Action structureChanged;
        public Action countsChanged;
        public void MarkDirty()
        {
            dirty = true;
        }
        public ThingDef StuffFor(ThingDef def, AdvancedTradeCart cart, AdvancedTradeContext context)
        {
            if (def == null || !def.MadeFromStuff)
                return null;
            if (stuffChoices.TryGetValue(def, out ThingDef chosen) && chosen != null)
                return chosen;
            List<BuyLine> lines = cart.Lines;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].def == def)
                    return lines[i].stuff;
            }
            return StuffSelectionUtility.DefaultStuff(def, context);
        }
        public void SetStuffChoice(ThingDef def, ThingDef stuff)
        {
            stuffChoices[def] = stuff;
        }
    }
}
