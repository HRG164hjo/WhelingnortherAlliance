using System.Collections.Generic;
using RimWorld;
using Verse;
using WNA.WNAUtility;

namespace WNA.WNARitual
{
    public class RitualObligationTargetWorker_WorldPawnWarp : RitualObligationTargetFilter
    {
        private static readonly string[] AllowedDefNames = { "Altar_Small", "Altar_Medium", "Altar_Large", "RitualSpot" };

        public RitualObligationTargetWorker_WorldPawnWarp()
        {
        }

        public RitualObligationTargetWorker_WorldPawnWarp(RitualObligationTargetFilterDef def) : base(def)
        {
        }

        public override IEnumerable<TargetInfo> GetTargets(RitualObligation obligation, Map map)
        {
            if (map == null)
                yield break;
            for (int i = 0; i < AllowedDefNames.Length; i++)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamed(AllowedDefNames[i], false);
                if (def == null)
                    continue;
                List<Thing> things = map.listerThings.ThingsOfDef(def);
                for (int j = 0; j < things.Count; j++)
                    yield return things[j];
            }
        }

        protected override RitualTargetUseReport CanUseTargetInternal(TargetInfo target, RitualObligation obligation)
        {
            if (!target.HasThing || target.Thing == null)
                return false;
            Thing thing = target.Thing;
            if (!IsAllowedDef(thing.def))
                return false;
            if (thing.Faction != null && !thing.Faction.IsPlayer)
                return false;
            if (!WorldPawnWarpUtility.ResearchCompleted)
                return false;
            return true;
        }

        public override IEnumerable<string> GetTargetInfos(RitualObligation obligation)
        {
            yield return "WNA_WorldPawnWarpTargetInfo".Translate().Resolve();
        }

        private static bool IsAllowedDef(ThingDef def)
        {
            if (def == null)
                return false;
            for (int i = 0; i < AllowedDefNames.Length; i++)
            {
                if (def.defName == AllowedDefNames[i])
                    return true;
            }
            return false;
        }
    }
}
