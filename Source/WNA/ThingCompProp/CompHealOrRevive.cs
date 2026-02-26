using RimWorld;
using Verse;
using Verse.AI;
using WNA.WNADefOf;

namespace WNA.ThingCompProp
{
    public class PropHealOrRevive : CompProperties
    {
        public PropHealOrRevive()
        {
            compClass = typeof(CompHealOrRevive);
        }
    }
    public class CompHealOrRevive : CompTargetEffect
    {
        public PropHealOrRevive Props => (PropHealOrRevive)props;
        public override void DoEffectOn(Pawn user, Thing target)
        {
            if (user.IsColonistPlayerControlled)
            {
                Job job = JobMaker.MakeJob(WNAMainDefOf.WNA_Job_Revive, target, parent);
                job.count = 1;
                job.playerForced = true;
                user.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }
        }
    }
}
