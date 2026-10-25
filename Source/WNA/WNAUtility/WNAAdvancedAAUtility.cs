using RimWorld;
using System.Collections.Generic;
using Verse;

namespace WNA.WNAUtility
{
    internal static class WNAAdvancedAAUtility
    {
        public static bool IsAircraft(Thing thing)
        {
            if (thing == null || thing.def == null)
                return false;
            if (thing is Skyfaller || thing is PawnFlyer)
                return true;
            return thing.def.skyfaller != null || thing.def.pawnFlyer != null;
        }
        public static void CollectPayload(Thing aircraft, List<Thing> outPayload)
        {
            outPayload.Clear();
            if (!(aircraft is IThingHolder holder))
                return;
            Stack<IThingHolder> stack = new Stack<IThingHolder>();
            stack.Push(holder);
            while (stack.Count > 0)
            {
                IThingHolder current = stack.Pop();
                ThingOwner direct = current.GetDirectlyHeldThings();
                if (direct != null)
                {
                    for (int i = 0; i < direct.Count; i++)
                        AddPayloadThing(direct[i], outPayload, stack);
                }
                List<IThingHolder> children = new List<IThingHolder>();
                current.GetChildHolders(children);
                for (int i = 0; i < children.Count; i++)
                {
                    if (children[i] is Thing)
                        continue;
                    stack.Push(children[i]);
                }
            }
        }
        private static void AddPayloadThing(Thing thing, List<Thing> outPayload, Stack<IThingHolder> stack)
        {
            if (thing == null || thing.Destroyed)
                return;
            if (thing is Pawn || !(thing is IThingHolder childHolder) || !ThingOwnerUtility.AreImmediateContentsReal(childHolder))
            {
                outPayload.Add(thing);
                return;
            }
            bool holdsSomething = false;
            ThingOwner direct = childHolder.GetDirectlyHeldThings();
            if (direct != null && direct.Count > 0)
                holdsSomething = true;
            if (!holdsSomething)
            {
                List<IThingHolder> children = new List<IThingHolder>();
                childHolder.GetChildHolders(children);
                holdsSomething = children.Count > 0;
            }
            if (holdsSomething)
                stack.Push(childHolder);
            else
                outPayload.Add(thing);
        }
        public static Thing FindDestructibleContent(List<Thing> payload, out bool isPawn)
        {
            isPawn = false;
            Thing hitPointThing = null;
            for (int i = 0; i < payload.Count; i++)
            {
                Thing thing = payload[i];
                if (thing == null || thing.Destroyed)
                    continue;
                if (thing is Pawn pawn)
                {
                    if (pawn.Dead)
                        continue;
                    isPawn = true;
                    return pawn;
                }
                if (hitPointThing == null && thing.def != null && thing.def.useHitPoints)
                    hitPointThing = thing;
            }
            return hitPointThing;
        }
        public static Faction ResolveTargetFaction(Thing aircraft, List<Thing> payload)
        {
            for (int i = 0; i < payload.Count; i++)
            {
                Faction faction = payload[i]?.Faction;
                if (faction != null)
                    return faction;
            }
            return aircraft.Faction;
        }
    }
}
