using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using WNA.WNAArea;
using WNA.WNADefOf;
using WNA.WNAMapComp;
using WNA.WNAUtility;

namespace WNA.WNAWorkGiver
{
    internal class WorkGiver_ClearRadField : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override IEnumerable<IntVec3> PotentialWorkCellsGlobal(Pawn pawn)
        {
            Area_RadClearArea area = WNARadFieldUtility.GetRadClearArea(pawn.Map);
            if (area == null)
                return Enumerable.Empty<IntVec3>();
            return area.ActiveCells;
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (!WNARadFieldUtility.ImmuneToRadiation(pawn))
                return true;
            Area_RadClearArea area = WNARadFieldUtility.GetRadClearArea(pawn.Map);
            return area == null || area.TrueCount == 0;
        }

        public override bool HasJobOnCell(Pawn pawn, IntVec3 c, bool forced = false)
        {
            if (!WNARadFieldUtility.ImmuneToRadiation(pawn))
                return false;
            Area_RadClearArea area = WNARadFieldUtility.GetRadClearArea(pawn.Map);
            if (area == null || !area[c])
                return false;
            MapComp_RadField rad = pawn.Map.GetComponent<MapComp_RadField>();
            if (rad == null || !rad.AnyRadAt(c))
                return false;
            return pawn.CanReserve(c, 1, -1, null, forced);
        }

        public override Job JobOnCell(Pawn pawn, IntVec3 c, bool forced = false)
        {
            return JobMaker.MakeJob(WNAMainDefOf.WNA_ClearRadField, c);
        }
    }
}
