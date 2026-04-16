using RimWorld;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNAThingCompProp
{
    public class PropFactionDeed_HostileDestruct : CompProperties
    {
        public FactionDef targetFactionDef;
        public PropFactionDeed_HostileDestruct()
        {
            compClass = typeof(CompFactionDeed_HostileDestruct);
        }
    }
    public class CompFactionDeed_HostileDestruct : ThingComp
    {
        public PropFactionDeed_HostileDestruct Props => (PropFactionDeed_HostileDestruct)props;
        public override void CompTickRare()
        {
            base.CompTickRare();
            if (!parent.Spawned) return;
            var fac = Find.FactionManager.FirstFactionOfDef(Props.targetFactionDef);
            if (fac == null) return;
            if (fac.RelationKindWith(Faction.OfPlayer) == FactionRelationKind.Hostile)
                parent.Destroy(DestroyMode.Vanish);
        }
    }
}
