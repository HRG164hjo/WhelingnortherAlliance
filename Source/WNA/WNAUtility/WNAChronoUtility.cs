using RimWorld;
using Verse;
using WNA.WNAThingClass;

namespace WNA.WNAUtility
{
    internal class WNAChronoUtility
    {
        internal static bool ChronoImmune(Thing thing)
        {
            if (thing == null)
                return false;
            if (TechnoConfigUtility.ThingHasFlag(thing, TechnoFlag.ImmuneToWarp))
                return true;
            if (thing.def.destroyable == false)
                return true;
            return thing is Pawn pawn && pawn is ThingClass_Whelingnorthan;
        }
    }
}
