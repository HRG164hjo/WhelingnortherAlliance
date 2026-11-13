using RimWorld;
using System.Collections.Generic;
using Verse;

namespace WNA.WNAUtility
{
    internal static class CaravanFreePassUtility
    {
        private static bool ignoresTerrain;

        internal static bool IgnoresTerrain => ignoresTerrain;

        internal static void Begin(bool value)
        {
            ignoresTerrain = value;
        }

        internal static void End()
        {
            ignoresTerrain = false;
        }

        internal static bool PawnHasFreePass(Pawn pawn)
        {
            return TechnoConfigUtility.PawnHasFlag(pawn, TechnoFlag.CanFreePass);
        }

        internal static bool IsMajority(List<Pawn> pawns)
        {
            if (pawns == null)
                return false;

            int total = 0;
            int freePass = 0;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn.Dead || pawn.Downed)
                    continue;
                total++;
                if (PawnHasFreePass(pawn))
                    freePass++;
            }
            return total > 0 && freePass * 2 >= total;
        }

        internal static bool IsMajorityInTransferables(List<TransferableOneWay> transferables, bool remainingOnly)
        {
            if (transferables == null)
                return false;

            int total = 0;
            int freePass = 0;
            for (int i = 0; i < transferables.Count; i++)
            {
                TransferableOneWay transferable = transferables[i];
                if (transferable == null || !transferable.HasAnyThing || !(transferable.AnyThing is Pawn))
                    continue;

                int from = remainingOnly ? transferable.CountToTransfer : 0;
                int to = remainingOnly ? transferable.things.Count : transferable.CountToTransfer;
                for (int j = from; j < to; j++)
                {
                    if (!(transferable.things[j] is Pawn pawn) || pawn.Dead || pawn.Downed)
                        continue;
                    total++;
                    if (PawnHasFreePass(pawn))
                        freePass++;
                }
            }
            return total > 0 && freePass * 2 >= total;
        }

        internal static bool IsMajorityInThingCounts(List<ThingCount> thingCounts)
        {
            if (thingCounts == null)
                return false;

            int total = 0;
            int freePass = 0;
            for (int i = 0; i < thingCounts.Count; i++)
            {
                if (thingCounts[i].Count <= 0 || !(thingCounts[i].Thing is Pawn pawn) || pawn.Dead || pawn.Downed)
                    continue;
                total++;
                if (PawnHasFreePass(pawn))
                    freePass++;
            }
            return total > 0 && freePass * 2 >= total;
        }
    }
}
