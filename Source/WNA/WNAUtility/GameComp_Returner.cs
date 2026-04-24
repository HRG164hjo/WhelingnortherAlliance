using RimWorld;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNAUtility
{
    public class GameComp_Returner : GameComponent
    {
        private const int IntervalTicks = 23;
        private Faction wna => Find.FactionManager.FirstFactionOfDef(WNAMainDefOf.WNA_FactionWNA);
        private Faction pcc => Find.FactionManager.FirstFactionOfDef(WNAMainDefOf.WNA_FactionPCC);
        public GameComp_Returner(Game game) { }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % IntervalTicks != 0)
                return;
            Map targetMap = Find.AnyPlayerHomeMap;
            if (targetMap == null)
                return;
            WorldComp_Permaconst comp = Find.World?.GetComponent<WorldComp_Permaconst>();
            bool active = comp != null && comp.EventActive;
            var pawns = PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (!ShouldProcess(pawn))
                    continue;
                if (IsWNATarget(pawn) && TryResurrectAndReturn(pawn, wna, targetMap))
                    continue;
                if (active && IsPCCTarget(pawn))
                    TryResurrectAndReturn(pawn, pcc, targetMap);
            }
        }
        private static bool ShouldProcess(Pawn pawn)
        {
            return pawn != null
                   && pawn.Dead
                   && !pawn.DestroyedOrNull()
                   && !pawn.Discarded;
        }
        private static bool IsWNATarget(Pawn pawn)
        {
            return pawn.def == WNAMainDefOf.WNA_WNThan
                   || pawn.health?.hediffSet?.HasHediff(WNAMainDefOf.WNA_Inhuman) == true;
        }
        private static bool IsPCCTarget(Pawn pawn)
        {
            return pawn.health?.hediffSet?.HasHediff(WNAMainDefOf.WNA_PermaconstActive) == true;
        }
        private static bool TryResurrectAndReturn(Pawn pawn, Faction targetFaction, Map map)
        {
            if (pawn == null || !pawn.Dead || pawn.Discarded || pawn.DestroyedOrNull())
                return false;
            if (!ResurrectionUtility.TryResurrect(pawn))
                return false;
            ReturnPawnToMap(pawn, targetFaction, map);
            return true;
        }
        private static void ReturnPawnToMap(Pawn pawn, Faction faction, Map map)
        {
            if (pawn == null || pawn.DestroyedOrNull() || map == null)
                return;
            if (faction != null && pawn.Faction != faction)
                pawn.SetFaction(faction);
            if (pawn.Spawned)
                pawn.DeSpawn();
            IntVec3 root = map.Center;
            if (!CellFinder.TryFindRandomSpawnCellForPawnNear(root, map, out IntVec3 cell, 10))
                cell = CellFinder.RandomClosewalkCellNear(root, map, 10);
            GenSpawn.Spawn(pawn, cell, map);
        }
    }
}