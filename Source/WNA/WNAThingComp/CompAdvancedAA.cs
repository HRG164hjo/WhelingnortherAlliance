using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.Sound;
using WNA.WNAUtility;

namespace WNA.WNAThingComp
{
    internal class PropAdvancedAA : CompProperties
    {
        public float range = 30f;
        public int scanIntervalTicks = 60;
        public int cooldownTicks = 90;
        public DamageDef damageDef = null;
        public int damage = 20;
        public float armorPenetration = 0f;
        public EffecterDef fireEffecter = null;
        public SoundDef fireSound = null;
        public bool AdvancedAA = false;
        public bool AffectsHostile = true;
        public bool AffectsNeutral = false;
        public bool AffectsAllies = false;

        public PropAdvancedAA()
        {
            compClass = typeof(CompAdvancedAA);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
                yield return error;
            if (range <= 0f)
                yield return parentDef.defName + " CompProperties_AntiAir has a non-positive range.";
            if (scanIntervalTicks <= 0)
                yield return parentDef.defName + " CompProperties_AntiAir has a non-positive scanIntervalTicks.";
            if (cooldownTicks < 0)
                yield return parentDef.defName + " CompProperties_AntiAir has a negative cooldownTicks.";
            if (!AdvancedAA && damage <= 0)
                yield return parentDef.defName + " CompProperties_AntiAir requires a positive damage when AdvancedAA is disabled.";
        }
    }
    internal class CompAdvancedAA : ThingComp
    {
        private Thing currentAircraft;
        private Thing currentAttackTarget;
        private bool currentAttacksShell;
        private int nextScanTick;
        private int nextFireTick;
        private static readonly List<Thing> tmpPayload = new List<Thing>();
        private static readonly List<Thing> tmpDroppable = new List<Thing>();
        public PropAdvancedAA Props => (PropAdvancedAA)props;
        private Map Map => parent.Map;
        public override void CompTick()
        {
            base.CompTick();
            Map map = Map;
            if (map == null || !parent.Spawned)
                return;
            int now = Find.TickManager.TicksGame;
            if (now >= nextScanTick)
            {
                nextScanTick = now + Props.scanIntervalTicks;
                AcquireTarget(map);
            }
            if (IsCurrentTargetValid(map) && now >= nextFireTick)
            {
                nextFireTick = now + Props.cooldownTicks;
                FireAtTarget(map);
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref currentAircraft, "currentAircraft");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                currentAttackTarget = null;
                currentAttacksShell = false;
                nextScanTick = 0;
                nextFireTick = 0;
            }
        }
        public override string CompInspectStringExtra()
        {
            string state = currentAircraft != null && !currentAircraft.Destroyed
                ? "HRG_AdvancedAA_Tracking".Translate(currentAircraft.LabelShort)
                : "HRG_AdvancedAA_Idle".Translate();
            if (Props.AdvancedAA)
                state = state + "\n" + "HRG_AdvancedAA_AdvancedMode".Translate();   
            return state;
        }
        private void AcquireTarget(Map map)
        {
            if (IsCurrentTargetValid(map) && TryResolveTarget(currentAircraft, out Thing refreshed, out bool refreshedShell))
            {
                currentAttackTarget = refreshed;
                currentAttacksShell = refreshedShell;
                return;
            }
            Thing bestAircraft = null;
            Thing bestTarget = null;
            bool bestShell = false;
            int bestDistSq = int.MaxValue;
            foreach (Thing candidate in GenRadial.RadialDistinctThingsAround(parent.Position, map, Props.range, true))
            {
                if (!WNAAdvancedAAUtility.IsAircraft(candidate))
                    continue;
                if (!parent.Position.InHorDistOf(candidate.Position, Props.range))
                    continue;
                if (!TryResolveTarget(candidate, out Thing target, out bool shell))
                    continue;
                int distSq = (candidate.Position - parent.Position).LengthHorizontalSquared;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestAircraft = candidate;
                    bestTarget = target;
                    bestShell = shell;
                }
            }

            currentAircraft = bestAircraft;
            currentAttackTarget = bestTarget;
            currentAttacksShell = bestShell;
        }
        private bool TryResolveTarget(Thing aircraft, out Thing attackTarget, out bool attacksShell)
        {
            attackTarget = null;
            attacksShell = false;
            if (aircraft == null || aircraft.Destroyed || !aircraft.Spawned)
                return false;
            WNAAdvancedAAUtility.CollectPayload(aircraft, tmpPayload);
            if (!PassesFactionFilter(aircraft))
                return false;
            if (Props.AdvancedAA)
            {
                attackTarget = aircraft;
                attacksShell = true;
                return true;
            }
            Thing content = WNAAdvancedAAUtility.FindDestructibleContent(tmpPayload, out _);
            if (content != null)
            {
                attackTarget = content;
                attacksShell = false;
                return true;
            }
            if (aircraft.def != null && aircraft.def.useHitPoints)
            {
                attackTarget = aircraft;
                attacksShell = true;
                return true;
            }
            return false;
        }
        private bool PassesFactionFilter(Thing aircraft)
        {
            Faction targetFaction = WNAAdvancedAAUtility.ResolveTargetFaction(aircraft, tmpPayload);
            if (targetFaction == null)
                return true;
            Faction myFaction = parent.Faction;
            if (myFaction == null)
                return true;
            if (targetFaction == myFaction)
                return Props.AffectsAllies;
            switch (myFaction.RelationKindWith(targetFaction))
            {
                case FactionRelationKind.Hostile:
                    return Props.AffectsHostile;
                case FactionRelationKind.Ally:
                    return Props.AffectsAllies;
                default:
                    return Props.AffectsNeutral;
            }
        }
        private bool IsCurrentTargetValid(Map map)
        {
            Thing aircraft = currentAircraft;
            if (aircraft == null || aircraft.Destroyed || !aircraft.Spawned)
                return false;
            if (aircraft.Map != map)
                return false;
            return parent.Position.InHorDistOf(aircraft.Position, Props.range);
        }
        private void FireAtTarget(Map map)
        {
            Thing aircraft = currentAircraft;
            if (aircraft == null || aircraft.Destroyed)
                return;
            if (Props.fireSound != null)
                Props.fireSound.PlayOneShot(SoundInfo.InMap(new TargetInfo(parent.Position, map)));
            if (Props.fireEffecter != null)
                Props.fireEffecter.Spawn().Trigger(new TargetInfo(parent.Position, map), new TargetInfo(aircraft));
            if (Props.AdvancedAA)
            {
                DestroyAircraftAndDropContent(aircraft);
                return;
            }
            DamageDef damageDef = Props.damageDef ?? DamageDefOf.Bullet;
            DamageInfo dinfo = new DamageInfo(damageDef, Props.damage, Props.armorPenetration, -1f, parent, null, parent.def);
            if (currentAttacksShell)
            {
                if (aircraft.def != null && aircraft.def.useHitPoints && aircraft.HitPoints <= Props.damage)
                {
                    DestroyAircraftAndDropContent(aircraft);
                    return;
                }
                if (!aircraft.Destroyed)
                    aircraft.TakeDamage(dinfo);
                return;
            }
            Thing target = currentAttackTarget;
            if (target != null && !target.Destroyed && !(target is Pawn alive && alive.Dead))
                target.TakeDamage(dinfo);
            if (target == null || target.Destroyed || (target is Pawn pawn && pawn.Dead))
                DestroyAircraftAndDropContent(aircraft);
        }
        private void DestroyAircraftAndDropContent(Thing aircraft)
        {
            if (aircraft == null || aircraft.Destroyed)
                return;
            Map map = aircraft.Map;
            if (map == null)
            {
                aircraft.Destroy(DestroyMode.Vanish);
                return;
            }
            IntVec3 dropPos = aircraft.Spawned ? aircraft.Position : aircraft.PositionHeld;
            WNAAdvancedAAUtility.CollectPayload(aircraft, tmpDroppable);
            for (int i = 0; i < tmpDroppable.Count; i++)
            {
                Thing thing = tmpDroppable[i];
                if (thing == null || thing.Destroyed)
                    continue;
                if (thing.holdingOwner != null)
                    thing.holdingOwner.Remove(thing);
                if (!GenPlace.TryPlaceThing(thing, dropPos, map, ThingPlaceMode.Near))
                    thing.Destroy();
            }
            tmpDroppable.Clear();
            if (!aircraft.Destroyed)
                aircraft.Destroy(DestroyMode.Vanish);
        }
    }
}
