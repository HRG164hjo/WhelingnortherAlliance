using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using WNA.WNAUtility;

namespace WNA.WNAWorldComp
{
    public class WorldComp_FactionDefeat : WorldComponent
    {
        private const int AuditIntervalTicks = 250;
        private const int FirstAuditDelayTicks = 120;
        private List<int> handledFactionKeys = new List<int>();
        private int nextAuditTick = -1;
        public WorldComp_FactionDefeat(World world) : base(world) { }
        public bool TryClaim(Faction faction)
        {
            if (handledFactionKeys.Contains(faction.randomKey))
                return false;
            handledFactionKeys.Add(faction.randomKey);
            return true;
        }
        public override void FinalizeInit(bool fromLoad)
        {
            base.FinalizeInit(fromLoad);
            nextAuditTick = Find.TickManager.TicksGame + FirstAuditDelayTicks;
        }
        public override void WorldComponentTick()
        {
            int now = Find.TickManager.TicksGame;
            if (now < nextAuditTick)
                return;
            nextAuditTick = now + AuditIntervalTicks;
            List<Faction> factions = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < factions.Count; i++)
            {
                Faction faction = factions[i];
                if (!WNAFactionDefeatUtility.IsLockedDefeated(faction) || handledFactionKeys.Contains(faction.randomKey))
                    continue;
                WNAFactionDefeatUtility.HandleDefeat(faction, PlanetTile.Invalid, announce: true);
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref handledFactionKeys, "wnaHandledDefeatedFactions", LookMode.Value);
            if (handledFactionKeys == null)
                handledFactionKeys = new List<int>();
        }
    }
}
