using System.Collections.Generic;
using UnityEngine;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNAMapComp
{
    [StaticConstructorOnStartup]
    internal class SectionLayer_RadField : SectionLayer
    {
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

        private static readonly List<WNA_RadFieldDef> tmpDefs = new List<WNA_RadFieldDef>();
        private float[][] defAdj;
        private Color[] defColors;
        private float[] defAlphas;

        static SectionLayer_RadField()
        {
            Mat = MaterialPool.MatFrom(ShaderDatabase.VertexColor);
        }

        public override bool Visible => true;

        public SectionLayer_RadField(Section section) : base(section)
        {
            relevantChangeTypes = WNAMainDefOf.WNA_RadField;
        }

        private static void CollectDefs()
        {
            tmpDefs.Clear();
            List<WNA_RadFieldDef> all = DefDatabase<WNA_RadFieldDef>.AllDefsListForReading;
            for (int i = 0; i < all.Count; i++)
                tmpDefs.Add(all[i]);
        }

        private void EnsureBuffers(int count)
        {
            if (defAdj == null || defAdj.Length < count)
            {
                defAdj = new float[count][];
                defColors = new Color[count];
                defAlphas = new float[count];
            }
            for (int i = 0; i < count; i++)
            {
                if (defAdj[i] == null)
                    defAdj[i] = new float[9];
            }
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
            CollectDefs();
            if (rad == null || tmpDefs.Count == 0)
            {
                subMesh.disabled = true;
                return;
            }

            int defCount = tmpDefs.Count;
            EnsureBuffers(defCount);
            for (int d = 0; d < defCount; d++)
            {
                defColors[d] = tmpDefs[d].RenderColor;
                defAlphas[d] = Mathf.Clamp01(tmpDefs[d].overlayAlpha);
            }

            CellRect rect = section.CellRect;
            bool any = false;
            for (int x = rect.minX; x <= rect.maxX; x++)
            {
                for (int z = rect.minZ; z <= rect.maxZ; z++)
                {
                    IntVec3 center = new IntVec3(x, 0, z);
                    for (int d = 0; d < defCount; d++)
                    {
                        float max = Mathf.Max(1f, tmpDefs[d].intensityMax);
                        float centerVal = Mathf.Clamp01(rad.GetRad(center, tmpDefs[d]) / max);
                        for (int k = 0; k < 9; k++)
                        {
                            IntVec3 c = center + GenAdj.AdjacentCellsAndInsideForUV[k];
                            defAdj[d][k] = c.InBounds(Map) ? Mathf.Clamp01(rad.GetRad(c, tmpDefs[d]) / max) : centerVal;
                        }
                    }
                    for (int v = 0; v < 9; v++)
                    {
                        List<int> weights = vertexWeights[v];
                        float r = 0f, g = 0f, b = 0f, a = 0f;
                        for (int d = 0; d < defCount; d++)
                        {
                            float sum = 0f;
                            for (int m = 0; m < weights.Count; m++)
                                sum += defAdj[d][weights[m]];
                            float da = Mathf.Clamp01(sum / weights.Count) * defAlphas[d];
                            if (da <= 0.001f) continue;
                            float remain = 1f - a;
                            if (remain <= 0f) break;
                            Color color = defColors[d];
                            r += color.r * da * remain;
                            g += color.g * da * remain;
                            b += color.b * da * remain;
                            a += da * remain;
                        }
                        if (a > 0.01f)
                        {
                            any = true;
                            r /= a;
                            g /= a;
                            b /= a;
                        }
                        subMesh.colors.Add(new Color32(
                            (byte)(Mathf.Clamp01(r) * 255f),
                            (byte)(Mathf.Clamp01(g) * 255f),
                            (byte)(Mathf.Clamp01(b) * 255f),
                            (byte)(Mathf.Clamp01(a) * 255f)));
                    }
                }
            }
            if (any)
            {
                subMesh.disabled = false;
                subMesh.FinalizeMesh(MeshParts.Colors);
            }
            else
            {
                subMesh.disabled = true;
            }
        }
    }
}
