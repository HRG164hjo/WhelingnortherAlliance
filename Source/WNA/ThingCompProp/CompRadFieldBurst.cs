using System.Collections.Generic;
using Verse;
using WNA.WNAUtility;

namespace WNA.ThingCompProp
{
    public class PropRadFieldBurst : CompProperties
    {
        public List<DestroyMode> modeList = null;
        public RadSpreadConfig config = new RadSpreadConfig();
        public PropRadFieldBurst() => compClass = typeof(CompRadFieldBurst);
    }
    public class CompRadFieldBurst : ThingComp
    {
        private PropRadFieldBurst Props => (PropRadFieldBurst)props;
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (previousMap == null) return;
            if (Props.modeList == null || Props.modeList.Contains(mode))
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
