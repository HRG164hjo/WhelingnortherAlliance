using Verse;

namespace WNA.WNAHediffCompProp
{
    public class PropPostRemoveHediff : HediffCompProperties
    {
        public HediffDef hediff = null;
        public float severity = 1f;
        public PropPostRemoveHediff()
        {
            compClass = typeof(CompPostRemoveHediff);
        }
    }
    public class CompPostRemoveHediff : HediffComp
    {
        public PropPostRemoveHediff Props => (PropPostRemoveHediff)props;
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (!Pawn.Dead)
            {
                if (Props.hediff != null)
                {
                    Hediff hediff = HediffMaker.MakeHediff(Props.hediff, Pawn);
                    Pawn.health.AddHediff(hediff);
                    hediff.Severity = Props.severity;
                }
                else
                {
                    Log.Message($"Attempted to add a null Hediff to {Pawn} after removal.");
                    return;
                }
            }
        }
    }
}
