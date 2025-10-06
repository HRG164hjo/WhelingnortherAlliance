using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace WNA.ThingClass
{
    public class Barrier : Building_Door
    {
        private bool freePassageWhenClearedReachabilityCache;
        protected override bool CheckFaction => true;
        public override bool ExchangeVacuum => false;
        protected override float TempEqualizeRate => 0f;
        public override bool FreePassage => false;
        protected override bool AlwaysOpen => true;
        public override bool FireBulwark => true;
        public override void PostMake()
        {
            base.PostMake();
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            ClearReachabilityCache(map);
            CheckClearReachabilityCacheBecauseOpenedOrClosed();
        }
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Map map = base.Map;
            base.DeSpawn(mode);
            ClearReachabilityCache(map);
        }
        public override void ExposeData()
        {
            base.ExposeData();
        }
        public override void SetFaction(Faction newFaction, Pawn recruiter = null)
        {
            base.SetFaction(newFaction, recruiter);
            if (base.Spawned) ClearReachabilityCache(base.Map);
        }
        protected override void Tick()
        {
            base.Tick();
            if (FreePassage != freePassageWhenClearedReachabilityCache)
                ClearReachabilityCache(base.Map);
            foreach (IntVec3 cell in this.OccupiedRect())
            {
                List<Thing> thingList = cell.GetThingList(base.Map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    Thing thing = thingList[i];
                    if (thing is Blueprint || thing is Frame || thing is Pawn || thing is Building)
                        continue;
                    if (thing.def.category == ThingCategory.Item || thing is Corpse)
                        GenPlace.TryPlaceThing(thing, cell, base.Map, ThingPlaceMode.Near);
                }
            }
            if (this.IsHashIntervalTick(def.building.doorTempEqualizeIntervalClosed))
                GenTemperature.EqualizeTemperaturesThroughBuilding(this, TempEqualizeRate, twoWay: false);
        }
        public new bool CanPhysicallyPass(Pawn p)
        {
            return PawnCanOpen(p);
        }
        public override bool PawnCanOpen(Pawn p)
        {
            if (p.CanOpenAnyDoor) return true;
            if (!p.CanOpenDoors) return false;
            if (p.HostileTo(this) ||
                p.InMentalState ||
                p.IsWildMan() ||
                (p.RaceProps.Animal && p.Faction == null)) return false;
            if (base.Faction == null) return p.RaceProps.canOpenFactionlessDoors;
            return !p.HostileTo(this);
        }
        public override bool BlocksPawn(Pawn p)
        {
            return !PawnCanOpen(p);
        }
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Graphic.Draw(drawLoc, Rotation, this);
            Comps_PostDraw();
        }
        private void ClearReachabilityCache(Map map)
        {
            map.reachability.ClearCache();
            freePassageWhenClearedReachabilityCache = FreePassage;
        }
        private void CheckClearReachabilityCacheBecauseOpenedOrClosed()
        {
            if (base.Spawned) base.Map.reachability.ClearCacheForHostile(this);
        }
        public override string GetInspectString()
        {
            return base.GetInspectString();
        }
    }
}
