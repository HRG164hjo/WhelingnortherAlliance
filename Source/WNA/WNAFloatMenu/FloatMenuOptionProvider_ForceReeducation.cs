using System;
using RimWorld;
using Verse;
using Verse.AI;
using WNA.WNADefOf;
using WNA.WNAUtility;

namespace WNA.WNAFloatMenu
{
    public class FloatMenuOptionProvider_ForceReeducation : FloatMenuOptionProvider
    {
        protected override bool Drafted => false;

        protected override bool Undrafted => true;

        protected override bool Multiselect => false;

        protected override bool RequiresManipulation => true;

        protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
        {
            if (!WNAPsychicDawnUtility.ConversionMotionActive || clickedPawn?.guest == null || !clickedPawn.IsPrisonerOfColony)
                return null;
            Pawn warden = context.FirstSelectedPawn;
            if (warden == null || !warden.IsFreeColonist)
                return null;
            string label = "WNA_ConversionMotion_ForceReeducate".Translate();
            if (clickedPawn.Downed)
                return new FloatMenuOption(label + ": " + "WNA_ConversionMotion_AlreadyDowned".Translate(), null);
            if (!warden.CanReach(clickedPawn, PathEndMode.Touch, Danger.Deadly))
                return new FloatMenuOption(label + ": " + "NoPath".Translate().CapitalizeFirst(), null);
            Action action = delegate
            {
                Job job = JobMaker.MakeJob(WNAMainDefOf.WNA_ForceReeducation, clickedPawn);
                warden.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            };
            return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(label, action, MenuOptionPriority.High, null, clickedPawn),
                warden,
                clickedPawn);
        }
    }
}
