using RimWorld;
using Verse;
using WNA.WNAUtility;

namespace WNA.WNAGameComp
{
    internal class GameComp_PsychicDawn : GameComponent
    {
        private const int psychicDawnIntervalTicks = 2500;
        private const int educationMinIntervalTicks = 2000;
        private const int educationMaxIntervalTicks = 3000;
        private const int confessionIntervalTicks = 86400;
        private const int preservationIntervalTicks = 250;
        private const int memoryRedactionIntervalTicks = 20000;

        private int educationTicksUntilNext = -1;
        private int confessionTicksUntilNext = -1;
        private int memoryRedactionTicksUntilNext = -1;

        public GameComp_PsychicDawn(Game game) { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref educationTicksUntilNext, "educationTicksUntilNext", -1);
            Scribe_Values.Look(ref confessionTicksUntilNext, "confessionTicksUntilNext", -1);
            Scribe_Values.Look(ref memoryRedactionTicksUntilNext, "memoryRedactionTicksUntilNext", -1);
        }

        public override void GameComponentTick()
        {
            int ticks = Find.TickManager.TicksGame;

            if (WNAPsychicDawnUtility.Active && ticks % psychicDawnIntervalTicks == 0)
            {
                foreach (Pawn pawn in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction)
                    WNAPsychicDawnUtility.HealPawnGear(pawn);
            }

            if (WNAPsychicDawnUtility.EducationMotionActive)
                TickEducationMotion();

            if (WNAPsychicDawnUtility.ConfessionMotionActive)
                TickConfessionMotion();

            if (WNAPsychicDawnUtility.MemoryRedactionActive)
                TickMemoryRedaction();

            if (WNAPsychicDawnUtility.PreservationAdministrationActive && ticks % preservationIntervalTicks == 0)
            {
                foreach (Pawn pawn in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction)
                    WNAPsychicDawnUtility.ApplyPreservation(pawn);
            }
        }

        private void TickEducationMotion()
        {
            if (educationTicksUntilNext < 0)
            {
                educationTicksUntilNext = Rand.RangeInclusive(educationMinIntervalTicks, educationMaxIntervalTicks);
                return;
            }
            if (--educationTicksUntilNext > 0)
                return;
            educationTicksUntilNext = Rand.RangeInclusive(educationMinIntervalTicks, educationMaxIntervalTicks);
            WNAPsychicDawnUtility.RunEducationMotion();
        }

        private void TickConfessionMotion()
        {
            if (confessionTicksUntilNext < 0)
            {
                confessionTicksUntilNext = confessionIntervalTicks;
                return;
            }
            if (--confessionTicksUntilNext > 0)
                return;
            confessionTicksUntilNext = confessionIntervalTicks;
            WNAPsychicDawnUtility.RunConfessionMotion();
        }

        private void TickMemoryRedaction()
        {
            if (memoryRedactionTicksUntilNext < 0)
            {
                memoryRedactionTicksUntilNext = memoryRedactionIntervalTicks;
                return;
            }
            if (--memoryRedactionTicksUntilNext > 0)
                return;
            memoryRedactionTicksUntilNext = memoryRedactionIntervalTicks;
            WNAPsychicDawnUtility.RunMemoryRedaction();
        }
    }
}
