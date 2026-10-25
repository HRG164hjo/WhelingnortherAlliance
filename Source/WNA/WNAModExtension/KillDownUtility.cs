using System.Collections.Generic;
using Verse;

namespace WNA.WNAModExtension
{
    internal class KillDownUtility : DefModExtension
    {
        public int canDieThreshold = 5;
        public bool canEverBeDowned = true;
        public List<PawnCapacityDef> minCapacities = null;
        public float minLevel = 0.3f;
    }
}
