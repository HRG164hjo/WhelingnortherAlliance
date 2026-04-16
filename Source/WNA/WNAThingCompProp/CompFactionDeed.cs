using RimWorld;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNAThingCompProp
{
    public class PropFactionDeed : CompProperties_Usable
    {
        public FactionDef targetFactionDef;
        public int goodwillCost = -23;
        public HistoryEventDef goodwillReason = WNAMainDefOf.WNA_HE_UseDeedOn;
        public PropFactionDeed()    
        {
            compClass = typeof(CompFactionDeed);
        }
    }
    public class CompFactionDeed : CompUsable
    {
        public PropFactionDeed DeedProps => (PropFactionDeed)props;
        public override AcceptanceReport CanBeUsedBy(Pawn p, bool forced = false, bool ignoreReserveAndReachable = false)
        {
            var baseRep = base.CanBeUsedBy(p, forced, ignoreReserveAndReachable);
            if (!baseRep.Accepted) return baseRep;
            var fac = Find.FactionManager.FirstFactionOfDef(DeedProps.targetFactionDef);
            if (fac == null) return "Target faction not found.";
            var kind = fac.RelationKindWith(Faction.OfPlayer);
            if (kind != FactionRelationKind.Ally)
                return kind == FactionRelationKind.Neutral ? "Need allied relation." : "Cannot use while hostile.";
            return true;
        }
    }
}
