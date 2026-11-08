using System.Collections.Generic;
using Verse;

namespace WNA.WNAModExtension
{
    public enum ProjFlightMode
    {
        Ballistic,
        Arcing,
        Homing,
        Instant
    }
    internal class ProjConfig : DefModExtension
    {
        public bool invisoHit = true;
        public ProjFlightMode flightMode = ProjFlightMode.Ballistic;
        // Ammo-side scatter, same distribution as VerbProperties.forcedMissRadius. Do not set both.
        public bool inaccurate = false;
        public float inaccurateRange = 0f;
        // ROT missile proj
        public int ROT = 15;
        public float maxTurnPerUpdate = 0f;
        public int maxFlightTicks = 600;
        // Composite warheads
        public List<ProjWarheadConfig> warheads = new List<ProjWarheadConfig>();
        // When true, warheads skip any target whose faction is not hostile to the launcher's faction.
        public bool affectsAllies = false;
        // Main explosion reads def.projectile explosion fields
        public bool explosive = false;
        public List<ProjExtraExplosionConfig> extraExplosions = new List<ProjExtraExplosionConfig>();
        public static ProjConfig Get(Def def)
        {
            return def?.GetModExtension<ProjConfig>();
        }
    }
}
