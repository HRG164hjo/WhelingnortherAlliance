using RimWorld;
using Verse;

namespace WNA.WNAModExtension
{
    internal class SeismicConfig : DefModExtension
    {
        public ThingDef flyerDef = null;
        public SimpleCurve distanceFromDamage = null;
        public SimpleCurve bodySizeDistanceFactor = null;
        public SimpleCurve landingDamageFromDistance = null;
        public SimpleCurve bodySizeDamageFactor = null;
        public float landingArmorPenetration = 0f;
        public float edgeKillMargin = 1f;
        public bool edgeKill = true;
        public EffecterDef flightEffecter = null;
        public SoundDef landingSound = null;

        internal float EvalDistance(float damage) => distanceFromDamage?.Evaluate(damage) ?? 0f;
        internal float EvalBodyDistanceFactor(float bodySize) => bodySizeDistanceFactor?.Evaluate(bodySize) ?? 1f;
        internal float EvalLandingDamage(float distance) => landingDamageFromDistance?.Evaluate(distance) ?? 0f;
        internal float EvalBodyDamageFactor(float bodySize) => bodySizeDamageFactor?.Evaluate(bodySize) ?? 1f;
    }
}
