using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace WNA.WNAThingComp
{
    internal enum StorageCargoDropOn
    {
        Always,
        Demolish,
        Never
    }

    internal class PropStorageCargo : CompProperties_Transporter
    {
        public float storageCapacity = 1500f;

        public bool allowPawns = false;

        public bool useCountInsteadMass = true;

        public bool countForWealth = true;

        public StorageCargoDropOn dropStorageOn = StorageCargoDropOn.Always;

        public PropStorageCargo()
        {
            compClass = typeof(CompStorageCargo);
            canChangeAssignedThingsAfterStarting = true;
            shouldTickContents = false;
        }

        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);
            massCapacity = storageCapacity;
        }
    }

    internal class CompStorageCargo : CompTransporter
    {
        private int contentsCount;

        private float massUsage = -1f;

        private bool contentsCacheDirty = true;

        private int intervalAccumulator;

        private int contentsCacheTick = -30;

        private string cachedSummary = string.Empty;

        private readonly List<ThingDef> groupDefs = new List<ThingDef>();

        private readonly List<int> groupCounts = new List<int>();

        private readonly Dictionary<ThingDef, int> groupIndex = new Dictionary<ThingDef, int>();

        public new PropStorageCargo Props => (PropStorageCargo)props;

        public float StorageCapacity => massCapacityOverride > 0f ? massCapacityOverride : Props.storageCapacity;

        public bool UseCount => Props.useCountInsteadMass;

        public float StorageUsage => UseCount ? contentsCount : MassUsage;

        public float StorageCapacityLeft => Mathf.Max(0f, StorageCapacity - StorageUsage);

        public int PendingLoadCount
        {
            get
            {
                if (leftToLoad == null)
                {
                    return 0;
                }
                int count = 0;
                for (int i = 0; i < leftToLoad.Count; i++)
                {
                    TransferableOneWay transferable = leftToLoad[i];
                    if (transferable != null && transferable.CountToTransfer > 0)
                    {
                        count += transferable.CountToTransfer;
                    }
                }
                return count;
            }
        }

        public IReadOnlyList<ThingDef> GroupDefs => groupDefs;

        public IReadOnlyList<int> GroupCounts => groupCounts;

        public CompStorageCargo()
        {
            innerContainer = new StorageCargoOwner(this);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                ((StorageCargoOwner)innerContainer).Bind(this);
                RebuildUsage();
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            RebuildUsage();
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            ClearLoadQueue();
            if (mode != DestroyMode.WillReplace && map != null && innerContainer.Any && ShouldDropContents(mode))
            {
                innerContainer.TryDropAll(parent.Position, map, ThingPlaceMode.Near);
            }
            RebuildUsage();
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (!innerContainer.Any)
            {
                return;
            }
            if (Props.dropStorageOn == StorageCargoDropOn.Never)
            {
                innerContainer.ClearAndDestroyContentsOrPassToWorld();
                RebuildUsage();
                return;
            }
            Map map = previousMap ?? ThingOwnerUtility.GetRootMap(this);
            IntVec3 loc = ThingOwnerUtility.GetRootPosition(this);
            if (map != null && loc.IsValid)
            {
                innerContainer.TryDropAll(loc, map, ThingPlaceMode.Near);
            }
            else
            {
                innerContainer.ClearAndDestroyContentsOrPassToWorld();
            }
            RebuildUsage();
        }

        private bool ShouldDropContents(DestroyMode mode)
        {
            switch (Props.dropStorageOn)
            {
                case StorageCargoDropOn.Never:
                    return false;
                case StorageCargoDropOn.Demolish:
                    return DemolishedOrDestroyed(mode);
                default:
                    return true;
            }
        }

        private static bool DemolishedOrDestroyed(DestroyMode mode)
        {
            return mode == DestroyMode.Deconstruct || mode == DestroyMode.KillFinalize || mode == DestroyMode.KillFinalizeLeavingsOnly;
        }

        public override void CompTick()
        {
            if (Props.shouldTickContents && parent.Spawned)
            {
                innerContainer.DoTick();
            }
        }

        public override void CompTickInterval(int delta)
        {
            intervalAccumulator += delta;
            if (intervalAccumulator < 250)
            {
                return;
            }
            TickUsageAndOccupants(intervalAccumulator);
            intervalAccumulator = 0;
        }

        public override void CompTickRare()
        {
            TickUsageAndOccupants(250);
        }

        private void TickUsageAndOccupants(int delta)
        {
            ReconcileUsage();
            if (!Props.allowPawns || Props.restEffectiveness <= 0f || !parent.Spawned)
            {
                return;
            }
            for (int i = 0; i < innerContainer.Count; i++)
            {
                if (innerContainer[i] is Pawn pawn && !pawn.Dead && pawn.needs?.rest != null)
                {
                    pawn.needs.rest.TickResting(Props.restEffectiveness * delta);
                }
            }
        }

        public string UsageString
        {
            get
            {
                if (UseCount)
                {
                    return "WNA_CargoHold_CapacityCount".Translate(contentsCount.ToString(), StorageCapacity.ToString("F0"));
                }
                return "WNA_CargoHold_CapacityMass".Translate(MassUsage.ToString("F0"), StorageCapacity.ToString("F0"));
            }
        }

        public override string CompInspectStringExtra()
        {
            EnsureContentsCache();
            StringBuilder builder = new StringBuilder();
            builder.Append(UsageString);
            if (!cachedSummary.NullOrEmpty())
            {
                builder.AppendLine().Append(cachedSummary);
            }
            return builder.ToString();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!parent.Spawned || parent.Faction != Faction.OfPlayer || Find.Selector.NumSelected > 1)
            {
                yield break;
            }
            yield return StorageCargoGizmoFactory.LoadCommand(this);
            if (AnythingLeftToLoad)
            {
                yield return StorageCargoGizmoFactory.CancelCommand(this);
            }
            if (innerContainer.Any)
            {
                yield return StorageCargoGizmoFactory.UnloadCommand(this);
            }
        }

        public void QueueTransferable(TransferableOneWay transferable, int count)
        {
            if (count <= 0 || !transferable.HasAnyThing)
            {
                return;
            }
            if (groupID < 0)
            {
                groupID = TransporterUtility.InitiateLoading(new List<CompTransporter> { this });
            }
            AddToTheToLoadList(transferable, count);
        }

        public void ClearLoadQueue()
        {
            leftToLoad?.Clear();
        }

        public void UnloadAllContents()
        {
            ClearLoadQueue();
            if (Map != null && innerContainer.Any)
            {
                innerContainer.TryDropAll(parent.Position, Map, ThingPlaceMode.Near);
            }
            RebuildUsage();
        }

        public void RebuildUsage()
        {
            contentsCount = -1;
            ReconcileUsage();
        }

        public void ReconcileUsage()
        {
            int count = 0;
            for (int i = 0; i < innerContainer.Count; i++)
            {
                Thing item = innerContainer[i];
                if (item != null)
                {
                    count += item.stackCount;
                }
            }
            if (contentsCount != count)
            {
                contentsCount = count;
                massUsage = -1f;
                contentsCacheDirty = true;
            }
        }

        public void NotifyCargoChanged(Thing item, int countDelta)
        {
            if (countDelta == 0)
            {
                return;
            }
            if (contentsCount < 0)
            {
                ReconcileUsage();
                return;
            }
            contentsCount = Mathf.Max(0, contentsCount + countDelta);
            if (massUsage >= 0f)
            {
                massUsage = Mathf.Max(0f, massUsage + MassOf(item, countDelta));
            }
            contentsCacheDirty = true;
        }

        public int CountForAccept(Thing item)
        {
            if (item == null || item.stackCount <= 0)
            {
                return 0;
            }
            if (item is Pawn && !Props.allowPawns)
            {
                return 0;
            }
            if (UseCount)
            {
                int room = Mathf.CeilToInt(Mathf.Max(0f, StorageCapacity - contentsCount));
                return Mathf.Min(item.stackCount, room);
            }
            if (MassUsage + ThingMass(item) > StorageCapacity && !innerContainer.Contains(item))
            {
                return 0;
            }
            return item.stackCount;
        }

        public new float MassUsage
        {
            get
            {
                if (massUsage < 0f)
                {
                    float total = 0f;
                    for (int i = 0; i < innerContainer.Count; i++)
                    {
                        total += ThingMass(innerContainer[i]);
                    }
                    massUsage = Mathf.Max(0f, total);
                }
                return massUsage;
            }
        }

        public void EnsureContentsCache(bool force = false)
        {
            if (!contentsCacheDirty)
            {
                return;
            }
            int now = Find.TickManager.TicksGame;
            if (!force && now - contentsCacheTick < 30)
            {
                return;
            }
            contentsCacheTick = now;
            contentsCacheDirty = false;
            groupDefs.Clear();
            groupCounts.Clear();
            groupIndex.Clear();
            if (!innerContainer.Any)
            {
                cachedSummary = string.Empty;
                return;
            }
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < innerContainer.Count; i++)
            {
                Thing item = innerContainer[i];
                if (item == null)
                {
                    continue;
                }
                if (groupIndex.TryGetValue(item.def, out int index))
                {
                    groupCounts[index] += item.stackCount;
                }
                else
                {
                    groupIndex[item.def] = groupDefs.Count;
                    groupDefs.Add(item.def);
                    groupCounts.Add(item.stackCount);
                }
            }
            int shown = Mathf.Min(8, groupDefs.Count);
            for (int i = 0; i < shown; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }
                builder.Append(GenLabel.ThingLabel(groupDefs[i], null, groupCounts[i]));
            }
            if (groupDefs.Count > shown)
            {
                builder.Append("...");
            }
            cachedSummary = builder.ToString();
        }

        public void DropGroup(ThingDef def, int count)
        {
            int remaining = count;
            for (int i = innerContainer.Count - 1; i >= 0 && remaining > 0; i--)
            {
                Thing item = innerContainer[i];
                if (item == null || item.def != def)
                {
                    continue;
                }
                int take = Mathf.Min(remaining, item.stackCount);
                remaining -= take;
                Thing piece = item.SplitOff(take);
                if (piece.stackCount > 0 && !piece.Destroyed)
                {
                    GenDrop.TryDropSpawn(piece, parent.Position, Map, ThingPlaceMode.Near, out _);
                }
                if (item.stackCount <= 0)
                {
                    innerContainer.Remove(item);
                }
            }
            ReconcileUsage();
            EnsureContentsCache(force: true);
        }

        internal static float ThingMass(Thing item)
        {
            if (item == null)
            {
                return 0f;
            }
            return MassOf(item, item.stackCount);
        }

        internal static float MassOf(Thing item, int count)
        {
            if (item is Pawn || item is Corpse || item is UnfinishedThing || item is MinifiedThing)
            {
                return item.GetStatValue(StatDefOf.Mass) * count;
            }
            return item.def.BaseMass * count;
        }

        internal class StorageCargoOwner : ThingOwner<Thing>
        {
            private CompStorageCargo hold;

            public StorageCargoOwner()
            {
            }

            public StorageCargoOwner(CompStorageCargo hold)
            {
                this.hold = hold;
            }

            public void Bind(CompStorageCargo comp)
            {
                hold = comp;
                if (owner == null)
                {
                    owner = comp;
                }
            }

            public override int GetCountCanAccept(Thing item, bool canMergeWithExistingStacks = true)
            {
                if (hold == null)
                {
                    return base.GetCountCanAccept(item, canMergeWithExistingStacks);
                }
                return hold.CountForAccept(item);
            }

            public override bool TryAdd(Thing item, bool canMergeWithExistingStacks = true)
            {
                if (item == null)
                {
                    return false;
                }
                int incoming = item.stackCount;
                bool added = base.TryAdd(item, canMergeWithExistingStacks);
                if (hold != null)
                {
                    int remainder = (!item.Destroyed && item.stackCount > 0) ? item.stackCount : 0;
                    int mergedIntoExistingStacks = incoming - remainder;
                    if (mergedIntoExistingStacks > 0)
                    {
                        hold.NotifyCargoChanged(item, mergedIntoExistingStacks);
                    }
                }
                return added;
            }

            protected override void NotifyAdded(Thing item)
            {
                base.NotifyAdded(item);
                if (item != null)
                {
                    hold?.NotifyCargoChanged(item, item.stackCount);
                }
            }

            protected override void NotifyRemoved(Thing item)
            {
                base.NotifyRemoved(item);
                if (item != null)
                {
                    hold?.NotifyCargoChanged(item, 0 - item.stackCount);
                }
            }
        }
    }
}
