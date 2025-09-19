using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using WNA.ThingCompProp;

namespace WNA.JobDriverClass
{
    public class AdjustHydroponics : JobDriver
    {
        private const TargetIndex BasinInd = TargetIndex.A;
        private const TargetIndex CropDefInd = TargetIndex.B;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(BasinInd, PathEndMode.ClosestTouch)
                .FailOnDespawnedOrNull(BasinInd);
            Toil chooseCrop = new Toil();
            chooseCrop.initAction = delegate
            {
                Pawn actor = chooseCrop.actor;
                Thing hydroponicsThing = job.targetA.Thing;
                Hydroponics compHydroponics = hydroponicsThing?.TryGetComp<Hydroponics>();

                if (compHydroponics == null)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                List<FloatMenuOption> options = new List<FloatMenuOption>
                {
                    new FloatMenuOption("WNA_None".Translate(), () =>
                    {
                        compHydroponics.ChooseCrop(null);
                        chooseCrop.actor.jobs.curDriver.ReadyForNextToil();
                    })
                };
                IEnumerable<ThingDef> validCrops = DefDatabase<ThingDef>.AllDefs.Where(def => compHydroponics.IsValidCrop(def));
                foreach (ThingDef plantDef in validCrops)
                {
                    if (plantDef == null) continue;
                    if (compHydroponics.Props.yieldFactor == 0 && !compHydroponics.extraCrops.Contains(plantDef.defName)) continue;

                    options.Add(new FloatMenuOption(
                        plantDef.LabelCap,
                        () =>
                        {
                            compHydroponics.ChooseCrop(plantDef);
                            chooseCrop.actor.jobs.curDriver.ReadyForNextToil();
                        },
                        plantDef,
                        extraPartWidth: 29f,
                        extraPartOnGUI: rect => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, plantDef)
                    ));
                }
                if (options.Count == 1)
                {
                    options.Add(new FloatMenuOption("WNA_NoValidCropsAvailable".Translate(), null));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            };
            chooseCrop.defaultCompleteMode = ToilCompleteMode.Never;
            yield return chooseCrop;
            yield return new Toil
            {
                initAction = delegate
                {
                    chooseCrop.actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}
