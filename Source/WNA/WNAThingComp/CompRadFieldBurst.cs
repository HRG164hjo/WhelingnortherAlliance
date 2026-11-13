using System.Collections.Generic;
using Verse;
using WNA.WNAUtility;

namespace WNA.WNAThingComp
{
    internal class PropRadFieldBurst : CompProperties
    {
        public List<DestroyMode> modeList = null;
        public RadSpreadConfig config = new RadSpreadConfig();
        public PropRadFieldBurst() => compClass = typeof(CompRadFieldBurst);
    }
    internal class CompRadFieldBurst : ThingComp
    {
        private PropRadFieldBurst Props => (PropRadFieldBurst)props;
        public override void PostDestroy(DestroyMode mode, Map map)
        {
            base.PostDestroy(mode, map);
            if (map == null) return;
            if (Props.modeList == null || Props.modeList.Contains(mode))
                WNARadFieldUtility.RadSpread(
                    parent.Position,
                    map,
                    Props.config,
                    Props.config.radius,
                    Props.config.ResolveRadLevel());
        }
    }
}
