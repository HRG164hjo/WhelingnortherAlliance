using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNAMiscs
{
    /*public static class TerrainDefGenerator_WNAStuffFloor
    {
        internal static TerrainDef ter = WNAMainDefOf.WNA_FocusFloor;
        internal static List<TerrainAffordanceDef> affordances = new List<TerrainAffordanceDef>
            {
                TerrainAffordanceDefOf.Walkable,
                TerrainAffordanceDefOf.Light,
                TerrainAffordanceDefOf.Medium,
                TerrainAffordanceDefOf.Heavy,
                DefDatabase<TerrainAffordanceDef>.GetNamedSilentFail("Substructure")
            };
        public static IEnumerable<TerrainDef> ImpliedTerrainDefs(bool hotReload = false)
        {
            Log.Warning("[WNA] Generator class loaded");
            foreach (ThingDef stuff in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (stuff.IsStuff && !(stuff.thingCategories.Contains(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("Leathers"))))
                {
                    string defName = "WNA_StuffFloor_" + stuff.defName;
                    if (DefDatabase<TerrainDef>.GetNamed(defName, false) != null) continue;
                    TerrainDef terrain = hotReload ?
                        (DefDatabase<TerrainDef>.GetNamed(defName, errorOnFail: false) ?? new TerrainDef()) :
                        new TerrainDef();
                    terrain.defName = defName;
                    terrain.label = stuff.LabelCap + ter.label;
                    terrain.texturePath = ter.texturePath;
                    terrain.color = stuff.stuffProps.color;
                    terrain.researchPrerequisites = ter.researchPrerequisites;
                    terrain.burnedDef = ter.burnedDef;
                    terrain.costList = new List<ThingDefCountClass>
                    {
                        new ThingDefCountClass(stuff, 1)
                    };
                    terrain.description = ter.description;
                    terrain.designatorDropdown = DefDatabase<DesignatorDropdownGroupDef>.GetNamedSilentFail("WNA_StuffFloor_Dropdown");
                    terrain.uiOrder = ter.uiOrder;
                    terrain.statBases = ter.statBases?.ListFullCopy();
                    terrain.constructionSkillPrerequisite = ter.constructionSkillPrerequisite;
                    terrain.canGenerateDefaultDesignator = ter.canGenerateDefaultDesignator;
                    terrain.tags = ter.tags;
                    terrain.dominantStyleCategory = ter.dominantStyleCategory;
                    terrain.layerable = true;
                    terrain.affordances = new List<TerrainAffordanceDef>(affordances.Where(a => a != null));
                    terrain.constructEffect = stuff.constructEffect ?? 
                        DefDatabase<EffecterDef>.GetNamedSilentFail("ConstructWood");
                    terrain.pollutionColor = new Color(1f, 1f, 1f, 0.8f);
                    terrain.pollutionOverlayScale = ter.pollutionOverlayScale;
                    terrain.pollutionOverlayTexturePath = ter.pollutionOverlayTexturePath;
                    terrain.terrainAffordanceNeeded = ter.terrainAffordanceNeeded;
                    terrain.renderPrecedence = ter.renderPrecedence;
                    terrain.traversedThought = ter.traversedThought;
                    terrain.isPaintable = ter.isPaintable;
                    terrain.generatedFilth = ter.generatedFilth;
                    terrain.isFoundation = ter.isFoundation;
                    terrain.pathCost = ter.pathCost;
                    terrain.fertility = ter.fertility;
                    terrain.filthAcceptanceMask = FilthSourceFlags.None;
                    terrain.designationCategory = ter.designationCategory ?? DesignationCategoryDefOf.Floors;
                    Log.Warning($"[WNA] Generating stuff floor for {stuff.LabelCap} {stuff.defName}");
                    if (ModsConfig.BiotechActive)
                        terrain.pollutionShaderType = ter.pollutionShaderType;
                    yield return terrain;
                }
            }
        }
    }*/
}
