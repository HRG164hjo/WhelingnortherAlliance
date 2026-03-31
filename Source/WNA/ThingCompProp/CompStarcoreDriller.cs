using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace WNA.ThingCompProp
{
    public class PropStarcoreDriller : CompProperties
    {
        public PropStarcoreDriller()
        {
            compClass = typeof(CompStarcoreDriller);
        }
        public float workPerPortion = 10000f;
        public float autoDrillEfficiency = 0.40f;
        public float yieldFactor = 10f;
        public int fallbackCountPerPortion = 1;
    }
    public class CompStarcoreDriller : ThingComp
    {
        public PropStarcoreDriller Props => (PropStarcoreDriller)props;
        private CompPowerTrader powerComp;
        private float portionProgress;
        private float accumulatedYieldWork;
        private int lastUsedTick = -99999;
        private bool autoMode;
        private Effecter activeEffecter;
        private ThingDef selectedResource;
        public float ProgressToNextPortionPercent => portionProgress / Props.workPerPortion;
        public ThingDef SelectedResource => selectedResource;
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            powerComp = parent.TryGetComp<CompPowerTrader>();
        }
        public override void PostExposeData()
        {
            Scribe_Values.Look(ref portionProgress, "portionProgress", 0f);
            Scribe_Values.Look(ref accumulatedYieldWork, "portionYieldPct", 0f);
            Scribe_Values.Look(ref lastUsedTick, "lastUsedTick", -99999);
            Scribe_Values.Look(ref autoMode, "autoMode", false);
            Scribe_Defs.Look(ref selectedResource, "selectedResource");
        }
        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            CleanupEffecter();
        }
        public override void CompTick()
        {
            base.CompTick();
            if (!autoMode || !CanDrillNow())
            {
                CleanupEffecter();
                return;
            }
            if (parent.IsHashIntervalTick(60))
            {
                DrillWorkDone(null, 60);
                EnsureEffecter();
            }
        }
        public bool CanDrillNow()
        {
            if (!parent.Spawned || parent.Map == null) return false;
            if (powerComp != null && !powerComp.PowerOn) return false;
            if (selectedResource == null) return false;
            if (parent.GetComp<CompForbiddable>().Forbidden) return false;
            return true;
        }
        public bool IsAutoMode()
        {
            if (autoMode == true) return true;
            return false;
        }
        public void DrillWorkDone(Pawn driller, int delta)
        {
            if (delta <= 0 || Props.workPerPortion <= 0f) return;
            float speed = driller != null
                ? driller.GetStatValue(StatDefOf.DeepDrillingSpeed)
                : Props.autoDrillEfficiency;
            if (speed <= 0f) return;
            float miningYield = driller != null ? driller.GetStatValue(StatDefOf.MiningYield) : 1f;
            float work = speed * delta;
            portionProgress += work * (driller != null ?
                (Mathf.Sqrt(driller.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation))) * 1.732f : 1f);
            accumulatedYieldWork += work * miningYield;
            lastUsedTick = Find.TickManager.TicksGame;
            while (portionProgress >= Props.workPerPortion)
            {
                float yieldPct = Mathf.Max(0.01f, accumulatedYieldWork / Mathf.Max(1f, portionProgress));
                TryProducePortion(yieldPct, driller);
                portionProgress -= Props.workPerPortion;
                accumulatedYieldWork = Mathf.Max(0f, accumulatedYieldWork - Props.workPerPortion * yieldPct);
            }
        }
        private void TryProducePortion(float yieldPct, Pawn driller)
        {
            if (selectedResource == null || parent.Map == null) return;
            int baseCount = GetCountPerPortion(selectedResource);
            int stackCount = Mathf.Max(1, GenMath.RoundRandom(baseCount * yieldPct));
            Thing thing = ThingMaker.MakeThing(selectedResource);
            thing.stackCount = stackCount;
            GenPlace.TryPlaceThing(
                thing,
                parent.InteractionCell,
                parent.Map,
                ThingPlaceMode.Near,
                null,
                p => p != parent.Position && p != parent.InteractionCell);
            if (driller != null)
            {
                Find.HistoryEventsManager.RecordEvent(
                    new HistoryEvent(HistoryEventDefOf.Mined,
                    driller.Named(HistoryEventArgsNames.Doer)));
            }
        }
        private int GetCountPerPortion(ThingDef def)
        {
            if (def.deepCountPerPortion > 0) return (int)(def.deepCountPerPortion * Props.yieldFactor);
            if (DrillTargetUtility.IsChunk(def)) return (int)Mathf.Max(1, Props.fallbackCountPerPortion * Props.yieldFactor);
            return (int)Mathf.Max(1, Props.fallbackCountPerPortion);
        }
        private void EnsureEffecter()
        {
            if (activeEffecter != null) return;
            activeEffecter = EffecterDefOf.Drill.Spawn();
            activeEffecter.Trigger(parent, parent);
        }
        private void CleanupEffecter()
        {
            if (activeEffecter == null) return;
            activeEffecter.Cleanup();
            activeEffecter = null;
        }
        public void SetResource(ThingDef def)
        {
            selectedResource = def;
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra()) yield return g;
            yield return new Command_Toggle
            {
                defaultLabel = "WNA_CompStarcoreDriller_Mode".Translate(autoMode ? "WNA_Auto".Translate() : "WNA_Manual".Translate()),
                defaultDesc = "WNA_CompStarcoreDriller_Mode.Desc".Translate(),
                isActive = () => autoMode,
                toggleAction = () => autoMode = !autoMode
            };
            yield return new Command_Action
            {
                defaultLabel = "WNA_CompStarcoreDriller_SelectResource".Translate(),
                defaultDesc = selectedResource != null ? "WNA_CompStarcoreDriller_CurrentTarget".Translate(selectedResource.LabelCap)
                : "WNA_CompStarcoreDriller_DefaultBedrock".Translate(),
                action = GenerateResourceMenu
            };
        }
        public override string CompInspectStringExtra()
        {
            if (selectedResource == null)
                return "WNA_Default".Translate();
            string resLabel = selectedResource?.LabelCap.ToString() ?? "WNA_CompStarcoreDriller_DefaultBedrock".Translate();
            string modLabel = autoMode ? "WNA_Auto".Translate() : "WNA_Manual".Translate();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("WNA_CompStarcoreDriller_InspectTarget".Translate() + ": " + resLabel);
            sb.AppendLine("WNA_CompStarcoreDriller_InspectProgress".Translate() + ": " + ProgressToNextPortionPercent.ToStringPercent("F0"));
            sb.Append("WNA_CompStarcoreDriller_InspectMode".Translate() + ": " + modLabel);
            return sb.ToString().TrimEnd();
        }
        public void GenerateResourceMenu()
        {
            List<ThingDef> candidates = DrillTargetUtility.GetCachedCandidates();
            List<FloatMenuOption> options = new List<FloatMenuOption>
            {
                new FloatMenuOption("WNA_Default".Translate(), () => SetResource(null))
            };
            foreach (ThingDef def in candidates)
            {
                ThingDef localDef = def;
                options.Add(new FloatMenuOption(
                    localDef.LabelCap,
                    () => SetResource(localDef),
                    localDef,
                    extraPartWidth: 29f,
                    extraPartOnGUI: rect =>
                        Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, localDef)
                ));
            }
            if (options.Count > 0)
                Find.WindowStack.Add(new FloatMenu(options));
        }
    }
    internal static class DrillTargetUtility
    {
        private static List<ThingDef> cachedCandidates;
        public static bool IsChunk(ThingDef def)
        {
            return def?.thingCategories != null &&
                (def.thingCategories.Contains(
                    DefDatabase<ThingCategoryDef>.GetNamedSilentFail("Chunks")) ||
                def.thingCategories.Contains(
                    DefDatabase<ThingCategoryDef>.GetNamedSilentFail("StoneChunks")));
        }
        public static bool IsValidDrillTarget(ThingDef def)
        {
            if (def == null || def.category != ThingCategory.Item) return false;
            bool isDeep = def.deepCommonality > 0f;
            bool isChunk = IsChunk(def);
            return isDeep || isChunk;
        }
        public static List<ThingDef> GetCachedCandidates()
        {
            if (cachedCandidates == null)
            {
                cachedCandidates = DefDatabase<ThingDef>.AllDefsListForReading
                    .Where(IsValidDrillTarget)
                    .OrderBy(d => d.label)
                    .ToList();
            }
            return cachedCandidates;
        }
    }
}