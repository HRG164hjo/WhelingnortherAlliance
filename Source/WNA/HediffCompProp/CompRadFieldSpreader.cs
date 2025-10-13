using Verse;
using WNA.ThingCompProp;
using WNA.WNAUtility;

namespace WNA.HediffCompProp
{
    public class CompRadFieldSpreader : HediffCompProperties
    {
        public RadSpreadConfig config = new RadSpreadConfig();
        public CompRadFieldSpreader() => compClass = typeof(RadFieldSpreader);
    }
    public class RadFieldSpreader : HediffComp
    {
        private CompRadFieldSpreader Props => (CompRadFieldSpreader)props;
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            Pawn pawn = base.Pawn;
            if (pawn.IsHashIntervalTick(Props.config.radLevelDelay) && pawn.Map != null)
            {
                float s = parent.Severity;
                if (s >= Props.config.threshold)
                {
                    float finalRadLevel = Props.config.radLevel;
                    float finalRadius = Props.config.radius;
                    if(Props.config.isSeverityRadLevel)
                        finalRadLevel *= s * Props.config.radLevelFactor;
                    if (Props.config.isSeverityRadRadius)
                        finalRadius *= s * Props.config.radRadiusFactor;
                    RadFieldUtility.RadSpread(
                        pawn.Position,
                        pawn.Map,
                        Props.config,
                        finalRadius,
                        (int)finalRadLevel
                    );
                }
            }
        }
    }
}
