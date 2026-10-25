using UnityEngine;
using Verse;

namespace WNA.WNAThingClass.WNAProj
{
    internal class Proj_Inviso : ProjBase
    {
        private Thing forcedTarget;
        public override Vector3 ExactPosition => destination + Vector3.up * def.Altitude;
        public override Quaternion ExactRotation
        {
            get
            {
                Vector3 dir = (destination - origin).Yto0();
                if (dir.sqrMagnitude < 1E-06f)
                    return Quaternion.identity;
                return Quaternion.LookRotation(dir);
            }
        }
        protected override void PrepareLaunch()
        {
            Map map = base.Map;
            forcedTarget = Config.invisoHit ? ResolveForcedTarget(map) : null;
            if (forcedTarget != null)
                destination = forcedTarget.DrawPos.Yto0();
            IntVec3 cell = DestinationCell;
            if (map != null && cell.InBounds(map))
                base.Position = cell;
        }
        protected override void FinishLaunch()
        {
            ImpactSomething();
        }
        protected override void ImpactSomething()
        {
            Thing target = forcedTarget;
            if (target != null && IsUsableTarget(target, base.Map))
            {
                Impact(target);
                return;
            }
            base.ImpactSomething();
        }
        private Thing ResolveForcedTarget(Map map)
        {
            if (map == null)
                return null;
            Thing thing = intendedTarget.HasThing ? intendedTarget.Thing : null;
            if (!IsUsableTarget(thing, map))
                thing = usedTarget.HasThing ? usedTarget.Thing : null;
            return IsUsableTarget(thing, map) ? thing : null;
        }

        private bool IsUsableTarget(Thing thing, Map map)
        {
            if (thing == null || map == null || thing == Launcher)
                return false;
            if (thing.Destroyed || !thing.Spawned || thing.Map != map)
                return false;
            return thing.Position.InBounds(map);
        }
    }
}
