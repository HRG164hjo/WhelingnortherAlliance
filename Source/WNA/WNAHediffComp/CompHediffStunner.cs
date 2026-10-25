using Verse;

namespace WNA.WNAHediffComp
{
    internal class PropHediffStunner : HediffCompProperties
    {
        public PropHediffStunner()
        {
            compClass = typeof(CompHediffStunner);
        }
    }
    internal class CompHediffStunner : HediffComp
    {
        public PropHediffStunner Props => (PropHediffStunner)props;
        private int ticker = -1;
        public override void CompPostTick(ref float severityAdjustment)
        {
            ticker--;
            if (ticker <= 0)
            {
                Pawn.stances?.stagger?.StaggerFor(13, 0);
                Pawn.stances?.stunner?.StunFor(13, null, false, false, true);
                ticker = 11;
            }
        }
        public override void CompExposeData()
        {
            Scribe_Values.Look(ref ticker, "ticker", 0);
        }
    }
}
