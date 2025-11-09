using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace WNA.ThingCompProp
{
    public class CompHydroponics : CompProperties
    {
        public float growthFactor = 1f;
        public float yieldFactor = 1f;
        public float extraYieldFactor = 1f;
        public int plantCount = 1;
        public float lowPowerGrowthFactor = 0f;
        public bool isManual = true;
        public CompHydroponics()
        {
            compClass = typeof(Hydroponics);
            extraYieldFactor = yieldFactor;
        }
    }
    public class Hydroponics : ThingComp
    {
        public CompHydroponics Props => (CompHydroponics)props;
        public ThingDef SelectedCrop;
        public int TicksToSpawn;
        public int CurrentHarvestCount;
        public int CurrentSpawnDelay;
        public readonly List<string> extraCrops = new List<string>
            {
                "Plant_TreeHarbinger",
                /* mashed's ashlands */
                "Mashed_Ashlands_Plant_TreeEmperorParasol",
                "Mashed_Ashlands_Plant_TreeKwamacap",
                "Mashed_Ashlands_Plant_TreeWeepingLeathercap",
                /* more tree product */
                "Plant_TreePine",
                "Plant_TreePalm",
                "Plant_TreeCecropia",
                "Plant_TreeMaple",
                "Plant_TreeOak",
                "Plant_TreeDrago",
                "Plant_Timbershroom",
                "Plant_TreeGrayPine",
                "Plant_RatPalm",
                "Plant_SaguaroCactus",
                "Plant_PebbleCactus",
                "RG_Plant_SpikedBoilingTreePine",
                "RG_Plant_BoilingTreePine",
                "RG_Plant_OrangeTreePine",
                "RG_Plant_BlueTreePine",
                "RG_Plant_LargeTreePine",
                "RG_Plant_TreeDwarfPalm",
                "RG_Plant_TallPalmTree",
                "RG_Tree_TundraTreePine",
                "RG_Plant_TreeToxipine",
                "RG_Plant_TreeSplitpine",
                "Plant_SnowPine",
                "Plant_ColdPine",
            };
        public readonly List<string> extraItems = new List<string>
            {
                "Meat_Twisted",
                /* mashed's ashlands */
                "Mashed_Ashlands_RawEmperorParasolMoss",
                "Mashed_Ashlands_RawAshFungus",
                "Mashed_Ashlands_FungalLeather",
                /* more tree product */
                "Romy_RawPineCones",
                "Romy_RawCoconuts",
                "Romy_RawCecropiaFruits",
                "Romy_RawMapleSap",
                "Romy_RawAcorns",
                "Romy_DragoTreeSap",
                "RawFungus",
                "Romy_RawPineCones",
                "Romy_RawCoconuts",
                "Romy_RawCactusFlesh",
                "Romy_RawCactusFlesh",
                "Romy_RawPineCones",
                "Romy_RawPineCones",
                "Romy_RawPineCones",
                "Romy_RawPineCones",
                "Romy_RawPineCones",
                "Romy_RawCoconuts",
                "Romy_RawCoconuts",
                "Romy_RawPineCones",
                "Romy_RawPineCones",
                "Romy_RawPineCones",
                "Romy_RawPineCones",
                "Romy_RawPineCones",
            };
        public readonly List<int> extraAmounts = new List<int>
            {
                30,
                /* mashed's ashlands */
                15,
                10,
                10,
                /* more tree product */
                10,
                4,
                8,
                8,
                10,
                4,
                20,
                8,
                3,
                6,
                6,
                10,
                10,
                10,
                10,
                10,
                4,
                4,
                10,
                8,
                8,
                10,
                10,
            };
        #region
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad && SelectedCrop == null)
            {
                ResetState();
            }
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Props.isManual)
            {
                yield return new Command_Action
                {
                    icon = SelectedCrop?.uiIcon ?? BaseContent.BadTex,
                    defaultLabel = SelectedCrop == null ? "WNA.CompHydroponics.SelectCrop".Translate() : "WNA.CompHydroponics.ChangeCrop".Translate(),
                    defaultDesc = SelectedCrop == null ?
                        "WNA.CompHydroponics.SelectCropDesc".Translate() :
                        "WNA.CompHydroponics.ChangeCropDesc".Translate(SelectedCrop.label),
                    action = CreateAndAssignAdjustJob
                };
            }
            else
            {
                yield return new Command_Action
                {
                    icon = SelectedCrop?.uiIcon ?? BaseContent.BadTex,
                    defaultLabel = SelectedCrop == null ? "WNA.CompHydroponics.SelectCrop".Translate() : "WNA.CompHydroponics.ChangeCrop".Translate(),
                    defaultDesc = SelectedCrop == null ?
                        "WNA.CompHydroponics.SelectCropDesc".Translate() :
                        "WNA.CompHydroponics.ChangeCropDesc".Translate(SelectedCrop.label),
                    action = GenerateCropMenu
                };
            }
            if (SelectedCrop != null)
            {
                yield return new Command_Action
                {
                    icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel"),
                    defaultLabel = "WNA.CompHydroponics.ClearCrop".Translate(),
                    defaultDesc = "WNA.CompHydroponics.ClearCropDesc".Translate(),
                    action = ResetState
                };
            }
        }
        public float GetCurrentGrowthFactor()
        {
            float currentGrowthFactor = Props.growthFactor;
            CompPowerTrader powerComp = parent.GetComp<CompPowerTrader>();
            if (powerComp != null && !powerComp.PowerOn) currentGrowthFactor *= Props.lowPowerGrowthFactor;
            CompRefuelable refuelableComp = parent.GetComp<CompRefuelable>();
            if (refuelableComp != null && !refuelableComp.HasFuel) currentGrowthFactor *= Props.lowPowerGrowthFactor;
            return currentGrowthFactor;
        }
        public override void CompTick()
        {
            base.CompTick();
            if (Find.TickManager.TicksGame % 60 != 0) return;

            if (SelectedCrop != null)
            {
                float currentGrowthFactor = GetCurrentGrowthFactor();
                if (currentGrowthFactor > 0)
                {
                    TicksToSpawn = Math.Max(0, TicksToSpawn - (int)(60 * currentGrowthFactor));
                }
                if (TicksToSpawn <= 0)
                {
                    SpawnHarvest();
                    TicksToSpawn = CurrentSpawnDelay;
                }
            }
        }
        public override string CompInspectStringExtra()
        {
            if (SelectedCrop == null) return "WNA.CompHydroponics.NoCrop".Translate();
            CompPowerTrader powerComp = parent.GetComp<CompPowerTrader>();
            CompRefuelable refuelableComp = parent.GetComp<CompRefuelable>();
            string statusString = "";
            if (powerComp != null && !powerComp.PowerOn)
            {
                statusString += "WNA.CompHydroponics.LowPower".Translate() + "\n";
            }
            if (refuelableComp != null && !refuelableComp.HasFuel)
            {
                statusString += "WNA.CompHydroponics.NoFuel".Translate() + "\n";
            }
            string growthFactorString = "WNA.CompHydroponics.CurrentGrowthFactor".Translate(GetCurrentGrowthFactor().ToString("F2"));
            return string.Concat(
                statusString,
                "WNA.CompHydroponics.Contains".Translate(Props.plantCount) + "\n",
                "WNA.CompHydroponics.Growing".Translate(SelectedCrop.label) + "\n",
                "WNA.CompHydroponics.HarvestIn".Translate(TicksToSpawn.ToStringTicksToPeriod()) + "\n",
                "WNA.CompHydroponics.Yield".Translate(CurrentHarvestCount) + "\n",
                growthFactorString
            );
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look(ref SelectedCrop, "SelectedCrop");
            Scribe_Values.Look(ref TicksToSpawn, "TicksToSpawn");
            Scribe_Values.Look(ref CurrentHarvestCount, "CurrentHarvestCount");
            Scribe_Values.Look(ref CurrentSpawnDelay, "CurrentSpawnDelay");
        }
        public void ResetState()
        {
            SelectedCrop = null;
            TicksToSpawn = 0;
            CurrentHarvestCount = 0;
            CurrentSpawnDelay = 0;
        }
        public bool IsValidCrop(ThingDef plantDef)
        {
            return plantDef?.plant != null &&
                !plantDef.plant.isStump &&
                plantDef.plant.harvestedThingDef != null;
        }
        public void ChooseCrop(ThingDef newCrop)
        {
            if (newCrop != null && newCrop.plant?.harvestedThingDef != null)
            {
                SelectedCrop = newCrop;
                float baseGrowDays = newCrop.plant.growDays / Props.growthFactor;
                CurrentSpawnDelay = (int)(baseGrowDays * 60000f);
                CurrentHarvestCount = Mathf.RoundToInt(newCrop.plant.harvestYield * Props.yieldFactor * Props.plantCount);
                TicksToSpawn = CurrentSpawnDelay;
            }
            else ResetState();
        }
        public void SpawnHarvest()
        {
            if (SelectedCrop == null || CurrentHarvestCount <= 0) return;
            try
            {
                Thing harvestThing = ThingMaker.MakeThing(SelectedCrop.plant.harvestedThingDef);
                harvestThing.stackCount = CurrentHarvestCount;
                GenPlace.TryPlaceThing(harvestThing, parent.Position, parent.Map, ThingPlaceMode.Near);
                for (int i = 0; i < extraCrops.Count; i++)
                {
                    ThingDef extraCropDef = DefDatabase<ThingDef>.GetNamedSilentFail(extraCrops[i]);
                    ThingDef itemDef = DefDatabase<ThingDef>.GetNamedSilentFail(extraItems[i]);
                    if (extraCropDef != null && extraCropDef == SelectedCrop && itemDef != null)
                    {
                        int amount = Mathf.RoundToInt(extraAmounts[i] * Props.extraYieldFactor * Props.plantCount);
                        GenerateExtras(itemDef, amount);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to spawn harvest: {ex}");
            }
        }
        public void GenerateExtras(ThingDef thing, int count)
        {
            if (count <= 0) return;
            try
            {
                Thing th = ThingMaker.MakeThing(thing);
                th.stackCount = count;
                GenPlace.TryPlaceThing(th, parent.Position, parent.Map, ThingPlaceMode.Near);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to spawn {thing.LabelCap}: {ex}");
            }
        }
        private void CreateAndAssignAdjustJob()
        {
            List<Pawn> freeColonists = Find.AnyPlayerHomeMap.mapPawns.FreeColonists;
            Pawn actor = null;
            if (freeColonists.Count > 0)
            {
                int randomIndex = Rand.Range(0, freeColonists.Count);
                actor = freeColonists[randomIndex];
            }
            if (actor == null)
            {
                Messages.Message("WNA_NoAvailableColonistForAdjustment".Translate(), MessageTypeDefOf.NeutralEvent, false);
                return;
            }
            Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("WNA_Job_AdjustHydroponics"), parent);
            actor.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }
        public void GenerateCropMenu()
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            options.Add(new FloatMenuOption("WNA_None".Translate(), () => ChooseCrop(null)));
            foreach (ThingDef plantDef in DefDatabase<ThingDef>.AllDefs.Where(IsValidCrop))
            {
                if (plantDef == null) continue;
                if (Props.yieldFactor == 0 && !extraCrops.Contains(plantDef.defName)) continue;
                options.Add(new FloatMenuOption(
                    plantDef.LabelCap,
                    () => ChooseCrop(plantDef),
                    plantDef,
                    extraPartWidth: 29f,
                    extraPartOnGUI: rect => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, plantDef)
                ));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }
        #endregion
    }
}
