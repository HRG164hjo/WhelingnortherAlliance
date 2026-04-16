using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNAHediffCompProp
{
    public class PropPermaconstHidden : HediffCompProperties
    {
        public PropPermaconstHidden()
        {
            compClass = typeof(CompPermaconstHidden);
        }
    }
    public class CompPermaconstHidden : HediffComp
    {
        public PropPermaconstHidden Props => (PropPermaconstHidden)props;
        private bool PawnValid(Pawn pawn)
        {
            if (pawn == null) return false;
            if (pawn.Faction != null)
            {
                if (pawn.Faction.def == WNAMainDefOf.WNA_FactionWNA ||
                    pawn.Faction.def == WNAMainDefOf.WNA_FactionPCC)
                    return true;
                if (pawn.Faction == Faction.OfPlayer && pawn.Ideo != null)
                {
                    if (pawn.Ideo.HasPrecept(WNAMainDefOf.WNA_P_Proselyte))
                        return true;
                }
            }
            return false;
        }
        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
            if (dinfo.Instigator != null && dinfo.Instigator is Pawn attacker)
            {
                if (PawnValid(attacker))
                    return;
                if (attacker.health != null && attacker.RaceProps.IsFlesh)
                {
                    if (!attacker.health.hediffSet.HasHediff(WNAMainDefOf.WNA_PermaconstHidden))
                    {
                        attacker.health.AddHediff(WNAMainDefOf.WNA_PermaconstHidden);
                        attacker.stances?.stunner?.StunFor(23, parent.pawn, true, true, true);
                    }
                }
            }
        }
    }
}
