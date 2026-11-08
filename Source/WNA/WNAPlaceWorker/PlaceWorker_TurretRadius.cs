using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace WNA.WNAPlaceWorker
{
    // RimWorld's PlaceWorker_ShowTurretRadius matches verbClass against typeof(Verb_Shoot) exactly and
    // then dereferences the lookup result, so WNA's Verb_LaunchProjectile-derived turret verbs crash it.
    internal class PlaceWorker_TurretRadius : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map,
            Thing thingToIgnore = null, Thing thing = null)
        {
            ThingDef def = checkingDef as ThingDef;
            List<VerbProperties> verbs = def?.building?.turretGunDef?.Verbs;
            if (verbs == null)
            {
                return true;
            }
            for (int i = 0; i < verbs.Count; i++)
            {
                VerbProperties verb = verbs[i];
                if (!IsTurretVerb(verb.verbClass))
                {
                    continue;
                }
                if (verb.range > 0f)
                {
                    GenDraw.DrawRadiusRing(loc, verb.range);
                }
                if (verb.minRange > 0f)
                {
                    GenDraw.DrawRadiusRing(loc, verb.minRange);
                }
                break;
            }
            return true;
        }
        private static bool IsTurretVerb(Type verbClass)
        {
            return verbClass != null
                && (typeof(Verb_LaunchProjectile).IsAssignableFrom(verbClass) || typeof(Verb_Spray).IsAssignableFrom(verbClass));
        }
    }
}
