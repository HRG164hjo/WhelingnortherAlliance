using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace WNA.WNAUtility
{
    internal class WNATeleporterUtility
    {
        private const int ScanInterval = 13;
        private const float SafeCellSearchRadius = 8f;
        public static bool TeleportCheck(Pawn pawn, TeleporterConfig config, ref int lastWarp)
        {
            if (pawn == null || config == null || !pawn.Spawned)
                return false;
            Map map = pawn.Map;
            if (map == null)
                return false;
            int now = Find.TickManager.TicksGame;
            if (now - lastWarp < config.ResolvedCooldownTicks)
                return false;
            if (!pawn.IsHashIntervalTick(ScanInterval))
                return false;
            if (!IsEligible(pawn, config, out IntVec3 destCell))
                return false;
            IntVec3 target = ClampToRange(pawn, destCell, config.ResolvedMaxDistance);
            if (!TryFindClosestSafeCell(pawn, map, target, out IntVec3 cell)
                || cell == pawn.Position)
                return false;
            DoTeleport(pawn, cell, destCell, config);
            lastWarp = now;
            return true;
        }
        private static bool IsEligible(Pawn pawn, TeleporterConfig config, out IntVec3 destCell)
        {
            destCell = IntVec3.Invalid;
            if (pawn.Dead || pawn.Downed)
                return false;
            if (config.InternalFactionOnly && pawn.Faction != Faction.OfPlayer && !General.IsInternalFaction(pawn.Faction))
                return false;
            if (pawn.CurJob != null && pawn.CurJob.def == JobDefOf.GotoWander)
                return false;
            if (pawn.stances != null && pawn.stances.FullBodyBusy)
                return false;
            if (pawn.roping != null && pawn.roping.IsRopingOthers)
                return false;
            Pawn_PathFollower pather = pawn.pather;
            if (pather == null || !pather.Moving)
                return false;
            LocalTargetInfo dest = pather.Destination;
            if (!dest.IsValid)
                return false;
            destCell = dest.Cell;
            if (destCell == pawn.Position)
                return false;
            int min = config.ResolvedMinDistance;
            return pawn.Position.DistanceToSquared(destCell) >= min * min;
        }
        private static IntVec3 ClampToRange(Pawn pawn, IntVec3 destCell, int maxDistance)
        {
            if (maxDistance <= 0
                || pawn.Position.DistanceToSquared(destCell) <= maxDistance * maxDistance)
                return destCell;
            PawnPath path = pawn.pather?.curPath;
            if (path != null && path.Found && path.NodesLeftCount > 1)
            {
                int nodesAhead = Mathf.Clamp(maxDistance, 1, path.NodesLeftCount - 1);
                return path.Peek(nodesAhead);
            }
            Vector3 dir = (destCell - pawn.Position).ToVector3().normalized;
            if (dir == Vector3.zero)
                return destCell;
            return (pawn.Position.ToVector3() + dir * maxDistance).ToIntVec3();
        }
        public static bool TryFindClosestSafeCell(Pawn pawn, Map map, IntVec3 target, out IntVec3 result)
        {
            result = target;
            if (pawn == null || map == null || !target.IsValid)
                return false;
            if (IsSafeCell(pawn, map, target))
                return true;
            int count = GenRadial.NumCellsInRadius(SafeCellSearchRadius);
            for (int i = 1; i < count; i++)
            {
                IntVec3 c = target + GenRadial.RadialPattern[i];
                if (c.InBounds(map) && IsSafeCell(pawn, map, c))
                {
                    result = c;
                    return true;
                }
            }
            return false;
        }
        private static bool IsSafeCell(Pawn pawn, Map map, IntVec3 c)
        {
            if (!c.InBounds(map) || c.Fogged(map))
                return false;
            if (!c.WalkableBy(map, pawn) || !c.Standable(map))
                return false;
            return !PawnUtility.AnyPawnBlockingPathAt(c, pawn, actAsIfHadCollideWithPawnsJob: false, collideOnlyWithStandingPawns: true);
        }
        private static void DoTeleport(Pawn pawn, IntVec3 cell, IntVec3 finalDest, TeleporterConfig config)
        {
            pawn.Position = cell;
            pawn.Notify_Teleported(endCurrentJob: false);
            pawn.pather?.StartPath(finalDest, PathEndMode.OnCell);
            int stuns = config.ResolvedStunTicks;
            if (stuns > 0 && pawn.stances != null)
                pawn.stances.stunner.StunFor(stuns, null, false, false, true);
        }
    }
    internal class TeleporterConfig
    {
        public const int DefaultMaxDistance = 400;
        public int MinDistance = 3;
        public int MaxDistance;
        public int CooldownTicks = 120;
        public int WarpStunEffect = -1;
        public bool InternalFactionOnly = true;
        public int ResolvedMinDistance => (MinDistance > 0) ? MinDistance : 1;
        public int ResolvedMaxDistance => (MaxDistance > 0) ? MaxDistance : DefaultMaxDistance;
        public int ResolvedCooldownTicks => (CooldownTicks > 0) ? CooldownTicks : 1;
        public int ResolvedStunTicks => (WarpStunEffect >= 0) ? WarpStunEffect : Mathf.Max(ResolvedCooldownTicks / 3, 1);
    }
}
