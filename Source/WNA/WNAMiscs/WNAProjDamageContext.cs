using RimWorld;
using Verse;
using WNA.WNAModExtension;
using WNA.WNAThingClass.WNAProj;

namespace WNA.WNAMiscs
{
    internal class WNAProjDamageContext
    {
        public DamageDef damageDef;
        public float amount;
        public float armorPenetration;
        public float angle;
        public Thing instigator;
        public ThingDef weaponDef;
        public Thing intendedTarget;
        public QualityCategory weaponQuality;
        public bool instigatorGuilty;
        public static WNAProjDamageContext From(ProjBase projectile)
        {
            Thing launcher = projectile.Launcher;
            bool guilty = !(launcher is Pawn pawn) || !pawn.Drafted;
            return new WNAProjDamageContext
            {
                damageDef = projectile.DamageDef,
                amount = projectile.DamageAmount,
                armorPenetration = projectile.ArmorPenetration,
                angle = projectile.ExactRotation.eulerAngles.y,
                instigator = launcher,
                weaponDef = projectile.EquipmentDef,
                intendedTarget = projectile.intendedTarget.Thing,
                weaponQuality = projectile.WeaponQuality,
                instigatorGuilty = guilty
            };
        }
        public WNAProjDamageContext WithWarhead(ProjWarheadConfig warhead)
        {
            return new WNAProjDamageContext
            {
                damageDef = warhead.damageDef ?? damageDef,
                amount = amount * warhead.damageFactor,
                armorPenetration = warhead.armorPenetration >= 0f ? warhead.armorPenetration : armorPenetration * warhead.armorPenetrationFactor,
                angle = angle,
                instigator = instigator,
                weaponDef = weaponDef,
                intendedTarget = intendedTarget,
                weaponQuality = weaponQuality,
                instigatorGuilty = instigatorGuilty
            };
        }
        public DamageInfo ToDamageInfo(float amountFactor = 1f)
        {
            DamageInfo dinfo = new DamageInfo(damageDef, amount * amountFactor, armorPenetration, angle, instigator, null, weaponDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget, instigatorGuilty);
            dinfo.SetWeaponQuality(weaponQuality);
            return dinfo;
        }
        public BattleLogEntry_RangedImpact MakeBattleLog(Thing recipient, ThingDef projectileDef, ThingDef coverDef)
        {
            return new BattleLogEntry_RangedImpact(instigator, recipient, intendedTarget, weaponDef, projectileDef, coverDef);
        }
    }
}
