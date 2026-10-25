using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using WNA.WNAUtility;

namespace WNA.WNARitual
{
    public class Dialog_AbilityChoice : Window
    {
        private const float ListEntryHeight = 28f;
        private const float CellGap = 6f;
        private const float CellHeight = 92f;
        private const int Columns = 3;
        private const float PageBarHeight = 32f;
        private const float ListWidth = 210f;

        private class SourceEntry
        {
            public readonly string Key;
            public readonly string Label;
            public readonly int Order;
            public readonly List<AbilityDef> Abilities = new List<AbilityDef>();

            public SourceEntry(string key, string label, int order)
            {
                Key = key;
                Label = label;
                Order = order;
            }
        }

        private class CategoryEntry
        {
            public readonly AbilityCategoryDef Category;
            public readonly string Label;
            public readonly int Order;
            public readonly int Count;

            public CategoryEntry(AbilityCategoryDef category, string label, int order, int count)
            {
                Category = category;
                Label = label;
                Order = order;
                Count = count;
            }
        }

        private readonly Pawn recipient;
        private readonly List<AbilityDef> abilities;
        private readonly Action<AbilityDef> onChosen;
        private readonly Action onCancelled;

        private readonly List<SourceEntry> sources = new List<SourceEntry>();
        private readonly List<CategoryEntry> categories = new List<CategoryEntry>();
        private readonly List<AbilityDef> filtered = new List<AbilityDef>();
        private readonly Dictionary<string, string> sourceLabelsByKey = new Dictionary<string, string>();
        private readonly Dictionary<AbilityCategoryDef, string> categoryLabels = new Dictionary<AbilityCategoryDef, string>();

        private bool hasSourceFilter;
        private string selectedSourceKey;
        private bool hasCategoryFilter;
        private AbilityCategoryDef selectedCategory;
        private int categoryTotal;
        private int page;
        private int pageSize = 1;
        private Vector2 sourcesScroll;
        private Vector2 categoriesScroll;
        private bool closedByChoice;

        public override Vector2 InitialSize =>
            new Vector2(Mathf.Min(1120f, UI.screenWidth - 80f), Mathf.Min(760f, UI.screenHeight - 80f));

        public Dialog_AbilityChoice(Pawn recipient, List<AbilityDef> abilities, Action<AbilityDef> onChosen, Action onCancelled)
        {
            this.recipient = recipient;
            this.abilities = abilities ?? new List<AbilityDef>();
            this.onChosen = onChosen;
            this.onCancelled = onCancelled;

            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            closeOnCancel = false;
            closeOnAccept = false;
            doCloseX = false;
            onlyOneOfTypeAllowed = true;
            openMenuOnCancel = false;

            BuildSources();
            BuildCategories();
            RebuildFilter(resetPage: true);
        }

        private static string KeyOf(AbilityDef def)
        {
            return def?.modContentPack?.PackageId ?? string.Empty;
        }

        private string LabelOfSource(string key)
        {
            if (sourceLabelsByKey.TryGetValue(key, out string label))
                return label;
            return key;
        }

        private string LabelOfCategory(AbilityCategoryDef category)
        {
            if (category == null)
                return "WNA_AbilityRitual_Uncategorized".Translate().Resolve();
            if (categoryLabels.TryGetValue(category, out string label))
                return label;
            label = category.LabelCap.Resolve();
            categoryLabels[category] = label;
            return label;
        }

        private void BuildSources()
        {
            sources.Clear();
            sourceLabelsByKey.Clear();
            Dictionary<string, SourceEntry> map = new Dictionary<string, SourceEntry>();
            for (int i = 0; i < abilities.Count; i++)
            {
                AbilityDef def = abilities[i];
                if (def == null)
                    continue;
                string key = KeyOf(def);
                if (!map.TryGetValue(key, out SourceEntry entry))
                {
                    entry = new SourceEntry(key, AbilityGrantUtility.SourceLabel(def), AbilityGrantUtility.SourceOrder(key));
                    map.Add(key, entry);
                    sourceLabelsByKey[key] = entry.Label;
                }
                entry.Abilities.Add(def);
            }
            sources.AddRange(map.Values);
            sources.Sort((a, b) =>
            {
                int c = a.Order.CompareTo(b.Order);
                return c != 0 ? c : string.Compare(a.Label, b.Label, StringComparison.CurrentCulture);
            });
        }

        private void BuildCategories()
        {
            categories.Clear();
            categoryLabels.Clear();
            Dictionary<AbilityCategoryDef, int> counts = new Dictionary<AbilityCategoryDef, int>();
            int total = 0;
            int uncategorized = 0;
            for (int i = 0; i < abilities.Count; i++)
            {
                AbilityDef def = abilities[i];
                if (def == null)
                    continue;
                if (hasSourceFilter && KeyOf(def) != selectedSourceKey)
                    continue;
                total++;
                if (def.category == null)
                {
                    uncategorized++;
                    continue;
                }
                counts.TryGetValue(def.category, out int count);
                counts[def.category] = count + 1;
            }
            categoryTotal = total;
            if (uncategorized > 0)
                categories.Add(new CategoryEntry(null, "WNA_AbilityRitual_Uncategorized".Translate().Resolve(), int.MaxValue, uncategorized));
            foreach (KeyValuePair<AbilityCategoryDef, int> pair in counts)
            {
                string label = pair.Key.LabelCap.Resolve();
                categoryLabels[pair.Key] = label;
                categories.Add(new CategoryEntry(pair.Key, label, pair.Key.displayOrder, pair.Value));
            }
            categories.Sort((a, b) =>
            {
                int c = a.Order.CompareTo(b.Order);
                return c != 0 ? c : string.Compare(a.Label, b.Label, StringComparison.CurrentCulture);
            });
            if (hasCategoryFilter && selectedCategory != null && !counts.ContainsKey(selectedCategory))
            {
                hasCategoryFilter = false;
                selectedCategory = null;
            }
        }

        private void RebuildFilter(bool resetPage)
        {
            filtered.Clear();
            for (int i = 0; i < abilities.Count; i++)
            {
                AbilityDef def = abilities[i];
                if (def == null)
                    continue;
                if (hasSourceFilter && KeyOf(def) != selectedSourceKey)
                    continue;
                if (hasCategoryFilter && def.category != selectedCategory)
                    continue;
                filtered.Add(def);
            }
            filtered.Sort(CompareAbilities);
            if (resetPage)
                page = 0;
        }

        private int CompareAbilities(AbilityDef a, AbilityDef b)
        {
            string keyA = KeyOf(a);
            string keyB = KeyOf(b);
            int c = AbilityGrantUtility.SourceOrder(keyA).CompareTo(AbilityGrantUtility.SourceOrder(keyB));
            if (c != 0)
                return c;
            c = string.Compare(LabelOfSource(keyA), LabelOfSource(keyB), StringComparison.CurrentCulture);
            if (c != 0)
                return c;
            int orderA = a.category?.displayOrder ?? int.MaxValue;
            int orderB = b.category?.displayOrder ?? int.MaxValue;
            c = orderA.CompareTo(orderB);
            if (c != 0)
                return c;
            c = a.displayOrder.CompareTo(b.displayOrder);
            if (c != 0)
                return c;
            return string.Compare(a.LabelCap.Resolve(), b.LabelCap.Resolve(), StringComparison.CurrentCulture);
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            GUI.color = Color.white;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f),
                "WNA_AbilityRitual_WindowTitle".Translate(recipient.Named("PAWN")).Resolve());

            Text.Font = GameFont.Small;
            GUI.color = new Color(0.75f, 0.75f, 0.75f);
            Widgets.Label(new Rect(inRect.x, inRect.y + 30f, inRect.width, 22f), "WNA_AbilityRitual_WindowSubtitle".Translate().Resolve());
            GUI.color = Color.white;

            const float footerHeight = 38f;
            Rect content = new Rect(inRect.x, inRect.y + 58f, inRect.width, inRect.height - 58f - footerHeight);

            if (Widgets.ButtonText(new Rect(inRect.xMax - 180f, inRect.yMax - 32f, 180f, 32f), "WNA_AbilityRitual_Cancel".Translate().Resolve()))
            {
                CancelSelection();
                return;
            }

            const float gap = 8f;
            Rect sourcesRect = new Rect(content.x, content.y, ListWidth, content.height);
            Rect categoriesRect = new Rect(sourcesRect.xMax + gap, content.y, ListWidth, content.height);
            Rect abilitiesRect = new Rect(categoriesRect.xMax + gap, content.y, content.xMax - categoriesRect.xMax - gap, content.height);

            DrawSourcePanel(sourcesRect);
            DrawCategoryPanel(categoriesRect);
            DrawAbilities(abilitiesRect);
        }

        private void DrawSourcePanel(Rect rect)
        {
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 24f), "WNA_AbilityRitual_Sources".Translate().Resolve());

            Rect outRect = new Rect(rect.x, rect.y + 26f, rect.width, rect.height - 26f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 18f, (sources.Count + 1) * ListEntryHeight);
            Widgets.BeginScrollView(outRect, ref sourcesScroll, viewRect);

            float y = 0f;
            bool allSourcesSelected = !hasSourceFilter;
            if (DrawListEntry(new Rect(0f, y, viewRect.width, ListEntryHeight),
                    "WNA_AbilityRitual_AllSources".Translate(abilities.Count.Named("COUNT")).Resolve(), allSourcesSelected))
            {
                hasSourceFilter = false;
                selectedSourceKey = null;
                hasCategoryFilter = false;
                selectedCategory = null;
                BuildCategories();
                RebuildFilter(resetPage: true);
            }
            y += ListEntryHeight;

            for (int i = 0; i < sources.Count; i++)
            {
                SourceEntry entry = sources[i];
                bool selected = hasSourceFilter && selectedSourceKey == entry.Key;
                string label = entry.Label + " (" + entry.Abilities.Count + ")";
                if (DrawListEntry(new Rect(0f, y, viewRect.width, ListEntryHeight), label, selected))
                {
                    hasSourceFilter = true;
                    selectedSourceKey = entry.Key;
                    hasCategoryFilter = false;
                    selectedCategory = null;
                    BuildCategories();
                    RebuildFilter(resetPage: true);
                }
                y += ListEntryHeight;
            }

            Widgets.EndScrollView();
        }

        private void DrawCategoryPanel(Rect rect)
        {
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 24f), "WNA_AbilityRitual_Categories".Translate().Resolve());

            Rect outRect = new Rect(rect.x, rect.y + 26f, rect.width, rect.height - 26f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 18f, (categories.Count + 1) * ListEntryHeight);
            Widgets.BeginScrollView(outRect, ref categoriesScroll, viewRect);

            float y = 0f;
            if (DrawListEntry(new Rect(0f, y, viewRect.width, ListEntryHeight),
                    "WNA_AbilityRitual_AllCategories".Translate(categoryTotal.Named("COUNT")).Resolve(), !hasCategoryFilter))
            {
                hasCategoryFilter = false;
                selectedCategory = null;
                RebuildFilter(resetPage: true);
            }
            y += ListEntryHeight;

            for (int i = 0; i < categories.Count; i++)
            {
                CategoryEntry entry = categories[i];
                bool selected = hasCategoryFilter && selectedCategory == entry.Category;
                string label = entry.Label + " (" + entry.Count + ")";
                if (DrawListEntry(new Rect(0f, y, viewRect.width, ListEntryHeight), label, selected))
                {
                    hasCategoryFilter = true;
                    selectedCategory = entry.Category;
                    RebuildFilter(resetPage: true);
                }
                y += ListEntryHeight;
            }

            Widgets.EndScrollView();
        }

        private static bool DrawListEntry(Rect rect, string label, bool selected)
        {
            if (selected)
                Widgets.DrawHighlightSelected(rect);
            else if (Mouse.IsOver(rect))
                Widgets.DrawLightHighlight(rect);

            Rect labelRect = rect.ContractedBy(4f, 0f);
            Text.Font = GameFont.Small;
            using (new TextBlock(TextAnchor.MiddleLeft))
            {
                Widgets.Label(labelRect, Text.ClampTextWithEllipsis(labelRect, label));
            }
            return Widgets.ButtonInvisible(rect);
        }

        private void DrawAbilities(Rect rect)
        {
            Rect grid = new Rect(rect.x, rect.y, rect.width, rect.height - PageBarHeight - 4f);
            Rect pageBar = new Rect(rect.x, rect.yMax - PageBarHeight, rect.width, PageBarHeight);

            int rows = Mathf.Max(1, Mathf.FloorToInt((grid.height + CellGap) / (CellHeight + CellGap)));
            pageSize = Mathf.Max(1, rows * Columns);
            int pageCount = Mathf.Max(1, Mathf.CeilToInt(filtered.Count / (float)pageSize));
            if (page > pageCount - 1)
                page = pageCount - 1;
            if (page < 0)
                page = 0;

            if (filtered.Count == 0)
            {
                Text.Font = GameFont.Small;
                GUI.color = Color.gray;
                using (new TextBlock(TextAnchor.MiddleCenter))
                {
                    Widgets.Label(grid, "WNA_AbilityRitual_NoMatches".Translate().Resolve());
                }
                GUI.color = Color.white;
            }
            else
            {
                float cellWidth = (grid.width - CellGap * (Columns - 1)) / Columns;
                int start = page * pageSize;
                int end = Mathf.Min(filtered.Count, start + pageSize);
                for (int i = start; i < end; i++)
                {
                    int local = i - start;
                    int col = local % Columns;
                    int row = local / Columns;
                    Rect cell = new Rect(grid.x + col * (cellWidth + CellGap), grid.y + row * (CellHeight + CellGap), cellWidth, CellHeight);
                    DrawAbilityCell(cell, filtered[i]);
                }
            }

            DrawPageBar(pageBar, pageCount);
        }

        private void DrawAbilityCell(Rect rect, AbilityDef def)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.11f, 0.11f, 0.11f, 0.85f));
            Widgets.DrawBox(rect);
            if (Mouse.IsOver(rect))
                Widgets.DrawHighlight(rect);

            Rect iconRect = new Rect(rect.x + 8f, rect.y + 8f, 48f, 48f);
            if (def.uiIcon != null)
                GUI.DrawTexture(iconRect, def.uiIcon, ScaleMode.ScaleToFit);

            float textX = iconRect.xMax + 8f;
            float textWidth = rect.xMax - textX - 8f;

            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Rect nameRect = new Rect(textX, rect.y + 8f, textWidth, 24f);
            using (new TextBlock(TextAnchor.UpperLeft))
            {
                Widgets.Label(nameRect, Text.ClampTextWithEllipsis(nameRect, def.LabelCap.Resolve()));
            }

            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.72f, 0.72f, 0.72f);
            Rect subRect = new Rect(textX, rect.y + 34f, textWidth, 18f);
            string sub = LabelOfSource(KeyOf(def)) + " · " + LabelOfCategory(def.category);
            using (new TextBlock(TextAnchor.UpperLeft))
            {
                Widgets.Label(subRect, Text.ClampTextWithEllipsis(subRect, sub));
            }
            GUI.color = Color.white;

            TooltipHandler.TipRegion(rect, def.GetTooltip(recipient));
            if (Widgets.ButtonInvisible(rect))
                Choose(def);
        }

        private void DrawPageBar(Rect rect, int pageCount)
        {
            Text.Font = GameFont.Small;
            bool canPrev = page > 0;
            bool canNext = page < pageCount - 1;
            if (Widgets.ButtonText(new Rect(rect.center.x - 180f, rect.y, 80f, rect.height - 2f), "WNA_AbilityRitual_PrevPage".Translate().Resolve(), active: canPrev))
                page--;
            if (Widgets.ButtonText(new Rect(rect.center.x + 100f, rect.y, 80f, rect.height - 2f), "WNA_AbilityRitual_NextPage".Translate().Resolve(), active: canNext))
                page++;

            GUI.color = Color.white;
            using (new TextBlock(TextAnchor.MiddleCenter))
            {
                Widgets.Label(new Rect(rect.center.x - 100f, rect.y, 200f, rect.height - 2f),
                    "WNA_AbilityRitual_Page".Translate((page + 1).Named("PAGE"), pageCount.Named("TOTAL")).Resolve());
            }
        }

        private void Choose(AbilityDef def)
        {
            if (closedByChoice)
                return;
            closedByChoice = true;
            Close();
            onChosen?.Invoke(def);
        }

        private void CancelSelection()
        {
            if (closedByChoice)
                return;
            closedByChoice = true;
            Close();
            onCancelled?.Invoke();
        }

        public void Dismiss()
        {
            if (closedByChoice)
                return;
            closedByChoice = true;
            Close();
        }

        public override void PostClose()
        {
            base.PostClose();
            if (closedByChoice)
                return;
            closedByChoice = true;
            onCancelled?.Invoke();
        }
    }
}
