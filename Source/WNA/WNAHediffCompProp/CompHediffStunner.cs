using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace WNA.WNAHediffCompProp
{
    public class PropHediffStunner : HediffCompProperties
    {
        public PropHediffStunner()
        {
            compClass = typeof(CompHediffStunner);
        }
    }
    public class CompHediffStunner : HediffComp
    {
        public PropHediffStunner Props => (PropHediffStunner)props;
        private int ticker = -1;
        public override void CompPostTick(ref float severityAdjustment)
        {
            ticker--;
            if (ticker <= 0)
            {
                Pawn.stances?.stagger?.StaggerFor(25, 0);
                Pawn.stances?.stunner?.StunFor(25, null, false, false, true);
                ticker = 23;
            }
        }
        public override void CompExposeData()
        {
            Scribe_Values.Look(ref ticker, "ticker", 0);
        }
    }
}
