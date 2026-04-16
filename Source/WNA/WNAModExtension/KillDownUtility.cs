using System.Collections.Generic;
using Verse;

namespace WNA.WNAModExtension
{
    public class KillDownUtility : DefModExtension
    {
        public int canDieThreshold = 5;
        public List<PawnCapacityDef> minCapacities;
        public float minLevel = 0.3f;
    }
}
