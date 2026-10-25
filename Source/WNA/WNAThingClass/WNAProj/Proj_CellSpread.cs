using Verse;
using WNA.WNAMiscs;
using WNA.WNAModExtension;
using WNA.WNAUtility;

namespace WNA.WNAThingClass.WNAProj
{
    internal class Proj_CellSpread : ProjBase
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            IntVec3 position = base.Position;
            WNAProjDamageContext ctx = WNAProjDamageContext.From(this);
            ThingDef projectileDef = def;
            ThingDef coverDef = CoverDef;
            ProjConfig cfg = Config;
            float radius = cfg.cellSpread;
            float falloffMin = cfg.percentAtMax;
            bool needLos = cfg.affectRequireLoS;
            base.Impact(hitThing, blockedByShield);
            if (map != null && radius > 0f && ctx.damageDef != null)
            {
                WNAProjCombatUtility.ApplyInRadius(map, position, radius, ctx, projectileDef, coverDef, needLos, falloffMin, hitThing, affectNonPawns: true);
            }
        }
    }
}
