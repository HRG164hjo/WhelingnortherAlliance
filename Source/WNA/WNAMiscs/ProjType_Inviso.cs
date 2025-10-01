using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace WNA.WNAMiscs
{
    public class ProjType_Inviso : Projectile
    {
        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
            ticksToImpact = 1;
            lifetime = 1;
        }
        protected override void TickInterval(int delta)
        {
            lifetime -= delta;
            if (landed) return;
            ticksToImpact -= delta;
            if (ticksToImpact <= 0)
            {
                if (!base.Spawned || base.Map == null) return;
                if (DestinationCell.InBounds(base.Map)) base.Position = DestinationCell;
                ImpactSomething();
                Destroy();
            }
        }
        public override Vector3 ExactPosition => DestinationCell.ToVector3Shifted();
        protected override void DrawAt(Vector3 drawLoc, bool flip = false) { }
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            if (hitThing != null)
            {
                float armorPenetration = base.def.projectile.GetArmorPenetration(base.equipment);
                float baseDamage = base.def.projectile.GetDamageAmount(base.equipment);
                DamageInfo dinfo = new DamageInfo(base.def.projectile.damageDef, baseDamage, armorPenetration, ExactRotation.eulerAngles.y, base.launcher, null, base.equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, base.intendedTarget.Thing);
                hitThing.TakeDamage(dinfo);
                base.def.projectile.soundImpact?.PlayOneShot(new TargetInfo(hitThing.Position, base.Map));
            }
            else
            {
                SoundDef soundImpactGround = base.def.projectile.soundImpactAnticipate ?? base.def.projectile.soundImpact;
                soundImpactGround?.PlayOneShot(new TargetInfo(base.Position, base.Map));
            }
        }
        protected override void ImpactSomething()
        {
            Thing hitThing = null;
            if (usedTarget.HasThing)
            {
                Thing target = usedTarget.Thing;
                if (CanHit(target)) hitThing = target;
                else if (!target.DestroyedOrNull() && target.Spawned) hitThing = target;
            }
            if (hitThing == null)
            {
                List<Thing> thingsInCell = DestinationCell.GetThingList(base.Map);
                for (int i = 0; i < thingsInCell.Count; i++)
                {
                    Thing thing = thingsInCell[i];
                    if (CanHit(thing))
                    {
                        hitThing = thing;
                        break;
                    }
                }
            }
            Impact(hitThing);
        }
    }
}
