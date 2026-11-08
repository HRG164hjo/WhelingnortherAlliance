using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using WNA.WNADefOf;
using WNA.WNAUtility;

namespace WNA.WNAJobDriver
{
    public class JobDriver_ReadHolyorder : JobDriver
    {
        private const TargetIndex HolyorderIndex = TargetIndex.A;
        private const TargetIndex ReadingSpotIndex = TargetIndex.B;
        private const int SpotSearchChecks = 40;
        private const int NearbySpotRadius = 5;
        private const float GainPerTick = 0.001f;
        private static readonly List<IntVec3> tmpHomeCells = new List<IntVec3>();
        private Mote readMote;
        private Apparel Holyorder => job.GetTarget(HolyorderIndex).Thing as Apparel;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            job.ignoreJoyTimeAssignment = true;
            ChooseReadingSpot();
            return true;
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
            if (pawn.Map != null && (!IsSafeReadingCell(cell) || !pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly)))
            {
                pawn.ClearReservationsForJob(job);
                cell = RepickReadingSpot();
                job.SetTarget(ReadingSpotIndex, cell);
            }
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
                ReadingGainings(delta);
                ReadingConversion(pawn);
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
            if (TryFindNearbyCell(out cell) && TryReserveSpot(cell))
            {
                job.SetTarget(ReadingSpotIndex, cell);
                return;
            }
            job.SetTarget(ReadingSpotIndex, pawn.Position);
        }
        private IntVec3 RepickReadingSpot()
        {
            IntVec3 cell;
            if (TryFindNearbyCell(out cell) && TryReserveSpot(cell))
                return cell;
            return pawn.Position;
        }
        private bool IsSafeReadingCell(IntVec3 cell)
        {
            return cell.IsValid && !IsInteractionCell(cell);
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
            return found != null && TryFindFreeSittingSpotOnThing(found, pawn, out cell);
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
            return TryFindFreeSittingSpotOnThing(t, pawn, out IntVec3 _);
        }
        private static bool TryFindFreeSittingSpotOnThing(Thing t, Pawn pawn, out IntVec3 cell)
        {
            Map map = t.Map;
            foreach (IntVec3 c in t.OccupiedRect())
            {
                if (c.IsBuildingInteractionCell(map) || c.Fogged(map))
                    continue;
                if (!pawn.CanReserveSittableOrSpot(c))
                    continue;
                cell = c;
                return true;
            }
            cell = IntVec3.Invalid;
            return false;
        }
        private bool IsInteractionCell(IntVec3 cell)
        {
            return cell.IsBuildingInteractionCell(pawn.Map);
        }
        private bool TryReserveSpot(IntVec3 cell)
        {
            if (!cell.IsValid || IsInteractionCell(cell))
                return false;
            if (!pawn.CanReserveSittableOrSpot(cell))
                return false;
            if (pawn.Map.pawnDestinationReservationManager.IsReserved(cell))
                return false;
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
        private bool TryFindNearbyCell(out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            foreach (IntVec3 c in GenRadial.RadialCellsAround(pawn.Position, NearbySpotRadius, true))
            {
                if (!c.InBounds(pawn.Map) || !c.Standable(pawn.Map))
                    continue;
                if (c.Fogged(pawn.Map) || c.IsForbidden(pawn))
                    continue;
                if (IsInteractionCell(c))
                    continue;
                if (c.GetDoor(pawn.Map) != null)
                    continue;
                Building edifice = c.GetEdifice(pawn.Map);
                if (edifice != null
                    && (edifice.def.building == null || !edifice.def.building.isSittable))
                    continue;
                if (c.GetDangerFor(pawn, pawn.Map) != Danger.None)
                    continue;
                if (!pawn.CanReach(c, PathEndMode.OnCell, Danger.None))
                    continue;
                if (!pawn.CanReserveSittableOrSpot(c))
                    continue;
                cell = c;
                return true;
            }
            return false;
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
                if (IsInteractionCell(c))
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
                if (pawn.Map.pawnDestinationReservationManager.IsReserved(candidate))
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
        private void ReadingGainings(int delta)
        {
            if (delta <= 0)
                return;
            float gain = GainPerTick * 50f * delta;
            pawn.psychicEntropy?.OffsetPsyfocusDirectly(gain);
            GainNeeds(gain);
            GainGeneResources(gain);
        }
        private void GainNeeds(float gain)
        {
            List<Need> needs = pawn.needs?.AllNeeds;
            if (needs == null)
                return;
            for (int i = 0; i < needs.Count; i++)
            {
                Need need = needs[i];
                if (IsUpgradableNeed(need))
                    need.CurLevel += gain;
            }
        }
        private static bool IsUpgradableNeed(Need need)
        {
            if (need?.def == null)
                return false;
            if (need is Need_Suppression)
                return false;
            return need.CurLevel < need.MaxLevel;
        }
        private void GainGeneResources(float gainPercent)
        {
            List<Gene> genes = pawn.genes?.GenesListForReading;
            if (genes == null)
                return;
            for (int i = 0; i < genes.Count; i++)
            {
                if (!(genes[i] is Gene_Resource resource) || !resource.Active)
                    continue;
                float max = resource.Max;
                if (max > 0f && resource.Value < max)
                    resource.Value += max * gainPercent;
            }
        }
        private static void ReadingConversion(Pawn pawn)
        {
            if (!General.PawnHasIdeo(pawn))
                pawn.ideo.SetIdeo(General.WNAIdeo);
            return;
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
