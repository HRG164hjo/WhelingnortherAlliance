using RimWorld;
using Verse;
using WNA.WNAMiscs;
using WNA.WNAModExtension;
using WNA.WNAUtility;

namespace WNA.WNAThingClass.WNAProj
{
    internal class Proj_Cluster : ProjBase
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            IntVec3 position = base.Position;
            WNAProjDamageContext ctx = WNAProjDamageContext.From(this);
            ThingDef projectileDef = def;
            ThingDef coverDef = CoverDef;
            ProjConfig cfg = Config;
            int amount = cfg.clusterAmount;
            float radius = cfg.clusterRadius;
            float factor = cfg.clusterDmgFactor;
            bool needLos = cfg.clusterRequireLoS;
            FleckDef fleck = cfg.clusterFleck ?? FleckDefOf.MicroSparks;
            base.Impact(hitThing, blockedByShield);
            if (map != null && amount > 0 && factor > 0f && ctx.damageDef != null)
            {
                ctx.amount *= factor;
                WNAProjCombatUtility.ApplyClusters(map, position, radius, amount, ctx, projectileDef, coverDef, needLos, affectNonPawns: true, fleck: fleck);
            }
        }
    }
}
