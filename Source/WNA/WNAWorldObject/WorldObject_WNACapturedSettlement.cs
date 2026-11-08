using RimWorld.Planet;

namespace WNA.WNAWorldObject
{
    // Deliberately not a DestroyedSettlement: vanilla empty-map cleanup and other mods' ruin expiry both filter
    // on that type, and both would delete these outposts. Staying off that hierarchy also keeps faction war mods
    // from treating them as invadable MapParents.
    public class WorldObject_WNACapturedSettlement : WorldObject
    {
    }
}
