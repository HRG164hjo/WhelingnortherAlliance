using Verse;
using Verse.Sound;
using WNA.DMExtension;
using WNA.WNAUtility;

namespace WNA.WNAMiscs
{
    public class ProjType_IronBlast : ProjType_Inviso
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            if (map == null) return;
            if (hitThing != null)
                base.def.projectile.soundImpact?.PlayOneShot(new TargetInfo(hitThing.Position, map));
            else
            {
                var sound = base.def.projectile.soundImpactAnticipate ?? base.def.projectile.soundImpact;
                sound?.PlayOneShot(new TargetInfo(base.Position, map));
            }
            IntVec3 center = hitThing?.PositionHeld ?? base.Position;
            if (!center.InBounds(map)) return;
            float radius = 4.9f;
            var cfg = TechnoConfig.Get(base.def);
            if (cfg?.cellSpread.HasValue == true)
                radius = cfg.cellSpread.Value;
            IronCurtainUtility.IronKill(map, center, radius);
            try
            {
                ThingDef etherDef = DefDatabase<ThingDef>.GetNamedSilentFail("WNA_Ether_IronEffect");
                if (etherDef != null)
                {
                    Thing ether = ThingMaker.MakeThing(etherDef);
                    GenSpawn.Spawn(ether, center, map, WipeMode.Vanish);
                }
                else
                    Log.Warning("[WNA_IronBlast] ThingDef 'WNA_Ether_IronEffect' not found, skipping spawn.");
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[WNA_IronBlast] Failed to spawn WNA_Ether_IronEffect: {ex}");
            }
            if(!this.Destroyed) Destroy();
        }
    }
}
