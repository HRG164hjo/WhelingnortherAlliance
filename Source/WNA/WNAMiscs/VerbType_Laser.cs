using UnityEngine;
using Verse;
using RimWorld;
using WNA.ThingClass;
using WNA.DMExtension;

namespace WNA.WNAMiscs
{
    public class VerbType_Laser : Verb_Shoot
    {
        public override float? AimAngleOverride
        {
            get
            {
                if (state != VerbState.Bursting)
                    return null;
                Vector3 Vdir = currentTarget.CenterVector3 - caster.DrawPos;
                return Vdir.AngleFlat();
            }
        }
        public override void WarmupComplete()
        {
            base.WarmupComplete();
            Map map = Caster.Map;
            Vector3 start = caster.DrawPos;
            Vector3 targetPos = currentTarget.CenterVector3;
            IntVec3 targetCell = targetPos.ToIntVec3();
            IntVec3 lastVisibleCell = GenSight.LastPointOnLineOfSight(
                caster.Position,
                targetCell,
                (IntVec3 c) => c.CanBeSeenOverFast(map),
                skipFirstCell: true
            );
            Vector3 end = targetPos;
            if (lastVisibleCell.IsValid && lastVisibleCell != targetCell)
                end = lastVisibleCell.ToVector3Shifted();
            Color cIn = new Color(1f, 0.25f, 0.25f, 1f);
            Color cOut = cIn;
            float spr = 1.2f;
            float width = 0.2f;
            int dura = 30;
            int dura2 = Mathf.RoundToInt(dura * spr);
            if (EquipmentSource != null)
            {
                var cfg = TechnoConfig.Get(EquipmentSource.def);
                if (cfg != null)
                {
                    if (cfg.laserInnerColor.HasValue)
                    {
                        Vector4 ic = cfg.laserInnerColor.Value / 255f;
                        cIn = new Color(ic.x, ic.y, ic.z, ic.w);
                    }
                    if (cfg.laserOuterColor.HasValue)
                    {
                        Vector4 oc = cfg.laserOuterColor.Value / 255f;
                        cOut = new Color(oc.x, oc.y, oc.z, oc.w);
                    }
                    else cOut = cIn;
                    if (cfg.laserOuterSpread.HasValue)
                        spr = cfg.laserOuterSpread.Value;

                    if (cfg.laserThickness.HasValue)
                        width = cfg.laserThickness.Value;

                    if (cfg.laserDuration.HasValue)
                        dura = cfg.laserDuration.Value;
                    dura2 = Mathf.RoundToInt(dura * spr);
                }
            }
            DummyLaser beam = (DummyLaser)ThingMaker.MakeThing(ThingDef.Named("WNA_LaserBeam"));
            beam.start = start;
            beam.end = end;
            beam.cIn = cIn;
            beam.cOut = cOut;
            beam.width = width;
            beam.dura = dura;
            beam.spr = spr;
            beam.dura2 = dura2;
            GenSpawn.Spawn(beam, caster.Position, map);
            FleckMaker.Static(end, map, FleckDefOf.ExplosionFlash, 1f);
        }
    }
}