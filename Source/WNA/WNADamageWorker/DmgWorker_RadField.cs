using RimWorld;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNADamageWorker
{
    internal class DmgWorker_RadField : DamageWorker_AddInjury
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            DamageInfo emp = new DamageInfo(DamageDefOf.EMP, dinfo.Amount, float.MaxValue);
            if (victim is Pawn pawn)
            {
                if (pawn == null || !pawn.Spawned)
                    return base.Apply(dinfo, victim);
                FleshTypeDef fleshType = pawn.RaceProps.FleshType;
                bool isNonBiological = fleshType != FleshTypeDefOf.Normal
                    && fleshType != FleshTypeDefOf.Insectoid;
                pawn.TakeDamage(emp);
                if (isNonBiological)
                    pawn.stances.stunner.StunFor((int)dinfo.Amount, null, false);
            }
            /*RadSpreadConfig config = new RadSpreadConfig();
            WNARadFieldUtility.RadSpread(
                victim.Position,
                victim.Map,
                config,
                0.3f,
                (int)dinfo.Amount);*/
            return base.Apply(dinfo, victim);
        }
    }
}
