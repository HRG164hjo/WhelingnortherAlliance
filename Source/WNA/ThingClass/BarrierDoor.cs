using RimWorld;
using UnityEngine;
using Verse;
using WNA.WNADefOf;

namespace WNA.ThingClass
{
    public class BarrierDoor : Building_SupportedDoor
    {
        private bool lastFreePassage;
        protected override bool CanDrawMovers => false;
        public override bool ExchangeVacuum => false;
        public override bool FreePassage
        {
            get
            {
                if (Faction == null) return false;
                Pawn p = TraverseParms.For(TraverseMode.PassDoors).pawn;
                if (p != null && (p.Faction.def == WNAMainDefOf.WNA_FactionWNA || p.Faction == Faction.OfPlayer))
                    return true;
                return false;
            }
        }
        protected override bool AlwaysOpen => true;
        protected override float TempEqualizeRate => 0f;
        protected override void Tick()
        {
            base.Tick();
            if (FreePassage != lastFreePassage)
            {
                ClearReachabilityCache(Map);
                lastFreePassage = FreePassage;
            }
            if (this.IsHashIntervalTick(def.building.doorTempEqualizeIntervalClosed))
                GenTemperature.EqualizeTemperaturesThroughBuilding(this, TempEqualizeRate, twoWay: false);
        }
        public override bool PawnCanOpen(Pawn p)
        {
            if (!p.CanOpenDoors) return false;
            if ((p.CanOpenAnyDoor && p.Faction.def == WNAMainDefOf.WNA_FactionWNA && !p.IsPrisonerOfColony)
                || p.Faction == Faction.OfPlayer)
            {
                if (p.InMentalState) return false;
                return true;
            }
            return false;
        }
        public override bool BlocksPawn(Pawn p)
        {
            return !PawnCanOpen(p);
        }
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Graphic.Draw(drawLoc, Rotation, this);
            if (def.building?.doorTopGraphic != null)
            {
                Graphic topGraphic = def.building.doorTopGraphic.Graphic;
                if (topGraphic != null)
                {
                    Vector3 topPos = drawLoc;
                    topPos.y += 0.03f;
                    topGraphic.Draw(topPos, Rotation, this);
                }
            }
            Comps_PostDraw();
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            ClearReachabilityCache(map);
        }
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Map map = Map;
            base.DeSpawn(mode);
            ClearReachabilityCache(map);
        }
        public override void SetFaction(Faction newFaction, Pawn recruiter = null)
        {
            base.SetFaction(newFaction, recruiter);
            if (Spawned) ClearReachabilityCache(Map);
        }
        private void ClearReachabilityCache(Map map)
        {
            map.reachability.ClearCache();
            lastFreePassage = FreePassage;
        }
    }
}
