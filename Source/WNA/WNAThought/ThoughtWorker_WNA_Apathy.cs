using RimWorld;
using Verse;
using WNA.WNAUtility;

namespace WNA.WNAThought
{
    public class ThoughtWorker_WNA_Apathy : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!General.HasApathyGene(p))
                return ThoughtState.Inactive;
            if (General.IsInternalFaction(p.Faction))
                return ThoughtState.ActiveAtStage(0);
            if (General.FactionHasIdeo(p.Faction) && General.PawnHasIdeo(p))
                return ThoughtState.ActiveAtStage(0);
            return ThoughtState.Inactive;
        }
    }
}
