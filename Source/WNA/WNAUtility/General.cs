using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace WNA.WNAUtility
{
    public class General
    {
        internal static void DebugalDestroy(Thing thing)
        {
            Thing.allowDestroyNonDestroyable = true;
            try
            {
                thing.Destroy();
            }
            finally
            {
                Thing.allowDestroyNonDestroyable = false;
            }
        }
        public static void TotalRemoving(Pawn pawn, bool remains = true)
        {
            if (pawn == null || pawn.DestroyedOrNull()) return;
            pawn.relations?.ClearAllRelations();
            pawn.ownership?.UnclaimAll();
            if (remains)
            {
                pawn.apparel?.DropAll(pawn.Position);
                pawn.inventory?.DropAllNearPawn(pawn.Position);
                pawn.equipment?.DropAllEquipment(pawn.Position);
            }
            else
            {
                pawn.apparel?.DestroyAll();
                pawn.inventory?.DestroyAll();
                pawn.equipment?.DestroyAllEquipment();
            }
            DebugalDestroy(pawn);
            if (pawn.IsWorldPawn())
                Find.WorldPawns.RemoveAndDiscardPawnViaGC(pawn);
        }
    }
}
