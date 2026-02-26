using RimWorld;
using Verse;
using WNA.WNADefOf;

namespace WNA.Damager
{
    public class RadField : DamageWorker_AddInjury
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            HediffDef hediff = WNAMainDefOf.WNA_RadRing;
            DamageInfo emp = new DamageInfo(DamageDefOf.EMP, dinfo.Amount * 10f, float.MaxValue);
            if(victim is Pawn pawn)
            {
                if (pawn == null || !pawn.Spawned)
                    return base.Apply(dinfo, victim);
                FleshTypeDef fleshType = pawn.RaceProps.FleshType;
                bool isNonBiological = fleshType != FleshTypeDefOf.Normal &&
                                       fleshType != FleshTypeDefOf.Insectoid;
                pawn.health.AddHediff(hediff);
                pawn.TakeDamage(emp);
                if (isNonBiological)
                    pawn.stances.stunner.StunFor((int)dinfo.Amount, null, false);
            }
            return base.Apply(dinfo, victim); ;
        }
    }
}
