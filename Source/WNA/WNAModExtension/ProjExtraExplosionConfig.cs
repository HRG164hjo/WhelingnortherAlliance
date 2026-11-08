using Verse;

namespace WNA.WNAModExtension
{
    public class ProjExtraExplosionConfig
    {
        public int count = 0;
        public float radius = 3.9f;
        public float areaSize = 5f;
        public DamageDef damageDef;
        public float damageAmountFactor = 1f;
        public float chanceToStartFire = 0f;
        public SoundDef soundExplode;
    }
}
