using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace WNA.WNAUtility
{
    public class LysisFieldData : IExposable
    {
        public int level;
        public int duration;
        public LysisFieldData(int level, int duration)
        {
            this.level = level;
            this.duration = duration;
        }
        public void ExposeData()
        {
            Scribe_Values.Look(ref level, "level", 0);
            Scribe_Values.Look(ref duration, "duration", 0);
        }
    }
    public class LysField_GameComp : GameComponent
    {
        private Dictionary<Thing, LysisFieldData> active = new Dictionary<Thing, LysisFieldData>();
        private const int interval = 15;
        public LysField_GameComp(Game game) { }
        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % interval != 0)
                return;
            List<Thing> toRemove = new List<Thing>();
            foreach (var kv in active.ToList())
            {
                Thing thing = kv.Key;
                LysisFieldData data = kv.Value;
                if (thing.Destroyed || !thing.Spawned)
                {
                    toRemove.Add(thing);
                    continue;
                }
                data.duration -= interval;
                if (data.duration <= 0)
                {
                    data.level--;
                    data.duration = 90;
                }
                if (data.level <= 0)
                    toRemove.Add(thing);
            }
            foreach (var t in toRemove)
                active.Remove(t);
        }
        public void AddOrUpdateField(Thing thing, int addLevel, int addDuration)
        {
            if (thing == null || thing.Destroyed) return;
            if (!thing.def.isSaveable) return;
            if (!active.TryGetValue(thing, out var data))
            {
                data = new LysisFieldData(addLevel, addDuration);
                active[thing] = data;
            }
            else
            {
                data.level = Math.Min(data.level + addLevel, 31);
                data.duration = Math.Max(data.duration, addDuration);
            }
        }
        public int GetLevel(Thing thing)
        {
            if (thing == null) return 0;
            if (active.TryGetValue(thing, out var data))
                return data.level;
            return 0;
        }
        public void Remove(Thing thing)
        {
            if (thing == null) return;
            active.Remove(thing);
        }
        public void Spread(Map map, IntVec3 center, int sourceLevel)
        {
            if (map == null || sourceLevel <= 0) return;
            int newLevel = (int)Math.Ceiling(sourceLevel * 0.5f);
            float radius = 5f;
            foreach (var cell in GenRadial.RadialCellsAround(center, radius, true))
            {
                if (!cell.InBounds(map)) continue;
                foreach (var t in cell.GetThingList(map))
                {
                    if (t == null || t.Destroyed) continue;
                    if (!t.def.destroyable) continue;
                    AddOrUpdateField(t, newLevel, 90);
                    int lvl = GetLevel(t);
                    if (lvl >= 31 && !t.Destroyed)
                        t.Destroy(DestroyMode.KillFinalize);
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            List<Thing> keys = null;
            List<LysisFieldData> values = null;
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                if (active != null && active.Count > 0)
                {
                    keys = active.Keys.ToList();
                    values = active.Values.ToList();
                }
                else
                {
                    keys = new List<Thing>();
                    values = new List<LysisFieldData>();
                }
            }
            Scribe_Collections.Look(ref active, "activeLysis", LookMode.Reference, LookMode.Deep, ref keys, ref values);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (active == null)
                    active = new Dictionary<Thing, LysisFieldData>();
            }
        }
        public static LysField_GameComp Instance =>
            Current.Game?.GetComponent<LysField_GameComp>();
    }
}
