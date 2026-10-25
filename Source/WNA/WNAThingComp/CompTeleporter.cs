using Verse;
using WNA.WNAUtility;

namespace WNA.WNAThingComp
{
    internal class PropTeleporter : CompProperties
    {
		public int minDistanceToBlink = 3;
        public int maxDistanceToBlink = 0;
        public int blinkIntervalTicks = 120;
        public int warpStunEffect = -1;
        public bool internalFactionOnly = true;
        public PropTeleporter()
        {
            compClass = typeof(CompTeleporter);
        }
        public TeleporterConfig ToSettings()
        {
            return new TeleporterConfig
            {
                MinDistance = minDistanceToBlink,
                MaxDistance = maxDistanceToBlink,
                CooldownTicks = blinkIntervalTicks,
                WarpStunEffect = warpStunEffect,
                InternalFactionOnly = internalFactionOnly
            };
        }
    }
    internal class CompTeleporter : ThingComp
    {
        private TeleporterConfig config;
        private Pawn pawn;
        private int lastWarp;
        public PropTeleporter Props => (PropTeleporter)props;
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            pawn = parent as Pawn;
            config = Props?.ToSettings();
        }
        public override void CompTick()
        {
            if (pawn == null || config == null)
                return;
            WNATeleporterUtility.TeleportCheck(pawn, config, ref lastWarp);
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastWarp, "lastWarp", 0);
        }
    }
}
