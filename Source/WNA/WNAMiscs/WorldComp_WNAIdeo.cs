using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNAMiscs
{
    public class WorldComp_WNAIdeo : WorldComponent
    {
        private readonly int interval = 53;
        public WorldComp_WNAIdeo(World world) : base(world) { }
        public override void WorldComponentTick()
        {
            if (Find.TickManager.TicksGame % interval != 0)
                return;
            List<Pawn> pawns = PawnsFinder.AllMapsWorldAndTemporary_Alive;
            for (int i = 0; i < pawns.Count; i++)
                ProcessPawn(pawns[i]);
        }
        private Faction wna => Find.FactionManager.FirstFactionOfDef(WNAMainDefOf.WNA_FactionWNA);
        private void ProcessPawn(Pawn pawn)
        {
            if (wna == null)
                return;
            Ideo ideo = wna.ideos.PrimaryIdeo;
            if (ideo == null)
                return;
            if (ShouldProcess(pawn))
            {
                pawn.ideo.SetIdeo(ideo);
                if (pawn.Faction != Faction.OfPlayer)
                    pawn.SetFaction(wna);
                else pawn.stances.stunner.StunFor(2357, null, false);
            }
        }
        private bool ShouldProcess(Pawn pawn)
        {
            return pawn.def == WNAMainDefOf.WNA_WNThan || pawn.def == WNAMainDefOf.WNA_Human;
        }
    }
}
