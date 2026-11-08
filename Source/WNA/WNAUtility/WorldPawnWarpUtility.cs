using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using WNA.WNADefOf;
using WNA.WNAGameComp;
using WNA.WNARitual;

namespace WNA.WNAUtility
{
    public static class WorldPawnWarpUtility
    {
        public const float MoteSizeCells = 5f;
        public const int MinDelayTicks = 20000;
        public const int MaxDelayTicks = 30000;
        public const int MinStunTicks = 1000;
        public const int MaxStunTicks = 1500;

        public static bool ResearchCompleted =>
            WNAMainDefOf.WNA_GlobalEntrallment?.IsFinished == true;

        public static bool RitualAvailable(out string reason)
        {
            reason = null;
            if (!ModsConfig.IdeologyActive)
            {
                reason = "WNA_WorldPawnWarpUnavailableIdeology".Translate().Resolve();
                return false;
            }
            if (!ResearchCompleted)
            {
                reason = "WNA_WorldPawnWarpUnavailableResearch".Translate().Resolve();
                return false;
            }
            return true;
        }

        public static List<Pawn> Candidates()
        {
            List<Pawn> result = new List<Pawn>();
            WorldPawns worldPawns = Find.WorldPawns;
            if (worldPawns == null)
                return result;
            AddCandidates(worldPawns.AllPawnsAlive, result);
            AddCandidates(worldPawns.AllPawnsDead, result);
            return result;
        }

        private static void AddCandidates(IEnumerable<Pawn> source, List<Pawn> result)
        {
            foreach (Pawn pawn in source)
            {
                if (CanBeSummoned(pawn))
                    result.Add(pawn);
            }
        }

        public static bool CanBeSummoned(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed || pawn.Discarded)
                return false;
            if (pawn.RaceProps == null || !pawn.RaceProps.Humanlike)
                return false;
            if (pawn.Dead && pawn.RaceProps.corpseDef == null)
                return false;
            WorldPawns worldPawns = Find.WorldPawns;
            if (worldPawns == null || !worldPawns.Contains(pawn))
                return false;
            if (pawn.SpawnedOrAnyParentSpawned)
                return false;
            if (pawn.Dead && pawn.ParentHolder is Corpse)
                return false;
            GameComp_WorldPawnWarp comp = GameComp_WorldPawnWarp.Instance;
            if (comp != null && comp.IsPending(pawn))
                return false;
            return true;
        }

        public static void BeginSelection(Map map, IntVec3 cell)
        {
            GameComp_WorldPawnWarp comp = GameComp_WorldPawnWarp.Instance;
            if (comp == null)
                return;
            if (!ModsConfig.IdeologyActive)
            {
                comp.ClearSelection();
                return;
            }
            if (map == null || !Find.Maps.Contains(map))
                map = Find.AnyPlayerHomeMap;
            List<Pawn> candidates = Candidates();
            if (candidates.Count == 0)
            {
                comp.ClearSelection();
                Messages.Message("WNA_WorldPawnWarpNoTargets".Translate(), MessageTypeDefOf.NeutralEvent, false);
                return;
            }
            comp.SetSelectionPending(map, cell);
            Find.WindowStack.Add(new Dialog_WorldPawnWarpTargets(candidates, map, cell));
        }

        public static void ScheduleSummons(List<Pawn> pawns, Map map, IntVec3 cell)
        {
            GameComp_WorldPawnWarp comp = GameComp_WorldPawnWarp.Instance;
            if (comp == null || pawns.NullOrEmpty())
            {
                Messages.Message("WNA_WorldPawnWarpFailed".Translate(), MessageTypeDefOf.NegativeEvent, false);
                return;
            }
            if (map == null || !Find.Maps.Contains(map))
                map = Find.AnyPlayerHomeMap;
            int delay = Rand.RangeInclusive(MinDelayTicks, MaxDelayTicks);
            comp.Schedule(pawns, map, cell, delay);
            comp.ClearSelection();
            Messages.Message("WNA_WorldPawnWarpQueued".Translate(pawns.Count.Named("COUNT")), MessageTypeDefOf.NeutralEvent, false);
        }

        // Returns false only when the pawn could not be placed yet and should be retried.
        public static bool TrySummon(Pawn pawn, Map map, IntVec3 cell)
        {
            if (pawn == null || pawn.Destroyed || pawn.Discarded)
                return true;
            if (pawn.SpawnedOrAnyParentSpawned)
                return true;
            if (pawn.Dead && pawn.ParentHolder is Corpse)
                return true;
            if (map == null || !Find.Maps.Contains(map))
                return false;
            IntVec3 root = cell.IsValid && cell.InBounds(map) ? cell : map.Center;
            if (!TryFindSpawnCell(root, map, out IntVec3 spawnCell))
                return false;
            DetachFromHolder(pawn);
            if (pawn.Dead)
                return SpawnCorpse(pawn, spawnCell, map);
            Find.WorldPawns.RemovePawn(pawn);
            GenSpawn.Spawn(pawn, spawnCell, map, Rot4.Random);
            pawn.stances?.stunner?.StunFor(Rand.RangeInclusive(MinStunTicks, MaxStunTicks), null, addBattleLog: false, showMote: false);
            Messages.Message("WNA_WorldPawnWarpArrived".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.PositiveEvent, false);
            return true;
        }

        private static bool SpawnCorpse(Pawn pawn, IntVec3 cell, Map map)
        {
            Find.WorldPawns.RemovePawn(pawn);
            Corpse corpse = pawn.MakeCorpse(null, false, 0f);
            if (corpse == null)
                return true;
            GenSpawn.Spawn(corpse, cell, map, Rot4.Random);
            Messages.Message("WNA_WorldPawnWarpArrivedDead".Translate(pawn.Named("PAWN")), corpse, MessageTypeDefOf.NeutralEvent, false);
            return true;
        }

        private static bool TryFindSpawnCell(IntVec3 root, Map map, out IntVec3 cell)
        {
            if (CellFinder.TryFindRandomSpawnCellForPawnNear(root, map, out cell, 10))
                return true;
            if (CellFinder.TryFindRandomCellNear(root, map, 20, c => c.Standable(map) && !c.Fogged(map), out cell))
                return true;
            return CellFinderLoose.TryGetRandomCellWith(c => c.Standable(map) && !c.Fogged(map), map, 200, out cell);
        }

        private static void DetachFromHolder(Pawn pawn)
        {
            if (pawn.ParentHolder == null)
                return;
            Caravan caravan = pawn.GetCaravan();
            if (caravan != null)
            {
                caravan.RemovePawn(pawn);
                return;
            }
            ThingOwner owner = pawn.ParentHolder.GetDirectlyHeldThings();
            if (owner != null && owner.Contains(pawn))
                owner.Remove(pawn);
        }

        public static void SpawnRitualMote(Vector3 loc, Map map)
        {
            ThingDef moteDef = WNAMainDefOf.WNA_Mote_UpliftMarker;
            if (moteDef == null || map == null || !loc.InBounds(map))
                return;
            float baseSize = moteDef.graphicData?.drawSize.x ?? 1f;
            float scale = baseSize > 0.001f ? MoteSizeCells / baseSize : MoteSizeCells;
            MoteMaker.MakeStaticMote(loc, map, moteDef, scale);
        }
    }
}
