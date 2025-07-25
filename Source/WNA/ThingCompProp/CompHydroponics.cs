using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace WNA.ThingCompProp
{
    public class CompHydroponics : CompProperties
    {
        public float growthFactor = 1f;
        public float yieldFactor = 1f;
        public float extraFactor = 1f;
        public int plantCount = 1;
        public CompHydroponics()
        {
            compClass = typeof(Hydroponics);
            extraFactor = yieldFactor;
        }
    }
    public class Hydroponics : ThingComp
    {
        public CompHydroponics Props => (CompHydroponics)props;
        public ThingDef SelectedCrop;
        public int TicksToSpawn;
        public int CurrentHarvestCount;
        public int CurrentSpawnDelay;
        private readonly List<string> extraCrops = new List<string>
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
        private readonly List<string> extraItems = new List<string>
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
        private readonly List<int> extraAmounts = new List<int>
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
        #region 碍眼
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
            yield return new Command_Action
            {
                icon = SelectedCrop?.uiIcon ?? BaseContent.BadTex,
                defaultLabel = SelectedCrop == null ? "Select Crop" : "Change Crop",
                defaultDesc = SelectedCrop == null ?
                    "Select a crop to grow" :
                    $"Currently growing {SelectedCrop.label}. Click to change.",
                action = GenerateCropMenu
            };
            if (SelectedCrop != null)
            {
                yield return new Command_Action
                {
                    icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel"),
                    defaultLabel = "Clear Crop",
                    defaultDesc = "Stop growing current crop",
                    action = ResetState
                };
            }
        }
        public override void CompTick()
        {
            base.CompTick();
            if (Find.TickManager.TicksGame % 240 != 0) return;
            if (SelectedCrop != null && TicksToSpawn > 0)
            {
                TicksToSpawn = Math.Max(0, TicksToSpawn - 240);
                if (TicksToSpawn <= 0)
                {
                    SpawnHarvest();
                    TicksToSpawn = CurrentSpawnDelay;
                }
            }
        }
        public override string CompInspectStringExtra()
        {
            if (SelectedCrop == null)
            {
                return "No crop selected";
            }
            return string.Concat(
                $"Containing {Props.plantCount} plantmatter units\n",
                $"Growing {SelectedCrop.label}\n",
                $"Harvest in: {TicksToSpawn.ToStringTicksToPeriod()}\n",
                $"Estimated yield: {CurrentHarvestCount}"
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
                    if (extraCropDef == null)
                    {
                        Log.Warning($"Could not find ThingDef for {extraCrops[i]}. Skipping...");
                        continue;
                    }
                    if (extraCropDef != SelectedCrop) continue;
                    ThingDef item = DefDatabase<ThingDef>.GetNamedSilentFail(extraItems[i]);
                    if (item != null)
                    {
                        int amount = Mathf.RoundToInt(extraAmounts[i] * Props.extraFactor * Props.plantCount);
                        GenerateExtras(item, amount);
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
                if (thing == null) return;
                Thing th = ThingMaker.MakeThing(thing);
                th.stackCount = count;
                GenPlace.TryPlaceThing(th, parent.Position, parent.Map, ThingPlaceMode.Near);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to spawn {thing.LabelCap}: {ex}");
            }
        }
        public void GenerateCropMenu()
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>
                {
                    new FloatMenuOption("None", () => ChooseCrop(null))
                };
            if (Props.yieldFactor == 0)
            {
                foreach (ThingDef plantDef in DefDatabase<ThingDef>.AllDefs.Where(IsValidCrop))
                {
                    if (extraCrops.Contains(plantDef.defName))
                    {
                        try
                        {
                            ThingDef extraCropDef = DefDatabase<ThingDef>.GetNamedSilentFail(plantDef.defName);
                            if (extraCropDef == null)
                            {
                                Log.Warning($"Could not find ThingDef for {plantDef.defName} in the DefDatabase. Skipping...");
                                continue;
                            }
                            options.Add(new FloatMenuOption(
                                extraCropDef.LabelCap,
                                () => ChooseCrop(extraCropDef),
                                extraCropDef,
                                extraPartWidth: 29f,
                                extraPartOnGUI: rect => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, extraCropDef)
                            ));
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"An error occurred while processing plant definition {plantDef.defName}: {ex.Message}");
                        }
                    }
                }
            }
            else
            {
                foreach (ThingDef plantDef in DefDatabase<ThingDef>.AllDefs.Where(IsValidCrop))
                {
                    options.Add(new FloatMenuOption(
                        plantDef.LabelCap,
                        () => ChooseCrop(plantDef),
                        plantDef,
                        extraPartWidth: 29f,
                        extraPartOnGUI: rect => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, plantDef)
                    ));
                }
            }
            if (options.Count == 1)
            {
                options.Add(new FloatMenuOption("No valid crops available", null));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }
        #endregion
    }
}
