using Verse;

namespace WNA.WNAMiscs
{
    public class HedifferTicker : HediffGiver
    {
        public float tickInterval;
        public override void OnIntervalPassed(Pawn pawn, Hediff cause)
        {
            int currentTick = Find.TickManager.TicksGame;
            if (currentTick % tickInterval == 0)
                TryApply(pawn);
        }
    }
}
