using RimWorld;
using System;
using Verse;

namespace WNA.WNADamageWorker
{
    internal class DmgWorker_AdvEMP : DamageWorker
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            DamageResult result = base.Apply(dinfo, victim);
            bool violence = dinfo.Def.ExternalViolenceFor(victim);
            int ticks = Convert.ToInt32(dinfo.Amount);
            if (victim is Pawn pawn)
            {
                int stun = (int)pawn.stances?.stunner?.StunTicksLeft;
                int stag = (int)pawn.stances?.stagger?.StaggerTicksLeft;
                pawn.stances?.stunner?.StunFor(stun + ticks, dinfo.Instigator, true, false);
                pawn.stances?.stagger?.StaggerFor(stag + ticks, 0);
                if (pawn.stances?.stunner?.StunTicksLeft >= (pawn.BodySize * pawn.HealthScale * 200))
                    pawn.health.AddHediff(HediffDefOf.CatatonicBreakdown);
                if (pawn.health.hediffSet.HasHediff(DefDatabase<HediffDef>.GetNamed("HeartAttack")))
                {
                    Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamed("HeartAttack"));
                    pawn.health.RemoveHediff(hediff);
                }
            }
            if (victim is ThingWithComps twc && twc.HasComp<CompStunnable>())
            {
                var comp = twc.GetComp<CompStunnable>();
                int old = comp.StunHandler.StunTicksLeft;
                int size = twc.def.size.x * twc.def.size.z;
                comp.StunHandler.StunFor((old + ticks), dinfo.Instigator, true, false);
                if (twc.HasComp<CompBreakdownable>())
                {
                    var cbd = twc.GetComp<CompBreakdownable>();
                    cbd.DoBreakdown();
                }
                if (twc.HasComp<CompExplosive>() && comp.StunHandler.StunTicksLeft > size * 300)
                    twc.Kill();
            }
            return violence? result : new DamageResult();
        }
    }
}
