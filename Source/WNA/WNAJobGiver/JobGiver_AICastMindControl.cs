using RimWorld;
using Verse;
using Verse.AI;
using WNA.WNAModExtension;
using WNA.WNAUtility;

namespace WNA.WNAJobGiver
{
    public class JobGiver_AICastMindControl : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Downed || pawn.Map == null || pawn.abilities == null)
                return null;
            if (pawn.Faction == null || pawn.Faction == Faction.OfPlayer || pawn.IsPrisoner || pawn.IsSlave)
                return null;

            foreach (Ability ability in pawn.abilities.AllAbilitiesForReading)
            {
                if (ability?.def == null || !ability.def.aiCanUse || !ability.CanCast)
                    continue;
                MindControlConfig config = MindControlConfig.Get(ability.def);
                if (config == null)
                    continue;
                if (pawn.CurJob?.ability != null)
                    return null;

                LocalTargetInfo target = FindTarget(pawn, ability, config);
                if (target.IsValid)
                    return ability.GetJob(target, target);
            }
            return null;
        }

        private static LocalTargetInfo FindTarget(Pawn pawn, Ability ability, MindControlConfig config)
        {
            float range = ability.def.verbProperties?.range ?? 0f;
            if (range <= 0f)
                range = 30f;
            Thing found = GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                ThingRequest.ForGroup(ThingRequestGroup.Pawn),
                PathEndMode.Touch,
                TraverseParms.For(pawn, Danger.Deadly, TraverseMode.NoPassClosedDoors, false),
                range,
                thing =>
                {
                    Pawn target = thing as Pawn;
                    if (target == null || target == pawn || target.Dead || target.Downed || !target.Spawned)
                        return false;
                    if (target.Map != pawn.Map || !target.HostileTo(pawn))
                        return false;
                    if (target.RaceProps.intelligence != Intelligence.Humanlike)
                        return false;
                    if (!WNAMindControlUtility.CanBeControlled(pawn, target, ability.def, config, out _))
                        return false;
                    return ability.AICanTargetNow(target);
                });
            return found != null ? new LocalTargetInfo(found) : LocalTargetInfo.Invalid;
        }
    }
}
