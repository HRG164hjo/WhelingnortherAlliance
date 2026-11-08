using RimWorld;
using WNA.WNAUtility;

namespace WNA.WNAMiscs
{
    public class GoodwillSituationWorker_WNADefeated : GoodwillSituationWorker
    {
        public override int GetMaxGoodwill(Faction other)
        {
            return WNAFactionDefeatUtility.IsLockedDefeated(other) ? def.baseMaxGoodwill : 100;
        }
    }
}
