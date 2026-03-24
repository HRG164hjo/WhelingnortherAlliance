using RimWorld;
using Verse;
using WNA.WNADefOf;

namespace WNA.ThingCompProp
{
    public class PropFactionDeed_TargetPawn : CompProperties_Targetable
    {
        public FactionDef targetFactionDef;
        public PropFactionDeed_TargetPawn()
        {
            compClass = typeof(CompFactionDeed_TargetPawn);
        }
    }
    public class CompFactionDeed_TargetPawn : CompTargetable_SinglePawn
    {
        public PropFactionDeed_TargetPawn DeedProps => (PropFactionDeed_TargetPawn)props;
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!base.ValidateTarget(target, showMessages)) return false;
            if (!target.TryGetPawn(out var pawn)) return false;
            if (pawn.Map != Find.CurrentMap) return false;
            var fac = Find.FactionManager.FirstFactionOfDef(DeedProps.targetFactionDef);
            if (fac == null || pawn.Faction != fac) return false;
            return true;
        }
    }
}
