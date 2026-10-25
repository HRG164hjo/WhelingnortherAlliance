using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WNA.WNAThingComp
{
    internal class BackupWeaponEntry
    {
        public ThingDef thingDef = null;
        public float weight = 1f;
    }

    internal class PropBackupWeapon : CompProperties
    {
        public List<BackupWeaponEntry> weapons = [];
        public bool allowPlayerColonist = false;
        public bool allowPlayerSlave = false;
        public bool allowPlayerTooluser = true;

        public PropBackupWeapon()
        {
            compClass = typeof(CompBackupWeapon);
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
                BackupWeaponEntry entry = weapons[i];
                if (entry?.thingDef == null)
                    yield return parentDef.defName + ": weapons[" + i + "].thingDef is null.";
                else if (!entry.thingDef.IsWeapon)
                    yield return parentDef.defName + ": weapons[" + i + "].thingDef (" + entry.thingDef.defName + ") is not a weapon.";
                if (entry != null && entry.weight <= 0f)
                    yield return parentDef.defName + ": weapons[" + i + "].weight must be > 0.";
            }
        }
    }
    internal class CompBackupWeapon : ThingComp
    {
        public PropBackupWeapon Props => (PropBackupWeapon)props;

        public override void Notify_Unequipped(Pawn p)
        {
            base.Notify_Unequipped(p);
            if (!parent.Destroyed)
                return;
            GiveReplacement(p);
        }
        private void GiveReplacement(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed || pawn.Dead || pawn.equipment == null)
                return;
            if (!AllowedForPlayerPawn(pawn))
                return;
            ThingDef weaponDef = PickWeaponDef();
            if (weaponDef == null)
                return;
            if (!(ThingMaker.MakeThing(weaponDef, ResolveStuff(weaponDef)) is ThingWithComps weapon))
                return;
            InheritQuality(weapon);
            pawn.equipment.AddEquipment(weapon);
        }
        private bool AllowedForPlayerPawn(Pawn pawn)
        {
            if (pawn.Faction == null || !pawn.Faction.IsPlayer)
                return true;
            if (pawn.IsSlaveOfColony)
                return Props.allowPlayerSlave;
            if (pawn.RaceProps.Humanlike)
                return Props.allowPlayerColonist;
            return Props.allowPlayerTooluser;
        }
        private ThingDef PickWeaponDef()
        {
            List<BackupWeaponEntry> entries = Props.weapons;
            if (entries.NullOrEmpty())
                return null;
            if (entries.Count == 1)
                return entries[0]?.thingDef;
            return entries.RandomElementByWeight(e => e?.weight ?? 0f)?.thingDef;
        }
        private ThingDef ResolveStuff(ThingDef weaponDef)
        {
            if (!weaponDef.MadeFromStuff)
                return null;
            ThingDef stuff = parent.Stuff;
            if (stuff != null && stuff.stuffProps != null && stuff.stuffProps.CanMake(weaponDef))
                return stuff;
            return GenStuff.DefaultStuffFor(weaponDef);
        }
        private void InheritQuality(ThingWithComps weapon)
        {
            CompQuality newQuality = weapon.TryGetComp<CompQuality>();
            if (newQuality == null)
                return;
            QualityCategory quality = parent.TryGetComp<CompQuality>()?.Quality ?? QualityCategory.Normal;
            newQuality.SetQuality(quality, ArtGenerationContext.Colony);
        }
    }
}
