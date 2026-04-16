using System.Collections.Generic;
using Verse;

namespace WNA.WNAHediffCompProp
{
    public class PropHediffHediffRemover : HediffCompProperties
    {
        public int interval = 250;
        public bool reverseEffect = false;
        public bool removeInjury = false;
        public PropHediffHediffRemover()
        {
            compClass = typeof(CompHediffHediffRemover);
        }
    }
    public class CompHediffHediffRemover : HediffComp
    {
        public PropHediffHediffRemover Props => (PropHediffHediffRemover)props;
        private int ticksUntilRemove;
        public override void CompPostMake()
        {
            base.CompPostMake();
            ticksUntilRemove = Props.interval;
        }
        private bool ShouldRemoveHediff(Hediff hediff)
        {
            if (hediff is Hediff_Injury || hediff is Hediff_MissingPart) return Props.removeInjury;
            if (Props.reverseEffect)
                return !(hediff.def.isBad);
            else
                return hediff.def.isBad;
        }
        private void RemoveHediffs(Pawn pawn)
        {
            if (pawn?.health?.hediffSet?.hediffs == null) return;
            var hediffs = new List<Hediff>(pawn.health.hediffSet.hediffs);
            foreach (var hediff in hediffs)
            {
                if (hediff == parent) continue;
                if (ShouldRemoveHediff(hediff))
                    pawn.health.RemoveHediff(hediff);
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            ticksUntilRemove--;
            if (ticksUntilRemove <= 0)
            {
                RemoveHediffs(Pawn);
                ticksUntilRemove = Props.interval;
            }
        }
    }
}
