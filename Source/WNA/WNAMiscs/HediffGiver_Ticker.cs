using Verse;

namespace WNA.WNAMiscs
{
    internal class HediffGiver_Ticker : HediffGiver
    {
        public float tickInterval = 60;
        private int lastApplicationTick = -1;
        public override void OnIntervalPassed(Pawn pawn, Hediff cause)
        {
            int currentTick = Find.TickManager.TicksGame;
            if (lastApplicationTick == -1 || currentTick - lastApplicationTick >= tickInterval)
            {
                if (TryApply(pawn))
                    lastApplicationTick = currentTick;
            }
        }
    }
}
