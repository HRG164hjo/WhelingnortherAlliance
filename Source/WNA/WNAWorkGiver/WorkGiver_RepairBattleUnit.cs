using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using WNA.WNADefOf;
using WNA.WNAThingClass;
using WNA.WNAThingComp;

namespace WNA.WNAWorkGiver
{
    internal class WorkGiver_RepairBattleUnit : WorkGiver_Scanner
    {
        private static List<Hediff_Injury> tmpInjuries = new List<Hediff_Injury>();

        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Deadly;
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return pawn is ThingClass_BattleUnit || pawn.Faction == null || pawn.Map == null;
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction);
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is ThingClass_BattleUnit unit) || unit.Dead || unit.Destroyed)
                return false;
            if (unit.Faction == null || !unit.Faction.IsPlayer || unit.Faction != pawn.Faction)
                return false;
            if (!forced && !(unit.TryGetComp<CompBattleUnitRepairable>()?.autoRepair ?? false))
                return false;
            if (unit.IsForbidden(pawn) || unit.IsBurning() || unit.IsAttacking())
                return false;
            if (!NeedsRepair(unit))
                return false;
            return pawn.CanReserveAndReach(unit, PathEndMode.Touch, MaxPathDanger(pawn), 1, -1, null, forced);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(WNAMainDefOf.WNA_RepairBattleUnit, t);
        }

        public static bool NeedsRepair(Pawn unit)
        {
            if (unit.Dead || unit.health?.hediffSet == null)
                return false;
            unit.health.hediffSet.GetHediffs(ref tmpInjuries, (Hediff_Injury injury) => !injury.IsPermanent() && injury.Severity > 0f);
            return tmpInjuries.Count > 0;
        }
    }
}
