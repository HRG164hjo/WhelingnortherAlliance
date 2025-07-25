using RimWorld;
using Verse;

namespace WNA.ThingCompProp
{
    public class CompApparelEquipHediff : CompProperties
    {
        public HediffDef hediff;
        public int checkInterval = 250;

        public CompApparelEquipHediff()
        {
            this.compClass = typeof(ApparelEquipHediff);
        }
    }
    public class ApparelEquipHediff : ThingComp
    {
        private int ticksUntilCheck = 0;
        public CompApparelEquipHediff Props => (CompApparelEquipHediff)props;
        public override void Notify_Equipped(Pawn pawn)
        {
            AddHediffIfMissing(pawn);
            ticksUntilCheck = Props.checkInterval;
        }
        public override void CompTick()
        {
            base.CompTick();
            if (parent is Apparel apparel && apparel.Wearer != null)
            {
                ticksUntilCheck--;
                if (ticksUntilCheck <= 0)
                {
                    AddHediffIfMissing(apparel.Wearer);
                    ticksUntilCheck = Props.checkInterval;
                }
            }
        }
        private void AddHediffIfMissing(Pawn pawn)
        {
            if (pawn.health.hediffSet.GetFirstHediffOfDef(Props.hediff) == null)
            {
                HediffComp_RemoveIfApparelDropped hediffComp = pawn.health.AddHediff(Props.hediff).TryGetComp<HediffComp_RemoveIfApparelDropped>();
                if (hediffComp != null)
                {
                    hediffComp.wornApparel = (Apparel)parent;
                }
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ticksUntilCheck, "ticksUntilCheck", 0);
        }
    }
}

