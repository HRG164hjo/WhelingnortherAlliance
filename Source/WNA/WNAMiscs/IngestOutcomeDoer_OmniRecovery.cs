using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace WNA.WNAMiscs
{
    public class IngestOutcomeDoer_OmniRecovery : IngestionOutcomeDoer
    {
        public float ratio = 0.8f;

        private const float PsyfocusMax = 1f;

        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
        {
            OffsetPsyfocus(pawn);
            OffsetNeeds(pawn);
            OffsetGeneResources(pawn);
        }
        private void OffsetPsyfocus(Pawn pawn)
        {
            pawn.psychicEntropy?.OffsetPsyfocusDirectly(Mathf.Max(200f, ratio * PsyfocusMax));
        }
        private void OffsetNeeds(Pawn pawn)
        {
            List<Need> needs = pawn.needs?.AllNeeds;
            if (needs == null)
                return;
            for (int i = 0; i < needs.Count; i++)
            {
                Need need = needs[i];
                if (need?.def == null || !IsRestorableNeed(need))
                    continue;
                need.CurLevel += Mathf.Max(2f, ratio * need.MaxLevel);
            }
        }
        private static bool IsRestorableNeed(Need need)
        {
            if (need is Need_Suppression)
                return false;
            return true;
        }
        private void OffsetGeneResources(Pawn pawn)
        {
            List<Gene> genes = pawn.genes?.GenesListForReading;
            if (genes == null)
                return;
            for (int i = 0; i < genes.Count; i++)
            {
                if (!(genes[i] is Gene_Resource resource) || !resource.Active)
                    continue;
                float max = resource.Max;
                if (max > 0f)
                    resource.Value += Mathf.Max(200f, ratio * max);
            }
        }
    }
}
