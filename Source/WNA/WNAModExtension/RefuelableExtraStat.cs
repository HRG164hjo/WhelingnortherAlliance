using RimWorld;
using Verse;

namespace WNA.WNAModExtension
{
    internal class RefuelableExtraStat : DefModExtension
    {
        public bool useBodySize = false;
        public bool useStat = false;
        public StatDef multStat = null;
        public float multFactor = 1f;
    }
}
