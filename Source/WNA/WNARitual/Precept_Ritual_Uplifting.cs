using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using WNA.WNAUtility;

namespace WNA.WNARitual
{
    public class Precept_Ritual_Uplifting : Precept_Ritual
    {
        public Thing pendingSubjectThing;
        public Pawn pendingSubjectPawn;
        public UpliftSubjectKind pendingSubjectKind = UpliftSubjectKind.Invalid;
        public IntVec3 pendingSubjectCell = IntVec3.Invalid;

        public void ClearPendingSubject()
        {
            pendingSubjectThing = null;
            pendingSubjectPawn = null;
            pendingSubjectKind = UpliftSubjectKind.Invalid;
            pendingSubjectCell = IntVec3.Invalid;
        }

        public override Window GetRitualBeginWindow(TargetInfo targetInfo, RitualObligation obligation = null, Action onConfirm = null, Pawn organizer = null, Dictionary<string, Pawn> forcedForRole = null, Pawn selectedPawn = null)
        {
            string canStart = behavior.CanStartRitualNow(targetInfo, this, selectedPawn, forcedForRole);
            if (!string.IsNullOrEmpty(canStart))
            {
                Messages.Message(canStart, targetInfo, MessageTypeDefOf.RejectInput, false);
                return null;
            }
            if (!UpliftUtility.RitualAvailable(out string reason))
            {
                Messages.Message(reason, targetInfo, MessageTypeDefOf.RejectInput, false);
                return null;
            }
            Map map = targetInfo.Map;
            if (map == null)
                return null;
            Messages.Message("WNA_UpliftSelectSubject".Translate(), MessageTypeDefOf.NeutralEvent, false);
            Find.Targeter.BeginTargeting(
                UpliftUtility.SubjectTargetingParameters(map),
                targ => OnSubjectSelected(targ, targetInfo, obligation, onConfirm, organizer, forcedForRole, selectedPawn),
                requiresCastedSelected: false);
            return null;
        }

        private void OnSubjectSelected(LocalTargetInfo targ, TargetInfo ritualTarget, RitualObligation obligation, Action onConfirm, Pawn organizer, Dictionary<string, Pawn> forcedForRole, Pawn selectedPawn)
        {
            Map map = ritualTarget.Map;
            if (map == null)
                return;
            if (!targ.HasThing)
                return;
            TargetInfo subjectTarget = targ.ToTargetInfo(map);
            if (!UpliftUtility.CanBeSubject(subjectTarget, map, out string subjectReason))
            {
                if (!subjectReason.NullOrEmpty())
                    Messages.Message(subjectReason, subjectTarget, MessageTypeDefOf.RejectInput, false);
                return;
            }
            string canStart = behavior.CanStartRitualNow(ritualTarget, this, selectedPawn, forcedForRole);
            if (!string.IsNullOrEmpty(canStart))
            {
                Messages.Message(canStart, ritualTarget, MessageTypeDefOf.RejectInput, false);
                return;
            }
            if (!UpliftUtility.RitualAvailable(out string reason))
            {
                Messages.Message(reason, ritualTarget, MessageTypeDefOf.RejectInput, false);
                return;
            }
            if (!UpliftUtility.TryResolveSubject(subjectTarget.Thing, out Thing subjectThing, out Pawn subjectPawn))
                return;
            pendingSubjectThing = subjectThing;
            pendingSubjectPawn = subjectPawn;
            pendingSubjectKind = UpliftUtility.Classify(subjectThing);
            pendingSubjectCell = subjectThing.PositionHeld.IsValid ? subjectThing.PositionHeld : subjectTarget.Cell;
            UpliftUtility.SpawnSubjectMarker(subjectThing, subjectPawn, map, subjectTarget.Cell);

            List<string> extraInfo = new List<string>();
            if (outcomeEffect?.def != null)
            {
                if (!outcomeEffect.def.extraInfoLines.NullOrEmpty())
                    extraInfo.AddRange(outcomeEffect.def.extraInfoLines);
                if (!outcomeEffect.def.extraPredictedOutcomeDescriptions.NullOrEmpty())
                {
                    foreach (string line in outcomeEffect.def.extraPredictedOutcomeDescriptions)
                        extraInfo.Add(line.Formatted(shortDescOverride ?? def.label));
                }
            }
            extraInfo.Add("WNA_UpliftSubjectInfo".Translate(subjectThing.Label.Named("SUBJECT")).Resolve());

            string ritualLabel = Label.CapitalizeFirst();
            Dialog_BeginRitual.ActionCallback action = delegate(RitualRoleAssignments assignments)
            {
                behavior.TryExecuteOn(ritualTarget, organizer, this, obligation, assignments, playerForced: true);
                onConfirm?.Invoke();
                return true;
            };
            Dialog_BeginRitual.PawnFilter filter = delegate(Pawn pawn, bool voluntary, bool allowOtherIdeos)
            {
                if (pawn == null || pawn == subjectPawn)
                    return false;
                if (pawn.GetLord() != null)
                    return false;
                if (pawn.IsSubhuman)
                    return false;
                if (pawn.RaceProps.Animal && !behavior.def.roles.Any(r => r.AppliesToPawn(pawn, out _, ritualTarget, null, null, null, skipReason: true)))
                    return false;
                return !ritualOnlyForIdeoMembers
                    || def.allowSpectatorsFromOtherIdeos
                    || pawn.Ideo == ideo
                    || !voluntary
                    || allowOtherIdeos
                    || pawn.IsPrisonerOfColony
                    || pawn.RaceProps.Animal
                    || (!forcedForRole.NullOrEmpty() && forcedForRole.ContainsValue(pawn));
            };
            List<Pawn> requiredPawns = organizer != null ? new List<Pawn> { organizer } : null;
            Dialog_BeginRitual dialog = new Dialog_BeginRitual(
                ritualLabel,
                this,
                ritualTarget,
                map,
                action,
                organizer,
                obligation,
                filter,
                "Begin".Translate(),
                requiredPawns,
                forcedForRole,
                null,
                extraInfo,
                selectedPawn);
            Find.WindowStack.Add(dialog);
        }
    }
}
