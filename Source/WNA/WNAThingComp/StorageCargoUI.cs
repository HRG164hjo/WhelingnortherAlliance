using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace WNA.WNAThingComp
{
    internal static class StorageCargoGizmoFactory
    {
        private static readonly Texture2D LoadTex = ContentFinder<Texture2D>.Get("UI/Commands/LoadTransporter");

        private static readonly Texture2D CancelTex = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");

        private static readonly Texture2D UnloadTex = ContentFinder<Texture2D>.Get("UI/Buttons/Abandon");

        public static Command_Action LoadCommand(CompStorageCargo hold)
        {
            return new Command_Action
            {
                defaultLabel = "WNA_CargoHold_CommandLoad".Translate(),
                defaultDesc = "WNA_CargoHold_CommandLoadDesc".Translate(),
                icon = LoadTex,
                action = delegate
                {
                    Find.WindowStack.Add(new Dialog_StorageCargoLoad(hold));
                }
            };
        }

        public static Command_Action CancelCommand(CompStorageCargo hold)
        {
            return new Command_Action
            {
                defaultLabel = "WNA_CargoHold_CommandCancelQueue".Translate(),
                defaultDesc = "WNA_CargoHold_CommandCancelQueueDesc".Translate(),
                icon = CancelTex,
                action = delegate
                {
                    hold.ClearLoadQueue();
                    SoundDefOf.Designate_Cancel.PlayOneShotOnCamera();
                }
            };
        }

        public static Command_Action UnloadCommand(CompStorageCargo hold)
        {
            return new Command_Action
            {
                defaultLabel = "WNA_CargoHold_CommandUnloadAll".Translate(),
                defaultDesc = "WNA_CargoHold_CommandUnloadAllDesc".Translate(),
                icon = UnloadTex,
                action = delegate
                {
                    hold.UnloadAllContents();
                }
            };
        }
    }

    internal class Dialog_StorageCargoLoad : Window
    {
        private enum Tab
        {
            Pawns,
            Items
        }

        private readonly CompStorageCargo hold;

        private readonly Map map;

        private List<TransferableOneWay> transferables;

        private TransferableOneWayWidget pawnsTransfer;

        private TransferableOneWayWidget itemsTransfer;

        private Tab tab;

        private int pendingCount;

        private float pendingMass;

        private static readonly Vector2 ButtonSize = new Vector2(160f, 40f);

        private static readonly List<TabRecord> TabsList = new List<TabRecord>();

        public Dialog_StorageCargoLoad(CompStorageCargo hold)
        {
            this.hold = hold;
            map = hold.Map;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnAccept = false;
        }

        public override Vector2 InitialSize => new Vector2(1024f, UI.screenHeight);

        protected override float Margin => 0f;

        public override void PostOpen()
        {
            base.PostOpen();
            BuildTransferables();
        }

        private void BuildTransferables()
        {
            transferables = new List<TransferableOneWay>();
            if (hold.Props.allowPawns)
            {
                foreach (Pawn pawn in CaravanFormingUtility.AllSendablePawns(map, true, false, false, false, false, -1))
                {
                    AddToTransferables(pawn);
                }
            }
            foreach (Thing item in CaravanFormingUtility.AllReachableColonyItems(map, !map.IsPlayerHome, false, false))
            {
                AddToTransferables(item);
            }

            bool useCount = hold.UseCount;
            Func<float> availableGetter = useCount ? null : new Func<float>(() => Mathf.Max(0f, hold.StorageCapacity - PlannedUsage));
            pawnsTransfer = new TransferableOneWayWidget(null, null, null,
                "WNA_CargoHold_ColonyThingCountTip".Translate(), drawMass: !useCount,
                ignorePawnInventoryMass: IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload,
                includePawnsMassInMassUsage: true, availableMassGetter: availableGetter, tile: map.Tile,
                drawMarketValue: true, drawEquippedWeapon: true);
            CaravanUIUtility.AddPawnsSections(pawnsTransfer, transferables);
            itemsTransfer = new TransferableOneWayWidget(
                transferables.Where(x => x.ThingDef.category != ThingCategory.Pawn), null, null,
                "WNA_CargoHold_ColonyThingCountTip".Translate(), drawMass: !useCount,
                ignorePawnInventoryMass: IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload,
                includePawnsMassInMassUsage: true, availableMassGetter: availableGetter, tile: map.Tile,
                drawMarketValue: true, drawItemNutrition: true, drawDaysUntilRot: true);
        }

        private void AddToTransferables(Thing thing)
        {
            TransferableOneWay match = TransferableUtility.TransferableMatching(thing, transferables, TransferAsOneMode.PodsOrCaravanPacking);
            if (match == null)
            {
                match = new TransferableOneWay();
                transferables.Add(match);
            }
            if (!match.things.Contains(thing))
            {
                match.things.Add(thing);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect titleRect = new Rect(0f, 0f, inRect.width, 35f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(titleRect, "WNA_CargoHold_DialogTitle".Translate(hold.parent.LabelShort).CapitalizeFirst());
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            DrawCapacityBar(new Rect(12f, 35f, inRect.width - 24f, 40f));
            inRect.yMin += 52f;

            bool showPawns = hold.Props.allowPawns;
            TabsList.Clear();
            if (showPawns)
            {
                TabsList.Add(new TabRecord("PawnsTab".Translate(), delegate { tab = Tab.Pawns; }, tab == Tab.Pawns));
            }
            TabsList.Add(new TabRecord("ItemsTab".Translate(), delegate { tab = Tab.Items; }, tab == Tab.Items || !showPawns));
            inRect.yMin += 67f;
            Widgets.DrawMenuSection(inRect);
            TabDrawer.DrawTabs(inRect, TabsList);

            inRect = inRect.ContractedBy(17f);
            inRect.height += 17f;
            Widgets.BeginGroup(inRect);
            Rect body = inRect.AtZero();
            DoBottomButtons(body);
            Rect listRect = body;
            listRect.yMax -= 76f;
            bool changed;
            if (showPawns && tab == Tab.Pawns)
            {
                pawnsTransfer.OnGUI(listRect, out changed);
            }
            else
            {
                itemsTransfer.OnGUI(listRect, out changed);
            }
            Widgets.EndGroup();
        }

        private float PlannedUsage => hold.StorageUsage + (hold.UseCount ? pendingCount : pendingMass);

        private void RecalculatePending()
        {
            pendingCount = 0;
            pendingMass = 0f;
            if (transferables == null)
            {
                return;
            }
            for (int i = 0; i < transferables.Count; i++)
            {
                TransferableOneWay transferable = transferables[i];
                int delta = transferable.CountToTransfer;
                if (delta <= 0)
                {
                    continue;
                }
                pendingCount += delta;
                if (!hold.UseCount)
                {
                    Thing thing = transferable.AnyThing;
                    if (thing != null)
                    {
                        pendingMass += CompStorageCargo.MassOf(thing, delta);
                    }
                }
            }
        }

        private void DrawCapacityBar(Rect rect)
        {
            RecalculatePending();
            float pending = hold.UseCount ? pendingCount : pendingMass;
            float used = hold.StorageUsage + pending;
            float capacity = hold.StorageCapacity;
            string label = hold.UseCount
                ? "WNA_CargoHold_CapacityCount".Translate(Mathf.RoundToInt(used).ToString(), capacity.ToString("F0"))
                : "WNA_CargoHold_CapacityMass".Translate(used.ToString("F0"), capacity.ToString("F0"));

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width * 0.5f, rect.height), label);
            Text.Anchor = TextAnchor.UpperLeft;

            Rect bar = new Rect(rect.x + rect.width * 0.5f, rect.y + rect.height * 0.25f, rect.width * 0.5f - 12f, rect.height * 0.5f);
            Widgets.DrawBoxSolid(bar, new Color(0.12f, 0.12f, 0.12f, 1f));
            Rect inner = new Rect(bar.x + 2f, bar.y + 2f, bar.width - 4f, bar.height - 4f);
            float ratio = capacity > 0f ? Mathf.Clamp01(used / capacity) : 1f;
            if (ratio > 0f)
            {
                Widgets.DrawBoxSolid(new Rect(inner.x, inner.y, inner.width * ratio, inner.height),
                    used >= capacity ? ColorLibrary.RedReadable : new Color(0.35f, 0.75f, 0.45f));
            }
        }

        private void DoBottomButtons(Rect rect)
        {
            if (Widgets.ButtonText(new Rect(0f, rect.height - 55f - 17f, ButtonSize.x, ButtonSize.y), "CancelButton".Translate()))
            {
                Close();
            }
            if (Widgets.ButtonText(new Rect(rect.width / 2f - ButtonSize.x / 2f, rect.height - 55f - 17f, ButtonSize.x, ButtonSize.y), "ResetButton".Translate()))
            {
                BuildTransferables();
            }
            if (Widgets.ButtonText(new Rect(rect.width - ButtonSize.x, rect.height - 55f - 17f, ButtonSize.x, ButtonSize.y), "AcceptButton".Translate()))
            {
                TryAccept();
            }
        }

        private void TryAccept()
        {
            hold.ClearLoadQueue();
            int queued = 0;
            for (int i = 0; i < transferables.Count; i++)
            {
                TransferableOneWay transferable = transferables[i];
                if (transferable.CountToTransfer > 0 && transferable.HasAnyThing)
                {
                    hold.QueueTransferable(transferable, transferable.CountToTransfer);
                    queued += transferable.CountToTransfer;
                }
            }
            if (queued > 0)
            {
                Messages.Message("WNA_CargoHold_Queued".Translate(queued.ToString()), hold.parent, MessageTypeDefOf.TaskCompletion, historical: false);
            }
            Close(doCloseSound: false);
        }

        public override void OnAcceptKeyPressed()
        {
            TryAccept();
        }
    }

    public class ITab_StorageCargoContents : ITab
    {
        private const float RowHeight = 28f;

        private const float IconSize = 28f;

        private Vector2 scrollPosition;

        private CompStorageCargo Hold => SelThing?.TryGetComp<CompStorageCargo>();

        public ITab_StorageCargoContents()
        {
            size = new Vector2(460f, 450f);
            labelKey = "WNA_CargoHold_TabContents";
        }

        public override bool IsVisible
        {
            get
            {
                CompStorageCargo hold = Hold;
                return hold != null && hold.parent.Faction == Faction.OfPlayer;
            }
        }

        protected override void FillTab()
        {
            CompStorageCargo hold = Hold;
            if (hold == null)
            {
                return;
            }
            hold.EnsureContentsCache();
            Rect outRect = new Rect(default(Vector2), size).ContractedBy(10f);
            outRect.yMin += 20f;
            int rows = hold.GroupDefs.Count;
            string header = hold.UsageString;
            int pendingLoad = hold.PendingLoadCount;
            if (pendingLoad > 0)
            {
                header += "\n" + "WNA_CargoHold_PendingLoad".Translate(pendingLoad.ToString());
            }
            Text.Font = GameFont.Small;
            float headerHeight = Text.CalcHeight(header, outRect.width);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, Mathf.Max(headerHeight + rows * RowHeight, outRect.height));
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            float curY = 0f;
            Widgets.Label(new Rect(0f, curY, viewRect.width, headerHeight), header);
            curY += headerHeight + 6f;
            Widgets.ListSeparator(ref curY, viewRect.width, "Contents".Translate());
            float viewTop = scrollPosition.y;
            float viewBottom = scrollPosition.y + outRect.height;
            for (int i = 0; i < rows; i++)
            {
                float rowY = curY + i * RowHeight;
                if (rowY + RowHeight < viewTop || rowY > viewBottom)
                {
                    continue;
                }
                DrawRow(hold, i, new Rect(0f, rowY, viewRect.width, RowHeight));
            }
            if (rows == 0)
            {
                Widgets.Label(new Rect(0f, curY, viewRect.width, RowHeight), "NoneLower".Translate());
            }
            Widgets.EndScrollView();
        }

        private void DrawRow(CompStorageCargo hold, int index, Rect rect)
        {
            ThingDef def = hold.GroupDefs[index];
            int count = hold.GroupCounts[index];
            Rect local = new Rect(0f, 0f, rect.width, rect.height);
            Widgets.BeginGroup(rect);
            if (count > 1)
            {
                if (Widgets.ButtonImage(new Rect(local.x + local.width - 24f, (local.height - 24f) / 2f, 24f, 24f),
                        CaravanThingsTabUtility.AbandonSpecificCountButtonTex))
                {
                    Find.WindowStack.Add(new Dialog_Slider("RemoveSliderText".Translate(def.label), 1, count,
                        delegate(int x)
                        {
                            hold.DropGroup(def, x);
                        }));
                }
                local.width -= 24f;
            }
            if (Mouse.IsOver(local))
            {
                GUI.color = ITab_Pawn_Gear.HighlightColor;
                GUI.DrawTexture(local, TexUI.HighlightTex);
            }
            if (def.DrawMatSingle != null && def.DrawMatSingle.mainTexture != null)
            {
                Widgets.ThingIcon(new Rect(4f, 0f, IconSize, IconSize), def);
            }
            Widgets.InfoCardButton(local.width - 24f, 0f, def);
            local.width -= 36f;
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = ITab_Pawn_Gear.ThingLabelColor;
            string label = GenLabel.ThingLabel(def, null, count).CapitalizeFirst();
            Widgets.Label(new Rect(36f, 0f, local.width, local.height), label.StripTags().Truncate(local.width));
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            TooltipHandler.TipRegion(local, label);
            Widgets.EndGroup();
        }
    }
}
