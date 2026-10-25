using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using WNA.WNADefOf;

namespace WNA.WNAJobDriver
{
    public class JobDriver_ReadHolyorder : JobDriver
    {
        private const TargetIndex HolyorderIndex = TargetIndex.A;
        private const TargetIndex ReadingSpotIndex = TargetIndex.B;
        private const int SpotSearchChecks = 40;
        private static readonly List<IntVec3> tmpHomeCells = new List<IntVec3>();
        private Mote readMote;
        private Apparel Holyorder => job.GetTarget(HolyorderIndex).Thing as Apparel;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }
        public override void Notify_Starting()
        {
            base.Notify_Starting();
            job.ignoreJoyTimeAssignment = true;
            ChooseReadingSpot();
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => pawn.apparel == null
                || Holyorder == null
                || !pawn.apparel.WornApparel.Contains(Holyorder));
            AddFinishAction(condition => CleanupReadMote());
            IntVec3 cell = job.GetTarget(ReadingSpotIndex).IsValid
                ? job.GetTarget(ReadingSpotIndex).Cell
                : pawn.Position;
            if (!cell.IsValid || !pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly))
                cell = pawn.Position;
            job.SetTarget(ReadingSpotIndex, cell);
            yield return Toils_Goto.GotoCell(cell, PathEndMode.OnCell);
            Toil wait = Toils_General.Wait(job.def.joyDuration);
            wait.handlingFacing = true;
            wait.initAction = delegate
            {
                pawn.pather.StopDead();
                FaceReadingDirection();
                EnsureReadMote();
            };
            wait.tickIntervalAction = delegate(int delta)
            {
                EnsureReadMote();
                readMote?.Maintain();
                GainReadingNeeds(delta);
            };
            yield return wait;
        }
        private void ChooseReadingSpot()
        {
            IntVec3 cell;
            if (TryFindSeat(out cell) && TryReserveSpot(cell))
            {
                job.SetTarget(ReadingSpotIndex, cell);
                return;
            }
            if (TryFindRoofedHomeCell(out cell) && TryReserveSpot(cell))
            {
                job.SetTarget(ReadingSpotIndex, cell);
                return;
            }
            if (TryFindRandomHomeCell(out cell) && TryReserveSpot(cell))
            {
                job.SetTarget(ReadingSpotIndex, cell);
                return;
            }
            job.SetTarget(ReadingSpotIndex, pawn.Position);
        }
        private bool TryFindSeat(out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            Thing found = GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial),
                PathEndMode.OnCell,
                TraverseParms.For(pawn),
                9999f,
                t => ValidateSeat(t, pawn));
            return found != null
                && Toils_Ingest.TryFindFreeSittingSpotOnThing(found, pawn, out cell);
        }
        private static bool ValidateSeat(Thing t, Pawn pawn)
        {
            if (t.def.building == null || !t.def.building.isSittable)
                return false;
            if (!t.Map.areaManager.Home[t.Position])
                return false;
            if (t.Fogged() || t.IsForbidden(pawn))
                return false;
            if (t.IsBurning() || t.HostileTo(pawn))
                return false;
            if (!t.IsSociallyProper(pawn))
                return false;
            if (t.Position.GetDangerFor(pawn, t.Map) != Danger.None)
                return false;
            if (!pawn.CanReach(t, PathEndMode.OnCell, Danger.None))
                return false;
            if (!pawn.CanReserve(t))
                return false;
            return Toils_Ingest.TryFindFreeSittingSpotOnThing(t, pawn, out IntVec3 _);
        }
        private bool TryReserveSpot(IntVec3 cell)
        {
            if (!pawn.ReserveSittableOrSpot(cell, job, false))
                return false;
            pawn.Map.pawnDestinationReservationManager.Reserve(pawn, job, cell);
            return true;
        }
        private bool TryFindRoofedHomeCell(out IntVec3 cell)
        {
            return TryFindHomeCell(c => c.Roofed(pawn.Map), out cell);
        }
        private bool TryFindRandomHomeCell(out IntVec3 cell)
        {
            return TryFindHomeCell(null, out cell);
        }
        private bool TryFindHomeCell(Predicate<IntVec3> extraValidator, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            tmpHomeCells.Clear();
            foreach (IntVec3 c in pawn.Map.areaManager.Home.ActiveCells)
            {
                if (!c.Standable(pawn.Map))
                    continue;
                if (c.Fogged(pawn.Map) || c.IsForbidden(pawn))
                    continue;
                if (c.GetDangerFor(pawn, pawn.Map) != Danger.None)
                    continue;
                if (c.GetDoor(pawn.Map) != null)
                    continue;
                Building edifice = c.GetEdifice(pawn.Map);
                if (edifice != null
                    && (edifice.def.building == null || !edifice.def.building.isSittable))
                    continue;
                if (extraValidator != null && !extraValidator(c))
                    continue;
                tmpHomeCells.Add(c);
            }
            if (tmpHomeCells.Count == 0)
                return false;
            tmpHomeCells.Shuffle();
            int checks = Mathf.Min(tmpHomeCells.Count, SpotSearchChecks);
            for (int i = 0; i < checks; i++)
            {
                IntVec3 candidate = tmpHomeCells[i];
                if (!pawn.CanReach(candidate, PathEndMode.OnCell, Danger.None))
                    continue;
                if (!pawn.CanReserveSittableOrSpot(candidate))
                    continue;
                cell = candidate;
                return true;
            }
            return false;
        }
        private void FaceReadingDirection()
        {
            Building chair = pawn.Position.GetEdifice(pawn.Map);
            if (chair != null && chair.def.building != null && chair.def.building.isSittable)
            {
                pawn.rotationTracker.FaceCell(pawn.Position + chair.Rotation.FacingCell);
                return;
            }
            pawn.rotationTracker.FaceCell(pawn.Position + new Rot4(Rand.Range(0, 4)).FacingCell);
        }
        private void GainReadingNeeds(int delta)
        {
            float level = 0.0006f;
            if (pawn.needs?.mood != null)
                pawn.needs.mood.CurLevel += level * delta;
            if (pawn.needs?.rest != null)
                pawn.needs.rest.CurLevel += level * delta;
            if (pawn.needs?.joy != null)
                pawn.needs.joy.CurLevel += level * delta;
            if (pawn.needs?.learning != null)
                pawn.needs.learning.CurLevel += level * delta;
            if (pawn.needs?.play != null)
                pawn.needs.play.CurLevel += level * delta;
        }
        private void EnsureReadMote()
        {
            if (readMote != null && !readMote.Destroyed)
                return;
            if (!pawn.Spawned || pawn.Map == null)
                return;
            readMote = MoteMaker.MakeAttachedOverlay(
                pawn,
                WNAMainDefOf.WNA_Mote_HolyorderRead,
                new Vector3(0f, 0f, 0.35f));
            if (readMote != null)
            {
                readMote.link1.rotateWithTarget = true;
                readMote.Maintain();
            }
        }
        private void CleanupReadMote()
        {
            if (readMote != null && !readMote.Destroyed)
                readMote.Destroy();
            readMote = null;
        }
    }
}
