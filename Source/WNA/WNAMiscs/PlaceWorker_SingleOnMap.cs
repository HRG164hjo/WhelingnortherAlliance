using RimWorld;
using Verse;

namespace WNA.WNAMiscs
{
    public class PlaceWorker_SingleOnMap : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                if (building.def == checkingDef && building.Faction == Faction.OfPlayer)
                {
                    return "WNA.PlaceWorker.AlreadyOne".Translate(checkingDef.label);
                }
            }
            return true;
        }
    }
}
