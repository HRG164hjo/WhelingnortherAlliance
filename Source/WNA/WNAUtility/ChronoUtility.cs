using RimWorld;
using Verse;
using WNA.WNADefOf;
using WNA.WNAModExtension;

namespace WNA.WNAUtility
{
    public class ChronoUtility
    {
        internal static bool ChronoImmune(Thing thing)
        {
            TechnoConfig config = TechnoConfig.Get(thing.def);
            if (config != null && config.immuneToWarp == true)
                return true;
            if (thing.def.useHitPoints)
                return true;
            if (thing is Pawn pawn)
            {
                if (pawn.def == WNAMainDefOf.WNA_WNThan)
                    return true;
                if (pawn.apparel != null)
                {
                    foreach (Apparel apparel in pawn.apparel.WornApparel)
                    {
                        TechnoConfig ac = TechnoConfig.Get(apparel.def);
                        if (ac != null && ac.immuneToWarp == true)
                            return true;
                    }
                }
                if (pawn.health != null && pawn.health.hediffSet != null)
                {
                    foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                    {
                        if (hediff.def != null)
                        {
                            TechnoConfig hc = TechnoConfig.Get(hediff.def);
                            if (hc != null && hc.immuneToWarp == true)
                                return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
