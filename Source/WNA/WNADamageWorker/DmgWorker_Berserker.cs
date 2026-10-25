using RimWorld;
using System;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNADamageWorker
{
    internal class DmgWorker_Berserker : DamageWorker
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            if (victim is Pawn pawn && !pawn.story.traits.HasTrait(WNAMainDefOf.WNA_Trait_Unshakable))
            {
                pawn.mindState.inspirationHandler.Reset();
                SetMentalState(pawn);
            }
            return new DamageResult();
        }
        internal void SetMentalState(Pawn pawn)
        {
            int[] numbers = { 0, 0, 0, 0, 0, 0, 1, 1, 1, 1 };
            bool wimp = pawn.story.traits.HasTrait(TraitDefOf.Wimp);
            Random random = new Random();
            int result = numbers[random.Next(numbers.Length)];
            switch (result)
            {
                case 0:
                    if (wimp)
                        pawn.health.AddHediff(HediffDefOf.CatatonicBreakdown);
                    else
                        pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.BerserkPermanent);
                    break;
                case 1:
                    if (wimp)
                        pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee);
                    else
                        pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent);
                    break;
            }
        }
    }
}
