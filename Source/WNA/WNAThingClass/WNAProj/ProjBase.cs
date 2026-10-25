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
        protected float SpeedPerTick => Mathf.Max(0, 01f, def.projectile.SpeedTilesPerTick);
        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
            PrepareLaunch();
            Map map = base.Map;
            if (map != null)
            {
                WeaponVfx.Notify_Launched(this, map, origin, VfxEndPoint);
            }
            FinishLaunch();
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
