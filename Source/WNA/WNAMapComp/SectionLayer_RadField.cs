using System.Collections.Generic;
using UnityEngine;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNAMapComp
{
    [StaticConstructorOnStartup]
    internal class SectionLayer_RadField : SectionLayer
    {
        private const float MaxAlpha = 0.6f;
        private static readonly Material Mat;
        private static readonly List<List<int>> vertexWeights = new List<List<int>>
        {
            new List<int> { 0, 1, 2, 8 },
            new List<int> { 2, 8 },
            new List<int> { 2, 3, 4, 8 },
            new List<int> { 4, 8 },
            new List<int> { 4, 5, 6, 8 },
            new List<int> { 6, 8 },
            new List<int> { 6, 7, 0, 8 },
            new List<int> { 0, 8 },
            new List<int> { 8 }
        };

        private readonly float[] adjTmp = new float[9];

        static SectionLayer_RadField()
        {
            Mat = MaterialPool.MatFrom(ShaderDatabase.VertexColor);
        }

        public override bool Visible => true;

        public SectionLayer_RadField(Section section) : base(section)
        {
            relevantChangeTypes = WNARadMeshFlagDefOf.WNA_RadField;
        }

        public override void Regenerate()
        {
            LayerSubMesh subMesh = GetSubMesh(Mat);
            if (subMesh == null)
                return;
            if (subMesh.mesh.vertexCount == 0)
                SectionLayerGeometryMaker_Solid.MakeBaseGeometry(section, subMesh, AltitudeLayer.Terrain);
            subMesh.Clear(MeshParts.Colors);

            MapComp_RadField rad = Map.GetComponent<MapComp_RadField>();
            if (rad == null)
            {
                subMesh.disabled = true;
                return;
            }

            CellRect rect = section.CellRect;
            float max = Mathf.Max(1f, rad.radLevelMax);
            bool any = false;
            for (int x = rect.minX; x <= rect.maxX; x++)
            {
                for (int z = rect.minZ; z <= rect.maxZ; z++)
                {
                    IntVec3 center = new IntVec3(x, 0, z);
                    float centerVal = Mathf.Clamp01(rad.GetRad(center) / max);
                    for (int k = 0; k < 9; k++)
                    {
                        IntVec3 c = center + GenAdj.AdjacentCellsAndInsideForUV[k];
                        adjTmp[k] = c.InBounds(Map) ? Mathf.Clamp01(rad.GetRad(c) / max) : centerVal;
                    }
                    for (int v = 0; v < 9; v++)
                    {
                        List<int> weights = vertexWeights[v];
                        float sum = 0f;
                        for (int m = 0; m < weights.Count; m++)
                            sum += adjTmp[weights[m]];
                        float val = Mathf.Clamp01(sum / weights.Count);
                        if (val > 0.01f)
                            any = true;
                        byte a = (byte)(val * MaxAlpha * 255f);
                        subMesh.colors.Add(new Color32(200, 100, 0, a));
                    }
                }
            }
            if (any)
            {
                subMesh.disabled = false;
                subMesh.FinalizeMesh(MeshParts.Colors);
            }
            else
                subMesh.disabled = true;
        }
    }
}
