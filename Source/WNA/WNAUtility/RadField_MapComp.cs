using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using WNA.DMExtension;
using WNA.WNADefOf;

namespace WNA.WNAUtility
{
    public class RadField_MapComp : MapComponent, ICellBoolGiver
    {
        private List<int> radLevel;
        public int radLevelMax = 2000;
        private int radLevelDelay = 90;
        private int radLevelDecay = 2;
        private float radLevelFactor = 0.1f;
        public RadField_MapComp(Map map) : base(map) { }
        private CellBoolDrawer drawerInt;
        public CellBoolDrawer Drawer
        {
            get
            {
                if (drawerInt == null)
                    drawerInt = new CellBoolDrawer(GetCellBool, () => Color.white, GetCellExtraColor, map.Size.x, map.Size.z, 3600);
                return drawerInt;
            }
        }
        public Color Color => Color.white;
        public bool GetCellBool(int index)
        {
            IntVec3 c = map.cellIndices.IndexToCell(index);
            if (!c.InBounds(map) || c.Filled(map) || c.Fogged(map))
                return false;
            return GetRad(c) > 0;
        }
        public Color GetCellExtraColor(int index)
        {
            IntVec3 c = map.cellIndices.IndexToCell(index);
            int rawRad = GetRad(c);
            float nrad = (radLevelMax > 0) ? (float)rawRad / (float)radLevelMax : 0f;
            Color final = Color.clear;
            float r = Mathf.Lerp(0.784314f, 1f, Mathf.Clamp01(nrad));
            float g = Mathf.Lerp(0.392157f, 0.607843f, Mathf.Clamp01(nrad));
            float a = Mathf.Lerp(0.1f, 1f, Mathf.Clamp01(nrad));
            final.r = r;
            final.g = g;
            final.a = a;
            return final;
        }
        public override void FinalizeInit()
        {
            base.FinalizeInit();
            if (radLevel == null)
            {
                radLevel = new List<int>(map.cellIndices.NumGridCells);
                for (int i = 0; i < map.cellIndices.NumGridCells; i++)
                {
                    radLevel.Add(0);
                }
            }
        }
        public int GetRad(IntVec3 c)
        {
            if (!c.InBounds(map)) return 0;
            return radLevel[map.cellIndices.CellToIndex(c)];
        }
        public void AddRad(IntVec3 c, int amount)
        {
            if (!c.InBounds(map)) return;
            int index = map.cellIndices.CellToIndex(c);
            radLevel[index] = Mathf.Clamp(radLevel[index] + amount, 0, radLevelMax);
            Drawer.SetDirty();
        }
        private bool IsImmuneToRadiation(Pawn pawn)
        {
            TechnoConfig pawnConfig = TechnoConfig.Get(pawn.def);
            if (pawnConfig != null && pawnConfig.immuneToRadiation == true)
                return true;
            if (pawn.apparel != null)
            {
                foreach (Apparel apparel in pawn.apparel.WornApparel)
                {
                    TechnoConfig apparelConfig = TechnoConfig.Get(apparel.def);
                    if (apparelConfig != null && apparelConfig.immuneToRadiation == true)
                        return true;
                }
            }
            if (pawn.equipment != null && pawn.equipment.Primary != null)
            {
                TechnoConfig equipmentConfig = TechnoConfig.Get(pawn.equipment.Primary.def);
                if (equipmentConfig != null && equipmentConfig.immuneToRadiation == true)
                    return true;
            }
            return false;
        }
        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (map.IsHashIntervalTick(250))
            {
                for (int i = 0; i < radLevel.Count; i++)
                {
                    IntVec3 c = map.cellIndices.IndexToCell(i);
                    if (radLevel[i] > 0)
                    {
                        radLevel[i] = Mathf.Max(0, radLevel[i] - radLevelDecay);
                        Drawer.SetDirty();
                    }
                }
            }
            if (map.IsHashIntervalTick(radLevelDelay))
            {
                List<Pawn> pawnErad = map.mapPawns.AllPawnsSpawned.ToList();
                foreach (Pawn pawn in pawnErad)
                {
                    if (IsImmuneToRadiation(pawn)) continue;
                    IntVec3 c = pawn.Position;
                    if (!c.InBounds(map)) continue;
                    int rad = GetRad(c);
                    if (rad >= 0)
                    {
                        float dmg = rad * radLevelFactor;
                        if (dmg > 0)
                        {
                            DamageInfo dinfo = new DamageInfo(WNAMainDefOf.WNA_RadBurn, dmg, float.MaxValue, -1f, null, null, null);
                            pawn.TakeDamage(dinfo);
                        }
                    }
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref radLevel, "radLevel", LookMode.Value);
        }
        public override void MapComponentDraw()
        {
            base.MapComponentDraw();
            Drawer.MarkForDraw();
            Drawer.CellBoolDrawerUpdate();
        }
    }
}