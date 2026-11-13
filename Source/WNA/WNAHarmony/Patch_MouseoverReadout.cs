using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using WNA.WNADefOf;
using WNA.WNAMapComp;

namespace WNA.WNAHarmony
{
    internal class Patch_MouseoverReadout
    {
        private const float BotLeftX = 15f;
        private const float BotLeftY = 65f;
        private const float YInterval = 19f;

        [HarmonyPatch(typeof(MouseoverReadout), nameof(MouseoverReadout.MouseoverReadoutOnGUI))]
        internal static class MouseoverReadout_RadField_Patch
        {
            static void Postfix()
            {
                if (Event.current.type != EventType.repaint) return;
                if (Find.MainTabsRoot.OpenTab != null) return;
                Map map = Find.CurrentMap;
                if (map == null) return;
                IntVec3 cell = UI.MouseCell();
                if (!cell.InBounds(map) || cell.Fogged(map)) return;
                MapComp_RadField rad = map.GetComponent<MapComp_RadField>();
                if (rad == null) return;

                List<WNA_RadFieldDef> defs = DefDatabase<WNA_RadFieldDef>.AllDefsListForReading;
                float baseOffset = VanillaReadoutHeight(map, cell);
                int shown = 0;
                Color prevColor = GUI.color;
                Text.Font = GameFont.Small;
                for (int i = 0; i < defs.Count; i++)
                {
                    WNA_RadFieldDef def = defs[i];
                    if (!def.showInReadout || def.intensityMax <= 0) continue;
                    int value = rad.GetRad(cell, def);
                    if (value <= 0) continue;
                    float yOffset = baseOffset + shown * YInterval;
                    Rect rect = new Rect(BotLeftX, UI.screenHeight - BotLeftY - yOffset, 999f, 999f);
                    Color color = def.RenderColor;
                    GUI.color = new Color(color.r, color.g, color.b, 0.95f);
                    Widgets.Label(rect, "WNA_RadFieldReadout".Translate(def.LabelCap, ((float)value / def.intensityMax).ToStringPercent(), value, def.intensityMax));
                    shown++;
                }
                GUI.color = prevColor;
            }
        }
        private static float VanillaReadoutHeight(Map map, IntVec3 cell)
        {
            float num = 0f;
            num += YInterval;
            if (map.Biome.inVacuum) num += YInterval;
            num += YInterval;
            if (cell.GetZone(map) != null) num += YInterval;
            if (map.snowGrid.GetDepth(cell) > 0.03f) num += YInterval;
            if (ModsConfig.OdysseyActive && map.sandGrid.GetDepth(cell) > 0.03f) num += YInterval;
            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                CompSelectProxy proxy = thing.TryGetComp<CompSelectProxy>();
                if (proxy != null && proxy.thingToSelect != null)
                    thing = proxy.thingToSelect;
                if (thing.def.category != ThingCategory.Mote && (!(thing is Pawn pawn) || !pawn.IsHiddenFromPlayer()))
                    num += YInterval;
            }
            if (cell.GetRoof(map) != null) num += YInterval;
            if (map.gasGrid != null && map.gasGrid.AnyGasAt(cell))
            {
                if (map.gasGrid.DensityAt(cell, GasType.BlindSmoke) > 0) num += YInterval;
                if (map.gasGrid.DensityAt(cell, GasType.ToxGas) > 0) num += YInterval;
                if (map.gasGrid.DensityAt(cell, GasType.RotStink) > 0) num += YInterval;
                if (map.gasGrid.DensityAt(cell, GasType.DeadlifeDust) > 0) num += YInterval;
            }
            if (ModsConfig.OdysseyActive && map.waterBodyTracker != null
                && map.waterBodyTracker.TryGetWaterBodyAt(cell, out var body) && body.HasFish)
                num += YInterval;
            return num;
        }
    }
}
