using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using WNA.DMExtension;

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
        private Dictionary<Thing, LysisFieldData> activeLysis = new Dictionary<Thing, LysisFieldData>();
        private const int interval = 15;
        private List<Thing> activeLysKeysWorkingList;
        private List<LysisFieldData> activeLysValuesWorkingList;
        public LysField_GameComp(Game game) { }
        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % interval != 0)
                return;
            List<Thing> toRemove = new List<Thing>();
            foreach (var kv in activeLysis.ToList())
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
                activeLysis.Remove(t);
        }
        public void AddOrUpdateField(Thing thing, int addLevel, int addDuration)
        {
            if (thing == null || thing.Destroyed) return;
            if (!thing.def.isSaveable) return;
            TechnoConfig cfg = TechnoConfig.Get(thing.def);
            if (cfg != null && cfg.immuneToRadiation == true) return;
            if (!activeLysis.TryGetValue(thing, out var data))
            {
                data = new LysisFieldData(addLevel, addDuration);
                activeLysis[thing] = data;
            }
            else
            {
                data.level = Math.Min(data.level + addLevel, 31);
                data.duration = Math.Max(data.duration, addDuration);
            }
            int lvl = GetLevel(thing);
            if (lvl >= 31 && !thing.Destroyed)
                thing.Kill();
        }
        public int GetLevel(Thing thing)
        {
            if (thing == null) return 0;
            if (activeLysis.TryGetValue(thing, out var data))
                return data.level;
            return 0;
        }
        public void Remove(Thing thing)
        {
            if (thing == null) return;
            activeLysis.Remove(thing);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref activeLysis,
                "activeLysis",
                LookMode.Reference,
                LookMode.Deep,
                ref activeLysKeysWorkingList,
                ref activeLysValuesWorkingList);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (activeLysis == null)
                    activeLysis = new Dictionary<Thing, LysisFieldData>();
                var toRemove = new List<Thing>();
                foreach (var kv in activeLysis)
                {
                    if (kv.Key == null || kv.Value == null) toRemove.Add(kv.Key);
                }
                for (int i = 0; i < toRemove.Count; i++)
                    activeLysis.Remove(toRemove[i]);
            }
        }
        public static LysField_GameComp Instance =>
            Current.Game?.GetComponent<LysField_GameComp>();
    }
}
