using Verse;

namespace WNA.HediffCompProp
{
    public class CompPostRemoveHediff : HediffCompProperties
    {
        public HediffDef hediff = null;
        public float severity = 1f;
        public CompPostRemoveHediff()
        {
            compClass = typeof(PostRemoveHediff);
        }
    }
    public class PostRemoveHediff : HediffComp
    {
        public CompPostRemoveHediff Props => (CompPostRemoveHediff)props;
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            var pawn = base.Pawn;
            if (!pawn.Dead)
            {
                if (Props.hediff != null)
                {
                    Hediff hediff = HediffMaker.MakeHediff(Props.hediff, pawn);
                    pawn.health.AddHediff(hediff);
                    hediff.Severity = Props.severity;
                }
                else
                {
                    Log.Message($"Attempted to add a null Hediff to {pawn} after removal.");
                    return;
                }
            }
        }
    }
}
