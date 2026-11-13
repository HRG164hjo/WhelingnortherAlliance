using System.Collections.Generic;
using RimWorld;
using Verse;
using WNA.WNAThingClass.WNAProj;
using WNA.WNAUtility.Renderer;

namespace WNA.WNAVerbType
{
    public class Verb_AbilityShootVfx : Verb_AbilityShoot
    {
        private static readonly HashSet<Def> loggedCasts = new HashSet<Def>();

        protected override bool TryCastShot()
        {
            LocalTargetInfo target = CurrentTarget;
            Def abilityDef = Ability?.def;
            if (abilityDef == null)
                Log.WarningOnce("[WNA] " + GetType().Name + " has no Ability reference; ability VFX config cannot be resolved.", 771208);
            else if (loggedCasts.Add(abilityDef))
                Log.Message("[WNA] Ability shot started: " + abilityDef.defName);
            Def previous = WeaponVfx.CurrentAbilityDef;
            WeaponVfx.CurrentAbilityDef = abilityDef;
            bool cast;
            try
            {
                cast = base.TryCastShot();
            }
            finally
            {
                WeaponVfx.CurrentAbilityDef = previous;
            }
            if (!cast)
            {
                if (abilityDef != null)
                    Log.WarningOnce("[WNA] Ability " + abilityDef.defName + " fired TryCastShot but no shot was made.", 771205);
                return false;
            }
            ThingDef projectileDef = Projectile;
            if (projectileDef == null || caster == null || !caster.Spawned)
            {
                if (abilityDef != null)
                    Log.WarningOnce("[WNA] Ability " + abilityDef.defName + " made a shot without a usable projectile or caster.", 771206);
                return true;
            }
            if (typeof(ProjBase).IsAssignableFrom(projectileDef.thingClass))
                return true;
            WeaponVfx.Notify_Launched(abilityDef, EquipmentSource?.def, projectileDef, caster.Map, caster.DrawPos, target.CenterVector3);
            return true;
        }
    }
}
