using RimWorld;
using UnityEngine;
using Verse;
using WNA.WNAModExtension;
using WNA.WNAUtility.Renderer;

namespace WNA.WNAThingClass.WNAProj
{
    internal abstract class ProjBase : Bullet
    {
        private static readonly ProjConfig DefaultConfig = new ProjConfig();
        private ProjConfig cachedConfig;
        private bool configResolved;
        internal ProjConfig Config
        {
            get
            {
                if (!configResolved)
                {
                    cachedConfig = ProjConfig.Get(def);
                    configResolved = true;
                }
                return cachedConfig ?? DefaultConfig;
            }
        }
        internal QualityCategory WeaponQuality => equipmentQuality;
        internal ThingDef CoverDef => targetCoverDef;
        internal Def CastingAbilityDef => WeaponVfx.CurrentAbilityDef ?? (launcher as Pawn)?.CurJob?.ability?.def;
        protected float SpeedPerTick => Mathf.Max(0, 01f, def.projectile.SpeedTilesPerTick);
        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            if (Config.inaccurate)
                usedTarget = new LocalTargetInfo(ScatterAround(launcher, usedTarget.Cell));
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
            PrepareLaunch();
            Map map = base.Map;
            if (map != null)
            {
                WeaponVfx.Notify_Launched(this, map, origin, VfxEndPoint);
            }
            FinishLaunch();
        }
        // Replaces the impact cell the way Verb_LaunchProjectile.GetForcedMissTarget does; intendedTarget
        // still points at the real target, so damage attribution and the battle log are unchanged.
        private IntVec3 ScatterAround(Thing shooter, IntVec3 targetCell)
        {
            Map map = base.Map ?? shooter?.Map;
            if (map == null)
                return targetCell;
            IntVec3 source = shooter != null ? shooter.Position : base.Position;
            float radius = VerbUtility.CalculateAdjustedForcedMiss(Config.inaccurateRange, targetCell - source);
            if (radius <= 0.5f)
                return targetCell;
            int cellsInRadius = GenRadial.NumCellsInRadius(radius);
            if (cellsInRadius <= 1)
                return targetCell;
            return (targetCell + GenRadial.RadialPattern[Rand.Range(0, cellsInRadius)]).ClampInsideMap(map);
        }
        protected virtual Vector3 VfxEndPoint => destination;
        protected virtual void PrepareLaunch()
        {
        }
        protected virtual void FinishLaunch()
        {
        }
    }
}
