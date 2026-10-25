using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using WNA.WNAUtility;

namespace WNA.WNAHarmony
{
    internal static class Patch_IdentityExclusion
    {
        [HarmonyPatch(typeof(FloatMenuMakerMap), nameof(FloatMenuMakerMap.GetOptions))]
        internal static class Patch_FloatMenuMakerMap_GetOptions
        {
            internal static bool Prefix(Vector3 clickPos, ref FloatMenuContext context, ref List<FloatMenuOption> __result)
            {
                if (!WNAPsychicDawnUtility.IdentityExclusionActive)
                    return true;

                List<Pawn> selected = Find.Selector.SelectedPawns;
                if (selected.Count != 1)
                    return true;

                Pawn pawn = selected[0];
                if (!WNAPsychicDawnUtility.IsIdentityExclusionTarget(pawn))
                    return true;

                Map map = Find.CurrentMap;
                if (map == null || pawn.MapHeld != map || pawn.Dead || pawn.Destroyed)
                    return true;

                if (!GenUI.ThingsUnderMouse(clickPos, 0.8f, TargetingParameters.ForPawns()).Contains(pawn))
                    return true;

                context = new FloatMenuContext(new List<Pawn> { pawn }, clickPos, map);
                if (!context.allSelectedPawns.Contains(pawn))
                    context.allSelectedPawns.Add(pawn);
                __result = new List<FloatMenuOption> { MakeOption(pawn) };
                return false;
            }

            private static FloatMenuOption MakeOption(Pawn pawn)
            {
                bool execute = pawn.IsSlaveOfColony
                    || pawn.IsPrisonerOfColony
                    || pawn.guilt?.IsGuilty == true;

                if (execute)
                {
                    string executeLabel = "WNA_IdentityExclusion_Execute".Translate(pawn.LabelShort);
                    return new FloatMenuOption(executeLabel,
                        delegate { WNAPsychicDawnUtility.ExecutePawn(pawn); },
                        MenuOptionPriority.High);
                }

                string sentenceLabel = "WNA_IdentityExclusion_Sentence".Translate(pawn.LabelShort);
                return new FloatMenuOption(sentenceLabel,
                    delegate { WNAPsychicDawnUtility.SentencePawn(pawn); },
                    MenuOptionPriority.High);
            }
        }

        [HarmonyPatch(typeof(HealthCardUtility), "VisibleHediffs")]
        internal static class Patch_HealthCardUtility_VisibleHediffs
        {
            internal static bool Prefix(Pawn pawn, bool showBloodLoss, ref IEnumerable<Hediff> __result)
            {
                if (!WNAPsychicDawnUtility.IdentityExclusionActive || pawn?.health?.hediffSet == null)
                    return true;
                __result = RevealedHediffs(pawn, showBloodLoss);
                return false;
            }

            private static IEnumerable<Hediff> RevealedHediffs(Pawn pawn, bool showBloodLoss)
            {
                List<Hediff_MissingPart> missingParts = pawn.health.hediffSet.GetMissingPartsCommonAncestors();
                for (int i = 0; i < missingParts.Count; i++)
                    yield return missingParts[i];

                List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
                for (int i = 0; i < hediffs.Count; i++)
                {
                    Hediff hediff = hediffs[i];
                    if (hediff is Hediff_MissingPart)
                        continue;
                    if (!hediff.Visible && hediff is Hediff_Injury)
                        continue;
                    if (!showBloodLoss && hediff.def == HediffDefOf.BloodLoss)
                        continue;
                    yield return hediff;
                }
            }
        }
    }
}
