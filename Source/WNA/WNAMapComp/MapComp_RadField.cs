using System.Collections.Generic;
using UnityEngine;
using Verse;
using WNA.WNADefOf;
using WNA.WNAUtility;

namespace WNA.WNAMapComp
{
    internal sealed class RadFieldLayer : IExposable
    {
        public WNA_RadFieldDef def;
        public List<int> levels;

        public RadFieldLayer() { }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            Scribe_Collections.Look(ref levels, "levels", LookMode.Value);
        }
    }

    internal class MapComp_RadField : MapComponent
    {
        private List<RadFieldLayer> layers = new List<RadFieldLayer>();
        private Dictionary<WNA_RadFieldDef, RadFieldLayer> lookup;
        private readonly List<Pawn> tmpPawns = new List<Pawn>();

        public MapComp_RadField(Map map) : base(map) { }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            ReindexLayers();
            ValidateLayers();
        }

        private void EnsureLookup()
        {
            if (layers == null)
                layers = new List<RadFieldLayer>();
            if (lookup != null) return;
            lookup = new Dictionary<WNA_RadFieldDef, RadFieldLayer>();
            for (int i = 0; i < layers.Count; i++)
            {
                RadFieldLayer layer = layers[i];
                if (layer?.def != null)
                    lookup[layer.def] = layer;
            }
        }

        private void ReindexLayers()
        {
            lookup = null;
            if (layers == null)
            {
                layers = new List<RadFieldLayer>();
            }
            else
            {
                for (int i = layers.Count - 1; i >= 0; i--)
                {
                    if (layers[i] == null || layers[i].def == null)
                        layers.RemoveAt(i);
                }
            }
            EnsureLookup();
        }

        private void ValidateLayers()
        {
            if (layers == null)
            {
                layers = new List<RadFieldLayer>();
                EnsureLookup();
                return;
            }
            int count = map.cellIndices.NumGridCells;
            for (int i = 0; i < layers.Count; i++)
            {
                RadFieldLayer layer = layers[i];
                if (layer?.def == null) continue;
                if (layer.levels == null || layer.levels.Count != count)
                    layer.levels = NewGrid();
            }
        }

        private List<int> NewGrid()
        {
            int count = map.cellIndices.NumGridCells;
            List<int> grid = new List<int>(count);
            for (int i = 0; i < count; i++)
                grid.Add(0);
            return grid;
        }

        private RadFieldLayer GetLayer(WNA_RadFieldDef def)
        {
            if (def == null) return null;
            EnsureLookup();
            if (lookup.TryGetValue(def, out RadFieldLayer layer))
                return layer;
            layer = new RadFieldLayer { def = def, levels = NewGrid() };
            layers.Add(layer);
            lookup[def] = layer;
            return layer;
        }

        public int GetRad(IntVec3 c, WNA_RadFieldDef def)
        {
            if (def == null || !c.InBounds(map)) return 0;
            EnsureLookup();
            if (!lookup.TryGetValue(def, out RadFieldLayer layer) || layer.levels == null)
                return 0;
            return layer.levels[map.cellIndices.CellToIndex(c)];
        }

        public void AddRad(IntVec3 c, WNA_RadFieldDef def, int amount)
        {
            if (def == null || !c.InBounds(map)) return;
            RadFieldLayer layer = GetLayer(def);
            if (layer == null) return;
            int index = map.cellIndices.CellToIndex(c);
            int next = Mathf.Clamp(layer.levels[index] + amount, 0, Mathf.Max(0, def.intensityMax));
            if (next == layer.levels[index]) return;
            layer.levels[index] = next;
            MarkRadDirty(c);
        }

        public void SetRad(IntVec3 c, WNA_RadFieldDef def, int value)
        {
            if (def == null || !c.InBounds(map)) return;
            RadFieldLayer layer = GetLayer(def);
            if (layer == null) return;
            int index = map.cellIndices.CellToIndex(c);
            int next = Mathf.Clamp(value, 0, Mathf.Max(0, def.intensityMax));
            if (next == layer.levels[index]) return;
            layer.levels[index] = next;
            MarkRadDirty(c);
        }

        public int ClearAllRad(IntVec3 c, out WNA_RadFieldDef dominant)
        {
            dominant = null;
            if (!c.InBounds(map)) return 0;
            EnsureLookup();
            int index = map.cellIndices.CellToIndex(c);
            bool dirty = false;
            int dominantValue = 0;
            for (int i = 0; i < layers.Count; i++)
            {
                RadFieldLayer layer = layers[i];
                if (layer?.def == null || !layer.def.clearable || layer.levels == null) continue;
                int value = layer.levels[index];
                if (value <= 0) continue;
                if (value > dominantValue)
                {
                    dominantValue = value;
                    dominant = layer.def;
                }
                layer.levels[index] = 0;
                dirty = true;
            }
            if (dirty)
                MarkRadDirty(c);
            return dominantValue;
        }

        public bool AnyRadAt(IntVec3 c)
        {
            if (!c.InBounds(map)) return false;
            EnsureLookup();
            int index = map.cellIndices.CellToIndex(c);
            for (int i = 0; i < layers.Count; i++)
            {
                RadFieldLayer layer = layers[i];
                if (layer?.def == null || !layer.def.clearable || layer.levels == null) continue;
                if (layer.levels[index] > 0) return true;
            }
            return false;
        }

        private void MarkRadDirty(IntVec3 c)
        {
            map.mapDrawer.MapMeshDirty(c, WNAMainDefOf.WNA_RadField, regenAdjacentCells: true, regenAdjacentSections: false);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (layers == null || layers.Count == 0) return;
            int layerCount = layers.Count;
            for (int l = 0; l < layerCount; l++)
            {
                RadFieldLayer layer = layers[l];
                WNA_RadFieldDef def = layer?.def;
                if (def == null || layer.levels == null) continue;

                if (def.decayIntervalTicks > 0 && map.IsHashIntervalTick(def.decayIntervalTicks))
                {
                    int decay = Mathf.Max(0, def.decayPerInterval);
                    for (int i = 0; i < layer.levels.Count; i++)
                    {
                        int cur = layer.levels[i];
                        if (cur <= 0) continue;
                        int next = Mathf.Max(0, cur - decay);
                        layer.levels[i] = next;
                        if (next == 0 || next % 5 == 0)
                            MarkRadDirty(map.cellIndices.IndexToCell(i));
                    }
                }

                if (def.damageDef == null || def.damageIntervalTicks <= 0 || def.damagePerIntensity <= 0f)
                    continue;
                if (!map.IsHashIntervalTick(def.damageIntervalTicks))
                    continue;

                tmpPawns.Clear();
                tmpPawns.AddRange(map.mapPawns.AllPawnsSpawned);
                for (int i = 0; i < tmpPawns.Count; i++)
                {
                    Pawn pawn = tmpPawns[i];
                    if (pawn.Dead || !pawn.Spawned) continue;
                    if (WNARadFieldUtility.ImmuneToRadiation(pawn)) continue;
                    IntVec3 c = pawn.Position;
                    if (!c.InBounds(map)) continue;
                    int rad = GetRad(c, def);
                    if (rad <= 0) continue;
                    float dmg = rad * def.damagePerIntensity;
                    if (dmg <= 0f) continue;
                    pawn.TakeDamage(new DamageInfo(def.damageDef, dmg, float.MaxValue, -1f, null, null, null));
                }
                tmpPawns.Clear();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref layers, "radFieldLayers", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                ReindexLayers();
        }
    }
}
