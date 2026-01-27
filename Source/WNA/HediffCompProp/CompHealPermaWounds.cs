using RimWorld;
using System.Linq;
using Verse;

namespace WNA.HediffCompProp
{
    public class PropHealPermaWounds : HediffCompProperties
    {
        public int wna_healTickerMin = 300;

        public int wna_healTickerMax = 500;
        public PropHealPermaWounds()
        {
            compClass = typeof(CompHealPermaWounds);
        }
    }
    public class CompHealPermaWounds : HediffComp
    {
        private int ticksToHeal = 1;
        public PropHealPermaWounds Props => (PropHealPermaWounds)props;
        public override void CompPostMake()
        {
            base.CompPostMake();
            ResetTicksToHeal();
        }
        private void ResetTicksToHeal()
        {
            ticksToHeal = Rand.Range(Props.wna_healTickerMin, Props.wna_healTickerMax);
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            ticksToHeal--;
            if (ticksToHeal <= 0)
            {
                TryHealRandomPermanentWound(base.Pawn, parent.LabelCap);
                ResetTicksToHeal();
            }
        }
        public static void TryHealRandomPermanentWound(Pawn pawn, string cause)
        {
            if (pawn.health.hediffSet.hediffs.Where((Hediff hd) => hd.IsPermanent() || hd.def.chronic).TryRandomElement(out var result))
            {
                HealthUtility.Cure(result);
                if (PawnUtility.ShouldSendNotificationAbout(pawn))
                {
                    Messages.Message("MessagePermanentWoundHealed".Translate(cause, pawn.LabelShort, result.Label, pawn.Named("PAWN")), pawn, MessageTypeDefOf.PositiveEvent);
                }
            }
        }
        public override void CompExposeData()
        {
            Scribe_Values.Look(ref ticksToHeal, "ticksToHeal", 0);
        }
        public override string CompDebugString()
        {
            return "ticksToHeal: " + ticksToHeal;
        }
    }
}
