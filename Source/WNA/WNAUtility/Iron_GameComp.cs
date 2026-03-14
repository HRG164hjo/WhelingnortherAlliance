using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using WNA.DMExtension;

namespace WNA.WNAUtility
{
    public class IronData : IExposable
    {
        public int duration = 2400;
        public IronData() { }
        public void ExposeData()
        {
            Scribe_Values.Look(ref duration, "duration", 2400);
        }
    }
    public class Iron_GameComp : GameComponent
    {
        private Dictionary<Thing, IronData> activeIron = new Dictionary<Thing, IronData>();
        private const int interval = 60;
        private List<Thing> activeIronKeysWorkingList;
        private List<IronData> activeIronValuesWorkingList;
        public Iron_GameComp(Game game) : base() { }
        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % interval != 0) return;
            if (activeIron.Count == 0) return;
            var toRemove = new List<Thing>();
            foreach (var kv in activeIron.ToList())
            {
                var thing = kv.Key;
                var data = kv.Value;
                if (thing == null || thing.Destroyed)
                {
                    toRemove.Add(thing);
                    continue;
                }
                data.duration -= interval;
                if (data.duration <= 0)
                    toRemove.Add(thing);
            }
            foreach (var t in toRemove)
                activeIron.Remove(t);
        }
        public void IronGive(Thing t, int durationTicks)
        {
            if (t == null || t.Destroyed) return;

            if (activeIron.TryGetValue(t, out var d))
                d.duration = Math.Max(d.duration, durationTicks);
            else
                activeIron[t] = new IronData();
        }
        public void IronRemove(Thing t)
        {
            if (t == null) return;
            activeIron.Remove(t);
        }
        public bool IsIroned(Thing t)
        {
            if (t == null) return false;
            return activeIron.TryGetValue(t, out var d) && d.duration > 0 && !t.Destroyed;
        }
        public void IronKill(Map map, IntVec3 center, float radius)
        {
            if (map == null) return;
            foreach (var cell in GenRadial.RadialCellsAround(center, radius, true))
            {
                if (!cell.InBounds(map)) continue;
                foreach (var thing in cell.GetThingList(map).ToList())
                {
                    if (thing == null) continue;
                    if (!thing.def.destroyable) continue;
                    var ext = TechnoConfig.Get(thing.def);
                    if (ext != null && ext.immuneToIronKill == true) continue;
                    if (IsIroned(thing)) continue;
                    if (thing is Pawn pawn)
                    {
                        if (!pawn.Dead || !pawn.Destroyed)
                            pawn.Destroy(DestroyMode.KillFinalize);
                    }
                    else if (!thing.Destroyed)
                        thing.Destroy(DestroyMode.KillFinalize);
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(
                ref activeIron,
                "activeIron",
                LookMode.Reference,
                LookMode.Deep,
                ref activeIronKeysWorkingList,
                ref activeIronValuesWorkingList
            );
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (activeIron == null)
                    activeIron = new Dictionary<Thing, IronData>();
                var toRemove = new List<Thing>();
                foreach (var kv in activeIron)
                {
                    if (kv.Key == null || kv.Value == null) toRemove.Add(kv.Key);
                }
                for (int i = 0; i < toRemove.Count; i++)
                    activeIron.Remove(toRemove[i]);
            }
        }
        public static Iron_GameComp Instance => Current.Game?.GetComponent<Iron_GameComp>();
    }
}
