using RimWorld;
using Verse;

namespace WNA.WNAThingComp
{
    internal class PropEquippedAOEDamage : PropAOEDamage
    {
        public PropEquippedAOEDamage()
        {
            compClass = typeof(CompEquippedAOEDamage);
        }
    }
    internal class CompEquippedAOEDamage : CompAOEDamage
    {
        private Pawn lastWearer;
        protected Pawn Wearer
        {
            get
            {
                if (parent is Apparel apparel)
                    return apparel.Wearer;
                if (parent.ParentHolder is Pawn_EquipmentTracker equipment)
                    return equipment.pawn;
                return null;
            }
        }
        protected override bool Active
        {
            get
            {
                Pawn wearer = Wearer;
                return wearer != null
                    && wearer.Spawned
                    && !wearer.Dead
                    && !wearer.Downed
                    && !wearer.IsPrisoner;
            }
        }
        protected override IntVec3 EffectCenter => Wearer?.Position ?? parent.Position;
        protected override Map EffectMap => Wearer?.Map ?? parent.Map;
        protected override Faction EffectFaction => Wearer?.Faction;
        protected override bool ShouldResetCache(Faction recordedFaction)
        {
            if (Wearer != lastWearer)
            {
                lastWearer = Wearer;
                return true;
            }
            return base.ShouldResetCache(recordedFaction);
        }
    }
}
