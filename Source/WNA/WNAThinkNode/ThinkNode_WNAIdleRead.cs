using Verse;
using Verse.AI;
using WNA.WNAJobGiver;

namespace WNA.WNAThinkNode
{
    public class ThinkNode_WNAIdleRead : ThinkNode_Priority
    {
        public float chance = 0.7f;
        public float readPriority = 0.5f;

        public override float GetPriority(Pawn pawn)
        {
            return JobGiver_WNAReadHolyorder.CanRead(pawn) ? readPriority : 0f;
        }

        public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        {
            if (!Rand.Chance(chance))
                return ThinkResult.NoJob;

            return base.TryIssueJobPackage(pawn, jobParams);
        }

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            ThinkNode_WNAIdleRead obj = (ThinkNode_WNAIdleRead)base.DeepCopy(resolve);
            obj.chance = chance;
            obj.readPriority = readPriority;
            return obj;
        }
    }
}
