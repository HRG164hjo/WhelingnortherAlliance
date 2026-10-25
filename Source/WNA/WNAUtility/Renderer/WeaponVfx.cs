using UnityEngine;
using Verse;
using WNA.WNAModExtension;
using WNA.WNAThingClass.WNAProj;

namespace WNA.WNAUtility.Renderer
{
    internal class WeaponVfx
    {
        private const float MinBeamLength = 0.05f;
        public static void Notify_Launched(ProjBase pj, Map map, Vector3 muzzle, Vector3 impactPoint)
        {
            if (pj == null || map == null)
                return;
            ThingDef weaponDef = pj.EquipmentDef;
            ThingDef projectileDef = pj.def;
            Vector3 a = BeamPoint(muzzle);
            Vector3 b = BeamPoint(impactPoint);
            if ((b - a).MagnitudeHorizontal() < MinBeamLength)
                return;
            WeaponLaserConfig laser = Resolve<WeaponLaserConfig>(weaponDef, projectileDef);
            if (laser != null)
            {
                BeamEffect_Laser laserFx = new BeamEffect_Laser();
                laserFx.Initialize(map, a, b, laser.LaserDuration);
                laserFx.Configure(laser);
                BeamDrawLayer.Spawn(laserFx, map);
            }
            WeaponRadBeamConfig radBeam = Resolve<WeaponRadBeamConfig>(weaponDef, projectileDef);
            if (radBeam != null)
            {
                BeamEffect_RadBeam radFx = new BeamEffect_RadBeam();
                radFx.Initialize(map, a, b, radBeam.RadBeamDuration);
                radFx.Configure(radBeam);
                BeamDrawLayer.Spawn(radFx, map);
            }
            WeaponTeslaBoltConfig tesla = Resolve<WeaponTeslaBoltConfig>(weaponDef, projectileDef);
            if (tesla != null && HasAnyBolt(tesla))
            {
                BeamEffect_TeslaBolt teslaFx = new BeamEffect_TeslaBolt();
                teslaFx.Initialize(map, a, b, tesla.BoltDuration);
                teslaFx.Configure(tesla);
                BeamDrawLayer.Spawn(teslaFx, map);
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

        private static T Resolve<T>(ThingDef weaponDef, ThingDef projectileDef) where T : DefModExtension
        {
            return weaponDef?.GetModExtension<T>() ?? projectileDef?.GetModExtension<T>();
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
