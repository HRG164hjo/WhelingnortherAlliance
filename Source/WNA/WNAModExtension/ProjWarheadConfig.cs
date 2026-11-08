using Verse;

namespace WNA.WNAModExtension
{
    public enum ProjWarheadType
    {
        Ambient,
        CellSpread,
        Cluster,
        DirectExtra
    }
    public class ProjWarheadConfig
    {
        public ProjWarheadType type;
        public float chance = 1f;
        public DamageDef damageDef;
        public float damageFactor = 1f;
        public float armorPenetrationFactor = 1f;
        public float armorPenetration = -1f;
        public bool affectNonPawns = true;
        public bool affectsDirectTarget = false;
        public bool requireLineOfSight = false;
        public int maxTargets = 0;
        public float width = 1f;
        public float radius = 0f;
        public float percentAtMax = 1f;
        public int amount = 1;
        public FleckDef fleck;
    }
}
