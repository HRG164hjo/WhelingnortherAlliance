using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNAUtility
{
    public class WorldComponent_HediffRemover : WorldComponent
    {
        private readonly int interval = 53;
        public WorldComponent_HediffRemover(World world) : base(world) { }
        internal static readonly HashSet<string> listedHediffs = new HashSet<string>()
        {
            "WNA_VoidDiver",
            // requires Anomaly DLC
            "DarkPsychicShock",
            "CubeInterest",
            "CubeWithdrawal",
            "MetalhorrorImplant",
        };
        public override void WorldComponentTick()
        {
            if (Find.TickManager.TicksGame % interval != 0)
                return;
            List<Pawn> pawns = PawnsFinder.AllMapsWorldAndTemporary_Alive;
            for (int i = 0; i < pawns.Count; i++)
                ProcessPawn(pawns[i]);
        }
        private void ProcessPawn(Pawn pawn)
        {
            if (!ShouldProcess(pawn))
                return;
            var hediffs = pawn.health?.hediffSet?.hediffs;
            if (hediffs == null) return;
            for (int i = hediffs.Count - 1; i >= 0; i--)
            {
                Hediff h = hediffs[i];
                if (IsValidHediff(h))
                    pawn.health.RemoveHediff(h);
            }
        }
        private bool ShouldProcess(Pawn pawn)
        {
            if (pawn.Ideo != null && pawn.Ideo.HasPrecept(WNAMainDefOf.WNA_P_Proselyte))
                return true;
            return pawn.def == WNAMainDefOf.WNA_WNThan || pawn.def == WNAMainDefOf.WNA_Human;
        }

        private bool IsValidHediff(Hediff h)
        {
            return h is Hediff_Injury
                || h is Hediff_MissingPart
                || (h.Severity > 0 && h.def.isBad)
                || listedHediffs.Contains(h.def.defName);
        }
    }
}