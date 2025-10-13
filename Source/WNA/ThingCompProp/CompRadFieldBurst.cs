using Verse;
using WNA.WNAUtility;

namespace WNA.ThingCompProp
{
    public class CompRadFieldBurst : CompProperties
    {
        public RadSpreadConfig config = new RadSpreadConfig();
        public CompRadFieldBurst() => compClass = typeof(RadFieldBurst);
    }
    public class RadFieldBurst : ThingComp
    {
        private CompRadFieldBurst Props => (CompRadFieldBurst)props;
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (previousMap != null)
            {
                RadFieldUtility.RadSpread(
                    parent.Position,
                    previousMap,
                    Props.config,
                    Props.config.radius,
                    Props.config.radLevel);
            }
        }
    }
}
