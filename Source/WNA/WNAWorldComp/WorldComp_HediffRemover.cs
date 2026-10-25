using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;
using WNA.WNADefOf;
using static WNA.WNAUtility.General;

namespace WNA.WNAWorldComp
{
    internal class WorldComp_HediffRemover : WorldComponent
    {
        public WorldComp_HediffRemover(World world) : base(world){}
        private readonly int limit = 29;
        private int interval = 0;
        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            interval++;
            if (interval - limit >= 0)
            {
                List<Pawn> pawns = PawnsFinder.AllMapsWorldAndTemporary_Alive;
                for (int i = 0; i < pawns.Count; i++)
                    ProcessPawn(pawns[i]);
            }
        }
        private bool ShouldProcess(Pawn pawn)
        {
            if (IsWNARace(pawn))
                return true;
            if (WNAMainDefOf.WNA_PsychicDawn.IsFinished)
            {
                if (PawnHasIdeo(pawn, false))
                    return true;
                return false;
            }
            else
                return false;
        }
        private void ProcessPawn(Pawn pawn)
        {
            if (!ShouldProcess(pawn))
                return;
            HediffRemoveAll(pawn, true);
        }
    }
}
