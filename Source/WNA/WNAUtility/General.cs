using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
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
        public static class RoomDoorUtility
        {
            public static HashSet<Building_Door> GetAllDoors(Room room)
            {
                HashSet<Building_Door> doors = new HashSet<Building_Door>();
                if (room == null || room.Regions == null) return doors;

                foreach (Region region in room.Regions)
                {
                    if (region?.links == null) continue;

                    foreach (RegionLink link in region.links)
                    {
                        Region other = link.GetOtherRegion(region);
                        if (other?.door != null)
                            doors.Add(other.door);
                    }
                }

                return doors;
            }
        }
    }
}
