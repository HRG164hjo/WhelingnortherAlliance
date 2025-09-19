using RimWorld;
using Verse;
using Verse.AI;

namespace WNA.HediffCompProp
{
    public class CompTemporalEffect : HediffCompProperties
    {
        public bool isMalicious = true;
        public CompTemporalEffect()
        {
            compClass = typeof(TemporalEffect);
        }
    }
    public class TemporalEffect : HediffComp
    {
        public CompTemporalEffect Props => (CompTemporalEffect)props;
        public override void CompPostTick(ref float severity)
        {
            parent.pawn.jobs?.StopAll();
            if (parent.pawn.jobs?.curJob?.def != JobDefOf.Wait_MaintainPosture)
            {
                Job job = JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture);
                parent.pawn.jobs.StartJob(job, JobCondition.InterruptForced);
            }
            parent.pawn.pather?.StopDead();
            parent.pawn.meleeVerbs?.TryMeleeAttack(null);
        }
    }
}
