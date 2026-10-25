using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using WNA.WNADefOf;
using WNA.WNAUtility;

namespace WNA.WNAMapComp
{
    internal class MapComp_RadField : MapComponent
    {
        private List<int> radLevel;
        public readonly int radLevelMax = 500;
        private readonly int radLevelDelay = 45;
        private readonly int radLevelDecay = 1;
        private readonly float radLevelFactor = 0.1f;
        public MapComp_RadField(Map map) : base(map) { }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            if (radLevel == null)
            {
                radLevel = new List<int>(map.cellIndices.NumGridCells);
                for (int i = 0; i < map.cellIndices.NumGridCells; i++)
                    radLevel.Add(0);
            }
        }
        public int GetRad(IntVec3 c)
        {
            if (radLevel == null || !c.InBounds(map)) return 0;
            return radLevel[map.cellIndices.CellToIndex(c)];
        }
        public void AddRad(IntVec3 c, int amount)
        {
            if (radLevel == null || !c.InBounds(map)) return;
            int index = map.cellIndices.CellToIndex(c);
            int next = Mathf.Clamp(radLevel[index] + amount, 0, radLevelMax);
            if (next == radLevel[index]) return;
            radLevel[index] = next;
            MarkRadDirty(c);
        }
        public void SetRad(IntVec3 c, int value)
        {
            if (radLevel == null || !c.InBounds(map)) return;
            int index = map.cellIndices.CellToIndex(c);
            int next = Mathf.Clamp(value, 0, radLevelMax);
            if (next == radLevel[index]) return;
            radLevel[index] = next;
            MarkRadDirty(c);
        }
        private void MarkRadDirty(IntVec3 c)
        {
            map.mapDrawer.MapMeshDirty(c, WNARadMeshFlagDefOf.WNA_RadField, regenAdjacentCells: true, regenAdjacentSections: false);
        }
        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (radLevel == null) return;
            if (map.IsHashIntervalTick(250))
            {
                for (int i = 0; i < radLevel.Count; i++)
                {
                    int cur = radLevel[i];
                    if (cur <= 0) continue;
                    int next = Mathf.Max(0, cur - radLevelDecay);
                    radLevel[i] = next;
                    if (next == 0 || next % 5 == 0)
                        MarkRadDirty(map.cellIndices.IndexToCell(i));
                }
            }
            if (map.IsHashIntervalTick(radLevelDelay))
            {
                List<Pawn> pawnErad = map.mapPawns.AllPawnsSpawned.ToList();
                foreach (Pawn pawn in pawnErad)
                {
                    if (WNARadFieldUtility.ImmuneToRadiation(pawn)) continue;
                    IntVec3 c = pawn.Position;
                    if (!c.InBounds(map)) continue;
                    int rad = GetRad(c);
                    if (rad > 0)
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
    }
}
