using RimWorld;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNAUtility
{
    internal class WNAPermaconstUtility
    {
        internal static bool Immunity(Pawn pawn)
        {
            if (pawn == null) return false;
            if (pawn.Faction != null)
            {
                if (General.IsInternalFaction(pawn.Faction))
                    return true;
                if (pawn.health.hediffSet.HasHediff(WNAMainDefOf.WNA_PermaconstActive))
                    return true;
                if (pawn.Faction == Faction.OfPlayer && pawn.Ideo != null)
                {
                    if (General.PawnHasIdeo(pawn))
                        return true;
                }
            }
            return false;
        }
        internal static void TryMarkHidden(Pawn attacker, Pawn infector)
        {
            if (Immunity(attacker))
                return;
            if (attacker.health != null && attacker.RaceProps.IsFlesh)
            {
                if (!attacker.health.hediffSet.HasHediff(WNAMainDefOf.WNA_PermaconstHidden))
                {
                    attacker.health.AddHediff(WNAMainDefOf.WNA_PermaconstHidden);
                    attacker.stances?.stunner?.StunFor(23, attacker, true, true, true);     
                }
            }
        }
        internal static void TryActivate(Pawn pawn)
        {
            if (pawn == null)
                return;
            if (!pawn.health.hediffSet.HasHediff(WNAMainDefOf.WNA_PermaconstActive))
                pawn.health.AddHediff(WNAMainDefOf.WNA_PermaconstActive);
        }
    }
}
