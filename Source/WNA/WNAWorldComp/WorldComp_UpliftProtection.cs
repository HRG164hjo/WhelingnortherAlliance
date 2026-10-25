using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace WNA.WNAWorldComp
{
    public class WorldComp_UpliftProtection : WorldComponent
    {
        private static WorldComp_UpliftProtection cached;
        private HashSet<Thing> protectedThings = new HashSet<Thing>();
        private HashSet<Pawn> protectedPawns = new HashSet<Pawn>();

        public WorldComp_UpliftProtection(World world) : base(world)
        {
            cached = this;
        }

        public static WorldComp_UpliftProtection Instance => cached ??= Find.World?.GetComponent<WorldComp_UpliftProtection>();

        public bool IsProtected(Thing thing)
        {
            if (thing == null)
                return false;
            if (protectedThings.Contains(thing))
                return true;
            if (thing is Pawn pawn)
                return protectedPawns.Contains(pawn);
            if (thing is Corpse corpse)
                return corpse.InnerPawn != null && protectedPawns.Contains(corpse.InnerPawn);
            return false;
        }

        public bool IsProtected(Pawn pawn)
        {
            return pawn != null && protectedPawns.Contains(pawn);
        }

        public void Register(Thing subjectThing, Pawn subjectPawn)
        {
            if (subjectThing != null && !subjectThing.Destroyed)
                protectedThings.Add(subjectThing);
            if (subjectPawn != null && !subjectPawn.Destroyed)
                protectedPawns.Add(subjectPawn);
        }

        public void Unregister(Thing subjectThing, Pawn subjectPawn)
        {
            if (subjectThing != null)
                protectedThings.Remove(subjectThing);
            if (subjectPawn != null)
                protectedPawns.Remove(subjectPawn);
        }

    }
}
