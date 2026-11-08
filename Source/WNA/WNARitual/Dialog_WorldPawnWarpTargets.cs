using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using WNA.WNAGameComp;
using WNA.WNAUtility;

namespace WNA.WNARitual
{
    public class Dialog_WorldPawnWarpTargets : Window
    {
        private const float HeaderHeight = 30f;
        private const float RowHeight = 32f;
        private const float FooterHeight = 40f;
        private const float CheckboxSize = 20f;
        private const float PortraitSize = 26f;

        private class Group
        {
            public string Label;
            public int Order;
            public Faction Faction;
            public bool Expanded = true;
            public readonly List<Pawn> Pawns = new List<Pawn>();
        }

        private readonly List<Group> groups = new List<Group>();
        private readonly HashSet<Pawn> selected = new HashSet<Pawn>();
        private readonly List<Pawn> candidates;
        private readonly Map map;
        private readonly IntVec3 cell;
        private readonly List<Pawn> visiblePawns = new List<Pawn>();
        private Vector2 scroll;
        private bool closedByConfirm;
        private bool draggingSelection;
        private bool dragSelectState;
        private int dragLastRow = -1;
        private int dragHoverRow = -1;
        private float dragScrollY;

        public override Vector2 InitialSize =>
            new Vector2(Mathf.Min(560f, UI.screenWidth - 80f), Mathf.Min(640f, UI.screenHeight - 80f));

        public Dialog_WorldPawnWarpTargets(List<Pawn> candidates, Map map, IntVec3 cell)
        {
            this.candidates = candidates ?? new List<Pawn>();
            this.map = map;
            this.cell = cell;

            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            closeOnCancel = false;
            closeOnAccept = false;
            doCloseX = false;
            onlyOneOfTypeAllowed = true;
            openMenuOnCancel = false;

            BuildGroups();
        }

        private void BuildGroups()
        {
            groups.Clear();
            selected.Clear();

            Faction playerFaction = Faction.OfPlayer;
            Group playerGroup = null;
            Group noFactionGroup = null;
            Dictionary<Faction, Group> factionGroups = new Dictionary<Faction, Group>();

            for (int i = 0; i < candidates.Count; i++)
            {
                Pawn pawn = candidates[i];
                if (pawn == null)
                    continue;
                Faction faction = pawn.Faction;
                Group group;
                if (faction == null)
                {
                    if (noFactionGroup == null)
                    {
                        noFactionGroup = new Group
                        {
                            Label = "WNA_WorldPawnWarpNoFaction".Translate().Resolve(),
                            Order = 2
                        };
                    }
                    group = noFactionGroup;
                }
                else if (faction == playerFaction)
                {
                    if (playerGroup == null)
                    {
                        playerGroup = new Group { Label = faction.Name, Order = 0, Faction = faction };
                    }
                    group = playerGroup;
                }
                else if (!factionGroups.TryGetValue(faction, out group))
                {
                    group = new Group { Label = faction.Name, Order = 1, Faction = faction };
                    factionGroups.Add(faction, group);
                }
                group.Pawns.Add(pawn);
            }

            if (playerGroup != null)
                groups.Add(playerGroup);
            groups.AddRange(factionGroups.Values);
            if (noFactionGroup != null)
                groups.Add(noFactionGroup);

            for (int i = 0; i < groups.Count; i++)
            {
                List<Pawn> pawns = groups[i].Pawns;
                pawns.Sort((a, b) => string.Compare(a.LabelShortCap, b.LabelShortCap, StringComparison.CurrentCulture));
            }

            groups.Sort((a, b) =>
            {
                int compare = a.Order.CompareTo(b.Order);
                return compare != 0 ? compare : string.Compare(a.Label, b.Label, StringComparison.CurrentCulture);
            });
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            GUI.color = Color.white;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f), "WNA_WorldPawnWarpWindowTitle".Translate().Resolve());

            Text.Font = GameFont.Small;
            GUI.color = new Color(0.75f, 0.75f, 0.75f);
            Widgets.Label(new Rect(inRect.x, inRect.y + 30f, inRect.width, 22f), "WNA_WorldPawnWarpWindowSubtitle".Translate().Resolve());
            GUI.color = Color.white;

            Rect listRect = new Rect(inRect.x, inRect.y + 58f, inRect.width, inRect.height - 58f - FooterHeight);
            DrawList(listRect);

            Rect footer = new Rect(inRect.x, inRect.yMax - FooterHeight + 8f, inRect.width, 32f);
            Text.Font = GameFont.Small;
            GUI.color = new Color(0.85f, 0.85f, 0.7f);
            Widgets.Label(new Rect(footer.x, footer.y, footer.width - 300f, 32f),
                "WNA_WorldPawnWarpSelected".Translate(selected.Count.Named("COUNT")).Resolve());
            GUI.color = Color.white;

            if (Widgets.ButtonText(new Rect(footer.xMax - 290f, footer.y, 130f, 32f), "WNA_WorldPawnWarpCancel".Translate().Resolve()))
            {
                CancelSelection();
                return;
            }
            if (Widgets.ButtonText(new Rect(footer.xMax - 150f, footer.y, 150f, 32f), "WNA_WorldPawnWarpConfirm".Translate().Resolve(), active: selected.Count > 0))
                Confirm();
        }

        private void DrawList(Rect outRect)
        {
            float contentHeight = 0f;
            for (int i = 0; i < groups.Count; i++)
            {
                contentHeight += HeaderHeight;
                if (groups[i].Expanded)
                    contentHeight += groups[i].Pawns.Count * RowHeight;
            }

            Rect viewRect = new Rect(0f, 0f, outRect.width - 18f, Mathf.Max(contentHeight, outRect.height));
            Widgets.BeginScrollView(outRect, ref scroll, viewRect);

            if (draggingSelection && (!Input.GetMouseButton(0) || Mathf.Abs(scroll.y - dragScrollY) > 0.5f))
                EndDrag();

            visiblePawns.Clear();
            dragHoverRow = -1;

            float visibleTop = scroll.y;
            float visibleBottom = scroll.y + outRect.height;
            float y = 0f;
            for (int i = 0; i < groups.Count; i++)
            {
                Group group = groups[i];
                if (y + HeaderHeight >= visibleTop && y < visibleBottom)
                    DrawGroupHeader(new Rect(0f, y, viewRect.width, HeaderHeight), group);
                y += HeaderHeight;

                if (!group.Expanded)
                    continue;

                for (int j = 0; j < group.Pawns.Count; j++)
                {
                    if (y + RowHeight >= visibleTop && y < visibleBottom)
                    {
                        visiblePawns.Add(group.Pawns[j]);
                        DrawPawnRow(new Rect(0f, y, viewRect.width, RowHeight), group.Pawns[j], visiblePawns.Count - 1);
                    }
                    y += RowHeight;
                }
            }

            if (draggingSelection && Input.GetMouseButton(0) && dragHoverRow >= 0)
                ApplyDragRange(dragHoverRow);

            Widgets.EndScrollView();
        }

        private void DrawGroupHeader(Rect rect, Group group)
        {
            bool over = Mouse.IsOver(rect);
            Widgets.DrawBoxSolid(rect, over
                ? new Color(0.27f, 0.27f, 0.27f, 0.95f)
                : new Color(0.18f, 0.18f, 0.18f, 0.9f));

            Color accent = (group.Faction?.Color) ?? new Color(0.6f, 0.6f, 0.6f);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y + 3f, 4f, rect.height - 6f), accent);

            Rect arrowRect = new Rect(rect.x + 10f, rect.y + (rect.height - 18f) * 0.5f, 18f, 18f);
            GUI.DrawTexture(arrowRect, group.Expanded ? TexButton.Collapse : TexButton.Reveal);

            Rect iconRect = new Rect(arrowRect.xMax + 2f, rect.y + 3f, 24f, 24f);
            Texture2D icon = group.Faction?.def?.FactionIcon;
            Color prevColor = GUI.color;
            if (icon != null && icon != BaseContent.BadTex)
            {
                GUI.color = accent;
                GUI.DrawTexture(iconRect, icon);
            }
            else
            {
                Widgets.DrawBoxSolid(iconRect.ContractedBy(6f), accent);
            }
            GUI.color = prevColor;

            float labelX = iconRect.xMax + 8f;
            Rect labelRect = new Rect(labelX, rect.y, rect.xMax - labelX - 8f, rect.height);
            Text.Font = GameFont.Small;
            GUI.color = Color.Lerp(accent, Color.white, 0.55f);
            using (new TextBlock(TextAnchor.MiddleLeft))
            {
                Widgets.Label(labelRect, Text.ClampTextWithEllipsis(labelRect, group.Label + " (" + group.Pawns.Count + ")"));
            }
            GUI.color = Color.white;

            if (Widgets.ButtonInvisible(rect))
                group.Expanded = !group.Expanded;
        }

        private void DrawPawnRow(Rect rect, Pawn pawn, int rowIndex)
        {
            bool over = Mouse.IsOver(rect);
            bool isSelected = selected.Contains(pawn);

            if (isSelected)
                Widgets.DrawBoxSolid(rect, new Color(0.25f, 0.5f, 0.3f, 0.22f));
            if (over)
                Widgets.DrawLightHighlight(rect);

            Widgets.CheckboxDraw(rect.x + 8f, rect.y + (RowHeight - CheckboxSize) * 0.5f, isSelected, disabled: false, CheckboxSize);

            Rect portraitRect = new Rect(rect.x + 8f + CheckboxSize + 8f,
                rect.y + (RowHeight - PortraitSize) * 0.5f, PortraitSize, PortraitSize);
            Widgets.DrawBoxSolid(portraitRect, new Color(0.1f, 0.1f, 0.1f, 0.5f));
            RenderTexture portrait = PortraitsCache.Get(pawn, new Vector2(PortraitSize, PortraitSize), Rot4.South);
            if (portrait != null)
                GUI.DrawTexture(portraitRect, portrait);

            float labelX = portraitRect.xMax + 8f;
            Rect labelRect = new Rect(labelX, rect.y, rect.xMax - labelX - 12f, rect.height);
            string label = pawn.LabelShortCap;
            if (pawn.Dead)
                label += " (" + "WNA_WorldPawnWarpDead".Translate().Resolve() + ")";

            Text.Font = GameFont.Small;
            using (new TextBlock(TextAnchor.MiddleLeft))
            {
                Widgets.Label(labelRect, Text.ClampTextWithEllipsis(labelRect, label));
            }

            if (!over)
                return;

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                BeginDrag(pawn, rowIndex, isSelected);
            if (draggingSelection && Input.GetMouseButton(0))
                dragHoverRow = rowIndex;

            TooltipHandler.TipRegion(rect, pawn.GetTooltip());
        }

        private void BeginDrag(Pawn pawn, int rowIndex, bool alreadySelected)
        {
            draggingSelection = true;
            dragSelectState = !alreadySelected;
            dragLastRow = rowIndex;
            dragScrollY = scroll.y;
            SetSelected(pawn, dragSelectState);
        }

        private void ApplyDragRange(int rowIndex)
        {
            int from = dragLastRow < 0 ? rowIndex : Mathf.Min(dragLastRow, rowIndex);
            int to = dragLastRow < 0 ? rowIndex : Mathf.Max(dragLastRow, rowIndex);
            for (int i = Mathf.Max(0, from); i <= to && i < visiblePawns.Count; i++)
                SetSelected(visiblePawns[i], dragSelectState);
            dragLastRow = rowIndex;
        }

        private void EndDrag()
        {
            draggingSelection = false;
            dragLastRow = -1;
            dragHoverRow = -1;
        }

        private void SetSelected(Pawn pawn, bool value)
        {
            if (value)
                selected.Add(pawn);
            else
                selected.Remove(pawn);
        }

        private void Confirm()
        {
            if (closedByConfirm || selected.Count == 0)
                return;
            closedByConfirm = true;
            List<Pawn> chosen = new List<Pawn>(selected);
            Close();
            WorldPawnWarpUtility.ScheduleSummons(chosen, map, cell);
        }

        private void CancelSelection()
        {
            if (closedByConfirm)
                return;
            closedByConfirm = true;
            Close();
            GameComp_WorldPawnWarp.Instance?.ClearSelection();
        }

        public override void PostClose()
        {
            base.PostClose();
            if (closedByConfirm)
                return;
            closedByConfirm = true;
            GameComp_WorldPawnWarp.Instance?.ClearSelection();
        }
    }
}
