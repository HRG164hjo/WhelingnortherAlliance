using RimWorld;
using Verse;
using WNA.WNADefOf;
using WNA.WNAGameComp;

namespace WNA.WNADamageWorker
{
    internal class DmgWorker_Seismic : DamageWorker_AddInjury
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            DamageResult result = base.Apply(dinfo, victim);
            if (dinfo.Amount > 0f && victim is Pawn pawn && !pawn.Dead && !pawn.Destroyed)
                GameComp_SeismicLaunch.Enqueue(pawn, dinfo.Def, dinfo.Angle, result.totalDamageDealt, dinfo.Instigator);
            return result;
        }

        internal static bool IsEdgeKillImmune(Pawn pawn) =>
            pawn.def == WNAMainDefOf.WNA_Humanoid || pawn.def == WNAMainDefOf.WNA_Whelingnorthan;
    }
}
