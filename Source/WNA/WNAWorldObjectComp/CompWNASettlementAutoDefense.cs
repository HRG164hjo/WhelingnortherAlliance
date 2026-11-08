using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using WNA.WNAUtility;

namespace WNA.WNAWorldObjectComp
{
    public class PropWNASettlementAutoDefense : WorldObjectCompProperties
    {
        public float rangeTiles = 14;
        public int rearmTicks = 2500;
        public int rearmJitterTicks = 7500;
        public int scanIntervalTicks = 30;
        public PropWNASettlementAutoDefense()
        {
            compClass = typeof(CompWNASettlementAutoDefense);
        }
    }

    public class CompWNASettlementAutoDefense : WorldObjectComp
    {
        private int rearmDoneTick;
        private int scanAccumulator;
        public PropWNASettlementAutoDefense Props => (PropWNASettlementAutoDefense)props;
        private bool Rearming => rearmDoneTick <= 0 || Find.TickManager.TicksGame < rearmDoneTick;
        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (WNAMod.Settings == null || !WNAMod.Settings.settlementAutoDefense)
                return;
            if (parent == null || parent.Destroyed || !parent.Spawned)
                return;
            scanAccumulator += delta;
            int interval = Mathf.Max(1, Props.scanIntervalTicks);
            if (scanAccumulator < interval)
                return;
            scanAccumulator = 0;
            TryScanAndFire();
        }
        private void TryScanAndFire()
        {
            Faction faction = parent.Faction;
            if (faction == null || !General.IsInternalFaction(faction))
                return;
            int now = Find.TickManager.TicksGame;
            if (rearmDoneTick <= 0)
            {
                // A launcher that just came online is empty: it cycles a round before it can engage.
                BeginRearm(now);
                return;
            }
            if (now < rearmDoneTick)
                return;
            WorldObject target = WNASettlementDefenseUtility.FindTarget(parent, Props.rangeTiles);
            if (target == null)
                return;
            if (!WNASettlementDefenseUtility.LaunchMissile(parent, target))
                return;
            BeginRearm(now);
        }
        private void BeginRearm(int now)
        {
            rearmDoneTick = now + Mathf.Max(1, Props.rearmTicks) + Rand.Range(0, Mathf.Max(0, Props.rearmJitterTicks));
        }
        public override string CompInspectStringExtra()
        {
            if (WNAMod.Settings == null || !WNAMod.Settings.settlementAutoDefense)
                return null;
            Faction faction = parent.Faction;
            if (faction == null || !General.IsInternalFaction(faction) || Faction.OfPlayer == null)
                return null;
            Color color = faction.HostileTo(Faction.OfPlayer) ? Color.red : Color.white;
            string key = Rearming ? "WNA_AutoDefense_Rearming" : "WNA_AutoDefense_Ready";
            return key.Translate().Colorize(color);
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref rearmDoneTick, "wnaAutoDefenseRearmDoneTick", 0);
        }
    }
}
