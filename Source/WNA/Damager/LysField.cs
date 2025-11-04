using Verse;
using WNA.WNAUtility;

namespace WNA.Damager
{
    public class LysField : DamageWorker_AddInjury
    {
        protected override BodyPartRecord ChooseHitPart(DamageInfo dinfo, Pawn pawn)
        {
            return base.ChooseHitPart(dinfo, pawn);
        }
        public override DamageResult Apply(DamageInfo dinfo, Thing thing)
        {
            var result = base.Apply(dinfo, thing);
            if (thing != null && thing.MapHeld != null)
            {
                var manager = LysField_GameComp.Instance;
                manager?.AddOrUpdateField(thing, 1, 90);
                int lvl = manager.GetLevel(thing);
                LysisFieldUtility.SpreadLysisField(thing.Map, thing.Position, lvl);
            }
            return result;
        }
    }
}
