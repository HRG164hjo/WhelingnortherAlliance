using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using WNA.WNADefOf;
using WNA.WNAThingClass;
using WNA.WNAThingComp;
using WNA.WNAWorkGiver;

namespace WNA.WNAJobGiver
{
    public class JobGiver_BattleUnitRepair : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!(pawn is ThingClass_BattleUnit) || pawn.Dead || pawn.Downed || !pawn.Spawned || pawn.Map == null)
                return null;
            if (pawn.Drafted || pawn.Faction == null)
                return null;

            if (CanRepair(pawn, pawn))
                return JobMaker.MakeJob(WNAMainDefOf.WNA_RepairBattleUnit, pawn);

            Pawn ally = FindAllyToRepair(pawn);
            if (ally != null)
                return JobMaker.MakeJob(WNAMainDefOf.WNA_RepairBattleUnit, ally);
            return null;
        }

        private static bool CanRepair(Pawn worker, Pawn target)
        {
            if (target.Dead || target.Destroyed || target.Map != worker.Map)
                return false;
            CompBattleUnitRepairable repairable = target.TryGetComp<CompBattleUnitRepairable>();
            if (repairable == null || !repairable.autoRepair)
                return false;
            if (target.IsBurning() || target.IsAttacking() || target.IsForbidden(worker))
                return false;
            if (!WorkGiver_RepairBattleUnit.NeedsRepair(target))
                return false;
            return target == worker || worker.CanReserveAndReach(target, PathEndMode.Touch, Danger.Deadly);
        }

        private static Pawn FindAllyToRepair(Pawn worker)
        {
            Pawn best = null;
            float bestDist = float.MaxValue;
            List<Pawn> allies = worker.Map.mapPawns.SpawnedPawnsInFaction(worker.Faction);
            for (int i = 0; i < allies.Count; i++)
            {
                Pawn ally = allies[i];
                if (ally == worker || !CanRepair(worker, ally))
                    continue;
                float dist = worker.Position.DistanceToSquared(ally.Position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = ally;
                }
            }
            return best;
        }
    }
}
