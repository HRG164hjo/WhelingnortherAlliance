using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using WNA.WNAMiscs;
using WNA.WNAModExtension;
using WNA.WNAUtility;

namespace WNA.WNAThingClass.WNAProj
{
    internal sealed class Proj_Complex : ProjBase
    {
        private int ticksSinceCorrection;
        private int ticksAlive;
        private IntVec3 lastKnownTargetCell = IntVec3.Invalid;
        private Thing forcedTarget;
        private ProjFlightMode FlightMode => Config.flightMode;
        public override Vector3 ExactPosition => FlightMode == ProjFlightMode.Instant ? destination + Vector3.up * def.Altitude : base.ExactPosition;
        public override Quaternion ExactRotation
        {
            get
            {
                Vector3 dir = (destination - origin).Yto0();
                if (dir.sqrMagnitude < 1E-06f)
                    return Quaternion.identity;
                if (FlightMode == ProjFlightMode.Arcing)
                {
                    // Projectile.DrawAt lifts the sprite along world +Z by ArcHeightFactor * InverseParabola(t);
                    // 4f * (1f - 2f * t) is that curve's derivative, so the sprite rides the tangent.
                    dir.z += ArcHeightFactor * 4f * (1f - 2f * DistanceCoveredFractionArc);
                }
                return Quaternion.LookRotation(dir);
            }
        }
        // Mirrors Projectile's private clamp so the attitude matches the arc vanilla draws in DrawAt.
        private float ArcHeightFactor
        {
            get
            {
                float height = def.projectile.arcHeightFactor;
                float horizontalDistSq = (destination - origin).MagnitudeHorizontalSquared();
                if (height * height > horizontalDistSq * 0.04f)
                    height = Mathf.Sqrt(horizontalDistSq) * 0.2f;
                return height;
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksSinceCorrection, "ticksSinceCorrection", 0);
            Scribe_Values.Look(ref ticksAlive, "ticksAlive", 0);
            Scribe_Values.Look(ref lastKnownTargetCell, "lastKnownTargetCell", IntVec3.Invalid);
        }
        protected override void PrepareLaunch()
        {
            switch (FlightMode)
            {
                case ProjFlightMode.Instant:
                    PrepareInstantLaunch();
                    break;
                case ProjFlightMode.Arcing:
                    if (def.projectile.arcHeightFactor <= 0f)
                        Log.WarningOnce($"WNA: {def.defName} uses flightMode Arcing but projectile.arcHeightFactor is 0, so it flies flat.", 40251312);
                    break;
                case ProjFlightMode.Homing:
                    ticksSinceCorrection = 0;
                    ticksAlive = 0;
                    RememberTargetCell();
                    break;
            }
        }
        protected override void FinishLaunch()
        {
            if (FlightMode == ProjFlightMode.Instant)
            {
                ImpactSomething();
            }
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
        protected override void TickInterval(int delta)
        {
            if (FlightMode != ProjFlightMode.Homing)
            {
                base.TickInterval(delta);
                return;
            }
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
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            IntVec3 position = base.Position;
            Vector3 impactPoint = hitThing != null ? hitThing.DrawPos : ExactPosition;
            List<ProjWarheadConfig> warheads = Config.warheads;
            int count = warheads?.Count ?? 0;
            WNAProjDamageContext baseCtx = count > 0 ? WNAProjDamageContext.From(this) : null;
            bool[] triggered = null;
            List<Thing>[] ambientTargets = null;
            if (count > 0)
            {
                triggered = new bool[count];
                for (int i = 0; i < count; i++)
                {
                    ProjWarheadConfig warhead = warheads[i];
                    if (!CanApply(warhead, baseCtx))
                    {
                        continue;
                    }
                    if (warhead.chance < 1f && !Rand.Chance(warhead.chance))
                    {
                        continue;
                    }
                    triggered[i] = true;
                    if (warhead.type == ProjWarheadType.Ambient && map != null)
                    {
                        if (ambientTargets == null)
                        {
                            ambientTargets = new List<Thing>[count];
                        }
                        List<Thing> buffer = new List<Thing>(16);
                        Vector3 from = FlightMode == ProjFlightMode.Instant && Launcher != null ? Launcher.DrawPos : origin;
                        WNAProjCombatUtility.CollectLineTargets(map, from, impactPoint, baseCtx.instigator, warhead.affectsDirectTarget ? null : hitThing, warhead.affectNonPawns, warhead.width, warhead.maxTargets, buffer, Config.affectsAllies);
                        ambientTargets[i] = buffer;
                    }
                }
            }
            base.Impact(hitThing, blockedByShield);
            if (map == null)
            {
                return;
            }
            if (Config.explosive)
            {
                DoExplosion(map, position);
                DoExtraExplosions(map, position);
            }
            if (count == 0)
            {
                return;
            }
            ThingDef projectileDef = def;
            ThingDef coverDef = CoverDef;
            for (int i = 0; i < count; i++)
            {
                if (!triggered[i])
                {
                    continue;
                }
                ProjWarheadConfig warhead = warheads[i];
                WNAProjDamageContext ctx = baseCtx.WithWarhead(warhead);
                if (ctx.damageDef == null || ctx.amount <= 0f)
                {
                    continue;
                }
                switch (warhead.type)
                {
                    case ProjWarheadType.Ambient:
                        ApplyAmbient(ambientTargets?[i], ctx, projectileDef, coverDef);
                        break;
                    case ProjWarheadType.CellSpread:
                        WNAProjCombatUtility.ApplyInRadius(map, position, warhead.radius, ctx, projectileDef, coverDef, warhead.requireLineOfSight, warhead.percentAtMax, exclude: warhead.affectsDirectTarget ? null : hitThing, affectNonPawns: warhead.affectNonPawns, affectsAllies: Config.affectsAllies);
                        break;
                    case ProjWarheadType.Cluster:
                        WNAProjCombatUtility.ApplyClusters(map, position, warhead.radius, warhead.amount, ctx, projectileDef, coverDef, warhead.requireLineOfSight, affectNonPawns: warhead.affectNonPawns, exclude: warhead.affectsDirectTarget ? null : hitThing, fleck: warhead.fleck ?? FleckDefOf.MicroSparks, affectsAllies: Config.affectsAllies);
                        break;
                    case ProjWarheadType.DirectExtra:
                        if (hitThing != null && WNAProjCombatUtility.IsValidTarget(hitThing, map, ctx.instigator, warhead.affectNonPawns, Config.affectsAllies))
                        {
                            WNAProjCombatUtility.ApplyToThing(hitThing, ctx, 1f, projectileDef, coverDef);
                        }
                        break;
                }
            }
        }
        private void DoExplosion(Map map, IntVec3 position)
        {
            ProjectileProperties props = def.projectile;
            if (props.explosionRadius <= 0f)
            {
                return;
            }
            if (props.explosionDelay > 0)
            {
                Log.WarningOnce("WNA: Proj_Complex does not support projectile.explosionDelay; the projectile explodes immediately.", 40251311);
            }
            if (props.explosionEffect != null)
            {
                Effecter effecter = props.explosionEffect.Spawn();
                if (props.explosionEffectLifetimeTicks != 0)
                {
                    map.effecterMaintainer.AddEffecterToMaintain(effecter, position, props.explosionEffectLifetimeTicks);
                }
                else
                {
                    effecter.Trigger(new TargetInfo(position, map), new TargetInfo(position, map));
                    effecter.Cleanup();
                }
            }
            float? direction = origin.AngleToFlat(destination);
            GenExplosion.DoExplosion(position, map, props.explosionRadius, DamageDef, Launcher, DamageAmount, ArmorPenetration, props.soundExplode, equipmentDef, def, intendedTarget.Thing, props.postExplosionSpawnThingDef ?? (props.explosionSpawnsSingleFilth ? null : props.filth), props.postExplosionSpawnChance, props.postExplosionSpawnThingCount, props.postExplosionGasType, null, 255, props.applyDamageToExplosionCellsNeighbors, props.preExplosionSpawnThingDef, props.preExplosionSpawnChance, props.preExplosionSpawnThingCount, props.explosionChanceToStartFire, props.explosionDamageFalloff, direction, null, null, props.doExplosionVFX, DamageDef.expolosionPropagationSpeed, 0f, doSoundEffects: true, props.postExplosionSpawnThingDefWater, props.screenShakeFactor, null, null, props.postExplosionSpawnSingleThingDef, props.preExplosionSpawnSingleThingDef);
            if (props.explosionSpawnsSingleFilth && props.filth != null && props.filthCount.TrueMax > 0 && Rand.Chance(props.filthChance) && !position.Filled(map))
            {
                FilthMaker.TryMakeFilth(position, map, props.filth, props.filthCount.RandomInRange);
            }
        }
        private void DoExtraExplosions(Map map, IntVec3 position)
        {
            List<ProjExtraExplosionConfig> extras = Config.extraExplosions;
            if (extras == null)
            {
                return;
            }
            for (int e = 0; e < extras.Count; e++)
            {
                ProjExtraExplosionConfig extra = extras[e];
                if (extra == null || extra.count <= 0 || extra.radius <= 0f)
                {
                    continue;
                }
                DamageDef damageDef = extra.damageDef ?? DamageDef;
                if (damageDef == null)
                {
                    continue;
                }
                CellRect rect = CellRect.CenteredOn(position, Mathf.Max(0, Mathf.RoundToInt(extra.areaSize)));
                rect.ClipInsideMap(map);
                int amount = Mathf.RoundToInt(DamageAmount * extra.damageAmountFactor);
                for (int i = 0; i < extra.count; i++)
                {
                    IntVec3 cell = rect.IsEmpty ? position : rect.RandomCell;
                    GenExplosion.DoExplosion(cell, map, extra.radius, damageDef, Launcher, amount, ArmorPenetration, extra.soundExplode, equipmentDef, def, intendedTarget.Thing, chanceToStartFire: extra.chanceToStartFire);
                }
            }
        }
        private static bool CanApply(ProjWarheadConfig warhead, WNAProjDamageContext baseCtx)
        {
            if (warhead == null || warhead.chance <= 0f || warhead.damageFactor <= 0f || baseCtx.amount <= 0f)
            {
                return false;
            }
            if (warhead.damageDef == null && baseCtx.damageDef == null)
            {
                return false;
            }
            switch (warhead.type)
            {
                case ProjWarheadType.CellSpread:
                    return warhead.radius > 0f;
                case ProjWarheadType.Cluster:
                    return warhead.amount > 0 && warhead.radius >= 0f;
                default:
                    return true;
            }
        }
        private static void ApplyAmbient(List<Thing> targets, in WNAProjDamageContext ctx, ThingDef projectileDef, ThingDef coverDef)
        {
            if (targets == null)
            {
                return;
            }
            for (int i = 0; i < targets.Count; i++)
            {
                WNAProjCombatUtility.ApplyToThing(targets[i], ctx, 1f, projectileDef, coverDef);
            }
        }
        private void PrepareInstantLaunch()
        {
            Map map = base.Map;
            forcedTarget = Config.invisoHit ? ResolveForcedTarget(map) : null;
            if (forcedTarget != null)
                destination = forcedTarget.DrawPos.Yto0();
            IntVec3 cell = DestinationCell;
            if (map != null && cell.InBounds(map))
                base.Position = cell;
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
