using RimWorld;
using UnityEngine;
using Verse;
using WNA.WNAArea;
using WNA.WNADefOf;
using WNA.WNAMapComp;

namespace WNA.WNAUtility
{
    internal class WNARadFieldUtility
    {
        internal static Area_RadClearArea GetRadClearArea(Map map)
        {
            return map?.areaManager?.Get<Area_RadClearArea>();
        }

        internal static Area_RadClearArea GetOrCreateRadClearArea(Map map)
        {
            if (map?.areaManager == null) return null;
            Area_RadClearArea area = map.areaManager.Get<Area_RadClearArea>();
            if (area == null)
            {
                area = new Area_RadClearArea(map.areaManager);
                map.areaManager.AllAreas.Add(area);
            }
            return area;
        }

        public static void RadSpread(IntVec3 c, Map map, RadSpreadConfig config, float finalRadius, int finalRadLevel)
        {
            RadSpread(c, map, config, config.FieldDef, finalRadius, finalRadLevel);
        }

        public static void RadSpread(IntVec3 c, Map map, RadSpreadConfig config, WNA_RadFieldDef def, float finalRadius, int finalRadLevel)
        {
            MapComp_RadField radMap = map.GetComponent<MapComp_RadField>();
            if (radMap == null || def == null) return;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(c, finalRadius, true))
            {
                if (!cell.InBounds(map)) continue;
                float distance = cell.DistanceTo(c);
                int radToAdd = config.CalculateRadiation(distance, finalRadius, finalRadLevel);
                if (radToAdd > 0)
                    radMap.AddRad(cell, def, radToAdd);
            }
        }

        internal static bool ImmuneToRadiation(Pawn pawn)
        {
            return TechnoConfigUtility.PawnHasFlag(pawn, TechnoFlag.ImmuneToRadiation);
        }
    }
    internal class RadSpreadConfig
    {
        // --- 基础参数 ---
        public WNA_RadFieldDef radField = null;
        public int radLevel = 0;
        public float radius = 5f;
        public float edgeFactor = 0.4f;
        public float threshold = 0.6f;
        public int radLevelDelay = 60;
        // --- Hediff 动态参数 ---
        public bool isSeverityRadLevel = false;
        public float radLevelFactor = 1f;
        public bool isSeverityRadRadius = false;
        public float radRadiusFactor = 1f;

        public WNA_RadFieldDef FieldDef => radField ?? WNAMainDefOf.WNA_RadField_Radiation;

        public int ResolveRadLevel()
        {
            if (radLevel > 0) return radLevel;
            return FieldDef?.intensityMax ?? 0;
        }

        public int CalculateRadiation(float distance, float finalRadius, int finalRadLevel)
        {
            if (distance >= finalRadius) return 0;
            float normDist = distance / finalRadius;
            int edgeRad = (int)(finalRadLevel * edgeFactor);
            float decayRange = finalRadLevel - edgeRad;
            float decayFactor = Mathf.Pow(normDist, 3f);
            int finalRad = (int)(edgeRad + decayRange * (1f - decayFactor));
            return Mathf.Max(0, finalRad);
        }
    }
}
