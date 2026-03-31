using RimWorld;
using Verse;
using WNA.WNADefOf;

namespace WNA.HediffCompProp
{
    public class PropPermaconstActive : HediffCompProperties
    {
        public PropPermaconstActive()
        {
            compClass = typeof(CompPermaconstActive);
        }
    }
    public class CompPermaconstActive : HediffComp
    {
        public PropPermaconstActive Props => (PropPermaconstActive)props;
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
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            Pawn p = parent.pawn;
            if (p == null || PawnValid(p)) return;
            Faction zombieFaction = Find.FactionManager.FirstFactionOfDef(WNAMainDefOf.WNA_FactionPCC);
            if (zombieFaction != null && p.Faction != zombieFaction)
            {
                p.SetFaction(zombieFaction);
                p.stances?.stunner?.StunFor(67, null, false, false, true);
            }
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
