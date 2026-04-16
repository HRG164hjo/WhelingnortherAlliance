using Verse;
using Verse.AI;

namespace WNA.WNAVerbType
{
    public class VerbType_NoFriendFire : Verb_Shoot
    {
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (!base.CanHitTargetFrom(root, targ))
                return false;
            return !IsSameFactionPawnTarget(targ);
        }
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!base.ValidateTarget(target, showMessages))
                return false;
            if (IsSameFactionPawnTarget(target))
            {
                if (caster is Pawn p)
                    p.jobs.EndCurrentJob(JobCondition.InterruptForced);
                return false;
            }
            return true;
        }
        private bool IsSameFactionPawnTarget(LocalTargetInfo target)
        {
            if (!target.IsValid || target.Pawn == null)
                return false;
            var targetPawn = target.Pawn;
            var myFaction = caster?.Faction;
            if (myFaction == null)
                return false;
            return targetPawn.Faction == myFaction;
        }
    }
}
