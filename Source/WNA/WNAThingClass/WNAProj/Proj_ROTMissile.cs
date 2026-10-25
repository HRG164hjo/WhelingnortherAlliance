using UnityEngine;
using Verse;
using WNA.WNAUtility;

namespace WNA.WNAThingClass.WNAProj
{
    internal class Proj_ROTMissile : ProjBase
    {
        private int ticksSinceCorrection;
        private int ticksAlive;
        private IntVec3 lastKnownTargetCell = IntVec3.Invalid;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksSinceCorrection, "ticksSinceCorrection", 0);
            Scribe_Values.Look(ref ticksAlive, "ticksAlive", 0);
            Scribe_Values.Look(ref lastKnownTargetCell, "lastKnownTargetCell", IntVec3.Invalid);
        }
        protected override void PrepareLaunch()
        {
            ticksSinceCorrection = 0;
            ticksAlive = 0;
            RememberTargetCell();
        }
        protected override void TickInterval(int delta)
        {
            Vector3 before = ExactPosition;
            base.TickInterval(delta);
            Map map = base.Map;
            if (map == null)
                return;
            if (WNABeamLineUtility.TryGetBlockingWall(map, before, ExactPosition, DestinationCell, out Thing wall))
            {
                IntVec3 cell = before.ToIntVec3();
                if (cell.InBounds(map))
                    base.Position = cell;
                Impact(wall);
                return;
            }
            ticksAlive += delta;
            ticksSinceCorrection += delta;
            if (ticksSinceCorrection >= Mathf.Max(1, Config.ROT))
            {
                ticksSinceCorrection = 0;
                Steer(map);
                if (base.Map == null)
                    return;
            }
            int fuse = Config.maxFlightTicks;
            if (fuse > 0 && ticksAlive >= fuse)
            {
                usedTarget = new LocalTargetInfo(base.Position);
                ImpactSomething();
            }
        }
        private void RememberTargetCell()
        {
            Thing thing = AcquireTarget();
            if (thing != null)
                lastKnownTargetCell = thing.Position;
            else if (!lastKnownTargetCell.IsValid && usedTarget.IsValid)
                lastKnownTargetCell = usedTarget.Cell;
        }
        private Thing AcquireTarget()
        {
            Map map = base.Map;
            if (map == null)
                return null;
            Thing thing = usedTarget.HasThing ? usedTarget.Thing : null;
            if (!IsTrackable(thing, map))
                thing = intendedTarget.HasThing ? intendedTarget.Thing : null;
            return IsTrackable(thing, map) ? thing : null;
        }
        private bool IsTrackable(Thing thing, Map map)
        {
            if (thing == null || thing.Destroyed || !thing.Spawned || thing.Map != map)
                return false;
            return thing.Position.InBounds(map);
        }
        private void Steer(Map map)
        {
            Vector3 goal;
            Thing thing = AcquireTarget();
            if (thing != null)
            {
                goal = thing.DrawPos.Yto0();
                lastKnownTargetCell = thing.Position;
            }
            else if (lastKnownTargetCell.IsValid && lastKnownTargetCell.InBounds(map))
                goal = lastKnownTargetCell.ToVector3Shifted();
            else
                return;
            Vector3 current = ExactPosition.Yto0();
            Vector3 toGoal = goal - current;
            float distance = toGoal.magnitude;
            if (distance < 0.05f)
                return;
            Vector3 desired = toGoal / distance;
            Vector3 heading = (destination - origin).Yto0();
            if (heading.sqrMagnitude > 1E-06f)
            {
                heading.Normalize();
                float maxTurn = Config.maxTurnPerUpdate;
                if (maxTurn > 0f && Vector3.Angle(heading, desired) > maxTurn)
                    desired = Vector3.RotateTowards(heading, desired, maxTurn * Mathf.Deg2Rad, 0f).normalized;
            }
            float speed = SpeedPerTick;
            float length = Mathf.Max(distance, speed);
            origin = current;
            destination = current + desired * length;
            ticksToImpact = Mathf.Max(1, Mathf.CeilToInt(length / speed));
            lifetime = ticksToImpact;
            base.Position = ExactPosition.ToIntVec3();
        }
    }
}
