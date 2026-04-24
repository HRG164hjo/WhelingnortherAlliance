using RimWorld;
using UnityEngine;
using Verse;

namespace WNA.WNAVerbType
{
    public class VerbType_SetOffExplosion : Verb_Shoot
    {
        protected override bool TryCastShot()
        {
            TryPreExplosionAtTarget();
            return base.TryCastShot();
        }
        private void TryPreExplosionAtTarget()
        {
            if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map)
                return;
            ThingDef projectileDef = Projectile;
            if (projectileDef?.projectile == null)
                return;
            ProjectileProperties proj = projectileDef.projectile;
            float radius = Mathf.Max(proj.explosionRadius, 0.9f);
            IntVec3 center = currentTarget.HasThing ? currentTarget.Thing.Position : currentTarget.Cell;
            Map map = caster.Map;
            if (map == null || !center.InBounds(map))
                return;
            GenExplosion.DoExplosion(
                center: center,
                map: map,
                radius: radius,
                damType: proj.damageDef ?? DamageDefOf.Bomb,
                instigator: caster,
                damAmount: proj.GetDamageAmount(EquipmentSource),
                armorPenetration: proj.GetArmorPenetration(EquipmentSource),
                weapon: EquipmentSource?.def,
                projectile: projectileDef,
                intendedTarget: currentTarget.Thing,
                preExplosionSpawnThingDef: proj.preExplosionSpawnThingDef,
                preExplosionSpawnChance: proj.preExplosionSpawnChance,
                postExplosionSpawnThingDef: proj.postExplosionSpawnThingDef,
                postExplosionSpawnChance: proj.postExplosionSpawnChance
            );
        }
    }
}
