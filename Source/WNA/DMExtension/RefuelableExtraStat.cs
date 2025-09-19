using RimWorld;
using Verse;

namespace WNA.DMExtension
{
    public class RefuelableExtraStat : DefModExtension
    {
        public bool useBodySize = false;
        public bool useStat = false;
        public StatDef multStat = StatDefOf.Mass;
        public float multFactor = 1f;
    }
}
