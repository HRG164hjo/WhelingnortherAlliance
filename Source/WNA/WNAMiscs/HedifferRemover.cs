using System.Collections.Generic;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNAMiscs
{
    public class HedifferRemover : HediffGiver
    {
        public int tickInterval = 600;
        private static readonly HashSet<string> includedHediffs = new HashSet<string>
            {
                "DarkPsychicShock",
                "CubeInterest",
                "CubeWithdrawal",
                "MetalhorrorImplant",
                "WNA_VoidDiver"
            };
        private static readonly HashSet<string> validPawn = new HashSet<string>
            {
                "WNA_WNThan",
                "WNA_Human",
                "WNA_DimBeastRace",
                "WNA_ThornBeastRace",
                "WNA_FerociousBeastRace"
            };
        public override void OnIntervalPassed(Pawn pawn, Hediff cause)
        {
            if (pawn.Faction != null && pawn.Faction.def.defName == "WNA_FactionWNA") RemoveTargetHediffs(pawn);
            else if (pawn.Ideo != null && pawn.Ideo.HasPrecept(WNAMainDefOf.WNA_P_Proselyte)) RemoveTargetHediffs(pawn);
            else if (validPawn.Contains(pawn.def.defName)) RemoveTargetHediffs(pawn);
        }
        private bool IsValidHediff(Hediff hediff)
        {
            return (hediff is Hediff_Injury
                || (hediff.Severity > 0 && hediff.def.isBad)
                || includedHediffs.Contains(hediff.def.defName)
                );
        }
        private void RemoveTargetHediffs(Pawn pawn)
        {
            HediffSet hediffSet = pawn.health.hediffSet;
            List<Hediff> hediffsToRemove = new List<Hediff>();
            foreach (Hediff hediff in hediffSet.hediffs)
            {
                if (IsValidHediff(hediff))
                {
                    hediffsToRemove.Add(hediff);
                }
            }
            foreach (Hediff hediff in hediffsToRemove)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }
    }
}
