using RimWorld;
using Verse;
using WNA.WNADefOf;
using WNA.WNAUtility;

namespace WNA.WNAHediffComp
{
    internal class PropPermaconstHidden : HediffCompProperties
    {
        public PropPermaconstHidden()
        {
            compClass = typeof(CompPermaconstHidden);
        }
    }
    internal class CompPermaconstHidden : HediffComp
    {
        public PropPermaconstHidden Props => (PropPermaconstHidden)props;
        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
            if (dinfo.Instigator != null && dinfo.Instigator is Pawn attacker)
                WNAPermaconstUtility.TryMarkHidden(attacker, parent.pawn);
        }
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            if (parent.pawn.Dead)
            {
                LongEventHandler.ExecuteWhenFinished(
                    delegate{ResurrectionUtility.TryResurrect(parent.pawn);});
            }
            Pawn.health.RemoveHediff(parent);
            WNAPermaconstUtility.TryActivate(parent.pawn);
        }
    }
}
