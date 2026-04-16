using RimWorld;
using Verse;

namespace WNA.WNAModExtension
{
    public class RefuelableExtraStat : DefModExtension
    {
        public bool useBodySize = false;
        public bool useStat = false;
        public StatDef multStat;
        public float multFactor = 1f;
    }
}
