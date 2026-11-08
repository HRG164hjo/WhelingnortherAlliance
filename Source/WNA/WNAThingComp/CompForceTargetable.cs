using Verse;

namespace WNA.WNAThingComp
{
    internal class PropForceTargetable : CompProperties
    {
        public PropForceTargetable()
        {
            compClass = typeof(CompForceTargetable);
        }
    }
    // Marker comp: lets a non-mannable turret keep a forced target and use the
    // force-attack and hold-fire gizmos.
    internal class CompForceTargetable : ThingComp
    {
    }
}
