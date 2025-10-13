using Verse;
using WNA.WNAUtility;

namespace WNA.ThingCompProp
{
    public class CompRadFieldAmbient : CompProperties
    {
        public RadSpreadConfig config = new RadSpreadConfig();
        public CompRadFieldAmbient() => compClass = typeof(RadFieldAmbient);
    }
    public class RadFieldAmbient : ThingComp
    {
        private CompRadFieldAmbient Props => (CompRadFieldAmbient)props;
        public override void CompTick()
        {
            base.CompTick();
            if (parent.IsHashIntervalTick(Props.config.radLevelDelay) && parent.Map != null)
            {
                float healthPercent = (float)parent.HitPoints / parent.MaxHitPoints;
                if (healthPercent < Props.config.threshold)
                {
                    RadFieldUtility.RadSpread(
                        parent.Position,
                        parent.Map,
                        Props.config,
                        Props.config.radius,
                        Props.config.radLevel
                    );
                }
            }
        }
    }
}
