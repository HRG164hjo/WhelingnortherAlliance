using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace WNA.WNAThingComp
{
    internal class PropThingSelfHeal : CompProperties
    {
        public int ticksPerHeal = 250;
        public int healPercent = 10;

        public PropThingSelfHeal()
        {
            compClass = typeof(CompThingSelfHeal);
        }
    }
    internal class CompThingSelfHeal : ThingComp
    {
        private static List<Hediff_Injury> tmpInjuries = new List<Hediff_Injury>();

        public int ticksPassedSinceLastHeal;
        public PropThingSelfHeal Props => (PropThingSelfHeal)props;
        public override void CompTick()
        {
            Tick(1);
        }
        public override void CompTickRare()
        {
            if (parent is Pawn)
                return;
            Tick(250);
        }
        public override void CompTickLong()
        {
            if (parent is Pawn)
                return;
            Tick(2000);
        }
        private void Tick(int ticks)
        {
            ticksPassedSinceLastHeal += ticks;
            if (ticksPassedSinceLastHeal < Props.ticksPerHeal)
                return;
            ticksPassedSinceLastHeal -= Props.ticksPerHeal;
            if (parent is Pawn pawn)
                HealPawn(pawn);
            else
                HealThing();
        }
        private void HealThing()
        {
            if (parent.HitPoints >= parent.MaxHitPoints)
                return;
            int healAmount = (int)(parent.MaxHitPoints * Props.healPercent / 100f);
            parent.HitPoints = Mathf.Min(parent.HitPoints + Mathf.Max(healAmount, 1), parent.MaxHitPoints);
        }
        private void HealPawn(Pawn pawn)
        {
            if (pawn.Dead || pawn.health?.hediffSet == null)
                return;
            pawn.health.hediffSet.GetHediffs(ref tmpInjuries, (Hediff_Injury injury) => !injury.IsPermanent());
            if (tmpInjuries.Count == 0)
                return;
            Hediff_Injury target = tmpInjuries.RandomElement();
            float partMaxHealth = target.Part != null ? target.Part.def.GetMaxHealth(pawn) : 50f;
            float healAmount = Mathf.Max(1f, partMaxHealth * Props.healPercent / 100f);
            target.Heal(healAmount);
            if (target.Severity <= 0f)
                pawn.health.RemoveHediff(target);
        }
        public override void PostExposeData()
        {
            Scribe_Values.Look(ref ticksPassedSinceLastHeal, "ticksPassedSinceLastHeal", 0);
        }
    }
}
