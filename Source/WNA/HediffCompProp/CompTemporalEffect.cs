using RimWorld;
using Verse;
using Verse.AI;

namespace WNA.HediffCompProp
{
    public class PropTemporalEffect : HediffCompProperties
    {
        public bool isMalicious = true;
        public PropTemporalEffect()
        {
            compClass = typeof(CompTemporalEffect);
        }
    }
    public class CompTemporalEffect : HediffComp
    {
        public PropTemporalEffect Props => (PropTemporalEffect)props;
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
