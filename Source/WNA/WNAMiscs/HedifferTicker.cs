using Verse;

namespace WNA.WNAMiscs
{
    public class HedifferTicker : HediffGiver
    {
        public float tickInterval;
        private int lastApplicationTick = -1;
        public override void OnIntervalPassed(Pawn pawn, Hediff cause)
        {
            int currentTick = Find.TickManager.TicksGame;
            if (lastApplicationTick == -1 || currentTick - lastApplicationTick >= tickInterval)
            {
                if (TryApply(pawn))
                {
                    lastApplicationTick = currentTick;
                }
            }
        }
    }
}
