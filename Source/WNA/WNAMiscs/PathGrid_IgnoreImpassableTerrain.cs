using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace WNA.WNAMiscs
{
    public class PathGrid_IgnoreImpassableTerrain : PathGrid
    {
        public const int ImpassableTerrainCost = 300;

        public PathGrid_IgnoreImpassableTerrain(Map map, PathGridDef def) : base(map, def)
        {
        }

        public override int CalculatedCostAt(IntVec3 c, bool perceivedStatic, IntVec3 prevCell, int? baseCostOverride = null)
        {
            int num = base.CalculatedCostAt(c, perceivedStatic, prevCell, baseCostOverride);
            if (num < ImpassableCost || !c.InBounds(map))
            {
                return num;
            }
            if (c.GetEdifice(map) != null)
            {
                return num;
            }
            List<Thing> things = map.thingGrid.ThingsListAt(c);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i].def.passability == Traversability.Impassable)
                {
                    return num;
                }
            }
            TerrainDef terrain = c.GetTerrain(map);
            if (terrain == null || terrain.passability != Traversability.Impassable)
            {
                return num;
            }
            return Mathf.Max(baseCostOverride ?? 0, ImpassableTerrainCost);
        }
    }
}
