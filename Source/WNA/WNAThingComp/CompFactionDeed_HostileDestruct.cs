using RimWorld;
using Verse;

namespace WNA.WNAThingComp
{
    internal class PropFactionDeed_HostileDestruct : CompProperties
    {
        public FactionDef targetFactionDef = null;
        public PropFactionDeed_HostileDestruct()
        {
            compClass = typeof(CompFactionDeed_HostileDestruct);
        }
    }
    internal class CompFactionDeed_HostileDestruct : ThingComp
    {
        public PropFactionDeed_HostileDestruct Props => (PropFactionDeed_HostileDestruct)props;
        public override void CompTickRare()
        {
            base.CompTickRare();
            if (!parent.Spawned)
                return;
            var fac = Find.FactionManager.FirstFactionOfDef(Props.targetFactionDef);
            if (fac == null)
                return;
            if (fac.RelationKindWith(Faction.OfPlayer) == FactionRelationKind.Hostile)
            {
                GenExplosion.DoExplosion(
                    parent.Position, parent.Map,
                    0.9f, DamageDefOf.EMP,
                    parent, 1, 0f,
                    DefDatabase<SoundDef>.GetNamed("Explosion_EMP"));
                parent.Destroy(DestroyMode.Vanish);
            }
        }
    }
}
