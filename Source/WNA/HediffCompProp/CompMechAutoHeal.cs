using System.Collections.Generic;
using Verse;

namespace WNA.HediffCompProp
{
    public class CompMechAutoHeal : HediffCompProperties
    {
        public int repairInterval = 600;
        public CompMechAutoHeal()
        {
            compClass = typeof(MechAutoHeal);
        }
    }
    public class MechAutoHeal : HediffComp
    {
        public CompMechAutoHeal Props => (CompMechAutoHeal)props;
        private int ticksToNextRepair;
        public int RepairInterval => Props.repairInterval;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            ticksToNextRepair--;

            if (ticksToNextRepair <= 0)
            {
                RepairMech();
                ticksToNextRepair = RepairInterval;
            }
        }
        private void RepairMech()
        {
            Pawn pawn = parent.pawn;
            List<Hediff> hediffsToRemove = new List<Hediff>();
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff is Hediff_Injury || (hediff is Hediff && hediff.Severity > 0 && hediff.def.isBad))
                {
                    hediffsToRemove.Add(hediff);
                }
            }
            foreach (Hediff hediff in hediffsToRemove)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref ticksToNextRepair, "ticksToNextRepair", 0);
        }
    }
}
