using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WNA.WNAThingComp
{
    internal class UnitWeaponEntry
    {
        public ThingDef thingDef = null;
        public float weight = 1f;
    }

    internal class PropUnitWeapon : CompProperties
    {
        public List<UnitWeaponEntry> weapons = [];

        public PropUnitWeapon()
        {
            compClass = typeof(CompUnitWeapon);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string err in base.ConfigErrors(parentDef))
                yield return err;
            if (weapons.NullOrEmpty())
            {
                yield return parentDef.defName + ": weapons list is empty.";
                yield break;
            }
            for (int i = 0; i < weapons.Count; i++)
            {
                UnitWeaponEntry entry = weapons[i];
                if (entry?.thingDef == null)
                    yield return parentDef.defName + ": weapons[" + i + "].thingDef is null.";
                else if (!entry.thingDef.IsWeapon)
                    yield return parentDef.defName + ": weapons[" + i + "].thingDef (" + entry.thingDef.defName + ") is not a weapon.";
                if (entry != null && entry.weight <= 0f)
                    yield return parentDef.defName + ": weapons[" + i + "].weight must be > 0.";
            }
        }
    }

    internal class CompUnitWeapon : ThingComp
    {
        public PropUnitWeapon Props => (PropUnitWeapon)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            EnsureWeapon();
        }

        public override void CompTick()
        {
            if (parent.IsHashIntervalTick(60))
                EnsureWeapon();
        }

        private void EnsureWeapon()
        {
            if (!(parent is Pawn pawn) || pawn.Dead || pawn.Destroyed || pawn.health == null || pawn.health.isBeingKilled)
                return;
            if (pawn.equipment == null || pawn.equipment.Primary != null)
                return;
            ThingDef weaponDef = PickWeaponDef();
            if (weaponDef == null)
                return;
            if (!(ThingMaker.MakeThing(weaponDef, ResolveStuff(weaponDef)) is ThingWithComps weapon))
                return;
            pawn.equipment.AddEquipment(weapon);
        }

        private ThingDef PickWeaponDef()
        {
            List<UnitWeaponEntry> entries = Props.weapons;
            if (entries.NullOrEmpty())
                return null;
            if (entries.Count == 1)
                return entries[0]?.thingDef;
            return entries.RandomElementByWeight(e => e?.weight ?? 0f)?.thingDef;
        }

        private static ThingDef ResolveStuff(ThingDef weaponDef)
        {
            return weaponDef.MadeFromStuff ? GenStuff.DefaultStuffFor(weaponDef) : null;
        }
    }
}
