using RimWorld;
using WNA.WNADefOf;
using WNA.WNAWorldComp;

namespace WNA.WNAMiscs
{
    public class GoodwillSituationWorker_WNAIdeoConflict : GoodwillSituationWorker
    {
        public override int GetMaxGoodwill(Faction other)
        {
            if (other?.def != WNAMainDefOf.WNA_FactionWNA)
                return 100;
            return WorldComp_WorldSync.IdeoConflictActive ? def.baseMaxGoodwill : 100;
        }
    }
}
