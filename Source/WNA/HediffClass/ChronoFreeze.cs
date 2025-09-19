using System.Collections.Generic;
using Verse;
using WNA.WNAUtility;
using static WNA.WNAUtility.ChronoUtility;

namespace WNA.HediffClass
{
    public class ChronoFreeze : HediffWithComps
    {
        public ChronoFreezeSource source;
        public int ticksLeft;
        public override void Tick()
        {
            base.Tick();
            if (ticksLeft > 0) ticksLeft--;
            else pawn.health.RemoveHediff(this);
        }
        public override bool Visible => false;
        public override bool ShouldRemove => ticksLeft <= 0;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksLeft, "ticksLeft");
            Scribe_Values.Look(ref source, "source");
        }
    }
    public class ChronoFreezeMalicious : HediffWithComps
    {
        public float warpProgress = 1.0f;
        public float warpDamage;
        public List<Pawn> attackers = new List<Pawn>();
        public override void Tick()
        {
            base.Tick();
            bool anyAttackerValid = false;
            attackers.RemoveAll(p => p == null || p.Dead || p.DestroyedOrNull());
            if (attackers.Count > 0) anyAttackerValid = true;
            if (!anyAttackerValid)
            {
                pawn.health.RemoveHediff(this);
                return;
            }
            if (pawn.IsHashIntervalTick(10))
            {
                float maxHealth = pawn.RaceProps.baseHealthScale * pawn.RaceProps.baseBodySize * 60;
                float progressToDeduct = warpDamage / maxHealth;
                warpProgress -= progressToDeduct * 10;
                if (warpProgress <= 0)
                {
                    General.TotalRemoving(pawn, false);
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref warpProgress, "warpProgress");
            Scribe_Values.Look(ref warpDamage, "warpDamage");
            Scribe_Collections.Look(ref attackers, "attackers");
        }
    }
}
