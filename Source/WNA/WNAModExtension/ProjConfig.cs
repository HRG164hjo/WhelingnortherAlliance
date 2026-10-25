using Verse;

namespace WNA.WNAModExtension
{
    internal class ProjConfig : DefModExtension
    {
        public bool invisoHit = true;
        // ROT missile proj
        public int ROT = 15;
        public float maxTurnPerUpdate = 0f;
        public int maxFlightTicks = 600;
        // Cluster-Scatter proj
        public float clusterRadius = 1.5f;
        public int clusterAmount = 3;
        public float clusterDmgFactor = 1f;
        public bool clusterRequireLoS = false;
        public FleckDef clusterFleck = null;
        // CellSpread proj
        public float cellSpread = 0f;
        public float percentAtMax = 1f;
        public bool affectRequireLoS = false;
        // Ambient proj
        public float ambientDmgFactor = 1f;
        public int ambientMaxAffect = 0;
        public float ambientWidth = 1f;
        public bool ambientAffectsNonPawn = true;
        public static ProjConfig Get(Def def)
        {
            return def?.GetModExtension<ProjConfig>();
        }
    }
}
