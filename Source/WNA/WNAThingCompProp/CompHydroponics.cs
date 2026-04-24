using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace WNA.WNAThingCompProp
{
    public class PropHydroponics : CompProperties
    {
        public float growthFactor = 1f;
        public float yieldFactor = 1f;
        public int plantCount = 1;
        public float lowPowerGrowthFactor = 0f;
        public PropHydroponics()
        {
            compClass = typeof(CompHydroponics);
        }
    }
    [StaticConstructorOnStartup]
    public class CompHydroponics : ThingComp
    {
        private const int WorkIntervalTicks = 60;
        private static readonly Texture2D CancelTex = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");
        private static readonly Texture2D SwitchTex = ContentFinder<Texture2D>.Get("UI/Designators/ForbidOff");
        private static readonly Texture2D WhatTex = ContentFinder<Texture2D>.Get("UI/Overlays/QuestionMark");
        private static Dictionary<bool, List<ThingDef>> cachedPlantsByMode = new Dictionary<bool, List<ThingDef>>();
        public PropHydroponics Props => (PropHydroponics)props;
        public ThingDef SelectedCrop;
        public bool treeMod = false;
        public int TicksToSpawn;
        public int CurrentHarvestCount;
        public int CurrentSpawnDelay;
        private float growthProgressBuffer;
        private CompPowerTrader powerComp;
        private CompRefuelable refuelableComp;
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            CacheComps();
            if (!respawningAfterLoad && SelectedCrop == null)
                ResetState();
        }
        public override void Notify_DefsHotReloaded()
        {
            base.Notify_DefsHotReloaded();
            cachedPlantsByMode?.Clear();
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Command_Action
            {
                icon = SwitchTex,
                defaultLabel = "WNA_Mode_What".Translate() + ":" +
                (treeMod == false
                    ? "WNA_Mode_Normal".Translate()
                    : "WNA_Mode_Tree".Translate()
                ),
                defaultDesc = "WNA_CompHydroponics_SwitchModeDesc".Translate(),
                action = delegate
                {
                    treeMod = !treeMod;
                    cachedPlantsByMode = null;
                    ResetState();
                }
            };
            yield return new Command_Action
            {
                icon = SelectedCrop?.uiIcon ?? WhatTex,
                defaultLabel = SelectedCrop == null
                    ? "WNA_CompHydroponics_SelectCrop".Translate()
                    : "WNA_CompHydroponics_ChangeCrop".Translate(),
                defaultDesc = SelectedCrop == null
                    ? "WNA_CompHydroponics_SelectCropDesc".Translate()
                    : "WNA_CompHydroponics_ChangeCropDesc".Translate(SelectedCrop.label),
                action = GenerateCropMenu
            };
            if (SelectedCrop != null)
            {
                yield return new Command_Action
                {
                    icon = CancelTex,
                    defaultLabel = "WNA_CompHydroponics_ClearCrop".Translate(),
                    defaultDesc = "WNA_CompHydroponics_ClearCropDesc".Translate(),
                    action = ResetState
                };
            }
        }
        public override void CompTick()
        {
            base.CompTick();
            if (SelectedCrop == null || !parent.IsHashIntervalTick(WorkIntervalTicks))
                return;
            TickGrowth(WorkIntervalTicks);
        }
        public override string CompInspectStringExtra()
        {
            var sb = new StringBuilder();
            sb.AppendLine("WNA_Mode_What".Translate() + ":" +
                (treeMod == false ? "WNA_Mode_Normal".Translate() : "WNA_Mode_Tree".Translate()));
            if (SelectedCrop == null)
            {
                sb.Append("WNA_CompHydroponics_NoCrop".Translate());
                return sb.ToString();
            }
            if (powerComp != null && !powerComp.PowerOn)
                sb.AppendLine("WNA_CompHydroponics_LowPower".Translate().ToString());
            if (refuelableComp != null && !refuelableComp.HasFuel)
                sb.AppendLine("WNA_CompHydroponics_NoFuel".Translate().ToString());
            sb.AppendLine("WNA_CompHydroponics_Contains".Translate(Props.plantCount).ToString());
            sb.AppendLine("WNA_CompHydroponics_Growing".Translate(SelectedCrop.label).ToString());
            sb.AppendLine("WNA_CompHydroponics_HarvestIn".Translate(TicksToSpawn.ToString()).ToString());
            sb.AppendLine("WNA_CompHydroponics_Yield".Translate(CurrentHarvestCount).ToString());
            sb.Append("WNA_CompHydroponics_CurrentGrowthFactor".Translate(GetCurrentGrowthFactor().ToString("F2")));
            return sb.ToString();
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look(ref SelectedCrop, "SelectedCrop");
            Scribe_Values.Look(ref TicksToSpawn, "TicksToSpawn");
            Scribe_Values.Look(ref CurrentHarvestCount, "CurrentHarvestCount");
            Scribe_Values.Look(ref CurrentSpawnDelay, "CurrentSpawnDelay");
            Scribe_Values.Look(ref growthProgressBuffer, "GrowthProgressBuffer", 0f);
            Scribe_Values.Look(ref treeMod, "TreeMode", false);
        }
        private void ResetState()
        {
            SelectedCrop = null;
            TicksToSpawn = 0;
            CurrentHarvestCount = 0;
            CurrentSpawnDelay = 0;
            growthProgressBuffer = 0f;
        }
        private void ChooseCrop(ThingDef crop)
        {
            if (crop == null || !IsValidForCurrentMode(crop))
            {
                ResetState();
                return;
            }
            SelectedCrop = crop;
            CurrentSpawnDelay = Mathf.Max(1, Mathf.RoundToInt(crop.plant.growDays * 60000f));
            CurrentHarvestCount = GetCurrentHarvestCount(crop.plant.harvestYield);
            TicksToSpawn = CurrentSpawnDelay;
            growthProgressBuffer = 0f;
        }
        private int GetCurrentHarvestCount(float yield)
        {
            int plantCount = Mathf.Max(1, Props.plantCount);
            float difficultyYield = Mathf.Max(1f, Find.Storyteller?.difficulty?.cropYieldFactor ?? 1f);
            return Mathf.Max(1, Mathf.RoundToInt(yield * Props.yieldFactor * difficultyYield * plantCount));
        }
        private bool IsValidForCurrentMode(ThingDef def)
        {
            return treeMod == false ? IsValidCropDef(def) : IsValidTreeDef(def);
        }
        private void GenerateCropMenu()
        {
            List<ThingDef> sortedPlants = GetValidPlants();
            var options = new List<FloatMenuOption>(sortedPlants.Count + 1)
            {
                new FloatMenuOption("WNA_None".Translate(), () => ChooseCrop(null))
            };
            foreach (ThingDef plantDef in sortedPlants)
            {
                ThingDef localDef = plantDef;
                options.Add(new FloatMenuOption(
                    localDef.LabelCap,
                    () => ChooseCrop(localDef),
                    localDef,
                    extraPartWidth: 29f,
                    extraPartOnGUI: rect =>
                        Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, localDef)
                ));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }
        private void CacheComps()
        {
            powerComp = parent.GetComp<CompPowerTrader>();
            refuelableComp = parent.GetComp<CompRefuelable>();
        }
        private float GetCurrentGrowthFactor()
        {
            float factor = Mathf.Max(0f, Props.growthFactor);
            float lowPowerMul = Mathf.Max(0f, Props.lowPowerGrowthFactor);
            if (powerComp != null && !powerComp.PowerOn)
                factor *= lowPowerMul;
            if (refuelableComp != null && !refuelableComp.HasFuel)
                factor *= lowPowerMul;
            return factor;
        }
        private void TickGrowth(int deltaTicks)
        {
            float currentGrowthFactor = GetCurrentGrowthFactor();
            if (currentGrowthFactor <= 0f)
                return;
            growthProgressBuffer += deltaTicks * currentGrowthFactor;
            int grownTicks = Mathf.FloorToInt(growthProgressBuffer);
            if (grownTicks <= 0)
                return;
            growthProgressBuffer -= grownTicks;
            TicksToSpawn = Math.Max(0, TicksToSpawn - grownTicks);
            if (TicksToSpawn > 0)
                return;
            SpawnHarvest();
            TicksToSpawn = CurrentSpawnDelay;
            growthProgressBuffer = 0f;
        }
        private List<ThingDef> GetValidPlants()
        {
            if (cachedPlantsByMode == null)
                cachedPlantsByMode = new Dictionary<bool, List<ThingDef>>();
            if (!cachedPlantsByMode.TryGetValue(treeMod, out var list))
            {
                var rawList = new List<ThingDef>();
                foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
                {
                    if (IsValidForCurrentMode(def))
                        rawList.Add(def);
                }
                list = rawList.OrderBy(pl => pl.modContentPack?.PackageIdPlayerFacing ?? "0_Unknown")
                              .ThenBy(pl => pl.defName)
                              .ToList();
                cachedPlantsByMode[treeMod] = list;
            }
            return list;
        }
        private static bool IsValidCropDef(ThingDef plantDef)
        {
            return plantDef?.plant != null &&
                    (plantDef.plant.treeCategory == TreeCategory.None ||
                    plantDef.plant.treeCategory == TreeCategory.Mini) &&
                !plantDef.plant.isStump &&
                plantDef.plant.harvestedThingDef != null;
        }
        private static bool IsValidTreeDef(ThingDef plantDef)
        {
            return plantDef?.plant != null &&
                    (plantDef.plant.treeCategory == TreeCategory.Full ||
                    plantDef.plant.treeCategory == TreeCategory.Super) &&
                !plantDef.plant.isStump &&
                plantDef.plant.harvestedThingDef != null;
        }
        private void SpawnHarvest()
        {
            if (SelectedCrop == null || CurrentHarvestCount <= 0 || !parent.Spawned || parent.Map == null)
                return;
            ThingDef harvestDef = SelectedCrop.plant?.harvestedThingDef;
            int remain = CurrentHarvestCount;
            PlaceThingStack(harvestDef, remain);
            if (SelectedCrop.thingClass.Name == "HarbingerTree")
            {
                ThingDef meat = ThingDefOf.Meat_Twisted;
                int meat_count = GetCurrentHarvestCount(30);
                PlaceThingStack(meat, meat_count);
            }
        }
        private void PlaceThingStack(ThingDef def, int count)
        {
            if (count <= 0 || def == null)
                return;
            int stackLimit = Mathf.Max(1, def.stackLimit);
            while (count > 0)
            {
                int toSpawn = Mathf.Min(count, stackLimit);
                Thing thing = ThingMaker.MakeThing(def);
                thing.stackCount = toSpawn;
                if (!GenPlace.TryPlaceThing(thing, parent.Position, parent.Map, ThingPlaceMode.Near))
                {
                    thing.Destroy();
                    Log.Warning($"[WNA] Failed to place harvest {def.defName} near {parent.Position}.");
                    break;
                }
                count -= toSpawn;
            }
        }
    }
}