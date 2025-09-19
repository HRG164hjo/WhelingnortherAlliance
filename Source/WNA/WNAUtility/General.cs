using RimWorld.Planet;
using Verse;

namespace WNA.WNAUtility
{
    public class General
    {
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
            if (pawn.IsWorldPawn())
            {
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
            }
            pawn.Destroy(DestroyMode.Vanish);
        }
    }
}
