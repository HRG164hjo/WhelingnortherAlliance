using System.Collections.Generic;
using Verse;

namespace WNA.HediffCompProp
{
    public class CompHediffHediffRemover : HediffCompProperties
    {
        public int interval = 250;
        public bool reverseEffect = false;
        public bool removeInjury = false;
        public CompHediffHediffRemover()
        {
            compClass = typeof(HediffHediffRemover);
        }
    }
    public class HediffHediffRemover : HediffComp
    {
        public CompHediffHediffRemover Props => (CompHediffHediffRemover)props;
        private static readonly HashSet<string> listedHediffs = new HashSet<string>
        {
            "WNA_Corrosion",
            "WNA_InMechanoid"
        };
        private int ticksUntilRemove;
        public override void CompPostMake()
        {
            base.CompPostMake();
            ticksUntilRemove = Props.interval;
        }
        private bool ShouldRemoveHediff(Hediff hediff)
        {
            if (hediff is Hediff_Injury) return Props.removeInjury;

            bool isListed = listedHediffs.Contains(hediff.def.defName);
            if (Props.reverseEffect)
                return !(hediff.def.isBad || isListed);
            else
                return hediff.def.isBad || isListed;
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
