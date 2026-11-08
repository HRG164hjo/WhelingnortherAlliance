using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI;
using WNA.WNADefOf;
using WNA.WNAMiscs;
using WNA.WNAModExtension;
using WNA.WNAUtility;

namespace WNA.WNAHarmony
{
    internal class Patch_Passability
    {
        // PathFinderCostTuning.costBlockedWallExtraForNaturalWalls 只被 BreachingGrid 使用，
        // 不参与 PathGridJob/PathFinderJob 的计算，但参与 MapGridRequest 的相等性比较。
        // 用它做标记，既能让 free-pass 请求拿到独立缓存的代价图，又不改变任何代价本身。
        private const int FreePassTuningMarker = 8123;

        // Reachability.CanReach 在 Pawn_PathFollower.StartPath 里是用不带 pawn 的 TraverseParms 调用的，
        // 只能靠这个"当前正在由谁发起移动"的标记来识别。
        private static Pawn freePassPather;

        internal static class PawnCheck
        {
            internal static bool IsValidPawn(Pawn pawn)
            {
                if (pawn == null || pawn.Faction == null)
                    return false;
                if (General.IsWNARace(pawn))
                    return true;
                if (pawn.def?.GetModExtension<TechnoConfig>()?.canFreePass == true)
                    return true;
                if (pawn.health?.hediffSet?.hediffs?.Any(h =>
                    h.def?.GetModExtension<TechnoConfig>()?.canFreePass == true) == true)
                    return true;
                if (General.PawnHasIdeo(pawn) && General.IsInternalFaction(pawn.Faction))
                    return true;
                return false;
            }
        }
        internal static class LocomotionUtility
        {
            internal static float ApplyLocomotionUrgency(Pawn pawn, float baseTicks)
            {
                if (pawn?.CurJob == null)
                    return baseTicks;

                switch (pawn.CurJob.locomotionUrgency)
                {
                    case LocomotionUrgency.Amble:
                        baseTicks *= 3f;
                        if (baseTicks < 60f) baseTicks = 60f;
                        break;
                    case LocomotionUrgency.Walk:
                        baseTicks *= 2f;
                        if (baseTicks < 50f) baseTicks = 50f;
                        break;
                    case LocomotionUrgency.Sprint:
                        baseTicks = Mathf.RoundToInt(baseTicks * 0.75f);
                        break;
                }
                return baseTicks;
            }
        }
        [HarmonyPatch(typeof(Pawn_PathFollower))]
        [HarmonyPatch("CostToMoveIntoCell")]
        [HarmonyPatch([typeof(Pawn), typeof(IntVec3)])]
        internal static class Patch_PathFollower
        {
            [HarmonyPostfix]
            internal static void AllowFreeTerrainTraversal(Pawn pawn, IntVec3 c, ref float __result)
            {
                try
                {
                    if (!PawnCheck.IsValidPawn(pawn))
                        return;
                    if (pawn.Map == null || !c.InBounds(pawn.Map))
                        return;
                    float baseTicks = (c.x != pawn.Position.x && c.z != pawn.Position.z)
                        ? pawn.TicksPerMoveDiagonal
                        : pawn.TicksPerMoveCardinal;
                    float moveTicks = LocomotionUtility.ApplyLocomotionUrgency(pawn, baseTicks);
                    __result = Mathf.Max(moveTicks, 1f);
                }
                catch (Exception ex)
                {
                    Log.Error($"[WNA] Patch_Passability error: {ex}");
                }
            }
        }

        // 上面的补丁只影响"走一格要花多少 tick"；Impassable 地形是在 A* 出路径之前就被排除的，
        // 所以下面还要处理代价图、邻接表和可达性预判。RimWorld 1.7 用 Waylith 替换了 Unity，
        // Unity.Jobs/Unity.Burst 由 UnityStubs 提供托管实现，寻路 job 相关的方法都能被正常拦截。

        // 代价图上下文；Pathing.Get 对未注册的 def 会抛异常，这里统一兜底，
        // 免得寻路被一个失效的 PathGridDef 拖垮。
        private static PathingContext FreePassContext(Pathing pathing)
        {
            PathGridDef def = WNAMainDefOf.WNA_PathGrid_FreePassTerrain;
            if (pathing == null || def == null)
                return null;
            try
            {
                return pathing.Get(def);
            }
            catch (Exception ex)
            {
                Log.ErrorOnce($"[WNA] Free-pass path grid not registered: {ex}", def.GetHashCode());
                return null;
            }
        }

        [HarmonyPatch(typeof(Pawn))]
        [HarmonyPatch("GetPathContext", typeof(Pathing))]
        internal static class Patch_Pawn_PathContext
        {
            [HarmonyPostfix]
            internal static void UseFreePassGrid(Pawn __instance, Pathing pathing, ref PathingContext __result)
            {
                if (__instance == null || !PawnCheck.IsValidPawn(__instance))
                    return;
                // 飞行单位使用原版 Flying 代价图，不覆盖。
                if (__result != pathing.Normal && __result != pathing.FenceBlocked)
                    return;
                PathingContext context = FreePassContext(pathing);
                if (context != null)
                    __result = context;
            }
        }

        [HarmonyPatch(typeof(PathFinderCostTuning))]
        [HarmonyPatch("For", typeof(Pawn))]
        internal static class Patch_CostTuning
        {
            [HarmonyPostfix]
            internal static void MarkFreePassRequest(Pawn pawn, ref PathFinderCostTuning __result)
            {
                if (PawnCheck.IsValidPawn(pawn))
                    __result.costBlockedWallExtraForNaturalWalls = FreePassTuningMarker;
            }
        }

        // 代价格图本身由 WNA.WNAMiscs.PathGrid_IgnoreImpassableTerrain 计算（只放开纯地形阻挡，
        // 墙/栅栏/不可通行的物件仍是 ImpassableCost）。这里把 free-pass 请求的那次代价图生成
        // 作业的输入数组换成它，CellIsPassable/CostForCell 的原版逻辑因此自然得出正确结果，
        // 不需要改动代价数字，也保留了原版的 TraverseMode/门禁/栅栏等所有限制。
        [HarmonyPatch(typeof(PathFinderMapData))]
        [HarmonyPatch("ParameterizeGridJob")]
        internal static class Patch_GridJob_Source
        {
            [HarmonyPostfix]
            internal static void UseFreePassGrid(Map ___map, ref PathFinder.MapGridRequest query, ref PathGridJob job)
            {
                if (___map == null || query.tuning.costBlockedWallExtraForNaturalWalls != FreePassTuningMarker)
                    return;
                PathingContext context = FreePassContext(___map.pathing);
                if (context?.pathGrid == null)
                    return;
                job.pathGridDirect = context.pathGrid.Grid_Unsafe.AsReadOnly();
            }
        }

        // 原版在算邻接表时就把 Impassable 地形的方向位抹掉了，A* 因此根本没有通往水里的边。
        // 这里只补回"纯地形阻挡、且格子上没有建筑"的方向；普通 pawn 的格子代价仍是 ImpassableCost，
        // 在 PathFinderJob 里照样会被跳过，所以这个放宽只多几次邻居判断。
        [HarmonyPatch(typeof(ConnectivitySource))]
        [HarmonyPatch("ComputeCellConnectivity", typeof(IntVec3))]
        internal static class Patch_Connectivity
        {
            private static readonly FieldInfo mapField =
                AccessTools.Field(typeof(SimplePathFinderDataSource<CellConnection>), "map");

            [HarmonyPostfix]
            internal static void ReconnectImpassableTerrain(ConnectivitySource __instance, IntVec3 cell, ref CellConnection __result)
            {
                Map map = mapField?.GetValue(__instance) as Map;
                if (map == null || __result == CellConnection.AllNeighbours)
                    return;
                for (int i = 0; i < CellConnectionExtensions.OffsetFromBitIndex.Length; i++)
                {
                    CellConnection flag = CellConnectionExtensions.FlagFromBitIndex[i];
                    if (__result.HasBit(flag))
                        continue;
                    IntVec3 c = cell + CellConnectionExtensions.OffsetFromBitIndex[i];
                    if (!c.InBounds(map) || c.GetEdifice(map) != null)
                        continue;
                    TerrainDef terrain = c.GetTerrain(map);
                    if (terrain == null || terrain.passability != Traversability.Impassable)
                        continue;
                    __result |= flag;
                }
            }
        }

        // StartPath 用的是不带 pawn 的 TraverseParms.For(TraverseMode.PassDoors)，所以这里只认
        // Patch_StartPath 记下的"正在发起移动"的那个 pawn。这样放开范围只到"已经在走路"为止，
        // AI 决策层的 pawn.CanReach（走 ReachabilityUtility → 带 pawn 的 TraverseParms）不受影响。
        [HarmonyPatch(typeof(Reachability))]
        [HarmonyPatch("CanReach", typeof(IntVec3), typeof(LocalTargetInfo), typeof(PathEndMode), typeof(TraverseParms))]
        internal static class Patch_Reachability
        {
            [HarmonyPostfix]
            internal static void AllowFreePass(LocalTargetInfo dest, ref bool __result)
            {
                if (__result)
                    return;
                Pawn pawn = freePassPather;
                if (!PawnCheck.IsValidPawn(pawn) || !pawn.Spawned)
                    return;
                if (dest.Cell.InBounds(pawn.Map))
                    __result = true;
            }
        }

        [HarmonyPatch(typeof(Pawn_PathFollower))]
        [HarmonyPatch("StartPath", typeof(LocalTargetInfo), typeof(PathEndMode))]
        internal static class Patch_StartPath
        {
            [HarmonyPrefix]
            internal static void Enter(Pawn ___pawn)
            {
                if (PawnCheck.IsValidPawn(___pawn))
                    freePassPather = ___pawn;
            }

            [HarmonyPostfix]
            internal static void Exit()
            {
                freePassPather = null;
            }

            [HarmonyFinalizer]
            internal static Exception Recover()
            {
                freePassPather = null;
                return null;
            }
        }
    }
}
