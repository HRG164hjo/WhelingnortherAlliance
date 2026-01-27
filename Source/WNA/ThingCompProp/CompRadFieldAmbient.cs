using Verse;
using WNA.WNAUtility;

namespace WNA.ThingCompProp
{
    public class PropRadFieldAmbient : CompProperties
    {
        public RadSpreadConfig config = new RadSpreadConfig();
        public PropRadFieldAmbient() => compClass = typeof(CompRadFieldAmbient);
    }
    public class CompRadFieldAmbient : ThingComp
    {
        private PropRadFieldAmbient Props => (PropRadFieldAmbient)props;
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
