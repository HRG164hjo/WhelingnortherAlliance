using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace WNA.WNAThingComp
{
    internal class PropBattleUnitDetonator : CompProperties
    {
        public float triggerRadius = 3.9f;
        public int checkIntervalTicks = 30;
        public bool useEnemyTarget = true;
        public bool ignoreFactionAllies = true;

        public PropBattleUnitDetonator()
        {
            compClass = typeof(CompBattleUnitDetonator);
        }
    }

    internal class CompBattleUnitDetonator : ThingComp
    {
        private static readonly List<Thing> tmpIgnoredThings = new List<Thing>();

        private PropBattleUnitDetonator Props => (PropBattleUnitDetonator)props;

        public override void CompTick()
        {
            if (Props.checkIntervalTicks <= 0 || Find.TickManager.TicksGame % Props.checkIntervalTicks != 0)
                return;
            if (!(parent is Pawn pawn) || !pawn.Spawned || pawn.Map == null)
                return;
            CompExplosive explosive = pawn.TryGetComp<CompExplosive>();
            if (explosive == null || explosive.wickStarted)
                return;
            if (!TryFindTriggerTarget(pawn, out _))
                return;
            if (Props.ignoreFactionAllies && pawn.Faction != null)
                IgnoreNearbyAllies(pawn, explosive);
            explosive.StartWick(pawn);
        }

        // CompExplosive detonates immediately on a lethal hit, before Kill is called,
        // so allies must be registered here to keep that blast faction-safe as well.
        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            base.PostPreApplyDamage(ref dinfo, out absorbed);
            if (absorbed || !Props.ignoreFactionAllies)
                return;
            if (!(parent is Pawn pawn) || !pawn.Spawned || pawn.Map == null || pawn.Faction == null)
                return;
            if (!dinfo.Def.ExternalViolenceFor(parent) || dinfo.Amount < pawn.HitPoints)
                return;
            CompExplosive explosive = pawn.TryGetComp<CompExplosive>();
            if (explosive == null || explosive.wickStarted)
                return;
            IgnoreNearbyAllies(pawn, explosive);
        }

        private bool TryFindTriggerTarget(Pawn pawn, out Thing target)
        {
            target = null;
            if (Props.useEnemyTarget)
            {
                Thing enemyTarget = pawn.mindState?.enemyTarget;
                if (enemyTarget != null && enemyTarget.Spawned
                    && pawn.Position.InHorDistOf(enemyTarget.Position, Props.triggerRadius))
                {
                    target = enemyTarget;
                    return true;
                }
            }
            target = GenClosest.ClosestThingReachable(
                pawn.Position, pawn.Map,
                ThingRequest.ForGroup(ThingRequestGroup.Pawn),
                PathEndMode.OnCell,
                TraverseParms.For(TraverseMode.NoPassClosedDoors),
                Props.triggerRadius,
                t => t is Pawn other && !other.Dead && other.HostileTo(pawn));
            return target != null;
        }

        private void IgnoreNearbyAllies(Pawn pawn, CompExplosive explosive)
        {
            tmpIgnoredThings.Clear();
            float radius = explosive.ExplosiveRadius();
            List<Pawn> allies = pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction);
            for (int i = 0; i < allies.Count; i++)
            {
                Pawn ally = allies[i];
                if (ally != pawn && ally.Position.InHorDistOf(pawn.Position, radius))
                    tmpIgnoredThings.Add(ally);
            }
            if (tmpIgnoredThings.Count > 0)
                explosive.AddThingsIgnoredByExplosion(tmpIgnoredThings);
        }
    }
}
