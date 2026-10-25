using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using WNA.WNAArea;
using WNA.WNADefOf;
using WNA.WNAMapComp;
using WNA.WNAUtility;

namespace WNA.WNAJobDriver
{
    internal class JobDriver_ClearRadField : JobDriver
    {
        private const float WorkAmount = 500f;
        private const int MaxDistanceToClear = 10;
        private const int MaxDistanceToClearSquared = 100;
        private const int CellsToClearPerJob = 6;
        private float workDone;
        private static readonly List<IntVec3> tmpCellsToClear = new List<IntVec3>();
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref workDone, "workDone", 0f);
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() =>
            {
                Area_RadClearArea area = WNARadFieldUtility.GetRadClearArea(Map);
                return area == null || !area[TargetLocA];
            });
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);
            Toil toil = ToilMaker.MakeToil("MakeNewToils");
            toil.tickIntervalAction = delegate(int delta)
            {
                workDone += pawn.GetStatValue(StatDefOf.GeneralLaborSpeed) * (float)delta;
                if (workDone >= WorkAmount)
                {
                    ClearRadAt(TargetLocA, Map);
                    SpawnWastepack(TargetLocA, Map);
                    ReadyForNextToil();
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.PlaySustainerOrSound(() => SoundDefOf.Interact_CleanFilth);
            toil.WithProgressBar(TargetIndex.A, () => workDone / WorkAmount, interpolateBetweenActorAndTarget: true);
            toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            toil.WithEffect(EffecterDefOf.Clean, TargetIndex.A);
            yield return toil;
        }
        private void ClearRadAt(IntVec3 root, Map map)
        {
            MapComp_RadField rad = map.GetComponent<MapComp_RadField>();
            Area_RadClearArea area = WNARadFieldUtility.GetRadClearArea(map);
            if (rad == null || area == null)
                return;
            tmpCellsToClear.Clear();
            tmpCellsToClear.AddRange(area.ActiveCells.Where((IntVec3 c) => c.DistanceToSquared(root) < MaxDistanceToClearSquared));
            tmpCellsToClear.SortBy((IntVec3 c) => c.DistanceToSquared(root));
            int num = CellsToClearPerJob;
            for (int i = 0; i < tmpCellsToClear.Count; i++)
            {
                if (CanClear(root, map, tmpCellsToClear[i], rad))
                {
                    rad.SetRad(tmpCellsToClear[i], 0);
                    num--;
                    if (num <= 0)
                        break;
                }
            }
            tmpCellsToClear.Clear();
            if (num > 0)
            {
                num -= ClearRadially(root, map, rad, num);
                if (num > 0)
                    ClearRadially(root, map, rad, num, ignoreOtherPawnsClearingCell: true);
            }
        }
        private int ClearRadially(IntVec3 root, Map map, MapComp_RadField rad, int maxToClear = 5, bool ignoreOtherPawnsClearingCell = false)
        {
            int num = 0;
            foreach (IntVec3 item in GenRadial.RadialCellsAround(root, MaxDistanceToClear, useCenter: true))
            {
                if (CanClear(root, map, item, rad, ignoreOtherPawnsClearingCell))
                {
                    rad.SetRad(item, 0);
                    num++;
                    if (num >= maxToClear)
                        break;
                }
            }
            return num;
        }
        private bool CanClear(IntVec3 root, Map map, IntVec3 c, MapComp_RadField rad, bool ignoreOtherPawnsClearingCell = false)
        {
            Area_RadClearArea area = WNARadFieldUtility.GetRadClearArea(map);
            if (area == null || !area[c])
                return false;
            if (rad.GetRad(c) <= 0)
                return false;
            if (!ignoreOtherPawnsClearingCell && AnyOtherPawnClearing(pawn, c))
                return false;
            if (c.GetRoom(map) != null && root.GetRoom(map) != c.GetRoom(map))
                return false;
            if (c.DistanceToSquared(root) > MaxDistanceToClearSquared)
                return false;
            return true;
        }
        private bool AnyOtherPawnClearing(Pawn pawn, IntVec3 cell)
        {
            List<Pawn> freeColonistsSpawned = pawn.Map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < freeColonistsSpawned.Count; i++)
            {
                if (freeColonistsSpawned[i] != pawn && freeColonistsSpawned[i].CurJobDef == WNAMainDefOf.WNA_ClearRadField)
                {
                    LocalTargetInfo target = freeColonistsSpawned[i].CurJob.GetTarget(TargetIndex.A);
                    if (target.IsValid && target.Cell == cell)
                        return true;
                }
            }
            return false;
        }
        private void SpawnWastepack(IntVec3 root, Map map)
        {
            ThingDef wastepack = DefDatabase<ThingDef>.GetNamedSilentFail("WNA_ParticleVial");
            if (wastepack == null)
                return;
            IntVec3 intVec = CellFinder.FindNoWipeSpawnLocNear(root, map, wastepack, Rot4.North);
            GenSpawn.Spawn(wastepack, intVec.IsValid ? intVec : root, map);
        }
    }
}
