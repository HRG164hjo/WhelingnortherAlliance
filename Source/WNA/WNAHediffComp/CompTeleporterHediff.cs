using Verse;
using WNA.WNAUtility;

namespace WNA.WNAHediffComp
{
    internal class PropTeleporterHediff : HediffCompProperties
    {
        public int minDistanceToBlink = 3;
        public int maxDistanceToBlink = 0;
        public int blinkIntervalTicks = 120;
        public int warpStunEffect = -1;
        public bool internalFactionOnly = true;

        public PropTeleporterHediff()
        {
            compClass = typeof(CompTeleporterHediff);
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
    internal class CompTeleporterHediff : HediffComp
    {
        private TeleporterConfig config;
        private bool settingsReady;
        private int lastBlinkTick;
        public PropTeleporterHediff Props => (PropTeleporterHediff)props;
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            Pawn pawn = parent?.pawn;
            if (pawn == null)
                return;
            if (!settingsReady)
            {
                config = Props?.ToSettings();
                settingsReady = true;
            }
            if (config == null)
                return;
            WNATeleporterUtility.TeleportCheck(pawn, config, ref lastBlinkTick);
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref lastBlinkTick, "lastBlinkTick", 0);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                settingsReady = false;
        }
    }
}
