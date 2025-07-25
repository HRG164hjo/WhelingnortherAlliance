using RimWorld;
using System.Collections.Generic;
using Verse;

namespace WNA.ThingCompProp
{
    public class CompApparelHediffRemover : CompProperties
    {
        public int interval = 250;
        public bool reverseEffect = false;
        public bool removeInjury = false;
        public CompApparelHediffRemover()
        {
            this.compClass = typeof(ApparelHediffRemover);
        }
    }
    public class ApparelHediffRemover : ThingComp
    {
        private static readonly HashSet<string> listedHediffs = new HashSet<string>
            {
                "WNA_Corrosion",
                "CubeInterest"
            };

        private int ticksUntilRemove;
        public CompApparelHediffRemover Props => (CompApparelHediffRemover)props;
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            ticksUntilRemove = Props.interval;
        }
        private bool ShouldRemoveHediff(Hediff hediff)
        {
            if (hediff is Hediff_Injury) return Props.removeInjury;
            bool isListed = listedHediffs.Contains(hediff.def.defName);
            if (Props.reverseEffect) return !(hediff.def.isBad || isListed);
            else return hediff.def.isBad || isListed;
        }
        private void RemoveHediffs(Pawn pawn)
        {
            if (pawn.health?.hediffSet?.hediffs == null) return;
            var hediffs = new List<Hediff>(pawn.health.hediffSet.hediffs);
            foreach (var hediff in hediffs)
            {
                if (ShouldRemoveHediff(hediff))
                {
                    pawn.health.RemoveHediff(hediff);
                }
            }
        }
        public override void CompTick()
        {
            base.CompTick();
            if (parent is Apparel apparel && apparel.Wearer != null)
            {
                ticksUntilRemove--;
                if (ticksUntilRemove <= 0)
                {
                    RemoveHediffs(apparel.Wearer);
                    ticksUntilRemove = Props.interval;
                }
            }
        }
    }
}
