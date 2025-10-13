using UnityEngine;
using Verse;

namespace WNA.WNAUtility
{
    public class RadSpreadConfig
    {
        // --- 基础参数 ---
        public int radLevel = 500;
        public float radius = 5f;
        public float edgeFactor = 0.4f;
        public float threshold = 0.6f;
        public int radLevelDelay = 90;
        // --- Hediff 动态参数 ---
        public bool isSeverityRadLevel = false;
        public float radLevelFactor = 1f;
        public bool isSeverityRadRadius = false;
        public float radRadiusFactor = 1f;
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
    public class RadFieldUtility
    {
        public static void RadSpread(IntVec3 c, Map map, RadSpreadConfig config, float finalRadius, int finalRadLevel)
        {
            RadField_MapComp radComp = map.GetComponent<RadField_MapComp>();
            if (radComp == null) return;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(c, finalRadius, true))
            {
                if (!cell.InBounds(map)) continue;
                float distance = cell.DistanceTo(c);
                int radToAdd = config.CalculateRadiation(distance, finalRadius, finalRadLevel);
                if (radToAdd > 0)
                    radComp.AddRad(cell, radToAdd);
            }
        }
    }
}
