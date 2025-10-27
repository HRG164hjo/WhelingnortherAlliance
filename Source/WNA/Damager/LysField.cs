using Verse;

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
                var manager = WNAUtility.LysField_GameComp.Instance;
                manager?.AddOrUpdateField(thing, 1, 90);
            }
            return result;
        }
    }
}
