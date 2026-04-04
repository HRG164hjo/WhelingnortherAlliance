using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace WNA.HediffCompProp
{
    public class PropProximityBlink : HediffCompProperties
    {
        public float range = 6.9f;
        public int cooldown = 23;
        public FloatRange skipaway = new FloatRange(19.9f, 44.9f);
        public bool counterBack = false;
        public DamageDef damageDef;
        public float power = 50000f;
        public float ap = float.MaxValue;
        public PropProximityBlink()
        {  
            compClass = typeof(CompProximityBlink);
        }
    }
    public class CompProximityBlink : HediffComp
    {
        private PropProximityBlink Props => (PropProximityBlink)props;
        private int cd = 0;
        public override void CompPostTick(ref float severityAdjustment)
        {
            cd--;
            if (cd<=0 && CanBlinkNow())
            {
                Pawn trigger = FindTrigger();
                if (trigger != null)
                {
                    Blink(trigger);
                    cd = Props.cooldown;
                }
            }
        }
        private bool CanBlinkNow()
        {
            if (Pawn.Spawned && !Pawn.DeadOrDowned && !Pawn.Drafted)
                return true;
            return false;
        }
        private Pawn FindTrigger()
        {
            List<IAttackTarget> targets = Pawn.Map.attackTargetsCache.GetPotentialTargetsFor(Pawn);
            Pawn best = null;
            float bestDist = float.MaxValue;
            foreach (IAttackTarget t in targets)
            {
                if (t is Pawn enemy &&
                    enemy.Spawned &&
                    !enemy.DeadOrDowned &&
                    Pawn.Position.InHorDistOf(enemy.Position, Props.range))
                {
                    float d = (enemy.Position - Pawn.Position).LengthHorizontalSquared;
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = enemy;
                    }
                }
            }
            return best;
        }
        private void Blink(Pawn trigger)
        {
            IntVec3 positionHeld = Pawn.PositionHeld;
            Map mapHeld = Pawn.MapHeld;
            positionHeld = FindCellToSpawn(positionHeld, mapHeld);
            Pawn.Position = positionHeld;
            Pawn.Notify_Teleported();
            float powerfinal = Props.power * Pawn.BodySize * Mathf.Sqrt(Pawn.skills.GetSkill(SkillDefOf.Melee).Level) * Pawn.GetStatValue(StatDefOf.MeleeDamageFactor);
            if (Props.counterBack && trigger != null && trigger.Spawned && !trigger.Dead)
            {
                DamageDef def = Props.damageDef ?? DamageDefOf.Crush;
                DamageInfo dinfo = new DamageInfo(
                    def,
                    Props.power,
                    0f,
                    -1f,
                    Pawn,
                    null,
                    null,
                    DamageInfo.SourceCategory.ThingOrUnknown,
                    trigger,
                    instigatorGuilty: false,
                    spawnFilth: false
                );
                trigger.TakeDamage(dinfo);
            }
        }
        private IntVec3 FindCellToSpawn(IntVec3 origin, Map map)
        {
            if (Props.skipaway.max > 0)
            {
                int maxExclusive = GenRadial.NumCellsInRadius(Props.skipaway.max);
                int num = GenRadial.NumCellsInRadius(Props.skipaway.min);
                for (int i = 0; i < 100; i++)
                {
                    IntVec3 intVec = origin + GenRadial.RadialPattern[Rand.Range(num, maxExclusive)];
                    if (intVec.InBounds(map) &&
                        intVec.WalkableBy(map, Pawn) &&
                        map.reachability.CanReach(intVec, origin, PathEndMode.OnCell, TraverseParms.For(Pawn)))
                        return intVec;
                }
                for (int j = 0; j < 100; j++)
                {
                    IntVec3 intVec2 = origin + GenRadial.RadialPattern[Rand.Range(1, num)];
                    if (intVec2.InBounds(map) &&
                        intVec2.WalkableBy(map, Pawn) &&
                        map.reachability.CanReach(intVec2, origin, PathEndMode.OnCell, TraverseParms.For(Pawn)))
                        return intVec2;
                }
            }
            return origin;
        }
    }
}
