using System.Collections.Generic;
using UnityEngine;
using Verse;
using WNA.WNAMiscs;
using WNA.WNAModExtension;
using WNA.WNAUtility;

namespace WNA.WNAThingClass.WNAProj
{
    internal class Proj_Ambient : Proj_Inviso
    {
        private static readonly List<Thing> pierceTargets = new List<Thing>(16);

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            WNAProjDamageContext ctx = WNAProjDamageContext.From(this);
            ThingDef projectileDef = def;
            ThingDef coverDef = CoverDef;
            ProjConfig cfg = Config;
            float factor = cfg.ambientDmgFactor;
            Vector3 from = Launcher != null ? Launcher.DrawPos : origin;
            Vector3 to = hitThing != null ? hitThing.DrawPos : ExactPosition;
            WNAProjCombatUtility.CollectLineTargets(map, from, to, ctx.instigator, hitThing, cfg.ambientAffectsNonPawn, cfg.ambientWidth, cfg.ambientMaxAffect, pierceTargets);
            base.Impact(hitThing, blockedByShield);
            if (map != null && factor > 0f && ctx.damageDef != null)
            {
                ctx.amount *= factor;
                for (int i = 0; i < pierceTargets.Count; i++)
                    WNAProjCombatUtility.ApplyToThing(pierceTargets[i], ctx, 1f, projectileDef, coverDef);
            }
            pierceTargets.Clear();
        }
    }
}
