using UnityEngine;
using Verse;

namespace WNA.ThingCompProp
{
    public class CompThingSelfHeal : CompProperties
    {
        public int ticksPerHeal = 250;
        public int healPercent = 10;
        public int damageLimit = -1;

        public CompThingSelfHeal()
        {
            compClass = typeof(ThingSelfHeal);
        }
    }
    public class ThingSelfHeal : ThingComp
    {
        public int ticksPassedSinceLastHeal;
        public CompThingSelfHeal Props => (CompThingSelfHeal)props;
        public override void CompTick()
        {
            Tick(1);
        }
        public override void CompTickRare()
        {
            Tick(250);
        }
        public override void CompTickLong()
        {
            Tick(2000);
        }
        private void Tick(int ticks)
        {
            ticksPassedSinceLastHeal += ticks;
            if (ticksPassedSinceLastHeal >= Props.ticksPerHeal)
            {
                ticksPassedSinceLastHeal -= Props.ticksPerHeal;
                if (parent.HitPoints < parent.MaxHitPoints)
                {
                    int healAmount = (int)(parent.MaxHitPoints * Props.healPercent / 100f);
                    parent.HitPoints = Mathf.Min(parent.HitPoints + healAmount, parent.MaxHitPoints);
                }
            }
        }
        public override void PostExposeData()
        {
            Scribe_Values.Look(ref ticksPassedSinceLastHeal, "ticksPassedSinceLastHeal", 0);
        }
    }
}
