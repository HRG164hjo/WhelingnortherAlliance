using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;
using WNA.WNAUtility;

namespace WNA.WNARitual
{
    public class RitualBehaviorWorker_WorldPawnWarp : RitualBehaviorWorker
    {
        private const int MoteIntervalTicks = 20;

        public RitualBehaviorWorker_WorldPawnWarp()
        {
        }

        public RitualBehaviorWorker_WorldPawnWarp(RitualBehaviorDef def) : base(def)
        {
        }

        public override string CanStartRitualNow(TargetInfo target, Precept_Ritual ritual, Pawn selectedPawn = null, Dictionary<string, Pawn> forcedForRole = null)
        {
            if (!WorldPawnWarpUtility.RitualAvailable(out string reason))
                return reason;
            return base.CanStartRitualNow(target, ritual, selectedPawn, forcedForRole);
        }

        public override bool ShouldInitAsSpectator(Pawn pawn, RitualRoleAssignments assignments)
        {
            return false;
        }

        public override void Tick(LordJob_Ritual ritual)
        {
            base.Tick(ritual);
            if (ritual == null || Find.TickManager.TicksGame % MoteIntervalTicks != 0)
                return;
            Map map = ritual.Map;
            if (map == null)
                return;
            TargetInfo target = ritual.selectedTarget;
            if (!target.IsValid)
                return;
            if (target.HasThing)
                WorldPawnWarpUtility.SpawnRitualMote(target.Thing.DrawPos, map);
            else
                WorldPawnWarpUtility.SpawnRitualMote(target.Cell.ToVector3Shifted(), map);
        }
    }
}
