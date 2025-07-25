using Verse;

namespace WNA.ThingCompProp
{
    public class CompHeatPusher : CompProperties
    {
        public float heatPushStandard = 0f;
        public float heatPushMult = 0f;
        public CompHeatPusher()
        {
            compClass = typeof(HeatPusher);
        }
    }
    public class HeatPusher : ThingComp
    {
        public bool enabled = true;
        public CompHeatPusher Props => (CompHeatPusher)props;
        public virtual bool ShouldBeActive
        {
            get
            {
                if (!parent.SpawnedOrAnyParentSpawned)
                {
                    return false;
                }
                CompHeatPusher compHeatPusher = Props;
                float ambientTemperature = parent.AmbientTemperature;
                if (enabled && ambientTemperature != compHeatPusher.heatPushStandard)
                {
                    return true;
                }
                return false;
            }
        }
        public override void CompTick()
        {
            base.CompTick();
            if (parent.IsHashIntervalTick(250) && ShouldBeActive)
            {
                float heatGap = Props.heatPushStandard - parent.AmbientTemperature;
                GenTemperature.PushHeat(parent.PositionHeld, parent.MapHeld, heatGap * Props.heatPushMult);
            }
        }
        public override void CompTickRare()
        {
            base.CompTickRare();
            if (ShouldBeActive)
            {
                float heatGap = Props.heatPushStandard - parent.AmbientTemperature;
                GenTemperature.PushHeat(parent.PositionHeld, parent.MapHeld, heatGap * Props.heatPushMult);
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref enabled, "enabled", defaultValue: true);
        }
    }
}
