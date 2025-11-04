using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using WNA.WNADefOf;

namespace WNA.ThingCompProp
{
    public class CompStarcoreDriller : CompProperties
    {
        public CompStarcoreDriller()
        {
            compClass = typeof(StarcoreDriller);
        }
        public float workPerPortion = 6000f;
        public float autoDrillEfficiency = 0.3f;
    }
    public class StarcoreDriller : ThingComp
    {
        public CompStarcoreDriller Props => (CompStarcoreDriller)props;
        private CompPowerTrader powerComp;
        private float portionProgress;
        private float portionYieldPct;
        private int lastUsedTick = -99999;
        private bool autoMode;
        private Effecter activeEffecter;
        private ThingDef selectedResource;
        public bool IsAutoMode() => autoMode;
        public float ProgressToNextPortionPercent => portionProgress / Props.workPerPortion;
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            powerComp = parent.TryGetComp<CompPowerTrader>();
        }
        public override void PostExposeData()
        {
            Scribe_Values.Look(ref portionProgress, "portionProgress", 0f);
            Scribe_Values.Look(ref portionYieldPct, "portionYieldPct", 0f);
            Scribe_Values.Look(ref lastUsedTick, "lastUsedTick", 0);
            Scribe_Values.Look(ref autoMode, "autoMode", false);
            Scribe_Defs.Look(ref selectedResource, "selectedResource");
        }
        public override void CompTick()
        {
            base.CompTick();
            if (!CanDrillNow()) return;

            if (autoMode && parent.IsHashIntervalTick(60))
            {
                DrillWorkDone(null, 60);
                if (activeEffecter == null)
                {
                    activeEffecter = EffecterDefOf.Drill.Spawn();
                    activeEffecter.Trigger(parent, parent);
                }
            }
            if (activeEffecter != null && !autoMode)
            {
                activeEffecter.Cleanup();
                activeEffecter = null;
            }
        }
        public bool CanDrillNow()
        {
            return (powerComp == null || powerComp.PowerOn);
        }
        public void DrillWorkDone(Pawn driller, int delta)
        {
            float speed = driller != null
                ? driller.GetStatValue(StatDefOf.DeepDrillingSpeed)
                : Props.autoDrillEfficiency * 1f;
            float num = speed * delta;
            portionProgress += num;
            portionYieldPct += num * (driller?.GetStatValue(StatDefOf.MiningYield) ?? 1f);
            lastUsedTick = Find.TickManager.TicksGame;
            if (portionProgress > Props.workPerPortion)
            {
                TryProducePortion(portionYieldPct, driller);
                portionProgress = 0f;
                portionYieldPct = 0f;
            }
        }
        private void TryProducePortion(float yieldPct, Pawn driller)
        {
            ThingDef resDef = selectedResource ?? GetDefaultResource();
            if (resDef == null) return;
            int num = resDef.deepCountPerPortion > 0 ? Mathf.Max(resDef.deepCountPerPortion, 30) : 30;
            int stackCount = Mathf.Max(1, GenMath.RoundRandom(num * yieldPct));
            Thing thing = ThingMaker.MakeThing(resDef);
            thing.stackCount = stackCount;
            GenPlace.TryPlaceThing(thing, parent.InteractionCell, parent.Map, ThingPlaceMode.Near);
        }
        private ThingDef GetDefaultResource()
        {
            Map map = parent.Map;
            if (map.Biome.hasBedrock)
            {
                var rock = DeepDrillUtility.RockForTerrain(map.terrainGrid.BaseTerrainAt(parent.Position));
                if (rock != null) return rock.building.mineableThing;
            }
            return DeepDrillUtility.Rocks.RandomElement().building.mineableThing;
            /*WNAMainDefOf.WNA_Quicklime*/
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra()) yield return g;
            yield return new Command_Toggle
            {
                defaultLabel = "模式切换： " + (autoMode ? "自动" : "手动"),
                defaultDesc = "切换钻井的工作模式。",
                isActive = () => autoMode,
                toggleAction = () => autoMode = !autoMode
            };
            yield return new Command_Action
            {
                defaultLabel = "选择资源",
                defaultDesc = (selectedResource != null ? selectedResource.LabelCap.ToString() : "当前目标：无（默认基岩）"),
                action = () => Find.WindowStack.Add(new Dialog_SelectResource(this))
            };
        }
        public void SetResource(ThingDef def)
        {
            selectedResource = def;
        }
        public override string CompInspectStringExtra()
        {
            string res = selectedResource?.LabelCap.ToString() ?? "默认基岩";
            return $"目标资源: {res}\n进度: {ProgressToNextPortionPercent.ToStringPercent("F0")}\n模式: {(autoMode ? "自动" : "手动")}";
        }
    }
    public class Dialog_SelectResource : Window
    {
        private readonly StarcoreDriller drill;
        public override Vector2 InitialSize => new Vector2(500f, 600f);
        private Vector2 scrollPosition = Vector2.zero;
        private float scrollViewHeight;
        public Dialog_SelectResource(StarcoreDriller drill)
        {
            this.drill = drill;
            forcePause = true;
            absorbInputAroundWindow = true;
            doCloseX = true;
            closeOnClickedOutside = true;
        }
        private bool IsChunk(ThingDef def)
        {
            if (def.thingCategories == null) return false;
            return def.thingCategories.Any(tc => tc.defName == "Chunks");
        }
        private bool IsValidDrillTarget(ThingDef def)
        {
            if (def == null) return false;
            if (def.category != ThingCategory.Item) return false;
            if (def.IsApparel || def.IsWeapon || def.IsCorpse) return false;
            if (def.plant != null) return false;
            if (def.building != null && !def.IsNonResourceNaturalRock) return false;
            bool isDeep = def.deepCommonality > 0;
            bool isRock = def.IsNonResourceNaturalRock;
            bool isChunk = IsChunk(def);
            return isDeep || isRock || isChunk;
        }
        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 30f), "选择钻取目标资源：");
            Text.Font = GameFont.Small;
            float listTop = 40f;
            Rect outRect = new Rect(0f, listTop, inRect.width, inRect.height - listTop);
            float viewHeight = Mathf.Max(scrollViewHeight, outRect.height);
            Rect viewRect = new Rect(0f, 0f, inRect.width - 20f, viewHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);
            if (listing.ButtonText("无（默认基岩）"))
            {
                drill.SetResource(null);
                Close();
            }
            listing.GapLine();
            var candidates = DefDatabase<ThingDef>.AllDefs.Where(IsValidDrillTarget).OrderBy(d => d.label).ToList();
            if (candidates.Count == 0)
                listing.Label("未找到可钻取资源（请检查过滤条件）");
            else
            {
                foreach (ThingDef def in candidates)
                {
                    if (listing.ButtonText(def.LabelCap))
                    {
                        drill.SetResource(def);
                        Close();
                    }
                }
            }
            listing.End();
            Widgets.EndScrollView();
            scrollViewHeight = listing.CurHeight + 30f;
        }
    }
    [HarmonyPatch(typeof(WorkGiver_DeepDrill), nameof(WorkGiver_DeepDrill.HasJobOnThing))]
    public static class Patch_DeepDrill_HasJobOnThing
    {
        public static void Postfix(Pawn pawn, Thing t, bool forced, ref bool __result)
        {
            if (__result) return;
            if (!(t is Building building)) return;
            var comp = building.TryGetComp<StarcoreDriller>();
            if (comp == null) return;
            if (comp.IsAutoMode()) return;
            if (!comp.CanDrillNow()) return;
            if (t.Faction != pawn.Faction) return;
            if (!pawn.CanReserve(building, 1, -1, null, forced)) return;
            if (building.Map.designationManager.DesignationOn(building, DesignationDefOf.Uninstall) != null) return;
            if (building.IsBurning()) return;
            __result = true;
        }
    }
}
