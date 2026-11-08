using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI.Group;
using WNA.WNADefOf;
using WNA.WNAUtility;

namespace WNA.WNAGameComp
{
    internal class GameComp_Returner : GameComponent
    {
        private const int IntervalTicks = 67;
        private static readonly List<Pawn> tmpPawns = [];
        public GameComp_Returner(Game game) {}
        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % IntervalTicks != 0)
                return;
            Map map = FindMapWithCondition();
            if (map == null || Find.WorldPawns == null)
                return;
            if (InternalPawnsOnMaps() >= WNAMod.Settings.pawnReturnerMaxLimit)
                return;
            tmpPawns.Clear();
            tmpPawns.AddRange(Find.WorldPawns.AllPawnsAlive);
            tmpPawns.AddRange(Find.WorldPawns.AllPawnsDead);
            for (int i = 0; i < tmpPawns.Count; i++)
            {
                Pawn pawn = tmpPawns[i];
                if (ShouldReturn(pawn))
                    ReturnToMap(pawn, map);
            }
        }
        private static Map FindMapWithCondition()
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i].gameConditionManager.ConditionIsActive(WNAMainDefOf.WNA_GameCond_PermaconstActive))
                    return maps[i];
            }
            return null;
        }
        private static int InternalPawnsOnMaps()
        {
            int count = 0;
            List<Pawn> pawns = PawnsFinder.AllMaps;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (General.IsInternalFaction(pawns[i].Faction))
                    count++;
            }
            return count;
        }
        private static bool ShouldReturn(Pawn pawn)
        {
            if (pawn == null || pawn.DestroyedOrNull() || pawn.Discarded)
                return false;
            if (pawn.def == null || pawn.kindDef == null)
                return false;
            if (pawn.def != WNAMainDefOf.WNA_Whelingnorthan
                && pawn.def != WNAMainDefOf.WNA_Humanoid
                && !(pawn.health.hediffSet.hediffs.Any(h => h.def == WNAMainDefOf.WNA_PermaconstActive)))
                return false;
            if (!General.IsInternalFaction(pawn.Faction))
                return false;
            if (General.IsInternalFaction(pawn.Faction) && !pawn.HostileTo(Faction.OfPlayer))
            {
                if (PawnUtility.IsFactionLeader(pawn))
                    return false;
                return !pawn.SpawnedOrAnyParentSpawned && LeftMapRecently(pawn);
            }
            return false;
        }
        private static bool LeftMapRecently(Pawn pawn)
        {
            if (pawn.becameWorldPawnTickAbs < 0)
                return false;
            int windowTicks = (int)(WNAMod.Settings.pawnReturnerRecencyDays * GenDate.TicksPerDay);
            return GenTicks.TicksAbs - pawn.becameWorldPawnTickAbs <= windowTicks;
        }
        private static void ReturnToMap(Pawn pawn, Map map)
        {
            if (pawn.Dead && !ResurrectionUtility.TryResurrect(pawn))
                return;
            if (pawn.Spawned)
                pawn.DeSpawn();
            IntVec3 root = map.Center;
            if (!CellFinder.TryFindRandomSpawnCellForPawnNear(root, map, out IntVec3 cell, 10))
                cell = CellFinder.RandomClosewalkCellNear(root, map, 10);
            GenSpawn.Spawn(pawn, cell, map);
            EnsureAssaultLord(pawn, map);
        }
        private static void EnsureAssaultLord(Pawn pawn, Map map)
        {
            if (pawn.Faction == null || pawn.GetLord() != null || !pawn.HostileTo(Faction.OfPlayer))
                return;
            LordMaker.MakeNewLord(pawn.Faction, new LordJob_AssaultColony(pawn.Faction), map, Gen.YieldSingle(pawn));
        }
    }
}
