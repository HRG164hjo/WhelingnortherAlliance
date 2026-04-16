using RimWorld;
using Verse;

namespace WNA.WNADamageWorker
{
    public class Hypothermal : DamageWorker_AddInjury
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            if (victim is Pawn pawn)
            {
                if(pawn.stances != null && pawn.stances.stunner != null)
                {
                    int stunOld = pawn.stances.stunner.StunTicksLeft;
                    int stunNew = stunOld + (int)dinfo.Amount;
                    int core = pawn.RaceProps.body.corePart != null ? pawn.RaceProps.body.corePart.def.hitPoints : 1;
                    int crit = (int)(core * pawn.BodySize * pawn.HealthScale);
                    pawn.stances.stunner.StunFor(stunNew, dinfo.Instigator, true, false);
                    if (pawn.Downed || pawn.stances.stunner.StunTicksLeft >= crit)
                        pawn.health.AddHediff(HediffDefOf.MissingBodyPart, dinfo.HitPart);
                }
                else if(pawn.Downed)
                        pawn.health.AddHediff(HediffDefOf.MissingBodyPart, dinfo.HitPart);
            }
            if (victim is Thing thing && thing.def.useHitPoints)
            {
                float mult = thing.MaxHitPoints / thing.HitPoints;
                float amount = dinfo.Amount * mult;
                dinfo.SetAmount(amount);
                if (thing is ThingWithComps twc && twc.HasComp<CompStunnable>())
                {
                    var comp = twc.GetComp<CompStunnable>();
                    comp.StunHandler.StunFor((int)amount, dinfo.Instigator, true, false);
                }
            }
            return base.Apply(dinfo, victim);
        }
    }
}
