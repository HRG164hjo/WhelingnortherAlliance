using Verse;
using WNA.WNAModExtension;

namespace WNA.WNAUtility
{
    internal enum TechnoFlag
    {
        IgnoreRoof,
        ImmuneToRadiation,
        ImmuneToWarp,
        ImmuneToMindControl,
        CanFreePass
    }

    internal static class TechnoConfigUtility
    {
        internal static bool ThingHasFlag(Thing thing, TechnoFlag flag)
        {
            if (thing == null)
                return false;
            if (DefHasFlag(thing.def, flag))
                return true;
            return thing is Pawn pawn && PawnHasFlag(pawn, flag);
        }

        internal static bool PawnHasFlag(Pawn pawn, TechnoFlag flag)
        {
            if (pawn == null)
                return false;
            if (DefHasFlag(pawn.def, flag))
                return true;

            var apparel = pawn.apparel?.WornApparel;
            if (apparel != null)
            {
                for (int i = 0; i < apparel.Count; i++)
                {
                    if (DefHasFlag(apparel[i].def, flag))
                        return true;
                }
            }

            var equipment = pawn.equipment?.AllEquipmentListForReading;
            if (equipment != null)
            {
                for (int i = 0; i < equipment.Count; i++)
                {
                    if (DefHasFlag(equipment[i].def, flag))
                        return true;
                }
            }

            var hediffs = pawn.health?.hediffSet?.hediffs;
            if (hediffs != null)
            {
                for (int i = 0; i < hediffs.Count; i++)
                {
                    if (DefHasFlag(hediffs[i].def, flag))
                        return true;
                }
            }

            return false;
        }

        private static bool DefHasFlag(Def def, TechnoFlag flag)
        {
            TechnoConfig config = TechnoConfig.Get(def);
            if (config == null)
                return false;

            switch (flag)
            {
                case TechnoFlag.IgnoreRoof:
                    return config.ignoreRoof == true;
                case TechnoFlag.ImmuneToRadiation:
                    return config.immuneToRadiation == true;
                case TechnoFlag.ImmuneToWarp:
                    return config.immuneToWarp == true;
                case TechnoFlag.ImmuneToMindControl:
                    return config.immuneToMindControl == true;
                case TechnoFlag.CanFreePass:
                    return config.canFreePass == true;
                default:
                    return false;
            }
        }
    }
}
