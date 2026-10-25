using RimWorld;
using Verse;
using WNA.WNAUtility;

namespace WNA.WNAMapComp
{
    internal class MapComp_PsychicDawn : MapComponent
    {
        private const int IntervalTicks = 250;

        public MapComp_PsychicDawn(Map map) : base(map) { }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            if (!WNAPsychicDawnUtility.Active)
                return;
            WNAPsychicDawnUtility.ApplyMapEffects(map);
            HealMapPawnGear();
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (!WNAPsychicDawnUtility.Active || !map.IsHashIntervalTick(IntervalTicks))
                return;
            WNAPsychicDawnUtility.ApplyMapEffects(map);
        }

        private void HealMapPawnGear()
        {
            var pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn.Faction == Faction.OfPlayer)
                    WNAPsychicDawnUtility.HealPawnGear(pawn);
            }
        }
    }
}
