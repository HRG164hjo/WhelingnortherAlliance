using System.Collections.Generic;
using UnityEngine;
using Verse;
using WNA.WNAModExtension;
using WNA.WNAThingClass.WNAProj;

namespace WNA.WNAUtility.Renderer
{
    internal class WeaponVfx
    {
        private const float MinBeamLength = 0.05f;
        private static readonly HashSet<Def> loggedAbilities = new HashSet<Def>();
        // Ability verbs push their def here while the shot spawns its projectile. Projectiles read
        // it before falling back to Pawn.CurJob, which is already cleared once the cast job ends.
        internal static Def CurrentAbilityDef { get; set; }
        public static void Notify_Launched(ProjBase pj, Map map, Vector3 muzzle, Vector3 impactPoint)
        {
            if (pj == null)
                return;
            Notify_Launched(pj.CastingAbilityDef, pj.EquipmentDef, pj.def, map, muzzle, impactPoint);
        }
        public static void Notify_Launched(Def abilityDef, ThingDef weaponDef, ThingDef projectileDef, Map map, Vector3 muzzle, Vector3 impactPoint)
        {
            if (map == null)
                return;
            Vector3 a = BeamPoint(muzzle);
            Vector3 b = BeamPoint(impactPoint);
            float length = (b - a).MagnitudeHorizontal();
            WeaponLaserConfig laser = Resolve<WeaponLaserConfig>(abilityDef, weaponDef, projectileDef);
            WeaponRadBeamConfig radBeam = Resolve<WeaponRadBeamConfig>(abilityDef, weaponDef, projectileDef);
            WeaponTeslaBoltConfig tesla = Resolve<WeaponTeslaBoltConfig>(abilityDef, weaponDef, projectileDef);
            bool teslaVisible = tesla != null && HasAnyBolt(tesla);
            if (length >= MinBeamLength)
            {
                if (laser != null)
                {
                    BeamEffect_Laser laserFx = new BeamEffect_Laser();
                    laserFx.Initialize(map, a, b, laser.LaserDuration);
                    laserFx.Configure(laser);
                    BeamDrawLayer.Spawn(laserFx, map);
                }
                if (radBeam != null)
                {
                    BeamEffect_RadBeam radFx = new BeamEffect_RadBeam();
                    radFx.Initialize(map, a, b, radBeam.RadBeamDuration);
                    radFx.Configure(radBeam);
                    BeamDrawLayer.Spawn(radFx, map);
                }
                if (teslaVisible)
                {
                    BeamEffect_TeslaBolt teslaFx = new BeamEffect_TeslaBolt();
                    teslaFx.Initialize(map, a, b, tesla.BoltDuration);
                    teslaFx.Configure(tesla);
                    BeamDrawLayer.Spawn(teslaFx, map);
                }
            }
            if (abilityDef != null && loggedAbilities.Add(abilityDef))
            {
                Log.Message("[WNA] Ability VFX: ability=" + abilityDef.defName
                    + " weapon=" + (weaponDef?.defName ?? "none")
                    + " projectile=" + (projectileDef?.defName ?? "none")
                    + " laser=" + (laser != null)
                    + " rad=" + (radBeam != null)
                    + " tesla=" + teslaVisible
                    + " length=" + length.ToString("F2")
                    + " layer=" + (BeamDrawLayer.For(map) != null));
            }
        }
        public static void SpawnBeam<T>(Map map, Vector3 from, Vector3 to, int durationTicks) where T : BeamEffect, new()
        {
            if (map == null)
                return;
            Vector3 a = BeamPoint(from);
            Vector3 b = BeamPoint(to);
            if ((b - a).MagnitudeHorizontal() < MinBeamLength)
                return;
            T effect = new T();
            effect.Initialize(map, a, b, durationTicks);
            BeamDrawLayer.Spawn(effect, map);
        }

        private static T Resolve<T>(Def abilityDef, ThingDef weaponDef, ThingDef projectileDef) where T : DefModExtension
        {
            return abilityDef?.GetModExtension<T>() ?? weaponDef?.GetModExtension<T>() ?? projectileDef?.GetModExtension<T>();
        }

        private static bool HasAnyBolt(WeaponTeslaBoltConfig config)
        {
            for (int i = 0; i < WeaponTeslaBoltConfig.BoltCount; i++)
            {
                if (config.IsBoltEnabled(i))
                    return true;
            }
            return false;
        }
        private static Vector3 BeamPoint(Vector3 v)
        {
            return new Vector3(v.x, AltitudeLayer.MoteOverhead.AltitudeFor(), v.z);
        }
    }
}
