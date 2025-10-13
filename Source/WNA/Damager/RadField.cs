using RimWorld;
using Verse;

namespace WNA.Damager
{
    public class RadField : DamageWorker_AddInjury
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            DamageResult damageResult = base.Apply(dinfo, victim);
            DamageInfo stun = new DamageInfo(DamageDefOf.Stun, dinfo.Amount, float.MaxValue);
            Pawn pawn = victim as Pawn;
            if (pawn == null || !pawn.Spawned)
                return damageResult;
            FleshTypeDef fleshType = pawn.RaceProps.FleshType;
            bool isNonBiological = fleshType != FleshTypeDefOf.Normal &&
                                   fleshType != FleshTypeDefOf.Insectoid;
            if (isNonBiological) pawn.TakeDamage(stun);
            return damageResult;
        }
    }
}
